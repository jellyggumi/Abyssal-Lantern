#!/usr/bin/env python3
"""Procedural FX art pack (deterministic, PIL) for the widened-battlefield update.

Interim art for the exact paths gen_fx_pack.py targets. The AI pipeline
(gen_fx_pack.py FORCE=1) overwrites these when the imagen backend recovers.
Writes Unity .meta (Sprite importer) via the same template+GUID scheme.
"""
import math
import random
import sys
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
from gen_fx_pack import write_meta  # noqa: E402  (meta template reuse)

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "Assets/Resources"


def canvas(w, h):
    return Image.new("RGBA", (w, h), (0, 0, 0, 0))


def soft_blob(draw_img, cx, cy, r, color, blur=0):
    """Filled circle on its own layer, optionally blurred, composited over."""
    layer = canvas(*draw_img.size)
    d = ImageDraw.Draw(layer)
    d.ellipse([cx - r, cy - r, cx + r, cy + r], fill=color)
    if blur > 0:
        layer = layer.filter(ImageFilter.GaussianBlur(blur))
    draw_img.alpha_composite(layer)


def radial_glow(size, inner, outer, stops):
    """Radial gradient sprite: stops = [(t, (r,g,b,a)), ...] sorted by t."""
    im = canvas(size, size)
    px = im.load()
    c = (size - 1) / 2
    for y in range(size):
        for x in range(size):
            d = math.hypot(x - c, y - c) / (size / 2)
            if d >= 1.0:
                continue
            for i in range(len(stops) - 1):
                t0, c0 = stops[i]
                t1, c1 = stops[i + 1]
                if t0 <= d <= t1:
                    f = (d - t0) / max(1e-6, t1 - t0)
                    px[x, y] = tuple(int(c0[k] + (c1[k] - c0[k]) * f) for k in range(4))
                    break
    return im


# ---------------------------------------------------------------- particles
def particle_ember(size=128):
    im = canvas(size, size)
    rng = random.Random(7)
    # Outer orange glow, hot yellow-white core, angular flame-chip silhouette.
    im.alpha_composite(radial_glow(size, 0, size, [
        (0.0, (255, 252, 220, 255)), (0.22, (255, 224, 120, 245)),
        (0.5, (255, 140, 40, 190)), (0.8, (200, 60, 10, 70)), (1.0, (150, 30, 0, 0)),
    ]))
    # Angular chip mask: polygon with jagged vertices.
    mask = Image.new("L", (size, size), 0)
    d = ImageDraw.Draw(mask)
    c = size / 2
    pts = []
    for i in range(9):
        a = i / 9 * math.tau
        r = size * (0.30 + 0.17 * rng.random())
        pts.append((c + r * math.cos(a), c + r * math.sin(a)))
    d.polygon(pts, fill=255)
    mask = mask.filter(ImageFilter.GaussianBlur(size * 0.045))
    r, g, b, a = im.split()
    a = Image.composite(a, Image.new("L", (size, size), 0), mask)
    return Image.merge("RGBA", (r, g, b, a))


def particle_smoke(size=128):
    im = canvas(size, size)
    rng = random.Random(11)
    base = (168, 158, 148)
    dark = (120, 112, 104)
    for _ in range(9):
        cx = size / 2 + rng.uniform(-size * 0.16, size * 0.16)
        cy = size / 2 + rng.uniform(-size * 0.14, size * 0.14)
        r = size * rng.uniform(0.14, 0.26)
        col = dark if cy > size * 0.55 else base
        soft_blob(im, cx, cy, r, col + (150,), blur=size * 0.05)
    soft_blob(im, size / 2, size * 0.44, size * 0.24, (200, 192, 184, 120), blur=size * 0.06)
    return im


