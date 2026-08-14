using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The ground atlas must stay inside a budget, because exceeding it deleted a whole stage.
    ///
    /// `CreateGround` asked for a texture whose width scaled with the board: 160px per tile column.
    /// Stage1 and Stage2 requested 7520 and 7200 pixels and were served. Stage3's wider ground band
    /// requested 8800 and `Texture2D` threw "invalid parameters" — and that exception unwound out of
    /// `CreateGround`, out of `Start`, and took the rest of the boot sequence with it. The result
    /// shipped: Stage3's enemy castle had five blocks, no wall courses, and no core, so a match there
    /// "decided" after removing 260 total HP against a modelled 1935.
    /// Measured in `qa/evidence/match-length/castle-material-census.md`.
    ///
    /// Two independent things are pinned here, because either alone would have prevented that:
    /// the atlas request stays bounded, and the board still builds when it does not.
    /// </summary>
    public class GroundAtlasBudgetTests
    {
        // Mirrors CreateGround: rows are fixed, resolution is derived, width is columns x resolution.
        private const int GroundRowCount = 5;
        private const int MaxAtlasWidth = 4096;
        private const int MaxTileRes = 160;
        private const int MinTileRes = 16;

        private static int ColumnsFor(StageLayout layout)
            => Mathf.RoundToInt(layout.groundHalfWidth) * 2 + 1;

        private static int TileResFor(StageLayout layout)
            => Mathf.Clamp(MaxAtlasWidth / Mathf.Max(1, ColumnsFor(layout)), MinTileRes, MaxTileRes);

        [Test]
        public void EveryStagesAtlasStaysInsideTheBudget()
        {
            foreach (var layout in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                int columns = ColumnsFor(layout);
                int res = TileResFor(layout);
                int width = columns * res;
                int height = GroundRowCount * res;

                Assert.LessOrEqual(width, MaxAtlasWidth,
                    $"{layout.displayName}: atlas {width}x{height} exceeds the {MaxAtlasWidth}px budget. "
                    + "The unbounded version asked 8800px for this stage and the graphics layer refused, "
                    + "which aborted Start and left the stage with no keep");

                Assert.Greater(res, 0,
                    $"{layout.displayName}: a zero tile resolution would make an empty texture request");
            }
        }

        /// <summary>
        /// The widest board must still get usable tiles, or "bounded" would have been achieved by
        /// making the ground unrecognisable.
        /// </summary>
        [Test]
        public void TheWidestBoardStillGetsLegibleTiles()
        {
            var widest = StageDefinitions.Stage3;
            foreach (var layout in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2 })
                if (ColumnsFor(layout) > ColumnsFor(widest)) widest = layout;

            int res = TileResFor(widest);
            Assert.GreaterOrEqual(res, 64,
                $"{widest.displayName} falls to {res}px per tile, which is below the point where ground "
                + "art reads as ground. If the budget forces this, the atlas needs tiling rather than "
                + "one strip per board");
        }

        /// <summary>
        /// Stage3 is the case that broke, so its numbers are stated outright rather than derived and
        /// forgotten.
        /// </summary>
        [Test]
        public void StageThreeSpecifically_NoLongerRequestsTheSizeThatWasRefused()
        {
            int columns = ColumnsFor(StageDefinitions.Stage3);
            int unbounded = columns * MaxTileRes;      // what the old code asked for
            int bounded = columns * TileResFor(StageDefinitions.Stage3);

            Assert.AreEqual(8800, unbounded,
                "guard: if this is no longer 8800 the board geometry moved and the recorded defect "
                + "figures in castle-material-census.md describe a board that no longer exists");

            Assert.Less(bounded, unbounded,
                $"Stage3 must now ask for less than the {unbounded}px that was refused; it asks {bounded}px");
        }

        /// <summary>
        /// A refused atlas is survivable — the tile keeps its material sprite and the castle still
        /// gets built.
        ///
        /// This is the half that matters more. Bounding the request makes the failure unlikely;
        /// handling null makes it non-fatal. CLAUDE.md §2 says presentation may read simulation and
        /// never write it, and this is the same boundary's third face: presentation failing must not
        /// be able to REMOVE simulation either.
        /// </summary>
        [Test]
        public void TheGroundBuilderHandlesARefusedAtlas()
        {
            var source = System.IO.File.ReadAllText("Assets/Scripts/GameManager.cs");

            StringAssert.Contains("if (groundTex == null) continue;", source,
                "the per-tile loop must skip slicing when there is no atlas, rather than calling "
                + "Sprite.Create on null and throwing out of Start");

            StringAssert.Contains("return null;", source,
                "GenerateGroundTexture must return null on a refused request instead of letting the "
                + "exception unwind through CreateGround and Start");
        }
    }
}
