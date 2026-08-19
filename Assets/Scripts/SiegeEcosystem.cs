using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// Retention loop (AC11): start → play → results → ranking → rematch.
    /// Pure grading/ranking rules live here so EditMode tests pin the contract;
    /// persistence is PlayerPrefs JSON (local, no backend in contract).
    /// </summary>
    public static class SiegeRank
    {
        [Serializable]
        public struct Entry
        {
            public int score;
            public int turns;
            public bool victory;
            public string grade;
            public string dateIso;
        }

        [Serializable]
        public class Board
        {
            public List<Entry> entries = new List<Entry>();
        }

        public const int Capacity = 10;

        /// <summary>
        /// S: crushing breach (win in ≤8 turns). A: clean win (≤14). B: any other win.
        /// C: honorable defeat (score ≥ 300 — the keep fell but the siege cost them).
        /// D: rout. Pure so the ladder is testable and the thresholds are documented.
        /// </summary>
        public static string ComputeGrade(bool victory, int turns, int score)
        {
            if (victory)
            {
                if (turns <= 8) return "S";
                if (turns <= 14) return "A";
                return "B";
            }
            return score >= 300 ? "C" : "D";
        }

        /// <summary>
        /// Insert into a score-desc (turns-asc tiebreak) ladder capped at Capacity.
        /// Returns the 0-based rank of the new entry, or -1 when it fell off the board.
        /// The list is mutated into the post-insert state (callers persist it).
        /// </summary>
        public static int Insert(List<Entry> entries, Entry entry)
        {
            entries.Add(entry);
            var ordered = entries
                .OrderByDescending(e => e.score)
                .ThenBy(e => e.turns)
                .ThenByDescending(e => e.dateIso, StringComparer.Ordinal)
                .Take(Capacity)
                .ToList();
            entries.Clear();
            entries.AddRange(ordered);
            return entries.FindIndex(e =>
                e.score == entry.score && e.turns == entry.turns && e.dateIso == entry.dateIso);
        }
    }

    /// <summary>PlayerPrefs-backed ladder store. JSON via JsonUtility, key versioned.</summary>
    public static class LeaderboardStore
    {
        public const string PrefsKey = "CastleBusters.Leaderboard.v1";

        public static SiegeRank.Board Load()
        {
            var json = PlayerPrefs.GetString(PrefsKey, string.Empty);
            if (string.IsNullOrEmpty(json)) return new SiegeRank.Board();
            try
            {
                return JsonUtility.FromJson<SiegeRank.Board>(json) ?? new SiegeRank.Board();
            }
            catch
            {
                return new SiegeRank.Board(); // corrupt prefs never brick the results screen
            }
        }

        public static void Save(SiegeRank.Board board)
        {
            PlayerPrefs.SetString(PrefsKey, JsonUtility.ToJson(board));
            PlayerPrefs.Save();
        }

        /// <summary>Record a finished match; returns the rank (0-based) or -1.</summary>
        public static int Record(SiegeRank.Entry entry)
        {
            var board = Load();
            int rank = SiegeRank.Insert(board.entries, entry);
            Save(board);
            return rank;
        }
    }

    /// <summary>
    /// Full-screen results card, built at runtime like the intro (no scene edits) and safe
    /// under Time.timeScale = 0. Shows the outcome banner, siege stats, grade seal, the
    /// top-10 ladder with the fresh run highlighted, and REMATCH / TITLE actions.
    /// </summary>
    public class ResultsScreenController : MonoBehaviour
    {
        private float bornAt;
        private CanvasGroup rootGroup;
        private RectTransform contentRoot;
        private RectTransform gradeRect;
        // Auto-advance (sequential campaign): counts down from AutoAdvanceDelay and
        // self-triggers NEXT STAGE unless the player navigates away first (Rematch/
        // Title/manual NextStage click, or R). null when this run didn't unlock a next
        // stage (defeat, final-stage clear, or replaying an already-cleared stage).
        private StageId? pendingNextStage;
        private bool navigated;
        private TextMeshProUGUI nextStageLabel;
        // Mid-series the R hotkey should continue the SAME series (RequestNextGame), not
        // silently throw away the running win count via a full RequestRematch reset.
        private bool seriesDecidedAtBuild;
        private const float AutoAdvanceDelay = 5f;


        public static ResultsScreenController Create(bool victory, int turns, int score,
            int maxCombo, bool lastStandUsed, StageId? nextStage,
            int seriesPlayerWins, int seriesEnemyWins, int seriesGameNumber, bool seriesDecided, int seriesScoreTotal, int warChestReward)
        {
            var go = new GameObject("ResultsScreen");
            var ctrl = go.AddComponent<ResultsScreenController>();
            ctrl.Build(victory, turns, score, maxCombo, lastStandUsed, nextStage,
                seriesPlayerWins, seriesEnemyWins, seriesGameNumber, seriesDecided, seriesScoreTotal, warChestReward);
            return ctrl;
        }

        private void Build(bool victory, int turns, int score, int maxCombo, bool lastStandUsed, StageId? nextStage,
            int seriesPlayerWins, int seriesEnemyWins, int seriesGameNumber, bool seriesDecided, int seriesScoreTotal, int warChestReward)
        {
            bornAt = Time.unscaledTime;
            pendingNextStage = nextStage;
            seriesDecidedAtBuild = seriesDecided;

            bool seriesWon = seriesDecided && SiegeSeries.PlayerWonSeries(seriesPlayerWins, seriesEnemyWins);

            var canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 600; // above intro (500) and every HUD canvas
            var fullBleedRoot = CreateChild<RectTransform>("FullBleedRoot", canvas.transform);
            Stretch(fullBleedRoot);
            var dim = CreateChild<Image>("Dim", fullBleedRoot);
            Stretch(dim.rectTransform);
            dim.color = new Color(0.03f, 0.02f, 0.05f, 0.88f);

            // Outcome backdrop, above the dim and below every readout. The dim sits at 0.88 alpha,
            // so the frozen diorama it covers was already only 12% visible — this fills a surface
            // that was near-black rather than hiding a view the player had.
            //
            // Safe to draw text over without a scrim: both images measure 0.088 mean luminance
            // across all five bands the card writes into (banner, seal, stats, ladder, buttons).
            // If either is ever redrawn brighter, that measurement is what stops being true, and
            // the banner's 0.22 outline is the only thing left protecting legibility.
            var outcomeArt = Resources.Load<Sprite>(victory ? "Result/victory_hero" : "Result/defeat_keep");
            if (outcomeArt != null)
            {
                var backdrop = CreateChild<Image>("OutcomeBackdrop", fullBleedRoot);
                Stretch(backdrop.rectTransform);
                backdrop.sprite = outcomeArt;
                backdrop.preserveAspect = true;
                backdrop.raycastTarget = false;
            }
            MobileSafeArea.ConfigureCanvas(canvas);
            contentRoot = MobileSafeArea.GetContentRoot(canvas);
            rootGroup = gameObject.AddComponent<CanvasGroup>();
            rootGroup.alpha = 0f;

            // Outcome banner. Best-of-3 series (SiegeSeries): mid-series this reports the
            // single game's result ("GAME n/3") since the series itself is still open; only
            // once the series is decided (2 wins clinched, or all 3 games played) does the
            // banner switch to the final SERIES verdict.
            var banner = CreateChild<TextMeshProUGUI>("Banner", contentRoot);
            banner.rectTransform.anchorMin = banner.rectTransform.anchorMax = new Vector2(0.5f, 0.86f);
            banner.rectTransform.sizeDelta = new Vector2(1400f, 130f);
            banner.text = seriesDecided
                ? (seriesWon
                    ? $"SERIES WON — VICTORY! ({seriesPlayerWins}-{seriesEnemyWins})\n시리즈 승리"
                    : $"SERIES LOST — DEFEAT ({seriesPlayerWins}-{seriesEnemyWins})\n시리즈 패배")
                : (victory
                    ? $"GAME {seriesGameNumber}/{SiegeSeries.MaxGames} — WIN\n이번 경기 승리"
                    : $"GAME {seriesGameNumber}/{SiegeSeries.MaxGames} — LOSS\n이번 경기 패배");
            banner.fontSize = 64f;
            banner.fontStyle = FontStyles.Bold;
            banner.alignment = TextAlignmentOptions.Center;
            banner.color = (seriesDecided ? seriesWon : victory) ? new Color(1f, 0.87f, 0.35f, 1f) : new Color(1f, 0.42f, 0.32f, 1f);
            banner.outlineWidth = 0.22f;
            banner.outlineColor = new Color(0.1f, 0.04f, 0.02f, 1f);

            // Grade seal (stamped in by Update animation). Mid-series it grades this one
            // game; once the series is decided it grades the series as a whole (aggregate
            // score/outcome), matching what actually gets persisted to the leaderboard below.
            int gradeScore = seriesDecided ? SiegeSeries.SeriesScore(seriesScoreTotal, seriesPlayerWins, seriesEnemyWins) : score;
            bool gradeVictory = seriesDecided ? seriesWon : victory;
            string grade = SiegeRank.ComputeGrade(gradeVictory, turns, gradeScore);
            var gradeText = CreateChild<TextMeshProUGUI>("Grade", contentRoot);
            gradeRect = gradeText.rectTransform;
            gradeRect.anchorMin = gradeRect.anchorMax = new Vector2(0.78f, 0.62f);
            gradeRect.sizeDelta = new Vector2(260f, 260f);
            gradeText.text = grade;
            gradeText.fontSize = 170f;
            gradeText.fontStyle = FontStyles.Bold;
            gradeText.alignment = TextAlignmentOptions.Center;
            gradeText.color = grade == "S" ? new Color(1f, 0.82f, 0.2f, 1f)
                : grade == "A" ? new Color(0.55f, 0.95f, 1f, 1f)
                : grade == "B" ? new Color(0.6f, 1f, 0.65f, 1f)
                : grade == "C" ? new Color(1f, 0.75f, 0.45f, 1f)
                : new Color(0.8f, 0.6f, 0.6f, 1f);
            gradeText.outlineWidth = 0.24f;
            gradeText.outlineColor = new Color(0.08f, 0.05f, 0.02f, 1f);

            // Stats block.
            var stats = CreateChild<TextMeshProUGUI>("Stats", contentRoot);
            stats.rectTransform.anchorMin = stats.rectTransform.anchorMax = new Vector2(0.28f, 0.62f);
            stats.rectTransform.sizeDelta = new Vector2(680f, 260f);
            // Single-language rows: the bilingual duplicates doubled the text volume for
            // zero information (UX text-diet pass).
            stats.text = $"<b>전과 보고</b>\n" +
                         $"턴 <b>{turns}</b> · 점수 <b>{score}</b>\n" +
                         $"최대 콤보 <b>x{maxCombo}</b>" +
                         (lastStandUsed ? "\n일발역전 <b>발동</b>" : "") +
                         $"\n<b>시리즈 {seriesPlayerWins} : {seriesEnemyWins}</b> (3전 2선승)";
            stats.fontSize = 34f;
            stats.alignment = TextAlignmentOptions.Left;
            stats.color = new Color(0.92f, 0.96f, 1f, 1f);
            stats.outlineWidth = 0.14f;
            stats.outlineColor = new Color(0.05f, 0.04f, 0.08f, 0.9f);
            if (seriesDecided && seriesWon)
                stats.text += $"\n전공 인장 <b>+{warChestReward}</b> · 휘장 교환 가능";

            // Persist + render the ladder — only once the SERIES is decided. A best-of-3
            // series is a single ranked contest: recording every individual game would let
            // one series inflate the ladder with up to 3 entries and rank mid-series games
            // that don't reflect who actually won the match.
            var ladder = CreateChild<TextMeshProUGUI>("Ladder", contentRoot);
            ladder.rectTransform.anchorMin = ladder.rectTransform.anchorMax = new Vector2(0.5f, 0.34f);
            ladder.rectTransform.sizeDelta = new Vector2(1000f, 330f);
            ladder.fontSize = 26f;
            ladder.alignment = TextAlignmentOptions.Center;
            ladder.color = new Color(0.88f, 0.93f, 1f, 0.98f);
            ladder.outlineWidth = 0.12f;
            ladder.outlineColor = new Color(0.05f, 0.04f, 0.08f, 0.9f);

            if (seriesDecided)
            {
                int rank = LeaderboardStore.Record(new SiegeRank.Entry
                {
                    score = gradeScore,
                    turns = turns,
                    victory = seriesWon,
                    grade = grade,
                    dateIso = DateTime.UtcNow.ToString("o"),
                });
                var board = LeaderboardStore.Load();

                var sb = new System.Text.StringBuilder("<b>SIEGE RANKING / 공성 랭킹</b>\n");
                for (int i = 0; i < board.entries.Count; i++)
                {
                    var e = board.entries[i];
                    string row = $"{i + 1,2}.  {e.grade}   {e.score,5} pts   {e.turns,2} turns   {(e.victory ? "VICTORY" : "DEFEAT")}";
                    sb.AppendLine(i == rank ? $"<color=#FFD75A><b>▶ {row} ◀</b></color>" : row);
                }
                if (rank < 0) sb.AppendLine("<color=#FF8866>(this run fell below the board / 순위권 밖)</color>");
                ladder.text = sb.ToString();
            }
            else
            {
                ladder.text = "<color=#9FB4CC>시리즈 진행 중 — 2선승을 확정지어야 랭킹에 기록됩니다.\n(Series in progress — ranked once 2 games are won)</color>";
            }
            if (seriesDecided && seriesWon)
            {
                TextMeshProUGUI bannerLabel = null;
                bool alreadyUnlocked = SiegePrototypeEconomy.HasBattleBannerSeal;
                bannerLabel = BuildButton("PrototypeBannerButton", new Vector2(0.5f, 0.155f),
                    alreadyUnlocked
                        ? "전공기 휘장 장착됨 · 로컬 전용"
                        : $"전공기 휘장 교환 · {SiegePrototypeEconomy.BattleBannerSealPrice} 인장 · 실제 결제 없음",
                    alreadyUnlocked ? new Color(0.45f, 0.72f, 0.9f, 0.92f) : new Color(0.92f, 0.8f, 0.3f, 0.96f),
                    () =>
                    {
                        if (!SiegePrototypeEconomy.TryUnlockBattleBannerSeal()) return;
                        bannerLabel.text = "전공기 휘장 장착됨 · 로컬 전용";
                        bannerLabel.GetComponentInParent<Button>().interactable = false;
                    });
                bannerLabel.GetComponentInParent<Image>().rectTransform.sizeDelta = new Vector2(520f, 56f);
                bannerLabel.GetComponentInParent<Button>().interactable = !alreadyUnlocked;
            }


            if (!seriesDecided)
            {
                // Mid-series: the only meaningful actions are continuing the SAME series to
                // its next game, or abandoning it back to the title (Rematch/NextStage don't
                // apply yet - the series hasn't been won or lost).
                int nextGameNumber = SiegeSeries.NextGameNumber(seriesGameNumber);
                BuildButton("NextGameButton", new Vector2(0.38f, 0.10f), $"다음 경기 ({nextGameNumber}/{SiegeSeries.MaxGames})",
                    new Color(0.35f, 0.92f, 0.55f, 1f), () => { navigated = true; GameManager.RequestNextGame(); });
                BuildButton("TitleButton", new Vector2(0.62f, 0.10f), "타이틀",
                    new Color(0.35f, 0.55f, 0.85f, 0.95f), () => { navigated = true; GameManager.RequestTitle(); });
                return;
            }

            // Sequential campaign: a fresh unlock earns a third, primary-styled action
            // slot. Symmetric 3-up layout (0.25/0.50/0.75) only when there's a next
            // stage to offer; otherwise the original 2-up centered layout (0.38/0.62)
            // is unchanged (defeats, and victories on the final stage/rematches of an
            // already-cleared stage never grow a third button).
            if (nextStage.HasValue)
            {
                BuildButton("RematchButton", new Vector2(0.25f, seriesWon ? 0.065f : 0.10f), "재도전 (R)",
                    new Color(0.95f, 0.62f, 0.18f, 0.98f), () => { navigated = true; GameManager.RequestRematch(); });
                nextStageLabel = BuildButton("NextStageButton", new Vector2(0.50f, seriesWon ? 0.065f : 0.10f),
                    $"다음 스테이지 ({Mathf.CeilToInt(AutoAdvanceDelay)})",
                    new Color(0.35f, 0.92f, 0.55f, 1f),
                    // Only latch `navigated` when the request was actually ACCEPTED. Latching
                    // first and hoping meant a refused request left the countdown stopped and
                    // the screen inert — the player could no longer advance at all.
                    () => { if (GameManager.RequestStage(nextStage.Value, skipIntro: true)) navigated = true; });
                BuildButton("TitleButton", new Vector2(0.75f, seriesWon ? 0.065f : 0.10f), "타이틀",
                    new Color(0.35f, 0.55f, 0.85f, 0.95f), () => { navigated = true; GameManager.RequestTitle(); });
            }
            else
            {
                BuildButton("RematchButton", new Vector2(0.38f, seriesWon ? 0.065f : 0.10f), "재도전 (R)",
                    new Color(0.95f, 0.62f, 0.18f, 0.98f), () => { navigated = true; GameManager.RequestRematch(); });
                BuildButton("TitleButton", new Vector2(0.62f, seriesWon ? 0.065f : 0.10f), "타이틀",
                    new Color(0.35f, 0.55f, 0.85f, 0.95f), () => { navigated = true; GameManager.RequestTitle(); });
            }
        }


        private TextMeshProUGUI BuildButton(string name, Vector2 anchor, string label, Color color, Action onClick)
        {
            var img = CreateChild<Image>(name, contentRoot);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = anchor;
            img.rectTransform.sizeDelta = new Vector2(360f, 92f);
            var card = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (card != null) { img.sprite = card; img.color = color; img.type = Image.Type.Sliced; }

            else img.color = color;

            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var text = CreateChild<TextMeshProUGUI>("Label", img.transform);
            Stretch(text.rectTransform);
            // Inset the label and auto-size it so the caption can never spill past the card:
            // fixed 30pt clipped "재도전"/"타이틀" to "전"/"이틀" on short/wide viewports.
            text.rectTransform.offsetMin = new Vector2(18f, 10f);
            text.rectTransform.offsetMax = new Vector2(-18f, -10f);
            text.text = label;
            text.enableWordWrapping = false;
            text.enableAutoSizing = true;
            text.fontSizeMin = 16f;
            text.fontSizeMax = 34f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = new Color(0.1f, 0.06f, 0.02f, 1f);
            return text;
        }

        private void Update()
        {
            float t = Time.unscaledTime - bornAt;
            if (rootGroup != null) rootGroup.alpha = Mathf.Clamp01(t / 0.45f);
            if (gradeRect != null)
            {
                // Grade seal stamps down with overshoot, then rests.
                float p = Mathf.Clamp01((t - 0.35f) / 0.4f);
                float eased = 1f + (1f - p) * (1f - p) * 2.2f;
                gradeRect.localScale = new Vector3(eased, eased, 1f);
            }

            // Auto-advance: counts the NEXT STAGE button down and self-triggers it
            // unless the player already navigated away (any button click, or R). The
            // flag guards the one-frame window between a click and the scene reload it
            // schedules, since RequestStage/RequestRematch/RequestTitle don't destroy
            // this object synchronously.
            if (pendingNextStage.HasValue && !navigated)
            {
                float remaining = AutoAdvanceDelay - t;
                if (nextStageLabel != null)
                    nextStageLabel.text = $"다음 스테이지 ({Mathf.Max(0, Mathf.CeilToInt(remaining))})";
                if (remaining <= 0f)
                {
                    // Latch only on acceptance. A refused request previously left `navigated`
                    // true forever, which froze the countdown AND disabled the button, so the
                    // player had no way to reach the next stage at all. On refusal the label
                    // says so plainly instead of counting down to nothing.
                    if (GameManager.RequestStage(pendingNextStage.Value, skipIntro: true))
                    {
                        navigated = true;
                    }
                    else
                    {
                        pendingNextStage = null;
                        if (nextStageLabel != null) nextStageLabel.text = "다음 스테이지 (잠김)";
                    }
                    return;
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                navigated = true;
                if (seriesDecidedAtBuild) GameManager.RequestRematch();
                else GameManager.RequestNextGame();
            }

        }

        private static T CreateChild<T>(string name, Transform parent) where T : Component
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
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
