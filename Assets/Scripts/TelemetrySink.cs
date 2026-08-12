using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// The runtime edge of <see cref="Telemetry"/>: session counters, persistence, and the
    /// console dump. Split from the pure type so the recording rules stay EditMode-testable
    /// while the parts that need <c>Application.isPlaying</c>, PlayerPrefs, and Debug live here.
    ///
    /// Static rather than a MonoBehaviour on purpose. A component would need a GameObject in
    /// every scene, and the one scene this game has is rebuilt per stage — a sink that dies on
    /// reload would lose exactly the cross-match counters (retries, stages cleared) that G7
    /// exists to measure. The static session state is reset explicitly by
    /// <see cref="BeginSession"/> so PlayMode isolation stays intact (the lesson of D-015,
    /// where surviving statics made the whole PlayMode suite non-deterministic).
    /// </summary>
    public static class TelemetrySink
    {
        /// <summary>Off by default in EditMode so a test never writes a player's prefs.</summary>
        public static bool Enabled = true;

        private static int matchesThisSession;
        private static int stagesClearedThisSession;
        private static bool sessionOpen;

        /// <summary>Blocks destroyed since the last turn boundary. Collapse is reported per
        /// resolved chain rather than per block: the cascade is the reward event, and a
        /// per-block record would flood the ring during a large one.</summary>
        private static int blocksThisTurn;
        private static int deepestChainThisTurn;

        /// <summary>Matches played this session beyond the first — the G7 repeat proxy.</summary>
        public static int RetryCount => Mathf.Max(0, matchesThisSession - 1);

        public static void BeginSession()
        {
            matchesThisSession = 0;
            stagesClearedThisSession = 0;
            blocksThisTurn = 0;
            deepestChainThisTurn = 0;
            sessionOpen = true;
            Telemetry.Clear();
        }

        public static void MatchStart(StageId stage, string deck)
        {
            if (!Enabled) return;
            if (!sessionOpen) BeginSession();
            matchesThisSession++;
            blocksThisTurn = 0;
            deepestChainThisTurn = 0;
            Telemetry.MatchStart(stage.ToString(), deck);
        }

        public static void Volley(string unit, float power, float angle, float wind)
        {
            if (!Enabled) return;
            Telemetry.Volley(unit, power, angle, wind);
        }

        /// <summary>Called by <see cref="DestructibleBlock"/> as blocks fall. Accumulates only;
        /// the event is emitted at the turn boundary by <see cref="TurnResolved"/>.</summary>
        public static void BlockDestroyed(int chainDepth)
        {
            if (!Enabled) return;
            blocksThisTurn++;
            if (chainDepth > deepestChainThisTurn) deepestChainThisTurn = chainDepth;
        }

        /// <summary>Turn boundary: emit the accumulated chain, then reset. A turn that broke
        /// nothing emits nothing — an empty collapse record would dilute the reward-density
        /// aggregate G4/G7 read.</summary>
        public static void TurnResolved()
        {
            if (!Enabled) return;
            if (blocksThisTurn > 0) Telemetry.Collapse(blocksThisTurn, deepestChainThisTurn);
            blocksThisTurn = 0;
            deepestChainThisTurn = 0;
        }

        public static void MatchEnd(bool playerWon, int turns, float coreHpDelta, bool stageCleared)
        {
            if (!Enabled) return;
            TurnResolved(); // never lose the final volley's cascade
            if (stageCleared) stagesClearedThisSession++;
            Telemetry.MatchEnd(playerWon ? Telemetry.WinnerPlayer : Telemetry.WinnerEnemy, turns, coreHpDelta);
            Telemetry.Session(stagesClearedThisSession, RetryCount);
            Telemetry.Flush();
            Dump();
        }

        /// <summary>Prints the aggregate line plus the raw JSON. This is the whole collection
        /// channel: the build is served from static hosting with no server, so a human reads
        /// the dump out of the browser console and pastes it into
        /// <c>_workspace/current/qa/gate-measurements.md</c>. Manual on purpose — an automatic
        /// upload would need an endpoint, and an endpoint would need a privacy notice.</summary>
        public static void Dump()
        {
            if (!Enabled) return;
            Debug.Log(Telemetry.Summary());
            Debug.Log("[telemetry-json] " + Telemetry.ToJson());
        }
    }
}
