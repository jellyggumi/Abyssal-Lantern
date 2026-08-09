# Castle Busters Mobile Store Release Guide

**Status:** pre-production release gate  
**Owner:** project owner / release operator  
**Updated:** 2026-07-13

This guide covers the one-product mobile commerce slice in `MobileStorefront` and its release boundary. It is an operator guide, not a claim that the current project is ready for a paid-store release.

## 1. Product contract

| Field | Value |
|---|---|
| Product ID | `com.jangyoung.unknowncastle.chronicle_pack` |
| Store type | One-time, non-consumable IAP |
| Intended entitlement | Replay the webtoon prologue from the local prototype entitlement; payment proof remains absent |
| Gameplay effect | None — it must not change combat values, stage access, or leaderboard results |
| Displayed price | Store-provided localized product metadata only |
| Supported storefronts | Google Play and Apple App Store only |

The identifier is a contract. Create this exact ID unchanged in **both** Google Play Console and App Store Connect before attempting a device purchase. Do not price, ship, or advertise a different entitlement under this ID.

## 2. Current implementation boundary

The current source path is deliberately narrow and uses the Unity IAP 5 `StoreController` lifecycle rather than IAP 4 listeners, `ProcessPurchase`, receipts, or platform extensions:

1. `MobileStorefront.InitializeIfSupported()` obtains the `StoreController`, subscribes to its lifecycle and order events, then connects to the store.
2. After connecting, it fetches exactly one `ProductDefinition`: `com.jangyoung.unknowncastle.chronicle_pack` as `ProductType.NonConsumable`. After that catalog result, it fetches purchases.
3. Fetched-pending processing is configured explicitly for manual handling. A pending order is cached and confirmed only when it has exactly one Chronicle non-consumable item and a non-empty transaction ID. That cache write is a local prototype behavior, not payment validation.
4. A confirmed order with that same exact one-item Chronicle contract can reconstruct the local cache. Deferred, unknown, malformed, multi-item, wrong-product, wrong-type, or missing-transaction-ID orders do not grant the cache and are not confirmed.
5. `MobileStorefrontPanel` renders only the localized price returned by the store; it never contains a hard-coded currency amount.

### Critical release blockers: the source migration is complete; the client remains non-authoritative

`MobileStoreEntitlements` persists a replay flag as an unencrypted, user-modifiable `PlayerPrefs` integer. It is a deliberately non-authoritative prototype cache: it is never payment proof, never store-ownership proof, and never a substitute for a verified entitlement record. The source-only Unity IAP migration is complete at **com.unity.purchasing 5.4.1**. The resolved package evidence in the focused sandbox identifies Android Play Billing Library **9.0.0**. That evidence proves source package resolution and compilation only; it does not prove the Billing Library embedded in a shipping Android artifact.

Google's current deprecation table requires Play Billing Library **8 or newer** for new apps and updates submitted on or after **2026-08-31**. Before release, inspect the generated Android artifact and retain evidence that the version it actually packages is accepted for the submission date. This release decision remains blocked even though the source package is currently compliant.

Before a paid release, implement both of the following and test them on both stores:

- **Authoritative path — required for a paid launch:** backend transaction validation and a server-owned entitlement record keyed to an authenticated player account.
- **Client-side tamper resistance — transitional defense, not authority:** project-generated obfuscated Google Play and Apple validation data, `CrossPlatformValidator`, explicit handling of its validation failures, and a parsed-receipt product-ID check before granting anything. This only raises the cost of tampering; it does not replace server validation.

Do not generate placeholder validation key classes, accept a fabricated receipt, or use a local preference flag as payment evidence. Apple’s classic receipt-validation path is transitional; the durable iOS design should validate StoreKit 2 signed transaction data on the server. Keep entitlement granting idempotent: one verified Chronicle product must unlock one replay feature, even if purchase callbacks or restoration are replayed.

### Current feature wiring

