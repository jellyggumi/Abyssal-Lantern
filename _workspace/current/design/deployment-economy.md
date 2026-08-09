# Deployment economy — castle-war (전투 중 생성 / 대포 설치)

- run-id: 20260809-castle-war-stage1
- owner: game-designer lane
- status: designed + implemented 2026-08-09; EditMode-pinned
- supersedes: the Bomber (폭탄병) roster slot and `VolleyRules.BomberVolleyCount`

## 1. Problem this solves

Playtest read: **"병사만 나오고 있다"** — every roster card resolved to the same
verb (drag the sling, launch a body). The board only ever gained soldiers, so
the field had no standing structure to fight over, and the only pacing beat was
"wait for your turn". Three defects behind that:

| Defect | Cause |
|---|---|
| Roster reads as one unit with three skins | Knight/Archer/Bomber all resolve through `LaunchManager.SpawnAndLaunchOne` |
| Nothing to do on the enemy's turn | Only brick designation (`BrickPlacementController`) was available |
| No cost curve | Creation was free and gated solely by "one launch per turn" |

## 2. Design decision

Add a **second creation verb** beside the launch, and give every roster entry
an explicit **생성조건 (creation condition)** it must satisfy.

- **Launch (기존)** — one drag-aimed volley per own turn. Free. Soldiers and the
  powder keg only. Unchanged: the precision contract in `balance-sheet.md` and
  the 0.02 s preview integration stay exactly as frozen.
- **Deploy (신규)** — place a unit directly on your own half by spending
  **Supply (보급)**. Available **during both turns**, so the enemy's turn stops
  being dead air. This is the "전투 도중에 생성" ask.

**대포 (Cannon) is deploy-only.** It is never launched — launching it would make
it a fourth projectile, which is the exact defect being fixed. It lands as a
**stationary installation** that auto-fires on a reload clock until destroyed.

The Bomber is **removed** from the roster. Its niche (splash damage at range) is
now the Cannon's, delivered from a fixed position over time instead of a
one-shot arc. The powder keg (화약통) stays: it is a field hazard, not a soldier.

## 3. Supply (보급) — the shared resource

Supply accrues in **real time during battle**, on both turns, for both sides.

```yaml
supply:
  max: 24.0
  start: 8.0
  regen_per_second: 0.7
  kill_bonus: 2.0            # your unit kills an enemy unit
  block_bonus: 0.5           # your side destroys an enemy block
```

A 15 s turn yields **10.5 supply** from regen alone — about two Knights, or one
Cannon every ~1.7 turns. The cap of 24 stops a passive player from banking a
five-unit alpha strike; the floor of 8 lets turn 1 open with an immediate
deploy so the mechanic teaches itself on the first beat.

Kill and block bonuses make the *launch* feed the *deploy*: a volley that
collapses a wall pays for the soldier that walks into the gap. That coupling is
the reason both verbs stay in the game instead of one replacing the other.

## 4. Roster and 생성조건

| Card | Kind | Verb | Cost | Cooldown | Unlock | Cap group (cap) |
|---|---|---|---|---|---|---|
| 1 기사 Knight | soldier | launch + deploy | 5 | 2.5 s | turn 0 | Body (6) |
| 2 궁수 Archer | soldier | launch + deploy | 6 | 3.5 s | turn 1 | Body (6) |
| 3 대포 Cannon | **installation** | **deploy only** | 12 | 12 s | turn 3 | Battery (2) |
| 4 화약통 Barrel | hazard | launch + deploy | 4 | 5 s | turn 2 | Hazard (3) |

Five conditions gate every deploy, checked **most-permanent-first** so the
rejection message names the blocker the player must actually solve:

1. **Unlock** — `turnCount >= unlockTurn`. Staggers the teaching order:
   Knight (melee) → Archer (range) → Barrel (hazard) → Cannon (structure).
2. **Field cap** — per cap group, per side. Knight and Archer **share** a body
   cap of 6, so the deploy verb thickens a line, it never floods the map.
3. **Cooldown** — per card, per side. Independent timers, so spending on a
   Cannon never locks the Knight card.
4. **Supply** — cost must be affordable.
5. **Zone** — the click must land in a legal band (§5).

`DeploymentRules.Evaluate(...)` returns a `DeployBlockReason` (`Locked`,
`FieldCap`, `Cooldown`, `Supply`, `Zone`, `None`) so the HUD prints the reason
instead of a silent no-op. UX rule from CLAUDE.md §2 holds: the card itself is
dimmed and shows the live cost/cooldown, so no tooltip is needed to read it.

