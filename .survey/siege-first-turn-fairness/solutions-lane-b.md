# Solution Landscape (Lane B)

- lane: B — 장치 카탈로그 (넓게 훑기 + 빈도표)
- run: `siege-first-turn-fairness`
- 작성일: 2026-08-14
- 산출물 범위: **본 파일 1개.** 코드 수정 없음. `context.md` / `actual-lane-c.md` / `alternatives-lane-d.md` 미접촉.
- 표본 선정 기준: **"선공 이점을 측정했거나, 그것을 겨냥한 규칙을 실제로 시행한" 룰셋만** 담았다.
  선공 문제를 다룬 기록이 없는 타이틀은 표본에 넣고 **"장치 없음"으로 명시**했다 —
  없는 것을 있다고 쓰지 않는 것이 이 레인의 규칙이다.
- 조사 언어: 검색·1차 출처는 전부 영어. 산출물은 한국어.

> ## ⚠️ 전제 정정 (2026-08-14, Main 재측정 반영)
>
> **이 문서 본문의 다수 판정이 낡은 전제 위에 쓰였다. 본문을 지우지 않고 여기서 정정한다.**
>
> | | 낡은 전제 (본문이 쓰인 근거) | 정정된 사실 (Main 재측정) |
> |---|---|---|
> | 선공 승률 | 87%, 선공이 38%p를 움직임 | **고정 선공 47.0% / 교대 53.0% — 둘 다 45~55% 밴드 안** |
> | 기준 시점 | 현재 | **2026-08-12 기준선. `OpeningVolleyDamageScale = 0.5` 투입 *이전*** |
> | N3(첫 턴 피해 감소) 평가 | **채택됐고 실패했다** | **채택됐고 성공했다** |
>
> **따라서 이 레인의 질문이 바뀐다.** "어떻게 고칠까"가 아니라
> **"우리가 이미 고른 장치가 좋은 선택인가"** 다. 그 판정은 본문 N3 행과 아래 §실력 절벽에 있다.
>
> **정정이 무효화하는 것 / 살리는 것:**
> - ❌ **무효**: N3 행의 *"실패했다"* 판정, 계보 교차 집계의 *"castle-war 1/4"* 해석,
>   C1·M1·R2를 *"선공 문제를 고치기 위해"* 권고한 대목. 선공 문제는 **이미 닫혔다.**
> - ✅ **유효**: 장치 27종의 카탈로그·수치·출처 전부. 빈도 순위. **"표본 19개에서 N3 외부 채택 0건"**
>   — 이것은 이제 *실패의 증거*가 아니라 **"우리 보정에 외부 선례가 없다"는 리스크 표시**다(Lane C 과제).
> - ⚠️ **의미가 바뀜**: **C1(라운드 완주)**. 선공 보정으로는 불필요해졌으나, 아래 §실력 절벽에서
>   측정해 보니 **절벽을 오히려 악화시킨다**(69.1% → 73.7%). 즉 이제는 **넣지 말아야 할 이유**가 있다.
>
> **이 문서를 읽기 전에 알아야 할 castle-war 사실 3개** (전부 `direct page retrieval — 코드`):
> ① **첫 턴 피해 감소 장치가 이미 들어가 있고 작동한다.** `OneShotSiegeRules.OpeningVolleyDamageScale = 0.5f`,
> `OpeningVolleyDamageMultiplier(completedTurns) => completedTurns <= 0 ? 0.5f : 1f`.
> 주석의 *"Reducing only that opening volley to 50% removes the measured 87% first-mover win rate"* 는
> **재측정으로 확인됐다**(고정 선공 47.0%).
> ② **승리 판정에 라운드 완주가 없다.** `CheckVictoryConditions()`는 `currentHP <= 0`을 보는 즉시
> `EndGame()`을 호출한다(`GameManager.cs:2387-2402`). **이것은 이제 결함이 아니라 절벽 완화 요인이다** — §실력 절벽 참조.
> ③ **매치 단위 컨테이너는 이미 있다.** `SiegeSeries.WinsNeeded = 2`, `MaxGames = 3`,
> `seriesGamesPlayed`는 static이며 씬 리로드를 넘어 생존한다. 그런데 `GameManager.cs:1865`는
> 시리즈 몇 번째 경기인지와 무관하게 `isPlayerTurn = true`를 무조건 세팅한다.

---

## Solutions

**장치 27종** (+ §실력 절벽에 11종 추가 = 총 38종). 계열 표기: `순서`=순서 자체를 바꿈, `자원`=후공에게 준다, `약화`=첫 턴을 깎는다,
`동시`=동시성으로 없앤다, `구조`=구조로 없앤다, `매치`=매치 단위로 없앤다.
`적용 가능성` 칸의 판정어는 **가능 / 조건부 / 불가 / 이미 보유** 네 가지이며, **"불가"도 판단**이다.

