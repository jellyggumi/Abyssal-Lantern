# 🏰 Castle Busters - Technical Specification & Implementation Blueprint

This document serves as a comprehensive, implementation-ready technical specification and blueprint for the multi-agent development team (**jeo team**). It expands upon the core Game Design Document (GDD) by defining exact class interfaces, data models, implementation phases, mathematical formulas, and verification plans.

---

## 1. System Architecture & Component Interaction

The game follows a state-driven architecture where the `GameManager` coordinates turns, while individual controllers manage physics, combat, and structural integrity.

```
+-----------------------------------------------------------------+
|                          GameManager                            |
|  - GameState (Setup, PlayerTurn, AITurn, GameOver)              |
|  - Manages active units, turn timers, and victory conditions    |
+-------------------------------+---------------------------------+
                                |
       +------------------------+------------------------+
       |                                                 |
       v                                                 v
+------------------------------+                 +------------------------------+
|        LaunchManager         |                 |           SimpleAI           |
|  - Handles player input      |                 |  - Calculates trajectories   |
|  - Trajectory preview        |                 |  - Launches AI units         |
|  - Instantiates player units |                 |  - Human-like error injection|
+--------------+---------------+                 +--------------+--------------+
               |                                                 |
               +------------------------+------------------------+
                                        |
                                        v
                         +------------------------------+
                         |        UnitController        |
                         |  - UnitState machine         |
                         |  - Movement & Targeting      |
                         |  - Combat (Melee / Ranged)   |
                         +--------------+--------------+
                                        |
                                        v (Attacks / Collides)
                         +------------------------------+
                         |      DestructibleBlock       |
                         |  - HP & Visual states        |
                         |  - PhysicsMaterial2D settings|
                         |  - Triggers collapse         |
                         +--------------+--------------+
                                        |
                                        v (Notifies)
                         +------------------------------+
                         |       CastleController       |
                         |  - BFS Structural Integrity  |
                         |  - Batching & Optimization   |
                         +------------------------------+
```

---

## 2. Detailed Class Specifications & API

### 2.1 GameManager.cs
Manages the global game state, turn transitions, and win/loss conditions.
* **Enums**:
  ```csharp
  public enum GameState { Setup, PlayerTurn, AITurn, GameOver }
  ```
* **Properties**:
  * `public static GameManager Instance { get; private set; }`
  * `public GameState currentState { get; private set; }`
  * `public float turnDuration = 15f`
  * `public CastleController playerCastle`
  * `public CastleController enemyCastle`
  * `public bool IsPlayerTurn { get; }`
* **Methods**:
  * `public void StartGame()`: Initializes the game, sets state to `PlayerTurn`, and resets timers.
  * `public void SelectUnit(int unitTypeIndex)`: Sets the active unit prefab for launching.
  * `public void OnUnitLaunched(UnitController unit)`: Registers the launched unit and transitions the turn.
  * `public void OnUnitDied(UnitController unit)`: Removes the unit from active tracking and checks victory conditions.
  * `public void CheckVictoryConditions()`: Counts remaining blocks in both castles. If either is 0, triggers `EndGame`.
  * `private void EndTurn()`: Toggles the active turn and resets the timer.
  * `private IEnumerator ExecuteAITurn()`: Delays for 1.5 seconds, then calls `SimpleAI.TakeTurn()`.
  * `private void EndGame(string result)`: Sets state to `GameOver` and displays the game over panel.

### 2.2 LaunchManager.cs
Handles mouse and touch input for aiming, drawing the trajectory preview, and launching units.
* **Properties**:
  * `public Transform launchPoint`
  * `public float maxDragDistance = 3.0f`
  * `public float launchForceMultiplier = 5.0f`
  * `public float maxLaunchVelocity = 25f`
  * `public LineRenderer trajectoryLine`
  * `public int trajectoryResolution = 30`
  * `public float timeStep = 0.1f`
* **Methods**:
  * `public void SetSelectedUnit(GameObject unitPrefab)`: Sets the prefab to instantiate on launch.
  * `private void HandleInput()`: Detects mouse/touch drag, clamps the drag vector, and calculates launch velocity.
  * `private void DrawTrajectory(Vector2 velocity)`: Computes and sets positions for the `LineRenderer` using kinematic equations.
  * `private void LaunchUnit()`: Instantiates the unit prefab, applies velocity, and notifies `GameManager`.

