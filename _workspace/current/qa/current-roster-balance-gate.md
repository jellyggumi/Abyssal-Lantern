# Current-roster deterministic balance gate

**Verdict: PASS (focused roster gate).** Unity executed the exact five-test fixture: **5 passed, 0 failed, 0 skipped**. This verdict covers the deterministic Knight/Archer/Cannon/Barrel matrices below. It does **not** claim a 45–55% match win rate because this fixture does not model symmetric match-level AI decisions.

## Execution evidence

```sh
"/Applications/Unity/Hub/Editor/2022.3.62f2/Unity.app/Contents/MacOS/Unity" \
  -batchmode -nographics \
  -projectPath . \
  -runTests -testPlatform EditMode \
  -testFilter CastleBusters.Tests.CurrentRosterBalanceGateTests \
  -testResults ./_workspace/current/qa/evidence/current-roster-balance-gate.xml \
  -logFile ./_workspace/current/qa/evidence/current-roster-balance-gate.log
```

- Fixture: `Assets/Tests/EditMode/CurrentRosterBalanceGateTests.cs`
- XML: `_workspace/current/qa/evidence/current-roster-balance-gate.xml`
- Log: `_workspace/current/qa/evidence/current-roster-balance-gate.log`
- Unity: `2022.3.62f2`
- Final process exit: `0`
- XML result: `total="5" passed="5" failed="0" skipped="0"`, start/end `2026-08-09 09:08:42Z`
- Log caveat: after the XML was written, the installed Unity-MCP package logged that no pending MCP request ID existed. The run was launched directly by CLI, so this collector message is out-of-band; process exit `0` and the complete NUnit XML establish the test result.
- Simulation tick: `0.01 s`; no wall-clock waits; no unseeded RNG
- Runtime faction sample: 8 seeded positions per side; each exercises the shipped `DeploymentRules.Evaluate` player/AI ownership branch for own-half acceptance and opposing-half rejection
- Seeds: `104729, 130363, 155921, 196613, 262147, 327673, 393241, 524287`

## Shipped inputs exercised

The fixture reads Knight and Archer directly from their shipped prefab `UnitController` values (including Archer damage `20` and cadence `0.95 s`) without manually invoking `UnitController.Awake`. It also uses the shipped `UnitCombos`, `DeploymentRules`, `CannonRules`, `LastStand`, Barrel prefab `ExplosiveGimmick`, and Archer projectile values. The core has no prefab: `GameManager.SpawnCastleCores` builds `CastleCoreGimmick` at runtime, so the fixture materializes that same runtime component, reads its initialized `150 HP`, crosses its shield trigger, and derives the one-time `50 HP` shield from observed absorption. No production value is copied into a test-only defense constant. Cannon ballistic travel uses `CannonRules.SolveShellVelocity` with current `Physics2D.gravity`.

| Role | HP | Sustained DPS | Supply | DPS / supply | HP / supply | Distinct condition |
|---|---:|---:|---:|---:|---:|---|
| Knight | 100 | 20.000 | 5 | 4.000 | 20.000 | Close body pressure with 3rd/6th combo hits |
| Archer | 80 | 25.263 | 6 | 4.211 | 13.333 | Open-field range with runtime 0.95 s cadence and 5th/10th volleys |
| Cannon | 140 | 13.125 single-target | 12 | 1.094 single; 4.375 at four splash targets | 11.667 | Deploy-only siege/splash installation |
| Barrel | hazard | 80 burst | 4 | not compared as sustained body DPS | n/a | 2.2 radius, 2.0 s fuse support/hazard |

## Threshold results

| Contract | Measurement | Threshold | Result |
|---|---:|---:|---|
| Comparable mobile-body damage efficiency | Knight vs Archer DPS/supply delta `5.00%` | `≤15%` | PASS |
| Cannon bounded useful condition | Four-target splash DPS/supply delta from mobile-body mean `6.17%` | `≤15%` | PASS |
| Cannon countercondition | Single-target DPS/supply `1.094`, below half the body mean (`2.053`) | `<50%` of body mean | PASS |
| Frontline survivability identity | HP/supply `Knight 20.000 > Archer 13.333 > Cannon 11.667` | strict ordering | PASS |
| Close Knight counter | Knight defeats Archer from `2.5` units in `3.640 s` | Knight win | PASS |
| Open Archer counter | Archer defeats Knight from `10.0` units in `4.690 s` | Archer win | PASS |
| Capped Cannon siege viability | One Cannon TTK `17.502 s`; two-Cannon cap TTK `11.102 s` against runtime-resolved `150 HP core + 50 HP shield = 200` defense | capped TTK `≤12.0 s` deploy cooldown; one slower than cap | PASS |
| Barrel cluster payoff | `160` damage into two `80 HP` Archers | `≥160` | PASS |
| Barrel radius counter | Damage at `radius + 0.01` is `0`; Archer triggers from `3.2` units in `0.320 s` | outside damage `0`; travel `<2.0 s` fuse | PASS |
| Runtime faction/side branch | Per side: `8/8` own-half deployments accepted and `8/8` opposing-half checks rejected; acceptance delta `0.000%` | `≤15%` | PASS |
| Comeback/reversal bound | Runtime-resolved defense: core `150`, shield `50`, total `200`; reachable launched roles: Knight `12.0%`, Archer `12.0%`, Barrel `30.0%`; conservative non-reachable Cannon formula envelope `25.2%` | each `≤30%` of shipped core + shield pool | PASS (Barrel exactly at ceiling) |

The Cannon row is **not a current runtime Last Stand outcome**. `GameManager.ApplyLastStandOnLaunch` is the only consumption path, while `DeploymentController.SpawnCannon` deploys a grounded stationary installation and never launches it. Its `25.2%` row is retained only as a conservative `LastStand.BuffedDamage` formula envelope; the production-reachable comeback ceiling is Barrel at `30.0%`.

## Role and dominance disposition

Three independent strategies win in their intended scripted conditions: Knight closes the near body fight, Archer wins the open body fight, and the bounded two-Cannon battery cap breaches the runtime-resolved 200-point core-defense pool within one 12-second Cannon deployment window. Barrel is evaluated separately as a hazard/support role: a two-Archer cluster is lethal, while spacing beyond 2.2 and a remote Archer trigger are explicit counterconditions.

No universal role dominates the modeled matrix: Knight/Archer ownership reverses with spacing; Cannon is competitive only when splash reaches four targets and pays more than a 50% single-target efficiency penalty; Barrel deals zero damage immediately outside its radius. This is a focused role/counter gate, not proof that every possible pair composition on every stage is balanced.

## Match-rate boundary

`45–55% win rate: NOT EVALUATED / NOT CLAIMED.` The seeded cases exercise the shipped `DeploymentRules.Evaluate` ownership branch for mirrored player/AI placement legality; they do not simulate combat-side AI behavior, card choice, targeting, economy, castles, turns, or match victory. Reporting these runtime faction checks as match win rate would be false evidence.

## Scope integrity

- Production code: unchanged.
- Added/updated test and evidence only.
- No broad Unity suite, build, visual QA, browser/WAI Play, or PlayMode physics run was performed.
- The final XML is the authority for the exact executed tests and contains each measured matrix row in its NUnit `<output>` nodes.
