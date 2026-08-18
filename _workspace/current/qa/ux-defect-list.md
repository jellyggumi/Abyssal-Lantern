# 인게임 UI/UX 결함 목록 — 코드 실측

- register-role: canonical
  UX-계열(감사 소견: 코드 경로와 좌표로 확인한 가시성·UX 결함)의 정본. 정본 근거는 코드 경로 인용이다.
  D-계열과 서로소인 것은 결함이 아니라 역할 분리다 — 같은 결함이 두 대장에 있으면 그것이 결함이다.

- run-id: 20260813-ux-defects-stage1
- owner: game-qa 레인 (UX 감사)
- 대상: 작업 트리 현재 상태. **코드 0줄 수정.** 읽기와 좌표 계산만.
- 규칙: 모든 행에 `파일:라인` 또는 이미지 경로. 근거 없는 항목은 쓰지 않았다. `[OBSERVED]` / `[INFERENCE]` 표기.
- 좌표 규약: 캔버스 기준 해상도 **1920×1080** (`GameFeelVfx.cs:779`, `MobileSafeArea.cs:41`). 스크린샷은 1280×720이므로 화면 픽셀 = 캔버스값 × 0.667.

---

## 0. 반드시 답할 질문 — 현재 HUD가 적 턴에 무엇을 하라고 말하는가?

**답: 두 가지를 하라고 말한다. 둘 다 거짓이다.**

'아무것도 말하지 않는다'보다 나쁘다. 화면은 침묵하지 않고 **틀린 지시를 두 줄 내보낸다.**

| # | 화면 위치 | 적 턴에 표시되는 문자열 | 지시 출처 | 그 입력이 막히는 지점 |
|---|---|---|---|---|
| 1 | 상단 중앙 (anchor 0.5, 0.9) | `적 포격 준비 중...  ·  클릭: 벽돌 예약` | `SiegeAlarmSystem.cs:231` | `BrickPlacementController.cs:76-82` — `EnforcesOneShotTurns`면 조기 반환. 기본값 `true` (`GameManager.cs:144`) |
| 2 | 하단 좌측 (anchor 0.02→0.82, 0.02) | `<b>ARCHER</b> 준비  ·  푸른 링 드래그 → 발사` | `LaunchManager.cs:118` (`BuildControlGuideText`) | `LaunchManager.cs:530-532` 조준 게이트 + `GameManager.cs:2069` `lm.enabled = false` |

**지시 2가 적 턴에도 살아 있는 이유는 우연이 아니라 경로가 그렇다** [OBSERVED]:
`EndTurn()` → `BeginOneShotTurn()` (`GameManager.cs:2138`) → `LaunchManagerRef?.SetSelectedUnit(...)` (`:1957`)
→ `SetSelectedUnit`이 `controlGuideText.text = BuildControlGuideText()`로 **매 턴 다시 쓴다** (`LaunchManager.cs:498`).
그리고 이 텍스트는 `LaunchManager.cs:304`에서 한 번 `SetActive(true)` 된 뒤 **어디에서도 꺼지지 않는다**
(`SetActive(false)`를 받는 것은 `launchStatsText`뿐 — `:309`, `:401`).

즉 적 턴 화면은 플레이어에게 "아처 준비됐다, 링을 드래그해라"라고 말하면서
드래그 핸들러를 컴포넌트째로 꺼둔 상태다.

**나머지 화면 요소는 전부 지시가 아니라 상태 표시다** [OBSERVED]:

| 요소 | 적 턴 동작 | 근거 |
|---|---|---|
| `turnText` | `"ENEMY BATTERY"` | `GameManager.cs:2174` |
| `timerText` | **"15"에서 얼어붙음** (§3 UX-016) | `GameManager.cs:1720`, `:1726` |
| 턴 진행 바 | 6% 남짓 줄다가 정지 | `GameFeelVfx.cs:931-936` |
| 적 진영 셰브런 | 점등 (anchor 0.58, 0.93) | `GameFeelVfx.cs:840`, `PersistentSiegeHudSignals.cs:42` |
| 발사 준비 크로스헤어 | alpha 0.22로 감광 | `GameFeelVfx.cs:959-964`, `PersistentSiegeHudSignals.cs:43` |
| 토스트 `"적 턴"` | 1.7초 표시 후 소멸 | `GameFeelVfx.cs:623` |
| 배치 HUD | 숨김 | `DeploymentController.cs:153-157` |
| 벽돌 타입 패널 | 숨김 (애초에 생성도 안 됨) | `BrickPlacementController.cs:80` |
| **활성 버튼 수** | **0** | 위 두 항목이 유일한 버튼 소유자 |

