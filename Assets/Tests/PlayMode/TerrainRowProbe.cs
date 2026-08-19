using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Asks the terrain blocks what sprite they are wearing, instead of reading it off a screenshot.
    ///
    /// The previous attempt sampled captured pixels at each row's expected screen position and
    /// concluded that only two of five rows carried terrain. That conclusion was unsafe: the sampled
    /// x-range ran through the castle walls and the palisade props, so a row whose pixels disagreed
    /// with its tile could equally be a wrong sprite or a correct sprite standing behind a fence.
    /// Screen sampling cannot separate those two, and no amount of care choosing the range fixes
    /// that — a prop is allowed to cover terrain, and terrain is not allowed to be the wrong tile.
    ///
    /// This measures the sprite each block actually holds. Occlusion becomes irrelevant, because
    /// nothing here looks at the screen.
    /// </summary>
    public class TerrainRowProbe
    {
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator EveryTerrainRowWearsTheTileItsDepthCallsFor()
        {
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            NarrativeVideoIntro.Active?.Skip();
            yield return null;

            var terrain = Object.FindObjectsByType<DestructibleBlock>(FindObjectsSortMode.None)
                .Where(b => b != null && b.IsTerrainTile)
                .ToArray();

            Assert.That(terrain.Length, Is.GreaterThan(0),
                "no block reports IsTerrainTile. Either CreateGround did not run, or the flag is not "
                + "being set — and with the flag unset CastleFacadeDirector re-skins the whole strip, "
                + "which is the regression this probe exists to keep closed.");

            // Derived, never hard-coded. Writing "41 x 5 = 205" here is exactly the mistake this
            // assertion caught on its first run: Stage1's groundHalfWidth is 23, so the strip is 47
            // columns and 235 tiles. A literal would have to be revisited every time a stage's
            // width changes, and the number it disagrees with is the one the game uses.
            int columns = Mathf.RoundToInt(StageDefinitions.Stage1.groundHalfWidth) * 2 + 1;
            const int rows = 5;

            // A short count means CreateGround bailed partway, which it can do silently:
            // `if (groundTex == null) continue` skips a tile without a word.
            Assert.That(terrain.Length, Is.EqualTo(columns * rows),
                $"expected {columns * rows} terrain tiles ({columns} columns x {rows} rows), "
                + $"found {terrain.Length}.");

            // Keyed on y*2, not y. Row centres are exactly -0.5, -1.5, -2.5, -3.5, -4.5, and
            // Mathf.RoundToInt rounds a .5 tie to the nearest EVEN integer — so those five became
            // 0, -2, -2, -4, -4 and five rows collapsed into three groups. Doubling moves every
            // centre onto an odd integer (-1, -3, -5, -7, -9), where no tie exists.
            var byDepth = terrain.GroupBy(b => Mathf.RoundToInt(b.transform.position.y * 2f))
                                 .OrderByDescending(g => g.Key)
                                 .ToArray();

            Assert.That(byDepth.Length, Is.EqualTo(rows), $"terrain must occupy exactly {rows} rows");

            var expected = new[]
            {
                "Ground/ground_tile_grass",
                "Ground/ground_edge_grass",
                "Ground/ground_tile_dirt",
                "Ground/ground_tile_stone",
                "Ground/ground_tile_stone",
            };

            var report = new List<string>();
            var failures = new List<string>();

            for (int i = 0; i < byDepth.Length; i++)
            {
                var want = TileMean(expected[i]);
                var block = byDepth[i].First();
                var sr = block.GetComponent<SpriteRenderer>();
                Assert.That(sr, Is.Not.Null, "a terrain block must have a SpriteRenderer");
                Assert.That(sr.sprite, Is.Not.Null,
                    $"row {i} (world y {byDepth[i].Key / 2f}) has no sprite at all");

                var got = SpriteMean(sr.sprite);
                float dist = Mathf.Sqrt((got.r - want.r) * (got.r - want.r)
                                      + (got.g - want.g) * (got.g - want.g)
                                      + (got.b - want.b) * (got.b - want.b));

                report.Add(string.Format(CultureInfo.InvariantCulture,
                    "row {0} y={1,2} sprite={2,-28} got=({3:F2},{4:F2},{5:F2}) want {6} ({7:F2},{8:F2},{9:F2}) dist={10:F3}",
                    i, byDepth[i].Key / 2f, sr.sprite.name, got.r, got.g, got.b,
                    expected[i].Replace("Ground/", ""), want.r, want.g, want.b, dist));

                // The slice is one cell of a mosaic, so it never equals the source tile exactly:
                // it is resampled and it carries a fraction of a neighbouring tile at the seams.
                // 0.16 sits above that spread and below the gap between any two distinct tiles
                // (dirt to stone is ~0.24), so it tells a wrong tile from a resampled right one.
                if (dist >= 0.16f)
                    failures.Add(report[report.Count - 1]);
            }

            var dir = Path.Combine("_workspace", "current", "qa", "evidence", "art-apply");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "terrain-rows.txt"),
                string.Join("\n", report) + "\n");

            Assert.That(failures, Is.Empty,
                "these terrain rows carry a sprite that does not match their depth:\n"
                + string.Join("\n", failures)
                + "\n\nfull measurement:\n" + string.Join("\n", report));
        }

        /// <summary>Mean RGB of a sprite's own rect, alpha-weighted out.</summary>
        private static Color SpriteMean(Sprite sprite)
        {
            var tex = sprite.texture;
            if (tex == null || !tex.isReadable) return new Color(float.NaN, float.NaN, float.NaN);

            var r = sprite.textureRect;
            int x0 = (int)r.x, y0 = (int)r.y, w = (int)r.width, h = (int)r.height;
            var px = tex.GetPixels(x0, y0, Mathf.Max(1, w), Mathf.Max(1, h));

            float sr = 0, sg = 0, sb = 0; int n = 0;
            for (int i = 0; i < px.Length; i += 3)
            {
                if (px[i].a < 0.125f) continue;
                sr += px[i].r; sg += px[i].g; sb += px[i].b; n++;
            }
            return n == 0 ? new Color(float.NaN, float.NaN, float.NaN)
                          : new Color(sr / n, sg / n, sb / n);
        }

        private static Color TileMean(string resourcePath)
        {
            var sprite = Resources.Load<Sprite>(resourcePath);
            return sprite == null ? new Color(float.NaN, float.NaN, float.NaN) : SpriteMean(sprite);
        }
    }
}
