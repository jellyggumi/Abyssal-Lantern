using TMPro;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// The one-shot loop removed every pre-shot choice, which left the player with two blind
    /// spots the HUD never covered (qa/ux-defect-list.md UX-004, UX-005):
    ///
    ///   - what is loaded next, and what the enemy will answer with. The cycle is a pure
    ///     deterministic function that anyone can read, but nothing read it to the player, so a
    ///     predictable rule played as an unpredictable one.
    ///   - where the match is. A decided match runs about 43 turns; the only progress signal was
    ///     core HP, which is non-linear and answers a different question.
    ///
    /// Both are single lines of text, so they share one strip rather than adding two widgets to
    /// a screen the same audit found already carries overlapping elements.
    ///
    /// It sits in the selection row the one-shot loop vacated — permanently empty, not
    /// conditionally (GameManager.SetSelectionControlsVisible(false)). The audit warned that the
    /// band is unguarded: D-009 shipped cards off-screen because their coordinates were inline
    /// literals nobody asserted. So the geometry lives in consts here and HudLayoutTests pins it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SiegeForecastStrip : MonoBehaviour
    {
        /// <summary>Bottom offset of the strip's centre, inside the vacated selection row
        /// (row centre 104, half-height 40.5 → band 63.5–144.5).</summary>
        public const float StripY = 104f;
        public const float StripHeight = 34f;
        public const float StripWidth = 540f;

        /// <summary>Turns a decided match is modelled to take — the denominator the player is
        /// measured against. Derived, not guessed: material / damage-per-turn.</summary>
        public static int ModelledMatchTurns =>
            Mathf.Max(1, Mathf.RoundToInt(MatchLengthModel.TargetMatchSeconds / MatchLengthModel.AverageTurnSeconds));

        private TMP_Text label;
        private int lastTurn = int.MinValue;
        private bool lastPlayerTurn;

        public static SiegeForecastStrip Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        /// <summary>Builds the strip under the same safe-area content root the rest of the HUD
        /// uses, so a notch cannot clip it (the defect list flagged two labels that skipped it).</summary>
        public static SiegeForecastStrip Ensure()
        {
            var existing = FindAnyObjectByType<SiegeForecastStrip>();
            if (existing != null) return existing;

            // HudCanvas.Resolve, never FindObjectOfType<Canvas>: picking a canvas by iteration
            // order is what once rendered HUD text at 6.5px, and a guard test forbids it.
            var parent = HudCanvas.Root();
            if (parent == null) return null;

            // RectTransform in the constructor, matching MobileSafeArea.GetContentRoot and every
            // other UI object built in this project. The first version created a bare GameObject
            // — which comes with a plain Transform — and tried to add the RectTransform after
            // parenting. That produced no strip at all in a running scene, silently, which is
            // precisely the failure this widget was written to stop happening to other labels.
            var host = new GameObject("SiegeForecastStrip", typeof(RectTransform));
            host.transform.SetParent(parent, false);

            var rt = host.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(StripWidth, StripHeight);
            rt.anchoredPosition = new Vector2(0f, StripY);

            var text = host.AddComponent<TextMeshProUGUI>();
            text.fontSize = 17;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.86f, 0.93f, 1f, 0.95f);
            // Every runtime-built HUD label carries an outline; the four scene-serialized ones
            // that skipped it are exactly the ones the audit found unreadable over bright sky.
            text.outlineWidth = 0.16f;
            text.outlineColor = new Color(0.02f, 0.02f, 0.03f, 0.95f);
            text.raycastTarget = false;

            // The behaviour goes on last, so its Awake cannot run against a half-built object.
            var strip = host.AddComponent<SiegeForecastStrip>();
            strip.label = text;

            return strip;
        }

        /// <summary>
        /// The line itself. Pure so a test can assert the wording without standing up a Canvas.
        /// </summary>
        public static string BuildLine(int turnCount, bool isPlayerTurn)
        {
            var now = OneShotSiegeRules.ProjectileForTurn(turnCount);
            var next = OneShotSiegeRules.ProjectileForNextTurn(turnCount);

            // "Next" belongs to the other side, so name whose it is. Telling a player that
            // 화약통 is next without saying it is the ENEMY's is worse than saying nothing.
            string nextOwner = isPlayerTurn ? "적" : "내";
            int shown = Mathf.Max(0, turnCount) + 1;

            return $"이번 <b>{OneShotSiegeRules.DisplayName(now)}</b>"
                 + $"   ·   다음 {nextOwner} <b>{OneShotSiegeRules.DisplayName(next)}</b>"
                 + $"   ·   {shown}턴 / 약 {ModelledMatchTurns}턴";
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (label == null || gm == null) return;

            bool live = gm.currentState == GameState.PlayerTurn || gm.currentState == GameState.AITurn;
            if (label.gameObject.activeSelf != live) label.gameObject.SetActive(live);
            if (!live) return;

            // Redraw only on change: this line is stable for a whole turn, and the audit's
            // standing complaint is churn, not absence.
            if (gm.TurnCount == lastTurn && gm.IsPlayerTurn == lastPlayerTurn) return;
            lastTurn = gm.TurnCount;
            lastPlayerTurn = gm.IsPlayerTurn;
            label.text = BuildLine(gm.TurnCount, gm.IsPlayerTurn);
        }
    }
}
