using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    public enum EventGateEffectType { Multiply, Reduce, PowerUp, PowerDown }

    /// <summary>
    /// One-shot aerial gate for run variety: duplicates, reduces, boosts, or weakens launched units/arrows.
    /// Processed objects are remembered so slow projectiles cannot repeatedly trigger the same gate.
    /// </summary>
    public class EventGateGimmick : MonoBehaviour
    {
        [Header("Gate Settings")]
        public EventGateEffectType effectType = EventGateEffectType.PowerUp;
        public float targetWorldSize = 2.4f;
        public int cloneCount = 1;
        public int maxTotalClones = 3;
        public float velocityMultiplier = 1.35f;
        public float damageSpeedMultiplier = 1.35f;
        public float effectDuration = 4f;
        public float reduceVelocityMultiplier = 0.55f;
        public float reduceDamageSpeedMultiplier = 0.65f;
        public bool destroyOnReduce = false;

        [Header("Presentation")]
        public float pulseSpeed = 3.5f;
        public float pulseAmount = 0.08f;

        private readonly HashSet<int> processedInstanceIds = new HashSet<int>();
        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;
        private int spawnedCloneCount;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.sortingOrder = 2;

            var col = GetComponent<Collider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            baseScale = transform.localScale;
        }

        // Visuals live in Start, not Awake: CreateEventGate() adds the component and assigns
        // effectType afterwards, so Awake would always tint with the default (PowerUp) hue.
        private void Start()
        {
            ApplyVisuals();
        }

        public void ApplyVisuals()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return;

            // Dedicated stone-arch portal art, tinted per effect type; the old translucent
            // block tint stays as fallback when the asset is missing.
            if (!GimmickSpriteLibrary.TryApply(spriteRenderer, GimmickSpriteLibrary.Gate, GetGateArtTint(effectType)))
            {
                spriteRenderer.color = GetGateColor(effectType);
            }

            ApplyPresentationScale();
            // Animated portal swirl (4-frame loop); tint set above survives the frame swap.
            GimmickFrameAnimator.TryAttach(gameObject, GimmickAnimLibrary.GateAnim, 7f);
            baseScale = transform.localScale;
        }

        private void ApplyPresentationScale()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;
            Vector2 native = spriteRenderer.sprite.bounds.size;
            float maxNative = Mathf.Max(native.x, native.y);
            if (maxNative <= 0.0001f) return;

            float scale = targetWorldSize / maxNative;
            transform.localScale = new Vector3(scale, scale, 1f);

            if (TryGetComponent<BoxCollider2D>(out var box))
            {
                box.size = native;
                box.offset = spriteRenderer.sprite.bounds.center;
            }
        }

        private void Update()
        {
            if (transform.position.y < ChariotRules.KillPlaneY)
            {
                if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
                return;
            }
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            int id = other.gameObject.GetInstanceID();
            if (!processedInstanceIds.Add(id)) return;

            // Portal-passage sparkle in the gate's identity tint, at the crossing point.
            FrameAnimEffect.Spawn(EffectSpriteLibrary.Sparkle, other.transform.position, 1.5f, GetGateArtTint(effectType), 16f);

            if (other.TryGetComponent<UnitController>(out var unit))
            {
                ApplyToUnit(unit);
            }
            else if (other.TryGetComponent<ArrowController>(out var arrow))
            {
                ApplyToArrow(arrow);
            }
            else if (other.TryGetComponent<Rigidbody2D>(out var rb))
            {
                ApplyToRigidbody(rb);
            }
        }

        public void ApplyToUnit(UnitController unit)
        {
            if (unit == null) return;

            switch (effectType)
            {
                case EventGateEffectType.Multiply:
                    MultiplyUnit(unit);
                    SpawnFloatingText(unit.transform.position, "RALLY x2", Color.cyan);
                    break;
                case EventGateEffectType.Reduce:
                    unit.ApplyLaunchPowerMultiplier(reduceVelocityMultiplier, reduceDamageSpeedMultiplier, effectDuration);
                    ApplyExplosiveScaling(unit, reduceDamageSpeedMultiplier);
                    SpawnFloatingText(unit.transform.position, "HEX SLOW", new Color(1f, 0.45f, 0.9f, 1f));
                    break;
                case EventGateEffectType.PowerUp:
                    unit.ApplyLaunchPowerMultiplier(velocityMultiplier, damageSpeedMultiplier, effectDuration);
                    ApplyExplosiveScaling(unit, damageSpeedMultiplier);
                    SpawnFloatingText(unit.transform.position, "WAR CRY+", Color.yellow);
                    break;
                case EventGateEffectType.PowerDown:
                    unit.ApplyLaunchPowerMultiplier(reduceVelocityMultiplier, reduceDamageSpeedMultiplier, effectDuration);
                    ApplyExplosiveScaling(unit, reduceDamageSpeedMultiplier);
                    SpawnFloatingText(unit.transform.position, "DAMPENED", new Color(0.75f, 0.45f, 1f, 1f));
                    break;
            }
        }

        // Bomber's blast potency now reverts after effectDuration just like the shared
        // velocity/damage-speed effect above (ApplyLaunchPowerMultiplier -> ApplyBuff/
        // ApplyDebuff), instead of permanently compounding on every gate pass.
        private void ApplyExplosiveScaling(UnitController unit, float multiplier)
        {
            var explosive = unit.GetComponent<ExplosiveGimmick>();
            if (explosive != null)
            {
                explosive.ApplyTemporaryPotencyMultiplier(multiplier, effectDuration);
            }
        }



        public void ApplyToArrow(ArrowController arrow)
        {
            if (arrow == null) return;

            switch (effectType)
            {
                case EventGateEffectType.Multiply:
                    MultiplyArrow(arrow);
                    SpawnFloatingText(arrow.transform.position, "RALLY x2", Color.cyan);
                    break;
                case EventGateEffectType.Reduce:
                    arrow.ApplyDebuff(reduceDamageSpeedMultiplier);
                    if (destroyOnReduce) DestroySafely(arrow.gameObject);
                    SpawnFloatingText(arrow.transform.position, "HEX SLOW", new Color(1f, 0.45f, 0.9f, 1f));
                    break;
                case EventGateEffectType.PowerUp:
                    arrow.ApplyBuff(damageSpeedMultiplier);
                    SpawnFloatingText(arrow.transform.position, "WAR CRY+", Color.yellow);
                    break;
                case EventGateEffectType.PowerDown:
                    arrow.ApplyDebuff(reduceDamageSpeedMultiplier);
                    SpawnFloatingText(arrow.transform.position, "DAMPENED", new Color(0.75f, 0.45f, 1f, 1f));
                    break;
            }
        }

        private void ApplyToRigidbody(Rigidbody2D rb)
        {
            if (rb == null) return;
            float multiplier = effectType == EventGateEffectType.PowerUp || effectType == EventGateEffectType.Multiply
                ? velocityMultiplier
                : reduceVelocityMultiplier;
            rb.velocity *= multiplier;
        }

        private void MultiplyUnit(UnitController source)
        {
            if (source == null || spawnedCloneCount >= maxTotalClones) return;
            int count = Mathf.Clamp(cloneCount, 1, Mathf.Max(1, maxTotalClones - spawnedCloneCount));
            Vector2 sourceVelocity = source.TryGetComponent<Rigidbody2D>(out var sourceRb) ? sourceRb.velocity : Vector2.right;

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(0f, (i + 1) * 0.28f, 0f);
                var cloneGo = Instantiate(source.gameObject, source.transform.position + offset, source.transform.rotation);
                cloneGo.name = source.gameObject.name + "_GateClone";
                processedInstanceIds.Add(cloneGo.GetInstanceID());
                if (cloneGo.TryGetComponent<UnitController>(out var clone))
                {
                    clone.InitializeUnit(source.isPlayerUnit, UnitState.Launched);
                    clone.Launch(sourceVelocity + new Vector2(0f, 0.45f * (i + 1)));
                }
                spawnedCloneCount++;
            }
            AnnounceCloneBudgetIfExhausted();
        }

        private void MultiplyArrow(ArrowController source)
        {
            if (source == null || spawnedCloneCount >= maxTotalClones) return;
            int count = Mathf.Clamp(cloneCount, 1, Mathf.Max(1, maxTotalClones - spawnedCloneCount));
            Vector2 sourceVelocity = source.TryGetComponent<Rigidbody2D>(out var sourceRb) ? sourceRb.velocity : Vector2.right * source.speed;

            for (int i = 0; i < count; i++)
            {
                Vector3 offset = new Vector3(0f, (i + 1) * 0.16f, 0f);
                var cloneGo = Instantiate(source.gameObject, source.transform.position + offset, source.transform.rotation);
                cloneGo.name = source.gameObject.name + "_GateClone";
                processedInstanceIds.Add(cloneGo.GetInstanceID());
                if (cloneGo.TryGetComponent<Rigidbody2D>(out var cloneRb))
                {
                    cloneRb.velocity = sourceVelocity + new Vector2(0f, 0.35f * (i + 1));
                }
                spawnedCloneCount++;
            }
            AnnounceCloneBudgetIfExhausted();
        }

        // Once the clone budget is spent the gate keeps pulsing/tinted as if it still works,
        // which misleads players into expecting a clone that will never spawn. Dim it and
        // say so, once, instead of staying silently inert.
        private bool cloneBudgetAnnounced;

        private void AnnounceCloneBudgetIfExhausted()
        {
            if (cloneBudgetAnnounced || spawnedCloneCount < maxTotalClones) return;
            cloneBudgetAnnounced = true;

            if (spriteRenderer != null)
            {
                var c = spriteRenderer.color;
                spriteRenderer.color = new Color(c.r, c.g, c.b, c.a * 0.35f);
            }
            SpawnFloatingText(transform.position + Vector3.up * 0.7f, "SPENT", new Color(0.6f, 0.6f, 0.6f, 0.9f));
        }

        private void SpawnFloatingText(Vector3 position, string text, Color color)
        {
            if (!Application.isPlaying)
            {
                GameFeelVfx.SpawnFeedbackLabel(position, text, color, 1.4f, 0.35f);
                return;
            }

            var go = new GameObject("EventGateFloatingText");
            go.transform.position = position + new Vector3(0f, 0.55f, 0f);
            var tmp = go.AddComponent<TMPro.TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 3.2f;
            tmp.fontStyle = TMPro.FontStyles.Bold;
            tmp.characterSpacing = 4f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.sortingOrder = 45;

            var animator = go.AddComponent<FloatingDamageText>();
            animator.lifetime = 0.8f;
            animator.riseDistance = 1.2f;
        }

        private static void DestroySafely(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
        }

        private static Color GetGateColor(EventGateEffectType type)
        {
            switch (type)
            {
                case EventGateEffectType.Multiply: return new Color(0.2f, 0.9f, 1f, 0.45f);
                case EventGateEffectType.Reduce: return new Color(1f, 0.3f, 0.85f, 0.45f);
                case EventGateEffectType.PowerUp: return new Color(1f, 0.9f, 0.2f, 0.45f);
                case EventGateEffectType.PowerDown: return new Color(0.55f, 0.3f, 1f, 0.45f);
                default: return Color.white;
            }
        }

        // Tints for the dedicated arch art: near-white so the stone/portal detail stays
        // readable, with just enough hue to telegraph the effect type at a glance.
        private static Color GetGateArtTint(EventGateEffectType type)
        {
            switch (type)
            {
                case EventGateEffectType.Multiply: return new Color(0.75f, 1f, 1f, 0.95f);
                case EventGateEffectType.Reduce: return new Color(1f, 0.75f, 0.95f, 0.95f);
                case EventGateEffectType.PowerUp: return new Color(1f, 0.98f, 0.8f, 0.95f);
                case EventGateEffectType.PowerDown: return new Color(0.85f, 0.78f, 1f, 0.95f);
                default: return Color.white;
            }
        }
    }
}
