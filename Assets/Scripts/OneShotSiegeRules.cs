using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Pure rules for the one-shot siege loop.  The launcher owns presentation and physics;
    /// this type owns the rules that must remain deterministic across player and AI turns.
    /// </summary>
    public static class OneShotSiegeRules
    {
        public enum Projectile { Knight, Archer, Barrel }

        private static readonly Projectile[] projectileCycle =
        {
            Projectile.Knight,
            Projectile.Archer,
            Projectile.Barrel
        };

        public const float OpeningVolleyDamageScale = 0.5f;

        /// <summary>
        /// Both factions use the same predictable round cycle.  A round is two turns, so each
        /// side receives the same automatically selected projectile before the cycle advances.
        /// Cannon is deliberately absent: installations are not part of a one-shot volley.
        /// </summary>
        public static Projectile ProjectileForTurn(int completedTurns)
        {
            int round = Mathf.Max(0, completedTurns) / 2;
            return projectileCycle[round % projectileCycle.Length];
        }

        /// <summary>
        /// The side that shoots first gets tempo before the defender can answer. Reducing only
        /// that opening volley to 50% removes the measured 87% first-mover win rate without
        /// changing projectile identity, later-turn damage, or who takes the first shot.
        /// </summary>
        public static float OpeningVolleyDamageMultiplier(int completedTurns)
            => completedTurns <= 0 ? OpeningVolleyDamageScale : 1f;

        /// <summary>Player-facing name. The rule owns this so the HUD cannot drift from the
        /// projectile actually loaded, which is the whole point of telegraphing it.</summary>
        public static string DisplayName(Projectile projectile)
        {
            switch (projectile)
            {
                case Projectile.Archer: return "궁수";
                case Projectile.Barrel: return "화약통";
                default: return "기사";
            }
        }

        /// <summary>
        /// What the NEXT turn will load. The cycle is fully deterministic, so this is knowable
        /// and was knowable all along — it simply had no reader. Without it the player learns
        /// their own weapon only as their turn opens and never learns the enemy's at all, which
        /// makes a predictable rule read as a random one.
        /// </summary>
        public static Projectile ProjectileForNextTurn(int completedTurns)
            => ProjectileForTurn(Mathf.Max(0, completedTurns) + 1);

        /// <summary>
        /// Pure apply boundary: multiplies damage by an already-captured multiplier. Never
        /// reads GameManager or any other mutable state — callers must capture the multiplier
        /// once (GameManager.CaptureDamageMultiplier) at action/projectile creation and carry
        /// the returned value through every delayed impact, so this is safe to call at impact
        /// time without re-deriving eligibility from whatever turn happens to be active then.
        /// </summary>
        public static float ApplyDamageMultiplier(float damage, float multiplier) => damage * multiplier;

        public static float ClampAngle(float degrees) => Mathf.Clamp(degrees, 10f, 80f);
        public static float ClampPower(float normalizedPower) => Mathf.Clamp01(normalizedPower);

        /// <summary>
        /// Angle and power are intentionally independent inputs.  A direction of 1 is a
        /// player volley (left-to-right); -1 mirrors the exact same aim for the enemy.
        /// </summary>
        public static Vector2 Velocity(float angleDegrees, float normalizedPower, float minSpeed, float maxSpeed, float direction)
        {
            float angle = ClampAngle(angleDegrees) * Mathf.Deg2Rad;
            float speed = Mathf.Lerp(Mathf.Max(0f, minSpeed), Mathf.Max(minSpeed, maxSpeed), ClampPower(normalizedPower));
            return new Vector2(Mathf.Cos(angle) * Mathf.Sign(direction), Mathf.Sin(angle)) * speed;
        }
    }

    /// <summary>Per-turn guard that makes a second launch impossible until the next turn begins.</summary>
    public sealed class OneShotTurnGate
    {
        public bool ShotCommitted { get; private set; }

        public void BeginTurn() => ShotCommitted = false;

        public bool TryCommitShot()
        {
            if (ShotCommitted) return false;
            ShotCommitted = true;
            return true;
        }
    }
}
