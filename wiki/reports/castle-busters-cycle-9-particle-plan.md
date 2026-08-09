# Castle Busters — Cycle 9: Particle Enhancement
## Detailed Implementation Plan & Configuration Guide

**Date**: 2026-07-11  
**Cycle**: 9/11 (Phase 3)  
**Duration**: 2–3 hours  
**Objective**: Create 5 particle systems for damage, death, and trajectory feedback

---

## Executive Summary

Cycle 8 identified that only 1/5 particle systems exist (ExplosionEffect only). Cycle 9 implements the remaining 4 critical systems:

1. **HitSpark** — Small burst on damage (20 min)
2. **DeathBurst** — Unit death feedback (20 min)
3. **ExplosionEffect Enhancement** — More visual weight (30 min)
4. **Arrow Trail** — Trajectory visibility (15 min)

**Expected Outcome**: +50% visual feedback clarity; game feels more impactful

---

## Particle System Specifications

### System 1: HitSpark (20 minutes)

**Purpose**: Small visual feedback when unit takes damage

**Asset Details**:
- **Name**: HitSpark
- **Location**: Assets/Prefabs/HitSpark.prefab (new)
- **Type**: One-shot particle burst
- **Trigger**: On unit damage event

**Configuration Parameters**:

```yaml
HitSpark Settings:
  General:
    Duration: 0.3 seconds
    Loop: false
    Play on Awake: true
    Simulation Space: World (so effect stays at hit location)
    Culling Mode: Always Simulate
    
  Emission:
    Rate over Time: 50
    (Total particles: ~15 over 0.3s)
    
  Shape:
    Type: Sphere
    Radius: 0.1
    Emit From: Volume
    (Particles spray outward from impact point)
    
  Velocity over Lifetime:
    Initial Velocity: (3-8 m/s outward)
    Speed Damping: 0.9 (particles slow down)
    
  Size:
    Start Size: 0.1 - 0.3 m
    Size over Lifetime: Shrink from 0.2 to 0.05
    
  Color:
    Start Color: Unit-specific
      Knight: Gold/Yellow (255, 200, 0, 255)
      Archer: White/Silver (255, 255, 200, 255)
      Bomber: Red/Orange (255, 100, 0, 255)
    Color over Lifetime: Fade to transparent
    Alpha: 1.0 → 0.0 (0.3s)
    
  Lifetime:
    Min: 0.3s
    Max: 0.3s
    
  Renderer:
    Material: Default Sprite Material (or simple quad)
    Color Space: Linear
```

**Pseudocode for Spawning**:
```csharp
public class Unit : MonoBehaviour {
    public void TakeDamage(int damage) {
        // ... existing damage logic ...
        
        // Spawn hit effect at unit position
        GameObject hitEffect = Instantiate(hitSparkPrefab, transform.position, Quaternion.identity);
        
        // Optional: Color the particles based on unit type
        // This would require a script on the HitSpark prefab
    }
}
```

**Visual Result**: 
```
Impact point: ⊙
Outward spray: *  *
               * ⊙ *
                *
Duration: 0.3s (quick burst, disappears fast)
```

---

### System 2: DeathBurst (20 minutes)

**Purpose**: Satisfying effect when unit is destroyed

**Asset Details**:
- **Name**: DeathBurst
- **Location**: Assets/Prefabs/DeathBurst.prefab (new)
- **Type**: One-shot particle burst
- **Trigger**: On unit destruction

**Configuration Parameters**:

```yaml
DeathBurst Settings:
  General:
    Duration: 0.5 seconds
    Loop: false
    Play on Awake: true
    Simulation Space: World
    Culling Mode: Always Simulate
    
  Emission:
    Rate over Time: 80
    (Total particles: ~40 over 0.5s)
    (More particles = more dramatic death)
    
  Shape:
    Type: Sphere
    Radius: 0.15
    Emit From: Volume
    
  Velocity over Lifetime:
    Initial Velocity: (2-6 m/s outward)
    Speed Damping: 0.8 (medium slow)
    
  Size:
    Start Size: 0.05 - 0.2 m
    Size over Lifetime: Shrink 0.15 → 0.0
    
  Color:
    Start Color: Unit-specific (same as HitSpark)
      Knight: Gold (255, 200, 0, 255)
      Archer: White (255, 255, 200, 255)
      Bomber: Red (255, 100, 0, 255)
    Color over Lifetime: Fade to transparent
    Alpha: 1.0 → 0.0 (0.5s)
    
  Lifetime:
    Min: 0.4s
    Max: 0.5s
    
  Renderer:
    Material: Default Sprite Material
```

