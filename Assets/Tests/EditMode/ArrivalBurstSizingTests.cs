using NUnit.Framework;
using UnityEngine;
using CastleBusters;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The reported defect was "the stone at the drop is too big". It was not a missing asset or a
    /// wrong sprite: fx_spawn was spawned at a hand-written world size that nobody had compared
    /// against the object it annotates, and one call site drifted to 1.8x its own subject.
    ///
    /// These assert the RATIO, which is the quantity the complaint is about. Asserting the burst
    /// size alone would pass just as happily with a 3-unit burst on a 3-unit brick.
    /// </summary>
    public class ArrivalBurstSizingTests
    {
        private const float BlockWorldSize = 1.0f;   // CastleController.blockSizeX/Y
        private const float VentWorldSize = 2.4f;    // EruptionVentGimmick.targetWorldSize
        private const float ShippedVentBurst = 2.1f; // DynamicBattlefield fx_spawn call sites

        [Test]
        public void ArrivalBurst_NeverOutsizesItsSubjectByMoreThanASkirt()
        {
            float burst = FrameAnimEffect.ArrivalBurstSizeFor(BlockWorldSize, 1.15f);
            float ratio = burst / BlockWorldSize;

            Assert.Greater(ratio, 1.0f,
                "a burst tucked entirely inside the object cannot read as dust leaving it");
            Assert.LessOrEqual(ratio, 1.3f,
                $"the burst annotates the object and must stay subordinate to it; got {ratio:0.00}x. "
                + "The shipped 1.8 on a 1.00-unit brick is the defect this bounds.");
        }

        [Test]
        public void TheBrickBurst_IsNoLongerAnOutlierAgainstItsSiblings()
        {
            // The field-piece call sites were never reported as wrong, so they carry the intended
            // relationship and are the yardstick. Before the fix the brick sat at 1.80/1.00 = 1.80x
            // against their 2.10/2.40 = 0.875x - a 2.06x disagreement between two uses of one effect.
            float ventRatio = ShippedVentBurst / VentWorldSize;
            float brickRatio = FrameAnimEffect.ArrivalBurstSizeFor(BlockWorldSize, 1.15f) / BlockWorldSize;

            float disagreement = Mathf.Max(brickRatio, ventRatio) / Mathf.Min(brickRatio, ventRatio);
            Assert.Less(disagreement, 1.5f,
                $"one effect should not be sized twice as generously in one place as another; "
                + $"brick {brickRatio:0.000}x vs vent {ventRatio:0.000}x = {disagreement:0.00}x apart");
        }

        [Test]
        public void AnUnmeasurableSubject_FallsBackInsteadOfCollapsingToZero()
        {
            // A burst spawned before its subject has a sprite must not become a 0-unit invisible
            // effect - soft-failing to nothing is how an effect silently stops existing.
            Assert.AreEqual(1.15f, FrameAnimEffect.ArrivalBurstSizeFor(0f, 1.15f), 1e-4f);
            Assert.AreEqual(1.3f, FrameAnimEffect.ArrivalBurstSizeFor(-3f, 1.3f), 1e-4f);
        }

        [Test]
        public void TheRatioScalesWithTheSubject_SoRescaledArtCannotStrandIt()
        {
            // The whole point of measuring off the object: doubling the object doubles the burst.
            float small = FrameAnimEffect.ArrivalBurstSizeFor(1.0f, 1.15f);
            float large = FrameAnimEffect.ArrivalBurstSizeFor(2.0f, 1.15f);
            Assert.AreEqual(2.0f, large / small, 1e-4f,
                "a hand-written constant is exactly what fails this");
        }
    }
}
