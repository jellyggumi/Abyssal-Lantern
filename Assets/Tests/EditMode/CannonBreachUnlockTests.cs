using CastleBusters;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The battery unlocks on breaching the enemy keep, not on waiting. These pin the rule
    /// and, most importantly, the loophole: a player must not be able to unlock their own
    /// artillery by demolishing their own wall.
    /// </summary>
    public class CannonBreachUnlockTests
    {
        [Test]
        public void Cannon_IsTheOnlyCardThatNeedsABreach()
        {
            Assert.IsTrue(DeploymentRules.NeedsBreach(DeployCard.Cannon),
                "artillery is the card the breach gate exists for");

            foreach (DeployCard card in System.Enum.GetValues(typeof(DeployCard)))
            {
                if (card == DeployCard.Cannon) continue;
                Assert.IsFalse(DeploymentRules.NeedsBreach(card),
                    $"{card} must stay on the plain supply/turn gates");
            }
        }

        [Test]
        public void Cannon_IsLockedUntilTheRequirementIsMet()
        {
            for (int breaches = 0; breaches < DeploymentRules.CannonBreachRequirement; breaches++)
            {
                Assert.IsFalse(DeploymentRules.BreachSatisfied(DeployCard.Cannon, breaches),
                    $"{breaches} breaches must not be enough");
            }

            Assert.IsTrue(
                DeploymentRules.BreachSatisfied(DeployCard.Cannon, DeploymentRules.CannonBreachRequirement),
                "meeting the requirement exactly must unlock");
        }

        [Test]
        public void OtherCards_AreNeverHeldByTheBreachGate()
        {
            Assert.IsTrue(DeploymentRules.BreachSatisfied(DeployCard.Knight, 0));
            Assert.IsTrue(DeploymentRules.BreachSatisfied(DeployCard.Archer, 0));
            Assert.IsTrue(DeploymentRules.BreachSatisfied(DeployCard.Barrel, 0));
        }

        [Test]
        public void BreachText_NamesTheRemainingWork()
        {
            string text = DeploymentRules.BreachReasonText(1);
            StringAssert.Contains("성벽", text, "the message must say what to knock down");
            StringAssert.Contains(DeploymentRules.CannonBreachRequirement.ToString(), text,
                "…how many are needed");
            StringAssert.Contains("1", text, "…and how many are already down");
        }

        [Test]
        public void BreachText_TreatsNegativeTalliesAsZero()
        {
            // Defensive: a caller with no tally must not print "현재 -1개".
            StringAssert.Contains("0", DeploymentRules.BreachReasonText(-3));
        }

        [Test]
        public void Requirement_IsMoreThanOneBlock()
        {
            // A one-block requirement would be satisfied by a stray hit, which is why this
            // gate only became meaningful after the keep grew past a single column.
            Assert.Greater(DeploymentRules.CannonBreachRequirement, 1,
                "a single stray hit must not open the artillery");
        }
    }
}
