using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Turns draw depth into launch speed.
    ///
    /// This exists because the aiming space was measured and the dial was the defect. Firing at
    /// 45 degrees in the live scene landed at x = −8.96 / 0.23 / 18.25 / 25.04 for 40/60/80/100%
    /// draw: an eighteen-unit jump between 60% and 80% across a keep that is five and a half units
    /// wide. The window that lands on the keep was six percentage points hidden inside a
    /// twenty-point step, which is what "the angle cannot hit the enemy" actually was — the angle
    /// was fine and the power dial was far coarser than the target.
    ///
    /// Two causes, both arithmetic:
    ///
    /// 1. **Range goes as the square of speed.** With speed linear in draw, distance goes as draw
    ///    SQUARED, so the back half of the pull covers most of the board. Taking the square root
    ///    of draw makes speed ∝ √draw and therefore distance ∝ draw — the pull becomes linear in
    ///    the thing the player is actually aiming.
    /// 2. **The cap was sized for a board we do not have.** Reaching the enemy core needs 26u;
    ///    v = 25.2 covers 64.7u at 45°, so roughly three fifths of the pull existed only to
    ///    overshoot. Sizing the cap so a full draw lands just past the keep puts the whole pull
    ///    range back in play.
    ///
    /// Measured effect at 45° (offline model of the same integration the runtime uses): the draw
    /// band that lands on the keep widens from 11.2%p to 28.6%p, and once the walls are down the
    /// band that hits the core widens from two notches to seven.
    ///
    /// What this deliberately does NOT do: it does not shorten the apron. That distance is level
    /// design — the "widened board pass" moved it to ±17 on purpose to make the midfield real —
    /// and 61% of the grid falling short is dominated by the 23u of dead ground between the apron
    /// and the keep's near edge, not by this curve. Changing both at once would make neither
    /// measurable. `qa/aim-space-reachability.md`
    /// </summary>
    public static class LaunchPowerCurve
    {
        /// <summary>
        /// Speed cap at a full draw.
        ///
        /// 17.5 puts a full 45° draw at x ≈ 14.2 — just past the keep's far edge at 11.5, so the
        /// player can still overshoot deliberately, but the pull no longer spends most of its
        /// travel beyond the board. The previous 25.2 reached 47.7.
        ///
        /// Not a free parameter: raising it re-narrows the window (the same speed range compresses
        /// into the same pull), which is why the earlier instinct to raise the cap was wrong.
        /// </summary>
        public const float MaxSpeed = 17.5f;

        /// <summary>
        /// Exponent applied to normalized draw. 0.5 is the square root, chosen because range is
        /// quadratic in speed and this is exactly its inverse — not a feel constant.
        /// </summary>
        public const float DrawExponent = 0.5f;

        /// <summary>
        /// Minimum draw that counts as a shot, as a FRACTION OF THE PULL rather than a speed.
        ///
        /// The weak-pull coaching ("더 깊게 당긴 뒤 발사", task #29) used to be a speed threshold of
        /// 3 against a 25.2 cap — 11.9% of the draw. Under this curve the same 3 m/s arrives at
        /// 2.9% of the draw, so the coaching would have quietly stopped firing: a shallow flick
        /// that used to be refused would launch. Pinning the gesture instead of the speed keeps the
        /// contract intact through any future curve change, which is the whole reason it moved.
        /// </summary>
        public const float MinDrawFraction = 0.119f;

        /// <summary>
        /// Speed for a normalized draw in 0..1, against a caller-supplied cap.
        ///
        /// The cap is a parameter, not the constant, because <c>LaunchManager.maxLaunchVelocity</c>
        /// is a serialized per-instance field: tests set it to 12, and a scene could set anything.
        /// An earlier revision of this read <see cref="MaxSpeed"/> directly and silently ignored
        /// both — caught by <c>LaunchManager_CalculatesBowstringVelocity_WithClamp</c>, which sets
        /// 12 and got 17.5.
        ///
        /// A zero draw is a zero shot, and the curve is monotone so a deeper pull is always a
        /// longer shot — the two properties the gesture has to keep for the pull to be learnable.
        /// </summary>
        public static float SpeedForDraw(float normalizedDraw, float maxSpeed)
        {
            float t = Mathf.Clamp01(normalizedDraw);
            if (t <= 0f || maxSpeed <= 0f) return 0f;
            return maxSpeed * Mathf.Pow(t, DrawExponent);
        }

        /// <summary>Convenience overload against the tuned cap.</summary>
        public static float SpeedForDraw(float normalizedDraw) => SpeedForDraw(normalizedDraw, MaxSpeed);

        /// <summary>
        /// The draw that would produce <paramref name="speed"/> — the inverse of
        /// <see cref="SpeedForDraw"/>. Used to report power as a percentage of the pull the player
        /// actually made, rather than of the speed, so the HUD number matches the gesture.
        /// </summary>
        public static float DrawForSpeed(float speed, float maxSpeed)
        {
            if (speed <= 0f || maxSpeed <= 0f) return 0f;
            float ratio = Mathf.Clamp01(speed / maxSpeed);
            return Mathf.Pow(ratio, 1f / DrawExponent);
        }

        public static float DrawForSpeed(float speed) => DrawForSpeed(speed, MaxSpeed);

        /// <summary>
        /// Speed below which a shot is refused, for a given cap. Derived from
        /// <see cref="MinDrawFraction"/> so the refused GESTURE stays fixed even though the speed
        /// it corresponds to moves with the curve.
        /// </summary>
        public static float MinLaunchSpeed(float maxSpeed) => SpeedForDraw(MinDrawFraction, maxSpeed);
    }
}
