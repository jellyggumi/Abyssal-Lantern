# Actual Behavior (Lane C)

> [!important] 전제 정정 (2026-08-14, Main IRC 브리핑) — 이 레인의 질문이 바뀌었다
> 트리아지의 "선공 38%p"는 **낡은 값**이다. 87.0%는 2026-08-12 기준선이고, 그 뒤
> `OneShotSiegeRules.OpeningVolleyDamageScale = 0.5`(개막 발사만 절반)가 들어가
> **고정 선공 47.0% / 교대 53.0%, 둘 다 45~55% 밴드 안**이다.
> 내가 코드로 직접 확인했다 — `Assets/Scripts/OneShotSiegeRules.cs:20,38-39`:
> `OpeningVolleyDamageMultiplier(completedTurns) => completedTurns <= 0 ? 0.5f : 1f`.
> 주석이 의도를 직접 적어두었다: *"Reducing only that opening volley to 50% removes the
> measured 87% first-mover win rate without changing projectile identity, later-turn
> damage, or who takes the first shot."* `direct page retrieval — 코드`
>
> **따라서 이 레인의 질문은 "어떻게 고칠까"가 아니라 "우리가 이미 고른 장치가 좋은
> 선택인가"다.** 핵심 판정 항목: 다른 게임이 *첫 턴 행동을 약화시키는* 장치를
> 채택했다가 버린 이력이 있는가. → **답은 §3.5와 맨 끝 절에 있다. 폐기 0건이다.**

이 레인은 장치를 넓게 훑지 않는다(그건 Lane B). **실제로 출하된 것**과 **버려진 것**만
1차 출처로 확인한다. 버린 사례가 채택 사례보다 값나간다 — 채택은 의도를 말하고,
폐기는 결과를 말한다.

---

## What People Actually Use

### 1.1 Gunbound — 턴 순서 자체를 무기 딜레이로 대체한 유일한 계보

castle-war의 "1턴 1발사"와 **직접 비교 가능한 유일한 대안 구조**다. 여기부터 자세히 쓴다.

Gunbound(Softnyx, 한국 2002-05 / 월드와이드 2003-08)는 **교대 턴을 쓰지 않는다.**
누가 다음에 쏘는지가 고정 순서가 아니라 **누적 딜레이 수치의 함수**다. 공식 백과 서술:

> *"Gunbound also implements a 'delay' turn system which is influenced by the Mobile, the
> weapon and/or item a player uses — using items or taking time with actions results in a
> longer wait before the player's next turn."*
> — Wikipedia "Gunbound", `direct page retrieval`

정확한 작동은 이렇다. 모든 플레이어는 누적 딜레이 값을 갖고, **그 값이 가장 낮은 쪽이
다음 턴을 가져간다.** 화면 좌하단 목록이 이 순서로 정렬되며, 표시되는 숫자는 절대값이
아니라 **자기 기준 상대값**이다(상대 이름 옆 `+100`이면 그가 나보다 딜레이 100 많다는
뜻이고, 따라서 내가 먼저 쏜다). 발사가 끝나면 그 턴에 쌓인 딜레이가 합산되고 목록이
재정렬된다. 여기서 결정적인 결과가 나온다 — **자기 딜레이가 계속 낮으면 상대가 한 번
쏘는 동안 두 번 쏠 수 있다(double turn).** 순서가 자원이 된다.

딜레이가 붙는 축이 셋이다.

| 축 | 값 | 근거 |
|---|---|---|
| **무기 선택** | Shot1 **250** (거의 모든 모빌 공통) / Shot2 **300~480** (모빌별) / SS **800** (Knight·Dragon만 840) | 하위 레인 `GunboundDelayNumbers`, StrategyWiki 본문 (archive 경유), `direct page retrieval` |
| **행동 시간** | **초당 10 딜레이**. Turtle만 **초당 12** — 게임 내 유일 예외 | 같음 |
| **아이템** | Dual **600**, Energy Up 2 **300**, Dual+ **250**, Thunder **200**, Change Wind/Power Up/Teleport 각 **150**, Energy Up **100**, Team Teleport/Bunge Shot 각 **50**, Blood **0** | 같음 |

원문 인용(하위 레인이 archive 경유로 본문 확인, `direct page retrieval`):

> *"Delay determines the order which players get their turn. Shot1 has the least delay.
> Shot2 has more delay (Except for J.Frog, Shot1 and Shot2 of this mobile has the same
> delay), and SS has lots of delay. Every second it takes to take your turn, 10 is added
> to your delay; except for Turtle, that gives 12 delay per second. Items can give lots of
> delay, oftentimes more than SS."*

그리고 double turn이 규칙에서 **계산 가능한 것**으로 문서화돼 있다:

> *"If you see more than +600 next to the name of your opponent, you can safely use Dual
> ... and know that you will move ahead of your opponent on the next turn."*
> 작동 예시: *"Opponent +800 → Upon using Dual and consuming 5 seconds to calculate your
> attack → Opponent +150 (800 − 600 (Dual) − 50 (5 seconds)). On the next turn, you will
> still move ahead of your opponent."*

하위 레인이 이 예시를 산술로 재현해 규칙 모델이 맞음을 확인했다(800 − 600 − 50 = 150). ✅

**castle-war에 주는 시사점이 셋이다.**

1. **선공 이점을 "보정"하지 않고 순서 개념 자체를 없앴다.** 첫 턴이 누구인가는
   Gunbound에서 의미가 약하다 — 두 번째 턴부터는 순서가 플레이어 선택의 결과다.
   castle-war는 반대 방향을 골랐다(순서는 고정, 첫 발 피해만 감쇠). 둘 다 유효하지만
   Gunbound 쪽은 **비용이 규칙 전체**이고 우리 쪽은 **비용이 상수 하나**다.
2. **강한 행동에 순서 비용을 매긴다.** SS는 피해가 크지만 딜레이 800을 낸다. 즉
   "강한 한 방"과 "빠른 다음 턴"이 교환 관계다. castle-war에는 이 교환이 없다 —
   발사체가 라운드마다 자동 순환하므로(`ProjectileForTurn`) 플레이어가 고르지 않는다.
3. **조준 시간에 값이 붙는다** — 초당 10. 이건 아래 실력 절벽 절에서 다시 다룬다.
   오래 조준해 정확도를 올리는 것이 **공짜가 아닌** 구조다.

### 1.2 Worms Armageddon — **선공 회전을 실제로 출하한 유일한 게임**

> [!warning] 내 초판이 틀렸다 — 정정
> 초판에 *"선공 이점 보정 장치는 없다"*고 썼다. **틀렸다.**
> 하위 레인 `ArtilleryShippedRules`가 개발자 ReadMe 원문을 찾아냈다.
> W:A는 **랜덤 + 라운드마다 회전**이라는 이중 구조를 갖고 있다.

Team17, 1999-01-29(영국). 유지보수가 2020-12-23 `3.8.1`까지 이어진 장수 타이틀
(`worms2d.info` Worms Knowledge Base, `direct page retrieval`).

선공 결정은 **매치 시작 시 랜덤 → 라운드마다 증분(회전)**이다. 매뉴얼에는 없고
개발자(Deadcode) ReadMe에 있다:

> *"the choice is made depending on **which team gets the first turn (which is random at
> the beginning of a game, then incremented after each round)**. The legacy behaviour was
> that the special weapon would be determined always by the last team in the team slot
> list, and this stayed constant throughout a match"*
> — `worms2d.info/Worms_Armageddon_ReadMe_(English)/v3.6.19.14_Beta_Update`,
> `direct page retrieval` (하위 레인 확인)

**이게 결정적이다.** 매치 승리는 다수 라운드 선취(**기본 2승**, 설정 1~9)이므로
**라운드 간 회전이 실제로 균등화 효과를 낸다.** 확정 수치(전부 `direct page retrieval`,
`worms2d.info/Worms_Armageddon_manual/Create_Game`):

| 파라미터 | 값 |
|---|---|
| Round time 기본 | **10분** (이후 Sudden Death) |
| Victories required | 1~9, **기본 2** |
| Starting energy | 100 / 150 / 200, 기본 100 |
| **Handicapping** | 팀별 시작 체력 **±50%**, 로스터에 `+` / `-` 기호로 표시 |
| scheme `0x0E` Worm Select | 0=Sequential / 1=On / 2=Random — **팀 내 웜** 순서이며 팀 순서와 무관 |

그리고 이것이 **버린 사례**이기도 하다 — 방향이 우리와 반대다.
v3.6.19.14가 레거시(특수 무기 결정이 "team slot list의 마지막 팀"으로 **매치 전체 고정**)를
버리고 랜덤+회전 연동으로 교체했다. **Team17/Deadcode는 고정 순서 → 랜덤+회전 방향으로
움직였고 되돌리지 않았다.**

> **castle-war 대응물.** 우리는 `SiegeSeries`(2승 / 최대 3경기)를 갖고 있지만
> `GameManager.cs:1865`가 경기 번호를 읽지 않고 무조건 `isPlayerTurn = true`다.
> **즉 W:A가 출하한 "라운드 간 회전"의 자리가 우리에게 비어 있다.**
> 개막 감쇠는 경기 *안*을 고쳤고, 회전은 매치 *사이*를 고친다 — 다른 층위다.

> **주의 — Worms W.M.D는 반대 방향의 증거다.** 후속작 W.M.D가 배치 단계
> (Teleport In) *뒤에* 턴 순서를 무작위화했고, 커뮤니티는 이것을 **개선이 아니라
> 퇴보**로 받았다: 순서를 모르면 배치 전략을 세울 수 없다는 것. Team17이 이것을
> 되돌린 공식 패치노트는 없다. `indexed snippet` — Steam 커뮤니티 토론 다수,
> 원 스레드 본문을 직접 열지는 못했다.
> **함의: 무작위화는 선공 이점을 "공정"하게 만들지만 계획 가능성을 파괴한다.**
> castle-war의 개막 감쇠는 결정론적이므로 이 함정을 피했다.

### 1.3 Hedgewars — 랜덤 1회, 회전 없음, 보정 장치 없음 (확정)

오픈소스 Worms 계보. 팀 순서는 호스트 설정에 달렸다: `Random Team Order` 수정자가
**꺼져 있으면** 팀 목록 순서대로, **켜져 있으면** 무작위. 결정적 세부 — 이 무작위화는
**경기 시작 시 한 번만** 일어나고 이후 고정이다. **즉 W:A와 달리 라운드 간 회전이 없다.**

내가 직접 연 URL은 전부 실패했다(`/kb/RandomTeamOrder` 404, `/kb/Game_modifiers`
330바이트, `/node/1522` 무관한 포럼 스레드). **하위 레인이 올바른 경로를 찾아
`direct page retrieval`로 확정했다** — `hedgewars.org/kb/`는 기술 위키이고 턴 규칙은
`hedgewars.org/wiki/` 쪽에 있다. 이건 내 경로 선택 실수였다.

**선공 보정 장치는 없다.** (하위 레인 확정)

### 1.3b 출하 게임은 피해 규칙을 **UI로 알린다** — 우리 구현과 가장 날카로운 대비

이건 내가 찾으려던 것이 아니었는데 하위 레인이 발견했고, **이 조사 전체에서 가장
실행 가능한 발견**이다.

Hedgewars는 **피해 규칙을 바꾸는 수정자마다 전용 상시 HUD 아이콘**을 붙인다 —
시작 시 1회 공지가 아니라 **경기 내내 표시**된다.
`hedgewars.org/wiki/game_modifiers`, `/wiki/Status_icons`, `direct page retrieval`:

| 수정자 | 규칙 | 표시 |
|---|---|---|
| **Karma** | *"Attacking hedgehogs will receive the same amount of damage they deal"* | *"**When Karma is in effect, this icon will be visible:**"* + 전용 아이콘 |
| **Vampirism** | *"The current hedgehog gains **80%** of the damage it causes."* | 전용 effect 아이콘 |
| **Extra Damage** | 피해 배수 변경 | 우하단 바람 바 위 상태 표시 |
| Paramedics | 회복 규칙 | 체력 아이콘 자체가 변형 |

게다가 시작 시 전체 규칙 패널이 있고 **언제든 다시 볼 수 있다**:
> *"the mission panel shows the most important rules and contains vital information."*
> *"**The mission panel can be reviewed at any time by pressing the mission panel key (M).**"*

다른 두 게임도 같다:
- **Worms**: Handicap(시작 체력 ±50%)을 로스터에 `+` / `-` **기호로 표시**하고,
  *"If neither symbol is shown (default)"*로 기본값도 읽게 한다.
- **ShellShock**: 바람을 로비뿐 아니라 **서버 목록에까지** 표시 —
  v1.1 *"**Wind on Game Listing** — Games that have wind enabled now display a wind icon
  in the server list."*

> [!important] castle-war 판정
> **보이지 않는 피해 규칙을 아무 표시 없이 적용한 출하 사례를 찾지 못했다.**
> 확인된 3개 게임 전부가 규칙 변경을 (a) 매치 전 설정, (b) 시작 패널, (c) 상시 HUD
> 아이콘 중 최소 하나로 노출한다. Karma·Vampirism·Extra Damage는 우리
> `OpeningVolleyDamageScale`과 **같은 범주**(피해 수치를 조용히 바꾸는 규칙)이고,
> Hedgewars는 이들에게 **전용 상시 아이콘**을 줬다.
>
> 우리는 첫 발 피해를 절반으로 깎으면서 **아무것도 표시하지 않는다.**
> 플레이어가 그것을 *"빗맞았다"* 로 읽을 근거가 충분하다 — 그러면 조준 학습이
> 오염된다. 절벽 문제(조준 1%p = 14%p)와 직접 연결된다: **자기 조준을 잘못 평가하면
> 배울 수 없다.** `[INFERENCE — 출하 관행 대조]`

### 1.4 Scorched Earth — 1991년에 이미 순서를 옵션으로 노출했다

Wendell Hicken, 1991. 이 장르의 DOS 계보 원형. **선공 처리를 플레이어에게 위임한
가장 이른 사례**이고, 옵션 목록 자체가 설계 공간의 지도다.

