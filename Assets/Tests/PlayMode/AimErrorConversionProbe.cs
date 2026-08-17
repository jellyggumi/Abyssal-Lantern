using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// M2: how many points of hit rate does a metre of aim error cost?
    ///
    /// This is the conversion the balance model cannot derive and the survey refused to invent.
    /// `SimpleAI.errorOffsetRange` displaces the aim point by up to that many world units on each
    /// axis (`SimpleAI.cs:53`); `SiegeBalanceSettings.fixedAimQuality` is a 0..1 damage multiplier.
    /// Nothing in code relates them, and deriving the relation needs wall hitboxes, blast radii and
    /// block placement — physics that exists only in a live scene. So it is measured in one.
    ///
    /// Method: pin the AI's aim error to a fixed value for a whole match and count how often its
    /// shots remove material from the PLAYER's keep. Pinning is done by collapsing the difficulty
    /// ramp (`aiErrorStart = aiErrorEnd = x`) so `CurrentAiErrorOffset` returns x at every turn
    /// regardless of turn count — otherwise the ramp would sweep the very variable under test.
    ///
    /// The offsets bracket what the shipped game actually produces: 0.8 is the ramp's tight end,
    /// 1.65 its midpoint, 2.5 its loose end, and 3.2 is 2.5 plus the widest handicap this project
    /// hands out. If hit rate does not fall monotonically across that range, the handicap is not
    /// doing what its name says and the grade ladder rests on nothing.
    ///
    /// <para><b>What this cannot measure.</b> The player here fires a fixed aim, so the player-side
    /// figures are sim-side and exclude human learning (N-21'). The AI-side figures are the point of
    /// the probe and are real: the AI's own aim model is what ships. Sample is one match per
    /// condition and the AI fires once per round, so roughly half the turn cap — thin, and stated
    /// as thin rather than dressed up.</para>
    ///
    /// Runs with `-nographics`: the MCP plugin's BufferedFileLogStorage hangs the domain reload
    /// otherwise (CLAUDE.md §5), and this probe writes no images.
    /// </summary>
    public class AimErrorConversionProbe
    {
        private const string EvidenceDir = "_workspace/current/qa/evidence/g2";

        /// <summary>Turns per condition. The AI fires once per round (two turns), so this yields
        /// about half as many AI shots. 40 keeps four conditions inside one session's reload
        /// window, which matters more than a larger sample that never completes.</summary>
        private const int TurnCap = 40;

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private sealed class Condition
        {
            public float AimError;
            public int AiShots;
            public int AiHits;
            public int PlayerShots;
            public int PlayerHits;
            public float PlayerMeanMaterial;
            public float PlayerCv;
            public int Turns;
            public bool Decided;
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator Probe_MeasureHitRateAgainstAimError()
        {
            // Ramp endpoints, plus the loose end raised by the widest handicap the game gives.
            var offsets = new[] { 0.8f, 1.65f, 2.5f, 3.2f };
            var results = new List<Condition>();

            foreach (float offset in offsets)
            {
                LogAssert.ignoreFailingMessages = true;
                GameManager.PendingStage = StageId.Stage1;
                SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                LogAssert.ignoreFailingMessages = true;

                var gm = GameManager.Instance;
                Assert.IsNotNull(gm, $"offset {offset}: GameManager missing");

                // Collapse the ramp so the variable under test stays put. Lerp(x, x, t) == x for
                // every t, so CurrentAiErrorOffset is constant without touching DifficultyCurve.
                gm.aiErrorStart = offset;
                gm.aiErrorEnd = offset;

                // Fresh counters, and a fresh handicap freeze. With no accumulated sample the grade
                // defaults to Elite, so MatchHandicap.Current is 0 and the offset above is the whole
                // aim error — which is the point: one variable, measured alone.
                TelemetrySink.Enabled = true;
                TelemetrySink.BeginSession();
                MatchHandicap.Clear();

                gm.BeginSiege();
                if (NarrativeVideoIntro.Active != null) NarrativeVideoIntro.Active.Skip();
                if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();
                foreach (var intro in Object.FindObjectsByType<IntroScreenController>(FindObjectsSortMode.None))
                    if (intro != null) intro.Dismiss();
                foreach (var p in Object.FindObjectsByType<WebtoonPrologueController>(FindObjectsSortMode.None))
                    if (p != null) p.Dismiss();
                Time.timeScale = 1f;
                yield return null;

                // BeginSiege re-froze the handicap from whatever the session held; clear it again so
                // the pinned offset is not silently widened.
                MatchHandicap.Clear();

                var lm = Object.FindFirstObjectByType<LaunchManager>();
                Assert.IsNotNull(lm, $"offset {offset}: LaunchManager missing");

                // The aim B0 measured as reaching the keep: 45 degrees at 86% draw. Fixed, because
                // the player is a constant here and the AI is the variable.
                const float aimDegrees = 45f;
                const float aimDraw = 0.86f;
                int firedThisTurn = -1;

                while (gm.TurnCount < TurnCap && gm.currentState != GameState.GameOver)
                {
                    if (gm.IsPlayerTurn && !gm.IsResolvingTurn && firedThisTurn != gm.TurnCount)
                    {
                        float speed = LaunchPowerCurve.SpeedForDraw(aimDraw, lm.maxLaunchVelocity);
                        float rad = aimDegrees * Mathf.Deg2Rad;
                        lm.SimulateLaunch(new Vector2(speed * Mathf.Cos(rad), speed * Mathf.Sin(rad)));
                        firedThisTurn = gm.TurnCount;

                        int firedOn = gm.TurnCount;
                        for (float t = 0f; t < 25f && gm.TurnCount == firedOn
                                           && gm.currentState != GameState.GameOver; t += Time.unscaledDeltaTime)
                        {
                            yield return null;
                        }
                    }
                    else
                    {
                        yield return null;
                    }
                }

                results.Add(new Condition
                {
                    AimError = offset,
                    AiShots = TelemetrySink.AiShots,
                    AiHits = TelemetrySink.AiHits,
                    PlayerShots = TelemetrySink.PlayerShots,
                    PlayerHits = TelemetrySink.PlayerHits,
                    PlayerMeanMaterial = TelemetrySink.PlayerMeanShotMaterial,
                    PlayerCv = TelemetrySink.PlayerShotMaterialCv,
                    Turns = gm.TurnCount,
                    Decided = gm.currentState == GameState.GameOver,
                });

                Debug.Log($"[conv] offset {offset:F2}: aiShots {TelemetrySink.AiShots} " +
                          $"aiHits {TelemetrySink.AiHits} " +
                          $"playerShots {TelemetrySink.PlayerShots} playerHits {TelemetrySink.PlayerHits} " +
                          $"playerCv {TelemetrySink.PlayerShotMaterialCv:F2} turns {gm.TurnCount}");
            }

            var report = new StringBuilder();
            report.AppendLine("# 미터 → 명중률 환산 실측 (M2)");
            report.AppendLine();
            report.AppendLine("`SimpleAI.errorOffsetRange`(월드 미터)를 고정하고 AI 명중률을 셌다.");
            report.AppendLine("난이도 램프를 `aiErrorStart = aiErrorEnd`로 접어 턴에 따라 움직이지 않게 했다.");
            report.AppendLine($"스테이지 Stage1, 조건당 최대 {TurnCap}턴, AI는 라운드당 1발.");
            report.AppendLine();
            report.AppendLine("| AI 조준 오차 | AI 발사 | AI 명중 | AI 명중률 | 턴 | 결착 |");
            report.AppendLine("|---:|---:|---:|---:|---:|---|");
            foreach (var c in results)
            {
                string rate = c.AiShots > 0 ? $"{(float)c.AiHits / c.AiShots * 100f:F1}%" : "n/a";
                report.AppendLine($"| {c.AimError:F2} | {c.AiShots} | {c.AiHits} | **{rate}** | " +
                                  $"{c.Turns} | {(c.Decided ? "예" : "상한")} |");
            }
            report.AppendLine();
            report.AppendLine("## 플레이어 쪽 (교차 확인용, 심 측정)");
            report.AppendLine();
            report.AppendLine("| AI 조준 오차 | 플레이어 발사 | 명중 | 명중률 | 평균 샷재료 | **CV** |");
            report.AppendLine("|---:|---:|---:|---:|---:|---:|");
            foreach (var c in results)
            {
                string rate = c.PlayerShots > 0 ? $"{(float)c.PlayerHits / c.PlayerShots * 100f:F1}%" : "n/a";
                string cv = c.PlayerCv > 0f ? $"{c.PlayerCv:F2}" : "n/a";
                report.AppendLine($"| {c.AimError:F2} | {c.PlayerShots} | {c.PlayerHits} | {rate} | " +
                                  $"{c.PlayerMeanMaterial:F1} | **{cv}** |");
            }
            report.AppendLine();
            report.AppendLine("플레이어 조준은 45°·당김 86% 고정이므로 이 열은 **AI 오차와 무관해야** 한다.");
            report.AppendLine("움직인다면 AI의 명중이 플레이어의 표적 상태를 바꾼 것이다(성벽이 먼저 무너지면");
            report.AppendLine("남은 표적이 달라진다) — 그 결합은 이 프로브가 분리하지 못한다.");
            report.AppendLine();
            report.AppendLine("**CV 열이 M1이다**: `qa/b1-measurement-findings.md`의 Stage1 실측 1.50과");
            report.AppendLine("대조한다. 같은 크기면 계측이 독립 확인되고, 0.1대면 b1이 틀렸거나 계측이 틀렸다.");

            File.WriteAllText(Path.Combine(EvidenceDir, "aim-error-conversion.md"), report.ToString());
            Debug.Log($"[conv] wrote {Path.Combine(EvidenceDir, "aim-error-conversion.md")}");

            // The one thing that must hold, or the handicap is not a handicap: more aim error must
            // not IMPROVE the AI's hit rate. Asserted across the extremes rather than pairwise,
            // because a one-match sample per condition cannot support a monotonicity claim.
            var tightest = results[0];
            var widest = results[results.Count - 1];
            if (tightest.AiShots > 0 && widest.AiShots > 0)
            {
                float tightRate = (float)tightest.AiHits / tightest.AiShots;
                float wideRate = (float)widest.AiHits / widest.AiShots;
                Assert.LessOrEqual(wideRate, tightRate + 0.25f,
                    $"aim error {widest.AimError:F2} produced a {wideRate:P0} hit rate against "
                    + $"{tightRate:P0} at {tightest.AimError:F2}. If widening the AI's aim error "
                    + "raises its hit rate, the handicap makes the AI BETTER and the grade ladder is "
                    + "inverted. The 0.25 tolerance is sampling slack, not agreement.");
            }

            Assert.Pass("conversion recorded");
        }
    }
}
