using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// EditMode pins for the AOS overhaul contract (docs/design/aos-overhaul.md):
    /// capture objective, unit combo beats, bomber volley scaling + fuse, vent scheduling,
    /// launch-ring exclusion, balance-event policy, and chariot phases.
    /// </summary>
    public class AosOverhaulTests
    {
        // ---- §2 Knight combos ----

        [Test]
        public void KnightHits_ThirdSwingDoubles_SixthTriples_AndCycles()
        {
            Assert.AreEqual(1, UnitCombos.KnightHits(1));
            Assert.AreEqual(1, UnitCombos.KnightHits(2));
            Assert.AreEqual(2, UnitCombos.KnightHits(3), "3rd swing must be a double");
            Assert.AreEqual(1, UnitCombos.KnightHits(4));
            Assert.AreEqual(1, UnitCombos.KnightHits(5));
            Assert.AreEqual(3, UnitCombos.KnightHits(6), "6th swing must be a triple");
            Assert.AreEqual(2, UnitCombos.KnightHits(9), "cycle repeats: 9th doubles");
            Assert.AreEqual(3, UnitCombos.KnightHits(12), "cycle repeats: 12th triples");
            Assert.AreEqual(1, UnitCombos.KnightHits(0), "guard: non-positive ordinals are single");
            Assert.AreEqual(1, UnitCombos.KnightHits(-3));
        }

        [Test]
        public void KnightShouldPush_OnlyWhenBlockerNear_AndObjectiveFar()
        {
            const float range = 1.5f;
            Assert.IsTrue(UnitCombos.KnightShouldPush(1.0f, 4.0f, range), "close blocker, far target → push");
            Assert.IsFalse(UnitCombos.KnightShouldPush(1.0f, 1.2f, range), "blocker IS the fight → no push");
            Assert.IsFalse(UnitCombos.KnightShouldPush(2.5f, 6.0f, range), "nobody in reach → no push");
        }

        // ---- §2 Archer combos ----

        [Test]
        public void ArcherVolley_FifthDoubles_TenthGoesAerial_AndCycles()
        {
            Assert.AreEqual(UnitCombos.ArcherVolleyKind.Single, UnitCombos.ArcherVolley(1));
            Assert.AreEqual(UnitCombos.ArcherVolleyKind.Single, UnitCombos.ArcherVolley(4));
            Assert.AreEqual(UnitCombos.ArcherVolleyKind.Double, UnitCombos.ArcherVolley(5));
            Assert.AreEqual(UnitCombos.ArcherVolleyKind.FrontAndLob, UnitCombos.ArcherVolley(10));
            Assert.AreEqual(UnitCombos.ArcherVolleyKind.Double, UnitCombos.ArcherVolley(15));
            Assert.AreEqual(UnitCombos.ArcherVolleyKind.FrontAndLob, UnitCombos.ArcherVolley(20));
            Assert.AreEqual(UnitCombos.ArcherVolleyKind.Single, UnitCombos.ArcherVolley(0));
        }

        [Test]
        public void ArcherVolley_ArrowCounts_MatchKind()
        {
            Assert.AreEqual(1, UnitCombos.ArrowsFor(UnitCombos.ArcherVolleyKind.Single));
            Assert.AreEqual(2, UnitCombos.ArrowsFor(UnitCombos.ArcherVolleyKind.Double));
            Assert.AreEqual(2, UnitCombos.ArrowsFor(UnitCombos.ArcherVolleyKind.FrontAndLob));
        }

        [Test]
        public void ArcherShouldJump_OnlyForMeaningfulElevation()
        {
            Assert.IsTrue(UnitCombos.ArcherShouldJump(0f, 1.5f));
            Assert.IsTrue(UnitCombos.ArcherShouldJump(0f, 1.2f), "threshold inclusive");
            Assert.IsFalse(UnitCombos.ArcherShouldJump(0f, 0.8f));
            Assert.IsFalse(UnitCombos.ArcherShouldJump(2f, 0f), "target below → never jump");
        }

        // ---- §2 Powder keg ----

        [Test]
        public void BarrelFuse_IsTwoSeconds()
        {
            Assert.AreEqual(2f, UnitCombos.BarrelFuseSeconds);
        }

        // ---- §3 Vent scheduling ----

        [Test]
        public void VentSchedule_SpawnsEveryThirdBeat_FromTurnTwo()
        {
            Assert.IsFalse(VentSchedule.ShouldSpawnOnTurn(0));
            Assert.IsFalse(VentSchedule.ShouldSpawnOnTurn(1));
            Assert.IsTrue(VentSchedule.ShouldSpawnOnTurn(2));
            Assert.IsFalse(VentSchedule.ShouldSpawnOnTurn(3));
            Assert.IsTrue(VentSchedule.ShouldSpawnOnTurn(5));
            Assert.IsTrue(VentSchedule.ShouldSpawnOnTurn(8));
        }

        [Test]
        public void VentSchedule_AlternatesStyles_AndExpires()
        {
            Assert.AreEqual(EruptionStyle.Magma, VentSchedule.StyleForTurn(2));
            Assert.AreEqual(EruptionStyle.Petal, VentSchedule.StyleForTurn(5));
            Assert.AreEqual(EruptionStyle.Magma, VentSchedule.StyleForTurn(8));
            Assert.IsFalse(VentSchedule.Expired(bornTurn: 2, currentTurn: 4));
            Assert.IsTrue(VentSchedule.Expired(bornTurn: 2, currentTurn: 5));
        }

        [Test]
        public void VentSchedule_PositionsStayBetweenTheCamps()
        {
            Assert.Greater(VentSchedule.MinX, -GameManager.CoreAbsX, "vents never spawn behind the player keep");
            Assert.Less(VentSchedule.MaxX, GameManager.CoreAbsX, "vents never spawn behind the enemy keep");
        }

        // ---- §5 Launch ring exclusion ----

        [Test]
        public void LaunchRing_RejectsMuzzlePositions_AllowsMidfield()
        {
            Assert.IsTrue(LaunchRingRules.IsInsideRing(new Vector2(-14.5f, 0.5f)), "player muzzle center");
            Assert.IsTrue(LaunchRingRules.IsInsideRing(new Vector2(14.5f, 0.5f)), "enemy muzzle center");
            Assert.IsTrue(LaunchRingRules.IsInsideRing(new Vector2(-12.0f, 0.5f)), "inside ring radius");
            Assert.IsFalse(LaunchRingRules.IsInsideRing(new Vector2(0f, 0.5f)), "bridge center is free");
            Assert.IsFalse(LaunchRingRules.IsInsideRing(new Vector2(-7.5f, 0.5f)), "wall slot is free");
            Assert.IsFalse(LaunchRingRules.IsInsideRing(new Vector2(7.5f, 0.5f)), "enemy wall slot is free");
        }

        [Test]
        public void CastleWall_BasePositions_AreOutsideBothRings()
        {
            foreach (var pos in GameManager.WallBasePositions)
            {
                Assert.IsFalse(LaunchRingRules.IsInsideRing(pos), $"wall base {pos} must clear the launch rings");
            }
        }

        // ---- §1 Capture objective ----

        [Test]
        public void CaptureRules_UncontestedAttackersFill_InCaptureSeconds()
        {
            // Accumulate to 5.5s (clearly inside the window), then push past 6s.
            float progress = 0f;
            for (int i = 0; i < 55; i++) progress = CaptureRules.Tick(progress, attackers: 1, defenders: 0, dt: 0.1f);
            Assert.IsFalse(CaptureRules.Captured(progress), "not full at 5.5s of a 6s capture");
            for (int i = 0; i < 8; i++) progress = CaptureRules.Tick(progress, 1, 0, 0.1f);
            Assert.IsTrue(CaptureRules.Captured(progress), "full after 6.3s uncontested");
        }

        [Test]
        public void CaptureRules_DefenderContests_GaugeHolds()
        {
            float progress = 0.5f;
            float after = CaptureRules.Tick(progress, attackers: 2, defenders: 1, dt: 1f);
            Assert.AreEqual(progress, after, 1e-5f, "contested zone must freeze the gauge");
        }

        [Test]
        public void CaptureRules_AbandonedZoneDecays_AtHalfRate()
        {
            float progress = 0.5f;
            float after = CaptureRules.Tick(progress, attackers: 0, defenders: 0, dt: 1f);
            float expected = 0.5f - (1f / CaptureRules.CaptureSeconds) * CaptureRules.DecayRate;
            Assert.AreEqual(expected, after, 1e-5f);
            Assert.AreEqual(0f, CaptureRules.Tick(0.01f, 0, 0, 10f), 1e-5f, "clamped at zero");
        }

        // ---- §6 Balance events ----

        [Test]
        public void BalanceEvents_FireOnlyOnEveryFourthBeat()
        {
            Assert.IsFalse(BalanceEventPlanner.ShouldFireOnTurn(0));
            Assert.IsTrue(BalanceEventPlanner.ShouldFireOnTurn(1));
            Assert.IsFalse(BalanceEventPlanner.ShouldFireOnTurn(2));
            Assert.IsTrue(BalanceEventPlanner.ShouldFireOnTurn(5));
            Assert.IsTrue(BalanceEventPlanner.ShouldFireOnTurn(9));
            Assert.AreEqual(BalanceEventPlanner.EventKind.None, BalanceEventPlanner.Plan(2, 0.2f, 1f).kind);
        }

        [Test]
        public void BalanceEvents_HelpTheTrailingSide_HinderTheLeader()
        {
            // Turn 1: help beat, rune flavor. Player trailing → buff lands on player approach.
            var help = BalanceEventPlanner.Plan(1, playerCoreFrac: 0.3f, enemyCoreFrac: 1f);
            Assert.AreEqual(BalanceEventPlanner.EventKind.BuffRune, help.kind);
            Assert.IsTrue(help.onPlayerSide);

            // Turn 5: hinder beat, rune flavor. Player trailing → hex lands on the LEADER (enemy).
            var hinder = BalanceEventPlanner.Plan(5, 0.3f, 1f);
            Assert.AreEqual(BalanceEventPlanner.EventKind.DebuffRune, hinder.kind);
            Assert.IsFalse(hinder.onPlayerSide);

            // Turn 9: help beat, gate flavor. Enemy trailing → power gate on enemy approach.
            var gate = BalanceEventPlanner.Plan(9, 1f, 0.3f);
            Assert.AreEqual(BalanceEventPlanner.EventKind.PowerGate, gate.kind);
            Assert.IsFalse(gate.onPlayerSide);

            // Turn 13: hinder beat, gate flavor. Enemy trailing → reduce gate on the LEADER (player).
            var reduce = BalanceEventPlanner.Plan(13, 1f, 0.3f);
            Assert.AreEqual(BalanceEventPlanner.EventKind.ReduceGate, reduce.kind);
            Assert.IsTrue(reduce.onPlayerSide);
        }

        [Test]
        public void BalanceEvents_NearParityYieldsNeutralGate()
        {
            var evt = BalanceEventPlanner.Plan(1, 0.9f, 0.85f);
            Assert.AreEqual(BalanceEventPlanner.EventKind.NeutralMultiplyGate, evt.kind);
        }

        // ---- §4 Chariot phases ----

        [Test]
        public void ChariotPhases_EscalateWithDamage()
        {
            float max = ChariotRules.MaxHP;
            Assert.AreEqual(ChariotRules.ChariotPhase.Patrol, ChariotRules.PhaseForHealth(max, max));
            Assert.AreEqual(ChariotRules.ChariotPhase.Patrol, ChariotRules.PhaseForHealth(max * 0.7f, max));
            Assert.AreEqual(ChariotRules.ChariotPhase.Frenzy, ChariotRules.PhaseForHealth(max * 0.5f, max));
            Assert.AreEqual(ChariotRules.ChariotPhase.Rampage, ChariotRules.PhaseForHealth(max * 0.2f, max));
            Assert.AreEqual(ChariotRules.ChariotPhase.Patrol, ChariotRules.PhaseForHealth(1f, 0f), "guard: no max HP");
        }

        [Test]
        public void ChariotPhases_SpeedAndSweep_GrowMonotonically()
        {
            Assert.Less(ChariotRules.SpeedFor(ChariotRules.ChariotPhase.Patrol), ChariotRules.SpeedFor(ChariotRules.ChariotPhase.Frenzy));
            Assert.Less(ChariotRules.SpeedFor(ChariotRules.ChariotPhase.Frenzy), ChariotRules.SpeedFor(ChariotRules.ChariotPhase.Rampage));
            Assert.Less(ChariotRules.SweepFor(ChariotRules.ChariotPhase.Patrol), ChariotRules.SweepFor(ChariotRules.ChariotPhase.Frenzy));
            Assert.Less(ChariotRules.SweepFor(ChariotRules.ChariotPhase.Frenzy), ChariotRules.SweepFor(ChariotRules.ChariotPhase.Rampage));
        }

        [Test]
        public void Chariot_RespawnDelay_IsFiveSeconds()
        {
            Assert.AreEqual(5f, ChariotRules.RespawnDelaySeconds);
        }

        // ---- §5 Launch gate art contract ----

        [Test]
        public void LaunchGateAnim_HasAtLeastFiveFrames()
        {
            var frames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.LaunchGateAnim);
            Assert.IsNotNull(frames, "launch gate frames must exist under Resources/Gimmicks/launch_gate_anim");
            Assert.GreaterOrEqual(frames.Length, 5, "portal animation needs at least 5 frames");
        }

        // ---- Targeting policy (gimmick-first, never the floor) ----

        [Test]
        public void Targeting_GroundTiles_AreNeverTargets()
        {
            Assert.IsTrue(TargetingRules.IsGroundTile(-0.5f), "top ground row is terrain");
            Assert.IsTrue(TargetingRules.IsGroundTile(-2.5f), "deep ground is terrain");
            Assert.IsFalse(TargetingRules.IsGroundTile(0.5f), "structures at y=0.5 are fair game");
            Assert.IsFalse(TargetingRules.IsGroundTile(3.5f));
        }

        [Test]
        public void Targeting_GimmicksOutrankUnitsOutrankPlainBlocks()
        {
            // Same distance: gimmick wins over unit wins over structure.
            float d = 5f;
            float gimmick = TargetingRules.Score(d, TargetingRules.GimmickWeight);
            float unit = TargetingRules.Score(d, TargetingRules.UnitWeight);
            float block = TargetingRules.Score(d, TargetingRules.StructureWeight);
            Assert.Less(gimmick, unit);
            Assert.Less(unit, block);

            // A gimmick moderately farther away still beats a nearby plain block…
            Assert.Less(TargetingRules.Score(8f, TargetingRules.GimmickWeight),
                        TargetingRules.Score(5f, TargetingRules.StructureWeight));
            // …but not one across the whole map.
            Assert.Greater(TargetingRules.Score(25f, TargetingRules.GimmickWeight),
                           TargetingRules.Score(5f, TargetingRules.StructureWeight));
        }

        [Test]
        public void Targeting_EnemyHalf_FollowsAttackerSide()
        {
            Assert.IsTrue(TargetingRules.OnEnemyHalf(6.5f, attackerIsPlayer: true));
            Assert.IsFalse(TargetingRules.OnEnemyHalf(-6.5f, attackerIsPlayer: true));
            Assert.IsTrue(TargetingRules.OnEnemyHalf(-6.5f, attackerIsPlayer: false));
            Assert.IsFalse(TargetingRules.OnEnemyHalf(0f, attackerIsPlayer: true), "bridge center is neutral");
        }

        // ---- Brick placement (pre-designated builds) ----

        [Test]
        public void BrickPlacement_RejectsLaunchRings_AndOutOfBand()
        {
            Assert.IsFalse(BrickPlacementRules.CanPlace(new Vector2(-14.5f, 0.5f)), "player muzzle = unit spawn area");
            Assert.IsFalse(BrickPlacementRules.CanPlace(new Vector2(14.5f, 0.5f)), "enemy muzzle = unit spawn area");
            Assert.IsFalse(BrickPlacementRules.CanPlace(new Vector2(-12f, 0.5f)), "inside ring radius");
            Assert.IsFalse(BrickPlacementRules.CanPlace(new Vector2(11.5f, 2f)), "beyond the keep band");
            Assert.IsFalse(BrickPlacementRules.CanPlace(new Vector2(0f, -1f)), "below the ground line");
            Assert.IsFalse(BrickPlacementRules.CanPlace(new Vector2(0f, 9f)), "above the build ceiling");
        }

        [Test]
        public void BrickPlacement_AcceptsMidfieldAndKeepApproaches()
        {
            Assert.IsTrue(BrickPlacementRules.CanPlace(new Vector2(0f, 0.5f)));
            Assert.IsTrue(BrickPlacementRules.CanPlace(new Vector2(-7.5f, 1.5f)));
            Assert.IsTrue(BrickPlacementRules.CanPlace(new Vector2(9f, 4f)));
        }

        [Test]
        public void BrickPlacement_PendingCap_IsTwo()
        {
            Assert.AreEqual(2, BrickPlacementRules.MaxPendingBricks);
        }

        [Test]
        public void BrickPlacement_RejectsIfOverlappingEnemyUnit()
        {
            // Arrange
            var enemyGo = new GameObject("TestEnemyUnit");
            enemyGo.transform.position = new Vector3(0f, 0.5f, 0f);
            var col = enemyGo.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            var unit = enemyGo.AddComponent<UnitController>();
            unit.isPlayerUnit = false; // enemy unit

            // Act & Assert
            // Position exactly at enemy position
            Assert.IsFalse(BrickPlacementRules.CanPlace(new Vector2(0f, 0.5f)), "cannot place block overlapping enemy unit");
            // Position far from enemy position
            Assert.IsTrue(BrickPlacementRules.CanPlace(new Vector2(3f, 0.5f)), "can place block far from enemy unit");

            // Clean up
            Object.DestroyImmediate(enemyGo);
        }

        [Test]
        public void BrickPlacement_IgnoresCollisionWithOverlappingEnemyUnitOnSpawn()
        {
            // Arrange
            var controllerGo = new GameObject("BrickPlacementController");
            var controller = controllerGo.AddComponent<BrickPlacementController>();
            
            var enemyGo = new GameObject("TestEnemyUnit");
            enemyGo.transform.position = new Vector3(2f, 0.5f, 0f);
            var enemyCol = enemyGo.AddComponent<BoxCollider2D>();
            enemyCol.size = new Vector2(1f, 1f);
            var enemyUnit = enemyGo.AddComponent<UnitController>();
            enemyUnit.isPlayerUnit = false;

            // Designate stone block
            controller.selectedBlockType = BrickPlacementController.SelectedBlockType.Stone;
            
            var ghostGo = new GameObject("Ghost");
            ghostGo.transform.position = new Vector3(2f, 0.5f, 0f); // directly overlapping
            var ghostInfo = ghostGo.AddComponent<PendingBrickInfo>();
            ghostInfo.blockType = BrickPlacementController.SelectedBlockType.Stone;
            
            var ghostsField = typeof(BrickPlacementController).GetField("ghosts", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var ghostsList = (System.Collections.Generic.List<GameObject>)ghostsField.GetValue(controller);
            ghostsList.Add(ghostGo);

            // Act - Trigger OnTurnChanged(true)
            controller.OnTurnChanged(true);

            // Find spawned PlayerBrick
            var brick = GameObject.Find("PlayerBrick");
            Assert.IsNotNull(brick, "brick must be spawned");
            var brickCol = brick.GetComponent<Collider2D>();
            Assert.IsNotNull(brickCol, "brick must have collider");

            // Assert that collision is ignored because they overlapped on spawn
            Assert.IsTrue(Physics2D.GetIgnoreCollision(brickCol, enemyCol), "collision must be ignored when overlapping on spawn");

            // Clean up
            Object.DestroyImmediate(controllerGo);
            Object.DestroyImmediate(enemyGo);
            if (brick != null) Object.DestroyImmediate(brick);
        }

        // ---- Arrow presentation scale (sprite-atlas remap ordering regression) ----

        [Test]
        public void ArrowController_ScalesToVisualLength_RegardlessOfSpriteAtlasRemap()
        {
            // Guards the fix in Awake(): the packed-atlas sprite must be assigned BEFORE
            // FitArrowToPlayableScale() runs, so the scale is computed against the sprite
            // that will actually render. Reversing the order (remap after scaling) let the
            // arrow render far smaller than visualLength while the collider still matched
            // the shrunken sprite — invisible in isolated collider checks, only visible as
            // "the arrow is tiny" in play.
            var go = new GameObject("TestArrow");
            go.AddComponent<Rigidbody2D>();
            var sr = go.AddComponent<SpriteRenderer>();
            // Any real sprite works: this pins that world size == visualLength regardless
            // of which sprite instance ends up assigned.
            var tex = new Texture2D(64, 64);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 64, 64), new Vector2(0.5f, 0.5f), 100f);
            go.AddComponent<BoxCollider2D>();
            var arrow = go.AddComponent<ArrowController>();

            // EditMode does not reliably run Awake() on AddComponent (project convention,
            // see other Awake-dependent tests in this file/GamePlayTests.cs) — invoke it
            // explicitly so FitArrowToPlayableScale() actually runs.
            var awakeMethod = typeof(ArrowController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(arrow, null);

            // Awake() already ran; localScale.x * native sprite width must equal
            // visualLength (not some fraction of it from a stale pre-remap bounds read).
            float actualWorldLength = go.transform.localScale.x * sr.sprite.bounds.size.x;
            Assert.AreEqual(arrow.visualLength, actualWorldLength, 0.01f,
                "arrow world length must match visualLength regardless of sprite remap timing");

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tex);
        }

        // ---- Flight patterns (flying war beast — never a fixed x-line) ----

        [Test]
        public void FlightPatterns_MoveInBothAxes_EveryPhase()
        {
            foreach (ChariotRules.ChariotPhase phase in System.Enum.GetValues(typeof(ChariotRules.ChariotPhase)))
            {
                float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
                for (float t = 0f; t < 20f; t += 0.25f)
                {
                    var p = FlightRules.FlightPoint(phase, t, 0f);
                    minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x);
                    minY = Mathf.Min(minY, p.y); maxY = Mathf.Max(maxY, p.y);
                }
                Assert.Greater(maxX - minX, 3f, $"{phase}: horizontal travel must be substantial");
                Assert.Greater(maxY - minY, 0.8f, $"{phase}: vertical travel must exist (no fixed x-axis line)");
            }
        }

        [Test]
        public void FlightPatterns_RampageDivesLowest()
        {
            float MinY(ChariotRules.ChariotPhase phase)
            {
                float min = float.MaxValue;
                for (float t = 0f; t < 20f; t += 0.2f) min = Mathf.Min(min, FlightRules.FlightPoint(phase, t, 0f).y);
                return min;
            }
            Assert.Less(MinY(ChariotRules.ChariotPhase.Rampage), MinY(ChariotRules.ChariotPhase.Patrol),
                "Rampage swoops must reach lower than Patrol glides");
            Assert.Less(MinY(ChariotRules.ChariotPhase.Rampage), FlightRules.BaseAltitude - FlightRules.DiveDepth + 0.5f,
                "Rampage must actually dive toward the deck");
        }

        // ---- Hero growth (loot content) ----

        [Test]
        public void HeroGrowth_StacksPerSide_AndCaps()
        {
            HeroGrowth.Reset();
            Assert.AreEqual(1f, HeroGrowth.DamageMult(true), 1e-5f);

            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(true, HeroItemType.Sword);
            Assert.AreEqual(1f + 2 * HeroGrowth.DamagePerSword, HeroGrowth.DamageMult(true), 1e-5f);
            Assert.AreEqual(1f, HeroGrowth.DamageMult(false), 1e-5f, "sides are independent");

            for (int i = 0; i < 10; i++) HeroGrowth.Grant(false, HeroItemType.Boots);
            Assert.AreEqual(HeroGrowth.MaxStacksPerType, HeroGrowth.Stacks(false, HeroItemType.Boots), "stacks cap");
            Assert.AreEqual(1f + HeroGrowth.MaxStacksPerType * HeroGrowth.SpeedPerBoots, HeroGrowth.SpeedMult(false), 1e-5f);

            HeroGrowth.Grant(true, HeroItemType.Shield);
            Assert.AreEqual(1f + HeroGrowth.HpPerShield, HeroGrowth.HpMult(true), 1e-5f);

            HeroGrowth.Reset();
            Assert.AreEqual(0, HeroGrowth.Stacks(true, HeroItemType.Sword), "reset clears everything");
        }

        [Test]
        public void ItemDrops_ChanceGate_AndTypeBuckets()
        {
            Assert.IsTrue(ItemDropRules.ShouldDrop(0.3f));
            Assert.IsFalse(ItemDropRules.ShouldDrop(0.9f));
            Assert.AreEqual(HeroItemType.Sword, ItemDropRules.TypeForRoll(0.1f));
            Assert.AreEqual(HeroItemType.Shield, ItemDropRules.TypeForRoll(0.5f));
            Assert.AreEqual(HeroItemType.Boots, ItemDropRules.TypeForRoll(0.9f));
        }

        [Test]
        public void BeastArt_FlyingFrames_HaveAtLeastFive()
        {
            var frames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.FlyingBeastAnim);
            Assert.IsNotNull(frames, "flying beast frames must exist under Resources/Gimmicks/flying_beast_anim");
            Assert.GreaterOrEqual(frames.Length, 5, "wing-flap cycle needs at least 5 frames");
        }

        [Test]
        public void ItemArt_AllThreeIcons_Exist()
        {
            Assert.IsNotNull(GimmickSpriteLibrary.Load("item_sword"));
            Assert.IsNotNull(GimmickSpriteLibrary.Load("item_shield"));
            Assert.IsNotNull(GimmickSpriteLibrary.Load("item_boots"));
        }
    }
}
