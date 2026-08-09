#!/usr/bin/env python3
"""Generate Unknown Castle unit sprites via god-tibo-imagen (gti).

Borrows perfectpixel/ppgen's generation *flow* — magenta keying canvas,
subject-lock reference image, explicit style contract — but uses the
`gti` CLI (Codex backend, no API key) as the image provider.

Pipeline per frame:
  1. gti renders the pose on a pure-magenta (#FF00FF) keying canvas.
  2. chroma-key matte the magenta away -> RGBA with clean alpha + despill.
  3. trim to content bbox, pad square, resize, write over the existing PNG
     (the .meta sidecar / GUID is preserved so Unity references stay valid).

Resumable: a frame whose PNG is already a real (non-placeholder) RGBA image
larger than MIN_DONE_PX is skipped unless --force is passed.
"""
import argparse
import io
import os
import subprocess
import sys
import tempfile
import time
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
FRAMES_DIR = ROOT / "Assets/Resources/GeneratedUnitFrames"
WORK = ROOT / "tools/.gen_work"
OUT_SIZE = 512          # final square sprite size
MIN_DONE_PX = 128       # an existing frame >= this is considered "real"

# ---- style contract (cartoon: matches the full-res AI-painted look) ----
STYLE = (
    "clean 2D cartoon game sprite, bold dark outline, flat vivid colors, "
    "simple two-tone cel shading, smooth rounded shapes, expressive but "
    "simple face, readable silhouette at small size. "
    "Never use pixelation, gradients, photo textures, glossy lighting, or 3D rendering."
)

# ---- unit canonical descriptions ----
UNITS = {
    "Knight": "a tiny chibi medieval knight in silver plate armor with a blue helmet plume, holding a short sword and a small round shield",
    "Archer": "a tiny chibi medieval archer in a green hood and leather tunic, holding a wooden longbow",
    "Bomber": "a tiny chibi medieval bomber in a brown leather vest and goggles, carrying a round black bomb with a lit fuse",
}

# ---- state -> per-frame action poses (2 frames each) ----
STATES = {
    "Idle":   ["relaxed idle standing pose, weight on both feet",
               "subtle idle breathing pose, chest slightly raised"],
    "Walk":   ["mid-stride walking pose, left leg forward, arms swinging",
               "mid-stride walking pose, right leg forward, arms swinging the other way"],
    "Attack": ["wind-up attack pose, weapon drawn back ready to strike",
               "follow-through attack pose, weapon swung fully forward"],
    "Launch": ["crouched launch pose, body coiled low ready to spring",
               "extended launch pose, body stretched upward as if just released"],
}

FACING = "full body, side view facing right, centered, full body fully inside the frame, standing on a common ground line"


def gti(prompt: str, out: Path, ref: Path | None = None, size: str = "1024x1024", attempts: int = 3) -> bool:
    cmd = ["gti", "--prompt", prompt, "--output", str(out), "--size", size]
    if ref is not None and ref.exists():
        cmd += ["--image", str(ref)]
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
    # magenta-ness: high R & B, low G relative to them
    mg = np.minimum(R, B) - G
    magenta = (R > 130) & (B > 130) & (mg > 45)
    alpha = np.where(magenta, 0.0, 255.0).astype(np.uint8)

    # despill: kill the pink fringe by clamping G up toward min(R,B) on edge pixels
    pink = (~magenta) & (mg > 18)
    g_target = np.minimum(R, B)
    Gd = G.copy()
    Gd[pink] = np.maximum(G[pink], g_target[pink] - 8)
    rgb = np.stack([R, Gd, B], axis=-1).clip(0, 255).astype(np.uint8)

    rgba = np.dstack([rgb, alpha])
    out = Image.fromarray(rgba, "RGBA")

    # soften alpha edge a touch to avoid jaggies
    al = out.getchannel("A").filter(ImageFilter.GaussianBlur(0.6))
    out.putalpha(al)

    # trim to content bbox
    bbox = out.getchannel("A").point(lambda v: 255 if v > 16 else 0).getbbox()
    if bbox:
        out = out.crop(bbox)

    # pad square with small margin
    w, h = out.size
    side = int(max(w, h) * 1.10) + 2
    canvas = Image.new("RGBA", (side, side), (0, 0, 0, 0))
    canvas.paste(out, ((side - w) // 2, (side - h) // 2), out)
    canvas = canvas.resize((size, size), Image.LANCZOS)
    dst.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(dst)


def is_real(p: Path) -> bool:
    if not p.exists():
        return False
    try:
        im = Image.open(p)
        return min(im.size) >= MIN_DONE_PX
    except Exception:
        return False


def build_prompt(desc: str, action: str) -> str:
    return (
        "BACKGROUND MANDATE (read first): fill the ENTIRE canvas, every pixel that is "
        "not part of the character, with solid pure magenta #FF00FF (RGB 255,0,255), "
        "edge to edge. No white, gray, black, scenery, floor, shadow, or frame. The "
        "character itself must contain NO magenta.\n\n"
        f"Draw one game-sprite pose of {desc}. Pose: {action}. {FACING}.\n\n"
        f"Render contract (obey strictly): {STYLE}\n\n"
        "Subject lock: if a reference image is attached it is the canonical character; "
        "match its face, build, outfit, gear and exact palette in every pose. Hold one "
        "fixed camera and facing — only the body moves between poses."
    )


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--units", default="Knight,Archer,Bomber")
    ap.add_argument("--states", default="Idle,Walk,Attack,Launch")
    ap.add_argument("--force", action="store_true")
    args = ap.parse_args()

    units = [u.strip() for u in args.units.split(",") if u.strip()]
    states = [s.strip() for s in args.states.split(",") if s.strip()]
    WORK.mkdir(parents=True, exist_ok=True)

    total = ok = skip = fail = 0
    for unit in units:
        desc = UNITS[unit]
        ref = WORK / f"{unit}_base.png"
        if args.force or not ref.exists() or ref.stat().st_size < 2000:
            print(f"[{unit}] base reference ...", flush=True)
            if not gti(build_prompt(desc, "neutral A-pose, arms slightly out, facing right"), ref):
                print(f"[{unit}] base FAILED — continuing without reference", flush=True)

        for state in states:
            for fi, action in enumerate(STATES[state]):
                total += 1
                dst = FRAMES_DIR / unit / state / f"{state.lower()}_{fi:03d}.png"
                if not args.force and is_real(dst):
                    print(f"  = skip {unit}/{state}/{dst.name}", flush=True)
                    skip += 1
                    continue
                raw = WORK / f"{unit}_{state}_{fi}.raw.png"
                print(f"  + gen {unit}/{state}/{dst.name}", flush=True)
                if not gti(build_prompt(desc, action), raw, ref=ref):
                    print(f"    FAIL {unit}/{state}/{fi}", flush=True)
                    fail += 1
                    continue
                chroma_key(raw, dst)
                ok += 1

    print(f"\nDONE total={total} generated={ok} skipped={skip} failed={fail}", flush=True)
    return 0 if fail == 0 else 1


if __name__ == "__main__":
    sys.exit(main())
