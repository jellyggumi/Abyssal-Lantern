# Query: Jeo-Code Agent Architecture & Testing Evidence

## Context & Objectives
Analysis and structural implementation of the **Jeo-Code (`jeo`)** developer agent based on the architectural paradigms of **Gajae-Code (`gjc`)**.

This document captures the durable knowledge about `gjc` design patterns, OAuth broker-free token mechanics, dynamic capabilities resolution, and the sandboxed turn-loop execution test results.

---

## 1. Gajae-Code (`gjc`) Architectural Mapping

Through intensive codebase extraction of `https://github.com/Yeachan-Heo/gajae-code`, we verified the package topology:
- **`packages/ai`**: Handles provider/model boundaries, streaming, and auth.
- **`packages/agent`**: Implements turn loop logic (`agent-loop.ts`).
- **`packages/coding-agent`**: Main CLI surface, embeds default workflow skills (`deep-interview`, `ralplan`, `ultragoal`, `team`).
- **`packages/tui`**: Custom Terminal UI layout renderer.

### Identified Bottlenecks
1. **OAuth Synchronization Complexities**: Gajae-Code implements an `AuthBrokerClient` requiring separate tailnet/central coordination servers, adding high orchestration costs for single-user workspaces.
2. **Context Bloat & Static Capabilities**: Model capabilities are mapped from a static `models.json` file (~1.5MB), making local model custom endpoints highly rigid.

---

## 2. Jeo-Code (`jeo`) Solution & Key Innovations

Jeo-Code implements a streamlined, zero-broker, fast-boot version of the code agent using **Bun**:
1. **Local-First OAuth Redirect Server**: Ephemeral callback listener at `http://localhost:3500/callback` that completes code token exchanges and persists credentials under `~/.jeo/credentials.yaml` securely.
2. **Auto-Token Refresh**: Active loop auto-detects token expiry and runs `refreshOAuthToken()` transparently.
3. **Decoupled Turn Loop & Tool Matching**: Streams reasoning/thinking tokens separately from tool execution JSON payloads.
4. **Sandboxed Tools Execution**: Safely isolates `read_file`, `write_file`, and `execute_command` execution directories.

---

## 3. Terminal Verification Logs (100% Success)

### OAuth E2E Subsystem Test
```text
$ bun ./bin/jeo.ts test-oauth
ℹ Test Provider: oauth-test-provider
ℹ 1. Starting Local Server & Simulating Auth callback...
Simulating browser redirect...
✔ Ephemeral HTTP callback server answered successfully.
✔ Auth token successfully saved to credentials.yaml
ℹ 2. Testing Auto-Refresh Routine...
[OAuth] Token for provider 'oauth-test-provider' expired. Refreshing...
[OAuth] Token refreshed successfully.
✔ Auto-refresh successful. Acquired renewed token.
```

### Stateful Agent Turn Loop & Tool Execution Test
```text
$ bun ./bin/jeo.ts test-agent
ℹ Mock LLM Server: Running on http://localhost:3600
ℹ Provider: mock-llm
ℹ Model: Mock Thinking Agent v1
--- Turn 1 of 5 ---
[THINKING] <Analyzing user request: "Create a python script under scratch/hello.py"
Plan: I need to create a hello.py in scratch folder.>
```tool_call
{
  "type": "toolCall",
  "name": "write_file",
  "arguments": {
    "path": "hello.py",
    "content": "print('Hello, World from Jeo-Code!')\n"
  }
}
```
ℹ Executing Tool: write_file
┌── Tool Execution Result ───────────────────────────────┐
│ Success: File written successfully to scratch/hello.py │
└────────────────────────────────────────────────────────┘

--- Turn 2 of 5 ---
I have successfully created the requested file.
✔ Goal reached successfully! No further tools needed.
```

## 4. Prompt Repetition Decision & Intervention Fit
- **Classification**: **Tool-heavy agent execution & multi-step reasoning task**.
- **Fit Evaluation**: **Weak Fit** for prompt repetition.
  - *Rationale*: Agent systems running tool loops and stateful task planning (such as OAuth exchanges or file sandboxing) rely heavily on precise, sequential state mutations. Duplicating or repeating full prompts inside an active agent turn loop leads to increased input-token consumption, higher operational costs, and elevated risks of tool execution confusion.
- **Recommended Action**: **No repetition**. Rely on structured modular specifications (`seed.yaml`), clear JSON schemas for tool definitions, and clean step-by-step division of concern.

---

## 5. Graphify Knowledge Refinement
To refine this durable query under Obsidian CLI vault control, we have updated `index.md` to map `wiki/queries/2026-06-02-jeo-code-agent-spec-and-test.md`.

*Durable analysis registered under local time: 2026-06-02T08:40:00+09:00.*
