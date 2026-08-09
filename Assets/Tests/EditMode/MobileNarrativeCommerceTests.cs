using NUnit.Framework;
using UnityEngine;
using UnityEngine.Purchasing;
using CastleBusters;

namespace CastleBusters.Tests
{
    [TestFixture]
    public class MobileNarrativeCommerceTests
    {
        [SetUp]
        public void SetUp()
        {
            MobileStoreEntitlements.ResetForTesting();
        }

        [TearDown]
        public void TearDown()
        {
            MobileStoreEntitlements.ResetForTesting();
        }

        [Test]
        public void GetNormalizedSafeArea_MapsNontrivialScreenSafeAreaToExactAnchors()
        {
            var normalized = MobileSafeArea.GetNormalizedSafeArea(
                new Rect(156f, 81f, 1608f, 918f),
                screenWidth: 1920,
                screenHeight: 1080);

            Assert.That(normalized.xMin, Is.EqualTo(0.08125f));
            Assert.That(normalized.yMin, Is.EqualTo(0.075f));
            Assert.That(normalized.xMax, Is.EqualTo(0.91875f));
            Assert.That(normalized.yMax, Is.EqualTo(0.925f));
        }

        [Test]
        public void NarrativeTypewriter_RevealsWholeEmojiAndCombiningTextElementsBeforeCompleting()
        {
            const string text = "🛡️e\u0301!";
            var typewriter = new NarrativeTypewriter(text, charactersPerSecond: 1f);

            typewriter.Advance(1f);
            Assert.That(typewriter.VisibleText, Is.EqualTo("🛡️"));
            Assert.That(typewriter.IsComplete, Is.False);

            typewriter.Advance(1f);
            Assert.That(typewriter.VisibleText, Is.EqualTo("🛡️e\u0301"));
            Assert.That(typewriter.IsComplete, Is.False);

            typewriter.Advance(1f);
            Assert.That(typewriter.VisibleText, Is.EqualTo(text));
            Assert.That(typewriter.IsComplete, Is.True);
        }

        [Test]
        public void CreateChronicleProductDefinition_UsesPublishedIdsAndNonConsumableType()
        {
            var product = MobileStoreCatalog.CreateChronicleProductDefinition();

            Assert.That(product.id, Is.EqualTo(MobileStoreCatalog.ChronicleProductId));
            Assert.That(product.storeSpecificId, Is.EqualTo(MobileStoreCatalog.ChronicleProductId));
            Assert.That(product.type, Is.EqualTo(ProductType.NonConsumable));
        }

        [Test]
        public void ChronicleCatalogAndEntitlementHelper_PreservePublishedIdAndIdempotentReplayOwnership()
        {
            Assert.That(MobileStoreCatalog.ChronicleProductId,
                Is.EqualTo("com.jangyoung.unknowncastle.chronicle_pack"));

            MobileStoreEntitlements.GrantChroniclePack();
            MobileStoreEntitlements.GrantChroniclePack();
            Assert.That(MobileStoreEntitlements.HasChroniclePack, Is.True,
                "Replayed purchase or restoration callbacks must preserve the boolean Chronicle replay entitlement.");

            MobileStoreEntitlements.ResetForTesting();
            MobileStoreEntitlements.ResetForTesting();
            Assert.That(MobileStoreEntitlements.HasChroniclePack, Is.False,
                "The test reset helper must remove the local entitlement even when called repeatedly.");
        }

        [Test]
        public void InitializeIfSupported_InEditor_MarksStorefrontUnavailableWithoutGrantingLocalEntitlement()
        {
            var storefrontObject = new GameObject("MobileStorefrontEditorContractTest");
            var storefront = storefrontObject.AddComponent<MobileStorefront>();

            try
            {
                storefront.InitializeIfSupported();

                Assert.That(storefront.State, Is.EqualTo(MobileStorefrontState.Unavailable));
                Assert.That(storefront.HasChroniclePack, Is.False,
                    "Editor storefront unavailability must not be treated as purchase evidence or grant local ownership.");
            }
            finally
            {
                Object.DestroyImmediate(storefrontObject);
            }
        }
    }
}
