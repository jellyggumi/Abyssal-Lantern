#!/usr/bin/env python3
"""FX art pack for the widened-battlefield update.

Generates (via gti + magenta chroma-key, same pipeline as gen_explosion_sprites.py):
  - Effects/fx_spawn/       4 frames  arcane materialize flash (referenced, was missing)
  - Effects/fx_eruption/    5 frames  vertical magma geyser column (portrait)
  - Effects/fx_petals/      4 frames  vertical petal-burst column (portrait)
  - Effects/particles/      3 single shards: ember / smoke / petal
  - Gimmicks/gimmick_vent_magma.png, gimmick_vent_petal.png  vent base craters
Writes Unity .meta (Sprite importer) for every new PNG using a template + fresh GUID.
"""
import os
import concurrent.futures
import subprocess
import sys
import time
import uuid
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter

ROOT = Path(__file__).resolve().parents[1]
RES = ROOT / "Assets/Resources"
WORK = ROOT / "tools/.gen_work/fxpack"
META_TEMPLATE = RES / "Effects/fx_dust/fx_dust_000.png.meta"

STYLE = (
    "clean 2D cartoon game sprite, bold dark outline, flat vivid colors, "
    "simple two-tone cel shading, smooth rounded shapes, readable silhouette. "
    "Never use pixelation, gradients, photo textures, glossy lighting, or 3D rendering."
)

def prompt_for(subject: str) -> str:
    return (
        "BACKGROUND MANDATE (read first): fill the ENTIRE canvas, every pixel that is "
        "not part of the subject, with solid pure magenta #FF00FF (RGB 255,0,255), "
        "edge to edge. No white, gray, black, scenery, floor, shadow, or frame. The "
        "subject itself must contain NO magenta.\n\n"
        f"{subject}\n\n"
        f"Render contract (obey strictly): {STYLE}"
    )

JOBS = []  # (relative output path, subject, size, max_side)

def add(rel, subject, size="1024x1024", max_side=512):
    JOBS.append((rel, subject, size, max_side))

# --- fx_spawn: obstacle-materialize flash (FrameAnimEffect key was referenced but had no art)
SPAWN = [
    "a faint circle of small glowing ice-blue arcane runes appearing in the air, thin wisps of pale blue light, sparse and dim, start of a magical summoning",
    "a bright ring of ice-blue arcane runes with a rising pillar of pale cyan light in the middle, glowing motes floating upward, magical summoning building up",
    "a brilliant white-and-cyan magical flash filling the shape, arcs of electric blue energy, star-shaped burst core at maximum brightness, peak of a summoning",
    "dissipating pale blue energy wisps and tiny fading sparkles drifting apart, last remnants of a magical summoning, dim and sparse",
]
for i, s in enumerate(SPAWN):
    add(f"Effects/fx_spawn/fx_spawn_{i:03d}.png",
        f"Draw one game-sprite frame of a magical summoning flash effect. Frame: {s}. Centered, full effect fully inside the frame.")

# --- fx_eruption: vertical magma geyser column (portrait art; tallest side scaled to worldSize)
ERUPT = [
    "a small burst of bright orange lava and grey smoke cracking out of the bottom, short flame tongue, start of a volcanic geyser, most of the tall frame still empty above",
    "a rising thick column of bright orange and yellow lava shooting straight up, reaching halfway up the tall frame, small glowing rocks flying, grey smoke at the base",
    "a full-height roaring vertical fountain of orange-yellow lava filling the tall frame top to bottom, glowing rocks and embers thrown out to the sides at the top, powerful volcanic geyser at maximum",
    "a collapsing lava column, top half breaking into falling glowing blobs and embers, lower half still bright orange, grey smoke spreading",
    "mostly grey and dark smoke wisps drifting up with a few tiny fading orange embers, volcanic geyser dying out, sparse",
]
for i, s in enumerate(ERUPT):
    add(f"Effects/fx_eruption/fx_eruption_{i:03d}.png",
        f"Draw one game-sprite frame of a tall VERTICAL volcanic lava geyser effect, portrait orientation. Frame: {s}. Column centered horizontally, anchored to the bottom edge of the frame.",
        size="1024x1536", max_side=640)

# --- fx_petals: vertical petal-burst column (portrait)
PETALS = [
    "a small puff of pink cherry-blossom petals popping out of the bottom, a few petals and pale pink sparkles, start of a flower geyser, most of the tall frame empty above",
    "a swirling vertical stream of pink and white cherry-blossom petals shooting straight up, reaching most of the tall frame, petals tumbling at different angles, joyful burst",
    "a full-height fountain of dense pink petals, white flower heads and golden pollen sparkles filling the tall frame, petals fanning out at the top like a firework",
    "petals slowly fluttering back down, sparse pink petals and fading golden sparkles drifting apart, flower geyser ending, gentle and sparse",
]
for i, s in enumerate(PETALS):
    add(f"Effects/fx_petals/fx_petals_{i:03d}.png",
        f"Draw one game-sprite frame of a tall VERTICAL burst of flower petals, portrait orientation. Frame: {s}. Column centered horizontally, anchored to the bottom edge of the frame.",
        size="1024x1536", max_side=640)

