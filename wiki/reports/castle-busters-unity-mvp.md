# Castle Busters (Castle Clashers) Unity MVP Implementation Plan & Codebase

- **Source Reference**: [[raw/sources/castle-busters]]
- **Target Platform**: Unity (Universal Render Pipeline - URP)
- **Genre**: Real-time 1v1 PvP Destructible Castle Defense / Artillery Strategy
- **Status**: Core Codebase Implemented & Compiled Successfully (0 errors, 0 warnings)

---

## 1. Game Analysis & Core Mechanics

Based on the scraped data from the Google Play Store page for **Castle Busters** (developed by VOODOO), the game is a real-time 1v1 PvP strategy game where the castle itself is a weapon. The core gameplay loop and mechanics are:

### Core Loop
1. **Deck Building**: Select up to 8 unique units (Knights, Archers, Bombers, Siege Weapons, etc.).
2. **Matchmaking**: Real-time 1v1 PvP (Local 1v1 or Player vs. AI for MVP).
3. **Tactical Combat**: Aim and launch/deploy units inside/against the opponent's castle.
4. **Destruction & Adaptation**: Destroy walls to create lines of sight/fire, collapse floors to drop enemy units, and adapt to the changing battlefield.
5. **Victory Condition**: Eliminate all enemy units OR reduce the opponent's castle to ruins.

### Key Gameplay Elements
* **Destructible Castle**: The castle is a dynamic, destructible weapon/shield. It has multiple floors (typically 3 levels) and walls that can be breached.
* **Physics-based Destruction**: Collapsing floors drop units to lower levels, causing fall damage or exposing them to new attack angles.
* **Aiming & Launching**: Drag-to-aim mechanics (similar to Angry Birds or Worms) for projectile/unit launching, combined with real-time unit deployment.
* **Unit Roles**:
  * **Knight**: Frontline breakthrough, high durability.
  * **Archer**: Ranged attacks targeting exposed units.
  * **Bomber**: High structural damage to walls and floors.
  * **Siege Weapon**: Heavy artillery for massive castle destruction.

---

## 2. Visual Presentation & Animations

### Art Style & Rendering
* **Style**: Stylized 2D or 2.5D physics-based art. Clean, readable silhouettes for units and castle structures.
* **Rendering (URP)**:
  * Use **URP 2D Renderer** with **2D Lights** (Point Lights for explosions, Global Light for day/night cycle).
  * **Sprite Lit Default** shaders for dynamic lighting on sprites.
  * **Post-Processing**: Bloom for explosions/projectiles, Vignette for damage feedback, and Color Adjustments for stylized color grading.

### Animation Requirements
* **Unit Animations (Spine or Unity Sprite Sheets)**:
  * Idle, Walk, Aim/Prepare, Attack/Launch, Hurt, Die, Fall.
* **Environmental Animations**:
  * Wall cracking/crumbling (particle systems + sprite swapping).
  * Floor collapse (physics-driven rigidbodies falling down).
  * Dust and debris particle effects on impact.
* **UI Animations**:
  * Aiming trajectory line (dotted line with moving dots).
  * Card selection hover and deployment feedback.
  * Health bars and damage numbers floating above units.

---

## 3. Implemented Codebase Architecture

The core C# scripts have been implemented in `Assets/Scripts/` and compiled successfully.

### A. Destructible Castle System (`DestructibleBlock.cs`)
Handles individual block health, sprite swapping based on damage, and falling physics when unsupported.
```csharp
// Assets/Scripts/DestructibleBlock.cs
// - Manages block HP, sprite swapping (normal, cracked, heavily cracked).
// - Triggers structural integrity check on destruction.
// - Becomes dynamic Rigidbody2D and falls when unsupported.
```

### B. Castle Structural Integrity (`CastleController.cs`)
Manages the grid of blocks and performs a Breadth-First Search (BFS) flood-fill from the ground anchors to determine which blocks are unsupported and should collapse.
```csharp
// Assets/Scripts/CastleController.cs
// - Performs BFS flood-fill starting from ground anchors.
// - Detaches and drops any blocks that are no longer connected to the ground.
```

