# Castle Clashers (Castle Busters) Analysis & Unity Implementation Design

- **Source Reference**: [[raw/sources/castle-busters]]
- **Target Platform**: Unity (Universal Render Pipeline - URP)
- **Genre**: Real-time 1v1 PvP Destructible Castle Defense / Artillery Strategy
- **Status**: Research & Design Phase

---

## 1. Game Analysis: Castle Clashers (com.epicoro.castleclashers)

### Core Loop & Mechanics
* **Deck Building**: Players select up to 8 unique units (Knights, Archers, Bombers, Siege Weapons, etc.) to form their battle deck.
* **Matchmaking**: Real-time 1v1 PvP matches where players face off against each other's castles.
* **Tactical Combat**: Players aim and launch/deploy units from their launch point towards the opponent's castle.
* **Destruction & Adaptation**: The castle is fully destructible. Destroying walls opens up new lines of sight/fire, and collapsing floors drops enemy units to lower levels, causing fall damage or exposing them to attacks.
* **Victory Condition**: Eliminate all enemy units OR reduce the opponent's castle to ruins.

### UI/UX & Progression
* **Main Menu**: Deck management, unit upgrades, rank/arena progression, and matchmaking button.
* **In-Game UI**:
  * **Turn/Timer Indicator**: Shows whose turn it is and the remaining time.
  * **Unit Selection Cards**: Displays the available units in the deck with their cost/cooldown.
  * **Aiming Trajectory**: A dotted line showing the predicted path of the launched unit.
  * **Castle Health/Status**: Visual representation of the structural integrity of both castles.
* **Progression**: Players earn rewards (gold, chests, cards) to upgrade unit levels, unlock new traits, and strengthen their castle walls.

---

## 2. Unity Architecture & Core Elements

### A. Physics-Based Destructible Castle System
* **Grid-Based Block Layout**: The castle is constructed using individual block prefabs arranged in a grid.
* **Destructible Block (`DestructibleBlock.cs`)**:
  * Each block has HP and sprite states (Normal, Cracked, Heavily Cracked).
  * When HP reaches 0, the block is destroyed, triggering a structural integrity check.
* **Structural Integrity (`CastleController.cs`)**:
  * Uses a Breadth-First Search (BFS) flood-fill algorithm starting from ground anchors.
  * Any blocks that are no longer connected to the ground anchors are detached, converted to dynamic Rigidbodies, and fall down due to gravity.

### B. Aiming & Launching Mechanics
* **Drag-to-Aim Input**: Detects touch/mouse drag to calculate launch angle and velocity.
* **Trajectory Prediction (`LaunchManager.cs`)**:
  * Uses kinematic equations to calculate the parabolic path of the projectile.
  * Renders the path using a `LineRenderer` with a dotted texture.
* **Projectile Physics**: Launched units have a `Rigidbody2D` and `Collider2D` to interact with the environment and enemy units.

### C. Unit State Machine & AI
* **Unit Controller (`UnitController.cs`)**:
  * States: `Idle`, `Launched`, `Grounded`, `Attacking`, `Dead`.
  * **Knight**: High HP, moves forward, attacks blocks/units in front.
  * **Archer**: Ranged attack, shoots arrows at exposed enemy units.
  * **Bomber**: Explodes on contact, dealing area-of-effect (AoE) damage to blocks and units.
* **Simple AI (`SimpleAI.cs`)**:
  * Simulates opponent actions by selecting a unit, aiming at a target block/unit, and launching it.

---

## 3. Unity Resources & Asset Search

### Recommended Unity Packages
* **Universal Render Pipeline (URP)**: For stylized 2D lighting, post-processing (bloom, vignette), and optimized rendering.
* **Cinemachine**: For dynamic camera tracking (following launched units, shaking on impact).
* **Input System**: For cross-platform touch and mouse input handling.
* **TextMeshPro**: For crisp, stylized UI text and damage numbers.

### Recommended Asset Store & Free Resources
* **2D Art Assets**:
  * *Stylized Castle/Dungeon Tilesets*: For building the castle blocks (wood, stone, iron).
  * *2D Fantasy Character Sprites*: For Knight, Archer, Bomber, and Siege Weapon animations.
* **Visual Effects (VFX)**:
  * *2D Cartoon Particle FX*: For explosions, dust, debris, and launch trails.
* **Audio Assets**:
  * *Casual Game SFX Pack*: For launch whooshes, block impacts, explosions, and UI clicks.

---

## 4. Brainstorming Ideas: Visual Effects & Animations

### Visual Effects (VFX)
* **Launch Trail**: A particle trail (smoke or magic dust) following the unit during flight.
* **Impact Explosion**: A burst of dust, wood splinters, or stone debris when a block is damaged or destroyed.
* **Screen Shake**: Dynamic camera shake proportional to the damage dealt (e.g., small shake for arrows, massive shake for bomber explosions).
* **Trajectory Animation**: Moving dots along the trajectory line to indicate direction.

### Animations
* **Unit Animations**:
  * *Idle*: Breathing/swaying animation.
  * *Aim/Launch*: Unit curls up or gets loaded into a catapult.
  * *Flight*: Spinning or flailing arms during flight.
  * *Attack*: Melee swing for Knight, bow release for Archer.
  * *Hurt/Die*: Flash red on damage, fade out or fly off-screen on death.
* **Environmental Animations**:
  * *Block Crumbling*: Sprite swapping to cracked states, followed by particle bursts.
  * *Floor Collapse*: Smooth physics-based falling and rotation of unsupported blocks.

### Game Feel & Juice
* **Hit Stop (Impact Freeze)**: Pause the game physics for a fraction of a second (e.g., 0.05s) on massive impacts to emphasize power.
* **Floating Damage Numbers**: Spawn stylized text above damaged blocks/units, floating upwards and fading out.
* **Camera Zoom/Pan**: Zoom in on the target castle during a launch, and pan back to the player's castle when the turn ends.

---

## 5. Detailed Implementation Plan & Roadmap

### Phase 1: Core Physics & Destruction (Days 1-3)
* Set up the URP 2D project and physics layers.
* Implement `DestructibleBlock.cs` and `CastleController.cs`.
* Create a test scene with a 3-story castle and verify that destroying bottom blocks causes the upper unsupported blocks to collapse.

### Phase 2: Aiming & Launching Mechanics (Days 4-5)
* Implement drag-to-aim input handling.
* Create the parabolic trajectory prediction line using `LineRenderer`.
* Implement unit launching with physics forces and collision detection.

### Phase 3: Unit AI & Combat Loop (Days 6-8)
* Implement the unit state machine (`UnitController.cs`).
* Create prefabs for Knight, Archer, and Bomber.
* Implement combat logic (melee attacks, ranged arrow shooting, bomber explosions).

### Phase 4: Game Loop, UI, & Simple AI (Days 9-10)
* Implement `GameManager.cs` to manage turn transitions and victory conditions.
* Create the UI Canvas (turn text, timer, unit cards, game over panel).
* Implement `SimpleAI.cs` to simulate opponent turns.

### Phase 5: Visual Polish, Juice, & Audio (Days 11-12)
* Add particle effects for launches, impacts, and explosions.
* Implement screen shake and floating damage numbers.
* Integrate sound effects and background music.
* Perform play testing, capture screenshots, and verify the game loop.
