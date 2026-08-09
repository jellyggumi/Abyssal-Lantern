using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Bakes a STATIC TMP font asset from the bundled Noto Sans KR subset with
/// every Hangul syllable found in the project's sources, and registers it in
/// the global TMP fallback chain.
///
/// Static, not dynamic, on purpose: WebGL runs single-threaded on a WASM stack
/// far smaller than native, and on-demand SDF rasterization of CJK glyphs
/// there produced "RangeError: Maximum call stack size exceeded" at load
/// (see _workspace/current/qa/defect-register.md D-001/D-002). Baking at build
/// time removes runtime rasterization entirely; the builder re-runs on every
/// build, so newly added Korean strings are re-extracted automatically.
/// </summary>
public static class KoreanFontAssetBuilder
{
    const string SourceFontPath = "Assets/Fonts/NotoSansKR-Regular.otf";
    const string OutputDir = "Assets/Resources/Fonts";
    const string OutputPath = OutputDir + "/NotoSansKR-Dynamic.asset"; // name kept: runtime loads Fonts/NotoSansKR-Dynamic
    // Non-ASCII characters used in UI outside the Hangul block.
    const string ExtraChars = "한…·→←↑↓×★☆%";

    [MenuItem("Build/Ensure Korean Font Asset")]
    public static void Ensure()
    {
        string chars = CollectProjectHangul();
        Debug.Log($"[KoreanFontAssetBuilder] Baking {chars.Length} characters into static atlas");

        // Always rebuild: the character set tracks the source tree.
        var stale = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
        if (stale != null)
        {
            TMP_Settings.fallbackFontAssets?.RemoveAll(f => f == null || f == stale);
            AssetDatabase.DeleteAsset(OutputPath);
        }

        var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (font == null)
        {
            throw new FileNotFoundException($"Source font missing: {SourceFontPath}");
        }

        var asset = TMP_FontAsset.CreateFontAsset(
            font, 32, 4, GlyphRenderMode.SDFAA, 1024, 1024,
            AtlasPopulationMode.Dynamic); // dynamic only while baking below
        if (asset == null)
        {
            throw new System.InvalidOperationException("TMP_FontAsset.CreateFontAsset failed for " + SourceFontPath);
        }

        asset.isMultiAtlasTexturesEnabled = true;
        if (!asset.TryAddCharacters(chars, out string missing) && !string.IsNullOrEmpty(missing))
        {
            Debug.LogWarning($"[KoreanFontAssetBuilder] {missing.Length} characters missing from source font: {missing}");
        }
        asset.atlasPopulationMode = AtlasPopulationMode.Static;

        Directory.CreateDirectory(OutputDir);
        AssetDatabase.CreateAsset(asset, OutputPath);
        if (asset.material != null) AssetDatabase.AddObjectToAsset(asset.material, asset);
        foreach (var tex in asset.atlasTextures)
        {
            if (tex != null) AssetDatabase.AddObjectToAsset(tex, asset);
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"[KoreanFontAssetBuilder] Created {OutputPath} ({asset.atlasTextures.Length} atlas texture(s))");

        var fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks != null && !fallbacks.Contains(asset))
        {
            fallbacks.Add(asset);
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
            Debug.Log("[KoreanFontAssetBuilder] Registered in TMP Settings fallback chain");
        }
    }

    static string CollectProjectHangul()
    {
        var found = new HashSet<char>();
        foreach (char c in ExtraChars) found.Add(c);

        var hangul = new Regex("[가-힣㄰-㆏]");
        var patterns = new[] { "*.cs", "*.unity", "*.prefab", "*.asset", "*.json" };
        foreach (var pattern in patterns)
        {
            foreach (var file in Directory.GetFiles("Assets", pattern, SearchOption.AllDirectories))
            {
                string text;
                try { text = File.ReadAllText(file, Encoding.UTF8); }
                catch { continue; }
                foreach (Match m in hangul.Matches(text)) found.Add(m.Value[0]);
            }
        }

        var sb = new StringBuilder(found.Count);
        foreach (char c in found) sb.Append(c);
        return sb.ToString();
    }
}
