using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Central loader for dedicated gimmick/UI art under Resources/Gimmicks/.
    /// Every gimmick used to reuse the stone-block texture with a color tint, which made
    /// runes/gates/rams read as "floating misplaced blocks" in playtests. Each gimmick now
    /// asks this library for its own silhouette; a missing asset returns null so callers
    /// keep their legacy tinted-block fallback and the game never hard-fails on art.
    /// </summary>
    public static class GimmickSpriteLibrary
    {
        public const string RallyRune = "gimmick_rally_rune";
        public const string HexRune = "gimmick_hex_rune";
        public const string Gate = "gimmick_gate";
        public const string GatePower = "gimmick_gate_power";
        public const string GateReduce = "gimmick_gate_reduce";
        public const string Ram = "gimmick_ram";
        public const string Barrel = "gimmick_barrel";
        public const string Cannon = "gimmick_cannon";
        public const string CannonBarrel = "gimmick_cannon_barrel";
        public const string Shell = "gimmick_shell";
        public const string MuzzleFlash = "gimmick_muzzle_flash";
        public const string WallBrick = "gimmick_wall_brick";
        public const string WallBrickCracked = "gimmick_wall_brick_cracked";
        public const string GaugeFrame = "ui_gauge_frame";
        public const string Core = "gimmick_core";
        public const string ButtonCard = "ui_button_card";
        public const string LastStandButton = "last_stand_button";
        public const string VentMagma = "gimmick_vent_magma";
        public const string VentPetal = "gimmick_vent_petal";
        public const string VentFrost = "gimmick_vent_frost";
        public const string SpikeTrapDormant = "gimmick_spiketrap_dormant";
        public const string SpikeTrapArmed = "gimmick_spiketrap_armed";
        public const string Stage1Card = "ui_stage1_card";
        public const string Stage2Card = "ui_stage2_card";
        public const string Stage3Card = "ui_stage3_card";
        public const string Stage1Barrel = "gimmick_stage1_barrel";
        public const string Stage2SpikeTrapDormant = "gimmick_stage2_spiketrap_dormant";
        public const string Stage2SpikeTrapArmed = "gimmick_stage2_spiketrap_armed";
        public const string Stage3FrostVent = "gimmick_stage3_frost_vent";
        /// <summary>Wordless drag-back gesture cue (first-play coach, drag-from-anywhere).</summary>
        public const string DragGesture = "ui_drag_gesture";
        /// <summary>Placement preview frame for deploy-only cards; tinted by placement legality.</summary>
        public const string DeployGhost = "ui_deploy_ghost";
        /// <summary>
        /// Aim-preview landing reticle. Greyscale on purpose: <see cref="LaunchManager"/> tints it
        /// amber normally and blue when the arc ends on your own keep, and a pre-coloured sprite
        /// would multiply the two. Not the post-impact badge — that one was deleted on survey
        /// evidence (`dbcfed78f`) and is a different question from previewing where a shot will land.
        /// </summary>
        public const string ImpactMarker = "ui_impact_marker";

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var sprite = Resources.Load<Sprite>($"Gimmicks/{key}");
#if UNITY_EDITOR
            // Read-only Editor convenience: an unimported asset still shows up while authoring.
            //
            // What used to be here also WROTE. On a miss it set `importer.textureType = Sprite` and
            // called `SaveAndReimport()`, rewriting the tracked `.meta` on disk. That turned the
            // EditMode suite into a repair tool: a run would silently fix four `Gimmicks` metas, go
            // green, and - if the working-tree change was never committed - ship a build that still
            // rendered nothing. A test that repairs its subject cannot fail, and that false pass is
            // why `fx_muzzle`, `fx_arcane`, and the white explosion each survived a green suite.
            //
            // The load stays (authoring convenience is real); the write is gone. A broken importer
            // is now visible as a broken importer, and `ResourceSpriteImportTests` fails on it.
            if (sprite == null)
            {
                string path = $"Assets/Resources/Gimmicks/{key}.png";
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }
#endif
            cache[key] = sprite;
            return sprite;
        }

        /// <summary>
        /// Assigns a dedicated sprite to the renderer when available. Returns true when the
        /// dedicated art was applied (callers then skip their legacy block tint).
        /// </summary>
        public static bool TryApply(SpriteRenderer renderer, string key, Color tint)
        {
            if (renderer == null) return false;
            var sprite = Load(key);
            if (sprite == null) return false;
            renderer.sprite = sprite;
            renderer.color = tint;
            return true;
        }
    }

    /// <summary>
    /// Cached access to optional Higgsfield-authored presentation art. Generated assets
    /// are additive: callers retain their existing gameplay-safe fallback when a sprite
    /// is absent or fails to import.
    /// </summary>
    public static class HiggsfieldSpriteLibrary
    {
        public const string Knight = "Knight";
        public const string Archer = "Archer";
        public const string Cannon = "Cannon";
        public const string Barrel = "Barrel";
        public const string Ram = "Ram";
        public const string Trap = "Trap";

        public const string Impact = "Impact";
        public const string Wind = "Wind";
        public const string CoreCrack = "CoreCrack";
        public const string CollapseDust = "CollapseDust";

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite LoadUi(string key)
        {
            return Load("UI", key);
        }

        public static Sprite LoadVfx(string key)
        {
            return Load("VFX", key);
        }

        private static Sprite Load(string folder, string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            string path = $"Higgsfield/{folder}/{key}";
            if (cache.TryGetValue(path, out var cached) && cached != null) return cached;

            var sprite = Resources.Load<Sprite>(path);
            cache[path] = sprite;
            return sprite;
        }
    }
}
