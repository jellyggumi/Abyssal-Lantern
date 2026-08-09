<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-27T14:30:33Z | Updated: 2026-06-27T14:30:33Z -->

# ProjectSettings

## Purpose
Contains all the project-wide settings and configurations for the Unity project.

## Key Files

| File | Description |
|------|-------------|
| `ProjectSettings.asset` | Main project settings (company name, product name, bundle identifier, etc.) |
| `GraphicsSettings.asset` | Graphics settings, including the active URP pipeline asset |
| `QualitySettings.asset` | Quality settings defining different graphics quality levels |
| `TagManager.asset` | Defines tags, layers, and sorting layers |
| `InputManager.asset` | Defines input axes and mappings |
| `EditorSettings.asset` | Editor-specific settings (serialization mode, version control mode, etc.) |

## Subdirectories

None.

## For AI Agents

### Working In This Directory
Avoid editing these files manually unless necessary. Use the Unity Project Settings window to modify them.

### Testing Requirements
Verify settings are applied correctly in the Unity Editor.

### Common Patterns
Unity project settings serialization.

## Dependencies

### Internal
- References assets in `Assets/Settings/` (e.g., URP assets)

### External
- Unity Editor

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
