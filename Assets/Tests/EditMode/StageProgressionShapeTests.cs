using CastleBusters;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the campaign's shape across the three sequentially-unlocked stages.
    ///
    /// The stages are tactical variations, not a numbers ladder, so most axes are
    /// deliberately non-monotonic and stay that way: wind is derived from throw distance
    /// (Stage2 is short and calm, Stage3 is long and gusty), and obstacle cap plus mutation
    /// cadence carry each board's pacing identity. Fortress height is the one axis that is
    /// pure progression, and it is pinned rising here — Stage3 used to inherit Stage1's
    /// 2-block wall, which left the final stage with the softest fortress in the game.
    /// </summary>
    public class StageProgressionShapeTests
    {
        [Test]
        public void WallHeight_RisesAcrossTheUnlockOrder()
        {
            Assert.Less(StageDefinitions.Stage1.wallHeightBlocks, StageDefinitions.Stage2.wallHeightBlocks,
                "Stage2 must be a heavier fortress than the Stage1 baseline.");
            Assert.Less(StageDefinitions.Stage2.wallHeightBlocks, StageDefinitions.Stage3.wallHeightBlocks,
                "Stage3 is the last unlock; it must not be softer to breach than Stage2.");
        }

        [Test]
        public void WindCap_StaysTiedToThrowDistanceNotUnlockOrder()
        {
            // Guards the redistribution from being "fixed" into a monotonic ladder later:
            // flight time scales with distance, so wind pressure must too.
            Assert.Less(StageDefinitions.Stage2.launchApronAbsX, StageDefinitions.Stage1.launchApronAbsX);
            Assert.Less(StageDefinitions.Stage2.windCapEnd, StageDefinitions.Stage1.windCapEnd,
                "The short board keeps the calmer air that makes precision, not compensation, the skill.");

            Assert.Greater(StageDefinitions.Stage3.launchApronAbsX, StageDefinitions.Stage1.launchApronAbsX);
            Assert.Greater(StageDefinitions.Stage3.windCapEnd, StageDefinitions.Stage1.windCapEnd,
                "The long board needs more wind for late-match aiming to stay meaningful.");
        }

        [Test]
        public void PacingStaysDistinctPerStage()
        {
            // Identity, not difficulty: dense-and-fast (Stage2) versus open-and-slow (Stage3).
            Assert.Less(StageDefinitions.Stage2.mutateEveryNTurns, StageDefinitions.Stage1.mutateEveryNTurns,
                "Stage2 cycles its field faster than the baseline.");
            Assert.Greater(StageDefinitions.Stage3.mutateEveryNTurns, StageDefinitions.Stage1.mutateEveryNTurns,
                "Stage3 breathes slower than the baseline.");
            Assert.Less(StageDefinitions.Stage2.maxFieldObstacles, StageDefinitions.Stage3.maxFieldObstacles,
                "The tight board stays leaner than the wide one.");
        }

        [Test]
        public void EveryStageKeepsItsOwnBackdrop()
        {
            Assert.AreNotEqual(StageDefinitions.Stage1.backgroundTint, StageDefinitions.Stage2.backgroundTint);
            Assert.AreNotEqual(StageDefinitions.Stage1.backgroundTint, StageDefinitions.Stage3.backgroundTint);
            Assert.AreNotEqual(StageDefinitions.Stage2.backgroundTint, StageDefinitions.Stage3.backgroundTint);
        }
    }
}
