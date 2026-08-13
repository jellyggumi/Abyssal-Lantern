# Solution Landscape: castle-war 가시성

- lane: B — 해법 지도 (장치 전수조사 + 빈도표)
- run: `siege-visibility-and-telegraph`
- 작성일: 2026-08-13
- 산출물 범위: **본 파일 1개.** 코드 수정 없음. `context.md` / `actual-lane-c.md` / `alternatives-lane-d.md`는 건드리지 않음.
- 선행 조사와의 관계: `.survey/siege-artillery-landscape/`(12개 비교작, 계보)와
  `_workspace/current/design/trend-survey/rally-structure-and-pikachu-volley.md`(D1~D9 관여장치)를
  **중복하지 않는다.** 선행 조사는 "관여(engagement) 장치"를 셌고, 본 조사는 **"정보 전달(visibility) 장치"** 를 센다.
  겹치는 항목은 D6(완전 텔레그래프) 하나이며, 본 문서는 그것을 **8개 하위 장치로 분해**한다.

---

## Solution List

**장치 18종.** 각 행의 `비고`에 증거 등급과 **castle-war 현재 보유 여부**를 함께 적었다.
castle-war 보유 판정은 전부 코드 실측 `[OBSERVED — 코드]`이다.

| Name | Approach | Strengths | Weaknesses | Notes |
|---|---|---|---|---|
| **V1. 적 의도 예고 (enemy intent telegraph)** | 적이 다음 턴에 **무엇을 할지**를 내 결정 전에 공개. Into the Breach는 대상·피해·공격종류를 전부, Slay the Spire는 적 머리 위 아이콘 + 피해 수치로 공개 | 적 턴이 "구경"에서 "풀어야 할 문제"로 바뀐다. 패배가 운이 아니라 내 실수로 읽힘 — Ma: *"every death felt like your own fault"* | UI 부담이 개발 기간의 절반을 먹었다는 개발자 증언 존재. 표시 못 할 메커니즘은 잘라내야 함 | `direct page retrieval` — gamedeveloper.com IGF 인터뷰 / RPS The Mechanic / StS Wiki. **castle-war: ❌ 없음** (`SimpleAI.cs:62` 계산 → `:74` 발사, 사이 0초) |
| **V2. 적 공격의 공간 표시 (공격 종류별 시각 문법)** | 공격 유형마다 **다른 선 모양**을 배정. ItB: 포물선=고리선(looping line), 직사=점선(dotted line), 근접=화살표 | 여러 적의 예고가 동시에 떠도 누가 무엇을 하는지 구분됨. 판을 "한눈에 파싱"(Davis) 가능 | 공격 유형 수를 3개로 강제 제한해야 성립. 별·삼각형 범위 공격은 이 문법으로 표현 불가라 폐기됨 | `direct page retrieval` — RPS. **castle-war: ❌** — 적 샷에 어떤 시각 문법도 없음 |
| **V3. 내 샷 궤적 프리뷰 (own trajectory preview)** | 커밋 전에 내 발사체의 포물선을 점선/실선으로 그림 | 포병 계보의 암기 장벽("각샷")을 제거. 모바일 진입 장벽의 직접 해답 | 내 샷만 보여주면 정보 비대칭이 **내 쪽에만** 생겨 적 턴 무료함은 그대로 | `direct page retrieval` — Angry Birds(Chillingo가 최종 폴리시에 "visible trajectory lines" 추가 주장), GunboundM(2017 "visible bullet path"). **castle-war: ✅ 보유** (`LaunchManager.cs:18-22`, 300스텝×0.02s=6초, 실전 물리 동일) |
| **V4. 결과 예측 수치 (pre-commit outcome numbers)** | 커밋 전에 피해량·명중률·잔여 HP를 수치로 제시. FE 전투예측창, XCOM 명중률 %, StS 의도 아이콘의 피해 숫자 | "위험을 알고 감수한 것"이 되어 피해가 공정하게 읽힘 | 수치가 노출되면 최적해가 계산 가능해져 플레이가 산수로 수렴할 수 있음 | FE/XCOM은 `thin evidence`(1차 위키 404, 선행 조사 §3.6·3.7 재인용), StS는 `direct page retrieval`. **castle-war: ❌** — 착탄 전 피해 예측 없음 |
| **V5. 위협 범위 오버레이 (danger zone)** | 적이 도달·타격 가능한 영역 전체를 색으로 덮음. 개별 적 / 전체 토글 | 위치 선정이 곧 전략이 됨("미끼" 플레이). 기억 부담을 UI로 외부화 | 격자 기반 게임에 최적화된 장치. 연속 공간·포물선 게임에는 그대로 이식 불가 | `thin evidence` — 1차 위키 404, 검색 종합 + 선행 조사 재인용. **castle-war: ❌** (연속 공간이라 직접 이식 부적합 `[INFERENCE]`) |
| **V6. 이전 샷 궤적 잔상 (previous-shot trace)** | 지난 샷이 그린 포물선을 화면에 남겨 다음 조준의 기준선으로 삼게 함 | **장르에서 가장 오래된 가시성 장치** (1980년 Apple II). 구현이 극히 저렴하고 예측이 아니라 기록이라 밸런스 영향 없음 | 이미 쏜 뒤에야 정보가 생김. 첫 샷은 여전히 맨눈 | `direct page retrieval` — en.wikipedia.org/wiki/Artillery_game: *"Some games used lines on the screen to show trajectories previous shots had taken"*. **castle-war: ▲ 부분** — `CannonShotVisuals.cs:10-12`가 궤적을 읽으라고 남기는 트레일 보유, 단 **화포 전용**이며 볼리 샷엔 없음 |
| **V7. 다음 무기 예고 (next weapon queue)** | 이번 것 말고 **다음에 무엇이 오는지** 미리 보여줌. Angry Birds는 새총 옆에 대기 중인 새들을 줄세워 표시 | 한 턴 앞을 계획하게 만들어 "지금 뭘 아껴야 하나"를 발생시킴 | 순서가 결정론적일 때만 성립 | `direct page retrieval` — AB 위키(새 종류가 진행에 따라 해금·레벨별 정해진 순서). **castle-war: ❌ 미표시 — 그런데 정보는 이미 결정론적으로 존재** (`OneShotSiegeRules.ProjectileForTurn`, 라운드마다 Knight→Archer→Barrel 순환). 코드 전역 grep 결과 이 값을 플레이어에게 보여주는 UI **0건** |
| **V8. 애니메이션 툴팁 (animated tooltip)** | 무기 설명을 문장이 아니라 **실제 유닛이 실제로 그 동작을 하는 작은 애니메이션**으로 보여줌 | Ma: *"a hundred times '...pushes adjacent tiles'"보다 "그 작은 애니메이션 하나가 천 배 효과적"*. Davis가 "가장 좋아하는 것" | 무기 수 × 상황 수만큼 동적 렌더링이 필요해 구현 비용이 큼 | `direct page retrieval` — RPS. AB도 2012년 업데이트에서 "animated tutorial" 추가. **castle-war: ❌** |
| **V9. 바람·환경 수치 표시** | 풍향·풍속을 화살표+수치로 상시 표시 | **포병 계보의 사실상 표준**(1982년 Artillery Duel부터). 물리에 개입하는 숨은 변수를 가시화 | 표시해도 그것이 궤적에 얼마나 영향을 주는지는 여전히 암산 | `direct page retrieval` — Artillery_game 위키(1980 Apple II 바람 계산, 1982 Artillery Duel "graphical readout of wind speed"). **castle-war: ✅ 보유** (`GameManager.cs:2293-2297`, `WIND >>> 2.3` / `WIND CALM`) |
| **V10. 각도·파워 수치 표시** | 조준 각도와 발사 강도를 숫자/게이지로 노출 | 포병 계보 전 타이틀 보유. 재현 가능한 조준(같은 값 = 같은 결과)의 전제 | 수치를 봐도 착탄점은 모름 — V3(궤적)이 없으면 반쪽 | `direct page retrieval` — Artillery_game 위키(초기작부터 "angle and power" 입력이 장르 정의). **castle-war: ✅ 보유** (`LaunchManager.launchStatsText`) |
| **V11. 턴 순서 타임라인 / delay 표시** | 다음에 누가 행동하는지를 자원화해 노출. Gunbound는 누적 delay가 낮은 쪽이 다음 턴 | 상대 턴에 **입력 없이 계산할 거리**를 준다. 구현이 턴 전환 규칙 변경뿐이라 저렴 | castle-war의 발사체 강제 순환(양 진영 동일)과 충돌 — 연속 턴이 생기면 대칭이 깨짐 | `direct page retrieval` — Gunbound 위키(*"delay turn system... using items or taking time with actions results in a longer wait"*). **castle-war: ❌**, 그리고 도입 시 규칙 충돌 있음 |
| **V12. 피해 숫자 팝업 (damage numbers)** | 착탄 순간 피해량을 숫자로 튀워 올림 | 본 표에서 **최빈 장치**. "내가 얼마나 잘했나"를 즉시 회신 | 결과 피드백일 뿐 예측이 아니다. 이것만으로는 "어떻게 해야 하는지"를 못 가르침 | `direct page retrieval`(StS 의도 수치) + 장르 통념. **castle-war: ✅ 보유** (`GameFeelVfx.SpawnDamageNumber`, `DestructibleBlock.cs:211`, `UnitController.cs:1081`) |
| **V13. 카메라 자동 추적** | 발사체·행동 주체를 카메라가 따라가 "어디서 뭐가 오는지" 강제로 보여줌 | 플레이어 조작 없이 시선을 옳은 곳에 둔다 | 따라가는 동안 다른 곳이 안 보임. 양쪽에서 동시에 일이 벌어지면 실패 | 장르 통념 + `direct page retrieval`(castle-war 코드). **castle-war: ✅ 보유** (`GamePresentationDirector.cs:98` followLerp 추적) |
| **V14. 리플레이 / 결정적 순간 재생** | 턴 종료 후 방금 일어난 일을 다시 보여줌 | 놓친 인과를 사후 복구. 관전·공유 가치 부수 획득 | 사후 장치라 **의사결정에 기여하지 않는다**. 경기 길이를 늘림 | `thin evidence` — Worms 리플레이는 위키 본문 미확인, 통념 수준. **castle-war: ▲** — 착탄 후 홀드 0.35초(`PostImpactHoldSeconds`)가 최소 형태 |
| **V15. 단계별 온보딩 코치** | 첫 판에 단계별 지시 + 대상을 가리키는 화살표 + 턴 타이머 정지 | 규칙 자체를 모르는 상태를 해소. 1회성이라 숙련자에게 비용 0 | **판이 매 턴 바뀌는 이유는 못 가르친다.** 규칙 교습이지 상황 가시성이 아님 | `direct page retrieval`(AB 위키 "animated tutorial"; Worms 위키 "series of training missions") + 코드. **castle-war: ✅ 보유** (`FirstPlayCoachController`, 단계 배너 + 월드 화살표 + 턴클럭 홀드, 프로필당 1회) |
| **V16. 예고 제거를 난이도 자원으로 판매** | 정보를 **빼앗는 것**을 유물/모드로 제공. StS `Runic Dome`은 적 의도를 못 보게 만드는 대신 강력한 보상 | 정보량을 난이도 축으로 전환. 숙련자용 상향 난이도를 콘텐츠 추가 없이 확보 | 기본값이 "보여줌"일 때만 성립하는 역방향 장치 | `direct page retrieval` — StS Wiki Intent 문서(*"The relic Runic Dome renders the player unable to see the intent"*). **castle-war: ❌** (기본값이 이미 "안 보여줌"이라 팔 것이 없음) |
| **V17. 위협 압력 카운터 (incoming pressure meter)** | 들어올 공격의 **총량**을 수치로 미리 공개 (뿌요뿌요 예고뿌요 카운터 계열) | 총량만 공개하므로 V1보다 정보량이 적고 구현이 싸다. "막을까 지를까" 결정 발생 | 어디에 맞는지는 모르므로 포물선 게임에선 정보가 얕음 | 선행 조사 §3.5 재인용, `thin evidence`. **castle-war: ❌** |
| **V18. 환경 위험의 사전 경고 (hazard telegraph)** | 지형·기믹이 발동하기 전에 색·라벨·진동으로 예고 | 예고 창을 두면 회피가 실력이 된다. 이미 castle-war가 **이 패턴을 알고 구현해 둔 상태** | 적 행동이 아니라 환경에만 적용됨 | `direct page retrieval`(코드). **castle-war: ✅ 보유** — `EruptionVentGimmick`: Dormant→**Warning(1.8초, 색 맥동 + "RUMBLE..." 라벨)**→Erupting; `MovingGimmick.cs:54` `phaseTelegraphDelaySeconds = 0.45f` 주석에 *"appliedPhase now lags lastPhase so the warning genuinely precedes the hazard"* |