def particle_petal(size=128):
    im = canvas(size, size)
    d = ImageDraw.Draw(im)
    c = size / 2
    # Teardrop with a tip notch: two arcs meeting at the tip.
    body = [(c, size * 0.10)]
    for t in range(1, 32):
        a = t / 32 * math.pi
        w = math.sin(a) * size * 0.30
        y = size * 0.10 + (size * 0.74) * (t / 32)
        body.append((c + w, y))
    for t in range(32, 0, -1):
        a = t / 32 * math.pi
        w = math.sin(a) * size * 0.30
        y = size * 0.10 + (size * 0.74) * (t / 32)
        body.append((c - w, y))
    d.polygon(body, fill=(255, 176, 205, 235))
    # Notch at the tip + darker base shading.
    d.polygon([(c - size * 0.05, size * 0.06), (c + size * 0.05, size * 0.06),
               (c, size * 0.16)], fill=(0, 0, 0, 0))
    grad = canvas(size, size)
    soft_blob(grad, c, size * 0.80, size * 0.24, (222, 120, 160, 160), blur=size * 0.08)
    r, g, b, a = im.split()
    grad.putalpha(Image.composite(grad.getchannel("A"), Image.new("L", (size, size), 0), a))
    im.alpha_composite(grad)
    return im


# ---------------------------------------------------------------- fx_spawn
def fx_spawn_frame(i, size=512):
    """4-frame arcane materialize flash: runes ring grows, flash peaks at 2, fades."""
    im = canvas(size, size)
    rng = random.Random(100 + i)
    c = size / 2
    t = i / 3
    ring_r = size * (0.16 + 0.26 * t)
    ring_alpha = [140, 220, 255, 90][i]
    core = [(0.5, 90), (0.8, 170), (1.0, 255), (0.35, 60)][i]

    # Glow core.
    im.alpha_composite(radial_glow(size, 0, size, [
        (0.0, (235, 250, 255, core[1])), (0.3, (150, 220, 255, int(core[1] * 0.7))),
        (0.7, (80, 160, 255, int(core[1] * 0.25))), (1.0, (40, 90, 220, 0)),
    ]))
    # Rune ring: dashes around a circle.
    layer = canvas(size, size)
    d = ImageDraw.Draw(layer)
    n = 14
    for k in range(n):
        a0 = k / n * math.tau + t * 0.8
        for rr, wobble in ((ring_r, 0.0), (ring_r * 0.78, 0.5)):
            ax = c + rr * math.cos(a0 + wobble)
            ay = c + rr * math.sin(a0 + wobble) * 0.55  # squashed ellipse (floor ring)
            s = size * (0.018 + 0.012 * rng.random())
            d.rectangle([ax - s, ay - s, ax + s, ay + s], fill=(170, 235, 255, ring_alpha))
    layer = layer.filter(ImageFilter.GaussianBlur(1.2))
    im.alpha_composite(layer)
    # Peak frame: vertical light pillar + cross flare.
    if i == 2:
        layer = canvas(size, size)
        d = ImageDraw.Draw(layer)
        d.polygon([(c - size * 0.045, size * 0.08), (c + size * 0.045, size * 0.08),
                   (c + size * 0.10, c), (c - size * 0.10, c)], fill=(220, 245, 255, 200))
        d.line([(size * 0.14, c), (size * 0.86, c)], fill=(200, 240, 255, 160), width=6)
        layer = layer.filter(ImageFilter.GaussianBlur(3))
        im.alpha_composite(layer)
    # Fade frame: sparse drifting motes.
    if i == 3:
        for _ in range(16):
            soft_blob(im, rng.uniform(size * 0.2, size * 0.8), rng.uniform(size * 0.15, size * 0.7),
                      size * rng.uniform(0.006, 0.016), (190, 235, 255, 170), blur=1)
    return im


