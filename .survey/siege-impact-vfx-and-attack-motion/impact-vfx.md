# Lane A — 탄착 VFX와 "직전 탄착점" 표식

castle-war의 흰색 네모(`ui_ph_impact_marker`)를 무엇으로 바꿀지 정하기 위한 조사.
**형태(shape)가 맞는지**를 먼저 묻는다 — 아트를 교체할지, 아이콘이라는 형식 자체를 버릴지.

선행 조사 `.survey/siege-visibility-and-telegraph/solutions.md`를 전제로 하고 중복하지 않는다.
사전 예고가 성립하지 않고 **사후 판독이 성립한다**는 결론은 거기서 이미 났다.
이 문서는 그 사후 판독을 **어떤 시각 형식으로** 만들 것인지만 다룬다.

인용 규칙: 직접 열어본 페이지에만 `direct page retrieval` + URL을 붙였다.
검색 요약만 있는 항목은 `SEARCH-SUMMARY-ONLY`로 표기하고 근거로 쓰지 않았다.

---

## 표본 (13종)

| 계보 | 타이틀 |
|---|---|
| 포병·파괴 (9) | Scorched Earth(1991) · Rampart(1990) · Worms Armageddon(1999) · 포트리스2(1999) · Gunbound(2002) · Pocket Tanks(2001) · Crush the Castle(2009) · Angry Birds(2009) · ShellShock Live(2015) |
| 현대 2D 타격감 (3) | Nuclear Throne(2015) · Luftrausers(2014) · Dead Cells(2018) |
| 은폐정보 격자 (1) | Battleship (Milton Bradley 1967 페그보드판) |

Battleship을 넣은 이유는 뒤의 §2에서 드러난다. **표본 중 탄착점에 아이콘을 남기는
유일한 게임**이고, 그게 우연이 아니다.

---

## 0. 1차 사료에 대해 — Nijman 강연은 실제로 열어서 확인했다

이 문서의 레이어 근거 대부분은 Jan Willem Nijman(Vlambeer)의 2013년 강연이다.
GDC Vault에 세션이 없고, 트랜스크립트·슬라이드·Gamasutra 기사도 찾지 못했다.
그래서 **공식 업로드(Dutch Game Garden)의 자동 캡션을 직접 받아서** 롤링 윈도우를
선형 트랜스크립트로 복원해 읽었다(1,031 세그먼트 / 7,378 단어 / 44:09).
기계 전사이므로 철자 오류가 있고(`sleep`→정확, `Vlambeer`→"vamir", `Luftrausers`→"Loof trousers"),
아래 인용은 **음성 그대로**다.
`direct caption retrieval (yt-dlp --write-auto-subs, en)` https://www.youtube.com/watch?v=AJdEqssNZ-U
— 페이지 본문이 아니라 **자동 캡션 트랙**을 받은 것이므로 `direct page retrieval`로 표기하지 않는다.

먼저 강연 제목 자체가 오해다. Nijman은 무대에서 제목을 거부하고 갈아치운다:

> "this talk is called the art of screen Shake um which is also a bit silly so I'm just
> going to cut all the talk crap ... so this talk is now officially called **30 tiny tricks
> that will make your action game better**" — `[00:07:04]` `[OBSERVED]`

즉 이건 "화면 흔들기 강연"이 아니라 **30개 항목 목록**이고, 우리에게 필요한 항목이
그 안에 있다.

---

## 1. 탄착 순간의 레이어 표

