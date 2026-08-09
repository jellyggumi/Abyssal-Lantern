---
title: "완료했습니다. `cli-anything` 스킬을 추가하고 커밋 `3c2ea02`로 origin/main에 푸시했습…"
created_at: "2026-06-11T05:58:57.627236+00:00"
section: "reports"
status: "captured"
session_id: "b133d257-c741-478d-b124-4905d8bb"
raw_output: "[[raw/sources/outputs/2026/06/11/055857-b133d257-c74-cli-anything-3c2ea02-origin-main]]"
source_summary: "[[wiki/sources/2026-06-11-055857-cli-anything-3c2ea02-origin-main-output]]"
---

# 완료했습니다. `cli-anything` 스킬을 추가하고 커밋 `3c2ea02`로 origin/main에 푸시했습…

## Answer Output (rtk-compressed)

완료했습니다. `cli-anything` 스킬을 추가하고 커밋 `3c2ea02`로 origin/main에 푸시했습니다 (main ↔ origin/main 동기화 확인).

## 작업 내용

**1. 소스 분석** — scrapling으로 [HKUDS/CLI-Anything](https://github.com/HKUDS/CLI-Anything) README 전체(61KB, 1,079줄)를 100% 읽고 파악했습니다. 핵심: GUI 자동화 대신 실제 소프트웨어(Blender, GIMP, LibreOffice, OBS, Godot, QGIS 등)를 Click 기반 agent-native CLI로 감싸는 생태계로, 40+ harness · 2,461 테스트 100% 통과 · Apache-2.0 · arXiv 기술 보고서 보유.

**2. 새 스킬 `.agent-skills/cli-anything/`** — spec-kit/opik 전례와 동일한 플러그인 설치형 구조:
- `SKILL.md` — 4가지 모드를 라우팅하는 스킬: ① CLI-Hub 패키지 매니저로 기성 harness 설치(`cli-hub list/search/info/install/launch`), ② 에이전트 자율 탐색 meta-skill 부여, ③ 임의 코드베이스·GitHub 저장소에서 7-phase 파이프라인으로 새 harness 생성(`/cli-anything`), ④ `:refine`/`:test`/`:validate` 반복 개선. Route-out: `harness`(에이전트 팀 설계), `browser-harness`(코드베이스 없는 GUI 대상), `ccpi-marketplace`, `scrapling`
- `references/commands.md` — cli-hub 커맨드, 9개 플랫폼(Claude Code/Codex/OpenCode/OpenClaw/Pi/Hermes/Qodercli/Copilot CLI/Goose) 설치 매트릭스, harness 카탈로그, HARNESS.md 설계 원칙, 한계점
- `scripts/install.sh` — 어제 opik에서 발견한 PEP 668 교훈을 처음부터 적용: venv 활성 시 `uv pip`/`pip`, venv 없으면 `uv tool install`(시스템 Python 보호). `CLI_ANYTHING_META_SKILL=1`로 meta-skill 옵션 설치. `bash -n` 문법 검증 및 frontmatter YAML 파싱 통과 확인

**3. 문서 3종 갱신** — 총계 134 → **135**:
- `README.md` / `README.ko.md` — 배지·총계, v2026-06-11 What's New 섹션, Agent Development 카탈로그 행 추가(영문 5→6, 한글 2→3)
- `setup-all-skills-prompt.md` — 총계 135 반영, Step 4 검증 루프에 `cli-anything` 추가, Skill Inventory의 Agent Development 행과 Core Skill Keyword Reference에 항목 추가

설치 명령: `npx skills add https://github.com/akillness/oh-my-skills --skill cli-anything` — opik 때처럼 설치 후 동작 확인까지 원하시면 말씀해주세요.

## Evidence and Citations

- [[wiki/sources/2026-06-11-055857-cli-anything-3c2ea02-origin-main-output]]
- [[raw/sources/outputs/2026/06/11/055857-b133d257-c74-cli-anything-3c2ea02-origin-main]]
