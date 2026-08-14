# Lane B — 공격 모션과 턴 주체 판독

**질문:** 플레이어가 개입할 수 없는 AI가 행동할 때, (a) *지금 공격이 일어난다*와
(b) *이건 누구의 공격이다*를 어떻게 오해 없이 전달하는가.

**표본 12종** (아래 모든 빈도 집계의 모집단):
Into the Breach · XCOM 2 · Fire Emblem: Three Houses · Slay the Spire ·
Worms Armageddon · Gunbound · 포트리스2 · Teamfight Tactics(오토배틀러) ·
Punch-Out!!(NES) · Guilty Gear Strive(프레임 데이터 기준선) · Rampart · Left 4 Dead

**표기:** `[OBSERVED]` = 직접 읽은 것 · `[INFERENCE]` = 추론 ·
`direct page retrieval` + URL = 이 조사에서 실제로 가져온 페이지.
검색엔진 요약만 있고 원문을 못 가져온 것은 `[검색 요약만]`으로 따로 표시했다 — 인용 등급이 다르다.

---

## 1. Windup findings — 선행동작이 읽히는 최소치

### 1.1 원칙: 문제는 anticipation이 아니라 staging이다

Thomas & Johnston의 12원칙 중 이 사안에 걸리는 것은 **두 개**다.

- **Anticipation** — *"used to prepare the audience for an action, and to make the action appear
  more realistic. A dancer jumping off the floor has to bend the knees first; a golfer making a
  swing has to swing the club back first."* `[OBSERVED]` `direct page retrieval`
  https://en.wikipedia.org/wiki/Twelve_basic_principles_of_animation
- **Staging** — Johnston & Thomas의 정의: *"the presentation of any idea so that it is completely
  and unmistakably clear."* 목적은 *"direct the audience's attention, and make it clear what is of
  greatest importance in a scene"*, 본질은 *"keeping focus on what is relevant, and avoiding
  unnecessary detail."* `[OBSERVED]` `direct page retrieval` (동일 URL)

사용자 문장이 *"공격을 하는지 모르겠다"*이므로 **실패한 원칙은 staging이다.** 선행동작은 staging을
달성하는 수단 중 하나일 뿐이다. 이 구분이 중요한 이유: anticipation은 시간을 쓰지만 staging은
**주의 배분**을 쓴다. 0.9초 예산에서는 후자가 살아남는다 `[INFERENCE]`.

세 번째로 걸리는 원칙이 압축의 열쇠다.

- **Slow in and slow out** — *"more pictures are drawn near the beginning and end of an action...
  This concept emphasizes the object's extreme poses. Inversely, fewer pictures are drawn within
  the middle of the animation to emphasize faster action."* `[OBSERVED]` `direct page retrieval` (동일 URL)

즉 **프레임을 양 극단에 몰고 중간을 비우는 것이 정석**이다. 짧은 창에서 판독성을 얻는 방법은
동작을 늘리는 것이 아니라 극단 포즈 2개(당김 / 방출)를 선명하게 세우는 것이다 `[INFERENCE]`.

### 1.2 숫자: 프레임 데이터

Guilty Gear Strive(60fps) Sol Badguy의 실측 startup — 격투게임은 "읽히는 선행동작"이 곧 밸런스
수치인 유일한 장르이므로 기준선으로 쓴다. `[OBSERVED]` `direct page retrieval`
https://dustloop.com/w/GGST/Sol_Badguy

| 기술 | startup | ms @60fps | 성격 |
|---|---|---|---|
| 5P (최속 잽) | 5F | **83ms** | 읽고 반응 불가, "빠름"만 전달 |
| Volcanic Viper (DP) | 9F | 150ms | 무적 리버설 |
| Bandit Revolver | 12F | 200ms | |
| Gun Flame | 16F | 267ms | 중거리 투사체 |
| **Fafnir (커밋 기술)** | **20F** | **333ms** | 크게 읽히는 돌진 |
| Aerial Bandit Bringer | 22F | 367ms | |

**읽히는 "무거운" 공격의 선행동작 대역은 대략 250–370ms다** `[OBSERVED — 위 표]`.
83ms짜리 잽은 *존재*는 읽히지만 *정체*는 안 읽힌다 — 이것이 지금 castle-war 발사의 상태다
(선행동작 0프레임이므로 83ms보다도 짧다) `[INFERENCE]`.