| 옵션 | 동작 |
|---|---|
| Sequential | 기본. 한 명씩 조준·발사 |
| Random | 라운드마다 순서 무작위 |
| **Losers-first** | **최하위가 먼저, 승자가 마지막** — 명시적 컴백 보정 |
| Winners-first | 위의 역 |
| Round-robin | 순서는 무작위지만 라운드 첫 발사권이 순차 순환 |
| **Simultaneous** | 실시간. 전원 동시 이동·발사. 발사 후 **탄이 비행 중에도 조준 변경 가능** |
| **Synchronous** | 전원이 각도·힘을 독립 입력 → 전원 입력 완료 후 **동시 해결** |

`indexed snippet` — Wikipedia 항목(`direct page retrieval`)이 게임의 존재와 계보를
확인하지만, 위 옵션 표는 검색엔진이 SCORCH.DOC 매뉴얼과 여러 아카이브를 종합한 것이고
**나는 매뉴얼 원문을 열지 못했다.** Simultaneous의 "3인 이하 권장" 근거 문장도
원문 대조 실패다(공유 키보드 물리 충돌 때문이라는 설명은 `indexed snippet`).

**Losers-first가 중요하다.** 1991년 게임이 이미 "밀리는 쪽에 순서를 준다"는 컴백
보정을 옵션으로 갖고 있었다. castle-war의 LAST STAND와 같은 계열이다.

> Lane B가 별도로 보고한 것: **Scorched Earth v1.2(1992)가 "Synchronous firing mode"를
> 추가**했다. 즉 동시 발사는 나중에 붙은 기능이다. 나는 이 버전 정보를 독립 확인하지
> 못했으므로 **Lane B 인용으로 표기한다.**

### 1.5 ShellShock Live — 검색엔진이 준 "동시 턴 업데이트"는 **실재하지 않는다**

> [!warning] 환각 확정 — 되살리지 말 것
> 검색엔진은 2026-06-24 "Walk of Life (The Simultaneous Turns Update)"가 동시 턴 모드를
> 추가했다고 단언했다. **하위 레인이 두 경로로 교차 확인해 부정했다.**

반증 근거 2건 (하위 레인 `ArtilleryShippedRules`, 둘 다 `direct page retrieval`):

1. **Steam Web API** — 총 **266건**, 최신 항목이 **v1.1.1 / 2023-05-20**
   (`date: 1684613635`). 그 이후 공지 **0건**.
   `api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=326460&count=15`
2. **Steam 커뮤니티 allnews** — 최상단이 "v1.1.1 Released! / 20 May, 2023",
   역순으로 2022-02까지 내려간 뒤 "No more content."
   `steamcommunity.com/app/326460/allnews/`

**2023-05-20 이후 이 앱의 Steam 공지가 0건이다.** 2026년 6월 업데이트는 존재하지 않는다.

그리고 개발사가 정반대를 직접 말했다 (kChamp, 2023-05-04 `Announcing ShellShot Arena`,
`direct page retrieval`):

> *"**ShellShock Live is and has always been turn-based.** This style of game is much more
> straightforward to create because network latency isn't a problem."*

**로비 옵션 이름도 내가 쓴 것이 틀렸다.** `One-Shot / Team-Shot / All-Shot`이 아니라
**`Single / Team / All`**이다. 개발사가 스토어에서 매뉴얼로 지정한 공식 위키 본문
(`shellshocklive.fandom.com/wiki/Match_options`, `/Modes`, `direct page retrieval`):

> **Shot Type | Single, Team, All** — *"The number of players allowed to fire per round.
> Either only a single player, a whole team, or all players at once."*

**`All`이 사실상 동시 턴이며, 신규 기능이 아니라 최소 2023-03 이전부터 존재한다** —
v1.1(2023-03-31) 패치노트: *"Fortify no longer stalls your turn in **all-shot mode**"*.
v1.0(2020-05-22)에도 관련 항목이 있다(`'Shoot-Only' mode`). `direct page retrieval`

확정 수치(전부 `direct page retrieval`):

| 파라미터 | 값 |
|---|---|
| Turn Time | 20 / 30 / 40 / 50 / 60초. **3턴 연속 스킵되면 AFK로 판정해 킥** |
| Players / HP | 2~8 / 100~600 |
| **Wind** | None **0** / Low **20** / Med **50** / High **100** — *"Implements a random amount of shot drift per turn if enabled."* |
| Points·Shoccer 모드 Turns | 5 / 10 / 15 / 20 / 25 / 30 — 총 턴 수를 고정해 **발사 기회를 동수로 만든다** |
| **Level Field** mod | *"Forces all players to have the same weapons, items and tank upgrades as **the lowest player in the game**."* (XP 0.9x) — 실력·진행도 격차 직접 보정 |
| 무기 수 | 스토어 원문 *"over **400** unique power-packed weapons"* |

**선공 보정 장치는 없다.** Shot Type은 페이스·혼돈 조절이며 선공 이점 보정으로
문서화돼 있지 않다. 턴 순서 랜덤화·선공 회전 서술을 공식 위키·패치노트에서 찾지 못했다
→ `확인 불가`(부재 추정).

### 1.6 Rampart — 성·대포·성벽 소재에서 순서를 페이즈로 바꿨다

Atari Games, 1990. castle-war와 **소재가 사실상 동일**하다(성, 대포, 파괴되는 성벽).
그런데 턴 구조가 다르다: 라운드가 세 페이즈로 반복되고, 2~3인전에서 이 페이즈는
**모든 플레이어에게 동시에** 진행된다.

1. **Battle** — 트랙볼로 상대 성벽에 포격. 약 10초 고정.
2. **Build & Repair** — 전원 동시. Tetris형 벽 조각을 배치해 성을 둘러싼다.
   시간 내 성 하나를 완전히 못 감싸면 **탈락**.
3. **Place Cannons** — 감싼 성 수만큼 대포를 받아 배치.

