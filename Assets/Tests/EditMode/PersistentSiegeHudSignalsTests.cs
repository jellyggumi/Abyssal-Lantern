using CastleBusters;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    public sealed class PersistentSiegeHudSignalsTests
    {
        [Test]
        public void EveryMatchStateExceptIntro_KeepsThePersistentSignalRailVisible()
        {
            foreach (GameState state in new[] { GameState.Setup, GameState.PlayerTurn, GameState.AITurn, GameState.GameOver })
            {
                Assert.IsTrue(PersistentSiegeHudState.From(state, true, false).IsVisible,
                    $"{state} must retain the nonverbal match HUD");
            }

            Assert.IsFalse(PersistentSiegeHudState.From(GameState.Intro, true, false).IsVisible,
                "the full-screen intro is the one state allowed to own the display");
        }

        [Test]
        public void PlayerAimState_HighlightsOnlyThePlayerFactionAndLaunchAffordance()
        {
            var state = PersistentSiegeHudState.From(GameState.PlayerTurn, true, false);

            Assert.IsTrue(state.PlayerFactionActive);
            Assert.IsFalse(state.EnemyFactionActive);
            Assert.IsTrue(state.LaunchReady);
            Assert.IsTrue(state.ObjectiveCoreHighlighted);
            Assert.IsFalse(state.MatchComplete);
        }

        [Test]
        public void ResolvingAndEnemyStates_RemoveTheLaunchAffordanceWhileRetainingTheCurrentFactionSignal()
        {
            var resolving = PersistentSiegeHudState.From(GameState.PlayerTurn, true, true);
            var enemy = PersistentSiegeHudState.From(GameState.AITurn, false, false);

            Assert.IsTrue(resolving.PlayerFactionActive);
            Assert.IsFalse(resolving.EnemyFactionActive);
            Assert.IsFalse(resolving.LaunchReady);
            Assert.IsTrue(enemy.EnemyFactionActive);
            Assert.IsFalse(enemy.PlayerFactionActive);
            Assert.IsFalse(enemy.LaunchReady);
        }

        [Test]
        public void GameOver_ReplacesTheLiveObjectiveHighlightWithACompletionSeal()
        {
            var state = PersistentSiegeHudState.From(GameState.GameOver, true, false);

            Assert.IsTrue(state.IsVisible);
            Assert.IsTrue(state.MatchComplete);
            Assert.IsFalse(state.ObjectiveCoreHighlighted);
            Assert.IsFalse(state.LaunchReady);
        }
    }
}
