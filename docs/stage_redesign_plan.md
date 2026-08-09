# Stage Redesign Plan: Desert Dunes & Volcanic Abyss

## 1. Background Concept Redesign
To create a clear progression and visual variety across the three stages, we are redesigning Stage 2 and Stage 3 to have distinct environmental themes compared to Stage 1's green plains.

### Stage 1: Siege Plains (공성 평원) - Existing
- **Theme**: Green meadows, plains, siege warfare.
- **Visuals**: Green grass, blue sky, medieval siege elements.
- **Gimmicks**: Barrel, MiniTower, Rune, Patrol.

### Stage 2: Desolate Dunes (황량한 모래언덕) - New
- **Theme**: Desert, ancient ruins, sandstorms, blazing sun.
- **Visuals**: Golden sand dunes, ancient stone ruins, pyramids in the background, hot sun.
- **Gimmicks**: Rune, SpikeTrap, Patrol. (Changed from MiniTower, Barrel, Patrol to focus on traps and ancient runes).
- **Background Tint**: Warm sandy yellow (`new Color(1.0f, 0.9f, 0.7f, 1f)`).

### Stage 3: Volcanic Abyss (화산 심연) - New
- **Theme**: Volcanic, lava rivers, dark obsidian rocks, glowing embers.
- **Visuals**: Jagged dark volcanic rocks, flowing red lava rivers, smoke, and glowing embers.
- **Gimmicks**: Barrel, MiniTower, SpikeTrap. (Changed from SpikeTrap, Rune, Patrol to focus on explosive barrels, defensive towers, and traps).
- **Background Tint**: Fiery reddish-orange (`new Color(1.0f, 0.75f, 0.7f, 1f)`).

---

## 2. Gimmick Type Variation Strategy
We ensure that each stage has a unique combination of gimmicks to vary the gameplay:
- **Stage 1**: Balanced mix (Barrel, MiniTower, Rune, Patrol).
- **Stage 2**: Trap & Magic focus (Rune, SpikeTrap, Patrol) - no towers or barrels.
- **Stage 3**: Siege & Hazard focus (Barrel, MiniTower, SpikeTrap) - no runes or patrols.

---

## 3. Image Generation Plan (gti / perfectpixel style)
We will generate the background images using the `gti` CLI tool. To match the game's pixel art style, we will use a detailed prompt specifying the pixel art style, resolution, and color palette.

### Prompt Generation & Cross-Validation (Prompt Repetition)
We will refine the prompts through 5 iterations of cross-validation to ensure the best quality and style consistency.

#### Stage 2 Prompt Iterations:
1. `pixel art desert background with sand dunes and pyramids`
2. `16-bit pixel art desert landscape, golden sand dunes, ancient ruins, blazing sun, game background`
3. `detailed 16-bit pixel art desert landscape, golden sand dunes, ancient stone ruins, pyramids in the background, hot sun, game background, vibrant colors`
4. `detailed 16-bit pixel art desert landscape, golden sand dunes, ancient stone ruins, pyramids in the background, hot sun, game background, vibrant colors, clean pixel lines, side-scroller style`
5. `detailed 16-bit pixel art desert landscape, golden sand dunes, ancient stone ruins, pyramids in the background, hot sun, game background, vibrant colors, clean pixel lines, side-scroller style, 2d platformer backdrop` (Final Selected)

#### Stage 3 Prompt Iterations:
1. `pixel art volcanic background with lava and rocks`
2. `16-bit pixel art volcanic landscape, lava rivers, dark rocks, glowing embers, game background`
3. `detailed 16-bit pixel art volcanic landscape, flowing red lava rivers, dark jagged obsidian rocks, smoke, glowing embers, game background, vibrant colors`
4. `detailed 16-bit pixel art volcanic landscape, flowing red lava rivers, dark jagged obsidian rocks, smoke, glowing embers, game background, vibrant colors, clean pixel lines, side-scroller style`
5. `detailed 16-bit pixel art volcanic landscape, flowing red lava rivers, dark jagged obsidian rocks, smoke, glowing embers, game background, vibrant colors, clean pixel lines, side-scroller style, 2d platformer backdrop` (Final Selected)

---

## 4. Implementation Steps
1. Generate `Background_Stage2.png` and `Background_Stage3.png` using `gti`.
2. Resize/crop the generated images to match the original aspect ratios/sizes:
   - Stage 2: 1717x916
   - Stage 3: 1693x929
3. Update `Assets/Scripts/StageDefinitions.cs` with the new stage names, Korean names, and allowed gimmicks.
4. Update `Assets/Editor/StageDefinitionsTests.cs` to reflect the new stage definitions and allowed gimmicks.
5. Run tests to verify correctness.
