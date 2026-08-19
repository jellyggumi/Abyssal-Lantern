# 아트 적용 기록 - 무엇이 들어갔고 무엇이 거부됐는가

- run-id: 20260809-castle-war-stage1 (cycle 3)
- date: 2026-08-19
- owner: game-qa 레인
- 발단: 사용자 지시 - *"현재 발주서 기반으로 이미지 만들었는데, 적용시켜줘"*
- 대상: `_workspace/current/design/concept/art-production-order/` 36장
- 근거: `qa/evidence/art-apply/`, `Assets/Tests/EditMode/GroundTileArtTests.cs`

---

## 0. 결론

**24장 적용, 12장 거부.** 거부는 취향이 아니라 실측입니다 - 거부한 12장은
아군 타일과 **구분되지 않습니다**.

그리고 적용 과정에서 **기존 결함 하나**를 찾았습니다: 지면 아틀라스가 6일 전부터
계산되자마자 버려지고 있었습니다. 제 아트가 아니라 그 결함이 "지면이 안 바뀐다"의
원인이었습니다.

| 항목 | 수 | 판정 |
|---|---:|---|
| 적용 | 24 | 임포터 24/24 `textureType 8`, guid 충돌 0 |
| 거부 (D-2 적 진영) | 12 | 아군과 패턴차 0.001~0.042, 실루엣차 0.0000~0.0027 |
| EditMode | 564/565 | 실패 0, Skip 1(정당) |
| 신규 테스트 | 6 | `GroundTileArtTests` |

---

## 1. D-2 거부: 측정값

발주서 D-2는 *"양 진영 실루엣이 같아 헷갈린다"* 를 근거로 적 성 타일 12장을
요구했습니다. 납품된 12장을 아군 `CastleSkin` 12장과 대조했습니다.

**실루엣** (알파 16×16 격자, 평균 절대차):

| 역할 | s0 | s1 | s2 |
|---|---:|---:|---:|
| base | 0.0000 | 0.0000 | 0.0000 |
| crown | 0.0027 | 0.0021 | 0.0019 |
| edge | 0.0000 | 0.0000 | 0.0000 |
| face | 0.0000 | 0.0000 | 0.0000 |

**패턴** (명도 16×16 격자): 0.001~0.042. **명도차**: ±0.02 이내.

구분 판정선은 0.08입니다. **12장 중 0장이 넘습니다.**

### 왜 실패했는가 - 제 명세가 구조를 무시했습니다

이 타일들은 **블록에 입혀지는 텍스처**입니다. 형태는 블록 격자가 정하고
스프라이트가 정하지 않습니다. *"첨탑을 뾰족하게"* 는 512×512 풀블리드 타일로
표현할 수 없습니다 - 알파가 전면 불투명이므로 실루엣이라는 개념 자체가 없습니다.

게다가 이 타일들은 **무채색 틴트 소스**입니다. `blockColor`가 칠하므로 두 진영이
같은 타일을 받고 색만 달라집니다. 12장을 넣어도 화면은 바뀌지 않습니다.

**진영 구분을 하려면** 블록 격자(성 형상) 또는 `blockColor`를 바꿔야 합니다.
타일 아트로는 도달할 수 없습니다. **D-2는 발주 자체가 잘못됐습니다.**

12장은 `design/concept/art-production-order/` 에 남깁니다 - 지우면 다음 세션이
같은 발주를 반복합니다.

---

## 2. 발견한 기존 결함: 지면 아틀라스가 버려지고 있었다

적용 후 캡처를 비교했더니 지면이 **소수점 3자리까지 동일**했습니다.

원인을 네 단계로 좁혔습니다:

1. **로드 실패를 의심했고 틀렸습니다** - `EveryRequiredGroundTileLoadsAsASprite`
   통과. 7장 전부 로드됩니다.
2. **아틀라스 미생성을 의심했고 틀렸습니다** -
   `TheAtlasBuilderReturnsArtRatherThanFallingBackSilently` 통과. 반사로 호출해
   비-null이고 행 내 분산 0.0004 초과(절차적 띠는 평탄).
3. **행 순서를 의심했고 틀렸습니다** -
   `TheAtlasRowsRunGrassDownToStoneTopToBottom` 통과. 위에서 아래로
   풀 → 경계 → 흙 → 돌 → 돌, 각 행이 해당 타일과 RGB 거리 0.12 이내.
