using CastleBusters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the two keg-placement invariants the 2026-08-12 live defect proved are safety
    /// rules, not taste ("core drains by itself", KEEP CORE 150→70 on the player's own
    /// first volley):
    ///
    /// 1. MUZZLE SPAWN FOOTPRINT — a launched body materializes AT the muzzle with the
    ///    collider Awake fits, which today is far wider than the rendered art (Knight:
    ///    5.28u collider vs 0.93u sprite — recorded, deliberately not "fixed" here, because
    ///    the whole balance stack was tuned on it). Any keg whose collider band overlaps
    ///    that spawn footprint detonates on frame 1, before the shot flies, and the blast
    ///    lands on whatever stands near the keg. The old Stage1 ±11 pair sat 3.5u out —
    ///    inside the 2.64 + 0.76 + drift band — and cost the player 80 core HP per match.
    ///
    /// 2. BLAST-vs-CORE REST CLEARANCE — a keg at rest must sit outside blast radius of
    ///    both cores, or any stray detonation (enemy arrow, falling block, chain) splashes
    ///    a core nobody aimed at. Shove-a-keg-then-pop-it stays legal play; the SPAWN
    ///    layout is what must never hand that out for free.
    ///
    /// Every number is derived from the real prefabs through the same resolver the runtime
    /// and the aim preview use (EstimateLaunchedWorldColliderBounds), so art or collider
    /// changes move the thresholds instead of silently invalidating them.
    /// </summary>
    [TestFixture]
    public sealed class KegPlacementSafetyTests
    {
        /// <summary>Observed keg drift within a match (spawn -11.0 → rest -10.87, plus
        /// settling) with headroom: kegs are dynamic bodies and combat shoves them.</summary>
        private const float DriftMargin = 0.8f;

        /// <summary>Rest-position margin over the blast radius. Deliberately smaller than
        /// DriftMargin: drifted-then-detonated is earned play, resting splash is not.</summary>
        private const float BlastRestMargin = 0.25f;

        private static GameObject LoadPrefab(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"expected prefab at {path}");
            return prefab;
        }

        /// <summary>Widest launched-body half-width across every projectile the volley
        /// cycle can put on the muzzle, resolved exactly as LaunchManager resolves it.</summary>
        private static float WidestLaunchedHalfWidth()
        {
            float widest = 0f;
            foreach (var path in new[]
                     {
                         "Assets/Prefabs/Knight.prefab",
                         "Assets/Prefabs/Archer.prefab",
                         "Assets/Prefabs/ExplosiveBarrel.prefab"
                     })
            {
                Bounds bounds = UnitController.EstimateLaunchedWorldColliderBounds(LoadPrefab(path));
                widest = Mathf.Max(widest, bounds.extents.x);
            }
            return widest;
        }

        private static float KegHalfWidth()
        {
            Bounds bounds = UnitController.EstimateLaunchedWorldColliderBounds(
                LoadPrefab("Assets/Prefabs/ExplosiveBarrel.prefab"));
            return bounds.extents.x;
        }

        private static float KegBlastRadius()
        {
            var explosive = LoadPrefab("Assets/Prefabs/ExplosiveBarrel.prefab")
                .GetComponent<ExplosiveGimmick>();
            Assert.IsNotNull(explosive, "the keg prefab must carry its ExplosiveGimmick");
            return explosive.PermanentExplosionRadius;
        }

        [Test]
        public void EveryStage_KegsClearTheLaunchedBodySpawnFootprint()
        {
            float footprintBand = WidestLaunchedHalfWidth() + KegHalfWidth() + DriftMargin;

            foreach (var stage in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                foreach (var keg in stage.barrelPositions)
                {
                    foreach (float muzzleX in new[] { -stage.launchApronAbsX, stage.launchApronAbsX })
                    {
                        Assert.Greater(Mathf.Abs(keg.x - muzzleX), footprintBand,
                            $"{stage.displayName}: keg at x={keg.x} is inside the launched-body spawn " +
                            $"footprint of the muzzle at x={muzzleX} (band {footprintBand:F2}u — body " +
                            $"{WidestLaunchedHalfWidth():F2} + keg {KegHalfWidth():F2} + drift {DriftMargin}). " +
                            "A volley from this muzzle detonates the keg on frame 1, before the shot flies.");
                    }
                }
            }
        }

        [Test]
        public void EveryStage_KegsRestOutsideBlastRangeOfBothCores()
        {
            float clearance = KegBlastRadius() + BlastRestMargin;

            foreach (var stage in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                foreach (var keg in stage.barrelPositions)
                {
                    foreach (float coreX in new[] { -GameManager.CoreAbsX, GameManager.CoreAbsX })
                    {
                        // Cores stand at y=0.5 like the kegs; the x axis is the binding one,
                        // but measure the real 2D distance the blast check itself uses.
                        float distance = Vector2.Distance(
                            new Vector2(keg.x, keg.y), new Vector2(coreX, 0.5f));
                        Assert.Greater(distance, clearance,
                            $"{stage.displayName}: keg at x={keg.x} rests {distance:F2}u from the core at " +
                            $"x={coreX} — inside blast {KegBlastRadius():F1} + rest margin {BlastRestMargin}. " +
                            "Any stray detonation splashes a core nobody aimed at.");
                    }
                }
            }
        }

        [Test]
        public void LaunchedBodyFootprint_MatchesTheLiveDefectTraceScale()
        {
            // The footprint band only protects the muzzle if the resolver keeps reporting
            // the REAL launched collider. The live trace measured the Knight at 2.64u
            // half-width (5.28u collider vs 0.93u rendered art — the documented mismatch).
            // If this shrinks, someone has done the collider/art rebalance: delete this
            // guard together with that work, and re-derive every keg position, because the
            // footprint band and possibly the removed ±11 wing kegs become renegotiable.
            Bounds knight = UnitController.EstimateLaunchedWorldColliderBounds(
                LoadPrefab("Assets/Prefabs/Knight.prefab"));
            Assert.That(knight.extents.x, Is.EqualTo(2.64f).Within(0.15f),
                "Knight launched half-width moved — the keg placement bands were derived " +
                "against 2.64u (live trace 2026-08-12). Re-derive stage keg positions.");
        }
    }
}
