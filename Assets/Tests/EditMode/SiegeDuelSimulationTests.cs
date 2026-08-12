using System.Collections.Generic;
using CastleBusters;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The G2 instrument. These pin the properties a win-rate measurement must have before its
    /// output is allowed to decide a gate: reproducibility, independent streams per side, and a
    /// separation between the structural first-move edge and an actual balance fault.
    ///
    /// Numbers here are asserted as relationships and bands, not as exact values. An assertion
    /// copied from a printed result tests that the code still does what it did, which is not the
    /// same as testing that it does the right thing.
    /// </summary>
    public class SiegeDuelSimulationTests
    {
        private static SiegeBalanceSettings Settings => SiegeBalanceSettings.Default;

        [Test]
        public void SameSeed_ReproducesTheSameMatch()
        {
            // A measurement that cannot be repeated is an anecdote.
            var a = SiegeDuelSimulation.RunMatch(Settings, seed: 7);
            var b = SiegeDuelSimulation.RunMatch(Settings, seed: 7);

            Assert.AreEqual(a.playerWon, b.playerWon);
            Assert.AreEqual(a.turns, b.turns);
            Assert.AreEqual(a.winnerMargin, b.winnerMargin, 0.0001f);
        }

        [Test]
        public void DifferentSeeds_ProduceDifferentMatches()
        {
            // If seeds did not separate outcomes, a "series" would be one match counted N times
            // and every aggregate below would be meaningless.
            var results = SiegeDuelSimulation.RunSeries(Settings, seed: 100, matches: 40);
            var turnCounts = new HashSet<int>();
            foreach (var r in results) turnCounts.Add(r.turns);

            Assert.Greater(turnCounts.Count, 1, "40 seeds producing one identical match means the streams are not seeded");
        }

        [Test]
        public void EachSide_DrawsFromAnIndependentStream()
        {
            // The whole reason this type exists. SiegePacingSimulation draws both sides from ONE
            // stream, so their errors are correlated and turn order decides everything. Proof of
            // independence here: swapping ONLY who moves first must not simply mirror the result,
            // because the two sides are sampling different noise.
            int mirrored = 0;
            const int trials = 60;
            for (int i = 0; i < trials; i++)
            {
                var first = SiegeDuelSimulation.RunMatch(Settings, seed: 500 + i, playerFirst: true);
                var second = SiegeDuelSimulation.RunMatch(Settings, seed: 500 + i, playerFirst: false);
                if (first.turns == second.turns) mirrored++;
            }

            Assert.Less(mirrored, trials,
                "identical turn counts on every seed would mean both sides share one noise stream");
        }

        [Test]
        public void PlayerAlwaysFirst_ExposesAStructuralAdvantage()
        {
            // THE FINDING this instrument was built to surface. Both sides need the same number
            // of turns to break a keep, so whoever shoots first arrives one turn earlier and the
            // aim spread is far too narrow to overturn it. This is a property of the balance
            // model, not a bug in the sim — and the shipped game always gives the player the
            // first shot.
            var results = SiegeDuelSimulation.RunSeries(
                Settings, seed: 1, matches: SiegeDuelSimulation.RequiredMatches, alternateFirstMove: false);
            var series = SiegeDuelSimulation.Summarize(results, Settings.secondsPerTurn);

            Assert.Greater(series.PlayerWinRate, SiegeDuelSimulation.G2UpperBound,
                "first-mover advantage must show up as an out-of-band win rate, not be smoothed away");
            Assert.IsFalse(series.InsideG2Band, "an out-of-band rate must fail the gate check");
        }

        [Test]
        public void AlternatingFirstMove_IsolatesBalanceFromTurnOrder()
        {
            // With turn order neutralised and skill equal, the model itself is fair. Reading this
            // together with the test above is what separates "the balance is broken" from "the
            // player always shoots first".
            var results = SiegeDuelSimulation.RunSeries(
                Settings, seed: 1, matches: SiegeDuelSimulation.RequiredMatches, alternateFirstMove: true);
            var series = SiegeDuelSimulation.Summarize(results, Settings.secondsPerTurn);

            Assert.IsTrue(series.InsideG2Band,
                $"equal skill with alternating turn order must land in 45-55%, got {series.PlayerWinRate:P1}");
        }

        [Test]
        public void FirstMoverWinRate_IsReportedSeparatelyFromPlayerWinRate()
        {
            // The diagnostic that tells a reader WHICH of the two situations they are looking at.
            var results = SiegeDuelSimulation.RunSeries(
                Settings, seed: 42, matches: 100, alternateFirstMove: true);
            var series = SiegeDuelSimulation.Summarize(results, Settings.secondsPerTurn);

            Assert.Greater(series.firstMoverWinRate, series.PlayerWinRate,
                "whoever moves first should win more often than the player does under alternation");
        }

        [Test]
        public void MoreSkill_NeverLowersTheWinRate()
        {
            // Monotonicity. A model where aiming better can lose you the match is inverted
            // somewhere, and no amount of tuning on top of it would help.
            float none = SiegeDuelSimulation.WinRateWithSkillDelta(Settings, seed: 11, skillDelta: 0f, matches: 100);
            float small = SiegeDuelSimulation.WinRateWithSkillDelta(Settings, seed: 11, skillDelta: 0.03f, matches: 100);
            float large = SiegeDuelSimulation.WinRateWithSkillDelta(Settings, seed: 11, skillDelta: 0.08f, matches: 100);

            Assert.LessOrEqual(none, small);
            Assert.LessOrEqual(small, large);
        }

        [Test]
        public void SmallSkillEdge_AlreadyDecidesTheMatch()
        {
            // Sensitivity, recorded as a fact rather than a complaint: a 3-point aim-quality edge
            // is not a small advantage in this model. G5 asks the same question about paid
            // advantage (delta <=5%p); if skill alone swings this hard, the comeback mechanics
            // are the only thing standing between a slight edge and a landslide.
            float edge = SiegeDuelSimulation.WinRateWithSkillDelta(Settings, seed: 3, skillDelta: 0.03f, matches: 100);

            Assert.Greater(edge, 0.60f,
                "documenting the sensitivity — if this ever drops, the damage curve was flattened");
        }

        [Test]
        public void AverageMatchLength_AgreesWithTheMatchLengthModel()
        {
            // Two independent statements about the same thing: MatchLengthModel predicts a
            // duration from material and damage, this sim plays it out. Disagreement means one of
            // them is lying, and a balance built on both would inherit the lie.
            var results = SiegeDuelSimulation.RunSeries(Settings, seed: 5, matches: 100, alternateFirstMove: true);
            var series = SiegeDuelSimulation.Summarize(results, Settings.secondsPerTurn);

            Assert.That(series.averageSeconds,
                Is.InRange(
                    MatchLengthModel.TargetMatchSeconds * (1f - MatchLengthModel.ToleranceFraction),
                    MatchLengthModel.TargetMatchSeconds * (1f + MatchLengthModel.ToleranceFraction)),
                $"simulated {series.averageSeconds:F0}s is outside the model's own +/-20% band");
        }

        [Test]
        public void DegenerateSettings_TerminateInsteadOfHanging()
        {
            // Zero damage would loop forever. A test suite that hangs reports nothing at all,
            // which is strictly worse than a test that fails.
            var noDamage = new SiegeBalanceSettings(
                SiegeBalanceSettings.DefaultMapId, SiegeBalanceSettings.DefaultSiegeWeaponId,
                wallBlockCount: 12, wallBlockHp: 90f, coreHp: 360f,
                baseShotDamage: 0f, secondsPerTurn: 7.5f, fixedAimQuality: 0f, beginnerAimError: 0f);

            var result = SiegeDuelSimulation.RunMatch(noDamage, seed: 1);

            Assert.AreEqual(SiegeDuelSimulation.MaxTurnsPerMatch, result.turns, "must stop at the cap");
            Assert.IsFalse(result.playerWon, "nobody wins a match nobody can damage");
        }

        [Test]
        public void EmptySeries_ReportsNoDataAndFailsTheBand()
        {
            var series = SiegeDuelSimulation.Summarize(new List<SiegeDuelResult>(), 7.5f);

            Assert.AreEqual(-1f, series.PlayerWinRate, 0.0001f, "no data must be negative, not 0");
            Assert.IsFalse(series.InsideG2Band, "no data must not pass the gate");
        }

        [Test]
        public void Summary_MatchesHandArithmetic()
        {
            var results = new List<SiegeDuelResult>
            {
                new SiegeDuelResult(true,  40, true,  100f),
                new SiegeDuelResult(false, 30, true,   50f),
                new SiegeDuelResult(true,  50, false,  20f),
                new SiegeDuelResult(false, 40, false, 300f),
            };

            var series = SiegeDuelSimulation.Summarize(results, 7.5f);

            Assert.AreEqual(4, series.matches);
            Assert.AreEqual(2, series.playerWins);
            Assert.AreEqual(0.5f, series.PlayerWinRate, 0.0001f);
            Assert.AreEqual(40f, series.averageTurns, 0.0001f);
            Assert.AreEqual(300f, series.averageSeconds, 0.0001f);
            // playerWon == playerMovedFirst on entries 1 and 4.
            Assert.AreEqual(0.5f, series.firstMoverWinRate, 0.0001f);
        }
    }
}
