---
title: gjc vs joc 아키텍처 비교와 개선 매핑
type: concept
date: 2026-06-10
tags: [coding-agent, gjc, jeo-code, joc, architecture, comparison]
---

# gjc vs joc 아키텍처 비교와 개선 매핑

> joc(`jeo-code`, ~/.superset/projects/jeo-code)는 gjc 스펙-퍼스트 계약을 Bun 단일 패키지로 재구현한 프로젝트.
> 이 페이지는 2026-06-10 기준 양쪽 코드 근거로 작성한 개선용 비교표.

## 구조 비교

| 영역 | gjc (모노레포) | joc (단일 패키지) | 격차/개선 포인트 |
|------|----------------|-------------------|------------------|
| 스킬 | 소스 번들 SKILL.md (`defaults/gjc/skills/`) + `.gjc` 오버라이드 디스커버리 | `src/skills/catalog.ts` 정적 TS 상수 — **파일 기반 스킬 없음** | SKILL.md 파일 로딩/오버라이드 체계 도입 |
| 훅 | 라이프사이클 이벤트 (agent-loop 이벤트, output minimizer 등) | **훅 시스템 부재** | 이벤트/훅 표면 정의 필요 |
| 룰 | sdk.ts에서 rules discovery → system prompt 조립 | 프로젝트 컨텍스트 파일 주입만 (`context-files.ts`) | 룰 파일(AGENTS.md 계열) 계층 로딩 정합 |
| Provider/OAuth | `packages/ai` 격리: auth broker/gateway/storage, 모델소스 4계층 병합 | `src/ai/*` + `src/auth/*`: OAuth PKCE + 토큰 자동갱신, 카탈로그 단일 소스 | retry 분리(`request`/`stream`)는 패리티 확보됨. broker식 자격증명 일원화는 미흡 |
| 에이전트 루프 | append-only context + compaction 1급 API, 스트림 이벤트 기반 | `runAgentLoop` strict-JSON 1툴/스텝, 가드(no-progress/연속실패/step cap) | joc는 JSON 루프 (네이티브 tool-calling 미사용) |
| 컨텍스트 압축 | `packages/agent` compaction (토큰 기반, 루프 통합) | `compaction.ts` — **char 기반** (120k chars), 요약 1회 누적 방식 | 토큰 기반 예산 + 세션 메모리 상한 검증 필요 |
| TUI | 컴포넌트 프레임워크 + diff state + Kitty/appearance | `LaunchTui` + 차등 ANSI `Renderer` 자체 구현 | 메모리 누적(스트림 버퍼/히스토리) 검증 필요 |
| 모드 | interactive/print/RPC/RPC-UI/ACP 5종 | TTY TUI / plain stream 2종 + MCP 서버 | RPC류는 비목표일 수 있음 |
| 상태 | `.gjc/` 단일 | `~/.joc/config.json` + `<cwd>/.joc/` | 구조는 패리티 |
| 검증 게이트 | 정의 가시성 게이트 스크립트 + 패키지별 체크 | `bun run typecheck` + `bun test` (705 pass) | 스킬/룰 도입 시 정의 게이트도 같이 도입 |

## joc 개선 의제 (2026-06-10 사용자 요청 기준)

1. **skill/hook/rule 파일 체계**: catalog.ts 하드코딩 → gjc식 소스 번들 + 파일 오버라이드
2. **OAuth/model 동작 무오류**: provider 라우팅(휴리스틱 regex) vs gjc catalog-authoritative — 토큰 갱신 경쟁/만료 경로 점검
3. **TUI 메모리 관리**: 컨텍스트 누적 시 메모리 비대 — compaction char 휴리스틱, 세션 JSON 누적, renderer 버퍼 점검
4. **흐름/UX**: gjc의 `deep-interview → ralplan → ultragoal(+team)` 계약과 TUI 테마/appearance 패리티

## 근거 문서
- [[wiki/sources/2026-06-10-gajae-code-repo]] — gjc 저장소 분석
- jeo-code/AGENTS.md, jeo-code/docs/improvements.md (로컬)