같은 페이지에 **시간 감속의 실측치**도 있다. GGST는 카운터히트 등급별로 슬로우를 넣는다:
Small 11F(183ms) · **Mid 25F(417ms)** · Large 35F(583ms) `[OBSERVED]` `direct page retrieval` (동일 URL).
0.5초 미만의 시간 감속이 상용 게임에서 강조 장치로 실제 출하된다는 증거다.

### 1.3 숫자: 인간 반응 시간의 바닥

- **경고 신호(foreperiod)의 최적치는 약 300ms다.** *"constant foreperiods of about 300 ms over a
  series of trials tends to produce the fastest responses."* 그리고 결정적으로 —
  *"foreperiods of less than 300 ms may produce delayed RTs because processing of the warning may
  not have had time to complete before the stimulus arrives."* `[OBSERVED]` `direct page retrieval`
  https://en.wikipedia.org/wiki/Mental_chronometry
  → **300ms보다 짧은 예고는 예고로 기능하지 않는다.** 선행동작 하한의 실증적 근거다.
- **소리가 눈보다 빠르다.** *"an auditory signal is able to reach central processing mechanisms
  within 8–10 ms, while visual stimulus tends to take around 20–40 ms."* `[OBSERVED]`
  `direct page retrieval` (동일 URL)
  → 짧은 창에서 **가장 싼 채널은 사운드**다. 화면 요소를 0개 늘리고 2–4배 빨리 도착한다.

### 1.4 표본 안의 선행동작 실태

- **Punch-Out!!** — *"The behavior of each opposing boxer follows a set pattern requiring trial and
  error and memorization to defeat them."* 그리고 별(uppercut 자원)은
  *"accomplished by counter-punching the opponent directly before or after certain attacks are
  launched."* `[OBSERVED]` `direct page retrieval`
  https://en.wikipedia.org/wiki/Punch-Out!!_(NES)
  → 선행동작이 게임의 **전부**인 사례. 단 성립 조건이 우리와 다르다: 플레이어가 그 창에 **입력할 수
  있다.** 우리는 입력이 0이다 `[OBSERVED — 선행 조사]`.
- **Left 4 Dead** — 실루엣 판독 + 특수 감염체별 고유 음성 큐로 "무엇이 오는가"를 어둠 속에서 전달.
  `[검색 요약만 — GDC 원문/발표자 확인 실패, §8 갭 참조]`

### 1.5 castle-war 실측

| 항목 | 값 | 출처 |
|---|---|---|
| 발사 선행동작 | **0프레임** (Instantiate → `Launch()` 동일 프레임) | `SimpleAI.cs:69-74` `[OBSERVED]` |
| 적 턴 예고 창 | 0.5초, **내용 0** (조준 계산이 창 뒤 `:31`/`:62`) | `SimpleAI.cs:30` `[OBSERVED]` |
| 발사체 비행 중 모션 | 있음 — `launchStretch` 0.18, `launchSpin` 120°/s | `UnitSpriteAnimator.cs:18-19,190-202` `[OBSERVED]` |
| 근접/사격 공격 펄스 | 있음 — `PulseAttack()` 0.18초 | `UnitSpriteAnimator.cs:286-288` `[OBSERVED]` |
| **발사 시점 펄스** | **없음** — `PulseAttack()`은 `UnitController.cs:975` 근접/아처 경로에서만 호출 | `[OBSERVED]` |
| 히트스톱 | 있음 — 0.05초 | `UnitController.cs:1161,1264` `[OBSERVED]` |

**핵심:** 이 게임에는 공격 모션 인프라가 이미 다 있다. 발사 경로에만 연결되지 않았다 `[OBSERVED]`.

---

## 2. Turn-based signal table

"입력 0으로 지켜보는 동안 *지금 일어난다*를 무엇이 전달하는가."
`0.9s 적합?` 열은 우리 예산(사전 창 0.5초 + 총 0.9초)에 넣을 수 있는지다.