4. **진짜 원인**: `CreateGround:1618-1619`가 모든 지면 블록을 성의 자식으로
   만들고, `:1623-1624`의 `RefreshBlockList()`가 `CastleFacadeDirector.ApplySkins`를
   부릅니다. `ApplySkins`는 `CastleCoreGimmick`만 건너뛰므로 **205개 지면 타일
   전부를 성벽 석재로 다시 스킨**합니다.

즉 아틀라스 생성 · 205회 슬라이스 · 타일별 균열 지연 베이크가 **할당된 다음
줄에서 폐기**되고 있었습니다.

**아무도 신고하지 않은 이유**: 폐기 결과도 정상적으로 보이는 판이고,
`ApplySkins`의 주석은 아직 *"no-op until the generated CastleSkin tiles exist"*
라고 적혀 있습니다 - 작성 시점엔 사실이었습니다. `CastleSkin` 아트가 생긴
순간부터 무음 회귀가 됐습니다.

### 부수 결과: 성 자신의 역할 배정도 틀려 있었다

`ApplySkins`는 역할(Edge/Face/Crown)을 **위치**로 배정합니다 - Edge는
"가장 왼쪽/오른쪽 열". 지면 41열이 바운딩 박스에 들어가 있었으므로 **성벽 블록의
역할이 성 너비의 4배 기준으로 계산**됐고, 모서리 석재가 성벽이 아니라 지면에
찍혔습니다.

### 고친 방법

`DestructibleBlock.IsTerrainTile` (프로퍼티, 직렬화 안 함 - 지면은 코드로만
생성되므로 Inspector에 노출하면 씬의 성벽 블록이 지형을 자칭할 수 있습니다).
`ApplySkins`가 스킨 루프와 **바운딩 박스 계산 양쪽**에서 제외합니다.

**측정**: 지면 띠 픽셀 변화 **0.64% -> 36.9%** (8단계 이상 차이).
전체 프레임 12.3%이므로 변화가 지면에 집중돼 있습니다.
증거: `qa/evidence/art-apply/ground-{before,after,after-fixed}.png`

---

## 3. 이번 세션에 제가 틀린 것

| # | 주장 | 실제 | 왜 틀렸나 |
|---|---|---|---|
| 1 | 미소비 자산 9장 | 3장 | `GimmickSpriteLibrary.{상수}`만 검색, 문자열 리터럴 배선 누락 |
| 2 | `ui_launch_origin` 필요 | 중복 | 발사점엔 새총 애니메이션(1.6u)+힌트 라벨이 이미 있음. 코드 주석이 *"고리는 왜 뒤로 당기면 앞으로 날아가는지 설명하지 못했다"* 며 교체한 이력을 남김 |
| 3 | 무료 3건 배선만 하면 됨 | 3건 전부 중복 | `gimmick_muzzle_flash`=`fx_muzzle` 프레임이 이미 포신에서 재생 / `gimmick_shell`=대포가 발사체 자체(별도 포탄 없음) / `gimmick_wall_brick_cracked`=`block_cracked`+`block_heavily_cracked`가 3단계 담당 |
| 4 | 절차적 파편은 원 | 4~6각 랜덤 다각형 | `DebrisSystem.cs:133`을 읽지 않고 발주서를 씀. 진짜 차이는 형태가 아니라 **순백 실루엣 대 명암면** |
| 5 | 지면 타일이 화면 최대 면적 | 얇은 띠 | 하단 30%는 배경 아트(`Background_Stage1.png`). 지면 블록은 화면 y 456~617 |
| 6 | 궤적선/`stone` 채도 위반 | 계약 오류 | `LineRenderer`가 틴트(`:555`), `#5A5A5A`는 채도 0 - **제 발주서가 지정한 색** |
| 7 | 두 결과 화면 97.6% 동일 | 다른 구도 | 8×8 셀 평균이 빈 하늘을 포함해 과소평가 |
| 8 | dash `wrapMode 0` | `wrapU: 1`(Clamp) | 주석에 검증 없이 씀. 실제로 고쳐야 했음 |

