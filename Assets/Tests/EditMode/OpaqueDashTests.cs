using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins that the live arc's dash reaches full alpha while keeping the spent arc's shape.
    ///
    /// The reported symptom was "the arc after firing is still too dark", and it survived two
    /// attempts at brightening because the colour was never the problem. The core draws through a
    /// dash whose marks peak at alpha 0.549, over a casing that is nearly black — so a white dot
    /// composites to 0.536 grey, which is DARKER than this board's sky at 0.72. Measured against
    /// sky that is 1.14:1 and against grass 1.25:1, where 3:1 is the floor for a non-text graphic.
    ///
    /// Every brighter colour measured worse: opaque white 2.44, amber 1.58, orange 1.06. Both board
    /// surfaces sit at mid luminance, so a pale line on pale sky has nowhere to go. Opacity is the
    /// lever — an opaque white dot against the dark casing measures 15.11:1, and the casing against
    /// the board 6.18 and 4.45.
    ///
    /// The lift has to preserve the dash SHAPE, because the live and spent arcs are meant to be one
    /// pattern at two opacities. A second authored asset is how the period and duty cycle drift
    /// apart, so this asserts the derivation rather than the artwork.
    /// </summary>
    public class OpaqueDashTests
    {
        [Test]
        public void TheOpaqueDashReachesFullAlphaWhereTheSharedOneDoesNot()
        {
            var shared = ShotTraceDirector.DashTexture();
            Assert.That(shared, Is.Not.Null, "the shared dash must resolve, authored or procedural");

            if (!shared.isReadable)
                Assert.Ignore("the authored dash is not readable here; the lift falls back by design");

            var opaque = ShotTraceDirector.OpaqueDashTexture();
            Assert.That(opaque, Is.Not.Null, "the opaque variant must resolve");

            byte sharedPeak = Peak(shared);
            byte opaquePeak = Peak(opaque);

            Assert.That(opaquePeak, Is.EqualTo(255).Within(2),
                $"the opaque dash peaks at {opaquePeak}/255. Its whole purpose is that the marks are "
                + "opaque, so the bright core composites to white over the casing instead of the "
                + "0.536 grey that made the arc read as dark.");

            // If the shared asset were already opaque there would be nothing to fix, and this test
            // would be asserting a tautology — worth catching, because the art can be redrawn.
            Assert.That(sharedPeak, Is.LessThan(250),
                $"the shared dash already peaks at {sharedPeak}/255, so the spent arc is opaque too "
                + "and the live/spent distinction this file exists for has quietly disappeared.");
        }

        [Test]
        public void TheLiftIsProportionalSoTheDashKeepsItsShape()
        {
            var shared = ShotTraceDirector.DashTexture();
            if (shared == null || !shared.isReadable)
                Assert.Ignore("needs a readable shared dash to compare against");

            var opaque = ShotTraceDirector.OpaqueDashTexture();
            Assert.That(opaque, Is.Not.Null);
            if (ReferenceEquals(opaque, shared))
                Assert.Ignore("the fallback returned the original; nothing to compare");

            Assert.That(opaque.width, Is.EqualTo(shared.width), "the period must not change");
            Assert.That(opaque.height, Is.EqualTo(shared.height), "the profile must not change");

            var a = shared.GetPixels32();
            var b = opaque.GetPixels32();
            Assert.That(b.Length, Is.EqualTo(a.Length));

            float lift = 255f / Peak(shared);
            int mismatches = 0;
            int softPixels = 0;

            for (int i = 0; i < a.Length; i++)
            {
                // A threshold would flatten the soft edges into stair-steps, which at ~3px of core
                // width is visible as the dots gaining corners. Proportional scaling keeps them.
                float expected = Mathf.Min(255f, a[i].a * lift);
                if (Mathf.Abs(expected - b[i].a) > 2f) mismatches++;

                // Count partially-transparent source pixels: if the art has none, "soft edges" is a
                // claim about nothing and the proportional lift is untested by this data.
                if (a[i].a > 8 && a[i].a < Peak(shared) - 8) softPixels++;
            }

            Assert.That(mismatches, Is.Zero,
                $"{mismatches} pixels do not match a proportional lift. A cutoff or a per-pixel "
                + "rewrite changes the dash's shape, and the live and spent arcs are supposed to be "
                + "the same pattern at two opacities.");

            Assert.That(softPixels, Is.GreaterThan(0),
                "the shared dash has no partially-transparent pixels, so its edges are already hard "
                + "and this test cannot show that the lift preserves soft ones. Not a failure of the "
                + "code — a sign the art changed and this assertion needs rethinking.");
        }

        private static byte Peak(Texture2D tex)
        {
            byte peak = 0;
            foreach (var p in tex.GetPixels32()) if (p.a > peak) peak = p.a;
            return peak;
        }
    }
}
