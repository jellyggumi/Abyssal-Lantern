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

            // The guard was `if (groundTex == null) continue;` until 2026-08-20, when that `continue`
            // turned out to skip the PARENTING below it too. Harmless while a null atlas was a rare
            // failure; wrong once CreateGround began skipping the atlas deliberately, because
            // unparented terrain never joins a castle's block list — so it is never skinned, and
            // never counted by the structural-integrity walk that decides what collapses.
            //
            // Pinned as the positive form, which cannot skip anything but the slice.
            StringAssert.Contains("if (groundTex != null)", source,
                "the per-tile loop must skip only the SLICING when there is no atlas — not the "
                + "parenting that follows it, and not by calling Sprite.Create on null and throwing "
                + "out of Start");

            StringAssert.DoesNotContain("if (groundTex == null) continue;", source,
                "this form skips the parenting at the end of the loop body as well as the slice. "
                + "Terrain that is never parented is never skinned and never load-bearing.");

            StringAssert.Contains("return null;", source,
                "GenerateGroundTexture must return null on a refused request instead of letting the "
                + "exception unwind through CreateGround and Start");
        }

        /// <summary>
        /// Builds the ground at Stage3's width and asserts the call returns instead of throwing.
        ///
        /// This is the measurement the PlayMode census could not deliver: four consecutive attempts
        /// hit the MCP plugin's domain-reload hang, and one of them never even reached the scene, so
        /// its zero-exception count proved nothing. Invoking `CreateGround` directly with the private
        /// width field forced to Stage3's value tests the same code path with no scene at all.
        ///
        /// Before the fix this threw `UnityException: Failed to create texture` from inside
        /// `GenerateGroundTexture`, which is exactly how the keep and core stopped being created.
        /// </summary>
        [Test]
        public void BuildingTheGroundAtStageThreesWidth_DoesNotThrow()
        {
            var go = new GameObject("GameManager_AtlasBudget");
            try
            {
                var gm = go.AddComponent<GameManager>();

                // ApplyStageLayout runs from Start(), which EditMode never calls, so the width is
                // set directly — the same value Stage3's layout would install.
                var widthField = typeof(GameManager).GetField("groundHalfWidth",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(widthField, "precondition: groundHalfWidth must still be the field name");
                widthField.SetValue(gm, Mathf.RoundToInt(StageDefinitions.Stage3.groundHalfWidth));

                var create = typeof(GameManager).GetMethod("CreateGround",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(create, "precondition: CreateGround must still exist");

                Assert.DoesNotThrow(() => create.Invoke(gm, null),
                    "building the ground at Stage3's width must not throw - when it did, the exception "
                    + "unwound out of Start and the stage shipped with no keep and no core");

                // And the tiles must actually exist, or "does not throw" was achieved by doing nothing.
                int columns = Mathf.RoundToInt(StageDefinitions.Stage3.groundHalfWidth) * 2 + 1;
                int found = 0;
                foreach (var b in Object.FindObjectsByType<DestructibleBlock>(FindObjectsSortMode.None))
                    if (b != null && b.name.StartsWith("GroundBlock_")) found++;

                Assert.AreEqual(columns * GroundRowCount, found,
                    $"Stage3's ground must be {columns}x{GroundRowCount} tiles; a short grid means the "
                    + "builder bailed partway rather than completing without art");
            }
            finally
            {
                foreach (var b in Object.FindObjectsByType<DestructibleBlock>(FindObjectsSortMode.None))
                    if (b != null) Object.DestroyImmediate(b.gameObject);
                Object.DestroyImmediate(go);
            }
        }
    }
}