### 2.3 UnitController.cs
Controls unit behavior, state transitions, movement, targeting, and combat.
* **Enums**:
  ```csharp
  public enum UnitType { Knight, Archer, Bomber }
  public enum UnitState { Idle, Launched, Grounded, Attacking, Dead }
  ```
* **Properties**:
  * `public UnitType unitType`
  * `public bool isPlayerUnit`
  * `public UnitData unitData` (ScriptableObject reference)
  * `public UnitState CurrentState { get; }`
* **Methods**:
  * `public void Launch(Vector2 velocity)`: Sets state to `Launched` and applies velocity to `Rigidbody2D`.
  * `public void TakeDamage(float damage)`: Reduces HP and triggers `Die()` if HP <= 0.
  * `private void FindTarget()`: Searches for the nearest active enemy unit or non-falling enemy castle block.
  * `private void MoveTowardsTarget()`: Moves horizontally towards the target. Transitions to `Attacking` when in range.
  * `private void TryAttack()`: Checks cooldown and triggers `MeleeAttack()` or `ShootArrow()`.
  * `private void MeleeAttack()`: Deals damage directly to the target.
  * `private void ShootArrow()`: Instantiates an arrow prefab and initializes it with damage and direction.
  * `private void Explode()`: (Bomber only) Deals AoE damage to all blocks and units within `explosionRadius` and destroys itself.
  * `private void Die()`: Sets state to `Dead`, disables colliders, and destroys the GameObject after a delay.

### 2.4 CastleController.cs
Manages the structural integrity of the castle using a Breadth-First Search (BFS) algorithm.
* **Properties**:
  * `public bool isPlayerCastle`
  * `public float blockSizeX = 1.0f`
  * `public float blockSizeY = 1.0f`
  * `public float adjacencyEpsilon = 0.1f`
* **Methods**:
  * `public void RefreshBlockList()`: Scans and caches all child `DestructibleBlock` components.
  * `public void OnBlockDestroyed(DestructibleBlock destroyedBlock)`: Removes the block from the cache and schedules a structural integrity check.
  * `public void CheckStructuralIntegrity()`: Performs BFS starting from ground anchors. Any block not connected to a ground anchor is marked as falling.
  * `private IEnumerable<DestructibleBlock> GetNeighbors(DestructibleBlock block)`: Finds adjacent blocks within the grid spacing defined by `blockSizeX/Y` and `adjacencyEpsilon`.

### 2.5 DestructibleBlock.cs
Represents an individual structural block of a castle.
* **Properties**:
  * `public BlockData blockData` (ScriptableObject reference)
  * `public bool isGroundAnchor`
  * `public bool IsFalling { get; }`
* **Methods**:
  * `public void TakeDamage(float damage)`: Reduces HP, updates sprites based on damage thresholds, and triggers destruction if HP <= 0.
  * `public void MakeFall()`: Transitions the block's `Rigidbody2D` to `Dynamic` and applies a random torque.
  * `private void UpdateVisuals()`: Swaps sprites to show cracks based on HP ratio (100-70%: Normal, 70-30%: Cracked, <30%: Heavily Cracked).
  * `private void DestroyBlock()`: Instantiates destruction particles, notifies `CastleController`, and destroys the GameObject.

### 2.6 SimpleAI.cs
Calculates trajectories and launches units automatically during the AI's turn.
* **Properties**:
  * `public Transform launchPoint`
  * `public GameObject[] unitPrefabs`
  * `public float maxLaunchVelocity = 25f`
  * `public float errorOffsetRange = 1.0f` (Adjusted by difficulty)
* **Methods**:
  * `public void TakeTurn()`: Starts the launch coroutine.
  * `private IEnumerator PerformLaunch()`: Selects a unit, finds a target, calculates the trajectory, and launches.
  * `private Vector2 FindTargetPosition()`: Selects a random player unit or player castle block.
  * `private Vector2 CalculateLaunchVelocity(Vector2 target)`: Computes the required velocity vector to hit the target at a 45-degree angle.

---

## 3. Data Models & Configuration (ScriptableObjects)

To decouple data from logic, unit and block statistics are configured via Unity ScriptableObjects.

### 3.1 UnitData.cs
```csharp
[CreateAssetMenu(fileName = "NewUnitData", menuName = "CastleBusters/Unit Data")]
public class UnitData : ScriptableObject
{
    public string unitName;
    public UnitType unitType;
    public float maxHP = 100f;
    public float moveSpeed = 2f;
    public float attackDamage = 20f;
    public float attackRange = 1.5f;
    public float attackCooldown = 1.5f;
    
    [Header("Bomber Specific")]
    public float explosionRadius = 2.5f;
    public float explosionDamage = 80f;
    
    [Header("Visuals")]
    public Sprite uiIcon;
    public GameObject prefab;
}
```

