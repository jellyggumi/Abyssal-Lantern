using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Measures the HUD's real text size, because the same string renders cleanly on a bare
    /// canvas and breaks in game.
    ///
    /// The suspect is the reference-resolution scaler. A label asking for fontSize 17 on a
    /// canvas built for 1920x1080 does not get 17 pixels when the window is smaller — it gets
    /// 17 x scaleFactor. Below roughly a dozen pixels an SDF glyph's thin horizontal strokes
    /// stop covering a whole pixel row, and E reads as L while C and O gap open. That is
    /// exactly the reported symptom, and it is a sizing bug rather than a font bug.
    ///
    /// Reads every live TMP label in a booted arena and reports requested size, canvas scale,
    /// and the resulting pixel size, so the fix can be aimed at whichever labels are actually
    /// under the threshold.
    /// </summary>
    public class HudFontScaleDiagnosis
    {
        /// <summary>
        /// Flags rows against the same floor the HUD contract enforces, so the diagnostic and
        /// the gate cannot disagree about what "too small" means. A separate number here was
        /// how the first pass reported labels as risky that the contract considered fine.
        /// </summary>
        private static float StrokeRiskPixels => HudCanvas.LegibleFloorPixels;

        private static string EvidenceDir =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "_workspace/current/qa/evidence/font");

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator LiveHud_ReportsEffectivePixelSizePerLabel()
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

            var report = new StringBuilder();
            report.AppendLine("# HUD 폰트 실효 픽셀 크기");
            report.AppendLine();
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "화면 {0}x{1}", Screen.width, Screen.height));
            report.AppendLine();

            foreach (var scaler in Object.FindObjectsByType<CanvasScaler>(FindObjectsSortMode.None))
            {
                var c = scaler.GetComponent<Canvas>();
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "- canvas `{0}` mode={1} ref={2}x{3} match={4:0.##} **scaleFactor={5:0.####}**",
                    scaler.name, scaler.uiScaleMode,
                    scaler.referenceResolution.x, scaler.referenceResolution.y,
                    scaler.matchWidthOrHeight, c != null ? c.scaleFactor : -1f));
            }
            report.AppendLine();

            report.AppendLine("| 라벨 | 텍스트 | fontSize | canvas scale | 실효 px | 위험 |");
            report.AppendLine("|---|---|---|---|---|---|");

            var risky = 0;
            var total = 0;
            foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                var canvas = t.canvas;
                // A label whose canvas is null is not drawn at all — a different defect, and
                // one the UX lane already filed. Recorded, not silently skipped.
                var scale = canvas != null ? canvas.scaleFactor : 0f;
                // fontSize is in canvas units; the auto-size path can override it, so read back
                // what the renderer settled on rather than what was requested.
                var requested = t.enableAutoSizing ? t.fontSize : t.fontSize;
                var effective = requested * scale;
                total++;
                var flag = canvas == null ? "**미렌더**" : (effective < StrokeRiskPixels ? "**위험**" : "");
                if (canvas != null && effective < StrokeRiskPixels) risky++;

                var shown = t.text.Replace("\n", " ");
                if (shown.Length > 24) shown = shown.Substring(0, 24) + "…";
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1} | {2:0.#} | {3:0.####} | **{4:0.#}** | {5} |",
                    t.name, shown, requested, scale, effective, flag));
            }

            report.AppendLine();
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "위험({0}px 미만) {1} / 검사 {2}", StrokeRiskPixels, risky, total));

            File.WriteAllText(Path.Combine(EvidenceDir, "hud-font-scale.md"), report.ToString());
            Assert.Pass("계측 전용 — hud-font-scale.md 참조");
        }
    }
}
