---
type: "assistant-output"
session_id: "460f3764-ec99-4b09-9fed-14575589"
captured_at: "2026-07-26T04:06:35.302712+00:00"
report: "[[wiki/reports/2026-07-26-040635-pass-14-complete-clean-tree-only-node-modules-sy-answer]]"
---

# Pass #14 complete. Clean tree (only `node_modules` symlink untr…

## Output

```text
Pass #14 complete. Clean tree (only `node_modules` symlink untracked), two independently-mergeable commits, green suite.

## 패스 #14 요약

**초점 이탈 (근거 있는 결정):** 순환상 #14 → mod-5=4 (스테이지 분위기)였으나, 재정향 중 **로컬 테스트 스위트가 레드**임을 발견해 축 작업 대신 이를 우선했다. 레드 스위트는 D33의 post-pass auto-push 게이트를 차단하므로 축보다 시급했다 (강제규칙 #3/#7). 직전 4패스의 "0 commit"은 코드 문제가 아니라 **월 지출 한도 소진(쿼터)**이 원인임도 로그로 실측 확인했다.

**근본 원인 (실측):** `tests/defense-stat-delta-browser.test.mjs`만 ESM 최상위에서 `import { chromium } from "playwright"`를 했다. playwright는 devDependency(CI엔 설치, 심링크된 로컬 `node_modules`엔 부재)라 `ERR_MODULE_NOT_FOUND`로 파일 전체가 import 시점 hard-fail → **로컬만 레드, CI는 green**. pass #10 회고의 "0 fail"은 당시 로컬에 playwright가 있었기 때문이며, 그 사이 사라졌다.

**수정:** 기존 `.cjs` 브라우저 테스트의 `--allow-missing-browser` 계약을 ESM으로 이식 — top-level import를 본문 안 **가드된 dynamic import + `t.skip()`**로 교체. CI는 검증을 그대로 전부 수행, 로컬/루프는 우아하게 skip.

**측정:**
- 격리 실행: 1 skip / 0 fail
- 전체 `node --test 'tests/**/*.test.mjs'`: **191 tests / 189 pass / 0 fail / 2 skip** (직전 189 pass / **1 fail** / 1 skip)
- 회귀 0, 시뮬/렌더/데이터 0줄 변경

**커밋:** `4d17ab2` (fix) · `1e90664` (docs: D34 + 회고). 위키 `abyssal-surge-hourly-studio-log.md` append 완료.

**미해결 (다음 패스 입력):**
1. **축 4(스테이지 분위기)는 손대지 못함** — 다음 mod-5=4 패스가 그대로 이어받아야 한다.
2. 로컬 playwright 부재 지속 시 브라우저 테스트 2건은 로컬에서 skip으로만 통과 (전수 검증은 CI 또는 로컬 `npm install` 후에만). 로컬 node_modules는 메인 저장소 심링크라 이 루프가 재설치하지 않는다.
3. 쿼터 종속이 루프 산출의 운영 리스크로 남음.

**세운 규약(D34):** 신규 `.mjs` 브라우저 테스트는 playwright를 top-level import하지 말고 가드된 dynamic import + `t.skip()` 패턴을 쓴다.
```
