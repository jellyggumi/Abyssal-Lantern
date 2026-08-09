---
type: "assistant-output"
session_id: "b133d257-c741-478d-b124-4905d8bb"
captured_at: "2026-06-15T17:10:26.843823+00:00"
report: "[[wiki/reports/2026-06-15-171026-agy-mcp-plugin-mcp-gemini-ant-answer]]"
---

# 조사 완료. agy에는 `mcp` 서브커맨드가 없고 `plugin`만 있으며, MCP는 `~/.gemini/ant…

## Output

```text
조사 완료. agy에는 `mcp` 서브커맨드가 없고 `plugin`만 있으며, MCP는 `~/.gemini/antigravity-cli/mcp/<name>/` 디렉터리로, 규칙은 AGENTS.md(Customizations Root)로 관리됩니다. 정밀 개선계획을 제시합니다.

# spec-stack Routing Brief

- **Pattern**: docs-only (분석 → 정밀 개선계획) — `setup-all-skills-prompt.md` 수정 전 경로 매핑
- **Target**: gemini-cli(`~/.gemini/`)로 가는 설치/등록을 antigravity-cli(`~/.gemini/antigravity-cli/`, 바이너리 `agy`)로 전환
- **Stack state**: spec-written 직전 (격차 특정 완료)

## 핵심 발견 — gemini-cli ≠ antigravity-cli (별도 런타임, 별도 경로)

머신에서 확인한 두 런타임의 **로드 방식이 완전히 다릅니다**:

| 항목 | gemini-cli (`~/.gemini/`) | antigravity-cli (`~/.gemini/antigravity-cli/`, `agy`) |
|------|--------------------------|------------------------------------------------------|
| MCP | `settings.json` → `mcpServers` JSON 블록 (현재 semble 등 9개 등록됨) | **`mcp/<name>/*.json` 디렉터리** (현재 agentation·graphify·ouroboros·playwriter·serena 5개) |
| 훅 | `settings.json` → `hooks` + `~/.gemini/hooks/` | **`~/.gemini/antigravity-cli/hooks/`** (현재 비어있음); settings.json엔 hooks 키 없음(model·trustedWorkspaces만) |
| 규칙/컨텍스트 | `~/.gemini/GEMINI.md` | **AGENTS.md** (agy 바이너리: "Append rules to AGENTS.md in the Global/Workspace Customizations Root") |
| skills | `~/.gemini/skills/` | `~/.gemini/antigravity/skills/` (이미 Step 2에서 올바르게 처리됨 ✅) |

**즉 현재 setup의 모든 gemini 등록은 `~/.gemini/`(gemini-cli)로 가고, agy 런타임에는 0% 반영됩니다.** skills(Step 2)와 OMA 설치(Step 3g)만 antigravity를 올바르게 타깃합니다.

## 정밀 개선계획 — 라인별 경로 매핑

| # | setup 위치 (라인) | 현재 (gemini-cli) | → antigravity-cli 정밀 경로 | 난이도 |
|---|------|------|------|--------|
| **A** | **Step 3f semble MCP** (505–513) | `~/.gemini/settings.json` mcpServers jq 병합 | `~/.gemini/antigravity-cli/mcp/semble/` 디렉터리 등록 — 단 agy는 `mcp` CLI 서브커맨드가 없어 **plugin 경로 또는 도구 JSON 생성 방식 확정 필요**(아래 검증 1) | ⚠️ 高 |
| **B** | **Step 3f-2 RTK×semble 규칙** (568) | `~/.gemini/GEMINI.md` 주입 루프 | `~/.gemini/antigravity-cli/AGENTS.md`로 주입 (Customizations Root — 검증 2) | 中 |
| **C** | **Step 6 KP BeforeAgent 훅** (1407–1413) | `~/.gemini/settings.json` hooks + `~/.gemini/hooks/llm-wiki-ingest.sh` | `~/.gemini/antigravity-cli/hooks/llm-wiki-ingest.sh` + agy 훅 등록 규약 확인(검증 3) | ⚠️ 高 |
| **D** | **Step 6 KP 규칙** (1447) | `inject_kp_rules ~/.gemini/GEMINI.md` | `inject_kp_rules ~/.gemini/antigravity-cli/AGENTS.md` | 中 |
| **E** | **Step 6 주석** (1313) | "Antigravity / Gemini CLI **shares** `~/.gemini/settings.json`" | **사실 오류 — 수정 필수.** antigravity는 별도 `antigravity-cli/` root | 低 |
| **F** | **Step 3f-2 / Step 6 감지 가드** (1407) | `command -v gemini \|\| command -v agy` | agy 우선 분기: agy면 antigravity-cli 경로, gemini면 `~/.gemini/` — **두 런타임을 모두 지원**하도록 분리 | 中 |

핵심 설계 원칙: gemini-cli 경로를 **삭제하지 말고**, `command -v agy`일 때 antigravity-cli root로 가는 **분기를 추가**합니다 (Step 2의 ohmg가 antigravity→gemini-cli fallback을 둔 것과 대칭). 그래야 두 런타임 사용자 모두 커버됩니다.

## 적용 전 필수 검증 3건 (경로 정밀도 확정)

문서 수정 전에 이 3가지를 실측해야 정확한 경로를 박을 수 있습니다 — 현재 불확실:

1. **antigravity MCP 등록 방법**: `mcp/<name>/` 디렉터리를 agy가 자동 생성하는지(plugin install 경유) vs 수동 도구 JSON 작성. → `agy plugin list`와 기존 5개 MCP가 어떻게 등록됐는지 추적. semble을 수동 도구 JSON으로 까는 건 비현실적이므로, agy의 MCP 등록 정식 경로 확인이 A의 전제.
2. **Customizations Root 정확 위치**: agy가 규칙으로 읽는 AGENTS.md의 Global Root가 `~/.gemini/antigravity-cli/AGENTS.md`인지 `~/.antigravitycli/AGENTS.md`인지(프로젝트엔 `.antigravitycli/`도 존재). → 1회 테스트로 확정.
3. **antigravity 훅 이벤트/등록 규약**: `~/.gemini/antigravity-cli/hooks/`가 디렉터리-드롭 방식인지 매니페스트 등록인지, BeforeAgent 상당 이벤트가 있는지. → TOON 노트(line 663)가 이미 이 경로에 `toon-skill-inject.sh`를 두므로 디렉터리-드롭일 가능성이 높지만 확정 필요.

## Route-outs
- 검증 3건은 **survey**의 platform-map 산출물로 정리하면 재사용 가능 (`.survey/antigravity-paths/`).
- 실제 문서 편집(6개 지점)은 다중 파일·민감 경로 변경이므로 **plannotator** 승인 게이트 권장.

## 다음 한 수
검증 3건을 먼저 실측해 경로를 확정할까요, 아니면 (a) 확실한 B·D·E·F(규칙 주입 + 주석 수정 + agy 분기)만 바로 적용하고 (b) 불확실한 A·C(MCP·훅)는 검증 후 별도 적용하는 2단계로 갈까요? 추천은 **2단계** — 규칙 표면은 즉시 antigravity로 옮기고, MCP/훅은 등록 규약 실측 후 정확히 박는 것입니다.
```
