using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CastleBusters;

namespace CastleBusters.Tests
{
    /// <summary>
    /// EditMode pins for Stage2 ("Ashen Bastion")'s field-obstacle schedule via the
    /// stage-aware GimmickFieldDirector.PlanForTurn(turn, aliveCount, maxObstacles, StageId)
    /// overload. Stage2 shares Stage1's 4-kind rotation (no SpikeTrap — the bastion's
    /// identity is spacing/walls, not a new hazard) but mutates every 2nd turn instead of
    /// every 3rd, so the board never rests twice in a row. These tests pin that cadence and
    /// the shared capacity-respecting fallthrough in PlanForTurnGeneric. Stage3's behavior
    /// via the same overload is covered elsewhere; this file is Stage2-only.
    /// </summary>
    public class Stage2FieldRulesTests
    {
        [Test]
        public void Stage2_MutatesEveryOtherTurn_FasterThanStage1()
        {
            const int aliveCount = 3;
            const int maxObstacles = 6;

            // Stage2: mutateEveryNTurns=2 -> mutate on every even turn (aliveCount>0 throughout).
            // Stage1: mutateEveryNTurns=3 -> mutate only on turns 3/6/9/12.
            for (int turn = 1; turn <= 12; turn++)
            {
                var stage2Plan = GimmickFieldDirector.PlanForTurn(turn, aliveCount, maxObstacles, StageId.Stage2);
                var stage1Plan = GimmickFieldDirector.PlanForTurn(turn, aliveCount, maxObstacles, StageId.Stage1);

                bool expectedStage2Mutate = turn % 2 == 0;
                bool expectedStage1Mutate = turn % 3 == 0;

                Assert.AreEqual(expectedStage2Mutate, stage2Plan.mutate,
                    $"Stage2 turn {turn}: mutate cadence must be every 2nd turn");
                Assert.AreEqual(expectedStage1Mutate, stage1Plan.mutate,
                    $"Stage1 turn {turn}: mutate cadence must remain every 3rd turn (unchanged baseline)");
            }
        }

        [Test]
        public void Stage2_NeverProducesBarrelOrMiniTowerKind()
        {
            for (int turn = 0; turn <= 30; turn++)
            {
                var plan = GimmickFieldDirector.PlanForTurn(turn, 0, 6, StageId.Stage2);
                Assert.AreNotEqual(FieldObstacleKind.Barrel, plan.kind,
                    $"Stage2 turn {turn}: must never produce Barrel, its identity is traps and runes");
                Assert.AreNotEqual(FieldObstacleKind.MiniTower, plan.kind,
                    $"Stage2 turn {turn}: must never produce MiniTower, its identity is traps and runes");
            }
        }

        [Test]
        public void Stage2_CoversAllThreeKinds_WithinFirstFewTurns()
        {
            var kinds = new HashSet<FieldObstacleKind>();
            for (int turn = 0; turn <= 10; turn++)
            {
                var plan = GimmickFieldDirector.PlanForTurn(turn, 0, 6, StageId.Stage2);
                kinds.Add(plan.kind);
            }

            Assert.AreEqual(3, kinds.Count, "Stage2 must cycle through exactly 3 obstacle kinds");
            Assert.IsFalse(kinds.Contains(FieldObstacleKind.Barrel));
            Assert.IsFalse(kinds.Contains(FieldObstacleKind.MiniTower));
            Assert.IsTrue(kinds.Contains(FieldObstacleKind.Rune));
            Assert.IsTrue(kinds.Contains(FieldObstacleKind.Patrol));
            Assert.IsTrue(kinds.Contains(FieldObstacleKind.SpikeTrap));
        }

        [Test]
        public void Stage2_RestBeatOnOddNonSpawnTurns_NeverDespawnsBelowCapacity()
        {
            // Below capacity: turn 2 is a mutate beat (aliveCount>0 -> mutate+spawn),
            // turn 3 is an odd non-mutate-multiple turn -> spawn only (still room to grow).
            var t2BelowCap = GimmickFieldDirector.PlanForTurn(2, 1, 4, StageId.Stage2);
            Assert.IsTrue(t2BelowCap.mutate, "turn 2: aliveCount>0 must mutate");
            Assert.IsTrue(t2BelowCap.spawn, "turn 2: mutate beat always spawns the replacement");

            var t3BelowCap = GimmickFieldDirector.PlanForTurn(3, 1, 4, StageId.Stage2);
            Assert.IsTrue(t3BelowCap.spawn, "turn 3 below capacity must spawn");
            Assert.IsFalse(t3BelowCap.despawnOldest, "turn 3 below capacity must not despawn");

            // At capacity: turn 3 (odd, not a mutate multiple) must despawn instead of overflow.
            var t3AtCap = GimmickFieldDirector.PlanForTurn(3, 4, 4, StageId.Stage2);
            Assert.IsFalse(t3AtCap.spawn, "turn 3 at capacity must not spawn");
            Assert.IsTrue(t3AtCap.despawnOldest, "turn 3 at capacity must despawn to make room");
        }

        [Test]
        public void Stage2_Turn0_IsAlwaysANoOp()
        {
            var plan = GimmickFieldDirector.PlanForTurn(0, 0, 6, StageId.Stage2);
            Assert.IsFalse(plan.spawn, "turn 0 must be a no-op guard: no spawn");
            Assert.IsFalse(plan.mutate, "turn 0 must be a no-op guard: no mutate");
            Assert.IsFalse(plan.despawnOldest, "turn 0 must be a no-op guard: no despawn");
        }
    }
}