| 신호 | 확인된 게임 | 개발자/문헌 근거 | 0.9s 적합? |
|---|---|---|---|
| **행동자로 카메라 이동** | XCOM 2 (Action Cam) | 끄는 옵션이 존재하고 커뮤니티가 끈다 `[검색 요약만]` | **아니오** — 멀미 + WCAG 2.3.3 (§6) |
| **원경↔근경 시점 전환** | Fire Emblem: Three Houses | *"transitioning from a top-down perspective to a third-person view when a battle is triggered"* `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Fire_Emblem:_Three_Houses | 아니오 — 컷 자체가 1초 이상 |
| **행동자만 하이라이트(색/알파)** | ItB, StS, FE (선택/활성 표시) | WCAG 2.3.3이 색·불투명도 변화를 motion animation에서 **제외** → 멀미 면제 `[OBSERVED]` (§6) | **예** — 0프레임 비용 |
| **시간 감속** | GGST 카운터히트 | Mid 25F=417ms, Large 35F=583ms 실출하 `[OBSERVED]` `direct page retrieval` https://dustloop.com/w/GGST/Sol_Badguy | **예** — 0.5초 창에 들어감 |
| **사운드 스팅** | Punch-Out!!(상대별 고유 음악), L4D(감염체별 음성) | 청각 8–10ms vs 시각 20–40ms `[OBSERVED]` (§1.3) | **예** — 가장 싼 채널 |
| **의도 아이콘/수치** | Slay the Spire, ItB | StS는 아이콘→수치 노출이 *더* 몰입적이었다고 기록 `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Slay_the_Spire | 예(단 사전 예고는 선행 조사에서 기각) |
| **의도적 정지/비트** | XCOM 2(행동 후 1–3초, 엄폐 2.75초) | 그 정지를 **삭제하는 모드**가 존재 `[검색 요약만]` (§6) | 조건부 — 우리는 이미 0.9초 지출 중 |
| **개시 시점 전체 조망** | **포트리스2**, Rampart | 포트리스2: *"게임이 시작되면 일정 시간 동안 모든 플레이어의 위치를 보여준다. 이때 플레이어들의 위치를 기억하는 것이 중요하다."* `[OBSERVED]` `direct page retrieval` https://ko.wikipedia.org/wiki/포트리스2 · Rampart: *"The game opens with an automated building phase in which the computer builds a wall around one castle."* `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Rampart_(video_game) | **예** — 턴당 아니라 경기당 1회 비용 |
| **선행동작(캐릭터 모션)** | Punch-Out!!, GGST | 250–370ms 대역 `[OBSERVED]` (§1.2) | **예** — 0.5초 창에 정확히 들어감 |
| **턴 지연 수치화** | Gunbound | *"a 'delay' turn system which is influenced by the Mobile, the weapon and/or item"* `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Gunbound | 해당 없음(2인 교대) |
| **사후 흔적 판독** | Rampart, ItB | 벽 구멍이 다음 건설을 지배 `[OBSERVED]` (Rampart URL 위) | 예 — **이미 구현**(레인 A) |
| **텍스트 라벨** | castle-war 현재 | `SiegeAlarmSystem.cs:220` *"적 포격 준비 중..."* `[OBSERVED]` | 표본 12종 중 **모션 대신 텍스트에 의존하는 게임 0종** `[OBSERVED]` |

---

## 3. The short-window answer — 0.9초 문제에 대한 정면 답변

### 3.1 정직한 답: 비슷한 게임들은 **더 쓴다**

| 게임 | AI/자동 구간 지출 | 출처 |
|---|---|---|
| XCOM 2 | 행동 후 정지 1–3초, 엄폐 전환 ~2.75초, 오버워치 슬로우 33% | `[검색 요약만]` |
| Worms Armageddon | 길다 — GameSpot이 *"the length of time that it takes for such worms to complete their turns"*를 **결함으로 지적** `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Worms_Armageddon | |
| Fire Emblem | 전투 트리거 시 3인칭 컷 전환(수 초) | `[OBSERVED]` (§2) |
| TFT | 라운드 전체가 입력 0 자동 해결 | `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Teamfight_Tactics |
| **castle-war (과거)** | **3.0초** (0.4+0.5의 이전 값 1.5+1.5) | `GameManager.cs:2273-2277` 주석 `[OBSERVED]` |

**답: 그렇다, 비슷한 게임은 초 단위로 쓴다. 그리고 우리도 예전엔 3.0초를 썼다.**
하지만 같은 증거가 반대 방향도 가리킨다 — **더 쓰는 게임들이 정확히 플레이어가 깎는 게임들이다.**
XCOM 2에는 그 정지를 삭제하는 모드가 있고 `[검색 요약만]`, Worms의 AI 턴 길이는 리뷰 결함이었고
`[OBSERVED]`, 우리 코드 주석은 3.0초를 *"~17% of a whole match spent watching nothing"*이라 적고
직접 깎았다 `[OBSERVED]`. **시간으로 판독성을 사면 그 판독성이 삭제 대상이 된다.**

### 3.2 예산은 실제로 얼마나 여유가 있나 — 계산

전제는 코드 상수다: `TargetMatchSeconds=300`, `ToleranceFraction=0.2`,
`AverageTurnSeconds=7.5`, `EffectiveDamagePerTurn=37`, Stage1 material `14×85+300=1490`
`[OBSERVED — MatchLengthModel.cs:36-51, MatchLengthModelTests.cs:17]`.

```
밴드            : 240–360s
현재            : N=40.3턴, s=7.5 → T=302s
천장까지 여유    : 58s = 턴 평균 +1.44s
적 턴은 절반    → 적 비트만 늘릴 경우 +2.88s
⇒ 0.9s → 3.78s 까지도 모델상 밴드 안
   (보수적으로 tolerance 절반만 쓰면 0.9s → 2.34s)
