using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Interactive real-time ruin feedback for castle blocks — a presentation-only observer
    /// called from DestructibleBlock.TakeDamage/DestroyBlock. Reads simulation state, never
    /// writes it (CLAUDE.md §2): no colliders, HP, forces, or transforms of live Static
    /// blocks are ever touched. Three layers:
    ///  1. Per-hit crack decals: meaningful hits (≥8% maxHP) stamp a fading crack child
    ///     sprite near the impact, capped per block, dying with the block.
    ///  2. Band-crossing "crumble moments": crossing the 0.7 / 0.3 display thresholds
    ///     spawns debris of the block's own sprite + a shockwave — the moment the wall
    ///     visibly loses integrity, not just a number.
    ///  3. Castle wholeness milestones (75/50/25% of aggregate castle HP): castle-wide
    ///     dust wave + a presentation wear-floor ratchet at 50% (every surviving block
    ///     displays at least the cracked skin — the fortress LOOKS like a ruin even where
    ///     individual stones are healthy).
    /// All state is per-castle, keyed by object identity, and cleared each match via
    /// GameManager.StartGame → ResetForNewMatch (scene reloads spawn fresh objects).
    /// </summary>
    public static class CastleRuinFx
    {
        // --- Tunables (presentation cadence; no gameplay meaning) ---
        private const float DecalDamageFraction = 0.08f;  // hits below 8% maxHP leave no decal
        private const int MaxDecalsPerBlock = 3;
        private const float DecalFadeIn = 0.08f;
        private static readonly float[] Milestones = { 0.75f, 0.5f, 0.25f };
        private const int WearFloorAtOrBelowMilestone = 1; // 50% milestone → display band ≥ cracked

        private sealed class CastleRuinState
        {
            public float initialTotalHP;
            public int nextMilestone; // index into Milestones
        }

        private static readonly Dictionary<CastleController, CastleRuinState> states =
            new Dictionary<CastleController, CastleRuinState>();
        private static readonly Dictionary<DestructibleBlock, int> decalCounts =
            new Dictionary<DestructibleBlock, int>();
        private static readonly List<DestructibleBlock> neighborScratch = new List<DestructibleBlock>();

        // Domain-init guard for fast-enter-playmode setups (domain reload disabled).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetForNewMatch();

        /// <summary>Clears identity-keyed presentation state. Called from GameManager.StartGame's
        /// rematch-hygiene block: SubsystemRegistration alone fires on domain init, NOT on the
        /// SceneManager.LoadScene rematch loop, so without this hook destroyed blocks/castles
        /// would accumulate as dead dictionary keys across "one more run" reloads.</summary>
        public static void ResetForNewMatch()
        {
            states.Clear();
            decalCounts.Clear();
        }

        /// <summary>Damage observer. prev/new ratios are the block's own HP fractions before and
        /// after this hit (caller computes them so we never re-read mid-destruction state).</summary>
        public static void NotifyBlockDamaged(DestructibleBlock block, float damage, float prevRatio, float newRatio)
        {
            if (!Application.isPlaying || block == null) return;
            var castle = block.GetComponentInParent<CastleController>();
            if (castle == null) return; // ground tiles / free props: existing feedback is enough

            if (block.maxHP > 0f && damage >= block.maxHP * DecalDamageFraction)
                StampCrackDecal(block, damage);

            // Band-crossing crumble moment (display bands: 0.7 cracked, 0.3 crumbling).
            bool crossedCracked = prevRatio > 0.7f && newRatio <= 0.7f;
            bool crossedCrumbling = prevRatio > 0.3f && newRatio <= 0.3f;
            if ((crossedCracked || crossedCrumbling) && newRatio > 0f)
            {
                var sr = block.GetComponent<SpriteRenderer>();
                Color dustColor = block.blockData != null ? block.blockData.blockColor : Color.gray;
                if (DebrisPool.Instance != null)
                    DebrisPool.Instance.SpawnDebrisBurst(block.transform.position, dustColor, crossedCrumbling ? 6 : 3);
                GameFeelVfx.SpawnCollapseDust(block.transform.position, crossedCrumbling ? 1.3f : 0.9f,
                    sr != null ? sr.sprite : null);
                if (crossedCrumbling)
                    GameFeelVfx.SpawnShockwaveRing(block.transform.position, new Color(0.85f, 0.8f, 0.7f, 0.5f), 0.9f, 0.3f);
            }

            CheckWholenessMilestones(castle);
        }

        /// <summary>Destruction observer: neighbor color pulse + seam dust so the survivors
        /// visibly "feel" the loss. Neighbor discovery is the same adjacency the structural
        /// integrity BFS uses, exposed read-only via CastleController.CollectNeighbors.</summary>
        public static void NotifyBlockDestroyed(DestructibleBlock block, CastleController castle)
        {
            decalCounts.Remove(block);
            if (!Application.isPlaying || castle == null) return;

            castle.CollectNeighbors(block, neighborScratch);
            for (int i = 0; i < neighborScratch.Count; i++)
            {
                var n = neighborScratch[i];
                if (n == null) continue;
                var nsr = n.GetComponent<SpriteRenderer>();
                if (nsr != null && NeighborPulseRunner.Instance != null)
                    NeighborPulseRunner.Instance.Pulse(nsr);
                // Seam dust between the destroyed block and this survivor.
                Vector3 seam = (block.transform.position + n.transform.position) * 0.5f;
                GameFeelVfx.SpawnCollapseDust(seam, 0.55f, nsr != null ? nsr.sprite : null);
            }

            CheckWholenessMilestones(castle);
        }

        // --- Layer 1: crack decals ---

        private static void StampCrackDecal(DestructibleBlock block, float damage)
        {
            if (decalCounts.TryGetValue(block, out int count) && count >= MaxDecalsPerBlock) return;
            var sr = block.GetComponent<SpriteRenderer>();
            if (sr == null || sr.sprite == null) return;

            var crackSprite = EffectSpriteLibrary.LoadParticleSprite(EffectSpriteLibrary.ParticleSmoke);
            if (crackSprite == null) return;

            var go = new GameObject("CrackDecal");
            go.transform.SetParent(block.transform, false);
            // Impact-ish point: center + jitter, kept inside the block's local bounds.
            go.transform.localPosition = new Vector3(
                Random.Range(-0.3f, 0.3f) * sr.sprite.bounds.size.x,
                Random.Range(-0.3f, 0.3f) * sr.sprite.bounds.size.y, 0f);
            go.transform.localRotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));

            var decal = go.AddComponent<SpriteRenderer>();
            decal.sprite = crackSprite;
            decal.sortingOrder = 3; // above the block face (2), below units
            float scale = Mathf.Clamp(damage / 60f, 0.35f, 0.8f);
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var fade = go.AddComponent<DecalFadeIn>();
            fade.targetAlpha = Mathf.Clamp(0.25f + damage / 120f, 0.25f, 0.6f);
            fade.duration = DecalFadeIn;

            decalCounts[block] = count + 1;
        }

        // --- Layer 3: castle wholeness milestones ---

        private static void CheckWholenessMilestones(CastleController castle)
        {
            if (!states.TryGetValue(castle, out var state))
            {
                state = new CastleRuinState { initialTotalHP = SumMaxHP(castle), nextMilestone = 0 };
                states[castle] = state;
            }
            if (state.initialTotalHP <= 0f || state.nextMilestone >= Milestones.Length) return;

            float ratio = SumCurrentHP(castle) / state.initialTotalHP;
            while (state.nextMilestone < Milestones.Length && ratio <= Milestones[state.nextMilestone])
            {
                FireMilestone(castle, Milestones[state.nextMilestone]);
                state.nextMilestone++;
            }
        }

        private static void FireMilestone(CastleController castle, float milestone)
        {
            var blocks = castle.Blocks;
            // Castle-wide dust wave: staggered spatially by using each block's own position.
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b == null || b.IsFalling) continue;
                if (milestone <= WearMilestoneRatio())
                    b.SetDisplayWearFloor(WearFloorAtOrBelowMilestone);
                if ((i & 1) == 0) // every other block keeps the wave readable, not a whiteout
                {
                    var sr = b.GetComponent<SpriteRenderer>();
                    GameFeelVfx.SpawnCollapseDust(b.transform.position, 0.7f, sr != null ? sr.sprite : null);
                }
            }
            Vector3 center = castle.transform.position + Vector3.up * 1.5f;
            GameFeelVfx.SpawnShockwaveRing(center, new Color(0.9f, 0.85f, 0.75f, 0.5f), 2.2f, 0.5f);
            GameFeelVfx.SpawnFeedbackLabel(center,
                milestone <= 0.25f ? "CRUMBLING!" : (milestone <= 0.5f ? "BREACHED!" : "DAMAGED!"),
                new Color(1f, 0.78f, 0.3f, 1f), 2.3f, 0.65f);
            if (ScreenShakeManager.Instance != null)
                ScreenShakeManager.Instance.TriggerShake(0.4f, milestone <= 0.25f ? 0.3f : 0.18f);
        }

        private static float WearMilestoneRatio() => 0.5f;

        private static float SumMaxHP(CastleController castle)
        {
            float sum = 0f;
            var blocks = castle.Blocks;
            for (int i = 0; i < blocks.Count; i++)
                if (blocks[i] != null) sum += blocks[i].maxHP;
            return sum;
        }

        private static float SumCurrentHP(CastleController castle)
        {
            float sum = 0f;
            var blocks = castle.Blocks;
            for (int i = 0; i < blocks.Count; i++)
                if (blocks[i] != null) sum += Mathf.Max(0f, blocks[i].currentHP);
            return sum;
        }
    }

    /// <summary>Tiny fade-in for crack decals; self-removes when done, leaving the decal static.</summary>
    public class DecalFadeIn : MonoBehaviour
    {
        public float targetAlpha = 0.5f;
        public float duration = 0.08f;
        private float t;
        private SpriteRenderer sr;

        private void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            if (sr != null) { var c = sr.color; c.a = 0f; sr.color = c; }
        }

        private void Update()
        {
            if (sr == null) { Destroy(this); return; }
            t += Time.deltaTime;
            var c = sr.color;
            c.a = Mathf.Lerp(0f, targetAlpha, Mathf.Clamp01(t / duration));
            sr.color = c;
            if (t >= duration) Destroy(this);
        }
    }

    /// <summary>Scene-lifetime coroutine host for neighbor dim pulses (statics can't run
    /// coroutines; mirrors the GameFeelVfxCoroutineRunner precedent).</summary>
    public class NeighborPulseRunner : MonoBehaviour
    {
        private static NeighborPulseRunner instance;
        public static NeighborPulseRunner Instance
        {
            get
            {
                if (instance == null && Application.isPlaying)
                {
                    var go = new GameObject("NeighborPulseRunner");
                    instance = go.AddComponent<NeighborPulseRunner>();
                }
                return instance;
            }
        }

        public void Pulse(SpriteRenderer target) => StartCoroutine(PulseRoutine(target));

        private System.Collections.IEnumerator PulseRoutine(SpriteRenderer target)
        {
            if (target == null) yield break;
            Color original = target.color;
            target.color = original * 0.75f;
            yield return new WaitForSeconds(0.15f);
            if (target != null) target.color = original;
        }
    }
}