| 이름 | 채택 게임 | 작동 방식(수치 포함) | 강점 | 약점 | castle-war 적용 가능성 |
|---|---|---|---|---|---|
| **T1. 단순 교대 (ABAB)** | 체스, 바둑, 장기, Hearthstone, MtG, Pokémon TCG, Worms, ShellShock Live, Scorched Earth, 승부차기(5킥씩), 야구, 크리켓 Super Over(각 6구) — 표본 대부분의 **기반값** | 매 턴/매 킥마다 행동권이 번갈아 이동. 추가 자원·감쇠 없음 | 구현 비용 0에 가깝고 설명이 필요 없다 | **표본에서 선공 이점을 측정한 룰셋은 전부 교대만으로 잔차가 남았다** — 체스 52~56%, 바둑 53%(덤 5.5 시절), 승부차기 51.5~60%. 교대는 보정 장치가 아니라 **보정 장치가 올라앉는 바닥**이다 | **조건부.** 시뮬 49.0%는 밴드 안이지만 AI 전용 조준 오차 미반영. `SiegeDuelSimulation.RunSeries(alternateFirstMove: true)`와 `firstMoverWinRate`가 **이미 구현돼 있어** 재측정 비용은 0. 단 `ExecuteAITurn` 호출처가 `EndTurn()` 하나뿐이라 AI 선공 경기는 시작 시 정지한다 |
| **T2. 랜덤 코인플립 / 주사위** | Pokémon TCG(동전, 공식 대회는 주사위), NFL 오버타임(팀 주장 입회 코인토스), MtG 1게임(CR 103.1 "any mutually agreeable method") | 경기 시작 시 무작위로 선공을 결정 | 편향이 장기적으로 0에 수렴. 규칙이 한 줄 | **한 경기 안에서는 아무것도 보정하지 않는다.** 38%p 격차가 그대로 남고 절반의 플레이어가 그 격차의 나쁜 쪽에 앉는다. NFL은 이것 때문에 2022년에 규칙을 바꿨다 | **불가(단독).** castle-war는 vs-AI 싱글이므로 무작위화는 "플레이어가 절반의 경기를 38%p 불리하게 시작"으로 번역된다. 오프라인 대전 규칙이 아니라 학습 곡선 파괴다 |
| **T3. 선택권 부여 (무작위 승자가 선/후를 고른다)** | Pokémon TCG("the winner of the coin flip will decide who goes first or second"), MtG(CR 103.1 "determine which one of them will choose who takes the first turn"), NFL(토스 승자가 receive/kick 선택) | 무작위는 **선택권**만 배분하고, 선공 여부는 플레이어가 고른다 | 선/후 각각에 장·단이 설계돼 있을 때만 성립하며, 성립하면 순서가 **전략**이 된다 | 선·후 비대칭이 이미 보정돼 있어야 의미가 생긴다. 보정 없으면 100% 선공 선택으로 붕괴 | **불가(현 상태).** 후공에 아무 보상이 없어 선택지가 1개다. R1/N2를 먼저 넣은 뒤에야 후속 장치로 성립 |
| **T4. 이전 게임 패자가 선공을 선택** | Magic: The Gathering — **CR 103.1**: *"In a match of several games, the loser of the previous game chooses who takes the first turn."* 무승부면 지난 게임에서 선택한 쪽이 다시 선택 | 게임 단위가 아니라 **매치 단위**로 순서를 배분. 뒤진 쪽에 순서 결정권 | 패자에게 주는 것이 승률이 아니라 **결정권**이라 보정이 자기 조절된다. 시리즈 전체 승률을 50%로 밀어붙임 | 단일 게임에는 효과 0. 매치 컨테이너가 필수 | **가능.** `SiegeSeries`(2승/최대 3경기)가 이미 있고 `seriesGamesPlayed`가 리로드를 넘어 생존한다. 다만 vs-AI에서 "AI가 선택"은 무의미하므로 **T4보다 M1(무조건 교대)이 castle-war 형태에 맞다** `[INFERENCE]` |
| **T5. 이전 기간 비선공자가 다음 기간 선공** | 3x3 농구 — *"The team that did not get first possession in the game gets first possession in overtime"*; FIBA 정규 농구 — 연장 개시는 **alternating possession rule** | 기간(게임/연장)이 넘어갈 때 선공권을 반대편으로 넘김 | 상태 하나(`누가 먼저 했나`)만 저장하면 끝. 누적 편향이 구조적으로 0 | 기간 경계가 있어야 성립 | **가능.** M1과 동일 계열. castle-war의 기간 경계는 **경기(SiegeSeries 1~3)** 와 **스테이지(1~3)** 두 층이 이미 존재 |
| **T6. 파이 룰 / 스왑 룰** | Hex(표준), TwixT(대회 규정), Meridians(선공이 돌 2개 놓고 후공이 색 선택), 바둑 auction komi. 최초 기록은 **1909년 Mancala 계열 게임** | 선공이 첫 수를 놓은 뒤, 후공이 ① 그대로 후공 유지 ② **자리를 바꿔 그 수를 자기 것으로** 중 택1 | "케이크 자르고 상대가 고르기". 선공이 **스스로 공정한 수를 고르도록 강제**된다. 수치 튜닝이 전혀 필요 없음 | 무승부 없는 완전정보 추상게임에 맞춘 장치. Hex에서는 이론상 후공 승이 되지만 실전에서는 균형으로 작동 | **불가.** castle-war의 첫 수는 조준·물리·바람이 섞인 연속 공간 행동이라 "그 수를 내 것으로 가져간다"가 정의되지 않는다. 좌우 대칭 성 배치를 스왑하는 변형은 이론적으로 가능하나 물리 지형이 좌우 비대칭이면 성립하지 않음 `[INFERENCE]` |
| **T7. 비대칭 순서열 (ABBA / Thue–Morse)** | 축구 승부차기 — IFAB가 **2017년 3월** 테니스 타이브레이크식 **ABBA** 시행 시험 승인. 2017 UEFA 여자 U-17 준결승(독일 v 노르웨이)이 최초 적용, 2017 FA Community Shield에도 사용. Palacios-Huerta는 **Thue–Morse 수열**을 제안 | ABAB 대신 A-BB-AA-BB… 로 배열해 "먼저 차는 쪽" 이점을 교대시킴 | 자원도 감쇠도 추가하지 않고 **순서열만** 바꿔서 편향을 상쇄. 수학적으로 가장 깔끔 | **IFAB 133차 총회(2018-11-22, 글래스고)에서 폐기됐다** — 사유: *"due to a lack of strong support, **mainly because of its complexity**"*. 즉 이 장치의 실증된 실패 원인은 효과가 아니라 **가독성**이다 | **불가(권고).** 1턴 1발사 포격전에서 "이번엔 적이 두 번 쏩니다"는 IFAB가 폐기한 바로 그 복잡성이다. 300초·38.8턴 경기에서 순서 예측 가능성은 조준 학습의 전제 `[INFERENCE]` |
| **T8. 지연(delay) 자원화 — 순서를 연속량으로** | **Gunbound**(2002, Softnyx) — *"a 'delay' turn system which is influenced by the Mobile, the weapon and/or item a player uses — using items or taking time with actions results in a longer wait before the player's next turn"* | 턴 순서가 교대가 아니라 **누적 delay가 낮은 쪽이 다음 턴**. 강한 무기·아이템은 delay를 더 먹음 | 선공 이점이 "무료"가 아니라 **가격이 붙는다**. 강한 첫 수를 쓰면 그만큼 다음 턴이 늦어짐 | 연속 턴이 발생해 대칭이 깨진다. 무기별 delay 수치 튜닝 부담이 큼 | **조건부 / 실질 불가.** `OneShotSiegeRules.ProjectileForTurn(completedTurns)`은 **라운드(2턴)마다** Knight→Archer→Barrel을 양 진영 동일하게 돌린다 — 코드 주석이 이를 *"the fairness device"* 로 명시(`SiegeDuelSimulation.cs:132-133`). delay를 넣으면 이 대칭이 즉시 붕괴 |
| **R1. 후공에게 카드+임시 자원 (The Coin)** | **Hearthstone** — 후공이 시작 카드 **4장**(선공 3장) + **The Coin**(비용 0 주문, 효과 *"Gain 1 Mana Crystal this turn only"*). 위키 원문: *"granted at the start of each game to whichever player is selected to go second. **Together with the fourth starting card**, The Coin is intended to counter some of the disadvantage of going second"* | 후공에게 ①카드 1장 ②그 턴만 유효한 마나 1 — 둘 다 **후공이 쓸 시점을 고른다** | 자원이 카드 형태라 **사용 타이밍의 재량**이 남고, 그것 자체가 플레이가 된다. 수치가 정수 2개(+1장, +1마나)로 극히 단순 | 보정 후에도 선공 승률이 몇 %p 우위로 남는다고 알려져 있다(정확한 수치는 회수 실패) | **조건부.** castle-war에 "카드"도 "마나"도 없다. 번역 가능한 후공 자원은 **성벽 블록 1개 선배치** 또는 **코어 HP 가산**뿐이며, 전자는 `BrickPlacementController`가 이미 존재해 배관 비용이 낮다 `[INFERENCE]` |
| **R2. 후공 점수 가산 — 덤(komi)** | **바둑.** 표준 **6.5**점(일본·한국 규칙), **7.5**점(중국·AGA·Ing), **7.0**점(뉴질랜드). 접바둑은 **0.5**점 | 후공(백)의 최종 점수에 고정값을 더한다. 0.5의 반칙수는 무승부(jigo) 방지용 | **선공 이점을 스칼라 하나로 압축**해 규칙 변경 없이 튜닝 가능. 100년에 걸쳐 실측으로 값을 올려온 계보가 있다 | 값이 틀리면 반대로 기운다. 완전한 값은 아무도 모른다 | **가능(가장 직역 가능한 장치).** castle-war의 코어 HP 150이 곧 점수판이다. **후공 코어 HP를 150 + Δ로 두는 것이 정확히 덤**이며, 19.4턴/성 1개라는 측정치가 Δ의 초기 후보를 계산 가능하게 한다 `[INFERENCE]` |
| **R2-a. 덤 값의 실측 이력 (튜닝 절차 그 자체)** | 바둑 — Hisekai(1922 설립)가 **4.5**로 시작 → 2.5/3 → 4.5 → **5.5**(장기간 표준) → **6.5**. 5.5 시절 데이터베이스가 **흑 승률 53%** 를 보였고 그것이 인상 근거 | 값을 고정하지 않고 **경기 데이터로 재평가**해 단계적으로 올린다 | castle-war에 필요한 것이 바로 이 절차다 — 한 번에 맞히려 하지 않는다. "완전한 덤은 area scoring·seki 부재 조건에서 홀수 정수이며 통계는 7을 지지"라는 이론적 상한도 존재 | 표본이 커야 한다. 프로 기보 수십 년 규모 | **가능.** `SiegeDuelSimulation`이 이미 시리즈 러너이고 `firstMoverWinRate`를 분리 보고한다. **덤 Δ를 파라미터로 스윕하는 것이 이 프로젝트에서 가장 값싼 실험** `[INFERENCE]` |
| **R3. 후공 점수 가산 — 장기(janggi) 1.5점** | **장기**(한국 체스). 일반 규칙상 무승부가 될 국면에서 남은 기물 점수를 합산하고, **후수에게 1.5점을 준다.** 모든 기물이 정수 점수라 무승부가 원리적으로 존재하지 않는다 — Kaufman이 *"the only competitively played version of chess where draws do not exist"* 로 기술 | 덤과 같은 발상을 **기물 점수제**로 구현. 0.5의 반칙수로 무승부까지 동시에 제거 | 하나의 상수가 ①선공 보정 ②무승부 제거를 겸한다 | 점수제가 이미 게임에 있어야 한다 | **가능.** castle-war는 `SIEGE SCORE {playerScore} - {enemyScore}`를 이미 표시한다(`GameManager.cs:2384`). 다만 현재 승패는 점수가 아니라 코어 HP로 갈리므로 R2 쪽이 직결 |
| **R4. 핸디캡 돌 + 최소 덤** | 바둑 접바둑 — 실력 차만큼 흑이 **먼저 N점을 미리 놓고** 시작하며 덤은 **0.5**로 고정(무승부 방지용만 남김) | 순서 보정과 **실력 보정**을 같은 축에서 처리 | 비대칭 강도를 연속적으로 조절 가능 | 선공 문제의 해법이 아니라 실력 차 해법이다 | **조건부.** castle-war의 난이도 램프(`CurrentAiErrorOffset`, `aiErrorStart→aiErrorEnd`)가 이미 이 역할을 한다. 선공 보정과 **섞으면 두 축이 교란되어 G2 재측정이 다시 불가능해진다** — 경고로서 유용 `[INFERENCE]` |
| **R5. 시작 손패 재시도의 상대 보상 (멀리건 보정)** | **Pokémon TCG** — Basic 포켓몬이 없으면 멀리건 필수이며 *"the opponent may draw one additional card per mulligan"*. **MtG** London Mulligan(2019, 전 경쟁 포맷) — 7장 새로 뽑고 멀리건 횟수만큼 카드를 덱 맨 아래로 | 한쪽이 재시도할 때마다 상대에게 자원 1을 자동 지급 | 보정이 **사건 기반**이라 필요한 만큼만 발생 | 시작 상태에 무작위성이 있어야 성립 | **불가.** castle-war의 시작 상태는 결정론적이다(성 배치·재질이 스테이지 상수). 재시도 개념이 없어 보상할 사건이 없다 |
| **R6. 패자·사망 측의 판 개입 자원** | **Gunbound** — 사망한 플레이어는 3열 슬롯 게임에 접근해 골드를 얻거나 **살아있는 플레이어의 샷에 영향을 주도록 바람 조건을 바꾸거나**, 무작위 아이템·폭탄을 하늘에서 떨군다 | 뒤진 쪽에 **직접 승률이 아니라 판의 변수**를 준다. 관전 시간이 행동 시간으로 전환됨 | 무작위성이 실력 신호를 흐린다. 대전 게임에서만 자연스러움 | **불가.** 선공 문제와 무관한 축이다. 다만 castle-war의 바람(`CurrentWindCap`, `WIND >>> 2.3`)이 이미 존재하는 개입 지점이라는 사실은 기록할 값이 있다 |
| **N1. 첫 턴 행동 제한** | **Pokémon TCG** — *"The player going first **cannot attack or play a Supporter card** on their first turn."* 진화 카드도 첫 턴 사용 불가 | 선공의 첫 턴에서 **가장 강한 행동 범주만** 봉인. 이동·에너지 부착 등 나머지는 허용 | 자원도 수치도 건드리지 않는다. "선공 첫 턴엔 공격 불가" 한 줄이며 **플레이어가 규칙을 즉시 이해**한다 | 게임이 여러 행동 범주를 가져야 성립 | **불가(현 형태).** castle-war는 **1턴 1발사**이고 그 발사가 유일한 행동이다(`OneShotTurnGate`). "공격 금지"는 곧 턴 스킵이며, 그것은 후공 선공화(=T1)와 같아진다. **행동 범주가 하나뿐인 게임에서 이 장치 계열 전체가 무력화된다** — 이 레인의 핵심 부정 발견 |
| **N2. 첫 턴 자원 박탈 (드로우 스킵)** | **Magic: The Gathering** — **공식 CR 103.8a**: *"In a two-player game, the player who plays first **skips the draw step** of their first turn."* 2인 게임에만 적용되며 다인전(103.8c)에서는 스킵하지 않는다 | 선공에게서 **정확히 카드 1장**을 뺀다. 30년 이상 유지된 값 | 비용이 정수 1이고 대칭적으로 서술된다. **다인전에서는 끄는 것**까지 규칙에 명시돼 있어 "언제 이 장치가 불필요한가"까지 문서화된 사례 | 반복 자원(매 턴 드로우)이 있어야 뺄 것이 생긴다 | **불가.** castle-war에 매 턴 갱신되는 손패·자원이 없다. `SiegePrototypeEconomy` supply는 배치(deploy)용이며 `enforceOneShotTurns`에서 발사와 무관하다 |
| **N3. 첫 턴 피해 감소** | **castle-war 자신** — `OneShotSiegeRules.OpeningVolleyDamageScale = 0.5f`, `completedTurns <= 0`에만 적용, `CaptureDamageMultiplier`가 `damageFromPlayer == true && IsPlayerTurn && ShotCommitted`로 게이팅 → **플레이어의 첫 발사 1발만 50%** | 발사 구조·투사체 정체성·순서를 하나도 바꾸지 않는다. 순수 곱셈 상수. **비용이 상수 1개이고 경기 길이에 영향이 없다** | **표본 19개 룰셋 중 이 장치를 채택한 외부 사례를 하나도 찾지 못했다** — 첫 턴 약화는 전부 자원 박탈(MtG 카드 1장) 또는 행동 제한(PTCG 공격·Supporter 금지) 형태였고, **곱셈 감쇠형은 0건**이다. 즉 우리 보정은 **외부 선례 없이 단독으로 서 있다**(Lane C 확인 과제) | **✅ 이미 보유, 그리고 작동이 실측됐다.** 고정 선공 **47.0%** / 교대 **53.0%** — 둘 다 45~55% 밴드 안(Main 재측정). ⚠️ 단 **왜 이렇게 잘 듣는지는 산술적으로 설명이 필요하다**: 1발 50% 감쇠는 총 피해의 약 2.6%(1/38.8)에 불과한데 47%까지 내려갔다. 아래 §실력 절벽이 답을 준다 — **경기가 20발 레이스이고 발당 분산이 sd 0.50발밖에 안 되므로, 총 피해의 2.6%가 곧 0.5발이고 그것이 sd 1개분**이다. 같은 민감도가 선공 보정을 쉽게 만들고 **실력 절벽을 만든다** |
| **S1. 동시 턴 해결** | **Frozen Synapse**(2011, Mode 7) — 양측이 **무제한 시간** 동안 명령을 계획하고 커밋, 그 뒤 **약 5초의 시뮬레이션**이 동시 해결. 전투는 무기·자세·poise 기반 **결정론적**. / **Scorched Earth** — **v1.2(1992)에 "Synchronous firing mode" 추가**. 즉 포격 계보 원형 자체가 동시 발사 모드를 출하했다 | 선공이라는 개념 자체를 제거. 순서가 없으면 순서 이점도 없다 | 예측(상대가 어디로 갈지)이 새 스킬 축으로 추가된다. 물리 게임에서는 두 발사체·두 붕괴가 겹쳐 인과 판독이 붕괴할 수 있다 | **조건부(가장 유력한 구조적 후보).** castle-war는 발사 후 물리 정착까지 대기하므로 동시 발사는 **경기 길이를 절반 가까이 줄인다**(38.8턴 → 약 19.4 해결). 300초 목표(허용 240~360) 대비 **291초가 밴드 하단을 뚫고 나갈 위험**이 최대 쟁점. Scorched Earth가 이것을 기본이 아니라 **별도 모드로** 출하했다는 사실이 그대로 선례 `[INFERENCE]` |
| **S2. 위상 동시 — 양측이 같은 페이즈에서 실시간 발사** | **Rampart**(1990, Atari Games) — *"Gameplay alternates between **two time-limited phases**: combat and building"*, *"In multi-player mode, the players shoot at each other's walls"*, 2인전 전투는 *"after a set time"* 종료 | 턴을 없애지 않고 **페이즈를 공유**한다. 순서 대신 시간 예산이 자원 | 성·대포·성벽이라는 **castle-war와 사실상 동일한 소재**에서 검증된 형태. 페이즈 길이가 경기 길이를 직접 통제 | 실시간 조준 요구가 생겨 모바일 진입 장벽과 충돌. 1턴 1발사 정체성이 사라짐 | **조건부.** 소재 유사성이 표본에서 가장 높은 장치다. 단 castle-war의 조준은 드래그 기반이고 `LaunchManager`의 궤적 프리뷰(300스텝×0.02s=6초)가 **정지 상태 계산**을 전제하므로 실시간화는 조준 UX 전면 재설계를 요구 |
| **C1. 라운드 완주 보장** | **NFL** — **2022년 규칙 변경**: 플레이오프 오버타임에서 *"gives both teams one possession to start the first overtime… **no matter whether or not a touchdown is scored first**"*, 2025년 정규시즌 확대. 계기는 선공 팀 터치다운으로 후공이 **공을 한 번도 못 잡은** 경기(Super Bowl LI: *"the Patriots scored a touchdown on their initial possession, so the Falcons never received the ball in overtime"*). / **AFL·NFL Europe**가 먼저 *"each team is guaranteed one possession"* 를 시행. / **야구 연장** — *"Complete innings are played, so if a team scores in the top half of the inning, the other team has the chance to play the bottom half"*. / **크리켓 Super Over** — 양팀 각 6구 | 순서를 손대지 않고 **"먼저 도달하면 끝"이라는 종료 규칙만** 고친다. 선공 이점의 발생 지점을 정확히 겨냥 | 경기가 최대 1라운드 길어진다. 동점 처리 규칙이 새로 필요 | **가능 — 그리고 이 표에서 castle-war 구조와 가장 정확히 맞물린다.** `CheckVictoryConditions()`는 `currentHP <= 0`에서 **즉시** `EndGame()`을 부른다(`:2387-2402`). 선공이 19.4턴에 먼저 0을 만들면 후공의 응사는 발생하지 않는다 — **NFL이 2022년에 고친 것과 동일한 실패 모드다.** 비용은 턴 1개(약 7.5초)이며 38.8턴 291초 예산 안에서 무시할 만하다 `[INFERENCE]` |
| **C2. 턴 수 상한 + 생존형 목표** | **Into the Breach**(2018, Subset Games) — 맵마다 *"an objective for that map along with **a fixed number of turns** to complete that objective"*. 개발자 근거: *"The **limited turn counter was used to keep battles short**, and Subset found that telegraphing the Vek's movements further helped to hasten the pace"* | 승패가 "누가 먼저 상대를 0으로 만드나"에서 **"제한 턴을 버티나"** 로 이동. 한 턴 앞선 것이 승패를 결정하지 않게 됨 | 경기 목표를 다시 쓰는 일이다. 밸런스 전체 재측정 필요 | **조건부.** castle-war는 이미 38.8턴/291초라는 사실상의 상한을 가지고 있으나 **승리 조건이 여전히 코어 0**이다. 상한을 규칙으로 승격하고 "상한 도달 시 코어 HP 비교 판정"으로 바꾸면 C1과 같은 효과를 얻으면서 경기 길이가 오히려 안정된다 `[INFERENCE]` |
| **C3. 승리 조건을 누적 침식에서 분리** | **Rampart** — *"The player loses when they **fail to have at least one surrounded castle** after the tile-placement phase"*; 2인전에서 양쪽이 생존하면 *"the one with the higher score is declared the winner"*. 즉 **패배 조건이 "코어가 0"이 아니라 "성을 다시 에워싸지 못함"**. / **Gunbound** Score/Jewel 모드 — **100점** 선취 또는 상대 전멸. / **Into the Breach** — 주 목표는 *"protect civilian structures which support the power grid"* | 선공의 한 턴 우위가 **승리 조건과 직접 연결되지 않게** 만든다. 근본 해법 | 게임이 다른 게임이 된다. 소재만 남고 규칙은 새것 | **조건부(장기).** 후반 스테이지 설계 원칙이 *"더 긴 싸움이 아니라 다른 싸움"* 이므로 **스테이지 2·3의 목표 전환 후보로 정확히 들어맞는다.** 단 스테이지 1의 선공 문제를 해결하지는 못한다 — 세 스테이지 공통 문제이기 때문 |
| **C4. 목표 비대칭** | **Frozen Synapse** — 멀티 모드에 last-man-standing 외 **area protection, hostage extraction**이 있고 싱글에는 hostage protection·escort. / **Into the Breach** — 플레이어는 건물을 지키고 Vek는 파괴한다(대칭 전멸전이 아님) | 양측이 **다른 것을 이기려 하면** 선공 이점의 정의가 흐려진다 | 팩션 대 팩션 대칭성이 게임 정체성인 경우 충돌 | **불가(코어 루프).** castle-war는 "팩션 대 팩션, 양측이 파괴 가능한 성벽+코어"라는 **대칭이 정체성**이다. 기믹 층에서 국소적으로 쓰는 것은 별개 축 |
| **C5. 목표 점수제 — 시계 제거 (Elam Ending)** | 농구. **The Basketball Tournament**(2018~) 4쿼터 4분 이하 첫 데드볼에 시계를 끄고 **선두 점수 + 8점**(최초 7점)을 목표로 설정, 먼저 도달한 팀 승. NBA All-Star(2020~2023) **+24**, NBA G League 연장 **+7**, CEBL **+9**, Unrivaled **+11** | "마지막에 공 잡은 쪽" 이점을 **구조적으로 소멸**시킨다. 경기가 반드시 득점으로 끝난다 | 남은 시간 예측이 불가능해져 페이싱 통제를 잃는다 | **불가.** castle-war는 300초 밴드(240~360)라는 **명시적 시간 예산**으로 게이팅된다. 목표 점수제는 그 예산을 포기하는 것이며, G2 재측정의 전제인 경기 길이 안정성과 정면 충돌 |
| **M1. 베스트오브N에서 선공 교대** | 컴퓨터 체스 대회 — 사전 선정 오프닝을 **경기당 2판씩, 각 플레이어가 백을 한 번씩** 잡는 방식이 표준(1928년 **Frank Marshall**이 이미 제안). / 축구 2연전(home/away) 합산 + away goals(UEFA는 2021–22부터 클럽 대회에서 연장 폐지·승부차기로 대체) | 게임 단위 편향을 **매치 단위에서 상쇄**. 선공 이점을 없애지 않고 **양쪽에 한 번씩 준다** | 개별 게임은 여전히 38%p 불공정하다. 플레이어가 매치를 완주해야 함 | **가능 — 구현 비용이 가장 낮다.** `SiegeSeries.MaxGames = 3`, `WinsNeeded = 2`, `seriesGamesPlayed`가 static으로 리로드를 넘어 생존하며(`GameManager.cs:2509-2517`) `GameManager.cs:1865`는 이 값을 **읽지 않고** `isPlayerTurn = true`를 무조건 세팅한다. 즉 `isPlayerTurn = (seriesGamesPlayed % 2 == 0)` 한 줄이 매치 단위 교대다 `[INFERENCE]` |
| **M2. 세트 스코어 / 합산 판정** | 축구 2연전 합산 스코어 후 away-goals(CONCACAF 클럽 대회는 유지, UEFA는 폐지). / castle-war의 `SiegeSeries.SeriesScore(seriesScoreTotal, ...)`가 이미 시리즈 총점 산출 | 단판 승패가 아니라 **누적 지표**로 판정 | 매치 길이가 N배. 개별 경기의 극적 종료가 약해짐 | **이미 보유(부분).** `SiegeEcosystem.cs:189`가 시리즈 결정 시 `SeriesScore`로 등급을 계산한다. 즉 M1을 넣으면 M2 표시 계층은 **이미 준비돼 있다** |

