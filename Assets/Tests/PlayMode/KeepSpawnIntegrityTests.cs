using System.Collections;
using System.Collections.Generic;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// What the keep profile promises has to actually stand on the field. Every previous check
    /// on the keep was arithmetic over KeepProfile — that the courses step up, clear the core,
    /// clear the launch rings — and none of them opened the scene to see whether the blocks
    /// arrived. A rejected spawn only writes a warning, so a keep can come up short and the
    /// suite stays green while the fortress visibly is not there.
    /// </summary>
    public class KeepSpawnIntegrityTests
    {
        static List<DestructibleBlock> WallBlocks(bool playerSide)
        {
            var found = new List<DestructibleBlock>();
            foreach (var block in Object.FindObjectsOfType<DestructibleBlock>())
            {
                // Wall columns are parented under a PlayerWall/EnemyWall root by SpawnCastleWall;
                // field obstacles and kegs are not, which is what separates them here.
                var parent = block.transform.parent;
                if (parent == null) continue;
                if (parent.name != (playerSide ? "PlayerWall" : "EnemyWall")) continue;
                if (block.GetComponent<CastleCoreGimmick>() != null) continue;
                found.Add(block);
            }
            return found;
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator Keep_SpawnsEveryBlockTheProfilePromises()
        {
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            var deadline = Time.realtimeSinceStartup + 8f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(GameManager.Instance, "the scene must bring up a GameManager");

            GameManager.Instance.BeginSiege();
            yield return null;

            int expected = GameManager.BlocksPerKeep(GameManager.Instance.ActiveLayout.wallHeightBlocks);

            Assert.AreEqual(expected, WallBlocks(true).Count,
                "the player keep is short of the blocks its profile declares — a spawn was refused");
            Assert.AreEqual(expected, WallBlocks(false).Count,
                "the enemy keep is short of the blocks its profile declares — a spawn was refused");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator Keep_StandsOnTheGroundRatherThanInTheAir()
        {
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            var deadline = Time.realtimeSinceStartup + 8f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(GameManager.Instance);

            GameManager.Instance.BeginSiege();
            yield return null;

            foreach (bool playerSide in new[] { true, false })
            {
                var blocks = WallBlocks(playerSide);
                Assert.IsNotEmpty(blocks, "a keep with no wall blocks is not a keep");

                float lowest = float.MaxValue;
                foreach (var block in blocks) lowest = Mathf.Min(lowest, block.transform.position.y);

                // SpawnCastleWall stacks from y = 0.5, one unit per block, so the bottom of a
                // column belongs at 0.5. Anything higher means the column is hanging in the air.
                Assert.AreEqual(0.5f, lowest, 0.35f,
                    $"the {(playerSide ? "player" : "enemy")} keep's lowest block sits at y={lowest:F2}, not on the ground");
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator LiveBoard_HasNoKegRestingOnMasonryOrInsideCoreBlast()
        {
            // KegPlacementSafetyTests proves the same invariant over StageLayout.barrelPositions,
            // and that was not enough: SampleScene had two kegs authored straight into it at
            // (±7, 1.5) — on the inner wall column, 2.0u from a core with a 2.2u blast. A table
            // audit cannot see a scene, so for two stages the fortress detonated its own core
            // whenever a friendly arrow clipped one. This opens the board and looks.
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            var deadline = Time.realtimeSinceStartup + 8f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(GameManager.Instance);

            GameManager.Instance.BeginSiege();
            yield return null;
            yield return null;   // Destroy() lands at end of frame; look after it has.

            foreach (var keg in Object.FindObjectsOfType<ExplosiveGimmick>())
            {
                if (keg == null) continue;
                float x = keg.transform.position.x;

                foreach (float coreX in new[] { -GameManager.CoreAbsX, GameManager.CoreAbsX })
                {
                    float distance = Vector2.Distance(
                        new Vector2(x, keg.transform.position.y), new Vector2(coreX, 0.5f));
                    Assert.Greater(distance, keg.explosionRadius,
                        $"a keg on the live board rests {distance:F2}u from the core at x={coreX}, " +
                        $"inside its own {keg.explosionRadius:F1}u blast — one stray hit splashes a " +
                        "core nobody aimed at");
                }

                foreach (var course in GameManager.KeepProfile)
                {
                    Assert.Greater(Mathf.Abs(Mathf.Abs(x) - course.AbsX), 0.5f,
                        $"a keg on the live board sits at x={x:F2}, on the wall column at " +
                        $"|x|={course.AbsX} — kegs belong in the open field, not in the masonry");
                }
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator LiveBoard_KeepsEveryKegTheStageTableAuthored()
        {
            // The inverse of the test above: illegal kegs must vanish, legal ones must arrive.
            // Stage3 is the only stage that authors kegs, so it is the only one that notices.
            //
            // Scope, measured rather than assumed: this does NOT detect the sweep being
            // reordered after the spawn loop. That was checked directly on 2026-08-13 by
            // reordering the call and re-running — still green, because Stage3's kegs clear
            // the sweep's thresholds by 0.5u and simply are not eaten today. What it DOES
            // catch is the case that reorder was only a proxy for: a keg the table placed
            // legally failing to reach the board, whether from a sweep that grew hungrier,
            // a spawn that silently refused, or a stage edit that walked one into the keep.
            GameManager.PendingStage = StageId.Stage3;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            var deadline = Time.realtimeSinceStartup + 8f;
            while (GameManager.Instance == null && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsNotNull(GameManager.Instance);

            GameManager.Instance.BeginSiege();
            yield return null;
            yield return null;   // let any Destroy() land before counting

            int authored = StageDefinitions.Stage3.barrelPositions.Length;
            Assert.Greater(authored, 0, "this pin is meaningless on a stage that authors no kegs");

            int live = 0;
            foreach (var keg in Object.FindObjectsOfType<ExplosiveGimmick>())
            {
                if (keg != null) live++;
            }

            Assert.AreEqual(authored, live,
                $"Stage3 authored {authored} kegs but {live} stand on the board — a keg the " +
                "stage table placed legally never reached the field.");
        }
    }
}
