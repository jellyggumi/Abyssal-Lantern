---
type: "assistant-output"
session_id: "52dd9a23-75d2-4469-ba27-aeb83df8"
captured_at: "2026-06-11T13:05:50.142367+00:00"
report: "[[wiki/reports/2026-06-11-130550-main-origin-main-answer]]"
---

# 검토 → 개선 → 푸시까지 완료했습니다. 작업 트리는 클린이고 `main`이 `origin/main`과 동기화된…

## Output

```text
검토 → 개선 → 푸시까지 완료했습니다. 작업 트리는 클린이고 `main`이 `origin/main`과 동기화된 상태입니다.

## 표준 검토에서 나온 문제와 수정

`skill-standardization` 검증기 2종을 돌려 찾은 문제입니다.

**spec-stack 자체 (validate_skill.sh: 1 error, 1 warning → 0/0)**
- ❌ description 1053자(최대 1024) → 트리거를 유지한 채 920자로 압축
- ⚠️ `## Examples` 섹션 누락 → 3개 예시 추가(패턴 A 전체 커맨드 시퀀스, 모호한 조합 요청 라우팅, route-out해야 하는 near-miss)
- 표준 Step 5에 따라 `evals/evals.json` 3건 신설: should-trigger(GIMP 자동화 풀스택), SSOT 분리 질문, route-out near-miss("하니스만 만들어줘" → cli-anything)

**카탈로그 드리프트 (validate_catalog_sync.py: 에러 다수 → "No drift detected")**
- 실제 스킬 폴더는 **137개**인데 문서는 135로 어긋나 있었습니다. 원인 추적 결과 6/1 `agenticskills` 추가 때 카운트가 안 올라간 이후 계속 1 적었던 것 — README(en/ko) 배지·헤더·본문·트리 스니펫, setup 프롬프트의 Install ALL/인벤토리 카운트를 전부 137로 정정하고, What's New에 정정 사유를 명시했습니다.
- `skills.json` 매니페스트가 132개로 멈춰 있어 5개 스킬(agenticskills, opik, spec-kit, cli-anything, spec-stack)을 frontmatter에서 파싱해 백필하고, `.agent-skills/` 접두사가 잘못 붙어 검증 실패하던 29개 엔트리의 path를 정규화했습니다.
- `skills.toon`에도 같은 5개를 TOON 포맷으로 추가하고 Count 헤더를 실제 값(94)으로 동기화했습니다.

## 푸시된 커밋 (origin/main)

'''
413be1e chore(catalog): backfill skills.json/skills.toon and normalize manifest paths
e5631e2 feat(spec-stack): add spec-kit × ooo × cli-anything composition skill (plugin-installable)
'''

이제 GitHub에 올라갔으므로 `npx skills add https://github.com/akillness/oh-my-skills --skill spec-stack` 설치가 실제로 동작합니다.

참고: 이전에 말씀드린 `fix_frontmatter.py`/`flatten_skills.py` 변경분은 다른 세션이 `f1eafa5 refactor(scripts)`로 이미 커밋해서 제 커밋과 섞이지 않았습니다. `skills.toon`은 5개 백필 후에도 94/137로 여전히 부분 카탈로그인데(이번 작업 전부터 그랬던 별도 부채), 전체 재생성이 필요하면 별도 패스로 처리하는 걸 권합니다.
```
