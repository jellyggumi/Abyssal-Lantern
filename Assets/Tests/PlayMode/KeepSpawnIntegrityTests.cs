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
    }
}
