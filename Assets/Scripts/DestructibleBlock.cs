using System;
using UnityEngine;
using System.Collections.Generic;

namespace CastleBusters
{
    public class DestructibleBlock : MonoBehaviour
    {
        public BlockData blockData;

        public float maxHP = 100f;
        public float currentHP;
        public int scoreValue = 10;

        [Header("Visuals")]
        public Sprite normalSprite;
        public Sprite crackedSprite;
        public Sprite heavilyCrackedSprite;
        public GameObject destructionEffectPrefab;

        [Header("Physics")]
        public bool isGroundAnchor;

        [Header("Presentation Scale")]
        [Tooltip("Final on-screen world size (units) of the block. Source sprites are authored ~12.5u; this rescales them to match the ~1u castle grid so the castle is not a giant overlapping blob.")]
        public float targetWorldSize = 1.0f;

        private SpriteRenderer spriteRenderer;
        private Rigidbody2D rb;
        private bool isFalling;
        private bool isDestroying;

        // Lazily-evaluated damage-state sprites. Used by callers (e.g. GameManager's ground tiling)
        // that would otherwise have to eagerly bake a unique cracked texture for every single tile up
        // front even though most tiles never visibly crack during a match. The factory runs at most
        // once per damage state and the resulting sprite is cached in crackedSprite/heavilyCrackedSprite.
        private Func<Sprite> crackedSpriteFactory;
        private Func<Sprite> heavilyCrackedSpriteFactory;
        // Castle-wide presentation wear floor (CastleFacadeDirector milestone ratchet): raises the
        // *displayed* damage band without ever touching HP. 0 = own band only.
        private int displayWearFloor;

        public bool IsFalling => isFalling;

        public static readonly List<DestructibleBlock> Active = new List<DestructibleBlock>();
        protected virtual void OnEnable() { Active.Add(this); }
        protected virtual void OnDisable() { Active.Remove(this); }
        // EditMode tests invoke explosion/integrity paths without OnEnable — see UnitController.ActiveOrScene.
        public static IReadOnlyList<DestructibleBlock> ActiveOrScene =>
            Application.isPlaying ? Active : (IReadOnlyList<DestructibleBlock>)FindObjectsOfType<DestructibleBlock>();

        protected virtual void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            rb = GetComponent<Rigidbody2D>();

            if (spriteRenderer != null) spriteRenderer.sortingOrder = 2; // Render in front of ground, behind units
            foreach (var childSr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                childSr.sortingOrder = 2;
            }

            ApplyBlockData(blockData);

            currentHP = maxHP;
            if (rb != null) rb.bodyType = RigidbodyType2D.Static;

            ApplyPresentationScale();
        }

        public void ApplyBlockData(BlockData data)
        {
            blockData = data;
            if (blockData == null) return;

            maxHP = blockData.maxHP;
            normalSprite = SpriteAtlasPacker.Instance != null ? SpriteAtlasPacker.Instance.GetPackedSprite(blockData.normalSprite) : blockData.normalSprite;
            crackedSprite = SpriteAtlasPacker.Instance != null ? SpriteAtlasPacker.Instance.GetPackedSprite(blockData.crackedSprite) : blockData.crackedSprite;
            heavilyCrackedSprite = SpriteAtlasPacker.Instance != null ? SpriteAtlasPacker.Instance.GetPackedSprite(blockData.heavilyCrackedSprite) : blockData.heavilyCrackedSprite;
            destructionEffectPrefab = blockData.destructionEffectPrefab;

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = normalSprite;
                spriteRenderer.color = blockData.blockColor;
            }

            currentHP = maxHP;
            if (rb != null)
            {
                rb.mass = blockData.mass;
                // Shared per-BlockData material instead of a fresh allocation per block: a castle wall
                // made of dozens of identical Wood/Stone/Iron tiles was allocating a brand-new
                // PhysicsMaterial2D for every single one (and again every time it started falling).
                rb.sharedMaterial = blockData.GetSharedPhysicsMaterial();
            }

