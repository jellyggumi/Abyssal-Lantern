using CastleBusters;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The match-length equation itself. These pin the arithmetic and the direction each term
    /// pulls, so a later edit cannot quietly invert a relationship — the model exists to stop
    /// balance being tuned by adding walls until it feels right, and a model nobody checks is
    /// just a comment.
    /// </summary>
    public class MatchLengthModelTests
    {
        [Test]
        public void Material_CountsEveryBlockAndTheCore()
        {
            Assert.AreEqual(14 * 85f + 300f, MatchLengthModel.Material(14, 85f, 300f), 0.001f);
        }

        [Test]
        public void Turns_AreMaterialOverDamage()
        {
            Assert.AreEqual(10f, MatchLengthModel.TurnsToDecide(420f, 42f), 0.001f);
        }

        [Test]
        public void Seconds_AreTurnsTimesTurnLength()
        {
            Assert.AreEqual(85f, MatchLengthModel.SecondsToDecide(420f, 42f, 8.5f), 0.001f);
        }

        [Test]
        public void MoreMaterial_MakesALongerMatch()
        {
            float lean = MatchLengthModel.EstimatedMatchSeconds(7, 85f, 300f);
            float heavy = MatchLengthModel.EstimatedMatchSeconds(14, 85f, 300f);
            Assert.Greater(heavy, lean, "doubling the keep must lengthen, not shorten, a match");
        }

        [Test]
        public void MoreDamage_MakesAShorterMatch()
        {
            float slow = MatchLengthModel.SecondsToDecide(1490f, 30f, 8.5f);
            float fast = MatchLengthModel.SecondsToDecide(1490f, 60f, 8.5f);
            Assert.Less(fast, slow, "landing more damage per turn must shorten a match");
        }

        [Test]
        public void ZeroDamage_DoesNotDivideByZero()
        {
            // A caller probing the model with a zero must get a large number, not an infinity
            // or a crash — this runs inside a test that reports on tuning, not in a hot path.
            Assert.IsFalse(float.IsNaN(MatchLengthModel.TurnsToDecide(1490f, 0f)));
            Assert.IsFalse(float.IsInfinity(MatchLengthModel.TurnsToDecide(1490f, 0f)));
        }

        [Test]
        public void TheInverse_RoundTripsWithTheForwardModel()
        {
            // Deriving material from a target and feeding it back must return the target,
            // otherwise the two directions disagree and one of them is lying.
            float material = MatchLengthModel.MaterialForTargetSeconds(MatchLengthModel.TargetMatchSeconds);
            float seconds = MatchLengthModel.SecondsToDecide(
                material,
                MatchLengthModel.EffectiveDamagePerTurn,
                MatchLengthModel.AverageTurnSeconds);
            Assert.AreEqual(MatchLengthModel.TargetMatchSeconds, seconds, 0.01f);
        }

        [Test]
        public void FixedAimScenario_RepeatsTheSameMapHealthWeaponAndDuration()
        {
            var settings = SiegeBalanceSettings.Default;
            SiegeMatchMeasurement first = SiegePacingSimulation.RunFixedAim(settings);
            SiegeMatchMeasurement second = SiegePacingSimulation.RunFixedAim(settings);

            Assert.That(first.mapId, Is.EqualTo(SiegeBalanceSettings.DefaultMapId));
            Assert.That(first.siegeWeaponId, Is.EqualTo(SiegeBalanceSettings.DefaultSiegeWeaponId));
            Assert.That(first.profile, Is.EqualTo(SiegeSimulationProfile.FixedAim));
            Assert.That(first.initialKeepDurability, Is.EqualTo(settings.KeepDurability));
            Assert.That(second.initialKeepDurability, Is.EqualTo(settings.KeepDurability));
            Assert.That(first.durationSeconds, Is.EqualTo(second.durationSeconds));
            Assert.That(first.turns, Is.EqualTo(second.turns));
            Assert.That(settings.KeepDurability, Is.EqualTo(12 * 90f + 360f));
        }

        [Test]
        public void FixedAimScenario_AppliesOpeningCompensationToFirstShotOnly()
        {
            var settings = new SiegeBalanceSettings(
                SiegeBalanceSettings.DefaultMapId,
                SiegeBalanceSettings.DefaultSiegeWeaponId,
                wallBlockCount: 0,
                wallBlockHp: 0f,
                coreHp: 150f,
                baseShotDamage: 100f,
                secondsPerTurn: 1f,
                fixedAimQuality: 1f,
                beginnerAimError: 0f);

            SiegeMatchMeasurement measurement = SiegePacingSimulation.RunFixedAim(settings);

            Assert.That(
                measurement.turns,
                Is.EqualTo(4),
                "one compensated opening shot followed by full-strength shots must decide this boundary duel on turn four");
        }

        [Test]
        public void BeginnerAimError_TwentyMatchGateMeetsFiveMinuteDistribution()
        {
            var measurements = SiegePacingSimulation.RunBeginnerSeries(SiegeBalanceSettings.Default, 20260811);
            float average = SiegePacingSimulation.AverageDuration(measurements);
            int earlyEnds = SiegePacingSimulation.EarlyEndCount(measurements);

            Assert.That(measurements, Has.Count.EqualTo(SiegePacingSimulation.RequiredBeginnerMatches));
            foreach (SiegeMatchMeasurement measurement in measurements)
            {
                Assert.That(measurement.mapId, Is.EqualTo(SiegeBalanceSettings.DefaultMapId));
                Assert.That(measurement.siegeWeaponId, Is.EqualTo(SiegeBalanceSettings.DefaultSiegeWeaponId));
                Assert.That(measurement.initialKeepDurability, Is.EqualTo(SiegeBalanceSettings.Default.KeepDurability));
                Assert.That(measurement.profile, Is.EqualTo(SiegeSimulationProfile.BeginnerAimError));
            }
            Assert.That(average, Is.InRange(
                SiegePacingSimulation.MinimumAverageSeconds,
                SiegePacingSimulation.MaximumAverageSeconds));
            Assert.That(earlyEnds, Is.LessThanOrEqualTo(SiegePacingSimulation.MaximumEarlyEndMatches));
        }
    }
}
