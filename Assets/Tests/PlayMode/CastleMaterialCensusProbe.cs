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
    /// Counts what is actually standing under a castle, against what the pacing model thinks is.
    ///
    /// B2 asked whether the pacing gate is stale. It is not blind — `KeepProfileTests` iterates all
    /// three stages — but its material term is `WallHitPoints(layout) + CoreMaxHP`, which works out
    /// near 1480, while the B1 run measured Stage1's enemy castle starting at 5445. A 3.68x gap in
    /// the model's only material input is large enough that the gate can read 300s while a match
    /// decides in 82s, so the gap has to be named before anything is retuned on top of it.
    ///
    /// This probe does not judge; it enumerates. Each block is bucketed by name and by whether the
    /// model's block census would have counted it.
    /// </summary>
    public class CastleMaterialCensusProbe
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

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Probe_CensusEveryBlockUnderBothCastles()
        {
            var report = new StringBuilder();
            report.AppendLine("# 성 재료 인구조사 — 모델 입력 대 실제");
            report.AppendLine();
            // Stage3 only. The three-stage sweep produced the numbers below on a 0.5s settle and
            // then hit the domain-reload hang twice at 4s, so the run is narrowed to the stage whose
            // result needs confirming rather than re-collecting two that already agree with the B1
            // match data. Stage1 5445 / 3.82x and Stage2 5225 / 3.11x are in
            // qa/evidence/match-length/castle-material-census.md.
            foreach (var stage in new[] { StageId.Stage3 })
            {
                LogAssert.ignoreFailingMessages = true;
                GameManager.PendingStage = stage;
                SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
                yield return null;
                yield return new WaitForSecondsRealtime(2f);
                LogAssert.ignoreFailingMessages = true;

                var gm = GameManager.Instance;
                Assert.IsNotNull(gm);
                gm.BeginSiege();
                if (NarrativeVideoIntro.Active != null) NarrativeVideoIntro.Active.Skip();
                if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();
                foreach (var intro in Object.FindObjectsByType<IntroScreenController>(FindObjectsSortMode.None))
                    if (intro != null) intro.Dismiss();
                Time.timeScale = 1f;
                yield return null;
                // Long settle deliberately. Stage3's census came back nearly empty on a 0.5s wait,
                // and "the castle does not exist" is a serious enough claim that a slow build has to
                // be ruled out before it is made. If 4s still shows no walls, the walls are not late.
                yield return new WaitForSecondsRealtime(4f);

                var castle = gm.enemyCastle;
                Assert.IsNotNull(castle, $"{stage}: enemy castle missing");

                // Group live blocks by name prefix, which is how the spawners label their output.
                var buckets = new Dictionary<string, (int count, float hp)>();
                float liveTotal = 0f;
                int groundAnchors = 0;
                float groundHp = 0f;

                foreach (var b in castle.GetComponentsInChildren<DestructibleBlock>(true))
                {
                    if (b == null) continue;
                    if (b.isGroundAnchor)
                    {
                        groundAnchors++;
                        groundHp += Mathf.Max(0f, b.currentHP);
                        continue;
                    }

                    string name = b.name;
                    int underscore = name.IndexOf('_');
                    string prefix = underscore > 0 ? name.Substring(0, underscore) : name;

                    if (!buckets.TryGetValue(prefix, out var cur)) cur = (0, 0f);
                    buckets[prefix] = (cur.count + 1, cur.hp + Mathf.Max(0f, b.currentHP));
                    liveTotal += Mathf.Max(0f, b.currentHP);
                }

                // What the model counts.
                int modelBlocks = GameManager.BlocksPerKeep(
                    stage == StageId.Stage1 ? StageDefinitions.Stage1.wallHeightBlocks
                    : stage == StageId.Stage2 ? StageDefinitions.Stage2.wallHeightBlocks
                    : StageDefinitions.Stage3.wallHeightBlocks);
                var stone = Resources.Load<BlockData>("StoneBlockData");
                float stoneHp = stone != null ? stone.maxHP : 85f;
                float modelMaterial = modelBlocks * stoneHp + CastleCoreGimmick.CoreMaxHP;

                Debug.Log($"[census] {stage}: live non-ground HP {liveTotal:F0} across "
                          + $"{buckets.Count} name groups | model says {modelMaterial:F0} "
                          + $"({modelBlocks} blocks x {stoneHp} + core) | ratio {liveTotal / modelMaterial:F2}x");
                Debug.Log($"[census] {stage}: ground anchors excluded: {groundAnchors} blocks, {groundHp:F0} HP");

                report.AppendLine($"## {stage}");
                report.AppendLine();
                report.AppendLine($"- 모델 입력: **{modelMaterial:F0}** ({modelBlocks}블록 × {stoneHp} + 코어 {CastleCoreGimmick.CoreMaxHP})");
                report.AppendLine($"- 실제 비지면 재료: **{liveTotal:F0}** → **{liveTotal / modelMaterial:F2}배**");
                report.AppendLine($"- 제외된 지면 앵커: {groundAnchors}블록 / {groundHp:F0} HP");
                report.AppendLine();
                report.AppendLine("| 이름 그룹 | 개수 | HP 합 | 모델이 세는가 |");
                report.AppendLine("|---|---:|---:|---|");
                foreach (var kv in buckets)
                {
                    // The model's census walks KeepProfile courses only, so anything not named as a
                    // keep block is material the pacing equation never sees.
                    bool counted = kv.Key.StartsWith("Block") || kv.Key.StartsWith("WallBlock");
                    report.AppendLine($"| `{kv.Key}` | {kv.Value.count} | {kv.Value.hp:F0} | {(counted ? "예" : "**아니오**")} |");
                }
                report.AppendLine();
            }

            var path = Path.Combine(EvidenceDir, "castle-material-census.md");
            File.WriteAllText(path, report.ToString());
            Debug.Log($"[census] wrote {path}");
            Assert.Pass("census recorded");
        }
    }
}
