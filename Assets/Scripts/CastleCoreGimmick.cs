using UnityEngine;

namespace CastleBusters
{
    public class CastleCoreGimmick : DestructibleBlock
    {
        [Header("Core Settings")]
        public bool isPlayerCore = true;
        public float coreTargetWorldSize = 2.3f; // Scaled up by 1.4375x (>= 0.4x increase) for usability and playability

        private const float ShieldMaxHP = 50f;
        private Vector3 coreBaseScale = Vector3.one;
        private bool shieldTriggered;
        private float shieldHP;
        // A pristine core always survives the first resolved volley. This closes the
        // barrel/clone collapse loophole where one launch could consume the whole match.
        private const float FullHealthVolleyDamageCap = 140f;
        private int damageBudgetTurn = int.MinValue;
        private float healthDamageThisTurn;
        private bool turnStartedAtFullHealth;
        private bool braceFeedbackShown;
        protected override void Awake()
        {
            maxHP = 150f;
            scoreValue = 500;

            base.Awake();

            currentHP = maxHP;
        }

        // Visuals live in Start, not Awake: SpawnCastleCores() adds this component and assigns
        // isPlayerCore afterwards, so Awake always saw the default (player) team tint - the enemy
        // core rendered blue. Start runs after field assignment in the same frame.
        private void Start()
        {
            ApplyCoreVisuals();
        }

        public void ApplyCoreVisuals()
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.sortingOrder = 3;

            var coreArt = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.Core);
            if (coreArt != null)
            {
                // Dedicated crystal-keep-core art: near-white team tints keep the pedestal/orb
                // detail readable. Registered via SetPresentationSprite so DestructibleBlock's
                // damage-state UpdateVisuals() keeps this sprite as "normal" instead of reverting
                // to a null placeholder on the first hit.
                SetPresentationSprite(coreArt, isPlayerCore ? new Color(0.75f, 0.9f, 1f) : new Color(1f, 0.92f, 0.7f));
            }
            else
            {
                sr.color = isPlayerCore ? new Color(0.2f, 0.7f, 1f) : new Color(1f, 0.8f, 0.2f); // Blue for player core, Gold for enemy core
            }

            ApplyCorePresentationScale(sr);
            // Animated crystal bob loop; suspended on first damage so cracked art wins.
            GimmickFrameAnimator.TryAttach(gameObject, GimmickAnimLibrary.CoreAnim, 6f);
            coreBaseScale = transform.localScale;
        }

        private void ApplyCorePresentationScale(SpriteRenderer sr)
        {
            if (sr == null || sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            float maxNative = Mathf.Max(native.x, native.y);
            if (maxNative <= 0.0001f) return;

            float scale = coreTargetWorldSize / maxNative;
            coreBaseScale = new Vector3(scale, scale, 1f);
            transform.localScale = coreBaseScale;

            if (TryGetComponent<BoxCollider2D>(out var box))
            {
                box.size = native;
                box.offset = sr.sprite.bounds.center;
            }
        }

        [HideInInspector]
        public float simulatedTime = 0f;

        private float GetTime() => Application.isPlaying ? Time.time : simulatedTime;

        public override void TakeDamage(float damage)
        {
            TakeDamage(damage, null);
        }

        public override void TakeDamage(float damage, bool? damageFromPlayer)
        {
            var gm = GameManager.Instance;
            int turn = gm != null ? gm.TurnCount : int.MinValue;
            if (gm != null && turn != damageBudgetTurn)
            {
                damageBudgetTurn = turn;
                healthDamageThisTurn = 0f;
                turnStartedAtFullHealth = currentHP >= maxHP - 0.01f;
                braceFeedbackShown = false;
            }

            if (shieldHP > 0f)
            {
                float absorbed = Mathf.Min(damage, shieldHP);
                shieldHP -= absorbed;
                damage -= absorbed;
                GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.5f, "ABSORBED!", new Color(0.35f, 0.85f, 1f, 1f), 1.8f, 0.45f);
                if (shieldHP <= 0f)
                {
                    GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.5f, "SHIELD BROKEN!", new Color(1f, 0.25f, 0.15f, 1f), 2.2f, 0.55f);
                }
            }

            if (gm != null && turnStartedAtFullHealth)
            {
                float remainingHealthDamage = Mathf.Max(0f, FullHealthVolleyDamageCap - healthDamageThisTurn);
                float uncappedDamage = damage;
                damage = Mathf.Min(damage, remainingHealthDamage);
                healthDamageThisTurn += damage;

                if (damage + 0.01f < uncappedDamage && !braceFeedbackShown)
                {
                    braceFeedbackShown = true;
                    GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.65f, "CORE BRACED!", new Color(1f, 0.78f, 0.25f, 1f), 2.1f, 0.5f);
                }
            }

            if (damage <= 0f) return;

            // Damage states own the renderer from here: stop the idle crystal loop.
            GetComponent<GimmickFrameAnimator>()?.Suspend();

            base.TakeDamage(damage, damageFromPlayer);

            // Cycle 20: Trigger shield when core drops below 50% HP (75 HP)
            if (!shieldTriggered && currentHP > 0f && currentHP <= maxHP * 0.5f)
            {
                shieldTriggered = true;
                shieldHP = ShieldMaxHP;
                GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.8f, "SHIELD ACTIVE!", new Color(0.35f, 0.85f, 1f, 1f), 2.5f, 0.65f);
                GameFeelVfx.SpawnShockwaveRing(transform.position, new Color(0.35f, 0.85f, 1f, 0.65f), 2.2f, 0.45f);
                if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.4f, 0.25f);
            }
        }
        protected override void Update()
        {
            base.Update();

            float time = GetTime();
            float pulse = 1f + Mathf.Sin(time * 4f) * 0.08f;
            transform.localScale = coreBaseScale * pulse;
        }

        private void OnDestroy()
        {
            if (Application.isPlaying && currentHP <= 0)
            {
                // Trigger game over when core is destroyed
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CheckVictoryConditions();
                }
            }
        }
    }
}