| 레이어 | 사료가 있는 게임 | 개발자 진술 | castle-war 판정 |
|---|---|---|---|
| **충격 버스트** (뭔가 맞았다는 즉시 표시) | Nuclear Throne `[OBSERVED]` | "**impact effects** like see that little thing playing when I hit a wall or an enemy that makes like bullets stop disappearing it's like **hey something hit here**" `[00:11:43]` | **이미 보유** (`SpawnImpactBurst`). 유지 |
| **히트스톱** (20ms 정지) | Nuclear Throne `[OBSERVED]` | "it just pauses the game for a couple of milliseconds and I use that **in moments of impact** so when I hit an enemy it will pause for **20 milliseconds** ... your brain won't notice that but kind of use that time to **process what's happening**" `[00:18:11]`, `[00:18:20]` | **이미 보유** (`DestructibleBlock` 피해 비례). 유지 |
| **화면 흔들림** | Nuclear Throne, Luftrausers `[OBSERVED]` | "usually the answer is a add some screen Shake because nobody does it and it's super easy and it makes your game better ... **this guy is sad this guy is happy**" `[00:16:45]` (전사 그대로. "is a add"의 `a`는 말더듬) | **이미 보유** (`ScreenShakeManager`). 단 §4의 상한 주의 |
| **먼지·연기** | Nuclear Throne `[OBSERVED]` | "even more permanence I think here I added like **simple smoke to the explosions** so that after a battle you can kind of **see the smoke fade away**" `[00:27:52]` | **이미 보유** (`SpawnCollapseDust`). 유지 |
| **플래시 / 흑백 명멸** | Nuclear Throne `[OBSERVED]` | "the explosions are just like **a circle that that flashes from black to white and it works** I think" `[00:27:34]` (전사 그대로. "that that" 중복은 말더듬) | 보유하나 **§4의 광민감성 상한**이 이 레이어에만 걸린다 |
| **넉백** (맞은 쪽이 밀림) | Nuclear Throne `[OBSERVED]` | "**enemy knockback** ... nobody really notices it but it can kind of **influence gameplay** even like you see how that guy actually fell to the right now I have a dynamic combat situation" `[00:12:36]` | 물리로 이미 발생(블록 붕괴). 별도 작업 불필요 |
| **파티클 스프레이 / 파편** | (Nijman 강연엔 없음 — §6 참조) Swink `[OBSERVED]` | "Polish can include **sprays or dustings of particles where things hit or interact**, screen shake, view angle shifts, or the squash and stretch of objects colliding" `direct page retrieval` https://www.gamedeveloper.com/design/game-feel-the-secret-ingredient | **이미 보유** (`DebrisSystem`, 블록 자체 스프라이트로 파편) |
| **잔존 흔적 (permanence)** | Nuclear Throne `[OBSERVED]` | §2 전체가 이 항목이다 | **결손. 이게 흰 네모가 메우려 한 자리다** |
| **충격파 링** | **사료 없음** — §6 기록된 공백 | — | 보유하나 **관례로 입증 못 함** |
| **피해 숫자** | **표본에서 사료 없음** — §6 | — | 보유(`SpawnFeedbackLabel`) |

핵심: **표 10줄 중 8줄은 castle-war가 이미 갖고 있다.** 사용자의 "흰 네모" 불만은
타격감 부족이 아니다. 정확히 한 줄, **잔존 흔적**만 결손이고 거기에 플레이스홀더가 박혀 있다.

---

## 2. 지속 표식 — 이 조사의 핵심

### 2.1 Nijman은 "permanence"를 이름 붙여 3번 반복한다

목록 중 가장 많이 재등장하는 항목이다. `[OBSERVED]` 전부 축어 확인:

> "**permanence** this is something super important like **why leave the [ __ ] battle
> empty after it's over** like now I'm here sending hey look I killed one two three four
> five six seven dudes **there was combat here**" — `[00:13:07]`

> "**more permanence** oh yeah I put little shells in and **they stay forever** because
> computers nowadays can handle shells that stay forever they don't have to disappear" — `[00:20:31]`

> "**even more permanence** ... simple smoke to the explosions so that after a battle you
> can kind of see the smoke fade away" — `[00:27:52]`

그리고 **우리 문제에 정확히 대응하는 문장이 따로 있다. 빗나간 탄에 대한 것이다:**

> "why in movies shootouts in bars are so good because **every bullet that misses actually
> hits a bottle** ... all the props in Nuclear Throne ... they're just there not to
> influence anything but **when you miss a shot something actually happens** ... that sense
> of permanence is really important" — `[00:14:20]` `[OBSERVED]`

이건 castle-war의 미스 케이스 그대로다. 빗맞은 탄은 지금 `ShotReadback`에 "빗나감"
한 줄만 남기고 필드에는 아무 물리적 결과도 남기지 않는다.
Nijman의 처방은 **소품이 반응하게 만드는 것**이다 — 아이콘을 띄우는 게 아니다.

### 2.2 그런데 포병 계보가 실제로 하는 것은 아이콘이 아니라 **월드 변형**이다

표본을 하나씩 확인했다. 전부 `[OBSERVED]`:

