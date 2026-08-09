using UnityEngine;

namespace CastleBusters
{
    public enum SpikeTrapPhase { Dormant, Arming, Active, Cooldown }

    /// <summary>
    /// Pure phase-transition + knockback math for SpikeTrapGimmick, kept static so EditMode
    /// tests can pin the contract with zero MonoBehaviour/Unity-lifecycle dependency (mirrors
    /// EruptionVentGimmick's WrapCycleTime/PhaseAt/LiftDeltaV pattern).
    /// </summary>
    public static class SpikeTrapRules
    {
        /// <summary>
        /// Pure phase-transition step. `phaseElapsed` is seconds since entering `current`.
        /// `bodyDetected` is only meaningful in Dormant (proximity check result for this
        /// frame) — every other phase transitions purely on elapsed time, so a trap that
        /// already armed can't be re-triggered mid-cycle, and a unit lingering through
        /// Cooldown can't force an early re-arm.
        /// </summary>
        public static SpikeTrapPhase NextPhase(SpikeTrapPhase current, bool bodyDetected,
            float phaseElapsed, float armDelaySeconds, float activeDuration, float cooldownDuration)
        {
            switch (current)
            {
                case SpikeTrapPhase.Dormant:
                    return bodyDetected ? SpikeTrapPhase.Arming : SpikeTrapPhase.Dormant;
                case SpikeTrapPhase.Arming:
                    return phaseElapsed >= armDelaySeconds ? SpikeTrapPhase.Active : SpikeTrapPhase.Arming;
                case SpikeTrapPhase.Active:
                    return phaseElapsed >= activeDuration ? SpikeTrapPhase.Cooldown : SpikeTrapPhase.Active;
                case SpikeTrapPhase.Cooldown:
                    return phaseElapsed >= cooldownDuration ? SpikeTrapPhase.Dormant : SpikeTrapPhase.Cooldown;
                default:
                    return current;
            }
        }

        /// <summary>
        /// Deterministic launch vector, horizontal-away-from-trap with a guaranteed positive
        /// y component (upward bias) regardless of horizontal direction — a hit unit is always
        /// launched up-and-out, never purely sideways or downward. `upwardBias` in (0,1)
        /// exclusive keeps both components non-zero.
        /// </summary>
        public static Vector2 KnockbackVelocity(Vector2 unitPosition, Vector2 trapPosition,
            float knockSpeed, float upwardBias)
        {
            float dx = unitPosition.x - trapPosition.x;
            float horizontalSign = Mathf.Abs(dx) < 0.0001f ? 1f : Mathf.Sign(dx);
            Vector2 dir = new Vector2(horizontalSign * (1f - upwardBias), upwardBias).normalized;
            return dir * knockSpeed;
        }
    }

    /// <summary>
    /// Proximity-triggered floor hazard ("함정/가시덫"): dormant until a unit walks within
    /// triggerRadius, then arms (telegraph throb), bursts once with damage + knockback, and
    /// cools down before it can re-arm. Unlike EruptionVentGimmick (fixed clock) or
    /// EventGateGimmick/BuffDebuffGimmick (one-shot collider trigger), this is a state machine
    /// driven by a repeated Physics2D.OverlapCircleAll proximity check in FixedUpdate — the
    /// trap can fire an unbounded number of times as long as units keep walking near it.
    /// </summary>
    public class SpikeTrapGimmick : MonoBehaviour
    {
        [Header("Trap Settings")]
        public float triggerRadius = 1.8f;
        public float burstRadius = 2.0f;
        public float armDelaySeconds = 0.4f;
        public float activeDuration = 0.5f;
        public float cooldownDuration = 2.0f;
        public float damage = 24f;
        public float knockSpeed = 9f;
        public float upwardBias = 0.6f;
        public float targetWorldSize = 1.15f;

        private static readonly Color WarningColor = new Color(0.9f, 0.25f, 0.15f, 1f);

        private SpriteRenderer sr;
        private Sprite dormantSprite;
        private Sprite armedSprite;
        private SpikeTrapPhase currentPhase = SpikeTrapPhase.Dormant;
        private SpikeTrapPhase lastPhase = SpikeTrapPhase.Dormant;
        private float phaseStartTime;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 3; // above ground tiles, below units/ram — matches vent hazard layer
            phaseStartTime = Time.time;
            ApplyVisuals();
        }

        // No spawner-assigned style/effectType field drives this gimmick's art (unlike
        // EruptionVentGimmick), so the classic AddComponent-then-assign desync doesn't apply
        // here by construction. Still re-applied idempotently in Start in case a future caller
        // configures fields post-add.
        private void Start()
        {
            ApplyVisuals();
        }

