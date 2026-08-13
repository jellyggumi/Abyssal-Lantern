using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Forbids the pattern that broke the HUD, at the place it is written.
    ///
    /// Six call sites found their canvas with <c>FindObjectOfType&lt;Canvas&gt;()</c>, whose
    /// order is not defined; the core badges landed on the cold open's canvas and rendered
    /// 17pt at 6.5px. Every one was routed through <c>HudCanvas</c> — and then a merge brought
    /// a seventh in from another session, because nothing stopped it being written.
    ///
    /// A runtime test cannot reliably catch this. A stray that lands on another system's
    /// canvas is indistinguishable, by structure alone, from that canvas's own content: the
    /// scene-graph rule that excludes the cold open's video frame also excludes a HUD element
    /// that landed beside it. Reading the source has no such blind spot — the call either
    /// appears or it does not.
    ///
    /// EditMode, so it costs seconds and runs on every gate.
    /// </summary>
    public class HudCanvasSourceGuardTests
    {
        private static readonly Regex CanvasLookup =
            new Regex(@"Find(First)?Objects?(ByType|OfType)\s*<\s*Canvas\s*>", RegexOptions.Compiled);

        /// <summary>The one file allowed to look for a canvas — it is the thing resolving one.</summary>
        private const string Resolver = "HudCanvas.cs";

        [Test]
        public void NoScript_FindsItsCanvasByIterationOrder()
        {
            var scripts = Path.Combine(Application.dataPath, "Scripts");
            Assert.IsTrue(Directory.Exists(scripts), $"Expected the script folder at {scripts}");

            var offenders = new List<string>();
            foreach (var file in Directory.GetFiles(scripts, "*.cs", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(file) == Resolver) continue;

                var lines = File.ReadAllLines(file);
                for (var i = 0; i < lines.Length; i++)
                {
                    // Prose about the old pattern is how the fix documents itself.
                    var trimmed = lines[i].TrimStart();
                    if (trimmed.StartsWith("//") || trimmed.StartsWith("*")) continue;
                    if (!CanvasLookup.IsMatch(lines[i])) continue;
                    offenders.Add($"{Path.GetFileName(file)}:{i + 1}");
                }
            }

            Assert.IsEmpty(offenders,
                "A canvas found by iteration order is the defect that made HUD text render at "
                + "6.5px. Use HudCanvas.Resolve() instead. Offenders: "
                + string.Join(", ", offenders));
        }

        /// <summary>
        /// The guard is worth nothing unless it would actually fire, so this checks its pattern
        /// against the exact calls it forbids. Without it the test above passes just as happily
        /// with a regex that matches nothing at all.
        /// </summary>
        [Test]
        public void TheGuardPattern_MatchesTheCallItForbids()
        {
            foreach (var forbidden in new[]
            {
                "var canvas = FindObjectOfType<Canvas>();",
                "var canvas = FindFirstObjectByType<Canvas>();",
                "foreach (var c in FindObjectsOfType<Canvas>())",
                "foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))",
                "var canvas = FindObjectOfType< Canvas >();",
            })
            {
                Assert.IsTrue(CanvasLookup.IsMatch(forbidden), $"The guard must match: {forbidden}");
            }

            foreach (var allowed in new[]
            {
                "var gm = FindObjectOfType<GameManager>();",
                "var canvas = HudCanvas.Resolve();",
                "FindObjectsByType<Graphic>(FindObjectsSortMode.None)",
            })
            {
                Assert.IsFalse(CanvasLookup.IsMatch(allowed), $"The guard must not match: {allowed}");
            }
        }
    }
}
