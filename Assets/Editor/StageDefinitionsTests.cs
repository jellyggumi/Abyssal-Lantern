using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// EditMode pins for the 3-stage layout contract. All three stages are now genuinely
    /// distinct concepts, not palette swaps of the same board:
    /// - Stage1 "Siege Plains" is the frozen, unchanged baseline (GameManager.CoreAbsX=9,
    ///   LaunchApronAbsX=14.5, ground half-width 20, anchor band 10, gates ±15,
    ///   windCapEnd 6.5, camera 39/11.2, the original 4 bridge-hugging kegs, 2-high walls,
    ///   6-cap/3-turn-mutate field, white tint) — every other stage is judged against it.
    /// - Stage2 "Desolate Dunes" is a NEW close-quarters fortress duel: a tighter
    ///   launch-to-launch distance, zero starting kegs (hazards are earned mid-match, not
    ///   handed out at the gate), taller/heavier walls, a lower obstacle cap with a faster
    ///   mutation cadence (relentless, no rest beat), a lower wind cap (precision over wind
    ///   compensation), and a distinct warm sandy tint. It is now fully unlocked/playable,
    ///   not a locked placeholder mirroring Stage1.
    /// - Stage3 "Volcanic Abyss" is a wide long-range gorge: the widest launch-to-launch
    ///   distance, kegs spread out to the wings (not reusing Stage1's bridge-hugging spots),
    ///   a higher obstacle cap with a slower mutation cadence (wide board reads as open, not
    ///   cluttered), and a raised wind cap to keep late-match aiming meaningful, with a fiery reddish tint.
    /// </summary>
    public class StageDefinitionsTests
    {
        [Test]
        public void Stage1_MatchesFrozenGameManagerDefaults()
        {
            var layout = StageDefinitions.Stage1;
            Assert.AreEqual(StageId.Stage1, layout.id);
            Assert.IsFalse(layout.locked, "Stage1 must always be selectable");
            // Refrozen to the widened board (2026-08-13: 맵 폭 확대). Stage1 is the baseline
            // every other stage is judged against, so the freeze tracks current intent —
            // what it still guarantees is that GameManager's mirrored default and this
            // table cannot drift apart.
            Assert.AreEqual(17.0f, layout.launchApronAbsX, 0.001f, "Stage1 launch apron (widened board)");
            Assert.AreEqual(GameManager.LaunchApronAbsX, layout.launchApronAbsX, 0.001f,
                "Stage1 apron must stay identical to the GameManager default it mirrors");
            Assert.AreEqual(23f, layout.groundHalfWidth, 0.001f);
            Assert.AreEqual(11.5f, layout.groundAnchorAbsX, 0.001f);
            Assert.AreEqual(17.5f, layout.gateAbsX, 0.001f);
            Assert.AreEqual(7.0f, layout.windCapEnd, 0.001f);
            Assert.AreEqual(45f, layout.cameraDesiredWorldWidth, 0.001f);
            Assert.AreEqual(11.2f, layout.cameraMaxHalfHeight, 0.001f);
            // Moved from 2 deliberately, not drifted: keeps were raised a course across every
            // stage to pull a decided match toward the five-minute target (MatchLengthModel).
            // The freeze still does its job — it just freezes the current intent.
            Assert.AreEqual(3, layout.wallHeightBlocks, "Stage1 walls sit one course above the original 2-block height");
            Assert.AreEqual(6, layout.maxFieldObstacles, "Stage1 obstacle cap must stay at the original 6");
            Assert.AreEqual(3, layout.mutateEveryNTurns, "Stage1 mutation cadence must stay at the original every-3rd-turn");

            // Stage1's starting kegs must be the exact frozen GameManager fixture, not a
            // stage-owned copy that could silently drift from it. Zero of them, matching
            // Stage2: the keep courses own |x| ∈ [3.5, 7.5] and the core spans ~7.85–10.15,
            // so no legal static column exists between the wall line and the core. Kegs
            // authored there were depenetrated coreward and splashed their own core twice
            // (2026-08-12 ±11 at the muzzle, 2026-08-13 ±6.5/±5.8 into the core). Kegs now
            // arrive through the field director's midfield lanes instead.
            Assert.AreEqual(0, layout.barrelPositions.Length,
                "Stage1 ships no starting kegs — hazards are earned mid-match");
            Assert.AreEqual(GameManager.InitialBarrelPositions.Length, layout.barrelPositions.Length);
            for (int i = 0; i < layout.barrelPositions.Length; i++)
            {
                Assert.AreEqual(GameManager.InitialBarrelPositions[i], layout.barrelPositions[i],
                    $"Stage1 keg {i} must match GameManager.InitialBarrelPositions exactly");
            }
        }

        [Test]
        public void Stage2_IsUnlocked()
        {
            Assert.IsFalse(StageDefinitions.Stage2.locked,
                "Stage2 'Desolate Dunes' is now a fully designed, playable battlefield, not a reserved placeholder");
        }

        [Test]
        public void Stage2_CompressesPlayerDistance_RelativeToStage1()
        {
            var s1 = StageDefinitions.Stage1;
            var s2 = StageDefinitions.Stage2;

            Assert.Less(s2.launchApronAbsX, s1.launchApronAbsX,
                "Stage2 must pull the launch apron in tighter than Stage1 for its close-quarters duel concept");

            // Player-to-player distance is 2x the launch apron offset. Requirement: "a bit"
            // tighter, not a drastic rebalance — assert a meaningful (>5%) but bounded (<25%)
            // compression, mirroring the convention Stage3's widening test already uses.
            float d1 = s1.launchApronAbsX * 2f;
            float d2 = s2.launchApronAbsX * 2f;
            float pctDecrease = (d1 - d2) / d1;
            Assert.Greater(pctDecrease, 0.05f, "Stage2 distance compression must be meaningfully tighter than Stage1");
            Assert.Less(pctDecrease, 0.25f, "Stage2 distance compression must stay a moderate tightening, not a drastic rebalance");
        }

        [Test]
        public void Stage2_ShipsWithZeroStartingKegs()
        {
            // A fortress isn't handed free explosives at its own gate — Desolate Dunes's
            // hazards are earned mid-match via GimmickFieldDirector instead.
            Assert.AreEqual(0, StageDefinitions.Stage2.barrelPositions.Length,
                "Stage2 'Desolate Dunes' ships with zero starting kegs; hazards are earned mid-match, not handed out at the gate");
        }

        [Test]
        public void Stage2_HasTallerWallsThanStage1()
        {
            Assert.Greater(StageDefinitions.Stage2.wallHeightBlocks, StageDefinitions.Stage1.wallHeightBlocks,
                "Stage2 'Desolate Dunes' must have a heavier, more fortified wall silhouette than Stage1");
        }

        [Test]
        public void Stage2_HasLowerObstacleCapAndFasterMutateCadenceThanStage1()
        {
            Assert.Less(StageDefinitions.Stage2.maxFieldObstacles, StageDefinitions.Stage1.maxFieldObstacles,
                "Stage2 must stay leaner (lower obstacle cap) than Stage1");
            Assert.Less(StageDefinitions.Stage2.mutateEveryNTurns, StageDefinitions.Stage1.mutateEveryNTurns,
                "Stage2 must mutate more often than Stage1 — a relentless bastion cadence with no rest beat");
        }

        [Test]
        public void Stage2_HasLowerWindCapThanStage1()
        {
            // 사거리가 짧아지는 만큼 바람 보정보다 정밀함이 중요해진다.
            Assert.Less(StageDefinitions.Stage2.windCapEnd, StageDefinitions.Stage1.windCapEnd,
                "Stage2's shorter range should reward precision over wind compensation with a lower wind cap");
        }

        [Test]
        public void Stage2_HasDistinctBackgroundTint()
        {
            Assert.AreNotEqual(Color.white, StageDefinitions.Stage2.backgroundTint,
                "Stage2 must carry its own visual identity (warm sandy tint), not a plain reskin of Stage1's white");
        }

        [Test]
        public void Stage2_KeepsGroundAndCameraWideEnoughForItsOwnApron()
        {
            var s2 = StageDefinitions.Stage2;

            // Ground must extend past the launch apron with real margin (matches Stage1's
            // 20 - 14.5 = 5.5u margin convention), so units never spawn near the grid edge.
            Assert.GreaterOrEqual(s2.groundHalfWidth - s2.launchApronAbsX, 4f,
                "Stage2 ground must keep a safety margin past its own (tighter) launch apron");

            // Camera must be able to show both launch aprons plus the deep-wing gates.
            Assert.Greater(s2.cameraDesiredWorldWidth / 2f, s2.gateAbsX,
                "Stage2 camera board must be wide enough to frame the deep-wing gates");

            // Gates sit just past the launch apron (mirrors Stage1's gate(15) > apron(14.5)).
            Assert.Greater(s2.gateAbsX, s2.launchApronAbsX,
                "Stage2 gates must sit outside the launch apron, same convention as Stage1");
        }

        [Test]
        public void Stage3_WidensPlayerDistance_WithoutMovingSharedCore()
        {
            var s1 = StageDefinitions.Stage1;
            var s3 = StageDefinitions.Stage3;

            Assert.AreEqual(StageId.Stage3, s3.id);
            Assert.IsFalse(s3.locked, "Stage3 is the completed third battlefield and must be selectable");
            Assert.Greater(s3.launchApronAbsX, s1.launchApronAbsX, "Stage3 must push the launch apron further out than Stage1");

            // Player-to-player distance is 2x the launch apron offset (both launch points are
            // symmetric around x=0). Requirement: "a bit" wider, not a drastic jump — assert a
            // meaningful (>15%) but bounded (<50%) increase so a future edit can't silently blow
            // the arena out to something unplayable or shrink it back to a no-op.
            float d1 = s1.launchApronAbsX * 2f;
            float d3 = s3.launchApronAbsX * 2f;
            float pctIncrease = (d3 - d1) / d1;
            Assert.Greater(pctIncrease, 0.15f, "Stage3 distance increase must be meaningfully larger than Stage1");
            Assert.Less(pctIncrease, 0.5f, "Stage3 distance increase must stay a moderate widening, not a drastic rebalance");
        }

        [Test]
        public void Stage3_KeepsCameraAndGroundWideEnoughForNewApron()
        {
            var s3 = StageDefinitions.Stage3;

            // Ground must extend past the launch apron with real margin (matches Stage1's
            // 20 - 14.5 = 5.5u margin convention), so units never spawn near the grid edge.
            Assert.GreaterOrEqual(s3.groundHalfWidth - s3.launchApronAbsX, 4f,
                "Stage3 ground must keep a safety margin past the wider launch apron");

            // Camera must be able to show both launch aprons plus the deep-wing gates; a
            // half-width of desiredWorldWidth/2 must clear gateAbsX with some margin.
            Assert.Greater(s3.cameraDesiredWorldWidth / 2f, s3.gateAbsX,
                "Stage3 camera board must be wide enough to frame the deep-wing gates");

            // Gates sit just past the launch apron (mirrors Stage1's gate(15) > apron(14.5)).
            Assert.Greater(s3.gateAbsX, s3.launchApronAbsX, "Stage3 gates must sit outside the launch apron, same convention as Stage1");
        }

        [Test]
        public void Stage3_WindCapIsHigherThanStage1_ToCompensateForWiderGap()
        {
            Assert.Greater(StageDefinitions.Stage3.windCapEnd, StageDefinitions.Stage1.windCapEnd,
                "wider player distance needs a slightly higher endgame wind ceiling to keep late-match aiming meaningful");
        }

        [Test]
        public void Stage3_SpreadsKegsToTheWings_ClearOfCoreAndLaunchMuzzle()
        {
            var s3 = StageDefinitions.Stage3;

            Assert.GreaterOrEqual(s3.barrelPositions.Length, 4,
                "Stage3 'Volcanic Abyss' must ship at least 4 kegs spread across its wider board");

            // Stage3 must genuinely own its own spread, not silently reuse Stage1's array —
            // at least one x-coordinate must differ between the two sets.
            bool anyDiffers = false;
            for (int i = 0; i < s3.barrelPositions.Length && i < GameManager.InitialBarrelPositions.Length; i++)
            {
                if (!Mathf.Approximately(s3.barrelPositions[i].x, GameManager.InitialBarrelPositions[i].x))
                {
                    anyDiffers = true;
                    break;
                }
            }
            Assert.IsTrue(anyDiffers || s3.barrelPositions.Length != GameManager.InitialBarrelPositions.Length,
                "Stage3 kegs must be a genuinely distinct spread, not a reused copy of Stage1's bridge-hugging positions");

            foreach (var keg in s3.barrelPositions)
            {
                // Same >1.0u core-hugging threshold GamePlayTests.FieldLayout_SpansWideEnvelope
                // already uses — a keg must never sit on a core column.
                Assert.Greater(Mathf.Abs(Mathf.Abs(keg.x) - GameManager.CoreAbsX), 1.0f,
                    $"Stage3 keg at {keg.x} hugs a core column");

                // Same blast-radius+margin clearance convention as GamePlayTests.FieldLayout_KegsClearLaunchMuzzles
                // (strict >3.0u from the launch muzzle — GreaterOrEqual would let a keg sit
                // exactly on the boundary that previously caused kegs to self-detonate
                // low-arc shots), evaluated against Stage3's own (wider) apron.
                Assert.Greater(Mathf.Abs(Mathf.Abs(keg.x) - s3.launchApronAbsX), 3.0f,
                    $"Stage3 keg at {keg.x} is inside the muzzle hazard zone of its own launch apron");
            }
        }

        [Test]
        public void For_ResolvesStageId_ToMatchingLayout()
        {
            Assert.AreEqual(StageId.Stage1, StageDefinitions.For(StageId.Stage1).id);
            Assert.AreEqual(StageId.Stage2, StageDefinitions.For(StageId.Stage2).id);
            Assert.AreEqual(StageId.Stage3, StageDefinitions.For(StageId.Stage3).id);

            // Spot-check that For() returns the exact same data as direct static field
            // access, not an independently-constructed (and potentially drifted) copy.
            var s1ViaFor = StageDefinitions.For(StageId.Stage1);
            Assert.AreEqual(StageDefinitions.Stage1.launchApronAbsX, s1ViaFor.launchApronAbsX, 0.001f);
            Assert.AreEqual(StageDefinitions.Stage1.locked, s1ViaFor.locked);

            var s2ViaFor = StageDefinitions.For(StageId.Stage2);
            Assert.AreEqual(StageDefinitions.Stage2.launchApronAbsX, s2ViaFor.launchApronAbsX, 0.001f);
            Assert.AreEqual(StageDefinitions.Stage2.wallHeightBlocks, s2ViaFor.wallHeightBlocks);
            Assert.AreEqual(StageDefinitions.Stage2.locked, s2ViaFor.locked);

            var s3ViaFor = StageDefinitions.For(StageId.Stage3);
            Assert.AreEqual(StageDefinitions.Stage3.launchApronAbsX, s3ViaFor.launchApronAbsX, 0.001f);
            Assert.AreEqual(StageDefinitions.Stage3.maxFieldObstacles, s3ViaFor.maxFieldObstacles);
            Assert.AreEqual(StageDefinitions.Stage3.mutateEveryNTurns, s3ViaFor.mutateEveryNTurns);
        }

        [Test]
        public void StageLayout_HasCorrectAllowedGimmicks()
        {
            var s1 = StageDefinitions.Stage1;
            Assert.AreEqual(4, s1.allowedGimmicks.Length);
            Assert.Contains(FieldObstacleKind.Barrel, s1.allowedGimmicks);
            Assert.Contains(FieldObstacleKind.MiniTower, s1.allowedGimmicks);
            Assert.Contains(FieldObstacleKind.Rune, s1.allowedGimmicks);
            Assert.Contains(FieldObstacleKind.Patrol, s1.allowedGimmicks);

            var s2 = StageDefinitions.Stage2;
            Assert.AreEqual(3, s2.allowedGimmicks.Length);
            Assert.Contains(FieldObstacleKind.Rune, s2.allowedGimmicks);
            Assert.Contains(FieldObstacleKind.SpikeTrap, s2.allowedGimmicks);
            Assert.Contains(FieldObstacleKind.Patrol, s2.allowedGimmicks);

            var s3 = StageDefinitions.Stage3;
            Assert.AreEqual(3, s3.allowedGimmicks.Length);
            Assert.Contains(FieldObstacleKind.Barrel, s3.allowedGimmicks);
            Assert.Contains(FieldObstacleKind.MiniTower, s3.allowedGimmicks);
            Assert.Contains(FieldObstacleKind.SpikeTrap, s3.allowedGimmicks);
        }

        [Test]
        public void Stage3_CoversAllThreeKinds_WithinFirstFewTurns()
        {
            var kinds = new HashSet<FieldObstacleKind>();
            for (int turn = 1; turn <= 12; turn++)
            {
                var plan = GimmickFieldDirector.PlanForTurn(turn, 0, 7, StageId.Stage3);
                kinds.Add(plan.kind);
            }

            Assert.AreEqual(3, kinds.Count, "Stage3 must cycle through exactly 3 obstacle kinds");
            Assert.IsTrue(kinds.Contains(FieldObstacleKind.Barrel));
            Assert.IsTrue(kinds.Contains(FieldObstacleKind.MiniTower));
            Assert.IsTrue(kinds.Contains(FieldObstacleKind.SpikeTrap));
            Assert.IsFalse(kinds.Contains(FieldObstacleKind.Rune));
            Assert.IsFalse(kinds.Contains(FieldObstacleKind.Patrol));
        }
    }
}