**적 턴 = 109.7초 / 경기의 34.1%** (`qa/idle-time-measurement.md:275`).
그 시간 동안 화면의 유일한 명령형 문장 두 개가 모두 작동하지 않는다.

---

## 1. 정보 전달 결함 — 플레이어가 알아야 하는데 화면이 말해주지 않는 것

### 1.1 추적 결과 요약

| 알아야 할 것 | 전달 UI | 상태 |
|---|---|---|
| 누구 턴인가 | `turnText` (상단 중앙) + 셰브런 2개 + 토스트 | **전달됨** |
| 발사 가능한가 | 크로스헤어 밝기 (`GameFeelVfx.cs:956-965`) + `launchStatsText` "발사!/더 당기기" (`LaunchManager.cs:391`) | **전달됨** (단 UX-007 가려짐) |
| 뭘 쏘는가 (이번 턴) | `controlGuideText` 유닛명 (`LaunchManager.cs:118`) | **전달됨** |
| 다음에 뭘 쏘는가 | — | **없음** (UX-004) |
| 적이 뭘 쏘는가 | — | **없음** (UX-004) |
| 얼마나 남았나 (경기) | — | **없음** (UX-005) |
| 얼마나 남았나 (턴) | `timerText` | 전달되나 적 턴엔 정지 (UX-016) |
| **바람은 어떤가** | `windText` | **렌더 불가** (UX-001) |
| 점수 | `scoreText` | **렌더 불가** (UX-002) |

### 1.2 결함표

| ID | 심각도 | 상태 | 증상 | 근거 | 제안 |
|---|---|---|---|---|---|
| UX-001 | **S1** | closed 2026-08-14 | **바람이 화면에 절대 표시되지 않는다.** `WindText`가 Canvas의 자식이 아니라 **씬 루트**다. `TextMeshProUGUI`는 상위에 Canvas가 없으면 그리지 않는다. 값은 매 턴 계산·포맷되지만 어디에도 나오지 않는다 | 부모 없음: `Assets/Scenes/SampleScene.unity:3868` `m_Father: {fileID: 0}` / 씬 루트 목록에 직접 등재: `:4306`. 대조군 `TurnText`는 `:962` `m_Father: {fileID: 339938660}` (= Canvas, `:1285`). 값 계산은 살아 있음: `GameManager.cs:2175-2180`. 화면 부재 확인: `qa/evidence/visual/ux-3-player-turn.png` (좌우 상단 어디에도 `BANNER WIND` 없음) | `WindText`를 Canvas 하위로 재부모화. 바람 상한이 2.0→6.5로 커지는 난이도 곡선(`GameManager.cs:65-66`)이 조준의 최대 변수인데 그 값이 안 보인다. 파티클(`WindVfxManager.cs:82-88`)만으로는 방향은 알아도 **세기 수치를 못 읽는다** |
| UX-002 | **S1** | closed 2026-08-14 | **점수가 화면에 절대 표시되지 않는다.** `ScoreText`도 동일하게 씬 루트 | `SampleScene.unity:2692` `m_Father: {fileID: 0}`, 씬 루트 등재 `:4307`. 값 갱신 경로 살아 있음: `GameManager.cs:2182`, 호출 `:1329`. 화면 부재: `ux-3-player-turn.png` 우상단 공백 | 동일 재부모화. 결과 화면은 점수를 쓰는데(`GameManager.cs:2270-2272`) 경기 중엔 누적을 못 본다 |
| UX-003 | **S1** | closed 2026-08-14 | 적 턴에 화면이 내리는 지시 2개가 전부 거짓 (§0) | `SiegeAlarmSystem.cs:231` + `BrickPlacementController.cs:76-82`; `LaunchManager.cs:118`+`:498`+`:304` + `LaunchManager.cs:530-532` | 둘 중 하나를 골라라. (a) D3 벽돌 예약을 실제로 켜서 문구를 참으로 만들거나, (b) 적 턴에 두 문자열을 상태 표현으로 교체. **문구만 지우는 것은 최악** — 적 턴 화면에서 텍스트가 하나 더 사라져 §0 표가 전부 수동태가 된다 |
| UX-004 | S2 | open | **다음 발사체를 아무도 예고하지 않는다.** `OneShotSiegeRules.ProjectileForTurn(turnCount)`은 완전 결정론적이고 public인데(`OneShotSiegeRules.cs:25-29`, `GameManager.cs:1971-1985`) 소비처는 프리팹 선택뿐. 플레이어는 자기 턴이 시작돼야 무기를 알고, **적 무기는 끝까지 모른다** | 결정 로직 `OneShotSiegeRules.cs:13-18`; 유일한 표시 경로 `LaunchManager.cs:118` (이번 턴 자기 것만); 그 외 소비처 없음 (`ProjectileForTurn` 전체 grep 결과 = `GameManager.cs:1951`, `:1975` 두 곳뿐) | 비교작 9개 장치 중 **D6(완전 텔레그래프)** 부분 보유를 완전 보유로 올릴 수 있는 최저비용 항목. 이미 순수 함수가 있으므로 UI만 붙이면 된다. 자리는 §4 밴드 A |
| UX-005 | S2 | open | **경기 진행도를 알 수 없다.** 턴 수는 `GameManager.TurnCount`(`:175`)로 노출돼 있으나 이를 그리는 UI가 하나도 없다. 목표 경기 길이 300초·약 43턴(`qa/idle-time-measurement.md:220-222`)인데 플레이어는 5턴째인지 35턴째인지 화면에서 알 수 없다 | `TurnCount` 소비처 grep: `GameFeelVfx.cs`·`SiegeAlarmSystem.cs`에 0건. 스테이지 진행은 결과 화면에만 존재(`SiegeEcosystem.cs:291-309`) | 코어 HP 배지(`GameFeelVfx.cs:833-834`)가 사실상 유일한 진행 신호다. HP는 비선형이라 "얼마 남았나"의 대용이 못 된다 |