`indexed snippet` — StrategyWiki·팬 사이트·아카이브를 종합한 결과이고, 나는 원문
페이지를 열지 않았다. 단 Lane B가 독립적으로 같은 구조를 보고했다(*"Gameplay alternates
between two time-limited phases: combat and building"*).

**함의: 순서를 없애는 방법이 "동시 발사"만은 아니다.** 페이즈를 공유하고 시간을
자원으로 만들면 순서 이점이 사라진다. castle-war는 턴당 0.9초(적) / 무제한(플레이어)
구조라 이 방향과 멀다.

### 1.7 Angry Birds / Crush the Castle — 선공 문제가 없는 구조

둘 다 **단독 플레이**다. 성을 부수지만 상대가 반격하지 않는다. 따라서 선공 개념이
성립하지 않는다. **이것도 발견으로 기록한다** — "성 파괴 게임"이라는 소재만으로는
선공 문제가 생기지 않고, **양측이 서로의 성을 부수며 경주할 때** 생긴다.
castle-war의 문제는 소재가 아니라 **대칭 경주 구조**에서 나온다.
`thin evidence` — 자명한 구조적 사실이라 별도 출처를 찾지 않았다.

---

## Common Workarounds

개발사가 장치를 넣지 않았을 때 커뮤니티·토너먼트가 무엇을 했는가.

### 2.1 Magic: The Gathering — 패자가 선공을 고른다 (MTR 2.2)

**가장 값나가는 사례다.** 규칙집 조항으로 명문화된 컴백 보정이다.

하위 레인 `CommunityWorkarounds`가 조항 번호까지 확보: **Magic Tournament Rules
2.2 "Play/Draw Rule"**. 1게임은 무작위(다이스/가위바위보 등)로 정하지만,
**2게임 이후에는 이전 게임의 패자가 선공/후공을 선택한다.**
`direct page retrieval` — 하위 레인 보고.

동시에 게임 규칙 쪽에는 선공 **비용**이 박혀 있다:
**Comprehensive Rules 103.8a** — *"In a two-player game, the player who plays first
skips the draw step of their first turn."* 2인전에만 적용되고 다인전(103.8c)에서는
스킵하지 않는다. `direct page retrieval` — Lane B 보고.

**두 층이 함께 작동한다.** 규칙층이 선공에게서 카드 정확히 1장을 뺀다(대칭·자동).
토너먼트층이 밀리는 쪽에 선택권을 준다(비대칭·컴백). castle-war 대응물:
개막 감쇠 = 규칙층(있음), 시리즈 내 선공 교대 = 토너먼트층(**없음** — `GameManager.cs:1865`가
경기 번호를 읽지 않고 무조건 `isPlayerTurn = true`).

> **castle-war는 이미 `SiegeSeries`(2승 / 최대 3경기)를 갖고 있다**(Lane B 보고,
> `SiegeSeries.cs:14-17`). MTG 형태를 적용할 자리가 이미 있다는 뜻이다.

### 2.2 바둑 — 색 결정 절차의 형식화 (nigiri)와 등급차 처리

동등 등급이면 komi로 위치 보정, 등급차가 있으면 **강자가 백을 잡고** komi를 0.5로
낮추거나 아예 없앤다. 핸디캡 대국은 사실상 komi 0.5 고정.
`direct page retrieval` — Sensei's Library "Komi":

> *"Komi typically applies only to games where both players are evenly ranked. In the case
> of a one-rank difference, the stronger player will typically play with the white stones
> and players often agree on a simple 0.5-point komi to break a tie (jigo) in favour of
> white, or no komi at all."*

**komi 값을 정하기 어렵다는 문제 자체를 커뮤니티가 우회한 방식**도 문서화돼 있다 —
**Auction komi**(입찰): 더 많은 점수를 내겠다는 쪽이 흑을 갖는다. 변형으로
*"One player chooses the komi for white, the other one chooses what color to play"*
(pie rule 계열). `direct page retrieval`, 같은 페이지.

이건 castle-war에 직접 쓸 수는 없지만 원리가 값나간다: **보정값을 설계자가 정하지
못하겠으면 플레이어에게 정하게 하라.**

### 2.3 FIDE — 흑 대국 수를 타이브레이크로 쓴다

체스는 선공(백) 이점을 대국 안에서 보정하지 않는다. 대신 **토너먼트 층위에서**
처리한다: 색을 교대 배정하고, 동점 시 **흑으로 둔 경기가 많은 쪽**을 위로 올린다.
하위 레인이 조항까지 확보: **FIDE C.04.1**, **C.07 7.3~7.4**.
`direct page retrieval` — 하위 레인 보고.

**함의: 한 경기 안에서 공정하게 만드는 것이 유일한 답이 아니다.** 매치·토너먼트
단위로 올려 해결할 수 있다. castle-war의 `SiegeSeries`가 그 층위다.

### 2.4 보드게임 일반 — 첫 플레이어 결정을 규칙으로 못 박기

하위 레인이 **BGA(Board Game Arena) Rules of Play 3·4**를 확보(`direct page retrieval`).
"누가 먼저"를 관습에 맡기지 않고 규칙집에 쓰는 관행 자체가 workaround다.

### 2.5 Worms 커뮤니티 — 스킴(scheme)으로 관리

Worms 계보는 선공 규칙을 손대는 대신 **스킴**(무기 배분·크레이트 확률·턴 시간 등의
설정 묶음)을 표준화해 경쟁 무대를 관리한다. 리그·에티켓 문서가 존재한다
(`worms2d.info/Leagues`, `/Etiquette`, `/Schemes`).
하위 레인 보고, `direct page retrieval`.

> [!important] **선공 관련 커뮤니티 규칙은 0건 — 이것은 조사 미완이 아니라 강한 부정 결과다**
> 하위 레인 `CommunityWorkarounds`가 4개 페이지 전문을 확인한 뒤 명시적으로 요청한
> 표현이다: **"안 만든 것이지 못 만든 것이 아니다."**
> Worms 커뮤니티는 선공 이점을 *관리 대상*으로 보지 않는다. 대신 규칙을 **코드로**
> 옮기는 파이프라인을 갖고 있다 — 관행이 굳으면 스킴 설정이나 게임 옵션이 된다
> (하위 레인 표현: *"the rule became coded"*).
>
> **castle-war 함의: 커뮤니티 관행에 맡길 수 있는 문제와 코드로 박아야 하는 문제가
> 다르다.** 우리는 싱글플레이어 대 AI이므로 **커뮤니티 층위가 존재하지 않는다.**
> Worms가 관행에 맡긴 것을 우리는 전부 코드로 결정해야 한다. `[INFERENCE]`

대신 Worms 계보가 커뮤니티 규칙으로 실제로 관리하는 것은 **화력**이다 —
Intermediate 경쟁판: *"**Mortar and Cluster Bomb power is reduced to 2 stars**"*,
Elite: Cluster/Mortar를 **1·2**로 고정, Shotgun 무제한→3발, Ninja Rope 5→2,
Round time **7분**(Intermediate 10분 대비). `direct page retrieval`
**단 이것은 (a) 커뮤니티 설정이고 (b) 첫 턴 한정이 아니라 전 턴 균일 하향이다** —
우리 개막 감쇠와 범주가 다르다.

---

## Pain Points With Current Solutions

채택된 장치가 만든 **새 문제**. 이 절이 이 레인의 핵심 산출이다.

### 3.1 바둑 komi — 값을 세 번 버렸고, 버릴 때마다 승률이 근거였다

**"채택했다가 버린" 사례 1.** 단일 장치가 아니라 **값의 폐기 연쇄**다.
`direct page retrieval` — Sensei's Library "Komi" / "History of Komi".

| 시기 | 값 | 왜 버렸는가 |
|---|---|---|
| ~1930년대 실험기 | 2 / 2.5 / 3 / 3.5 / 4 / 4.5 / 5 등 난립 | 정착 전 |
| 1940년대~ | **4.5** 표준 | *"Game results from the next two decades showed that 4.5 komi still favored black"* |
| 1955(Oza)~ | **5.5** | *"research found that 5.5 points was insufficient to compensate for White's disadvantage"* |
| 2002-09(일본) | **6.5** | 닛폰키인이 **1996~2001년 약 15,000국**을 조사, 흑 승률 **51.86%** → **이사회 표결로 변경** |
| 중국 | 5.5 → **7.5** | 면적 계산법에서는 점수차가 거의 항상 홀수라 **2점 단위로 뛴다** |
| AGA | 5.5 → **7.5** (2004-08 결정, 2005 발효) | 2004년 회의록 9항 |

프로 통계 원문: 5.5 komi로 둔 **12,607국**에서 흑 **6,701승(53.15%)** / 백
**5,906승(46.84%)**. 6.5에서는 흑 50.58%.

> **castle-war에 주는 가장 중요한 숫자.** 바둑은 **1.86%p 편향(51.86%)에서 규칙을
> 바꿨다.** 우리 밴드는 45~55%, 즉 **±5%p**다. 바둑 기준으로는 매우 느슨하다.
> 현재 고정 선공 47.0%는 밴드 안이지만 **50%에서 3%p 아래**다 — 바둑이라면 이미
> 조정 대상 크기다. `[INFERENCE — 기준 대조]`
>
> 그리고 폐기 연쇄가 말하는 것: **보정값은 한 번 정하고 끝나는 것이 아니다.**
> 메타(정석 연구)가 진화하면 같은 값이 다시 틀려진다. 우리 `0.5`도 발사체
> 순환·재질 티어·AI 곡선이 바뀌면 재측정 대상이다.

부수 발견: AI가 이 논쟁을 다시 열었다. KataGo 1.15.3 기준 영역 계산 완전 komi는 **7**,
영토 계산은 **6**으로 추정된다(빈 판 흑 승률 komi=5 → 62.5%, komi=6 → 52.8%,
komi=7 → 42.4%). 즉 **현행 6.5도 최적이 아닐 수 있다.** `direct page retrieval`

또한 komi의 크기 근거가 명시돼 있다 — **첫 수의 가치는 프로 판단 11~14점**이고
komi는 그 **절반**이다. *"It is then normal that the value of komi be equal to half the
value of a move in the opening."* 우리 장치와 비교하면 흥미롭다: 우리는 첫 발
가치의 **정확히 절반**을 뺐다(0.5 배수). 같은 논리 형태다. `[INFERENCE]`

### 3.2 Gunbound 딜레이 — 채택했다가 버린 사례 2 (후속작에서)

**GunboundM(2017-07 출시)이 딜레이 시스템을 버리고 고정 교대 턴으로 갔다.**

하위 레인 `GunboundDelayNumbers` 판정: 개발사 공식 문서 3곳에서 '딜레이' 스탯이
**구조적으로 소멸**했음을 확인. 단 **"삭제했다"는 개발자 명시 문장은 확인 불가.**
즉 부재는 확인됐고 의도 진술은 못 찾았다. 정직하게 그 등급으로 기록한다.

Wikipedia가 출시일과 계보를 확인한다(`direct page retrieval`): *"A spin-off mobile game
titled GunboundM was released in July 2017."*

버린 이유로 제시되는 것들 — 모바일 짧은 세션(5분 목표), 밸런싱 복잡도 감소(무기를
피해와 딜레이 **두 축**으로 잡아야 하는 부담 제거) — 은 전부 `indexed snippet`이며
**1차 출처를 찾지 못했다.** 추측으로 표시한다.

> **이것이 우리 판정에 주는 함의는 양방향이다.**
> (가) 순서를 플레이어 선택으로 만드는 정교한 장치는 **유지 비용이 크다** — 원작이
> 15년 쓴 시스템을 후속작이 버렸다.
> (나) 반대로 **castle-war의 개막 감쇠는 상수 하나**다. 유지 비용이 거의 없다.
> Gunbound의 폐기는 "보정하지 마라"가 아니라 **"보정을 규칙 전체로 만들지 마라"**
> 는 교훈이다. `[INFERENCE]`

### 3.3 Hearthstone The Coin — 채택 안 한 안 2종, 그리고 보정이 실력을 요구하는 함정

먼저 **정확한 분류**: 이건 "출시 후 철회"가 **아니다.** 알파/개발 단계에서 **채택하지
않은** 안이다. 내 기준(시도했다가 버린)에 부분적으로만 맞으므로 그렇게 표기한다.

폐기된 안 2종:
- **Avatar of the Coin** — 0마나 1/1 중립 하수인, `GAME_002` / dbfId **1733**,
  플레이버 *"You lost the coin flip, but gained a friend."* 알파 Patch 1.0.0.3140에
  추가된 뒤 **unused 처리**. `direct page retrieval` — hearthstone.wiki.gg
- **후공이 35 생명으로 시작** — 위키 본문 문장은 직접 확인됐다:
  *"A variety of approaches were considered and tested for this purpose, including Avatar
  of the Coin, and the second player starting with 35 Health. However, The Coin (as well
  as drawing an extra card) was ultimately selected to even the balance."*
  **그러나 위키가 건 각주는 Ben Brode 트윗(2015-03-12)이고 하위 레인이 그 트윗을 열지
  못했다.** → 위키 서술은 `direct page retrieval`, **근거는 `thin evidence`**.
  Blizzard 공식 발언 URL 확보 실패. **위키 재인용으로 표기한다.**
  (하위 레인이 위키 각주의 개발 슬라이드 파일도 직접 열어봤는데 수치표가 아니라
  단순 게임플레이 스크린샷이었다 — 수치 근거로 쓰면 안 된다.)

**The Coin이 만든 새 문제 — 이것이 우리에게 직접 걸린다.**

> *"good use of The Coin requires an additional degree of strategy, therefore offering a
> disadvantage for less experienced players."*
> — Lane A가 전달, hearthstone.fandom.com, `browser-rendered indexed snippet` (팬 위키 = 2차)

즉 **보정 장치가 쓰는 데 실력을 요구하면 초보에게 역효과다.** 후공 보정을 받았는데
그것을 쓸 줄 몰라서 더 진다.

| castle-war 장치 | 이 함정에 걸리는가 |
|---|---|
| 개막 발사 50% 감쇠 | **아니다.** 자동 적용, 플레이어 입력 0 |
| **LAST STAND** | **걸린다.** 수동 장전(R 키) + AI는 자동 장전 |

Lane A가 코드 근거까지 붙였다(`DynamicBattlefield.cs:705-708`,
`ComebackAsymmetryTests.cs:90-95`). **컴백 장치가 초보를 못 구할 수 있다.**

The Coin의 다른 알려진 부작용 — 0마나 주문이라는 이유만으로 주문 카운트/콤보 트리거를
공짜로 켜서 특정 덱을 과하게 강화하는 것 — 은 널리 논의되지만 **구체적 카드명 + 패치
조정 기록을 1차 출처로 확보하지 못했다.** `indexed snippet`, 미확정으로 남긴다.

### 3.4 TFT — 컴백 보정이 고의 패배를 만들고, Riot이 그것을 정식 빌드로 승격시켰다

**채택했다가 버린 사례 3.** 하위 레인 `AdoptedDeviceProblems`가 Riot 1차 출처 확보.

**캐러셀 제거** (patch 17.1, `direct page retrieval`):
> *"Replacing the Carousel, The Realm of the Gods offers you two gods to choose an offering
> from, then a less powerful, generic one from Pengu."*

Set 개요가 **왜 컴백 성분만은 남겼는지** 직접 적는다(`direct page retrieval`):
> *"Carousel might be on vacation this set, but that doesn't mean it's gone forever. One of
> the core parts of Carousel is the comeback mechanic of being able to make a choice earlier
> when you're lower in HP. This aspect is sticking around in The Realm of the Gods."*

**장치는 버렸지만 컴백 원리는 이식했다.** 이게 정확한 형태다.

그런데 더 값나가는 것이 같은 패치에 있다. **컴백 보정이 고의 패배 악용을 만들었고
Riot이 그것을 공식 인정하고 코드로 막았다**(`direct page retrieval`):
> *"...causing the fight to be over instantly which had significant impact for players
> looking to lose streak"* → 항복 시 **-99 체력 강제**.

그리고 Riot의 최종 대응이 역설적이다 — **고의 패배를 없애는 대신 정식 빌드로
승격시켰다**: Anima = *"our lose streak trait this set"*, Soraka = *"Feeling desperate
about your ability to pull off lose streaking?"* `direct page retrieval`

> **castle-war 함의.** 밀리는 쪽에 보상을 주는 장치는 **밀리는 것을 이득으로 만든다.**
> LAST STAND가 코어 35%에서 발동한다면, 플레이어가 코어를 일부러 깎는 경로가 있는지
> 확인해야 한다. Riot의 답이 둘 중 하나임을 기억할 것: **막거나(항복 -99), 정식
> 전략으로 인정하거나.** 방치는 답이 아니다. `[INFERENCE]`

### 3.5 첫 턴 약화 장치의 폐기 이력 — 0건 (Main 핵심 질문 직답)

하위 레인 `AbandonedMechanisms`의 판정. **이 레인이 답해야 했던 질문의 직답이다.**

**폐기 0건. 유지 6건. NFL은 오히려 4차례에 걸쳐 선공 억제를 강화했다.**

유지된 첫 턴 약화 장치:

| 장치 | 상태 |
|---|---|
| Yu-Gi-Oh Master Rule 3 (2014-03) 선공 draw 박탈 | 현행 유지 |
| Pokémon TCG 선공 공격 금지 (XY기) | 현행 유지 |
| Pokémon TCG 선공 Supporter 금지 (2020-02-21 토너먼트 적용) | 현행 유지 |
| MTG 선공 draw 스킵 (CR 103.8a) | 30년+ 유지 |
| Warhammer 40k 8판 Tactical Reserves 첫 배틀라운드 도착 금지 | matched play 유지 |
| NFL 오버타임 | 1974 sudden death → 2010/2012 modified → 2022 포스트시즌 양팀 공격 보장 → 2025 정규시즌 확대 — **계속 강화** |

폐기된 장치들의 사유는 **세 종류뿐**이고, 우리 장치는 셋 다 구조적으로 회피한다:

| 폐기 사유 | 사례 | 우리 장치와의 거리 |
|---|---|---|
| **복잡성** | IFAB ABBA 승부차기 순서 | 회피. 순서를 재배열하지 않고 첫 발사에 상수 0.5를 곱한다. 설명이 한 문장 |
| **계획 불가능한 무작위성** | Smash Bros. Brawl random tripping | 회피. 완전 결정론적. **단 이 사례는 실력 절벽 절에 직접 경고가 된다** |
| **단일 사건으로 승부 결정** | 테니스 9점 sudden-death 타이브레이크, NFL 순수 sudden death, ICC boundary countback | 무관. 승부 결정 방식을 안 바꾼다 |

**정직한 공백 하나.** 폐기 선례가 0건인 것은 안전 신호지만, **"첫 턴 피해를 곱셈
감쇠"하는 형태의 *채택* 선례도 찾지 못했다.** Lane B의 표본 19개 룰셋에서도 0건이다.
우리는 검증된 형태(자원 박탈 / 행동 제한)가 아닌 **새 형태**를 쓰고 있다.
→ 이것은 **"위험"이 아니라 "선례 없음"으로 기재한다.**

또한 **"보정이 과해서 후공이 유리해졌다"는 이유로 되돌린 사례는 확보한 9건 중 0건**이다.
즉 현재 47.0%(3%p 과보정)를 근거로 장치를 버린 선례는 없다. `[사례 부재의 진술 — 추측 아님]`

### 3.6 FIDE 규정에서 실제로 삭제된 것 — 선공 보정의 세부는 버려도 본체는 남는다

하위 레인이 FIDE 공식 핸드북 5개 버전을 기계 비교했다(`direct page retrieval`).

**삭제 1 — Median Buchholz / Median Buchholz 2.** pre-2023 조항 4.2·4.3에 존재했고
2023-09-01 발효 개정판에서 **완전 삭제**. 이후 042024 / 082024 / 032026 전 버전에서
0회 등장. 원문 언급 4회 → 0회(문자열 카운트로 확인).
**사유는 규정 원문에 없다** — FIDE는 개정판에 사유를 적지 않는다. `[확인 불가]`

**삭제 2 — 선공 보정과 직접 관련된 유일한 A급 사례.** 흑 대국 수 타이브레이크에
붙어 있던 세부 조항이 삭제됐다:
- pre-2023 8.1: *"The greater number of games played with the black pieces (unplayed games
  shall be counted as played with the white pieces)."*
- 2023 이후 7.3: *"Number of Games Played with Black (BPG) — The number of games played
  over the board with the black pieces."*

**상위 타이브레이크 자체(BPG, BWG)는 유지됐다.** 선공 보정 장치를 버린 게 아니라
그 **세부 처리**를 버렸다.

> **함의: 체스는 선공 보정을 40년 이상 유지하면서 세부 규칙만 정리했다.**
> castle-war 대응: 보정을 유지하되 **"어느 발사까지가 개막인가"를 명확히 정의하라.**
> 현재 `completedTurns <= 0`이므로 **정확히 첫 1발**이다. 이미 명확하다. ✅

---

## Sources

강도 라벨 정의: `direct page retrieval` = 페이지를 열어 본문 문장을 확인 /
`indexed snippet` = 검색엔진 요약만, 원문 미대조 / `browser-rendered indexed snippet` =
렌더링된 2차 페이지 / `feed recovery` = 피드 경유 / `thin evidence` = 근거 약함·미확정.

### 내가 직접 열어 확인한 것

