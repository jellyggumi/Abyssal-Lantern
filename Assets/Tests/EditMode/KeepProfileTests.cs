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

        static float LoadedHp(string resource)
        {
            var data = Resources.Load<BlockData>(resource);
            Assert.IsNotNull(data, $"wall blocks take their health from {resource}");
            return data.maxHP;
        }

        static float WallHitPoints(StageLayout layout) => GameManager.KeepWallHitPoints(
            layout, LoadedHp("WoodBlockData"), LoadedHp("StoneBlockData"), LoadedHp("IronBlockData"));

        [Test]
        public void Keep_IsSizedForTheTargetMatchLength()
        {
            // This replaced a flat cap of eight blocks. That cap came from the thirty-game
            // harness failing to finish, but the harness fires at random velocities — its
            // trouble reaching a screened core said nothing about a player who aims. Sizing the
            // keep against a target match length is the thing actually worth defending, and it
            // fails loudly in both directions: too thin ends matches early, too thick grinds.
            // Measured against the REAL per-course materials (wood outpost, stone walls, iron
            // inner) rather than pretending every course is stone.
            float material = WallHitPoints(StageDefinitions.Stage1) + CastleCoreGimmick.CoreMaxHP;
            float seconds = MatchLengthModel.SecondsToDecide(
                material,
                MatchLengthModel.EffectiveDamagePerTurn,
                MatchLengthModel.AverageTurnSeconds);
            float tolerance = MatchLengthModel.TargetMatchSeconds * MatchLengthModel.ToleranceFraction;

            Assert.That(seconds,
                Is.EqualTo(MatchLengthModel.TargetMatchSeconds).Within(tolerance),
                $"a decided match models at {seconds:F0}s against a {MatchLengthModel.TargetMatchSeconds:F0}s target");
        }

        [Test]
        public void KeepMaterials_EveryStageProfilesEveryCourse()
        {
            foreach (var layout in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                Assert.IsNotNull(layout.keepCourseMaterials,
                    $"{layout.displayName} must declare a keep material profile");
                Assert.AreEqual(GameManager.KeepProfile.Length, layout.keepCourseMaterials.Length,
                    $"{layout.displayName} must assign a material to every keep course");
            }
        }

        [Test]
        public void KeepMaterials_DurabilityClimbsAcrossTheCampaign()
        {
            // 1435 → 1690 → 1965: each unlock should present a genuinely tougher fortress,
            // through materials and height together, never a step down.
            float s1 = WallHitPoints(StageDefinitions.Stage1);
            float s2 = WallHitPoints(StageDefinitions.Stage2);
            float s3 = WallHitPoints(StageDefinitions.Stage3);

            Assert.Less(s1, s2, "Stage2's bastion must out-endure Stage1's plains keep");
            Assert.Less(s2, s3, "Stage3's citadel must out-endure Stage2's bastion");
        }

        [Test]
        public void KeepMaterials_StayNearTheAllStoneBaseline()
        {
            // Materials are level design, not a stealth balance patch: each stage's mixed
            // profile must stay within −10%/+25% of what the same courses would total in
            // plain stone, so pacing shifts stay a deliberate, reviewed decision.
            float stoneHp = LoadedHp("StoneBlockData");
            foreach (var layout in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                float mixed = WallHitPoints(layout);
                float allStone = GameManager.BlocksPerKeep(layout.wallHeightBlocks) * stoneHp;
                Assert.That(mixed / allStone, Is.InRange(0.9f, 1.25f),
                    $"{layout.displayName}: mixed-material walls ({mixed}) drift too far from the all-stone baseline ({allStone})");
            }
        }
    }
}
