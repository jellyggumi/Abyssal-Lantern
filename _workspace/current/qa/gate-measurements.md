# Gate measurements — G1–G8

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: game-qa lane (측정) / director (판정)
- date: 2026-08-13
- 규칙: **측정값 + 측정 방법 + 증거 경로** 세 가지가 없으면 FAIL. 형용사는 게이트를 통과하지 못한다.

---

## 현황 요약

| 게이트 | 측정값 | 방법 | 증거 | 판정 |
|---|---|---|---|---|
| G1 세계관 | — | 문자열 전수 감사 | — | **FAIL (미측정)** |
| G2 밸런스 | **2026-08-12: 선공 87.0% / 교대 49.0%** (**출시 턴 순서만으로 38%p 격차**) → **2026-08-13: 고정 선공 47.0% / 교대 53.0% / 첫-무버 47.0%** (모두 45–55 밴드 내) | 대칭 AI 심 100매치 + 1000매치 회귀 assertion; production PlayMode에서 turn-0 capture → 지연 impact 경로 검증 | `evidence/g2-winrate-measurement.txt`, `TestResults/g2-opening-balance.log`, `TestResults/pr44-final-editmode-v2.xml` (45/45), `TestResults/pr44-damage-hardened-v5.xml` (13/13), `TestResults/pr44-final-playmode-v4.xml` (54/54 prior full baseline) | **FAIL — 수치 밴드와 runtime route correctness는 확인; 대칭 ≥20매치 runtime 승률 표본 부재** |
| G3 아키타입 | — | 로테이션 5종 ×5매치 | `playtest-report.md` (빈 표) | **FAIL (미실시)** |
| G4 몰입 | — | 구조화 채점 8장면 | `playtest-report.md` (빈 표) | **FAIL (미실시)** |
| G5 매출 | — | 공정성 심 + pm 감사 | — | **FAIL (pm 레인 부재)** |
| G6 운영 | 부분 | 텔레메트리 커버리지 | 아래 §G6 | **FAIL (perf·rollback 미비)** |
| G7 코어루프 | — | `Telemetry.RepeatRate()` ≥20세션 | — | **FAIL (미측정)** |
| G8 참신성 | 빈도 ✅ / 인상 — | 서베이 12표본 + 채점 | `.survey/siege-artillery-landscape/` | **FAIL (절반)** |

**통과: 0 / 8.** G2 수치 밴드와 production PlayMode damage-route correctness는 확인됐다. 그러나 대칭 ≥20매치 runtime 승률 표본이 없어 G2는 FAIL이며, 나머지 게이트도 각 증거 블로커가 남아 있다.

---

---
## G2 — 재측정 완료, 첫-무버 교정됨 [OBSERVED 2026-08-13]

**경과**: PR#44 당김 발사체 개편 후 열린 퀵 스팟 진행. 기준선 2026-08-12 87.0% 대비 출시 턴 순서 이점을 0.5 배수로 정정.

측정: `SiegeDuelSimulation`, 100매치, `SiegeBalanceSettings.Default`(추가 개편 없음)
명령: `Unity -batchmode -quit -executeMethod CastleBusters.EditorTools.G2Measurement.Run`
증거: `TestResults/g2-opening-balance.log` (100매치), G2 회귀 1000매치 밴드 통과 (`TestResults/pr44-final-editmode-v2.xml` 내 assertion pass)

| 조건 | 플레이어 승률 | 선공 승률 | 평균 턴 | 평균 길이 | G2 밴드 |
|---|---|---|---|---|---|
| **고정 선공 (출시 턴 순서)** | **47.0%** | 47.0% | 39.4 | 295s | **INSIDE** |
| **교대 (밸런스 격리)** | **53.0%** | 53.0% | 39.4 | 295s | **INSIDE** |
| 회귀 1000매치 (첫-무버·고정) | — | — | — | — | **INSIDE** (밴드 assertion pass; exact aggregate values were not emitted) |

