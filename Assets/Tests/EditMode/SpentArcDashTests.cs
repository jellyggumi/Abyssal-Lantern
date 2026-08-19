using System.IO;
using NUnit.Framework;
using UnityEngine;
using CastleBusters;

namespace CastleBusters.Tests
{
    /// <summary>
    /// A spent arc must read as a translucent DOTTED line. This file exists because the obvious
    /// test for that does not work: the previous dash attempt in this codebase dropped vertices,
    /// the vertex-count assertion passed, and the arc still rendered solid - a LineRenderer is one
    /// continuous strip, so a missing vertex is a chord, not a hole.
    ///
    /// So these measure the thing that actually cuts the gaps - transparent pixels in the tiled
    /// texture - rather than any property that merely correlates with having tried.
    /// </summary>
    public class SpentArcDashTests
    {
        [Test]
        public void TheDashTexture_ActuallyContainsGaps()
        {
            Assert.IsNotNull(ShotTraceDirector.DashTexture(),
                "no texture means Tile mode repeats nothing and the arc is solid");

            // A gap in a tiled dash is a COLUMN with no ink, because Tile repeats along U and each
            // column is one slice across the line's width. Counting bare pixels would let a texture
            // that is 50% transparent but has ink in every column pass while rendering solid -
            // which is the failure mode a capture already caught on the preview arc once.
            var columns = ColumnAlphaProfile();
            int inked = 0, empty = 0;
            foreach (float a in columns)
            {
                if (a > 0f) inked++; else empty++;
            }

            Assert.Greater(inked, 0, "a fully transparent texture erases the arc entirely");
            Assert.Greater(empty, 0,
                "every column carries ink, so the tiled strip has no holes - this is the exact "
                + "state the preview arc shipped in (939 contiguous columns, zero gaps) while a "
                + "texture-level assertion would have stayed green");
        }

        [Test]
        public void TheGapIsLargeEnoughToRead_AndSmallEnoughToStayALine()
        {
            var columns = ColumnAlphaProfile();
            int empty = 0;
            foreach (float a in columns) if (a <= 0f) empty++;
            float gapFraction = (float)empty / columns.Length;

            // Measured on the shipped art: 28 of 64 columns bare = 0.438, which is the "44% duty
            // cycle" the preview arc's own notes quote. Asserted so a redraw cannot quietly close
            // the gaps back up.
            Assert.AreEqual(0.438f, gapFraction, 0.06f,
                "the shipped dash art no longer matches the duty cycle the design notes quote");
            Assert.Greater(gapFraction, 0.15f, "too little gap and it reads as a solid line again");
            Assert.Less(gapFraction, 0.6f, "too much gap and the arc stops reading as one path");
        }

        [Test]
        public void ASpentArcIsTranslucent_ButNotBelowTheAlphaThatWasMeasuredAndReverted()
        {
            // Assert the COMPOSITE, not the vertex alpha. The rendered arc is texture alpha times
            // vertex alpha, so a test on either factor alone certifies a translucency it never
            // measured - and the vertex alpha is deliberately near-opaque here.
            var columns = ColumnAlphaProfile();
            float peak = 0f;
            foreach (float a in columns) if (a > peak) peak = a;

            Assert.AreEqual(ShotTraceDirector.DashPeakAlpha, peak, 0.02f,
                "the art's peak alpha moved; the composition below is derived from it");

            float effective = peak * ShotTraceDirector.SpentAlpha;
            Assert.Less(effective, 0.7f, "a spent arc must read as translucent, not as a live trail");
            Assert.Greater(effective, 0.35f,
                "0.5 vertex alpha was measured and reverted once because it cost the casing its "
                + "contrast; compounding a faded texture with a faded vertex colour lands there again");
        }

        [Test]
        public void TheSpentArcReusesTheDashArtThePreviewAlreadyMeasured()
        {
            // Two independently authored dash patterns in one game drift apart. More importantly,
            // the preview's pattern is the one with a deployed-build capture behind it.
            var authored = Resources.Load<Sprite>("Effects/trajectory_dash");
            Assert.IsNotNull(authored,
                "the shared dash asset is missing; the spent arc would silently fall back to a "
                + "procedural pattern that nobody has measured on screen");
            Assert.AreSame(authored.texture, ShotTraceDirector.DashTexture(),
                "the spent arc must use the same dash texture as the preview arc");
        }

        /// <summary>
        /// The shipped PNG, decoded from disk rather than read off the imported Texture2D.
        ///
        /// The import has Read/Write disabled, so GetPixels on the live texture throws - and an
        /// unreadable texture is the correct import setting for art that only ever gets sampled by
        /// a shader. Decoding the source file measures exactly what ships without weakening the
        /// import to suit the test.
        /// </summary>
        private static Texture2D DecodeShippedDashTexture()
        {
            string path = Path.Combine(Application.dataPath, "Resources/Effects/trajectory_dash.png");
            Assert.IsTrue(File.Exists(path), $"shared dash art missing at {path}");
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.IsTrue(tex.LoadImage(File.ReadAllBytes(path)), "dash art failed to decode");
            return tex;
        }
        /// <summary>
        /// Peak alpha per texture COLUMN. The dash tiles along U, so a column is the unit that
        /// either draws or leaves a hole in the rendered strip.
        /// </summary>
        private static float[] ColumnAlphaProfile()
        {
            var tex = DecodeShippedDashTexture();
            var pixels = tex.GetPixels();
            var columns = new float[tex.width];
            for (int x = 0; x < tex.width; x++)
            {
                float peak = 0f;
                for (int y = 0; y < tex.height; y++)
                {
                    float a = pixels[y * tex.width + x].a;
                    if (a > peak) peak = a;
                }
                columns[x] = peak;
            }
            return columns;
        }

    }
}
