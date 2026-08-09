# Castle Busters Vertical-Slice Quality Contract

- **Status:** frozen for this improvement pass
- **Product direction:** compact single-player physics-siege tactics vertical slice
- **Next milestone:** a 10–15 minute player-build demo where the first launch happens in under 60 seconds and a complete Stage 1 siege ends cleanly.

## Product boundary

Castle Busters is currently a turn-based player-versus-AI physics siege game: read wind, choose Knight/Archer/Bomber/Barrel, breach a destructible castle, and win by core destruction or capture. This pass preserves that loop.

It does **not** add real-time PvP, an eight-unit deck, ads, IAP, gacha, energy timers, or a live-service backend. The viable commercial hypothesis is free vertical-slice demo to a small premium game; collect local/offline session evidence before adding any analytics or commerce integration.

## Measurable acceptance criteria

1. Unity compiles the current test assemblies without the obsolete `CastleController.GetBlockCount` call.
2. A PlayMode boot → intro → first launch → turn-resolution test completes with no suppressed unexpected errors.
3. Shockwave VFX remains visible and self-cleans without constructing a `Material` from a missing shader.
4. Chariot destruction during a live turn still grants its reward and schedules its respawn; scene teardown grants neither reward nor respawn.
5. New-match initial units receive no HeroGrowth stacks from the preceding match.
6. Stage 1, Stage 2, and Stage 3 resolve distinct, player-build-loadable background art from `Resources`; runtime background scaling is derived from sprite dimensions.
7. Build Settings contains exactly one enabled `Assets/Scenes/SampleScene.unity` entry.
8. The existing unit animation contract remains intact: all Knight, Archer, and Bomber Idle/Walk/Attack/Launch frame resources resolve.
9. Twenty evidence-backed quality cycles are recorded in the QA ledger; each cycle has a check and a result, with failures fixed rather than suppressed.

10. A launch-quality toast and a concurrent block-break combo never render as overlapping central banners; combo accounting still advances and a later no-toast combo still renders.
11. Particle VFX reuse one cached `Material` per resolved texture and every current `GameFeelVfx.GetParticleMaterial` consumer assigns it through `sharedMaterial`.

## Evidence ledger — 2026-07-10

- **22 focused QA loops passed during this pass:** Runtime reliability 9/9, rendering/series 7/7, bugfix gameplay 5/5, and automated intro→launch→impact capture 1/1.
- **Runtime contracts exercised:** normal boot→launch→AI turn has no unexpected logs; VFX self-cleans; teardown cannot respawn gameplay objects; chariot reward is exactly-once; new-match HeroGrowth reset; stage art resolves per stage; guide outline is opaque/dark; particle materials cache; and toast/combo arbitration preserves combo state.
- **Presentation evidence:** automated capture exercised intro plus six gameplay frames. The new UI-state regression proves the central toast owns the lane while its active duration; a later no-toast combo returns normally.
- **Full-suite limitation:** the unfiltered PlayMode command exceeded its 900-second outer limit in legacy `Cycle3_PlaytestDataCollection_30Games`; it is not recorded as a passing gate. Its 900-second per-test timeout means it needs a dedicated long-run CI lane rather than the merge gate.
## Delivery order

1. Restore test compilation and remove error masking from touched integration tests.
2. Repair lifecycle/material defects that make the prior PlayMode pass untrustworthy.
3. Repair per-stage art loading and new-match progression state.
4. Remove duplicate build-scene configuration.
5. Run focused EditMode/PlayMode checks, inspect player-build compatible resource paths, and record the evidence.

## Product metrics for the next external playtest

- **North Star:** a qualified demo session — a player completes a siege and voluntarily chooses Continue or Rematch.
- **OMTM:** Stage 1 first-siege completion.
- **Instrument later, before monetization:** prologue skip, title start, first launch, first result, continue/rematch, stage, duration, and end reason; no PII.
- **Target hypotheses:** first launch under 60 seconds; >=80% of starts reach first launch; >=45% finish first siege; >=30% of finishers continue or rematch; >=99% sessions have no crash/blocking progression failure.

## Out-of-scope follow-ups

- Optional/shortened lore-first flow, then in-context first-shot coaching.
- Shipped Korean TMP fallback asset for non-macOS players.
- Audio/SFX identity and accessibility settings.
- Player-build performance capture for a 30-turn maximum-obstacle match.
- Store messaging alignment: do not claim real-time PvP or an eight-unit deck until implemented.
