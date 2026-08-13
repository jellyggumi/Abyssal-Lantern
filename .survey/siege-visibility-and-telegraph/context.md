# Context: castle-war 가시성

- run-id: `20260813-siege-visibility-lane-a`
- lane: A (워크플로우 맥락) / mode: `market-landscape`
- 조사 대상 11종: Worms 시리즈(Armageddon·Revolution·W.M.D), ShellShock Live, Gunbound,
  포트리스2, Angry Birds, Crush the Castle, Into the Breach, Advance Wars, Slay the Spire,
  Teamfight Tactics, Hills of Steel
- 수집 방법: Steam 공개 리뷰 API(영어, `filter=helpful`, 전기간)로 6개 앱 1,300여 건을 받아
  가시성 관련 어구로 필터링한 뒤, **인용한 리뷰마다 개별 페이지 URL이 HTTP 200 +
  제목의 게임명 일치로 응답하는지 재확인**했다(2026-08-13). 검증 스크립트가 게임 귀속 불일치 1건을
  잡아내 해당 인용을 교체했다.
- **코드 0줄 수정.** 조사 문서 1개.
- 선행 조사와의 분업: `.survey/siege-artillery-landscape/`(계보 12종)와 중복하지 않는다.
  본 문서는 **"처음 잡은 사람이 한 턴 안에서 무엇을 모르는가"** 만 다룬다.
  "예고 과다의 역효과"는 레인 C가 담당한다(IRC 합의).

---

## Workflow Context

### 0. 사용자의 3문장을 인지 단계로 분해한다

사용자가 말한 것은 세 개의 서로 다른 실패다. 한 덩어리가 아니다.

| 사용자 원문 | 실패한 인지 단계 | castle-war 측 근거 |
|---|---|---|
| "게임 진행이 어떻게 보이는지 가시적으로 보이지 않는다" | ① 지금 내 차례인가 / ⑤ 경기 진행도 | UX-005 — `TurnCount`를 그리는 UI가 0개 `[OBSERVED]` |
| "내 캐릭터가 돌을 쏠 때 어떻게 써야 되는지도 안 보이고" | ② 무엇을 쓰는가 (④는 §정정 1로 조치 확인) | UX-004 — 다음 발사체·적 발사체 예고 없음 `[OBSERVED]` |
| "적이 어떻게 쏘는지도 안 보인다" | ⑥ 적은 무엇을 했나 | `SimpleAI.cs:62`→`:74` 예고 0초 `[OBSERVED]` |
| "전체적인 플레이를 어떻게 해야 되는지 모르겠다" | 6단계 전부의 합 | 적 턴 활성 버튼 0개 × 109.7초 `[OBSERVED]` |

> **핵심**: 사용자는 "UI가 촌스럽다"고 말한 것이 아니다.
> **한 턴을 도는 6개의 질문 중 4개에 화면이 답하지 않는다**고 말한 것이다. `[INFERENCE — 원문 대조]`

### 1. 턴제 포병 장르의 인지 6단계

이 장르에서 플레이어는 한 턴에 반드시 아래 순서를 통과한다.
순서를 건너뛸 수 없고, 앞 단계가 막히면 뒷 단계는 시도되지 않는다. `[INFERENCE — 10종 조작 구조에서 유도]`

```
① 지금 내 차례인가      ─→ ② 무엇을 쓰는가 ─→ ③ 어디를 노리는가
                                                     │
⑥ 적은 무엇을 했나  ←─  ⑤ 맞았나  ←─  ④ 얼만큼 세게 ─┘
        │
        └─→ (다음 턴 ①로) ※ 이 화살표가 끊기면 "턴"이 아니라 "독립 시행"이 된다
```

**6단계 전수 대조표**

| # | 플레이어의 질문 | 비교작 표준 해법 | castle-war 현재 | 판정 |
|---|---|---|---|---|
| ① | 지금 내 차례인가 | Worms: 카메라가 활성 웜으로 이동 + 팀명 / TFT: 준비·전투 페이즈 분리 | `turnText` + 셰브런 2개 + 토스트 1.7초 | ✅ **전달됨** |
| ② | 무엇을 쓰는가 | Worms/ShellShock: 무기 메뉴(플레이어 선택) / Gunbound: 1·2·SS 3발 상시 노출 | 이번 턴 **내 것만** `controlGuideText` | ⚠️ **부분** — 다음 턴·적 무기 없음 |
| ③ | 어디를 노리는가 | 대부분 **십자선 없음** → 플레이어가 각도표를 외운다 / Angry Birds: 궤적 점선 | **궤적 프리뷰 = 실전 물리와 동일** | ✅ **장르 상위권** |
| ④ | 얼만큼 세게 | 파워 게이지 + **바람 인디케이터** (1980 Apple II부터 표준) | 당김 거리 = 힘, `파워 60%` 표시 + **바람 수치 표시(런타임 입양)** | ⚠️ **조치됨** — 런타임 재확인 필요 |
| ⑤ | 맞았나 | Angry Birds: "왜 실패했는지 해독 가능하게" 조준 기구 조정 / **Scorched Earth(1991)·Apple II(1980): 이전 샷 궤적선 잔류** | 착탄 홀드 0.35초 + 구조 붕괴 + **점수 표시(런타임 입양)** / 비행 흔적은 착탄 시 소멸 | ⚠️ **부분** — 샷 이력이 남지 않음 (선례 35년) |
| ⑥ | 적은 무엇을 했나 | Into the Breach·Slay the Spire: **완전 예고** / Advance Wars: 피해 프리뷰 | **예고 0초.** 계산 즉시 발사 | ❌ **완전 부재 — 유일하게 모호함 없는 공백** |

**castle-war 측 근거** `[OBSERVED — 전부 코드/QA 실측]`:
①③ `qa/ux-defect-list.md:56-58` · ② UX-004 (`OneShotSiegeRules.cs:25-29`는 순수 결정론 public 함수인데 소비처가 프리팹 선택뿐) ·
④ **조치됨** — `SampleScene.unity`가 `windText: {fileID: 1739190289}`로 할당하고,
`GameManager.cs:309 Start()` → `:321 SetupUIButtons()` → `:1129 HudCanvas.Adopt(windText)`가
`HudCanvas.cs:112-134`에서 앵커·포지션 보존 **재부모화**를 수행한다. 값 갱신도 살아 있다
(`:2294` `WIND >>> 2.3`, `:2297` 3.5 이상 경고색). 상세는 §정정 1 ·
⑤ **부분** — 점수는 ④와 동일 경로로 조치됨(`:1130 HudCanvas.Adopt(scoreText)`, 갱신 `:2299` `SIEGE SCORE 3 - 2`).
남은 것은 **샷 이력의 부재** — `UnitController`의 `TrailRenderer`는 `time = 0.5f`(`:482`)의
비행 흔적이며 착탄 후 사라진다. 1980년 Apple II가 남긴 "이전 샷 궤적선"에 해당하는 것이 없다 ·
⑥ `SimpleAI.cs:62` 속도 계산 → `:74` 즉시 발사, 사이 0초.

### 2. 이 장르가 ③에서 실패해 온 역사 — 그리고 castle-war가 이미 통과한 지점

포병 계보는 40년간 ③(어디를 노리는가)을 플레이어에게 떠넘겼다.

