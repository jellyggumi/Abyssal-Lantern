using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Holds the line on art whose generation history is unknown.
    ///
    /// 204 of 272 images under Resources have no `.provenance.json` beside them, so nobody can say
    /// which tool, model, prompt or palette produced them. That is not recoverable — the history was
    /// never written — and this test does not pretend otherwise. What it does is stop the number
    /// growing.
    ///
    /// It counts rather than lists, after a directory-level version of this test was written first
    /// and immediately proved too coarse: `Effects/fx_spark` and `Effects/particles` are each
    /// PARTLY documented (fx_spark_000 got a provenance file when it was redrawn, its three
    /// siblings did not), so excusing a directory would have excused every future file dropped into
    /// it. A ceiling has no such hole: an undocumented file added anywhere raises the count, and 204
    /// files is too many to enumerate without the list itself becoming the thing nobody reads.
    ///
    /// The cost of the gap is concrete. A tone-matched addition to `CastleSkin` needs to know how
    /// those twelve tiles were made; the answer is unavailable, so any new tile is judged by eye.
    /// `design/tone-reference.json` records what each group measures today, which makes a mismatch
    /// detectable after the fact even when the recipe is lost.
    /// </summary>
    public class ArtProvenanceGuardTests
    {
        // Measured 2026-08-19: 272 images, 68 documented, 204 not. Re-measured 2026-08-21 after the
        // fun-cycle art pass (portraits, gate variants, hex rune, gate anim documented on creation):
        // 282 images, 96 documented, 186 not. This is a ceiling, not a target — lowering it is the
        // point, and the second test below fails if it drifts too far above the real number, so a
        // batch of deletions cannot quietly buy headroom for new undocumented art.
        private const int UndocumentedCeiling = 186;

        private static string[] AllArt()
        {
            const string root = "Assets/Resources";
            if (!Directory.Exists(root)) return System.Array.Empty<string>();
            return Directory.GetFiles(root, "*.*", SearchOption.AllDirectories)
                .Where(f =>
                {
                    var e = Path.GetExtension(f).ToLowerInvariant();
                    return e == ".png" || e == ".jpg" || e == ".jpeg";
                })
                .ToArray();
        }

        [Test]
        public void NoNewArtArrivesWithoutRecordingHowItWasMade()
        {
            var art = AllArt();
            if (art.Length == 0) Assert.Ignore("Assets/Resources holds no images");

            var undocumented = art.Where(p => !File.Exists(p + ".provenance.json")).ToArray();

            Assert.That(undocumented.Length, Is.LessThanOrEqualTo(UndocumentedCeiling),
                $"{undocumented.Length} images have no .provenance.json, above the recorded ceiling of "
                + $"{UndocumentedCeiling}. New art must record the tool, model and prompt that produced "
                + "it — or say plainly that it is hand-made. Six months from now somebody has to make a "
                + "matching asset, and 204 existing images already cannot answer that question; the "
                + "point of the ceiling is that the answer stops getting rarer.\n\nMost recent by write "
                + "time:\n  "
                + string.Join("\n  ", undocumented
                    .OrderByDescending(p => File.GetLastWriteTimeUtc(p))
                    .Take(12)
                    .Select(p => p.Replace("Assets/Resources/", ""))));
        }

        [Test]
        public void TheCeilingStaysHonestAsArtGetsDocumented()
        {
            var art = AllArt();
            if (art.Length == 0) Assert.Ignore("Assets/Resources holds no images");

            int undocumented = art.Count(p => !File.Exists(p + ".provenance.json"));
            int slack = UndocumentedCeiling - undocumented;

            // A ceiling far above the real count is not a guard, it is permission. Ten is wide enough
            // that documenting a small batch does not fail the build the moment it lands, and narrow
            // enough that it cannot absorb a whole new undocumented asset group.
            Assert.That(slack, Is.LessThanOrEqualTo(10),
                $"only {undocumented} images are undocumented but the ceiling still says "
                + $"{UndocumentedCeiling}, leaving {slack} of unearned slack. Lower "
                + "UndocumentedCeiling to the measured number. Left alone, the gap that was closed "
                + "becomes budget for the next one, and the guard reads as passing while doing nothing.");
        }
    }
}
