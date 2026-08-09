# Generated Unit Frames

Drop PerfectPixel Studio exported per-frame PNGs here so `UnitSpriteAnimator` can play authored sprite animation at runtime.

Expected paths:

- `Assets/Resources/GeneratedUnitFrames/Knight/Idle/*.png`
- `Assets/Resources/GeneratedUnitFrames/Knight/Walk/*.png`
- `Assets/Resources/GeneratedUnitFrames/Knight/Attack/*.png`
- `Assets/Resources/GeneratedUnitFrames/Knight/Launch/*.png`
- Repeat the same state folders for `Archer` and `Bomber`.

Use stable lexical frame names such as `idle_000.png`, `idle_001.png`, etc.; Unity's `Resources.LoadAll<Sprite>()` output is sorted by sprite name before playback.
