using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Sequential campaign unlock rules — pure logic only, mirrors the SiegeRank/
    /// LeaderboardStore split (SiegeEcosystem.cs) so the contract is EditMode-pinnable
    /// (StageProgressionTests) without touching PlayerPrefs. Campaign order is exactly
    /// the StageId enum's declaration order (Stage1 -> Stage2 -> Stage3): Stage1 always
    /// starts unlocked, and clearing a stage unlocks the very next one. This is
    /// orthogonal to StageLayout.locked (a design-time "not finished/offered yet" flag,
    /// StageDefinitions.cs) — a stage must be BOTH structurally unlocked AND
    /// progression-unlocked to be selectable (see GameManager.RequestStage).
    /// </summary>
    public static class StageProgress
    {
        private static readonly StageId[] Order = { StageId.Stage1, StageId.Stage2, StageId.Stage3 };

        /// <summary>True when `stage` is at or before the player's current unlock frontier.</summary>
        public static bool IsUnlocked(StageId highestUnlocked, StageId stage) => (int)stage <= (int)highestUnlocked;

        /// <summary>The stage immediately after `stage` in campaign order, or null when
        /// `stage` is already the last one (nothing left to offer/unlock).</summary>
        public static StageId? NextStage(StageId stage)
        {
            int next = (int)stage + 1;
            return next < Order.Length ? Order[next] : (StageId?)null;
        }

        /// <summary>
        /// Folds a stage clear into the unlock frontier: beating `completed` unlocks
        /// whatever comes right after it. Never regresses — replaying/rematching a stage
        /// earlier than the current frontier is a no-op — and clamps at the final stage
        /// (clearing Stage3 again just stays at Stage3).
        /// </summary>
        public static StageId Advance(StageId highestUnlocked, StageId completed)
        {
            var unlockedByThisClear = NextStage(completed) ?? completed;
            return (int)unlockedByThisClear > (int)highestUnlocked ? unlockedByThisClear : highestUnlocked;
        }
    }

    /// <summary>PlayerPrefs-backed campaign unlock frontier. JSON-free (a single enum
    /// ordinal), key versioned like LeaderboardStore. Not unit-tested directly — same
    /// precedent as LeaderboardStore, which leaves its PlayerPrefs I/O to live PlayMode
    /// verification instead of polluting the editor's actual prefs during EditMode runs.
    /// </summary>
    public static class StageProgressStore
    {
        private const string PrefsKey = "CastleBusters.StageProgress.v1";

        // Session mirror of the frontier. PlayerPrefs is the durable record, but on WebGL it
        // writes to IndexedDB asynchronously and a Save() can be lost to a tab close, a
        // private-browsing profile, or a storage-quota refusal — all silent. Losing it
        // mid-session used to mean the stage the player had JUST cleared into was refused by
        // RequestStage's unlock gate, stranding them on the results screen. The mirror never
        // regresses within a session, so a failed write costs persistence across reloads, not
        // the ability to advance right now.
        private static StageId sessionFrontier = StageId.Stage1;
        private static bool sessionFrontierLoaded;

        public static StageId Load()
        {
            int raw = PlayerPrefs.GetInt(PrefsKey, (int)StageId.Stage1);
            // Defensive clamp: a stale/corrupt/hand-edited pref must never point past the
            // last real stage (or before Stage1).
            int clamped = Mathf.Clamp(raw, (int)StageId.Stage1, (int)StageId.Stage3);
            var stored = (StageId)clamped;

            if (!sessionFrontierLoaded)
            {
                sessionFrontier = stored;
                sessionFrontierLoaded = true;
                return stored;
            }
            // Whichever source is further along wins: a persisted frontier from a previous
            // session, or one earned in this one whose write may not have survived.
            return (int)stored > (int)sessionFrontier ? stored : sessionFrontier;
        }

        public static void Save(StageId highestUnlocked)
        {
            if ((int)highestUnlocked > (int)sessionFrontier) sessionFrontier = highestUnlocked;
            sessionFrontierLoaded = true;
            PlayerPrefs.SetInt(PrefsKey, (int)highestUnlocked);
            PlayerPrefs.Save();
        }

        /// <summary>Test/title hook: forget the session mirror so a fresh profile reads clean.</summary>
        public static void ResetSessionMirror()
        {
            sessionFrontier = StageId.Stage1;
            sessionFrontierLoaded = false;
        }

        /// <summary>Record a stage clear: advances + persists the unlock frontier if the
        /// clear pushed it forward. Returns the (possibly unchanged) highest unlocked
        /// stage so the caller can decide what to offer next (e.g. results-screen
        /// "NEXT STAGE" button).</summary>
        public static StageId RecordVictory(StageId completedStage)
        {
            var current = Load();
            var advanced = StageProgress.Advance(current, completedStage);
            if (advanced != current) Save(advanced);
            return advanced;
        }
    }
}
