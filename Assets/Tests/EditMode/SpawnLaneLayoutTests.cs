using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Field obstacles are laid out around the keeps, not through them, and never piled on
    /// top of each other. Enlarging the keep silently invalidated the old ±6.5 lanes — they
    /// fell inside the new wall courses and could no longer spawn anything — so these pin
    /// the relationship rather than the numbers.
    /// </summary>
    public class SpawnLaneLayoutTests
    {
        /// <summary>Innermost and outermost wall course, from GameManager.KeepProfile.</summary>
        static (float min, float max) KeepFootprint()
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (var course in GameManager.KeepProfile)
            {
                min = Mathf.Min(min, course.AbsX - 0.5f);
                max = Mathf.Max(max, course.AbsX + 0.5f);
            }
            return (min, max);
        }

        [Test]
        public void Lanes_NeverFallInsideAKeep()
        {
            var keep = KeepFootprint();
            foreach (float lane in GimmickFieldDirector.SpawnLanes)
            {
                float abs = Mathf.Abs(lane);
                bool insideKeep = abs > keep.min && abs < keep.max;
                Assert.IsFalse(insideKeep,
                    $"lane {lane} sits inside the keep ({keep.min}–{keep.max}); it could never spawn");
            }
        }

        [Test]
        public void Lanes_KeepTheMinimumSpacingFromEachOther()
        {
            var lanes = (float[])GimmickFieldDirector.SpawnLanes.Clone();
            System.Array.Sort(lanes);

            for (int i = 1; i < lanes.Length; i++)
            {
                float gap = lanes[i] - lanes[i - 1];
                Assert.GreaterOrEqual(gap, GimmickFieldDirector.MinObstacleSpacing - 0.001f,
                    $"lanes {lanes[i - 1]} and {lanes[i]} are {gap}u apart — hazards would read as piled up");
            }
        }

        [Test]
        public void Lanes_StayClearOfTheCoreColumns()
        {
            foreach (float lane in GimmickFieldDirector.SpawnLanes)
            {
                Assert.Greater(Mathf.Abs(Mathf.Abs(lane) - GameManager.CoreAbsX), 0.9f,
                    $"lane {lane} sits on a core column");
            }
        }

        [Test]
        public void Lanes_AreMirrored()
        {
            foreach (float lane in GimmickFieldDirector.SpawnLanes)
            {
                if (Mathf.Approximately(lane, 0f)) continue;
                Assert.IsTrue(System.Array.Exists(GimmickFieldDirector.SpawnLanes,
                        l => Mathf.Approximately(l, -lane)),
                    $"lane {lane} has no mirror; the two sides would not get the same field");
            }
        }

        [Test]
        public void Lanes_SpanBothWings()
        {
            float min = float.MaxValue, max = float.MinValue;
            foreach (float lane in GimmickFieldDirector.SpawnLanes)
            {
                min = Mathf.Min(min, lane);
                max = Mathf.Max(max, lane);
            }
            Assert.GreaterOrEqual(max - min, 20f,
                "hazards must still reach both wings, not cluster at midfield");
        }
    }
}
