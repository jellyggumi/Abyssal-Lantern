#!/usr/bin/env python3
"""Border-flood white-key for Assets/Sprites object/effect/character sprites.

The AI-generated object sprites (blocks, arrow, explosion, cannonball, unit
portraits) ship as RGB on a near-white studio background with NO alpha channel,
so Unity renders them as opaque white boxes. This adds a clean alpha channel by
flood-keying the background that is *connected to the image border*, which
preserves interior white highlights (cracks, fletching, glints).

- Overwrites PNG bytes IN PLACE (keeps the .meta GUID / Unity references).
- Same width/height as the source (no trim/resize -> prefab placement intact).
- Background.png is intentionally excluded (it is an opaque scene backdrop).

Usage:
    python3 tools/key_object_sprites.py                # all eligible sprites
    python3 tools/key_object_sprites.py arrow explosion  # subset by stem
    python3 tools/key_object_sprites.py --force        # re-key even if alpha exists
"""
import sys
import glob
import os
import numpy as np
from PIL import Image, ImageFilter
from scipy import ndimage

SRC_DIR = os.path.join(os.path.dirname(__file__), "..", "Assets", "Sprites")
EXCLUDE = {"Background"}            # opaque scene backdrop, leave as-is
WHITE_MIN = int(os.environ.get("WHITE_MIN", "232"))  # per-channel floor for "white-ish"
SAT_MAX = 16                       # max(c)-min(c) ceiling (low saturation)
FEATHER = 1.2                      # gaussian blur radius on the alpha edge
DESPILL = 0.85                     # multiply rim RGB toward neutral to kill halo


def white_mask(rgb: np.ndarray) -> np.ndarray:
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mn = np.minimum(np.minimum(r, g), b)
    mx = np.maximum(np.maximum(r, g), b)
    return (mn >= WHITE_MIN) & ((mx - mn) <= SAT_MAX)


def border_connected(mask: np.ndarray) -> np.ndarray:
    """Components of `mask` that touch any image edge = background."""
    lbl, n = ndimage.label(mask)
    if n == 0:
        return np.zeros_like(mask, dtype=bool)
    edge = set(lbl[0, :]) | set(lbl[-1, :]) | set(lbl[:, 0]) | set(lbl[:, -1])
    edge.discard(0)
    if not edge:
        return np.zeros_like(mask, dtype=bool)
    return np.isin(lbl, list(edge))


def key_one(path: str, force: bool) -> str:
    im = Image.open(path)
    if im.mode == "RGBA" and not force:
        a = np.asarray(im.split()[3])
        if (a < 16).mean() > 0.02:        # already has real transparency
            return "skip(has-alpha)"
    rgb = np.asarray(im.convert("RGB"), dtype=np.uint8)
    bg = border_connected(white_mask(rgb))
    if bg.mean() < 0.01:
        return "skip(no-bg-found)"

    alpha = np.where(bg, 0, 255).astype(np.uint8)
    a_img = Image.fromarray(alpha, "L").filter(ImageFilter.GaussianBlur(FEATHER))
    alpha = np.asarray(a_img, dtype=np.uint8)

    # Despill: pixels that became semi-transparent get their bright white rim
    # pulled down so no halo bleeds onto the atlas.
    rim = (alpha > 8) & (alpha < 230)
    out = rgb.astype(np.float32)
    out[rim] *= DESPILL
    out = np.clip(out, 0, 255).astype(np.uint8)

    rgba = np.dstack([out, alpha])
    Image.fromarray(rgba, "RGBA").save(path)
    opa = (alpha > 200).mean()
    return f"keyed opaque={opa:.2f} transparent={(alpha < 30).mean():.2f}"


def main() -> None:
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    force = "--force" in sys.argv[1:]
    files = sorted(glob.glob(os.path.join(SRC_DIR, "*.png")))
    for f in files:
        stem = os.path.splitext(os.path.basename(f))[0]
        if stem in EXCLUDE:
            print(f"  - exclude {stem}")
            continue
        if args and stem not in args:
            continue
        print(f"  {stem:26} {key_one(f, force)}")


if __name__ == "__main__":
    main()
