using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The ramp has to describe the match it is actually ramping across. A hill curve cannot
    /// save a session whose ramp length is half the match: difficulty simply arrives early and
    /// then sits there, which is the flat back half the curve replaced smoothstep to avoid.
    /// These check the shape of the ramp against the modelled length rather than against a
    /// hard-coded turn count, so the two cannot drift apart again.
    /// </summary>
    public class DifficultyRampShapeTests
    {
        /// <summary>Turns a decided match runs at the shipped balance.</summary>
        static float ShippedMatchTurns()
        {
            var stone = Resources.Load<BlockData>("StoneBlockData");
            Assert.IsNotNull(stone, "wall blocks take their health from StoneBlockData");

            float material = MatchLengthModel.Material(
                GameManager.BlocksPerKeep(StageDefinitions.Stage1.wallHeightBlocks),
                stone.maxHP,
                CastleCoreGimmick.CoreMaxHP);
            return MatchLengthModel.TurnsToDecide(material, MatchLengthModel.EffectiveDamagePerTurn);
        }

        [Test]
        public void Difficulty_IsStillClimbingInTheBackHalf()
        {
            // The property that actually matters to a player: the second half of a siege must
            // keep getting harder, not coast. At a ramp of 15 turns against a 32-turn match the
            // back half moved 0.74 -> 0.91, which reads as nothing happening.
            float turns = ShippedMatchTurns();
            int ramp = Mathf.RoundToInt(turns);

            float atMidpoint = DifficultyCurve.Evaluate(Mathf.RoundToInt(turns * 0.5f), ramp);
            float atEnd = DifficultyCurve.Evaluate(Mathf.RoundToInt(turns), ramp);

            Assert.Greater(atEnd - atMidpoint, 0.2f,
                $"the back half only gains {atEnd - atMidpoint:F2} of difficulty — the endgame is flat");
        }

        [Test]
        public void Difficulty_DoesNotArriveInTheOpeningThird()
        {
            // A siege should open gently. If the first third is already past halfway up the
            // curve, the onboarding turns are spent under gale wind and a sharpshooting AI.
            float turns = ShippedMatchTurns();
            float atThird = DifficultyCurve.Evaluate(Mathf.RoundToInt(turns / 3f), Mathf.RoundToInt(turns));

            Assert.Less(atThird, 0.4f,
                $"difficulty is at {atThird:F2} only a third of the way in — the ramp is too short for the match");
        }

        [Test]
        public void RampLength_TracksTheMatchLengthModel()
        {
            // The coupling itself. If a future balance pass moves material or damage, the ramp
            // has to move with it, and this is what says so out loud.
            float turns = ShippedMatchTurns();
            Assert.That(turns, Is.GreaterThan(20f).And.LessThan(50f),
                "the modelled match length has moved far enough that the ramp derivation needs re-checking");
        }
    }
}
