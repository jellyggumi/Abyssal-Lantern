---
type: "assistant-output"
session_id: "96feac17-dc48-4278-95c8-7c5f2e79"
captured_at: "2026-07-25T23:16:28.628487+00:00"
report: "[[wiki/reports/2026-07-25-231628-pass-10-complete-summary-answer]]"
---

# Pass #10 complete. Summary:

## Output

```text
Pass #10 complete. Summary:

## Pass #10 — Balance / 재미있는 코어타임 (axis 5, %5=5)

**Focus (flagged input):** Pass #5's retro flagged its #1 unresolved item — stages 2–10 replay with **fixed enemy composition** (only cinder-span varies *what* spawns; the others vary only *when/where/how many*). That fixed composition is a repeat-play boredom source.

**Change (data + 1 code branch, determinism-isolated):**
- `buildWaveSchedule` else-branch split three ways: a seeded selection RNG draw is taken **only when `alternatives.length > 1`**, so single-composition stages keep their exact draw order → **byte-identical digests**. Variant stages get composition variety **plus** the existing timing/density jitter (a superset of cinder-span's jitter-free authored path).
- New `STAGE_WAVE_VARIANTS` table gives veil-citadel / echo-throne / sunken-bastion (stages 2–4, the most-replayed early grind band) per-wave pure↔mixed alternatives. Each mixed variant preserves the **same total count** using only enemy classes already in that stage → spawn budget (HP/XP band) unchanged.

**Measured (not self-reported):**
- Untouched 7 stages × 3 seeds: `getRunDigest` **byte-identical** before/after (git-stash comparison).
- Variety: 12-seed opening waves → 4 / 6 / 6 distinct compositions.
- Full suite **191 / 190 pass / 0 fail / 1 skip** including g2-full-route (10-stage real sim) — zero band violations, zero determinism breaks. New guard test covers both the data contract (budget preservation) and runtime selection.

**Commits:** `d6f0ff7` (feat), `7db70f0` (test) — the code was snapshotted into two clean commits by a concurrent session/driver during the pass (author/timestamps/line-counts confirm it's my exact working-tree work; working tree == committed == tested tree); `fcdd528` (my docs: D31 + retro). Each is independently merge-ready.

**Left open (next axis-5 input):** stages 5–10 not yet varied (data-only extension, no code change needed — deprioritized by lower replay frequency); cinder-span still jitter-0; D27's defensive-playstyle late-game XP-denial dead-end is a separate (XP-policy) axis.

**Note:** `scripts/hourly-studio-cycle.sh` is modified in the tree by another session — I left it untouched. Retro (`hourly-passes.md`), decision log (D31), and the wiki studio-log were all updated.
```