---

## 2. 밀도 / 가려짐

### 2.1 상태별 화면 요소 수 [OBSERVED]

| 상태 | 계측 텍스트 | 계측 버튼 | 출처 |
|---|---|---|---|
| 타이틀 | 19 | 9 | `qa/evidence/visual/ux-measurements.txt:3` |
| 매치 시작 | 11 | 1 | `:7` |
| 플레이어 턴 | 10 | 1 | `:11` |
| **적 턴** | **미계측** | **미계측** | UX-015 |

계측 방식은 `isActiveAndEnabled && text != 공백`으로 세는 것이다 (`VisualEvidenceCapture.cs:277-286`).
**이 계수는 실제 렌더 수보다 2 많다** — UX-001·UX-002의 두 텍스트가 `isActiveAndEnabled == true`
(`SampleScene.unity:3758`, `:2582` 둘 다 `m_IsActive: 1`)이면서 Canvas가 없어 그려지지 않기 때문이다.

> **실제 화면 요소 수: 플레이어 턴 8개** (계측 10 − 유령 2) [INFERENCE — 계수 규칙과 씬 구조에서 유도]
> 8개 = 턴 라벨 / 타이머 / 보급 게이지 / 배치 토글 / KEEP 배지 / BREACH 배지 / 발사 스탯 / 조작 가이드.
> `ux-3-player-turn.png`에서 세어지는 텍스트 덩어리 수와 일치한다.

플레이어 턴 → 매치 시작의 차이 1개는 `"내 턴"` 토스트다 (1.7초 수명, `GameFeelVfx.cs:623`).
`ux-2-match-start.png`에는 있고 `ux-3-player-turn.png`에는 없다 — 두 캡처 간격 1.5초(`VisualEvidenceCapture.cs:261`)와 정합.

**밀도 자체는 문제가 아니다. 8개는 적다.** 문제는 그중 3개가 겹쳐 있고 2개가 유령이라는 것이다.

### 2.2 겹침 결함표

모든 좌표는 캔버스 1920×1080 기준으로 앵커·피벗·크기에서 직접 계산했다 [OBSERVED — 상수 전부 인용].

