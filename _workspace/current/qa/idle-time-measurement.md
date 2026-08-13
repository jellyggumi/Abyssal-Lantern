# 유휴 시간 계측 — 플레이어는 경기의 몇 %를 관전하는가

- run-id: 20260812-idle-time-stage1
- owner: game-qa 레인 (계측)
- 대상 커밋 상태: 작업 트리 현재 상태 (코드 수정 없음 — 읽기와 계산만)
- 규칙: 모든 상수는 `파일:라인` 인용. 못 찾은 값은 "못 찾음"이라 쓴다. 추측은 `[INFERENCE]`.

---

## 0. 한 줄 답

**플레이어는 경기 시간의 약 62%를 조작 불가 상태로 보낸다.**

전체 321초 중 조작 가능 시간은 약 122초뿐이다. 나머지 200초는 입력이 물리적으로 차단되어 있다.

| | 값 | 가정 |
|---|---|---|
| **채택값** | **62.2 %** | 정착 1.2s 만기 + 배럴 도화선 3턴 중 1턴 (둘 다 코드 근거 있음) |
| 보수적 하한 | 53.3 % | 배럴 도화선을 무시할 경우 |
| 감도 밴드 전체 | 45 ~ 62 % | 정착을 0.6s까지 낮춰본 경우 포함 |

어느 가정을 택하든 절반을 넘는다. 유도 경로는 §3, 감도표는 §3.5에 있다.

---

## 1. 조작 가능 / 불가능의 경계 — 코드에서 확정 [OBSERVED]

"유휴"를 느낌으로 정의하지 않기 위해, 먼저 **입력이 코드에서 차단되는 지점**을 찾았다.
차단 근거는 두 겹이고, 두 번째가 더 강하다.

### 1.1 게이트 1 — 입력 핸들러가 플레이어 턴을 요구

```
LaunchManager.cs:529-532
    bool canAim = gameManager != null
        && gameManager.currentState == GameState.PlayerTurn
        && gameManager.IsPlayerTurn;
    if (canAim && selectedUnitPrefab != null && !deployArmed) HandleInput();
```

`HandleInput()`이 드래그·키보드 조준·발사를 전부 소유한다(`LaunchManager.cs:627-640`).
따라서 `GameState.AITurn`에서는 조준도 발사도 호출되지 않는다.

### 1.2 게이트 2 — 컴포넌트 자체가 꺼진다 (더 강한 근거)

```
GameManager.cs:2068-2069
    var lm = FindObjectOfType<LaunchManager>();
    if (lm != null) lm.enabled = false;
...
GameManager.cs:2120
    if (lm != null) lm.enabled = true;
```

볼리가 해소되는 동안 `LaunchManager`는 **비활성화**된다. `Update()`가 아예 돌지 않으므로
게이트 1을 통과하는지 여부와 무관하게 입력 경로가 존재하지 않는다.
이 구간은 `OnUnitLaunched()`(`GameManager.cs:2048-2054`)부터 `EndTurn()`(`GameManager.cs:2122`)까지다.

### 1.3 나머지 입력 표면도 같은 구간에서 닫힌다

| 표면 | 차단 지점 | 상태 |
|---|---|---|
| 화포 배치 (D키) | `DeploymentController.cs:149-158` — `playerCanAct`가 `PlayerTurn && IsPlayerTurn && !IsResolvingTurn`을 요구, 아니면 `DisarmDeployMode(); return;` | 적 턴·해소 중 닫힘 |
| Last Stand (R키) | `GameManager.cs:1150-1156` `CanActivatePlayerLastStand()`가 동일 3조건 요구 | 적 턴·해소 중 무효 |
| 유닛 카드 선택 (1/2/3키) | `GameManager.cs:1707` `if (isPlayerTurn && !enforceOneShotTurns)` | **출하 설정에서 항상 닫힘** |
| 벽돌 사전 지정 (클릭) | `BrickPlacementController.cs:76-82` `if (gm.EnforcesOneShotTurns) { panel.SetActive(false); return; }` | **출하 설정에서 항상 닫힘** |

`enforceOneShotTurns`의 기본값은 `true`다 — `GameManager.cs:144`.

