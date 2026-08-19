# 결함 대장 화해 — S1 재검증과 게이트 재판정

- register-role: derived
  두 정본을 대조한 감사 뷰. 여기 등재된 모든 ID는 정본 대장에 존재해야 한다.

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: game-qa 레인 (대장 감사)
- date: 2026-08-18
- 대상: **`feature/hero-growth-series` @ `2333e93e`** — 라이브 계보(§0.5).
  **게임 코드 0줄 수정.** 읽기와 추적만.
- 규칙: 모든 판정에 `파일:줄` 연쇄. `[OBSERVED]` / `[INFERENCE]` / `[확인 불가]` 표기.
- 기준 커밋: **`2333e93e` (= 라이브 `73f79240` + 2).** `origin/main`이 아니다 — §0.5가
  본문이다. 감사 중 작업 트리에 부모 레인의 뮤테이션(`Adopt(windText)` 삭제)이 살아
  있었으므로 판정은 `git show <ref>:` 기준이며, 트리의 일시 상태를 판정으로 적지 않았다(§0.4).
  **네 건의 판정은 `origin/main`에서도 재확인했다**(§0.5 표).

## 결론 (한 문단)

**S1 4건 중 3건 closed, 1건 OPEN.** UX-014는 닫히지 않았다 — 인테이크가 인용한
`SiegeAlarmSystem.cs:234`는 `:217`이 선점하므로 **적 턴에 도달 불가**다(§0.1).
Director가 **open / S1 / 면제 거절**로 확정했으므로(§1.4) 계약
*"Any open S1 defect blocks every gate"*가 **실제로 걸린다** — 지금 재측정으로 PASS를 받을
게이트는 **0개**다(§3.1). 그리고 차단이 걷혀도 PASS는 0개다: 8개 게이트 전부에 자기 증거
블로커가 따로 있다(§3.1a). 해제의 임계 경로는 S1이 아니라 **S2인 UX-015**(적 턴 캡처
부재)이며 — 낮은 심각도가 높은 심각도의 선행 조건이다. 신규 결함 후보 2건을 등재 제안한다
(§1.6). 이 사이클의 산출은 "게이트 해제"가 아니라 **"게이트가 왜 닫혀 있는지 정확히 아는 것"**
이며, status 열이 없어서 지금까지는 그조차 몰랐다(§2.2 — 소급하면 오늘까지 UX 16건 전부 open).

**반증 7건 중 5건은 내 자신에게 향한다** (§5 항목 8): 없는 탄착 마커 인용(§0.3),
`baseline: HEAD`(브랜치 미표기, §0.5), 그 §0.5를 쓰면서 **같은 결함의 반대 축 반복**
(끝점 미표기 → 틀린 인과), `grep → 0건`을 근거로 삼았으나 **그 0건이 거짓**(bash grep의
조용한 위음성 — 도구 이슈 신고함), 그리고 `:2200-2400` 줄번호를 **뮤테이션 중인 트리에서**
읽어 1줄씩 어긋남. **뒤의 둘은 이 문서가 다른 사람에게 지적한 것과 정확히 같은 오류다** —
§4.1이 인테이크의 `0건`을 정정했고, §0.4가 "뮤테이션 중인 트리를 재지 말라"고 적었다.
Director도 같은 형태를 자기 F-1에서 기록했다: **개인의 부주의가 아니라 이 사이클의 패턴이다.**

---

## 0. 부모 판정 반증 — 먼저 읽어라

인테이크(`intake/production-brief-registers.md:25-32`)는 S1 4건이 **전부 closed**라고 측정했다.
**3건은 유지되고, 1건은 반증된다.** 그리고 인용된 증거 하나가 주장보다 약하다.

### 0.1 반증 — UX-014는 closed가 아니다. 분기 순서가 그것을 불가능하게 한다 [OBSERVED]

인테이크 표는 이렇게 적었다:

> UX-014 | **closed (처방 변경)** | `ShotTraceDirector`가 사후 판독을 실어
> `SiegeAlarmSystem.cs:234` `LatestLine`으로 표시

`SiegeAlarmSystem.UpdateFlowStrip`(`:197-255`)은 **단일 if / else-if 사슬**이다:

| 줄 | 조건 | 결과 |
|---|---|---|
| `:210` | `if (gm.IsResolvingTurn)` | `"볼리 해결 중..."` |
| `:217` | `else if (!gm.IsPlayerTurn)` | `"적 포격 준비 중..."` |
| **`:234`** | `else if (!string.IsNullOrEmpty(ShotTraceDirector.LatestLine))` | **판독 줄** |
| `:245` | `else` | 스트립 `SetActive(false)` |

적 턴이면 `!gm.IsPlayerTurn`이 참이므로 **`:217`이 선점한다.** 따라서 `:234`는
**적 턴에 구조적으로 도달 불가**다.

코드 자신의 주석이 의도까지 확증한다 — `:236`:

> `// The player's turn is exactly when last turn's result is worth reading`

즉 판독 줄은 **플레이어 턴의 장치**다. 인테이크는 `:234`를 줄번호까지 인용하면서
**그 위 17줄(`:217`)을 읽지 않았다.** 배선의 존재를 확인하고 적용 위치를 확인하지 않은 것이다.

**표시할 것이 있는가** (부모가 물은 질문) — **있다** [OBSERVED]:
- `LatestLine`은 적 턴에 **비지 않는다.** 플레이어 샷이 `ShotTraceDirector.cs:246-260`
  `Seal()`에서 채웠고(`WaitAndEndTurn`의 정착 경계에서 호출, docstring `:241-244`),
  `:129`의 초기화는 `ResetForNewMatch`에서만 일어난다.
- 월드 궤적도 남는다 — §0.3.

→ **내용은 있고 HUD 경로만 닫혀 있다.**

**프레이밍 교정 (Designer 논증 채택)** — 초안은 "처방이 측정된 자리에 착지하지 않았다"고
적었다. 더 정확한 서술은 **"측정된 자리는 판독이 갈 자리가 아니었다"**다. 근거:
조사가 인용한 Worms 원문이 `when all movement on the battlefield has ceased`이고
`GameManager.cs:2339`이 정확히 그 자리(정착 루프 뒤)에서 `Seal()`한다. 적 턴에는 아직
움직이는 중이므로 **판독할 완료 사실이 없다.** 즉 `:234`가 플레이어 턴인 것은 버그가 아니라
처방 준수다. 반증되는 것은 인테이크의 **"적 턴에 표시된다"는 주장**이며, 구현의 옳음이 아니다.

**status/severity — Director 확정 (초안의 보류를 갱신)**: **open / S1 유지 / 면제 거절.**
판정 근거와 신설 규칙은 §1.4. 이 문서가 스스로 확정하지 않은 것은 옳았고, 판정이 도착해
갱신했다.

> **대장 등재 문구**: `open (S1). 기제: 적 턴 HUD 판독 도달 불가(SiegeAlarmSystem.cs:217이
> :234를 선점). 처방은 구현됐으나 적 턴은 판독 대상이 아니다 — 남은 것은 입력 0.`

### 0.2 반증 — UX-001/002의 인용 증거에 입양 **전** 측정이 없다 [OBSERVED]

인테이크 `:44-45`:

> 네 건 모두 코드에서 닫혔고 증거도 있다(`qa/evidence/font/orphan-labels.md`가 입양
> 전/후 좌표를 표로 실측)

`orphan-labels.md`의 두 표는 **부모 열이 동일하다**:

| 표 | 라벨 | 캔버스 | 부모 |
|---|---|---|---|
| `:5` **입양 전** | windText | `GameplayHudCanvas` | `MobileSafeArea` |
| `:12` **입양 후** | windText | `GameplayHudCanvas` | `MobileSafeArea` |

입양 **전**의 `windText`는 정의상 Canvas 조상이 없다(`m_Father: {fileID: 0}`). 그렇다면
"입양 전" 행의 캔버스 열은 비어 있어야 한다. **이미 입양된 상태가 두 번 찍혀 있다.**

출처로 확정된다 [OBSERVED]:
```
git log --oneline -S "HudCanvas.Adopt(windText)" -- Assets/Scripts/GameManager.cs
  → 0cb0efb9 fix(hud): one canvas for the HUD, and a floor under its text
git show 0cb0efb9:_workspace/current/qa/evidence/font/orphan-labels.md
  → "입양 전" 표에 이미 GameplayHudCanvas / MobileSafeArea
```
**하네스가 수정과 같은 커밋에서 돌았다.** 그러므로 이 문서는 **닫힌 상태를 증명하고,
전이는 증명하지 않는다.**

판정에 미치는 영향: UX-001/002의 **closed 판정 자체는 유지된다**(§1.1의 코드 연쇄가 독립
근거다). 무효화되는 것은 "입양 전/후를 실측했다"는 **증거의 성격 주장**이다. 전/후 대조가
필요하면 부모의 뮤테이션 증명이 그 자리를 채운다(§4.1).

### 0.3 내 자신의 오류 정정 — 탄착 마커는 없다 [OBSERVED]

감사 중 내가 IRC로 "궤적·탄착 마커"라고 브로드캐스트했다. **마커는 존재하지 않는다.**
`ShotTraceDirector.cs:275` docstring:

> `There is deliberately no marker at the endpoint.`

13개 비교작 조사로 아이콘 형식을 기각하고 월드 변형(부서진 블록)에 맡겼다(`:277-290`).
표시되는 것은 **궤적 폴리라인 하나**이고 마지막 정점이 탄착점을 말한다(`:287`).
인테이크가 절반만 읽었다고 지적하면서 나도 절반만 읽었다. 기록으로 남긴다.

**궤적 수명** [OBSERVED] — 적 턴 화면이 "완전 공백"인지 가리는 값이다:

| # | 사실 | 근거 |
|---|---|---|
| 1 | `BeginShot`은 궤적을 지우지 않는다 (`samples`만 비움) | `:165-174`, docstring `:159-161` "the previous trace for this side stays on screen until `Seal()` replaces it" |
| 2 | `Seal`의 `Draw`는 **제자리 교체** — `t.Root == null`일 때만 생성, 이후 `Apply`가 정점 덮어씀 | `:300-314`, `:344-345` |
| 3 | 파괴는 `ResetForNewMatch → Discard`뿐. 호출처는 `GameManager.StartGame` 리매치 위생 | `:126-133`, `:135-141`, docstring `:120-124` |

→ 측당 1개, 상한 2개, **한 경기 내내 지워지지 않는다.**
→ 적 턴 화면 = **HUD 판독 줄 0 + 월드 궤적 2개(정적, 지난 턴 산물)**.
   적 발사체 본체와 부서지는 블록은 실시간으로 별개 발생.

### 0.4 감사 중 기준선이 움직였다 — 기록 [OBSERVED]

감사 도중 `Assets/Scripts/GameManager.cs`의 `HudCanvas.Adopt(windText)` 한 줄이
작업 트리에서 **사라졌다**(`git diff --stat` → `1 files +0 -1`). 부모 레인의 뮤테이션
증명이다(IRC 확인). 그 상태를 그대로 읽으면 UX-001이 **open**으로 판정된다.

이 문서는 **HEAD를 기준으로 판정했다**:
```
git show HEAD:Assets/Scripts/GameManager.cs | grep -n "HudCanvas.Adopt"
  1170: HudCanvas.Adopt(turnText);
  1176: HudCanvas.Adopt(windText);
  1177: HudCanvas.Adopt(scoreText);
  1178: HudCanvas.Adopt(timerText);
```
**대장은 커밋 상태를 재는 것이고, 뮤테이션 중인 트리를 재는 것이 아니다.** 이것이 대장에
`기준 커밋` 항목이 필요한 이유이며 §2의 열 집합에 반영했다.

**후속 확인 — 뮤테이션이 바이트 동일 원복됐다** [OBSERVED, 감사 종료 시점]:
```
grep -c "HudCanvas.Adopt" Assets/Scripts/GameManager.cs   → 4
git diff --stat -- Assets/Scripts/GameManager.cs          → (출력 없음 = 차이 없음)
```
즉 **작업 트리가 판정 기준과 다시 같아졌다.** 이 절의 우려는 해소됐고, 기록은 남긴다 —
감사 중 기준선이 실제로 움직였다는 사실이 `baseline` 칸의 존재 이유이기 때문이다.
부모의 뮤테이션 증명(`Adopt` 삭제 → `:357` 빨강 확인 → 원복)이 정상 완료된 형태다.

**0.4a 두 번째 뮤테이션은 원복되지 않았고, 그것이 내 대장 파일이었다** [OBSERVED,
`ProgrammerPinPlan` 발견, 내가 원복]

F-2의 뮤테이션 증명은 **두 곳**을 건드렸다 — `zz-probe.md` 생성, 그리고
`ux-defect-list.md`의 **`상태` 열 제거**(조건 2 `EverySeverityTable_CanExpressStatus`를
빨강으로 만들기 위해). probe는 지워졌고 **열은 복원되지 않았다.**

즉 감사 종료 시점에 내 대장이 **내 문서가 "계약을 받을 수 없다"고 판정한 바로 그 상태**로
디스크에 남아 있었다(§2.2). 원복했다:
```
git checkout -- _workspace/current/qa/ux-defect-list.md
→ git diff --stat  (출력 없음)
→ :72 / :110 / :124 세 표에 `| ID | 심각도 | 상태 | …` 복원, UX-014 = **open**
```

**그리고 원복하면서 세 번째 상태를 발견했다 — 이것이 §0.5의 축을 하나 더 늘린다** [OBSERVED]:

| 상태 | `상태` 열 |
|---|---|
| **HEAD** (마지막 커밋) | **없음** |
| **index** (스테이지됨) | **있음** — `closed 2026-08-14` / `open` 값까지 |
| **작업 트리** (원복 후) | == index ✓ |

`상태` 열은 부모의 **스테이지된 미커밋 작업**이다. 그러므로:

> **`git checkout -- <file>`은 index에서 복원하고 HEAD에서 복원하지 않는다.**
> 만약 누가 "HEAD에 있다"는 서술을 근거로 명령을 `git checkout HEAD -- <file>`로
> "고쳤다면" **부모의 스테이지된 스키마 작업을 파괴했을 것이다.**

`ProgrammerPinPlan`의 **명령은 옳았고 서술이 부정확했다**("HEAD에 상태 열이 있다" → 실제로는
index). 명령이 옳았으므로 결과는 안전했지만, **근거가 틀린 옳은 명령은 다음 사람이 근거를
따라 명령을 바꿀 때 위험해진다.** §0.5가 왼쪽 끝(브랜치)과 오른쪽 끝(기준 팁)을 고쳤는데,
**git의 상태는 둘이 아니라 셋이다** — HEAD · index · 작업 트리.

