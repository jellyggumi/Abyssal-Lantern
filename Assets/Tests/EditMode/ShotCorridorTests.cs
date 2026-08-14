using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the geometric relationship nobody had checked: where field obstacles stand versus where
    /// the player's shot has to fly.
    ///
    /// `DynamicBattlefield.SpawnLanes` documents being clear of the launch aprons and the eruption
    /// vent columns. It says nothing about the shot corridor, and the live sweep found out why that
    /// matters — at 45 degrees, shots were intercepted by `FieldTowerBlock_1` at x=0 and by the
    /// flying beast at x=0.32, before reaching the keep. Measurements in
    /// `_workspace/current/qa/aim-space-reachability.md`.
    ///
    /// These tests do not assert that interception is wrong. A destructible obstacle in the way is a
    /// legitimate design, and the beast carries its own HP. They assert that the collision is
    /// DELIBERATE: the numbers that produce it are named here, so moving a lane or an altitude
    /// changes a test and has to be argued rather than drifted into.
    /// </summary>
    public class ShotCorridorTests
    {
        // Read from source rather than restated as literals where possible; the ones that cannot be
        // read (physics, apron) are named with where they come from.
        private const float Gravity = 9.81f;          // Physics2D default * gravityScale 1
        private const float SpawnHeight = 0.9f;       // UnitController.DefaultLaunchSpawnHeight
        private const float ApronX = 17.0f;           // GameManager.LaunchApronAbsX

        /// <summary>Height of a 45-degree shot as it crosses a given x, for a given draw.</summary>
        private static float AltitudeAt(float x, float draw)
        {
            float speed = LaunchPowerCurve.SpeedForDraw(draw);
            float a = 45f * Mathf.Deg2Rad;
            float vx = speed * Mathf.Cos(a), vy = speed * Mathf.Sin(a);
            float t = (x + ApronX) / vx;
            return SpawnHeight + vy * t - 0.5f * Gravity * t * t;
        }

        /// <summary>
        /// The solid-obstacle lanes sit inside the corridor, and that is now written down.
        ///
        /// Solid obstacles fold onto the inner lanes, one of which is exactly x=0 — the midpoint of
        /// a shot from the player apron to the enemy keep. A mini tower there stands 2 units tall
        /// (blocks at y=0.5 and 1.5), so it intercepts any shot crossing below that.
        /// </summary>
        [Test]
        public void SolidObstacleLanes_IncludeTheCorridorMidpoint()
        {
            var inner = GimmickFieldDirector.SpawnLanes
                .Where((_, i) => i == 2 || i == 3 || i == 4)   // InnerLaneIndices
                .ToArray();

            Assert.Contains(0f, inner,
                "solid field obstacles fold onto the inner lanes, and x=0 is the corridor midpoint. "
                + "If this lane set changes, the interception measurements in "
                + "qa/aim-space-reachability.md no longer describe the shipped board");

            foreach (float lane in inner)
            {
                Assert.Less(Mathf.Abs(lane), ApronX,
                    $"lane {lane} must be between the aprons to be a midfield obstacle at all");
            }
        }

        /// <summary>
        /// A low shot is intercepted by a tower on the midpoint lane; a high one is not.
        ///
        /// This is the arithmetic that makes draw matter for reasons other than range: below roughly
        /// two-thirds draw the arc is still under a tower's roof when it reaches midfield.
        /// </summary>
        [Test]
        public void LowDraws_PassBelowAMidfieldTowerRoof()
        {
            const float towerTop = 2.0f;   // SpawnMiniTower: blocks at y=0.5 and y=1.5

            float lowAlt = AltitudeAt(2.5f, 0.60f);
            Assert.Less(lowAlt, towerTop,
                $"a 60% draw crosses lane x=2.5 at {lowAlt:F2}u, under a {towerTop}u tower - this is "
                + "the interception the live sweep recorded, not a hypothetical");

            float highAlt = AltitudeAt(2.5f, 0.86f);
            Assert.Greater(highAlt, towerTop,
                $"an 86% draw crosses the same lane at {highAlt:F2}u and clears the tower");
        }

        /// <summary>
        /// The flying beast's altitude band overlaps the corridor, so it is a corridor obstacle and
        /// not only a wall-ramming gimmick.
        ///
        /// It patrols around x=0 at <see cref="FlightRules.BaseAltitude"/> with a bob, and is sized
        /// 3.1 world units, so its body reaches well above its centre. The live sweep hit it at
        /// x=0.32 with an 86% draw.
        /// </summary>
        [Test]
        public void TheFlyingBeast_SitsInsideTheCorridorAltitudeBand()
        {
            // Patrol bob is ±0.9 around base; Rampage dives DiveDepth below it.
            float bandLow = FlightRules.BaseAltitude - FlightRules.DiveDepth;
            float bandHigh = FlightRules.BaseAltitude + 1.8f;   // Sweep phase bob amplitude

            Assert.Greater(bandHigh, 0f, "precondition: the beast flies above the deck");

            // The corridor's altitude at the beast's patrol centre, across the usable draw range.
            float lowDrawAlt = AltitudeAt(0f, 0.60f);
            float highDrawAlt = AltitudeAt(0f, 1.00f);

            Assert.Less(lowDrawAlt, bandHigh,
                $"a 60% draw crosses x=0 at {lowDrawAlt:F2}u, inside the beast's band "
                + $"({bandLow:F1}..{bandHigh:F1}) - so the beast can eat low shots");

            // And the top of the usable range should clear it, or there is no shot that gets past.
            Assert.Greater(highDrawAlt, bandHigh,
                $"a full draw crosses x=0 at {highDrawAlt:F2}u and must clear the beast's band "
                + $"top {bandHigh:F1}u, otherwise no draw at 45 degrees can reach the keep past it");
        }

        /// <summary>
        /// Obstacle lane assignment is a pure function of turn, with no randomness.
        ///
        /// Worth pinning because the sweep's run-to-run variance looked like random lane placement
        /// at first. It is not: the same turn always picks the same lane, so any variance comes from
        /// elsewhere (the beast moves, and occupancy probing can skip a spawn).
        /// </summary>
        [Test]
        public void LaneAssignment_IsDeterministicPerTurn()
        {
            for (int turn = 1; turn <= 12; turn++)
            {
                int first = GimmickFieldDirector.LaneIndexFor(FieldObstacleKind.MiniTower, turn);
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    Assert.AreEqual(first,
                        GimmickFieldDirector.LaneIndexFor(FieldObstacleKind.MiniTower, turn),
                        $"turn {turn} must always choose the same lane - the sweep's variance is not from here");
                }
            }
        }
    }
}
