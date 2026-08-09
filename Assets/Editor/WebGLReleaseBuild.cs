using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class WebGLReleaseBuild
{
    const string OutputPath = "Builds/WebGL/castle-war";

    [MenuItem("Build/WebGL Release (castle-war)")]
    public static void Build()
    {
        // Hangul renders as tofu in WebGL without the bundled fallback asset.
        KoreanFontAssetBuilder.Ensure();

        // Dark siege-themed loading screen + canvas focus/touch handling (Assets/WebGLTemplates/CastleWar).
        PlayerSettings.WebGL.template = "PROJECT:CastleWar";
        // Diagnostic builds enable full stack traces; never let that persisted editor setting
        // leak into an optimized release, where Unity 2022 WebGL can recurse in stack unwinding.
        PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;

        var scenes = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();
        if (scenes.Length == 0)
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" };
        }

        // GitHub Pages sends no Content-Encoding headers, so a plain Brotli/Gzip
        // build 404s at load time; decompressionFallback keeps it self-decoding.
        PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Gzip;
        PlayerSettings.WebGL.decompressionFallback = true;

        var report = BuildPipeline.BuildPlayer(scenes, OutputPath, BuildTarget.WebGL, BuildOptions.None);
        BuildSummary summary = report.summary;
        Debug.Log($"WebGL build: result={summary.result} sizeBytes={summary.totalSize} errors={summary.totalErrors} output={OutputPath}");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