> **규칙 보강: `baseline` 칸은 어느 상태를 재는지도 밝힌다.** 커밋 해시만 적으면
> 스테이지된 미커밋 작업이 판정에서 사라진다 — 지금 `상태` 열이 정확히 그 자리에 있다.

**메타 발견의 여섯 번째 사례이고 축이 또 새롭다**(§2.3): 뮤테이션은 **만든 곳이 둘이면
되돌릴 곳도 둘이다.** 한 곳만 되돌리면 남은 쪽이 **실결함과 구별되지 않는다** — Director가
부모의 `Adopt` 뮤테이션을 S1 회귀로 의심했던 그 형태이며, 이번에는 probe가 아니라
**내 대장**이 그 상태로 남았다. 증명을 위해 만든 상태는 증명이 끝나면 전수 원복해야 한다.

> **후속 — 부모가 이 창을 다시 열었고 이번엔 예고했다** [OBSERVED, 감사 종료 직전]:
> 내가 원복한 뒤 부모가 **같은 뮤테이션을 재개**했다. 이유는 정당하다 — 정리 단계에서
> `g4`/`g5` XML을 지워 **조건 1의 이빨 증거가 사라졌고**, 그러면 그 표가 증거가 아니라
> 주장이 된다(인테이크에 요구한 기준과 같다). XML 쌍을 `qa/evidence/registers/`에 영구
> 보존하기 위한 재실행이다.
>
> **차이는 예고다.** 지난 창은 조용히 열려 세 레인이 같은 착오를 했다 — 프로그래머의 결함
> 오보, 내 줄번호 1줄 허림(§5 항목 8 (마)), 디렉터의 S1 회귀 의심. 이번엔 시작·범위·예상
> 소요와 "그 사이 이 파일을 결함으로 판정하지 말라"가 함께 왔다.
>
> **그러므로 §0.4a의 교훈은 "원복을 빠뜨렸다"가 아니라 그 아래 한 겹이다**:
> **원자성은 쓰는 쪽의 성질이고, 읽는 쪽에는 창이 보인다**(ProgrammerPinPlan 정식화).
> 뮤테이션을 전수 원복하는 것으로 충분하지 않다 — **창이 열려 있는 동안 읽는 사람이 있다.**
> 예고가 그 창을 결함이 아니라 상태로 만든다. 이것이 §2.3 메타 발견의 형태이기도 하다:
> 원복(방어)을 설계하면서 **원복 전 구간**이라는 새 축을 만들었고, 예고가 그 축을 막는다.
>
> **판정에 미치는 영향 없음**: 이 문서의 S1 4건 판정은 `ux-defect-list.md`의 서식이 아니라
> **코드·씬 추적**에 근거한다(§1). 그리고 `baseline` 칸이 있는 이유가 정확히 이것이다 —
> 대장 파일이 창 안에 있어도 판정은 자기 기준선을 인용한다.
>
> **창 종료 확인 — 그리고 이번엔 증거가 디스크에 있다** [OBSERVED, 내가 검증]:
> ```
> git diff --quiet -- ux-defect-list.md          → 원복됨 (:72/:110/:124 상태 열 복귀, UX-014 = open)
> cond1-status-column-removed-RED.xml            → result=Failed, 4 total / 3 passed / 1 failed
>                                                  EverySeverityTable_CanExpressStatus 포함,
>                                                  "unevaluable blocker" 메시지 포함
> cond1-baseline-GREEN.xml                       → result=Passed, 4/4
> ```
> **조건 1의 이빨이 이제 주장이 아니라 증거다.** 빨강 실행의 메시지가 이 문서 §2.2의
> 논증을 그대로 적는다 — *"a severity with no status column means the predicate cannot be
> evaluated at all — and an unevaluable blocker reads as no blocker."* 내가 산문으로 적은 것을
> 테스트가 실패 메시지로 말한다. **그것이 문서가 코드로 옮겨진 형태다.**
>
> **부수 관측 — 파일명이 검증 중에 바뀌었다**(일곱 번째 사례): IRC 예고는
> `gate-mutation-red-status-column.xml` / `gate-mutation-green-restored.xml`였고 디스크는
> `cond1-status-column-removed-RED.xml` / `cond1-baseline-GREEN.xml`이다. `ls`가 예고된 이름을
> 보였는데 직접 접근은 실패했고, 재열거로 현재 이름을 얻었다. **§4.1의 "값이 아니라 재생성
> 명령"이 정확히 이 자리를 위한 것이다** — 이 문서가 파일명을 값으로 박았다면 지금 낡았다.
> 그래서 위 블록은 **결과 필드**(result/total/passed/failed + 포함 문자열)를 인용하고,
> 재현은 `find _workspace/current/qa/evidence/registers -type f`로 현재 이름을 얻는 것이다.

**0.4b bash grep은 위음성만이 아니다 — 그럴듯한 틀린 수를 낸다** [OBSERVED, 재현함]

§0.4a를 재현하다 가장 나쁜 형태를 만났다. `상태` 열이 어느 상태에 있는지 세려면:
```
git show HEAD:…/ux-defect-list.md | grep -c "심각도 | 상태"   → 16   (진상: 0)
git show     :…/ux-defect-list.md | grep -c "심각도 | 상태"   → 16   (진상: 3)
```
**두 버전은 228줄 vs 232줄이고 해당 문자열이 3건 다르다. 그런데 같은 16을 냈다.**
grep 도구로 두 파일을 직접 읽어서야 0과 3이 나왔다(§0.4a 표가 그 값이다).

**이것이 위음성보다 나쁜 이유**: 빈 결과는 "이상하다"는 신호를 주지만
**그럴듯한 16은 정상 산출물처럼 보인다.** `ProgrammerPinPlan`은 이 수로 하마터면
내 §0.4a 반증을 **기각할 뻔했다** — "세 상태가 같은 수니 QA가 틀렸다"가 자연스러운 답이다.

**유일한 신호는 값의 특징이 아니라 값들 사이의 관계였다**(ProgrammerPinPlan 정식화):
**증명적으로 다른 두 입력이 같은 수를 낼 이유가 없다.** 이제 카운트 오류가 세 얼굴을 가진다:

| 수 | 왜 걸렸거나 못 걸렸는가 |
|---|---|
| `10` (ProgrammerPinPlan `#if UNITY_EDITOR`) | **그럴듯해서 통과했다** — 내 5건보다 커서 확신을 줬다 |
| `0` (내 §1.3) | **빈 결과가 신호였다** — 도구를 바꾸게 만들었다 |
| `16` × 2 (이번) | **값은 그럴듯했고 관계가 신호였다** — 다른 두 상태가 같은 수 |

> **카운트 규칙 상향(§2.3)**: 교차 확인은 **부재 주장만이 아니라 모든 카운트**에 필요하다.
> bash grep은 **양방향으로** 조용히 틀린다 — 빈 결과도, 산출된 그럴듯한 수도.
> 도구 이슈에 이 사례를 추가 신고했다.

**내 산출에는 16이 들어가지 않았다** [OBSERVED]: 감사 중 나도 같은 명령을 돌려 16을 받았으나
**그것을 버그로 인지해 grep 도구로 전환했다**(§1.3에서 이미 한 번 당해 봤기 때문이다).
§0.4a의 0과 3은 grep 도구 값이다. **한 번 당한 함정이 다음에 신호가 된 사례**로 남긴다 —
그것이 §1.3에 "불일치가 예상된 결과가 되면 위음성이 신호로 바뀐다"를 적은 이유다.

### 0.5 반증 — **내 자신의 기준선 표기가 부실했다.** `HEAD`는 브랜치를 말하지 않는다 [OBSERVED]

`PmGateImpact`가 배포 계보를 보고했고, 검증하다 **내 문서의 결함**을 찾았다.
초안은 기준선을 `HEAD`라고만 적었다. **`HEAD`는 어느 브랜치인지 말하지 않는다.**

측정한 것 [OBSERVED]:
```
git rev-parse --abbrev-ref HEAD      → feature/hero-growth-series
git rev-parse --short HEAD           → 2333e93e
git merge-base --is-ancestor HEAD origin/main   → no
```
**내 감사는 `origin/main`을 재지 않았다.** 그리고 그 차이는 실재한다:
```
git cat-file -e origin/main:Assets/Scripts/SiegeForecastStrip.cs  → 있음
ls Assets/Scripts/SiegeForecastStrip.cs                            → 없음
```
`origin/main`에 있는 파일이 내 작업 트리에 **없다.**

**계보 확정** [OBSERVED]:

| 관계 | 결과 | 명령 |
|---|---|---|
| 라이브(`73f79240`)가 내 HEAD의 조상인가 | **yes** | `git merge-base --is-ancestor 73f79240 HEAD` |
| 내 HEAD ↔ 라이브 격차 | **0 뒤 / 2 앞** | `git rev-list --left-right --count 73f79240...HEAD` |
| 라이브가 `origin/main`의 조상인가 | **no** | `--is-ancestor 73f79240 origin/main` |
| `origin/main` ↔ 라이브 격차 | **14 / 5** (양방향 분기) | `git rev-list --left-right --count origin/main...73f79240` |
| `SiegeForecastStrip.cs` 라이브에 | **없음** | `git cat-file -e 73f79240:…` |

→ **라이브는 미병합 피처 브랜치에서 나갔고, 내 작업 트리는 그 라이브 계보 + 2다.**

**정정 — 내가 틀린 인과를 적었다** [OBSERVED, `PmGateImpact` 반증]:
초안은 PM 보고(`14 7`)와 내 측정(`14 5`)의 차이를 **"브랜치가 그 사이 움직였다"**로 적었다.
**그것은 틀렸다.** 두 숫자는 **동시에 참**이고 차이는 시점이 아니라 **끝점**이다:
```
git rev-list --left-right --count origin/main...HEAD       → 14  7   (끝점 = 작업 트리)
git rev-list --left-right --count origin/main...73f79240   → 14  5   (끝점 = 라이브 팁)
git rev-list --count 73f79240..HEAD                        → 2
7 − 5 = 2 ✓
```
그 2개가 정확히 미배포 델타다:
```
2333e93e docs(rules): five invariants, four of them about tests that could not fail
28226111 fix(vfx+aim): the art was never missing, and the default aim fired into our own wall
```
**즉 두 숫자는 서로 다른 질문의 답이다** — `...73f79240`은 *플레이어가 받은 것* 기준,
`...HEAD`는 *작업 트리* 기준. 그리고 이것은 **§0.5가 지적한 것과 같은 결함 클래스이며
축만 다르다**: 나는 왼쪽 끝(브랜치명)을 고쳤고 **오른쪽 끝(기준 팁)을 표기하지 않았다.**

> **규칙 추가: `shipped` 값에는 기준 팁을 명시한다.** `not-live` 단독은 값이 아니다 —
> `not-live (vs 73f79240)`처럼 끝점을 붙여야 다음 배포 후에도 그 값의 뜻이 보존된다.
> §2.3 #8을 그렇게 고쳤다.

**세 번째 축 — 로컬 `main`이 낡아서 같은 커밋이 반대 답을 낸다** [OBSERVED,
`ProgrammerPinPlan` 보고, 내가 재현]:
```
git log -1 --format="%ci %h" main         → 2026-08-12 20:15:25  6bfae546
git log -1 --format="%ci %h" origin/main  → 2026-08-14 23:18:49  873334c4
git rev-list --count main..origin/main    → 83

git merge-base --is-ancestor 0cb0efb9 main         → not-ancestor
git merge-base --is-ancestor 0cb0efb9 origin/main  → ANCESTOR

git rev-list --left-right --count main...HEAD         → 0  76
git rev-list --left-right --count origin/main...HEAD  → 14 7
```
**로컬 `main`이 83커밋·2일 낡았다.** 그래서 UX-001/002를 닫은 커밋 `0cb0efb9`가
`main` 기준으로는 **미병합**이고 `origin/main` 기준으로는 **병합됨**이다. 그리고 격차가
`0 76`(단방향, HEAD가 앞섬)과 `14 7`(양방향 분기)로 **질적으로 다르게 보인다.**

→ 같은 결함의 **세 번째 축**이다. 왼쪽 끝(브랜치명, §0.5), 오른쪽 끝(기준 팁, 위 정정),
   그리고 **ref 이름공간**(로컬 vs 원격 추적).

> **규칙 보강: 끝점은 원격 추적 ref로 표기한다. 로컬 브랜치명 금지.**
> `main`으로 구현하면 **UX-001/002 수정 자체를 미병합이라고 부른다.**
> Director F-2의 집행 코드에도 이 제약이 필요하다(`ProgrammerPinPlan`이 자기 계획에 반영).

**이것이 판정을 무효화하는가 — 아니다. 재확인했다** [OBSERVED]:

| 판정 | `origin/main`에서 | 확인 방법 |
|---|---|---|
| UX-014 분기 순서 (`:210` → `:217` → `:234`) | **바이트 동일** | `git show origin/main:…SiegeAlarmSystem.cs` → `:210/:217/:234` 같은 순서, `:236` 주석 동일 |
| UX-001/002 입양 4줄 | **존재** (`:1164`, `:1170`, `:1171`, `:1172` — 줄만 이동) | `git show origin/main:…GameManager.cs` |
| UX-003b 게이트 | **존재** (`:656-659` — 줄만 이동) | `git show origin/main:…LaunchManager.cs` |

**네 건의 판정은 두 브랜치에서 같다.** 다만 그것은 **확인해서 아는 것**이고, 초안은
확인하지 않고 `HEAD`라고만 썼다. 운이 좋았다.

**그러나 우연히 옳은 브랜치를 쟀다** — 대장이 재야 하는 것은 **플레이어가 받은 것**이므로
라이브 계보가 정답이다. `origin/main`을 쟀다면 `SiegeForecastStrip`이 있는 상태를 보고
UX-004/005를 닫았을 것이고, **라이브에는 그것이 없다.**

> **규칙: 대장의 `baseline` 칸은 브랜치명 + 짧은 해시를 쓴다. `HEAD`는 값이 아니다.**
> §2.3 #5를 그렇게 고쳤다.

**PM의 UX-004/005 관측 — 구조가 §0.1과 같다** `[INFERENCE — 커밋 메시지 근거, 코드 미추적]`:
`709695ad`(`fix(hud): the forecast strip was never in the running game`, 2026-08-14)의
메시지가 직접 적는다 — 스트립이 `Start()`에서 인트로 오버레이와 함께 철거되고
`Start()`는 두 번 돌지 않으므로 매치 시작 시 이미 사라져 있었다. **줄이 파일에 있었고 그
상태에서 실행되지 않았다** — UX-014와 정확히 같은 형태이며, 자리가 적 턴이 아니라
**매치 그 자체**다. 그 커밋은 **라이브에 없다**(`--is-ancestor 709695ad 73f79240` → no).
**여기서 UX-004/005의 status를 확정하지 않는다** — 코드 추적을 하지 않았고 PM 레인 산출
(`pm/gate-unblock-impact.md`)이 정본이다. 다만 이것이 §2.3에 `shipped` 칸을 넣는 근거다.

