# castle-war (project)

Unity 2D physics siege game, pivoting from single-castle artillery duel
("Castle Busters") to a **faction-vs-faction castle war** (blue vs red).
Repository: `jellyggumi/Abyssal-Lantern` → rename to `castle-war` pending
owner action. Public beat: WebGL build on the
[jellyggumi.github.io](https://jellyggumi.github.io/) menu.

## Status (2026-08-09)

- **Playable on the web**: https://jellyggumi.github.io/games/castle-war/
  (WebGL, gzip + decompression fallback; menu at /games/). Build via
  `Assets/Editor/WebGLReleaseBuild.cs`, batch CLI, editor closed.
- Concept prototype (faction-war reframe testbed) published as a Claude
  artifact: war-bar HUD, drag-launch, ground-war validation.
- Build carried in: 3 units (Knight/Archer/Bomber), 3 stages, 41 EditMode
  tests, balance at 50/50 win-rate (Phase 2 complete).
- Cycle: Stage 1 concept pivot — see
  `_workspace/current/production/task-manifest.md` (live) for tasks.
- Rule file: repository `CLAUDE.md` (AGENTS.md points to it).

## Pages

- Reference analysis: `_workspace/current/design/trend-survey/archery-bastions-castle-war.md`
  (Archery Bastions: Castle War — adopt faction readability, differentiate on
  physics destruction + meta spine)
- Legacy history: [[castle-busters-phase-1-analysis]],
  [[castle-busters-phase-2-completion]] (under `wiki/reports/`)

## Conventions

Durable castle-war findings file here (`wiki/projects/castle-war/`); live
cycle evidence stays in `_workspace/current/` and is only summarized into the
wiki at cycle close, per repository CLAUDE.md §4.