| ID | 심각도 | 상태 | 증상 | 근거 | 제안 |
|---|---|---|---|---|---|
| UX-007 | S2 | open | **발사 준비 크로스헤어가 파워/각도 수치를 관통한다.** 다이아몬드 마커가 `파워 60%`의 글자를 덮는다 | `LaunchStatsText` anchor (0.5, **0.15**) → 하단에서 162px (`LaunchManager.cs:267-270`). `LaunchReadyMarker` anchor (0.5, **0.17**) → 하단에서 183.6px (`GameFeelVfx.cs:841`). 간격 21.6px. 크로스헤어는 42×42 다이아몬드 + 58px 세로 바(`GameFeelVfx.cs:856-860`) → 중심에서 상하 ±29px. **하단 끝 154.6px < 텍스트 중심 162px** → 관통. 육안 확인: `ux-3-player-turn.png` 중앙 하단, 노란 다이아몬드가 `발사!`와 `파워 60%` 사이를 가로지름 | 크로스헤어를 실제 발사 링 위치로 옮기거나(현재는 화면 중앙 하단 고정인데 플레이어 링은 좌측 x=−14.5), 스탯 텍스트를 밴드 A로 내려라 |
| UX-008 | S2 | open | **벽돌 타입 선택 패널이 화면 밖에 있다.** 50px 높이 중 14px만 보인다. WOOD/STONE/IRON 버튼 3개가 사실상 클릭 불가 | `BrickPlacementController.cs:288-292`: anchorMin/Max (0.5, **0**), pivot (0.5, **0**), sizeDelta (390, **50**), anchoredPosition (0, **−36**). 하단 앵커·피벗 0에서 y=−36이면 패널 하단이 화면 아래 36px. 코드 주석은 `// centered and clean above unit selection`이라고 적혀 있어 **의도와 값이 어긋난다.** `HudLayoutTests.cs`는 이 패널을 전혀 보지 않는다(§4.1) | y −36 → **+94** (§4 밴드 A에 정확히 안착). 단, 좌표를 `:292` 인라인 리터럴로 두면 D-009 재발이다 — **상수로 올리고 `패널상단 < LastStandBottom` 단언을 `HudLayoutTests`에 추가**해야 한다. **D3(벽돌 예약) 재활성의 선행조건** — 게이트만 풀면 "패널은 떴는데 못 누른다"가 된다 |
| UX-009 | S3 | open | 진영 셰브런 2개가 턴 진행 바를 덮는다 | `PlayerTurnMarker` anchor (0.42, 0.93) → 상단에서 75.6px, 34×34을 45° 회전 → 세로 반지름 24px → 51.6~99.6px 점유 (`GameFeelVfx.cs:839-840`). `TurnProgressBackground`는 상단 −78px, 높이 10 → 73~83px (`GameFeelVfx.cs:815-816`). **바 전체가 셰브런 세로 범위 안에 들어간다.** 가로도 겹침: 셰브런 x 782~830, 바 x 640~1260 | 셰브런을 y 0.93 → 0.955로 올리거나 바를 −78 → −92로 내려라. 둘 다 상시 표시 신호라 어느 하나가 잠깐 사라지는 문제가 아니다 |
| UX-010 | S3 | open | KEEP CORE 배지 좌하단 모서리가 배치 토글 버튼 우상단과 겹친다 (28.4 × 9.2px) | 배지 anchor (0.18, 0.84), size 260×44 (`GameFeelVfx.cs:833`, `:1062`) → x 215.6~475.6, 상단에서 150.8~194.8px. 토글 anchor (0,1) pivot (0,1) pos (18, −134) size 226×26 (`DeploymentController.cs:634-638`) → x 18~244, 상단 134~160px. 교집합 x 215.6~244, y 150.8~160. 육안: `ux-3-player-turn.png` 좌상단 `배치 모드` 회색 바와 `KEEP CORE 150/150` 배지 | 배지를 x 0.18 → 0.20으로 밀거나 토글 폭을 226 → 195로 줄여라 |
| UX-011 | S3 | open | 플로우 스트립과 타이머가 **간격 0px**로 맞닿아 있다. 폰트·줄높이가 조금만 커지면 즉시 겹친다 | 스트립 anchor (0.5, 0.9) pivot (0.5,1) size 700×26 (`SiegeAlarmSystem.cs:95-97`) → 상단 108~**134**px. `timerText` pos (0, −134) pivot (0.5,1) size 100×40 (`GameFeelVfx.cs:827-830`) → 상단 **134**~174px. 경계 정확히 일치 | 스트립을 0.9 → 0.905로 (약 5px 여유). 한국어 문자열이 들어가는 자리라 안전 마진이 필요하다 |
| UX-012 | S3 | open | 씬에 직렬화된 HUD 텍스트 4종만 **외곽선이 없다.** 밝은 하늘 배경 위 흰 글자라 대비가 낮다 | `SampleScene.unity` 전체에서 `m_outlineWidth` 매치 **0건** → 4종 전부 기본값 0. 런타임 생성 텍스트는 전부 외곽선 보유: `GameFeelVfx.cs:876` (0.18), `SiegeAlarmSystem.cs:93` (0.16), `:141` (0.15), `LaunchManager.cs:286` (0.18), `GameManager.cs:1308` (0.16). `TurnText`는 24pt (`SampleScene.unity:1016`). 육안: `ux-3-player-turn.png` 상단 `YOUR SIEGE TURN`이 구름에 묻힘 | 4종에 `outlineWidth 0.16` 적용. 나머지 HUD가 전부 쓰는 값이라 새 규칙이 아니라 누락 보충이다 |
| UX-013 | S4 | open | `LaunchStatsText`·`ControlGuideText`가 `MobileSafeArea` 콘텐츠 루트를 거치지 않고 Canvas에 직접 붙는다 — 노치 영역 침범 가능 | `LaunchManager.cs:260`, `:282` `SetParent(canvas.transform, false)`. 대조군은 전부 `MobileSafeArea.GetContentRoot(canvas)` 사용: `GameFeelVfx.cs:795`, `SiegeAlarmSystem.cs:79`·`:86`, `BrickPlacementController.cs:286`, `DeploymentController` 계열 | 두 줄을 콘텐츠 루트로 교체. `ControlGuideText`는 anchorMin y 0.02로 화면 최하단이라 홈 인디케이터와 충돌 위험이 실제로 있다 |

