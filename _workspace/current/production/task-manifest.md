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
| 16 | Final combat-feel hardening: suppress terrain-anchor break feedback/combo inflation and credit fatal core score before GameOver freezes results | engineering + QA | done 2026-08-09 — production ordering fixed in `DestructibleBlock.DestroyBlock`; exact EditMode regressions pass for ground-anchor combo suppression and 500-point enemy-core attribution/results snapshot. Precision and current-roster focused gates were re-run individually and passed. | G2/G4/G7 |
| 17 | 병사생성 포털 → **새총(slingshot)**, 지켜야 할 기지 → **성(castle keep)**, 성이 점점 부서지는 3단계 애니메이션 | design + engineering + QA | done 2026-08-10 — spec `design/siege-art-spec.md`. CLAUDE.md §3 우선순위(Codex CLI)는 쿼터 소진(`Aug 16th` 리셋), `gti`(god-tibo-imagen)는 같은 백엔드라 HTTP 429, `ppgen`은 설치 빌드가 해당 프로바이더 미지원 + 키 없음 → **Higgsfield CLI**(flux_2 / flux_kontext, 6.5 credits)로 생성. 스테이지 1·2는 독립 렌더가 아니라 s0의 image-conditioned edit이라 **같은 성의 세 파괴 단계**. 21개 PNG를 `Resources/Gimmicks/`로 승격(전부 `.provenance.json` 동반). **285 EditMode 중 281 pass**, 신규 핀 12개(`Assets/Tests/EditMode/SiegeArtResourceTests.cs`). 잔여 4건은 커밋 `bf491069`(타 세션, 02:50)이 `DrawTrajectory` 적분 루프를 `i=1`→`i=0`으로 바꾸며 무효화한 **선행 실패**이며, `DrawTrajectory`는 HEAD와 바이트 동일(함수 본문 추출로 검증) — 본 작업과 무관. | G4 + G7 |

본 작업에서 **실제 버그 2건**을 발견해 함께 고쳤다. (1) `LaunchManager.Update()`가 매 프레임 발사 어포던스의 `localScale`에 맥동값을 **대입**해 셋업 시 계산한 월드 크기 핏을 파괴하고 있었다(기존 포털 아트도 네이티브 스케일로 렌더 중이었다). (2) PNG를 `.meta` 없이 복사해 Unity가 `textureType: Default`로 임포트 → `Resources.LoadAll<Sprite>`가 **빈 배열**을 반환해 새총이 조용히 절차적 링으로 폴백하고 있었다. 21개 메타를 Sprite 임포터로 재작성해 해결했고, import probe로 6/4/4/4 스프라이트 로드를 확인했다.

| 19 | 다음 스테이지로 넘어가지 않는 오류 | engineering + QA | done 2026-08-10 — 원인은 `GameManager.RequestStage`의 `if (PendingStage == stage) return;` 조기 반환이었다. 세 호출부(결과 화면 "다음 스테이지" 버튼, 5초 자동 진행, 인트로 스테이지 선택)가 **호출 전에** `navigated = true`를 먼저 걸어서, 이 조용한 거부가 카운트다운을 멈추고 버튼도 소진시켜 플레이어를 결과 화면에 가둬버렸다. `PendingStage`는 "다음 부팅이 어떤 레이아웃을 지을지"만 기록할 뿐 "리로드가 필요 없다"는 뜻이 아니다 — 결과 경로는 `skipIntroOnce`+`ResetSeries`도 함께 적용해야 하고, 인트로 피커는 같은 스테이지 재선택이 아무 일도 안 하는 죽은 카드였다. 조기 반환을 제거하고 `RequestStage`가 **수락 여부를 bool로 반환**하게 바꿔, 호출부는 수락됐을 때만 `navigated`를 건다. 자동 진행은 거부 시 라벨을 `다음 스테이지 (잠김)`으로 바꾸고 멈추지 않는다. 추가로 `StageProgressStore`에 **세션 내 비후퇴 미러**를 넣었다 — WebGL은 `PlayerPrefs.Save()`가 IndexedDB 비동기/시크릿모드/쿼터로 조용히 유실될 수 있고, 그러면 방금 클리어한 스테이지가 해금 게이트에서 거부된다. 진단은 추측이 아니라 프로브로 확정했다(`RequestStage` 3개 가드를 스테이지별로 출력 → 유일한 거부가 same-pending). **285/285 EditMode**, 신규 핀 15케이스/8메서드(`Assets/Tests/EditMode/StageAdvanceRegressionTests.cs`, 뮤테이션 검증: 미러 제거 시 정확히 T6만 red). | G7 |
| 20 | 대포 타겟 재탐색이 프레임마다 전체 블록을 스캔하던 성능 회귀 | engineering | done 2026-08-10 — `CannonController.FindTarget()`이 매 프레임·포대마다 살아있는 `DestructibleBlock` 전부(41×5 지형 + 양측 성 ≈ 200개)를 돌며 각각 `GetComponentInParent`(트랜스폼 계층 순회)를 호출했다. 배치 경제가 필드 인구를 늘리면서 이게 프레임 예산을 잡아먹어 **30게임 PlayMode 심이 1310초 → 2400초 초과(타임아웃)** 로 2배 느려졌다. 재탐색을 0.25초 간격으로 스로틀하고(재장전 3.2초보다 훨씬 촘촘하다), 캐시된 타겟이 죽거나 사거리를 벗어나면 즉시 재탐색하는 저비용 검증을 추가했다. | G7 |

