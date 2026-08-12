using System.Text;
using UnityEditor;
using UnityEngine;

namespace CastleBusters.EditorTools
{
    /// <summary>
    /// Prints the G2 win-rate measurement so a gate verdict can cite a number instead of a
    /// passing test. The duel tests assert bands; this reports the values inside them.
    ///
    /// Batch:
    ///   Unity -batchmode -quit -projectPath &lt;abs&gt; \
    ///     -executeMethod CastleBusters.EditorTools.G2Measurement.Run -logFile &lt;abs&gt;
    ///
    /// Output goes to `qa/gate-measurements.md#g2`. Read the two win rates together: the shipped
    /// game always gives the player the first shot, so the "player always first" row is what a
    /// player experiences, and the alternating row is what the balance itself does. A verdict
    /// that quotes only one of them is answering a different question than the one asked.
    /// </summary>
    public static class G2Measurement
    {
        [MenuItem("CastleBusters/QA/Measure G2 win rate")]
        public static void Run()
        {
            var settings = SiegeBalanceSettings.Default;
            var sb = new StringBuilder();
            sb.AppendLine("=== G2 MEASUREMENT ===");
            sb.AppendLine($"settings: keep={settings.KeepDurability} shot={settings.baseShotDamage} " +
                          $"aim={settings.fixedAimQuality} err={settings.beginnerAimError} s/turn={settings.secondsPerTurn}");
            sb.AppendLine($"matches per series: {SiegeDuelSimulation.RequiredMatches}");
            sb.AppendLine($"G2 band: {SiegeDuelSimulation.G2LowerBound:P0}-{SiegeDuelSimulation.G2UpperBound:P0}");
            sb.AppendLine();

            Report(sb, "player always first (shipped turn order)", settings, alternate: false);
            Report(sb, "alternating first move (balance isolated)", settings, alternate: true);

            sb.AppendLine();
            sb.AppendLine("--- skill sensitivity (alternating, so turn order is neutral) ---");
            foreach (float delta in new[] { 0f, 0.01f, 0.03f, 0.05f, 0.10f })
            {
                float wr = SiegeDuelSimulation.WinRateWithSkillDelta(settings, seed: 4242, skillDelta: delta);
                sb.AppendLine($"  aim +{delta:F2} -> player win rate {wr:P1}");
            }

            sb.AppendLine();
            sb.AppendLine("--- seed stability (alternating, 4 seeds) ---");
            foreach (int seed in new[] { 1, 1000, 20000, 999983 })
            {
                var results = SiegeDuelSimulation.RunSeries(
                    settings, seed, SiegeDuelSimulation.RequiredMatches, alternateFirstMove: true);
                var s = SiegeDuelSimulation.Summarize(results, settings.secondsPerTurn);
                sb.AppendLine($"  seed {seed,7}: win {s.PlayerWinRate:P1}  turns {s.averageTurns:F1}  {s.averageSeconds:F0}s");
            }

            Debug.Log(sb.ToString());
        }

        private static void Report(StringBuilder sb, string label, SiegeBalanceSettings settings, bool alternate)
        {
            var results = SiegeDuelSimulation.RunSeries(
                settings, seed: 4242, matches: SiegeDuelSimulation.RequiredMatches, alternateFirstMove: alternate);
            var s = SiegeDuelSimulation.Summarize(results, settings.secondsPerTurn);

            sb.AppendLine($"--- {label} ---");
            sb.AppendLine($"  matches        : {s.matches}");
            sb.AppendLine($"  player win rate: {s.PlayerWinRate:P1}  ({s.playerWins}/{s.matches})");
            sb.AppendLine($"  first-mover    : {s.firstMoverWinRate:P1}");
            sb.AppendLine($"  average turns  : {s.averageTurns:F1}");
            sb.AppendLine($"  average length : {s.averageSeconds:F0}s (model target {MatchLengthModel.TargetMatchSeconds:F0}s)");
            sb.AppendLine($"  G2 band        : {(s.InsideG2Band ? "INSIDE" : "OUTSIDE")}");
        }
    }
}
