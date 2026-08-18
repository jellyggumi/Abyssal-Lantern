using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Walks every image under <c>Assets/Resources</c> and proves the RUNTIME loader hands back a
    /// Sprite for it. Companion to <see cref="SiegeArtResourceTests"/>, deliberately not a
    /// replacement: that fixture iterates the keys the sprite libraries DECLARE, and a test that
    /// iterates declared keys is structurally blind to an asset nobody declared. This one iterates
    /// the DISK.
    ///
    /// THE DEFECT THIS EXISTS TO CATCH, which has now shipped four times.
    /// A PNG imports with `textureType: 0` (Default) and `spriteMode: 0` instead of
    /// `textureType: 8` (Sprite) / `spriteMode: 1`. The file is on disk, the art is colourful,
    /// the editor often looks fine — but `Resources.Load&lt;Sprite&gt;` / `LoadAll&lt;Sprite&gt;`
    /// return null / an empty array, every caller takes its null branch, and the null branch of the
    /// particle path lands on <see cref="GameFeelVfx.GetDefaultParticleTexture"/>: a
    /// `Color(1f, 1f, 1f, alpha)` radial blob. The player sees a PURE WHITE CIRCLE.
    /// Known instances: `fx_muzzle`, `fx_arcane`, `GeneratedExplosionFrames/*` (reported as
    /// "explosion shows up white"), and `Webtoon/panel-*` — the last of which was found BY this
    /// test, because it was never in any library's key list.
    ///
    /// WHY THE ASSERTION IS ON THE LOADER, NOT ON THE .meta.
    /// The contract is "the runtime gets a sprite", not "the importer file contains a given
    /// number". Meta parsing is used only to enrich failure messages; nothing here passes or fails
    /// on it, so a future importer format change degrades the diagnostics and never the coverage.
    ///
    /// HOW THE NON-SPRITE EXEMPTION IS DERIVED (not hardcoded).
    /// Some images are legitimately NOT sprites: `Backgrounds/*` is read by
    /// `GameManager.GetStageBackgroundSprite` through `Resources.Load&lt;Texture2D&gt;` and wrapped
    /// with `Sprite.Create` by hand, so Default is the CORRECT import for it and demanding a Sprite
    /// there would be a false alarm. Rather than hardcode that folder, the exemption is derived:
    /// <see cref="DeriveTextureOnlyFolders"/> scans the runtime sources for
    /// `Resources.Load&lt;Texture2D&gt;` call sites and harvests the resource-path literals around
    /// them. An image is excused only when BOTH hold:
    ///   1. its folder was harvested from such a call site, and
    ///   2. `Resources.Load&lt;Texture2D&gt;` on its own path actually returns a texture.
    /// The scan is FAIL-CLOSED in the direction that matters: if it harvests nothing, every image
    /// must load as a Sprite and the suite gets noisier, never quieter. It can only ever excuse a
    /// folder that runtime code demonstrably reads as a raw texture. A whitelist would instead have
    /// to be edited by the same person who adds the next unreadable folder — which is precisely how
    /// this defect survived three times.
    ///
    /// Scanning source to BUILD the exemption set is not the same as asserting on source text: no
    /// assertion here inspects source, and a rename inside `GameManager` cannot make a broken asset
    /// pass — at worst it widens what must load as a Sprite.
    /// </summary>
    [TestFixture]
    public sealed class ResourceSpriteImportTests
    {
        /// <summary>Extensions Unity routes through the texture importer, i.e. the files that can
        /// carry the Default-instead-of-Sprite defect. Both are present under Resources today
        /// (238 .png, 11 .jpg) and the .jpg set is exactly where the fourth instance lived.</summary>
        private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg" };

        /// <summary>`ParticleSystem.textureSheetAnimation` in Sprites mode needs a second frame
        /// before there is a sequence to play at all, so two is the floor for "animated". Deliberately
        /// a floor and not the count on disk: asserting the exact file count would pin bookkeeping
        /// rather than behaviour and would fail on an intentional re-author.</summary>
        private const int MinAnimationFrames = 2;

        /// <summary>Saturation at or below this reads as greyscale. The default particle texture is
        /// literally `Color(1,1,1,alpha)`, so its true value is 0.</summary>
        private const float ColourlessSaturation = 0.02f;

        /// <summary>Sampling grid for the mean-saturation probe (32x32 = 1024 taps).</summary>
        private const int SaturationGrid = 32;

        [SetUp]
        public void ClearLoaderCaches()
        {
            // ExplosionFrames memoises its result, so without this a test could pass on a sibling
            // test's cached array instead of on an actual load.
            ExplosionFrames.ClearCache();
        }

        // ---------------------------------------------------------------------------------
        // 1. Every image under Resources resolves through the runtime sprite loader.
        // ---------------------------------------------------------------------------------

        [Test]
        public void EveryResourceImage_ResolvesAsASpriteThroughTheRuntimeLoaderOrIsProvablyReadAsARawTexture()
        {
            string resourcesRoot = Path.Combine(Application.dataPath, "Resources");
            Assert.That(Directory.Exists(resourcesRoot), Is.True,
                $"'{resourcesRoot}' does not exist, so this test would walk nothing and pass vacuously "
                + "while every asset in the build went unchecked.");

            List<string> images = FindImagesUnder(resourcesRoot);
            Assert.That(images, Is.Not.Empty,
                "Found no images under Assets/Resources. Either the art is gone or this walk is broken; "
                + "either way an empty walk means this test is asserting nothing.");

            HashSet<string> textureOnlyFolders = DeriveTextureOnlyFolders();
            var failures = new List<string>();

            foreach (string absolutePath in images)
            {
                string resourcePath = ToResourcePath(resourcesRoot, absolutePath);

                // The contract, exercised exactly as shipping code exercises it. Both entry points
                // are tried because a single-sprite import answers Load<Sprite> while a sliced sheet
                // only answers LoadAll<Sprite>; either one returning art means the asset is usable.
                var single = Resources.Load<Sprite>(resourcePath);
                var sliced = Resources.LoadAll<Sprite>(resourcePath);
                if (single != null || (sliced != null && sliced.Length > 0)) continue;

                // Not a sprite. Excused only if runtime code demonstrably reads this folder as a raw
                // texture AND the raw read genuinely succeeds.
                string folder = FolderOf(resourcePath);
                if (folder.Length > 0 && textureOnlyFolders.Contains(folder)
                    && Resources.Load<Texture2D>(resourcePath) != null)
                {
                    continue;
                }

                failures.Add(DescribeFailure(absolutePath, resourcePath, folder, textureOnlyFolders));
            }

            Assert.That(failures, Is.Empty,
                "These images are on disk but the runtime sprite loader hands back nothing for them, so "
                + "every caller takes its null branch. On the particle path that branch reaches "
                + "GameFeelVfx.GetParticleMaterial(null) -> GetDefaultParticleTexture(), which is "
                + "Color(1f,1f,1f,alpha): the player sees a PURE WHITE CIRCLE where the art should be. "
                + "This is the defect that shipped as fx_muzzle, then fx_arcane, then the white "
                + "explosion. Fix the import (textureType: 8 / spriteMode: 1), do not add an "
                + $"exemption.\n  {string.Join("\n  ", failures)}\n");
        }

        // ---------------------------------------------------------------------------------
        // 2. The explosion frames, pinned by name — this is the asset the player reported.
        // ---------------------------------------------------------------------------------

        [Test]
        public void ExplosionFrames_LoadAsSpritesThroughTheOneRuntimePathInsteadOfCollapsingToTheWhiteBlob()
        {
            Sprite[] frames = ExplosionFrames.Load();

            Assert.That(frames, Is.Not.Null,
                "ExplosionFrames.Load() promises an empty array rather than null so callers can branch "
                + "on length; a null here breaks that contract and throws inside the configurator.");
            Assert.That(frames.Length, Is.GreaterThanOrEqualTo(MinAnimationFrames),
                $"'{ExplosionFrames.ResourceFolder}' resolves {frames.Length} sprite(s) through the "
                + "runtime loader. ExplosionEffectConfigurator.Awake only builds a texture sheet when "
                + "this array is non-empty; below that it falls through to "
                + "GetParticleMaterial(null) -> GetDefaultParticleTexture(), a white radial blob under "
                + "a white startColor. That is the exact 'the explosion shows up white' report, and it "
                + "is what a textureType: 0 / spriteMode: 0 import produces even though the PNGs are "
                + "present and colourful.");

            for (int i = 0; i < frames.Length; i++)
            {
                Assert.That(frames[i], Is.Not.Null,
                    $"Frame {i} of the explosion is null, so AddSprite feeds a hole into the texture "
                    + "sheet and the explosion blinks out for one frame of its playback.");
                Assert.That(frames[i].texture, Is.Not.Null,
                    $"Frame {i} ('{frames[i].name}') loaded as a Sprite with no texture behind it — it "
                    + "renders as nothing at all, which the length check above cannot see.");
            }
        }

        [Test]
        public void ExplosionFrames_ArriveInOrdinalNameOrderSoTheBlastPlaysForwardsNotBackwards()
        {
            Sprite[] frames = ExplosionFrames.Load();
            Assert.That(frames.Length, Is.GreaterThanOrEqualTo(MinAnimationFrames),
                "Ordering is unobservable on a set this small; the load itself is already broken and "
                + "the preceding test explains the consequence.");

            for (int i = 1; i < frames.Length; i++)
            {
                Assert.That(
                    string.CompareOrdinal(frames[i - 1].name, frames[i].name),
                    Is.LessThan(0),
                    "Playback order IS array order: the configurator AddSprite()s this array front to "
                    + "back, and Resources.LoadAll makes no ordering promise whatsoever. A set that is "
                    + "not strictly ascending by name plays the blast out of sequence — a detonation "
                    + "that dissipates then flashes, or runs backwards — which no length or null check "
                    + $"can detect (saw '{frames[i - 1].name}' before '{frames[i].name}').");
            }
        }

        // ---------------------------------------------------------------------------------
        // 3. What the screen actually shows when the art does not load.
        // ---------------------------------------------------------------------------------

        [Test]
        public void DefaultParticleTexture_CarriesNoColourAtAllWhichIsWhyAMissingSpriteLooksLikeAWhiteCircle()
        {
            // This pins the SYMPTOM end of the defect. GetDefaultParticleTexture() writes
            // Color(1f, 1f, 1f, alpha) across a radial falloff, and GetParticleMaterial(null)
            // installs it whenever a caller has no sprite texture to offer. So this texture IS what
            // the player sees in place of missing art: a white circle. Pinning its colourlessness
            // states out loud that the fallback can never resemble the art it replaces, which is why
            // an import regression reads as a rendering fault rather than as a missing file.
            Texture2D fallback = GameFeelVfx.GetDefaultParticleTexture();

            Assert.That(fallback, Is.Not.Null,
                "The fallback particle texture is null, so GetParticleMaterial(null) installs no "
                + "texture and the particle path renders untextured quads.");

            if (!TryMeanSaturation(fallback, out float saturation))
            {
                // GetDefaultParticleTexture() calls Apply(false, true), which discards the CPU copy,
                // so GetPixel is unavailable by construction and the only route is a GPU readback.
                // Reported rather than silently skipped: a soft pass here would be a green check over
                // an unmeasured claim.
                Assert.Ignore(
                    "LIMITATION — the fallback particle texture could not be sampled. It is created "
                    + "with Apply(false, true) (CPU copy discarded, isReadable false) and has no source "
                    + "file to decode off disk, so a GPU readback is the only route and no graphics "
                    + $"device was available (graphicsDeviceType={SystemInfo.graphicsDeviceType}). The "
                    + "colourlessness of the fallback is therefore UNVERIFIED in this run.");
            }

            Assert.That(saturation, Is.LessThanOrEqualTo(ColourlessSaturation),
                $"The fallback particle texture measured {saturation:F4} mean saturation. It is "
                + "supposed to be Color(1f,1f,1f,alpha) — a colourless radial falloff — because "
                + "ExplosiveGimmick and ExplosionEffectConfigurator rely on the tint being the only "
                + "thing that can carry colour there. If this ever gains colour of its own, a missing "
                + "asset stops reading as the white circle players report and starts masquerading as "
                + "real art, which would hide the next import regression entirely.");
        }

        [Test]
        public void ExplosionFrames_PaintRealColourSoTheyAreDistinguishableFromTheWhiteFallback()
        {
            Sprite[] frames = ExplosionFrames.Load();
            Assert.That(frames.Length, Is.GreaterThanOrEqualTo(MinAnimationFrames),
                "The frames do not load, so there is nothing to compare against the fallback; the "
                + "load test above owns that failure.");

            Texture2D fallback = GameFeelVfx.GetDefaultParticleTexture();
            Assert.That(ReferenceEquals(frames[0].texture, fallback), Is.False,
                "The explosion's own frame texture IS the procedural white-blob fallback. The art has "
                + "been replaced by the placeholder it is supposed to displace, so the explosion "
                + "renders as a white circle while every load check above still passes.");

            // Colour is measured off the source file, which import settings cannot make unreadable —
            // the same escape hatch SiegeArtResourceTests uses, because project textures import
            // non-readable and GetPixel is therefore unavailable.
            Texture2D decoded = DecodeSourceImage(frames[0].texture);
            if (decoded == null)
            {
                Assert.Ignore(
                    "LIMITATION — the explosion's opening frame could not be decoded from disk, so its "
                    + "colour is UNVERIFIED in this run. Reference-distinctness from the fallback was "
                    + "checked above; the stronger 'this art is not white' claim was not.");
            }

            try
            {
                Assert.That(TryMeanSaturation(decoded, out float saturation), Is.True,
                    "The decoded explosion frame could not be sampled even though it was decoded into "
                    + "a readable texture, which means this measurement is broken rather than the art.");
                Assert.That(saturation, Is.GreaterThan(ColourlessSaturation),
                    $"The explosion's opening frame measured {saturation:F4} mean saturation — "
                    + "indistinguishable from the colourless fallback. Either the art was overwritten "
                    + "with a greyscale placeholder or the wrong file is being loaded; either way the "
                    + "blast reads on screen as the same white circle a failed import produces, and "
                    + "the load and ordering checks above would all still pass.");
            }
            finally
            {
                Object.DestroyImmediate(decoded);
            }
        }

        // ---------------------------------------------------------------------------------
        // Helpers — disk walk
        // ---------------------------------------------------------------------------------

        /// <summary>Every texture-importer file beneath <paramref name="root"/>, sorted so a failure
        /// list reads in the same order twice.</summary>
        private static List<string> FindImagesUnder(string root)
        {
            var found = new List<string>();
            foreach (string path in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            {
                string extension = Path.GetExtension(path).ToLowerInvariant();
                for (int i = 0; i < ImageExtensions.Length; i++)
                {
                    if (extension == ImageExtensions[i])
                    {
                        found.Add(path);
                        break;
                    }
                }
            }

            found.Sort(System.StringComparer.Ordinal);
            return found;
        }

        /// <summary>Absolute path to the key `Resources.Load` expects: root-relative, forward
        /// slashes, no extension.</summary>
        private static string ToResourcePath(string resourcesRoot, string absolutePath)
        {
            string relative = absolutePath.Substring(resourcesRoot.Length)
                .TrimStart(Path.DirectorySeparatorChar, '/')
                .Replace('\\', '/');

            int dot = relative.LastIndexOf('.');
            return dot < 0 ? relative : relative.Substring(0, dot);
        }

        /// <summary>Folder portion of a resource path, or empty for an asset at the Resources
        /// root.</summary>
        private static string FolderOf(string resourcePath)
        {
            int slash = resourcePath.LastIndexOf('/');
            return slash < 0 ? string.Empty : resourcePath.Substring(0, slash);
        }

        // ---------------------------------------------------------------------------------
        // Helpers — deriving the raw-texture exemption from runtime source
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Resource folders that runtime code reads as raw textures rather than as sprites, harvested
        /// from `Resources.Load&lt;Texture2D&gt;` / `LoadAll&lt;Texture2D&gt;` call sites under
        /// `Assets/Scripts`.
        ///
        /// Only runtime sources are scanned. Tests are not consumers: a test loading something as a
        /// Texture2D says nothing about how the game reads it, and letting test code widen the
        /// exemption set would let a test excuse the very asset it should be failing on.
        ///
        /// The path literal is frequently not on the call line — `GameManager` picks between three
        /// stage backdrops several lines above its `Resources.Load&lt;Texture2D&gt;` — so a window
        /// around each call site is harvested rather than the call line alone. Over-harvesting is the
        /// known risk of that window; it is bounded by the second condition at the call site (the raw
        /// load must actually succeed) and by the fact that a harvested folder can only ever excuse an
        /// asset that already failed to load as a Sprite.
        /// </summary>
        private static HashSet<string> DeriveTextureOnlyFolders()
        {
            var folders = new HashSet<string>();
            string scriptsRoot = Path.Combine(Application.dataPath, "Scripts");
            if (!Directory.Exists(scriptsRoot)) return folders;

            var callSite = new Regex(@"Resources\.Load(?:All)?\s*<\s*Texture2?D?\s*>");
            var literal = new Regex(@"""([^""\\\r\n]*)""");
            const int lookBehind = 24;
            const int lookAhead = 8;

            foreach (string file in Directory.GetFiles(scriptsRoot, "*.cs", SearchOption.AllDirectories))
            {
                string[] lines = File.ReadAllLines(file);
                for (int i = 0; i < lines.Length; i++)
                {
                    if (!callSite.IsMatch(lines[i])) continue;

                    int from = Mathf.Max(0, i - lookBehind);
                    int to = Mathf.Min(lines.Length - 1, i + lookAhead);
                    for (int line = from; line <= to; line++)
                    {
                        foreach (Match match in literal.Matches(lines[line]))
                        {
                            string folder = FolderOfLiteral(match.Groups[1].Value);
                            if (folder.Length > 0) folders.Add(folder);
                        }
                    }
                }
            }

            return folders;
        }

        /// <summary>Folder portion of a source literal that looks like a resource path. Interpolation
        /// holes truncate the literal — `$"GeneratedUnitFrames/{name}/Idle"` contributes
        /// `GeneratedUnitFrames`, never a folder built from a runtime value. Literals with no slash
        /// contribute nothing: a root-level asset never reaches the exemption check because
        /// <see cref="FolderOf"/> returns empty for it.</summary>
        private static string FolderOfLiteral(string value)
        {
            int brace = value.IndexOf('{');
            if (brace >= 0) value = value.Substring(0, brace);

            int slash = value.LastIndexOf('/');
            if (slash <= 0) return string.Empty;

            string folder = value.Substring(0, slash);
            return folder.Contains(" ") ? string.Empty : folder;
        }

        // ---------------------------------------------------------------------------------
        // Helpers — failure diagnostics
        // ---------------------------------------------------------------------------------

        /// <summary>Names the file, the importer setting behind the failure, and what the player sees.
        /// The importer values are read for the MESSAGE only — no assertion depends on them, so an
        /// importer format change costs diagnostics, never coverage.</summary>
        private static string DescribeFailure(string absolutePath, string resourcePath, string folder,
            HashSet<string> textureOnlyFolders)
        {
            string assetPath = "Assets" + absolutePath.Substring(Application.dataPath.Length).Replace('\\', '/');
            string settings = ReadImporterSettings(absolutePath + ".meta");

            string readAsTexture = Resources.Load<Texture2D>(resourcePath) != null
                ? "it DOES load as a raw Texture2D"
                : "it does not load as a raw Texture2D either";

            string exemption = folder.Length == 0
                ? "asset sits at the Resources root, so no folder exemption can apply"
                : textureOnlyFolders.Contains(folder)
                    ? $"folder '{folder}' IS read as a raw texture by runtime code, but the raw read failed too"
                    : $"no runtime code reads folder '{folder}' as a raw texture, so it must be a Sprite";

            return $"{assetPath} -> Resources.Load<Sprite>(\"{resourcePath}\") == null; {settings}; "
                + $"{readAsTexture}; {exemption}";
        }

        private static string ReadImporterSettings(string metaPath)
        {
            if (!File.Exists(metaPath)) return "no .meta beside it";

            string textureType = null;
            string spriteMode = null;
            foreach (string line in File.ReadAllLines(metaPath))
            {
                string trimmed = line.Trim();
                if (textureType == null && trimmed.StartsWith("textureType:"))
                {
                    textureType = trimmed.Substring("textureType:".Length).Trim();
                }
                else if (spriteMode == null && trimmed.StartsWith("spriteMode:"))
                {
                    spriteMode = trimmed.Substring("spriteMode:".Length).Trim();
                }
            }

            if (textureType == null) return "importer settings unreadable from .meta";

            string verdict = textureType == "8"
                ? "already Sprite — look past the importer"
                : $"textureType {textureType} is not Sprite (8), so no sprite is generated; needs "
                  + "textureType: 8 with spriteMode: 1";

            return $"textureType: {textureType}, spriteMode: {spriteMode ?? "absent"} ({verdict})";
        }

        // ---------------------------------------------------------------------------------
        // Helpers — colour measurement
        // ---------------------------------------------------------------------------------

        /// <summary>
        /// Mean HSV saturation over the texture, weighted by alpha so a radial falloff's transparent
        /// margin cannot dilute the reading of its opaque core. Uses GetPixel when the texture is
        /// readable and a GPU readback otherwise, because the interesting texture here is created
        /// with Apply(false, true) and has no CPU copy at all. Returns false only when neither route
        /// is available, which callers must report rather than treat as a pass.
        /// </summary>
        private static bool TryMeanSaturation(Texture2D texture, out float saturation)
        {
            saturation = 0f;
            if (texture == null || texture.width <= 0 || texture.height <= 0) return false;

            Texture2D readable = texture;
            Texture2D copy = null;
            try
            {
                if (!texture.isReadable)
                {
                    copy = ReadBackFromGpu(texture);
                    if (copy == null) return false;
                    readable = copy;
                }

                float weighted = 0f;
                float weight = 0f;
                for (int gy = 0; gy < SaturationGrid; gy++)
                {
                    for (int gx = 0; gx < SaturationGrid; gx++)
                    {
                        int px = Mathf.Clamp(
                            (int)(((gx + 0.5f) / SaturationGrid) * readable.width), 0, readable.width - 1);
                        int py = Mathf.Clamp(
                            (int)(((gy + 0.5f) / SaturationGrid) * readable.height), 0, readable.height - 1);

                        Color sample = readable.GetPixel(px, py);
                        Color.RGBToHSV(sample, out _, out float s, out _);
                        weighted += s * sample.a;
                        weight += sample.a;
                    }
                }

                if (weight <= 0f) return false;

                saturation = weighted / weight;
                return true;
            }
            catch (UnityException)
            {
                // GetPixel on a texture Unity still considers unreadable.
                return false;
            }
            finally
            {
                if (copy != null) Object.DestroyImmediate(copy);
            }
        }

        /// <summary>Blits a GPU-only texture into a RenderTexture and reads it back. This is the
        /// house pattern for headless pixel access (see the PlayMode capture fixtures); it needs a
        /// graphics device and returns null when there is none.</summary>
        private static Texture2D ReadBackFromGpu(Texture2D texture)
        {
            if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            {
                return null;
            }

            RenderTexture rt = RenderTexture.GetTemporary(
                texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
            RenderTexture previous = RenderTexture.active;
            Texture2D copy = null;
            try
            {
                Graphics.Blit(texture, rt);
                RenderTexture.active = rt;

                copy = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
                copy.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
                copy.Apply(false, false);
                return copy;
            }
            catch (System.Exception)
            {
                if (copy != null) Object.DestroyImmediate(copy);
                return null;
            }
            finally
            {
                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);
            }
        }

        /// <summary>Decodes a texture's source file off disk into a readable copy. Import settings
        /// cannot make this unavailable, which is why it is preferred over GetPixel for assets that
        /// have a file behind them.</summary>
        private static Texture2D DecodeSourceImage(Texture2D imported)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(imported);
            if (string.IsNullOrEmpty(assetPath)) return null;

            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string absolutePath = Path.Combine(projectRoot, assetPath);
            if (!File.Exists(absolutePath)) return null;

            var decoded = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(decoded, File.ReadAllBytes(absolutePath), false))
            {
                Object.DestroyImmediate(decoded);
                return null;
            }

            return decoded;
        }
    }
}
