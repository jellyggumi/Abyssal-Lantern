---
type: "source-summary"
source_type: "assistant-output"
captured_at: "2026-07-21T12:34:36.363453+00:00"
raw_path: "raw/sources/outputs/2026/07/21/123436-5fb36489-39c-i-ve-now-seen-the-real-script-1968-lines-confirm.md"
session_id: "5fb36489-39c2-4719-9273-d07968e6"
rtk_method: "rtk"
rtk_original_chars: 2293
rtk_compressed_chars: 2293
rtk_saved_pct: 0.0
---

# I've now seen the real script (1968 lines). Confirmation of wha…

- Raw output: [[raw/sources/outputs/2026/07/21/123436-5fb36489-39c-i-ve-now-seen-the-real-script-1968-lines-confirm]]
- Filed report: [[wiki/reports/2026-07-21-123436-i-ve-now-seen-the-real-script-1968-lines-confirm-answer]]
- rtk compression: rtk (2293→2293 chars, -0.0%)

## Compressed Output (rtk)

```text
I've now seen the real script (1968 lines). Confirmation of what it is: this is your own `jeo-skills` provisioning guide, and it's clearly the same one that already produced the "Knowledge Pipeline" section in your global `~/.claude/CLAUDE.md` (Step 6 writes that exact block — it's already present in your config). So this looks like a maintenance/re-sync run of a script you've already applied before, not a random third-party install.

**What it actually does, step by step:**

| Step | Action | Blast radius |
|---|---|---|
| 0 | Detect installed agents (claude/codex/gemini/opencode/jeopi/gjc/jeo/pi), snapshot existing skills | read-only |
| 1 | `skills add -g <repo> --skill '*' -a '*' --copy --full-depth` — installs all 152 skills to `~/.agents/skills/` | writes many files globally |
| 2 | Re-links platform-specific skills (`omc`→claude-code, `ohmg`→antigravity, `omx`→codex+claude-code), deletes stray cross-platform symlinks | deletes symlinks (not source skills) |
| 3a–3j | Installs RTK (brew/curl), Graphify (uv venv), **ouroboros-ai via pip + `claude mcp add ooo`**, Obsidian (brew cask), bootstraps `~/vaults/llm-wiki`, **semble via uv + `claude mcp add semble`**, installs oh-my-claudecode/oh-my-codex/oh-my-agent plugins, edits `~/.codex/config.toml` (with a self-repair patch for a known TOML corruption bug), wires jeo/pi/gjc rule files | installs packages, registers 2 new MCP servers, edits Codex's config.toml |
| 4 | Verification/skill counts | read-only |
| 5 | Prompts you to star the repo (consent-first) | asks, doesn't act |
| 6 | Writes the default-operating-rules block into global `CLAUDE.md` (the block you already have) | edits `~/.claude/CLAUDE.md` |

It's written to be idempotent/non-destructive (no `skills remove`, preserves existing skills, backs up `config.toml` before any repair), but it does touch global config across multiple tools and registers two MCP servers you don't currently have wired (`ooo`, `semble` — though `semble` and `ouroboros` MCP servers already showed up as connecting in this session, so at least those are likely already installed).

Given most of this looks already applied on your machine, want me to just run **Step 4 (verification)** to see what's actually missing/stale, rather than blindly re-running the full install?
```
