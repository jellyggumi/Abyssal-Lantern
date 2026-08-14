# Context

- run-id: `20260814-siege-first-turn-lane-a`
- lane: A (플레이어 경험 맥락) / mode: `market-landscape`
- 이 레인이 다루는 것: **선공 이점이 플레이어에게 무엇으로 느껴지는가.** 밸런스 수치가 아니라
  발언·불평·자가 대처가 대상이다. 장치 카탈로그는 레인 B, 출하 이력은 레인 C, 대체 경로는 레인 D.
- **전제 정정 (2026-08-14, Main IRC 브리핑)**: 트리아지에 적힌 "선공 38%p"는 낡은 값이다.
  87.0%는 2026-08-12 기준선이고, 그 뒤 `OneShotSiegeRules.OpeningVolleyDamageScale = 0.5`
  (개막 발사만 절반)이 들어가 **고정 선공 47.0% / 교대 53.0%, 둘 다 45~55 밴드 안**이다
  (`_workspace/current/qa/gate-measurements.md:15,38-39`). 따라서 이 문서의 질문은
  "어떻게 고칠까"가 아니라 **"이미 고른 장치가 플레이어에게 좋은 선택인가"**로 바뀐다.

## 수집 방법과 검증

- **Steam 공개 리뷰 API** (영어, 전기간)로 8개 앱을 받아 선공/실력격차 어구로 필터링했다.
- **appid 귀속을 개별 검증했고 3건이 틀렸다** — `1041130`은 Hedgewars가 아니라 *Darkest Depths*,
  `301250`은 *SPORT1 Live : Duel*, `219150`은 Worms Clan Wars가 아니라 **Hotline Miami**였다.
  세 앱을 코퍼스에서 **폐기**했다. 남은 검증 코퍼스는 ShellShock Live 400 / Worms Armageddon 400 /
  Worms W.M.D 400 / Worms Revolution 400 / Worms Ultimate Mayhem 300 = **1,900건**.
  (검증 없이 썼다면 이 문서는 Hotline Miami 리뷰를 "Worms 플레이어 발언"으로 인용했을 것이다.)
- **인용한 Steam 리뷰는 전부 개별 퍼머링크가 HTTP 200 + 본문 문자열 일치로 재확인**됐다(2026-08-14). 9/9 MATCH.
- Reddit은 API가 403이라 **브라우저 렌더링**으로 회수했다.
- `worms2d.info`(Worms 커뮤니티 위키)는 **anti-bot 챌린지(Anubis)로 직접 회수 실패**했다.
  따라서 Worms 대회 규정·턴 순서 규칙에 대한 1차 확인은 **하지 못했다**. 아래에서 그렇게 표기한다.

---

## Workflow Context

### 1. "한 턴 먼저"가 누적되는 게임과 아닌 게임

턴제 포격에서 선공은 그 자체로 이점이 아니다. **이점이 되려면 먼저 쏜 한 발이 남아야 한다.**
남는 방식이 구조를 가른다.

| 구조 | 첫 발의 운명 | 선공 이점 | 사례 |
|---|---|---|---|
| **동시 침식형** | 양측 자원이 각자 깎이고 복구되지 않음 → 첫 발의 차이가 **끝까지 보존** | **누적** | castle-war, 바둑(집), 체스(템포) |
| **상태 재설정형** | 매 턴 위치·자원이 재배치되어 첫 발 효과가 희석 | 약함 | Worms (웜 이동·크레이트·지형 변화) |
| **자원 성장형** | 뒤에 두는 쪽이 더 큰 자원을 먼저 씀 | 상쇄 가능 | Hearthstone (The Coin + 추가 카드) |

바둑은 이 문제를 가장 오래 계량한 종목이다. 흑의 선착 이점은 **종국 시점 5~7집**으로 평가되고,
보정(덤)은 2.5 → 4.5 → 5.5 → 6.5/7.5집으로 **수십 년에 걸쳐 상향**됐다. 5.5집으로도 부족하다는 것이
통계로 확인됐고, 데이터베이스가 **흑 승률 53%** 를 최상위 이외 구간에서도 확인했다.
`[direct page retrieval — https://en.wikipedia.org/wiki/Komi_(Go)]`

> 덤의 교훈은 "보정을 넣었다"가 아니라 **"한 번에 맞히지 못했다"**다.
> 100년 가까이 재측정하며 값을 올렸다. 첫 보정값이 정답일 확률은 낮다는 뜻이다. `[INFERENCE]`

