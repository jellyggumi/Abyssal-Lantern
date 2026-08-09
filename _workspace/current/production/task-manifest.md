# Task manifest — castle-war

- run-id: 20260809-castle-war-stage1
- stage: **Stage 1 (concept pivot)** — entered 2026-08-09
- next_public_beat: WebGL build linked from https://jellyggumi.github.io/ menu

| # | Task | Lane | Status | Beat it serves |
|---|---|---|---|---|
| 1 | Repo identity: commit local castle-war restructure, push to origin | production | in_progress | all |
| 1b | Remote rename `Abyssal-Lantern → castle-war` | production | **blocked: needs owner (`jellyggumi`) admin** | all |
| 2 | Rule file (CLAUDE.md + AGENTS.md pointer) | production | done 2026-08-09 | all |
| 3 | Trend survey: Archery Bastions (scrapling capture) | design | done 2026-08-09 | G8 |
| 4 | Production brief (concept-pivot mode) | intake | done 2026-08-09 | all |
| 5 | llm-wiki project page `wiki/projects/castle-war/` + index link | production | in_progress | docs |
| 6 | wai-play install; verify Unity MCP + CLI control path | engineering | pending | web beat |
| 7 | Faction-war design pack: worldview, core-loop model, presentation spec | design | pending | G1/G7 draft |
| 8 | WebGL build script (`Assets/Editor/`) + Pages deploy + menu link | engineering | pending | web beat |
| 9 | BGM via Gemini(playwriter) → `Assets/Resources/Audio/` after audit | design | pending | Stage 3 |
| 10 | Codex art pass (faction key art, UI) → concept lane first | design | pending | Stage 3 |

Blocking notes:
- 1b: `gh repo rename` returned 404 (akillness has push, not admin). Owner
  must rename in Settings; then update `origin` URL here.
- 8 depends on 6 only for the CLI verification path, not for authoring the
  build script.
