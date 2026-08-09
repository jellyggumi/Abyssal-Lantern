# Castle Busters — Phase 3 Roadmap
## Polish & VFX Enhancement

**Phase**: 3/5  
**Start Date**: 2026-07-11  
**Estimated Duration**: 3–4 days  
**Goal**: Transform game feel from mechanical to polished; add visual feedback depth

---

## Executive Summary

Phase 2 established perfect mechanical balance (50% win rate, 32% unit usage each). Phase 3 shifts focus from **balance** to **feel** — visual polish, particle effects, animation, and screen feedback that make the game rewarding to play.

**Current State**:
- ✅ Mechanics: Perfect (all units balanced, no bugs)
- ✅ Balance: Perfect (50%/50% win rate)
- ❌ VFX: Minimal (basic particles only)
- ❌ Animation: None (units appear instantly)
- ❌ Feedback: Basic (no screen shake, minimal particles)

**Phase 3 Objectives**:
1. ✅ VFX Baseline Assessment (Cycle 8)
2. ✅ Particle Effect Enhancement (Cycle 9)
3. ✅ Animation Implementation (Cycle 10)
4. ✅ Screen Feedback & Polish (Cycle 11)

---

## Scope

### Included in Phase 3

| Area | Current | Target | Effort |
|------|---------|--------|--------|
| **Particle Effects** | 1 (explosion only) | 5+ (hit, death, AoE) | 4 hours |
| **Animation** | 0 (none) | 6+ (unit movement, death) | 8 hours |
| **Screen Feedback** | None | Screen shake, color flash | 3 hours |
| **Audio SFX** | None | 6+ sound effects | 4 hours (optional) |
| **UI Polish** | Basic | Smooth transitions, feedback | 2 hours |

**Total Estimate**: 21–25 hours (3–4 days aggressive schedule)

### Excluded from Phase 3

- ❌ AI Difficulty Tuning (deferred to Cycle 12–13)
- ❌ Networking/Multiplayer (future phase)
- ❌ Mobile Optimization (future phase)
- ❌ Tutorial/Onboarding (future phase)

---

## Cycle Breakdown

### Cycle 8: VFX & Animation Baseline Assessment
**Date**: 2026-07-11  
**Duration**: ~1 hour  
**Objective**: Audit current visual state; identify gaps; establish benchmark

#### Tasks

1. **Prefab Analysis**
   - ✅ List all particle systems (ExplosionEffect, etc.)
   - ✅ Check animation controllers (missing?)
   - ✅ Document current feedback mechanisms
   - **Deliverable**: VFX inventory checklist

2. **Gap Identification**
   - ✅ Archer: No hit animation
   - ✅ Bomber: Minimal explosion feedback
   - ✅ Knight: No melee swing animation
   - ✅ Arrow: Invisible flight, no trail
   - ✅ Screen: No shake on impact/explosion
   - **Deliverable**: Priority list (5–7 items)

3. **Benchmark Capture**
   - ✅ Record baseline video (5 games)
   - ✅ Identify "feels weak" moments
   - ✅ Note player feedback areas
   - **Deliverable**: Baseline report

#### Success Criteria
- [ ] All assets inventoried
- [ ] 5+ improvement areas identified
- [ ] Baseline video captured
- [ ] Priority ranking established (high/medium/low)

#### Files to Generate
- `castle-busters-cycle-8-vfx-assessment.md`

---

### Cycle 9: Particle Effects Enhancement
**Date**: 2026-07-11 (evening)  
**Duration**: ~2–3 hours  
**Objective**: Create/enhance particle systems for major events

#### Tasks

1. **Hit Feedback Particles** (15 min)
   - ✅ Create HitSpark prefab (small bursts on damage)
   - ✅ Implement for all unit types
   - ✅ Color-code by unit (Knight=gold, Archer=white, Bomber=red)
   - **Expected Impact**: Damage feedback clarity +70%

2. **Explosion Effects** (30 min)
   - ✅ Enhance ExplosionEffect (bigger, longer)
   - ✅ Add secondary particles (smoke trail, embers)
   - ✅ Adjust for Bomber vs castle destruction
   - **Expected Impact**: Visual weight +50%

3. **Death Animation Particles** (20 min)
   - ✅ Create DeathBurst prefab (quick dissipate effect)
   - ✅ Spawn on unit death
   - ✅ Vary by unit type
   - **Expected Impact**: Polish +40%

4. **Arrow Trail** (15 min)
   - ✅ Add line renderer or particle trail to Arrow
   - ✅ Fade on impact
   - ✅ Visible arc for trajectory feedback
   - **Expected Impact**: Clarity +60%

5. **Buff/Debuff Zones** (10 min)
   - ✅ Add subtle pulse particle to zone edges
   - ✅ Glow effect to indicate active zone
   - **Expected Impact**: Gimmick clarity +50%

