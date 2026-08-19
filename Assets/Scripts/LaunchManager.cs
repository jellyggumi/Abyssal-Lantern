using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

namespace CastleBusters
{
    public class LaunchManager : MonoBehaviour
    {
        [Header("Launch Settings")]
        public Transform launchPoint;
        public float maxDragDistance = 4.2f;
        public float launchForceMultiplier = 6f;
        /// <summary>
        /// Speed cap. Mirrors <see cref="LaunchPowerCurve.MaxSpeed"/> — the curve is the authority
        /// and this field exists so the keyboard aim path and the power readout share one number.
        /// Was 25.2, which covered 64.7u at 45° against 26u of need: most of the pull existed only
        /// to overshoot, and that is what made the usable window six percentage points wide.
        /// </summary>
        public float maxLaunchVelocity = LaunchPowerCurve.MaxSpeed;
        public float minLaunchVelocity = 3f;

        /// <summary>
        /// Speed below which a draw is refused with the "더 깊게 당긴 뒤 발사" coaching.
        ///
        /// Derived from <see cref="LaunchPowerCurve.MinDrawFraction"/> rather than read from
        /// <see cref="minLaunchVelocity"/>, because the contract is a GESTURE: the shallow flick
        /// that used to be refused must keep being refused. A fixed 3 m/s was 11.9% of the draw
        /// under the old linear curve and only 2.9% under this one, so honouring the serialized
        /// number would have quietly deleted the coaching. `minLaunchVelocity` is kept for the
        /// keyboard aim path's own floor and for scene compatibility.
        /// </summary>
        public float EffectiveMinLaunchSpeed => LaunchPowerCurve.MinLaunchSpeed(maxLaunchVelocity);

        [Header("Trajectory Line")]
        public LineRenderer trajectoryLine;
        // 300 × 0.02s = 6 seconds of predicted flight. 150 truncated a full-power lob
        // mid-air on wide stages, so the arc the player pulled against simply ended in
        // the sky — the preview must always reach the impact or leave the board.
        public int trajectoryResolution = 300;
        public float timeStep = 0.02f;

        [Header("Visuals")]
        public GameObject impactMarkerPrefab;
        public GameObject launchPointIndicatorPrefab;
        public TMP_Text launchStatsText;
        public TMP_Text controlGuideText;
        public LineRenderer rubberBandLine;

        private readonly List<Vector3> trajectoryPoints = new List<Vector3>(310);
        private readonly RaycastHit2D[] trajectoryHits = new RaycastHit2D[16];
        private readonly HashSet<EntityId> previewCrossedGateIds = new HashSet<EntityId>(8);

        private Vector2 dragStartPos;
        private Vector2 launchVelocity;
        private bool isDragging;
        private GameObject selectedUnitPrefab;
        private Bounds selectedLaunchBodyBounds = new Bounds(Vector3.zero, new Vector3(0.05f, 0.05f, 0f));
        private bool selectedUnitUsesDeployment;
        [Header("Separated Aim")]
        [Range(10f, 80f)] public float aimAngleDegrees = 45f;

        /// <summary>
        /// Default draw for the keyboard/Space path, raised 0.55 -> 0.82 because 0.55 fired into the
        /// player's OWN keep.
        ///
        /// This path does not use the draw curve. `OneShotSiegeRules.Velocity` takes a linear
        /// `Lerp(minSpeed, maxSpeed, power)`, so at 0.55 the shot left at 10.975 m/s and landed at
        /// x=-4.7 with the player's own wall standing at x=-7..-4.
        ///
        /// 0.82 is the CENTRE of the reaching band, not the lowest reaching value. Measured at 45deg
        /// against the runtime integrator (validated to 0.1% of the closed form), the band that lands
        /// anywhere on the enemy keep is 0.775..0.860 - only 2.12 `powerStep` presses wide. 0.82
        /// leaves 1.12 presses below and 1.00 above; it is the ONLY value in that band with a full
        /// press of room on both sides.
        ///
        /// The designer lane asked for 0.80 so the default would strike the forward outpost at x=4
        /// (`GameManager` :810, "the first thing to fall"). That is not available with margin: the
        /// outpost band bottoms out at 0.775, so 0.80 sits 0.63 presses from falling short and one
        /// press down stops reaching at all. `AimDefaultReachTests` failed on exactly that and the
        /// number moved to the centre. The cost is honest - 0.82 lands at x=5.53, the OUTER course,
        /// so the default no longer nominates the outpost. Widening the band needs a different
        /// `powerStep` or keep geometry, which is a design change, not a default.
        ///
        /// It went stale rather than being wrong when written: task #60 lowered
        /// `LaunchPowerCurve.MaxSpeed` from 25.2 to 17.5, and 0.55 reached at the old speed. A
        /// default that depends on another constant needs a test tying them together, which is what
        /// `AimDefaultReachTests` now does.
        /// design/aim-space-and-preview-verdict.md, qa/evidence/aim-space/trajectory-blockers.md
        /// </summary>
        [Range(0f, 1f)] public float aimPower = 0.82f;
        public float angleStepDegrees = 2f;
        public float powerStep = 0.04f;

        private GameObject impactMarkerInstance;
        private GameObject launchPointIndicatorInstance;

        /// <summary>
        /// The established friendly tint for "this shot hits your own keep" — the same
        /// (0.45, 0.85, 1) already used for a player unit's trail, its launch flash, and its damage
        /// numbers (<see cref="UnitController"/> :495, :520, :1136). A self-hit is the player's own
        /// side stopping the shot, so it wears the player's own colour.
        ///
        /// An earlier revision of this invented amber (1, 0.62, 0.15) to sit beside the damage
        /// numbers. Two things were wrong with that: it was a fourth signal colour where a
        /// perfectly good third existed, and it lands within 0.03 of the Barrel tint
        /// (1, 0.65, 0.12) at :496 — a self-hit would have read as a barrel shot.
        /// </summary>
        private static readonly Color SelfHitTrajectoryColor = new Color(0.45f, 0.85f, 1f, 0.85f);

        // The line's authored colours, captured once at setup so the self-hit tint can be undone
        // without hardcoding a guess at what the designer set.
        private Color authoredTrajectoryStart;
        private Color authoredTrajectoryEnd;
        private bool authoredTrajectoryColorsCaptured;