### 해석 — 0.5 배수 적용 후 밴드 수렴

고정/첫-무버/교대 조건 모두 **45–55 밴드 내**로 수렴. 1000매치 회귀에서도 경계 내 안정(assertion pass).

G2가 FAIL인 이유는 damage route가 아니라 **대칭 ≥20매치 runtime 승률 측정 부재**다. Turn-0의 0.5 ownership capture는 production PlayMode에서 committed melee, arrow, cannon splash, launched-barrel fuse, launched-unit → production field-keg handoff와 같은 프레임의 경쟁 hit까지 13/13 통과했다. 첫 fatal hit 이후의 damage entry는 ownership/multiplier를 덮어쓰지 않으며, 지연 impact와 chain explosion도 turn handoff 뒤 같은 capture를 적용한다 (`TestResults/pr44-damage-hardened-v5.xml`). 전체 focused PlayMode 기준선도 54/54다 (`TestResults/pr44-final-playmode-v4.xml`; 새 fatal-context 회귀 추가 전 기준선). 반면 `SiegeDuelSimulation`은 실제 AI 오차 곡선·Last Stand·플레이어 입력을 포함하지 않으므로, 45–55%의 full-match runtime 결론이나 G2 PASS는 아직 주장할 수 없다.

---

측정: `SiegeDuelSimulation`, 100매치, `SiegeBalanceSettings.Default`
(keep 1440 / shot 106 / aim 0.70 / err 0.09 / 7.5s per turn)
명령: `Unity -batchmode -quit -executeMethod CastleBusters.EditorTools.G2Measurement.Run`
증거: `evidence/g2-winrate-measurement.txt`, `evidence/editmode-duel-sim.xml`

| 조건 | 플레이어 승률 | 선공 승률 | 평균 턴 | 평균 길이 | G2 밴드 |
|---|---|---|---|---|---|
| **선공 고정 (출하 턴 순서)** | **87.0%** | 87.0% | 38.8 | 291s | **OUTSIDE** |
| 선공 교대 (밸런스 격리) | 49.0% | 87.0% | 38.8 | 291s | INSIDE |

### 발견 — 선공 이점이 38%p다

양측이 성 하나를 부수는 데 약 19.4턴이 걸린다. 먼저 쏘는 쪽이 **한 턴 일찍 도달**하고,
조준 오차(±0.09)의 분산으로는 그 격차를 뒤집지 못한다.
**밸런스 모델 자체는 공정하다(교대 49.0%). 불공정한 것은 턴 순서다.**

### 시드 안정성

| 시드 | 1 | 1000 | 20000 | 999983 |
|---|---|---|---|---|
| 교대 승률 | 54.0% | 48.0% | 50.0% | 47.0% |

±3.5%p — n=100의 예상 노이즈(약 ±5%p) 안이다. 측정이 시드에 휘둘리지 않는다.

### 실력 민감도 — 재측정 2026-08-14 [OBSERVED]

측정: 같은 `G2Measurement.Run`, `SiegeDuelSimulation` 100매치, 교대(순서 중립).
증거: `evidence/g2/g2-remeasured-20260814.log`

| 조준 우위 | +0.00 | +0.01 | +0.03 | +0.05 | +0.10 |
|---|---|---|---|---|---|
| 승률 (2026-08-14) | **53.0%** | **67.0%** | **94.0%** | **100.0%** | 100.0% |
| 승률 (2026-08-12 기준선) | 49.0% | 60.0% | 90.0% | 96.0% | 100.0% |

**조준 품질 0.01(1%p)이 승률을 14.0%p 움직인다** — 기록된 11%p보다 **악화**됐다.
구간 기울기: 0.00→0.01 **14.0**pp, 0.01→0.03 **13.5**pp, 0.03→0.05 **3.0**pp(포화).
밴드 근방 선형 기울기 **1,400pp / 조준 1.0 단위**.

