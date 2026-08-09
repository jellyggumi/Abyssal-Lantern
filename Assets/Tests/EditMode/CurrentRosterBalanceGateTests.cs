using System;
using System.Collections.Generic;
using CastleBusters;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CastleBusters.Tests
{
    [TestFixture]
    public class CurrentRosterBalanceGateTests
    {
        private const float TickSeconds = 0.01f;
        private const float ComparableTtkTolerance = 0.15f;
        private const float ReversalShareLimit = 0.30f;
        private const float MaxEngagementSeconds = 30f;

        private static readonly int[] Seeds =
        {
            104729, 130363, 155921, 196613, 262147, 327673, 393241, 524287
        };

        private readonly List<GameObject> spawned = new List<GameObject>();
        private RoleProfile knight;
        private RoleProfile archer;
        private RoleProfile cannon;
        private HazardProfile barrel;

        private CoreDefenseProfile coreDefense;
        [OneTimeSetUp]
        public void LoadShippedRoster()
        {
            HeroGrowth.Reset();
            knight = LoadBody(DeployCard.Knight, "Assets/Prefabs/Knight.prefab");
            archer = LoadBody(DeployCard.Archer, "Assets/Prefabs/Archer.prefab");
            cannon = new RoleProfile(
                DeployCard.Cannon,
                CannonRules.MaxHP,
                CannonRules.ShellDamage,
                CannonRules.Range,
                0f,
                CannonRules.ReloadSeconds,
                CannonRules.ReloadSeconds,
                0f,
                CannonRules.ShellSplashRadius);

            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExplosiveBarrel.prefab");
            Assert.That(barrelPrefab, Is.Not.Null, "The shipped Barrel prefab is required to measure its hazard role.");
            var explosive = barrelPrefab.GetComponent<ExplosiveGimmick>();
            Assert.That(explosive, Is.Not.Null, "The shipped Barrel prefab must expose its runtime explosion component.");
            barrel = new HazardProfile(explosive.explosionDamage, explosive.explosionRadius, UnitCombos.BarrelFuseSeconds);
            coreDefense = LoadCoreDefense();
        }

        [OneTimeTearDown]
        public void DestroyLoadedRosterInstances()
        {
            for (int i = spawned.Count - 1; i >= 0; i--)
            {
                if (spawned[i] != null) UnityEngine.Object.DestroyImmediate(spawned[i]);
            }
            spawned.Clear();
            HeroGrowth.Reset();
        }

        [Test]
        public void DamageSurvivabilityAndCostMatrix_PreservesDistinctEfficientRoles()
        {
            float knightDps = SustainedDps(knight, 6);
            float archerDps = SustainedDps(archer, 10);
            float cannonDps = CannonRules.SustainedDps;

            float knightDamagePerSupply = knightDps / DeploymentRules.CostOf(DeployCard.Knight);
            float archerDamagePerSupply = archerDps / DeploymentRules.CostOf(DeployCard.Archer);
            float bodyEfficiencyDelta = RelativeDelta(knightDamagePerSupply, archerDamagePerSupply);

            float bodyEfficiencyMean = (knightDamagePerSupply + archerDamagePerSupply) * 0.5f;
            float cannonSingleTargetPerSupply = cannonDps / DeploymentRules.CostOf(DeployCard.Cannon);
            float cannonFourTargetPerSupply = cannonSingleTargetPerSupply * 4f;
            float cannonUsefulDelta = RelativeDelta(cannonFourTargetPerSupply, bodyEfficiencyMean);

            float knightHpPerSupply = knight.Hp / DeploymentRules.CostOf(DeployCard.Knight);
            float archerHpPerSupply = archer.Hp / DeploymentRules.CostOf(DeployCard.Archer);
            float cannonHpPerSupply = cannon.Hp / DeploymentRules.CostOf(DeployCard.Cannon);

            TestContext.WriteLine(
                $"BALANCE_MATRIX|Knight|dps={knightDps:F3}|dps_per_supply={knightDamagePerSupply:F3}|hp_per_supply={knightHpPerSupply:F3}");
            TestContext.WriteLine(
                $"BALANCE_MATRIX|Archer|dps={archerDps:F3}|dps_per_supply={archerDamagePerSupply:F3}|hp_per_supply={archerHpPerSupply:F3}");
            TestContext.WriteLine(
                $"BALANCE_MATRIX|Cannon|single_dps={cannonDps:F3}|single_dps_per_supply={cannonSingleTargetPerSupply:F3}|four_target_dps_per_supply={cannonFourTargetPerSupply:F3}|hp_per_supply={cannonHpPerSupply:F3}");
            TestContext.WriteLine(
                $"THRESHOLD|body_dps_per_supply_delta={bodyEfficiencyDelta:P2}|limit={ComparableTtkTolerance:P0}|cannon_four_target_delta={cannonUsefulDelta:P2}");

            Assert.That(bodyEfficiencyDelta, Is.LessThanOrEqualTo(ComparableTtkTolerance),
                "The two directly comparable mobile bodies must stay within the default ±15% sustained damage-per-supply band.");
            Assert.That(cannonUsefulDelta, Is.LessThanOrEqualTo(ComparableTtkTolerance),
                "A four-target splash opportunity must make the expensive artillery competitive with body damage efficiency.");
            Assert.That(cannonSingleTargetPerSupply, Is.LessThan(bodyEfficiencyMean * 0.5f),
                "Cannon must pay a real single-target efficiency cost for its range and splash; otherwise it dominates body cards.");
            Assert.That(knightHpPerSupply, Is.GreaterThan(archerHpPerSupply),
                "Knight's frontline role must buy more survivability per supply than Archer's ranged role.");
            Assert.That(archerHpPerSupply, Is.GreaterThan(cannonHpPerSupply),
                "The deploy-only Cannon must not also become the roster's most efficient health purchase.");
        }

        [Test]
        public void ScriptedCounters_KeepKnightArcherAndCannonIndependentlyViable()
        {
            EngagementResult close = SimulateDuel(knight, archer, 2.5f);
            EngagementResult open = SimulateDuel(knight, archer, 10f);
            float cappedBatteryTtk = SiegeTtk(cannonCount: DeploymentRules.BatteryCap, targetHp: coreDefense.Total, distance: 10f);
            float singleBatteryTtk = SiegeTtk(cannonCount: 1, targetHp: coreDefense.Total, distance: 10f);

            TestContext.WriteLine(
                $"COUNTER|Knight_vs_Archer_close|distance=2.500|winner={close.Winner}|ttk={close.Ttk:F3}");
            TestContext.WriteLine(
                $"COUNTER|Knight_vs_Archer_open|distance=10.000|winner={open.Winner}|ttk={open.Ttk:F3}");
            TestContext.WriteLine(
                $"ROLE_OUTCOME|Cannon_siege|core_hp={coreDefense.CoreHp:F3}|shield_hp={coreDefense.ShieldHp:F3}|defense_pool={coreDefense.Total:F3}|one_battery_ttk={singleBatteryTtk:F3}|cap_batteries={DeploymentRules.BatteryCap}|cap_ttk={cappedBatteryTtk:F3}|threshold={DeploymentRules.CooldownOf(DeployCard.Cannon):F3}");

            Assert.That(close.Winner, Is.EqualTo(DeployCard.Knight),
                "Knight must retain a close-start body matchup where armor and combo pressure matter.");
            Assert.That(open.Winner, Is.EqualTo(DeployCard.Archer),
                "Archer must retain an open-field counter where range converts into a win before Knight closes.");
            Assert.That(cappedBatteryTtk, Is.LessThanOrEqualTo(DeploymentRules.CooldownOf(DeployCard.Cannon)),
                "Two unanswered batteries at the shipped cap must remain a viable siege strategy within one Cannon deploy-cooldown window.");
            Assert.That(singleBatteryTtk, Is.GreaterThan(cappedBatteryTtk),
                "Cannon viability must scale through its bounded battery cap rather than one installation dominating alone.");
        }

        [Test]
        public void SeededRuntimeDeploymentFactionBranch_MirrorsPlayerAndAiLegalityWithinFifteenPercent()
        {
            int playerLegal = 0;
            int aiLegal = 0;
            int playerEnemyHalfRejected = 0;
            int aiEnemyHalfRejected = 0;

            foreach (int seed in Seeds)
            {
                var random = new System.Random(seed);
                bool seededSideIsPlayer = seed % 5 < 2;
                float magnitude = NextRange(random, 2f, 8f);
                float y = NextRange(random, DeploymentRules.MinY + 0.25f, DeploymentRules.MaxY - 0.25f);
                Vector2 seededOwnHalf = new Vector2(seededSideIsPlayer ? -magnitude : magnitude, y);
                Vector2 mirroredOwnHalf = new Vector2(-seededOwnHalf.x, seededOwnHalf.y);

                AssertRuntimeDeploymentOwnership(
                    seed,
                    seededSideIsPlayer,
                    seededOwnHalf,
                    ref playerLegal,
                    ref aiLegal,
                    ref playerEnemyHalfRejected,
                    ref aiEnemyHalfRejected);
                AssertRuntimeDeploymentOwnership(
                    seed,
                    !seededSideIsPlayer,
                    mirroredOwnHalf,
                    ref playerLegal,
                    ref aiLegal,
                    ref playerEnemyHalfRejected,
                    ref aiEnemyHalfRejected);
            }

            float playerAcceptance = playerLegal / (float)Seeds.Length;
            float aiAcceptance = aiLegal / (float)Seeds.Length;
            float sideDelta = RelativeDelta(playerAcceptance, aiAcceptance);
            TestContext.WriteLine(
                $"FACTION_BRANCH|samples_per_side={Seeds.Length}|seeds={string.Join(",", Seeds)}|player_own_half_legal={playerLegal}|ai_own_half_legal={aiLegal}|player_enemy_half_rejected={playerEnemyHalfRejected}|ai_enemy_half_rejected={aiEnemyHalfRejected}|acceptance_delta={sideDelta:P3}|limit={ComparableTtkTolerance:P0}");
            TestContext.WriteLine("WIN_RATE|not_claimed=true|reason=runtime_deployment_ownership_is_not_a_match_AI_decision_model");

            Assert.That(sideDelta, Is.LessThanOrEqualTo(ComparableTtkTolerance),
                "The shipped player/AI deployment ownership branch must stay inside the ±15% side-parity band.");
        }

        [Test]
        public void BarrelHazard_HasAClusterPayoffAndARangedCountercondition()
        {
            float clusteredTargetHp = archer.Hp * 2f;
            float clusteredDamage = barrel.Damage * 2f;
            float outsideDamage = DamageAtDistance(barrel, barrel.Radius + 0.01f);

            var arrow = archer.ArrowPrefab != null ? archer.ArrowPrefab.GetComponent<ArrowController>() : null;
            Assert.That(arrow, Is.Not.Null, "Archer's shipped projectile is required for the ranged Barrel countercondition.");
            float counterDistance = Mathf.Min(archer.Range, barrel.Radius + 1f);
            float remoteTriggerSeconds = counterDistance / arrow.speed;

            TestContext.WriteLine(
                $"HAZARD|Barrel|damage={barrel.Damage:F3}|radius={barrel.Radius:F3}|fuse={barrel.FuseSeconds:F3}|two_archer_hp={clusteredTargetHp:F3}|two_target_damage={clusteredDamage:F3}");
            TestContext.WriteLine(
                $"HAZARD_COUNTER|trigger_distance={counterDistance:F3}|arrow_travel={remoteTriggerSeconds:F3}|outside_damage={outsideDamage:F3}|fuse={barrel.FuseSeconds:F3}");

            Assert.That(clusteredDamage, Is.GreaterThanOrEqualTo(clusteredTargetHp),
                "A well-placed Barrel must threaten a two-Archer cluster; otherwise the hazard card has no bounded useful condition.");
            Assert.That(outsideDamage, Is.Zero,
                "The first point beyond Barrel radius must be a real no-damage countercondition, not a hidden falloff ambiguity.");
            Assert.That(counterDistance, Is.GreaterThan(barrel.Radius),
                "Archer must be able to trigger a Barrel from outside the blast.");
            Assert.That(remoteTriggerSeconds, Is.LessThan(barrel.FuseSeconds),
                "The shipped arrow must reach a remotely triggered Barrel before its armed fuse resolves.");
        }

        [TestCase(false, TestName = "Barrel_LastStandThenPowerUp_RemainsAtDamageCap")]
        [TestCase(true, TestName = "Barrel_PowerUpThenLastStand_RemainsAtDamageCap")]
        public void Barrel_LastStandPowerUpComposition_RemainsAtDamageCap(bool powerUpFirst)
        {
            const float baseDamage = 60f;
            const float baseRadius = 2f;
            const float powerUpMultiplier = 1.35f;
            const float maximumCombinedDamage = 140f;

            var managerObject = new GameObject("LastStandPowerUpCompositionGameManager")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            managerObject.SetActive(false);
            var gameManager = managerObject.AddComponent<GameManager>();
            var barrelObject = new GameObject("LastStandPowerUpCompositionBarrel")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var gateObject = new GameObject("LastStandPowerUpCompositionGate")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                var explosive = barrelObject.AddComponent<ExplosiveGimmick>();
                explosive.SetPermanentPotency(baseDamage, baseRadius);
                var unit = barrelObject.AddComponent<UnitController>();
                unit.unitType = UnitType.Barrel;
                unit.isPlayerUnit = true;

                var gate = gateObject.AddComponent<EventGateGimmick>();
                gate.effectType = EventGateEffectType.PowerUp;
                gate.damageSpeedMultiplier = powerUpMultiplier;
                gameManager.playerLastStand = LastStand.Phase.Active;

                if (powerUpFirst) gate.ApplyToUnit(unit);
                gameManager.ApplyLastStandOnLaunch(unit, Vector2.right);
                if (!powerUpFirst) gate.ApplyToUnit(unit);

                Assert.That(explosive.explosionDamage, Is.EqualTo(maximumCombinedDamage).Within(0.0001f),
                    "A Barrel whose 60 damage crosses both the 2.2x Last Stand and 1.35x PowerUp boundary must stop at 140 regardless of composition order.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gateObject);
                UnityEngine.Object.DestroyImmediate(barrelObject);
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void Barrel_PowerUpWithoutLastStand_AppliesConfiguredDamageMultiplier()
        {
            const float baseDamage = 60f;
            const float baseRadius = 2f;
            const float powerUpMultiplier = 1.35f;

            var managerObject = new GameObject("OrdinaryPowerUpGameManager")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            managerObject.SetActive(false);
            var gameManager = managerObject.AddComponent<GameManager>();
            var barrelObject = new GameObject("OrdinaryPowerUpBarrel")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            var gateObject = new GameObject("OrdinaryPowerUpGate")
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            try
            {
                var explosive = barrelObject.AddComponent<ExplosiveGimmick>();
                explosive.SetPermanentPotency(baseDamage, baseRadius);
                var unit = barrelObject.AddComponent<UnitController>();
                unit.unitType = UnitType.Barrel;
                unit.isPlayerUnit = true;

                var gate = gateObject.AddComponent<EventGateGimmick>();
                gate.effectType = EventGateEffectType.PowerUp;
                gate.damageSpeedMultiplier = powerUpMultiplier;
                gameManager.playerLastStand = LastStand.Phase.Locked;

                gameManager.ApplyLastStandOnLaunch(unit, Vector2.right);
                gate.ApplyToUnit(unit);

                Assert.That(explosive.explosionDamage, Is.EqualTo(baseDamage * powerUpMultiplier).Within(0.0001f),
                    "An ordinary Barrel must retain the configured PowerUp explosion scaling when no Last Stand cap source was applied.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(gateObject);
                UnityEngine.Object.DestroyImmediate(barrelObject);
                UnityEngine.Object.DestroyImmediate(managerObject);
            }
        }

        [Test]
        public void LastStand_ExtraDamageShare_DoesNotExceedThirtyPercentOfCoreDefensePool()
        {
            var launchDamageByRole = new Dictionary<DeployCard, float>
            {
                { DeployCard.Knight, knight.Damage },
                { DeployCard.Archer, archer.Damage },
                { DeployCard.Barrel, barrel.Damage }
            };

            foreach (DeployCard card in Enum.GetValues(typeof(DeployCard)))
            {
                Assert.That(
                    launchDamageByRole.ContainsKey(card),
                    Is.EqualTo(!DeploymentRules.IsDeployOnly(card)),
                    $"{card} Last Stand applicability must match the shipped launch-versus-deploy-only selection contract.");
            }

            float worstShare = 0f;
            DeployCard worstRole = DeployCard.Knight;
            foreach (var entry in launchDamageByRole)
            {
                float buffed = LastStand.BuffedDamage(entry.Value, isPlayer: true);
                float extraShare = (buffed - entry.Value) / coreDefense.Total;
                if (extraShare > worstShare)
                {
                    worstShare = extraShare;
                    worstRole = entry.Key;
                }
                TestContext.WriteLine(
                    $"REVERSAL|{entry.Key}|applicable=true|base={entry.Value:F3}|buffed={buffed:F3}|extra_defense_share={extraShare:P2}");
            }
            TestContext.WriteLine(
                "REVERSAL|Cannon|applicable=false|reason=deploy_only|runtime_extra_share=0.00%");
            TestContext.WriteLine(
                $"CORE_DEFENSE|runtime_component={nameof(CastleCoreGimmick)}|core_hp={coreDefense.CoreHp:F3}|shield_hp={coreDefense.ShieldHp:F3}|defense_pool={coreDefense.Total:F3}");
            TestContext.WriteLine(
                $"THRESHOLD|worst_reachable_role={worstRole}|worst_extra_share={worstShare:P2}|limit={ReversalShareLimit:P0}|defense_pool={coreDefense.Total:F3}");

            Assert.That(worstShare, Is.LessThanOrEqualTo(ReversalShareLimit + 0.0001f),
                "The largest one-shot Last Stand uplift among runtime-reachable launched roles must not reverse more than 30% of the shipped runtime core-plus-shield defense pool.");
        }

        private RoleProfile LoadBody(DeployCard card, string prefabPath)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            Assert.That(prefab, Is.Not.Null, $"The shipped {card} prefab is required for the balance gate.");
            var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            Assert.That(instance, Is.Not.Null, $"Could not instantiate shipped {card} runtime values.");
            instance.hideFlags = HideFlags.HideAndDontSave;
            spawned.Add(instance);

            var unit = instance.GetComponent<UnitController>();
            Assert.That(unit, Is.Not.Null, $"The shipped {card} prefab must expose UnitController.");
            return new RoleProfile(
                card,
                unit.maxHP,
                unit.attackDamage,
                unit.attackRange,
                unit.moveSpeed,
                unit.attackCooldown,
                0f,
                unit.knightComboIntervalSeconds,
                0f,
                unit.arrowPrefab);
        }

        private CoreDefenseProfile LoadCoreDefense()
        {
            var coreObject = new GameObject("CurrentRosterBalanceGate_RuntimeCore");
            coreObject.hideFlags = HideFlags.HideAndDontSave;
            coreObject.AddComponent<SpriteRenderer>();
            coreObject.AddComponent<BoxCollider2D>();
            coreObject.AddComponent<Rigidbody2D>();
            var core = coreObject.AddComponent<CastleCoreGimmick>();
            spawned.Add(coreObject);

            Assert.That(core, Is.Not.Null,
                "The shipped runtime core source must expose CastleCoreGimmick.");
            var awake = typeof(CastleCoreGimmick).GetMethod(
                "Awake",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null,
                "CastleCoreGimmick runtime initialization must remain discoverable.");
            awake.Invoke(core, null);

            float coreHp = core.maxHP;
            Assert.That(coreHp, Is.GreaterThan(0f),
                "The shipped runtime core component must expose a positive defense value.");
            Assert.That(core.currentHP, Is.EqualTo(coreHp).Within(0.0001f),
                "CastleCoreGimmick runtime initialization must materialize its full core HP.");

            float halfCoreDamage = coreHp * 0.5f;
            UnityEngine.Random.State priorRandomState = UnityEngine.Random.state;
            float hpAfterShieldTrigger;
            float shieldHp;
            try
            {
                UnityEngine.Random.InitState(Seeds[0]);
                core.TakeDamage(halfCoreDamage);
                hpAfterShieldTrigger = core.currentHP;
                core.TakeDamage(halfCoreDamage);
                float coreDamageAfterShield = hpAfterShieldTrigger - core.currentHP;
                shieldHp = halfCoreDamage - coreDamageAfterShield;
            }
            finally
            {
                UnityEngine.Random.state = priorRandomState;
            }
            Assert.That(shieldHp, Is.GreaterThan(0f),
                "Crossing the shipped core threshold must materialize a positive one-time shield.");

            return new CoreDefenseProfile(coreHp, shieldHp);
        }

        private static void AssertRuntimeDeploymentOwnership(
            int seed,
            bool deployerIsPlayer,
            Vector2 ownHalfPosition,
            ref int playerLegal,
            ref int aiLegal,
            ref int playerEnemyHalfRejected,
            ref int aiEnemyHalfRejected)
        {
            DeployBlockReason ownHalf = DeploymentRules.Evaluate(
                DeployCard.Knight,
                DeploymentRules.UnlockTurn(DeployCard.Knight),
                aliveInGroup: 0,
                cooldownRemaining: 0f,
                supply: DeploymentRules.CostOf(DeployCard.Knight),
                position: ownHalfPosition,
                deployerIsPlayer: deployerIsPlayer);
            DeployBlockReason enemyHalf = DeploymentRules.Evaluate(
                DeployCard.Knight,
                DeploymentRules.UnlockTurn(DeployCard.Knight),
                aliveInGroup: 0,
                cooldownRemaining: 0f,
                supply: DeploymentRules.CostOf(DeployCard.Knight),
                position: ownHalfPosition,
                deployerIsPlayer: !deployerIsPlayer);

            Assert.That(ownHalf, Is.EqualTo(DeployBlockReason.None),
                $"Seed {seed}: {(deployerIsPlayer ? "player" : "AI")} ownership must accept its own-half deployment.");
            Assert.That(enemyHalf, Is.EqualTo(DeployBlockReason.Zone),
                $"Seed {seed}: the opposing ownership branch must reject that same position as enemy territory.");

            if (deployerIsPlayer)
            {
                playerLegal++;
                aiEnemyHalfRejected++;
            }
            else
            {
                aiLegal++;
                playerEnemyHalfRejected++;
            }
        }

        private static float SustainedDps(RoleProfile role, int attacks)
        {
            float damage = 0f;
            for (int ordinal = 1; ordinal <= attacks; ordinal++)
            {
                if (role.Card == DeployCard.Knight)
                {
                    damage += role.Damage * UnitCombos.KnightHits(ordinal);
                }
                else if (role.Card == DeployCard.Archer)
                {
                    damage += role.Damage * UnitCombos.ArrowsFor(UnitCombos.ArcherVolley(ordinal));
                }
                else
                {
                    damage += role.Damage;
                }
            }
            return damage / (attacks * role.Cooldown);
        }

        private static EngagementResult SimulateDuel(RoleProfile leftProfile, RoleProfile rightProfile, float separation)
        {
            var left = new Combatant(leftProfile, -separation * 0.5f);
            var right = new Combatant(rightProfile, separation * 0.5f);

            for (float time = 0f; time <= MaxEngagementSeconds; time += TickSeconds)
            {
                float distance = Mathf.Abs(right.X - left.X);
                ScheduleAttack(left, right, distance, time);
                ScheduleAttack(right, left, distance, time);

                float damageToLeft = ConsumeDueDamage(right, time);
                float damageToRight = ConsumeDueDamage(left, time);
                left.Hp -= damageToLeft;
                right.Hp -= damageToRight;

                bool leftDead = left.Hp <= 0f;
                bool rightDead = right.Hp <= 0f;
                if (leftDead || rightDead)
                {
                    DeployCard? winner = leftDead == rightDead
                        ? (DeployCard?)null
                        : leftDead ? right.Profile.Card : left.Profile.Card;
                    return new EngagementResult(winner, time, left.Hp, right.Hp);
                }

                MoveToward(left, direction: 1f, distance, TickSeconds);
                MoveToward(right, direction: -1f, distance, TickSeconds);
            }

            return new EngagementResult(null, MaxEngagementSeconds, left.Hp, right.Hp);
        }

        private static void ScheduleAttack(Combatant attacker, Combatant target, float distance, float time)
        {
            if (attacker.Hp <= 0f || target.Hp <= 0f) return;
            if (distance > attacker.Profile.Range + 0.0001f) return;
            if (time + 0.0001f < attacker.NextAttack) return;

            attacker.AttackOrdinal++;
            attacker.NextAttack = time + attacker.Profile.Cooldown;

            if (attacker.Profile.Card == DeployCard.Knight)
            {
                int hits = UnitCombos.KnightHits(attacker.AttackOrdinal);
                for (int i = 0; i < hits; i++)
                {
                    attacker.Pending.Add(new ScheduledHit(time + i * attacker.Profile.ComboDelay, attacker.Profile.Damage));
                }
                return;
            }

            if (attacker.Profile.Card == DeployCard.Archer)
            {
                int arrows = UnitCombos.ArrowsFor(UnitCombos.ArcherVolley(attacker.AttackOrdinal));
                attacker.Pending.Add(new ScheduledHit(time, attacker.Profile.Damage));
                if (arrows == 2) attacker.Pending.Add(new ScheduledHit(time + 0.18f, attacker.Profile.Damage));
                return;
            }

            float flightSeconds = BallisticFlightSeconds(distance);
            attacker.Pending.Add(new ScheduledHit(time + flightSeconds, attacker.Profile.Damage));
        }

        private static float ConsumeDueDamage(Combatant attacker, float time)
        {
            float damage = 0f;
            for (int i = attacker.Pending.Count - 1; i >= 0; i--)
            {
                if (attacker.Pending[i].At > time + 0.0001f) continue;
                damage += attacker.Pending[i].Damage;
                attacker.Pending.RemoveAt(i);
            }
            return damage;
        }

        private static void MoveToward(Combatant combatant, float direction, float distance, float dt)
        {
            if (combatant.Profile.Speed <= 0f || distance <= combatant.Profile.Range) return;
            float maxAdvance = Mathf.Max(0f, distance - combatant.Profile.Range);
            combatant.X += direction * Mathf.Min(combatant.Profile.Speed * dt, maxAdvance);
        }

        private static float SiegeTtk(int cannonCount, float targetHp, float distance)
        {
            float volleyDamage = cannonCount * CannonRules.ShellDamage;
            int volleys = Mathf.CeilToInt(targetHp / volleyDamage);
            return volleys * CannonRules.ReloadSeconds + BallisticFlightSeconds(distance);
        }

        private static float BallisticFlightSeconds(float distance)
        {
            Vector2 velocity = CannonRules.SolveShellVelocity(
                new Vector2(0f, CannonRules.MuzzleHeight),
                new Vector2(distance, 0f),
                Physics2D.gravity.y);
            return Mathf.Abs(distance / velocity.x);
        }

        private readonly struct CoreDefenseProfile
        {
            public readonly float CoreHp;
            public readonly float ShieldHp;
            public float Total => CoreHp + ShieldHp;

            public CoreDefenseProfile(float coreHp, float shieldHp)
            {
                CoreHp = coreHp;
                ShieldHp = shieldHp;
            }
        }

        private static float DamageAtDistance(HazardProfile hazard, float distance)
        {
            return distance <= hazard.Radius ? hazard.Damage : 0f;
        }

        private static float RelativeDelta(float a, float b)
        {
            float denominator = Mathf.Max(Mathf.Abs(a), Mathf.Abs(b));
            return denominator <= 0.0001f ? 0f : Mathf.Abs(a - b) / denominator;
        }

        private static float NextRange(System.Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }

        private sealed class RoleProfile
        {
            public readonly DeployCard Card;
            public readonly float Hp;
            public readonly float Damage;
            public readonly float Range;
            public readonly float Speed;
            public readonly float Cooldown;
            public readonly float FirstAttackDelay;
            public readonly float ComboDelay;
            public readonly float SplashRadius;
            public readonly GameObject ArrowPrefab;

            public RoleProfile(
                DeployCard card,
                float hp,
                float damage,
                float range,
                float speed,
                float cooldown,
                float firstAttackDelay,
                float comboDelay,
                float splashRadius,
                GameObject arrowPrefab = null)
            {
                Card = card;
                Hp = hp;
                Damage = damage;
                Range = range;
                Speed = speed;
                Cooldown = cooldown;
                FirstAttackDelay = firstAttackDelay;
                ComboDelay = comboDelay;
                SplashRadius = splashRadius;
                ArrowPrefab = arrowPrefab;
            }
        }

        private readonly struct HazardProfile
        {
            public readonly float Damage;
            public readonly float Radius;
            public readonly float FuseSeconds;

            public HazardProfile(float damage, float radius, float fuseSeconds)
            {
                Damage = damage;
                Radius = radius;
                FuseSeconds = fuseSeconds;
            }
        }

        private sealed class Combatant
        {
            public readonly RoleProfile Profile;
            public readonly List<ScheduledHit> Pending = new List<ScheduledHit>();
            public float Hp;
            public float X;
            public float NextAttack;
            public int AttackOrdinal;

            public Combatant(RoleProfile profile, float x)
            {
                Profile = profile;
                Hp = profile.Hp;
                X = x;
                NextAttack = profile.FirstAttackDelay;
            }
        }

        private readonly struct ScheduledHit
        {
            public readonly float At;
            public readonly float Damage;

            public ScheduledHit(float at, float damage)
            {
                At = at;
                Damage = damage;
            }
        }

        private readonly struct EngagementResult
        {
            public readonly DeployCard? Winner;
            public readonly float Ttk;
            public readonly float LeftHp;
            public readonly float RightHp;

            public EngagementResult(DeployCard? winner, float ttk, float leftHp, float rightHp)
            {
                Winner = winner;
                Ttk = ttk;
                LeftHp = leftHp;
                RightHp = rightHp;
            }
        }
    }
}