- 1980년 Apple II *Artillery* / *Artillery Simulator*가 바람을 도입하면서 동시에
  **"이전 샷의 궤적을 화면에 선으로 남겨 다음 샷의 시각 데이터로 쓰게 하는"** 장치를 넣었다.
  `[direct page retrieval — https://en.wikipedia.org/wiki/Artillery_game]`
  → **탄도 가시화는 신기능이 아니라 1980년에 이미 있던 것이다.** `[OBSERVED]`
- Gunbound(2002)는 "지형·바람·원소 현상이 플레이어에게 조준과 파워 설정을 **끊임없이 바꾸도록 강제한다**"
  는 구조를 그대로 유지했다. 프리뷰 없이. `[direct page retrieval — https://en.wikipedia.org/wiki/Gunbound]`
- ShellShock Live(2015)는 지금도 프리뷰가 없다. 그 결과가 §Current Workarounds의 암기표 문화다.
- **그리고 계보는 한 번 있던 것을 잃었다.** 선행 조사가 Scorched Earth(1991)를 "현대 포맷의 원형"으로
  기록하며 **"이전 샷 궤적선 표시"**를 특기했고, 같은 표가 ShellShock Live(2015)를 그 계보의
  현대판으로 분류한다 (`.survey/siege-artillery-landscape/solutions.md:10-11`)
  `[direct page retrieval — 선행 조사 표]`.
  → 1991년 원형에 있던 샷 이력이 2015년 후계작에서 사라졌다.
  **고유 방문 36,505명이 읽는 각도 조회표는 그 손실의 인간 대체물이다.** `[INFERENCE]`

> **castle-war는 ③을 이미 해결했다.** 궤적 프리뷰가 동일 적분기·300스텝·6초·착탄까지 실전과 같다.
> **장르가 가장 오래 실패한 단계를 이 게임은 통과하고 있다.**
> 따라서 사용자가 느끼는 "안 보인다"는 ③의 문제가 **아니다** — ⑥(적 예고)의 문제이고,
> 부차적으로 ②(다음 발사체)·⑤(샷 이력)다. `[INFERENCE]`
> 이 구분이 이 조사에서 가장 실무적인 결론이다. ③에 더 투자하면 이미 해결된 곳에 돈을 쓰게 된다.

### 3. ⑥이 끊기면 "턴제"가 아니라 "번갈아 하는 솔리테어"가 된다

⑥(적은 무엇을 했나)은 다음 턴의 ①②③④를 결정하는 입력이다. 이 화살표가 끊기면
턴이 서로 연결되지 않고 독립 시행 43개가 나란히 놓인다. `[INFERENCE]`

**Into the Breach가 이 지점의 정면 반례다** — 그리고 상식과 반대 방향으로:

> "Subset은 각 전투를 짧게 유지하고 싶었다. 제한 턴 카운터를 썼고,
> **Vek의 움직임을 텔레그래프하는 것이 진행 속도를 더 빠르게 하는 데 도움이 된다는 것을 발견했다.**"
> `[direct page retrieval — https://en.wikipedia.org/wiki/Into_the_Breach]`

**텔레그래프는 게임을 느리게 하지 않는다. 빠르게 한다.**
castle-war의 유휴 62.2%·적 턴 34.1%는 텔레그래프를 넣을 **이유**이지 못 넣을 이유가 아니다. `[INFERENCE — 위 1차 증거 + 계측]`

같은 발견이 Slay the Spire에도 기록돼 있다:

> "원래 적은 다음 의도한 행동을 보여주지 않았다… **플레이테스트에서 그들은 플레이어가
> 카드 능력을 적용할 명확한 상황이 없어 혼란스러워한다는 것을 발견했다.**"
> 이후 `Next Turn` → `Intents` 아이콘으로 옮겼고, 처음에는 정확한 수치를 뺐다.
> "너무 많은 숫자로 플레이어를 압도하고 싶지 않았지만,
> **테스터를 통해 숫자를 노출하는 것이 더 몰입적이고, 기호를 암기할 필요를 없애며,
> 새 전략을 만들게 한다는 것을 발견했다.**"
> `[direct page retrieval — https://en.wikipedia.org/wiki/Slay_the_Spire]`

> **castle-war에 대한 직접 함의**: "숫자를 줄여 깔끔하게"는 검증된 실패 경로다.
> 그리고 castle-war는 이미 숫자를 **계산해 두고 안 보여주는** 상태다
> (UX-004 다음 발사체 · UX-005 경기 진행도 · ⑥ 적 의도 — 셋 다 순수 결정론 값이 이미 존재한다).
> 없는 정보를 만드는 게 아니라 **있는 정보를 연결하는** 문제다. `[INFERENCE]`

### 4. 실패는 ⑥에서만 나지 않는다 — ①에서도 난다

가장 낮은 단계도 무료가 아니다. Worms W.M.D 로컬 멀티 플레이어의 증언:

> "우리는 계속 카메라를 고치고 **어느 웜의 턴인지 필사적으로 알아내려 한다.
> 너무 혼란스러워서 모두가 몇 분 만에 흥미를 잃는다.**"
> `[direct page retrieval — https://steamcommunity.com/profiles/76561198091707599/recommended/327030/]`

castle-war는 ①을 3중(라벨·셰브런·토스트)으로 전달해 이 함정을 피하고 있다 `[OBSERVED — ux-defect-list.md:56]`.
**단, 적 턴에는 ①의 보조 신호인 타이머가 "15"에서 얼어붙는다**(UX-016: `GameManager.cs:1720`의
`isResolvingTurn` 조기 반환이 `:1726` 타이머 갱신보다 앞선다. AI가 0.9초에 발사하므로 14.1에서 정지).
가장 큰 숫자 위젯이 정지 신호를 내보내는 동안 화면은 "드래그해라"라고 말한다. `[OBSERVED]`

### 5. 화면이 침묵하는 것보다 나쁜 상태 — 거짓 지시

castle-war 적 턴 화면은 명령형 문장 2개를 내보내고 **둘 다 작동하지 않는다** `[OBSERVED — ux-defect-list.md §0]`:

| 표시 문자열 | 출처 | 그 입력이 막히는 지점 |
|---|---|---|
| `적 포격 준비 중... · 클릭: 벽돌 예약` | `SiegeAlarmSystem.cs:231` | `BrickPlacementController.cs:76-82` 조기 반환 (기본값 `enforceOneShotTurns = true`, `GameManager.cs:144`) |
| `<b>ARCHER</b> 준비 · 푸른 링 드래그 → 발사` | `LaunchManager.cs:118` | `LaunchManager.cs:530-532` 조준 게이트 + `GameManager.cs:2069` `lm.enabled = false` |

이것은 §1의 6단계 어디에도 속하지 않는 **7번째 실패**다 — 화면이 존재하지 않는 단계를 지시한다.
Angry Birds 개발 기록이 이 문제의 반대편을 보여준다:

> "**조준 기구는 플레이어가 왜 실패했는지 해독할 수 있도록 조정되었다.**"
> `[direct page retrieval — https://en.wikipedia.org/wiki/Angry_Birds_(video_game)]`

