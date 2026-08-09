using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    public enum EruptionStyle { Magma, Petal, Frost }

    /// <summary>
    /// Vertical-hazard gimmick ("화산폭발/꽃가루"): a ground vent that periodically erupts a
    /// tall column straight up through the shot lanes. Bodies caught in the column are
    /// shoved skyward (both styles) and scorched (magma only), so a live vent bends any
    /// low-arc volley crossing it — the disruption is spatial and readable, never random.
    /// Cycle timing is pure static math (PhaseAt/WrapCycleTime) so EditMode tests pin it.
    /// </summary>
    public class EruptionVentGimmick : MonoBehaviour
    {
        public EruptionStyle style = EruptionStyle.Magma;

        [Header("Presentation")]
        public float targetWorldSize = 2.4f;

        [Header("Cycle (seconds)")]
        public float dormantDuration = 6.5f;
        public float warningDuration = 1.8f;
        public float eruptDuration = 2.2f;
        // Desync offset so the two vents never fire in the same beat.
        public float phaseOffset = 0f;

        [Header("Column")]
        public float columnWidth = 1.9f;
        public float columnHeight = 7.5f;
        // Upward acceleration applied to dynamic bodies inside the column. Deliberately
        // below gravity*3 so a shot is bent, not teleported — counterplay is "arc higher".
        public float liftAcceleration = 24f;
        // Terminal upward speed the vent can push a body to; prevents orbit launches.
        public float maxLiftSpeed = 8.5f;
        public float tickDamage = 4f;          // magma only
        public float damageTickInterval = 0.5f;
        // Frost-only: horizontal knockback + slow instead of vertical lift + damage.
        public float frostKnockSpeed = 7f;      // horizontal push speed applied to bodies in a Frost column
        public float frostSlowMultiplier = 0.55f; // ApplyDebuff multiplier (<1 = slower)
        public float frostSlowDuration = 2.5f;

        public enum Phase { Dormant, Warning, Erupting }

        private SpriteRenderer sr;
        private float bornTime;
        private Phase lastPhase = Phase.Dormant;
        private float nextColumnFxTime;
        private float nextDamageTickTime;
        private ParticleSystem columnParticles;
        private Color identityColor;
        // Tracks which units already got the Frost slow this eruption phase so FixedUpdate
        // (which runs every physics tick while Erupting) applies the debuff exactly once.
        private readonly HashSet<int> frostSlowedThisEruption = new HashSet<int>();

        /// <summary>Wraps elapsed time (plus offset) onto the cycle; never negative.</summary>
        public static float WrapCycleTime(float elapsed, float offset, float cycleTotal)
        {
            if (cycleTotal <= 0f) return 0f;
            float t = (elapsed + offset) % cycleTotal;
            return t < 0f ? t + cycleTotal : t;
        }

        /// <summary>Pure phase schedule: dormant → warning → erupting → (wrap).</summary>
        public static Phase PhaseAt(float cycleTime, float dormant, float warning, float erupt)
        {
            if (cycleTime < dormant) return Phase.Dormant;
            if (cycleTime < dormant + warning) return Phase.Warning;
            return Phase.Erupting;
        }

        /// <summary>Column AABB used for the physics sweep, anchored on the vent mouth.</summary>
        public static Rect ColumnRect(Vector2 ventPos, float width, float height)
        {
            return new Rect(ventPos.x - width * 0.5f, ventPos.y, width, height);
        }

        /// <summary>
        /// Velocity change for one physics step, honoring the lift ceiling. Returns 0 once
        /// the body already rises at maxLiftSpeed so the column can never slingshot.
        /// </summary>
        public static float LiftDeltaV(float currentVy, float liftAccel, float maxSpeed, float dt)
        {
            if (currentVy >= maxSpeed) return 0f;
            return Mathf.Min(liftAccel * dt, maxSpeed - currentVy);
        }

        public float CycleTotal => dormantDuration + warningDuration + eruptDuration;

        public Phase CurrentPhase => PhaseAt(
            WrapCycleTime(Time.time - bornTime, phaseOffset, CycleTotal),
            dormantDuration, warningDuration, eruptDuration);

        private void Awake()
        {
            bornTime = Time.time;
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3; // above ground tiles, below units/ram
            ApplyStyleVisuals();
        }

        // Spawners AddComponent() first and assign `style` right after, so Awake always
        // sees the default (Magma) — the petal vent rendered magma art (same trap as
        // MovingGimmick's presentation scale). Start runs after field assignment;
        // re-applying is idempotent because everything derives from `style`.
        private void Start()
        {
            ApplyStyleVisuals();
        }

        private void ApplyStyleVisuals()
        {
            string artKey;
            switch (style)
            {
                case EruptionStyle.Magma:
                    identityColor = new Color(1f, 0.5f, 0.2f, 1f);
                    artKey = GimmickSpriteLibrary.VentMagma;
                    break;
                case EruptionStyle.Frost:
                    identityColor = new Color(0.65f, 0.85f, 1f, 1f);
                    artKey = GameManager.Instance != null && GameManager.Instance.currentStage == StageId.Stage3
                        ? GimmickSpriteLibrary.Stage3FrostVent
                        : GimmickSpriteLibrary.VentFrost;
                    break;
                default: // Petal
                    identityColor = new Color(1f, 0.62f, 0.85f, 1f);
                    artKey = GimmickSpriteLibrary.VentPetal;
                    break;
            }

            if (!GimmickSpriteLibrary.TryApply(sr, artKey, Color.white))
            {
                // Procedural crater-dome fallback: dedicated art is generated separately and
                // may lag behind the code drop; the hazard must stay VISIBLE either way.
                sr.sprite = GetFallbackVentSprite();
                sr.color = identityColor;
            }
            ApplyPresentationScale();
        }

        private void ApplyPresentationScale()
        {
            if (sr == null || sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            float maxNative = Mathf.Max(native.x, native.y);
            if (maxNative <= 0.0001f) return;
            float scale = targetWorldSize / maxNative;
            transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static Sprite cachedFallbackVent;

        private static Sprite GetFallbackVentSprite()
        {
            if (cachedFallbackVent != null) return cachedFallbackVent;

            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var clear = new Color(1f, 1f, 1f, 0f);
            float cx = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Low dome with a dark mouth on top: |pos| inside a squashed ellipse.
                    float nx = (x - cx) / (size * 0.5f);
                    float ny = y / (size * 0.62f);
                    bool inDome = y < size * 0.6f && (nx * nx + ny * ny) <= 1f;
                    bool inMouth = y >= size * 0.42f && y < size * 0.6f && Mathf.Abs(x - cx) < size * 0.18f;
                    tex.SetPixel(x, y, inMouth ? new Color(0.12f, 0.08f, 0.08f, 1f)
                        : inDome ? Color.white : clear);
                }
            }
            tex.Apply(false, true);
            cachedFallbackVent = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), size);
            cachedFallbackVent.name = "FallbackVentDome";
            return cachedFallbackVent;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;
            Phase phase = CurrentPhase;

            if (phase != lastPhase)
            {
                if (phase == Phase.Warning) EnterWarning();
                else if (phase == Phase.Erupting) EnterErupting();
                else ExitErupting();
                lastPhase = phase;
            }

            if (phase == Phase.Warning)
            {
                // Telegraph: the vent mouth throbs in its identity color.
                float throb = 0.5f + 0.5f * Mathf.Sin(Time.time * 14f);
                sr.color = Color.Lerp(Color.white, identityColor, 0.35f + 0.45f * throb);
            }
            else if (phase == Phase.Erupting && Time.time >= nextColumnFxTime)
            {
                SpawnColumnFrameFx();
                nextColumnFxTime = Time.time + 0.4f;
            }
        }

        private void EnterWarning()
        {
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.6f,
                style == EruptionStyle.Magma ? "RUMBLE..." :
                style == EruptionStyle.Frost ? "FROST SURGE... / 서리 몰아침..." : "BLOOM...",
                identityColor, 2.0f, warningDuration);
            FrameAnimEffect.Spawn(EffectSpriteLibrary.Dust,
                transform.position + Vector3.up * 0.35f, 1.4f,
                new Color(0.8f, 0.74f, 0.66f, 0.9f), 10f, 30);
        }

        private void EnterErupting()
        {
            sr.color = Color.white;
            nextColumnFxTime = 0f;
            nextDamageTickTime = Time.time + damageTickInterval;
            frostSlowedThisEruption.Clear();

            GameFeelVfx.SpawnShockwaveRing(transform.position, identityColor, 1.8f, 0.4f);
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.3f, 0.15f);
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 1.2f,
                style == EruptionStyle.Magma ? "ERUPTION!" :
                style == EruptionStyle.Frost ? "FROST BURST! / 서릿발 돌풍!" : "PETAL BURST!",
                identityColor, 2.6f, 0.8f);

            columnParticles = BuildColumnParticles();
        }

        private void ExitErupting()
        {
            sr.color = Color.white;
            if (columnParticles != null)
            {
                columnParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                Destroy(columnParticles.gameObject, 1.6f);
                columnParticles = null;
            }
        }

        private void SpawnColumnFrameFx()
        {
            string key = style == EruptionStyle.Magma
                ? EffectSpriteLibrary.Eruption
                : EffectSpriteLibrary.Petals;
            // Frost reuses the petal frame strip (no dedicated Frost frame art yet); the
            // ice-blue tint below is what reads it as frost instead of petals.
            Color columnFxTint = style == EruptionStyle.Frost
                ? new Color(0.75f, 0.92f, 1f, 0.95f)
                : Color.white;
            // Column strips are portrait art: FrameAnimEffect scales the tallest dimension
            // to worldSize, so the column visually fills the physics sweep band.
            FrameAnimEffect.Spawn(key,
                transform.position + Vector3.up * (columnHeight * 0.5f),
                columnHeight, columnFxTint, 14f, 33);
        }

        private ParticleSystem BuildColumnParticles()
        {
            var go = new GameObject($"VentColumn_{style}");
            go.transform.position = transform.position + Vector3.up * 0.3f;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = eruptDuration;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.1f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(6.5f, 11f);
            main.startSize = style == EruptionStyle.Magma
                ? new ParticleSystem.MinMaxCurve(0.22f, 0.5f)
                : new ParticleSystem.MinMaxCurve(0.28f, 0.55f);
            main.startColor = style == EruptionStyle.Magma
                ? new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f, 1f), new Color(1f, 0.35f, 0.1f, 0.9f))
                : style == EruptionStyle.Frost
                    ? new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.95f, 1f, 1f), new Color(0.55f, 0.75f, 1f, 0.85f))
                    : new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.9f, 1f), new Color(1f, 0.55f, 0.8f, 0.9f));
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = style == EruptionStyle.Magma ? 0.85f : 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = style == EruptionStyle.Magma ? 46f : 60f;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 9f;
            shape.radius = columnWidth * 0.3f;
            shape.rotation = new Vector3(-90f, 0f, 0f); // cone fires straight up

            var rot = ps.rotationOverLifetime;
            rot.enabled = style == EruptionStyle.Petal || style == EruptionStyle.Frost;
            rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f);

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 34;
            var particleSprite = EffectSpriteLibrary.LoadParticleSprite(style == EruptionStyle.Magma
                ? EffectSpriteLibrary.ParticleEmber
                : EffectSpriteLibrary.ParticlePetal);
            if (particleSprite != null)
            {
                var sheet = ps.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                sheet.AddSprite(particleSprite);
                renderer.sharedMaterial = GameFeelVfx.GetParticleMaterial(particleSprite.texture as Texture2D);
            }
            else
            {
                renderer.sharedMaterial = GameFeelVfx.GetParticleMaterial();
            }

            ps.Play();
            return ps;
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying || CurrentPhase != Phase.Erupting) return;

            Rect column = ColumnRect(transform.position, columnWidth, columnHeight);
            var hits = Physics2D.OverlapAreaAll(column.min, column.max);
            bool damageTick = style == EruptionStyle.Magma && Time.time >= nextDamageTickTime;
            if (damageTick) nextDamageTickTime = Time.time + damageTickInterval;

            foreach (var hit in hits)
            {
                if (hit == null || hit.gameObject == gameObject) continue;
                var rb = hit.attachedRigidbody;

                if (style == EruptionStyle.Frost)
                {
                    // Horizontal gust: shove sideways away from the vent instead of lifting;
                    // slow (not damage) any unit caught in it — counterplay is "stay out of
                    // the frost lane" rather than "don't get launched".
                    if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                    {
                        float dir = Mathf.Sign(hit.transform.position.x - transform.position.x);
                        if (Mathf.Approximately(dir, 0f)) dir = 1f;
                        rb.velocity = new Vector2(dir * frostKnockSpeed, rb.velocity.y);
                    }
                    if (hit.TryGetComponent<UnitController>(out var frostUnit) && frostSlowedThisEruption.Add(frostUnit.GetInstanceID()))
                    {
                        frostUnit.ApplyDebuff(frostSlowMultiplier, frostSlowDuration);
                    }
                    continue;
                }

                if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic)
                {
                    float dv = LiftDeltaV(rb.velocity.y, liftAcceleration, maxLiftSpeed, Time.fixedDeltaTime);
                    if (dv > 0f) rb.velocity += new Vector2(Random.Range(-0.35f, 0.35f) * Time.fixedDeltaTime * 10f, dv);
                }
                if (damageTick && hit.TryGetComponent<UnitController>(out var unit))
                {
                    unit.TakeDamage(tickDamage);
                    GameFeelVfx.SpawnImpactBurst(hit.transform.position, identityColor, 0.5f);
                }
            }
        }
    }
}
