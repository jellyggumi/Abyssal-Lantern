# HUD 입양 고정 — 지역·형태·구멍·사이드 결함

- lane: engineering (Programmer)
- run-id: 20260809-castle-war-stage1 (cycle 2)
- date: 2026-08-18
- 대상 질문: "모든 씬 저작 HUD 라벨이 HUD 캔버스 상위에 있다"를 **어떤 형태로 써야 하는가**
- 소유권 주의: `Assets/Tests/PlayMode/HudCanvasContractTests.cs`는 **부모(Main)가 편집 중**이다.
  이 문서는 그 파일을 읽고 검수만 한다. 편집하지 않았다.
- **인용 기준선**: `GameManager.cs`의 줄 번호는 전부 **HEAD 기준**이다
  (`git show HEAD:Assets/Scripts/GameManager.cs`로 검증). 기준선을 명시하는 이유는
  이 문서를 쓰는 동안 부모의 뮤테이션 증명이 `:1176 HudCanvas.Adopt(windText);`를 지웠다
  넣었기 때문이다 — 그 창 동안 작업 트리는 1176 이후가 전부 -1이었고
  (`gameOverPanel.SetActive(false)`가 HEAD `:2486` / 뮤테이션 트리 `:2485`),
  Director가 그 한 줄 차이를 S1 회귀로 의심했다. **뮤테이션은 이후 바이트 동일하게
  원복됐다** (`git diff --stat -- Assets/Scripts/GameManager.cs` 비어 있음, `Adopt` 4호출이
  1170/1176/1177/1178로 복귀) — 그래서 지금은 작업 트리 = HEAD이고 모든 인용이 그대로
  확인된다. 병행 세션에서는 **어느 기준선을 인용하는지 문서가 스스로 말해야 한다**:
  한 줄 어긋남도 회귀와 구별 불가능하게 생겼다. 다른 파일의 줄 번호도 작업 트리 = HEAD다.
- **⚠ `HudCanvasContractTests.cs`의 줄 번호는 스냅숏이다. 지속적 식별자는 테스트 이름이다.**
  그 파일은 이 검수 동안 **여섯 번 움직였다**(`Adopt(windText)` 삭제 → 바이트 동일 원복 →
  +122줄 → 오탐 2건 수정 → 공허통과 가드 → 여섯 번째). 본문의 `:357`, `:399`, `:322` 등은
  **초안 검수 시점**의 값이고, 아래 표로 재앵커하라.

  | 테스트/지점 (지속적 식별자) | 초안 검수 | 2차 | 3차 (2026-08-18) |
  |---|---|---|---|
  | `GameplayHudLabels_AllShareTheOneHudCanvas` | `:310` | `:332` | **`:348`** |
  | 그 안의 `if (canvas == null) continue;` skip | `:322` | `:349` | **`:365`** |
  | `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll` | `:357` | `:384` | **`:400`** |
  | `SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas` | `:399` | `:437` | **`:453`** |
  | `HudLabelSizes_ClearTheLegibilityFloorAtTheSmallestWindow` | `:482` | `:529` | **`:545`** |
  | `BootScene()` (§1.3 권고로 신설) | — | `:52-62` | 이름으로 인용 |
  | 활성 게이트 / 진단 분리 / 공허통과 가드 | — | `:467` / `:461`+`:486` / `:490` | 이름으로 인용 |

  **이 표 자체가 낡는다 — 3차 열은 다음 편집에 무효가 된다.** 그래서 값이 아니라
  **재생성 방법**을 남긴다. 어느 시점이든 이 명령이 현재 열을 만든다:

  ```
  # 도구: grep 도구 (bash 금지 — 조용히 빈다, W-14)
  # 범위: Assets/Tests/PlayMode/HudCanvasContractTests.cs
  # 패턴: public IEnumerator \w+\(\)          → 테스트 앵커
  #       if \(canvas == null\) continue;      → skip 지점
  ```

  **표의 범위**: 위 표는 **이 문서가 인용하는** 앵커만 담는다. 재생성 명령은 파일의
  테스트 **7개 전부**를 낸다(`:277` `:309` `:348` `:400` `:453` `:545` `:581` —
  QA 실행값). 표에 5개만 있는 것은 누락이 아니라 범위다 — 두 런타임 빌더 테스트와
  `HudSetup_DoesNotRewriteAnotherSystemsCanvas`는 이 문서가 줄 번호로 인용하지 않는다.
  **범위를 적지 않으면 7과 5의 차이가 낡음으로 읽힌다** — 카운트의 3항(도구·범위·패턴)이
  대응표에도 그대로 적용되는 자리다.

  **표를 손으로 쫓는 것은 지는 싸움이다.** 여섯 번 움직인 파일에 세 번 표를 고쳤고
  QA가 여섯 번째를 먼저 발견했다. 재현 명령을 적는 것이 표를 고치는 것보다 강하다 —
  **표는 과거를 재현하고 명령은 현재를 생성한다.** 둘 다 필요하고, 이것이 §8이 기록한
  세트 구조의 네 번째 사례다(이름 + 대응표 + **재생성 명령**).

  이것이 `regression-guard` 칸이 **줄 번호가 아니라 테스트 이름**을 받아야 하는 이유와
  같은 이유다(QA/PM 스키마). 이름은 여섯 번의 이동을 견뎠고 줄 번호는 한 번도 못 견뎠다.

---

## 0. 인테이크가 이 문서를 쓰는 동안 두 번 무효화됐다

이 레인의 첫 산출은 계획이 아니라 **전제의 정정**이다.

### 0.1 "고정이 없다"는 틀린 서술이었다 (더 나쁜 것이 있었다)

인테이크는 `grep -rln "Adopt|windText|orphan" Assets/Tests/` → 0건을 근거로 "회귀를 막는
것이 아무것도 없다"고 적었다. 실측:

```
grep -rn "Adopt\|windText\|orphan" Assets/Tests/
→ 1건: Assets/Tests/PlayMode/HudOverlapTests.cs:19 (주석)
```

"0건"은 틀렸다. **"단언 0건"이 맞다.** 부정확한 grep이 결함 규모를 과장했다.

그리고 진짜 상태는 "고정 없음"보다 나쁘다. 고정이 **있는 척하는 것**이 있었다.
HUD 스위트 전체가 고아 상태(`canvas == null`)를 **표본 정의에서 제외**한다:

| 위치 | 코드 | 성격 |
|---|---|---|
| `HudCanvasContractTests.cs:322` | `if (canvas == null) continue;   // not drawn at all — a separate defect, UX-001/002` | 주석이 UX-001/002를 **이름으로** 지목하고 건너뛴다 |
| `HudCanvasContractTests.cs:495` | `if (!t.isActiveAndEnabled \|\| t.canvas == null) continue;` | 크기 검사에서 제외 |
| `HudOverlapTests.cs:126` | `if (!t.isActiveAndEnabled \|\| t.canvas == null) continue;` | 겹침 검사에서 제외 |
| `HudFixEvidenceCapture.cs:146` | `if (!t.isActiveAndEnabled \|\| t.canvas == null) continue;` | 증거 캡처에서 제외 |

**4곳이다. 5곳이 아니다.** 부모가 5번째로 보고한 `VisualEvidenceCapture.cs:179`는
오탐이다 — 읽어보면 라벨 skip이 아니라 캔버스 렌더모드 **원복 루프의 널 가드**다:

```csharp
// VisualEvidenceCapture.cs:177-183
foreach (var (canvas, mode, worldCam, dist) in restore)
{
    if (canvas == null) continue;      // ← 원복 대상이 파괴됨, 라벨과 무관
    canvas.renderMode = mode;
```

`grep`으로 센 수를 읽지 않고 인용하면 4가 5가 된다. 이 사이클의 주제가 그것이다.

기록하는 유일한 자리는 단언이 아니다 — `HudFontScaleDiagnosis.cs:151-159`가
`**미렌더**`로 표에 적을 뿐이고, 진단 캡처는 게이트를 막지 않는다.

**결과**: `HudCanvas.Adopt(windText)` 한 줄을 지우면 `windText.canvas`가 null이 되고,
`:322`가 그것을 표본에서 버리고, `GameplayHudLabels_AllShareTheOneHudCanvas`(`:310`)는
**녹색**이다. 없는 테스트는 없다고 자백한다. 실패를 표본에서 빼는 테스트는 거짓말을 한다.

### 0.2 회의 중에 부모가 인테이크를 무효화했다

`git diff --stat` = `Assets/Tests/PlayMode/HudCanvasContractTests.cs | 122 +++++`.
부모가 회의 진행 중 두 테스트를 썼다: `:357 EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll`,
`:399 SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas`. 따라서 "고정이 없다"는
**작성 시점엔 옳았고 지금은 아니다.** 이 문서의 남은 일은 새로 쓰는 것이 아니라 **그 122줄을
검수**하는 것이다.

---

## 1. 지역 판정 — PlayMode 필수. 단, 현재 비용의 1/6로 줄일 수 있다

### 1.1 질문의 전제부터 틀렸다

과제는 "`SetupUIButtons`는 `Awake`/`Start` 경로에 있고 `GameManager.Instance`를 건드린다"고
적었다. **건드리지 않는다.**

```
grep -n "Instance" Assets/Scripts/GameManager.cs:1163-1211   → 0건
grep -n "GameManager.Instance\|Instance\." \
    Assets/Scripts/HudCanvas.cs MobileSafeArea.cs HudScaleFloor.cs → 0건
```

`SetupUIButtons`(`GameManager.cs:1163`)는 `Start()`(`:361`)에서만 불리고, 본문은 자기
직렬화 필드와 `HudCanvas`/`Style`/`Layout` 헬퍼만 만진다. 싱글톤 슬롯 탈취는 **이 경로의
문제가 아니다.**

그리고 EditMode에서 싱글톤을 훔치지 않는 법은 이 저장소에 이미 있다 — 과제가 지목한
전례가 정확하다:

| 전례 | 코드 |
|---|---|
| `CurrentRosterBalanceGateTests.cs:218-219` | `managerObject.SetActive(false);` **다음** `AddComponent<GameManager>()` |
| `KegPlacementSafetyTests.cs:202` | 동일 |
| `SkillGradingTests.cs:68-69` | 동일 |
| `AimDefaultReachTests.cs:46` | `go.SetActive(false);` 다음 `AddComponent<LaunchManager>()` |

비활성 GameObject에는 `Awake`가 발화하지 않으므로 `GameManager.Awake`(`:279-315`)의
`Instance = this`(`:284`)가 실행되지 않는다. `hideFlags = HideAndDontSave` +
`finally { DestroyImmediate }`가 세트로 붙는다.

### 1.2 그런데 EditMode로는 **이 계약**을 못 잰다 — 이유가 전제가 아니라 산출 시점이다

EditMode에서 가능한 것은 확인했다:
- `HudCanvas.Resolve()`(`:51`)는 `GameObject.Find` + `new GameObject` + `AddComponent`만
  쓴다 → EditMode 합법
- `HudCanvas.Adopt()`(`:112-134`)는 순수 `RectTransform` 재부모화다 → EditMode 합법
- EditMode asmdef이 `CastleBusters`, `Unity.TextMeshPro`를 참조하고
  `includePlatforms: ["Editor"]`이므로 `UnityEditor` 사용 가능
  (`Assets/Tests/EditMode/CastleBusters.EditModeTests.asmdef`, 전례
  `CurrentRosterBalanceGateTests.cs:5 using UnityEditor;`)

막는 것은 딱 하나다. **입양의 산출은 `Start()`가 만든다**(`GameManager.cs:361`). Unity는
EditMode에서 `Start()`를 돌리지 않는다. 그러므로 EditMode에서 결과를 보려면
`SetupUIButtons`를 리플렉션으로 직접 불러야 하는데, 그 본문은 입양 4줄에서 끝나지 않는다:

