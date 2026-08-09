using UnityEngine;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using CastleBusters;

namespace CastleBusters.Tests
{
    public class AutoPlayTest
    {
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator PlaySequenceAndCapture()
        {

            // Load the scene

            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            yield return null;

            // Wait for initialization; the game now boots into the Intro state (frozen diorama
            // behind the title card), so capture that first.
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.IsNotNull(GameManager.Instance, "GameManager must exist after scene load");
            Assert.AreEqual(GameState.Intro, GameManager.Instance.currentState, "Game must boot into the Intro state");
            ScreenCapture.CaptureScreenshot("IntroCapture.png");
            Debug.Log("Captured intro screen");
            yield return new WaitForSecondsRealtime(1.0f);

            // Dismiss the intro exactly like the START button does.
            GameManager.Instance.BeginSiege();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(GameState.PlayerTurn, GameManager.Instance.currentState, "BeginSiege must hand control to the player");
            Assert.AreEqual(1f, Time.timeScale, 0.01f, "Gameplay must run at full timescale");

            // Simulate a launch
            var launchManager = Object.FindObjectOfType<LaunchManager>();
            if (launchManager != null && GameManager.Instance != null)
            {
                GameManager.Instance.SelectUnit(0); // Select Knight
                launchManager.SimulateLaunch(new Vector2(15f, 10f));
            }

            // Capture frames during flight and impact
            for (int i = 1; i <= 5; i++)
            {
                yield return new WaitForSecondsRealtime(0.5f);
                ScreenCapture.CaptureScreenshot($"GameplayCapture_{i}.png");
                Debug.Log($"Captured frame {i}");
            }

            // Wait for the turn to end and capture final state
            yield return new WaitForSecondsRealtime(2f);
            ScreenCapture.CaptureScreenshot("GameplayCapture_6.png");
            Debug.Log("Captured frame 6");
        }
    }
}
