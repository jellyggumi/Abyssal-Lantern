# Castle Busters QA and Release Continuation Guide

**Audience:** maintainers and release operators  
**Status:** implementation QA is green for the recorded focused gates; paid-store release is blocked  
**Updated:** 2026-07-13  
**Scope:** repeatable verification, evidence interpretation, and the next safe work order for the current Unity 2022.3 vertical slice.

This is an internal maintenance guide. It does not claim that the game is ready for a paid-store submission or that the full PlayMode assembly has passed.

## 1. Current product and release boundary

The current vertical slice is a compact single-player physics-siege demo: read wind, choose a unit, breach a destructible castle, and complete a Stage 1 siege. Its strategy contract keeps real-time PvP, gacha, ads, and live-service infrastructure out of scope. See [the frozen vertical-slice quality contract](specs/castle-busters-vertical-slice-quality.md).

A `Chronicle Pack` storefront prototype exists, but it remains a **non-production boundary**:

- Title replay consumes only the non-authoritative local `PlayerPrefs` entitlement cache; it is not payment proof.
- The project now pins Unity IAP 5.4.1, and `MobileStorefront` owns a source-level IAP 5 adapter. Its resolved Android Billing Library dependency is 9.0.0.
- This source state is not evidence that a shipping artifact is submission-compatible or that a store transaction can establish payment ownership.
- No store metadata, signing, payment-validation service, console configuration, or physical-device purchase evidence exists in this repository.

The full store gate and its operator steps are in [Mobile Store Release Guide](mobile_release_guide.md). Do not describe the prototype as an available paid product.

## 2. Test assembly contract

Tests are intentionally split by execution model:

| Assembly | Source folder | Platform | Purpose |
|---|---|---|---|
| `CastleBusters.EditModeTests` | `Assets/Tests/EditMode/` | Editor | fast deterministic logic and UI-contract checks |
| `CastleBusters.PlayModeTests` | `Assets/Tests/PlayMode/` | all player platforms | scene lifecycle, UI interaction, rendering, and turn-flow checks |

The obsolete monolithic test assembly is removed. New tests must be added to the matching folder rather than a legacy `Assets/Tests/` root.

`RuntimeReliabilityRegressionTests.RestoreRuntimeState()` restores PlayerPrefs **before** scene loading. This prevents test-mutated progression, economy, entitlement, or leaderboard state from being stranded if a scene operation fails unexpectedly.

## 3. Recorded verification evidence

| Gate | Result | Evidence | What it proves |
|---|---:|---|---|
| Complete EditMode suite | **155 / 155 passed** | `/tmp/unknown-castle-editmode-complete.xml` | the current Editor assembly contracts compile and pass |
| Runtime reliability PlayMode fixture | **18 / 18 passed**, 88.30 s | `/tmp/unknown-castle-playmode-reliability.xml` | title/intro, unit launch, AI turn resolution, Last Stand, HUD arbitration, match reset, Stage picker, stage art, generated gimmicks, VFX cleanup, Webtoon construction, and Chronicle replay |
| Mobile narrative/commerce fixture | **5 / 5 passed** | `/tmp/unknown-castle-iap5-fi2tu7fy/iap-v5-mobile-narrative-results.xml` | IAP 5.4.1 source-adapter compilation, catalog definition ID/type, safe-area and Unicode-safe typewriter behavior, and Editor storefront no-grant behavior |
| Chronicle replay flow | **1 / 1 passed**, 3.33 s | `/tmp/unknown-castle-chronicle-replay.xml` | entitlement-gated title action, Webtoon replay, title return, paused Intro state, and no storefront/economy/progression/leaderboard mutation |

The focused mobile fixture was recorded at 2026-07-13 12:35:51Z in a disposable Unity 2022.3.62f2 project copy. It does **not** demonstrate an actual store purchase, payment ownership, backend validation, console setup, or physical-device behavior.

The full 37-test PlayMode assembly is **not** a green release gate. `CastleBustersAnalysisTests` includes 10–15 minute test timeouts; an earlier unfiltered run exceeded the outer limit in legacy 30-game analysis. Keep that workload in a dedicated long-run or CI lane.

## 4. Running the focused regression gates

### Prerequisites

1. Use the pinned Unity Editor `2022.3.62f2`.
2. Close any Unity Editor instance holding the target project; Unity batch mode cannot open a project already open in another Editor process.
3. Preserve the source project. The previously recorded clean test runs used a disposable local project copy with the Unity-MCP package removed only there, because an unauthenticated background MCP log can contaminate test results. Do not remove packages from the working project merely to run a test.
4. Give every invocation a unique `-testResults` path, then inspect its XML before treating it as evidence.

Unity batch-mode reference: <https://docs.unity3d.com/2022.3/Documentation/Manual/EditorCommandLineArguments.html>

### Complete EditMode suite

```sh
UNITY="/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics \
  -projectPath "$PROJECT_COPY" \
  -runTests -testPlatform EditMode \
  -testResults /tmp/unknown-castle-editmode-complete.xml
```

### Focused runtime PlayMode fixture

