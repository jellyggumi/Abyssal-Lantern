# HUD 글씨 깨짐 — 원인과 수정

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: game-programmer / game-qa lane
- date: 2026-08-13
- 사용자 보고: *"글씨가 깨지고 있어, 버전업 후에"*
- 증거: `qa/evidence/font/`, `qa/evidence/hud-fix/`

---

## 재현

`KEEP CORE 150/150`이 화면에 **`KLLP CORL 150/150`** 으로 찍혔다.
E의 가로획 세 개 중 위·가운데가 사라져 L로 읽힌다. C·O·R·5·0도 획이 끊긴다.

원본 캡처를 8배 확대해 확인: `qa/evidence/visual/ux-3-player-turn.png`

---

## 원인 — 폰트가 아니라 캔버스였다

먼저 폰트 에셋을 의심했고, **틀렸다.** 기록해 둔다:

| 가설 | 검증 | 결과 |
|---|---|---|
| 아틀라스에 글리프 누락 | `LiberationSans SDF.asset` 파싱 | 대문자 26/26, 숫자 10/10 **정상** |
| 글리프 좌표 충돌 | E·L·K·P 등 rect 대조 | 전부 고유, 중복 0 **정상** |
| 한글 폰트 padding=0 | `NotoSansKR-Dynamic` 확인 | 런타임 padding=4 **정상** |
| 6000이 폰트를 재직렬화 | `git log` | 최종 변경이 최초 임포트 **무관** |

깨끗한 캔버스에 같은 문자열을 17pt로 그려보니 **완벽하게 나왔다**
(`qa/evidence/font/probe-17.png`). 폰트는 멀쩡했다.

### 실제 원인

HUD를 만드는 6개 호출부가 캔버스를 **순회 순서로** 집는다.

```
LaunchManager.cs:256, :278, :314    FindObjectOfType<Canvas>()
BrickPlacementController.cs:280                "
DeploymentController.cs:590                    "
GameFeelVfx.cs:766                  FindObjectsOfType<Canvas>() 의 첫 항목
SiegeAlarmSystem.cs:68                         "
```

`FindObjectsOfType`의 순서는 어떤 계약도 아니다. 실측하니 같은 프레임에
**서로 다른 캔버스**에 라벨이 붙어 있었다:

| 라벨 | 붙은 캔버스 | scaleFactor | 실효 px |
|---|---|---|---|
| TurnText | 씬 `Canvas` (ConstantPixelSize) | 1.000 | 24 |
| ControlGuideText | 씬 `Canvas` | 1.000 | 22 |
| **KEEP CORE** | **`NarrativeCanvas`** (ScaleWithScreenSize) | **0.385** | **6.5** |
| **보급 9/24** | `NarrativeCanvas` | 0.385 | **5.8** |
| **배치 모드 OFF** | `NarrativeCanvas` | 0.385 | **5.4** |

`NarrativeCanvas`는 **콜드오픈 영상의 캔버스**다. `GameFeelVfx`가 그걸 집은 뒤
스케일러를 1920×1080 ScaleWithScreenSize로 **덮어썼다** — 남의 캔버스를.

6.5px에서는 SDF 글리프의 가로획이 한 픽셀 행을 채우지 못해 사라진다.

계측: `qa/evidence/font/hud-font-scale.md`

### 6000 업그레이드 탓인가

**단정하지 않는다.** 2022.3이 이 머신에 없어 A/B 대조가 불가능하다.
확실한 것은 순회 순서에 의존하는 코드가 있고 그것이 지금 틀린 캔버스를 집는다는 것뿐이다.
순서가 언제 바뀌었는지는 미확인. `[INFERENCE]`

