using UnityEngine;
using TMPro;
using System.Collections.Generic;

namespace CastleBusters
{
    public class LaunchManager : MonoBehaviour
    {
        [Header("Launch Settings")]
        public Transform launchPoint;
        public float launchActivationRadius = 3.5f;
        public float maxDragDistance = 4.2f;
        public float launchForceMultiplier = 6.0f;
        public float maxLaunchVelocity = 32f;
        public float minLaunchVelocity = 0.75f;

        [Header("Trajectory Line")]
        public LineRenderer trajectoryLine;
        public int trajectoryResolution = 30;
        public float timeStep = 0.1f;

        [Header("Visuals")]
        public GameObject impactMarkerPrefab;
        public GameObject launchPointIndicatorPrefab;
        public TMP_Text launchStatsText;
        public TMP_Text controlGuideText;
        public LineRenderer rubberBandLine;

        private Vector2 dragStartPos;
        private Vector2 launchVelocity;
        private bool isDragging;
        private GameObject selectedUnitPrefab;

        private GameObject impactMarkerInstance;
        private GameObject launchPointIndicatorInstance;
        private GameObject invalidStartMarkerInstance;
        private LineRenderer boundaryLine;
        private float boundaryFlashTimer = 0f;
        private float invalidStartMarkerTimer;
        private readonly Color boundaryNormalColor = new Color(0.2f, 0.6f, 1f, 0.35f);
        private readonly Color boundaryFlashColor = new Color(1f, 0.2f, 0.2f, 0.95f);
        private TextMeshProUGUI launchAlertText;
        private TextMeshPro launchPointHintLabel;
        private string selectedUnitName = "Knight";

        /// <summary>True while the player is actively drawing the bowstring (drag in progress).</summary>
        public bool IsAiming => isDragging;

        private Sprite CreateCircleSprite(float radius, Color color)
        {
            int size = 32;
            var tex = new Texture2D(size, size);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    tex.SetPixel(x, y, dist < center ? color : Color.clear);
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size / (radius * 2f));
        }

        private string BuildControlGuideText()
        {
            // One compact line: playtest feedback flagged the old three-line bilingual block
            // as HUD noise that pulled focus from the battlefield. Numeral hints use the same
            // gold as the selected unit-card border so the guide visually maps to the row above it.
            return $"<b>{selectedUnitName.ToUpperInvariant()}</b> 준비  ·  <color=#FFC73D>1 기사 · 2 궁수 · 3 폭탄병</color>  ·  푸른 링에서 드래그 → 발사";
        }


        public float GetPullTensionRatio(Vector2 pointerWorldPosition)
        {
            float dragDistance = Vector2.Distance(GetLaunchPosition(), pointerWorldPosition);
            return Mathf.Clamp01(dragDistance / Mathf.Max(0.01f, maxDragDistance));
        }

        private void SetupDefaultVisuals()
        {
            if (launchPoint != null && launchPointIndicatorPrefab != null)
            {
                launchPointIndicatorInstance = Instantiate(launchPointIndicatorPrefab, launchPoint.position, Quaternion.identity, launchPoint);
            }
            else if (launchPoint != null)
            {
                var go = new GameObject("DefaultLaunchPointIndicator");
                go.transform.position = launchPoint.position;
                go.transform.SetParent(launchPoint);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(0.55f, new Color(0.35f, 0.9f, 1f, 0.82f));
                sr.sortingOrder = 8;
                launchPointIndicatorInstance = go;

                // Launch-gate portal (§5): dedicated gti-generated frame animation replaces
                // the flat circle when the art exists (min 5 frames, 8fps loop). Fails soft
                // to the procedural ring on missing art.
                var gateFrames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.LaunchGateAnim);
                if (gateFrames != null && gateFrames.Length >= 2)
                {
                    sr.sprite = gateFrames[0];
                    float native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                    if (native > 0.0001f)
                    {
                        float s = 2.4f / native; // portal spans ~2.4u over the muzzle
                        go.transform.localScale = new Vector3(s, s, 1f);
                    }
                    GimmickFrameAnimator.TryAttach(go, GimmickAnimLibrary.LaunchGateAnim, 8f);
                }
            }

