# UX-001/002 회귀 방어 증거 (F-1)

- date: 2026-08-18
- 산출: `Assets/Tests/PlayMode/HudCanvasContractTests.cs` 신규 2건
- 축 구분: **이 문서는 `qa/evidence/registers/`의 F-2 증거와 다른 축이다.** `cond1-*`은
  `DefectRegisterGateTests`가 "대장이 status를 표현할 수 있는가"를 증명한다. 여기는
  `HudCanvasContractTests`가 **"씬 저작 라벨이 HUD 캔버스에 붙는가"**를 증명한다.
  QA가 이 구분을 명시적으로 요구했다 — 한 축의 증거로 다른 축을 주장하는 것이 범주 오류다.

## 무엇이 방어되지 않고 있었는가

`GameManager.cs:1176-1177`이 `HudCanvas.Adopt(windText)` / `Adopt(scoreText)`를 무조건
호출하므로 UX-001/002는 **닫혀 있었다.** 그런데 그 호출을 지워도 스위트가 녹색이었다 —
HUD 테스트 5곳이 `if (canvas == null) continue;`로 **캔버스 없는 라벨을 표본에서 버리고**,
`HudCanvasContractTests.cs:344`의 주석이 그것이 UX-001/002라고 이름까지 적으면서 건너뛴다.

## 두 테스트

| 테스트 | 계약 |
|---|---|
| `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll` | 활성 그래픽은 **그려진다** — `:344`가 버리는 그 집합을 센다 |
| `SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas` | 씬이 저작한 라벨은 **HUD 캔버스에** 있다(아무 캔버스가 아니라) |

**결과를 재고 호출을 재지 않는다.** `Adopt`라는 단어가 단언에 없으므로 씬 재부모화·프리팹
재생산·코드 생성·입양 API 교체가 전부 통과한다 — 화면이 옳으면 통과한다.

## 증거 쌍

| 파일 | 상태 | 결과 |
|---|---|---|
| `ux001-002-defence-GREEN.xml` | 출하 상태 | **2/2 통과** |
| `ux001-adopt-removed-RED.xml` | `Adopt(windText)` 제거 | **0/1** — `Undrawn: WindText(TextMeshProUGUI) under [WindText]` |

빨강 메시지가 **씬 루트에 있다는 것을 부모 연쇄로 말한다**(`under [WindText]` — 조상이 자기
뿐). 사용자 원문 *"바람이 화면에 표시되지 않는다"* 의 기제 그대로다.

**원복 확인**: `shasum` 2파일 1해시 `54ecee29…`.

## 이 테스트가 자기 오탐을 하나 잡았다

기준선 첫 실행이 **빨강**이었고 범인은 `CaptureLabel` 2개였다. 그런데 그것은
`TMPro.TextMeshPro`(월드 스페이스, MeshRenderer)이고 `TextMeshProUGUI`(CanvasRenderer)가
아니다 — `CaptureZoneController.cs:57`이 존 링 위 0.7유닛에 만드는 월드 텍스트이고
**캔버스가 필요 없다.**

깨끗한 저장소에서 빨강인 테스트였고, 동료 레인이 한 시간 전 같은 파일의 형제 테스트에서
잡은 것과 **같은 결함 형태**다. 타입 목록이 아니라 **렌더러에 물어** 고쳤다:

```csharp
if (g.GetComponent<CanvasRenderer>() == null && g.GetComponent<Renderer>() != null) continue;
```

앞으로 어떤 클래스의 월드 그래픽도 이 줄을 고치지 않고 커버된다. **타입을 나열하면 다음
클래스에서 또 걸린다** — 이 파일의 `:299-301`이 정확히 그 이력을 적어 뒀다.

## 실행 조건 — PlayMode는 두 번 돌려야 한다

`CLAUDE.md`가 기록한 도메인 리로드 행이 이 세션에서 **네 번** 발생했다(902초·702초·746초
타임아웃 3회 + 1회차 행 1회). 규칙대로 **스크립트를 바꾸지 않고 연속 2회** 돌리면 2회차가
완주한다 — 기준선과 뮤테이션 둘 다 그렇게 얻었다.

제가 규칙을 알면서 한 번 어겼다: 1회차 행 뒤에 오탐을 고치고 재실행해 **재컴파일을 만들어
카운터를 리셋했다.** 두 번째 시도에서 순서를 지켜 얻었다.

## 남은 것

- **`regression-guard` 칸을 이 테스트 이름으로 채울 수 있다.** QA의 9열 스키마에서
  UX-001/002의 그 칸이 `확인 중`이었고, 이 XML 쌍이 그것을 `HudCanvasContractTests`
  두 테스트 이름 + `(씬)` 관측층으로 바꾼다.
- **`(씬)`은 픽셀을 뜻하지 않는다.** 동료 레인의 정정을 그대로 인용한다 — 이 테스트는
  부모 연쇄와 캔버스 이름을 재고 **렌더된 픽셀을 보지 않는다.** `709695ad`의 자백
  (*"values asserted, pixels never checked"*)이 같은 자리를 가리킨다.
- **UX-014는 여전히 open**이므로 게이트는 전부 차단이다. 이 증거는 그것을 바꾸지 않는다.