> [!important] 적 턴 전용 입력 창은 코드에 **존재하지만 꺼져 있다**
> `BrickPlacementController.cs:95`의 주석은 `// Designation window: the OPPONENT's turn only`이고,
> `:92`는 `blockUIPanel.SetActive(gm.currentState == GameState.AITurn)`이다.
> 즉 적 턴에 클릭해 벽돌 2개(`SiegeTactics.cs:40` `MaxPendingBricks = 2`)까지 예약하는 기능이
> 이미 구현되어 있다. 그런데 `:76-82`의 조기 반환이 원샷 모드에서 이 전체를 무효화한다.
> **"적 턴 입력 0"은 설계 부재가 아니라 의도적 비활성화다.**
> (이 발견은 `PikaVolleySurvey` 에이전트가 지목했고, 위 라인을 직접 읽어 확인했다.)

### 1.4 결론

> **적 턴은 100% 조작 불가다. 플레이어 턴도 발사 커밋 이후는 100% 조작 불가다.**
> 조작 가능 구간은 오직 "플레이어 턴 시작 ~ 발사 커밋"뿐이다.

---

## 2. 한 턴의 시간 구조 분해 [OBSERVED — 상수 전부 인용]

### 2.1 공통 꼬리 — 발사 커밋 이후 (양 진영 동일)

| 구간 | 값 | 출처 |
|---|---|---|
| 비행 (`Launched` 상태 유지) | 유도값 §2.4 | `GameManager.cs:2078-2088` 대기 루프, 워치독 상한 `12f` (`:2078`) |
| 배럴 도화선 (배럴 턴만) | **2.0s** | `SiegeTactics.cs:73` `BarrelFuseSeconds = 2f`; 루프가 `IsFusePending`로 대기 — `GameManager.cs:2084` |
| 착탄 홀드 | **0.35s** | `GameManager.cs:17` `PostImpactHoldSeconds = 0.35f`, 소비 지점 `:2089` |
| 정착 대기 | **최대 1.2s** | `GameManager.cs:2100` `while (settleTimer < 1.2f)` |
| 턴 전환 자체 | 0s (동기 호출) | `GameManager.cs:2122` `EndTurn()` — 코루틴 대기 없음 |

정착 루프는 "아무것도 안 움직일 때만" 조기 탈출한다(`GameManager.cs:2114`).
코드 주석이 실제 거동을 명시한다:

```
GameManager.cs:2092-2098
    // Settle window. QA measured a real handoff at 6.39s per shot, and this cap was
    // most of it: the loop only exits early when NOTHING is moving, and a landed
    // knight walking into the rubble keeps nudging blocks, so it ran the full window
    // nearly every turn.
```

→ **성을 맞힌 턴에서 정착은 사실상 항상 1.2s 만기다.** 모델에 1.2s를 쓴다.

### 2.2 적(AI) 턴 — 전 구간 유휴

| 구간 | 값 | 출처 |
|---|---|---|
| `ExecuteAITurn` 선지연 | **0.4s** | `GameManager.cs:2159` `yield return new WaitForSeconds(0.4f)` |
| `SimpleAI.PerformLaunch` 선지연 | **0.5s** | `SimpleAI.cs:30` `yield return new WaitForSeconds(0.5f)` |
| 조준 계산 | ~0s (동기) | `SimpleAI.cs:169-196` — 5패스 × 5×5 그리드 탐색이지만 한 프레임 내 완료 |
| 이후 | 공통 꼬리 §2.1 | |

**AI 발사 전 고정 데드에어 = 0.4 + 0.5 = 0.9s.** 이 구간에는 아무 일도 일어나지 않는다.

> [!warning] `turnDuration = 15f`를 적 턴 길이로 쓰면 과대계상이다
> `GameManager.cs:21`의 15초는 **플레이어 조준 상한**일 뿐이다. AI는 타이머를 소진하지 않고
> 0.9초 뒤 발사하며, 발사 순간 `isResolvingTurn = true`(`GameManager.cs:2051`)가 되어
> 타이머 감산 자체가 멈춘다(`GameManager.cs:1720` `if (isResolvingTurn) return;`).

### 2.3 플레이어 턴

