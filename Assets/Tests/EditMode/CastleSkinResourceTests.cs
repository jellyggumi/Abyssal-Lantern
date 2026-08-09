using System.Collections.Generic;
using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    [TestFixture]
    public sealed class CastleSkinResourceTests
    {
        private GameObject blockObject;

        [SetUp]
        public void SetUp()
        {
            CastleSkinLibrary.ClearCache();
        }

        [TearDown]
        public void TearDown()
        {
            if (blockObject != null)
            {
                Object.DestroyImmediate(blockObject);
            }

            CastleSkinLibrary.ClearCache();
        }

        [Test]
        public void TryGetSkin_LoadsDistinctNormalCrackedAndHeavySpritesForEveryFacadeRole()
        {
            var roles = new[]
            {
                CastleSkinRole.Face,
                CastleSkinRole.Crown,
                CastleSkinRole.Edge,
                CastleSkinRole.Base,
            };
            var loadedSprites = new HashSet<Sprite>();

            foreach (CastleSkinRole role in roles)
            {
                bool loaded = CastleSkinLibrary.TryGetSkin(role, out var normal, out var cracked, out var heavy);

                Assert.That(loaded, Is.True,
                    $"{role} must resolve its normal, cracked, and heavy sprites from Resources/CastleSkin.");
                Assert.That(normal, Is.Not.Null, $"{role} normal sprite must be loadable from Resources.");
                Assert.That(cracked, Is.Not.Null, $"{role} cracked sprite must be loadable from Resources.");
                Assert.That(heavy, Is.Not.Null, $"{role} heavy sprite must be loadable from Resources.");
                Assert.That(loadedSprites.Add(normal), Is.True,
                    $"{role} normal sprite must not alias another facade role or damage state.");
                Assert.That(loadedSprites.Add(cracked), Is.True,
                    $"{role} cracked sprite must not alias another facade role or damage state.");
                Assert.That(loadedSprites.Add(heavy), Is.True,
                    $"{role} heavy sprite must not alias another facade role or damage state.");
            }

            Assert.That(loadedSprites.Count, Is.EqualTo(12),
                "Four facade roles with three authored damage states must resolve twelve distinct sprite assets.");
        }

        [Test]
        public void SetSkinSprites_AppliesCurrentDamageArtWithoutChangingColliderHealthOrMass()
        {
            Assert.That(
                CastleSkinLibrary.TryGetSkin(CastleSkinRole.Face, out var faceNormal, out _, out _),
                Is.True,
                "The baseline Face sprite must be available from Resources.");
            Assert.That(
                CastleSkinLibrary.TryGetSkin(CastleSkinRole.Crown, out var crownNormal, out var crownCracked, out var crownHeavy),
                Is.True,
                "The replacement Crown sprites must be available from Resources.");

            blockObject = new GameObject("CastleSkinGameplayNeutralityProbe");
            var renderer = blockObject.AddComponent<SpriteRenderer>();
            var collider = blockObject.AddComponent<BoxCollider2D>();
            var body = blockObject.AddComponent<Rigidbody2D>();
            var block = blockObject.AddComponent<DestructibleBlock>();
            MethodInfo awake = typeof(DestructibleBlock).GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null,
                "The EditMode fixture must execute DestructibleBlock.Awake through the production lifecycle path.");
            awake.Invoke(block, null);

            block.targetWorldSize = 1.25f;
            block.SetPresentationSprite(faceNormal);
            block.maxHP = 250f;
            block.currentHP = 150f;
            body.mass = 3.75f;
            Physics2D.SyncTransforms();

            Vector2 colliderWorldSizeBefore = collider.bounds.size;
            float maxHpBefore = block.maxHP;
            float currentHpBefore = block.currentHP;
            float massBefore = body.mass;

            block.SetSkinSprites(crownNormal, crownCracked, crownHeavy, flipX: true);
            Physics2D.SyncTransforms();

            Assert.That(renderer.sprite, Is.SameAs(crownCracked),
                "A block at 60% HP must immediately display the loaded cracked Crown sprite, proving the skin was applied.");
            Assert.That(collider.bounds.size.x, Is.EqualTo(colliderWorldSizeBefore.x).Within(0.0001f),
                "Changing facade art must not change the block collider's world width.");
            Assert.That(collider.bounds.size.y, Is.EqualTo(colliderWorldSizeBefore.y).Within(0.0001f),
                "Changing facade art must not change the block collider's world height.");
            Assert.That(block.maxHP, Is.EqualTo(maxHpBefore),
                "Applying presentation art must not change maximum HP.");
            Assert.That(block.currentHP, Is.EqualTo(currentHpBefore),
                "Applying presentation art must not heal or damage the block.");
            Assert.That(body.mass, Is.EqualTo(massBefore),
                "Applying presentation art must not change Rigidbody2D mass.");
        }
    }

    [TestFixture]
    public sealed class HiggsfieldSpriteLibraryTests
    {
        private static readonly string[] UiKeys =
        {
            HiggsfieldSpriteLibrary.Knight,
            HiggsfieldSpriteLibrary.Archer,
            HiggsfieldSpriteLibrary.Cannon,
            HiggsfieldSpriteLibrary.Barrel,
            HiggsfieldSpriteLibrary.Ram,
            HiggsfieldSpriteLibrary.Trap,
        };

        private static readonly string[] VfxKeys =
        {
            HiggsfieldSpriteLibrary.Impact,
            HiggsfieldSpriteLibrary.Wind,
            HiggsfieldSpriteLibrary.CoreCrack,
            HiggsfieldSpriteLibrary.CollapseDust,
        };

        [TestCaseSource(nameof(UiKeys))]
        public void LoadUi_AuthoredKey_Resolves512SpriteWithTransparentCorner(string key)
        {
            AssertAuthoredSprite(HiggsfieldSpriteLibrary.LoadUi(key), key);
        }

        [TestCaseSource(nameof(VfxKeys))]
        public void LoadVfx_AuthoredKey_Resolves512SpriteWithTransparentCorner(string key)
        {
            AssertAuthoredSprite(HiggsfieldSpriteLibrary.LoadVfx(key), key);
        }

        private static void AssertAuthoredSprite(Sprite sprite, string key)
        {
            Assert.That(sprite, Is.Not.Null, $"{key} must resolve through the public Higgsfield Resources API.");
            Assert.That(sprite.texture.width, Is.EqualTo(512), $"{key} must retain its authored 512-pixel width.");
            Assert.That(sprite.texture.height, Is.EqualTo(512), $"{key} must retain its authored 512-pixel height.");
            Assert.That(HasTransparentCorner(sprite.texture), Is.True,
                $"{key} must preserve transparent corner pixels instead of importing an opaque backdrop.");
        }

        private static bool HasTransparentCorner(Texture2D texture)
        {
            string assetPath = UnityEditor.AssetDatabase.GetAssetPath(texture);
            var importer = UnityEditor.AssetImporter.GetAtPath(assetPath) as UnityEditor.TextureImporter;
            if (importer == null
                || importer.alphaSource != UnityEditor.TextureImporterAlphaSource.FromInput
                || !importer.alphaIsTransparency)
            {
                return false;
            }

            string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
            string absolutePath = System.IO.Path.Combine(projectRoot, assetPath);
            var readableSource = new Texture2D(2, 2, TextureFormat.RGBA32, false);

            try
            {
                if (!ImageConversion.LoadImage(
                        readableSource,
                        System.IO.File.ReadAllBytes(absolutePath),
                        false))
                {
                    return false;
                }

                Color32[] cornerPixels =
                {
                    readableSource.GetPixel(0, 0),
                    readableSource.GetPixel(readableSource.width - 1, 0),
                    readableSource.GetPixel(0, readableSource.height - 1),
                    readableSource.GetPixel(readableSource.width - 1, readableSource.height - 1),
                };

                for (int i = 0; i < cornerPixels.Length; i++)
                {
                    if (cornerPixels[i].a <= 16) return true;
                }

                return false;
            }
            finally
            {
                Object.DestroyImmediate(readableSource);
            }
        }
    }
}
