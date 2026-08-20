using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>Position-aware castle facade roles. A block's role is a pure function of
    /// its integer grid coordinate within the castle's occupied bounds, so the mapping is
    /// EditMode-testable without a scene (CastleSkinTests). Priority on ties:
    /// Crown (top row) beats Edge (outer column) beats Base (bottom row) — a 1-wide tower's
    /// top block is Crown, its bottom block is Base only if it isn't also the top.</summary>
    public enum CastleSkinRole { Face = 0, Crown = 1, Edge = 2, Base = 3 }

    /// <summary>
    /// Loads and caches the generated castle skin tiles (Resources/CastleSkin/{role}_{s0|s1|s2})
    /// and owns the pure grid→role assignment. Art-absent builds stay correct: when any of a
    /// role's three damage states is missing, TryGetSkin reports false and callers keep the
    /// BlockData sprites — the skin system is presentation-only and strictly optional.
    /// </summary>
    public static class CastleSkinLibrary
    {
        private static readonly Dictionary<CastleSkinRole, Sprite[]> cache = new Dictionary<CastleSkinRole, Sprite[]>();

        /// <summary>Pure role assignment. Coordinates are integer grid cells; bounds are the
        /// castle's occupied min/max cells (inclusive).</summary>
        public static CastleSkinRole AssignRole(int x, int y, int minX, int maxX, int minY, int maxY)
        {
            if (y == maxY) return CastleSkinRole.Crown;
            if (x == minX || x == maxX) return CastleSkinRole.Edge;
            if (y == minY) return CastleSkinRole.Base;
            return CastleSkinRole.Face;
        }

        /// <summary>Presentation-band arithmetic shared with DestructibleBlock.UpdateVisuals:
        /// band 0 = intact, 1 = cracked (HP ratio ≤ 0.7), 2 = crumbling (≤ 0.3). The castle-wide
        /// wear floor only ever raises the *displayed* band — HP is never touched.</summary>
        public static int ComputeDisplayBand(float hpRatio, int wearFloor)
        {
            int own = hpRatio <= 0.3f ? 2 : (hpRatio <= 0.7f ? 1 : 0);
            return Mathf.Max(own, Mathf.Clamp(wearFloor, 0, 2));
        }

        /// <summary>All three damage states for a role, or false when the generated art is not
        /// (yet) present under Resources/CastleSkin/.</summary>
        public static bool TryGetSkin(CastleSkinRole role, out Sprite normal, out Sprite cracked, out Sprite heavy)
        {
            if (!cache.TryGetValue(role, out var states))
            {
                string prefix = "CastleSkin/" + role.ToString().ToLowerInvariant() + "_s";
                states = new[]
                {
                    Resources.Load<Sprite>(prefix + "0"),
                    Resources.Load<Sprite>(prefix + "1"),
                    Resources.Load<Sprite>(prefix + "2"),
                };
                cache[role] = states;
            }
            normal = states[0]; cracked = states[1]; heavy = states[2];
            return normal != null && cracked != null && heavy != null;
        }

        /// <summary>Test/reload hook: drops cached sprite lookups (EditMode domain reloads keep
        /// statics alive; stale null results would otherwise mask newly imported art).</summary>
        public static void ClearCache() => cache.Clear();
    }

    /// <summary>
    /// Applies position-aware skins to every block of a castle. Invoked from
    /// CastleController.RefreshBlockList, so scene-authored castles, runtime-spawned walls
    /// (GameManager.SpawnCastleWall → RefreshBlockList) and rebuilt lists all converge here.
    /// Presentation-only: writes sprite fields + flipX via DestructibleBlock.SetSkinSprites;
    /// colliders/HP/mass are never modified (the world-space collider size is invariant —
    /// ApplyPresentationScale normalizes any native sprite size to targetWorldSize).
    /// </summary>
    public static class CastleFacadeDirector
    {
        public static void ApplySkins(CastleController castle, IReadOnlyList<DestructibleBlock> blocks)
        {
            if (castle == null || blocks == null || blocks.Count == 0) return;

            float sx = Mathf.Max(0.05f, castle.blockSizeX);
            float sy = Mathf.Max(0.05f, castle.blockSizeY);

            // Terrain is excluded from the bounds too, not just from skinning. Role assignment is
            // positional — Edge is "the leftmost/rightmost column", Face is "inside" — so leaving
            // the 41-column ground strip in this box computed every wall block's role against a
            // span four times the castle's own width, and the quoined edge column landed on
            // terrain instead of on the wall's silhouette.
            int minX = int.MaxValue, maxX = int.MinValue, minY = int.MaxValue, maxY = int.MinValue;
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b == null || b.IsTerrainTile) continue;
                int gx = Mathf.RoundToInt(b.transform.position.x / sx);
                int gy = Mathf.RoundToInt(b.transform.position.y / sy);
                if (gx < minX) minX = gx;
                if (gx > maxX) maxX = gx;
                if (gy < minY) minY = gy;
                if (gy > maxY) maxY = gy;
            }
            if (minX > maxX) return;

            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b == null) continue;
                // The core is the win-condition landmark — its bespoke look stays.
                if (b is CastleCoreGimmick) continue;
                // Terrain IS skinned, deliberately. Excluding it (2026-08-19) so the ground atlas
                // would survive turned the board into a slab: CastleSkin tiles are 47-82% opaque
                // masonry and the background's own grass and path read through them, while the
                // ground tiles are 100% opaque and cover it. A 47x5 opaque rectangle across the
                // middle of the screen reads as one enormous wall, which is exactly how it was
                // reported.
                //
                // Terrain is outside the bounds computed above, so AssignRole returns Face for all
                // of it — the interior masonry, uniform across the strip, which is what it looked
                // like before and what it should look like. The bounds exclusion stays: that fixed
                // a separate defect where the ground's 47 columns decided every WALL block's role.

                int gx = Mathf.RoundToInt(b.transform.position.x / sx);
                int gy = Mathf.RoundToInt(b.transform.position.y / sy);
                var role = CastleSkinLibrary.AssignRole(gx, gy, minX, maxX, minY, maxY);
                if (!CastleSkinLibrary.TryGetSkin(role, out var n, out var c, out var h)) continue;

                // Mirror the quoined edge column on the right flank so one Edge tile serves
                // both silhouette sides.
                bool flipX = role == CastleSkinRole.Edge && gx == maxX;
                b.SetSkinSprites(n, c, h, flipX);
            }
        }
    }
}