| 구간 | 값 | 출처 |
|---|---|---|
| **조준 가능 시간 (유일한 조작 구간)** | 상한 15s, 실제는 유도값 §3 | 상한 `GameManager.cs:21` `turnDuration = 15f`, 리셋 `:1679`/`:2137` |
| 조준 중 만료 시 유예 1회 | +4.0s | `GameManager.cs:138` `AimGraceSeconds = 4f`, 부여 `:1759-1762` |
| 유휴 넛지 주기 | 5.0s | `GameManager.cs:140` `IdleNudgeIntervalSeconds = 5f`, 발화 `:1740-1746` |
| 긴급 경고 임계 | 잔여 5.0s | `GameManager.cs:139` `UrgencyThresholdSeconds = 5f` |
| 발사 커밋 이후 | 공통 꼬리 §2.1 | |

조준을 안 하고 방치하면 턴을 몰수당한다 — `GameManager.cs:1780-1785` `DecideTurnExpiry`.

### 2.4 비행 시간 유도 [INFERENCE — 물리 상수는 OBSERVED]

비행 시간은 상수로 적혀 있지 않으므로 물리에서 유도한다.

**입력 상수 (전부 OBSERVED):**

| 값 | 출처 |
|---|---|
| 중력 `(0, -9.81)` | `ProjectSettings/Physics2DSettings.asset` `m_Gravity` |
| 발사체 `m_LinearDrag: 0`, `m_GravityScale: 1` | `Assets/Prefabs/{Knight,Archer,ExplosiveBarrel}.prefab` |
| 발사 속도 범위 3 ~ 25.2 | `LaunchManager.cs:14-15` `maxLaunchVelocity=25.2f`, `minLaunchVelocity=3f` |
| 기본 조준각 45°, 기본 파워 0.55 | `LaunchManager.cs:43-44` `aimAngleDegrees=45f`, `aimPower=0.55f` |
| 발사 원점 x = −14.5 | `GameManager.cs:557` `LaunchApronAbsX = 14.5f`, 배치 `:924` |
| 코어 위치 (±9, 0.5) | `GameManager.cs:552` `CoreAbsX = 9f`, 스폰 `:883`/`:900` |
| 스폰 높이 +0.9 | `UnitController.cs:25` `DefaultLaunchSpawnHeight = 0.9f`, 적용 `LaunchManager.cs:781` |
| 성벽 열 x = 4, 5, 6, 7 | `GameManager.cs:676-679` `KeepProfile` |
| Stage1 벽 높이 3, 오프셋 +0/+0/+1/+2 | `StageDefinitions.cs:120` `wallHeightBlocks: 3`; `GameManager.cs:713-716` |

**바람은 비행 시간을 바꾸지 못한다** — `UnitController.cs:37`이 `new Vector2(windForce / mass, 0f)`,
즉 수평 성분만 반환한다. 비행 시간은 수직 운동만으로 결정된다. 항력도 0이다.
→ 순수 포물선 계산이 성립한다.

**기본 조준(45°, 파워 0.55) 계산:**

```
v  = Lerp(3, 25.2, 0.55) = 15.21 m/s          (OneShotSiegeRules.cs:41)
vx = vy = 15.21 · cos45° = 10.755 m/s
스폰 (−14.5, 1.4)  [코어 y=0.5 + 스폰높이 0.9]

x=4  → t=1.720s, y=5.39   (전초 상단 3.0)  통과
x=5  → t=1.813s, y=4.78   (외곽 상단 3.0)  통과
x=6  → t=1.906s, y=4.08   (중간 상단 4.0)  통과 (여유 0.08 — 사실상 스침)
x=7  → t=1.999s, y=3.30   (내성 상단 5.0)  충돌
```

→ **기본 조준탄은 내성(x=7)에 t ≈ 2.0s에 착탄한다.**
장애물이 없을 경우 코어(x=9)까지는 t = 2.19s, 지면까지는 2.28s.

**모델에 쓰는 값: 비행 F = 2.0s.**

**검증 — 실측 1건과 대조 [OBSERVED]:**

`_workspace/current/qa/evidence/playmode-hero-growth-contract.xml:231`
`Turn handoff observed after 1.63s`

해당 테스트(`Assets/Tests/PlayMode/CastleBustersAnalysisTests.cs:94`)는
`SimulateLaunch(new Vector2(10f, 5f))` — 즉 vx=10, vy=5로 빈 필드에 쏜다.

