using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// Battlefield alarm feed + flow-state strip (flow-clarity pass).
    ///
    /// ALARM FEED (top-left): every meaningful battlefield event — vents erupting, balance
    /// events, beast phase shifts, brick builds, item drops/pickups, capture progress —
    /// posts one line via <see cref="Post"/>. Up to 4 lines stack newest-first and fade out,
    /// so the player can always reconstruct "what just changed" at a glance.
    ///
    /// FLOW STRIP (top-center, under the turn bar): one short line that always states what
    /// the game is doing RIGHT NOW (aim / volley resolving / enemy battery / build window),
    /// killing the "is it frozen or thinking?" ambiguity.
    /// </summary>
    public class SiegeAlarmSystem : MonoBehaviour
    {
        public static SiegeAlarmSystem Instance { get; private set; }

        private const int MaxLines = 4;
        private const float LineLifetime = 4.5f;

        private struct AlarmLine
        {
            public TextMeshProUGUI text;
            public float bornAt;
        }

        private readonly List<AlarmLine> lines = new List<AlarmLine>();
        private RectTransform feedRoot;
        private TextMeshProUGUI flowStrip;
        private Canvas canvas;

        public static void Post(string message, Color color)
        {
            if (Instance == null) return;
            Instance.PostInternal(message, color);
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void EnsureUi()
        {
            if (feedRoot != null) return;
            canvas = null;
            foreach (var c in FindObjectsOfType<Canvas>())
            {
                if (c.GetComponent<IntroScreenController>() != null) continue;
                if (c.GetComponent<ResultsScreenController>() != null) continue;
                canvas = c;
                break;
            }
            if (canvas == null) return;
            MobileSafeArea.ConfigureCanvas(canvas);

            var rootGo = new GameObject("SiegeAlarmFeed");
            rootGo.transform.SetParent(MobileSafeArea.GetContentRoot(canvas), false);
            feedRoot = rootGo.AddComponent<RectTransform>();
            feedRoot.anchorMin = feedRoot.anchorMax = new Vector2(0.015f, 0.72f);
            feedRoot.pivot = new Vector2(0f, 1f);
            feedRoot.sizeDelta = new Vector2(460f, 150f);

            var stripGo = new GameObject("FlowStateStrip");
            stripGo.transform.SetParent(MobileSafeArea.GetContentRoot(canvas), false);
            flowStrip = stripGo.AddComponent<TextMeshProUGUI>();
            flowStrip.fontSize = 17f;
            flowStrip.fontStyle = FontStyles.Bold;
            flowStrip.alignment = TextAlignmentOptions.Center;
            flowStrip.enableWordWrapping = false;
            flowStrip.raycastTarget = false;
            TryApplyOutline(flowStrip, 0.16f, new Color(0.02f, 0.02f, 0.04f, 0.9f));
            var stripRt = stripGo.GetComponent<RectTransform>();
            stripRt.anchorMin = stripRt.anchorMax = new Vector2(0.5f, 0.9f);
            stripRt.pivot = new Vector2(0.5f, 1f);
            stripRt.sizeDelta = new Vector2(700f, 26f);
        }

        /// <summary>
        /// Applies a text outline only when TMP can actually build the material instance.
        /// `outlineWidth` internally does `new Material(fontSharedMaterial)`, which throws
        /// ArgumentNullException when the font asset has not resolved yet — reachable in
        /// batchmode and on the very first frame after a scene load, before TMP's default
        /// font is bound. An alarm that cannot draw an outline is cosmetically poorer; an
        /// alarm that throws takes the caller down with it (this aborted a PlayMode run when
        /// selecting the Cannon card posted an alarm during scene setup).
        /// </summary>
        private static void TryApplyOutline(TextMeshProUGUI text, float width, Color color)
        {
            if (text == null) return;
            if (text.font == null || text.fontSharedMaterial == null) return;
            try
            {
                text.outlineWidth = width;
                text.outlineColor = color;
            }
            catch (System.ArgumentNullException)
            {
                // TMP resolved a font but not its material; readable text without an outline
                // is strictly better than no text at all.
            }
        }

        private void PostInternal(string message, Color color)
        {
            EnsureUi();
            if (feedRoot == null) return;

            var go = new GameObject("AlarmLine");
            go.transform.SetParent(feedRoot, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = $"▶ {message}";
            text.fontSize = 16f;
            text.fontStyle = FontStyles.Bold;
            text.color = color;
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            TryApplyOutline(text, 0.15f, new Color(0.02f, 0.02f, 0.04f, 0.92f));
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(0f, 24f);

            lines.Insert(0, new AlarmLine { text = text, bornAt = Time.unscaledTime });
            while (lines.Count > MaxLines)
            {
                var last = lines[lines.Count - 1];
                lines.RemoveAt(lines.Count - 1);
                if (last.text != null) Destroy(last.text.gameObject);
            }
            LayoutLines();
        }

        private void LayoutLines()
        {
            for (int i = 0; i < lines.Count; i++)
            {
                var t = lines[i].text;
                if (t == null) continue;
                t.rectTransform.anchoredPosition = new Vector2(0f, -i * 25f);
            }
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            bool overlay = gm == null || gm.currentState == GameState.Intro || gm.currentState == GameState.GameOver;

            // Expire aged alarm lines (unscaled: alarms must age even during freezes).
            bool removed = false;
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                var line = lines[i];
                if (line.text == null) { lines.RemoveAt(i); removed = true; continue; }
                float age = Time.unscaledTime - line.bornAt;
                if (age >= LineLifetime)
                {
                    Destroy(line.text.gameObject);
                    lines.RemoveAt(i);
                    removed = true;
                }
                else if (age > LineLifetime - 1f)
                {
                    var c = line.text.color;
                    c.a = LineLifetime - age;
                    line.text.color = c;
                }
            }
            if (removed) LayoutLines();

            if (feedRoot != null && feedRoot.gameObject.activeSelf == overlay)
            {
                feedRoot.gameObject.SetActive(!overlay);
            }

            UpdateFlowStrip(gm, overlay);
        }

        private void UpdateFlowStrip(GameManager gm, bool overlay)
        {
            EnsureUi();
            if (flowStrip == null) return;
            if (overlay)
            {
                if (flowStrip.gameObject.activeSelf) flowStrip.gameObject.SetActive(false);
                return;
            }
            if (gm.IsPlayerTurn && !gm.IsResolvingTurn)
            {
                if (flowStrip.gameObject.activeSelf) flowStrip.gameObject.SetActive(false);
                return;
            }
            if (!flowStrip.gameObject.activeSelf) flowStrip.gameObject.SetActive(true);

            string text;
            Color color;
            if (gm.IsResolvingTurn)
            {
                // Animated ellipsis: motion in the strip itself proves the game is alive.
                int dots = 1 + (int)(Time.time * 2.5f) % 3;
                text = $"볼리 해결 중{new string('.', dots)}"; // no ⚔: glyph missing from base TMP font
                color = new Color(1f, 0.82f, 0.35f, 1f);
            }
            else
            {
                int dots = 1 + (int)(Time.time * 2.5f) % 3;
                text = $"적 포격 준비 중{new string('.', dots)}  ·  클릭: 벽돌 예약";
                color = new Color(1f, 0.55f, 0.4f, 1f);
            }
            flowStrip.text = text;
            flowStrip.color = color;
        }
    }
}