함의 — **그리고 그 함의가 같은 날 뒤집혔다. 아래 정정을 먼저 읽어라.**
1. 난이도 램프(`aiError` 2.5 → 0.8)가 **엄청난 일을 하고 있다** — 이 값이 곧 승률이다.
2. G5의 "과금 승률 격차 ≤5%p" 허용치가 조준 우위 **0.36%p**에서 소진된다(5pp ÷ 1,400pp).
   Lane D가 독립 재현했다. **← 이 값은 심의 허용치다. §정정 참조.**
3. EGF 레이팅 모델에서 바둑 1등급이 +13.7%p이므로 같은 크기다(모델 기대값 대 심 측정 —
   0.3%p 차이는 우연이며 "일치한다"고 주장하지 않는다).

### 정정 2026-08-14 — 이 절벽은 심의 성질일 가능성이 높다 [OBSERVED]

**위 수치는 전부 `SiegeDuelSimulation`의 피해 모델 위에서 나온 것이고, 그 모델의 분산이
실측과 8~18배 어긋난다.** Lane B가 자기 보고를 검증하다 이 문서의 형제 파일을 찾았고,
본 세션이 산수를 독립 재현했다.

심의 피해식은 `base × clamp01(quality)` — 연속이고 **0이 절대 나오지 않는다.** 실측은
`qa/b1-measurement-findings.md` §1.2에 이미 있었다:

| 스테이지 | 평균 샷피해 | 0피해 턴 | **CV** | `sd(N) = √(1440/μ)·CV` |
|---|---:|---:|---:|---:|
| Stage1 | 96.59 | 6/22 (27%) | **1.50** | **5.79발** |
| Stage2 | 128.33 | 1/6 (17%) | **0.70** | **2.34발** |
| Stage3 (재측정) | 128.00 | 6/14 (43%) | **1.34** | **4.49발** |
| — 심의 가정 | 106 | 0% | 0.0847 | **0.31발** |

Lane B의 닫힌 형태 `승률 = Φ(Δ발수 / (sd·√2))`에 **실측 sd**를 넣으면:

| 근거 | sd(발수) | 조준 +0.01 승률 | G5 허용치 |
|---|---:|---:|---:|
| 심 모델 | 0.31 | **67.0%** | 0.20%p |
| Stage1 실측 | 5.79 | **51.4%** | 3.68%p |
| Stage2 실측 | 2.34 | **53.4%** | 1.49%p |
| Stage3 실측 | 4.49 | **51.8%** | 2.85%p |

**세 스테이지 모두 55% 유지 요건(sd ≥ 1.58발)을 이미 충족한다.** 즉 실물 게임에는
심이 보여준 절벽이 없을 가능성이 높다 — 실물은 물리·붕괴 연쇄·자기 파괴가 만드는 두꺼운
꼬리를 갖고 있고 심에는 그것이 전부 빠져 있다.

**따라서 위 §함의 2의 0.36%p는 심의 허용치이며, 실측 기반 허용치는 1.5~3.7%p다.**
여전히 좁지만 "유료 요소 하나가 즉시 밴드를 깬다"는 강도는 아니다.

**미해결 — 이 정정도 확정이 아니다.** (가) b1도 스크립트 플레이어(45°·당김 86%, 학습
없음) 측정이고 표본이 22발·6발·14발로 작다. (나) `Φ()` 근사는 거의 정규인 심 분포에
대해 검증됐고 CV 1.5의 두꺼운 꼬리에서는 거칠어진다 — 51~53%는 방향과 크기의 추정이다.
(다) 그러나 **8~18배의 CV 차이는 근사 오차로 설명되지 않는다.**
이 세션이 붙인 런타임 CV 계측(`TelemetrySink.PlayerShotMaterialCv`)이 이 표를 확인하거나
반박한다 — 실측 CV가 0.7~1.5를 재확인하면 절벽 없음, 0.1대면 b1이 틀렸고 절벽 실재다.

