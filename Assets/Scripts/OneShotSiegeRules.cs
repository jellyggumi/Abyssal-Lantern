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
