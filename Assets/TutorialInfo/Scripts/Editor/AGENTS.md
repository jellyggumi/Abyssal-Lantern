<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-06-27T14:30:33Z | Updated: 2026-06-27T14:30:33Z -->

# Editor

## Purpose
Contains the custom editor script for the Readme class, handling automatic layout loading and custom inspector rendering.

## Key Files

| File | Description |
|------|-------------|
| `ReadmeEditor.cs` | Custom editor for the Readme ScriptableObject, which automatically selects the readme and loads the layout on startup |

## Subdirectories

None.

## For AI Agents

### Working In This Directory
This is an Editor directory. All scripts here must use UnityEditor APIs and will not be included in the final build.

### Testing Requirements
Verify that selecting the Readme asset displays the custom inspector and that the layout loads correctly.

### Common Patterns
Custom Editor scripting, InitializeOnLoad, SessionState.

## Dependencies

### Internal
- Depends on `Assets/TutorialInfo/Scripts/Readme.cs`

### External
- Unity Editor
- Unity Engine

<!-- MANUAL: Any manually added notes below this line are preserved on regeneration -->
