<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-27T14:30:33Z | Updated: 2026-06-27T14:30:33Z -->

# Assets

## Purpose
The main assets directory containing all scenes, settings, and scripts for the Unity project.

## Key Files

| File | Description |
|------|-------------|
| `Readme.asset` | ScriptableObject asset containing tutorial/readme information for the project |
| `UniversalRenderPipelineGlobalSettings.asset` | Global settings for the Universal Render Pipeline |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Scenes/` | Contains Unity scene files (see `Scenes/AGENTS.md`) |
| `Settings/` | Contains URP settings and renderer profiles (see `Settings/AGENTS.md`) |
| `TutorialInfo/` | Contains scripts, icons, and layout files for the project readme/tutorial (see `TutorialInfo/AGENTS.md`) |

## For AI Agents

### Working In This Directory
Organize new assets into appropriate subdirectories (e.g., Scripts, Scenes, Settings). Do not clutter the root of the `Assets/` directory.

### Testing Requirements
Verify changes in the Unity Editor by opening the relevant scenes or inspecting assets.

### Common Patterns
Standard Unity asset organization and URP configuration.

## Dependencies

### Internal
- `Packages/` for package dependencies
- `ProjectSettings/` for project-wide configurations

### External
- Unity Engine
- Universal Render Pipeline

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
