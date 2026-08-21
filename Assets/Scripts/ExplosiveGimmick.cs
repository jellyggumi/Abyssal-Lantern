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
        private float temporaryPotencyMultiplier = 1f;
        // Optional permanent-source ceiling. Only owners such as Last Stand opt in; ordinary
        // barrels keep the uncapped base × temporary-gate composition.
        [SerializeField, HideInInspector] private bool hasMaximumEffectiveDamage;
        [SerializeField, HideInInspector] private float maximumEffectiveDamage;
        // Cached sibling body: OnDestroy previously did a GetComponent per destruction.
        private DestructibleBlock cachedBlock;
        private bool? damageFromPlayer;
        // Captured once at the moment this detonation's cause was decided (arrow/unit impact,
        // cannon splash, chain-reacting neighbor, or DestructibleBlock.TakeDamage transferring
        // context before a melee/arrow/cannon kill) and never recomputed from mutable turn
        // state here at explode time. Defaults to 1 so untouched/environmental detonations
        // (fall-through-floor Update below, or a bare Explode() call) apply no scaling.
        private float sourceMultiplier = 1f;

        /// <summary>
        /// Assigns the side + opening-volley multiplier that caused this detonation, captured
        /// once by the caller at action/projectile creation, so delayed chains keep both the
        /// ownership and the origin scale. sourceMultiplier defaults to 1 for the (still
        /// supported) owner-only call shape.
        /// </summary>
        public void SetDamageContext(bool? isPlayer, float sourceMultiplier = 1f)
        {
            damageFromPlayer = isPlayer;
            this.sourceMultiplier = sourceMultiplier;
        }
        /// <summary>
        /// Rebinds the damageable field-keg body after GameManager normalizes a shared prefab
        /// instance. The same prefab remains unbound when used as a launched UnitController.
        /// </summary>
        public void BindDestructibleBlock()
        {
            cachedBlock = GetComponent<DestructibleBlock>();
        }



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

        public float PermanentExplosionRadius
        {
            get
            {
                CaptureBasePotency();
                return baseExplosionRadius;
            }
        }

        public float PermanentExplosionDamage
        {
            get
            {
                CaptureBasePotency();
                return baseExplosionDamage;
            }
        }

        /// <summary>
        /// Replaces the permanent potency while preserving any active gate multiplier and cap.
        /// Hero Growth uses this path so a later gate expiry cannot erase it.
        /// </summary>
        public void SetPermanentPotency(float damage, float radius)
        {
            CaptureBasePotency();
            baseExplosionDamage = Mathf.Max(0f, damage);
            baseExplosionRadius = Mathf.Max(0f, radius);
            RefreshPotency();
        }

        /// <summary>
        /// Replaces permanent potency and installs an effective-damage ceiling owned by the
        /// permanent source. Temporary gates still scale radius, but cannot exceed the ceiling.
        /// </summary>
        public void SetPermanentPotency(float damage, float radius, float damageCap)
        {
            hasMaximumEffectiveDamage = true;
            maximumEffectiveDamage = Mathf.Max(0f, damageCap);
            SetPermanentPotency(damage, radius);
        }

        private void RefreshPotency()
        {
            float scaledDamage = baseExplosionDamage * temporaryPotencyMultiplier;
            explosionDamage = hasMaximumEffectiveDamage
                ? Mathf.Min(scaledDamage, maximumEffectiveDamage)
                : scaledDamage;
            explosionRadius = baseExplosionRadius * temporaryPotencyMultiplier;
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


            temporaryPotencyMultiplier = Mathf.Max(0f, multiplier);
            RefreshPotency();

            if (Application.isPlaying && duration > 0f)
            {
                potencyRevertRoutine = StartCoroutine(RevertPotencyAfter(duration));
            }
        }

        private IEnumerator RevertPotencyAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            temporaryPotencyMultiplier = 1f;
            RefreshPotency();
            potencyRevertRoutine = null;
        }

        public void Explode()
        {
            if (hasExploded) return;
            hasExploded = true;

            SpawnExplosionVisual();

            // The loudest beat in the loop: hit-stop + the biggest screen shake fired in
            // total silence, which read as a dropped frame rather than a detonation.
            GameFeelVfx.PlayExplosionSfx(explosionRadius);

            if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(0.075f);
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.45f, 0.2f);
            // Chained detonations each re-centre the held impact frame on the newest blast.
            GamePresentationDirector.Instance?.RefreshLinger(transform.position);

            float outgoingDamage = OneShotSiegeRules.ApplyDamageMultiplier(explosionDamage, sourceMultiplier);

            var blocks = DestructibleBlock.ActiveOrScene;
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var block = blocks[i];
                if (block != null && block.gameObject != gameObject && Vector2.Distance(transform.position, block.transform.position) <= explosionRadius)
                {
                    block.TakeDamage(outgoingDamage, damageFromPlayer, sourceMultiplier);
                }
            }

            var units = UnitController.ActiveOrScene;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                var unit = units[i];
                if (unit != null && Vector2.Distance(transform.position, unit.transform.position) <= explosionRadius)
                {
                    unit.TakeDamage(outgoingDamage, damageFromPlayer, sourceMultiplier);
                }
            }

            // Propagate the original attacker through static-keg chains. Launched Barrels are
            // handled by their UnitController HP/death path above and must not be forced early.
            foreach (var other in FindObjectsOfType<ExplosiveGimmick>())
            {
                if (other == null || other == this || other.hasExploded) continue;
                if (other.GetComponent<UnitController>() != null) continue;
                if (Vector2.Distance(transform.position, other.transform.position) > explosionRadius) continue;
                other.SetDamageContext(damageFromPlayer, sourceMultiplier);
                other.Explode();
            }

            // Content hook: destroyed kegs can drop hero-growth loot (60% chance).
            ItemDropper.TrySpawn(transform.position);
            if (GameManager.Instance != null && damageFromPlayer.HasValue)
                GameManager.Instance.AddScore(damageFromPlayer.Value, 100);
        }

        private void SpawnExplosionVisual()
        {
            if (!Application.isPlaying) return;

            // WHY THE EXPLOSION WAS WHITE, corrected twice during the meeting that found it.
            //
            // Not a wiring defect. `ExplosiveBarrel.prefab:176` SERIALISES a reference to
            // ExplosionEffect.prefab, and a serialised reference is a build dependency even outside
            // Resources — so the prefab loads, `ExplosionEffectConfigurator.Awake()` runs, and this
            // happens in the editor and in a build alike. The first two diagnoses (mine: "the
            // editor-only AssetDatabase load leaves it null in a build"; then "the fallback's
            // particles go white") were both wrong, and QA's serialisation check is what settled it.
            //
            // The real cause was the IMPORTER. The six frames in
            // Assets/Resources/GeneratedExplosionFrames shipped as `textureType: 0` (Default) with
            // `spriteMode: 0`, so `Resources.LoadAll<Sprite>` returned an EMPTY array — the art is
            // colourful (saturation 0.31-0.95, under 1% near-white) and simply was not a Sprite.
            // The configurator then took its null-texture branch, landing on
            // `GetParticleMaterial(null)` -> `GetDefaultParticleTexture()`: a pure white radial
            // blob, with `main.startColor = Color.white` on top of it.
            //
            // Third instance of that defect class here (fx_muzzle and fx_arcane were the first two),
            // and it survived because the regression test walks the keys `EffectSpriteLibrary`
            // declares and this folder is not among them. Fixed by the importer metas plus a test
            // that walks the FOLDER.
            //
            // The fallback below now loads the same frames, so a missing prefab is cosmetic rather
            // than a white flash.
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

            var frames = ExplosionFrames.Load();
            var sr = fallback.AddComponent<SpriteRenderer>();
            if (frames.Length > 0)
            {
                // Frame 1 rather than 0: measured saturation 0.95 against frame 0's 0.31, so the
                // single static frame behind the particles is the one that reads as fire.
                sr.sprite = frames[Mathf.Min(1, frames.Length - 1)];
                sr.color = Color.white;   // real art, so tinting would only mute it
            }
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

