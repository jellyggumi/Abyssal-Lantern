# Lane D — JTBD 대체재: "지금 무슨 일이 일어나는지 알리기"

- slug: `siege-visibility-and-telegraph`
- lane: D (JTBD 대체재 / 간접 대체재 / 타 산업 병행)
- owner: Survey Lane D
- created: 2026-08-13
- mode: `market-landscape` (레인 D 기본형 — **platform-map 아님**. 이 주제는 플랫폼 비교가 아니다)
- **코드 수정 없음. 조사 문서 1개.**
- 선행 조사와의 관계: `.survey/siege-artillery-landscape/`(12개 비교작)와
  `_workspace/current/design/trend-survey/rally-structure-and-pikachu-volley.md`(D1~D9 관여 장치)는
  **UI/메커니즘 차원의 텔레그래프(D6)** 를 이미 다뤘다. 본 문서는 그 반대편,
  **UI를 쓰지 않고 같은 과업을 푸는 경로**만 다룬다.
- 자매 레인과의 분담: 예고 **과다**의 역효과(게임 내부 클러터)는 Lane C 담당.
  본 문서는 그 논지의 **게임 밖 증거**(병원 알람 피로 등)만 §3에서 인용한다.

---

## 0. 요약 — 이 레인의 결론 3줄

1. **과업을 잘못 잡고 있었다.** 사용자의 불만은 "정보가 없다"가 아니라
   **"화면이 나에게 거짓을 말한다"** 이다. 우리 화면의 지시문 2개가 실제로 거짓이고
   `[OBSERVED — 코드]`, 거짓 지시는 정보 부족보다 나쁘다(§3.4 알람 피로).
   → **가장 값싼 개선은 새 요소 추가가 아니라 거짓 문구를 참인 상태 표현으로 교체하는 것이다.**
   ⚠️ **삭제가 아니다** — QA UX-003이 *"문구만 지우는 것은 최악"* 이라고 명시한다
   `[OBSERVED — QA 계측]`. §3.5 TCAS가 같은 답을 냈다(모호한 문구를 **지우지 않고 교체**).
   상세는 §4.2.
2. **텔레그래프는 템포를 늦추지 않는다. 빠르게 한다.** Into the Breach 개발 기록이
   *"telegraphing the Vek's movements further helped to hasten the pace"* 라고 직접 적었고
   `[direct page retrieval]`, 철도 원거리 신호기는 **속도를 올리기 위해** 도입됐다
   `[direct page retrieval]`. 경기 300초 밴드는 텔레그래프의 제약이 아니라 **근거**다.
3. **이 프로젝트의 진짜 병목은 "계산은 되는데 렌더되지 않는다"이며, 현재 2곳에 남아 있다**
   `[OBSERVED — 코드]`:
   - `SimpleAI.cs:28-30` — "적이 조준하는 것으로 읽히도록" 설계된 0.9초 창. 주석에만 의도가 있고 렌더 0
   - `ProjectileForTurn` (UX-004, S2) — 완전 결정론적 public 함수인데 소비처가 프리팹 선택뿐, 예고 렌더 0

   > **셋째 사례였던 `windText`(UX-001)는 철회한다.** 초판/개정1은 이것을 현재 결함으로 적었으나,
   > **런타임 입양 경로가 존재하고 도달한다** — `GameManager.cs:309 Start()` → `:321 SetupUIButtons()`
   > → `:1129 HudCanvas.Adopt(windText)` → `HudCanvas.Root()`/`Resolve()`가 캔버스를 실제 생성
   > (`HudCanvas.cs:51-90`). `Adopt`의 조기 반환은 `rect.parent == root` 하나뿐이고
   > `windText`의 부모는 null이므로 통과한다(`HudCanvas.cs:119`) `[OBSERVED — 코드]`.
   > 결정적으로 `GameManager.cs:1124-1128`의 주석이 **과거형**이다 —
   > *"TMP drew nothing **while** UpdateUI kept writing to them ... **Adoption is what makes them
   > appear**"*. 즉 UX-001은 **발견 시점의 결함이고 이미 조치됐다.**
   > 씬의 `m_Father: 0`은 결함의 증거가 아니라 런타임 입양이 존재하는 이유다.
   > (Lane B가 최초 지적, Lane A가 재검증, 본 레인이 위 경로를 독립 확인.)

   → **정보를 만드는 일은 이미 끝나 있다. 남은 일은 그것을 화면에 도달시키는 것이다.**
   따라서 이 레인의 "화면 요소 증가 0" 경로들(§2)은 정보를 새로 만드는 것이 아니라
   **이미 계산된 정보의 출력 채널을 여는 것**이다.
   그리고 **`windText`는 그 일이 실제로 가능하다는 증거다** — 같은 병목이 한 번 해결됐다.

---

## 1. 과업(JTBD) 재정의

사용자 원문은 세 가지를 한 문장에 묶어 놓았다. 분리하면 대체재의 후보군이 달라진다.

| # | 사용자의 말 | 실제 과업 | 이 과업의 정보 방향 |
|---|---|---|---|
| J1 | "내 캐릭터가 돌을 쏠 때 어떻게 써야 되는지도 안 보이고" | **내 행동의 결과 예측** | 이미 해결됨 — 궤적 프리뷰가 실전 물리와 동일 `[OBSERVED — 코드]` |
| J2 | "적이 어떻게 쏘는지도 안 보인다" | **상대 의도의 사전 공개** | 미해결 — 예고 0초 |
| J3 | "전체적인 플레이를 어떻게 해야 되는지 모르겠고" | **내가 지금 할 수 있는 행동의 목록** | **역행 중** — 지시문 2개가 거짓 |

> **핵심 관찰**: J1은 이미 풀려 있다. 그런데 사용자는 J1도 "안 보인다"고 말했다.
> 궤적선은 존재하고(`LaunchManager.cs:18` `trajectoryLine`, 300스텝 × 0.02s = 6초 예측)
> 실전과 동일한 적분기를 쓴다 `[OBSERVED — 코드]`.
> **기능이 있는데 없다고 느끼는 것은 가시성 문제가 아니라 신뢰 문제다** `[INFERENCE]`.
> J3에서 화면이 두 번 거짓말을 하면, 사용자는 화면 전체를 신뢰하지 않게 된다 —
> 이것이 §3.4의 알람 피로와 같은 구조다.

---

## 2. 간접 대체재 — UI로 보여주지 *않고* 푸는 6가지

각 항목: 실제 게임 → 그 게임이 실제로 하는 것 → castle-war 적용 → 우리 화면 요소 증가분.

### 2.1 애니메이션 선행동작 (wind-up) — *Punch-Out!!* (Nintendo, 1987)

**그 게임이 하는 것**: 상대 복서마다 **정해진 패턴**이 있고, 위키가
*"The behavior of each opposing boxer follows a set pattern requiring trial and error and
memorization to defeat them"* 라고 명시한다 `[direct page retrieval]`.
결정적인 것은 보상 구조다 — 가장 강한 공격인 어퍼컷은 별(star)이 있어야 쓸 수 있고,
별은 *"counter-punching the opponent directly before or after certain attacks are launched"* 로만
얻는다 `[direct page retrieval]`.