---

## Frequency Ranking

### 표본 룰셋 19개 + 대조군 1개 (명시)

| # | 룰셋 | 계보 | 이 런에서의 근거 |
|---|---|---|---|
| 1 | **체스** | 추상 2인 | `direct page retrieval` — en.wikipedia.org/wiki/First-move_advantage_in_chess |
| 2 | **바둑** | 추상 2인 | `direct page retrieval` — en.wikipedia.org/wiki/Komi_(Go) |
| 3 | **장기 (janggi)** | 추상 2인 | `direct page retrieval` — 체스 문서 내 Kaufman 서술 |
| 4 | **Hex** (+TwixT, Meridians) | 추상 2인 | `direct page retrieval` — en.wikipedia.org/wiki/Pie_rule |
| 5 | **Hearthstone** | 디지털 CCG | `direct page retrieval` — hearthstone.wiki.gg/wiki/The_Coin |
| 6 | **Magic: The Gathering** | TCG | `direct page retrieval` — **공식 Comprehensive Rules 원문 파일** (media.wizards.com) |
| 7 | **Pokémon TCG** | TCG | `direct page retrieval` — en.wikipedia.org/wiki/Pokémon_Trading_Card_Game |
| 8 | **Worms 시리즈** | 턴제 포격 | `direct page retrieval` — en.wikipedia.org/wiki/Worms_(series) |
| 9 | **Gunbound** | 턴제 포격 | `direct page retrieval` — en.wikipedia.org/wiki/Gunbound |
| 10 | **Scorched Earth** | 턴제 포격 | `direct page retrieval` — en.wikipedia.org/wiki/Scorched_Earth_(video_game) |
| 11 | **ShellShock Live** | 턴제 포격 | `direct page retrieval` — Wikipedia wikitext (action=raw) |
| 12 | **Rampart** | 성 파괴·건설 | `direct page retrieval` — Wikipedia wikitext (action=raw), Gameplay 절 전문 |
| 13 | **Frozen Synapse** | 동시 턴 전술 | `direct page retrieval` — en.wikipedia.org/wiki/Frozen_Synapse |
| 14 | **Into the Breach** | 턴제 전술 | `direct page retrieval` — en.wikipedia.org/wiki/Into_the_Breach |
| 15 | **축구 승부차기 (IFAB)** | 스포츠 | `direct page retrieval` — en.wikipedia.org/wiki/Penalty_shoot-out_(association_football) |
| 16 | **미식축구 오버타임 (NFL/AFL/NCAA)** | 스포츠 | `direct page retrieval` — en.wikipedia.org/wiki/Overtime_(sports) |
| 17 | **야구 연장** | 스포츠 | `direct page retrieval` — 동일 문서 |
| 18 | **농구 (FIBA·3x3·Elam)** | 스포츠 | `direct page retrieval` — 동일 문서 |
| 19 | **크리켓 Super Over** | 스포츠 | `direct page retrieval` — 동일 문서 |
| — | **castle-war** | 턴제 포격 (대조군) | `direct page retrieval — 코드` |

> ⚠️ **표본 한계 4가지.**
> ① **Hedgewars / Pocket Tanks / 포트리스2는 표본에서 제외했다.** Hedgewars는 영문 위키백과에서 404,
> 나머지는 순서 규칙을 기술한 1차 페이지를 회수하지 못했다. **`thin evidence`로 표에 끼워 넣는 대신 뺐다.**
> ② **스포츠 4종(16~19)은 "경기 전체"가 아니라 "동점 처리 절차"** 를 표본으로 잡았다.
> 그 구간이 castle-war와 같은 조건 — *한 번 앞서면 상대의 응답 없이 끝난다* — 을 갖기 때문이다.
> ③ **포격 계보 4종(8~11)에서 선공 보정 장치를 하나도 찾지 못했다.** 이것은 "조사 실패"가 아니라
> **발견**이며 아래 순위표의 가장 중요한 행이다.
> ④ NFL의 승률 수치(2010–2022 플레이오프 코인토스 승자 10/12 = 83.3%, 사전 60~61%)는
> **`indexed snippet`** 이다 — 검색 요약이 theguardian/foxsports/bsu.edu를 근거로 제시했으나
> 본 런에서 해당 페이지를 직접 회수하지 않았다. **2022년 규칙 변경 사실 자체는 `direct`** 다.

### 빈도 순위 — 보정 장치

> **등급 규칙.** `direct` = 해당 룰셋의 1차 페이지를 본 런에서 회수해 확인한 칸.
> `inference` = 회수한 서술로부터 본 조사가 추론한 칸. 두 근거가 섞이면 쪼개 적는다.

| 순위 | 장치 | 출현 | 근거 분해 | 채택 룰셋 | castle-war |
|---|---|---|---|---|---|
| **0** | **T1 단순 교대 (기반값)** | `[OBSERVED 17/19]` | direct 17 | 체스·바둑·장기·Hex·HS·MtG·PTCG·Worms·SSL·SE·FS·ItB·승부차기·야구·농구·크리켓 (Rampart·Gunbound 제외) | **보유** |
| 1 | **T3 선택권 부여** | `[OBSERVED 4/19]` | direct 4 (PTCG 원문, MtG CR 103.1, NFL 토스, 승부차기 2016 골대 선택 코인토스) | Pokémon TCG, MtG, NFL, 승부차기 | ❌ |
| 1 | **C1 라운드 완주 보장** | `[OBSERVED 4/19]` | **direct 4** — NFL 2022 규칙 원문, 야구 "complete innings", 크리켓 각 6구, AFL/NFL Europe "guaranteed one possession" | NFL, 야구, 크리켓, (AFL·NFL Europe) | ❌ **불필요(선공 해결됨) + 넣으면 절벽 악화 69.1%→73.7%** |
| 3 | **R2 계열 후공 점수 가산 (덤·1.5점)** | `[OBSERVED 2/19]` | **direct 2** — 바둑 komi 6.5/7.5/7.0, 장기 후수 1.5점 | 바둑, 장기 | ❌ **가능** |
| 3 | **T2 랜덤 코인플립** | `[OBSERVED 3/19]` | direct 3 | Pokémon TCG, NFL, MtG(1게임) | ❌ |
| 3 | **C3 승리 조건 분리** | `[OBSERVED 3/19]` | **direct 3** — Rampart "fail to have at least one surrounded castle", Gunbound Score/Jewel 100점, ItB "protect civilian structures" | Rampart, Gunbound, Into the Breach | ❌ |
| 6 | **T5/M1 기간·매치 단위 선공 교대** | `[OBSERVED 3/19]` | direct 3 — 3x3 농구 원문, FIBA alternating possession, 축구 2연전 home/away. 컴퓨터 체스 2판제는 체스 문서 내 서술(direct) | 농구, 축구, (체스 대회 관행) | ❌ **가능(최저비용)** |
| 6 | **T6 파이 룰 / 스왑** | `[OBSERVED 2/19]` | **direct 2** — Hex(+TwixT·Meridians), 바둑 auction komi | Hex, 바둑 | ❌ **불가** |
| 8 | **S1 동시 턴 해결** | `[OBSERVED 2/19]` | **direct 2** — Frozen Synapse(~5초 결정론적), **Scorched Earth v1.2(1992) "Synchronous firing mode"** | Frozen Synapse, Scorched Earth | ❌ **조건부** |
| 8 | **R5 멀리건 상대 보상** | `[OBSERVED 2/19]` | direct 2 — PTCG "one additional card per mulligan", MtG London Mulligan | Pokémon TCG, MtG | ❌ 불가 |
| 10 | **N1 첫 턴 행동 제한** | `[OBSERVED 1/19]` | **direct 1** — PTCG "cannot attack or play a Supporter card on their first turn" | Pokémon TCG 단독 | ❌ **불가** |
| 10 | **N2 첫 턴 자원 박탈** | `[OBSERVED 1/19]` | **direct 1** — MtG **공식 CR 103.8a** | Magic: The Gathering 단독 | ❌ 불가 |
| 10 | **R1 후공 카드+임시 자원** | `[OBSERVED 1/19]` | **direct 1** — Hearthstone Wiki 원문 | Hearthstone 단독 | ❌ 조건부 |
| 10 | **T4 이전 게임 패자 선택** | `[OBSERVED 1/19]` | **direct 1** — MtG **공식 CR 103.1** | Magic: The Gathering 단독 | ❌ 조건부 |
| 10 | **T8 지연 자원화** | `[OBSERVED 1/19]` | **direct 1** — Gunbound 위키 원문 | Gunbound 단독 | ❌ **불가(대칭 붕괴)** |
| 10 | **S2 위상 동시 실시간** | `[OBSERVED 1/19]` | **direct 1** — Rampart Gameplay 절 | Rampart 단독 | ❌ 조건부 |
| 10 | **C2 턴 상한 + 생존 목표** | `[OBSERVED 1/19]` | **direct 1** — ItB "fixed number of turns" + 개발자 근거 인용 | Into the Breach 단독 | ▲ 사실상 보유, 규칙 미승격 |
| 10 | **C4 목표 비대칭** | `[OBSERVED 2/19]` | direct 2 — FS 모드 목록, ItB 건물 보호 | Frozen Synapse, Into the Breach | ❌ 불가 |
| 10 | **C5 목표 점수제 (Elam)** | `[OBSERVED 1/19]` | **direct 1** — 농구 Elam Ending 수치 5개(+8/+24/+7/+9/+11) | 농구 단독 | ❌ 불가 |
| 10 | **R6 패자 측 판 개입** | `[OBSERVED 1/19]` | **direct 1** — Gunbound 사망자 바람 변경 | Gunbound 단독 | ❌ 무관 |
| 20 | **T7 ABBA / Thue–Morse** | `[OBSERVED 1/19 — 그리고 폐기됨]` | **direct 1** — IFAB 2017 승인 → **2018-11-22 폐기** 원문 | 승부차기 (시험 후 철회) | ❌ **불가(권고)** |
| 20 | **R4 핸디캡 돌 + 최소 덤** | `[OBSERVED 1/19]` | direct 1 — 바둑 접바둑 | 바둑 단독 | ▲ 난이도 램프가 유사 역할 |
| — | **N3 첫 턴 피해 감소** | `[OBSERVED 0/19]` | **외부 채택 0건** — 표본 19개에서 확인 실패 | **castle-war 단독** | ✅ **보유, 실패 실측** |

