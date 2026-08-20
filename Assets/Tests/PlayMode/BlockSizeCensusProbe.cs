using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Walks every block on a live board, forces each damage band, and measures what the camera
    /// would see.
    ///
    /// Written because the reporter could not say where the oversized rectangles came from. Repro
    /// steps are the cheap way to find a defect and they were unavailable, so this takes the
    /// expensive way instead: check every block in every band rather than guess which one.
    ///
    /// It also settles the open question from the fix that preceded it. `UpdateVisuals` was swapping
    /// sprite without rescaling, and that gap provably renders at 12.54 world units — but the two
    /// obvious triggers were both absent from the shipped project: the prefab HAS a sprite, and the
    /// three damage PNGs all measure 1254px on disk. The remaining candidate is
    /// <see cref="SpriteAtlasPacker"/>, whose `PackTextures` downscales to fit the atlas while
    /// `Sprite.Create` keeps the ORIGINAL pixelsPerUnit — so a packed sprite's world size is not its
    /// authored world size, and three sprites that agree on disk need not agree after packing.
    ///
    /// This measures that directly instead of arguing about it.
    /// </summary>
    public class BlockSizeCensusProbe
    {
        private List<string> bigAfterShot = new List<string>();

        /// <summary>Every enabled renderer over 2.5 world units, largest first.</summary>
        private static List<string> CensusLarge()
        {
            var found = new List<(float size, string line)>();
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr == null || sr.sprite == null || !sr.enabled) continue;
                float size = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                if (size < 2.5f) continue; // a 1-unit grid; under 2.5 is not one of these rectangles

                var comps = string.Join("+", sr.GetComponents<Component>()
                    .Where(c => c != null && !(c is Transform))
                    .Select(c => c.GetType().Name));
                found.Add((size, string.Format(CultureInfo.InvariantCulture,
                    "{0,7:F2}u  {1,-26} sprite={2,-22} [{3}]",
                    size, sr.gameObject.name, sr.sprite.name, comps)));
            }
            found.Sort((a, b) => b.size.CompareTo(a.size));
            return found.Select(f => f.line).ToList();
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator EveryBlockRendersAtItsTargetSizeInEveryDamageBand()
        {
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            NarrativeVideoIntro.Active?.Skip();
            yield return null;

            var gm = GameManager.Instance;
            Assert.That(gm, Is.Not.Null, "the arena must have a GameManager");
            gm.BeginSiege();
            yield return new WaitForSecondsRealtime(1.0f);

            var blocks = Object.FindObjectsByType<DestructibleBlock>(FindObjectsSortMode.None)
                .Where(b => b != null)
                .ToArray();
            Assert.That(blocks.Length, Is.GreaterThan(0), "the board must have blocks to census");

            var updateVisuals = typeof(DestructibleBlock).GetMethod(
                "UpdateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(updateVisuals, Is.Not.Null, "UpdateVisuals is gone or renamed");

            // Atlas state first: it is the remaining candidate trigger, and it is a property of the
            // run rather than of any one block.
            var packer = SpriteAtlasPacker.Instance;
            var report = new List<string>();
            report.Add($"blocks: {blocks.Length}");
            report.Add($"atlas packer present: {packer != null}");

            if (packer != null)
            {
                // Do the three shipped damage sprites still agree on world size AFTER packing? On
                // disk all three are 1254px at 100 ppu = 12.54 units. If packing gives them
                // different rects, a band swap changes the native size and the old scale is wrong.
                var stone = Resources.Load<BlockData>("StoneBlockData");
                if (stone != null)
                {
                    foreach (var (label, src) in new[]
                    {
                        ("normal", stone.normalSprite),
                        ("cracked", stone.crackedSprite),
                        ("heavy", stone.heavilyCrackedSprite),
                    })
                    {
                        if (src == null) { report.Add($"  {label}: null on BlockData"); continue; }
                        var packed = packer.GetPackedSprite(src);
                        report.Add(string.Format(CultureInfo.InvariantCulture,
                            "  {0,-8} disk {1:F0}px -> packed {2:F0}px | world {3:F2}u -> {4:F2}u | same object: {5}",
                            label, src.rect.width, packed != null ? packed.rect.width : -1,
                            src.bounds.size.x, packed != null ? packed.bounds.size.x : -1f,
                            ReferenceEquals(packed, src)));
                    }
                }
            }

            // Now the census. Each block, each band, measured through the renderer.
            var offenders = new List<string>();
            int measured = 0;

            foreach (var block in blocks)
            {
                var sr = block.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                float target = block.targetWorldSize;
                if (target <= 0.0001f) continue;

                float savedHp = block.currentHP;
                float maxHp = block.maxHP > 0.0001f ? block.maxHP : 1f;

                // Ratios chosen from CastleSkinLibrary.ComputeDisplayBand: >0.7 intact,
                // <=0.7 cracked, <=0.3 crumbling.
                foreach (var (bandName, ratio) in new[] { ("intact", 0.95f), ("cracked", 0.5f), ("crumbling", 0.15f) })
                {
                    block.currentHP = maxHp * ratio;
                    updateVisuals.Invoke(block, null);

                    if (sr.sprite == null) continue;
                    float rendered = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                    measured++;

                    // 25% is deliberately loose. The failure this hunts is a MULTIPLE — 4x, 12x —
                    // and a tight bar would flag ordinary rounding as if it were the same fault.
                    if (Mathf.Abs(rendered - target) > target * 0.25f)
                    {
                        offenders.Add(string.Format(CultureInfo.InvariantCulture,
                            "  {0} [{1}] renders {2:F2}u vs target {3:F2}u ({4:F1}x) sprite={5}",
                            block.name, bandName, rendered, target, rendered / target,
                            sr.sprite.name));
                    }
                }

                block.currentHP = savedHp;
                updateVisuals.Invoke(block, null);
            }

            // Everything on the board, largest first. The census above cleared all 284 blocks, so
            // whatever the reported rectangles are, they are not blocks with a stale scale — and
            // guessing which component they belong to has already been wrong twice (fx_spawn's
            // burst, then the event gates). This lists candidates by the only property the report
            // gave us: they are large.
            var big = CensusLarge();
            report.Add("");
            report.Add($"renderers over 2.5u at match start: {big.Count}");
            foreach (var e in big.Take(10)) report.Add("  " + e);

            // Fire, and census again. Match start had exactly one renderer over 2.5u — the
            // background, legitimately 81.5u — so the reported rectangles do not exist yet at that
            // point. The screenshot showed HIT, BLOOM and turn 7, i.e. after impacts, so whatever
            // they are is spawned mid-match and a start-state census cannot see it.
            var lm = Object.FindFirstObjectByType<LaunchManager>();
            if (lm != null)
            {
                lm.SimulateLaunch(lm.GetSeparatedAimVelocity());
                // Long enough for the flight, the impact, and the burst that follows it.
                yield return new WaitForSecondsRealtime(3.5f);

                var after = CensusLarge();
                report.Add("");
                report.Add($"renderers over 2.5u after one shot: {after.Count}");
                foreach (var e in after.Take(25)) report.Add("  " + e);
                bigAfterShot = after;
            }
            else
            {
                report.Add("");
                report.Add("no LaunchManager; could not census after a shot");
            }

            var unusedBig = new List<(float size, string name, string sprite, string comps)>();
            foreach (var sr in System.Array.Empty<SpriteRenderer>())
            {
                _ = sr;
            }

            report.Add("");
            report.Add($"measurements: {measured}");
            report.Add($"offenders: {offenders.Count}");
            report.AddRange(offenders.Take(30));

            var dir = Path.Combine("_workspace", "current", "qa", "evidence", "block-size");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "census.txt"), string.Join("\n", report) + "\n");

            Assert.That(measured, Is.GreaterThan(100),
                $"only {measured} block/band pairs were measured; a Stage1 board has hundreds, so "
                + "this census covered too little to mean anything.");

            Assert.That(offenders, Is.Empty,
                $"{offenders.Count} block/band pairs render at the wrong size:\n"
                + string.Join("\n", offenders.Take(30))
                + "\n\nA block's scale comes from its sprite's bounds, so any of these is a sprite "
                + "assignment that did not rescale afterwards. Full report: "
                + "_workspace/current/qa/evidence/block-size/census.txt");
        }
    }
}
