using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Screen-handling arithmetic for the siege camera — zoom limits and aim framing —
    /// kept pure so the rules are pinned without a scene.
    ///
    /// Why the input channel is deliberately narrow (usability request, 2026-08-13 —
    /// 화면 핸들링): press-and-drag ANYWHERE is the launch gesture, pinned by
    /// LaunchManager_DragFromAnywhere_AimsByGestureNotByAbsolutePosition. A drag-to-pan
    /// camera would turn every pan attempt into a zero-power draw, so panning is not
    /// offered at all. Zoom rides the wheel / pinch, which the sling never reads, and the
    /// framing the player needs most — seeing the target while pulling — is automatic.
    /// </summary>
    public static class CameraFraming
    {
        /// <summary>Closest the player may zoom in, as a fraction of the fitted board.</summary>
        public const float MinZoom = 1.0f;
        /// <summary>Furthest out, for reading the whole field before committing a shot.</summary>
        public const float MaxZoom = 1.6f;
        public const float ZoomStep = 0.12f;

        /// <summary>Extra width the view opens to while the sling is drawn.</summary>
        public const float AimZoomOut = 1.18f;
        /// <summary>How far the view slides toward the sling while aiming, 0..1.</summary>
        public const float AimShiftWeight = 0.34f;
        public const float AimEasePerSecond = 3.2f;

        /// <summary>
        /// Zoom clamped to the legal band.
        ///
        /// The floor is 1.0 — the aspect-fitted board — on purpose: zooming in past the fit
        /// crops the field, and on a wide monitor the first thing to leave the screen is the
        /// enemy keep the player is aiming at. Losing the target to a stray scroll is a
        /// worse failure than not being able to inspect masonry up close.
        /// </summary>
        public static float ClampZoom(float zoom) => Mathf.Clamp(zoom, MinZoom, MaxZoom);

        /// <summary>Applies one wheel/pinch notch. Positive scroll zooms in.</summary>
        public static float ApplyZoomInput(float currentZoom, float scrollDelta)
        {
            return ClampZoom(currentZoom - scrollDelta * ZoomStep);
        }

        /// <summary>
        /// Orthographic size for a given fitted base and zoom. Separate from
        /// <see cref="GamePresentationDirector.CalculateOrthographicSize"/> so the fit rule
        /// (board must fit the aspect) and the zoom rule stay independently pinnable.
        /// </summary>
        public static float SizeForZoom(float fittedSize, float zoom) => fittedSize * ClampZoom(zoom);

        /// <summary>
        /// Eases the aim-framing weight toward its target. Framed as a rate rather than a
        /// duration so the ease is frame-rate independent — a 30fps browser tab and a 144Hz
        /// desktop must widen at the same speed or the aim feels different per machine.
        /// </summary>
        public static float EaseAimWeight(float current, bool aiming, float deltaTime)
        {
            float target = aiming ? 1f : 0f;
            float t = 1f - Mathf.Exp(-AimEasePerSecond * Mathf.Max(0f, deltaTime));
            return Mathf.Lerp(current, target, Mathf.Clamp01(t));
        }

        /// <summary>Zoom multiplier contributed by aim framing at the given weight.</summary>
        public static float AimZoomMultiplier(float aimWeight) =>
            Mathf.Lerp(1f, AimZoomOut, Mathf.Clamp01(aimWeight));

        /// <summary>
        /// Where the camera centre sits while aiming: slid from the board centre toward the
        /// player's sling, so the pouch being pulled and the keep being aimed at are on
        /// screen together. Never travels the full distance — centring ON the sling would
        /// push the target off the far edge, which is the framing this replaces.
        /// </summary>
        public static float AimCenterX(float boardCenterX, float slingX, float aimWeight)
        {
            float shift = Mathf.Clamp01(aimWeight) * AimShiftWeight;
            return Mathf.Lerp(boardCenterX, slingX, shift);
        }
    }
}
