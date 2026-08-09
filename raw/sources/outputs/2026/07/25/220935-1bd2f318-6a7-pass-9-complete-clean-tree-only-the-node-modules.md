---
type: "assistant-output"
session_id: "1bd2f318-6a7d-4953-b51c-020a4688"
captured_at: "2026-07-25T22:09:35.392367+00:00"
report: "[[wiki/reports/2026-07-25-220935-pass-9-complete-clean-tree-only-the-node-modules-answer]]"
---

# Pass #9 complete. Clean tree (only the `node_modules` symlink u…

## Output

```text
Pass #9 complete. Clean tree (only the `node_modules` symlink untracked, as expected), both commits on `studio-loop/main`.

## Pass #9 요약 — 축 4 (스테이지 구성 / 분위기)

**초점**: 스테이지별 안개 심도(fog near/far) 배선.

**발견 (실측)**: D22가 `applyStagePalette`로 스테이지별 안개/조명 **색**은 붙였지만, 안개 **심도**(near/far)는 `mount()`의 전역 상수 하나(`WORLD_SCALE*1.8/*4.2`)로 고정 — 10개 스테이지가 전부 동일한 대기 깊이로 읽혀, 축-4 핵심 질문("스테이지마다 시각적 차별점이 있는가")에서 가장 강한 분위기 레버가 균일했다. `stage-composition-20260725.md §3`은 스테이지마다 다른 안개 밀도를 명시 요구했으나 렌더러가 소비하지 않는 상태였다.

**구현**: `STAGE_FOG_MULTIPLIERS` 데이터 테이블 + `stageFogRange(stageId)` 순수 export 헬퍼 추가, `applyStagePalette`에서 색 직후 near/far 적용. 매직넘버 0 (전부 `WORLD_SCALE` 배수), 값은 §3 모티프 근거.

**측정**: far 스프레드 **1.87×** — Echo Throne 42.0 (가장 폐쇄, 공허/저해상 은폐) → Gate Zenith 78.4 (가장 개방, 정점 조망). 폐쇄 모티프(밤/서약/장막) 42–48, 개방 조망(황야/정점) 75–78.

**격리**: 안개는 순수 씬 렌더 상태 — 스냅샷/`getRunDigest` 미접촉(렌더러 단방향 유지), 신규 에셋·네트워크 0, PMREM/림(렌더러 게이트 부분) 미변경.

**검증**: `node --test` **190/189/0/1** (회귀 0, 결정론 테스트 포함 전부 통과). 신규 테스트가 실제 `applyStagePalette`를 실제 `THREE.Fog`에 10스테이지 구동 → near/far가 `stageFogRange` 오라클과 정확 일치(테이블 소비 증명) + 스프레드 ≥1.5× + 조망 2종 > 폐쇄 2종 실증. `world-presentation-browser.cjs` green (exit 0).

**커밋**: `31e506d` (feature) · `02b30ed` (D30 decision-log + retro + wiki).

**미해결 (다음 축-4 입력)**: (1) 조명 key/rim 방향·강도는 여전히 스테이지 무관 — 1순위 후보. (2) Glass Necropolis 환경맵 서사 결함(D22 deferred 유지, 스파이크 선행). (3) terrain GLB 임의각 감사 미수행. (4) "개방감 대비가 실제로 체감되는가"는 사람 브라우저 정성 검증 필요.
```
