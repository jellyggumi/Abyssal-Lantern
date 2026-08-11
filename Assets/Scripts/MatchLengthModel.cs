using UnityEngine;
using System.Collections.Generic;

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

    /// <summary>
    /// Explicit input values for the repeatable siege pacing measurement.  This is deliberately
    /// data-only: scene physics can use these values, but visual systems must not alter a result.
    /// Damage per shot is <c>baseShotDamage * automaticProjectileMultiplier * aimQuality</c> and
    /// one keep's durability is <c>wallBlockCount * wallBlockHp + coreHp</c>.
    /// </summary>
    public sealed class SiegeBalanceSettings
    {
        public const string DefaultMapId = "SampleScene";
        public const string DefaultSiegeWeaponId = "StandardSlingshot";

        public readonly string mapId;
        public readonly string siegeWeaponId;
        public readonly int wallBlockCount;
        public readonly float wallBlockHp;
        public readonly float coreHp;
        public readonly float baseShotDamage;
        public readonly float secondsPerTurn;
        public readonly float fixedAimQuality;
        public readonly float beginnerAimError;

        public SiegeBalanceSettings(
            string mapId,
            string siegeWeaponId,
            int wallBlockCount,
            float wallBlockHp,
            float coreHp,
            float baseShotDamage,
            float secondsPerTurn,
            float fixedAimQuality,
            float beginnerAimError)
        {
            this.mapId = mapId;
            this.siegeWeaponId = siegeWeaponId;
            this.wallBlockCount = wallBlockCount;
            this.wallBlockHp = wallBlockHp;
            this.coreHp = coreHp;
            this.baseShotDamage = baseShotDamage;
            this.secondsPerTurn = secondsPerTurn;
            this.fixedAimQuality = fixedAimQuality;
            this.beginnerAimError = beginnerAimError;
        }

        /// <summary>Five-minute tuning: 12 * 90 + 360 durability against 120 base damage.</summary>
        public static SiegeBalanceSettings Default => new SiegeBalanceSettings(
            DefaultMapId,
            DefaultSiegeWeaponId,
            12,
            90f,
            360f,
            120f,
            8.5f,
            0.70f,
            0.09f);

        public float KeepDurability => wallBlockCount * wallBlockHp + coreHp;
    }

    public enum SiegeSimulationProfile
    {
        FixedAim,
        BeginnerAimError
    }

    public struct SiegeMatchMeasurement
    {
        public readonly string mapId;
        public readonly string siegeWeaponId;
        public readonly SiegeSimulationProfile profile;
        public readonly int seed;
        public readonly float initialKeepDurability;
        public readonly float durationSeconds;
        public readonly int turns;

        public SiegeMatchMeasurement(
            string mapId,
            string siegeWeaponId,
            SiegeSimulationProfile profile,
            int seed,
            float initialKeepDurability,
            float durationSeconds,
            int turns)
        {
            this.mapId = mapId;
            this.siegeWeaponId = siegeWeaponId;
            this.profile = profile;
            this.seed = seed;
            this.initialKeepDurability = initialKeepDurability;
            this.durationSeconds = durationSeconds;
            this.turns = turns;
        }
    }

    /// <summary>
    /// Deterministic, headless pacing measurement.  Each sample starts from the same map, keep
    /// durability, and weapon; the sole variable in the beginner profile is a bounded aim error.
    /// A profile step is one alternating one-shot turn, so no deployment or player projectile
    /// choice can influence this gate.
    /// </summary>
    public static class SiegePacingSimulation
    {
        public const int RequiredBeginnerMatches = 20;
        public const float MinimumAverageSeconds = 270f;
        public const float MaximumAverageSeconds = 330f;
        public const float EarlyEndSeconds = 180f;
        public const int MaximumEarlyEndMatches = 2;

        // The game rule selects this sequence; its mean is one so automatic variety cannot
        // silently move the long-term damage budget.
        private static readonly float[] AutomaticProjectileMultipliers = { 1f, 0.95f, 1.05f };

        public static SiegeMatchMeasurement RunFixedAim(SiegeBalanceSettings settings)
        {
            return Run(settings, SiegeSimulationProfile.FixedAim, 0);
        }

        public static List<SiegeMatchMeasurement> RunBeginnerSeries(SiegeBalanceSettings settings, int seed)
        {
            var measurements = new List<SiegeMatchMeasurement>(RequiredBeginnerMatches);
            for (int match = 0; match < RequiredBeginnerMatches; match++)
                measurements.Add(Run(settings, SiegeSimulationProfile.BeginnerAimError, seed + match));
            return measurements;
        }

        public static float AverageDuration(IList<SiegeMatchMeasurement> measurements)
        {
            if (measurements == null || measurements.Count == 0) return 0f;

            float total = 0f;
            for (int i = 0; i < measurements.Count; i++) total += measurements[i].durationSeconds;
            return total / measurements.Count;
        }

        public static int EarlyEndCount(IList<SiegeMatchMeasurement> measurements)
        {
            if (measurements == null) return 0;

            int count = 0;
            for (int i = 0; i < measurements.Count; i++)
                if (measurements[i].durationSeconds < EarlyEndSeconds) count++;
            return count;
        }

        private static SiegeMatchMeasurement Run(
            SiegeBalanceSettings settings,
            SiegeSimulationProfile profile,
            int seed)
        {
            float playerKeep = settings.KeepDurability;
            float enemyKeep = settings.KeepDurability;
            var random = new DeterministicAimRandom((uint)(seed + 1));
            int turns = 0;

            while (playerKeep > 0f && enemyKeep > 0f)
            {
                float aimQuality = settings.fixedAimQuality;
                if (profile == SiegeSimulationProfile.BeginnerAimError)
                    aimQuality += random.NextSignedUnit() * settings.beginnerAimError;

                aimQuality = Mathf.Clamp01(aimQuality);
                float projectileMultiplier = AutomaticProjectileMultipliers[(turns / 2) % AutomaticProjectileMultipliers.Length];
                float damage = settings.baseShotDamage * projectileMultiplier * aimQuality;

                if ((turns & 1) == 0) enemyKeep -= damage;
                else playerKeep -= damage;
                turns++;
            }

            return new SiegeMatchMeasurement(
                settings.mapId,
                settings.siegeWeaponId,
                profile,
                seed,
                settings.KeepDurability,
                turns * settings.secondsPerTurn,
                turns);
        }

        /// <summary>Small platform-independent PRNG so a seed remains a reproducible measurement.</summary>
        private struct DeterministicAimRandom
        {
            private uint state;

            public DeterministicAimRandom(uint seed) { state = seed == 0 ? 1u : seed; }

            public float NextSignedUnit()
            {
                state = state * 1664525u + 1013904223u;
                return ((state >> 8) / 16777215f) * 2f - 1f;
            }
        }
    }
}