| 줄 | 부수 효과 |
|---|---|
| `:1196-1199` | `StyleSelectionButton` × 4 |
| `:1201-1204` | `LayoutSelectionRow` — 선택 행 재앵커 |
| `:1206-1209` | `AddComponent<GameButtonAnimator>()` × 4 |
| `:1210` → `:1234` | `SetupLastStandButton` → **`Instantiate(gimmickButton.gameObject, ...)`** |
| `:1250` | `GimmickSpriteLibrary.Load(...)` |

EditMode에서 열린 씬에 `Instantiate`를 하고 컴포넌트를 붙이면 **씬을 더럽힌다.** 그리고
이 저장소는 그 위험에 대해 이미 정책을 적어 뒀다:

> `StageAdvanceRegressionTests.cs:20-21` —
> "GameManager / ResultsScreenController / IntroScreenController are deliberately never
> constructed — they reach into scene singletons and SceneManager.LoadScene."

또 EditMode 전체에서 씬을 여는 테스트는 **0건**이다:

```
grep -rn "EditorSceneManager\|OpenScene\|SceneManager\." Assets/Tests/EditMode/
→ 1건, 그것도 위 정책 주석(StageAdvanceRegressionTests.cs:21)
```

씬 열기는 이 스위트에 전례가 없는 새 능력이고, 에디터 세션 상태를 바꾼다.
`StageAdvanceRegressionTests.cs:23-27`이 PlayerPrefs 하나 만지는 데도 SetUp/TearDown
스냅숏 규율을 요구하며 "그 규율을 건너뛰는 테스트를 여기 추가하지 마라"고 적었다.

**판정: 결과 계약은 PlayMode에 남긴다.** 이유는 "싱글톤 탈취 위험"이 아니라
**산출을 만드는 것이 `Start()`이고 EditMode에는 `Start()`가 없다**는 것이다.
EditMode로 옮기면 `SetupUIButtons` 리플렉션 호출 + 씬 오염 + 무전례 씬 열기 세 가지 비용을
치르고, 그래도 재는 것은 "내가 부른 메서드가 재부모화를 했다"이지 "게임이 부팅하면
플레이어가 본다"가 아니다. **지역을 바꾸면 계약의 의미가 약해진다.**

### 1.3 그러나 PlayMode 비용은 6배 과다하다 — 같은 파일에 싼 관용구가 있다

부모의 두 새 테스트는 `BootMatch()`(`:28-40`)를 쓴다:

```csharp
SceneManager.LoadScene(...);  yield return null;
yield return new WaitForSecondsRealtime(1.5f);   // ← 1.5s
gm.BeginSiege();              yield return null;
yield return new WaitForSecondsRealtime(1.5f);   // ← 1.5s
```

**3초의 실시간 대기**다. 그런데 입양은 `Start()`에서 끝나고 `BeginSiege()`는 플레이어
입력이다 — 입양을 보는 데 `BeginSiege`도, 3초도 필요 없다.

**같은 파일 `:123-189`에 정확한 관용구가 이미 있다.** `BootRuntimeHudBuilder`는
실시간 대기가 0이다:

```csharp
// :155-157
var loadOperation = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
yield return loadOperation;
// :172-176  "These explicit player-loop turns then exercise LaunchManager.Start ...
//            without relying on a wall-clock delay."
yield return null;
yield return null;
```

주석이 이유까지 적어 놨다 — **wall-clock 대기에 기대지 않는다.**

**권고**: 두 새 테스트를 `BootMatch`(3s)에서 `LoadSceneAsync` + 프레임 2회 관용구로
바꾼다. `Start()`가 끝났음을 프레임 턴으로 보장하고, `BeginSiege`는 부르지 않는다.
두 테스트 합쳐 **6초 → 0.x초**. 같은 파일 안의 두 부팅 관용구 중 비싼 쪽을 새 코드가
고른 것이므로, 이것은 취향이 아니라 **일관성 결함**이다.

---

## 2. 계약 형태 3후보 — 무엇을 고정하면 미래 수정을 막는가

과제의 가장 중요한 질문. 세 후보를 **각자 무엇을 부수는지**와 함께 적는다.

### 후보 A — "`Adopt` 호출이 소스에 있다" (소스 문자열 단언)

```csharp
StringAssert.Contains("HudCanvas.Adopt(windText)", File.ReadAllText("Assets/Scripts/GameManager.cs"));
```

**이 저장소에 전례가 있다** — `GroundAtlasBudgetTests.cs:103-107`:
```csharp
var source = System.IO.File.ReadAllText("Assets/Scripts/GameManager.cs");
StringAssert.Contains("if (groundTex == null) continue;", source, ...);
```

**방해 범위 (가장 넓다)**:
- 씬 재부모화로 고치기 → 막힘 (호출이 사라지므로 빨강, 그런데 화면은 옳다)
- 프리팹 재생산으로 고치기 → 막힘
- 라벨을 코드 생성으로 옮기기 → 막힘
- 입양을 `SetupUIButtons` → `Awake`로 이동 → 통과(문자열은 그대로), **그러나 잘못 통과**
- 변수명 `windText` → `windReadout` 리팩터 → 막힘

**기각.** 메커니즘을 고정하고 결과를 고정하지 않는다. 화면이 옳은데 빨강이 되고, 화면이
틀렸는데 녹색이 될 수 있다. 이름 리팩터까지 막는다. `GroundAtlasBudgetTests`의 전례는
**널 처리 존재**라는, 결과로 잴 수 없는 것에 쓴 예외이고(`:95-98`이 그 이유를 적는다)
여기 복제할 근거가 아니다.

### 후보 B — "라벨은 정확히 4개이고 각각 HUD 캔버스에 있다" (선언 목록)

```csharp
foreach (var name in new[]{"WindText","ScoreText","TurnText","TimerText"}) { ... }
```

**방해 범위**:
- 5번째 HUD 라벨 추가 → **목록에 없으면 검사되지 않는다.** 막지는 않고 **놓친다**
- 라벨 이름 변경 → 막힘
- 라벨 제거 → 막힘

**기각.** 그리고 이것은 `CLAUDE.md`가 이번 세션에 적은 불변식의 정면 위반이다 —
"선언된 목록을 도는 테스트는 목록에 없는 것을 못 본다." 부모의 `:352-353` 주석이 같은
결론에 도달해 있다: *"a count of four goes stale the moment a fifth label ships."*
`:299-301`이 그 사고의 실측 기록이다 — 타입 목록을 `TextMeshProUGUI`로 좁혔더니 새
`Image`(`SelectedUnitPortrait`)가 옛 경로로 새고 스위트는 녹색이었다.

### 후보 C — "활성 그래픽에 캔버스 조상이 있다" (발견된 집합 + 결과 단언) ← **선택**

```csharp
foreach (var g in FindObjectsByType<Graphic>(...)) {
    if (!g.isActiveAndEnabled) continue;
    if (g.canvas != null) continue;
    undrawn.Add(...);            // 부모의 :357-372 가 정확히 이 형태다
}
```

**방해 범위 (가장 좁다)**:
- 씬 재부모화로 고치기 → **통과**
- 프리팹 재생산 → **통과**
- 코드 생성으로 전환 → **통과**
- 라벨 이름/개수 변경 → **통과**
- `Adopt` API 폐기·교체 → **통과**
- 막는 것: "활성 그래픽이 화면에 안 그려진다" 하나. **그것이 정확히 UX-001/002다.**

**선택 이유**: 결과를 재고, 집합을 선언하지 않고 발견하며, 고치는 방법을 규정하지 않는다.
`Adopt`라는 단어가 단언에 등장하지 않는다 — 그래서 입양을 없애는 수정도 화면이 옳으면
통과한다.

**부모의 `:357`이 이미 후보 C다.** 새로 쓸 것이 없다. 검수 결과는 §3.

### `:322` 존치 판단

부모가 물었다: `:322`의 skip을 지워야 하는가. **아니다, 남기는 것이 옳다.**

`:310`은 "한 캔버스에 모여 있는가"(**갈림**)를 재고, 캔버스가 없는 라벨은 그 질문의
대상이 아니다 — 비교할 캔버스가 없다. 표본 정의로서 `:322`는 여전히 정확하다.
이제 그 버려진 집합을 세는 짝(`:357`)이 생겼으므로 두 테스트는 상보다.

**다만 한 가지가 결함이다**: `:322`의 주석은 "a separate defect, UX-001/002"라고
적으면서 **어디서 세는지를 적지 않는다.** 그 상태로는 다음 읽는 사람이 `:322`를 구멍으로
읽는다 — 내가 이 회의 초반에 정확히 그렇게 읽었다. 한 줄이면 해결된다:
`// counted by EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll` (`:357`).
반대 방향 두 테스트가 한 파일에 있을 때, 상보성은 **명시되지 않으면 존재하지 않는다.**

---

## 3. 부모의 122줄 검수 — `SceneAuthoredHudLabels_...`는 깨끗한 저장소에서도 빨강이었다 (수정됨, §3 후속)

`:357 EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll` — **결함 없음.** 후보 C
그대로다. `:364`에서 `isActiveAndEnabled`를 검사하고, `:370`이 부모 연쇄 전체를 출력해
"루트 저작"과 "서브트리 탈락"을 구분한다. 승인.

`:399 SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas` — **오탐 2건. 현재 상태로는
커밋 불가.**

리플렉션이 훑는 집합은 6개다(`Instance|Public|NonPublic` × `TMP_Text` 대입 가능):

```
grep -nE "(TextMeshProUGUI|TMP_Text|TextMeshPro)[[:space:]]+[a-zA-Z_]+[[:space:]]*[;=]" \
     Assets/Scripts/GameManager.cs
→ :95 turnText  :96 timerText  :97 resultText  :98 windText  :99 scoreText  :118 gimmickStatusText
```

`Adopt` 호출은 HEAD에서 4곳이다(현재 작업 트리는 부모의 뮤테이션으로 3곳):

```
git show HEAD:Assets/Scripts/GameManager.cs | grep -n "HudCanvas.Adopt"
→ 1170 turnText   1176 windText   1177 scoreText   1178 timerText
```

`resultText`와 `gimmickStatusText`는 **입양 대상이 아니다.** 그런데 리플렉션은 둘을 훑는다.

### 오탐 1 — `gimmickStatusText`: 죽은 필드를 결함으로 보고한다

| 사실 | 근거 |
|---|---|
| 씬이 비워 뒀다 | `Assets/Scenes/SampleScene.unity:1508` `gimmickStatusText: {fileID: 0}` |
| 어디서도 쓰지 않는다 | `grep -rn "gimmickStatusText" Assets/Scripts/` → **1건, 선언뿐**(`GameManager.cs:118`) |
| 테스트가 무조건 담는다 | `HudCanvasContractTests.cs:425` `problems.Add($"{field.Name}: field is empty, so nothing was adopted and nothing draws")` |

읽기·쓰기가 0건인 필드다. 씬이 비워둔 것이 옳다. `problems`에 들어가면 안 된다.

### 오탐 2 — `resultText`: 어느 분기로 가도 실패한다

| 사실 | 근거 |
|---|---|
| 씬이 배선했다 (null 아님) | `SampleScene.unity:1499` `resultText: {fileID: 472746463}` |
| 부모가 비활성 패널이다 | `ResultText.m_Father: {fileID: 871755123}`(`:1729`) = `GameOverPanel`, 그 `m_IsActive: 0`(`:2716`) |
| 입양 대상이 아니다 | `Adopt` 4호출에 `resultText` 없음 (위 표) |
| 패널은 켜지지도 않는다 | `GameManager.cs:2486` `if (gameOverPanel != null) gameOverPanel.SetActive(false);` — 결과 카드로 대체된 레거시 |

두 분기 모두 `problems`에 들어간다:
- `.canvas`가 null → `:433` `"no Canvas ancestor - invisible"`
- `.canvas`가 씬 Canvas로 풀림 → `:435-440` `"on canvas 'Canvas' instead of 'GameplayHudCanvas'"`

**그래서 Unity의 `Graphic.canvas`가 비활성 조상에서 뭘 반환하는지는 측정할 필요가 없다.**
결론이 같다. 추측을 우회해서 결론이 나오는 형태로 논증을 짰다.