```
비행 t = (5 + √(5² + 2·9.81·1.4)) / 9.81 = 1.248s   (착탄 x = −2.02, 개활지)
+ 홀드 0.35s
= 예측 1.598s     vs     실측 1.63s     오차 2.0%
```

잔차 0.032s는 정착 루프의 첫 프레임(움직이는 것이 없어 `:2114`에서 즉시 탈출)이다.
**분해식 `F + 0.35 + 정착`이 실측과 2% 안에서 일치한다.**

---

## 3. 유휴 비율 계산

### 3.1 정의

```
유휴 시간 = 플레이어가 입력할 수 없는 시간
          = (모든 적 턴 전체) + (플레이어 턴 중 발사 커밋 이후)
유휴 비율 = 유휴 시간 / 전체 경기 시간
```

### 3.2 경기 길이 — 출하 상수로

`MatchLengthModel`의 식 (`MatchLengthModel.cs:53-70`): `M = b·h + c`, `N = M/d`, `T = N·s`

| 기호 | 값 | 출처 |
|---|---|---|
| 벽 HP 합계 | 3·30 + 3·85 + 4·85 + 5·150 = **1435** | `StageDefinitions.cs:126-127` (주석에 이 산식이 그대로 적혀 있음), 재료 HP는 `Assets/Resources/{Wood,Stone,Iron}BlockData.asset` `maxHP: 30/85/150` |
| 코어 HP `c` | **150** | `CastleCoreGimmick.cs:39` `CoreMaxHP = 150f` |
| 재료 `M` | 1435 + 150 = **1585** | |
| 턴당 유효 피해 `d` | **37** | `MatchLengthModel.cs:51` `EffectiveDamagePerTurn = 37f` |
| 턴당 초 `s` | **7.5** | `MatchLengthModel.cs:45` `AverageTurnSeconds = 7.5f` |

```
N = 1585 / 37   = 42.84 턴
T = 42.84 × 7.5 = 321.3 초
```

목표 300초(`MatchLengthModel.cs:36`)의 ±20% 밴드(`:40` `ToleranceFraction = 0.2f`) = 240~360초 안이다.
`StageDefinitions.cs:128`의 주석 "~321s"와 일치한다.

> [!note] `s = 7.5`는 **플레이어 턴과 적 턴을 합친 평균**이다 — 한쪽 턴 길이가 아니다
> 두 가지 독립 근거:
> 1. `SiegePacingSimulation.Run`(`MatchLengthModel.cs:224-257`)이 양 진영 턴을 같은 `turns`
>    카운터로 센다(`:244-246`) — 짝수 턴은 적 성, 홀수 턴은 내 성을 깎는다. 그리고 길이를
>    `turns * settings.secondsPerTurn`으로 낸다(`:255`).
> 2. 모델 주석(`MatchLengthModel.cs:42-44`)이 "AI 데드에어가 3.0s → 0.9s로 줄자 8.5 → 7.5로
>    내렸다"고 적는다. 감소 2.1s가 전체의 절반인 적 턴에만 걸리므로 평균은 2.1 × 0.5 = **1.05s**
>    내려가야 하고, 실제 내린 값은 1.0s다. **모델 자신의 산술이 이 해석을 확증한다.**

### 3.3 턴 길이를 두 종류로 가르기

미지수는 하나뿐이다 — 플레이어의 실제 조준 소요 `D`.

```
꼬리(양측 공통)  tail = F + 도화선지분 + 홀드 + 정착
                     = 2.0 + (2.0 / 3) + 0.35 + 1.2
                     = 4.22 s

  · 도화선 지분: 발사체는 Knight→Archer→Barrel 3순환(OneShotSiegeRules.cs:13-17, :25-29)이므로
    배럴은 3턴 중 1턴. 배럴은 착탄 시 폭발하지 않고 2초 도화선을 문다
    (UnitController.cs:1109-1112, :1135-1136) → 평균 2.0/3 = 0.667 s/턴.

적 턴   A = 0.4 + 0.5 + tail = 5.12 s      (전부 유휴)
플레이어 턴 P = D + tail                     (D만 조작 가능)

제약: (P + A) / 2 = s = 7.5
   →  D + 4.22 + 5.12 = 15.0
   →  D = 5.67 s
```

