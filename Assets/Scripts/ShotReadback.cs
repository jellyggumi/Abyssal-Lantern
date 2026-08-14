using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// What the last shot did, as a sentence, and where it flew, as a polyline.
    ///
    /// The user reported not being able to read the enemy's fire. The draft answer was a
    /// pre-action telegraph; a survey of twelve comparable titles refuted it
    /// (`.survey/siege-visibility-and-telegraph/`, `design/visibility-spec-v2.md`). A telegraph
    /// needs counterplay and a long window, and this game gives the player neither during the
    /// enemy turn — zero inputs, 0.9 seconds. What the survey found instead is post-action
    /// readback, whose precedent is structural rather than analogous: Rampart (1990) is
    /// castle-versus-castle artillery with destructible walls, no telegraph at all, and a loop
    /// where the damage the enemy left behind governs the next decision.
    ///
    /// Pure on purpose, like <see cref="Telemetry"/> beside <see cref="TelemetrySink"/>: the
    /// arithmetic and the wording are EditMode-testable with no scene, and an observer that
    /// cannot touch the simulation cannot perturb it (CLAUDE.md §2 — presentation may read
    /// simulation state, never write it).
    /// </summary>
    public static class ShotReadback
    {
        /// <summary>One resolved turn, told from the point of view of whoever is reading it.</summary>
        public struct Summary
        {
            /// <summary>True when the PLAYER fired the shot being described.</summary>
            public bool ByPlayer;
            /// <summary>Display name of the projectile, already in the roster's Korean vocabulary.</summary>
            public string Projectile;
            /// <summary>Castle wall/structure blocks destroyed by this shot.</summary>
            public int BlocksDestroyed;
            /// <summary>
            /// Midfield furniture destroyed by this shot: field towers, the flying beast, lane
            /// barrels.
            ///
            /// Separate from <see cref="BlocksDestroyed"/> because they mean different things to
            /// the next shot. A downed wall block is a hole to aim through; a downed field tower is
            /// a cleared corridor. Reporting both as "성벽" told players they had breached a wall
            /// their shot never reached — measured in qa/aim-space-reachability.md §0-C, where
            /// three shots hit a field tower, an enemy archer and bare ground and all three were
            /// announced as wall breaches.
            /// </summary>
            public int FieldPiecesDestroyed;
            /// <summary>Damage dealt to the opposing core. Zero when the core was untouched.</summary>
            public float CoreDamage;
            /// <summary>False when nothing at all was recorded — the shot missed everything.</summary>
            public bool HitSomething => BlocksDestroyed > 0 || FieldPiecesDestroyed > 0 || CoreDamage > 0f;
        }

        /// <summary>
        /// The readback line for a resolved turn.
        ///
        /// Worms fixes the timing rule this follows: damage is reported "after any player's
        /// turn, when all movement has ceased". That is why this reads as a completed fact and
        /// never as a prediction — it is composed at the settle boundary, not mid-flight.
        ///
        /// A miss is reported, not omitted. Silence after a miss is indistinguishable from the
        /// readback being broken, and the player's own misses are the shots they most need to
        /// count when re-aiming.
        /// </summary>
        public static string Compose(Summary s)
        {
            string who = s.ByPlayer ? "아군" : "적";
            string what = string.IsNullOrWhiteSpace(s.Projectile) ? "발사체" : s.Projectile.Trim();

            if (!s.HitSomething) return $"{who} {what} → 빗나감";

            var parts = new List<string>(3);
            if (s.BlocksDestroyed > 0) parts.Add($"성벽 {s.BlocksDestroyed}블록 파괴");
            // Named separately so a cleared corridor does not read as a breached wall. Ordered
            // after the wall because the wall is what the next shot has to get through.
            if (s.FieldPiecesDestroyed > 0) parts.Add($"야전 구조물 {s.FieldPiecesDestroyed} 파괴");
            // Rounded to a whole point: fractional siege damage is an artefact of multipliers,
            // and "-39.6" reads as precision the player cannot act on.
            if (s.CoreDamage > 0f) parts.Add($"코어 -{Mathf.RoundToInt(s.CoreDamage)}");

            return $"{who} {what} → {string.Join(" · ", parts)}";
        }
    }

    /// <summary>
    /// The path a shot actually flew, kept after it lands.
    ///
    /// Layer one of the three readback layers the survey identified (trajectory trace / terrain
    /// trace / numeric readback). This game already has layer two — collapsed structure stays
    /// collapsed — and had neither of the others. The trajectory existed but had no time axis:
    /// <c>TrailRenderer.time = 0.5f</c> with <c>emitting = false</c> on impact, so aim learning
    /// could never accumulate across shots. Tracer rounds have been solving exactly this since
    /// 1915; the gap here was persistence, not colour (team tint and width already exist in
    /// <see cref="UnitController.SetupTrailRenderer"/>).
    ///
    /// Pure geometry so the sampling rule can be pinned without a physics step.
    /// </summary>
    public static class ShotTracePath
    {
        /// <summary>
        /// Minimum world distance between retained samples.
        ///
        /// A trace is drawn once and never animated, so the only budget that matters is vertex
        /// count. At 0.35u a full-power lob across the 40u field keeps well under a hundred
        /// points while staying visually smooth — the arc is a parabola, not a squiggle, and
        /// oversampling a parabola buys nothing.
        /// </summary>
        public const float MinSampleDistance = 0.35f;

        /// <summary>Hard cap. A projectile that never resolves (wedged body, watchdog case) must
        /// not grow the line without bound while the 12s watchdog in
        /// <c>GameManager.WaitAndEndTurn</c> waits it out.</summary>
        public const int MaxSamples = 256;

        /// <summary>
        /// Whether <paramref name="candidate"/> earns a place in the trace.
        ///
        /// Distance-gated rather than time-gated so the shape is identical regardless of frame
        /// rate — a 30fps browser tab and a 120Hz display must draw the same arc, or two players
        /// comparing shots would be comparing their hardware.
        /// </summary>
        public static bool ShouldSample(IReadOnlyList<Vector2> existing, Vector2 candidate)
        {
            if (existing == null) return false;
            if (existing.Count >= MaxSamples) return false;
            if (existing.Count == 0) return true;
            return Vector2.Distance(existing[existing.Count - 1], candidate) >= MinSampleDistance;
        }

        /// <summary>
        /// Whether a sealed trace is worth drawing.
        ///
        /// A one-point trace is a dot at the muzzle, which reads as a rendering fault rather
        /// than as a shot. Two points are the minimum that describes a direction.
        /// </summary>
        public static bool IsDrawable(IReadOnlyList<Vector2> points) => points != null && points.Count >= 2;
    }
}
