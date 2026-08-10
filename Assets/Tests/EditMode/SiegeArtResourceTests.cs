using System.Collections.Generic;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the presentation contracts of the siege re-skin: the unit-launch affordance is a
    /// 새총 slingshot (not the old portal ring), and the keep the player defends visibly
    /// crumbles through three progressively-destroyed damage stages.
    ///
    /// These are presentation rules with no gameplay assertion behind them, so nothing else in
    /// the suite notices when the art silently stops resolving: GimmickFrameAnimator.TryAttach
    /// fails soft (returns null and leaves the procedural fallback on screen) and
    /// GimmickSpriteLibrary.Load returns null so callers keep their tinted-block fallback. A
    /// broken import therefore ships as a *visual* regression with a green suite — which is
    /// exactly what these tests exist to catch.
    /// </summary>
    [TestFixture]
    public sealed class SiegeArtResourceTests
    {
        /// <summary>GimmickFrameAnimator.TryAttach rejects any set with fewer than two frames
        /// and leaves the host on its static/procedural art, so two is the floor for "animated".</summary>
        private const int MinLoopFrames = 2;

        private const int SlingshotFrameCount = 6;
        private const int KeepAnimFrameCount = 4;
        private const int KeepStages = 3;

        /// <summary>Samples per axis for the coarse content signature (16x16 = 256 taps).</summary>
        private const int SignatureGrid = 16;

        // ---------------------------------------------------------------------------------
        // 1. The slingshot art exists and is a usable loop.
        // ---------------------------------------------------------------------------------

        [Test]
        public void SlingshotAnim_ResolvesEnoughFramesToAnimateInsteadOfFallingBackToTheProceduralRing()
        {
            Sprite[] frames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.SlingshotAnim);

            Assert.That(frames, Is.Not.Null,
                "The slingshot is the player's launch affordance; with no frame set at all the launcher "
                + "renders as the old procedural ring and the 새총 re-skin never reaches the screen.");
            Assert.That(frames.Length, Is.GreaterThanOrEqualTo(MinLoopFrames),
                "GimmickFrameAnimator.TryAttach refuses any set with fewer than two frames and fails soft, "
                + "so a one-frame slingshot would silently leave the procedural portal ring on screen "
                + "instead of reporting an error.");
            Assert.That(frames.Length, Is.EqualTo(SlingshotFrameCount),
                "The authored slingshot loop must import complete — a short set means frames were dropped "
                + "on import and the launch pull-back animation plays truncated.");
        }

        [Test]
        public void SlingshotAnim_FramesArriveInOrdinalNameOrderSoThePullBackPlaysForwards()
        {
            Sprite[] frames = RequireLoop(GimmickAnimLibrary.SlingshotAnim);

            for (int i = 1; i < frames.Length; i++)
            {
                Assert.That(
                    string.CompareOrdinal(frames[i - 1].name, frames[i].name),
                    Is.LessThan(0),
                    "Playback order IS file order: the animator walks the array start-to-end, so a set that "
                    + "is not strictly ascending by name plays the slingshot's pull-back and release out of "
                    + $"sequence (saw '{frames[i - 1].name}' before '{frames[i].name}').");
            }
        }

        // ---------------------------------------------------------------------------------
        // 2. Every frame is real art, not an empty texture.
        // ---------------------------------------------------------------------------------

        [Test]
        public void SiegeLoops_EveryFrameIsRealArtRatherThanAnEmptyTexture()
        {
            var keys = new List<string> { GimmickAnimLibrary.SlingshotAnim };
            for (int stage = 0; stage < KeepStages; stage++)
            {
                keys.Add(GimmickAnimLibrary.CastleKeepAnim(stage));
            }

            foreach (string key in keys)
            {
                Sprite[] frames = RequireLoop(key);

                for (int i = 0; i < frames.Length; i++)
                {
                    Assert.That(frames[i], Is.Not.Null,
                        $"A null frame inside '{key}' blanks the sprite renderer for one tick of the loop, "
                        + "so the launcher or keep flickers out of existence mid-animation.");
                    Assert.That(frames[i].texture, Is.Not.Null,
                        $"Frame '{frames[i].name}' of '{key}' carries no texture, so it draws nothing — a "
                        + "sprite that loads successfully but shows nothing is the failure mode this guards.");
                    Assert.That(frames[i].rect.width * frames[i].rect.height, Is.GreaterThan(0f),
                        $"Frame '{frames[i].name}' of '{key}' has zero drawable area. A silently-empty PNG "
                        + "still 'loads', so only a positive-area rect proves the frame can actually paint "
                        + "pixels instead of blanking the affordance on screen.");
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // 3. All three keep stages exist, both as animation sets and as stills.
        // ---------------------------------------------------------------------------------

        [Test]
        public void CastleKeep_EveryDamageStageShipsBothAnAnimatedLoopAndAStaticStill()
        {
            for (int stage = 0; stage < KeepStages; stage++)
            {
                Sprite[] frames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.CastleKeepAnim(stage));

                Assert.That(frames, Is.Not.Null,
                    $"Keep damage stage {stage} has no animated loop, so the keep freezes on static art the "
                    + "moment the castle crosses into that damage band.");
                Assert.That(frames.Length, Is.GreaterThanOrEqualTo(MinLoopFrames),
                    $"Keep damage stage {stage} must supply at least two frames or GimmickFrameAnimator "
                    + "refuses to animate it, leaving one stage conspicuously still while the others breathe.");

                Assert.That(
                    GimmickSpriteLibrary.Load(GimmickAnimLibrary.CastleKeepStill(stage)),
                    Is.Not.Null,
                    $"Keep damage stage {stage} has no static still. The still feeds the damage-state sprite "
                    + "slots used whenever the loop is suspended, so without it the keep falls back to the "
                    + "tinted stone block and stops reading as a castle at that stage.");
            }
        }

        // ---------------------------------------------------------------------------------
        // 4. The three keep stages are genuinely DIFFERENT art. (Load-bearing.)
        // ---------------------------------------------------------------------------------

        [Test]
        public void CastleKeepStills_ThreeStagesAreDifferentArtNotThreeCopiesOfOneImage()
        {
            var stills = new Sprite[KeepStages];
            var signatures = new ulong[KeepStages];
            var sampled = new bool[KeepStages];

            for (int stage = 0; stage < KeepStages; stage++)
            {
                stills[stage] = GimmickSpriteLibrary.Load(GimmickAnimLibrary.CastleKeepStill(stage));
                Assert.That(stills[stage], Is.Not.Null,
                    $"Keep damage stage {stage} must resolve before its art can be compared against the "
                    + "other stages.");
                sampled[stage] = TryComputeContentSignature(stills[stage], out signatures[stage]);
            }

            for (int a = 0; a < KeepStages; a++)
            {
                for (int b = a + 1; b < KeepStages; b++)
                {
                    Assert.That(ReferenceEquals(stills[a], stills[b]), Is.False,
                        $"Keep stages {a} and {b} resolve to the same sprite asset. The three stages must be "
                        + "visibly different art or the castle never appears to crumble and the player gets "
                        + "no read on how close the keep is to falling.");

                    if (sampled[a] && sampled[b])
                    {
                        Assert.That(signatures[a], Is.Not.EqualTo(signatures[b]),
                            $"Keep stages {a} and {b} paint identical pixels across the sampled grid. Two "
                            + "distinct assets holding the same picture defeat the whole feature: the keep "
                            + "must be visibly more destroyed at each stage, not merely a different file.");
                    }
                    else
                    {
                        Assert.That(stills[a].rect.size, Is.Not.EqualTo(stills[b].rect.size),
                            $"LIMITATION — pixel content for keep stages {a}/{b} could not be sampled (the "
                            + "imported texture is not readable and the source PNG could not be decoded from "
                            + "disk), so this pair is only checked for differing sprite dimensions. A "
                            + "same-size repaint would slip past this weaker check; the stages must still be "
                            + "visibly different art or the castle never appears to crumble.");
                    }
                }
            }
        }

        // ---------------------------------------------------------------------------------
        // 5. Stage index clamping.
        // ---------------------------------------------------------------------------------

        [Test]
        public void CastleKeepKeys_ClampOutOfRangeStagesSoABadBandNeverAsksForArtThatWasNeverAuthored()
        {
            Assert.That(GimmickAnimLibrary.CastleKeepAnim(-5),
                Is.EqualTo(GimmickAnimLibrary.CastleKeepAnim(0)),
                "A band that underflows must resolve to the intact keep rather than a missing-resource key: "
                + "a null frame set drops the keep back to the procedural block mid-match.");
            Assert.That(GimmickAnimLibrary.CastleKeepAnim(99),
                Is.EqualTo(GimmickAnimLibrary.CastleKeepAnim(2)),
                "A band that overflows must resolve to the most destroyed keep rather than a missing-resource "
                + "key, so a miscomputed band degrades the picture instead of erasing it.");
            Assert.That(GimmickAnimLibrary.CastleKeepStill(-5),
                Is.EqualTo(GimmickAnimLibrary.CastleKeepStill(0)),
                "The still key must clamp with the loop key, or a bad band animates one stage while the "
                + "suspended damage-state slot shows nothing at all.");
            Assert.That(GimmickAnimLibrary.CastleKeepStill(99),
                Is.EqualTo(GimmickAnimLibrary.CastleKeepStill(2)),
                "The still key must clamp with the loop key, or a bad band animates one stage while the "
                + "suspended damage-state slot shows nothing at all.");

            var animKeys = new[]
            {
                GimmickAnimLibrary.CastleKeepAnim(0),
                GimmickAnimLibrary.CastleKeepAnim(1),
                GimmickAnimLibrary.CastleKeepAnim(2),
            };
            var stillKeys = new[]
            {
                GimmickAnimLibrary.CastleKeepStill(0),
                GimmickAnimLibrary.CastleKeepStill(1),
                GimmickAnimLibrary.CastleKeepStill(2),
            };

            Assert.That(animKeys, Is.Unique,
                "Clamping must not collapse the in-range stages onto one key — each damage stage has to "
                + "address its own art or the keep stops changing as it takes damage.");
            Assert.That(stillKeys, Is.Unique,
                "Clamping must not collapse the in-range stages onto one key — each damage stage has to "
                + "address its own still or the suspended keep looks identical at full health and near-ruin.");

            Assert.That(GimmickSpriteLibrary.Load(GimmickAnimLibrary.CastleKeepStill(99)), Is.Not.Null,
                "The clamped key must resolve to art that actually exists; clamping to a key nobody authored "
                + "would trade a missing-resource crash for a silently invisible keep.");
        }

        // ---------------------------------------------------------------------------------
        // 6. HP ratio maps to the intended keep stage.
        // ---------------------------------------------------------------------------------

        [Test]
        public void ComputeDisplayBand_KeepBreaksExactlyAtTheDocumentedHealthBoundaries()
        {
            Assert.That(CastleSkinLibrary.ComputeDisplayBand(1f, 0), Is.EqualTo(0),
                "An untouched keep must read as intact; showing damage the player has not inflicted destroys "
                + "the only at-a-glance signal of how the siege is going.");
            Assert.That(CastleSkinLibrary.ComputeDisplayBand(0.7001f, 0), Is.EqualTo(0),
                "Just above the first boundary the keep must still read intact — this is the exact health at "
                + "which the player first sees the castle break, so it must not drift earlier.");
            Assert.That(CastleSkinLibrary.ComputeDisplayBand(0.7f, 0), Is.EqualTo(1),
                "The first boundary is inclusive: reaching it must flip the keep to battered art, so the "
                + "player gets feedback the moment the threshold is crossed rather than one hit later.");
            Assert.That(CastleSkinLibrary.ComputeDisplayBand(0.3001f, 0), Is.EqualTo(1),
                "Just above the second boundary the keep must still read battered — promoting it to near-ruin "
                + "early would tell the player the castle is about to fall when it is not.");
            Assert.That(CastleSkinLibrary.ComputeDisplayBand(0.3f, 0), Is.EqualTo(2),
                "The second boundary is inclusive: reaching it must flip the keep to near-ruin art, the "
                + "player's last warning before the base is lost.");
            Assert.That(CastleSkinLibrary.ComputeDisplayBand(0f, 0), Is.EqualTo(2),
                "A dead keep must show the most destroyed art; anything less contradicts the loss state on "
                + "screen.");
        }

        [Test]
        public void ComputeDisplayBand_WearFloorOnlyRaisesTheDisplayedStageAndNeverLeavesTheAuthoredRange()
        {
            float[] ratios = { 1f, 0.85f, 0.7001f, 0.7f, 0.5f, 0.3001f, 0.3f, 0.1f, 0f };
            int[] wearFloors = { -3, 0, 1, 2, 7 };

            foreach (float ratio in ratios)
            {
                int unworn = CastleSkinLibrary.ComputeDisplayBand(ratio, 0);

                foreach (int wearFloor in wearFloors)
                {
                    int band = CastleSkinLibrary.ComputeDisplayBand(ratio, wearFloor);

                    Assert.That(band, Is.GreaterThanOrEqualTo(unworn),
                        $"Castle-wide wear (floor {wearFloor} at ratio {ratio}) may only make the keep look "
                        + "MORE broken. Letting it lower the stage would visually repair a castle the player "
                        + "already damaged.");
                    Assert.That(band, Is.InRange(0, 2),
                        $"A wear floor of {wearFloor} must still land on an authored stage; a band outside "
                        + "0..2 asks for keep art nobody drew.");
                }
            }

            Assert.That(CastleSkinLibrary.ComputeDisplayBand(1f, 2), Is.EqualTo(2),
                "A fully worn castle must show near-ruin keep art even at full keep HP — the wear floor is "
                + "the mechanism by which late-match attrition reads on the base.");
            Assert.That(CastleSkinLibrary.ComputeDisplayBand(0f, 0), Is.EqualTo(2),
                "With no wear applied the keep's own health must still be able to drive it to near-ruin, or "
                + "the floor becomes the only thing that ever breaks the castle.");
        }

        // ---------------------------------------------------------------------------------
        // 7. The loop math is stable.
        // ---------------------------------------------------------------------------------

        [Test]
        public void LoopFrameAt_WrapsForeverInsideBothTheSlingshotAndKeepFrameCounts()
        {
            const float frameSeconds = 0.125f; // 8 fps — the animator's default cadence.
            int[] frameCounts = { SlingshotFrameCount, KeepAnimFrameCount };

            foreach (int frameCount in frameCounts)
            {
                Assert.That(GimmickFrameAnimator.LoopFrameAt(0f, frameSeconds, frameCount), Is.EqualTo(0),
                    $"A {frameCount}-frame loop must open on the authored first frame; TryAttach paints "
                    + "frames[0] on attach, so starting anywhere else pops the sprite on the first tick.");

                for (int i = 0; i < frameCount; i++)
                {
                    Assert.That(
                        GimmickFrameAnimator.LoopFrameAt(i * frameSeconds, frameSeconds, frameCount),
                        Is.EqualTo(i),
                        $"A {frameCount}-frame loop must advance exactly one frame per interval, or the "
                        + "animation plays at the wrong speed or skips authored frames.");
                }

                Assert.That(
                    GimmickFrameAnimator.LoopFrameAt(frameCount * frameSeconds, frameSeconds, frameCount),
                    Is.EqualTo(0),
                    $"A {frameCount}-frame loop must return to its first frame after one full cycle so the "
                    + "idle animation reads as seamless rather than restarting mid-pose.");

                float[] hostileElapsed = { 100000f, 987654.5f, -0.125f, -7.3f, -100000f };
                foreach (float elapsed in hostileElapsed)
                {
                    Assert.That(
                        GimmickFrameAnimator.LoopFrameAt(elapsed, frameSeconds, frameCount),
                        Is.InRange(0, frameCount - 1),
                        $"Elapsed {elapsed} on a {frameCount}-frame loop must still index a real frame. The "
                        + "result feeds frames[index] directly every Update, so a negative or overrun index "
                        + "throws and kills the gimmick's rendering for the rest of the match.");
                }
            }
        }

        [Test]
        public void LoopFrameAt_DegenerateTimingReturnsASafeFrameInsteadOfDividingByZero()
        {
            Assert.That(GimmickFrameAnimator.LoopFrameAt(3f, 0.125f, 0), Is.EqualTo(0),
                "A gimmick whose art failed to load has no frames; the loop must yield a safe index rather "
                + "than reaching into an empty array every Update.");
            Assert.That(GimmickFrameAnimator.LoopFrameAt(3f, 0f, SlingshotFrameCount), Is.EqualTo(0),
                "A zero frame duration must not divide by zero — a misconfigured fps must degrade to a still "
                + "slingshot, not an exception storm in Update.");
            Assert.That(GimmickFrameAnimator.LoopFrameAt(3f, -1f, SlingshotFrameCount), Is.EqualTo(0),
                "A negative frame duration must degrade to a safe index rather than driving the loop "
                + "backwards out of the frame array.");
        }

        // ---------------------------------------------------------------------------------
        // 8. The legacy portal key still resolves separately from the slingshot.
        // ---------------------------------------------------------------------------------

        [Test]
        public void SlingshotArt_DidNotOverwriteTheLegacyPortalRingTheRuntimeStillReferences()
        {
            Assert.That(GimmickAnimLibrary.SlingshotAnim, Is.Not.EqualTo(GimmickAnimLibrary.LaunchGateAnim),
                "The slingshot and the superseded portal ring must stay separate keys; collapsing them would "
                + "make the documented fallback unreachable and leave callers that still ask for the gate "
                + "with no art at all.");

            Sprite[] slingshot = RequireLoop(GimmickAnimLibrary.SlingshotAnim);
            Sprite[] legacy = RequireLoop(GimmickAnimLibrary.LaunchGateAnim);

            var legacyNames = new HashSet<string>();
            foreach (Sprite frame in legacy)
            {
                legacyNames.Add(frame.name);
            }

            foreach (Sprite frame in slingshot)
            {
                Assert.That(legacyNames.Contains(frame.name), Is.False,
                    $"Frame '{frame.name}' appears in both the slingshot and legacy gate sets, so the two "
                    + "keys resolve to the same folder. The runtime still falls back to the gate, and a "
                    + "shared set means that fallback can never differ from what it is falling back from.");
            }

            if (TryComputeContentSignature(slingshot[0], out ulong slingshotSignature)
                && TryComputeContentSignature(legacy[0], out ulong legacySignature))
            {
                Assert.That(slingshotSignature, Is.Not.EqualTo(legacySignature),
                    "The slingshot's opening frame paints the same picture as the legacy portal ring's. The "
                    + "새총 art must not have been written over the fallback the runtime still points at, or "
                    + "the 'fallback' silently shows the very art it is supposed to replace.");
            }
            else
            {
                Assert.That(ReferenceEquals(slingshot[0], legacy[0]), Is.False,
                    "LIMITATION — pixel content could not be sampled for the slingshot and legacy gate "
                    + "opening frames (textures not readable and the source PNGs could not be decoded from "
                    + "disk), so only reference-distinctness is checked here. Art copied between the two "
                    + "folders would slip past this weaker check; the slingshot must not have overwritten "
                    + "the fallback the runtime still references.");
            }
        }

        // ---------------------------------------------------------------------------------
        // 9. The animated sets must be import-correct at rest — no editor-only rescue.
        // ---------------------------------------------------------------------------------

        [Test]
        public void AnimFrameSets_ResolveThroughTheRuntimeLoaderAloneWithNoEditorOnlySelfHeal()
        {
            var keys = new List<string> { GimmickAnimLibrary.SlingshotAnim, GimmickAnimLibrary.LaunchGateAnim };
            for (int stage = 0; stage < KeepStages; stage++)
            {
                keys.Add(GimmickAnimLibrary.CastleKeepAnim(stage));
            }

            foreach (string key in keys)
            {
                // Resources.LoadAll is the whole of the animated path: unlike the single-sprite
                // library, GimmickAnimLibrary has no editor-side repair to fall back on, and any
                // such repair is compiled out of a player build anyway. Calling the runtime API
                // directly is therefore the only way to prove the art is import-correct at rest
                // rather than being rescued by the Editor at load time.
                Sprite[] runtimeFrames = Resources.LoadAll<Sprite>($"Gimmicks/{key}");

                Assert.That(runtimeFrames, Is.Not.Null,
                    $"'{key}' resolves nothing at all through the runtime loader, so a player build has no "
                    + "art for it whatsoever.");
                Assert.That(runtimeFrames.Length, Is.GreaterThanOrEqualTo(MinLoopFrames),
                    $"'{key}' does not resolve as sprites through the runtime loader alone. The stills can "
                    + "survive a bad import because GimmickSpriteLibrary.Load repairs the texture type in "
                    + "the Editor, but the animated path has no such rescue and that rescue does not exist "
                    + "in a player build — so art that only loads in-Editor ships as a launcher stuck on "
                    + "the procedural ring and a keep that never animates.");
            }
        }

        // ---------------------------------------------------------------------------------
        // Helpers
        // ---------------------------------------------------------------------------------

        /// <summary>Loads a frame set and fails the test unless it is animatable, so callers can
        /// iterate it without the loop body silently never running on an empty set.</summary>
        private static Sprite[] RequireLoop(string key)
        {
            Sprite[] frames = GimmickAnimLibrary.LoadFrames(key);

            Assert.That(frames, Is.Not.Null,
                $"'{key}' resolves no frames, so every gimmick that asks for it falls back to procedural art "
                + "with no error surfaced anywhere.");
            Assert.That(frames.Length, Is.GreaterThanOrEqualTo(MinLoopFrames),
                $"'{key}' must supply at least two frames — below that GimmickFrameAnimator.TryAttach fails "
                + "soft and the affordance never animates.");

            return frames;
        }

        /// <summary>
        /// Coarse content signature: samples the sprite's rect on a fixed normalised grid and folds
        /// the RGBA bytes into an FNV-1a accumulator. Two sprites showing the same picture produce
        /// the same value, so an inequality assertion on this proves the art genuinely differs.
        ///
        /// The project's textures import non-readable, so GetPixel on the imported texture is
        /// unavailable; the source PNG is decoded from disk instead (the same escape hatch
        /// HiggsfieldSpriteLibraryTests uses). Sampling is done in normalised sprite-rect space so
        /// both paths address the same region even when the importer resized the texture. Returns
        /// false only when neither path can produce pixels, which callers must surface rather than
        /// quietly skip.
        /// </summary>
        private static bool TryComputeContentSignature(Sprite sprite, out ulong signature)
        {
            signature = 0UL;
            if (sprite == null || sprite.texture == null) return false;

            Texture2D imported = sprite.texture;
            if (imported.width <= 0 || imported.height <= 0) return false;

            Texture2D decoded = null;
            try
            {
                Texture2D source = imported;
                if (!source.isReadable)
                {
                    decoded = DecodeSourceImage(imported);
                    if (decoded == null) return false;
                    source = decoded;
                }

                Rect rect = sprite.rect;
                if (rect.width <= 0f || rect.height <= 0f) return false;

                float originU = rect.x / imported.width;
                float originV = rect.y / imported.height;
                float spanU = rect.width / imported.width;
                float spanV = rect.height / imported.height;

                ulong hash = 14695981039346656037UL;
                for (int gy = 0; gy < SignatureGrid; gy++)
                {
                    for (int gx = 0; gx < SignatureGrid; gx++)
                    {
                        float u = originU + spanU * ((gx + 0.5f) / SignatureGrid);
                        float v = originV + spanV * ((gy + 0.5f) / SignatureGrid);
                        int px = Mathf.Clamp((int)(u * source.width), 0, source.width - 1);
                        int py = Mathf.Clamp((int)(v * source.height), 0, source.height - 1);

                        Color32 sample = source.GetPixel(px, py);
                        hash = Fold(hash, sample.r);
                        hash = Fold(hash, sample.g);
                        hash = Fold(hash, sample.b);
                        hash = Fold(hash, sample.a);
                    }
                }

                signature = hash;
                return true;
            }
            finally
            {
                if (decoded != null) Object.DestroyImmediate(decoded);
            }
        }

        private static ulong Fold(ulong hash, byte value) => (hash ^ value) * 1099511628211UL;

        /// <summary>Decodes the sprite's source PNG off disk into a readable texture. Import
        /// settings cannot make this unavailable, which is why it is preferred over GetPixel.</summary>
        private static Texture2D DecodeSourceImage(Texture2D imported)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(imported);
            if (string.IsNullOrEmpty(assetPath)) return null;

            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            string absolutePath = System.IO.Path.Combine(projectRoot, assetPath);
            if (!System.IO.File.Exists(absolutePath)) return null;

            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(decoded, System.IO.File.ReadAllBytes(absolutePath), false))
            {
                Object.DestroyImmediate(decoded);
                return null;
            }

            return decoded;
        }
    }
}
