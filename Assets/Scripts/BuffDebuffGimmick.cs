using System.Collections.Generic;
using UnityEngine;


namespace CastleBusters
{
    public enum GimmickEffectType { Buff, Debuff }

    public class BuffDebuffGimmick : MonoBehaviour
    {
        [Header("Gimmick Settings")]
        public GimmickEffectType effectType = GimmickEffectType.Buff;
        public float targetWorldSize = 2.6f; // Scaled up by 1.44x (from 1.8f to 2.6f) for usability and playability
        public float pulseSpeed = 3.0f;

        private SpriteRenderer spriteRenderer;
        private Vector3 baseScale;

        // Unlike EventGateGimmick (which has a one-shot processedInstanceIds guard), this
        // zone is a persistent AoE, so a unit that jitters across the trigger boundary
        // (common after a launch/knockback) could re-fire OnTriggerEnter2D repeatedly and
        // restack ApplyBuff/ApplyDebuff, extending the effect duration unpredictably for
        // whichever unit happens to bounce there. The cooldown only suppresses a repeat of
        // the SAME effectType within the window (jitter spam); a genuinely different effect
        // — e.g. this zone's effectType was reconfigured, or the unit crossed into a second
        // zone occupying the same collider — still applies immediately.
        private readonly Dictionary<int, (GimmickEffectType type, float time)> lastAppliedByInstance =
            new Dictionary<int, (GimmickEffectType type, float time)>();
        private const float retriggerCooldownSeconds = 1.0f;


        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null) spriteRenderer.sortingOrder = 1; // Render behind units/blocks

            // Ensure it has a trigger collider
            var col = GetComponent<Collider2D>();
            if (col == null) col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = true;

            baseScale = transform.localScale;
        }

        // Visuals live in Start, not Awake: spawners AddComponent() first and assign effectType
        // right after, so Awake still sees the default value and would pick the wrong rune art.
        private void Start()
        {
            ApplyVisuals();
        }

        public void ApplyVisuals()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return;

            // Dedicated rune decal art; tinted-block fallback keeps old scenes working.
            bool dedicated = GimmickSpriteLibrary.TryApply(
                spriteRenderer,
                effectType == GimmickEffectType.Buff ? GimmickSpriteLibrary.RallyRune : GimmickSpriteLibrary.HexRune,
                new Color(1f, 1f, 1f, 0.9f));
            if (!dedicated)
            {
                // Semi-transparent color: Green for Buff, Purple for Debuff
                spriteRenderer.color = effectType == GimmickEffectType.Buff
                    ? new Color(0.2f, 0.8f, 0.3f, 0.4f)
                    : new Color(0.7f, 0.2f, 0.8f, 0.4f);
            }

            ApplyPresentationScale();
            // Animated rune art (4-frame pulse loop) takes over when the frames exist; world
            // size from ApplyPresentationScale is preserved by TryAttach. baseScale captures
            // the animator's rescale so the existing scale-pulse keeps working on top.
            GimmickFrameAnimator.TryAttach(gameObject,
                effectType == GimmickEffectType.Buff ? GimmickAnimLibrary.RallyRuneAnim : GimmickAnimLibrary.HexRuneAnim,
                7f);
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
            // Gentle pulsing animation to indicate it's an active zone
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * 0.1f;
            transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);
        }


        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null) return;
            int id = other.gameObject.GetInstanceID();
            float now = Application.isPlaying ? Time.time : 0f;
            if (lastAppliedByInstance.TryGetValue(id, out var last) &&
                last.type == effectType && now - last.time < retriggerCooldownSeconds) return;
            lastAppliedByInstance[id] = (effectType, now);


            // Apply effect to units
            if (other.TryGetComponent<UnitController>(out var unit))
            {
                ApplyToUnit(unit);
                SpawnRuneSparkle(unit.transform.position);
            }
            // Apply effect to projectiles (arrows)
            else if (other.TryGetComponent<ArrowController>(out var arrow))
            {
                ApplyToArrow(arrow);
                SpawnRuneSparkle(arrow.transform.position);
            }
        }


        private void SpawnRuneSparkle(Vector3 position)
        {
            // Dedicated sparkle frames, tinted to the rune's identity color.
            FrameAnimEffect.Spawn(EffectSpriteLibrary.Sparkle, position, 1.3f,
                effectType == GimmickEffectType.Buff ? new Color(0.6f, 1f, 0.7f, 1f) : new Color(0.85f, 0.6f, 1f, 1f), 16f);
        }

        private void ApplyToUnit(UnitController unit)
        {
            if (effectType == GimmickEffectType.Buff)
            {
                unit.ApplyBuff(1.5f, 5f); // 1.5x damage/speed for 5 seconds
                SpawnFloatingText(unit.transform.position, "BUFF!", Color.green);
            }
            else
            {
                unit.ApplyDebuff(0.5f, 5f); // 0.5x speed/damage for 5 seconds
                SpawnFloatingText(unit.transform.position, "DEBUFF!", Color.magenta);
            }
        }

        private void ApplyToArrow(ArrowController arrow)
        {
            if (effectType == GimmickEffectType.Buff)
            {
                arrow.ApplyBuff(1.5f); // 1.5x damage/speed
                SpawnFloatingText(arrow.transform.position, "BOOST!", Color.green);
            }
            else
            {
                arrow.ApplyDebuff(0.5f); // 0.5x speed/damage
                SpawnFloatingText(arrow.transform.position, "SLOW!", Color.magenta);
            }
        }

        private void SpawnFloatingText(Vector3 position, string text, Color color)
        {
            if (!Application.isPlaying) return;

            var go = new GameObject("FloatingText");
            go.transform.position = position + new Vector3(0f, 0.45f, 0f);
            var tmp = go.AddComponent<TMPro.TextMeshPro>();
            tmp.text = text;
            tmp.fontSize = 3.5f;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.sortingOrder = 40;

            var animator = go.AddComponent<FloatingDamageText>();
            animator.lifetime = 0.75f;
            animator.riseDistance = 1.15f;
        }
    }
}
