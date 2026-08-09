# castle-war — agent instructions

The single repository contract lives in **[CLAUDE.md](CLAUDE.md)**. Read and
apply it in full; this file is a pointer, not a second copy (two contracts
drift, and a drifted contract is worse than none).

Quick orientation:
- Live run artifacts: `_workspace/current/` (only writable run folder;
  `_workspace/archive/**` is read-only history).
- Engine: Unity 2022.3.62f2 (2D URP). Unity MCP package is preinstalled;
  batch-mode CLI commands are in CLAUDE.md §5–§6.
- Production cycle + quality gates: `skill://game-studio-harness`, resume from
  `_workspace/current/production/task-manifest.md`.