# --- particle shards (small single sprites for ParticleSystems)
add("Effects/particles/particle_ember.png",
    "Draw one tiny game particle sprite: a single glowing ember shard, bright yellow-white core with orange edges, slightly angular flame-chip shape. One object only, centered.",
    max_side=128)
add("Effects/particles/particle_smoke.png",
    "Draw one tiny game particle sprite: a single soft round puff of warm grey smoke, simple cartoon cloud blob with slightly darker flat shading on one side. One object only, centered.",
    max_side=128)
add("Effects/particles/particle_petal.png",
    "Draw one tiny game particle sprite: a single pink cherry blossom petal, teardrop shape with a small notch at the tip, flat pastel pink with a slightly darker base. One object only, centered.",
    max_side=128)

# --- vent base craters (gimmick art)
add("Gimmicks/gimmick_vent_magma.png",
    "Draw one game-sprite of a small volcanic ground vent: a low wide rocky crater mound of dark grey stone with glowing orange lava cracks and a round glowing orange-red mouth on top, a few small embers. Single object, centered, viewed from the side like a 2D platformer prop.",
    max_side=512)
add("Gimmicks/gimmick_vent_petal.png",
    "Draw one game-sprite of a small magical flower vent: a low wide mound of green grass and leaves with a ring of pink cherry blossoms around a round glowing soft-pink mouth on top, a few floating petals. Single object, centered, viewed from the side like a 2D platformer prop.",
    max_side=512)


def gti(prompt: str, out: Path, size: str, attempts: int = 3) -> bool:
    cmd = ["gti", "--prompt", prompt, "--output", str(out), "--size", size]
    for i in range(attempts):
        try:
            r = subprocess.run(cmd, capture_output=True, text=True, timeout=420)
        except subprocess.TimeoutExpired:
            print(f"    ! timeout attempt {i+1} for {out.name}", flush=True)
            continue
        if r.returncode == 0 and out.exists() and out.stat().st_size > 2000:
            return True
        print(f"    ! gti rc={r.returncode} attempt {i+1} {out.name}: {r.stderr.strip()[:160]}", flush=True)
        time.sleep(2)
    return False


def chroma_key(src: Path, dst: Path, max_side: int) -> None:
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

    out = Image.fromarray(np.dstack([rgb, alpha]), "RGBA")
    out.putalpha(out.getchannel("A").filter(ImageFilter.GaussianBlur(0.6)))

    bbox = out.getchannel("A").point(lambda v: 255 if v > 16 else 0).getbbox()
    if bbox:
        out = out.crop(bbox)

    # Preserve aspect: pad 6%, then scale the longest side to max_side.
    w, h = out.size
    pw, ph = int(w * 1.06) + 2, int(h * 1.06) + 2
    canvas = Image.new("RGBA", (pw, ph), (0, 0, 0, 0))
    canvas.paste(out, ((pw - w) // 2, (ph - h) // 2), out)
    scale = max_side / max(pw, ph)
    canvas = canvas.resize((max(1, int(pw * scale)), max(1, int(ph * scale))), Image.LANCZOS)
    dst.parent.mkdir(parents=True, exist_ok=True)
    canvas.save(dst)


def write_meta(png: Path) -> None:
    meta = png.with_name(png.name + ".meta")
    if meta.exists():
        return
    template = META_TEMPLATE.read_text()
    lines = template.splitlines()
    out = []
    for line in lines:
        if line.startswith("guid: "):
            out.append(f"guid: {uuid.uuid4().hex}")
        elif "spriteID:" in line:
            out.append(f"    spriteID: {uuid.uuid4().hex[:16]}0800000000000000")
        else:
            out.append(line)
    meta.write_text("\n".join(out) + "\n")


def run_job(job):
    rel, subject, size, max_side = job
    dst = RES / rel
    # FORCE=1 overwrites existing art (used to replace the procedural interim pack
    # from gen_fx_procedural.py once the imagen backend recovers from rate limiting).
    force = os.environ.get("FORCE") == "1"
    if dst.exists() and dst.stat().st_size > 1000 and not force:
        write_meta(dst)
        return f"skip {rel}"
    raw = WORK / (rel.replace("/", "_") + ".raw.png")
    raw.parent.mkdir(parents=True, exist_ok=True)
    if not gti(prompt_for(subject), raw, size):
        return f"FAIL {rel}"
    chroma_key(raw, dst, max_side)
    write_meta(dst)
    return f"ok   {rel}"


def main():
    WORK.mkdir(parents=True, exist_ok=True)
    failures = 0
    with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
        for result in pool.map(run_job, JOBS):
            print(result, flush=True)
            if result.startswith("FAIL"):
                failures += 1
    print(f"done, {failures} failures", flush=True)
    return 1 if failures else 0


if __name__ == "__main__":
    sys.exit(main())