### 확정 2026-08-14 — 런타임 계측이 그 예측을 확인했다 [OBSERVED]

위 문단이 정한 판정 기준("실측 CV가 0.7~1.5면 절벽 없음, 0.1대면 절벽 실재")에 따라
런타임에서 실측했다. 프로브 `AimErrorConversionProbe`, PlayMode, Stage1 3조건.
증거: `evidence/g2/aim-error-conversion.md`

| | CV | `sd(N)` | 조준 +0.01 승률 | G5 허용치 |
|---|---:|---:|---:|---:|
| **런타임 실측 (본 프로브)** | **1.06 / 1.04 / 1.15** | **4.17발** | **51.9%** | **2.65%p** |
| b1 Stage1 (독립 측정) | 1.50 | 5.79발 | 51.4% | 3.68%p |
| 심 가정 | 0.0847 | 0.31발 | 73.7% | 0.20%p |

**두 독립 계측이 같은 자리를 가리킨다** — b1은 턴별 재료 차분, 본 프로브는 피해 적용
지점 누적(`TelemetrySink.NoteMaterialRemoved`). 코드 경로가 다르고 크기가 같다.

**판정: 절벽은 심의 성질이었다.** 요건 sd ≥ 1.58발에 대해 실측 4.17발은 **2.6배**다.
이 게임은 조준 1%p 우위에서 승률 55%를 유지할 분산을 이미 갖고 있다.

**G5 함의**: 허용치가 **2.65%p**이고 심이 함의한 0.36%p의 **7배**다. 조준을 돕는 유료
요소가 즉시 밴드를 깨지는 않는다. 다만 2.65%p도 넉넉하지 않으므로 **조준 보조 계열은
여전히 G5 심사 대상**이다.

**남은 한계**: 이것도 스크립트 플레이어(45°·당김 86%) 측정이고 **인간의 발간 분산은
여전히 미측정**이다. b1도 같은 조건이었으므로 두 값의 대조는 유효하지만, 둘 다 인간을
재지 않았다. 인간이 더 일정하게 쏘면(CV가 낮으면) 절벽이 되살아난다.

### 절벽 측정이 말하지 않는 것

> **이 수치는 LAST STAND가 없는 상태에서 측정됐다.** `SiegeDuelSimulation`은 Last Stand·
> AI 전용 오차 곡선·플레이어 입력을 포함하지 않는다(§46). 따라서 "LAST STAND에도
> 불구하고 절벽이 남았다"고 말할 수 없다. 말할 수 있는 것은 둘로 나뉜다 — 절벽은
> LAST STAND 없이 측정됐다(미측정), 그리고 LAST STAND의 구조는 절벽을 완화하는 종류가
> 아니다(코드 근거). 후자의 근거는 `ComebackAsymmetryTests`와 Lane D C-2다.

### 이 측정이 말하지 않는 것 [중요]

> [!warning] "출하 게임의 승률이 87%다"라고 읽으면 오독이다
> 심은 **양측 실력이 같다**고 가정한다. 실제 AI는 다른 오차 모델을 쓴다 —
> `aiErrorStart 2.5 → aiErrorEnd 0.8`로 초반에 크게 빗나가고 점점 정확해진다.
> Last Stand(코어 35%에서 플레이어 2.2× / AI 1.6×)도 모델에 없다.
>
> 심이 말하는 것은 정확히 이것이다: **동일 실력 대칭 듀얼에서 턴 순서만으로 38%p가 갈린다.**
> 실제 승률은 PlayMode 측정이 필요하며 `test-plan.md`에 후속으로 명시되어 있다.

또한 심은 물리·블록 배치·붕괴 연쇄·지형·바람을 다루지 않는다(씬 밖에 존재하지 않는 것들).
구조적 불균형은 잡지만, 블록이 서로 무너져야 드러나는 결함은 잡지 못한다.

### 판정