| 게임 | 탄착점에 남는 것 | 형태 | 기계적 의미 | 사료 |
|---|---|---|---|---|
| Worms Armageddon | 크레이터 | 원형 구멍, **영구** | **파워별 지름이 밸런스 수치다**: 파워11 47px / 파워3 97px / 파워15 199px | `direct page retrieval` https://worms2d.info/Bazooka — "Standard effects: 50hp injury (max.), **Small circular crater**" + Power/Crater-diameter 표 |
| 포트리스2 | 지형 소실 | 파인 땅, 영구 | **탈락 조건**: "폭발로 인해 캐릭터가 올라서 있을 지형이 없어져 스테이지 밑으로 떨어지는 경우 게임에서 탈락하게 된다" | `direct page retrieval` https://ko.wikipedia.org/wiki/포트리스2 |
| Gunbound | 땅 파괴 | 파인 땅, 영구 | **번지(bunge)가 HP와 대등한 승리 조건**: "Bunging is **destroying the land around an opponent's area, causing the opponent to fall**" / 모빌 스탯에 "**bunge (land damage) ability**"가 방어·공격·체력과 나란히 실린다 | `direct page retrieval` https://en.wikipedia.org/wiki/GunBound |
| Gunbound (Jewel 모드) | 땅 파괴만 | — | **피해와 지형파괴가 분리된 채널**임을 증명: "Shots fired at the enemy **will not cause damage, but can destroy land**" | 동일 URL |
| Scorched Earth | 지형 소실 | 영구 | 흙을 덮거나 파낸다: "**earth weapons** — allowing the player to dump dirt on other tanks or to **remove ground from beneath them**" | `direct page retrieval` https://en.wikipedia.org/w/index.php?title=Scorched_Earth_(video_game)&action=raw |
| Rampart | **벽의 구멍** | 영구, 다음 페이즈 입력 | 공격자의 목표 자체다: "The goal of the attacker in both cases is to **make holes in the walls**" / "the damage caused during the combat phase is normally **spread out**, repairing it can be difficult" | `direct page retrieval` https://en.wikipedia.org/w/index.php?title=Rampart_(video_game)&action=raw |
| Pocket Tanks | 지형 소실 | 영구 | "features a **fully destructible environment**, which allows the player to create and put themselves **on pedestals or in bunkers**" | `direct page retrieval` https://en.wikipedia.org/w/index.php?title=Pocket_Tanks&action=raw |
| Angry Birds | 구조물 잔해 | 물리 잔해, 잔존 | 파괴율이 측정 대상: "Total Destruction"에서 "**achieving 100% destruction** earns the player a Mighty Eagle feather" | `direct page retrieval` https://en.wikipedia.org/w/index.php?title=Angry_Birds_(video_game)&action=raw |
| Crush the Castle | 구조물 잔해 | 물리 잔해 | 성 파괴가 목표 | 동일 방식 https://en.wikipedia.org/w/index.php?title=Crush_the_Castle&action=raw |
| Nuclear Throne | 부서진 벽 + 탄피 + 시체 | 영구 | Wikipedia "Destructible environment" 문서의 **대표 이미지가 Nuclear Throne**이다 | `direct page retrieval` https://en.wikipedia.org/w/index.php?title=Destructible_environment&action=raw |

**형태에 대한 일관된 사실:** 이 계보에서 탄착 흔적은 **월드 자체의 상태 변화**이고,
그 크기가 **밸런스 수치**다(Worms의 크레이터 지름 표, Gunbound의 land damage 스탯,
포트리스2의 탱크별 파괴 범위). 즉 흔적은 장식이 아니라 **규칙의 일부**다.

### 2.3 그렇다면 아이콘은 어디서 관례인가 — Battleship

표본에서 탄착점에 **아이콘**을 남기는 게임은 Battleship 한 종이다. 그리고 색까지 규정돼 있다:

> "The attacking player marks the hit or miss on their own "tracking" or "target" grid ...
> or the appropriate color peg in the pegboard version (**red for "hit", white for "miss"**),
> in order to **build up a picture of the opponent's fleet**" `[OBSERVED]`
> `direct page retrieval` https://en.wikipedia.org/w/index.php?title=Battleship_(game)&action=raw

여기서 아이콘이 성립하는 조건이 그대로 드러난다. Battleship은 **월드가 보이지 않는 게임**이다:

> "The game is a **discovery game** in which players need to discover their opponent's ship
> positions." `[OBSERVED]` 동일 URL

월드가 안 보이니 월드가 변형돼도 읽을 수 없다. 그래서 **페그가 유일한 기록**이 된다.
아이콘은 은폐정보의 대체물이다.

### 2.4 명시적 답 — 아이콘인가 월드 변형인가

