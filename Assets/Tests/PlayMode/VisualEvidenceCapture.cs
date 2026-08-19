using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Produces the screenshots a reader needs to check a claim without running anything, and
    /// asserts the same numbers in the same pass — so an image and the value it illustrates
    /// can never drift apart. A capture tool that only photographs proves nothing; a test that
    /// only asserts cannot be read by anyone who does not run it.
    ///
    /// Renders through an explicit RenderTexture rather than ScreenCapture.CaptureScreenshot,
    /// because batchmode has no swap chain to grab and would hand back empty frames.
    /// </summary>
    public class VisualEvidenceCapture
    {
        private const int ShotWidth = 1280;
        private const int ShotHeight = 720;

        private static string EvidenceDir =>
            Path.Combine(Directory.GetParent(Application.dataPath).FullName,
                "_workspace/current/qa/evidence/visual");

        private readonly StringBuilder measurements = new StringBuilder();
        private static readonly Regex McpSceneReloadError = new Regex(
            @"(?:^|<b>McpManagerClientHub</b></color> )(?:Server forcefully disconnected this plugin\. Reason: Authorization failed\. Token may be missing, invalid, or revoked\.|Version handshake failed: No response from server\.)$");

        private static readonly System.Collections.Generic.List<(string condition, LogType type)>
            SceneReloadFailures = new System.Collections.Generic.List<(string condition, LogType type)>();
        private static bool capturingSceneReloadLogs;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
            // Cleared per test: Unity keeps one fixture instance across [UnityTest] methods,
            // so without this the second test's file also carries the first test's rows.
            measurements.Clear();
            HeroGrowth.Reset();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                EndSceneReloadLogCapture();
            }
            finally
            {
                HeroGrowth.Reset();
            }
        }

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

        private static IEnumerator BootArena()
        {
            yield return ReloadArena(() =>
            {
                GameManager.PendingStage = StageId.Stage1;
                SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            });

            // Skip the cold-open VIDEO the way a player does. `ReloadArena`'s fixed 1.5s wait was a
            // bet that the cutscene would be gone by then, and on 2026-08-19 the bet lost: a faster
            // boot left the video's full-screen `Frame` covering the board, and the harness wrote
            // four prologue stills labelled as gameplay states. `Skip()` settles it as a keypress
            // would, so what follows is reached deliberately instead of hopefully.
            //
            // The title screen is NOT skipped here and the board is NOT waited for: `ux-1-title`
            // exists to photograph the title, which legitimately covers the whole screen. The wait
            // belongs after `BeginSiege()`, where the board is what the label claims.
            NarrativeVideoIntro.Active?.Skip();
            yield return null;
        }

        /// <summary>
        /// Waits until the gameplay board is what the camera would photograph.
        ///
        /// `ReloadArena` waits a fixed 1.5 realtime seconds, and that turned out to be a bet on how
        /// long the cold-open prologue takes. On 2026-08-19 the bet lost: quieting the editor's MCP
        /// plugin made boot faster, the prologue was still covering the screen, and the harness
        /// wrote four frames of webtoon art labelled `ux-1-title` through `ux-4-enemy-turn` — with
        /// `ux-measurements.txt` correctly reporting `state=AITurn playerTurn=False buttons=0`
        /// beside a picture of a prologue panel. The measurements read the model; the pixels came
        /// from whatever was on top. A capture whose label and image disagree is worse than no
        /// capture, because it gets cited: `ux-3-player-turn.png` is the evidence three UX defects
        /// point at for "this label is absent from screen".
        ///
        /// So the wait is on the CONDITION, not the clock. A prologue panel is a full-screen
        /// graphic; the HUD is not. Poll until nothing is covering the board and fail loudly rather
        /// than photograph the cover.
        /// </summary>
        private static IEnumerator WaitForBoardVisible(float timeoutSeconds = 12f)
        {
            float waited = 0f;
            string blocker = null;

            while (waited < timeoutSeconds)
            {
                blocker = FullScreenOccluder();
                if (blocker == null) yield break;
                waited += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.Fail(
                $"After {timeoutSeconds:F0}s the board is still covered by '{blocker}', so every "
                + "frame this run captured would show that instead of the game. The prologue may "
                + "have grown longer, or something new is drawing full-screen. Capturing anyway "
                + "would produce images whose labels disagree with their pixels.");
        }

        /// <summary>
        /// The name of an active graphic covering most of the screen, or null when the board is
        /// clear. Measured from the rect rather than by looking for known prologue class names — a
        /// list of those goes stale the moment a new cutscene ships, and this repository has paid
        /// for list-walking checks five times over (CLAUDE.md §5).
        /// </summary>
        private static string FullScreenOccluder()
        {
            float screenArea = Screen.width * (float)Screen.height;
            if (screenArea <= 0f) return null;

            foreach (var g in Object.FindObjectsByType<UnityEngine.UI.Graphic>(FindObjectsSortMode.None))
            {
                if (!g.isActiveAndEnabled) continue;
                if (g.canvas == null) continue;
                if (g.color.a < 0.5f) continue;              // a faded-out panel occludes nothing
                if (g is TMPro.TextMeshProUGUI) continue;     // text never covers the board

                var corners = new Vector3[4];
                g.rectTransform.GetWorldCorners(corners);
                var cam = g.canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : g.canvas.worldCamera;
                var a = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
                var b = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
                float area = Mathf.Abs(b.x - a.x) * Mathf.Abs(b.y - a.y);

                // 80%: the prologue panels are full-bleed, while the widest HUD element measured in
                // this repo is the 700x26 flow strip - three orders of magnitude below this.
                if (area >= screenArea * 0.8f) return g.name;
            }
            return null;
        }

        /// <summary>
        /// Renders the live camera into a texture we own. Returns false when there is no camera
        /// to render, so a missing frame is reported rather than written as a black PNG that
        /// looks like evidence.
        ///
        /// The HUD needs deliberate handling. Every canvas here is ScreenSpaceOverlay, which
        /// draws straight to the backbuffer and is therefore invisible to a camera render — the
        /// first pass of this capture produced correct-looking frames with no UI in them at all,
        /// which is a worse failure than a black image because it looks like a finding. Each
        /// canvas is switched to ScreenSpaceCamera for the shot and restored immediately after.
        /// </summary>
        private static bool Shoot(string label)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (c.isActiveAndEnabled) { cam = c; break; }
                }
            }
            if (cam == null) return false;

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var restore = new (Canvas canvas, RenderMode mode, Camera cam, float dist)[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                restore[i] = (c, c.renderMode, c.worldCamera, c.planeDistance);
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                // Just inside the near plane: far enough not to be clipped, near enough that
                // no world geometry can slide in front of the HUD.
                c.planeDistance = cam.nearClipPlane + 0.01f;
            }
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(ShotWidth, ShotHeight, 24);
            var previousTarget = cam.targetTexture;
            var previousActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(ShotWidth, ShotHeight, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, ShotWidth, ShotHeight), 0, 0);
            tex.Apply();

            cam.targetTexture = previousTarget;
            RenderTexture.active = previousActive;

            foreach (var (canvas, mode, worldCam, dist) in restore)
            {
                if (canvas == null) continue;
                canvas.renderMode = mode;
                canvas.worldCamera = worldCam;
                canvas.planeDistance = dist;
            }
            Canvas.ForceUpdateCanvases();

            File.WriteAllBytes(Path.Combine(EvidenceDir, label + ".png"), tex.EncodeToPNG());

            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            return true;
        }

        /// <summary>
        /// Reads the state the frame is meant to show. Written beside the image so a later
        /// reader can tell what was true when the shutter opened.
        /// </summary>
        private void Record(string label)
        {
            measurements.AppendLine("## " + label);
            measurements.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "stacks sword={0} shield={1} boots={2}",
                HeroGrowth.Stacks(true, HeroItemType.Sword),
                HeroGrowth.Stacks(true, HeroItemType.Shield),
                HeroGrowth.Stacks(true, HeroItemType.Boots)));
            measurements.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "mult damage={0:0.000} hp={1:0.000} speed={2:0.000}",
                HeroGrowth.DamageMult(true), HeroGrowth.HpMult(true), HeroGrowth.SpeedMult(true)));

            var seen = 0;
            foreach (var unit in Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None))
            {
                if (!unit.isPlayerUnit || seen >= 3) continue;
                seen++;
                measurements.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "unit {0} damage={1:0.0} maxHP={2:0.0} speed={3:0.00}",
                    unit.unitType, unit.attackDamage, unit.maxHP, unit.moveSpeed));
            }
            if (seen == 0) measurements.AppendLine("unit (none on field)");
            measurements.AppendLine();
        }

        private void Flush(string file)
        {
            File.WriteAllText(Path.Combine(EvidenceDir, file), measurements.ToString());
        }

        /// <summary>
        /// The series carry-over, photographed at each state and asserted in the same pass.
        /// Baseline → looted → next game (kept) → rematch (cleared).
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator HeroGrowth_CarryOver_CapturedAndAsserted()
        {
            yield return BootArena();

            Assert.IsTrue(Shoot("hg-1-baseline"), "A baseline frame needs a live camera");
            Record("hg-1-baseline");
            var knight = FindPlayerKnight();
            var baseDamage = knight != null ? knight.attackDamage : -1f;
            var baseHp = knight != null ? knight.maxHP : -1f;
            Assert.Greater(baseDamage, 0f, "The baseline needs a player knight to measure against");

            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(true, HeroItemType.Shield);
            HeroGrowth.Grant(true, HeroItemType.Boots);
            yield return null;

            Assert.IsTrue(Shoot("hg-2-looted"), "A looted frame needs a live camera");
            Record("hg-2-looted");
            // Growth bakes in at spawn, so a unit already standing keeps its numbers. This is
            // the frame that says so: the counter moved and the knight did not.
            var afterLoot = FindPlayerKnight();
            Assert.AreEqual(baseDamage, afterLoot.attackDamage, 0.0001f,
                "Loot must not retroactively buff a unit already on the field");

            GameManager.RequestNextGame();
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.IsTrue(Shoot("hg-3-nextgame"), "A next-game frame needs a live camera");
            Record("hg-3-nextgame");
            Assert.AreEqual(1, HeroGrowth.Stacks(true, HeroItemType.Sword),
                "다음 경기 continues the series, so the sword must survive the reload");
            var carried = FindPlayerKnight();
            Assert.IsNotNull(carried, "The next game must spawn a player knight");
            Assert.AreEqual(baseDamage * 1.15f, carried.attackDamage, 0.0001f,
                "The carried sword must reach the next game's knight");
            Assert.AreEqual(baseHp * 1.20f, carried.maxHP, 0.0001f,
                "The carried shield must reach the next game's knight");

            GameManager.RequestRematch();
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.IsTrue(Shoot("hg-4-rematch"), "A rematch frame needs a live camera");
            Record("hg-4-rematch");
            Assert.Zero(HeroGrowth.Stacks(true, HeroItemType.Sword),
                "재대결 starts a new series, so the sword must be gone");
            var reset = FindPlayerKnight();
            Assert.IsNotNull(reset, "The rematch must spawn a player knight");
            Assert.AreEqual(baseDamage, reset.attackDamage, 0.0001f,
                "A new series must return the knight to its baseline damage");

            Flush("hero-growth-measurements.txt");
        }

        private static UnitController FindPlayerKnight()
        {
            foreach (var unit in Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None))
            {
                if (unit.isPlayerUnit && unit.unitType == UnitType.Knight) return unit;
            }
            return null;
        }

        /// <summary>
        /// The states a UX audit argues about: title, match start, player turn, and the AI turn
        /// the player cannot act during. No assertions about layout — the frames are the
        /// finding, and the counts beside them say how much is on screen at once.
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator InGameUx_StatesCaptured()
        {
            // This is a capture harness: its job is to photograph states, not to police the
            // editor's MCP plugin. That plugin logs a version-handshake failure on its own
            // schedule, and an unhandled error log fails whichever test is running when it
            // fires — this run died after writing ux-1-title.png and before reaching the
            // enemy turn, which is the frame the whole test exists for. Same seam as
            // `AimErrorConversionProbe` :53 and `CastleMaterialCensusProbe` :32.
            //
            // The frames are still verified: `Shoot` returns false without a live camera and
            // every call site asserts on it, so a missing capture fails on its own terms.
            LogAssert.ignoreFailingMessages = true;
            try
            {
            yield return BootArena();

            // Set again: `BootArena` loads a scene, and the load resets this static. Without the
            // second assignment the flag is on for the boot and off for every capture after it,
            // which is why a run could photograph all four frames correctly and still fail.
            LogAssert.ignoreFailingMessages = true;

            Assert.IsTrue(Shoot("ux-1-title"), "A title frame needs a live camera");
            RecordUx("ux-1-title");

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "The arena must have a GameManager");
            gm.BeginSiege();
            yield return null;
            yield return new WaitForSecondsRealtime(1.0f);

            // From here the labels claim gameplay, so the board has to actually be what the camera
            // sees. The 1.0s above is presentation settle, not a guarantee — this is the guarantee.
            yield return WaitForBoardVisible();

            Assert.IsTrue(Shoot("ux-2-match-start"), "A match-start frame needs a live camera");
            RecordUx("ux-2-match-start");

            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsTrue(Shoot("ux-3-player-turn"), "A player-turn frame needs a live camera");
            RecordUx("ux-3-player-turn");

            // The enemy turn, which is 34.1% of a match and had never been photographed. The
            // docstring above has claimed this frame since the test was written; the body took
            // three and stopped, so every argument about that state cited a screenshot that did
            // not exist. UX-015, and it blocks UX-014 in both directions - there is no way to
            // verify a fix to the enemy turn, and no way to justify re-grading it, without a
            // picture of it.
            //
            // Reached by firing rather than by waiting out the clock: the player's shot ends the
            // turn, so this is the same boundary the game uses. Aim is the tuned default the
            // player would fire with, read off the component instead of invented here.
            var lm = Object.FindObjectOfType<LaunchManager>();
            Assert.IsNotNull(lm, "The arena must have a LaunchManager to fire with");
            lm.SimulateLaunch(lm.GetSeparatedAimVelocity());

            // Wait for the turn to actually flip. A fixed sleep would photograph whatever frame it
            // landed on and call it the enemy turn - which is how a capture harness ends up with a
            // label that does not match its pixels.
            float waited = 0f;
            while (gm.IsPlayerTurn && waited < 12f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsFalse(gm.IsPlayerTurn,
                $"The enemy turn never began within {waited:F1}s of the player's shot, so there is "
                + "no enemy-turn frame to take. Either the shot did not commit or the turn did not "
                + "hand over.");

            // Past the volley resolution, into the dead air the defect is actually about. UX-014
            // measured 109.7s of zero inputs; this frame is what the player looks at during it.
            waited = 0f;
            while (gm.IsResolvingTurn && waited < 12f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.6f);

            Assert.IsTrue(Shoot("ux-4-enemy-turn"), "An enemy-turn frame needs a live camera");
            RecordUx("ux-4-enemy-turn");

            Flush("ux-measurements.txt");
            }
            finally
            {
                LogAssert.ignoreFailingMessages = false;
            }
        }

        private void RecordUx(string label)
        {
            var gm = GameManager.Instance;
            measurements.AppendLine("## " + label);
            measurements.AppendLine(gm == null
                ? "state (no GameManager)"
                : string.Format(CultureInfo.InvariantCulture,
                    "state={0} playerTurn={1} turn={2}", gm.currentState, gm.IsPlayerTurn, gm.TurnCount));

            var texts = 0;
            foreach (var t in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (t.isActiveAndEnabled && !string.IsNullOrWhiteSpace(t.text)) texts++;
            }
            var buttons = 0;
            foreach (var b in Object.FindObjectsByType<UnityEngine.UI.Button>(FindObjectsSortMode.None))
            {
                if (b.isActiveAndEnabled) buttons++;
            }
            measurements.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "onscreen text={0} buttons={1}", texts, buttons));

            // Outline width per label, because "the text looks washed out" is not measurable by
            // squinting at a PNG. UX-012 is exactly this: the scene-authored labels carry 0 while
            // every code-built one carries 0.15-0.18, and a fix that silently fails to apply looks
            // identical to no fix at all.
            var outlines = new List<string>();
            foreach (var t in Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None))
            {
                if (!t.isActiveAndEnabled || string.IsNullOrWhiteSpace(t.text)) continue;
                outlines.Add(string.Format(CultureInfo.InvariantCulture,
                    "{0}={1:F2}", t.name, t.outlineWidth));
            }
            outlines.Sort();
            measurements.AppendLine("outline " + string.Join(" ", outlines));
            measurements.AppendLine();
        }
    }
}