| # | URL / 경로 | 강도 | 확인 내용 |
|---|---|---|---|
| 1 | `Assets/Scripts/OneShotSiegeRules.cs:20,27-39` | `direct page retrieval — 코드` | `OpeningVolleyDamageScale = 0.5f`; `OpeningVolleyDamageMultiplier(completedTurns) => completedTurns <= 0 ? 0.5f : 1f`. 주석이 의도를 직접 진술: *"Reducing only that opening volley to 50% removes the measured 87% first-mover win rate..."*. `ProjectileForTurn`이 라운드(2턴)마다 순환 → 발사체는 플레이어 선택이 아니다 |
| 2 | `_workspace/current/qa/gate-measurements.md:84-86` | `direct page retrieval — 저장소` | **조준 품질 0.01(1%p) = 승률 14.0%p.** 구간 기울기 0.00→0.01 **14.0**pp / 0.01→0.03 **13.5**pp / 0.03→0.05 **3.0**pp(포화). 밴드 근방 선형 기울기 **1,400pp / 조준 1.0 단위** |
| 3 | `Assets/Scripts/MatchLengthModel.cs:162-164, 318-325` | `direct page retrieval — 코드` | `damage = baseShotDamage * projectileMultiplier * openingVolleyMultiplier * aimQuality` — **피해가 조준 품질에 선형**. `fixedAimQuality` 0.70, `beginnerAimError` 0.09. 양 진영 공통 한 줄, 진영 구분은 `turns & 1`뿐 |
| 4 | `Assets/Scripts/SiegeDuelSimulation.cs:124-125, 213-215` | `direct page retrieval — 코드` | `alternateFirstMove: true, // isolate skill from turn order` — 실력 축과 순서 축을 분리해 재는 경로가 이미 구현돼 있다 |
| 5 | https://senseis.xmp.net/?Komi | `direct page retrieval` | komi 4.5→5.5→6.5 폐기 연쇄; 닛폰키인 1996~2001 **약 15,000국 흑 51.86%** → 이사회 표결로 6.5(2002-09); 5.5에서 **12,607국 흑 6,701(53.15%) / 백 5,906(46.84%)**; 6.5에서 흑 50.58%; 중국 5.5→7.5(면적 계산은 2점 단위); AGA 2004-08 결정 2005 발효; KataGo 완전 komi 추정 영토 6 / 영역 7(komi=5 흑 62.5%, 6 → 52.8%, 7 → 42.4%); 첫 수 가치 11~14점, komi는 그 절반; 등급차 시 강자가 백 + komi 0.5; **Auction komi / pie rule** |
| 6 | https://senseis.xmp.net/?HistoryOfKomi | `direct page retrieval` | GoGoD 17,000국 DB 기준 각 komi 값 최초 사용 연도(2 → 1935, 3 → 1852, 4.5 → 1934, 5.5 → 1955 Oza, 6.5 → 1984 일본 아마 / 1997 한국); 6.5 신뢰 가능한 최초는 **4th LG Cup**; AGA 변경은 2004 회의록 9항 |
| 7 | https://en.wikipedia.org/wiki/GunBound | `direct page retrieval` | *"Gunbound also implements a 'delay' turn system which is influenced by the Mobile, the weapon and/or item a player uses—using items or taking time with actions results in a longer wait before the player's next turn."* 한국 2002-05 / 월드와이드 2003-08; **GunboundM 2017-07**; New Gunbound는 2021-02 Steam에서 내려감; 모빌마다 delay 스탯이 다름 |
| 8 | https://en.wikipedia.org/wiki/Komi_(Go) | `direct page retrieval` | 일본·한국 6.5 / 중국·Ing·AGA 7.5 / 뉴질랜드 7; 1920년대 Hisekai가 4.5 사용; *"Statistical analyses of the year's games would sometimes appear in the Igo Nenkan"*; 흑 승률 53% 확인 |
| 9 | https://worms2d.info/Worms_Armageddon | `direct page retrieval` | Team17, 1999-01-29 영국 출시, 최신 **3.8.1 / 2020-12-23**, 유지보수자 Deadcode·CyberShadow. **턴 순서 규칙 하위 문서는 열지 못함**(`/Turn`, `/Game_logic` 404 또는 빈 응답) |
| 10 | https://store.steampowered.com/news/app/326460 | `direct page retrieval` **이지만 내용 없음** | Steam 전역 보일러플레이트만 반환, 뉴스 항목 0건. → ShellShock Live 동시 턴 모드 **확인 실패** |
| 11 | https://steamdb.info/app/326460/patchnotes/ | **HTTP 403** | 접근 차단 |
| 12 | https://gunbound.fandom.com/wiki/Delay 및 Special:Search | **404 / 결과 0건** | Gunbound 팬 위키에 Delay 문서 없음 |
| 13 | https://strategywiki.org/wiki/Gunbound | **HTTP 403** | 직접 접근 차단 → 하위 레인이 web.archive.org 경유로 우회 |
| 14 | https://www.hedgewars.org/kb/RandomTeamOrder, /kb/Game_modifiers, /node/1522 | **404 / 330B / 무관** | Hedgewars 공식 위키 본문 대조 **실패** |

### 하위 레인이 확인한 것 (내가 재확인하지 않음 — 그들의 강도 라벨을 그대로 인용)

| # | URL | 강도 | 확인 내용 | 출처 레인 |
|---|---|---|---|---|
| 15 | https://web.archive.org/web/20250521154129/https://strategywiki.org/wiki/Gunbound/Gameplay | `direct page retrieval` | 딜레이 전문. Shot1 250 / Shot2 300~480 / SS 800(Knight·Dragon 840); 초당 10, Turtle 12; Dual 600, Energy Up 2 300, Dual+ 250, Thunder 200, Change Wind·Power Up·Teleport 150, Energy Up 100, Team Teleport·Bunge Shot 50, Blood 0; 상대 표시값은 자기 기준 상대값; double turn 작동 예시(800−600−50=150)를 산술 재현으로 검증 | GunboundDelayNumbers |
| 16 | Softnyx 공식 문서 3곳 (URL은 하위 레인 산출물) | 구조적 부재 확인 / **의도 진술 확인 불가** | GunboundM에서 '딜레이' 스탯이 소멸. "삭제했다"는 개발자 문장은 못 찾음 | GunboundDelayNumbers |
| 17 | https://hearthstone.wiki.gg/wiki/Avatar_of_the_Coin | `direct page retrieval` | 0마나 1/1 중립, `GAME_002` / dbfId **1733**, 플레이버 *"You lost the coin flip, but gained a friend."*, 알파 Patch 1.0.0.3140 추가 후 unused | AdoptedDeviceProblems |
| 18 | hearthstone.wiki.gg / hearthstone.fandom.com — The Coin | 위키 본문 `direct page retrieval` / **근거 각주 `thin evidence`** | *"A variety of approaches were considered and tested ... including Avatar of the Coin, and the second player starting with 35 Health. However, The Coin (as well as drawing an extra card) was ultimately selected"*. **각주는 Brode 트윗(2015-03-12)이며 열지 못했다. Blizzard 공식 URL 확보 실패 → 위키 재인용** | AdoptedDeviceProblems / LaneAContext |
| 19 | hearthstone.fandom.com — The Coin | `browser-rendered indexed snippet` (팬 위키 = 2차) | *"good use of The Coin requires an additional degree of strategy, therefore offering a disadvantage for less experienced players."* | LaneAContext |
| 20 | Riot TFT patch 17.1 + Set 개요 | `direct page retrieval` | 캐러셀 제거: *"Replacing the Carousel, The Realm of the Gods offers you two gods..."* / 컴백 성분 이식: *"...the comeback mechanic of being able to make a choice earlier when you're lower in HP. This aspect is sticking around..."* / 고의 패배 악용 인정 + 항복 시 **-99 체력**: *"...causing the fight to be over instantly which had significant impact for players looking to lose streak"* / 고의 패배를 정식화: Anima *"our lose streak trait this set"*, Soraka *"Feeling desperate about your ability to pull off lose streaking?"* | AdoptedDeviceProblems |
| 21 | https://handbook.fide.com/chapter/TieBreakRegulationsPre2023 · /TieBreakRegulations2023 · /TieBreakRegulations032026 | `direct page retrieval` (5개 버전 기계 비교) | Median Buchholz(4.2)·Median Buchholz 2(4.3) 2023-09-01 발효판에서 **완전 삭제**(언급 4회 → 0회). 미실시 경기 백 간주 조항 삭제: pre-2023 8.1 *"(unplayed games shall be counted as played with the white pieces)"* → 2023 이후 7.3은 *"games played over the board with the black pieces"*. **상위 타이브레이크 BPG·BWG는 유지.** 삭제 사유는 규정 원문에 없음 | AbandonedMechanisms |
| 22 | FIDE C.04.1, C.07 7.3~7.4 | `direct page retrieval` | 색 교대 배정 + 흑 대국 수 타이브레이크 | CommunityWorkarounds |
| 23 | Magic Tournament Rules **2.2 Play/Draw Rule** | `direct page retrieval` | 2게임 이후 **이전 게임 패자가 선공/후공 선택** | CommunityWorkarounds |
| 24 | MTG Comprehensive Rules **103.8a** | `direct page retrieval` | *"In a two-player game, the player who plays first skips the draw step of their first turn."* 다인전(103.8c)은 스킵 없음 | Lane B |
| 25 | Board Game Arena Rules of Play 3·4 | `direct page retrieval` | 첫 플레이어 결정을 규칙집에 명문화 | CommunityWorkarounds |
| 26 | worms2d.info /Leagues, /Etiquette, /Schemes | `direct page retrieval` | 스킴 표준화로 경쟁 무대 관리. **선공 이점 상쇄 규칙은 확인되지 않음** | CommunityWorkarounds |
| 27 | SmashWiki — tripping | 제거 사실 `direct page retrieval` / **도입 의도 `indexed snippet`** | Brawl(2008) random tripping: 대시 입력 **1%**, 달리기 중 방향전환 **1.25%**, 발동 후 **10초** 유예, 저마찰 지형은 traction으로 나눠 상승(1%/0.2 = **5%**), Brawl에서는 끌 수 없음. **SSB4(2014)에서 제거, Ultimate에도 부재.** 기술 유발 forced tripping은 유지. **Sakurai 인터뷰 1차 원문 미확보** | AbandonedMechanisms |
| 28 | NFL 규칙 변경 이력 | `direct page retrieval` (Lane B 병행 확인) | 2022 포스트시즌 오버타임 양팀 공격 보장, 2025 정규시즌 확대. 계기: 선공 팀 터치다운으로 후공이 공을 못 잡은 경기(Super Bowl LI) | AbandonedMechanisms / Lane B |
| 29 | https://worms2d.info/Worms_Armageddon_ReadMe_(English)/v3.6.19.14_Beta_Update | `direct page retrieval` | **W:A 선공 회전 확정** — *"which team gets the first turn (**which is random at the beginning of a game, then incremented after each round**). The legacy behaviour was that the special weapon would be determined always by the last team in the team slot list, and this stayed constant throughout a match"*. 동맹 그룹 선공 버그 수정도 같은 릴리스 | ArtilleryShippedRules |
| 30 | https://worms2d.info/Worms_Armageddon_manual/Create_Game · /Introduction | `direct page retrieval` | Round time 기본 **10분**; Victories required 1~9 **기본 2**; Starting energy 100/150/200; **Handicapping ±50%**를 로스터 `+`/`-` 기호로 표시(*"If neither symbol is shown (default)"*); Reinforcements(게임 내 **"Delay"**) 슬라이더 OFF~1-9 라운드 | ArtilleryShippedRules |
| 31 | https://worms2d.info/Game_scheme_file | `direct page retrieval` | 바이트 단위: `0x1B` Turn Time 0~127초 / `0x1C` Round Time / `0x0E` Worm Select(0=Sequential,1=On,2=Random, **팀 내** 순서) / `0x12E` Wind 기본 100 / **`0x130` Wind Bias 기본 15** / `0x144` Circular Aim / **`0x145` Anti-Lock Aim — 표준은 랜덤 값으로 리셋** / `0x146` Anti-Lock Power / `0x18D` RubberWorm Anti-Lock Aim(0도 리셋). **기본 전부 off**. 무기당 Delay 1바이트, `0x80~0xFF`는 *"unlimited delay, 'blocking' it"* | ArtilleryShippedRules |
| 32 | https://worms2d.info/Sudden_Death | `direct page retrieval` | *"Sudden Death **always occurs between turns, never during a turn**"* / *"the precise timing... is **determined randomly**"*. 옵션: Round ends / Nuclear / **전 웜 HP 1** / 무피해. 수위 상승 0=0px, 1=5px, 2=20px, 3=45px per turn |
| 33 | https://api.steampowered.com/ISteamNews/GetNewsForApp/v2/?appid=326460&count=15 · https://steamcommunity.com/app/326460/allnews/ | `direct page retrieval` (2경로 교차) | **ShellShock 2026-06 동시 턴 업데이트 반증.** 총 266건, 최신 **v1.1.1 / 2023-05-20** (`date: 1684613635`), 이후 공지 **0건**, allnews 최하단 "No more content." | ArtilleryShippedRules |
| 34 | ShellShock `Announcing ShellShot Arena` (2023-05-04, kChamp) | `direct page retrieval` | *"**ShellShock Live is and has always been turn-based.** This style of game is much more straightforward to create because network latency isn't a problem."* | ArtilleryShippedRules |
| 35 | ShellShock v1.1 Sneak Peek (2023-03-28, kChamp) | `direct page retrieval` | *"using any software to **aim-assist** or xp-farm is against the SSL Terms of Service and can result in an account ban. We're going to be more strict about this moving forward."* | ArtilleryShippedRules |
| 36 | https://shellshocklive.fandom.com/wiki/Match_options · /Modes (개발사가 스토어에서 매뉴얼로 지정) | `direct page retrieval` | 실제 라벨은 **Single / Team / All** (`One-Shot/Team-Shot/All-Shot` 아님); Turn Time 20~60초 + **3턴 연속 스킵 시 AFK 킥**; Wind None 0 / Low 20 / Med 50 / High 100 *"random amount of shot drift per turn"*; Turns 5~30; **Level Field** *"same weapons, items and tank upgrades as the lowest player"*; Max Lvl Diff 10/20/40/Any; **Atmospheric Nudge** *"Shots fired at higher power will have less accuracy... **typically only used to counteract ruler cheats**"*; **Shot Tracer** *"the path of your **last** shot taken"*; Explosion Radius 수치 공개 | ArtilleryShippedRules |
| 37 | ShellShock v1.0(2020-05-22) · v1.1(2023-03-31) 패치노트 | `direct page retrieval` | *"Fortify no longer stalls your turn in **all-shot mode**"*(v1.1) → 동시 발사는 **최소 2023-03 이전 기능**. *"**Ally Aim Visibility** — Teammate aim details are now fully visible... even visible after your tank has been destroyed"*(v1.0). *"**Wind on Game Listing** — Games that have wind enabled now display a wind icon in the server list"*(v1.1) | ArtilleryShippedRules |
| 38 | https://store.steampowered.com/app/326460/ShellShock_Live/ | `direct page retrieval` | *"over **400** unique power-packed weapons"* (100+가 아님) | ArtilleryShippedRules |
| 39 | https://www.hedgewars.org/wiki/game_modifiers · /wiki/Status_icons · /wiki/mission_panel · /wiki/Rules_of_the_game | `direct page retrieval` | **피해 규칙에 상시 HUD 아이콘.** Karma *"Attacking hedgehogs will receive the same amount of damage they deal... **When Karma is in effect, this icon will be visible**"*; Vampirism *"gains **80%** of the damage it causes"*; effect 아이콘 목록에 **Extra Damage**; *"**The mission panel can be reviewed at any time by pressing the mission panel key (M).**"*; Laser sight 상시 수정자 승격 *"permanently activated for the active hedgehog, helping with aiming"* | ArtilleryShippedRules |
| 40 | https://worms2d.info/Laser_Sight | `direct page retrieval` | 대상 무기 **6종 한정**(Shotgun, Handgun, Uzi, Minigun, Longbow + Kamikaze). *"**Aiming without Laser Sight requires more aiming skills**, because of that, the utility might be considered less fun for some players and may not appear in many schemes."* | ArtilleryShippedRules |
| 41 | Worms Knowledge Base — 바람 / 무조준 무기 / 유도 무기 | `direct page retrieval` | 바람 **21단계 양자화** *"there are in fact only 21 distinct values"*, 세기 중력 대비 11.9~119.0%; **Grenade/Mortar/Cluster/Banana/HHG는 바람 무관**; 50턴 측정 — 중앙 웜 우20/좌26/무풍4, 최우측 웜 우10/**좌38**/무풍2 + *"leftward... hits maximum strength 20 times... Rightward wind... **never once reached maximum strength**"*; 무조준 무기 **정확히 14종**(65 슬롯의 최소 22%); 유도 무기는 자동 추적 아님 — *"The attraction is **not particularly strong**... will begin orbiting the target in an ellipse"*, *"**It's a good crutch for when you don't have enough skill to hit the target with a Bazooka**"*; Team17이 조준 생략 글리치를 v3.7.2.1/v3.8에서 **좁혔다** | ArtilleryShippedRules |
| 42 | Hedgewars 공식 Weapons Manual | `direct page retrieval` | 무기 **58종**, 행 단위로 무조준 계열 묶임(행6 커서 폭격, 행4 접촉, 행7 지형, 행8 이동). Homing Bee: Damage 50 / lock 1s / flight 5s / **`Affected by wind: No`** / *"**Don't shoot with full power to improve its precision**"* / *"not very smart... Try to practice"* | ArtilleryShippedRules |
| 43 | https://worms2d.info/Intermediate · /Elite | `direct page retrieval` | 경쟁 스킴의 파워 하향 — Intermediate *"**Mortar and Cluster Bomb power is reduced to 2 stars**"*; Elite는 Cluster/Mortar를 **1·2**로 고정, Shotgun 무제한→3발, Ninja Rope 5→2, Round time **7분**. **단 커뮤니티 설정이고 첫 턴 한정이 아니라 전 턴 균일** | ArtilleryShippedRules |
| 44 | https://namu.wiki/w/건바운드 | `direct page retrieval` | 기체별 **탄 분산** — 에세트 *"돌풍 안에서 레이저가 분산되기 때문에 계산을 두 번 해야"*, 트리코 *"돌풍벽 통과시 분산되는 약점"*, 부머런처 *"바람의 영향을 잘 받는 편"*, 바람 위성 *"방향과 세기가 계속 바뀌어서 플레이를 방해"*. **주의: 이 문서에 딜레이 메커니즘 절이 없다(2026-06-28 판)** — 바람 근거로만 사용 | GunboundDelayNumbers |
| 45 | https://web.archive.org/web/20250514043701/https://strategywiki.org/wiki/Gunbound/Controls | `direct page retrieval` | **원작 PC에 조준 보조 없음** — 컨트롤 목록이 마우스/방향키/스페이스뿐, 가이드 토글 부재. drag style / slice style 2종 | GunboundDelayNumbers |
| 46 | https://store.steampowered.com/app/991710/GunboundM/ | `direct page retrieval` | *"The game system directly shows the player the flight guidelines... **However, this guideline is for windless conditions.** Players need to predict the wind well"* — **결정론은 자동화, 확률은 플레이어 몫** | GunboundDelayNumbers |
| 47 | https://dargomstudio.com/index.php/gbm-tanks/ · /gbm-gamesystem/ | `direct page retrieval` | *"Accuracy — All tanks have a slightly different launch angle error... A higher value reduces this error. **\* This figure has nothing to do with the effect of wind on the bomb.**"* — **발사 오차와 바람을 분리된 두 채널로 관리**. *"When the **wind speed is 10 or more**, it is judged for a 'Sky Shot'"* — 바람은 정수 스케일 | GunboundDelayNumbers |
| 48 | https://dargomstudio.com/index.php/gbm-avatars/ | `direct page retrieval` | **아바타 스킬 40여 종에 조준 보조 0개.** 폭발 반경 확대 5종(Land Destruction 30%→+8%, Big Bomb 20%→+7%, Shovel of hero 아군2기 파괴→+14%, Burning Night, DestructionTimer). **러버밴드 12종+** — **Overcome** *"If the [Attack Power] I did to enemies last turn is **less than 20%** of my [Max HP], [Attack Power] is Increased by **16%**"*, **DestructionTimer** *"When you **receive more than 5 turns**, your [Explosion Range] increases by **14%**"*, Wrath(HP<30%→+16.5%), Wrath of last(HP<15%→+19.5%), Weak Point Defense, Hero, Restore of hero, SS Addiction, Emergency Restore, Counterattack, Second Recovery, Leadership | GunboundDelayNumbers |
| 49 | https://dargomstudio.com/index.php/gbm-battleanalysis/ | `direct page retrieval` (개발사 게시 실측) | 밸런싱 규칙 *"If there are too many players using this tank, and the tank's win rate is too high, its weapon and attack power will be nerfed."* ProBattle 2025-07-23 Season 98 League≥10: Mage **37.25%**(1635), Ice 50.72%(2902), Turtle 51.85%(2185), NakMachine 60.15%(1561), Boomer **65.69%**(819), DarkNak(2:2) **66.88%**. → 현역 상용 게임 실측 폭 **37~67%** | GunboundDelayNumbers |
| 50 | GunboundM 패치노트 2026-08-10 · 2026-07-10 | `direct page retrieval` | `TxBigfoot` *"Weapon#1 TxBomb's AttackPower **-5% on the 2nd turn**, AttackPower **+10% after 2 turns**"* → **개막 억제가 아니라 후반 증폭**. 그리고 조작 편의 **추가**: *"The player can immediately load the controls from the last shot."* | GunboundDelayNumbers |
| 51 | https://www.ssbwiki.com/Tripping | `direct page retrieval` | 제거 이유 원문 *"The randomness of Brawl's tripping mechanic was generally negatively received, viewed as **counterproductive to the idea of a skill-based match**..."* / *"forced tripping... is generally **better-received**, since, unlike random tripping, **its deliberate use can be planned for and planned against**."* | AbandonedMechanisms |
| 52 | IFAB 133차 **ABM** (2018-11-22 글래스고) | `direct page retrieval` | ABBA 승부차기 순서 폐기 — *"due to a lack of strong support, mainly because of its complexity"*. **주의: 133차 AGM(2019-03-02 애버딘)은 다른 회의이며 ABBA 언급 0건** — 혼동하면 틀린 인용 | AbandonedMechanisms |
| 53 | Worms `Scales of Justice` (WKB) | `direct page retrieval` | 전 팀 총 체력 균등 분배 출하 컴백 장치. 수치 공개(400+200=600 → 각 300; 4웜 각 75 / 2웜 각 150). *"always **round down**... Worms can be reduced to **0 HP**... killing them"*, *"**Not useful if you are in the lead.**"* **자동 발동이 아니라 플레이어가 쓰는 무기이고 효과가 체력 숫자로 즉시 보인다 — 무표시 자동 보정과 반대 설계** | ArtilleryShippedRules |
| 54 | Fortnite Legacy 컨트롤러 조준 보조 제거 (2020-03-06 공지 / 2020-03-24 핫픽스) | `indexed snippet` | 조준 버튼 연타 시 조준선이 대상에 반복 스냅, 건축물·연무 관통 추적. **Epic이 버린 것은 보조 자체가 아니라 스냅 방식이고 Linear/Exponential 곡선으로 교체.** **Epic 공식 patch note 원문 회수 실패 — 등급 올리지 않음** | AbandonedMechanisms |

