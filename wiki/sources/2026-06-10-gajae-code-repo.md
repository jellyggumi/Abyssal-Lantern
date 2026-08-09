---
title: gajae-code (gjc) 저장소 분석
type: source
url: https://github.com/Yeachan-Heo/gajae-code
date: 2026-06-10
tags: [coding-agent, gjc, tui, oauth, agent-loop]
---

# gajae-code (gjc) 저장소 분석

> Source: https://github.com/Yeachan-Heo/gajae-code (TypeScript+Rust 모노레포, MIT, ★454)
> 분석일: 2026-06-10. 근거: 저장소 README + `docs/codebase-overview.md`.

## 제품 형태

`gjc`는 외부(external) 코딩 에이전트 하네스. 다른 에이전트 런타임에 플러그인되지 않고, 선택한 repo/worktree에서 독립 실행된다. 공개 워크플로우 표면은 **의도적으로 4개 스킬 + 4개 롤 에이전트로 고정**되어 있다.

```
deep-interview → ralplan → ultragoal
                        └─ (선택) team: tmux 병렬 워커
```

## 핵심 프로세스별 내용

### 1. CLI 부트스트랩 → 세션 생성
- `packages/coding-agent/src/cli.ts`: 명령 등록 (`setup`, `deep-interview`, `ralplan`, `ultragoal`, `team`, 기본 launch)
- `src/main.ts`: CLI 옵션 → 세션 생성, 모드 디스패치 (interactive TUI / print / RPC / RPC-UI / ACP — 5개 모드)
- `src/sdk.ts`: settings + model registry + auth + workspace/context discovery + **skills + rules** + tools + system prompt + agent-core 조립

### 2. 스킬/룰 체계 (joc 개선의 핵심 참조점)
- 기본 워크플로우 스킬은 **소스 번들**: `packages/coding-agent/src/defaults/gjc/skills/<name>/SKILL.md` → `gjc-defaults.ts`가 임베드/설치
- 프로젝트/유저 `.gjc` 오버라이드 디스커버리 지원하되, `.gjc` 디렉토리가 없어도 기본 표면이 사라지지 않음
- 롤 에이전트 프롬프트: `src/prompts/agents/<role>.md` (executor / architect / planner / critic)
- 정의 변경 게이트: `check-visible-definitions.ts`, `verify-g002-gates.ts`, `default-gjc-definitions.test.ts`

### 3. LLM provider / OAuth (`packages/ai/`)
- 모델 registry/resolution, provider 구현, **auth broker / gateway / storage**, 스트리밍, usage, retry/overflow 유틸, OAuth, discovery, validation을 한 패키지로 격리
- `stream.ts`: 모델 기반으로 provider/API 구현에 디스패치, 스트림 이벤트 정규화
- `model-manager.ts`: static + cached + dynamic + remote 모델 소스 병합
- retry 정책: `requestMaxRetries`(스트림 수립 전) / `streamMaxRetries`(replay-safe 일시 오류만) 분리. invalid auth·unsupported model·malformed·context overflow·abort·permanent quota는 **fail-fast**

### 4. 에이전트 루프 (`packages/agent/`)
- `agent-loop.ts`: 컨텍스트 변환 → 모델 스트림 → 툴 실행 → 툴 결과 append → 라이프사이클 이벤트
- `agent.ts`: 루프 래핑 + mutable state, 구독, prompt/continue/abort, 큐, provider 세션 상태, 텔레메트리
- **append-only context + compaction**을 패키지 1급 API로 노출 (joc의 char 기반 maybeCompact와 대비됨)

### 5. TUI (`packages/tui/`)
- 컴포넌트 프레임워크: text, input, editor, markdown, loaders, select/settings list, image, box, container
- `tui.ts`: 컴포넌트 렌더링, focus, overlay, 터미널 치수, **diff state**, synchronized output
- `terminal.ts`: 터미널 라이프사이클, Kitty 프로토콜, appearance 감지
- 테마: 다크=red-claw, 라이트=blue-crab (appearance 자동 감지, 유저 설정 우선)

### 6. 네이티브 계층 (`packages/natives/` + Rust crates)
- N-API: AST 검색/편집, fs scan/cache, grep/glob, syntax highlight, 토큰 카운팅, 텍스트 측정/래핑, PTY/shell, 코드 요약
- `pi-shell`: brush 기반 영속/원샷 셸, 스트리밍, 취소, output minimizer 텔레메트리
- joc는 zero-native 정책이므로 이 계층은 참조만 (포팅 대상 아님)

### 7. 관측/원격
- `packages/stats/`: 세션·모델 사용량 SQLite 집계 + 로컬 대시보드
- `python/gjc-rpc`: `gjc --mode rpc` typed 클라이언트, `python/robogjc`: GitHub triage 봇

## 운영 계약
- 런타임 상태는 전부 `.gjc/` (specs, plans, goals, team state, overrides)
- 설정: `~/.gjc/config.yml` (retry 예산 등)
- 실행 진입: `gjc`, `gjc --tmux`, `gjc --tmux --worktree <path>`

## 관련 문서
- [[wiki/concepts/gjc-vs-joc-architecture]] — joc(jeo-code) 개선을 위한 구조 비교