## 5. Deploy zone (설치 가능 구역)

```yaml
deploy_zone:
  min_abs_x: 0.5     # never on the center line
  max_abs_x: 12.5    # inside the keeps' band
  min_y: 0.0
  max_y: 8.0
```

Plus two exclusions reused from existing frozen rules:

- **Own half only** — `TargetingRules.OnEnemyHalf` mirrored: player deploys at
  `x < -0.5`, enemy at `x > 0.5`. You reinforce your side; you do not teleport a
  knight onto the enemy's doorstep.
- **Launch-ring exclusion** — `LaunchRingRules.IsInsideRing` rejects the muzzle.
  Same reason bricks are excluded: a body in the ring blocks every volley of
  that side. This makes the effective player band ≈ `x ∈ [-11, -0.5]`.
- **No enemy overlap** — a deploy may not materialize on top of a live enemy
  body (mirrors `BrickPlacementRules.CanPlace`).

## 6. Cannon (대포) — the installation

```yaml
cannon:
  max_hp: 140.0
  range: 13.0
  reload_seconds: 3.2
  shell_damage: 42.0
  shell_splash_radius: 1.5
  muzzle_height: 0.55
  arc_apex_bonus: 2.5     # ballistic solve clears its own wall
```

- **Stationary.** Kinematic body, no walk logic, no target chasing. It holds the
  ground it was placed on.
- **Auto-fires** at the nearest valid enemy inside `range`, priority
  units → blocks → core, on a `reload_seconds` clock.
- **Ballistic shell**, not a hitscan: the shot arcs over the caster's own wall,
  which is what makes placement behind your line a real decision rather than a
  formality.
- **Destructible** at 140 HP — it is a target the opponent's volley can answer,
  so 2 batteries cannot become an un-counterable turret wall.

DPS check: `42 / 3.2 = 13.1` sustained, ×2 at cap = **26.2 dps** against a 150 HP
core + 50 shield. Two uncontested cannons need ≈7.6 s of clear line to erase a
full core pool. That is inside one turn, which is deliberate: leaving two
batteries alive and unanswered should lose the game. The counterplay is the
volley — one direct hit removes a 140 HP battery.

## 7. Balance intent

- **The launch stays the primary damage verb.** Deploy adds bodies and
  structure; it does not out-damage a well-aimed volley. A Knight costs 5 supply
  (≈7 s of regen) and deals its damage over the walk-in.
- **Supply is the pacing floor, the cap is the ceiling.** Regen sets minimum
  action density (something is always ~7 s away); the 24 cap forbids hoarding.
- **The enemy turn is now playable.** Deploy + brick designation both run during
  `GameState.AITurn`, so the loop's ≥3-actions requirement (`core-loop.md`) is
  satisfied on *both* halves of the turn cycle, not just the player's.
- **Symmetry.** The AI accrues supply on the same curve and deploys against the
  same conditions and caps. No hidden multiplier; difficulty comes from the
  existing `CurrentAiErrorOffset` ramp, not from cheating the economy.

## 8. What is frozen and must not drift

- Launch precision contract (`balance-sheet.md` §launch) — untouched.
- Knight/Archer combo beats (`UnitCombos`) — untouched.
- `LaunchRingRules`, `BrickPlacementRules`, `TargetingRules` — reused, not
  edited; the deploy zone composes them.
- Removed with the Bomber: `VolleyRules.BomberVolleyCount`,
  `VolleyRules.OwnTurnOrdinal`, `UnitCombos.BomberFuseSeconds`, and
  `LaunchManager`'s volley-multiplicity path. The 50/50 win-rate sim in
  `wiki/reports/castle-busters-phase-2-completion.md` covered a
  Knight/Archer/Bomber roster and **no longer describes this roster** — it must
  be re-run before any win-rate claim is made about the new one. [TARGET]

## 9. Verification targets

1. Cost/cooldown/unlock/cap table matches `DeploymentRules` exactly.
2. `Evaluate` returns the most-permanent blocker when several conditions fail.
3. Knight and Archer share one body cap; a full body cap does not block a Cannon.
4. Deploy zone rejects: center line, enemy half, above/below band, launch ring.
5. Supply clamps at 0 and 24; regen, kill bonus, and block bonus are additive.
6. Cannon reload and shell damage match this sheet.
7. `UnitType` no longer contains `Bomber`; no script references it.
