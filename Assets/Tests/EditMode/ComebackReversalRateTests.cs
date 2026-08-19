using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the G5 comeback-reversal instrument.
    ///
    /// The threshold — "comeback instant-reversal probability ≤30% per activation" — sat
    /// unmeasurable until 2026-08-19 because neither route to it existed: `SiegeDuelSimulation` has
    /// no `LastStand` reference at all, and `Telemetry.EventKind` had no comeback event. The gate
    /// review (`production/gate-reviews/stage2-g5-verdict.md`) recorded that as FIX rather than
    /// waiving it, on the grounds that a definable measurement must not be waived.
    ///
    /// What the instrument must NOT do is read a silent failure as a good score. A rate of 0 with no
    /// data looks like a perfect pass, and this suite exists mostly to make that impossible.
    /// </summary>
    public sealed class ComebackReversalRateTests
    {
        [SetUp]
        [TearDown]
        public void ClearRing() => Telemetry.Clear();

        /// <summary>
        /// Without this, an instrumentation failure reads as 0% reversals — the best possible score
        /// on a threshold that asks for ≤30%. `PlayerWinRate` already carries this contract and its
        /// comment gives the reason; the comeback rate inherits it because the failure mode is the
        /// same shape and worse: nobody notices a gate that passes.
        /// </summary>
        [Test]
        public void WithNoActivations_TheRateIsNegativeRatherThanZero()
        {
            Assert.AreEqual(-1f, Telemetry.ComebackReversalRate(), 0.0001f,
                "an empty ring must report -1 ('no data'), because 0 would be indistinguishable "
                + "from 'no activation could ever reverse' — which is a PASS on this gate");

            var (player, ai) = Telemetry.ComebackActivations();
            Assert.AreEqual(0, player, "no activations recorded means no player activations");
            Assert.AreEqual(0, ai, "and no AI activations");
        }

        /// <summary>
        /// The cap is the whole reason this threshold is about the FOE's core. 140 against a 150 max
        /// means a pristine core survives any single buffed hit, so an activation against full health
        /// is never a reversal — and one against a core already inside the cap always is.
        /// </summary>
        [Test]
        public void AReversalIsDecidedByTheFoesCoreAgainstTheCapNotByOurOwn()
        {
            float max = 150f;
            float danger = max * LastStand.DangerHpFraction;

            // Activation against a pristine foe: the cap cannot finish it.
            Telemetry.Comeback(byPlayer: true, ownCoreHp: danger, ownCoreMax: max,
                foeCoreHp: max, foeCoreMax: max);
            Assert.AreEqual(0f, Telemetry.ComebackReversalRate(), 0.0001f,
                $"a foe at full {max} is above the {LastStand.SingleHitDamageCap} cap, so this "
                + "activation cannot reverse the match — the cap exists to guarantee exactly that");

            // Same own-core state, foe already inside the cap: now it is a reversal.
            Telemetry.Comeback(byPlayer: true, ownCoreHp: danger, ownCoreMax: max,
                foeCoreHp: LastStand.SingleHitDamageCap, foeCoreMax: max);
            Assert.AreEqual(0.5f, Telemetry.ComebackReversalRate(), 0.0001f,
                "one of two activations could finish the foe, so the rate is 50% — and the own-core "
                + "value was identical in both, which is the point: the cap is compared against the "
                + "FOE's remaining core, not against the activation condition");
        }

        /// <summary>
        /// The boundary is inclusive, matching `IsDanger`'s boundary handling, because a foe at
        /// exactly the cap is exactly reachable. Asserted rather than assumed: an off-by-one here
        /// moves the measured rate, and this threshold has 30 percentage points of room total.
        /// </summary>
        [Test]
        public void AFoeAtExactlyTheCapCounts()
        {
            float max = 150f;
            Telemetry.Comeback(true, max * LastStand.DangerHpFraction, max, LastStand.SingleHitDamageCap, max);
            Assert.AreEqual(1f, Telemetry.ComebackReversalRate(), 0.0001f,
                $"a foe at exactly {LastStand.SingleHitDamageCap} is removable by one capped hit");

            Telemetry.Clear();
            Telemetry.Comeback(true, max * LastStand.DangerHpFraction, max, LastStand.SingleHitDamageCap + 1f, max);
            Assert.AreEqual(0f, Telemetry.ComebackReversalRate(), 0.0001f,
                "one point above the cap survives, so it is not a reversal");
        }

        /// <summary>
        /// The two sides are counted apart because they behave apart:
        /// `ComebackAsymmetryTests.ThePlayerHoldsTheComebackAndTheAiSpendsItImmediately` pins that
        /// the player latches at Armed and times the shot while the AI goes straight to Active. A
        /// single pooled rate would average a timed decision with a reflex.
        /// </summary>
        [Test]
        public void EachSideIsCountedSeparatelyBecauseTheySpendItDifferently()
        {
            float max = 150f;
            float danger = max * LastStand.DangerHpFraction;

            Telemetry.Comeback(byPlayer: true, ownCoreHp: danger, ownCoreMax: max, foeCoreHp: 100f, foeCoreMax: max);
            Telemetry.Comeback(byPlayer: false, ownCoreHp: danger, ownCoreMax: max, foeCoreHp: 100f, foeCoreMax: max);
            Telemetry.Comeback(byPlayer: false, ownCoreHp: danger, ownCoreMax: max, foeCoreHp: 100f, foeCoreMax: max);

            var (player, ai) = Telemetry.ComebackActivations();
            Assert.AreEqual(1, player, "one player activation was recorded");
            Assert.AreEqual(2, ai, "and two AI activations");
        }

        /// <summary>
        /// The maxima are stored, not just the current values, so a later core-HP or stage-height
        /// retune cannot silently reinterpret an old dump. This asserts they survive the round trip —
        /// a field that is written but never read would drift without anyone noticing.
        /// </summary>
        [Test]
        public void BothMaximaSurviveTheRecordSoOldDumpsStayInterpretable()
        {
            Telemetry.Comeback(byPlayer: true, ownCoreHp: 40f, ownCoreMax: 150f, foeCoreHp: 90f, foeCoreMax: 200f);

            var events = Telemetry.Snapshot();
            Assert.AreEqual(1, events.Count, "exactly one event was recorded");

            var e = events[0];
            Assert.AreEqual(nameof(Telemetry.EventKind.Comeback), e.kind, "kind is the wire name");
            Assert.AreEqual("player", e.label, "label carries the activating side");
            Assert.AreEqual(40f, e.a, 0.0001f, "a = own core HP");
            Assert.AreEqual(90f, e.b, 0.0001f, "b = foe core HP, the value the cap is compared against");
            Assert.AreEqual(150f, e.c, 0.0001f, "c = own core max");
            Assert.AreEqual(200f, e.d, 0.0001f,
                "d = foe core max, so a dump recorded under a different core scale is still readable");
        }
    }
}
