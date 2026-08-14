using System;
using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Procedural 2D sprite animation layer plus optional generated sprite-frame playback.
    /// It keeps PNG-only units animated today, while allowing PerfectPixel exported
    /// frame PNGs under Resources/GeneratedUnitFrames/{Unit}/{State} to override the base pose.
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    public class UnitSpriteAnimator : MonoBehaviour
    {
        public float idleBobAmplitude = 0.045f;
        public float idleBobSpeed = 4.5f;
        public float walkStrideAmplitude = 5f;
        public float launchSpinDegreesPerSecond = 120f;
        public float launchStretch = 0.18f;
        public float attackPulse = 0.12f;
        public float frameAnimationFps = 8f;
        public bool useGeneratedSpriteFrames = true;
        // Ally/enemy team tints. Previously both were near-white (player = pure white,
        // enemy = a barely-saturated pink), so at a glance every unit looked the
        // same regardless of side. Bumped saturation on both so player (cool blue) and
        // enemy (warm red) read as distinct teams while still preserving sprite detail.
        // Defaults are sourced from UnitController's shared constants (also used by
        // ExplosiveGimmick's ApplyTeamTint) so units and gimmicks never drift apart.
        public Color playerTint = UnitController.AllySpriteTint;
        public Color enemyTint = UnitController.EnemySpriteTint;
        public Color flashTint = new Color(1f, 0.35f, 0.25f, 1f);




        private UnitController unit;
        private SpriteRenderer[] renderers = Array.Empty<SpriteRenderer>();
        private Vector3[] rendererBaseLocalPositions = Array.Empty<Vector3>();
        private SpriteRenderer primaryRenderer;
        private Transform visualRoot;
        private Vector3 visualRootBaseLocalPosition;
        private Vector3 visualRootBaseLocalScale = Vector3.one;
        private Quaternion visualRootBaseLocalRotation = Quaternion.identity;
        private Sprite[] idleFrames;
        private Sprite[] walkFrames;
        private Sprite[] attackFrames;
        private Sprite[] launchFrames;
        private Sprite[] deadFrames;
        private Sprite[] activeFrames;
        private float facingSign = 1f;
        private float randomPhase;
        private float pulseTimer;
        private float flashTimer;

        /// <summary>Active buff/debuff colour and its blend weight; weight 0 means none.</summary>
        private Color statusTint = Color.white;
        private float statusTintWeight;
        private float frameTimer;
        private int frameIndex;
        private Vector3 previousPosition;

        private void Awake()
        {
            unit = GetComponent<UnitController>();

            // Programmatically move root SpriteRenderer presentation to a child GameObject so
            // visual bobbing, frame swapping, and launch squash/stretch do not distort the
            // physics root transform or colliders. In play mode Destroy is delayed, so the
            // disabled original renderer is explicitly excluded from the animation list below.
            SpriteRenderer originalRootRenderer = GetComponent<SpriteRenderer>();
            if (originalRootRenderer != null)
            {
                var visualGo = new GameObject("Visual");
                visualGo.transform.SetParent(transform, false);
                visualGo.transform.localPosition = Vector3.zero;
                visualGo.transform.localRotation = Quaternion.identity;
                visualGo.transform.localScale = Vector3.one;

                var childRenderer = visualGo.AddComponent<SpriteRenderer>();
                childRenderer.sprite = SpriteAtlasPacker.Instance != null
                    ? SpriteAtlasPacker.Instance.GetPackedSprite(originalRootRenderer.sprite)
                    : originalRootRenderer.sprite;
                childRenderer.color = originalRootRenderer.color;
                childRenderer.material = originalRootRenderer.material;
                childRenderer.sortingLayerID = originalRootRenderer.sortingLayerID;
                childRenderer.sortingLayerName = originalRootRenderer.sortingLayerName;
                childRenderer.sortingOrder = originalRootRenderer.sortingOrder;
                childRenderer.flipX = originalRootRenderer.flipX;
                childRenderer.flipY = originalRootRenderer.flipY;
                childRenderer.drawMode = originalRootRenderer.drawMode;
                childRenderer.size = originalRootRenderer.size;

                originalRootRenderer.enabled = false;
                if (Application.isPlaying)
                    Destroy(originalRootRenderer);
                else
                    DestroyImmediate(originalRootRenderer);
            }

            CacheRenderers(originalRootRenderer);
            CaptureBaseScale();
            randomPhase = UnityEngine.Random.value * Mathf.PI * 2f;
            previousPosition = transform.position;
            LoadGeneratedFrameSets();
            // Team identity applied at spawn through the same composed path LateUpdate uses, so a
            // unit is never briefly the wrong colour on its first frame.
            SetRendererColor(CurrentBaseTint());
        }

        private void Start()
        {
            CacheRenderers(null);
            CaptureBaseScale();
            LoadGeneratedFrameSets();
        }

        private void CacheRenderers(SpriteRenderer rendererToIgnore)
        {
            var found = GetComponentsInChildren<SpriteRenderer>(true);
            var usable = new List<SpriteRenderer>(found.Length);
            foreach (var sr in found)
            {
                if (sr == null || sr == rendererToIgnore || !sr.enabled) continue;
                usable.Add(sr);
            }

            renderers = usable.ToArray();
            primaryRenderer = renderers.Length > 0 ? renderers[0] : null;
            visualRoot = primaryRenderer != null ? primaryRenderer.transform : null;
            rendererBaseLocalPositions = new Vector3[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                rendererBaseLocalPositions[i] = renderers[i].transform.localPosition;
            }
        }

        public void CaptureBaseScale()
        {
            if (visualRoot == null && primaryRenderer != null) visualRoot = primaryRenderer.transform;
            if (visualRoot == null) return;

            visualRootBaseLocalPosition = visualRoot.localPosition;
            visualRootBaseLocalScale = visualRoot.localScale;
            visualRootBaseLocalRotation = visualRoot.localRotation;
            facingSign = unit != null && unit.isPlayerUnit ? 1f : -1f;
        }

        private void LoadGeneratedFrameSets()
        {
            if (!useGeneratedSpriteFrames || unit == null) return;

            string unitName = unit.unitType.ToString();
            idleFrames = LoadFrames(unitName, "Idle");
            walkFrames = LoadFrames(unitName, "Walk");
            attackFrames = LoadFrames(unitName, "Attack");
            launchFrames = LoadFrames(unitName, "Launch");
            deadFrames = LoadFrames(unitName, "Dead");
            activeFrames = null;
            frameIndex = 0;
            frameTimer = 0f;
        }

        private static Sprite[] LoadFrames(string unitName, string stateName)
        {
            var frames = Resources.LoadAll<Sprite>($"GeneratedUnitFrames/{unitName}/{stateName}");
            if (frames == null || frames.Length == 0) return null;

            Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            return frames;
        }

        private void LateUpdate()
        {
            if (unit == null) return;

            Vector3 velocity = (transform.position - previousPosition) / Mathf.Max(Time.deltaTime, 0.0001f);
            previousPosition = transform.position;

            if (Mathf.Abs(velocity.x) > 0.05f)
            {
                float direction = Mathf.Sign(velocity.x);
                float defaultFacing = unit.isPlayerUnit ? 1f : -1f;
                facingSign = direction * defaultFacing;
            }
            else if (unit.CurrentState == UnitState.Idle)
            {
                facingSign = unit.isPlayerUnit ? 1f : -1f;
            }

            UpdateFrameAnimation(velocity);

            float bob = unit.CurrentState == UnitState.Grounded || unit.CurrentState == UnitState.Attacking
                ? Mathf.Sin(Time.time * idleBobSpeed + randomPhase) * idleBobAmplitude
                : 0f;

            float stretch = unit.CurrentState == UnitState.Launched
                ? Mathf.Clamp01(velocity.magnitude / 16f) * launchStretch
                : 0f;

            if (pulseTimer > 0f) pulseTimer -= Time.deltaTime;
            float pulse = pulseTimer > 0f ? Mathf.Sin((pulseTimer / 0.18f) * Mathf.PI) * attackPulse : 0f;

            float movingStride = unit.CurrentState == UnitState.Grounded && Mathf.Abs(velocity.x) > 0.08f
                ? Mathf.Sin(Time.time * idleBobSpeed * 1.8f + randomPhase) * walkStrideAmplitude
                : 0f;
            float launchSpin = unit.CurrentState == UnitState.Launched
                ? Mathf.Repeat(Time.time * launchSpinDegreesPerSecond * Mathf.Sign(Mathf.Abs(velocity.x) > 0.01f ? velocity.x : facingSign), 360f)
                : 0f;

            if (visualRoot != null)
            {
                Vector3 animatedScale = visualRootBaseLocalScale;
                animatedScale.x = Mathf.Abs(animatedScale.x) * facingSign * (1f + stretch + pulse);
                animatedScale.y *= 1f - stretch * 0.45f + pulse * 0.5f;
                visualRoot.localScale = animatedScale;
                visualRoot.localPosition = visualRootBaseLocalPosition + new Vector3(
                    unit.CurrentState == UnitState.Grounded ? Mathf.Sin(Time.time * idleBobSpeed * 1.8f + randomPhase) * 0.015f : 0f,
                    bob,
                    0f);
                visualRoot.localRotation = visualRootBaseLocalRotation * Quaternion.Euler(0f, 0f, launchSpin + movingStride);
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                var sr = renderers[i];
                if (sr == null || sr.transform == transform || sr.transform == visualRoot) continue;
                sr.transform.localPosition = rendererBaseLocalPositions[i];
            }

            // One owner for the colour channel, three writers composed in priority order.
            //
            // The buff tint used to be written straight onto sr.color from
            // UnitController.ApplyBuff, and this method overwrote it with the team tint on the very
            // next frame — so an active buff rendered for ZERO frames. The hit flash survived only
            // because it lives inside this pipeline rather than outside it. Anything that wants to
            // colour a unit has to come through here for the same reason.
            //
            // Priority: hit flash beats buff (a unit being struck reads as struck first), buff beats
            // team tint, and the team identity is never fully discarded — the buff is BLENDED, so
            // blue and red stay tellable apart while buffed. That matters more than buff visibility:
            // a player who cannot tell whose soldier it is has lost more information than one who
            // cannot tell it is buffed.
            if (flashTimer > 0f)
            {
                flashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(flashTimer / 0.12f);
                SetRendererColor(Color.Lerp(CurrentBaseTint(), flashTint, t));
            }
            else
            {
                SetRendererColor(CurrentBaseTint());
            }
        }

        /// <summary>
        /// The unit's colour before any hit flash: team identity, blended toward an active
        /// buff/debuff tint when one is set.
        /// </summary>
        private Color CurrentBaseTint()
        {
            Color team = unit != null && unit.isPlayerUnit ? playerTint : enemyTint;
            if (statusTintWeight <= 0f) return team;
            return Color.Lerp(team, statusTint, Mathf.Clamp01(statusTintWeight));
        }

        /// <summary>
        /// Sets a status tint (buff, debuff) that survives the per-frame team-tint write.
        ///
        /// <paramref name="weight"/> 0 clears it. It is a blend rather than a replacement so the
        /// team colour is never lost; 0.55 reads clearly as "something is on this unit" while blue
        /// and red stay separable.
        /// </summary>
        public void SetStatusTint(Color tint, float weight)
        {
            statusTint = tint;
            statusTintWeight = weight;
        }

        private void UpdateFrameAnimation(Vector3 velocity)
        {
            if (!useGeneratedSpriteFrames || primaryRenderer == null) return;

            Sprite[] requestedFrames = GetFramesForCurrentState(velocity);
            if (requestedFrames == null || requestedFrames.Length == 0) return;

            if (requestedFrames != activeFrames)
            {
                activeFrames = requestedFrames;
                frameIndex = 0;
                frameTimer = 0f;
            }

            frameTimer += Time.deltaTime;
            float frameDuration = 1f / Mathf.Max(1f, frameAnimationFps);
            while (frameTimer >= frameDuration)
            {
                frameTimer -= frameDuration;
                // The attack clip plays ONCE and then holds its first frame; everything else loops.
                //
                // Holding frame 0 rather than the last frame is a measured choice, not a preference.
                // Design lane scored silhouette distance (1 - IoU against idle) for every attack
                // frame: the recovered stance at frame 4 scores 0.235 for the knight and 0.077 for
                // the archer, and the archer's own walk-cycle noise floor is 0.225 — so parking on
                // frame 4 would make a fighting archer indistinguishable from an idle one, erasing
                // exactly the distinction this change exists to create. Frame 0 is the blade drawn
                // back: 0.297 / 0.400, clearing the floor by 1.53x and 1.78x, and it reads as
                // "wound up for the next blow", which is what a soldier between swings is doing.
                bool holdingSwing = unit != null && unit.CurrentState == UnitState.Attacking
                                    && activeFrames == attackFrames;
                if (holdingSwing && frameIndex + 1 >= activeFrames.Length)
                {
                    frameIndex = 0;
                    frameTimer = 0f;
                    break;
                }
                frameIndex = (frameIndex + 1) % activeFrames.Length;
            }

            Sprite origSprite = activeFrames[Mathf.Clamp(frameIndex, 0, activeFrames.Length - 1)];
            primaryRenderer.sprite = SpriteAtlasPacker.Instance != null
                ? SpriteAtlasPacker.Instance.GetPackedSprite(origSprite)
                : origSprite;
        }

        private Sprite[] GetFramesForCurrentState(Vector3 velocity)
        {
            switch (unit.CurrentState)
            {
                case UnitState.Launched:
                    return launchFrames ?? idleFrames;
                case UnitState.Attacking:
                    // The swing is an EVENT, not a state texture.
                    //
                    // This used to hand back attackFrames for the whole time the unit sat in
                    // Attacking, and UpdateFrameAnimation modulo-loops whatever it is given. A
                    // 5-frame clip at 8fps is 0.625s against a 1.5s knight cooldown, so the knight
                    // performed 2.40 swings per single damage event (archer: 1.52). The animation
                    // was not missing — it was reporting a hit count that never happened, which is
                    // worse, because the player counts swings to read what a soldier is doing.
                    //
                    // Sample evidence (design/unit-action-legibility.md): of five verifiable
                    // comparable titles, all five play one swing per damage event and none loops
                    // through cooldown. Age of Empires II computes the damage instant at half the
                    // animation length rather than looping; Battle Cats stands in a separate wait
                    // state for the 449 frames its 151-frame attack does not cover.
                    // Attack frames stay selected through the cooldown; UpdateFrameAnimation plays
                    // the clip once and then parks on frame 0 (the windup). Returning idleFrames here
                    // instead would have made an engaged soldier look like a waiting one, which the
                    // silhouette measurements rule out.
                    return attackFrames ?? walkFrames ?? idleFrames;
                case UnitState.Grounded:
                    return Mathf.Abs(velocity.x) > 0.08f ? (walkFrames ?? idleFrames) : idleFrames;
                case UnitState.Dead:
                    return deadFrames ?? idleFrames;
                default:
                    return idleFrames;
            }
        }

        public void FlashHit()
        {
            flashTimer = 0.12f;
        }

        /// <summary>
        /// Called by <c>UnitController.TryAttack</c> at the instant a swing is committed.
        ///
        /// Restarts the attack clip from frame 0, which is what makes one swing mean one hit: the
        /// clip plays through once (<c>UpdateFrameAnimation</c> parks it on frame 0 at the end
        /// instead of wrapping), and the only thing that starts it again is another committed
        /// attack. The swing count therefore equals the damage-event count by construction — there
        /// is no rate to keep in sync, which is what went wrong when the clip free-looped.
        /// </summary>
        public void PulseAttack()
        {
            pulseTimer = 0.18f;
            frameIndex = 0;
            frameTimer = 0f;
        }


        private void SetRendererColor(Color color)
        {
            foreach (var sr in renderers)
            {
                if (sr != null) sr.color = color;
            }
        }
    }
}
