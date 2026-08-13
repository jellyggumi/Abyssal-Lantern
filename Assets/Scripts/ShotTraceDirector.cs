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
        /// <summary>
        /// Width of the PLAYER's arc core, in world units (~2.9px at a 45u-wide board).
        ///
        /// Width is the non-colour channel that separates the two arcs. It is needed because they
        /// coexist on screen — turn order cannot tell them apart the way it does in the artillery
        /// lineage, where only one side's shot is ever visible — and WCAG 2.2 SC 1.4.1 forbids
        /// colour as the only visual means of distinguishing an element.
        ///
        /// This began as a dashed enemy arc and that was a real bug, caught by measuring the
        /// capture rather than by reading the code: a LineRenderer is ONE continuous strip, so
        /// omitting vertices does not punch a hole — it draws a straight chord across the gap.
        /// The enemy arc rendered solid AND slightly wrong-shaped, while the vertex-count
        /// assertion still passed, which is the worst combination a test can produce.
        ///
        /// A texture-tiled dash or one renderer per dash would both work; width is chosen because
        /// it needs no new asset, no extra renderer, and cannot silently stop working.
        /// </summary>
        private const float PlayerCoreWidth = 0.105f;

        /// <summary>The enemy's core, deliberately thinner — a 2.1x ratio at the core.</summary>
        private const float EnemyCoreWidth = 0.05f;

        /// <summary>
        /// Widths of the dark casing behind each core. Without the casing the arcs were measurably
        /// invisible — see <see cref="CasingColor"/>.
        ///
        /// These are what the player actually perceives as "how thick the arc is", because the
        /// casing is the outer silhouette. The first attempt kept an equal 1.44px dark margin on
        /// both arcs, which made the casings only 1.31x apart even though the cores were 1.9x —
        /// the live test measured the silhouette and rejected it, correctly. Widening the player's
        /// and tightening the enemy's gives 1.75x on the silhouette and 2.1x on the core, while
        /// keeping at least ~1.06px of dark on each side of both: below one full pixel row a
        /// casing can disappear into antialiasing, which is exactly the failure it exists to fix.
        /// </summary>
        private const float PlayerCasingWidth = 0.21f;
        private const float EnemyCasingWidth = 0.12f;

        /// <summary>
        /// The casing that makes the arc legible at all.
        ///
        /// Measured from a real capture: the enemy arc scored 1.13:1 against the sky, against a
        /// WCAG non-text minimum of 3.0:1. Raising alpha does not fix it — solving the blend
        /// equation shows the enemy tint reaches only 1.18:1 at alpha 1.0 and the player tint
        /// 1.65:1, because the team colours differ from that sky in HUE while matching it in
        /// LUMINANCE. WCAG 1.4.1's own escape clause names the missing ingredient: colours must
        /// "differ not only in their hue, but ... also have a significant difference in lightness".
        ///
        /// So the arc is drawn twice. On bright ground the dark casing carries the contrast
        /// (6.4:1 sky, 7.3:1 grass, 8.8:1 cloud); on dark ground the casing washes out (1.8:1 on
        /// dirt) and the bright core takes over there instead (3.2:1 enemy, 6.2:1 player). Every
        /// background the arc crosses is covered by at least one of the two layers.
        /// </summary>
        private static readonly Color CasingColor = new Color(0.03f, 0.028f, 0.05f, 0.85f);

        private const int TraceSortingOrder = 2;   // matches the live trail
        private const int CoreSortingOrder = 3;    // above its own casing

        private class Trace
        {
            public GameObject Root;
            public LineRenderer Line;     // bright team-coloured core
            public LineRenderer Casing;   // dark backing, drawn wider and beneath
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
            t.Casing = null;
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

        /// <summary>
        /// Draws the spent arc. There is deliberately no marker at the endpoint.
        ///
        /// A survey of thirteen comparable titles found the icon-at-impact form in exactly one
        /// — Battleship — and that one is a "discovery game in which players need to discover
        /// their opponent's ship positions". An icon substitutes for a world you cannot see.
        /// Ten of thirteen instead change the WORLD at the impact point (Worms' craters, whose
        /// diameter is a balance figure; Gunbound's land damage, which is a win condition;
        /// Rampart's wall holes, which drive the next building phase), and castle-war already
        /// does that — ground tiles are DestructibleBlocks and `isGroundAnchor` covers only the
        /// border columns and bottom two rows, so a shot into the middle band already digs.
        ///
        /// So the placeholder white box was not missing art; it was the wrong form, laid over
        /// the deformation it was hiding. The arc's final vertex states where the shot landed,
        /// which is the same job Scorched Earth's tracers do ("allow the player to more
        /// accurately adjust the trajectory on their next turn"). Removing the icon costs no
        /// readback information. `.survey/siege-impact-vfx-and-attack-motion/`
        /// </summary>
        private static void Draw(Trace t, bool byPlayer, List<Vector2> points)
        {
            // Team tint at near-full alpha. The previous 0.5 was chosen to keep a spent shot
            // subordinate to the live trail, and measurement showed it bought nothing: alpha is
            // not what made these arcs hard to see (see CasingColor). Subordination is carried by
            // width instead — the core is 0.075 against the live trail's 0.09-0.14.
            Color tint = byPlayer ? new Color(0.45f, 0.85f, 1f, 0.95f) : new Color(1f, 0.35f, 0.25f, 0.95f);

            if (t.Root == null)
            {
                t.Root = new GameObject(byPlayer ? "ShotTrace_Player" : "ShotTrace_Enemy");

                // Casing on the root, core on a child: GameObject.Find(...).GetComponent<LineRenderer>()
                // is how the live-scene tests reach the arc, and the geometry they measure is the
                // dashed/solid shape, which both layers share.
                t.Casing = t.Root.AddComponent<LineRenderer>();
                ConfigureLine(t.Casing, TraceSortingOrder);

                var coreGo = new GameObject("Core");
                coreGo.transform.SetParent(t.Root.transform, false);
                t.Line = coreGo.AddComponent<LineRenderer>();
                ConfigureLine(t.Line, CoreSortingOrder);
            }

            // Width is the non-colour channel: the player's arc is the thicker of the two. Both
            // layers of one arc share the same polyline, so the casing always backs exactly what
            // the core draws.
            float coreWidth = byPlayer ? PlayerCoreWidth : EnemyCoreWidth;
            float casingWidth = byPlayer ? PlayerCasingWidth : EnemyCasingWidth;

            Apply(t.Casing, points, casingWidth, CasingColor, CasingColor);
            Apply(t.Line, points, coreWidth,
                new Color(tint.r, tint.g, tint.b, tint.a * 0.55f), // fades toward the muzzle
                tint);                                             // full at the impact
        }

        private static void ConfigureLine(LineRenderer line, int sortingOrder)
        {
            line.useWorldSpace = true;
            line.material = new Material(Shader.Find("Sprites/Default"));
            line.sortingOrder = sortingOrder;
            line.numCapVertices = 2;
            line.textureMode = LineTextureMode.Stretch;
        }

        private static void Apply(LineRenderer line, List<Vector2> points, float width, Color start, Color end)
        {
            if (line == null) return;
            line.startWidth = width;
            line.endWidth = width;
            line.startColor = start;
            line.endColor = end;
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
        }

        // (A geometry-based Dash() used to live here. It could not work: a LineRenderer is one
        //  continuous strip, so dropping vertices draws a straight chord across the intended gap
        //  instead of a hole. Width replaces it — see PlayerCoreWidth.)
    }
}
