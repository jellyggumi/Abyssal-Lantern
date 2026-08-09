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
| 8 | WebGL build script (`Assets/Editor/`) + Pages deploy + menu link | engineering | done 2026-08-09 — batch build Succeeded (70.8MB, 0 errors, `webgl-build.log`); deployed to `jellyggumi.github.io` gh-pages `e3ceeac` → https://jellyggumi.github.io/games/castle-war/ (menu: /games/, homepage GAMES link) | web beat |
| 9 | BGM + 승/패 스팅어 | design + engineering | done 2026-08-09 — Higgsfield `sonilo_music`(playwriter/Gemini 대신 직접 경로), OGG 변환, provenance 첨부, `BgmManager` 자립 재생. 근거: `engineering/resource-manifest.md`, `design/presentation-spec.md#sound` | Stage 3 |
| 10 | Codex art pass (faction key art, UI) → concept lane first | design | pending | Stage 3 |
| 14 | 인트로 UI 텍스트가 프레임을 넘던 문제 | design + engineering | done 2026-08-09 — 모든 라벨이 한/영 중복이라 폭이 2배였다. 한글 전용 + 여백/자동크기로 수정(START·연대기·프롤로그·스테이지 카드), 설명 4줄→1줄. 근거: `qa/evidence/final-webgl-title-*.png` | G4 |
| 15 | 웹툰 프롤로그 아트 11장 + 인트로 릴 | design | done 2026-08-09 — 시나리오는 기존 `WebtoonPrologueController.Pages[]`를 샷 리스트로 사용(재작성 아님). `god-tibo-imagen` 생성 → 1600×896 JPEG(29MB→3.1MB) → `Resources/Webtoon/`, 단색 매트 폴백 유지 + 텍스트 스크림. ffmpeg로 33초 릴 합성해 메뉴에 게시 | G4 + web beat |

Higgsfield 영상 모델은 유료 플랜 전용이라 사용하지 못했고(무료 잔액 23.67 <
최저가 영상 잡 24), 스틸 합성으로 우회했다. `perfectpixel`(ppgen)은 설치된
빌드가 god-tibo-imagen 프로바이더를 지원하지 않으며(Gemini/OpenAI/OpenRouter/
Fal/BytePlus만) 해당 키가 이 머신에 없어 실행 불가 — 스프라이트 애니메이션
생성은 키 확보 시까지 보류다.

| 11 | Pristine-core volley cap: preserve one answer after barrel/clone collapse | engineering + QA | done 2026-08-09 — 3/3 focused EditMode tests pass (`qa/evidence/core-volley-cap-editmode.xml`) | G2/G7 |
| 12 | Precision/presentation pass: trajectory parity, drag cancellation, deployment guidance, ramped probabilities, hit feedback/audio, HUD density, castle facade skins | design + engineering + QA | done 2026-08-09 — focused Unity regressions green; final WebGL `result=Succeeded`, 74,571,664 bytes, 0 errors; rebuilt browser boot/launch produced 0 console/page errors (`qa/evidence/final-core-cap-*.png`) | G2/G4/G7 + web beat |
| 13 | Roster overhaul: remove 폭탄(Bomber), add 대포(Cannon) as a deploy-only installation, add per-card 생성조건 + mid-battle Supply economy | design + engineering + QA | done 2026-08-09 — design `design/deployment-economy.md`; rules `Assets/Scripts/DeploymentRules.cs`; runtime `DeploymentController.cs` + `CannonController.cs`; `Bomber.prefab` deleted (pre-delete tag `pre-bomber-removal-20260809`); **256/256 EditMode + 256/256 PlayMode pass, 0 compile errors** (`editmode-results.xml`, `playmode-results.xml`). 48 new EditMode pins in `Assets/Editor/DeploymentEconomyTests.cs`, mutation-tested 24/24 killed. Fixed a real click collision: deploy mode now suppresses `BrickPlacementController` so one enemy-turn click cannot both deploy and designate a brick. | G1/G2/G7 |

Blocking notes:
- 1b: `gh repo rename` returned 404 (akillness has push, not admin). Owner
  must rename in Settings; then update `origin` URL here.
- Large pushes to origin need `http.postBuffer` ≥ pack size (set to 1GB
  locally after a 629MB pack failed at the 500MB default).
- Unity editor and batch builds are mutually exclusive (project lock);
  batch-launched builds also require the WebGL module, installed 2026-08-09
  via Unity Hub headless CLI.
