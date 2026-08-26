using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// WebGL load time is dominated by the download+JS-gunzip of castle-war.data.unityweb.
/// The data file was 72MB because ~282 Resources textures shipped as uncrunched DXT
/// (a 2048^2 DXT5 is ~4MB on disk). Crunch compression transcodes that DXT payload to a
/// far smaller on-disk form that decompresses back to DXT at load, so the DOWNLOAD shrinks
/// while GPU format is unchanged. This pass turns crunch on for every shipped texture under
/// Resources/ and Sprites/ (except the TMP font atlas under Fonts/), then builds WebGL.
/// </summary>
public static class TextureCrunchBuild
{
    // Folders whose textures ship in the player. Fonts/ is excluded: its TMP atlas is an
    // SDF asset whose crispness must not be block-compressed.
    static readonly string[] TargetFolders = { "Assets/Resources", "Assets/Sprites" };
    const int CrunchQuality = 50; // Unity default; balances on-disk size against DXT artifacting.

    [MenuItem("Build/Apply Texture Crunch")]
    public static int ApplyCrunch()
    {
        var guids = AssetDatabase.FindAssets("t:Texture2D", TargetFolders);
        int changed = 0, skipped = 0;
        var touched = new List<string>();

        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.Contains("/Fonts/")) { skipped++; continue; }

            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { skipped++; continue; }

            bool needs = !importer.crunchedCompression
                         || importer.textureCompression == TextureImporterCompression.Uncompressed;
            if (!needs) { skipped++; continue; }

            if (importer.textureCompression == TextureImporterCompression.Uncompressed)
                importer.textureCompression = TextureImporterCompression.Compressed;
            importer.crunchedCompression = true;
            importer.compressionQuality = CrunchQuality;

            importer.SaveAndReimport();
            changed++;
            touched.Add(path);
        }

        Debug.Log($"[TextureCrunch] crunched={changed} skipped={skipped} scanned={guids.Length}");
        foreach (var p in touched) Debug.Log($"[TextureCrunch] +crunch {p}");
        return changed;
    }

    /// <summary>Batch entry: crunch textures, then run the standard WebGL release build.</summary>
    public static void BuildWebGLCrunched()
    {
        ApplyCrunch();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        // WebGLReleaseBuild.Build handles template, gzip+fallback, and EditorApplication.Exit.
        WebGLReleaseBuild.Build();
    }
}
