namespace CastleBusters
{
    /// <summary>
    /// Pure presentation contract for the match-critical, text-independent HUD signals.
    /// Keeping this mapping outside of the UI builder makes the state readable and testable
    /// without needing a scene, canvas, or localization table.
    ///
    /// (A LaunchReady signal used to live here, rendered as a bottom-center crosshair.
    /// Playtest feedback read that diamond as meaningless — the slingshot and its bobbing
    /// hint label already ARE the launch affordance — so the signal left with its renderer.)
    /// </summary>
    public readonly struct PersistentSiegeHudState
    {
        public bool IsVisible { get; }
        public bool PlayerFactionActive { get; }
        public bool EnemyFactionActive { get; }
        public bool ObjectiveCoreHighlighted { get; }
        public bool MatchComplete { get; }

        private PersistentSiegeHudState(
            bool isVisible,
            bool playerFactionActive,
            bool enemyFactionActive,
            bool objectiveCoreHighlighted,
            bool matchComplete)
        {
            IsVisible = isVisible;
            PlayerFactionActive = playerFactionActive;
            EnemyFactionActive = enemyFactionActive;
            ObjectiveCoreHighlighted = objectiveCoreHighlighted;
            MatchComplete = matchComplete;
        }

        public static PersistentSiegeHudState From(GameState state, bool isPlayerTurn, bool isResolvingTurn)
        {
            // Intro owns the whole display. Every other state, including the frozen result
            // state, keeps the match vocabulary on-screen so the win condition never vanishes.
            bool visible = state != GameState.Intro;
            bool complete = state == GameState.GameOver;
            // Resolution locks the shot, not the faction. Keep its chevron lit through the
            // impact/settling phase so a player never loses track of whose turn is resolving.
            bool playerActive = visible && !complete && isPlayerTurn;
            bool enemyActive = visible && !complete && !isPlayerTurn;

            return new PersistentSiegeHudState(
                visible,
                playerActive,
                enemyActive,
                visible && !complete,
                complete);
        }
    }
}