> **즉 이 게임은 "예고를 읽는 것"에 게임 내 최강 자원을 걸어 두었다.**
> 예고는 친절이 아니라 **점수원**이다. GamesRadar가 이 게임을
> *"brilliant puzzle game [disguised] as a sports game"* 이라 부른 이유다 `[direct page retrieval]`.

**castle-war 적용**: 적 투석기의 팔이 당겨지는 동작만으로 "무엇이 온다"를 말할 수 있다.
발사체는 이미 **규칙으로 공개**되어 있다 — `OneShotSiegeRules.ProjectileForTurn(completedTurns)`가
`round = completedTurns / 2`로 Knight→Archer→Barrel을 순환시키고 양 진영이 동일하다
`[OBSERVED — 코드]`. 따라서 적의 탄종은 **비밀이 아니고**, 선행동작 3종을 구분해 주는 것은
정보 누설이 아니라 이미 공개된 규칙의 시각화다.

**화면 요소 증가분: 0** (기존 스프라이트의 애니메이션)

### 2.2 사운드 큐 — *Left 4 Dead* (Valve, 2008)

**그 게임이 하는 것**: 위키가 특수 감염체를
*"Each of the special infected have a distinctive sound, silhouette, and musical cue,
making their presence easily recognizable by players"* 라고 기술한다 `[direct page retrieval]`.
음악 담당 Director가 별도로 존재하며, 플레이어마다 **개별 믹스**를 실시간 생성한다
`[direct page retrieval]`. 커뮤니티 정리에 따르면 거리까지 악기로 인코딩된다 —
피아노=가까움, 현악=멀다 `[indexed snippet]`.

**castle-war 적용**: 적 턴 0.9초 동안 탄종별 장전음 3종. 화면은 그대로다.
우리 게임은 1v1 좌우 대칭이라 **스테레오 패닝만으로 좌/우 진영을 구분**할 수 있다 `[INFERENCE]`.

**화면 요소 증가분: 0** (화면에 아무것도 안 그린다)

### 2.3 발사체·이펙트 자체가 예고 — *Left 4 Dead* / *Superhot* (Superhot Team, 2016)

이것이 이 레인에서 가장 저평가된 경로다. **HUD가 아니라 월드에 있는 오브젝트가 정보를 나른다.**

**L4D**: *"Additional communication of player actions is conveyed through lights;
weapon-mounted flashlights and muzzle flashes help players determine when their companions
are shooting, performing melee attacks, reloading or moving"* `[direct page retrieval]`.
즉 **총구 화염이 팀원 상태창을 대체한다.**

**Superhot**: The Verge 평이 인용된 부분 —
*"the inclusion of a red trail to show the path of bullets that subtly allow the player to
identify their source"* `[direct page retrieval]`.
**탄도 궤적이 곧 "누가 어디서 쏘는가"의 UI다.**

**castle-war 적용 — 단, 이미 있는 것부터 확인했다** `[OBSERVED — 코드]`:

| 요소 | 현재 상태 | 남은 공백 |
|---|---|---|
| 팀 구분 색 | **이미 있음** — 아군 하늘색 `(0.45,0.85,1)`, 적 붉은색 `(1,0.35,0.25)` (`UnitController.cs:487`) | 없음 |
| 탄종 구분 | **부분** — Barrel만 주황 `(1,0.65,0.12)`으로 덮어씀 (`:488`). Knight·Archer는 팀색 공유 | Knight/Archer 구분 |
| 굵기 | 이미 탄종별 — Barrel 0.14, 그 외 0.09 (`:483`) | 없음 |
| **비행 후 잔존** | **없음** — `time = 0.5f`(`:482`)이고 착탄 시 `emitting = false`(`:636`, `:1174`, `:1194`) | **샷 이력** |

> ⚠️ **내 초판의 "적 탄 탄종별 트레일 색 구분" 권고는 절반이 이미 구현돼 있었다.**
> 팀 색과 굵기는 있고, 빠진 것은 Knight/Archer 구분 하나뿐이다.
> Lane A가 경고한 "결함표를 일반화하지 말고 항목별로 확인하라"가 **내 권고에도 적용됐다.**

**따라서 이 절의 실제 공백은 색이 아니라 시간축이다 — 샷 이력이 남지 않는다.**
트레일은 0.5초이고 착탄과 함께 소멸하므로, **여러 샷에 걸친 조준 학습이 축적되지 않는다.**
선행 조사가 장르 선례를 이미 기록해 두었다 — Scorched Earth(1991)는
**이전 샷 궤적선을 표시**했다 (`.survey/siege-artillery-landscape/solutions.md`) `[OBSERVED — 선행 조사]`.
35년 전 선례가 있는 기능이다.

- **적용**: 착탄 지점에 흐릿한 마커 또는 직전 궤적 1~2개를 잔존시킨다(페이드)
- **§3.3(NFL 노란 선)과 정합**: 월드 좌표에 그리고, 전경이 가리게 둔다
- **§2.6(패턴 규칙성)과 결합**: 적의 샷이 누적되면 **적의 조준 패턴 자체가 읽히는 정보**가 된다

**화면 요소 증가분: 0** (월드 렌더, HUD 카운트에 들어가지 않음)

### 2.4 시간 감속 / 정지 — *Superhot* (2016), *Super Smash Bros.* 시리즈

**Superhot**: *"time within the game progresses at normal speed only when the player moves;
this creates the opportunity for the player to assess their situation in slow motion"*
`[direct page retrieval]`. 태그라인이 *"Time Moves Only When You Move"* 다.
**시간 자체가 정보 전달 채널이다** — 화면에 요소를 하나도 더하지 않고 "읽을 시간"을 만든다.

**Smash Bros. 히트랙(hitlag/hitstop)**: SmashWiki가 기능을 둘로 명시한다 —
*"The first is a visual indicator that an attack connects; the brief moment where both fighters
freeze gives both players more time to plan their next moves"* `[direct page retrieval]`.
피해량이 클수록 히트랙이 길고, 상한은 *Brawl* 이후 30프레임 `[direct page retrieval]`.
Sakurai 본인이 이 정지를 무술 영화의 타격 강조 기법에 비유했다 `[indexed snippet]`.

> **castle-war에 직접 적용되는 규칙**: **피해가 클수록 정지를 길게.**
> 우리는 이미 착탄 후 홀드 0.35초를 갖고 있다. 이것을 **피해량 비례**로 만들면
> "방금 큰 일이 일어났다"가 숫자 없이 전달된다 `[INFERENCE]`.

**화면 요소 증가분: 0** (`Time.timeScale` 또는 기존 홀드 시간의 함수화)

### 2.5 카메라 워크 — *Peggle* (PopCap, 2007)

**그 게임이 하는 것**: 개발 기록에 정확히 남아 있다 — 마지막 오렌지 펙이 남았을 때
*"adding a zoom on the current ball as it neared the last orange peg to be cleared"*
`[direct page retrieval]`. 팀은 원래 파칭코식 화려한 연출을 원했지만
플레이어가 **단순한 플레이스홀더에 더 잘 반응**해서 그쪽을 다듬었다 `[direct page retrieval]`.

