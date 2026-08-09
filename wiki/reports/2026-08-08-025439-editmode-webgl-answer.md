---
title: "통합 파이프라인(EditMode 전량 → WebGL 빌드 체인)이 백그라운드로 실행 중입니다."
created_at: "2026-08-08T02:54:39.335215+00:00"
section: "reports"
status: "captured"
session_id: "f40d2c84-4012-4f97-bf8c-1ec02651"
raw_output: "[[raw/sources/outputs/2026/08/08/025439-f40d2c84-401-editmode-webgl]]"
source_summary: "[[wiki/sources/2026-08-08-025439-editmode-webgl-output]]"
---

# 통합 파이프라인(EditMode 전량 → WebGL 빌드 체인)이 백그라운드로 실행 중입니다.

## Answer Output (rtk-compressed)

통합 파이프라인(EditMode 전량 → WebGL 빌드 체인)이 백그라운드로 실행 중입니다.

**이번 사이클 요약 (진행 중)**

| 항목 | 상태 |
|---|---|
| 획득 토스트 팝업 (`LootToastQueue` 순수 C# + HUD 렌더, 등급 차별화) | ✅ 구현 완료 |
| 획득음 배선 — 픽업 리스트 per-tick diff 방식 | ✅ (`LastLootGrade`가 "마지막 드롭"이지 "수거 아이템"이 아님을 잡아내 정확한 방식으로 교체 — Epic 드롭 직후 Basic을 주워도 골드로 칠해지던 함정 회피) |
| 기존 오디오 버그 수정 | ✅ 장비 파편이 cue-pickup을 **2회 재생**하던 선재 결함 제거 |
| 지도 정리 — 상태칩 "강하 가능"/"잠김" 제거(3중·4중 중복), "정화 완료"만 유지 | ✅ 정보 손실 감사 포함 |
| 지도 개선 2건 — 잠김 라벨 대비 1.56:1→6.5:1, reduced-motion 프론티어 정적 링 | ✅ |
| 효과음 3종 (asset-lane3) | ✅ |
| 폰트 재생성 | 불필요 확인 (신규 한글 0자, fontTools 검증) |
| EditMode + 빌드 | 실행 중 |

**사람 판단 대기 3건** (통합 후 함께 정리해 드림): ① "우측 이정표"가 상태칩이 아니라 SORTIE 패널 전체를 의미했다면 지도-경로선택기 통합 재설계가 필요(이번 범위 밖), ② 미점등 링크 대비 1.34:1 — 리빌 문법 유지 vs 접근성 바닥값, ③ 시련 카드의 "잠김" 유지(지도 노드가 없어 중복 아님).

검증 통과 시 커밋 → push → 배포 → 실측까지 마치고 최종 보고드리겠습니다.

## Evidence and Citations

- [[wiki/sources/2026-08-08-025439-editmode-webgl-output]]
- [[raw/sources/outputs/2026/08/08/025439-f40d2c84-401-editmode-webgl]]
