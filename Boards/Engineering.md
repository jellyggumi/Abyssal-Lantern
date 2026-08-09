---

kanban-plugin: board

---

## 📥 Backlog


## 🏃 Sprint


## 🔨 In Progress


## ✅ Done

- [x] ~~🟢 **2D Resource Generation**~~ ✅ 2026-06-28
	Generated StoneBlock, Cannonball, and Background 2D pixel art assets using god-tibo-imagen. [[Projects/CastleBusters]]

- [x] ~~🔴 **ScriptableObjects Integration**~~ ✅ 2026-06-28
	Created UnitData and BlockData ScriptableObjects and integrated them into UnitController and DestructibleBlock. [[Projects/CastleBusters]]
- [x] ~~🔴 **BFS Structural Integrity Optimization**~~ ✅ 2026-06-28
	Implemented batching and slicing in CastleController to prevent CPU spikes. [[Projects/CastleBusters]]
- [x] ~~🟡 **AI Trajectory Calibration**~~ ✅ 2026-06-28
	Added error offset range to SimpleAI aiming. [[Projects/CastleBusters]]
- [x] ~~🟡 **Juice & Polish (Hit-Stop)**~~ ✅ 2026-06-28
	Implemented HitStopManager to freeze time during major collapses and explosions. [[Projects/CastleBusters]]
- [x] ~~🔴 **AOS Overhaul (capture zones, unit combat traits, event scheduling, Chariot)**~~ ✅ 2026-07-03
	Added `CaptureZoneController` alternate win condition, Knight/Archer/Bomber combat trait passes, event-driven vent/buff/debuff/gate scheduling, and 3-phase Chariot siege machine. No new UnitType added — guardrail-checked. [[Projects/CastleBusters]]

- [x] ~~🟡 **Content & Playtest QA pass**~~ ✅ 2026-07-04
	Flying war-beast enemy variant, hero loot growth curve, siege alarm feed, GimmickButton wiring, particle polish, buff/debuff clarity, damage-number weight tuning. [[Projects/CastleBusters]]

- [x] ~~🔴 **Gimmick fairness fixes (code review cycle 3)**~~ ✅ 2026-07-04
	Fixed 2 P1 regressions caught by CodeReview/DesignCritic: keg-vs-launch-muzzle clearance (`LaunchApronAbsX`/`InitialBarrelPositions` widened to ≥3.5u) and `GimmickFrameAnimator` world-footprint mismatch on sprite swap. Plus Knight/Archer trait tunables, architect-reviewed. `.specify/cycles.md` backfilled from 1→6 rows to reflect this and the AOS overhaul work; live Unity MCP re-verification still pending (editor lock + empty MCP tool list this session). [[Projects/CastleBusters]]



%% kanban:settings
```
{"kanban-plugin":"board","list-collapse":[false,false,false,false]}
```
%%