---

## 3. 피드백 공백

| ID | 심각도 | 상태 | 증상 | 근거 | 제안 |
|---|---|---|---|---|---|
| UX-014 | **S1** | **open** | **적 턴 109.7초 동안 활성 버튼 0개, 유효 입력 0개.** 화면이 보여주는 것은 상태 5종(턴 라벨·정지한 타이머·정지한 바·셰브런·감광된 크로스헤어) + 거짓 지시 2종 + 이벤트 발생 시 알람 줄뿐이다 | 버튼 소유자 전부 차단: `DeploymentController.cs:153-157` (배치 HUD), `BrickPlacementController.cs:80` (벽돌 패널), `GameManager.cs:1164` (Last Stand는 위기 시에만). 입력 3중 차단은 `qa/idle-time-measurement.md:31-76`에 확정. 시간 근거 `:275` | §4의 자리에 적 턴 전용 인터랙션을 놓아라. 이 34.1%가 지금 완전한 공백이다 |
| UX-015 | S2 | open | **적 턴 화면의 시각 증거가 존재하지 않는다.** 캡처 하네스 주석은 `"...and the AI turn the player cannot act during"`이라고 명시하는데(`VisualEvidenceCapture.cs:239-240`) **본문은 그 상태를 찍지 않는다** | 캡처 호출 3건뿐: `:249` `ux-1-title`, `:258` `ux-2-match-start`, `:262` `ux-3-player-turn`. `AITurn` 대기·캡처 코드 없음. 결과 파일도 3블록: `ux-measurements.txt:1-11` | 경기의 34.1%를 차지하는 상태에 스크린샷이 없다. 이 문서의 §0이 코드 추적으로만 작성된 이유다. `ux-4-enemy-turn` 캡처 추가 필요 |
| UX-016 | S3 | open | **타이머 위젯의 의미가 상태마다 뒤집히고, 적 턴엔 얼어붙는다.** 내 턴엔 "지금 행동하라", 적 턴엔 "기다려라" — 같은 숫자가 반대 뜻이다. 게다가 적 턴 값은 거의 움직이지 않는다 | `GameManager.cs:1720` `if (isResolvingTurn) return;`이 `:1726`의 `timerText` 갱신보다 **앞선다**. AI는 0.9초 만에 발사하고(`GameManager.cs:2159` 0.4s + `SimpleAI.cs:30` 0.5s) 발사 즉시 `isResolvingTurn = true`(`GameManager.cs:2051`) → 타이머는 15 → **14.1에서 정지**. 남은 약 4.2초의 꼬리 구간(`qa/idle-time-measurement.md:249`) 내내 같은 숫자. 진행 바도 동일 소스(`GameFeelVfx.cs:934`)라 6% 줄고 멈춤 | 적 턴엔 카운트다운을 숨기고 다른 표현을 써라. 지금은 "멈춘 것처럼 보임"을 플로우 스트립의 애니메이션 점(`SiegeAlarmSystem.cs:224`, `:230`)만으로 방어하고 있는데, 정작 가장 큰 숫자 위젯이 얼어 있어 반대 신호를 낸다 |
| UX-017 | S3 | open | 벽돌 예약 힌트가 **월드 좌표 (0, 4.5) 고정 1회성 라벨**로 설계돼 있다. 전장 한복판에 2.2초 떴다 사라진다 | `BrickPlacementController.cs:112-114` `SpawnFeedbackLabel(new Vector3(0f, 4.5f, 0f), ...)`, 수명 2.2s, `hintShownThisTurn` 가드(`:109-111`). 단 `:76-82` 조기 반환이 앞서므로 **출하 설정에선 실행되지 않는 죽은 경로** | D3 재활성 시 이 힌트는 재설계 대상이다. 전장 중앙은 성 두 채 사이 궤적이 지나는 자리다 |

