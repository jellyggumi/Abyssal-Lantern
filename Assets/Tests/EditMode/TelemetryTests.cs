using CastleBusters;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The measurement instrument itself. These exist because a gate reading a broken
    /// instrument is worse than a gate reading nothing: "win rate 0%" and "no data" must never
    /// look alike, a truncated ring must not silently pass for a complete session, and an
    /// observer must not perturb what it observes.
    /// </summary>
    public class TelemetryTests
    {
        [SetUp]
        public void SetUp()
        {
            Telemetry.Clear();
            TelemetrySink.Enabled = false; // never touch PlayerPrefs or the log from a test
        }

        [TearDown]
        public void TearDown()
        {
            Telemetry.Clear();
            TelemetrySink.Enabled = true;
        }

        [Test]
        public void RingBuffer_NeverExceedsCapacity()
        {
            for (int i = 0; i < Telemetry.Capacity + 25; i++) Telemetry.Volley("Knight", i, 45f, 0f);

            Assert.AreEqual(Telemetry.Capacity, Telemetry.Count, "the ring must bound itself");
            Assert.AreEqual(25, Telemetry.Dropped, "every discarded event must be counted, not lost silently");
        }

        [Test]
        public void RingBuffer_DropsOldestFirst()
        {
            for (int i = 0; i < Telemetry.Capacity + 3; i++) Telemetry.Volley("Knight", i, 45f, 0f);

            var events = Telemetry.Snapshot();
            // Events 0,1,2 fell out; the window now starts at 3 and stays chronological.
            Assert.AreEqual(3f, events[0].a, 0.001f, "the oldest surviving event must be the 4th recorded");
            Assert.AreEqual(Telemetry.Capacity + 2f, events[events.Count - 1].a, 0.001f);
        }

        [Test]
        public void WinRate_DistinguishesNoDataFromZeroWins()
        {
            // The distinction the gate depends on: an instrument that never fired must not
            // read as a game the player always loses.
            Assert.AreEqual(-1f, Telemetry.PlayerWinRate(), 0.001f, "no data must be negative, not 0");

            Telemetry.MatchEnd(Telemetry.WinnerEnemy, 30, -150f);
            Assert.AreEqual(0f, Telemetry.PlayerWinRate(), 0.001f, "a real loss must read as exactly 0");
        }

        [Test]
        public void WinRate_AndAverageTurns_MatchHandArithmetic()
        {
            Telemetry.MatchEnd(Telemetry.WinnerPlayer, 40, 20f);
            Telemetry.MatchEnd(Telemetry.WinnerPlayer, 30, 10f);
            Telemetry.MatchEnd(Telemetry.WinnerEnemy, 50, -30f);
            Telemetry.MatchEnd(Telemetry.WinnerEnemy, 40, -10f);

            Assert.AreEqual(0.5f, Telemetry.PlayerWinRate(), 0.001f);
            Assert.AreEqual(40f, Telemetry.AverageTurns(), 0.001f);
        }

        [Test]
        public void Volleys_DoNotContaminateMatchAggregates()
        {
            // A kind filter that leaked would inflate the denominator and quietly halve the
            // measured win rate — the failure mode that makes a wrong number look plausible.
            Telemetry.Volley("Knight", 80f, 45f, 2f);
            Telemetry.Collapse(6, 3);
            Telemetry.MatchEnd(Telemetry.WinnerPlayer, 30, 15f);

            Assert.AreEqual(1f, Telemetry.PlayerWinRate(), 0.001f);
            Assert.AreEqual(30f, Telemetry.AverageTurns(), 0.001f);
        }

        [Test]
        public void RepeatRate_CountsSessionsThatReEnteredTheLoop()
        {
            Telemetry.Session(stagesCleared: 1, retryCount: 0); // played once, left
            Telemetry.Session(stagesCleared: 2, retryCount: 3); // came back

            Assert.AreEqual(0.5f, Telemetry.RepeatRate(), 0.001f);
        }

        [Test]
        public void Json_RoundTripsEveryFieldAndTheDropCount()
        {
            Telemetry.MatchStart("Stage2", "one-shot");
            Telemetry.Volley("Archer", 72.5f, 33.25f, -1.5f);
            Telemetry.Collapse(7, 4);
            Telemetry.MatchEnd(Telemetry.WinnerPlayer, 28, 42.5f);
            string json = Telemetry.ToJson();

            Telemetry.Clear();
            Telemetry.FromJson(json);

            var events = Telemetry.Snapshot();
            Assert.AreEqual(4, events.Count);
            Assert.AreEqual(Telemetry.EventKind.MatchStart, events[0].Kind);
            Assert.AreEqual("Stage2", events[0].label);
            Assert.AreEqual("Archer", events[1].label);
            Assert.AreEqual(72.5f, events[1].a, 0.001f);
            Assert.AreEqual(33.25f, events[1].b, 0.001f);
            Assert.AreEqual(-1.5f, events[1].c, 0.001f);
            Assert.AreEqual(7f, events[2].a, 0.001f);
            Assert.AreEqual(4f, events[2].b, 0.001f);
            Assert.AreEqual(42.5f, events[3].b, 0.001f);
        }

        [Test]
        public void Json_CorruptInputClearsInsteadOfThrowing()
        {
            // A corrupt pref must not brick a player's boot for the sake of a measurement.
            Assert.DoesNotThrow(() => Telemetry.FromJson("{not json at all"));
            Assert.AreEqual(0, Telemetry.Count);
        }

        [Test]
        public void Record_DoesNotMutateGameState()
        {
            // CLAUDE.md §2 as a test, not a comment: an observer that writes back into the
            // simulation invalidates every deterministic sim and every EditMode pin that
            // depends on one. Telemetry runs with no scene at all here — if it touched
            // GameManager statics, these frozen values would move.
            float apronBefore = GameManager.LaunchApronAbsX;
            int blocksBefore = GameManager.BlocksPerKeep(3);
            float scaleBefore = GameManager.StageActorVisualScale;

            Telemetry.MatchStart("Stage1", "one-shot");
            Telemetry.Volley("Knight", 90f, 50f, 3f);
            Telemetry.Collapse(9, 5);
            Telemetry.MatchEnd(Telemetry.WinnerPlayer, 33, 60f);
            Telemetry.Session(1, 2);

            Assert.AreEqual(apronBefore, GameManager.LaunchApronAbsX, 0.0001f);
            Assert.AreEqual(blocksBefore, GameManager.BlocksPerKeep(3));
            Assert.AreEqual(scaleBefore, GameManager.StageActorVisualScale, 0.0001f);
        }

        [Test]
        public void Summary_ReportsNoDataAsNotAvailable()
        {
            // A dashboard that prints "0.0%" for an instrument that never fired is how a
            // silent instrumentation failure gets mistaken for a balance problem.
            StringAssert.Contains("n/a", Telemetry.Summary());

            Telemetry.MatchEnd(Telemetry.WinnerPlayer, 30, 10f);
            StringAssert.Contains("100.0%", Telemetry.Summary());
        }
    }
}
