# Castle Busters Implementation Log - 2026-06-28

- **Source Reference**: [[wiki/sources/castle-busters-project]]
- **Date**: 2026-06-28
- **Tags**: #dev-log #castle-busters

---

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
- The trajectory calculation formula is calibrated to hit targets within specified error margins.

### 4. Juice & Polish (Hit-Stop & Screen Shake)
- Implemented `HitStopManager.cs` to temporarily freeze time (`Time.timeScale = 0f`) for 0.05 seconds during major collapses and Bomber explosions.
- Implemented `ScreenShakeManager.cs` to shake the main camera's local position during explosions and block destruction, without external Cinemachine dependencies.
- Integrated Hit-Stop and Screen Shake triggers into `UnitController.Explode()` and `DestructibleBlock.DestroyBlock()` / `MakeFall()`.

### 5. Verification & Testing
- Added new automated tests to `Assets/Editor/GamePlayTests.cs`:
  - `UnitController_UsesUnitData_WhenAssigned`
  - `DestructibleBlock_UsesBlockData_WhenAssigned`
  - `HitStopManager_Singleton_IsInitialized`
  - `ScreenShakeManager_Singleton_IsInitialized`
- Verified that the entire project compiles cleanly with **0 errors and 0 warnings**.

### 6. Gameplay & Playability Improvements
- **Fixed Unit Freezing Bug**: Moved Rigidbody static initialization from `Start()` to `Awake()` in `UnitController.cs`. This prevents the Rigidbody from being set back to `Static` after it is launched, which was freezing the units in mid-air.
- **Fixed Launch Selection Bug**: Initialized the selected unit prefab in `GameManager.StartGame()` by calling `lm.SetSelectedUnit(selectedUnitPrefab)`. Previously, the selected unit was `null` in `LaunchManager`, preventing any launch input.
- **Added Keyboard Selection**: Added keyboard inputs (1, 2, 3) in `GameManager.Update()` to allow the player to select Knight, Archer, or Bomber.
- **Dynamic Ground Creation**: Added a `CreateGround()` method in `GameManager.cs` that dynamically creates a ground collider and a visual line at `y: 0` so that units and blocks don't fall forever into the void.
- **Wait for Turn to Settle**: Changed `OnUnitLaunched` to start the `WaitAndEndTurn` coroutine instead of calling `EndTurn()` immediately. This coroutine waits for the launched unit to land/explode and for any falling blocks to settle before ending the turn.
- **Fixed AI Trajectory Calculation**: Fixed the trajectory calculation in `SimpleAI.cs` to support leftward launches by using absolute horizontal displacement (`Mathf.Abs(x)`).
- **Added Verification Tests**: Added `UnitController_Awake_InitializesRigidbodyToStatic` and `Prefabs_HaveRequiredComponents` to `Assets/Editor/GamePlayTests.cs`.