`D = 5.67s`는 조준 상한 15s(`GameManager.cs:21`)의 38%다 — 모델 주석
(`MatchLengthModel.cs:22-24`) "타이머는 플레이어가 좀처럼 도달하지 않는 천장"과 정합한다.

### 3.4 결과

```
플레이어 턴 수 = 42.84 / 2 = 21.4

조작 가능 = 21.4 × 5.67 = 121.5 s
유휴      = 321.3 − 121.5 = 199.8 s

유휴 비율 = 199.8 / 321.3 = 62.2 %
```

**유휴의 내역:**

| 구간 | 초 | 전체 대비 |
|---|---|---|
| 적 턴 (전체) | 21.4 × 5.12 = **109.7** | 34.1 % |
| 내 턴 중 발사 이후 | 21.4 × 4.22 = **90.4** | 28.1 % |
| **유휴 합** | **199.8** | **62.2 %** |
| 조작 가능 | 121.5 | 37.8 % |

**비율은 턴 수와 무관하다.** 유도식을 정리하면

```
유휴 비율 = (A + tail) / (2s) = 1 − D / (2 × 7.5) = 1 − D / 15
```

경기가 길어지든 짧아지든, 벽을 몇 겹 쌓든 이 비율은 변하지 않는다.
`D`(실제 조준 소요)와 `s`(턴 평균)만이 이 숫자를 움직인다.

### 3.5 감도 — 어떤 가정이 이 숫자를 흔드는가

`F = 2.0`은 고정(§2.4에서 실측 대조 완료)하고, 불확실한 두 값을 흔들었다.

| 정착 | 도화선 지분 | tail | D | **유휴 비율** |
|---|---|---|---|---|
| 0.6 | 0 | 2.95 | 8.20 | 45.3 % |
| 0.6 | 0.67 | 3.62 | 6.87 | 54.2 % |
| 0.9 | 0 | 3.25 | 7.60 | 49.3 % |
| 0.9 | 0.67 | 3.92 | 6.27 | 58.2 % |
| 1.2 | 0 | 3.55 | 7.00 | **53.3 %** ← 도화선 무시 시 하한 |
| **1.2** | **0.67** | **4.22** | **5.67** | **62.2 %** ← 채택 |

**밴드: 45 ~ 62 %. 코드 근거가 가장 강한 조합은 62.2 %.**

- 정착 1.2를 택한 이유: `GameManager.cs:2092-2098` 주석이 "거의 매 턴 만기까지 돌았다"고 명시.
- 도화선을 포함한 이유: `GameManager.cs:2084`의 대기 조건에 `u.IsFusePending`이 실제로 들어 있고,
  배럴은 규칙상 3턴에 1번 강제된다(`OneShotSiegeRules.cs:13-17`).

**하한을 쓰고 싶다면 53%를 쓰라** (도화선 지분 0 가정). 어느 쪽이든 절반을 넘는다.

### 3.6 이 계산이 말하지 않는 것 [중요]

> [!warning] `D = 5.67s`는 실측이 아니라 **모델에서 역산한 값**이다
> `s = 7.5`는 `MatchLengthModel.cs:28-31`이 스스로 "정직한 약점 — 계측이 아니라 관찰에서 나왔다"고
> 적은 보정 상수다. 따라서 `D`는 **관찰 기반 상수에서 나온 2차 유도값**이다.
>
> 다만 비율의 두 항 중 **유휴 쪽(A + tail = 9.34s)은 전부 코드 상수와 물리에서 나온다.**
> 흔들리는 것은 분모의 `D`뿐이다. 그래서 감도표(§3.5)를 함께 낸다.
>
> **`D`를 직접 재는 법**: `OnUnitLaunched`(`GameManager.cs:2048`) 진입 시각에서
> 직전 `EndTurn`(`:2125`) 시각을 빼면 그 턴의 `D + 0`이 나온다. 텔레메트리에 필드 하나
> (`Volley.d`가 현재 상수 0으로 비어 있다 — `Telemetry.cs:101`)를 쓰면 즉시 실측된다.
> 이 문서 전체가 그 한 필드로 대체 가능하다.

---

## 4. 입력 빈도

### 4.1 한 턴에 커밋 가능한 결정의 수 = 1 [OBSERVED]