**FAIL.** 출하 턴 순서에서 87.0%는 45–55% 밴드 밖이다.
다만 원인이 밸런스 수치가 아니라 **턴 순서**임이 함께 측정되었으므로,
FIX 방향은 밸런스 재튜닝이 아니라 선공 보정이다 → `production/decision-log.md` 필요.

---

## G6 — 부분 측정 [OBSERVED 2026-08-12]

### 텔레메트리 필드 커버리지

계약(`ops/telemetry-contract.md`) 5개 필드 전부 계측 완료.

| 필드 | 발신 지점 | 상태 |
|---|---|---|
| `match_start {stage_id, deck}` | `GameManager.BeginSiege()` | ✅ 구현 |
| `volley {unit, power, angle, wind}` | `LaunchManager.LaunchUnit()` | ✅ 구현 |
| `collapse {blocks, chain_depth}` | `GameManager.EndTurn()` (턴 경계 집계) | ✅ 구현 |
| `match_end {winner, turns, core_hp_delta}` | `GameManager.EndGame()` | ✅ 구현 |
| `session {stages_cleared, retry_count}` | `EndGame()` 동시 | ✅ 구현 |

수집: localStorage(PlayerPrefs→IndexedDB) 링버퍼 500 + 콘솔 덤프. 서버 0.

### 아직 없는 것

| 항목 | 상태 |
|---|---|
| p95 프레임 ≤16.7ms | 미측정 — `engineering/perf-budget.md` 부재 |
| 롱프레임 <0.5% | 미측정 |
| 30분 소크 메모리 안정 | 미측정 |
| 입력 지연 ≤100ms | 미측정 |
| `ops/rollback-runbook.md` 테스트 1회 | 미실시 |
| `ops/release-readiness.md` | 부재 |

---

## G8 — 빈도 조건 [OBSERVED 2026-08-12]

12개 비교작 표본. 임계값 "≥5개 중 ≤2개".

| 요소 | 빈도 | 통과 |
|---|---|---|
| N-1 양방향 턴제 × 구조 붕괴 × 방어 코어 | **0 / 12** | ✅ |
| N-2 발사체 규칙 강제 순환 | **0 / 12** | ✅ |
| N-3 프리뷰 = 실전 물리 | 2 / 12 | ✅ |
| (새총 드래그-릴리스) | 2 / 12 | ✅ |

증거: `.survey/siege-artillery-landscape/solutions.md#frequency-ranking` (validator PASS)

**인상 점수: 미측정.** 빈도 **AND** 인상 둘 다 필요하므로 G8은 FAIL.

한계: "0 / 12"는 12개 표본 내 관측이며 `[INFERENCE]`다.
**검색 부재는 부재의 증명이 아니다.** 미조사 영역 — itch.io 인디, 중국·동남아 모바일, Roblox.

---

## 검증 이력 [OBSERVED]

### 2026-08-12 — Unity 6000 업그레이드 + 측정 인프라

| 항목 | 결과 |
|---|---|
| 엔진 | 2022.3.62f2 → **6000.5.6f1** (사용자 결정). 사전 태그 `pre-unity6000-upgrade-20260812` |
| **EditMode 전체** | **384개 중 383 통과** (`evidence/editmode-duel-sim.xml`) |
| 유일한 실패 | `SpriteAtlasPacker_PacksSpritesCorrectly` — **단독 실행 통과(1/1)**. 원인은 코드가 아니라 Unity MCP 플러그인의 인증 실패 로그를 NUnit이 미처리 에러로 집계한 것 |
| 신규 코드 | `Telemetry.cs` · `TelemetrySink.cs` · `SiegeDuelSimulation.cs` · `G2Measurement.cs` |
| 신규 테스트 | `TelemetryTests.cs` 10개 · `SiegeDuelSimulationTests.cs` 12개 |
| G2 실측 | 위 §G2 |

### 업그레이드에서 실제로 깨진 것

API 업데이터가 자동 처리한 것과 **수동 개입이 필요했던 것**을 구분해 남긴다.