            ApplyPresentationScale();
        }

        // Overrides the rendered "normal" sprite (e.g. a slice of a seamless ground/tilemap texture)
        // and immediately recomputes scale + collider from it. Callers that swap in a sprite authored
        // at a different native size than blockData.normalSprite MUST go through this method rather
        // than poking spriteRenderer/normalSprite directly - otherwise the collider/scale stay locked
        // to the previous sprite's bounds while the visible art no longer matches it, which is exactly
        // the "collision box floats free of the art" bug this fixes.
        public void SetPresentationSprite(Sprite sprite, Color? tint = null)
        {
            normalSprite = sprite;
            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = sprite;
                if (tint.HasValue) spriteRenderer.color = tint.Value;
            }
            ApplyPresentationScale();
        }


        // Registers deferred generators for the damage-cracked sprites. Only invoked the first time
        // the block's HP actually drops into that damage band, and only once (the result is cached).
        public void SetLazyCrackedSprites(Func<Sprite> crackedFactory, Func<Sprite> heavilyCrackedFactory)
        {
            crackedSpriteFactory = crackedFactory;
            heavilyCrackedSpriteFactory = heavilyCrackedFactory;
        }

        /// <summary>Assigns position-aware facade skins (CastleFacadeDirector). Presentation-only:
        /// replaces the three damage-state sprites + mirrors the renderer, then re-resolves the
        /// currently displayed band. Clears any lazy factories — the skin IS the damage art now.</summary>
        public void SetSkinSprites(Sprite normal, Sprite cracked, Sprite heavy, bool flipX = false)
        {
            if (normal == null || cracked == null || heavy == null) return;
            normalSprite = normal;
            crackedSprite = cracked;
            heavilyCrackedSprite = heavy;
            crackedSpriteFactory = null;
            heavilyCrackedSpriteFactory = null;
            if (spriteRenderer != null) spriteRenderer.flipX = flipX;
            ApplyPresentationScale();
            UpdateVisuals();
        }

        /// <summary>Presentation-only floor on the displayed damage band (0..2). The facade
        /// director ratchets this castle-wide at wholeness milestones; HP is never modified.</summary>
        public void SetDisplayWearFloor(int floor)
        {
            int clamped = Mathf.Clamp(floor, 0, 2);
            if (clamped == displayWearFloor) return;
            displayWearFloor = clamped;
            UpdateVisuals();
        }

        /// <summary>
        /// The damage band currently DISPLAYED (0 intact, 1 cracked, 2 crumbling) — true HP
        /// ratio raised by any castle-wide wear floor. Subclasses whose presentation is
        /// staged (the castle keep swapping silhouettes as it is battered) read this instead
        /// of recomputing the band, so a wear-floor ratchet moves them too.
        /// </summary>
        protected int DisplayBand =>
            CastleSkinLibrary.ComputeDisplayBand(maxHP > 0f ? currentHP / maxHP : 0f, displayWearFloor);

        // Rescales the (oversized) source sprite + its collider so the block renders at
        // 'targetWorldSize' world units, matching the ~1u castle grid spacing. Both the
        // SpriteRenderer and the BoxCollider2D end up at the same world size.
        private void ApplyPresentationScale()
        {
            targetWorldSize = Mathf.Max(0.05f, targetWorldSize);
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;

            Vector2 native = spriteRenderer.sprite.bounds.size;
            float maxNative = Mathf.Max(native.x, native.y);
            if (maxNative <= 0.0001f) return;

            float scale = targetWorldSize / maxNative;
            transform.localScale = new Vector3(scale, scale, 1f);

            if (TryGetComponent<BoxCollider2D>(out var box))
            {
                // Local collider == native sprite size, so world collider == targetWorldSize.
                box.size = native;
                box.offset = spriteRenderer.sprite.bounds.center;
            }
        }

        protected virtual void Update()
        {
            if (transform.position.y < ChariotRules.KillPlaneY)
            {
                var chariot = GetComponent<MovingGimmick>();
                if (chariot != null && chariot.chariotMode)
                {
                    chariot.DestroyFromKillPlane();
                    return;
                }

                DestroyBlock();
            }
        }

        // Carries a just-captured opening-volley multiplier from the 3-arg TakeDamage entry
        // point (below) down into this class's own 2-arg virtual implementation, without
        // changing the 2-arg signature CastleCoreGimmick overrides (out of scope here) — see
        // TakeDamage(float, bool?, float) for why this indirection exists.
        private float sourceMultiplierInFlight = 1f;

        public virtual void TakeDamage(float damage)
        {
            TakeDamage(damage, null);
        }

        public virtual void TakeDamage(float damage, bool? damageFromPlayer)
        {
            if (isDestroying) return;

            // Centrally transfers owner + multiplier to a sibling ExplosiveGimmick (a field
            // keg's detonator) before this hit can possibly destroy the block, covering every
            // caller of this virtual entry point — melee/arrow/cannon/explosion via the 3-arg
            // overload below, and environmental/falling/trap callers directly here, which never
            // set sourceMultiplierInFlight and so correctly forward null owner at scale 1.
            GetComponent<ExplosiveGimmick>()?.SetDamageContext(damageFromPlayer, sourceMultiplierInFlight);

            if (isFalling && currentHP <= 0) return;
            float prevRatio = maxHP > 0f ? currentHP / maxHP : 0f;
            currentHP -= damage;
            Color feedbackColor = blockData != null ? blockData.blockColor : new Color(0.65f, 0.55f, 0.42f, 1f);
            GameFeelVfx.SpawnDamageNumber(transform.position, damage, new Color(1f, 0.85f, 0.25f, 1f));
            GameFeelVfx.SpawnImpactBurst(transform.position, feedbackColor, Mathf.Clamp(damage / 35f, 0.45f, 1.8f), spriteRenderer != null ? spriteRenderer.sprite : null, false);
            // Dedicated star-flash impact frames on top of the procedural burst; size tracks damage.
            //
            // Tinted warm, not white. A/B measurement (qa/impact-white-square.md): with this flash
            // on screen the impact carries pale NEUTRAL pixels (209,209,207); disabling it takes
            // them to zero. The art is authored greyscale and the tint multiplies into it, so
            // Color.white left it colourless - and colourless over bright grass and sky is the
            // "white square" that was reported, since nothing else at the impact is neutral. Warm
            // amber matches the burst (0.80,0.50,0.20), the damage number, and the Higgsfield
            // starburst that draws beside it.
            FrameAnimEffect.Spawn(EffectSpriteLibrary.Spark, transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.15f),
                Mathf.Clamp(0.6f + damage / 60f, 0.6f, 1.6f), new Color(1f, 0.78f, 0.36f, 1f), 20f);
            GameplayUxDirector.NotifyDamage(transform.position, damage, this is CastleCoreGimmick);

            if (damage >= maxHP * 0.15f && DebrisPool.Instance != null)
            {
                DebrisPool.Instance.SpawnDebrisBurst(transform.position, feedbackColor, UnityEngine.Random.Range(2, 5));

            }

            if (damage >= maxHP * 0.28f)
            {
                GameFeelVfx.SpawnShockwaveRing(transform.position, new Color(1f, 0.82f, 0.35f, 0.45f), Mathf.Clamp(damage / 45f, 0.55f, 1.55f), 0.28f);
                GameFeelVfx.SpawnFeedbackLabel(transform.position, damage >= maxHP * 0.55f ? "CRACK!" : "CHIP", new Color(1f, 0.9f, 0.35f, 1f), 1.9f, 0.5f);

                // Cycle 9 & 10: Scale hit-stop and screen shake based on damage
                float hitStopDuration = Mathf.Clamp(damage / 250f, 0.03f, 0.15f);
                float shakeMagnitude = Mathf.Clamp(damage / 150f, 0.05f, 0.35f);
                if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(hitStopDuration);
                if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.25f, shakeMagnitude);
            }
            UpdateVisuals();
            // Interactive ruin feedback (crack decals, band-crossing crumble moments, castle
            // wholeness milestones) — presentation-only observer, reads state, never writes it.
            CastleRuinFx.NotifyBlockDamaged(this, damage, prevRatio, maxHP > 0f ? currentHP / maxHP : 0f);
            if (currentHP <= 0) DestroyBlock(damageFromPlayer);
        }

        /// <summary>
        /// Central entry point for callers carrying a captured opening-volley multiplier
        /// (melee, arrow, cannon splash, explosion chains). <paramref name="damage"/> must
        /// already be the final scaled amount — this never re-applies the multiplier to it,
        /// it only propagates <paramref name="sourceMultiplier"/> as metadata to a sibling
        /// ExplosiveGimmick so a keg finished off by this hit detonates with the same origin
        /// scale, then dispatches through the virtual 2-arg overload so subclass rules (e.g.
        /// CastleCoreGimmick's shield/damage budget) still run unchanged.
        /// </summary>
        public void TakeDamage(float damage, bool? damageFromPlayer, float sourceMultiplier)
        {
            if (isDestroying) return;

            float previous = sourceMultiplierInFlight;
            sourceMultiplierInFlight = sourceMultiplier;
            try
            {
                TakeDamage(damage, damageFromPlayer);
            }
            finally
            {
                sourceMultiplierInFlight = previous;
            }
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer == null) return;
            float ratio = currentHP / maxHP;
            int band = CastleSkinLibrary.ComputeDisplayBand(ratio, displayWearFloor);

            if (band >= 2)
            {
                if (heavilyCrackedSprite == null && heavilyCrackedSpriteFactory != null)
                {
                    heavilyCrackedSprite = heavilyCrackedSpriteFactory();
                    heavilyCrackedSpriteFactory = null; // one-shot; release the closure once baked
                }
                spriteRenderer.sprite = heavilyCrackedSprite != null ? heavilyCrackedSprite : (crackedSprite != null ? crackedSprite : normalSprite);
            }
            else if (band == 1)
            {
                if (crackedSprite == null && crackedSpriteFactory != null)
                {
                    crackedSprite = crackedSpriteFactory();
                    crackedSpriteFactory = null;
                }
                spriteRenderer.sprite = crackedSprite != null ? crackedSprite : normalSprite;
            }
            else
            {
                spriteRenderer.sprite = normalSprite;
            }

            if (blockData != null)
            {
                // Tint tracks TRUE hp ratio (not the displayed band) so the wear-floor ratchet
                // darkens nothing — a healthy block under a milestone floor shows worn art at
                // healthy brightness, which reads as "aged", not "about to break".
                Color baseColor = blockData.blockColor;
                spriteRenderer.color = Color.Lerp(baseColor * 0.5f, baseColor, ratio);
            }
        }

        public void MakeFall()
        {
            if (isGroundAnchor || isFalling) return;
            isFalling = true;
            GameFeelVfx.SpawnCollapseDust(transform.position, 1.1f, spriteRenderer != null ? spriteRenderer.sprite : null);

            if (DebrisPool.Instance != null)
            {
                Color debrisColor = blockData != null ? blockData.blockColor : (spriteRenderer != null ? spriteRenderer.color : Color.white);
                DebrisPool.Instance.SpawnDebrisBurst(transform.position, debrisColor, 3);
            }

            if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(0.05f);

            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.mass = blockData != null ? blockData.mass : 1.0f;

                if (blockData != null)
                {
                    rb.sharedMaterial = blockData.GetSharedPhysicsMaterial();
                }

                rb.AddTorque(UnityEngine.Random.Range(-10f, 10f));
                rb.AddForce(new Vector2(UnityEngine.Random.Range(-0.6f, 0.6f), 1.5f), ForceMode2D.Impulse);

            }
        }

        /// <summary>How many collapse links deep this block sits. A block hit directly by a
        /// volley is 0; a block broken by something falling on it is one deeper than the faller.
        /// Set in <see cref="OnCollisionEnter2D"/>, read once at destruction so telemetry can
        /// report cascade depth without walking the BFS. Purely observational — nothing in the
        /// simulation reads it.</summary>
        private int collapseChainDepth;

        protected virtual void DestroyBlock(bool? damageFromPlayer = null)
        {
            if (isDestroying) return;
            isDestroying = true;

            var chariot = GetComponent<MovingGimmick>();
            if (chariot != null && chariot.chariotMode)
            {
                chariot.HandleGameplayDestruction();
            }
            // Ground/foundation tiles can disappear in large batches after one explosion.
            // The explosion already owns that feedback; repeating the full structure-break
            // stack per tile inflated combo results and multiplied transient VFX/hit-stop.
            if (!isGroundAnchor)
            {
                if (destructionEffectPrefab != null) Instantiate(destructionEffectPrefab, transform.position, Quaternion.identity);
                GameFeelVfx.SpawnCollapseDust(transform.position, 1.5f, spriteRenderer != null ? spriteRenderer.sprite : null);
                // Dedicated billowing dust-cloud frames make the break read even when debris is sparse.
                FrameAnimEffect.Spawn(EffectSpriteLibrary.Dust, transform.position,
                    this is CastleCoreGimmick ? 2.6f : 1.7f, Color.white, 14f, 34);
                GameFeelVfx.SpawnShockwaveRing(transform.position, new Color(1f, 0.62f, 0.18f, 0.6f), 1.25f, 0.36f);
                GameFeelVfx.SpawnFeedbackLabel(transform.position, "BREAK!", new Color(1f, 0.72f, 0.18f, 1f), 2.2f, 0.6f);
                GameplayUxDirector.NotifyBreak(transform.position, this is CastleCoreGimmick);

                if (DebrisPool.Instance != null)
                {
                    Color debrisColor = blockData != null ? blockData.blockColor : (spriteRenderer != null ? spriteRenderer.color : Color.white);
                    DebrisPool.Instance.SpawnDebrisBurst(transform.position, debrisColor, 8);
                }

                // Cycle 9 & 10: Scale hit-stop and screen shake on block destruction.
                float hitStopDuration = this is CastleCoreGimmick ? 0.18f : 0.08f;
                float shakeMagnitude = this is CastleCoreGimmick ? 0.45f : 0.22f;
                float shakeDuration = this is CastleCoreGimmick ? 0.75f : 0.45f;
                if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(hitStopDuration);
                if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(shakeDuration, shakeMagnitude);
            }

            // Observation only (CLAUDE.md §2): telemetry reads the cascade, never steers it.
            // Ground anchors are excluded for the same reason they skip premium break feedback
            // (D-004) — 205 terrain tiles falling from one blast is not a reward event, and
            // counting them would drown the keep-collapse signal G4/G7 actually measure.
            if (!isGroundAnchor) TelemetrySink.BlockDestroyed(collapseChainDepth);
            // Same exclusion, same reason, for the player-facing readback: a shot that dropped
            // three wall blocks and forty terrain tiles reads as "성벽 3블록", because the wall
            // is what the next shot has to get through. Cores are counted as core damage, not
            // as a block (CastleCoreGimmick taps NoteCoreDamage directly).
            //
            // But NOT everything left over is a wall. A live sweep fired three shots that hit a
            // midfield field-tower, an enemy archer, and bare ground, and the readback called all
            // three "성벽 N블록 파괴" (qa/aim-space-reachability.md §0-C). Field obstacles and the
            // flying beast are DestructibleBlocks too, so the readback was telling the player they
            // had breached a wall they never touched — the exact confusion the readback exists to
            // remove. The category is resolved from parentage, which the ownership code below
            // already establishes: under a CastleController it is the keep, otherwise it is field
            // furniture.
            if (!isGroundAnchor && !(this is CastleCoreGimmick))
            {
                ShotTraceDirector.NoteBlockDestroyed(
                    GetComponentInParent<CastleController>() != null
                        ? ShotTraceDirector.TargetKind.Wall
                        : ShotTraceDirector.TargetKind.FieldObstacle);
            }
            // Resolve and award ownership before CastleController can end the match.
            // EndGame snapshots the current score into the results card, so a fatal block
            // must be credited before that transition.
            var gameManager = GameManager.Instance;
            bool? resolvedDamageFromPlayer = damageFromPlayer
                ?? (gameManager != null ? gameManager.IsPlayerTurn : (bool?)null);
            var castle = GetComponentInParent<CastleController>();
            if (castle != null)
            {
                DeploymentController.Instance?.CreditBlockDestroyed(
                    castle.isPlayerCastle, blockWasCore: this is CastleCoreGimmick);
                if (gameManager != null && resolvedDamageFromPlayer.HasValue)
                {
                    bool attackerIsPlayer = resolvedDamageFromPlayer.Value;
                    if (castle.isPlayerCastle != attackerIsPlayer)
                    {
                        gameManager.AddScore(attackerIsPlayer, scoreValue);
                    }
                }
                castle.OnBlockDestroyed(this);
                CastleRuinFx.NotifyBlockDestroyed(this, castle);
            }

            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }

        // Falling-block impact damage: relative-velocity based with a hard cap so one broken
        // bridge tile cannot chain-react the whole 41x5 ground grid into dust. 45 still breaks
        // Wood (50HP) in ~1 hit and chips Stone/Iron, keeping collapse chains fun but bounded.
        public const float FallImpactDamageCap = 45f;
        public const float FallImpactMinSpeed = 2f;

        public static float CalculateFallImpactDamage(float relativeSpeed)
        {
            if (relativeSpeed <= FallImpactMinSpeed) return 0f;
            return Mathf.Min(relativeSpeed * 8f, FallImpactDamageCap);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (!isFalling || rb == null) return;

            float relativeSpeed = collision.relativeVelocity.magnitude;
            float damage = CalculateFallImpactDamage(relativeSpeed);
            if (damage <= 0f) return;

            var otherBlock = collision.gameObject.GetComponent<DestructibleBlock>();
            if (otherBlock != null && !otherBlock.isFalling)
            {
                // One link deeper than whatever fell on it. Recorded before the damage so a
                // block that dies on this hit carries the correct depth into DestroyBlock.
                // Max, not assignment: a block struck twice keeps the deepest chain that
                // reached it rather than the most recent one.
                otherBlock.collapseChainDepth = Mathf.Max(otherBlock.collapseChainDepth, collapseChainDepth + 1);
                otherBlock.TakeDamage(damage);
            }
            collision.gameObject.GetComponent<UnitController>()?.TakeDamage(damage);

            if (collision.gameObject.CompareTag("Ground"))
            {
                // Cycle 8: Spawn dust cloud on ground impact
                GameFeelVfx.SpawnCollapseDust(collision.GetContact(0).point, 1.2f, spriteRenderer != null ? spriteRenderer.sprite : null);
            }

            // The falling block itself takes damage from the impact
            TakeDamage(damage);
        }
    }
}
