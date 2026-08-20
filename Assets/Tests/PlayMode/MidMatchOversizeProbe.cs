using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Plays a match turn by turn, deploys a cannon, and counts oversized renderers at every step.
    ///
    /// A start-of-match census found exactly one renderer over 2.5 world units — the background,
    /// legitimately 81.5u — and so did a census after one shot landed. The report that prompted this
    /// showed turn 7, a spent supply counter, `D → 화포 설치` in the hint line, and HIT plus BLOOM
    /// plus falling leaves at once. None of that is turn 1.
    ///
    /// The reporter could not say where the rectangles came from, which is fair: they appear during
    /// play, not at a state a probe can load directly. So this walks the match instead of guessing
    /// at it, and records the FIRST turn anything oversized appears rather than only whether it
    /// eventually does — a count at the end cannot say what produced it.
    ///
    /// Cannon deployment gets its own pass because the HUD in the screenshot pointed at it, and
    /// because it is the one action that buys a turn INSTEAD of a shot, so a shot-only walk never
    /// exercises it.
    ///
    /// What it found: `HiggsfieldCollapseDustAccent` at 3.64 units and `HiggsfieldImpactAccent` at
    /// 2.61, appearing from turn 1 on every impact and collapse. Both are `GameFeelRingPulse`, which
    /// assigned `finalRadius` directly to localScale — a world radius only for a sprite already 1
    /// unit across. Its procedural ring is exactly that (48px at 48 ppu); the Higgsfield art is
    /// 512px at 100 ppu, i.e. 5.12 units, so every request came out 5.12x too large. A collapse
    /// accent asking for 0.71 drew at 3.63. After the fix, neither appears at any turn.
    /// </summary>
    public class MidMatchOversizeProbe
    {
        // 3.0, measured rather than chosen.
        //
        // The defect this probe was written to find drew at 3.63-3.67 units: Higgsfield VFX art is
        // 5.12 units native and GameFeelRingPulse was assigning finalRadius straight to localScale,
        // so a 0.71 request became 0.71 x 5.12. With that fixed, the largest legitimate thing the
        // walk produces is 2.76 (an arrival burst on a deployed unit) followed by 2.64 (a rally
        // rune). 3.0 sits above both and below the defect, so this catches a return of the class
        // without flagging content that was never wrong.
        //
        // The background is excluded by name, not by threshold: it is 78-81 units by design, and a
        // threshold high enough to clear it would be high enough to miss everything else.
        private const float OversizeThreshold = 3.0f;
        private const int TurnsToWalk = 9;

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator NothingOversizedAppearsAcrossNineTurnsOrOnCannonDeployment()
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

            var report = new List<string>();
            var firstSightings = new List<string>();
            report.Add($"threshold: {OversizeThreshold:F2}u   turns walked: {TurnsToWalk}");
            report.Add("");

            // Baseline, so a hit later in the walk can be told from something that was always there.
            var baseline = Oversized();
            report.Add($"turn {gm.TurnCount} (start): {baseline.Count} oversized");
            foreach (var line in baseline.Take(6)) report.Add("    " + line);

            var known = new HashSet<string>(baseline.Select(Key));

            for (int step = 0; step < TurnsToWalk; step++)
            {
                int turnBefore = gm.TurnCount;

                if (gm.currentState == GameState.GameOver)
                {
                    report.Add($"match ended at turn {turnBefore}; stopping the walk");
                    break;
                }

                // Fire when it is ours to fire, otherwise let the AI beat play out.
                if (gm.IsPlayerTurn && !gm.IsResolvingTurn)
                {
                    lm.SimulateLaunch(lm.GetSeparatedAimVelocity());
                }

                // Long enough for flight, impact, the burst that follows, and the turn handoff.
                yield return new WaitForSecondsRealtime(3.0f);

                var now = Oversized();
                var fresh = now.Where(l => !known.Contains(Key(l))).ToList();
                foreach (var l in fresh) known.Add(Key(l));

                report.Add($"turn {gm.TurnCount}: {now.Count} oversized, {fresh.Count} new");
                foreach (var l in fresh.Take(8))
                {
                    report.Add("    NEW " + l);
                    firstSightings.Add($"turn {gm.TurnCount}: {l}");
                }
            }

            // Cannon deployment. The HUD in the report showed this armed, and it is the one action
            // that consumes a turn instead of firing, so the walk above never reaches it.
            report.Add("");
            var deployment = DeploymentController.Instance;
            if (deployment == null)
            {
                report.Add("no DeploymentController; cannon pass skipped");
            }
            else
            {
                // Wait for a player turn rather than forcing state — TryDeploy checks it, and a
                // forced deploy would test a path the game cannot reach.
                float waited = 0f;
                while ((!gm.IsPlayerTurn || gm.IsResolvingTurn) && waited < 12f
                       && gm.currentState != GameState.GameOver)
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                    waited += 0.5f;
                }

                report.Add($"cannon pass: playerTurn={gm.IsPlayerTurn} resolving={gm.IsResolvingTurn} "
                           + $"supply={deployment.PlayerSupply:F1} waited={waited:F1}s");

                // On the apron, player side, clear of the wall courses.
                var spot = new Vector2(-9f, 0.6f);
                var reason = deployment.TryDeploy(DeployCard.Cannon, spot, true);
                report.Add($"TryDeploy(Cannon) -> {reason}");

                yield return new WaitForSecondsRealtime(2.5f);

                var afterDeploy = Oversized();
                var freshDeploy = afterDeploy.Where(l => !known.Contains(Key(l))).ToList();
                report.Add($"after deploy: {afterDeploy.Count} oversized, {freshDeploy.Count} new");
                foreach (var l in freshDeploy.Take(8))
                {
                    report.Add("    NEW " + l);
                    firstSightings.Add($"cannon deploy: {l}");
                }

                // And the brick path, which shares fx_spawn with the cannon and is the other thing
                // the supply counter buys.
                var brickController = Object.FindFirstObjectByType<BrickPlacementController>();
                if (brickController != null)
                {
                    report.Add($"brick controller present: yes");
                    yield return new WaitForSecondsRealtime(0.5f);
                    var afterBrick = Oversized();
                    var freshBrick = afterBrick.Where(l => !known.Contains(Key(l))).ToList();
                    report.Add($"after brick check: {afterBrick.Count} oversized, {freshBrick.Count} new");
                    foreach (var l in freshBrick.Take(8))
                    {
                        report.Add("    NEW " + l);
                        firstSightings.Add($"brick path: {l}");
                    }
                }
            }

            var dir = Path.Combine("_workspace", "current", "qa", "evidence", "block-size");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "mid-match-oversize.txt"),
                string.Join("\n", report) + "\n");

            Assert.That(firstSightings, Is.Empty,
                "oversized renderers appeared during play:\n  " + string.Join("\n  ", firstSightings)
                + "\n\nEach line is the FIRST turn that renderer was seen, so the turn number and the "
                + "component list together say what produced it. Full walk: "
                + "_workspace/current/qa/evidence/block-size/mid-match-oversize.txt");
        }

        /// <summary>Every enabled renderer above the threshold, largest first.</summary>
        private static List<string> Oversized()
        {
            var found = new List<(float size, string line)>();
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr == null || sr.sprite == null || !sr.enabled) continue;
                // Two exclusions, both by name and both earned.
                //
                // The backdrop is legitimately the size of the sky, so no threshold can separate it
                // from a defect without also missing every defect.
                //
                // The vent column is legitimately a 7.5-unit pillar: EruptionVentGimmick uses the
                // same `columnHeight` for the effect and for the Physics2D.OverlapArea rect that
                // applies its lift, so the art matches the hazard it advertises. Measured at 8.07
                // and 8.35 — that is 7.5 with FrameAnimEffect's deliberate +/-12% spawn jitter.
                // Excluding it by name rather than raising the bar to 9 keeps the bar at 3, where it
                // still catches the 3.63 defect this probe was written for.
                string objName = sr.gameObject.name;
                if (objName == "Background") continue;
                if (objName.StartsWith("Fx_fx_eruption") || objName.StartsWith("Fx_fx_petals")) continue;
                float size = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                if (size < OversizeThreshold) continue;

                var comps = string.Join("+", sr.GetComponents<Component>()
                    .Where(c => c != null && !(c is Transform))
                    .Select(c => c.GetType().Name));
                found.Add((size, string.Format(CultureInfo.InvariantCulture,
                    "{0,7:F2}u  {1,-26} sprite={2,-22} [{3}]",
                    size, sr.gameObject.name, sr.sprite.name, comps)));
            }
            found.Sort((a, b) => b.size.CompareTo(a.size));
            return found.Select(f => f.line).ToList();
        }

        /// <summary>
        /// Identity for "have I already seen this one". Name plus sprite, without the size, because
        /// an effect animating through frames of different sizes is one sighting rather than several.
        /// </summary>
        private static string Key(string line)
        {
            int i = line.IndexOf("  ", System.StringComparison.Ordinal);
            return i < 0 ? line : line.Substring(i).Trim();
        }
    }
}