`IntroScreenController` now renders `ChronicleReplayButton` only when `MobileStoreEntitlements.HasChroniclePack` is already true. `GameManager` routes that explicit title action through the existing `WebtoonPrologueController` and returns to title after SKIP/completion. The action only consumes the non-authoritative local prototype cache: it does not grant authoritative ownership, initialize a store, modify gameplay, or validate a payment. A physical-device purchase/restore test and server-authoritative entitlement evidence remain mandatory before the entitlement can be marketed, priced, or submitted.

## 3. Required store configuration

### Google Play Console

1. Create a **one-time product** with the exact product ID in the table above.
2. Complete its price, tax, localization, and required policy metadata.
3. Configure a test track and licensed test accounts before distribution.
4. Install the test-track build through Google Play on a physical Android device and complete a test purchase.

Google Play Billing integration testing: <https://developer.android.com/google/play/billing/test>

### App Store Connect

1. Create a **non-consumable** IAP with the exact product ID in the table above.
2. Supply required localization, price schedule, review information, and any required screenshots.
3. Create and use Sandbox test accounts. Allow metadata propagation time before treating an unavailable product as a code defect.
4. Test purchase, cancellation, Ask to Buy / deferred approval where applicable, and Restore Purchases on a physical iOS device.

Apple references:

- Create consumable or non-consumable IAP: <https://developer.apple.com/help/app-store-connect/manage-in-app-purchases/create-consumable-or-non-consumable-in-app-purchases/>
- Sandbox test overview: <https://developer.apple.com/documentation/storekit/testing-in-app-purchases-with-sandbox>

## 4. Mobile player configuration

Run **Castle Busters → Apply Mobile Landscape Release Baseline** before generating Android or iOS builds. It sets the project to landscape-only auto-rotation and IL2CPP for both target groups; it intentionally does not set bundle identifiers, signing certificates, or store credentials.

`MobileSafeArea` creates a `MobileSafeArea` parent for interactive/runtime content and maps `Screen.safeArea` into normalized anchors. Every interactive or content root in a new full-screen runtime UI must be parented through `MobileSafeArea.GetContentRoot(canvas)`. Intentionally full-bleed, non-interactive dim or backdrop layers MAY remain direct `Canvas` children only after they pass the device safe-area checks below.

Unity 2022.3 safe-area reference: <https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Screen-safeArea.html>

Required device checks:

- Landscape-left and landscape-right on an ordinary phone.
- A notched or rounded-corner device in both allowed rotations.
- A tablet and an Android device with a non-default display cutout.
- Store overlay, cancellation, and restoration with the safe-area layout still visible after the callback.

## 5. Storefront test matrix

Use store-distributed builds on physical devices. Unity Editor behavior is expected to report the storefront as unavailable; it is not purchase evidence.

| Scenario | Expected result | Release evidence |
|---|---|---|
| First launch, no ownership | Store connects, fetches the exact one-item non-consumable definition, then fetches purchases; localized price is shown when the product is available | Android and iOS screen recording plus store-console product state |
| Successful purchase | The paid build grants only after backend validation records the authenticated account's Chronicle entitlement; the replay action becomes available | Verified server transaction/entitlement record, post-purchase recording, and replay-access recording |
| Cancelled purchase | No entitlement; status describes failure/cancel state; player can retry | Recording and console callback log |
| Pending / delayed Google Play payment | **Shipping behavior:** no paid entitlement before server validation confirms `PURCHASED`. The current source-only prototype grants its local `PlayerPrefs` replay flag and confirms only a matching one-item Chronicle pending order with a transaction ID; that action is neither a paid entitlement nor release evidence. | Slow-test-card approval and decline recordings with transaction state |
| Deferred / Ask to Buy | No cache grant, no confirmation, and no paid entitlement before approval and server validation; UI stays non-granting | iOS sandbox evidence where account/device supports it |
| Cold relaunch after purchase | Backend-verified store ownership restores the entitlement exactly once; the current source-only prototype can only reconstruct its local cache from matching fetched pending or confirmed Chronicle orders | Relaunch recording on each store and server entitlement record |
| Offline or unavailable catalog | No purchase starts and no local entitlement is granted | Device recording and error-state capture |
| Replayed callback / duplicate restore | No duplicate gameplay reward; entitlement remains a boolean replay unlock | Automated regression test plus device evidence |

