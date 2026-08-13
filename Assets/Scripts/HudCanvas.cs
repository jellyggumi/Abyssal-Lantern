using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// The one canvas the gameplay HUD draws on.
    ///
    /// Six call sites used to find their parent by taking whatever
    /// <c>FindObjectOfType&lt;Canvas&gt;()</c> returned first, and that order is not defined.
    /// The result was measured on 2026-08-13: in a single frame the launch labels landed on the
    /// scene's ConstantPixelSize canvas (scaleFactor 1) while the core badges and the supply
    /// gauge landed on <c>NarrativeCanvas</c> — the cold-open video's own canvas — which
    /// <see cref="GameFeelVfx"/> then rewrote to ScaleWithScreenSize 1920x1080. At a 640x480
    /// window that scaler yields 0.385, so a 17pt badge rendered at 6.5px and "KEEP CORE" read
    /// as "KLLP CORL": the crossbars of E fall below one pixel row and vanish.
    /// Evidence: `_workspace/current/qa/evidence/font/hud-font-scale.md`.
    ///
    /// Two rules follow, and both matter:
    ///
    /// 1. **Resolve by name, never by iteration order.** Ordering is an implementation detail
    ///    of the engine; depending on it means the HUD's parent can change between Unity
    ///    versions without a single line of game code changing.
    /// 2. **Never reconfigure a canvas we did not create.** Rewriting another system's scaler
    ///    resizes everything that system drew. The cold-open canvas belongs to the cold open.
    /// </summary>
    public static class HudCanvas
    {
        public const string CanvasName = "GameplayHudCanvas";

        /// <summary>
        /// Reference height the HUD's point sizes are authored against. Font sizes below are
        /// expressed in these units; multiply by <see cref="CanvasScaler.scaleFactor"/> to get
        /// pixels.
        /// </summary>
        public const float ReferenceWidth = 1920f;
        public const float ReferenceHeight = 1080f;

        /// <summary>
        /// Sorting order for the HUD. Below the cold-open video (900) so a cutscene still
        /// covers the board, and above the world so gameplay never draws over a readout.
        /// </summary>
        private const int HudSortingOrder = 100;

        private static Canvas cached;

        /// <summary>
        /// Returns the gameplay HUD canvas, creating it on first use. Always the same canvas
        /// for every caller in a scene, which is the whole point.
        /// </summary>
        public static Canvas Resolve()
        {
            // Unity-null aware: a scene load destroys the object while the C# reference lives on,
            // and `cached != null` is the comparison that notices.
            if (cached != null) return cached;

            var existing = GameObject.Find(CanvasName);
            if (existing != null)
            {
                cached = existing.GetComponent<Canvas>();
                if (cached != null) return cached;
                Object.Destroy(existing); // name taken by something that is not a canvas
            }

            var go = new GameObject(CanvasName);
            cached = go.AddComponent<Canvas>();
            cached.renderMode = RenderMode.ScreenSpaceOverlay;
            cached.sortingOrder = HudSortingOrder;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(ReferenceWidth, ReferenceHeight);
            // Match on height. The board is laid out horizontally and browser windows are
            // usually wider than they are tall, so matching width (or averaging, as before)
            // let a short window shrink the text further than the artwork needed.
            scaler.matchWidthOrHeight = 1f;

            go.AddComponent<GraphicRaycaster>();
            MobileSafeArea.ConfigureCanvas(cached);

            // Last, deliberately. ConfigureCanvas sets ScaleWithScreenSize, and HudScaleFloor's
            // Awake switches to a factor it computes itself — the clamp only holds if it runs
            // after, or the scaler recomputes it away.
            go.AddComponent<HudScaleFloor>();
            return cached;
        }

        /// <summary>
        /// The safe-area-inset root every HUD element should parent to.
        /// </summary>
        public static RectTransform Root() => MobileSafeArea.GetContentRoot(Resolve());

        /// <summary>
        /// Drops the cached reference. Called on scene teardown so the next scene builds its
        /// own canvas instead of resurrecting a destroyed one.
        /// </summary>
        public static void Forget() => cached = null;

        /// <summary>
        /// Moves a scene-authored HUD element onto the HUD canvas, keeping its layout.
        ///
        /// Labels placed in the scene sit on the scene's own canvas, which is ConstantPixelSize:
        /// their text stays the same pixel height at every window, so it is 4.2% of screen
        /// height at 1024x576 and 1.1% at 4K — the HUD shrinks as the display grows. The
        /// code-built labels scale with the screen and hold 2.4% everywhere. Two rules on one
        /// HUD is the defect; this brings the scene ones onto the rule that holds.
        ///
        /// Anchors, pivot and anchoredPosition are expressed relative to the parent rect, and
        /// both canvases are full-screen overlays, so the transfer preserves position exactly.
        /// The scene keeps ownership of *where* the label sits; the HUD canvas owns *how big*
        /// it draws.
        /// </summary>
        public static void Adopt(Component element)
        {
            if (element == null) return;
            var rect = element.transform as RectTransform;
            if (rect == null) return;

            var root = Root();
            if (root == null || rect.parent == root) return;

            var anchorMin = rect.anchorMin;
            var anchorMax = rect.anchorMax;
            var pivot = rect.pivot;
            var anchored = rect.anchoredPosition;
            var size = rect.sizeDelta;

            rect.SetParent(root, false);

            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchored;
            rect.sizeDelta = size;
        }

        /// <summary>
        /// Smallest window the HUD promises to stay readable at. Chosen from the deployed
        /// surface: the WebGL canvas fills the browser window, and 1024x576 is the smallest
        /// 16:9 viewport a laptop browser realistically presents.
        /// </summary>
        public const float MinSupportedHeight = 576f;

        /// <summary>
        /// Scale factor at <see cref="MinSupportedHeight"/> — 0.533. Any HUD point size must
        /// clear the legibility floor after this multiplication, which is what sets the sizes
        /// below.
        /// </summary>
        public static float WorstCaseScale => MinSupportedHeight / ReferenceHeight;

        /// <summary>
        /// Pixel size below which this SDF face visibly drops a glyph's thin horizontal strokes.
        ///
        /// Bounded by observation rather than a swept threshold, because two attempts at
        /// measuring the exact break point failed and both failures are worth recording:
        ///
        ///   - comparing total ink area across sizes could not see it at all. A lost crossbar
        ///     is a few percent of a glyph's ink, under the sampling noise; the sweep reported
        ///     10px broken, 9px fine, 7px broken, 6px fine.
        ///   - comparing a native render against a supersampled reference measured nothing but
        ///     misalignment: scaling fontSize by 8 moves every glyph, so the two images
        ///     disagreed 130-160% everywhere, including at sizes that are plainly fine.
        ///
        /// What is actually known, from frames rather than statistics:
        /// 17px on a scale-1 canvas renders "KEEP CORE 150/150" intact
        /// (`qa/evidence/font/probe-17.png`), and 6.5px renders it as "KLLP CORL"
        /// (`qa/evidence/font/hud-font-scale.md` plus the in-game capture). The floor is
        /// therefore somewhere in (6.5, 17]; 12 is chosen inside that interval with margin on
        /// the side that costs nothing — a slightly larger label. Narrowing it needs a
        /// per-glyph structural check, not another area metric.
        /// </summary>
        public const float LegibleFloorPixels = 12f;

        /// <summary>
        /// Point size for a primary HUD readout — core health, turn state, anything the player
        /// reads to decide a shot. 26 x 0.533 = 13.9px at the smallest supported window.
        /// </summary>
        public const float PrimaryLabelSize = 26f;

        /// <summary>
        /// Point size for secondary readouts. 23 x 0.533 = 12.3px — the smallest size that
        /// still clears <see cref="LegibleFloorPixels"/>, so nothing on the HUD is authored
        /// below the floor. The previous 17/15/14 sizes were not: they became 9.1/8.0/7.5px.
        /// </summary>
        public const float SecondaryLabelSize = 23f;
    }
}
