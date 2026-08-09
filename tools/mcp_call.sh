#!/bin/bash
# Usage: mcp_call.sh <tool-name> '<json-args>' [timeout-secs]
TOOL_NAME=$1
ARGUMENTS=${2:-'{}'}
TIMEOUT=${3:-120}
H_JSON="Content-Type: application/json"
H_ACC="Accept: application/json, text/event-stream"
URL=http://127.0.0.1:27513/mcp
SESSION_ID=$(curl -s -i -X POST $URL -H "$H_JSON" -H "$H_ACC" -d '{"jsonrpc":"2.0","id":1,"method":"initialize","params":{"protocolVersion":"2024-11-05","capabilities":{},"clientInfo":{"name":"gjc","version":"1.0"}}}' | grep -i 'Mcp-Session-Id' | awk '{print $2}' | tr -d '\r')
[ -z "$SESSION_ID" ] && { echo "no session" >&2; exit 1; }
curl -s -X POST $URL -H "$H_JSON" -H "$H_ACC" -H "Mcp-Session-Id: $SESSION_ID" -d '{"jsonrpc":"2.0","method":"notifications/initialized"}' >/dev/null
curl -s -m "$TIMEOUT" -X POST $URL -H "$H_JSON" -H "$H_ACC" -H "Mcp-Session-Id: $SESSION_ID" \
  -d "{\"jsonrpc\":\"2.0\",\"id\":2,\"method\":\"tools/call\",\"params\":{\"name\":\"$TOOL_NAME\",\"arguments\":$ARGUMENTS}}" \
  | grep '^data: ' | tail -1 | cut -c7-