---

## 4. 신규 기믹이 들어올 UI 자리 — 있다

**결론: 있다. 원샷 모드가 화면 하단을 통째로 비워놨다.**

### 4.1 밴드 A — 비워진 선택 행 (최우선)

```
하단 오프셋 63.5 ~ 144.5 px   (높이 81)
가로        중앙 기준 ±270 px  (폭 540)
```

- 근거: `GameManager.cs:1949` `SetSelectionControlsVisible(false)` → `:1995-1998`이 4장을 전부 `SetActive(false)`.
  카드 4장이 쓰던 자리는 `SelectionRowY = 104` ± `SelectionRowCardHeight/2 = 40.5` (`GameManager.cs:1065-1066`).
  폭은 82×1.5×4 + 16×3 = 540 (`:1035-1039`, `:1049`).
- **원샷 모드에서 상시 비어 있다** — 조건부가 아니다.
- **경계값은 상수로 고정돼 있으나, 밴드 내부의 새 점유자는 아무 보호도 못 받는다.**
  `HudLayoutTests.cs`는 전문 38줄의 순수 상수 테스트다 — 씬도 GameObject도 읽지 않고
  `GameManager`의 const 4개끼리의 부등식 3개만 단언한다(`:24`, `:26`, `:33`).
  밴드가 비었는지도, 무엇이 들어왔는지도 검증하지 않는다. 새 요소를 넣어도 깨지지 않지만
  **지켜주지도 않는다.** 이 자리를 쓰는 작업은 좌표를 상수로 올리고 단언을 한 줄 추가해야 한다.
  근거: D-009가 정확히 이 실패 모드였다 — 좌표가 인라인 리터럴로 흩어져 카드가 화면 밖으로
  나갔는데 애니메이션도 되고 `interactable`도 true라 아무도 눈치채지 못했다
  (`defect-register.md:14`). UX-008의 벽돌 패널이 **지금 똑같은 상태**다.
- 아래쪽 `ControlGuideText`(하단 오프셋 21.6~93.6, `LaunchManager.cs:292-296`)와는
  **y 94 이상**을 쓰면 겹치지 않는다 → 실사용 밴드 **94 ~ 144.5** (높이 50).
- **UX-008의 벽돌 패널(높이 정확히 50)이 이 밴드에 그대로 들어간다.**
  패널은 pivot y=0이라 `anchoredPosition.y`가 곧 하단 → 94~144.
  위쪽 Last Stand 카드 하단 160(§4.4)까지 **16px 여유**. 위기 상황에 카드가 올라와도 안 겹친다.

### 4.2 밴드 B — 좌측 열 (적 턴에 확장됨)

```
플레이어 턴:  상단 오프셋 160 ~ 302 px  (높이 142),  가로 18 ~ 244 px
적 턴:        상단 오프셋 104 ~ 302 px  (높이 198)   ← 배치 HUD가 숨으며 확장
```

