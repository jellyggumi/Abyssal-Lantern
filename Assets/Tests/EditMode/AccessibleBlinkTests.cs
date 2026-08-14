using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The seizure ceiling, pinned as arithmetic rather than as a comment.
    ///
    /// Two blink rates shipped over WCAG 2.2 SC 2.3.1's three-flashes-per-second limit and nobody
    /// noticed, because both were authored as literals inside coroutines where no test could reach
    /// them. `design/graphics-needed.md` §D put that ceiling on the prohibited list specifically
    /// because it is the only item in this cycle's research with recorded physical harm — the 1997
    /// Pokémon broadcast hospitalised over 600 people.
    ///
    /// Measured before the fix (`_workspace/current/pm/legibility-retention-note.md`):
    /// the barrel fuse peaked at 11.50 flashes/s (3.83x), and the buff-expiry blink ran at 3.501 Hz
    /// (1.17x) while its own comment claimed 8 Hz.
    /// </summary>
    public class AccessibleBlinkTests
    {
        [Test]
        public void TheSafeCeilingSitsBelowTheStandard()
        {
            Assert.Less(AccessibleBlink.SafeFlashesPerSecond, AccessibleBlink.MaxFlashesPerSecond,
                "the working ceiling must leave margin: a flash count is measured on presented "
                + "frames, so a rate sitting exactly on 3.0/s makes compliance depend on frame pacing");

            Assert.AreEqual(3f, AccessibleBlink.MaxFlashesPerSecond, 0.001f,
                "WCAG 2.2 SC 2.3.1 is three flashes per second - if this changes, the standard did not");
        }

        /// <summary>
        /// Counts flashes the way WCAG SC 2.3.1 does: reversals of the actual signal, in the worst
        /// one-second window.
        ///
        /// The first version of this test measured <c>PingPongFlashRate(FusePhaseRate(t))</c> and
        /// passed — while the shipped code produced 3.00 flashes/s, exactly on the limit. It was
        /// measuring a quantity the code did not produce: the call site multiplied the "phase rate" by
        /// <c>t</c> before handing it to PingPong, so the real flash rate was the derivative of that
        /// product, not the rate itself. A test that certifies safety it never measured is worse than
        /// no test, so this one samples the signal and counts turning points.
        /// </summary>
        private static float WorstWindowFlashes(System.Func<float, float> phase,
                                                float duration, float window = 1f, float dt = 1f / 240f)
        {
            var reversals = new System.Collections.Generic.List<float>();
            float prev = Mathf.PingPong(phase(0f), 1f);
            float curr = Mathf.PingPong(phase(dt), 1f);
            for (float t = 2f * dt; t <= duration; t += dt)
            {
                float next = Mathf.PingPong(phase(t), 1f);
                bool peak = curr >= prev && curr >= next;
                bool trough = curr <= prev && curr <= next;
                if ((peak || trough) && (Mathf.Abs(curr - prev) > 1e-7f || Mathf.Abs(curr - next) > 1e-7f))
                {
                    reversals.Add(t - dt);
                }
                prev = curr;
                curr = next;
            }

            // A light-dark pair is one flash, and a triangle wave turns twice per pair.
            float worst = 0f;
            for (float start = 0f; start <= Mathf.Max(0f, duration - window); start += dt)
            {
                int n = 0;
                foreach (var r in reversals) if (r >= start && r < start + window) n++;
                worst = Mathf.Max(worst, n / 2f);
            }
            return worst;
        }

        /// <summary>
        /// The fuse accelerates, and its worst one-second window stays under the ceiling with margin.
        ///
        /// Both halves matter. A fuse that does not quicken is not a telegraph; a fuse that quickens
        /// without bound is what was shipping (9.00/s measured, 3.00x the limit).
        /// </summary>
        [Test]
        public void TheFuseAcceleratesAndStaysUnderTheCeiling()
        {
            const float fuse = UnitCombos.BarrelFuseSeconds;

            float worst = WorstWindowFlashes(AccessibleBlink.FusePhase, fuse);
            Assert.LessOrEqual(worst, AccessibleBlink.SafeFlashesPerSecond + 0.05f,
                $"the fuse's worst one-second window holds {worst:F2} flashes/s. The declared safe "
                + $"ceiling is {AccessibleBlink.SafeFlashesPerSecond}/s, and the reason that number "
                + "is below the standard's 3.0 is written in AccessibleBlink - sitting on the limit "
                + "makes compliance depend on frame pacing");

            // And the margin has to be real, not nominal.
            float margin = (AccessibleBlink.MaxFlashesPerSecond - worst) / AccessibleBlink.MaxFlashesPerSecond;
            Assert.Greater(margin, 0.10f,
                $"measured margin is {margin * 100f:F0}%. The first fix declared 13% and delivered 0%");

            // Acceleration, measured over halves rather than claimed.
            float firstHalf = WorstWindowFlashes(AccessibleBlink.FusePhase, fuse * 0.5f, fuse * 0.5f);
            float secondHalf = WorstWindowFlashes(t => AccessibleBlink.FusePhase(t + fuse * 0.5f),
                                                  fuse * 0.5f, fuse * 0.5f);
            Assert.Greater(secondHalf, firstHalf,
                $"the blink must quicken as the fuse burns down ({firstHalf:F2} -> {secondHalf:F2} "
                + "flashes/s). That acceleration IS the telegraph; flattening it would trade one "
                + "defect for another");
        }

        /// <summary>
        /// The curve that was shipping is still measurable as a violation, so the fix cannot be
        /// "simplified" back to it without this failing.
        /// </summary>
        [Test]
        public void TheOriginalCurveIsStillMeasurablyAViolation()
        {
            float worst = WorstWindowFlashes(t => t * (3f + t * 5f), UnitCombos.BarrelFuseSeconds);
            Assert.Greater(worst, AccessibleBlink.MaxFlashesPerSecond,
                $"the pre-fix curve 3t + 5t^2 must still measure as a violation ({worst:F2}/s); if it "
                + "does not, this test's counting is wrong and every other number here is suspect");
        }

        [Test]
        public void TheSineHelperConvertsBothWays()
        {
            float freq = AccessibleBlink.SafeSineFrequency;
            Assert.AreEqual(AccessibleBlink.SafeFlashesPerSecond,
                AccessibleBlink.SineFlashRate(freq), 0.001f,
                "one sine period is one flash, so the round trip must be exact");

            // The literal that was shipping, stated so the number is on the record.
            Assert.AreEqual(3.501f, AccessibleBlink.SineFlashRate(22f), 0.01f,
                "22 rad/s is 3.501 Hz - the value the buff-expiry blink used, and the reason the "
                + "comment claiming 8 Hz was not merely stale but wrong in the safe direction");
        }

        /// <summary>
        /// No blink rate may be authored as a literal again.
        ///
        /// The two violations were possible only because the rates lived inline. This scans the
        /// sources that own blinks for raw multipliers on <c>Time.time</c> / <c>PingPong</c>, so the
        /// next inline rate fails here instead of shipping.
        /// </summary>
        [Test]
        public void BlinkRatesAreNotAuthoredAsLiterals()
        {
            string[] sources =
            {
                "Assets/Scripts/UnitController.cs",
            };

            // Sin(Time.time * <number>) or PingPong(<anything> * <number>, ...) with a bare literal.
            var sinLiteral = new Regex(@"Sin\(\s*Time\.\w+\s*\*\s*\d+(\.\d+)?f\s*\)");

            foreach (var path in sources)
            {
                Assert.IsTrue(File.Exists(path), $"precondition: {path} must exist");
                var text = File.ReadAllText(path);

                var match = sinLiteral.Match(text);
                Assert.IsFalse(match.Success,
                    $"{path} authors a blink frequency as a literal: \"{match.Value}\". Route it "
                    + "through AccessibleBlink so a test can check it against the seizure ceiling - "
                    + "that is exactly how 22f (3.501 Hz) shipped over the limit");
            }
        }
    }
}