### 3.2 BlockData.cs
```csharp
[CreateAssetMenu(fileName = "NewBlockData", menuName = "CastleBusters/Block Data")]
public class BlockData : ScriptableObject
{
    public string blockName;
    public float maxHP = 100f;
    public float mass = 1.0f;
    public float friction = 0.5f;
    public float bounciness = 0.05f;
    
    [Header("Visuals")]
    public Sprite normalSprite;
    public Sprite crackedSprite;
    public Sprite heavilyCrackedSprite;
    public GameObject destructionEffectPrefab;
}
```

---

## 4. Implementation Phases & Task Breakdown

This section defines the step-by-step implementation plan for the **jeo team**. Each phase consists of specific tasks with clear inputs, outputs, and verification criteria.

### Phase 1: Core Physics & Launch System
* **Task 1.1: Drag-and-Launch Input & Trajectory Preview**
  * **Assigned Role**: Input & Physics Developer
  * **Inputs**: `LaunchManager.cs`, `LineRenderer` component.
  * **Implementation Steps**:
    1. Implement mouse/touch drag detection in `LaunchManager.HandleInput()`.
    2. Clamp the drag vector to `maxDragDistance = 3.0f`.
    3. Calculate launch velocity: `velocity = dragVector * launchForceMultiplier`.
    4. Draw the trajectory using:
       $$\vec{p}(t) = \vec{p}_0 + \vec{v}_0 t + \frac{1}{2} \vec{g} t^2$$
    5. Clear the trajectory line on release and instantiate the unit.
  * **Verification**: Run `LaunchManagerTests` to verify that the trajectory line positions match the mathematical formula.
* **Task 1.2: Physics Material & Mass Configuration**
  * **Assigned Role**: Physics Developer
  * **Inputs**: `PhysicsMaterial2D` assets, `DestructibleBlock.cs`.
  * **Implementation Steps**:
    1. Create three `PhysicsMaterial2D` assets: Wood, Stone, Iron.
    2. Set friction and bounciness values according to Section 5.1.
    3. Update `DestructibleBlock.MakeFall()` to apply the correct mass and material properties dynamically.
  * **Verification**: Verify block behavior in the physics sandbox scene. Blocks must not slide excessively or bounce unrealistically.

### Phase 2: Unit State Machine & Combat Logic
* **Task 2.1: Unit State Machine & Movement**
  * **Assigned Role**: Gameplay Developer
  * **Inputs**: `UnitController.cs`, `UnitData` ScriptableObject.
  * **Implementation Steps**:
    1. Implement the `UnitState` machine in `UnitController.Update()`.
    2. In `Grounded` state, call `FindTarget()` to locate the nearest enemy unit or block.
    3. Move towards the target using `Rigidbody2D.velocity` based on `moveSpeed`.
  * **Verification**: Run `UnitController_TakesDamage_AndDies` and verify state transitions from `Launched` to `Grounded`.
* **Task 2.2: Combat & Projectile System**
  * **Assigned Role**: Gameplay Developer
  * **Inputs**: `UnitController.cs`, `ArrowController.cs`, `Arrow` prefab.
  * **Implementation Steps**:
    1. Implement `MeleeAttack()` for Knight and Bomber.
    2. Implement `ShootArrow()` for Archer, instantiating the arrow and setting its velocity towards the target.
    3. Implement `ArrowController.OnTriggerEnter2D()` to deal damage to enemies and destroy itself.
    4. Implement `Explode()` for Bomber to deal AoE damage.
  * **Verification**: Run `BomberUnit_ExplodesOnCollision_AndDealsAoEDamage` and verify that nearby blocks take damage.

### Phase 3: Castle Structure & BFS Collapse System
* **Task 3.1: BFS Structural Integrity Check**
  * **Assigned Role**: Core Systems Developer
  * **Inputs**: `CastleController.cs`, `DestructibleBlock.cs`.
  * **Implementation Steps**:
    1. Implement `CastleController.CheckStructuralIntegrity()` using a BFS algorithm.
    2. Start BFS from all blocks marked as `isGroundAnchor` that are not falling.
    3. Traverse adjacent blocks using `GetNeighbors()`.
    4. Any block not visited during BFS must call `MakeFall()`.
  * **Verification**: Run `CastleController_BFS_DetectsUnsupportedBlocks` and verify that unsupported blocks fall.
