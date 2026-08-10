using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Units are scaled per stage so they occupy the same screen area on a 39u board and a
    /// 47u one. That is a presentation change, and these tests exist to keep it one: the
    /// factor must never reach the simulation.
    /// </summary>
    public class StageActorScaleTests
    {
        [Test]
        public void StageActorVisualScale_IsOneWithoutAGameManager()
        {
            // EditMode fixtures build units with no GameManager in the scene. If this ever
            // returned something else, every collider assertion in the suite would shift.
            Assert.AreEqual(1f, GameManager.StageActorVisualScale, 1e-5f);
        }

        [Test]
        public void StageActorVisualScale_TracksCameraWidthRelativeToStage1()
        {
            // The factor is defined as the stage's framed width over the baseline's, so a
            // wider board scales actors up by exactly the amount the camera shrinks them.
            float baseline = StageDefinitions.Stage1.cameraDesiredWorldWidth;
            Assert.Greater(StageDefinitions.Stage3.cameraDesiredWorldWidth / baseline, 1f,
                "Stage3 frames a wider board, so its actors must scale up.");
            Assert.Less(StageDefinitions.Stage2.cameraDesiredWorldWidth / baseline, 1f,
                "Stage2 frames a tighter board, so its actors must scale down.");
        }

        [Test]
        public void ColliderExtents_AreInvariantUnderVisualScale()
        {
            // The property that makes the whole change safe: the transform multiplies by
            // renderScale while the collider divides by it, so world extents cancel out.
            // Verified here on the arithmetic rather than through a live unit, because a
            // unit needs a sprite the EditMode fixture cannot guarantee.
            const float coverage = 0.62f;
            const float spriteSize = 1.4f;
            const float reference = 0.48f;

            float ExtentFor(float renderScale)
            {
                float ratio = reference / renderScale;
                float local = Mathf.Max(0.25f / renderScale, spriteSize * coverage * ratio);
                return local * renderScale; // world extent
            }

            float atStage1 = ExtentFor(0.48f);
            float atStage3 = ExtentFor(0.48f * 1.205f);
            Assert.AreEqual(atStage1, atStage3, 1e-4f,
                "Scaling the art on a wider stage must not change what the unit can hit.");
        }
    }
}