### 이 조사에서 가장 중요한 숫자 — 계보별 교차 집계

**포격 계보(Worms, Gunbound, Scorched Earth, ShellShock Live) 4개 타이틀의 선공 보정 장치:**

| 계보 | 표본 | 후공 자원(R계) | 첫 턴 약화(N계) | 순서 보정(T3~T7) | 라운드 완주(C1) | 합계 |
|---|---|---|---|---|---|---|
| 턴제 포격 | 4 | **0** (Gunbound R6은 사망자 개입으로 별개 축) | **0** | **0** (Gunbound T8은 보정이 아니라 순서 재정의) | **0** | **0 / 16** |
| 추상 2인 (체스·바둑·장기·Hex) | 4 | 3 | 0 | 2 | 0 | **5 / 16** |
| TCG·CCG (HS·MtG·PTCG) | 3 | 3 | 2 | 4 | 0 | **9 / 12** |
| 스포츠 (승부차기·NFL·야구·농구·크리켓) | 5 | 0 | 0 | 4 | **4** | **8 / 20** |
| 턴제/동시 전술 (FS·ItB·Rampart) | 3 | 0 | 0 | 0 | 0 | **0 / 12** (대신 구조로 우회: C2·C3·C4·S1·S2) |
| **castle-war** | — | 0 | **1 (N3, 성공 — 47.0%/53.0%)** | 0 | **0** | **1 / 4** |

> **`[OBSERVED 0/4]` — 포격 계보 4개 타이틀 중 선공 보정 장치를 가진 것은 0개다.**
> 이것은 castle-war가 나쁘게 만들어졌다는 증거가 **아니다.** 계보 전체가 이 문제를 다루지 않았다.
> 대전 게임이라 두 플레이어가 번갈아 불운을 나눠 가졌을 뿐이고,
> **castle-war는 vs-AI 싱글이라 플레이어가 항상 같은 쪽에 앉는다**(`isPlayerTurn = true`).
> **그래서 castle-war는 계보에 없는 장치(N3)를 스스로 만들어야 했고, 만들었고, 작동한다**(47.0%/53.0%).
>
> ⚠️ **정정 후 이 집계가 뜻하는 것.** 이 표는 이제 *"castle-war가 무엇을 빠뜨렸나"* 가 아니라
> **"castle-war의 보정이 표본 어디에도 선례가 없다"** 를 보여준다. 그것은 두 가지로 읽힌다:
> - **위험 신호** — 19개 룰셋 중 곱셈 감쇠형 첫 턴 보정은 0건이다. 아무도 안 한 이유가 있을 수 있다(Lane C 과제).
> - **또는 구조적 필연** — N1(행동 제한)·N2(자원 박탈)는 **행동 범주 다수** 또는 **반복 자원**을 전제한다.
>   1턴 1발사·무자원 게임에서는 그 둘이 정의되지 않으므로, **남는 형태가 곱셈 감쇠뿐**이다.
>   즉 선례가 없는 것은 우리가 이상해서가 아니라 **우리 같은 구조가 표본에 없어서**일 수 있다. `[INFERENCE]`
>
> **그리고 이제 스포츠 계보의 C1은 권고가 아니라 경고다.** castle-war의 `CheckVictoryConditions()`가
> `currentHP <= 0`에서 즉시 `EndGame()`을 부르는 구조는 NFL이 2022년에 고친 실패 모드와 형태가 같지만,
> **선공 이점이 이미 밴드 안이므로 고칠 대상이 없고**, 아래 §실력 절벽에서 측정해 보면
> C1을 넣는 순간 **절벽이 69.1% → 73.7%로 나빠진다** — 선공 타이브레이크가 실력 신호를 가려주고 있었기 때문이다.

---

## Categories

장치 27종은 **"선공 이점이 승리로 번역되는 경로 중 어디를 끊는가"** 를 기준으로 다섯 무리로 갈린다.
이 축을 고른 이유는, castle-war에서 **어떤 무리는 구조적으로 차단돼 있고 어떤 무리는 배관이 이미 깔려 있어서**,
분류가 곧 우선순위표가 되기 때문이다.

**A. 순서 배분을 바꾼다** (T1·T2·T3·T4·T5·T6·T7·T8 — 8종)
→ 누가 먼저 하는지를 다시 정한다. 이점의 **크기는 그대로 두고 배분만** 바꾼다.
→ 트레이드오프: **개별 경기의 불공정은 남는다.** 무작위화(T2)는 절반의 경기를 나쁜 쪽에 앉히고,
   교대(T1·T5·M1)는 매치를 완주해야 상쇄된다. 파이 룰(T6)만이 경기 내에서 자기 조절되지만
   연속 공간·물리 게임에는 정의되지 않는다. T7은 **효과가 아니라 복잡성 때문에 실제로 폐기된** 사례다.
→ castle-war: T1 조건부, T5/M1 **가능(최저비용)**, T2·T3·T6·T7·T8 불가.

**B. 후공에게 자원을 준다** (R1·R2·R2-a·R3·R4·R5·R6 — 7종)
→ 이점을 없애지 않고 **반대편에 같은 크기의 것을 얹는다.**
→ 트레이드오프: **값을 알아야 한다.** 바둑이 100년(4.5→5.5→6.5)에 걸쳐 실측으로 올려온 것이 그 대가다.
   반대로 값을 **스칼라 하나로 압축**할 수 있다는 것이 이 무리의 최대 강점이다 —
   규칙·구조·연출을 하나도 건드리지 않고 숫자만 스윕할 수 있다.
→ castle-war: **R2가 가장 직역 가능하다.** 코어 HP 150이 이미 점수판이므로
   `후공 코어 HP = 150 + Δ`가 곧 덤이며, 19.4턴/성이라는 측정치가 Δ의 초기 후보를 준다.
   R1·R4·R5·R6는 castle-war에 해당 자원 축이 없어 불가·조건부.

**C. 첫 턴을 깎는다** (N1·N2·N3 — 3종)
→ 이점의 **원천을 직접 축소**한다. 순서도 자원도 그대로.
→ 트레이드오프: **깎을 것이 있어야 한다.** MtG는 카드 1장(CR 103.8a), Pokémon TCG는 공격·Supporter를 깎는다.
   둘 다 **행동 범주가 여럿이거나 반복 자원이 있다**는 전제에 기댄다.
→ castle-war: **이 무리는 구조적으로 막혀 있다.** 1턴 1발사(`OneShotTurnGate`)라 행동 범주가 하나뿐이고
   매 턴 갱신 자원이 없다. 남는 유일한 형태가 N3(피해 곱셈)이며 **그것을 이미 넣었고 실패했다** —
   1발 50%는 38.8턴 경기 총 피해의 약 2.6%다. 이 무리에서 더 가져올 것은
   **"첫 발"이 아니라 "여러 턴에 걸친 감쇠"** 뿐이고, 그것은 밸런스 전면 재측정을 뜻한다.

**D. 동시성으로 순서를 없앤다** (S1·S2 — 2종)
→ 선공이라는 **개념 자체를 제거**한다. 보정할 것이 남지 않는다.
→ 트레이드오프: **경기 길이와 인과 판독이 대가다.** castle-war는 발사 후 물리 정착까지 대기하므로
   동시화는 38.8턴 → 약 19.4해결로 **경기를 절반 가까이 줄인다** — 291초가 허용 하단 240초를
   뚫고 나갈 수 있다. 그리고 두 발사체·두 붕괴가 겹치면 "무엇이 왜 무너졌나"가 사라진다.
→ **가장 중요한 선례:** Scorched Earth는 동시 발사를 **기본이 아니라 v1.2의 별도 모드로** 출하했다.
   포격 계보의 원형이 이 장치를 알았고, 기본값으로 삼지 않기로 했다.
→ castle-war: S1·S2 모두 조건부. **Rampart(성·대포·성벽)가 소재 유사성 1위**라는 점은 기록할 값이 있다.

**E. 구조로 없앤다 — 종료 규칙과 승리 조건** (C1·C2·C3·C4·C5 + M1·M2 — 7종)
→ 이점이 **승리로 번역되는 마지막 단계**를 끊는다. 순서·자원·피해를 전혀 건드리지 않는다.
→ 트레이드오프가 무리 안에서 극단적으로 갈린다:
   - **C1(라운드 완주)** — 대가가 턴 1개(약 7.5초)와 동점 규칙 하나. **이 표에서 비용 대비 표적 정확도가 가장 높다.**
   - **C3·C4(승리 조건 재정의)** — 게임이 다른 게임이 된다. 스테이지 2·3의
     *"더 긴 싸움이 아니라 다른 싸움"* 원칙에는 맞지만 스테이지 1을 구하지 못한다.
   - **C5(목표 점수제)** — 300초 예산을 포기해야 하므로 castle-war에서는 불가.
   - **M1·M2(매치 단위)** — 개별 경기의 불공정은 남지만 **`SiegeSeries`가 이미 존재**해 배관 비용이 사실상 0.
→ castle-war: **C1 가능, M1 가능, C2 사실상 보유(규칙 미승격), C3 조건부(후반 스테이지), C4·C5 불가.**

> **분류가 내놓은 한 줄 (전제 정정 반영):**
> castle-war에서 막힌 것과 열린 것이 **무리 단위로 갈린다.**
> **C(첫 턴 깎기)는 형태가 하나로 좁혀져 있다** — 1턴 1발사·무자원이라 N1·N2가 정의되지 않고
> **곱셈 감쇠(N3)만 가능하다.** 그 하나를 넣었고 **작동했다**(47.0%/53.0%).
> **B(후공 자원)는 R2 하나만 열려 있다** — 코어 HP가 점수판이기 때문이다.
> **E(구조)와 A(순서)는 배관이 깔려 있다** — `CheckVictoryConditions()`, `SiegeSeries`,
> `SiegeDuelSimulation(alternateFirstMove:)`가 전부 존재한다. **다만 지금은 쓸 이유가 없다.**
>
> **선공 문제가 닫혔으므로 이 카탈로그의 용도가 바뀐다.**
> A·B·E 무리는 **"쓸 후보"에서 "쓰지 말아야 할 이유가 문서화된 목록"** 이 되었다 —
> 선공이 이미 밴드 안일 때 이 장치들을 추가로 넣으면 **밴드를 반대편으로 밀어낸다.**
> 특히 C1은 절벽을 악화시키므로(69.1% → 73.7%) **명시적 비추천**이다.
> 이 카탈로그가 지금 답하는 질문은 아래 §실력 절벽으로 이동한다.

---

## Curated Sources

> **라벨 규칙.** `direct page retrieval` = 본 런에서 해당 URL을 직접 회수해 원문을 읽음.
> `indexed snippet` = 검색 도구의 요약이 근거 도메인을 제시했으나 해당 페이지를 직접 회수하지 않음.
> `thin evidence` = 회수 실패 또는 2차 종합 수준. **검색 요약을 1차 인용처럼 쓰지 않았다.**

### 1차 출처 — 공식 규칙 문서

| # | 출처 | 강도 | 무엇을 확인해 주는가 |
|---|---|---|---|
| 1 | `https://media.wizards.com/2025/downloads/MagicCompRules%2020250207.txt` (Wizards of the Coast, **공식 Comprehensive Rules 원문**) | `direct page retrieval` | **CR 103.8a** — *"In a two-player game, the player who plays first skips the draw step of their first turn"* (N2의 정확한 비용 = 카드 1장). **CR 103.8c** — 다인전에서는 스킵하지 않음(장치를 끄는 조건까지 규칙화). **CR 103.1** — *"In a match of several games, the loser of the previous game chooses who takes the first turn"* (T4). 본 문서에서 **유일한 게임사 1차 규칙 파일** |

### 1차 출처 — 공식/준공식 위키

