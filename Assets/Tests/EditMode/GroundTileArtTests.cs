using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins that the authored ground tiles load, that they are tonally separated, and that
    /// <c>CreateGround</c> actually reaches the art path instead of the procedural fallback.
    ///
    /// The third pin is the one that earns the file. The art landed, the importers were correct,
    /// and a captured board measured identical to the previous one to three decimal places. The
    /// first two tests here were written expecting to find a failed load — they passed, so the
    /// cause was NOT the load. That left the fallback silently winning for some other reason, and
    /// the fallback is invisible by construction: `?? GenerateGroundTexture(...)` produces a
    /// perfectly good board either way, which is exactly why nothing anywhere reported a problem.
    ///
    /// Tonal separation is pinned because tiles that all load but share one brightness still
    /// render as a single flat surface, and the board reading as flat is the complaint this art
    /// was ordered to answer.
    /// </summary>
    public class GroundTileArtTests
    {
        // Resource paths, not file paths: the runtime reaches these through Resources.Load, and
        // that is the call that failed while the files were present the whole time.
        private static readonly string[] RequiredTiles =
        {
            "Ground/ground_tile_grass",
            "Ground/ground_edge_grass",
            "Ground/ground_tile_dirt",
            "Ground/ground_tile_stone",
        };

        private static readonly string[] GrassVariants =
        {
            "Ground/ground_variant_a",
            "Ground/ground_variant_b",
            "Ground/ground_variant_c",
        };

        [Test]
        public void EveryRequiredGroundTileLoadsAsASprite()
        {
            var missing = RequiredTiles.Where(p => Resources.Load<Sprite>(p) == null).ToArray();

            Assert.That(missing, Is.Empty,
                "these ground tiles did not load as sprites: " + string.Join(", ", missing)
                + ". The atlas builder treats a single null as all-or-nothing and falls back to the "
                + "procedural bands, so a partial set shows the player none of the authored art while "
                + "the files sit on disk looking correct. The usual cause is textureType: the importer "
                + "must be 8 (Sprite), and a PNG imported as Default returns null here.");
        }

        [Test]
        public void GrassVariantsLoadSoAdjacentColumnsDoNotRepeat()
        {
            var missing = GrassVariants.Where(p => Resources.Load<Sprite>(p) == null).ToArray();

            Assert.That(missing, Is.Empty,
                "grass variants missing: " + string.Join(", ", missing)
                + ". These are not required for the ground to draw — the builder substitutes the base "
                + "grass tile — but without them 41 columns draw the same image and the top row reads "
                + "as wallpaper rather than terrain.");
        }

        [Test]
        public void GroundTilesAreReadableBecauseTheBuilderSamplesTheirPixels()
        {
            foreach (var path in RequiredTiles.Concat(GrassVariants))
            {
                var sprite = Resources.Load<Sprite>(path);
                if (sprite == null) continue; // the load test above owns that failure

                Assert.That(sprite.texture.isReadable, Is.True,
                    $"{path} must keep Read/Write enabled. BuildGroundAtlasFromArt samples every tile "
                    + "through GetPixels32 to scale it into one atlas cell, and an unreadable texture "
                    + "throws UnityException there. That throw is caught — deliberately, so a cosmetic "
                    + "problem cannot delete the board — which means the failure is a transparent cell "
                    + "rather than an error anyone would see.");
            }
        }

        [Test]
        public void TheFourDepthTilesAreTonallySeparated()
        {
            // Row order the builder assigns, top of the board downward.
            var order = new[]
            {
                ("grass", "Ground/ground_tile_grass"),
                ("edge",  "Ground/ground_edge_grass"),
                ("dirt",  "Ground/ground_tile_dirt"),
                ("stone", "Ground/ground_tile_stone"),
            };

            var lums = order.Select(o => (o.Item1, Mean(Resources.Load<Sprite>(o.Item2)))).ToArray();
            if (lums.Any(l => float.IsNaN(l.Item2))) Assert.Ignore("tiles do not load; see the load test");

            // 0.02 is the floor at which two adjacent rows stop reading as one surface. Below it the
            // rows differ in hue only, and hue alone does not carry depth — the delivered art first
            // arrived clustered inside 0.007 and the board still looked like a single plane.
            for (int i = 0; i < lums.Length - 1; i++)
            {
                float d = Mathf.Abs(lums[i + 1].Item2 - lums[i].Item2);
                Assert.That(d, Is.GreaterThan(0.02f),
                    $"{lums[i].Item1} ({lums[i].Item2:F3}) and {lums[i + 1].Item1} ({lums[i + 1].Item2:F3}) "
                    + $"are {d:F3} apart in mean luminance. Adjacent depth rows this close render as one "
                    + "flat surface, which is the exact complaint the tiles were drawn to answer.");
            }
        }

        [Test]
        public void TheAtlasBuilderReturnsArtRatherThanFallingBackSilently()
        {
            // Exactly the arguments CreateGround passes for Stage1: 41 columns, 5 rows, and the
            // blockRes it derives from the 4096 atlas budget.
            const int columns = 41;
            const int rows = 5;
            int blockRes = Mathf.Clamp(4096 / columns, 16, 160);

            var builder = typeof(GameManager).GetMethod(
                "BuildGroundAtlasFromArt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            Assert.That(builder, Is.Not.Null,
                "BuildGroundAtlasFromArt is gone or changed shape. It is private and static by design "
                + "— it touches no instance state — but that also means only reflection can reach it, "
                + "so a rename silently turns this pin into a no-op unless this assert catches it.");

            var atlas = (Texture2D)builder.Invoke(null,
                new object[] { columns * blockRes, rows * blockRes, blockRes, rows, columns });

            Assert.That(atlas, Is.Not.Null,
                "the builder returned null, so CreateGround fell through to GenerateGroundTexture and "
                + "the player sees procedural bands. Null here means at least one of the four required "
                + "tiles failed to load or Texture2D refused the requested size — and neither shows up "
                + "as an error anywhere, because `?? GenerateGroundTexture(...)` produces a working "
                + "board regardless. That silence is the whole reason this test exists.");

            // Sample the row the grass tiles occupy and the row the stone tiles occupy. The
            // procedural fallback wrote flat bands plus ±10 noise; authored tiles carry per-pixel
            // structure. Comparing variance inside one row separates them without hard-coding
            // either one's colours.
            Assert.That(atlas.isReadable, Is.True, "the atlas must stay readable for this sampling");
            float topVariance = RowVariance(atlas, atlas.height - blockRes / 2, blockRes);
            Assert.That(topVariance, Is.GreaterThan(0.0004f),
                $"the top ground row varies by {topVariance:F6} across its width, which is flat enough "
                + "to be the procedural band rather than four interchangeable grass tiles. A row built "
                + "from authored art varies both within a tile and between neighbouring tiles.");
        }

        [Test]
        public void TheAtlasRowsRunGrassDownToStoneTopToBottom()
        {
            const int columns = 41;
            const int rows = 5;
            int blockRes = Mathf.Clamp(4096 / columns, 16, 160);

            var builder = typeof(GameManager).GetMethod(
                "BuildGroundAtlasFromArt",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            var atlas = (Texture2D)builder.Invoke(null,
                new object[] { columns * blockRes, rows * blockRes, blockRes, rows, columns });
            if (atlas == null) Assert.Ignore("builder returned null; the fallback test owns that");

            // Expected top-down: grass surface, the grass→dirt transition, dirt, then stone twice.
            // Grass on top is not a style choice — the top row is the surface units stand on, and a
            // board that runs stone-over-grass reads as a cave floor.
            var expected = new[]
            {
                "Ground/ground_tile_grass",
                "Ground/ground_edge_grass",
                "Ground/ground_tile_dirt",
                "Ground/ground_tile_stone",
                "Ground/ground_tile_stone",
            };

            for (int fromTop = 0; fromTop < rows; fromTop++)
            {
                // Atlas y counts up from the bottom, so the visually-top row is the highest y.
                int y = (rows - 1 - fromTop) * blockRes + blockRes / 2;
                var actual = RowMean(atlas, y, blockRes);
                var want = TileMean(Resources.Load<Sprite>(expected[fromTop]));
                if (float.IsNaN(want.r)) Assert.Ignore("tiles do not load; see the load test");

                float dist = Mathf.Sqrt((actual.r - want.r) * (actual.r - want.r)
                                      + (actual.g - want.g) * (actual.g - want.g)
                                      + (actual.b - want.b) * (actual.b - want.b));

                // 0.12 in normalised RGB: the four distinct tiles sit far further apart than this
                // (dirt to stone is ~0.24), and the three grass variants sit far closer, so this
                // separates "wrong tile" from "an interchangeable variant of the right one".
                Assert.That(dist, Is.LessThan(0.12f),
                    $"atlas row {fromTop} from the top averages ({actual.r:F2},{actual.g:F2},{actual.b:F2}) "
                    + $"but {expected[fromTop]} averages ({want.r:F2},{want.g:F2},{want.b:F2}) — {dist:F2} apart. "
                    + "The builder's row assignment and CreateGround's pixelY must agree about which end "
                    + "of the texture is the top; when they disagree the board renders its depth ramp "
                    + "upside down, and both halves look individually correct while doing it.");
            }
        }

        private static Color RowMean(Texture2D atlas, int y, int step)
        {
            y = Mathf.Clamp(y, 0, atlas.height - 1);
            float r = 0, g = 0, b = 0; int n = 0;
            for (int x = 0; x < atlas.width; x += Mathf.Max(1, step / 4))
            {
                var c = atlas.GetPixel(x, y);
                if (c.a < 0.125f) continue;
                r += c.r; g += c.g; b += c.b; n++;
            }
            return n == 0 ? new Color(float.NaN, float.NaN, float.NaN) : new Color(r / n, g / n, b / n);
        }

        private static Color TileMean(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null || !sprite.texture.isReadable)
                return new Color(float.NaN, float.NaN, float.NaN);
            var px = sprite.texture.GetPixels32();
            float r = 0, g = 0, b = 0; int n = 0;
            for (int i = 0; i < px.Length; i += 4)
            {
                if (px[i].a < 32) continue;
                r += px[i].r / 255f; g += px[i].g / 255f; b += px[i].b / 255f; n++;
            }
            return n == 0 ? new Color(float.NaN, float.NaN, float.NaN) : new Color(r / n, g / n, b / n);
        }

        /// <summary>Luminance variance along one horizontal line of the atlas.</summary>
        private static float RowVariance(Texture2D atlas, int y, int step)
        {
            y = Mathf.Clamp(y, 0, atlas.height - 1);
            var vals = new System.Collections.Generic.List<float>();
            for (int x = 0; x < atlas.width; x += Mathf.Max(1, step / 8))
            {
                var c = atlas.GetPixel(x, y);
                if (c.a < 0.125f) continue;
                vals.Add(0.2126f * c.r + 0.7152f * c.g + 0.0722f * c.b);
            }
            if (vals.Count < 2) return 0f;
            float mean = vals.Average();
            return vals.Sum(v => (v - mean) * (v - mean)) / vals.Count;
        }

        private static float Mean(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null || !sprite.texture.isReadable) return float.NaN;
            var px = sprite.texture.GetPixels32();
            if (px.Length == 0) return float.NaN;

            double total = 0;
            int counted = 0;
            // Every 4th pixel: this is a tone check, and a full 128x128 read per tile per test run
            // buys nothing the sample does not already give.
            for (int i = 0; i < px.Length; i += 4)
            {
                if (px[i].a < 32) continue;
                total += (0.2126 * px[i].r + 0.7152 * px[i].g + 0.0722 * px[i].b) / 255.0;
                counted++;
            }
            return counted == 0 ? float.NaN : (float)(total / counted);
        }
    }
}