* **Task 3.2: BFS Performance Optimization (Batching & Slicing)**
  * **Assigned Role**: Optimization Specialist
  * **Inputs**: `CastleController.cs`.
  * **Implementation Steps**:
    1. Implement a batching queue for block destruction events.
    2. Instead of running BFS immediately on block destruction, queue the request and run it once at the end of the frame using `WaitForEndOfFrame` or `LateUpdate`.
    3. For large castles, implement a coroutine-based BFS that limits traversals per frame (e.g., max 50 nodes per frame) to prevent spikes.
  * **Verification**: Profile the game during a Bomber explosion. The frame rate must remain stable above 60 FPS on target mobile devices.

### Phase 4: Game Loop & AI System
* **Task 4.1: GameManager Turn Loop & UI**
  * **Assigned Role**: Core Systems Developer
  * **Inputs**: `GameManager.cs`, UI Text elements.
  * **Implementation Steps**:
    1. Implement the `GameState` machine (`Setup`, `PlayerTurn`, `AITurn`, `GameOver`).
    2. Implement a 15-second turn timer. If the timer reaches 0, call `EndTurn()`.
    3. Implement victory condition checks by counting active blocks.
  * **Verification**: Play through a full game loop in the editor and verify that turns transition correctly and the game ends when a castle is destroyed.
* **Task 4.2: AI Trajectory Calculation & Calibration**
  * **Assigned Role**: AI Developer
  * **Inputs**: `SimpleAI.cs`.
  * **Implementation Steps**:
    1. Implement the trajectory calculation formula in `SimpleAI.CalculateLaunchVelocity()`.
    2. Inject a random offset to the target position based on the selected difficulty level.
  * **Verification**: Verify that the AI successfully hits targets within the specified error margins.

### Phase 5: Juice & Polish
* **Task 5.1: Screen Shake & Hit-Stop**
  * **Assigned Role**: UI/UX Developer
  * **Inputs**: Cinemachine package, `GameManager.cs`, `DestructibleBlock.cs`.
  * **Implementation Steps**:
    1. Set up a Cinemachine Virtual Camera with a `CinemachineImpulseListener`.
    2. Add a `CinemachineImpulseSource` to the Bomber and explosive barrel prefabs.
    3. Implement a `HitStop` manager that temporarily sets `Time.timeScale = 0f` for 0.05 seconds during major collapses.
  * **Verification**: Visual inspection during gameplay. Major explosions must trigger a satisfying screen shake and a brief pause.

---

## 5. Advanced Technical Algorithms & Math

### 5.1 Trajectory Equation
The trajectory of a launched unit is calculated using the standard kinematic equation:
$$\vec{p}(t) = \vec{p}_0 + \vec{v}_0 t + \frac{1}{2} \vec{g} t^2$$
Where:
* $\vec{p}(t)$ is the position at time $t$.
* $\vec{p}_0$ is the launch point position.
* $\vec{v}_0$ is the initial launch velocity vector.
* $\vec{g}$ is the gravity vector (typically `Physics2D.gravity`).

### 5.2 AI Launch Velocity Calculation
To hit a target at position $\vec{p}_t = (x, y)$ relative to the launch point $(0, 0)$ with a launch angle $\theta = 45^\circ$:
$$v^2 = \frac{g x^2}{2 \cos^2(\theta) (x \tan(\theta) - y)}$$
Since $\theta = 45^\circ$, $\cos(	heta) = \frac{\sqrt{2}}{2}$ and $\tan(	heta) = 1$. The formula simplifies to:
$$v^2 = \frac{g x^2}{x - y}$$
If $v^2 \le 0$ (target is mathematically unreachable at this angle), the AI falls back to a default velocity:
$$v = v_{\text{max}} \times 0.7$$

### 5.3 BFS Structural Integrity Algorithm
The structural integrity check is executed as follows:
```
Algorithm: CheckStructuralIntegrity
Input: allBlocks (List of all blocks in the castle)
Output: Marks unsupported blocks as falling

1. Let supported = empty HashSet
2. Let queue = empty Queue
3. For each block in allBlocks:
     If block.isGroundAnchor and not block.IsFalling:
       Add block to supported
       Enqueue block
4. While queue is not empty:
     Let current = Dequeue from queue
     For each neighbor of current in allBlocks:
       If neighbor is not in supported and not neighbor.IsFalling:
         Add neighbor to supported
         Enqueue neighbor
5. For each block in allBlocks:
     If block is not in supported and not block.IsFalling:
       Call block.MakeFall()
```

