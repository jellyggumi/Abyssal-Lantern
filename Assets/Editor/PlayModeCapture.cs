using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CastleBusters;

// Non-destructive play-mode capture: enters play mode in the live editor, drives a
// bomber launch, captures screenshots that prove the generated sprites + VFX render
// and animate, then returns to EDIT mode (never calls EditorApplication.Exit, so the
// interactive session survives). State is carried across the play-mode domain reload
// via SessionState. Trigger: menu CastleBusters/PlayMode Capture.
[InitializeOnLoad]
public static class PlayModeCapture
{
    private const string FlagKey = "CastleBusters.PlayModeCapture.Active";
    private static string Root => Directory.GetParent(Application.dataPath).FullName;
    private static string WorkDir => Path.Combine(Root, "tools", ".gen_work");

    private static int frame;
    private static int shots;
    private static bool launched;

    static PlayModeCapture()
    {
        if (SessionState.GetBool(FlagKey, false) && EditorApplication.isPlayingOrWillChangePlaymode)
        {
            frame = 0; shots = 0; launched = false;
            EditorApplication.update += OnUpdate;
        }
    }

    [MenuItem("CastleBusters/PlayMode Capture")]
    public static void Begin()
    {
        Directory.CreateDirectory(WorkDir);
        foreach (var f in Directory.GetFiles(WorkDir, "play_*.png")) File.Delete(f);
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity", OpenSceneMode.Single);
        SessionState.SetBool(FlagKey, true);
        EditorApplication.isPlaying = true;
    }

    private static void OnUpdate()
    {
        if (!EditorApplication.isPlaying) return;
        frame++;

        if (!launched && frame > 60)
        {
            var gm = Object.FindObjectOfType<GameManager>();
            var lm = Object.FindObjectOfType<LaunchManager>();
            if (gm != null && lm != null)
            {
                gm.SelectUnit(3); // powder keg: clearest explosion/collapse (slot 2 is the deploy-only Cannon)
                lm.SimulateLaunch(new Vector2(12f, 10f));
                launched = true;
            }
        }

        if (frame % 40 == 0 && shots < 5)
        {
            var path = Path.Combine(WorkDir, $"play_{shots}.png");
            ScreenCapture.CaptureScreenshot(path);
            shots++;
        }

        if (shots >= 5 || frame > 360)
        {
            EditorApplication.update -= OnUpdate;
            SessionState.SetBool(FlagKey, false);
            File.WriteAllText(Path.Combine(WorkDir, "play_report.json"),
                $"{{ \"shots\": {shots}, \"frames\": {frame}, \"launched\": {(launched ? "true" : "false")} }}\n");
            EditorApplication.isPlaying = false; // back to edit mode, session intact
            Debug.Log($"[PlayModeCapture] done shots={shots} frames={frame} launched={launched}");
        }
    }
}
