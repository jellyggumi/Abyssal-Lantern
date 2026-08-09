using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Keeps the artillery board readable across very wide game-view captures and adds lightweight
    /// runtime polish without requiring authored animation clips.
    /// </summary>
    public class GamePresentationDirector : MonoBehaviour
    {
        public static GamePresentationDirector Instance { get; private set; }

        [Header("Camera Framing")]
        public Vector2 boardCenter = new Vector2(0f, 3.0f);
        public float targetHalfHeight = 8.4f;  // widened board pass (cores ±9, aprons ±14.5)
        public float minHalfHeight = 7.2f;
        public float maxHalfHeight = 11.2f;
        public float desiredWorldWidth = 39f;
        public float followLerp = 3.5f;
        public float focusExtraHeight = 0.9f; // Readjusted from 0.65f to 0.9f (1.4x)

        [Header("Presentation")]
        public bool pixelSnapCamera = true;
        public float backgroundParallax = 0.08f;

        private Camera mainCamera;
        private Vector3 baseCameraPosition;
        private Transform focusTarget;
        private SpriteRenderer backgroundRenderer;
        private const float MaxFocusHorizontalPosition = 7.5f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;

            mainCamera.orthographic = true;
            baseCameraPosition = new Vector3(boardCenter.x, boardCenter.y, mainCamera.transform.position.z);
            FitCameraToAspect();
            mainCamera.transform.position = baseCameraPosition;

            var background = GameObject.Find("Background");
            if (background != null) backgroundRenderer = background.GetComponent<SpriteRenderer>();
            FitBackgroundToCamera();
        }

        private void LateUpdate()
        {
            if (mainCamera == null) return;

            FitCameraToAspect();

            Vector3 targetPosition = baseCameraPosition;
            if (focusTarget != null)
            {
                float clampedX = Mathf.Clamp(focusTarget.position.x, -MaxFocusHorizontalPosition, MaxFocusHorizontalPosition);
                float clampedY = Mathf.Clamp(focusTarget.position.y + focusExtraHeight, -0.25f, 5.2f);
                targetPosition = new Vector3(clampedX, clampedY, baseCameraPosition.z);
            }

            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetPosition, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

            if (pixelSnapCamera)
            {
                var p = mainCamera.transform.position;
                float pixelsPerUnit = 32f;
                p.x = Mathf.Round(p.x * pixelsPerUnit) / pixelsPerUnit;
                p.y = Mathf.Round(p.y * pixelsPerUnit) / pixelsPerUnit;
                mainCamera.transform.position = p;
            }

            if (backgroundRenderer != null)
            {
                Vector3 bgPosition = backgroundRenderer.transform.position;
                bgPosition.x = mainCamera.transform.position.x * backgroundParallax;
                backgroundRenderer.transform.position = bgPosition;
            }
            FitBackgroundToCamera();
        }

        public void Focus(Transform target)
        {
            focusTarget = target;
        }

        public void ClearFocus(Transform target)
        {
            if (focusTarget == target) focusTarget = null;
        }

        public static float CalculateOrthographicSize(float targetHalfHeight, float desiredWorldWidth, float aspect)
        {
            float safeAspect = aspect > 0f ? aspect : 16f / 9f;
            return Mathf.Max(targetHalfHeight, desiredWorldWidth / (2f * safeAspect));
        }

        private void FitCameraToAspect()
        {
            mainCamera.orthographicSize = CalculateOrthographicSize(
                targetHalfHeight, desiredWorldWidth, mainCamera.aspect);
            baseCameraPosition = new Vector3(boardCenter.x, boardCenter.y, mainCamera.transform.position.z);
        }

        private void FitBackgroundToCamera()
        {
            if (backgroundRenderer == null || backgroundRenderer.sprite == null) return;

            Vector2 spriteSize = backgroundRenderer.sprite.bounds.size;
            if (spriteSize.x <= 0.0001f || spriteSize.y <= 0.0001f) return;

            float cameraHalfHeight = mainCamera.orthographicSize;
            float cameraHalfWidth = cameraHalfHeight * mainCamera.aspect;
            float maximumParallaxOffset = MaxFocusHorizontalPosition * Mathf.Abs(1f - backgroundParallax);
            float requiredWidth = 2f * (cameraHalfWidth + maximumParallaxOffset);
            float verticalOffset = Mathf.Abs(backgroundRenderer.transform.position.y - mainCamera.transform.position.y);
            float requiredHeight = 2f * (cameraHalfHeight + verticalOffset);
            float uniformScale = Mathf.Max(requiredWidth / spriteSize.x, requiredHeight / spriteSize.y);

            backgroundRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }
    }
}