---

## 5.4 Recent Implementation Updates (2026-06-28)
- **ScriptableObjects Integration**: UnitData and BlockData ScriptableObjects were created and integrated into UnitController and DestructibleBlock.
- **BFS Structural Integrity Optimization**: Implemented batching and slicing in CastleController to prevent CPU spikes.
- **AI Trajectory Calibration**: Added error offset range to SimpleAI aiming.
- **Juice & Polish (Hit-Stop)**: Implemented HitStopManager to freeze time during major collapses and explosions.
- **2D Resource Generation**: Generated StoneBlock, Cannonball, and Background 2D pixel art assets using god-tibo-imagen.


## 6. Verification & Automated Testing Plan

### 6.1 Automated Test Suite
The automated test suite is located in `Assets/Editor/GamePlayTests.cs`. It must be run using the Unity Test Runner or the command line.

* **Run Tests Command**:
  ```bash
  # Run editor and playmode tests via Unity command line
  Unity -runTests -projectPath . -testResults test-results.xml -testPlatform editmode
  ```

### 6.2 Test Cases
1. **`DestructibleBlock_TakesDamage_AndDestroys`**:
   * **Goal**: Verify that blocks take damage and destroy themselves at 0 HP.
   * **Assertion**: `Assert.AreEqual(expectedHP, block.currentHP)` and `Assert.IsTrue(block == null)`.
2. **`UnitController_TakesDamage_AndDies`**:
   * **Goal**: Verify that units take damage and enter the `Dead` state at 0 HP.
   * **Assertion**: `Assert.IsTrue(unit == null)` or state is `Dead`.
3. **`BomberUnit_ExplodesOnCollision_AndDealsAoEDamage`**:
   * **Goal**: Verify that Bomber units deal AoE damage to nearby blocks.
   * **Assertion**: Verify HP reduction of a block placed within the explosion radius.
4. **`CastleController_BFS_DetectsUnsupportedBlocks`**:
   * **Goal**: Verify that removing a supporting block causes floating blocks to fall.
   * **Assertion**: `Assert.IsTrue(unsupportedBlock.IsFalling)`.

## Recent Updates
- **Visual Polish**: Added `ExplosionEffect` particle system to `DestructibleBlock` and `UnitController` prefabs.
- **Gameplay Verification**: Verified the game is playable in the Unity Editor. The `AutoPlayTest` script successfully simulated a drag-and-drop launch, confirming the physics and turn-based mechanics work as intended.
- **Code Refactoring (Ponytail Ultra)**: Aggressively refactored all core C# scripts (`GameManager`, `UnitController`, `DestructibleBlock`, `CastleController`, `LaunchManager`, `SimpleAI`, etc.) to collapse verbose logic, inline variables, and remove unnecessary brackets, achieving a much leaner codebase without altering behavior.
- **Asset Integration**: Configured generated `.png` files as `Sprite` type and assigned them to `DestructibleBlock`, `Bomber`, and `Background` prefabs/GameObjects.
- **AOS Overhaul (2026-07-03/04)**: Added an alternate capture-zone win condition (`CaptureZoneController`), Knight/Archer/Bomber combat trait passes, event-driven vent/buff/debuff/gate scheduling, and a 3-phase Chariot siege machine (see `docs/design/aos-overhaul.md`). Followed by a content pass (flying war-beast, hero loot growth, siege alarm feed) and a playtest QA pass (GimmickButton wiring, particle/damage-number polish).
- **Gimmick Fairness Fixes (code review cycle 3, 2026-07-04)**: Fixed 2 P1 regressions the review pass caught (keg-vs-launch-muzzle clearance, `GimmickFrameAnimator` world-footprint mismatch on sprite swap) plus Knight/Archer trait tunables.
- **Tracking backfill (2026-07-04)**: `.specify/cycles.md` was found stuck at 1/10 rows with a stale "pending" verdict despite 5 real commits of work since; backfilled to 6 honest rows. `dotnet build` confirms 0 errors at HEAD; live Unity MCP EditMode re-verification is still pending next session (the running editor holds the project lock and its MCP bridge currently reports an empty tools list).

