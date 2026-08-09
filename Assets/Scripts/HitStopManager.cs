using UnityEngine;
using System.Collections;

namespace CastleBusters
{
    public class HitStopManager : MonoBehaviour
    {
        public static HitStopManager Instance { get; private set; }

        private Coroutine hitStopCoroutine;
        private float originalTimeScale = 1f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnEnable()
        {
            // Domain reloads mid-play wipe statics without re-running Awake; re-register so a
            // recompile during a session never leaves the singleton slot empty.
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>
        /// True only while a turn is actually being played. Hit-stops during the intro card or
        /// the results screen (both intentional Time.timeScale = 0 freezes) must never run:
        /// the realtime restore would flip timeScale back to 1 and un-freeze the overlay,
        /// letting the board fight itself behind the title card (rematch/title dead-scene bug).
        /// </summary>
        private static bool GameplayIsLive()
        {
            var gm = GameManager.Instance;
            if (gm == null) return true; // tests / standalone scenes without a GameManager
            return gm.currentState == GameState.PlayerTurn || gm.currentState == GameState.AITurn;
        }

        /// <summary>Global damper on every freeze. Stacked collapse chains fire hit stop
        /// repeatedly, and at full strength the board reads as stuttering rather than
        /// impactful. Tune here, never per call site.</summary>
        public const float IntensityScale = 0.6f;

        public void TriggerHitStop(float duration = 0.05f)
        {
            duration *= IntensityScale;

            if (!gameObject.activeInHierarchy || !Application.isPlaying) return;
            if (Time.timeScale <= 0f) return; // already frozen intentionally — do not fight it
            if (!GameplayIsLive()) return;

            if (hitStopCoroutine != null) StopCoroutine(hitStopCoroutine);
            hitStopCoroutine = StartCoroutine(ExecuteHitStop(duration));
        }

        /// <summary>Drop any pending restore. Call before an intentional freeze (intro/results).</summary>
        public void CancelPendingHitStop()
        {
            if (hitStopCoroutine != null)
            {
                StopCoroutine(hitStopCoroutine);
                hitStopCoroutine = null;
            }
        }

        private IEnumerator ExecuteHitStop(float duration)
        {
            if (Time.timeScale > 0f)
            {
                originalTimeScale = Time.timeScale;
            }

            Time.timeScale = 0f;
            yield return new WaitForSecondsRealtime(duration);
            // Re-check: an EndGame/ShowIntro freeze may have started during the stop window.
            if (GameplayIsLive()) Time.timeScale = originalTimeScale;
            hitStopCoroutine = null;
        }
    }
}