> **월드 변형이 관례다. 탄착점에 아이콘을 띄우는 것은 관례가 아니고,
> 표본 13종 중 1종에만 있으며 그 1종은 월드가 숨겨진 게임이다.**
>
> **castle-war는 월드가 전부 보인다. 따라서 형태 선택이 틀렸다 — 아트 문제가 아니다.**
> 흰 네모를 예쁜 네모로 바꾸는 것은 잘못된 형식을 유지하는 수선이다.

부연 두 가지, 정직하게:

1. **우리는 이미 월드 변형 능력을 갖고 있다.** 지면 타일이 `DestructibleBlock`이고
   `isGroundAnchor`는 바깥 열과 아래 2행에만 붙는다 —
   `GameManager.cs:1508` `block.isGroundAnchor = (x <= -groundAnchorAbsX || x >= groundAnchorAbsX) || yIndex >= groundRowCount - 2;` `[OBSERVED — 코드]`
   즉 중앙 대역에 떨어진 탄은 **이미 지형을 파낸다.** 관례를 새로 도입할 필요가 없고,
   **이미 하고 있는 일을 아이콘이 가리고 있다.**
2. **다만 완전히 무근거는 아니다.** Scorched Earth의 트레이서는 "다음 턴 조준용 기록"이라는
   기능이 아이콘 계열로 존재한다: "All weapons can be upgraded with **tracers** which allow
   the player to **more accurately adjust the trajectory on their next turn**" `[OBSERVED]`
   그러나 이것은 **탄착점의 점**이 아니라 **궤적의 기록**이다.
   그리고 castle-war는 그 궤적 기록을 이미 `ShotTraceDirector`의 `LineRenderer`로 갖고 있다.
   → 마커를 지워도 **판독 정보는 하나도 잃지 않는다.** 궤적선이 끝점을 이미 지시한다.
     `[INFERENCE — 코드 구조에서 도출]`

---

## 3. 누가 쐈는지 읽히게 하기

### 3.1 색은 필요하지만 단독으론 안 된다 (표준 근거)

castle-war는 현재 팀 색만으로 구분한다 —
아군 `(0.45, 0.85, 1.0)` 하늘색 / 적 `(1.0, 0.35, 0.25)` 주황, 알파 0.5
(`ShotTraceDirector.Draw()`) `[OBSERVED — 코드]`

- **WCAG 2.2 SC 1.4.1 (Level A)**: "Color is not used as the **only** visual means of
  conveying information, indicating an action, prompting a response, or **distinguishing a
  visual element**." 대처는 "Use information **in addition to** color, such as **shape or
  text**, to convey meaning." `[OBSERVED]`
  `direct page retrieval` https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html
- 같은 문서에 우리에게 유리한 완화 조항이 있다: "If content is conveyed through the use of
  colors that **differ not only in their hue, but that also have a significant difference in
  lightness**, then this counts as an ad[ditional visual distinction]" `[OBSERVED]` 동일 URL
- **Game Accessibility Guidelines** — "Ensure no essential information is conveyed by a
  fixed colour alone": "Difficulty perceiving **red or green** in particular is very common,
  affecting around **8-10% of males** ... Wherever you can, use colour as a **back-up** for
  another means of communicating the information, such as **text or a symbol, pattern or
  shape**." `[OBSERVED]`
  `direct page retrieval` https://gameaccessibilityguidelines.com/ensure-no-essential-information-is-conveyed-by-a-fixed-colour-alone/

**우리 팀 색 선택은 실제로 잘 골랐다.** 위험 축은 적록이고 우리는 **청 대 주황**이다.
게다가 하늘색은 밝고 주황은 어두워 명도 차가 있어 위 완화 조항에 걸린다. `[INFERENCE]`
문제는 색이 아니라 **형태가 양쪽 다 같은 네모라서** 색 외의 채널이 0이라는 점이다.

### 3.2 이미 색 외 채널을 붙인 선례 — Dead Cells 2.9

접근성 전용 업데이트("Breaking Barriers", 2022-06-23)의 비디오 옵션 목록에 두 항목이 있다:

> "**Display stats icons in addition to their color.**"
> "**Reduce the number of particles.**" `[OBSERVED]`
> `direct page retrieval` https://deadcells.wiki.gg/wiki/Version_2.9

앞줄은 §3.1의 정확한 실행이고, 뒷줄은 §4로 이어진다.
같은 페이지에 "Outlines for the Beheaded, **Enemies**, Skills, **Projectiles** and Secrets"도
있어, **아웃라인이 진영 구분의 실사용 채널**이라는 근거가 된다. `[OBSERVED]`

### 3.3 계보가 실제로 쓰는 방식