### 2. castle-war가 누적형인 이유

- 양측이 **성벽·코어를 서로 깎기만 한다.** 회복 수단이 없다 → 첫 발의 차이가 보존된다.
- 성 하나 파괴에 **19.4턴**이 걸리고 먼저 쏘는 쪽이 **한 턴 일찍** 도달한다.
- **조준 오차(±0.09)의 분산으로 그 격차가 뒤집히지 않는다.**
  `[direct page retrieval — _workspace/current/qa/gate-measurements.md:60-63]`

즉 castle-war의 선공 이점은 "먼저 때려서 기선을 잡는다"는 심리적인 것이 아니라
**19.4턴 뒤에 정확히 한 턴만큼 먼저 도착한다**는 산술적인 것이다. 그래서 조준 실력으로 상쇄되지 않았다.

### 3. 현재 채택된 장치와 그 성격

`OneShotSiegeRules.cs:20,38-39` — `OpeningVolleyDamageScale = 0.5`,
`OpeningVolleyDamageMultiplier(completedTurns) => completedTurns <= 0 ? 0.5f : 1f`.
**개막 1발에만** 적용된다. 코드 주석이 의도를 직접 적어두었다:

> "The side that shoots first gets tempo before the defender can answer. Reducing only
> that opening volley to 50% removes the measured 87% first-mover win rate without
> changing projectile identity, later-turn damage, or who takes the first shot."
> `[direct page retrieval — Assets/Scripts/OneShotSiegeRules.cs:33-39]`

플레이어 경험 관점에서 이 장치의 가장 중요한 성질은 **플레이어가 아무것도 하지 않아도 적용된다**는 것이다.
이 성질이 왜 결정적인지는 §Adjacent Problems에서 Hearthstone The Coin과 대조한다.

### 4. 선공 이점은 실력에 비례해 커진다 — 이 레인의 핵심 발견

체스는 백이 **54~56%** 를 얻는다. 그런데 중요한 것은 평균이 아니라 **기울기**다.

> "White's advantage is less significant in blitz games and games between lower-level players,
> and becomes greater as the level of play rises."
> `[direct page retrieval — https://en.wikipedia.org/wiki/First-move_advantage_in_chess]`

> GM Evgeny Sveshnikov: "statistics show that White has **no advantage over Black in games
> between beginners**, but 'if the players are stronger, White has the lead'."
> `[direct page retrieval — 동일]`

Adorján 데이터: Elo 2700+ 구간 백 승률 **55.7%**, Elo 2100 미만 구간 **53.1%**. `[direct page retrieval — 동일]`

> **castle-war 심은 "양측 실력이 같다"고 가정한다** — 그리고 그 가정은 **완벽한 실력**에 가깝다.
> 즉 심이 측정한 87%/47%는 체스로 치면 **최상위 구간의 값**이다.
> 실제 플레이어가 서툴수록 선공 이점은 **줄어든다**. 47.0%라는 값은
> 초보 구간에서 더 중앙에 가까울 가능성이 높다. `[INFERENCE — 체스 기울기 + 심 가정 대조]`

단, Hearthstone은 반대 방향의 데이터를 낸다: 2013년 Blizzard 통계는 **상위 랭크 선공 51.3%,
하위 구간 53%** 로 **낮은 구간에서 선공 이점이 더 컸다.**
`[direct page retrieval — https://hearthstone.fandom.com/wiki/The_Coin]`
→ **두 종목이 반대다.** "선공 이점은 실력에 비례한다"를 보편 법칙으로 쓰면 안 된다.
castle-war가 어느 쪽인지는 **실제 사람 플레이테스트 없이는 알 수 없다.** `[INFERENCE]`

---

## Affected Users

같은 38%p(혹은 지금의 6%p)도 집단마다 다른 것으로 느껴진다. **가장 중요한 비대칭은
"선공을 인지하는가"** 이다 — 인지하지 못하면 불평이 아니라 **이탈**로 나타난다.

