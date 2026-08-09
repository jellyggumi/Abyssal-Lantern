# Prompt Knowledge Workflow

## Summary

Every user prompt and assistant output should be handled as a compact, reusable knowledge loop when the result may matter beyond the current chat.

## Operating Rules

- Compress prompt input and assistant output with `rtk` before they become durable wiki knowledge; preserve verbatim originals under `raw/sources/prompts/` or `raw/sources/outputs/`.
- Use `~/vaults/llm-wiki` as the single root for llm-wiki, Graphify, and the Obsidian vault (`llm-wiki`).
- Read `index.md` before broad search.
- Search linked `wiki/` pages before raw sources.
- Use Graphify artifacts and focused graph queries before rebuilding context from scratch.
- Store the authoritative full graph at `graphify-out/graph.json`; store the auto-rebuilt prompt/output structural graph at `graphify-out/prompts/graph.json`.
- File reusable prompts, answers, and reports in `wiki/queries/` or `wiki/reports/`.
- Update `index.md` and `log.md` after durable filing.

## Tool Roles

| Tool | Role |
|------|------|
| `rtk` | Token-compress shell output, prompt input, and assistant output before durable storage |
| `graphify` | Refine durable knowledge into graph-backed queryable structure |
| Obsidian / `obsidian-cli` | Vault UI plus file/folder management at `~/vaults/llm-wiki` |
| `llm-wiki` | Persistent markdown knowledge root and maintenance contract |

## Default Paths

- Vault root: `~/vaults/llm-wiki`
- Obsidian vault: `llm-wiki`
- Wiki index: `~/vaults/llm-wiki/index.md`
- Graphify output: `~/vaults/llm-wiki/graphify-out/`

## Related Pages

- [[wiki/concepts/graphify-integration]]