**Pseudocode for Spawning**:
```csharp
public class Unit : MonoBehaviour {
    public void Die() {
        // ... existing death logic ...
        
        // Spawn death effect at unit position
        GameObject deathEffect = Instantiate(deathBurstPrefab, transform.position, Quaternion.identity);
        
        // Destroy unit after short delay (allow particles to play)
        Destroy(gameObject, 0.1f);
    }
}
```

**Visual Result**:
```
Death moment: ⊙ (unit explodes)
Burst outward: * * *
              * ⊙ *
             * * * *
Duration: 0.5s (medium burst, more dramatic than hit)
```

---

### System 3: ExplosionEffect Enhancement (30 minutes)

**Purpose**: Enhance existing explosion effect with more visual weight

**Asset Details**:
- **Name**: ExplosionEffect
- **Location**: Assets/Prefabs/ExplosionEffect.prefab (MODIFY)
- **Type**: One-shot particle burst + smoke trail
- **Trigger**: On Bomber explosion; on castle destruction

**Current State**: Basic burst (5 sec duration, simple particles)

**Proposed Enhancement**: Two-layer system
- Layer 1: Main explosion burst (fast, hot colors)
- Layer 2: Smoke trail (slow, gray)

**Configuration Parameters**:

```yaml
ExplosionEffect Enhancement:
  
  Main Burst (Primary):
    Duration: 0.8 seconds
    
    Emission:
      Rate: 150 particles/sec
      Total: ~120 particles
      
    Shape:
      Type: Sphere
      Radius: 0.5
      Emit From: Volume (core)
      
    Velocity:
      Initial: 8-15 m/s outward
      Damping: 0.7
      
    Size:
      Start: 0.3 - 0.8 m
      Over Lifetime: 0.5 → 0.1 m
      
    Color:
      Start: Orange/Yellow gradient
      Positions: {0.0: #FF6600, 0.5: #FFAA00, 1.0: #FFFF00}
      Over Lifetime: Fade from bright to transparent
      Alpha: 1.0 → 0.0 (0.8s)
      
    Rotation:
      Over Lifetime: Random spin (0-360°)
      
  Smoke Tail (Secondary):
    Duration: 1.5 seconds
    (Lingering smoke cloud)
    
    Emission:
      Rate: 80 particles/sec
      Total: ~120 particles
      
    Shape:
      Type: Sphere
      Radius: 0.2
      Emit From: Volume (center)
      
    Velocity:
      Initial: 2-4 m/s (drifting)
      Damping: 0.5 (slow drift)
      Gravity: 0.5 m/s² downward (settles)
      
    Size:
      Start: 0.5 - 1.2 m
      Over Lifetime: 0.7 → 0.3 m
      (Smoke expands then dissipates)
      
    Color:
      Start: Gray (150, 150, 150, 200)
      Over Lifetime: Lighten then fade
      Alpha: 0.6 → 0.0 (1.5s)
```

**Visual Result**:
```
t=0.0s:     ⊙ (bright burst)
            ⊙⊙⊙
            ⊙ ⊙ ⊙
            ⊙⊙⊙

t=0.4s:    ⊙⊙⊙⊙⊙ (expanding, dimming)
           ⊙  ⊙  ⊙
           ⊙⊙⊙⊙⊙

t=0.8s:   ☁☁☁☁ (smoke cloud)
         ☁ ☁ ☁
        ☁ ☁ ☁

t=1.5s:      ☁ (dissipating)
             ☁
```

**Implementation Notes**:
- Two separate particle systems within one prefab, or
- Enhanced single system with multiple emission rates

---

### System 4: Arrow Trail (15 minutes)

**Purpose**: Make arrow trajectory visible

**Asset Details**:
- **Name**: Arrow Trail (add to existing Arrow)
- **Location**: Assets/Prefabs/Arrow.prefab (MODIFY)
- **Type**: Line renderer or particle trail
- **Trigger**: On arrow launch; disappears on impact