| 집단 | 무엇을 느끼는가 | 선공을 인지하는가 | 관측되는 행동 |
|---|---|---|---|
| **캐주얼 플레이어** | "이 게임 좀 이상한데" 수준의 막연함. 원인을 턴 순서로 특정하지 못함 | ✗ **거의 못 함** | 조용히 그만둔다. 리뷰를 남기지 않는다 |
| **경쟁 플레이어** | 승패의 귀속이 흐려지는 것에 분노. 실력 증명이 오염됨 | ✓ 명확히 | 포럼에 수치를 들고 온다. 하우스 룰을 만든다 |
| **스트리머/시청자** | 첫 턴에 끝나는 경기 = 방송 사고. 콘텐츠가 안 됨 | ✓ 결과로 | 클립화("turn 1에 죽었다")하거나 그 게임을 접는다 |
| **밸런스 담당자** | 다른 모든 측정이 오염됨 | ✓ 측정으로 | 기믹 평가가 불가능해진다 |
| **초보를 데려온 숙련자** | 친구가 재미없어하는 것을 실시간으로 봄. 자기가 이겨서 미안함 | ✓ 대리로 | **스스로를 약화시킨다** (§Current Workarounds) |

**캐주얼이 침묵한다**는 것이 이 표에서 가장 중요하다. 검증된 1,900건 코퍼스에서
선공을 명시적으로 문제 삼은 리뷰는 **4건뿐**이었고 실력 격차 불만은 훨씬 많았다.
선공 불공정은 **리뷰로 잘 표현되지 않는 종류의 불만**이다. `[OBSERVED — 1,900건 필터 결과]`

> castle-war의 원래 트리아지가 지적한 문제가 정확히 이것이다:
> "플레이어는 **이기는 이유가 실력이 아니라 순서라는 것을 모르는 채 이긴다.**"
> `[direct page retrieval — .survey/siege-first-turn-fairness/triage.md:10-11]`

숙련자 집단은 실제로 관측된다. ShellShock Live 75레벨 플레이어가 10~15레벨 친구들과 놀기 위해
쓴 글 전체가 자기 약화 방법을 묻는 내용이다:

> "I'm Lvl 75 and my buddies are 10-15, the skill gap is wide … I still end up **rolling them
> most the time** though..."
> `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/1on2sqp/how_do_i_nerf_myself_for_my_low_level_friends/]`

---

## Current Workarounds

플레이어와 커뮤니티가 **게임이 해주지 않아서 스스로 만든** 대처들이다.
분류하면 (a) 규칙 차원 보정, (b) 자가 약화, (c) 상대 선별, (d) 이탈 — 네 가지다.

### (a) 규칙 차원 보정 — 추상 전략 게임이 표준화한 것

- **덤/komi (바둑)**: 후수에게 고정 점수를 준다. 표준 6.5(일·한), 7.5(중·AGA), 7(뉴질랜드).
  **반집 단위**를 쓰는 이유는 무승부를 없애기 위해서다.
  `[direct page retrieval — https://en.wikipedia.org/wiki/Komi_(Go)]`
- **파이 룰 / 스왑 룰**: 한 명이 첫 수를 두면 **상대가 그 자리를 가져갈지 고른다.**
  "한 사람이 파이를 자르고 다른 사람이 고른다"에서 온 이름. Hex가 채택, TwixT 대회 규정.
  1909년 만칼라 계열에서 최초 보고. `[direct page retrieval — https://en.wikipedia.org/wiki/Pie_rule]`
- **경매 덤 (auction komi)**: 덤 값 자체를 플레이어가 부르고 상대가 색을 고른다.
  → **정답 보정값을 몰라도 공정해진다.** 이것이 파이 룰의 핵심 성질이다.
  `[direct page retrieval — 동일]`
- **핸디캡 대국**: 실력 차가 나면 덤을 0.5로 두고 흑이 미리 여러 점을 깐다.
  즉 바둑은 **선공 보정과 실력 보정을 분리된 두 장치로** 처리한다.
  `[direct page retrieval — https://en.wikipedia.org/wiki/Komi_(Go)]`

> castle-war에 직접 쓸 수 있는 것은 **경매 덤/파이 룰의 사고방식**이다 —
> "0.5가 정답인지 모르겠다"면 값을 고정하지 말고 **고르게 하면** 된다. 단 1인용 vs AI에서는
> 상대가 고를 주체가 없으므로 그대로는 성립하지 않는다. `[INFERENCE]`

### (b) 자가 약화 — 숙련자가 스스로를 깎는다

