using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins that a block renders at <c>targetWorldSize</c> no matter which sprite it is wearing.
    ///
    /// The scale is applied by <c>ApplyPresentationScale</c>, and every assignment site called it
    /// except <c>UpdateVisuals</c> — the one that swaps sprite on every damage-band change. Two
    /// things fell through that hole. <c>ApplyPresentationScale</c> returns early when the renderer
    /// has no sprite, so a block reaching Awake with a null sprite kept localScale 1 and then
    /// rendered the first band's art at its authored size: block_normal is 1254px at 100 ppu, i.e.
    /// **12.54 world units** on a 1-unit grid. And damage art is not guaranteed to share a native
    /// size with the normal sprite, so a future mismatch would have shown as blocks jumping when
    /// they cracked.
    ///
    /// It shipped because nothing measured RENDERED size after a band change. There were tests for
    /// which sprite each band picks, and tests for collider/visual agreement at construction, and
    /// between them sat the swap.
    ///
    /// Sizes are asserted through <c>SpriteRenderer.bounds</c> — what the camera sees — rather than
    /// through localScale, because the defect is a size on screen and scale is only one of the
    /// inputs to it.
    /// </summary>
    public class BlockRenderedSizeTests
    {
        private const float TargetSize = 1.0f;

        [Test]
        public void ABlockKeepsItsTargetSizeAcrossEveryDamageBand()
        {
            var go = new GameObject("BlockRenderedSize_Block");
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                var block = go.AddComponent<DestructibleBlock>();
                block.targetWorldSize = TargetSize;
                Awaken(block);

                // Three sprites at three DIFFERENT native sizes. Today's shipped trio happens to
                // match at 1254px, which is precisely why a missing rescale was invisible — the test
                // has to supply the disagreement the art does not currently have.
                var normal = MakeSprite(64);
                var cracked = MakeSprite(256);
                var heavy = MakeSprite(512);

                var data = ScriptableObject.CreateInstance<BlockData>();
                data.blockName = "SizeProbe";
                data.maxHP = 100f;
                data.normalSprite = normal;
                data.crackedSprite = cracked;
                data.heavilyCrackedSprite = heavy;
                data.blockColor = Color.white;

                block.ApplyBlockData(data);

                // Band 0. ApplyBlockData already scales, so this is the control: if it fails, the
                // problem is not the swap.
                AssertRendered(sr, "band 0 (intact)");

                // Band 1 at ratio <= 0.7, band 2 at <= 0.3 — see CastleSkinLibrary.ComputeDisplayBand.
                SetHp(block, 60f);
                AssertRendered(sr, "band 1 (cracked)");
                Assert.That(sr.sprite, Is.SameAs(cracked), "band 1 must be wearing the cracked art");

                SetHp(block, 20f);
                AssertRendered(sr, "band 2 (crumbling)");
                Assert.That(sr.sprite, Is.SameAs(heavy), "band 2 must be wearing the heavy art");

                // And back up, because the wear ratchet can lower the displayed band and a rescale
                // that only ever grows would pass everything above.
                SetHp(block, 95f);
                AssertRendered(sr, "band 0 again (repaired)");

                Object.DestroyImmediate(data);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ABlockThatStartsWithNoSpriteStillLandsAtTargetSize()
        {
            var go = new GameObject("BlockRenderedSize_LateSprite");
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                var block = go.AddComponent<DestructibleBlock>();
                block.targetWorldSize = TargetSize;
                Awaken(block);

                // The exact path that produced the reported rectangle: no sprite when the scale is
                // first computed, so ApplyPresentationScale returns early and localScale stays 1.
                // Whatever arrives next used to render at its authored size — 12.54 units for the
                // shipped block art.
                Assert.That(sr.sprite, Is.Null, "this case is about arriving with nothing to scale");

                var data = ScriptableObject.CreateInstance<BlockData>();
                data.blockName = "LateSpriteProbe";
                data.maxHP = 100f;
                data.normalSprite = null;
                data.crackedSprite = MakeSprite(1254);
                data.heavilyCrackedSprite = MakeSprite(1254);
                data.blockColor = Color.white;

                block.ApplyBlockData(data);
                SetHp(block, 50f); // crosses into band 1, where the first real sprite appears

                Assert.That(sr.sprite, Is.Not.Null, "band 1 must have put a sprite on the renderer");
                AssertRendered(sr, "first sprite to arrive after a null start");

                Object.DestroyImmediate(data);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        private static void AssertRendered(SpriteRenderer sr, string what)
        {
            Assert.That(sr.sprite, Is.Not.Null, $"{what}: no sprite to measure");
            float rendered = Mathf.Max(sr.bounds.size.x, sr.bounds.size.y);

            // 2% tolerance covers float division, not a size mistake: the failure mode here is
            // multiples — 12.54 against 1.00 — so anything near the target is the target.
            Assert.That(rendered, Is.EqualTo(TargetSize).Within(TargetSize * 0.02f),
                $"{what}: renders {rendered:F2} world units against a target of {TargetSize:F2}. "
                + $"That is {rendered / TargetSize:F1}x its grid cell. The scale is derived from the "
                + "sprite's own bounds, so a sprite swap without a rescale leaves the previous "
                + "sprite's scale — or, from a null start, no scale at all.");
        }

        /// <summary>
        /// Sets HP directly and redraws, so no damage side effects (debris, labels, telemetry) run.
        /// </summary>
        private static void SetHp(DestructibleBlock block, float hp)
        {
            block.currentHP = hp;

            var update = typeof(DestructibleBlock).GetMethod(
                "UpdateVisuals", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(update, Is.Not.Null, "UpdateVisuals is gone or renamed");
            update.Invoke(block, null);
        }

        /// <summary>
        /// Runs Awake by hand. EditMode's AddComponent does not, and Awake is where the block caches
        /// the SpriteRenderer it scales — without it ApplyBlockData sees a null renderer, skips the
        /// sprite assignment entirely, and the test measures nothing while appearing to.
        /// </summary>
        private static void Awaken(DestructibleBlock block)
        {
            var awake = typeof(DestructibleBlock).GetMethod(
                "Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null, "Awake is gone or renamed");
            awake.Invoke(block, null);
        }

        /// <summary>A square sprite of the given pixel size at 100 pixels-per-unit.</summary>
        private static Sprite MakeSprite(int px)
        {
            var tex = new Texture2D(px, px, TextureFormat.RGBA32, false);
            var fill = new Color32[px * px];
            for (int i = 0; i < fill.Length; i++) fill[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(fill);
            tex.Apply();
            // 100 ppu matches the shipped block art, so px/100 is the authored world size and the
            // numbers in the failure messages are the ones a reader would see in the importer.
            return Sprite.Create(tex, new Rect(0, 0, px, px), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
