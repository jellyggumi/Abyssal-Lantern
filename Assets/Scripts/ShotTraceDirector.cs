using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Keeps the last shot on screen after it lands: the arc it flew, a marker where it struck,
    /// and one sentence saying what it did.
    ///
    /// This is the runtime edge of <see cref="ShotReadback"/> / <see cref="ShotTracePath"/>, split
    /// the same way <see cref="TelemetrySink"/> is split from <see cref="Telemetry"/> — the rules
    /// stay EditMode-testable, and the parts that need GameObjects live here.
    ///
    /// Strictly an observer. It reads simulation state and draws; it never writes back
    /// (CLAUDE.md §2). The traces are inert renderers with no colliders, so a projectile can
    /// never collide with the memory of a previous one.
    ///
    /// One trace per side, replaced rather than accumulated. Two traces are the comparison the
    /// player needs — my last shot beside the enemy's last shot; a third would be clutter, and
    /// clutter is the documented failure mode this whole change was designed around
    /// (`design/visibility-spec-v2.md` §1-B).
    /// </summary>
    public static class ShotTraceDirector
    {
        /// <summary>Placeholder art for the impact marker. Real art replaces the file, not the
        /// name — renaming breaks every reference (task #17's lesson).</summary>
        public const string ImpactMarkerSprite = "ui_ph_impact_marker";

        private const float TraceWidth = 0.075f;
        private const int TraceSortingOrder = 2;   // matches the live trail
        private const int MarkerSortingOrder = 3;  // just above its own trace

        private class Trace
        {
            public GameObject Root;
            public LineRenderer Line;
            public SpriteRenderer Marker;
        }

        // Unity objects in statics: every read must be Unity-null aware, because a scene load
        // destroys the GameObject while the C# reference lives on. NarrativeVideoIntro shipped
        // that exact bug — `?.` only sees C# null and happily touched a destroyed object.
        private static readonly Trace playerTrace = new Trace();
        private static readonly Trace enemyTrace = new Trace();

        // ---- The shot currently in the air --------------------------------------------------

        private static readonly List<Vector2> samples = new List<Vector2>(ShotTracePath.MaxSamples);
        private static bool shotOpen;
        private static bool shotByPlayer;
        private static string shotProjectile;
        private static int blocksThisShot;
        private static float coreDamageThisShot;

        /// <summary>The composed readback for the most recently sealed shot, or empty.</summary>
        public static string LatestLine { get; private set; } = string.Empty;

        /// <summary>Whether <see cref="LatestLine"/> describes a shot the player fired. Drives
        /// the strip's colour so friendly and hostile results are separable at a glance.</summary>
        public static bool LatestLineByPlayer { get; private set; }

        /// <summary>Samples retained for the in-flight shot. Exposed for tests and diagnostics.</summary>
        public static int SampleCount => samples.Count;

        // Domain-init guard for fast-enter-playmode (domain reload disabled), mirroring CastleRuinFx.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => ResetForNewMatch();

        /// <summary>
        /// Drops both traces and the pending shot. Called from <c>GameManager.StartGame</c>'s
        /// rematch-hygiene block: SubsystemRegistration fires on domain init but NOT on the
        /// scene-reload rematch loop, so without this a fresh match would open with the previous
        /// match's arcs still drawn over a board they no longer describe.
        /// </summary>
        public static void ResetForNewMatch()
        {
            AbandonShot();
            LatestLine = string.Empty;
            LatestLineByPlayer = false;
            Discard(playerTrace);
            Discard(enemyTrace);
        }

        private static void Discard(Trace t)
        {
            if (t.Root != null) Object.Destroy(t.Root);
            t.Root = null;
            t.Line = null;
            t.Marker = null;
        }

        /// <summary>Forgets the in-flight shot without sealing it, so a shot interrupted by a
        /// scene reload cannot leak its samples into the next arc.</summary>
        private static void AbandonShot()
        {
            shotOpen = false;
            samples.Clear();
            blocksThisShot = 0;
            coreDamageThisShot = 0f;
            shotProjectile = string.Empty;
        }

        // ---- Recording ----------------------------------------------------------------------

        /// <summary>
        /// A projectile just left the muzzle. Opens the recording window; the previous trace for
        /// this side stays on screen until <see cref="Seal"/> replaces it, so the player never
        /// looks at an empty field mid-shot.
        /// </summary>
        public static void BeginShot(bool byPlayer, string projectileDisplayName, Vector2 muzzle)
        {
            samples.Clear();
            samples.Add(muzzle);
            shotOpen = true;
            shotByPlayer = byPlayer;
            shotProjectile = projectileDisplayName;
            blocksThisShot = 0;
            coreDamageThisShot = 0f;
        }

        /// <summary>Offers a flight position. Distance-gated so the arc is frame-rate independent
        /// (see <see cref="ShotTracePath.ShouldSample"/>).</summary>
        public static void Sample(Vector2 position)
        {
            if (!shotOpen) return;
            if (ShotTracePath.ShouldSample(samples, position)) samples.Add(position);
        }

        /// <summary>
        /// A structural block came down while this shot was resolving.
        ///
        /// Attribution is by resolution window, not by a damage-source flag: collapse chains
        /// propagate through <c>DestructibleBlock.OnCollisionEnter2D</c> carrying no flag at all,
        /// and a cascade is precisely the part of a shot's result the player most needs counted.
        /// The claim the line makes — "this turn's shot left N blocks down" — is therefore
        /// literally what was measured.
        /// </summary>
        public static void NoteBlockDestroyed()
        {
            if (!shotOpen) return;
            blocksThisShot++;
        }

        /// <summary>Effective core damage — after the shield absorb and the full-health volley cap,
        /// so the number matches the gauge the player is watching rather than the raw roll.</summary>
        public static void NoteCoreDamage(float amount)
        {
            if (!shotOpen || amount <= 0f) return;
            coreDamageThisShot += amount;
        }

        // ---- Sealing ------------------------------------------------------------------------

        /// <summary>
        /// Freezes the shot: draws the arc and marker, and composes the readback line.
        ///
        /// Called at the settle boundary in <c>GameManager.WaitAndEndTurn</c> — after every body
        /// and fuse has resolved. That timing is Worms' rule ("after any player's turn, when all
        /// movement has ceased") and it is what lets this report a completed fact instead of
        /// racing the animation it describes.
        /// </summary>
        public static void Seal()
        {
            if (!shotOpen) return;
            shotOpen = false;

            LatestLine = ShotReadback.Compose(new ShotReadback.Summary
            {
                ByPlayer = shotByPlayer,
                Projectile = shotProjectile,
                BlocksDestroyed = blocksThisShot,
                CoreDamage = coreDamageThisShot,
            });
            LatestLineByPlayer = shotByPlayer;

            if (Application.isPlaying && ShotTracePath.IsDrawable(samples))
            {
                Draw(shotByPlayer ? playerTrace : enemyTrace, shotByPlayer, samples);
            }

            samples.Clear();
            blocksThisShot = 0;
            coreDamageThisShot = 0f;
        }

        private static void Draw(Trace t, bool byPlayer, List<Vector2> points)
        {
            // Same team tint the live trail uses (UnitController.SetupTrailRenderer), dimmed:
            // a spent shot must be legible as history, not compete with the live one.
            Color tint = byPlayer ? new Color(0.45f, 0.85f, 1f, 0.5f) : new Color(1f, 0.35f, 0.25f, 0.5f);

            if (t.Root == null)
            {
                t.Root = new GameObject(byPlayer ? "ShotTrace_Player" : "ShotTrace_Enemy");
                t.Line = t.Root.AddComponent<LineRenderer>();
                t.Line.useWorldSpace = true;
                t.Line.material = new Material(Shader.Find("Sprites/Default"));
                t.Line.sortingOrder = TraceSortingOrder;
                t.Line.numCapVertices = 2;

                var markerGo = new GameObject("Impact");
                markerGo.transform.SetParent(t.Root.transform, false);
                t.Marker = markerGo.AddComponent<SpriteRenderer>();
                t.Marker.sortingOrder = MarkerSortingOrder;
            }

            t.Line.startWidth = TraceWidth;
            t.Line.endWidth = TraceWidth;
            t.Line.startColor = new Color(tint.r, tint.g, tint.b, tint.a * 0.35f); // faint at the muzzle
            t.Line.endColor = tint;                                                // solid at the impact
            t.Line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) t.Line.SetPosition(i, points[i]);

            var impact = points[points.Count - 1];
            t.Marker.transform.position = impact;
            var sprite = GimmickSpriteLibrary.Load(ImpactMarkerSprite);
            t.Marker.sprite = sprite;
            t.Marker.color = tint;
            // No art → no marker, rather than a magenta quad. The arc still reads on its own.
            t.Marker.enabled = sprite != null;
        }
    }
}