---

## 1. S1 4건 재검증 — 각 건 추적 연쇄

### 1.1 UX-001 (바람 미표시) — **closed** [OBSERVED]

부모가 의심한 지점("`rect.parent == root` 조기반환이 부모 null에서 어떻게 동작하는가",
"직렬화 필드가 비었으면 `Adopt(null)`이 조용히 반환")을 **둘 다 직접 확인했고 둘 다
결함이 아니다.**

**연쇄 (1) — 직렬화는 살아 있다:**

| # | 사실 | 근거 |
|---|---|---|
| 1 | 씬의 GameManager 컴포넌트 | `SampleScene.unity:1476` `guid: 61c21a07257e54a25a6a569b13ac6706` (= `GameManager.cs.meta:2`) |
| 2 | `windText` 필드가 **비어 있지 않다** | `:1500` `windText: {fileID: 1739190289}` — **0이 아니다** |
| 3 | 그 fileID는 `WindText` GO의 `TextMeshProUGUI` | `:3751` 컴포넌트 목록에 `1739190289`, `:3753` `m_Name: WindText`, `:3759` `--- !u!114 &1739190289` |
| 4 | GO는 활성 | `:3758` `m_IsActive: 1` |
| 5 | RectTransform 존재, **부모 없음** | `:3856` `--- !u!224 &1739190291`, `:3868` `m_Father: {fileID: 0}` |

→ **`Adopt(null)` 경로는 발생하지 않는다.** 부모의 첫 우려는 기각.

**연쇄 (2) — 호출은 무조건이다:**

`GameManager.cs:349` `Start()` → `:361` `SetupUIButtons();` (조건문 없음)
→ `:1163` `SetupUIButtons()` → `:1176` `HudCanvas.Adopt(windText);` (조건문 없음)

**연쇄 (3) — 조기반환은 부모 null에서 발동하지 않는다:**

| 줄 | 가드 | 이 케이스의 값 | 통과? |
|---|---|---|---|
| `HudCanvas.cs:114` | `if (element == null) return;` | 연쇄(1)-2로 non-null | 통과 |
| `:116` | `if (rect == null) return;` | 연쇄(1)-5로 RectTransform 존재 | 통과 |
| `:118` | `var root = Root();` | **절대 null이 아님** — 아래 | — |
| `:119` | `if (root == null \|\| rect.parent == root) return;` | `root` non-null; `rect.parent`는 **null**, `root`는 non-null → `null == root`는 **false** | **통과** |
| `:127` | `rect.SetParent(root, false);` | **실행됨** | — |

`Root()`가 null이 아닌 이유 [OBSERVED]:
`HudCanvas.cs:90` `Root() => MobileSafeArea.GetContentRoot(Resolve())`.
`Resolve()`(`:51-84`)는 캔버스가 없으면 **만든다**(`:65-66`) → 절대 null 반환 없음.
`GetContentRoot`(`MobileSafeArea.cs:17-32`)는 `canvas == null`일 때만 null(`:19`)이고,
그 외에는 찾거나(`:22`) **만든다**(`:24-32`).

> **판정 근거의 핵심**: `:119`의 조기반환은 **멱등성 가드**다(이미 입양된 것을 다시 옮기지
> 않음). 부모 없는 rect에서는 발동하지 않는다. 부모의 두 우려 모두 코드로 기각된다.

**회귀 방어**: **미커밋 핀 2건 → 커밋·통과 확인 중** (§4.1). 확정하지 않는다.

### 1.2 UX-002 (점수 미표시) — **closed** [OBSERVED]

UX-001과 동형. 연쇄만 다르다:

| # | 사실 | 근거 |
|---|---|---|
| 1 | `scoreText` 필드 비어 있지 않음 | `SampleScene.unity:1501` `scoreText: {fileID: 835917193}` |
| 2 | `ScoreText` GO의 TMP 컴포넌트 | `:2575`, `:2577` `m_Name: ScoreText`, `:2583` `--- !u!114 &835917193` |
| 3 | GO 활성 | `:2582` `m_IsActive: 1` |
| 4 | RectTransform 부모 없음 | `:2680` `--- !u!224 &835917195`, `:2692` `m_Father: {fileID: 0}` |
| 5 | 무조건 입양 | `GameManager.cs:1177` `HudCanvas.Adopt(scoreText);` |

`Adopt` 가드 통과는 §1.1 연쇄(3)과 동일.
값 갱신 경로도 살아 있다 — `:2438` `scoreText.text = $"SIEGE SCORE  {playerScore} - {enemyScore}"`.

**회귀 방어**: UX-001과 동일 — 미커밋 핀 2건, 확정 보류.

### 1.3 UX-003 (적 턴 거짓 지시 2개) — **closed, 단 두 지시를 분리 판정** [OBSERVED]

원문(`ux-defect-list.md:72`)은 **지시 2개**를 하나의 ID로 묶었다. 인테이크는 지시 1의
수정만 인용했다. **둘 다 독립 확인했고 둘 다 닫혔다** — 다만 방어 상태가 다르다.

**지시 1 — 플로우 스트립 `"클릭: 벽돌 예약"`: closed**

| # | 사실 | 근거 |
|---|---|---|
| 1 | 단언이 **질의로 교체됨** | `SiegeAlarmSystem.cs:228` `if (BrickPlacementRules.DesignationOpen(gm.EnforcesOneShotTurns, true, deployArmed))` |
| 2 | 규칙이 원샷 모드에서 창을 닫는다 | `SiegeTactics.cs:65` `if (enforcesOneShotTurns) return false;` |
| 3 | 출하 기본값이 원샷 | `GameManager.cs:144` `public bool enforceOneShotTurns = true;` |
| 4 | 노출 프로퍼티 | `:184` `public bool EnforcesOneShotTurns => enforceOneShotTurns;` |
| 5 | 입력측도 같은 규칙을 묻는다 | `BrickPlacementController.cs:113` 동일 호출 → 문구와 규칙이 어긋날 수 없다 |

→ 출하 설정에서 `DesignationOpen`은 **false** → `:230`이 실행되지 않음 → 문구 부재.
설계 의도는 docstring `SiegeTactics.cs:56-58`이 적는다 — "문구를 지우지 않고 술어로 만든
것은 기능이 재활성되면 안내가 **스스로 돌아오게** 하기 위함".

**방어 있음** [OBSERVED]: `Assets/Tests/EditMode/ShotReadbackTests.cs:420-444` 4개 단언.
`:421-423`이 정확히 이 케이스를 고정한다 — `enforcesOneShotTurns: true` → `IsFalse`,
주석이 "this is the case the HUD was lying about".

**지시 2 — `controlGuideText` `"푸른 링 드래그 → 발사"`: closed, 방어 없음**

| # | 사실 | 근거 |
|---|---|---|
| 1 | 가시성이 **입력 게이트와 동일 술어**로 묶임 | `LaunchManager.cs:731` `bool guidanceIsTrue = canAim \|\| deployArmed;` → `:734` `SetActive(guidanceIsTrue)` |
| 2 | `canAim`은 플레이어 턴만 참 | `:666-668` `currentState == GameState.PlayerTurn && IsPlayerTurn` |
| 3 | 주석이 UX-003을 이름으로 인용 | `:723` `// UX-003b: this line reads ... it used to stay on screen through the enemy turn` |
| 4 | 두 번째 독립 경로 | `:651-656` `OnDisable()` → 활성이면 `SetActive(false)` |
| 5 | 컴포넌트 비활성 구간 확인 | `GameManager.cs:2284` `lm.enabled = false` (정착 대기 진입) → `:2341` `lm.enabled = true` → `:2343` `EndTurn()`. **적 턴 동안 `enabled == true`이므로 `Update`가 돌고 `:731` 게이트가 매 프레임 작동한다** |

→ 적 턴에 `canAim == false`, `deployArmed == false` → `guidanceIsTrue == false` → 숨김.

**방어 없음 — 단 근거를 정정한다** [OBSERVED]:

초안은 *"`grep -rln "controlGuideText\|guidanceIsTrue\|ControlGuide" Assets/Tests/` → **0건**"*
이라고 적었다. **그 0건은 거짓이다.** `DirectorArbitration`이 이 저장소에서 bash grep이 빈
결과를 주는 버그를 보고했고, 같은 패턴을 grep 도구로 다시 돌리자 **5개 파일**이 나왔다:

| 파일 | 무엇을 단언하는가 | 가시성 게이트를 재는가 |
|---|---|---|
| `PreviewParityRegressionTests.cs:219-222` | 캐넌 가이드 **문구**(`배치`/`설치` 포함, `드래그` 불포함) | **아니다** — 내용 |
| `ProductionPathRegressionTests.cs:271` | 픽스처 배선만 | 아니다 |
| `VisibilitySpecAssetTests.cs:60` | 주석 한 줄 | 아니다 |
| `HudCanvasContractTests.cs:302, :306-307` | 존재 + `HudCanvas.Root` 부모 | **아니다** — 존재·부모 |
| `RuntimeReliabilityRegressionTests.cs:1039-1052` | 외곽선 폭·색 | **아니다** — 스타일 |

그리고 가시성 자체를 재는 것은 **정말 없다.** 부재 주장이므로 3항 형식으로 적는다
(§2.3 카운트 규칙, `ProgrammerPinPlan` 형식):

```
# 도구: grep 도구. bash 금지 — 이 저장소에서 조용히 빈 결과를 준다(§2.3, W-14).
#       bash로 재현하면 0건이 나오지만 그것은 위음성이며 예측된 결과다.
# 범위: Assets/Tests  (Assets/Scripts 아님 — 여기서 찾는 것은 "고정하는 테스트"다)
# 패턴: controlGuideText\.gameObject | controlGuideText.*activeSelf | activeSelf.*[Gg]uide
#       activeSelf를 반드시 포함해야 한다. 심볼 이름만으로 훑으면 문구·외곽선을 재는
#       테스트 5개가 잡혀 "방어 있음"으로 오독된다 — 그것이 초안의 오류였다.
→ No matches found
```

**패턴 줄에 "왜 위음성/위양성이었는지"를 적는 것이 재현 가능성을 완성한다**
(`ProgrammerPinPlan` 권고, 채택): 다음 사람이 bash로 재현해 0건을 얻어도 **그 0건이 예측된
결과**가 되고, 그러면 **위음성이 신호로 바뀐다.**

→ **결론은 유지된다** — `:731` 게이트를 고정하는 것은 없고, 그 한 줄이 `true`로 바뀌거나
   게이트가 지워지면 UX-003b가 조용히 재발하며 스위트는 녹색이다. **그러나 초안의 근거는
   틀렸다**: "테스트가 이 심볼을 언급하지 않는다"가 아니라 **"언급하는 5개가 전부 다른 것을
   잰다"**가 참이다. 후자가 더 나쁜 사실이다 — §2.3의 관측층 논증과 같은 형태이고,
   여기서는 층이 아니라 **재는 대상**이 어긋난다.

> **이것이 내 네 번째 자기 오류다** (§5 항목 8). 그리고 인테이크의
> `grep … Assets/Tests/ = 0건`을 §4.1에서 "부정확하다"고 정정한 문서가 **같은 도구로 같은
> 오류를 저질렀다.** 도구 이슈는 보고했다.

> **잔여 관측 — 1프레임 점멸 가능성** `[INFERENCE]`
> `SetSelectedUnit` 경로가 `controlGuideText.gameObject.SetActive(true)`를 무조건 실행하고
> (`LaunchManager.cs:401-404`), 이 경로는 매 턴 호출된다
> (`GameManager.cs:2172` `LaunchManagerRef?.SetSelectedUnit(selectedUnitPrefab)`).
> 적 턴 시작 시 켜졌다가 다음 `Update`의 `:734`가 끄면 **최대 1프레임 노출**이다.
> **[확인 불가]** — 프레임 단위 관측은 런타임 캡처가 필요하고 이 감사는 코드 추적만 했다.
> S1 판정에 영향 없음(1프레임은 "적 턴 내내 거짓 지시"가 아니다). 신규 결함으로 등재할
> 값이 되려면 캡처가 선행해야 한다.

### 1.4 UX-014 (적 턴 109.7초 공백) — **OPEN / S1 (Director 확정)**

§0.1이 본문이다. 요약 연쇄:

| # | 사실 | 근거 |
|---|---|---|
| 1 | 판독 줄 분기가 적 턴 분기 **뒤**에 있다 | `SiegeAlarmSystem.cs:217` → `:234` (단일 if/else-if 사슬 `:210-250`) |
| 2 | 코드 주석이 플레이어 턴 장치임을 명시 | `:236` |
| 3 | `LatestLine`은 적 턴에 **비지 않는다** | `ShotTraceDirector.cs:246-260` `Seal()`, 초기화는 `:129` `ResetForNewMatch`만 |
| 4 | 월드 궤적은 적 턴에 살아 있다 | §0.3 표 |
| 5 | 원문 측정값은 무효화되지 않았다 | `ux-defect-list.md:122` "활성 버튼 0개, 유효 입력 0개"; 시간 근거 `idle-time-measurement.md:275` `21.4 × 5.12 = 109.7` / `34.1 %` — **재확인함** |

**Director 판정 (`production/decision-log.md`, 2026-08-18)** — 이 문서의 초안은 status를
`pending-verdict`로 두었고, **판정이 도착했으므로 갱신한다.** 계약(`quality-gates.md`)이
*"QA owns measurement; the director owns the verdict"*로 소유권을 나누므로 QA는 측정을 싣고
판정은 인용한다:

| 항목 | 판정 | 근거 |
|---|---|---|
| status | **open** | 처방이 구현됐고 측정된 자리에 없다 |
| severity | **S1 유지** | 심각도가 붙은 측정("버튼 0·입력 0")이 무효화되지 않았다 |
| 면제 | **거절** | G4 임계값이 *"0 unresolved readability complaints (S1/S2)"*로 면제 대상 클래스를 **명명**하므로 순환. 신설 규칙: **임계값이 면제 대상을 명명하는 게이트에는 면제를 발행할 수 없다** |
| 부수 | 만료일을 쓰려면 적 턴을 측정해야 하는데 캡처가 없다(UX-015) | §3.2 |

→ **차단은 헛것이 아니었다.** 인테이크의 원 가설("문서만 낡았으므로 착오")이 틀렸다.
**심각도 역전**: S2(UX-015)가 S1(UX-014)의 해제 경로를 막고 있다.

