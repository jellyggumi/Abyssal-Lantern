using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The forecast line exists because the one-shot loop knows three things exactly and told
    /// the player none of them: what is loaded, what answers it, and how far along the match is.
    /// These pin the two that can be wrong in a way nobody would notice on screen — naming the
    /// wrong projectile, or attributing the next one to the wrong side.
    /// </summary>
    public class SiegeForecastTests
    {
        [Test]
        public void NextTurnProjectile_MatchesWhatThatTurnWillActuallyLoad()
        {
            // The forecast must be derived from the same rule the launcher obeys, not a second
            // copy of the cycle that can drift from it.
            for (int turn = 0; turn < 24; turn++)
            {
                Assert.AreEqual(
                    OneShotSiegeRules.ProjectileForTurn(turn + 1),
                    OneShotSiegeRules.ProjectileForNextTurn(turn),
                    $"turn {turn}: the forecast disagrees with what turn {turn + 1} loads");
            }
        }

        [Test]
        public void TheLine_NamesTheCurrentProjectileAndAttributesTheNextToTheOtherSide()
        {
            // On the player's turn the next shot is the enemy's, and vice versa. Telling someone
            // a 화약통 is next without saying whose it is is worse than staying silent.
            string playerTurn = SiegeForecastStrip.BuildLine(0, isPlayerTurn: true);
            string enemyTurn = SiegeForecastStrip.BuildLine(0, isPlayerTurn: false);

            string now = OneShotSiegeRules.DisplayName(OneShotSiegeRules.ProjectileForTurn(0));
            StringAssert.Contains(now, playerTurn, "the line must name what is loaded now");

            StringAssert.Contains("적", playerTurn, "on the player's turn the next shot is the enemy's");
            StringAssert.Contains("내", enemyTurn, "on the enemy's turn the next shot is the player's");
        }

        [Test]
        public void TheLine_ShowsProgressAgainstTheModelledMatchLength()
        {
            // Core HP was the only progress signal and it answers a different question, so the
            // denominator here has to be the modelled match length rather than a magic number.
            Assert.AreEqual(
                Mathf.RoundToInt(MatchLengthModel.TargetMatchSeconds / MatchLengthModel.AverageTurnSeconds),
                SiegeForecastStrip.ModelledMatchTurns,
                "the turn denominator must be derived from the match-length model");

            StringAssert.Contains("1턴", SiegeForecastStrip.BuildLine(0, true),
                "turn 0 must read as turn 1 to the player, not turn 0");
            StringAssert.Contains($"{SiegeForecastStrip.ModelledMatchTurns}턴",
                SiegeForecastStrip.BuildLine(0, true),
                "the line must state the match length it is measuring against");
        }
    }
}
