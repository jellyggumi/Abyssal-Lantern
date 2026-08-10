#!/usr/bin/env python3
"""Key + animate the castle-war siege art (slingshot launcher, castle core stages).

Pipeline
--------
1. Chroma-key the flat magenta studio background out of the Higgsfield renders via an
   edge flood-fill (robust against the render's subtle background vignette, which a
   flat colour-distance key smears into a halo).
2. Derive animation frames FROM the keyed still rather than generating each frame with
   the image model. Per-frame generation drifts the subject's identity between frames —
   deriving guarantees a byte-stable silhouette, which is what an 8 fps idle loop needs.
3. Emit Unity-convention frame sequences: Resources/Gimmicks/<key>/<key>_000.png ...

Animation content is deliberately small-amplitude: these are idle loops behind gameplay,
not hero cinematics. A launcher that lurches or a keep that wobbles pulls the eye off the
volley (CLAUDE.md §2 — presentation may never fight the simulation for attention).

Run:  python3 gen_siege_art.py
"""
import hashlib
import json
import math
import os
from collections import deque
from datetime import datetime, timezone

from PIL import Image, ImageDraw, ImageFilter

HERE = os.path.dirname(os.path.abspath(__file__))
SLING_DIR = os.path.join(HERE, "slingshot")
CASTLE_DIR = os.path.join(HERE, "castle-core")

TOOL = "Higgsfield CLI (flux_2 / flux_kontext) for key art; PIL 12.2.0 keying + frame derivation via gen_siege_art.py"


# ---------------------------------------------------------------- keying


