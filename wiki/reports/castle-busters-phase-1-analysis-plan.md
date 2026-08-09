# Castle Busters — Phase 1 Analysis Plan (Cycles 1–5)

**Prepared**: 2026-07-11  
**Status**: Framework ready for execution  
**Perspective**: $bmad-gds × $product-strategy × $spec-stack  

---

## Executive Summary

Castle Busters is a **playable Unity vertical slice** featuring:
- ✅ 3 unit types (Knight, Archer, Bomber)
- ✅ 3 gimmick systems (Moving, Buff/Debuff, Castle Core)
- ✅ 1v1 PvP turn-based artillery gameplay
- ✅ Physics-based destruction and impact detection
- ✅ Procedural animation and particle effects

**This phase establishes baseline metrics** and identifies the **top 5 improvement candidates** for Cycles 6–25.

---

## What We Know (From Code Analysis)

### Architecture Strengths
✅ **Clean state machine**: `GameState.Intro/PlayerTurn/AITurn/GameOver`  
✅ **Modular units**: Each unit (Knight/Archer/Bomber) has its own controller with state transitions  
✅ **Gimmick system**: BuffDebuff, MovingObstacle, CastleCore all properly integrated  
✅ **Physics pipeline**: 2D rigidbodies, colliders, destruction on impact  
✅ **Presentation layer**: Sprite animation, particle effects, camera tracking  

### Code Health Indicators (Scan Results)
| Component | File Count | Stability | Completeness |
|-----------|-----------|-----------|--------------|
| **Core Game** | 3 | ✅ Stable | ✅ Complete |
| **Units** | 6 | ✅ Stable | ✅ Complete |
| **Gimmicks** | 4 | ✅ Stable | ✅ Complete |
| **Presentation** | 5 | ✅ Stable | ✅ Complete |
| **Tests** | 5 | ✅ Good | 🔶 Partial (no full coverage) |

---

## Cycle-by-Cycle Execution Plan

### ✅ CYCLE 1: Build & Stability (2–3 hours)

**Executor Tasks**:
1. Open project in Unity Editor
2. Verify compilation: 0 errors, 0 warnings
3. Run scene in Play mode for 30 seconds
4. Capture: startup time, FPS, memory baseline
5. Log any Console warnings/errors
6. Test manual unit launch

**Metrics to Capture**:
- Compile time
- Scene load time
- Baseline FPS (Intro screen)
- Memory usage (before/after scene load)
- Runtime exception count

**Success Criteria**:
- ✅ Compiles cleanly
- ✅ No MissingReferenceException
- ✅ Scene loads & plays without crashes

---

### ✅ CYCLE 2: Mechanics Validation (3–4 hours)

**Executor Tasks**:
1. Test each unit individually (Knight → Archer → Bomber)
2. Verify unit state transitions (Launched → Grounded → Attacking → Dead)
3. Test each gimmick (Moving, Buff, Debuff, Castle Core)
4. Verify win/loss conditions
5. Run 10 automated full-game playtests

**Metrics to Capture**:
- Unit launch success rate
- Attack accuracy (hits vs misses)
- Gimmick trigger rate
- Game completion rate

**Success Criteria**:
- ✅ All 3 units launch and attack
- ✅ All gimmicks trigger correctly
- ✅ 10/10 games complete without errors

---

### ✅ CYCLE 3: Large-Scale Playtest (4–5 hours)

**Executor Tasks**:
1. Run 30 automated games (both units selectable, random targets)
2. Collect per-game statistics:
   - Winner (Player vs AI)
   - Game duration
   - Turn count
   - Unit preferences
   - Damage dealt/taken
3. Aggregate statistics

**Metrics to Capture**:
- Player win rate: X%
- AI win rate: X%
- Average game duration: X min
- Average turns per game: X
- Unit usage: Knight/Archer/Bomber percentage
- Most effective unit: [Unit] (by win rate when selected)

**Success Criteria**:
- ✅ 30/30 games complete
- ✅ No crashes/hangs
- ✅ Data quality check (metrics make sense)