---

## Categories

장치 18종은 **정보가 도착하는 시점**을 기준으로 네 무리로 갈린다.
이 축이 중요한 이유는, castle-war가 가진 장치와 없는 장치가 **정확히 이 선을 따라 갈리기** 때문이다.

**A. 예측 정보 — 내가 결정하기 *전*에 도착** (V1, V2, V4, V5, V7)
→ 플레이어의 **결정 품질**을 바꾼다. 유일하게 "무엇을 해야 하는가"에 답하는 무리.
→ **castle-war 보유: 0 / 5.**

**B. 조준 보조 — 내가 결정하는 *중*에 도착** (V3, V6, V9, V10)
→ 내 의도를 결과로 번역해 준다. 포병 계보가 20년에 걸쳐 이쪽으로 이동해 온 무리.
→ **castle-war 보유: 3.5 / 4** (V3·V9·V10 완비, V6는 화포 전용 부분 보유).

**C. 결과 피드백 — 결정 *후*에 도착** (V12, V13, V14)
→ 무슨 일이 있었는지 회신한다. 만족감을 만들지만 **다음 결정을 못 바꾼다.**
→ **castle-war 보유: 2.5 / 3.**

**D. 교습·메타 — 판 바깥에서 도착** (V8, V11, V15, V16, V17, V18)
→ 규칙과 난이도를 다룬다.
→ **castle-war 보유: 2 / 6** (V15 온보딩, V18 환경 예고).

