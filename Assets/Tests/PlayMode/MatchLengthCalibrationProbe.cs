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
    /// Measures the two constants `MatchLengthModel` says are its weak point.
    ///
    /// The model states its own gap outright: "The two calibration constants (d and s) are the
    /// honest weak point: they come from watching play rather than from instrumentation. They are
    /// named and isolated here so that when telemetry does measure them, one edit re-derives the
    /// whole balance." `EffectiveDamagePerTurn = 37` has never been measured, and the balance audit
    /// refused to retune anything on top of an unverified input — that refusal is what makes this
    /// probe B1's first item rather than an optional extra.
    ///
    /// Three of B1's four items come from one run, because they are three views of the same match:
    ///   1. hit distribution — where shots actually land
    ///   2. d, effective damage landed per turn
    ///   3. real match length per stage
    ///
    /// <para><b>What this probe cannot measure.</b> The player here is a script firing a fixed
    /// aim, so `d` measured this way is the SIMULATION's damage per turn, not a human's. It
    /// excludes learning, hesitation and misaiming. `Telemetry.Volley` records power/angle/wind
    /// but no result-versus-intent field, so human `d` is not measurable at all today — the gap is
    /// instrumentation, not sequencing. Every number this probe prints must be read as
    /// "sim-side, excluding human learning effects".</para>
    ///
    /// Runs with `-nographics`: the MCP plugin's BufferedFileLogStorage hangs the domain reload
    /// otherwise, and this probe writes no images.
    /// </summary>
    public class MatchLengthCalibrationProbe
    {
        private const string EvidenceDir = "_workspace/current/qa/evidence/match-length";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private sealed class TurnRecord
        {
            public int Turn;
            public bool ByPlayer;
            public float EnemyMaterialBefore;
            public float EnemyMaterialAfter;
            public float PlayerMaterialBefore;
            public float PlayerMaterialAfter;
            public float Seconds;
            public string Readback;
        }

        /// <summary>
        /// Total remaining material on one side: wall block HP plus core HP.
        ///
        /// This is the model's M, measured rather than computed from `BlocksPerKeep`. Ground
        /// anchors are excluded for the reason the reachability probe found them: the ground tiling
        /// is parented under the castle, and counting it put a wall census at 143 where a keep is
        /// 15 blocks.
        /// </summary>
        private static float Material(CastleController castle)
        {
            if (castle == null) return 0f;
            float total = 0f;
            foreach (var b in castle.GetComponentsInChildren<DestructibleBlock>(true))
            {
                if (b == null || b.isGroundAnchor) continue;
                total += Mathf.Max(0f, b.currentHP);
            }
            return total;
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator Probe_MeasureDamagePerTurnAndMatchLength()
        {
            // Stage3 only, on re-measurement.
            //
            // The first B1 run measured all three, but Stage3's figures turned out to describe a
            // stage with no castle: a ground-atlas request was throwing out of Start and the keep,
            // the core and every later gimmick were never built (task #63). Its d=5.31 and 81%
            // zero-damage rate were reading an empty board. Stage1's 96.59 and Stage2's 128.33 stand
            // — those boards were intact — so only the invalidated stage is re-run, which also keeps
            // the session inside the reload-hang window.
            var stages = new[] { StageId.Stage3 };
            var report = new StringBuilder();
            report.AppendLine("# B1 실측 — d(턴당 유효 피해) · s(턴 소요) · 경기 길이");
            report.AppendLine();
            report.AppendLine("모두 **심 측정**이며 인간 학습 효과를 포함하지 않는다 (N-21′).");
            report.AppendLine();

            foreach (var stage in stages)
            {
                LogAssert.ignoreFailingMessages = true;
                GameManager.PendingStage = stage;
                SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                LogAssert.ignoreFailingMessages = true;

                var gm = GameManager.Instance;
                Assert.IsNotNull(gm, $"{stage}: GameManager missing");
                gm.BeginSiege();
                if (NarrativeVideoIntro.Active != null) NarrativeVideoIntro.Active.Skip();
                if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();
                foreach (var intro in Object.FindObjectsByType<IntroScreenController>(FindObjectsSortMode.None))
                    if (intro != null) intro.Dismiss();
                foreach (var p in Object.FindObjectsByType<WebtoonPrologueController>(FindObjectsSortMode.None))
                    if (p != null) p.Dismiss();
                Time.timeScale = 1f;
                yield return null;

                var lm = Object.FindFirstObjectByType<LaunchManager>();
                Assert.IsNotNull(lm, $"{stage}: LaunchManager missing");

                var records = new List<TurnRecord>();
                float matchStart = Time.unscaledTime;
                int lastTurn = -1;
                int firedThisTurn = -1;

                // The aim that B0 measured as landing on the keep: 45 degrees at 86% draw.
                const float aimDegrees = 45f;
                const float aimDraw = 0.86f;

                // Cap the run so a stalemate cannot hang the session; 60 turns is well past the
                // model's prediction of 39-43 for every stage.
                while (gm.TurnCount < 60 && gm.currentState != GameState.GameOver)
                {
                    if (gm.IsPlayerTurn && !gm.IsResolvingTurn && firedThisTurn != gm.TurnCount)
                    {
                        var rec = new TurnRecord
                        {
                            Turn = gm.TurnCount,
                            ByPlayer = true,
                            EnemyMaterialBefore = Material(gm.enemyCastle),
                            PlayerMaterialBefore = Material(gm.playerCastle),
                        };
                        float turnStart = Time.unscaledTime;

                        float speed = LaunchPowerCurve.SpeedForDraw(aimDraw, lm.maxLaunchVelocity);
                        float rad = aimDegrees * Mathf.Deg2Rad;
                        lm.SimulateLaunch(new Vector2(speed * Mathf.Cos(rad), speed * Mathf.Sin(rad)));
                        firedThisTurn = gm.TurnCount;

                        // Wait for the turn to resolve and flip.
                        int firedOn = gm.TurnCount;
                        for (float t = 0f; t < 25f && gm.TurnCount == firedOn
                                           && gm.currentState != GameState.GameOver; t += Time.unscaledDeltaTime)
                        {
                            yield return null;
                        }

                        rec.EnemyMaterialAfter = Material(gm.enemyCastle);
                        rec.PlayerMaterialAfter = Material(gm.playerCastle);
                        rec.Seconds = Time.unscaledTime - turnStart;
                        rec.Readback = ShotTraceDirector.LatestLineByPlayer
                            ? ShotTraceDirector.LatestLine : "";
                        records.Add(rec);
                    }
                    else
                    {
                        if (gm.TurnCount != lastTurn) lastTurn = gm.TurnCount;
                        yield return null;
                    }
                }

                float matchSeconds = Time.unscaledTime - matchStart;

                // --- Aggregate ---
                float damageDealt = 0f;
                int landed = 0;
                float turnSecondsTotal = 0f;
                foreach (var r in records)
                {
                    float dealt = Mathf.Max(0f, r.EnemyMaterialBefore - r.EnemyMaterialAfter);
                    damageDealt += dealt;
                    if (dealt > 0.01f) landed++;
                    turnSecondsTotal += r.Seconds;
                }

                float measuredD = records.Count > 0 ? damageDealt / records.Count : 0f;
                float measuredS = records.Count > 0 ? turnSecondsTotal / records.Count : 0f;
                float hitRate = records.Count > 0 ? (float)landed / records.Count : 0f;

                string outcome = gm.currentState == GameState.GameOver ? "decided" : "UNDECIDED at cap";

                Debug.Log($"[b1] {stage}: {outcome} after {gm.TurnCount} turns, "
                          + $"{records.Count} player shots, {matchSeconds:F1}s wall clock");
                Debug.Log($"[b1] {stage}: measured d={measuredD:F2} (model 37) "
                          + $"s={measuredS:F2}s (model 7.5) hitRate={hitRate * 100:F0}%");

                report.AppendLine($"## {stage}");
                report.AppendLine();
                report.AppendLine($"- 결과: **{outcome}**, {gm.TurnCount}턴, 플레이어 발사 {records.Count}회");
                report.AppendLine($"- **d 실측: {measuredD:F2}** (모델 37 → 비 {(measuredD > 0 ? 37f / measuredD : 0f):F2}배)");
                report.AppendLine($"- **s 실측: {measuredS:F2}s** (모델 7.5s)");
                report.AppendLine($"- 유효타율: **{hitRate * 100:F0}%** ({landed}/{records.Count})");
                report.AppendLine($"- 누적 적 재료 감소: {damageDealt:F0}");
                report.AppendLine();
                report.AppendLine("| 턴 | 적 재료 전 | 후 | 피해 | 초 | 리드백 |");
                report.AppendLine("|---:|---:|---:|---:|---:|---|");
                foreach (var r in records)
                {
                    float dealt = Mathf.Max(0f, r.EnemyMaterialBefore - r.EnemyMaterialAfter);
                    report.AppendLine($"| {r.Turn} | {r.EnemyMaterialBefore:F0} | {r.EnemyMaterialAfter:F0} "
                                      + $"| {dealt:F0} | {r.Seconds:F1} | {r.Readback} |");
                }
                report.AppendLine();
            }

            var path = Path.Combine(EvidenceDir, "b1-stage3-remeasured.md");
            File.WriteAllText(path, report.ToString());
            Debug.Log($"[b1] wrote {path}");

            Assert.Pass("B1 measurement recorded");
        }
    }
}
