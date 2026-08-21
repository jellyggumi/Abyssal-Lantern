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
        private UnityEngine.UI.Button swapButton;
        private TMP_Text swapLabel;
        private int lastTurn = int.MinValue;
        private bool lastPlayerTurn;
        private bool lastSwapped;

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
            strip.BuildSwapButton(rt);

            return strip;
        }

        /// <summary>
        /// The line itself. Pure so a test can assert the wording without standing up a Canvas.
        /// </summary>
        public static string BuildLine(int turnCount, bool isPlayerTurn)
            => BuildLine(turnCount, isPlayerTurn, swappedThisTurn: false);

        /// <summary>Swap-aware overload. The swapped flag redraws "이번" with the paid
        /// projectile — the strip must promise exactly what the launcher will load, and
        /// after a swap that is no longer the plain cycle entry.</summary>
        public static string BuildLine(int turnCount, bool isPlayerTurn, bool swappedThisTurn)
        {
            var now = swappedThisTurn
                ? OneShotSiegeRules.SwappedProjectileForTurn(turnCount)
                : OneShotSiegeRules.ProjectileForTurn(turnCount);
            var next = OneShotSiegeRules.ProjectileForNextTurn(turnCount);

            // "Next" belongs to the other side, so name whose it is. Telling a player that
            // 화약통 is next without saying it is the ENEMY's is worse than saying nothing.
            string nextOwner = isPlayerTurn ? "적" : "내";
            int shown = Mathf.Max(0, turnCount) + 1;

            return $"이번 <b>{OneShotSiegeRules.DisplayName(now)}</b>"
                 + (swappedThisTurn ? " <color=#FFD64C>(교체)</color>" : "")
                 + $"   ·   다음 {nextOwner} <b>{OneShotSiegeRules.DisplayName(next)}</b>"
                 + $"   ·   {shown}턴 / 약 {ModelledMatchTurns}턴";
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (label == null || gm == null) return;

            bool live = gm.currentState == GameState.PlayerTurn || gm.currentState == GameState.AITurn;
            if (label.gameObject.activeSelf != live) label.gameObject.SetActive(live);
            if (swapButton != null)
            {
                // The swap is sold only when it can actually be bought: player's actionable
                // window, not yet swapped, affordable. Showing a dead button teaches the
                // player it is decoration; hiding it entirely teaches them it doesn't exist —
                // so it stays visible while live but greys out when unaffordable.
                bool playerWindow = live && gm.currentState == GameState.PlayerTurn
                    && gm.IsPlayerTurn && !gm.IsResolvingTurn && gm.EnforcesOneShotTurns;
                bool purchasable = playerWindow && !gm.HasSwappedThisTurn;
                if (swapButton.gameObject.activeSelf != purchasable)
                    swapButton.gameObject.SetActive(purchasable);
                if (purchasable)
                {
                    var deployment = DeploymentController.Instance;
                    bool affordable = deployment != null
                        && deployment.PlayerSupply + 1e-4f >= OneShotSiegeRules.SwapCost;
                    swapButton.interactable = affordable;
                    var c = swapLabel.color;
                    c.a = affordable ? 1f : 0.45f;
                    swapLabel.color = c;
                }
            }
            if (!live) return;

            // Redraw only on change: this line is stable for a whole turn — except the swap,
            // which changes it mid-turn, so the swap state joins the change key.
            if (gm.TurnCount == lastTurn && gm.IsPlayerTurn == lastPlayerTurn
                && gm.HasSwappedThisTurn == lastSwapped) return;
            lastTurn = gm.TurnCount;
            lastPlayerTurn = gm.IsPlayerTurn;
            lastSwapped = gm.HasSwappedThisTurn;
            label.text = BuildLine(gm.TurnCount, gm.IsPlayerTurn, lastSwapped);
        }

        /// <summary>
        /// The swap verb: one small button at the strip's right edge. Costs SwapCost supply,
        /// advances this turn's projectile one cycle step (GameManager owns the rule). Built
        /// beside the strip so the choice lives exactly where the forecast poses it.
        /// </summary>
        private void BuildSwapButton(RectTransform stripRt)
        {
            var host = new GameObject("ProjectileSwapButton", typeof(RectTransform));
            host.transform.SetParent(stripRt, false);

            var rt = host.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(96f, 30f);
            rt.anchoredPosition = new Vector2(10f, 0f);

            var bg = host.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.13f, 0.1f, 0.06f, 0.88f);

            swapButton = host.AddComponent<UnityEngine.UI.Button>();
            swapButton.onClick.AddListener(() => GameManager.Instance?.TryPurchaseProjectileSwap());

            var textGo = new GameObject("Label", typeof(RectTransform));
            textGo.transform.SetParent(host.transform, false);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            swapLabel = textGo.AddComponent<TextMeshProUGUI>();
            swapLabel.fontSize = 15;
            swapLabel.alignment = TextAlignmentOptions.Center;
            swapLabel.color = new Color(1f, 0.84f, 0.3f, 1f);
            swapLabel.outlineWidth = 0.16f;
            swapLabel.outlineColor = new Color(0.02f, 0.02f, 0.03f, 0.95f);
            swapLabel.text = $"교체 {OneShotSiegeRules.SwapCost:0}";
        }
    }
}