### C. Aiming & Launching System (`LaunchManager.cs`)
Handles drag input, calculates launch velocity, renders a parabolic trajectory line using kinematic equations, and instantiates/launches the selected unit.
```csharp
// Assets/Scripts/LaunchManager.cs
// - Drag-to-aim input handling.
// - Parabolic trajectory prediction using LineRenderer.
// - Spawns and launches units with physics force.
```

### D. Unit AI & Physics (`UnitController.cs` & `ArrowController.cs`)
Manages unit state machine (Idle, Launched, Grounded, Attacking, Dead), movement towards targets, melee/ranged attacks, and Bomber explosion logic.
```csharp
// Assets/Scripts/UnitController.cs
// - State machine for Knight, Archer, and Bomber.
// - Handles movement, target acquisition (enemy units or blocks), and combat.
// - Bomber explodes on contact, dealing area-of-effect damage.
```

### E. Game Loop & UI (`GameManager.cs` & `SimpleAI.cs`)
Manages turn transitions, card selection, victory conditions, and simple AI opponent actions.
```csharp
// Assets/Scripts/GameManager.cs
// - Singleton managing turn-based flow (Player vs. AI).
// - Monitors victory conditions (all enemy blocks destroyed or units dead).
```

---

## 4. Unity Editor Setup Guide

To set up the MVP in the Unity Editor, follow these steps:

### Step 1: Layers & Tags Setup
1. Create the following **Layers**:
   - `PlayerUnit` (Layer 8)
   - `EnemyUnit` (Layer 9)
   - `PlayerCastle` (Layer 10)
   - `EnemyCastle` (Layer 11)
   - `Ground` (Layer 12)
2. Configure **Physics 2D Collision Matrix** (Project Settings > Physics 2D):
   - `PlayerUnit` should collide with `EnemyUnit`, `EnemyCastle`, and `Ground`.
   - `EnemyUnit` should collide with `PlayerUnit`, `PlayerCastle`, and `Ground`.
   - `PlayerCastle` blocks should collide with each other and `Ground`.
   - `EnemyCastle` blocks should collide with each other and `Ground`.

### Step 2: Prefab Setup
1. **Destructible Block Prefab**:
   - Create a 2D Sprite GameObject (e.g., a 1x1 square).
   - Add a `BoxCollider2D` and a `Rigidbody2D` (set Body Type to `Static`).
   - Add the `DestructibleBlock` component.
   - Assign normal, cracked, and heavily cracked sprites.
2. **Unit Prefabs (Knight, Archer, Bomber)**:
   - Create 2D Sprite GameObjects for each unit type.
   - Add a `CapsuleCollider2D` or `CircleCollider2D` and a `Rigidbody2D` (set Body Type to `Static` initially).
   - Add the `UnitController` component and configure stats (HP, speed, damage, range).
   - For the **Archer**, assign an arrow prefab and create a child GameObject for the `FirePoint`.
   - For the **Bomber**, assign an explosion particle effect prefab.

### Step 3: Scene Hierarchy Setup
1. **GameManager**: Create an empty GameObject and attach `GameManager`.
2. **LaunchManager**: Create an empty GameObject and attach `LaunchManager`. Add a `LineRenderer` component to it and assign it to the `Trajectory Line` field.
3. **Player Castle**:
   - Create an empty GameObject named `PlayerCastle` and attach `CastleController` (set `Is Player Castle` to `true`).
   - Instantiate `DestructibleBlock` prefabs as children of `PlayerCastle` to build a 3-story structure.
   - Mark the bottom-most blocks as `Is Ground Anchor = true`.
4. **Enemy Castle**:
   - Mirror the Player Castle on the right side of the screen.
   - Attach `CastleController` (set `Is Player Castle` to `false`).
5. **Simple AI**: Create an empty GameObject and attach `SimpleAI`. Assign the unit prefabs and the enemy launch point.
6. **UI Canvas**:
   - Create a Canvas with a Turn Text, Timer Text, and Game Over Panel.
   - Add buttons for unit selection (Knight, Archer, Bomber) and link them to `GameManager.SelectUnit(int)`.

---

## 5. Next Steps & Polish
1. **Visual Effects**: Add screen shake on block destruction and unit explosions.
2. **Sound Effects**: Add launch whoosh, block impact/crumble, and explosion sounds.
3. **Juice**: Add floating damage numbers when blocks or units take damage.
4. **Level Design**: Create different castle layouts and block types (e.g., wood, stone, iron) with varying HP and weights.
