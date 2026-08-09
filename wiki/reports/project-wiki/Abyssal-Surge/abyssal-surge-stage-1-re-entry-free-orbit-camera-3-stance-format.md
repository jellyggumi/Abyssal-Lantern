---
title: "Abyssal Surge — Stage 1 Re-entry: Free-Orbit Camera + 3-Stance Formation Redesign (2026-07-25)"
tags: ["abyssal-surge", "game-studio-harness", "free-orbit-camera", "formation-stance-system", "deterministic-simulation-qa", "service-worker-cache-trap", "cross-lane-integration-bug", "css-selector-collision", "parallel-subagent-verification", "gdd-simulation-mismatch", "cross-lane-css-collision", "cycle-close"]
created: 2026-07-25T12:31:44.815Z
updated: 2026-07-25T12:39:58.321Z
sources: ["_workspace/20260723-solo-warden-rpg-concept/production/decision-log.md D21/D22", "_workspace/20260723-solo-warden-rpg-concept/qa/review-checkpoint-1-20260725.md", "_workspace/20260723-solo-warden-rpg-concept/qa/review-checkpoint-2-20260725.md", "_workspace/20260723-solo-warden-rpg-concept/retrospectives/cycle-4-retrospective.md", "commit e450173"]
links: []
category: session-log
confidence: high
schemaVersion: 1
---

# Abyssal Surge — Stage 1 Re-entry: Free-Orbit Camera + 3-Stance Formation Redesign (2026-07-25)

# Stage 1 재진입 — 자유궤도 카메라 + 3-스탠스 편성 재설계

run-id: `20260723-solo-warden-rpg-concept` (repo: Abyssal-Surge / Abyssal-Command)

## 핵심 결정

**카메라 방향 결정 (D21)**: 8게임 리서치가 "장르 표준은 고정 카메라"(Diablo Immortal/Torchlight
Infinite 둘 다 고정)라는 상반된 증거를 제시했음에도, 사용자는 D17(이미 확정된 사용자 결정)의 완전한
자유 궤도 카메라 구현을 재확인했다. 이는 리서치가 기존 결정을 뒤집을 근거로 자동 적용되지 않고,
director가 새 증거를 사용자에게 명시적으로 알린 뒤 재확인받는 패턴을 보여준다 — "리서치가 다르게
말한다"는 이유만으로 이미 확정된 사용자 요청을 조용히 되돌리지 않는다.

**3-스탠스 편성 시스템**: 전열(Vanguard, FRONT 2)/포대(Turret, FRONT 0)/분산(Split, FRONT 1) —
GDD에 1 사이클 전부터 명세돼 있었으나(offset 벡터, 파생 FRONT수까지 완전 수치화) 실제 코드는 여전히
구식 2슬롯 FRONT/BACK 이진 토글이었다. 이런 "명세는 있으나 미구현" 격차는 게임 개발에서 흔하다 —
설계 문서와 라이브 코드를 항상 직접 대조 확인할 것.

## 방법론적 교훈 (재사용 가능)

1. **결정론적 시뮬레이션을 QA에 직접 활용**: "직관성/밸런스/재미" 같은 주관적 체크포인트도, 게임에
   결정론적 시뮬레이션 레이어가 있다면 실제 수치(승률/피해량/생존율)를 뽑아 실측 근거로 삼을 수 있다.
   추측이 아니라 실행 결과로 밸런스를 검증했다.

2. **코드 리뷰 ≠ 통합 검증**: 5개 병렬 구현 레인이 각자 파일 경계를 완벽히 지켰음에도(diff 리뷰로는
   전부 클린), 실제 브라우저 실행에서만 발견되는 크로스-레인 버그가 있었다 — 신규 토스트 컴포넌트가
   기존의 느슨한 CSS 셀렉터(`.edge-card:not(.defense-result)`)에 우연히 매치되어 렌더 직후 자동
   삭제되는 문제. **코드 diff가 전부 정상으로 보여도, 실제 브라우저에서 클릭/드래그로 검증하기 전까지
   "작동한다"고 주장하지 말 것.**

3. **서비스워커 캐시 함정 (재발)**: 로컬 정적 서버로 신규 코드를 서빙해도, 이전 세션에서 등록된
   서비스워커가 캐시된 구버전 파일을 계속 서빙해 "코드를 고쳤는데 반영이 안 된다"는 착시를 만든다.
   이 프로젝트에서 최소 3회(D19, 이번 세션 카메라 힌트 디버깅 2회) 반복된 함정 — PWA 프로젝트를
   로컬에서 반복 검증할 때는 항상 `navigator.serviceWorker.getRegistrations()` +
   `caches.keys()`로 잔존 캐시를 먼저 제거할 것.