> **이것이 우리와 같은 계보의 증거다** — Peggle도 포물선/충돌 물리 게임이고,
> **"결정적 순간"을 UI 텍스트가 아니라 카메라로** 표시했다.

**참고**: *Worms Armageddon*은 기본 설정에서 카메라가 활성 웜과 이동하는 발사체를
자동 추적한다(Scroll Lock으로 토글) `[indexed snippet]`.
포병 계보가 이미 "카메라가 시선을 대신 옮긴다"를 표준으로 쓰고 있다는 뜻이다.

**castle-war 적용**: 적 턴에 카메라가 적 진영으로 팬 → 발사 → 탄을 따라 이동.
현재 적 턴 109.7초(경기의 34.1%) 동안 화면이 정지해 있는데, 이 시간이
**"볼 것이 없는 시간"에서 "보여 주는 시간"으로** 바뀐다.

**화면 요소 증가분: 0** (카메라 변환)

### 2.6 패턴의 반복 자체가 예고 — *Punch-Out!!* / *Advance Wars* (2001)

**Punch-Out!!**: §2.1의 "set pattern" — **예고는 1회성 신호가 아니라 학습 가능한 규칙성**이다.
**Advance Wars**: 디렉터 Shimojo가 게임 템포를 *"waves of excitement"* 로 설계했고,
긴 영화가 좋은 페이싱을 위해 **의도적으로 조용한 구간**을 넣는 것에 비유했다
`[direct page retrieval]`.

> **castle-war에 주는 교정**: 유휴 62.2%를 전부 없애는 것이 목표가 아닐 수 있다.
> 문제는 **조용한 구간이 있다는 것이 아니라, 그 구간에 아무 정보도 흐르지 않는다는 것**이다
> `[INFERENCE]`.

**화면 요소 증가분: 0** (규칙을 일관되게 유지하는 것)

### 2.7 간접 대체재 요약표

| # | 경로 | 실제 게임 | 채널 | 우리 적용 지점 | 화면 요소 증가 |
|---|---|---|---|---|---|
| 2.1 | 선행동작 | *Punch-Out!!* (1987) | 애니메이션 | 적 0.9초 창 | 0 |
| 2.2 | 사운드 큐 | *Left 4 Dead* (2008) | 오디오 | 적 0.9초 창 | 0 |
| 2.3 | 오브젝트가 UI | *L4D* / *Superhot* | 월드 렌더 | 적 탄 트레일 | 0 |
| 2.4 | 시간 감속·정지 | *Superhot* / *Smash* | 시간 | 착탄 홀드 0.35초 | 0 |
| 2.5 | 카메라 워크 | *Peggle* (2007) | 카메라 | 적 턴 전체 | 0 |
| 2.6 | 패턴 규칙성 | *Punch-Out!!* / *Advance Wars* | 설계 일관성 | 탄종 순환 | 0 |

**6개 경로 전부 화면 요소 증가분이 0이다.** 이것이 이 레인의 실질적 발견이다.

---

## 3. 타 산업 병행 사례 — "상대의 다음 행동을 미리 알리는" 설계

각 항목 끝에 **우리가 빌려올 원칙 한 줄**을 붙였다.

### 3.1 자동차 방향지시등

- 점멸 주기가 **분당 60~120회(1~2 Hz)로 규격화**되어 있다 `[direct page retrieval]`.
- 색도 규격화 — 1949 제네바 / 1968 비엔나 협약 이후 방향지시등은 **호박색(amber)** `[direct page retrieval]`.
- 규격화의 효과가 측정되어 있다: NHTSA 2008 연구는 후방 지시등이 적색 대신 호박색인 차량이
  특정 유형 충돌에 연루될 확률이 **최대 28% 낮다**고 시사하고, 2009년 후속 연구가 유의한
  전체 안전 이득을 확인했으며, 2015년에는 **호박색이 적색과 비슷한 비용으로 제공 가능**하다고
  판정했다 `[direct page retrieval]`.
- 조작이 **자기 취소(self-cancelling)** 된다 — 1940년 개발, 핸들이 직진으로 돌아오면 해제
  `[direct page retrieval]`.

> **우리가 빌릴 원칙**: **신호는 한 종류·한 리듬·한 색으로 고정하라 — 규격화된 저비용 신호 하나가
> 측정 가능한 안전 이득을 낸다(28%). 그리고 신호는 끝날 때 스스로 사라져야 한다.**

### 3.2 철도 원거리 신호기 (distant signal)

- 초기 신호는 정지/진행뿐이었고, 교통 밀도가 올라가자 **정지 신호 접근로에 원거리 신호기**를 추가했다.
  운전자에게 "곧 정지를 요구할 신호가 있다"를 미리 알린다 `[direct page retrieval]`.
- **도입 이유가 결정적이다**: *"This allowed for an overall increase in speed, since train drivers
  no longer had to drive at a speed within sighting distance of the stop signal"*
  `[direct page retrieval]`.
- 고장 시 규칙: 소등·이상 신호는 **가장 제한적인 지시(정지)로 해석**해야 한다 `[direct page retrieval]`.
- 점멸 황색은 "두 번째 앞 신호가 정지", 상시 황색은 "다음 신호가 정지" — **예고의 깊이가 계층화**되어 있다
  `[direct page retrieval]`.

> **우리가 빌릴 원칙**: **예고는 속도의 적이 아니라 속도의 조건이다 — 미리 알려 주면 더 빨리 달릴 수 있다.
> 예고 없이는 "볼 수 있는 거리"까지 느려져야 한다.**

> ⚠️ **이 항목이 300초 밴드 제약을 정면으로 다룬다.**
> "텔레그래프를 넣으면 경기가 길어진다"는 직관은 철도에서 **반대로** 판명됐다.
> 게임 쪽 독립 증거도 있다 — Into the Breach는 XCOM의 1시간 전투와 대조적으로 짧은 전투를 원했고,
> *"Subset found that telegraphing the Vek's movements further helped to hasten the pace"*
> `[direct page retrieval]`. **두 산업이 같은 결론에 도달했다.**

### 3.3 스포츠 중계 그래픽 — NFL 노란 1st & Ten 선

- 삽입된 그래픽이 **경기장 좌표에 고정**되고, **전경 객체가 배경을 가리는 시각 규칙을 따른다**
  `[direct page retrieval]`.
- 선은 **실제 경기장에 없다. TV 시청자만 본다** `[direct page retrieval]`.
- 경기장마다 배수용 곡면이 달라 **시즌당 1회 3D 모델을 만들고**, 카메라에 팬/틸트/줌/포커스
  엔코더를 달아 초당 30회 이상 자세를 전송한다 `[direct page retrieval]`.
- 확장 사례가 원칙을 증명한다: 4다운에 선이 적색으로 바뀌고, 눈/안개로 경기장 표시가 전부 가려지면
  **가상 경기장 전체를 투영**한다 `[direct page retrieval]`.

