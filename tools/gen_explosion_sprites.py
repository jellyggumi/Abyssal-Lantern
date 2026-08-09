#!/usr/bin/env python3
import os
import subprocess
import time
from pathlib import Path
import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
FRAMES_DIR = ROOT / "Assets/Resources/GeneratedExplosionFrames"
WORK = ROOT / "tools/.gen_work"
OUT_SIZE = 512

STYLE = (
    "clean 2D cartoon game sprite, bold dark outline, flat vivid colors, "
    "simple two-tone cel shading, smooth rounded shapes, readable silhouette. "
    "Never use pixelation, gradients, photo textures, glossy lighting, or 3D rendering."
)

FRAMES = [
    "initial small spark and puff of smoke, start of explosion",
    "expanding fireball with orange and yellow flames, growing rapidly",
    "maximum size fiery explosion with smoke puffs, bright yellow core",
    "dissipating fireball, flames shrinking, smoke expanding, orange embers",
    "mostly smoke puffs with small lingering embers, fading out",
    "fading smoke puffs disappearing into the air, final frame"
]

def gti(prompt: str, out: Path, size: str = "1024x1024", attempts: int = 3) -> bool:
    cmd = ["gti", "--prompt", prompt, "--output", str(out), "--size", size]
    for i in range(attempts):
        try:
            r = subprocess.run(cmd, capture_output=True, text=True, timeout=300)
        except subprocess.TimeoutExpired:
            print(f"    ! timeout (attempt {i+1})", flush=True)
            continue
        if r.returncode == 0 and out.exists() and out.stat().st_size > 2000:
            return True
        print(f"    ! gti rc={r.returncode} (attempt {i+1}) {r.stderr.strip()[:160]}", flush=True)
        time.sleep(2)
    return False

def chroma_key(src: Path, dst: Path, size: int = OUT_SIZE) -> None:
    im = Image.open(src).convert("RGB")
    a = np.asarray(im).astype(np.float32)
    R, G, B = a[..., 0], a[..., 1], a[..., 2]
    mg = np.minimum(R, B) - G
    magenta = (R > 130) & (B > 130) & (mg > 45)
    alpha = np.where(magenta, 0.0, 255.0).astype(np.uint8)

    pink = (~magenta) & (mg > 18)
    g_target = np.minimum(R, B)
    Gd = G.copy()
    Gd[pink] = np.maximum(G[pink], g_target[pink] - 8)
    rgb = np.stack([R, Gd, B], axis=-1).clip(0, 255).astype(np.uint8)

    rgba = np.dstack([rgb, alpha])
    out = Image.fromarray(rgba, "RGBA")

    al = out.getchannel("A").filter(ImageFilter.GaussianBlur(0.6))
    out.putalpha(al)

    bbox = out.getchannel("A").point(lambda v: 255 if v > 16 else 0).getbbox()
    if bbox:
        out = out.crop(bbox)

    w, h = out.size
    side = int(max(w, h) * 1.10) + 2
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(out, ((side - w) // 2, (side - h) // 2), out)
    canvas = canvas.resize((size, size), Image.LANCZOS)
    dst.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(dst)

def build_prompt(action: str) -> str:
    return (
        "BACKGROUND MANDATE (read first): fill the ENTIRE canvas, every pixel that is "
        "not part of the explosion, with solid pure magenta #FF00FF (RGB 255,0,255), "
        "edge to edge. No white, gray, black, scenery, floor, shadow, or frame. The "
        "explosion itself must contain NO magenta.\n\n"
        f"Draw one game-sprite frame of a fiery explosion. Frame: {action}. Centered, full effect fully inside the frame.\n\n"
        f"Render contract (obey strictly): {STYLE}"
    )

def main():
    WORK.mkdir(parents=True, exist_ok=True)
    FRAMES_DIR.mkdir(parents=True, exist_ok=True)
    
    print("Starting explosion sprite generation...", flush=True)
    for fi, action in enumerate(FRAMES):
        dst = FRAMES_DIR / f"explosion_{fi:03d}.png"
        if dst.exists() and dst.stat().st_size > 2000:
            print(f"Skipping frame {fi} (already exists)", flush=True)
            continue
        raw = WORK / f"explosion_{fi}.raw.png"
        print(f"Generating frame {fi}: {dst.name}", flush=True)
        prompt = build_prompt(action)
        if gti(prompt, raw):
            chroma_key(raw, dst)
            print(f"Successfully generated {dst.name}", flush=True)
        else:
            print(f"Failed to generate frame {fi}", flush=True)
            return 1
    print("All explosion frames generated successfully!", flush=True)
    return 0

if __name__ == "__main__":
    import sys
    sys.exit(main())