```

**따라서 "0.9초는 늘릴 수 없다"는 전제는 모델 기준으로는 틀렸다.** 약 3배까지 여유가 있다
`[OBSERVED — 위 산식]`.

그런데 **써서는 안 되는 이유**가 같은 파일에 적혀 있다. 템포 패스는 `AverageTurnSeconds` 8.5→7.5와
동시에 `EffectiveDamagePerTurn` 42→37을 내렸고(주석: *"so faster turns lengthen the match instead
of silently shortening it"*) 결과적으로 T는 302s → 302s로 **동일**하다
`[OBSERVED — 계산으로 확인]`. 즉 절약한 2.1초는 이미 **더 많은 턴/발사로 재투자됐다.**
그 시간을 다시 죽은 공기로 되돌리는 것은 방금 내린 설계 결정을 되돌리는 것이다 `[INFERENCE]`.

### 3.3 결정적 재구성: 0.9초는 적 턴의 17%에 불과하다

선행 조사 실측 적 턴 총량 109.7초, 모델 40.3턴(적 턴 ≈20.1회) `[OBSERVED]`:

```
적 턴 1회 ≈ 5.44s
  죽은 공기        0.90s (17%)
  이미 모션 중     4.54s (83%)  ← 비행 + 탄착 + 정착
```

**결함은 "볼 시간이 없다"가 아니다. 4.54초 동안 화면은 이미 움직인다.**
그 모션에 **주체가 없을 뿐**이다 — 발사체가 선행동작 0프레임으로 등장하므로
(§1.5) 어디서 왔는지, 누가 쐈는지가 그 4.54초 어디에도 인코딩되지 않는다 `[INFERENCE — 코드 근거]`.

이것이 §1.1의 결론과 같다: **staging 문제이고, staging은 시간이 아니라 주의 배분으로 푼다.**

### 3.4 그래서 압축은 가능한가 — 예, 이미 충분하다

| 필요 | 근거 | 우리 창 0.5초 |
|---|---|---|
| 예고가 예고로 기능하는 하한 | ~300ms `[OBSERVED §1.3]` | 0.5s > 0.3s ✔ |
| 읽히는 무거운 선행동작 | 250–370ms `[OBSERVED §1.2]` | 0.5s ⊃ 0.37s ✔ |
| 시간 감속 강조 | 183–583ms 실출하 `[OBSERVED §1.2]` | 0.5s ⊃ 0.42s ✔ |
| 사운드 선도 | 8–10ms `[OBSERVED §1.3]` | 무시 가능 ✔ |

**0.9초는 이 문제에 짧지 않다. 이미 필요한 모든 장치가 들어가는 크기이고, 지금은 비어 있다.**
`SimpleAI.cs:30`의 주석이 그 창을 *"enough of a pause to read as the enemy taking aim"*이라고
적어 놓았는데 조준 계산이 창 뒤에 오므로 **의도는 정확했고 순서만 틀렸다** `[OBSERVED]`.

---

## 4. Non-text turn ownership — 텍스트 없이 주체를 인코딩하는 법

표본에서 확인된 방식과, 각각의 접근성 청구서:

| 방식 | 확인 | 접근성 청구서 |
|---|---|---|
| 유닛 측 색 구분 | castle-war 보유 — ally 0.55/0.78/1.0 vs enemy 팀 틴트 `[OBSERVED — UnitSpriteAnimator.cs:23-25, UnitController.cs:44]` | **색 단독 불가** — 아래 |
| 시점/프레이밍 전환 | Fire Emblem `[OBSERVED §2]` | 컷 비용 |
| 행동자 하이라이트 | ItB/StS/FE `[OBSERVED §2]` | **면제** — 아래 |
| 개시 시 전체 조망 | 포트리스2 `[OBSERVED §2]` | 없음 |
| 카메라 측면 이동 | XCOM 2 | 멀미 |
| 음악/앰비언스 전환 | Punch-Out!! — 입장 음악이 **상대별로 다르다**(단 Bald Bull·Mr. Sandman·Tyson은 없음) `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Punch-Out!!_(NES) | 청각 단독 불가 |

