using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// 11-page pre-title webtoon prologue shown before the title card.
    /// Built entirely at runtime so the SampleScene asset stays untouched.
    ///
    /// Visual approach:
    /// - reuses the project's generated frame strips (PerfectPixel-style exported PNG sequences)
    ///   under Resources/GeneratedUnitFrames and Resources/Gimmicks
    /// - pixel-snaps UI motion so the comic panels and character loops feel consistent with the
    ///   game's low-resolution sprite language instead of introducing smooth sub-pixel shimmer
    /// - hands off to IntroScreenController after the last panel, keeping the existing title UX
    /// </summary>
    public class WebtoonPrologueController : MonoBehaviour
    {
        public const int PageCount = 11;
        public const float DefaultHoldSeconds = 1.7f;
        public const float DefaultTransitionSeconds = 0.45f;
        public const float UiPixelsPerUnit = 32f;
        private const float ActorFps = 8f;
        private const float PageWidth = 1920f;
        private const float PageHeight = 1080f;
        // Cold-open pacing: playtest feedback was the very first cinematic cut straight from
        // a blank/loading screen into the fade-in with no beat to breathe, feeling abrupt.
        // Holding on solid black for this long before the fade-in (and every other timed
        // element, since they all derive from the same `elapsed` clock) starts gives the
        // opening a deliberate pause instead of an instant jump-cut.
        private const float PreRollSeconds = 1.2f;


        private static readonly ProloguePage[] Pages =
        {
            new ProloguePage("01", "새벽 4시 · 북벽 외곽", "검은 성벽 너머, 적의 심장이 깨어난다.", "오늘은 성이 무너진다.", "KnightPrologue", "Core", new Color(0.18f, 0.16f, 0.22f, 1f), new Color(1f, 0.82f, 0.48f, 1f)),
            new ProloguePage("02", "전열 브리핑", "돌격병은 벽을 열고, 모든 각도를 몸으로 버틴다.", "정면은 내가 연다.", "Knight", null, new Color(0.16f, 0.18f, 0.24f, 1f), new Color(0.6f, 0.84f, 1f, 1f)),
            new ProloguePage("03", "풍향 확인", "궁수는 바람을 읽고, 곡선을 먼저 쏜다.", "동풍이야. 화살이 성벽을 돌아간다.", "Archer", null, new Color(0.14f, 0.17f, 0.21f, 1f), new Color(0.78f, 0.92f, 1f, 1f)),
            new ProloguePage("04", "화약 적재", "화약통은 균열을 찾는다. 작은 틈이 성 전체를 무너뜨린다.", "한 번만 비면, 나머지는 연쇄다.", "Barrel", "Knight", new Color(0.22f, 0.14f, 0.14f, 1f), new Color(1f, 0.65f, 0.36f, 1f)),
            new ProloguePage("05", "중앙 교전지", "전장에는 화약통과 기믹이 숨어 있다. 잘 쓰면 길이 되고, 잘못 건드리면 함정이 된다.", "저 통 하나가 성문보다 무서울 때가 있어.", "Knight", "Barrel", new Color(0.19f, 0.15f, 0.13f, 1f), new Color(1f, 0.76f, 0.42f, 1f)),
            new ProloguePage("06", "적 성심부", "적의 코어는 벽보다 깊숙이 숨었지만, 무너지기 시작한 성은 스스로 적을 배신한다.", "벽이 아니라 심장을 겨눠.", "Archer", "Core", new Color(0.16f, 0.13f, 0.18f, 1f), new Color(1f, 0.48f, 0.54f, 1f)),
            new ProloguePage("07", "폭풍 전야", "바람은 매 턴 바뀐다. 같은 사격은 두 번 다시 없다.", "한 발 늦으면, 바람이 먼저 변해.", "Archer", "Knight", new Color(0.13f, 0.18f, 0.22f, 1f), new Color(0.7f, 0.9f, 1f, 1f)),
            new ProloguePage("08", "붕괴 계산", "벽돌 하나의 균열, 포탄 하나의 착탄, 낙하 하나의 무게가 승부를 바꾼다.", "무너지는 건 블록이 아니라 균형이야.", "Knight", "Barrel", new Color(0.2f, 0.16f, 0.12f, 1f), new Color(1f, 0.78f, 0.52f, 1f)),
            new ProloguePage("09", "성벽 위의 결의", "살아남은 병사는 다시 일어나고, 마지막 한 발은 언제나 역전을 꿈꾼다.", "끝까지 남으면, 마지막 한 발이 온다.", "Knight", "Archer", new Color(0.12f, 0.16f, 0.24f, 1f), new Color(0.84f, 0.92f, 1f, 1f)),
            new ProloguePage("10", "돌입 직전", "선택하라. 돌격병, 궁수, 대포, 그리고 전장을 바꾸는 기믹.", "누굴 먼저 쏘든, 목표는 하나야.", "Knight", "Barrel", new Color(0.21f, 0.15f, 0.1f, 1f), new Color(1f, 0.74f, 0.4f, 1f)),
            new ProloguePage("11", "첫 포성", "성문을 넘어, 코어를 부수고, 성을 무너뜨려라.", "이제 — 공성을 시작한다.", "Knight", "Core", new Color(0.15f, 0.12f, 0.1f, 1f), new Color(1f, 0.88f, 0.56f, 1f)),
        };

        private Action onComplete;
        private float bornAt;
        private float manualAdvanceSeconds;
        private bool completed;

        private RectTransform stripRect;
        private RectTransform viewportRect;
        private CanvasGroup promptGroup;
        private TextMeshProUGUI promptText;
        private Image fadeOverlay;
        private readonly List<PageVisual> pageVisuals = new List<PageVisual>();
        private readonly List<Sparkle> sparkles = new List<Sparkle>();
        private int typedPageIndex = -1;
        private static Sprite fallbackBubbleSprite;

        private sealed class ProloguePage
        {
            public readonly string pageNo;
            public readonly string heading;
            public readonly string narration;
            public readonly string speech;
            public readonly string leftActor;
            public readonly string rightActor;
            public readonly Color tone;
            public readonly Color accent;

            public ProloguePage(string pageNo, string heading, string narration, string speech,
                string leftActor, string rightActor, Color tone, Color accent)
            {
                this.pageNo = pageNo;
                this.heading = heading;
                this.narration = narration;
                this.speech = speech;
                this.leftActor = leftActor;
                this.rightActor = rightActor;
                this.tone = tone;
                this.accent = accent;
            }
        }

        private sealed class PageVisual
        {
            public RectTransform root;
            public CanvasGroup group;
            public Image leftActor;
            public Image rightActor;
            public RectTransform leftRect;
            public RectTransform rightRect;
            public Vector2 leftBase;
            public Vector2 rightBase;
            public Sprite[] leftFrames;
            public Sprite[] rightFrames;
            public float phase;
            public float zoomSeed;
            public TextMeshProUGUI narration;
            public TextMeshProUGUI speech;
            public NarrativeTypewriter narrationTypewriter;
            public NarrativeTypewriter speechTypewriter;
        }

        private struct Sparkle
        {
            public RectTransform rect;
            public Image image;
            public float baseX;
            public float speed;
            public float sway;
            public float phase;
            public float offset;
            public float size;
        }

        public static WebtoonPrologueController Create(Action onComplete)
        {
            var go = new GameObject("WebtoonPrologue");
            var controller = go.AddComponent<WebtoonPrologueController>();
            controller.onComplete = onComplete;
            controller.Build();
            return controller;
        }

        public static float PixelSnap(float value, float pixelsPerUnit = UiPixelsPerUnit)
        {
            if (pixelsPerUnit <= 0f) return value;
            return Mathf.Round(value * pixelsPerUnit) / pixelsPerUnit;
        }

        public static float SlideProgressAt(float elapsed, float holdSeconds = DefaultHoldSeconds,
            float transitionSeconds = DefaultTransitionSeconds)
        {
            if (transitionSeconds <= 0f) return elapsed >= holdSeconds ? 1f : 0f;
            return Mathf.Clamp01((elapsed - holdSeconds) / transitionSeconds);
        }

        public static float StripPageOffsetAt(float elapsed, int totalPages = PageCount,
            float holdSeconds = DefaultHoldSeconds, float transitionSeconds = DefaultTransitionSeconds)
        {
            if (totalPages <= 1) return 0f;
            float block = Mathf.Max(0.01f, holdSeconds + transitionSeconds);
            int pageIndex = Mathf.Clamp(Mathf.FloorToInt(Mathf.Max(0f, elapsed) / block), 0, totalPages - 1);
            float inBlock = Mathf.Max(0f, elapsed - pageIndex * block);
            if (pageIndex >= totalPages - 1) return totalPages - 1;
            return pageIndex + SlideProgressAt(inBlock, holdSeconds, transitionSeconds);
        }

        private void Build()
        {
            // + PreRollSeconds: elapsed (= Time.unscaledTime - bornAt) starts negative and
            // every Clamp01/Max(0, .)-guarded timing function below naturally reads that as
            // "not started yet", holding a plain black screen for the pre-roll before the
            // fade-in and page timeline begin.
            bornAt = Time.unscaledTime + PreRollSeconds;


            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;
            var fullBleedRoot = CreateChild<RectTransform>("FullBleedRoot", canvas.transform);
            Stretch(fullBleedRoot);
            MobileSafeArea.ConfigureCanvas(canvas);
            var contentRoot = MobileSafeArea.GetContentRoot(canvas);

            viewportRect = CreateChild<RectTransform>("Viewport", contentRoot);
            Stretch(viewportRect);

            var bg = CreateChild<Image>("Backdrop", fullBleedRoot);
            Stretch(bg.rectTransform);
            var art = Resources.Load<Sprite>("IntroKeyArt");
            bg.sprite = art;
            bg.color = art != null ? new Color(0.32f, 0.32f, 0.36f, 1f) : new Color(0.07f, 0.06f, 0.11f, 1f);
            bg.preserveAspect = false;

            var dim = CreateChild<Image>("BackdropDim", fullBleedRoot);
            Stretch(dim.rectTransform);
            dim.color = new Color(0f, 0f, 0f, 0.4f);
            dim.raycastTarget = false;

            BuildSparkles(fullBleedRoot);

            stripRect = CreateChild<RectTransform>("PanelStrip", viewportRect);
            stripRect.anchorMin = new Vector2(0.5f, 0.5f);
            stripRect.anchorMax = new Vector2(0.5f, 0.5f);
            stripRect.pivot = new Vector2(0.5f, 0.5f);
            stripRect.sizeDelta = new Vector2(PageWidth * PageCount, PageHeight);
            stripRect.anchoredPosition = Vector2.zero;

            for (int i = 0; i < Pages.Length; i++)
            {
                BuildPage(i, Pages[i]);
            }

            BuildPrompt(viewportRect);

            fadeOverlay = CreateChild<Image>("FadeOverlay", canvas.transform);
            Stretch(fadeOverlay.rectTransform);
            fadeOverlay.color = Color.black;
            fadeOverlay.raycastTarget = false;
        }

        private void BuildPage(int index, ProloguePage page)
        {
            var root = CreateChild<RectTransform>($"Page_{index + 1}", stripRect);
            root.anchorMin = new Vector2(0f, 0.5f);
            root.anchorMax = new Vector2(0f, 0.5f);
            root.pivot = new Vector2(0f, 0.5f);
            root.sizeDelta = new Vector2(PageWidth, PageHeight);
            root.anchoredPosition = new Vector2(index * PageWidth, 0f);

            var panelShadow = CreateChild<Image>("Shadow", root);
            panelShadow.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panelShadow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panelShadow.rectTransform.sizeDelta = new Vector2(1580f, 900f);
            panelShadow.rectTransform.anchoredPosition = new Vector2(18f, -18f);
            panelShadow.color = new Color(0f, 0f, 0f, 0.33f);

            var panel = CreateChild<Image>("Panel", root);
            panel.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            panel.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            panel.rectTransform.sizeDelta = new Vector2(1580f, 900f);
            panel.color = new Color(0.96f, 0.94f, 0.9f, 1f);
            panel.sprite = GetBubbleSprite();
            panel.type = Image.Type.Sliced;

            var matte = CreateChild<Image>("Matte", panel.transform);
            Stretch(matte.rectTransform);
            matte.color = page.tone;

            // Painted panel art when the page has one, flat tone when it does not. The art
            // is authored at the panel's own 16:9 aspect, so it fills without letterboxing,
            // and a missing file degrades to exactly the previous look rather than a gap.
            var art = Resources.Load<Sprite>($"Webtoon/panel-{page.pageNo}");
            if (art != null)
            {
                matte.sprite = art;
                matte.color = Color.white;

                // Narration and dialogue sit directly on this art. Without a scrim the
                // bright ember panels wash out their own text, which is a readability
                // defect before it is an art one.
                var scrim = CreateChild<Image>("ArtScrim", matte.transform);
                Stretch(scrim.rectTransform);
                scrim.color = new Color(0f, 0f, 0f, 0.32f);
            }

            var tint = CreateChild<Image>("AccentWash", matte.transform);
            Stretch(tint.rectTransform);
            tint.color = new Color(page.accent.r, page.accent.g, page.accent.b, 0.07f);

            var heading = CreateChild<TextMeshProUGUI>("Heading", panel.transform);
            heading.rectTransform.anchorMin = new Vector2(0.08f, 0.89f);
            heading.rectTransform.anchorMax = new Vector2(0.92f, 0.89f);
            heading.rectTransform.sizeDelta = new Vector2(0f, 76f);
            heading.text = $"EP.0 {page.pageNo}  ·  {page.heading}";
            heading.fontSize = 34f;
            heading.fontStyle = FontStyles.Bold;
            heading.color = page.accent;
            heading.alignment = TextAlignmentOptions.Left;
            heading.outlineWidth = 0.18f;
            heading.outlineColor = new Color(0.04f, 0.04f, 0.05f, 0.95f);

            var narrationBox = CreateChild<Image>("NarrationBox", panel.transform);
            narrationBox.rectTransform.anchorMin = new Vector2(0.08f, 0.73f);
            narrationBox.rectTransform.anchorMax = new Vector2(0.92f, 0.73f);
            narrationBox.rectTransform.sizeDelta = new Vector2(0f, 120f);
            narrationBox.color = new Color(0.06f, 0.06f, 0.08f, 0.72f);
            narrationBox.sprite = GetBubbleSprite();
            narrationBox.type = Image.Type.Sliced;

            var narration = CreateChild<TextMeshProUGUI>("Narration", narrationBox.transform);
            Stretch(narration.rectTransform);
            narration.rectTransform.offsetMin = new Vector2(28f, 16f);
            narration.rectTransform.offsetMax = new Vector2(-28f, -16f);
            narration.text = string.Empty;
            narration.fontSize = 40f;
            narration.alignment = TextAlignmentOptions.Center;
            narration.color = new Color(0.98f, 0.98f, 1f, 1f);
            narration.enableWordWrapping = true;

            var speechBox = CreateChild<Image>("SpeechBox", panel.transform);
            speechBox.rectTransform.anchorMin = new Vector2(0.57f, 0.18f);
            speechBox.rectTransform.anchorMax = new Vector2(0.92f, 0.18f);
            speechBox.rectTransform.sizeDelta = new Vector2(0f, 170f);
            speechBox.color = new Color(0.98f, 0.97f, 0.93f, 0.96f);
            speechBox.sprite = GetBubbleSprite();
            speechBox.type = Image.Type.Sliced;

            var speech = CreateChild<TextMeshProUGUI>("Speech", speechBox.transform);
            Stretch(speech.rectTransform);
            speech.rectTransform.offsetMin = new Vector2(26f, 18f);
            speech.rectTransform.offsetMax = new Vector2(-26f, -18f);
            speech.text = string.Empty;
            speech.fontSize = 34f;
            speech.fontStyle = FontStyles.Bold;
            speech.alignment = TextAlignmentOptions.Center;
            speech.color = new Color(0.1f, 0.09f, 0.08f, 1f);
            speech.enableWordWrapping = true;

            var leftActor = CreateChild<Image>("LeftActor", panel.transform);
            leftActor.rectTransform.anchorMin = new Vector2(0.18f, 0.28f);
            leftActor.rectTransform.anchorMax = new Vector2(0.18f, 0.28f);
            leftActor.rectTransform.sizeDelta = new Vector2(380f, 380f);
            leftActor.preserveAspect = true;

            var rightActor = CreateChild<Image>("RightActor", panel.transform);
            rightActor.rectTransform.anchorMin = new Vector2(0.78f, 0.39f);
            rightActor.rectTransform.anchorMax = new Vector2(0.78f, 0.39f);
            rightActor.rectTransform.sizeDelta = new Vector2(320f, 320f);
            rightActor.preserveAspect = true;

            var pageNo = CreateChild<TextMeshProUGUI>("PageNo", panel.transform);
            pageNo.rectTransform.anchorMin = new Vector2(0.92f, 0.92f);
            pageNo.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
            pageNo.rectTransform.sizeDelta = new Vector2(160f, 42f);
            pageNo.alignment = TextAlignmentOptions.Right;
            pageNo.fontSize = 24f;
            pageNo.color = new Color(1f, 1f, 1f, 0.72f);
            pageNo.text = $"{index + 1}/{PageCount}";

            var group = root.gameObject.AddComponent<CanvasGroup>();

            var visual = new PageVisual
            {
                root = root,
                group = group,
                leftActor = leftActor,
                rightActor = rightActor,
                leftRect = leftActor.rectTransform,
                rightRect = rightActor.rectTransform,
                narration = narration,
                speech = speech,
                narrationTypewriter = new NarrativeTypewriter(page.narration, 52f),
                speechTypewriter = new NarrativeTypewriter(page.speech, 44f),
                leftBase = leftActor.rectTransform.anchoredPosition,
                rightBase = rightActor.rectTransform.anchoredPosition,
                leftFrames = LoadActorFrames(page.leftActor),
                rightFrames = LoadActorFrames(page.rightActor),
                phase = index * 0.73f,
                zoomSeed = 0.11f * index
            };

            ApplyInitialActorSprite(visual.leftActor, visual.leftFrames, false);
            ApplyInitialActorSprite(visual.rightActor, visual.rightFrames, true);
            pageVisuals.Add(visual);
        }

        private void BuildPrompt(RectTransform root)
        {
            // Created after the page art so every non-SKIP tap reaches the same
            // reveal/advance flow. The later prompt and SKIP controls remain above it.
            var tapCatcher = CreateChild<Image>("TapAnywhereAdvance", root);
            Stretch(tapCatcher.rectTransform);
            tapCatcher.color = new Color(0f, 0f, 0f, 0f);
            var tapAnywhereButton = tapCatcher.gameObject.AddComponent<Button>();
            tapAnywhereButton.targetGraphic = tapCatcher;
            tapAnywhereButton.onClick.AddListener(AdvancePage);

            var promptBg = CreateChild<Image>("AdvancePageButton", root);
            promptBg.rectTransform.anchorMin = new Vector2(0.5f, 0.055f);
            promptBg.rectTransform.anchorMax = new Vector2(0.5f, 0.055f);
            promptBg.rectTransform.sizeDelta = new Vector2(700f, 74f);
            promptBg.color = new Color(0f, 0f, 0f, 0.48f);
            promptBg.sprite = GetBubbleSprite();
            promptBg.type = Image.Type.Sliced;

            var advanceButton = promptBg.gameObject.AddComponent<Button>();
            advanceButton.targetGraphic = promptBg;
            advanceButton.onClick.AddListener(AdvancePage);

            promptGroup = promptBg.gameObject.AddComponent<CanvasGroup>();
            promptText = CreateChild<TextMeshProUGUI>("Prompt", promptBg.transform);
            Stretch(promptText.rectTransform);
            promptText.text = "탭: 문장 완성 / 다음 컷  ·  TAP TO CONTINUE";
            promptText.fontSize = 24f;
            promptText.alignment = TextAlignmentOptions.Center;
            promptText.color = new Color(0.96f, 0.97f, 1f, 0.96f);

            var skipBg = CreateChild<Image>("SkipToTitleButton", root);
            skipBg.rectTransform.anchorMin = new Vector2(0.89f, 0.93f);
            skipBg.rectTransform.anchorMax = new Vector2(0.89f, 0.93f);
            skipBg.rectTransform.sizeDelta = new Vector2(250f, 58f);
            skipBg.color = new Color(0.18f, 0.16f, 0.24f, 0.94f);
            skipBg.sprite = GetBubbleSprite();
            skipBg.type = Image.Type.Sliced;
            var skipButton = skipBg.gameObject.AddComponent<Button>();
            skipButton.targetGraphic = skipBg;
            skipButton.onClick.AddListener(SkipToTitle);

            var skipLabel = CreateChild<TextMeshProUGUI>("Label", skipBg.transform);
            Stretch(skipLabel.rectTransform);
            skipLabel.text = "SKIP  ·  타이틀";
            skipLabel.fontSize = 20f;
            skipLabel.fontStyle = FontStyles.Bold;
            skipLabel.alignment = TextAlignmentOptions.Center;
            skipLabel.color = new Color(0.96f, 0.97f, 1f, 0.96f);
        }

        private void BuildSparkles(RectTransform root)
        {
            var sprite = EffectSpriteLibrary.LoadParticleSprite(EffectSpriteLibrary.ParticleEmber);
            if (sprite == null)
            {
                var sparkleFrames = EffectSpriteLibrary.LoadFrames(EffectSpriteLibrary.Sparkle);
                if (sparkleFrames != null && sparkleFrames.Length > 0) sprite = sparkleFrames[0];
            }
            if (sprite == null) sprite = GetBubbleSprite();

            var rng = new System.Random(20260703);
            for (int i = 0; i < 18; i++)
            {
                var img = CreateChild<Image>($"Sparkle_{i}", root);
                img.sprite = sprite;
                img.raycastTarget = false;
                img.color = new Color(1f, 0.76f, 0.42f, 0f);
                var rect = img.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.zero;
                float size = Mathf.Lerp(8f, 22f, (float)rng.NextDouble());
                rect.sizeDelta = new Vector2(size, size);
                sparkles.Add(new Sparkle
                {
                    rect = rect,
                    image = img,
                    baseX = Mathf.Lerp(60f, 1860f, (float)rng.NextDouble()),
                    speed = Mathf.Lerp(0.035f, 0.11f, (float)rng.NextDouble()),
                    sway = Mathf.Lerp(12f, 42f, (float)rng.NextDouble()),
                    phase = (float)rng.NextDouble() * Mathf.PI * 2f,
                    offset = (float)rng.NextDouble(),
                    size = size,
                });
            }
        }

        public void AdvancePage()
        {
            if (completed) return;

            float currentElapsed = Time.unscaledTime - bornAt + manualAdvanceSeconds;
            if (currentElapsed < 0f) return;
            if (RevealCurrentPage()) return;

            float block = DefaultHoldSeconds + DefaultTransitionSeconds;
            int currentPage = Mathf.Clamp(Mathf.FloorToInt(currentElapsed / Mathf.Max(0.01f, block)), 0, PageCount - 1);
            if (currentPage >= PageCount - 1)
            {
                Complete();
                return;
            }

            float desiredElapsed = (currentPage + 1) * block;
            manualAdvanceSeconds += Mathf.Max(0f, desiredElapsed - currentElapsed);
        }

        public void SkipToTitle()
        {
            if (completed) return;
            Complete();
        }

        public void Dismiss()
        {
            completed = true;
            onComplete = null;
            Destroy(gameObject);
        }

        private void Update()
        {
            float elapsed = Time.unscaledTime - bornAt + manualAdvanceSeconds;
            UpdateFade(elapsed);
            UpdatePrompt(elapsed);
            UpdateSparkles(elapsed);
            UpdateTypewriters(elapsed);
            UpdateStrip(elapsed);
            UpdatePageVisuals(elapsed);

            float totalDuration = (PageCount - 1) * (DefaultHoldSeconds + DefaultTransitionSeconds) + DefaultHoldSeconds + 0.2f;
            if (elapsed >= totalDuration) Complete();
        }
        private void UpdateTypewriters(float elapsed)
        {
            if (elapsed < 0f || pageVisuals.Count == 0) return;

            float block = DefaultHoldSeconds + DefaultTransitionSeconds;
            int pageIndex = Mathf.Clamp(Mathf.FloorToInt(elapsed / Mathf.Max(0.01f, block)), 0, pageVisuals.Count - 1);
            if (typedPageIndex != pageIndex)
            {
                typedPageIndex = pageIndex;
                var visual = pageVisuals[pageIndex];
                visual.narrationTypewriter.Reset(visual.narrationTypewriter.FullText);
                visual.speechTypewriter.Reset(visual.speechTypewriter.FullText);
                visual.narration.text = string.Empty;
                visual.speech.text = string.Empty;
            }

            var activeVisual = pageVisuals[pageIndex];
            activeVisual.narrationTypewriter.Advance(Time.unscaledDeltaTime);
            activeVisual.speechTypewriter.Advance(Time.unscaledDeltaTime);
            activeVisual.narration.text = activeVisual.narrationTypewriter.VisibleText;
            activeVisual.speech.text = activeVisual.speechTypewriter.VisibleText;
        }

        private bool RevealCurrentPage()
        {
            if (typedPageIndex < 0 || typedPageIndex >= pageVisuals.Count) return false;
            var visual = pageVisuals[typedPageIndex];
            if (visual.narrationTypewriter.IsComplete && visual.speechTypewriter.IsComplete) return false;

            visual.narrationTypewriter.RevealAll();
            visual.speechTypewriter.RevealAll();
            visual.narration.text = visual.narrationTypewriter.VisibleText;
            visual.speech.text = visual.speechTypewriter.VisibleText;
            return true;
        }

        private void UpdateFade(float elapsed)
        {
            if (fadeOverlay == null) return;
            float reveal = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.55f));
            fadeOverlay.color = new Color(0f, 0f, 0f, 1f - reveal);
            if (reveal >= 1f && fadeOverlay.gameObject.activeSelf) fadeOverlay.gameObject.SetActive(false);
        }

        private void UpdatePrompt(float elapsed)
        {
            if (promptGroup == null || promptText == null) return;
            float gate = Mathf.Clamp01((elapsed - 0.6f) / 0.4f);
            promptGroup.alpha = gate * (0.7f + Mathf.PingPong(Time.unscaledTime * 0.7f, 0.3f));
        }

        private void UpdateSparkles(float elapsed)
        {
            for (int i = 0; i < sparkles.Count; i++)
            {
                var s = sparkles[i];
                float cycle = Mathf.Repeat(elapsed * s.speed + s.offset, 1f);
                float y = Mathf.Lerp(-30f, 1120f, cycle);
                float x = s.baseX + Mathf.Sin(elapsed * 0.9f + s.phase) * s.sway;
                s.rect.anchoredPosition = new Vector2(PixelSnap(x), PixelSnap(y));
                float alpha = Mathf.Clamp01(Mathf.Sin(cycle * Mathf.PI)) * 0.24f;
                var c = s.image.color;
                c.a = alpha;
                s.image.color = c;
                s.rect.localRotation = Quaternion.Euler(0f, 0f, elapsed * 30f + s.phase * 42f);
            }
        }

        private void UpdateStrip(float elapsed)
        {
            if (stripRect == null) return;
            float offsetPages = StripPageOffsetAt(elapsed, PageCount, DefaultHoldSeconds, DefaultTransitionSeconds);
            stripRect.anchoredPosition = new Vector2(PixelSnap(-offsetPages * PageWidth), 0f);
        }

        private void UpdatePageVisuals(float elapsed)
        {
            float pageFloat = StripPageOffsetAt(elapsed, PageCount, DefaultHoldSeconds, DefaultTransitionSeconds);
            for (int i = 0; i < pageVisuals.Count; i++)
            {
                var visual = pageVisuals[i];
                float focus = 1f - Mathf.Clamp01(Mathf.Abs(pageFloat - i));
                visual.group.alpha = 0.36f + focus * 0.64f;
                float zoom = 1f + Mathf.Sin(elapsed * 0.35f + visual.zoomSeed) * 0.015f + focus * 0.01f;
                visual.root.localScale = new Vector3(zoom, zoom, 1f);

                UpdateActorImage(visual.leftActor, visual.leftFrames, elapsed + visual.phase, false);
                UpdateActorImage(visual.rightActor, visual.rightFrames, elapsed + visual.phase * 1.23f, true);

                if (visual.leftRect != null)
                {
                    visual.leftRect.anchoredPosition = new Vector2(
                        PixelSnap(visual.leftBase.x + Mathf.Sin(elapsed * 1.8f + visual.phase) * 8f),
                        PixelSnap(visual.leftBase.y + Mathf.Sin(elapsed * 2.2f + visual.phase) * 10f));
                }
                if (visual.rightRect != null)
                {
                    visual.rightRect.anchoredPosition = new Vector2(
                        PixelSnap(visual.rightBase.x + Mathf.Sin(elapsed * 1.35f + visual.phase) * 6f),
                        PixelSnap(visual.rightBase.y + Mathf.Sin(elapsed * 1.9f + visual.phase + 0.6f) * 8f));
                }
            }
        }

        private void Complete()
        {
            if (completed) return;
            completed = true;
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
            Destroy(gameObject);
        }

        private static void ApplyInitialActorSprite(Image image, Sprite[] frames, bool mirror)
        {
            if (image == null) return;
            image.enabled = frames != null && frames.Length > 0;
            if (!image.enabled) return;
            image.sprite = frames[0];
            image.color = Color.white;
            image.rectTransform.localScale = new Vector3(mirror ? -1f : 1f, 1f, 1f);
        }

        private static void UpdateActorImage(Image image, Sprite[] frames, float elapsed, bool mirror)
        {
            if (image == null) return;
            if (frames == null || frames.Length == 0)
            {
                image.enabled = false;
                return;
            }

            image.enabled = true;
            int frame = GimmickFrameAnimator.LoopFrameAt(elapsed, 1f / ActorFps, frames.Length);
            image.sprite = frames[frame];
            image.rectTransform.localScale = new Vector3(mirror ? -1f : 1f, 1f, 1f);
        }

        private static Sprite[] LoadActorFrames(string actorKey)
        {
            if (string.IsNullOrEmpty(actorKey)) return null;

            Sprite[] frames = null;
            switch (actorKey)
            {
                case "Knight":
                case "KnightPrologue":
                case "Archer":
                // "Bomber" art folder is retained for the prologue only; no roster unit uses it.
                    frames = Resources.LoadAll<Sprite>($"GeneratedUnitFrames/{actorKey}/Idle");
                    break;
                case "Barrel":
                    frames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.BarrelAnim);
                    break;
                case "Core":
                    frames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.CoreAnim);
                    break;
            }

            if (frames != null && frames.Length > 1)
            {
                Array.Sort(frames, (a, b) => string.CompareOrdinal(a.name, b.name));
                return frames;
            }
            if (frames != null && frames.Length == 1) return frames;

            Sprite fallback = null;
            switch (actorKey)
            {
                case "Knight":
                case "KnightPrologue":
                case "Archer":
                case "Bomber": // legacy prologue art key only — not a roster unit
                    fallback = Resources.Load<Sprite>($"GeneratedUnitFrames/{actorKey}/Idle/idle_000");
                    break;
                case "Barrel":
                    fallback = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.Barrel);
                    break;
                case "Core":
                    fallback = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.Core);
                    break;
            }
            return fallback != null ? new[] { fallback } : null;
        }

        private static Sprite GetBubbleSprite()
        {
            if (fallbackBubbleSprite != null) return fallbackBubbleSprite;

            const int size = 16;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Color edge = new Color(0f, 0f, 0f, 1f);
            Color fill = Color.white;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool border = x <= 1 || x >= size - 2 || y <= 1 || y >= size - 2;
                    tex.SetPixel(x, y, border ? edge : fill);
                }
            }
            tex.Apply(false, true);
            fallbackBubbleSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size, 0, SpriteMeshType.FullRect, new Vector4(4f, 4f, 4f, 4f));
            fallbackBubbleSprite.name = "GeneratedComicBubble";
            return fallbackBubbleSprite;
        }

        private static T CreateChild<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            if (typeof(T) == typeof(RectTransform))
            {
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
