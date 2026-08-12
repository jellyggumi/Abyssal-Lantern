# Gate measurements — G1–G8

- run-id: 20260809-castle-war-stage1 (cycle 2)
- owner: game-qa lane (측정) / director (판정)
- date: 2026-08-12
- 규칙: **측정값 + 측정 방법 + 증거 경로** 세 가지가 없으면 FAIL. 형용사는 게이트를 통과하지 못한다.

---

## 현황 요약

| 게이트 | 측정값 | 방법 | 증거 | 판정 |
|---|---|---|---|---|
| G1 세계관 | — | 문자열 전수 감사 | — | **FAIL (미측정)** |
| G2 밸런스 | **선공 87.0% / 교대 49.0%** | 대칭 AI 심 100매치 | `evidence/g2-winrate-measurement.txt` | **FAIL (선공 밴드 밖)** |
| G3 아키타입 | — | 로테이션 5종 ×5매치 | `playtest-report.md` (빈 표) | **FAIL (미실시)** |
| G4 몰입 | — | 구조화 채점 8장면 | `playtest-report.md` (빈 표) | **FAIL (미실시)** |
| G5 매출 | — | 공정성 심 + pm 감사 | — | **FAIL (pm 레인 부재)** |
| G6 운영 | 부분 | 텔레메트리 커버리지 | 아래 §G6 | **FAIL (perf·rollback 미비)** |
| G7 코어루프 | — | `Telemetry.RepeatRate()` ≥20세션 | — | **FAIL (미측정)** |
| G8 참신성 | 빈도 ✅ / 인상 — | 서베이 12표본 + 채점 | `.survey/siege-artillery-landscape/` | **FAIL (절반)** |

**통과: 0 / 8.** 대부분 "나쁘다"가 아니라 "아직 재지 않았다"였다.
G2는 이제 **재고 나서 실패한** 첫 게이트다 — 아래 §G2 참조.

---
## G2 — 측정 완료, 실패 [OBSERVED 2026-08-12]

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

### 실력 민감도 — 더 중요한 발견일 수 있다

| 조준 우위 | +0.00 | +0.01 | +0.03 | +0.05 | +0.10 |
|---|---|---|---|---|---|
| 승률 | 49.0% | **60.0%** | 90.0% | 96.0% | 100.0% |

**조준 품질 0.01(1%p) 차이가 승률을 11%p 움직인다.** 곡선이 매우 가파르다.

함의 두 가지:
1. 난이도 램프(`aiError` 2.5 → 0.8)가 **엄청난 일을 하고 있다** — 이 값이 곧 승률이다.
2. G5의 "과금 승률 격차 ≤5%p" 기준이 이 민감도 위에서는 **매우 좁은 여유**다.
   조준을 조금 돕는 유료 요소 하나가 즉시 밴드를 깬다.

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

## 남은 블로커

| # | 블로커 | 막는 게이트 |
|---|---|---|
| B-1 | ~~대칭 AI 심 부재~~ → **해소.** G2 실측 완료 | ~~G2~~ |
| B-2 | 아키타입 로테이션 미실시 | G3 · G4 · G8 인상 |
| B-3 | pm 레인 부재 (reward-bands) | G5 |
| B-4 | perf-budget · rollback-runbook · release-readiness 부재 | G6 |
| B-5 | **6000 배포 경로 미검증** — PlayMode 미실행, WebGL 재빌드 미실시, Unity MCP 패키지 11종 6000 호환성 미확인 | G6 · 배포 |
| B-6 | G7 세션 로그 미수집 (≥20세션) | G7 |

> [!danger] B-5가 가장 위험하다
> EditMode 통과는 **WebGL 빌드 성공을 보장하지 않는다.** IL2CPP·셰이더·패키지 호환성은
> 별개 축이고, 라이브 사이트는 아직 2022.3 빌드를 서빙 중이다.
> **배포 전 재빌드와 라이브 검증이 필수다.**
