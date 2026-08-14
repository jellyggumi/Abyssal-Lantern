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

        /// <summary>
        /// Blocks rise while hit points stay flat, and that coexistence is the design — unwritten
        /// until now, which is how it nearly got "fixed" into a broken gate.
        ///
        /// I wrote a test here asserting wall hit points must rise across the unlock order, on the
        /// evidence that Stage3 carries 21 blocks against Stage2's 18 yet both demolish at 1470 HP.
        /// It was asserting a design position that had already been rejected with reasons:
        /// `KeepProfileTests.EveryStage_HoldsTheTargetMatchLength` measures ONE target for all three
        /// stages, because "a later stage must be a different fight, not a longer one" — escalation
        /// belongs to mutation cadence, obstacle cap, wind and AI aim, all of which do climb.
        ///
        /// Every material change that makes Stage3 genuinely heavier leaves that gate: W,W,S,I gives
        /// 1800 HP and 395s against a 240-360s band. So a rising HP ladder and a single time target
        /// cannot both hold, and the target is the one with a reason written down.
        ///
        /// What IS true: the fortress grows in extent (more blocks, more collapse surface, more to
        /// chew through spatially) at constant demolition cost. Both halves are pinned, because
        /// either alone reads as a defect and invites the wrong repair.
        /// Measured in qa/evidence/match-length/castle-material-census-by-role.md.
        /// </summary>
        [Test]
        public void TheKeepGrowsInExtentWhileStayingConstantInCost()
        {
            const float wood = 30f, stone = 85f, iron = 150f;

            var layouts = new[]
                { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 };

            int previousBlocks = 0;
            float firstHp = 0f;
            for (int i = 0; i < layouts.Length; i++)
            {
                int blocks = GameManager.BlocksPerKeep(layouts[i].wallHeightBlocks);
                float hp = GameManager.KeepWallHitPoints(layouts[i], wood, stone, iron);

                Assert.Greater(blocks, previousBlocks,
                    $"{layouts[i].displayName}: the keep must grow in extent across the unlock order");
                previousBlocks = blocks;

                if (i == 0) firstHp = hp;
                else
                {
                    // Within a quarter. Not equality: the profiles differ course by course, and
                    // demanding an exact match would forbid any material variation at all.
                    Assert.AreEqual(firstHp, hp, firstHp * 0.25f,
                        $"{layouts[i].displayName} demolishes at {hp:F0} HP against Stage1's "
                        + $"{firstHp:F0}. A later stage that costs materially more to breach breaks "
                        + "the single match-length target - see KeepProfileTests. If this is meant "
                        + "to change, the target becomes per-stage first, and this test says so");
                }
            }
        }

        /// <summary>
        /// The two figures that both call themselves "material" must agree.
        ///
        /// The difficulty ramp read blocks x stone HP while the pacing gate read the real course
        /// materials. They disagreed by 0.90x, 1.04x and 1.19x per stage, so the ramp length was
        /// derived from a keep that does not exist. Both now read KeepWallHitPoints.
        /// </summary>
        [Test]
        public void TheAllStoneApproximationIsNotTheKeep()
        {
            const float wood = 30f, stone = 85f, iron = 150f;

            foreach (var layout in new[]
                     { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                float real = GameManager.KeepWallHitPoints(layout, wood, stone, iron);
                float allStone = GameManager.BlocksPerKeep(layout.wallHeightBlocks) * stone;

                Assert.AreNotEqual(allStone, real,
                    $"{layout.displayName}: if these ever match, either the stage went all-stone or "
                    + "somebody made the approximation the source of truth again");
            }
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