# ------------------------------------------------------------- fx columns
def flame_column(i, w=426, h=640, petal=False):
    """Bottom-anchored vertical eruption column, 5 (magma) / 4 (petal) frame arc."""
    im = canvas(w, h)
    rng = random.Random((300 if petal else 200) + i)
    frames = 4 if petal else 5
    t = i / (frames - 1)
    # Envelope: how tall/wide/dense the column is this frame.
    height_f = [0.30, 0.62, 1.0, 0.78, 0.35][i] if not petal else [0.30, 0.75, 1.0, 0.45][i]
    density = [26, 60, 100, 62, 24][i] if not petal else [30, 80, 110, 40][i]
    fading = t > 0.6

    if petal:
        cols = [(255, 190, 215), (255, 150, 190), (255, 220, 235), (250, 205, 150)]
    else:
        cols = [(255, 230, 110), (255, 160, 40), (255, 110, 20), (255, 245, 200)]
    smoke = (150, 140, 132)

    cx = w / 2
    top = h * (1 - 0.92 * height_f)
    # Core column glow (skip for dissipating last frames).
    if not fading or petal:
        layer = canvas(w, h)
        d = ImageDraw.Draw(layer)
        core_w = w * (0.16 + 0.10 * height_f)
        d.polygon([(cx - core_w * 0.5, h), (cx + core_w * 0.5, h),
                   (cx + core_w * 0.9, top + (h - top) * 0.25), (cx, top),
                   (cx - core_w * 0.9, top + (h - top) * 0.25)],
                  fill=cols[0] + (150 if petal else 190,))
        layer = layer.filter(ImageFilter.GaussianBlur(w * 0.03))
        im.alpha_composite(layer)
    # Blobs: petals / flame lobes / smoke puffs along the column.
    for _ in range(density):
        y = rng.uniform(top, h * 0.98)
        yf = (y - top) / max(1.0, h - top)  # 0 at top, 1 at base
        spread = w * (0.06 + 0.26 * (1 - yf) * (0.5 + 0.5 * height_f))
        x = cx + rng.uniform(-spread, spread)
        if fading and not petal and rng.random() < 0.75:
            col, alpha, r = smoke, 130, w * rng.uniform(0.035, 0.09)
            soft_blob(im, x, y, r, col + (alpha,), blur=w * 0.02)
            continue
        col = cols[rng.randrange(len(cols))]
        if petal:
            # Little rotated petal ellipse.
            r = w * rng.uniform(0.018, 0.045)
            layer = canvas(w, h)
            d = ImageDraw.Draw(layer)
            d.ellipse([x - r, y - r * 0.55, x + r, y + r * 0.55], fill=col + (225,))
            layer = layer.rotate(rng.uniform(0, 360), center=(x, y), resample=Image.BICUBIC)
            im.alpha_composite(layer)
        else:
            r = w * rng.uniform(0.03, 0.085) * (0.7 + 0.6 * (1 - yf))
            soft_blob(im, x, y, r, col + (215,), blur=w * 0.012)
    # Ejecta at the crown on the peak frame.
    if abs(height_f - 1.0) < 1e-3:
        for _ in range(10):
            x = cx + rng.uniform(-w * 0.38, w * 0.38)
            y = top + rng.uniform(-h * 0.02, h * 0.10)
            r = w * rng.uniform(0.012, 0.028)
            col = cols[3] if not petal else (255, 235, 160)
            soft_blob(im, x, y, r, col + (235,), blur=1)
    # Base flash.
    if not fading:
        soft_blob(im, cx, h * 0.97, w * 0.20, (cols[3] if not petal else (255, 240, 245)) + (200,),
                  blur=w * 0.05)
    return im