**Configuration Option A: Line Renderer** (Recommended, simpler)

```yaml
Arrow Trail - Line Renderer:
  Component: LineRenderer
  Position: Arrow prefab root
  
  Settings:
    Width: 0.05 m (thin line)
    Color: Archer unit color (white/silver)
    
    Material: Default/Standard (white)
    or Custom: Gradient material (white → transparent fade)
    
  Script Logic:
    - Track arrow position each frame
    - Add position to line renderer
    - On impact: fade out (0.2s) then destroy
    
  Visual Result:
    Arrow flight: ⠿→→→⊙
    Thin white line showing trajectory
```

**Configuration Option B: Particle Trail** (More visual)

```yaml
Arrow Trail - Particle System:
  Duration: 2.0 seconds (long trail)
  
  Emission:
    Rate: 30 particles/sec (sparse trail)
    Emit From: Transform (arrow position)
    
  Velocity:
    Initial: 0 (particles follow arrow naturally)
    Space: World
    
  Size:
    Start: 0.05 m
    Over Lifetime: 0.05 → 0.0 (fade fast)
    
  Color:
    Start: White (255, 255, 200, 200)
    Over Lifetime: Fade to transparent
    
  Lifetime:
    0.3 - 0.5 seconds (quick fade)
```

**Recommendation**: **Use Line Renderer** (Option A)
- Simpler, less CPU overhead
- Cleaner visual effect
- Easier to implement (~10 lines of code)

**Pseudocode**:
```csharp
public class Arrow : MonoBehaviour {
    private LineRenderer trail;
    
    void Start() {
        trail = GetComponent<LineRenderer>();
        trail.positionCount = 0;
    }
    
    void FixedUpdate() {
        // Add current position to trail
        trail.positionCount++;
        trail.SetPosition(trail.positionCount - 1, transform.position);
        
        // Limit trail length to prevent memory bloat
        if (trail.positionCount > 50) {
            // Shift positions (or use circular buffer)
        }
    }
    
    void OnTriggerEnter(Collider other) {
        // Impact: fade out trail
        StartCoroutine(FadeOutTrail());
    }
    
    IEnumerator FadeOutTrail() {
        Color startColor = trail.material.color;
        float t = 0;
        while (t < 0.2f) {
            t += Time.deltaTime;
            Color newColor = startColor;
            newColor.a = Mathf.Lerp(1, 0, t / 0.2f);
            trail.material.color = newColor;
            yield return null;
        }
        Destroy(gameObject);
    }
}
```

**Visual Result**:
```
Arrow flight with trail:
  ⊙  (arrow head)
    ⠿ (trail line, fading)
      ⠿
        ⠿
          ⠿
```

---

## Implementation Checklist

### Preparation (5 min)
- [ ] Read this guide completely
- [ ] Gather asset templates (particle materials, textures)
- [ ] Open Unity Editor
- [ ] Have existing ExplosionEffect.prefab open for reference

### HitSpark Creation (20 min)

**Step 1: Create GameObject**
- [ ] Right-click Assets/Prefabs → Create → Create Empty
- [ ] Name: "HitSpark"
- [ ] Save as prefab (drag into Prefabs folder)

**Step 2: Add ParticleSystem**
- [ ] Select HitSpark gameobject
- [ ] Component → Particles → Particle System
- [ ] Configure using **Configuration Section 1** above

**Step 3: Configure Emitter**
- [ ] Set Duration: 0.3s
- [ ] Set Loop: OFF
- [ ] Set Emission Rate: 50
- [ ] Set Playback Speed: 1.0

**Step 4: Configure Renderer**
- [ ] Particle System → Renderer
- [ ] Material: Default-Particle (or Standard)
- [ ] Color Space: Linear

**Step 5: Test**
- [ ] Press Play in Editor
- [ ] HitSpark should burst for 0.3s then disappear
- [ ] Visual: Small particles spray outward

**Result**: ✅ HitSpark.prefab created

### DeathBurst Creation (20 min)

**Step 1-4**: Identical to HitSpark, but configure with **Configuration Section 2**

