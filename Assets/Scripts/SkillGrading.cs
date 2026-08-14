using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// The scale this game did not have: a player's measured aiming skill, quantised into grades,
    /// and the handicap each grade is owed.
    ///
    /// WHY A SCALE AT ALL. Aim quality +0.01 moves the win rate 14.0 percentage points
    /// (53.0% -> 67.0%, `qa/evidence/g2/g2-remeasured-20260814.log`). That slope is not a defect —
    /// the European Go Federation's rating model puts one Go grade at +13.7pp, the same size. What
    /// Go has on top of it is a grade scale and a handicap; what this game has is the slope and
    /// nothing else. Survey: `.survey/siege-first-turn-fairness/`.
    ///
    /// The Go sources are explicit about three constraints, and each one shapes a decision here:
    ///
    /// - Competitive play gets NO handicap ("in tournaments, particularly when prize money is at
    ///   stake, no handicap will be given to the weaker player" — senseis.xmp.net/?RankAndHandicap,
    ///   community wiki). There is no versus mode today; if one is added, this must not follow it
    ///   silently. See <see cref="HandicapAimError"/>'s caller.
    /// - A LIGHT handicap beats a complete one: "a handicap system that is a little light provides
    ///   a reasonable environment for the rapidly-improving player" (same source). Traditional
    ///   handicap stones are, by that page's own arithmetic, an UNDER-compensation. So this pays
    ///   20% of a grade gap, not 100% — flattening the curve entirely would delete the skill it
    ///   measures.
    /// - Nobody has solved this: "the optimal amount and type of handicap able to create a fair
    ///   game in go is an ongoing area of research and study" (senseis.xmp.net/?Handicap). Komi
    ///   itself walked 0 -> 4.5 -> 5.5 -> 6.5 -> 7.5 over decades. So the constants below are a
    ///   first value with a measurement path attached (`SkillGradingTests`, V1-V5 in
    ///   `design/skill-grading-and-handicap.md`), not a settled answer.
    ///
    /// WHAT THIS IS NOT. It is not a comeback device. `LastStand` and `BalanceEventPlanner` read
    /// STATE mid-match (core HP) and hand out POWER; their value is proportional to the holder's
    /// skill, which steepens the curve rather than flattening it. This reads measured SKILL before
    /// the match and hands the weaker player the opponent's ERROR. Every shipped first-turn fix
    /// found in the survey acts on order, initial resources, or match structure — none buffs the
    /// trailing side mid-game, and the power-buff comebacks that do (Mario Kart, LoL bounties) are
    /// criticised for decoupling outcome from skill.
    /// </summary>
    public static class SkillGrading
    {
        /// <summary>
        /// Grades, coarsest first. Four rather than a continuous scale because the Go sources name
        /// rank SPACING as the real problem — "the spacing between ranks is likely to be a more
        /// serious issue than this one of first-play advantage" — so the boundaries have to be
        /// stated and defended rather than emerging from a formula.
        /// </summary>
        public enum Grade
        {
            /// <summary>Below the worst measured stage. Cannot find the aim space at all.</summary>
            Novice = 0,
            /// <summary>Around Stage3's measured 0.57.</summary>
            Apprentice = 1,
            /// <summary>Around Stage1's measured 0.73.</summary>
            Skilled = 2,
            /// <summary>At or above Stage2's measured 0.83. Receives no handicap.</summary>
            Elite = 3,
        }

        // Boundaries sit between the three measured hit rates rather than on round numbers, so a
        // player performing like a measured match lands in the grade that match implies.
        // Stage3 0.57, Stage1 0.73, Stage2 0.83 - qa/b1-measurement-findings.md.
        public const float ApprenticeFloor = 0.45f;
        public const float SkilledFloor = 0.62f;
        public const float EliteFloor = 0.78f;

        /// <summary>
        /// Shots before a grade is trusted. Below this the player has no grade and no handicap:
        /// a hit rate from two shots is noise, and handing out compensation on noise would swing
        /// the AI's accuracy for reasons the player cannot perceive.
        /// </summary>
        public const int MinimumShotsForGrade = 8;

        /// <summary>
        /// Ceiling on the whole handicap, and the value the per-grade step is derived FROM.
        ///
        /// 0.7 is 41% of the ramp's 1.7 span, so a Novice facing an Elite-tuned ramp still faces a
        /// ramp: at the ramp's tightest end (0.8) the widest handicap gives 1.5, still under its
        /// loosest (2.5). Without a ceiling the widest gap would erase the difficulty progression
        /// `DifficultyCurve` exists to provide.
        /// </summary>
        public const float MaximumHandicapAimError = 0.7f;

        /// <summary>
        /// Extra AI aim error per grade below Elite — the ceiling divided by the widest gap.
        ///
        /// DERIVED, not chosen, and the first version got this wrong. It was a flat 0.35 (20% of the
        /// ramp span), which put Novice at 3 x 0.35 = 1.05 and Apprentice at 2 x 0.35 = 0.70 — both
        /// clamped to the 0.7 ceiling, so the two weakest grades received an IDENTICAL handicap and
        /// a four-grade scale behaved as three. Measured in
        /// `qa/evidence/g2/handicap-sensitivity.log`. Dividing the ceiling by the widest gap instead
        /// keeps all four distinct by construction, and makes each step LIGHTER (13.7% of the span
        /// against 20.6%) — which is the direction the Go sources argue for: "a handicap system that
        /// is a little light provides a reasonable environment for the rapidly-improving player".
        ///
        /// THIS IS A DESIGN CHOICE, NOT A PREDICTION. The survey produced a closed form for the win
        /// rate — Phi((delta_skill - delta_handicap) / (sd * sqrt(2))) — and this value CANNOT be
        /// substituted into it. `SimpleAI.errorOffsetRange` is a world-space offset in metres
        /// (`SimpleAI.cs:53` draws `Random.Range(-r, r)` on two axes); `fixedAimQuality` is a 0..1
        /// damage multiplier. No conversion between them exists in code, and deriving one needs wall
        /// hitboxes, blast radii, and block placement — physics the simulator does not have.
        /// Inventing a factor would make the equation start lying. The conversion arrives by
        /// measurement: how many points of hit rate does this actually cost the AI
        /// (<see cref="TelemetrySink.AiHits"/> against <see cref="TelemetrySink.AiMeanAimError"/>).
        /// Until then this constant has a rationale but no predicted effect size.
        /// </summary>
        public const float AimErrorPerGrade = MaximumHandicapAimError / 3f;

        /// <summary>
        /// Hit rate -> grade. A hit is a shot that removed material from the OPPONENT's keep;
        /// see <see cref="TelemetrySink.NoteShotOutcome"/> for why that definition and not
        /// "damage dealt".
        /// </summary>
        public static Grade GradeForHitRate(float hitRate)
        {
            if (hitRate >= EliteFloor) return Grade.Elite;
            if (hitRate >= SkilledFloor) return Grade.Skilled;
            if (hitRate >= ApprenticeFloor) return Grade.Apprentice;
            return Grade.Novice;
        }

        /// <summary>
        /// The handicap a grade is owed, in AI aim-error units. Elite gets exactly zero — a strong
        /// player's win is not shaved by compensation, which is the same reason competitive Go
        /// gives no stones.
        /// </summary>
        public static float HandicapAimError(Grade grade)
        {
            int gapFromElite = (int)Grade.Elite - (int)grade;
            return Mathf.Min(gapFromElite * AimErrorPerGrade, MaximumHandicapAimError);
        }

        /// <summary>
        /// Sample-gated handicap: below <see cref="MinimumShotsForGrade"/> there is no measurement,
        /// so there is no handicap. Returning 0 rather than a default grade is deliberate — a
        /// beginner's first shots would otherwise be graded Novice on one miss and hand them an
        /// advantage they never earned and cannot see.
        /// </summary>
        public static float HandicapForSample(int shots, int hits)
        {
            if (shots < MinimumShotsForGrade) return 0f;
            if (shots <= 0) return 0f;
            return HandicapAimError(GradeForHitRate((float)hits / shots));
        }

        /// <summary>
        /// The composition rule, in one place: the ramp's own schedule PLUS a handicap. ADDED, never
        /// multiplied — a multiplier would deform the Hill curve `DifficultyCurve` was rewritten to
        /// produce (task #17) differently for each grade, so the difficulty SHAPE would become a
        /// function of the player's skill rather than of the turn.
        ///
        /// Shipped practice agrees with the distinction rather than the rule: Worms multiplies a
        /// SCALAR handicap (team health, +/-50%) but wind, which varies per turn, enters as an
        /// additive force. A schedule is not a scalar, so preserving its shape is what decides this.
        /// </summary>
        public static float Compose(float rampError, float handicap) => rampError + handicap;

        /// <summary>
        /// Sample-derived convenience over <see cref="Compose"/>. The GAME does not call this — it
        /// composes with <see cref="MatchHandicap.Current"/>, which was frozen at match start so the
        /// value cannot shift mid-match. This overload exists for the balance model and for tests
        /// that exercise the sample-to-error path end to end.
        /// </summary>
        public static float EffectiveAiAimError(float rampError, int shots, int hits)
            => Compose(rampError, HandicapForSample(shots, hits));
    }
}

