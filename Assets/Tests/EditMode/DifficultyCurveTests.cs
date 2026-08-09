using CastleBusters;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the shape of the session difficulty ramp. The properties matter more than the
    /// exact numbers: the old smoothstep satisfied "rises" but flattened permanently at the
    /// ramp turn, which is the regression these tests exist to prevent.
    /// </summary>
    public class DifficultyCurveTests
    {
        const int Ramp = 15;

        [Test]
        public void Evaluate_StartsAtZeroAndStaysBelowOne()
        {
            Assert.AreEqual(0f, DifficultyCurve.Evaluate(0, Ramp), 1e-6f,
                "Turn 0 must carry no accumulated pressure.");
            Assert.Less(DifficultyCurve.Evaluate(10000, Ramp), 1f,
                "The curve approaches 1 asymptotically; reaching it would let a downstream " +
                "value sit permanently at its hardest bound.");
        }

        [Test]
        public void Evaluate_IsStrictlyIncreasingWellPastTheRampTurn()
        {
            // The point of the change: difficulty must keep moving after difficultyRampTurns.
            for (int turn = 0; turn < 60; turn++)
            {
                float a = DifficultyCurve.Evaluate(turn, Ramp);
                float b = DifficultyCurve.Evaluate(turn + 1, Ramp);
                Assert.Greater(b, a, $"Turn {turn + 1} must be harder than turn {turn}.");
            }
        }

        [Test]
        public void Evaluate_IsHalfwayAtTheConfiguredHalfTurn()
        {
            int half = (int)(Ramp * DifficultyCurve.HalfTurnFraction);
            Assert.AreEqual(0.5f, DifficultyCurve.Evaluate(half, Ramp), 0.02f,
                "The half turn is the curve's anchor point for tuning.");
        }

        [Test]
        public void Evaluate_OpeningIsGentlerThanLinear()
        {
            // A forgiving opening is the whole reason for a non-linear curve: at a third of
            // the ramp, pressure must be well under a third of maximum.
            int early = Ramp / 3;
            float linear = early / (float)Ramp;
            Assert.Less(DifficultyCurve.Evaluate(early, Ramp), linear,
                "Early turns must teach, not punish.");
        }

        [Test]
        public void Evaluate_SlopeShrinksAfterTheHalfTurn()
        {
            // Non-linear in both directions: steepest in the middle, ever-smaller increments
            // late, so a long match tightens without becoming unplayable.
            int half = (int)(Ramp * DifficultyCurve.HalfTurnFraction);
            float midSlope = DifficultyCurve.Evaluate(half + 1, Ramp) - DifficultyCurve.Evaluate(half, Ramp);
            float lateSlope = DifficultyCurve.Evaluate(half + 21, Ramp) - DifficultyCurve.Evaluate(half + 20, Ramp);
            Assert.Less(lateSlope, midSlope, "Late increments must diminish.");
            Assert.Greater(lateSlope, 0f, "…but never reach zero.");
        }
    }
}