#### Technical Details

**Particle Settings Template**:
```yaml
Hit Spark:
  Duration: 0.3 sec
  Size: 0.1 - 0.3 m
  Color: Unit-specific + fade
  Speed: Fast burst outward

Explosion:
  Duration: 0.8 sec (primary) + 1.5 sec (smoke)
  Size: 1–3 m (scaling)
  Color: Orange/yellow + smoke gray
  Speed: Medium burst + drift

Death Burst:
  Duration: 0.4 sec
  Size: 0.05 - 0.2 m
  Color: Unit color + fade
  Speed: Quick outward burst
```

#### Success Criteria
- [ ] 5 particle systems created/enhanced
- [ ] All major events have particle feedback
- [ ] No performance impact (<2% CPU)
- [ ] Visual clarity improved (subjective: +40%+)

#### Files to Modify
- `Assets/Prefabs/ExplosionEffect.prefab` (enhance)
- `Assets/Prefabs/HitSpark.prefab` (new)
- `Assets/Prefabs/DeathBurst.prefab` (new)
- `Assets/Prefabs/Arrow.prefab` (add trail)

#### Files to Generate
- `castle-busters-cycle-9-particle-enhancement.md`

---

### Cycle 10: Animation Implementation
**Date**: 2026-07-12 (morning)  
**Duration**: ~2–3 hours  
**Objective**: Add unit animations for movement and actions

#### Tasks

1. **Knight Animation Rig** (40 min)
   - ✅ Create simple Knight 3D model or sprite with frames
   - ✅ Idle pose
   - ✅ Walk/move animation (loop)
   - ✅ Attack pose (swing animation, 0.3 sec)
   - ✅ Death animation (collapse, 0.5 sec)
   - **Expected Impact**: Character appeal +60%

2. **Archer Animation Rig** (40 min)
   - ✅ Idle pose (at rest)
   - ✅ Aim animation (0.2 sec wind-up)
   - ✅ Fire animation (release, 0.1 sec)
   - ✅ Walk animation (loop)
   - ✅ Death animation (fall, 0.5 sec)
   - **Expected Impact**: Action clarity +70%

3. **Bomber Animation Rig** (40 min)
   - ✅ Idle animation
   - ✅ Waddle/walk animation (slower, comedic)
   - ✅ Fuse light animation (pulsing glow)
   - ✅ Explosion animation (instant, triggers particles)
   - ✅ Death animation (quick, triggers explosion)
   - **Expected Impact**: Character personality +80%

4. **Animation Integration** (20 min)
   - ✅ Link animations to unit state machine
   - ✅ Test transitions (idle → walk → attack → death)
   - ✅ Verify timing with particle systems
   - **Expected Impact**: Cohesion +50%

#### Technical Details

**Animation Controller Structure**:
```
AnimatorController (per unit)
├─ States:
│  ├─ Idle (default)
│  ├─ Walk (parameter: isMoving)
│  ├─ Attack (trigger: attack)
│  ├─ Hit (trigger: hit)
│  └─ Death (trigger: death)
└─ Parameters:
   ├─ isMoving (bool)
   ├─ attack (trigger)
   ├─ hit (trigger)
   └─ death (trigger)
```

#### Asset Requirements

**Per Unit**:
- 1 Animator Controller
- 5–6 Animation Clips (Idle, Walk, Attack, Hit, Death, optional: Confused)
- Sprite sheet or 3D model with skeleton

**Recommendation**: Use simple 2D sprites with sprite animation or simple 3D models.

#### Success Criteria
- [ ] 3 animation controllers created (Knight, Archer, Bomber)
- [ ] 15+ animation clips created
- [ ] All state transitions smooth
- [ ] No clipping/z-fighting
- [ ] Timing aligned with game events

#### Files to Modify
- `Assets/Prefabs/Knight.prefab` (add animator)
- `Assets/Prefabs/Archer.prefab` (add animator)
- `Assets/Prefabs/Bomber.prefab` (add animator)

#### Files to Generate
- `Assets/Animation/Knight.controller` (new)
- `Assets/Animation/Archer.controller` (new)
- `Assets/Animation/Bomber.controller` (new)
- `castle-busters-cycle-10-animation-implementation.md`

---

### Cycle 11: Screen Feedback & Final Polish
**Date**: 2026-07-12 (afternoon)  
**Duration**: ~1–2 hours  
**Objective**: Add screen-level effects and final visual tuning

#### Tasks

1. **Screen Shake on Impact** (20 min)
   - ✅ Implement camera shake script
   - ✅ Trigger on major events (explosion, unit death, castle hit)
   - ✅ Intensity scales with damage (light: 0.1s, heavy: 0.3s)
   - **Expected Impact**: Viscerality +60%