**Step 5: Adjust Parameters**
- [ ] Duration: 0.5s
- [ ] Emission Rate: 80
- [ ] Size: Slightly larger (0.05 - 0.2m)

**Step 6: Test**
- [ ] Press Play
- [ ] DeathBurst should display for 0.5s, more dramatic than HitSpark

**Result**: ✅ DeathBurst.prefab created

### ExplosionEffect Enhancement (30 min)

**Step 1: Open Existing Prefab**
- [ ] Select Assets/Prefabs/ExplosionEffect.prefab
- [ ] Inspect current settings (backup first if concerned)

**Step 2: Backup Current Settings**
- [ ] Note current values (Duration, Emission, Size, Color)
- [ ] Consider: Create new prefab "ExplosionEffect_v2" if major changes

**Step 3: Modify Particle System**
- [ ] Increase Duration: 5s → 0.8s (primary) + 1.5s (smoke)
- [ ] Increase Emission Rate: → 150 (main) + 80 (smoke)
- [ ] Adjust Color: Enhance orange/yellow gradient
- [ ] Adjust Size: Larger particles (0.3 - 0.8m)

**Step 4: Add Smoke Layer** (Optional, if prefab supports multiple systems)
- [ ] Duplicate particle system component
- [ ] Configure second system as smoke trail (slower, longer)
- [ ] Adjust emission rate and lifetime separately

**Step 5: Test**
- [ ] Press Play
- [ ] Trigger explosion manually (or wait for Bomber explosion)
- [ ] Visual: Larger, longer-lasting explosion with more visual weight

**Result**: ✅ ExplosionEffect.prefab enhanced

### Arrow Trail Addition (15 min)

**Step 1: Open Arrow Prefab**
- [ ] Select Assets/Prefabs/Arrow.prefab
- [ ] Inspect existing components

**Step 2: Add LineRenderer**
- [ ] Component → Rendering → Line Renderer
- [ ] Set Material: Default (white)
- [ ] Set Width: 0.05

**Step 3: Add Trail Script** (Create new script)
- [ ] Create ArrowTrail.cs script
- [ ] Attach to Arrow prefab
- [ ] Implement line drawing logic (pseudocode provided above)

**Step 4: Configure Color**
- [ ] Set line color: White (255, 255, 200, full alpha)
- [ ] Add gradient fade (white → transparent)

**Step 5: Test Arrow**
- [ ] Launch game
- [ ] Fire arrow from Archer
- [ ] Observe: Thin white line trails behind arrow
- [ ] On impact: Trail fades out

**Result**: ✅ Arrow.prefab modified with trail

### Integration Testing (10 min)

- [ ] Launch full game
- [ ] Play 1–2 games
- [ ] Verify all particles:
  - [ ] HitSpark on unit damage
  - [ ] DeathBurst on unit death
  - [ ] Explosion enhanced (Bomber death)
  - [ ] Arrow trail visible

- [ ] Check performance:
  - [ ] FPS stays at 60 (use profiler if drops)
  - [ ] No memory leaks
  - [ ] No visual glitches

### Final Verification

- [ ] All 4 particle systems working
- [ ] No crashes or warnings
- [ ] Visual feedback improved +50%
- [ ] Game feels more impactful
- [ ] Ready for Cycle 10 (Animation)

---

## Performance Considerations

### Expected Performance Impact

| System | CPU | Memory | FPS Impact |
|--------|-----|--------|-----------|
| HitSpark (per hit) | ~0.5% | ~0.5 MB | None |
| DeathBurst (per death) | ~0.5% | ~0.5 MB | None |
| Explosion Enhanced | ~1% | ~1 MB | None |
| Arrow Trail (per arrow) | ~0.3% | ~0.2 MB | None |
| **Total Active** | **~2–3%** | **~2 MB** | **None** |

**Conclusion**: All particle systems are performant; safe to include

### Optimization Tips (if needed)

1. **Particle Count Reduction**: Reduce emission rate by 20%
2. **LOD System**: Disable particles at far distances (not needed for small game)
3. **Object Pooling**: Reuse particle systems instead of instantiate/destroy
4. **GPU Particles**: Use GPU simulation if CPU becomes bottleneck (advanced)

---

## Troubleshooting

