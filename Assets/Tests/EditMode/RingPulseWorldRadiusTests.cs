using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins that <c>GameFeelRingPulse.finalRadius</c> means world units for any sprite.
    ///
    /// It did not. The pulse assigned <c>localScale = finalRadius</c>, which is only a world radius
    /// when the sprite is already 1 unit across — and exactly one of its two callers satisfied that.
    /// <c>SpawnShockwaveRing</c> builds its ring as <c>Sprite.Create(tex, rect, pivot, 48)</c> at
    /// 48px, i.e. 1.0 unit native, so the field's name was true there. <c>SpawnHiggsfieldAccent</c>
    /// passes authored art at 512px and 100 ppu — 5.12 units native — so the same assignment
    /// rendered 5.12x what the caller asked for.
    ///
    /// Measured on a live board before the fix: a collapse accent asking for 0.71
    /// (<c>SpawnCollapseDust</c> clamps to 0.4-0.85) drew at 3.64 units. 0.71 x 5.12 = 3.63. Those
    /// were the translucent overlapping quads reported as huge rectangles.
    ///
    /// The two cases are asserted together on purpose. A test that only checked the art would pass
    /// on a "fix" that broke the procedural ring, and the ring was the one that already worked.
    /// </summary>
    public class RingPulseWorldRadiusTests
    {
        [TestCase(48, 48f, 1.00f, TestName = "procedural ring, 1.0u native")]
        [TestCase(512, 100f, 5.12f, TestName = "Higgsfield VFX art, 5.12u native")]
        public void ThePulseReachesItsRequestedWorldRadiusWhateverTheSpriteMeasures(
            int pixels, float pixelsPerUnit, float expectedNative)
        {
            const float requested = 0.71f; // the collapse accent's measured radius

            var go = new GameObject("RingPulseProbe");
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = MakeSprite(pixels, pixelsPerUnit);

                // Sanity: the case is only meaningful if the native size is what it claims.
                float native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
                Assert.That(native, Is.EqualTo(expectedNative).Within(0.01f),
                    $"this case is about a {expectedNative:F2}u sprite; it built {native:F2}u");

                var pulse = go.AddComponent<GameFeelRingPulse>();
                // A long lifetime with elapsed just under it. Update opens with
                // `elapsed += Time.deltaTime`, and EditMode's deltaTime is whatever the editor last
                // measured — with a 0.35s lifetime that push was enough to carry t past 1, where the
                // pulse calls Destroy and EditMode logs an error NUnit fails on. Scaling both up by
                // ~300x makes deltaTime irrelevant while leaving t at 0.99.
                pulse.lifetime = 100f;
                pulse.finalRadius = requested;
                pulse.startColor = Color.white;

                Invoke(pulse, "Awake");

                // Ease is 1 - (1-t)^2, so t = 0.99 puts the scale within 0.01% of its target.
                SetPrivate(pulse, "elapsed", 99f);
                Invoke(pulse, "Update");

                float rendered = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);
                Assert.That(rendered, Is.EqualTo(requested).Within(requested * 0.05f),
                    $"asked for {requested:F2} world units and drew {rendered:F2} "
                    + $"({rendered / requested:F2}x) from a {native:F2}u sprite. finalRadius is a "
                    + "world radius, so the pulse has to divide by the sprite's own size — assigning "
                    + "it straight to localScale makes the field mean 'multiples of whatever the art "
                    + "happens to measure', which is 5.12x for the 512px VFX set.");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void APulseWithNoSpriteStillScalesRatherThanVanishing()
        {
            var go = new GameObject("RingPulseNoSprite");
            try
            {
                go.AddComponent<SpriteRenderer>(); // present, but no sprite assigned
                var pulse = go.AddComponent<GameFeelRingPulse>();
                pulse.finalRadius = 0.8f;
                pulse.lifetime = 100f; // see the case above: keeps Time.deltaTime from ending it
                Invoke(pulse, "Awake");
                SetPrivate(pulse, "elapsed", 99f);
                Invoke(pulse, "Update");

                // Nothing to normalise against, so the factor stays 1 and the old behaviour holds.
                // The alternative — dividing by zero, or refusing to scale — turns a missing sprite
                // into an invisible or infinite effect instead of a harmless one.
                Assert.That(go.transform.localScale.x, Is.EqualTo(0.8f).Within(0.01f),
                    "with no sprite to measure, finalRadius must pass through unchanged");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static Sprite MakeSprite(int px, float pixelsPerUnit)
        {
            var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
            var fill = new Color32[px * px];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(fill);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), pixelsPerUnit);
        }

        private static void Invoke(object target, string method)
        {
            var m = target.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(m, Is.Not.Null, $"{target.GetType().Name}.{method} is gone or renamed");
            m.Invoke(target, null);
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(f, Is.Not.Null, $"{target.GetType().Name}.{field} is gone or renamed");
            f.SetValue(target, value);
        }
    }
}
