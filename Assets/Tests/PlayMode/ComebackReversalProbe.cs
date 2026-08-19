using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// G5 FIX round two: run matches until comeback activations accumulate, then read the rate.
    ///
    /// Round one added the instrument (`Telemetry.Comeback`) and the gate review recorded that
    /// `ComebackReversalRate()` returns -1 because no match had recorded an activation. This is the
    /// measurement run, not more code — and it is a PROBE rather than a test: it asserts that it
    /// collected a sample and reports the number, because the threshold (≤30%) cannot be judged
    /// until the sample is large enough for a confidence interval that clears 30 percentage points,
    /// and that size depends on the observed rate.
    ///
    /// Deliberately does not fail on the rate. A probe that failed at 31% would be asserting a
    /// verdict the sample cannot support; this cycle already paid for that shape when a 10-point
    /// difference across three conditions turned out to have overlapping intervals at n=9.
    /// </summary>
    public sealed class ComebackReversalProbe
    {
        private const int Matches = 16;
        private const int TurnCap = 60;
        private const string EvidenceDir = "_workspace/current/qa/evidence/g5";

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        [UnityTest]
        [Timeout(2400000)]  // 40 min: 6 matches measured 538s, so 16 needs ~24
        public IEnumerator Probe_MeasureComebackReversalRate()
        {
            TelemetrySink.Enabled = true;
            Telemetry.Clear();

            var report = new StringBuilder();
            report.AppendLine("# G5 컴백 역전율 — 측정 실행");
            report.AppendLine();
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "- date: 2026-08-19  ·  경기 {0}회  ·  턴 상한 {1}", Matches, TurnCap));
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "- 캡 {0} / 코어 최대 150 / 위험 {1:P0}",
                LastStand.SingleHitDamageCap, LastStand.DangerHpFraction));
            report.AppendLine();
            report.AppendLine("| # | 턴 | 결착 | 누적 발동 (P/AI) | 누적 역전율 |");
            report.AppendLine("|---|---:|---|---|---|");

            for (int match = 1; match <= Matches; match++)
            {
                GameManager.PendingStage = StageId.Stage1;
                var load = SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);
                yield return load;
                yield return null;
                yield return null;

                // Re-armed every match: a scene load resets this static, so the [SetUp] assignment
                // only covers the first one. The MCP editor plugin logs an authorization failure on
                // its own schedule and an unhandled error log fails whichever test is running, which
                // killed two runs of this probe before the sample loop ever completed.
                LogAssert.ignoreFailingMessages = true;

                // The cold open would eat the match clock; skip it the way a player does.
                NarrativeVideoIntro.Active?.Skip();
                yield return null;

                var gm = GameManager.Instance;
                Assert.IsNotNull(gm, $"match {match}: the arena must have a GameManager");
                gm.BeginSiege();
                yield return null;

                var lm = Object.FindObjectOfType<LaunchManager>();
                Assert.IsNotNull(lm, $"match {match}: the arena must have a LaunchManager");

                int firedOn = -1;
                while (gm.TurnCount < TurnCap && gm.currentState != GameState.GameOver)
                {
                    if (gm.IsPlayerTurn && !gm.IsResolvingTurn && firedOn != gm.TurnCount)
                    {
                        firedOn = gm.TurnCount;

                        // Spend the comeback the moment it is available. The first two runs recorded
                        // 0 player activations across 22 matches because nothing here pressed the
                        // button — so half the population was invisible while the report implied a
                        // rate for both. `ActivatePlayerLastStand` is a no-op unless Armed, so this
                        // is safe to call every turn.
                        //
                        // This measures the EARLIEST spend, not a typical one: the design gives the
                        // player the choice of timing (`Advance` latches at Armed while the AI's
                        // `AdvanceAuto` fires on sight), and firing immediately makes the player
                        // behave like the AI. So the player figure this yields is a lower bound on
                        // how favourable a well-timed comeback can be, and it must not be read as
                        // "what players do".
                        gm.ActivatePlayerLastStand();

                        lm.SimulateLaunch(lm.GetSeparatedAimVelocity());

                        // Let the volley resolve rather than sleeping a guess: a fixed wait either
                        // wastes seconds or fires into an unresolved turn.
                        for (float t = 0f; t < 25f && gm.TurnCount == firedOn
                                           && gm.currentState != GameState.GameOver;
                             t += Time.unscaledDeltaTime)
                        {
                            yield return null;
                        }
                    }
                    yield return null;
                }

                var (p, a) = Telemetry.ComebackActivations();
                float rate = Telemetry.ComebackReversalRate();
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "| {0} | {1} | {2} | {3}/{4} | {5} |",
                    match, gm.TurnCount,
                    gm.currentState == GameState.GameOver ? "예" : "턴 상한",
                    p, a,
                    rate < 0f ? "n/a (발동 0)" : rate.ToString("P1", CultureInfo.InvariantCulture)));
            }

            var (players, ais) = Telemetry.ComebackActivations();
            int total = players + ais;
            float finalRate = Telemetry.ComebackReversalRate();

            report.AppendLine();
            report.AppendLine("## 결과");
            report.AppendLine();
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "- 총 발동 **{0}** (플레이어 {1} / AI {2})", total, players, ais));
            report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "- 역전율 **{0}**",
                finalRate < 0f ? "측정 불가 — 발동 0건" : finalRate.ToString("P1", CultureInfo.InvariantCulture)));
            report.AppendLine();

            if (total == 0)
            {
                report.AppendLine("**발동이 0건이다.** 스크립트 플레이어는 튜닝된 기본 조준으로만 쏘고");
                report.AppendLine("AI는 미러이므로, 어느 쪽 코어도 35% 아래로 내려가지 않는 경기가 나온다.");
                report.AppendLine("즉 이 프로브는 **컴백이 발동하는 상황을 만들지 못한다** — 코어를 인위적으로");
                report.AppendLine("깎지 않으면 표본이 쌓이지 않고, 깎으면 그것은 실제 경기의 분포가 아니다.");
                report.AppendLine();
                report.AppendLine("**남은 경로**: (가) 경기 수를 크게 늘린다, (나) 시뮬레이터에 LastStand를");
                report.AppendLine("모델한다, (다) 사람 플레이 세션에서 수집한다. (나)가 가장 싸고 (다)가 가장");
                report.AppendLine("정확하다 — 스크립트 플레이어의 분산은 인간의 것이 아니다(이 사이클 M2의 한계).");
            }
            else
            {
                report.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "표본 {0}건은 ≤30% 판정에 충분하지 않을 수 있다. 신뢰구간이 30%p를 가르는", total));
                report.AppendLine("크기는 관측 비율에 따라 달라지고, 그 계산은 이 값을 받아서 한다.");
            }

            Directory.CreateDirectory(EvidenceDir);
            File.WriteAllText(Path.Combine(EvidenceDir, "comeback-reversal-run.md"), report.ToString());
            Debug.Log($"[comeback] activations {total} (P{players}/AI{ais}) rate {finalRate:P1}");

            // The probe asserts it RAN, not what it found. A run that silently collected nothing and
            // reported a clean rate is the failure this must not permit.
            Assert.Pass($"activations={total} rate={(finalRate < 0f ? "n/a" : finalRate.ToString("P1"))}");
        }
    }
}
