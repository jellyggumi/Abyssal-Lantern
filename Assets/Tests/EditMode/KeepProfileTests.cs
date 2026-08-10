using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the keep's shape. A castle used to be one 1-block column plus the core — thinner
    /// than the slingshot battering it — so these guard the two properties that made the
    /// enlargement worth doing: it is actually deep, and it still fits the board.
    /// </summary>
    public class KeepProfileTests
    {
        /// <summary>Core centre ±9 with a 2.3u core spans 7.85–10.15.</summary>
        const float CoreNearEdgeAbsX = 7.85f;

        /// <summary>Wall height of the opening stage — the one the playtest harness runs.</summary>
        static int BaselineWallHeightBlocks => StageDefinitions.Stage1.wallHeightBlocks;

        [Test]
        public void Profile_HasAnOutpostAheadOfEveryWallCourse()
        {
            var outpost = System.Array.Find(GameManager.KeepProfile, c => c.IsOutpost);
            Assert.Greater(outpost.AbsX, 0f, "the keep must have a forward outwork");

            foreach (var course in GameManager.KeepProfile)
            {
                if (course.IsOutpost) continue;
                Assert.Less(outpost.AbsX, course.AbsX,
                    "the outpost stands between the field and the walls, so it falls first");
            }
        }

        [Test]
        public void Profile_StepsUpTowardTheCore()
        {
            // The silhouette should rise as it approaches the core; a flat run of columns
            // would read as a fence rather than a fortress.
            bool havePrevious = false;
            int previous = 0;
            foreach (var course in GameManager.KeepProfile)
            {
                if (course.IsOutpost) continue;
                if (havePrevious)
                {
                    Assert.GreaterOrEqual(course.HeightOffset, previous,
                        "wall courses must not step down as they approach the core");
                }
                previous = course.HeightOffset;
                havePrevious = true;
            }
            Assert.IsTrue(havePrevious, "the keep must have at least one wall course");
        }

        [Test]
        public void Profile_NeverOverlapsTheCore()
        {
            foreach (var course in GameManager.KeepProfile)
            {
                // Blocks are 1u wide and centred on AbsX, so the inner face is AbsX + 0.5.
                Assert.LessOrEqual(course.AbsX + 0.5f, CoreNearEdgeAbsX + 0.01f,
                    $"course at {course.AbsX} would be built inside the core");
            }
        }

        [Test]
        public void Profile_ClearsBothLaunchRings()
        {
            foreach (var pos in GameManager.WallBasePositions)
            {
                Assert.IsFalse(LaunchRingRules.IsInsideRing(pos),
                    $"keep block at {pos} would be rejected at spawn for sitting in a ring");
            }
        }

        [Test]
        public void Profile_IsMirroredAcrossTheField()
        {
            foreach (var course in GameManager.KeepProfile)
            {
                Assert.IsTrue(System.Array.Exists(GameManager.WallBasePositions,
                        p => Mathf.Approximately(p.x, -course.AbsX)),
                    "every course must exist on the player side");
                Assert.IsTrue(System.Array.Exists(GameManager.WallBasePositions,
                        p => Mathf.Approximately(p.x, course.AbsX)),
                    "…and on the enemy side, or the match is not symmetric");
            }
        }

        [Test]
        public void Profile_IsSubstantiallyDeeperThanTheOldSingleColumn()
        {
            // Measured in blocks, not courses: courses are how the keep is authored, blocks
            // are what the attacker actually has to knock down.
            Assert.GreaterOrEqual(GameManager.BlocksPerKeep(BaselineWallHeightBlocks), 6,
                "a keep that is one column deep is what this replaced");
        }

        [Test]
        public void Profile_StaysThinEnoughForAMatchToEnd()
        {
            // The guard this file was missing. A four-course keep (11 blocks a side at the
            // baseline stage) screened the core so completely that a match no longer reached
            // GameOver inside the twenty turns the playtest harness allows, which is also the
            // point at which a real match stops being winnable and starts being a chore.
            // Anyone deepening the keep again has to re-measure rather than re-discover this.
            Assert.LessOrEqual(GameManager.BlocksPerKeep(BaselineWallHeightBlocks), 8,
                "the keep is deep enough again to screen the core out of reach");
        }
    }
}
