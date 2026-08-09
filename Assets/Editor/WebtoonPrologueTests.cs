using NUnit.Framework;
using System;
using UnityEngine;

namespace CastleBusters.Tests
{
    public class WebtoonPrologueTests
    {
        [Test]
        public void PixelSnap_RoundsToConfiguredGrid()
        {
            float snapped = WebtoonPrologueController.PixelSnap(1.02f, 32f);
            Assert.AreEqual(1.03125f, snapped, 0.0001f);

            snapped = WebtoonPrologueController.PixelSnap(1.048f, 32f);
            Assert.AreEqual(1.0625f, snapped, 0.0001f);
        }


        [Test]
        public void SlideProgressAt_StaysInHold_ThenCompletesTransition()
        {
            Assert.AreEqual(0f,
                WebtoonPrologueController.SlideProgressAt(1.2f,
                    WebtoonPrologueController.DefaultHoldSeconds,
                    WebtoonPrologueController.DefaultTransitionSeconds),
                0.0001f);

            Assert.AreEqual(1f,
                WebtoonPrologueController.SlideProgressAt(
                    WebtoonPrologueController.DefaultHoldSeconds + WebtoonPrologueController.DefaultTransitionSeconds,
                    WebtoonPrologueController.DefaultHoldSeconds,
                    WebtoonPrologueController.DefaultTransitionSeconds),
                0.0001f);
        }

        [Test]
        public void StripPageOffsetAt_AdvancesOnePagePerBlock_AndClampsOnLastPage()
        {
            float block = WebtoonPrologueController.DefaultHoldSeconds + WebtoonPrologueController.DefaultTransitionSeconds;

            Assert.AreEqual(0f, WebtoonPrologueController.StripPageOffsetAt(0f), 0.0001f);
            Assert.AreEqual(1f, WebtoonPrologueController.StripPageOffsetAt(block), 0.0001f);
            Assert.AreEqual(10f, WebtoonPrologueController.StripPageOffsetAt(block * 20f), 0.0001f,
                "11 pages should clamp the strip to the last page index (10)");
        }

        [Test]
        public void KnightPrologueIdleResources_LoadsFourOrderedFrames()
        {
            const string resourcePath = "GeneratedUnitFrames/KnightPrologue/Idle";
            Sprite[] frames = Resources.LoadAll<Sprite>(resourcePath);

            Assert.AreEqual(4, frames.Length, $"Expected exactly four sprites at Resources/{resourcePath}.");
            foreach (Sprite frame in frames)
            {
                Assert.IsNotNull(frame, $"Resources/{resourcePath} must not contain null frames.");
            }

            Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name));

            string[] expectedNames = { "idle_000", "idle_001", "idle_002", "idle_003" };
            for (int index = 0; index < expectedNames.Length; index++)
            {
                Assert.AreEqual(expectedNames[index], frames[index].name,
                    $"Unexpected frame at sorted index {index} from Resources/{resourcePath}.");
            }
        }
    }
}
