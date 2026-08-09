---
title: "이 문서는 두 개의 서로 다른 "흐름"을 담고 있습니다 — ① 설치 스크립트 자체가 실행되는 순서, ② 설치가 끝…"
created_at: "2026-07-21T12:37:12.575804+00:00"
section: "reports"
status: "captured"
session_id: "5fb36489-39c2-4719-9273-d07968e6"
raw_output: "[[raw/sources/outputs/2026/07/21/123712-5fb36489-39c-assistant-output]]"
source_summary: "[[wiki/sources/2026-07-21-123712-assistant-output-output]]"
---

# 이 문서는 두 개의 서로 다른 "흐름"을 담고 있습니다 — ① 설치 스크립트 자체가 실행되는 순서, ② 설치가 끝…

## Answer Output (rtk-compressed)

이 문서는 두 개의 서로 다른 "흐름"을 담고 있습니다 — ① 설치 스크립트 자체가 실행되는 순서, ② 설치가 끝난 뒤 에이전트가 매 턴마다 따르는 운영 흐름. 나눠서 설명하겠습니다.

## ① 설치 스크립트 실행 흐름 (Step 0 → 6)

```
Step 0  환경 감지
        └─ OS 판별 → 어떤 에이전트(claude/codex/gemini/opencode/jeopi/gjc/jeo/pi)가 깔려있는지 체크
        └─ 기존 skills 목록 스냅샷 (삭제 방지용 기준점)
        └─ skills CLI(vercel) 없으면 npm install -g skills

Step 1  152개 skill 전체 설치
        └─ skills add -g <repo> --skill '*' -a '*' --copy --full-depth
        └─ ~/.agents/skills/ 에 전부 복사, 감지된 모든 에이전트에 링크

Step 2  플랫폼 전용 skill 재배치(dedup)
        └─ omc → claude-code 전용, ohmg → antigravity 전용, omx → codex+claude-code
        └─ Step1에서 '*'로 잘못 걸린 심볼릭 링크를 지우고 올바른 대상에만 재연결

Step 3  핵심 툴 설치 (3a~3j, 순차)
        └─ 3a rtk (brew/curl) → 3b graphify (uv venv) → 3c ooo/Ouroboros
           (pip install + claude mcp add ooo + codex config.toml에 [mcp_servers.ooo] 추가)
        └─ 3d obsidian(brew cask) → 3e llm-wiki 볼트 부트스트랩(~/vaults/llm-wiki)
        └─ 3f semble(uv tool + claude mcp add semble)
        └─ 3g oh-my-claudecode/oh-my-codex/oh-my-agent 플러그인 설치
        └─ 3h/3i/3j gjc·jeo·pi 쪽 rule/hook 배선

Step 4  검증 — skill 개수, 각 도구 설치 여부 재확인 (읽기 전용)

Step 5  GitHub star 요청 — 동의 먼저 구하고 행동 (요청만, 강제 없음)

Step 6  "기본 운영 규칙" 주입
        └─ $ooo→$graphify→$rtk→$obsidian→$llm-wiki 순서를 CLAUDE.md/AGENTS.md에 박아넣음
        └─ 동시에 Knowledge Pipeline 훅(아래 ②)을 각 에이전트 settings에 등록
```

각 스텝은 **멱등적**으로 짜여 있어서(이미 있으면 스킵/업데이트, 삭제 명령 없음) 재실행해도 안전하도록 설계돼 있습니다.

## ② 설치 후 매 턴 실행되는 "지식 파이프라인" 흐름

Step 6이 심어놓는 것이 지금 당신 환경의 `CLAUDE.md`에 이미 있는 바로 그 블록입니다:

```
사용자 프롬프트 입력
   │
   ▼  (UserPromptSubmit 훅, ingest-prompt.py)
rtk 압축 → raw/sources/prompts/ 에 원문 보존 → graphify 구조화
   │
   ▼  (에이전트가 실제로 응답 생성)
   │
   ▼  (Stop 훅, turn-end)
rtk 압축 → raw/sources/outputs/ 에 원문 보존 → graphify 재클러스터링
   │
   ▼
~/vaults/llm-wiki (Obsidian 볼트) 의 wiki/index.md, log.md 갱신
```

- **입력 시점**: `UserPromptSubmit` (Claude Code) / Codex는 `hooks.json`의 `UserPromptSubmit` / Gemini·Antigravity는 `BeforeAgent` — 프롬프트 원문을 캡처.
- **응답 종료 시점**: `Stop`(Claude/Codex) / `AfterAgent`(Gemini) — 프롬프트 캡처 없이 그래프만 재빌드.
- 즉 **턴마다 2번** (프롬프트 들어올 때, 응답 끝날 때) 지식 그래프와 위키가 갱신되도록 설계된 구조입니다.

그리고 실제 작업 중에는 이 5개 스킬이 상황별로 순서대로 관여하도록 규칙화되어 있습니다:
1. `$ooo` — 모호하거나 다단계인 요청이면 먼저 spec 확정
2. `$graphify` — 구조/관계를 영속시켜야 하면 그래프 갱신
3. `$rtk` — 모든 쉘 명령은 기본적으로 압축 래퍼 경유
4. `$obsidian` — 실제 데스크톱 Obsidian 볼트에 기록/핸드오프 필요할 때만
5. `$llm-wiki` — 재사용 가능한 결론을 `wiki/`에 파일링, `index.md`/`log.md` 최신화

요약하면, 이 문서는 "한 번 실행하는 설치 스크립트"이자 동시에 "설치 후 영구적으로 지켜야 할 운영 규칙"을 CLAUDE.md에 심어놓는 이중 구조입니다. 지금 이 세션의 CLAUDE.md에 이미 그 운영 규칙(②)이 적용돼 있는 걸 보면, Step 6까지 과거에 이미 한 번 실행됐던 것으로 보입니다.

다음으로 뭘 하시겠어요? (예: Step 4 검증만 실제로 돌려서 뭐가 빠졌는지 확인 / 특정 스텝만 재실행 / 여기서 종료)

## Evidence and Citations

- [[wiki/sources/2026-07-21-123712-assistant-output-output]]
- [[raw/sources/outputs/2026/07/21/123712-5fb36489-39c-assistant-output]]
