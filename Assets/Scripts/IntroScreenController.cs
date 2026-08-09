using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// Full-screen animated title card shown before the siege starts. Built entirely at runtime
    /// (no scene edits, mirrors how the rest of the HUD is generated) and safe under
    /// Time.timeScale = 0: every animation runs on unscaled time.
    ///
    /// Presentation timeline (seconds, unscaled, from Build; scaled by EntranceTimeScale=0.85
    /// so the whole reveal lands inside a 2s budget):
    ///   0.0-0.68  black fade-in reveals the key art
    ///   0.26-0.81 title stamps down (large -> 1.0 with overshoot) + canvas jolt on impact
    ///   0.94-1.36 tagline slides up + fades in
    ///   1.19-1.62 START button pops in
    ///   1.45-1.87 how-to strip fades in
    ///   1.53-1.96 stage picker fades in (last element - finishes the transition under 2s)
    ///   loop      Ken Burns drift on the backdrop, rising ember motes, title bob, prompt pulse

    /// Dismissed by the START button, Space, or Enter (allowed mid-entrance).
    /// </summary>
    public class IntroScreenController : MonoBehaviour
    {
        private Action onStart;
        private Action onStore;
        private Action onChronicleReplay;
        private float bornAt;
        // Title-screen transition budget: playtest/QA ask was "title screen transition
        // should finish within 2 seconds" - the original stagger (fade 0-0.8s, title
        // 0.3-0.95s+jolt, tagline 1.1-1.6s, button 1.4-1.9s, how-to 1.7-2.2s, stage picker
        // 1.8-2.3s) finished at ~2.3s. Scaling every entrance timestamp/duration below by
        // this factor keeps the same relative choreography/feel but lands the last element
        // (stage picker) at ~1.96s, inside the 2s budget.
        private const float EntranceTimeScale = 0.85f;


        private RectTransform rootRect;      // joltable content container
        private RectTransform backdropRect;  // ken burns target
        private Image fadeOverlay;
        private RectTransform titleRect;
        private CanvasGroup titleGroup;
        private CanvasGroup taglineGroup;
        private RectTransform taglineRect;
        private CanvasGroup buttonGroup;
        private RectTransform buttonRect;
        private CanvasGroup howToGroup;
        private TextMeshProUGUI promptText;
        private bool joltFired;
        private CanvasGroup stagePickerGroup;
        private Image stage1Image;
        private Image stage2Image;   // Ashen Bastion (close-quarters fortress duel)
        private Image stage3Image;   // Frostbound Gorge

        private readonly List<Ember> embers = new List<Ember>();
        private static Sprite cachedEmberSprite;

        private struct Ember
        {
            public RectTransform rect;
            public Image image;
            public float x;       // base anchored x (reference-resolution units)
            public float speed;   // normalized rise per second
            public float sway;    // horizontal sway amplitude
            public float phase;
            public float size;
            public float offset;  // life-cycle offset so embers don't rise in lockstep
        }

        public static IntroScreenController Create(Action onStart, Action onStore = null, Action onChronicleReplay = null)
        {
            var go = new GameObject("IntroScreen");
            var controller = go.AddComponent<IntroScreenController>();
            controller.onStart = onStart;
            controller.onStore = onStore;
            controller.onChronicleReplay = onChronicleReplay;
            controller.Build();
            return controller;
        }

        // Pure easing kept static for EditMode tests: 0 before start, 1 after start+duration,
        // smoothstepped in between.
        public static float EasePhase(float now, float start, float duration)
        {
            if (duration <= 0f) return now >= start ? 1f : 0f;
            return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((now - start) / duration));
        }

        private void Build()
        {
            bornAt = Time.unscaledTime;

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500; // above every gameplay HUD canvas
            var fullBleedRoot = CreateChild<RectTransform>("FullBleedRoot", canvas.transform);
            Stretch(fullBleedRoot);
            MobileSafeArea.ConfigureCanvas(canvas);
            gameObject.AddComponent<CanvasGroup>();

            // Joltable container: the title-stamp impact shakes this, not the raw canvas.
            var root = CreateChild<RectTransform>("Root", MobileSafeArea.GetContentRoot(canvas));
            Stretch(root);
            rootRect = root;

            // Key art backdrop (falls back to a dark vignette when the sprite is missing).
            var art = Resources.Load<Sprite>("IntroKeyArt");
            var backdrop = CreateChild<Image>("Backdrop", fullBleedRoot);
            Stretch(backdrop.rectTransform);
            backdropRect = backdrop.rectTransform;
            if (art != null)
            {
                backdrop.sprite = art;
                backdrop.preserveAspect = false;
                backdrop.color = Color.white;
            }
            else
            {
                backdrop.color = new Color(0.07f, 0.06f, 0.11f, 1f);
            }

            // Bottom gradient scrim so title/buttons stay readable over bright art.
            var scrim = CreateChild<Image>("Scrim", fullBleedRoot);
            Stretch(scrim.rectTransform);
            scrim.color = new Color(0f, 0f, 0f, 0.35f);

            BuildEmbers(root);

            BuildBannerStrips(root);

            // Title block.
            var title = CreateChild<TextMeshProUGUI>("Title", root);
            titleRect = title.rectTransform;
            titleRect.anchorMin = new Vector2(0.5f, 0.72f);
            titleRect.anchorMax = new Vector2(0.5f, 0.72f);
            titleRect.sizeDelta = new Vector2(1400f, 220f);
            title.text = "CASTLE BUSTERS";
            title.fontSize = 128f;
            title.fontStyle = FontStyles.Bold;
            title.alignment = TextAlignmentOptions.Center;
            title.characterSpacing = 8f;
            title.color = new Color(1f, 0.9f, 0.55f, 1f);
            title.outlineWidth = 0.22f;
            title.outlineColor = new Color(0.12f, 0.05f, 0.02f, 1f);
            titleGroup = title.gameObject.AddComponent<CanvasGroup>();
            titleGroup.alpha = 0f;

            var tagline = CreateChild<TextMeshProUGUI>("Tagline", root);
            taglineRect = tagline.rectTransform;
            taglineRect.anchorMin = new Vector2(0.5f, 0.62f);
            taglineRect.anchorMax = new Vector2(0.5f, 0.62f);
            taglineRect.sizeDelta = new Vector2(1400f, 80f);
            tagline.text = "발사하라 · 부숴라 · 무너뜨려라";
            tagline.fontSize = 40f;
            tagline.alignment = TextAlignmentOptions.Center;
            tagline.color = new Color(0.95f, 0.97f, 1f, 0.95f);
            tagline.outlineWidth = 0.18f;
            tagline.outlineColor = new Color(0.05f, 0.04f, 0.08f, 0.9f);
            taglineGroup = tagline.gameObject.AddComponent<CanvasGroup>();
            taglineGroup.alpha = 0f;

            // How-to strip: teaches the one core loop before the first turn.
            var howTo = CreateChild<TextMeshProUGUI>("HowTo", root);
            howTo.rectTransform.anchorMin = new Vector2(0.5f, 0.30f);
            howTo.rectTransform.anchorMax = new Vector2(0.5f, 0.30f);
            howTo.rectTransform.sizeDelta = new Vector2(1500f, 120f);
            // One line, Korean only: the bilingual two-line version doubled the width and
            // read as a manual, not an affordance. Wind and unit keys are taught in-match.
            howTo.text = "유닛 선택 → 푸른 링에서 당겨 발사 → 적 코어 파괴";
            howTo.fontSize = 30f;
            howTo.alignment = TextAlignmentOptions.Center;
            howTo.color = new Color(0.85f, 0.92f, 1f, 0.9f);
            howTo.outlineWidth = 0.16f;
            howTo.outlineColor = new Color(0.05f, 0.04f, 0.08f, 0.9f);
            howToGroup = howTo.gameObject.AddComponent<CanvasGroup>();
            howToGroup.alpha = 0f;

            BuildStagePicker(root);

            // START button (card art face when available).
            var buttonImage = CreateChild<Image>("StartButton", root);
            buttonRect = buttonImage.rectTransform;
            buttonRect.anchorMin = new Vector2(0.5f, 0.45f);
            buttonRect.anchorMax = new Vector2(0.5f, 0.45f);
            buttonRect.sizeDelta = new Vector2(420f * 1.2f, 110f * 1.2f); // +20% (title-button playtest sizing pass), ratio unchanged

            var cardSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (cardSprite != null)
            {
                buttonImage.sprite = cardSprite;
                // 9-slice (root-cause text-overflow fix, see GimmickSpriteLibrary.ButtonCard
                // border metadata): keeps the frame's border constant regardless of stretch.
                buttonImage.type = Image.Type.Sliced;

                buttonImage.color = new Color(1f, 0.86f, 0.6f, 1f);
            }
            else
            {
                buttonImage.color = new Color(0.92f, 0.55f, 0.16f, 0.97f);
            }

            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.onClick.AddListener(HandleStart);
            buttonImage.gameObject.AddComponent<GameButtonAnimator>();
            buttonGroup = buttonImage.gameObject.AddComponent<CanvasGroup>();
            buttonGroup.alpha = 0f;

            var buttonLabel = CreateChild<TextMeshProUGUI>("Label", buttonImage.transform);
            Stretch(buttonLabel.rectTransform);
            // Inset + auto-size: the caption can never spill past the (now +20%) card face
            // regardless of viewport scale (button text-overflow QA pass). Inset and font
            // range scale with the card so the label grows in step with the button.
            buttonLabel.rectTransform.offsetMin = new Vector2(24f, 14.4f);
            buttonLabel.rectTransform.offsetMax = new Vector2(-24f, -14.4f);
            // Korean only: the bilingual label overran the 9-sliced frame's right border
            // even with auto-sizing, because fallback-font metrics are measured after the
            // first fit pass. A short label keeps the text inside the wood at full size.
            buttonLabel.text = "공성 개시";
            buttonLabel.enableWordWrapping = false;
            buttonLabel.enableAutoSizing = true;
            buttonLabel.fontSizeMin = 18f;
            buttonLabel.fontSizeMax = 40f;

            buttonLabel.fontStyle = FontStyles.Bold;
            buttonLabel.alignment = TextAlignmentOptions.Center;
            buttonLabel.color = new Color(0.12f, 0.06f, 0.02f, 1f);

            BuildStoreButton(root);
            if (MobileStoreEntitlements.HasChroniclePack && onChronicleReplay != null)
                BuildChronicleReplayButton(root);

            promptText = CreateChild<TextMeshProUGUI>("Prompt", root);
            promptText.rectTransform.anchorMin = new Vector2(0.5f, 0.375f);
            promptText.rectTransform.anchorMax = new Vector2(0.5f, 0.375f);
            promptText.rectTransform.sizeDelta = new Vector2(900f, 50f);
            promptText.text = "SPACE / ENTER";
            promptText.fontSize = 26f;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = new Color(1f, 1f, 1f, 0f);

            // Black overlay on top: fades out first, revealing the whole composition.
            fadeOverlay = CreateChild<Image>("FadeOverlay", canvas.transform);
            Stretch(fadeOverlay.rectTransform);
            fadeOverlay.color = Color.black;
            fadeOverlay.raycastTarget = false;
        }

        private void BuildStoreButton(RectTransform root)
        {
            var image = CreateChild<Image>("ChronicleStoreButton", root);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.84f, 0.45f);
            image.rectTransform.sizeDelta = new Vector2(265f, 74f);
            image.color = new Color(0.46f, 0.72f, 1f, 0.96f);
            var card = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (card != null)
            {
                image.sprite = card;
                image.type = Image.Type.Sliced;
            }

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() =>
            {
                if (onStore != null) onStore();
                else MobileStorefront.OpenStore();
            });

            var label = CreateChild<TextMeshProUGUI>("Label", image.transform);
            // Inset from the 9-sliced frame: a stretched label with no padding printed
            // straight over the card's carved border, which is what made the old
            // two-line English text unreadable at the edges.
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(20f, 12f);
            label.rectTransform.offsetMax = new Vector2(-20f, -12f);
            label.text = "연대기";
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 26f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.05f, 0.08f, 0.15f, 1f);
        }

        private void BuildChronicleReplayButton(RectTransform root)
        {
            var image = CreateChild<Image>("ChronicleReplayButton", root);
            image.rectTransform.anchorMin = image.rectTransform.anchorMax = new Vector2(0.16f, 0.45f);
            image.rectTransform.sizeDelta = new Vector2(265f, 74f);
            image.color = new Color(1f, 0.78f, 0.42f, 0.96f);
            var card = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (card != null)
            {
                image.sprite = card;
                image.type = Image.Type.Sliced;
            }

            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(() => onChronicleReplay());

            var label = CreateChild<TextMeshProUGUI>("Label", image.transform);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(20f, 12f);
            label.rectTransform.offsetMax = new Vector2(-20f, -12f);
            label.text = "프롤로그";
            label.enableWordWrapping = false;
            label.enableAutoSizing = true;
            label.fontSizeMin = 14f;
            label.fontSizeMax = 26f;
            label.fontStyle = FontStyles.Bold;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.12f, 0.06f, 0.02f, 1f);
        }

        // Stage picker ("3스테이지 시스템"): three small toggle cards below the how-to strip,
        // sitting in the open band above the bottom edge. Selecting an unlocked stage sets
        // GameManager.PendingStage and reloads the scene (same ReloadArena path as
        // Rematch/Title) so the whole runtime-generated world — ground width, launch
        // apron distance, camera framing — rebuilds fresh for that stage; there is no
        // in-place layout swap. Defaults to whichever stage is already PendingStage so a
        // reload lands with the correct card highlighted instead of always resetting to
        // Stage1. A card is locked when EITHER StageDefinitions.For(id).locked
        // (design-time "not finished/offered yet") OR the sequential campaign hasn't
        // been earned yet (StageProgress/StageProgressStore — Stage2/3 require clearing
        // the stage right before them at least once); see IsStageLocked().
        private void BuildStagePicker(RectTransform root)
        {
            var pickerGo = new GameObject("StagePicker");
            pickerGo.transform.SetParent(root, false);
            var pickerRect = pickerGo.AddComponent<RectTransform>();
            pickerRect.anchorMin = new Vector2(0.5f, 0.15f);
            pickerRect.anchorMax = new Vector2(0.5f, 0.15f);
            pickerRect.sizeDelta = new Vector2(800f, 220f);
            stagePickerGroup = pickerGo.AddComponent<CanvasGroup>();
            stagePickerGroup.alpha = 0f;
            stagePickerGroup.interactable = false;
            stagePickerGroup.blocksRaycasts = false;

            var label = CreateChild<TextMeshProUGUI>("StageLabel", pickerRect);
            label.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            label.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            label.rectTransform.pivot = new Vector2(0.5f, 1f);
            label.rectTransform.sizeDelta = new Vector2(800f, 26f);
            label.text = "전장 선택";
            label.fontSize = 18f;
            label.alignment = TextAlignmentOptions.Center;
            label.color = new Color(0.85f, 0.9f, 1f, 0.85f);
            label.outlineWidth = 0.14f;
            label.outlineColor = new Color(0.05f, 0.04f, 0.08f, 0.9f);

            // Three cards, 230 wide, centers 250 apart (20u gap) — fits the 800-wide picker
            // with margin either side. `locked` folds structural + progression gates
            // (IsStageLocked) so a not-yet-earned stage renders identically to a
            // not-yet-finished one.
            stage1Image = CreateStageButton(pickerRect, StageId.Stage1, new Vector2(-250f, -12f),
                StageDefinitions.Stage1.displayName, locked: IsStageLocked(StageId.Stage1));
            stage2Image = CreateStageButton(pickerRect, StageId.Stage2, new Vector2(0f, -12f),
                StageDefinitions.Stage2.displayName, locked: IsStageLocked(StageId.Stage2));
            stage3Image = CreateStageButton(pickerRect, StageId.Stage3, new Vector2(250f, -12f),
                StageDefinitions.Stage3.displayName, locked: IsStageLocked(StageId.Stage3));

            RefreshStagePickerVisuals();
        }

        /// <summary>Combined structural + sequential-campaign lock check for one stage
        /// card. See BuildStagePicker's remarks for the two gates this folds together.</summary>
        private static bool IsStageLocked(StageId stage) =>
            StageDefinitions.For(stage).locked || !StageProgress.IsUnlocked(StageProgressStore.Load(), stage);

        private static string GetStageCardKey(StageId stage)
        {
            switch (stage)
            {
                case StageId.Stage1: return GimmickSpriteLibrary.Stage1Card;
                case StageId.Stage2: return GimmickSpriteLibrary.Stage2Card;
                case StageId.Stage3: return GimmickSpriteLibrary.Stage3Card;
                default: return GimmickSpriteLibrary.ButtonCard;
            }
        }

        /// <summary>Card face shows only the Korean half of a "ENGLISH / 한글" stage name.
        /// The full bilingual string wrapped to three lines inside a 230x155 card and
        /// collided with the frame art; StageDefinitions keeps the long name for logs.</summary>
        private static string ShortStageName(string displayName)
        {
            if (string.IsNullOrEmpty(displayName)) return displayName;
            int split = displayName.IndexOf('/');
            return split >= 0 ? displayName.Substring(split + 1).Trim() : displayName.Trim();
        }

        private Image CreateStageButton(RectTransform parent, StageId stage, Vector2 anchoredPos, string label, bool locked)
        {
            var img = CreateChild<Image>($"Stage_{stage}", parent);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = new Vector2(230f, 155f);

            var cardSprite = GimmickSpriteLibrary.Load(GetStageCardKey(stage));
            if (cardSprite == null) cardSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (cardSprite != null) { img.sprite = cardSprite; img.type = Image.Type.Sliced; }

            else img.color = new Color(0.2f, 0.24f, 0.32f, 0.9f);

            var button = img.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.interactable = !locked;
            if (!locked) button.onClick.AddListener(() => GameManager.RequestStage(stage));
            img.gameObject.AddComponent<GameButtonAnimator>();

            var text = CreateChild<TextMeshProUGUI>("Label", img.transform);
            text.rectTransform.anchorMin = new Vector2(0.06f, 0.04f);
            text.rectTransform.anchorMax = new Vector2(0.94f, 0.40f);
            text.rectTransform.offsetMin = Vector2.zero;
            text.rectTransform.offsetMax = Vector2.zero;
            string cardName = ShortStageName(label);
            text.text = locked ? cardName + "\n잠김" : cardName;
            text.enableAutoSizing = true;
            text.fontSizeMin = 7f;
            text.fontSizeMax = 16f;
            text.enableWordWrapping = true;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = locked ? new Color(0.7f, 0.72f, 0.76f, 0.75f) : new Color(0.96f, 0.98f, 1f, 1f);
            text.outlineWidth = 0.16f;
            text.outlineColor = new Color(0.02f, 0.015f, 0.01f, 0.95f);

            return img;
        }

        private void RefreshStagePickerVisuals()
        {
            ApplyStageButtonTint(stage1Image, stage1Image != null && stage1Image.sprite != null,
                GameManager.PendingStage == StageId.Stage1, IsStageLocked(StageId.Stage1));
            ApplyStageButtonTint(stage2Image, stage2Image != null && stage2Image.sprite != null,
                GameManager.PendingStage == StageId.Stage2, IsStageLocked(StageId.Stage2));
            ApplyStageButtonTint(stage3Image, stage3Image != null && stage3Image.sprite != null,
                GameManager.PendingStage == StageId.Stage3, IsStageLocked(StageId.Stage3));
        }

        private static void ApplyStageButtonTint(Image image, bool cardLook, bool selected, bool locked)
        {
            if (image == null) return;
            if (locked)
            {
                // Always dim, never "selected" — a locked card can't be the active pick.
                image.color = cardLook
                    ? new Color(0.32f, 0.32f, 0.35f, 0.55f)
                    : new Color(0.18f, 0.2f, 0.24f, 0.5f);
                return;
            }
            image.color = cardLook
                ? (selected ? Color.white : new Color(0.55f, 0.55f, 0.6f, 0.82f))
                : (selected ? new Color(1f, 0.86f, 0.4f, 1f) : new Color(0.3f, 0.34f, 0.42f, 0.75f));
        }

        // Frame-animated war banners flanking the title (video-like loop, AC3). Driven by
        // the shared loop math on unscaled time since the intro freezes Time.timeScale.
        private Image leftBanner;
        private Image rightBanner;
        private Sprite[] bannerFrames;
        private float bannerElapsed;
        private int bannerLastFrame = -1;
        public const float BannerFps = 8f;

        private void BuildBannerStrips(RectTransform root)
        {
            bannerFrames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.IntroBanner);
            if (bannerFrames == null || bannerFrames.Length < 2) return;

            leftBanner = CreateBanner(root, new Vector2(0.18f, 0.62f), false);
            rightBanner = CreateBanner(root, new Vector2(0.82f, 0.62f), true);
        }

        private Image CreateBanner(RectTransform root, Vector2 anchor, bool mirrored)
        {
            var img = CreateChild<Image>(mirrored ? "BannerR" : "BannerL", root);
            var rt = img.rectTransform;
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(300f, 300f);
            rt.localScale = new Vector3(mirrored ? -1f : 1f, 1f, 1f);
            img.sprite = bannerFrames[0];
            img.preserveAspect = true;
            img.raycastTarget = false;
            img.color = new Color(1f, 1f, 1f, 0.96f);
            return img;
        }

        private void UpdateBanners()
        {
            if (bannerFrames == null || bannerFrames.Length < 2) return;
            bannerElapsed += Time.unscaledDeltaTime;
            int frame = GimmickFrameAnimator.LoopFrameAt(bannerElapsed, 1f / BannerFps, bannerFrames.Length);
            if (frame == bannerLastFrame) return;
            bannerLastFrame = frame;
            if (leftBanner != null) leftBanner.sprite = bannerFrames[frame];
            if (rightBanner != null) rightBanner.sprite = bannerFrames[frame];
        }

        private void BuildEmbers(RectTransform root)
        {
            // Rising warm motes over the battlefield art. Uses a sparkle frame when the dedicated
            // effect art exists, otherwise a tiny procedural dot - purely ambient either way.
            Sprite emberSprite = null;
            var sparkleFrames = EffectSpriteLibrary.LoadFrames(EffectSpriteLibrary.Sparkle);
            if (sparkleFrames != null && sparkleFrames.Length > 0) emberSprite = sparkleFrames[0];
            if (emberSprite == null) emberSprite = GetFallbackEmberSprite();

            var rng = new System.Random(20260702);
            for (int i = 0; i < 16; i++)
            {
                var image = CreateChild<Image>($"Ember_{i}", root);
                image.sprite = emberSprite;
                image.raycastTarget = false;
                image.color = new Color(1f, 0.75f, 0.35f, 0f);

                var rect = image.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                float size = Mathf.Lerp(10f, 30f, (float)rng.NextDouble());
                rect.sizeDelta = new Vector2(size, size);

                embers.Add(new Ember
                {
                    rect = rect,
                    image = image,
                    x = Mathf.Lerp(60f, 1860f, (float)rng.NextDouble()),
                    speed = Mathf.Lerp(0.05f, 0.13f, (float)rng.NextDouble()),
                    sway = Mathf.Lerp(18f, 60f, (float)rng.NextDouble()),
                    phase = (float)rng.NextDouble() * Mathf.PI * 2f,
                    size = size,
                    offset = (float)rng.NextDouble(),
                });
            }
        }

        private static Sprite GetFallbackEmberSprite()
        {
            if (cachedEmberSprite != null) return cachedEmberSprite;
            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            float center = (size - 1) / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(1f - d)));
                }
            }
            tex.Apply(false, true);
            cachedEmberSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            cachedEmberSprite.name = "GeneratedEmber";
            return cachedEmberSprite;
        }

        private void HandleStart()
        {
            var callback = onStart;
            onStart = null;
            callback?.Invoke();
        }

        public void Dismiss()
        {
            onStart = null;
            Destroy(gameObject);
        }

        private void Update()
        {
            // timeScale is 0 during the intro - drive all motion with unscaled time.
            float now = Time.unscaledTime;
            float t = now - bornAt;

            // 1) Black reveal.
            if (fadeOverlay != null)
            {
                float reveal = EasePhase(t, 0f, 0.8f * EntranceTimeScale);

                fadeOverlay.color = new Color(0f, 0f, 0f, 1f - reveal);
                if (reveal >= 1f && fadeOverlay.gameObject.activeSelf) fadeOverlay.gameObject.SetActive(false);
            }

            // 2) Ken Burns drift on the backdrop: slow zoom + gentle pan, forever.
            if (backdropRect != null)
            {
                float zoom = 1.06f + Mathf.Sin(now * 0.11f) * 0.035f;
                backdropRect.localScale = new Vector3(zoom, zoom, 1f);
                backdropRect.anchoredPosition = new Vector2(Mathf.Sin(now * 0.07f) * 22f, Mathf.Cos(now * 0.09f) * 12f);
            }

            // 3) Title stamp: oversized -> 1.0 with a decel curve, then impact jolt + idle bob.
            if (titleRect != null && titleGroup != null)
            {
                float stamp = EasePhase(t, 0.3f * EntranceTimeScale, 0.65f * EntranceTimeScale);

                titleGroup.alpha = stamp;
                float scale = Mathf.Lerp(2.1f, 1f, stamp);
                // Small overshoot right after landing.
                float overshoot = 1f + Mathf.Sin(EasePhase(t, 0.95f * EntranceTimeScale, 0.25f * EntranceTimeScale) * Mathf.PI) * 0.05f;

                titleRect.localScale = new Vector3(scale * overshoot, scale * overshoot, 1f);

                float bob = stamp >= 1f ? Mathf.Sin(now * 1.6f) * 6f : 0f;
                titleRect.anchoredPosition = new Vector2(0f, bob);

                if (!joltFired && stamp >= 1f)
                {
                    joltFired = true; // single frame flag; jolt decays below
                }
            }

            // 4) Impact jolt on the whole composition (decays over 0.35s after the stamp lands).
            if (rootRect != null)
            {
                float joltPhase = Mathf.Clamp01((t - 0.95f * EntranceTimeScale) / (0.35f * EntranceTimeScale));

                if (joltPhase > 0f && joltPhase < 1f)
                {
                    float amp = (1f - joltPhase) * 9f;
                    rootRect.anchoredPosition = new Vector2(
                        (Mathf.PerlinNoise(now * 30f, 0.3f) - 0.5f) * 2f * amp,
                        (Mathf.PerlinNoise(0.7f, now * 30f) - 0.5f) * 2f * amp);
                }
                else
                {
                    rootRect.anchoredPosition = Vector2.zero;
                }
            }

            // 5) Staggered entrances.
            if (taglineGroup != null)
            {
                float p = EasePhase(t, 1.1f * EntranceTimeScale, 0.5f * EntranceTimeScale);

                taglineGroup.alpha = p;
                taglineRect.anchoredPosition = new Vector2(0f, Mathf.Lerp(-24f, 0f, p));
            }
            if (buttonGroup != null)
            {
                float p = EasePhase(t, 1.4f * EntranceTimeScale, 0.5f * EntranceTimeScale);

                buttonGroup.alpha = p;
                float pop = Mathf.Lerp(0.6f, 1f, p) * (1f + Mathf.Sin(p * Mathf.PI) * 0.08f);
                buttonRect.localScale = new Vector3(pop, pop, 1f);
            }
            if (howToGroup != null)
            {
                howToGroup.alpha = EasePhase(t, 1.7f * EntranceTimeScale, 0.5f * EntranceTimeScale);

            }
            if (stagePickerGroup != null)
            {
                float pickerEntrance = EasePhase(t, 1.8f * EntranceTimeScale, 0.5f * EntranceTimeScale);
                stagePickerGroup.alpha = pickerEntrance;
                bool pickerReady = pickerEntrance >= 0.999f;
                stagePickerGroup.interactable = pickerReady;
                stagePickerGroup.blocksRaycasts = pickerReady;
            }

            // Waving war banners: frame-animated loop, part of the "video-like" title motion.
            UpdateBanners();

            // 6) Prompt pulse (only once the button has landed).
            if (promptText != null)
            {
                float gate = EasePhase(t, 1.9f * EntranceTimeScale, 0.3f * EntranceTimeScale);

                var c = promptText.color;
                c.a = gate * (0.45f + Mathf.PingPong(now * 0.9f, 0.5f));
                promptText.color = c;
            }

            // 7) Rising embers, looping forever.
            for (int i = 0; i < embers.Count; i++)
            {
                var e = embers[i];
                float cycle = Mathf.Repeat(now * e.speed + e.offset, 1f);
                float y = Mathf.Lerp(-40f, 1120f, cycle);
                float x = e.x + Mathf.Sin(now * 0.8f + e.phase) * e.sway;
                e.rect.anchoredPosition = new Vector2(x, y);
                // Fade in low, fade out near the top; entrance-gated by the black reveal.
                float alpha = Mathf.Clamp01(Mathf.Sin(cycle * Mathf.PI)) * 0.55f * EasePhase(t, 0.5f, 0.6f);
                var col = e.image.color;
                col.a = alpha;
                e.image.color = col;
                e.rect.localRotation = Quaternion.Euler(0f, 0f, now * 40f + e.phase * 57f);
            }
        }

        private static T CreateChild<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (typeof(T) == typeof(RectTransform))
            {
                // NOTE: no `??` here - Unity's fake-null would slip through the native
                // null-coalescing operator and hand back a missing component.
                var rect = go.GetComponent<RectTransform>();
                if (rect == null) rect = go.AddComponent<RectTransform>();
                return rect as T;
            }
            return go.AddComponent<T>();
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
