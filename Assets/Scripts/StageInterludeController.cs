using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// Full-screen narration cutscene surface (인트로 / 스테이지간 컷씬 / 내레이션 씬).
    ///
    /// Built entirely at runtime like the webtoon prologue, so `SampleScene` stays untouched.
    /// Plays an <see cref="InterludeScript"/>: a backdrop panel, a heading, and narration
    /// lines revealed by <see cref="NarrativeTypewriter"/>, then hands control back through
    /// its completion callback.
    ///
    /// Runs on <see cref="Time.unscaledTime"/> because every caller freezes the board
    /// (`Time.timeScale = 0`) while a cutscene plays — a scaled clock would hang forever.
    ///
    /// Input contract: click / Space / Enter completes the current line instantly, and a
    /// second press advances to the next one. Escape skips the whole scene. A cutscene the
    /// player cannot leave is a cutscene they resent on the second playthrough.
    /// </summary>
    public sealed class StageInterludeController : MonoBehaviour
    {
        public static StageInterludeController Active { get; private set; }

        private InterludeScript script;
        private Action onComplete;
        private float bornAt;
        private float manualAdvanceSeconds;
        private bool completed;

        private CanvasGroup rootGroup;
        private Image backdrop;
        private Image vignette;
        private Image fadeOverlay;
        private TextMeshProUGUI headingText;
        private TextMeshProUGUI speakerText;
        private TextMeshProUGUI bodyText;
        private TextMeshProUGUI skipHint;

        private NarrativeTypewriter typewriter;
        private int typedLineIndex = -1;

        /// <summary>Elapsed script time, including any manual advance the player forced.</summary>
        public float Elapsed => Time.unscaledTime - bornAt + manualAdvanceSeconds;
        public InterludeKind Kind => script.kind;

        /// <summary>
        /// Plays <paramref name="script"/> and invokes <paramref name="onComplete"/> exactly
        /// once when it ends (played out, skipped, or dismissed). An empty script completes
        /// immediately rather than showing a blank screen.
        /// </summary>
        public static StageInterludeController Play(InterludeScript script, Action onComplete)
        {
            if (!script.HasContent)
            {
                onComplete?.Invoke();
                return null;
            }

            if (Active != null) Active.Dismiss();

            var go = new GameObject("StageInterlude");
            var controller = go.AddComponent<StageInterludeController>();
            controller.script = script;
            controller.onComplete = onComplete;
            controller.Build();
            Active = controller;
            return controller;
        }

        private void Build()
        {
            bornAt = Time.unscaledTime;

            var canvasGo = new GameObject("InterludeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above the results screen and HUD: a cutscene owns the screen while it runs.
            canvas.sortingOrder = 900;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            rootGroup = canvasGo.AddComponent<CanvasGroup>();
            rootGroup.alpha = 0f;

            backdrop = CreateStretched<Image>("Backdrop", canvasGo.transform);
            var panel = string.IsNullOrEmpty(script.backdropKey) ? null : Resources.Load<Sprite>(script.backdropKey);
            if (panel != null)
            {
                backdrop.sprite = panel;
                backdrop.preserveAspect = true;
                // Tone-wash the photo panel so white body text stays legible over any artwork.
                backdrop.color = new Color(script.tone.r + 0.45f, script.tone.g + 0.45f, script.tone.b + 0.45f, 1f);
            }
            else
            {
                backdrop.color = script.tone;
            }

            vignette = CreateStretched<Image>("Vignette", canvasGo.transform);
            vignette.color = new Color(script.tone.r, script.tone.g, script.tone.b, 0.62f);
            vignette.raycastTarget = false;

            headingText = CreateText("Heading", canvasGo.transform,
                new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.90f), 54f, TextAlignmentOptions.Left);
            headingText.text = script.heading;
            headingText.color = script.accent;
            headingText.fontStyle = FontStyles.Bold;

            speakerText = CreateText("Speaker", canvasGo.transform,
                new Vector2(0.08f, 0.30f), new Vector2(0.92f, 0.37f), 34f, TextAlignmentOptions.Left);
            speakerText.color = script.accent;
            speakerText.fontStyle = FontStyles.Bold;

            bodyText = CreateText("Body", canvasGo.transform,
                new Vector2(0.08f, 0.13f), new Vector2(0.92f, 0.30f), 40f, TextAlignmentOptions.TopLeft);
            bodyText.color = new Color(0.97f, 0.97f, 0.99f, 1f);
            bodyText.enableWordWrapping = true;

            skipHint = CreateText("SkipHint", canvasGo.transform,
                new Vector2(0.55f, 0.04f), new Vector2(0.95f, 0.10f), 24f, TextAlignmentOptions.Right);
            skipHint.text = "클릭 / Space — 다음    ·    Esc — 건너뛰기";
            skipHint.color = new Color(0.82f, 0.86f, 0.92f, 0.8f);

            fadeOverlay = CreateStretched<Image>("Fade", canvasGo.transform);
            fadeOverlay.color = Color.black;
            fadeOverlay.raycastTarget = false;

            KoreanFontSupport.EnsureFallback();
        }

        private T CreateStretched<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var comp = go.AddComponent<T>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return comp;
        }

        private TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, float size, TextAlignmentOptions align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            text.fontSize = size;
            text.alignment = align;
            text.raycastTarget = false;
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color(0.02f, 0.02f, 0.04f, 0.95f);
            return text;
        }

        private void Update()
        {
            if (completed) return;

            float elapsed = Elapsed;
            HandleInput();
            UpdateFade(elapsed);
            UpdateLine(elapsed);

            if (StageInterlude.IsComplete(script, elapsed)) Complete();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Complete();
                return;
            }

            bool advance = Input.GetMouseButtonDown(0)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter);
            if (!advance) return;

            // First press finishes the reveal, second moves on — the standard VN contract, so
            // a fast reader is never held hostage by the typewriter.
            if (typewriter != null && !typewriter.IsComplete)
            {
                typewriter.RevealAll();
                bodyText.text = typewriter.VisibleText;
                return;
            }
            AdvanceLine();
        }

        /// <summary>Jumps the clock to the start of the next line.</summary>
        private void AdvanceLine()
        {
            float elapsed = Elapsed;
            int index = StageInterlude.LineIndexAt(script, elapsed);
            if (index < 0)
            {
                // Still in pre-roll: skip straight to the first line.
                manualAdvanceSeconds += Mathf.Max(0f, StageInterlude.PreRollSeconds - elapsed);
                return;
            }

            float remaining = StageInterlude.LineDurationSeconds(script.Lines[index].text)
                - StageInterlude.TimeInLine(script, elapsed);
            manualAdvanceSeconds += Mathf.Max(0f, remaining);
        }

        private void UpdateFade(float elapsed)
        {
            // Fade up out of black during pre-roll, and back down during the tail.
            float total = StageInterlude.TotalDurationSeconds(script);
            float inAlpha = 1f - Mathf.Clamp01(elapsed / Mathf.Max(0.01f, StageInterlude.PreRollSeconds));
            float tailStart = total - StageInterlude.TailSeconds;
            float outAlpha = Mathf.Clamp01((elapsed - tailStart) / Mathf.Max(0.01f, StageInterlude.TailSeconds));
            fadeOverlay.color = new Color(0f, 0f, 0f, Mathf.Max(inAlpha, outAlpha));
            if (rootGroup != null) rootGroup.alpha = 1f;
        }

        private void UpdateLine(float elapsed)
        {
            int index = StageInterlude.LineIndexAt(script, elapsed);
            if (index < 0)
            {
                bodyText.text = string.Empty;
                speakerText.text = string.Empty;
                return;
            }

            if (index != typedLineIndex)
            {
                typedLineIndex = index;
                var line = script.Lines[index];
                typewriter = new NarrativeTypewriter(line.text, StageInterlude.TypeCharactersPerSecond);
                speakerText.text = line.IsNarration ? string.Empty : $"— {line.speaker}";
                // Narration is impersonal prose; spoken lines get the accent + quotes so the
                // two never read as the same voice.
                bodyText.color = line.IsNarration
                    ? new Color(0.94f, 0.95f, 0.98f, 1f)
                    : new Color(1f, 0.96f, 0.86f, 1f);
            }

            typewriter?.Advance(Time.unscaledDeltaTime);
            if (typewriter != null) bodyText.text = typewriter.VisibleText;
        }

        private void Complete()
        {
            if (completed) return;
            completed = true;
            var callback = onComplete;
            onComplete = null;
            if (Active == this) Active = null;
            Destroy(gameObject);
            callback?.Invoke();
        }

        /// <summary>Tears the surface down WITHOUT running the completion callback.</summary>
        public void Dismiss()
        {
            completed = true;
            onComplete = null;
            if (Active == this) Active = null;
            Destroy(gameObject);
        }

        private void OnDestroy()
        {
            if (Active == this) Active = null;
        }
    }
}
