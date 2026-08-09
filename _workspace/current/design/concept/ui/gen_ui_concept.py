#!/usr/bin/env python3
"""Procedural draft UI concept art for castle-war (Abyssal-Surge).

Generates faction pennant banners (blue / red) and a siege war-bar frame as
transparent PNGs, purely with PIL primitives -- no external image assets.
Draft-quality concept art only; NOT wired into Assets/, NOT runtime-eligible.
"""
import hashlib
import json
import math
import os
from datetime import datetime, timezone

from PIL import Image, ImageDraw

OUT_DIR = os.path.join(
    os.path.dirname(os.path.abspath(__file__)), "out"
)
os.makedirs(OUT_DIR, exist_ok=True)

TOOL = "PIL (Pillow) 12.2.0 procedural draw, script gen_ui_concept.py"


def lerp(a, b, t):
    return a + (b - a) * t


def draw_banner(path, base_color, shadow_color, trim_color, label):
    """Vertical siege pennant: gradient cloth, swallow-tail cut, crest disc, rope trim."""
    w, h = 256, 512
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    pole_x = 34
    cloth_left = pole_x + 14
    cloth_right = w - 18
    cloth_top = 26
    cloth_bottom = h - 96
    tail_depth = 64

    # Cloth body with a soft vertical gradient + swallow-tail bottom cut.
    cloth_w = cloth_right - cloth_left
    cloth_h = cloth_bottom - cloth_top
    for y in range(cloth_h):
        t = y / max(1, cloth_h - 1)
        r = int(lerp(base_color[0] * 1.25, base_color[0] * 0.55, t))
        g = int(lerp(base_color[1] * 1.25, base_color[1] * 0.55, t))
        b = int(lerp(base_color[2] * 1.25, base_color[2] * 0.55, t))
        r, g, b = (min(255, r), min(255, g), min(255, b))
        draw.line(
            [(cloth_left, cloth_top + y), (cloth_right, cloth_top + y)],
            fill=(r, g, b, 255),
        )

    tail_poly = [
        (cloth_left, cloth_bottom),
        (cloth_left, cloth_bottom + tail_depth),
        ((cloth_left + cloth_right) / 2, cloth_bottom + tail_depth * 0.35),
        (cloth_right, cloth_bottom + tail_depth),
        (cloth_right, cloth_bottom),
    ]
    draw.polygon(tail_poly, fill=(base_color[0], base_color[1], base_color[2], 255))

    # Mask out the swallow-tail notch (transparent V cut).
    notch = [
        ((cloth_left + cloth_right) / 2, cloth_bottom + tail_depth * 0.35),
        (cloth_left + cloth_w * 0.42, cloth_bottom + tail_depth),
        (cloth_left + cloth_w * 0.58, cloth_bottom + tail_depth),
    ]
    draw.polygon(notch, fill=(0, 0, 0, 0))
    # Clear anything below the tail bounding box entirely.
    draw.rectangle([0, cloth_bottom + tail_depth, w, h], fill=(0, 0, 0, 0))

    # Inner drop-shadow edge for cloth depth.
    draw.line([(cloth_left, cloth_top), (cloth_left, cloth_bottom)], fill=(*shadow_color, 160), width=3)

    # Rope/trim border along the pole edge.
    for i in range(0, cloth_h, 10):
        yy = cloth_top + i
        draw.ellipse([cloth_left - 6, yy - 3, cloth_left + 2, yy + 3], fill=(*trim_color, 230))

    # Crest disc (simple castle silhouette) centered in the upper cloth.
    cx, cy, cr = (cloth_left + cloth_right) / 2, cloth_top + 110, 62
    draw.ellipse([cx - cr, cy - cr, cx + cr, cy + cr], fill=(20, 16, 14, 235))
    draw.ellipse([cx - cr, cy - cr, cx + cr, cy + cr], outline=(*trim_color, 255), width=5)
    # Tiny castle glyph: three merlons + gate, drawn in trim color.
    merlon_w, merlon_h = 14, 20
    base_y = cy + 18
    for i, dx in enumerate((-30, -10, 10, 30)):
        draw.rectangle(
            [cx + dx - merlon_w / 2, base_y - merlon_h, cx + dx + merlon_w / 2, base_y],
            fill=(*trim_color, 255),
        )
    draw.rectangle([cx - 34, base_y, cx + 34, base_y + 20], fill=(*trim_color, 255))
    draw.polygon(
        [(cx - 8, base_y + 20), (cx + 8, base_y + 20), (cx, base_y + 4)],
        fill=(20, 16, 14, 255),
    )

    # Wooden pole.
    draw.rectangle([pole_x - 6, 12, pole_x + 6, h - 8], fill=(64, 44, 28, 255))
    draw.ellipse([pole_x - 10, 4, pole_x + 10, 24], fill=(120, 96, 60, 255))

    img.save(path)
    return {"width": w, "height": h, "label": label}


