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

        /// <summary>
        /// The model attributes all of a keep's loss to the attacker, and a live match does not.
        ///
        /// `M = b·h + c` with `N = M/d` has exactly one damage term, so it says: one side removes the
        /// other's material at rate d. B1 measured three full matches
        /// (`qa/b1-measurement-findings.md`) and found 39%, 42% and 67% of the enemy's material loss
        /// was inflicted by the enemy itself — the launch apron sits at ±17 while its own keep
        /// courses stand at ±4..7, so a shallow shot fires into its own wall. Stage3 is the extreme:
        /// the player dealt 85 while the enemy did 175 to itself, and the match still resolved.
        ///
        /// This test does not assert a corrected model. It asserts the current one cannot be repaired
        /// by recalibrating d, because the arithmetic shows what recalibration would silently do:
        /// fold the enemy's self-damage into the player's damage term, after which no material or
        /// pacing change can be read. That is why B2 and B3 stay blocked on a model change rather
        /// than on a better constant.
        /// </summary>
        [Test]
        public void RecalibratingDamagePerTurn_WouldAbsorbTheEnemysSelfDamage()
        {
            // Measured in the B1 run, Stage1.
            const float playerDealt = 2125f;
            const float enemySelfInflicted = 1332f;
            const int playerShots = 22;

            float naiveD = (playerDealt + enemySelfInflicted) / playerShots;   // what a probe sees
            float honestD = playerDealt / playerShots;                         // the player's own rate

            Assert.Greater(naiveD, honestD * 1.5f,
                $"a naive recalibration reads d={naiveD:F1} where the player's own contribution is "
                + $"{honestD:F1} - a {naiveD / honestD:F2}x overstatement, entirely from damage the "
                + "enemy did to itself");

            Assert.Less(MatchLengthModel.EffectiveDamagePerTurn, honestD,
                $"d={MatchLengthModel.EffectiveDamagePerTurn} is below even the player-only rate "
                + $"({honestD:F1}) in Stage1, while being 7x ABOVE the Stage3 rate (5.31). One "
                + "constant cannot straddle a 24x spread between stages");
        }

        /// <summary>
        /// d is a distribution, not a constant, and a single number erases the part that matters.
        ///
        /// Stage3's median damage per shot is ZERO — 13 of 16 shots did nothing — while its mean is
        /// 5.31. A model consuming only the mean cannot express "most shots accomplish nothing",
        /// which is precisely the complaint that opened this investigation.
        /// </summary>
        [Test]
        public void TheMeasuredDamagePerTurn_HasATailAConstantCannotCarry()
        {
            // Stage3 per-shot damage, from qa/evidence/match-length/b1-measurement.md.
            float[] stage3 = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 35, 50, 0 };

            var sorted = (float[])stage3.Clone();
            System.Array.Sort(sorted);
            float median = sorted[sorted.Length / 2];

            float mean = 0f;
            foreach (var v in stage3) mean += v;
            mean /= stage3.Length;

            Assert.AreEqual(0f, median, 0.001f,
                "more than half of Stage3's shots deal nothing at all");
            Assert.Greater(mean, median,
                $"the mean ({mean:F2}) sits above the median ({median:F2}) because two shots carry "
                + "94% of the total - reporting that mean as 'damage per turn' describes a turn that "
                + "almost never happens");
        }

        /// <summary>
        /// The factorisation reproduces every measured stage, which is the cheap half of the claim.
        /// </summary>
        [Test]
        public void DamagePerTurn_FactorisesIntoHitRateAndDamagePerLandedShot()
        {
            (string stage, float p, float q, float d)[] measured =
            {
                ("Stage1", 16f / 22f, 2125f / 16f,  96.59f),
                ("Stage2",  5f /  6f,  770f /  5f, 128.33f),
                ("Stage3",  3f / 16f,   85f /  3f,   5.31f),
            };

            foreach (var m in measured)
            {
                Assert.AreEqual(m.d, MatchLengthModel.DamagePerTurn(m.p, m.q), 0.02f,
                    $"{m.stage}: p={m.p:F3} x q={m.q:F1} must reproduce the measured d");
            }
        }

        /// <summary>
        /// And here is the expensive half: the two factors disagree about what is broken.
        ///
        /// This is the test that should stop B3 from tuning the wrong thing. Durability and material
        /// changes move q. Stage3's q is 28.3 against Stage1's 132.8, but its p is 0.19 against 0.73
        /// — and lifting p alone to Stage1's level moves its damage per turn 3.9x, without touching
        /// a single material value. A pass that raises damage while leaving 81% of shots landing on
        /// nothing has improved the number the player never gets to use.
        /// </summary>
        [Test]
        public void HitRate_IsTheFactorStageThreeIsShortOf()
        {
            const float stage3HitRate = 3f / 16f;
            const float stage3PerLanded = 85f / 3f;
            const float stage1HitRate = 16f / 22f;

            float asShipped = MatchLengthModel.DamagePerTurn(stage3HitRate, stage3PerLanded);
            float withStage1Accuracy = MatchLengthModel.DamagePerTurn(stage1HitRate, stage3PerLanded);

            Assert.Greater(withStage1Accuracy / asShipped, 3.5f,
                $"raising only the hit rate takes Stage3 from {asShipped:F2} to "
                + $"{withStage1Accuracy:F2} damage per turn. If this ratio falls below 3.5x the "
                + "measurement changed and the 'fix accuracy, not damage' conclusion needs redoing");

            Assert.Less(stage3HitRate, 0.25f,
                "Stage3's hit rate is the outlier that makes its d unusable, not its damage");
        }

        /// <summary>
        /// The unmodelled term stays visible, and stays out of the equation.
        ///
        /// A future reader will be tempted to add a self-damage term that reproduces these shares.
        /// It would fit perfectly and predict nothing — dividing the total by the sum of two rates
        /// derived from that same total returns the observed turn count by construction. The shares
        /// are recorded so the gap is known; the equation stays honest by not pretending to cover
        /// it until the three causes are separated.
        /// </summary>
        [Test]
        public void TheSelfInflictedShareIsRecordedButNotFoldedIntoTheModel()
        {
            Assert.AreEqual(3, MatchLengthModel.SelfInflictedShareIsNotModelled.Length,
                "three stages were measured");

            foreach (var s in MatchLengthModel.SelfInflictedShareIsNotModelled)
            {
                Assert.Greater(s.selfShare, 0.3f,
                    $"{s.stage}: a share this large cannot be treated as noise");
                Assert.Less(s.selfShare, 1f, $"{s.stage}: share must be a fraction");
            }

            // Stage3 is the case that changes what a win means.
            var stage3 = System.Array.Find(MatchLengthModel.SelfInflictedShareIsNotModelled,
                                           s => s.stage == "Stage3");
            Assert.Greater(stage3.selfShare, 0.5f,
                "in Stage3 the defender destroyed more of its own keep than the attacker did, so a "
                + "Stage3 victory is not evidence the attacker's shots worked");
        }
    }
}