namespace CastleBusters
{
    /// <summary>
    /// The grade a match is played under, frozen when it starts.
    ///
    /// The first cut of this read the session counters every AI turn, and that was wrong in a way
    /// the survey caught: a player crossing the 8-shot sample gate mid-match would see the AI
    /// suddenly start missing, and a player crossing a grade boundary would see it suddenly tighten.
    /// Neither has a cause the player can perceive. Worms Armageddon stores AI level as a single
    /// byte in the team file (worms2d.info/Team_file, values 0x01-0x05) authored BEFORE the match,
    /// and keeps cumulative win/loss/kill counts in the same record without ever feeding them back
    /// into that byte. Two stored things, no automatic link — which is exactly the separation this
    /// type provides.
    ///
    /// The default when the sample is short is <see cref="SkillGrading.Grade.Elite"/>, i.e. NO
    /// handicap, and that is a deliberate choice rather than a fallback. Defaulting to Novice would
    /// hand every player maximum compensation for their first eight shots and then yank it away at
    /// the gate — the same unexplainable tightening, on every first match. "If we have not measured
    /// it, we do not intervene" is the safer default, and it fails toward the competitive rule the
    /// Go sources state (no handicap) rather than away from it.
    /// </summary>
    public static class MatchHandicap
    {
        private static float frozenHandicap;
        private static bool frozen;

        /// <summary>The handicap in force for this match. Zero until <see cref="FreezeForMatch"/>
        /// runs, so a scene that never starts a match cannot accidentally hand one out.</summary>
        public static float Current => frozen ? frozenHandicap : 0f;

        /// <summary>True once a match has fixed its handicap. Exposed so a test can tell "no
        /// handicap because it was measured as zero" from "no handicap because nothing froze".</summary>
        public static bool IsFrozen => frozen;

        /// <summary>
        /// Called at match start with the session's accumulated sample. Runs exactly once per match;
        /// later shots change the NEXT match's handicap, never this one's.
        /// </summary>
        public static void FreezeForMatch(int shots, int hits)
        {
            frozenHandicap = SkillGrading.HandicapForSample(shots, hits);
            frozen = true;
        }

        /// <summary>Test/session reset. Not a game hook — a match always freezes explicitly.</summary>
        public static void Clear()
        {
            frozenHandicap = 0f;
            frozen = false;
        }
    }
}
