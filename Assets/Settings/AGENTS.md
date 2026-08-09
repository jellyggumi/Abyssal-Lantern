<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-27T14:30:33Z | Updated: 2026-06-27T14:30:33Z -->

# Settings

## Purpose
Contains the Universal Render Pipeline (URP) settings and renderer profiles for different quality levels (Balanced, High Fidelity, Performant), along with post-processing profiles.

## Key Files

| File | Description |
|------|-------------|
| `SampleSceneProfile.asset` | Post-processing profile for the SampleScene |
| `URP-Balanced.asset` | Balanced quality URP settings asset |
| `URP-Balanced-Renderer.asset` | Balanced quality URP renderer asset |
| `URP-HighFidelity.asset` | High fidelity quality URP settings asset |
| `URP-HighFidelity-Renderer.asset` | High fidelity quality URP renderer asset |
| `URP-Performant.asset` | Performant quality URP settings asset |
| `URP-Performant-Renderer.asset` | Performant quality URP renderer asset |

## Subdirectories

None.

## For AI Agents

### Working In This Directory
Modify these assets via the Unity Inspector to avoid corrupting the serialized YAML files.

### Testing Requirements
Verify rendering quality and performance in the Unity Editor Game view when switching quality levels.

### Common Patterns
URP asset and renderer configuration.

## Dependencies

### Internal
- Referenced by `ProjectSettings/` (GraphicsSettings.asset, QualitySettings.asset) and scenes.

### External
- Universal Render Pipeline package

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