| # | 출처 | 강도 | 무엇을 확인해 주는가 |
|---|---|---|---|
| 2 | `https://hearthstone.wiki.gg/wiki/The_Coin` | `direct page retrieval` | R1의 정확한 구성 — 후공에게 부여, **"Together with the fourth starting card"**, 카드 텍스트 **"Gain 1 Mana Crystal this turn only"**, 비용 0, 종류 Spell. Blizzard 공식이 아닌 **커뮤니티 위키(2차)** 이나 카드 데이터 자체는 게임 내 값 |
| 3 | `https://en.wikipedia.org/wiki/Komi_(Go)` | `direct page retrieval` | R2·R2-a 전부 — 표준 **6.5**(일본·한국) / **7.5**(중국·AGA·Ing) / **7.0**(뉴질랜드), 접바둑 **0.5**, 역사 이력(Hisekai 1922년 **4.5** → 2.5/3 → 4.5 → **5.5** → **6.5**), **5.5 시절 흑 승률 53%** 가 인상 근거였다는 서술, 완전한 덤은 홀수 정수이며 통계가 **7**을 지지 |
| 4 | `https://en.wikipedia.org/wiki/Pie_rule` | `direct page retrieval` | T6 — 파이 룰 정의(두 선택지), **최초 기록 1909년 Mancala 계열**, 채택 게임(Hex·TwixT 대회·Meridians는 돌 2개), 바둑 auction komi 응용, "무승부 없는 게임에서는 이론상 후공 승이나 실전에서는 균형" |
| 5 | `https://en.wikipedia.org/wiki/First-move_advantage_in_chess` | `direct page retrieval` | 선공 이점의 **측정 방법론과 규모** — 승점률 52~56%, Streeter 5,598기 53.4%, Chessgames 739,769기 **54.95%**(W37.50/D34.90/L27.60), Sonas 266,000기 회귀 **54.1767% + 0.001164×Elo차** = **35 Elo 상당**, Kaufman "템포 1개 ≈ 폰 0.4, 선공 ≈ 0.20". **장기(janggi) 후수 1.5점**(R3) 서술도 이 문서 |
| 6 | `https://en.wikipedia.org/wiki/Penalty_shoot-out_(association_football)` | `direct page retrieval` | T1(5킥 교대)·T7 전부 — Palacios-Huerta **선축 팀 60% 승** 주장과 **Thue–Morse 제안**, InStat **2,000+ 승부차기에서 51.48%**, **2024년 연구는 현대 유럽 축구에서 선축 이점 없음**, IFAB **2017-03 ABBA 시험 승인** → 최초 적용(2017 여자 U-17 준결승 독일 v 노르웨이, 2017 FA Community Shield) → **2018-11-22 글래스고 133차 총회에서 폐기, 사유 "lack of strong support, mainly because of its complexity"** |
| 7 | `https://en.wikipedia.org/wiki/Overtime_(sports)` | `direct page retrieval` | C1의 **네 가지 독립 채택 사례** — NFL **2022년 플레이오프 규칙 변경**(양팀 1회 공격 보장, 2025년 정규시즌 확대)과 그 계기(**Super Bowl LI: "the Patriots scored a touchdown on their initial possession, so the Falcons never received the ball in overtime"**), **AFL·NFL Europe "each team is guaranteed one possession"**, 야구 **"Complete innings are played"**, 크리켓 **Super Over 각 6구**. 추가로 T5(**3x3 농구 "The team that did not get first possession in the game gets first possession in overtime"**, FIBA alternating possession)와 C5(Elam Ending 수치 **+8/+24/+7/+9/+11**) |
| 8 | `https://en.wikipedia.org/wiki/Pok%C3%A9mon_Trading_Card_Game` | `direct page retrieval` | N1 — **"The player going first cannot attack or play a Supporter card on their first turn"**, 진화 카드 첫 턴 불가. T2·T3 — 코인플립 승자가 선/후 결정. R5 — **"the opponent may draw one additional card per mulligan"** |

### 1차 출처 — 게임 문서 (포격·성 계보)

| # | 출처 | 강도 | 무엇을 확인해 주는가 |
|---|---|---|---|
| 9 | `https://en.wikipedia.org/wiki/Rampart_(video_game)` (**wikitext, `action=raw`**) | `direct page retrieval` | S2·C3 — **"Gameplay alternates between two time-limited phases: combat and building"**, **"In multi-player mode, the players shoot at each other's walls"**, 2인전 전투는 **"after a set time"** 종료, **"The player loses when they fail to have at least one surrounded castle after the tile-placement phase"**, 양쪽 생존 시 **"the one with the higher score is declared the winner"**. 오픈은 자동 건설 페이즈. ※ 위키백과 API 요약은 리드만 반환해 **wikitext 직접 회수로 Gameplay 절 전문을 얻었다** |
| 10 | `https://en.wikipedia.org/wiki/Scorched_Earth_(video_game)` | `direct page retrieval` | S1의 **계보 내 선례** — **v1.2(1992)에 "Synchronous firing mode" 추가**. 조준 보조로 tracers("adjust the trajectory on their next turn"), 방어 자원(deflector shields·recharge batteries·parachutes)이 **"much harder to score a kill with a single hit"** 를 만든다는 서술. **선공 보정 장치는 문서에 없음** |
| 11 | `https://en.wikipedia.org/wiki/Gunbound` | `direct page retrieval` | T8 — **"a 'delay' turn system which is influenced by the Mobile, the weapon and/or item a player uses — using items or taking time with actions results in a longer wait before the player's next turn"**. R6 — 사망자가 슬롯으로 **바람 조건 변경**·아이템/폭탄 투하. C3 — Score/Jewel 모드 **100점** 승리. **선공 보정 장치는 문서에 없음** |
| 12 | `https://en.wikipedia.org/wiki/Worms_(series)` | `direct page retrieval` | 턴 제한 존재(**"Each turn is time-limited"**), 결과 보고 시점(**"after any player's turn, when all movement on the battlefield has ceased"**), sudden death는 **경기 길이** 장치. **선공 보정 장치는 문서에 없음 — 이것이 발견이다** |
| 13 | `https://en.wikipedia.org/wiki/ShellShock_Live` (**wikitext, `action=raw`**) | `direct page retrieval` | 각도 360°·파워 0–100, 최대 8인, 9개 모드, 400+ 무기. **선공/순서 규칙에 대한 서술 0건.** ※ 문서 자체에 `{{Primary sources}}`·`{{More citations needed}}` 배너가 붙어 있음 |
| 14 | `https://en.wikipedia.org/wiki/Frozen_Synapse` | `direct page retrieval` | S1 — 계획 시간 무제한, 커밋 후 **약 5초 시뮬레이션 동시 해결**, 전투는 무기·자세·poise 기반 **결정론적**, 결과 리플레이·예측 시뮬 제공. C4 — hostage protection/escort/area protection 등 비대칭 목표 |
| 15 | `https://en.wikipedia.org/wiki/Into_the_Breach` | `direct page retrieval` | C2·C3 — **"an objective for that map along with a fixed number of turns"**, 주 목표는 **"protect civilian structures which support the power grid"**, 8×8 격자·메크 3기. 개발자 근거: **"The limited turn counter was used to keep battles short, and Subset found that telegraphing the Vek's movements further helped to hasten the pace"** |

### 대조군 — castle-war 코드 (실측)

| # | 출처 | 강도 | 무엇을 확인해 주는가 |
|---|---|---|---|
| 16 | `Assets/Scripts/OneShotSiegeRules.cs:20, 27-39` | `direct page retrieval — 코드` | **N3이 이미 출하돼 있다.** `OpeningVolleyDamageScale = 0.5f`; `OpeningVolleyDamageMultiplier(completedTurns) => completedTurns <= 0 ? 0.5f : 1f`. 주석의 *"Reducing only that opening volley to 50% removes the measured 87% first-mover win rate"* 는 **Main 재측정으로 확인됐다**(고정 선공 47.0% / 교대 53.0%). 또한 `ProjectileForTurn(completedTurns)`이 **라운드(2턴)마다** 순환해 양 진영이 같은 투사체를 받는다 |
| 17 | `Assets/Scripts/GameManager.cs:177-197, 1865, 2387-2402, 2439-2446, 2509-2517` | `direct page retrieval — 코드` | ① `CaptureDamageMultiplier`가 `damageFromPlayer == true && IsPlayerTurn && ShotCommitted`로 게이팅 → **플레이어의 첫 발만** 감쇠. ② `:1865` `isPlayerTurn = true` **무조건**, 시리즈 경기 번호를 읽지 않음. ③ `CheckVictoryConditions()`가 `currentHP <= 0`에서 **즉시** `EndGame()` → **C1 부재**. ④ `SiegeSeries` 집계(2승/최대 3경기)와 `seriesGamesPlayed` static 생존 → **M1 배관 존재** |
| 18 | `Assets/Scripts/SiegeDuelSimulation.cs:33-44, 132-134, 156-199` | `direct page retrieval — 코드` | `RunSeries(alternateFirstMove:)`와 `firstMoverWinRate`가 **이미 구현돼 있다** — T1/M1 재측정 비용 0. 주석이 투사체 라운드 순환을 *"the fairness device"* 로 명시. 동시에 이 시뮬은 *"cannot catch a balance fault that only appears once blocks fall on each other"* 라고 스스로 한계를 적어 두었다 |
| 19 | `Assets/Scripts/SiegeSeries.cs:14-17`, `SiegeEcosystem.cs:150, 176-178, 189, 291-292` | `direct page retrieval — 코드` | M1·M2 — `WinsNeeded = 2`, `MaxGames = 3`, `GAME n/3` 배너, `SeriesScore(seriesScoreTotal, ...)` 등급 산출, "다음 경기 (n/3)" 버튼. **매치 단위 장치의 표시 계층까지 완비** |

### 강도가 낮은 출처 — 결론에 사용하지 않았거나 제한적으로만 사용

| # | 출처 | 강도 | 처리 |
|---|---|---|---|
| 20 | NFL 오버타임 승률 수치(2010–2022 플레이오프 코인토스 승자 **10/12 = 83.3%**, 사전 시대 **60~61%**, 2010/2012 개정 후 **50~53%**) — 검색 요약이 theguardian.com·foxsports.com·bsu.edu·researchgate.net을 근거로 제시 | `indexed snippet` | **본문에서 수치에 라벨을 붙여 표기했고, C1 권고의 근거로는 쓰지 않았다.** C1 권고는 **규칙 변경 사실**(`direct`, 출처 7)과 **castle-war 코드**(`direct`, 출처 17)만으로 성립한다 |
| 21 | Hearthstone 보정 후 잔존 선공 우위("몇 %p") | `thin evidence` | 검색 요약만 존재. **수치를 인용하지 않고 "정확한 수치는 회수 실패"로 표기** |
| 22 | Hedgewars — `en.wikipedia.org/wiki/Hedgewars` **HTTP 404**(위키백과 API·wikitext 양쪽) | `회수 실패` | **표본에서 제외.** 추측으로 채우지 않음 |
| 23 | Pocket Tanks, 포트리스2, Artillery(1980s BASIC 계보) | `회수 실패` | Artillery_game 문서는 본 런에서 **리드 14줄만** 반환됐고 순서 규칙 서술이 없었다. **표본에서 제외** |
| 24 | Starcraft 맵 밸런싱, MOBA 첫 블루 버프, Pokémon VGC 스피드 타이(50/50), TFT 연패 골드 | `미조사` | 시간 배분상 회수하지 않았다. **표에 넣지 않았다** — 있었다고 쓰지 않기 위해서. Lane D가 다룰 여지로 남긴다 |

### 이 레인이 회수하지 못해 남기는 공백 (다음 세션용)

1. **N3(첫 턴 피해 감소) 외부 채택 사례.** 표본 19개에서 0건이다. 표본을 넓히면(격투 게임 첫 라운드,
   RTS 초반 러시 억제, 클래시 로열류) 나올 수 있다. 현재 판정 *"castle-war 단독"* 은 **표본 19개 범위 내에서만** 유효하다.
2. **Hearthstone The Coin 도입 후 잔존 선공 승률의 1차 수치.** Blizzard 공식 밸런스 포스트를 회수하지 못했다.
   R1의 "보정해도 잔차가 남는다"는 주장이 현재 `thin evidence`에 걸려 있다.
3. **바둑 덤 인상 시 승률이 실제로 얼마나 움직였는가.** 위키는 5.5 시절 흑 53%만 준다.
   6.5 이후 수치가 있으면 **R2의 Δ 스윕에 직접 쓸 수 있는 유일한 실증 곡선**이 된다.
4. **Rampart 2인전의 실제 승률 편향.** 페이즈 동시성이 편향을 0으로 만들었는지에 대한 데이터가 없다.
   S2 권고가 지금은 "구조상 편향이 생길 수 없다"는 **논증**에 기대고 있다.

---

## 실력 절벽 — 추가 조사

- 과제: **실력 표현을 경사로 만드는 장치**를 카탈로그에 추가한다 — 발사당 분산, 바람 같은 잡음,
  조준 보조, 피해 감쇠, 다중 발사, 명중 판정 폭. 각각 채택 게임과 수치.
- 이 절은 위 카탈로그와 **다른 문제**를 다룬다. 위는 *순서*, 여기는 *실력*이다.

> **Main 측정치(출발점):** 조준 품질 **+0.01(1%p)** 이 승률을 **53.0% → 67.0%**, **+0.03 → 94%**, **+0.05 → 100%**.
> 수익 게이트 *"과금 승률 격차 ≤5%p"* 가 **조준 우위 0.36%p**에서 전부 소진된다.

