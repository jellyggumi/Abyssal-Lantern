<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-27T14:30:33Z | Updated: 2026-06-27T14:30:33Z -->

# TutorialInfo

## Purpose
Contains the assets, scripts, and layout files for the project's tutorial/readme system, which helps onboard users to the project.

## Key Files

| File | Description |
|------|-------------|
| `Layout.wlt` | Unity window layout file used to set up the editor layout for the tutorial |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Icons/` | Contains icons used in the readme/tutorial (see `Icons/AGENTS.md`) |
| `Scripts/` | Contains the C# scripts for the readme and its custom editor (see `Scripts/AGENTS.md`) |

## For AI Agents

### Working In This Directory
Be careful when modifying the layout file (`Layout.wlt`). Scripts are located in the `Scripts/` subdirectory.

### Testing Requirements
Select the `Readme.asset` in the Unity Editor to verify the custom inspector layout and functionality.

### Common Patterns
Custom Editor scripting in Unity.

## Dependencies

### Internal
- Depends on `Assets/Readme.asset`

### External
- Unity Editor
- Unity Engine

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