- 상한: 배치 토글 하단 = 134 + 26 = 160 (`DeploymentController.cs:637-638`). 적 턴엔 `SetHudVisible(false)`(`:153`)로 보급 게이지(상단 104~130, `:599-600`)까지 비므로 104까지 올라간다.
- 하한: 알람 피드 상단 = 0.72 × 1080 → 하단에서 777.6 = **상단에서 302.4** (`SiegeAlarmSystem.cs:81`).
- **적 턴에만 커지는 자리**라는 성질이 D2/D3형 기믹과 맞는다.

### 4.3 밴드 C — 알람 피드의 우측 거울면

```
상단 오프셋 302 ~ 452 px,  가로 1431 ~ 1891 px  (460 × 150)
```

- 좌측 알람 피드(anchor 0.015, 0.72 / 460×150 / pivot 0,1 — `SiegeAlarmSystem.cs:81-83`)를
  x축 대칭 이동한 영역. 현재 이 사각형을 점유하는 요소 없음.
- 상단 BREACH CORE 배지(anchor 0.82, 0.84 → 상단 150.8~194.8)와 **107px 여유**로 분리된다.

### 4.4 밴드 D — Last Stand 카드 슬롯 (조건부)

```
하단 오프셋 160 ~ 264 px  (높이 104),  가로 중앙 ±78 px
```

- `LastStandCardY = 212` ± `LastStandCardHeight/2 = 52` (`GameManager.cs:1067-1068`), 폭 156 (`:1116`).
- **위기 상태에서만 나타난다** (`GameManager.cs:1162-1164` `SetActive(available)`).
  즉 평시엔 비지만 언제든 카드가 올라와 덮는다. 상시 요소를 놓을 자리가 아니다.

### 4.5 요약

| 밴드 | 위치 | 크기 | 가용성 | 추천 용도 |
|---|---|---|---|---|
| **A** | 하단 중앙 94~144.5 | 540 × 50 | **원샷 모드 상시** | 벽돌 타입 패널 이전(UX-008), 다음 발사체 예고(UX-004) |
| **B** | 좌측 상단 104~302 | 226 × 198 | 적 턴에 확장 | 적 턴 전용 입력 위젯 |
| **C** | 우측 302~452 | 460 × 150 | 상시 | 경기 진행도(UX-005), 적 예고 |
| D | 하단 중앙 160~264 | 156 × 104 | 위기 시 점유됨 | 상시 요소 금지 |

---

## 5. 심각도별 집계

| 심각도 | 건수 | ID |
|---|---|---|---|
| S1 (치명) | 4 | UX-001, UX-002, UX-003, UX-014 |
| S2 (중대) | 5 | UX-004, UX-005, UX-007, UX-008, UX-015 |
| S3 (경미) | 6 | UX-009, UX-010, UX-011, UX-012, UX-016, UX-017 |
| S4 (미관) | 1 | UX-013 |
| **합계** | **16** | |

> ID는 UX-001~UX-017 중 **UX-006이 결번**이다. 초안에서 밀도 항목 하나를 §2.1 본문 계측으로
> 흡수하면서 비었다. 삭제된 결함이 아니라 번호만 비어 있다.

---

## 6. 이 감사가 확인하지 못한 것

정직하게 남긴다.

1. **적 턴 실화면.** 캡처가 없다(UX-015). §0은 전부 코드 경로 추적이며, 화면 대조를 거치지 않았다.
   `ux-4-enemy-turn` 캡처가 나오면 §0 표를 재검증해야 한다.
2. **UX-001/002의 렌더 부재는 두 경로로 확인했으나 런타임 단언은 없다.** 씬 구조(부모 없음)와
   스크린샷 부재가 일치하므로 결론은 견고하나, `windText.canvas == null` 같은 직접 단언은 돌리지 않았다
   (Unity가 배치로 점유 중 — 하네스 규칙).
3. **모바일 실기기 세이프에어리어.** UX-013은 코드 구조상의 위험이며, 노치 있는 실기기에서
   실제로 잘리는지는 측정하지 않았다.
4. **1280×720 외 해상도.** 겹침 계산은 캔버스 1920×1080 기준이다. `matchWidthOrHeight = 0.5`
   (`GameFeelVfx.cs:780`)이므로 극단적 종횡비에서는 앵커 기반 요소와 픽셀 기반 요소의
   상대 위치가 달라진다. UX-009·UX-010·UX-011은 종횡비에 따라 악화될 수 있다 [INFERENCE].
