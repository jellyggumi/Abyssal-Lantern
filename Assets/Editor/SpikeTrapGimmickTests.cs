using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// EditMode pins for the SpikeTrapGimmick proximity-triggered state machine: phase
    /// transitions (Dormant/Arming/Active/Cooldown) and the deterministic knockback formula.
    /// </summary>
    public class SpikeTrapGimmickTests
    {
        private const float ArmDelay = 0.4f;
        private const float ActiveDuration = 0.5f;
        private const float CooldownDuration = 2.0f;

        [Test]
        public void Dormant_StaysDormant_WhenNoBodyDetected_RegardlessOfElapsed()
        {
            Assert.AreEqual(SpikeTrapPhase.Dormant,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Dormant, false, 0f, ArmDelay, ActiveDuration, CooldownDuration));
            Assert.AreEqual(SpikeTrapPhase.Dormant,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Dormant, false, 100f, ArmDelay, ActiveDuration, CooldownDuration),
                "large elapsed time must not force a transition without a detected body");
        }

        [Test]
        public void Dormant_TransitionsToArming_TheInstantBodyDetected()
        {
            Assert.AreEqual(SpikeTrapPhase.Arming,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Dormant, true, 0f, ArmDelay, ActiveDuration, CooldownDuration),
                "elapsed=0 must still arm the instant a body is detected");
        }

        [Test]
        public void Arming_HoldsUntilArmDelay_ThenBecomesActive()
        {
            Assert.AreEqual(SpikeTrapPhase.Arming,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Arming, false, ArmDelay - 0.01f, ArmDelay, ActiveDuration, CooldownDuration),
                "just under armDelaySeconds must still be Arming");
            Assert.AreEqual(SpikeTrapPhase.Active,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Arming, false, ArmDelay, ArmDelay, ActiveDuration, CooldownDuration),
                "exactly at armDelaySeconds must transition to Active");
            Assert.AreEqual(SpikeTrapPhase.Active,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Arming, true, ArmDelay + 0.1f, ArmDelay, ActiveDuration, CooldownDuration),
                "bodyDetected is irrelevant outside Dormant — timing alone drives Arming->Active");
        }

        [Test]
        public void Active_HoldsUntilActiveDuration_ThenBecomesCooldown()
        {
            Assert.AreEqual(SpikeTrapPhase.Active,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Active, false, ActiveDuration - 0.01f, ArmDelay, ActiveDuration, CooldownDuration),
                "just under activeDuration must still be Active");
            Assert.AreEqual(SpikeTrapPhase.Cooldown,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Active, false, ActiveDuration, ArmDelay, ActiveDuration, CooldownDuration),
                "exactly at activeDuration must transition to Cooldown");
        }

        [Test]
        public void Cooldown_ReturnsToDormant_AfterCooldownDuration_EvenIfBodyStillDetected()
        {
            Assert.AreEqual(SpikeTrapPhase.Cooldown,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Cooldown, true, CooldownDuration - 0.01f, ArmDelay, ActiveDuration, CooldownDuration),
                "just under cooldownDuration must still be Cooldown");
            Assert.AreEqual(SpikeTrapPhase.Dormant,
                SpikeTrapRules.NextPhase(SpikeTrapPhase.Cooldown, true, CooldownDuration, ArmDelay, ActiveDuration, CooldownDuration),
                "Cooldown must return to Dormant on schedule even with bodyDetected=true — no direct Cooldown->Arming shortcut, no early re-arm");
        }

        [Test]
        public void KnockbackVelocity_AlwaysHasPositiveY_IncludingAtZeroHorizontalOffset()
        {
            var trap = new Vector2(0f, 0f);

            Vector2 left = SpikeTrapRules.KnockbackVelocity(new Vector2(-3f, 0f), trap, 9f, 0.6f);
            Vector2 right = SpikeTrapRules.KnockbackVelocity(new Vector2(3f, 0f), trap, 9f, 0.6f);
            Vector2 same = SpikeTrapRules.KnockbackVelocity(new Vector2(0f, 0f), trap, 9f, 0.6f);

            Assert.Greater(left.y, 0f);
            Assert.Greater(right.y, 0f);
            Assert.Greater(same.y, 0f, "unitPosition.x == trapPosition.x must not throw/NaN and must still launch upward");
            Assert.IsFalse(float.IsNaN(same.x), "zero-vector normalize guard must avoid NaN");
            Assert.IsFalse(float.IsNaN(same.y));
        }

        [Test]
        public void KnockbackVelocity_PushesAwayFromTrap_MatchingHorizontalSign()
        {
            var trap = new Vector2(5f, 0f);

            Vector2 unitToLeft = SpikeTrapRules.KnockbackVelocity(new Vector2(2f, 0f), trap, 9f, 0.6f);
            Vector2 unitToRight = SpikeTrapRules.KnockbackVelocity(new Vector2(8f, 0f), trap, 9f, 0.6f);

            Assert.Less(unitToLeft.x, 0f, "unit left of trap must be pushed further left");
            Assert.Greater(unitToRight.x, 0f, "unit right of trap must be pushed further right");
        }

        [Test]
        public void SpikeTrapGimmick_AddComponent_DoesNotThrow_AndHasSpriteRenderer()
        {
            // Project convention (confirmed live against Unity 2022.3 EditMode): every gimmick
            // spawner in the codebase pre-adds SpriteRenderer BEFORE the gimmick component,
            // because a component's own Awake() calling AddComponent<SpriteRenderer>() on the
            // same GameObject silently fails to attach when triggered from within another
            // component's synchronous Awake() in EditMode. Mirrors
            // CastleCoreGimmick_InitializesAndPulses / Gimmicks_UseDedicatedSprites_NotTintedBlocks
            // in GamePlayTests.cs and GameManager.SpawnExplosiveBarrel/SpawnMovingGimmick.
            var go = new GameObject("SpikeTrapTest");
            try
            {
                go.AddComponent<SpriteRenderer>();
                var trap = go.AddComponent<SpikeTrapGimmick>();
                Assert.IsNotNull(trap);
                Assert.IsNotNull(go.GetComponent<SpriteRenderer>(), "SpriteRenderer must be present");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
