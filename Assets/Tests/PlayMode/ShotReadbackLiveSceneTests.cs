using System.Collections;
using System.Reflection;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Proves post-action readback actually reaches the player in the real scene, not merely that
    /// the arithmetic composes a string.
    ///
    /// The EditMode pins next to this file establish that a tally becomes a sentence. They cannot
    /// establish that a shot fired in the live scene opens a window, that flight positions get
    /// sampled, that the settle boundary seals it, or that the HUD strip ever displays it — four
    /// separate wirings, each of which can be dead while every unit test passes. Task #37 made
    /// the same distinction for the cannon and found the rules table healthy while asking whether
    /// the feature was reachable at all.
    ///
    /// The complaint being answered is a user report: *"적이 어떻게 쏘는지도 안 보인다."*
    /// </summary>
    public class ShotReadbackLiveSceneTests
    {
        /// <summary>
        /// Scoped to this fixture, never suite-wide. The Unity MCP plugin logs an authorization
        /// failure when it cannot reach the local hub, and NUnit collects any stray Debug.LogError
        /// as an unhandled message — which failed this fixture on plugin connectivity rather than
        /// on anything the game did. Documented as environment noise in
        /// `_workspace/current/qa/playmode-6000-triage.md`.
        /// </summary>
        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        /// <summary>
        /// Loads the arena with the noise guard re-armed around the load.
        ///
        /// Re-armed here and not only in SetUp because the runner re-arms LogAssert per test
        /// phase: a flag set in SetUp is already gone by the time the scene load lets the plugin
        /// log. This is the house pattern (see HudCanvasContractTests.BootMatch).
        /// </summary>
        private static IEnumerator BootMatch()
        {
            LogAssert.ignoreFailingMessages = true;
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            LogAssert.ignoreFailingMessages = true;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"expected private field {target.GetType().Name}.{field}");
            f.SetValue(target, value);
        }

        private static GameObject FindTrace(bool player) =>
            GameObject.Find(player ? "ShotTrace_Player" : "ShotTrace_Enemy");

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator PlayerShot_LeavesAnArcThatEndsAtTheImpactAndAReadbackLine()
        {
            yield return BootMatch();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "the scene must bring up a GameManager");
            gm.BeginSiege();
            yield return null;

            // Start from a clean slate: BeginSiege may have run garrison fire, and this test is
            // about one identified shot rather than whatever the board did on its own.
            ShotTraceDirector.ResetForNewMatch();
            Assert.IsEmpty(ShotTraceDirector.LatestLine, "precondition: nothing has been read back yet");
            Assert.IsNull(FindTrace(true), "precondition: no player arc on the field");

            var lm = Object.FindFirstObjectByType<LaunchManager>();
            Assert.IsNotNull(lm, "the scene must provide a LaunchManager");

            SetPrivate(gm, "isPlayerTurn", true);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.PlayerTurn;
            yield return null;

            // Toward the enemy keep and high enough to fly rather than roll: the trace only means
            // something if the projectile actually travels.
            lm.SimulateLaunch(new Vector2(15f, 11f));

            // Sampling happens in FixedUpdate while the body is Launched. Give it flight frames
            // before asserting anything about the shape.
            float flightWatch = 0f;
            while (ShotTraceDirector.SampleCount < 2 && flightWatch < 8f)
            {
                flightWatch += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.GreaterOrEqual(ShotTraceDirector.SampleCount, 2,
                $"the flight was never sampled — UnitController.FixedUpdate is not feeding the trace "
                + $"(watched {flightWatch:F1}s)");

            // The seal happens at the settle boundary inside WaitAndEndTurn: past the flight
            // watchdog, the post-impact hold, and the settle loop. Wait for the turn to hand over
            // rather than guessing a duration — an earlier probe elsewhere in this suite asserted
            // a fixed 3s sleep and was measuring the stopwatch, not the game (task #35).
            float settleWatch = 0f;
            while (string.IsNullOrEmpty(ShotTraceDirector.LatestLine) && settleWatch < 25f)
            {
                settleWatch += Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.IsNotEmpty(ShotTraceDirector.LatestLine,
                $"no readback after {settleWatch:F1}s. The shot resolved but nothing was sealed — "
                + "GameManager.WaitAndEndTurn is not calling ShotTraceDirector.Seal()");
            Assert.IsTrue(ShotTraceDirector.LatestLineByPlayer,
                $"the player fired, so the line must read as friendly. Got: {ShotTraceDirector.LatestLine}");

            var trace = FindTrace(true);
            Assert.IsNotNull(trace,
                "the arc was never drawn. Samples existed and the shot sealed, so this is the "
                + "renderer path failing, not the recording path");

            var line = trace.GetComponent<LineRenderer>();
            Assert.IsNotNull(line, "the trace root must carry the LineRenderer");
            Assert.GreaterOrEqual(line.positionCount, 2,
                "a one-point line is a dot at the muzzle, which reads as a rendering fault");

            // The marker belongs at the end of the arc: that is the whole claim of R-2 — the
            // trajectory already existed and never said where it concluded — and the endpoint is
            // now how it says that. The icon that used to sit here was cut: a survey of thirteen
            // comparable titles found icon-at-impact in exactly one, Battleship, whose board is
            // hidden, while ten change the world at the impact point, which this game already
            // does. So the arc must END where the shot landed, and carry no child renderer.
            Assert.IsEmpty(trace.GetComponentsInChildren<SpriteRenderer>(true),
                "the impact icon was removed as a form error; the arc must carry no child renderer");

            var lastPoint = line.GetPosition(line.positionCount - 1);
            var body = Object.FindObjectsByType<UnitController>(FindObjectsSortMode.None);
            float nearest = float.MaxValue;
            foreach (var u in body)
            {
                if (u == null || u.isPlayerUnit == false) continue;
                nearest = Mathf.Min(nearest, Vector2.Distance(u.transform.position, lastPoint));
            }
            Assert.Less(nearest, 6f,
                $"the arc's final vertex must land near where the projectile actually came to rest, "
                + $"since that endpoint is now the only thing marking the impact (nearest player "
                + $"body {nearest:F2}u away)");

            // The trace must be inert. A collider here would let the memory of a shot deflect the
            // next one — presentation writing back into simulation (CLAUDE.md §2).
            Assert.IsEmpty(trace.GetComponentsInChildren<Collider2D>(true),
                "the trace is an observer; it must not be able to touch a projectile");
        }

        /// <summary>
        /// The line has to be on screen, not merely in a static. The strip is the only surface
        /// that carries it, and it used to hide itself for the entire player turn — which is
        /// precisely when last turn's result is the input to this turn's aim.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator ReadbackLine_IsDisplayedOnTheStripDuringThePlayerTurn()
        {
            yield return BootMatch();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();
            yield return null;

            var alarm = Object.FindFirstObjectByType<SiegeAlarmSystem>();
            Assert.IsNotNull(alarm, "the scene must provide a SiegeAlarmSystem — it owns the strip");

            // Seal a known result without flying a projectile: this test is about the display
            // path, and mixing in physics would make a strip failure look like a shot failure.
            ShotTraceDirector.ResetForNewMatch();
            ShotTraceDirector.BeginShot(false, DeploymentRules.DisplayName(UnitType.Barrel), Vector2.zero);
            ShotTraceDirector.NoteBlockDestroyed();
            ShotTraceDirector.NoteCoreDamage(40f);
            ShotTraceDirector.Seal();
            var expected = ShotTraceDirector.LatestLine;

            SetPrivate(gm, "isPlayerTurn", true);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.PlayerTurn;

            // One live Update of the alarm system is what writes the strip.
            yield return null;
            yield return null;

            var strip = GameObject.Find("FlowStateStrip");
            Assert.IsNotNull(strip, "the strip must exist once the alarm system has built its UI");
            Assert.IsTrue(strip.activeInHierarchy,
                "the strip hid itself for the whole player turn before this change; the readback "
                + "is useless if it is only visible when the player cannot act on it");

            var text = strip.GetComponent<TextMeshProUGUI>();
            Assert.IsNotNull(text);
            Assert.AreEqual(expected, text.text,
                "the strip must show the sealed readback verbatim");
        }

        /// <summary>
        /// UX-003a: the strip must not advertise a click the game refuses. This is the defect
        /// class the survey found in zero of twelve comparable titles, and it is worth its own
        /// live assertion because the string and the gate lived in different files.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator EnemyTurnStrip_DoesNotOfferBrickReservationWhileTheClickIsRefused()
        {
            yield return BootMatch();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();
            yield return null;

            Assert.IsTrue(gm.EnforcesOneShotTurns,
                "precondition: the one-shot loop is what suspends brick reservation");

            SetPrivate(gm, "isPlayerTurn", false);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.AITurn;

            yield return null;
            yield return null;

            var strip = GameObject.Find("FlowStateStrip");
            Assert.IsNotNull(strip);
            var text = strip.GetComponent<TextMeshProUGUI>().text;

            StringAssert.Contains("적 포격", text, "the enemy turn should still say whose turn it is");
            StringAssert.DoesNotContain("벽돌", text,
                "BrickPlacementController returns early in the one-shot loop and eats the click, so "
                + "offering it here is an instruction the game will not honour");
        }
    }
}
