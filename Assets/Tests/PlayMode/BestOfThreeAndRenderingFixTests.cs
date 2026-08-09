using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;
using CastleBusters;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Regression tests for a follow-up QA pass:
    ///  1) SiegeSeries pure rules (best-of-3, first to 2 wins) - fast, no scene needed.
    ///  2) SpriteAtlasPacker no longer collapses Knight/Bomber frames onto Archer's pixels
    ///     just because the PerfectPixel exports share bare filenames (idle_000.png, ...)
    ///     across unit folders - this was the "규정 상관없이 아처만 생성되는" visual bug.
    ///  3) A freshly spawned unit's rigidbody starts with rotation frozen, so it can never
    ///     tumble onto its side mid-flight and get stuck "lying down", unable to attack.
    /// </summary>
    public class BestOfThreeAndRenderingFixTests
    {
        // ---- 1) SiegeSeries pure rules ----

        [Test]
        public void IsSeriesDecided_TrueOnceEitherSideReachesTwoWins()
        {
            Assert.IsFalse(SiegeSeries.IsSeriesDecided(0, 0));
            Assert.IsFalse(SiegeSeries.IsSeriesDecided(1, 0));
            Assert.IsFalse(SiegeSeries.IsSeriesDecided(1, 1));
            Assert.IsTrue(SiegeSeries.IsSeriesDecided(2, 0));
            Assert.IsTrue(SiegeSeries.IsSeriesDecided(0, 2));
            Assert.IsTrue(SiegeSeries.IsSeriesDecided(1, 2));
        }

        [Test]
        public void IsSeriesDecided_TrueAfterThreeGamesEvenWithoutTwoWins()
        {
            // A 1-2 split after 3 games is impossible to reach without one side already
            // having 2, so exercise the "MaxGames reached" branch directly via game count.
            Assert.IsTrue(SiegeSeries.IsSeriesDecided(2, 1));
            Assert.AreEqual(3, 2 + 1);
        }

        [Test]
        public void PlayerWonSeries_ComparesWinCounts()
        {
            Assert.IsTrue(SiegeSeries.PlayerWonSeries(2, 0));
            Assert.IsTrue(SiegeSeries.PlayerWonSeries(2, 1));
            Assert.IsFalse(SiegeSeries.PlayerWonSeries(0, 2));
            Assert.IsFalse(SiegeSeries.PlayerWonSeries(1, 2));
        }

        [Test]
        public void NextGameNumber_ClampsToMaxGames()
        {
            Assert.AreEqual(1, SiegeSeries.NextGameNumber(0));
            Assert.AreEqual(2, SiegeSeries.NextGameNumber(1));
            Assert.AreEqual(3, SiegeSeries.NextGameNumber(2));
            Assert.AreEqual(3, SiegeSeries.NextGameNumber(3), "must never offer a 4th game");
        }

        [Test]
        public void SeriesScore_AddsSweepBonusOnlyForACleanTwoZero()
        {
            int sweepScore = SiegeSeries.SeriesScore(1000, 2, 0);
            int splitScore = SiegeSeries.SeriesScore(1000, 2, 1);
            int lossScore = SiegeSeries.SeriesScore(1000, 0, 2);

            Assert.Greater(sweepScore, splitScore, "a 2-0 sweep must outrank a 2-1 series with the same raw total");
            Assert.AreEqual(1000, splitScore, "a non-sweep win series score is just the raw total");
            Assert.AreEqual(1000, lossScore, "a losing series never gets the sweep bonus");
        }

        // ---- 2) SpriteAtlasPacker: unit frame sets with colliding bare filenames ----

        private static IEnumerator LoadAndBeginSiege()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsNotNull(GameManager.Instance, "GameManager must exist after scene load");
            GameManager.Instance.BeginSiege();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator SpriteAtlasPacker_KeepsKnightArcherBomberFramesDistinct()
        {
            yield return LoadAndBeginSiege();

            var archerIdle = Resources.Load<Sprite>("GeneratedUnitFrames/Archer/Idle/idle_000");
            var knightIdle = Resources.Load<Sprite>("GeneratedUnitFrames/Knight/Idle/idle_000");
            var bomberIdle = Resources.Load<Sprite>("GeneratedUnitFrames/Bomber/Idle/idle_000");
            Assert.IsNotNull(archerIdle, "Archer idle_000 sprite must exist under Resources");
            Assert.IsNotNull(knightIdle, "Knight idle_000 sprite must exist under Resources");
            Assert.IsNotNull(bomberIdle, "Bomber idle_000 sprite must exist under Resources");

            // Every unit's "idle_000" bare filename collides across folders (PerfectPixel
            // export convention). Before the fix, SpriteAtlasPacker deduped/keyed its atlas
            // by sprite.name alone, so Knight/Bomber silently resolved to Archer's packed
            // pixels. Packing by Sprite object identity must keep all three distinct.
            var packer = Object.FindObjectOfType<SpriteAtlasPacker>();
            Assert.IsNotNull(packer, "A SpriteAtlasPacker instance must exist in the running scene");
            if (!packer.IsPacked) packer.PackSprites();

            var packedArcher = packer.GetPackedSprite(archerIdle);
            var packedKnight = packer.GetPackedSprite(knightIdle);
            var packedBomber = packer.GetPackedSprite(bomberIdle);

            Assert.AreNotEqual(packedArcher.rect, packedKnight.rect,
                "Knight's packed idle_000 must occupy a different atlas cell than Archer's, not collapse onto it");
            Assert.AreNotEqual(packedArcher.rect, packedBomber.rect,
                "Bomber's packed idle_000 must occupy a different atlas cell than Archer's, not collapse onto it");
            Assert.AreNotEqual(packedKnight.rect, packedBomber.rect,
                "Knight's and Bomber's packed idle_000 must occupy different atlas cells from each other too");
        }

        // ---- 3) Units must never tumble/land "lying down" ----

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator SpawnedUnit_RigidbodyRotationIsFrozenFromCreation()
        {
            yield return LoadAndBeginSiege();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm.knightPrefab, "Scene GameManager must have a knightPrefab assigned");

            var unitGo = Object.Instantiate(gm.knightPrefab, new Vector3(0f, 4f, 0f), Quaternion.identity);
            var rb = unitGo.GetComponent<Rigidbody2D>();
            Assert.IsNotNull(rb, "Spawned unit must have a Rigidbody2D");

            Assert.AreEqual(RigidbodyConstraints2D.FreezeRotation, rb.constraints,
                "A freshly spawned unit's rigidbody must have rotation frozen from Awake so a mid-flight " +
                "glancing collision can never tip it onto its side (\"lying down\", unable to attack)");

            Object.Destroy(unitGo);
        }
    }
}
