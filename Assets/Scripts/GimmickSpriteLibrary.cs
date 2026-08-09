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
        public const string Ram = "gimmick_ram";
        public const string Barrel = "gimmick_barrel";
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

        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (cache.TryGetValue(key, out var cached) && cached != null) return cached;

            var sprite = Resources.Load<Sprite>($"Gimmicks/{key}");
#if UNITY_EDITOR
            if (sprite == null)
            {
                string path = $"Assets/Resources/Gimmicks/{key}.png";
                sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (sprite == null)
                {
                    var importer = UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.TextureImporter;
                    if (importer != null)
                    {
                        if (importer.textureType != UnityEditor.TextureImporterType.Sprite)
                        {
                            importer.textureType = UnityEditor.TextureImporterType.Sprite;
                            importer.SaveAndReimport();
                            sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
                        }
                    }
                    if (sprite == null)
                    {
                        var tex = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                        if (tex != null)
                        {
                            sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                        }
                    }
                }
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
}
