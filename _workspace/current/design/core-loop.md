# Core loop — castle-war

- run-id: 20260809-castle-war-stage1
- owner: game-designer lane
- status: G7 implementation pass — precision/balance contract frozen; repeat-rate pending QA [TARGET]

## Loop 1 (mandatory): Volley → Collapse → Reward

The carried build already implements this loop (README "Current Playable
Slice"); the pivot re-frames it as a faction exchange.

| G7 requirement | Model | Evidence |
|---|---|---|
| Period 30–180s | One turn cycle ≈ 12–25s (aim 4–10s, flight/impact 3–6s, enemy response 5–9s); loop = 2–4 turn cycles ≈ 45–100s | [OBSERVED] AutoPlayTest capture timings; re-measure in Stage 2 |
| ≥3 actions/loop | select unit (1/2/3) → read wind → drag-aim → release → watch collapse ≥ 4 distinct player actions | [OBSERVED] input map |
| ≥1 reward event/loop | Block collapse chain (BFS) + damage callouts + core HP delta every successful volley | [OBSERVED] DestructibleBlock/BFS system |
| Repeat-rate >=70% | two-minute loop remains visually distinct and requires >=3 player decisions; automated proxy records two or more resolved turn cycles in one session | [TARGET] Stage 2. The 2026-08-09 focused runs prove only sub-cycle beats: 22.4 s natural player→AI handoff, plus 8.89 s and 5.39 s cannon/fuse contracts. No two-minute same-session capture exists, so G7 stays open. |

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

## Current roster balance contract

The shipped roster is Knight, Archer, Cannon, and Barrel. The obsolete
Knight/Archer/Bomber Phase-2 result is historical only and cannot support a
current win-rate claim. The current deterministic role gate instead proves
condition-dependent viability: Knight wins the close body fight, Archer wins
the open body fight, the two-Cannon battery cap breaches 200 defense inside
its 12 s deployment window, and Barrel has a lethal two-body cluster payoff
with an explicit spacing/ranged-trigger counter. Full measurements and limits
live in `../qa/current-roster-balance-gate.md`. Full-match 45–55% runtime
win rate remains **not evaluated** until a symmetric ≥20-match runtime sample
exists; that missing measurement is separate from the route-correctness coverage below.
- **Read → aim → release**: the preview runs for 6.0 s at the runtime physics step (300 × 0.02 s), includes gravity and in-radius wind, and uses the launched unit's runtime mass. A visible arc that disagrees with the shot is a control defect, not player error.
- **Impact hierarchy**: damage magnitude scales number size/color; impact, core-hit, and core-destroyed feedback have distinct visual weights. Launch, impact, and combo SFX are short mono cues with conservative per-shot volume so multi-block collapse chains do not become a clipping wall.
- **Turn readability**: the launch result remains visible through flight and impact. Turn-change toasts cannot overwrite an active launch/combo banner; combo state survives that suppression.
- **Fair pressure ramp**: wind, AI accuracy, and storm probability reach their final values over a derived `EffectiveDifficultyRampTurns` per stage, using an asymptotic Hill curve approach `n^p/(n^p+h^p)` (h=0.6×ramp, p=1.8) so difficulty climbs each turn and approaches a ceiling. Last Stand arms at 35% core HP and caps each buffed hit at 140. A separate 140-health pristine-turn budget stops barrel chains or cloned projectiles from ending a full-health match in one volley; the core remains fully defeatable after the turn advances.
- **First-volley tempo compensation**: damage ownership and the 0.5 scalar are captured when the turn-0 player action is committed or its deploy is created, then applied at the eventual damage event. Production PlayMode covers committed melee, arrow impact, cannon splash, launched-barrel fuse, and launched-unit → production field-keg handoff; delayed impacts and chained explosions preserve the capture after turn handoff (`TestResults/pr44-damage-hardened-v4.xml`, 12/12). Later and enemy-owned actions capture full damage. This closes route correctness only; G2 remains FAIL without a symmetric ≥20-match runtime win-rate sample.
- **Loot compounds and resets explicitly within series.** Hero Growth stacks (sword +15% damage, shield +20% HP, boots +12% speed per stack, cap 5) persist through Next Game reloads and compound across all games in one best-of-three series. Stacks reset on rematch, title return, stage selection, fresh runtime boot, or after the series reaches a decided winner before the next series initial spawn. This creates an explicit series identity and prevents infinite scaling across independent runs.
- **Tactical scale**: Stage 2 is a compressed, fast-mutating fortress duel; Stage 3 is a wide, higher-wind gorge with a slower mutation cadence. Exact values and probability boundaries live in `balance-sheet.md`.
