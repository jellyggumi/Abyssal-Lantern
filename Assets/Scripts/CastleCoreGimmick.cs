using UnityEngine;

namespace CastleBusters
{
    public class CastleCoreGimmick : DestructibleBlock
    {
        [Header("Core Settings")]
        public bool isPlayerCore = true;
        // The keep is the object the whole match is about — it carries the largest presentation
        // footprint on the board so "how close am I to losing" is legible without a HUD glance.
        public float coreTargetWorldSize = 2.3f;

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
        // Damage stage currently displayed (0 intact, 1 battered, 2 near-ruin). -1 = not yet
        // applied, so the first RefreshKeepStage() always installs stage art.
        private int keepStage = -1;
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

            // The base you defend is a CASTLE KEEP, not an abstract crystal: three
            // hand-authored damage stages whose SILHOUETTES differ (roofs → gone, battlements
            // → gap-toothed, wall → breached), so its condition reads at gameplay scale
            // without a health bar. Falls back to the legacy crystal-core art when the keep
            // art is absent, keeping art-less builds correct (same contract as CastleSkinLibrary).
            var keepArt = GimmickSpriteLibrary.Load(GimmickAnimLibrary.CastleKeepStill(0));
            bool hasKeepArt = keepArt != null;
            var art = hasKeepArt ? keepArt : GimmickSpriteLibrary.Load(GimmickSpriteLibrary.Core);

            if (art != null)
            {
                // Registered through SetPresentationSprite so DestructibleBlock's damage-state
                // UpdateVisuals() treats this as "normal" instead of reverting to a null
                // placeholder on the first hit.
                SetPresentationSprite(art, isPlayerCore ? new Color(0.75f, 0.9f, 1f) : new Color(1f, 0.92f, 0.7f));
            }
            else
            {
                sr.color = isPlayerCore ? new Color(0.2f, 0.7f, 1f) : new Color(1f, 0.8f, 0.2f);
            }

            if (hasKeepArt)
            {
                // Feed the damage-state slots so a band change swaps the keep silhouette even
                // if the frame animator is unavailable (missing frames, suspended, EditMode).
                SetSkinSprites(
                    keepArt,
                    GimmickSpriteLibrary.Load(GimmickAnimLibrary.CastleKeepStill(1)) ?? keepArt,
                    GimmickSpriteLibrary.Load(GimmickAnimLibrary.CastleKeepStill(2)) ?? keepArt);
            }

            ApplyCorePresentationScale(sr);

            // Idle loop for the current stage: banner flutter + window-glow life while intact,
            // guttering embers and settling dust once breached.
            GimmickFrameAnimator.TryAttach(gameObject,
                hasKeepArt ? GimmickAnimLibrary.CastleKeepAnim(0) : GimmickAnimLibrary.CoreAnim,
                hasKeepArt ? 5f : 6f);
            keepStage = hasKeepArt ? 0 : -1;
            coreBaseScale = transform.localScale;
        }

        /// <summary>
        /// Points the idle loop at the frame set for the keep's current damage band, so the
        /// castle visibly degrades as it is battered instead of freezing on a still.
        /// Presentation-only (CLAUDE.md §2): reads HP, never writes simulation state.
        /// </summary>
        private void RefreshKeepStage()
        {
            if (keepStage < 0) return; // legacy crystal-core art path: nothing staged to swap
            int band = DisplayBand;
            if (band == keepStage) return;

            var anim = GetComponent<GimmickFrameAnimator>();
            if (anim != null && anim.Retarget(GimmickAnimLibrary.CastleKeepAnim(band), band == 0 ? 5f : 4f))
            {
                keepStage = band;
            }
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

            // Legacy crystal-core art has only one loop, so damage art must own the renderer
            // and the loop stops. The staged castle keep instead KEEPS animating and swaps to
            // the frame set for its new damage band — the keep crumbles on screen rather than
            // freezing on a still the moment it is first hit.
            if (keepStage < 0) GetComponent<GimmickFrameAnimator>()?.Suspend();

            base.TakeDamage(damage, damageFromPlayer);
            RefreshKeepStage();

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
