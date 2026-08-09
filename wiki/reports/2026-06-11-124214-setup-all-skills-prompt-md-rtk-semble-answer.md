---
title: "완료했습니다. `setup-all-skills-prompt.md`에 rtk×semble 호환 설치 단계를 추가하고…"
created_at: "2026-06-11T12:42:14.216879+00:00"
section: "reports"
status: "captured"
session_id: "b133d257-c741-478d-b124-4905d8bb"
raw_output: "[[raw/sources/outputs/2026/06/11/124214-b133d257-c74-setup-all-skills-prompt-md-rtk-semble]]"
source_summary: "[[wiki/sources/2026-06-11-124214-setup-all-skills-prompt-md-rtk-semble-output]]"
---

# 완료했습니다. `setup-all-skills-prompt.md`에 rtk×semble 호환 설치 단계를 추가하고…

## Answer Output (rtk-compressed)

완료했습니다. `setup-all-skills-prompt.md`에 rtk×semble 호환 설치 단계를 추가하고(커밋 `3cd9497` 푸시 완료), 요청하신 대로 **rtk가 적용된 모든 훅·규칙 지점에서 semble이 호환 동작하는지 실제 검증**까지 마쳤습니다.

## 문서 변경 (setup-all-skills-prompt.md)

- **Step 3f 확장** — MCP만 등록하던 것에 더해 ① `uv tool install semble`로 **CLI도 설치**(셸에서 rtk 래핑 명령과 나란히 동작), ② **Gemini CLI에도 semble MCP 등록**(jq 멱등 병합 — Claude·Codex와 패리티)
- **Step 3f-2 신설 (RTK × semble 호환 배선)** — 두 도구 PATH 공존 확인 → 마커 가드(`RTK-SEMBLE:START/END`)로 역할 분담 규칙을 설치된 에이전트별 지침 파일에 멱등 주입 → rtk 훅 아래서 semble 정상 실행 스모크 체크. 규칙 요지: **탐색은 semble 1차 패스(무엇을 읽을지), 그 외 모든 셸 출력은 rtk 자동 압축(어떻게 읽을지)**

## 훅·규칙 검증 결과 (이 머신에서 실증)

| rtk 적용 지점 | semble 호환 확인 |
|---|---|
| **Claude 훅** (`settings.json`의 `rtk hook claude`) | `git status` → `rtk git status` 재작성 확인, `semble search …`는 **무수정 통과** (훅 출력 없음 = 개입 안 함) ✅ |
| **Gemini 훅** (`rtk-hook-gemini.sh` = `exec rtk hook gemini`) | semble 명령에 `{"decision":"allow"}` — 변경 없이 허용 ✅ |
| **규칙 파일** (`~/.claude/CLAUDE.md`, `~/.codex/AGENTS.md`, `~/.gemini/GEMINI.md`) | 세 파일 모두에 역할 분담 규칙 주입 완료, 2회 실행으로 중복 없음(멱등) 확인 ✅ |
| **MCP 등록** | Claude ✅(기존) · Codex ✅(기존) · Gemini ✅(이번에 추가, JSON 유효성 검증) |
| **CLI 공존** | rtk 0.37.1(`/opt/homebrew/bin`) + semble(`~/.local/bin`, uv tool 격리 설치) — `semble --help` 정상 ✅ |

결론: rtk 훅의 재작성 로직은 알려진 명령 집합(git/grep/cat/test/lint…)에만 작동하고 `semble`은 건드리지 않으므로 별도 제외 설정이 필요 없으며, 이제 세 에이전트(Claude·Codex·Gemini) 모두 "탐색=semble → 검증·기타=rtk" 규칙과 MCP/CLI 양쪽 표면이 갖춰진 상태입니다.

## Evidence and Citations

- [[wiki/sources/2026-06-11-124214-setup-all-skills-prompt-md-rtk-semble-output]]
- [[raw/sources/outputs/2026/06/11/124214-b133d257-c74-setup-all-skills-prompt-md-rtk-semble]]
