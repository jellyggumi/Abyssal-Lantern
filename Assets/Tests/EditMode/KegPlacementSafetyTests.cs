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

        /// <summary>
        /// Rest-position margin over the blast radius.
        ///
        /// Was 0.25u until 2026-08-13, on the reasoning that "drifted-then-detonated is
        /// earned play, resting splash is not". A live PlayMode probe disproved the split:
        /// the ±6.5 keg rested 2.5u from a 2.2u blast — inside this margin — and a friendly
        /// GARRISON ARCHER's stray arrow detonated it into its own core for 80. Nobody
        /// earned that; the keg simply sat close enough that ordinary crossfire reached it.
        /// A resting keg must clear the blast by the same drift allowance the muzzle band
        /// uses, because combat moves kegs whether or not anyone aimed at them.
        /// </summary>
        private const float BlastRestMargin = DriftMargin;

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
        public void EveryStage_KegsRestOutsideEveryKeepWallColumn()
        {
            // The invariant this suite was missing, and the one that actually broke twice:
            // a keg authored INSIDE a wall column is depenetrated out of it on the first
            // physics step, and the wall stands between the muzzle and the core — so the
            // ejection is always COREWARD. Measured live 2026-08-13: kegs authored at ±6.5
            // and at ±5.8 both came to rest at −7.13, 2.18u from a 2.2u blast, and splashed
            // their own core for 80 with nobody aiming at anything. Clearance measured at
            // spawn is meaningless if the spawn point is inside masonry.
            float kegHalf = KegHalfWidth();

            foreach (var stage in new[] { StageDefinitions.Stage1, StageDefinitions.Stage2, StageDefinitions.Stage3 })
            {
                foreach (var keg in stage.barrelPositions)
                {
                    foreach (var course in GameManager.KeepProfile)
                    {
                        foreach (float columnX in new[] { -course.AbsX, course.AbsX })
                        {
                            // Blocks are 1u wide, so a column owns ±0.5u around its centre.
                            float required = 0.5f + kegHalf;
                            Assert.Greater(Mathf.Abs(keg.x - columnX), required,
                                $"{stage.displayName}: keg at x={keg.x} overlaps the keep column at " +
                                $"x={columnX} (needs >{required:F2}u). Physics will eject it toward the " +
                                "core and its blast will land on masonry nobody attacked.");
                        }
                    }
                }
            }
        }

        [Test]
        public void LaunchedBodyFootprint_EqualsTheAuthoredBodyAndStaysUnderOneBlock()
        {
            // History: this guard was written on 2026-08-12 pinning 2.64u, the half-width a
            // Knight ACTUALLY had when its collider was derived from whichever sprite was
            // current — 5.28u of hitbox behind 0.93u of art, which is how a launched knight
            // detonated a keg 3.3u away on frame 1. The 2026-08-13 pass replaced that rule
            // with an authored body size, so the guard now pins the property that matters:
            // the launched footprint IS the authored body, and a soldier is not wider than
            // the wall blocks it is thrown at.
            var prefab = LoadPrefab("Assets/Prefabs/Knight.prefab");
            var unit = prefab.GetComponent<UnitController>();
            Assert.IsNotNull(unit, "the knight prefab must carry its UnitController");

            Bounds knight = UnitController.EstimateLaunchedWorldColliderBounds(prefab);
            Vector2 authored = UnitController.BodyWorldColliderSize(
                unit.bodyWorldHeight, unit.colliderVisualCoverage);

            Assert.That(knight.size.x, Is.EqualTo(authored.x).Within(0.001f),
                "the launched footprint must equal the authored body — no sprite may resize it");
            Assert.That(knight.size.y, Is.EqualTo(authored.y).Within(0.001f),
                "the launched footprint must equal the authored body — no sprite may resize it");
            Assert.That(knight.size.x, Is.LessThan(1.5f),
                "a soldier wider than ~1.5u re-opens the muzzle-footprint defect family: it " +
                "starts overlapping neighbouring board furniture the moment it spawns");
        }

        [Test]
        public void SpawnFieldBarrel_ShippedPrefab_IsNormalizedToFallbackGameplayContract()
        {
            var prefab = LoadPrefab("Assets/Prefabs/ExplosiveBarrel.prefab");
            var managerObject = new GameObject("BarrelPrefabParityGameManager")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            managerObject.SetActive(false);
            GameObject spawned = null;

            try
            {
                var gameManager = managerObject.AddComponent<GameManager>();
                gameManager.explosiveBarrelPrefab = prefab;

                spawned = gameManager.SpawnFieldBarrel(new Vector3(123f, 45f, 0f));

                Assert.That(spawned, Is.Not.Null,
                    "SpawnFieldBarrel must return the shipped prefab instance it created.");

                var block = spawned.GetComponent<DestructibleBlock>();
                Assert.That(block, Is.Not.Null,
                    "A field Barrel must be damageable regardless of whether it came from the prefab or fallback path.");
                Assert.That(block.maxHP, Is.EqualTo(20f),
                    "The prefab path must normalize the Barrel to the fallback's 20 maximum HP.");
                Assert.That(block.currentHP, Is.EqualTo(20f),
                    "The prefab path must normalize the live Barrel to full 20 HP.");
                Assert.That(block.scoreValue, Is.EqualTo(50),
                    "Destroying a prefab-backed field Barrel must award the same 50 score as the fallback.");

                var body = spawned.GetComponent<Rigidbody2D>();
                Assert.That(body, Is.Not.Null,
                    "A field Barrel must retain the physical body used by collapse and impact gameplay.");
                Assert.That(body.mass, Is.EqualTo(2f),
                    "The prefab path must normalize Barrel mass to the fallback's 2 units.");
                Assert.That(body.constraints, Is.EqualTo(
                        RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation),
                    "A field Barrel must freeze horizontal drift and rotation while leaving vertical collapse legal.");

                Assert.That(spawned.GetComponent<ExplosiveGimmick>(), Is.Not.Null,
                    "A field Barrel must retain its explosion behavior after prefab normalization.");
            }
            finally
            {
                if (spawned != null) Object.DestroyImmediate(spawned);
                Object.DestroyImmediate(managerObject);
            }
        }
    }
}