### 미확정·확인 실패로 남기는 것 (없는 것을 있다고 쓰지 않기 위해 명시)

| 항목 | 상태 | 왜 |
|---|---|---|
| ShellShock Live 2026-06 "Walk of Life" 동시 턴 모드 | **해소 — 실재하지 않음으로 확정** | 하위 레인이 Steam Web API + allnews 2경로로 반증. 2023-05-20 이후 공지 0건. **환각 표로 이동 대상** |
| Worms Armageddon 턴 순서 규칙 | **해소 — 회전 확정** | 하위 레인이 Deadcode ReadMe 원문 확보. 내 초판의 "보정 없음"은 오류였고 §1.2에서 정정 |
| Hedgewars Random Team Order | **해소** | 하위 레인이 `hedgewars.org/wiki/`(내가 시도한 `/kb/`가 아님)에서 확정 |
| Scorched Earth SCORCH.DOC 옵션 표 원문 | `indexed snippet` — **미해소** | 매뉴얼 원문 미확보. 1.5 매뉴얼 HTML이 `Totally Scorched` zip 내부라 문장 직접 인용 불가. "3인 이하 권장" 근거 문장도 미대조. Sequential/Synchronous/Simultaneous 3분류와 "공정성 목적" 진술이 여기 걸려 있다 |
| Scorched Earth v1.2 Synchronous 추가 시점 | **Lane B + ArtilleryShippedRules 병행 보고** | 내가 독립 확인하지 않음. 두 레인이 같은 값을 보고해 신뢰도는 올랐으나 등급은 올리지 않는다 |
| Worms W.M.D 순서 무작위화 커뮤니티 반발 | `indexed snippet` | Steam 스레드 본문 미열람. **Team17이 되돌린 패치노트는 없음** |
| Rampart 페이즈 구조 세부 | `indexed snippet` | 원문 페이지 미열람 (Lane B가 독립 보고) |
| GunboundM 딜레이 삭제 **의도** | **확인 불가** | 구조적 부재는 confirmed(탱크 스탯 9종에 Delay 없음, 아이템 15종 딜레이 0, 비용이 공격력 −30%로 교체, 6개월 패치노트 0건). **개발자 명시 문장 없음.** 검색엔진은 단언하나 인용 URL이 전부 프록시라 검증 불가 → 채택하지 않음 |
| The Coin이 특정 카드/덱을 과강화한 구체 사례 | `indexed snippet` | 카드명 + 패치 조정 기록 1차 출처 미확보 |
| Advance Wars: Days of Ruin CO Power 축소 의도 | **채택하지 않음** | 개발자 1차 출처 확보 실패 (AbandonedMechanisms 판정) |
| Pocket Tanks 선공 결정 / volley별 교대 | **확인 불가** | 공식 사이트에 서술 없음. 게임 내 도움말 또는 Deluxe 매뉴얼 필요. **단 구조적 등가 보정은 확인** — 10 volley 동수 발사 + 점수제 |
| Tank Wars(1990) / Artillery-3·WAR3(BASIC) 선공 | `thin evidence` | 좌→우 고정 순서가 2차 출처뿐. atariarchives.org *More BASIC Computer Games* 본문 미확인 |
| W:A 기본 Turn Time 초 값 | **확인 불가** | 매뉴얼이 Round time 기본(10분)은 명시하나 Turn time 기본 수치는 명시하지 않음. Intermediate `.wsc` 파싱하면 확정 가능 |
| ShellShock 턴 순서 랜덤화 여부 | **확인 불가**(부재 추정) | 공식 위키·패치노트에 서술 없음 |
| 무기 다양성이 조준 실력 비중을 낮추는 **정량값** | **확인 불가** | 어느 게임도 공개하지 않음. 구조의 존재는 확정, 크기는 미지 |
| Gunbound 원작 바람 수치 범위 / 사거리 비례 | **확인 불가** | GunboundM은 "wind speed 10 or more"로 정수 스케일 확인. 원작 범위와 사거리 누적은 not found. 고각 보너스가 "2.5초 이상 체공" 조건인 점은 정황상 일관되나 `[INFERENCE]`로만 |
| 조준 보조를 넣었다 **버린** 포격 장르 사례 | **부재** | 3개 게임 전부 유지 또는 확대. 근거 부재이며 반증 아님 |
| "첫 턴 피해 감쇠"의 **채택** 선례 | **부재** | Lane B 19개 룰셋 0건과 일치. **위험이 아니라 선례 없음** |

### 환각으로 판정해 폐기한 항목 (되살리지 말 것)

| 폐기 항목 | 이유 |
|---|---|
| Ben Brode "알파 선공 승률 격차 20%" | 출처 URL **404**. 하위 레인이 폐기 판정. **어느 레인 문서에도 넣지 말 것** |
| Atlas Reactor Will Cook 동시 턴 관련 인용 | 2차 검색이 스스로 철회. **환각** |
| Pokémon TCG "선공 공격 금지에서 벗어난 이유" | **전제가 성립하지 않는다.** Pokémon은 그 규칙을 벗어난 적이 없고 제약을 **누적**했다(공격 금지 위에 Supporter 금지 추가). Main이 후보로 준 항목이지만 사실이 아니다 |
| Jonathan Chey의 Into the Breach 참여 | 사실 아님. 개발은 Justin Ma·Matthew Davis (FTL 듀오) |
| **ShellShock Live "Walk of Life / Simultaneous Turns Update" (2026-06-24)** | **존재하지 않는다.** 검색엔진이 날짜·업데이트명·기능까지 붙여 단언했으나 Steam Web API(266건, 최신 v1.1.1 / 2023-05-20)와 allnews 2경로가 반증. 개발사는 *"is and has always been turn-based"*라고 직접 말했다. **이 조사에서 가장 그럴듯했던 환각** |
| ShellShock 로비 옵션 `One-Shot / Team-Shot / All-Shot` | 실제 라벨은 **`Single / Team / All`**. 내 초판이 과제문의 표현을 검증 없이 옮겼다 |
| ShellShock 무기 "100+" | 스토어 원문은 *"over **400**"* |
| Worms 유도 무기가 자동 추적한다는 서술 | 틀렸다. *"The attraction is **not particularly strong**... will begin orbiting the target in an ellipse"*, *"Judging the path... is initially very difficult"* |
| Worms 바람이 조준 실력을 희석하는 랜덤 노이즈라는 내 초판 서술 | 틀렸다. **21단계 양자화**(암기 가능) + 영향 무기 소수(회피 가능)이고 실제 목적은 **위치 우위 상쇄**다. §실력절벽 §2에서 정정 |
| TUS 선공 관행 / Gunbound "No First Turn" 규칙 / 니기리 절차 상세 | 하위 레인 `CommunityWorkarounds`가 폐기 판정 — **URL 0건 무출처 생성 요약** |
| FIDE Armageddon 백5분/흑4분 시간 배분 | 타이브레이크 규정 5개 버전 전체에서 해당 문구 **0회**. 인용하지 않는다 |
| IFAB 133차 **AGM**(2019-03-02 애버딘)을 ABBA 폐기 회의로 인용 | **틀린 회의다.** ABBA 폐기는 133차 **ABM**(2018-11-22 글래스고). AGM 전문을 렌더링했을 때 ABBA 언급 0건 |

