using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Makes a cannon shot legible. Artillery fires from off to one side at something the
    /// player is usually not looking at, so without a mark left in the air the only evidence
    /// it worked was a block disappearing — the cannon was doing its job invisibly.
    ///
    /// Three marks, each answering a different question. The trail answers "where did it go":
    /// a burning arc that hangs long enough to read the trajectory after the shell has already
    /// landed. The shell afterimages answer "how fast": discrete ghosts spaced by travel, so a
    /// fast shell leaves a stretched dotted line and a lobbed one leaves a tight arc. The
    /// barrel afterimage answers "did it fire": a ghost of the gun at full recoil, which is the
    /// moment the eye misses when it happens in a tenth of a second.
    ///
    /// All of it is cosmetic and self-destructing. Nothing here is allowed to touch a
    /// collider, a rigidbody, or a lifetime that the balance is written against.
    /// </summary>
    public static class CannonShotVisuals
    {
        /// <summary>Long enough to still be on screen when the shell lands, short enough that
        /// two volleys do not braid together into one unreadable smear.</summary>
        private const float TrailSeconds = 0.85f;

        private static Material spriteMaterial;

        private static Material SpriteMaterial()
        {
            // Cached: a Material per shell would leak one per shot, and every trail wants the
            // same unlit sprite pass.
            if (spriteMaterial == null)
            {
                var shader = Shader.Find("Sprites/Default");
                if (shader != null) spriteMaterial = new Material(shader);
            }
            return spriteMaterial;
        }

        private static Color HotColor(bool isPlayerShell) => isPlayerShell
            ? new Color(1f, 0.80f, 0.35f, 0.95f)   // player: warm gold
            : new Color(1f, 0.48f, 0.28f, 0.95f);  // enemy: hotter red, readable at a glance

        /// <summary>The burning arc. Tinted by side so a player can tell whose shell is in the
        /// air without following it back to the gun.</summary>
        public static void AttachShellTrail(GameObject shell, bool isPlayerShell)
        {
            if (shell == null) return;

            var trail = shell.AddComponent<TrailRenderer>();
            trail.time = TrailSeconds;
            trail.minVertexDistance = 0.05f;
            trail.autodestruct = false;
            trail.numCapVertices = 2;
            trail.alignment = LineAlignment.View;
            trail.sortingOrder = 4;   // under the shell (5) so the ball stays the bright point

            // Authored in local units, and the shell is scaled down to match its collider, so
            // the width is divided back out — otherwise the trail is a hairline on a small ball.
            float inverseScale = 1f / Mathf.Max(0.0001f, shell.transform.localScale.x);
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.20f * inverseScale),
                new Keyframe(1f, 0.02f * inverseScale));

            var hot = HotColor(isPlayerShell);
            var smoke = new Color(0.42f, 0.40f, 0.40f, 0.35f);
            trail.colorGradient = new Gradient
            {
                colorKeys = new[]
                {
                    new GradientColorKey(hot, 0f),
                    new GradientColorKey(smoke, 0.55f),
                    new GradientColorKey(smoke, 1f),
                },
                alphaKeys = new[]
                {
                    new GradientAlphaKey(0.95f, 0f),
                    new GradientAlphaKey(0.45f, 0.5f),
                    new GradientAlphaKey(0f, 1f),
                },
            };

            var material = SpriteMaterial();
            if (material != null) trail.material = material;

            var emitter = shell.AddComponent<ShellAfterimage>();
            emitter.tint = hot;
        }

        /// <summary>A ghost of the barrel at full recoil, left behind as the gun springs back.</summary>
        public static void SpawnBarrelAfterimage(SpriteRenderer barrel, bool isPlayerCannon)
        {
            if (barrel == null || barrel.sprite == null) return;

            var ghost = new GameObject("CannonBarrelAfterimage");
            ghost.transform.SetPositionAndRotation(barrel.transform.position, barrel.transform.rotation);
            ghost.transform.localScale = barrel.transform.lossyScale;

            var sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = barrel.sprite;
            sr.flipX = barrel.flipX;
            sr.sortingLayerID = barrel.sortingLayerID;
            sr.sortingOrder = barrel.sortingOrder - 1;   // behind the real barrel, never over it

            var hot = HotColor(isPlayerCannon);
            sr.color = new Color(hot.r, hot.g, hot.b, 0.55f);

            var fade = ghost.AddComponent<FadingGhost>();
            fade.lifetime = 0.22f;
        }
    }

    /// <summary>
    /// Drops a fading copy of the shell at a fixed cadence. Spacing comes from travel rather
    /// than from a distance test, so the gaps themselves report speed.
    /// </summary>
    public sealed class ShellAfterimage : MonoBehaviour
    {
        public Color tint = Color.white;

        private const float EmitIntervalSeconds = 0.055f;
        private const float GhostLifetime = 0.30f;

        private SpriteRenderer source;
        private float nextEmitAt;

        private void Awake()
        {
            source = GetComponent<SpriteRenderer>();
            nextEmitAt = Time.time;
        }

        private void LateUpdate()
        {
            // LateUpdate, not FixedUpdate: the ghost must copy the pose the player was shown,
            // and physics has not necessarily written it yet at fixed-step time.
            if (source == null || source.sprite == null) return;
            if (Time.time < nextEmitAt) return;
            nextEmitAt = Time.time + EmitIntervalSeconds;

            var ghost = new GameObject("CannonShellAfterimage");
            ghost.transform.SetPositionAndRotation(transform.position, transform.rotation);
            ghost.transform.localScale = transform.lossyScale;

            var sr = ghost.AddComponent<SpriteRenderer>();
            sr.sprite = source.sprite;
            sr.sortingLayerID = source.sortingLayerID;
            sr.sortingOrder = source.sortingOrder - 1;
            sr.color = new Color(tint.r, tint.g, tint.b, 0.42f);

            var fade = ghost.AddComponent<FadingGhost>();
            fade.lifetime = GhostLifetime;
        }
    }

    /// <summary>Fades a sprite out and removes itself. Unscaled so a hit-stop does not freeze
    /// a row of ghosts on screen.</summary>
    public sealed class FadingGhost : MonoBehaviour
    {
        public float lifetime = 0.25f;

        private SpriteRenderer sr;
        private float elapsed;
        private float startAlpha;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            startAlpha = sr != null ? sr.color.a : 0f;
        }

        private void Update()
        {
            if (sr == null) { Destroy(gameObject); return; }

            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, lifetime));
            var c = sr.color;
            // Squared falloff: holds its shape briefly, then leaves quickly, which reads as a
            // trail rather than as a row of stationary copies.
            sr.color = new Color(c.r, c.g, c.b, startAlpha * (1f - t) * (1f - t));

            if (t >= 1f) Destroy(gameObject);
        }
    }
}
