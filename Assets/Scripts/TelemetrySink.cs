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

        /// <summary>Player shots and how many reached the ENEMY keep. Session-scoped, like the
        /// retry counter: a grade from one match is noise, and the whole point is a measure stable
        /// enough to hand a handicap off. Reset by <see cref="BeginSession"/> so PlayMode
        /// isolation holds (D-015).</summary>
        private static int playerShots;
        private static int playerHits;

        /// <summary>The AI's mirror, plus the summed aim-error offset it fired under. Not consumed
        /// by any rule — it exists to measure how much a metre of aim error costs in hit rate,
        /// which is the conversion the balance model has no way to derive.</summary>
        private static int aiShots;
        private static int aiHits;
        private static float aiAimErrorSum;

        /// <summary>Did the shot in flight reach the enemy keep? Cleared at each side's turn
        /// boundary. A bool rather than a count because one shot per turn is the game's rule
        /// (<see cref="OneShotSiegeRules"/>), so a second landing is the same shot's cascade.</summary>
        private static bool landedThisTurn;
        private static bool aiLandedThisTurn;

        /// <summary>
        /// Per-shot material removed, and the running sums the coefficient of variation needs.
        ///
        /// The survey's diagnosis is that the win-rate cliff comes from an absence of VARIANCE, not
        /// from aim being powerful: shots-to-destroy has a standard deviation of about half a shot,
        /// so a 1%p aim edge (0.28 shots) is 0.55 sigma. The closed form is
        /// sd(shots) = sqrt(durability / mean) * CV, where CV is over PER-SHOT DAMAGE.
        ///
        /// Hit rate cannot supply that — it is the same distribution binarised to "zero or not", so
        /// it discards exactly the magnitude CV is made of. And shots-to-destroy cannot be measured
        /// directly: a match ends when the FIRST keep falls, so the loser's shot count is never
        /// observed and what is left is a censored minimum. Per-shot damage has neither problem —
        /// every shot is a sample.
        ///
        /// Sums rather than a list: Welford is unnecessary at these magnitudes and a list would grow
        /// per shot in a ring-buffer-conscious file.
        /// </summary>
        private static float shotMaterialThisTurn;
        private static int playerDamagedShots;
        private static float playerDamageSum;
        private static float playerDamageSumSquares;

        /// <summary>Matches played this session beyond the first — the G7 repeat proxy.</summary>
        public static int RetryCount => Mathf.Max(0, matchesThisSession - 1);

        public static void BeginSession()
        {
            matchesThisSession = 0;
            stagesClearedThisSession = 0;
            blocksThisTurn = 0;
            deepestChainThisTurn = 0;
            playerShots = 0;
            playerHits = 0;
            landedThisTurn = false;
            aiShots = 0;
            aiHits = 0;
            aiLandedThisTurn = false;
            shotMaterialThisTurn = 0f;
            playerDamagedShots = 0;
            playerDamageSum = 0f;
            playerDamageSumSquares = 0f;
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

        /// <summary>
        /// One comeback activation. Emitted immediately rather than accumulated to a turn boundary,
        /// unlike <see cref="BlockDestroyed"/>: the values that matter are both cores AT the instant
        /// of activation, and the buffed shot that follows changes one of them.
        /// </summary>
        public static void Comeback(bool byPlayer, float ownCoreHp, float ownCoreMax, float foeCoreHp, float foeCoreMax)
        {
            if (!Enabled) return;
            Telemetry.Comeback(byPlayer, ownCoreHp, ownCoreMax, foeCoreHp, foeCoreMax);
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

        /// <summary>
        /// One shot's outcome, for the skill measurement the game did not have.
        ///
        /// A "hit" is a shot that removed material from the OPPONENT's keep — not damage dealt,
        /// and not blocks broken. The distinction is load-bearing: a shallow draw fires into the
        /// player's OWN wall (the launch apron sits at ±17 and the keep at 4-7), and B1 measured
        /// that happening on 71% of the player's own turns. Counting blocks would score those as
        /// skill. <see cref="DestructibleBlock"/> already computes the predicate at the point it
        /// awards score: <c>castle.isPlayerCastle != attackerIsPlayer</c>.
        ///
        /// Both sides are counted, for different reasons. The PLAYER's rate is the skill measure
        /// the handicap reads. The AI's rate is the only way to build the conversion the survey
        /// says cannot be derived: `errorOffsetRange` is world metres and the balance model's
        /// aim quality is a 0..1 damage multiplier, with no relation between them in code. Logging
        /// the AI's hit rate against the offset it fired under is what turns +0.35 from a design
        /// choice into a measured effect size.
        /// </summary>
        public static void NoteShotOutcome(bool byPlayer, bool onOpponentKeep)
        {
            if (!Enabled) return;
            if (byPlayer)
            {
                if (landedThisTurn) return;   // one shot per turn; the first landing decides it
                landedThisTurn = onOpponentKeep;
            }
            else
            {
                if (aiLandedThisTurn) return;
                aiLandedThisTurn = onOpponentKeep;
            }
        }

        /// <summary>
        /// Material a shot removed, accumulated until the turn closes. Called from
        /// <see cref="DestructibleBlock"/> for every hit, so a cascade sums into one shot's total —
        /// which is correct: the shot caused the cascade.
        /// </summary>
        public static void NoteMaterialRemoved(bool? byPlayer, float material)
        {
            if (!Enabled) return;
            if (byPlayer != true) return;   // player's shots only; the AI's aim is a tuned constant
            if (material <= 0f) return;
            shotMaterialThisTurn += material;
        }

        /// <summary>
        /// Turn boundary for the skill measure. Separate from <see cref="TurnResolved"/> because
        /// that one runs for both sides and this must attribute per side — a turn nobody shot in is
        /// not a miss for anyone.
        /// </summary>
        public static void PlayerTurnEnded(bool playerFired)
        {
            if (!Enabled) return;
            if (playerFired)
            {
                playerShots++;
                if (landedThisTurn) playerHits++;
                // Zero-damage shots ARE samples: they are most of what makes the distribution
                // heavy-tailed (B1 measured 6 of 14 shots dealing nothing). Dropping them would
                // measure the CV of successful shots, which is not the quantity the model needs.
                playerDamagedShots++;
                playerDamageSum += shotMaterialThisTurn;
                playerDamageSumSquares += shotMaterialThisTurn * shotMaterialThisTurn;
            }
            landedThisTurn = false;
            shotMaterialThisTurn = 0f;
        }

        /// <summary>Mean material a player shot removes, and the coefficient of variation over those
        /// shots. The CV is the number the cliff diagnosis needs; the mean is what turns it into
        /// sd(shots to destroy) via sqrt(durability / mean) * CV.</summary>
        public static float PlayerMeanShotMaterial =>
            playerDamagedShots > 0 ? playerDamageSum / playerDamagedShots : 0f;

        public static float PlayerShotMaterialCv
        {
            get
            {
                if (playerDamagedShots < 2) return 0f;
                float mean = playerDamageSum / playerDamagedShots;
                if (mean <= 0f) return 0f;
                float variance = playerDamageSumSquares / playerDamagedShots - mean * mean;
                return variance <= 0f ? 0f : Mathf.Sqrt(variance) / mean;
            }
        }

        /// <summary>
        /// AI turn boundary. <paramref name="aimErrorUsed"/> is the offset the AI actually fired
        /// under, which is what makes the pair usable: hit rate alone cannot separate a handicap
        /// from the difficulty ramp, since both move the same field.
        /// </summary>
        public static void AiTurnEnded(bool aiFired, float aimErrorUsed)
        {
            if (!Enabled) return;
            if (aiFired)
            {
                aiShots++;
                if (aiLandedThisTurn) aiHits++;
                aiAimErrorSum += aimErrorUsed;
            }
            aiLandedThisTurn = false;
        }

        /// <summary>Shots the player has taken this session, and how many reached the enemy keep.
        /// Read by <see cref="SkillGrading"/>; the sample gate lives there, not here.</summary>
        public static int PlayerShots => playerShots;
        public static int PlayerHits => playerHits;

        /// <summary>The AI's side of the same measure, plus the mean aim error it fired under.
        /// Reported in the dump rather than consumed by any rule — its job is to supply the
        /// metres-to-hit-rate conversion the balance model is missing.</summary>
        public static int AiShots => aiShots;
        public static int AiHits => aiHits;
        public static float AiMeanAimError => aiShots > 0 ? aiAimErrorSum / aiShots : 0f;

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
            Debug.Log(SkillSummary());
            Debug.Log("[telemetry-json] " + Telemetry.ToJson());
        }

        /// <summary>
        /// The skill line: the player's grade inputs, the handicap they earned, and the two figures
        /// the cliff diagnosis needs. Separate from <see cref="Telemetry.Summary"/> because these
        /// are session counters rather than ring events — flushing them into the ring would spend
        /// its capacity on numbers that are already aggregates.
        ///
        /// "n/a" where a sample is too small to mean anything, on the same rule as the win rate:
        /// a reader must never be handed a number that means "no data".
        /// </summary>
        public static string SkillSummary()
        {
            string hit = playerShots > 0 ? $"{(float)playerHits / playerShots * 100f:F1}%" : "n/a";
            string aiHit = aiShots > 0 ? $"{(float)aiHits / aiShots * 100f:F1}%" : "n/a";
            string cv = playerDamagedShots >= 2 ? $"{PlayerShotMaterialCv:F2}" : "n/a";
            var grade = playerShots >= SkillGrading.MinimumShotsForGrade
                ? SkillGrading.GradeForHitRate((float)playerHits / playerShots).ToString()
                : "ungraded";
            return "[telemetry-skill] " +
                   $"playerShots={playerShots} hitRate={hit} grade={grade} " +
                   $"handicap={SkillGrading.HandicapForSample(playerShots, playerHits):F2} " +
                   $"meanShotMaterial={PlayerMeanShotMaterial:F1} cv={cv} " +
                   $"aiShots={aiShots} aiHitRate={aiHit} aiMeanAimError={AiMeanAimError:F2}";
        }
    }
}