---

## 실력 절벽 — 추가 조사

> Main 지시(2026-08-14): 조준 품질 +0.01(1%p)이 승률을 **53.0% → 67.0%, 14%p** 움직인다.
> +0.03에서 94%, +0.05에서 100%. 이 레인 담당: **실제 출하된 게임이 이 곡선을 어떻게
> 다뤘는가**, Gunbound의 딜레이·바람과 Worms의 무기 다양성이 순수 조준 실력의 비중을
> 낮추는 구조인지, 그리고 **조준 보조를 넣었다가 버린 사례**.

### 0. 먼저 — 절벽의 원인을 코드에서 확인했다

`MatchLengthModel.cs:162-164, 318-325` (`direct page retrieval — 코드`):

```
damage = baseShotDamage × projectileMultiplier × openingVolleyMultiplier × aimQuality
```

**피해가 조준 품질에 선형이다.** 명중/빗맞음 판정이 없고, 분산이 없고, 하한이 없다.
조준 품질 1%는 그대로 피해 1%가 되고, 그 피해가 고정 HP 풀을 향해 경주하므로
**19턴에 걸쳐 복리로 누적된다.** 그래서 1%p가 14%p가 된다.

측정된 기울기(`qa/gate-measurements.md:84-86`, `direct page retrieval — 저장소`):

| 구간 | 승률 변화 |
|---|---|
| 0.00 → 0.01 | **14.0**pp |
| 0.01 → 0.03 | **13.5**pp |
| 0.03 → 0.05 | **3.0**pp (포화) |

밴드 근방 선형 기울기 **1,400pp / 조준 1.0 단위**. `fixedAimQuality` 0.70,
`beginnerAimError` 0.09 — **초보 오차 0.09는 이 기울기에서 승률 100%~0%를 오간다.**

**이것이 이 절의 핵심 대조점이다.** 아래에서 확인하는 출하 게임들은 하나도
"피해 = 조준 품질 × 상수" 구조가 아니다. 전부 **명중 판정 + 폭발 반경**이라는
비선형을 갖는다. 반경은 조준 오차를 **흡수**한다 — 조금 빗나가도 부분 점수가 나온다.
우리 모델에는 그 흡수층이 없다. `[INFERENCE — 구조 대조]`

### 1. 조준 보조를 넣었다가 버린 사례 — 포격 장르에서 0건, 반대로 확대되는 방향

**세 게임 전부 조준 보조를 유지하거나 확대했다.** 하위 레인 `ArtilleryShippedRules`
확정(전부 `direct page retrieval`):

| 게임 | 조준 보조 | 방향 |
|---|---|---|
| **Worms** | Laser Sight — 수집형 유틸리티, **대상 무기 6종 한정**(Shotgun, Handgun, Uzi, Minigun, Longbow + Kamikaze) | 1999~현재 **유지**. 스킴에서 끌 수 있게 하는 방식 |
| **Hedgewars** | Laser sight를 **상시 수정자로 승격** — *"permanently activated for the active hedgehog, helping with aiming. Laser sight is removed from the weapons set."* | **확대** |
| **ShellShock** | Shot Tracer, Ally Aim Visibility | **계속 추가** |

Worms 위키가 Laser Sight가 왜 널리 안 쓰이는지까지 적어둔다:
> *"**Aiming without Laser Sight requires more aiming skills**, because of that, the utility
> might be considered less fun for some players and may not appear in many schemes."*

**→ 조준 보조를 넣었다 버린 포격 장르 사례: `확인 불가`(부재).**

Gunbound 계보도 같다(하위 레인 `GunboundDelayNumbers`, `direct page retrieval`):
- **원작 PC에 조준 보조 없음** — 공식 컨트롤 목록이 마우스/방향키/스페이스뿐,
  가이드 토글이 목록에 없다. 입력 방식은 drag style / slice style 2종.
- **GunboundM은 추가했다. 그리고 의도적으로 불완전하다:**
  > *"The game system directly shows the player the flight guidelines as the bombs are fired
  > and flew. **However, this guideline is for windless conditions.** Players need to predict
  > the wind well and adjust the firing direction and distance to hit the enemy accurately."*

  **결정론적 부분(포물선 수학)은 자동화하고 확률적 부분(바람 예측)은 플레이어에게 남긴다.**
  이것이 이 계보가 출하한 해법이다.
- **뺀 이력 없음.** 반대로 2026-07-10에 *"The player can immediately load the controls from
  the last shot"*로 조작 편의를 **더했다.**

장르 밖 유일한 폐기 사례 — **Fortnite Legacy 컨트롤러 조준 보조**(2020-03 제거).
조준 버튼 연타로 조준선이 대상에 반복 스냅되어 건축물·연무를 관통 추적했다.
2020-03-06 공지, 2020-03-24 핫픽스로 제거. **단 Epic이 버린 것은 보조 자체가 아니라
스냅 방식이고, Linear/Exponential 곡선으로 교체했다.**
`indexed snippet` — 하위 레인이 Epic 공식 patch note 원문 회수 실패. 등급 그대로 유지.

> **함의: "보조를 없애라"가 아니라 "스냅형 보조는 실력 표현을 파괴하고 곡선형은
> 살아남는다"**다. 우리가 조준 보조를 넣는다면 *"맞춰준다"*가 아니라
> *"조준 난이도 곡선을 완만하게 한다"* 형태여야 한다. `[INFERENCE]`

### 2. 바람은 절벽을 완화하지 않는다 — 이 조사에서 가장 값나가는 반증

**내 초판 §1.1이 바람을 "노이즈로 조준 실력 비중을 낮추는 축"으로 썼다. Worms에 대해서는
틀렸다.** 하위 레인이 수치로 반증했다(`direct page retrieval`).

W:A 바람은 연속 랜덤이 아니라 **21단계 양자화**다 — *"there are in fact only 21 distinct
values"* (방향별 10 + 무풍 1). 각 단계 세기가 중력 대비
**11.9 / 23.8 / 35.7 / … / 119.0%**로 위키 표에 공개돼 있다.
**21단계면 암기 가능하다** — 즉 숙련자에게 유리한 축이다.

게다가 **영향받는 무기가 소수다.** Bazooka·독가스·화염계·MB Bomb·Mail Strike·Parachute뿐이고
**Grenade / Mortar / Cluster / Banana / Holy Hand Grenade는 바람 무관**이라
**회피 가능하다.** Hedgewars Homing Bee는 명시적으로 `Affected by wind: No`.

그리고 **바람의 실제 설계 목적이 밝혀졌다 — 절벽 완화가 아니라 위치 우위 상쇄다.**
위키에 50턴 측정 데이터가 있다:

| 웜 위치 | 우 / 좌 / 무풍 | 해석 |
|---|---|---|
| 맵 중앙 | 20 / 26 / 4 | 평균 0 (Deadcode 확인) |
| **최우측 픽셀** | 10 / **38** / 2 | *"leftward... hits maximum strength 20 times... Rightward wind... **never once reached maximum strength**, or even half-maximum"* |

scheme `0x130` **Wind Bias 기본 15** — *"The higher the value, the more likely worms on the
right side of the map will get leftwards wind, and vice versa."*
**바람은 지형·위치 우위를 상쇄하는 장치이고, 조준 실력을 희석하는 장치가 아니다.**

> **castle-war 판정: 절벽 완화를 원하면 바람은 잘못된 도구다.**
> castle-war에도 바람이 있고 거리에 따라 세지는데, 그것은 (a) 위치 보정으로는 일하고
> (b) 절벽에는 듣지 않으며 (c) 양자화돼 있으면 오히려 숙련자 이점이 된다.

**단 Gunbound는 다르다.** 같은 "바람"이라는 이름으로 다른 일을 한다 —
하위 레인이 나무위키 본문에서 기체별 **탄 분산**을 확인했다(`direct page retrieval`):
- 에세트: *"돌풍 안에서 레이저가 분산되기 때문에 **계산을 두 번 해야** 해서 화력이 아주 약해진다"*
- 트리코 2번 무기: *"**돌풍벽 통과시 분산되는 약점**이 있다"*
- 바람 위성: *"**바람의 방향과 세기가 계속 바뀌어서** 플레이를 방해한다"*

**Gunbound의 돌풍은 탄을 분산시킨다 — 즉 명중을 확률화한다.** Worms의 바람(궤적을
예측 가능하게 휘게 함)과 범주가 다르다. GunboundM은 여기에 더해
**발사 각도 오차를 기체 스탯으로 상수화**했고, 각주로 바람과 명시적으로 분리했다:
> *"Accuracy — All tanks have a slightly different launch angle error when firing a bomb...
> A higher value reduces this error. **\* This figure has nothing to do with the effect of
> wind on the bomb.**"*

**두 노이즈 채널(발사 오차 / 바람)을 분리해 관리한다.** 우리 모델은 둘 다 없다.

### 3. 딜레이는 절벽 완화 레버로 약하다 — 수치로 반증됨

내 §1.1 point 3이 *"조준 시간에 값이 붙는다"*를 시사점으로 적었다.
**하위 레인이 크기를 재서 그것이 약한 레버임을 보였다.**

초당 10 딜레이는 *"오래 조준하면 다음 턴이 늦어진다"*를 그대로 구현한 것이지만,
**20초 풀사용 vs 1초 드래그의 격차가 190이고, S1 한 턴(250)의 0.73턴에 불과하다.**
반면 SS(800) 또는 Dual(600) 한 번은 상대에게 **3연속 턴**을 준다.

> **즉 Gunbound의 딜레이는 "느린 조준"을 처벌하지 않는다. "나쁜 자원 선택"을 처벌한다.**
> castle-war가 조준 시간에 비용을 붙여 절벽을 깎으려 한다면, **원작조차 0.73턴이었다는
> 것이 반례다.** (하위 레인 계산, StrategyWiki 수치 기반)

### 4. 실제로 출하된 절벽 완화 장치 — 두 갈래로 수렴한다

이 계보가 조준 절벽에 대응하는 방식은 조준 보조가 **아니다.** 둘이다.

#### (가) 폭발 반경 = 미스 용서 반경

| 게임 | 장치 |
|---|---|
| **ShellShock** | 무기별 Explosion Radius를 수치로 공개 (Shot 10 / 20PU, Splitter 20, Sniper는 Scaling Distance). 반경 큰 무기가 조준 오차를 흡수 |
| **GunboundM** | **Explosion Range를 기체 스탯으로 승격**. 아바타 스킬 5종이 추가 확대 — Land Destruction(30% 확률 +8%), Big Bomb(20% 확률 +7%), Shovel of hero(아군 2기 파괴 시 +14%), Burning Night, DestructionTimer |
| **원작 Gunbound** | Bunge Shot (딜레이 50) — *"25% more area damage"* = 근접 미스 용서 |

#### (나) 부진·열세 감지 러버밴드

**GunboundM 아바타 스킬 40여 종에 조준 보조는 0개인데, 러버밴드는 12종 이상이다.**
(`dargomstudio.com/index.php/gbm-avatars/`, `direct page retrieval`)

가장 직접적인 둘:

| 스킬 | 효과 | 트리거 |
|---|---|---|
| **Overcome (Lv3)** | *"If the [Attack Power] I did to enemies last turn is **less than 20%** of my [Max HP], [Attack Power] is Increased by **16%**"* | **직전 턴에 못 맞혔으면 다음 턴 강화** |
| **DestructionTimer (Lv3)** | *"When you **receive more than 5 turns**, your [Explosion Range] increases by **14%**"* | **턴을 많이 헌납했으면 명중 반경 확대** |

나머지는 HP 문턱(Wrath 30% → +16.5%, Wrath of last 15% → +19.5%,
Weak Point Defense 30% → 방어 +27%, Second Recovery 20% → HP +16.5%),
아군 전멸(Hero +14%, Restore of hero HP 19%, SS Addiction +2.0),
피격량(Emergency Restore 35% → HP 14%, Counterattack 27% → 피해 +14%),
팀 열세(Leadership HP 40% 미만 → 아군 전체 공격력 +7%).

> **castle-war 대응: LAST STAND가 이 계열이다. 그런데 트리거가 하나(코어 HP)뿐이다.**
> GunboundM은 **4가지 축**으로 세분화했다 — HP / 아군 전멸 / **직전 턴 성과** / **받은 턴 수**.
> 절벽 문제에 직접 듣는 것은 뒤의 둘이다: **"못 맞혔음"을 감지하는 트리거.**
> 우리에게 그것이 없다. `[INFERENCE — 트리거 축 대조]`

#### (다) 조준 자체를 우회하는 무기 — Worms 14종, 확정 열거

하위 레인이 WKB의 메커니즘 기반 열거 목록을 확보했다(자체 분류가 아님):
Homing Missile, Homing Pigeon, Patsy's Magic Bullet, Air Strike, Napalm Strike,
Mail Strike, Mine Strike, Mole Squadron, MB Bomb, French Sheep Strike,
Mike's Carpet Bomb, Concrete Donkey, Teleport, Girder — **정확히 14종**,
무기 패널 65 슬롯 중 **최소 22%**.

**단 중요한 정정: 유도 무기는 자동 추적이 아니다.**
> *"The attraction is **not particularly strong**... will begin orbiting the target in an
> ellipse"* / *"Judging the path... is initially very difficult"*

그리고 **절벽에 직결되는 원문 표현이 위키에 그대로 있다:**
> *"**It's a good crutch for when you don't have enough skill to hit the target with a
> Bazooka**"*

**즉 실력 요구를 0으로 만들지 않고 더 관대한 축으로 옮긴다.** 이것이 정확한 형태다.
(Team17은 조준 생략 글리치를 v3.7.2.1 / v3.8에서 오히려 **좁혔다** — pre-targeting만 허용.)

