---
type: "source-summary"
source_type: "assistant-output"
captured_at: "2026-06-11T12:41:20.070056+00:00"
raw_path: "raw/sources/outputs/2026/06/11/124120-52dd9a23-75d-cli-anything.md"
session_id: "52dd9a23-75d2-4469-ba27-aeb83df8"
rtk_method: "rtk"
rtk_original_chars: 3168
rtk_compressed_chars: 3168
rtk_saved_pct: 0.0
---

# CLI-Anything 설명 & 현재 환경 비교

- Raw output: [[raw/sources/outputs/2026/06/11/124120-52dd9a23-75d-cli-anything]]
- Filed report: [[wiki/reports/2026-06-11-124120-cli-anything-answer]]
- rtk compression: rtk (3168→3168 chars, -0.0%)

## Compressed Output (rtk)

```text
# CLI-Anything 설명 & 현재 환경 비교

## CLI-Anything이 뭔가요

HKUDS의 [CLI-Anything](https://github.com/HKUDS/CLI-Anything)은 **"어떤 소프트웨어든 에이전트-네이티브로"** 만드는 프로젝트입니다. 핵심 아이디어는 에이전트가 GIMP, Blender, LibreOffice, OBS, Godot 같은 실제 애플리케이션을 스크린샷 기반 GUI 자동화(RPA)로 더듬는 대신, **구조화된 CLI로 직접 제어**하게 하는 것입니다.

구성 요소는 4가지 모드로 나뉩니다:

| 모드 | 무엇 | 진입점 |
|------|------|--------|
| 에코시스템 사용 | 이미 만들어진 40+개 하니스 설치 (GIMP, Blender, ComfyUI, Ollama, QGIS…) | `pip install cli-anything-hub` → `cli-hub search/install/launch` |
| 에이전트 자율화 | 에이전트가 스스로 CLI를 찾고 설치하는 메타스킬 | `npx skills add HKUDS/CLI-Anything --skill cli-hub-meta-skill` |
| 하니스 생성 | 코드베이스/GitHub 레포에서 새 CLI 하니스를 7단계 파이프라인으로 생성 | `/cli-anything <path-or-repo>` |
| 하니스 반복 개선 | 커버리지 갭 분석, 테스트, 방법론 검증 | `/cli-anything:refine` · `:test` · `:validate` |

생성되는 하니스의 특징: Click 기반 서브커맨드 + 상태 유지 REPL 이중 모드, 모든 명령에 `--json` 출력, undo/redo, 자동 생성된 `SKILL.md`. 전체 2,461개 테스트가 실제 백엔드(진짜 GIMP, 진짜 Blender)를 호출해 검증합니다. 원칙은 **"레지스트리 먼저 검색, 생성은 폴백"** — `cli-hub search`는 몇 초, 7단계 빌드는 한 세션을 소모합니다.

## 현재 프로젝트/환경에서 비슷하거나 호환되는 것

먼저 확인한 사실: 이 레포(oh-my-skills)의 `skills/` 폴더에는 현재 **cli-anything 하나만** 들어 있고(847edd8 커밋에서 프로젝트 설치 잠금), 나머지 비교 대상은 글로벌 `~/.claude/skills/`와 이 세션의 MCP 서버들입니다.

**이미 호환·연동되어 있는 것 (가장 중요)**

- **`mcp__cli-anything__hub_*` MCP 도구** — 이 세션에 이미 CLI-Hub가 MCP 서버로 연결되어 있습니다 (`hub_list`, `hub_search`, `hub_info`, `hub_install`, `hub_launch`, `hub_update`, `hub_uninstall`). 즉 `pip install` 없이도 제가 지금 바로 레지스트리를 검색/설치할 수 있는 상태입니다.
- **`cli-hub-meta-skill`** — 에이전트 자율 발견용 메타스킬도 이미 스킬 목록에 설치되어 있습니다.
- **`rtk`** (보완재) — CLI 출력 토큰 압축 프록시. cli-anything 하니스의 출력도 rtk 훅을 그대로 통과하므로 철학(토큰 효율적 CLI)이 정확히 맞물립니다. `--json` 출력 + rtk 압축 조합이 자연스럽습니다.

**같은 목표(에이전트가 실제 소프트웨어 제어), 다른 접근**

- **`unity-mcp`** — Unity 에디터를 MCP로 제어. cli-anything의 "MCP 버전" 격인 접근입니다. 차이: MCP는 세션 연결형 프로토콜, cli-anything은 어디서나 호출 가능한 독립 CLI + 파일 기반 상태. Unity는 CLI-Hub 레지스트리에도 게임엔진 계열(Godot)이 있어 영역이 겹칩니다.
- **`obsidian-cli`** — Obsidian을 agent-native CLI로 다루는 동일 철학의 단일 사례. 사용자님의 llm-wiki 파이프라인이 이미 이 패턴(GUI 앱을 CLI로 구동)을 쓰고 있습니다.
- **`chrome-devtools` MCP / `browser-harness` / `playwriter`** — cli-anything이 대체하려는 GUI 자동화 계열. 단, **코드베이스도 API도 없는 대상**(웹 서비스, 브라우저 전용)에는 여전히 이쪽이 정답이라 상호 보완입니다.

**라우트아웃 (이름은 비슷하지만 용도가 다름)**

- **`harness`** — 소프트웨어 래퍼가 아니라 도메인 에이전트 팀(agents + skills) 설계용.
- **`ccpi-marketplace`** — Claude 플러그인 일반 탐색용.
- **`scrapling`** — 웹 스크래핑/추출 전용.

## Routing Brief

'''markdown
# cli-anything Routing Brief

## Scope
- Mode: use-ecosystem (설명/비교 단계 — 빌드 불필요)
- Target software: undecided
- Agent platform: claude

## Recommended next move
- cli-hub-search — MCP 도구(hub_search)가 이미 연결돼 있어 설치 단계조차 생략 가능

## Why
- skills/에 cli-anything이 프로젝트 설치·잠금 완료, 메타스킬도 글로벌 설치됨
- CLI-Hub MCP 서버가 이 세션에 살아 있어 즉시 레지스트리 탐색 가능
- 레지스트리 우선 원칙: 40+ 하니스 검색이 7단계 생성보다 항상 먼저

## Route-outs
- `harness` — 에이전트 팀 아키텍처 (소프트웨어 래퍼 아님)
- `browser-harness`/`playwriter` — 코드베이스 없는 GUI/브라우저 대상
- `ccpi-marketplace` — 일반 Claude 플러그인 탐색
'''

요약하면: 이 환경은 cli-anything 기준으로 **이미 풀스택 연동 상태**(스킬 + 메타스킬 + MCP 허브)이고, 가장 가까운 친척은
```
