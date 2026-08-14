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
                $"d={MatchLengthModel.EffectiveDamagePerTurn} is below the player-only rate "
                + $"({honestD:F1}) in Stage1, and below it in every stage measured — a consistent "
                + "2.6x to 3.5x understatement rather than the 24x spread first reported, which was "
                + "Stage3 measured while its castle failed to build");
        }

        /// <summary>
        /// d is a distribution, not a constant, and a single number erases the part that matters.
        ///
        /// This one survived the Stage3 correction. Even with the castle standing, 6 of 14 shots deal
        /// nothing, the median is 62.5 against a mean of 128.00, and one barrel did 560. A model
        /// consuming only the mean describes a turn that rarely happens — which is the complaint that
        /// opened this investigation, stated as arithmetic.
        /// </summary>
        [Test]
        public void TheMeasuredDamagePerTurn_HasATailAConstantCannotCarry()
        {
            // Stage3 per-shot damage, re-measured with its castle present.
            // qa/evidence/match-length/b1-stage3-remeasured.md
            float[] stage3 = { 0, 130, 175, 465, 150, 560, 85, 0, 40, 0, 0, 187, 0, 0 };

            var sorted = (float[])stage3.Clone();
            System.Array.Sort(sorted);
            float median = 0.5f * (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]);

            float mean = 0f;
            foreach (var v in stage3) mean += v;
            mean /= stage3.Length;

            int zeros = 0;
            foreach (var v in stage3) if (v == 0f) zeros++;

            Assert.AreEqual(128.00f, mean, 0.01f,
                "guard: this array must reproduce the measured mean or it is not the measured data");
            Assert.Greater(mean, median * 1.5f,
                $"the mean ({mean:F2}) must sit well above the median ({median:F2}) - a heavy tail is "
                + "the property a single constant cannot carry");
            Assert.GreaterOrEqual(zeros, stage3.Length / 4,
                $"{zeros} of {stage3.Length} shots dealt nothing; if that fraction ever drops below a "
                + "quarter the aiming problem improved and this figure needs re-recording");
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
                // Re-measured with the castle present. The first pass read 0.19 / 28.3 / 5.31 on a
                // stage booting without walls or a core (task #63).
                ("Stage3",  8f / 14f, 1792f /  8f, 128.00f),
            };

            foreach (var m in measured)
            {
                Assert.AreEqual(m.d, MatchLengthModel.DamagePerTurn(m.p, m.q), 0.02f,
                    $"{m.stage}: p={m.p:F3} x q={m.q:F1} must reproduce the measured d");
            }
        }

        /// <summary>
        /// The two factors are separable but NOT ranked, and that distinction is the useful part.
        ///
        /// I first wrote this test asserting accuracy beats damage. It failed: closing Stage1's hit
        /// rate to Stage2's level yields 110.7 while a 15% damage buff yields 111.1. Of course it
        /// does — p and q enter d multiplicatively, so a 15% gain in either is a 15% gain in d. There
        /// is no arithmetic ranking to discover, and a test claiming one was asserting a preference
        /// dressed as a measurement.
        ///
        /// What the factorisation is actually for: p and q are measured separately, so a tuning pass
        /// can say WHICH it moved and verify it moved. Choosing between them is a design question
        /// about cost and side effects — accuracy changes what the player can do, damage changes what
        /// their shots are worth — and this test deliberately refuses to prejudge it.
        /// </summary>
        [Test]
        public void HitRateAndPerShotDamage_AreSeparableButNotRanked()
        {
            const float stage1HitRate = 16f / 22f;
            const float stage1PerLanded = 2125f / 16f;

            float asIs = MatchLengthModel.DamagePerTurn(stage1HitRate, stage1PerLanded);

            // The same relative gain applied to each factor in turn.
            const float gain = 1.15f;
            float viaAccuracy = MatchLengthModel.DamagePerTurn(stage1HitRate * gain, stage1PerLanded);
            float viaDamage = MatchLengthModel.DamagePerTurn(stage1HitRate, stage1PerLanded * gain);

            Assert.Greater(viaAccuracy, asIs, "raising accuracy alone must raise damage per turn");
            Assert.Greater(viaDamage, asIs, "raising per-shot damage alone must also raise it");

            Assert.AreEqual(viaAccuracy, viaDamage, 0.01f,
                $"equal relative gains must produce equal results ({viaAccuracy:F2} vs "
                + $"{viaDamage:F2}); if they diverge, d has stopped being the plain product of p and "
                + "q and every figure derived from the factorisation needs rechecking");
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
                Assert.Greater(s.selfShare, 0.2f,
                    $"{s.stage}: a quarter of a keep's own destruction cannot be treated as noise");
                Assert.Less(s.selfShare, 1f, $"{s.stage}: share must be a fraction");
            }

            // Stage3 was the extreme reading, and correcting it is part of the record.
            var stage3 = System.Array.Find(MatchLengthModel.SelfInflictedShareIsNotModelled,
                                           s => s.stage == "Stage3");
            Assert.Less(stage3.selfShare, 0.5f,
                "with its castle present Stage3's defender no longer out-damages the attacker (0.26). "
                + "The 0.67 first recorded came from a stage that booted without walls or a core, "
                + "where almost nothing the attacker did could land");
        }
    }
}
