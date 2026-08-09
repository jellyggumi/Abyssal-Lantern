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

        public void TriggerShake(float duration = 0.3f, float magnitude = 0.15f)
        {
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
