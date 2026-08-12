using System.Collections;
using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Proves the one-shot cannon is reachable in the real scene, not merely present in the
    /// rules table. Every gate the player meets is exercised in the order they meet it:
    /// the siege has to be running, the battery has to be unlocked by turn and by breach,
    /// supply has to be paid, deploy mode has to be armed, and the placement has to consume
    /// the turn. A unit test on DeploymentRules can pass while any one of those is dead in
    /// the built game, which is exactly the gap this closes.
    /// </summary>
    public class OneShotCannonLiveSceneTests
    {
        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"expected private field {target.GetType().Name}.{field}");
            f.SetValue(target, value);
        }

        [UnityTest]
        [Timeout(180000)]
        public IEnumerator OneShotCannon_IsPlaceableInTheLiveSceneAndSpendsTheTurn()
        {
            SceneManager.LoadScene("SampleScene");
            yield return new WaitForSecondsRealtime(2f);

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "the scene must bring up a GameManager");
            gm.BeginSiege();
            yield return null;

            var deployment = DeploymentController.Instance;
            Assert.IsNotNull(deployment,
                "GameManager must provide a DeploymentController — without it the cannon is unreachable");

            Assert.IsTrue(gm.EnforcesOneShotTurns, "precondition: this is the one-shot loop");

            // Stand the match where a player who has been breaching walls would be: past the
            // battery's unlock turn, with the required breaches earned and supply saved.
            SetPrivate(gm, "turnCount", DeploymentRules.UnlockTurn(DeployCard.Cannon) + 2);
            SetPrivate(gm, "isPlayerTurn", true);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.PlayerTurn;
            for (int i = 0; i < DeploymentRules.CannonBreachRequirement; i++)
            {
                // Credited by whose wall fell: the enemy's, so it unlocks the PLAYER's battery.
                deployment.CreditBlockDestroyed(blockBelongedToPlayer: false);
            }
            SetPrivate(deployment, "<PlayerSupply>k__BackingField", DeploymentRules.CostOf(DeployCard.Cannon) + 4f);

            Assert.AreEqual(DeploymentRules.CannonBreachRequirement, deployment.PlayerBreaches,
                "breaching enemy walls must credit the player's tally, or the battery never unlocks");

            // One frame of the live Update: this is what selects the battery and shows its HUD
            // in the one-shot loop.
            yield return null;
            Assert.AreEqual(DeployCard.Cannon, deployment.SelectedCard,
                "the one-shot loop must offer the battery as the turn's alternative action");

            int cannonsBefore = CountPlayerCannons();

            // A player picks a spot they can see is clear; the test must do the same rather
            // than hard-code one square and call a busy tile a broken feature.
            // Mirror the game's own criterion — a legal zone with no live OPPOSING body on it.
            // An earlier revision of this probe demanded zero colliders of any kind, which no
            // square on a dressed battlefield satisfies, and reported a healthy feature broken.
            Vector2? clear = null;
            for (float x = -1.5f; x >= -11.5f && clear == null; x -= 1f)
            {
                var candidate = new Vector2(x, 0.5f);
                if (!DeploymentRules.InDeployZone(candidate, true)) continue;
                if (!HasOpposingBody(candidate, deployerIsPlayer: true)) clear = candidate;
            }
            Assert.IsTrue(clear.HasValue,
                $"the player's whole half offered no clear battery site. Bodies on the field: {DescribeField()}");

            var site = clear.Value;
            var reason = deployment.TryDeploy(DeployCard.Cannon, site, true);

            Assert.AreEqual(DeployBlockReason.None, reason,
                $"a fully-qualified battery placement at {site} was refused: {reason}. Overlaps: {DescribeOverlaps(site)}");
            Assert.AreEqual(cannonsBefore + 1, CountPlayerCannons(),
                "a successful placement must actually put a cannon on the field");
            Assert.IsTrue(gm.IsResolvingTurn,
                "the emplacement is the turn's action, so it must resolve the turn like a volley");
            Assert.IsFalse(gm.TryCommitTurnShot(),
                "a turn spent on artillery must not also be able to fire");

            yield return null;
        }

        /// <summary>Names every body the placement check can see at a site, so a Zone refusal
        /// says WHICH object refused it instead of leaving the reader to guess.</summary>
        private static string DescribeOverlaps(Vector2 site)
        {
            var parts = new System.Text.StringBuilder();
            foreach (var hit in Physics2D.OverlapCircleAll(site, DeploymentRules.EnemyOverlapRadius))
            {
                if (hit == null) continue;
                var unit = hit.GetComponent<UnitController>();
                parts.Append($"[{hit.name}");
                if (unit != null) parts.Append($" unit={unit.unitType} isPlayerUnit={unit.isPlayerUnit} state={unit.CurrentState}");
                parts.Append("] ");
            }
            return parts.Length == 0 ? "(none)" : parts.ToString();
        }

        /// <summary>The same test DeploymentController applies before a placement.</summary>
        private static bool HasOpposingBody(Vector2 site, bool deployerIsPlayer)
        {
            foreach (var hit in Physics2D.OverlapCircleAll(site, DeploymentRules.EnemyOverlapRadius))
            {
                var unit = hit != null ? hit.GetComponent<UnitController>() : null;
                if (unit != null && unit.isPlayerUnit != deployerIsPlayer && unit.CurrentState != UnitState.Dead)
                    return true;
            }
            return false;
        }

        /// <summary>Every live body and where it stands — the evidence for whether the field
        /// is legitimately crowded or something is spawning where it should not.</summary>
        private static string DescribeField()
        {
            var parts = new System.Text.StringBuilder();
            foreach (var unit in Object.FindObjectsOfType<UnitController>())
            {
                if (unit == null || unit.CurrentState == UnitState.Dead) continue;
                parts.Append($"[{unit.name} {unit.unitType} player={unit.isPlayerUnit} at x={unit.transform.position.x:F1},y={unit.transform.position.y:F1}] ");
            }
            return parts.Length == 0 ? "(none)" : parts.ToString();
        }

        private static int CountPlayerCannons()
        {
            int count = 0;
            foreach (var unit in Object.FindObjectsOfType<UnitController>())
            {
                if (unit != null && unit.isPlayerUnit && unit.unitType == UnitType.Cannon) count++;
            }
            return count;
        }
    }
}