> **우리가 빌릴 원칙**: **규칙을 화면 구석의 패널이 아니라 월드 좌표에 그려라 —
> 그리고 그것이 월드의 가림 규칙을 따르게 하라. 시선을 옮기지 않아도 읽힌다.**

> **castle-war 직접 대응**: 적 궤적 프리뷰는 HUD가 아니라 **월드에 그려지는 선**이다.
> 즉 §4의 "화면 요소 증가 0" 주장과 정합한다.

### 3.4 병원 환자 모니터 — 알람 피로 (alarm fatigue)

**이것은 "하지 말아야 할 것"의 증거다.**

- 정의: 경보가 과다하면 작업자가 **둔감해져 무시하거나 적절히 대응하지 못한다** `[direct page retrieval]`.
- 규모: 미국 FDA가 2005~2008년 **무시된 경보로 인한 사망 566건**을 집계했고,
  Joint Commission 감시사건 보고는 수년간 **경보 관련 사망 80건·중상 13건**을 기록했다
  `[direct page retrieval]`. ECRI Institute는 2007년부터 경보를 위험 상위 10위에 올렸고
  **2014년에는 1위**였다 `[direct page retrieval]`.
- **우회 행동이 핵심 관찰이다**: 결과 목록에 *"misuse of monitor equipment including
  'work-arounds' such as turning down alarm volumes or adjusting device settings"* 가 있다
  `[direct page retrieval]`. **신호를 늘리면 사용자가 신호를 끈다.**
- 교통 사례가 정량적이다: 2009년 워싱턴 DC 열차 충돌에서 궤도회로 경보가 **주당 약 8,000건**
  발생했고, NTSB는 *"the extremely high incidence of track-circuit alarms would have thoroughly
  desensitized [the dispatchers]"* 라고 결론했다 `[direct page retrieval]`.
- 캘리포니아 Proposition 65: 경고 라벨을 남발하면 *"meaningless warnings"* 가 되고,
  **불필요한 경고를 붙이는 데 벌칙이 없어서** 과다 경고가 구조적으로 발생한다 `[direct page retrieval]`.

> **우리가 빌릴 원칙**: **틀린 신호 하나는 없는 신호보다 나쁘다 — 사용자는 개별 신호를 끄지 않고
> 신호 체계 전체를 끈다. 그래서 첫 작업은 새 요소 추가가 아니라 거짓 신호를 참으로 만드는 것이다
> (지우는 것이 아니다 — §4.2).**

> **castle-war 직접 대응**: 우리 화면은 지금 **두 번 거짓말한다** `[OBSERVED — 코드]`:
> - `SiegeAlarmSystem.cs:225` — 적 턴에 `"적 포격 준비 중…  ·  클릭: 벽돌 예약"` 을 표시하지만,
>   `BrickPlacementController.cs:76`이 `EnforcesOneShotTurns`일 때 early-return으로 막는다.
> - `LaunchManager.cs:121` — `"아무 곳이나 당겨 발사"` 가 적 턴에도 남는데 조준이 차단되어 있다.
>
> Proposition 65와 같은 구조다 — **틀린 안내를 남겨 두는 데 아무 벌칙이 없어서 남아 있다.**

### 3.5 항공 충돌회피 — TCAS / ACAS II

- **정보와 지시가 분리되어 있다.** TA(Traffic Advisory)는 *"Traffic, traffic"* 만 말하고
  *"does not offer any suggested remedy; it is up to the pilot to decide what to do"* 다.
  RA(Resolution Advisory)는 *"Climb, climb"* / *"Descend, descend"* / *"Level off, level off"* —
  **해야 할 행동을 지정**한다 `[direct page retrieval]`.
- **RA는 관제 지시보다 우선한다** (FAA/EASA 규칙) `[direct page retrieval]`.
  두 기체가 서로 반대 방향 RA를 받도록 **조율**된다 `[direct page retrieval]`.
- 효과: 안전도를 **3~5배** 개선한 것으로 추정된다 `[direct page retrieval]`.
- **그런데 모호한 문구는 실제로 실패했다.** 7.1 버전에서
  애매한 *"Adjust Vertical Speed, Adjust"* 를 *"Level off, Level off"* 로 **교체**했다 —
  이유가 문서에 *"to prevent improper response by the pilots"* 로 명시되어 있다
  `[direct page retrieval]`.
- 한계도 정직하게: Eurocontrol 2017 ACAS 가이드는 **약 25%의 경우 조종사가 RA를 부정확하게 따른다**고
  보고했다 `[direct page retrieval]`.

> **우리가 빌릴 원칙**: **"무엇이 오는가"(정보)와 "무엇을 하라"(지시)를 분리하고, 지시문은
> 모호하면 실패한다 — 애매한 문구는 고치는 게 아니라 명령형으로 교체하라.**

### 3.6 타 산업 요약표

| # | 도메인 | 장치 | 빌릴 원칙 (한 줄) |
|---|---|---|---|
| 3.1 | 자동차 | 방향지시등 1~2 Hz 호박색 | 한 종류·한 리듬·한 색으로 고정하고, 끝나면 스스로 사라지게 하라 |
| 3.2 | 철도 | 원거리 신호기 | 예고는 속도의 적이 아니라 조건이다 — 미리 알리면 더 빨라진다 |
| 3.3 | 스포츠 중계 | NFL 노란 선 | 규칙을 HUD가 아니라 월드 좌표에 그리고 가림 규칙을 따르게 하라 |
| 3.4 | 의료 | 환자 모니터 알람 | 틀린 신호는 없는 신호보다 나쁘다 — 지우지 말고 참으로 만들어라 |
| 3.5 | 항공 | TCAS TA/RA | 정보와 지시를 분리하고, 지시는 명령형으로 — 모호하면 실패한다 |

---

## 4. UI를 늘리지 않는 방법 — 비용 없는 선택지

### 4.1 먼저: 우리 화면 요소는 실제로 몇 개인가

브리핑에 "10~19개"로 전달됐으나, QA 계측 정본은 **상태별로 다르고 계수에 보정이 필요하다**.
정확히 인용한다 (`_workspace/current/qa/ux-defect-list.md:80-95`) `[OBSERVED — QA 계측]`:

| 상태 | 계측 텍스트 | 계측 버튼 | 비고 |
|---|---|---|---|
| 타이틀 | 19 | 9 | |
| 매치 시작 | 11 | 1 | |
| 플레이어 턴 | 10 | 1 | **8~10** — 아래 주의 참조 |
| **적 턴** | **미계측** | **미계측** | UX-015 |

