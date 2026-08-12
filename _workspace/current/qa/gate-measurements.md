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
| G2 밸런스 | — | 대칭 AI 심 ≥100매치 | — | **FAIL (미측정)** |
| G3 아키타입 | — | 로테이션 5종 ×5매치 | `playtest-report.md` (빈 표) | **FAIL (미실시)** |
| G4 몰입 | — | 구조화 채점 8장면 | `playtest-report.md` (빈 표) | **FAIL (미실시)** |
| G5 매출 | — | 공정성 심 + pm 감사 | — | **FAIL (pm 레인 부재)** |
| G6 운영 | 부분 | 텔레메트리 커버리지 | 아래 §G6 | **FAIL (perf·rollback 미비)** |
| G7 코어루프 | — | `Telemetry.RepeatRate()` ≥20세션 | — | **FAIL (미측정)** |
| G8 참신성 | 빈도 ✅ / 인상 — | 서베이 12표본 + 채점 | `.survey/siege-artillery-landscape/` | **FAIL (절반)** |

**통과: 0 / 8.** 대부분 "나쁘다"가 아니라 **"아직 재지 않았다"** 이다.

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

### 2026-08-12 — 텔레메트리 구현

| 항목 | 결과 |
|---|---|
| 신규 코드 | `Telemetry.cs` (순수), `TelemetrySink.cs` (런타임) |
| 배선 | 4곳 (`BeginSiege` / `LaunchUnit` / `EndTurn` / `EndGame`) + `DestructibleBlock` 체인 깊이 |
| 신규 EditMode 테스트 | `TelemetryTests.cs` 10개 |
| **컴파일 검증** | dotnet 교차 컴파일 — **내 변경에서 비롯된 에러 0건** |
| **Unity 테스트 실행** | **미실시** — 아래 §블로커 |

#### 컴파일 검증의 한계 [중요]

핀된 에디터(2022.3.62f2)가 이 머신에 없어 **Unity 6000.5.6f1의 관리 어셈블리로
교차 컴파일**했다. 이는 프록시 검증이며 Unity 실제 컴파일과 동일하지 않다.

잔여 에러 9건은 전부 내 수정 라인 밖임을 git diff 라인 대조로 확인:
- 6건 `CS0619 GetInstanceID obsolete` — **Unity 6000에서만 deprecated**. 2022.3에서는 정상 API
- 3건 `CS0103` — `MobileStorefront` 제외로 인한 연쇄. 해당 파일은 `UnityEngine.Purchasing`
  패키지를 요구하는데 이 에디터 설치본에 없다

**내 신규 파일 2개는 에러 목록에 등장하지 않았다.**

---

## 블로커

> [!danger] 핀된 Unity 에디터 부재
> `ProjectSettings/ProjectVersion.txt`가 **2022.3.62f2**를 고정하는데
> 이 머신에는 **6000.5.6f1만** 설치되어 있다.
> 6000으로 프로젝트를 열면 **되돌릴 수 없는 업그레이드**가 발생하므로 열지 않았다.
> Unity Hub 릴리스 채널에 2022.3.x가 더 이상 노출되지 않아 changeset(`7670c08855a9`)
> 직접 설치를 시도 중이다.
>
> **따라서 EditMode 회귀(기존 338 + 신규 10 = 348 예상)는 아직 실행되지 않았다.**
> 이 문서의 어떤 게이트도 그 실행 없이 PASS로 바뀌지 않는다.
