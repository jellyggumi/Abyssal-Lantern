using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins what may be used as particle art, and pins the frame-size consistency the effect
    /// player depends on.
    ///
    /// Both defects here were found by capturing the frame a block is struck on and measuring it,
    /// not by reading the code. The measurement found zero pure-white pixels and a cluster of pale
    /// grey (210,209,207) at the impact — which is what `face_s0`, a 512x512 brick-mortar overlay
    /// that is almost entirely white, looks like once it is shrunk to a 2-12px particle.
    /// `_workspace/current/qa/evidence/impact-vfx/`
    /// </summary>
    public class ImpactParticleArtTests
    {
        [Test]
        public void RealParticleArt_IsAccepted()
        {
            var ember = EffectSpriteLibrary.LoadParticleSprite(EffectSpriteLibrary.ParticleEmber);
            Assert.IsNotNull(ember, "the ember particle art must load, or every burst falls back further");
            Assert.IsTrue(GameFeelVfx.IsUsableParticleSprite(ember),
                "the project's own particle art must pass its own vetting");
        }

        /// <summary>
        /// The exact sprite that caused the report. A block's facade overlay is not particle art:
        /// it is a full-tile pattern of white with hairline mortar, and a particle-sized copy of it
        /// is a white smudge.
        /// </summary>
        [Test]
        public void TheBlockFacadeOverlay_IsRefused()
        {
            var facade = Resources.Load<Sprite>("CastleSkin/face_s0");
            Assert.IsNotNull(facade, "precondition: the facade sprite this guards against must exist");
            Assert.IsFalse(GameFeelVfx.IsUsableParticleSprite(facade),
                "face_s0 is a 512x512 brick-mortar overlay, nearly all white. Handed to a particle "
                + "system it renders as the pale grey smudge measured at the impact point");
        }

        [Test]
        public void NullSprite_IsRefusedRatherThanPassedThrough()
        {
            Assert.IsFalse(GameFeelVfx.IsUsableParticleSprite(null),
                "a null must take the fallback path explicitly, not be treated as usable");
        }

        /// <summary>
        /// Every sprite the block renders must be refused, not just the one that was reported.
        /// The four call sites hand over whatever the renderer currently holds, so the damage-state
        /// variants are equally reachable.
        /// </summary>
        [Test]
        public void EveryCastleSkinSprite_IsRefused()
        {
            var skins = Resources.LoadAll<Sprite>("CastleSkin");
            Assert.IsNotEmpty(skins, "precondition: the castle skin folder should hold sprites");

            var accepted = skins.Where(GameFeelVfx.IsUsableParticleSprite).Select(s => s.name).ToArray();
            Assert.IsEmpty(accepted,
                "no castle-skin sprite is particle art. Accepted by mistake: " + string.Join(", ", accepted));
        }

        /// <summary>
        /// Frames of one effect should share a size, and this records how far the shipped art is
        /// from that.
        ///
        /// <see cref="FrameAnimEffect"/> swaps frames into a single SpriteRenderer, so a frame of a
        /// different pixel size changes the drawn size mid-playback — the effect visibly jumps. The
        /// capture probe printed the sizes side by side and found this on FIVE strips, not the one
        /// that had been noticed: fx_sparkle is the worst at 77x77 followed by three 256s, a 3.3x
        /// jump on the first frame.
        ///
        /// Pinned as an exact baseline rather than as "must be empty", because fixing it means
        /// regenerating art and that is a request, not a code change
        /// (`design/graphics-needed.md`). An exact list fails both ways: a NEW offender fails it,
        /// and repairing one of these also fails it — which is the point. The list is the to-do,
        /// and shrinking it is the edit that removes an entry.
        /// </summary>
        [Test]
        public void EffectFrames_SizeMismatchesMatchTheRecordedBaseline()
        {
            string[] keys =
            {
                EffectSpriteLibrary.Spark,
                EffectSpriteLibrary.Dust,
                EffectSpriteLibrary.Sparkle,
                EffectSpriteLibrary.Spawn,
                EffectSpriteLibrary.Eruption,
                EffectSpriteLibrary.Petals,
                EffectSpriteLibrary.Arcane,
                EffectSpriteLibrary.MuzzleBlast,
            };

            // Measured 2026-08-14. Each entry is an effect whose frames do not all share a size.
            // fx_arcane joined this list only after its importer was repaired — it was declared,
            // present on disk, and imported as textureType Default, so it loaded as zero sprites
            // and its mismatch was invisible. Fixing one defect exposed another.
            //
            // fx_spark LEFT the list on 2026-08-19: its odd first frame was redrawn at 256x256 to
            // match its three siblings, so the strip no longer jumps on frame one. This test is the
            // only reason that repair is provable rather than asserted — it failed on the redraw and
            // named the effect that had changed.
            var knownOffenders = new[]
            {
                EffectSpriteLibrary.Dust,       // 190x190 then 256x256 x3
                EffectSpriteLibrary.Sparkle,    // 77x77 then 256x256 x3 — the worst jump
                EffectSpriteLibrary.Eruption,   // 545x639 vs 443x640
                EffectSpriteLibrary.Petals,     // 368x640 vs siblings
                EffectSpriteLibrary.Arcane,     // surfaced by the importer repair
            };

            var offenders = new System.Collections.Generic.List<string>();
            foreach (var key in keys)
            {
                var frames = EffectSpriteLibrary.LoadFrames(key);
                if (frames == null || frames.Length < 2) continue;

                var first = frames[0].rect.size;
                foreach (var f in frames)
                {
                    if (f.rect.size != first)
                    {
                        offenders.Add(key);
                        break;
                    }
                }
            }

            CollectionAssert.AreEquivalent(knownOffenders, offenders,
                "the set of effects with mismatched frame sizes changed. If an effect was repaired, "
                + "remove it from knownOffenders; if a new one appeared, its first frame is a "
                + "different pixel size from its siblings and the effect will jump mid-play. "
                + "Found: " + string.Join(", ", offenders));
        }

        /// <summary>
        /// A declared effect key must load frames — and exactly one currently does not.
        ///
        /// <c>FrameAnimEffect.Spawn</c> soft-fails on a missing strip: it returns null and draws
        /// nothing. So a declared-but-unloadable effect is silent rather than loud, which is how two
        /// of them survived. This test found both:
        ///
        /// - **fx_muzzle** (cannon muzzle flash, built by task #19) and **fx_arcane** had frames on
        ///   disk imported as <c>textureType: Default</c>, so <c>Resources.LoadAll&lt;Sprite&gt;</c>
        ///   returned an empty array and both effects were invisible. Repaired by rewriting their
        ///   importer settings — the same failure task #17 hit, and the same fix.
        /// - **fx_frost** (Stage3 frost vents) genuinely has zero files. That is an art request, not
        ///   a code fix, so it is pinned as a known gap: when the art lands this test fails and the
        ///   entry gets removed, which is the record that the gap closed.
        /// </summary>
        [Test]
        public void OnlyTheKnownArtGapIsMissingItsFrames()
        {
            (string key, string usedBy)[] declared =
            {
                (EffectSpriteLibrary.Spark, "block damage"),
                (EffectSpriteLibrary.Dust, "collapse"),
                (EffectSpriteLibrary.Sparkle, "runes and gates"),
                (EffectSpriteLibrary.Spawn, "spawns"),
                (EffectSpriteLibrary.Eruption, "vents"),
                (EffectSpriteLibrary.Petals, "petal vents"),
                (EffectSpriteLibrary.Frost, "Stage3 frost vents"),
                (EffectSpriteLibrary.Arcane, "arcane gates"),
                (EffectSpriteLibrary.MuzzleBlast, "cannon muzzle"),
            };

            // Every declared effect must load. This array was `{ Frost }` while fx_frost was a key
            // with no files; the six frames landed 2026-08-19 and EruptionVentGimmick now spawns
            // them, so the gap is closed and the array is empty. Emptying it is the point: a new
            // entry here would mean art regressed to absent, and the assertion below catches that.
            var knownGaps = new string[0];

            var missing = declared
                .Where(d => (EffectSpriteLibrary.LoadFrames(d.key)?.Length ?? 0) == 0)
                .Select(d => d.key)
                .ToArray();

            CollectionAssert.AreEquivalent(knownGaps, missing,
                "the set of effects that fail to load changed. A NEW entry means art is absent or "
                + "imported as textureType Default, and the effect is playing silently — check the "
                + "importer before assuming the art is missing. A REMOVED entry means the gap "
                + "closed; delete it from knownGaps. Found: " + string.Join(", ", missing)
                + ". Art request: _workspace/current/design/graphics-needed.md");
        }
    }
}