### 이것이 지금 진행 중인 뮤테이션 증명을 무효화한다

작업 트리는 `-HudCanvas.Adopt(windText);` 상태다(`git diff`, 1 file +0 -1). 부모가 예고한
증명이다. 그런데 문제 개수가:

| 상태 | `problems` |
|---|---|
| 뮤테이션 (현재) | `gimmickStatusText`, `resultText`, **`windText`** = 3 |
| 원복 후 | `gimmickStatusText`, `resultText` = **2, 0이 아니다** |

`checkedCount`는 5(`gimmickStatusText` 제외)이므로 `:444`의 `Assert.Greater(checkedCount, 0)`는
통과하고, `:448`의 `Assert.IsEmpty(problems)`가 **양쪽 다 빨강**이다.

**빨강/녹색으로는 뮤테이션이 잡혔는지 알 수 없다.** 판정 기준을 `problems`의 **원소
이름**으로 잡아야 한다 — 뮤테이션 시 `windText` 항목이 *추가로* 나타나는가.

### 수정 방향 (파일은 부모 소유, 편집하지 않음)

문제의 뿌리는 한 파일 두 테스트의 **비대칭**이다. `:357`은 `:364`에서
`isActiveAndEnabled`를 검사하고, `:399`는 검사하지 않는다. 같은 파일에서 같은 위험을
한쪽만 막았다.

1. `:412` 루프에 `isActiveAndEnabled` 게이트를 넣는다 — `resultText` 오탐이 사라진다.
   `:357`이 이미 그 형태이므로 **새 판단이 아니라 일관성 복구**다.
2. 빈 필드 보고는 **유지가 옳다**(부모의 `:394-395` 근거가 정확하다 — `Adopt(null)`이
   조용히 반환하므로 빈 필드와 입양된 라벨이 하류에서 구별되지 않는다). 그러나
   `Assert` 대상이 아니라 **진단 출력**으로 내려야 한다. 이름 화이트리스트는 후보 B가
   되므로 금지.
3. `BootMatch` → `LoadSceneAsync` + 프레임 2회 (§1.3).

### 후속 — 세 권고가 전부 채택됐고 기준선이 0으로 돌아갔다 [OBSERVED 2026-08-18]

부모가 이 검수를 받아 고쳤다. 현재 파일에서 확인한 것:

| 권고 | 반영 | 위치 |
|---|---|---|
| 1. `isActiveAndEnabled` 게이트 | `if (!label.isActiveAndEnabled) continue;` + 이유 주석("a results-screen label under a closed panel is not a HUD label yet") | `:467` |
| 2. 빈 필드를 Assert에서 진단으로 | `unwired` 목록 분리(`:461`) → `Debug.Log("[hud-pin] …")` | `:461`, `:486-488` |
| 3. `BootMatch` → 프레임 2회 | 신규 `BootScene()` — `LoadSceneAsync` + `yield return null` × 2 | `:52-62`, 사용처 `:386`, `:439` |

그리고 **내가 권고하지 않은 것 하나가 더 들어왔고 그게 옳다**: `:490`
`Assert.Greater(checkedCount, 0)`. 활성 라벨이 하나도 없으면 `problems`가 비어 **공허하게
통과**하는데, `:467`의 스킵을 넣으면 그 구멍이 새로 생긴다. 내 권고 1이 만든 위험을
부모가 같은 편집에서 막았다. `:493`이 실패 메시지에 `unwired` 목록을 실어 "왜 0인가"까지
답한다.

`BootScene`의 docstring(`:43-51`)이 §1.3의 근거를 그대로 적었다 — `BeginSiege`는 플레이어
행동이고 입양은 `GameManager.Start`(`:361`)에서 끝나므로 프레임 2회면 된다는 것,
그리고 그 관용구가 같은 파일 `:175-176`에 이미 있었다는 것.