            if (launchPoint != null && launchPointHintLabel == null)
            {
                var hintGo = new GameObject("LaunchPointHintLabel");
                hintGo.transform.SetParent(launchPoint, false);
                hintGo.transform.localPosition = new Vector3(0f, 1.45f, 0f);
                launchPointHintLabel = hintGo.AddComponent<TextMeshPro>();
                launchPointHintLabel.alignment = TextAlignmentOptions.Center;
                launchPointHintLabel.fontSize = 2.4f;
                launchPointHintLabel.sortingOrder = 18;
                launchPointHintLabel.color = new Color(0.75f, 0.95f, 1f, 0.92f);
                launchPointHintLabel.text = "▼ 여기서 장전 ▼";
            }

            if (impactMarkerPrefab != null)
            {
                impactMarkerInstance = Instantiate(impactMarkerPrefab);
                impactMarkerInstance.SetActive(false);
            }
            else
            {
                var go = new GameObject("DefaultImpactMarker");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(0.22f, new Color(1f, 0.25f, 0.15f, 0.9f));
                sr.sortingOrder = 12;
                go.SetActive(false);
                impactMarkerInstance = go;
            }

            if (invalidStartMarkerInstance == null)
            {
                var go = new GameObject("InvalidLaunchStartMarker");
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = CreateCircleSprite(0.32f, new Color(1f, 0.1f, 0.05f, 0.82f));
                sr.sortingOrder = 13;
                go.SetActive(false);
                invalidStartMarkerInstance = go;
            }

            if (rubberBandLine == null)
            {
                var go = new GameObject("DefaultRubberBandLine");
                go.transform.SetParent(transform);
                rubberBandLine = go.AddComponent<LineRenderer>();
                rubberBandLine.positionCount = 0;
                rubberBandLine.startWidth = 0.10f;
                rubberBandLine.endWidth = 0.06f;
                rubberBandLine.material = new Material(Shader.Find("Sprites/Default"));
                rubberBandLine.startColor = new Color(1f, 1f, 1f, 0.75f);
                rubberBandLine.endColor = new Color(0.55f, 0.85f, 1f, 0.45f);
                rubberBandLine.sortingOrder = 11;
            }