> 계수 방식은 `isActiveAndEnabled && text != 공백`이다(`VisualEvidenceCapture.cs:277-286`).
> QA는 `windText`·`scoreText`가 Canvas 없이 활성이라 세어지므로 **실제 렌더보다 2 많다**고 보고
> 실질 8개로 추정했다 `[OBSERVED — QA 계측]`.
>
> ⚠️ **그 보정은 지금은 성립하지 않을 수 있다.** §0-3에서 확인했듯 `HudCanvas.Adopt`가
> `Start()`에서 두 라벨을 실제로 입양한다 `[OBSERVED — 코드]`. 입양이 캡처 시점보다 먼저 끝나면
> 두 라벨은 **정상 렌더되므로 계측 10이 곧 실제 10**이다.
> QA가 근거로 든 `ux-3-player-turn.png` 우상단 공백은 **발견 시점 캡처**로 보인다.
> **순서상 10에 가까울 가능성이 높다** — 입양은 `Start()`(`GameManager.cs:309`) 내부 `:321`에서
> 일어나고 `VisualEvidenceCapture`는 매치 시작 이후에 찍으므로 입양이 먼저 끝난다
> (Lane A 지적, 본 레인 동의) `[INFERENCE]`.
> 확정에는 Unity 런타임 확인이 필요하다(하네스 규칙상 본 레인은 돌리지 않음).
> **따라서 인게임 실질 요소는 8~10, 10 쪽이 유력하며 확정값은 미정이다** `[INFERENCE]`.
>
> **"19개"는 타이틀 화면 값이므로 인게임 밀도 논거로 인용하면 오독이다.**

**이 불확정성은 §2·§3의 결론을 바꾸지 않는다.** 8이든 10이든 이 레인의 논거는 밀도가 아니라
**신뢰**이기 때문이다(§3.4) — 그리고 §2의 6개 경로는 어느 쪽이어도 요소를 늘리지 않는다.

### 4.2 비용 0: 거짓 지시문 2개를 **참인 상태 표현으로 교체** (삭제 아님)

§3.4(알람 피로)의 원칙은 "무의미한 경보를 없애라"이지만, **여기서 "없애라"는 삭제가 아니라
신호를 참으로 만들라는 뜻이다.** 이 구분이 castle-war에서는 결정적이다.

> ⚠️ **QA가 삭제를 명시적으로 반대한다.** UX-003(S1)의 제안 —
> *"(a) D3 벽돌 예약을 실제로 켜서 문구를 참으로 만들거나, (b) 적 턴에 두 문자열을 상태 표현으로 교체.
> **문구만 지우는 것은 최악** — 적 턴 화면에서 텍스트가 하나 더 사라져 §0 표가 전부 수동태가 된다"*
> `[OBSERVED — QA 계측]`.
>
> **§3.5 TCAS가 독립적으로 같은 답을 냈다.** 7.1 버전은 모호한
> *"Adjust Vertical Speed, Adjust"* 를 **지우지 않고** *"Level off, Level off"* 로 **교체**했다
> `[direct page retrieval]`. 항공은 "애매하면 삭제"가 아니라 "애매하면 명령형으로 교체"를 택했다.

**따라서 올바른 조치는 교체다:**

| 위치 | 현재 (거짓) | 교체 방향 |
|---|---|---|
| `SiegeAlarmSystem.cs:225` | `"적 포격 준비 중…  ·  클릭: 벽돌 예약"` | 뒷절을 **적의 다음 탄종 예고**로 교체 (`ProjectileForTurn`이 이미 결정론적 public — UX-004) |
| `LaunchManager.cs:121` | 적 턴에도 남는 `"아무 곳이나 당겨 발사"` | 적 턴에는 **적이 무엇을 하는 중인지**를 능동태로 (§3.5 TA/RA 분리: 지금은 "정보" 국면) |

> **효과**: 화면 요소 수는 **그대로**(±0)인데 거짓이 참으로 바뀐다.
> 그리고 교체 내용이 **§0-3의 "계산은 되는데 렌더 0"인 정보를 그대로 재사용한다** —
> 새 계산도, 새 요소도 필요 없다. **이것이 이 레인에서 가장 값싼 조치다.**

> **왜 삭제가 최악인가** — 알람 피로 문헌의 우회 행동과 같은 구조다.
> 신호를 지우면 사용자는 "화면이 조용해졌다"가 아니라 **"이 화면은 나에게 아무 말도 안 한다"** 로 읽는다.
> 적 턴은 이미 활성 버튼 0개이므로, 텍스트까지 줄면 J3("무엇을 해야 하는지 모르겠다")이 악화된다 `[INFERENCE]`.

### 4.3 비용 0: 이미 존재하는 0.9초 창을 채운다

**코드가 의도를 문자로 남겨 놓았다** `[OBSERVED — 코드]`:

```
// SimpleAI.cs:28-30
// Half of the 0.9s AI beat (GameManager.ExecuteAITurn holds the other 0.4s) —
// enough of a pause to read as the enemy taking aim, not a wait.
yield return new WaitForSeconds(0.5f);
```

`GameManager.cs:2263-2267`의 주석은 이 값의 역사까지 남겼다 —
과거 1.5s + 1.5s = **3.0초의 데드에어가 매 적 턴마다** 발생해 *"~17% of a whole match"* 를
소모했고, 그래서 0.4s + 0.5s로 줄였다 `[OBSERVED — 코드]`.

> **여기가 이 레인의 가장 실행 가능한 발견이다.**
> 팀은 "적이 조준하는 것으로 읽히는 정지"를 **설계했고**, 그 정지 시간을 **유지했다**.
> 그러나 그 0.9초 동안 렌더되는 것이 없다. **의도는 주석에만 있다.**
> §2.1(선행동작) / §2.2(사운드)는 **이 창 안에서 화면 요소 0개로** 구현된다.

**⚠️ 순서 제약 (구현 시 반드시 확인)**: `SimpleAI.cs`는 `WaitForSeconds(0.5f)`(:30) **다음에**
`FindTargetPosition()`(:31)을 호출한다 `[OBSERVED — 코드]`.
따라서 **선행동작·사운드는 지금 구조로 즉시 가능**하지만,
**조준 방향/궤적을 보여 주려면 표적 계산을 정지 앞으로 옮겨야 한다** `[INFERENCE]`.

### 4.4 비용 낮음: 궤적 프리뷰의 **소유자만 바꾼다**

새 UI를 만들 필요가 없다 — 렌더러가 이미 있고, **이미 적 턴을 알고 있다**.

`LaunchManager.cs:938` `[OBSERVED — 코드]`:
```csharp
bool previewIsPlayer = gameManager == null || gameManager.IsPlayerTurn;
```
`DrawTrajectory`는 충돌 필터링에서 **"이 프리뷰가 플레이어 것인가"를 이미 분기**한다.
즉 적 소유 프리뷰가 구조적으로 배제되어 있지 않다.

- 렌더러: `trajectoryLine` (LineRenderer) — 기존 `[OBSERVED — 코드]`
- 예측 길이: 300스텝 × 0.02s = **6초**, 착탄까지 도달 보장 `[OBSERVED — 코드]`
- **HUD가 아니라 월드 선** → §3.3 NFL 노란 선과 정확히 같은 범주

> **화면 요소 증가분: 0** (HUD 카운트에 들어가지 않는 월드 렌더)
> **단, 이것은 정보량을 늘린다** — Lane C가 다루는 "예고 과다" 위험의 대상이다.
> 전량 공개가 아니라 **부분 공개**(방향만 / 착탄 구역만)가 절충안이다 `[INFERENCE]`.

### 4.5 비용 0: 카메라와 시간

