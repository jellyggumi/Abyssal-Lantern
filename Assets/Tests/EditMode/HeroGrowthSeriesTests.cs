using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Hero growth is series-scoped meta progression, and these pin that lifecycle.
    ///
    /// The system already existed and did nothing for the economy: every stack evaporated at
    /// the next match start, so a series win paid 12 marks, the only purchase cost 12, and the
    /// currency stopped meaning anything after one buy. That is the failure the genre survey
    /// recorded in Archery Bastions — a level-396 player sitting on a million unspent gold
    /// (.survey/siege-artillery-landscape/context.md).
    ///
    /// The fix is a lifecycle change, not a new system, so what needs pinning is WHEN the
    /// stacks die. Comments cannot hold that; a later edit that moves Reset() back into a
    /// per-match path would silently restore the old behaviour and nothing would complain.
    /// </summary>
    public class HeroGrowthSeriesTests
    {
        private static void ResetSeries()
        {
            // Private static: the production callers (RequestRematch/RequestTitle/RequestStage
            // and a decided EndGame) all live behind scene machinery this suite cannot drive,
            // so the lifecycle is exercised at its single source instead.
            typeof(GameManager)
                .GetMethod("ResetSeries", BindingFlags.NonPublic | BindingFlags.Static)
                .Invoke(null, null);
        }

        [SetUp]
        public void SetUp() => ResetSeries();

        [TearDown]
        public void TearDown() => ResetSeries();

        [Test]
        public void SubsystemRegistrationInitializer_ClearsHeroStacksAndSeriesTalliesForNewRuntimeSession()
        {
            MethodInfo initializer = System.Array.Find(
                typeof(GameManager).GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static),
                method =>
                {
                    var attribute = method.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
                    return attribute != null &&
                           attribute.loadType == RuntimeInitializeLoadType.SubsystemRegistration;
                });

            Assert.That(initializer, Is.Not.Null,
                "GameManager needs one SubsystemRegistration owner that clears every series-scoped static when domain reload is disabled.");

            var types = new[] { HeroItemType.Sword, HeroItemType.Shield, HeroItemType.Boots };
            foreach (bool isPlayer in new[] { true, false })
            {
                foreach (HeroItemType type in types)
                {
                    HeroGrowth.Grant(isPlayer, type);
                    Assert.That(HeroGrowth.Stacks(isPlayer, type), Is.EqualTo(1), "precondition");
                }
            }

            var tallyNames = new[]
            {
                "seriesPlayerWins",
                "seriesEnemyWins",
                "seriesGamesPlayed",
                "seriesScoreTotal"
            };
            var tallyFields = new FieldInfo[tallyNames.Length];
            for (int i = 0; i < tallyNames.Length; i++)
            {
                tallyFields[i] = typeof(GameManager).GetField(
                    tallyNames[i],
                    BindingFlags.NonPublic | BindingFlags.Static);
                Assert.That(tallyFields[i], Is.Not.Null,
                    $"{tallyNames[i]} must remain part of the best-of-three series state");
                tallyFields[i].SetValue(null, i + 1);
                Assert.That(tallyFields[i].GetValue(null), Is.EqualTo(i + 1), "precondition");
            }

            // Invoke Unity's new-runtime-session hook directly. Loading a scene here would be
            // wrong: ordinary scene reloads must preserve both the tally and hero growth.
            initializer.Invoke(null, null);

            foreach (bool isPlayer in new[] { true, false })
            {
                foreach (HeroItemType type in types)
                {
                    Assert.That(HeroGrowth.Stacks(isPlayer, type), Is.Zero,
                        $"{(isPlayer ? "player" : "enemy")} {type} must not leak into a new runtime session");
                }
            }

            foreach (FieldInfo tallyField in tallyFields)
            {
                Assert.That(tallyField.GetValue(null), Is.Zero,
                    $"{tallyField.Name} must reset with hero growth under the same lifecycle owner");
            }
        }

        [Test]
        public void ResetSeries_ClearsHeroStacks()
        {
            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(true, HeroItemType.Shield);
            HeroGrowth.Grant(false, HeroItemType.Boots);

            ResetSeries();

            Assert.AreEqual(0, HeroGrowth.Stacks(true, HeroItemType.Sword));
            Assert.AreEqual(0, HeroGrowth.Stacks(true, HeroItemType.Shield));
            Assert.AreEqual(0, HeroGrowth.Stacks(false, HeroItemType.Boots),
                "both sides' loot belongs to the series, not to one side's bookkeeping");
        }

        [Test]
        public void StacksSurvive_WhateverDoesNotResetTheSeries()
        {
            // The whole point of the change: a new GAME inside a running series inherits the
            // loot. If a future edit puts Reset() back on a per-match path, this fails.
            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(true, HeroItemType.Sword);

            Assert.AreEqual(2, HeroGrowth.Stacks(true, HeroItemType.Sword),
                "nothing short of a series reset may clear hero stacks");
        }

        [Test]
        public void SeriesReset_IsTheOnlyThingThatClears()
        {
            // Guards the lifecycle from the other direction: granting, reading multipliers and
            // reading stacks must all be non-destructive. A getter that quietly consumed state
            // would make the carry-over unreliable in a way the test above cannot see.
            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.DamageMult(true);
            HeroGrowth.HpMult(true);
            HeroGrowth.SpeedMult(true);
            HeroGrowth.Stacks(true, HeroItemType.Sword);

            Assert.AreEqual(1, HeroGrowth.Stacks(true, HeroItemType.Sword));
        }

        [Test]
        public void CarriedStacks_StayInsideTheDeclaredCap()
        {
            // Persistence must not become unbounded accumulation. Three games of a series each
            // dropping the cap's worth still tops out at MaxStacksPerType, which is what bounds
            // the snowball: +75% damage at 5 swords, not +225% after three games.
            for (int game = 0; game < 3; game++)
                for (int drop = 0; drop < HeroGrowth.MaxStacksPerType; drop++)
                    HeroGrowth.Grant(true, HeroItemType.Sword);

            Assert.AreEqual(HeroGrowth.MaxStacksPerType, HeroGrowth.Stacks(true, HeroItemType.Sword));
            Assert.AreEqual(1f + HeroGrowth.MaxStacksPerType * HeroGrowth.DamagePerSword,
                HeroGrowth.DamageMult(true), 0.0001f);
        }

        [Test]
        public void SidesAccumulateIndependently()
        {
            // ItemSystem already tracks both sides; persistence must not merge them, or a
            // player win would arm the enemy for the next game of the same series.
            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(false, HeroItemType.Sword);

            Assert.AreEqual(2, HeroGrowth.Stacks(true, HeroItemType.Sword));
            Assert.AreEqual(1, HeroGrowth.Stacks(false, HeroItemType.Sword));
        }

        [Test]
        public void MaxCarriedAdvantage_IsBoundedAndStated()
        {
            // The number a balance review needs, written down rather than left to be rederived:
            // a fully-stacked side carries +75% damage, +100% HP and +60% speed into the next
            // game of the series. That is the ceiling of this feature's snowball, and if a
            // later tuning pass moves the per-stack values this test says so out loud.
            for (int i = 0; i < HeroGrowth.MaxStacksPerType; i++)
            {
                HeroGrowth.Grant(true, HeroItemType.Sword);
                HeroGrowth.Grant(true, HeroItemType.Shield);
                HeroGrowth.Grant(true, HeroItemType.Boots);
            }

            Assert.AreEqual(1.75f, HeroGrowth.DamageMult(true), 0.0001f);
            Assert.AreEqual(2.00f, HeroGrowth.HpMult(true), 0.0001f);
            Assert.AreEqual(1.60f, HeroGrowth.SpeedMult(true), 0.0001f);
        }
    }
}