**공통 오류**: 1~3은 전부 *"키가 안 쓰이나"* 를 물었고 *"기능이 이미 다른 아트로
충족되나"* 를 묻지 않았습니다. 4·6·8은 코드를 읽기 전에 문서를 썼습니다.

---

## 4. 적용된 24장과 배선

| 자산 | 소비처 | 배선 상태 |
|---|---|---|
| 지면 7장 | `GameManager.BuildGroundAtlasFromArt` (신규) | 신규 배선 + 파사드 결함 수정 |
| `fx_frost` 6장 | `EruptionVentGimmick.SpawnColumnFrameFx` | 페탈 차용 -> 전용 프레임. 차용 경로의 청색 틴트는 폴백 전용으로 유지 |
| `debris_chunk_01~04` | `DebrisPool.GenerateFragmentSprites` | 절차적 8종 -> 저작 4종 ×2. 절차 경로 폴백 유지 |
| `ui_impact_marker` | `LaunchManager` 조준 프리뷰 | 절차적 원(호박색을 픽셀에 구움) -> 무채색 아트 + 렌더러 틴트. 자기 성벽 명중 시 색 반전이 이제 곱셈 오염 없이 동작 |
| `trajectory_dash` | `LaunchManager` 궤적 `LineRenderer` | `LineTextureMode.Tile` + `wrapU: 0` |
| `victory_hero`/`defeat_keep` | `ResultsScreenController` | `Dim`(알파 0.88) 위, 읽을 값 아래 |
| `fx_spark_000` (교체) | 기존 | 182px -> 256px. 형제 3장과 일치하므로 `knownOffenders`에서 제거 |
| `CollapseDust` (교체) | 기존 | 채도 0.248 -> 0.403 |
| `ui_deploy_ghost` | `DeploymentController.EnsureGhost` | 이전 턴 배선 + 테스트 고정 |

`ui_launch_origin`은 디스크에 두고 **배선하지 않습니다**(§3-2).

---

## 5. 검증

| 항목 | 결과 |
|---|---|
| EditMode 전체 | **564/565**, 실패 0, Skip 1(`DefaultParticleTexture_...` 정당) |
| `GroundTileArtTests` | 6/6 |
| PlayMode 캡처 | 1/1 (`InGameUx_StatesCaptured`) |
| 임포터 | 24/24 `textureType 8` · `spriteMode 1` · `alphaIsTransparency 1` |
| guid | 프로젝트 전체 중복 0 |
| MCP 설정 | 캡처용으로 일시 조용화 후 **바이트 동일 복원**(sha256 일치 확인) |

**테스트가 실제로 일한 사례 2건** - 둘 다 제가 놓친 것을 잡았습니다:

- `LaunchManager_Visuals_AreInitializedAndUpdated` - 제 dash 코드가
  `trajectoryLine.material`을 **읽어** EditMode에서 머티리얼 누수 오류를 냈습니다.
  `.material`은 읽는 순간 사본을 만듭니다. `sharedMaterial`로 고쳤습니다.
- `EffectFrames_SizeMismatchesMatchTheRecordedBaseline` - `fx_spark`가 수리됐음을
  **테스트가 먼저 알렸습니다.** 기준선에서 제거했습니다.

---

## 6. 미해결

- **지면 5행 중 2행만 화면에서 지형으로 식별됩니다.** 나머지 3행 위치의 표본은
  배경 초지에 가깝습니다(RGB 거리 19~30). 아틀라스는 5행 전부 올바르므로
  (`TheAtlasRowsRunGrassDownToStoneTopToBottom` 통과) 렌더 단계 문제이거나
  **제 표본 x범위(250~700)에 성벽·방책이 겹친 탓**입니다. 후자를 배제하지
  못했습니다.
- **아트가 게임성을 늘렸다는 측정은 없습니다.** 픽셀이 바뀐 것은 쟀고
  플레이가 나아진 것은 재지 않았습니다.
- **32px 축소 문제**: 1 world unit = 32.14 화면 픽셀이므로 128px 타일이 32px로
  렌더됩니다. 타일 내부 디테일 대부분은 화면에 도달하지 못합니다.
  발주서는 이 비율을 고려하지 않고 썼습니다.
- **`Higgsfield`가 PATH에 없습니다.** `provenance` 없는 자산군 22개는 같은 톤
  추가 생성이 보장되지 않습니다.
