using UnityEngine;
using System.Collections;

namespace CastleBusters
{
    public class ScreenShakeManager : MonoBehaviour
    {
        public static ScreenShakeManager Instance { get; private set; }

        private Vector3 originalPos;
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
                var mainCamera = Camera.main;
                if (mainCamera == null) return;

                if (shakeCoroutine != null)
                {
                    StopCoroutine(shakeCoroutine);
                    mainCamera.transform.localPosition = originalPos;
                }
                else
                {
                    originalPos = mainCamera.transform.localPosition;
                }

                shakeCoroutine = StartCoroutine(ExecuteShake(mainCamera, duration, magnitude));
            }
        }

        private IEnumerator ExecuteShake(Camera mainCamera, float duration, float magnitude)
        {
            float elapsed = 0.0f;

            while (elapsed < duration)
            {
                mainCamera.transform.localPosition = originalPos + new Vector3(Random.Range(-1f, 1f) * magnitude, Random.Range(-1f, 1f) * magnitude, 0f);
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            mainCamera.transform.localPosition = originalPos;
            shakeCoroutine = null;
        }
    }
}
