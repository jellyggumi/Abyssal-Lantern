using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// One owner for every blink rate in the game, because two of them shipped over the seizure
    /// threshold and a third disagreed with its own comment by 2.28x.
    ///
    /// WCAG 2.2 SC 2.3.1 permits no more than three flashes per second. `design/graphics-needed.md`
    /// §D lists that ceiling among the prohibited techniques, and the reason it is on that list
    /// rather than in a style guide is the 1997 Pokémon broadcast: "Over 600 people, mostly children,
    /// were taken to hospitals", triggered by rapid full-screen flashing. It is the only layer of
    /// this investigation with recorded physical harm.
    ///
    /// What was found shipping (`_workspace/current/pm/legibility-retention-note.md`):
    /// - the barrel fuse ran at **9.00 flashes/s** in its worst one-second window, 3.00x the ceiling,
    ///   with 85% of its two-second telegraph above the line. (Its instantaneous derivative peaks at
    ///   11.50/s, but SC 2.3.1 counts flashes per second, so the window figure is the honest one —
    ///   two of us quoted the derivative before checking which the standard measures;
    /// - the buff-expiry blink ran at **3.501 Hz**, 1.17x the ceiling, while its own comment claimed
    ///   8 Hz — the code was the safer of the two and nobody had checked either.
    ///
    /// Rates live here as pure functions so `AccessibleBlinkTests` can assert the ceiling with no
    /// scene, no camera, and no frame loop. A rate authored inline in a coroutine is a rate nobody
    /// can test, which is how both of these survived.
    /// </summary>
    public static class AccessibleBlink
    {
        /// <summary>WCAG 2.2 SC 2.3.1 ceiling, in flashes per second.</summary>
        public const float MaxFlashesPerSecond = 3f;

        /// <summary>
        /// Working ceiling, held below the standard's limit.
        ///
        /// The margin exists because a flash count is measured on the presented frames, not on the
        /// intended curve: a frame-rate spike, a hitch, or a `Time.deltaTime` outlier can push an
        /// on-the-line rate over it. Sitting at exactly 3.0 would make compliance depend on frame
        /// pacing.
        /// </summary>
        public const float SafeFlashesPerSecond = 2.6f;

        /// <summary>
        /// Phase rate for a <see cref="Mathf.PingPong"/> blink held at the safe ceiling.
        ///
        /// PingPong with a period of 1 completes one light-dark pair per 2 units of phase, so a
        /// target of N flashes per second needs a phase rate of 2N.
        /// </summary>
        public const float SafePingPongPhaseRate = SafeFlashesPerSecond * 2f;

        /// <summary>
        /// Angular frequency for a <see cref="Mathf.Sin"/> blink held at the safe ceiling.
        ///
        /// One sine period is one flash, so N flashes per second is 2*pi*N radians per second. The
        /// buff-expiry blink used a literal 22f, which is 3.501 Hz — this returns 16.34.
        /// </summary>
        public static float SafeSineFrequency => SafeFlashesPerSecond * 2f * Mathf.PI;

        /// <summary>
        /// PingPong phase for the barrel fuse at elapsed time <paramref name="elapsedSeconds"/>.
        ///
        /// Returns the PHASE, not a rate, and that distinction is the whole point of this method
        /// existing. Two people got it wrong in a row: the flash rate is the derivative of the phase
        /// fed to <see cref="Mathf.PingPong"/>, so clamping a "phase rate" that then gets multiplied
        /// by <c>t</c> at the call site does not clamp anything. The first attempt at this fix
        /// clamped `3 + 5t` to 5.2 and still measured 3.00 flashes/s in the worst one-second window —
        /// exactly on the WCAG limit, with none of the margin
        /// <see cref="SafeFlashesPerSecond"/> exists to provide.
        ///
        /// The curve is <c>3t + 0.55t²</c>. Its derivative is <c>3 + 1.1t</c>, so the flash rate runs
        /// 1.50/s at ignition to 2.50/s at detonation over a two-second fuse — still monotonically
        /// accelerating, which is the readable part of a fuse, and 17% under the ceiling measured by
        /// counting real reversals rather than by trusting the algebra.
        ///
        /// Callers pass this straight to PingPong. There is no multiplication left to do, which is
        /// what makes the mistake unrepeatable.
        /// </summary>
        public static float FusePhase(float elapsedSeconds) =>
            3f * elapsedSeconds + 0.55f * elapsedSeconds * elapsedSeconds;

        /// <summary>
        /// Flashes per second a <see cref="Mathf.PingPong"/> blink produces at a constant phase rate.
        ///
        /// PingPong with period 1 delivers one light-dark pair per 2 units of phase. Only valid for a
        /// CONSTANT rate — an accelerating phase has to be measured by counting reversals, which is
        /// what <c>AccessibleBlinkTests</c> does and what this helper cannot do for you.
        /// </summary>
        public static float PingPongFlashRate(float constantPhaseRate) => constantPhaseRate * 0.5f;

        /// <summary>Flashes per second a <see cref="Mathf.Sin"/> blink produces at a given angular frequency.</summary>
        public static float SineFlashRate(float angularFrequency) => angularFrequency / (2f * Mathf.PI);
    }
}