포병 계보는 **턴 순서 자체가 귀속 정보**다. 한 번에 한 명만 쏘므로 색이 필요 없다.
포트리스2는 시작 시 위치를 보여주고 기억하게 만든다:
"게임이 시작되면 일정 시간 동안 모든 플레이어의 위치를 보여준다.
이때 플레이어들의 위치를 **기억하는 것이 중요**하다" `[OBSERVED]`
`direct page retrieval` https://ko.wikipedia.org/wiki/포트리스2

**castle-war의 상황은 다르다.** 우리 사후 판독은 아군 흔적과 적 흔적을 **동시에 나란히**
남긴다(`playerTrace` / `enemyTrace` 2개 유지). 즉 동시 병존이므로 턴 순서로는 구분되지 않고,
**색 외 채널이 실제로 필요하다.** `[INFERENCE]`

---

## 4. 개발자가 공개적으로 후회한 선택들

### 4.1 과도한 흔들림 — Nijman 본인의 진술 (가장 직접적인 사료)

강연 Q&A에서 "언제가 과한가"를 묻자 본인이 답한다:

> "too much — actually **we had to put like an option in nuclear [Throne] to disable the
> screen Shake because some people were getting really nauseous** okay and I guess like a
> **warning** with screen Shake is that **it gets kind of addictive and you get used to it**
> and you put a ton of it in your games and **you stop noticing it** and then **everybody
> else throws up when they play it**" — `[00:32:21]` `[OBSERVED]`

이건 커뮤니티 추측이 아니라 **개발자가 무대에서 인정한 회고**다.
그리고 실패 기제가 명시돼 있다: **제작자는 적응해서 못 느끼고, 신규 플레이어만 토한다.**
castle-war는 이미 여러 경로에서 흔들림을 부른다(`CannonController`, `DestructibleBlock`,
`CastleCoreGimmick` 0.4/0.25 등) — **추가하지 말고 상한을 두는 쪽**이 사료가 지지하는 방향이다.

> 참고: Nuclear Throne 흔들림 슬라이더의 "0–200% 범위"는 검색 요약에만 있었고
> 1차 사료로 확인하지 못했다. `SEARCH-SUMMARY-ONLY` — 근거로 쓰지 않았다.
> 위 인용이 확인한 것은 **"disable 옵션이 존재하고 그 이유가 구역질"**까지다.

### 4.2 플래시 / 화이트아웃 — 실제 피해가 기록된 유일한 레이어

- **1997 Pokémon "Dennō Senshi Porygon" 사건**: "multiple scenes with flashing lights
  induced **photosensitive epileptic seizures** in children across the country.
  **Over 600 people**, mostly children, were **taken to hospitals**" `[OBSERVED]`
  `direct page retrieval` https://en.wikipedia.org/w/index.php?title=Dennō_Senshi_Porygon&action=raw
  방아쇠가 된 장면은 우리 도메인과 정확히 같다: "an attack, resulting in an **explosion**
  that is depicted by **rapid flashing lights that fill the screen**" `[OBSERVED]` 동일 URL
- **정량 기준 — GAG**: 회피해야 하는 것으로 명시된 항목:
  "More than **three flashes** in a single second, covering **25%+ of the screen**" /
  "Any sequence of flashing images that lasts for **more than 5 seconds**" /
  "**Static repeated patterns** ... covering **40%+ of the screen**" —
  각주가 flash를 "an instantaneous high change in brightness/contrast (including fast cuts),
  **or to/from the colour red**"로 정의하고, 패턴을 "more than 8 static or **5 moving** high
  contrast repeated stripes — **parallel or radial**"로 정의한다. `[OBSERVED]`
  `direct page retrieval` https://gameaccessibilityguidelines.com/avoid-flickering-images-and-repetitive-patterns/
- **WCAG 2.2 SC 2.3.1 (Level A)**: "do not contain anything that flashes **more than three
  times in any one second** period, or the flash is below the general flash and red flash
  thresholds." 의도 절에 "People are **even more sensitive to red flashing**"과
  "close-ups of **rapid-fire explosions**"이 예시로 적혀 있다. `[OBSERVED]`
  `direct page retrieval` https://www.w3.org/WAI/WCAG22/Understanding/three-flashes-or-below-threshold

**우리에게 적용되는 결론 두 개:**
1. 흰색 전체화면 플래시는 이 문서에서 **유일하게 실제 인체 피해가 기록된 레이어**다.
   충격 표현을 강화할 때 **플래시로 강화하는 선택만은 근거가 반대 방향**이다.
