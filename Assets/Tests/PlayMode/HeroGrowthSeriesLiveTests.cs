using System.Collections;
using System.Text.RegularExpressions;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Does the carry-over actually survive the real path?
    ///
    /// The EditMode suite proves ResetSeries() clears the stacks and that nothing else does,
    /// but it drives the reset directly and never loads a scene. The behaviour players get runs
    /// through a scene reload: finish a game, press 다음 경기, and the arena is rebuilt from
    /// scratch. Start() and StartGame() both run again on that new scene — and those are exactly
    /// the two places the Reset() call was removed from.
    ///
    /// So the EditMode tests cannot see the failure mode that matters: a Reset() left on a boot
    /// path would still let ResetSeries() pass every unit test while wiping the loot on the next
    /// game anyway. Only a real reload catches that.
    /// </summary>
    public class HeroGrowthSeriesLiveTests
    {
        /// <summary>
        /// Explicit isolation, because the assembly-wide ResetSessionState action does not
        /// reach these tests. Measured, not assumed: run in name order, NextGame leaves two
        /// swords and Rematch then reads three after granting one — a leak of exactly two.
        ///
        /// Notably that only became visible with this change in place. While Start() still
        /// called HeroGrowth.Reset(), every scene load scrubbed the stacks and hid the gap.
        /// The isolation was always missing; the old code was just papering over it.
        /// </summary>
        [SetUp]
        public void SetUp()
        {
            HeroGrowth.Reset();
        }


        [TearDown]
        public void TearDown()
        {
            try
            {
                EndSceneReloadLogCapture();
            }
            finally
            {
                HeroGrowth.Reset();
            }
        }

        private static readonly Regex McpSceneReloadError = new Regex(
            @"(?:^|<b>McpManagerClientHub</b></color> )(?:Server forcefully disconnected this plugin\. Reason: Authorization failed\. Token may be missing, invalid, or revoked\.|Version handshake failed: No response from server\.)$");

        private static readonly System.Collections.Generic.List<(string condition, LogType type)>
            SceneReloadFailures = new System.Collections.Generic.List<(string condition, LogType type)>();
        private static bool capturingSceneReloadLogs;

        private static void BeginSceneReloadLogCapture()
        {
            SceneReloadFailures.Clear();
            capturingSceneReloadLogs = true;
            Application.logMessageReceived += CaptureSceneReloadFailure;
            LogAssert.ignoreFailingMessages = true;
        }

        private static void EndSceneReloadLogCapture()
        {
            if (!capturingSceneReloadLogs) return;

            Application.logMessageReceived -= CaptureSceneReloadFailure;
            LogAssert.ignoreFailingMessages = false;
            capturingSceneReloadLogs = false;
            try
            {
                foreach (var failure in SceneReloadFailures)
                {
                    Assert.That(failure.type, Is.EqualTo(LogType.Error),
                        $"Scene reload emitted an unexpected {failure.type}: {failure.condition}");
                    Assert.That(failure.condition, Does.Match(McpSceneReloadError.ToString()),
                        $"Scene reload emitted an unexpected error: {failure.condition}");
                }
            }
            finally
            {
                SceneReloadFailures.Clear();
            }
        }

        private static void CaptureSceneReloadFailure(string condition, string stackTrace, LogType type)
        {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception)
                SceneReloadFailures.Add((condition, type));
        }

        private static IEnumerator ReloadArena(System.Action reload)
        {
            BeginSceneReloadLogCapture();
            try
            {
                reload();
                yield return null;
                yield return new WaitForSecondsRealtime(1.5f);
            }
            finally
            {
                EndSceneReloadLogCapture();
            }
        }

        private static IEnumerator BootArena()
        {
            yield return ReloadArena(() =>
            {
                GameManager.PendingStage = StageId.Stage1;
                SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            });
            Assert.IsNotNull(GameManager.Instance, "the arena must have a GameManager after loading");
        }

        [UnityTest]
        public IEnumerator NextGame_CarriesHeroStacksThroughTheSceneReload()
        {
            yield return BootArena();

            // Loot earned in game one of the series.
            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(false, HeroItemType.Shield);
            Assert.AreEqual(2, HeroGrowth.Stacks(true, HeroItemType.Sword), "precondition");

            // The real "다음 경기" action: continues the series and rebuilds the arena.
            yield return ReloadArena(GameManager.RequestNextGame);

            Assert.AreEqual(2, HeroGrowth.Stacks(true, HeroItemType.Sword),
                "loot earned earlier in the series must survive the reload into the next game");
            Assert.AreEqual(1, HeroGrowth.Stacks(false, HeroItemType.Shield),
                "the enemy's loot is series-scoped on the same terms");
            Assert.AreEqual(1f + 2 * HeroGrowth.DamagePerSword, HeroGrowth.DamageMult(true), 0.0001f,
                "the carried stacks must still be doing work, not just be counted");
        }

        [UnityTest]
        public IEnumerator Rematch_StartsAFreshSeriesWithNoCarriedLoot()
        {
            yield return BootArena();

            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(true, HeroItemType.Boots);
            Assert.AreEqual(1, HeroGrowth.Stacks(true, HeroItemType.Sword), "precondition");

            // Rematch deliberately abandons the series, so it must abandon the loot with it.
            yield return ReloadArena(GameManager.RequestRematch);

            Assert.AreEqual(0, HeroGrowth.Stacks(true, HeroItemType.Sword),
                "a rematch starts a new series at 0-0, so it cannot inherit the old one's loot");
            Assert.AreEqual(0, HeroGrowth.Stacks(true, HeroItemType.Boots));
            Assert.AreEqual(1f, HeroGrowth.DamageMult(true), 0.0001f);
        }

        [UnityTest]
        public IEnumerator Title_ClearsLootAlongWithTheSeries()
        {
            yield return BootArena();

            HeroGrowth.Grant(true, HeroItemType.Shield);
            Assert.AreEqual(1, HeroGrowth.Stacks(true, HeroItemType.Shield), "precondition");

            yield return ReloadArena(GameManager.RequestTitle);

            Assert.AreEqual(0, HeroGrowth.Stacks(true, HeroItemType.Shield),
                "returning to the title abandons the campaign, and the loot goes with it");
        }

        [UnityTest]
        public IEnumerator TwoConsecutiveGames_AccumulateInsteadOfResetting()
        {
            // The point of the feature, stated as behaviour rather than as a reset rule: three
            // games of one series should compound, which is what makes destroying a gimmick
            // worth anything beyond the current match.
            yield return BootArena();

            HeroGrowth.Grant(true, HeroItemType.Sword);          // game 1 loot

            yield return ReloadArena(GameManager.RequestNextGame);
            HeroGrowth.Grant(true, HeroItemType.Sword);          // game 2 loot

            yield return ReloadArena(GameManager.RequestNextGame);
            HeroGrowth.Grant(true, HeroItemType.Sword);          // game 3 loot

            Assert.AreEqual(3, HeroGrowth.Stacks(true, HeroItemType.Sword),
                "a best-of-three should compound across its games, not restart each one");
            Assert.AreEqual(1.45f, HeroGrowth.DamageMult(true), 0.0001f,
                "+45% by game three matches the snowball table in design/hero-growth-persistence.md");
        }
    }
}