Hedgewars도 같은 구조다 — 공식 Weapons Manual 58종이 행 단위로 무조준 계열을 묶는다
(행6 전부 커서 폭격, 행4 접촉, 행7 지형도구, 행8 이동). Homing Bee 수치 공개:
Damage 50 / lock 1s / flight 5s / *"**Don't shoot with full power to improve its
precision**"* / *"not very smart... Try to practice"*.

> **단 정직하게: "무기 다양성이 조준 실력 비중을 정량적으로 얼마나 낮추는가"는
> 어느 1차 출처에도 수치가 없다.** → `확인 불가`. 구조의 존재는 확정, 크기는 미지.

#### (라) 다중 발사로 분산을 평균화

ShellShock 무기 다수가 투사체 여러 발(UZI 10, M4 8, Counter 3000 10) —
한 발의 조준 오차가 결과를 지배하지 않게 한다. `direct page retrieval`
원작 Gunbound의 Dual(600) / Dual+(250)도 같은 원리(시행 2회).

#### (마) 실력·진행도 격차를 직접 보정

- **ShellShock `Level Field`**: *"Forces all players to have the same weapons, items and
  tank upgrades as **the lowest player in the game**"* (XP 0.9x).
  추가로 `Max Lvl Diff` 10/20/40/Any로 **로비 입장 자체를 실력대로 제한**.
- **W:A Handicapping**: 팀별 시작 체력 **±50%**, 로스터에 `+`/`-` 표시.

### 5. 무작위성으로 절벽을 깎으려는 안에 대한 정면 반례

**Super Smash Bros. Brawl random tripping** (2008 도입 → 2014 SSB4에서 제거).
하위 레인 `AbandonedMechanisms` 확정.

작동: 대시 입력 시 **1%**, 달리기 중 방향전환 시 **1.25%** 확률로 넘어져 무방비.
발동 후 **10초** 유예. 저마찰 지형(얼음)은 확률을 traction으로 나눠 상승(1% / 0.2 = **5%**).
**Brawl에서는 끌 수 없었다.**

도입 의도는 실력 격차 축소 및 고수의 최적화 반복 이동 억제 —
`indexed snippet`, **Sakurai 인터뷰 1차 원문 미확보** (등급 올리지 않음).

제거 이유는 원문 확보(`ssbwiki.com/Tripping`, `direct page retrieval`):
> *"The randomness of Brawl's tripping mechanic was generally negatively received, viewed as
> **counterproductive to the idea of a skill-based match** with a minimum of disruptive,
> chance-based elements."*
> *"forced tripping by certain moves is generally **better-received**, since, unlike random
> tripping, **its deliberate use can be planned for and planned against**."*

> **판정: 절벽 완화용으로 "발사당 분산"을 넣는 안은 이 선례에 정면으로 걸린다.**
> **단 핵심은 무작위성 자체가 아니다** — 살아남은 것(forced tripping)과 버려진 것
> (random tripping)의 차이는 **예측·대비 가능성**이다.
>
> 따라서 절벽 완화 장치는 (a) 플레이어가 **사전에 관측**할 수 있고 (b) **대비 행동이
> 존재**해야 한다. **바람처럼 발사 전에 표시되는 외란은 이 조건을 만족하고, 발사 후
> 무작위로 굴리는 분산은 만족하지 않는다.**

반대 방향의 출하 사례도 있다 — **W:A는 조준을 *더 어렵게* 만드는 옵션을 출하했다**
(scheme 확장, **기본 전부 off**, `direct page retrieval`):

| 오프셋 | 옵션 | 원문 |
|---|---|---|
| 0x144 | Circular Aim | *"to make aiming at specific angles more challenging"* |
| **0x145** | **Anti-Lock Aim** | *"**aim is reset between turns**, to make repeat shots more challenging"* — **표준 변종은 랜덤 값으로 리셋** |
| 0x146 | Anti-Lock Power | *"Intended to make full-power shots more challenging"* |
| 0x18D | RubberWorm Anti-Lock Aim | *"aiming angle is reset to zero degrees after each shot (**the standard anti-lock aim resets to a random value**)"* |

**즉 "조준각에 턴당 노이즈"는 실제 출하된 옵션이다** — Lane B 카탈로그의 "발사당 분산"에
해당하는 실물. 단 **방향이 우리와 반대**(실력 요구를 높이는 쪽)이고 기본값은 off다.

### 6. 정확도를 파워의 함수로 만든 출하 장치 — 의도를 오해하지 말 것

**ShellShock Live `Atmospheric Nudge`** (`direct page retrieval`):
> *"Shots fired at higher power will have less accuracy when enabled.
> **This is typically only used to counteract ruler cheats.**"*

메커니즘은 우리 절벽을 정확히 겨냥한다 — 고출력 발사에 분산을 넣어 **조준 정밀도의
수익률을 떨어뜨린다.** 그러나 **설계 목적은 화면에 자를 대고 각도를 재는 치트 무력화였다.**
**우연히 그 효과를 낸 장치이고 절벽 완화를 목표로 설계된 것이 아니다.** 이 구분을 지우면 안 된다.

같은 개발자가 조준 보조를 **치트로 규정**했다(kChamp, 2023-03-28, `direct page retrieval`):
> *"using any software to aim-assist or xp-farm is against the SSL Terms of Service and can
> result in an account ban. We're going to be more strict about this moving forward."*

**ShellShock의 입장은 일관된다: 조준 실력의 비중을 낮추려는 게 아니라, 조준 실력을
외부 도구로 살 수 없게 만드는 방향이다.**

### 7. "배우는 구간이 없다"에 대응하는 출하 패턴 — 피드백형 보조

Main이 지적한 (가) *배우는 구간이 없다*에 정확히 대응하는 출하 사례가 있다.
**ShellShock의 조준 보조 3종은 전부 예측형이 아니라 피드백·공유형이다**
(하위 레인 확정, `direct page retrieval`):

| 장치 | 원문 | 성격 |
|---|---|---|
| **Shot Tracer** | *"Show the path of your **last** shot taken"* / *"Gives players a dashed line indicating the path of their **last** shot"* | **지난 발사만** — 다음 발사를 맞춰주지 않는다 |
| Ally Aim Visibility | *"Teammate aim details are now fully visible... even visible after your tank has been destroyed"* | 팀 내 공유 |
| Atmospheric Nudge | (위 §6) | 역방향 |

> **이것이 이 조사에서 castle-war에 가장 직접 적용 가능한 발견이다.**
> Shot Tracer는 **실력을 대신 주지 않고 빗맞은 이유만 보여준다.**
> 절벽 문제의 (가) 절반 — *"못 맞히면 계속 지고 맞히면 계속 이긴다"* — 은
> **피드백 부재 문제**이고, 그것은 조준 보조 없이 고칠 수 있다.
>
> 그리고 우리 저장소가 같은 공백을 이미 기록해두었다:
> `Telemetry.Volley(unit, power, angle, wind)`는 **입력만** 남기고 결과-대-의도를
> 남기지 않아 **조준 품질 필드가 스키마에 없다**(`cycle-2-retrospective.md` §5 F-3,
> Lane A 경유 확인). **플레이어에게 보여줄 데이터가 애초에 수집되지 않고 있다.**

### 8. 참조점 — 현역 상용 게임의 실측 승률 밴드

GunboundM 개발사가 실측 승률과 밸런싱 규칙을 공개한다
(`dargomstudio.com/index.php/gbm-battleanalysis/`, `direct page retrieval`).

명시된 규칙:
> *"1. If this tank has too few players, its weapon or attack power will be upgraded."*
> *"2. If there are too many players using this tank, and the tank's win rate is too high,
> its weapon and attack power will be nerfed."*

ProBattle 실측 (2025-07-23, Season 98, League≥10):

| 기체 | 사용률 | 승률 |
|---|---|---|
| Mage | 2.91% (1635) | **37.25%** |
| Ice | 5.16% (2902) | 50.72% |
| Turtle | 3.88% (2185) | 51.85% |
| NakMachine | 2.78% (1561) | 60.15% |
| Boomer | 1.46% (819) | **65.69%** |
| DarkNak (Score 2:2) | — | **66.88%** |

> **40여 기체를 운영하는 현역 상용 게임의 실측 승률 폭이 37~67%다.**
> 우리 45~55% 단일 밴드는 이 장르 현역 기준보다 **훨씬 엄격한 자기 규율**이다.
> **단 이는 기체별 축이며 castle-war의 선공/후공 또는 과금/무과금 격차와 같은 축이
> 아니다** — 밴드 폭의 참조점으로만 쓸 것. (하위 레인 경고를 그대로 옮긴다.)
>
> 대조: 바둑은 **1.86%p**(51.86%)에서 규칙을 바꿨다(§3.1). 즉 장르·경기 유형에 따라
> 허용 편향이 **두 자릿수 배로** 다르다. 우리 ±5%p는 그 사이에 있다.

### 9. 첫 턴 약화 장치의 폐기 이력 — 우리 보정의 판정 근거 (재확인)

§3.5에서 이미 다뤘고, 실력 절벽 관점에서 하나만 덧붙인다.

**castle-war의 `OpeningVolleyDamageScale = 0.5`는 피해 축 보정이다.
선공 이점(템포)에는 들었지만 조준 절벽에는 원리적으로 듣지 않는다** —
두 문제는 같은 축에 있지 않다. 실제로 측정이 그것을 보여준다:
**개막 감쇠 이후에도 조준 +0.01이 14%p를 움직인다.**
`[INFERENCE — Lane A가 같은 결론에 독립 도달]`

그리고 이 계보에 **개막만 약화하는 선례가 없다**는 것을 하위 레인이 재확인했다.
가장 가까운 것조차 방향이 반대다 — GunboundM `TxBigfoot`:
> *"Weapon#1 TxBomb's AttackPower **-5% on the 2nd turn**, AttackPower **+10% after 2
> turns**"* (2026-08-10)

원작 서든데스도 같은 방향(일정 턴 후 화력 증폭으로 종결).
**즉 이 장르의 지배적 관행은 개막 억제가 아니라 후반 증폭으로 종결이다.**

### 10. 이 절의 결론 5줄

1. **절벽의 원인은 `damage = … × aimQuality` 선형성이다.** 출하 게임은 하나도 이 구조가
   아니고, 전부 **명중 판정 + 폭발 반경**이라는 흡수층을 갖는다. 우리에게 그것이 없다.
2. **조준 보조를 넣었다 버린 포격 장르 사례는 0건.** 세 게임 전부 유지·확대다.
   장르 밖 Fortnite도 보조를 버린 게 아니라 **스냅 → 곡선으로 교체**했다.
3. **바람은 절벽에 듣지 않는다.** W:A 바람은 21단계 양자화(암기 가능) + 영향 무기 소수
   (회피 가능)이고, 실제 목적은 **위치 우위 상쇄**다. 단 Gunbound의 돌풍은 탄을
   **분산**시켜 명중을 확률화하므로 범주가 다르다.
4. **출하된 실효 대응은 둘이다** — (가) **폭발 반경 = 미스 용서 반경**,
   (나) **부진 감지 러버밴드**. GunboundM은 후자를 HP·아군 전멸·**직전 턴 성과**·
   **받은 턴 수** 4축으로 세분화했다. 우리 LAST STAND는 트리거가 HP 하나뿐이다.
5. **"배우는 구간"은 조준 보조 없이 고칠 수 있다** — ShellShock `Shot Tracer`가
   출하 증거다(지난 발사 경로만 표시). 그런데 우리는 `Telemetry.Volley`가 입력만
   남겨 **보여줄 데이터 자체가 없다**. 그리고 개막 감쇠를 **무표시로** 적용해
   플레이어가 그것을 "빗맞았다"로 오독할 근거를 만들었다(§1.3b) — **절벽 완화의
   선행 조건은 계측과 표시다.**


---

## 부록 — 하위 레인 `ArtilleryShippedRules` 회수분

> 부모 레인(LaneCActual)이 최종 보고 단계에서 퇴화해 이 부록 3개가 본문에 병합되지
> 못했다. 하위 레인의 `'/Users/seokcmin/.jeopi/agent/sessions/-Desktop-castle-war/2026-08-13T12-23-46-615Z_019ffb14-1ab7-7000-aacc-4f32a490dac3/local/lanec-artillery.md'`에서 원문 그대로 회수한 것이다.
> 세 부록은 (1) 바람과 조준 오차를 한 필드로 합치면 안 되는 이유, (2) AI 난이도는
> 경기 전 저장된 값이라는 출하 근거, (3) CPU 레벨이 다축이라는 확인이다.

# 부록 — 바람과 조준 오차를 한 필드로 합치면 안 되는 이유 (출하 근거)

맥락: `GunboundDelayNumbers`가 "조준 오차와 바람을 한 float으로 합치지 말라"는 지적을 냈고, `SkillGradingTests`가 그것이 TelemetrySink 계약이라 자기 파일 밖이라고 넘겼다. 해당 에이전트가 종료되어 전달 실패했으므로 근거를 여기 남긴다. 내 레인(출하 사례)에 직접 걸리는 질문이다. Gunbound는 형제 레인 담당이라 미조사 — 아래는 전부 Worms/Hedgewars/ShellShock 1차 출처다.

## 출하 게임에서 바람은 조준 오차와 **다른 종류의 양**이다 — 네 가지 성질

### 1. 이산이다 (연속 float이 아니다)
> "Although it may appear as though the wind indicator can display a continuous spectrum of lengths, **there are in fact only 21 distinct values** that it will take (10 for each direction and 1 for no wind at all)."

각 단계 세기(바주카에 가해지는 힘을 중력의 %로, "Figures are exact, rounded to the nearest 0.1%"):
**11.9 / 23.8 / 35.7 / 47.6 / 59.5 / 71.4 / 83.3 / 95.2 / 107.1 / 119.0 %** — 방향별 10단계. 표시 바 길이도 픽셀 단위로 표에 명시(좌: 7/14/22/29/37/45/52/60/67/75px, 우: 6/14/21/29/36/44/52/59/67/74px).

→ 조준 오차는 사수의 연속 스칼라, 바람은 이산 열거형. 한 float에 합치면 이산성이 소실된다.

### 2. 독립 노이즈가 아니라 **위치의 함수**다
> "The direction and strength of the wind generated for a given turn is **influenced by the position of the active worm** at the start of that turn (this has been both confirmed by **Deadcode and CyberShadow** but details remain secret, and settings added to adjust this bias in update 3.8)."

