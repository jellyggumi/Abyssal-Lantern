using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>One resolved duel. Everything a G2 verdict needs, and nothing it does not.</summary>
    public readonly struct SiegeDuelResult
    {
        public readonly bool playerWon;
        public readonly int turns;
        public readonly bool playerMovedFirst;
        /// <summary>Winner's remaining keep durability. A pile of near-zero margins means the
        /// matchup is a coin flip decided by the last shot, which reads very differently from
        /// the same win rate produced by comfortable wins on both sides.</summary>
        public readonly float winnerMargin;

        public SiegeDuelResult(bool playerWon, int turns, bool playerMovedFirst, float winnerMargin)
        {
            this.playerWon = playerWon;
            this.turns = turns;
            this.playerMovedFirst = playerMovedFirst;
            this.winnerMargin = winnerMargin;
        }
    }

    /// <summary>Aggregate over a duel series — the numbers that go into `qa/gate-measurements.md#g2`.</summary>
    public readonly struct SiegeDuelSeries
    {
        public readonly int matches;
        public readonly int playerWins;
        public readonly float averageTurns;
        public readonly float averageSeconds;
        /// <summary>Win rate of whichever side moved first, across the whole series. Separating
        /// this from <see cref="PlayerWinRate"/> is the point of the alternating mode: it tells
        /// you whether an off-band player win rate is a balance fault or just turn order.</summary>
        public readonly float firstMoverWinRate;

        public SiegeDuelSeries(int matches, int playerWins, float averageTurns, float averageSeconds, float firstMoverWinRate)
        {
            this.matches = matches;
            this.playerWins = playerWins;
            this.averageTurns = averageTurns;
            this.averageSeconds = averageSeconds;
            this.firstMoverWinRate = firstMoverWinRate;
        }

        public float PlayerWinRate => matches == 0 ? -1f : (float)playerWins / matches;

        /// <summary>G2's band. -1 (no data) fails, exactly as an out-of-band rate does.</summary>
        public bool InsideG2Band =>
            matches > 0 &&
            PlayerWinRate >= SiegeDuelSimulation.G2LowerBound &&
            PlayerWinRate <= SiegeDuelSimulation.G2UpperBound;
    }

    /// <summary>
    /// Symmetric aiming-AI duel, built because the harness we already had cannot measure a win
    /// rate.
    ///
    /// Two existing options were both wrong for G2:
    ///
    /// - `Cycle3_PlaytestDataCollection_30Games` fires at RANDOM velocities. A random shot almost
    ///   never reaches a core behind four courses, so its 30 games exhausting 20 turns is a
    ///   statement about the harness rather than the game (D-008, D-014). A win rate from it
    ///   would make the gate lie.
    /// - <see cref="SiegePacingSimulation"/> draws both sides' aim error from ONE stream, so the
    ///   two sides' errors are correlated and the side that shoots first wins structurally. It
    ///   measures pacing correctly and win rate not at all.
    ///
    /// This type fixes exactly that: one independent PRNG stream per side, so "equal skill" means
    /// statistically independent draws from the same distribution rather than the same draw twice.
    ///
    /// WHAT THIS MEASURES, AND WHAT IT DOES NOT. It exercises the balance MODEL — durability,
    /// per-shot damage, projectile rotation, turn order. It does not exercise physics, block
    /// placement, collapse cascades, terrain, or wind, because none of those exist outside a
    /// scene. So it is a first-order check that catches structural imbalance (first-mover
    /// dominance, an asymmetric damage budget, a durability ladder that decides matches before
    /// they start) and cannot catch a balance fault that only appears once blocks fall on each
    /// other. A G2 verdict built on this alone would be overclaiming; PlayMode confirmation is
    /// named as a follow-up in `qa/test-plan.md`.
    /// </summary>
    public static class SiegeDuelSimulation
    {
        public const float G2LowerBound = 0.45f;
        public const float G2UpperBound = 0.55f;

        /// <summary>Series size. 50 carries roughly ±10%p of sampling noise at p≈0.5 — wider than
        /// the 45–55% band itself, so a 50-match series cannot decide the gate it is measuring.
        /// 100 halves that to about ±5%p, which is the coarsest sample that can return a verdict
        /// instead of a shrug.</summary>
        public const int RequiredMatches = 100;

        /// <summary>Hard stop so a mis-tuned setting (zero damage, infinite durability) fails loudly
        /// instead of hanging a test run.</summary>
        public const int MaxTurnsPerMatch = 2000;

        // Same rotation the real game applies (OneShotSiegeRules cycles Knight/Archer/Barrel per
        // round). Mean is 1, so variety cannot silently shift the long-term damage budget.
        private static readonly float[] ProjectileMultipliers = { 1f, 0.95f, 1.05f };

        /// <summary>
        /// One duel. <paramref name="playerFirst"/> chooses turn order;
        /// <paramref name="playerSkill"/>/<paramref name="enemySkill"/> override aim quality so a
        /// skill delta can be dialled in (leave negative for "same as settings", i.e. equal skill).
        /// </summary>
        public static SiegeDuelResult RunMatch(
            SiegeBalanceSettings settings,
            int seed,
            bool playerFirst = true,
            float playerSkill = -1f,
            float enemySkill = -1f)
        {
            float playerKeep = settings.KeepDurability;
            float enemyKeep = settings.KeepDurability;

            // Two streams, offset so they cannot land in phase. Correlated error is exactly the
            // defect that makes the pacing sim unusable for win rate.
            var playerRng = new SplitRandom((uint)(seed * 2 + 1));
            var enemyRng = new SplitRandom((uint)(seed * 2 + 2) ^ 0x9E3779B9u);

            float playerBase = playerSkill < 0f ? settings.fixedAimQuality : playerSkill;
            float enemyBase = enemySkill < 0f ? settings.fixedAimQuality : enemySkill;

            bool playerToAct = playerFirst;
            int turns = 0;

            while (playerKeep > 0f && enemyKeep > 0f && turns < MaxTurnsPerMatch)
            {
                // Rotation advances per ROUND (two turns), matching OneShotSiegeRules: both sides
                // receive the same projectile before it changes, which is the fairness device.
                float multiplier = ProjectileMultipliers[(turns / 2) % ProjectileMultipliers.Length];

                float quality = playerToAct
                    ? playerBase + playerRng.NextSignedUnit() * settings.beginnerAimError
                    : enemyBase + enemyRng.NextSignedUnit() * settings.beginnerAimError;

                float damage = settings.baseShotDamage * multiplier * Mathf.Clamp01(quality);

                if (playerToAct) enemyKeep -= damage;
                else playerKeep -= damage;

                playerToAct = !playerToAct;
                turns++;
            }

            bool won = enemyKeep <= 0f && playerKeep > 0f;
            float margin = Mathf.Max(0f, won ? playerKeep : enemyKeep);
            return new SiegeDuelResult(won, turns, playerFirst, margin);
        }

        /// <summary>
        /// A full series. <paramref name="alternateFirstMove"/> swaps turn order every match so
        /// the structural first-move edge cancels out; leave it false to measure what a player
        /// actually experiences, since the shipped game always gives them the first shot.
        /// Report both — see <see cref="SiegeDuelSeries.firstMoverWinRate"/>.
        /// </summary>
        public static List<SiegeDuelResult> RunSeries(
            SiegeBalanceSettings settings,
            int seed,
            int matches = RequiredMatches,
            bool alternateFirstMove = false,
            float playerSkill = -1f,
            float enemySkill = -1f)
        {
            var results = new List<SiegeDuelResult>(matches);
            for (int i = 0; i < matches; i++)
            {
                bool playerFirst = !alternateFirstMove || (i % 2 == 0);
                results.Add(RunMatch(settings, seed + i, playerFirst, playerSkill, enemySkill));
            }
            return results;
        }

        public static SiegeDuelSeries Summarize(IList<SiegeDuelResult> results, float secondsPerTurn)
        {
            if (results == null || results.Count == 0)
                return new SiegeDuelSeries(0, 0, 0f, 0f, -1f);

            int wins = 0, firstMoverWins = 0, turnTotal = 0;
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                if (r.playerWon) wins++;
                if (r.playerWon == r.playerMovedFirst) firstMoverWins++;
                turnTotal += r.turns;
            }

            float avgTurns = (float)turnTotal / results.Count;
            return new SiegeDuelSeries(
                results.Count,
                wins,
                avgTurns,
                avgTurns * secondsPerTurn,
                (float)firstMoverWins / results.Count);
        }

        /// <summary>
        /// How much a skill gap moves the outcome. G5 asks whether a paid advantage keeps the
        /// win-rate delta ≤5%p; this answers the same question for skill, and a balance where a
        /// small skill edge produces a landslide is one where the comeback mechanics are doing
        /// nothing. Returns the player's win rate when they aim <paramref name="skillDelta"/>
        /// better than the opponent.
        /// </summary>
        public static float WinRateWithSkillDelta(SiegeBalanceSettings settings, int seed, float skillDelta, int matches = RequiredMatches)
        {
            var results = RunSeries(
                settings, seed, matches,
                alternateFirstMove: true, // isolate skill from turn order
                playerSkill: Mathf.Clamp01(settings.fixedAimQuality + skillDelta),
                enemySkill: settings.fixedAimQuality);
            return Summarize(results, settings.secondsPerTurn).PlayerWinRate;
        }

        /// <summary>
        /// PCG-style stream. The pacing sim's plain LCG has weak low-bit behaviour on nearby
        /// seeds, and this sim feeds it seed*2+1 and seed*2+2 — adjacent by construction. Two
        /// streams that start correlated would reintroduce the exact defect this type exists to
        /// remove, so the output is permuted rather than taken raw.
        /// </summary>
        private struct SplitRandom
        {
            private uint state;

            public SplitRandom(uint seed)
            {
                state = seed == 0u ? 0x853C49E6u : seed;
                NextSignedUnit(); // discard one: the first draw after seeding is the least mixed
            }

            public float NextSignedUnit()
            {
                state = state * 747796405u + 2891336453u;
                uint word = ((state >> (int)((state >> 28) + 4u)) ^ state) * 277803737u;
                uint result = (word >> 22) ^ word;
                return (result / 4294967295f) * 2f - 1f;
            }
        }
    }
}
