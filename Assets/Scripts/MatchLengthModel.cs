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

        /// <summary>s — seconds a turn actually takes at the table, not the turn timer's cap.
        /// Dropped from 8.5 when the AI's pre-shot dead air went 3.0s → 0.9s (arcade tempo
        /// pass): enemy turns now spend their time on the shot, not the wait.</summary>
        public const float AverageTurnSeconds = 7.5f;

        /// <summary>
        /// d — effective material one side removes from the OTHER side's keep per shot it takes.
        ///
        /// Recalibrated 37 → 85.7 in the same edit that fixed <see cref="TurnsToDecide"/>, because
        /// the two were wrong together and either alone breaks the estimate. The old 37 was never a
        /// measured rate: it compensated for the equation counting one side's shots as if they were
        /// all of the match's turns. 85.7 / 37 = 2.32, and the factor the equation was missing is 2.
        ///
        /// Calibrated against observed turn counts rather than raw measurement, deliberately. B1
        /// measured 96.59 material removed per player shot (Stage1, 22 shots), but real matches ran
        /// 35-39 turns where that rate predicts 32.8 — because a shot's material does not all reach
        /// the enemy keep. Self-inflicted destruction is 26-42% of the total and terrain absorbs
        /// shots that never arrive, and the model has a term for neither. Setting d to the raw rate
        /// would make the equation confidently wrong in a new direction; setting it to what
        /// reproduces 37 turns keeps it honest about being a fit.
        /// qa/evidence/g2/aim-error-conversion.md, qa/b1-measurement-findings.md
        /// </summary>
        public const float EffectiveDamagePerTurn = 85.7f;

        /// <summary>M = b·h + c</summary>
        public static float Material(int blocksPerKeep, float blockHealth, float coreHealth)
            => blocksPerKeep * blockHealth + coreHealth;

        /// <summary>
        /// N = 2·M / d — turns, not shots.
        ///
        /// THE FACTOR OF 2 IS THE FIX. `d` is what ONE side removes per shot IT takes, and a turn
        /// belongs to one side, so a match in which the attacker needs M/d shots spans twice that
        /// many turns: the defender takes one between each. The equation read `M / d` and called the
        /// result turns, losing the alternation the whole game is built on.
        ///
        /// Measured both ways against real matches (Stage1, M=1585, d as measured 96.59): the old
        /// form predicts 16.4 turns against 35-39 observed, off by 2.1-2.4x. With the factor it
        /// predicts 32.8, and calibrating d closes the rest.
        /// </summary>
        public static float TurnsToDecide(float material, float damagePerTurn)
            => 2f * material / Mathf.Max(0.01f, damagePerTurn);

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
        /// changing the target, so walls are derived from the goal rather than guessed toward it.
        /// Carries the same factor of 2 as <see cref="TurnsToDecide"/> — an inverse that dropped it
        /// would hand back half the material the forward direction needs.</summary>
        public static float MaterialForTargetSeconds(float targetSeconds)
            => targetSeconds / AverageTurnSeconds * EffectiveDamagePerTurn / 2f;

        // ---- Measured decomposition (B1, 2026-08-14) -------------------------------------
        //
        // Everything above treats d as one constant. Three measured matches put the attacker's own
        // rate at 96.59 / 128.33 / 128.00 for Stage1/2/3 against the shipped 37 — a consistent 2.6x
        // to 3.5x understatement, spread only 1.33x between stages.
        //
        // An earlier version of this comment claimed a 24x spread. That was Stage3 measured while a
        // ground-atlas exception aborted its boot, so it had no wall courses and no core: its d read
        // 5.31 because there was nothing to hit. Re-measured with the castle present it is 128.00.
        // A single constant is therefore defensible after all — it is simply mis-set. Data:
        // qa/b1-measurement-findings.md, qa/evidence/match-length/b1-stage3-remeasured.md.
        //
        // The constants above are LEFT IN PLACE anyway, for two reasons that outlived the correction.
        // First, 26-42% of each keep's material loss is inflicted by its own owner — a launch apron
        // at ±17 firing over its own courses at ±4..7 — so a d fitted to observed material loss
        // credits the attacker for the defender's mistakes. Second, the model's OTHER input is wrong
        // by nearly the same factor: the live castle carries 3.1-3.8x the material the equation
        // counts, because the census walks KeepProfile courses while the board also parents ground
        // tiles and the core under the same transform. The two understatements cancel to 0.89-1.46x,
        // which is why the pacing gate reads plausible. Correcting d alone drops every stage from
        // ~300s to 91-123s, moving the gate from wrong-and-green to wrong-and-red.

        /// <summary>
        /// d factorised into the two things that can actually be measured separately.
        ///
        /// <c>d = p · q</c> where <c>p</c> is the fraction of shots that damage anything and
        /// <c>q</c> is the damage a landed shot does. The identity is trivial — p·q reduces to
        /// dealt/shots — and that is not the point. The point is that p and q say different things
        /// and have different fixes:
        ///
        /// <code>
        ///            p (hit rate)   q (per landed)      d
        ///   Stage1        0.73            132.8      96.59
        ///   Stage2        0.83            154.0     128.33
        ///   Stage3        0.57            224.0     128.00
        /// </code>
        ///
        /// Stage3's row is the re-measured one. Before its castle existed it read p=0.19, q=28.3,
        /// d=5.31, and that row is what made the spread look like 24x — a factorisation inherits
        /// whatever defect its measurement had.
        ///
        /// Within a stage the distribution is still heavy-tailed: Stage3's median damage per shot is
        /// 62.5 against a mean of 128.00, 43% of shots deal nothing, and one shot did 560. A model
        /// consuming only the mean cannot express "many shots accomplish nothing", which is the
        /// complaint that opened this investigation.
        /// </summary>
        public static float DamagePerTurn(float hitRate, float damagePerLandedShot)
            => Mathf.Clamp01(hitRate) * Mathf.Max(0f, damagePerLandedShot);

        /// <summary>
        /// SHOTS the attacker needs to remove a keep, given the measured factors.
        ///
        /// Deliberately shots, not turns. <see cref="TurnsToDecide"/> multiplies by 2 because the
        /// sides alternate; this one stops before that, so the two are not interchangeable and the
        /// names now say which is which. The earlier version of this docstring warned that
        /// `TurnsToDecide` "silently means the same thing while reading as if it covered the whole
        /// match" — that warning was correct and is now obsolete, because the factor was the defect
        /// and it is fixed.
        ///
        /// What remains unmodelled: a keep also loses material to its OWN owner's shots, and in
        /// Stage3 that channel was once larger than the attacker's. A caller that wants match length
        /// from first principles needs both — see <see cref="SelfInflictedShareIsNotModelled"/>. The
        /// shipped <see cref="EffectiveDamagePerTurn"/> absorbs that gap by being calibrated against
        /// observed turn counts rather than derived from this function.
        /// </summary>
        public static float AttackerShotsToRemove(float material, float hitRate, float damagePerLandedShot)
            => material / Mathf.Max(0.01f, DamagePerTurn(hitRate, damagePerLandedShot));

        /// <summary>
        /// The measured share of a keep's material loss that its OWN side inflicted, per stage.
        ///
        /// Documented as data rather than folded into an equation, because the honest thing to
        /// report is that the model has no term for it. Adding one that reproduces these numbers
        /// would be an identity — total divided by the sum of two rates derived from that same
        /// total returns the observed turn count by construction, predicting nothing. Closing this
        /// properly needs the three causes separated first (own shots, the flying beast's wall
        /// rams, collapse chains crossing a turn boundary), which B1 did not do.
        /// </summary>
        public static readonly (string stage, float selfShare)[] SelfInflictedShareIsNotModelled =
        {
            ("Stage1", 0.39f),
            ("Stage2", 0.42f),
            // 0.26 re-measured with Stage3's castle present. It read 0.67 while the stage booted
            // without walls or a core, which made the defender look like the primary demolisher; with
            // the keep standing the attacker is comfortably ahead. Still far too large to ignore.
            ("Stage3", 0.26f),
        };
    }

    /// <summary>
    /// Explicit input values for the repeatable siege pacing measurement.  This is deliberately
    /// data-only: scene physics can use these values, but visual systems must not alter a result.
    /// Damage per shot is <c>baseShotDamage * automaticProjectileMultiplier *
    /// openingVolleyMultiplier * aimQuality</c>; one keep's durability is
    /// <c>wallBlockCount * wallBlockHp + coreHp</c>.
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

        /// <summary>Five-minute tuning: 12 * 90 + 360 durability against 106 base damage at
        /// 7.5s turns. The arcade tempo pass shortened the turn (8.5 → 7.5s) and the damage
        /// came down with it (120 → 106) so the average beginner match stays inside the
        /// 270–330s acceptance band — faster beats, same five-minute siege.</summary>
        public static SiegeBalanceSettings Default => new SiegeBalanceSettings(
            DefaultMapId,
            DefaultSiegeWeaponId,
            12,
            90f,
            360f,
            106f,
            7.5f,
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
                float damage = settings.baseShotDamage * projectileMultiplier *
                               OneShotSiegeRules.OpeningVolleyDamageMultiplier(turns) * aimQuality;

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
