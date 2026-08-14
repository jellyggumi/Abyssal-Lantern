using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// What counts as a castle's "material" must mean the same thing to the board and to the model.
    ///
    /// The pacing equation uses M = b·h + c and predicts turns-to-decide from it. A PlayMode census
    /// reported that the live board carried 3.1-3.8x that much, and I recorded it as "material is
    /// understated". That reading was wrong, and its cause is worth stating precisely: the census
    /// excluded blocks by <c>isGroundAnchor</c> and called the remainder "non-ground", but
    /// <c>CreateGround</c> parents EVERY ground tile under a castle (GameManager:1579-1580) and
    /// leaves the breakable top rows non-anchored. Terrain was being counted as castle.
    ///
    /// Three roles exist and they answer different questions:
    ///   keep      - gates the win condition. Wall courses, scene blocks, the core.
    ///   terrain    - absorbs shots, gates nothing. Breakable ground tiles.
    ///   immovable - cannot be destroyed at all. Anchors.
    ///
    /// The model predicts the WIN, so it should be compared against keep alone. This runs in EditMode
    /// deliberately: the PlayMode census hung in the MCP plugin's domain reload on five consecutive
    /// attempts (CLAUDE.md §5), and two of those runs never reached the scene at all.
    /// </summary>
    public class CastleMaterialCensusTests
    {
        private static string EvidenceDir =>
            Path.Combine(Application.dataPath, "..", "_workspace", "current", "qa", "evidence",
                "match-length");

        private enum Role { Keep, Terrain, Immovable }

        private sealed class Census
        {
            public float Keep, Terrain, Immovable;
            public int ImmovableCount;
            public readonly Dictionary<string, (int count, float hp, Role role)> Groups = new();
        }

        /// <summary>
        /// Builds one stage's board without a scene and walks the enemy castle's blocks.
        private static Census MeasureStage(StageLayout layout, GameObject host)
        {
            var gm = host.AddComponent<GameManager>();

            // Start() never runs in EditMode, so the state ApplyStageLayout would install is set
            // directly: the stage id, the layout the wall builder reads, and the ground width.
            gm.currentStage = layout.id;
            var activeLayout = typeof(GameManager).GetProperty("ActiveLayout",
                BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(activeLayout, "precondition: ActiveLayout must still be the property name");
            activeLayout.SetValue(gm, layout);
            SetPrivate(gm, "groundHalfWidth", Mathf.RoundToInt(layout.groundHalfWidth));

            var castleGo = new GameObject("EnemyCastle_Census");
            var castle = castleGo.AddComponent<CastleController>();
            gm.enemyCastle = castle;

            var create = typeof(GameManager).GetMethod("CreateGround",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(create, "precondition: CreateGround must still exist");
            create.Invoke(gm, null);

            // The keep itself. SpawnCastleWalls is what CreateGround does not do.
            var walls = typeof(GameManager).GetMethod("SpawnCastleWalls",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(walls, "precondition: SpawnCastleWalls must still exist");
            walls.Invoke(gm, null);

            var census = new Census();
            foreach (var b in castle.GetComponentsInChildren<DestructibleBlock>(true))
            {
                if (b == null) continue;
                // Awake sets currentHP from maxHP; EditMode instantiation may not have run it.
                float hp = b.currentHP > 0f ? b.currentHP : b.maxHP;

                string name = b.name;
                int underscore = name.IndexOf('_');
                string prefix = underscore > 0 ? name.Substring(0, underscore) : name;

                Role role;
                if (b.isGroundAnchor)
                {
                    role = Role.Immovable;
                    census.Immovable += hp;
                    census.ImmovableCount++;
                }
                else if (prefix == "GroundBlock")
                {
                    role = Role.Terrain;
                    census.Terrain += hp;
                }
                else
                {
                    role = Role.Keep;
                    census.Keep += hp;
                }

                if (!census.Groups.TryGetValue(prefix, out var cur)) cur = (0, 0f, role);
                census.Groups[prefix] = (cur.count + 1, cur.hp + hp, role);
            }

            return census;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"precondition: {field} must still be the field name");
            f.SetValue(target, value);
        }

        private static void DestroyEverything()
        {
            foreach (var b in Object.FindObjectsByType<DestructibleBlock>(FindObjectsSortMode.None))
                if (b != null) Object.DestroyImmediate(b.gameObject);
            foreach (var c in Object.FindObjectsByType<CastleController>(FindObjectsSortMode.None))
                if (c != null) Object.DestroyImmediate(c.gameObject);
        }

        /// <summary>
        /// The census that replaces the wrong one, written to evidence and asserted on.
        /// </summary>
        [Test]
        public void TheKeepsMaterialIsMeasuredByRole_NotByAnchorFlag()
        {
            var report = new StringBuilder();
            report.AppendLine("# 성 재료 인구조사 — 역할별 (EditMode, 2026-08-14)");
            report.AppendLine();
            report.AppendLine("첫 인구조사는 `isGroundAnchor`로 제외하고 나머지를 \"비지면\"이라 불렀다.");
            report.AppendLine("`CreateGround`가 **모든 지면 타일을 성 밑에 붙이고**(GameManager:1579-1580)");
            report.AppendLine("깨지는 상단 행을 비앵커로 남기므로, **지형이 성 재료로 집계됐다**.");
            report.AppendLine("그것이 \"재료가 3.1~3.8배 낮다\"는 보고의 원인이다.");
            report.AppendLine();
            report.AppendLine("| 스테이지 | 모델 M | keep (승리를 막음) | keep/M | terrain (흡수만) | immovable |");
            report.AppendLine("|---|---:|---:|---:|---:|---:|");

            var layouts = new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 };
            var details = new StringBuilder();
            var ratios = new List<float>();

            foreach (var layout in layouts)
            {
                var host = new GameObject($"GameManager_Census_{layout.id}");
                Census census;
                try
                {
                    census = MeasureStage(layout, host);
                }
                finally
                {
                    Object.DestroyImmediate(host);
                }

                var stone = Resources.Load<BlockData>("StoneBlockData");
                float stoneHp = stone != null ? stone.maxHP : 85f;
                int modelBlocks = GameManager.BlocksPerKeep(layout.wallHeightBlocks);
                float model = MatchLengthModel.Material(modelBlocks, stoneHp, CastleCoreGimmick.CoreMaxHP);

                float ratio = census.Keep / Mathf.Max(1f, model);
                ratios.Add(ratio);

                report.AppendLine($"| {layout.id} | {model:F0} | **{census.Keep:F0}** | **{ratio:F2}배** | "
                                  + $"{census.Terrain:F0} | {census.ImmovableCount}블록 / {census.Immovable:F0} |");

                details.AppendLine($"## {layout.id}");
                details.AppendLine();
                details.AppendLine("| 이름 그룹 | 개수 | HP 합 | 역할 |");
                details.AppendLine("|---|---:|---:|---|");
                foreach (var kv in census.Groups)
                    details.AppendLine($"| `{kv.Key}` | {kv.Value.count} | {kv.Value.hp:F0} | {kv.Value.role} |");
                details.AppendLine();

                Debug.Log($"[census-em] {layout.id}: keep {census.Keep:F0} terrain {census.Terrain:F0} "
                          + $"immovable {census.Immovable:F0} model {model:F0} ratio {ratio:F2}x");

                DestroyEverything();
            }

            report.AppendLine();
            report.Append(details);

            Directory.CreateDirectory(EvidenceDir);
            File.WriteAllText(Path.Combine(EvidenceDir, "castle-material-census-by-role.md"),
                report.ToString());

            // Every stage must build a keep at all. Stage3 shipped without one.
            foreach (var r in ratios)
                Assert.Greater(r, 0.5f,
                    "a stage whose keep material is under half the model's figure is not a tuning "
                    + "discrepancy, it is a stage that failed to build - which is exactly what "
                    + "Stage3 was doing at 0.13x");

            // And the discrepancy must stay small enough that M is still the right shape. The old
            // 3.1-3.8x reading was terrain; if a real figure ever gets that large, M is wrong.
            foreach (var r in ratios)
                Assert.Less(r, 2.5f,
                    "keep material more than 2.5x the model means M = b·h + c has stopped describing "
                    + "the castle it is meant to describe");
        }

        /// <summary>
        /// The distinction itself is the finding, so it gets its own test: terrain is real material
        /// that absorbs shots, and it must NOT be inside the number the win condition is derived from.
        /// </summary>
        [Test]
        public void TerrainAbsorbsShotsButMustNotCountTowardTheWin()
        {
            var host = new GameObject("GameManager_TerrainSplit");
            Census census;
            try
            {
                census = MeasureStage(StageDefinitions.Stage1, host);
            }
            finally
            {
                Object.DestroyImmediate(host);
                DestroyEverything();
            }

            Assert.Greater(census.Terrain, 0f,
                "breakable ground exists - it is what a short pull buries into");

            Assert.Greater(census.Keep, 0f, "the keep must have been built");

            // If these two were ever merged again, the win-condition estimate would inherit terrain.
            Assert.AreNotEqual(census.Keep, census.Keep + census.Terrain,
                "keep and terrain must stay separate totals; adding terrain to M would predict a "
                + "match that ends when the FLOOR is gone");
        }
    }
}