| 항목 | 처리 |
|---|---|
| `Rigidbody2D.velocity` → `linearVelocity` 외 다수 | API 업데이터 자동 (11개 파일) |
| `GetInstanceID()` 6곳 | **수동** — `UnityUpgradable` 표시가 없어 업데이터가 못 고침. `GetEntityId()`로 치환 |
| `EntityId → int` 암묵 변환 6곳 | **수동** — 치환 후 2차로 드러남. `HashSet<int>`/`Dictionary<int,…>` 선언 자체를 `EntityId` 키로 변경 |
| TMP 번들 예제 63건 (`Vector2[]` → `Vector4[]`) | **삭제** — `Examples & Extras` 193파일. 코드·씬·테스트 참조 0건 확인 후 제거 |

> [!note] 업그레이드는 3단 계단이었다
> `GetInstanceID` 하나를 고치면 `EntityId` 변환이 드러나고, 그걸 고치면 TMP 예제가 드러났다.
> 각 단계가 이전 단계를 고쳐야만 보였다 — 한 번의 컴파일로 전체 목록을 얻을 수 없는 형태다.
> 되돌리려면 이 세 가지를 함께 되돌려야 하며, `GetEntityId`는 2022.3에 존재하지 않는다.

---

## B-5 검증 완료 [OBSERVED 2026-08-12]

### PlayMode — 54개 중 49 통과

실패 5건 전수 분류는 `playmode-6000-triage.md`. 요약:
**실제 게임 에러(`error CS`·셰이더·`BuildFailedException`) 0건**,
5건 중 4건이 선행 결함 또는 환경 노이즈(MCP 인증 로그 2회, TMP 버전 체크 오탐 1회),
1건(D-016)은 거동 변화로 재조사 대상.
증거: `evidence/playmode-6000.xml`, `evidence/playmode-6000-isolated.xml`

### WebGL 빌드 — 성공

| 항목 | 값 |
|---|---|
| 결과 | `result=Succeeded` |
| 크기 | **95,214,964 bytes** (2022.3의 93,068,518에서 +2.3%) |
| 압축 | **gzip 확인** — 3개 파일 전부 매직바이트 `1f8b` |
| 폴백 | `.unityweb` 확장자 = decompressionFallback ON (CLAUDE.md §6 계약 충족, Brotli 아님) |
| 실제 에러 | **0건** (보고된 `errors=2`는 MCP 노이즈) |
| Unity MCP 패키지 11종 | 빌드를 막지 않음 |

증거: `evidence/webgl-6000-build.txt`

> [!warning] 첫 빌드는 내 실수로 실패했다
> WebGL 빌드가 도는 중에 PlayMode 테스트를 동시 실행했고, 그 명령의
> `rm -f Temp/UnityLockfile`이 **빌드가 쥔 락을 지웠다.** 두 Unity가 `Temp/`를
> 놓고 싸워 Burst가 중간 파일(`lib_burst_generated_part_*.bc`)을 잃었다.
> 타임라인으로 확정: D-016 시작 `16:02:37`, 빌드 종료 `16:03:58` — **80초 중첩.**
> `CLAUDE.md` §5가 이미 경고한 것(배치 모드와 열린 에디터는 프로젝트 락을 두고 싸운다)을
> 내가 어겼다. Burst/Bee 캐시를 지우고 단독 재실행해 성공했다.

### 라이브 부팅 — 확인

| 항목 | 값 |
|---|---|
| 로딩바 숨김 | ✅ (성공 콜백 전용 코드 경로) |
| 캔버스 | 1706×960 |
| JS 에러 / 페이지 에러 | **0 / 0** |
| 한글 렌더 | 정상 (타이틀·HUD·결과 화면) |

### 텔레메트리 — 실제 배포 빌드에서 종단 작동 확인 ✅

한 판(13턴)을 실제로 플레이해 얻은 실측 덤프. 증거: `evidence/telemetry-live-webgl6000.json`

