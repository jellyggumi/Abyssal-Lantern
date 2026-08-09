# NotebookLM Skill & MCP Server Setup Report

## 1. Overview
This report details the successful installation and configuration of **Google NotebookLM** integration for Claude Code via browser automation (Patchright), setting up both the **Claude Code Skill** and the **MCP Server** protocols on this macOS system.

## 2. Completed Operations

### A. Claude Code Skill Setup
- Cloned the `notebooklm-skill` repository into `~/.claude/skills/notebooklm`.
- Created a Python virtual environment and installed all required dependencies (`patchright`, `python-dotenv`).
- **Mac-Specific Optimization**: Patched `browser_utils.py` to automatically detect and utilize the macOS system-wide Google Chrome app (`/Applications/Google Chrome.app`) as the executable path. This bypasses the typical `patchright install chrome` root/sudo password requirements, ensuring clean and secure operations.

### B. MCP Server Setup
- Cloned the official Node/TypeScript `notebooklm-mcp` server repository into `~/.claude/mcp/notebooklm`.
- Successfully installed packages and compiled the code (`npm run build`), generating `dist/index.js`.
- Configured and registered `notebooklm` under the `"mcpServers"` block in `~/.claude/mcp.json`.

```json
"notebooklm": {
  "command": "node",
  "args": [
    "/Users/jangyoung/.claude/mcp/notebooklm/dist/index.js"
  ],
  "env": {
    "HEADLESS": "true"
  }
}
```

### C. Validation & Verification
- Directly launched the compiled MCP server to verify correct module linkages, context loading, and stdio pipe stabilization.
- The server initializes `SharedContextManager`, confirms persistent Chrome profiles, validates standard tools (e.g. `ask_question`, `add_notebook`, `setup_auth`, etc.), and successfully enters the ready state waiting for Claude Code interactions.

## 3. Next Steps & User Instructions
To start querying notebooks:
1. **First-time Authentication**:
   Run the following command to open a real Chrome window and log in manually to your Google Account (this only needs to be done once):
   ```bash
   node ~/.claude/mcp/notebooklm/dist/index.js setup_auth
   ```
2. **Add a Notebook**:
   ```bash
   node ~/.claude/mcp/notebooklm/dist/index.js add_notebook --url "https://notebooklm.google.com/notebook/YOUR_NOTEBOOK_ID" --name "my-research"
   ```
3. **Query Directly**:
   Ask Claude Code to find information on your research files, and Claude will automatically invoke the registered `ask_question` MCP tool to pull answers from your NotebookLM documents!
