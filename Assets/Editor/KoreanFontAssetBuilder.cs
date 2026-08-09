using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Creates a serialized dynamic TMP font asset from the bundled Noto Sans KR
/// subset and registers it in the global TMP fallback chain. WebGL builds have
/// no OS fonts (Font.GetPathsToOSFonts returns nothing in the browser), so the
/// Korean fallback must ship inside the build instead of being discovered at
/// runtime the way KoreanFontSupport does in the editor.
/// </summary>
public static class KoreanFontAssetBuilder
{
    const string SourceFontPath = "Assets/Fonts/NotoSansKR-Regular.otf";
    const string OutputDir = "Assets/Resources/Fonts";
    const string OutputPath = OutputDir + "/NotoSansKR-Dynamic.asset";

    [MenuItem("Build/Ensure Korean Font Asset")]
    public static void Ensure()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutputPath);
        if (asset == null)
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
            {
                throw new FileNotFoundException($"Source font missing: {SourceFontPath}");
            }

            asset = TMP_FontAsset.CreateFontAsset(
                font, 32, 4, GlyphRenderMode.SDFAA, 512, 512,
                AtlasPopulationMode.Dynamic);
            if (asset == null)
            {
                throw new System.InvalidOperationException("TMP_FontAsset.CreateFontAsset failed for " + SourceFontPath);
            }

            asset.isMultiAtlasTexturesEnabled = true;
            Directory.CreateDirectory(OutputDir);
            AssetDatabase.CreateAsset(asset, OutputPath);
            if (asset.material != null) AssetDatabase.AddObjectToAsset(asset.material, asset);
            if (asset.atlasTexture != null) AssetDatabase.AddObjectToAsset(asset.atlasTexture, asset);
            AssetDatabase.SaveAssets();
            Debug.Log("[KoreanFontAssetBuilder] Created " + OutputPath);
        }

        var fallbacks = TMP_Settings.fallbackFontAssets;
        if (fallbacks != null && !fallbacks.Contains(asset))
        {
            fallbacks.Add(asset);
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
            Debug.Log("[KoreanFontAssetBuilder] Registered in TMP Settings fallback chain");
        }
    }
}