**Designer 분할 권고 (미채택, 기록)** — `design/visibility-closure-verdict.md`는 UX-014가
두 주장을 묶었다며 분할을 권고한다: (i) 화면 정보량(상태5+거짓2) → 닫힘, (ii) 활성 버튼 0·
유효 입력 0 → 미착수, S2 재등록. 그리고 `:234`가 플레이어 턴인 것은 **버그가 아니라 처방
준수**라고 논증한다 — Worms 원문이 `when all movement on the battlefield has ceased`이고
적 턴에는 아직 움직이는 중이라 판독할 완료 사실이 없다는 것. 그 논증은 타당하고, 내 §0.1의
"측정된 자리에 안 들어갔다"보다 **"측정된 자리는 판독이 갈 자리가 아니었다"**가 정확하다.
**그러나 status/severity는 Director 소관이고 위 판정이 S1 open으로 확정했다.** 분할은
구조 제안으로 남기며, 채택되면 `superseded → UX-014a/b` 형태로 등재한다.

**방어 없음**: `grep -rn "LatestLine" Assets/Tests/` → 8파일 매치이나 **적 턴 부재/존재를
단언하는 것은 0건**. `ShotReadbackLiveSceneTests.cs:112-122`는 플레이어 샷 후 비지 않음을
단언하고, `ShotReachabilityProbe.cs:308` 주석은 "the 8s wait lets the AI take its turn, and
its shot seals over LatestLine"이라 적어 **적 턴 덮어쓰기를 진단으로만** 다룬다.

### 1.5 재검증 집계

| ID | 인테이크 판정 | **본 감사 판정** | 근거 성격 | 회귀 방어 |
|---|---|---|---|---|
| UX-001 | closed | **closed** (유지) | 코드 연쇄 5+3단 | 미커밋 핀 2건 → 확인 중 |
| UX-002 | closed | **closed** (유지) | 코드 연쇄 5+3단 | 미커밋 핀 2건 → 확인 중 |
| UX-003 | closed | **closed** (유지, 2지시 분리) | 지시1 규칙질의 / 지시2 게이트+OnDisable | 지시1 **있음** (`ShotReadbackTests.cs:420-444`) / 지시2 **없음** |
| UX-014 | closed (처방 변경) | **OPEN / S1** — 반증, Director 확정 | 분기 순서 `:217` 선점 | 없음 |

**4건 중 3건 closed, 1건 OPEN.** 계약 *"Any open S1 defect blocks every gate"*가
**실제로 걸린다.**

### 1.6 신규 결함 후보 2건 — 이 감사에서 드러났고 두 대장 어디에도 없다

심각도는 제안값이며 **Director 소관**이다(§1.4의 소유권 규칙과 동일).

#### 후보 A — 플레이어 자기 샷의 판독이 화면에 도달하지 않는다 (제안 `UX-018`, S2)

`DesignerVisibilityCheck` 발견. **독립 검증했고 유지된다** [OBSERVED].

`SiegeAlarmSystem.cs:241`의 `LatestLineByPlayer ? 파란색` 분기가 **정상 플레이에서
도달 불가**다. 연쇄:

| # | 사실 | 근거 |
|---|---|---|
| 1 | `Seal()` 호출처는 **한 곳** | `GameManager.cs:2339` (`grep ShotTraceDirector.Seal` → 1건) |
| 2 | 그 직후 동기적으로 턴이 넘어간다 | `:2341` `lm.enabled = true` → `:2342` `isResolvingTurn = false` → `:2343` `EndTurn()` |
| 3 | `EndTurn`이 **무조건** 교대 | `:2366` `isPlayerTurn = !isPlayerTurn` |
| 4 | 양측이 같은 경로로 봉인 | `Seal`은 `WaitAndEndTurn`(`:2281`) 안, 그 호출처는 `OnUnitLaunched`(`:2263`) **한 곳**이며 플레이어·AI 공용 (`LaunchManager.cs:1198` / `SimpleAI.cs:114`가 같은 것을 부른다) |
| 5 | 따라서 내 샷 봉인 직후는 **항상 적 턴** → `:217`이 선점 → 판독 미표시 | §0.1 |
| 6 | 적 샷이 `Seal`에서 `LatestLine`을 덮은 뒤에야 내 턴이 온다 | `:260` `LatestLineByPlayer = shotByPlayer` |

→ 내 턴에 `:234`가 실행될 때 `LatestLineByPlayer`는 **항상 false** → 주황 분기만 실행.

**"죽은 코드"는 틀린 문안이다** — `PmGateImpact`·`DesignerVisibilityCheck` 양쪽이 내 초안
헤더를 반증했다. 초안은 *"적 턴이 봉인 없이 끝나는 경로가 4개"*라고 적었는데 **표 자신이
그것을 부정한다**: 경로 1·2·4는 `Seal()`에 **도달하고 no-op일 뿐**이다. 표 내용은 옳았고
**헤더가 틀렸다.** 정확한 구분은 세 갈래다 [OBSERVED]:

| # | 경로 | `Seal()` 도달? | 결과 | 내 파란 줄 |
|---|---|---|---|---|
| 1 | `SimpleAI.cs:43` `unitPrefabs` 비었음 → `OnUnitLaunched(null)` | **도달** — `GameManager.cs:2268`이 null에도 무조건 `StartCoroutine(WaitAndEndTurn(unit))`. `:2265`의 `unit != null`은 `activeUnits.Add`만 가린다 | `shotOpen == false` → `ShotTraceDirector.cs:248` 조기반환 → **`LatestLine` 손대지 않음** | **살아남아 표시된다** |
| 2 | `SimpleAI.cs:67` `prefab == null` | **도달** (동일) | 동일 | **살아남는다** |
| 4 | 프리팹에 `UnitController` 없음 → `SimpleAI.cs:101` `if (unit != null)`가 `Launch()`를 가린다 → `BeginShot`(`UnitController.cs:543`) 미호출 | **도달** — `:114 OnUnitLaunched(unit)`은 `unit == null`에도 **무조건** 실행 | `shotOpen`이 애초에 켜지지 않음(`:167`은 `BeginShot`에서만) | **살아남는다** |
| 3 | `SimpleAI.cs:94` `!TryCommitTurnShot()` → 맨 `yield break` | **미도달** — `OnUnitLaunched`조차 호출 안 함 | `WaitAndEndTurn`이 시작되지 않음 | **다른 종류** — 턴 자체가 진행되지 않는다 |

> **경로 수 이견을 여기서 종결한다** [OBSERVED]. 세 레인이 다른 수를 냈다 —
> 내 초안 4, `DesignerVisibilityCheck` 3(도달), `ProgrammerPinPlan` 2(도달).
> **차이는 경로 4다.** `SimpleAI.cs:96-114`를 직접 읽어 확정했다:
> `:101`이 `if (unit != null) { … unit.Launch(velocity); … }`로 `Launch()`를 가리지만
> `:114 GameManager.Instance?.OnUnitLaunched(unit)`은 **가드 밖**이다. 따라서
> `UnitController` 없는 프리팹은 `BeginShot`을 못 부르고도 `Seal()`에 도달해 no-op이 된다.
> → **도달 경로 3개(1·2·4), 미도달 1개(3).** ProgrammerPinPlan이 명시적 `yield break`만
> 세어 4를 놓쳤고, Designer의 3이 맞다. 단 경로 4는 `AutomaticProjectilePrefab`에
> `UnitController`가 없어야 하므로 1·2와 같은 오설정 등급이다.

> **추가 관측 — 파란 줄은 두 번째 플레이어 턴부터다** (`ProgrammerPinPlan` 보고, 추적 일치)
> `[INFERENCE]`: 첫 플레이어 턴엔 `LatestLine`이 비어 `:245 else`가 스트립을 숨긴다(`:248`).
> 결함 상태에서도 파란색이 보이려면 (a) 플레이어가 쏘고 봉인 → (b) 적이 발사 실패로 no-op
> → (c) 플레이어 턴 복귀, 즉 **최소 2번째 플레이어 턴**이다. 실행으로 확인하지 않았다.

**Director 판정 (`D-2026-08-18-L`)**: **UX-018 = open / S2.** 등급 근거를 인용한다 —
월드 궤적이 자기 샷을 계속 그리므로 정보가 0이 아니라 **열화**다(S1 아님). 그러나 판독 줄은
`visibility-spec-v2` §3-R3이 처방한 장치이고 그 절반이 도달하지 않는다(S3 아님).
**그리고 이 발견이 Director의 선행 판정 `D-H`("UX-014a = closed")를 철회시켰다** — 판독이
플레이어 턴에 착지한 것까지 확인하고 **그 판독이 누구 것인지** 확인하지 않았기 때문이다.

**등급 규칙 신설 (Director, UX-018과 UX-019에 다른 답을 주는 근거)**:

> **구조적 결함에는 구조적 근거로 등급을 매기고, 행동적 해악에는 행동 측정을 요구한다.**

UX-018은 "분기가 도달 불가"로 **코드 구조**에서 판정된다 — UX-001의 "Canvas 조상 없음"과
같은 종류이고 이 대장이 그 근거로 S1을 매겨 왔다. UX-019는 "플레이어가 그 숫자로 보정을
배워서 배신당한다"로 **플레이어 행동에 대한 주장**이라 구조로 판정할 수 없다 →
**등급 미부여**. 이 규칙을 적지 않으면 두 건의 다른 처우가 자의적으로 보인다.

**따라서 등재 문안은 "죽은 코드"가 아니다** (Designer 제안 채택, 더 강하다):

> 플레이어가 자기 샷의 판독을 보는 **유일한 경우는 적이 발사에 실패했을 때**다.
> 정상 출하 흐름에서는 도달 불가이고, 도달하는 경로는 전부 결함 경로(프리팹 누락·턴 게이트
> 거부)다. 즉 **판독이 뜨는 것 자체가 이상 신호**다.

이 문안이 "절대적 죽은 코드는 아니다"를 흡수하면서 심각성을 낮추지 않는다 — 오히려 더
나쁜 사실을 적는다. 그리고 경로 3을 1·2·4와 **한 묶음으로 적지 않는다**: 안 그러면
나중에 "94에서도 파란 줄이 뜨는가"로 다시 갈린다.

**한계 [확인 불가]**: 세 경로를 **실행해보지 않았다.** 도달 판정은 호출 그래프 추적
(`:2268` 무조건 시작 + `:248` 조기반환의 결합)이며 파란 줄이 실제로 뜨는 것을 재현하지
않았다. Designer도 같은 한계를 자기 §5에 박았다.

**테스트가 이것을 이미 알고 있다** [OBSERVED]:
- `ShotReachabilityProbe.cs:308` 저자 자백 — "The 8s wait lets the AI take its turn, and its
  shot seals over LatestLine."
- `ShotReadbackLiveSceneTests.cs:169` `ReadbackLine_IsDisplayedOnTheStripDuringThePlayerTurn`은
  `:184` `BeginShot(false, …)` — **적 샷만** 봉인해 띄운다. 내 샷 경로는 고정이 없다.
- `:121-122`가 `LatestLineByPlayer == true`를 단언하지만, 그것은 **봉인 시점의
  `ShotTraceDirector` 상태**이고 `EndTurn` 이후 스트립 표시가 아니다. 다른 계약이다.

#### 후보 B — 표시된 바람 숫자가 배럴 턴마다 25% 거짓이 된다 (제안 `UX-019`, S2)

`DesignerVisibilityCheck` 발견. **질량 부분을 독립 검증했고 유지된다** [OBSERVED].

| # | 사실 | 근거 |
|---|---|---|
| 1 | 바람 가속은 질량으로 나눈다 | `UnitController.cs:37` `windForce / Mathf.Max(MinRuntimeMass, mass)` |
| 2 | 런타임 질량은 0.35배 스케일 | `:22` `RuntimeMassScale = 0.35f`, 적용 `:224` |
| 3 | Knight/Archer 원본 질량 **1.0** | `Knight.prefab:50`, `Archer.prefab:50` `m_Mass: 1` |
| 4 | ExplosiveBarrel 원본 질량 **0.8** | `ExplosiveBarrel.prefab:147` `m_Mass: 0.8` |
| 5 | 따라서 런타임 0.35 vs 0.28 → 가속비 **1.25배** | 산수: `1/0.28 ÷ 1/0.35 = 1.25` |
| 6 | **하한이 물리지 않는다** — Designer가 남긴 미확인 항목 | `max(0.15, 0.35) = 0.35`, `max(0.15, 0.28) = 0.28`. 둘 다 `MinRuntimeMass = 0.15`(`:23`) 위이므로 **비율 1.25가 런타임에도 보존된다.** `SimpleAI.cs:74`가 같은 clamp를 쓰고 `UnitController.cs:224`도 동일 |

→ 같은 `"WIND >>> 4.0"` 표시에서 배럴이 **25% 더 밀린다.**
발사체는 3턴 강제 순환이므로(`OneShotSiegeRules`) **3턴 중 1턴**이 표시값을 배반한다.

**이 결함이 지금 생긴 이유**: UX-001이 닫히기 전에는 숫자가 안 보였으므로 무해했다.
**바람을 보이게 만든 수정이 이 결함을 활성화했다** — 플레이어가 이제 그 숫자로 보정을
배우고 3턴마다 배신당한다. `[INFERENCE — 학습 행동은 플레이테스트 미측정]`

**두 오차가 같은 방향이다** (`DesignerVisibilityCheck` 추가, 게이트 코드 확인) [OBSERVED]:
바람은 발사 원점 반경 안에서만 작용한다 — `UnitController.cs:35`
`if ((position - windOrigin).sqrMagnitude > windRadius * windRadius) return Vector2.zero;`,
반경 값은 `GameManager.cs:117`. 따라서 표시된 숫자는 **크기도 25% 틀리고(배럴 턴) 적용
구간도 과대표현한다** — 두 오차가 같은 방향으로 겹친다. 비행 중 몇 %에 걸리는지는
Designer가 턴 43에서 30%로 계산했고 **내가 재현하지 않았다** `[확인 불가]`.

**Director 판정 — 등급 미부여 (W-8 유지)**: 이유가 명시적이다.

> 구조적 결함에는 구조적 근거로 등급을 매기고, **행동적 해악에는 행동 측정을 요구한다.**

UX-019의 해악 주장은 "플레이어가 그 숫자로 보정을 배워서 배신당한다"이고 이것은
**플레이어 행동에 대한 주장**이다. 질량비 1.25는 구조적으로 확정됐으나(위 표 1~6)
그것이 곧 해악의 크기는 아니다. 플레이테스트 측정 전까지 등급을 매기지 않는다.
**대조**: UX-018은 "분기가 도달 불가"로 코드 구조에서 판정되므로 S2가 부여됐다.

