using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
// Headless verifier: confirms the regenerated/keyed sprites are actually imported
// with alpha by the live Unity Editor, and renders the SampleScene to a PNG for
// visual proof. Triggered from outside the editor by dropping a sentinel file at
// tools/.gen_work/validate_request.txt, then focusing Unity (auto-refresh -> domain
// reload). Writes tools/.gen_work/validate_report.json + scene_render.png and then
// deletes the sentinel so it runs exactly once per request.
[InitializeOnLoad]
public static class SpriteApplyValidator
{
    private static string Root => Directory.GetParent(Application.dataPath).FullName;
    private static string WorkDir => Path.Combine(Root, "tools", ".gen_work");
    private static string RequestPath => Path.Combine(WorkDir, "validate_request.txt");
    private static string ReportPath => Path.Combine(WorkDir, "validate_report.json");
    private static string RenderPath => Path.Combine(WorkDir, "scene_render.png");

    static SpriteApplyValidator()
    {
        EditorApplication.delayCall += MaybeRun;
    }

    [MenuItem("CastleBusters/Validate Sprite Application")]
    public static void RunMenu() => Run();

    private static void MaybeRun()
    {
        if (File.Exists(RequestPath))
            Run();
    }

    private static void Run()
    {
        var sb = new StringBuilder();
        sb.Append("{\n");
        sb.Append("  \"timestamp\": \"").Append(DateTime.UtcNow.ToString("o")).Append("\",\n");
        sb.Append("  \"unityVersion\": \"").Append(Application.unityVersion).Append("\",\n");
        sb.Append("  \"sprites\": [\n");

        var dirs = new[]
        {
            "Assets/Sprites",
            "Assets/Resources/GeneratedUnitFrames",
            "Assets/Resources/Gimmicks",
            "Assets/Resources/Backgrounds",
        };

        var entries = new List<string>();
        int withAlpha = 0, total = 0, problems = 0;

        foreach (var dir in dirs)
        {
            var guids = AssetDatabase.FindAssets("t:Texture2D", new[] { dir });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) continue;
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                total++;

                bool sourceAlpha = importer.DoesSourceTextureHaveAlpha();
                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                bool isSprite = importer.textureType == TextureImporterType.Sprite;
                var name = Path.GetFileNameWithoutExtension(path);
                bool isOpaqueTextureBackdrop = path.StartsWith("Assets/Resources/Backgrounds/", StringComparison.Ordinal) ||
                    name.StartsWith("Background_Stage", StringComparison.OrdinalIgnoreCase);
                bool isOpaqueBackdrop = isOpaqueTextureBackdrop ||
                    name.Equals("Background", StringComparison.OrdinalIgnoreCase) ||
                    name.Equals("IntroKeyArt", StringComparison.OrdinalIgnoreCase);
                bool expectsSprite = !isOpaqueTextureBackdrop;
                bool hasTransparentPixels = sourceAlpha && HasTransparentPixels(path);

                if (sourceAlpha) withAlpha++;
                bool typeIsCorrect = isSprite == expectsSprite;
                bool alphaContractIsCorrect = isOpaqueBackdrop
                    ? !sourceAlpha
                    : sourceAlpha && importer.alphaIsTransparency && hasTransparentPixels;
                bool ok = typeIsCorrect && alphaContractIsCorrect;
                if (!ok) problems++;

                entries.Add(string.Format(CultureInfo.InvariantCulture,
                    "    {{ \"path\": \"{0}\", \"w\": {1}, \"h\": {2}, \"sourceHasAlpha\": {3}, \"alphaIsTransparency\": {4}, \"hasTransparentPixels\": {5}, \"alphaSource\": \"{6}\", \"sRGB\": {7}, \"isSprite\": {8}, \"expectsSprite\": {9}, \"ok\": {10} }}",
                    path,
                    tex != null ? tex.width : 0,
                    tex != null ? tex.height : 0,
                    sourceAlpha ? "true" : "false",
                    importer.alphaIsTransparency ? "true" : "false",
                    hasTransparentPixels ? "true" : "false",
                    importer.alphaSource,
                    importer.sRGBTexture ? "true" : "false",
                    isSprite ? "true" : "false",
                    expectsSprite ? "true" : "false",
                    ok ? "true" : "false"));
            }
        }

        sb.Append(string.Join(",\n", entries)).Append("\n");
        sb.Append("  ],\n");

        string renderResult = RenderScene();
        string frameSets = VerifyFrameSets(out int frameSetsOk, out int frameSetsTotal);
        sb.Append("  \"frameSets\": [\n").Append(frameSets).Append("\n  ],\n");

        sb.Append("  \"summary\": { \"total\": ").Append(total)
          .Append(", \"withAlpha\": ").Append(withAlpha)
          .Append(", \"problems\": ").Append(problems)
          .Append(", \"frameSetsOk\": ").Append(frameSetsOk)
          .Append(", \"frameSetsTotal\": ").Append(frameSetsTotal)
          .Append(", \"sceneRender\": \"").Append(renderResult).Append("\" }\n");
        sb.Append("}\n");

        Directory.CreateDirectory(WorkDir);
        File.WriteAllText(ReportPath, sb.ToString());
        if (File.Exists(RequestPath)) File.Delete(RequestPath);
        Debug.Log($"[SpriteApplyValidator] total={total} withAlpha={withAlpha} problems={problems} render={renderResult}");
    }

    // Loads each unit/state frame set through the same Resources path that
    // UnitSpriteAnimator.LoadFrames uses at runtime, proving the animations are
    // discoverable and carry transparency.
    private static string VerifyFrameSets(out int ok, out int total)
    {
        ok = 0; total = 0;
        var units = new[] { "Knight", "Archer", "Bomber" };
        var states = new[] { "Idle", "Walk", "Attack", "Launch" };
        var rows = new List<string>();
        foreach (var u in units)
        foreach (var s in states)
        {
            total++;
            var frames = Resources.LoadAll<Sprite>($"GeneratedUnitFrames/{u}/{s}");
            int count = frames != null ? frames.Length : 0;
            bool good = count >= 2;
            if (good) ok++;
            rows.Add(string.Format(CultureInfo.InvariantCulture,
                "    {{ \"unit\": \"{0}\", \"state\": \"{1}\", \"frames\": {2}, \"ok\": {3} }}",
                u, s, count, good ? "true" : "false"));
        }
        return string.Join(",\n", rows);
    }


    private static string RenderScene()
    {
        var sceneSetup = EditorSceneManager.GetSceneManagerSetup();
        bool restoreSceneSetup = false;
        foreach (var setup in sceneSetup)
        {
            if (!setup.isLoaded) continue;

            restoreSceneSetup = true;
            break;
        }
        Camera cam = null;
        RenderTexture rt = null;
        Texture2D tex = null;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = null;

        try
        {
            const string scenePath = "Assets/Scenes/SampleScene.unity";
            if (SceneManager.GetActiveScene().path != scenePath)
                EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            cam = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
            if (cam == null) return "no-camera";

            const int W = 1920, H = 1080;
            rt = new RenderTexture(W, H, 24, RenderTextureFormat.ARGB32);
            previousTarget = cam.targetTexture;
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.ReadPixels(new Rect(0, 0, W, H), 0, 0);
            tex.Apply();

            long nonBlack = 0;
            var px = tex.GetPixels32();
            for (int i = 0; i < px.Length; i += 97)
            {
                var c = px[i];
                if (c.r > 8 || c.g > 8 || c.b > 8) nonBlack++;
            }

            File.WriteAllBytes(RenderPath, tex.EncodeToPNG());
            return $"ok:{nonBlack}";
        }
        catch (Exception e)
        {
            return "error:" + e.Message.Replace("\"", "'");
        }
        finally
        {
            if (cam != null) cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            if (tex != null) UnityEngine.Object.DestroyImmediate(tex);
            if (rt != null) UnityEngine.Object.DestroyImmediate(rt);
            if (restoreSceneSetup) EditorSceneManager.RestoreSceneManagerSetup(sceneSetup);
        }
    }

    private static bool HasTransparentPixels(string path)
    {
        Texture2D decoded = null;
        try
        {
            decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(path), false)) return false;

            var pixels = decoded.GetPixelData<Color32>(0);
            for (int sampleY = 0; sampleY < 5; sampleY++)
            {
                int y = sampleY == 4 ? decoded.height - 1 : sampleY * (decoded.height - 1) / 4;
                for (int sampleX = 0; sampleX < 5; sampleX++)
                {
                    int x = sampleX == 4 ? decoded.width - 1 : sampleX * (decoded.width - 1) / 4;
                    if (pixels[y * decoded.width + x].a < byte.MaxValue) return true;
                }
            }

            return false;
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            if (decoded != null) UnityEngine.Object.DestroyImmediate(decoded);
        }
    }
}