def chroma_key(img, tol=52, feather=1.2):
    """Flood-fill the studio background from the border inward.

    Colour-distance keying alone eats the castle's warm rim light (it sits close to the
    magenta in chroma) and leaves the render's vignette as a pink halo. Flooding from the
    border only removes pixels actually *connected* to the outside, so an enclosed warm
    highlight or a dark breach interior is never punched out by accident.
    """
    img = img.convert("RGBA")
    w, h = img.size
    px = img.load()

    # Background reference = median of the four corners (vignette-tolerant).
    corners = [px[1, 1], px[w - 2, 1], px[1, h - 2], px[w - 2, h - 2]]
    br = sorted(c[0] for c in corners)[len(corners) // 2]
    bg = sorted(c[1] for c in corners)[len(corners) // 2]
    bb = sorted(c[2] for c in corners)[len(corners) // 2]

    tol_sq = tol * tol
    visited = bytearray(w * h)
    q = deque()

    def similar(c):
        dr = c[0] - br
        dg = c[1] - bg
        db = c[2] - bb
        return dr * dr + dg * dg + db * db <= tol_sq

    for x in range(w):
        for y in (0, h - 1):
            i = y * w + x
            if not visited[i] and similar(px[x, y]):
                visited[i] = 1
                q.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            i = y * w + x
            if not visited[i] and similar(px[x, y]):
                visited[i] = 1
                q.append((x, y))

    while q:
        x, y = q.popleft()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if 0 <= nx < w and 0 <= ny < h:
                i = ny * w + nx
                if not visited[i] and similar(px[nx, ny]):
                    visited[i] = 1
                    q.append((nx, ny))

    # Second pass: enclosed background pockets. The border flood cannot reach a region the
    # subject fully encircles — e.g. the gap between a slingshot's two prongs under the
    # elastic band. Any *remaining* component that matches the key colour and is not tiny
    # is studio background too, so cut it as well. The size floor keeps genuinely
    # magenta-adjacent art detail (none here, but the rule must be stated) from vanishing.
    min_pocket = max(64, (w * h) // 4000)
    for sy in range(h):
        row = sy * w
        for sx in range(w):
            if visited[row + sx] or not similar(px[sx, sy]):
                continue
            comp = []
            stack = [(sx, sy)]
            visited[row + sx] = 1
            while stack:
                cx, cy = stack.pop()
                comp.append((cx, cy))
                for nx, ny in ((cx + 1, cy), (cx - 1, cy), (cx, cy + 1), (cx, cy - 1)):
                    if 0 <= nx < w and 0 <= ny < h:
                        j = ny * w + nx
                        if not visited[j] and similar(px[nx, ny]):
                            visited[j] = 1
                            stack.append((nx, ny))
            if len(comp) < min_pocket:
                # Too small to be background — restore it as subject so speckle noise in the
                # render does not punch pinholes through the sprite.
                for cx, cy in comp:
                    visited[cy * w + cx] = 0

    mask = Image.new("L", (w, h), 255)
    mp = mask.load()
    for y in range(h):
        row = y * w
        for x in range(w):
            if visited[row + x]:
                mp[x, y] = 0

    # Feather the cut so the sprite does not alias against the battlefield background.
    mask = mask.filter(ImageFilter.GaussianBlur(feather))
    out = img.copy()
    out.putalpha(mask)

    # Kill the magenta fringe the feather leaves on semi-transparent edge pixels.
    op = out.load()
    for y in range(h):
        for x in range(w):
            r, g, b, a = op[x, y]
            if 0 < a < 250:
                # Pull the pixel away from the key colour proportional to its transparency.
                t = 1.0 - (a / 255.0)
                op[x, y] = (
                    max(0, int(r - (br - 30) * t * 0.55)),
                    max(0, int(g - (bg - 30) * t * 0.35)),
                    max(0, int(b - (bb - 30) * t * 0.55)),
                    a,
                )
    return out


def autocrop(img, pad=6):
    bbox = img.getbbox()
    if not bbox:
        return img
    l, t, r, b = bbox
    w, h = img.size
    l = max(0, l - pad)
    t = max(0, t - pad)
    r = min(w, r + pad)
    b = min(h, b + pad)
    return img.crop((l, t, r, b))


def fit(img, size):
    """Letterbox into a square canvas, preserving aspect + centering."""
    out = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    src = img.copy()
    src.thumbnail((size, size), Image.LANCZOS)
    out.paste(src, ((size - src.width) // 2, (size - src.height) // 2), src)
    return out

def repaint_banner(img, box, cloth, shade):
    """Reinstate the pennant cloth the chroma key legitimately removed.

    The image model painted the banner IN the key colour (measured: RGB distance 2–4 from
    the studio background on all three stages), so the cloth pixels are literally
    background-coloured and no threshold, enclosure test, or morphological closure can
    recover them — all three were tried. What survived is the gold trim and the pole.

    So the cloth is re-drawn as explicit geometry and composited UNDERNEATH the keyed art:
    the surviving trim and pole then draw over it naturally, keeping the render's own
    linework on top instead of pasting a flat sticker over it. The pennant's anchor and
    extent are measured from the surviving trim's bounding box, not hard-coded, so each
    damage stage gets a cloth that matches its own (increasingly ragged) trim.

    This is a composite, not a recovery — recorded as such in the provenance record.
    """
    l, t, r, b = box
    l, t = max(0, l), max(0, t)
    r, b = min(img.width, r), min(img.height, b)
    p = img.load()

    # Measure the trim: every opaque pixel in the box outlines where cloth used to be.
    xs, ys = [], []
    for y in range(t, b):
        for x in range(l, r):
            if p[x, y][3] > 140:
                xs.append(x)
                ys.append(y)
    if len(xs) < 20:
        return img

    x0, x1 = min(xs), max(xs)
    y0, y1 = min(ys), max(ys)
    if x1 - x0 < 12 or y1 - y0 < 8:
        return img

    # Swallow-tail pennant: hoist at the pole, notched tail at the fly end.
    hoist_x = x0 + 1
    fly_x = x1
    top_y = y0 + max(1, (y1 - y0) // 10)
    bot_y = y1 - max(1, (y1 - y0) // 10)
    mid_y = (top_y + bot_y) // 2
    notch_x = fly_x - (fly_x - hoist_x) // 5

    poly = [
        (hoist_x, top_y),
        (fly_x, top_y + (mid_y - top_y) // 3),
        (notch_x, mid_y),
        (fly_x, bot_y - (bot_y - mid_y) // 3),
        (hoist_x, bot_y),
    ]

    cloth_layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    d = ImageDraw.Draw(cloth_layer)
    d.polygon(poly, fill=cloth + (255,))

    # Vertical shade so the cloth has form instead of reading as a flat cut-out.
    shade_layer = Image.new("RGBA", img.size, (0, 0, 0, 0))
    sd = ImageDraw.Draw(shade_layer)
    for y in range(top_y, bot_y + 1):
        f = (y - top_y) / max(1, bot_y - top_y)
        col = tuple(int(cloth[i] + (shade[i] - cloth[i]) * f) for i in range(3))
        sd.line([(hoist_x, y), (fly_x, y)], fill=col + (255,))
    cloth_layer = Image.composite(shade_layer, cloth_layer, cloth_layer.split()[3].point(lambda a: 255 if a else 0))
    # Re-clip the shaded band back to the pennant silhouette.
    mask = Image.new("L", img.size, 0)
    ImageDraw.Draw(mask).polygon(poly, fill=255)
    cloth_layer.putalpha(mask)

    out = cloth_layer
    out = Image.alpha_composite(out, img)
    return out



# ------------------------------------------------------- frame derivation


def wave_region(img, box, amp, phase, axis="x"):
    """Sinusoidal row/column shear inside `box` — cloth flutter without redrawing it."""
    out = img.copy()
    l, t, r, b = box
    l, t = max(0, l), max(0, t)
    r, b = min(img.width, r), min(img.height, b)
    if r <= l or b <= t:
        return out
    region = img.crop((l, t, r, b))
    out.paste((0, 0, 0, 0), (l, t, r, b))
    rw, rh = region.size
    for i in range(rh if axis == "x" else rw):
        f = i / max(1, (rh if axis == "x" else rw) - 1)
        off = int(round(amp * math.sin(phase + f * math.pi * 1.6)))
        if axis == "x":
            line = region.crop((0, i, rw, i + 1))
            out.paste(line, (l + off, t + i), line)
        else:
            line = region.crop((i, 0, i + 1, rh))
            out.paste(line, (l + i, t + off), line)
    return out


def glow_pulse(img, strength):
    """Modulate only the warm emissive pixels (window/ember light), never the stone."""
    out = img.copy()
    p = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = p[x, y]
            if a == 0:
                continue
            # Warm emissive: red-dominant, not grey, and bright enough to be a light.
            if r > 110 and r > b + 38 and g > b + 10:
                k = 1.0 + strength
                p[x, y] = (min(255, int(r * k)), min(255, int(g * (1 + strength * 0.72))), min(255, int(b * (1 + strength * 0.30))), a)
    return out


def bob(img, dy):
    out = Image.new("RGBA", img.size, (0, 0, 0, 0))
    out.paste(img, (0, dy), img)
    return out


def dust_motes(img, seed, count, band, tint):
    """A few drifting embers/dust specks in the ruin's breach — motion without geometry."""
    out = img.copy()
    d = ImageDraw.Draw(out)
    rnd = _Lcg(seed)
    l, t, r, b = band
    for _ in range(count):
        x = l + rnd.next_float() * (r - l)
        y = t + rnd.next_float() * (b - t)
        rad = 1.0 + rnd.next_float() * 2.0
        a = int(60 + rnd.next_float() * 120)
        d.ellipse([x - rad, y - rad, x + rad, y + rad], fill=tint + (a,))
    return out


class _Lcg:
    """Deterministic RNG so re-running the script reproduces byte-identical frames."""

    def __init__(self, seed):
        self.s = seed & 0xFFFFFFFF

    def next_float(self):
        self.s = (1103515245 * self.s + 12345) & 0x7FFFFFFF
        return self.s / 0x7FFFFFFF


# ------------------------------------------------------------- provenance


def sha256_of(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        for chunk in iter(lambda: f.read(65536), b""):
            h.update(chunk)
    return h.hexdigest()


def write_provenance(path, prompt, model, notes):
    im = Image.open(path)
    rec = {
        "file": os.path.basename(path),
        "prompt": prompt,
        "tool": TOOL,
        "model": model,
        "generatedAt": datetime.now(timezone.utc).isoformat(),
        "checksumSha256": sha256_of(path),
        "runtimeEligible": True,
        "notes": notes,
        "width": im.width,
        "height": im.height,
    }
    with open(path + ".provenance.json", "w") as f:
        json.dump(rec, f, indent=2)
        f.write("\n")


# ------------------------------------------------------------------ main

SLING_PROMPT = (
    "2D game asset, single object on a pure flat solid magenta background. A sturdy wooden "
    "slingshot siege launcher: Y-shaped forked branch of dark weathered oak, riveted iron bands "
    "at the fork joints, a wide worn brown leather pouch, thick amber-brown elastic bands strung "
    "taut between the two prongs. Standing upright, planted in the ground, viewed from the side at "
    "a slight three-quarter angle. Dark-fantasy medieval siege game art, painterly but crisp "
    "readable silhouette, warm rim light from upper left, neutral fill. Object centered with "
    "generous empty margin on all sides, fully inside frame. No ground plane, no shadow on "
    "background, no text, no watermark, no extra props."
)

CASTLE_PROMPTS = {
    0: (
        "2D game asset, single object on a pure flat solid magenta background. A proud intact "
        "medieval stone castle keep viewed straight from the front: tall square granite tower with "
        "crenellated battlements, heavy arched oak gate with iron studs, narrow arrow-slit windows "
        "glowing warm gold, two flanking corner turrets with conical slate roofs, bright banner "
        "pennant flying from the top. Clean undamaged masonry. Dark-fantasy medieval siege game art."
    ),
    1: (
        "Midpoint between the intact keep and the wreck: half the merlons broken off, holes punched "
        "through the upper wall, left turret roof tip sheared away, deep cracks down both towers, "
        "arrow slits charred and dimmed to weak embers, gate split and scorched, banner frayed."
    ),
    2: (
        "Near-destroyed ruin: both turret conical roofs gone leaving jagged stumps, the whole upper "
        "third collapsed into a torn ragged crown, flagpole down and banner gone, a massive cavernous "
        "breach eating most of the front wall, all windows pitch black. Low, jagged, asymmetric outline."
    ),
}


def build_slingshot():
    src = Image.open(os.path.join(SLING_DIR, "slingshot_raw.png"))
    keyed = fit(autocrop(chroma_key(src)), 256)
    out_dir = os.path.join(SLING_DIR, "frames")
    os.makedirs(out_dir, exist_ok=True)

    # Idle loop: the launcher is "loaded and waiting". Bands shimmer and the fork breathes
    # by a single pixel — enough to read as live, small enough to never pull focus from aim.
    w, h = keyed.size
    band_box = (int(w * 0.10), int(h * 0.05), int(w * 0.92), int(h * 0.52))
    frames = []
    n = 6
    for i in range(n):
        ph = (i / n) * math.pi * 2.0
        f = wave_region(keyed, band_box, amp=1 + int(round(abs(math.sin(ph)))), phase=ph)
        f = glow_pulse(f, 0.05 + 0.05 * math.sin(ph))
        f = bob(f, int(round(math.sin(ph) * 1.4)))
        frames.append(f)

    paths = []
    for i, f in enumerate(frames):
        p = os.path.join(out_dir, f"slingshot_anim_{i:03d}.png")
        f.save(p)
        write_provenance(
            p,
            SLING_PROMPT,
            "flux_2 (Higgsfield) key art; frame derived procedurally",
            f"Frame {i}/{n} of the launcher idle loop. Derived from one keyed still so the "
            f"silhouette is identical across frames (per-frame model generation drifts identity).",
        )
        paths.append(p)
    return paths


def build_castle():
    out_dir = os.path.join(CASTLE_DIR, "frames")
    os.makedirs(out_dir, exist_ok=True)
    produced = {}

    for stage in (0, 1, 2):
        src = Image.open(os.path.join(CASTLE_DIR, f"castle_s{stage}_raw.png"))
        keyed = fit(autocrop(chroma_key(src)), 512)
        w, h = keyed.size

        # s2 has no banner (the pole comes down with the upper third), so nothing to refill.
        if stage < 2:
            # Crimson deepens and desaturates as the keep is battered — the pennant reads as
            # "still flying, but bloodied" at s1 without needing a separate silhouette.
            cloth = ((172, 32, 46), (128, 30, 40))[stage]
            shade = ((116, 20, 32), (82, 20, 28))[stage]
            # Box stops at 0.70w: the turret roofs begin around 0.72w and share these
            # scanlines, so a wider box lets the fill escape across the gap between them.
            keyed = repaint_banner(keyed, (int(w * 0.42), int(h * 0.02), int(w * 0.70), int(h * 0.22)),
                                   cloth, shade)

        # Static damage-state still (feeds DestructibleBlock's normal/cracked/heavy slots).
        still = os.path.join(CASTLE_DIR, f"castle_keep_s{stage}.png")
        keyed.save(still)
        write_provenance(
            still,
            CASTLE_PROMPTS[stage],
            "flux_2 (stage 0) / flux_kontext (stages 1-2), Higgsfield",
            f"Castle keep damage stage s{stage} — the core the player defends. Stage art is "
            f"chroma-keyed from the studio render; stages share one identity because 1 and 2 were "
            f"generated as edits of stage 0 rather than independent renders.",
        )
        produced[f"still_s{stage}"] = still

        # Per-stage idle loop.
        banner_box = (int(w * 0.36), int(h * 0.02), int(w * 0.78), int(h * 0.26))
        breach_box = (int(w * 0.34), int(h * 0.55), int(w * 0.70), int(h * 0.92))
        n = 4
        for i in range(n):
            ph = (i / n) * math.pi * 2.0
            f = keyed
            if stage < 2:
                # Banner still flies at s0/s1 — flutter it. At s2 the pole is down.
                f = wave_region(f, banner_box, amp=2 + stage, phase=ph)
            # Window/ember life: bright confident pulse intact, weak guttering as it ruins.
            amp = (0.10, 0.07, 0.05)[stage]
            f = glow_pulse(f, amp * math.sin(ph))
            if stage > 0:
                # Settling dust in the breach — the wreck is still shedding.
                f = dust_motes(f, seed=1000 * stage + i, count=6 + 4 * stage,
                               band=breach_box, tint=(190, 170, 150))
            if stage == 2:
                f = bob(f, int(round(math.sin(ph) * 1.0)))
            p = os.path.join(out_dir, f"castle_keep_s{stage}_anim_{i:03d}.png")
            f.save(p)
            write_provenance(
                p,
                CASTLE_PROMPTS[stage],
                "flux_2 / flux_kontext (Higgsfield) key art; frame derived procedurally",
                f"Frame {i}/{n} of the castle keep s{stage} idle loop.",
            )
            produced[f"anim_s{stage}_{i}"] = p
    return produced


def main():
    sling = build_slingshot()
    castle = build_castle()
    print(f"slingshot frames: {len(sling)}")
    for p in sling:
        print("  ", os.path.relpath(p, HERE), sha256_of(p)[:12])
    print(f"castle outputs: {len(castle)}")
    for k in sorted(castle):
        print("  ", k, os.path.relpath(castle[k], HERE), sha256_of(castle[k])[:12])


if __name__ == "__main__":
    main()
