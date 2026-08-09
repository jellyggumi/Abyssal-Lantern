# Production brief — castle-war

- run-id: 20260809-castle-war-stage1
- date: 2026-08-09
- director: game-production-director (session-run)

## bmad-gds intake schema

| Field | Value |
|---|---|
| game_type | 2D physics siege, **faction-vs-faction castle war** (1v1, blue vs red), Unity 2D URP |
| team_shape | 5-role harness (director / designer / PM / programmer / QA) run as sequential sub-agent lanes |
| engine | Unity 2022.3.62f2 (pinned); Unity MCP (`com.ivanmurzak.unity.mcp` 0.87.0) + batch CLI |
| current_stage | Existing playable build ("Castle Busters"): 3 units, 3 stages, 41 EditMode tests, balance Phase 2 complete (50/50 win-rate) → **enter Stage 1 as a concept pivot**, carrying the build |
| next_public_beat | WebGL build playable from the menu page of https://jellyggumi.github.io/ |
| source_packet | README.md (Castle Busters state), `design/trend-survey/archery-bastions-castle-war.md`, wiki/reports/castle-busters-phase-{1,2}-*.md |
| main_constraint | Remote rename to `castle-war` blocked on owner (`jellyggumi`) admin action; web deploy must fit GitHub Pages static hosting (no server, Brotli headers unavailable) |
| main_question | How does the single-castle artillery duel become a readable **faction war** (squads exchanging fire continuously, not strictly turn-based single launches) without breaking the balanced 50/50 sim? |

## Operating mode (ONE per cycle)

**Concept-pivot Stage 1**: re-frame the existing build as castle-war
(faction identity, continuous-pressure presentation, meta progression spine),
produce the pivot design pack, and stand up the web-deploy path. Balance
retune (Stage 2) and ops/presentation polish (Stage 3) follow in later cycles.

## Direction pillars (from the user, 2026-08-09)

1. **UX first**: play must be intuitive without reading; presentation (camera,
   art, sound) serves readability before spectacle.
2. Reference genre: Archery Bastions-style faction duel; differentiate with
   physics destruction and stage identity (see trend survey).
3. Asset pipeline: art via Codex CLI, BGM via Gemini(playwriter), per
   CLAUDE.md §3.
4. Everything documented per-project in the llm-wiki
   (`wiki/projects/castle-war/`).

## Stage 1 exit gates

- G7 draft: faction-war core loop modeled numerically + present in build
- G1 draft: worldview (blue/red faction identity + tone) locked
- G6-ops draft: telemetry contract + resource manifest exist
