using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Measures what a live block DRAWS versus what it COLLIDES with.
    ///
    /// An EditMode probe reported a 12.54x mismatch, but that was an artifact: Object.Instantiate
    /// does not run Awake outside play mode, so it measured the authored prefab rather than a live
    /// block. This runs in play mode, where the production sizing path actually executes.
    /// </summary>
    public class BlockSizeProbeTests
    {
        [UnityTest]
        public IEnumerator LiveBlock_DrawsAtItsColliderSize()
        {
            var prefab = Resources.Load<GameObject>("DestructibleBlock");
            Assert.IsNotNull(prefab, "Resources/DestructibleBlock must exist.");

            var go = Object.Instantiate(prefab);
            yield return null;                 // let Awake/Start run
            Physics2D.SyncTransforms();

            var sr = go.GetComponent<SpriteRenderer>();
            var box = go.GetComponent<BoxCollider2D>();
            Assert.IsNotNull(sr, "block must have a SpriteRenderer");
            Assert.IsNotNull(box, "block must have a BoxCollider2D");
            Assert.IsNotNull(sr.sprite, "block must resolve a sprite");

            Vector2 drawn = sr.bounds.size;
            Vector2 collided = box.bounds.size;
            Debug.Log($"[BlockSize] scale={go.transform.localScale} drawn={drawn} collider={collided} " +
                      $"ratio=({drawn.x / collided.x:F3}, {drawn.y / collided.y:F3})");

            Object.Destroy(go);

            Assert.AreEqual(collided.x, drawn.x, 0.02f,
                "A block must draw at the width it collides with — art wider than its collision box " +
                "is the 'bricks are too big' defect.");
            Assert.AreEqual(collided.y, drawn.y, 0.02f,
                "A block must draw at the height it collides with.");
        }

        /// <summary>
        /// UpdateVisuals() swaps in the cracked / heavily-cracked sprites directly and does not
        /// re-run the sizing pass. If that art is authored at different native bounds than the
        /// intact art, a block silently changes rendered size the moment it takes damage while
        /// its collision box stays put.
        /// </summary>
        [UnityTest]
        public IEnumerator DamagedBlock_KeepsDrawingAtItsColliderSize()
        {
            var prefab = Resources.Load<GameObject>("DestructibleBlock");
            Assert.IsNotNull(prefab, "Resources/DestructibleBlock must exist.");

            var go = Object.Instantiate(prefab);
            yield return null;
            Physics2D.SyncTransforms();

            var sr = go.GetComponent<SpriteRenderer>();
            var box = go.GetComponent<BoxCollider2D>();
            var block = go.GetComponent<DestructibleBlock>();
            Assert.IsNotNull(block, "block must have a DestructibleBlock");

            Vector2 intact = sr.bounds.size;

            // Walk the damage bands the way play does: through public damage, not by poking sprites.
            for (int i = 0; i < 3; i++)
            {
                block.TakeDamage(block.maxHP * 0.3f, true);
                yield return null;
                Physics2D.SyncTransforms();

                if (sr == null || box == null || sr.sprite == null) break;   // destroyed: nothing to measure

                Vector2 drawn = sr.bounds.size;
                Vector2 collided = box.bounds.size;
                Debug.Log($"[BlockSize] band{i} sprite='{sr.sprite.name}' drawn={drawn} collider={collided} " +
                          $"ratio=({drawn.x / collided.x:F3}, {drawn.y / collided.y:F3})");

                Assert.AreEqual(collided.x, drawn.x, 0.02f,
                    $"After damage step {i} the block must still draw at its collision width.");
                Assert.AreEqual(collided.y, drawn.y, 0.02f,
                    $"After damage step {i} the block must still draw at its collision height.");
            }

            if (go != null) Object.Destroy(go);
            yield return null;
        }
    }
}
