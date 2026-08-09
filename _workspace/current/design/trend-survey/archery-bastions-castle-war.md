# Trend survey — Archery Bastions: Castle War

- run-id: 20260809-castle-war-stage1
- source: https://play.google.com/store/apps/details?id=com.bastion.archers
- captured: 2026-08-09 via scrapling fetch (status 200) [OBSERVED]
- role: primary genre reference for castle-war's faction-vs-faction pivot

## Store facts [OBSERVED]

| Field | Value |
|---|---|
| Title | Archery Bastions: Castle War |
| Publisher | CASUAL AZUR GAMES (dev: AI GAMES FZ LLC) |
| Rating | 4.6 ★ (128K reviews) |
| Downloads | 10M+ |
| Monetization | In-app purchases + ads (reviews mention paid ad-removal) |
| Platforms | Android, Windows (Google Play Games PC) |
| Content | All ages, mild violence |
| Last update | 2026-08-07 |

## Core structure (from store copy + reviews) [OBSERVED]

- Two fixed factions: **blue (player) vs red (enemy)** — identity is color-coded,
  zero narrative overhead ("Grass is ever green. Enemies are ever red.").
- Side-vs-side screen layout: each faction holds a castle/bastion; archers
  exchange fire across the middle.
- Progression: endless numbered levels (players report level 396, 1,576).
- Unlockable magician skills: ground army spawn, double arrows, firework
  arrows, hero call, flying pigs — comedic tone carries the presentation.
- Upgrade loop: gold → castle/troop upgrades between levels.

## Weaknesses named by players [OBSERVED — review quotes]

1. **No meta goal**: "easy and repetitive with no ultimate goals… needs an
   overarching map with unique boss levels and a simple story" (level 396
   player with ~1M unspent gold — economy stops mattering).
2. **Progress fragility**: freeze at ~level 315 lost all progress; no cloud
   save / recovery path.
3. **Fairness perception**: units dying "for no reason" post-update reads as
   the AI cheating — invisible mechanics get read as unfair.

## What castle-war takes from this [INFERENCE]

| Adopt | Differentiate |
|---|---|
| Instant faction readability (blue vs red, one-screen duel) | Our physics destruction + BFS structural collapse is the moment-to-moment hook the reference lacks |
| Skill-unlock cadence as the dopamine spine | Stage identity (Siege Plains / Ashen Bastion / Frostbound Gorge) already gives us the "overarching map" players asked for |
| Comedic, low-lore tone (cheap to produce, ages well) | Deterministic sim + visible wind/gimmick telegraphing answers the "AI cheats" complaint — nothing invisible ever kills a unit |
| Short session, endless-level economy | Bounded comeback mechanics (G5: reversal ≤30%) instead of raw stat inflation |

## Gate relevance

- G8 (novelty): physics-destruction faction duel appears in ≤2 of the 6
  surveyed comparables on the store page (Merge Archers, Conquer the Tower,
  Top War, Boom Stick, King or Fail, Tank Stars — none combine BFS structural
  collapse with 1v1 faction siege). Needs the full ≥5-title frequency table in
  Stage 2. [TARGET]
- G7 (core loop): reference validates a 30–90s volley→collapse→reward loop.