```sh
UNITY="/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics \
  -projectPath "$PROJECT_COPY" \
  -runTests -testPlatform PlayMode \
  -testFilter '^CastleBusters\.Tests\.RuntimeReliabilityRegressionTests\.' \
  -testResults /tmp/unknown-castle-playmode-reliability.xml
```

### Focused mobile IAP source fixture

```sh
UNITY="/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity"
"$UNITY" -batchmode -nographics \
  -projectPath "$PROJECT_COPY" \
  -runTests -testPlatform EditMode \
  -testFilter '^CastleBusters\.Tests\.MobileNarrativeCommerceTests\.' \
  -testResults /tmp/unknown-castle-iap5-fi2tu7fy/iap-v5-mobile-narrative-results.xml
```

For this project’s recorded test commands, do **not** add `-quit`: Unity Test Framework schedules the run on an Editor update, and prior evidence was only emitted reliably without that argument. This is a project-specific operating rule, not a replacement for Unity’s general command-line guidance.

A valid XML must have `total` greater than zero and `failed="0"`. A `total="0"` result is a filter miss, not a pass.

Unity Test Framework reference: <https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/index.html>

## 5. Mandatory invariants for follow-up work

- An unavailable Editor storefront must never grant ownership or start a fake purchase.
- A replayed purchase or restoration callback must preserve a boolean entitlement; it must not create a repeatable gameplay reward.
- Unlocks must not alter combat values, stage progression, leaderboard results, or rewards.
- Locked stage cards are dim and non-interactive; only a selectable unlocked card may render as selected.
- Full-bleed visual layers may bypass the safe-area root only when non-interactive. Interactive UI must use `MobileSafeArea.GetContentRoot(canvas)`.
- A runtime test must restore all mutated static and persistent state, including PlayerPrefs, even if a scene transition fails.

Safe-area API reference: <https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Screen-safeArea.html>

## 6. Next work order

### Completed source-controlled slice

Title replay consumes only the non-authoritative local `MobileStoreEntitlements.HasChroniclePack` cache at title construction. It is absent while unentitled, opens the existing Webtoon Prologue through an explicit click while entitled, and remains available after SKIP returns to a fresh paused title. The focused PlayMode regression passed **1 / 1** at `/tmp/unknown-castle-chronicle-replay.xml`.

`MobileStorefront` now owns a source-level IAP 5 adapter. Neither the title replay cache nor that adapter establishes payment authority: the cache is not payment proof, and the adapter has no proven store or server transaction.

The slice does not grant authoritative ownership or alter combat/progression/leaderboards; it exposes no verified or publishable paid product until the external gates are met.

### External release prerequisites: blocked outside the repository

- Verify the shipping Android artifact's Billing Library compatibility at submission time. The current Google Play policy requires Billing Library 8 or later for new apps and updates by 2026-08-31; the source-resolved IAP 5.4.1 dependency is Billing Library 9.0.0, but that is not shipping-artifact evidence.
- Determine whether Unity IAP 5.4's Developer Data consent/`UnityConsent` flow applies to the shipped product; implement it when applicable and retain verification evidence. Generic privacy metadata is not proof that this gate is complete.
- Add authenticated server-side transaction validation and a server-owned entitlement record.
- Configure the exact products and required metadata in Google Play Console and App Store Connect.
- Execute sandbox/test-track purchase, cancellation, pending/deferred, restore, cold-relaunch, and offline scenarios on physical Android and iOS devices.

## 7. Authoritative external references

- Unity Test Framework: <https://docs.unity3d.com/Packages/com.unity.test-framework@1.1/manual/index.html>
- Unity 2022.3 command-line arguments: <https://docs.unity3d.com/2022.3/Documentation/Manual/EditorCommandLineArguments.html>
- Unity `Screen.safeArea`: <https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Screen-safeArea.html>
- Unity IAP 5 migration guide: <https://docs.unity.com/en-us/iap/upgrade-to-iap-v5>
- Unity IAP 5 purchase lifecycle and acknowledgement: <https://docs.unity.com/en-us/iap/purchases>
- Unity IAP 5.4.1 changelog: <https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/changelog/CHANGELOG.html>
- Google Play Billing integration testing: <https://developer.android.com/google/play/billing/test>
- Google Play Billing Library deprecation policy: <https://developer.android.com/google/play/billing/deprecation-faq>
- Apple non-consumable IAP setup: <https://developer.apple.com/help/app-store-connect/manage-in-app-purchases/create-consumable-or-non-consumable-in-app-purchases/>
- Apple StoreKit Sandbox testing: <https://developer.apple.com/documentation/storekit/testing-in-app-purchases-with-sandbox>

## 8. Evidence and strategy records

- [Vertical-slice quality contract](specs/castle-busters-vertical-slice-quality.md)
- [Mobile Store Release Guide](mobile_release_guide.md)
- `~/vaults/llm-wiki/wiki/reports/castle-busters-implementation-log.md` — append-only implementation and verification evidence
- `~/vaults/llm-wiki/wiki/reports/castle-busters-qa-balance-cycles.md` — evidence-backed QA cycles