ShellShock Live의 75레벨 플레이어는 이미 이것들을 하고 있었다:
스킬 포인트를 최고 레벨 친구에 맞추고, 피해를 균등 분배하고, 좋은 무기를 피한다.
**그래도 대부분 이긴다.** 커뮤니티가 제안한 추가 수단:

- **리바운드 제한**: "you only get to aim by hitting off of the side walls or hitting a bumper first"
  → 원 글쓴이 후기: "The trickshots only helped a good deal, its actually quite fun to use that
  limiter as well" — **자가 핸디캡이 숙련자 본인에게도 재미가 됐다.**
- **게임 제공 균등화 사용**: "Use 'level field' settings in your lobby customization.
  This way everyone will be 'nerfed' to the same level as the lowest configured member."

`[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/1on2sqp/how_do_i_nerf_myself_for_my_low_level_friends/]`

> **그런데 그 스레드의 결론이 이 조사에서 가장 중요한 한 줄이다:**
> > "Skill diff is still a major aspect."
> ShellShock Live는 **장비를 완전히 균등화하는 공식 기능이 있는데도** 실력 격차가 남는다.
> → **장비/피해 균등화는 조준 실력 절벽을 고치지 못한다.** `[direct page retrieval — 동일]`
> castle-war의 개막 발사 50% 감쇠는 **피해 축**의 보정이다. 같은 한계를 물려받을 가능성이 높다. `[INFERENCE]`

### (c) 상대 선별 — 매칭을 손으로 한다

- **레벨 상한 로비**: "Try setting up your own lobby and **reducing the level difference maximum**
  down to the lowest amount that will let you play with your friends."
  `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/7ewgh2/this_game_is_very_unfriendly_to_new_players_and/]`
- **높은 레벨 축출** — 4줄짜리 리뷰 전문이 이 워크어라운드와 그 실패를 동시에 적는다:
  > "1. Host Game / 2. **Kick all those of a higher level than you** / 3. Play game / **4. Still lose**"
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198119498152/recommended/326460/ · +60 helpful]`

### (d) 규칙 변경 요구 / 이탈

- **턴 제한시간 폐지 요구**: "If T17 is interested in growing the player base, a great way to do it
  would be to make the learning curve waaaaaaay lower by providing an **unlimited turn time option**."
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198362135968/recommended/327030/ · Worms W.M.D · +42]`

### 확인하지 못한 것

**Worms 대회의 선공 규정을 1차로 확인하지 못했다.** `worms2d.info`가 anti-bot 챌린지로
직접 회수를 거부했고, 웹 검색 결과는 리다이렉트 URL만 반환해 원문 대조가 불가능했다.
"Worms는 라운드마다 선공을 랜덤 재추첨한다"는 진술을 검색 요약에서 봤으나
**1차 확인 실패이므로 사실로 쓰지 않는다.** `[thin evidence — 원문 대조 실패]`

---

## Adjacent Problems

### 1. 보정 장치 자체가 초보에게 불리해질 수 있다 — 가장 중요한 인접 문제

Hearthstone은 후수에게 **The Coin**(마나 +1 일회성)과 **추가 카드 1장**을 준다.
Blizzard는 여러 대안을 시험했고 그중 **"후수가 35 체력으로 시작"을 채택하지 않았다.**
`[direct page retrieval — https://hearthstone.fandom.com/wiki/The_Coin]`

그리고 같은 문서가 이렇게 적는다:

> "It can be argued that **good use of The Coin requires an additional degree of strategy,
> therefore offering a disadvantage for less experienced players.**"
> `[browser-rendered indexed snippet — 동일 페이지, 팬 위키 = 2차 출처]`

> **castle-war 대조 — 우리 두 장치는 정반대 성질을 가진다.** `[INFERENCE]`
>
> | 장치 | 발동 | 초보가 혜택을 받는가 |
> |---|---|---|
> | **개막 발사 50% 감쇠** | **자동.** 플레이어 입력 0 | ✓ **받는다** — 몰라도 적용됨 |
> | **LAST STAND (일발역전)** | **플레이어가 R을 눌러 수동 장전.** AI는 자동 장전 | ✗ **못 받을 수 있다** |
>
> 근거: `DynamicBattlefield.cs:705-708` — "The player arms it manually (R); the AI's weaker
> mirror arms itself." 그리고 `ComebackAsymmetryTests.cs:90-95` — 플레이어 래치는 `Armed`에서
> 멈추고("the shot is theirs to time") AI 래치는 `Active`로 직행한다.
> `[direct page retrieval — 저장소 코드]`
>
> **즉 개막 감쇠는 The Coin의 함정을 피했고, LAST STAND는 정확히 그 함정에 들어가 있다.**
> R을 모르는 초보에게 LAST STAND는 존재하지 않는 장치이고, AI에게는 항상 존재하는 장치다.
> 컴백 장치가 **컴백이 필요한 쪽에게만 조건부로 작동하지 않는다.**
> 이것은 레인 D의 "LAST STAND가 절벽을 완화하는가" 질문에 대한 이 레인의 입력이다.