            if (launchStatsText == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var go = new GameObject("LaunchStatsText");
                    go.transform.SetParent(canvas.transform, false);
                    var textComp = go.AddComponent<TextMeshProUGUI>();
                    textComp.fontSize = 24;
                    textComp.color = Color.white;
                    textComp.alignment = TextAlignmentOptions.Center;

                    var rectTransform = go.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 0.15f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.15f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = Vector2.zero;

                    launchStatsText = textComp;
                }
            }

            if (controlGuideText == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var go = new GameObject("ControlGuideText");
                    go.transform.SetParent(canvas.transform, false);
                    var textComp = go.AddComponent<TextMeshProUGUI>();
                    textComp.fontSize = 22;
                    textComp.color = new Color(0.8f, 0.95f, 1f, 0.95f);
                    textComp.outlineWidth = 0.18f;
                    textComp.outlineColor = new Color(0.02f, 0.015f, 0.01f, 0.95f);
                    textComp.alignment = TextAlignmentOptions.Left;
                    textComp.text = BuildControlGuideText();

                    var rectTransform = go.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.02f, 0.02f);
                    rectTransform.anchorMax = new Vector2(0.82f, 0.02f);
                    rectTransform.pivot = new Vector2(0f, 0f);
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.sizeDelta = new Vector2(0f, 72f);

                    controlGuideText = textComp;
                }
            }

            if (controlGuideText != null)
            {
                controlGuideText.gameObject.SetActive(true);
            }

            if (launchStatsText != null)
            {
                launchStatsText.gameObject.SetActive(false);
            }

            if (launchAlertText == null)
            {
                var canvas = FindObjectOfType<Canvas>();
                if (canvas != null)
                {
                    var go = new GameObject("LaunchAlertText");
                    go.transform.SetParent(canvas.transform, false);
                    launchAlertText = go.AddComponent<TextMeshProUGUI>();
                    launchAlertText.fontSize = 26;
                    launchAlertText.color = new Color(1f, 0.25f, 0.2f, 1f);
                    launchAlertText.alignment = TextAlignmentOptions.Center;
                    launchAlertText.text = "";

                    var rectTransform = go.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.5f, 0.30f);
                    rectTransform.anchorMax = new Vector2(0.5f, 0.30f);
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.sizeDelta = new Vector2(680f, 70f);
                }
            }

            if (boundaryLine == null)
            {
                var go = new GameObject("DefaultBoundaryLine");
                go.transform.SetParent(transform);
                boundaryLine = go.AddComponent<LineRenderer>();
                boundaryLine.positionCount = 73;
                boundaryLine.startWidth = 0.10f;
                boundaryLine.endWidth = 0.10f;
                boundaryLine.loop = true;
                boundaryLine.material = new Material(Shader.Find("Sprites/Default"));
                boundaryLine.startColor = boundaryNormalColor;
                boundaryLine.endColor = boundaryNormalColor;
                boundaryLine.sortingOrder = 7;
                UpdateBoundaryLineGeometry();
            }
        }

        private void UpdateBoundaryLineGeometry()
        {
            if (boundaryLine == null) return;

            boundaryLine.positionCount = 73;
            Vector3[] points = new Vector3[73];
            Vector2 center = GetLaunchPosition();
            for (int i = 0; i <= 72; i++)
            {
                float angle = i * 5f * Mathf.Deg2Rad;
                points[i] = new Vector3(center.x + Mathf.Cos(angle) * launchActivationRadius, center.y + Mathf.Sin(angle) * launchActivationRadius, 0f);
            }
            boundaryLine.SetPositions(points);
        }

        private void UpdateLaunchStats(Vector2 velocity)
        {
            if (launchStatsText == null) return;
            float forcePercent = maxLaunchVelocity > 0f ? (velocity.magnitude / maxLaunchVelocity) * 100f : 0f;

            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            string ready = velocity.magnitude >= minLaunchVelocity ? "발사!" : "더 당기기";
            launchStatsText.text = $"<b>{ready}</b>  ·  파워 {forcePercent:F0}% · 각도 {angle:F0}°";
            launchStatsText.color = velocity.magnitude >= minLaunchVelocity ? new Color(0.92f, 0.98f, 1f, 1f) : new Color(1f, 0.72f, 0.24f, 1f);
            launchStatsText.gameObject.SetActive(true);
        }

        private void HideLaunchStats()
        {
            if (launchStatsText != null)
            {
                launchStatsText.gameObject.SetActive(false);
            }
        }

        private void UpdateRubberBand(Vector2 pointerPos)
        {
            if (rubberBandLine == null) return;

            float tensionRatio = GetPullTensionRatio(pointerPos);
            Vector2 anchor = GetLaunchPosition();
            Vector2 pull = Vector2.ClampMagnitude(pointerPos - anchor, maxDragDistance);
            Vector2 clampedPointer = anchor + pull;

            rubberBandLine.positionCount = 3;
            rubberBandLine.SetPosition(0, new Vector3(anchor.x, anchor.y + 0.28f, 0));
            rubberBandLine.SetPosition(1, new Vector3(clampedPointer.x, clampedPointer.y, 0));
            rubberBandLine.SetPosition(2, new Vector3(anchor.x, anchor.y - 0.28f, 0));

            Color tensionColor = Color.Lerp(new Color(0.35f, 0.85f, 1f, 0.85f), new Color(1f, 0.25f, 0.15f, 0.9f), tensionRatio);
            rubberBandLine.startColor = tensionColor;
            rubberBandLine.endColor = new Color(tensionColor.r, tensionColor.g, tensionColor.b, 0.35f);
        }

        private void HideRubberBand()
        {
            if (rubberBandLine != null)
            {
                rubberBandLine.positionCount = 0;
            }
        }

        private void UpdateImpactMarker(bool active, Vector2 position = default)
        {
            if (impactMarkerInstance == null) return;
            if (active)
            {
                impactMarkerInstance.transform.position = new Vector3(position.x, position.y, 0);
                impactMarkerInstance.SetActive(true);
            }
            else
            {
                impactMarkerInstance.SetActive(false);
            }
        }

        private void CleanUpVisuals()
        {
            HideLaunchStats();
            HideRubberBand();
            UpdateImpactMarker(false);
        }

        private void OnDestroy()
        {
            if (impactMarkerInstance != null) Destroy(impactMarkerInstance);
            if (invalidStartMarkerInstance != null) Destroy(invalidStartMarkerInstance);
            if (launchPointIndicatorInstance != null) Destroy(launchPointIndicatorInstance);
            if (launchStatsText != null && launchStatsText.gameObject.name == "LaunchStatsText") Destroy(launchStatsText.gameObject);
            if (launchAlertText != null && launchAlertText.gameObject.name == "LaunchAlertText") Destroy(launchAlertText.gameObject);
        }

        private void Start()
        {
            SetupDefaultVisuals();
            if (trajectoryLine != null)
            {
                trajectoryLine.positionCount = 0;
                trajectoryLine.startWidth = 0.12f;
                trajectoryLine.endWidth = 0.08f;
                trajectoryLine.sortingLayerName = "Default";
                trajectoryLine.sortingOrder = 10;
                if (trajectoryLine.sharedMaterial == null)
                {
                    trajectoryLine.material = new Material(Shader.Find("Sprites/Default"));
                }
                trajectoryLine.startColor = new Color(1f, 1f, 1f, 0.9f);
                trajectoryLine.endColor = new Color(0.5f, 0.8f, 1f, 0.25f);
            }
        }

        public void SetSelectedUnit(GameObject unitPrefab)
        {
            selectedUnitPrefab = unitPrefab;
            selectedUnitName = ResolveSelectedUnitName(unitPrefab);
            if (controlGuideText != null) controlGuideText.text = BuildControlGuideText();
            if (launchPointHintLabel != null) launchPointHintLabel.text = $"▼ {selectedUnitName} 장전 ▼";
        }

        private string ResolveSelectedUnitName(GameObject unitPrefab)
        {
            if (unitPrefab != null && unitPrefab.TryGetComponent<UnitController>(out var unit))
            {
                return unit.unitType.ToString();
            }

            if (unitPrefab != null && !string.IsNullOrWhiteSpace(unitPrefab.name))
            {
                return unitPrefab.name.Replace("(Clone)", string.Empty).Trim();
            }

            return "Unit";
        }

        private void Update()
        {
            if (GameManager.Instance?.IsPlayerTurn == true && selectedUnitPrefab != null) HandleInput();

            // Cycle 13: Animate trajectory line color over time to make it feel alive
            if (isDragging && trajectoryLine != null && trajectoryLine.positionCount > 0)
            {
                float offset = Time.time * 2.5f;
                Color startCol = Color.Lerp(new Color(1f, 1f, 1f, 0.95f), new Color(0.35f, 0.85f, 1f, 0.95f), Mathf.Sin(offset) * 0.5f + 0.5f);
                Color endCol = new Color(startCol.r, startCol.g, startCol.b, 0.15f);
                trajectoryLine.startColor = startCol;
                trajectoryLine.endColor = endCol;
            }

            bool isPlayerTurn = GameManager.Instance?.IsPlayerTurn == true;

            if (launchPointIndicatorInstance != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * 6f) * 0.18f;
                launchPointIndicatorInstance.transform.localScale = new Vector3(pulse, pulse, 1f);
                launchPointIndicatorInstance.SetActive(isPlayerTurn);

                var sr = launchPointIndicatorInstance.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    // Drag-start affordance: raised the floor so the ring never dims below a
                    // clearly-visible glow, keeping it legible as the "start here" hint at rest.
                    float alpha = 0.58f + Mathf.Sin(Time.time * 8f) * 0.26f;
                    sr.color = new Color(0.35f, 0.9f, 1f, alpha);
                }
            }

            if (launchPointHintLabel != null)
            {
                launchPointHintLabel.gameObject.SetActive(isPlayerTurn && !isDragging);
                launchPointHintLabel.transform.localPosition = new Vector3(0f, 1.45f + Mathf.Sin(Time.time * 7f) * 0.16f, 0f);
                launchPointHintLabel.text = $"▼ {selectedUnitName} 장전 ▼";
            }

            UpdateBoundaryLineGeometry();

            if (invalidStartMarkerTimer > 0f)
            {
                invalidStartMarkerTimer -= Time.deltaTime;
                float pulse = 1f + Mathf.Sin(Time.time * 18f) * 0.25f;
                if (invalidStartMarkerInstance != null)
                {
                    invalidStartMarkerInstance.transform.localScale = Vector3.one * pulse;
                    if (invalidStartMarkerTimer <= 0f) invalidStartMarkerInstance.SetActive(false);
                }
            }

            if (boundaryFlashTimer > 0f)
            {
                boundaryFlashTimer -= Time.deltaTime;
                float t = Mathf.Clamp01(boundaryFlashTimer);
                Color c = Color.Lerp(boundaryNormalColor, boundaryFlashColor, t);
                if (boundaryLine != null)
                {
                    boundaryLine.startColor = c;
                    boundaryLine.endColor = c;
                }
                if (boundaryFlashTimer <= 0f && controlGuideText != null)
                {
                    controlGuideText.text = BuildControlGuideText();
                    controlGuideText.color = new Color(0.8f, 0.95f, 1f, 0.95f);
                    if (launchAlertText != null) launchAlertText.text = "";
                }
            }
        }

        private void HandleInput()
        {
            if (!TryReadPointer(out var pointerWorldPos, out var pressed, out var held, out var released)) return;

            if (pressed)
            {
                if (IsWithinLaunchAffordance(pointerWorldPos))
                {
                    if (UnityEngine.EventSystems.EventSystem.current != null)
                    {
                        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
                    }

                    isDragging = true;
                    dragStartPos = GetLaunchPosition();
                    launchVelocity = Vector2.zero;
                    if (trajectoryLine != null) trajectoryLine.positionCount = trajectoryResolution;
                    if (launchAlertText != null) launchAlertText.text = "DRAW THE SIEGE LINE";
                    if (controlGuideText != null)
                    {
                        controlGuideText.text = $"{selectedUnitName} 조준 중 — 궤적과 바람을 보고 발사";
                        controlGuideText.color = new Color(0.94f, 0.98f, 1f, 0.95f);
                    }
                }
                else
                {
                    if (UnityEngine.EventSystems.EventSystem.current == null || !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                    {
                        TriggerBoundaryFlash(pointerWorldPos);
                    }
                }
            }

            if (isDragging && held)
            {
                launchVelocity = CalculateLaunchVelocity(pointerWorldPos);
                DrawTrajectory(launchVelocity);
                UpdateRubberBand(pointerWorldPos);
                UpdateLaunchStats(launchVelocity);
            }

            if (isDragging && released)
            {
                isDragging = false;
                if (trajectoryLine != null) trajectoryLine.positionCount = 0;
                CleanUpVisuals();
                if (launchVelocity.magnitude >= minLaunchVelocity)
                {
                    LaunchUnit();
                }
                else
                {
                    TriggerWeakLaunchFeedback();
                }
            }
        }

        public void TriggerBoundaryFlash(Vector2? invalidWorldPosition = null)
        {
            boundaryFlashTimer = 1.0f;
            if (controlGuideText != null)
            {
                controlGuideText.text = "⚠️ 푸른 링 안에서 드래그를 시작하세요";
                controlGuideText.color = Color.red;
            }
            if (launchAlertText != null)
            {
                launchAlertText.text = "⚠️ 푸른 링으로 돌아가세요";
            }
            if (invalidWorldPosition.HasValue && invalidStartMarkerInstance != null)
            {
                invalidStartMarkerInstance.transform.position = new Vector3(invalidWorldPosition.Value.x, invalidWorldPosition.Value.y, 0f);
                invalidStartMarkerInstance.transform.localScale = Vector3.one;
                invalidStartMarkerInstance.SetActive(true);
                invalidStartMarkerTimer = 0.65f;
            }
        }

        private void TriggerWeakLaunchFeedback()
        {
            boundaryFlashTimer = 0.45f;
            if (launchAlertText != null) launchAlertText.text = "더 깊게 당긴 뒤 발사";
            if (controlGuideText != null)
            {
                controlGuideText.text = BuildControlGuideText();
                controlGuideText.color = new Color(1f, 0.78f, 0.25f, 0.95f);
            }
        }

        public bool IsWithinLaunchAffordance(Vector2 worldPosition)
        {
            return launchPoint != null && Vector2.Distance(worldPosition, launchPoint.position) <= launchActivationRadius;
        }

        public Vector2 CalculateLaunchVelocity(Vector2 pointerWorldPosition)
        {
            Vector2 dragVector = pointerWorldPosition - GetLaunchPosition();
            Vector2 clampedDrag = Vector2.ClampMagnitude(dragVector, maxDragDistance);
            Vector2 velocity = clampedDrag * launchForceMultiplier;
            float cappedMagnitude = Mathf.Min(maxLaunchVelocity, velocity.magnitude);
            return velocity.sqrMagnitude > 0.0001f ? velocity.normalized * cappedMagnitude : Vector2.zero;
        }

        public Vector2 GetLaunchPosition()
        {
            return launchPoint != null ? (Vector2)launchPoint.position : (Vector2)transform.position;
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        private bool useSimulatedPointer = false;
        private Vector2 simulatedPointerWorldPos;
        private bool simulatedPressed;
        private bool simulatedHeld;
        private bool simulatedReleased;

        public void SetSimulatedPointer(Vector2 worldPos, bool pressed, bool held, bool released)
        {
            useSimulatedPointer = true;
            simulatedPointerWorldPos = worldPos;
            simulatedPressed = pressed;
            simulatedHeld = held;
            simulatedReleased = released;
        }

        public void ClearSimulatedPointer()
        {
            useSimulatedPointer = false;
        }
#endif

        private bool TryReadPointer(out Vector2 worldPosition, out bool pressed, out bool held, out bool released)
        {
            worldPosition = default;
            pressed = held = released = false;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (useSimulatedPointer)
            {
                worldPosition = simulatedPointerWorldPos;
                pressed = simulatedPressed;
                held = simulatedHeld;
                released = simulatedReleased;
                return pressed || held || released;
            }
#endif

            var cam = Camera.main;
            if (cam == null) return false;

            // Legacy Input Manager is used because the new Input System package is not installed in the project.
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                worldPosition = cam.ScreenToWorldPoint(touch.position);
                pressed = touch.phase == TouchPhase.Began;
                held = touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary;
                released = touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled;
                return true;
            }

            worldPosition = cam.ScreenToWorldPoint(Input.mousePosition);
            pressed = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            released = Input.GetMouseButtonUp(0);
            return pressed || held || released;
        }

        private void DrawTrajectory(Vector2 velocity)
        {
            if (trajectoryLine == null) return;
            Vector2 startPos = GetLaunchPosition();
            Vector2 gravity = Physics2D.gravity;
            float wind = GameManager.Instance != null ? GameManager.Instance.currentWindForce : 0f;

            float mass = 1f;
            if (selectedUnitPrefab != null && selectedUnitPrefab.TryGetComponent<Rigidbody2D>(out var prefabRb))
            {
                // Match the runtime mass reduction UnitController.Awake() applies on spawn
                // (see UnitController.RuntimeMassScale) so the previewed arc matches how the
                // projectile actually flies once wind is factored in.
                mass = Mathf.Max(UnitController.MinRuntimeMass, prefabRb.mass * UnitController.RuntimeMassScale);
            }

            float windAccel = wind / mass;

            Vector2 prevPoint = startPos;
            bool hitDetected = false;
            Vector2 hitPoint = Vector2.zero;
            List<Vector3> points = new List<Vector3>();
            points.Add(new Vector3(startPos.x, startPos.y, 0));

            for (int i = 1; i < trajectoryResolution; i++)
            {
                float t = i * timeStep;
                Vector2 point = startPos + velocity * t + 0.5f * (gravity + new Vector2(windAccel, 0f)) * (t * t);

                if (!hitDetected)
                {
                    int layerMask = LayerMask.GetMask("Ground", "PlayerCastle", "EnemyCastle");
                    if (layerMask == 0)
                    {
                        layerMask = ~LayerMask.GetMask("PlayerUnit", "EnemyUnit");
                    }

                    RaycastHit2D hit = Physics2D.Linecast(prevPoint, point, layerMask);
                    if (hit.collider != null)
                    {
                        hitDetected = true;
                        hitPoint = hit.point;
                        points.Add(new Vector3(hitPoint.x, hitPoint.y, 0));
                        break;
                    }
                }

                points.Add(new Vector3(point.x, point.y, 0));
                prevPoint = point;
            }

            trajectoryLine.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++)
            {
                trajectoryLine.SetPosition(i, points[i]);
            }

            UpdateImpactMarker(hitDetected, hitPoint);
        }

        private void LaunchUnit()
        {
            if (selectedUnitPrefab == null) return;

            // AOS overhaul (§2): bomber volleys scale with the owner's turn ordinal —
            // 2 bombs from the 3rd own turn, 4 from the 9th. Other units always fire one.
            int volley = 1;
            var gm = GameManager.Instance;
            bool isBomber = selectedUnitPrefab.TryGetComponent<UnitController>(out var proto) && proto.unitType == UnitType.Bomber;
            if (isBomber && gm != null)
            {
                volley = VolleyRules.BomberVolleyCount(VolleyRules.OwnTurnOrdinal(gm.TurnCount));
            }

            var firstUnit = SpawnAndLaunchOne(launchVelocity);
            
            // Set wind effect origin and radius for this launch
            if (GameManager.Instance != null)
            {
                GameManager.Instance.windEffectOrigin = GetLaunchPosition();
                GameManager.Instance.windEffectRadius = 10f;  // Radius of wind effect (adjust as needed)
            }
            
            if (volley > 1)
            {
                GameFeelVfx.SpawnFeedbackLabel(GetLaunchPosition() + Vector2.up * 1.0f,
                    $"x{volley} VOLLEY!", new Color(1f, 0.75f, 0.25f, 1f), 2.2f, 0.6f);
                StartCoroutine(LaunchVolleyRest(volley - 1));
            }

            float powerPercent = Mathf.Clamp01(launchVelocity.magnitude / Mathf.Max(0.01f, maxLaunchVelocity)) * 100f;
            float angle = Mathf.Atan2(launchVelocity.y, launchVelocity.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            GameplayUxDirector.NotifyLaunch(selectedUnitName, powerPercent, angle);
            GameFeelVfx.SpawnShockwaveRing(GetLaunchPosition(), new Color(0.55f, 0.9f, 1f, 0.45f), 1.25f, 0.3f);
            GameFeelVfx.SpawnFeedbackLabel(GetLaunchPosition() + Vector2.up * 0.45f, "LAUNCH!", new Color(0.7f, 0.95f, 1f, 1f), 1.7f, 0.45f);
            if (GameManager.Instance != null) GameManager.Instance.OnUnitLaunched(firstUnit);
        }

        private System.Collections.IEnumerator LaunchVolleyRest(int extraCount)
        {
            for (int i = 0; i < extraCount; i++)
            {
                yield return new WaitForSeconds(0.16f);
                // Slight per-shot jitter so the salvo fans out instead of stacking.
                Vector2 jitter = new Vector2(Random.Range(-0.06f, 0.06f), Random.Range(0f, 0.08f));
                SpawnAndLaunchOne(launchVelocity + launchVelocity.magnitude * jitter);
            }
        }

        private UnitController SpawnAndLaunchOne(Vector2 velocity)
        {
            var unitGo = Instantiate(selectedUnitPrefab, GetLaunchPosition(), Quaternion.identity);
            var unit = unitGo.GetComponent<UnitController>();
            if (unit == null && unitGo.GetComponent<ExplosiveGimmick>() != null)
            {
                unit = unitGo.AddComponent<UnitController>();
                unit.unitType = UnitType.Bomber;
                unit.maxHP = 20f;
                unit.currentHP = 20f;
            }
            // Awake() already auto-fit unitGo's collider to its sprite (see
            // UnitController.ApplyPlayableScaleAndCollider), centered on the spawn transform.
            // GetLaunchPosition() is a ground-level marker, so without this the unit spawns
            // already embedded in the ground/platform and instantly "lands" - see
            // UnitController.SnapColliderAboveGround for the full explanation.
            UnitController.SnapColliderAboveGround(unitGo, GetLaunchPosition().y);
            if (unit != null)

            {
                unit.isPlayerUnit = GameManager.Instance != null ? GameManager.Instance.IsPlayerTurn : true;
                // Barrel gimmicks own their sprite directly (no UnitSpriteAnimator - see
                // UnitController.Awake's isGimmickVisual check), so they need an explicit
                // team recolor now that isPlayerUnit is finally known.
                unitGo.GetComponent<ExplosiveGimmick>()?.ApplyTeamTint(unit.isPlayerUnit);
                unit.Launch(velocity);
                GamePresentationDirector.Instance?.Focus(unit.transform);
            }

            return unit;
        }


        public void SimulateLaunch(Vector2 velocity)
        {
            launchVelocity = velocity;
            LaunchUnit();
        }
    }
}
