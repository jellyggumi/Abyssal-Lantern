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

        /// <summary>
        /// Vertex alpha of a spent arc's coloured core - deliberately NOT the knob that makes it
        /// translucent.
        ///
        /// Translucency lives in the dash texture, whose measured peak alpha is 0.549. Multiplying
        /// a reduced vertex alpha on top of that compounds: 0.72 would have composited to 0.40 and
        /// taken the dark casing down with it, and the casing is the only reason these arcs are
        /// legible at all (6.4:1 sky, 7.3:1 grass - see CasingColor). Alpha at 0.5 was already
        /// measured once and reverted for exactly that reason.
        ///
        /// So this stays near-opaque and the texture supplies the fade: 0.549 x 0.95 = 0.52
        /// effective, which is translucent without spending the contrast margin twice.
        /// </summary>
        public const float SpentAlpha = 0.95f;

        /// <summary>Measured peak alpha of the shared dash art, asserted in SpentArcDashTests so
        /// this composition cannot drift if the art is redrawn.</summary>
        public const float DashPeakAlpha = 0.549f;

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
        private static int fieldPiecesThisShot;
        private static int ownBlocksThisShot;
        private static float coreDamageThisShot;

        /// <summary>The composed readback for the most recently sealed shot, or empty.</summary>
        public static string LatestLine { get; private set; } = string.Empty;

        /// <summary>Whether <see cref="LatestLine"/> describes a shot the player fired. Drives
        /// the strip's colour so friendly and hostile results are separable at a glance.</summary>
        public static bool LatestLineByPlayer { get; private set; }

        /// <summary>Samples retained for the in-flight shot. Exposed for tests and diagnostics.</summary>
        public static int SampleCount => samples.Count;

        /// <summary>
        /// True while a shot is airborne and its arc is still growing.
        ///
        /// Exposed for the same reason as <see cref="SampleCount"/>: the arc is now drawn DURING
        /// flight, and a test that checks it after the turn resolves cannot tell a live arc from the
        /// spent one that replaces it. This is the window's own name.
        /// </summary>
        public static bool ShotOpen => shotOpen;

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

            // Both dash caches go too. `dashTexture` may hold the authored asset's texture, which a
            // scene load can unload, and `opaqueDashTexture` is a Texture2D this class created and
            // therefore owns. A destroyed Texture2D still passes a null check against its C#
            // wrapper, so the stale reference would be handed to a material as a live texture.
            dashTexture = null;
            if (opaqueDashTexture != null && opaqueDashTexture.name == "ShotTraceDashOpaque")
            {
                // DestroyImmediate outside play mode: `Destroy` is deferred to the next frame, and
                // EditMode has no next frame — it logs an error instead, which fails whichever test
                // happens to be running. Caught by ConsecutiveShots_DoNotInheritEachOthersTally,
                // which calls this in SetUp.
                if (Application.isPlaying) Object.Destroy(opaqueDashTexture);
                else Object.DestroyImmediate(opaqueDashTexture);
            }
            opaqueDashTexture = null;
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
            fieldPiecesThisShot = 0;
            ownBlocksThisShot = 0;
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
            fieldPiecesThisShot = 0;
            ownBlocksThisShot = 0;
            coreDamageThisShot = 0f;
        }

        /// <summary>
        /// Offers a flight position, and draws what has accumulated so far.
        ///
        /// Distance-gated so the arc is frame-rate independent (see
        /// <see cref="ShotTracePath.ShouldSample"/>).
        ///
        /// The drawing is the part that was missing. Until now this method only accumulated, and the
        /// arc first appeared in <see cref="Seal"/> — at turn resolution, after the projectile had
        /// already landed. So the flight itself was untraced: the player watched an unadorned sprite
        /// travel and only learned its path once the path no longer mattered. Requested directly
        /// ("아군이 발사체를 놓았을 때는 날아가는게 보이게, 그때 궤도가 점선으로").
        ///
        /// Nothing new is drawn WITH: the dash texture, the dark casing, the tile mode and the
        /// widths are the same layers the spent arc already used, so a live arc and a remembered one
        /// are the same object at two moments rather than two things to keep consistent.
        ///
        /// Redrawing the whole strip per sample is O(n) each time, and the gate makes n small: 0.35
        /// world units between points, capped at MaxSamples. A full-power lob is on the order of a
        /// hundred points, and only when it has moved far enough to add one.
        /// </summary>
        public static void Sample(Vector2 position)
        {
            if (!shotOpen) return;
            if (!ShotTracePath.ShouldSample(samples, position)) return;
            samples.Add(position);

            if (Application.isPlaying && ShotTracePath.IsDrawable(samples))
            {
                Draw(shotByPlayer ? playerTrace : enemyTrace, shotByPlayer, samples);
            }
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
        /// <param name="wallOwnerIsPlayer">
        /// Which side owned the wall, or null for field furniture with no owner. Compared against
        /// the side that fired: a shot that drops its own wall is not a breach, and reporting it as
        /// one reads as progress. A live sweep caught exactly that — a shallow draw from the -17
        /// apron struck the player's own keep at x=-8 and the line said "성벽 3블록 파괴".
        /// </param>
        public static void NoteBlockDestroyed(TargetKind kind = TargetKind.Wall,
                                              bool? wallOwnerIsPlayer = null)
        {
            if (!shotOpen) return;
            if (kind != TargetKind.Wall) { fieldPiecesThisShot++; return; }

            // Unknown owner counts as the opponent's, preserving what every existing caller meant
            // before ownership was threaded through.
            bool ownWall = wallOwnerIsPlayer.HasValue && wallOwnerIsPlayer.Value == shotByPlayer;
            if (ownWall) ownBlocksThisShot++;
            else blocksThisShot++;
        }

        /// <summary>
        /// What kind of thing a destroyed block was.
        ///
        /// Needed because the readback used to call every DestructibleBlock a wall, including
        /// midfield towers and the flying beast, so a shot intercepted at x=0 was reported as a
        /// breach. The default is <see cref="TargetKind.Wall"/> so existing callers keep their
        /// meaning; the field category is opt-in from the one site that can tell them apart.
        /// </summary>
        public enum TargetKind
        {
            /// <summary>Part of a castle — the thing the next shot has to get through.</summary>
            Wall,
            /// <summary>Midfield furniture: field towers, the flying beast, barrels in the lanes.</summary>
            FieldObstacle,
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
                FieldPiecesDestroyed = fieldPiecesThisShot,
                OwnBlocksDestroyed = ownBlocksThisShot,
                CoreDamage = coreDamageThisShot,
            });
            LatestLineByPlayer = shotByPlayer;

            if (Application.isPlaying && ShotTracePath.IsDrawable(samples))
            {
                Draw(shotByPlayer ? playerTrace : enemyTrace, shotByPlayer, samples);
            }

            samples.Clear();
            blocksThisShot = 0;
            fieldPiecesThisShot = 0;
            ownBlocksThisShot = 0;
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
            // A spent shot is now a translucent dotted arc: requested so the memory of the last
            // shot reads as memory, not as a second live trail.
            //
            // Alpha alone was tried before and rejected on measurement - it is not what made these
            // arcs hard to see (see CasingColor), and dropping it far enough to feel "faded" cost
            // the contrast the casing exists to provide. The dash is what carries the fade here:
            // it removes ~43% of the ink outright, so the alpha only has to soften what remains.
            // Hence a moderate SpentAlpha rather than the 0.5 that was measured and reverted.
            Color tint = byPlayer ? new Color(0.45f, 0.85f, 1f, SpentAlpha) : new Color(1f, 0.35f, 0.25f, SpentAlpha);

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

            // A shot still in the air gets OPAQUE dots; a spent one keeps the translucent ones.
            //
            // Reported as "the arc after firing is still too dark", and measuring it explained why
            // brightening the colour had not helped and could not. The core is drawn through a dash
            // asset whose marks peak at alpha 0.549, over a casing that is nearly black — so a white
            // dot composites to 0.536 grey, DARKER than this board's sky (0.72). Against sky that
            // grey measures 1.14:1, against grass 1.25:1, where 3:1 is the floor. Every brighter
            // colour measured worse still: opaque white reaches 2.44 and amber 1.58, because both
            // board surfaces sit at mid luminance and a pale line on pale sky has nowhere to go.
            //
            // Opacity is the lever, not hue. An opaque white dot against the dark casing measures
            // 15.11:1, and the casing against the board is 6.18 (sky) and 4.45 (grass) — so every
            // edge the eye uses clears the floor by a wide margin. The two-layer structure was
            // already here; the dash's alpha cap was stopping the bright layer from being bright.
            //
            // Spent arcs are deliberately left alone: their translucency is what makes the last
            // shot read as memory rather than as a second live trail, and the request was explicit
            // that only the in-flight case needed to change.
            bool live = shotOpen;
            if (t.Line != null && t.Line.material != null)
            {
                var dash = live ? OpaqueDashTexture() : DashTexture();
                if (dash != null) t.Line.material.mainTexture = dash;
            }

            // 1.9 at the head. The projectile is the last vertex, so the strip is widest exactly
            // where the thing being followed is — and it narrows behind it, which reads as direction
            // without an arrowhead. Both layers taper together so the casing keeps backing the core
            // along the whole length instead of letting it spill past the rim at the head.
            const float LiveHeadTaper = 1.9f;
            float taper = live ? LiveHeadTaper : 1f;

            Apply(t.Casing, points, casingWidth, CasingColor, CasingColor, taper);
            Apply(t.Line, points, coreWidth,
                // Live: full alpha at both ends. The muzzle-ward fade is a memory cue, and a shot
                // that has not landed yet has nothing to remember — fading its tail hides the part
                // of the path the player is checking against the wind.
                live ? new Color(tint.r, tint.g, tint.b, 1f)
                     : new Color(tint.r, tint.g, tint.b, tint.a * 0.55f),
                live ? new Color(tint.r, tint.g, tint.b, 1f) : tint,
                taper);
        }

        private static void ConfigureLine(LineRenderer line, int sortingOrder)
        {
            line.useWorldSpace = true;
            line.material = new Material(Shader.Find("Sprites/Default"));
            var dash = DashTexture();
            if (dash != null)
            {
                line.material.mainTexture = dash;
                // Tile, not Stretch: this is what actually punches holes in the strip. The geometry
                // approach that used to live at the bottom of this file could not - a LineRenderer
                // is one continuous strip, so dropping vertices draws a chord across the gap, and
                // the vertex-count assertion still passed while the arc rendered solid.
                line.textureMode = LineTextureMode.Tile;
                line.textureScale = new Vector2(DashStretch, 1f);
            }
            line.sortingOrder = sortingOrder;
            // A round cap bridges the gaps the texture just cut.
            line.numCapVertices = 0;
        }

        /// <summary>
        /// U stretch for the tiled dash, taken from the preview arc's MEASURED value rather than
        /// derived here.
        ///
        /// LaunchManager's dotted preview shipped at textureScale 1 and a capture of the deployed
        /// build showed it was not dotted at all: 939 contiguous arc columns, ZERO gaps, 30%
        /// brightness modulation, autocorrelation period 7.0px - soft dot edges below ~7px pitch
        /// blur into a faintly ribbed line. Stretching U to 0.35 targets a ~20px period, where the
        /// gaps survive.
        ///
        /// A larger number here means MORE repeats and a SHORTER period, i.e. the wrong direction.
        /// This started at 1.6 on a tiling model rather than a measurement, which would have
        /// reproduced exactly the solid line that capture already caught once.
        /// </summary>
        private const float DashStretch = 0.35f;

        /// <summary>Mark fraction of the PROCEDURAL fallback cell only. The authored asset carries
        /// its own duty cycle (44%, with peak alpha 0.55) and is preferred.</summary>
        public const float DashDutyCycle = 0.57f;

        private static Texture2D dashTexture;

        /// <summary>
        /// The same dash art the preview arc uses. Shared deliberately: two independently authored
        /// dash patterns in one game is how the two lines drift apart, and this one has a capture
        /// behind its dimensions. Falls back to a procedural mark/gap ramp only if the asset is
        /// missing, so a stripped Resources folder degrades to a plain line instead of throwing.
        /// </summary>
        public static Texture2D DashTexture()
        {
            if (dashTexture != null) return dashTexture;
            var authored = Resources.Load<Sprite>("Effects/trajectory_dash");
            if (authored != null && authored.texture != null)
            {
                dashTexture = authored.texture;
                return dashTexture;
            }

            const int width = 32;
            var tex = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                name = "ShotTraceDashFallback"
            };
            int mark = Mathf.Clamp(Mathf.RoundToInt(width * DashDutyCycle), 1, width - 1);
            for (int x = 0; x < width; x++)
            {
                tex.SetPixel(x, 0, x < mark ? Color.white : new Color(1f, 1f, 1f, 0f));
            }
            tex.Apply();
            dashTexture = tex;
            return dashTexture;
        }

        private static Texture2D opaqueDashTexture;

        /// <summary>
        /// The same dash pattern with its alpha lifted so the marks reach 1.0.
        ///
        /// Derived from <see cref="DashTexture"/> rather than authored separately, because the point
        /// is that the live and spent arcs share one dash SHAPE and differ only in opacity. A second
        /// hand-drawn asset is how the period, duty cycle and phase drift apart, and the shared one
        /// has a capture behind its dimensions.
        ///
        /// The lift is proportional, not a threshold: every pixel's alpha is divided by the strip's
        /// peak. A soft-edged dot stays soft-edged, so the marks do not gain the hard stair-steps
        /// that a cutoff would produce at this size (~3px of core width at a 45-unit board).
        ///
        /// Read/Write may be disabled on the authored asset, in which case GetPixels32 throws and
        /// this returns the translucent original — the arc is then dim rather than absent, which is
        /// the same trade every other art path here makes.
        /// </summary>
        public static Texture2D OpaqueDashTexture()
        {
            if (opaqueDashTexture != null) return opaqueDashTexture;

            var src = DashTexture();
            if (src == null) return null;

            Color32[] pixels;
            try
            {
                pixels = src.GetPixels32();
            }
            catch (UnityException)
            {
                opaqueDashTexture = src;
                return opaqueDashTexture;
            }

            byte peak = 0;
            for (int i = 0; i < pixels.Length; i++) if (pixels[i].a > peak) peak = pixels[i].a;
            if (peak == 0) { opaqueDashTexture = src; return opaqueDashTexture; }

            float lift = 255f / peak;
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i].a = (byte)Mathf.Min(255f, pixels[i].a * lift);
            }

            var tex = new Texture2D(src.width, src.height, TextureFormat.RGBA32, false)
            {
                // Repeat, matching the source: LineTextureMode.Tile relies on it, and clamping
                // smears the last column down the whole tail instead of dashing it.
                wrapMode = TextureWrapMode.Repeat,
                filterMode = src.filterMode,
                name = "ShotTraceDashOpaque"
            };
            tex.SetPixels32(pixels);
            tex.Apply();
            opaqueDashTexture = tex;
            return opaqueDashTexture;
        }

        /// <summary>
        /// Writes a polyline onto a LineRenderer.
        ///
        /// <paramref name="endWidthMult"/> widens the strip toward its LAST vertex. For a shot in
        /// flight that vertex is the projectile's current position, so a taper marks the projectile
        /// itself — the second half of the request ("날라가는 물체가 잘 보이게") — without a second
        /// renderer to keep in step with the arc, and without a marker that outlives the shot.
        ///
        /// 1 for a spent arc: it has no head to mark, and tapering a memory would imply the last
        /// sample matters more than the rest of the path, which is the opposite of what a spent arc
        /// is for.
        /// </summary>
        private static void Apply(LineRenderer line, List<Vector2> points, float width,
                                  Color start, Color end, float endWidthMult = 1f)
        {
            if (line == null) return;
            line.startWidth = width;
            line.endWidth = width * endWidthMult;
            line.startColor = start;
            line.endColor = end;
            line.positionCount = points.Count;
            for (int i = 0; i < points.Count; i++) line.SetPosition(i, points[i]);
        }

        // (A geometry-based Dash() used to live here. It could not work: a LineRenderer is one
        //  continuous strip, so dropping vertices draws a straight chord across the intended gap
        //  instead of a hole. The dash is now cut by a tiled mark/gap texture instead - see
        //  DashTexture() - which is the approach the original note named as workable. Width stays
        //  as the player/enemy channel; the dash is shared by both arcs and separates neither.)
    }
}
