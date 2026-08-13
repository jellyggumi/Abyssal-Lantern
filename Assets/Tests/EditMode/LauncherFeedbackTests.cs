using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the launcher's aim-and-fire arithmetic (`LauncherFeedback`).
    ///
    /// These are not taste constants. Each is bounded by something sourced in
    /// `.survey/siege-impact-vfx-and-attack-motion/`, and the pins state the bound rather than the
    /// number so a later tuning pass can move the value and still be told when it leaves the band
    /// the evidence supports.
    /// </summary>
    public class LauncherFeedbackTests
    {
        /// <summary>
        /// The enemy beat is 0.4s + 0.5s, and the windup has to live inside the 0.5s half without
        /// extending it. Spending more AI time is the move the evidence argues against: GameSpot
        /// faulted Worms Armageddon for how long its AI turns took, and this project already
        /// reclaimed 2.1s of dead air and reinvested it in more turns.
        /// </summary>
        [Test]
        public void Windup_FitsInsideTheExistingAiPause()
        {
            Assert.Less(LauncherFeedback.WindupSeconds, 0.5f,
                "the windup must fit the pause SimpleAI already takes — it may not buy new seconds");
        }

        /// <summary>
        /// And it must clear the floor where a warning stops working as one: constant foreperiods
        /// under about 300ms can produce *slower* responses, because processing of the warning has
        /// not completed when the stimulus arrives.
        /// </summary>
        [Test]
        public void Windup_ClearsTheForeperiodFloor()
        {
            Assert.GreaterOrEqual(LauncherFeedback.WindupSeconds, 0.3f,
                "below ~300ms a warning signal is not read as a warning");
        }

        /// <summary>
        /// Fighting games put a readable committed windup in the 250-370ms band (Guilty Gear
        /// Strive at 60fps: Gun Flame 16F/267ms, Fafnir 20F/333ms). Staying inside it keeps the
        /// pose in the range where a heavy action is legible as heavy rather than as a twitch.
        /// </summary>
        [Test]
        public void Windup_SitsInTheReadableHeavyAttackBand()
        {
            Assert.That(LauncherFeedback.WindupSeconds, Is.InRange(0.25f, 0.37f),
                "250-370ms is the observed band for a windup that reads as a committed heavy attack");
        }

        [Test]
        public void WindupProgress_RunsFromRestToFullDraw()
        {
            Assert.AreEqual(0f, LauncherFeedback.WindupProgress(0f), 1e-4f);
            Assert.AreEqual(1f, LauncherFeedback.WindupProgress(LauncherFeedback.WindupSeconds), 1e-4f);
            Assert.AreEqual(1f, LauncherFeedback.WindupProgress(LauncherFeedback.WindupSeconds * 3f), 1e-4f,
                "an overrun windup holds at full draw rather than wrapping");
        }

        /// <summary>
        /// Eased, not linear. The extremes are what the eye reads — "more pictures are drawn near
        /// the beginning and end of an action... fewer pictures are drawn within the middle" — and
        /// a launcher that creeps uniformly presents no extreme to read.
        /// </summary>
        [Test]
        public void WindupProgress_IsEasedNotLinear()
        {
            float mid = LauncherFeedback.WindupProgress(LauncherFeedback.WindupSeconds * 0.5f);
            Assert.AreEqual(0.5f, mid, 1e-3f, "smoothstep crosses its own midpoint at half time");

            float quarter = LauncherFeedback.WindupProgress(LauncherFeedback.WindupSeconds * 0.25f);
            Assert.Less(quarter, 0.25f, "the pose should start slowly, so early progress lags linear");
        }

        [Test]
        public void WindupScale_LoadsTheLauncherThenHoldsIt()
        {
            Assert.AreEqual(1f, LauncherFeedback.WindupScale(0f), 1e-4f);
            Assert.AreEqual(1f + LauncherFeedback.WindupSquash, LauncherFeedback.WindupScale(1f), 1e-4f);
            Assert.Greater(LauncherFeedback.WindupScale(1f), LauncherFeedback.WindupScale(0.5f));
        }

        /// <summary>
        /// Cubic decay, matching the cannon barrel already on this board: most of the travel lands
        /// in the first frames, which reads as a punch instead of a slide. A linear kick would put
        /// half the travel at half the time; this must be well above that.
        /// </summary>
        [Test]
        public void RecoilOffset_FrontLoadsTheTravel()
        {
            float peak = LauncherFeedback.RecoilOffset(LauncherFeedback.RecoilSeconds);
            Assert.AreEqual(LauncherFeedback.RecoilDistance, peak, 1e-4f);

            float half = LauncherFeedback.RecoilOffset(LauncherFeedback.RecoilSeconds * 0.5f);
            Assert.Less(half, peak * 0.5f,
                "cubic decay must leave less than half the travel at half the timer — that is what "
                + "distinguishes a kick from a slide");
        }

        [Test]
        public void RecoilOffset_IsZeroAtRestAndNeverNegative()
        {
            Assert.AreEqual(0f, LauncherFeedback.RecoilOffset(0f), 1e-6f);
            Assert.AreEqual(0f, LauncherFeedback.RecoilOffset(-1f), 1e-6f);
        }

        /// <summary>
        /// The waiting side is dimmed, never hidden. Hiding was the actual defect: the player's
        /// launcher was switched off for the whole enemy turn and the enemy apron had no visual at
        /// all, so both muzzles were empty for the 0.9s in which the enemy shoots.
        /// </summary>
        [Test]
        public void WaitingSide_IsDimmedButStillPresent()
        {
            Assert.Greater(LauncherFeedback.IdleSideAlpha, 0f,
                "a hidden launcher is what removed the only thing that could carry attribution");
            Assert.Less(LauncherFeedback.IdleSideAlpha, LauncherFeedback.ActingSideAlpha,
                "the acting side must be the brighter of the two, or the highlight says nothing");
        }

        /// <summary>
        /// The gap has to be wide enough to read at a glance. 1.5x is a conservative floor — well
        /// under the 3:1 lightness ratio that would let this stand alone as a distinction, which is
        /// why the arcs also carry a non-colour channel rather than leaning on this one.
        /// </summary>
        [Test]
        public void ActingHighlight_IsAVisibleGapNotACosmeticOne()
        {
            Assert.GreaterOrEqual(LauncherFeedback.ActingSideAlpha / LauncherFeedback.IdleSideAlpha, 1.5f);
        }

        [Test]
        public void SideAlpha_TracksWhoIsActing()
        {
            Assert.AreEqual(LauncherFeedback.ActingSideAlpha, LauncherFeedback.SideAlpha(true), 1e-4f);
            Assert.AreEqual(LauncherFeedback.IdleSideAlpha, LauncherFeedback.SideAlpha(false), 1e-4f);
        }

        /// <summary>
        /// The two launchers share one physical vocabulary. If the cannon's recoil and the sling's
        /// diverge, the board is telling the player that two machines obey different physics.
        /// </summary>
        [Test]
        public void Recoil_SharesTheCannonsTiming()
        {
            Assert.AreEqual(0.26f, LauncherFeedback.RecoilSeconds, 1e-4f,
                "CannonController.RecoilSeconds is 0.26f; the launchers must kick on the same clock");
        }
    }
}