2. **Unit Hit Flash** (15 min)
   - ✅ Add sprite color flash on damage (white/red 0.1s)
   - ✅ Apply to Knight, Archer, Bomber, Castle
   - ✅ Intensity scales with damage
   - **Expected Impact**: Feedback clarity +50%

3. **Death Effect Sequence** (20 min)
   - ✅ Death flash (white burst, 0.1s)
   - ✅ Particle burst (DeathBurst)
   - ✅ Fade out (0.3s)
   - ✅ Clean removal from scene
   - **Expected Impact**: Closure/satisfaction +70%

4. **Audio SFX** (optional, 30 min)
   - ✅ Archer shot sfx (light pew sound)
   - ✅ Knight swing sfx (metal/wood strike)
   - ✅ Bomber fuse & explosion sfx (sizzle + boom)
   - ✅ Hit sound (light thud, volume = damage)
   - ✅ Death sound (quick burst, unit-specific)
   - **Expected Impact**: Immersion +80%

5. **UI Polish** (15 min)
   - ✅ Smooth button transitions
   - ✅ Feedback on unit selection
   - ✅ Health bar animations
   - **Expected Impact**: Professionalism +40%

#### Technical Details

**Screen Shake Script Template**:
```csharp
public class ScreenShake : MonoBehaviour {
    public void Shake(float intensity, float duration) {
        // Move camera position with Perlin noise
        // intensity: 0.1f = light, 0.3f = heavy
        // duration: shake time in seconds
    }
}
```

**Hit Flash Script Template**:
```csharp
public class HitFlash : MonoBehaviour {
    public void Flash(float intensity, float duration) {
        // Temporarily set sprite color to white
        // Lerp back to original color over duration
        // intensity: 0.5f = medium, 1.0f = maximum white
    }
}
```

#### Success Criteria
- [ ] Camera shake working on all impact events
- [ ] Hit flash visible on all units
- [ ] Death sequence smooth and satisfying
- [ ] (Optional) All SFX playing correctly
- [ ] No visual glitches or clipping
- [ ] Performance stable (60 FPS)

#### Files to Generate
- `Assets/Scripts/ScreenShake.cs` (new)
- `Assets/Scripts/HitFlash.cs` (new)
- `castle-busters-cycle-11-polish.md`

---

## Success Metrics & Quality Gates

### Cycle 8 (Assessment)
| Criterion | Target | Verification |
|-----------|--------|--------------|
| Assets inventoried | 100% | Checklist complete |
| Gaps identified | 5+ items | Priority list |
| Baseline captured | 5 games | Video + notes |

### Cycle 9 (Particles)
| Criterion | Target | Verification |
|-----------|--------|--------------|
| Particle systems | 5+ | Prefabs created |
| Major events covered | 100% | All events have particles |
| Performance impact | <2% CPU | Profiler check |
| Visual clarity | +40% | Subjective (playtest) |

### Cycle 10 (Animation)
| Criterion | Target | Verification |
|-----------|--------|--------------|
| Animation controllers | 3 | 1 per unit |
| Animation clips | 15+ | All states covered |
| Transitions | Smooth | No pops/glitches |
| Alignment | Perfect | Timing matches events |

### Cycle 11 (Polish)
| Criterion | Target | Verification |
|-----------|--------|--------------|
| Screen shake | All events | Impact tested |
| Hit flash | All units | Visual confirmed |
| Death sequence | Satisfying | Playtest feedback |
| Performance | 60 FPS | No drops |

---

## Timeline & Milestones

```
Phase 3 — Polish & VFX
│
├─ Cycle 8: VFX Assessment (1 hour)
│  └─ Output: castle-busters-cycle-8-vfx-assessment.md
│
├─ Cycle 9: Particle Enhancement (2–3 hours)
│  └─ Output: castle-busters-cycle-9-particle-enhancement.md + prefabs
│
├─ Cycle 10: Animation Implementation (2–3 hours)
│  └─ Output: castle-busters-cycle-10-animation-implementation.md + animators
│
├─ Cycle 11: Final Polish (1–2 hours)
│  └─ Output: castle-busters-cycle-11-polish.md + scripts
│
└─ Phase 3 Completion Report (30 min)
   └─ Output: castle-busters-phase-3-completion.md
```

**Total Duration**: 6–8 hours (1–2 days aggressive, 2–3 days comfortable)

---

## Risk Assessment

### Risk 1: Animation Complexity (MEDIUM)
**Issue**: Creating good animations may take longer than estimated  
**Mitigation**: Start with simple 2D sprite-based animations; use Unity's built-in sprite animation (not skeletal)  
**Fallback**: Skip animations, proceed to particles-only Phase 3

### Risk 2: Performance Impact (LOW)
**Issue**: Particles + animations might drop FPS below 60  
**Mitigation**: Profile each cycle; limit particle counts  
**Fallback**: Reduce particle density or effect duration

