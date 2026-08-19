using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Loader for frame-animated effect art under Resources/Effects/{key}/.
    /// Frames follow the project convention: lowercase key prefix + zero-padded index
    /// (fx_spark_000.png, fx_spark_001.png, ...) sorted lexically.
    /// </summary>
    public static class EffectSpriteLibrary
    {
        public const string Spark = "fx_spark";
        public const string Dust = "fx_dust";
        public const string Sparkle = "fx_sparkle";
        public const string Spawn = "fx_spawn";
        public const string Eruption = "fx_eruption";
        public const string Petals = "fx_petals";
        public const string Frost = "fx_frost";
        // Magical rune/gate spawns (BuffRune/DebuffRune/PowerGate/ReduceGate/Multiply) keep
        // the original clean light-burst art; the physical "fx_spawn" key was reskinned with
        // a real stone/brick-dust texture (playtest QA: "벽돌 생기는 하얀색 이펙트" — the flat
        // white burst read as a placeholder, not a brick materializing) via god-tibo-imagen,
        // which would look wrong multiplied under a rune's blue/orange glow tint.
        public const string Arcane = "fx_arcane";

        // Cannon muzzle blast. Frames are derived from the drawn muzzle-flash art by a
        // scale/alpha ramp (punch out, expand, dissipate) rather than drawn one by one, so
        // every frame is the same blast and the registration cannot drift between them.
        public const string MuzzleBlast = "fx_muzzle";


        // Single-sprite particle art under Resources/Effects/particles/. These skin the
        // procedural ParticleSystems (impact bursts, collapse dust, vent columns) that used
        // to render a bare radial-gradient dot.
        public const string ParticleEmber = "particle_ember";
        public const string ParticleSmoke = "particle_smoke";
        public const string ParticlePetal = "particle_petal";

        // Ambient weather, one per stage — see StageWeather. Rain for the plain, snow for
        // the dunes' cold nights, ash for the volcanic gorge.
        public const string ParticleRain = "particle_rain";
        public const string ParticleSnow = "particle_snow";
        public const string ParticleAsh = "particle_ash";

        private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();
        private static readonly Dictionary<string, Sprite> singleCache = new Dictionary<string, Sprite>();

        public static Sprite[] LoadFrames(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (cache.TryGetValue(key, out var cached) && cached != null && cached.Length > 0) return cached;

            var frames = Resources.LoadAll<Sprite>($"Effects/{key}");
            if (frames != null && frames.Length > 0)
            {
                System.Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
            }
            cache[key] = frames;
            return frames;
        }

        /// <summary>Cached single-sprite loader for particle art. Null-safe without art.</summary>
        public static Sprite LoadParticleSprite(string name)
        {
            if (string.IsNullOrEmpty(name)) return null;
            if (singleCache.TryGetValue(name, out var cached) && cached != null) return cached;
            var sprite = Resources.Load<Sprite>($"Effects/particles/{name}");
            singleCache[name] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// One-shot world-space frame animation player for the dedicated effect strips
    /// (impact sparks, collapse dust, gate sparkles). Plays every frame once at the
    /// requested fps and destroys itself. Uses scaled time on purpose: hit-stop then
    /// freezes the effect on its brightest frame, which reads as the impact frame.
    /// </summary>
    public class FrameAnimEffect : MonoBehaviour
    {
        private Sprite[] frames;
        private float frameDuration;
        private float elapsed;
        private SpriteRenderer sr;
        private Color baseColor;

        /// <summary>
        /// The world size every frame is scaled to span, jitter included. Held rather than
        /// recomputed because the jitter is per-instance: re-rolling it on each frame change would
        /// make the effect flicker in size instead of holding one.
        /// </summary>
        private float targetWorldSize;


        /// <summary>
        /// How large an arrival burst may be relative to the thing that just arrived.
        ///
        /// The burst annotates an object, so it must not outsize it. One shipped call site was
        /// doing exactly that: a placed brick is 1.00 world units (CastleController.blockSizeX/Y)
        /// and its fx_spawn burst was 1.80 - 1.8x its own subject. The field-piece call sites are
        /// the counter-example that fixes the intended relationship: a vent is 2.40 units
        /// (EruptionVentGimmick.targetWorldSize) under a 2.10 burst, i.e. 0.875x. Measured against
        /// its siblings the brick case was off by roughly 2x, which is why it read as "the stone
        /// is too big" rather than as dust.
        ///
        /// 1.15 keeps a visible skirt of debris past the silhouette while leaving the object the
        /// largest thing at its own arrival.
        /// </summary>
        public const float ArrivalBurstRatio = 1.15f;

        /// <summary>Pure ratio math, separated so EditMode can assert it without a scene.</summary>
        public static float ArrivalBurstSizeFor(float subjectWorldSize, float fallbackWorldSize)
            => subjectWorldSize > 0.0001f ? subjectWorldSize * ArrivalBurstRatio : fallbackWorldSize;

        /// <summary>
        /// Arrival-burst world size for <paramref name="subject"/>, measured from what it actually
        /// renders rather than from a constant beside the call. A hand-written size cannot notice
        /// that the art it accompanies was rescaled; this can.
        /// </summary>
        public static float ArrivalBurstSize(GameObject subject, float fallbackWorldSize)
        {
            if (subject == null) return fallbackWorldSize;
            var sr = subject.GetComponentInChildren<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return fallbackWorldSize;
            return ArrivalBurstSizeFor(Mathf.Max(sr.bounds.size.x, sr.bounds.size.y), fallbackWorldSize);
        }

        public static FrameAnimEffect Spawn(string effectKey, Vector3 position, float worldSize, Color tint,
            float fps = 18f, int sortingOrder = 36)
        {
            if (!Application.isPlaying) return null;
            var frames = EffectSpriteLibrary.LoadFrames(effectKey);
            if (frames == null || frames.Length == 0) return null; // soft-fail without art

            var go = new GameObject($"Fx_{effectKey}");
            go.transform.position = position;
            var fx = go.AddComponent<FrameAnimEffect>();
            fx.Initialize(frames, worldSize, tint, fps, sortingOrder);
            return fx;
        }

        public void Initialize(Sprite[] frameSet, float worldSize, Color tint, float fps, int sortingOrder)
        {
            frames = frameSet;
            frameDuration = fps <= 0f ? 0.05f : 1f / fps;
            sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            baseColor = tint;
            sr.color = tint;
            sr.sortingOrder = sortingOrder;

            // Playtest note: every fx_spawn/fx_arcane burst was pixel-identical on repeat,
            // which reads as a stamped decal rather than a live effect once you see it a
            // few times in a row. A small per-instance size jitter (+/-12%) keeps repeated
            // spawns visually distinct without changing readability.
            targetWorldSize = worldSize * Random.Range(0.88f, 1.12f);

            ApplyScaleFor(frames[0]);

            // Slight random roll keeps repeated sparks from looking stamped.
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-14f, 14f));
        }

        /// <summary>
        /// Sizes the transform so THIS frame spans <see cref="targetWorldSize"/> units.
        ///
        /// Called on every frame change, not once at spawn. Scale used to be computed from
        /// frames[0] alone while <see cref="Update"/> swapped the sprite underneath it, so a strip
        /// whose frames differ in pixel size changed its drawn size mid-playback - the effect
        /// visibly jumped. Six of this project's nine strips differ that way, fx_sparkle worst at
        /// 77x77 followed by three 256x256 (a 3.3x step), so the jump was the normal case rather
        /// than the exception. Compensating here means mismatched art still plays at one size.
        /// </summary>
        private void ApplyScaleFor(Sprite frame)
        {
            if (frame == null) return;
            Vector2 native = frame.bounds.size;
            float maxNative = Mathf.Max(native.x, native.y);
            if (maxNative <= 0.0001f) return;
            float scale = targetWorldSize / maxNative;
            transform.localScale = new Vector3(scale, scale, 1f);
        }


        // Pure frame math kept static so EditMode tests can pin the contract.
        public static int FrameIndexAt(float elapsedSeconds, float frameSeconds, int frameCount)
        {
            if (frameCount <= 0) return 0;
            if (frameSeconds <= 0f) return frameCount; // degenerate -> finished
            return (int)(elapsedSeconds / frameSeconds);
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            int index = FrameIndexAt(elapsed, frameDuration, frames.Length);
            if (index >= frames.Length)
            {
                Destroy(gameObject);
                return;
            }
            if (sr.sprite != frames[index])
            {
                sr.sprite = frames[index];
                // Re-fit: frames within a strip are not all the same pixel size in this project.
                ApplyScaleFor(frames[index]);
            }

            // Playtest note: the old strip just cut to Destroy() on the final frame, which
            // read as an abrupt "pop" when frames.Length was small (e.g. 4-frame fx_spawn).
            // Ease the alpha out over the closing 25% of the strip so it visually settles
            // instead of vanishing on a hard cut.
            float totalDuration = frameDuration * frames.Length;
            if (totalDuration > 0f)
            {
                float t = Mathf.Clamp01(elapsed / totalDuration);
                const float fadeStart = 0.75f;
                if (t > fadeStart)
                {
                    float fadeT = (t - fadeStart) / (1f - fadeStart);
                    var c = baseColor;
                    c.a = baseColor.a * (1f - fadeT);
                    sr.color = c;
                }
            }
        }
    }
}

