using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CastleBusters;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// No two HUD readouts may cover each other.
    ///
    /// Adopting <c>windText</c> and <c>scoreText</c> onto the HUD canvas made them visible for
    /// the first time — and immediately put the wind readout on top of the supply gauge, the
    /// deploy toggle and the friendly core badge, all of which had quietly been sharing the
    /// top-left corner with a label nobody could see. Making a thing visible is not finished
    /// until you have looked at where it lands.
    ///
    /// Overlap is measured in screen rects, so it catches a collision regardless of which
    /// system placed either label.
    /// </summary>
    public class HudOverlapTests
    {
        /// <summary>
        /// Fraction of the smaller rect that may sit inside the larger before it counts as a
        /// collision. Not zero: TMP rects carry padding beyond the drawn glyphs, so abutting
        /// labels routinely share a few percent without a visible touch.
        /// </summary>
        private const float AllowedOverlapFraction = 0.10f;

        private static string EvidenceDir =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "_workspace/current/qa/evidence/hud-fix");
        private static readonly Regex McpSceneReloadError = new Regex(
            @"(?:^|<b>McpManagerClientHub</b></color> )(?:Server forcefully disconnected this plugin\. Reason: Authorization failed\. Token may be missing, invalid, or revoked\.|Version handshake failed: No response from server\.)$");

        private static readonly List<(string condition, LogType type)>
            SceneReloadFailures = new List<(string condition, LogType type)>();
        private static bool capturingSceneReloadLogs;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
        }

        [TearDown]
        public void TearDown() => EndSceneReloadLogCapture();
        private static void BeginSceneReloadLogCapture()
        {
            SceneReloadFailures.Clear();
            capturingSceneReloadLogs = true;
            Application.logMessageReceived += CaptureSceneReloadFailure;
            LogAssert.ignoreFailingMessages = true;
        }

        private static void EndSceneReloadLogCapture()
        {
            if (!capturingSceneReloadLogs) return;

            Application.logMessageReceived -= CaptureSceneReloadFailure;
            LogAssert.ignoreFailingMessages = false;
            capturingSceneReloadLogs = false;
            try
            {
                foreach (var failure in SceneReloadFailures)
                {
                    Assert.That(failure.type, Is.EqualTo(LogType.Error),
                        $"Scene reload emitted an unexpected {failure.type}: {failure.condition}");
                    Assert.That(failure.condition, Does.Match(McpSceneReloadError.ToString()),
                        $"Scene reload emitted an unexpected error: {failure.condition}");
                }
            }
            finally
            {
                SceneReloadFailures.Clear();
            }
        }

        private static void CaptureSceneReloadFailure(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
                SceneReloadFailures.Add((condition, type));
        }

        private static IEnumerator ReloadArena(System.Action reload)
        {
            BeginSceneReloadLogCapture();
            try
            {
                reload();
                yield return null;
                yield return new WaitForSecondsRealtime(1.5f);
            }
            finally
            {
                EndSceneReloadLogCapture();
            }
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator HudReadouts_DoNotCoverEachOther()
        {
            yield return ReloadArena(() =>
            {
                GameManager.PendingStage = StageId.Stage1;
                SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            });

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "The arena must have a GameManager");
            gm.BeginSiege();
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            var rects = new List<(string name, Rect rect)>();
            foreach (var t in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || t.canvas == null) continue;
                if (t.canvas.name != HudCanvas.CanvasName) continue;
                if (string.IsNullOrWhiteSpace(t.text)) continue;
                rects.Add((Describe(t), ScreenRect(t)));
            }

            var report = new StringBuilder();
            report.AppendLine("# HUD 겹침 검사");
            report.AppendLine();
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "화면 {0}x{1} · 검사 대상 {2}개 · 허용 겹침 {3:0}%",
                Screen.width, Screen.height, rects.Count, AllowedOverlapFraction * 100f));
            report.AppendLine();

            var collisions = new List<string>();
            for (var i = 0; i < rects.Count; i++)
            {
                for (var j = i + 1; j < rects.Count; j++)
                {
                    var a = rects[i];
                    var b = rects[j];
                    var overlap = Intersection(a.rect, b.rect);
                    if (overlap <= 0f) continue;

                    var smaller = Mathf.Min(a.rect.width * a.rect.height, b.rect.width * b.rect.height);
                    if (smaller <= 0f) continue;
                    var fraction = overlap / smaller;
                    if (fraction <= AllowedOverlapFraction) continue;

                    collisions.Add(string.Format(CultureInfo.InvariantCulture,
                        "{0} × {1} = {2:0}%", a.name, b.name, fraction * 100f));
                }
            }

            report.AppendLine(collisions.Count == 0 ? "겹침 없음." : "## 겹침");
            foreach (var c in collisions) report.AppendLine("- " + c);
            report.AppendLine();
            report.AppendLine("## 배치");
            report.AppendLine();
            report.AppendLine("| 라벨 | x | y | 크기 |");
            report.AppendLine("|---|---|---|---|");
            foreach (var (name, r) in rects)
            {
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1:0}~{2:0} | {3:0}~{4:0} | {5:0}x{6:0} |",
                    name, r.xMin, r.xMax, r.yMin, r.yMax, r.width, r.height));
            }

            File.WriteAllText(Path.Combine(EvidenceDir, "hud-overlap.md"), report.ToString());

            Assert.IsEmpty(collisions,
                "HUD readouts must not cover each other: " + string.Join("; ", collisions));
        }

        private static string Describe(TextMeshProUGUI t)
        {
            // Two core badges are both named "Label", so the text disambiguates them.
            var head = t.text.Length > 12 ? t.text.Substring(0, 12) : t.text;
            return $"{t.name}(\"{head.Replace("\n", " ")}\")";
        }

        /// <summary>
        /// Screen rect of the *rendered text*, not of its RectTransform.
        ///
        /// The first version of this measured the transform and passed while the screen was
        /// visibly wrong: the wind readout is two lines in a box authored for one, so TMP drew
        /// well outside the rect it was asked to fit. A layout test that trusts the box cannot
        /// see the overflow that is the whole problem.
        /// </summary>
        private static Rect ScreenRect(TextMeshProUGUI t)
        {
            t.ForceMeshUpdate();
            var b = t.textBounds;                    // local space, actual glyph extent
            var rt = t.rectTransform;
            var min = rt.TransformPoint(new Vector3(b.min.x, b.min.y, 0f));
            var max = rt.TransformPoint(new Vector3(b.max.x, b.max.y, 0f));
            var minX = Mathf.Min(min.x, max.x);
            var maxX = Mathf.Max(min.x, max.x);
            var minY = Mathf.Min(min.y, max.y);
            var maxY = Mathf.Max(min.y, max.y);
            return new Rect(minX, minY, maxX - minX, maxY - minY);
        }
        private static float Intersection(Rect a, Rect b)
        {
            var w = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            var h = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            return (w <= 0f || h <= 0f) ? 0f : w * h;
        }
    }
}