> ## 🛑 이 절 전체에 걸리는 정정 (2026-08-14, 종료 직전)
>
> **아래의 절벽 수치는 심 모델의 성질이고, 실물 게임의 성질이 아닐 가능성이 높다.**
> `qa/b1-measurement-findings.md:62-66`이 **샷당 피해 CV를 이미 실측해 두었다.**
> 본 레인이 그 파일을 읽지 않고 시작한 것이 원인이다 — triage가 이름까지 지정한 파일이며 **본 레인의 누락**이다.
>
> | | 평균 | 0피해 턴 | **실측 CV** |
> |---|---|---|---|
> | Stage1 | 96.59 | 6/22 (27%) | **1.50** |
> | Stage2 | 128.33 | 1/6 (17%) | **0.70** |
> | Stage3(재측정) | — | **43%** | **1.39** |
>
> **본 절의 모델 가정은 CV 0.0847이다. 실측은 0.70~1.50 — 8~18배 차이다.**
> `sd(N) = √(1440/μ) × CV`에 실측을 넣으면:
>
> | | sd(발수) | 조준 +0.01 예측 승률 |
> |---|---|---|
> | 본 절의 모델 가정 | 0.50 | 69.1% (아래 전부) |
> | 실측 Stage2 (CV 0.70) | **2.34** | **53.3%** |
> | 실측 Stage1 (CV 1.50) | **5.79** | **51.4%** |
>
> **요건은 sd ≥ 1.56발이었다. 실측 최저값조차 이미 1.5배 초과 달성한다.**
>
> **이유는 명확하다.** `SiegeDuelSimulation`의 피해식은 `base × clamp01(quality)` — **연속이고 0이 절대 안 나온다.**
> 실물은 **0피해가 17~43%**, 최대 671(평균 96.59)인 두꺼운 꼬리다. 심에는 물리·붕괴 연쇄·자기 파괴가
> 없으므로 그 분산이 전부 빠져 있다. b1 문서도 살아남는 발견으로 *"스테이지 내부의 두꺼운 꼬리
> (재측정 Stage3도 CV 1.39, 0피해 43%)"* 를 적어 두었다.
>
> **무엇이 무효화되는가:**
> - ❌ §0~§2의 **절벽 크기**(69.1%, 게이트 0.25%p)와 그로부터 나온 **개입 필요성**. 실측 CV에서는 51~53%다.
> - ❌ **G2(이산 명중 판정) 권고.** 실물이 이미 0피해 17~43%를 내고 있다 — **이산 명중 판정을 사실상 보유 중**이다.
>   추가하면 중복이며 본 절이 측정한 과잉 구간(조준이 승률에 전혀 영향 없음)으로 넘어갈 위험이 있다.
> - ⚠️ §4-⑤의 치석 정밀도 요건(잔차 ≤0.32%p)은 **sd에 비례하므로 실측 sd에서는 훨씬 느슨해진다.**
>
> **무엇이 살아남는가:**
> - ✅ **닫힌 형태 `승률 = Φ(Δ발수/(sd·√2))`와 `sd(N) = √N × CV`.** 이것이 실측 CV를 곧바로
>   승률 예측으로 바꿔 주는 도구이며, 오히려 이 정정을 **가능하게 한 것**이 이 공식이다.
> - ✅ **§4-⑥ 측정 레시피**(발수 대신 샷당 피해, 검열 논증, 표본 효율 20배). b1이 정확히 그 방식으로
>   측정했고 CV를 보고했다 — **레시피가 독립적으로 검증된 셈**이다.
> - ✅ **C1·다중발사 역효과 판정.** "분산을 줄이는 장치"라는 성질은 sd가 0.50이든 5.79든 같다.
>   오히려 실측 sd가 크면 그것을 깎는 손실이 더 크다.
> - ✅ **G1 철회와 그 함정 기록.** 심 상수 문제는 CV와 무관하게 그대로다.
>
> **다음 행동:** Main의 CV 계측의 역할이 *"미지를 재는 것"* 에서 **"두 값을 대조하는 것"** 으로 바뀐다.
> 계측이 CV 0.7~1.5를 재확인하면 → **절벽 없음, 개입 불필요.** 0.1대를 재면 → b1이 틀렸고 절벽 실재.
>
> **정직한 한계 3개.** ① b1도 스크립트 플레이어(45°·당김 86%, 학습 없음) 측정이라 인간 학습 효과가 없다.
> ② 표본이 22발·6발로 작다. ③ 본 절의 `Φ()` 근사는 심 분포(거의 정규)에 대해 검증했고 CV 1.5는
> 두꺼운 꼬리라 근사가 거칠어진다 — 51~53%는 **방향과 크기의 추정이며 정밀값이 아니다.**
> **그래도 8~18배 CV 차이는 근사 오차로 설명되지 않는다.**

### 0. 먼저: 왜 절벽인가 — 원인을 수치로 특정했다

장치를 고르기 전에 절벽의 원인을 알아야 한다. `SiegeDuelSimulation.RunMatch`의 피해 모델을
**그대로 파이썬으로 이식해 재현**했다(`direct page retrieval — 코드` → 독립 재구현):

```
damage = baseShotDamage × projectileMultiplier × openingVolley(turns) × clamp01(quality)
quality = base ± uniform(-1,1) × beginnerAimError        ← 양측 독립 스트림
```

`SiegeBalanceSettings.Default` 실측값: `wallBlockCount=12`, `wallBlockHp=90`, `coreHp=360`,
`baseShotDamage=106`, `secondsPerTurn=7.5`, `fixedAimQuality=0.70`, `beginnerAimError=0.09`.
→ `KeepDurability = 12×90+360 = 1440`, `1440 / (106×0.70) = **19.4발**` — triage의 19.4턴과 일치.

**재현 결과 (교대, 25,000경기):**

| 조준 우위 | 본 재현 | Main 측정 |
|---|---|---|
| +0.00 | 49.1% | 53.0% |
| +0.01 | **69.1%** | **67.0%** |
| +0.02 | 84.2% | — |
| +0.03 | **93.6%** | **94%** |
| +0.05 | 99.4% | 100% |

> ⚠️ **일치와 불일치를 분리해 적는다.** +0.03·+0.05는 사실상 일치(93.6 vs 94, 99.4 vs 100)하고
> **절벽의 형태는 독립적으로 재현됐다.** 등속 지점(49.1 vs 53.0)은 다르다 —
> `SiegeDuelSimulation.RequiredMatches = 100`이고 코드 주석이 **"100 halves that to about ±5%p"**
> 라고 스스로 적고 있으므로 Main의 두 값은 각각 ±5%p를 달고 있다. 5%p 차이는 **1σ 이내**다.
> 본 재현은 25,000경기로 ±0.3%p이므로 절벽 판정에는 본 재현 쪽을 쓴다.
> 단 두 모델 모두 `SiegeDuelSimulation` 주석의 자기 한계 — *"cannot catch a balance fault that
> only appears once blocks fall on each other"* — 를 그대로 물려받는다. **물리·붕괴는 없다.**

**원인은 신호 대 잡음비다.** 절벽의 정체를 `승리 = Φ(Δ발수 / (sd·√2))` 로 닫힌 형태로 특정했다:

| | sd(성 1개 파괴에 걸리는 발수) | 예측 승률(+0.01) | 실측 |
|---|---|---|---|
| 현재 (shipped) | **0.50발** | 65.3% | 69.1% |
| 발당 분산 ×2 | 0.93발 | 58.3% | 60.5% |
| 발당 분산 ×4 | 1.32발 | 55.9% | 54.9% |
| 명중 판정(binary) | **2.87발** | 52.7% | 52.3% |

> **닫힌 형태가 17%p 구간에서 오차 4%p 이내로 실측을 예측한다.** 따라서:
>
> **`d(발수)/d(조준) = 1440/(106×0.70²) = 27.7발`.** 조준 +0.01은 **0.28발 일찍 도달**하게 만든다.
> 그런데 **현재 발수 분포의 sd가 0.50발뿐**이다. 즉 1%p 조준 우위가 **0.55σ**를 움직인다.
> **절벽은 조준이 강력해서가 아니라 잡음이 없어서 생긴다.** `[INFERENCE — 재현 실측 기반]`
>
> **이것이 N3(개막 발사 50%)가 왜 그렇게 잘 들었는지도 설명한다.** 1발 절반은 총 피해의 2.6%이고
> 그것이 곧 **0.5발 = sd 1개분**이다. **같은 민감도가 선공 보정을 쉽게 만들고 실력 절벽을 만든다.**
> 하나의 원인, 두 개의 증상이다.

### 1. 장치 카탈로그 — 실력 표현을 경사로 만드는 것

`측정` 칸은 위 재현 모델에 그 장치를 넣고 25,000경기를 돌린 결과다(`+0.01`에서의 승률 / 게이트).
`게이트` = **승률을 55%까지 밀어올리는 조준 우위** — 클수록 좋다(현재 0.25%p).