**미검증 (Designer 소관, 내가 재지 않았다)** `[확인 불가]`:
windCap 5.22 vs 문서 6.5, 호박색 임계 3.5의 턴 18 이전 불가능성, `UpdateWind`가
라운드당 2회 재추첨. 전부 상수 유도이며 Designer 레인의 산출이다. **여기서는 등재하지
않는다** — 내 대장은 검증한 것만 싣는다. Designer 문서가 정본이다.

---

## 2. 대장 구조 — 역할 분할과 열 집합

### 2.1 판정: 합치지 말고 **역할을 나눈다.** 단 교차 참조를 필수화한다

합치면 안 되는 이유 [OBSERVED — 두 문서의 실제 성격에서]:

| | `defect-register.md` (D-계열) | `ux-defect-list.md` (UX-계열) |
|---|---|---|
| 단위 | **실패한 실행** — 테스트·빌드·라이브 | **감사 소견** — 좌표 계산·경로 추적 |
| 발견 방식 | 스위트가 빨강이 됨 | 사람이 읽고 계산함 |
| 증거 형태 | XML / 로그 / 스크린샷 | `파일:줄` + 좌표 산수 |
| 닫는 방법 | 그 테스트가 녹색 | 코드 경로가 바뀜 + (필요시) 새 테스트 |
| 재발 감지 | 스위트가 자동 | **아무도 안 봄** ← 이번 사이클의 결함 |
| 현재 상태 | D-001~D-017, status 있음, 1건 open(D-016) | UX-001~UX-017(006 결번), **status 없음** |

두 대장은 **관측 도구가 다르다.** 합치면 "S3 간헐 실패 테스트"와 "S3 셰브런 4px 겹침"이
같은 열에 서고 정렬 기준이 사라진다. `defect-register.md:21`(D-016)의 상세도와
`ux-defect-list.md:110`(UX-009)의 상세도는 같은 표에 들어갈 형태가 아니다.

**그러나 §0의 사고가 재발한다** — 계약(`quality-gates.md` Blocking rules)은
`"Any open S1 defect blocks every gate"`라고만 적고 **어느 대장인지 말하지 않는다.**
두 대장이 서로소면 게이트 심사자가 한쪽만 보고 통과시킬 수 있다.

**따라서 분할 + 강제 교차 참조:**

```
qa/defect-register.md   = 기술/회귀 결함 (D-계열).   정본: 테스트 실행 결과
qa/ux-defect-list.md    = 가시성/UX 결함 (UX-계열). 정본: 코드 경로 + 좌표
qa/register-index.md    = S1/S2 전수 롤업 (신규).   정본: 위 둘의 status 열
```

`register-index.md`는 **결함 내용을 복제하지 않는다.** ID·심각도·status·소속 대장만
싣는다. 게이트 심사자가 보는 단일 문서가 이것이고, 두 대장 중 하나에라도 open S1이
있으면 여기서 즉시 보인다.

### 2.2 status 열이 없는 대장은 계약을 받을 수 없다 — 그리고 부재는 open이다

**명시한다**: 계약이 `open` S1을 차단 조건으로 삼는데 대장에 `status` 열이 없으면,
**그 대장은 계약의 입력이 될 수 없다.** `ux-defect-list.md`의 결함표 열은
`ID / 심각도 / 증상 / 근거 / 제안`(`:68`, `:106`, `:120`)이고 `status`가 없다.
그러므로 지금까지 게이트 심사는 **UX 대장을 읽을 수 없었고**, 4건이 open인지 closed인지
모르는 상태로 8개 게이트를 판정해 왔다. 이것이 §0을 가능하게 한 구조적 원인이다.

**Director 판정 — 부재의 기본값 (`production/decision-log.md`)**: 이 구멍은 대장의 태만이
아니라 **계약이 자기 패턴을 한 곳에만 적용한 자리**다. 판정 근거는 비대칭이다 —
계약은 바로 옆 줄에서 *"Missing evidence path = FAIL"*로 **증거 부재의 기본값을 이미
골랐고**, status 부재는 고르지 않았다.

> **신설 규칙: 심각도가 있고 status가 없으면 `open`으로 읽는다.**

