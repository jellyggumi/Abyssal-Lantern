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
    /// Pins that the terrain strip wears the see-through CastleSkin masonry, not an opaque ground
    /// tile.
    ///
    /// This is a reversal of what the file asserted when it was written, and the reversal is the
    /// point. Terrain was excluded from <c>CastleFacadeDirector</c> on 2026-08-19 so the ground
    /// atlas would survive being assigned — the atlas was genuinely being discarded, and that part
    /// was true. What went unmeasured was opacity: CastleSkin tiles are 47-82% opaque masonry that
    /// the background's grass and path read through, and the ground tiles are 100% opaque. Letting
    /// the ground win covered the background with a 47x5 opaque rectangle across the middle of the
    /// screen, which was reported as one enormous wall. It was.
    ///
    /// So the atlas loses and the look wins, and this test is what stops the atlas being restored
    /// on the strength of the waste argument alone. Opacity is asserted directly, because "it looks
    /// like a slab" is the symptom and "the sprite is opaque" is the cause.
    ///
    /// The probe reads sprites off the blocks rather than pixels off a screenshot. Screen sampling
    /// cannot separate a wrong tile from a correct tile standing behind a fence — a prop may cover
    /// terrain, a tile may not be wrong, and both produce the same pixels. That mistake cost a
    /// session's worth of wrong conclusions before this file existed.
    /// </summary>
    public class TerrainRowProbe
    {
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator TerrainWearsSeeThroughMasonryRatherThanAnOpaqueGroundTile()
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
                + "being set — and the flag is what keeps the ground's 47 columns out of the bounds "
                + "that decide every WALL block's facade role.");

            // Derived, never hard-coded. A literal "41 x 5 = 205" here failed on this assertion's
            // first run: Stage1's groundHalfWidth is 23, so the strip is 47 columns and 235 tiles.
            int columns = Mathf.RoundToInt(StageDefinitions.Stage1.groundHalfWidth) * 2 + 1;
            const int rows = 5;

            Assert.That(terrain.Length, Is.EqualTo(columns * rows),
                $"expected {columns * rows} terrain tiles ({columns} columns x {rows} rows), "
                + $"found {terrain.Length}. A short count means CreateGround bailed partway, which it "
                + "can do without a word.");

            // Keyed on y*2. Row centres are exactly -0.5, -1.5, -2.5, -3.5, -4.5, and
            // Mathf.RoundToInt sends a .5 tie to the nearest EVEN integer — so those five became
            // 0, -2, -2, -4, -4 and five rows collapsed into three groups.
            var byDepth = terrain.GroupBy(b => Mathf.RoundToInt(b.transform.position.y * 2f))
                                 .OrderByDescending(g => g.Key)
                                 .ToArray();

            Assert.That(byDepth.Length, Is.EqualTo(rows), $"terrain must occupy exactly {rows} rows");

            Assert.That(CastleSkinLibrary.TryGetSkin(CastleSkinRole.Face, out var faceSkin, out _, out _),
                Is.True,
                "the Face skin must load; terrain sits outside the castle bounds so AssignRole gives "
                + "it Face, and without that art there is nothing see-through to wear.");

            var report = new List<string>();
            var opaque = new List<string>();
            var wrongSprite = new List<string>();

            for (int i = 0; i < byDepth.Length; i++)
            {
                var block = byDepth[i].First();
                var sr = block.GetComponent<SpriteRenderer>();
                Assert.That(sr, Is.Not.Null, "a terrain block must have a SpriteRenderer");
                Assert.That(sr.sprite, Is.Not.Null,
                    $"row {i} (world y {byDepth[i].Key / 2f}) has no sprite at all");

                // Sprite IDENTITY, not runtime opacity. The first version measured coverage here and
                // it returned NaN every row: CastleSkin textures ship with Read/Write disabled, so
                // GetPixels throws and the guard `!float.IsNaN(coverage)` quietly skipped the
                // assertion. It passed on every board, including a slab.
                //
                // The opacity numbers this rests on were measured offline instead — CastleSkin
                // 0.47-0.82, Ground a flat 1.000 — and identity is what carries them: wearing a
                // CastleSkin sprite IS wearing something 47-82% opaque. Enabling Read/Write on
                // twelve 512px textures to re-derive that at runtime would cost memory in the
                // shipped build to re-learn a constant.
                bool isSkin = sr.sprite.name.StartsWith("base_")
                              || sr.sprite.name.StartsWith("face_")
                              || sr.sprite.name.StartsWith("edge_")
                              || sr.sprite.name.StartsWith("crown_");

                report.Add(string.Format(CultureInfo.InvariantCulture,
                    "row {0} y={1,4} sprite={2,-28} skin={3}",
                    i, byDepth[i].Key / 2f, sr.sprite.name, isSkin));

                // A ground slice is named GroundSlice_*; block_normal is the BlockData default a
                // block keeps when nothing skinned it. Both mean the facade did not reach this
                // tile, and they mean it for different reasons worth telling apart.
                if (sr.sprite.name.StartsWith("GroundSlice"))
                    wrongSprite.Add(report[report.Count - 1]);
                else if (!isSkin)
                    opaque.Add(report[report.Count - 1]);
            }

            var dir = Path.Combine("_workspace", "current", "qa", "evidence", "art-apply");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "terrain-rows.txt"), string.Join("\n", report) + "\n");

            Assert.That(wrongSprite, Is.Empty,
                "terrain is wearing ground-atlas slices again:\n" + string.Join("\n", wrongSprite)
                + "\n\nThat means CastleFacadeDirector is skipping terrain. The atlas being discarded "
                + "is real waste, but the fix for it is not building the atlas — CreateGround skips "
                + "it when the skin exists. Excluding terrain from the facade instead puts the slab "
                + "back on screen.\n\nfull measurement:\n" + string.Join("\n", report));

            Assert.That(opaque, Is.Empty,
                "terrain tiles are effectively opaque:\n" + string.Join("\n", opaque)
                + "\n\nThe background art already draws this band's grass, path and rocks. A terrain "
                + "sprite that covers it replaces detailed art with a flat rectangle 47 units wide, "
                + "and that rectangle is what got reported as a giant wall. The masonry skin works "
                + "because it is 47-82% opaque and the background reads through it.\n\nfull "
                + "measurement:\n" + string.Join("\n", report));
        }

    }
}
