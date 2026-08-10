using CastleBusters;
using NUnit.Framework;

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

    }
}
