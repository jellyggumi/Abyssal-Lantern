using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the draw→speed curve that fixed the aiming space.
    ///
    /// The defect these guard was measured, not felt: at 45° the live scene landed at x = −8.96 /
    /// 0.23 / 18.25 / 25.04 for 40/60/80/100% draw, so the band that lands on the keep was six
    /// percentage points hidden inside a twenty-point step. Both causes were arithmetic — range is
    /// quadratic in speed, and the cap was sized for 64.7u of reach against 26u of need.
    ///
    /// The tests state the PROPERTY rather than the constant wherever possible, so a later tuning
    /// pass can move a number and still be told the moment it leaves the band the measurement
    /// supports.
    /// </summary>
    public class LaunchPowerCurveTests
    {
        private const float Gravity = 9.81f;
        private const float ApronAbsX = 17.0f;
        private const float KeepNear = 6.0f;
        private const float KeepFar = 11.5f;

        /// <summary>Ideal no-drag landing x for a 45° shot from the player apron.</summary>
        private static float LandingAt45(float speed) => -ApronAbsX + speed * speed / Gravity;

        [Test]
        public void ZeroDraw_IsAZeroShot()
        {
            Assert.AreEqual(0f, LaunchPowerCurve.SpeedForDraw(0f), 1e-5f);
            Assert.AreEqual(0f, LaunchPowerCurve.SpeedForDraw(-1f), 1e-5f, "a negative draw cannot fire");
        }

        [Test]
        public void FullDraw_ReachesTheCap()
        {
            Assert.AreEqual(LaunchPowerCurve.MaxSpeed, LaunchPowerCurve.SpeedForDraw(1f), 1e-4f);
            Assert.AreEqual(LaunchPowerCurve.MaxSpeed, LaunchPowerCurve.SpeedForDraw(3f), 1e-4f,
                "an over-full draw clamps rather than exceeding the cap");
        }

        /// <summary>
        /// A deeper pull must always be a longer shot. Without this the gesture is unlearnable —
        /// the player cannot build a mental model of a dial that folds back on itself.
        /// </summary>
        [Test]
        public void Curve_IsMonotoneInDraw()
        {
            float previous = -1f;
            for (int i = 0; i <= 100; i++)
            {
                float speed = LaunchPowerCurve.SpeedForDraw(i / 100f);
                Assert.Greater(speed, previous - 1e-6f, $"draw {i}% must not fire shorter than {i - 1}%");
                previous = speed;
            }
        }

        /// <summary>
        /// The point of the curve: distance responds LINEARLY to draw. Range goes as v², so a
        /// linear speed curve made distance quadratic and put most of the board in the back half of
        /// the pull. Halving the draw should roughly halve the distance travelled.
        /// </summary>
        [Test]
        public void DistanceIsLinearInDraw_NotQuadratic()
        {
            float full = LandingAt45(LaunchPowerCurve.SpeedForDraw(1f)) + ApronAbsX;   // distance travelled
            float half = LandingAt45(LaunchPowerCurve.SpeedForDraw(0.5f)) + ApronAbsX;

            Assert.AreEqual(0.5f, half / full, 0.02f,
                "half the draw must travel about half the distance — a linear speed curve gives a "
                + "quarter, which is the defect this replaced");
        }

        /// <summary>
        /// A full draw must clear the keep but not by much. Overshooting has to stay POSSIBLE (the
        /// player may want to reach past a wall), while the pull must stop spending most of its
        /// travel beyond the board — which is what a 64.7u reach against 26u of need did.
        /// </summary>
        [Test]
        public void FullDraw_LandsJustPastTheKeep()
        {
            float landing = LandingAt45(LaunchPowerCurve.SpeedForDraw(1f));
            Assert.Greater(landing, KeepFar,
                "a full draw must be able to overshoot, or the far edge is unreachable");
            Assert.Less(landing, KeepFar + 8f,
                $"a full draw landing at {landing:F1} wastes most of the pull beyond the board; "
                + "the previous cap reached 47.7");
        }

        /// <summary>
        /// The headline: how much of the pull lands on the keep at 45°.
        ///
        /// Measured against the OLD curve by the same arithmetic this test uses, so the comparison
        /// is like-for-like: 11.2%p before, 17.6%p after — a 1.6x widening.
        ///
        /// The first version of this test asserted 20%p, quoting 28.6 from a different measurement
        /// (a walls-included simulation counting wall hits as success). The test failed and it was
        /// right to: 17.6 is what THIS metric yields, and citing a number from another metric is
        /// how a threshold ends up unfalsifiable. The floor is now stated as a ratio against the
        /// old curve, which cannot drift out of date the way a literal can.
        /// </summary>
        [Test]
        public void KeepWindow_IsWiderThanTheLinearCurveItReplaced()
        {
            const float oldCap = 25.2f;   // the linear curve this replaced

            int newInWindow = 0, oldInWindow = 0;
            for (int i = 0; i <= 1000; i++)
            {
                float draw = i / 1000f;
                if (Landed(LaunchPowerCurve.SpeedForDraw(draw))) newInWindow++;
                if (Landed(oldCap * draw)) oldInWindow++;      // old: speed linear in draw
            }

            float newWidth = newInWindow / 10f;   // to percentage points
            float oldWidth = oldInWindow / 10f;

            Assert.Greater(newWidth, oldWidth * 1.4f,
                $"the draw band landing on the keep is {newWidth:F1}%p against the old curve's "
                + $"{oldWidth:F1}%p. The live scene measured about 6%p of usable window in practice, "
                + "which is why the angle appeared unable to hit the enemy at all");

            Assert.GreaterOrEqual(newWidth, 15f,
                $"{newWidth:F1}%p is too fine a needle to aim at regardless of the improvement");
        }

        private static bool Landed(float speed)
        {
            float x = LandingAt45(speed);
            return x >= KeepNear && x <= KeepFar;
        }

        [Test]
        public void DrawForSpeed_InvertsSpeedForDraw()
        {
            for (int i = 0; i <= 10; i++)
            {
                float draw = i / 10f;
                float roundTrip = LaunchPowerCurve.DrawForSpeed(LaunchPowerCurve.SpeedForDraw(draw));
                Assert.AreEqual(draw, roundTrip, 1e-3f, $"round trip must preserve draw {draw:F1}");
            }
        }

        /// <summary>
        /// The power readout reports the pull, not the speed. Under the √ curve a half draw makes
        /// 70.7% of max speed, so a speed-based percentage would tell the player they pulled harder
        /// than they did — and the pull is the thing they are learning to repeat.
        /// </summary>
        [Test]
        public void ReportedPower_MatchesTheDrawNotTheSpeed()
        {
            float speedAtHalfDraw = LaunchPowerCurve.SpeedForDraw(0.5f);
            float speedRatio = speedAtHalfDraw / LaunchPowerCurve.MaxSpeed;

            Assert.AreEqual(0.5f, LaunchPowerCurve.DrawForSpeed(speedAtHalfDraw), 1e-3f);
            Assert.Greater(speedRatio, 0.6f,
                "precondition: the speed ratio really does differ from the draw, or this test is vacuous");
        }

        /// <summary>
        /// Both sides must fire the same weapon.
        ///
        /// This is a source guard because the field default is NOT what runs: Unity serializes
        /// public fields into the scene, so an edited default is ignored while the scene keeps its
        /// old value. That exact trap cost this project a session before — task #49's kegs were
        /// authored in the scene while every fix was applied to the code table. The scene carried
        /// maxLaunchVelocity: 25.2 twice, for the player and the AI.
        /// </summary>
        [Test]
        public void Scene_DoesNotCarryAStaleLaunchCap()
        {
            var scenePath = Path.Combine(Application.dataPath, "Scenes/SampleScene.unity");
            Assert.IsTrue(File.Exists(scenePath), $"expected the arena scene at {scenePath}");

            var text = File.ReadAllText(scenePath);
            var matches = Regex.Matches(text, @"maxLaunchVelocity:\s*([-\d.]+)");
            Assert.Greater(matches.Count, 0, "the scene should serialize a launch cap to check");

            foreach (Match m in matches)
            {
                float serialized = float.Parse(m.Groups[1].Value,
                    System.Globalization.CultureInfo.InvariantCulture);
                Assert.AreEqual(LaunchPowerCurve.MaxSpeed, serialized, 0.01f,
                    $"the scene serializes maxLaunchVelocity: {serialized} while the curve caps at "
                    + $"{LaunchPowerCurve.MaxSpeed}. The serialized value wins at runtime, so a "
                    + "code-only change would not reach the game");
            }
        }
    }
}