- **카메라**: 적 턴 팬 → 발사 → 탄 추적 (§2.5 Peggle, §2.5 참고의 Worms 자동 추적)
- **시간**: 착탄 홀드 0.35초를 **피해량 비례**로 (§2.4 Smash 히트랙 — 피해가 크면 정지가 길다)

둘 다 **새 오브젝트를 만들지 않는다.** 카메라 변환과 시간 스케일뿐이다.

### 4.5b 비용 0: 바람을 월드 채널에 얹는다 — **숫자가 없어서가 아니라, 숫자로는 안 되기 때문**

> ⚠️ **전제 정정**: 초판/개정1은 이 절을 "UX-001(바람이 렌더되지 않음)을 보완한다"로 썼다.
> **그 전제는 철회했다** — `windText`는 `HudCanvas.Adopt`로 입양되어 렌더된다(§0-3) `[OBSERVED — 코드]`.
> 바람 문자열도 살아 있다: `GameManager.cs:2294`가 `"WIND >>> 2.3"` 형태로 쓰고,
> `:2297`이 3.5 이상일 때 경고색으로 바꾼다 `[OBSERVED — 코드]`.

**그런데 이 절의 결론은 정정 후 오히려 강해진다.** 숫자가 이미 화면에 있다면,
남은 문제는 "표시가 없다"가 아니라 **"그 표시가 정보를 다 담지 못한다"** 이다.

근거는 코드 주석 한 줄이다 — `SimpleAI.cs:57-59` `[OBSERVED — 코드]`:

```
// Wind is spatial. The runtime body and this prediction must start from the
// AI muzzle, never from the previous player shot.
gameManager.windEffectOrigin = GetLaunchPosition();
```

> **바람이 공간적이면 스칼라 하나로는 원리적으로 표현되지 않는다.**
> `"WIND >>> 2.3"`은 방향과 세기를 주지만 **어디서 어떻게 작용하는지**를 주지 못한다.
> 발사 위치마다 효과가 다른데 표시는 화면당 하나다 `[INFERENCE]`.

**따라서 월드 채널이 대체재가 아니라 정확도상 우월한 선택이다:**

- **§2.3(오브젝트가 UI)**: 깃발·먼지·나뭇잎의 기울기 — **위치마다 다르게** 놓을 수 있다.
  이것이 스칼라 라벨이 구조적으로 못 하는 일이다
- **§3.1(방향지시등)**: 한 종류·한 리듬·한 색 — 세기를 **흔들림 진폭 하나**로 인코딩
- **§3.3(NFL 노란 선)**: 규칙을 월드 좌표에 그린다 — 바람은 월드의 속성이므로 월드에 그리는 것이 정합

**화면 요소 증가분: 0** (월드 렌더). 기존 `windText`를 **지우지 않는다** — §5-7의 이유로 병행한다.

### 4.6 "UI를 늘리지 않는 방법" 정리 — 비용 순

| 순위 | 조치 | 화면 요소 변화 | 근거 | 선행 조건 |
|---|---|---|---|---|
| 1 | 거짓 지시문 2개를 참인 상태 표현으로 **교체** | **±0** | §3.4 알람 피로 + §3.5 TCAS + QA UX-003 | 없음 — 즉시 가능 |
| 2 | 0.9초 창에 선행동작 애니메이션 | 0 | §2.1 Punch-Out!! | 없음 — 창이 이미 존재 |
| 3 | 0.9초 창에 탄종별 사운드 | 0 | §2.2 L4D | 없음 |
| 4 | 적 턴 카메라 팬·추적 | 0 | §2.5 Peggle | 없음 |
| 5 | 착탄 홀드를 피해량 비례로 | 0 | §2.4 Smash 히트랙 | 없음 |
| 6 | **샷 이력 잔존** (착탄 마커 / 직전 궤적 페이드) | 0 (월드) | §2.3 — Scorched Earth(1991) 선례 | 없음 |
| 7 | 바람을 월드 오브젝트로 (깃발·먼지) | 0 (월드) | §4.5b — 바람이 **공간적**이라 스칼라 라벨로는 불가 | 없음 — 값이 이미 계산됨 |
| 8 | Knight/Archer 트레일 구분 | 0 (월드) | §2.3 — 팀색·굵기·Barrel은 **이미 구현됨** | 없음 (가장 작은 잔여 항목) |
| 9 | 적 궤적 프리뷰(부분 공개) | 0 (월드) | §3.3 NFL / §4.4 | **표적 계산을 정지 앞으로 이동** |

**1~8번은 선행 조건이 없고 화면 요소를 늘리지 않는다.** 9번만 코드 순서 변경이 필요하다.

> ⚠️ **2건은 초판에서 정정됐다.**
> 1번은 "삭제"가 아니라 "교체"다(QA UX-003 근거).
> 8번은 초판에 "적 탄 트레일 색 구분"으로 크게 적었으나, 팀색·굵기·Barrel 구분은
> **이미 구현되어 있어서**(`UnitController.cs:483-490`) 잔여 범위가 Knight/Archer 하나로 줄었다.
> 그 자리를 6번(샷 이력)이 대체한다 — 이쪽이 실제 공백이다.

---

## 5. 반례와 한계 — 이 레인이 주장하지 않는 것

1. **"UI를 쓰면 안 된다"고 말하지 않는다.** §2의 6개 경로는 UI의 **대체재**이지 우월재가 아니다.
   Into the Breach의 텔레그래프는 **UI(타일 표시)** 이고 효과가 입증됐다 `[direct page retrieval]`.
2. **간접 채널은 접근성 위험이 있다.** 사운드 큐(§2.2)는 무음 환경·청각 장애에서 사라지고,
   색 트레일(§2.3)은 색각 이상에서 무력하다. **간접 채널은 단독 채널이 되면 안 된다** `[INFERENCE]`.
3. **선행동작에는 최소 길이가 필요하다.** 텔레그래프 설계 논의는 인간 반응시간을 약 0.3초로 잡고,
   예고는 지각+판단을 담을 만큼 길어야 한다고 말한다 `[indexed snippet]`.
   우리 창은 0.9초이므로 **여유가 있다** — 다만 0.5초 정지 뒤에 표적이 계산되므로(§4.3)
   실효 예고 길이는 구현 방식에 따라 0.9초보다 짧아질 수 있다 `[INFERENCE]`.
4. **과다 예고의 역효과는 실재한다.** 텔레그래프가 과하면 적이 수동적·기계적으로 느껴진다는 지적이
   있다 `[indexed snippet]`. 게임 내부 사례는 **Lane C 담당**.
5. **§2의 "화면 요소 증가 0"은 계수 규칙에 의존한다.** QA 계수는 텍스트/버튼을 세므로
   (`isActiveAndEnabled && text != 공백`, `VisualEvidenceCapture.cs:277-286`)
   월드 렌더·카메라·사운드·시간은 원래 계수 대상이 아니다 `[OBSERVED — QA 계측]`.
   **"인지 부하가 0"이라는 뜻은 아니다** `[INFERENCE]`.
