# Triage

- Problem: castle-war가 어느 장르 계보에 서 있는지, 그리고 **무엇이 실제로 참신한지**가 근거 없이 주장되어 있다. `_workspace/current/design/trend-survey/`에는 단일 레퍼런스(Archery Bastions) 1건만 있고, G8 게이트가 요구하는 **≥5개 비교작 빈도표는 `[TARGET]`으로 비어 있다**. 사용자가 지목한 포트리스·앵그리버드 계보도 문서화된 적이 없다.
- Audience: castle-war designer 레인(참신성 스코어카드 작성), director(G8 판정), PM(포지셔닝·수익 지점 설계). 2차로 이 게임을 설명해야 하는 모든 세션.
- Why now: G8은 Stage 2 종료 조건이고 현재 사이클은 Stage 1에 머물러 있다. 빈도표 없이는 G8이 **측정 불가 = FAIL**이다. 또한 2026-08-11의 원샷 턴 개편(#27)으로 게임의 장르 좌표 자체가 이동했다 — 물리 파괴 퍼즐에서 **턴제 포병 대전**으로. 개편 이전에 잡힌 레퍼런스 1건은 현재 게임을 설명하지 못한다.

## Survey run

```yaml
survey_run:
  primary_mode: market-landscape
  scope: broad
  evidence_floor: primary-pages-first
  output_language: user-language
  needs_platform_map: false
  reuse_existing: false   # 기존 archery-bastions 문서는 보존, 이번 조사가 상위 집합
```

`needs_platform_map: false` — 게임 장르 조사이며 에이전트/툴링 플랫폼 비교가 아니므로
Lane D는 `platform-map.md`가 아니라 **JTBD 대체재 / 인접 산업 병렬**을 `solutions.md`에 포함한다.
