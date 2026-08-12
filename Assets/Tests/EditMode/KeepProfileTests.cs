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
        public void EveryStage_HoldsTheTargetMatchLength()
        {
            // This replaced a "durability climbs across the campaign" pin, which was the
            // wrong property to defend and hid a real defect: heights climb 3→4→5, so a
            // later keep carries 18 and 21 blocks against Stage1's 15. Stacking a material
            // ladder on top of that made Stage2 model at 373s and Stage3 at 429s — a
            // seven-minute grind — while the pacing gate only ever measured Stage1 and
            // stayed green. A later stage must be a different fight, not a longer one:
            // escalation belongs to the field mutation cadence, obstacle cap, wind, and AI
            // aim, all of which already climb. Every stage is measured here.
            float tolerance = MatchLengthModel.TargetMatchSeconds * MatchLengthModel.ToleranceFraction;

            foreach (var layout in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                float material = WallHitPoints(layout) + CastleCoreGimmick.CoreMaxHP;
                float seconds = MatchLengthModel.SecondsToDecide(
                    material,
                    MatchLengthModel.EffectiveDamagePerTurn,
                    MatchLengthModel.AverageTurnSeconds);

                Assert.That(seconds,
                    Is.EqualTo(MatchLengthModel.TargetMatchSeconds).Within(tolerance),
                    $"{layout.displayName} models at {seconds:F0}s against a {MatchLengthModel.TargetMatchSeconds:F0}s target");
            }
        }

        [Test]
        public void TallerStages_UseLighterMaterialSoSizeIsNotDuration()
        {
            // The design consequence of the rule above, pinned so a future "make the last
            // stage tougher" edit cannot quietly reintroduce the seven-minute match: as the
            // keep grows in blocks, its average block must get cheaper to break.
            float s1 = WallHitPoints(StageDefinitions.Stage1) / GameManager.BlocksPerKeep(StageDefinitions.Stage1.wallHeightBlocks);
            float s2 = WallHitPoints(StageDefinitions.Stage2) / GameManager.BlocksPerKeep(StageDefinitions.Stage2.wallHeightBlocks);
            float s3 = WallHitPoints(StageDefinitions.Stage3) / GameManager.BlocksPerKeep(StageDefinitions.Stage3.wallHeightBlocks);

            Assert.Greater(s1, s2, "Stage2 is the bigger keep, so its average block must be softer than Stage1's");
            Assert.Greater(s2, s3, "Stage3 is bigger still, so its average block must be softer than Stage2's");
        }

        [Test]
        public void KeepMaterials_StayNearTheAllStoneBaseline()
        {
            // Materials are level design, not a stealth balance patch: each stage's mixed
            // profile must stay within −20%/+25% of what the same courses would total in
            // plain stone, so pacing shifts stay a deliberate, reviewed decision. The lower
            // bound is what the taller keeps spend to hold the five-minute target.
            float stoneHp = LoadedHp("StoneBlockData");
            foreach (var layout in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                float mixed = WallHitPoints(layout);
                float allStone = GameManager.BlocksPerKeep(layout.wallHeightBlocks) * stoneHp;
                Assert.That(mixed / allStone, Is.InRange(0.8f, 1.25f),
                    $"{layout.displayName}: mixed-material walls ({mixed}) drift too far from the all-stone baseline ({allStone})");
            }
        }
    }
}