> **분류가 내놓은 한 줄**:
> castle-war의 결손은 균일하지 않다. **B·C(내 행동에 관한 정보)는 거의 완비돼 있고,
> A(상대 행동에 관한 정보)만 0이다.**
> 사용자가 말한 두 증상 — *"내 돌을 어떻게 써야 하는지 안 보인다"* 와 *"적이 어떻게 쏘는지 안 보인다"* — 는
> 같은 결핍이 아니다. 후자는 **장치 부재(A군 0/5)**, 전자는 **장치는 있는데 도달하지 않는 문제**다.
> 전자의 성격은 Lane C가 다룬다.

---

## What People Actually Use

**1. 장르가 갈린다 — 예고는 전술 계보의 문법이고, 포병 계보는 쓰지 않는다.**
표본 12개를 계보로 나누면 A군(전술: ItB, StS, FE, XCOM2)은 적 정보 장치를 평균 3개 이상 갖고,
B군(포병: Worms, Gunbound, GunboundM, 포트리스2, Scorched Earth, ShellShock)은 **V1·V2·V4·V5가 전부 0**이다.
포병 계보가 표시하는 것은 오로지 **바람·각도·파워** — 즉 *내 조준의 입력 변수*뿐이다.
**castle-war는 B군의 정보 태도를 그대로 물려받았다.**

**2. 포병 계보는 "보여주는 쪽"으로 이동 중이지만, 이동한 것은 B군(조준 보조)뿐이다.**
- 1980년 Apple II 세대: 이전 샷 궤적선 등장 (V6)
- 1982년 Artillery Duel: 풍속 그래픽 표시 등장 (V9)
- 2009년 Angry Birds: Chillingo가 최종 폴리시에 **궤적선** 추가 (V3)
- 2017년 GunboundM: 15년 된 원작에 없던 **탄도 가이드** 추가 (V3)

37년에 걸쳐 네 번 개선됐고 **네 번 모두 "내 샷을 더 잘 보이게" 하는 방향**이었다.
**적을 보여주는 방향으로 간 사례는 이 계보에 없다.** `[OBSERVED 0/6 — B군 표본]`

