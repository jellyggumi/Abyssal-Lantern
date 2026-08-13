using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// Stops the HUD from scaling itself into illegibility on a small window.
    ///
    /// <see cref="CanvasScaler.ScaleMode.ScaleWithScreenSize"/> keeps the HUD a constant
    /// fraction of the screen, which is what we want down to a point and wrong past it. The
    /// WebGL canvas fills the browser window, so there is no lower bound on how small that
    /// window gets, and below roughly 1024x576 a 26pt label lands under the pixel size where
    /// this SDF face keeps a glyph's thin horizontal strokes — "KEEP CORE" starts reading as
    /// "KLLP CORL".
    ///
    /// So the scale is clamped: proportional above the floor, fixed below it. Under the floor
    /// the HUD occupies a larger share of a small window, which is the correct trade — a
    /// reader can ignore a HUD that is bigger than they would like, and cannot read one whose
    /// letters have holes in them.
    ///
    /// Runs the arithmetic itself rather than post-correcting CanvasScaler, because the scaler
    /// recomputes in its own update and a value written after it is overwritten next frame.
    /// </summary>
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class HudScaleFloor : MonoBehaviour
    {
        private CanvasScaler scaler;
        private int lastWidth;
        private int lastHeight;

        private void Awake()
        {
            scaler = GetComponent<CanvasScaler>();
            // Constant mode, with the factor supplied here. Leaving the scaler in
            // ScaleWithScreenSize would have it recompute and discard the clamp.
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            Apply();
        }

        private void Update()
        {
            if (Screen.width == lastWidth && Screen.height == lastHeight) return;
            Apply();
        }

        private void Apply()
        {
            lastWidth = Screen.width;
            lastHeight = Screen.height;
            scaler.scaleFactor = ScaleFor(lastHeight);
        }

        /// <summary>
        /// Scale factor for a given screen height: proportional to the reference above the
        /// supported floor, pinned at the floor's value below it.
        ///
        /// Pure and public so the contract can be asserted without a screen.
        /// </summary>
        public static float ScaleFor(int screenHeight)
        {
            if (screenHeight <= 0) return 1f;
            var proportional = screenHeight / HudCanvas.ReferenceHeight;
            var floor = HudCanvas.MinSupportedHeight / HudCanvas.ReferenceHeight;
            return Mathf.Max(proportional, floor);
        }
    }
}
