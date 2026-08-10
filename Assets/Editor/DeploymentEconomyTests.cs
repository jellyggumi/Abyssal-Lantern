using System;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// EditMode pins for the deployment economy contract (design/deployment-economy.md):
    /// supply curve, per-card 생성조건, cap groups, Evaluate precedence, deploy-zone
    /// geometry, and the Cannon installation's reload/ballistic contract.
    ///
    /// Pure rule classes only — no scene, no GameObject, no MonoBehaviour.
    /// </summary>
    public class DeploymentEconomyTests
    {
        private const float Tol = 1e-4f;
        /// <summary>Float32 sqrt/divide accumulation slack for the ballistic oracles.</summary>
        private const float BallisticTol = 5e-3f;
        private const float Gravity = 9.81f;

        /// <summary>Midfield on the player's half: inside the band, clear of both rings.</summary>
        private static readonly Vector2 LegalPlayerSpot = new Vector2(-6f, 1f);
        /// <summary>Mirror of the above on the enemy's half.</summary>
        private static readonly Vector2 LegalEnemySpot = new Vector2(6f, 1f);

        private static readonly DeployCard[] AllCards =
            (DeployCard[])Enum.GetValues(typeof(DeployCard));

        // A prior PlayMode session can leave the ring statics on another stage's apron.
        // Pin Stage1 so this file cannot be polluted by, or pollute, that state.
        private float _ringPlayerX;
        private float _ringEnemyX;

        [SetUp]
        public void PinStage1LaunchRings()
        {
            _ringPlayerX = LaunchRingRules.PlayerRingX;
            _ringEnemyX = LaunchRingRules.EnemyRingX;
            LaunchRingRules.PlayerRingX = -14.5f;
            LaunchRingRules.EnemyRingX = 14.5f;
        }

        [TearDown]
        public void RestoreLaunchRings()
        {
            LaunchRingRules.PlayerRingX = _ringPlayerX;
            LaunchRingRules.EnemyRingX = _ringEnemyX;
        }

        // ---- §4 Roster table: cost / cooldown / unlock / cap ----

        [Test]
        public void RosterTable_MatchesTheDesignSheet_ForEveryCard()
        {
            // The authoritative 생성조건 table. Any silent drift here is a balance regression.
            var rows = new[]
            {
                new { card = DeployCard.Knight, cost = 5f,  cooldown = 2.5f, unlock = 0, group = DeployCapGroup.Body,    cap = 6 },
                new { card = DeployCard.Archer, cost = 6f,  cooldown = 3.5f, unlock = 1, group = DeployCapGroup.Body,    cap = 6 },
                new { card = DeployCard.Cannon, cost = 12f, cooldown = 12f,  unlock = 3, group = DeployCapGroup.Battery, cap = 2 },
                new { card = DeployCard.Barrel, cost = 4f,  cooldown = 5f,   unlock = 2, group = DeployCapGroup.Hazard,  cap = 3 },
            };

            Assert.AreEqual(AllCards.Length, rows.Length,
                "every roster card must carry an explicit 생성조건 — a new card may not inherit one silently");

            foreach (var row in rows)
            {
                Assert.AreEqual(row.cost, DeploymentRules.CostOf(row.card), Tol,
                    $"{row.card}: supply price is the pacing lever — it may not drift off the design sheet");
                Assert.AreEqual(row.cooldown, DeploymentRules.CooldownOf(row.card), Tol,
                    $"{row.card}: reuse delay is the per-card rate limit — it may not drift off the design sheet");
                Assert.AreEqual(row.unlock, DeploymentRules.UnlockTurn(row.card),
                    $"{row.card}: unlock turn sets the teaching order melee → range → hazard → structure");
                Assert.AreEqual(row.group, DeploymentRules.GroupOf(row.card),
                    $"{row.card}: cap group decides which ceiling the card competes against");
                Assert.AreEqual(row.cap, DeploymentRules.CapFor(row.card),
                    $"{row.card}: field ceiling caps how much board one card can own");
            }
        }

        [Test]
        public void UnlockOrder_TeachesMelee_ThenRange_ThenHazard_ThenStructure()
        {
            // Relational pin: survives a global re-tune, still catches a reordered curriculum.
            Assert.Less(DeploymentRules.UnlockTurn(DeployCard.Knight), DeploymentRules.UnlockTurn(DeployCard.Archer),
                "melee must be taught before range");
            Assert.Less(DeploymentRules.UnlockTurn(DeployCard.Archer), DeploymentRules.UnlockTurn(DeployCard.Barrel),
                "range must be taught before the hazard");
            Assert.Less(DeploymentRules.UnlockTurn(DeployCard.Barrel), DeploymentRules.UnlockTurn(DeployCard.Cannon),
                "the structure is the last thing taught — it is the most board-changing card");
            Assert.AreEqual(0, DeploymentRules.UnlockTurn(DeployCard.Knight),
                "one card must be legal on the opening beat or turn 0 has no deploy verb");
        }

        [Test]
        public void CannonIsThePremiumPurchase_BarrelTheCheapest()
        {
            foreach (var card in AllCards)
            {
                if (card == DeployCard.Cannon) continue;
                Assert.Less(DeploymentRules.CostOf(card), DeploymentRules.CostOf(DeployCard.Cannon),
                    $"{card} must stay cheaper than the Cannon — structure is the premium buy");
                Assert.Less(DeploymentRules.CooldownOf(card), DeploymentRules.CooldownOf(DeployCard.Cannon),
                    $"{card} must recharge faster than the Cannon — batteries are rate-limited hardest");
                Assert.GreaterOrEqual(DeploymentRules.CostOf(card), DeploymentRules.CostOf(DeployCard.Barrel),
                    $"{card} must not undercut the Barrel — the hazard is the entry-price card");
            }
        }

        // ---- §4 Cap groups: Knight and Archer share one ceiling ----

        [Test]
        public void KnightAndArcher_ShareOneBodyCap_CannonAndBarrelDoNot()
        {
            Assert.AreEqual(DeploymentRules.GroupOf(DeployCard.Knight), DeploymentRules.GroupOf(DeployCard.Archer),
                "Knight and Archer must share one body cap so deploy thickens a line instead of flooding the map");
            Assert.AreEqual(DeployCapGroup.Body, DeploymentRules.GroupOf(DeployCard.Knight),
                "soldiers belong to the body ceiling");
            Assert.AreNotEqual(DeploymentRules.GroupOf(DeployCard.Cannon), DeploymentRules.GroupOf(DeployCard.Knight),
                "the battery must not eat the body ceiling — installations are budgeted separately");
            Assert.AreNotEqual(DeploymentRules.GroupOf(DeployCard.Barrel), DeploymentRules.GroupOf(DeployCard.Knight),
                "the hazard must not eat the body ceiling — a keg is not a soldier");
            Assert.AreNotEqual(DeploymentRules.GroupOf(DeployCard.Barrel), DeploymentRules.GroupOf(DeployCard.Cannon),
                "hazard and battery are independent budgets");
        }

        [Test]
        public void CapFor_GivesEachGroupItsOwnCeiling()
        {
            Assert.AreEqual(6, DeploymentRules.CapFor(DeployCapGroup.Body),
                "the body ceiling is what stops the deploy verb from flooding the map");
            Assert.AreEqual(2, DeploymentRules.CapFor(DeployCapGroup.Battery),
                "two batteries is the ceiling that keeps turrets counterable by one volley");
            Assert.AreEqual(3, DeploymentRules.CapFor(DeployCapGroup.Hazard),
                "the hazard ceiling keeps the field from becoming a minefield");
            Assert.AreEqual(DeploymentRules.CapFor(DeploymentRules.GroupOf(DeployCard.Cannon)),
                DeploymentRules.CapFor(DeployCard.Cannon),
                "the per-card cap must resolve through the card's own group, never a fixed number");
        }

        [Test]
        public void FullBodyCap_BlocksASoldier_ButNeverABattery()
        {
            // Same board, same instant: six live bodies, zero live batteries.
            var soldier = DeploymentRules.Evaluate(
                DeployCard.Knight, turnCount: 3, aliveInGroup: 6,
                cooldownRemaining: 0f, supply: 24f, position: LegalPlayerSpot, deployerIsPlayer: true);
            var battery = DeploymentRules.Evaluate(
                DeployCard.Cannon, turnCount: 3, aliveInGroup: 0,
                cooldownRemaining: 0f, supply: 24f, position: LegalPlayerSpot, deployerIsPlayer: true);

            Assert.AreEqual(DeployBlockReason.FieldCap, soldier,
                "a full body ceiling must refuse another soldier");
            Assert.AreEqual(DeployBlockReason.None, battery,
                "a full body ceiling must never block a battery — the cap groups are independent budgets");
        }

        [Test]
        public void FieldCap_BitesAtTheCeiling_NotBeforeIt()
        {
            int cap = DeploymentRules.CapFor(DeployCard.Knight);

            Assert.AreEqual(DeployBlockReason.None, DeploymentRules.Evaluate(
                DeployCard.Knight, 3, cap - 1, 0f, 24f, LegalPlayerSpot, true),
                "one slot below the ceiling must still deploy — the cap is a ceiling, not a fence one short of it");
            Assert.AreEqual(DeployBlockReason.FieldCap, DeploymentRules.Evaluate(
                DeployCard.Knight, 3, cap, 0f, 24f, LegalPlayerSpot, true),
                "the ceiling itself must refuse — cap counts occupied slots, not spare ones");
        }

        // ---- §4 Evaluate precedence: most-permanent-first ----

        [Test]
        public void Evaluate_NamesTheMostPermanentBlocker_NotTheFirstOneTripped()
        {
            // A descending ladder: every rung fails EVERY condition at or below it. If the
            // implementation reordered its checks, at least one rung reports the wrong blocker.
            // Archer: unlock 1, cost 6, body cap 6.
            var offHalf = LegalEnemySpot; // illegal for a player deploy

            Assert.AreEqual(DeployBlockReason.Locked, DeploymentRules.Evaluate(
                DeployCard.Archer, turnCount: 0, aliveInGroup: 6,
                cooldownRemaining: 2f, supply: 0f, position: offHalf, deployerIsPlayer: true),
                "a locked card outranks every other blocker — waiting turns is the only fix the player has");

            Assert.AreEqual(DeployBlockReason.FieldCap, DeploymentRules.Evaluate(
                DeployCard.Archer, turnCount: 1, aliveInGroup: 6,
                cooldownRemaining: 2f, supply: 0f, position: offHalf, deployerIsPlayer: true),
                "a full field outranks cooldown, supply and zone — it costs a unit's life to clear");

            Assert.AreEqual(DeployBlockReason.Cooldown, DeploymentRules.Evaluate(
                DeployCard.Archer, turnCount: 1, aliveInGroup: 0,
                cooldownRemaining: 2f, supply: 0f, position: offHalf, deployerIsPlayer: true),
                "cooldown outranks supply and zone — the card itself is unavailable, so the click cannot help");

            Assert.AreEqual(DeployBlockReason.Supply, DeploymentRules.Evaluate(
                DeployCard.Archer, turnCount: 1, aliveInGroup: 0,
                cooldownRemaining: 0f, supply: 0f, position: offHalf, deployerIsPlayer: true),
                "supply outranks zone — moving the click cannot pay for the card");

            Assert.AreEqual(DeployBlockReason.Zone, DeploymentRules.Evaluate(
                DeployCard.Archer, turnCount: 1, aliveInGroup: 0,
                cooldownRemaining: 0f, supply: 6f, position: offHalf, deployerIsPlayer: true),
                "zone is the last blocker reported — it is the one the player fixes by clicking elsewhere");

            Assert.AreEqual(DeployBlockReason.None, DeploymentRules.Evaluate(
                DeployCard.Archer, turnCount: 1, aliveInGroup: 0,
                cooldownRemaining: 0f, supply: 6f, position: LegalPlayerSpot, deployerIsPlayer: true),
                "all five conditions satisfied must deploy — the gate may not invent a sixth refusal");
        }

        [Test]
        public void Evaluate_EachAdjacentPair_ResolvesToTheMorePermanentBlocker()
        {
            // Exactly two conditions fail per case, so a check that merely counts failures
            // (or that fires in declaration order) cannot pass all four.
            Assert.AreEqual(DeployBlockReason.Locked, DeploymentRules.Evaluate(
                DeployCard.Archer, 0, 6, 0f, 24f, LegalPlayerSpot, true),
                "locked beats a full field — the card is not even in the player's hand yet");

            Assert.AreEqual(DeployBlockReason.FieldCap, DeploymentRules.Evaluate(
                DeployCard.Archer, 1, 6, 2f, 24f, LegalPlayerSpot, true),
                "a full field beats cooldown — the ceiling outlives the timer");

            Assert.AreEqual(DeployBlockReason.Cooldown, DeploymentRules.Evaluate(
                DeployCard.Archer, 1, 0, 2f, 0f, LegalPlayerSpot, true),
                "cooldown beats missing supply — supply regenerates while the card is still barred");

            Assert.AreEqual(DeployBlockReason.Supply, DeploymentRules.Evaluate(
                DeployCard.Archer, 1, 0, 0f, 0f, LegalEnemySpot, true),
                "missing supply beats a bad click — the HUD must not send the player hunting for a legal tile");
        }

        [Test]
        public void Evaluate_CooldownBitesWhileRunning_ClearsAtZero()
        {
            Assert.AreEqual(DeployBlockReason.Cooldown, DeploymentRules.Evaluate(
                DeployCard.Knight, 3, 0, 0.01f, 24f, LegalPlayerSpot, true),
                "any remaining cooldown must bar the card — a nearly-expired timer is still a timer");
            Assert.AreEqual(DeployBlockReason.None, DeploymentRules.Evaluate(
                DeployCard.Knight, 3, 0, 0f, 24f, LegalPlayerSpot, true),
                "an expired cooldown must free the card immediately");
        }

        [Test]
        public void Evaluate_SupplyGate_AllowsExactAffordability()
        {
            float cost = DeploymentRules.CostOf(DeployCard.Cannon);

            Assert.AreEqual(DeployBlockReason.None, DeploymentRules.Evaluate(
                DeployCard.Cannon, 3, 0, 0f, cost, LegalPlayerSpot, true),
                "supply exactly equal to the price must buy the card — the last coin still spends");
            Assert.AreEqual(DeployBlockReason.Supply, DeploymentRules.Evaluate(
                DeployCard.Cannon, 3, 0, 0f, cost - 0.5f, LegalPlayerSpot, true),
                "short of the price must refuse — the gate may not extend credit");
        }

        [Test]
        public void Evaluate_HoldsForBothSides_NoHiddenAiDiscount()
        {
            foreach (var card in AllCards)
            {
                int turn = DeploymentRules.UnlockTurn(card);
                float cost = DeploymentRules.CostOf(card);
                var player = DeploymentRules.Evaluate(card, turn, 0, 0f, cost, LegalPlayerSpot, true);
                var enemy = DeploymentRules.Evaluate(card, turn, 0, 0f, cost, LegalEnemySpot, false);
                Assert.AreEqual(player, enemy,
                    $"{card}: the AI must clear the same 생성조건 as the player — difficulty comes from aim, never a cheaper economy");
            }
        }

        [Test]
        public void ReasonText_SpeaksForEveryRefusal_AndStaysSilentOnSuccess()
        {
            Assert.IsEmpty(DeploymentRules.ReasonText(DeployBlockReason.None, DeployCard.Knight, 0),
                "a successful deploy must print nothing — the HUD only speaks to explain a refusal");

            foreach (DeployBlockReason reason in Enum.GetValues(typeof(DeployBlockReason)))
            {
                if (reason == DeployBlockReason.None) continue;
                Assert.IsNotEmpty(DeploymentRules.ReasonText(reason, DeployCard.Cannon, 1),
                    $"{reason} must name itself to the player — a refused deploy is never a silent no-op");
            }
        }

        // ---- §5 Deploy zone geometry ----

        [Test]
        public void DeployZone_AcceptsMidfield_OnEachSidesOwnHalf()
        {
            Assert.IsTrue(DeploymentRules.InDeployZone(LegalPlayerSpot, deployerIsPlayer: true),
                "the player must be able to reinforce their own midfield — the zone cannot be empty in practice");
            Assert.IsTrue(DeploymentRules.InDeployZone(LegalEnemySpot, deployerIsPlayer: false),
                "the enemy half must be usable by the AI on the same terms");
        }

        [Test]
        public void DeployZone_RejectsTheCenterLine_ForBothSides()
        {
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(0f, 2f), true),
                "dead center belongs to neither side, so no deploy may claim it");
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(0f, 2f), false),
                "dead center is refused symmetrically");
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(-0.4f, 2f), true),
                "the no-man's band applies even on the player's own side of the line");
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(0.4f, 2f), false),
                "the no-man's band applies even on the enemy's own side of the line");
        }

        [Test]
        public void DeployZone_RejectsTheWrongHalf_ForEachDeployer()
        {
            Assert.IsFalse(DeploymentRules.InDeployZone(LegalEnemySpot, deployerIsPlayer: true),
                "you reinforce your own side — a deploy may not teleport onto the enemy's doorstep");
            Assert.IsFalse(DeploymentRules.InDeployZone(LegalPlayerSpot, deployerIsPlayer: false),
                "the same half-field restriction binds the AI");
        }

        [Test]
        public void DeployZone_InnerBound_IsInclusiveAtMinAbsX()
        {
            Assert.IsTrue(DeploymentRules.InDeployZone(new Vector2(-DeploymentRules.MinAbsX, 2f), true),
                "the inner bound is INCLUSIVE — a deploy exactly on the band edge is legal");
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(-DeploymentRules.MinAbsX + 0.01f, 2f), true),
                "one step inside the inner bound is refused — the center line stays clear");
        }

        [Test]
        public void DeployZone_OuterBound_IsInclusiveAtMaxAbsX()
        {
            // y = 5 keeps both probes well clear of the launch ring, so only MaxAbsX can decide.
            Assert.IsTrue(DeploymentRules.InDeployZone(new Vector2(-DeploymentRules.MaxAbsX, 5f), true),
                "the outer bound is INCLUSIVE — the keeps' band edge is still deployable");
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(-DeploymentRules.MaxAbsX - 0.1f, 5f), true),
                "past the outer bound is refused — deploys stay inside the keeps' band");
        }

        [Test]
        public void DeployZone_VerticalBand_IsInclusiveAtBothEnds()
        {
            Assert.IsTrue(DeploymentRules.InDeployZone(new Vector2(-6f, DeploymentRules.MinY), true),
                "the floor is INCLUSIVE — ground level is the normal place to put a body");
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(-6f, DeploymentRules.MinY - 0.01f), true),
                "below the floor is refused — nothing deploys underground");
            Assert.IsTrue(DeploymentRules.InDeployZone(new Vector2(-6f, DeploymentRules.MaxY), true),
                "the ceiling is INCLUSIVE — the top of the band is still legal");
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(-6f, DeploymentRules.MaxY + 0.01f), true),
                "above the ceiling is refused — nothing deploys into open sky");
        }

        [Test]
        public void DeployZone_ExcludesTheLaunchRing_OnBothSides()
        {
            var playerMuzzle = new Vector2(-12f, LaunchRingRules.RingY);
            var enemyMuzzle = new Vector2(12f, LaunchRingRules.RingY);

            Assert.IsTrue(LaunchRingRules.IsInsideRing(playerMuzzle), "fixture guard: probe must sit inside the ring");
            Assert.IsTrue(LaunchRingRules.IsInsideRing(enemyMuzzle), "fixture guard: probe must sit inside the ring");

            Assert.IsFalse(DeploymentRules.InDeployZone(playerMuzzle, true),
                "a body in your own muzzle blocks every volley you fire, so the ring is never deployable");
            Assert.IsFalse(DeploymentRules.InDeployZone(enemyMuzzle, false),
                "the AI's muzzle is protected by the same exclusion");

            // Same height, same half, same band — only the ring distance differs.
            Assert.IsTrue(DeploymentRules.InDeployZone(new Vector2(-6f, LaunchRingRules.RingY), true),
                "clear of the ring at the same height must deploy — the ring, not the row, is the blocker");
        }

        [Test]
        public void DeployZone_EffectivePlayerBand_EndsWhereTheRingBegins()
        {
            // §5 documents the composed band as roughly x ∈ [-11, -0.5].
            Assert.IsTrue(DeploymentRules.InDeployZone(new Vector2(-10.9f, LaunchRingRules.RingY), true),
                "just short of the ring must remain deployable — the usable band reaches the muzzle's edge");
            Assert.IsFalse(DeploymentRules.InDeployZone(new Vector2(-11.1f, LaunchRingRules.RingY), true),
                "just inside the ring must be refused — the band ends where the muzzle starts");
        }

        // ---- §3 Supply curve ----

        [Test]
        public void Regen_AccruesOverTime_AndParksAtTheCap()
        {
            Assert.AreEqual(11.5f, SupplyRules.Regen(8f, 5f), Tol,
                "supply must accrue in real time — the deploy verb is paced by the clock, not by turns");

            float atCap = SupplyRules.Regen(SupplyRules.MaxSupply, 10f);
            Assert.AreEqual(SupplyRules.MaxSupply, atCap, Tol,
                "regen must park at the ceiling — hoarding may not fund an alpha strike");
            Assert.LessOrEqual(atCap, SupplyRules.MaxSupply,
                "regen must never overshoot the ceiling");
            Assert.AreEqual(SupplyRules.MaxSupply, SupplyRules.Regen(SupplyRules.MaxSupply - 0.1f, 100f), Tol,
                "a long tick lands exactly on the ceiling instead of running past it");
        }

        [Test]
        public void Regen_IgnoresNonPositiveTicks()
        {
            Assert.AreEqual(8f, SupplyRules.Regen(8f, 0f), Tol,
                "a paused frame must not pay supply — regen is time, not calls");
            Assert.AreEqual(8f, SupplyRules.Regen(8f, -3f), Tol,
                "a negative tick must never drain the player — a clock glitch cannot cost a deploy");
        }

        [Test]
        public void Credit_AddsBonuses_ButCannotOverfill()
        {
            Assert.AreEqual(10f, SupplyRules.Credit(8f, SupplyRules.KillBonus), Tol,
                "a kill must pay the killer — the launch verb funds the deploy verb");
            Assert.AreEqual(8.5f, SupplyRules.Credit(8f, SupplyRules.BlockBonus), Tol,
                "breaking a block must pay too — collapsing a wall funds the body that walks the gap");
            Assert.AreEqual(SupplyRules.MaxSupply, SupplyRules.Credit(SupplyRules.MaxSupply - 0.5f, 10f), Tol,
                "event bonuses obey the same ceiling as regen — no path may overfill the bank");
        }

        [Test]
        public void SupplySources_AreAdditive_AcrossRegenAndEvents()
        {
            float supply = 8f;
            supply = SupplyRules.Regen(supply, 5f);                  // +3.5
            supply = SupplyRules.Credit(supply, SupplyRules.KillBonus);  // +2
            supply = SupplyRules.Credit(supply, SupplyRules.BlockBonus); // +0.5
            Assert.AreEqual(14f, supply, Tol,
                "regen and event bonuses must stack — playing well has to accelerate the economy");
        }

        [Test]
        public void Clamp_FloorsAtZero_AndCeilingsAtMax()
        {
            Assert.AreEqual(0f, SupplyRules.Clamp(-5f), Tol,
                "supply can never go negative — a debt would silently bar every future deploy");
            Assert.AreEqual(SupplyRules.MaxSupply, SupplyRules.Clamp(999f), Tol,
                "supply can never exceed the ceiling regardless of how it got there");
        }

        [Test]
        public void TrySpend_ChargesExactly_WhenAffordable()
        {
            float remaining;
            Assert.IsTrue(SupplyRules.TrySpend(10f, 4f, out remaining),
                "an affordable card must be purchasable");
            Assert.AreEqual(6f, remaining, Tol,
                "a purchase must debit exactly the card's price — no tax, no rebate");
        }

        [Test]
        public void TrySpend_SucceedsAtExactAffordability()
        {
            float remaining;
            Assert.IsTrue(SupplyRules.TrySpend(6f, 6f, out remaining),
                "spending your last coin must succeed — an off-by-epsilon here silently eats a legal deploy");
            Assert.AreEqual(0f, remaining, Tol,
                "an exact purchase must leave an empty bank, never a negative one");

            Assert.IsTrue(SupplyRules.TrySpend(5.99995f, 6f, out remaining),
                "float drift under the price must still buy — accumulated regen error may not cost a deploy");
        }

        [Test]
        public void TrySpend_LeavesTheBankUntouched_WhenUnaffordable()
        {
            float remaining;
            Assert.IsFalse(SupplyRules.TrySpend(3f, 12f, out remaining),
                "an unaffordable card must be refused");
            Assert.AreEqual(3f, remaining, Tol,
                "a refused purchase must not partially spend — the player keeps every coin");

            Assert.IsFalse(SupplyRules.TrySpend(11.5f, 12f, out remaining),
                "just short of the price is still a refusal");
            Assert.AreEqual(11.5f, remaining, Tol,
                "a near miss must not skim the bank on the way out");
        }

        [Test]
        public void TrySpend_RefusesNegativePrices()
        {
            float remaining;
            Assert.IsFalse(SupplyRules.TrySpend(5f, -10f, out remaining),
                "a negative price must be refused — spending may never mint supply");
            Assert.AreEqual(5f, remaining, Tol,
                "a refused negative price leaves the bank exactly as it was");
        }

        [Test]
        public void OpeningSupply_BuysAnImmediateBody_ButNotABattery()
        {
            float remaining;
            Assert.IsTrue(SupplyRules.TrySpend(SupplyRules.StartSupply, DeploymentRules.CostOf(DeployCard.Knight), out remaining),
                "the opening bank must fund a deploy on the first beat so the mechanic teaches itself");
            Assert.AreEqual(DeployBlockReason.None, DeploymentRules.Evaluate(
                DeployCard.Knight, 0, 0, 0f, SupplyRules.StartSupply, LegalPlayerSpot, true),
                "turn 0 must already have one legal deploy or the verb is invisible on the opening beat");

            Assert.IsFalse(SupplyRules.TrySpend(SupplyRules.StartSupply, DeploymentRules.CostOf(DeployCard.Cannon), out remaining),
                "the opening bank must NOT fund a battery — the Cannon has to be saved for");
        }

        [Test]
        public void SupplyCeiling_ForbidsAFiveBodyAlphaStrike()
        {
            int knightsAtCap = Mathf.FloorToInt(SupplyRules.MaxSupply / DeploymentRules.CostOf(DeployCard.Knight));
            Assert.Less(knightsAtCap, 5,
                "a full bank must not buy five bodies at once — the ceiling exists to forbid the banked alpha strike");
            Assert.GreaterOrEqual(knightsAtCap, 3,
                "a full bank must still feel like a war chest, or the ceiling is punishing rather than pacing");
        }

        [Test]
        public void OneTurnOfRegen_FundsTwoBodies_ButNotABattery()
        {
            const float turnSeconds = 15f;
            float earned = SupplyRules.Regen(0f, turnSeconds);

            Assert.GreaterOrEqual(earned, 2f * DeploymentRules.CostOf(DeployCard.Knight),
                "one turn of regen must fund two bodies — that is the pacing floor that keeps action density up");
            Assert.Less(earned, DeploymentRules.CostOf(DeployCard.Cannon),
                "one turn of regen must NOT fund a battery — siting one has to cost more than a single turn");
        }

        // ---- §6 Cannon installation ----

        [Test]
        public void CannonStatBlock_MatchesTheDesignSheet()
        {
            Assert.AreEqual(140f, CannonRules.MaxHP, Tol,
                "battery HP is what makes a turret answerable by one volley");
            Assert.AreEqual(13f, CannonRules.Range, Tol,
                "battery reach decides whether siting one behind your line is a real decision");
            Assert.AreEqual(3.2f, CannonRules.ReloadSeconds, Tol,
                "the reload clock is the battery's rate limit");
            Assert.AreEqual(42f, CannonRules.ShellDamage, Tol,
                "shell damage is the battery's whole damage contribution");
            Assert.AreEqual(2.4f, CannonRules.ShellSplashRadius, Tol,
                "splash radius is the niche the removed bomber left behind, and it has to be " +
                "wide enough to catch a block's neighbours or the battery is just a slow arrow");
        }

        [Test]
        public void SustainedDps_IsDamagePerReload()
        {
            Assert.AreEqual(CannonRules.ShellDamage / CannonRules.ReloadSeconds, CannonRules.SustainedDps, Tol,
                "sustained output must follow damage and reload — it may not be pinned to a stale literal");
            Assert.AreEqual(13.125f, CannonRules.SustainedDps, 1e-3f,
                "the battery's sustained output is the number the whole §6 counterplay budget is built on");
        }

        [Test]
        public void BatteriesAtCap_StayBelowTheVolleysBurst()
        {
            float atCap = CannonRules.SustainedDps * DeploymentRules.CapFor(DeployCard.Cannon);
            Assert.AreEqual(26.25f, atCap, 1e-2f,
                "two batteries is the full artillery budget a player can ever face at once");
            Assert.Greater(CannonRules.MaxHP, CannonRules.ShellDamage,
                "a battery must survive its own shell's worth of damage — trading batteries cannot be free");
        }

        [Test]
        public void InRange_IsInclusiveAtRange_AndRejectsNonsense()
        {
            Assert.IsTrue(CannonRules.InRange(0f),
                "a target on the muzzle is in reach");
            Assert.IsTrue(CannonRules.InRange(CannonRules.Range),
                "the range value is INCLUSIVE — a target exactly at reach is a valid target");
            Assert.IsFalse(CannonRules.InRange(CannonRules.Range + 0.01f),
                "one step past reach must be refused — range is the hard edge of the battery's threat");
            Assert.IsFalse(CannonRules.InRange(-0.01f),
                "a negative distance is not a target — bad geometry must not be read as point blank");
        }

        // ---- §6 Ballistic shell ----

        [Test]
        public void SolveShellVelocity_ArcsUpward_ForALevelTarget()
        {
            var muzzle = new Vector2(0f, CannonRules.MuzzleHeight);
            var v = CannonRules.SolveShellVelocity(muzzle, new Vector2(8f, CannonRules.MuzzleHeight), Gravity);

            Assert.Greater(v.y, 0f,
                "the shell must leave the muzzle climbing — a flat shot would drill into the battery's own wall");
        }

        [Test]
        public void SolveShellVelocity_PointsAtTheTarget_InBothDirections()
        {
            var muzzle = new Vector2(0f, CannonRules.MuzzleHeight);
            var right = CannonRules.SolveShellVelocity(muzzle, new Vector2(8f, CannonRules.MuzzleHeight), Gravity);
            var left = CannonRules.SolveShellVelocity(muzzle, new Vector2(-8f, CannonRules.MuzzleHeight), Gravity);

            Assert.Greater(right.x, 0f,
                "a target to the right must be shot toward the right");
            Assert.Less(left.x, 0f,
                "a target to the left must be shot toward the left — the battery serves whichever side deployed it");
            Assert.AreEqual(right.x, -left.x, BallisticTol,
                "mirrored targets must get mirrored shots — the solve may not favour one facing");
            Assert.AreEqual(right.y, left.y, BallisticTol,
                "facing must not change the arc height");
        }

        [Test]
        public void SolveShellVelocity_ActuallyLandsOnTheTarget()
        {
            // Independent oracle: integrate the returned velocity under gravity and solve for
            // the moment it returns to the target's height. A straight-line "solve" cannot pass.
            AssertShellLands(new Vector2(0f, CannonRules.MuzzleHeight), new Vector2(8f, CannonRules.MuzzleHeight));
            AssertShellLands(new Vector2(0f, CannonRules.MuzzleHeight), new Vector2(-8f, CannonRules.MuzzleHeight));
            AssertShellLands(new Vector2(0f, CannonRules.MuzzleHeight), new Vector2(6f, 3f));
            AssertShellLands(new Vector2(0f, 4f), new Vector2(7f, CannonRules.MuzzleHeight));
        }

        [Test]
        public void SolveShellVelocity_ApexesAboveTheHigherEndpoint_SoItClearsTheWall()
        {
            // Peak height from the returned velocity alone (vy² / 2g) — derived from physics,
            // not from the solver's internals.
            AssertShellApexClearsWall(new Vector2(0f, CannonRules.MuzzleHeight), new Vector2(8f, CannonRules.MuzzleHeight));
            AssertShellApexClearsWall(new Vector2(0f, CannonRules.MuzzleHeight), new Vector2(6f, 3f));
            AssertShellApexClearsWall(new Vector2(0f, 4f), new Vector2(7f, CannonRules.MuzzleHeight));
        }

        [Test]
        public void SolveShellVelocity_FallsBackToAFlatLob_WhenGravityIsDisabled()
        {
            var muzzle = new Vector2(0f, CannonRules.MuzzleHeight);
            var right = CannonRules.SolveShellVelocity(muzzle, new Vector2(8f, CannonRules.MuzzleHeight), 0f);
            var left = CannonRules.SolveShellVelocity(muzzle, new Vector2(-8f, CannonRules.MuzzleHeight), 0f);

            Assert.AreEqual(CannonRules.Range, right.magnitude, BallisticTol,
                "with gravity off the shell must still travel at the battery's own reach, not at an arbitrary speed");
            Assert.Greater(right.x, 0f,
                "the gravity-free fallback must still aim at the target");
            Assert.Less(left.x, 0f,
                "the gravity-free fallback must aim left for a left-hand target");
            Assert.AreEqual(0f, right.y, BallisticTol,
                "with no gravity to fight there is nothing to arc over, so the lob goes flat");
        }

        [Test]
        public void SolveShellVelocity_StaysFinite_ForADegenerateTarget()
        {
            var muzzle = new Vector2(-6f, CannonRules.MuzzleHeight);
            var v = CannonRules.SolveShellVelocity(muzzle, muzzle, Gravity);

            Assert.IsFalse(float.IsNaN(v.x) || float.IsNaN(v.y),
                "a target on the muzzle must not produce NaN — one bad frame cannot fling a shell nowhere");
            Assert.IsFalse(float.IsInfinity(v.x) || float.IsInfinity(v.y),
                "a zero-distance solve must not divide by a zero flight time");
        }

        // ---- §2 Card identity and selection ----

        [Test]
        public void OnlyTheCannonIsDeployOnly()
        {
            foreach (var card in AllCards)
            {
                bool expected = card == DeployCard.Cannon;
                Assert.AreEqual(expected, DeploymentRules.IsDeployOnly(card),
                    expected
                        ? "the Cannon is an installation — launching it would make it a fourth projectile, the exact defect being fixed"
                        : $"{card} must keep the launch verb — the deploy verb was added beside it, not in place of it");
            }
        }

        [Test]
        public void FromIndex_MapsEachRosterSlot_AndClampsUnknownSlots()
        {
            Assert.AreEqual(DeployCard.Knight, DeploymentRules.FromIndex(0), "roster slot 1 is the melee card");
            Assert.AreEqual(DeployCard.Archer, DeploymentRules.FromIndex(1), "roster slot 2 is the ranged card");
            Assert.AreEqual(DeployCard.Cannon, DeploymentRules.FromIndex(2), "roster slot 3 is the battery that replaced the launched bomb soldier");
            Assert.AreEqual(DeployCard.Barrel, DeploymentRules.FromIndex(3), "roster slot 4 is the hazard");

            Assert.AreEqual(DeployCard.Knight, DeploymentRules.FromIndex(-1),
                "an out-of-range slot must resolve to a legal card, never throw at the player's click");
            Assert.AreEqual(DeployCard.Knight, DeploymentRules.FromIndex(99),
                "an unwired button must fall back to the turn-0 card rather than a locked one");
        }

        [Test]
        public void FromIndex_GivesEveryCardItsOwnSlot()
        {
            var seen = new System.Collections.Generic.HashSet<DeployCard>();
            for (int i = 0; i < AllCards.Length; i++)
            {
                Assert.IsTrue(seen.Add(DeploymentRules.FromIndex(i)),
                    $"slot {i} collides with an earlier slot — two buttons would deploy the same card");
            }
            Assert.AreEqual(AllCards.Length, seen.Count,
                "every roster card must be reachable from some button, or a card is dead content");
        }

        [Test]
        public void AiPreferenceOrder_CoversEveryCardExactlyOnce()
        {
            var order = DeploymentRules.AiPreferenceOrder;
            var seen = new System.Collections.Generic.HashSet<DeployCard>();

            foreach (var card in order)
            {
                Assert.IsTrue(seen.Add(card),
                    $"{card} appears twice in the AI's preference walk — a duplicate silently doubles its weight");
            }

            Assert.AreEqual(AllCards.Length, order.Length,
                "the AI must consider the whole roster — a missing entry makes that card unreachable for the AI");
            foreach (var card in AllCards)
            {
                Assert.IsTrue(seen.Contains(card),
                    $"{card} is unreachable by the AI — the enemy economy must be symmetric with the player's");
            }
        }

        [Test]
        public void AiPreferenceOrder_ReachesForTheMostBoardChangingCardFirst()
        {
            var order = DeploymentRules.AiPreferenceOrder;
            Assert.AreEqual(DeployCard.Cannon, order[0],
                "the AI must reach for the structure first — otherwise it spends its bank before it can ever afford one");
            Assert.AreEqual(DeployCard.Cannon, MostExpensive(order),
                "the AI's first pick must be its priciest card, or the preference walk is not board-impact ordered");
        }

        // ---- helpers ----

        private static DeployCard MostExpensive(DeployCard[] cards)
        {
            var best = cards[0];
            foreach (var card in cards)
            {
                if (DeploymentRules.CostOf(card) > DeploymentRules.CostOf(best)) best = card;
            }
            return best;
        }

        /// <summary>
        /// Forward-integrates the solved velocity under gravity and asserts the shell reaches
        /// the target's height at the target's x. Uses the closed-form projectile solution as an
        /// oracle independent of how the solver splits its flight time.
        /// </summary>
        private static void AssertShellLands(Vector2 muzzle, Vector2 target)
        {
            var v = CannonRules.SolveShellVelocity(muzzle, target, Gravity);
            float dy = target.y - muzzle.y;
            float disc = v.y * v.y - 2f * Gravity * dy;

            Assert.Greater(disc, 0f,
                $"shell fired at {target} never reaches the target's height — the arc undershoots");

            float flight = (v.y + Mathf.Sqrt(disc)) / Gravity;
            float landX = muzzle.x + v.x * flight;

            Assert.AreEqual(target.x, landX, BallisticTol,
                $"the shell must come down on {target}: a ballistic solve that misses its own target is a straight line with extra steps");
        }

        /// <summary>
        /// Asserts the trajectory peaks ArcApexBonus above the higher endpoint — the documented
        /// reason the battery can be sited behind its own wall.
        /// </summary>
        private static void AssertShellApexClearsWall(Vector2 muzzle, Vector2 target)
        {
            var v = CannonRules.SolveShellVelocity(muzzle, target, Gravity);
            float peak = muzzle.y + (v.y * v.y) / (2f * Gravity);
            float wall = Mathf.Max(muzzle.y, target.y);

            Assert.AreEqual(wall + CannonRules.ArcApexBonus, peak, BallisticTol,
                $"the arc to {target} must crest clear above the higher endpoint — that clearance is what lets a battery sit behind its own line");
            Assert.Greater(peak, target.y,
                $"the arc to {target} must come down onto the target from above, never rise into it");
        }
    }
}