| 이름 | 채택 게임 (수치) | 작동 방식 | 강점 | 약점 | 측정 (본 모델) |
|---|---|---|---|---|---|
| **G1. 발사당 분산 확대** ⚠️**철회 — 함정 기록** | **Scorched Earth** — 발사체가 *"partially random effects"*, 바람·실드·유도 장치가 궤적을 교란 | 조준 품질에 곱해지는 잡음의 폭을 키운다 | **철회 전 근거(그대로 남긴다):** 절벽의 원인(sd 0.50)을 직접 겨냥하고, **게이트 1.8배**, **경기 길이 영향 0**(307초 유지), **상수 1개**. `beginnerAimError = 0.09`가 `SiegeBalanceSettings`에 이미 있으므로 "손잡이가 이미 코드에 있다"고 판단했다 — **이것이 함정이었다** | **❌ `beginnerAimError`는 게임 손잡이가 아니다 — 심 전용이다.** 전수 grep: `MatchLengthModel`(정의+`SiegePacingSimulation`), `SiegeDuelSimulation.cs:137-138`, `Editor/G2Measurement.cs`(출력), 테스트 2개 — **게임플레이 코드 0건.** 플레이어 조준은 `LaunchManager` 드래그이고 **오차 항이 아예 없다.** 즉 이 상수는 인간 오차를 **모델링**하고 **제어하지 않는다.** 0.18로 바꾸면 **심 출력만 움직이고 게임은 그대로다.**<br>**→ 반복되는 함정: 심 상수는 인간 오차의 모델이지 손잡이가 아니다.** 카탈로그에 "손잡이가 이미 있다"고 쓰기 전에 **게임플레이 코드에서의 사용처**를 grep해야 한다 | ×2(0.18): 60.5%, 게이트 0.45%p<br>×4(0.36): 54.9%, 게이트 0.95%p<br>**⚠️ 조건부 값이다 — "인간의 실제 발간 오차가 그만큼이라면"의 의미이고 실행 가능한 변경이 아니다** |
| **G2. 명중 판정 폭 — 이산 명중/실패** | **XCOM 2**(2016, Firaxis) — UI가 명중 확률을 % 로 표시하고 *"At a high percentage of chance, they can still miss their shots while at a low percentage, players may be able to land some hits"*. 개발자 Solomon: 팀이 *"the idea of unpredictability and randomness"* 를 **의도적으로** 중시 | 피해를 연속량이 아니라 **베르누이 시행**으로 만든다. 조준 품질 = 명중 확률 | **본 조사 최강 장치.** 게이트가 **6.6배** 넓어진다. 경기 길이도 밴드 안(41.7턴 313초). 발수 sd가 0.50 → **2.87**로 5.7배 | 물리 포격 게임에서 "맞았는데 0 피해"는 정당화가 어렵다. 성벽 붕괴 연출과 충돌 | **52.3%**, 게이트 **1.65%p** (6.6×) |
| **G3. 피해 상한 (per-shot cap)** | **골프 — Equitable Stroke Control**(USGA, 1974 도입) 및 **net double bogey**: 핸디캡 계산 시 홀당 최대 타수를 고정해 *"reduce the impact of very high scores on one or more individual holes"* | 한 발이 낼 수 있는 최대 피해를 자른다 | 잘 쏜 샷의 상한을 눌러 격차 누적을 막는다. 개념이 단순 | **castle-war에서는 거의 듣지 않는다** — 상한이 꼬리만 자르기 때문. 그리고 0.60까지 낮추면 **조준이 무의미해진다**(모든 델타에서 정확히 50.0%) | cap 0.75: 66.1%, 게이트 0.30%p (1.2×)<br>cap 0.60: **50.0% 전 구간 — 실력 소멸** |
| **G4. 피해 하한 (floor)** | **골프 핸디캡 — "potential/average best"** 개념: 핸디캡은 평균이 아니라 *"average best"* 를 반영해 최악값의 영향을 제거 | 못 맞혀도 최소 피해를 보장 | 초보의 "아무것도 못 했다"를 없앤다 | **본 모델에서 완전 무효.** 하한 0.55는 작동 구간(0.70±0.09 = 0.61~0.79) **아래**라 한 번도 걸리지 않는다 — 작동 구간 밖의 하한은 장치가 아니다 | floor 0.55: **69.1%, 게이트 0.25%p — 베이스라인과 동일(변화 0)** |
| **G5. 조준→피해 사상 압축 (sublinear)** | **골프 — "bonus of excellence" 계수.** USGA가 최고 10개 차이값 평균에 **×0.96**을 곱한다. 원래 **85%** 였고 *"changed to 96% after being seen to favor better players too heavily"* — **보정 계수를 실측으로 재조정한 문서화된 선례** | `damage ∝ quality^γ`, γ<1. 조준 차이를 피해 차이로 덜 번역 | 분산을 늘리지 않고 절벽을 눕힌다. **G1보다 효율이 좋다** | 경기 길이가 줄어든다(34.2턴 256초 — 밴드 하단 240에 접근). γ=0.25에서는 **실력 소멸**(51.1%) | γ=0.5: **59.7%**, 게이트 **0.525%p** (2.1×)<br>γ=0.25: 51.1% — 실력 거의 소멸 |
| **G6. 잡음원 추가 — 바람·환경** | **Gunbound** — *"terrain condition, wind currents and elemental phenomena force players to continuously change their aim"*; 사망자가 **살아있는 플레이어의 바람을 바꾼다**. **Scorched Earth** — gravity·wind·meteorite showers를 옵션화. **castle-war** — `CurrentWindCap`(`windCapStart→windCapEnd`), `CurrentStormChance`, 거리 비례 강화 | 조준 이외의 변수를 판에 넣어 조준 실력의 비중을 낮춘다 | **castle-war가 이미 보유**하고 난이도 곡선에 묶어 두었다. 서사적 정당화가 이미 됨 | 본 모델(물리 없음)에서 **측정 불가.** 바람은 `SiegeDuelSimulation`에 존재하지 않는다 | **미측정** — 실물 PlayMode 필요. 다만 바람은 결국 조준 품질 분산으로 환원되므로 **G1과 같은 축일 가능성이 높다** `[INFERENCE]` |
| **G7. 조준 보조 (aim assist)** | **GunboundM**(2017) — 원작 15년 뒤 *"a visible bullet path for players to aim their shots"* 추가. **Angry Birds** — Chillingo가 최종 폴리시에 궤적선 추가. **Scorched Earth** — *"All weapons can be upgraded with **tracers** which allow the player to more accurately adjust the trajectory on their next turn"* | 조준 난이도 자체를 낮춘다 | 초보의 절대 조준 품질을 올린다 | **절벽을 완화하지 않는다 — 이동시킨다.** 모두의 base가 올라가면 `d(발수)/d(조준) = D/(dmg×q²)`의 q가 커져 **민감도는 오히려 떨어지지만**, 상대 격차 Δ는 그대로다. castle-war는 **이미 궤적 프리뷰 보유**(`LaunchManager` 300스텝×0.02s) | **미측정.** base를 0.70→0.85로 올리면 `d(발수)/d(조준)`이 27.7 → 19.0으로 **31% 완화**되나, 절벽 제거에는 부족 `[INFERENCE — 해석적]` |
| **G8. 다중 발사 (turn당 여러 발)** | **Worms** — 팀당 여러 마리, 매 턴 하나를 골라 행동. **ShellShock Live** — 최대 8인. **Scorched Earth** — MIRV 등 분열 탄두 | 한 턴의 결과를 여러 시행의 평균으로 만든다 | 한 발의 불운이 턴을 망치지 않는다 | **역효과다.** 평균화는 분산을 **줄여** 절벽을 세운다 — 큰 수의 법칙이 정확히 반대로 작동 | 3발/턴: **74.4%**, 게이트 **0.20%p** — **베이스라인보다 나쁨** |
| **G9. 러버밴딩 — 뒤진 쪽 강화** | **Mario Kart** — *"The game selects an item based on the player's current position in the race, utilising a mechanism known as **rubber banding**"*; 뒤진 플레이어는 Bullet Bill 같은 강력 아이템, 선두는 방어용 소형 아이템만. 명시 목적: *"allows other racers a realistic chance to catch up to the leading racer"* | 열세 측의 출력을 동적으로 올린다 | 결과를 마지막까지 미결로 유지 | 실력 신호를 직접 훼손. 그리고 **castle-war는 vs-AI라 AI를 강화하는 것이 곧 플레이어 처벌** | ⚠️ **측정 실패 — 보고하지 않는다.** 본 구현이 등속 지점을 54.1%/38.3%로 왜곡했고, 이는 발사 순서 처리의 비대칭(구현 결함)이다. **장치의 성질이 아니라 내 코드의 결함**이므로 수치를 근거로 쓰지 않는다 |
| **G10. 핸디캡 — 계측 후 보정** | **골프 WHS** — 최근 **20라운드 중 최고 8개** 차이값 평균, soft cap(3.0 초과 상승을 50%로), hard cap(상승 5.0 제한), Slope Rating **55~155**(표준 113). **USGA** — 최고 10/20 × **0.96**. **볼링** — 스크래치 181 vs 183인데 핸디캡 58 vs 53으로 **239 vs 236 역전**. **육상·수영 pursuit** — 느린 선수가 먼저 출발, *"An ideal handicap race is one in which all participants finish at the same time"*. **모터스포츠** — 드라이버 등급별 **피트 정지 시간** 차등(International GT Open) | 플레이어의 과거 성적을 계측해 다음 경기의 출력을 조정 | **실력 격차를 승률 격차로 번역하지 않는 유일한 정공법.** 표본이 광범위(go·shogi·chess·croquet·golf·bowling·polo·basketball·육상) | 계측 기간이 필요하다. vs-AI 싱글에서는 "게임이 나를 봐주고 있다"가 노출되면 성취감이 붕괴 | **미측정.** castle-war의 `CurrentAiErrorOffset`(`aiErrorStart→aiErrorEnd`)이 **이미 동적 난이도 손잡이**이고 턴 수에 묶여 있다 — 이것을 *플레이어 성적*에 묶으면 그대로 핸디캡이다 `[INFERENCE]` |
| **G11. 부분 성공 등급화** | **Angry Birds** — 별 1~3개 등급으로 클리어. **골프 Stableford** — 홀별 점수를 포인트로 환산. **Elam Ending** 계열은 반대로 목표 점수 고정 | 승/패 이진이 아니라 **정도**로 결과를 보고 | 절벽 자체를 없애지 않고 **절벽의 체감**을 없앤다 — 져도 등급이 오름 | 승패 자체는 그대로 절벽. 수익 게이트(승률 격차)는 해결되지 않음 | **castle-war 이미 보유** — `SiegeRank.ComputeGrade(victory, turns, score)`, `SIEGE SCORE` 표시. 절벽 완화가 아니라 **완충재**로 이미 작동 중 |

### 2. 측정 종합 — 무엇이 실제로 듣는가

| 장치 | +0.01 승률 | 게이트 | 배수 | 경기 길이 | 판정 |
|---|---|---|---|---|---|
| — 현재 (shipped) | 69.1% | 0.250%p | 1.0× | 40.9턴 / 307초 | 기준선 |
| **G2 명중 판정(binary)** | **52.3%** | **1.650%p** | **6.6×** | 41.7턴 / 313초 ✅ | **최강. 단 연출 충돌** |
| ~~G1 분산 ×4~~ | 54.9% | 0.950%p | 3.8× | 41.1턴 / 308초 | ⚠️ **조건부 — 심 상수, 게임 손잡이 아님** |
| G5 γ=0.5 | 59.7% | 0.525%p | 2.1× | 34.2턴 / 256초 ⚠️ | 효율 좋음, 길이 하단 접근 |
| ~~G1 분산 ×2~~ | 60.5% | 0.450%p | 1.8× | 40.9턴 / 307초 | ⚠️ **조건부 — 심 상수, 게임 손잡이 아님** |
| G3 cap 0.75 | 66.1% | 0.300%p | 1.2× | — | 거의 무효 |
| G4 floor 0.55 | 69.1% | 0.250%p | **1.0×** | — | **완전 무효(작동구간 밖)** |
| **G8 3발/턴** | **74.4%** | **0.200%p** | **0.8×** | — | ❌ **역효과** |
| **C1 라운드 완주** | **73.7%** | 0.250%p | 1.0× | — | ❌ **역효과** |
| G3 cap 0.60 / G5 γ=0.25 | 50.0% / 51.1% | — | — | — | ❌ **실력 소멸(과잉)** |

> **설계 방정식** (게이트를 지키는 데 필요한 분산):
> `sd(발수) ≥ d(발수)/(√2 × z₀.₅₅)`, `z₀.₅₅ = 0.1257`
> - 조준 +0.005에서 55% 유지 → sd ≥ **0.78발** (현재 0.50의 **1.6배**)
> - 조준 +0.010에서 55% 유지 → sd ≥ **1.56발** (현재의 **3.1배**)
> - 조준 +0.020에서 55% 유지 → sd ≥ **3.12발** (현재의 **6.2배**)
>
> **즉 목표가 "1%p 조준 우위에서 승률 55% 이하"라면 발수 분산을 3배 이상 키워야 한다.**
> G1 ×4(sd 1.32)로도 부족하고, **G2 명중 판정(sd 2.87)만 단독으로 이 조건을 넘긴다.**
> G1과 G5를 겹치면 게이트 0.600%p로, 단독 G1 ×4(0.950%p)보다 **나쁘다** — 겹치기가 가산되지 않는다.

### 3. 부정 발견 — 반드시 기록해야 하는 세 가지

**① C1(라운드 완주)은 절벽을 악화시킨다: 69.1% → 73.7%.**
본문 카탈로그가 C1을 선공 보정 최우선 후보로 올렸는데, 실력 축에서는 **반대 방향**이다.
이유: 지금은 **선공 타이브레이크가 실력 신호를 가려주고 있다.** 두 쪽이 같은 발수를 필요로 할 때
선공이 이기는데, 이 "무작위에 가까운" 승부가 절벽을 눌러 준다. 라운드 완주는 그 동점을 무승부로
빼내므로 **결정된 경기만 남고, 결정된 경기에서는 실력이 더 선명하게 드러난다.**
**선공 문제가 이미 닫힌 상태에서 C1을 넣을 이유는 없고, 넣지 말아야 할 이유는 측정됐다.**

**② 다중 발사(G8)는 직관과 반대로 절벽을 세운다: 69.1% → 74.4%.**
"한 발의 불운을 여러 발로 나눈다"는 발상은 **분산을 줄이는** 조치다. 큰 수의 법칙이
평균을 안정시키므로 지속적 base 격차가 **더 확실하게** 승패로 번역된다.
**Worms의 팀 편성이나 MIRV를 "실력 완화 장치"로 인용하는 것은 틀렸다** —
그것은 전술 다양성 장치이고, 순수 조준 실력의 비중을 낮추는 것은 무기 종류의 **선택 폭**이지
발사 횟수가 아니다. (무기 다양성의 효과는 Lane C 소관.)

**③ 작동 구간 밖의 장치는 장치가 아니다.** 피해 하한 0.55는 조준 작동 구간(0.61~0.79) 아래라
게이트를 **0.000%p** 움직였다. 그리고 상한을 0.60까지, γ를 0.25까지 밀면 **조준이 승률에 전혀
영향을 주지 않는다**(50.0%, 51.1%). **완화와 소멸 사이의 창이 좁다** — 이것이 이 축의 진짜 난점이다.

### 4. castle-war 적용 판정 — 이 레인의 권고

> ⚠️ **2026-08-14 정정 2건.** ① **G1 철회** — Main이 물었다: *"`beginnerAimError`가 게임 쪽 대응 상수를
> 갖고 있습니까?"* **없다, 심 전용이다.** ② **G2 보류** — `qa/b1-measurement-findings.md`가 실측한
> **0피해 17~43%**는 실물이 **이산 명중 판정을 사실상 이미 보유**하고 있음을 뜻한다(이 절 상단 🛑 정정 참조).

| 순위 | 후보 | 근거 | 비용 |
|---|---|---|---|
| **1** | **G10: 계측 후 핸디캡** — `SimpleAI.errorOffsetRange`에 플레이어 등급별 가산 (Main의 `SkillGrading.cs`) | 게임에 실재하는 손잡이다. 골프·볼링·모터스포츠·바둑에 광범위한 선례. 상한이 있어 폭주하지 않는다. **근거가 바뀌었다** — 절벽을 눕히려는 것이 아니라 **실력 격차 자체를 보정**하는 장치로서 1순위다 | 중간. **계측 선행 필수** |
| **최우선 선행** | **CV 대조** — Main의 샷당피해 계측 vs `qa/b1-measurement-findings.md`의 실측 CV 0.70~1.50 | 계측의 역할이 *"미지를 재는 것"* → **"두 값을 대조하는 것"** 으로 바뀌었다. 0.7~1.5 재확인 → **절벽 없음·개입 불필요.** 0.1대 → b1이 틀렸고 절벽 실재 | 이미 구현 중 |
| **보류** | ~~G2: 명중 판정 도입~~ | 심 모델에서는 유일하게 방정식을 단독 만족했다(게이트 6.6배). **그러나 실물은 0피해 17~43%로 이미 이 성질을 갖고 있다** — 추가는 중복이며 **과잉 구간**(cap 0.60·γ=0.25에서 조준이 승률에 전혀 영향 없음)으로 넘어갈 위험 | — |
| 3 | G6: 바람(`CurrentWindCap`) — 게임에 실재하는 유일한 **대칭** 분산원 | 이미 존재하고 난이도 곡선에 묶여 있으며 서사적 정당화가 됨 | **미측정** — 심에 바람이 없어 PlayMode 실측 필요 |
| ❌ **철회** | ~~G1: `beginnerAimError` 0.09→0.18~~ | **심 전용 상수.** 바꾸면 심 출력만 움직이고 게임은 그대로 | — |
| — | G5 γ | 효율은 좋으나 경기 길이 256초로 하단(240) 접근. 그리고 실측 CV에서는 애초에 불필요 | 길이 재튜닝 필요 |
| ❌ | C1, G8, G4 | 각각 악화(73.7%) / 악화(74.4%) / 무효(0.000%p) — **측정됨.** 이 판정은 실측 CV에서도 유효하다 — 분산을 줄이는 성질은 sd 크기와 무관하고, 실측 sd가 크면 깎는 손실이 더 크다 | — |

