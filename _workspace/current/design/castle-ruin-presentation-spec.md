# Castle ruin presentation — spec (전면개선)

- run-id: 20260809-castle-war-stage1
- owner: design + engineering lanes (presentation-only; sim untouched)
- status: spec locked 2026-08-09; style proof pending audit

## Problem [OBSERVED]

All castle/wall blocks render the same `block_normal.png` triple
(`Assets/Sprites/block_*.png`, 1254², shared via `BlockData`). Result: the
castle reads as a repeated-tile blob ("단일 오브젝트처럼 보임") even though
collision is already per-block (`DestructibleBlock` + BoxCollider2D each).
Damage feedback exists per hit (burst/shake/labels) but the *structure* never
visibly becomes a ruin until blocks pop out of existence.

## Goal

1. **Facade unity**: blocks get position-aware skin roles so the castle reads
   as one silhouette (crown/face/edge/base), not N identical tiles.
2. **Progressive ruin**: per-block 3-state damage skins + castle-wide wear
   ratchet — the fortress visibly degrades as aggregate HP drops.
3. **Interactive real-time feedback**: crack decals appear at hit moments,
   band transitions produce crumble moments, block destruction shakes the
   neighbors' presentation, aggregate milestones (75/50/25%) fire castle-wide
   dust waves.
4. Art generated via **Codex backend (`gti`, god-tibo-imagen)** per CLAUDE.md
   §3 — concept lane first, `.provenance.json`, audit before `Assets/`.

## Non-goals / boundaries

- No sim writes from presentation (CLAUDE.md §2). Rejected: neighbor
  "shudder" via transform offset — a Static collider move IS a sim write.
  Neighbor feedback = color pulse + seam dust only.
- No new damage bands: keep existing 0.7 / 0.3 HP-ratio thresholds
  (`DestructibleBlock.UpdateVisuals`).
- BlockData material identity (wood/stone/iron HP+mass) unchanged; skins
  layer on top via existing tint multiply.

## Skin taxonomy

Role assigned per block from its grid position within the castle's block
bounds (pure function → EditMode-testable):

| Role | Rule (grid coords) | Tile |
|---|---|---|
| Crown | top row (y == maxY) | crenellated cap, transparent sky notches |
| Edge | x == minX or x == maxX, not top | quoined corner column; right side = flipX |
| Base | bottom row (y == minY), not edge | heavy foundation stones, moss line |
| Face | everything else | ashlar wall face |

Ties: Crown beats Edge beats Base (a 1-wide tower block is Crown at top).
3 damage states per role: `s0` intact / `s1` cracked / `s2` crumbling.
12 tiles total, 512² each, magenta #FF00FF matte → alpha-keyed at slice time.

## Runtime architecture (all new code presentation-side)

- `CastleSkinLibrary` (static): `Resources/CastleSkin/{role}_{s0|s1|s2}`
  lazy-load + cache; `AssignRole(x,y,bounds)` pure static. Missing textures →
  null → callers keep BlockData sprites (art-absent build stays correct).
- `DestructibleBlock.SetSkinSprites(n,c,h)`: assigns the three sprite fields
  + re-applies current band; `SetDisplayWearFloor(int)`: presentation-only
  floor on the *displayed* band (`band = max(ownBand, wearFloor)`) — HP never
  touched.
- `CastleFacadeDirector` (static, invoked from `CastleController.
  RefreshBlockList` + block-destroyed hook): computes bounds → assigns roles
  → pushes skins; tracks aggregate HP ratio (Σ currentHP / Σ maxHP, read-only)
  → milestone events at 0.75/0.5/0.25:
  - dust wave across castle span + shockwave ring + shake (existing
    GameFeelVfx surface)
  - wear-floor ratchet: <0.5 → all surviving blocks display ≥ s1.
- `CastleRuinFx.NotifyBlockDamaged(block, dmg, prevRatio, newRatio)` called
  from `TakeDamage` (mirrors `GameplayUxDirector.NotifyDamage` precedent):
  - every hit ≥8% maxHP: pooled crack decal child sprite at impact-ish point
    (block center + jitter), sorting order 3, fades in 80ms, lives with block,
    cap 3/block.
  - band transition (0.7/0.3 crossing): "crumble moment" = extra debris using
    the block's current sprite + louder shockwave + decal.
  - block destroyed: neighbor color pulse (0.15s dim) + seam dust on adjacent
    survivors (via CastleController.GetNeighbors surface).

## Asset plan (gti / god-tibo-imagen)

1 style-proof strip first (Face, 3 states, 1536×512, magenta bg, style ref =
existing `block_normal.png` weathered-stone look) → human-auditable PNG in
`_workspace/current/design/concept/castle-skin/` + `.provenance.json`
(prompt, tool=gti, model, sha256). After audit: 3 remaining strips with the
approved Face strip passed as `--input` style anchor. Slice 512² cells +
magenta→alpha key (Python/PIL) → `Assets/Resources/CastleSkin/` (12 PNG).
PPU irrelevant: `ApplyPresentationScale` normalizes native size to
`targetWorldSize` (DestructibleBlock.cs:127).

## Acceptance (Stage-3 presentation gate inputs)

- [x] EditMode: role assignment pure-function tests (corners, 1-wide tower, 2x2 minimum castle) green; runtime smoke also resolves every role's three Resources sprites.
- [x] EditMode: wear-floor band arithmetic tests green; existing 41-test suite untouched and green.
- [x] PlayMode automated: impact burst/decal/hit-stop and combo-session contracts green; focused player→AI handoff resolves in 22.4 s.
- [x] PlayMode manual/WebGL: volley into castle produced the impact flash, debris/dust collapse sequence, milestone wave, and neighbor response without repeated milestone spam; `qa/evidence/final-castle-skin-combat.png`.
- [x] All 12 tiles are present in `Assets/Resources/CastleSkin/`; each concept source now has a SHA-256 `.provenance.json` companion. The exact original generation prompt/model identifier was not available, so the provenance records the frozen brief and that limitation rather than inventing metadata.
- [x] Visual capture: `qa/evidence/final-castle-skin-combat.png` records the active damage/collapse moment; `qa/evidence/final-core-cap-after-shot.png` records the rebuilt counterplay-safe post-impact state.