**그래서 §3 제목의 "깨끗한 저장소에서도 빨강"은 이제 과거형이다.** 기준선 `problems`는
0이고, 뮤테이션 판정은 원소 이름을 읽지 않고 빨강/녹색으로 할 수 있다. 내가 지정한
판정 기준(원소 이름)은 **오탐이 살아 있는 동안만 필요했던 우회**이고, 오탐이 사라지면서
함께 필요 없어졌다 — 그것이 애초에 권고한 순서("오탐을 먼저 고치면 기준선이 0이 되고
그때는 빨강/녹색이 그대로 판정이 된다")였다.

Director의 W-15가 이 자리를 정확히 가리킨다: **합격 조건을 숫자로 쓰면 낡는다.**
세 판본(오탐 2건 → 뮤테이션 3건 → 수정 후 0건)에서 참인 것은 숫자가 아니라 관계였다 —
*"뮤테이션이 `windText` 항목을 추가하고 원복이 그것을 없앤다."*

### `Image` 확대 판단 — **확대하지 않는다**

부모가 물었다. `:299`가 `SelectedUnitPortrait` 누출을 기록하므로 리플렉션을 `Image`로
넓힐지. 답: 넓혀도 **아무것도 찾지 못한다.**

```
grep -nE "(UnityEngine\.UI\.)?Image[[:space:]]+[a-zA-Z_]+[[:space:]]*[;=]" \
     Assets/Scripts/GameManager.cs      → 0건
grep -rn "SelectedUnitPortrait" Assets/Scripts/
→ LaunchManager.cs:386  var go = new GameObject("SelectedUnitPortrait");
```

`GameManager`에 `Image` 필드가 없고, `SelectedUnitPortrait`는 **직렬화 필드가 아니라
`LaunchManager`가 코드로 만드는 객체**다. `:399`의 역할은 "**씬이 저작한** 것이 입양됐는가"
이므로 코드 생성물은 그 테스트의 대상이 아니다 — 그것은 `:310`/`:357`이 `Graphic` 전체를
훑어 이미 덮는다. 확대하면 두 테스트의 경계가 흐려지고, 얻는 것은 없다.

---

## 4. `Adopt(null)` 구멍 — 잡을 수 있다. 씬 YAML을 읽을 필요는 없다

```csharp
// HudCanvas.cs:112-114
public static void Adopt(Component element)
{
    if (element == null) return;      // ← 조용한 반환
```

과제의 시나리오: 누가 씬에서 `windText` 배정을 날린다. 호출(`GameManager.cs:1176`)은 살아
있고, `Adopt`는 조용히 반환하고, 화면은 다시 말이 없다.

**후보 C(= 부모의 `:357`)가 이것을 잡는가?** 아니다. 필드가 비면 `windText`가 가리키던
`WindText` GameObject는 여전히 씬 루트에 있고 활성이므로 `:357`은 그것을 잡는다 —
**단, 그 오브젝트가 씬에 남아 있는 경우에만.** 배정만 날아가고 오브젝트가 남으면 잡는다.
오브젝트까지 삭제되면 잡을 것이 없고, 그건 결함이 아니라 제거다.

**그러므로 `:357` + 빈 필드 진단 두 개로 충분하고, 씬 YAML을 읽을 필요가 없다.**
`:399`가 하는 리플렉션 발견이 바로 그 진단이다 — 부모의 `:394-395` 판단이 옳다.
필요한 것은 그 보고를 `Assert`에서 진단으로 내리는 것뿐(§3).

### 씬 YAML 읽기의 보험성 대가 (과제가 요구한 항목)

전례는 있다 — `LaunchPowerCurveTests.cs:173-190`이 `SampleScene.unity`를 `File.ReadAllText`
하고 정규식으로 `maxLaunchVelocity`를 뽑아 코드 상수와 비교한다. 그 테스트가 자기 존재
이유를 `:166-170`에 적었다:

> "the field default is NOT what runs: Unity serializes public fields into the scene, so an
> edited default is ignored while the scene keeps its old value. That exact trap cost this
> project a session before — the scene carried maxLaunchVelocity: 25.2 twice."

**그 조건이 여기에는 성립하지 않는다.** 그 테스트가 YAML을 읽는 이유는 *씬 값이 코드
값을 이긴다*는 것이고, 입양은 반대다 — 씬이 *어디*를 갖고 HUD 캔버스가 *얼마나 크게*를
갖는다(`HudCanvas.cs:109-110`). 런타임 부모는 씬 값이 아니라 `Start()` 결과다.

대가를 그래도 적는다:
- **좌표계 대가**: `m_Father: {fileID: N}`가 가리키는 것은 `RectTransform`이고, 부모
  GameObject 이름을 얻으려면 fileID → GameObject → `m_Name` 2단 역참조가 필요하다.
  나 자신이 `resultText` 사슬을 밟는 데 세 번 읽었다(`:1499` → `:1729` → `:2716`).
- **낡음 대가**: 씬 상태가 결함의 정의가 아니다. 인테이크가 `m_Father: 0`을 보고
  "문서가 낡아 보이는 이유"로 쓴 것처럼(§0), 씬 YAML은 **수정 전 상태를 계속 보여준다.**
  YAML을 단언하면 "씬을 고쳐서 해결"하는 미래 수정이 막힌다 → 후보 A와 같은 병.
- **얻는 것 없음**: 런타임 결과를 이미 `:357`이 재고 있다.

**판정: 씬 YAML은 앵커의 출처로만 쓰고 단언하지 않는다.** 인테이크 §"하지 않을 것"의
결론과 일치하고, 도달 경로는 다르다 — 나는 "두 수정이 같은 것을 두 번 한다"가 아니라
"YAML 단언이 씬 수정 경로를 막는다"로 도달했다.

---

## 5. 사이드 구조 결함 실측 — **확인 0건.** QA의 5건은 낡았다

과제 후보: `#if UNITY_EDITOR` 안에서만 자산을 얻는 런타임 경로. QA가 5건 잔존이라
보고했다. 세었다.

```
# 도구: grep 도구 (bash 아님 — bash는 이 저장소에서 조용히 빈 결과를 낸다, W-14)
# 범위: Assets/Scripts (Assets/ 전체가 아니다 — 테스트·씬의 언급은 제외)
# 패턴: ^\s*#if UNITY_EDITOR  (`#` 필수. 빼면 산문 주석이 섞인다)
→ 지시자 8건 / 6파일
```

**정정**: 이 문서의 이전 판은 "10건 / 7파일"이라고 적었다. 틀렸다. bash로
`grep -rh "if UNITY_EDITOR"`를 돌렸는데 패턴에 `#`가 없어서 **산문 주석 2건**
(`ExplosionFrames.cs:16`, `GameManager.cs:955` — 둘 다 *이미 고친* 결함을 설명하는 문장)이
지시자로 집계됐다. 실제 지시자는 `GimmickSpriteLibrary.cs:53`, `LaunchManager.cs:765/923/950`,
`MobileStorefront.cs:132`, `SpriteAtlasPacker.cs:177`, `UnitController.cs:1092/1315`의 **8건**이다.
결론(확인 0건)은 유지된다 — 늘어난 2건이 주석이었고 자산 획득 4건의 런타임 경로 판정은
그대로다. 그러나 **수가 틀렸고, 그 수로 QA의 5건을 정정한 문서였다.**

8건을 하나씩 읽고, "런타임 경로가 없다"를 성립시키는지 확인했다:

| 위치 | 자산 획득 | 런타임 경로 | 판정 |
|---|---|---|---|
| `GimmickSpriteLibrary.cs:53` | `AssetDatabase` | **있다** — `:52` `Resources.Load<Sprite>($"Gimmicks/{key}")`가 **먼저** 실행 | 결함 아님 |
| `SpriteAtlasPacker.cs:177` | `AssetDatabase` | **있다** — `:158` 씬 렌더러 수집 + `:167` `Resources.LoadAll("GeneratedUnitFrames")`가 먼저. 주석도 `:176` "Editor-only **fallback**" | 결함 아님 |
| `UnitController.cs:1092-1094` | `AssetDatabase` (`Arrow.prefab`) | **있다** — `Archer.prefab:135` `arrowPrefab: {guid: 02db9a978e95e42dd8d42fe20a19a802}` | 결함 아님 |
| `UnitController.cs:1315-1317` | `AssetDatabase` (`ExplosionEffect.prefab`) | **있다** — `Archer.prefab:133`, `Knight.prefab:133`, `ExplosiveBarrel.prefab:176` 배선 | 결함 아님 (단서 아래) |
| `MobileStorefront.cs:132` | 자산 아님 | `:135` `#else` 분기 존재 | 결함 아님 |
| `LaunchManager.cs:765, 923, 950` | 자산 아님 | `#if UNITY_EDITOR \|\| DEVELOPMENT_BUILD`, 시뮬레이트 포인터 **테스트 심** | 결함 아님 |

guid 대조로 확인했다:
```
grep -n guid Assets/Prefabs/Arrow.prefab.meta   → guid: 02db9a978e95e42dd8d42fe20a19a802
grep -rn "arrowPrefab\|explosionEffectPrefab" Assets/Prefabs/
  → Archer.prefab:135  arrowPrefab: {guid: 02db9a978e95e42dd8d42fe20a19a802}   ← 일치
  → Knight.prefab:135  arrowPrefab: {fileID: 0}                                ← 근접 유닛, 정상
```

**확인된 결함 0건.** QA의 "5건 잔존"은 낡았다 — 이번 세션에 `GimmickSpriteLibrary`의
쓰기 제거와 `SpawnBalanceGate` 수정이 그 수를 줄였고, 남은 것은 전부 런타임 경로가
검증된 편의 fallback이다.

### 나 자신의 중간 오판을 기록한다

나는 `.cs`만 읽고 `UnitController.cs:1092`, `:1315`를 **결함 2건으로 세었다.** 근거는
"`#endif` 직후에 fallback이 없다"였고, 그것은 파일 안에서는 사실이다. 프리팹 배선을
확인하고서야 무효가 됐다. `#if` 블록을 소스만 보고 세면 이 저장소가 두 번 대가를 치른
것과 같은 종류의 실수가 된다 — **직렬화된 값이 코드 기본값을 이긴다**
(`LaunchPowerCurveTests.cs:166-170`). 5 → 2 → 0.

### 단서 하나 — 코드 생성 배럴은 열화되지만 침묵하지 않는다

프리팹이 아니라 코드로 만드는 유닛은 `explosionEffectPrefab`이 null이다:
`DeploymentController.cs:412`, `:441`, `GameManager.cs:1209` (`AddComponent<UnitController>()`).
그런데 `UnitController.cs:1319`가 `if (explosionEffectPrefab != null)`로 가드하고,
`:1320-1321`의 `GameFeelVfx.SpawnImpactBurst` / `SpawnShockwaveRing`이 **무조건** 실행된다.
폭발은 프리팹 없이도 보인다. **열화이고 침묵이 아니다** → S1의 "화면에 아무것도 없다"
부류가 아니다.

### 다른 후보 두 개 — 세지 않았다

과제가 "이벤트 구독이 날아가는 경로"와 "상수 드리프트가 가능한 자리"도 후보로 적었다.
**추정하지 않는다.** 이 레인에서 grep으로 세지 않았으므로 수를 적지 않는다. 상수 드리프트에
대해서는 이미 집행하는 테스트가 있다(`LaunchPowerCurveTests.cs:173`가 씬↔코드 대조,
`HudLayoutTests.cs:19-31`이 HUD 기하 관계 대조). 이벤트 구독은 다음 사이클 항목이다.

### 다른 결함 부류 하나는 실제로 셌다 — 제어 흐름 순서 때문에 도달 불가한 분기 (QA의 UX-018)

QA가 `SiegeAlarmSystem.cs:241`의 파란색 분기(플레이어 자기 샷 판독)가 출하 경로에서
도달 불가라고 보고하고, "절대적 죽은 코드는 아니다 — 봉인 없이 적 턴이 끝나는 경로가
4개 있다"는 단서를 달았다. **핵심 주장은 옳고, 기제를 특정했고, 도달 경로는 3개다**
(내 첫 판은 2개라고 적었고 틀렸다 — 아래 표의 경로 4).

**도달 불가의 근거** (QA 주장 확인):
`Seal()` 호출처는 **1곳**이다 — `GameManager.cs:2339`(QA는 `:2338`로 인용, 한 줄 어긋남).
그 직후 `:2343 EndTurn()` → `:2366 isPlayerTurn = !isPlayerTurn` **무조건 교대**다.
그러므로 플레이어 샷이 봉인되면 곧바로 적 턴이고, `SiegeAlarmSystem :217 !IsPlayerTurn`이
먼저 잡는다. 플레이어 턴이 돌아올 때는 적이 자기 샷을 봉인해 놓았으므로 `:234`에 도달할
때 `LatestLineByPlayer`는 false다 → **주황만 실행되고 `:242` 파란색은 실행되지 않는다.**

**그런데 도달 경로가 있다. 기제는 `Seal()`의 조기 반환이다:**

```
ShotTraceDirector.cs:248   public static void Seal() { if (!shotOpen) return; ...
                :251,:260  LatestLine = ...;  LatestLineByPlayer = shotByPlayer;
```

적이 **샷을 열지 않고** 턴을 끝내면 `shotOpen`이 false이므로 `Seal()`이 `:248`에서
반환하고 `:251/:260`을 건드리지 않는다 — **플레이어의 줄과 `LatestLineByPlayer = true`가
그대로 살아서** 다음 플레이어 턴으로 넘어간다. 그때 `:234`가 참이고 파란색이 실행된다.

그 경로를 전수 확인했다. **첫 판에서 2개라고 적었고 틀렸다** — QA/Designer의 3개가 맞다:

| 경로 | 코드 | 턴이 끝나는가 | 파란색 도달 |
|---|---|---|---|
| 1 `SimpleAI.cs:43` | `unitPrefabs` 없음 → `OnUnitLaunched(null)` | 예 (`:2268 WaitAndEndTurn`) | **예** |
| 2 `SimpleAI.cs:67` | `prefab == null` → `OnUnitLaunched(null)` | 예 | **예** |
| 3 `SimpleAI.cs:94` | `!TryCommitTurnShot()` → 맨 `yield break` | **아니오** — `OnUnitLaunched` 미호출로 `EndTurn` 미도달 | 아니오 |
| 4 `SimpleAI.cs:114` | **`UnitController` 없는 프리팹** — `:101` 가드가 `unit.Launch()`를 건너뛰어 샷이 열리지 않는데, `:114 OnUnitLaunched(unit)`은 **가드 밖이라 무조건 실행** | 예 | **예** |

**3개다.** 경로 4가 내가 놓친 것이고, 놓친 이유에 형태가 있다: 나는 명시적 `yield break`만
세고 `:94`에서 읽기를 멈췄다. **정상 낙하 경로가 샷을 열지 않고도 `Seal()`에 도달할 수
있다는 것을 확인하지 않았다.** `:100 var unit = unitGo.GetComponent<UnitController>();`가
null이면 `:101`이 통째로 건너뛰어지는데 `:114`는 그 블록 밖에 있다.

이것은 §0에서 인테이크를 정정하며 내가 이름 붙인 실패의 같은 종이다 —
**"선언된 목록을 도는 것은 목록에 없는 것을 못 본다."** 내 목록은 "`yield break`가 있는 줄"
이었고, 경로 4에는 `yield break`가 없다.

세 경로 전부 프리팹/배선 결함을 전제하므로 **정상 배선에서 파란색은 절대 실행되지 않는다**
— QA의 핵심 판정은 그대로 맞다.

덧붙임: 첫 플레이어 턴에는 `LatestLine`이 비어 `:245 else`가 잡아 스트립을 숨긴다
(`:248`). 그래서 결함 상태에서도 파란색은 **플레이어의 두 번째 턴부터**만 나타난다.

**등재 시 이 구분을 적어야 반증당하지 않는다**(QA가 요청한 것): "죽은 코드"가 아니라
**"정상 배선에서 도달 불가, 배선 결함 시 3경로로 도달"**이다. 그리고 Designer의 문안이
더 낫다 — *"플레이어가 자기 샷 판독을 보는 유일한 경우는 적이 발사에 실패했을 때"*,
즉 **판독이 뜨는 것 자체가 이상 신호**다.

## 6. F-2 초안 — 차단 술어를 디스크에서 집행한다 (Director 지정)

Director가 "status 열은 손으로 넣으면 손으로 낡는다"고 판정했다. 이 레인의 설계 답:

**형태**: EditMode 테스트. `_workspace/current/qa/*.md`를 **디스크에서 읽고** 파싱한다.
파일 목록을 테스트에 적지 않는다 — `Directory.GetFiles`로 발견한다.

전례가 전부 있다:

| 능력 | 전례 |
|---|---|
| `_workspace` 경로 접근 | `CastleMaterialCensusTests.cs:31-33` `Path.Combine(Application.dataPath, "..", "_workspace", "current", "qa", ...)` |
| 디스크 파일 발견(선언 아님) | `ResourceSpriteImportTests.cs:283` `Directory.GetFiles(root, "*", SearchOption.AllDirectories)` |
| md 읽고 줄 단위 단언 | `WorldLabelLegibilityTests.cs:229-233`, `AccessibleBlinkTests.cs:155-159` |

**술어 6개** (1~3은 Director의 F-2 그대로, 4는 그가 추가 지정, 5는 PM 측정 후 그가 추가,
6은 내 구현 제약이 그의 결정 로그에서 독립 조건으로 승격된 것 —
`production/decision-log.md:473`):
1. 심각도가 있고 status가 없는 항목 → **실패**. Director의 판정 §3("심각도가 있고 status가
   없으면 open으로 읽는다")을 집행 가능하게 만든 것.
2. S1 open이 하나라도 있는데 게이트 PASS 주장이 있으면 → **실패**.
3. 정본 선언이 없으면 → **실패** (두 대장 중 무엇이 정본인가).
4. **closed 근거 커밋이 배포에 도달했는가** → 미도달이면 실패. **P-shipped만 게이트,
   P-merged는 진단**(아래 정정).
5. **배포 출처가 기록됐는가**(해시 + 브랜치) → 미기록이면 실패.
6. **끝점은 원격 추적 ref로 표기한다. 로컬 브랜치명 금지.** 아래 "구현 제약" 절이 근거이고,
   Director가 *"이것 하나로 F-2가 정반대 답을 낸다"*는 이유로 독립 조건으로 올렸다.

### 조건 4 — 내 첫 설계가 틀렸다. Director가 그것을 이미 채택했으므로 정정이 먼저다

나는 조건 4를 4a("`origin/main` 조상인가", 선행 조건 없음)와 4b("라이브 배포 조상인가",
W-7 선행)로 갈랐고 Director가 그대로 결정 본문에 넣었다. **4a를 게이트로 쓰면 안 된다.**
조건 5의 근거 수치를 검증하다 반증됐다.

PM이 좁힌 라이브 소스 후보 4개를 전수 검사했다:

```
$ for c in 3da3dd9c 747f926d 4861a266 73f79240; do
    git merge-base --is-ancestor $c origin/main; done
3da3dd9c  origin/main: not-ancestor   HEAD: ANCESTOR
747f926d  origin/main: not-ancestor   HEAD: ANCESTOR
4861a266  origin/main: not-ancestor   HEAD: ANCESTOR
73f79240  origin/main: not-ancestor   HEAD: ANCESTOR

$ git rev-list --left-right --count origin/main...HEAD   → 14   7
```

**네 후보 전부 `origin/main`의 조상이 아니다.** 즉 라이브 빌드 자체가 미병합 피처
브랜치에서 나갔다. 그러므로 4a를 closed 판정의 게이트로 쓰면 **이 사이클에 닫은 항목
전부가 실패한다** — 라이브에 들어간 커밋을 "배포 미도달"이라고 부르게 된다.

**그것은 내가 §3에서 부모의 테스트를 비판한 것과 같은 결함이다**(깨끗한 상태에서 빨강).
이 사이클 주제의 네 번째 인스턴스이고, 이번엔 내 것이다.

### 정정된 설계 — 두 술어는 다른 질문이므로 이름을 갈라야 한다

| 술어 | 묻는 것 | `origin/main` 기준 | 게이트 자격 |
|---|---|---|---|
| **P-merged** | 메인라인에 병합됐는가 | 조상 검사 | **게이트 아님. 진단만** — 오늘 전 항목이 실패한다 |
| **P-shipped** | 플레이어가 받았는가 | 배포 기록 기준 | **게이트.** 조건 5의 `(source <hash> on <branch>)`가 기준선을 공급한다 |

두 질문이 실제로 갈라지는 것을 실측으로 확인했다. 세 칸이 전부 채워진다 —
QA가 `register-reconciliation.md`에서 UX-001/002의 `shipped`로 인용한 `0cb0efb9`를
내 술어로 재검증한 것이 세 번째 사례를 공급했다:

| 커밋 | P-merged | P-shipped | 실제 |
|---|---|---|---|
| `0cb0efb9` (UX-001/002 수정, "one canvas for the HUD") | ancestor | ancestor (후보 4개 **전부**) | **병합됐고 배포됨** |
| 라이브 후보 4개 | not-ancestor | (그 자신이 라이브) | **배포됨** |
| `28226111` (W-9) | not-ancestor | not-ancestor (후보 4개 전부) | **진짜 미배포** |

가운데 행이 결정적이다 — P-merged에서 실패하면서 P-shipped에서 참이다. 하나의 술어로
두 질문에 답하면 **라이브 빌드를 미배포로 부른다.** 그리고 첫 행이 그 술어가 쓸모 있다는
것도 보여준다: 이 문서가 다루는 그 수정 자체가 양쪽 다 참이다.

### 구현 제약 — `main`이 아니라 `origin/main`이어야 한다. 이 저장소에서 그 차이가 결함을 만든다

QA가 "끝점 미표기가 `14 7`과 `14 5`를 낳았다"고 보고했다. 그 두 수는 재현하지 못했으나
**더 나쁜 것을 재현했다** — 끝점을 바꾸면 같은 저장소에서 답이 뒤집힌다:

```
$ git rev-list --left-right --count origin/main...HEAD   → 14   7
$ git rev-list --left-right --count main...HEAD          → 0    76
$ git rev-list --left-right --count origin/main...main   → 83   0
$ git log -1 --format="%ci %h" main          → 2026-08-12 20:15:25  6bfae546
$ git log -1 --format="%ci %h" origin/main   → 2026-08-14 23:18:49  873334c4
```

**로컬 `main`이 `origin/main`보다 83커밋·2일 낡았다.** 그래서 같은 커밋이 기준선에 따라
반대 답을 낸다:

```
$ git merge-base --is-ancestor 0cb0efb9 origin/main   → ANCESTOR
$ git merge-base --is-ancestor 0cb0efb9 main          → not-ancestor
```

`0cb0efb9`는 **이 문서가 다루는 UX-001/002 수정 자체**다. P-merged를 `main`으로 구현하면
병합됐고 배포된 수정을 "미병합"이라고 부른다 — **집행하려는 그 결함을 테스트 안에
심는 것**이다.

그래서 구현 제약 두 개를 술어에 붙인다:
1. 원격 추적 ref만 쓴다(`origin/main`). 로컬 브랜치명은 금지.
2. 기준선 ref와 그 해시·날짜를 **실패 메시지에 실어 출력한다.** 끝점을 적지 않은 수는
   재현 불가이고, QA가 자기 문서에서 같은 실수를 두 번 했다고 기록한 것이 그 증거다.

### 관측층 축 — QA/PM의 `regression-guard (관측층)`이 내 §2 후보 평가와 맞물린다

QA가 `regression-guard`를 `테스트이름 (관측층)` 형식으로 정하고 층을 `순수`/`씬`/`배포`로
뒀다(PM 제안, 근거 `709695ad`의 자백 *"values asserted, pixels never checked"*).
**이 축이 §2의 후보 평가와 같은 것을 다른 각도에서 말한다.**

내가 고른 후보 C(`:357`)의 관측층은 **씬**이다 — 런타임 계층 구조를 읽으므로 값이 아니라
구조를 본다. 그러나 **픽셀은 보지 않는다.** 캔버스 조상이 있으면서도 알파 0, 화면 밖,
또는 다른 요소에 완전히 가려진 라벨은 `:357`을 통과하고 플레이어에게는 여전히 안 보인다.
그 층은 `HudOverlapTests.cs`(겹침)와 `HudFixEvidenceCapture.cs`(캡처)가 나눠 갖고 있다.

그래서 UX-001/002의 `regression-guard`에 적어야 하는 정확한 값은
`EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll (씬)`이고,
**`(배포)`로 적으면 안 된다** — 그 테스트는 배포된 빌드를 보지 않는다.
§2의 후보 C 선택은 "결과를 잰다"는 것이었지만, 정확히는 **"씬 층의 결과"**다.
이 구분이 없으면 `709695ad`의 자백이 반복된다.

**그래서 조건 4의 의존 구조도 내가 준 것과 다르다**: P-shipped는 W-7의 *해시*가 아니라
**조건 5의 브랜치 기록**을 필요로 한다. 해시만으로는 그것이 어느 계보의 해시인지 나오지
않는다 — Director가 조건 5를 만든 이유가 정확히 그것이고, 그 이유가 4a에도 소급된다.

### 그런데 W-7 이전에도 P-shipped를 답할 수 있는 경우가 있다 — 후보 집합 전체에 물으면 된다

W-7이 미착지라 라이브 해시는 4개 후보로만 좁혀진다. 그러나 **후보 전체에서 답이 일치하면
해시 미확정이 결론을 막지 않는다**:

```
$ for c in 3da3dd9c 747f926d 4861a266 73f79240; do
    git merge-base --is-ancestor 28226111 $c; done
3da3dd9c 2026-08-14 23:39:02  contains-28226111: no
747f926d 2026-08-14 23:42:40  contains-28226111: no
4861a266 2026-08-14 23:44:02  contains-28226111: no
73f79240 2026-08-14 23:45:40  contains-28226111: no
```

**4개 전부 no.** `28226111`은 2026-08-18이고 후보는 전부 08-14다. 어느 후보가 진짜든
W-9는 미배포다. **전칭 논증이므로 W-7을 기다릴 필요가 없다.**

구현 형태: P-shipped를 단일 해시가 아니라 **후보 집합**에 대해 평가한다.
- 기록이 있으면(조건 5 이후) 집합의 크기는 1이고 답은 확정이다.
- 기록이 없으면 집합에 대해 평가하고, **전부 일치하면 그 값을 반환**하고
  **갈리면 `indeterminate`를 반환**한다 — pass도 fail도 아니다.

`indeterminate`를 pass로 접으면 증거 부재가 통과가 되고, fail로 접으면 오늘 전 항목이
막힌다. 계약이 *"Missing evidence path = FAIL"*로 부재를 이미 골랐지만, 그것은
**증거 경로 부재**에 대한 규정이고 여기는 **증거가 있으나 해상도가 부족한** 경우다.
세 번째 값이 필요한 자리이고, Director의 §3 판정(부재 기본값을 계약이 한 곳에만
적용했다)과 같은 구조다.

### 조건 5는 경고로 집행할 수 없다 — 무엇을 실패로 만들지 정해야 한다

Director는 조건 5를 "배포를 막지 않고 미기록을 막는다(경고 + 양방향 개수 출력)"로 적었다.
**NUnit에 게이트하는 경고는 없다.** `Assert`하지 않으면 그 테스트는 아무것도 막지 않고,
출력은 로그로 흘러간다 — 이 저장소가 이미 그 형태를 갖고 있다(`HudFontScaleDiagnosis.cs`가
`**미렌더**`를 표에 적지만 게이트를 막지 않는다, §0.1).

집행 가능한 형태는 Director의 의도를 그대로 옮긴 것이다 — **미기록을 실패로 만든다**:
- 배포 기록에 해시가 없으면 → **실패**
- 해시가 있으나 브랜치가 없으면 → **실패**
- 브랜치가 `main`이 아니면 → **실패 아님.** 개수(`rev-list --left-right --count`)를
  메시지에 실어 출력한다

off-main 자체는 위반이 아니라 데이터다. 위반은 **그것을 적지 않은 것**이다. 이 구분이
없으면 조건 5가 배포 정책을 강제하게 되고, Director는 그것을 원하지 않는다고 명시했다.

**핵심 설계 제약**: 항목 목록을 테스트에 적으면 **후보 B가 되고 E-1의 네 번째
인스턴스가 된다.** 그래서 표를 파싱해 **행을 발견**하고, 행마다 열 존재를 검사한다.
"UX-001..UX-016"을 적는 순간 17번째 결함이 보이지 않는다.

이 레인은 설계까지만 낸다. 구현 소유권은 부모가 배정한다 — `qa/` 문서는 QA 레인이,
테스트 파일은 부모가 편집 중인 파일과 별개 파일이어야 한다(§7 충돌 회피).

### 후속 — F-2가 구현됐다. 검수 결과: 설계대로, 잠재 결함 2건 + 관찰 1건 → **전부 수정됨** [OBSERVED 2026-08-18]

`Assets/Tests/EditMode/DefectRegisterGateTests.cs`(신규, 320줄)가 조건 1·2·3을 구현했다.
**설계 의도가 지켜졌다:**

| 설계 요구 | 구현 |
|---|---|
| 항목 목록 선언 금지, 행을 발견 | `DiscoverSeverityTables()`가 `Directory.GetFiles(..., AllDirectories)`로 파일을 발견하고, 표는 **셀에 S1/S2/S3가 있으면** 자격을 얻는다. `UX-001..016` 목록 없음 |
| 표본 하한(내 권고가 만든 구멍의 짝) | **두 테스트에 다 있다** — `Assert.That(tables, Is.Not.Empty, "an empty walk asserts nothing")`, `Assert.That(registers, Is.Not.Empty, "this test asserted nothing")` |
| 감사 뷰 오탐 회피 | `IsRegisterFile()`이 이름으로 대장만 고른다. docstring이 그 이유를 적었다 — 감사의 한 줄 판정 표(`결함 \| 등급 \| 한 줄 판정`)는 status 열이 없는 게 옳고, 요구하면 **정상 문서 5건에서 빨강**이 된다 |

현재 상태: 역할 선언 5파일 전부 존재, `gate-reviews/`에 `PASS` 0건(UX-014가 open이므로
정확한 동작) → 4개 테스트 전부 녹색으로 보인다.

**결함 A (잠재) — 이 사이클이 방금 고친 그 모양이 재현됐다.**

```csharp
if (!Directory.Exists(reviewRoot))
{
    Assert.That(openS1, Is.Not.Null);   // nothing to contradict yet
    return;
}
```

`openS1`은 `.ToList()` 결과이므로 **null이 될 수 없다. 이 단언은 항상 참이다.** 따라서
`gate-reviews/`가 없거나 이름이 바뀌면 **계약의 중심 문장("any open S1 blocks every gate")이
무단언으로 통과**한다. 지금은 디렉터리가 있어서 살아 있지만, 이번 사이클에 파일·디렉터리가
여섯 번 움직였다.

이것은 `HudFontScaleDiagnosis.cs`의 모양(기록하되 게이트하지 않음)이고, **`Assert.Greater(
checkedCount, 0)`이 막으려고 추가된 바로 그 구멍**이다. 같은 사이클에 한 테스트에서 고치고
다른 테스트에서 재현됐다 — **표본 하한을 두 곳에 넣은 저자가 세 번째 자리를 놓쳤다.**
처방도 같다: 부재를 실패로 만들거나(`Assert.That(Directory.Exists(...), Is.True, ...)`),
최소한 `Assert.Ignore`로 **무단언임을 드러낸다.** 조용한 녹색이 가장 나쁘다.

**결함 B (잠재) — 예외 블랙리스트가 선언된 목록이다.**

```csharp
Regex.IsMatch(lines[i], @"\bPASS\b") && !Regex.IsMatch(lines[i], @"PASS / FIX|FIX / |가능|후보|불가")
```

**후보 B / E-1 계층 4다.** `PASS 조건`, `PASS 기준`, `PASS 아님`, 제목 `## G4 PASS`가 전부
"PASS 주장"으로 걸린다. 지금 `gate-reviews/`에 `PASS`가 0건이라 무해하지만, **게이트 리뷰를
쓰는 순간부터 이 블랙리스트가 판정을 지배**한다. 구조로 바꾸면 목록이 필요 없다 — 판정 줄을
`verdict: PASS|FIX|BLOCKED` 같은 **키 형식**으로 요구하고 그 키만 읽는 것. **산문에서 단어를
찾는 대신 구조를 읽는다**가 §2 후보 A를 기각한 것과 같은 이유다.

**관찰 — 검출 정밀도가 셀 서식에 의존한다.** `ux-defect-list.md:206-209`의 롤업 표
(`심각도 | 건수 | ID`)는 status 열이 없는데 **검출을 피한다.** 이유가 설계가 아니라 우연이다:
셀이 `S1 (치명)`이라 `SeverityCell`의 `^\**\s*(S[123])\s*\**$` 앵커에 걸리지 않는다.
누가 그 표를 `| S1 | 4 | … |`로 정리하면 **롤업에 status 열을 요구하며 빨강이 된다.**
롤업이 면제되는 것은 옳지만, 면제의 근거가 서식이면 서식을 고치는 사람이 게이트를 깨뜨린다.
(부수: `:207` 구분선이 4칸인데 헤더가 3칸이다.)

**후속 — 세 건 전부 고쳐졌다. 확인했다.**

| 내 보고 | 부모의 수정 | 위치 |
|---|---|---|
| 결함 A: `Assert.That(openS1, Is.Not.Null)`가 항상 참 | `Assert.That(Directory.Exists(reviewRoot), Is.True, …)` — 부재를 **실패**로. 메시지가 "would pass while asserting nothing"까지 적었다 | `:298-301` |
| 결함 B: PASS 예외 블랙리스트 | `verdict:` **키를 구조로 읽는다** — `^\s*[-*]?\s*verdict:\s*\**\s*([A-Za-z]+)`. 블랙리스트 제거, `PASS 조건`·`## G4 PASS` 오독 해소 | `:308` |
| 관찰: 롤업 면제가 서식 의존 | **내 제안보다 낫다.** 정규식 앵커를 열고(`^\**\s*(S[123])\b`, `:47`) 롤업을 **구조로** 면제한다 — `RollupCount`(`:54`)가 "심각도 + 맨 숫자" 모양을 인식하고 `:189`가 그 행을 건너뛴다 | `:47`, `:54`, `:189` |

세 번째가 중요하다. 나는 **서식 의존을 관찰로만 보고**했고 면제를 어떻게 정의할지는 적지
않았다. 부모가 그것을 **구조 판정**으로 올렸다 — 롤업은 셀 장식이 아니라 **모양**(심각도 뒤에
맨 숫자) 때문에 면제된다. `:37-41`의 주석이 이유를 적었고 내 문장이 거기 인용됐다:
*"Detection that depends on formatting is detection that moves when someone reformats."*

**세 건 모두 "집행 코드가 자기 계약을 위반한 자리"였다.** 무단언 통과, 선언된 목록,
서식 의존 — 이 문서가 §0·§2·§8에 적은 실패 모양 그대로이고, F-2는 그 실패들을 막으려고
쓴 테스트다. 문서와 코드가 같은 사이클에 있어야 하는 이유가 그것이다.

**그리고 QA가 자기 스키마의 축 하나를 새로 지목했다**: 열 집합(무엇을 요구하는가)을
설계하면서 **면제 조건(무엇을 요구하지 않는가)을 구조로 정의하지 않았고**, 그 공백을 구현이
서식으로 메웠다. → **규칙을 쓸 때 적용 대상의 경계도 규칙이다.** §8의 "방금 만든 방어가
무엇을 새로 열었는가"의 또 다른 얼굴이다.

조건 4·5·6은 미구현이고 예상대로다(W-7·브랜치 기록 선행). 구현 시 **조건 6을 반드시 함께**
넣어야 한다 — 로컬 `main`이 83커밋 낡아 `0cb0efb9`(UX-001/002 수정 자체)를 미병합으로 부른다.

### 후속 2 — F-2의 실행 증거. 그리고 내가 뮤테이션 창 중간을 읽었다 [OBSERVED 2026-08-18 16:34]

QA가 닫으며 남긴 마지막 항목이 *"F-2도 통과 XML이 없다 — 집행 코드에도 실행 증거가
필요하다"*였다. **있었다.** 프로젝트 루트에 XML 4건이 있었고(에디터가 락을 잡고 있어 배치
실행은 하지 않았다 — 읽기만 했다) 조건별 뮤테이션 증명이 전부 남아 있었다:

| XML | 시각 | 뮤테이션 | 결과 |
|---|---|---|---|
| `g4.xml` | 16:32:03 | 상태 열 제거 | `EverySeverityTable_CanExpressStatus` **Failed** — 표 3개·15행·ID를 이름으로 지목 |
| `g5.xml` | 16:33:19 | 없음(원복) | **4/4 Passed**, 스위트 `result="Passed"`, `testcasecount="324"` ← **기준선 증거** |
| `m1.xml` | 16:33:55 | `zz-probe.md`에 `verdict: PASS` | `WhileAnyS1IsOpen_NoGateReviewClaimsPass` **Failed** — `UX-014` open + probe 인용 |
| `m2.xml` | 16:34:12 | 위 둘 동시 | 둘 다 **Failed** |

> **⚠ 이 네 경로는 존재하지 않는다** — 부모가 정리 단계에서 삭제했다. 위 표의 값은 파일이
> 존재하던 동안 내가 추출한 것이고, 그 시점에 이 표는 **주장이었고 증거가 아니었다**
> (계약의 *"Missing evidence path = FAIL"*을 내 문서에 적용한 결과).
>
> **해소됐다** [OBSERVED 2026-08-18 16:48]. 내가 요청하고 부모가 **조건 1의 뮤테이션 쌍을
> 영구 보존**했다. 내가 확인한 것:
>
> | 보존 경로 | `EverySeverityTable_CanExpressStatus` | 시각 |
> |---|---|---|
> | `qa/evidence/registers/cond1-status-column-removed-RED.xml` | **`result="Failed"`** | 07:48:11Z |
> | `qa/evidence/registers/cond1-baseline-GREEN.xml` | **`result="Passed"`** | 07:48:28Z |
>
> **같은 테스트, 반대 결과, 17초 차.** 이것이 뮤테이션 증명의 완성형이다 — 실패본만 남기면
> "빨강일 수 있다"만 증명되고 **"올바른 상태에서 녹색이다"**가 빠지는데, 그것이 §3에서 부모
> 테스트에 지적한 바로 그 구멍이다. 쌍이어야 증명이다.
>
> 파일명이 내가 요청한 형태다(`cond1-…-RED`/`cond1-baseline-GREEN`) — **경로가 아니라 내용으로
> 인용된다.** 부모의 메시지는 다른 이름(`gate-mutation-red-status-column.xml` 등)을 적었으나
> 디스크의 실제 이름은 위와 같다. **보고와 디스크가 어긋날 때 디스크가 권위다** — 이 회의가
> 여섯 번 확인한 것이고, 내가 메시지의 이름으로 grep해서 빈 결과를 받고 다시 확인했다.
>
> 전체 스위트: `editmode-baseline-green.xml`(552/553), 그리고 앞선 `editmode-553.xml`.
> `blocking-predicate-proof.md`는 조건 2(probe)를 기록한다 — 두 조건의 이빨이 이제 각각
> 디스크에 있다.

**세 조건이 각각 독립적으로 이빨을 증명했다.** 조건별로 하나씩 죽였고 원복본이 녹색이다 —
§6이 요구한 증거의 정확한 형태이고, 조건 1의 실패 메시지가 표·행·ID를 이름 부르므로
**실패가 재현 가능**하다. **위 쌍의 보존으로 이 판정은 주장에서 증거로 돌아왔다.**

**그리고 그 직후 내가 상태 열 부재를 관측해 "원복 누락"으로 긴급 보고했다 — 진단이 틀렸다(아래 정정).**

```
$ sed -n '72p' _workspace/current/qa/ux-defect-list.md
| ID | 심각도 | 증상 | 근거 | 제안 |          ← 상태 열 없음
$ git diff --stat -- _workspace/current/qa/ux-defect-list.md
1 file changed, 26 insertions(+), 26 deletions(-)
```

**정정 (QA 반증, 내가 재확인) — 내가 "HEAD에 상태 열이 있다"고 적은 것은 틀렸다.
`상태` 열은 HEAD가 아니라 index에 있다**(부모의 스테이지된 미커밋 작업):

```
# 도구: grep 도구 / 범위: git show로 추출한 각 상태 / 패턴: ^\| ID \| 심각도 \| 상태 \|
HEAD  (228줄) → No matches found      ← 상태 열 없음
index (232줄) → :72 :110 :124  3건    ← 여기 있다
git diff --cached --stat → +27 -23     ← HEAD ≠ index
```

**내가 준 명령은 옳았고 근거가 틀렸다.** `git checkout -- <file>`은 **index에서** 복원하므로
결과가 정확히 맞았다. 그러나 누가 내 근거("HEAD에 있다")를 읽고 명령을
`git checkout HEAD -- <file>`로 **"고쳤다면" 부모의 스테이지된 스키마 작업이 파괴됐다.**

> **근거가 틀린 옳은 명령은, 다음 사람이 근거를 따라 명령을 바꿀 때 위험해진다.**
> "값이 아니라 재생성 명령을 남겨라"에서 한 걸음 더 간 자리다 — **명령과 근거가 어긋나면
> 명령이 옳아도 안전하지 않다.**

그리고 이것이 축을 하나 더 만든다: **git의 상태는 둘이 아니라 셋이다** — HEAD · index ·
작업 트리. 내 §인용 기준선은 `GameManager.cs`를 "HEAD 기준"으로만 선언했고 **index를
구분하지 않았다.** `baseline` 칸에 커밋 해시만 적으면 **스테이지된 미커밋 작업이 판정에서
사라진다**(QA §0.5의 세 번째 축). `상태` 열이 정확히 그 자리에 있었다.

원복은 `git checkout -- _workspace/current/qa/ux-defect-list.md`(index에서 복원)이고
QA 소유 파일이라 편집하지 않고 QA·부모 양쪽에 보고했다.

**정정 — 내 진단이 틀렸다. 원복 누락이 아니라 뮤테이션 창 중간 읽기였다** (부모 설명, 근거
대조로 확인). 그 실행은 **617초**였고 두 뮤테이션이 그 안에서 순차로 돌았다. 부모는 뮤테이션과
원복을 **한 셸 호출 안에서 원자적으로** 처리했고(`cp /tmp/ux.bak` 후 `shasum` 2파일 1해시
`2b0a1fa8...`), 내가 그 창 안에서 디스크를 읽었다. **두 관측이 둘 다 참이고 시각이 다르다** —
내가 본 상태 열 부재는 그 순간 실재했고, 원복도 실재했다.

그러므로 "원복이 한 곳만 됐다"는 내 서술은 **거짓**이다. 지워야 할 것은 두 곳이었고 부모는
두 곳을 지웠다. 내가 그 사이를 봤다.

**교훈의 소재도 나에게서 부모에게로 옮겨간다** — 부모의 정리: *"뮤테이션을 원자적으로
만들었지만 그 창 동안 다른 레인이 디스크를 읽을 수 있다는 것을 고려하지 않았다. 공유
작업 트리에서 뮤테이션은 배타적 작업이고 시작·종료를 브로드캐스트해야 했다."*

> **원자성은 쓰는 쪽의 성질이고, 읽는 쪽에는 창이 보인다.** 공유 작업 트리에서 원자적
> 뮤테이션도 관측자에게는 결함으로 보이는 구간을 만든다.

이것이 E-3(무효화의 시간 상수)의 세 번째 사례이고, 이번엔 **분이 아니라 초 단위**이며
무효화 주체가 **관측 대상 자신**이다. 같은 대역에서 QA도 줄번호를 1줄 틀렸고, Director는
부모의 `Adopt` 뮤테이션을 S1 회귀로 의심했다. **세 레인이 같은 창에서 같은 착오를 했다** —
개인의 부주의가 아니라 **공유 트리에 뮤테이션 프로토콜이 없었다는 구조 문제**다.

**그리고 그 처방이 즉시 적용됐다 — 프로토콜의 첫 사용을 기록한다** [OBSERVED 2026-08-18].
부모가 다음 뮤테이션을 **창 시작 브로드캐스트**로 열었다: *"뮤테이션 창 시작 —
`ux-defect-list.md`의 상태 열을 제거합니다. 약 2~3분. 그 사이 이 파일을 읽고 결함으로
판정하지 마십시오. 제가 종료를 브로드캐스트합니다."*

**한 줄이 내 추론 전체를 불필요하게 만든다.** 지난 창에서 나는 락파일과 2분 내 XML 4건이라는
신호를 **이미 보고서도** 결함 보고로 읽었다. 이번에는 추론할 것이 없다 — **무엇을 하지 않아야
하는지가 명시**돼 있다. 이것이 §8의 "방어가 무엇을 새로 열었는가"의 반대 방향 사례다:
**정보를 추론하게 만드는 대신 선언하면 추론 오류의 표면이 사라진다.**

목적도 이 문서의 요청이었다 — 부모가 정리 단계에서 `g4`/`g5`를 지워 **조건 1의 이빨 증거가
사라졌고**, 그것을 `qa/evidence/registers/`에 영구 보존하기 위한 뮤테이션이다. 보존 시
두 가지를 요청했다: (1) **실패본과 원복본을 쌍으로** — 실패만 남기면 "빨강일 수 있다"만
증명되고 "올바른 상태에서 녹색이다"가 빠지는데, 그것이 §3에서 부모 테스트에 지적한 바로 그
구멍이다. (2) **파일명에 조건을 넣어** — `g4`/`g5`는 이 세션 밖에서 의미가 없고,
`cond1-status-column-removed.xml` 형태면 **경로가 아니라 내용으로** 인용된다. 이름이 이동을
견디고 줄번호가 못 견딘다는 이 회의의 결론의 **파일 판**이다.

### 후속 3 — 원복 확인. F-2는 아카이브된 통과 증거를 갖는다 [OBSERVED 2026-08-18]

보고 후 원복됐고 확인했다:

```
$ sed -n '72p' _workspace/current/qa/ux-defect-list.md
| ID | 심각도 | 상태 | 증상 | 근거 | 제안 |          ← 복원
$ git diff --stat -- _workspace/current/qa/ux-defect-list.md
(빈 출력 = **index와** 바이트 동일. HEAD가 아니다 — `git diff`는 작업 트리 대 index다)
```

표 3개 전부(`:72` `:110` `:124`) 헤더와 값이 살아 있다(`closed 2026-08-14`, `open`,
`**open**`). probe도 부재. **`git diff`가 비었다는 것이 권위 있는 원복 증거**이고, 이것이
부모가 `Adopt(windText)`에서 쓴 것과 같은 판정 기준이다.

그리고 **아카이브된 통과 증거가 생겼다**:
`qa/evidence/registers/editmode-553.xml` — `DefectRegisterGateTests` `result="Passed"`,
4/4, **553 테스트 전체 실행** 중. 내가 §6에서 요구한 것보다 강하다(나는 필터 실행을
상정했다). QA가 닫으며 남긴 *"F-2도 통과 XML이 필요하다"*가 이것으로 닫힌다.

**부수 발견 — bash grep이 네 번째로 물었고 이번엔 내가 먼저 잡았다.**
원복을 확인하며 `grep -c "^| ID | 심각도 | 상태 |"`를 bash로 돌렸더니 **232**가 나왔다.
헤더 3개인 파일에서 불가능한 수다. grep 도구로 다시 세니 **3건**(`:72` `:110` `:124`)이다.
W-14의 네 번째 사례이고, **차이는 내가 이번엔 그 수를 인용하지 않았다는 것**이다 —
232이 그럴듯하지 않았기 때문이다. §8이 적은 *"위양성은 풍성함으로 신호를 지운다"*의 역:
**불가능하게 큰 수는 신호를 지우지 못한다.** 10은 그럴듯했고 232는 아니었다.

---

## 7. 산출 요약

| 과제 항목 | 답 |
|---|---|
| 지역 (EditMode vs PlayMode) | **PlayMode 유지.** 이유는 싱글톤이 아니라 산출을 `Start()`가 만들고 EditMode에 `Start()`가 없기 때문. 단 `BootMatch` 3초 → `LoadSceneAsync`+프레임 2회로 교체 (같은 파일 `:172-176`에 관용구 존재) |
| 계약 형태 3후보 | A 소스 문자열(씬·프리팹·코드생성 수정 전부 막음, 기각) / B 선언 목록(5번째 라벨을 놓침, 기각) / **C 발견된 집합 + 결과 단언(선택)**. 부모의 `:357`이 이미 C |
| `Adopt(null)` 구멍 | 잡을 수 있다 — `:357`(오브젝트 잔존 시) + 빈 필드 **진단**(`:399` 리플렉션). 씬 YAML 읽기는 불필요하고 씬 수정 경로를 막으므로 금지 |
| 사이드 구조 결함 | `#if UNITY_EDITOR` **지시자 8건 / 6파일** 중 **확인 0건.** QA의 5건, 내 중간값 2건, 내 첫 판의 "10건/7파일" 모두 무효 |
| 검수 결과 | `:357` 승인. `:399`는 오탐 2건(`gimmickStatusText`, `resultText`)으로 **깨끗한 저장소에서도 빨강이었고**, 부모가 세 권고를 전부 채택해 고쳤다(`:467` 활성 게이트, `:461`/`:486` 진단 분리, `:52 BootScene`) + 내가 권고하지 않은 `:490` 공허통과 방지까지. **기준선 0 복귀** |
| `:322` 존치 | 남긴다. 단 `:357`을 가리키는 한 줄이 없으면 다음 읽는 사람이 구멍으로 읽는다(내가 그랬다) |
| `Image` 확대 | 하지 않는다. `GameManager`에 `Image` 필드 0건, `SelectedUnitPortrait`는 코드 생성(`LaunchManager.cs:386`) |
| F-2 | EditMode + 디스크 발견(`Directory.GetFiles`) **술어 6개**. 항목 목록 선언 금지. 조건 4는 **P-merged(진단 전용)** 와 **P-shipped(게이트)** 로 분리 — 내 첫 4a 설계는 **철회**했다(라이브 후보 4개 전부 main 비조상이라 게이트로 쓰면 전 항목 실패). P-shipped는 후보 집합에 평가하고 갈리면 `indeterminate`. 조건 6(원격 추적 ref)은 내 구현 제약이 독립 조건으로 승격된 것 |

## 8. 이 레인이 틀렸을 수 있는 곳

- **`Graphic.canvas`의 비활성 조상 의미론을 측정하지 않았다.** §3의 논증은 두 분기가
  모두 실패한다는 형태로 이 미지를 우회한다. 우회가 성립하지 않는 경우가 있다면
  §3 오탐 2가 오탐 1개로 줄어든다 — `gimmickStatusText`는 씬 fileID 0으로 확정이므로
  **"깨끗한 저장소에서 빨강"이라는 결론은 바뀌지 않는다.**
- **`BootMatch` → 프레임 2회 교체를 실행하지 않았다.** `Start()` 완료가 프레임 2회로
  보장된다는 근거는 같은 파일 `:172-176`의 주석과 그 테스트들의 통과 이력이고,
  두 새 테스트에 대해 직접 돌려본 것이 아니다. 부모가 교체할 때 측정이 필요하다.
- **테스트를 실행하지 않았다.** 지역 판정·형태 선택·오탐 2건은 코드·씬·프리팹 읽기와
  grep으로만 도출했다. `problems`가 원복 후 정확히 2인지는 부모의 실행이 답한다.
  이 문서는 그 실행의 **판정 기준**(원소 이름, 개수 아님)을 지정하는 것까지다.
- **4b는 pages 저장소를 직접 조회하지 않고 설계했다.** "두 히스토리가 분리돼 있다"의 근거는
  원격 URL이 다른 저장소라는 것과 배포 절차가 rsync라는 문서
  (`ops/deploy-blocked-pages-credentials.md:65-76`)이고, pages 히스토리를 clone해서 확인한
  것이 아니다. P-merged 쪽은 다르다 — 로컬에서 실행해 W-9와 라이브 후보 4개를 실측했다.
- **내 4a 설계가 틀렸고 Director가 그것을 채택한 뒤에 반증됐다.** 원인은 내가 술어를
  하나 만들어 두 질문("병합됐는가"·"배포됐는가")에 쓰려 한 것이고, 반증한 것은 내가
  Director의 조건 5 수치를 **인용하지 않고 재실행**했기 때문이다(후보 4개 전수 검사).
  이 문서가 §0에서 인테이크에 대해 지적한 것과 같은 실패를 내가 §6에서 했다 —
  **한 번 검증한 술어를 다른 질문에 재사용하면 검증은 이전되지 않는다.**
- **`indeterminate` 3값 설계를 구현·실행하지 않았다.** 전칭 논증이 성립하는 경우
  (W-9, 후보 4개 전부 no)는 실측했으나, 후보가 갈리는 실제 사례를 만나본 것이 아니다.
  갈리는 경우의 처리는 설계이고 측정이 아니다.
- **bash `grep`이 이 저장소에서 조용히 빈 결과를 준다 — 그리고 내 수 하나가 그것 때문에
  틀렸다.** 세션 중 bash `grep`이 존재하는 대상에 대해 두 번 빈 출력을 냈고
  (`LayoutSelectionRow`, `AddComponent<UnitController>`), 나는 그때마다 grep 도구로
  바꿔 답을 얻고 **원인을 조사하지 않았다.** QA가 같은 함정에 빠진 것을 보고하고서야
  전면 재확인했다. 재확인 결과 `Image` 필드 0건과 `SelectedUnitPortrait` 코드 생성은
  grep 도구로도 동일했으나, **`#if UNITY_EDITOR` 수는 8/6이지 10/7이 아니었다**(§5 정정).
  `0건`은 측정값이 아니라 주장이다 — 어느 도구로 셌는지가 그 주장의 일부다.
- **UX-018 경로 수를 2로 적었고 3이 맞았다**(§5 정정). 놓친 것은 `SimpleAI.cs:114`이고,
  놓친 형태가 이 문서의 주제 그 자체다: 나는 "명시적 `yield break`가 있는 줄"이라는
  **목록을 만들어 그것을 돌았고**, 경로 4에는 `yield break`가 없다. 정상 낙하 경로가
  샷을 열지 않고도 `Seal()`에 도달할 수 있는지를 확인하지 않았다.
- **UX-018의 3경로를 실행으로 확인하지 않았다.** `unitPrefabs`를 비우거나 `UnitController`
  없는 프리팹을 물려 실제로 파란색이 뜨는 것을 본 것이 아니고, 제어 흐름 읽기로 도출했다.
- **줄 번호 기준선을 한 파일에만 방어했다.** 헤더에 `GameManager.cs`의 HEAD 기준선을
  명시해 Director의 오판을 막았는데, **실제로 다섯 번 움직인 파일은
  `HudCanvasContractTests.cs`였고 그쪽은 방어하지 않았다.** 내가 만든 방어를 정작
  움직이는 대상에 적용하지 않은 것이다.
  일반형: **어느 좌표계를 쓰는지 밝히는 것으로 끝나지 않는다. 그 좌표계가 흔들리는
  대상에 실제로 적용됐는지 확인해야 한다.**

  **처방 정정 (QA 관찰로 내 첫 결론을 철회한다)**: 나는 "QA가 줄 번호를 아예 지운 것이
  더 나은 답이었다"고 적었다. **양보가 과했다.** QA가 지적했다 — 이름만으로 인용하면
  **검수 시점의 상태를 재현할 수 없다.** 내 `:322`가 무엇을 가리켰는지 알려면 그 시점의
  줄 번호가 필요하고, 대응표가 그것을 준다. 반대로 줄 번호만 있으면 다섯 번의 이동을
  못 견딘다.

  정답은 **이름 + 스냅숏 대응표**이고, 둘 중 하나만으로는 불완전하다 — 이름은 이동을
  견디고 대응표는 과거를 재현한다. **§6의 "전수 열거 + 표본 하한"과 같은 세트 구조다**:
  한쪽만 있으면 다른 쪽 실패 양상이 그대로 남는다. 이 문서에서 그 구조가 세 번 나왔다
  (전수+하한, 이름+대응표, 그리고 카운트의 도구+범위+패턴).

- **내 카운트 오류의 축은 "패턴"이었다.** QA가 세 레인의 오류를 축으로 갈랐고 원인이 셋 다
  달랐다 — QA는 **도구**(bash 위음성, 0건→5파일), 나는 **패턴**(`#` 누락, 10→8),
  Director는 **범위**(`Assets/Scripts/` vs `Assets/`, 1→3). 셋 다 결론은 유지됐으므로
  실패의 정체는 "결론이 틀렸다"가 아니라 **"재현 불가능한 주장이었다"**다. 그래서 카운트를
  적을 때 **도구·범위·패턴 세 항을 함께** 적어야 하고, 하나라도 빠지면 남이 재현할 때
  다른 수가 나오면서 **어느 축의 오류인지 판별할 수 없다.**
- **내 대응표가 그 자체로 낡았다 — 방어가 자기 실패 양상을 갖는 네 번째 사례.**
  나는 줄 번호가 흔들린다는 것을 잡아 이름↔줄 대응표를 붙였는데, **표에 적은 값도
  똑같이 흔들린다.** 파일이 여섯 번 움직이는 동안 표를 세 번 고쳤고, **여섯 번째는
  QA가 먼저 발견했다** — 즉 내 방어는 자기가 낡는 축을 막지 않았다. 이것은 §8이 이미
  기록한 명제가 내 방어 자체에 적용된 것이다: **부분적 방어는 자기가 막지 않는 축의
  실패를 그대로 남긴다.**
  처방: 표에 **값만 남기지 말고 재생성 명령을 함께 남긴다**(헤더에 반영).
  **표는 과거를 재현하고 명령은 현재를 생성한다** — 세트다. 손으로 표를 쫓는 것은
  움직이는 파일에 대해 지는 싸움이고, 세 번 고친 것이 그 증거다.
- **대응표에 범위를 적지 않았다 — 다섯 번째 사례, 그리고 우리가 방금 만든 규칙을 자기 표에
  적용하지 않은 것이다.** QA의 재생성 명령이 테스트 **7개**를 내는데 내 표에는 5개뿐이었다.
  확인하니 누락이 아니라 **범위**였다(두 런타임 빌더 테스트와
  `HudSetup_DoesNotRewriteAnotherSystemsCanvas`는 이 문서가 줄 번호로 인용하지 않는다).
  그러나 **범위를 적지 않으면 7과 5의 차이가 낡음으로 읽힌다.** 카운트에 도구·범위·패턴을
  붙이라는 규칙을 만든 문서가 자기 대응표에는 붙이지 않았다.
- **QA의 자기참조 정식화가 내 것보다 한 겹 깊고, 그것이 이 §8의 최종형이다.** 나는
  "부분적 방어는 막지 않는 축을 남긴다"까지 갔는데 QA가 **"방어를 설계하는 행위가 새 축을
  만든다"**로 밀었다. 세 사례가 전부 그 형태다 — 나는 줄 흔들림을 막는 표를 만들어
  **표의 낡음**이라는 축을 만들었고, QA는 카운트 축 하나를 적어 **나머지 두 축**을 남겼고,
  부모는 오탐 필터를 넣어 **빈 표본**이라는 축을 만들었다. 셋 다 **방어가 없었으면
  존재하지 않던 축**이다.
  그래서 물어야 하는 것이 하나 더 있다: "무엇을 막지 않는가"에서 끝나지 않고
  **"내가 방금 만든 방어가 무엇을 새로 열었는가"**까지다.
- **내 §8 정식화의 빠진 절반을 QA가 채웠다.** 나는 *"방금 만든 방어가 무엇을 새로
  열었는가"*까지 갔는데, QA가 자기 스키마에서 다른 절반을 지목했다 —
  **규칙을 쓸 때 적용 대상의 경계도 규칙이다.** QA는 열 집합(무엇을 요구하는가)을 정하고
  **면제 조건(무엇을 요구하지 않는가)을 구조로 정의하지 않았고**, 그 공백을 구현이
  **서식으로** 메웠다(`S1 (치명)`이 앵커에 안 걸려 롤업이 우연히 면제된 것).
  둘이 세트다: 전자는 새 축을 묻고, 후자는 **정하지 않은 것이 사라지지 않고 한 층 아래에서
  결정된다**는 것을 말한다. **규칙에 경계가 없으면 경계는 없어지는 게 아니라 정규식이 된다.**
- **내가 문제를 지목했고 해법의 층위는 부모가 찾았다.** 롤업 건에서 나는 "서식 의존"을
  **관찰로만** 보고하고 면제를 어떻게 정의할지 적지 않았다. 부모가 그것을 구조 판정으로
  올렸다(`RollupCount` — 심각도 뒤 맨 숫자라는 **모양**). 결함을 찾는 것과 그 결함이
  **어느 층에서 고쳐져야 하는지** 말하는 것은 다른 작업이고, 검수 보고는 후자까지 가야
  완결이다. 이 문서의 §2(후보 A/B/C를 "무엇을 방해하는가"로 평가)가 그 형태였는데,
  롤업 관찰에는 같은 규율을 적용하지 않았다.
- **"HEAD에 상태 열이 있다"고 적었고 틀렸다 — index였다.** QA가 반증하고 내가 재확인했다
  (HEAD 228줄에 헤더 0건, index 232줄에 3건, `git diff --cached` +27 -23).
  **내가 준 명령은 옳았다** — `git checkout -- <file>`은 index에서 복원하므로 결과가 맞았다.
  **근거만 틀렸고, 그것이 더 위험한 종류다**: 누가 내 근거를 읽고 명령을
  `git checkout HEAD -- <file>`로 "고쳤다면" 부모의 스테이지된 스키마 작업이 파괴됐다.
  > **근거가 틀린 옳은 명령은, 다음 사람이 근거를 따라 명령을 바꿀 때 위험해진다.**
  이것은 §8의 "재생성 명령을 남겨라"에서 한 걸음 더 간 자리다 — 명령을 남기는 것으로
  끝나지 않고 **명령과 근거가 같은 것을 가리켜야 한다.**
  파생: 내 §인용 기준선은 `GameManager.cs`를 "HEAD 기준"으로 선언하고 **index를 구분하지
  않았다.** git의 상태는 둘이 아니라 **셋**(HEAD · index · 작업 트리)이고, 커밋 해시만
  적으면 **스테이지된 미커밋 작업이 판정에서 사라진다.**
- **bash `grep`이 다섯 번째로 물었고, 이번엔 나를 오답으로 끌 뻔했다.** 세 상태를 확인하려
  `git show <ref>:<path> | grep -c "심각도 | 상태"`를 돌렸더니 **HEAD·index·작업 트리가 전부
  16**이었다 — 전부 틀린 값이고(정답 0/3/3), **그 값을 믿었다면 QA의 정확한 반증을 기각**했다.
  grep 도구로 파일을 직접 읽어야 답이 나왔다(`git show`로 `/tmp`에 추출 후 조회).
  §8이 적은 위양성 규칙의 세 번째 얼굴이다: **10은 그럴듯해서 통과했고, 232는 불가능해서
  걸렸고, 16은 그럴듯한데 세 상태가 같다는 점이 유일한 신호였다.** 같은 값이 세 번 나오는
  것 자체가 의심할 근거다 — 서로 다른 세 상태가 같은 수를 낼 이유가 없다.
- **"뮤테이션이 원복되지 않았다"고 긴급 보고했고 틀렸다 — 원자적 뮤테이션 창 중간을
  읽었다.** 부모의 실행은 617초였고 뮤테이션·원복이 한 셸 호출 안에 있었다(`shasum` 2파일
  1해시로 원복 확인). 내가 본 상태는 **그 순간 실재했고** 원복도 실재했다 — 두 관측이 둘 다
  참이고 시각이 다르다. 내 오류는 관측이 아니라 **진단**이다: 스냅숏 하나로 "원복 누락"이라는
  **완료 상태**를 단정했고, 진행 중인 절차와 미완 절차를 구별할 근거가 그 스냅숏에는 없었다.
  방어였다면 물어야 했던 것: **"이 파일을 지금 누가 쓰고 있는가"** — `Temp/UnityLockfile`이
  있고 XML 4건이 2분 내에 쓰였다는 것을 나는 이미 봤고, **그 두 사실이 '뮤테이션 진행 중'을
  말하고 있었는데 결함 보고 쪽으로 읽었다.**
  Director가 같은 실수를 `Adopt` 뮤테이션에서 했고 파생 규칙을 만들었다 —
  *"grep 결과가 직전 관측과 다르면 결함 선언 전에 `git diff`로 누구 작업인지 판정한다."*
  나는 그 규칙을 **읽었고, 인용했고, 다른 파일에 적용하지 않았다.**
- **내가 인용한 증거 경로 4건이 삭제됐다 — 내 문서에 계약을 적용하면 그 표는 증거가 아니라
  주장이다.** `g4/g5/m1/m2.xml`은 부모의 정리로 사라졌고, 값은 내가 존재하던 동안 추출한
  것만 남았다. 계약의 *"Missing evidence path = FAIL"*이 여기에 그대로 걸린다 — 이 문서가
  §0에서 인테이크에 요구한 것과 같은 기준이다. 경고문으로 지위를 명시했고(§후속 2),
  살아 있는 것은 `qa/evidence/registers/editmode-553.xml`뿐이다.
  일반형: **증거를 인용할 때 그 경로의 수명을 함께 확인해야 한다.** 남이 만든 임시 산출물을
  인용하면 정리 단계에서 내 근거가 사라지고, 그 시점에 내 판정은 재검증 불가가 된다.
- **보고된 파일명과 디스크의 파일명이 달랐다 — 마지막 사례이고, 이번엔 내가 디스크를 먼저 봤다.**
  부모가 보존 완료를 알리며 `gate-mutation-red-status-column.xml` /
  `gate-mutation-green-restored.xml` / `editmode-baseline-green.xml`을 적었는데, 디스크의
  실제 이름은 `cond1-status-column-removed-RED.xml` / `cond1-baseline-GREEN.xml` /
  `editmode-baseline-green.xml`이다(셋 중 하나만 일치). 내가 **메시지의 이름으로 grep해서 빈
  결과를 받고** `ls`로 실제 이름을 찾은 뒤 다시 조회했다.
  **보고와 디스크가 어긋날 때 디스크가 권위다.** 이 회의가 여섯 번 확인한 것이고
  (인테이크의 grep 0건, QA의 5곳 skip, 내 10/7, HEAD vs index, 뮤테이션 창, 그리고 이것),
  마지막 사례에서는 **빈 grep 결과가 도구 결함이 아니라 이름 불일치의 신호**였다 —
  W-14를 겪은 뒤라 "bash가 또 비었나"로 읽을 수도 있었는데, `ls`로 교차 확인한 것이 갈랐다.
  **부재의 원인이 둘 이상일 때는 원인을 특정하기 전에 다른 도구로 한 번 더 봐야 한다.**