2. **"radial" 반복 패턴 조항이 우리 충격파 링에 걸린다.** 현재 링은 1개라 기준(정적 8줄 /
   동적 5줄) 아래로 안전하다 — 다만 **동심원을 여러 겹 겹치는 방향으로 강화하면 기준에
   접근한다.** 링을 늘리는 설계는 이 조항을 먼저 봐야 한다. `[INFERENCE — 기준을 우리 구현에 적용]`

### 4.3 VFX가 보드를 가리는 문제

- 선행 조사가 이미 확립한 것을 재확인만 한다: ItB의 "아이콘 쓰레기장", 화면 요소 증가 비용.
  `.survey/siege-visibility-and-telegraph/solutions.md` 참조.
- 새로 붙일 근거는 Dead Cells가 **파티클 수 감소를 접근성 옵션으로 출하**했다는 사실이다
  (§3.2 인용). 파티클 과다는 취향 문제가 아니라 **접근성 항목으로 취급된 전례**가 있다. `[OBSERVED]`

### 4.4 웹/저사양 파티클 비용

Unity 공식 문서로 확인한 것은 **다운로드 비용**과 **텍스처 업로드 비용**이고
(§5), **런타임 파티클 수의 웹 비용을 직접 규정한 1차 사료는 찾지 못했다.**
§6에 공백으로 기록한다.

---

## 5. 절차적 생성 vs 저작 아트

우리 코드가 실제로 코드에서 만드는 것(전부 `[OBSERVED — 코드]`):
- `GameFeelVfx.GetRingSprite()` — 48x48, 외경 22 / 내경 17 (**5px 띠**), `Color.white`,
  `FilterMode.Point`, 캐시 1개
- `GameFeelVfx.GetDefaultParticleTexture()` — 32x32 방사 감쇠(`Mathf.Pow(alpha, 1.5f)`),
  `FilterMode.Bilinear`, 캐시 1개
- 그 외 `DebrisSystem.GenerateFragmentTexture`, `GameManager.GenerateGroundTexture` 등

### 5.1 크기 논거는 공식 문서로 지지된다

- "When publishing for Web, it is important to **keep your build size low** so users get
  reasonable **download times** before the content starts." `[OBSERVED]`
  `direct page retrieval` https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-distributionsize-codestripping.html
- "Typically, assets like **textures**, sounds, and animations **take up the most storage**.
  **Scripts, scenes, and shaders usually have the smallest impact.**" `[OBSERVED]`
  `direct page retrieval` https://docs.unity3d.com/6000.0/Documentation/Manual/ReducingFilesize.html
- WebGL 빌드는 AssetBundle/Addressables를 안 쓰면 전부 선행 다운로드 대상인 단일
  `.data`로 묶인다: "`[ExampleBuild].data` | Asset data and Scenes." `[OBSERVED]`
  `direct page retrieval` https://docs.unity3d.com/6000.0/Documentation/Manual/webgl-building.html

→ **웹 타깃에서 "코드로 만든다"의 크기 이득은 Unity 문서가 직접 지지한다.**

### 5.2 대가도 문서화돼 있다 — 그리고 우리 코드는 이미 옳게 처리했다

- "`Apply` is an **expensive operation** because it **copies all the pixels** in the texture
  even if you've only changed some of the pixels, so change as many pixels as possible
  before you call it." `[OBSERVED]`
- "Unity can store a copy of the texture in **both CPU and GPU memory**. The CPU copy is
  optional." / "If you set `makeNoLongerReadable` to `true`, Unity **deletes the CPU copy**
  of the texture after it uploads it to the GPU." `[OBSERVED]`
  `direct page retrieval` https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Texture2D.Apply.html

우리 링 생성은 `texture.Apply(false, true)` — **밉맵 재계산 끄고 CPU 복사본 폐기**.
문서 권고와 정확히 일치하고, 캐시가 1개라 `Apply`는 세션당 1회다. `[OBSERVED — 코드]`
**즉 성능은 이 문제의 축이 아니다.**

### 5.3 진짜 반대 논거는 아틀라스 배제다

> "If you use a **separate texture for each of your sprites**, Unity has to create and
> **send a separate draw call to the GPU for each texture**. As a result, performance can
> decrease. **To reduce the number of draw calls, create a sprite atlas.**" `[OBSERVED]`
> `direct page retrieval` https://docs.unity3d.com/6000.0/Documentation/Manual/SpriteAtlasWorkflow.html

