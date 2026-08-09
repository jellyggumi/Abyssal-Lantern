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

        public virtual void TakeDamage(float damage)
        {
            if (isFalling && currentHP <= 0) return;
            currentHP -= damage;
            Color feedbackColor = blockData != null ? blockData.blockColor : new Color(0.65f, 0.55f, 0.42f, 1f);
            GameFeelVfx.SpawnDamageNumber(transform.position, damage, new Color(1f, 0.85f, 0.25f, 1f));
            GameFeelVfx.SpawnImpactBurst(transform.position, feedbackColor, Mathf.Clamp(damage / 35f, 0.45f, 1.8f), spriteRenderer != null ? spriteRenderer.sprite : null);
            // Dedicated star-flash impact frames on top of the procedural burst; size tracks damage.
            FrameAnimEffect.Spawn(EffectSpriteLibrary.Spark, transform.position + (Vector3)(UnityEngine.Random.insideUnitCircle * 0.15f),
                Mathf.Clamp(0.6f + damage / 60f, 0.6f, 1.6f), Color.white, 20f);
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
            if (currentHP <= 0) DestroyBlock();
        }

        private void UpdateVisuals()
        {
            if (spriteRenderer == null) return;
            float ratio = currentHP / maxHP;

            if (ratio <= 0.3f)
            {
                if (heavilyCrackedSprite == null && heavilyCrackedSpriteFactory != null)
                {
                    heavilyCrackedSprite = heavilyCrackedSpriteFactory();
                    heavilyCrackedSpriteFactory = null; // one-shot; release the closure once baked
                }
                spriteRenderer.sprite = heavilyCrackedSprite != null ? heavilyCrackedSprite : (crackedSprite != null ? crackedSprite : normalSprite);
            }
            else if (ratio <= 0.7f)
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

        protected virtual void DestroyBlock()
        {
            if (isDestroying) return;
            isDestroying = true;

            var chariot = GetComponent<MovingGimmick>();
            if (chariot != null && chariot.chariotMode)
            {
                chariot.HandleGameplayDestruction();
            }
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

            // Cycle 9 & 10: Scale hit-stop and screen shake on block destruction
            float hitStopDuration = this is CastleCoreGimmick ? 0.18f : 0.08f;
            float shakeMagnitude = this is CastleCoreGimmick ? 0.45f : 0.22f;
            float shakeDuration = this is CastleCoreGimmick ? 0.75f : 0.45f;
            if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(hitStopDuration);
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(shakeDuration, shakeMagnitude);
            var castle = GetComponentInParent<CastleController>();
            if (castle != null)
            {
                castle.OnBlockDestroyed(this);
                if (GameManager.Instance != null && castle.isPlayerCastle != GameManager.Instance.IsPlayerTurn)
                {
                    GameManager.Instance.AddScore(GameManager.Instance.IsPlayerTurn, scoreValue);
                }
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
            if (otherBlock != null && !otherBlock.isFalling) otherBlock.TakeDamage(damage);
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
