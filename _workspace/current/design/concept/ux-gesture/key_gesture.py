#!/usr/bin/env python3
"""Key + downsize the drag-gesture pictogram (same flood-fill approach as
gen_siege_art.py: an edge flood keys the studio background without eating the
white interior shapes a flat colour-distance key would halo)."""
from collections import deque

from PIL import Image

SRC = "drag_gesture_cue_raw.png"
OUT = "ux_drag_gesture.png"
TOLERANCE = 70  # colour distance from the sampled corner key


def main():
    src = Image.open(SRC).convert("RGBA")
    px = src.load()
    w, h = src.size

    # Key colour: average of the four 8x8 corners.
    samples = []
    for cx, cy in ((0, 0), (w - 8, 0), (0, h - 8), (w - 8, h - 8)):
        for dx in range(8):
            for dy in range(8):
                samples.append(px[cx + dx, cy + dy][:3])
    key = tuple(sum(c[i] for c in samples) // len(samples) for i in range(3))

    def is_key(p):
        return sum((p[i] - key[i]) ** 2 for i in range(3)) ** 0.5 <= TOLERANCE

    # Edge flood fill: only background CONNECTED to the border is cleared.
    seen = bytearray(w * h)
    queue = deque()
    for x in range(w):
        for y in (0, h - 1):
            queue.append((x, y))
    for y in range(h):
        for x in (0, w - 1):
            queue.append((x, y))
    while queue:
        x, y = queue.popleft()
        if not (0 <= x < w and 0 <= y < h) or seen[y * w + x]:
            continue
        seen[y * w + x] = 1
        if not is_key(px[x, y][:3]):
            continue
        px[x, y] = (0, 0, 0, 0)
        queue.extend(((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)))

    # Crop to content, square-pad, downsize for UI use.
    bbox = src.getbbox()
    pad = 30
    box = (max(0, bbox[0] - pad), max(0, bbox[1] - pad),
           min(w, bbox[2] + pad), min(h, bbox[3] + pad))
    img = src.crop(box)
    side = max(img.width, img.height)
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(img, ((side - img.width) // 2, (side - img.height) // 2))
    canvas = canvas.resize((256, 256), Image.LANCZOS)
    canvas.save(OUT)

    alpha = canvas.getchannel("A")
    hist = alpha.histogram()
    opaque = sum(hist[200:]) / (256 * 256)
    clear = sum(hist[:30]) / (256 * 256)
    print(f"key={key} opaque={opaque:.3f} transparent={clear:.3f} -> {OUT}")


if __name__ == "__main__":
    main()
