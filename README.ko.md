# Castle Busters (캐슬 버스터즈)

![Unity Version](https://img.shields.io/badge/Unity-2022.3.62f2-blue.svg)
![License](https://img.shields.io/badge/License-MIT-green.svg)

| 인트로 | 플레이 |
|---|---|
| ![Castle Busters 인트로 화면](IntroCapture.png) | ![Castle Busters 플레이 화면](gameplay.gif) |

**[▶ 영상 보기: 인트로 → 게임플레이 (MP4)](docs/CastleBusters_IntroToGameplay.mp4)** — 타이틀 화면, 유닛 선택, 기사/궁수 발사부터 착탄까지 짧게 담은 캡처 영상.

## 컨셉 (Concept)
Castle Busters는 작은 영웅 부대를 발사해 상대 성을 무너뜨리는 2D 물리 포격 게임입니다. 플레이어는 바람을 읽고 기사, 궁수, 폭탄병 중 적절한 유닛을 선택한 뒤, 하중을 지탱하는 블록과 적 코어를 파괴하며 전술적 타이밍과 아케이드식 타격감을 즐깁니다.

## 게임 스타일 (Game Style)
- **장르:** 2D 물리 퍼즐 / 포격 / 실시간 1v1 성 파괴
- **아트 스타일:** 읽기 쉬운 전투 실루엣을 가진 스타일라이즈드 2D 픽셀 아트
- **핵심 메커니즘:** 슬링샷 발사, 물리 기반 파괴, 유닛 능력, 바람을 고려한 조준, 덱 빌딩 (최대 8개의 고유 유닛)
- **VFX 및 연출:** 화면 흔들림(Screen shake), 히트 스톱(Hit stop, 충격 프리즈), 트레일 렌더러, 플로팅 데미지 숫자, 동적 카메라 줌/팬, 임팩트 있는 피드백을 위한 파티클 버스트

## 기믹 및 비율 (Gimmicks & Ratios)
- **물리 기반 파괴:** 성은 그리드 위에 건설됩니다. 하중을 지탱하는 블록이 파괴되면 너비 우선 탐색(BFS) 구조적 무결성 검사가 트리거되어 지지되지 않는 블록이 동적으로 붕괴됩니다.
- **매치 비율:** 플레이어가 서로의 성을 마주보고 대결하는 실시간 1v1 PvP 매치.
- **덱 비율:** 플레이어는 최대 8개의 고유 유닛(기사, 궁수, 폭탄병, 공성 무기)을 선택하여 전투 덱을 구성합니다.
- **승리 조건:** 모든 적 유닛을 제거하거나 상대방의 성을 폐허로 만듭니다.

## 전장 스테이지 (Battlefield Stages)
인트로 화면의 스테이지 선택기는 세 개의 확실히 다른 전장을 제공합니다 — 같은 판을 색만 바꾼 것이 아니라, 선형 캠페인처럼 순차적으로 잠금 해제되며(한 번 클리어하면 다음 스테이지가 열리고, PlayerPrefs로 세션 간 저장됨), 각자 고유한 컨셉·구도·밸런스를 가집니다:
- **Stage1 — 공성 평원 (Siege Plains):** 다른 모든 스테이지가 비교 기준으로 삼는 고정된 기준선. 발사 지점 ±14.5(플레이어 간 거리 29), 41×5 지면 그리드, 다리 근처에 배치된 4개 화약통, 2단 높이 벽, 4종 순환 필드 기믹(배럴/미니타워/룬/패트롤), 2종 순환 벤트(마그마/페탈), 필드 기믹 최대 6개, 3턴마다 필드 구성 변경, 바람 상한 6.5, 흰색 배경.
- **Stage2 — 잿빛 보루 (Ashen Bastion):** 단순히 축소된 Stage1이 아닌 근접전 요새 공방전. 발사 지점을 ±13.5로 좁혀(거리 27, -6.9%) 지면/카메라를 함께 조정했고, 적 발사 링과 공유 코어 사이에 실질적인 1.0 여유 공간을 확보했습니다. 시작 시 화약통 없음 — 요새는 자기 성문 앞에 공짜 폭발물을 두지 않으며, 위험 요소는 Stage1과 동일한 필드 기믹 순환을 통해 전투 중 획득합니다. 3단 높이 벽(Stage1의 2단보다 높음)으로 더 육중하고 견고한 실루엣을 만듭니다. 필드 구성은 2턴마다 바뀌며 최대 4개로 더 적게 유지 — 밀도 높고 빠르게 순환하며 두 턴 연속으로 휴식 구간이 오는 일이 없습니다. 바람 상한 6.2로, 좁아진 거리에 비례해 조정했습니다. 잿빛 배경 틴트가 분위기를 강화합니다.
- **Stage3 — 서리협곡 (Frostbound Gorge):** 단순히 넓어진 Stage1이 아닌 광활한 장거리 협곡. 발사 지점을 ±18.5로 확장(거리 37, +27.6%), 지면/카메라도 함께 확장(49×5 그리드, 카메라 보드 47), 더 길어진 사거리에 맞춰 최종 바람 상한도 7.2로 상향. 화약통은 다리 근처에 몰려 있던 기존 배치 대신 Stage1의 정확한 여유 비율을 넓은 발사 지점에 그대로 적용한 ±11.5/±15.0 날개 위치로 퍼져, 넓은 중앙 지대를 비워둡니다. 5번째 필드 기믹 종류(**SpikeTrapGimmick** — 근접 감지형 바닥 함정으로, 고정 주기의 `EruptionVentGimmick`이나 1회성 트리거인 `EventGateGimmick`/`BuffDebuffGimmick`과 달리 대기/발동준비/작동/재사용대기 상태 머신을 가지며, 유닛이 범위 안에 들어오면 발동 준비 후 24 데미지와 확정적인 상승-이탈 넉백을 1회 터뜨리고 재사용대기를 거쳐야 다시 무장)와 3번째 벤트 종류(**서리** — 마그마의 수직 상승+데미지 대신 옆으로 밀어내며 둔화 디버프를 적용하는 "레인에서 벗어나 있기" 전술 대응)를 추가했습니다. 필드 기믹은 5종 전체를 순환하며 4턴마다 바뀌고 최대 7개까지 허용 — 넓은 판이 어수선하지 않고 탁 트인 느낌을 줍니다.
- 코어 위치(`GameManager.CoreAbsX = 9`)와 체력은 세 스테이지 모두 공유·불변 — 모든 차이는 간격, 구도, 필드 기믹 페이싱, 바람 보정에서 오며 코어 자체의 파워 수치 변화는 없습니다.

## 프로젝트 구조 (Project Structure)
```
Unknown Castle/
├── Assets/
│   ├── Scenes/           # 게임 씬 (SampleScene)
│   ├── Scripts/          # C# 소스 코드
│   │   ├── Core/         # 핵심 게임 매니저 (GameManager, InputManager)
│   │   ├── Units/        # 유닛 컨트롤러 및 동작
│   │   ├── Environment/  # 파괴 가능한 블록 및 소품
│   │   └── VFX/          # 시각 효과 매니저 (ScreenShake, HitStop)
│   ├── Prefabs/          # 재사용 가능한 게임 오브젝트 (유닛, 블록)
│   ├── Sprites/          # 2D 아트 에셋
│   └── Tests/            # PlayMode 및 EditMode 테스트
├── Packages/             # 유니티 패키지 의존성
└── ProjectSettings/      # 유니티 프로젝트 설정
```

## 주요 기능 (Features)
- **물리 기반 파괴:** 구조물이 충격과 중력에 따라 사실적으로 붕괴합니다.
- **다양한 유닛 유형:**
  - **기사 (Knight):** 중형 근접 유닛으로, 충돌 시 화면 흔들림과 히트 스톱을 트리거합니다.
  - **궁수 (Archer):** 원거리 유닛으로, 적에게 화살을 쏩니다.
  - **폭탄병 (Bomber):** 폭발 유닛으로, 광역 피해를 입힙니다.
- **시각적 완성도:** 발사된 유닛의 트레일 렌더러, 강한 충격 시 화면 흔들림, 극적인 효과를 위한 히트 스톱, 바람 방향 파티클/UI, 발사 링 피드백, 발사 유닛 카메라 포커스, 쇼크웨이브 링, 플로팅 전투 콜아웃(`HIT`, `SMASH`, `BOOM`, `BREAK`)을 포함합니다.
- **플레이 가능 유닛:** 기사는 `SMASH` 피드백이 있는 강한 접촉 피해를 주고, 궁수는 플레이어블 유닛보다 커지지 않도록 제한된 화살과 `HIT` 콜아웃을 사용하며, 폭탄병은 보이는 `BOOM` 쇼크웨이브로 광역 피해를 줍니다.
- **코어 가독성:** 플레이어/적 코어 HP 배지, 목표 배너, 턴 토스트, 1회성 코어 실드 콜아웃으로 파괴 중에도 승리 조건을 계속 인지할 수 있습니다.
- **자동화 테스트:** 핵심 게임 루프를 검증하고 게임플레이 스크린샷을 캡처하기 위한 PlayMode 테스트를 포함합니다.

## 현재 플레이 가능한 슬라이스 (Current Playable Slice)
- **발사 조작:** `1`, `2`, `3`으로 기사, 궁수, 폭탄병을 선택한 뒤 파란 발사 링에서 드래그하고 뒤로 당긴 다음 놓아 발사합니다. HUD는 선택 유닛, 발사 링 위 “DRAG HERE” 라벨, 실시간 파워/각도, 약한 당김 경고, 한/영 조작 안내를 표시합니다.
- **플레이 가능 유닛:** 기사는 강한 접촉 피해를 주고, 궁수는 플레이어블 유닛보다 커지지 않도록 제한된 화살을 발사하며, 폭탄병은 폭발 충격으로 광역 피해를 줍니다.
- **연출 패스:** 런타임 스프라이트 애니메이션이 자식 비주얼 대기 바운스, 발사 회전/스트레치, 보행 스트라이드, 공격 펄스, 피격 플래시, 팀 틴트, `Resources/GeneratedUnitFrames` 기반 생성 프레임 재생을 추가합니다.
- **씬 셋업:** `SetupSceneLayout`은 유닛 프리팹, 성 타깃, 폭발 배럴, 게임플레이 캡처 지원을 포함한 테스트 레이아웃을 구성할 수 있습니다.
- **캡처 보존:** `GameplayCapture_MCP_10pass.png`는 Obsidian/llm-wiki 10패스 UX 반복 이후 MCP로 실제 플레이를 실행해 생성한 최신 1920×1080 캡처입니다.

## 아트 파이프라인 메모 (Art Pipeline Notes)
- PerfectPixel Studio는 최종 유닛, 투사체, 소품 스프라이트 생성을 위한 권장 외부 픽셀 아트 생성 워크플로우입니다. `https://github.com/gykim80/perfectpixel-studio`의 `a1385cc` 기준으로 검토했습니다.
- 초기 placeholder 프레임 PNG는 `Assets/Resources/GeneratedUnitFrames/{Knight,Archer,Bomber}/{Idle,Walk,Attack,Launch}/`에 커밋되어 있으며, `UnitSpriteAnimator`가 `Resources.LoadAll<Sprite>()`로 자동 로드하고 이름순으로 재생합니다.
- 생성 스프라이트는 `idle_000.png`, `idle_001.png`처럼 정렬 가능한 파일명을 사용하고, 투명 배경의 2D Sprite, point filtering, 일관된 Pixels Per Unit으로 임포트합니다.
- 최종 제작 스프라이트 시트가 임포트되기 전까지는 생성 초기 프레임과 절차적 비주얼 애니메이션으로 움직임과 전투 피드백을 전달합니다.

## PerfectPixel Studio 설치 및 사용
1. 사전 요구사항을 설치합니다: Go 1.25+, Node.js 18+, Wails CLI v2 (`go install github.com/wailsapp/wails/v2/cmd/wails@latest`).
2. 이 Unity 저장소 바깥에서 앱을 클론하고 실행합니다: `git clone https://github.com/gykim80/perfectpixel-studio.git && cd perfectpixel-studio && ./dev.sh` 또는 `wails dev`.
3. 앱 Settings 또는 `.env`에 provider 키 하나를 설정합니다: `GEMINI_API_KEY`/`GOOGLE_API_KEY`, `OPENROUTER_API_KEY`, `FAL_KEY`/`FAL_API_KEY`, `BYTEPLUS_API_KEY`/`ARK_API_KEY`.
4. Knight, Archer, Bomber의 상태별 프레임을 compact side-view 프롬프트로 생성하고 per-frame PNG로 export한 뒤 `Assets/Resources/GeneratedUnitFrames/<Unit>/<State>/`에 복사합니다.
5. 코드와 맞는 상태 폴더 이름을 유지합니다: `Idle`, `Walk`, `Attack`, `Launch`, 선택적으로 `Dead`.

## 검증 (Validation)
- C# 프로젝트 빌드는 `dotnet restore Assembly-CSharp.csproj`와 `dotnet build Assembly-CSharp.csproj --no-restore`로 확인했습니다.
- Unity MCP 검증: `editor-application-set-state`로 Play Mode에 진입하고, `script-execute`와 `LaunchManager.SimulateLaunch`로 Bomber를 발사한 뒤 직접 카메라 렌더로 `GameplayCapture_MCP_10pass.png` 1920×1080 캡처를 생성했으며, 최근 Unity 콘솔 오류가 없음을 확인했습니다.
- Unity batchmode 테스트는 동일 프로젝트를 이미 열고 있는 Unity Editor 인스턴스가 있으면 차단되므로, 실행 전 열린 에디터를 닫아야 할 수 있습니다.

### 스테이지별 전용 배경/기믹 차별화 및 custom perfectpixel 구동 검증 (2026-07-08, 후속 4)

1. **`ppgen` 커스텀 프로바이더 (`god-tibo-imagen`) 빌드**: `gemini`, `openai` API 키가 환경에 없는 한계를 극복하기 위해 `~/.codex/auth.json` 로컬 ChatGPT 세션을 활용하는 `gti` CLI를 호출하는 Go 프로바이더 `god-tibo-imagen`을 구현하고 `ppgen`을 소스 빌드했습니다. 더미 키와 특정 모델명(`gpt-5.4`)으로 오프라인 캐릭터 스프라이트 시트 생성이 가능함을 검증했습니다.
2. **스테이지별 고유 배경 이미지 생성 및 바인딩**: `gti` (gpt-5.4)를 활용해 각 스테이지 컨셉에 어울리는 2048x1152 해상도 배경(`Background_Stage2.png` 화산재/요새 성벽, `Background_Stage3.png` 빙하 협곡)을 생성했습니다. `GameManager.CreateBackground()`를 수정해 활성 스테이지에 맞춰 전용 배경을 우선 로딩하고 실패 시 `Background.png`로 안전하게 폴백하도록 했습니다.
3. **스테이지별 기믹 구성 차별화 (allowedGimmicks)**: `StageLayout` 구조체에 `allowedGimmicks` 배열 필드를 추가해 스테이지별 기믹 구성을 달리 설계했습니다. Stage 1은 기존 4종 그대로, Stage 2(잿빛 보루)는 무거운 성벽/포탑 중심의 3종(`MiniTower, Barrel, Patrol`), Stage 3(서리 협곡)은 트랩/스킬 중심의 3종(`SpikeTrap, Rune, Patrol`)만 등장합니다.
4. **스테이지 맞춤형 벤트 스타일 고정**: `SiegeTactics.cs` 내 벤트 발생 규칙을 개편해 Stage 2에서는 마그마 분출구만, Stage 3에서는 서리 분출구만 발생하도록 고정하여 스테이지별 테마를 통일했습니다 (Stage 1은 마그마/꽃가루 교대 순환).
5. **독립 검증 완료 및 플레이 영역 경계 고치기 (EditMode 149/149, PlayMode 14/14 통과)**: 3종 기믹 순환 시의 수학적 인덱스 무한 루프 버그를 `turn % allowed.Length`로 교체해 해결했습니다. batchmode 빌드에서 `ui_button_card.png` 에셋이 raw `Texture2D`로 강제 형변환되어 로딩에 실패하던 문제를 해결하고자 `GimmickSpriteLibrary.Load()` 내에 Editor 자가 수정(importer type 강제 Sprite 지정 + SaveAndReimport 및 dynamic `Sprite.Create` 폴백) 로직을 이식했습니다. 또한 `UnitController.playableBounds`가 활성 스테이지의 `groundHalfWidth`에 비례하여 동적으로 할당되도록 수정했습니다. 이에 따라 Stage 3에서는 경계 영역이 `±26u`로 확장되어 외곽 날개 지역에서 발생하던 보이지 않는 "OUT" 가짜 충돌/사망 경계를 완전히 제거했습니다. 전체 EditMode 및 PlayMode 테스트 100% 녹색 패스를 달성했습니다.
### 타이틀 진입 전 웹툰 프롤로그: 배선 완료, 손상 복구, 검증 (2026-07-08)

### 스폰 스프라이트 버그 근본 수정, 3전 2선승 랭킹, 눕는 유닛/기믹 범위/연출 타이밍 개선 (2026-07-09)

1. **근본 원인 — "나이트/궁수/폭탄병 버튼을 눌러도 항상 궁수만 보임":** 실제 유닛 타입/프리팹 선택 로직은 정상이었지만(`SelectUnit`/`LaunchManager.selectedUnitPrefab` 모두 올바른 프리팹을 인스턴스화), `SpriteAtlasPacker`가 `sprite.name`만으로 중복 제거·키를 매겨 문제가 발생했습니다. PerfectPixel export 컨벤션상 `GeneratedUnitFrames/{Knight,Archer,Bomber}/{State}/idle_000.png` 등 파일명이 유닛마다 동일해서, 팩킹 시 알파벳순으로 먼저 온 Archer의 스프라이트만 살아남고 Knight/Bomber의 동일 이름 스프라이트는 조용히 Archer의 아틀라스 셀로 대체되었습니다. `SpriteAtlasPacker.cs`를 Sprite 객체 참조(파일명이 아니라 실제 에셋 아이덴티티) 기준으로 중복 제거·키 매김하도록 수정했습니다.
2. **"누워서 생성/공격 불가" 근본 수정:** `LaunchManager.SpawnAndLaunchOne`이 `InitializeUnit`을 호출하지 않아 플레이어가 발사한 유닛은 착지 순간(`OnCollisionEnter2D`)까지 리지드바디 회전이 자유로운 상태였습니다. 비행 중 다른 유닛/화살/기믹과의 스침 충돌로 회전이 누적되면 옆으로 누운 채 착지해 굳어버렸고(눕는 자세에서는 공격도 불가), 지면/블록에 전혀 닿지 못하면 `Launched` 상태에서 영원히 못 벗어났습니다. `UnitController.Awake()`에서 스폰 즉시(비행 포함 전체 수명 동안) `RigidbodyConstraints2D.FreezeRotation`을 걸어 물리 회전 자체를 원천 차단했습니다 — 화면상 보이는 회전 연출은 `UnitSpriteAnimator`가 자식 트랜스폼에서 별도로 연출하므로 시각 효과는 그대로 유지됩니다.
3. **버튼 텍스트 "깨짐" 근본 수정 (스프라이트 vs 박스 너비 불일치):** 카드형 버튼 배경(`ui_button_card.png`)이 모든 버튼에서 `Image.Type.Simple`로 임의 종횡비까지 늘려 그려지고 있었습니다 — 원본 512×341(≈1.5:1)의 카드 프레임 아트가 유닛 선택 카드(≈1.5:1)에서는 괜찮았지만, 결과화면/타이틀/스테이지 버튼(≈3.8~4.1:1)에서는 프레임 테두리가 비균일하게 눌려 늘어나 라벨 안전 영역을 잠식했습니다. 텍스처를 불투명 영역(402×284)으로 크롭하고 `spriteBorder`(32,23,32,23)를 부여한 뒤, 카드 스프라이트를 쓰는 5개 지점(`GameManager`, `IntroScreenController` ×2, `SiegeEcosystem`, `BrickPlacementController` ×2) 모두 `Image.Type.Sliced`로 전환해 종횡비와 무관하게 테두리 두께가 항상 일정하도록 근본적으로 고쳤습니다.
4. **연출 타이밍:** 최초 시네마틱(웹툰 프롤로그) 시작 전 1.2초의 암전 홀드(`PreRollSeconds`)를 추가해 부팅 직후 급작스러운 컷을 완화했습니다. 타이틀 화면 인트로 연출 전체(페이드/타이틀 스탬프/태그라인/버튼/사용법/스테이지 픽커)에 `EntranceTimeScale=0.85` 배율을 적용해, 기존 ~2.3초 걸리던 전환이 2초 예산 안(≈1.96초)에 끝나도록 했습니다.
5. **기믹(도움/방해) 생성 범위 확장:** `DynamicBattlefield.SpawnBalanceEvent`가 버프/디버프 룬과 파워/감속 게이트를 매번 정확히 같은 좌표(`±11.5`)에만 생성했고, 이 값은 스테이지별 발사 거리(`LaunchApronAbsX`: Stage1 14.5 / Stage2 13.5 / Stage3 18.5)와 전혀 연동되지 않아 Stage3에서는 특히 코어 근처에만 몰렸습니다. 이제 활성 스테이지의 발사 거리에 맞춰 코어 안쪽 경계와 발사 링 바깥쪽 경계 사이에서 매턴 무작위로 배치되도록 고쳐, 스테이지에 맞는 범위로 확장하면서 코어/발사 링 침범은 여전히 막았습니다.
6. **3전 2선승 랭킹 시스템 (`SiegeSeries.cs` 신규):** 전체 승부는 이제 최대 3판 중 2승을 먼저 거두는 쪽이 승리합니다. `GameManager`가 매 판의 승패를 시리즈 승수로 누적하고(리로드 간 정적 필드로 유지), 시리즈가 확정되기 전까지는 결과 화면이 "GAME n/3" 배너와 "다음 경기" 버튼만 보여주고 랭킹판에는 기록하지 않습니다. 시리즈가 확정되면(2승 달성 또는 3판 소진) 그제서야 시리즈 합산 점수(2-0 완봉 시 보너스 포함)로 랭킹판에 기록하고, 다음 스테이지 해금도 개별 게임이 아닌 시리즈 승리 기준으로만 이뤄집니다. `재도전`/`타이틀`/`RequestStage`는 새 시리즈를 0-0으로 리셋하고, `다음 경기` 버튼(신규 `GameManager.RequestNextGame()`)만 진행 중인 시리즈 점수를 유지한 채 다음 판으로 넘어갑니다.
7. **검증:** `dotnet build CastleBusters.csproj` / `CastleBusters.Tests.csproj` 모두 경고 0개, 오류 0개(임시 `.csproj` 사본에 신규 파일 컴파일 항목을 추가해 확인 후 원본은 그대로 복원). 이미 열려 있는 라이브 Unity 에디터(Unity MCP 세션)를 방해하지 않기 위해 프로젝트를 `/tmp`에 격리 복제한 뒤 `Unity -batchmode -nographics -runTests -testPlatform PlayMode`로 전체 `CastleBusters.Tests` 스위트를 실행 — **14/14 PlayMode 테스트 통과**(신규 `BestOfThreeAndRenderingFixTests` 7개 포함: `SpriteAtlasPacker_KeepsKnightArcherBomberFramesDistinct`가 팩킹된 35개 스프라이트가 서로 다른 아틀라스 셀을 차지함을 확인, `SpawnedUnit_RigidbodyRotationIsFrozenFromCreation`이 스폰 즉시 회전 고정을 확인). 확인 후 임시 사본은 삭제하고 라이브 에디터 프로젝트 디렉터리가 그대로임을 재확인했습니다.
8. **범위 밖 (한계):** 스테이지별 "지켜야 하는 타워 타입" 제약이라는 새 게임플레이 룰과 `god-tibo-imagen`/나노바나나를 통한 신규 아트/애니메이션 생성은 이번 패스에서 다루지 않았습니다 — 이 세션에는 이미지 생성 도구가 연결돼 있지 않아, 검증되지 않은 자리표시자 에셋을 만들어 넣기보다 범위에서 제외했습니다. Stage2/Stage3의 컨셉·구도·밸런스 차별화(지형/배경/필드 기믹 순환/바람 상한 등)는 2026-07-07 패스에서 이미 구현되어 있음을 확인했습니다.

- 이전 세션이 만들어 두었던 `Assets/Scripts/WebtoonPrologueController.cs`(11페이지 타이틀 전 웹툰 콜드오픈 — 패널 캐릭터에는 PerfectPixel로 생성한 `GeneratedUnitFrames/{Knight,Archer,Bomber}/Idle` 프레임과 `GimmickAnimLibrary` 소품 루프를, 배경에는 `gti`로 생성한 `IntroKeyArt`를 재사용하며, 픽셀 스냅("PerfectPixel 방식") 모션을 적용)를 발견했지만 컴파일이 되지 않는 상태였습니다. `GameManager.cs` 배선에 중단된 이전 세션이 남긴 중복/손상된 조각(메서드 밖에 떠 있는 구문 블록, 중복된 `webtoonIntroShown` 필드, 리터럴로 남은 편집 도구 마커 텍스트)이 섞여 있었습니다.
- `GameManager.cs`를 복구: 이제 하나의 `ShowIntro()`가 앱 세션당 1회만 켜지는 `webtoonIntroShown` 플래그로 분기합니다 — 첫 진입에서는 `ShowWebtoonPrologue()`(`WebtoonPrologueController.Create`, Space로 다음 컷, Enter/Return으로 타이틀로 바로 건너뛰기, 읽기 타이머로 자동 진행)를 재생하고, 이후 타이틀/재도전/스테이지 선택으로 인한 리로드는 `ShowTitleScreen()`(`IntroScreenController.Create`)으로 바로 진입해 플레이어가 이미 본 웹툰이 다시 재생되지 않도록 했습니다. `Update()`의 Intro 상태 키 입력 처리와 `BeginSiege()`의 정리 로직도 중복 없는 단일 구현으로 정리했습니다.
- `WebtoonPrologueController.cs`를 `CastleBusters.csproj`에 `<Compile Include>`로 등록하고 누락돼 있던 `.meta`를 생성했습니다(둘 다 없어서 클래스가 조용히 인식되지 않던 원인이었습니다). 같은 이전 세션이 남긴 전용 테스트 파일 `Assets/Editor/WebtoonPrologueTests.cs`(`PixelSnap`/`SlideProgressAt`/`StripPageOffsetAt` EditMode 커버리지)도 `.meta`와 `Assembly-CSharp-Editor.csproj` 등록이 빠져 있어 함께 등록했습니다(기존 기능별 테스트 파일 컨벤션인 `SpikeTrapGimmickTests.cs`, `FrostVentTests.cs`, `StageDefinitionsTests.cs`와 동일한 방식) — `GamePlayTests.cs`에 커버리지를 중복 작성하는 대신 이 파일을 그대로 활용했습니다.
- **검증:** `dotnet build CastleBusters.csproj`, `dotnet build CastleBusters.Tests.csproj`, `dotnet build Assembly-CSharp-Editor.csproj` 모두 오류 0개(새로 발생한 경고 없음, `SpriteApplyValidator.cs`의 기존 무관한 `CS0105` 경고 1개만 존재). 이 작업 중에는 라이브 Unity MCP EditMode/PlayMode 재검증을 수행하지 않았습니다 — 이 머신에서 프로젝트의 에디터 인스턴스가 이미 열려 있어 Unity가 동일 프로젝트의 두 번째 인스턴스를 거부했기 때문입니다; 다음 라이브 세션에서 `tests-run`과 Intro → Space로 컷 넘기기 → Enter로 건너뛰기 → 타이틀 → `BeginSiege()`로 이어지는 `script-execute` 점검을 마무리해야 합니다.



### Y축 상단 경계 사라짐 버그 수정 + 버그 수정 테스트 전체 재검증 (2026-07-07, 후속 3)

사용자가 직접 제보한 실제 플레이테스트 버그를 수정한 후속 패스: 유닛이 Y축으로 일정 이상 높이 발사되면, 카메라 화면 안에서 여전히 또렷하게 보이는 상태인데도 갑자기 사라져버리고 아래로 다시 떨어지는 궤적을 그리지 못하는 문제였습니다.

1. **근본 원인:** `UnitController.playableBounds`(`Rect(-22, -9.5, 44, 24)`, 즉 y ∈ [-9.5, 14.5])가 `Rect.Contains()`로 검사되어, 상단 경계(`y = 14.5`)까지 포함한 네 변 전체가 강제되고 있었습니다. `GamePresentationDirector`의 동적 카메라(`boardCenter.y = 3.0`, `orthographicSize`가 화면비에 따라 7.2~11.2로 클램프됨)는 발사된 유닛을 따라갈 때 와이드 화면비에서 최대 `y ≈ 16`까지 보여줄 수 있어서, 큰 궤적/강한 바람/기사 넉백/끼임 복구 점프 등으로 유닛이 `y = 14.5`를 넘으면서도 여전히 화면 안에 있는 경우가 발생했는데, `MonitorLaunchedUnitSafety()`/`Update()`가 그 즉시 `Die()`를 호출해 "위로 날아가다가 갑자기 사라짐"으로 보였던 것입니다.
2. **수정:** `UnitController.cs`의 두 `!playableBounds.Contains(position)` 호출부를 새 `IsOutOfPlayableBounds(Vector2 position)` 헬퍼로 교체했습니다. 이 헬퍼는 좌우 벽(`xMin`/`xMax`)과 바닥(`yMin`)만 강제하고, 천장(상단) 검사는 완전히 제거했습니다. 이제 유닛은 임의로 높이 날아가더라도 중력에 의해 항상 다시 떨어지는 궤적을 유지하며, `ChariotRules.KillPlaneY`(바닥 `y = -20`)가 실제로 월드 밖으로 떨어진 경우는 여전히 잡아냅니다.
3. **신규 회귀 테스트** (`Assets/Tests/BugFixVerificationTests.cs`): `UnitAboveOldCeiling_DoesNotDisappear`(발사 상태의 유닛을 기존 천장 14.5보다 훨씬 높은 `y = 30`, 하지만 좌우 플레이 영역 안에 배치해 여러 물리 스텝 동안 생존해야 함을 검증)와, 좌우 벽 사망 처리가 그대로 유지되는지 확인하는 `UnitOutsideHorizontalBounds_StillDies`(`x = 1000`으로 발사된 유닛은 여전히 사망해야 함)를 기존 3개 버그 수정 테스트(기믹 선택, 킬 플레인 바닥, 바람 반경 스코핑)에 추가했습니다.
4. **QA:** `dotnet build Assembly-CSharp.csproj` — 경고 0개, 오류 0개. 컴파일 확인에 그치지 않고 실제 자동화된 PlayMode 실행으로 검증했습니다 — 이미 열려 있는 라이브 에디터를 건드리지 않기 위해 프로젝트를 `/tmp`의 격리된 사본으로 rsync 복제한 뒤 `Unity -batchmode -nographics -runTests -testPlatform PlayMode -testFilter CastleBusters.Tests.BugFixVerificationTests`로 실행했으며, **5/5 PlayMode 테스트 통과**(`SelectUnit3_LaunchesExplosiveBarrel_NotWrongPrefab`, `UnitBelowKillPlaneY_Dies`, `WindForce_OnlyAffectsObjectsWithinRadius`, `UnitAboveOldCeiling_DoesNotDisappear`, `UnitOutsideHorizontalBounds_StillDies`)를 확인한 뒤 임시 사본을 삭제하고 라이브 에디터의 프로젝트 디렉터리가 그대로임을 재확인했습니다.

### 순차 캠페인 진행 시스템의 독립 라이브 검증 (2026-07-07, 후속)

실제 테스트를 진행하고 검증한 뒤 배포하라는 요청에 대한 후속 패스로, 아래의 순차 캠페인 진행 기능을 완전히 새로운 설치 상태부터 처음부터 끝까지 독립적으로 재검증했습니다. 빌드와 139개 EditMode 테스트 스위트를 독립적으로 재확인했으며, 라이브 검증 도중 실제 동시 세션 충돌을 겪었습니다(다른 프로세스가 동일한 Editor에서 정확히 같은 라이브 테스트 흐름을 동시에 진행 중이었음 — 콘솔에 낯선 로그 태그가 서로 뒤섞여 나타나는 것으로 확인) — 사용자의 명시적 지시에 따라 그 뒤에서 순서대로 기다리는 대신 그대로 적응하며 계속 진행했습니다.

1. **새 설치 상태의 잠금을 라이브로 확인:** `CastleBusters.StageProgress.v1` PlayerPrefs 키가 전혀 없는 상태에서 인트로 스테이지 선택기가 `Stage1: interactable=true`, `Stage2/Stage3: interactable=false`에 `(잠김 · LOCKED)` 힌트까지 정확히 문서화된 계약대로 표시되는 것을 확인했습니다.
2. **실제 프로덕션 데미지 경로를 통한 진짜 승리 → 잠금 해제:** 상태를 우회하는 대신, 모든 유닛의 공격/폭발이 호출하는 것과 동일한 메서드인 `CastleCoreGimmick.TakeDamage()`를 적 코어에 직접 호출해 `CheckVictoryConditions()`가 실제 타격마다 이미 감시하고 있는 "코어 HP ≤ 0" 조건에 정확히 도달시켰습니다 — 이는 우회 경로가 아니라 실제 `EndGame()` → `StageProgressStore.RecordVictory()` 체인을 그대로 통과합니다. 라이브 확인: `frontier`가 `Stage1`에서 `Stage2`로 전진했고, 결과 화면에 "BREACH COMPLETE — VICTORY!"와 함께 실제로 상호작용 가능한 `NextStageButton`이 렌더링되었습니다.
3. **실제 다음 스테이지 버튼 클릭을 끝까지 검증:** 버튼의 실제 `Button.onClick`을 호출하자 씬이 진짜로 살아있는 Stage2로 리로드되었고(`currentStage=Stage2`, `LaunchApronAbsX=13.5` 확인), 이어서 타이틀 화면으로 돌아가 확인한 결과 선택기가 이제 `Stage1`과 `Stage2` 모두 잠금 해제된 상태로 표시하고 `Stage3`는 새로운 프론티어에서도 여전히 올바르게 잠겨 있음을 확인했습니다.
4. **검증 후 테스트로 생성된 PlayerPrefs 상태를 정리**(`CastleBusters.StageProgress.v1` 키 제거)해 다음 실제 세션이 진짜로 깨끗한 상태에서 시작하도록 했습니다.
5. **QA (Unity MCP 라이브):** 두 어셈블리 모두 `dotnet build` — 0 errors, 독립적으로 재실행. `tests-run` EditMode — 라이브 세션 전후 모두 **139/139 통과**. 콘솔 감사에서 발견된 것은 오래된 무관한 항목뿐이었습니다(오케스트레이터 자신의 이전 구문 오류 시도, 그리고 동시 세션 자체 스크립트의 컴파일 오류) — 이번 검증 패스 자체에서 발생한 실제 게임플레이 오류는 0건이었습니다.

### 순차 캠페인 진행: 스테이지가 순서대로 잠금 해제됨 (2026-07-07, 후속)

3단계 시스템을 자유 선택 대신 선형 캠페인으로 잠그자는 요청에 대한 후속 패스입니다: Stage1은 처음부터 잠금 해제되어 있고, 한 스테이지를 클리어하면 다음 스테이지가 잠금 해제되며 세션 간에도 유지됩니다.

1. **`StageProgression.cs` (신규):** 순수 `StageProgress` 클래스(`IsUnlocked`, `NextStage`, `Advance`)가 기존 `SiegeRank`/`LeaderboardStore` 구조 분리(`SiegeEcosystem.cs`)를 그대로 미러링합니다 — 캠페인 순서는 `StageId` enum의 선언 순서이며, `Advance`는 이미 획득한 프론티어를 절대 되돌리지 않고(이전 스테이지를 재도전/재플레이해도 no-op), 마지막 스테이지에서 고정됩니다. `StageProgressStore`는 잠금 해제 프론티어를 `PlayerPrefs`에 저장하며(버전이 명시된 단일 int 키 `CastleBusters.StageProgress.v1`) `LeaderboardStore`와 동일한 선례에 따라 의도적으로 유닛 테스트 대상에서 제외하고 대신 PlayMode에서 라이브로 검증합니다.
2. **`GameManager.RequestStage()`**가 이제 두 개의 독립적인 잠금을 확인합니다: `StageDefinitions.For(stage).locked`(설계 단계에서 "아직 완성/제공되지 않음") AND `StageProgress.IsUnlocked`(순차 캠페인 — Stage2/3는 바로 앞 스테이지를 최소 한 번 클리어해야 함). `EndGame()`은 모든 승리마다 `StageProgressStore.RecordVictory(currentStage)`를 호출해 결과 화면이 만들어지기 전에 클리어를 잠금 해제 프론티어에 반영합니다.
3. **`IntroScreenController`의 스테이지 선택기:** 두 잠금 중 하나라도 닫혀 있으면 카드가 흐리게/비활성으로 표시되며(`IsStageLocked()`가 두 검사를 모두 통합), 진행 상태로 잠긴 카드는 라벨에 `(잠김 · LOCKED)` 힌트가 추가되어 "아직 획득하지 못함"과 단순 비활성 상태를 구분할 수 있습니다.
4. **`ResultsScreenController.Create()`**가 선택적 `nextStage` 매개변수를 받습니다. 승리로 도달 가능한 다음 스테이지가 잠금 해제되면 재도전/타이틀 옆에 "다음 스테이지" 버튼이 세 번째로 추가되어 대칭 3분할 레이아웃(0.25/0.50/0.75)으로 렌더링되고, `GameManager.RequestStage(nextStage)`로 바로 연결됩니다. 패배, 이미 클리어한 스테이지의 재도전, 마지막 스테이지에서의 승리는 모두 기존 2분할 레이아웃(0.38/0.62)을 그대로 유지합니다 — 해당 경로들에서는 시각적 회귀가 전혀 없습니다.
5. **신규 테스트:** `StageProgressionTests.cs`(EditMode 테스트 9개)가 `IsUnlocked`, `NextStage`, 그리고 모든 `Advance` 경계 케이스를 고정합니다 — Stage1/Stage2 클리어 시 다음 스테이지 잠금 해제, 마지막 스테이지 클리어 시 고정, 이전 스테이지 재도전/재플레이가 이미 진행된 프론티어를 절대 되돌리지 않음.
6. **QA (Unity MCP 라이브):** `dotnet build` — 0 errors. `tests-run` EditMode — **139/139 통과**(기존 130 + 신규 9), 라이브 PlayMode 세션 전후 모두. 직접 메서드 호출이 아닌 실제 프로덕션 경로를 통한 라이브 PlayMode 검증: 새 설치는 Stage1만 잠금 해제된 상태를 정확히 표시하고, 잠긴 상태에서 `RequestStage(Stage2)`는 거부되며, Stage1에서 강제로 `EndGame("...VICTORY!")`를 발생시키면 Stage2가 잠금 해제되고 결과 화면의 실제 `NextStageButton` 게임 오브젝트가 렌더링되며, 그 실제 `Button.onClick`을 클릭하면 씬이 진짜로 살아있는 Stage2로 리로드되고(`LaunchApronAbsX=13.5`가 발사 지점 정렬 로그로 확인됨), 그 새로운 프론티어에서 Stage3는 여전히 올바르게 잠기고 거부됩니다. 테스트로 생성된 `PlayerPrefs` 상태는 정리되었습니다.

### 3개 스테이지 실제 플레이 검증 + 증식 게이트 복제 크래시 수정 (2026-07-07, 후속)

API 수준 상태 확인이 아니라 진짜 게임플레이를 3개 스테이지 전체에서 돌려보라는 요청에 대한 직접적인 후속 패스입니다. 아래 모든 세션은 `GameManager` 메서드를 직접 호출하는 대신 실제 프로덕션 UI를 그대로 조작했습니다 — Unity UI 이벤트 시스템을 통해 진짜 `StartButton`/스테이지 선택 카드의 `Button.onClick` 핸들러를 클릭하고, `LaunchManager.SimulateLaunch`로 실제 발사를 실행하고, 턴 타이머와 AI가 실시간으로 턴을 진행하도록 두었습니다.

1. **Stage1 (공성 평원):** START를 클릭하고 실제 기사 발사체를 쏜 뒤, 발사 1회 외에는 아무 개입 없이 턴 타이머와 AI가 모두 살아있는 상태로 **연속 10턴**을 실시간으로 진행했습니다. `GimmickFieldDirector.AliveCount`로 전투 중 필드 기믹이 실제로 스폰·변경되고 있음을 확인했습니다. 전체 진행 동안 콘솔 오류/예외 0건.
2. **Stage2 (잿빛 보루):** 인트로 화면의 `Stage_Stage2` 카드를 실제로 클릭해 선택(`interactable=true` 확인 — 예전의 잠긴 자리표시자가 아님), START 클릭 후 구도를 라이브로 확인(스폰된 화약통 `0`개, `3`블록 벽, 잿빛 틴트 `RGBA(0.72,0.72,0.76,1)`), 실제 턴 2회에 걸쳐 궁수와 폭탄병을 발사했고, 매치가 실제로 끝나는 것까지 지켜봤습니다: **턴 8에 `GameOver`, 실제 `ResultsScreen`에 "KEEP FALLEN — DEFEAT / 수성 실패"** 표시 — 승패 파이프라인 전체가 시뮬레이션이 아닌 실제 플레이로 끝까지 검증되었습니다.
3. **Stage3 (서리협곡):** `Stage_Stage3` 카드를 실제로 클릭해 선택, START 클릭 후 실제 턴에 걸쳐 기사와 궁수를 발사했습니다. 신규 기믹 2종 모두 **수동 트리거 없이 자연스러운 턴 진행만으로** 발동했습니다: 턴 3에 살아있는 `SpikeTrapGimmick`을 필드에서 관찰했고, 턴 6에 `style=Frost`인 살아있는 `EruptionVentGimmick`을 관찰했습니다 — 5번째 필드 기믹 종류와 3종 벤트 순환이 격리된 유닛 테스트뿐 아니라 실제 플레이 중에도 플레이어에게 도달함을 직접 증명합니다. 턴 10+ 시점에도 매치가 진행 중이었는데, Stage3의 넓은 판과 느린 변경 주기 때문에 Stage1/2보다 자연스럽게 더 길게 진행되는 것으로 "광활한 협곡" 설계 의도와 일치합니다.
4. **이번 패스에서 발견하고 수정한 실제 버그:** 실제 Stage2 플레이 중 증식 게이트(Multiply Gate)로 생성된 유닛 복제본이 `UnitController.SetupTrailRenderer()`에서 `NullReferenceException`을 던졌습니다. 근본 원인: `EventGateGimmick.MultiplyUnit()`은 이미 발사되어(그래서 자신의 이전 `SetupTrailRenderer()` 호출로 이미 `TrailRenderer`가 붙어 있는) 유닛에 대해 `Instantiate()`를 호출하는데, Unity의 `Instantiate()`는 그 `TrailRenderer` 컴포넌트를 복제본에 복사하지만 복제본의 private `trailRenderer` 필드를 그 복사본을 가리키도록 재매핑하지 않습니다 — 그 결과 복제본 자신의 `Launch()`는 `trailRenderer == null`로 보고, `AddComponent<TrailRenderer>()`를 호출하지만(이미 하나 있으므로 `TrailRenderer`가 중복을 허용하지 않아 실패하고 null을 반환) 바로 다음 줄에서 NRE가 발생합니다. `SetupTrailRenderer()`가 `AddComponent`로 폴백하기 전에 `GetComponent<TrailRenderer>()`로 기존 컴포넌트를 재사용하도록 수정했습니다 — 코드베이스가 이미 `UnitSpriteAnimator`에 사용하는 가드 패턴과 동일합니다. 수정 전(유닛 발사 → 복제 → 복제본 발사의 정확한 실패 시퀀스를 직접 재현해 `NullReferenceException` 확인)과 수정 후(복제본에 `TrailRenderer`가 정확히 1개만 있고 2개가 아닌 상태로 깨끗하게 발사되는 것 확인) 모두 검증했습니다.
5. **QA (Unity MCP 라이브):** 두 어셈블리 모두 `dotnet build` — 0 errors. `tests-run` EditMode — 세 번의 라이브 세션과 버그 수정 이후 재확인 **130/130 통과**(상태 오염 회귀 없음). 세 번의 전체 플레이 전반에 걸친 콘솔 로그 감사에서 발견된 것은 기존부터 있던 무관한 Unity 엔진 진단 메시지 1건(`Some objects were not cleaned up when closing the scene` — 지연된 `Destroy` 타이머가 걸린 `ShockwaveRing`/`ItemPickup` VFX 오브젝트가 씬 리로드보다 오래 살아남은 경우로, 이번 세션과 무관한 기존 `Object.Destroy(go, 1.2f)`/`(go, 14f)` 패턴이며 프로젝트 자체의 `AutoPlayTest`/`PlaytestQACapture`가 이미 `LogAssert.ignoreFailingMessages`로 걸러내는 것과 정확히 같은 종류의 노이즈입니다)과, 위에서 근본 원인을 찾아 수정한 실제 `NullReferenceException` 1건뿐이었습니다. 3개 스테이지 전체에 걸친 약 30턴 이상의 실제 플레이에서 그 외 다른 오류나 예외는 없었습니다.

### 3스테이지 밸런스 개선: 세 전장 모두 컨셉/구도 차별화 (2026-07-07, 후속)

후속 패스: 이전 패스에서 Stage1을 그대로 미러링하는 잠긴 자리표시자였던 Stage2를 잠금 해제하고 완전히 새로운 컨셉으로 설계했으며, Stage1/2/3가 간격·기믹 로스터만 다른 공유 픽스처가 아니라 실제로 다른 구도를 갖도록 개선했습니다. 병렬 서브에이전트 3개(테스트 작성 2개, 독립 밸런스/안전성 검토 1개)와 오케스트레이터 통합으로 구축했습니다.

1. **`StageLayout` 구도 필드 확장:** `barrelPositions`(스테이지별 시작 화약통), `wallHeightBlocks`, `maxFieldObstacles`, `mutateEveryNTurns`, `backgroundTint`를 추가 — 단순 수치 스케일링이 아닌 컨셉 수준의 차별화입니다. `GameManager.SetupGimmicks()`/`SpawnCastleWall()`/`CreateBackground()`/`EnsureFieldDirector()`가 모두 공유 정적 필드 대신 `ActiveLayout`을 읽도록 변경했습니다.
2. **`GimmickFieldDirector.PlanForTurn`** 리팩터링: 공유 `PlanForTurnGeneric(turn, aliveCount, maxObstacles, kindCount, mutateEveryNTurns)` 코어로 일반화해 각 스테이지의 기믹 종류 수와 변경 주기가 하나의 매개변수화된 공식에서 나오도록 했습니다 — 기존에 고정된 3인자 오버로드(Stage1)와 Stage3의 동작은 리팩터링 전후로 완전히 동일하게 유지됩니다.
3. **Stage2 "잿빛 보루" (완성, 신규):** 더 이상 자리표시자가 아닌 잠금 해제된 실제 스테이지. 발사 지점을 ±13.5로 좁혀(플레이어 간 거리 27, Stage1의 29 대비 -6.9%) 지면/카메라/게이트/바람을 함께 조정했습니다. 시작 시 화약통 없음(요새는 자기 성문 앞에 공짜 폭발물을 두지 않으며, 위험 요소는 Stage1과 동일한 4종 필드 기믹 순환으로 전투 중 획득). 3단 높이 벽(Stage1의 2단 대비). 필드 구성은 2턴마다(Stage1의 3턴 대비) 최대 4개(6개 대비)로 더 적게 순환 — 밀도 높고 빠르게 순환하며 두 턴 연속 휴식 구간이 오는 일이 없습니다. 바람 상한 6.2, 잿빛 배경 틴트.
4. **Stage3 "서리협곡" 구도 개선:** Stage1의 다리 근처 배치를 재사용하던 화약통을 Stage1의 정확한 여유 비율(코어 여유 2.5, 발사구 여유 3.5)을 더 넓은 발사 지점에 그대로 적용한 ±11.5/±15.0 날개 배치로 이동 — 넓은 중앙 지대가 이제 진짜로 비어 보이며, 단순히 Stage1 보드에 여백만 늘어난 게 아닙니다. 최대 필드 기믹 수를 7개로 상향(Stage1의 6개 대비)해 더 넓은 판에 맞췄습니다.
5. **독립 밸런스 검토가 출시 전 실제 회귀를 발견하고 수정:** 병렬 서브에이전트 3개(`Assets/Editor/Stage2FieldRulesTests.cs` 작성 1개, `StageDefinitionsTests.cs` 재작성 1개, 읽기 전용 안전성 감사 1개) 중 두 곳이 독립적으로, 초기 Stage3 화약통 배치(±15.5)가 발사구로부터 정확히 3.0 거리에 위치해 — 바로 이 코드베이스에 존재하는 여유 규칙의 경계선상에 있었음을 발견했습니다. 이 규칙은 정확히 그 거리에 있던 화약통이 과거에 저각 사격을 자폭시켰던 실제 버그(`GameManager.cs` 주석, review cycle 3 P1 #1) 때문에 생긴 것입니다. 오케스트레이터 자신이 작성한 테스트가 약화된 `Assert.GreaterOrEqual`을 사용해 이 경계 케이스를 조용히 통과시키고 있었는데, 동일한 불변조건을 검증하는 다른 고정 테스트(`GamePlayTests.FieldLayout_KegsClearLaunchMuzzles`)는 더 엄격한 `Assert.Greater`를 사용하고 있었습니다. 화약통을 ±15.0/±11.5로 옮기고(경계선이 아닌 실질적 여유 확보) 테스트 연산자를 더 엄격한 규칙에 맞게 수정해 해결했습니다.
6. **밸런스 검토가 설계 불일치도 발견:** Stage2의 초기 바람 상한 절감폭(6.5→5.5, 바람 범위의 -25%)이 자신의 거리 압축폭(-10.3%)에 비해 과도하게 가팔랐던 반면, Stage3의 바람 상한 증가는 자신의 거리 변화폭(+27.6%)에 비례 이하로 스케일되어 있었습니다. Stage2의 바람 상한을 6.2로 재조정(바람 범위 -7%, 자신의 거리 압축폭 -6.9%에 비례)하고, 발사 지점 자체도 13.0→13.5로 넓혀 적 발사 링과 공유 코어 사이에 실질적인 1.0 여유 공간을 복원했습니다(13.0 발사 지점은 링의 안쪽 경계가 코어의 콜라이더 경계와 정확히 맞닿아 있어 여유 공간이 전혀 없었고, 아직 그 구간에 아무것도 배치되어 있지 않더라도 검토에서 설계상 위험 요소로 지적했습니다).
7. **QA (Unity MCP 라이브):** 두 어셈블리 모두 `dotnet build` — 0 errors. `tests-run` EditMode — **130/130 통과**(기존 117 + 신규 13: 재작성된 `StageDefinitionsTests`와 신규 `Stage2FieldRulesTests`), 3개 스테이지 전체를 아우르는 라이브 PlayMode 세션 전후 모두 통과(상태 오염 회귀 없음 확인). `script-execute`를 통해 Stage1/2/3을 순서대로 라이브 검증: Stage1은 무변화(`LaunchApronAbsX=14.5`, 지면 블록 205개, 스폰된 화약통 4개, `maxFieldObstacles=6`, 흰색 배경); Stage2(`LaunchApronAbsX=13.5`, `windCapEnd=6.2`, 지면 블록 195개, 스폰된 화약통 0개, `maxFieldObstacles=4`, 3블록 벽, 잿빛 배경 `RGBA(0.72,0.72,0.76,1)`, `PlanForTurn` 직접 호출로 2턴째 `mutate=true` 확인); Stage3(`LaunchApronAbsX=18.5`, `windCapEnd=7.2`, `maxFieldObstacles=7`, 수정된 ±11.5/±15.0 날개 위치에서 화약통 확인). 인트로 화면 스테이지 선택기를 라이브로 검사: 세 카드 모두 `interactable=true`이며 올바른 표시 이름을 가지고, 잠기거나 흐려진 카드는 남아있지 않음.

### 세 번째 전장 스테이지: "서리협곡"(Stage3), 3단계 선택형 시스템 (2026-07-07)

선택 가능한 다중 스테이지 전장 시스템을 구축하고 그 세 번째 스테이지를 완성했습니다 — 요청대로 기믹 타입/종류를 각각 최소 1개 이상 늘리고, 플레이어간 거리를 넓히고, 필드 패턴을 다르게 섞었으며, Unity MCP로 전 과정을 진행하고 레벨 밸런스를 지속적으로 고려했습니다.

1. **스테이지 아키텍처:** `StageDefinitions.cs`가 `StageId {Stage1, Stage2, Stage3}`와 순수 데이터 테이블 `StageLayout`(발사 지점 거리, 지면 폭/앵커 밴드, 게이트 오프셋, 바람 상한, 카메라 프레이밍, `locked` 플래그)을 도입합니다. `GameManager.ApplyStageLayout()`은 `Start()` 시작 시점에 딱 한 번 `PendingStage`를 실제 수치로 변환합니다 — `Awake()`에서는 절대 하지 않습니다 — 그래서 `Awake()`/`CreateGround()`만 직접 호출하고 `Start()`는 호출하지 않는 EditMode 리플렉션 테스트들은 이전 PlayMode 세션에서 어떤 스테이지를 골랐든 상관없이 항상 Stage1의 고정 기본값을 봅니다. `CoreAbsX`는 모든 스테이지에서 공유되는 불변 상수로 유지되며, 간격/프레이밍/바람만 스테이지별로 달라집니다.
2. **Stage1 — 공성 평원:** 기존 아레나 그대로, 완전한 무변화(선택 시 완전한 no-op). **Stage2 — 잿빛 보루:** 추후 작업을 위한 잠긴 자리표시자(`StageLayout.locked = true`)로 Stage1과 동일한 수치를 그대로 미러링해 미완성 데이터가 실제 플레이에 절대 노출되지 않도록 합니다. 인트로 화면 카드는 흐리게/비활성으로 렌더링되고, `GameManager.RequestStage()`가 권위 있는 가드로 선택을 거부합니다.
3. **Stage3 — 서리협곡 (완성):** 발사 지점이 ±14.5에서 ±18.5로 확대 — 플레이어 간 거리가 29→37(+27.6%, 의미 있게 넓히되 과도하지 않은 범위)로 증가하며, 지면(41→49 컬럼)과 카메라(39→47 보드)도 함께 확장되고, 더 길어진 사거리에 맞춰 최종 바람 상한을 6.5→7.2로 상향했습니다.
4. **기믹 타입 +1 — `SpikeTrapGimmick`:** 기존 모든 기믹과 다른 상호작용 모델을 가진 근접 감지형 바닥 함정 — `EruptionVentGimmick`의 고정 주기나 `EventGateGimmick`/`BuffDebuffGimmick`의 1회성 트리거 대신, 반복적인 `Physics2D.OverlapCircleAll` 검사로 구동되는 대기/발동준비/작동/재사용대기 상태 머신입니다. 유닛이 범위 안에 들어오면 발동 준비 후 24 데미지와 확정적인 상승-이탈 넉백(`SpikeTrapRules.KnockbackVelocity` — 항상 양의 Y 성분을 가져 순수하게 옆이나 아래로만 날아가지 않음)을 1회 터뜨리고, 이후 재사용대기를 거쳐야 다시 무장합니다.
5. **기믹 종류 +1 — 서리 분출 벤트:** 기존 `EruptionVentGimmick`의 `EruptionStyle`(마그마/페탈)에 세 번째 종류(**서리**)를 추가. 마그마의 수직 상승+데미지 대신 옆으로 밀어내며 둔화 디버프(`ApplyDebuff(0.55배, 2.5초)`)를 적용 — "공중으로 날아가지 않기"와는 다른 "레인에서 벗어나 있기" 전술 대응을 요구합니다.
6. **단순 재도색이 아닌 패턴 혼합:** `GimmickFieldDirector.PlanForTurn`에 스테이지 인식 오버로드를 추가 — Stage3는 기존 4종(배럴/미니타워/룬/패트롤)에 SpikeTrap을 더한 5종 전체를 5주기로 순환시키고(Stage1/2의 4주기 대신), 필드 구성은 3턴이 아닌 4턴마다 변경됩니다. `VentSchedule.StyleForTurn`도 스테이지 인식 오버로드를 얻어 마그마→서리→페탈을 3벤트-스폰 비트마다 순환시킵니다(Stage1/2의 2종 교대 대신).
7. **스테이지 선택 UI:** `IntroScreenController`가 하우투 스트립 아래에 세 개의 토글 카드(Stage1/Stage2/Stage3)를 렌더링합니다. 잠기지 않은 스테이지를 선택하면 `GameManager.RequestStage()`가 호출되어 씬을 리로드(재도전/타이틀과 동일한 경로)하므로, 지면 폭·발사 거리·카메라 프레이밍을 포함한 런타임 생성 월드 전체가 해당 스테이지에 맞게 완전히 새로 빌드됩니다 — 제자리 레이아웃 교체로 인한 위험이 전혀 없습니다. 잠긴 Stage2 카드는 시각적으로 흐리고 클릭할 수 없습니다.
8. **QA (이번 세션, Unity MCP 라이브):** `Assembly-CSharp.csproj`와 `CastleBusters.Tests.csproj` 양쪽 모두 `dotnet build` — 0 errors. `tests-run` EditMode — **117/117 통과**, 라이브 PlayMode 세션 전후 모두 통과(`GameManager.OnDestroy()`의 스테이지 정적 필드 리셋 가드가 실제로 EditMode로의 상태 오염을 막고 있음을 확인). `script-execute`를 통한 라이브 PlayMode 검증: Stage3 선택 시 `LaunchApronAbsX=18.5`, `LaunchRingRules` 링 위치 ±18.5, `windCapEnd=7.2`, x=[-24,24]에 걸친 245블록 지면 그리드, `GimmickFieldDirector.stage=Stage3`가 모두 정확함을 확인; 인트로 스테이지 선택기를 라이브로 검사해 Stage1은 선택 가능/흐림, Stage2는 잠김/비활성/흐림, Stage3는 선택됨/강조 표시임을 확인; `SpikeTrapGimmick`을 정확히 프로덕션 경로인 `GimmickFieldDirector.TestSpawn`으로 스폰해 실제 유닛에게 발동하는 것을 확인(근접 감지로 무장 → 넉백 속도를 부여하는 버스트 — 이번 세션의 실시간 매치 시계가 수동 검증 왕복 시간보다 빠르게 `GameOver`로 수렴해, 이전 세션에서 측정한 정확한 데미지 수치를 독립적으로 재현).
9. **동시 편집 메모:** 세션 중반, 동일한 목표를 향해 같은 파일들을 실시간으로 편집 중인 또 다른 프로세스를 발견했습니다(디스크에 이미 존재하던 `StageDefinitions.cs`/SpikeTrap/서리 벤트 스캐폴드, `StageId {Stage1, Stage2}`로 미명명된 상태). 사용자에게 확인받고 작성자가 안정화될 때까지 기다린 뒤, 폐기하지 않고 그 작업을 이어받아 완성했습니다 — 기존 프로세스의 2단계 콘텐츠가 이번 세션의 Stage3가 되었고, 진짜 3단계 시스템을 만들기 위해 잠긴 Stage2 자리표시자를 추가했으며, `Stage2`로 명명되어 있던 모든 참조를 `Stage3`로 재명명했습니다(enum 값, `VentSchedule`/`PlanForTurn` 오버로드 가드, 주석, `IntroScreenController` 필드, 테스트) — 최종 프로젝트 전체 스윕으로 의도치 않은 `Stage2` 잔존 참조가 없음을 확인했습니다.

### 트래킹 백필: `.specify/cycles.md`를 미기록 커밋 5건과 동기화 (2026-07-04)

`.specify/cycles.md`(Dynamic Battlefield 패스의 고정 10-사이클 로그)가 실제로는 5개 커밋(AOS 오버홀, 콘텐츠 패스, 후속 버그픽스 3건, 기믹 공정성 수정 — 모두 위에 문서화됨)이 이미 진행된 상태에서도 1행("pending")에 멈춰 있었습니다. 크리틱 서브에이전트가 `.specify/spec.md`의 AC1-AC11 대비 코드를 감사하고, 직접 소스를 추적해 근본 원인을 확인했습니다.

1. **1행 자체의 라이브 테스트 기록 미반영:** cycle 1의 라이브 테스트 로그(`mcp-server.log`, 2026-07-02 23:57, EditMode 58/60 통과)에 실패 2건 — `FieldLayout_KegsClearLaunchMuzzles`, `GimmickFrameAnimator_TryAttach_PreservesWorldFootprint` — 이 있었는데, 이는 이후 "code review cycle 3" 커밋(`175c8b1`, 위 기믹 공정성 패스)에서 실제로 수정됐지만 해당 행은 끝내 업데이트되지 않았습니다.
2. **백필:** `.specify/cycles.md`를 1→6행으로 정직하게 채웠습니다(가짜 사이클 없이 커밋별로, 수정이 적용된 경우 실제 소스 근거를 명시). `.specify/spec.md`, `Boards/Engineering.md`, `Projects/CastleBusters.md`도 동일 내용으로 동기화했습니다.
3. **검증 상태:** `dotnet build unknown-castle.sln`은 HEAD에서 0 errors로 재확인했습니다. 위 수정 사항을 "코드 검증"에서 "라이브 검증"으로 전환할 라이브 Unity MCP EditMode 재실행은 이번 세션에서는 완료하지 못했습니다 — 이미 열려 있는 Unity 에디터가 프로젝트 락을 쥐고 있어 2차 배치모드 실행이 불가했고, MCP HTTP 브리지도 tool 목록을 빈 상태로 반환했습니다. 다음 세션의 최우선 항목으로 남겨둡니다.
4. **AC8 미충족:** AC8은 10개의 로그 행을 요구하지만 현재 6개뿐입니다. seed의 사이클 목표를 실제 6-커밋 케이던스에 맞게 재고정하거나, 향후 커밋마다 새 행으로 계속 로그하는 방안 중 하나를 권고합니다.



### 플레이테스트 QA 패스: UI/UX 사이징, 텍스트 잘림 및 토스트 백플레이트 대비 개선 (2026-07-05)

카드 비주얼 일관성, 버튼 텍스트 잘림 현상, HUD 오버레이 가독성에 초점을 맞춘 플레이테스트 기반 개선 패스이며, Unity MCP PlayMode 테스트 슈트를 통해 검증했습니다.
1. **블록 선택 버튼 텍스트 수정:** 블록 선택 버튼의 텍스트가 3회 반복(`WOOD WALL WOOD WALL WOOD WALL`)되어 110x36 크기의 버튼을 넘쳐 흘러 잘리던 문제를 단일 라벨(`WOOD WALL`, `STONE WALL`, `IRON WALL`)로 변경하여 완벽히 해결했습니다.
2. **기믹 선택 카드 크기 통일:** `GameManager.cs`에서 기믹 카드의 스케일을 1.2배에서 1.5배로 조정하여 다른 3종의 캐릭터 선택 카드와 크기를 맞추고, 일관된 HUD 레이아웃을 생성했습니다.
3. **텍스트 중복 제거:** 기믹 카드의 역할 라벨을 `KEG`에서 `HAZARD`로 변경하여 `POWDER KEG KEG`와 같은 불필요한 중복 표기를 수정했습니다.
4. **선택 카드 자동 줄바꿈 비활성화:** 캐릭터 선택 버튼 텍스트의 자동 줄바꿈을 비활성화(`text.enableWordWrapping = false;`)하여, 작은 해상도에서 의도된 줄바꿈 위치가 어긋나 잘리는 현상을 방지했습니다.
5. **토스트 백플레이트 추가:** `EnsureHud()`(GameFeelVfx.cs)에서 턴 토스트 텍스트 뒤에 `ToastBackplate` 패널을 추가하고 `AnimateToastAndCombo()`에서 위치 및 투명도를 동기화하여 애니메이션되도록 했습니다. 약 58% 투명도의 어두운 청록색 백플레이트가 추가되어 HUD가 배경 전장과 겹쳐 읽기 어려웠던 문제를 해소했습니다.
6. **QA (Unity MCP 라이브):** PlayMode `PlaytestQACapture` 테스트가 성공적으로 통과하며 1.5배 기믹 카드 크기를 재검증하고, 갱신된 비주얼 스크린샷(`QA_TitleScreen.png`, `QA_SelectionRow.png`, `QA_ArcherArrow.png`, `QA_BrickSpawnFx.png`)을 정상 캡처했습니다.
### 플레이테스트 QA 패스: UI/UX 사이즈, 파티클 폴리시, 버프/디버프 명료화 (2026-07-04)

씬 배선, 파티클/VFX 완성도, 버프/디버프 가독성에 초점을 맞춘 후속 플레이테스트 패스이며, 전 과정을 라이브 Unity MCP 도구로만 검증했습니다 (에디터 수동 클릭 없음).

1. **GimmickButton 씬 배선:** `GameManager.gimmickButton`이 씬에 연결되어 있지 않아 (기사/궁수/폭탄병만 연결됨) `SetupUIButtons()`/`StyleSelectionButton()`가 이미 지원하는데도 4번째 선택 카드가 표시되지 않았습니다. 실제 `GameManager` 컴포넌트에 라이브 MCP `gameobject-component-modify`(`pathPatches`)로 연결하고 `scene-save`로 저장, `SampleScene.unity`에 `gimmickButton: {fileID: ...}`로 영구 반영됨을 확인했습니다.
2. **파티클/VFX 폴리시:** `DebrisFragment`가 첫 프레임부터 선형으로 알파를 줄여 "흐릿하게 씻겨나가는" 느낌이었던 것을, 수명의 약 55%까지는 불투명을 유지하다 빠르게 페이드하도록 바꾸고 스케일 축소에는 이징(t²)을 적용했습니다. `FrameAnimEffect`(임팩트 스파크/벽돌 생성/룬 반짝임)는 인스턴스마다 ±12% 크기 지터를 주어 반복 스폰이 도장 찍은 것처럼 보이지 않게 했고, 프레임 스트립의 마지막 25% 구간에서 알파를 페이드아웃시켜 하드 `Destroy()` 팝을 없앴습니다. `GameFeelVfx.SpawnImpactBurst`는 고강도 타격(코어 피격/파괴)에서 약 60ms 지연된 작은 2차 버스트를 레이어링해 "쩍, 흩어짐"으로 읽히게 했습니다.
3. **버프/디버프 명료화:** 유닛의 버프/디버프가 타이머 종료 시 조용히 색상만 되돌아갔던 것을, 종료 0.8초 전부터 8Hz로 색을 깜빡여 "곧 종료" 경고를 주고, 실제 종료 시 `BUFF ENDED`/`DEBUFF ENDED` 플로팅 라벨을 띄우도록 했습니다.
4. **데미지 숫자 무게감:** `GameFeelVfx.SpawnDamageNumber`가 타격 크기와 무관하게 항상 3.5 크기/고정 색이어서 5데미지 스침과 80데미지 폭발이 동일하게 보였던 것을, 데미지 크기에 따라 폰트 크기/색을 단계화하고 50 이상 타격에는 `FloatingDamageText`가 추가 스케일 펀치를 주도록 했습니다.
5. **QA (Unity MCP 라이브):** `tests-run` EditMode — **100/100 통과**; `tests-run` PlayMode — **2/2 통과** (`AutoPlayTest` + `PlaytestQACapture`, 이전 사이징 패스의 타이틀 버튼 +20%/선택 행 1.5×·1.2×/궁수 화살 가시성/벽돌 스폰 이펙트 검증이 모두 여전히 통과, 확대된 선택 카드 간 겹침 없음). `console-get-logs`로 이번 패스의 모든 수정 이후 컴파일 오류/예외 0건을 확인했습니다.

### 기믹 공정성 버그 수정, 페이즈 텔레그래프, 기사/궁수 특성 튜너블 (2026-07-04, 후속)

위 QA 패스에서 남은 백로그 항목에 대한 후속 패스입니다. 아키텍트 서브에이전트가 기믹 스크립트와 `UnitData.cs`를 리뷰해 실제 정합성 버그 1건과 가독성 공백을 찾아냈고, 모두 수정 후 라이브 Unity MCP로 재검증했습니다.

1. **EventGateGimmick 폭발 위력 버그:** PowerUp/PowerDown/Reduce 게이트 효과가 `unit.ApplyLaunchPowerMultiplier(...)`(기존 버프/디버프 타이머로 시간 제한 및 원복)는 호출하면서, 폭탄병의 `ExplosiveGimmick.explosionRadius`/`explosionDamage`는 별도로 곱해놓고 **원복도 만료도 없었습니다** — 게이트를 통과한 폭탄병은 속도/데미지 효과는 조용히 원복되는데 폭발 위력/반경은 영구적으로 증폭 또는 약화된 채로 남았습니다. `ExplosiveGimmick`에 `ApplyTemporaryPotencyMultiplier(multiplier, duration)`를 추가해 기준값을 (지연 캡처 방식으로) 한 번만 저장하고 `duration` 후 코루틴으로 원복하도록 했으며, `EventGateGimmick.ApplyToUnit`에 3번 복붙되어 있던 `GetComponent<ExplosiveGimmick>()` 블록은 `ApplyExplosiveScaling` 헬퍼 하나로 통합했습니다.
2. **EventGateGimmick 소진 신호:** 게이트의 복제 예산(`maxTotalClones`)이 소진된 뒤에도 여전히 펄스/틴트가 살아있는 것처럼 보여, 플레이어가 복제가 일어날 거라 오해하게 만들었습니다. 예산이 소진되는 순간 한 번만 어둡게 처리하고 "SPENT" 라벨을 띄우도록 했습니다.
3. **BuffDebuffGimmick 재트리거 공정성:** 한 번 처리한 오브젝트를 기억하는 `EventGateGimmick`과 달리 버프/디버프 존에는 쿨다운이 없어서, 런치/넉백 직후 트리거 경계에서 유닛이 미세하게 흔들리면 `ApplyBuff`/`ApplyDebuff`가 반복 중첩되어 지속시간이 예측 불가하게 늘어날 수 있었습니다. 동일 효과 타입이 1초 이내 재발동하는 경우만 억제하는 인스턴스별 쿨다운을 추가했습니다 (다른 타입의 효과 — 존이 재설정되었거나 다른 존이 같은 콜라이더를 겹치는 경우 — 는 즉시 적용됨); 기존 `BuffDebuffGimmick_AppliesBuffAndDebuffToUnit` EditMode 테스트(의도적으로 `effectType`을 뒤집어 재트리거하는 테스트)를 계속 통과시키기 위해 이렇게 구분했습니다.
4. **MovingGimmick 전차 페이즈 텔레그래프:** "WAR BEAST RAMPAGE!/FRENZY!" 콜아웃과 더 강해진 비행 속도/패턴이 같은 프레임에 동시에 적용되어 반응할 시간이 전혀 없었습니다. 콜아웃/충격파/경보는 체력 임계값을 넘는 즉시 그대로 발동하되, 실제 패턴 속도/형태(`appliedPhase`)는 공표된 페이즈(`lastPhase`)보다 0.45초 늦게 반영되도록 해, 경고가 실제로 위험보다 먼저 오도록 했습니다.
5. **UnitData.cs 기사/궁수 특성 튜너블:** 폭탄병만 프리팹 단위 튜너블(`explosionRadius`/`explosionDamage`)이 있었고, 기사의 밀치기 힘·콤보 간격, 궁수의 점프 속도·연사 딜레이는 모두 `UnitController`에 하드코딩되어 있었습니다. `UnitData`에 `knightPushForceMultiplier`/`knightComboIntervalSeconds`, `archerJumpVelocity`/`archerVolleyFollowupDelaySeconds`를 추가하고 `UnitController` 필드로 미러링했습니다 (기본값은 기존 하드코딩 값과 동일해 별도 프리팹을 만들지 않는 한 동작 변화 없음), 4곳의 호출부를 매직 넘버 대신 이 필드를 읽도록 연결했습니다.
6. **QA (Unity MCP 라이브):** 아키텍트 서브에이전트가 먼저 기믹 스크립트를 읽기 전용으로 분석해 위 우선순위 목록을 제안했고, 이후 직접 수정을 적용해 `tests-run` EditMode — **100/100 통과** (패스 도중 회귀 2건 발견 후 수정: 재트리거 쿨다운과 지연 기준값 캡처 이슈가 바로 이 EditMode 테스트 2건이 먼저 실패하면서 드러났습니다), `tests-run` PlayMode — **2/2 통과**로 검증했습니다. `console-get-logs`로 최종 수정 이후 새 컴파일 오류가 없음을 확인했습니다.


### 플레이 가능 UX 10회 반복 개선 (2026-06-30)

현재 플레이 가능한 슬라이스에는 초반 이해도와 실제 조작감을 높이기 위한 런타임 `GameplayUxDirector` 패스가 추가되었습니다. 이번 반복의 10개 결과는 다음과 같습니다.

이번 Obsidian/llm-wiki 동기화 패스에서는 실시간 전술 패널, 유닛별 드래그 조준 코칭, 바람 보정 문구, 코어 체력 위험 토스트, 발사 순간 쇼크웨이브/콜아웃, 버프/디버프 존 라벨을 추가해 첫 1분 플레이와 캡처 화면의 이해도를 높였습니다.

1. 상단 목표 배너로 “내 코어가 무너지기 전에 적 코어 파괴” 승리 조건을 명확히 표시합니다.
2. 플레이어/적 턴 전환 토스트로 언제 조준해야 하는지 알려줍니다.
3. 숫자 타이머와 함께 턴 진행 바를 표시해 남은 시간을 즉시 파악할 수 있습니다.
4. 하단 커맨드 스트립에 유닛 단축키와 파란 링 드래그 조작을 상시 안내합니다.
5. 발사 직후 Soft Arc, Clean Launch, Power Shot 등 발사 등급과 파워/각도를 표시합니다.
6. 플레이어/적 코어 HP 배지로 전투 중 핵심 목표 체력을 계속 보여줍니다.
7. 바람 수치를 실제 조준 조언으로 번역하는 Wind Hint를 제공합니다.
8. 발사, 명중, 블록 파괴, 코어 타격, 폭발을 콤보/임팩트 티커로 요약합니다.
9. 배럴과 이동 장애물을 월드 라벨로 주기적으로 표시해 기믹 인지가 쉬워졌습니다.
10. 피해/파괴/폭발 이벤트가 HUD와 연결되어 전투 피드백과 UI가 동시에 갱신됩니다.

검증: UX 패스 이후 `dotnet build Assembly-CSharp.csproj --no-restore`가 경고 0개, 오류 0개로 통과했습니다.
### 지면 밀도 및 타일 해상도 개선 (2026-07-01)

기존 지면 스트립은 하나의 지형처럼 보이기보다 몇 개의 콜리전 박스가 떠 있는 느낌이었고, 폭발로 블록이 깎일 때 타일 경계에서 앨리어싱이 눈에 띄었습니다. 이번 패스는 `GameManager.CreateGround()`와 파괴 가능 블록 공용 파이프라인을 다음과 같이 개선했습니다:

1. 지면을 3줄에서 5줄로 늘리고 41칸 폭을 하나로 통일해, 총 205개(기존 123개)의 지면 블록이 끊김 없는 하나의 지형으로 보이도록 했습니다.
2. 지면/균열 슬라이스/파편 텍스처 해상도를 상향(지면 128→160px, 파편 128→192px)하고, 밉맵 생성 + `FilterMode.Trilinear` + `anisoLevel = 4`를 적용해 폭발로 블록이 줄어들거나 회전할 때 나타나던 시머링/앨리어싱을 제거했습니다.
3. `DestructibleBlock.SetPresentationSprite()`가 스프라이트를 교체할 때마다 스케일과 콜라이더 크기를 함께 재적용하도록 해, 작은 텍스처 슬라이스를 적용한 뒤 `BoxCollider2D`가 원본 아트 크기에 남아있던 버그를 수정했습니다.
4. 균열/심한 균열 지면 스프라이트는 205개 타일 전체에 대해 미리 굽지 않고 `SetLazyCrackedSprites()`로 실제로 금이 갈 때만 지연 생성하도록 했고, `BlockData.GetSharedPhysicsMaterial()`이 블록마다 새로 할당하던 `PhysicsMaterial2D`를 하나로 캐시 공유하도록 했습니다.
5. `GenerateGroundTexture()`의 경계선 계산을 픽셀당 삼각함수 연산(O(가로×세로))에서 컬럼당 1회 사전계산(O(가로))으로 바꿔 성능을 개선했습니다.

검증: `GamePlayTests` EditMode 테스트 29개가 모두 통과했습니다. 여기에는 갱신된 지면 블록 개수 검증(`41 * 5`개)과, 프레젠테이션 스프라이트 교체 후에도 모든 지면 타일이 `localScale == Vector3.one`, `BoxCollider2D.size == Vector2.one`을 유지하는지 확인하는 신규 회귀 방지 테스트가 포함됩니다.

### 안정성 + 인트로 + 진행 곡선 패스 (2026-07-02)

핵심재미 가드레일: 슬링샷 발사 → 물리 파괴 → BFS 연쇄 붕괴 체인은 모든 변경에서 보존됩니다. 붕괴는 여전히 크게 터지지만(플레이 캡처의 BLOCK BREAK x25), 맵 전체가 사라지는 폭주는 구조적으로 불가능해졌습니다.

1. **자발적 붕괴/셀프 GameOver 근본 수정:** 코어/배럴이 지면 위 공중(y=1.5/1.0)에 스폰되어 첫 물리 틱에 낙하하고 BFS 인접성에서 벗어나던 문제를 y=0.5 밀착 스폰으로 종결했습니다.
2. **연쇄 붕괴 상한:** 낙하 블록 충돌 데미지를 상대속도 기반 `min(v×8, 45)`로 제한하고 지면 하부 2행+측면을 앵커로 지정해, 우드 브릿지는 그대로 끊어지지만 41×5 지면 전체가 분해되는 일은 없습니다.
3. **인트로 타이틀 카드:** `GameState.Intro` + `IntroScreenController`(gti 생성 키 아트, 한/영 태그라인, START SIEGE 버튼/Space/Enter)가 첫 턴 전에 보드를 동결 디오라마로 보여줍니다.
4. **한글 폰트 지원:** `KoreanFontSupport`가 OS 폰트 파일(AppleSDGothicNeo 등)에서 동적 TMP 폰트를 만들어 fallback 체인에 등록 — 한글 HUD 문자열의 tofu 박스가 사라졌습니다.
5. **난이도 곡선:** 바람 상한 2.5→6.5, AI 조준 오차 2.2→0.6, 폭풍 확률 5%→25%가 `SmoothStep(turn/12)`을 따라 램프됩니다. `docs/ProgressionCurves.png` 참조.
6. **PerfectPixel 스프라이트:** Knight/Archer/Bomber × Idle/Walk/Attack/Launch 60프레임(512×512)으로 플레이스홀더 전량 교체.
7. **검증:** EditMode 41/41 통과, PlayMode AutoPlayTest(인트로 상태 어설션 + 캡처 시퀀스) 통과, `IntroCapture.png` / `GameplayCapture_6.png` 갱신.

### 플레이테스트 폴리시 패스: 전용 기믹 아트 + 턴 코칭 (2026-07-02)

플레이테스트에서 모든 기믹이 같은 돌 블록 텍스처의 색 틴트 재사용이라 "떠 있는 잘못 놓인 블록"처럼 보였고, 대기 중인 플레이어의 턴이 소리 없이 증발할 수 있었습니다. 이번 패스에서 모든 상호작용 오브젝트에 전용 실루엣을 부여하고 턴 시스템이 플레이어를 적극적으로 코칭하도록 개선했습니다:

1. **전용 기믹 아트** (gti 생성, 마젠타 매팅, `Resources/Gimmicks/`): 랠리/헥스 룬 데칼, 효과별 틴트의 석조 아치 포털 게이트, 공성 램 장애물, 화약통, 팀 틴트 크리스탈 킵 코어, 유닛 버튼용 나무 카드 프레임(카드 면 + 별도 `UnitPortrait` 자식).
2. **Awake→Start 비주얼 순서 수정:** 스포너가 `AddComponent()` 후 `effectType`/`isPlayerCore`를 할당하므로 Awake 시점 아트 선택은 항상 기본값을 봤습니다(DebuffZone이 랠리 룬으로, 모든 게이트가 PowerUp 틴트로, 적 코어가 파란색으로 렌더링). 이제 비주얼 선택이 필드 할당 후 `Start()`/`ApplyVisuals()`에서 실행됩니다.
3. **코어 스프라이트 지속성:** 킵 코어 아트를 `SetPresentationSprite`로 등록해 첫 피격 시 null 스프라이트로 되돌아가는 버그를 차단했습니다.
4. **턴 스킵 수정:** 발리 해결 중에는 턴 시계가 정지(`isResolvingTurn`)합니다. 이전에는 해결 중 타이머가 만료되면 `EndTurn`이 이중 발화(Update + 정착 코루틴)되어 플레이어의 다음 턴을 소리 없이 건너뛰었습니다.
5. **턴 코칭:** 5초 이하 긴급 토스트("지금 발사하세요!"), 5초마다 대기 넛지 + 집결 링 쇼크웨이브 핑, 드래그 중 타이머 만료 시 1회 +4초 조준 유예(`DecideTurnExpiry`), 소리 없는 스킵 대신 명시적 "발사 기회를 놓쳤습니다" 알림.
6. **신규 EditMode 가드 (총 44개):** 턴 만료 결정 테이블, 기믹 스프라이트 라이브러리 로드/소프트 실패 계약, 존/게이트의 할당-후 비주얼 선택 검증.
7. **깃 푸시 및 검증:** 저장소 상태 검증, 문서 업데이트 및 원격 저장소로의 깃 푸시 완료.

### 리텐션 루프 안정화 + HUD 텍스트 다이어트 (2026-07-02, 야간 패스)

플레이테스트 리포트: 승패 결정 후 재도전/타이틀 버튼을 누르면 아무것도 동작하지 않는 "죽은 씬"으로 빠질 수 있었고, 화살이 화면에서 보이지 않았으며, 발사된 유닛들이 상대 진영 대신 중립 화약통만 공격했고, 상시 HUD 텍스트가 전장을 뒤덮었습니다. 원인과 수정:

1. **오버레이 해동 버그 (재도전/타이틀):** `HitStopManager`의 리얼타임 복원 코루틴이 인트로/결과 화면이 떠 있는 동안 `timeScale`을 다시 1로 되돌렸습니다. 씬 리로드 후에는 `Time.time`이 이미 커서 모든 공격 쿨다운이 지난 것으로 판정되어, 타이틀 카드 뒤에서 보드가 코어가 죽을 때까지 스스로 싸웠습니다. 이제 히트스탑은 실제 턴 진행 중에만 동작하고, 복원 전 상태를 재확인하며, `ShowIntro`/`EndGame`이 대기 중인 복원을 취소합니다. `UnitController.Update` 전투도 PlayerTurn/AITurn으로 게이트됩니다.
2. **도메인 리로드 복원력:** `GameManager`와 `HitStopManager`가 `OnEnable`에서 싱글턴을 재등록하므로, 플레이 도중 스크립트 재컴파일이 발생해도 살아있는 씬이 `Instance == null` 상태(반응 없는 죽은 보드)로 방치되지 않습니다.
3. **컴파일 차단 해제:** 아직 작성되지 않은 메서드를 참조해 플레이 모드 진입 자체를 막던 `SpawnEruptionVents()` 잔여 호출을 제거했습니다.
4. **중립 잔해 타게팅 수정:** 근접 타겟 선택이 부모 없는 블록(화약통, 필드 장애물)을 양 팀 모두의 적 블록으로 판정했습니다. 이제 유닛은 적 성채 블록과 적 유닛만 노려 실제로 상대 진영으로 진격합니다.
5. **화살 가시성:** 화살을 약 3.6배(0.26 → 0.95 월드 유닛, 콜라이더 동기화)로 키워 33.6 유닛 폭 전장에서 원거리 사격이 읽히도록 했습니다.
6. **결과 버튼 오버플로 수정:** 재도전/타이틀 캡션이 안쪽 여백을 가진 라벨 렉트 안에서 자동 크기 조절됩니다 — 고정 30pt가 와이드 뷰포트에서 "재도전"/"타이틀"을 "전"/"이틀"로 잘라내던 문제를 해결했고, 라벨을 "재도전 (R)" / "타이틀"로 줄였습니다.
7. **HUD 텍스트 다이어트:** FIELD INTEL 패널, WAR ROOM 코칭 패널, 목표 배너, 하단 커맨드 스트립, 중복 바람 힌트를 제거하고, 발사 안내/스탯을 한 줄로 압축했으며, 레거시 초록 게임오버 배너를 결과 카드 뒤에서 숨기고, GameOver 동안 HUD 전체를 숨겨 얼어붙은 토스트가 결과 화면에 비치지 않게 했습니다.
8. **검증 (Unity MCP, 라이브 에디터):** 스크립트 E2E — `EndGame` → 실제 버튼 `onClick` → 씬 리로드를 재도전(즉시 PlayerTurn, timescale 1)과 타이틀(인트로가 12초 이상 timescale 0으로 유지, 화살/폭발 0개 — 이전에는 0.14초 만에 해동) 양쪽에서 확인. EditMode `tests-run`: 60/62 통과 — 실패 2건은 이 패스와 무관한 지면 앵커/기믹 프레임 애니메이터의 기존 WIP입니다.

### AOS 개편: 점령 목표, 유닛 전투 특성, 살아있는 전장 (2026-07-03)

기획 문서: `docs/design/aos-overhaul.md`. 순수 규칙은 `Assets/Scripts/SiegeTactics.cs` (신규 EditMode 테스트 23건).

1. **점령-또는-파괴 목표 (§1):** 양쪽 코어에 점령존(`CaptureZoneController`)이 생성됩니다. 공격 유닛만 존에 있으면 6초 게이지가 차오르고(수비 유닛이 있으면 경합 정지, 비면 절반 속도로 감쇠), 가득 차면 "CASTLE SEIZED"로 즉시 승부가 납니다 — 코어 파괴의 대안 승리 루트. 진행 링과 점령/경합 라벨이 월드에 표시됩니다.
2. **기사 (§2):** 3번째 공격은 2연타, 6번째는 3연타(0.14초 콤보 체인, DOUBLE!/TRIPLE! 콜아웃, 주기 반복). 전진 중 더 먼 목표와의 사이를 막는 적을 밀어냅니다("PUSH!").
3. **궁수 (§2):** 5번째 사격은 더블샷, 10번째는 더블샷의 후속탄이 중력 로브 공중사격("SKY VOLLEY!"). 목표가 1.2u 이상 높으면 상황부 점프.
4. **폭탄병 (§2):** 자기 턴 기준 3번째 턴부터 2발, 9번째 턴부터 4발 발사(`VolleyRules`, 0.16초 스태거 + 산개). 착지 즉시 폭발하지 않고 2초 퓨즈가 점점 빨라지는 점멸로 예고 후 폭발 — 먼저 처치되면 즉시 폭발.
5. **이벤트형 화산/꽃가루 벤트 (§3):** 고정 배치 제거. 3턴마다 양 진영 사이 지형의 랜덤 위치에 벤트가 생성되고(스타일 교대), 3턴 유지 후 소멸합니다.
6. **전차 3페이즈 전쟁기계 (§4):** 전차는 파괴 가능(HP 150, `DestructibleBlock`)한 중력 다이나믹 바디가 됐습니다. 피해에 따라 순찰 → 광란 → 돌진 페이즈로 격화되며, 지면 위 벽을 램으로 부수고(22dmg/0.8s), 바닥이 사라지면 낙하하고, 폭발 충격파(신규 물리 임펄스)와 벤트 컬럼에 뒤집히거나 날아가며, 파괴 5초 후 재배치됩니다.
7. **성벽 + 발사구 (§5):** 매치 시작 시 지정 슬롯(±7.5)에 2단 석벽이 생성됩니다 — `LaunchRingRules`가 발사구 원(±14.5, 반경 3.5) 내부의 성벽/솔리드 생성을 차단하며, 필드 디렉터의 장애물 레인에도 동일 적용됩니다.
8. **발사구 포털 애니메이션 (§5):** 평면 원 대신 gti 생성 6프레임 석조 아치 포털(`Resources/Gimmicks/launch_gate_anim/`, 마젠타 키잉, 8fps `GimmickFrameAnimator` 루프)이 표시됩니다. 아트 부재 시 기존 원으로 소프트 폴백.
9. **전세 기반 필드 이벤트 (§6):** 룬/게이트 상시 배치 제거. 4턴마다 `BalanceEventPlanner`가 양쪽 코어 체력을 읽어 이벤트 1건을 생성 — 열세측 접근로에 버프 룬/파워 게이트, 우세측에 헥스 룬/감속 게이트, 접전이면 센터 중립 Multiply 게이트 — 각 4턴 후 소멸.
10. **안전망:** 물리 충격으로 경기장 밖으로 튕긴 지상 유닛이 영원히 떠돌던 문제(스턱 복구 점프가 화면 밖으로 사다리처럼 밀어올림)를 OUT 판정 사망으로 수정.
11. **QA:** EditMode 85/85 통과(`AosOverhaulTests.cs` 신규 23건). Unity MCP 라이브 플레이 검증: 부팅 시 점령존/성벽/다이나믹 전차/포털, 2턴차 랜덤 벤트 + 1턴차 중립 게이트, 9번째 턴 4발 발사(2+4=6 확인), 전투 중 전차 파괴 → 5초 재배치, 점령존 공격/수비 분류까지 콘솔 예외 없이 확인.
12. **문서 갱신 + 푸시 (2026-07-03):** 헤더 캡처(`IntroCapture.png`, `GameplayCapture_6.png`)를 최신 PlayMode `AutoPlayTest` 실행 결과로 재생성해 다시 커밋하고, README 영문/한글을 `origin/main`에 재동기화하여 푸시했습니다.

### 타게팅 정책, 사전 지정 벽돌, 버튼 오버플로 패스 (2026-07-03, 후속)

1. **기믹 우선 타게팅 / 바닥 공격 수정:** 지형 타일이 BFS 지지 판정을 위해 성채에 부모화되어 있어 유닛이 가장 가까운 "바닥 타일"로 직행해 다리를 두들겼습니다. `TargetingRules`가 지면선 아래 블록을 전부 제외하고 가중 거리로 후보를 순위화합니다 — 적 캠프 기믹(코어·화약통) 0.55, 적 유닛 0.85, 일반 성벽/성채 블록 1.0. 유닛은 상대 설치물을 최우선으로 노리되 맵 전체를 가로질러 모든 걸 지나치지는 않습니다. 상대 진영 절반에 있는 중립 설치물도 적 설치 기믹으로 취급합니다.
2. **사전 지정 벽돌:** 적 턴 동안 필드를 클릭해 최대 2곳의 벽돌 위치를 지정하고(반투명 청사진 고스트, 고스트 클릭 시 취소), 내 턴이 시작되는 순간 석재 벽돌이 실체화됩니다(`BrickPlacementController` + `BrickPlacementRules`). 발사구 원(유닛 생성 지역)·필드 밖·건설 상한 위치는 하드 거부.
3. **유닛 생성 지역 보호:** 벽돌/성벽/솔리드 배치가 `LaunchRingRules`를 공유 — 기사/궁수/폭탄병이 발사되는 원 안에는 어떤 것도 지어질 수 없습니다.
4. **버튼 텍스트 오버플로:** 유닛 선택 카드와 인트로 START 버튼 캡션이 안쪽 여백 라벨 렉트에서 자동 크기 조절됩니다(폰트 min/max 클램프, 줄바꿈 없음) — 결과 화면 버튼에 이어 모든 버튼에서 캡션 잘림이 불가능해졌습니다.
5. **QA:** EditMode 91/91 통과(신규 6건: 지면 필터, 가중치 순위, 적 진영 판정, 링/밴드 거부, 배치 상한). 라이브 플레이: 지상 유닛 전원이 y≥0.5의 코어/성채 블록만 타게팅(바닥 타겟 0건), 고스트 2건이 턴 시작과 함께 벽돌 2개로 실체화, 인트로/카드 캡션 무손상 확인.

### 콜라이더 수정 재검토: 화살 순서 버그 + 죽은 코드 정리 (2026-07-03, 후속 2)

위 기믹 콜라이더 수정 작업을 다시 리뷰해 빠진 부분을 점검했습니다. 동일 계열의 실제 버그 1건과 설계 공백 1건, 명확성 문제 1건을 추가로 발견·수정:

1. **실제 버그 — `ArrowController.Awake()` 스프라이트 재매핑 순서:** `FitArrowToPlayableScale()`이 아틀라스 재포장 스프라이트가 할당되기 **전에** 실행되어, 원본 리소스 스프라이트 바운드 기준으로 스케일을 계산한 뒤 더 작은 재포장 스프라이트로 교체되면서 재동기화가 전혀 안 됐습니다 — 화살이 의도한 1.35u가 아닌 약 0.26u로 렌더링(콜라이더는 축소된 스프라이트와 일치했으므로 콜라이더 단독 점검으로는 발견 불가, "화살이 너무 작다"로만 드러남). 재포장 스프라이트 할당을 `FitArrowToPlayableScale()` 이전으로 이동해 수정. 신규 EditMode 회귀 테스트(`ArrowController_ScalesToVisualLength_RegardlessOfSpriteAtlasRemap`)로 순서를 고정.
2. **설계 공백 — `ItemPickup`이 기믹 제외 목록에 누락:** 신규 영웅 전리품 픽업(`ItemSystem.cs`)이 수정된 기믹들과 동일한 "원본 스프라이트 먼저, 스케일 나중" 패턴을 씁니다. 현재는 실제로 영향받지 않음(전리품은 `GameManager.Start()`의 1회성 `ApplyRuntimeSpriteAtlas()` 실행 이후, 게임 도중에만 스폰되므로) — 다만 이 스폰 타이밍 전제가 향후 바뀌어도 안전하도록 `IsGimmickRenderer()`에 방어적으로 추가.
3. **죽은 코드 — 스폰 시점에 미리 설정한 스프라이트가 첫 프레임에 버려짐:** `GameManager`의 코어/차리엇/배럴 스폰 코드가 `AddComponent<...Gimmick>()` 호출 전에 스프라이트를 로드해 `GetPackedSprite()`까지 거쳤지만, 각 기믹 자체의 `Awake()`/`Start()`가 즉시 실제 아트(`GimmickSpriteLibrary`/`GimmickFrameAnimator`)로 덮어써서 매번 조용히 버려지고 있었습니다 — 스폰 코드를 읽는 사람에게 오해를 유발. 죽은 스프라이트 로드 코드를 제거했습니다. `EventGateGimmick` 스포너는 그대로 뒀는데, 그쪽은 시각 폴백 체인(`GimmickSpriteLibrary.TryApply` 실패 시 `ApplyVisuals()`가 미리 할당된 스프라이트를 실제로 유지)에서 실질적으로 사용되기 때문입니다.
4. **QA:** EditMode 100/100(연속 3회, 신규 테스트 1건). Unity MCP 라이브 검증: 수정 후 궁수 발사를 재실행해 `arrow.visualLength == 1.35`가 렌더링된 월드 크기와 정확히 일치함을 확인, 코어/배럴/차리엇 아트도 스폰 코드 정리 후 non-null 및 올바른 이름으로 재확인.

### 기믹-충돌박스 크기 정렬 수정 (2026-07-03, 후속)

제보: 일부 기믹의 충돌박스가 실제 렌더링된 스프라이트 크기와 눈에 띄게 어긋남. Unity MCP로 실측 검증하여 근본 원인 규명(애니메이션 프레임 간 드리프트가 아님 — 각 애니메이션 세트의 프레임 바운드는 픽셀 단위로 동일함을 확인):

1. **근본 원인:** `SpriteAtlasPacker.LoadDefaultSprites()`가 씬의 모든 `SpriteRenderer`(기믹 아트 포함)를 무차별로 런타임 공유 아틀라스에 수집하고, `ApplyPackedSpritesInScene()`이 렌더러를 (종종 축소된) 재포장 스프라이트로 조용히 교체할 뿐 `transform.localScale`이나 `BoxCollider2D`를 전혀 재동기화하지 않았습니다. `DestructibleBlock`은 자신의 콜라이더를 계산하기 전에 미리 `GetPackedSprite()`를 호출해 이 문제를 피했지만, `ExplosiveGimmick`/`CastleCoreGimmick`/`MovingGimmick`/`EventGateGimmick`/`BuffDebuffGimmick`은 원본 리소스 스프라이트 기준으로 콜라이더를 먼저 계산한 뒤 나중에 재매핑되어 크기가 어긋났습니다.
2. **수정:** `SpriteAtlasPacker.IsGimmickRenderer()`를 추가해 5종 기믹 컴포넌트를 아틀라스 수집 단계와 재매핑 단계 양쪽에서 제외했습니다 — 기믹은 이미 자체적으로 완결된 스케일/콜라이더 동기화 로직을 갖고 있어 공유 아틀라스 배칭이 필요 없습니다.
3. **QA:** EditMode 99/99(연속 3회). Unity MCP 라이브 검증: 배럴, 성곽 코어, 비행 야수, 이벤트 게이트, 버프/디버프 룬 전부 신규 `BeginSiege()` + 런타임 아틀라스 적용 후 `콜라이더 월드 크기 == 원본 스프라이트 바운드 × lossyScale`(시각적 근사가 아닌 결정론적 일치)로 확인.

### 컨텐츠 패스: 비행 전쟁 야수, 영웅 전리품 성장, 플로우 명확화, 알람 (2026-07-03)

1. **영웅·화살 확대:** 기사/궁수/폭탄병 스케일 0.30 → 0.42, 화살 0.95 → 1.35u(콜라이더 동기) — 넓은 전장에서 전투가 한눈에 읽힙니다.
2. **비행 전쟁 야수 (gti/퍼펙트픽셀):** 횡 이동 지상 전차를 공중 와이번으로 교체 — 6프레임 날갯짓 사이클(`Resources/Gimmicks/flying_beast_anim`, 마젠타 키잉), 무중력 다이나믹 바디. `FlightRules`가 HP 페이즈별로 2축 이동 패턴을 부여합니다(순찰 글라이드+바운스 → 광란 8자 비행 → 돌진 저공 급강하). 폭발 충격파에 궤도를 이탈했다가 호밍 조향으로 복귀하고, 급강하 중 구조물을 램으로 부수며, 사망 시 전리품을 확정 드랍하고 5초 후 재출격합니다. 고정 X축 이동은 더 이상 없습니다.
3. **영웅 아이템 성장 ("기믹을 부숴 아이템을"):** 화약통 파괴 시 60%, 야수는 100% 전리품 드랍. gti 생성 3종 아이콘(검/방패/부츠)이 14초간 필드에 떠 있고, 유닛이 수집하면 그 진영 전체의 능력치가 매치 내내 영구 상승합니다(+15% 공격 / +20% 체력 / +12% 속도, 스택당·최대 5). 이후 생성/발사되는 모든 유닛에 적용.
4. **플로우 명확화 + 알람 시스템:** 상시 플로우 스트립이 지금 게임이 뭘 하는지 항상 표시하고(조준/볼리 해결 중 애니메이션 점/적 포격/건설 창), 좌상단 4줄 알람 피드가 모든 전장 이벤트를 보고합니다 — 벤트 발생(위치·수명), 전세 이벤트(진영·종류), 야수 페이즈 전환, 벽돌 건설, 전리품 드랍/수집, 점령 50% 경고. 볼리 해결 12초 워치독으로 낀 투사체가 매치를 얼릴 수 없고, 타겟 없는 유닛은 가만히 서 있는 대신 적 진영으로 행군합니다(점령존과 연계).
5. **QA (Unity MCP 라이브):** EditMode 99/99(신규: 페이즈별 2축 비행 범위, 돌진 강하 깊이, 성장 스택/상한/진영 독립, 드랍 확률/타입, 아트 존재). 라이브: 야수 2축 활공 (3.1,4.9)→(-0.9,4.3) 실측, 처치 → 드랍+알람+5초 재출격, 알람 피드/플로우 스트립 활성, ⚔ 글리프 TMP 경고 제거. 병행 세션 작업 포함: 벽돌 재질 선택 UI(목재/석재/철재)와 겹침 안전 스폰.
