using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Siege background music: one looping battle bed plus a victory/defeat stinger.
    ///
    /// Fully self-driving. It boots itself with <see cref="RuntimeInitializeOnLoadMethod"/>
    /// and reads <see cref="GameManager.currentState"/> each frame, so combat, intro, and
    /// results code carry no music call sites and stay owned by their own lanes.
    ///
    /// Timing matters on WebGL: browsers refuse to open an AudioContext until the page has
    /// seen a user gesture, so music must not start during load. Waiting for the match to
    /// actually begin means a click or keypress has already happened. Play() is retried
    /// while the bed should be running but is not, because the very first attempt can still
    /// land before the browser has unblocked audio.
    /// </summary>
    public class BgmManager : MonoBehaviour
    {
        private const string BattleLoopPath = "Audio/BGM/battle-loop";
        private const string VictoryPath = "Audio/BGM/victory";
        private const string DefeatPath = "Audio/BGM/defeat";

        // The bed sits well under the SFX bus: launch/impact cues carry the read of the
        // match, and music that competes with them costs readability (presentation-spec G4).
        private const float LoopVolume = 0.28f;
        private const float StingerVolume = 0.6f;
        private const float DuckSeconds = 0.6f;
        private const float RetrySeconds = 0.5f;

        private static BgmManager instance;

        private AudioSource loopSource;
        private AudioSource stingerSource;
        private AudioClip battleLoop;
        private bool outcomeAnnounced;
        private float duckTimer;
        private float retryTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (instance != null) return;

            var host = new GameObject("BgmManager");
            DontDestroyOnLoad(host);
            host.AddComponent<BgmManager>();
        }

        private void Awake()
        {
            if (instance != null && instance != this) { Destroy(gameObject); return; }
            instance = this;

            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.loop = true;
            loopSource.spatialBlend = 0f;
            loopSource.volume = LoopVolume;

            stingerSource = gameObject.AddComponent<AudioSource>();
            stingerSource.playOnAwake = false;
            stingerSource.loop = false;
            stingerSource.spatialBlend = 0f;
            stingerSource.volume = StingerVolume;
        }

        private void Update()
        {
            // Fade the bed out under the stinger instead of cutting it: an abrupt stop
            // reads as a bug, a fade reads as the match resolving.
            if (duckTimer > 0f && loopSource != null)
            {
                duckTimer -= Time.unscaledDeltaTime;
                loopSource.volume = LoopVolume * Mathf.Clamp01(duckTimer / DuckSeconds);
                if (duckTimer <= 0f) loopSource.Stop();
            }

            var game = GameManager.Instance;
            if (game == null) return;

            switch (game.currentState)
            {
                case GameState.PlayerTurn:
                case GameState.AITurn:
                    // A fresh match after a finished one re-arms the stinger.
                    outcomeAnnounced = false;
                    EnsureLoopPlaying();
                    break;

                case GameState.GameOver:
                    if (!outcomeAnnounced)
                    {
                        outcomeAnnounced = true;
                        PlayOutcomeStinger(PlayerWon());
                    }
                    break;
            }
        }

        private void EnsureLoopPlaying()
        {
            if (loopSource == null || loopSource.isPlaying || duckTimer > 0f) return;

            retryTimer -= Time.unscaledDeltaTime;
            if (retryTimer > 0f) return;
            retryTimer = RetrySeconds;

            if (battleLoop == null)
            {
                battleLoop = Resources.Load<AudioClip>(BattleLoopPath);
                if (battleLoop == null)
                {
                    Debug.LogWarning($"[BgmManager] Missing music clip at Resources/{BattleLoopPath}");
                    enabled = false;
                    return;
                }
            }

            loopSource.clip = battleLoop;
            loopSource.volume = LoopVolume;
            loopSource.Play();
        }

        /// <summary>The match is a loss only when the player's own core is gone; every
        /// other terminal state (enemy core destroyed, enemy wiped) is a win.</summary>
        private static bool PlayerWon()
        {
            foreach (var core in FindObjectsOfType<CastleCoreGimmick>())
            {
                if (core != null && core.isPlayerCore && core.currentHP <= 0f) return false;
            }
            return true;
        }

        private void PlayOutcomeStinger(bool victory)
        {
            if (loopSource != null && loopSource.isPlaying) duckTimer = DuckSeconds;

            var clip = Resources.Load<AudioClip>(victory ? VictoryPath : DefeatPath);
            if (clip == null || stingerSource == null) return;

            stingerSource.clip = clip;
            stingerSource.Play();
        }
    }
}
