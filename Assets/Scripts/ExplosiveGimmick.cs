using System.Collections;
using UnityEngine;

namespace CastleBusters
{
    public class ExplosiveGimmick : MonoBehaviour
    {
        public float explosionRadius = 2.2f;
        public float explosionDamage = 80f;
        public GameObject explosionEffectPrefab;

        [Header("Presentation Scale")]
        [Tooltip("On-screen world size (units) of the barrel. Source sprite is ~12.5u; rescaled to sit beside the ~1u blocks.")]
        public float targetWorldSize = 1.7f; // Scaled up by 1.41x (from 1.2f to 1.7f) for usability and playability

        private bool hasExploded = false;

        // Base potency captured once so EventGateGimmick's PowerUp/PowerDown/Reduce effects
        // can be applied as a temporary multiplier (matching how the same gate's velocity/
        // damage-speed effect on UnitController already reverts via ApplyBuff/ApplyDebuff)
        // instead of permanently compounding explosionRadius/explosionDamage on every pass.
        private bool basePotencyCaptured;
        private float baseExplosionRadius;
        private float baseExplosionDamage;
        private Coroutine potencyRevertRoutine;
        // Cached sibling body: OnDestroy previously did a GetComponent per destruction.
        private DestructibleBlock cachedBlock;

        // Also called (idempotently) from ApplyTemporaryPotencyMultiplier: a barrel that gets
        // scaled by a gate before its own Awake has run (e.g. AddComponent<ExplosiveGimmick>()
        // followed immediately by a gate call, as in test/edit-mode setup code) must still
        // capture whatever explosionRadius/explosionDamage are configured to at that point,
        // not silently base off the float defaults.
        private void CaptureBasePotency()
        {
            if (basePotencyCaptured) return;
            basePotencyCaptured = true;
            baseExplosionRadius = explosionRadius;
            baseExplosionDamage = explosionDamage;
        }

        private void Awake()
        {
            CaptureBasePotency();

            cachedBlock = GetComponent<DestructibleBlock>();


            bool isStage1 = GameManager.Instance != null && GameManager.Instance.currentStage == StageId.Stage1;
            var sr = GetComponent<SpriteRenderer>();

            if (sr != null)
            {
                sr.sortingOrder = 2; // Render in front of ground, behind units
                // Stage1 uses its dedicated barrel skin; other stages retain the legacy keg.
                string artKey = isStage1
                    ? GimmickSpriteLibrary.Stage1Barrel
                    : GimmickSpriteLibrary.Barrel;
                GimmickSpriteLibrary.TryApply(sr, artKey, Color.white);
            }
            ApplyPresentationScale(sr);
            // Sparking-fuse loop (4 frames); TryAttach preserves the world size chosen above.
            GimmickFrameAnimator.TryAttach(gameObject,
                isStage1 ? GimmickAnimLibrary.Stage1BarrelAnim : GimmickAnimLibrary.BarrelAnim, isStage1 ? 4f : 8f);
        }
        /// <summary>
        /// Recolors the barrel to match the launching side, same as UnitController.AllySpriteTint/
        /// EnemySpriteTint used by UnitSpriteAnimator for real units. LaunchManager.SpawnAndLaunchOne
        /// bolts a UnitController onto this gimmick and only learns isPlayerUnit *after* Instantiate
        /// (i.e. after Awake already ran with the neutral Color.white), so callers must invoke this
        /// once the launching side is known. Pre-placed neutral kegs that are never launched (e.g.
        /// level-design obstacles) simply never call this and keep their neutral Awake-time color.
        /// </summary>
        public void ApplyTeamTint(bool isPlayerUnit)
        {
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = isPlayerUnit ? UnitController.AllySpriteTint : UnitController.EnemySpriteTint;
        }


        // Rescales the oversized barrel sprite + collider to 'targetWorldSize' so it is
        // proportional to the rescaled blocks and characters.
        private void ApplyPresentationScale(SpriteRenderer sr)
        {
            targetWorldSize = Mathf.Max(0.05f, targetWorldSize);
            if (sr == null || sr.sprite == null) return;

            Vector2 native = sr.sprite.bounds.size;
            float maxNative = Mathf.Max(native.x, native.y);
            if (maxNative <= 0.0001f) return;

            float scale = targetWorldSize / maxNative;
            transform.localScale = new Vector3(scale, scale, 1f);

            if (TryGetComponent<BoxCollider2D>(out var box))
            {
                box.size = native;
                box.offset = sr.sprite.bounds.center;
            }
        }

