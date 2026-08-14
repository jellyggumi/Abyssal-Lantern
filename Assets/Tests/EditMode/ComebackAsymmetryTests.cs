using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// What the comeback layer's asymmetry actually is, because the recorded one is erased by a cap.
    ///
    /// Three documents state the player/AI difference as multipliers — `qa/exploit-register.md` E-1
    /// reasons about it directly: "플레이어 버프(2.2×)가 AI(1.6×)보다 세다". At the shipped base shot
    /// damage of 106 that is false. `SingleHitDamageCap` is 140, so 106×2.2 = 233 and 106×1.6 = 170
    /// both clamp to exactly 140 and the two sides land identical hits.
    ///
    /// The multipliers only differ in a window: base damage between 63.6 (140/2.2) and 87.5
    /// (140/1.6), where the player is capped and the AI is not. Above 87.5 both are capped; below
    /// 63.6 neither is. The shipped shot sits above the window.
    ///
    /// The real asymmetry is the LATCH, not the number. The player arms at 35% and holds Armed
    /// indefinitely, spending it on a shot they choose; the AI's `AdvanceAuto` goes straight to
    /// Active and fires at whatever line it happens to have. At the measured hit rates (Stage1 0.73,
    /// Stage3 0.57) waiting for a clean line is worth 1.30x to 1.67x the expected damage. That is
    /// the advantage, and it is a skill-expression advantage — which is the same axis as the
    /// measured skill cliff, so it compounds rather than compensating.
    ///
    /// Measured in `qa/evidence/g2/g2-remeasured-20260814.log` and the survey at
    /// `.survey/siege-first-turn-fairness/`.
    /// </summary>
    public class ComebackAsymmetryTests
    {
        // SiegeBalanceSettings.Default.baseShotDamage. Referenced rather than copied so a retune
        // moves this test with it.
        private static float ShippedBaseShot => SiegeBalanceSettings.Default.baseShotDamage;

        [Test]
        public void AtTheShippedShot_TheCapErasesTheMultiplierAsymmetry()
        {
            float player = LastStand.BuffedDamage(ShippedBaseShot, isPlayer: true);
            float ai = LastStand.BuffedDamage(ShippedBaseShot, isPlayer: false);

            Assert.AreEqual(LastStand.SingleHitDamageCap, player, 0.001f,
                $"the player's buffed shot ({ShippedBaseShot}x{LastStand.PlayerDamageMult}) must be "
                + "capped at the shipped base damage");
            Assert.AreEqual(LastStand.SingleHitDamageCap, ai, 0.001f,
                $"the AI's buffed shot ({ShippedBaseShot}x{LastStand.AiDamageMult}) must be capped too");

            Assert.AreEqual(player, ai, 0.001f,
                "both sides land the same buffed hit at the shipped base damage, so any document "
                + "reasoning from '2.2x is stronger than 1.6x' is reasoning about a window the "
                + "shipped shot is not in - see qa/exploit-register.md E-1");
        }

        [Test]
        public void TheMultipliersOnlyDifferInsideAMeasurableWindow()
        {
            float playerCapFloor = LastStand.SingleHitDamageCap / LastStand.PlayerDamageMult;
            float aiCapFloor = LastStand.SingleHitDamageCap / LastStand.AiDamageMult;

            Assert.Less(playerCapFloor, aiCapFloor,
                "the stronger multiplier must reach the cap at lower base damage");

            // Inside the window: player capped, AI not. This is where the recorded asymmetry is real.
            float inside = 0.5f * (playerCapFloor + aiCapFloor);
            Assert.AreEqual(LastStand.SingleHitDamageCap,
                LastStand.BuffedDamage(inside, isPlayer: true), 0.001f,
                $"at base {inside:F1} the player must be capped");
            Assert.Less(LastStand.BuffedDamage(inside, isPlayer: false), LastStand.SingleHitDamageCap,
                $"at base {inside:F1} the AI must NOT be capped - this is the only band where the "
                + "player's larger multiplier means anything");

            // Below the window: neither capped, and the asymmetry is the raw ratio.
            float below = playerCapFloor * 0.5f;
            float ratio = LastStand.BuffedDamage(below, isPlayer: true)
                          / LastStand.BuffedDamage(below, isPlayer: false);
            Assert.AreEqual(LastStand.PlayerDamageMult / LastStand.AiDamageMult, ratio, 0.001f,
                "below the window the documented ratio holds exactly");

            Assert.Greater(ShippedBaseShot, aiCapFloor,
                $"the shipped shot ({ShippedBaseShot}) sits ABOVE the window ({playerCapFloor:F1}-"
                + $"{aiCapFloor:F1}), which is why the asymmetry is not observable in a real match");
        }

        /// <summary>
        /// The latch difference, which is what the asymmetry actually is. Pinned as behaviour rather
        /// than prose so a future edit to either Advance function has to face it.
        /// </summary>
        [Test]
        public void ThePlayerHoldsTheComebackAndTheAiSpendsItImmediately()
        {
            // Entering danger.
            Assert.AreEqual(LastStand.Phase.Armed,
                LastStand.Advance(LastStand.Phase.Locked, inDanger: true),
                "the player's latch stops at Armed - the shot is theirs to time");
            Assert.AreEqual(LastStand.Phase.Active,
                LastStand.AdvanceAuto(LastStand.Phase.Locked, inDanger: true),
                "the AI's latch goes straight to Active - it fires on the line it has");

            // And the hold survives leaving the band, which is what makes it a stored resource.
            Assert.AreEqual(LastStand.Phase.Armed,
                LastStand.Advance(LastStand.Phase.Armed, inDanger: false),
                "an armed comeback must survive recovering above the band, or arming would be a "
                + "trap rather than a resource");
        }

        /// <summary>
        /// Worms' community rules had to patch an exploit where protection became a shield
        /// (worms2d.info Scheme_rules, "Pile" clause: immunity does not apply while piled with an
        /// attackable player). The equivalent here would be camping the danger band to keep the
        /// buff. This design forecloses it, and the reason is worth pinning: the phase is a one-way
        /// consumable, so there is nothing to re-earn by staying wounded.
        /// </summary>
        [Test]
        public void CampingTheDangerBandCannotReArmTheComeback()
        {
            // Once consumed, re-entering danger must not hand it back.
            Assert.AreEqual(LastStand.Phase.Consumed,
                LastStand.Advance(LastStand.Phase.Consumed, inDanger: true),
                "a consumed comeback must stay consumed - otherwise sitting at 34% core would be a "
                + "renewable buff, which is the exploit Worms' Pile clause had to patch");
            Assert.AreEqual(LastStand.Phase.Consumed,
                LastStand.AdvanceAuto(LastStand.Phase.Consumed, inDanger: true),
                "same for the AI mirror");

            // And an active one is not re-activated into anything stronger.
            Assert.AreEqual(LastStand.Phase.Active,
                LastStand.Advance(LastStand.Phase.Active, inDanger: true),
                "danger must not stack onto an already-active comeback");
        }

        /// <summary>
        /// The threshold is inclusive at exactly 35%, and that boundary is load-bearing: an
        /// exclusive test would leave a core sitting at precisely the fraction with no comeback,
        /// which reads as the mechanic being broken rather than as a boundary choice.
        /// </summary>
        [Test]
        public void TheDangerBoundaryIsInclusiveAndDeathIsNotDanger()
        {
            float max = 150f;
            float exactly = max * LastStand.DangerHpFraction;

            Assert.IsTrue(LastStand.IsDanger(exactly, max),
                $"a core at exactly {LastStand.DangerHpFraction:P0} must be in danger");
            Assert.IsTrue(LastStand.IsDanger(exactly - 1f, max), "below the fraction is danger");
            Assert.IsFalse(LastStand.IsDanger(exactly + 1f, max), "above the fraction is not");

            Assert.IsFalse(LastStand.IsDanger(0f, max),
                "a destroyed core is not in danger - it is dead, and arming a comeback for it would "
                + "fire after the match is decided");
        }
    }
}
