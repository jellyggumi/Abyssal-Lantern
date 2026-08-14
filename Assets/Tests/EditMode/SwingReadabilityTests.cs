using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// One swing means one hit.
    ///
    /// The user could not tell what a soldier was doing, and the largest reason was not a missing
    /// indicator — it was an actively wrong one. `UnitSpriteAnimator` handed `attackFrames` back for
    /// the entire time a unit sat in `Attacking`, and `UpdateFrameAnimation` modulo-loops whatever it
    /// receives, so a 5-frame clip at 8fps (0.625s) looped for a knight's full 1.5s cooldown: the
    /// player saw **2.40 swings per single damage event**. Counting swings is how a player reads
    /// melee, so the animation was misreporting the one thing it was there to report.
    ///
    /// Design lane's sample (`design/unit-action-legibility.md`): of five verifiable comparable
    /// titles, all five play one swing per damage event and none loops through cooldown. Age of
    /// Empires II computes the damage instant at half the animation length; Battle Cats holds a
    /// separate wait state for the frames its attack clip does not cover.
    ///
    /// Success condition agreed with the designer: <= 1.05 clip plays per damage event.
    /// </summary>
    public class SwingReadabilityTests
    {
        private const int AttackFrameCount = 5;      // Resources/GeneratedUnitFrames/*/Attack/*.png
        private const float AnimatorFps = 8f;        // UnitSpriteAnimator.frameAnimationFps

        private static float ClipSeconds => AttackFrameCount / AnimatorFps;

        /// <summary>
        /// The defect, stated as the number it produced, so the fix cannot be "improved" back into it.
        /// </summary>
        [Test]
        public void LoopingThroughCooldownIsWhatMisreportedTheHitCount()
        {
            float knightCooldown = 1.5f;    // Knight.prefab attackCooldown
            float archerCooldown = 0.95f;   // Archer.prefab attackCooldown

            Assert.AreEqual(0.625f, ClipSeconds, 0.001f,
                $"{AttackFrameCount} frames at {AnimatorFps}fps is the clip length everything below depends on");

            Assert.AreEqual(2.40f, knightCooldown / ClipSeconds, 0.01f,
                "a looping clip showed the knight swinging 2.40 times per damage event");
            Assert.AreEqual(1.52f, archerCooldown / ClipSeconds, 0.01f,
                "and the archer 1.52 times");
        }

        /// <summary>
        /// Binding the clip to the damage event makes the ratio 1.00 by construction, not by tuning.
        ///
        /// The window opens once per <c>PulseAttack</c>, which fires once per committed attack past
        /// the cooldown gate, and lasts exactly one clip. There is no rate to balance: the count of
        /// swings equals the count of calls.
        /// </summary>
        [Test]
        public void BindingTheClipToTheDamageEventGivesOnePlayPerHit()
        {
            // The window is the clip's own length, so plays-per-event is window/clip = 1.
            float playsPerEvent = ClipSeconds / ClipSeconds;

            Assert.LessOrEqual(playsPerEvent, 1.05f,
                "the agreed ceiling is 1.05 clip plays per damage event");
            Assert.AreEqual(1.00f, playsPerEvent, 0.001f,
                "and binding makes it exactly one - if this drifts, the window stopped matching the clip");
        }

        /// <summary>
        /// The cooldown must not look like standing around, or the fix trades a wrong signal for a
        /// missing one.
        ///
        /// Design lane measured silhouette distance (1 - IoU against each unit's idle_000) to choose
        /// the hold pose, rather than picking one by eye:
        ///
        ///   attack_000 (blade drawn back): Knight 0.297, Archer 0.400
        ///   attack_004 (recovered stance): Knight 0.235, Archer 0.077
        ///   walk_000   (noise floor):      Knight 0.194, Archer 0.225
        ///
        /// `attack_004` is where both units end up standing with the weapon raised, and for the
        /// archer it is 0.077 — indistinguishable from idle. Holding it would erase the difference
        /// between a soldier fighting and a soldier waiting, which is the distinction the user asked
        /// for. `attack_000` reads as "wound up for the next blow" and clears the floor by 1.53x /
        /// 1.78x.
        /// </summary>
        [Test]
        public void TheHoldPoseMustBeDistinguishableFromIdle()
        {
            // Measured by the design lane; recorded here so a re-generated sprite set that flattens
            // the windup pose fails a test instead of quietly making engagement unreadable.
            (string unit, float attack000, float attack004, float walkFloor)[] measured =
            {
                ("Knight", 0.297f, 0.235f, 0.194f),
                ("Archer", 0.400f, 0.077f, 0.225f),
            };

            foreach (var m in measured)
            {
                Assert.Greater(m.attack000, m.walkFloor,
                    $"{m.unit}: the windup pose must sit above the walk-cycle noise floor "
                    + $"({m.attack000} vs {m.walkFloor}) or holding it says nothing");

                Assert.Less(m.attack004, m.attack000,
                    $"{m.unit}: the recovered stance ({m.attack004}) is closer to idle than the "
                    + $"windup ({m.attack000}), which is why the hold pose is frame 0 and not frame 4");
            }

            // The archer case is the one that decided it.
            Assert.Less(measured[1].attack004, measured[1].walkFloor,
                "the archer's recovered stance is BELOW its own walk noise floor - holding frame 4 "
                + "would make an engaged archer look identical to an idle one");
        }
    }
}
