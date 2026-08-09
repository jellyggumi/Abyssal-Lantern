---
type: "prompt"
session_id: "0256d4a5-86d9-4bfe-bffe-ad2fc8146a69"
captured_at: "2026-06-02T01:59:52.688784+00:00"
query_note: "[[wiki/queries/2026-06-02-015952-task-notification]]"
---

# <task-notification>

## Prompt

```text
<task-notification>
<task-id>aacae178828e44da3</task-id>
<tool-use-id>toolu_01TQWxrcz1dizNFXrpomxq5c</tool-use-id>
<output-file>/private/tmp/claude-501/-Users-jangyoung--superset-projects-jeo-code/0256d4a5-86d9-4bfe-bffe-ad2fc8146a69/tasks/aacae178828e44da3.output</output-file>
<status>completed</status>
<summary>Agent "Codex gjc structural review" completed</summary>
<result>## 8. Codex structural-review pass

gjc is a multi-package Bun workspace with separate AI/provider, CLI/runtime, stats, utils, native, and TUI packages; joc is currently a single compact package where CLI dispatch, config, provider calls, auth token storage, workflow state, and tools live mostly under `src/cli.ts` and `src/agent/*`. Shell `gh`/`curl` returned no remote output here, so gjc evidence below is grounded in fetched raw GitHub files; files that failed fetch are not used as internal evidence.

1. **Workspace/package boundaries** | gjc evidence: root `package.json` declares `workspaces.packages: ["packages/*", ...]` and catalogs `@gajae-code/ai`, `@gajae-code/coding-agent`, `@gajae-code/agent-core`, `@gajae-code/utils`, `@gajae-code/stats`, `@gajae-code/tui`; joc `package.json` is one package. | Recommended joc path + exports: split ownership into `packages/ai`, `packages/coding-agent`, `packages/agent-core`, `packages/utils`; export `@jeo-code/ai`, `@jeo-code/agent-core`, `@jeo-code/coding-agent`. | **CORE**

2. **Lazy CLI runner plus default launch command** | gjc evidence: `packages/coding-agent/src/cli.ts` registers `CommandEntry[]`, lazy-loads command modules, checks Bun version, and routes non-subcommand argv to `launch`; joc `src/cli.ts` uses direct imports and a hardcoded switch with no default agent launch. | Recommended joc path + exports: `src/cli/runner.ts` export `runCli(argv)`, `commands`; `src/commands/launch.ts` export default launch command; CLI owns dispatch only. | **CORE**

3. **Provider/model abstraction layer** | gjc evidence: `packages/ai/src/index.ts` exports provider modules, `model-manager`, `model-cache`, `provider-models`, discovery, stream, schema, usage; `model-manager.ts` defines `ModelManagerOptions`, refresh strategies, cache TTL, static/dynamic/model.dev merge. joc `src/agent/loop.ts` infers providers from model strings and embeds HTTP calls inline. | Recommended joc path + exports: `src/ai/index.ts`, `src/ai/providers/*`, `src/ai/model-manager.ts`; export `createModelManager`, `resolveProviderModels`, provider `stream/call` adapters. | **CORE**

4. **Auth subsystem separation** | gjc evidence: `packages/ai/src/index.ts` exports `auth-broker`, `auth-gateway`, `auth-storage`, OAuth types; `auth-broker/index.ts` exports `client`, `refresher`, `remote-store`, `server`, `types`; `auth-gateway/index.ts` exports `http`, `server`, `types`. joc `src/commands/auth.ts` asks users to paste bearer tokens into `~/.joc/config.json`. | Recommended joc path + exports: `src/auth/index.ts`, `src/auth/storage.ts`, `src/auth/oauth.ts`, `src/auth/refresh.ts`; export `AuthStorage`, `loginOAuth`, `refreshOAuthToken`, `resolveCredential`. | **CORE**

5. **Runtime extensibility and team/session surfaces** | gjc evidence: `packages/coding-agent/package.json` exports broad `session/*`, `task/*`, `tools/*`, `slash-commands/*`, `extensibility/{custom-commands,custom-tools,extensions,hooks,plugins}/*`; `commands/team.ts` delegates to `gjc-runtime/team-runtime` with start/list/status/shutdown/api operations. joc has fixed workflow commands and local tool functions only. | Recommended joc path + exports: `src/runtime/team-runtime.ts`, `src/session/*`, `src/tools/registry.ts`, `src/extensibility/*`; export `startJocTeam`, `listJocTeams`, `ToolRegistry`, `ExtensionRegistry`. | **CORE**

Highest-priority next action: carve out `src/ai/*` and `src/auth/*` first, because provider/model/auth boundaries unblock the launch command, config schema, and runtime extensibility cleanly.</result>
<usage><total_tokens>13256</total_tokens><tool_uses>1</tool_uses><duration_ms>271211</duration_ms></usage>
</task-notification>
```