런타임 생성 텍스처는 임포트 파이프라인 밖에 있어 저작 스프라이트와 같은 아틀라스에
묶이지 않는다 → 배치가 끊긴다. **이것이 절차적 2D VFX 아트에 대한 가장 강한 기술적 반론이다.**
단, 위 문서가 SRP Batcher 사용 시 "the number of draw calls might not decrease, but
performance still improves a similar amount"이라 덧붙이므로, 우리 URP 환경에서 이 비용이
얼마인지는 **측정 문제로 남는다.** `[INFERENCE]`

### 5.4 절차적 생성의 고전 사례 — 이득과 청구서 양쪽

.kkrieger(Farbrausch/.theprodukkt, Breakpoint 2004 96k 부문 1위):
- 이득: "Textures are stored via their **creation history** instead of a per-pixel basis,
  thus only requiring the history data and the generator code to be compiled into the
  executable, producing a **relatively small file size**." `[OBSERVED]`
- 청구서: "These two-generation processes account for the **extensive loading time** of the
  game; **all assets of the gameplay are reproduced during** [startup]." `[OBSERVED]`
  `direct page retrieval` https://en.wikipedia.org/w/index.php?title=.kkrieger&action=raw

교환비가 명확하다: **다운로드를 로딩 시간으로 바꾼다.** 우리 규모(48x48 하나, 32x32 하나)에선
로딩 비용이 무의미하므로 교환이 유리한 쪽이다. `[INFERENCE]`

### 5.5 그러나 정작 물은 질문은 답을 못 찾았다

**절차적 2D VFX(방사 그라데이션 파티클, 링 스프라이트)를 저작 스프라이트 시트와
직접 비교한 개발자 논평은 찾지 못했다.** 절차적 텍스처·메시 일반론(.kkrieger, procedural
texture)은 있지만, **2D VFX 아트를 대상으로 한 진술이 아니다.**
일반론을 2D VFX 논평인 것처럼 쓰지 않는다. **UNSOURCED** — §6에 기록.

가장 가까운 근접 사료는 Nijman의 폭발 진술이다:

> "the explosions are just like **a circle that that flashes from black to white and it
> works** I think" — `[00:27:34]` `[OBSERVED]`

**세계적으로 타격감으로 알려진 게임의 폭발이 코드로 그릴 수 있는 원 하나였다.**
이건 "절차적이 저작보다 낫다"는 근거는 아니지만, **"저작 아트가 없어서 못 한다"는 반대
논거를 무력화**한다. 형태가 맞으면 원 하나로도 성립한다. `[INFERENCE]`

---

## 6. 표본 빈도 순위 (13종)

각 줄은 위에서 URL로 확인한 것만 센다.

| 순위 | 항목 | 빈도 | 비고 |
|---|---|---|---|
| 1 | **탄착점의 월드 상태 변화** (크레이터/구멍/잔해) | **10 / 13** | 확인 YES 10 · 확인 NO 1(Battleship) · 미확인 2(Luftrausers, Dead Cells) |
| 2 | 그 변화가 **기계적으로 유의미** (규칙에 물림) | **7 / 13** | Worms(지름=밸런스) · 포트리스2(탈락) · Gunbound(승리조건+스탯) · Scorched(흙무기) · Rampart(다음 건설) · Pocket Tanks(엄폐 생성) · Angry Birds(파괴율 측정) |
| 3 | 결과 피드백 일반 | 13 / 13 | 선행 조사와 일치(12/12). 재조사 아님 |
| 4 | 화면 흔들림 / 히트스톱 사료 | 2 / 13 | Nuclear Throne · Luftrausers — **둘 다 같은 강연 하나에서 나온다** |
| 5 | 색 외 채널을 옵션으로 출하 | 1 / 13 | Dead Cells 2.9만 |
| 6 | **탄착점에 남는 아이콘** | **1 / 13** | **Battleship 단독. 그리고 월드가 숨겨진 유일한 게임이다** |
| 7 | 충격파 링을 관례로 확인 | **0 / 13** | 사료 0 |
| 8 | 피해 숫자를 사료로 확인 | **0 / 13** | 표본 사료 0 (castle-war는 보유) |

**읽는 법:** 우리가 현재 그리고 있는 형식(아이콘)은 표본에서 **1/13**이고,
그리지 않고 있는 형식(월드 변화)은 **10/13**이다. 그리고 아이콘 1종은
**우리와 정보 구조가 반대인 게임**이다. 순위 1과 순위 6의 격차가 이 조사의 답이다.

또 하나: 순위 2가 순위 1보다 겨우 3 낮다. **흔적을 남긴 게임의 70%가 그 흔적을 규칙에
물렸다.** 흔적은 이 계보에서 장식으로 존재한 적이 거의 없다.