Blocking notes:
- 1b: `gh repo rename` returned 404 (akillness has push, not admin). Owner
  must rename in Settings; then update `origin` URL here.
- Large pushes to origin need `http.postBuffer` ≥ pack size (set to 1GB
  locally after a 629MB pack failed at the 500MB default).
- Unity editor and batch builds are mutually exclusive (project lock);
  batch-launched builds also require the WebGL module, installed 2026-08-09
  via Unity Hub headless CLI.

| 17 | 난이도 곡선 재설계 + 스테이지 재배치 | design + engineering + QA | done 2026-08-10 — `SmoothStep`이 15턴에서 1.0 고정·평탄화되던 것을 Hill 곡선 `n^p/(n^p+h^p)`(h=0.6×램프, p=1.8)로 교체해 매 턴 상승·점근하도록 함(`DifficultyCurve.cs`, 신규 테스트 5). 스테이지는 성벽 높이만 2/3/**4**로 재배치 — Stage3가 근거 없이 Stage1 기본값 2를 물려받아 마지막 해금이 가장 무른 요새였음. 바람(거리 종속)과 페이싱(전장 정체성)은 의도적으로 비단조 유지하고 그 이유를 `StageProgressionShapeTests`로 고정. 근거: `qa/evidence/editmode-stage-redistribution.xml` (273 중 269 통과, 실패 4는 D-004) | G2/G7 |
| 18 | 최종 배포 | ops | done 2026-08-10 — castle-war `8f9edb8` → pages `275ce7a`. 라이브 검증: 페이지 오류 0, AudioContext running, 실제 매치 진입 (`qa/evidence/live-final-deploy.png`). https://jellyggumi.github.io/games/castle-war/ | web beat |
| 19 | VFX: 포구 화염 + 전 리소스 커버리지 감사 | design + engineering + QA | done 2026-08-10 — 선언된 기믹 키 26개 전수 조사 결과 정적 아트는 이미 완전(파일 32개, 절차 생성은 폴백뿐)했고 실제 공백은 움직임이었다. 대포 발사에 그려진 화염이 없어 `fx_muzzle` 6프레임을 기존 아트에서 스케일·알파 램프로 파생(개별 생성 시 프레임 간 정합 드리프트를 피하기 위함), 발사 방향 회전 적용. 알파 인용 버그로 1~3프레임이 1%로 나온 것을 프레임별 평균 알파 측정으로 검출·재생성. **300/300 EditMode 통과 — D-004 해소로 이번 사이클 첫 완전 녹색** (`qa/evidence/editmode-all-green.xml`) | G4 |
| 20 | 배포 | ops | done 2026-08-10 — castle-war `724ea59d` → pages `038603a`. 라이브 검증: 페이지 오류 0, AudioContext running (`qa/evidence/live-muzzle-vfx-deploy.png`) | web beat |
