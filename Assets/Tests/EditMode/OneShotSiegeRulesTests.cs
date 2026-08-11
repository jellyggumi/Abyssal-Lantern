using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    [TestFixture]
    public sealed class OneShotSiegeRulesTests
    {
        [Test]
        public void ProjectileForTurn_IsAutomaticSymmetricAndNeverAnInstallation()
        {
            Assert.That(OneShotSiegeRules.ProjectileForTurn(0), Is.EqualTo(OneShotSiegeRules.Projectile.Knight));
            Assert.That(OneShotSiegeRules.ProjectileForTurn(1), Is.EqualTo(OneShotSiegeRules.Projectile.Knight));
            Assert.That(OneShotSiegeRules.ProjectileForTurn(2), Is.EqualTo(OneShotSiegeRules.Projectile.Archer));
            Assert.That(OneShotSiegeRules.ProjectileForTurn(4), Is.EqualTo(OneShotSiegeRules.Projectile.Barrel));
        }

        [Test]
        public void Velocity_ChangesAngleAndPowerIndependently()
        {
            Vector2 baseline = OneShotSiegeRules.Velocity(45f, 0.5f, 3f, 25f, 1f);
            Vector2 changedAngle = OneShotSiegeRules.Velocity(70f, 0.5f, 3f, 25f, 1f);
            Vector2 changedPower = OneShotSiegeRules.Velocity(45f, 0.8f, 3f, 25f, 1f);

            Assert.That(changedAngle.magnitude, Is.EqualTo(baseline.magnitude).Within(0.001f));
            Assert.That(changedAngle.normalized.y, Is.GreaterThan(baseline.normalized.y));
            Assert.That(changedPower.normalized.x, Is.EqualTo(baseline.normalized.x).Within(0.001f));
            Assert.That(changedPower.magnitude, Is.GreaterThan(baseline.magnitude));
        }

        [Test]
        public void TurnGate_AllowsExactlyOneShotUntilTheNextTurn()
        {
            var gate = new OneShotTurnGate();

            gate.BeginTurn();
            Assert.That(gate.TryCommitShot(), Is.True);
            Assert.That(gate.TryCommitShot(), Is.False);

            gate.BeginTurn();
            Assert.That(gate.TryCommitShot(), Is.True);
        }
    }
}
