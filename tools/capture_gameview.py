#!/usr/bin/env python3
"""Grab the Unity game view via MCP screenshot-game-view and save PNG."""
import base64
import json
import subprocess
import sys

def capture(path):
    out = subprocess.run(["./tools/mcp_call.sh", "screenshot-game-view", "{}", "90"],
                         capture_output=True, text=True)
    d = json.loads(out.stdout)
    for c in d["result"]["content"]:
        if c.get("type") == "image":
            with open(path, "wb") as f:
                f.write(base64.b64decode(c["data"]))
            return True
    return False

if __name__ == "__main__":
    path = sys.argv[1] if len(sys.argv) > 1 else "capture.png"
    ok = capture(path)
    print(("saved " if ok else "FAILED ") + path)
    sys.exit(0 if ok else 1)
