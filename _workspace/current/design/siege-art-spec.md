# Siege art spec — 새총 launcher + 성 (castle keep)

- run-id: 20260809-castle-war-stage1
- owner: design lane
- status: generated, keyed, promoted, EditMode-pinned 2026-08-10
- scope: the unit-launch affordance and the base the player defends

## 1. What changed and why

| Before | After | Defect it fixes |
|---|---|---|
| Launch point = abstract glowing **portal ring** | **새총 (slingshot)** | The ring never explained *why* dragging backwards fires a soldier forwards. A slingshot states the control's whole grammar in its silhouette. |
| Base = abstract **crystal core** | **성 (castle keep)**, 3 damage stages | "How close am I to losing" required reading a number. A keep that visibly loses its roofs, battlements and wall front answers it at a glance. |
| Core froze on a still at first damage | Keep **keeps animating**, swapping frame set per band | The most dramatic moment of the match had the least motion. |

CLAUDE.md §2 holds throughout: all of this is presentation. Nothing here reads
or writes simulation state except *reading* the HP band.

## 2. Generation provenance

CLAUDE.md §3 nominates **Codex CLI** for 2D sprites, with `god-tibo-imagen`/`ppgen`
as the fallback. Both were unavailable this session:

- `codex exec` → `You've hit your usage limit … try again at Aug 16th, 2026`.
- `gti` (god-tibo-imagen) → HTTP 429 on every attempt; it proxies the *same*
  Codex backend, so the quota exhaustion is shared, not independent.
- `ppgen` → installed and runnable, but the shipped build advertises only
  `gemini|openrouter|fal|byteplus` (no `god-tibo-imagen` provider), and no key
  for any of those exists on this machine.

Fallback actually used: **Higgsfield CLI** (authenticated, 8.76 credits), which
is a real image generator rather than a procedural stand-in.

| Asset | Model | Credits |
|---|---|---|
| Slingshot key art | `flux_2` | 1.0 |
| Castle keep s0 (intact) | `flux_2` | 1.0 |
| Castle keep s1 (battered) | `flux_kontext`, edit of s0 + s2 | 1.5 |
| Castle keep s2 (near-ruin) | `flux_kontext`, edit of s0 then re-pushed | 3.0 |

Using `flux_kontext` (image-conditioned edit) for stages 1–2 rather than three
independent renders is what makes them **the same castle** at three points of
destruction instead of three different castles.

Every promoted PNG carries an adjacent `.provenance.json` recording prompt,
tool, model, checksum and notes, per CLAUDE.md §3.

## 3. Keying and frame derivation

`_workspace/current/design/concept/gen_siege_art.py` owns this and is
deterministic — re-running it reproduces byte-identical frames.

**Keying.** Border flood-fill rather than colour-distance: the renders carry a
subtle background vignette that a flat distance key smears into a pink halo, and
flooding only removes pixels *connected to the outside* so an enclosed warm
highlight is never punched out. A second pass removes enclosed background
pockets (the gap between the slingshot's prongs), with a size floor so render
speckle cannot punch pinholes through the sprite.

**Banner reconstruction.** The model painted the pennant cloth *in the key
colour* — measured RGB distance 2–4 from the background on all three stages. No
threshold, enclosure test, or morphological closure can separate pixels that are
literally background-coloured; all three were tried and are recorded in the
script's docstrings. The cloth is therefore re-drawn as explicit geometry
composited **underneath** the keyed art, so the render's own surviving gold trim
and pole draw over it. This is a **composite, not a recovery**, and says so in
the provenance.

**Frame derivation.** Animation frames are derived from one keyed still per
stage, not generated per-frame. Per-frame model generation drifts the subject's
identity between frames; deriving guarantees a byte-stable silhouette, which is
what an idle loop needs.

## 4. Shipped animation

| Key | Frames | fps | Content |
|---|---|---|---|
| `Gimmicks/slingshot_anim` | 6 | 8 | band sway + 1px breathe |
| `Gimmicks/castle_keep_s0_anim` | 4 | 5 | banner flutter, confident window glow |
| `Gimmicks/castle_keep_s1_anim` | 4 | 4 | ragged banner, weak embers, settling dust |
| `Gimmicks/castle_keep_s2_anim` | 4 | 4 | no banner, dead windows, heavier dust, slump |

Amplitudes are deliberately small. These are idle loops behind gameplay: a
launcher that lurches or a keep that wobbles pulls the eye off the volley.

## 5. Runtime wiring

- `GimmickAnimLibrary.SlingshotAnim`, `CastleKeepAnim(stage)`, `CastleKeepStill(stage)`
  (both stage helpers clamp to 0..2 so a bad band can never request unauthored art).
- `GimmickFrameAnimator.Retarget(key, fps)` — swaps the running loop **without
  re-attaching**, so the keep keeps one animator and one world footprint across a
  stage change. Re-attaching would re-measure the footprint from the new stage's
  art, and s2's smaller ruin silhouette would visibly shrink the keep's collider.
  Loop phase is preserved so a band crossing never snaps the flutter mid-beat.
- `CastleCoreGimmick` loads the keep stills into the damage-state sprite slots
  **and** drives the staged loop, so the silhouette still changes when the
  animator is unavailable (missing frames, EditMode, suspended).
- `LaunchManager` prefers the slingshot, falls back to the legacy portal frames,
  then to the procedural ring — an art-less build keeps a readable affordance.

## 6. Two real bugs found and fixed on the way

1. **`LaunchManager.Update()` clobbered the launch affordance's fitted scale.**
   It assigned the raw breathing pulse to `localScale` each frame, discarding the
   world-size fit computed at setup — so framed launch art had been rendering at
   native sprite scale. The pulse now multiplies *into* the captured base scale.
   Pre-existing; the portal art was affected too.
2. **The promoted PNGs imported as `textureType: Default`.** Copying files in
   without `.meta` made Unity mint default importers, so
   `Resources.LoadAll<Sprite>` returned an **empty array** for every frame folder
   and the launcher silently degraded to its procedural ring. All 21 metas were
   rewritten as Sprite importers (`textureType: 8`, `spriteMode: 1`) with GUIDs
   preserved. Caught by the test lane before it shipped.

## 7. Verification [OBSERVED 2026-08-10]

- Sprite import probe: `slingshot_anim` → 6 sprites, each keep stage → 4 sprites,
  all three stills → 512×512. Zero-sprite folders would have meant silent fallback.
- **285 EditMode tests, 281 pass.** 12 new pins in
  `Assets/Tests/EditMode/SiegeArtResourceTests.cs` cover frame counts, ordinal
  frame ordering, non-empty art, all three stages present as both loop and still,
  the three stages being **genuinely different art** (pixel-signature compare,
  not just distinct references), stage-index clamping, HP→band boundaries,
  loop-wrap math, and that the slingshot did not overwrite the legacy portal art.
- The 4 failures are **pre-existing and unrelated**: they are `DrawTrajectory`
  point-count and float-horizon assertions that commit `bf491069` (02:50,
  another session) invalidated by changing the integration loop from `i=1` to
  `i=0`. `DrawTrajectory` is byte-identical between `HEAD` and this working tree
  — verified by function-body extraction, not by inspection. Not touched here.
- Frame-difference proof sheet: `design/concept/anim_frames_proof.png`.