        // Same reversible-overlay treatment for the impact marker's own colour.
        private Color authoredMarkerColor;
        private bool authoredMarkerColorCaptured;
        // Fitted world scale of the launch affordance, captured at setup. Update() multiplies
        // its breathing pulse into this instead of overwriting it.
        private Vector3 launchPointIndicatorBaseScale = Vector3.one;
        // True when the affordance is textured art (slingshot / gate frames) rather than the
        // procedural cyan ring — art must not be tinted, only alpha-pulsed.
        private bool launchPointIndicatorIsArt;
        // Owns the launcher's dim / windup / recoil. Lazily bound in Update because the
        // affordance may be an authored prefab or built procedurally, and both paths land here.
        private LauncherView playerLauncherView;
        // Weak-pull coaching flash: colors the guide line back to normal when it expires.
        private float weakPullFlashTimer = 0f;
        private int trajectoryCollisionMask;
        private TextMeshProUGUI launchAlertText;
        private TextMeshPro launchPointHintLabel;
        // Player-facing name (Korean, DeploymentRules.DisplayName vocabulary)...
        private string selectedUnitName = "기사";
        // ...and the stable English identifier telemetry aggregates by. Renaming the
        // display must never fork the analytics key ("기사" vs "Knight" would split every
        // per-unit aggregate the balance gates read).
        private string selectedUnitTelemetryName = "Knight";
        // Portrait of the projectile the turn will fire (playtest feedback: the guide
        // showed a raw prefab name like EXPLOSIVEBARREL — an image reads instantly).
        private Image selectedUnitPortrait;
        // Keyboard aim preview is opt-in per turn: at idle the board stays clean; the
        // arc+stats appear once the player actually touches an arrow key.
        private bool keyboardAimTouchedThisTurn;

        /// <summary>True while the player is actively drawing the bowstring (drag in progress).</summary>
        public bool IsAiming => isDragging;

        public void SetAimAngle(float degrees) => aimAngleDegrees = OneShotSiegeRules.ClampAngle(degrees);
        public void SetAimPower(float normalizedPower) => aimPower = OneShotSiegeRules.ClampPower(normalizedPower);
        public void AdjustAimAngle(float deltaDegrees) => SetAimAngle(aimAngleDegrees + deltaDegrees);
        public void AdjustAimPower(float deltaPower) => SetAimPower(aimPower + deltaPower);

        public Vector2 GetSeparatedAimVelocity()
        {
            bool playerTurn = GameManager.Instance == null || GameManager.Instance.IsPlayerTurn;
            return OneShotSiegeRules.Velocity(
                aimAngleDegrees,
                aimPower,
                minLaunchVelocity,
                maxLaunchVelocity,
                playerTurn ? 1f : -1f);
        }

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
            if (selectedUnitUsesDeployment)
            {
                return $"<b>{selectedUnitName}</b> 배치 준비  ·  전장 클릭 → 설치";
            }

            // One compact line: the unit cards already carry roster shortcuts, so this guide
            // only preserves readiness plus the launch gesture the player must perform.
            string guide = $"<b>{selectedUnitName}</b> 준비  ·  아무 곳이나 당겨 발사";

            // The one-shot turn may buy an emplacement INSTEAD of its shot, but a player
            // who is never told that will never find it. Only advertised once the breach
            // requirement is actually met, so the line names an action available now.
            var deployment = DeploymentController.Instance;
            var gm = GameManager.Instance;
            if (gm != null && gm.EnforcesOneShotTurns && deployment != null &&
                DeploymentRules.BreachSatisfied(DeployCard.Cannon, deployment.BreachesFor(true)))
            {
                guide += "  ·  D → 화포 설치(턴 소모)";
            }

            return guide;
        }


        public float GetPullTensionRatio(Vector2 pointerWorldPosition)
        {
            float dragDistance = Vector2.Distance(GetLaunchAnchorPosition(), pointerWorldPosition);
            return Mathf.Clamp01(dragDistance / Mathf.Max(0.01f, maxDragDistance));
        }

