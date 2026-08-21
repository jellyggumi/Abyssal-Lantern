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
        private float followWeight;
        private float fittedSize = 1f;

        [Header("Watch The Shot")]
        // Impact linger: after a tracked shot settles or dies, hold the frame on the impact
        // for a beat before easing home. Cancelled instantly by a new Focus, the player
        // starting to draw, or game over.
        public float lingerSeconds = 0.8f;
        private float lingerRemaining;
        private Vector3 lingerWorldPosition;

        [Header("Presentation")]
        public bool pixelSnapCamera = true;
        public float backgroundParallax = 0.08f;

        private Camera mainCamera;
        private Vector3 baseCameraPosition;
        // The follow lerp runs on this unshaken rig position; shake is added after, as a
        // pure offset, so a mid-flight shake can never freeze the lerp or restore a stale
        // frame (the old ScreenShakeManager absolute-write bug).
        private Vector3 smoothedPosition;
        private Transform focusTarget;
        private SpriteRenderer backgroundRenderer;

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
            smoothedPosition = baseCameraPosition;

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

            // Priority: Aiming > Tracking > Linger > Overview. The player taking the sling
            // (or the results card) cancels a held impact frame outright — aim framing is
            // the one camera rule that must never be preempted.
            var gm = GameManager.Instance;
            if (aiming || (gm != null && gm.currentState == GameState.GameOver)) lingerRemaining = 0f;

            bool tracking = focusTarget != null && !aiming;
            bool lingering = !tracking && lingerRemaining > 0f;
            if (lingering) lingerRemaining -= Time.deltaTime;

            // While lingering the frame freezes: the follow zoom holds at its released value
            // so position and zoom ease home together only after the beat has landed.
            if (!lingering)
            {
                followWeight = CameraFraming.EaseFollowWeight(followWeight, tracking, Time.unscaledDeltaTime);
            }

            FitCameraToAspect();

            Vector3 targetPosition = baseCameraPosition;
            if (tracking)
            {
                targetPosition = ClampedFocusPosition(focusTarget.position);
            }
            else if (lingering)
            {
                targetPosition = ClampedFocusPosition(lingerWorldPosition);
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

            smoothedPosition = Vector3.Lerp(smoothedPosition, targetPosition, 1f - Mathf.Exp(-followLerp * Time.deltaTime));

            Vector3 rendered = smoothedPosition;
            if (ScreenShakeManager.Instance != null) rendered += ScreenShakeManager.Instance.CurrentOffset;

            if (pixelSnapCamera)
            {
                float pixelsPerUnit = 32f;
                rendered.x = Mathf.Round(rendered.x * pixelsPerUnit) / pixelsPerUnit;
                rendered.y = Mathf.Round(rendered.y * pixelsPerUnit) / pixelsPerUnit;
            }
            mainCamera.transform.position = rendered;

            if (backgroundRenderer != null)
            {
                Vector3 bgPosition = backgroundRenderer.transform.position;
                bgPosition.x = mainCamera.transform.position.x * backgroundParallax;
                backgroundRenderer.transform.position = bgPosition;
            }
            FitBackgroundToCamera();
        }

        /// <summary>
        /// Where the camera may sit to watch a world position. Horizontal travel is computed
        /// from what the tightened frame actually hides — half the authored board width minus
        /// the visible half width — so the follow zoom can ride a shot all the way to the
        /// keeps without ever showing void past the board edge. The Y band is the original
        /// authored clamp, unchanged.
        /// </summary>
        private Vector3 ClampedFocusPosition(Vector3 worldPosition)
        {
            float travel = CameraFraming.MaxFocusTravel(
                desiredWorldWidth * 0.5f, mainCamera.orthographicSize, mainCamera.aspect);
            float clampedX = Mathf.Clamp(worldPosition.x, boardCenter.x - travel, boardCenter.x + travel);
            float clampedY = Mathf.Clamp(worldPosition.y + focusExtraHeight, -0.25f, 5.2f);
            return new Vector3(clampedX, clampedY, baseCameraPosition.z);
        }

        public void Focus(Transform target)
        {
            focusTarget = target;
            lingerRemaining = 0f; // a live shot always outranks a held impact frame
        }

        /// <summary>
        /// Tracked-shot handoff: instead of snapping home the moment the shot settles or
        /// dies, hold the frame on the impact for <see cref="lingerSeconds"/>, then ease
        /// back. Same guard as <see cref="ClearFocus"/> — only the transform currently
        /// being tracked may release it.
        /// </summary>
        public void ReleaseFocus(Transform target)
        {
            if (target == null || focusTarget != target) return;
            focusTarget = null;
            lingerWorldPosition = target.position;
            lingerRemaining = lingerSeconds;
        }

        /// <summary>
        /// Re-centres and restarts the linger on a blast position — chained keg explosions
        /// each pull the held frame to the newest detonation. Never preempts a live tracked
        /// shot, the player drawing, or the results screen.
        /// </summary>
        public void RefreshLinger(Vector3 position)
        {
            if (focusTarget != null || IsPlayerDrawing()) return;
            var gm = GameManager.Instance;
            if (gm != null && gm.currentState == GameState.GameOver) return;
            lingerWorldPosition = position;
            lingerRemaining = lingerSeconds;
        }

        /// <summary>Instant cancel — tracking stops with no linger.</summary>
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
            // The fitted size is the board-must-fit rule; player zoom, aim framing and the
            // shot-follow zoom are multipliers ON that fit, never replacements for it, so no
            // combination of scroll, aiming and tracking can crop the field below the
            // authored framing — except the follow zoom's deliberate tighten (< 1), which is
            // forced back to 1 whenever the player is drawing.
            fittedSize = CalculateOrthographicSize(targetHalfHeight, desiredWorldWidth, mainCamera.aspect);
            float zoom = CameraFraming.ClampZoom(playerZoom)
                * CameraFraming.AimZoomMultiplier(aimWeight)
                * CameraFraming.FollowZoomMultiplier(followWeight);
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
            // Worst-case horizontal camera excursion: the follow zoom's tightest frame
            // (MinZoom * FollowZoomIn) opens the most focus travel, and aim framing slides
            // at most AimShiftWeight of the way to a sling that sits within the board.
            float tightestOrthoSize = fittedSize * CameraFraming.MinZoom * CameraFraming.FollowZoomIn;
            float boardHalfWidth = desiredWorldWidth * 0.5f;
            float maxCameraTravel = Mathf.Max(
                CameraFraming.MaxFocusTravel(boardHalfWidth, tightestOrthoSize, mainCamera.aspect),
                boardHalfWidth * CameraFraming.AimShiftWeight);
            float maximumParallaxOffset = maxCameraTravel * Mathf.Abs(1f - backgroundParallax);
            float requiredWidth = 2f * (cameraHalfWidth + maximumParallaxOffset);
            float verticalOffset = Mathf.Abs(backgroundRenderer.transform.position.y - mainCamera.transform.position.y);
            float requiredHeight = 2f * (cameraHalfHeight + verticalOffset);
            float uniformScale = Mathf.Max(requiredWidth / spriteSize.x, requiredHeight / spriteSize.y);

            backgroundRenderer.transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }
    }
}
