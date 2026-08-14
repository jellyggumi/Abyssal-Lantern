using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// The arithmetic that makes a launcher look like it is aiming and then firing.
    ///
    /// Split out as a pure type for the usual reason (EditMode can check it with no scene), but
    /// also because the numbers here were chosen against sourced bands rather than by feel, and a
    /// pure type is where that reasoning survives:
    ///
    /// - A warning signal needs roughly 300ms to function as one; shorter foreperiods can produce
    ///   *slower* responses because processing has not completed when the stimulus arrives.
    /// - Fighting games put a readable, committed windup in the 250–370ms band (Guilty Gear
    ///   Strive: Gun Flame 16F/267ms, Fafnir 20F/333ms at 60fps).
    /// - The enemy beat here is 0.4s + 0.5s = 0.9s, and the 0.5s half is currently empty: the AI's
    ///   own comment calls it "enough of a pause to read as the enemy taking aim" while the aim is
    ///   computed *after* the wait. The intent was right and the order was wrong.
    ///
    /// So the windup fits the existing budget without spending another millisecond, which matters
    /// because the games that spend more are the games whose players cut it back — GameSpot faulted
    /// Worms Armageddon for "the length of time that it takes for such worms to complete their
    /// turns", and this project already reclaimed 2.1s of AI dead air and reinvested it in more
    /// turns. `.survey/siege-impact-vfx-and-attack-motion/`
    /// </summary>
    public static class LauncherFeedback
    {
        /// <summary>
        /// How long the aim windup runs.
        ///
        /// Three bounds close on this value at once, and it has to satisfy all of them: inside
        /// <c>SimpleAI</c>'s existing 0.5s pause (no new seconds), above the ~300ms foreperiod
        /// floor (below that a warning can read slower than no warning), and inside the
        /// 250–370ms band where fighting games place a committed heavy windup.
        ///
        /// First written as 0.42 — inside the pause and above the floor, but outside the band I
        /// had just cited. `LauncherFeedbackTests.Windup_SitsInTheReadableHeavyAttackBand` caught
        /// it. 0.36 is the widest value that satisfies all three, which is what a windup wants:
        /// as much time as the evidence allows, and not one frame past it.
        /// </summary>
        public const float WindupSeconds = 0.36f;

        /// <summary>
        /// Fire kick duration. Matches <see cref="CannonController"/>'s established recoil so the
        /// two launchers on the board share one physical vocabulary.
        /// </summary>
        public const float RecoilSeconds = 0.26f;

        /// <summary>Peak backward travel of the fire kick, in world units.</summary>
        public const float RecoilDistance = 0.34f;

        /// <summary>Extra scale at full draw — the launcher visibly loads before it throws.</summary>
        public const float WindupSquash = 0.12f;

        /// <summary>
        /// Alpha for a launcher whose side is NOT acting.
        ///
        /// Dimming the idle side rather than hiding it: the acting side is the most common
        /// legibility channel in the sample (8/12), it costs no new screen element, and WCAG 2.2
        /// SC 2.3.3 explicitly excludes "changes of color, blurring, or opacity which do not
        /// change the perceived size, shape, or position" from motion animation — so unlike a
        /// camera move it carries no nausea obligation and needs no opt-out.
        ///
        /// Hiding was the previous behaviour and it was the actual defect: with the player's
        /// launcher switched off and the enemy's never drawn at all, the enemy turn showed an
        /// empty muzzle on both sides.
        /// </summary>
        public const float IdleSideAlpha = 0.42f;

        /// <summary>Alpha for the side currently acting.</summary>
        public const float ActingSideAlpha = 1f;

        /// <summary>
        /// Backward offset of the fire kick, given time remaining on the recoil timer.
        ///
        /// Cubic decay, same as the cannon barrel: most of the travel lands in the first frames,
        /// which is what reads as a punch instead of a slide.
        /// </summary>
        public static float RecoilOffset(float timerRemaining)
        {
            if (timerRemaining <= 0f) return 0f;
            float t = Mathf.Clamp01(timerRemaining / RecoilSeconds);
            return RecoilDistance * (t * t * t);
        }

        /// <summary>
        /// Draw progress in 0..1 for a windup that has been running <paramref name="elapsed"/>
        /// seconds. Eased so the pose settles at full draw rather than arriving linearly — the
        /// extremes are what the eye reads (slow in / slow out), and a launcher that creeps
        /// uniformly has no extreme to read.
        /// </summary>
        public static float WindupProgress(float elapsed)
        {
            if (elapsed <= 0f) return 0f;
            float t = Mathf.Clamp01(elapsed / WindupSeconds);
            return t * t * (3f - 2f * t); // smoothstep
        }

        /// <summary>
        /// Uniform scale multiplier for the launcher at a given windup progress: it loads toward
        /// <see cref="WindupSquash"/> and snaps back once the shot leaves.
        /// </summary>
        public static float WindupScale(float progress) => 1f + WindupSquash * Mathf.Clamp01(progress);

        /// <summary>
        /// Alpha for one launcher. The acting side is solid, the waiting side is dimmed but
        /// present — presence is the point, since the complaint being answered is that neither
        /// muzzle showed anything during the enemy turn.
        /// </summary>
        public static float SideAlpha(bool thisSideIsActing) =>
            thisSideIsActing ? ActingSideAlpha : IdleSideAlpha;
    }
}
