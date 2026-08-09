---
date: 2026-06-28
tags:
  - dev-log
  - castle-busters
---

# 🛠️ Castle Busters Implementation Log - 2026-06-28

Implemented the core design requirements for the Castle Busters project, including ScriptableObjects, structural integrity optimizations, AI calibration, and juice/polish.

## 📝 Work Done

### 1. ScriptableObjects Integration
- Created `UnitData.cs` ScriptableObject to decouple unit statistics (maxHP, moveSpeed, attackDamage, attackRange, attackCooldown, explosionRadius, explosionDamage) from runtime logic.
- Created `BlockData.cs` ScriptableObject to configure block statistics (maxHP, mass, friction, bounciness, sprites, and destruction effects).
- Refactored `UnitController.cs` and `DestructibleBlock.cs` to initialize their properties dynamically from these ScriptableObjects if assigned, while maintaining backward compatibility for existing prefabs and unit tests.

### 2. BFS Structural Integrity Optimization
- Refactored `CastleController.cs` to implement batching and slicing for the BFS structural integrity check.
- Added end-of-frame batching using `WaitForEndOfFrame` to prevent multiple BFS runs in a single frame.
- Added coroutine-based slicing (max 50 traversals per frame) to prevent CPU spikes during major collapses.
- Maintained synchronous execution fallback for unit tests and edit-mode operations.

### 3. AI Trajectory Calibration
- Updated `SimpleAI.cs` to inject a random target offset based on the `errorOffsetRange` property, simulating human-like aiming error.

### 4. Juice & Polish (Hit-Stop)
- Implemented `HitStopManager.cs` to temporarily freeze time (`Time.timeScale = 0f`) for 0.05 seconds during major collapses and Bomber explosions.
- Integrated Hit-Stop triggers into `UnitController.Explode()` and `DestructibleBlock.MakeFall()`.

### 5. Verification & Testing
- Added new automated tests to `Assets/Editor/GamePlayTests.cs`:
  - `UnitController_UsesUnitData_WhenAssigned`
  - `DestructibleBlock_UsesBlockData_WhenAssigned`
- Verified that the entire project compiles cleanly with **0 errors and 0 warnings**.