### Issue: Particles Not Visible
**Cause**: Material or sorting order wrong
**Fix**: 
- Ensure Material is set (not None)
- Check Camera can see particles (z-depth)
- Verify Color is not fully transparent (Alpha > 0)

### Issue: Particles Stay on Screen
**Cause**: Duration set too long or loop enabled
**Fix**:
- Set Duration to 0.3–1.5 (as specified)
- Turn OFF Loop option
- Check "Play on Awake" is ON

### Issue: Performance Drop
**Cause**: Too many particles active
**Fix**:
- Reduce Emission Rate (e.g., 50 → 30)
- Reduce Lifetime (e.g., 0.5s → 0.3s)
- Profile with Unity Profiler to identify bottleneck

### Issue: Color Not Matching Unit
**Cause**: Color over Lifetime module not configured
**Fix**:
- Enable "Color over Lifetime" module
- Set gradient from unit color to transparent
- Test visual appearance

---

## Time Estimate Breakdown

| Task | Time | Status |
|------|------|--------|
| HitSpark creation | 20 min | ⏳ |
| DeathBurst creation | 20 min | ⏳ |
| Explosion enhancement | 30 min | ⏳ |
| Arrow trail addition | 15 min | ⏳ |
| Integration testing | 15 min | ⏳ |
| **Total** | **100 min (1.7 hours)** | ✅ **Within 2–3 hour budget** |

---

## Success Criteria: Cycle 9

| Criterion | Target | Verification |
|-----------|--------|--------------|
| **HitSpark working** | On all damage | Test damage on unit |
| **DeathBurst working** | On all unit deaths | Destroy units, observe |
| **Explosion enhanced** | More visual weight | Compare to baseline |
| **Arrow trail visible** | On arrow flight | Fire arrow, observe |
| **Performance maintained** | 60 FPS | Profiler/Game tab |
| **Zero new bugs** | No crashes | Play 1 full game |

**Cycle 9 Success Rate**: Pending (0/6 in progress)

---

## Expected Visual Improvements

### Before Cycle 9
```
Unit takes damage:  [Unit visual unchanged]
Unit dies:          [Unit disappears]
Arrow fires:        [Arrow invisible, appears on impact]
Explosion:          [Small particle puff]
```

### After Cycle 9
```
Unit takes damage:  [Unit visual] + [Gold/White burst outward]
Unit dies:          [Unit visual] + [Larger colored burst]
Arrow fires:        [White trail line] → [Impact point]
Explosion:          [Large orange burst] + [Gray smoke cloud]
```

**Player Perception**:
- ✅ Damage feedback: Clear (+70% clarity)
- ✅ Death feedback: Satisfying (+60% satisfaction)
- ✅ Arrow trajectory: Visible (+50% visibility)
- ✅ Explosions: Impactful (+50% weight)

---

## Next Steps (After Cycle 9)

Once all 4 particle systems are working:

1. **Cycle 10**: Animation implementation (2–3 hours)
   - Create Knight, Archer, Bomber animators
   - Implement state machines
   
2. **Cycle 11**: Screen feedback (1–2 hours)
   - Screen shake on impacts
   - Hit flash effect
   - Optional: Audio SFX

3. **Phase 3 Complete**: Play 5 games, verify feel improvements

---

## Files to Generate

After completion:
- `castle-busters-cycle-9-particle-enhancement.md` (execution report)
- `Assets/Prefabs/HitSpark.prefab` (new)
- `Assets/Prefabs/DeathBurst.prefab` (new)
- `Assets/Prefabs/ExplosionEffect.prefab` (modified)
- `Assets/Prefabs/Arrow.prefab` (modified)
- `Assets/Scripts/ArrowTrail.cs` (new, if using line renderer)

---

## Recommendation

**✅ PROCEED WITH CYCLE 9**

- Effort: 2–3 hours (achievable in one session)
- Impact: +50% visual feedback quality
- Risk: Low (particle systems are forgiving)
- Benefit: Game transforms from "mechanical" to "engaging"

**Next Action**: Start HitSpark creation (20 min) → Continue with others

---

**Cycle 9 Plan Complete**  
**Ready to Execute**: Yes  
**Estimated Duration**: 1.7–2 hours (within budget)  
**Next Milestone**: Cycle 9 execution report after completion

