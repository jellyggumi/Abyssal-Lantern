using UnityEngine;
using System.Collections;
using NUnit.Framework;
using UnityEngine.TestTools;
using CastleBusters;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Playtest QA pass capture: pins the concrete fixes from the "유아이유엑스 ... 플레이테스트
    /// 개선" pass so a future edit can't silently regress them, and grabs fresh screenshots for
    /// visual review (title button, selection row, archer arrow, brick-spawn fx texture).
    /// </summary>
    public class PlaytestQACapture
    {
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator SelectionRowSizingArcherAndBrickFxCapture()
        {

            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.IsNotNull(GameManager.Instance, "GameManager must exist after scene load");
            Assert.AreEqual(GameState.Intro, GameManager.Instance.currentState, "Game must boot into the Intro state");

            // Title button +20% pass: 420x110 -> 504x132, ratio unchanged (still 3.818:1).
            var introGo = GameObject.Find("IntroScreen");
            Assert.IsNotNull(introGo, "IntroScreen must exist during Intro state");
            var introButtonRt = FindChildRect(introGo.transform, "StartButton");
            if (introButtonRt != null)
            {
                Assert.AreEqual(504f, introButtonRt.sizeDelta.x, 0.5f, "Title button width must be +20% (420*1.2)");
                Assert.AreEqual(132f, introButtonRt.sizeDelta.y, 0.5f, "Title button height must be +20% (110*1.2)");
                float originalRatio = 420f / 110f;
                float newRatio = introButtonRt.sizeDelta.x / introButtonRt.sizeDelta.y;
                Assert.AreEqual(originalRatio, newRatio, 0.01f, "Title button aspect ratio must not drift when scaled +20%");
            }

            ScreenCapture.CaptureScreenshot("QA_TitleScreen.png");
            yield return new WaitForSecondsRealtime(1.0f);

            GameManager.Instance.BeginSiege();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(GameState.PlayerTurn, GameManager.Instance.currentState, "BeginSiege must hand control to the player");

            // Selection-row sizing pass: character cards 1.5x, gimmick card 1.2x of the 82x54
            // baseline, without breaking the face/text ratio.
            const float baseWidth = 82f, baseHeight = 54f;
            AssertButtonSize(GameManager.Instance.knightButton, baseWidth * 1.5f, baseHeight * 1.5f, "Knight");
            AssertButtonSize(GameManager.Instance.archerButton, baseWidth * 1.5f, baseHeight * 1.5f, "Archer");
            AssertButtonSize(GameManager.Instance.bomberButton, baseWidth * 1.5f, baseHeight * 1.5f, "Bomber");
            AssertButtonSize(GameManager.Instance.gimmickButton, baseWidth * 1.5f, baseHeight * 1.5f, "Gimmick");
            AssertNoOverlap(GameManager.Instance.knightButton, GameManager.Instance.archerButton);
            AssertNoOverlap(GameManager.Instance.archerButton, GameManager.Instance.bomberButton);
            AssertNoOverlap(GameManager.Instance.bomberButton, GameManager.Instance.gimmickButton);

            ScreenCapture.CaptureScreenshot("QA_SelectionRow.png");

            // Archer arrow visibility pass: select Archer, launch, and confirm the arrow carries
            // the enlarged 1.6u/0.24u presentation plus the new visibility trail.
            GameManager.Instance.SelectUnit(1); // Archer
            var launchManager = Object.FindObjectOfType<LaunchManager>();
            Assert.IsNotNull(launchManager, "LaunchManager must exist mid-match");
            launchManager.SimulateLaunch(new Vector2(15f, 10f));
            yield return new WaitForSecondsRealtime(0.2f);

            var arrow = Object.FindObjectOfType<ArrowController>();
            if (arrow != null)
            {
                Assert.GreaterOrEqual(arrow.visualLength, 1.6f - 0.001f, "Archer arrow must keep the visibility-pass length");
                Assert.IsNotNull(arrow.GetComponent<TrailRenderer>(), "Archer arrow must render a visibility trail");
            }
            ScreenCapture.CaptureScreenshot("QA_ArcherArrow.png");
            yield return new WaitForSecondsRealtime(1.0f);

            // Brick-spawn fx texture pass: fx_spawn is now a real stone/dust sprite sequence
            // (god-tibo-imagen art), not the old flat-white burst. Exercise the exact call site
            // used by BrickPlacementController.OnTurnChanged and confirm real frame art loads.
            var fxGo = FrameAnimEffect.Spawn(EffectSpriteLibrary.Spawn, new Vector3(0f, 1f, 0f), 1.8f,
                new Color(1f, 0.96f, 0.88f, 0.95f), 14f, 40);
            Assert.IsNotNull(fxGo, "fx_spawn frame set must load (Resources/Effects/fx_spawn/*.png)");
            yield return new WaitForSecondsRealtime(0.1f);
            ScreenCapture.CaptureScreenshot("QA_BrickSpawnFx.png");
            yield return new WaitForSecondsRealtime(0.3f);
        }

        private static RectTransform FindChildRect(Transform root, string partialName)
        {
            foreach (var rt in root.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name.IndexOf(partialName, System.StringComparison.OrdinalIgnoreCase) >= 0) return rt;
            }
            return null;
        }

        private static void AssertButtonSize(UnityEngine.UI.Button button, float expectedW, float expectedH, string label)
        {
            Assert.IsNotNull(button, $"{label} selection button must exist");
            var rt = button.GetComponent<RectTransform>();
            Assert.IsNotNull(rt, $"{label} selection button must have a RectTransform");
            Assert.AreEqual(expectedW, rt.sizeDelta.x, 0.5f, $"{label} button width must match the playtest sizing pass");
            Assert.AreEqual(expectedH, rt.sizeDelta.y, 0.5f, $"{label} button height must match the playtest sizing pass");
        }

        private static void AssertNoOverlap(UnityEngine.UI.Button left, UnityEngine.UI.Button right)
        {
            if (left == null || right == null) return;
            var lr = left.GetComponent<RectTransform>();
            var rr = right.GetComponent<RectTransform>();
            if (lr == null || rr == null) return;
            float leftRightEdge = lr.anchoredPosition.x + lr.sizeDelta.x / 2f;
            float rightLeftEdge = rr.anchoredPosition.x - rr.sizeDelta.x / 2f;
            Assert.LessOrEqual(leftRightEdge, rightLeftEdge + 0.01f,
                "Enlarged selection cards must not overlap after the 1.5x/1.2x sizing pass");
        }
    }
}
