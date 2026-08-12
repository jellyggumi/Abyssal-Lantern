using System.Collections.Generic;
using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the first-play guided-objective contract (2026-08-12 first-contact feedback:
    /// "무슨 게임인지 파악이 안 된다 / 단계별로 한개씩 목적을 주면서 진행해야").
    ///
    /// Scope: the pure <see cref="FirstPlayGuide"/> state machine plus the one GameManager
    /// hook it drives (<c>HoldTurnTimerForCoaching</c>). Every failure here is a first-time
    /// player experience regression — a step that advances on the wrong action teaches the
    /// wrong control, a step with no instruction is a silent banner, a broken clock hold
    /// forfeits the player's first turn while they are still reading what a turn is, and a
    /// hold that works during the enemy turn would freeze the match instead of coaching it.
    ///
    /// The post-shot transitions are pinned against SAMPLED observations, not continuous
    /// ones: hit-stops freeze the board mid-resolve and the live 2026-08-12 QA run proved a
    /// committed shot can resolve entirely inside one swallowed sampling window. The guide
    /// must therefore never regress on a gap — the turn counter carries the truth across it.
    ///
    /// FirstPlayCoachController is deliberately never constructed: it builds a Canvas and
    /// reads Input/Time, which is PlayMode's job. Everything below is arithmetic over data.
    /// </summary>
    [TestFixture]
    public sealed class FirstPlayGuideTests
    {
        private static FirstPlayGuide.Observation Frame(
            bool acknowledged = false,
            bool playerTurn = true,
            bool aiming = false,
            bool resolving = false,
            bool gameOver = false,
            int turn = 0)
        {
            return new FirstPlayGuide.Observation(acknowledged, playerTurn, aiming, resolving, gameOver, turn);
        }

        [Test]
        public void HappyPath_WalksEveryStepInOrder_OnTheActionEachStepTeaches()
        {
            var guide = new FirstPlayGuide();
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.Goal),
                "a fresh guide must open on the goal statement — what game this is comes first");

            // Reading the goal card is not an action; nothing but acknowledge advances it.
            Assert.That(guide.Advance(Frame(aiming: true, resolving: true)), Is.False,
                "the goal card must not advance on stray board activity");
            Assert.That(guide.Advance(Frame(acknowledged: true)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.Draw));

            // Draw advances when the player actually starts pulling, not on acknowledge.
            Assert.That(guide.Advance(Frame(acknowledged: true)), Is.False,
                "clicking through must not skip the draw lesson — only drawing completes it");
            Assert.That(guide.Advance(Frame(aiming: true)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.Release));

            // Release advances when the shot is committed (volley resolving, same turn).
            Assert.That(guide.Advance(Frame(aiming: true)), Is.False,
                "holding the pull is still the release step");
            Assert.That(guide.Advance(Frame(resolving: true)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.EnemyReply));

            // The reply beat completes when control returns: the enemy's turn was turn 1,
            // and turn 2 is the player's again. Turn 0 winding down is not enough.
            Assert.That(guide.Advance(Frame(playerTurn: true, turn: 0)), Is.False,
                "the player's own turn winding down is not yet the enemy's reply");
            Assert.That(guide.Advance(Frame(playerTurn: false, turn: 1)), Is.False,
                "the enemy acting is observed, not completed — the step ends on control returning");
            Assert.That(guide.Advance(Frame(playerTurn: true, turn: 2)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.FreePlay));

            Assert.That(guide.Advance(Frame(acknowledged: true, turn: 2)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.Done));
            Assert.That(guide.IsFinished, Is.True);
            Assert.That(guide.Advance(Frame(acknowledged: true, aiming: true, resolving: true, turn: 9)), Is.False,
                "a finished guide must stay finished");
        }

        [Test]
        public void SwallowedResolveWindow_AdvancesToEnemyReply_NeverBackToDraw()
        {
            // The live QA regression (2026-08-12): a full-power shot resolved entirely
            // inside hit-stop sampling gaps. The frame after the pull ended, the guide saw
            // aiming=false, resolving=false — and the flag-sampled version fell back to
            // Draw, narrating "press the ring" over a match that was already two turns on.
            var guide = new FirstPlayGuide();
            guide.Advance(Frame(acknowledged: true));                    // Goal -> Draw
            guide.Advance(Frame(aiming: true, turn: 0));                 // Draw -> Release

            // Next sampled frame: resolve was never observed, but the turn moved on.
            Assert.That(guide.Advance(Frame(playerTurn: false, turn: 1)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.EnemyReply),
                "a turn advance past the pull's turn proves the shot was spent — the guide must never regress to Draw");

            // And the whole reply can be swallowed too: next observation is already the
            // player's next turn.
            Assert.That(guide.Advance(Frame(playerTurn: true, turn: 2)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.FreePlay));
        }

        [Test]
        public void WeakRelease_FallsBackToDraw_InsteadOfNarratingALaunchThatNeverHappened()
        {
            var guide = new FirstPlayGuide();
            guide.Advance(Frame(acknowledged: true));                    // Goal -> Draw
            guide.Advance(Frame(aiming: true, turn: 0));                 // Draw -> Release

            // The launcher refuses a too-shallow pull: aiming ends, same turn, no resolve.
            Assert.That(guide.Advance(Frame(aiming: false, turn: 0)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.Draw),
                "a refused shot must return to the draw instruction, not stay on release");

            // And the retry still works.
            guide.Advance(Frame(aiming: true, turn: 0));
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.Release));
        }

        [Test]
        public void KeyboardCommit_SkipsTheDragObservation_ButNeverTheReplyBeat()
        {
            var guide = new FirstPlayGuide();
            guide.Advance(Frame(acknowledged: true));                    // Goal -> Draw

            // Space commits a shot without ever entering the drag state: the volley resolving
            // during the player's turn IS the proof a shot happened.
            Assert.That(guide.Advance(Frame(resolving: true, playerTurn: true, turn: 0)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.EnemyReply),
                "a committed volley must advance the coach past the pointing-at-the-slingshot step");

            // Reply completes on control returning at turn 2.
            Assert.That(guide.Advance(Frame(playerTurn: true, turn: 2)), Is.True);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.FreePlay));
        }

        [Test]
        public void EnemyResolve_DoesNotAdvanceTheDrawStep()
        {
            var guide = new FirstPlayGuide();
            guide.Advance(Frame(acknowledged: true));                    // Goal -> Draw

            // A player who forfeited their first turn watches the AI shoot: resolving is
            // true but it is not the player's shot, so the draw lesson stays.
            Assert.That(guide.Advance(Frame(resolving: true, playerTurn: false, turn: 1)), Is.False);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.Draw));

            // The draw lesson also survives the turn coming back around.
            Assert.That(guide.Advance(Frame(playerTurn: true, turn: 2)), Is.False);
            Assert.That(guide.Current, Is.EqualTo(FirstPlayGuide.Step.Draw));
        }

        [Test]
        public void GameOver_EndsCoachingFromAnyStep()
        {
            foreach (var priming in new[] { 0, 1, 2, 3, 4 })
            {
                var guide = new FirstPlayGuide();
                // Walk the guide forward `priming` steps along the happy path.
                var walk = new[]
                {
                    Frame(acknowledged: true),
                    Frame(aiming: true, turn: 0),
                    Frame(resolving: true, turn: 0),
                    Frame(playerTurn: false, turn: 1),
                    Frame(playerTurn: true, turn: 2)
                };
                for (int i = 0; i < priming; i++) guide.Advance(walk[i]);

                Assert.That(guide.Advance(Frame(gameOver: true, turn: 3)), Is.True,
                    $"game over must finish the guide from step index {priming}");
                Assert.That(guide.IsFinished, Is.True);
            }
        }

        [Test]
        public void ClockHold_CoversExactlyThePreShotSteps()
        {
            var guide = new FirstPlayGuide();
            Assert.That(guide.HoldsTurnClock, Is.True, "Goal reads under a held clock");

            guide.Advance(Frame(acknowledged: true));
            Assert.That(guide.HoldsTurnClock, Is.True, "Draw is still pre-shot coaching");

            guide.Advance(Frame(aiming: true, turn: 0));
            Assert.That(guide.HoldsTurnClock, Is.True, "an active pull may finish under the hold");

            guide.Advance(Frame(resolving: true, turn: 0));
            Assert.That(guide.HoldsTurnClock, Is.False,
                "once the shot is away the match runs on the real clock — enemy turns are never held");

            guide.Advance(Frame(playerTurn: true, turn: 2));
            Assert.That(guide.HoldsTurnClock, Is.False, "free play is the real game");
        }

        [Test]
        public void EveryVisibleStep_CarriesAnInstructionAndALabel()
        {
            foreach (FirstPlayGuide.Step step in System.Enum.GetValues(typeof(FirstPlayGuide.Step)))
            {
                if (step == FirstPlayGuide.Step.Done) continue;
                Assert.That(FirstPlayGuide.Instruction(step), Is.Not.Empty,
                    $"{step} would render a silent banner");
                Assert.That(FirstPlayGuide.StepLabel(step), Does.Match(@"^\d/\d$"),
                    $"{step} must show its place in the sequence (단계별로 한개씩)");
            }

            Assert.That(FirstPlayGuide.Instruction(FirstPlayGuide.Step.Done), Is.Empty);
            Assert.That(FirstPlayGuide.StepLabel(FirstPlayGuide.Step.Done), Is.Empty);
        }

        // ---- GameManager.HoldTurnTimerForCoaching ----

        private readonly List<GameObject> createdObjects = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null) Object.DestroyImmediate(createdObjects[i]);
            }
            createdObjects.Clear();
        }

        [Test]
        public void HoldTurnTimer_TopsUpOnlyALivePlayerTurn()
        {
            var gameManager = CreateGameManager();
            gameManager.currentState = GameState.PlayerTurn;
            SetPrivateField(gameManager, "isPlayerTurn", true);
            SetPrivateField(gameManager, "turnTimer", 2f);

            gameManager.HoldTurnTimerForCoaching(8f);
            Assert.That(GetPrivateField<float>(gameManager, "turnTimer"), Is.EqualTo(8f).Within(0.0001f),
                "a nearly-expired first turn must be topped up while coaching");

            // Never shortens: a timer already above the minimum is left alone.
            SetPrivateField(gameManager, "turnTimer", 12f);
            gameManager.HoldTurnTimerForCoaching(8f);
            Assert.That(GetPrivateField<float>(gameManager, "turnTimer"), Is.EqualTo(12f).Within(0.0001f),
                "the hold is a floor, not an assignment");
        }

        [Test]
        public void HoldTurnTimer_RefusesEnemyTurnsResolutionAndGameOver()
        {
            var gameManager = CreateGameManager();

            // Enemy turn: holding it would freeze the AI's clock, not coach the player.
            gameManager.currentState = GameState.AITurn;
            SetPrivateField(gameManager, "isPlayerTurn", false);
            SetPrivateField(gameManager, "turnTimer", 1f);
            gameManager.HoldTurnTimerForCoaching(8f);
            Assert.That(GetPrivateField<float>(gameManager, "turnTimer"), Is.EqualTo(1f).Within(0.0001f));

            // Player state but volley resolving: the resolve owns the clock.
            gameManager.currentState = GameState.PlayerTurn;
            SetPrivateField(gameManager, "isPlayerTurn", true);
            SetPrivateField(gameManager, "isResolvingTurn", true);
            gameManager.HoldTurnTimerForCoaching(8f);
            Assert.That(GetPrivateField<float>(gameManager, "turnTimer"), Is.EqualTo(1f).Within(0.0001f));

            // Game over: nothing to hold.
            SetPrivateField(gameManager, "isResolvingTurn", false);
            gameManager.currentState = GameState.GameOver;
            gameManager.HoldTurnTimerForCoaching(8f);
            Assert.That(GetPrivateField<float>(gameManager, "turnTimer"), Is.EqualTo(1f).Within(0.0001f));
        }

        private GameManager CreateGameManager()
        {
            var gameObject = new GameObject("FirstPlayGuide_GameManager")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            createdObjects.Add(gameObject);
            var gameManager = gameObject.AddComponent<GameManager>();
            MethodInfo awake = typeof(GameManager).GetMethod(
                "Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null);
            awake.Invoke(gameManager, null);
            return gameManager;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {target.GetType().Name}.{fieldName}.");
            field.SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {target.GetType().Name}.{fieldName}.");
            return (T)field.GetValue(target);
        }
    }
}
