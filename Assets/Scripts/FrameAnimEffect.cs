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
            float sizeJitter = worldSize * Random.Range(0.88f, 1.12f);

            // Uniform scale so the largest frame dimension spans worldSize units.
            Vector2 native = frames[0].bounds.size;
            float maxNative = Mathf.Max(native.x, native.y);
            if (maxNative > 0.0001f)
            {
                float scale = sizeJitter / maxNative;
                transform.localScale = new Vector3(scale, scale, 1f);
            }
            // Slight random roll keeps repeated sparks from looking stamped.
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-14f, 14f));
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
            if (sr.sprite != frames[index]) sr.sprite = frames[index];

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

