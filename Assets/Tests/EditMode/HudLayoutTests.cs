using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The bottom-anchored HUD stack. The Last Stand card is a clone of a selection-row button
    /// and is placed in a different method from the row itself, so the two drifted apart once:
    /// the row moved to a bottom anchor while the card kept a y written for a centre-anchored
    /// parent, which put it below the screen edge. It still animated and still reported itself
    /// interactable — nothing looked broken except that a pointer could never hit it. These
    /// pin the geometry that made the difference.
    /// </summary>
    public class HudLayoutTests
    {
        static float RowTop => GameManager.SelectionRowY + GameManager.SelectionRowCardHeight / 2f;
        static float LastStandBottom => GameManager.LastStandCardY - GameManager.LastStandCardHeight / 2f;

        [Test]
        public void BottomAnchoredCards_StayOnScreen()
        {
            // Anchored to the bottom edge, so any y that puts a card's lower edge below zero
            // puts it off the display entirely.
            Assert.Greater(GameManager.SelectionRowY - GameManager.SelectionRowCardHeight / 2f, 0f,
                "the selection row hangs off the bottom edge");
            Assert.Greater(LastStandBottom, 0f,
                "the Last Stand card hangs off the bottom edge — it would be unclickable");
        }

        [Test]
        public void LastStandCard_SitsAboveTheSelectionRowWithoutOverlapping()
        {
            Assert.Greater(LastStandBottom, RowTop,
                "the Last Stand card overlaps the selection row; whichever draws last steals the tap");
        }

        static float StripTop => SiegeForecastStrip.StripY + SiegeForecastStrip.StripHeight / 2f;
        static float StripBottom => SiegeForecastStrip.StripY - SiegeForecastStrip.StripHeight / 2f;

        [Test]
        public void ForecastStrip_StaysOnScreenAndClearOfTheLastStandCard()
        {
            // The strip took the row the one-shot loop vacated. The audit's warning about that
            // band was explicit: its bounds are constants, but nothing checked what moved in.
            // D-009 was exactly that failure — a card animated, reported interactable, and sat
            // off-screen — so the new occupant gets the same guard the cards have.
            Assert.Greater(StripBottom, 0f,
                "the forecast strip hangs off the bottom edge");
            Assert.Greater(LastStandBottom, StripTop,
                "the forecast strip overlaps the Last Stand card");
        }

        [Test]
        public void ForecastStrip_FitsInsideTheVacatedSelectionRow()
        {
            // It should occupy that row, not merely avoid collisions somewhere else on screen:
            // if it drifts out, it lands on whatever else the bottom band holds.
            float rowBottom = GameManager.SelectionRowY - GameManager.SelectionRowCardHeight / 2f;
            Assert.GreaterOrEqual(StripBottom, rowBottom - 0.01f,
                "the strip sits below the selection row band");
            Assert.LessOrEqual(StripTop, RowTop + 0.01f,
                "the strip sits above the selection row band");
        }
    }

    /// <summary>
    /// Guards the scaler ownership boundary between the HUD's legibility floor and safe-area setup.
    /// </summary>
    public class HudScaleFloorIntegrationTests
    {
        [Test]
        public void ConfigureCanvas_WithHudScaleFloor_PreservesConstantPixelSize()
        {
            var hud = new GameObject(
                "HudScaleFloorIntegrationTest",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler));

            try
            {
                var canvas = hud.GetComponent<Canvas>();
                var scaler = hud.GetComponent<CanvasScaler>();
                var floor = hud.AddComponent<HudScaleFloor>();

                // EditMode construction does not drive MonoBehaviour.Awake, and SendMessage
                // attempts Unity lifecycle dispatch that asserts in EditMode. Reflection invokes
                // only the real callback that installs the floor's ConstantPixelSize scaling mode.
                typeof(HudScaleFloor)
                    .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                    .Invoke(floor, null);
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ConstantPixelSize),
                    "The test must begin with HudScaleFloor owning the scaler mode.");

                MobileSafeArea.ConfigureCanvas(canvas);

                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ConstantPixelSize),
                    "Safe-area setup must not replace the HUD text-size floor with ScaleWithScreenSize.");
            }
            finally
            {
                Object.DestroyImmediate(hud);
            }
        }

        [Test]
        public void ConfigureCanvas_WithoutHudScaleFloor_BuildsDefaultCanvasInfrastructure()
        {
            var canvasObject = new GameObject(
                "MobileSafeAreaNoFloorTest",
                typeof(RectTransform),
                typeof(Canvas));

            try
            {
                var canvas = canvasObject.GetComponent<Canvas>();

                MobileSafeArea.ConfigureCanvas(canvas);

                var scaler = canvasObject.GetComponent<CanvasScaler>();
                Assert.That(scaler, Is.Not.Null,
                    "Safe-area setup must add a scaler when no HUD scale floor owns the canvas.");
                Assert.That(scaler.uiScaleMode, Is.EqualTo(CanvasScaler.ScaleMode.ScaleWithScreenSize));
                Assert.That(scaler.referenceResolution, Is.EqualTo(new Vector2(1920f, 1080f)));
                Assert.That(scaler.matchWidthOrHeight, Is.EqualTo(0.5f));
                Assert.That(canvasObject.GetComponent<GraphicRaycaster>(), Is.Not.Null,
                    "Safe-area setup must make the canvas raycast-capable.");

                var contentRoot = canvas.transform.Find(MobileSafeArea.ContentRootName) as RectTransform;
                Assert.That(contentRoot, Is.Not.Null,
                    "Safe-area setup must create its content root beneath the canvas.");
                Assert.That(contentRoot.GetComponent<MobileSafeArea>(), Is.Not.Null,
                    "The content root must own the component that applies safe-area anchors.");
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }
    }
}