**소급 결과** [OBSERVED — 규칙을 `ux-defect-list.md:202-208`에 적용]: 오늘까지
**UX 16건 전부 open**이었다. 그러므로 인테이크의 발단 문장("그 4건이 open이면 모든 게이트가
이미 차단이다")은 추측이 아니라 **정확한 계약 독해**였다. 이 감사가 3건을 닫았고 1건은
실제로 open이다.

**이 규칙이 §2.3 열 집합에 미치는 영향**: `status`는 **선택 열이 아니다.** 빈 칸은 중립이
아니라 `open`이며, 그것이 S1이면 8개 게이트 전부가 차단된다. 따라서 대장에 행을 추가하는
사람은 status를 쓰지 않을 자유가 없다.

### 2.3 유효한 열 집합 (최소)

부모 요구(id / severity / status / closed-by 인용 / 고정 테스트)에 감사에서 필요가
드러난 4열을 더한다. 마지막 `shipped`는 `PmGateImpact` 제안이며 §0.5가 그 근거다.

| # | 열 | 값 규칙 | 누가 채우나 | 왜 필요한가 |
|---|---|---|---|---|
| 1 | `id` | `UX-nnn` / `D-nnn` | 사람 | 결번 유지(UX-006, `ux-defect-list.md:210-211`) |
| 2 | `severity` | S1~S4 | 사람 | 계약이 S1만 차단 |
| 3 | `status` | `open` / `closed` / `superseded → ID` / `pending-verdict` | 사람 | §2.2. **빈 칸 = `open`**. `pending-verdict`는 기제가 확인됐고 판정 주체가 다를 때(UX-014가 그랬고 판정이 도착해 `open`이 됐다) |
| 4 | `closed-by` | `파일:줄` 또는 테스트 이름. **산문 금지** | 사람 | §0.1이 산문 인용("`:234`로 표시")으로 발생했다 |
| 5 | `baseline` | **브랜치명 + 짧은 해시.** `HEAD`는 값이 아니다 | 기계 | §0.5. 초안이 `HEAD`라고만 써서 어느 브랜치를 쟀는지 문서가 말하지 못했다 |
| 6 | `regression-guard` | `테스트이름 (관측층)` — 관측층은 `순수` / `씬` / `배포` 중 하나. 또는 `없음` / `미커밋 → 확인 중` | 사람이 적고 **기계가 실재·통과 확인** | 인테이크 §40이 정확히 이 칸의 부재였다. **`closed-by`와 별개 칸** — "고쳤다"와 "재발을 막는다"는 다른 주장. **관측층은 `PmGateImpact` 제안이며 아래가 근거다** |
| 7 | `evidence-strength` | `실행` / `추적` / `대조 없음` | 사람 | §0.2. `orphan-labels.md`는 닫힌 상태만 증명하고 전이를 증명하지 않는다 |
| 8 | `shipped` | **끝점 필수.** 배포 팁의 조상이면 `커밋+브랜치 (vs <팁>)`, 아니면 `not-live (vs <팁>)`. `not-live` 단독은 값이 아니다 | **전부 기계** | §0.5. 배포는 다른 세션·다른 머신에서 일어난다 — 사람이 알 수 없는 값이므로 손으로 적으면 반드시 낡는다. **끝점 미표기가 오늘 같은 저장소에서 `14 7`과 `14 5`를 낳았다**(§0.5 정정) |
| 9 | `gate-impact` | 차단하는 게이트 ID 또는 `—` | 사람 | 심사자가 역방향으로 조회 |

**`regression-guard`에 관측층을 붙이는 이유 — 통과하는 테스트가 결함을 못 보는 자리가 있다**
[OBSERVED — 커밋 메시지 직접 확인]

`709695ad`가 자기 실패를 이렇게 적는다:

> The strip was built, committed, deployed and **reported as delivered**, and it does not exist
> in a running match. **Three pure-string tests passed the whole time, which is the same reason
> wind and score were invisible for so long: values asserted, pixels never checked.**

**두 번째 문장이 이 대장 작업에 직접 걸린다** — UX-001/002(바람·점수)와 UX-004/005를
**같은 원인**으로 묶는다. 지금 status를 채우려는 네 건이 전부 같은 계측 공백에서 나왔다.
같은 메시지가 진단 실패도 남긴다: 프로브를 `Awake`/`OnDestroy`에 붙여 측정하기 전에
**추론이 두 번 틀렸다**("`Ensure()`가 null을 돌려주지 않았고 생성 순서도 원인이 아니었다").
그 커밋이 함께 넣은 것은 라이브 씬 테스트 2건이고(`--stat` 확인: `SiegeForecastLiveSceneTests.cs`
**+80줄**), 메시지가 그 차이를 명시한다 — *"a widget frozen on turn 0 would fail while
satisfying every pure assertion."*

**같은 형태 세 건, 자리만 다르다** (Director `D-2026-08-18-K`, PM 전달):

| 결함 | 처방 | 처방이 **없는** 자리 |
|---|---|---|
| UX-014 | 사후 판독 | **제어 흐름** — `SiegeAlarmSystem.cs:217`이 `:234` 선점 |
| UX-004/005 | 예보 스트립 | **객체 수명**(`Start()` 1회) → 그리고 **배포**(main O / 라이브 X) |
| 웹툰 11장 | 임포터 수정 | **배포** (피처 브랜치에만) |

→ 세 축이 전부 "줄이 파일에 있다 ≠ 그 상태에서 실행된다"다. `regression-guard`가 이름만
   받으면 **"closed + pin 있음 + 그런데 pin이 픽셀을 안 본다"**가 표에서 보이지 않는다.
   관측층을 붙이면 보인다:

| 관측층 | 무엇을 볼 수 있나 | 이 감사의 사례 |
|---|---|---|
| `순수` | 값·규칙. **픽셀·수명·배포 못 봄** | `ShotReadbackTests.cs:420-444` (UX-003 지시1) — 유효하지만 화면을 안 본다 |
| `씬` | 실제 GameObject·Canvas 조상·활성 상태 | 부모의 `HudCanvasContractTests` 미커밋 추가분이 메우는 칸 |
| `배포` | 라이브 팁에 있는가 | 어떤 테스트도 못 잼 — `shipped` 칸(#8)이 그 자리 |

> **`씬`도 픽셀을 보장하지 않는다** (`ProgrammerPinPlan` 정정, 채택):
> `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll`은 **Canvas 조상만** 본다.
> 알파 0·화면 밖·완전 가림은 **통과한다** — 그 층은 `HudOverlapTests`/`HudFixEvidenceCapture`가
> 나눠 가진다. 따라서 UX-001/002의 정확한 값은
> `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll (씬)`이며
> **`(배포)`로 적으면 안 되고, `(씬)`이 "픽셀 확인됨"을 뜻한다고 읽어서도 안 된다.**
> `709695ad`의 자백이 반복되지 않으려면 이 칸이 정확해야 한다.

**네 번째 계층 — 조사자가 즉석에서 만든 목록** [OBSERVED, `ProgrammerPinPlan` 자기 보고]:

이 사이클은 "선언 목록을 걷는 테스트는 목록에 없는 것을 못 본다"를 세 계층으로 기록했다
(자산 선언 / 런타임 `canvas == null` skip / status 없는 대장). **네 번째가 나왔고 출처가
다르다** — 남이 선언한 목록이 아니라 **조사자 자신이 방금 만든 목록**이다.

`ProgrammerPinPlan`이 UX-018의 경로를 셀 때 *"명시적 `yield break`가 있는 줄"*이라는 목록을
만들어 그것을 돌았다. **경로 4에는 `yield break`가 없다** — `SimpleAI.cs:101`이 `Launch()`를
가리고 `:114`가 가드 밖에서 `OnUnitLaunched`를 부르는 형태다. 그래서 2를 얻었고 정답은 3이다.

**내 §1.3 오류도 같은 계층이다**: 나는 *"테스트가 이 심볼을 언급하는가"*라는 목록을 만들어
bash grep으로 돌았고, 그 목록이 **도구 위음성으로 비었다.** 둘 다 "내가 정한 기준으로 훑었고
기준 밖은 안 봤다"이며, 차이는 ProgrammerPinPlan은 기준이 좁았고 나는 **기준을 적용하는
도구가 거짓말했다**는 것이다.

> **따라서 방어는 두 겹이다**: (1) 목록을 만들지 말고 **전수 열거 + 표본 하한**을 써라
> (부모의 리플렉션 + `Assert.Greater(checkedCount, 0)`가 정확히 그 형태다 — §4.1).
> (2) 부재를 주장할 때는 **다른 도구로 교차 확인**하라. `0건`은 측정값이 아니라 주장이고,
> **어느 도구로 셌는지가 그 주장의 일부다**(`ProgrammerPinPlan` 표현, 채택).

**카운트 규칙의 최종형 — 도구 + 범위 + 패턴** [OBSERVED, `ProgrammerPinPlan` 정식화]:

세 레인이 이 회의에서 카운트를 틀렸고 **원인이 셋 다 달랐다**:

| 누가 | 무엇을 틀렸나 | 원인 | 결론은? |
|---|---|---|---|
| 나 (§1.3) | `0건` → 실제 5파일 | **도구** 위음성(bash grep) | 유지 |
| ProgrammerPinPlan | `#if UNITY_EDITOR` 10건 → 8건 | **패턴**이 느슨해 산문 주석 2건 집계 | 유지 |
| Director | `gimmickStatusText` 1건 → 3건 | **범위** — `Assets/Scripts/` vs `Assets/` | 유지 |

`gimmickStatusText`를 직접 확인했다 [OBSERVED]: `Assets/Scripts/` 범위 **1건**(선언 `:118`뿐),
`Assets/` 전체 **3건**(+ 씬 `SampleScene.unity:1508` `fileID: 0` 미배선, + 부모 테스트
docstring `:425`). **같은 심볼, 같은 도구, 다른 경로 → 다른 수.** 둘 다 옳고 둘 다 불완전하다.

> **따라서 카운트 주장은 세 가지를 함께 적어야 한다: 도구 · 범위 · 패턴.**
> 하나라도 빠지면 다른 사람이 재현할 때 다른 수를 얻고, 그 불일치가 어느 쪽 오류인지 알 수 없다.
> Director의 W-14를 **"부재 주장"에서 "모든 카운트"로 넓힐 것**을 권고했다.

**그리고 위양성이 위음성보다 조용하다** (내 관찰, ProgrammerPinPlan이 일반화):
bash가 빈 결과를 주면 "이상하다"는 신호가 있어 도구를 바꾸게 된다(ProgrammerPinPlan은 실제로
두 번 바꿨다). 그러나 **느슨한 패턴은 그럴듯하게 많은 수를 낸다** — 그의 10건은 내 5건보다
커서 오히려 "제대로 세었다"는 확신을 줬다. **틀린 방향이 확신을 주는 방향과 같았다.**
위음성은 침묵으로 신호를 주고, **위양성은 풍성함으로 신호를 지운다.**

**메타 발견 — 부분적 방어는 자기가 막지 않는 축을 그대로 남긴다** [OBSERVED,
`ProgrammerPinPlan` 정식화, 채택]

이 사이클에서 **같은 모양의 "세트"가 네 번** 나왔다. 각 세트는 두(또는 세) 항이 함께여야
계약이 닫히고, 한 항만 있으면 나머지 축의 실패가 **조용히 남는다**:

| 세트 | 한쪽만 있으면 | 나온 자리 |
|---|---|---|
| **전수 열거 + 표본 하한** | 전수만 → 필터가 표본을 비운다(`:467`이 만든 구멍) / 하한만 → 목록이 좁은 것을 못 잡는다 | §4.1 부모 테스트 |
| **이름 + 스냅숏 대응표** | 이름만 → 검수 시점을 재현 못 한다(내 초안) / 줄번호만 → 여섯 번의 이동을 못 견딘다 | §4.1 대응표 |
| **카운트의 도구 + 범위 + 패턴** | 하나라도 빠지면 남이 재현할 때 다른 수가 나오고 어느 쪽 오류인지 알 수 없다 | 위 표 3레인 |
| **대응표 + 재생성 명령** | 표만 → **표 자신이 낡는다**(ProgrammerPinPlan이 세 번 고쳤고 여섯 번째는 내가 먼저 봤다) / 명령만 → 과거 스냅숏을 재현 못 한다 | §4.1 재생성 명령 |

> **네 번 같은 모양이 나왔다는 것이 그 자체로 발견이다**:
> **부분적 방어는 자기가 막지 않는 축의 실패를 그대로 남기고, 그 남은 축이 정확히 다음
> 사람이 걸리는 자리다.**

이 사이클의 실제 사례로 확인된다 — `ProgrammerPinPlan`은 좌표계를 밝혔으나 **흔들리는 대상에
적용하지 않았고**(`GameManager.cs`는 방어, `HudCanvasContractTests.cs`는 미방어), 나는
카운트의 **축 하나(도구)만 적고** 범위·패턴을 안 적었다. 둘 다 "방어를 했다"고 믿을 수 있는
상태였고 **남은 축에서 각자 걸렸다.**

**네 번째 세트가 이 명제를 한 겹 더 깊게 만든다** (`ProgrammerPinPlan` 자기 보고):
그는 줄번호의 흔들림을 막는 방어(대응표)를 설계하면서 **그 방어 자체의 낡음을 묻지 않았다.**
즉 부분적 방어는 **자기 자신에게도 적용된다** — 방어를 설계하는 행위가 새 축을 만들고,
그 축을 묻지 않으면 한 겹 아래에서 같은 실패가 반복된다.

> **따라서 방어를 설계할 때 물어야 하는 것은 "무엇을 막는가"가 아니라
> "무엇을 막지 않는가, 그리고 그 축은 누가 막는가"다.**
> **그 질문을 방어 자신에게도 적용해야 한다** — 안 하면 방어가 낡는 축이 남는다.

**다섯 번째 사례 — 그리고 이번엔 내 스키마다** [OBSERVED, `ProgrammerPinPlan` 발견,
부모가 수정, 내가 확인]

Director의 F-2가 구현됐다(`Assets/Tests/EditMode/DefectRegisterGateTests.cs`) — §2.3의
9열 스키마와 §2.2의 "부재는 open" 규칙을 **디스크에서 집행**한다. 검수하다 내 설계의 결함이
드러났다:

`ux-defect-list.md:206-212`의 심각도 롤업 표는 **status 열이 없다.** 롤업이므로 면제되는 것이
옳다 — 그 표는 결함을 추적하지 않고 **집계**한다. 그런데 초판 게이트가 그것을 면제한 이유는
설계가 아니라 **서식**이었다:

```
셀 값:  "S1 (치명)"
초판 정규식: ^\**\s*(S[123])\s*\**$     ← 앵커가 닫혀 "(치명)" 때문에 안 걸린다
```

→ **면제가 우연이었다.** 누가 셀을 `| S1 |`로 정리하면 그 롤업이 갑자기 게이트에 걸려
   status 열을 요구받고 빨강이 된다. **서식을 고치는 사람이 게이트를 깨뜨린다.**

**부모의 수정이 옳은 층위다** [OBSERVED]: 정규식을 `^\**\s*(S[123])\b`로 열고(`:46-47`),
롤업은 **구조로** 면제한다 — `RollupCount`(`:54` `^\**\s*\d+\s*\**$`)가 "심각도 + 맨 숫자"
모양을 인식해 집계 행으로 분류한다. `:37-41`이 그 이유를 적었다:
**"Detection that depends on formatting is detection that moves when someone reformats."**

> **이것이 메타 발견의 다섯 번째 사례이고 축이 또 새롭다**: 나는 열 집합(무엇을 요구하는가)을
> 설계하면서 **면제 조건(무엇을 요구하지 않는가)을 구조로 정의하지 않았다.**
> §2.2가 "빈 칸 = open"을 정했으나 **"어떤 표가 애초에 이 규칙의 대상인가"**를 정하지 않았고,
> 그 공백을 구현이 서식으로 메웠다. 규칙을 쓸 때 **적용 대상의 경계도 규칙이다.**
>
> **`ProgrammerPinPlan`의 정식화가 더 날카롭고 이것이 메타 발견의 최종형이다**:
> **규칙에 경계가 없으면 경계는 없어지는 게 아니라 정규식이 된다.**
> 그의 "방금 만든 방어가 무엇을 새로 열었는가"와 세트다 — 전자는 **새 축**을 묻고,
> 후자는 **정하지 않은 것이 사라지지 않고 아래 층에서 결정된다**는 것을 말한다.
> 내가 §2.2에서 면제 경계를 안 정했고, 그것은 소멸하지 않고
> `^\**\s*(S[123])\s*\**$`라는 **한 줄의 정규식으로 결정됐다.**

**F-2 검수에서 확인한 것 2건 더** [OBSERVED] — 둘 다 이 사이클이 방금 고친 모양이며
`ProgrammerPinPlan`이 보고하고 부모가 이미 수정했다:

| 초판 결함 | 왜 이 사이클의 모양인가 | 수정 |
|---|---|---|
| `Assert.That(openS1, Is.Not.Null)` — `.ToList()`는 null이 안 되므로 **항상 참**. 리뷰 디렉터리가 사라지면 계약의 중심 문장이 무단언 통과 | §4.1의 **공허한 통과** 그 자체 | `Assert.That(Directory.Exists(reviewRoot), Is.True, …)` (`:298-301`) |
| PASS 판정을 **산문**으로 찾고 예외 블랙리스트(`PASS / FIX`, `가능`, `후보`…)를 들었다 — `PASS 조건`·`## G4 PASS`를 오독한다 | **선언 목록을 걷는** 계층 4 | `verdict:` 키를 **구조로** 읽는다 (`:308`) — `^\s*[-*]?\s*verdict:\s*\**\s*([A-Za-z]+)` |

세 건 모두 **집행 코드가 자기 계약을 위반한 자리**였고, 세 건 모두 이 회의가 문서에 적은
실패 모양이다. 그것이 문서와 코드가 같은 사이클에 있어야 하는 이유다.

**ProgrammerPinPlan의 `:322` skip 목록이 `순수`층의 한계를 보여준 것**이고, 부모의 추가분이
`씬`층을 메운다. 두 보고가 같은 축의 양끝이다.

**UX-004/005 등재 제한 (Director W-10, 채택)**: **"처방 구현됨"으로 등재하지 않는다.**
관측 근거가 커밋 메시지뿐이고 코드 추적을 하지 않았다. §2.4 행은 `[PM 레인 소관]`으로 두고
`regression-guard`에 `(main only)`를, `shipped`에 `not-live`를 적는다.

**`shipped`가 뜻하는 것은 `origin/main`이 아니라 라이브다** (PM 제안 채택).
근거: 결함 상태의 기준은 **플레이어가 받은 것**이다. 그리고 §0.5가 측정한 대로 두 축이
실제로 갈라져 있다 — 라이브(`73f79240`)는 `origin/main`의 조상이 **아니고** 양방향으로
`14 / 5` 분기했다. 따라서 `"main에 있나"`와 `"라이브에 있나"`는 **서로 다른 질문**이며,
대장이 둘 중 무엇을 뜻하는지 정하지 않으면 §0.5의 사고가 반복된다.

**세 칸을 분리하는 것이 이 설계의 핵심이다** — `status` / `regression-guard` / `shipped`는
독립적으로 참·거짓이 갈린다. PM이 UX-004/005에서 보고한 조합이 그 증거다:

| 축 | UX-004/005의 값 |
|---|---|
| 처방 구현 | yes (`SiegeForecastStrip` 존재) |
| 런타임 도달 | `origin/main`에서 yes, **라이브에서 no** |
| 회귀 방어 | `SiegeForecastLiveSceneTests.cs` — **main only** (내 트리에 파일 없음, §0.5) |

한 칸으로는 적을 수 없다. 지금 UX-001/002도 같은 구조다 —
`status: closed` / `regression-guard: 미커밋 → 확인 중` / `shipped: 라이브 계보에 있음`.
한 칸이었다면 "closed"가 방어까지 함의해 인테이크 §40의 경고가 표에서 사라진다.

### 2.4 UX 대장에 적을 행 (제안값)

`baseline`은 §0.5의 규칙대로 브랜치+해시. `shipped`는 **끝점 명시** — 기준 팁은 라이브
`73f79240`이다. `regression-guard`는 **테스트이름 (관측층)** 형식.

| id | sev | status | closed-by | baseline | regression-guard | evidence-strength | shipped | gate-impact |
|---|---|---|---|---|---|---|---|---|
| UX-001 | S1 | closed | `GameManager.cs:1176` `HudCanvas.Adopt(windText)`; 가드 통과 `HudCanvas.cs:114-127` | `feature/hero-growth-series@2333e93e` | **미커밋 핀 2건 (씬) → 커밋·통과 확인 중** | 추적 (대조 없음 — §0.2) | 있음 (vs `73f79240`) | — |
| UX-002 | S1 | closed | `GameManager.cs:1177` `HudCanvas.Adopt(scoreText)` | 동일 | **미커밋 핀 2건 (씬) → 확인 중** | 추적 (대조 없음) | 있음 (vs `73f79240`) | — |
| UX-003 | S1 | closed | 지시1 `SiegeAlarmSystem.cs:228` + `SiegeTactics.cs:65`; 지시2 `LaunchManager.cs:731` + `:651-656` | 동일 | 지시1 `ShotReadbackTests.cs:420-444` **(순수)** / **지시2 없음** | 추적 | 있음 (vs `73f79240`) | — |
| UX-014 | S1 | **open** | — (닫히지 않음). 기제: `SiegeAlarmSystem.cs:217`이 `:234` 선점 | 동일 (`origin/main`에서도 동일 — §0.5) | 없음 | 추적 (적 턴 캡처 부재 — UX-015) | 있음 (vs `73f79240`) | **G1~G8 전부** |
| UX-018 | **S2 (Director `D-2026-08-18-L` 확정)** | open | — . 기제: `SiegeAlarmSystem.cs:241` 파란 분기가 정상 배선에서 도달 불가 (`GameManager.cs:2339` Seal 1곳 → `:2343` → `:2366` 무조건 교대) | 동일 | 없음 | 추적 (3경로 도달 미실행 확인) | 있음 (vs `73f79240`) | G4 |
| UX-019 | **등급 미부여 (Director W-8 유지)** | open | — | 동일 | 없음 | 추적(질량비 1.25 검증) / **미검증(곡선·행동)** | 있음 (vs `73f79240`) | G2·G4 후보 |
| UX-004/005 | S2 | **[PM 레인 소관 — 확정 금지]** | `709695ad` (커밋 메시지 근거만) | — | `SiegeForecastLiveSceneTests.cs` **(씬, main only — 내 트리에 파일 없음)** | 커밋 메시지 (코드 미추적) | **`not-live` (vs `73f79240`)** | G4 |

> **UX-001/002의 `shipped: 있음`이 뜻하는 것**: 수정 커밋(`0cb0efb9`)이 라이브 팁의
> 조상이다. 내 작업 트리는 라이브 + 2이고 그 2개는 §0.5의 미배포 델타이며 이 네 건과
> 무관하다. **`regression-guard`는 그와 별개로 미커밋**이다 — 수정은 배포됐고 방어는 안 됐다.
> 세 칸이 독립이라는 것이 바로 이 형태다.

---

## 3. 게이트 재판정 — 무엇을 재돌려야 하는가

**측정하지 않았다.** 재측정 대상만 적는다.

### 3.1 확정: 재측정 가능한 게이트는 **0개다** — UX-014가 open이기 때문 [OBSERVED]

계약(`quality-gates.md` Blocking rules) 첫 줄이 `"Any open S1 defect blocks every gate"`이고,
§1.4가 UX-014를 **open / S1**으로 확정했다(Director, 면제 거절). 따라서:

> **G1~G8 전부가 차단 상태다. 재측정으로 PASS를 받을 수 있는 게이트는 없다.**

이것은 인테이크의 가설과 정반대다. 인테이크(`:15-17`)는 차단이 **문서 착오**이므로 화해가
끝나면 해제된다고 봤다. **차단은 실재했다.**

### 3.1a 그리고 차단이 풀려도 PASS는 0개다 — 각 게이트에 자기 블로커가 있다 [OBSERVED]

`gate-measurements.md:12-23`: **8개 전부 FAIL이고 각 사유가 S1과 무관하다.**

| 게이트 | 현재 판정 | FAIL 사유 (S1과 무관) | 근거 |
|---|---|---|---|
| G1 세계관 | FAIL | 문자열 전수 감사 **미실시** | `:14` |
| G2 밸런스 | FAIL | 대칭 ≥20매치 runtime 승률 **표본 부재** | `:15`, `:46` |
| G3 아키타입 | FAIL | 로테이션 **미실시** (빈 표) | `:16` |
| G4 몰입 | FAIL | 구조화 채점 8장면 **미실시** (빈 표) | `:17` |
| G5 매출 | FAIL | **pm 레인 부재** | `:18` |
| G6 운영 | FAIL | perf·rollback **미비** | `:19`, `:209-214` |
| G7 코어루프 | FAIL | `RepeatRate()` ≥20세션 **미측정** | `:20` |
| G8 참신성 | FAIL | 인상 점수 **미측정** (빈도만 통과) | `:21`, `:231` |

**통과 0 / 8**(`:23`).

→ 차단은 **상위 게이트**였고 그 아래에 각 게이트 자신의 증거 블로커가 있다. 그러므로
   S1 차단 해제는 **필요조건이지 충분조건이 아니다.** 인테이크 `:16-17`의
   "Stage 3 진행 불가"는 두 겹이다 — UX-014의 차단 **그리고** G4/G6/G1 측정의 부재.

**심각도 역전 (Director 발견, 재확인함)**: S2인 UX-015(적 턴 캡처 부재)가 S1인 UX-014의
**해제 경로를 막는다.** UX-014를 닫으려면 적 턴을 측정해야 하고, 그 캡처가 없다
(`evidence/visual/`에 `ux-4-*` 부재 — 확인함). **낮은 심각도가 높은 심각도의 선행 조건이다.**

이 사이클의 산출이 바뀐다:
**"게이트 해제"가 아니라 "게이트가 왜 닫혀 있는지 정확히 아는 것"**이 산출이다.
status 열이 없어서 지금까지는 그조차 몰랐고, §2.2의 소급 규칙에 따르면 오늘까지
**UX 16건 전부 open**이었다.

### 3.2 차단 해제를 향해 **지금 바로 착수 가능한** 작업

게이트 PASS 측정이 아니다 — **차단을 걷어내는 작업**이다. 전수 스위트 금지, 각 항목 자기 명령만.

| 우선 | 대상 | 재돌릴 것 | 왜 지금 가능한가 | 선행 조건 |
|---|---|---|---|---|
| 1 | **UX-015** (S2, 차단의 차단) | `ux-4-enemy-turn` 캡처 추가 — `VisualEvidenceCapture.cs`에 `AITurn` 대기·캡처 없음(`:249`/`:258`/`:262` 3건뿐) | `evidence/visual/`에 `ux-4-*` 부재 확인 | 없음. **여기가 임계 경로다** |
| 2 | **UX-014** (S1, 유일한 차단자) | 적 턴 입력 0의 코드 처방 → 그 뒤 재측정 | 기제는 §0.1로 확정 | 우선 1 (측정 없이 닫으면 여섯 번째 "측정 없이 결론") |
| 3 | UX-001/002 방어 확정 | 부모의 미커밋 핀 2건 — `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll`, `SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas`. `-testFilter "HudCanvasContractTests"` | 파일에 이미 있음(미커밋) | `regression-guard` 칸 확정의 유일한 경로. 통과 XML 없이 확정 금지 |
| 4 | UX-003b 방어 | §4.2의 술어 일치 단언 신규 1건 | 코드 게이트는 이미 있음(`LaunchManager.cs:731`) | 없음 |
| 5 | G4 선행 | S2 가독성 분류 판정 — **UX-007** | G4 임계값이 `0 unresolved readability complaints (S1/S2)` | Director 판정 (§3.3) |
| 6 | G6 | perf 4항 (p95 프레임 / 롱프레임 / 30분 소크 / 입력지연) | `gate-measurements.md:209-212` 전부 미측정 | `engineering/perf-budget.md` 부재 — 문서 선행 |
| 7 | G6 | `ops/rollback-runbook.md` 1회 테스트 | `:213` 미실시 | 런북 존재 확인 필요 |
| 8 | G1 | 문자열 전수 감사 | `:14` 미측정 | 없음. 착수 가능 |
| 9 | G2 | 대칭 ≥20매치 **runtime** 승률 | 수치 밴드·damage route는 확인됨(`:46`); 빠진 것은 runtime 표본뿐 | PlayMode 비결정성(D-015) — 단일 실행으로 주장 금지 |

> **우선 6~9는 차단 해제 전에는 PASS 주장에 쓸 수 없다.** 측정 자체는 가능하고 값은
> 유효하지만, UX-014가 open인 동안 그 값으로 게이트 판정을 내리면 계약 위반이다.
> 값을 쌓아 두는 것은 낭비가 아니다 — 차단이 걷히는 순간 판정이 즉시 가능해진다.

### 3.3 경고 — G4는 S1이 전부 닫혀도 열리지 않는다 [OBSERVED]

G4 임계값은 S1**과 S2**를 함께 요구한다. `ux-defect-list.md`의 S2는 5건(`:205`):
UX-004 / UX-005 / UX-007 / UX-008 / UX-015.

이 중 **가독성(readability) 성격**은 판정이 필요하다:
- **UX-007** — 크로스헤어가 `파워 60%` 글자를 관통(`:108`). **가독성 결함으로 읽힌다.**
- **UX-008** — 벽돌 패널 50px 중 14px만 보임(`:109`). 조작 불가 = 가독성인지 기능인지 경계.
- UX-004 / UX-005 / UX-015 — 정보 부재·증거 부재. 가독성 항목으로 세기 어렵다 `[INFERENCE]`.

→ **UX-007을 S2 가독성으로 세면 G4는 S1 전건 closed에도 FAIL이다.**
   이 분류는 QA 단독 판정 대상이 아니라 게이트 심사자(Director)의 판정이 필요하다.
   대장의 `gate-impact` 열(§2.3 #8)이 이 조회를 위해 존재한다.

---

## 4. 고정 제안 — 그리고 박으면 위험한 것

### 4.1 이미 존재하는 핀 — 새로 쓰지 말고 이것을 확정하라

`Assets/Tests/PlayMode/HudCanvasContractTests.cs`에 **미커밋 추가분**이 있고 그 안에 필요한
계약 2개가 이미 들어와 있다 [OBSERVED — 파일 직접 확인]:

| 테스트 | 무엇을 고정하는가 | 관측층 |
|---|---|---|
| `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll` | `canvas == null`을 **skip에서 fail로 반전** | 씬 (**Canvas 조상까지만** — 픽셀 아님) |
| `SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas` | 리플렉션으로 씬 저작 `TMP_Text` 필드 전수 → `canvas == null` → `problems.Add(… no Canvas ancestor - invisible)` | 씬 |

> **이름으로 서술하고, 재현은 대응표로 한다** [OBSERVED]: 부모가 이 파일을 편집 중이고
> 감사 중 **여섯 번** 움직였다.
>
> **초안은 줄번호를 아예 지웠고 그것도 불완전했다** (`ProgrammerPinPlan` 반증, 채택):
> 이름은 이동을 견디지만 **검수 시점을 재현하지 못한다.** 어떤 스냅숏에서 무엇을 봤는지
> 알려면 그 시점의 줄번호가 필요하다. 정답은 **이름 + 스냅숏 대응표**이며 둘 중 하나만으로는
> 불완전하다 — 이름은 이동을 견디고, 대응표는 과거를 재현한다.
>
> | 테스트 / 지점 | 초안 검수 | 2차 | **현재** |
> |---|---|---|---|
> | `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll` | `:357` | `:384` | **`:400`** |
> | `SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas` | `:399` | `:437` | **`:453`** |
> | `GameplayHudLabels_AllShareTheOneHudCanvas`의 `canvas == null` skip | `:322` | `:349` | **`:365`** |
>
> **표도 낡는다 — 그래서 재생성 명령을 함께 남긴다** (`ProgrammerPinPlan` 반증, 채택).
> 그는 대응표를 붙여 줄번호의 흔들림을 잡았으나 **표에 적은 값도 똑같이 흔들렸다** —
> 파일이 여섯 번 움직이는 동안 표를 세 번 고쳤고 여섯 번째는 내가 먼저 발견했다.
> **손으로 표를 쫓는 것은 움직이는 파일에 대해 지는 싸움이다.**
>
> ```
> # 도구: grep 도구 (bash 금지 — 이 저장소에서 조용히 빈다, §2.3 카운트 규칙)
> # 범위: Assets/Tests/PlayMode/HudCanvasContractTests.cs
> # 패턴: public IEnumerator \w+\(\)        → 테스트 앵커
> #       if \(canvas == null\) continue;   → skip 지점
> ```
> 확인함 [OBSERVED]: 이 명령이 위 `현재` 열을 그대로 재생성한다(`:400`/`:453`/`:365`).
> **표는 과거를 재현하고 명령은 현재를 생성한다.** 둘 다 필요하다 —
> 이것이 세트 구조의 **네 번째** 사례다(§2.3 메타 발견).
>
> 본문 서술은 이름으로, 과거 재현은 표로, 현재 확인은 명령으로.
> `regression-guard` 칸이 이름을 받는 이유도 같다.
> 그리고 부모의 docstring이 이 계약의 필요성을 직접 적는다 — 삭제 시 "restore the silence
> with the suite green", 그리고 `Adopt(null)`이 조용히 반환하므로 미배선 필드는 진단 줄로
> 보고한다(단언이 아니라 `Debug.Log`).

**갱신 — 부모가 감사 중 테스트를 정교화했다** [OBSERVED, `DirectorArbitration` 보고 후 직접 확인]:
`SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas`가 세 가지를 얻었다:

| 추가 | 무엇을 해결하는가 | 줄 |
|---|---|---|
| `if (!label.isActiveAndEnabled) continue;` | 닫힌 패널의 라벨(`resultText`)이 오탐을 내던 것 | `:467` |
| 미배선 필드를 `unwired` 진단 목록으로 분리 → `Debug.Log`만 | `Adopt(null)`이 조용히 반환하므로 미배선과 입양된 것이 구별 불가 — **단언이 아니라 이름을 남긴다** | `:461`, `:484-488` |
| `Assert.Greater(checkedCount, 0, …)` | **공허한 통과 방지** — 표본이 0이면 아무것도 단언하지 않은 것이다 | `:490-493` |

**세 번째가 §4.3과 같은 논증이고, 인과가 앞의 두 개와 이어져 있다** [OBSERVED,
`ProgrammerPinPlan` 정정]. 초안은 `:322` skip 진단이 이 가드를 낳았다고 추측했다.
**더 직접적인 사슬이 있다**:

1. ProgrammerPinPlan이 활성 게이트(`:467`)를 권고했다 — 닫힌 패널 라벨의 오탐을 없애려고.
2. 그 스킵이 **새 구멍을 만든다**: 활성 라벨이 0이면 `checkedCount`가 0이고 `problems`도
   비어서 **`Assert.IsEmpty(problems)`가 녹색**이다. 아무것도 단언하지 않고 통과한다.
3. 부모가 **같은 편집에서** `:490` `Assert.Greater(checkedCount, 0)`로 막았다 —
   권고에 없던 것이다.

→ **목록을 좁히는 수정은 표본을 비울 수 있고, 표본이 비면 계약이 조용히 녹색이 된다.**
   §2.3 계층 4의 또 다른 얼굴이며, 이번엔 **자기 수정이 만든 구멍을 자기가 못 본** 형태다
   (ProgrammerPinPlan 자기 보고).

> **따라서 방어 두 개는 세트다** (`ProgrammerPinPlan` 정식화, 채택):
> **전수 열거만 있으면 필터가 표본을 비우고, 표본 하한만 있으면 목록이 좁은 것을 못 잡는다.**
> 부모의 테스트가 지금 둘을 다 갖췄다 — 리플렉션 전수(`:451-456`) + 하한(`:490`).

> **Director W-15와 수렴한다**: *"합격 조건을 숫자로 쓰면 낡는다."* Director는 F-1의
> 오탐 수를 `2`로 적었다가 부모의 수정으로 `0`이 되어 두 번째로 낡았다고 기록했다.
> 세 판본에서 모두 참인 것은 **관계**였다 — *"뮤테이션이 `windText`를 `problems`에 넣고
> 원복이 빼낸다."* 이것이 §4.3 위험 4("값이 아니라 관계를 박아라")의 같은 얼굴이며,
> 내가 §4.1에서 줄번호를 지우고 **테스트 이름으로 인용한 것**과 같은 판단이다.
> **기준선이 이 감사 중 다섯 번 움직였다** — `Adopt` 삭제 → 원복 → 테스트 추가분 →
> 정교화(오탐 2건 해소) → 공허 통과 가드. 이름으로 인용한 것이 그 창을 견뎠다.

**소유권**: 부모 레인이 편집 중이다(IRC 확인). 나는 읽기만 했고 편집하지 않았다.

**인테이크 정정 — 내가 직접 재확인했다** [OBSERVED]: 인테이크 `:42`의
`grep -rln "Adopt|windText|orphan" Assets/Tests/` = **0건**은 **부정확하다.**
grep 도구로 확인한 매치(전부 주석·docstring, 단언 0건):

| 파일 | 줄 | 성격 | 부모 122줄 이전에 존재? |
|---|---|---|---|
| `HudOverlapTests.cs` | `:19` | docstring — "Adopting `windText` and `scoreText` onto the HUD canvas made them visible for the first time" | **예** — 인테이크 작성 시점에 이미 있었다 |
| `HudCanvasContractTests.cs` | `:46`, `:370`, `:430`, `:437`, `:465`, `:487` | 부모의 미커밋 +122줄 | 아니오 |

→ **단언 0건은 맞고 `0건`은 틀렸다.** `HudOverlapTests.cs:19`가 인테이크 시점에 존재했으므로
   이것은 부모의 추가로 생긴 것이 아니다(`ProgrammerPinPlan` 보고와 일치, 내가 재확인).
   그리고 그 진술은 **작성 시점엔 "단언이 없다"는 뜻으로 옳았고, 지금은 부모 자신이
   무효화했다** — `:437`이 그 단언이다.
   **§5 항목 8(라)와 같은 교훈**: `0건`은 측정값이 아니라 주장이다.

**기존 skip과의 관계** [OBSERVED]: `:322` `if (canvas == null) continue;
// not drawn at all — a separate defect, UX-001/002`는 `GameplayHudLabels_AllShareTheOneHudCanvas`
(`:310`)의 **표본 정의**로 여전히 옳다(그 테스트는 "갈림"을 잰다). `:336-338`이 새 테스트의
docstring에서 그 관계를 명시적으로 적고 있으므로 **두 테스트는 보완**이다. 같은 파일에
반대 방향 두 개가 있어도 docstring이 이유를 적으면 결함이 아니다.

**QA가 요구하는 것**: `regression-guard` 칸은 **통과 XML이 나올 때까지 확정하지 않는다.**
값은 `미커밋 핀 2건 → 커밋·통과 확인 중`이다. Director 판정에 동의한다 —
통과 증거 없이 S1 closed를 확정하면 이 사이클이 기록한 "측정 없이 결론"의 반복이다.

> **F-2의 통과 XML을 이 칸의 증거로 읽지 말 것** [OBSERVED, 축 구분]:
> `cond1-*-RED/GREEN.xml`(§0.4a)은 **`DefectRegisterGateTests`**의 실행이며 내 **스키마가
> 집행된다**는 것을 증명한다. UX-001/002의 방어는 **`HudCanvasContractTests`**의
> `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll` /
> `SceneAuthoredHudLabels_AreAdoptedOntoTheHudCanvas`이고 **그 실행 증거는 아직 없다.**
>
> 두 개는 **다른 축**이다 — 하나는 "대장이 status를 표현할 수 있는가", 다른 하나는
> "씬 저작 라벨이 HUD 캔버스에 붙는가". **한 축의 증거로 다른 축을 주장하는 것이 이 사이클이
> 반복 기록한 범주 오류**이며(§0.1이 인테이크의 그 형태를 반증했다), 여기서 그것을 하면
> 여덟 번째 사례가 된다. 그래서 이 칸은 F-2 XML이 있어도 **여전히 `확인 중`이다.**

### 4.2 추가로 박을 값이 있는 계약 1건

**`controlGuideText`의 가시성이 입력 게이트와 같은 술어를 쓴다** (UX-003b, §1.3).
현재 방어 0. 고정 형태 제안:

> 적 턴(또는 `canAim == false && deployArmed == false`)에서
> `controlGuideText.gameObject.activeSelf == false`

**주의**: 문자열이 아니라 **`activeSelf`와 술어의 일치**를 단언해야 한다. 이유는 §4.3.

### 4.3 박으면 위험한 것 — 이쪽이 더 중요하다

이 사이클이 이미 한 번 경험했다: `defect-register.md:10`(D-004)에서 세 테스트가 **낡은
기대치**를 들고 통과 중인 계약과 모순됐고, 원인은 값을 박아둔 것이었다.

**위험 1 — 플로우 스트립의 분기 순서나 적 턴 문자열을 박는 것. 최상위 위험.**

`"적 턴이면 스트립이 '적 포격 준비 중'을 보인다"`를 단언하면 **UX-014의 수정을 막는다.**
§0.1이 밝힌 것은 `:217`이 `:234`를 선점한다는 **기제**이고, 그 선점의 정당성은 별개다.

**그리고 이 위험이 이 사이클에서 이미 반쯤 실현됐다** — Designer가 논증한 대로
`:234`가 플레이어 턴인 것은 **Worms 처방 준수**다(`when all movement has ceased`).
즉 지금 코드는 옳고, **적 턴에 판독을 넣는 수정이 오히려 처방 위반**일 수 있다.
그러므로 위험은 양방향이다:

| 무엇을 박으면 | 무엇이 막히는가 |
|---|---|
| "적 턴에 판독 없음" | 적 턴 판독을 넣는 수정 (설계 판정이 그리 나오면) |
| "적 턴에 판독 있음" | **현재의 옳은 구현** — 처방 준수를 빨강으로 만든다 |

→ **어느 방향도 박지 말라.** UX-014의 남은 결함은 판독 위치가 아니라 **입력 0**이므로
   (§1.4 Director 판정), 박을 값이 생기는 것은 **적 턴 인터랙션이 구현된 뒤**다.
   그때 박을 것은 "적 턴에 유효 입력 ≥1"이고 문자열이나 분기 순서가 아니다.

> **분기 순서는 결함의 원인이지 계약이 아니다.** 원인을 박으면 수정을 막는다.
> 그리고 **원인이라고 생각한 것이 실은 처방 준수였다** — 이번 건이 그 사례다.

**위험 2 — `m_Father: 0`(씬 루트)을 정상으로 박는 것.**

인테이크 `:67-69`가 씬을 고치지 않는다고 정했고 그 판단은 타당하다(입양이 정식 경로).
그러나 테스트가 `WindText.m_Father == 0`을 단언하면, 누군가 나중에 씬에서 재부모화하는
**더 단순한 수정**을 할 때 빨강이 된다. 씬 값은 **앵커의 출처**로 쓰이는 것이고
(`orphan-labels.md`의 `anchoredPosition (150,-30)`이 그것) 부모 값은 계약이 아니다.

→ 박을 것은 **"렌더된다"**(= Canvas 조상이 있다)이고, **"어떻게 렌더되게 만들었는가"**가
   아니다. 부모의 `EveryActiveHudGraphic_HasACanvasAncestorSoItIsDrawnAtAll`이 정확히 전자를 박는다. 옳은 층위다.

**위험 3 — `LatestLine`의 정확한 문자열.**

`ShotReadbackTests.cs:308`이 이미 완전 일치를 단언한다
(`"적 화약통 → 성벽 2블록 파괴 · 코어 -30"`). 판독 문구는 UX 개선의 직접 대상이고
(§0.1의 설계 판정이 여기를 건드릴 수 있다), 완전 일치 단언은 문구 개선마다 깨진다.
**이미 존재하는 핀이므로 새로 늘리지 말라는 권고**이며, 기존 것을 지우라는 뜻은 아니다
(`Compose`의 결정론은 지킬 값이 있다). 새로 추가할 때는 `StringAssert.Contains`
(같은 파일 `:296`, `:329`가 쓰는 형태)를 쓰라.

**위험 4 — 좌표 리터럴을 그대로 박는 것.**

`ux-defect-list.md:144-151`이 이미 경고하고 D-009(`defect-register.md:14`)가 실증했다.
UX-007~UX-011의 겹침을 고정할 때 `anchor 0.15` 같은 값을 박으면 레이아웃 변경마다
빨강이 된다. 박을 것은 **관계**(`패널상단 < LastStandBottom`)이고 값이 아니다.
`HudLayoutTests`가 이미 그 형태다(`:146` — const 4개끼리의 부등식 3개).

### 4.4 고정 우선순위

| 순위 | 대상 | 형태 | 관측층 | 상태 |
|---|---|---|---|---|
| 1 | 씬 저작 HUD 라벨이 Canvas 조상을 갖는다 | 존재 단언 (부모의 두 테스트, §4.1) | **씬** (Canvas 조상까지만) | **미커밋 — 커밋·통과 확인 중** |
| 2 | `controlGuideText` 가시성 == 입력 술어 | 술어 일치 단언 (§4.2) | **씬** (순수로는 못 잼) | 미작성. 방어 0 |
| 3 | `DesignationOpen` 규칙 | 이미 있음 (`ShotReadbackTests.cs:420-444`) | 순수 | **완료** |
| — | 적 턴 스트립 문구·분기 순서 | **박지 말 것 (양방향)** | — | §4.3 위험 1 |
| — | 씬 `m_Father` 값 | **박지 말 것** | — | §4.3 위험 2 |
| — | `LatestLine` 완전 일치 문자열 (신규) | **늘리지 말 것** — `Contains` 사용 | — | §4.3 위험 3 |
| — | 좌표 리터럴 | **박지 말 것** — 관계를 박아라 | — | §4.3 위험 4 |

> **관측층이 이 표에 필요한 이유**: 순위 2를 순수 단언으로 쓰면 `guidanceIsTrue` 계산은
> 검증되고 **`SetActive`가 실제로 불렸는지는 검증되지 않는다.** `709695ad`가 기록한
> *"values asserted, pixels never checked"*가 정확히 그 실패다(§2.3). 씬층이어야 한다.

**신규 후보 2건의 고정 가능성** (§1.6):

| 후보 | 지금 박을 수 있는가 | 이유 |
|---|---|---|
| UX-018 (내 샷 판독 미도달) | **아직 아니다** | 결함이 `EndTurn`의 무조건 교대(`GameManager.cs:2365`)와 판독 타이밍의 상호작용이다. 어느 쪽을 고칠지 결정되기 전에 박으면 위험 1과 같은 실패다. **박을 수 있는 것은 하나** — `LatestLineByPlayer == true`인 상태가 스트립에 도달하는 경로가 존재한다는 단언(현재는 도달 불가이므로 **지금 쓰면 빨강**이고, 그것이 결함의 정직한 표현이다). Director가 등재를 승인하면 red-first 테스트로 쓸 값이 있다 |
| UX-019 (배럴 25% 드리프트) | **부분적으로** | 박을 값은 "표시된 바람과 실제 가속의 비가 발사체 종류에 무관하다"이고, 질량 상수(`UnitController.cs:22`, 프리팹 `m_Mass`)끼리의 관계로 표현 가능하다. **단 이것은 밸런스 결정이다** — 배럴이 더 밀리는 것이 의도일 수 있으므로(무게가 가벼우니 바람에 밀린다는 물리적 정합) 고정 전에 설계 판정이 필요하다. **박으면 위험한 쪽**: "모든 발사체의 질량이 같다"를 박으면 의도적 질량 차등을 막는다 |

---

## 5. 이 감사가 확인하지 못한 것

1. **런타임 단언 없음.** 모든 판정이 코드·씬 YAML 추적이다. `windText.canvas != null`을
   실제로 돌리지 않았다 — 부모 레인이 뮤테이션 증명 중이고 배치 모드와 열린 에디터가
   프로젝트 락을 다툰다(`gate-measurements.md:292-298`가 그 사고를 기록). 부모의 통과 XML이
   이 칸을 채운다.
2. **1프레임 점멸 [확인 불가].** §1.3의 `SetSelectedUnit → SetActive(true)` 경로가 적 턴
   시작에 1프레임 노출을 만드는지는 프레임 캡처가 필요하다.
3. **적 턴 실화면 여전히 없음** [OBSERVED]. `evidence/visual/`에 `ux-4-*` 부재 —
   UX-015는 open이고 §0.1의 기제 판정도 코드 추적이다. 캡처가 나오면 §0.3의
   "궤적 2개 + 판독 0"을 육안 대조해야 한다. **이것이 UX-014 해제의 임계 경로다**(§3.1a).
4. **UX-019의 곡선 부분 [확인 불가].** 질량비 1.25배는 검증했으나 windCap 5.22 vs 문서 6.5,
   호박색 임계 3.5의 도달 가능 턴, `UpdateWind` 라운드당 2회 재추첨은 **재지 않았다.**
   Designer 레인 산출(`design/visibility-closure-verdict.md`)이 정본이다.
5. **UX-018의 학습 행동 [확인 불가].** 플레이어가 표시된 숫자로 보정을 배우는지는
   플레이테스트 측정이며 이 감사의 범위가 아니다. 코드 도달 가능성만 확정했다.
6. **S2 이하 전수 재검증 안 함.** 차단 규칙은 S1만 걸리므로 범위 밖이었다. 단 §3.3대로
   **G4는 S2 가독성 결함으로 독립 차단**되므로 UX-007의 분류는 게이트 심사 전에 필요하고,
   §3.1a대로 **UX-015가 S1의 선행 조건**이다. "S1만 보면 된다"는 이번 사이클에 반증됐다.
7. **`register-index.md` 미작성.** §2.1의 제안이며 이 문서는 설계만 담았다.
   Director의 F-2("차단 술어를 집행 가능하게, 디스크를 걸어라")가 이 자리를 코드로 채우는
   요구이며, **손으로 넣은 status 열은 손으로 낡는다**는 지적에 동의한다.
8. **내 자신의 오류 5건을 기록으로 남긴다.**
   (가) §0.3 — 존재하지 않는 "탄착 마커"를 브로드캐스트에서 인용했다. 인테이크가 절반만
   읽었다고 지적하면서 나도 절반만 읽었다.
   (나) §0.5 — 기준선을 `HEAD`라고만 적었다. `HEAD`는 브랜치를 말하지 않는다.
   (다) §0.5 정정 — (나)를 고치는 절을 쓰면서 **같은 결함의 반대 축을 반복했다.**
   끝점을 표기하지 않아 `14 7` vs `14 5`를 "브랜치가 움직였다"는 틀린 인과로 설명했다.
   `PmGateImpact`가 반증했고 재현해 확인했다(`7−5=2`, `73f79240..HEAD`가 정확히 2커밋).
   (라) §1.3 — **`grep → 0건`을 근거로 "방어 없음"을 주장했다. 그 0건이 거짓이었다.**
   같은 패턴을 grep 도구로 돌리면 **5개 파일**이 나온다. bash grep이 이 저장소에서 조용히
   빈 결과를 준다(`DirectorArbitration` 보고, 도구 이슈로 신고함). **결론은 유지되지만**
   (5개 전부 다른 것을 잰다) 근거가 틀렸고, **§4.1에서 인테이크의 똑같은 `0건` 주장을
   "부정확하다"고 정정한 문서가 같은 도구로 같은 오류를 저질렀다.**
   (마) §1.6 — `:2200-2400` 대역의 줄번호를 **부모 뮤테이션이 살아 있는 동안** 읽어 전부
   1줄씩 어긋났다(`:2338` → 실제 `:2339` 등). `ProgrammerPinPlan`이 잡았다. §0.4가
   "뮤테이션 중인 트리를 재지 말라"고 적은 문서가 그 대역에서 정확히 그것을 했다.

   **(다)와 (마)가 가장 무겁다** — 둘 다 **자기가 방금 만든 규칙을 자기 문서에서 절반만
   적용한 것**이다. (다)는 끝점 규칙, (마)는 기준 커밋 규칙.
   `ProgrammerPinPlan`이 같은 형태를 자수하며 더 나은 일반형을 냈고 채택한다:
   > **어느 좌표계를 쓰는지 밝히는 것으로 끝나지 않는다. 그 좌표계가 흔들리는 대상에
   > 실제로 적용됐는지 확인해야 한다.**
   그는 `GameManager.cs`에 HEAD 기준선을 선언해 Director의 오판을 막았으나 **실제로 다섯 번
   움직인 파일은 `HudCanvasContractTests.cs`였고 그쪽은 방어하지 않았다.** Director도 F-1에서
   같은 형태를 기록했다 — **개인의 부주의가 아니라 이 사이클의 반복 패턴이다.**
   **그리고 (라)가 방법론적으로 가장 위험하다**: `0건`은 측정값이 아니라 **주장**이며,
   재현 가능하려면 **도구 · 범위 · 패턴** 셋을 함께 적어야 한다(§2.3 카운트 규칙).
   세 레인이 이 회의에서 카운트를 틀렸고 원인이 셋 다 달랐다 — 나는 도구, ProgrammerPinPlan은
   패턴, Director는 범위. **셋 다 결론은 유지됐다.** 그래서 이것은 "결론이 틀렸다"가 아니라
   **"재현 불가능한 주장이었다"**다.
9. **이 문서의 S1 판정은 실행으로 뒷받침되지 않았다.** 계보 판정(§0.5)은 git 명령의 출력이라
   `실행`이지만 **S1 4건의 open/closed는 `추적`이다.** `evidence-strength` 칸이 그것을 표에
   드러내는 이유이고, 부모의 통과 XML이 UX-001/002를 `실행`으로 올릴 유일한 경로다.