---

### 🔍 CYCLE 4: Balance & Usability Analysis (2–3 hours)

**Architect Tasks** (reads Cycle 3 output):
1. Analyze win-rate variance across units
2. Identify outliers (too strong/weak units)
3. Review gimmick effectiveness
4. Assess animation smoothness
5. Identify UI/UX friction points

**Key Questions**:
- Is the win-rate variance >±20%? → Unit imbalance
- Does one unit dominate? → Needs tuning
- Are particles/effects too subtle/intense? → Needs adjustment
- Is camera tracking lost during flight? → UX issue

**Outputs**:
- Top 5 issues (prioritized by impact × frequency)
- Root cause analysis for each
- Quick fix vs deep fix assessment

---

### 🎯 CYCLE 5: Improvement Proposals (2–3 hours)

**Architect Tasks**:
1. For each of the Top 5 issues:
   - Propose specific fix (code + config)
   - Estimate effort (small/medium/large)
   - Estimate impact (high/medium/low)
2. Prioritize by effort × impact ratio
3. Create concrete tasks for Cycles 6–10

**Deliverables**:
- Issue priority matrix (effort vs impact)
- Concrete code changes (PRs ready)
- Difficulty scaling proposal (AI tuning)
- Content depth assessment (v1.0 vs v2.0 scope)

---

## Data Aggregation & Reporting

### After Cycle 5, Aggregate Report

**File**: `wiki/reports/castle-busters-phase-1-summary.md`

```markdown
# Phase 1 Summary (Cycles 1–5)

## Baseline Metrics
- Compile Status: ✅
- Runtime Stability: ✅
- Player Win Rate: X%
- Avg Game Duration: X min
- Unit Balance (variance): ±X%

## Top 5 Issues (Ranked by Priority)
1. [Issue] — Impact: HIGH, Effort: MEDIUM
2. [Issue] — Impact: HIGH, Effort: LARGE
3. [Issue] — Impact: MEDIUM, Effort: SMALL
...

## Recommendations for Phase 2
- Focus on [Issue 1] first (quick win)
- Then [Issue 2] (larger payoff)
- Parallel work: [Issue 3] + [Issue 4]

## Content Scope (v1.0)
- Keep: 3 units, 3 gimmicks, 1 stage
- Add (if time): AI difficulty levels, additional gimmick variance
```

---

## Quality Gate Checklist (Critic Role)

For each cycle, **Critic verifies**:

- [ ] All metrics collected and logged
- [ ] No data contradictions
- [ ] Findings are actionable (specific, not vague)
- [ ] Success criteria met
- [ ] Handoff to next cycle is clear

---

## Timeline & Effort

| Cycle | Executor | Architect | Critic | Total |
|-------|----------|-----------|--------|-------|
| 1 | 2–3 hrs | — | 0.5 hr | ~3 hrs |
| 2 | 3–4 hrs | — | 0.5 hr | ~4.5 hrs |
| 3 | 4–5 hrs | — | 0.5 hr | ~5.5 hrs |
| 4 | — | 2–3 hrs | 0.5 hr | ~3.5 hrs |
| 5 | — | 2–3 hrs | 0.5 hr | ~3.5 hrs |
| **Total** | **9–12** | **4–6** | **2–3** | **~20 hrs** |

*(Can be parallelized: 4 agents working concurrently = ~5 hrs wall time)*

---

## Success Criteria (Phase 1 Complete)

✅ All 5 cycles logged with data  
✅ Game compiles & runs stably  
✅ 30+ games playtested  
✅ Top 5 issues identified + ranked  
✅ Concrete improvement tasks ready for Cycles 6–15  
✅ Phase 1 summary report filed  

---

## Next Steps (Phase 2 — Future Session)

Once Phase 1 is complete:
1. **Executor**: Implement fixes for Top 5 issues
2. **Architect**: Design AI scaling, additional content
3. **Critic**: Validate each fix, measure impact

Continue until Cycle 25 (game is "done").

