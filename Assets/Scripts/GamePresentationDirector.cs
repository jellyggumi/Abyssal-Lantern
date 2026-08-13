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
        public float targetHalfHeight = 8.4f;  // widened board pass (cores ±9, aprons ±17.0)
        public float maxHalfHeight = 11.2f;
        public float desiredWorldWidth = 45f;  // widened board pass 2026-08-13 (was 39)
        public float followLerp = 3.5f;
        public float focusExtraHeight = 0.9f; // Readjusted from 0.65f to 0.9f (1.4x)

        [Header("Screen Handling")]
        // Wheel/pinch zoom only — press+drag is the launch gesture (pinned) and must stay
        // unshared, so drag-to-pan is deliberately not offered.
        public bool allowPlayerZoom = true;
        private float playerZoom = CameraFraming.MinZoom;
        private float aimWeight;
        private float fittedSize = 1f;

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

            HandleZoomInput();
            bool aiming = IsPlayerDrawing();
            aimWeight = CameraFraming.EaseAimWeight(aimWeight, aiming, Time.unscaledDeltaTime);

            FitCameraToAspect();

            Vector3 targetPosition = baseCameraPosition;
            if (focusTarget != null)
            {
                float clampedX = Mathf.Clamp(focusTarget.position.x, -MaxFocusHorizontalPosition, MaxFocusHorizontalPosition);
                float clampedY = Mathf.Clamp(focusTarget.position.y + focusExtraHeight, -0.25f, 5.2f);
                targetPosition = new Vector3(clampedX, clampedY, baseCameraPosition.z);
            }
            else if (aimWeight > 0.001f)
            {
                // Aim framing: slide toward the sling the player is pulling so the pouch and
                // the keep being aimed at share the screen. The old fixed frame put the
                // sling at the very edge, so a player pulling could not see their target.
                targetPosition = new Vector3(
                    CameraFraming.AimCenterX(boardCenter.x, PlayerSlingX(), aimWeight),
                    baseCameraPosition.y,
                    baseCameraPosition.z);
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
            // The fitted size is the board-must-fit rule; player zoom and aim framing are
            // multipliers ON that fit, never replacements for it, so no combination of
            // scroll and aiming can crop the field below the authored framing.
            fittedSize = CalculateOrthographicSize(targetHalfHeight, desiredWorldWidth, mainCamera.aspect);
            float zoom = CameraFraming.ClampZoom(playerZoom) * CameraFraming.AimZoomMultiplier(aimWeight);
            mainCamera.orthographicSize = fittedSize * zoom;
            baseCameraPosition = new Vector3(boardCenter.x, boardCenter.y, mainCamera.transform.position.z);
        }

        /// <summary>
        /// Wheel (desktop) and two-finger pinch (touch). Never a drag: a single press and
        /// drag anywhere on the board is the launch gesture, and sharing that channel would
        /// turn every attempted pan into a spent, zero-power volley.
        /// </summary>
        private void HandleZoomInput()
        {
            if (!allowPlayerZoom) return;

            float scroll = Input.mouseScrollDelta.y;
            if (Input.touchCount == 2)
            {
                Touch a = Input.GetTouch(0), b = Input.GetTouch(1);
                float previous = ((a.position - a.deltaPosition) - (b.position - b.deltaPosition)).magnitude;
                float current = (a.position - b.position).magnitude;
                scroll += (current - previous) * 0.01f;
            }

            if (Mathf.Abs(scroll) > 0.001f)
            {
                playerZoom = CameraFraming.ApplyZoomInput(playerZoom, scroll);
            }
        }

        /// <summary>True while the player is drawing the sling — drives aim framing.</summary>
        private bool IsPlayerDrawing()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.currentState != GameState.PlayerTurn || !gm.IsPlayerTurn) return false;
            if (launchManager == null) launchManager = FindObjectOfType<LaunchManager>();
            return launchManager != null && launchManager.IsAiming;
        }

        private LaunchManager launchManager;

        /// <summary>The player's sling x, read from the live ring rules (stage-aware).</summary>
        private static float PlayerSlingX() => LaunchRingRules.PlayerRingX;

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