가시성의 목표는 "정보를 많이 띄우기"가 아니라 **"플레이어가 인과를 되짚을 수 있게 하기"** 다. `[INFERENCE]`

---

## Affected Users

| Role | Responsibility | Skill Level |
|---|---|---|
| **첫 플레이어 (튜토리얼 없이 진입)** | 6단계 전부를 스스로 발견해야 함. ①③④는 화면에서 얻지만 ②의 다음 발사체와 ⑥의 적 의도는 어디서도 얻지 못함 | **낮음** — 이 장르 리뷰에서 가장 많은 이탈 발생 지점. "설명이 아무것도 없어서 뭐가 뭔지 모른다"가 반복 문구 `[direct page retrieval — Steam 리뷰 다수]` |
| **적 턴의 모든 플레이어** (= 경기의 34.1%) | ⑥을 관측해야 하는데 관측 대상이 0.9초에 끝나고 그 후 활성 버튼 0개로 대기 | **무관** — 실력과 무관하게 전원이 겪는다. 109.7초 / 경기의 34.1% `[OBSERVED — qa/idle-time-measurement.md:275]` |
| **포병 경험자** (포트리스2·건바운드·ShellShock 출신) | ④를 자체 계산으로 해결하려 시도. 각도표·증분 카운팅·바람 보정계수를 이미 습관으로 보유 | **높음** — 이들에게는 **바람 수치(`WIND >>> 2.3`)와 궤적 프리뷰가 모두 제공되므로 ④는 장르 대비 유리하다.** 습관이 무력화되는 쪽은 오히려 ⑥ — 상대 의도를 읽는 경험이 통째로 쓸 데가 없다 `[INFERENCE — §정정 1 + UX-004]` |
| **물리 퍼즐 팬** (앵그리버드·Crush the Castle 출신) | ③⑤ 중심으로 플레이. 구조의 약점을 읽고 붕괴를 본다. ⑥을 기대하지 않음(원작이 단방향) | **중간** — castle-war의 양방향 턴제가 이들에게 **새 학습 부담**이다. ⑥이 처음 요구되는데 화면이 답을 안 준다 `[INFERENCE]` |
| **모바일 터치 유저** | 한 손 당김 제스처로 ③④를 동시 입력. 좁은 화면에서 6단계 정보를 모두 읽어야 함 | **낮음** — 겹침 결함 5건(UX-007~011)과 세이프에어리어 미적용(UX-013)이 이 사용자에게 집중된다. UX-008은 버튼 3개가 화면 밖 `[OBSERVED]` |
| **복귀 플레이어** (규칙을 잊은 재방문자) | ② 발사체 순환 규칙(Knight→Archer→Barrel)을 기억에서 복원해야 함 | **가변** — 규칙이 결정론적이므로 표시만 하면 즉시 회복되나, 현재 표시가 이번 턴 자기 것뿐이다. Worms 복귀자 증언: "10년 만에 다시 배우는 게 게임에서 겪은 가장 화나는 경험" `[direct page retrieval — Steam]` |
| **관전자 / 스트림 시청자** | 화면만 보고 상황을 읽어야 함. 입력 없음 | **낮음** — ①②④⑤⑥ 중 하나라도 화면에 없으면 관전 불가. **바람·점수는 조치됐고(§정정 1) 남은 공백은 경기 진행도(UX-005)와 ⑥ 예고**다. 다음 수를 예상할 수 없어 해설이 성립하지 않는다 `[INFERENCE]` |

---

## Current Workarounds

가시성이 나쁜 게임에서 플레이어는 견디지 않는다. **게임이 안 주는 정보를 스스로 만든다.**
아래는 그 산업 규모의 증거다.

### 1. 각도 암기표를 커뮤니티가 직접 쓴다 — 독자 36,505명

ShellShock Live는 궤적 프리뷰가 없다. 그 공백을 플레이어가 문서로 메웠다.
전부 `[direct page retrieval — steamcommunity.com]`, 2026-08-13 확인:

| 가이드 | 평가 수 | 내용 | URL |
|---|---|---|---|
| **Basic Angles You Should Know.** | **1,334** (고유 방문 **36,505** / 즐겨찾기 1,646) | 무기별 사거리·각도 조회표 | `https://steamcommunity.com/sharedfiles/filedetails/?id=926040949` |
| The 86 Aim Rule | 427 | "위로 최대 힘 → 방향키 4틱 = 각도 100,86 → 조준원 경계에 정확히 착탄" | `.../?id=699384173` |
| A Ruler Makes You Worse! + Tactics | 352 | "맵 반대편 = 100,66 / 중앙 = 100,79 / 사이 증분 13개, 증분당 탱크 2대 폭. **바람 10 = 1도 보정**" | `.../?id=717164360` |
| Perfect accuracy with CALCULATIONS | 334 | **게임의 중력 상수를 픽셀/초²로 역산하고 파워–초기속도 관계식을 유도**하는 물리 재구성 | `.../?id=1327582953` |
| How to measure in PU [Updated] | — | 플레이어가 **자체 단위 "PU"(power units)를 발명**해 폭발 크레이터 지름을 재고 반경으로 환산 | `.../?id=909582082` |

> **이것이 워크어라운드의 정점이다.** 게임이 물리를 안 보여주니
> 플레이어가 (a) 조회표를 만들고 (b) 방정식을 재도출하고 (c) **측정 단위를 새로 정의했다.**
> 36,505명이 그 문서를 읽었다. `[OBSERVED — 가이드 페이지 실측]`

### 2. 화면에 자를 붙인다 / 소프트웨어 오버레이

물리적 각도기를 모니터에 테이프로 붙이거나, 궤적선을 그려주는 외부 오버레이를 쓴다.
ShellShock Live에서 가장 유용하다고 평가된 부정 리뷰(추천 365)가 이 도구("ssundee ruler")를
정면으로 지목한다 `[direct page retrieval — https://steamcommunity.com/profiles/76561198125062755/recommended/326460/]`.
커뮤니티는 물리적 자는 "그냥 수학"으로, 소프트웨어 오버레이는 치팅으로 나누는 경향이다 `[thin evidence — 종합 검색, 개별 스레드 미검증]`.

> **castle-war 함의**: **궤적 프리뷰가 이미 있다는 것은 이 워크어라운드 전체를 무력화한다.**
> 이 게임에서는 오버레이를 만들 동기가 없다. 장르의 가장 큰 부정행위 벡터가 설계로 닫혀 있다. `[INFERENCE]`

### 3. 게임 밖에서 배운다 — 커뮤니티 가이드와 영상