## 6. Release gate

A paid-store submission is blocked until every item below has evidence. The completed source-only IAP 5.4.1 migration does not satisfy the shipping-artifact, server-authority, store-console, or physical-device gates:

- [ ] Product IDs, type, localization, price, and review metadata are complete in both stores.
- [ ] Android and iOS bundle IDs, signing, version codes/build numbers, privacy declarations, and store accounts are owner-configured.
- [ ] The release owner determines whether Unity IAP 5.4 Developer Data consent / `UnityConsent` applies, implements that flow when applicable, and retains verification evidence; the generic privacy declaration above does not satisfy this gate.
- [x] Source-only migration uses Unity IAP 5.4.1; focused sandbox evidence resolved Android Play Billing Library 9.0.0 and passed compilation. This is not shipping-artifact verification.
- [ ] The shipping Android artifact is inspected and shown to package a Play Billing Library version accepted on its actual submission date; submissions on or after 2026-08-31 require Billing Library 8 or newer.
- [ ] A server-owned validation and entitlement implementation replaces the current `PlayerPrefs` authority boundary; client receipt validation performs a deep product-ID check if retained as defense-in-depth.
- [ ] Sandbox / test-track transactions pass on physical devices, including cancellation, Android pending/delayed-payment approval and decline, and restoration.
- [ ] Landscape and safe-area checks pass on the named device matrix.
- [ ] UI strings, support path, privacy disclosure, and refund / restoration support copy have release-owner approval.
- [ ] Focused Unity regression tests pass for safe-area normalization, storefront state transitions, the exact catalog definition, one-time entitlement semantics, prologue replay, and non-gameplay reward isolation.

## 7. Regression command

Close any Unity Editor holding the target project, then run this focused EditMode fixture with the project’s pinned 2022.3.62f2 editor:

```sh
"/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath "$PWD" -runTests -testPlatform EditMode -testFilter '^CastleBusters\.Tests\.MobileNarrativeCommerceTests\.' -testResults /tmp/castle-busters-mobile-editmode.xml
```

Do **not** add `-quit`: Unity Test Framework schedules the test run on an editor update and exits itself after writing results. The focused Unity 2022.3.62f2 sandbox run recorded `total="5"`, `passed="5"`, and `failed="0"` in `/tmp/unknown-castle-iap5-fi2tu7fy/iap-v5-mobile-narrative-results.xml`; `total="0"` is a vacuous filter miss, not a pass. The fixture is `Assets/Tests/EditMode/MobileNarrativeCommerceTests.cs`. Its five tests cover safe-area normalization, Unicode-safe typewriter behavior, unavailable-in-editor storefront state, idempotent local-entitlement helper reset/grant semantics, and the fixed catalog definition through `CreateChronicleProductDefinition_UsesPublishedIdsAndNonConsumableType`.

## 8. Primary source references

- Unity IAP v5 StoreController migration and lifecycle: <https://docs.unity.com/en-us/iap/upgrade-to-iap-v5>
- Unity IAP v5 purchase retrieval and fulfillment: <https://docs.unity.com/en-us/iap/purchases>
- Unity IAP 5.4 package changelog: <https://docs.unity3d.com/Packages/com.unity.purchasing@5.4/changelog/CHANGELOG.html>
- Unity 2022.3 `Screen.safeArea` API: <https://docs.unity3d.com/2022.3/Documentation/ScriptReference/Screen-safeArea.html>
- Google Play Billing testing: <https://developer.android.com/google/play/billing/test>
- Google Play Billing Library deprecation: <https://developer.android.com/google/play/billing/deprecation-faq>
- Apple Sandbox testing: <https://developer.apple.com/documentation/storekit/testing-in-app-purchases-with-sandbox>
- Apple non-consumable product setup: <https://developer.apple.com/help/app-store-connect/manage-in-app-purchases/create-consumable-or-non-consumable-in-app-purchases/>

Source documentation changes over time. Confirm that the target Unity IAP package, the Play Billing submission cutoff, and store-console pages remain current during the actual release train.
