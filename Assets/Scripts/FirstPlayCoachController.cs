using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// Runtime surface of the first-play guided objectives: a single-line objective banner
    /// (step counter + instruction + skip), a world-space attention arrow over whatever the
    /// current step is about, and a turn-clock hold so the very first turn is never forfeited
    /// while the player is still being told what a turn is.
    ///
    /// Built entirely at runtime like every other overlay in this project (no scene edits).
    /// Spawned once per profile by <see cref="EnsureForFirstPlay"/> from GameManager.StartGame:
    /// PlayerPrefs remembers completion, so returning players and rematches never see it.
    /// The step logic itself is pure and lives in <see cref="FirstPlayGuide"/>.
    /// </summary>
    public sealed class FirstPlayCoachController : MonoBehaviour
    {
        /// <summary>Versioned like LeaderboardStore.PrefsKey; bump to re-show after redesigns.</summary>
        public const string PrefsKey = "CastleBusters.FirstPlayCoach.v1";

        /// <summary>
        /// Automation seam: PlayMode tests drive matches through the same StartGame entry a
        /// player uses, and the editor's PlayerPrefs would make the coach appear on exactly
        /// one test per fresh profile — a moving target. Session reset sets this instead of
        /// deleting the player's real pref.
        /// </summary>
        public static bool SuppressForSession;

        private static FirstPlayCoachController active;

        private readonly FirstPlayGuide guide = new FirstPlayGuide();
        private Canvas canvas;
        private TextMeshProUGUI stepText;
        private TextMeshProUGUI instructionText;
        private RectTransform bannerRect;
        private CanvasGroup bannerGroup;
        private Image gestureIcon;
        private TextMeshPro worldArrow;
        private float stepEnteredAt;
        private float timerHeldSeconds;
        private LaunchManager launchManager;

        public static bool HasSeenCoach => PlayerPrefs.GetInt(PrefsKey, 0) == 1;

        /// <summary>Spawns the coach exactly once per profile, on a real interactive match.</summary>
        public static void EnsureForFirstPlay()
        {
            if (!Application.isPlaying || Application.isBatchMode) return;
            if (SuppressForSession || active != null || HasSeenCoach) return;
            var gm = GameManager.Instance;
            // The instructions teach the one-shot loop; a roster-mode match (tests, tuning
            // scenes) would be narrated wrong, so it simply gets no coach.
            if (gm == null || !gm.EnforcesOneShotTurns) return;

            var go = new GameObject("FirstPlayCoach");
            active = go.AddComponent<FirstPlayCoachController>();
        }

        private void Awake()
        {
            KoreanFontSupport.EnsureFallback();
            BuildBanner();
            launchManager = FindObjectOfType<LaunchManager>();
            stepEnteredAt = Time.unscaledTime;
            ApplyStep();
        }

        private void OnDestroy()
        {
            if (active == this) active = null;
            if (worldArrow != null) Destroy(worldArrow.gameObject);
            if (canvas != null) Destroy(canvas.gameObject);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Destroy(gameObject);
                return;
            }

            // Only a real screen owner (intro card, narration cutscene) hides coaching and
            // pauses observation. NOT a bare timeScale check: hit-stops zero the timescale
            // for a tenth of a second on every good hit, and pausing observation there is
            // exactly how the flag-sampled guide missed a whole resolve window in live QA.
            bool boardLive = gm.currentState != GameState.Intro && StageInterludeController.Active == null;
            if (bannerGroup != null && !boardLive) bannerGroup.alpha = 0f;
            if (!boardLive)
            {
                stepEnteredAt = Time.unscaledTime;
                return;
            }

            var obs = new FirstPlayGuide.Observation(
                acknowledged: ReadAcknowledge(),
                isPlayerTurn: gm.IsPlayerTurn,
                isAiming: launchManager != null && launchManager.IsAiming,
                isResolvingTurn: gm.IsResolvingTurn,
                isGameOver: gm.currentState == GameState.GameOver,
                turnCount: gm.TurnCount);

            if (guide.Advance(obs))
            {
                stepEnteredAt = Time.unscaledTime;
                if (guide.IsFinished)
                {
                    Complete();
                    return;
                }
                ApplyStep();
            }

            // The first turn must survive being read about — but only up to the cap, so an
            // abandoned tab still forfeits and the match still ends on its own.
            if (guide.HoldsTurnClock && timerHeldSeconds < FirstPlayGuide.MaxTimerHoldSeconds)
            {
                timerHeldSeconds += Time.deltaTime;
                gm.HoldTurnTimerForCoaching(8f);
            }

            AnimateBanner();
            AnimateArrow();
        }

        /// <summary>
        /// Acknowledge = any press, or the step's own dwell elapsing. Only the two reading
        /// steps (Goal, FreePlay) consume it, and both auto-advance so a player who never
        /// clicks is not held hostage by a text card.
        /// </summary>
        private bool ReadAcknowledge()
        {
            float dwell = Time.unscaledTime - stepEnteredAt;
            switch (guide.Current)
            {
                case FirstPlayGuide.Step.Goal:
                    return dwell >= FirstPlayGuide.GoalAutoAdvanceSeconds || AnyPress();
                case FirstPlayGuide.Step.FreePlay:
                    return dwell >= FirstPlayGuide.FreePlayAutoAdvanceSeconds || AnyPress();
                default:
                    return false;
            }
        }

        private static bool AnyPress()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) return true;
            return Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began;
        }

        private void Complete()
        {
            PlayerPrefs.SetInt(PrefsKey, 1);
            PlayerPrefs.Save();
            Destroy(gameObject);
        }

        /// <summary>Skip button: same persistence as finishing — a skipped guide is a seen guide.</summary>
        private void Skip() => Complete();

        // ---- presentation ----

        private void ApplyStep()
        {
            if (stepText != null) stepText.text = $"첫 출정 안내  {FirstPlayGuide.StepLabel(guide.Current)}";
            if (instructionText != null) instructionText.text = FirstPlayGuide.Instruction(guide.Current);
            if (bannerGroup != null) bannerGroup.alpha = 0f; // fade back in per step
            if (gestureIcon != null)
            {
                // The drag steps carry the wordless gesture pictogram: a hand pulling
                // back is readable by a player who cannot (or will not) read the line.
                bool showGesture = gestureIcon.sprite != null
                    && (guide.Current == FirstPlayGuide.Step.Draw
                        || guide.Current == FirstPlayGuide.Step.Release);
                gestureIcon.gameObject.SetActive(showGesture);
            }
            PositionArrowForStep();
        }

        private void AnimateBanner()
        {
            if (bannerGroup == null) return;
            bannerGroup.alpha = Mathf.MoveTowards(bannerGroup.alpha, 1f, Time.unscaledDeltaTime * 3.5f);
            if (bannerRect != null)
            {
                float settle = Mathf.Clamp01((Time.unscaledTime - stepEnteredAt) / 0.25f);
                bannerRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(14f, 0f, 1f - Mathf.Pow(1f - settle, 2f)));
            }
        }

        private void AnimateArrow()
        {
            if (worldArrow == null || !worldArrow.gameObject.activeSelf) return;
            var pos = worldArrow.transform.position;
            worldArrow.transform.position = new Vector3(pos.x, ArrowBaseY() + Mathf.Sin(Time.unscaledTime * 6f) * 0.22f, 0f);
        }

        private float arrowAnchorY;

        private float ArrowBaseY() => arrowAnchorY;

        /// <summary>
        /// The arrow marks the OBJECT of the current instruction. Draw/Release steps get no
        /// arrow: the slingshot already carries its own bobbing "장전" hint, and a second
        /// marker on the same spot would just be noise.
        /// </summary>
        private void PositionArrowForStep()
        {
            bool wantsArrow;
            Vector2 target = default;
            switch (guide.Current)
            {
                case FirstPlayGuide.Step.Goal:
                case FirstPlayGuide.Step.FreePlay:
                    wantsArrow = TryGetCorePosition(playerCore: false, out target);
                    break;
                case FirstPlayGuide.Step.EnemyReply:
                    wantsArrow = TryGetCorePosition(playerCore: true, out target);
                    break;
                default:
                    wantsArrow = false;
                    break;
            }

            if (!wantsArrow)
            {
                if (worldArrow != null) worldArrow.gameObject.SetActive(false);
                return;
            }

            if (worldArrow == null)
            {
                var go = new GameObject("FirstPlayCoachArrow");
                worldArrow = go.AddComponent<TextMeshPro>();
                worldArrow.alignment = TextAlignmentOptions.Center;
                worldArrow.fontSize = 5f;
                worldArrow.sortingOrder = 46; // above feedback labels (45)
                worldArrow.text = "▼";
            }

            // Enemy core objective is the win condition (amber, like the HUD's objective
            // diamond); the player's own core warning reads as danger.
            worldArrow.color = guide.Current == FirstPlayGuide.Step.EnemyReply
                ? new Color(1f, 0.45f, 0.28f, 0.95f)
                : new Color(1f, 0.82f, 0.22f, 0.95f);
            arrowAnchorY = target.y + 2.6f;
            worldArrow.transform.position = new Vector3(target.x, arrowAnchorY, 0f);
            worldArrow.gameObject.SetActive(true);
        }

        private static bool TryGetCorePosition(bool playerCore, out Vector2 position)
        {
            for (int i = 0; i < DestructibleBlock.Active.Count; i++)
            {
                if (DestructibleBlock.Active[i] is CastleCoreGimmick core && core.isPlayerCore == playerCore)
                {
                    position = core.transform.position;
                    return true;
                }
            }
            position = default;
            return false;
        }

        private void BuildBanner()
        {
            var canvasGo = new GameObject("FirstPlayCoachCanvas");
            canvasGo.transform.SetParent(transform, false);
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above the gameplay HUD (default order) and the intro (500), below the results
            // card (600) and cutscenes (900): those own the screen when they appear.
            canvas.sortingOrder = 550;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            MobileSafeArea.ConfigureCanvas(canvas);

            var root = MobileSafeArea.GetContentRoot(canvas);

            // Banner sits in the quiet band between the toast lane (~0.78) and the combo
            // lane (~0.60) so coaching, turn toasts, and hit feedback never overprint.
            var banner = new GameObject("CoachBanner");
            banner.transform.SetParent(root, false);
            bannerRect = banner.AddComponent<RectTransform>();
            bannerRect.anchorMin = bannerRect.anchorMax = new Vector2(0.5f, 0.685f);
            bannerRect.pivot = new Vector2(0.5f, 0.5f);
            bannerRect.sizeDelta = new Vector2(980f, 92f);
            bannerGroup = banner.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            bannerGroup.blocksRaycasts = true;

            var plate = banner.AddComponent<Image>();
            plate.color = new Color(0.02f, 0.05f, 0.09f, 0.72f);
            plate.raycastTarget = false;

            var accent = new GameObject("Accent");
            accent.transform.SetParent(banner.transform, false);
            var accentImg = accent.AddComponent<Image>();
            accentImg.color = new Color(1f, 0.82f, 0.22f, 0.95f);
            accentImg.raycastTarget = false;
            var accentRt = accent.GetComponent<RectTransform>();
            accentRt.anchorMin = new Vector2(0f, 0f);
            accentRt.anchorMax = new Vector2(0f, 1f);
            accentRt.pivot = new Vector2(0f, 0.5f);
            accentRt.sizeDelta = new Vector2(6f, 0f);
            accentRt.anchoredPosition = Vector2.zero;

            stepText = CreateLabel(banner.transform, "StepLabel", 20f, new Color(1f, 0.82f, 0.22f, 0.95f));
            stepText.rectTransform.anchorMin = new Vector2(0f, 1f);
            stepText.rectTransform.anchorMax = new Vector2(1f, 1f);
            stepText.rectTransform.pivot = new Vector2(0.5f, 1f);
            stepText.rectTransform.offsetMin = new Vector2(26f, -34f);
            stepText.rectTransform.offsetMax = new Vector2(-26f, -6f);
            stepText.alignment = TextAlignmentOptions.Left;

            instructionText = CreateLabel(banner.transform, "Instruction", 30f, new Color(0.95f, 0.97f, 1f, 1f));
            instructionText.rectTransform.anchorMin = new Vector2(0f, 0f);
            instructionText.rectTransform.anchorMax = new Vector2(1f, 1f);
            instructionText.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            instructionText.rectTransform.offsetMin = new Vector2(26f, 8f);
            instructionText.rectTransform.offsetMax = new Vector2(-26f, -30f);
            instructionText.alignment = TextAlignmentOptions.Left;
            instructionText.fontStyle = FontStyles.Bold;

            // Wordless gesture pictogram (Higgsfield flux_2, ui_drag_gesture): sits to the
            // right of the instruction on the drag steps. Sprite-less builds simply never
            // activate it — the coach stays fully functional as text.
            var gestureGo = new GameObject("GestureIcon");
            gestureGo.transform.SetParent(banner.transform, false);
            gestureIcon = gestureGo.AddComponent<Image>();
            gestureIcon.sprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.DragGesture);
            gestureIcon.preserveAspect = true;
            gestureIcon.raycastTarget = false;
            var gestureRt = gestureGo.GetComponent<RectTransform>();
            gestureRt.anchorMin = gestureRt.anchorMax = new Vector2(1f, 0.5f);
            gestureRt.pivot = new Vector2(1f, 0.5f);
            gestureRt.sizeDelta = new Vector2(72f, 72f);
            gestureRt.anchoredPosition = new Vector2(-140f, 0f);
            gestureGo.SetActive(false);

            // Skip: small, explicit, top-right of the banner. The only raycast target the
            // coach owns — the banner itself must never eat a board press.
            var skipGo = new GameObject("SkipButton");
            skipGo.transform.SetParent(banner.transform, false);
            var skipImg = skipGo.AddComponent<Image>();
            skipImg.color = new Color(1f, 1f, 1f, 0.08f);
            var skipRt = skipGo.GetComponent<RectTransform>();
            skipRt.anchorMin = skipRt.anchorMax = new Vector2(1f, 1f);
            skipRt.pivot = new Vector2(1f, 1f);
            skipRt.sizeDelta = new Vector2(120f, 30f);
            skipRt.anchoredPosition = new Vector2(-8f, -5f);
            var skipButton = skipGo.AddComponent<Button>();
            skipButton.onClick.AddListener(Skip);

            var skipLabel = CreateLabel(skipGo.transform, "SkipLabel", 18f, new Color(0.8f, 0.86f, 0.95f, 0.85f));
            skipLabel.rectTransform.anchorMin = Vector2.zero;
            skipLabel.rectTransform.anchorMax = Vector2.one;
            skipLabel.rectTransform.offsetMin = Vector2.zero;
            skipLabel.rectTransform.offsetMax = Vector2.zero;
            skipLabel.alignment = TextAlignmentOptions.Center;
            skipLabel.text = "건너뛰기 ▶";
        }

        private static TextMeshProUGUI CreateLabel(Transform parent, string name, float fontSize, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.outlineWidth = 0.16f;
            text.outlineColor = new Color(0.02f, 0.015f, 0.01f, 0.9f);
            return text;
        }
    }
}
