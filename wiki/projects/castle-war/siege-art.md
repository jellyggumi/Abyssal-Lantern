# castle-war — 새총 launcher & 성 (castle keep) art

Durable record of the 2026-08-10 presentation pass that replaced the abstract
launch portal with a **slingshot** and the abstract crystal core with a
**castle keep that visibly crumbles through three damage stages**.

Live spec while the cycle is open: `_workspace/current/design/siege-art-spec.md`.

## Why

Two abstractions were costing the game legibility:

- The launch point was a glowing **portal ring**. It never explained why
  dragging *backwards* fires a soldier *forwards*. A slingshot states the whole
  control grammar in its silhouette.
- The base you defend was a **crystal core**. "How close am I to losing" needed
  a number. A keep that loses its roofs, then its battlements, then its wall
  front answers it at a glance — and the most dramatic moment of the match had
  previously had the *least* motion, because the core froze on a still the
  instant it was first hit.

## Generation path (and why it is not the one CLAUDE.md §3 names)

§3 nominates Codex CLI for 2D sprites. It was unavailable:

| Tool | Result |
|---|---|
| `codex exec` | usage limit exhausted, resets **Aug 16 2026** |
| `gti` (god-tibo-imagen) | HTTP 429 — proxies the *same* Codex backend, so the quota is shared |
| `ppgen` (perfectpixel) | installed build advertises only `gemini\|openrouter\|fal\|byteplus`; no key present |

Fallback used: **Higgsfield CLI**, 6.5 credits total — `flux_2` for the
slingshot and the intact keep, `flux_kontext` for the damaged stages.

The load-bearing choice: stages 1 and 2 are **image-conditioned edits of stage
0**, not independent renders. That is what makes them the same castle at three
points of destruction rather than three different castles.

## Pipeline

`_workspace/current/design/concept/gen_siege_art.py` (deterministic — re-running
reproduces byte-identical frames):

1. **Key** — border flood-fill, not colour distance (the renders carry a
   vignette that a distance key smears into a halo), plus a second pass for
   enclosed pockets such as the gap between the slingshot's prongs.
2. **Reconstruct the banner** — the model painted the pennant cloth *in the key
   colour* (measured RGB distance 2–4 from the background on all three stages).
   Nothing can separate pixels that are literally background-coloured;
   threshold, enclosure and morphological closure were each tried and failed.
   The cloth is re-drawn as geometry composited *under* the keyed art so the
   render's own gold trim draws over it. Recorded as a composite, not a recovery.
3. **Derive frames** from one keyed still per stage. Per-frame model generation
   drifts identity between frames; deriving guarantees a stable silhouette.

## Shipped

| Resources key | Frames | fps | Content |
|---|---|---|---|
| `Gimmicks/slingshot_anim` | 6 | 8 | band sway + 1px breathe |
| `Gimmicks/castle_keep_s0_anim` | 4 | 5 | banner flutter, confident window glow |
| `Gimmicks/castle_keep_s1_anim` | 4 | 4 | ragged banner, weak embers, settling dust |
| `Gimmicks/castle_keep_s2_anim` | 4 | 4 | no banner, dead windows, heavy dust, slump |

Plus three stills `Gimmicks/castle_keep_s{0,1,2}.png` feeding the damage-state
sprite slots, so the silhouette still changes when the animator is unavailable.

`GimmickFrameAnimator.Retarget()` swaps the keep's loop **without re-attaching**,
preserving one world footprint and the loop phase across a stage change —
re-attaching would re-measure the footprint from s2's smaller ruin silhouette
and visibly shrink the keep's collider.

## Two real bugs this surfaced

1. **`LaunchManager.Update()` clobbered the launch affordance's fitted scale** —
   it assigned the raw breathing pulse to `localScale` every frame, discarding
   the world-size fit from setup. Pre-existing; the old portal art had been
   rendering at native sprite scale the whole time.
2. **Promoted PNGs imported as `textureType: Default`** — copying files in
   without `.meta` let Unity mint default importers, so
   `Resources.LoadAll<Sprite>` returned an **empty array** and the slingshot
   silently degraded to the procedural ring. All 21 metas rewritten as Sprite
   importers with GUIDs preserved. Caught by the test lane before it shipped.

## Verification [OBSERVED 2026-08-10]

- Import probe: 6 / 4 / 4 / 4 sprites per frame folder, three 512×512 stills.
- **285 EditMode tests, 281 pass.** 12 new pins in
  `Assets/Tests/EditMode/SiegeArtResourceTests.cs`, including a pixel-signature
  comparison proving the three keep stages are genuinely different art rather
  than three copies of one image.
- The 4 failures are pre-existing and unrelated — commit `bf491069` (another
  session) changed `DrawTrajectory`'s integration loop from `i=1` to `i=0`,
  invalidating point-count and float-horizon assertions. `DrawTrajectory` is
  byte-identical between `HEAD` and this working tree.
