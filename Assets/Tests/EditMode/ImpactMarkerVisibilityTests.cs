using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins that the predicted-impact marker is a FILLED dot, not an outline.
    ///
    /// "붉은 점" is a stated requirement, and <c>LaunchManager.PredictedDestinationColor</c> carries a
    /// docstring saying so — but nothing measured the SHAPE, so the requirement was breakable in
    /// silence. On 2026-08-19 it broke: the procedural disc was swapped for
    /// <c>ui_impact_marker.png</c>, which is a thin crosshair with 8.8% of its pixels opaque. At the
    /// 0.44 world units the marker draws at — about 14 screen pixels — an 8.8% outline is not a dot,
    /// it is nothing. The player reported the red dot as gone. It was.
    ///
    /// The swap was made for a real reason: the disc baked amber into its pixels, so the self-hit
    /// path multiplied amber by blue and got mud. That is fixed here by drawing the disc WHITE and
    /// letting the renderer tint carry the colour — which keeps the fill and the clean tint at once,
    /// instead of trading one for the other.
    ///
    /// Coverage is measurable here precisely because the sprite is procedural: it is built at
    /// runtime from a Texture2D this code owns, so GetPixels works. The equivalent check on the
    /// authored art would need Read/Write enabled on a shipped texture to re-learn a constant.
    /// </summary>
    public class ImpactMarkerVisibilityTests
    {
        [Test]
        public void TheMarkerIsAFilledDiscRatherThanAnOutline()
        {
            var go = new GameObject("ImpactMarkerVisibility_LaunchManager");
            try
            {
                var lm = go.AddComponent<LaunchManager>();

                var builder = typeof(LaunchManager).GetMethod(
                    "CreateCircleSprite", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(builder, Is.Not.Null,
                    "CreateCircleSprite is gone or renamed. It is the marker's shape — if it has been "
                    + "replaced by an authored sprite, measure that sprite's coverage here instead of "
                    + "deleting this test, because coverage is the property that failed.");

                // The same arguments the marker branch passes: 0.22 radius, white pixels.
                var sprite = (Sprite)builder.Invoke(lm, new object[] { 0.22f, Color.white });
                Assert.That(sprite, Is.Not.Null, "the builder must return a sprite");

                var px = sprite.texture.GetPixels();
                int opaque = px.Count(c => c.a >= 0.5f);
                float coverage = (float)opaque / px.Length;

                // A disc inscribed in its square covers pi/4 = 0.785. 0.5 is the floor that separates
                // "filled" from every outline: ui_impact_marker measures 0.088, ui_launch_origin
                // 0.078, ui_deploy_ghost 0.086 — an outline cannot reach half.
                Assert.That(coverage, Is.GreaterThan(0.5f),
                    $"the marker covers {coverage:P1} of its texture. Below half it is an outline, and "
                    + "an outline at 14 screen pixels is invisible — which is how the red dot went "
                    + "missing. A filled disc measures about 78%.");

                // White pixels are the other half of the fix. A pre-coloured sprite multiplies against
                // the renderer tint, and the self-hit path retints to blue: amber x blue is mud, at
                // the one moment the marker matters most.
                var lit = px.Where(c => c.a >= 0.5f).ToArray();
                Assert.That(lit.All(c => c.r > 0.95f && c.g > 0.95f && c.b > 0.95f), Is.True,
                    "the marker's opaque pixels must be white so the renderer tint is the only thing "
                    + "colouring them. Baking the colour in means the self-hit retint multiplies two "
                    + "colours together instead of replacing one.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void TheStatedRedRemainsRedRatherThanDriftingToWhateverArtShipped()
        {
            var field = typeof(LaunchManager).GetField(
                "PredictedDestinationColor", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null,
                "PredictedDestinationColor is gone. It exists so '붉은 점' is a named constant rather "
                + "than a literal in two construction branches, which is how a requirement quietly "
                + "becomes whatever the art happened to ship with.");

            var c = (Color)field.GetValue(null);

            // Red-dominant and clearly warm. Not an exact tuple: the shade is a design choice, "it is
            // red" is the requirement, and pinning the exact float would make a tasteful nudge fail.
            Assert.That(c.r, Is.GreaterThan(0.8f), $"the marker must read as red; r={c.r:F2}");
            Assert.That(c.r - c.g, Is.GreaterThan(0.4f),
                $"red must dominate green by a clear margin; r={c.r:F2} g={c.g:F2}");
            Assert.That(c.r - c.b, Is.GreaterThan(0.4f),
                $"red must dominate blue by a clear margin; r={c.r:F2} b={c.b:F2}");
            Assert.That(c.a, Is.GreaterThan(0.7f),
                $"the dot must be near-opaque; a translucent dot on a busy board is the same failure "
                + $"as an outline. a={c.a:F2}");
        }
    }
}
