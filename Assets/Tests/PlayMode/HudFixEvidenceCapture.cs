using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
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
    /// Photographs the HUD at the window sizes the fix is about, so the change can be judged by
    /// looking rather than by reading a scale factor.
    ///
    /// The bug reproduced only below a certain window size, so a single screenshot could not
    /// show it. Each capture here renders the live HUD into a render target of a given size
    /// with the canvas scaled exactly as that window would scale it — including the clamp that
    /// stops the shrink at the supported floor.
    /// </summary>
    public class HudFixEvidenceCapture
    {
        private static readonly (int w, int h, string label)[] Windows =
        {
            (1920, 1080, "1920x1080"),
            (1280, 720,  "1280x720"),
            (1024, 576,  "1024x576"),
            (640,  480,  "640x480"),
        };

        private static string EvidenceDir =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "_workspace/current/qa/evidence/hud-fix");

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator Hud_CapturedAcrossWindowSizes()
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

            var hud = GameObject.Find(HudCanvas.CanvasName);
            Assert.IsNotNull(hud, "The HUD canvas must exist");
            var scaler = hud.GetComponent<CanvasScaler>();
            Assert.IsNotNull(scaler, "The HUD canvas must have a scaler");

            var report = new StringBuilder();
            report.AppendLine("# HUD 수정 증거 — 창 크기별");
            report.AppendLine();
            report.AppendLine("| 창 | HUD scale | 최소 라벨 실효 px | 하한 통과 |");
            report.AppendLine("|---|---|---|---|");

            foreach (var (w, h, label) in Windows)
            {
                // Drive the scaler exactly as that window would, clamp included, rather than
                // resizing the player — batchmode cannot resize, and the clamp is the thing
                // under test.
                scaler.scaleFactor = HudScaleFloor.ScaleFor(h);
                Canvas.ForceUpdateCanvases();
                yield return null;

                Shoot(w, h, Path.Combine(EvidenceDir, $"hud-{label}.png"));

                var smallest = float.MaxValue;
                foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
                {
                    if (!t.isActiveAndEnabled || t.canvas == null) continue;
                    if (t.canvas.name != HudCanvas.CanvasName) continue;
                    smallest = Mathf.Min(smallest, t.fontSize * scaler.scaleFactor);
                }

                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1:0.000} | {2:0.0} | {3} |",
                    label, scaler.scaleFactor, smallest,
                    smallest >= HudCanvas.LegibleFloorPixels ? "OK" : "**FAIL**"));

                Assert.GreaterOrEqual(smallest, HudCanvas.LegibleFloorPixels,
                    $"At {label} every HUD label must clear the legibility floor");
            }

            File.WriteAllText(Path.Combine(EvidenceDir, "hud-fix.md"), report.ToString());
        }

        private static void Shoot(int w, int h, string path)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (c.isActiveAndEnabled) { cam = c; break; }
                }
            }
            if (cam == null) return;

            // Overlay canvases bypass camera rendering, so each is briefly switched to camera
            // space for the shot and restored afterwards.
            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var saved = new (Canvas c, RenderMode mode, Camera cam, float dist)[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                saved[i] = (c, c.renderMode, c.worldCamera, c.planeDistance);
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane + 0.01f;
            }
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(w, h, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;

            foreach (var (c, mode, worldCam, dist) in saved)
            {
                if (c == null) continue;
                c.renderMode = mode;
                c.worldCamera = worldCam;
                c.planeDistance = dist;
            }
            Canvas.ForceUpdateCanvases();

            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
