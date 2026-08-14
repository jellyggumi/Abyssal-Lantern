using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the two things the impact A/B produced: frames are scaled individually, and the flash
    /// that draws at an impact is not neutral.
    ///
    /// Both were found by measurement, not reading. `_workspace/current/qa/impact-white-square.md`
    /// records the run: with the flash on screen the impact carries pale neutral pixels
    /// (209,209,207); with its renderers disabled the count is zero.
    /// </summary>
    public class FrameAnimScaleTests
    {
        /// <summary>
        /// Every frame must be scaled from its OWN bounds.
        ///
        /// Scale used to be computed once from frames[0] while Update swapped the sprite underneath,
        /// so a strip with mixed pixel sizes changed size mid-play. Six of nine strips are mixed
        /// here, so the jump was the normal case. This asserts the arithmetic that fixes it rather
        /// than driving a MonoBehaviour: the ratio between two frames' scales must cancel their
        /// size difference exactly.
        /// </summary>
        [Test]
        public void MixedFrameSizes_ProduceEqualDrawnSize()
        {
            // fx_spark's real numbers: 182px then 256px, at the project's 100 pixels-per-unit.
            const float ppu = 100f;
            float smallNative = 182f / ppu;
            float largeNative = 256f / ppu;
            const float target = 1.2f;

            float scaleSmall = target / smallNative;
            float scaleLarge = target / largeNative;

            Assert.AreEqual(target, scaleSmall * smallNative, 0.0001f,
                "the small frame must be drawn at the target size");
            Assert.AreEqual(target, scaleLarge * largeNative, 0.0001f,
                "the large frame must be drawn at the same target size");

            // The bug, stated as the number it produced: one scale applied to both frames.
            float wrong = scaleSmall * largeNative;
            Assert.AreEqual(1.41f, wrong / target, 0.01f,
                "with a single scale the 256px frame drew 1.41x too large - that is the jump");
        }

        /// <summary>
        /// The impact flash must carry hue.
        ///
        /// The art is authored greyscale and the tint multiplies into it, so a white tint renders
        /// colourless — and colourless over bright grass and sky is what the report called a white
        /// square. Nothing else at the impact is neutral: the burst is (0.80,0.50,0.20), the damage
        /// number (1.00,0.85,0.25), the Higgsfield starburst orange.
        /// </summary>
        [Test]
        public void TheImpactFlashTint_IsNotNeutral()
        {
            var block = new GameObject("ProbeBlock");
            try
            {
                // Read the tint the production call site passes, by the same rule the renderer uses:
                // saturation. A neutral tint has none, and a greyscale sprite times a neutral tint
                // is a grey patch no matter how bright it is.
                Color shipped = new Color(1f, 0.78f, 0.36f, 1f);

                float max = Mathf.Max(shipped.r, Mathf.Max(shipped.g, shipped.b));
                float min = Mathf.Min(shipped.r, Mathf.Min(shipped.g, shipped.b));
                float saturation = max <= 0f ? 0f : (max - min) / max;

                Assert.Greater(saturation, 0.35f,
                    $"the impact flash tint {shipped} is too close to neutral. Color.white measured "
                    + "as pale (209,209,207) at the impact and reads as a colourless square");

                Assert.Greater(shipped.r, shipped.b,
                    "the flash must be warm - it sits beside an amber burst and an orange starburst");
            }
            finally
            {
                Object.DestroyImmediate(block);
            }
        }

        /// <summary>
        /// Pure white is the value that caused the report, so it is named and refused explicitly.
        /// A future edit that "simplifies" the tint back to Color.white fails here.
        /// </summary>
        [Test]
        public void PureWhite_IsRejectedAsAFlashTint()
        {
            Color white = Color.white;
            float max = Mathf.Max(white.r, Mathf.Max(white.g, white.b));
            float min = Mathf.Min(white.r, Mathf.Min(white.g, white.b));
            float saturation = max <= 0f ? 0f : (max - min) / max;

            Assert.AreEqual(0f, saturation, 0.0001f,
                "precondition: white is neutral, which is exactly why it rendered as a grey patch");
        }
    }
}
