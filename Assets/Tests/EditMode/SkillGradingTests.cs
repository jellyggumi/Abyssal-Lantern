using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The verification path `design/skill-grading-and-handicap.md` §3.3 demanded for itself.
    ///
    /// The device is a skill grade plus a handicap: measured hit rate picks a grade, the grade
    /// owes the weaker player extra AI aim error. It exists because aim quality +0.01 moves the
    /// win rate 14.0 percentage points (53.0% -> 67.0%,
    /// `qa/evidence/g2/g2-remeasured-20260814.log`), which is a normal skill-unit size — one Go
    /// grade is +13.7pp — and Go's answer to a slope that size is a handicap, not a flatter slope.
    ///
    /// These tests exist because the precedent in this repo is a tuning constant that shipped with
    /// no way to re-measure it: `OpeningVolleyDamageScale = 0.5` over-corrects to 47.0% and nobody
    /// found out for a day and a half, because there was nothing to find out with. §3.3 quotes the
    /// primary source saying the optimal handicap is unsolved ("an ongoing area of research and
    /// study") and that komi itself walked 0 -> 4½ -> 5½ -> 6½ -> 7½ over decades. So every
    /// constant here is READ FROM THE TYPE, never restated: a retune moves these tests with it,
    /// and what they defend is the RELATIONS between the constants, which is what the design
    /// argued for and what a retune can silently break.
    ///
    /// The three hardcoded numbers are the exception, and deliberately so: 0.57 / 0.73 / 0.83 are
    /// PlayMode measurements (`qa/b1-measurement-findings.md`), not code constants. They are the
    /// evidence the boundaries were fitted to, so writing them as literals is the point — if a
    /// boundary edit stops honouring the matches that produced it, V1 says so.
    ///
    /// V4 from §6 is deliberately absent: it needs a handicap term inside `SiegeDuelSimulation`,
    /// which does not exist, and asserting a win rate against a simulator that has no AI-specific
    /// error model would be an assertion about nothing.
    /// </summary>
    [TestFixture]
    public sealed class SkillGradingTests
    {
        // ---- measured, not configured ----
        // B1 PlayMode hit rates, qa/b1-measurement-findings.md. Hardcoded ON PURPOSE: these are
        // observations of real matches, and the grade boundaries were fitted to them. If a
        // boundary moves off the evidence that justified it, V1 fails.
        private const float Stage3MeasuredHitRate = 0.57f;
        private const float Stage1MeasuredHitRate = 0.73f;
        private const float Stage2MeasuredHitRate = 0.83f;

        /// <summary>
        /// The production difficulty ramp, driven through the real property rather than a copy.
        ///
        /// `GameManager.CurrentAiErrorOffset` is <c>Mathf.Lerp(aiErrorStart, aiErrorEnd,
        /// DifficultyT)</c> over the real <see cref="DifficultyCurve"/>, and this probe reads it
        /// on a live component, so the ramp shape under test is the shipped one — a rewrite of the
        /// curve or of the lerp endpoints reaches these tests instead of drifting away from a
        /// mirrored formula. The host is created INACTIVE so `OnEnable` cannot install it as
        /// `GameManager.Instance`; `StageActorScaleTests` asserts the EditMode suite runs with no
        /// GameManager in the scene, and stealing that slot would break it.
        /// </summary>
        private GameObject rampHost;
        private GameManager ramp;
        private FieldInfo turnCountField;

        [SetUp]
        public void SetUp()
        {
            rampHost = new GameObject("SkillGrading_RampProbe")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            rampHost.SetActive(false);
            ramp = rampHost.AddComponent<GameManager>();

            turnCountField = typeof(GameManager).GetField(
                "turnCount", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(turnCountField,
                "precondition: GameManager.turnCount must still be the field DifficultyT reads");
        }

        [TearDown]
        public void TearDown()
        {
            if (rampHost != null) Object.DestroyImmediate(rampHost);
            rampHost = null;
            ramp = null;
        }

        /// <summary>Ramp-only AI aim error at a turn, straight out of the shipped property.</summary>
        private float RampErrorAtTurn(int turn)
        {
            turnCountField.SetValue(ramp, turn);
            return ramp.CurrentAiErrorOffset;
        }

        private float RampStart => ramp.aiErrorStart;
        private float RampEnd => ramp.aiErrorEnd;

        /// <summary>The width the design pays a percentage OF (§4.5): 2.5 -> 0.8 is 1.7.</summary>
        private float RampSpan => RampStart - RampEnd;

        /// <summary>
        /// Turns spanning a real siege, from the opening to well past the modelled length. Derived
        /// from the ramp the stage actually gets, so a longer match resamples rather than leaving
        /// these probes bunched in the opening third.
        /// </summary>
        private int[] SampleTurns()
        {
            int rampTurns = ramp.EffectiveDifficultyRampTurns;
            return new[]
            {
                1,
                Mathf.Max(2, rampTurns / 8),
                Mathf.Max(3, rampTurns / 4),
                Mathf.Max(4, rampTurns / 2),
                rampTurns,
                rampTurns * 3 / 2,
                rampTurns * 2,
            };
        }

        /// <summary>
        /// A shots/hits pair that lands in a given grade, derived from the boundary constants so a
        /// retune re-derives the sample instead of silently testing the neighbouring grade.
        /// </summary>
        private static (int shots, int hits) SampleFor(SkillGrading.Grade grade)
        {
            float rate = grade switch
            {
                SkillGrading.Grade.Novice => SkillGrading.ApprenticeFloor * 0.5f,
                SkillGrading.Grade.Apprentice =>
                    0.5f * (SkillGrading.ApprenticeFloor + SkillGrading.SkilledFloor),
                SkillGrading.Grade.Skilled =>
                    0.5f * (SkillGrading.SkilledFloor + SkillGrading.EliteFloor),
                _ => 0.5f * (SkillGrading.EliteFloor + 1f),
            };

            // 100 shots: comfortably over the sample gate, and fine enough that rounding cannot
            // walk a midpoint across a boundary the grades keep ~0.17 apart.
            int shots = 100;
            int hits = Mathf.RoundToInt(rate * shots);

            Assert.AreEqual(grade, SkillGrading.GradeForHitRate((float)hits / shots),
                $"fixture precondition: {hits}/{shots} was derived as a mid-band {grade} sample and "
                + "no longer grades as one. A boundary moved far enough that these samples must be "
                + "re-derived - the tests below would otherwise assert about the wrong grade");

            return (shots, hits);
        }

        // ------------------------------------------------------------------ V1

        /// <summary>
        /// V1. The boundaries have to be a scale over MEASURED play, not round numbers.
        ///
        /// This is the whole claim behind the grade table in §4.3: 0.45 / 0.62 / 0.78 sit BETWEEN
        /// the three PlayMode hit rates rather than on tidy tenths, so a player performing like a
        /// measured match lands in the grade that match implies. Lane C's judgement was that rank
        /// SPACING, not the handicap value, is the real problem ("the spacing between ranks is
        /// likely to be a more serious issue than this one of first-play advantage"), which is why
        /// the boundaries are defended against evidence here instead of being left to a formula.
        ///
        /// Written with the measurements as literals on purpose: if someone rounds the boundaries
        /// off to 0.5/0.65/0.8 the arithmetic still looks reasonable, and only the real matches
        /// reveal that Stage3's play now grades Novice.
        /// </summary>
        [Test]
        public void TheGradeBoundariesAreAScaleOverTheMeasuredHitRates()
        {
            var cases = new[]
            {
                ("Stage3", Stage3MeasuredHitRate, SkillGrading.Grade.Apprentice),
                ("Stage1", Stage1MeasuredHitRate, SkillGrading.Grade.Skilled),
                ("Stage2", Stage2MeasuredHitRate, SkillGrading.Grade.Elite),
            };

            foreach (var (stage, measured, expected) in cases)
            {
                var actual = SkillGrading.GradeForHitRate(measured);
                Assert.AreEqual(expected, actual,
                    $"{stage}'s measured hit rate {measured:F2} grades as {actual}, not {expected}. "
                    + $"The boundaries ({SkillGrading.ApprenticeFloor:F2}/"
                    + $"{SkillGrading.SkilledFloor:F2}/{SkillGrading.EliteFloor:F2}) were fitted to "
                    + "these three PlayMode measurements (qa/b1-measurement-findings.md); a scale "
                    + "that no longer sorts real play is a scale over nothing");
            }
        }

        /// <summary>
        /// The same three measurements must land in three DIFFERENT grades, which is the property
        /// that makes the scale a scale. A boundary set that graded all three the same would pass
        /// nothing above except by accident of which grade it collapsed to, and it would mean the
        /// device cannot tell the weakest measured play from the strongest.
        /// </summary>
        [Test]
        public void TheMeasuredHitRatesLandInThreeDistinctGrades()
        {
            var s3 = SkillGrading.GradeForHitRate(Stage3MeasuredHitRate);
            var s1 = SkillGrading.GradeForHitRate(Stage1MeasuredHitRate);
            var s2 = SkillGrading.GradeForHitRate(Stage2MeasuredHitRate);

            Assert.AreNotEqual(s3, s1,
                $"Stage3 ({Stage3MeasuredHitRate:F2}) and Stage1 ({Stage1MeasuredHitRate:F2}) both "
                + $"grade {s3} - a 0.16 hit-rate difference in real play must cross a boundary or "
                + "the scale cannot resolve the skill it was built to measure");
            Assert.AreNotEqual(s1, s2,
                $"Stage1 ({Stage1MeasuredHitRate:F2}) and Stage2 ({Stage2MeasuredHitRate:F2}) both "
                + $"grade {s1} - the strongest measured play must not share a grade with the middle");
            Assert.Less((int)s3, (int)s1, "grades must rise with measured hit rate");
            Assert.Less((int)s1, (int)s2, "grades must rise with measured hit rate");
        }

        // ------------------------------------------------------------------ V2

        /// <summary>
        /// V2. The handicap must not deform the difficulty ramp, only offset it.
        ///
        /// §4.4 is explicit that the grade term is ADDED and never multiplied, and the reason is
        /// task #17: the ramp was rewritten from smoothstep to a Hill curve precisely so difficulty
        /// keeps tightening instead of flattening after the ramp turn, and that shape is pinned by
        /// `DifficultyRampShapeTests`. A multiplied handicap would scale the ramp's every increment
        /// by the player's grade, so the difficulty SHAPE would become a function of who is playing
        /// rather than of the turn — the back half would flatten for exactly the weak players the
        /// curve rewrite was meant to keep engaged.
        ///
        /// Two things are asserted, and the second is the one with teeth. Every grade's curve still
        /// falls turn over turn; and the turn-to-turn DELTA is identical across grades, i.e. the
        /// curves are parallel translates of one ramp. A multiplicative implementation passes the
        /// first and fails the second.
        ///
        /// The ramp is read through the shipped `GameManager.CurrentAiErrorOffset`, so this is the
        /// real Hill curve and the real endpoints, not a formula copied into a test.
        /// </summary>
        [Test]
        public void EveryGradesErrorCurveIsTheSameRampTranslated()
        {
            int[] turns = SampleTurns();
            var grades = (SkillGrading.Grade[])System.Enum.GetValues(typeof(SkillGrading.Grade));

            // Precondition: the thing being offset must itself be a descending ramp, or "the
            // handicap preserves the shape" would be a claim about a shape that is not there.
            for (int i = 1; i < turns.Length; i++)
            {
                Assert.Less(RampErrorAtTurn(turns[i]), RampErrorAtTurn(turns[i - 1]),
                    $"precondition: AI aim error must fall from turn {turns[i - 1]} to {turns[i]} "
                    + $"({RampErrorAtTurn(turns[i - 1]):F3} -> {RampErrorAtTurn(turns[i]):F3}). The "
                    + "difficulty ramp is what the handicap offsets; if it is not descending, "
                    + "DifficultyRampShapeTests is the test that has something to say");
            }

            foreach (var grade in grades)
            {
                var (shots, hits) = SampleFor(grade);

                for (int i = 1; i < turns.Length; i++)
                {
                    float previous = SkillGrading.EffectiveAiAimError(
                        RampErrorAtTurn(turns[i - 1]), shots, hits);
                    float current = SkillGrading.EffectiveAiAimError(
                        RampErrorAtTurn(turns[i]), shots, hits);

                    Assert.Less(current, previous,
                        $"{grade}: effective AI aim error rises from turn {turns[i - 1]} to "
                        + $"{turns[i]} ({previous:F3} -> {current:F3}). The handicap has inverted "
                        + "the ramp for this grade, so a match gets EASIER as it runs - the flat "
                        + "back half task #17 removed, made worse");

                    // The shape claim. Same increment for every grade => parallel translates.
                    float rampDelta = RampErrorAtTurn(turns[i - 1]) - RampErrorAtTurn(turns[i]);
                    Assert.AreEqual(rampDelta, previous - current, 1e-4f,
                        $"{grade}: turn {turns[i - 1]}->{turns[i]} tightens by "
                        + $"{previous - current:F4} against the ramp's own {rampDelta:F4}. The "
                        + "handicap is scaling the ramp's increments instead of offsetting them, "
                        + "which makes the difficulty SHAPE a function of the player's grade - "
                        + "§4.4 forbids exactly this, and it is why the term is added not multiplied");
                }
            }
        }

        /// <summary>
        /// The offset itself: at every sampled turn, effective error minus ramp error is the grade's
        /// handicap and nothing else. The parallel-translate test above proves the increments match;
        /// this pins the constant they are translated BY, so a handicap that drifted with the turn
        /// (a ramp-scaled or turn-decayed handicap) is caught even where the increments survive.
        ///
        /// Worth defending separately because a turn-dependent handicap is a plausible "improvement"
        /// — fade the compensation as the match runs — and it would quietly make the compensation a
        /// function of match length, which is per-stage (`EffectiveDifficultyRampTurns`). A Novice
        /// would then get a different handicap on Stage3 than on Stage1 for the same measured skill.
        /// </summary>
        [Test]
        public void TheHandicapIsTheSameOffsetAtEveryTurn()
        {
            foreach (var grade in (SkillGrading.Grade[])System.Enum.GetValues(typeof(SkillGrading.Grade)))
            {
                var (shots, hits) = SampleFor(grade);
                float expected = SkillGrading.HandicapAimError(grade);

                foreach (int turn in SampleTurns())
                {
                    float rampError = RampErrorAtTurn(turn);
                    float offset = SkillGrading.EffectiveAiAimError(rampError, shots, hits) - rampError;

                    Assert.AreEqual(expected, offset, 1e-4f,
                        $"{grade} at turn {turn}: the handicap arrived as {offset:F4} against the "
                        + $"{expected:F4} the grade is owed. A handicap that varies with the turn "
                        + "also varies with match length, which is per-stage - the same player "
                        + "would be compensated differently on Stage1 and Stage3");
                }
            }
        }

        // ------------------------------------------------------------------ V3

        /// <summary>
        /// V3. The handicap must not cancel the ramp.
        ///
        /// The turn-independent form of the claim, and the one that actually binds: the ramp's
        /// tightest error plus the largest handicap must still be tighter than the ramp's loosest
        /// error. Break it and the whole ramp is inside the handicap — a graded player's endgame AI
        /// aims worse than an ungraded player's opening AI, so `DifficultyCurve` is decoration and
        /// the match has no difficulty progression at all. §4.5 chose the ceiling at 41% of the span
        /// for exactly this reason.
        ///
        /// Note what is NOT claimed: a Novice's OPENING error does exceed the ramp start, because
        /// the handicap is added on top. That is the device working as designed — the AI misses
        /// more. V3 is about whether the ramp's own travel survives the offset, and that is a
        /// statement about the endpoints.
        /// </summary>
        [Test]
        public void TheWidestHandicapStillLeavesTheRampItsTravel()
        {
            float tightestWithMaxHandicap = RampEnd + SkillGrading.MaximumHandicapAimError;

            Assert.Less(tightestWithMaxHandicap, RampStart,
                $"the tightest handicapped error is {tightestWithMaxHandicap:F2} "
                + $"({RampEnd:F2} + {SkillGrading.MaximumHandicapAimError:F2}) against a ramp that "
                + $"starts at {RampStart:F2}. The handicap now spans the entire ramp: a compensated "
                + "player's endgame is looser than an uncompensated player's opening, so there is "
                + "no difficulty progression left to ramp. Either the ceiling comes down or "
                + "DifficultyCurve stops meaning anything");

            // And concretely, on the real curve: the endgame a Novice actually plays must be
            // tighter than the opening the ramp hands everyone.
            int rampTurns = ramp.EffectiveDifficultyRampTurns;
            var (shots, hits) = SampleFor(SkillGrading.Grade.Novice);
            float noviceEndgame = SkillGrading.EffectiveAiAimError(
                RampErrorAtTurn(rampTurns), shots, hits);
            float openingRamp = RampErrorAtTurn(1);

            Assert.Less(noviceEndgame, openingRamp,
                $"a Novice's error at the modelled last turn ({rampTurns}) is {noviceEndgame:F3}, "
                + $"looser than the turn-1 ramp's {openingRamp:F3}. The weakest player experiences "
                + "the match getting easier from start to finish, which is the opposite of a ramp");
        }

        // ------------------------------------------------------------------ V5

        /// <summary>
        /// V5, first half. Elite is owed exactly zero.
        ///
        /// §3.1's primary source is unambiguous — "in tournaments, particularly when prize money is
        /// at stake, no handicap will be given to the weaker player" — and §4.4 applies the same
        /// logic upward: a strong player's win must not be shaved by compensation. Anything above
        /// zero here means the best measured play in the game (Stage2's 0.83) is being quietly
        /// handed a weakened AI, which is both unearned and invisible.
        ///
        /// Exact zero rather than a tolerance, because that is the contract: the gap from Elite to
        /// Elite is 0 grades, and 0 x anything is exactly 0.0f in IEEE-754. A tolerance here would
        /// accept a small nonzero handicap, which is the defect.
        /// </summary>
        [Test]
        public void EliteIsOwedExactlyNothing()
        {
            Assert.That(SkillGrading.HandicapAimError(SkillGrading.Grade.Elite), Is.EqualTo(0f),
                "Elite must receive exactly zero handicap - §3.1's competitive-play constraint "
                + "applied upward: compensation that shaves a strong player's win is the thing "
                + "tournament Go refuses to do");

            // Through the sampled path too, including flawless play.
            var (shots, hits) = SampleFor(SkillGrading.Grade.Elite);
            Assert.That(SkillGrading.HandicapForSample(shots, hits), Is.EqualTo(0f),
                $"{hits}/{shots} grades Elite, so the sampled path must also hand back zero");
            Assert.That(SkillGrading.HandicapForSample(shots, shots), Is.EqualTo(0f),
                "a player who hits with every shot must not be compensated at all");

            // And the ramp must come through completely untouched for them.
            foreach (int turn in SampleTurns())
            {
                float rampError = RampErrorAtTurn(turn);
                Assert.That(SkillGrading.EffectiveAiAimError(rampError, shots, hits),
                    Is.EqualTo(rampError).Within(1e-6f),
                    $"turn {turn}: an Elite player must face the unmodified ramp");
            }
        }

        /// <summary>
        /// V5, second half. A player with no measurement has no grade and no handicap.
        ///
        /// The gate is the difference between measuring skill and reacting to noise. Without it a
        /// beginner's FIRST miss reads as a 0/1 hit rate, grades Novice, and buys the widest
        /// handicap in the table — an advantage they never earned, cannot perceive, and which
        /// evaporates the moment their sample grows. §4.2 already learned this lesson the expensive
        /// way at the measurement layer; this is the same lesson at the consumption layer.
        ///
        /// Both directions are asserted. One-sided, this test would pass against a function that
        /// returns 0 forever.
        /// </summary>
        [Test]
        public void AnUnmeasuredPlayerIsNeitherPunishedNorPaid()
        {
            int gate = SkillGrading.MinimumShotsForGrade;

            // Below the gate: silent, regardless of what the thin sample looks like.
            for (int shots = 0; shots < gate; shots++)
            {
                for (int hits = 0; hits <= shots; hits++)
                {
                    Assert.That(SkillGrading.HandicapForSample(shots, hits), Is.EqualTo(0f),
                        $"{hits}/{shots} is below the {gate}-shot gate and must buy no handicap. "
                        + "Grading a sample this thin hands a beginner compensation off their first "
                        + "miss - an advantage they did not earn and cannot see, which disappears "
                        + "again as they play");
                }
            }

            // At the gate: the measurement starts counting. Without this the gate could be a
            // permanent off switch and every assertion above would still pass.
            float atGate = SkillGrading.HandicapForSample(gate, 0);
            Assert.Greater(atGate, 0f,
                $"0/{gate} is a measured Novice and must be compensated; the handicap came back "
                + $"{atGate:F3}. If the gate never opens, the whole device is inert");
            Assert.AreEqual(SkillGrading.HandicapAimError(SkillGrading.Grade.Novice), atGate, 1e-4f,
                "once the sample is trusted, the sampled path must agree with the grade's own table");

            // A shot count is not a licence: crossing the gate with strong play still pays nothing.
            Assert.That(SkillGrading.HandicapForSample(gate, gate), Is.EqualTo(0f),
                $"{gate}/{gate} crosses the gate as Elite, so the handicap must stay at zero - the "
                + "gate opens a measurement, it does not grant compensation");
        }

        // ------------------------------------------------- contract, beyond the V list

        /// <summary>
        /// Getting better must never buy a bigger handicap.
        ///
        /// This is the property that keeps the device from paying for failure. If the handicap is
        /// not monotonically non-increasing in hit rate, there exists a band where improving your
        /// aim loosens the AI's — a rewarded regression, and worse, one the player can feel without
        /// being able to name. It is also the only invariant here that survives ANY boundary
        /// retune, so it is the right shape to state as a sweep rather than as cases.
        ///
        /// The strictness check at the end is load-bearing: non-increasing alone is satisfied by a
        /// function that returns 0 everywhere, which is the device switched off.
        /// </summary>
        [Test]
        public void ImprovingYourAimNeverBuysMoreCompensation()
        {
            const int steps = 200;
            float previous = SkillGrading.HandicapAimError(SkillGrading.Grade.Novice);
            float previousRate = 0f;

            for (int i = 0; i <= steps; i++)
            {
                float rate = i / (float)steps;
                float handicap = SkillGrading.HandicapAimError(SkillGrading.GradeForHitRate(rate));

                Assert.LessOrEqual(handicap, previous + 1e-6f,
                    $"hit rate {previousRate:F3} -> {rate:F3} raised the handicap "
                    + $"{previous:F3} -> {handicap:F3}. Improving aim must never loosen the AI's: "
                    + "that is a band where playing worse is rewarded, and the player would feel "
                    + "it without being able to name it");

                previous = handicap;
                previousRate = rate;
            }

            Assert.Greater(SkillGrading.HandicapAimError(SkillGrading.GradeForHitRate(0f)),
                SkillGrading.HandicapAimError(SkillGrading.GradeForHitRate(1f)),
                "the sweep must actually descend - a handicap that is flat across the whole hit-rate "
                + "range is the device turned off, and every non-increasing assertion above would "
                + "still pass");
        }

        /// <summary>
        /// The boundaries are inclusive upward, and that has to be pinned rather than inferred.
        ///
        /// `GradeForHitRate` compares with `>=`, so a hit rate landing exactly on a floor takes the
        /// HIGHER grade. Left unstated, a later edit reads the table in §4.3 ("0.45 ~ 0.62") as
        /// exclusive at the top, flips a comparison, and every player sitting precisely on a
        /// boundary silently changes grade — and with it their compensation. Boundary direction is
        /// cheap to fix and expensive to notice, which is exactly what a pin is for.
        /// </summary>
        [Test]
        public void ABoundaryHitRateTakesTheHigherGrade()
        {
            // 0.001 is far inside the ~0.16 band width, so "just below" is unambiguous without
            // reasoning about float ULPs.
            const float justBelow = 0.001f;

            var boundaries = new[]
            {
                (SkillGrading.ApprenticeFloor, SkillGrading.Grade.Apprentice, SkillGrading.Grade.Novice),
                (SkillGrading.SkilledFloor, SkillGrading.Grade.Skilled, SkillGrading.Grade.Apprentice),
                (SkillGrading.EliteFloor, SkillGrading.Grade.Elite, SkillGrading.Grade.Skilled),
            };

            foreach (var (floor, atFloor, below) in boundaries)
            {
                Assert.AreEqual(atFloor, SkillGrading.GradeForHitRate(floor),
                    $"a hit rate of exactly {floor:F2} must grade {atFloor} - the floors are "
                    + "inclusive, and a player sitting precisely on one must not change grade "
                    + "because a comparison was read as exclusive");
                Assert.AreEqual(below, SkillGrading.GradeForHitRate(floor - justBelow),
                    $"{floor - justBelow:F3} is below the {floor:F2} floor and must grade {below}");
            }
        }

        /// <summary>
        /// The scale must stay a scale: four grades, four distinct handicaps.
        ///
        /// THIS TEST REPLACES A WRONG ONE I WROTE, and the mistake is the reason to keep the story.
        /// The original asserted "the ceiling is two grades' worth AND the widest gap reaches it",
        /// against a flat `AimErrorPerGrade = 0.35` with a 0.7 ceiling. Every assertion passed. The
        /// ceiling did bind — and its binding WAS the defect: Novice wanted 3 x 0.35 = 1.05 and
        /// Apprentice wanted 2 x 0.35 = 0.70, so both clamped to 0.70 and the two weakest grades
        /// received an IDENTICAL handicap. A four-grade scale behaved as three, and the grade a
        /// struggling player was most likely to hold was the one that had been silently merged.
        /// Measured, not reasoned: `qa/evidence/g2/handicap-sensitivity.log` prints
        /// `Novice handicap 0.70` and `Apprentice handicap 0.70` on consecutive lines.
        ///
        /// `design/skill-grading-and-handicap.md` §"첫 시도가 눈금을 하나 잃었다" states the lesson
        /// against my test by name: it asserted whether the ceiling was reached, the ceiling was
        /// reached, so it passed — what it missed was that two grades became indistinguishable AS A
        /// RESULT. "The ceiling works" and "the scale is a scale" are different contracts and only
        /// the first one existed. So this asserts the second, which is the one a player can feel.
        ///
        /// Deliberately NOT asserting `ceiling / step == 3`: the step is now DEFINED as
        /// `MaximumHandicapAimError / 3f`, so that ratio cannot fail and would be a tautology
        /// dressed as a check. What can fail is the coupling below — the divisor is a literal `3`
        /// while the widest gap is a property of the `Grade` enum, and those are independent facts.
        /// Add a fifth grade and the widest gap becomes 4: the new bottom grade wants
        /// 4 x (0.7/3) = 0.933, clamps to 0.7, and collides with its neighbour exactly as before.
        /// That is the same defect returning through a different door, and it is what this catches.
        /// </summary>
        [Test]
        public void EveryGradeReceivesADistinctHandicap()
        {
            var grades = (SkillGrading.Grade[])System.Enum.GetValues(typeof(SkillGrading.Grade));

            // The property whose absence was the bug. Pairwise, so the failure names both culprits.
            for (int i = 0; i < grades.Length; i++)
            {
                for (int j = i + 1; j < grades.Length; j++)
                {
                    float a = SkillGrading.HandicapAimError(grades[i]);
                    float b = SkillGrading.HandicapAimError(grades[j]);
                    Assert.AreNotEqual(a, b,
                        $"{grades[i]} and {grades[j]} both receive a handicap of {a:F3}, so the scale "
                        + $"has {grades.Length} grades but fewer than {grades.Length} outcomes. This "
                        + "is the first version's defect verbatim (handicap-sensitivity.log lines "
                        + "17-18): the grades a struggling player actually holds are the ones that "
                        + "merge, so the measurement is finer than the compensation it drives");
                }
            }

            // Why distinctness holds by construction: the step is the ceiling divided by the REAL
            // widest gap. The divisor in the source is a literal; this is what ties it to the enum.
            int widestGap = (int)SkillGrading.Grade.Elite - (int)grades[0];
            Assert.AreEqual(SkillGrading.MaximumHandicapAimError,
                widestGap * SkillGrading.AimErrorPerGrade, 1e-4f,
                $"the widest gap ({widestGap} grades) x the per-grade step "
                + $"({SkillGrading.AimErrorPerGrade:F4}) is "
                + $"{widestGap * SkillGrading.AimErrorPerGrade:F4}, not the ceiling "
                + $"{SkillGrading.MaximumHandicapAimError:F4}. The step must be the ceiling divided "
                + "by the widest gap the enum actually has - if a grade was added, re-derive the "
                + "step, or the bottom grades clamp into each other again");
        }

        /// <summary>
        /// The ceiling is not a bound the grades sit under — it is a value exactly one grade is PAID.
        ///
        /// `EveryGradeReceivesADistinctHandicap` above proves the constants are consistent with each
        /// other (`widestGap x step == cap`), but every figure in that assertion is a `const`: it
        /// never calls <see cref="SkillGrading.HandicapAimError"/>. So it pins the ARITHMETIC and
        /// leaves the FUNCTION free, and the two can part company without anything going red.
        ///
        /// The mutation that proves the gap, run rather than imagined — multiply the gap term by
        /// 0.5f inside `HandicapAimError` and the whole fixture stays green. Novice is handed 0.350
        /// where §4.5 owes it 0.700; the pairwise test passes because 0.350/0.233/0.117/0.000 are
        /// still four distinct values; the hit-rate sweep passes because the ladder still descends;
        /// `EliteIsOwedExactlyNothing` passes because 0 x anything is still 0; the constants
        /// assertion passes because no constant moved. Every stated contract holds and the weakest
        /// player quietly receives half the compensation the design argued for. Nobody would find
        /// out — which is precisely the failure `OpeningVolleyDamageScale` already cost this repo a
        /// day and a half to learn (see this fixture's own summary).
        ///
        /// Exact equality, not `>=` or a band, and the exactness is the contract. `AimErrorPerGrade`
        /// is DEFINED as `cap / 3`, so the widest gap lands ON the ceiling rather than over it: the
        /// clamp in `HandicapAimError` is calibrated to be inert, and inert is the whole repair.
        /// Under the first version it was load-bearing, and it doing the work WAS the damage —
        /// 1.05 and 0.70 both truncated to 0.70 and the four-grade scale lost a rung
        /// (`qa/evidence/g2/handicap-sensitivity.log` lines 17-18).
        ///
        /// The second half is what keeps "reachable" from becoming "shared". A ceiling reached by
        /// two grades is not a ceiling, it is the original defect: so every grade above the weakest
        /// must be STRICTLY below it. Distinctness says no two grades collide anywhere; this says
        /// where the collision would land first, and that the ladder's top rung is occupied once.
        /// </summary>
        [Test]
        public void OnlyTheWeakestGradeIsPaidTheEntireCeiling()
        {
            var grades = (SkillGrading.Grade[])System.Enum.GetValues(typeof(SkillGrading.Grade));
            var weakest = grades[0];

            float paid = SkillGrading.HandicapAimError(weakest);
            Assert.AreEqual(SkillGrading.MaximumHandicapAimError, paid, 1e-4f,
                $"{weakest} is the widest gap from Elite, so it must be paid the ceiling "
                + $"{SkillGrading.MaximumHandicapAimError:F4} exactly - it received {paid:F4}. "
                + $"The step is defined as the ceiling over {(int)SkillGrading.Grade.Elite - (int)weakest} "
                + "grades precisely so this lands ON the ceiling and the clamp never truncates. Off "
                + "the ceiling in either direction is a real defect: BELOW means the weakest player "
                + "is paid less than §4.5 owes them while every other assertion here still passes, "
                + "ABOVE means the clamp is load-bearing again and the bottom grades merge");

            foreach (var grade in grades)
            {
                if (grade == weakest) continue;

                float handicap = SkillGrading.HandicapAimError(grade);
                Assert.Less(handicap, SkillGrading.MaximumHandicapAimError,
                    $"{grade} also receives the full ceiling ({handicap:F4}), so {weakest} and "
                    + $"{grade} are indistinguishable at the top of the ladder. A ceiling two grades "
                    + "reach is not a ceiling - it is the first version's defect returning, where "
                    + "the two weakest grades clamped onto one value and the scale lost a rung");
            }
        }

        /// <summary>
        /// The lightness the design cited a source for, held to arithmetic.
        ///
        /// §3.2 does not treat "light" as taste; it quotes it — "a handicap system that is a little
        /// light provides a reasonable environment for the rapidly-improving player" — and notes the
        /// same page calls traditional handicap stones an UNDER-compensation by their own arithmetic.
        /// §4.5 turns that into arithmetic: a grade pays the ceiling divided by the widest grade
        /// gap, which against the ramp's 1.7 span is 13.7% — not the 100% that repaying the gap in
        /// full would cost, because that deletes the skill difference the scale exists to measure.
        /// The first version paid a flat 0.35 (20.6%) and that is the version which lost a rung, so
        /// the repair moved the compensation TOWARD the source §3.2 quotes rather than away from it:
        /// keeping four grades distinct and making each step lighter were the same edit.
        ///
        /// No literal percentage is asserted below, deliberately. This docstring said "20%" for a
        /// while after the constant became `cap / 3`, and so did the failure message — prose citing
        /// a number that the code derives goes stale silently, which is the same class of drift as
        /// the ramp reading a keep that did not exist (`StageProgressionShapeTests`). The assertions
        /// compute the live figure and print it; the design's reasoning is what they name.
        ///
        /// Both the step and the ceiling are held under half the span, since the design's phrasing
        /// is "lighter than half" and the ceiling is the value a real Novice actually receives.
        /// Every figure is computed from the constants and the shipped ramp endpoints, so a retune
        /// of either is measured against the argument rather than against a copied number.
        /// </summary>
        [Test]
        public void OneGradeOfCompensationStaysLighterThanHalfTheRamp()
        {
            float half = RampSpan * 0.5f;

            Assert.Less(SkillGrading.AimErrorPerGrade, half,
                $"one grade pays {SkillGrading.AimErrorPerGrade:F3} of a {RampSpan:F2} ramp span "
                + $"({SkillGrading.AimErrorPerGrade / RampSpan:P1}), which is at or past half. §3.2 "
                + "cites a primary source for a LIGHT handicap and §4.5 derives the step as the "
                + "ceiling over the widest grade gap; paying a grade gap in full flattens the skill "
                + "difference the scale measures");

            Assert.Less(SkillGrading.MaximumHandicapAimError, half,
                $"the ceiling is {SkillGrading.MaximumHandicapAimError:F2} of a {RampSpan:F2} span "
                + $"({SkillGrading.MaximumHandicapAimError / RampSpan:P1}). This is what a real "
                + "Novice receives, so it is the figure the lightness claim has to hold for - past "
                + "half the span, compensation stops being light in any sense §3.2 would recognise");

            // Lightness is not nothing: a step of zero would satisfy both bounds and mean the
            // scale grades players it never compensates.
            Assert.Greater(SkillGrading.AimErrorPerGrade, 0f,
                "a grade gap must be worth something, or the grades are a readout with no device "
                + "attached");
        }
    }
}
