# Task manifest — castle-war

- run-id: 20260809-castle-war-stage1
- stage: **Stage 1 (concept pivot)** — entered 2026-08-09
- next_public_beat: WebGL build linked from https://jellyggumi.github.io/ menu

| # | Task | Lane | Status | Beat it serves |
|---|---|---|---|---|
| 1 | Repo identity: commit local castle-war restructure, push to origin | production | done 2026-08-09 (`b639788c` pushed) | all |
| 1b | Remote rename `Abyssal-Lantern → castle-war` | production | **blocked: needs owner (`jellyggumi`) admin** | all |
| 2 | Rule file (CLAUDE.md + AGENTS.md pointer) | production | done 2026-08-09 | all |
| 3 | Trend survey: Archery Bastions (scrapling capture) | design | done 2026-08-09 | G8 |
| 4 | Production brief (concept-pivot mode) | intake | done 2026-08-09 | all |
| 5 | llm-wiki project page `wiki/projects/castle-war/` + index link | production | done 2026-08-09 | docs |
| 6 | wai-play install; verify Unity MCP + CLI control path | engineering | done 2026-08-09 (checkout `~/orca/wai-play`, doctor OK keyless; `unity-mcp-cli` + batch CLI verified) | web beat |
| 7 | Faction-war design pack: worldview, core-loop model, presentation spec | design | done 2026-08-09 (G1/G7 draft + presentation spec + telemetry draft + resource manifest) | G1/G7/G6-ops draft |
| 8 | WebGL build script (`Assets/Editor/`) + Pages deploy + menu link | engineering | in_progress (WebGL module installed; batch build running; pages `games/` menu authored) | web beat |
| 9 | BGM via Gemini(playwriter) → `Assets/Resources/Audio/` after audit | design | pending (playwriter 0.4.0 + Chrome relay verified live) | Stage 3 |
| 10 | Codex art pass (faction key art, UI) → concept lane first | design | pending | Stage 3 |

Blocking notes:
- 1b: `gh repo rename` returned 404 (akillness has push, not admin). Owner
  must rename in Settings; then update `origin` URL here.
- Large pushes to origin need `http.postBuffer` ≥ pack size (set to 1GB
  locally after a 629MB pack failed at the 500MB default).
- Unity editor and batch builds are mutually exclusive (project lock);
  batch-launched builds also require the WebGL module, installed 2026-08-09
  via Unity Hub headless CLI.