### 2. 첫 턴 정보 비대칭

선공은 **정보를 주지 않고 행동해야 하는 쪽**이기도 하다. Worms 커뮤니티에서 웜 배치가
"placement gambling"으로 불리는 이유가 이것이다. 다만 앞서 밝힌 대로 이 진술은
1차 확인에 실패했다. `[thin evidence]`
castle-war는 배치 단계가 없고 첫 발이 곧 첫 행동이므로 **이 문제는 거의 없다.** `[INFERENCE — 코드 구조 대조]`

### 3. 첫 턴 즉사 = 콘텐츠가 아니라 사고

ShellShock Live에서 첫 턴 사망은 **밈으로 소비될 만큼** 흔하다.

- 리뷰: "I once made a friend **rage quit first turn** because I lined up a sniper shot and
  one hit KO'ed him. Good game."
  `[direct page retrieval — https://steamcommunity.com/profiles/76561198082132193/recommended/326460/]`
- 서브레딧 게시물 제목: **"first turn of the first game of the day :("** — 본인이 자기 탄에 죽는 영상.
  댓글: "wow, you managed to get yourself killed in turn 1." / "this video should actually be a world record."
  `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/w84dme/first_turn_of_the_first_game_of_the_day/]`
- 서브레딧 게시물 제목: **"Me: \*dies in first Turn\* My Teammates:"**
  `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/g2z5gq/me_dies_in_first_turn_my_teammates/]`

> castle-war는 **코어 150 HP**라 1발 즉사가 구조적으로 불가능하고, 개막 발사는 다시 절반이다.
> 이 인접 문제는 **이미 이중으로 막혀 있다.** `[INFERENCE — 코드 대조]`
> 다만 반대 위험이 생긴다: 첫 발이 절반이면 **첫 턴이 시시하게 느껴질 수 있다.**
> ShellShock Live의 밈이 성립하는 이유는 첫 턴이 극적이기 때문이다. 여기에 대한 플레이어 발언은
> **찾지 못했다** — 개막 발사를 약화시킨 게임의 플레이어 반응은 이 조사 범위에서 확인되지 않았다.
> (해당 이력 조사는 레인 C 담당.)

### 4. 집중 공격 / 킬 우선순위

다자전에서는 선공 이점이 "누가 먼저 맞는가"로 변형된다. ShellShock Live 플레이어의 답변이
그 계산을 그대로 노출한다:

> "I always **focus whoever has the least amount of armor and has a high level.** … taking you
> off the battlefield earlier is a huge advantage."
> `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/wjnogx/i_am_being_focused_all_the_time/]`

castle-war는 1대1이므로 직접 적용되지 않지만, **"먼저 제거하면 그 뒤 모든 턴이 이득"** 이라는
누적 논리는 동일하다. `[INFERENCE]`

### 5. 선공 결정 방식에 대한 불신 → 핵 의심

무작위로 정해지는 이점은 **부정행위로 읽힌다.** r/ShellShockLive 검색 상위에 "Possible Hackers?",
"Recently played against a hacker" 스레드가 선공 관련 검색어로 함께 걸린다.
`[direct page retrieval — https://www.reddit.com/r/ShellShockLive/search/?q=first+turn&restrict_sr=1]`
선행 조사도 같은 패턴을 기록했다: **"텔레그래프되지 않은 규칙은 AI 치팅으로 해석된다"**
`[direct page retrieval — .survey/siege-artillery-landscape/context.md:61-63]`

> castle-war는 현재 **항상 플레이어 선공**이라 이 문제가 없다.
> 만약 교대나 랜덤을 도입한다면 **선공이 누구인지, 왜 그런지 화면에 보여야** 한다. `[INFERENCE]`

---

## User Voices

