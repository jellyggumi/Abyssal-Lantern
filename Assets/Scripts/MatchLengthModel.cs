using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// How long a decided match should take, written as an equation instead of a feeling.
    ///
    /// Match length had been tuned by adding and removing walls until it "felt right", which is
    /// why it swung from ending in a single volley to not ending at all. The relationship is
    /// simple enough to state outright:
    ///
    ///     M = b · h + c                    material one side must lose
    ///     N = M / d                        turns to decide
    ///     T = N · s = (b · h + c) · s / d  seconds to decide
    ///
    ///     b  blocks in one keep      (GameManager.BlocksPerKeep)
    ///     h  health per wall block   (StoneBlockData.maxHP)
    ///     c  keep core health        (CastleCoreGimmick.CoreMaxHP)
    ///     d  effective damage landed per turn — on-target damage only, so it is well under a
    ///        unit's raw damage: most turns chip, some miss, a few collapse a column
    ///     s  seconds a player actually spends on a turn, which is NOT the turn timer. The
    ///        timer (GameManager.turnDuration) is a ceiling players rarely reach; aiming and
    ///        firing takes about eight seconds.
    ///
    /// Everything here is per side, because a match ends when ONE keep falls, not both.
    ///
    /// The two calibration constants (d and s) are the honest weak point: they come from
    /// watching play rather than from instrumentation. They are named and isolated here so that
    /// when telemetry does measure them, one edit re-derives the whole balance rather than
    /// sending someone back to guessing at wall counts.
    /// </summary>
    public static class MatchLengthModel
    {
        /// <summary>The design target: a decided match runs about five minutes.</summary>
        public const float TargetMatchSeconds = 300f;

        /// <summary>Acceptable spread around the target before the tuning is considered off.
        /// ±20% is roughly the point where a match stops reading as "about five minutes".</summary>
        public const float ToleranceFraction = 0.2f;

        /// <summary>s — seconds a turn actually takes at the table, not the turn timer's cap.</summary>
        public const float AverageTurnSeconds = 8.5f;

        /// <summary>d — effective on-target damage a side lands per turn.</summary>
        public const float EffectiveDamagePerTurn = 42f;

        /// <summary>M = b·h + c</summary>
        public static float Material(int blocksPerKeep, float blockHealth, float coreHealth)
            => blocksPerKeep * blockHealth + coreHealth;

        /// <summary>N = M / d</summary>
        public static float TurnsToDecide(float material, float damagePerTurn)
            => material / Mathf.Max(0.01f, damagePerTurn);

        /// <summary>T = N · s</summary>
        public static float SecondsToDecide(float material, float damagePerTurn, float turnSeconds)
            => TurnsToDecide(material, damagePerTurn) * turnSeconds;

        /// <summary>T for the shipped constants — the number the balance is actually aiming at.</summary>
        public static float EstimatedMatchSeconds(int blocksPerKeep, float blockHealth, float coreHealth)
            => SecondsToDecide(
                Material(blocksPerKeep, blockHealth, coreHealth),
                EffectiveDamagePerTurn,
                AverageTurnSeconds);

        /// <summary>Inverse of the model: the material a target length implies. Use this when
        /// changing the target, so walls are derived from the goal rather than guessed toward it.</summary>
        public static float MaterialForTargetSeconds(float targetSeconds)
            => targetSeconds / AverageTurnSeconds * EffectiveDamagePerTurn;
    }
}