        private void ApplyVisuals()
        {
            bool isStage2 = GameManager.Instance != null && GameManager.Instance.currentStage == StageId.Stage2;
            dormantSprite = GimmickSpriteLibrary.Load(isStage2
                ? GimmickSpriteLibrary.Stage2SpikeTrapDormant
                : GimmickSpriteLibrary.SpikeTrapDormant);
            armedSprite = GimmickSpriteLibrary.Load(isStage2
                ? GimmickSpriteLibrary.Stage2SpikeTrapArmed
                : GimmickSpriteLibrary.SpikeTrapArmed);
            if (dormantSprite == null) dormantSprite = GetFallbackTrapSprite();
            if (sr.sprite == null) sr.sprite = dormantSprite;
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

        private static Sprite cachedFallbackTrap;

        // Optional robustness only: the real art (SpikeTrapDormant/SpikeTrapArmed) is already
        // committed, so this flat plate exists purely so the hazard never renders invisible if
        // an asset import ever regresses.
        private static Sprite GetFallbackTrapSprite()
        {
            if (cachedFallbackTrap != null) return cachedFallbackTrap;

            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var plate = new Color(0.25f, 0.25f, 0.28f, 1f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    tex.SetPixel(x, y, plate);
                }
            }
            tex.Apply(false, true);
            cachedFallbackTrap = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.1f), size);
            cachedFallbackTrap.name = "FallbackSpikeTrapPlate";
            return cachedFallbackTrap;
        }

        private void Update()
        {
            if (!Application.isPlaying) return;

            if (currentPhase != lastPhase)
            {
                if (currentPhase == SpikeTrapPhase.Arming) EnterArming();
                lastPhase = currentPhase;
            }

            if (currentPhase == SpikeTrapPhase.Arming)
            {
                float throb = 0.5f + 0.5f * Mathf.Sin(Time.time * 14f);
                sr.color = Color.Lerp(Color.white, WarningColor, 0.35f + 0.45f * throb);
                sr.sprite = dormantSprite;
            }
            else if (currentPhase == SpikeTrapPhase.Active)
            {
                sr.color = Color.white;
                sr.sprite = armedSprite != null ? armedSprite : dormantSprite;
            }
            else // Dormant or Cooldown
            {
                sr.color = Color.white;
                sr.sprite = dormantSprite;
            }
        }

        private void EnterArming()
        {
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.6f,
                "함정 발동! TRAP ARMING", WarningColor, 1.6f, armDelaySeconds);
        }

        private void FixedUpdate()
        {
            if (!Application.isPlaying) return;

            float elapsed = Time.time - phaseStartTime;
            bool bodyDetected = false;
            if (currentPhase == SpikeTrapPhase.Dormant)
            {
                var hits = Physics2D.OverlapCircleAll(transform.position, triggerRadius);
                foreach (var h in hits)
                {
                    if (h != null && h.GetComponent<UnitController>() != null) { bodyDetected = true; break; }
                }
            }

            var next = SpikeTrapRules.NextPhase(currentPhase, bodyDetected, elapsed,
                armDelaySeconds, activeDuration, cooldownDuration);
            if (next != currentPhase)
            {
                if (next == SpikeTrapPhase.Active) FireBurst();
                currentPhase = next;
                phaseStartTime = Time.time;
            }
        }

        private void FireBurst()
        {
            var hits = Physics2D.OverlapCircleAll(transform.position, burstRadius);
            foreach (var hit in hits)
            {
                if (hit == null) continue;
                if (hit.TryGetComponent<UnitController>(out var unit))
                {
                    unit.TakeDamage(damage);
                    if (hit.attachedRigidbody != null)
                    {
                        hit.attachedRigidbody.velocity = SpikeTrapRules.KnockbackVelocity(
                            (Vector2)unit.transform.position, (Vector2)transform.position, knockSpeed, upwardBias);
                    }
                    GameFeelVfx.SpawnImpactBurst(hit.transform.position, new Color(0.85f, 0.2f, 0.15f, 0.9f), 0.9f);
                }
            }

            FrameAnimEffect.Spawn(EffectSpriteLibrary.Spark, transform.position + Vector3.up * 0.3f,
                1.6f, new Color(1f, 0.4f, 0.2f, 0.95f), 16f);
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.9f,
                "SPIKES!", new Color(1f, 0.35f, 0.2f, 1f), 2.2f, 0.55f);
            HitStopManager.Instance?.TriggerHitStop(0.05f);
            ScreenShakeManager.Instance?.TriggerShake(0.22f, 0.1f);
        }
    }
}