**규칙: 실제로 존재하고 URL로 재확인된 발언만 인용한다.** Steam 인용은 전부 개별 퍼머링크
HTTP 200 + 본문 문자열 일치로 검증했다(2026-08-14, 9/9). 창작·의역 없음.

### 선공에 대하여

1. > "I once made a friend **rage quit first turn** because I lined up a sniper shot and one hit
   > KO'ed him. Good game."
   — ShellShock Live 리뷰 (+5 helpful, 44.8h)
   `[direct page retrieval — https://steamcommunity.com/profiles/76561198082132193/recommended/326460/]`

2. > "wow, you managed to get yourself killed in turn 1." / "this video should actually be a world record."
   — r/ShellShockLive, "first turn of the first game of the day :(" 댓글
   `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/w84dme/first_turn_of_the_first_game_of_the_day/]`

3. > "many of those who play it still use the option of **'rage quit' after 1 missed** or for a
   > little dmg done instead of continuing the fight"
   — ShellShock Live 리뷰 (+21 helpful, 711.9h, 추천)
   `[direct page retrieval — https://steamcommunity.com/profiles/76561198393970191/recommended/326460/]`

4. Ben Brode (Hearthstone 개발): **"It's about three percent better to go first over second.
   It's only a tiny bit better to go first at the lower levels of play."** (2014-02-05)
   그리고 2015-03: "the win rate is very close to 50-50 regardless of who has the coin",
   다만 최상위 0.01%에서도 "still better to go first, on average".
   `[browser-rendered indexed snippet — https://hearthstone.fandom.com/wiki/The_Coin · 팬 위키 = 2차 출처, 원 발언은 개발자]`

5. GM Evgeny Sveshnikov: **"White has no advantage over Black in games between beginners"**,
   그러나 "if the players are stronger, White has the lead".
   `[direct page retrieval — https://en.wikipedia.org/wiki/First-move_advantage_in_chess]`

> **선공 자체에 대한 플레이어 불평은 의외로 희소하다.** 검증된 1,900건 Steam 리뷰에서
> 선공/턴 순서를 명시한 것은 **4건**이고, 그중 불공정을 주장한 것은 **0건**이다.
> 나머지는 첫 턴 사고를 농담으로 소비한다. **이것도 발견이다** — 플레이어는 선공 불공정을
> "불공정"이라는 말로 표현하지 않는다. `[OBSERVED — 1,900건 필터 결과]`

### 자가 대처에 대하여

6. > "I'm Lvl 75 and my buddies are 10-15, the skill gap is wide, we already play with wind and
   > I match my skill points with the highest level player other than myself. I spread damage
   > evenly between them, I also avoid using lesser quality weapons if I can avoid it.
   > **I still end up rolling them most the time though...**"
   — r/ShellShockLive, "How do I nerf myself for my low level friends?"
   `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/1on2sqp/how_do_i_nerf_myself_for_my_low_level_friends/]`

7. > "1. Host Game / 2. **Kick all those of a higher level than you** / 3. Play game / **4. Still lose**"
   — ShellShock Live 리뷰 전문 (+60 helpful, 41.4h, 추천)
   `[direct page retrieval — https://steamcommunity.com/profiles/76561198119498152/recommended/326460/]`

---

## 실력 절벽 — 추가 조사

**추가 전제 (Main IRC 브리핑, 2026-08-14)**: 같은 실행에서 조준 품질 **+0.01(1%p)** 이
승률을 **53.0% → 67.0%(+14%p)**, +0.03에서 **94%**, +0.05에서 **100%** 로 움직인다.
승률이 실력의 **절벽 함수**다. 결과로 (가) 배우는 구간이 없고 (나) 수익 게이트
"과금 승률 격차 ≤5%p"가 **조준 우위 0.36%p에서 전부 소진**된다.
`[thin evidence — Main IRC 브리핑 수치. 저장소 문서에서 대응 기록을 찾지 못했다;
_workspace/current/qa/gate-measurements.md에는 47.0/53.0까지만 있고 조준 민감도 표는 없다]`

이 레인의 질문: **실력 차가 승률을 지배하는 게임에서 초보는 무엇을 경험하고 무엇을 불평하는가.**

### 1. 초보가 실제로 쓰는 말 — "zero chance"

가장 강한 증거는 ShellShock Live의 **비추천** 리뷰다(+178 helpful, 플레이타임 223.9h —
초보가 아니라 **숙련자가 초보를 대신해 쓴 글**이다):