        private void SetupDefaultVisuals()
        {
            if (launchPoint != null && launchPointIndicatorPrefab != null)
            {
                launchPointIndicatorInstance = Instantiate(launchPointIndicatorPrefab, launchPoint.position, Quaternion.identity, launchPoint);
                launchPointIndicatorBaseScale = launchPointIndicatorInstance.transform.localScale;
                launchPointIndicatorIsArt = true; // an authored prefab owns its own look
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

                // 병사생성 포털 → 새총. The launch point used to be an abstract glowing portal
                // ring, which never explained WHY dragging backwards fires a soldier forwards.
                // A slingshot states the control's whole grammar in its silhouette: pull the
                // pouch, release, the band throws. Prefers the slingshot art, falls back to
                // the legacy portal frames, then to the procedural ring — an art-less build
                // still gets a readable launch affordance.
                string gateKey = GimmickAnimLibrary.SlingshotAnim;
                var gateFrames = GimmickAnimLibrary.LoadFrames(gateKey);
                if (gateFrames == null || gateFrames.Length < 2)
                {
                    gateKey = GimmickAnimLibrary.LaunchGateAnim;
                    gateFrames = GimmickAnimLibrary.LoadFrames(gateKey);
                }
                if (gateFrames != null && gateFrames.Length >= 2)
                {
                    sr.sprite = gateFrames[0];
                    float native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                    if (native > 0.0001f)
                    {
                        // The slingshot reads as a physical siege machine standing on the
                        // apron, but even at 2.2u it still rivalled a wall course. 1.6u makes
                        // the fortress unambiguously the giant on the board and the slingshot
                        // the tool battering it — the proportion every siege game sells.
                        float target = gateKey == GimmickAnimLibrary.SlingshotAnim ? 1.6f : 2.4f;
                        float s = target / native;
                        go.transform.localScale = new Vector3(s, s, 1f);
                    }
                    // Slingshot stands ON the ground; the portal floated centered on the muzzle.
                    if (gateKey == GimmickAnimLibrary.SlingshotAnim)
                    {
                        // Ground offset tracks the height above: 0.85 was tuned for the 3.1u
                        // frame, so it scales with it or the machine sinks into the apron.
                        go.transform.localPosition = new Vector3(0f, 0.85f * (1.6f / 3.1f), 0f);
                        sr.color = Color.white; // no cyan portal wash over the wood/leather
                    }
                    // TryAttach re-derives scale from frame 0 to preserve the world footprint,
                    // so the authoritative base scale is read back AFTER it runs.
                    GimmickFrameAnimator.TryAttach(go, gateKey, 8f);
                    launchPointIndicatorIsArt = true;
                }
                launchPointIndicatorBaseScale = go.transform.localScale;
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
                launchPointHintLabel.text = "▼ 발사 준비 ▼";
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
                // Authored art is greyscale so the tint below is the only thing colouring it. The
                // procedural circle baked amber into its pixels, which meant the self-hit path
                // multiplied amber by blue and got mud — the one moment the marker matters most.
                var art = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ImpactMarker);
                if (art != null)
                {
                    sr.sprite = art;
                    sr.color = new Color(1f, 0.25f, 0.15f, 0.9f);
                    // 128px art at 0.44 world units matches the procedural 0.22 radius it replaces.
                    float native = Mathf.Max(0.0001f, art.bounds.size.x);
                    go.transform.localScale = Vector3.one * (0.44f / native);
                }
                else
                {
                    sr.sprite = CreateCircleSprite(0.22f, new Color(1f, 0.25f, 0.15f, 0.9f));
                }
                sr.sortingOrder = 12;
                go.SetActive(false);
                impactMarkerInstance = go;
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

            // (The authored-colour capture used to sit here. It ran BEFORE Start() assigned the
            //  trajectory's colours, so it snapshotted the scene's serialized gradient — opaque
            //  white, no fade — and DrawTrajectory's non-self-hit branch then "restored" that over
            //  the authored pair on every draw. While dragging, the Update() hue animation
            //  overwrote it in the same frame and hid the defect; the keyboard aim path does not
            //  animate (it returns early on !isDragging), so arrow-key aiming rendered a flat
            //  opaque arc. The capture now happens after Start() authors the colours.)

            if (launchStatsText == null)
            {
                var root = HudCanvas.Root();
                if (root != null)
                {
                    var go = new GameObject("LaunchStatsText");
                    go.transform.SetParent(root, false);
                    var textComp = go.AddComponent<TextMeshProUGUI>();
                    textComp.fontSize = HudCanvas.PrimaryLabelSize;
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
                var root = HudCanvas.Root();
                if (root != null)
                {
                    var go = new GameObject("ControlGuideText");
                    go.transform.SetParent(root, false);
                    var textComp = go.AddComponent<TextMeshProUGUI>();
                    textComp.fontSize = HudCanvas.SecondaryLabelSize;
                    textComp.color = new Color(0.8f, 0.95f, 1f, 0.95f);
                    textComp.outlineWidth = 0.18f;
                    textComp.outlineColor = new Color(0.02f, 0.015f, 0.01f, 0.95f);
                    textComp.alignment = TextAlignmentOptions.Left;
                    textComp.text = BuildControlGuideText();

                    var rectTransform = go.GetComponent<RectTransform>();
                    rectTransform.anchorMin = new Vector2(0.02f, 0.02f);
                    rectTransform.anchorMax = new Vector2(0.82f, 0.02f);
                    rectTransform.pivot = new Vector2(0f, 0f);
                    // Text starts past the 64px portrait slot so the two never overprint.
                    rectTransform.anchoredPosition = new Vector2(76f, 0f);
                    rectTransform.sizeDelta = new Vector2(-76f, 72f);

                    controlGuideText = textComp;
                }
            }

            // Projectile portrait (playtest feedback: "발사체 이미지로" — show WHAT fires,
            // not just its name). Sits in the slot the guide text's 76px inset reserves.
            if (selectedUnitPortrait == null)
            {
                var root = HudCanvas.Root();
                if (root != null)
                {
                    var go = new GameObject("SelectedUnitPortrait");
                    go.transform.SetParent(root, false);
                    selectedUnitPortrait = go.AddComponent<Image>();
                    selectedUnitPortrait.preserveAspect = true;
                    selectedUnitPortrait.raycastTarget = false;
                    var rt = go.GetComponent<RectTransform>();
                    rt.anchorMin = rt.anchorMax = new Vector2(0.02f, 0.02f);
                    rt.pivot = new Vector2(0f, 0f);
                    rt.anchoredPosition = new Vector2(0f, 4f);
                    rt.sizeDelta = new Vector2(64f, 64f);
                    go.SetActive(false); // RefreshSelectedUnitPortrait shows it when art exists
                }
                RefreshSelectedUnitPortrait();
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
                var root = HudCanvas.Root();
                if (root != null)
                {
                    var go = new GameObject("LaunchAlertText");
                    go.transform.SetParent(root, false);
                    launchAlertText = go.AddComponent<TextMeshProUGUI>();
                    launchAlertText.fontSize = HudCanvas.PrimaryLabelSize;
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

            // No boundary ring: input is drag-from-anywhere, so a circle around the sling
            // would re-teach the removed "press here" rule the moment it appeared.
        }

        private void UpdateLaunchStats(Vector2 velocity)
        {
            if (launchStatsText == null) return;
            bool canLaunch = velocity.magnitude >= EffectiveMinLaunchSpeed;
            var gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                velocity = gameManager.PreviewLastStandLaunchVelocity(gameManager.IsPlayerTurn, velocity);
            }
            float forcePercent = LaunchPowerCurve.DrawForSpeed(velocity.magnitude, maxLaunchVelocity) * 100f;

            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            if (angle < 0) angle += 360f;
            string ready = canLaunch ? "발사!" : "더 당기기";
            launchStatsText.text = $"<b>{ready}</b>  ·  파워 {forcePercent:F0}% · 각도 {angle:F0}°";
            launchStatsText.color = canLaunch ? new Color(0.92f, 0.98f, 1f, 1f) : new Color(1f, 0.72f, 0.24f, 1f);
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
            Vector2 anchor = GetLaunchAnchorPosition();
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

        private void UpdateImpactMarker(bool active, Vector2 position = default, bool ownKeep = false)
        {
            if (impactMarkerInstance == null) return;
            if (active)
            {
                impactMarkerInstance.transform.position = new Vector3(position.x, position.y, 0);
                impactMarkerInstance.SetActive(true);
                // Same amber as the arc, on the marker the player is looking at. A shot that lands
                // on your own keep still gets a marker - hiding it would remove the readout at the
                // exact moment it matters most.
                var markerRenderer = impactMarkerInstance.GetComponent<SpriteRenderer>();
                if (markerRenderer != null)
                {
                    if (!authoredMarkerColorCaptured)
                    {
                        authoredMarkerColor = markerRenderer.color;
                        authoredMarkerColorCaptured = true;
                    }
                    markerRenderer.color = ownKeep ? SelfHitTrajectoryColor : authoredMarkerColor;
                }
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
            if (launchAlertText != null) launchAlertText.text = "";
            if (controlGuideText != null)
            {
                controlGuideText.text = BuildControlGuideText();
                controlGuideText.color = new Color(0.8f, 0.95f, 1f, 0.95f);
            }
        }


        private void OnDestroy()
        {
            if (impactMarkerInstance != null) Destroy(impactMarkerInstance);
            if (launchPointIndicatorInstance != null) Destroy(launchPointIndicatorInstance);
            if (launchStatsText != null && launchStatsText.gameObject.name == "LaunchStatsText") Destroy(launchStatsText.gameObject);
            if (launchAlertText != null && launchAlertText.gameObject.name == "LaunchAlertText") Destroy(launchAlertText.gameObject);
            if (selectedUnitPortrait != null) Destroy(selectedUnitPortrait.gameObject);
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
                // Everything here goes through `sharedMaterial`. Reading `.material` instantiates a
                // per-renderer copy the moment it is READ, not written — in EditMode that logs a
                // material-leak error and fails every test that did not expect it, which is how
                // this was caught. The pre-existing line assigned through `.material`, which was
                // harmless only because it never read it.
                if (trajectoryLine.sharedMaterial == null)
                {
                    trajectoryLine.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
                }
                // A solid line does not read as a predicted path — it reads as a drawn object. The
                // dot texture tiles along the arc, which is the convention every comparable title
                // in the survey uses. Tiling needs the texture to repeat along the line: the
                // importer sets `wrapU: 0` for exactly this. The default the meta shipped with was
                // `wrapU: 1` (Clamp), which smears the last pixel column down the whole tail — and
                // that failure looks like a solid line, i.e. exactly what this replaces.
                var dash = Resources.Load<Sprite>("Effects/trajectory_dash");
                if (dash != null && trajectoryLine.sharedMaterial != null)
                {
                    trajectoryLine.sharedMaterial.mainTexture = dash.texture;
                    trajectoryLine.textureMode = LineTextureMode.Tile;
                    // Measured on the deployed build at textureScale 1: the arc ran 939 contiguous
                    // columns with zero gaps and only 30% brightness modulation — a period of 7.0px
                    // (autocorrelation of the arc's brightness profile), which is below what soft
                    // dot edges survive, so the dots merged into a faintly ribbed line. Stretching
                    // U by this factor targets a ~20px period, where the gaps read as gaps.
                    trajectoryLine.textureScale = new Vector2(0.35f, 1f);
                }
                // Translucency now lives in the TEXTURE (peak alpha 0.55, 44% duty cycle), so these
                // vertex colours only shape the near-to-far falloff on top of it. The tail alpha is
                // deliberately far higher than the 0.25 it replaced: alpha multiplies, and 0.25 over
                // a dotted texture left the far half of the arc at 0.14 — invisible over bright
                // terrain. That was survivable when the texture was a 75%-duty near-solid bar; with
                // real gaps it is not, and a preview that fades out before the impact is the same
                // "arc ends in the sky" defect the resolution comment above describes.
                trajectoryLine.startColor = new Color(1f, 1f, 1f, 0.85f);
                trajectoryLine.endColor = new Color(0.5f, 0.8f, 1f, 0.55f);

                // Capture the pair authored immediately above, so the self-hit tint stays a
                // reversible overlay. This must run AFTER the assignment: it used to live in
                // SetupDefaultVisuals(), which Start() calls first, so it snapshotted the scene's
                // serialized gradient instead — opaque white, no fade — and DrawTrajectory then
                // restored THAT over these colours on every non-self-hit draw.
                authoredTrajectoryStart = trajectoryLine.startColor;
                authoredTrajectoryEnd = trajectoryLine.endColor;
                authoredTrajectoryColorsCaptured = true;
            }
        }

        public void SetSelectedUnit(GameObject unitPrefab, DeployCard? selectedCard = null)
        {
            selectedUnitPrefab = unitPrefab;
            selectedUnitUsesDeployment = selectedCard.HasValue && DeploymentRules.IsDeployOnly(selectedCard.Value);
            selectedUnitTelemetryName = ResolveTelemetryName(unitPrefab, selectedCard);
            selectedUnitName = ResolveDisplayName(unitPrefab, selectedCard);
            selectedLaunchBodyBounds = UnitController.EstimateLaunchedWorldColliderBounds(unitPrefab);
            // A new selection is a new turn for aiming purposes: the keyboard preview is
            // re-armed only by the next arrow press, so the board opens each turn clean.
            keyboardAimTouchedThisTurn = false;
            if (!isDragging)
            {
                if (trajectoryLine != null) trajectoryLine.positionCount = 0;
                HideLaunchStats();
                UpdateImpactMarker(false);
            }
            if (controlGuideText != null) controlGuideText.text = BuildControlGuideText();
            RefreshSelectedUnitPortrait();
            if (launchPointHintLabel != null)
            {
                launchPointHintLabel.text = selectedUnitUsesDeployment
                    ? "▼ 전장에 설치 지점 선택 ▼"
                    : $"▼ {selectedUnitName} 장전 ▼";
            }
        }

        /// <summary>Stable English analytics key (Telemetry aggregates by string equality).</summary>
        private static string ResolveTelemetryName(GameObject unitPrefab, DeployCard? selectedCard)
        {
            if (unitPrefab != null && unitPrefab.TryGetComponent<UnitController>(out var unit))
            {
                return unit.unitType.ToString();
            }
            // The powder-keg projectile prefab carries no UnitController (it gains one at
            // spawn — see SpawnAndLaunchOne); its gimmick identifies it.
            if (unitPrefab != null && unitPrefab.GetComponent<ExplosiveGimmick>() != null)
            {
                return UnitType.Barrel.ToString();
            }
            if (selectedCard.HasValue) return selectedCard.Value.ToString();
            if (unitPrefab != null && !string.IsNullOrWhiteSpace(unitPrefab.name))
            {
                return unitPrefab.name.Replace("(Clone)", string.Empty).Trim();
            }
            return "Unit";
        }

        /// <summary>
        /// Player-facing name, always the DeploymentRules Korean vocabulary. The old path
        /// leaked engine identifiers ("EXPLOSIVEBARREL 준비") whenever the prefab carried no
        /// UnitController — a raw asset name is developer text, not game text.
        /// </summary>
        private static string ResolveDisplayName(GameObject unitPrefab, DeployCard? selectedCard)
        {
            if (selectedCard.HasValue) return DeploymentRules.DisplayName(selectedCard.Value);
            if (unitPrefab != null && unitPrefab.TryGetComponent<UnitController>(out var unit))
            {
                switch (unit.unitType)
                {
                    case UnitType.Knight: return DeploymentRules.DisplayName(DeployCard.Knight);
                    case UnitType.Archer: return DeploymentRules.DisplayName(DeployCard.Archer);
                    case UnitType.Cannon: return DeploymentRules.DisplayName(DeployCard.Cannon);
                    case UnitType.Barrel: return DeploymentRules.DisplayName(DeployCard.Barrel);
                }
            }
            if (unitPrefab != null && unitPrefab.GetComponent<ExplosiveGimmick>() != null)
            {
                return DeploymentRules.DisplayName(DeployCard.Barrel);
            }
            return "부대";
        }

        /// <summary>
        /// The projectile as an image beside the guide line (playtest feedback: a portrait
        /// reads instantly where a name must be parsed). Higgsfield art keyed by the same
        /// English identifier telemetry uses; no art → the icon simply stays hidden.
        /// </summary>
        private void RefreshSelectedUnitPortrait()
        {
            if (selectedUnitPortrait == null) return;
            var sprite = HiggsfieldSpriteLibrary.LoadUi(selectedUnitTelemetryName);
            selectedUnitPortrait.sprite = sprite;
            selectedUnitPortrait.gameObject.SetActive(sprite != null);
        }

        /// <summary>
        /// <c>GameManager.WaitAndEndTurn</c> disables this component while a volley resolves, and
        /// a disabled Update cannot retract what it drew. Without this the aim guidance freezes
        /// on screen for the whole resolution — the same false-instruction defect as UX-003b,
        /// just arriving through the component lifecycle instead of the turn state.
        /// </summary>
        private void OnDisable()
        {
            if (controlGuideText != null && controlGuideText.gameObject.activeSelf)
            {
                controlGuideText.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            // Aim and deploy are mutually exclusive verbs: while placement is armed the same
            // click must not also start drawing the sling (design/deployment-economy.md §2).
            bool deployArmed = DeploymentController.Instance != null && DeploymentController.Instance.DeployModeArmed;
            if (deployArmed && isDragging) CancelAim();
            var gameManager = GameManager.Instance;
            bool canAim = gameManager != null
                && gameManager.currentState == GameState.PlayerTurn
                && gameManager.IsPlayerTurn;
            if (canAim && selectedUnitPrefab != null && !deployArmed) HandleInput();

            // The hue drift makes the arc feel live. Its ALPHAS must stay equal to the authored
            // pair set in Start(): this runs every frame of the draw and overwrites them, so any
            // disagreement here silently wins and the dotted line snaps back to near-opaque the
            // instant the player pulls — which is precisely the appearance this replaced.
            if (isDragging && trajectoryLine != null && trajectoryLine.positionCount > 0)
            {
                float offset = Time.time * 2.5f;
                Color startCol = Color.Lerp(new Color(1f, 1f, 1f, 0.85f), new Color(0.35f, 0.85f, 1f, 0.85f), Mathf.Sin(offset) * 0.5f + 0.5f);
                Color endCol = new Color(startCol.r, startCol.g, startCol.b, 0.55f);
                trajectoryLine.startColor = startCol;
                trajectoryLine.endColor = endCol;
            }

            bool isPlayerTurn = canAim;

            if (launchPointIndicatorInstance != null)
            {
                // Motion, dim, and fire kick now belong to LauncherView, which owns both sides.
                // The old code hid this launcher for the whole enemy turn — and since the enemy
                // apron has no visual of its own, that left BOTH muzzles empty for the 0.9s in
                // which the enemy actually shoots. Dimming the waiting side instead keeps the
                // board populated and is the sample's most common way to say who is acting
                // (8/12), at no screen-element cost. `.survey/siege-impact-vfx-and-attack-motion/`
                if (playerLauncherView == null)
                {
                    playerLauncherView = launchPointIndicatorInstance.GetComponent<LauncherView>()
                        ?? launchPointIndicatorInstance.AddComponent<LauncherView>();
                    playerLauncherView.isPlayerSide = true;
                    playerLauncherView.CaptureRestPose();
                }

                // Deploy mode still hides it: while placement is armed the launcher is not the
                // verb in play, and leaving it lit would advertise a gesture the click is not
                // going to perform (the same false-affordance class as UX-003).
                bool visible = !deployArmed;
                if (launchPointIndicatorInstance.activeSelf != visible)
                {
                    launchPointIndicatorInstance.SetActive(visible);
                }

                if (!launchPointIndicatorIsArt)
                {
                    // Procedural ring fallback keeps its cyan identity; LauncherView drives the
                    // alpha, so only the hue is set here.
                    var sr = launchPointIndicatorInstance.GetComponent<SpriteRenderer>();
                    if (sr != null) sr.color = new Color(0.35f, 0.9f, 1f, sr.color.a);
                }
            }

            if (launchPointHintLabel != null)
            {
                launchPointHintLabel.gameObject.SetActive(isPlayerTurn && !isDragging && !deployArmed);
                launchPointHintLabel.transform.localPosition = new Vector3(0f, 1.45f + Mathf.Sin(Time.time * 7f) * 0.16f, 0f);
            }

            // UX-003b: this line reads "아무 곳이나 당겨 발사", and it used to stay on screen
            // through the enemy turn while three separate paths refused the drag. An instruction
            // the game will not honour is not guidance, it is a trap — and the survey of twelve
            // comparable titles found none that ships one (`design/visibility-spec-v2.md` §5-A).
            // Gated on the same `canAim` the input path uses, so the label and the rule cannot
            // disagree. Deploy mode keeps it: there the line describes placement, which IS live.
            if (controlGuideText != null)
            {
                bool guidanceIsTrue = canAim || deployArmed;
                if (controlGuideText.gameObject.activeSelf != guidanceIsTrue)
                {
                    controlGuideText.gameObject.SetActive(guidanceIsTrue);
                }
            }

            if (weakPullFlashTimer > 0f)
            {
                weakPullFlashTimer -= Time.deltaTime;
                if (weakPullFlashTimer <= 0f && controlGuideText != null)
                {
                    controlGuideText.text = BuildControlGuideText();
                    controlGuideText.color = new Color(0.8f, 0.95f, 1f, 0.95f);
                    if (launchAlertText != null) launchAlertText.text = "";
                }
            }
        }

        /// <summary>
        /// Drops any in-progress draw and clears its visuals without firing. Called when the
        /// player arms deploy mode mid-aim so the drawn shot is abandoned, never spent.
        /// </summary>
        public void CancelAim()
        {
            if (!isDragging) return;
            isDragging = false;
            launchVelocity = Vector2.zero;
            if (trajectoryLine != null) trajectoryLine.positionCount = 0;
            CleanUpVisuals();
        }

        private void HandleInput()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            // Test fixtures drive the same drag path through the simulated pointer seam so
            // what they exercise is exactly what the player performs.
            if (useSimulatedPointer)
            {
                HandleDragAimInput();
                return;
            }
#endif
            HandleKeyboardFineTune();
            HandleDragAimInput();
        }

        /// <summary>
        /// Keyboard fallback: arrows nudge angle/power, Space commits the tuned shot.
        /// A pointer click deliberately does NOT fire — committing a launch requires
        /// either the full pull gesture or an explicit Space press, never a bare tap.
        /// The preview arc + power/angle readout are OPT-IN per turn: at idle they used to
        /// render permanently at the bottom center ("발사! 파워 60% · 각도 45°" over a dotted
        /// arc), which playtest feedback read as meaningless chrome. The first arrow press
        /// summons them; a clean turn opens with a clean board.
        /// </summary>
        private void HandleKeyboardFineTune()
        {
            bool arrowTouched = false;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) { AdjustAimAngle(-angleStepDegrees); arrowTouched = true; }
            if (Input.GetKeyDown(KeyCode.RightArrow)) { AdjustAimAngle(angleStepDegrees); arrowTouched = true; }
            if (Input.GetKeyDown(KeyCode.DownArrow)) { AdjustAimPower(-powerStep); arrowTouched = true; }
            if (Input.GetKeyDown(KeyCode.UpArrow)) { AdjustAimPower(powerStep); arrowTouched = true; }
            if (arrowTouched) keyboardAimTouchedThisTurn = true;

            if (isDragging) return; // an active pull owns the trajectory preview

            // Space must stay armed with the tuned default even when the preview is hidden:
            // the commit consumes the aim state, not the preview.
            launchVelocity = GetSeparatedAimVelocity();
            if (keyboardAimTouchedThisTurn)
            {
                DrawTrajectory(launchVelocity);
                UpdateLaunchStats(launchVelocity);
            }

            if (Input.GetKeyDown(KeyCode.Space)) LaunchUnit();
        }

        private void HandleDragAimInput()
        {
            if (!TryReadPointer(out var pointerWorldPos, out var pressed, out var held, out var released)) return;

            if (pressed)
            {
                if (UnityEngine.EventSystems.EventSystem.current != null
                    && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                {
                    return; // UI owns this press (cards, buttons, skip)
                }

                // Drag-from-anywhere (first-contact playtest, 2026-08-12): requiring the
                // press to start inside the 3.5u ring made the very first gesture a
                // precision test — most new players pressed the castle they wanted to hit
                // and got scolded. Any off-UI press now starts the draw; the pull is
                // measured FROM THE PRESS POINT, so a press is always a zero-power start
                // and the gesture (not the cursor's absolute position) is what aims.
                isDragging = true;
                dragStartPos = pointerWorldPos;
                launchVelocity = Vector2.zero;
                if (trajectoryLine != null) trajectoryLine.positionCount = trajectoryResolution;
                if (launchAlertText != null) launchAlertText.text = "DRAW THE SIEGE LINE";
                if (controlGuideText != null)
                {
                    controlGuideText.text = $"{selectedUnitName} 조준 중 — 궤적과 바람을 보고 발사";
                    controlGuideText.color = new Color(0.94f, 0.98f, 1f, 0.95f);
                }
            }

            if (isDragging && held)
            {
                Vector2 anchorSpacePointer = ToAnchorSpace(pointerWorldPos);
                launchVelocity = CalculateLaunchVelocity(anchorSpacePointer);
                DrawTrajectory(launchVelocity);
                UpdateRubberBand(anchorSpacePointer);
                UpdateLaunchStats(launchVelocity);
            }

            if (isDragging && released)
            {
                isDragging = false;
                if (trajectoryLine != null) trajectoryLine.positionCount = 0;
                CleanUpVisuals();
                if (launchVelocity.magnitude >= EffectiveMinLaunchSpeed)
                {
                    LaunchUnit();
                }
                else
                {
                    TriggerWeakLaunchFeedback();
                }
            }
        }

        /// <summary>
        /// Maps a drag gesture that started at <see cref="dragStartPos"/> onto the sling
        /// anchor: the pull the player performs anywhere on screen is replayed as if it
        /// had started on the pouch. Starting AT the anchor degenerates to identity, which
        /// is why every anchor-press test and the keyboard path are unaffected.
        /// </summary>
        private Vector2 ToAnchorSpace(Vector2 pointerWorldPosition)
        {
            return GetLaunchAnchorPosition() + (pointerWorldPosition - dragStartPos);
        }

        private void TriggerWeakLaunchFeedback()
        {
            weakPullFlashTimer = 0.45f;
            if (launchAlertText != null) launchAlertText.text = "더 깊게 당긴 뒤 발사";
            if (controlGuideText != null)
            {
                controlGuideText.text = BuildControlGuideText();
                controlGuideText.color = new Color(1f, 0.78f, 0.25f, 0.95f);
            }
        }

        /// <summary>
        /// Slingshot pull: the shot flies OPPOSITE the drag. Pulling the pouch down-left
        /// throws up-right, exactly like the band the affordance art depicts. Draw depth
        /// (clamped to maxDragDistance) sets power; the pull direction sets the angle.
        /// </summary>
        public Vector2 CalculateLaunchVelocity(Vector2 pointerWorldPosition)
        {
            Vector2 pullVector = GetLaunchAnchorPosition() - pointerWorldPosition;
            Vector2 clampedPull = Vector2.ClampMagnitude(pullVector, maxDragDistance);
            if (clampedPull.sqrMagnitude <= 0.0001f) return Vector2.zero;

            // Draw depth as a fraction of the full pull, then through the power curve. This used
            // to be `clampedPull * launchForceMultiplier` capped at maxLaunchVelocity — speed
            // linear in draw, which made DISTANCE quadratic in draw because range goes as v².
            // Measured consequence at 45°: impact jumped from x=0.23 at 60% draw to x=18.25 at
            // 80%, across a keep 5.5u wide. See LaunchPowerCurve for the arithmetic and the
            // before/after windows.
            float normalizedDraw = clampedPull.magnitude / Mathf.Max(0.0001f, maxDragDistance);
            float speed = LaunchPowerCurve.SpeedForDraw(normalizedDraw, maxLaunchVelocity);
            return clampedPull.normalized * speed;
        }

        public Vector2 GetLaunchAnchorPosition()
        {
            return launchPoint != null ? (Vector2)launchPoint.position : (Vector2)transform.position;
        }

        public Vector2 GetLaunchPosition()
        {
            Vector2 anchor = GetLaunchAnchorPosition();
            float colliderBottomFromRoot = selectedLaunchBodyBounds.center.y - selectedLaunchBodyBounds.extents.y;
            float clearedRootY = anchor.y - colliderBottomFromRoot;
            float spawnY = Mathf.Max(anchor.y + UnitController.DefaultLaunchSpawnHeight, clearedRootY);
            return new Vector2(anchor.x, spawnY);
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
            var gameManager = GameManager.Instance;
            float windForce = gameManager != null ? gameManager.currentWindForce : 0f;
            if (gameManager != null)
            {
                velocity = gameManager.PreviewLastStandLaunchVelocity(gameManager.IsPlayerTurn, velocity);
            }
            float windRadius = gameManager != null ? gameManager.windEffectRadius : 0f;
            Vector2 windOrigin = startPos;
            float integrationStep = Mathf.Max(0.001f, timeStep);
            UnitController prefabController = selectedUnitPrefab != null
                ? selectedUnitPrefab.GetComponent<UnitController>()
                : null;
            bool addsControllerAtRuntime = prefabController == null
                && selectedUnitPrefab != null
                && selectedUnitPrefab.GetComponent<ExplosiveGimmick>() != null;
            float hardCeilingY = prefabController != null
                ? prefabController.hardCeilingY
                : addsControllerAtRuntime
                    ? UnitController.DefaultHardCeilingY
                    : float.PositiveInfinity;

            float mass = 1f;
            float linearDrag = 0f;
            if (selectedUnitPrefab != null && selectedUnitPrefab.TryGetComponent<Rigidbody2D>(out var prefabRb))
            {
                // Match the runtime mass reduction and linear damping applied to the launched
                // Rigidbody2D so the preview uses the same fixed-step flight model.
                mass = Mathf.Max(UnitController.MinRuntimeMass, prefabRb.mass * UnitController.RuntimeMassScale);
                linearDrag = Mathf.Max(0f, prefabRb.linearDamping);
            }

            Vector2 castSize = new Vector2(selectedLaunchBodyBounds.size.x, selectedLaunchBodyBounds.size.y);
            Vector2 colliderCenterOffset = selectedLaunchBodyBounds.center;
            Vector2 prevPoint = startPos;
            bool hitDetected = false;
            // Set when the arc terminates on the shooter's OWN keep. Carried out of the loop so the
            // line and the marker can say it, rather than adding a new HUD element - this repo's own
            // visibility survey found that adding an icon per missed signal is a documented failure
            // path (one team spent eighteen months building what they called "an icon mess").
            bool hitOwnKeep = false;
            Vector2 hitPoint = Vector2.zero;
            Vector2 currentVelocity = velocity;

            previewCrossedGateIds.Clear();
            trajectoryPoints.Clear();
            trajectoryPoints.Add(new Vector3(startPos.x, startPos.y, 0f));

            if (trajectoryCollisionMask == 0)
            {
                // Project gameplay colliders currently share Default; layer-name masks cannot
                // distinguish bodies. Use Unity's normal raycast mask and mirror the runtime's
                // explicit same-team UnitController collision ignores below.
                trajectoryCollisionMask = Physics2D.DefaultRaycastLayers;
            }

            for (int i = 0; i < trajectoryResolution; i++)
            {
                // UnitController enforces its ceiling at the start of FixedUpdate, before
                // gravity/wind integration. Clamp against the previous step position so a
                // crossing step completes and upward motion is stopped on the following step.
                if (prevPoint.y > hardCeilingY && currentVelocity.y > 0f)
                {
                    currentVelocity = new Vector2(currentVelocity.x, 0f);
                }

                Vector2 acceleration = gravity + UnitController.CalculateWindAcceleration(
                    prevPoint,
                    mass,
                    windForce,
                    windOrigin,
                    windRadius);

                // Mirror Rigidbody2D's fixed-step semi-implicit integration: force changes
                // velocity first, then the updated velocity advances the body.
                currentVelocity += acceleration * integrationStep;
                currentVelocity /= 1f + linearDrag * integrationStep;
                Vector2 nextPoint = prevPoint + currentVelocity * integrationStep;

                if (!hitDetected)
                {
                    Vector2 segment = nextPoint - prevPoint;
                    float segmentDistance = segment.magnitude;
                    int hitCount = segmentDistance > 0.0001f
                        ? Physics2D.BoxCastNonAlloc(
                            prevPoint + colliderCenterOffset,
                            castSize,
                            0f,
                            segment / segmentDistance,
                            trajectoryHits,
                            segmentDistance,
                            trajectoryCollisionMask)
                        : 0;
                    bool previewIsPlayer = gameManager == null || gameManager.IsPlayerTurn;
                    RaycastHit2D nearestHit = default;
                    float nearestDistance = float.PositiveInfinity;
                    for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                    {
                        RaycastHit2D candidate = trajectoryHits[hitIndex];
                        if (candidate.collider == null || candidate.collider.isTrigger) continue;
                        UnitController hitUnit = candidate.collider.GetComponentInParent<UnitController>();
                        if (hitUnit != null && hitUnit.isPlayerUnit == previewIsPlayer) continue;
                        if (candidate.distance >= nearestDistance) continue;

                        nearestHit = candidate;
                        nearestDistance = candidate.distance;
                    }

                    // Triggers are passages, not impacts. Event gates are the one exception we
                    // inspect: crossing one changes velocity for the next fixed-step sample,
                    // exactly once per gate during this prediction, without mutating runtime state.
                    for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                    {
                        RaycastHit2D candidate = trajectoryHits[hitIndex];
                        if (candidate.collider == null || !candidate.collider.isTrigger) continue;
                        if (candidate.distance > nearestDistance) continue;

                        var gate = candidate.collider.GetComponentInParent<EventGateGimmick>();
                        if (gate == null || !previewCrossedGateIds.Add(gate.GetEntityId())) continue;
                        currentVelocity *= gate.PreviewVelocityMultiplier;
                    }

                    if (nearestHit.collider != null)
                    {
                        hitDetected = true;
                        hitPoint = nearestHit.point;
                        // WHOSE keep, not just where. The preview already stops on the player's own
                        // wall and that is correct - the real shot does too, on 41.1% of the aim
                        // space (angle 10-80 x draw 10-100%, measured offline against this same
                        // integrator). What it never said is that the wall is YOURS, so the arc read
                        // as a rendering fault rather than as a shot about to hit your own keep.
                        //
                        // Read from the collider that stopped it rather than from geometry: a keep
                        // moves with its stage layout, and comparing x against a hardcoded apron
                        // would silently invert on a stage whose sides differ.
                        var hitCastle = nearestHit.collider.GetComponentInParent<CastleController>();
                        hitOwnKeep = hitCastle != null && hitCastle.isPlayerCastle == previewIsPlayer;
                        Vector2 rootAtImpact = nearestHit.centroid - colliderCenterOffset;
                        trajectoryPoints.Add(new Vector3(rootAtImpact.x, rootAtImpact.y, 0f));
                        break;
                    }
                }

                trajectoryPoints.Add(new Vector3(nextPoint.x, nextPoint.y, 0));
                prevPoint = nextPoint;
            }

            trajectoryLine.positionCount = trajectoryPoints.Count;
            for (int i = 0; i < trajectoryPoints.Count; i++)
            {
                trajectoryLine.SetPosition(i, trajectoryPoints[i]);
            }

            // The arc says where the shot goes; its colour now says whose wall stops it. Amber for a
            // shot that will hit your own keep, the authored colour otherwise. No new element, and
            // the signal rides the line the player is already reading while they aim.
            if (hitOwnKeep)
            {
                trajectoryLine.startColor = SelfHitTrajectoryColor;
                trajectoryLine.endColor = new Color(
                    SelfHitTrajectoryColor.r, SelfHitTrajectoryColor.g, SelfHitTrajectoryColor.b, 0.55f);
            }
            else if (authoredTrajectoryColorsCaptured)
            {
                trajectoryLine.startColor = authoredTrajectoryStart;
                trajectoryLine.endColor = authoredTrajectoryEnd;
            }

            UpdateImpactMarker(hitDetected, hitPoint, hitOwnKeep);
        }

        private void LaunchUnit()
        {
            if (selectedUnitPrefab == null) return;

            var gameManager = GameManager.Instance;
            if (gameManager != null && !gameManager.TryCommitTurnShot()) return;

            Vector2 reportedVelocity = launchVelocity;
            if (gameManager != null)
            {
                reportedVelocity = gameManager.PreviewLastStandLaunchVelocity(gameManager.IsPlayerTurn, reportedVelocity);
            }

            var firstUnit = SpawnAndLaunchOne(launchVelocity);

            // Set wind effect origin and radius for this launch
            if (GameManager.Instance != null)
            {
                GameManager.Instance.windEffectOrigin = GetLaunchPosition();
                GameManager.Instance.windEffectRadius = 10f;  // Radius of wind effect (adjust as needed)
            }


            // Report the DRAW the player made, not the speed ratio. Under the √ curve a half pull
            // produces 70.7% of max speed, so a speed-based readout would tell the player they
            // pulled harder than they did — and the number they are learning to repeat is the pull.
            float powerPercent = LaunchPowerCurve.DrawForSpeed(reportedVelocity.magnitude, maxLaunchVelocity) * 100f;
            float angle = Mathf.Atan2(reportedVelocity.y, reportedVelocity.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            GameplayUxDirector.NotifyLaunch(selectedUnitName, powerPercent, angle);
            // The launcher itself reacts. Until now the slingshot looped at a fixed 8fps forever,
            // so the frame the shot left was indistinguishable from the frame before it.
            playerLauncherView?.NotifyFired(reportedVelocity);
            TelemetrySink.Volley(
                selectedUnitTelemetryName, // analytics key stays English across UI renames
                powerPercent,
                angle,
                GameManager.Instance != null ? GameManager.Instance.currentWindForce : 0f);
            GameFeelVfx.SpawnShockwaveRing(GetLaunchPosition(), new Color(0.55f, 0.9f, 1f, 0.45f), 1.25f, 0.3f);
            GameFeelVfx.SpawnFeedbackLabel(GetLaunchPosition() + Vector2.up * 0.45f, "LAUNCH!", new Color(0.7f, 0.95f, 1f, 1f), 1.7f, 0.45f);
            if (GameManager.Instance != null) GameManager.Instance.OnUnitLaunched(firstUnit);
        }


        private UnitController SpawnAndLaunchOne(Vector2 velocity)
        {
            Vector2 spawnPosition = GetLaunchPosition();
            var unitGo = Instantiate(selectedUnitPrefab, spawnPosition, Quaternion.identity);
            var unit = unitGo.GetComponent<UnitController>();
            if (unit == null && unitGo.GetComponent<ExplosiveGimmick>() != null)
            {
                unit = unitGo.AddComponent<UnitController>();
                unit.unitType = UnitType.Barrel;
                unit.maxHP = 20f;
                unit.currentHP = 20f;
            }
            // GetLaunchPosition resolves the selected body's predicted Awake collider bounds
            // against the launch anchor, so runtime and preview share this exact root position.
            // No post-Instantiate snap is allowed here: that would move runtime away from the arc.
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
