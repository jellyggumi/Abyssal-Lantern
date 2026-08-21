using UnityEngine;
using System.Collections;

namespace CastleBusters
{
    /// <summary>
    /// Shake is an OFFSET PROVIDER, not a transform writer. It used to capture the camera
    /// position once and write absolute localPosition — during a follow that froze the lerp
    /// mid-flight and "restored" a position the camera had long since left. Now it only
    /// exposes <see cref="CurrentOffset"/>; GamePresentationDirector adds it after its own
    /// follow lerp (before the pixel snap), so shake and follow compose instead of fighting.
    /// </summary>
    public class ScreenShakeManager : MonoBehaviour
    {
        public static ScreenShakeManager Instance { get; private set; }

        /// <summary>Camera-space jitter this frame. Zero when no shake is running.</summary>
        public Vector3 CurrentOffset { get; private set; }

        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            // Coroutines die with the disable; never leave a stale offset applied.
            shakeCoroutine = null;
            CurrentOffset = Vector3.zero;
        }

        /// <summary>Global damper on every shake in the game. Call sites keep their
        /// relative weighting — a core hit still outshakes a chipped brick — but the
        /// whole channel sits lower, because a board this dense reads worse when the
        /// camera is constantly moving. Tune here, never per call site.</summary>
        public const float IntensityScale = 0.55f;

        public void TriggerShake(float duration = 0.3f, float magnitude = 0.15f)
        {
            magnitude *= IntensityScale;

            if (gameObject.activeInHierarchy && Application.isPlaying)
            {
                if (shakeCoroutine != null)
                {
                    StopCoroutine(shakeCoroutine);
                }

                shakeCoroutine = StartCoroutine(ExecuteShake(duration, magnitude));
            }
        }

        private IEnumerator ExecuteShake(float duration, float magnitude)
        {
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                // Decaying jitter: full punch on the impact frame, easing to nothing so the
                // end of a shake never pops.
                float falloff = 1f - Mathf.Clamp01(elapsed / duration);
                CurrentOffset = new Vector3(
                    Random.Range(-1f, 1f) * magnitude * falloff,
                    Random.Range(-1f, 1f) * magnitude * falloff,
                    0f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            CurrentOffset = Vector3.zero;
            shakeCoroutine = null;
        }
    }
}