6. **적 턴 화면 요소는 미계측이다** (UX-015) `[OBSERVED — QA 계측]`.
   적 턴에 무엇을 더하든, **비교 기준선이 아직 없다.**
7. **"요소를 줄여 깔끔하게"는 검증된 실패 경로다.** 본 레인의 "화면 요소 증가 0"은
   **요소를 줄이자는 주장이 아니다.** Lane A가 Slay the Spire 사례를 공유했다 —
   아이콘만 쓰다가 숫자를 노출했을 때 **더** 몰입적이었다는 개발 기록
   (Lane A 경유, 본 레인에서 1차 미검증 → `thin evidence`).
   QA UX-003의 "문구만 지우는 것은 최악"과 같은 방향이다 `[OBSERVED — QA 계측]`.
   → **§2의 간접 채널은 직접 표시를 대체하러 온 것이 아니라, 직접 표시가 닿지 못하는 곳
   (적 턴 0.9초, 공간적 바람)을 메우러 온 것이다.**
8. **§4.2의 "교체"는 초판의 "삭제"를 정정한 것이다.** 초판은 알람 피로(§3.4)만 보고
   삭제를 1순위로 적었다. QA UX-003(S1)과 §3.5 TCAS의 교체 선례가 이를 반박한다.
   **이 문서에서 실제로 뒤집힌 결론이므로 명시해 둔다.**
9. **QA 결함표는 발견 시점 스냅샷이며, "오래됐다"를 일반화하면 안 된다.**
   본 문서에서 UX-001(바람)은 **이미 조치됨**으로 확인됐고 UX-002(점수)도 같은 커밋으로 조치됐다
   (`GameManager.cs:1129-1130` 연속 두 줄) `[OBSERVED — 코드]`.
   그러나 UX-004(적 예고 부재)는 **여전히 유효하다** — `ProjectileForTurn` 소비처를 전수 확인한 결과
   `GameManager.cs:2059`(카드 상태 동기화)와 `:2083`(프리팹 선택) 둘뿐이고,
   플레이어가 보는 것은 `LaunchManager`의 `selectedUnitName`(자기 이번 턴 것)이 전부다
   `[OBSERVED — 코드]`. **적 탄종·다음 탄종을 그리는 경로는 없다.**
   → **결함은 항목별로 확인해야 한다.** Lane A가 UX-005(경기 진행도)에 대해 같은 결론에 도달했다.
10. **이 규율은 내 자신의 권고에도 적용됐다.** 초판 §2.3의 "적 탄 트레일 색 구분" 권고는
   **절반이 이미 구현돼 있었다** — 팀색·굵기·Barrel 구분 존재(`UnitController.cs:483-490`)
   `[OBSERVED — 코드]`. 권고를 쓰기 전에 현재 상태를 확인하지 않은 것이 원인이다.
   §2.3을 현재 상태표로 재작성하고 실제 공백(샷 이력)으로 교체했다.

---

## 6. 출처와 증거 등급

| 출처 | 등급 | 이 문서에서의 용도 |
|---|---|---|
| en.wikipedia.org/wiki/Punch-Out!!_(NES) | `direct page retrieval` | 정해진 패턴, 카운터펀치로만 별 획득, 퍼즐 평가 |
| en.wikipedia.org/wiki/Left_4_Dead | `direct page retrieval` | 특수 감염체의 사운드·실루엣·음악 큐, 총구 화염이 팀원 상태 전달, 동적 색보정 |
| en.wikipedia.org/wiki/Superhot | `direct page retrieval` | "Time Moves Only When You Move", 상황 판단 시간, 붉은 탄도 트레일 |
| www.ssbwiki.com/Hitlag | `direct page retrieval` | 히트랙의 두 기능(타격 확인 + 계획 시간), 30프레임 상한, 피해량 비례 |
| en.wikipedia.org/wiki/Peggle | `direct page retrieval` | 마지막 펙 접근 시 카메라 줌, 단순 연출이 더 잘 반응 |
| en.wikipedia.org/wiki/Into_the_Breach | `direct page retrieval` | **텔레그래프가 템포를 빠르게 했다**, 짧은 전투 목표, XCOM 대조 |
| en.wikipedia.org/wiki/Advance_Wars | `direct page retrieval` | "waves of excitement" 페이싱, 의도적 조용한 구간 |
| en.wikipedia.org/wiki/Automotive_lighting | `direct page retrieval` | 1~2 Hz 규격, 호박색 협약, NHTSA 28%·비용 동등, 자기 취소 |
| en.wikipedia.org/wiki/Railway_signal | `direct page retrieval` | 원거리 신호기, **속도 증가가 도입 이유**, 실패 시 최대 제한 해석, 황색 계층 |
| en.wikipedia.org/wiki/1st_%26_Ten_(graphics_system) | `direct page retrieval` | 경기장 좌표 고정·가림 규칙, 실물 아님, 시즌 3D 모델, 확장 사례 |
| en.wikipedia.org/wiki/Alarm_fatigue | `direct page retrieval` | FDA 566건, JC 80건/13건, ECRI 2014년 1위, **볼륨 낮추기 우회**, NTSB 주 8,000건, Prop 65 |
| en.wikipedia.org/wiki/Traffic_collision_avoidance_system | `direct page retrieval` | TA/RA 분리, RA가 ATC 우선, 3~5배, 모호 문구 교체, Eurocontrol 25% |
| L4D 특수 감염체 거리별 악기(피아노/현악) | `indexed snippet` | §2.2 보강 (커뮤니티 정리, 1차 미검증) |
| Worms Armageddon 카메라 자동 추적(Scroll Lock) | `indexed snippet` | §2.5 참고 — worms2d.info 해당 페이지 404, 1차 확인 실패 |
| Sakurai의 히트랙 무술영화 비유 | `indexed snippet` | §2.4 보강 (SmashWiki 외부 링크로 존재, 원문 미검증) |
| 텔레그래프 설계 논의(반응시간 0.3초, 과다 예고 역효과) | `indexed snippet` | §5 한계 — gdkeys / gamedeveloper 등, 개별 URL 미검증 |
| `Assets/Scripts/SimpleAI.cs:28-31` | `[OBSERVED — 코드]` | 0.9초 AI 비트 주석, 0.5초 정지, 정지 **뒤** 표적 계산 |
| `Assets/Scripts/GameManager.cs:2263-2267` | `[OBSERVED — 코드]` | 과거 3.0초 데드에어 ≈ 경기의 17%, 현재 0.4초 |
| `Assets/Scripts/LaunchManager.cs:18,22,121,938` | `[OBSERVED — 코드]` | trajectoryLine, 300스텝, 발사 안내 문구, `previewIsPlayer` 분기 |
| `Assets/Scripts/SiegeAlarmSystem.cs:225` | `[OBSERVED — 코드]` | `"클릭: 벽돌 예약"` 거짓 지시문 |
| `Assets/Scripts/OneShotSiegeRules.cs:25-28` | `[OBSERVED — 코드]` | 탄종 순환이 공개 규칙 |
| `_workspace/current/qa/ux-defect-list.md:80-98` | `[OBSERVED — QA 계측]` | 상태별 화면 요소 수, 적 턴 미계측(UX-015). **"유령 텍스트 2개" 보정은 §4.1에서 조건부로 재평가** |
| ~~`ux-defect-list.md:70` (UX-001)~~ → `GameManager.cs:1124-1131`, `:2294-2297`, `HudCanvas.cs:112-134`, `:51-90` | `[OBSERVED — 코드]` | **UX-001 철회 근거** — `HudCanvas.Adopt(windText)`가 `Start()`에서 입양, 조기 반환은 `parent == root`뿐. 주석이 과거형(*"Adoption is what makes them appear"*) |
| `_workspace/current/qa/ux-defect-list.md:72` (UX-003, S1) | `[OBSERVED — QA 계측]` | **"문구만 지우는 것은 최악"** — §4.2의 삭제→교체 정정 근거 |
| `_workspace/current/qa/ux-defect-list.md:73` (UX-004, S2) + 본 레인 전수 재확인 | `[OBSERVED — QA 계측 / 코드]` | `ProjectileForTurn` 소비처는 `GameManager.cs:2059`·`:2083` 둘뿐이고 플레이어가 보는 것은 `LaunchManager`의 `selectedUnitName`(자기 턴)뿐 — **적 예고 경로 없음. 여전히 유효한 결함** |
| `Assets/Scripts/UnitController.cs:482-490`, `:636`, `:1174`, `:1194` | `[OBSERVED — 코드]` | 트레일 팀색·굵기·Barrel 구분 **이미 구현**, `time = 0.5f`, 착탄 시 `emitting = false` → §2.3 정정 근거 |
| `Assets/Scripts/GameManager.cs:1130`, `:2299` | `[OBSERVED — 코드]` | `Adopt(scoreText)` + 점수 문자열 갱신 — UX-002도 조치됨(Lane A 지적, 본 레인 확인) |
| `.survey/siege-artillery-landscape/solutions.md` | `[OBSERVED — 선행 조사]` | Scorched Earth(1991)의 **이전 샷 궤적선** 선례 → §2.3 샷 이력 근거 |
| `Assets/Scripts/SimpleAI.cs:57-59` | `[OBSERVED — 코드]` | *"Wind is spatial"* 주석 + `windEffectOrigin` 리셋 (§4.5b) |
| Slay the Spire — 아이콘만 쓰다 숫자 노출이 더 몰입적이었다는 기록 | `thin evidence` | §5-7. **Lane A 경유, 본 레인 1차 미검증** |

