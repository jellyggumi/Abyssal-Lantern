# Core loop — castle-war

- run-id: 20260809-castle-war-stage1
- owner: game-designer lane
- status: G7 draft — numeric model below; playtest repeat-rate pending QA [TARGET]

## Loop 1 (mandatory): Volley → Collapse → Reward

The carried build already implements this loop (README "Current Playable
Slice"); the pivot re-frames it as a faction exchange.

| G7 requirement | Model | Evidence |
|---|---|---|
| Period 30–180s | One turn cycle ≈ 12–25s (aim 4–10s, flight/impact 3–6s, enemy response 5–9s); loop = 2–4 turn cycles ≈ 45–100s | [OBSERVED] AutoPlayTest capture timings; re-measure in Stage 2 |
| ≥3 actions/loop | select unit (1/2/3) → read wind → drag-aim → release → watch collapse ≥ 4 distinct player actions | [OBSERVED] input map |
| ≥1 reward event/loop | Block collapse chain (BFS) + damage callouts + core HP delta every successful volley | [OBSERVED] DestructibleBlock/BFS system |
| Repeat-rate ≥70% | pending QA playtest proxy | [TARGET] Stage 2 |

## Faction-war reframe (Stage 1 pivot work)

The single-launch duel becomes a **war between sides**, not a lone artillerist:

1. **Squad presence**: launched units that survive impact fight on the ground
   (existing unit AI) — the field accumulates a blue line vs red line.
2. **Continuous pressure read**: a war-bar HUD (front-line position derived
   from surviving units + castle HP ratio) makes "who is winning the war"
   visible at a glance — the UX answer to the reference's readability.
3. **Skill cadence** (reference-adopted): unlockable volley modifiers
   (double-shot, ground-spawn, hero call analogues) on a per-N-turns cadence
   feed the reward spine between collapses.

## Loop 2 (meta, answers the reference's weakness): Campaign spine

Stage picker (3 battlefields, sequential unlock) already exists; extend to a
valley map with per-stage star goals so the economy has a sink. [TARGET,
Stage 2 scope decision]

## Numbers carried from the balanced build

Knight/Archer/Bomber at 50/50 win-rate, usage 31.9/36.2/31.9% (wiki/reports/
castle-busters-phase-2-completion.md). The faction reframe must not touch
unit stats without re-running the Phase-2 sim — presentation-only changes
first (CLAUDE.md §2 sim/presentation boundary).