게임 안에서 못 배우니 밖에서 배운다. 이 워크어라운드의 핵심은 플레이어 본인이 **그것이 결함임을 안다**는 점이다.
- ShellShock Live (Scorched Earth 복귀자, 13시간): "많은 무기는 뭘 하는지 명백하지만 **많은 무기는 그렇지 않다.
  툴팁을, 뭐라도 제공하라. 내가 찾던 것은 커뮤니티 섹션에서 발견했지만, 이런 걸 알아내려고
  가이드를 찾아다녀야 할 이유는 없다.** 무기는 미리 선택돼 있거나 무작위로 뽑히는 것처럼 보인다"
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198052665508/recommended/326460/]`
  → **castle-war의 ②와 정확히 같은 형태다.** 발사체가 규칙으로 순환하는데(플레이어 선택 아님)
  그 규칙이 화면에 없다(UX-004). 이 리뷰어가 요구한 것은 선택권과 **툴팁 둘 다**였다 `[INFERENCE]`
- Worms Revolution: "**100번의 시행착오를 거치다가 결국 해답 영상을 찾아보게 되는** 퍼즐이 상당 부분"
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198007359565/recommended/200170/]`
- Into the Breach: "예측할 수 없는 상황에서 승리 불가 상태에 쉽게 빠진다. …
  **선제 전략을 구글링할 수도 있겠지만 그건 별로 흥미로운 제안이 아니다**"
  `[direct page retrieval — https://steamcommunity.com/profiles/76561197977850135/recommended/590380/]`

### 4. 시행착오와 트레이닝 모드 반복

