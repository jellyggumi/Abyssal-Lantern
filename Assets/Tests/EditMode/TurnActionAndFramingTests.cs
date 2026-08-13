using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the two usability models added 2026-08-13 (대포 설치 / 턴 행동 / 화면 핸들링).
    ///
    /// Both exist because a rule the player cannot see is, to that player, a bug: the
    /// battery sat behind four invisible gates reachable only by an unadvertised key, and
    /// the camera framed the sling at the screen edge so a player pulling could not see
    /// what they were aiming at.
    /// </summary>
    [TestFixture]
    public sealed class TurnActionAndFramingTests
    {
        // ---- TurnActionPrompt ----

        [Test]
        public void CannonPrompt_NamesTheFirstUnmetGate_InPermanenceOrder()
        {
            // Too early: the unlock turn is the gate, even with breaches and supply banked.
            var early = TurnActionPrompt.ForCannon(
                playerCanAct: true, deployArmed: false, turnCount: 0,
                breaches: 9, supply: 99f, cooldownRemaining: 0f);
            Assert.That(early.tone, Is.EqualTo(TurnActionPrompt.Tone.Blocked));
            Assert.That(early.label, Does.Contain("턴"), "an unlock gate must say how long is left");
            Assert.That(early.interactable, Is.False, "a blocked action must not offer a live button");

            // Unlocked but unbreached: the breach requirement is what the player must solve.
            int unlocked = DeploymentRules.UnlockTurn(DeployCard.Cannon) + 1;
            var unbreached = TurnActionPrompt.ForCannon(
                playerCanAct: true, deployArmed: false, turnCount: unlocked,
                breaches: 0, supply: 99f, cooldownRemaining: 0f);
            Assert.That(unbreached.tone, Is.EqualTo(TurnActionPrompt.Tone.Blocked));
            Assert.That(unbreached.label, Does.Contain("성벽"),
                "the breach gate is the least discoverable rule; it must be named outright");

            // Breached but broke: supply is the gate, and the number must be actionable.
            var poor = TurnActionPrompt.ForCannon(
                playerCanAct: true, deployArmed: false, turnCount: unlocked,
                breaches: DeploymentRules.CannonBreachRequirement, supply: 0f, cooldownRemaining: 0f);
            Assert.That(poor.tone, Is.EqualTo(TurnActionPrompt.Tone.Blocked));
            Assert.That(poor.label, Does.Contain("보급"));
        }

        [Test]
        public void CannonPrompt_WhenAvailable_StatesCostAndThatItSpendsTheTurn()
        {
            var ready = TurnActionPrompt.ForCannon(
                playerCanAct: true, deployArmed: false,
                turnCount: DeploymentRules.UnlockTurn(DeployCard.Cannon) + 1,
                breaches: DeploymentRules.CannonBreachRequirement,
                supply: DeploymentRules.CostOf(DeployCard.Cannon) + 1f,
                cooldownRemaining: 0f);

            Assert.That(ready.tone, Is.EqualTo(TurnActionPrompt.Tone.Ready));
            Assert.That(ready.interactable, Is.True);
            Assert.That(ready.label, Does.Contain("설치"), "the label must name the action");
            Assert.That(ready.label, Does.Contain("보급"), "…and its price");
            Assert.That(ready.label, Does.Contain("턴 소모"),
                "'one action per turn' is the rule players lose most often — say it on the button");
        }

        [Test]
        public void CannonPrompt_WhenArmed_SaysWhereToClickAndHowToCancel()
        {
            var armed = TurnActionPrompt.ForCannon(
                playerCanAct: true, deployArmed: true,
                turnCount: 99, breaches: 9, supply: 99f, cooldownRemaining: 0f);

            Assert.That(armed.tone, Is.EqualTo(TurnActionPrompt.Tone.Armed));
            Assert.That(armed.label, Does.Contain("위치"), "an armed click changes meaning — say what it does");
            Assert.That(armed.label, Does.Contain("Esc"), "…and always offer the way out");
        }

        [Test]
        public void CannonPrompt_OutsideThePlayersTurn_IsIdleAndInert()
        {
            var idle = TurnActionPrompt.ForCannon(
                playerCanAct: false, deployArmed: false,
                turnCount: 99, breaches: 9, supply: 99f, cooldownRemaining: 0f);

            Assert.That(idle.tone, Is.EqualTo(TurnActionPrompt.Tone.Idle));
            Assert.That(idle.interactable, Is.False);
        }

        [Test]
        public void EveryTone_HasItsOwnColour()
        {
            var tones = (TurnActionPrompt.Tone[])System.Enum.GetValues(typeof(TurnActionPrompt.Tone));
            for (int i = 0; i < tones.Length; i++)
            {
                for (int j = i + 1; j < tones.Length; j++)
                {
                    Assert.AreNotEqual(TurnActionPrompt.ColorFor(tones[i]), TurnActionPrompt.ColorFor(tones[j]),
                        $"{tones[i]} and {tones[j]} must be distinguishable before the text is read");
                }
            }
        }

        // ---- CameraFraming ----

        [Test]
        public void Zoom_NeverCropsBelowTheFittedBoard()
        {
            // The floor is the aspect fit: on a wide monitor the first thing lost to an
            // over-zoom is the enemy keep the player is aiming at.
            Assert.That(CameraFraming.ClampZoom(0.1f), Is.EqualTo(CameraFraming.MinZoom));
            Assert.That(CameraFraming.ClampZoom(99f), Is.EqualTo(CameraFraming.MaxZoom));
            Assert.That(CameraFraming.MinZoom, Is.EqualTo(1f),
                "zooming in past the fit crops the board — the fit is the floor");

            float zoom = CameraFraming.MinZoom;
            for (int i = 0; i < 50; i++) zoom = CameraFraming.ApplyZoomInput(zoom, +1f); // scroll in, hard
            Assert.That(zoom, Is.EqualTo(CameraFraming.MinZoom), "repeated zoom-in must rest exactly at the fit");

            for (int i = 0; i < 50; i++) zoom = CameraFraming.ApplyZoomInput(zoom, -1f); // scroll out, hard
            Assert.That(zoom, Is.EqualTo(CameraFraming.MaxZoom), "zoom-out is bounded too");
        }

        [Test]
        public void ZoomOut_EnlargesTheViewMonotonically()
        {
            float fitted = 12f;
            float near = CameraFraming.SizeForZoom(fitted, CameraFraming.MinZoom);
            float far = CameraFraming.SizeForZoom(fitted, CameraFraming.MaxZoom);

            Assert.That(near, Is.EqualTo(fitted).Within(0.0001f));
            Assert.That(far, Is.GreaterThan(near), "zooming out must show more board, not less");
        }

        [Test]
        public void AimFraming_WidensAndSlidesTowardTheSling_ThenReturns()
        {
            // Widening while drawing is the whole point: the fixed frame put the sling at the
            // screen edge, so a player pulling could not see the keep they were aiming at.
            Assert.That(CameraFraming.AimZoomMultiplier(1f), Is.GreaterThan(CameraFraming.AimZoomMultiplier(0f)));
            Assert.That(CameraFraming.AimZoomMultiplier(0f), Is.EqualTo(1f),
                "at rest the aim framing must contribute nothing");

            float slingX = -17f;
            float centered = CameraFraming.AimCenterX(0f, slingX, 0f);
            float aimed = CameraFraming.AimCenterX(0f, slingX, 1f);
            Assert.That(centered, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(aimed, Is.LessThan(0f), "aiming must slide toward the player's sling");
            Assert.That(aimed, Is.GreaterThan(slingX),
                "…but never centre ON it: that pushes the target off the far edge, which is " +
                "exactly the framing this replaces");
        }

        [Test]
        public void AimEase_IsFrameRateIndependentAndSettlesBothWays()
        {
            // One second of easing must land in the same place whether the browser tab runs
            // at 30fps or the desktop at 144Hz.
            float slow = 0f, fast = 0f;
            for (int i = 0; i < 30; i++) slow = CameraFraming.EaseAimWeight(slow, true, 1f / 30f);
            for (int i = 0; i < 144; i++) fast = CameraFraming.EaseAimWeight(fast, true, 1f / 144f);
            Assert.That(slow, Is.EqualTo(fast).Within(0.02f));

            Assert.That(slow, Is.GreaterThan(0.9f), "a second of aiming must be nearly fully widened");

            float releasing = slow;
            for (int i = 0; i < 144; i++) releasing = CameraFraming.EaseAimWeight(releasing, false, 1f / 144f);
            Assert.That(releasing, Is.LessThan(0.05f), "releasing must return the frame to rest");
        }
    }
}