**3. 전술 계보는 예고를 사후 추가가 아니라 설계 전제로 깔았다.**
Into the Breach는 Davis가 *"처음부터 UI 악몽을 각오하고 있었다"*(원문: *"I think we were resolved to
having a UI nightmare from the beginning"*)고 말할 만큼 대가를 알고도 그대로 갔다.
Slay the Spire는 초기 버전에 의도 표시가 **없었다가** 이후 도입된 것으로 알려져 있다 —
없을 때 플레이어가 적 패턴을 **암기**해야 했다는 설명이 반복되나,
이 도입 경위는 **1차 출처를 회수하지 못했다** `[thin evidence]`.
확실한 것은 **결과**다 — 두 게임 모두 최종적으로 적 의도를 공개하는 쪽을 택했고(V1, direct 2/2),
이는 포트리스 유저가 "각샷"을 외우는 상태와 정반대 방향이다.

**4. 가장 널리 쓰이는 장치는 정작 문제를 못 푼다.**
피해 숫자(V12) 9/12, 바람 표시(V9) 7/12, 각도·파워(V10) 6/12 — 상위 3개가 전부
**castle-war가 이미 가진 것들**이다. 흔한 장치를 다 갖췄는데도 가시성이 나쁘다는 것이
이 조사의 출발점이자, 흔한 장치가 답이 아니라는 증거다.

---

## Frequency Ranking

### 표본 게임 12종 (명시)

| # | 게임 | 계보 | 근거 등급 |
|---|---|---|---|
| 1 | **Into the Breach** (2018, Subset Games) | A 턴제 전술 | `direct page retrieval` ×3 (Wikipedia / gamedeveloper.com / RPS) |
| 2 | **Slay the Spire** (2019, Mega Crit) | A 턴제 전술 | `direct page retrieval` (StS Wiki: Intent) |
| 3 | **Fire Emblem** 시리즈 | A 턴제 전술 | `thin evidence` (1차 위키 404 ×3, 검색 종합 + 선행 조사 §3.7) |
| 4 | **XCOM 2** (2016, Firaxis) | A 턴제 전술 | `thin evidence` (선행 조사 §3.6 재인용) |
| 5 | **Worms Armageddon** (1999, Team17) | B 턴제 포병 | `direct page retrieval` (Wikipedia) |
| 6 | **Gunbound** 원작 (2002, Softnyx) | B 턴제 포병 | `direct page retrieval` (Wikipedia) |
| 7 | **GunboundM** (2017, DargomStudio) | B 턴제 포병 | `direct page retrieval` (Wikipedia: *"visible bullet path"*) |
| 8 | **포트리스2** (1999, CCR) | B 턴제 포병 | `direct page retrieval` (선행 조사 인용) |
| 9 | **Scorched Earth** (1991, Wendell Hicken) | B 턴제 포병 | `direct page retrieval` (Artillery_game 위키) |
| 10 | **ShellShock Live** (2015) | B 턴제 포병 | `indexed snippet` (Artillery_game 위키에 계보로 등재) |
| 11 | **Angry Birds** (2009, Rovio) | C 물리 파괴 | `direct page retrieval` (Wikipedia) |
| 12 | **Crush the Castle** (2009, Armor Games) | C 물리 파괴 | `direct page retrieval` (선행 조사 인용) |

> ⚠️ **표본 한계 2가지.**
> ① Angry Birds / Crush the Castle는 **적 턴이 존재하지 않는다.** V1·V2·V4·V5·V11에 대해
> "없음"이 아니라 **해당없음**이며, 아래 분모 12에는 포함하되 표에 명기한다.
> ② Gunbound를 원작/M으로 분리한 것은 의도적이다 — **같은 게임이 15년 뒤 장치를 추가한 사례**라
> "장르가 어느 방향으로 움직이는가"의 직접 증거가 되기 때문이다.

### 빈도 순위

> **등급 규칙 (본 표에만 적용).** 과제가 요구한 `[OBSERVED n/m]` 표기를 쓰되,
> **어떤 근거로 셌는지를 행마다 분리**한다. 이 구분을 생략하면 아래 Curated Sources의
> "검색 요약을 인용으로 쓰지 말라"는 경고를 본 표가 스스로 위반하게 된다.
> - `direct` = 해당 타이틀의 1차 페이지를 본 런에서 직접 회수해 확인한 칸
> - `convention` = 장르 통념·플레이 경험 기반 판정. **본 런에서 타이틀별로 회수하지 않음**
> - 두 근거가 섞인 행은 `direct n + convention m` 으로 쪼개 적는다.

| 순위 | 장치 | 출현 | 근거 분해 | 보유 타이틀 | castle-war |
|---|---|---|---|---|---|
| 1 | **V12 피해 숫자 팝업** | `[OBSERVED 9/12]` | direct 1 (StS) + **convention 8** | ItB, StS, FE, XCOM2, Worms, Gunbound, GunboundM, 포트리스2, ShellShock | ✅ |
| 2 | **V9 바람·환경 수치** | `[OBSERVED 7/12]` | direct 4 (Gunbound·GunboundM 위키 "wind currents"; SE·ShellShock는 Artillery_game 위키 계보 서술) + convention 3 | Worms, Gunbound, GunboundM, 포트리스2, SE, ShellShock, ItB(환경 아이콘) | ✅ |
| 3 | **V10 각도·파워 수치** | `[OBSERVED 6/12]` | **direct(장르 수준)** — Artillery_game 위키가 "angle and power"를 장르 정의로 명시. 타이틀별 개별 회수는 아님 | Worms, Gunbound, GunboundM, 포트리스2, SE, ShellShock | ✅ |
| 3 | **V13 카메라 자동 추적** | `[OBSERVED 6/12]` | **convention 6** — 본 런 회수 0건 | Worms, Gunbound, GunboundM, 포트리스2, ShellShock, XCOM2 | ✅ |
| 5 | **V15 단계별 온보딩** | `[OBSERVED 5/12]` | direct 2 (Worms 위키 "series of training missions"; AB 위키 "animated tutorial") + convention 3 | AB, ItB, StS, FE, Worms | ✅ |
| 6 | **V4 결과 예측 수치** | `[OBSERVED 4/12]` | direct 1 (StS 의도 아이콘 피해 수치) + convention 3 (FE는 1차 404, XCOM2·ItB 미회수) | ItB, StS, FE, XCOM2 — **전부 A군** | ❌ |
| 7 | **V5 위협 범위 오버레이** | `[OBSERVED 3/12]` | **convention 3** — FE 1차 404 ×4, 미회수 | ItB, FE, XCOM2(부분) — **전부 A군** | ❌ |
| 8 | **V3 내 샷 궤적 프리뷰** | `[OBSERVED 2/12]` | **direct 2** — AB 위키(Chillingo "visible trajectory lines"), GunboundM 위키("visible bullet path") | Angry Birds, GunboundM | ✅ |
| 8 | **V1 적 의도 예고** | `[OBSERVED 2/12]` (+1 부분, 해당없음 2) | **direct 2** — ItB 위키·RPS·IGF 인터뷰, StS Wiki Intent | ItB, StS (+FE 부분) | ❌ |
| 8 | **V8 애니메이션 툴팁** | `[OBSERVED 2/12]` | **direct 2** — RPS(ItB 동적 툴팁), AB 위키(2012 animated tutorial) | ItB, Angry Birds | ❌ |
| 8 | **V11 턴 순서 타임라인** | `[OBSERVED 2/12]` | **direct 2** — Gunbound 위키 delay 시스템 원문 | Gunbound, GunboundM | ❌ |
| 12 | **V6 이전 샷 궤적 잔상** | `[OBSERVED 2/12]` (+2 부분) | **direct(장르 수준)** — Artillery_game 위키: *"Some games used lines... trajectories previous shots had taken"*. ⚠️ 위키는 **1980년 Apple II 세대**를 가리키며 SE·ShellShock 개별 명시가 아님 → 타이틀 귀속은 `[INFERENCE]` | SE, ShellShock (+포트리스2, Worms 부분) | ▲ 화포 전용 |
| 12 | **V2 적 공격의 공간 문법** | `[OBSERVED 1/12]` | **direct 1** — RPS(고리선/점선/화살표) | Into the Breach 단독 | ❌ |
| 12 | **V7 다음 무기 예고** | `[OBSERVED 1/12]` | direct 1 (AB 위키: 새 종류 해금·레벨별 정해진 순서). ⚠️ "새총 옆 대기열 UI"의 화면 표시 자체는 위키 미명시 → 표시 형태는 `[INFERENCE]` | Angry Birds 단독 | ❌ *(정보는 존재)* |
| 12 | **V16 예고 제거 = 난이도** | `[OBSERVED 1/12]` | **direct 1** — StS Wiki: *"Runic Dome renders the player unable to see the intent"* | Slay the Spire 단독 | ❌ |
| 12 | **V18 환경 위험 사전 경고** | `[OBSERVED 1/12]` | convention 1 (ItB 화염·A.C.I.D. 타일 표시는 RPS에 서술되나 "사전 경고" 프레이밍은 본 조사의 해석) | Into the Breach | ✅ |
| 17 | **V14 리플레이 / 재생** | `[thin evidence 1/12]` | 회수 실패 — Worms 위키 본문에 리플레이 서술 없음 | Worms(미확인), XCOM2(부분) | ▲ 0.35초 홀드 |
| 17 | **V17 위협 압력 카운터** | `[thin evidence 0/12]` | 표본 밖, 선행 조사 §3.5 재인용 | 뿌요뿌요 계열 | ❌ |

> **이 표에서 가장 신뢰도가 높은 구간이 하필 결론 구간이다.**
> `direct`만으로 채워진 행은 V1·V2·V3·V8·V11·V16 여섯 개인데,
> 그중 **V1·V2·V16이 castle-war 결손 항목**이고 **V3이 보유 항목**이다.
> 반대로 `convention` 비중이 큰 행(V12·V13·V5)은 결론에 영향을 주지 않는다 —
> 전부 이미 보유했거나(V12·V13) 격자 전용이라 이식 부적합(V5)이기 때문이다.
> **즉 근거가 약한 칸들이 결론을 떠받치고 있지 않다.**

### 계보별 교차 집계 — 이 조사에서 가장 중요한 숫자

**A군(적 정보) 장치 V1·V2·V4·V5의 계보별 출현:**

| 계보 | 표본 수 | V1 | V2 | V4 | V5 | 합계 |
|---|---|---|---|---|---|---|
| A 턴제 전술 | 4 | 2 (+1▲) | 1 | **4** | 3 | **10 / 16** |
| B 턴제 포병 | 6 | **0** | **0** | **0** | **0** | **0 / 24** |
| C 물리 파괴 | 2 | 해당없음 | 해당없음 | 해당없음 | 해당없음 | — |
| **castle-war** | — | ❌ | ❌ | ❌ | ❌ | **0 / 4** |

> **`[OBSERVED 0/6]` — 포병 계보 6개 타이틀 중 적 정보 장치를 하나라도 가진 것은 0개다.**
> 이것이 castle-war가 나쁜 게임이라서 생긴 결손이 **아니라는** 증거인 동시에,
> 계보를 따라가는 한 해결되지 않는다는 증거이기도 하다.
>
> **G8 임계값 대조** (novelty-scorecard 기준: *"≥5개 비교작 중 ≤2개 출현"*):
> V1(2/12)·V2(1/12)·V7(1/12)·V16(1/12)·V8(2/12)·V11(2/12)이 모두 통과한다.
> 즉 **적 예고는 castle-war에서 "결손 보완"이면서 동시에 "참신성 후보"** 다.
> 특히 **V2(1/12)와 V7(1/12)** 이 가장 희귀하며, 뒤에서 보듯 V7은 구현 비용이 가장 낮다.

---

## Key Gaps

**1. A군(예측 정보) 5종 전부 부재 — 그리고 이는 계보 전체의 결손이다.**
castle-war가 가진 가시성 장치 **8종**(V3·V6▲·V9·V10·V12·V13·V15·V18 — 완전 보유 7 + 부분 1)은 **전부 B·C·D군**이다.
"적이 어떻게 쏘는지 안 보인다"는 사용자 진술은 **정확히 A군 0/5의 체감**이다.

**2. 예고 창이 시간상 이미 존재하는데 정보가 비어 있다 — 그리고 비어 있는 이유가 순서다.**
`SimpleAI.PerformLaunch()`를 실측하면 `[OBSERVED — 코드]`:

```
:28-29 주석: "Half of the 0.9s AI beat (GameManager.ExecuteAITurn holds the other 0.4s) —
              enough of a pause to read as the enemy taking aim, not a wait."
:30    yield return new WaitForSeconds(0.5f);   ← 대기가 먼저
:31    targetPos = FindTargetPosition() + 랜덤오차   ← 조준 대상이 대기 후에 정해진다
:34    prefab = gm.AutomaticProjectilePrefab          ← 발사체 선택도 대기 후 (단서: 아래 참조)
:62    desiredFinalVelocity = CalculateLaunchVelocity(...)  ← 속도 계산도 대기 후
:74    unit.Launch(velocity)                          ← :62에서 여기까지 yield 0개
```

> **결정적인 것은 `:28-29` 주석이다.** 코드가 그 0.5초를 비워 둔 목적을 스스로 적어 두었다 —
> *"적이 조준하는 것으로 **읽히게** 하려고, 기다림이 아니라(not a wait)."*
> 그런데 **그 0.5초 동안 화면에 조준을 나타내는 것이 하나도 없고**, 조준값 자체가 그 뒤에 계산된다.
>
> **→ ⑥(적 예고)의 부재는 설계 결정이 아니라 의도와 구현 사이에 벌어진 틈이다.** `[INFERENCE]`
> 이 구분이 실무적으로 중요하다: 예고 추가는 **"새 기능 도입"이 아니라 "미완성 의도의 완성"** 이며,
> 코드가 이미 그 의도를 문서화해 두었으므로 설계 논쟁의 출발점이 다르다.
> (이 프레이밍은 Lane A가 제시했고, 본 레인이 `:28-29` 원문으로 확인했다.)

> **시간 예산**: `GameManager.cs:2159`의 0.4초를 더하면 예고 예산은 이미 **0.9초** 확보되어 있고,
> 이 시간은 **이미 경기 길이 300초 예산에서 지불되고 있다.**
> 요점은 "0.9초를 늘리자"가 **아니다** — 그 창이 이미 있고 이미 지불됐는데
> **표시할 값이 아직 계산되지 않았을 뿐**이다. 순서 교체의 추가 시간 비용은 0이다. `[INFERENCE]`

> **⚠️ `:34`에 대한 정밀 단서 (본 레인 확인).** 위 목록에서 `:34`는 실행 순서상 대기 뒤에 오지만
> **나머지 둘과 성격이 다르다.** `GameManager.AutomaticProjectilePrefab`은
> `OneShotSiegeRules.ProjectileForTurn(turnCount)` 위의 **순수 getter**로,
> `turnCount` 외에 아무것도 참조하지 않는다(`GameManager.cs:2078-2093`). `[OBSERVED — 코드]`
> 즉 **발사체는 대기 전에도, 사실은 턴 시작 시점에도 이미 알 수 있다** — 재배치가 필요 없다.
> 순서 교체가 실제로 필요한 것은 **`:31` 대상과 `:62` 속도** 둘뿐이다.
>
> 이 구분이 오히려 유리하다. 예고를 **2단으로 쪼갤 수 있기 때문**이다:
> **발사체 종류(V7)는 턴 시작 즉시** — 재배치 0, 계산 0, 이미 public —
> **대상·궤적(V1·V2)은 0.9초 창 안에서** — `:31`·`:62`만 대기 앞으로.
> 두 단계의 구현 비용이 다르므로 한 덩어리로 묶어 판단할 필요가 없다. `[INFERENCE]`

**3. 결정론적으로 존재하는 정보가 표시되지 않는다 (V7).**
`OneShotSiegeRules.ProjectileForTurn(turnCount)`는 라운드마다 Knight→Archer→Barrel을
**양 진영 동일하게** 순환시킨다. 즉 다음 턴 발사체는 **이미 계산 가능한 확정값**이다.
그런데 이 값을 플레이어에게 보여주는 UI는 코드 전역 grep 결과 **0건**이다. `[OBSERVED — 코드]`
V7은 표본에서 1/12로 가장 희귀한 축이면서, castle-war에서는 **새 정보를 만들 필요조차 없는** 장치다.

**4. 자기 예고 패턴을 이미 알고 있으면서 적에게만 적용하지 않았다 (V18 → V1).**
`EruptionVentGimmick`은 Dormant→**Warning(1.8초 색 맥동 + 라벨)**→Erupting 3단계를 갖고,
`MovingGimmick.cs:54`는 *"the warning genuinely precedes the hazard"* 를 위해
0.45초 지연을 **일부러 넣었다**. 프로젝트는 예고의 필요성·구현법·적정 길이를 전부 안다.
**환경 위험에는 적용했고 적 포격에는 적용하지 않았다.** 기술 격차가 아니라 적용 범위 격차다.

**5. 표시가 거짓말을 하는 사례는 비교작 12개 어디에도 없다.**
`SiegeAlarmSystem.cs:225`는 적 턴 내내 `"적 포격 준비 중...  ·  클릭: 벽돌 예약"`을 띄우는데
`BrickPlacementController.cs:76-82`가 `EnforcesOneShotTurns`일 때 early-return으로 막는다.
`LaunchManager.cs:121`의 `"아무 곳이나 당겨 발사"`는 적 턴에도 남지만 조준은 3중으로 차단돼 있다.
**이것은 A군 결손과 다른 종류의 결함이다.** 장치가 없는 것이 아니라 **있는 장치가 틀린 것**이며,
가시성 개선 이전에 선행 처리되어야 한다 — 예고를 추가해도 거짓 지시가 남으면 신뢰가 회복되지 않는다. `[INFERENCE]`

**6. 이력 경고 — castle-war에는 "계산했으나 아무도 못 본" 전례가 있다. 그리고 그 전례는 이미 고쳐졌다.**
`GameManager.cs:1124-1128` 주석이 기록한다: `windText`와 `scoreText`가 Canvas 조상이 없어
*"UpdateUI kept writing to them every turn — wind strength and the running score were computed
and formatted for an audience of nobody."* `[OBSERVED — 코드 주석]`
**장치 보유와 정보 도달은 별개의 문제이며, 이 프로젝트는 이미 그 간극에 한 번 빠졌다.**
A군 장치를 추가할 때 같은 실패가 재발할 수 있다.

> **⚠️ 현재 상태 정정 (2026-08-13, Lane A와 교차 검증 완료).**
> QA 결함표(`qa/ux-defect-list.md` UX-001)는 바람이 **"화면에 절대 표시되지 않는다"** 고 S1으로 올렸고,
> 그 근거는 씬 파일의 `m_Father: {fileID: 0}`이다. **이 판정은 발견 시점의 것이며 현재는 유효하지 않다.**
> 런타임 입양 경로가 스킵 분기 없이 끝까지 추적된다 `[OBSERVED — 코드]`:
>
> `GameManager.cs:309 Start()` → `:321 SetupUIButtons()` → `:1129 HudCanvas.Adopt(windText)`
> → `HudCanvas.cs:112-134` **진짜 재부모화**(`rect.SetParent(root, false)`, 앵커·피벗·앵커드포지션·사이즈 앞뒤 보존)
> → 조기 반환 조건은 `rect.parent == root` 뿐이고 windText의 부모는 null이므로 **통과**
> → `Root()` → `MobileSafeArea.GetContentRoot(Resolve())`, `Resolve()`가 캔버스를 실제로 생성(`HudCanvas.cs:51-90`)
> → 값 갱신 생존: `GameManager.cs:2294` `WIND >>> 2.3` / `WIND CALM`, `:2297` 3.5 이상 경고색.
> 씬 파일에 부모가 없는 것은 **런타임 입양이 존재하는 이유**이지 렌더 부재의 증거가 아니다.
> `gate-reviews/stage1-rally-arbitration.md:137`도 이 건을 조치 완료로 올려 두었다.
>
> **증거 대칭성 판정**: 양쪽 모두 Unity를 돌리지 못했으므로(하네스 규칙) 어느 쪽도 픽셀을 보지 못했다.
> 그러나 두 주장은 **대등하지 않다** — 씬 정적 구조는 런타임 재부모화를 반박할 수 없는 반면,
> 입양 경로는 분기마다 추적되어 끊기는 지점이 없다. 따라서 "조치됨"이 "렌더 0"보다 강한 주장이다.
> 남은 정직한 표현은 **"조치됨, 런타임 재확인 필요"** 이며 `[OBSERVED — 코드]` + `[INFERENCE — 픽셀 미확인]`이다.
>
> 이 정정은 Lane A가 **본 레인의 근거를 신뢰하지 않고 6단계를 독립 재현**해 확인했다.
> 따라서 Solution List의 **V9 "castle-war ✅ 보유" 판정은 2개 레인 독립 교차 검증**을 거친 항목이다.
> Lane A는 이에 따라 자기 문서의 결론을 수정했다 — *"모호함 없는 공백은 ⑥(적 예고) 하나"*.
> **본 레인의 A군 0/5 진단과 정확히 일치한다.**

---

## Contradictions

**1. "예고를 넣으면 턴이 길어진다"는 직관이 유일한 1차 증거와 정반대다.**
Into the Breach 위키가 개발자 인터뷰를 인용해 기록한다:
*"The limited turn counter was used to keep battles short, and Subset found that
**telegraphing the Vek's movements further helped to hasten the pace**."*
`[direct page retrieval — en.wikipedia.org/wiki/Into_the_Breach]`
→ **예고는 경기를 늘린 게 아니라 줄였다.** 300초 밴드(270~330) 제약이 예고 도입의 반대 근거로
쓰일 수 없다는 뜻이며, 오히려 유일한 실증은 반대 방향을 가리킨다.
단, ItB는 격자 전술이고 castle-war는 연속 물리라 그대로 이전되지 않는다. `[INFERENCE]`

**2. 완전 텔레그래프의 진짜 청구서는 UI 작업이 아니라 *메커니즘 삭감*이다.**
Ma: *"Our requirement that the player has to understand what's going on in any situation
**restricted our game design options considerably**."*
그 결과 공격 유형은 3종(근접/직사/포물)으로 강제 제한됐고, 별·삼각형 범위 공격,
얼음 위 밀기 연쇄, 규칙을 어기는 바위 무기가 전부 **재미있다고 인정하면서도** 잘려나갔다.
Ma의 원칙: *"we would sacrifice cool ideas for the sake of clarity every time."*
`[direct page retrieval — RPS]`
→ **castle-war에 이 원칙을 적용하면 기믹(EventGate, Chariot, EruptionVent, BuffDebuff)이
삭감 후보가 된다.** 예고 도입은 순수 추가가 아니라 **교환**이다. 이 대가는 명시돼야 한다.

**3. 정보 공개는 되돌릴 수 있지만, 정보 은폐는 상품이 안 된다.**
Slay the Spire는 `Runic Dome`으로 **의도 표시를 없애는 대신 보상을 주는** 유물을 판다.
기본값이 "보여줌"이기에 "안 보여줌"이 난이도 콘텐츠가 된다.
→ castle-war는 기본값이 이미 "안 보여줌"이라 **팔 것이 없다.**
현재 상태는 난이도가 아니라 그냥 정보 부재다. 방향을 뒤집으면 난이도 축을 하나 얻는다. `[INFERENCE]`

**4. 예고 과잉은 실재하는 실패 모드다 — 그리고 해법이 반직관적이다.**
Davis: *"One and a half years ago the game was just an icon mess."*
플레이테스터가 정보를 놓칠 때마다 아이콘을 추가한 결과였다.
그들의 해결책은 **표시를 줄이는 것이 아니라 표시할 대상 자체를 줄이는 것**이었고,
타일당 효과를 1개로 제한한 규칙도 *"둘 다 화면에 표시하는 게 불가능해서"* 생겼다.
`[direct page retrieval — RPS]`
→ **이 항목의 심화는 Lane C 소유다.** 본 레인은 증거만 남기고 판단을 넘긴다 (2026-08-13 IRC 전달 완료).

**5. 개발자 본인이 자기 해법을 최적이라고 생각하지 않는다.**
Ma: *"I don't even think we have the best solution. This could be way better.
But it's entirely functional."* / Davis: *"I think we brute-forced this into something that works."*
→ 장르 최고 사례조차 **무차별 반복으로 도달한 결과**다. castle-war가 1회 설계로
정답을 낼 것이라 기대하면 안 되며, **반복 예산**이 계획에 포함돼야 한다. `[INFERENCE]`

**6. 표본 내부 모순: 가장 흔한 장치를 다 갖춘 게임이 가장 안 보인다.**
castle-war는 상위 5개 빈도 장치 중 **5개 전부**를 보유한다(V12·V9·V10·V13·V15).
그럼에도 사용자 진술은 "가시성이 안 좋다"이다.
→ **빈도는 필요성의 지표가 아니다.** 흔한 장치는 흔하기 때문에 흔한 것이지
이 문제를 풀기 때문에 흔한 것이 아니다. 빈도 하위권(V1 2/12, V2 1/12, V7 1/12)에
답이 몰려 있다는 것이 이 표의 역설적 결론이다.

---

## Key Insight

**castle-war는 포병 계보의 정보 태도를 물려받았는데, 그 태도를 정당화하던 조건을 스스로 제거했다.**

포병 계보가 적 정보를 숨긴 것은 게으름이 아니었다. 그 계보에서 **조준 추정 자체가 실력**이었기 때문이다.
포트리스 유저가 "각샷"을 외우고, Gunbound 유저가 바람 차트를 암기한 것이 그 증거다.
정보를 숨기는 대가로 **암기와 감각이라는 실력 축**을 얻었다 — 은폐에 수익이 있었다.

castle-war는 그 수익을 이미 포기했다.

- **궤적 프리뷰가 실전 물리와 동일하다** (`LaunchManager`, 동일 적분기, 300스텝, 착탄까지)
  → 내 샷의 추정 실력은 이미 0이다. 외울 것이 없다.
- **발사체가 선택이 아니라 규칙이다** (`ProjectileForTurn`, 양 진영 동일 순환)
  → 무엇이 올지 고르는 실력도 없다.
- **한 턴 = 한 발이다** (`OneShotTurnGate`)
  → 자원 배분 실력도 없다.

> **즉 castle-war는 은폐의 대가를 지불하면서 은폐의 수익은 전부 반납한 상태다.**
> 적 정보를 숨겨서 지켜지는 실력 축이 하나도 남아 있지 않다.
> 남은 것은 비용뿐이다 — 경기의 34.1%(109.7초) 동안 플레이어가 볼 것도 할 것도 없다는 비용.

이것이 A군 0/5가 **계보의 유전이면서 동시에 castle-war 고유의 모순**인 이유다.
포병 계보에서 정보 은폐는 실력 축과 짝을 이룬 거래였다. castle-war에서는 짝이 사라진 잔재다.

**그리고 이 조사가 찾은 가장 실용적인 사실은, 되돌리는 데 필요한 것이 대부분 이미 있다는 것이다:**

| 필요한 것 | 현재 상태 | 근거 |
|---|---|---|
| 예고할 시간 | **이미 0.9초 확보, 이미 지불 중** | `SimpleAI.cs:30` 0.5s + `GameManager.cs:2159` 0.4s |
| **예고하겠다는 의도** | **이미 코드에 문서화됨** — *"적이 조준하는 것으로 읽히게, 기다림이 아니라"* | `SimpleAI.cs:28-29` 주석 |
| 예고할 정보(대상·속도) | 계산 가능. 단 **계산 시점이 예고 창보다 늦음** — `:31`·`:62`만 앞으로 | `SimpleAI.cs:31`·`:62`가 `:30` 이후 |
| 예고할 정보(발사체) | **완전 결정론. 순수 getter라 재배치조차 불필요. 표시 UI만 0건** | `GameManager.cs:2078-2093` → `ProjectileForTurn` |
| 궤적을 그리는 기술 | **보유. 실전 물리와 동일** | `LaunchManager.cs:18-22` |
| 예고 UI 패턴 | **보유. 환경 위험에 이미 적용** | `EruptionVentGimmick` Warning 1.8초 |
| 궤적 잔상 렌더러 | **보유. 화포 전용** | `CannonShotVisuals.cs:10-12` |

> **결론: 이것은 "없는 것을 만드는 문제"가 아니라 "있는 것을 적 쪽으로 겨누는 문제"에 가깝다.**
> 표의 둘째 행이 그 이유를 가장 압축해서 보여준다 — **의도조차 이미 코드에 적혀 있다.**
> `SimpleAI.cs:28-29`는 그 0.5초가 *"적이 조준하는 것으로 읽히게 하기 위한"* 것이라고 스스로 선언하는데,
> 그 창에서 조준을 나타내는 것은 아무것도 그려지지 않는다.
> **따라서 적 예고는 신규 기능 제안이 아니라 미완성 의도의 완성으로 제시하는 것이 정확하다.** `[INFERENCE]`
>
> 단, 되돌림이 균일하게 싼 것은 아니다. 위 표는 **2단으로 갈린다**:
> **1단 — 발사체 예고(V7)**: 재배치 0, 계산 0, 이미 public. 사실상 표시만 하면 된다.
> **2단 — 대상·궤적 예고(V1·V2)**: `:31`·`:62`를 대기 앞으로 옮기는 실제 순서 변경이 필요하고,
> 무엇을 얼마나 보여줄지(전체 궤적 / 착탄점만 / 방향만)가 밸런스 결정이 된다.
>
> 다만 Contradiction 2가 경고하듯 대가가 있다 — 예고 가능한 것만 남기려면
> 현재 기믹 일부는 삭감 후보가 된다. 그 판단은 설계 레인의 몫이며 본 조사의 범위가 아니다.
>
> **그리고 Key Gap 5가 우선한다.** 화면이 거짓을 말하는 동안에는 참을 추가해도 신뢰가 회복되지 않는다.

---

## Curated Sources

### 1차 설계 자료 (과제 요구: ≥3개)

| # | 출처 | URL | 등급 | 이 조사에서의 용도 |
|---|---|---|---|---|
| **S1** | **Rock Paper Shotgun — "The Mechanic: Into the Breach's interface was a nightmare to make and the key to its greatness"** (Alex Wiltshire, 2018-03-05) | https://www.rockpapershotgun.com/into-the-breach-interface-design | `direct page retrieval` | **본 조사의 핵심 출처.** Ma·Davis 직접 인용. 4년 중 절반을 UI에 씀 / 공격 3유형 강제 제한 / 고리선·점선·화살표 시각 문법(V2) / 애니메이션 툴팁(V8) / *"icon mess"* 과잉 자백 / *"sacrifice cool ideas for the sake of clarity every time"* |
| **S2** | **Game Developer (구 Gamasutra) — "Road to the IGF: Subset Games' Into the Breach"** (Joel Couture, 2018-02-23) | https://www.gamedeveloper.com/game-platforms/road-to-the-igf-subset-games-i-into-the-breach-i- | `direct page retrieval` | **"왜 전부 보여주는가"에 대한 설계자의 답.** Ma: *"We prefer games with clear rulesets... We wanted to make something where **every death felt like your own fault**. This lead us to use of telegraphed enemy attacks as a core mechanic."* 및 숙련자 대응(*"solving fresh puzzles every time"*), 실패 원인의 명료성 원칙 |
| **S3** | **Slay the Spire Wiki — Intent** | https://slay-the-spire.fandom.com/wiki/Intent | `direct page retrieval` | 의도 아이콘의 **정보 설계 명세**: 피해 구간별 아이콘 등급(0-4 / 5-9 / 10-14 / 15-19 / 20-24 / 25-29 / 30+), 디버프 보정 반영, 그리고 **`Runic Dome`이 예고를 제거하는 대신 보상을 주는 역방향 장치**(V16) |
| **S4** | **Wikipedia — Into the Breach** | https://en.wikipedia.org/wiki/Into_the_Breach | `direct page retrieval` | 개발자 인터뷰 인용: *"telegraphing the Vek's movements further **helped to hasten the pace**"* — Contradiction 1의 유일한 실증 |
| **S5** | **Wikipedia — Artillery game** | https://en.wikipedia.org/wiki/Artillery_game | `direct page retrieval` | 포병 계보 가시성 장치의 **연대기**: 1980 Apple II 바람 계산 + 이전 샷 궤적선(V6), 1982 Artillery Duel 풍속 그래픽 표시(V9), 각도·파워 입력의 장르 정의성(V10) |
| **S6** | **Wikipedia — Gunbound** | https://en.wikipedia.org/wiki/Gunbound | `direct page retrieval` | delay 턴 시스템(V11) 원문 명세, GunboundM(2017)의 *"visible bullet path"* 추가 — 계보 이동의 직접 증거 |
| **S7** | **Wikipedia — Angry Birds** | https://en.wikipedia.org/wiki/Angry_Birds_(video_game) | `direct page retrieval` | Chillingo의 *"adding visible trajectory lines"* 주장(V3), 2012년 *"animated tutorial"* 추가(V8·V15), 새총 프로토타입 기록 |
| **S8** | **Wikipedia — Worms Armageddon** | https://en.wikipedia.org/wiki/Worms_Armageddon | `direct page retrieval` | *"series of training missions"*(V15). ⚠️ **부정 결과도 기록**: 본문에 조준 UI·바람 표시·리플레이 서술 없음 → V14를 `thin evidence`로 강등한 근거 |

### 회수 실패 및 강등 기록 (정직성 표기)

| 대상 | 시도 | 결과 | 처리 |
|---|---|---|---|
| Fire Emblem 전투예측창 / 위협범위 | `fireemblemwiki.org/wiki/Battle_forecast`, `/Battle_Forecast`, `fireemblem.fandom.com/wiki/Battle_Forecast`, `/Battle_forecast` | **HTTP 404 ×4** | V4·V5의 FE 항목을 `thin evidence`로 강등. 검색 종합 + 선행 조사 §3.7 재인용으로만 사용 |
| Slay the Spire 개발자 1차 증언 | GDC 2019 "Metrics Driven Design and Balance"(Giovannetti) 확인됨 | 세션은 실재하나 **밸런싱 주제이며 의도 시스템 설계 근거가 아님** | 인용하지 않음. StS 근거는 Wiki 명세(S3)로 한정 |
| "의도 시스템은 암기 부담 때문에 도입됐다" | 검색 종합만 확보, 1차 출처 미확인 | 개발자 발언으로 인용 불가 | 본문에서 `[INFERENCE]`로 처리하고 단정 회피 |
| XCOM 2 Overwatch / 명중률 | 1차 재확인 미실시 (선행 조사 §3.6 존재) | — | `thin evidence`, 선행 조사 재인용 명시 |

> **검색 도구 주의 (다음 레인·다음 런을 위한 기록):** 본 런에서 `web_search`는
> **LLM이 합성한 요약 + 리다이렉트 URL**을 반환했고, 원문에 없는 진술이 섞여 있었다.
> 실제로 S1·S2를 직접 회수한 뒤 대조하니 요약에 있던 *"time travel = localized precognition"* 등
> 일부 서술은 두 원문 어디에도 없었다. **검색 요약은 발견용으로만 쓰고,
> 인용은 반드시 직접 회수 후에 한다** — 본 문서의 모든 직접 인용문은 원문 회수로 대조했다.

> **결함표 신선도 주의 (본 런에서 실제로 발생한 오류):** `qa/ux-defect-list.md`는 같은 날짜 산출물인데도
> **일부 항목이 이미 조치된 상태를 미조치로 싣고 있다**(UX-001 = 바람 렌더 0, Key Gap 6 참조).
> 원인은 감사 방법이다 — 결함표가 **씬 파일 정적 구조**를 근거로 삼았는데, 이 프로젝트는 HUD를
> **런타임에 코드로 재부모화**한다(`HudCanvas.Adopt`). 씬만 읽으면 조치된 결함이 영구히 미조치로 보인다.
> 결함표 자신도 §한계 2에서 *"런타임 단언은 없다"* 고 밝혀 두었다.
> **→ 결함표를 인용할 때는 해당 심볼의 런타임 경로를 한 번 더 추적하라.** 특히 `S1` 등급일수록
> 디렉터가 즉시 예산을 배정하므로, 이미 고친 것에 재투자할 위험이 그만큼 크다.

> **셸 grep 함정 (Lane A가 걸렸다가 빠져나온 실제 사례):** macOS 기본 BSD `grep`은 BRE에서 `\|`를
> 대안(alternation)으로 해석하지 않는다. `grep "windText:\|scoreText:"` 는 **실재하는 문자열에도 무매치**를
> 반환하고, 이를 "씬에 할당돼 있지 않다"는 증거로 읽으면 정반대 결론에 도달한다.
> Lane A는 `awk` 재확인으로 자력 정정했다. **무매치는 부재의 증거가 아니다 — 먼저 도구를 의심하라.**
> (본 저장소 규칙상 내용 검색은 `grep` 툴을 쓰므로 이 함정은 셸을 직접 호출할 때만 발생한다.)

### castle-war 코드 실측 근거 (전부 `[OBSERVED — 코드]`)

| 파일:행 | 확인 사실 | 대응 장치 |
|---|---|---|
| `SimpleAI.cs:30` / `:62` / `:74` | 0.5초 대기가 조준 계산보다 **먼저** 옴. `:62`→`:74` 사이 yield 0개 | V1 결손의 근본 원인 |
| `LaunchManager.cs:18-22` | 궤적 300스텝 × 0.02s = 6초, *"preview must always reach the impact"* | V3 보유 |
| `LaunchManager.cs:121` | `"아무 곳이나 당겨 발사"` — 적 턴에도 잔존 | 거짓 지시 ② |
| `SiegeAlarmSystem.cs:225` | `"적 포격 준비 중...  ·  클릭: 벽돌 예약"` | 거짓 지시 ① |
| `BrickPlacementController.cs:76-82` | `EnforcesOneShotTurns`일 때 early-return → 벽돌 예약 차단 | 거짓 지시 ①의 반증 |
| `OneShotSiegeRules.ProjectileForTurn` | 라운드마다 Knight→Archer→Barrel, 양 진영 동일 = 결정론 | V7 정보원 (표시 UI 0건) |
| `EruptionVentGimmick.cs:66-71, 200-218` | Dormant→Warning(1.8초 맥동+라벨)→Erupting | V18 보유 |
| `MovingGimmick.cs:51-54` | `phaseTelegraphDelaySeconds = 0.45f`, *"the warning genuinely precedes the hazard"* | V18 보유 |
| `GameManager.cs:2293-2297` | `WIND >>> 2.3` / `WIND CALM`, 3.5 이상 시 색 경고 | V9 보유 |
| `GameManager.cs:1124-1128` | windText/scoreText가 Canvas 없이 *"an audience of nobody"* 였던 이력 | Key Gap 6 |
| `GameFeelVfx.SpawnDamageNumber` + `DestructibleBlock.cs:211` + `UnitController.cs:1081` | 피해 숫자 팝업 | V12 보유 |
| `GamePresentationDirector.cs:98` | followLerp 카메라 추적 | V13 보유 |
| `GameManager.cs:309`→`:321`→`:1129` + `HudCanvas.cs:112-134, 51-90` | windText 런타임 입양 경로 **전 구간 추적, 끊김 없음** → UX-001은 조치됨 | Key Gap 6 정정 · **Lane A 독립 재현 완료** |
| `CannonShotVisuals.cs:10-15` | *"a burning arc that hangs long enough to read the trajectory"* — 화포 전용 | V6 부분 보유 |
| `FirstPlayCoachController.cs:8-16` | 단계 배너 + 월드 화살표 + 턴클럭 홀드, 프로필당 1회 | V15 보유 |

---

## 레인 경계 (디렉터용)

- **본 레인이 소유**: 장치 18종 정의, 계보별 빈도표, 표본 12종 선정과 등급, 코드 실측 대조표.
- **Lane C로 넘김**: 예고 과잉 역효과(Contradiction 4). ItB "icon mess" 1차 증거를 2026-08-13 IRC로 전달 완료.
- **본 레인이 판단하지 않음**: 무엇을 채택할지, 어떤 기믹을 삭감할지. 전부 설계 레인 소관.
- **합병 시 주의**: 본 파일의 `## Solution List` / `## Categories` / `## What People Actually Use` /
  `## Frequency Ranking` / `## Key Gaps` / `## Contradictions` / `## Key Insight` 는
  `solutions.md` 계약 헤딩과 동일하게 맞춰 두었으므로 Lane C와 항목 병합이 가능하다.
  단 **빈도표의 분모(12)와 표본 목록은 본 레인 기준**이며, Lane C가 다른 표본을 쓰면 분모를 분리 표기해야 한다.
