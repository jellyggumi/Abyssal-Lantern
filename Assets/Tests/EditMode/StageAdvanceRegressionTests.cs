using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the campaign-advance path that once stranded the player on the results screen
    /// ("다음 스테이지" did nothing). GameManager.RequestStage used to be void and to bail out
    /// early whenever the requested stage already matched PendingStage; every caller latched
    /// itself as "already navigated" BEFORE calling it, so a silent refusal froze the player
    /// where they stood. RequestStage now returns whether it accepted the request, and the
    /// callers only latch on acceptance — but that only helps if the store underneath it
    /// actually reports the stage the player just earned. These tests pin that store.
    ///
    /// Scope: StageProgressStore (PlayerPrefs I/O + the session mirror) plus the two gates
    /// RequestStage consults. The pure StageProgress rules (IsUnlocked / NextStage / Advance)
    /// are already pinned in Assets/Editor/StageProgressionTests.cs and are not repeated here;
    /// they appear below only where the store's result and a gate form one contract.
    /// GameManager / ResultsScreenController / IntroScreenController are deliberately never
    /// constructed — they reach into scene singletons and SceneManager.LoadScene.
    ///
    /// WARNING: unlike the rest of the EditMode suite, this fixture writes the REAL
    /// PlayerPrefs key the shipped game uses. SetUp snapshots the developer's actual campaign
    /// progress and TearDown restores it exactly (including restoring "no saved profile at
    /// all"), and both ends clear the static session mirror so no state leaks between tests
    /// or into the editor session. Do not add a test here that skips that discipline.
    /// </summary>
    [TestFixture]
    public sealed class StageAdvanceRegressionTests
    {
        private const string PrefsKey = "CastleBusters.StageProgress.v1";

        /// <summary>Sentinel for "the developer had no saved profile". Not a valid StageId, so
        /// it cannot collide with real progress; a literal -1 already on disk is corrupt data
        /// that Load() clamps to the opening stage exactly like an absent key, so restoring it
        /// as absent loses nothing.</summary>
        private const int NoStoredProfile = -1;

        private int savedProfile;

        [SetUp]
        public void SetUp()
        {
            savedProfile = PlayerPrefs.GetInt(PrefsKey, NoStoredProfile);
            PlayerPrefs.DeleteKey(PrefsKey);
            StageProgressStore.ResetSessionMirror();
        }

        [TearDown]
        public void TearDown()
        {
            if (savedProfile == NoStoredProfile) PlayerPrefs.DeleteKey(PrefsKey);
            else PlayerPrefs.SetInt(PrefsKey, savedProfile);
            PlayerPrefs.Save();
            StageProgressStore.ResetSessionMirror();
        }

        /// <summary>Walks the campaign the way a player does — clearing every stage from the
        /// first through <paramref name="through"/> in order — and returns the frontier the
        /// last clear reported to the results screen.</summary>
        private static StageId ClearCampaignThrough(StageId through)
        {
            var frontier = StageProgressStore.Load();
            for (int i = (int)StageId.Stage1; i <= (int)through; i++)
                frontier = StageProgressStore.RecordVictory((StageId)i);
            return frontier;
        }

        // 1 -----------------------------------------------------------------------------

        [Test]
        public void FreshProfile_OpensTheFirstStageAndNothingElse()
        {
            var frontier = StageProgressStore.Load();

            Assert.AreEqual(StageId.Stage1, frontier,
                "a player who has never won a match must boot into the opening stage, not somewhere mid-campaign");
            Assert.IsFalse(StageProgress.IsUnlocked(frontier, StageId.Stage2),
                "the campaign gate must stay shut until something is actually cleared, or the picker hands out stages nobody earned");
        }

        // 2 -----------------------------------------------------------------------------

        [TestCase(StageId.Stage1, StageId.Stage2)]
        [TestCase(StageId.Stage2, StageId.Stage3)]
        public void ClearingAStage_ReportsTheNextOneAndKeepsItAcrossTheReload(StageId cleared, StageId expectedFrontier)
        {
            var reported = ClearCampaignThrough(cleared);

            Assert.AreEqual(expectedFrontier, reported,
                "the results screen picks what to offer from this value; a stale frontier here aims the NEXT STAGE button at the match the player just finished");

            // Drop the in-session mirror so the confirming read has to come off disk. Without
            // this the mirror would answer from memory and a Save() that never persisted
            // anything would still slip through green.
            StageProgressStore.ResetSessionMirror();

            Assert.AreEqual(expectedFrontier, StageProgressStore.Load(),
                "the advance reloads the scene, so the earned frontier must survive a restart or the player relaunches into a re-locked campaign");
        }

        // 3 -----------------------------------------------------------------------------

        [TestCase(StageId.Stage1)]
        [TestCase(StageId.Stage2)]
        public void TheStageJustCleared_PassesBothGatesGuardingTheAdvance(StageId cleared)
        {
            var next = ClearCampaignThrough(cleared);

            bool offeredByDesign = !StageDefinitions.For(next).locked;
            bool unlockedByProgress = StageProgress.IsUnlocked(StageProgressStore.Load(), next);

            Assert.IsTrue(offeredByDesign && unlockedByProgress,
                "the stage the player just cleared into must pass BOTH gates or the NEXT STAGE button silently does nothing — " +
                $"offered by design: {offeredByDesign}, unlocked by progress: {unlockedByProgress}");
        }

        // 4 -----------------------------------------------------------------------------

        [Test]
        public void ReplayingAnEarlierStage_LeavesTheEarnedFrontierAlone()
        {
            ClearCampaignThrough(StageId.Stage3);
            var earnedFrontier = StageProgressStore.Load();

            var afterReplay = StageProgressStore.RecordVictory(StageId.Stage1);

            Assert.AreEqual(earnedFrontier, afterReplay,
                "winning a rematch on an early stage must not be reported as the new frontier, or the results screen offers a stage the player cleared long ago");

            StageProgressStore.ResetSessionMirror();
            Assert.AreEqual(earnedFrontier, StageProgressStore.Load(),
                "a replay must never write progress backwards — that would re-lock the rest of the campaign the player already earned");
        }

        // 5 -----------------------------------------------------------------------------

        [Test]
        public void ClearingTheLastStage_HoldsTheFrontierAndOffersNothingFurther()
        {
            ClearCampaignThrough(StageId.Stage2);

            var afterFinalClear = StageProgressStore.RecordVictory(StageId.Stage3);

            Assert.AreEqual(StageId.Stage3, afterFinalClear,
                "finishing the campaign must leave the frontier resting on the last stage, never one past the end of the stage table");

            StageProgressStore.ResetSessionMirror();
            Assert.AreEqual(StageId.Stage3, StageProgressStore.Load(),
                "a completed campaign must reload as completed rather than as a profile pointing past its final stage");

            Assert.IsNull(StageProgress.NextStage(afterFinalClear),
                "with the campaign finished there is nothing left to offer, so the results screen must not advertise a fourth stage");
        }

        // 6 -----------------------------------------------------------------------------

        [Test]
        public void AFrontierWhosePersistenceWasLost_StillAdvancesForTheRestOfTheSession()
        {
            var earned = StageProgressStore.RecordVictory(StageId.Stage1);
            Assert.AreEqual(StageId.Stage2, earned,
                "guard: this test says nothing unless the clear really did move the frontier first");

            // The WebGL failure this defends against: PlayerPrefs.Save() returns normally but
            // the IndexedDB write never lands — tab closed too early, private-browsing profile,
            // or a storage-quota refusal. All silent, all leave the key simply absent.
            PlayerPrefs.DeleteKey(PrefsKey);
            PlayerPrefs.Save();

            Assert.AreEqual(StageId.Stage2, StageProgressStore.Load(),
                "a storage write the browser quietly dropped must not strand the player on the results screen unable to enter the stage they just earned");

            StageProgressStore.ResetSessionMirror();
            Assert.AreEqual(StageId.Stage1, StageProgressStore.Load(),
                "the safety net is scoped to the live session only — progress that never persisted must not come back as permanently earned on the next launch");
        }

        // 7 -----------------------------------------------------------------------------

        [TestCase(3)]
        [TestCase(99)]
        [TestCase(int.MaxValue)]
        [TestCase(-1)]
        [TestCase(-99)]
        [TestCase(int.MinValue)]
        public void ACorruptStoredFrontier_ResolvesToAStageThatActuallyExists(int corruptValue)
        {
            PlayerPrefs.SetInt(PrefsKey, corruptValue);
            PlayerPrefs.Save();
            StageProgressStore.ResetSessionMirror();

            var frontier = StageProgressStore.Load();

            // Round-trip through the layout table rather than a range check: For() answers an
            // unknown id with the first stage's layout instead of failing, so an out-of-range
            // frontier would quietly boot the wrong board. Identity here proves the frontier
            // names a stage the table can genuinely build.
            Assert.AreEqual(frontier, StageDefinitions.For(frontier).id,
                "a hand-edited or corrupted save must resolve to a stage the layout table can actually build, not one the game only pretends to load");
            Assert.IsTrue(StageProgress.IsUnlocked(frontier, StageId.Stage1),
                "however broken the save file is, the opening stage must stay enterable — a below-range frontier would lock the player out of the whole game");
        }

        // 8 -----------------------------------------------------------------------------

        [Test]
        public void AFurtherAlongSavedProfile_WinsOverThisSessionsSafetyNet()
        {
            // Cold boot on a profile that really is parked at the opening stage — written
            // explicitly rather than leaning on the absent-key default, so the seed stays
            // unambiguous even if that default ever changes.
            PlayerPrefs.SetInt(PrefsKey, (int)StageId.Stage1);
            PlayerPrefs.Save();
            StageProgressStore.ResetSessionMirror();

            Assert.AreEqual(StageId.Stage1, StageProgressStore.Load(),
                "guard: the session mirror has to latch low here or this test proves nothing");

            // Now the real saved profile shows up — a frontier earned in an earlier session.
            PlayerPrefs.SetInt(PrefsKey, (int)StageId.Stage3);
            PlayerPrefs.Save();

            Assert.AreEqual(StageId.Stage3, StageProgressStore.Load(),
                "progress restored from a previous session must not be dragged backwards by this session's safety net, or returning players find their unlocks gone");
        }
    }
}
