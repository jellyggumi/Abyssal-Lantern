using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Session difficulty as a function of turn number.
    ///
    /// The previous ramp was <c>SmoothStep(0,1, turn/rampTurns)</c>, which reaches exactly
    /// 1.0 at the ramp turn and then stays there: after turn 15 the wind ceiling, AI
    /// accuracy and storm odds never changed again, so a long match had a flat back half.
    ///
    /// This is a Hill curve instead:
    /// <code>D(n) = n^p / (n^p + h^p)</code>
    /// It is strictly increasing for every additional turn, is non-linear in both
    /// directions (a forgiving opening, the steepest slope around the half turn, then
    /// ever-smaller increments), and approaches 1 asymptotically without ever reaching a
    /// plateau. Difficulty therefore keeps tightening for as long as the match runs while
    /// staying bounded, so no downstream value can escape its configured band.
    ///
    /// Shape with the defaults (h = 9, p = 1.8):
    /// turn 1 → 0.02, 3 → 0.12, 6 → 0.33, 9 → 0.50, 12 → 0.63, 15 → 0.72, 30 → 0.90.
    /// </summary>
    public static class DifficultyCurve
    {
        /// <summary>Turn at which difficulty is exactly half. Derived from the configured
        /// ramp length so existing tuning keeps its meaning.</summary>
        public const float HalfTurnFraction = 0.6f;

        /// <summary>Curve steepness. Above 1 the opening turns stay gentle and the middle
        /// sharpens; at 1 the curve degenerates to a plain saturating hyperbola.</summary>
        public const float Steepness = 1.8f;

        /// <summary>Difficulty in [0,1) for a turn index, given the configured ramp length.</summary>
        public static float Evaluate(int turn, int rampTurns)
        {
            float n = Mathf.Max(0f, turn);
            if (n <= 0f) return 0f;

            float half = Mathf.Max(1f, rampTurns * HalfTurnFraction);
            float np = Mathf.Pow(n, Steepness);
            float hp = Mathf.Pow(half, Steepness);
            return np / (np + hp);
        }
    }
}
