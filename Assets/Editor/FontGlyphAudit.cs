using System.Collections.Generic;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Audits every non-ASCII character the project's sources can put on screen against the
/// baked Korean atlas (Resources/Fonts/NotoSansKR-Dynamic). The builder already warns about
/// characters the SOURCE FONT cannot supply, but that warning scrolls away inside a build
/// log — this probe makes "which glyphs will render as tofu" a direct, runnable question.
/// Exits 0 when every used character is covered, 1 otherwise (batch mode), so it can gate.
/// </summary>
public static class FontGlyphAudit
{
    [MenuItem("Build/Audit Font Glyph Coverage")]
    public static void Audit()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/Resources/Fonts/NotoSansKR-Dynamic.asset");
        if (asset == null)
        {
            Debug.LogError("[FontGlyphAudit] Baked font asset missing — run Build/Ensure Korean Font Asset first.");
            if (Application.isBatchMode) EditorApplication.Exit(1);
            return;
        }

        // Same sweep as KoreanFontAssetBuilder.CollectProjectHangul: every non-ASCII,
        // non-control, non-surrogate char in the source tree. Tests/ and Editor/ are
        // excluded — the builder bakes them too (cheap), but only characters a PLAYER can
        // see should be able to fail this gate; a U+FE0F inside a test's sample data is
        // not a defect the build must block on.
        var used = new Dictionary<char, string>(); // char -> first file seen in
        var patterns = new[] { "*.cs", "*.unity", "*.prefab", "*.asset", "*.json" };
        foreach (var pattern in patterns)
        {
            foreach (var rawFile in Directory.GetFiles("Assets", pattern, SearchOption.AllDirectories))
            {
                string file = rawFile.Replace('\\', '/');
                if (file.StartsWith("Assets/Resources/Fonts/")) continue; // the atlas itself
                if (file.StartsWith("Assets/Tests/") || file.StartsWith("Assets/Editor/")) continue;
                string text;
                try { text = File.ReadAllText(file, Encoding.UTF8); }
                catch { continue; }
                foreach (char c in text)
                {
                    if (c < 0x80 || char.IsControl(c) || char.IsSurrogate(c)) continue;
                    if (!used.ContainsKey(c)) used[c] = file;
                }
            }
        }

        var missing = new List<string>();
        foreach (var pair in used)
        {
            if (!asset.HasCharacter(pair.Key, searchFallbacks: false))
            {
                missing.Add($"U+{(int)pair.Key:X4} '{pair.Key}' (first: {pair.Value})");
            }
        }

        missing.Sort();
        if (missing.Count == 0)
        {
            Debug.Log($"[FontGlyphAudit] OK — all {used.Count} distinct non-ASCII characters are baked.");
        }
        else
        {
            Debug.LogError($"[FontGlyphAudit] {missing.Count} character(s) used in sources but NOT in the baked atlas:\n"
                + string.Join("\n", missing));
        }

        if (Application.isBatchMode) EditorApplication.Exit(missing.Count == 0 ? 0 : 1);
    }
}