> "This is probably **the most unfriendly game towards newer players that I've ever seen.**"
>
> "What if they want to play with friends that have been playing for some time? Well too bad,
> should've joined them at the same time they started playing, because **you're going to be
> severely under equipped while your friends painfully outclass you and destroy you every game.**"
>
> "**Newer players simply have zero chance that it's not even funny.**"
>
> `[direct page retrieval — https://steamcommunity.com/profiles/76561198122681974/recommended/326460/]`

핵심 어휘는 **"every game"** 과 **"zero chance"** 다. 초보는 "졌다"가 아니라 **"항상 진다"**
를 불평한다. 이것이 절벽 함수의 체감 형태다 — 분산이 없으면 패배가 사건이 아니라 **상태**가 된다. `[INFERENCE]`

### 2. 이탈은 "재미없다"가 아니라 "죽는다"로 표현된다

서브레딧 최상위 문제제기 글의 제목 자체가 이탈 예측이다:

> **"This Game Is Very Unfriendly to New Players and Unless Addressed This Game Will Die Out."**
>
> 본문: "the gap between player skill can make the games **unexciting**" …
> "**No one wants to play a game where you are getting shit on the entire time,
> either mechanically or verbally.** As a result this game in its current state is very
> discouraging towards new players and **without new players it WILL Die out.**"
>
> `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/7ewgh2/this_game_is_very_unfriendly_to_new_players_and/]`

주목할 표현은 **"unexciting"** 이다. 실력 격차의 1차 증상은 분노가 아니라 **지루함**이다.
결과가 확정된 경기는 화나기 전에 **재미가 없다.** `[INFERENCE]`

### 3. 배우는 구간이 없으면 배울 시간을 달라고 한다

Worms W.M.D 플레이어들이 요구한 것은 밸런스가 아니라 **시간**이었다:

> "**Everyone I've tried to get into this game just gets frustrated and panicked trying to figure
> out what to do** and then try and pull it off in 90 seconds while I try to explain the controls
> and what all the weapons do."
>
> 인용자 본인: "I'm in the same scenario with this guy, I play with family and girlfriend,
> **they are totally frustrated and panicked during their turns.**"
>
> `[direct page retrieval — https://steamcommunity.com/profiles/76561198362135968/recommended/327030/ · +42]`

또 다른 리뷰(+59)는 **툴팁 부재**를 학습곡선 문제로 지목한다:
"It would be such an easy thing to add and would really make **the learning curve more enjoyable**."
`[direct page retrieval — 검증 코퍼스 recommendationid 70255153, Worms W.M.D]`

> castle-war 대조: 적 턴 0.9초 / 플레이어 입력 제한시간 없음. **시간 압박은 없다.**
> 따라서 이 계열의 불만은 castle-war에 해당하지 않는다. 우리 절벽은 **시간이 아니라 정밀도**다.
> 시간을 더 준다고 조준 품질 +0.01이 메워지지 않는다. `[INFERENCE]`

### 4. 장비 균등화로는 안 고쳐진다 — 반증 사례

이것이 이 절의 실무적으로 가장 중요한 발견이다. ShellShock Live에는
**"level field"** 라는 공식 균등화 옵션이 있다:

> "Use 'level field' settings in your lobby customization. This way everyone will be 'nerfed'
> to the same level as the lowest configured member."
>
> 바로 다음 답글: > **"Skill diff is still a major aspect."**
>
> `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/1on2sqp/how_do_i_nerf_myself_for_my_low_level_friends/]`

> **장비·피해를 완전히 균등화해도 조준 실력 격차는 남는다.**
> castle-war의 `OpeningVolleyDamageScale = 0.5`는 **피해 축** 보정이다.
> 선공 이점(템포)에는 들었지만, **조준 절벽에는 원리적으로 듣지 않는다** —
> 두 문제는 같은 축에 있지 않다. `[INFERENCE — 반증 사례 대조]`
> 실제로 측정이 그것을 보여준다: 개막 감쇠 이후에도 조준 +0.01이 14%p를 움직인다.

### 5. 절벽은 커뮤니티 독성으로 번역된다

실력 격차가 크면 상위 플레이어가 하위를 **표적화**한다. 이것은 감정이 아니라 반복 관측이다:

> "I was barely 100 online games in, and was already confronted with the most foul language you
> can imagine, **rage quitters, people kicking me from games, just because I won the previous one**
> and certain 'top players', who think they are God's gift to humanity"
> — Worms W.M.D (+98 helpful, 237.1h, 추천)
> `[direct page retrieval — https://steamcommunity.com/profiles/76561198067384854/recommended/327030/]`

> "there were other players **calling my friends bad, noobs, trash**, ect. **just because they were
> lower levels.**"
> `[direct page retrieval — https://www.reddit.com/r/shellshocklive/comments/7ewgh2/this_game_is_very_unfriendly_to_new_players_and/]`

castle-war는 현재 **PvE(대 AI)** 이므로 이 경로는 직접 해당하지 않는다.
다만 절벽이 남은 채로 PvP를 붙이면 **이 증상이 그대로 따라온다.** `[INFERENCE]`

### 6. castle-war 고유 요인 — AI가 스스로 워밍업한다

측정 문서가 심의 한계를 명시한다:

> "심은 **양측 실력이 같다**고 가정한다. 실제 AI는 다른 오차 모델을 쓴다 —
> `aiErrorStart 2.5 → aiErrorEnd 0.8`로 **초반에 크게 빗나가고 점점 정확해진다.**
> Last Stand(코어 35%에서 플레이어 2.2× / AI 1.6×)도 모델에 없다."
> `[direct page retrieval — _workspace/current/qa/gate-measurements.md:89-92]`

> 즉 **실제 게임의 AI는 이미 난이도 램프를 갖고 있다.** 초반에 빗나가 주므로 초보가 초반에
> 전멸하지 않는다. 그런데 **조준 민감도 측정은 그 램프가 없는 심에서 나왔다.**
> → 실제 사람이 겪는 절벽은 측정치보다 **완만할 수 있다.** 반대로 후반부에는 AI가 0.8까지
> 정확해지므로 **경기 후반에 절벽이 집중**될 가능성이 있다. 둘 다 아직 사람으로 확인되지 않았다.
> `[INFERENCE — 심 가정과 런타임 AI 모델의 차이]`

### 7. 찾지 못한 것

- **"조준 보조를 넣었다가 뺐다"는 플레이어 반응**을 찾지 못했다. 조준 보조 도입/철회 이력 자체는
  레인 C 담당이지만, 그에 대한 **플레이어 발언**은 이 조사 범위에서 확인되지 않았다.
- **"개막 공격을 약화시킨 게임"의 플레이어 반응**도 찾지 못했다. 첫 턴이 시시해졌다는 불평이
  존재하는지 확인하지 못했다.
- **Gunbound 초보 이탈 발언**의 1차 출처를 확보하지 못했다. 웹 검색은 요약문만 반환하고
  리다이렉트 URL만 제공해 원문 대조가 불가능했으며, 해당 서브레딧 검색은 결과 0건이었다.
  **따라서 Gunbound에 대한 주장은 이 문서에 쓰지 않았다.** `[thin evidence — 원문 확보 실패]`

### 8. 이 레인이 다음 결정에 넘기는 것

1. **개막 발사 50% 감쇠는 초보 친화적 성질을 갖는다** — 자동 적용이라 The Coin의 함정
   ("보정을 쓰려면 실력이 필요하다")을 피했다. 이 점에서 **좋은 선택이다.** `[INFERENCE]`
2. **그러나 그것으로 절벽이 해결되지 않는다** — ShellShock Live의 "level field" 반증이
   피해 축 균등화의 한계를 보여준다. 조준 절벽은 별개 축의 문제다. `[INFERENCE]`
3. **LAST STAND는 재검토 대상이다** — 수동 장전(R) 요구가 초보를 배제한다.
   AI는 자동 장전한다. 컴백 장치가 컴백이 필요한 쪽에 조건부로만 작동한다. `[INFERENCE — 코드 대조]`
4. **캐주얼은 불평하지 않고 사라진다** — 1,900건에서 선공 불공정 명시 불평 0건.
   따라서 **리뷰·포럼 부재를 "문제 없음"으로 읽으면 안 된다.** `[OBSERVED]`
5. **사람 플레이테스트 없이는 절벽의 실제 기울기를 알 수 없다** — 체스는 실력이 오를수록
   선공 이점이 커지고, Hearthstone은 반대다. 두 종목이 반대 방향이므로 심 값만으로
   castle-war가 어느 쪽인지 결정할 수 없다. `[INFERENCE]`