**색 단독 인코딩의 정확한 규칙** (WCAG 2.2 SC 1.4.1, Level A) `[OBSERVED]` `direct page retrieval`
https://www.w3.org/WAI/WCAG22/Understanding/use-of-color.html
- 위반: *"Color is not used as the only visual means of conveying information... or distinguishing
  a visual element."* 실패 사례 F81이 정확히 "색 차이만으로 구분"이다.
- **면책 조항이 있다:** *"If content is conveyed through the use of colors that differ not only in
  their hue, but that also have a significant difference in lightness, then this counts as an
  additional visual distinction, as long as the... contrast ratio [is] 3:1 or greater."*
  → 우리 팀 색(밝은 하늘색 vs 어두운 주황)은 **명도 차이를 3:1 이상으로 만들면 색 단독이 아니게 된다.**
  형태를 추가하지 않고도 규정을 만족하는 경로다 `[INFERENCE — 규정 적용]`.

**모션 인코딩의 정확한 규칙** (WCAG 2.2 SC 2.3.3) `[OBSERVED]` `direct page retrieval`
https://www.w3.org/WAI/WCAG22/Understanding/animation-from-interactions.html
- *"Motion animation triggered by interaction can be disabled, unless the animation is essential."*
  `essential`의 정의: *"information and functionality cannot be achieved in another way."*
  → **"누가 행동하는가"를 카메라로 전달하면 essential이 아니다** (하이라이트로 달성 가능하므로).
  따라서 카메라 이동은 **끌 수 있어야 한다** `[INFERENCE — 규정 적용]`.
- 결정적 제외 조항: *"Motion animation does not include changes of color, blurring, or opacity
  which do not change the perceived size, shape, or position of the element."*
  → **색/알파 하이라이트는 motion animation이 아니다 = 멀미 규정에서 아예 면제된다.** `[OBSERVED]`
- 부작용의 무게: *"Triggered reactions include nausea, migraine headaches, and potentially needing
  bed rest to recover."* `[OBSERVED]`

**결론:** 주체 인코딩의 최적 채널은 **행동자 하이라이트(색·알파)** 다. 표본에서 가장 흔하고
(§7), 프레임 비용 0이고, 멀미 규정에서 면제되고, 명도 대비 3:1로 색 단독 위반도 피한다 `[INFERENCE]`.

---

## 5. Idle vs acting — 행동이 사건으로 읽히기 위한 조건

**원칙(moving hold):** *"even characters sitting still, or hardly moving, can display some sort of
movement, such as breathing, or very slightly changing position. This prevents the animation from
becoming 'lifeless'."* `[OBSERVED]` `direct page retrieval`
https://en.wikipedia.org/wiki/Twelve_basic_principles_of_animation

**castle-war는 이 원칙을 이미 만족한다** — `idleBobAmplitude 0.045`, `idleBobSpeed 4.5`가
Grounded/Attacking 상태에서 sin 보빙을 돈다 `[OBSERVED — UnitSpriteAnimator.cs:15-16,186-188]`.

**그러나 같은 문헌이 실패 모드도 명시한다.** Secondary action 항목:
*"The important thing about secondary actions is that they emphasize, rather than take attention
away from the main action. **If the latter is the case, those actions are better left out.**"*
그리고 *"during a dramatic movement, facial expressions will often go unnoticed. In these cases,
it is better to include them at the beginning and the end of the movement, rather than during."*
`[OBSERVED]` `direct page retrieval` (동일 URL)

→ **모두가 항상 움직이면 아무것도 사건으로 읽히지 않는다.** 이것이 지금 castle-war의 상태다:

| 요소 | 상태 | 근거 |
|---|---|---|
| 유닛 idle 보빙 | 항상 | `UnitSpriteAnimator.cs:186-188` `[OBSERVED]` |
| **투석기 애니메이션** | **항상 8fps 루프** — `LoopFrameAt`이 *"wraps forever"*, 발사 트리거 **없음** | `DynamicBattlefield.cs:28-34`, `LaunchManager.cs:168-199` `[OBSERVED]` |
| 궤적선 색상 | 항상 애니메이션 (*"Animate trajectory line color over time to make it feel alive"*) | `LaunchManager.cs:575-578` `[OBSERVED]` |
| 턴 텍스트 점 | 항상 애니메이션 | `SiegeAlarmSystem.cs:219` `[OBSERVED]` |
| **발사 순간** | **상태 변화 0프레임** | `SimpleAI.cs:69-74` `[OBSERVED]` |

**투석기가 발사와 무관하게 영원히 같은 루프를 돈다.** 그래서 발사 시점에 투석기 화면은
발사 전과 **구별되지 않는다.** 사용자의 *"공격을 하는지 모르겠다"*에 정확히 대응하는 기계적 사실이다
`[INFERENCE — 코드 근거]`. 원칙대로면 극단 포즈(당김/방출)에 프레임을 몰아야 하는데(§1.1 slow in/out),
현재는 균일 루프이므로 극단이 존재하지 않는다 `[OBSERVED]`.

---

## 6. Anti-patterns — 근거 있는 역효과 사례