# ---------------------------------------------------------------- vents
def vent_base(magma=True, size=512):
    im = canvas(size, size)
    rng = random.Random(41 if magma else 43)
    d = ImageDraw.Draw(im)
    cx, base_y = size / 2, size * 0.92
    dome_w, dome_h = size * 0.44, size * 0.34
    rock = (96, 88, 84) if magma else (96, 128, 72)
    rock_hi = (128, 118, 112) if magma else (122, 158, 92)
    mouth_glow = (255, 120, 30) if magma else (255, 150, 195)
    # Dome silhouette (lumpy: overlapping circles).
    for k in range(10):
        a = (k / 9 - 0.5) * math.pi
        x = cx + math.sin(a) * dome_w * 0.8
        y = base_y - math.cos(a) * dome_h * 0.55
        r = size * rng.uniform(0.10, 0.16)
        col = rock_hi if y < base_y - dome_h * 0.35 else rock
        soft_blob(im, x, y, r, col + (255,), blur=2)
    d = ImageDraw.Draw(im)
    d.ellipse([cx - dome_w, base_y - dome_h * 0.5, cx + dome_w, base_y + dome_h * 0.4],
              fill=rock + (255,))
    # Mouth: dark crater with glowing rim on top of the dome.
    mouth_y = base_y - dome_h * 0.72
    d.ellipse([cx - size * 0.15, mouth_y - size * 0.055, cx + size * 0.15, mouth_y + size * 0.055],
              fill=(30, 22, 20, 255))
    glow = canvas(size, size)
    soft_blob(glow, cx, mouth_y, size * 0.13, mouth_glow + (190,), blur=size * 0.03)
    im.alpha_composite(glow)
    if magma:
        # Lava cracks radiating down the dome.
        layer = canvas(size, size)
        dd = ImageDraw.Draw(layer)
        for k in range(6):
            a = math.pi * (0.15 + 0.7 * k / 5)
            x0, y0 = cx + math.cos(a) * size * 0.10, mouth_y + size * 0.04
            x1 = cx + math.cos(a) * dome_w * 0.85
            y1 = base_y - rng.uniform(0, dome_h * 0.2)
            mx = (x0 + x1) / 2 + rng.uniform(-8, 8)
            dd.line([(x0, y0), (mx, (y0 + y1) / 2), (x1, y1)], fill=(255, 140, 40, 230), width=5)
        layer = layer.filter(ImageFilter.GaussianBlur(1.4))
        im.alpha_composite(layer)
    else:
        # Blossom ring around the mouth.
        for k in range(9):
            a = k / 9 * math.tau
            x = cx + math.cos(a) * size * 0.20
            y = mouth_y + math.sin(a) * size * 0.085
            for pk in range(5):
                pa = pk / 5 * math.tau
                soft_blob(im, x + math.cos(pa) * size * 0.018, y + math.sin(pa) * size * 0.018,
                          size * 0.016, (255, 170, 200, 255), blur=1)
            soft_blob(im, x, y, size * 0.010, (255, 235, 120, 255))
    # Grass tufts at the foot.
    foot = (76, 120, 56) if not magma else (70, 62, 58)
    for k in range(14):
        x = cx + rng.uniform(-dome_w, dome_w)
        soft_blob(im, x, base_y + size * 0.015, size * rng.uniform(0.02, 0.04), foot + (255,), blur=1)
    return im


OUTPUTS = []
for i in range(4):
    OUTPUTS.append((f"Effects/fx_spawn/fx_spawn_{i:03d}.png", lambda i=i: fx_spawn_frame(i)))
for i in range(5):
    OUTPUTS.append((f"Effects/fx_eruption/fx_eruption_{i:03d}.png", lambda i=i: flame_column(i)))
for i in range(4):
    OUTPUTS.append((f"Effects/fx_petals/fx_petals_{i:03d}.png", lambda i=i: flame_column(i, petal=True)))
OUTPUTS += [
    ("Effects/particles/particle_ember.png", particle_ember),
    ("Effects/particles/particle_smoke.png", particle_smoke),
    ("Effects/particles/particle_petal.png", particle_petal),
    ("Gimmicks/gimmick_vent_magma.png", lambda: vent_base(True)),
    ("Gimmicks/gimmick_vent_petal.png", lambda: vent_base(False)),
]


def main():
    for rel, fn in OUTPUTS:
        dst = RES / rel
        if dst.exists() and "--force" not in sys.argv:
            print(f"skip {rel} (exists)")
            write_meta(dst)
            continue
        dst.parent.mkdir(parents=True, exist_ok=True)
        fn().save(dst)
        write_meta(dst)
        print(f"ok   {rel}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