50턴 측정(1920px 맵, 고정 위치 단독 웜):
- 맵 중앙: 우향 20 / 좌향 26 / 무풍 4 — "statistically consistent with the average wind being zero in this position, and that is indeed the case, **as confirmed by Deadcode**"
- 맵 최우측 픽셀: 우향 10 / **좌향 38** / 무풍 2 — "Not only is leftward wind more frequent, **but also stronger. In fact, it hits maximum strength 20 times**... Rightward wind... **in this test never once reached maximum strength, or even half-maximum.**"

scheme 노출값: `0x130` Wind Bias 기본 **15**.

→ 바람은 **위치와 상관된** 양이다. 조준 오차와 합치면 이 상관을 사후 분리할 수 없다.

### 3. **탄종 조건부**다 (조준 오차는 무조건부)
바람이 영향을 주는 것만 열거된다: Bazooka / Suicide Bomber·Skunk의 독가스 / Flamethrower·Petrol Bomb·Napalm·Oil Drum·모든 크레이트의 화염 / MB Bomb / Mail Strike / Parachute.

**Grenade · Mortar · Cluster Bomb · Banana Bomb · Holy Hand Grenade는 목록에 없다 = 바람 무관.** Hedgewars Homing Bee 속성표: `Affected by wind: No`.

→ 조준 오차는 무엇을 쏘든 붙지만 바람은 탄종에 따라 정확히 0이다. 한 float이면 이 조건부를 표현할 수 없다.

### 4. 턴 간 **자기상관이 음(-)**이다 (독립 난수도 아니다)
> "The wind will **almost never be the same for two consecutive turns**. If the wind were completely random, then for 50 consecutive turns, there would be a 91% chance that the wind would be the same for two consecutive turns on at least one occasion. Since this did not happen in the test illustrated below, it can be said with **at least 91% certainty that the game's code prevents it** from happening."

1000턴 초과 테스트에서 연속 동일 발생 1회.

→ 코드가 연속 동일값을 능동적으로 회피한다. i.i.d. 난수 하나로 모델링할 수 없다.

## 노출 방식도 다르다

바람은 **전용 UI로 별도 표시**된다 — W:A 바람 지시바(21단계, 픽셀 길이까지 결정적), ShellShock은 로비 설정값(None 0/Low 20/Med 50/High 100) + v1.1에서 서버 목록 아이콘까지 추가("Games that have wind enabled now display a wind icon in the server list"). 조준 오차는 어느 게임에서도 플레이어에게 표시되지 않는다.

→ 출하 관행상 두 양은 **플레이어에게 다른 가시성**을 갖는다. 하나로 합치면 "바람은 보여주고 실력 오차는 숨긴다"는 구분 자체가 불가능해진다.

- URL: https://worms2d.info/Wind , https://worms2d.info/Game_scheme_file , https://www.hedgewars.org/wiki/Homing_Bee , https://shellshocklive.fandom.com/wiki/Modes — 전부 `direct page retrieval`

## 결론

두 양을 한 float으로 합치면 (a) 이산/연속, (b) 위치 상관 유무, (c) 탄종 조건부, (d) 턴 간 자기상관 부호 — 네 가지가 전부 소실되고, **빗맞음을 "실력" vs "환경"으로 귀속시킬 수 없다.** `LaneBSolutions`가 핸디캡과 난이도 램프가 같은 필드를 움직여 명중률만으로 분리 불가라고 낸 지적과 동일한 성질이다. `lastAiAimError`를 분리 필드로 유지하는 현재 형태가 이 관점에서 맞다.

## 부수 확인 — "매치 상수는 곱, 턴 변동은 가산"이 출하 관행이다

`SkillGradingTests`가 고정한 계약(EffectiveAiAimError가 램프에 **더한다**, 곱하지 않는다)은 출하 사례와 부합한다:
- **매치 전 1회 고정되는 보정은 곱** — Worms Handicapping: 시작 체력 **±50%**(로스터에 +/− 기호로 표시).
- **턴마다 움직이는 노이즈는 가산 힘** — W:A 바람은 바주카에 가해지는 **힘을 중력의 %로** 더한다(11.9~119.0%). 배수가 아니라 가속도 항이다.

→ 곱셈으로 바꾸는 뮤테이션을 테스트가 잡아야 한다는 판단이 출하 관행 쪽에서도 지지된다.

## 부록 보충 — AI 난이도는 출하 게임에서 **경기 전 저장된 값**이다 (경기 중 재계산 아님)

맥락: Main이 "치석 등급을 매 AI 턴에 `TelemetrySink.PlayerShots/PlayerHits`로 재계산하므로 경기 중 등급이 바뀔 수 있다"는 결함을 스스로 발견하고 매치 시작 1회 고정으로 수정하겠다고 알려왔다. 그 결정을 지지하는 출하 근거를 확인했다.

**Worms Armageddon 팀 파일(.WGT) 포맷 — CPU 난이도가 디스크에 1바이트로 저장된다:**
> `| 1 | byte | **Control** Determines who gets to contol the team. Values: **0x00 = player, 0x01 to 0x05 = CPU level 1 to 5.** |`

- URL: https://worms2d.info/Team_file — `direct page retrieval`

세 가지가 따라온다:

1. **경기 전 저작(authored)이며 런타임 파생이 아니다.** 팀 이름·웜 이름·사운드뱅크와 같은 레코드에 들어 있고 팀 선택 시 로드된다. 경기 중 플레이어 성적으로 다시 계산되는 경로가 포맷에 없다.
2. **이산 5단계다**(0x01~0x05). 연속값이 아니므로 경기 중 미세하게 흘러갈 수 없다. 매뉴얼도 "you can also change the skill level of the team from EASY through to DIFFICULT"로 사용자 선택 항목임을 명시(https://worms2d.info/Worms_Armageddon_manual/Create_Game — `direct page retrieval`).
3. **누적 성적은 별도로, 그리고 난이도에 자동 반영되지 않는다.** 같은 팀 레코드가 Team Win/Loss/Draw Count, Team Kills, Team Deaths, Deathmatch 별도 집계, Team Deathmatch Rank를 각각 저장한다. 즉 Worms는 (a) 저장된 난이도 설정과 (b) 저장된 누적 통계를 **둘 다** 갖지만, (b)가 (a)를 자동으로 움직이지 않는다.

→ Main이 가려는 형태(세션 누적 표본으로 **매치 시작에 결정**, 그 경기 동안 동결)가 출하 아키텍처와 일치한다. Worms는 난이도를 매치 경계에서만 바뀌는 양으로 취급한다.

ShellShock도 같은 경계다: `Max Lvl Diff`(10/20/40/Any)와 `Level Field`는 **로비 설정**이고 경기 중 변하지 않으며, Elo는 경기 사이에 갱신된다(v1.1에 "New **Ranked** Game Option that can now be disabled to stop matches from affecting your Elo Skill Rating") — `direct page retrieval`.

### 다만 내 "매치 상수는 곱, 턴 변동은 가산" 규칙은 Main의 반론으로 좁혀졌다

Main의 구분이 맞다: Worms Handicapping이 곱하는 대상은 **체력이라는 스칼라**(±50%)이고, Main이 건드리는 대상은 **일정(schedule)** — `Mathf.Lerp(aiErrorStart 2.5, aiErrorEnd 0.8, DifficultyT)`에서 `DifficultyT`가 Hill 곡선 `n^p/(n^p+h^p)`이다. 일정에 곱하면 **등급마다 곡선 형태가 달라지고**(초심자는 완만, 정예는 급함) 난이도 진행의 모양이 플레이어 실력의 함수가 된다. 가산이면 모든 등급이 같은 형태를 평행 이동한다.

**따라서 내 규칙은 이렇게 좁혀야 한다: "스칼라 보정은 곱이 출하 관행이고, 일정/곡선 보정은 형태 보존을 위해 가산이 맞다."** 대상이 스칼라냐 일정이냐가 갈림이며, Worms 선례는 스칼라 사례이므로 Main의 선택과 충돌하지 않는다. 이 구분은 Main이 제시했고 내가 받아들인 것이다 — 내 원래 규칙은 대상 종류를 구분하지 않아 과일반화였다.

## 부록 보충 2 — CPU 레벨이 **무엇을** 바꾸는가: 내부는 미문서화, 그러나 축이 여러 개임은 확정

Main의 질문: Worms CPU 레벨 1~5가 조준 정확도인지 무기 선택인지 반응인지 문서화돼 있는가. 우리 치석이 AI 조준 오차 하나만 건드리는데 출하 게임이 여러 축을 쓴다면 단일 축이 빈약하다는 신호일 수 있다.

### (a) 내부 메커니즘은 **문서화돼 있지 않다** → `확인 불가`

- WKB에 AI 페이지가 없다 — `worms2d.info/Artificial_intelligence` **HTTP 404**.
- `"CPU level"` 완전일치 검색 결과 **5건뿐**이고 전부 파일 포맷 / 랭크 표 / 전술 페이지다(Deathmatch, Team file, Team file (first generation), Multiplayer Deathmatch, Training Disciplines (WWP)). 어느 것도 레벨이 조준각·파워·무기선택·반응 중 무엇을 바꾸는지 서술하지 않는다.
- `CPU level intelligence aim` 검색은 결과 1건이며 미션 공략 페이지의 무관한 문장이다.
- Hedgewars도 `hedgewars.org/wiki/Computer_player` → "Page does not exist". Team 페이지에도 AI 레벨 항목이 없다(name/8 hedgehogs/flag/grave/fort/voice만).

→ **레벨→내부 파라미터 매핑은 1차 출처로 확정 불가.** 리버스 엔지니어링 자료가 WKB에 올라와 있지 않다.

### (b) 그러나 **난이도가 단일 축이 아니라는 것은 확정된다** — Deathmatch가 최소 3축을 동시에 움직인다

> "The game mode is rank based, with the difficulty increasing per rank via **the worm counts being more and more skewed in the CPU's favor** and **the CPU teams on average having higher difficulty presets**."

랭크 표(WWP 기준, 21랭크)에서 읽히는 세 축:

| 축 | 랭크 0 (Absolute beginner) | 랭크 20 (Elite) |
|---|---|---|
| **인간 웜 수** | 8 | **2** |
| **적 팀 수** | 3 (ROYALTY/TEAM17/NASTY CREW) | **5** (VENOM까지) |
| **적 CPU 레벨** | 전부 1 | **전부 5** |
| **적 웜 총수** | 1+1+1 = 3 | 5+4+3+2+1 = **15** |

즉 인간 웜은 8→6→4→2로 줄고, 적은 팀 수·웜 수·CPU 레벨이 동시에 오른다. **주된 난이도 레버는 정확도가 아니라 수량 비대칭이다.**

### (c) 그리고 CPU에는 레벨과 무관한 **영구 능력 상한**이 있다

> "Remember that **CPU worms can't move very well (no backflips, Jet Packs, or Ninja Ropes)**, limited to just walking, front-jumping, back-jumping, and using a Teleport."

> "CPU worms will **try to avoid hitting their mates**, especially if they belong to their own team."

→ AI는 레벨 5에서도 로프·제트팩·백플립을 못 쓴다. 난이도를 올려도 **이동 능력은 절대 오르지 않는다.** 즉 Worms는 정확도/수량은 스케일하면서 **기동성은 상한으로 고정**했다.

- URL: https://worms2d.info/Deathmatch , https://worms2d.info/Team_file , https://www.hedgewars.org/wiki/Team — `direct page retrieval`

### (d) Main의 우려에 대한 답

**단일 축(조준 오차)은 출하 관행 대비 실제로 좁다.** Worms는 최소 3축(수량 비대칭 / 팀 수 / CPU 프리셋)을 쓰고 1축(기동성)을 상한으로 고정한다. 다만 두 가지 유보:

1. Worms의 주력 축은 **수량**이고 이건 castle-war 구조(코어 대 코어)에 그대로 이식되지 않는다.
2. Worms의 수량 축은 **플레이어에게 완전히 보인다**(웜 수를 세면 된다). 우리 조준 오차 축은 보이지 않는다. §8 노출 관행과 같은 문제가 반복된다 — 축을 늘릴 때 각 축이 보이는지 따로 판단해야 한다.

### (e) 어휘 일치 참고

Worms Deathmatch 랭크 21개 중 **4번이 "Novice", 20번이 "Elite"** 이며, 최상단이 Elite로 고정(Elite에서 이기면 계속 Elite 유지)이다. Main의 등급 이름(Novice/Elite)이 Worms 랭크 어휘와 겹친다 — 우연이라도 이름 충돌은 아니고 같은 계보의 관용어다. 단 Worms는 21단계, 우리는 4단계다.

### (f) 표본 미달 기본값에 대한 **반대 방향 선례** (Main의 Elite 선택과 충돌하지 않지만 기록)

Worms Deathmatch에서 **정보가 없는 신규 플레이어는 랭크 0에서 시작하며, 그것이 가장 쉬운 상태다**(인간 8웜 vs 레벨1 단일웜 3팀). 즉 Worms의 "모르는 상태" 기본값은 **최대한 관대함**이고, 이기면 올라간다. Main이 고른 `Elite`(치석 0 = 개입 없음 = AI 최대 정확도)는 반대 방향이다.

다만 Main의 논거가 이 선례보다 우선한다고 본다. 이유: 동결 경계에서 불연속은 **어느 방향으로든 발생하며, 방향의 부호가 지각을 결정한다.**
- `Novice` 기본 → 8발 경계에서 잘하는 플레이어의 치석이 사라짐 = **AI가 갑자기 정확해짐** = "게임이 나를 속였다"로 읽힘.
- `Elite` 기본 → 8발 경계에서 약한 플레이어에게 치석이 붙음 = **AI가 갑자기 부정확해짐** = "내가 나아졌다"로 읽히거나 최소한 불만을 만들지 않음.

불연속을 없앨 수 없다면 **플레이어에게 유리한 방향으로 터지게** 하는 것이 맞고, Main의 선택이 그것이다. 또한 Worms는 이 문제 자체가 없다 — 난이도가 사용자 선택값이라 "표본 미달" 상태가 존재하지 않는다. 표본 파생 설계에서만 생기는 결정이며, Main이 명시적으로 정하고 이유를 코드·문서에 남긴 것이 옳다.
