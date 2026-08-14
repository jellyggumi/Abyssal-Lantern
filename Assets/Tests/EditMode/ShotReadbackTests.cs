using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins post-action readback: the sentence a resolved turn produces, and the sampling rule
    /// for the arc it leaves behind (`design/visibility-spec-v2.md` §3).
    ///
    /// These exist because the readback is the answer to a user complaint — "적이 어떻게 쏘는지
    /// 안 보인다" — and an answer that silently degrades is worse than the complaint. A miss that
    /// reports nothing, or an arc that changes shape with the frame rate, would both look like
    /// the feature working.
    /// </summary>
    public class ShotReadbackTests
    {
        private static ShotReadback.Summary Enemy(int blocks, float core) => new ShotReadback.Summary
        {
            ByPlayer = false,
            Projectile = "화약통",
            BlocksDestroyed = blocks,
            CoreDamage = core,
        };

        [Test]
        public void Compose_NamesTheSideAndTheProjectile()
        {
            var line = ShotReadback.Compose(Enemy(3, 0f));
            StringAssert.StartsWith("적 화약통 →", line);
        }

        [Test]
        public void Compose_PlayerShotIsNamedAsFriendly()
        {
            var line = ShotReadback.Compose(new ShotReadback.Summary
            {
                ByPlayer = true,
                Projectile = "기사",
                BlocksDestroyed = 1,
            });
            StringAssert.StartsWith("아군 기사 →", line);
        }

        /// <summary>
        /// A miss is the shot the player most needs counted when re-aiming, and silence after one
        /// is indistinguishable from the readback being broken.
        /// </summary>
        [Test]
        public void Compose_ReportsAMissRatherThanGoingSilent()
        {
            var line = ShotReadback.Compose(Enemy(0, 0f));
            Assert.IsNotEmpty(line);
            StringAssert.Contains("빗나감", line);
        }

        [Test]
        public void Compose_ReportsBlocksAndCoreTogetherWhenBothLanded()
        {
            var line = ShotReadback.Compose(Enemy(3, 40f));
            StringAssert.Contains("성벽 3블록", line);
            StringAssert.Contains("코어 -40", line);
        }

        [Test]
        public void Compose_OmitsTheHalfThatDidNotHappen()
        {
            StringAssert.DoesNotContain("코어", ShotReadback.Compose(Enemy(2, 0f)));
            StringAssert.DoesNotContain("성벽", ShotReadback.Compose(Enemy(0, 25f)));
        }

        /// <summary>
        /// A field obstacle is not a wall, and the line must not say it is.
        ///
        /// This is the defect a live sweep caught: three shots hit a midfield field-tower, an enemy
        /// archer and bare ground, and every one was announced as "성벽 N블록 파괴"
        /// (`qa/aim-space-reachability.md` §0-C). The readback exists so the player can tell what
        /// their shot did; announcing a breach that never happened is worse than saying nothing,
        /// because the player aims the next shot at a hole that is not there.
        /// </summary>
        [Test]
        public void Compose_DoesNotCallAFieldObstacleAWall()
        {
            var line = ShotReadback.Compose(new ShotReadback.Summary
            {
                ByPlayer = true,
                Projectile = "기사",
                FieldPiecesDestroyed = 2,
            });

            StringAssert.DoesNotContain("성벽", line);
            StringAssert.Contains("야전 구조물 2", line);
        }

        /// <summary>
        /// Both categories in one shot are reported separately, wall first.
        ///
        /// A shot can clear a field tower on the way in and still take a wall block; the player
        /// needs to know both, and which is which, because only one of them is a hole to aim at.
        /// </summary>
        [Test]
        public void Compose_SeparatesWallsFromFieldPieces()
        {
            var line = ShotReadback.Compose(new ShotReadback.Summary
            {
                ByPlayer = true,
                Projectile = "기사",
                BlocksDestroyed = 3,
                FieldPiecesDestroyed = 1,
            });

            StringAssert.Contains("성벽 3블록", line);
            StringAssert.Contains("야전 구조물 1", line);
            Assert.Less(line.IndexOf("성벽"), line.IndexOf("야전"),
                "the wall comes first - it is what the next shot has to get through");
        }

        /// <summary>
        /// Destroying only field furniture still counts as hitting something.
        ///
        /// Otherwise a shot that killed the flying beast would be reported as a miss, which is the
        /// opposite error from the one being fixed.
        /// </summary>
        [Test]
        public void FieldOnlyHit_IsNotReportedAsAMiss()
        {
            var line = ShotReadback.Compose(new ShotReadback.Summary
            {
                ByPlayer = true,
                Projectile = "기사",
                FieldPiecesDestroyed = 1,
            });

            StringAssert.DoesNotContain("빗나감", line);
        }

        /// <summary>
        /// Siege damage is a product of multipliers, so the raw figure is routinely fractional.
        /// "-39.6" reads as precision the player cannot act on, and the gauge it describes moves
        /// in whole points anyway.
        /// </summary>
        [Test]
        public void Compose_RoundsCoreDamageToAWholePoint()
        {
            StringAssert.Contains("코어 -40", ShotReadback.Compose(Enemy(0, 39.6f)));
        }

        /// <summary>
        /// A projectile with no resolved display name must still produce a sentence. The failure
        /// this guards is specific: an earlier path leaked the raw asset name to the player
        /// ("EXPLOSIVEBARREL 준비", task #48) because a missing name was never handled.
        /// </summary>
        [Test]
        public void Compose_FallsBackToAGenericNounRatherThanEmptyText()
        {
            var line = ShotReadback.Compose(new ShotReadback.Summary { Projectile = null, BlocksDestroyed = 1 });
            StringAssert.Contains("발사체", line);
        }

        // ---- Trace sampling -----------------------------------------------------------------

        [Test]
        public void ShouldSample_TakesTheFirstPointUnconditionally()
        {
            Assert.IsTrue(ShotTracePath.ShouldSample(new List<Vector2>(), new Vector2(-14.5f, 3f)));
        }

        /// <summary>
        /// Distance-gated, never time-gated: a 30fps browser tab and a 120Hz display must draw
        /// the same arc, or two players comparing shots would be comparing their hardware.
        /// </summary>
        [Test]
        public void ShouldSample_RejectsPointsCloserThanTheSpacing()
        {
            var points = new List<Vector2> { Vector2.zero };
            var tooClose = new Vector2(ShotTracePath.MinSampleDistance * 0.5f, 0f);
            var farEnough = new Vector2(ShotTracePath.MinSampleDistance * 1.01f, 0f);

            Assert.IsFalse(ShotTracePath.ShouldSample(points, tooClose));
            Assert.IsTrue(ShotTracePath.ShouldSample(points, farEnough));
        }

        /// <summary>
        /// A projectile that never resolves is a real state here — <c>WaitAndEndTurn</c> carries a
        /// 12s watchdog for exactly that — and an unbounded line would grow for its whole duration.
        /// </summary>
        [Test]
        public void ShouldSample_StopsAtTheCap()
        {
            var points = new List<Vector2>(ShotTracePath.MaxSamples);
            for (int i = 0; i < ShotTracePath.MaxSamples; i++) points.Add(new Vector2(i, 0f));

            Assert.IsFalse(ShotTracePath.ShouldSample(points, new Vector2(9999f, 0f)));
        }

        /// <summary>A single point is a dot at the muzzle, which reads as a rendering fault
        /// rather than as a shot.</summary>
        [Test]
        public void IsDrawable_NeedsTwoPointsToDescribeADirection()
        {
            Assert.IsFalse(ShotTracePath.IsDrawable(null));
            Assert.IsFalse(ShotTracePath.IsDrawable(new List<Vector2>()));
            Assert.IsFalse(ShotTracePath.IsDrawable(new List<Vector2> { Vector2.zero }));
            Assert.IsTrue(ShotTracePath.IsDrawable(new List<Vector2> { Vector2.zero, Vector2.one }));
        }
    }

    /// <summary>
    /// Pins the accumulation window of <see cref="ShotTraceDirector"/> — the part that decides
    /// which damage belongs to which shot. Drawing needs a scene and is exercised in PlayMode;
    /// the bookkeeping does not, and it is where a misattribution would hide.
    /// </summary>
    public class ShotTraceDirectorTests
    {
        [SetUp]
        public void Reset() => ShotTraceDirector.ResetForNewMatch();

        [TearDown]
        public void Cleanup() => ShotTraceDirector.ResetForNewMatch();

        /// <summary>
        /// Damage outside a shot's window must not attach to the next shot. Garrison archers fire
        /// on their own schedule and a keep has detonated its own core between turns before
        /// (task #49) — either would otherwise be reported as the player's opening shot.
        /// </summary>
        [Test]
        public void DamageOutsideAShot_IsNotAttributedToTheNextOne()
        {
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.NoteCoreDamage(80f);

            ShotTraceDirector.BeginShot(true, "기사", Vector2.zero);
            ShotTraceDirector.Seal();

            StringAssert.Contains("빗나감", ShotTraceDirector.LatestLine);
        }

        [Test]
        public void SealedShot_ReportsTheDamageRecordedInsideItsWindow()
        {
            ShotTraceDirector.BeginShot(false, "화약통", new Vector2(14.5f, 3f));
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.NoteCoreDamage(30f);
            ShotTraceDirector.Seal();

            Assert.AreEqual("적 화약통 → 성벽 2블록 파괴 · 코어 -30", ShotTraceDirector.LatestLine);
            Assert.IsFalse(ShotTraceDirector.LatestLineByPlayer);
        }

        /// <summary>
        /// Each shot reports its own result. A tally that carried over would make every later
        /// shot look better than it was, and the numbers would drift upward all match.
        /// </summary>
        [Test]
        public void ConsecutiveShots_DoNotInheritEachOthersTally()
        {
            ShotTraceDirector.BeginShot(true, "기사", Vector2.zero);
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.Seal();

            ShotTraceDirector.BeginShot(false, "궁수", Vector2.zero);
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.Seal();

            StringAssert.Contains("성벽 1블록", ShotTraceDirector.LatestLine);
        }

        [Test]
        public void Sample_IgnoresPositionsWhenNoShotIsOpen()
        {
            ShotTraceDirector.Sample(new Vector2(1f, 1f));
            Assert.AreEqual(0, ShotTraceDirector.SampleCount);
        }

        [Test]
        public void Sample_RetainsTheMuzzleAndSpacedFlightPoints()
        {
            ShotTraceDirector.BeginShot(true, "기사", Vector2.zero);
            Assert.AreEqual(1, ShotTraceDirector.SampleCount, "the muzzle is the first point");

            ShotTraceDirector.Sample(new Vector2(0.01f, 0f)); // inside the spacing
            Assert.AreEqual(1, ShotTraceDirector.SampleCount);

            ShotTraceDirector.Sample(new Vector2(2f, 1f));
            Assert.AreEqual(2, ShotTraceDirector.SampleCount);
        }

        /// <summary>
        /// A shot interrupted mid-flight must not leak its samples into the next arc — the next
        /// shot would otherwise be drawn starting from the abandoned one's path. The interruption
        /// that actually happens is the stage/rematch scene reload.
        /// </summary>
        [Test]
        public void ResetMidFlight_DropsTheInFlightRecordWithoutPublishingALine()
        {
            ShotTraceDirector.BeginShot(true, "기사", Vector2.zero);
            ShotTraceDirector.Sample(new Vector2(3f, 2f));
            ShotTraceDirector.ResetForNewMatch();

            Assert.AreEqual(0, ShotTraceDirector.SampleCount);
            Assert.IsEmpty(ShotTraceDirector.LatestLine);

            // And the next shot starts from its own muzzle, not the abandoned path.
            ShotTraceDirector.BeginShot(false, "궁수", new Vector2(14.5f, 3f));
            Assert.AreEqual(1, ShotTraceDirector.SampleCount);
        }

        /// <summary>
        /// Sealing twice must not republish. <c>WaitAndEndTurn</c> can be re-entered by the
        /// watchdog path, and a second seal on an empty window would overwrite a real result
        /// with "빗나감".
        /// </summary>
        [Test]
        public void SealingTwice_KeepsTheFirstResult()
        {
            ShotTraceDirector.BeginShot(true, "기사", Vector2.zero);
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.Seal();
            var first = ShotTraceDirector.LatestLine;

            ShotTraceDirector.Seal();

            Assert.AreEqual(first, ShotTraceDirector.LatestLine);
        }

        /// <summary>
        /// A new match opens with a clean strip. The previous match's line describes a board that
        /// no longer exists, and the rematch loop reloads the scene rather than the domain.
        /// </summary>
        [Test]
        public void ResetForNewMatch_ClearsTheLastLine()
        {
            ShotTraceDirector.BeginShot(false, "화약통", Vector2.zero);
            ShotTraceDirector.NoteCoreDamage(40f);
            ShotTraceDirector.Seal();
            Assert.IsNotEmpty(ShotTraceDirector.LatestLine);

            ShotTraceDirector.ResetForNewMatch();

            Assert.IsEmpty(ShotTraceDirector.LatestLine);
        }
    }

    /// <summary>
    /// Pins the designation-window predicate that UX-003a turned on.
    ///
    /// The defect was not a wrong string, it was a string asserting a fact nobody checked: the
    /// HUD advertised "클릭: 벽돌 예약" on every enemy turn while the controller returned early
    /// and ate the click. Both surfaces now ask this predicate, so the only way they can disagree
    /// again is for one of them to stop asking.
    /// </summary>
    public class BrickDesignationWindowTests
    {
        [Test]
        public void OneShotTurns_CloseTheWindow()
        {
            Assert.IsFalse(BrickPlacementRules.DesignationOpen(
                enforcesOneShotTurns: true, isOpponentTurn: true, deployModeArmed: false),
                "the one-shot loop suspends placement verbs — this is the case the HUD was lying about");
        }

        [Test]
        public void OwnTurn_ClosesTheWindow()
        {
            Assert.IsFalse(BrickPlacementRules.DesignationOpen(false, isOpponentTurn: false, deployModeArmed: false));
        }

        /// <summary>Deploy owns the click while armed; both systems listen for the same button,
        /// and one click must never spend supply and a brick slot at once.</summary>
        [Test]
        public void ArmedDeployMode_ClosesTheWindow()
        {
            Assert.IsFalse(BrickPlacementRules.DesignationOpen(false, true, deployModeArmed: true));
        }

        [Test]
        public void OpponentTurn_WithoutOneShotOrDeploy_OpensTheWindow()
        {
            Assert.IsTrue(BrickPlacementRules.DesignationOpen(false, true, false));
        }
    }
}
