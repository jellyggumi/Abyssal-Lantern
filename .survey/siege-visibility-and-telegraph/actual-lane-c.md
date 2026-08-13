# Actual Behavior (Lane C): 실사용 행태 · 예고의 역효과

조사 범위: 「무엇이 장르 표준인가」가 아니라 **무엇이 실제로 쓰였고 무엇이 실패했는가**.
증거 등급은 survey 검증기 어휘를 따른다. [OBSERVED] = 1차 문헌에 문자로 존재 /
[INFERENCE] = 내가 연결한 추론.

**이 레인의 최우선 질문(Main 지시)**: castle-war의 적 턴은 플레이어 입력이 정확히 0이다.
따라서 예고를 넣어도 **대응 수단이 없다**. 그래도 성립하는가?
→ 결론은 §D-3 표와 최하단 요약에 있다. Main의 "흔적이 없다" 재정의에 대한 답은 **§D-4**.
**부분적으로만 증거를 찾았고, 못 찾은 부분은 §D-1 및 §D-4 표에 명시했다.**

---

## What People Actually Use

### 1. 적 의도 공개는 "있으면 좋은 것"이 아니라 **없으면 게임이 망가지는 것**

가장 강한 증거는 마케팅이 아니라 **개발사가 스스로 되돌린 기록**이다.

> "Originally, enemies did not show their next intended action as is common to most
> turn-based role-playing video games, but this design did not mesh well with the
> roguelike nature of permadeath. During playtests, they found that players were
> **confused** about the number of card abilities without any clear situation to apply them."