- Worms Armageddon: "이 게임의 컨트롤로 **많은 추측 작업**을 해야 했다"
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198063036176/recommended/217200/]`
- Worms Armageddon: "백스페이스? 클릭? 숫자키? 엔터? 스페이스바? 각 버튼이 뭘 하는지
  알아내는 건 행운을 빈다, **시행착오다**"
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198025426945/recommended/217200/]`
- Into the Breach조차: "특정 분대/미션 조합은 승리 불가인데 모르고 선택할 수 있다.
  신규 플레이어는 **시행착오로 알아낼 때까지** 이를 알 수 없다"
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198053358125/recommended/590380/]`

### 5. 옆에 앉은 사람이 통역한다 (로컬 멀티의 인간 UI)

가장 저비용이고 가장 흔한 워크어라운드다. **숙련자가 실시간으로 화면을 해설한다.**
Worms W.M.D 리뷰가 포럼 인용을 통해 이 상태를 기록한다:

> "이 게임에 입문시켜 본 사람 모두가 **뭘 해야 할지 알아내려다 좌절하고 당황하며**,
> 내가 컨트롤과 무기 설명을 해주는 동안 90초 안에 그걸 해내려 애쓴다.
> 내 여자친구는 평생 비디오게임을 해본 적이 없다. **90초는 내가 한 턴을 안내하기에 턱없이 짧다.**"
> `[direct page retrieval — https://steamcommunity.com/profiles/76561198362135968/recommended/327030/]` (추천 42)

> 이 증언은 **턴 타이머가 가시성 결함의 증폭기**임을 보여준다.
> 정보가 없는데 시간 제한까지 있으면 학습이 아니라 공황이 된다.
> castle-war 턴 타이머는 15초다(`GameManager.cs:21`) — Worms의 90초보다 **6배 짧다.** `[OBSERVED]`
> 단 castle-war는 프리뷰가 있어 계산 부담이 낮다. 이 두 사실의 상호작용은 실측 대상이다. `[INFERENCE]`

### 6. 적 턴에 딴짓을 한다

⑥을 관측할 방법이 없으면 플레이어는 화면을 떠난다.
- Worms Armageddon: "**80%가 기다림으로 구성된다.** 미션 시작을 기다리고,
  모스 부호 메시지가 사라지길 기다리고, **적이 턴을 끝내기를 기다리고**…"
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198027049756/recommended/217200/]`
- Into the Breach 긍정 리뷰: "**일하면서 하기 좋은 게임.**"
  `[direct page retrieval — Steam 리뷰 API, recommendationid 230052966]`

### 7. 물리를 아예 없애는 우회 (개발자 측 워크어라운드)

Hills of Steel(모바일)은 각도·힘 입력을 **제거**하고 실시간 전후진 + 발사 버튼으로 바꿨다.
④를 UI 문제에서 위치 선정 문제로 치환한 것이다. `[thin evidence — 종합 검색, Play 스토어 원본 페이지 미수복(404)]`

---

## Adjacent Problems

가시성과 **같이** 오는 문제들. 따로 고칠 수 없다.

### A. 예고되지 않은 규칙은 부정행위로 읽힌다

가장 반복되는 인접 문제다. 정보가 없으면 플레이어는 **불운이 아니라 사기를 의심한다.**

- Worms Armageddon: "옛날에 게임이 '치팅한다'거나 '불가능하다'고 욕하던 때를 기억하는가?
  **이 게임은 실제로 치팅한다**" `[direct page retrieval — .../76561198058683396/recommended/217200/]`
- Worms Armageddon: "**컴퓨터가 치팅한다고 주장해도 틀리지 않을 수 있는 유일한 게임이다**"
  `[direct page retrieval — .../76561198076176773/recommended/217200/]`
- Worms Revolution: "AI가 맵 전체를 가로질러 수류탄을 던져 **벽 15개를 튕겨** 웜의 은신처로 굴려보낸다…
  **정확한 바람과 낙차를 알고** 코비처럼 꽂아넣는다" `[direct page retrieval — .../76561197961403796/recommended/200170/]`
- Into the Breach: "**상대가 계속 치팅할 수 있다면 그게 로봇 체스다**"
  `[direct page retrieval — .../76561198308508219/recommended/590380/]`

**Into the Breach의 대칭 반례**가 이 문제의 해법을 증명한다 — 완전 예고를 쓰면
"건물을 잃은 것은 불공정한 RNG가 아니라 **계획의 실패**로 인식된다" `[indexed snippet — 신뢰도 medium, 다수 2차 출처 종합]`.

> **castle-war 함의**: 결정론적 시뮬레이션 + 바람 표시 + 적 예고 3개가 이 문제의 정면 답이다.
> 세 요소 중 **결정론과 바람 표시는 이미 있고(§정정 1), 남은 것은 적 예고 하나다.** `[OBSERVED]`
> 선행 조사가 Archery Bastions에서 관측한 "유닛이 이유 없이 죽는다"는 불만도 같은 계보다.

### B. 유휴 시간과 가시성은 곱셈으로 악화된다

두 문제는 독립적이지 않다. **볼 것이 없는 시간**은 짧아도 길게 느껴지고, 길면 이탈이 된다.

- Worms Revolution: "AI는 행동하기까지 **평균 30초**를 쓴다. 취소할 수 없는 5초 버퍼 타이머가 있다…
  결국 당신은 자기 턴 전에 **아주 오래 기다린다**" `[direct page retrieval — .../76561198122627449/recommended/200170/]`
- Worms W.M.D (추천 101): "게임이 너무 느리게 진행된다. 애니메이션 시간은 절반으로 줄여야 했다…
  **AI는 움직이기 전에 15초를 생각해야 한다. 왜 모든 게 이렇게 느린가?**"
  `[direct page retrieval — .../76561197963849657/recommended/327030/]`

castle-war 대조 `[OBSERVED — 코드]`: AI 예고 지연은 **0.9초**로 이미 극단적으로 짧다.
분해하면 `GameManager.cs:2159`의 0.4초 + `SimpleAI.cs:30`의 0.5초다.
**즉 castle-war의 문제는 Worms형 "AI가 너무 느림"이 아니라 정반대 — "AI가 너무 빨라 관측 창이 없음"이다.**

그런데 그 0.9초의 **구조**가 결정적이다 (`SimpleAI.cs:24-62` 직접 확인) `[OBSERVED — 코드]`:

```
:30  yield return new WaitForSeconds(0.5f)   ← 대기가 먼저 온다
:31  FindTargetPosition()                    ← 조준 대상은 대기 "후"에 정해진다
:34  AutomaticProjectilePrefab               ← 순수 getter (아래 주의)
:62  CalculateLaunchVelocity(...)            ← 속도 계산도 대기 후
```

**예고 창은 이미 존재하고, 이미 300초 예산에서 지불되고 있다.**
다만 그 창이 열려 있는 동안 표시할 값이 **아직 계산되지 않았다.** `[INFERENCE — 위 실행 순서에서 유도]`
(레인 B가 같은 지점을 독립 발견했다 — §정정 기록 「레인 간 수렴」.)

**단, 세 값이 같은 난이도가 아니다.** 레인 B의 정밀 지적을 받아 `:34`를 재확인했다 —
`AutomaticProjectilePrefab`(`GameManager.cs:2079-2093`)은 `OneShotSiegeRules.ProjectileForTurn(turnCount)`
스위치가 전부인 **순수 getter**이고 `turnCount` 외에 아무것도 읽지 않는다.
즉 이 값은 대기 전은 물론 **턴 시작 시점에 이미 알 수 있다** — 재배치가 필요 없다. `[OBSERVED — 코드]`
따라서 예고는 한 덩어리가 아니라 **2단으로 분해된다**:

| 단 | 예고 대상 | 필요한 변경 | 비용 |
|---|---|---|---|
| **1단** | **적 발사체 종류** (UX-004) | **없음** — 이미 public 순수 함수. 표시만 붙이면 된다 | 재배치 0 · 계산 0 |
| **2단** | 적 조준 대상·궤적 | `:31`·`:62`를 대기(`:30`) 앞으로 이동 | 순서 변경 + "얼마나 보여줄지"가 밸런스 결정 |

> **1단은 2단의 승인을 기다릴 필요가 없다.** 디렉터가 궤적 노출(2단)을 밸런스 이유로 보류해도
> 발사체 예고(1단)는 독립적으로 성립한다. 그리고 §Workflow Context 1의 ② 판정을
> ⚠️부분 → ✅전달됨으로 올리는 것이 정확히 이 1단이다. `[INFERENCE — 레인 B와 공동 결론]`

더 결정적인 것은 그 대기의 **주석에 적힌 의도**다 (`SimpleAI.cs:28-29`):

> "0.9초 AI 비트의 절반 (`GameManager.ExecuteAITurn`이 나머지 0.4초를 보유) —
> **적이 조준하는 것으로 읽힐 만큼의 멈춤, 기다림이 아니라.**"

> 설계 의도가 이미 "적이 조준하는 것으로 읽히게 하라"인데,
> **그 0.9초 동안 화면에 조준을 나타내는 것이 아무것도 없다.**
> ⑥의 부재는 설계 결정이 아니라 **의도와 구현 사이에 벌어진 틈**이다. `[INFERENCE]`

> 이 구분이 처방을 정한다. Worms의 처방(속도를 올려라)을 castle-war에 적용하면 문제가 악화된다.
> castle-war에 필요한 것은 **0.9초를 늘리는 것이 아니라, 이미 있는 0.9초를 채우는 것**이다 —
> 1단은 이미 아는 값을 그리기만 하고, 2단은 계산 순서를 앞당긴다. 어느 쪽도 시간을 더 쓰지 않는다.
> §Workflow Context 3의 Into the Breach 증거(텔레그래프가 속도를 **올렸다**)가 이 방향을 지지한다.

### C. 정보 부족과 시간 압박은 함께 오면 공황이 된다

§Current Workarounds 5의 Worms W.M.D 증언이 정확히 이 조합이다.
학습 곡선을 낮추는 요청이 "튜토리얼 추가"가 아니라 **"무제한 턴 시간 옵션"** 이었다는 점이 중요하다.
플레이어가 원한 것은 설명이 아니라 **읽을 시간**이었다. `[direct page retrieval — 동일 리뷰]`

### D. 튜토리얼은 가시성의 대체재가 아니다

이 장르 리뷰에서 "튜토리얼 없음"은 최다 불만이지만, **튜토리얼이 있어도 해결되지 않는다.**
- Worms Armageddon: "기본 훈련은 뭘 하라고만 말하고 **어떻게 하는지는 아무 힌트도 주지 않는다**"
  `[direct page retrieval — .../76561198008598176/recommended/217200/]`
- Worms Revolution: 튜토리얼 강제 + 훈련 미션 20개를 통과해야 본 게임 진입 → 그 자체가 이탈 요인
  `[direct page retrieval — .../76561198007359565/recommended/200170/]`

반대편 증거 — Advance Wars는 서양 출시를 위해
"메커니즘을 이해하기 쉽게 만들고 **매뉴얼을 읽을 필요가 없는 심층 튜토리얼**을 추가했다"
`[direct page retrieval — https://en.wikipedia.org/wiki/Advance_Wars]`.
핵심은 튜토리얼의 존재가 아니라 **매뉴얼 없이 화면만으로 이해 가능한가**다. `[INFERENCE]`

### E. 모바일 터치는 가시성 결함을 증폭한다

좁은 화면에서 6단계 정보를 다 읽어야 하는데 castle-war의 겹침 결함 5건이
전부 정보 표시 영역에서 발생한다 `[OBSERVED — UX-007~011]`.
특히 UX-007은 발사 크로스헤어가 `파워 60%` 글자를 관통한다 — **④의 유일한 수치 표시가 가려진다.**

Angry Birds가 이 문제를 푼 방식은 입력 축소였다:
트레뷰셋의 "클릭-발사, 클릭-정지"를 **드래그-릴리스 새총**으로 바꿨고,
"플레이어가 즉시 사용법을 이해했기 때문에" 새총으로 되돌아왔다
`[direct page retrieval — https://en.wikipedia.org/wiki/Angry_Birds_(video_game)]`.
castle-war는 이미 새총 제스처를 채택했다 — **입력은 해결됐고 출력(표시)이 남았다.** `[INFERENCE]`

### F. 관전 불가는 마케팅 문제로 번진다

**경기 진행도**가 미표시라 관전자가 "몇 턴째인지 / 얼마나 남았는지"를 읽을 수 없고,
⑥ 예고가 없어 다음 수를 예상할 수도 없다 `[OBSERVED — UX-005 + ⑥]`
(바람·점수는 §정정 1 참조 — 런타임 입양으로 조치됨).
스트림·트레일러·스토어 스크린샷이 전부 같은 화면을 쓰므로 이 결함은 획득 단계까지 전파된다. `[INFERENCE]`

### G. 적 턴 화면의 시각 증거가 아직 없다

정직하게 남긴다. 경기의 34.1%를 차지하는 상태에 스크린샷이 없다
(UX-015: 캡처 3건이 전부 타이틀·매치시작·플레이어턴)
`[OBSERVED — qa/ux-defect-list.md:123]`.
본 문서의 ⑥ 관련 서술은 **코드 경로 추적과 QA 문서에 의존**하며 화면 대조를 거치지 않았다.
`ux-4-enemy-turn` 캡처가 나오면 §Workflow Context 5의 표를 재검증해야 한다.

---

## User Voices

전부 실제 인용. 각 항목에 출처 URL과 증거 등급을 붙였다.
Steam 리뷰는 공개 리뷰 API로 수집한 뒤 **개별 리뷰 페이지 URL이 HTTP 200 + 제목 일치로 응답하는지 재확인**했다
(2026-08-13). 따라서 등급은 `direct page retrieval`이다.

**"뭔가 일어나는지 모르겠다" 계열 (우선)**

1. > "이 게임에 입문시켜 본 사람 모두가 **뭘 해야 할지 알아내려다 좌절하고 당황하며**, 내가 컨트롤과
   > 무기 설명을 해주는 동안 90초 안에 그걸 해내려 애쓴다. … **90초는 내가 한 턴을 안내하기에 턱없이 짧다.**"
   — Worms W.M.D, 추천 42, 16시간 플레이 (원문은 포럼 인용을 재인용)
   `https://steamcommunity.com/profiles/76561198362135968/recommended/327030/`
   `[direct page retrieval]` `[OBSERVED]`

2. > "이 게임은 **선택지가 과도하게 많아** 고통받는다. 개발자들은 분명 플레이어가 마음껏 놀 수 있게
   > 열어주고 싶었겠지만, 내가 발견한 문제는 **인터페이스가 너무 복잡해서 내가 뭘 하고 있는지,
   > 어디로 가고 있는지, 어떤 종류의 게임을 하려는 건지 알 수 없다는 것**이다."
   — Worms Armageddon, 추천 4, 1시간 플레이
   `https://steamcommunity.com/profiles/76561197998527528/recommended/217200/`
   `[direct page retrieval]` `[OBSERVED]` — ①②③을 한 문장에 담은 증언

3. > "우리는 계속 카메라를 고치고 **어느 웜의 턴인지 필사적으로 알아내려 한다. 너무 혼란스러워서
   > 모두가 몇 분 만에 흥미를 잃는다.** 로컬 멀티플레이는 플레이 불가다."
   — Worms W.M.D, 추천 6, 4시간 플레이
   `https://steamcommunity.com/profiles/76561198091707599/recommended/327030/`
   `[direct page retrieval]` `[OBSERVED]` — ① 실패 사례

4. > "**설명을 아무것도 안 해주기 때문에 뭐가 어떻게 작동하는지 모르겠다.** … 이미 이 게임을
   > 출시부터 해온 게 아니라면 당신을 위한 게임이 아니다."
   — ShellShock Live, 추천 2, 2시간 플레이
   `https://steamcommunity.com/profiles/76561199026098511/recommended/326460/`
   `[direct page retrieval]` `[OBSERVED]`

5. > "**설명서가 없다, 어떻게 뭐가 뭘 하는지 내가 어떻게 알겠나**, 게다가 다 발사 방식이 달라서
   > **조준을 어떻게 하는지 도저히 모르겠다.**"
   — ShellShock Live, 16시간 플레이 (원문 오타 다수, 뜻 보존해 옮김)
   `https://steamcommunity.com/profiles/76561198301295465/recommended/326460/`
   `[direct page retrieval]` `[OBSERVED]` — ②③④를 한 문장에 담은 증언

6. > "게임플레이가 **무슨 일이 벌어지고 있는지에 대한 실질적 설명 없이** 바로 시작하는 것도
   > 별로 좋아하지 않는다. 20분도 안 하고 껐다."
   — Into the Breach, 추천 15, 0시간(환불) — **텔레그래프의 모범작조차 진입 설명에서는 같은 불만을 받는다**
   `https://steamcommunity.com/profiles/76561198353593495/recommended/590380/`
   `[direct page retrieval]` `[OBSERVED]`

7. > "**적에게 반응하기가 매우 어렵고 어떤 종류가 침입하는지 알 수 없다.**"
   — Into the Breach, 추천 11, 8시간 플레이
   `https://steamcommunity.com/profiles/76561198076193960/recommended/590380/`
   `[direct page retrieval]` `[OBSERVED]` — **예고가 부분적일 때 남는 정확한 공백.
   castle-war의 UX-004(적 발사체 미예고)와 같은 형태다** `[INFERENCE]`

**④ 조준 정보 부재 → 자체 계산 계열**

8. > "A. **모니터에 각도기를 영구히 테이프로 붙이거나** B. **매 샷을 놓치지 않기 위해 각도
   > 스프레드시트를 암기하거나** C. 실제로 쓸 만한 무기를 얻으려 100시간 XP를 갈아넣는 것에
   > 흥미가 있다면, 이 게임은 당신을 위한 것이다."
   — ShellShock Live, 추천 2, 74시간 플레이
   `https://steamcommunity.com/profiles/76561198260601229/recommended/326460/`
   `[direct page retrieval]` `[OBSERVED]` — 워크어라운드를 플레이어 본인이 요약한 문장

9. > "맵 반대편을 맞히고 싶은가? — 100,66. 중앙을 맞히고 싶은가? — 100,79. …
   > 중앙과 최원거리 사이에 **증분 13개**가 있다. 각 증분 사이 간격은 **탱크 2대 폭**이다. …
   > **바람 10 = 1도 변화**로 계산해 보정하라."
   — ShellShock Live 커뮤니티 가이드 *A Ruler Makes You Worse! + Tactics*, 평가 352
   `https://steamcommunity.com/sharedfiles/filedetails/?id=717164360`
   `[direct page retrieval]` `[OBSERVED]` — 게임이 안 주는 ④를 플레이어가 표로 재구성한 실물

10. > "2D 포물선 운동에서 x방향 속도는 일정하고, y방향 속도는 오직 중력에 의해 변한다. 우리의 목표는
    > 두 탱크의 좌표와 임의의 각도가 주어졌을 때 필요한 **파워**를 찾는 것이다. …
    > **중력 상수를 픽셀/초²로 계산**하고 초기속도와 파워의 관계를 찾아야 한다."
    — ShellShock Live 커뮤니티 가이드 *Perfect accuracy with CALCULATIONS*, 평가 334
    `https://steamcommunity.com/sharedfiles/filedetails/?id=1327582953`
    `[direct page retrieval]` `[OBSERVED]` — **플레이어가 게임의 물리 엔진을 역공학한 문서**

**⑥ 적 행동 불투명 → 치팅 인식 계열**

11. > "게임 안에서 컨트롤을 볼 방법도, 무기를 효과적으로 쓸 방법도 없어서 **1대1 대전에서
    > 쓸모없이 허우적거리게 된다.** … 옛날에 게임이 '치팅한다'거나 '불가능하다'고 욕하던 때를
    > 기억하는가? **이 게임은 실제로 치팅한다.**"
    — Worms Armageddon, 추천 8, 20시간 플레이
    `https://steamcommunity.com/profiles/76561198058683396/recommended/217200/`
    `[direct page retrieval]` `[OBSERVED]` — 정보 부재 → 부정행위 인식으로 넘어가는 경로가 한 문장에 있다

12. > "AI가 맵 전체를 가로질러 수류탄을 던져 **벽 15개를 튕겨** 웜의 은신처로 정확히 굴려보낸다.
    > … **정확한 바람과 낙차를 알고** 코비처럼 꽂아넣는다. … 적 웜이 턴의 절반을 '생각'하는 데 쓰고
    > 나서 결국 그 신급 조준 바주카를 쏜다. **이건 당신의 시간을 존중하지 않는다.**"
    — Worms Revolution, 추천 5, 16시간 플레이
    `https://steamcommunity.com/profiles/76561197961403796/recommended/200170/`
    `[direct page retrieval]` `[OBSERVED]` — ⑥ 불투명 + 유휴가 같은 리뷰에서 결합한다

**유휴 계열**

13. > "**80%가 기다림으로 구성된다.** 미션 시작을 기다리고, 멍청한 모스 부호 메시지가 사라지길
    > 기다리고, **적이 턴을 끝내기를 기다리고**, 맵에 무기가 스폰되길 기다리고…"
    — Worms Armageddon, 추천 3, 14시간 플레이
    `https://steamcommunity.com/profiles/76561198027049756/recommended/217200/`
    `[direct page retrieval]` `[OBSERVED]`

14. > "AI는 행동하기까지 **평균 30초**를 쓰고, 매 턴 전 취소 불가능한 5초 버퍼 타이머가 있다.
    > 게임은 또 물리가 정리될 때까지 멈춰야 한다. 결국 **자기 턴 전에 아주 오래 기다리게 되고,
    > 실수하면 더 답답하다.**"
    — Worms Revolution, 추천 5, 19시간 플레이
    `https://steamcommunity.com/profiles/76561198122627449/recommended/200170/`
    `[direct page retrieval]` `[OBSERVED]`

**개발자 측 1차 증언 (해법 방향)**

15. > "프로토타입이 만들어졌을 때 **테스트 플레이어들은 무엇을 해야 할지 전혀 몰랐다.** 개발진은
    > **'알아볼 수 있는 메커니즘'** 이 필요하다고 판단했다. … **조준 기구는 플레이어가 왜 실패했는지
    > 해독할 수 있도록 조정되었다.** … Chillingo는 최종 폴리싱에 **눈에 보이는 궤적선** 추가 등으로
    > 참여했다고 주장한다."
    — Angry Birds 개발 기록
    `https://en.wikipedia.org/wiki/Angry_Birds_(video_game)`
    `[direct page retrieval]` `[OBSERVED]`

16. > "원래 적들은 다음 의도한 행동을 보여주지 않았다. … **플레이테스트에서 플레이어가 카드 능력을
    > 적용할 명확한 상황이 없어 혼란스러워한다는 것을 발견했다.** … 처음에는 정확한 수치를 빼고
    > 아이콘만 썼다. … 그러나 테스터를 통해 **숫자를 노출하는 것이 더 몰입적이고, 기호를 암기할
    > 필요를 없애며, 새 전략을 만들게 한다는 것을 발견했다.**"
    — Slay the Spire 개발 기록 (Anthony Giovannetti)
    `https://en.wikipedia.org/wiki/Slay_the_Spire`
    `[direct page retrieval]` `[OBSERVED]` — 레인 C와 공유 출처, 인용 각도 상이(C는 장르 표준화)

17. > "Subset은 각 전투를 짧게 유지하고 싶었다. 제한 턴 카운터를 썼고,
    > **Vek의 움직임을 텔레그래프하는 것이 진행 속도를 더 빠르게 하는 데 도움이 된다는 것을 발견했다.**"
    — Into the Breach 개발 기록
    `https://en.wikipedia.org/wiki/Into_the_Breach`
    `[direct page retrieval]` `[OBSERVED]` — **본 조사에서 castle-war에 가장 직접적인 1차 증거.
    유휴 62.2%는 텔레그래프를 넣을 이유이지 못 넣을 이유가 아니다** `[INFERENCE]`

18. > "일부 게임은 **이전 샷이 지나간 궤적을 화면에 선으로 표시해**, 플레이어가 다음 샷을 고려할 때
    > 시각 데이터를 쓸 수 있게 했다." (1980년 Apple II *Artillery* / *Artillery Simulator*)
    — Artillery game
    `https://en.wikipedia.org/wiki/Artillery_game`
    `[direct page retrieval]` `[OBSERVED]` — 탄도 가시화는 1980년에 이미 있었다

---

## 정정 기록

### 정정 1 — ④ 바람 미표시(UX-001)는 **이미 조치됐다**. 초판 판정을 철회한다.

초판은 ④를 "❌ 최대 변수가 안 보임"으로, 그리고 ⑥과 함께 **두 개의 핵심 공백**으로 적었다.
**틀렸다.** 레인 B(`LaneBSolutions`)가 지적했고, 그 근거를 그대로 믿지 않고 본 레인이
독립적으로 6단계 전부 재추적해 확인했다. 전부 `[OBSERVED — 코드]`:

| # | 링크 | 근거 |
|---|---|---|
| 1 | 씬이 참조를 실제로 할당한다 | `SampleScene.unity` GameManager MonoBehaviour 블록: `windText: {fileID: 1739190289}`, `scoreText: {fileID: 835917193}` |
| 2 | 부트 경로에 있다 | `GameManager.cs:309 Start()` → `:321 SetupUIButtons()` |
| 3 | 입양이 호출된다 | `GameManager.cs:1129 HudCanvas.Adopt(windText)` (주석 `:1124-1128`이 UX-001을 그대로 서술하며 착지 좌표까지 실측: 좌 80-213 / 우 427-560, 겹침 없음) |
| 4 | 입양은 진짜 재부모화다 | `HudCanvas.cs:112-134` — `rect.SetParent(root, false)` 전후로 앵커·앵커드포지션·피벗·사이즈를 보존. 조기 반환 조건은 `rect.parent == root`뿐이고 windText의 부모는 null이라 통과한다 |
| 5 | 부모가 실제 Canvas다 | `HudCanvas.cs:90 Root()` → `MobileSafeArea.GetContentRoot(Resolve())`; `Resolve()`(`:51-85`)가 `GameplayHudCanvas`를 생성하고 ScreenSpaceOverlay·sortingOrder 100을 설정 |
| 6 | 값 갱신이 살아 있다 | `GameManager.cs:2294` `WIND >>> 2.3` / `WIND CALM`, `:2297` 세기 3.5 이상이면 경고색 |

**초판이 왜 틀렸는가**: `qa/ux-defect-list.md`의 UX-001을 **현재 상태로 읽었으나 실제로는
발견 시점의 스냅샷**이었다. 씬 파일이 여전히 `m_Father: {fileID: 0}`인 것은 결함의 잔존이 아니라
**런타임 입양이 존재하는 이유**다. 씬 정적 구조만 보면 이 항목은 영원히 "렌더 0"으로 읽힌다.

**증거 등급의 비대칭을 명시한다**: 어느 레인도 Unity를 돌리지 않았다(하네스 규칙).
따라서 "픽셀을 봤다"는 주장은 양쪽 모두 불가능하다. 그러나 두 주장은 대등하지 않다 —
입양 경로는 스킵 분기 없이 6개 링크가 끝까지 이어지는 반면,
"렌더 0"은 씬 정적 구조 하나에만 의존하며 런타임 재부모화를 반박할 수 없다.
**따라서 "조치됨"을 채택하고, 잔존 리스크는 "런타임 재확인 필요"로 남긴다.** `[INFERENCE — 근거 강도 비교]`

**결론이 어떻게 바뀌는가** (이 문서에서 가장 중요한 변경):

> 초판: "진짜 공백은 ④바람과 ⑥적예고 둘이다."
> 정정: **"유일하게 모호함 없는 공백은 ⑥ 하나다."**
> ①③④는 전달되고, ②⑤는 부분이며, ⑥만 완전 부재다.
> ④에 재투자하면 **이미 고친 것에 돈을 쓰게 된다.**

수정한 위치: §Workflow Context 0 표 · 6단계 판정표 ④⑤⑥ · 근거 목록 ·
§3 함의 · §Affected Users 첫 플레이어·포병 경험자·관전자 행 · §Adjacent Problems A·F.

**정정 1의 파급 — ⑤ 점수(UX-002)도 같은 조치에 걸린다.** 레인 D가 §4.1 렌더 수 계측 문제를
지적하면서 드러났다. UX-002는 UX-001과 **같은 결함·같은 수정**이다:
`GameManager.cs:1130 HudCanvas.Adopt(scoreText)`가 바로 다음 줄에 있고, 갱신도 살아 있다
(`:2299` `SIEGE SCORE 3 - 2`). 초판은 ④만 정정하고 ⑤를 그대로 뒀는데, 이는 **같은 근거로 한쪽만
고친 비일관**이었다. `[OBSERVED — 코드]`
→ ⑤의 남은 공백은 점수가 아니라 **샷 이력**이다. `UnitController`의 `TrailRenderer`는
`time = 0.5f`(`:482`)이고, 착탄·정지 시 `emitting = false`로 꺼진다
(`:636` 지면 접촉, `:698` 타깃 소실, `:1174`·`:1194` 정지 경로). 즉 **비행 중에만 보이고 흔적이 남지 않는다.**
`[OBSERVED — 코드, 레인 D와 교차 확인]`

**선례는 1980년이 아니라 1991년에도 있다** — 선행 조사가 이미 기록해 뒀다:
`.survey/siege-artillery-landscape/solutions.md:10`이 **Scorched Earth(1991)**를
"현대 포맷의 원형"으로 평가하며 특기 사항으로 **"이전 샷 궤적선 표시"**를 적었다
`[direct page retrieval — 선행 조사 표]`. 같은 표(`:11`)가 ShellShock Live(2015)를
"Scorched Earth 계보의 현대 온라인판"으로 분류한다.

> **그래서 계보가 이 기능을 잃었다.** 1991년 원형에는 샷 이력이 있었고,
> 2015년 후계작에는 없다. 그 공백을 메운 것이 §Current Workarounds 1의 조회표 문화다 —
> **고유 방문 36,505명이 읽는 각도표는 게임이 없앤 샷 이력의 인간 대체물이다.** `[INFERENCE]`
> castle-war는 ③(프리뷰)로 이 손실을 앞쪽에서 보상했으나, ⑤(사후 이력)는 여전히 비어 있다.
> 35년 전 선례가 있는 기능이다.

**정정 1이 파급되지 않는 항목도 확인했다** — UX-005(경기 진행도)는 여전히 유효하다.
`TurnCount`(`GameManager.cs:175`)의 소비처를 전수 확인한 결과 전부 로직이며
(`DeploymentController`·`CastleCoreGimmick`·`FirstPlayGuide`·`FirstPlayCoachController`)
**이 값을 그리는 UI는 하나도 없다.** `[OBSERVED — 코드 전수]`
즉 "결함표가 오래됐다"를 모든 항목에 일반화하면 안 된다. 항목별로 확인해야 한다.

### 정정 2 — 인용 1건 귀속 오류

초판 초고에서 Steam appid `1076160`을 Worms Rumble로 오기했으나 실제로는
**Command: Modern Operations**였다(리뷰 본문 "From WW2 to modern day"가 단서).
URL별 페이지 제목 자동 대조 검증이 잡아냈고, 검증된 ShellShock Live 리뷰로 교체했다.
교체본이 오히려 더 적합했다 — "가이드를 찾아다녀야 할 이유는 없다"가 UX-004와 같은 형태다.
조사 대상 목록도 12종→11종으로 정정했다.

### 레인 간 수렴 (참고)

- 레인 B가 `SimpleAI.cs:30`의 0.5초 지연이 조준 계산(`:62`)보다 **먼저** 온다는 것을 확인했다.
  → 예고 창은 **이미 존재하고 이미 300초 예산에서 지불 중**이며, 그 시점에 보여줄 값이
  아직 계산되지 않았을 뿐이다. 순서만 바꾸면 시간 비용 0.
  이것은 §Adjacent Problems B의 "0.9초를 늘리는 것이 아니라 그 전에 예고를 놓는 것"에
  코드 수준 근거를 붙여준다. `[OBSERVED — 레인 B 추적, 본 레인 미재검증]`
- 레인 D가 §Workflow Context 3(텔레그래프가 속도를 올린다)을 **철도 원거리 신호기**에서 독립 확인했다:
  도입 이유가 "정지 신호의 가시 거리 내 속도로 운전할 필요가 없어져 전반적 속도가 증가"였다.
  게임(Into the Breach)과 철도가 같은 답에 도달했다. `[OBSERVED — 레인 D 1차 회수, 본 레인 미재검증]`
- 레인 D가 §Workflow Context 5(거짓 지시)와 같은 결론에 도달했다 — 문구 삭제가 아니라
  **참인 상태 표현으로 교체**. TCAS 7.1이 모호한 "Adjust Vertical Speed"를 지우지 않고
  "Level off"로 바꾼 사례가 근거다. `[OBSERVED — 레인 D 1차 회수, 본 레인 미재검증]`
- 레인 D의 "계산은 되는데 렌더 0" 3연속 패턴에서 **windText는 제외되어야 한다**(정정 1).
  남는 2건(SimpleAI 0.9초 창, `ProjectileForTurn`)으로도 패턴은 성립하며,
  오히려 "3건 중 1건은 이미 고쳤다"가 조치 가능성의 증거가 된다. 통보 완료.

---

## 이 레인이 확인하지 못한 것

1. **포트리스2 커뮤니티 1차 출처.** "각샷"·화면 등분 감각의 구체 증언은 선행 조사가
   `browser-rendered indexed snippet`으로 남긴 상태이며, 본 레인에서 1차 페이지로 승격하지 못했다.
2. **Hills of Steel Play 스토어 원본.** 패키지 ID 4종 시도 전부 404. `thin evidence`로 표기했다.
3. **castle-war 적 턴 실화면.** UX-015 — 캡처 부재. ⑥ 관련 서술은 코드 추적 의존.
4. **한국 커뮤니티(인벤·디시) 증언.** 검색 범위를 영어권 1차 출처로 한정했으므로 미수집.
   포병 경험자 행(§Affected Users)의 습관 서술이 이 공백에 가장 민감하다.
5. **④ 바람 표시의 런타임 확인.** 정정 1은 입양 코드 경로를 6개 링크로 추적한 결과이며
   **실행 화면에서 픽셀을 확인한 것이 아니다.** `windText.canvas != null` 같은 직접 단언이나
   `ux-4-enemy-turn` 캡처로 재확인해야 최종 확정된다. 그때까지 판정은 "조치됨(런타임 재확인 필요)"이다.
