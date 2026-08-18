using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// One runtime path to the explosion art, because there were three and two of them were
    /// editor-only.
    ///
    /// The frames live at `Assets/Resources/GeneratedExplosionFrames/explosion_000..005.png` —
    /// inside Resources, so `Resources.LoadAll` reaches them in a WebGL build. Measured saturation
    /// runs 0.31 / 0.95 / 0.79 / 0.51 / 0.45 / 0.32 with under 1% near-white pixels, so this is
    /// colourful art that needs no tint. It was never the missing piece.
    ///
    /// What WAS missing: `ExplosiveGimmick` and `UnitController` both reached for
    /// `Assets/Prefabs/ExplosionEffect.prefab` through `AssetDatabase.LoadAssetAtPath` inside
    /// `#if UNITY_EDITOR`. That compiles away in a build, no scene assigns the field, and
    /// `Assets/Prefabs` is outside Resources — so a build had no way to reach the object that wires
    /// these frames up, fell through to a procedural fallback whose sprite loaded by the same
    /// editor-only path, and ended on `GameFeelVfx.GetParticleMaterial()`'s default texture: a pure
    /// white radial blob. Editor: correct. Build: white. Every test in this repo runs in the editor,
    /// which is why it survived.
    ///
    /// Sorted by name, because `Resources.LoadAll` does not promise an order and a shuffled
    /// explosion plays its frames out of sequence.
    /// </summary>
    public static class ExplosionFrames
    {
        public const string ResourceFolder = "GeneratedExplosionFrames";

        private static Sprite[] cached;

        /// <summary>
        /// The frames, name-sorted, cached after the first call. Empty (never null) when the folder
        /// is absent, so callers branch on length rather than on null.
        /// </summary>
        public static Sprite[] Load()
        {
            if (cached != null && cached.Length > 0) return cached;

            var loaded = Resources.LoadAll<Sprite>(ResourceFolder);
            if (loaded == null || loaded.Length == 0)
            {
                cached = new Sprite[0];
                return cached;
            }

            System.Array.Sort(loaded, (a, b) => string.Compare(a.name, b.name, System.StringComparison.Ordinal));
            cached = loaded;
            return cached;
        }

        /// <summary>Test seam: forget the cache so a test can assert the load rather than a
        /// previous test's result.</summary>
        public static void ClearCache() => cached = null;
    }
}
