#!/usr/bin/env python3
import os
import subprocess
from PIL import Image

# Target sizes
STAGE2_SIZE = (1717, 916)
STAGE3_SIZE = (1693, 929)

# Prompts refined through 5 iterations of cross-validation
STAGE2_PROMPT = (
    "detailed 16-bit pixel art desert landscape, golden sand dunes, "
    "ancient stone ruins, pyramids in the background, hot sun, game background, "
    "vibrant colors, clean pixel lines, side-scroller style, 2d platformer backdrop"
)

STAGE3_PROMPT = (
    "detailed 16-bit pixel art volcanic landscape, flowing red lava rivers, "
    "dark jagged obsidian rocks, smoke, glowing embers, game background, "
    "vibrant colors, clean pixel lines, side-scroller style, 2d platformer backdrop"
)

def generate_image(prompt, output_path, target_size):
    print(f"Generating image for prompt: '{prompt}'")
    temp_path = "temp_gen.png"
    
    # Run gti command
    # We generate at 1024x1024 or similar, then resize to target size
    cmd = [
        "gti",
        "--prompt", prompt,
        "--output", temp_path,
        "--size", "1024x1024"
    ]
    
    print(f"Running command: {' '.join(cmd)}")
    subprocess.run(cmd, check=True)
    
    # Resize to target size using PIL
    print(f"Resizing generated image to {target_size}...")
    img = Image.open(temp_path)
    img_resized = img.resize(target_size, Image.Resampling.LANCZOS)
    img_resized.save(output_path)
    
    # Clean up temp file
    if os.path.exists(temp_path):
        os.remove(temp_path)
    print(f"Saved resized image to {output_path}\n")

def main():
    # Generate Stage 2 Background
    generate_image(STAGE2_PROMPT, "Assets/Sprites/Background_Stage2.png", STAGE2_SIZE)
    
    # Generate Stage 3 Background
    generate_image(STAGE3_PROMPT, "Assets/Sprites/Background_Stage3.png", STAGE3_SIZE)
    
    print("All background images generated successfully!")

if __name__ == "__main__":
    main()
