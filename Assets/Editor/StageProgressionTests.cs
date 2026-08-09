using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// EditMode pins for the sequential-campaign unlock contract (StageProgression.cs).
    /// Pure logic only (StageProgress) — StageProgressStore's PlayerPrefs I/O follows
    /// the same untested-at-unit-level precedent as LeaderboardStore (SiegeEcosystem.cs);
    /// its behavior is exercised live in PlayMode instead.
    /// </summary>
    public class StageProgressionTests
    {
        [Test]
        public void Stage1_IsAlwaysUnlocked_RegardlessOfFrontier()
        {
            Assert.IsTrue(StageProgress.IsUnlocked(StageId.Stage1, StageId.Stage1));
        }

        [Test]
        public void IsUnlocked_TrueForStagesAtOrBeforeFrontier_FalseAfter()
        {
            Assert.IsTrue(StageProgress.IsUnlocked(StageId.Stage2, StageId.Stage1), "earlier stage stays unlocked");
            Assert.IsTrue(StageProgress.IsUnlocked(StageId.Stage2, StageId.Stage2), "frontier stage itself is unlocked");
            Assert.IsFalse(StageProgress.IsUnlocked(StageId.Stage2, StageId.Stage3), "stage past the frontier is locked");
        }

        [Test]
        public void NextStage_ReturnsImmediateSuccessor()
        {
            Assert.AreEqual(StageId.Stage2, StageProgress.NextStage(StageId.Stage1));
            Assert.AreEqual(StageId.Stage3, StageProgress.NextStage(StageId.Stage2));
        }

        [Test]
        public void NextStage_IsNullPastTheFinalStage()
        {
            Assert.IsNull(StageProgress.NextStage(StageId.Stage3));
        }

        [Test]
        public void Advance_ClearingStage1_UnlocksStage2()
        {
            var result = StageProgress.Advance(StageId.Stage1, StageId.Stage1);
            Assert.AreEqual(StageId.Stage2, result);
        }

        [Test]
        public void Advance_ClearingStage2_UnlocksStage3()
        {
            var result = StageProgress.Advance(StageId.Stage2, StageId.Stage2);
            Assert.AreEqual(StageId.Stage3, result);
        }

        [Test]
        public void Advance_ClearingFinalStage_StaysClampedAtFinalStage()
        {
            var result = StageProgress.Advance(StageId.Stage3, StageId.Stage3);
            Assert.AreEqual(StageId.Stage3, result);
        }

        [Test]
        public void Advance_ReplayingAnAlreadyClearedEarlierStage_NeverRegressesFrontier()
        {
            // Frontier is already at Stage3 (player unlocked everything); rematching/
            // replaying Stage1 must not roll the frontier backward.
            var result = StageProgress.Advance(StageId.Stage3, StageId.Stage1);
            Assert.AreEqual(StageId.Stage3, result, "clearing an earlier stage again must not regress an already-advanced frontier");
        }

        [Test]
        public void Advance_ClearingTheFrontierStageAgain_IsIdempotent()
        {
            var result = StageProgress.Advance(StageId.Stage2, StageId.Stage1);
            // Frontier is Stage2 (Stage1 was already cleared once); clearing Stage1 again
            // must not move the frontier past what it already earned from a different run.
            Assert.AreEqual(StageId.Stage2, result);
        }
    }
}
