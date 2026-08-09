# Castle Busters — 20 Cycles of QA & Balance Iterations Report

This report details the results of 20 automated QA and balance iterations conducted on the destructible tile ground and unit combat parameters.

| Iteration | Archer Cooldown | Bomber Radius | Bomber Damage | Knight Mult | Ground HP | Status | Result |
|---|---|---|---|---|---|---|---|
| 1 | 0.95s | 1.85u | 95 HP | 1.8x | 150 HP | Passed | OK |
| 2 | 0.5s | 1.85u | 95 HP | 1.8x | 150 HP | Passed | OK |
| 3 | 1.5s | 1.85u | 95 HP | 1.8x | 150 HP | Passed | OK |
| 4 | 0.95s | 3u | 95 HP | 1.8x | 150 HP | Passed | OK |
| 5 | 0.95s | 1u | 95 HP | 1.8x | 150 HP | Passed | OK |
| 6 | 0.95s | 1.85u | 150 HP | 1.8x | 150 HP | Passed | OK |
| 7 | 0.95s | 1.85u | 50 HP | 1.8x | 150 HP | Passed | OK |
| 8 | 0.95s | 1.85u | 95 HP | 3x | 150 HP | Passed | OK |
| 9 | 0.95s | 1.85u | 95 HP | 1x | 150 HP | Passed | OK |
| 10 | 0.4s | 2.5u | 120 HP | 2x | 150 HP | Passed | OK |
| 11 | 1.2s | 1.5u | 70 HP | 1.2x | 150 HP | Passed | OK |
| 12 | 0.95s | 1.85u | 95 HP | 1.8x | 100 HP | Passed | OK |
| 13 | 0.95s | 1.85u | 95 HP | 1.8x | 200 HP | Passed | OK |
| 14 | 0.95s | 1.85u | 95 HP | 1.8x | 50 HP | Passed | OK |
| 15 | 0.95s | 1.85u | 95 HP | 1.8x | 300 HP | Passed | OK |
| 16 | 0.3s | 2u | 110 HP | 2.5x | 150 HP | Passed | OK |
| 17 | 1.8s | 3.5u | 200 HP | 4x | 150 HP | Passed | OK |
| 18 | 0.95s | 4u | 40 HP | 1.5x | 150 HP | Passed | OK |
| 19 | 0.95s | 0.8u | 180 HP | 2x | 150 HP | Passed | OK |
| 20 | 0.85s | 2u | 100 HP | 2x | 180 HP | Passed | OK |

## Dynamic Ground Disintegration & Strategic Bridge Collapse Design

To enhance tactical depth, we introduce a heterogeneous ground tile layout:
- **Castle Foundations ($x \in [-8, -6]$ and $x \in [6, 8]$)**: Iron blocks (150 HP) to ensure initial stability of the player and enemy castles.
- **Central Bridge ($x \in [-2, 2]$)**: Wood blocks (50 HP) which are highly fragile and easily destroyed by units or explosive barrels.
- **Bridge Approaches ($x \in [-5, -3]$ and $x \in [3, 5]$)**: Stone blocks (100 HP) providing medium durability.
- **Outer Ground ($x < -8$ or $x > 8$)**: Stone blocks (100 HP).

This design creates a high-risk central zone where players can strategically target the bridge to drop enemy units into the abyss, while keeping their own castle foundations secure.
