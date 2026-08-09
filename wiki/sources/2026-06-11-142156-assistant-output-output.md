---
type: "source-summary"
source_type: "assistant-output"
captured_at: "2026-06-11T14:21:56.637065+00:00"
raw_path: "raw/sources/outputs/2026/06/11/142156-a10a9da6-0a3-assistant-output.md"
session_id: "a10a9da6-0a3e-4b1c-84ba-9ecc0e97"
rtk_method: "rtk"
rtk_original_chars: 2118
rtk_compressed_chars: 2118
rtk_saved_pct: 0.0
---

# 현재 세션에 적용된 규칙 스택을 실제 설정 파일과 이 세션에서 관측된 동작 기준으로 정리해드립니다.

- Raw output: [[raw/sources/outputs/2026/06/11/142156-a10a9da6-0a3-assistant-output]]
- Filed report: [[wiki/reports/2026-06-11-142156-assistant-output-answer]]
- rtk compression: rtk (2118→2118 chars, -0.0%)

## Compressed Output (rtk)

```text
현재 세션에 적용된 규칙 스택을 실제 설정 파일과 이 세션에서 관측된 동작 기준으로 정리해드립니다.

## 1. 지침 계층 (CLAUDE.md 캐스케이드 — 모델에게 주어지는 "텍스트 규칙")

세션 시작 시 세 단계의 CLAUDE.md가 컨텍스트에 합쳐져 들어옵니다:

| 파일 | 역할 |
|---|---|
| `~/.claude/CLAUDE.md` (전역) | OMC 멀티에이전트 오케스트레이션 원칙, `@RTK.md` import, 지식 파이프라인 규칙, rtk×semble 역할 분담 |
| `~/CLAUDE.md` | Claude(오케스트레이터)/Gemini(분석)/Codex(실행)/OpenContext(메모리) 역할 분담 문서 |
| `~/.superset/projects/CLAUDE.md` (상위 폴더) | oh-my-agent 규칙 — `.agents/` SSOT 보호(직접 수정 금지), serena 우선 코드 검색, 워크플로 키워드 |

이것들은 **권고 텍스트**입니다 — 제가 판단에 반영하지만 강제 실행되는 건 아닙니다. 충돌 시 더 구체적인 지시와 사용자의 직접 요청이 우선합니다.

## 2. 훅 (settings.json — 하네스가 강제 실행하는 자동화)

`~/.claude/settings.json`에서 확인된 실제 등록 훅:

- **`PreToolUse [Bash]` → `rtk hook claude`**: 제가 실행하는 모든 셸 명령을 rtk로 감싸 출력을 압축합니다. 이번 세션에서 `git push` 결과가 `ok main` 한 줄로, 커밋 결과가 `ok`로 줄어든 게 이 훅의 효과입니다.
- **`UserPromptSubmit` → `llm-wiki-ingest.sh`**: 입력하신 모든 프롬프트가 `~/vaults/llm-wiki`에 원문 보존 + 압축 요약으로 적재됩니다.
- **`Stop` → `knowledge-pipeline.sh`**: 제 턴이 끝날 때마다 출력이 같은 파이프라인으로 적재되고 graphify 그래프가 갱신됩니다.
- **Superset `notify.sh`**: SessionStart/End, PostToolUse, PermissionRequest 등 거의 모든 이벤트에 알림 훅 (SUPERSET_HOME_DIR 있을 때만 동작).

그 외 **OMC 플러그인 훅**이 추가로 동작 중입니다 — 이번 세션에서 보인 것: SessionStart에 프로젝트 메모리 주입(hot paths: `setup-all-skills-prompt.md` 25회 등), Bash 호출마다 "병렬 실행 권장" 리마인더, Edit마다 "수정 후 검증" 리마인더, 도구 실패 시 "원인 분석 후 계속" 리마인더. 이런 건 `<system-reminder>`로 저한테 주입되고, 저는 그에 맞춰 행동합니다 (예: 리팩터링 후 바이트 단위 검증을 돌린 것).

## 3. 스킬 (슬래시 명령 → 워크플로 주입)

`/code-refactoring`을 입력하셨을 때 스킬 본문 전체가 컨텍스트에 주입됐고, 저는 그 절차를 그대로 따랐습니다: 클린업 패킷 식별 → 모드 1개 선택(`local-safe-refactor`) → 행위 가드레일 고정(원본 vs 리팩터링 출력 비교) → 작은 슬라이스 → 검증 보고. 키워드 트리거(`ralph`, `autopilot`, `ulw` 등)는 UserPromptSubmit 훅이 감지해서 해당 스킬을 자동 호출하게 합니다.

## 4. 우선순위와 탈출구

- 강제력 순서: **훅(하네스 실행) > 권한 모드 > 사용자 직접 지시 > CLAUDE.md 텍스트 규칙**. 훅은 제가 무시할 수 없고, CLAUDE.md는 제 판단에 반영되는 수준입니다.
- 끄는 방법: `DISABLE_OMC`, `OMC_SKIP_HOOKS` 환경변수(OMC 훅), settings.json에서 해당 항목 제거(rtk/llm-wiki 훅).

**참고로 발견한 이슈 하나**: 스킬 목록에 `agent-browser 오후 11.16.16`처럼 Finder 복제본 스킬이 수십 개 중복 등록되어 있습니다 — `~/.claude/skills/` 안에 복사본 폴더가 쌓여 있어서인데, 아까 지운 `.omc 오후*`와 같은 패턴입니다. 스킬 자동 매칭 정확도를 떨어뜨릴 수 있으니 원하시면 이것도 정리해드릴게요.
```