`OneShotTurnGate`(`OneShotSiegeRules.cs:47-59`)가 턴당 커밋을 1회로 잠근다.
`TryCommitShot()`은 두 번째 호출에서 `false`를 반환한다(`:53-58`).

이 게이트를 소비하는 경로는 두 개이고, **서로 배타적**이다:

| 행위 | 커밋 지점 | 비고 |
|---|---|---|
| 발사 | `LaunchManager.cs:994` `if (gameManager != null && !gameManager.TryCommitTurnShot()) return;` | 기본 경로 |
| 화포 설치 (D키) | `DeploymentController.cs:321-325` | 발사 **대신** 소비 |

원샷 모드에서 죽어 있는 행위들:

| 행위 | 차단 |
|---|---|
| 유닛 카드 선택 (1/2/3) | `GameManager.cs:1707` — 발사체는 규칙이 정한다(`OneShotSiegeRules.cs:25-29`) |
| 벽돌 사전 지정 | `BrickPlacementController.cs:76-82` |
| 로스터 배치 (화포 외) | `DeploymentController.cs:148` `SelectedCard = DeployCard.Cannon` 고정 |

경기 전체에서 1회만 가능한 행위:

| 행위 | 상한 | 출처 |
|---|---|---|
| Last Stand (R) | 경기당 1회 | `GameManager.cs:1835` `Phase.Active` → `:1877` `Phase.Consumed`; 조건 `:1150-1156` |

### 4.2 경기당 커밋 결정 수

```
플레이어 턴 21.4회 × 턴당 1회 = 21.4
+ Last Stand 최대 1회
= 최대 22.4회 / 321.3초
```

| 지표 | 값 |
|---|---|
| 전체 경기 기준 | **0.070 회/초** — 14.3초에 1회 |
| 조작 가능 시간(121.5s) 기준 | **0.185 회/초** — 5.4초에 1회 |

### 4.3 실측 대조 — 실제 커밋률은 이보다 낮다 [OBSERVED, n=1]

`_workspace/current/qa/evidence/telemetry-live-webgl6000.json` (WebGL 6000 빌드 실플레이 1판)

```
MatchEnd: turns=13, winner=enemy
Volley 이벤트: 5건
```

`TelemetrySink.Volley`는 `LaunchManager.LaunchUnit`(`LaunchManager.cs:1016`)에서만 발신된다 —
`SimpleAI`에는 호출이 없다. 즉 **5건은 전부 플레이어 발사다.**

플레이어 선공(`GameManager.cs:1678` `isPlayerTurn = true`)이므로 13턴 = 플레이어 7턴 + AI 6턴.
링버퍼는 500이고 `dropped=0`이므로 유실이 아니다.

→ **플레이어 7턴 중 5턴만 발사했다 = 0.71 발사/턴.** 나머지 2턴은 몰수
(`GameManager.cs:1764-1767` `ForfeitPlayerTurn`)이거나 경기 종료로 소실됐다.

> 표본 1판이므로 `[OBSERVED, n=1]`이다. 일반화하면 안 된다.
> 다만 방향은 명확하다 — 실제 커밋률은 상한 1.0/턴보다 **낮다**.

### 4.4 같은 덤프에서 나온 별개의 발견 [OBSERVED]

5건의 Volley가 **전부 같은 값**이다:

```
power = 60.357147 %,  angle = 45.0°   (5/5 동일, 비트 단위로)
```

역산: `Lerp(3, 25.2, 0.55) = 15.21`, `15.21 / 25.2 × 100 = 60.357142…`
이는 `LaunchManager.cs:43-44`의 **기본값 `aimAngleDegrees = 45f`, `aimPower = 0.55f`와 정확히 일치**한다.

→ **그 판에서 플레이어는 각도와 파워를 단 한 번도 건드리지 않았다.**
드래그 제스처는 포인터 위치에서 속도를 만들므로(`LaunchManager.cs:762-769`)
5회 연속 동일 값이 나올 수 없다. 키보드 경로(`:656` `launchVelocity = GetSeparatedAimVelocity();`
→ `:660` `if (Input.GetKeyDown(KeyCode.Space)) LaunchUnit();`)로 기본값을 그대로 쐈다.

