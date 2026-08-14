using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CastleBusters.EditorTools
{
    /// <summary>
    /// V4 from `design/skill-grading-and-handicap.md`: what the handicap does to the win rate.
    ///
    /// Reports a SENSITIVITY TABLE, not a prediction. `SkillGrading` raises the AI's aim error by
    /// 0.35 world metres per grade and this model has no metres — only a 0..1 damage multiplier — so
    /// the honest form is conditional: if the handicap costs the AI this much aim quality, the win
    /// rate lands here. Converting metres to quality needs wall hitboxes, blast radii and block
    /// placement, none of which exist outside a scene.
    ///
    /// The conversion arrives from the runtime instead: `TelemetrySink.AiHits / AiShots` measured
    /// against `AiMeanAimError`. Until a real session supplies that, this table is the bracket the
    /// answer must fall inside.
    /// </summary>
    public static class HandicapMeasurement
    {
        [MenuItem("CastleBusters/QA/Measure handicap sensitivity")]
        public static void Run()
        {
            var settings = SiegeBalanceSettings.Default;
            var sb = new StringBuilder();
            sb.AppendLine("=== HANDICAP SENSITIVITY (V4) ===");
            sb.AppendLine($"settings: keep={settings.KeepDurability} shot={settings.baseShotDamage} " +
                          $"aim={settings.fixedAimQuality} err={settings.beginnerAimError}");
            sb.AppendLine($"matches per point: {SiegeDuelSimulation.RequiredMatches}, alternating turn order");
            sb.AppendLine($"G2 band: {SiegeDuelSimulation.G2LowerBound:P0}-{SiegeDuelSimulation.G2UpperBound:P0}");
            sb.AppendLine();
            sb.AppendLine("--- IF the handicap costs the AI this much aim quality ---");
            sb.AppendLine("  penalty | player win rate | inside band");

            foreach (float penalty in new[] { 0f, 0.01f, 0.02f, 0.03f, 0.05f, 0.08f, 0.12f })
            {
                float wr = SiegeDuelSimulation.WinRateWithEnemyPenalty(settings, seed: 4242, enemyQualityPenalty: penalty);
                bool inside = wr >= SiegeDuelSimulation.G2LowerBound && wr <= SiegeDuelSimulation.G2UpperBound;
                sb.AppendLine($"  {penalty,7:F2} | {wr,15:P1} | {(inside ? "yes" : "NO")}");
            }

            sb.AppendLine();
            sb.AppendLine("--- the grade ladder, in aim-error metres (NOT convertible to the above) ---");
            foreach (SkillGrading.Grade g in System.Enum.GetValues(typeof(SkillGrading.Grade)))
            {
                sb.AppendLine($"  {g,-11} handicap {SkillGrading.HandicapAimError(g):F2} " +
                              $"-> AI error {SkillGrading.Compose(1.65f, SkillGrading.HandicapAimError(g)):F2} " +
                              "(at the ramp midpoint 1.65)");
            }

            sb.AppendLine();
            sb.AppendLine("--- what the runtime must measure to close the gap ---");
            sb.AppendLine("  TelemetrySink.AiHits / AiShots against AiMeanAimError:");
            sb.AppendLine("  that pair converts metres of aim error into points of hit rate, which is");
            sb.AppendLine("  the same unit as the quality penalty above. Until then the handicap has a");
            sb.AppendLine("  rationale (20% of the ramp span per grade) and no predicted effect size.");

            string report = sb.ToString();
            Debug.Log(report);

            var dir = Path.Combine(Application.dataPath, "..", "_workspace", "current", "qa", "evidence", "g2");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "handicap-sensitivity.log"), report);
            Debug.Log($"[handicap] wrote {Path.Combine(dir, "handicap-sensitivity.log")}");
        }
    }
}