1. **AI 턴이 길어 리뷰 결함이 된다 (우리 장르에서 직접 확인).**
   GameSpot의 Worms Armageddon 리뷰가 *"the AI-controlled worms' nearly perfect accuracy and
   **the length of time that it takes for such worms to complete their turns**"*를 단점으로 적었다.
   `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Worms_Armageddon
   → 포병 턴제에서 AI 턴 길이는 **관측된 실패**다. §3.1의 "더 쓴다"를 그대로 따라가면 이 결과가 나온다.

2. **속도를 올려도 학습은 해결되지 않는다 (오토배틀러 1차 증거).**
   TFT 게임 디렉터 Peter Whalen이 Hyper Roll 폐지를 설명하며:
   *"Hyper Roll was released...with the goal of delivering a more straightforward, shorter TFT
   experience that could help new players learn the game... What happened on release was pretty
   different—Hyper Roll was certainly faster, but **the lack of downtime meant you still needed a
   ton of set familiarity and a deep understanding of tempo to stay alive**... We knew Hyper Roll
   missed our intended goals. **Players still lacked an effective tool for learning TFT.**"*
   `[OBSERVED]` `direct page retrieval` https://en.wikipedia.org/wiki/Teamfight_Tactics
   → **이 조사에서 가장 값진 인용이다.** 시간을 줄이는 것도 늘리는 것도 판독성을 주지 않는다.
   판독성은 **그 시간에 무엇이 인코딩되는가**의 문제다. 우리 0.9초가 비어 있는 것이 정확히 그 결함이다.

3. **카메라 연출은 끄는 기능으로 귀결된다.**
   XCOM 2는 Zip Mode(애니메이션 가속)와 Action Cam 비활성 옵션을 제공하고, 커뮤니티는 여기에 더해
   정지 구간을 삭제하는 모드를 쓴다 `[검색 요약만 — §8 갭]`.
   규정 측 근거는 확실하다: WCAG 2.3.3은 essential이 아닌 모션은 **끌 수 있어야 한다**고 요구하고,
   반응으로 *"nausea, migraine headaches"*를 명시한다 `[OBSERVED §4]`.

4. **예고 과다는 긴장이 아니라 정답 찾기를 만든다.**
   선행 조사가 ItB Steam 부정 리뷰(*"전략이 아니라 퍼즐"* +11)로 확인함
   `[OBSERVED — .survey/siege-visibility-and-telegraph/solutions.md:16,75]`.
   ItB의 텔레그래프 목적이 *"내 턴에 적의 계획을 교란"*인데 우리는 교란 수단이 0이다 `[OBSERVED]`.

5. **UI 요소를 결함마다 하나씩 늘리면 아이콘 쓰레기장이 된다.**
   ItB가 2년 쓰고 되돌아온 경로 `[OBSERVED — 선행 조사 solutions.md:31]`.
   → 본 조사의 권고가 UI 추가가 **아니어야** 하는 이유.

---

## 7. Frequency ranking — 표본 12종 집계

"AI/자동 행동을 *지금 일어나는 일*로 읽히게 하는 장치"의 보유 빈도.
분모는 12. 확인한 것만 센다(부재 확인 못 한 것은 미보유로 세지 않고 §8에 기록).

| 순위 | 장치 | 빈도 | 확인된 게임 |
|---|---|---|---|
| 1 | **행동 결과 피드백**(피해 수치·히트 이펙트·화면 흔들림) | **12/12** | 전부 — 선행 조사 집계와 일치 `[OBSERVED]` |
| 2 | **행동자 시각 강조**(하이라이트/틴트/선택 표시) | **8/12** | ItB · XCOM2 · FE · StS · Worms · Gunbound · 포트리스2 · TFT `[OBSERVED — 각 §2]` |
| 3 | **사운드로 주체·사건 식별** | **7/12** | Punch-Out!!(상대별 입장 음악, 14명 중 11명 `[OBSERVED]`) · L4D(감염체별 `[검색 요약만]`) · XCOM2 · FE · StS · Worms · Gunbound `[뒤 5종은 INFERENCE — 개별 확인 안 함]` |
| 4 | **의도적 정지/비트** | **5/12** | XCOM2 · FE · TFT · Worms · Rampart `[OBSERVED]` |
| 5 | **카메라 이동/시점 전환** | **4/12** | XCOM2 · FE · Worms · TFT `[OBSERVED]` |
| 6 | **캐릭터 선행동작이 판독의 주 채널** | **2/12** | Punch-Out!! · GGST `[OBSERVED]` |
| 7 | **시간 감속** | **1/12** | GGST(카운터히트 11–35F) `[OBSERVED]` |
| 8 | **개시 시 전체 조망 비트** | **2/12** | 포트리스2 · Rampart `[OBSERVED]` |
| — | **모션 대신 텍스트 라벨에 의존** | **0/12** | 없음. castle-war만 해당 `[OBSERVED]` |

**표본이 말하는 두 가지.**

- **선행동작은 턴제에서 오히려 드물다(2/12).** 둘 다 실시간 입력이 있는 게임이다. 즉 §1의 프레임
  수치는 *상한 근거*로 쓰되, 우리의 주 채널로 삼을 근거는 약하다 `[OBSERVED — 집계]`.
- **가장 흔한 두 장치는 우리가 이미 부분 보유한 것들이다** — 결과 피드백(12/12, 보유)과
  행동자 강조(8/12, **팀 틴트는 있으나 "지금 행동 중"을 표시하는 상태 강조는 없음**)
  `[OBSERVED — UnitSpriteAnimator.cs:23-25 vs 부재]`.
  결손은 새 장치가 아니라 **기존 장치의 미연결**이다.

---

## 8. Key gaps — 못 채운 것

기록하는 편이 확신보다 낫다는 원칙대로, 메울 수 없었던 것을 명시한다.

1. **XCOM 2의 정지 시간 수치를 1차 출처로 확정하지 못했다.**
   1–3초 행동 후 정지, ~2.75초 엄폐 전환, 33% 오버워치 슬로우는 **검색 요약에서만** 나왔다.
   Steam Workshop 페이지 직접 조회를 시도했으나 내가 추정한 ID(1122974240)는 다른 모드
   (robojumper's Squad Select)였고 `[OBSERVED — 조회 결과]`, 정확한 ID를 확정하지 못했다.
   Firaxis의 Zip Mode 공식 패치노트도 원문 미확보. **§3.1 표의 XCOM 행은 인용 등급이 낮다.**
   → 후속: Zip Mode 도입 패치노트(2016-03) 원문과 해당 모드 페이지 확정.

2. **Left 4 Dead의 "clear visual language"를 1차 출처로 못 잡았다.**
   검색 결과 자체가 *"no single GDC talk focuses exclusively on"* 이 주제라고 답했다.
   Mike Booth의 GDC 2009 강연이 유력 후보지만 확인하지 못했다. **§1.4와 §7의 L4D 항목은
   추정이다.** 과제가 명시적으로 요청한 소스인데 못 채웠다.

3. **Fire Emblem의 전투 애니메이션 on/off 토글을 1차 출처로 못 잡았다.**
   fireemblem.fandom.com, fireemblemwiki.org, serenesforest.net 모두 404
   `[OBSERVED — 조회 결과]`. 시리즈에 이 옵션이 있다는 것은 널리 알려졌지만 **확인하지 못했으므로
   주장하지 않는다.** 이 토글은 "판독 연출을 플레이어가 끈다"의 가장 오래된 사례일 것이므로
   §6.3을 강화할 수 있었다.

4. **Dark Souls 텔레그래프에 대한 개발자 발언을 찾지 않았다.**
   과제에 후보로 적혀 있었으나, 우리 구조(입력 0)와의 거리가 Punch-Out!!보다 멀고 §7 집계가
   이미 "선행동작은 턴제에서 드물다"를 보였으므로 우선순위를 내렸다. **의도적 미조사다.**

5. **Street Fighter 프레임 데이터 대신 Guilty Gear Strive를 썼다.**
   supercombo.gg(SF6)는 HTTP 403, infil.net 용어집은 본문 미렌더
   `[OBSERVED — 조회 결과]`. Dustloop(GGST)이 열려서 그것으로 대체했다.
   둘 다 60fps 2D 격투이므로 대역 추정에는 무해하나, **과제가 지정한 게임은 아니다.**

6. **"turn-based에서 선행동작이 드물다"의 부재 증명은 하지 않았다.**
   §7의 2/12는 *확인된 보유*를 센 것이다. 나머지 10종에 선행동작이 **없다**는 것을 개별 확인하지
   않았으므로, 이 수치는 하한이다 `[OBSERVED — 집계 방식]`.

7. **0.9초 예산 산식은 모델 상수 기반이고 실측이 아니다.**
   `AverageTurnSeconds`는 측정값이 아니라 설계 상수다(`MatchLengthModel.cs:42-45`가 그렇게 적고
   있다) `[OBSERVED]`. §3.2의 "3.78초까지 여유"는 **모델이 허용하는 값**이고 실제 체감이 아니다.
   실측 없이 이 여유를 쓰는 결정을 해서는 안 된다 `[INFERENCE]`.

---

## 9. 요약 — 이 레인이 확정한 것

1. **결함은 시간 부족이 아니다.** 적 턴 1회 5.44초 중 4.54초(83%)는 이미 모션 중이고,
   0.9초 죽은 공기는 17%다 `[OBSERVED §3.3]`.
2. **결함은 staging이다** — 그 4.54초의 모션에 **주체가 인코딩되지 않는다.** 발사가 선행동작
   0프레임이고(`SimpleAI.cs:69-74`), 투석기는 발사와 무관한 영구 루프이며
   (`DynamicBattlefield.cs:28-34`), 주체는 텍스트로만 알려진다(`SiegeAlarmSystem.cs:220`)
   `[OBSERVED §1.5, §5]`.
3. **0.9초는 이 문제에 충분하다.** 예고 하한 ~300ms, 읽히는 선행동작 250–370ms, 실출하 시간 감속
   183–583ms — 모두 0.5초 창에 들어간다 `[OBSERVED §3.4]`.
4. **늘릴 수는 있지만(모델상 3.78초까지) 늘려서는 안 된다.** 더 쓰는 게임들이 정확히 플레이어가
   깎는 게임들이고(Worms 리뷰 결함, XCOM 모드), 우리가 절약한 2.1초는 이미 더 많은 발사로
   재투자됐다 `[OBSERVED §3.1-3.2]`.
5. **TFT Hyper Roll이 결론을 못박는다:** 빠르게 만드는 것도 학습을 주지 않았다. 시간의 양이 아니라
   **그 시간에 무엇이 들어가는가**가 판독성이다 `[OBSERVED §6.2]`.
6. **표본이 지목하는 1순위 채널은 행동자 강조(8/12, 개별 확인됨)다** — 화면 요소 0개 추가,
   WCAG 2.3.3 모션 면제(색·알파), 명도 대비 3:1로 색 단독 위반도 회피 `[OBSERVED §4, §7]`.
   **사운드는 근거 등급이 낮다**(7/12 중 5종이 미확인 추정, §7). 다만 채널 자체의 이점은
   표본과 무관하게 성립한다 — 청각 8–10ms vs 시각 20–40ms `[OBSERVED §1.3]`.
7. **선행동작은 턴제의 주 채널이 아니다(2/12).** 우리 0.5초 창에 들어가긴 하지만, 표본 근거는
   보조 역할을 지지한다 `[OBSERVED §7]`.
