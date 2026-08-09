---
type: "source-summary"
source_type: "assistant-output"
captured_at: "2026-07-25T21:13:09.502607+00:00"
raw_path: "raw/sources/outputs/2026/07/25/211309-78abb2c6-751-pass-8-complete-clean-tree-only-the-node-modules.md"
session_id: "78abb2c6-7516-446b-9668-7e641c3b"
rtk_method: "rtk"
rtk_original_chars: 1479
rtk_compressed_chars: 1479
rtk_saved_pct: 0.0
---

# Pass #8 complete. Clean tree (only the `node_modules` symlink u…

- Raw output: [[raw/sources/outputs/2026/07/25/211309-78abb2c6-751-pass-8-complete-clean-tree-only-the-node-modules]]
- Filed report: [[wiki/reports/2026-07-25-211309-pass-8-complete-clean-tree-only-the-node-modules-answer]]
- rtk compression: rtk (1479→1479 chars, -0.0%)

## Compressed Output (rtk)

```text
Pass #8 complete. Clean tree (only the `node_modules` symlink untracked, as expected), both commits authored correctly on `studio-loop/main`.

## Pass #8 요약 — 축 3 (RPG 성장/캐릭터)

**한 축 = 한 개선.** 지속(passive) 스킬 빌드 배지를 엣지 HUD에 상시 노출.

**발견 (실측)**: `renderControls`의 `#skill-actions`가 `kind === "active"`만 렌더(`app.js:1589`) → 지속 스킬 3종(Dusk Edge/Echo Magnet/Gate Binder)은 습득 후 2초 레벨업 토스트가 끝나면 전투 화면에서 완전히 소멸. **런-스코프 성장 풀의 절반이 "성장이 체감되는가"에서 탈락** — 플레이어가 자기 Dusk Warden의 누적 지속 특성을 런 내내 볼 수단이 없었다.

**구현**: 우상단 액티브 버튼 아래 컬럼 스택에 읽기전용 `#passive-badges` 스트립 추가. 각 배지 = 글리프 + 스킬명 + 성장 프리뷰와 동일한 per-skill boon(`+180 공격`/`+1500 회수`/`+120 내구`). 값은 전부 `SKILLS` 카탈로그 파생(매직넘버 0), 순수 클라 렌더.

**검증 (자기보고 아님)**:
- `node --test` **189 tests / 188 pass / 0 fail / 1 skip** — 회귀 0, 결정론 무접촉(시뮬/`getRunDigest`/카탈로그 미변경, 신규 에셋·네트워크 0)
- 신규 브라우저 테스트 `verifyPassiveBadges`가 결정론 frame-pump로 실제 성장 오퍼를 구동, (a) 엣지 HUD 내부 렌더, (b) boon이 독립 오라클 `PASSIVE_BOONS`와 정확 일치(카탈로그 배선 증명), (c) 액티브 id 부재를 실증
- 스크린샷 `/tmp/passive-badges.png`: "◎ Echo Magnet +1500 회수" 칩이 레벨업 토스트의 회수반경 12000→13500과 일치, 아케인 바이올렛 색으로 액티브/목표 칩과 구별, 중앙 전장 미가림

**커밋**: `9e44245`(feat) + `6870537`(docs/D29). 위키 누적 로그 append 완료.

**이월 미해결** (다음 RPG 패스 입력, 은폐 없음):
1. 성장 오퍼 카드에 **현재 보유 빌드 미표시** → 1/3 선택이 시너지-블라인드 (다음 1순위 후보)
2. `skillRanks[id]`가 항상 1로 하드코딩(`defense-run-simulation.js:644`) → 랭크업 없음, 빌드 투자 깊이 부재 (도입은 시뮬/결정론 변경이라 스파이크 선행 필수, 범위 밖)
3. `verifyBossMeshRegression`는 사전존재 실패(`.glb` D26 제거, 타 세션 소관) — 미접촉, 신규 테스트를 그 앞에 배치해 격리된 양성 증거 확보
```
