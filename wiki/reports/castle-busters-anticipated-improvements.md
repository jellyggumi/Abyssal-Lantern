# Castle Busters — Anticipated Improvements & Optimization Opportunities

**Prepared**: 2026-07-11  
**Status**: Pre-playtest analysis  
**Framework**: $bmad-gds × $product-strategy  

---

## Overview

Based on **code analysis** and **prior cycle sync notes**, this document identifies **likely improvement opportunities** that will be validated in Phase 1 (Cycles 1–5).

---

## Category 1: Game Feel & Presentation

### 1.1 Animation Synchronization
**Issue**: Attack animations may not sync with damage events  
**Impact**: Makes combat feel delayed/disconnected  
**Current State**: `UnitSpriteAnimator.PulseAttack()` exists, but timing needs verification  
**Improvement Proposal**:
- [ ] Audit animation frame counts vs damage timing
- [ ] Adjust frame duration (target: 100–150ms attack animation)
- [ ] Sync particle burst with damage application
- **Effort**: SMALL | **Impact**: HIGH

### 1.2 Particle Effect Intensity
**Issue**: Explosion particles may be too subtle or too intense  
**Impact**: Visual feedback unclear  
**Current State**: `GameFeelVfx.cs` has configurable particle scaling  
**Improvement Proposal**:
- [ ] Playtest with 30 games, survey participants
- [ ] Adjust particle lifetime, velocity, scale per gimmick
- [ ] Target: explosions feel "meaty", debris feels "satisfying"
- **Effort**: SMALL | **Impact**: MEDIUM

### 1.3 Screen Shake Tuning
**Issue**: Screen shake strength may need calibration  
**Impact**: Impact feel / sense of weight  
**Current State**: `HitStopManager` + `GameFeelVfx` control shake  
**Improvement Proposal**:
- [ ] Test shake on different impact types (melee, ranged, explosion)
- [ ] Target: 0.05s hit stop for heavy impacts, 0.02s for light
- [ ] Shake magnitude: scale by damage dealt
- **Effort**: SMALL | **Impact**: MEDIUM

### 1.4 Camera Framing During Flight
**Issue**: Camera may lose sight of launched unit  
**Impact**: Player can't track their shot  
**Current State**: `GamePresentationDirector` has camera tracking  
**Improvement Proposal**:
- [ ] Verify camera smoothly follows launched units
- [ ] Test with fast/slow units
- [ ] Return to board view after landing
- **Effort**: SMALL | **Impact**: MEDIUM

---

## Category 2: Game Balance

### 2.1 Unit Damage Values
**Issue**: Damage variance across units unknown until playtest  
**Impact**: One unit may dominate → stale gameplay  
**Current State**: Knight melee, Archer ranged, Bomber AoE  
**Improvement Proposal**:
- [ ] Collect win rate per unit from 30-game playtest
- [ ] Target: ±20% variance (no unit >60% or <40% win rate)
- [ ] If imbalanced:
  - Knight too weak: ↑ damage or speed
  - Archer too strong: ↓ range or damage
  - Bomber unclear: ensure AoE radius visible
- **Effort**: SMALL | **Impact**: HIGH

### 2.2 Unit Launch Speed / Cooldown
**Issue**: Launch delays may feel slow or punish cautious play  
**Impact**: Gameplay pacing  
**Current State**: Turn timer = 15s per player  
**Improvement Proposal**:
- [ ] Measure average time-to-first-launch in playtest
- [ ] If >10s: streamline UI (faster unit selection)
- [ ] If <2s: add breathing room (narrative beat)
- **Effort**: SMALL | **Impact**: MEDIUM

### 2.3 Gimmick Balance
**Issue**: Buff/Debuff zones may be too powerful or useless  
**Impact**: Game feels random or deterministic  
**Current State**: Buff ×1.5, Debuff ÷0.5 damage  
**Improvement Proposal**:
- [ ] Track unit win rate when buff/debuff is active
- [ ] If debuff zones are ignored: ↑ effect radius or impact
- [ ] If buff zones dominate: ↓ 1.5× to 1.3×
- **Effort**: SMALL | **Impact**: MEDIUM

---

## Category 3: Content & Progression

### 3.1 Difficulty Curve
**Issue**: Game may be too easy (player always wins) or too hard (RNG-dependent)  
**Impact**: Low replayability  
**Current State**: Single fixed AI difficulty  
**Improvement Proposal**:
- [ ] Measure player win rate from playtest
- [ ] If >60%: Increase AI difficulty (better targeting, faster response)
- [ ] If <40%: Decrease AI difficulty (more random shots, slower response)
- [ ] Implement difficulty scaling (Easy/Normal/Hard)
- **Effort**: MEDIUM | **Impact**: HIGH

### 3.2 Stage Variety
**Issue**: Only 1 stage; gimmick placement fixed  
**Impact**: Low content depth  
**Current State**: Moving obstacle + Buff/Debuff zones in center  
**Improvement Proposal** (v1.0 scope decision):
- **Keep for v1.0**: Current stage (polished)
- **Defer to v2.0**: Additional stages, destructible obstacles
- **Reasoning**: 1 stage is enough for portfolio v1.0, shows core loop
- **Effort**: N/A (scope decision) | **Impact**: N/A

