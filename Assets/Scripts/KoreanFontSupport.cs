using System.Linq;
using TMPro;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Runtime Hangul support for TextMeshPro. The bundled LiberationSans SDF has no Korean
    /// glyphs, so every bilingual HUD string ("전장 첩보", "여기서 장전", ...) rendered as tofu
    /// boxes. Instead of shipping a multi-megabyte baked Korean atlas, this builds a dynamic
    /// TMP_FontAsset from an OS-installed Korean font once per session and appends it to the
    /// global fallback chain - TMP then resolves missing glyphs on demand.
    ///
    /// Implementation note: TMP_FontAsset.CreateFontAsset(Font) cannot read the face of a
    /// Font.CreateDynamicFontFromOSFont(...) handle on macOS (.ttc collections report
    /// "Unable to load font face"). Loading the font FILE via Font.GetPathsToOSFonts() +
    /// new Font(path) gives FontEngine a real file-backed face, which works reliably.
    /// </summary>
    public static class KoreanFontSupport
    {

        // Path fragments ordered by platform likelihood: macOS first (dev machines), then
        // Windows, then Linux/Noto. Matched case-insensitively against OS font file paths.
        private static readonly string[] CandidatePathFragments =
        {
            "AppleSDGothicNeo",
            "AppleGothic",
            "malgun",
            "NanumBarunGothic",
            "NanumGothic",
            "NotoSansKR",
            "NotoSansCJK",
        };

        public static void EnsureFallback()
        {
            // No one-shot flag: dynamic font assets created at runtime die on domain reload,
            // leaving null holes in TMP_Settings' persisted fallback list. Re-checking live state
            // every call makes this self-healing across editor sessions, playmode entry, and tests.
            if (SupportsHangul(TMP_Settings.defaultFontAsset)) return;

            var fallbacks = TMP_Settings.fallbackFontAssets;
            if (fallbacks != null)
            {
                fallbacks.RemoveAll(f => f == null); // dead assets from previous domain reloads
                if (fallbacks.Any(SupportsHangul)) return; // live Hangul fallback already present
            }

            // Bundled font first: WebGL (and any player build) has no OS font paths, so the
            // serialized dynamic asset created by KoreanFontAssetBuilder is the only source
            // of Hangul glyphs outside the editor.
            var bundled = Resources.Load<TMP_FontAsset>("Fonts/NotoSansKR-Dynamic");
            if (bundled != null && SupportsHangul(bundled))
            {
                if (fallbacks != null && !fallbacks.Contains(bundled))
                {
                    fallbacks.Add(bundled);
                }
                return;
            }

            string[] osFontPaths;
            try { osFontPaths = Font.GetPathsToOSFonts(); }
            catch { osFontPaths = null; }
            if (osFontPaths == null || osFontPaths.Length == 0)
            {
                Debug.LogWarning("[KoreanFontSupport] OS font paths unavailable; Hangul UI text may render as boxes.");
                return;
            }

            foreach (var fragment in CandidatePathFragments)
            {
                string path = osFontPaths.FirstOrDefault(
                    p => p.IndexOf(fragment, System.StringComparison.OrdinalIgnoreCase) >= 0);
                if (path == null) continue;

                TMP_FontAsset fontAsset = null;
                try
                {
                    var fileFont = new Font(path);
                    fontAsset = TMP_FontAsset.CreateFontAsset(
                        fileFont, 32, 4, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024,
                        AtlasPopulationMode.Dynamic);
                }
                catch
                {
                    continue;
                }

                if (fontAsset == null || !SupportsHangul(fontAsset)) continue;

                fontAsset.name = $"KoreanFallback ({System.IO.Path.GetFileNameWithoutExtension(path)})";
                if (TMP_Settings.fallbackFontAssets != null && !TMP_Settings.fallbackFontAssets.Contains(fontAsset))
                {
                    TMP_Settings.fallbackFontAssets.Add(fontAsset);
                }
                return;
            }

            Debug.LogWarning("[KoreanFontSupport] No Korean-capable OS font found; Hangul UI text may render as boxes.");
        }

        public static bool SupportsHangul(TMP_FontAsset fontAsset)
        {
            // '한' U+D55C - representative syllable; dynamic atlases pull the glyph on demand.
            return fontAsset != null && fontAsset.HasCharacter('한', searchFallbacks: false, tryAddCharacter: true);
        }
    }
}