**⑤ 치석의 정밀도 스펙 — 방정식이 내놓는 요건.**
치석은 sd를 키우지 않고 **Δ를 줄인다.** 같은 방정식의 다른 항이다:
`승률 = Φ((Δ_skill − Δ_handicap) / (sd·√2))`.
곡선의 기울기가 그대로 남으므로 **잔차에 그 기울기가 그대로 적용된다**:

| 목표 | 허용 잔차 Δ_aim |
|---|---|
| 45~55% 밴드 유지 | **≤ 0.32%p** (조준 0.70 기준 **0.46%**) |
| 60% 이하 | ≤ 0.65%p |
| 현재 체감(67%) | ≤ 1.12%p |

> **→ 치석 한 등급의 폭이 0.32%p보다 크면 반드시 오버슈트 또는 언더슈트한다.** 등급 경계를 이 값으로 잡아라.
>
> ⚠️ **단 `errorOffsetRange` +0.35를 이 방정식에 직접 넣을 수는 없다.**
> `SimpleAI.cs:53`의 `errorOffsetRange`는 `Random.Range(-r,r)` 두 축의 **월드 좌표 오프셋(미터)**이고,
> `fixedAimQuality`는 **0~1 피해 배율**이다. **코드에 둘 사이 변환이 없다.**
> 미터→피해배율 환산은 성벽 히트박스·폭발 반경·블록 배치에 달려 있고 그것은 심에 없는 물리다.
> **환산 계수를 지어내면 이 방정식이 거짓말을 시작한다.** 명중률 텔레메트리가 실측으로 채워야 하는 칸이다.

**⑥ sd를 실측하려면 무엇을 재야 하는가 — Main의 질문에 대한 답.**

**발수를 직접 재지 마라. 샷당 피해를 재라.** 이유는 셋이다.

누적 피해가 `n·μ ± √n·σ`이므로 성 하나를 깨는 데 걸리는 발수의 분산은 닫힌 형태로 나온다:

```
N        = 내구도 / 평균샷피해          (= 1440 / μ)
sd(N)    ≈ √N × CV,   CV = sd(샷피해) / 평균(샷피해)
```

검증 (본 모델, 40,000경기):

| 가정 조준오차 | CV(합성) | 공식 예측 sd(N) | 실측 sd(N) |
|---|---|---|---|
| 0.09 (현재 가정) | 0.0847 | 0.373 | **0.50** ← 이산화 바닥 |
| 0.18 | 0.1540 | 0.678 | 0.71 |
| 0.36 | 0.2997 | 1.320 | 1.32 |

**① 명중률만으로는 sd가 나오지 않는다.** 명중률은 이 분포의 *"0이냐 아니냐"* 이진화이고
**크기 정보를 버린다.** `TelemetrySink.NoteShotOutcome`(상대 성에 맞았는가)은 `CV`의 분자를 못 준다.
필요한 것은 **그 샷이 실제로 넣은 피해량**이다 — `DestructibleBlock.TakeDamage` / `UnitController.TakeDamage`에
이미 흐르는 값이며, 샷 단위로 합산해 기록하면 된다.

**② 발수를 재면 검열(censoring)된 표본이 된다.** 경기는 먼저 깬 쪽에서 끝나므로
**패자의 발수는 관측되지 않는다.** 관측되는 것은 두 값의 최소값이고, 최소값의 sd는 원 변수의 sd가 아니다.
샷당 피해는 **모든 샷이 표본**이라 검열이 없다.

**③ 표본 효율이 약 20배다.** 경기당 샷당피해 표본이 약 19개, 발수 표본은 1개(그것도 승자만).

> **그리고 이 계측이 절벽을 가장 선명하게 드러낼 것이다.** 현재 가정에서 발수 분포는
> **19발 0.2% / 20발 55.0% / 21발 44.7% / 22발 0.0%** — 서로 다른 값이 사실상 **둘**뿐이다.
> **이 게임은 "20발이냐 21발이냐" 하나로 갈린다.** 그래서 조준 1%p(=0.28발)가 승률 14%p를 움직인다.
> sd 0.50은 연속 분산 0.373발이 **정수로 이산화된** 결과이며, 이 이산화가 sd의 바닥을 만든다 —
> 즉 **분산을 조금 키우는 것은 효과가 없고**(이산화 바닥에 묻힌다) 유의미하게 키워야 곡선이 눕는다.
> 실측 CV가 나오면 `sd(N) = √(1440/μ) × CV`에 넣어 **가정 없이** 실제 절벽 기울기를 계산할 수 있다.

> **한 줄 판정 (정정 후).** 절벽의 원인은 조준이 강력해서가 아니라 **발수 분산이 sd 0.50발밖에 없어서**다.
> 원인 진단은 유지되지만 **처방은 뒤집혔다** — 분산을 키우는 손잡이가 게임에 없다.
> 게임에 있는 것은 **바람과 AI 오차**뿐이고, 후자를 플레이어 성적에 묶는 것이 곧 핸디캡이다.
> **그리고 분산 증가는 자기 절벽을 갖는다** — cap 0.60·γ=0.25에서 조준이 승률에 **전혀** 영향을 주지
> 않게 된다(50.0%/51.1%). 완화와 소멸 사이 창이 좁다. 핸디캡은 상한이 있어 그 방향으로 폭주하지 않는다.
> **그러므로 계측 → 잔차에 핸디캡, 이 순서가 맞다.**
>
> **마지막 경고 — 그리고 이 경고가 실제로 실현됐다: sd 0.50발은 실측이 아니라
> `beginnerAimError = 0.09`라는 가정의 결과다.** 종료 직전 `qa/b1-measurement-findings.md`를 읽어
> **실측 CV 0.70~1.50**(0피해 17~43%)을 발견했다. 그것을 넣으면 sd(발수)가 **2.34~5.79**로
> 본 절 가정의 **5~12배**이며 조준 +0.01 예측 승률이 **51~53%** 가 된다. 이 절 상단 🛑 정정이 전문이다.
> **즉 본 절의 절벽 수치는 심 모델의 성질이고, 실물 게임의 성질이 아닐 가능성이 높다.**
> 이 문서를 이어받는 사람은 **§0~§2의 수치를 실측 CV로 먼저 재계산**해야 한다 —
> 도구(`sd(N) = √N × CV`, `승률 = Φ(Δ/(sd·√2))`)는 그대로 쓰고 **CV만 갈아 끼우면 된다.**
>
> **그럼에도 N3와의 대칭은 남는다** — 총 피해 2.6%의 개입이 선공 승률을 크게 움직였다는 사실은
> 이 게임이 **작은 지속적 우위에 민감하다**는 뜻이고, **그 민감도의 크기가 CV에 달려 있다**는 것이
> 이 절이 남기는 실질 결론이다.

### 5. 이 절에서 추가된 출처

| # | 출처 | 강도 | 무엇을 확인해 주는가 |
|---|---|---|---|
| 25 | `https://en.wikipedia.org/wiki/XCOM_2` | `direct page retrieval` | G2 — UI가 명중 확률 % 표시, *"At a high percentage of chance, they can still miss their shots while at a low percentage, players may be able to land some hits"*, 팀이 *"the idea of unpredictability and randomness"* 를 의도적으로 중시. 난이도 4단(Rookie/Veteran/Commander/Legend). **추가로: Solomon이 실제 계산을 UI 표시와 다르게 두어 *"match the player's psychological feeling about that number"* 하게 만들었다고 진술** — 표시와 실제를 의도적으로 어긋나게 한 1차 진술 |
| 26 | `https://en.wikipedia.org/wiki/Handicap_(golf)` | `direct page retrieval` | G3·G5·G10 — WHS 최고 **8/20**, soft cap(>3.0 상승을 50%로) / hard cap(상승 5.0), Slope **55~155**(표준 113), Equitable Stroke Control(**1974**), net double bogey. **USGA 최고 10/20 × 0.96 — 원래 85%였고 "changed to 96% after being seen to favor better players too heavily"**. 핸디캡은 평균이 아니라 *"potential or average best"* |
| 27 | `https://en.wikipedia.org/wiki/Handicapping` (wikitext, `action=raw`) | `direct page retrieval` | G10 — 핸디캡 채택 종목 열거(go·shogi·chess·croquet·golf·bowling·polo·basketball·track and field). **볼링 수치: 스크래치 181 vs 183, 핸디캡 58 vs 53 → 총점 239 vs 236 역전.** pursuit 방식 *"The slowest swimmer, or cyclist, for example, starts first and the fastest starts last"*, *"An ideal handicap race is one in which all participants finish at the same time"*. 모터스포츠 **드라이버 등급별 피트 정지 시간 차등**(International GT Open) |
| 28 | `https://en.wikipedia.org/wiki/Mario_Kart` | `direct page retrieval` | G9 — *"The game selects an item based on the player's current position in the race, utilising a mechanism known as rubber banding"*, 뒤진 쪽 Bullet Bill / 선두는 소형 방어 아이템, 목적 *"allows other racers a realistic chance to catch up to the leading racer"* |
| 29 | `Assets/Scripts/MatchLengthModel.cs:47-51, 162-164, 203-218, 310-340` | `direct page retrieval — 코드` | 모델 재현의 근거값 전부 — `Default`(12/90/360/106/7.5/**0.70**/**0.09**), `KeepDurability = wallBlockCount*wallBlockHp + coreHp`, 피해식 `baseShotDamage * projectileMultiplier * openingVolleyMultiplier * aimQuality`, `EffectiveDamagePerTurn = 37f`(120→106 리튜닝 이력 주석) |
| 30 | `Assets/Scripts/SiegeDuelSimulation.cs:87-98, 104-154, 202-217, 235-238` | `direct page retrieval — 코드` | 재현 대상 원본. `G2LowerBound/UpperBound = 0.45/0.55`, **`RequiredMatches = 100`과 "100 halves that to about ±5%p" 주석**(Main 측정치의 오차 근거), `WinRateWithSkillDelta`, `NextSignedUnit()`이 균등 [-1,1], 그리고 **주석이 이미 절벽을 예견**: *"small skill edge produces a landslide is one where the comeback mechanics are doing nothing"* |
| 31 | 본 레인의 파이썬 재현 (25,000경기/조건, `numpy`) | `독립 재구현 — 코드 이식` | §0~3의 모든 측정 수치. **1차 출처가 아니라 계산이다.** 재현 대상(`SiegeDuelSimulation`)의 한계를 그대로 상속 — 물리·블록 배치·붕괴 연쇄·지형·바람 **없음**. 따라서 이 수치는 **밸런스 모델의 성질**이고 실물 게임의 성질이 아니다 |
| 32 | `_workspace/current/qa/b1-measurement-findings.md:9-28, 62-66, 87-89, 102-106` | `direct page retrieval — 저장소 실측` | **이 절의 절벽 수치를 무너뜨린 출처.** 샷당 피해 **실측 CV: Stage1 1.50 / Stage2 0.70 / Stage3(재측정) 1.39**, 0피해 턴 **6/22(27%) / 1/6(17%) / 43%**, Stage1 평균 96.59·최대 **671**. 살아남는 발견으로 *"스테이지 내부의 두꺼운 꼬리(재측정 Stage3도 CV 1.39, 0피해 43%)"* 를 명시. 추가로 **자기 파괴가 피해의 39~42%**(Stage3은 67%)이며 *"G2 87%의 일부는 적이 스스로 무너지는 것으로 설명된다"*. ⚠️ 본 문서도 **스크립트 플레이어(45°·당김 86%, 학습 없음)** 측정이고 표본이 22발·6발로 작다. **triage가 이름을 지정했는데 본 레인이 착수 시점에 읽지 않았다 — 누락이다** |

### 6. 이 절이 남기는 공백

1. **G6(바람)의 실측.** 바람은 `SiegeDuelSimulation`에 존재하지 않아 본 모델로 측정 불가.
   바람이 발수 분산에 얼마를 기여하는지는 **PlayMode 실측만이 답할 수 있다.**
   ⚠️ **정정:** 앞서 "분산 손잡이가 두 개(G1, G6)"라고 썼으나 **G1은 심 전용이므로 손잡이가 아니다.**
   게임에 실재하는 대칭 분산원은 **바람 하나뿐**이며(AI 오차는 비대칭 — AI에만 걸린다),
   따라서 §4-⑥의 샷당피해 계측이 G6의 실제 기여도를 재는 유일한 경로다.
2. **G9(러버밴딩) 재측정.** 본 구현이 발사 순서 비대칭으로 등속 지점을 왜곡했다. 수치를 폐기했다.
   **castle-war의 LAST STAND(`LastStand.Phase`, `RefreshLastStandButton`)가 이 계열의 기존 장치**이며,
   그 평가는 **Lane D 소관**이다 — 본 레인은 코드 앵커만 넘긴다.
3. **G2의 연출 정합성.** 명중 판정이 유일하게 방정식을 만족하지만, "맞았는데 0 피해"를
   물리 붕괴 연출과 어떻게 화해시킬지는 설계 문제이며 본 조사 범위 밖이다.
   포병 계보 표본 4종(Worms·Gunbound·Scorched Earth·ShellShock)에서 **이산 명중 판정은 0건**이다 —
   전부 연속 피해다. **G2는 계보를 벗어나는 선택이다.**
4. **Fire Emblem 2RN(true hit)** — 표시 명중률과 실제 명중률을 두 개의 난수 평균으로 어긋나게 하는
   장치로, G2의 변형이자 XCOM 2 진술과 같은 계열이다. `fireemblem.fandom.com/wiki/True_Hit` **404**로
   회수 실패했다. **표에 넣지 않았다.** 1차 출처를 얻으면 G2의 "표시 vs 실제" 설계 선택지가 넓어진다.
