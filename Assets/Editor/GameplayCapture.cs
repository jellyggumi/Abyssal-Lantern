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
                // Slot 2 is now the deploy-only Cannon (it has no launch prefab), so the
                // capture uses the powder keg — still the clearest explosion/collapse shot.
                gameManager.SelectUnit(3);
                launchManager.SimulateLaunch(new Vector2(12f, 10f));
                hasSimulatedPlayerLaunch = true;
                Debug.Log("GameplayCapture simulated a powder-keg launch.");
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
