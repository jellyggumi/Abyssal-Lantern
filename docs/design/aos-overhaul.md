# AOS Overhaul — 점령/파괴 목표 + 유닛 특성 + 이벤트 필드 (2026-07-03)

## 1. 승리 조건 (AOS-style)
- 기존: 적 코어 HP 0 → 승리 (유지, "망가뜨리기").
- 신규: **점령** — 공격측 유닛이 적 코어 반경(CaptureRadius 2.6u) 안에 수비 유닛 없이
  머무르면 점령 게이지가 차오르고(기본 6초), 가득 차면 즉시 승리.
  - 수비 유닛이 존 안에 있으면 게이지 정지(경합), 공격 유닛이 없으면 초당 50% 속도로 감쇠.
  - 규칙은 `CaptureRules.Tick` 순수 함수 — EditMode 테스트로 고정.

## 2. 유닛 공격 특성 (attackCount 1-base, 6/10 주기 순환)
| 유닛 | 규칙 | 구현 |
|---|---|---|
| Knight | 3번째 공격 2연타, 6번째 공격 3연타 (`UnitCombos.KnightHits`: n%6==0→3, n%3==0→2, else 1) | 0.14s 간격 다중 타격 코루틴 + DOUBLE!/TRIPLE! 라벨 |
| Knight | 전진 시 밀기: 이동 중 전방 적 유닛을 밀어냄 (situational push) | 전방 overlap 검사 → 적 rb에 수평 push velocity |
| Archer | 5번째 공격 더블샷, 10번째 공격 더블샷+공중(로브) 사격 연계 (`UnitCombos.ArcherVolley`: n%10==0→FrontAndLob, n%5==0→Double, else Single) | 0.18s 간격 연속 발사, 로브샷은 +55° 고각 |
| Archer | 상황부 점프: 목표가 1.2u 이상 높거나 전방이 막히면 점프 | 기존 장애물 점프 + 고저차 조건 추가 |
| Bomber | 자기 턴 3번째부터 2발, 9번째부터 4발 발사 (`VolleyRules.BomberVolleyCount(ownTurnOrdinal)`: ≥9→4, ≥3→2, else 1) | LaunchManager 0.16s 스태거 다중 발사 |
| Bomber | 착지 즉시 폭발 금지 → **착지 2초 후 폭발** (`UnitCombos.BomberFuseSeconds = 2`) | Armed 상태 + 점멸 텔레그래프, 사망 시엔 즉시 폭발 |

## 3. 화산/꽃가루 벤트 — 이벤트 스폰
- 고정 배치 제거. `VentSchedule.ShouldSpawnOnTurn(turn)`: turn%3==2 에 1기 스폰(스타일 교대),
  위치는 양 진영 사이 지형 위 랜덤 x∈[-7,7] (발사구 원, 코어 주변 제외), 수명 3턴 후 소멸.
- 벤트 내부 로직(EruptionVentGimmick)은 유지 — 스폰 오케스트레이션만 GimmickFieldDirector로 이동.

## 4. 전차(Chariot) 3페이즈
- 전차는 DestructibleBlock(HP 150)을 가진 파괴 가능 전쟁기계.
- 페이즈(HP 비율): P1(>2/3) 저속 순찰 / P2(1/3~2/3) 고속·광폭 스윕 / P3(<1/3) 돌진 램.
- **벽 파괴**: 이동 방향 블록과 접촉 시 램 데미지(쿨다운 0.8s).
- **중력**: Dynamic 바디 — 발밑 지형이 사라지면 낙하, y<-10 → 파괴 → **5초 후 리스폰**.
- **넉백/전복**: 폭발·벤트 컬럼의 힘을 받는 dynamic 바디 + 회전 허용(뒤집힘 가능).
- 필드 패트롤(kinematic sine)은 기존 동작 유지 — `chariotMode`로 분리.

## 5. 성벽 & 발사구
- 성벽(수비벽)은 매치 시작 시 지정 좌표에 런타임 생성. `LaunchRingRules.IsInsideRing`으로
  발사구 원(반경 3.5, ±14.5) 내부 생성 금지 — 필드 디렉터의 솔리드 스폰에도 동일 적용.
- 발사구 비주얼: gti(god-tibo-imagen) 생성 6프레임 포털 게이트 애니메이션
  (`Resources/Gimmicks/launch_gate_anim/`, GimmickFrameAnimator 8fps 루프).

## 6. 버프/너프/게이트 — 전세 기반 이벤트
- 상시 배치 제거. `BalanceEventPlanner.Plan(turn, playerFrac, enemyFrac)`:
  turn%4==1 에 이벤트 1건 — 열세측(코어 HP 비율 낮은 쪽) 접근로에 Buff 룬/PowerUp 게이트,
  우세측 접근로에 Debuff 룬/Reduce 게이트. 격차 0.15 미만이면 중립(센터 Multiply 게이트).
- 수명 4턴 후 소멸. 모든 규칙 순수 함수로 테스트 고정.
