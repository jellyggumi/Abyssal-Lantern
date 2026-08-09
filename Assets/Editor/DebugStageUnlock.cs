using UnityEditor;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Dev-only shortcuts for the sequential campaign unlock frontier (StageProgression.cs).
    /// Bypasses the normal "clear Stage1 to unlock Stage2" path so Stage2/Stage3 can be
    /// play-tested directly from the intro screen's stage picker without a full replay.
    /// Writes the same PlayerPrefs key StageProgressStore uses at runtime, so a normal
    /// gameplay clear afterwards still folds in correctly (Advance never regresses).
    /// </summary>
    public static class DebugStageUnlock
    {
        [MenuItem("CastleBusters/Debug/Unlock All Stages")]
        public static void UnlockAll()
        {
            StageProgressStore.Save(StageId.Stage3);
            Debug.Log("[DebugStageUnlock] Unlocked all stages (frontier = Stage3).");
        }

        [MenuItem("CastleBusters/Debug/Reset Stage Progress")]
        public static void ResetProgress()
        {
            StageProgressStore.Save(StageId.Stage1);
            Debug.Log("[DebugStageUnlock] Reset stage progress (frontier = Stage1).");
        }
    }
}