다만 6000이 `GetInstanceID()`를 `GetEntityId()`로 바꿨고
(`production/task-manifest.md` #39) `FindObjectsOfType`의 기본 정렬이 인스턴스 ID
기준이라는 점은 정황으로 남긴다.

---

## 수정

| # | 수정 | 파일 |
|---|---|---|
| 1 | `HudCanvas.Resolve()` — 이름으로 확정하는 단일 해결자 | `HudCanvas.cs` (신규) |
| 2 | 6개 호출부를 해결자로 이관 | 5파일 |
| 3 | 남의 캔버스를 재설정하지 않음 | `GameFeelVfx.cs`, `SiegeAlarmSystem.cs` |
| 4 | 폰트 크기 상수화 (26 / 23pt) | 4파일 |
| 5 | `HudScaleFloor` — 최소 창 아래로 안 줄어듦 | `HudScaleFloor.cs` (신규) |

### 크기 결정 근거

최소 지원 창을 1024×576으로 잡았다(WebGL 캔버스가 브라우저 창을 채우므로
노트북 브라우저가 실제로 제시하는 가장 작은 16:9). 그 창의 scale은 0.533이므로:

| | 26pt | 23pt |
|---|---|---|
| 1920×1080 | 26.0px | 23.0px |
| 1024×576 | **13.9px** | **12.3px** |
| 640×480 (클램프) | 13.9px | 12.3px |

하한 12px는 **관측으로 경계만 지었다** — 17px 정상, 6.5px 파손이 확인된 두 점이고
그 사이 어딘가다. 정확한 값은 모른다.

---

## 측정에 두 번 실패했다

기록해 둔다. 둘 다 그럴듯해 보이지만 작동하지 않는다.

**1차 — 잉크 면적 비교.** 크기별로 잉크/크기²를 비교해 곡선에서 떨어지는 지점을
찾으려 했다. 실패: 가로획 하나는 글리프 잉크의 몇 %라 샘플링 노이즈에 묻힌다.
결과가 10px 손실, 9px 정상, 7px 손실, 6px 정상으로 나와 단조롭지도 않았다.

**2차 — 슈퍼샘플 대조.** 같은 문자열을 8배로 그린 뒤 축소해 기준으로 삼고
직접 렌더와 비교했다. 실패: `fontSize`만 8배로 키우고 RectTransform은 그대로라
글자가 다른 자리에 놓였다. 정렬 불일치를 획 손실로 오독해 32px에서도 156% 불일치.

두 실패가 `HudCanvas.LegibleFloorPixels`의 주석에 남아 있다.

---

## 겹침 — 고치면서 새로 만든 것

`windText`·`scoreText`는 **캔버스 조상이 아예 없어** 매 턴 갱신되면서 한 번도
그려지지 않았다(UX-001/002). 입양해 보이게 만들었더니 **좌상단이 충돌했다.**

첫 겹침 테스트는 RectTransform을 쟀고 **통과했는데 화면은 겹쳐 있었다.**
TMP는 상자를 넘쳐 그린다. 글리프 실측(`textBounds`)으로 바꾸니 4건이 드러났다:

| 쌍 | 겹침 | 성격 |
|---|---|---|
| WindText × KEEP CORE | 27% | 입양이 드러냄 |
| WindText × 배치토글 | 40% | 입양이 드러냄 |
| WindText × 보급 | 41% | 입양이 드러냄 |
| 배치토글 × 보급 | **74%** | **원래 있던 것** |

원인은 전부 같다 — **상자는 1줄인데 글자가 2줄**이다.

| 수정 | 내용 |
|---|---|
| 바람 | `BANNER WIND EAST >>>\nSTEADY 1.1` → `WIND >>> 1.1` (EAST와 >>>가 같은 말) |
| 보급 | 상자 226→300, 줄바꿈 금지 |
| 배치토글 | -134→-152로 간격 확보, 상자 높이 26→34 |
| 코어 배지 | 0.18/0.82 → 0.325/0.675 (성 위로) |
| 턴 토스트 | 0.78 → 0.76 (상시 타이머에 양보) |

결과: **겹침 0** (`qa/evidence/hud-fix/hud-overlap.md`)

---

## 검증

| 검사 | 결과 | 실행 시점 | 증거 |
|---|---|---|---|
| `HudCanvasContractTests` (3) | 통과 | 병합 전 | 단일 캔버스 · 하한 · 남의 캔버스 불변 |
| `HudScaleFloorTests` (4) | 통과 | 병합 전 | 클램프 · 비례 · 퇴화 입력 |
| `HudOverlapTests` (1) | 통과 | 병합 전 | `qa/evidence/hud-fix/hud-overlap.md` |
| `HudFixEvidenceCapture` (1) | 통과 | 병합 전 | `qa/evidence/hud-fix/` 4개 창 |
| EditMode 전체 | **401/401** | 병합 전 | `qa/evidence/editmode-hud-canvas-fix.xml` |
| PlayMode 전체 | **73/77** | 병합 전 | `qa/evidence/playmode-hud-canvas-fix.xml` |
| EditMode 전체 | 405/405 | 병합 후 | **증거 없음** |
| 글리프 감사 (main 게이트) | 406/406 | 병합 후 | **증거 없음** |

> **병합 후 두 값은 미검증으로 취급한다.** 실행 출력에서 읽은 값이지만, 스크래치 정리에서
> XML·로그를 지우며 보관본을 남기지 않았다. 하네스 규칙은 *"증거 경로 누락 = 주장 값과
> 무관하게 FAIL"* 이다 — 값이 맞는지와 별개로 게이트를 통과시킬 수 없다.
> 재실행해 `evidence/`에 보관하는 것이 남은 작업이다.
>
> 재발 방지: **정리 전에 보관한다.** 이번엔 `mem.xml`(병합 후)이 아니라
> `fem.xml`(병합 전)을 보관한 뒤 원본을 지웠다.

회귀 방어의 요점: `HudLabelSizes_ClearTheLegibilityFloorAtTheSmallestWindow`가
출하 당시 크기(17/15/14pt)를 넣으면 **즉시 빨간불**이 된다. 이 결함이 다시
들어오면 소리가 난다.

---

## 고치지 않은 것

- **6000 인과 미확정.** 2022.3 부재로 A/B 불가.
- **인트로·웹툰 캔버스는 손대지 않았다.** 측정하지 않았고, 게임플레이 HUD와
  다른 수명·다른 소유자를 갖는다.
- **하한 12px는 경계값이지 측정값이 아니다.** 정확한 임계는 글리프 단위
  구조 검사가 필요하다.