§2.4의 계산과 합치면: **기본값 그대로 쏘면 내성(x=7)에 맞는다.** 조준하지 않아도 성에는 맞는다.

> 표본 1판, 그리고 개발자 본인의 플레이일 가능성이 높다. `[OBSERVED, n=1]`.
> 그러나 "기본 조준이 이미 유효타"라는 것은 계산으로도 확인되는 구조적 사실이다.

---

## 5. 결론

**castle-war의 플레이어는 5분짜리 한 판에서 약 200초, 즉 62%를 조작 불가 상태로 보낸다.**
조작 가능한 시간은 122초뿐이고, 그 안에서 내리는 커밋 결정은 21.4회(Last Stand 포함 최대 22.4회)
— 경기 전체로 환산하면 평균 14.3초에 한 번이다.

그 62%는 두 덩어리로 갈린다. 절반 조금 넘는 110초(34%)는 **적 턴 전체**다.
적 턴에는 입력 표면이 하나도 열려 있지 않다 — 조준 게이트가 닫히고(`LaunchManager.cs:529-532`),
그 위에 컴포넌트 자체가 꺼지며(`GameManager.cs:2069`), 배치·Last Stand·벽돌 지정도 같이 닫힌다.
나머지 90초(28%)는 **자기 턴에 발사 버튼을 놓은 뒤** 날아가는 것을 보는 시간이다:
비행 2.0s + 배럴 도화선 지분 0.67s + 착탄 홀드 0.35s + 정착 1.2s = 턴당 4.22초.

숫자가 나온 경로는 이렇다. 경기 길이는 출하 상수로 321초다(재료 1585 ÷ 턴당 피해 37 = 42.8턴,
× 턴 평균 7.5초 — `MatchLengthModel.cs:45,51`, `StageDefinitions.cs:126`, `CastleCoreGimmick.cs:39`).
꼬리 4.22초와 적 턴 선지연 0.9초(`GameManager.cs:2159` + `SimpleAI.cs:30`)는 전부 코드 상수이고,
비행 2.0초는 물리에서 유도해 실측 1.63초 핸드오프와 2% 안에서 맞췄다.
남은 미지수인 조준 소요는 턴 평균 제약에서 5.67초로 역산된다.
정리하면 **유휴 비율 = 1 − D/15**이며, 이 값은 경기 길이·벽 두께·턴 수와 무관하다.
가정을 흔들면 45~62% 밴드가 나오고, 코드 근거가 가장 강한 조합이 62.2%다.

주목할 점은 이 62%가 **버그가 아니라 켜져 있는 스위치의 결과**라는 것이다.
적 턴에 클릭해 벽돌을 예약하는 창이 이미 구현되어 있고(`BrickPlacementController.cs:92-95`),
유닛 카드 선택도 구현되어 있다(`GameManager.cs:1707`). 둘 다 `enforceOneShotTurns = true`
(`GameManager.cs:144`) 하나에 의해 꺼져 있다. 적 턴을 되살릴 코드는 이미 리포지토리 안에 있다.

---

## 6. 검증 상태

| 항목 | 상태 |
|---|---|
| 코드 수정 | **없음** (읽기 전용 — 인용한 모든 라인은 미변경) |
| 프로젝트 테스트/린터 실행 | **안 함** (지시대로) |
| 비행 시간 유도 | 실측 1건(`playmode-hero-growth-contract.xml:231`, 1.63s)과 오차 2.0%로 대조 |
| 경기 길이 유도 | `StageDefinitions.cs:128` 주석의 "~321s"와 일치 |
| `s = 7.5`의 의미 | 모델 자신의 산술(2.1 × 0.5 ≈ 1.0)로 교차 확인 |
| 미검증 | `D = 5.67s` — 관찰 기반 상수에서 역산. 직접 실측 경로는 §3.6 |
| 표본 한계 | 텔레메트리 대조는 실플레이 1판(`n=1`) |

### 후속 제안 (이 문서 밖)

`Telemetry.cs:101`의 `Volley` 이벤트에서 `d` 필드가 상수 `0f`로 비어 있다.
여기에 "턴 시작 → 발사 커밋" 경과 초를 넣으면 `D`가 직접 측정되고,
§3.5의 감도표 전체가 하나의 실측치로 대체된다. 필드는 이미 존재하고 계약도 이미 흐른다.