```
[telemetry] events=21 dropped=0 winRate=0.0% avgTurns=13.0 repeatRate=0.0%
```

| 검증 항목 | 결과 |
|---|---|
| 5종 이벤트 전부 발생 | ✅ MatchStart 1 · Volley 5 · Collapse 13 · MatchEnd 1 · Session 1 |
| 발사체 규칙 순환 | ✅ Barrel → Knight → Archer → Barrel → Knight |
| 각도(b) 실값 | ✅ 45.0 |
| **바람(c) 실값** | ✅ 1.65 / 0.25 / 0.44 / 1.59 / 1.79 — **스텁이 아니라 실제로 흐른다** |
| MatchEnd | ✅ `winner=enemy turns=13 coreHpDelta=-76.5` |
| 집계 정확성 | ✅ 패배했으므로 winRate 0.0%, 첫 세션이므로 repeatRate 0.0% |
| 링버퍼 | ✅ dropped=0 |

> [!success] 이전 세션의 내 주장 두 가지가 틀렸음이 밝혀졌다
> (1) "릴리스 WebGL 빌드는 `Debug.Log`를 콘솔에 내보내지 않는다" — **틀렸다.** 위 덤프가
> 콘솔에서 그대로 나왔다. (2) "그래서 jslib로 `window.castleWarTelemetry`에 게시하도록 고쳤다"
> — **그런 jslib는 존재한 적이 없다.** `find Assets -name "*.jslib"` = 0건이며
> `TelemetrySink.Dump()`는 `Debug.Log` 두 줄이 전부다. 수집 경로는 처음부터 콘솔이었고,
> 콘솔은 처음부터 작동했다. 빌드 설정도 이를 뒷받침한다 —
> `exceptionSupport = ExplicitlyThrownExceptionsOnly`(None이 아니다), 로깅 비활성 설정 없음.

### 실측이 드러낸 결함 — `chainDepth`가 항상 0

13건의 Collapse 이벤트 전부 `b`(chainDepth) = 0이다. **11블록이 한 턴에 무너진 이벤트조차 0이다.**

블록 수(a)는 정상으로 흐른다: `1, 3, 6, 11, 5, 2, 1, 1, 3, 4, 2, 5, 3`.

원인 후보 둘, **아직 가르지 못했다**:
1. 구현 결함 — `OnCollisionEnter2D`의 낙하 전파가 실제로 안 걸린다
2. 측정은 맞고 현상이 없다 — 블록이 **낙하 충격이 아니라 폭발 피해로** 죽는다.
   낙하 피해 상한은 45이고 Stone은 85 HP라 한 번 맞아서는 죽지 않는다

> 후자라면 이 필드는 설계상 **실전에서 0이 아닐 수 없다** — 즉 지표로서 쓸모가 없다.
> 둘 중 어느 쪽인지 가르기 전에는 `collapse.chain_depth`를 게이트 근거로 쓰면 안 된다.

---

## 남은 블로커

| # | 블로커 | 막는 게이트 |
|---|---|---|
| B-1 | ~~대칭 AI 심 부재~~ → **해소.** G2 실측 완료 | ~~G2~~ |
| B-2 | 아키타입 로테이션 미실시 | G3 · G4 · G8 인상 |
| B-3 | pm 레인 부재 (reward-bands) | G5 |
| B-4 | perf-budget · rollback-runbook · release-readiness 부재 | G6 |
| B-5 | ~~6000 배포 경로 미검증~~ → **해소.** PlayMode·빌드·부팅·텔레메트리 전부 확인 | ~~G6·배포~~ |
| B-6 | G7 세션 로그 미수집 (≥20세션) | G7 |
| **B-7** | **`collapse.chain_depth`가 항상 0** — 구현 결함인지 측정상 정상인지 미확정 | G4 · G7 보상 밀도 |
| **B-8** | **라이브 사이트가 아직 2022.3 빌드** — 6000 빌드는 로컬에만 존재 | 배포 |