        private void OnDestroy()
        {
            if (!hasExploded && Application.isPlaying && cachedBlock != null && cachedBlock.currentHP <= 0)
            {
                Explode();
            }
        }

        /// <summary>
        /// Temporarily scales explosionRadius/explosionDamage by 'multiplier' for 'duration'
        /// seconds, then reverts to the base value captured in Awake. A second call while one
        /// is still active restarts the window from the new multiplier rather than compounding
        /// on top of the previous one.
        /// </summary>
        public void ApplyTemporaryPotencyMultiplier(float multiplier, float duration)
        {
            CaptureBasePotency();

            if (potencyRevertRoutine != null)
            {
                StopCoroutine(potencyRevertRoutine);
                potencyRevertRoutine = null;
            }


            explosionRadius = baseExplosionRadius * multiplier;
            explosionDamage = baseExplosionDamage * multiplier;

            if (Application.isPlaying && duration > 0f)
            {
                potencyRevertRoutine = StartCoroutine(RevertPotencyAfter(duration));
            }
        }

        private IEnumerator RevertPotencyAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            explosionRadius = baseExplosionRadius;
            explosionDamage = baseExplosionDamage;
            potencyRevertRoutine = null;
        }

        public void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            SpawnExplosionVisual();

            if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(0.075f);
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.45f, 0.2f);

            var blocks = DestructibleBlock.ActiveOrScene;
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var block = blocks[i];
                if (block != null && block.gameObject != gameObject && Vector2.Distance(transform.position, block.transform.position) <= explosionRadius)
                {
                    block.TakeDamage(explosionDamage);
                }
            }

            var units = UnitController.ActiveOrScene;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                var unit = units[i];
                if (unit != null && Vector2.Distance(transform.position, unit.transform.position) <= explosionRadius)
                {
                    unit.TakeDamage(explosionDamage);
                }
            }

            // Content hook: destroyed kegs can drop hero-growth loot (60% chance).
            ItemDropper.TrySpawn(transform.position);
            if (GameManager.Instance != null) GameManager.Instance.AddScore(GameManager.Instance.IsPlayerTurn, 100);
        }

        private void SpawnExplosionVisual()
        {
            if (!Application.isPlaying) return;
            if (explosionEffectPrefab == null)
            {
#if UNITY_EDITOR
                explosionEffectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExplosionEffect.prefab");
#endif
            }
            if (explosionEffectPrefab != null)
            {
                var effect = Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
                effect.transform.localScale = Vector3.one * 0.75f;
                return;
            }

            var fallback = new GameObject("ExplosionEffect")
            {
                transform =
                {
                    position = transform.position,
                    localScale = Vector3.one * 0.75f
                }
            };
            var sr = fallback.AddComponent<SpriteRenderer>();
            Sprite origSprite = null;
#if UNITY_EDITOR
            origSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/explosion.png");
#endif
            sr.sprite = SpriteAtlasPacker.Instance != null ? SpriteAtlasPacker.Instance.GetPackedSprite(origSprite) : origSprite;
            sr.color = new Color(1f, 0.5f, 0f, 0.8f);
            sr.sortingOrder = 30;

            var particles = fallback.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.45f);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.15f, 0.95f), new Color(1f, 0.2f, 0f, 0.55f));
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = true;

            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 36) });

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.28f;

            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 31;
            var ember = EffectSpriteLibrary.LoadParticleSprite(EffectSpriteLibrary.ParticleEmber);
            if (ember != null)
            {
                var sheet = particles.textureSheetAnimation;
                sheet.enabled = true;
                sheet.mode = ParticleSystemAnimationMode.Sprites;
                sheet.AddSprite(ember);
                renderer.sharedMaterial = GameFeelVfx.GetParticleMaterial(ember.texture as Texture2D);
            }
            else
            {
                renderer.sharedMaterial = GameFeelVfx.GetParticleMaterial();
            }

            if (Application.isPlaying) Destroy(fallback, 0.65f); else DestroyImmediate(fallback);
        }

        private void Update()
        {
            if (transform.position.y < ChariotRules.KillPlaneY)
            {
                if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
            }
        }
    }
}

