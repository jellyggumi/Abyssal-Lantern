# Telemetry contract — castle-war (G6-ops draft)

- run-id: 20260809-castle-war-stage1
- status: draft — fields defined, emission NOT yet implemented [TARGET]

## Fields (PM forecast + QA verification)

| Field | Type | Emitted at | Consumer |
|---|---|---|---|
| match_start {stage_id, deck} | event | 매치 시작 | QA G3 archetype table |
| volley {unit, power, angle, wind} | event | 발사마다 | designer 밸런스 리플레이 |
| collapse {blocks, chain_depth} | event | 붕괴 체인 종료 | G4/G7 reward 밀도 |
| match_end {winner, turns, core_hp_delta} | event | 매치 종료 | G2 win-rate, G5 fairness |
| session {stages_cleared, retry_count} | aggregate | 세션 종료 | G7 repeat-rate proxy |

## Current state [OBSERVED]

빌드는 웹 정적 호스팅(GitHub Pages) 대상이라 서버 수집이 없다. Stage 1
draft 구현은 localStorage 링 버퍼 + 콘솔 덤프(수동 수거)로 충분하다.
Stage 2에서 QA 시뮬레이션이 이 스키마 그대로를 파일로 기록한다.

## Rollback runbook (draft)

웹 배포 롤백 = pages repo에서 `games/castle-war/` 이전 커밋 revert 후
push. 테스트 1회는 Stage 3 G6 final 전에 수행. [TARGET]