### 3.3 Unit Type Depth
**Issue**: Only 3 unit types; limited strategic diversity  
**Impact**: Gameplay feels narrow  
**Current State**: Knight, Archer, Bomber (all functional)  
**Improvement Proposal** (v1.0 scope decision):
- **Keep for v1.0**: 3 unit types (sufficient for core mechanics showcase)
- **Defer to v2.0**: 5–8 unit types, class system, synergies
- **Reasoning**: Depth can be added via balance tuning, not new units
- **Effort**: N/A (scope decision) | **Impact**: N/A

---

## Category 4: Stability & Performance

### 4.1 Memory Leaks
**Issue**: Units/particles not cleaned up properly  
**Impact**: Game slows after 20+ games  
**Current State**: `DestroyAfterTime` used for cleanup  
**Improvement Proposal**:
- [ ] Profile 30-game playtest session
- [ ] Check memory growth per game
- [ ] If >10% growth per game: audit object pooling
- [ ] Target: <2% memory growth per game
- **Effort**: MEDIUM | **Impact**: HIGH

### 4.2 Frame Rate Stability
**Issue**: FPS may drop during explosions or many particles  
**Impact**: Input feels unresponsive  
**Current State**: Particle effects + physics simulation  
**Improvement Proposal**:
- [ ] Profile FPS during heavy particle spawning
- [ ] If FPS <50: reduce particle count or lifetime
- [ ] If FPS >55 throughout: no change needed
- **Effort**: MEDIUM | **Impact**: MEDIUM

### 4.3 Edge Cases
**Issue**: Unhandled state transitions or null refs  
**Impact**: Game crashes  
**Current State**: State machine in place, but edge cases unknown  
**Improvement Proposal**:
- [ ] Run fuzzing test: rapid unit launches, gimmick triggers
- [ ] Log any MissingReferenceException or NullReferenceException
- [ ] Add null-safety checks to high-risk paths
- **Effort**: MEDIUM | **Impact**: HIGH

---

## Category 5: Monetization & User Retention (Future Phases)

### 5.1 Session Length
**Issue**: Game may be too short (<2 min) or too long (>10 min)  
**Impact**: Player drop-off  
**Improvement Proposal** (v2.0):
- [ ] Target session: 3–8 minutes for "satisfying match"
- [ ] If too short: ↑ stage size or gimmick variety
- [ ] If too long: ↓ turn timer or HP scaling
- **Effort**: LARGE | **Impact**: HIGH (future phase)

### 5.2 Progression / Unlock System
**Issue**: No progression hook for repeat play  
**Impact**: No incentive to return  
**Improvement Proposal** (v2.0):
- [ ] Design progression: defeats unlock difficulty levels
- [ ] Design cosmetics: unit skins, castle themes
- [ ] Design narrative: short story beats between matches
- **Effort**: LARGE | **Impact**: HIGH (future phase)

### 5.3 Monetization Model
**Issue**: No revenue model defined  
**Impact**: Portfolio project, but monetization unknown  
**Improvement Proposal** (v2.0 planning):
- [ ] Evaluate: Premium, F2P+ads, F2P+cosmetics
- [ ] Recommended for this game: F2P cosmetics (low friction)
- [ ] Example cosmetics: unit skins ($2–5), castle themes ($3–7)
- **Effort**: N/A (business decision) | **Impact**: N/A (future)

---

## Quick Prioritization Matrix

| Area | Issue | Effort | Impact | Priority | Phase |
|------|-------|--------|--------|----------|-------|
| **Feel** | Animation sync | S | H | 1 | v1.0 |
| **Balance** | Unit damage | S | H | 1 | v1.0 |
| **Balance** | Difficulty curve | M | H | 2 | v1.0 |
| **Performance** | Memory leaks | M | H | 2 | v1.0 |
| **Feel** | Particle intensity | S | M | 3 | v1.0 |
| **Feel** | Screen shake | S | M | 3 | v1.0 |
| **Feel** | Camera framing | S | M | 3 | v1.0 |
| **Stability** | Edge cases | M | H | 2 | v1.0 |
| **Content** | Difficulty scaling | M | M | 4 | v1.0 |
| **Content** | Progression | L | H | 5 | v2.0 |

---

## Execution Roadmap (Cycles 6–15)

Once Phase 1 confirms these hypotheses:

**Cycles 6–7** (Priority 1): Animation + Unit Balance  
**Cycles 8–9** (Priority 2): Difficulty + Memory  
**Cycles 10–11** (Priority 3): Feel Polish  
**Cycles 12–13** (Priority 4): Difficulty Scaling  
**Cycles 14–15** (Validation): 30-game retest, measure improvements  

---

## Next Step

Execute **Phase 1 (Cycles 1–5)** to validate these hypotheses with actual playtest data.

