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
- **Roster now Knight / Archer / Cannon / Barrel** (2026-08-09): the Bomber was
  removed and the Cannon added as a deploy-only installation, alongside a
  Supply economy that allows creation *during* battle. 246/246 EditMode tests.
  See [[wiki/projects/castle-war/deployment-economy]].
- **Sound and prologue art** (2026-08-09): siege BGM with victory/defeat
  stingers (Higgsfield `sonilo_music`), and painted webtoon panels for all 11
  prologue pages (`god-tibo-imagen`), assembled into a 33s intro reel with
  ffmpeg. Higgsfield's video models are paid-plan only, so the reel is
  composited from stills rather than generated — see
  `_workspace/current/engineering/resource-manifest.md`.
- **Launcher and keep art** (2026-08-10): the launch portal became a **새총
  (slingshot)** and the defended base became a **성 (castle keep)** that
  visibly crumbles through three damage stages. Codex/gti quota was exhausted,
  so generation ran on Higgsfield (`flux_2` + `flux_kontext`); stages 1–2 are
  image-conditioned edits of stage 0, so all three are the same castle.
  See [[wiki/projects/castle-war/siege-art]].
- **Visibility: post-action readback** (2026-08-13): the answer to *"적이 어떻게
  쏘는지 안 보인다"* flipped from adding UI to keeping the last shot readable — the
  arc persists past impact (the marker that first ended it was cut a day later — see
  below) and one line reports what the
  turn cost. A survey of twelve titles refuted the draft's pre-action telegraph
  (0.9s window, zero enemy-turn inputs) and its seven new UI elements (a documented
  "icon mess" failure path); six of those placeholders were deleted. Precedent is
  Rampart (1990) — structurally the same game, no telegraph at all.
  See [[wiki/projects/castle-war/visibility-readback]].
- **Attack motion + impact VFX** (2026-08-14): two reports — a white box on impact,
  and no way to tell an attack was happening — had one cause. Everything needed was
  already present and unwired: PulseAttack never fired on launch, the enemy apron was
  a bare Transform, the player's launcher hid for the whole enemy turn, and enemy
  volleys were silent. The impact icon turned out to be a form error (1/13 in the
  sample, and that one hides its board) and was deleted, so **zero placeholders
  remain**. Net screen elements: **−1**. Verification caught two defects in the
  freshly shipped arcs: they measured 1.13:1 against the sky (alpha cannot fix a
  hue-only difference) and the dashed enemy arc never rendered at all.
  See [[wiki/projects/castle-war/attack-motion-and-impact-vfx]].
- Cycle: Stage 1 concept pivot — see
  `_workspace/current/production/task-manifest.md` (live) for tasks.
- Rule file: repository `CLAUDE.md` (AGENTS.md points to it).

## Pages

- Reference analysis: `_workspace/current/design/trend-survey/archery-bastions-castle-war.md`
  (Archery Bastions: Castle War — adopt faction readability, differentiate on
  physics destruction + meta spine)
- Deployment economy (roster overhaul, 대포 + 전투 중 생성):
  [[wiki/projects/castle-war/deployment-economy]]
- Launcher + castle keep art (새총, 성, 3단계 파괴 애니메이션):
  [[wiki/projects/castle-war/siege-art]]
- Visibility / post-action readback (궤적 잔존, 턴 판독 한 줄):
  [[wiki/projects/castle-war/visibility-readback]]
- Attack motion + impact VFX (양측 발사기, 반동·와인드업, 아크 카싱):
  [[wiki/projects/castle-war/attack-motion-and-impact-vfx]]
- Legacy history: [[castle-busters-phase-1-analysis]],
  [[castle-busters-phase-2-completion]] (under `wiki/reports/`)

## Conventions

Durable castle-war findings file here (`wiki/projects/castle-war/`); live
cycle evidence stays in `_workspace/current/` and is only summarized into the
wiki at cycle close, per repository CLAUDE.md §4.