### Risk 3: Audio Sync Issues (MEDIUM)
**Issue**: Sound effects might not sync with animations  
**Mitigation**: Use audio delay/offset parameters; test multiple times  
**Fallback**: Skip SFX in Cycle 11 (optional anyway)

### Risk 4: Visual Clipping (LOW)
**Issue**: Particles or animations might appear behind/through units  
**Mitigation**: Establish z-order convention; test on all units  
**Fallback**: Adjust sorting order in sprite renderers

---

## Dependencies & Prerequisites

### Required for Phase 3
- ✅ Phase 2 complete (balance finalized)
- ✅ Zero known bugs (no crashes)
- ✅ Stable FPS baseline (60 locked)

### Assets to Source/Create
- Animation sprites or 3D models (create or source free asset)
- Audio clips (create or use royalty-free library)
- Particle texture assets (use Unity built-ins or create simple textures)

### Tools Required
- Unity Editor (standard)
- Sprite editor or animation software (optional for custom assets)
- Audio editor (optional for SFX creation)

---

## Recommended Asset Sources

### Free Animations & Models
- **Quaternius** (simple 3D models): https://quaternius.com
- **Kenney.nl** (game assets): https://kenney.nl
- **OpenGameArt**: https://opengameart.org
- **Unity Asset Store** (free): unity3d.com/asset-store

### Free Audio
- **Freesound.org**: https://freesound.org
- **Zapsplat**: https://www.zapsplat.com
- **OpenGameArt Audio**: https://opengameart.org (audio category)

### Particle Creation
- Use **Unity's built-in Shuriken particle system** (no external tools needed)

---

## Comparison: Phase 1 vs 2 vs 3

| Phase | Focus | Duration | Output | Impact |
|-------|-------|----------|--------|--------|
| **Phase 1** | Balance analysis | 1 hour | 5 reports | Data foundation |
| **Phase 2** | Balance implementation | 2 hours | 4 reports + code | Perfect 50%/50% |
| **Phase 3** | Visual polish | 6–8 hours | 4 reports + assets | Game feel |
| **Phase 4** | AI difficulty | 4–6 hours | 2 reports + code | Challenge |
| **Phase 5** | Final tuning | 2–4 hours | 1 report | Release candidate |

---

## Success Criteria: Phase 3 Complete

| Criterion | Status |
|-----------|--------|
| **All 4 cycles executed** | ⏳ Pending |
| **5+ particle systems created** | ⏳ Pending |
| **3 animation controllers created** | ⏳ Pending |
| **Screen shake implemented** | ⏳ Pending |
| **Hit flash feedback added** | ⏳ Pending |
| **60 FPS maintained** | ⏳ Pending |
| **Zero new bugs** | ⏳ Pending |
| **Playtest feedback positive** | ⏳ Pending |

**Phase 3 Success Rate**: Pending (0/8 in progress)

---

## Next Actions (Immediate)

1. **NOW** → Cycle 8 (VFX Assessment)
   - Audit current particle systems
   - Identify animation gaps
   - Record baseline video
   - Estimated: 1 hour

2. **THEN** → Cycle 9 (Particle Enhancement)
   - Create HitSpark, DeathBurst, Arrow Trail
   - Enhance ExplosionEffect
   - Test all major events
   - Estimated: 2–3 hours

3. **NEXT** → Cycle 10 (Animation)
   - Source or create unit animations
   - Build animation controllers
   - Integrate into prefabs
   - Estimated: 2–3 hours

4. **FINALLY** → Cycle 11 (Final Polish)
   - Screen shake script
   - Hit flash script
   - Optional: Audio SFX
   - Estimated: 1–2 hours

---

## Files to Generate (Phase 3)

```
wiki/reports/
├── castle-busters-cycle-8-vfx-assessment.md      (1 hour)
├── castle-busters-cycle-9-particle-enhancement.md (2–3 hours)
├── castle-busters-cycle-10-animation-implementation.md (2–3 hours)
├── castle-busters-cycle-11-polish.md             (1–2 hours)
└── castle-busters-phase-3-completion.md          (30 min)

Code/Assets to Create:
├── Assets/Scripts/ScreenShake.cs                 (new)
├── Assets/Scripts/HitFlash.cs                    (new)
├── Assets/Prefabs/HitSpark.prefab                (new)
├── Assets/Prefabs/DeathBurst.prefab              (new)
├── Assets/Animation/ (folder)                    (new)
│   ├── Knight.controller
│   ├── Archer.controller
│   └── Bomber.controller
└── Animation clips (15+)
```

---

**Phase 3 Roadmap Complete**  
**Ready to Start**: Cycle 8 — VFX Assessment  
**Estimated Duration**: 6–8 hours (1–2 days)  
**Expected Outcome**: Polished, visually satisfying game  
**Next Review**: After Cycle 11 completion