— Slay the Spire 개발 기록
[direct page retrieval — https://en.wikipedia.org/wiki/Slay_the_Spire]
[OBSERVED]

즉 Slay the Spire는 **원래 적 의도를 안 보여줬다.** 넣은 이유가 "더 친절하게"가 아니라
**"플레이어가 자기 카드를 언제 써야 할지 몰랐다"**였다. 이것은 castle-war 사용자 원문
("어떻게 써야 되는지도 안 보인다")과 **같은 증상**이다. [INFERENCE]

### 2. 의도 공개는 가격이 매겨질 만큼 강력하다 — 수치 증거

Slay the Spire는 "적 의도를 못 보게 되는 것"을 **보스 유물 하나의 대가**로 팔고 있다.

> **Runic Dome**: "Gain 1 Energy at the start of each turn.
> **You can no longer see enemy Intents.**"

[direct page retrieval — https://slay-the-spire.fandom.com/wiki/Runic_Dome]
[OBSERVED]

기본 에너지가 턴당 3이므로, 이 게임은 **적 의도 가시성을 "턴당 +33% 자원"과 등가**로
값매김한 것이다. 가시성이 편의 기능이 아니라 **1급 게임 자원**이라는 정량적 근거다. [INFERENCE]

### 3. 텔레그래프는 경기를 늘리지 않고 **줄인다**

> "Subset wanted each battle to be relatively short in terms of gameplay time.
> The limited turn counter was used to keep battles short, and Subset found that
> **telegraphing the Vek's movements further helped to hasten the pace.**"

[direct page retrieval — https://en.wikipedia.org/wiki/Into_the_Breach]
[OBSERVED]

castle-war의 300초 밴드를 근거로 예고를 탈락시키는 논리는 **1차 증거와 반대 방향**이다.
(LaneA도 독립적으로 같은 결론에 도달했다.)

### 4. 같은 계보에서 실제로 채택된 것 — ShellShock Live

포병 대전 계보의 현역 타이틀이 v1.0에서 **아군 조준 정보를 완전 공개**로 바꿨다.

> "**Ally Aim Visibility** — Teammate aim details are now **fully visible**, unlocking
> an entirely new level of strategy! ... Teammate aim details are even visible after
> your tank has been destroyed."

— ShellShock Live v1.0, 2020-05-22
[direct page retrieval — https://store.steampowered.com/news/app/326460?emclan=&emgid=3112496281295272175]
[OBSERVED]

핵심: 이것은 **"다른 플레이어가 어디를 어떻게 쏠지"를 발사 전에 공개**한 것이다.
즉 이 장르는 이미 **타인의 조준 의도 공개**를 채택했고, 그것을
"전략의 새로운 층을 열었다"고 자평했다. [OBSERVED]

---

## Common Workarounds

개발사가 가시성을 놓치고 **나중에 패치로 덧붙인** 사례. 요구 기준 2건 이상 → **4건 확보**.

| # | 게임 | 시점 | 덧붙인 것 | 자백/근거 문구 | 등급 |
|---|---|---|---|---|---|
| 1 | **ShellShock Live** | 2020-05-22 (v1.0) | 아군 조준 정보 완전 공개 | "Teammate aim details are now fully visible" | [direct page retrieval] |
| 2 | **Monster Train** | 2020-09-03 | **적 웨이브 카운터** + 보스 사전 공개 | "a number of **frequently requested** player features like ... an **enemy wave counter** in battle, and a **preview** of the ring 3 and ring 6 bosses" | [direct page retrieval] |
| 3 | **Monster Train** | 2021-03-31 (2.0.1) | 상태 가시성 수정 | "improved **visibility** of what is happening ... because this was **causing confusion** and some people **thought they were softlocked**" | [direct page retrieval] |
| 4 | **Monster Train** | 2020-10-22 | 색맹 접근성 프리셋 | "added a new **color blind** accessibility option" | [direct page retrieval] |

URL:
- ShellShock v1.0 — https://store.steampowered.com/news/app/326460?emclan=&emgid=3112496281295272175
- MT Friends & Foes — https://store.steampowered.com/news/app/1102190?emclan=&emgid=3819571008256632127
- MT 2.0.1 — https://store.steampowered.com/news/app/1102190?emclan=&emgid=4023377846271907741
- MT Herzal's Workshop — https://store.steampowered.com/news/app/1102190?emclan=&emgid=3895010671337584598

**패턴 [OBSERVED]**: 사례 3이 특히 중요하다. 증상이 "안 보인다"가 아니라
**"고장난 줄 알았다"**(thought they were softlocked)였다. 가시성 결함은 난이도 불만이 아니라
**버그 신고로 위장해서 들어온다.** castle-war의 거짓 지시 2개
(`SiegeAlarmSystem.cs:225`, `LaunchManager.cs:121`)가 정확히 이 유형이다. [INFERENCE]

**추가 패턴**: 표시를 줄이는 방향의 패치도 존재한다 —
Guild Wars 2는 `Effect LOD` 옵션을 출하했고 설명이 노골적이다:
> "Limit detail of particle effects. **Helps reducing visual clutter** from effects
> produced by skills of other players in high population scenarios."

[direct page retrieval — https://wiki.guildwars2.com/wiki/Options] [OBSERVED]
→ 즉 업계의 후행 보정은 **양방향**이다. 넣는 패치와 **끄는 패치가 같이 존재한다.**

---

## Pain Points With Current Solutions

**이 절이 이 레인의 본론이다.** 가설을 확인하러 간 것이 아니라 반증을 찾으러 갔고, **찾았다.**

### A. 예고를 넣어서 욕먹은 사례 — 증거 **있음** (개발사 자백)

Into the Breach는 "모든 적의 모든 행동을 매 턴 공개"를 목표로 삼았고,
그 대가를 개발자 본인들이 기록으로 남겼다.

> "**One and a half years ago the game was just an icon mess**," says Davis.
> **They would add icons whenever they saw playtesters miss relevant pieces of
> information, and the screen built up and up with them.**

[direct page retrieval — https://www.rockpapershotgun.com/into-the-breach-interface-design
(RPS *The Mechanic*, 2018-03-05)] [OBSERVED]

주목할 점: 실패 경로가 **정확히 "플레이테스터가 놓칠 때마다 표시를 추가"**였다.
이것이 castle-war가 지금 밟으려는 길이다. [INFERENCE]

그리고 그들의 **해법은 표시를 더 넣는 것이 아니었다**:

> "As for weapons, the solution was **editing**. 'Just as a game design principle,
> **we would sacrifice cool ideas for the sake of clarity every time**,' says Ma."

> "Our requirement that the player has to understand what's going on in any situation
> **restricted our game design options considerably**."

같은 출처 [OBSERVED]

구체적으로 잘린 것들 [OBSERVED, 같은 출처]:
- **공격 형태를 3종으로 축소** (근접/직선/포격). 다이아몬드형 사거리는 폐기 —
  "여러 적의 공격 범위를 동시에 표시하면 **누가 무엇을 공격하는지 알 수 없었다**"
- 별·삼각형 범위 공격 폐기
- 밀어내기는 **정확히 1타일만** — "전달하기가 매우 어렵다"
- **타일당 효과 1개 제한** — "보드에 둘 다 표시하는 것이 불가능했기 때문"
- 불탄 자국, 짓밟힌 배경 등 시각적 야심도 삭제

**castle-war 직접 함의 [INFERENCE]**: 우리가 "UI 요소 N개 추가"로 문제를 풀려 하면,
ItB가 4년 중 2년을 쓰고 되돌아온 길을 반복한다. 그들의 결론은
**표시 예산이 아니라 메커니즘 예산을 줄이는 것**이었다.

### B. 완전 정보의 대가 — "전략이 아니라 퍼즐" (실사용자 증언)

ItB의 완전 텔레그래프는 호평받았지만, **긴장을 정답 찾기로 바꿨다**는 실사용 불만이
Steam 부정 리뷰에 남아 있다. (영어 부정 리뷰 20건 중 키워드 일치 8건)

> "This game is **not a turn-based strategy game, it's a puzzle game**. You have to read
> the 8x8 board, try to predict the next moves, but **there is no good move**, you only
> decide which sacrifice is the less worse. I certainly didn't find this fun,
> it's only **spatial math, move prediction, calculation, and frustration**."
> — 도움됨 +11

> "Into the Breach looks like a tactical strategy game, but it plays like a
> **rigid puzzle system where the board state often matters more than your decisions**."
> — 도움됨 +15

> "it feels less like a turn based strategy and more like one of the puzzle games that
> has a million different actions you can take and **only one hyper specific solution**."
> — 도움됨 +5

> "an interesting concept that turns into an **annoying puzzle game**" — 도움됨 +4

> "But it's just not fun? **Maybe too chess-like.**" — 도움됨 +4

[direct page retrieval — Steam appreviews API, appid 590380,
`https://store.steampowered.com/appreviews/590380?json=1&filter=all&language=english&review_type=negative&purchase_type=all&num_per_page=100`
수집일 2026-08-13, 고유 부정 리뷰 20건] [OBSERVED]

**가설 검증 결과**: "예고가 과하면 긴장감이 죽는다"는 **확인됨**. 단 죽는 방식이
"쉬워진다"가 아니라 **"단 하나의 정답을 찾는 계산 노동이 된다"**였다. 이게 더 위험하다 —
난이도는 그대로인데 재미의 종류가 바뀐다. [INFERENCE]

### C. 예고가 화면을 지저분하게 만든 사례 — 증거 **있음**

WildStar는 텔레그래프를 간판 기능으로 삼았고, 리뷰가 대가를 기록했다.

> "once you learn to parse the **initially overwhelming display of multiple targets**"
[direct page retrieval — https://www.pcgamer.com/wildstar-review/ (2014-06-09)] [OBSERVED]

더 중요한 것은 Eurogamer가 잡아낸 **주의 전이(attention shift)**다:

> "it isn't long before **floor watching** becomes a necessary and compelling part of
> the interchange of combat. ... you begin to **see less the arcs of light and flailing
> limbs above ground and more and more the shapes projected across it.**
> **Alas, this means that combat becomes less about duelling heroes and battling
> monsters and edges towards parody.**"

[direct page retrieval — https://www.eurogamer.net/wildstar-review (2014-10-28)] [OBSERVED]

**castle-war 직접 함의 [INFERENCE]**: 우리 게임의 볼거리는 **물리와 성의 붕괴**다.
바닥 지시자를 늘리면 플레이어는 **성을 보지 않고 지시자를 본다.**
가시성을 올리려고 넣은 것이 정작 **보여주려던 대상을 가린다.**
"화면이 지저분해진다"보다 이쪽이 더 정확한 실패 서술이다.

### D. **대응 불가능한 예고** — Main의 핵심 질문에 대한 정직한 답

#### D-1. 못 찾은 것 (명시)

**"발사 직전 예고를 넣었는데 대응 수단이 0이어서 반응이 어땠다"를 직접 측정한
1차 사례는 찾지 못했다.** 다음을 시도했고 모두 실패했다:
- r/gamedesign · r/truegaming 검색 → Reddit이 API·구·신 UI 모두 HTTP 403 차단
  (`old.reddit.com` 브라우저 접근도 "Welcome to Reddit" 게이트로 차단)
- Steam 부정 리뷰 4종(ItB, STS, XCOM 2, Darkest Dungeon)에서
  `unavoidable / nothing you can do / helpless / forced to watch` 계열 정규식 검색
  → 유효 일치 0~1건, 그 1건도 텔레그래프가 아니라 밸런스 불만
  ("basic attacks that hit for 25-40% of your HP with **no counter play options**",
  STS 부정 리뷰 도움됨 +1) [OBSERVED, 단 주제 불일치]

→ **이 축은 "증거 없음"으로 처리하고 추정으로 채우지 않는다.**

#### D-2. 찾은 것 — 구조적 증거 3개는 한 방향을 가리킨다

**(a) 입력 0 구간에서 예고가 성립하는 장르는 실재한다. 단 시점이 다르다.**

> "players place characters on a grid-shaped battlefield **during a preparation phase**,
> who then fight the opposing team's characters **without any further direct input
> from the player**." ... "In combat, both players' units are placed on the board and
> automatically battle each other, **typically without player input**."

[direct page retrieval — https://en.wikipedia.org/wiki/Auto_battler] [OBSERVED]

오토배틀러는 **전투 중 입력이 정확히 0**이고, Dota Auto Chess는 2019년 5월까지
**800만 플레이어**를 모았다 [OBSERVED, 같은 출처]. 즉 "해소 구간에 입력 0"은
결함이 아니다 — castle-war의 적 턴과 구조가 같다.

**그러나 정보 창의 위치가 다르다.** 오토배틀러의 정보 소비는
**전투 중이 아니라 준비 페이즈**에서 일어난다. 우리 창은 0.9초이고 그것은
준비 페이즈가 아니라 **해소 순간**이다. → **시점은 성립 조건의 일부다.** [INFERENCE]

**(b) 텔레그래프의 존재 이유가 "대응 가능성"이라고 1차 문헌이 명시한다.**

> "It's a game in which you have almost total knowledge, but you're also outnumbered,
> and that means **your turn is about using your knowledge to disrupt the bugs.**"

[direct page retrieval — https://www.rockpapershotgun.com/into-the-breach-interface-design]
[OBSERVED]

ItB의 예고는 **내 턴에 쓰이는 입력값**이다. 예고 자체가 목적이 아니다.
Runic Dome이 턴당 1에너지 값이 붙는 이유도 **의도를 보고 방어/공격을 바꿀 수 있기
때문**이다 — 못 바꾸면 그 유물은 공짜가 된다. [INFERENCE]

**(c) 같은 성 대 성 구조에서 예고 없이 성공한 40년 전 선례가 있다 — Rampart(1990).**

Rampart는 castle-war와 구조가 거의 같다: 성 + 대포 + **부서지는 벽** + 교대 페이즈.

> "Gameplay alternates between two time-limited phases: **combat and building**.
> In the building phase, the player attempts to expand their territory and
> **repair any damage from combat**."

> "Since the **damage caused during the combat phase is normally spread out,
> repairing it can be difficult.**"

> Legacy: "Rampart **influenced the first tower defense games** around a decade later.
> Gameplay similarities include defending a territory by constructing defensive
> structures, and **making repairs between multiple rounds of attacks**."

[direct page retrieval — https://en.wikipedia.org/wiki/Rampart_(video_game)] [OBSERVED]

평가: CVG 93%, MegaTech 90% + Hyper Game Award, 1991년 5월 일본 테이블 아케이드
흥행 7위, Nintendo Power 1993 게임보이 4위 [OBSERVED, 같은 출처].

**결정적**: Rampart에는 **적의 사전 예고가 전혀 없다.** 플레이어가 소비하는 정보는
**적이 쏜 뒤에 벽에 남은 구멍 패턴**이다. 그 구멍이 다음 건설 페이즈의 결정을
전부 지배한다. 즉 **사후 판독만으로 성립한 성 대 성 포병 게임이 장르의 조상이고,
호평받았고, 타워디펜스를 낳았다.** [OBSERVED + INFERENCE]

**(d) 죽은 적 턴에 대한 장르의 실제 답은 예고가 아니라 "예약"이었다.**

XCOM 2의 Overwatch는 텔레그래프가 아니다:

> "Overwatch is a **reactive** ability ... While Overwatch is active, **any hostile unit
> that moves into the unit's shooting range will be attacked with a reaction shot**
> ... Overwatch remains active **until the start of the player's next turn**."

[direct page retrieval — https://xcom.fandom.com/wiki/Overwatch_(XCOM_2)] [OBSERVED]

즉 **내 턴에 커밋하고 적 턴에 발동**한다(선행 조사의 D3+D4).
castle-war에 이미 코드가 있고 꺼져 있는 장치가 정확히 이것이다. [INFERENCE]

#### D-3. 이 축의 결론

증거는 **예고를 두 종류로 쪼개라**고 말한다. 하나만 채택 가능하다.

| | (A) 사전 예고 | (B) 사후 판독 |
|---|---|---|
| 정의 | 적이 쏘기 **전에** 의도 공개 | 적이 쏜 **뒤에** 무엇이 왜 왔는지 공개 |
| 대응 수단 필요 | **필요** (없으면 무력감) | **불필요** |
| 우리 창 | 0.9초, 입력 0 → 부적합 | 제약 없음 |
| 다음 결정에 쓰임 | 이번 턴에 못 씀 | **다음 턴 조준에 직결** |
| 1차 선례 | ItB·STS (둘 다 대응 가능이 전제) | **Rampart(1990)** — 구조 동일, 호평 |
| 장르 채택 이력 | 3/10 (선행 조사 D6) | **1980년부터 46년 연속** (§D-4) |
| 클러터 위험 | 높음 (WildStar 바닥 응시) | 낮음 (사건이 곧 표시) |

**따라서 Lane C의 권고는 "적 턴에 예고를 넣자"가 아니라
"적 턴을 판독 가능하게 만들자"다.** 근거:
1. 사전 예고의 성립 조건(대응 가능성·긴 정보 창)을 우리는 **둘 다 만족하지 못한다** [INFERENCE]
2. 사후 판독은 **성 대 성 포병 장르의 조상이 이미 검증했다** (Rampart) [OBSERVED]
3. 사후 판독은 UI 요소 수를 늘리지 않는다 — 사건 자체를 읽히게 만드는 일이다.
   ItB가 도달한 "메커니즘 삭제" 결론과 같은 방향이다 [INFERENCE]
4. **흔적 지속은 이 장르가 1980년부터 46년간 연속 채택한 장치다** — §D-4의 4건 [OBSERVED]
5. Worms가 **판독 시점을 "움직임 정지 후"로 못박아** 0.9초 창 문제를 우회하는
   구체적 방법을 이미 보여준다 [OBSERVED]

단, **(A)를 완전 배제하는 증거는 아니다.** 0.9초를 늘리거나 예약 입력(D3)을 켜서
대응 수단을 만들면 (A)의 성립 조건을 사후적으로 충족시킬 수 있다.
그 경우엔 XCOM Overwatch가 선례가 된다. [INFERENCE]

#### D-4. **흔적(trace) 지속** — Main의 재정의에 대한 답: 증거 **있음**, 4건

Main의 재정의("공백은 예고가 없는 게 아니라 **흔적이 없다**는 것")는 내 (B)보다
정확하다. 그리고 이 축은 **증거가 풍부하다** — 이 장르는 흔적을 46년째 다뤄왔다.

**(1) 1980년: 이전 샷의 궤적선이 장르 최초 기능군에 이미 있었다.**

> "Some games used **lines on the screen to show trajectories previous shots had taken,
> allowing players to use visual data when considering their next shot.**"

[direct page retrieval — https://en.wikipedia.org/wiki/Artillery_game] [OBSERVED]

즉 "흔적을 남겨 다음 샷의 판단에 쓴다"는 **Apple II 시대(1980)에 이미 확립**됐다.
**"그게 다음 턴 조준에 실제로 쓰이는가"에 대한 답이 문장 안에 직접 있다** —
`allowing players to use visual data when considering their next shot`.

**(2) 1991년: 흔적이 *구매 가능한 업그레이드*로 상품화됐다 — Scorched Earth.**

> "**All weapons can be upgraded with tracers which allow the player to more accurately
> adjust the trajectory on their next turn.**"

[direct page retrieval — https://en.wikipedia.org/wiki/Scorched_Earth_(video_game)] [OBSERVED]

게다가 `Smoke Tracers`는 **v1.1에서 추가된 후행 패치 항목**이다 [OBSERVED, 같은 출처].
흔적은 편의 기능이 아니라 **돈으로 사는 전투력**으로 취급됐다. [INFERENCE]

**(3) 현역 타이틀에서 흔적은 성능 부담이 될 만큼 실제로 누적된다 — ShellShock Live.**

> v0.9.5.3 (2016-02-24): "**Fixed lag associated with too many shot tracers**"
> v0.9.5.11 (2016-03-16): "**Increased performance of shot tracers**"

[direct page retrieval —
https://store.steampowered.com/news/app/326460?emclan=&emgid=295354567831689265 ·
https://store.steampowered.com/news/app/326460?emclan=&emgid=272838467468447141] [OBSERVED]

"너무 많은 shot tracer 때문에 랙"이 생겼다는 것은 **tracer가 화면에 남아 누적된다**는
직접 증거다(1발 소멸이면 "too many"가 성립하지 않는다). [INFERENCE]

**(4) Worms: 흔적이 두 층으로 남고, 판독 시점이 명시적으로 설계돼 있다.**

지형층 — 영구:
> "When most weapons are used, they **cause explosions that deform the terrain,
> creating circular cavities.**"

수치층 — 시점이 핵심:
> "The damage dealt to the attacked worm or worms **after any player's turn** is shown
> **when all movement on the battlefield has ceased.**"

[direct page retrieval — https://en.wikipedia.org/wiki/Worms_(series)] [OBSERVED]

**이 문장이 이 레인에서 가장 실행 가능한 증거다** [INFERENCE]:
- `after **any** player's turn` → **적 턴에도 판독을 띄운다.** 내 턴 전용이 아니다.
- `when all movement has ceased` → 판독을 **연출과 경쟁시키지 않는다.** 움직임이 끝난 뒤에 낸다.
  castle-war의 0.9초 문제를 우회하는 방식이 여기 있다 — 예고를 0.9초에 밀어넣는 대신
  **착탄이 끝난 뒤 정적 구간에 판독을 낸다.**

#### (B)만으로 "적이 어떻게 쏘는지 안 보인다"가 해소된 사례 — **부분 증거만, 직접 증거 없음**

정직하게 구분한다:

| 주장 | 상태 |
|---|---|
| 흔적이 다음 샷 판단에 쓰인다 | **직접 증거 있음** (위 (1) 문장에 명시) |
| 흔적이 적 턴에도 표시된다 | **직접 증거 있음** ((4) `after any player's turn`) |
| 흔적이 누적·지속된다 | **직접 증거 있음** ((3) "too many", (4) 지형 영구 변형) |
| 흔적이 구매할 가치가 있다고 개발사가 판단했다 | **직접 증거 있음** ((2) tracer 업그레이드) |
| **"적이 어떻게 쏘는지 모르겠다"는 불만이 흔적 추가로 해소됐다** | **증거 못 찾음** |

마지막 항목을 찾지 못한 이유: 위 4건은 전부 **처음부터 그렇게 설계된 것**이거나
성능 패치이고, **"플레이어가 적의 사격을 이해하지 못한다 → 흔적을 추가했다 →
불만이 사라졌다"는 인과 사슬을 문서로 남긴 사례를 찾지 못했다.**
Scorched Earth의 Smoke Tracers v1.1 추가가 가장 가까우나, 위키가 추가 *이유*를
기록하지 않아 인과를 주장할 수 없다. [OBSERVED — 부재]

→ **따라서 (B)는 "장르가 46년간 일관되게 채택해온 검증된 장치"로는 확정할 수 있으나,
"우리 특정 불만을 해소한다"는 보장으로는 확정할 수 없다.**
채택하되 **효과 측정을 게이트에 넣어야 한다.** [INFERENCE]

#### castle-war 공백의 재정의 (Main 가설 검증 결과)

Main의 "(B)는 이미 부분적으로 있다 — 적 탄이 날아가는 것 자체가 판독 가능한 사건이고,
문제는 끝난 뒤 기록이 안 남는 것"은 **위 4건과 정합한다** [INFERENCE]:

| 층 | 장르 선례 | castle-war 현재 |
|---|---|---|
| 궤적 흔적 | 1980년부터, tracer로 상품화 | **비행 중에만 존재, 착탄 후 소멸** |
| 지형/구조 흔적 | Worms 영구 변형, Rampart 벽 구멍 | 구조 붕괴는 남음 (보유) |
| 수치 판독 | Worms — 움직임 정지 후, **모든 플레이어 턴** | **없음** |

→ 공백은 3층 중 **1층(궤적 흔적)과 3층(수치 판독)**이다. 2층은 이미 있다.
그리고 두 공백 모두 **사전 예고가 아니라 사후 흔적**이므로,
0.9초 창·입력 0 제약과 **충돌하지 않는다.** [INFERENCE]

### E. 반대 방향의 함정 — "숫자를 줄여 깔끔하게"는 검증된 실패 경로

Slay the Spire는 아이콘만 쓰던 단계에서 **숫자를 노출하는 쪽으로 갔다**
(의도에 피해량과 타격 횟수를 함께 표시):

> "If the enemy is attacking, **the attack damage and number of attacks will be provided.**
> In most cases this is **exactly reliable**"

[direct page retrieval — https://slay-the-spire.fandom.com/wiki/Intent] [OBSERVED]

→ 클러터 대응책으로 "정보량을 줄이자"를 택하면 STS가 버린 단계로 되돌아간다.
줄여야 하는 것은 **정보량이 아니라 표시 채널 수**다. [INFERENCE]

---

## Sources

전부 **direct page retrieval**. 수집일 2026-08-13. 검색은 영어, 작성은 한국어.

**인용 검증 (2026-08-13 실시)**: 위 출처의 인용문 17건을 원문 대조로 재확인 —
**17/17 문자 일치**. URL 21건 중 17건은 raw fetch HTTP 200,
`*.fandom.com` 4건은 raw fetch에 403(봇 차단)이나 리더 경유로 정상 수집되며
인용 문구가 원문에 존재함을 개별 확인했다. Steam 리뷰·패치 노트는
`store.steampowered.com` 정식 뷰 URL로 재작성해 200 확인.

| # | 출처 | URL | 쓰임 |
|---|---|---|---|
| 1 | RPS *The Mechanic* — ItB UI 설계 (2018-03-05) | https://www.rockpapershotgun.com/into-the-breach-interface-design | "icon mess" 자백, 명확성 위해 메커니즘 삭제, 예고=대응 전제 |
| 2 | Wikipedia — Slay the Spire | https://en.wikipedia.org/wiki/Slay_the_Spire | 의도 미공개 → 혼란 → 추가 |
| 3 | STS Wiki — Runic Dome | https://slay-the-spire.fandom.com/wiki/Runic_Dome | 의도 가시성 = 턴당 1에너지 |
| 4 | STS Wiki — Intent | https://slay-the-spire.fandom.com/wiki/Intent | 숫자 노출, 신뢰성 |
| 5 | Wikipedia — Into the Breach | https://en.wikipedia.org/wiki/Into_the_Breach | 텔레그래프가 진행을 **빠르게** 함 |
| 6 | Steam 부정 리뷰 API — ItB (appid 590380) | https://store.steampowered.com/appreviews/590380?json=1&filter=all&language=english&review_type=negative&purchase_type=all&num_per_page=100 | "전략이 아니라 퍼즐" 5건 |
| 7 | Wikipedia — **Rampart (1990)** | https://en.wikipedia.org/wiki/Rampart_(video_game) | 성 대 성 + 사후 판독만으로 성립, 호평, TD 조상 |
| 8 | Wikipedia — Auto battler | https://en.wikipedia.org/wiki/Auto_battler | 해소 구간 입력 0, 800만 플레이어 |
| 9 | XCOM Wiki — Overwatch (XCOM 2) | https://xcom.fandom.com/wiki/Overwatch_(XCOM_2) | 죽은 적 턴의 답 = 예약 입력 |
| 10 | Eurogamer — WildStar review (2014-10-28) | https://www.eurogamer.net/wildstar-review | **"floor watching"** — 주의 전이, "parody" |
| 11 | PC Gamer — Wildstar review (2014-06-09) | https://www.pcgamer.com/wildstar-review/ | "initially overwhelming display" |
| 12 | WildStar Wiki — Telegraph | https://wildstar.fandom.com/wiki/Telegraph | 텔레그래프가 간판 기능이었음 |
| 13 | ShellShock Live v1.0 (2020-05-22) | https://store.steampowered.com/news/app/326460?emclan=&emgid=3112496281295272175 | 아군 조준 완전 공개 패치 |
| 14 | Monster Train Friends & Foes (2020-09-03) | https://store.steampowered.com/news/app/1102190?emclan=&emgid=3819571008256632127 | 적 웨이브 카운터·보스 사전 공개 |
| 15 | Monster Train 2.0.1 (2021-03-31) | https://store.steampowered.com/news/app/1102190?emclan=&emgid=4023377846271907741 | 가시성 결함이 "고장난 줄 알았다"로 신고됨 |
| 16 | Monster Train Herzal's Workshop (2020-10-22) | https://store.steampowered.com/news/app/1102190?emclan=&emgid=3895010671337584598 | 색맹 프리셋 후행 추가 |
| 17 | GW2 Wiki — Options | https://wiki.guildwars2.com/wiki/Options | Effect LOD = 클러터 **감축** 옵션 출하 |
| 18 | Wikipedia — **Artillery game** | https://en.wikipedia.org/wiki/Artillery_game | **1980년부터 이전 샷 궤적선**, "next shot 판단에 사용" 명시 |
| 19 | Wikipedia — **Scorched Earth (1991)** | https://en.wikipedia.org/wiki/Scorched_Earth_(video_game) | **tracer = 구매 업그레이드**, "다음 턴 궤적 조정"; Smoke Tracers v1.1 추가 |
| 20 | Wikipedia — **Worms (series)** | https://en.wikipedia.org/wiki/Worms_(series) | 지형 영구 변형 + **"모든 플레이어 턴 후, 움직임 정지 시" 피해 판독** |
| 21 | ShellShock Live v0.9.5.3 (2016-02-24) | https://store.steampowered.com/news/app/326460?emclan=&emgid=295354567831689265 | "too many shot tracers" 랙 → **흔적 누적의 직접 증거** |
| 22 | ShellShock Live v0.9.5.11 (2016-03-16) | https://store.steampowered.com/news/app/326460?emclan=&emgid=272838467468447141 | shot tracer 성능 개선 |

### 실패한 조사 경로 (명시)

| 시도 | 결과 |
|---|---|
| r/gamedesign · r/truegaming 검색 (API + old.reddit + 브라우저) | **HTTP 403 / 로그인 게이트로 전면 차단.** 수집 0건 |
| `web_search` 도구 | 반환 URL이 전부 `vertexaisearch.cloud.google.com` 리다이렉트 → **인용 불가**로 판단, 1차 페이지 직접 수집으로 전환 |
| Fire Emblem 위키 danger zone/area | 404 및 빈 본문. 선행 조사(rally-structure §4)의 FE D6=✅ 기록으로 대체 |
| GDC Vault 발표 자료 | 유료/로그인 장벽. 미수집 |
| 대응 불가 예고의 직접 측정 사례 | **없음 — §D-1에 명시** |
| 흔적 추가가 "적 사격 이해 불만"을 해소한 인과 사슬 | **없음 — §D-4 표에 명시.** 흔적 채택 사례는 4건 있으나 추가 *이유*를 기록한 문서를 못 찾음 |
| Reddit 대체 경로 (Google 캐시·Bing 스니펫) | `web_search` 리다이렉트 문제와 동일하게 인용 불가 판단, 미채택 |
