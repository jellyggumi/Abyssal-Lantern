using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the shipped default aim to reaching the enemy keep.
    ///
    /// A player who never touches the aim keys gets `LaunchManager`'s serialized defaults. Those
    /// defaults shipped at `aimPower = 0.55`, which left the muzzle at 10.975 m/s and landed the
    /// shot at x = -4.7 — inside the player's OWN keep, which stands at x = -7..-4. The user
    /// reported it as "the arc is drawn somewhere it cannot reach the enemy".
    ///
    /// It was not wrong when written. Task #60 lowered <see cref="LaunchPowerCurve.MaxSpeed"/> from
    /// 25.2 to 17.5 and 0.55 reached at the old speed; the default went stale because nothing tied
    /// it to the constant it depends on. That is what these tests are: the tie. Every number below
    /// is read from the type that owns it, so moving `MaxSpeed`, the apron, or the keep profile
    /// turns this suite red instead of silently un-aiming the default.
    /// </summary>
    public sealed class AimDefaultReachTests
    {
        private const float Dt = 0.02f;
        private const int MaxSteps = 400;
        private const float HalfBlock = 0.5f;

        private static float ApronAbsX => StageDefinitions.Stage1.launchApronAbsX;
        private static float SpawnY => UnitController.DefaultLaunchSpawnHeight;
        private static float CeilingY => UnitController.DefaultHardCeilingY;

        private static float OutpostAbsX => GameManager.KeepProfile.First(c => c.IsOutpost).AbsX;
        private static float InnerCourseAbsX => GameManager.KeepProfile.Max(c => c.AbsX);
        private static float OuterCourseAbsX => GameManager.KeepProfile.Min(c => c.AbsX);

        /// <summary>
        /// The serialized defaults, read off a fresh component.
        ///
        /// Deliberately NOT reflection over the field initialisers: this reads what a scene
        /// instantiation would hand the player. The GameObject is created inactive so `Awake` never
        /// runs and this cannot steal <c>GameManager.Instance</c> or any other singleton slot —
        /// the idiom `CurrentRosterBalanceGateTests` established.
        /// </summary>
        private static (float angle, float power, float minSpeed, float maxSpeed, float powerStep) ShippedAim()
        {
            var go = new GameObject("aim-default-probe");
            go.SetActive(false);
            try
            {
                var lm = go.AddComponent<LaunchManager>();
                return (lm.aimAngleDegrees, lm.aimPower, lm.minLaunchVelocity, lm.maxLaunchVelocity, lm.powerStep);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        /// <summary>
        /// Integrates one shot the way the runtime does: semi-implicit Euler, gravity, the hard
        /// ceiling clamp, and the velocity from <see cref="OneShotSiegeRules.Velocity"/> rather than
        /// a local sin/cos. Going through that function is the point — its LINEAR
        /// `Lerp(minSpeed, maxSpeed, power)` is what makes 0.55 fall short, and a test that
        /// recomputed the speed itself would not see a change to it.
        /// </summary>
        private static float LandingX(float angleDegrees, float power, float minSpeed, float maxSpeed)
        {
            Vector2 v = OneShotSiegeRules.Velocity(angleDegrees, power, minSpeed, maxSpeed, 1f);
            float x = -ApronAbsX, y = SpawnY;
            float peakY = y;
            bool descending = false;

            for (int i = 0; i < MaxSteps; i++)
            {
                if (y > CeilingY && v.y > 0f) v.y = 0f;
                v.y += Physics2D.gravity.y * Dt;
                x += v.x * Dt;
                y += v.y * Dt;

                if (y > peakY) peakY = y;
                else descending = true;

                // Only a descending crossing counts. Testing y <= SpawnY from the first step
                // returns the launch point itself, which is how the offline model behind the
                // intake brief reported x = -17 and mis-stated this defect twice.
                if (descending && y <= SpawnY) return x;
                if (y < -1f) return x;
            }
            return float.NaN;
        }

        private static string Where(float landingX)
        {
            if (float.IsNaN(landingX)) return "never landed inside the step budget";
            if (landingX <= -OuterCourseAbsX + HalfBlock && landingX >= -InnerCourseAbsX - HalfBlock)
                return $"the PLAYER'S OWN keep (x {-InnerCourseAbsX:F1}..{-OuterCourseAbsX:F1})";
            if (landingX < 0f) return "the player's own half, short of the field";
            if (Mathf.Abs(landingX - OutpostAbsX) <= HalfBlock)
                return $"the enemy FORWARD OUTPOST (x {OutpostAbsX:F1})";
            if (landingX >= OuterCourseAbsX - HalfBlock && landingX <= InnerCourseAbsX + HalfBlock)
                return $"the enemy keep (x {OuterCourseAbsX:F1}..{InnerCourseAbsX:F1})";
            if (landingX > InnerCourseAbsX + HalfBlock) return "past the keep, toward the core or beyond";
            return "the neutral field between the keeps";
        }

        /// <summary>
        /// Without this, a default that cannot cross the field ships again. The whole complaint
        /// that opened this investigation is one key-press away from being invisible: a player who
        /// nudges the aim keys never sees it, so only the untouched default exposes it.
        /// </summary>
        [Test]
        public void ShippedDefaultAim_LandsOnTheEnemyKeepRatherThanSomewhereItCannotReach()
        {
            var aim = ShippedAim();
            float landing = LandingX(aim.angle, aim.power, aim.minSpeed, aim.maxSpeed);

            Assert.That(landing, Is.Not.NaN,
                $"The default shot ({aim.angle:F0}deg, power {aim.power:F2}) never came down within "
                + $"{MaxSteps} steps of {Dt}s. Either the ceiling clamp changed or the speed did.");

            Assert.That(landing, Is.GreaterThanOrEqualTo(OutpostAbsX - HalfBlock),
                $"The default aim ({aim.angle:F0}deg, power {aim.power:F2}, speed "
                + $"{OneShotSiegeRules.Velocity(aim.angle, aim.power, aim.minSpeed, aim.maxSpeed, 1f).magnitude:F3} m/s) "
                + $"lands at x={landing:F2} — {Where(landing)}. The enemy keep starts at the forward "
                + $"outpost x={OutpostAbsX:F1}. A player who never touches the aim keys cannot reach the "
                + "enemy at all. This usually means a speed constant moved without the default moving "
                + $"with it: MaxSpeed is currently {LaunchPowerCurve.MaxSpeed:F1}.");

            Assert.That(landing, Is.LessThanOrEqualTo(InnerCourseAbsX + HalfBlock),
                $"The default aim lands at x={landing:F2} — {Where(landing)}. It overshoots the keep "
                + $"(inner course x={InnerCourseAbsX:F1}), so the opening shot sails past every wall it "
                + "is supposed to teach the player to break.");
        }

        /// <summary>
        /// Stated separately from the reach assertion on purpose. Falling short and demolishing your
        /// own wall are the same failed reach but different player experiences, and this one is the
        /// symptom the user actually described. Kept independent so it still speaks when the reach
        /// assertion fails for an unrelated reason.
        /// </summary>
        [Test]
        public void ShippedDefaultAim_DoesNotDetonateOnThePlayersOwnKeep()
        {
            var aim = ShippedAim();
            float landing = LandingX(aim.angle, aim.power, aim.minSpeed, aim.maxSpeed);
            float ownNear = -OuterCourseAbsX, ownFar = -InnerCourseAbsX;

            Assert.That(landing >= ownFar - HalfBlock && landing <= ownNear + HalfBlock, Is.False,
                $"The default aim lands at x={landing:F2}, inside the player's OWN keep "
                + $"(x {ownFar:F1}..{ownNear:F1}). Firing with the shipped defaults destroys your own "
                + "wall and does nothing to the enemy — the defect the user reported as the arc being "
                + "drawn where it cannot reach. ShotReadback calls this outcome a LOSS, not a hit.");
        }

        /// <summary>
        /// A default sitting on the edge of the reaching band is one key-press from breaking, and
        /// `powerStep` is the size of that press. This measures the room on both sides in presses,
        /// so a future retune cannot leave the default technically-reaching but practically fragile.
        /// </summary>
        [Test]
        public void ReachingBand_LeavesRoomOnBothSidesOfTheDefaultMeasuredInKeyPresses()
        {
            var aim = ShippedAim();
            float lo = float.NaN, hi = float.NaN;

            for (int i = 0; i <= 200; i++)
            {
                float p = i / 200f;
                float x = LandingX(aim.angle, p, aim.minSpeed, aim.maxSpeed);
                bool reaches = !float.IsNaN(x)
                    && x >= OutpostAbsX - HalfBlock
                    && x <= InnerCourseAbsX + HalfBlock;
                if (reaches)
                {
                    if (float.IsNaN(lo)) lo = p;
                    hi = p;
                }
            }

            Assert.That(lo, Is.Not.NaN,
                $"No draw between 0 and 1 lands on the enemy keep at {aim.angle:F0}deg. The keep is "
                + "unreachable at the default angle, which is a geometry or speed problem, not a "
                + "default-value problem.");

            float below = (aim.power - lo) / aim.powerStep;
            float above = (hi - aim.power) / aim.powerStep;

            Assert.That(below, Is.GreaterThanOrEqualTo(1f),
                $"The default power {aim.power:F2} sits {below:F1} presses above the bottom of the "
                + $"reaching band ({lo:F2}..{hi:F2}) at powerStep {aim.powerStep:F2}. One press down "
                + "and the shot no longer reaches, so the default is on a cliff.");
            Assert.That(above, Is.GreaterThanOrEqualTo(1f),
                $"The default power {aim.power:F2} sits {above:F1} presses below the top of the "
                + $"reaching band ({lo:F2}..{hi:F2}). One press up and the shot overshoots the keep.");
        }

        /// <summary>
        /// The integrator itself, checked against the closed form before any judgment rests on it.
        /// The offline model behind the intake brief was wrong twice — a retired MaxSpeed and an
        /// ascent counted as a landing — and both would have been caught here. A measurement device
        /// gets validated before it gets believed.
        /// </summary>
        [Test]
        public void TheIntegratorAgreesWithTheClosedFormSoItsVerdictsMeanSomething()
        {
            var aim = ShippedAim();
            float speed = aim.maxSpeed;
            float analyticRange = speed * speed * Mathf.Sin(2f * 45f * Mathf.Deg2Rad) / -Physics2D.gravity.y;

            // Flat-ground closed form starts and ends at the same height; the integrator starts at
            // SpawnY and ends there too, so the two are directly comparable.
            float landing = LandingX(45f, 1f, aim.minSpeed, aim.maxSpeed);
            float measuredRange = landing - -ApronAbsX;

            Assert.That(measuredRange, Is.EqualTo(analyticRange).Within(0.02f * analyticRange),
                $"At 45deg and full draw the integrator flew {measuredRange:F2}u where the closed form "
                + $"says {analyticRange:F2}u. Every landing figure in this file is suspect until that "
                + "agrees — this is the check that would have caught the two errors in the offline "
                + "model that mis-stated this defect twice.");
        }
    }
}