### 등급 해설

- `direct page retrieval` — 1차 페이지를 직접 가져와 본문에서 읽음 (2026-08-13)
- `indexed snippet` — 검색 종합. 개별 URL을 1차로 확인하지 못함. **약한 근거로 취급**
- `[OBSERVED — 코드]` / `[OBSERVED — QA 계측]` — 저장소 파일을 직접 읽음
- `[INFERENCE]` — 위 증거에서 유도한 판단. 관측이 아님
- `thin evidence` — 타 레인 경유 전달. 본 레인에서 1차 확인 안 함. **가장 약한 근거**

> **부재의 증명에 대한 주의**: 본 문서는 "이 방법이 어디에도 없다"고 주장하지 않는다.
> §2의 6개 경로는 **표본 6개 게임 내 관측**이며 전수 조사가 아니다.

---

## 7. 개정 기록

| 시각 | 변경 | 사유 |
|---|---|---|
| 초판 | §0-1 / §4.2 / §4.6-1위를 **"거짓 지시문 삭제"** 로 작성 | §3.4 알람 피로만 근거로 삼음 |
| 개정 1 | **"참인 상태 표현으로 교체"** 로 정정 | QA UX-003(S1)이 *"문구만 지우는 것은 최악"* 이라 명시. §3.5 TCAS 7.1도 모호 문구를 **교체**(삭제 아님)했으므로 초판은 자체 근거와도 불일치했다 |
| 개정 1 | §0-3을 "계산은 되는데 렌더 0" **3중** 반복으로 확장 | Lane A가 UX-001(바람)을 지목 |
| 개정 1 | §4.5b(바람 월드 채널) 추가, §4.6 표에 7번 삽입 | 위와 동일 |
| 개정 1 | §5-7(요소 축소는 실패 경로)·§5-8(정정 명시) 추가 | 본 문서가 "숫자를 줄여라"로 오독될 위험 차단 |
| **개정 2** | §0-3을 **3중 → 2중**으로 축소, `windText` 사례 **철회** | Lane B 최초 지적 → Lane A 재검증 → 본 레인 독립 확인. `HudCanvas.Adopt`가 `Start()`에서 `windText`를 입양하고 조기 반환 분기를 통과하므로 **렌더된다**. UX-001은 발견 시점 결함이고 이미 조치됐다 `[OBSERVED — 코드]` |
| **개정 2** | §4.5b를 "렌더 0을 보완"에서 **"바람은 공간적이라 스칼라 라벨로 불가"** 로 재작성 | 전제가 틀렸으나 결론은 강해졌다. `SimpleAI.cs:57-59`의 *"Wind is spatial"* 이 유일하게 남은 논지이며, 이것은 표시 부재가 아니라 표현력 한계의 문제다 |
| **개정 2** | §4.1의 "실제 렌더 8개"를 **"8~10, 확정값 미정"** 으로 하향 | 입양이 캡처보다 먼저 끝나면 계측 10이 곧 실제 10이다. 내가 QA의 보정을 검증 없이 인용했으므로 불확정성을 명시한다 |
| **개정 3** | §2.3을 **현재 상태표로 재작성**, 권고를 "트레일 색 구분" → **"샷 이력 잔존"** 으로 교체 | 내 초판 권고의 절반이 이미 구현돼 있었다 — 팀색 `(0.45,0.85,1)`/`(1,0.35,0.25)`, Barrel 주황, 굵기 0.14/0.09 전부 존재(`UnitController.cs:483-490`). 잔여는 Knight/Archer 구분 하나. 실제 공백은 색이 아니라 **시간축** — `time = 0.5f`(`:482`) + 착탄 시 `emitting = false`(`:636`)로 샷 이력이 남지 않는다. Scorched Earth(1991) 선례 있음 `[OBSERVED — 코드 / 선행 조사]` |
| **개정 3** | §4.6 표를 8행 → **9행**으로, 6번에 샷 이력 신설·8번을 Knight/Archer로 축소 | 위와 동일. 초판 6번("적 탄 트레일 색 구분")은 범위가 과대했다 |
| **개정 3** | §5-9(결함표는 항목별 확인)·§5-10(내 권고에도 적용) 추가 | Lane A의 "일반화 금지" 경고를 반영. UX-001·002는 조치됐지만 UX-004는 전수 확인 결과 **여전히 유효**하다. 같은 규율을 내 §2.3 권고에 적용해 오류를 찾았다 |
| **개정 3** | §4.1에 순서 추론 추가 — **10 쪽이 유력** | 입양은 `Start()` 내 `:321`, 캡처는 매치 시작 이후이므로 입양이 먼저 끝난다(Lane A 지적, 본 레인 동의) `[INFERENCE]` |