---

## 7. 기록된 공백 (사료를 못 찾은 것)

정직하게 남긴다. 확인 못 한 것을 확인한 것처럼 쓰지 않았다.

1. **절차적 2D VFX vs 저작 스프라이트 시트의 직접 개발자 논평 — UNSOURCED.**
   과제가 명시적으로 물은 항목이고, 못 찾았다. 절차적 텍스처 일반론으로 대체하지 않았다(§5.5).
2. **충격파 링의 관례 근거 — 0/13.** 우리는 링을 그리는데, 표본 어디서도 링을
   탄착 어휘의 항목으로 확인하지 못했다. 링이 틀렸다는 뜻은 아니지만
   **관례라는 주장을 지지하는 사료가 없다.**
3. **Nijman 강연에 "particle"·"debris" 단어가 없다.** 트랜스크립트 전문 검색 0건.
   파편 레이어 근거는 Swink로 대체했다. 이 강연을 파편 근거로 인용하면 안 된다.
   같은 이유로 **"bullet holes"·"scorch marks"도 이 강연에 없다** — 그의 permanence
   예시는 시체·탄피·연기다. 그을음 데칼을 이 강연에 귀속시키면 오인용이다.
4. **Cyberpunk 2077 브레인댄스 광민감성 사례 — 회수 실패.** Game Informer(2020-12),
   GamesRadar, Eurogamer, Wayback 모두 본문을 못 받았고 현재 Wikipedia 본문에도
   해당 서술이 없다(grep 0건). **§4.2는 Porygon + WCAG + GAG만으로 세웠다.**
5. **웹/저사양에서 런타임 파티클 수의 비용을 규정한 1차 사료 — 못 찾음.**
   확인한 것은 다운로드 크기와 `Apply` 업로드 비용까지다(§5).
6. **ShellShock Live의 지형 파괴 — 미확인.** 회수한 문서에 destructible 서술이 없다
   ("Players control tanks in a 2D landscape"까지만). 순위 1의 미확인 2종에 포함시키지 않고
   별도로 남긴다 — 계보상 있을 것이라 **추측했지만 세지 않았다.**
7. **포트리스2 "각샷" 암기 문화가 곧 "게임이 탄착 기억을 주지 않았다"는 증거인지 — 미확정.**
   논리적으로는 이어지지만 그 인과를 진술한 사료를 확보하지 못했다. `[INFERENCE]`로만 유효.
8. **Nuclear Throne 흔들림 슬라이더의 수치 범위 — SEARCH-SUMMARY-ONLY.**
   "disable 옵션 존재 + 이유는 구역질"까지만 1차 확인(§4.1).

---

## 8. 이 레인의 결론

1. **흰 네모는 아트 결손이 아니라 형식 오류다.** 표본 13종 중 탄착점 아이콘은 1종,
   그것도 월드가 숨겨진 게임이다. 월드가 전부 보이는 castle-war에서 아이콘은
   **이미 보이는 것을 가리는 중복**이다.
2. **관례는 월드 변형이고, 우리는 그 능력을 이미 갖고 있다.** 지면 타일이 파괴 가능하고
   (`isGroundAnchor`는 테두리만), 파편·먼지·버스트·흔들림·히트스톱도 이미 있다.
   결손은 표 10줄 중 **permanence 한 줄**뿐이다.
3. **마커를 지워도 판독 정보는 잃지 않는다.** 궤적선의 끝점이 탄착점을 이미 지시한다.
   즉 이 레인의 최소 조치는 "무엇을 추가할까"가 아니라 **"틀린 형식을 제거하고,
   이미 있는 월드 변화가 보이게 할까"** 다.
4. **강화 방향으로 플래시와 다중 링만은 근거가 반대다** — 유일하게 인체 피해가 기록된
   레이어이고(Porygon 600명 이상 입원), GAG의 radial 반복 패턴 조항이 다중 링에 걸린다.
5. **색 외 채널은 실제로 필요하다.** 우리 청/주황 선택은 적록 위험을 피해 잘 골랐지만,
   아군·적 흔적이 **동시에 병존**하므로 턴 순서로 구분되지 않는다. 형태나 아웃라인 등
   색 아닌 채널이 WCAG 1.4.1이 요구하는 최소치다(Dead Cells 2.9가 실행 선례).

권고의 우선순위·비용·하지 말 것은 `synthesis.md`에서 lane B와 합쳐 정한다.
