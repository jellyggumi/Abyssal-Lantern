# castle-war — deployment economy (전투 중 생성 / 대포)

Durable record of the 2026-08-09 roster overhaul that removed the Bomber
(폭탄병) and introduced the Cannon (대포) as a **placed installation**, plus the
Supply resource that lets both sides create units **during battle**.

Source of truth while the cycle is live:
`_workspace/current/design/deployment-economy.md`. Numbers also mirrored into
`_workspace/current/design/balance-sheet.md`.

## The defect this answered

Playtest read: **"병사만 나오고 있다"** — three roster cards, one verb. Knight,
Archer, and Bomber all resolved through `LaunchManager.SpawnAndLaunchOne`, so
the field only ever gained flying soldiers, nothing was ever *built*, and the
opponent's turn was dead air for the player.

## The change

Two creation verbs now coexist:

| Verb | Cost | When | Cards |
|---|---|---|---|
| **Launch** (existing) | free, 1/own turn | own turn only | Knight, Archer, Barrel |
| **Deploy** (new) | Supply | **both turns** | Knight, Archer, Barrel, **Cannon** |

The **Cannon is deploy-only** — placed, never launched. Launching it would have
made it a fourth projectile, which is the exact defect being fixed.

## Supply (보급)

Real-time accrual on both turns for both sides: `max 24`, `start 8`,
`0.7/s`, `+2` per kill, `+0.5` per enemy block destroyed. A 15 s turn yields
10.5 — about two Knights, or one Cannon every ~1.7 turns. Kill/block bonuses
make the *launch* fund the *deploy*: the volley that opens a wall pays for the
soldier who walks through it.

## 생성조건 (per-card creation conditions)

| Card | Cost | Cooldown | Unlock | Cap group (cap) |
|---|---|---|---|---|
| 기사 Knight | 5 | 2.5 s | turn 0 | Body (6) |
| 궁수 Archer | 6 | 3.5 s | turn 1 | Body (6) |
| 대포 Cannon | 12 | 12 s | turn 3 | Battery (2) |
| 화약통 Barrel | 4 | 5 s | turn 2 | Hazard (3) |

Knight and Archer **share** the Body cap, so deploy thickens a line rather than
flooding the map. Five conditions gate a deploy, evaluated
**most-permanent-first** — `Locked → FieldCap → Cooldown → Supply → Zone` — so
the HUD names the blocker the player must actually solve, not the first one
tripped. That ordering is the single most load-bearing rule in the system and
is pinned by a dedicated precedence test.

## Cannon contract

140 HP, range 13, reload 3.2 s, 42 damage, 1.5 splash, ballistic shell (arcs
over its own wall). 13.1 sustained dps; 26.2 at the 2-battery cap, which erases
a 150 HP core + 50 shield in ≈7.6 s. Deliberately inside one turn — leaving two
batteries unanswered should lose the game; the counterplay is the volley, since
one direct hit removes a 140 HP battery.

## Implementation map

| Concern | File |
|---|---|
| Pure rules (cost/cooldown/unlock/cap/zone/precedence, supply curve, cannon stats + ballistic solve) | `Assets/Scripts/DeploymentRules.cs` |
| Supply ownership, cooldown clocks, click-to-place, AI mirror, HUD | `Assets/Scripts/DeploymentController.cs` |
| Installation aiming/firing + shell | `Assets/Scripts/CannonController.cs` |
| Stationary-installation mode, `DeployGrounded` entry | `Assets/Scripts/UnitController.cs` |
| Deploy/aim mutual exclusion (`CancelAim`) | `Assets/Scripts/LaunchManager.cs` |

Removed: `VolleyRules` (bomber volley multiplicity), `UnitCombos.BomberFuseSeconds`
(renamed `BarrelFuseSeconds`), `UnitType.Bomber` (now `Barrel` + `Cannon`),
`GameManager.bomberPrefab`, and `Assets/Prefabs/Bomber.prefab` (pre-delete tag
`pre-bomber-removal-20260809`). The `Bomber` sprite folder is retained: the
prologue webtoon still uses it as art, but no roster unit does.

## Verification [OBSERVED 2026-08-09]

- **246/246 EditMode tests pass** (`editmode-results.xml`), up from 198 — the
  48 new pins live in `Assets/Editor/DeploymentEconomyTests.cs`.
- Those 48 were mutation-tested: 24 surgical implementation mutations, 24
  killed, 0 survivors. Three separate reorderings of `Evaluate` each turn the
  precedence tests red, so precedence is provably distinguished from
  first-failure ordering.
- Zero compile errors across the roster removal.

## Open [TARGET]

The 50/50 Knight/Archer/Bomber win-rate in
[[castle-busters-phase-2-completion]] **no longer describes this roster**. It
is historical only and cannot back a win-rate claim about Knight/Archer/Cannon
until the sim is re-run.