4. **GDD 서술을 맹신하지 말고 실측으로 검증**: 문서가 "포대 스탠스 = 지속 화력 최대"라고 서술했지만
   실측 결과 포대가 3개 스탠스 중 화력이 최저였다(후열 시너지 보너스가 FRONT≥1을 요구해 포대만
   구조적으로 배제됨). 진짜 효과는 "동료 완전 무피해"(0 피격) — 설계 문서 자체를 실측 기반으로
   정정했다.

5. **병렬 서브에이전트 취소 ≠ 작업 실패**: 3개 구현 서브에이전트 중 2개가 타임아웃으로 "cancelled"
   상태였지만, 실제로는 전체 작업과 자체 검증(teeth-test 포함)을 완료한 뒤였다 — transcript를 직접
   읽고 git diff/테스트 실행으로 실제 상태를 확인하는 것이 상태 라벨보다 신뢰할 수 있는 근거다.

## 재사용 가능한 도구

`scripts/audit-glb-angle-readiness.py` — Blender headless로 GLB를 8방위×2고도(16앵글)
렌더링해 실루엣 커버리지 알파 비율로 임의각 뷰잉 결손(백페이스 컬링/미완성 후면)을 자동 감지하는
휴리스틱 감사 도구. 자유 궤도/회전 카메라를 도입하는 프로젝트에서 재사용 가능.

---

## Update (2026-07-25T12:39:58.321Z)

## Cycle Close 업데이트 (구현+검증+회귀 완료)

이 항목은 초기 설계 단계에서 기록됐다 — 이제 구현·검증·회귀까지 전체 사이클이 완결됐다. 전체
사이클 상세는 `_workspace/20260723-solo-warden-rpg-concept/retrospectives/cycle-4-retrospective.md`.

### 최종 결과
- 3개 병렬 구현 레인(카메라/편성시뮬레이션/UI) 완료, 전부 브라우저 실측 검증
- 전체 회귀 182/182 통과(1건 정당한 skip), 0 실패 — 3회 반복 확인
- 커밋: `39f887f`(설계), `e450173`(구현+버그수정), 둘 다 `feat/game-studio-core-loop-ui-redesign` 브랜치에 푸시 완료

### 실제로 발견·수정된 버그 2건 (재사용 가능한 패턴)

**1. GDD 서술이 실측과 정반대였던 사례** — "포대(Turret) 스탠스 = 지속 화력 최대"라는 설계 문서
서술이, 실제 결정론적 시뮬레이션 실측 결과 정반대(3스탠스 중 화력 최저, 대신 동료 피격량 정확히
0)로 드러났다. FRONT 파생값 0이 후열 시너지 보너스(FRONT≥1 요구)를 구조적으로 배제하는 게 원인 —
설계자가 문서 작성 시점에 이 교차-시스템 상호작용을 놓쳤다. **교훈: 게임에 결정론적 시뮬레이션
레이어가 있다면, "설계 문서가 이렇게 말한다"를 믿지 말고 항상 시뮬레이션을 돌려 실측할 것.**

**2. 코드 리뷰로는 안 잡히는 크로스-레인 CSS 셀렉터 충돌** — 3개 병렬 구현 레인이 각자 파일
경계를 정확히 지키고 diff도 전부 클린했지만, 신규 토스트 컴포넌트(`.edge-card.defense-toast`)가
기존의 느슨한 정리 로직 셀렉터(`.edge-card:not(.defense-result)`, "성장 카드가 아닌 다른
edge-card는 지운다"는 오래된 가정)에 우연히 매치돼 렌더 직후(1.5ms만에) 자동 삭제됐다. **교훈:
병렬 레인이 서로 다른 파일을 건드려도, 공유 CSS 클래스 이름공간의 셀렉터 하나가 사전 조율 없이
충돌할 수 있다 — 신규 컴포넌트를 기존 셀렉터가 의도치 않게 매치하는지 항상 실제 브라우저에서
확인할 것.**

### 방법론 확인 (재사용 가치 높음)
- 서비스워커 캐시가 로컬 반복 검증마다 최신 코드를 가려버리는 함정이 이 세션에서만 2회 재발
  — PWA 프로젝트는 로컬 검증 루틴에 `navigator.serviceWorker.getRegistrations()`+`caches.keys()`
  삭제를 습관화할 것.
- "cancelled" 상태의 병렬 서브에이전트가 실제로는 작업+자체검증까지 끝냈을 수 있다 — 상태
  라벨보다 transcript 직접 확인이 우선.

### 미해결 (다음 사이클 이월, Stage 2 리튠 권고)
정식 G2/G3 아키타입 로테이션 밸런스 프로토콜, G6 정식 perf 예산 측정, R2(역할 다양성) 검증
매트릭스 확장 — 전부 이미 확정된 시스템의 수치 튜닝 단계로, 새 컨셉/설계 작업이 아님.

