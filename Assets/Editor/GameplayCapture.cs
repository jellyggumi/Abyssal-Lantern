using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using CastleBusters;

public static class GameplayCapture
{
    public static void Capture()
    {
        frameCount = 0;
        captureCount = 0;
        hasSimulatedPlayerLaunch = false;
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        EditorApplication.isPlaying = true;
        EditorApplication.update += OnUpdate;
    }

    private static int frameCount = 0;
    private static int captureCount = 0;
    private static bool hasSimulatedPlayerLaunch = false;

    private static void OnUpdate()
    {
        if (!EditorApplication.isPlaying) return;

        frameCount++;

        if (!hasSimulatedPlayerLaunch && frameCount > 90)
        {
            var gameManager = Object.FindObjectOfType<GameManager>();
            var launchManager = Object.FindObjectOfType<LaunchManager>();
            if (gameManager != null && launchManager != null)
            {
                gameManager.SelectUnit(2); // Bomber gives the clearest explosion/collapse capture.
                launchManager.SimulateLaunch(new Vector2(12f, 10f));
                hasSimulatedPlayerLaunch = true;
                Debug.Log("GameplayCapture simulated a bomber launch.");
            }
        }

        if (frameCount % 45 == 0 && captureCount < 12)
        {
            ScreenCapture.CaptureScreenshot($"GameplayCapture_{captureCount}.png");
            captureCount++;
            Debug.Log($"Captured gameplay frame {captureCount}");
        }

        if (captureCount >= 12 || frameCount > 900)
        {
            EditorApplication.isPlaying = false;
            EditorApplication.update -= OnUpdate;
            Debug.Log("Gameplay capture complete.");
            EditorApplication.Exit(0);
        }
    }
}
