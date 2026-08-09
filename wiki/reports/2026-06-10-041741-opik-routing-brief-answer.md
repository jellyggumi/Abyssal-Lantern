---
title: "opik Routing Brief"
created_at: "2026-06-10T04:17:41.977554+00:00"
section: "reports"
status: "captured"
session_id: "87e421e1-455c-4e52-b3bd-0cc0e4c5"
raw_output: "[[raw/sources/outputs/2026/06/10/041741-87e421e1-455-opik-routing-brief]]"
source_summary: "[[wiki/sources/2026-06-10-041741-opik-routing-brief-output]]"
---

# opik Routing Brief

## Answer Output (rtk-compressed)

# opik Routing Brief

## Scope
- **Server mode**: undecided (cloud vs `./opik.sh` local)
- **SDK**: python (default; TS/Ruby-OTel also available)
- **Lifecycle stage**: tracing — current model is `claude-opus-4-7` (Anthropic SDK target)

## Recommended next move
- **install-sdk + wire-integration** → `pip install opik && opik configure`, then use Opik's native Anthropic integration to auto-capture every `claude-opus-4-7` call (tokens, latency, prompt, response, cost).

## Why
- "현재 모델 분석" on Claude means tracing real calls — Opik's Anthropic integration logs each request with model metadata and feedback scores, which is the prerequisite for any later analysis (judge metrics, cost dashboards, regression comparisons).
- You're on Opus 4.7 (premium tier) — token/cost visibility matters; Opik's dashboard surfaces token + latency per trace out of the box.
- No stack decision yet — start with `./opik.sh` locally to validate the loop in minutes, switch to Comet cloud or k8s later if you scale.

## Quick start (Anthropic + Opik)
```python
import opik
from opik.integrations.anthropic import track_anthropic
from anthropic import Anthropic

opik.configure(use_local=True)
client = track_anthropic(Anthropic())

msg = client.messages.create(
    model="claude-opus-4-7",
    max_tokens=512,
    messages=[{"role": "user", "content": "..."}],
)
```

## Route-outs
- `langsmith` — if the observability stack is LangSmith, not Opik
- `monitoring-observability` — for non-LLM service dashboards
- `claude-api` — if the question is about Claude SDK usage / model migration, not tracing

**Decision needed**: cloud (Comet.com signup) vs local (`./opik.sh`)? I can wire either.

## Evidence and Citations

- [[wiki/sources/2026-06-10-041741-opik-routing-brief-output]]
- [[raw/sources/outputs/2026/06/10/041741-87e421e1-455-opik-routing-brief]]
