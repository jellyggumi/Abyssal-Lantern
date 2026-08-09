using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CastleBusters
{
    public class CastleController : MonoBehaviour
    {
        public bool isPlayerCastle = true;
        public float blockSizeX = 1.0f;
        public float blockSizeY = 1.0f;
        public float adjacencyEpsilon = 0.1f;

        private readonly List<DestructibleBlock> allBlocks = new List<DestructibleBlock>();
        private Coroutine integrityCheckCoroutine;

        /// <summary>Read-only view of the live block list for presentation observers
        /// (CastleRuinFx aggregate-HP milestones). Never mutate through this.</summary>
        public System.Collections.Generic.IReadOnlyList<DestructibleBlock> Blocks => allBlocks;

        private void Start() => RefreshBlockList();

        public void RefreshBlockList()
        {
            allBlocks.Clear();
            allBlocks.AddRange(GetComponentsInChildren<DestructibleBlock>());
            AutoAssignFoundationAnchors();
            // Position-aware facade skins (presentation-only; no-op until the generated
            // CastleSkin tiles exist under Resources/). Runs after anchors so scene-authored
            // castles, runtime walls and rebuilt lists all converge on the same look.
            CastleFacadeDirector.ApplySkins(this, allBlocks);
        }

        // Castle blocks are hand-placed scene content; none of them carry isGroundAnchor=true by
        // default (the prefab default is false, and no scene override sets it either). Without at
        // least one ground-anchored block, the structural-integrity BFS below always starts from an
        // empty "supported" set, so the very first block destroyed anywhere in the castle marks EVERY
        // remaining block - including the core - as falling, and they all drop past the
        // DestructibleBlock kill-plane (y < -10) within seconds: a total, near-instant collapse from a
        // single hit. Auto-anchoring the lowest row (the castle's foundation) instead makes collapse
        // depend on actually breaking through the base, matching the intended siege gameplay.
        private void AutoAssignFoundationAnchors()
        {
            if (allBlocks.Count == 0) return;

            float minY = allBlocks.Min(b => b.transform.position.y);
            foreach (var block in allBlocks)
            {
                if (Mathf.Abs(block.transform.position.y - minY) <= adjacencyEpsilon)
                {
                    block.isGroundAnchor = true;
                }
            }
        }


        public void OnBlockDestroyed(DestructibleBlock destroyedBlock)
        {
            allBlocks.Remove(destroyedBlock);
            if (GameManager.Instance != null) GameManager.Instance.CheckVictoryConditions();
            if (!Application.isPlaying) { RunStructuralIntegritySync(); return; }

            if (integrityCheckCoroutine != null) StopCoroutine(integrityCheckCoroutine);
            integrityCheckCoroutine = StartCoroutine(CheckStructuralIntegrityCoroutine());
        }

        [ContextMenu("Check Structural Integrity")]
        public void CheckStructuralIntegrity()
        {
            if (integrityCheckCoroutine != null) { StopCoroutine(integrityCheckCoroutine); integrityCheckCoroutine = null; }
            RunStructuralIntegritySync();
        }

        // Reused scratch buffers so per-destruction integrity checks allocate nothing.
        // Instance (not static) on purpose: the coroutine yields mid-BFS every 50 traversals,
        // and both castles can run integrity coroutines concurrently across frames — shared
        // static buffers would corrupt each other's traversal state.
        private readonly HashSet<DestructibleBlock> supported = new HashSet<DestructibleBlock>();
        private readonly Queue<DestructibleBlock> bfsQueue = new Queue<DestructibleBlock>();
        private readonly List<DestructibleBlock> toFall = new List<DestructibleBlock>();
        private readonly List<DestructibleBlock> neighborScratch = new List<DestructibleBlock>();

        private void SeedSupportedSet()
        {
            supported.Clear();
            bfsQueue.Clear();
            for (int i = 0; i < allBlocks.Count; i++)
            {
                var b = allBlocks[i];
                if (b.isGroundAnchor && !b.IsFalling)
                {
                    supported.Add(b);
                    bfsQueue.Enqueue(b);
                }
            }
        }

        private void ExpandSupportFrom(DestructibleBlock block)
        {
            GetNeighbors(block, neighborScratch);
            for (int i = 0; i < neighborScratch.Count; i++)
            {
                var neighbor = neighborScratch[i];
                if (supported.Contains(neighbor) || neighbor.IsFalling) continue;
                supported.Add(neighbor);
                bfsQueue.Enqueue(neighbor);
            }
        }

        private void CollectAndDropUnsupported()
        {
            toFall.Clear();
            for (int i = 0; i < allBlocks.Count; i++)
            {
                var b = allBlocks[i];
                if (!supported.Contains(b) && !b.IsFalling) toFall.Add(b);
            }
            for (int i = 0; i < toFall.Count; i++) toFall[i].MakeFall();
        }

        private void RunStructuralIntegritySync()
        {
            allBlocks.RemoveAll(item => item == null);
            SeedSupportedSet();

            while (bfsQueue.Count > 0) ExpandSupportFrom(bfsQueue.Dequeue());

            CollectAndDropUnsupported();
        }

        private IEnumerator CheckStructuralIntegrityCoroutine()
        {
            yield return new WaitForEndOfFrame();

            allBlocks.RemoveAll(item => item == null);
            SeedSupportedSet();

            int traversalsThisFrame = 0;

            while (bfsQueue.Count > 0)
            {
                ExpandSupportFrom(bfsQueue.Dequeue());

                if (++traversalsThisFrame >= 50)
                {
                    traversalsThisFrame = 0;
                    yield return null;
                }
            }

            CollectAndDropUnsupported();
            integrityCheckCoroutine = null;
        }


        private void GetNeighbors(DestructibleBlock block, List<DestructibleBlock> results)
        {
            results.Clear();
            var pos = block.transform.position;
            var maxX = blockSizeX + adjacencyEpsilon;
            var maxY = blockSizeY + adjacencyEpsilon;

            for (int i = 0; i < allBlocks.Count; i++)
            {
                var other = allBlocks[i];
                if (other == block || other.IsFalling) continue;
                if ((Mathf.Abs(pos.x - other.transform.position.x) <= maxX && Mathf.Abs(pos.y - other.transform.position.y) <= adjacencyEpsilon) ||
                    (Mathf.Abs(pos.y - other.transform.position.y) <= maxY && Mathf.Abs(pos.x - other.transform.position.x) <= adjacencyEpsilon))
                {
                    results.Add(other);
                }
            }
        }

        /// <summary>Public read-only adjacency for presentation observers (CastleRuinFx
        /// neighbor pulse). Same rules as the integrity BFS's GetNeighbors.</summary>
        public void CollectNeighbors(DestructibleBlock block, List<DestructibleBlock> results) =>
            GetNeighbors(block, results);
    }
}
