using System.Collections;
using System.Text;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins the one-shot loop's core-damage contract in the REAL scene: while nobody fires,
    /// no core loses a point of durability.
    ///
    /// Why this exists (defect, 2026-08-12 live QA ×2): during the player's very first
    /// aim, KEEP CORE fell 150→70 with rising HIT combo toasts — before any enemy volley.
    /// The shield masks another 50 silently (it raises at ≤50% and absorbs before health),
    /// so true incoming was ~130. The one-shot balance model (SiegeBalanceSettings,
    /// MatchLengthModel) prices EVERY point of core damage in shots; a source that drains
    /// cores during idle time invalidates the 5-minute pacing gate those models enforce.
    ///
    /// The probe is attribution-first: when durability moves, the failure message names
    /// every body near the damaged core and every unit mid-Attack on the field, so the
    /// next failure identifies its own culprit instead of re-opening a screenshot hunt.
    /// </summary>
    public class UntouchedCoreIntegrityTests
    {
        private const float ObservationSeconds = 25f;

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator IdleSiege_NoVolley_LeavesBothCoreDurabilitiesUntouched()
        {
            SceneManager.LoadScene("SampleScene");
            yield return new WaitForSecondsRealtime(2f);

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "the scene must bring up a GameManager");
            Assert.IsTrue(gm.EnforcesOneShotTurns, "precondition: this is the one-shot loop");
            gm.BeginSiege();
            yield return null;

            var playerCore = FindCore(playerCore: true);
            var enemyCore = FindCore(playerCore: false);
            Assert.IsNotNull(playerCore, "player core must exist after BeginSiege");
            Assert.IsNotNull(enemyCore, "enemy core must exist after BeginSiege");

            float playerBefore = TotalDurability(playerCore);
            float enemyBefore = TotalDurability(enemyCore);

            // Idle through several turn boundaries at real speed. No simulated pointer, no
            // Space, no deploys: every turn the player forfeits and the AI answers with its
            // own volley — which this probe must tolerate? No: the AI DOES fire. So freeze
            // turn handoff instead by holding the clock — the same hook the first-play coach
            // uses — keeping the match legally inside the player's un-acted turn while the
            // field units (the suspects) live out their full autonomous behavior.
            float elapsed = 0f;
            while (elapsed < ObservationSeconds)
            {
                gm.HoldTurnTimerForCoaching(30f);
                if (TotalDurability(playerCore) < playerBefore - 0.01f ||
                    TotalDurability(enemyCore) < enemyBefore - 0.01f)
                {
                    break; // damage observed — fail fast with a live field snapshot
                }
                elapsed += Time.deltaTime;
                yield return null;
            }

            float playerAfter = TotalDurability(playerCore);
            float enemyAfter = TotalDurability(enemyCore);

            Assert.That(playerAfter, Is.EqualTo(playerBefore).Within(0.01f),
                $"PLAYER core durability moved {playerBefore}→{playerAfter} with no volley fired. " +
                $"Field at failure: {DescribeSuspects(playerCore)}");
            Assert.That(enemyAfter, Is.EqualTo(enemyBefore).Within(0.01f),
                $"ENEMY core durability moved {enemyBefore}→{enemyAfter} with no volley fired. " +
                $"Field at failure: {DescribeSuspects(enemyCore)}");
        }

        /// <summary>
        /// Diagnostic twin of the live-QA reproduction: fire ONE player volley with the
        /// exact aim the QA sessions used (full power, ~48°), then trace both cores through
        /// the resolve and the AI's answer. Asserts the one contract the balance model
        /// depends on: the PLAYER core loses durability only after the ENEMY's volley is
        /// actually in the air — never during the player's own resolve window.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator PlayerVolleyResolve_DoesNotDamageThePlayersOwnCore()
        {
            SceneManager.LoadScene("SampleScene");
            yield return new WaitForSecondsRealtime(2f);

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "the scene must bring up a GameManager");
            gm.BeginSiege();
            yield return null;

            var launchManager = Object.FindObjectOfType<LaunchManager>();
            Assert.IsNotNull(launchManager, "the scene must bring up a LaunchManager");
            var playerCore = FindCore(playerCore: true);
            Assert.IsNotNull(playerCore, "player core must exist after BeginSiege");

            float before = TotalDurability(playerCore);

            // The QA aim: full draw, 48° — the shot that preceded both live drains.
            launchManager.SetAimAngle(48f);
            launchManager.SetAimPower(1f);
            launchManager.SimulateLaunch(launchManager.GetSeparatedAimVelocity());
            Assert.IsTrue(gm.IsResolvingTurn, "the volley must be resolving after launch");

            // Walk the player's whole resolve window frame by frame: while it is still the
            // player's turn, the player's own core must not lose a point.
            float guard = 0f;
            while (gm.IsPlayerTurn && guard < 30f)
            {
                float now = TotalDurability(playerCore);
                Assert.That(now, Is.EqualTo(before).Within(0.01f),
                    $"PLAYER core durability moved {before}→{now} during the PLAYER's own resolve " +
                    $"window (state={gm.currentState}, resolving={gm.IsResolvingTurn}, " +
                    $"turn={gm.TurnCount}). Field: {DescribeSuspects(playerCore)}");
                guard += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(gm.IsPlayerTurn, "the volley must eventually hand the turn to the AI");
        }

        private static CastleCoreGimmick FindCore(bool playerCore)
        {
            for (int i = 0; i < DestructibleBlock.Active.Count; i++)
            {
                if (DestructibleBlock.Active[i] is CastleCoreGimmick core && core.isPlayerCore == playerCore)
                {
                    return core;
                }
            }
            return null;
        }

        /// <summary>Health plus live shield: the shield absorbs silently, so raw HP alone
        /// understates incoming damage — exactly how the live defect hid 50 of ~130.
        /// The shield field is private (nothing in the game reads it back); reflection here
        /// keeps it that way instead of widening runtime API for a probe.</summary>
        private static float TotalDurability(CastleCoreGimmick core)
        {
            var field = typeof(CastleCoreGimmick).GetField(
                "shieldHP",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, "expected private field CastleCoreGimmick.shieldHP");
            return core.currentHP + Mathf.Max(0f, (float)field.GetValue(core));
        }

        private static string DescribeSuspects(CastleCoreGimmick core)
        {
            var sb = new StringBuilder();
            Vector2 corePos = core.transform.position;
            sb.Append($"core at={corePos:F1} | ");

            sb.Append("launched bodies: ");
            for (int i = 0; i < UnitController.Active.Count; i++)
            {
                var u = UnitController.Active[i];
                if (u != null && u.CurrentState == UnitState.Launched)
                {
                    sb.Append($"[{u.unitType} isPlayer={u.isPlayerUnit} at={u.transform.position:F1}] ");
                }
            }

            sb.Append("near-core bodies: ");
            foreach (var hit in Physics2D.OverlapCircleAll(corePos, 3.5f))
            {
                var unit = hit != null ? hit.GetComponent<UnitController>() : null;
                if (unit != null)
                {
                    sb.Append($"[{unit.unitType} isPlayer={unit.isPlayerUnit} state={unit.CurrentState} " +
                              $"at={unit.transform.position:F1}] ");
                }
                else if (hit.GetComponent<DestructibleBlock>() is DestructibleBlock block && block.IsFalling)
                {
                    sb.Append($"[falling block {block.name} at={block.transform.position:F1}] ");
                }
            }

            sb.Append("| attacking units anywhere: ");
            for (int i = 0; i < UnitController.Active.Count; i++)
            {
                var u = UnitController.Active[i];
                if (u != null && u.CurrentState == UnitState.Attacking)
                {
                    sb.Append($"[{u.unitType} isPlayer={u.isPlayerUnit} at={u.transform.position:F1}] ");
                }
            }
            return sb.ToString();
        }
    }
}
