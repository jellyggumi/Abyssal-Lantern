using System.Collections;
using System.Collections.Generic;
using CastleBusters;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the two properties that made HUD text unreadable after the Unity 6000 upgrade.
    ///
    /// Six call sites parented their labels to whatever <c>FindObjectOfType&lt;Canvas&gt;()</c>
    /// returned first. Engine iteration order is not part of any contract, and when it changed
    /// the badges landed on the cold open's canvas — whose scaler <see cref="GameFeelVfx"/>
    /// then rewrote — so a 17pt label rendered at 6.5px and "KEEP CORE" read as "KLLP CORL".
    ///
    /// Both halves are asserted because either one alone still breaks: one canvas with
    /// under-floor sizes is unreadable, and correct sizes split across two canvases with
    /// different scalers are inconsistent.
    /// </summary>
    public class HudCanvasContractTests
    {
        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private static IEnumerator BootMatch()
        {
            LogAssert.ignoreFailingMessages = true;
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            LogAssert.ignoreFailingMessages = true;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "The arena must have a GameManager");
            gm.BeginSiege();
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
        }

        /// <summary>
        /// Every gameplay HUD label shares one canvas, so one scaler governs all of them.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator GameplayHudLabels_AllShareTheOneHudCanvas()
        {
            yield return BootMatch();

            var hud = GameObject.Find(HudCanvas.CanvasName);
            Assert.IsNotNull(hud, $"The HUD canvas '{HudCanvas.CanvasName}' must exist once a match is running");

            var strays = new List<string>();
            foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled) continue;
                var canvas = t.canvas;
                if (canvas == null) continue;   // not drawn at all — a separate defect, filed as UX-001/002
                // The intro/webtoon/results screens own their own canvases by design; only the
                // gameplay HUD is under this contract.
                if (canvas.GetComponentInParent<IntroScreenController>() != null) continue;
                if (canvas.GetComponentInParent<WebtoonPrologueController>() != null) continue;
                if (canvas.name != HudCanvas.CanvasName) strays.Add($"{t.name} → {canvas.name}");
            }

            Assert.IsEmpty(strays,
                "Gameplay HUD labels must all live on the one HUD canvas; a split means two "
                + "scalers and two text sizes. Strays: " + string.Join(", ", strays));
        }

        /// <summary>
        /// No HUD label is authored below the legibility floor once the worst supported window
        /// has scaled it. This is the assertion that would have caught the original bug: the
        /// shipped sizes 17/15/14 become 9.1/8.0/7.5px at 1024x576.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator HudLabelSizes_ClearTheLegibilityFloorAtTheSmallestWindow()
        {
            yield return BootMatch();

            var hud = GameObject.Find(HudCanvas.CanvasName);
            Assert.IsNotNull(hud, "The HUD canvas must exist");

            var scale = HudCanvas.WorstCaseScale;
            var tooSmall = new List<string>();
            var checkedCount = 0;

            foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || t.canvas == null) continue;
                if (t.canvas.name != HudCanvas.CanvasName) continue;
                checkedCount++;
                var effective = t.fontSize * scale;
                if (effective < HudCanvas.LegibleFloorPixels)
                {
                    tooSmall.Add($"{t.name} {t.fontSize}pt → {effective:0.0}px");
                }
            }

            Assert.Greater(checkedCount, 0, "The HUD must have labels to check");
            Assert.IsEmpty(tooSmall,
                $"At the smallest supported window ({HudCanvas.MinSupportedHeight}p, scale {scale:0.000}) "
                + $"every HUD label must render at least {HudCanvas.LegibleFloorPixels}px, or its thin "
                + "horizontal strokes drop out. Under floor: " + string.Join(", ", tooSmall));
        }

        /// <summary>
        /// The HUD does not reconfigure a canvas it does not own. The cold open's canvas keeps
        /// whatever scaling its own author chose.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator HudSetup_DoesNotRewriteAnotherSystemsCanvas()
        {
            LogAssert.ignoreFailingMessages = true;
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.0f);

            // A foreign canvas with deliberately distinctive settings, standing in for the
            // cold-open canvas that used to be adopted and rewritten.
            var foreignGo = new GameObject("ForeignCanvas", typeof(Canvas), typeof(CanvasScaler));
            var foreignScaler = foreignGo.GetComponent<CanvasScaler>();
            foreignScaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            foreignScaler.scaleFactor = 2f;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "The arena must have a GameManager");
            gm.BeginSiege();
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.AreEqual(CanvasScaler.ScaleMode.ConstantPixelSize, foreignScaler.uiScaleMode,
                "Building the HUD must not change another canvas's scale mode");
            Assert.AreEqual(2f, foreignScaler.scaleFactor, 0.0001f,
                "Building the HUD must not change another canvas's scale factor");

            Object.DestroyImmediate(foreignGo);
        }
    }
}

namespace CastleBusters.Tests
{
    /// <summary>
    /// The HUD scale stops shrinking at the smallest supported window.
    ///
    /// Pure arithmetic, so it runs without a screen and cannot be made to pass by resizing a
    /// test window. The property being pinned is that a browser window smaller than the
    /// supported floor gets a larger-than-proportional HUD instead of unreadable text.
    /// </summary>
    public class HudScaleFloorTests
    {
        [Test]
        public void AboveTheFloor_ScaleIsProportionalToScreenHeight()
        {
            Assert.AreEqual(1f, HudScaleFloor.ScaleFor(1080), 0.0001f, "the reference height scales 1:1");
            Assert.AreEqual(2f, HudScaleFloor.ScaleFor(2160), 0.0001f, "4K doubles the HUD with the screen");
            Assert.AreEqual(0.6667f, HudScaleFloor.ScaleFor(720), 0.001f, "720p scales down proportionally");
        }

        [Test]
        public void BelowTheFloor_ScaleStopsShrinking()
        {
            var atFloor = HudScaleFloor.ScaleFor((int)HudCanvas.MinSupportedHeight);
            Assert.AreEqual(atFloor, HudScaleFloor.ScaleFor(480), 0.0001f,
                "a window under the floor must not shrink the HUD further");
            Assert.AreEqual(atFloor, HudScaleFloor.ScaleFor(200), 0.0001f,
                "and no window, however small, may shrink it");
        }

        [Test]
        public void EveryHudSize_ClearsTheLegibilityFloorAtAnyWindow()
        {
            // The clamp exists so this holds for every window, not just supported ones. If a
            // future edit lowers a size or raises the floor, this is what says so.
            foreach (var height in new[] { 2160, 1440, 1080, 900, 720, 576, 480, 320, 200 })
            {
                var scale = HudScaleFloor.ScaleFor(height);
                Assert.GreaterOrEqual(HudCanvas.SecondaryLabelSize * scale, HudCanvas.LegibleFloorPixels,
                    $"the smallest HUD size must stay legible at {height}p");
            }
        }

        [Test]
        public void DegenerateScreenHeight_DoesNotProduceAZeroScale()
        {
            // Screen.height reads 0 for a frame on some platforms during startup; a 0 scale
            // would collapse the HUD to nothing and look like a rendering bug.
            Assert.AreEqual(1f, HudScaleFloor.ScaleFor(0), 0.0001f);
            Assert.AreEqual(1f, HudScaleFloor.ScaleFor(-100), 0.0001f);
        }
    }
}
