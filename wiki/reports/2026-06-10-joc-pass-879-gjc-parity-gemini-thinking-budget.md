---
tags: [report, joc, gjc, gemini, oauth]
date: 2026-06-10
source: jeo-code pass 879 (commit f99c091)
---

# joc pass 879 — gjc-parity hardening + Gemini thinking-budget fix

Grounded in upstream [[wiki/sources/2026-06-10-gajae-code-repo|gajae-code (gjc)]] analysis; extends [[wiki/reports/2026-06-10-joc-gjc-parity-deep-dive]] and [[wiki/concepts/gjc-vs-joc-architecture]].

## Key durable findings

### 1. Gemini 2.5+/latest empty-reply mechanism (live-found bug)
- Gemini 2.5-class and `*-latest` alias models **think by default** and bill thought tokens against `maxOutputTokens`.
- A small-budget call (e.g. `maxTokens: 16`) burns the whole budget on thoughts → `finishReason=MAX_TOKENS` with **zero text parts** → adapters that join `parts[].text` silently return `""`.
- Fix pattern (joc `src/ai/providers/gemini.ts`): explicit `generationConfig.thinkingConfig.thinkingBudget` — flash-class default **0**, pro-class floor **128** (cannot disable), pre-2.5 **omit** (rejects the field); effort low/medium/high → 1024/4096/8192 clamped to keep ≥ ~1K output tokens for text. Empty+MAX_TOKENS now raises an actionable error.

### 2. gjc parity surfaces adopted by joc
- **Skills**: source-bundled `SKILL.md` embeds (`src/prompts/skills/<name>/SKILL.md`), mirroring gjc `packages/coding-agent/src/defaults/gjc/skills/`. Catalog parses frontmatter-ish markdown; `skills --write` emits raw docs.
- **Context files**: parent walk to git root/$HOME + nested `AGENTS.md` (depth ≤ 3) with cwd→nested→parent budget priority; hooks/rules (`.agents/rules|hooks`, `.joc/rules`) keep a reserved guidance budget.
- **OAuth**: cross-process file lock (`oauth-<provider>.lock`, stale takeover) + in-lock freshness re-check prevents refresh-token double-spend; config saves are temp-then-rename atomic.
- **Retry**: gjc `retry:` config semantics — `requestMaxRetries` (non-stream), `streamMaxRetries` (stream), `maxRetries` fallback, `maxDelayMs` cap.

### 3. Live model verification matrix (2026-06-10)
| Provider | Model | Result |
|---|---|---|
| anthropic (OAuth) | claude-haiku-4-5 | OK |
| anthropic (OAuth) | claude-sonnet-4-5 / opus-4-5 | rate-limited (subscription window; routing+credential verified) |
| openai (Codex OAuth) | gpt-5.5 / gpt-5.4 | OK |
| gemini (API key) | gemini-2.5-flash | OK |
| gemini (API key) | gemini-2.5-pro / flash-latest | quota-limited (free tier) |
| ollama (local) | qwen2.5:0.5b | OK |

Tool: `jeo-code/scripts/verify-models.ts` — exercises the real manager→routing→credential→adapter→retry path; distinguishes RATE (credential verified) from FAIL.

## Verification
`tsc` 0 · `bun test` 743 pass / 0 fail (94 files) · build ok · 0 hard live failures. Commit `f99c091`.