def draw_warbar_frame(path):
    """Ornate horizontal frame for a siege HP/war-progress bar: hollow center for
    the runtime fill sprite to show through, beveled metal edge, end caps."""
    w, h = 640, 96
    img = Image.new("RGBA", (w, h), (0, 0, 0, 0))
    draw = ImageDraw.Draw(img)

    outer = [4, 4, w - 4, h - 4]
    inner_margin = 14
    inner = [outer[0] + inner_margin, outer[1] + inner_margin, outer[2] - inner_margin, outer[3] - inner_margin]

    metal_dark = (36, 32, 30, 255)
    metal_mid = (92, 84, 74, 255)
    metal_light = (168, 152, 120, 255)
    gold = (214, 168, 74, 255)

    # Outer beveled plate.
    draw.rounded_rectangle(outer, radius=18, fill=metal_dark)
    draw.rounded_rectangle(
        [outer[0] + 3, outer[1] + 3, outer[2] - 3, outer[3] - 3], radius=16, outline=metal_mid, width=3
    )
    draw.rounded_rectangle(
        [outer[0] + 6, outer[1] + 6, outer[2] - 6, outer[3] - 6], radius=14, outline=gold, width=2
    )

    # Hollow center cut out so a runtime fill bar can render underneath the frame.
    draw.rounded_rectangle(inner, radius=8, fill=(0, 0, 0, 0))
    draw.rounded_rectangle(inner, radius=8, outline=metal_light, width=3)
    draw.rounded_rectangle(
        [inner[0] - 2, inner[1] - 2, inner[2] + 2, inner[3] + 2], radius=9, outline=(*gold[:3], 200), width=1
    )

    # Rivets along the top/bottom edge.
    rivet_y_top = outer[1] + 9
    rivet_y_bot = outer[3] - 9
    for x in range(int(outer[0] + 26), int(outer[2] - 20), 34):
        draw.ellipse([x - 4, rivet_y_top - 4, x + 4, rivet_y_top + 4], fill=metal_light)
        draw.ellipse([x - 4, rivet_y_bot - 4, x + 4, rivet_y_bot + 4], fill=metal_light)

    # End caps (small diamond studs) marking the bar's min/max ends.
    for ex in (outer[0] + 12, outer[2] - 12):
        pts = [(ex, h / 2 - 14), (ex + 10, h / 2), (ex, h / 2 + 14), (ex - 10, h / 2)]
        draw.polygon(pts, fill=gold)

    img.save(path)
    return {"width": w, "height": h}


def sha256_of(path):
    h = hashlib.sha256()
    with open(path, "rb") as f:
        h.update(f.read())
    return h.hexdigest()


def write_provenance(png_path, prompt, meta):
    now = datetime.now(timezone.utc).isoformat()
    data = {
        "file": os.path.basename(png_path),
        "prompt": prompt,
        "tool": TOOL,
        "generatedAt": now,
        "checksumSha256": sha256_of(png_path),
        "runtimeEligible": False,
        "notes": "Draft concept art for the UI resource-refresh pass. Not promoted to Assets/; requires design audit before any runtime use.",
        **meta,
    }
    prov_path = png_path + ".provenance.json"
    with open(prov_path, "w", encoding="utf-8") as f:
        json.dump(data, f, ensure_ascii=False, indent=2)
    return prov_path


def main():
    blue_path = os.path.join(OUT_DIR, "faction-banner-blue.png")
    red_path = os.path.join(OUT_DIR, "faction-banner-red.png")
    warbar_path = os.path.join(OUT_DIR, "war-bar-frame.png")

    blue_meta = draw_banner(
        blue_path,
        base_color=(46, 95, 163),
        shadow_color=(18, 40, 74),
        trim_color=(214, 168, 74),
        label="blue faction pennant",
    )
    red_meta = draw_banner(
        red_path,
        base_color=(163, 46, 46),
        shadow_color=(74, 18, 18),
        trim_color=(214, 168, 74),
        label="red faction pennant",
    )
    warbar_meta = draw_warbar_frame(warbar_path)

    write_provenance(
        blue_path,
        prompt="Procedural PIL draw: vertical siege pennant banner, blue faction, gradient cloth, "
        "swallow-tail cut, gold rope trim, castle-crest disc emblem, wooden pole.",
        meta=blue_meta,
    )
    write_provenance(
        red_path,
        prompt="Procedural PIL draw: vertical siege pennant banner, red faction, gradient cloth, "
        "swallow-tail cut, gold rope trim, castle-crest disc emblem, wooden pole.",
        meta=red_meta,
    )
    write_provenance(
        warbar_path,
        prompt="Procedural PIL draw: ornate horizontal war-bar frame, beveled dark metal plate, "
        "gold inlay, rivets, diamond end caps, hollow transparent center for a runtime fill sprite.",
        meta=warbar_meta,
    )

    print("wrote:")
    for p in (blue_path, red_path, warbar_path):
        print(" ", p, sha256_of(p)[:12])


if __name__ == "__main__":
    main()
