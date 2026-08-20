using System.Collections;
using System.Globalization;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins that the shot's arc is on screen WHILE the projectile flies, not only after it lands.
    ///
    /// `ShotTraceDirector.Sample` accumulated points and drew nothing; the arc first appeared in
    /// `Seal`, which runs at turn resolution. So the flight was untraced — the player watched a bare
    /// sprite travel and learned its path once the path had stopped being useful. Nothing caught
    /// that, because every existing test measured the arc AFTER a turn resolved, which is exactly
    /// when it did work.
    ///
    /// Measuring mid-flight is therefore the whole point of this file, and it is why the check reads
    /// the renderer during a coroutine rather than after one.
    /// </summary>
    public class LiveArcDuringFlightProbe
    {
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator TheArcIsDrawnWhileTheProjectileIsStillMoving()
        {
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            NarrativeVideoIntro.Active?.Skip();
            yield return null;

            var gm = GameManager.Instance;
            Assert.That(gm, Is.Not.Null, "the arena must have a GameManager");
            gm.BeginSiege();
            yield return new WaitForSecondsRealtime(1.0f);

            var lm = Object.FindFirstObjectByType<LaunchManager>();
            Assert.That(lm, Is.Not.Null, "the arena must have a LaunchManager");

            // Nothing should be drawn before the first shot of the match.
            Assert.That(FindArc(), Is.Null,
                "a player arc exists before anything has been fired; this probe cannot tell a live "
                + "arc from a leftover one if the board opens with one already drawn.");

            // Arm the aim preview the way a player does, BEFORE launching.
            //
            // Without this the probe proves nothing about the reported case: `SimulateLaunch` alone
            // leaves `keyboardAimTouchedThisTurn` false, so `HandleKeyboardFineTune` never draws the
            // preview and "the preview is gone during flight" is true because it was never there.
            // A mutation test caught exactly that — removing the clear in `LaunchUnit` left the
            // probe passing.
            var touched = typeof(LaunchManager).GetField(
                "keyboardAimTouchedThisTurn",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(touched, Is.Not.Null, "keyboardAimTouchedThisTurn is gone or renamed");
            touched.SetValue(lm, true);

            var draw = typeof(LaunchManager).GetMethod(
                "DrawTrajectory",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(draw, Is.Not.Null, "DrawTrajectory is gone or renamed");
            draw.Invoke(lm, new object[] { lm.GetSeparatedAimVelocity() });

            Assert.That(lm.trajectoryLine.positionCount, Is.GreaterThan(1),
                "the aim preview must actually be up before launching, or this measures nothing");

            lm.SimulateLaunch(lm.GetSeparatedAimVelocity());

            // Poll while the shot is airborne. The sample gate is 0.35 world units, so two points
            // exist very early in the flight — but the turn does not resolve for seconds, and that
            // gap between "moving" and "resolved" is the window this asserts on.
            int peakPositions = 0;
            float peakAt = -1f;
            bool sawWhileOpen = false;
            float elapsed = 0f;
            bool aimVisibleDuringFlight = false;
            int aimPeak = 0;

            while (elapsed < 2.5f)
            {
                var arc = FindArc();
                if (arc != null && arc.positionCount > peakPositions)
                {
                    peakPositions = arc.positionCount;
                    peakAt = elapsed;
                }

                // ShotOpen is the director's own word for "the projectile has not resolved yet".
                if (arc != null && arc.positionCount >= 2 && ShotTraceDirector.ShotOpen)
                {
                    sawWhileOpen = true;
                }

                // Same loop, second question: is the AIM arc still up?
                if (ShotTraceDirector.ShotOpen)
                {
                    int aimCount = lm.trajectoryLine != null ? lm.trajectoryLine.positionCount : 0;
                    if (aimCount > aimPeak) aimPeak = aimCount;
                    if (aimCount > 1) aimVisibleDuringFlight = true;
                }

                yield return new WaitForSecondsRealtime(0.1f);
                elapsed += 0.1f;
            }

            // A mid-flight still, because "the arc is too dark" is a claim about what the screen
            // shows and every number here is a claim about the renderer. One is checkable by eye.
            var shotDir = Path.Combine("_workspace", "current", "qa", "evidence", "visibility");
            Directory.CreateDirectory(shotDir);
            lm.SimulateLaunch(lm.GetSeparatedAimVelocity());
            yield return new WaitForSecondsRealtime(0.8f);
            if (ShotTraceDirector.ShotOpen)
            {
                var cam = Camera.main;
                if (cam != null)
                {
                    var rt = new RenderTexture(1280, 720, 24);
                    var prevTarget = cam.targetTexture;
                    var prevActive = RenderTexture.active;
                    cam.targetTexture = rt;
                    cam.Render();
                    RenderTexture.active = rt;
                    var shot = new Texture2D(1280, 720, TextureFormat.RGB24, false);
                    shot.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
                    shot.Apply();
                    cam.targetTexture = prevTarget;
                    RenderTexture.active = prevActive;
                    File.WriteAllBytes(Path.Combine(shotDir, "live-arc-inflight.png"), shot.EncodeToPNG());
                    Object.DestroyImmediate(shot);
                    Object.DestroyImmediate(rt);
                }
            }

            var dir = Path.Combine("_workspace", "current", "qa", "evidence", "visibility");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "live-arc.txt"), string.Format(
                CultureInfo.InvariantCulture,
                "saw arc while shot open: {0}\npeak positions: {1}\nfirst seen at: {2:F1}s\n"
                + "aim preview visible during flight: {3}\naim peak positions: {4}\n",
                sawWhileOpen, peakPositions, peakAt, aimVisibleDuringFlight, aimPeak));

            Assert.That(sawWhileOpen, Is.True,
                $"the arc never had two or more points while the shot was still open. Peak was "
                + $"{peakPositions} positions at {peakAt:F1}s. `Sample` must draw what it has "
                + "accumulated; accumulating silently and drawing in `Seal` puts the arc on screen "
                + "only after the projectile has landed, which is the defect this pins.");

            // The AIM preview must be gone while the shot flies. It is a different renderer from the
            // trace — LaunchManager's `trajectoryLine` plus the dark halo behind it — and nothing
            // cleared it on launch, so the predicted arc stayed drawn over the whole flight. While
            // it was a thin translucent line nobody noticed; with the halo it became a 10.8px navy
            // ribbon from launcher to impact, and four rounds of fixes went into the wrong renderer
            // chasing it.
            Assert.That(aimVisibleDuringFlight, Is.False,
                $"the aim preview was still drawn during flight (peak {aimPeak} positions). Its job "
                + "ends when the unit leaves the sling, and the in-flight trace is what describes the "
                + "shot from then on. Two arcs over one projectile is the defect, whatever either "
                + "one's colour is.");

            Assert.That(peakPositions, Is.GreaterThan(3),
                $"the live arc peaked at {peakPositions} positions. The sample gate is 0.35 world "
                + "units, so any real flight crosses that several times — a strip this short means "
                + "the draw is firing once and then not tracking the projectile.");
        }

        /// <summary>The player's arc core, or null when it has not been created yet.</summary>
        private static LineRenderer FindArc()
        {
            var root = GameObject.Find("ShotTrace_Player");
            if (root == null) return null;
            var core = root.transform.Find("Core");
            return core == null ? null : core.GetComponent<LineRenderer>();
        }
    }
}
