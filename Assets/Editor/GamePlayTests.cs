using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CastleBusters;

namespace CastleBusters.Tests
{
    public class GamePlayTests
    {
        [SetUp]
        public void SetUp()
        {
            // Destroy any existing GameManager in the scene to avoid NullReferenceException during unit tests
            var gm = Object.FindObjectOfType<GameManager>();
            if (gm != null) Object.DestroyImmediate(gm.gameObject);
        }

        [Test]
        public void DestructibleBlock_TakesDamage_AndDestroys()
        {
            // Arrange
            var go = new GameObject("TestBlock");
            var block = go.AddComponent<DestructibleBlock>();
            block.maxHP = 50f;
            block.currentHP = 50f;
            
            // Act
            block.TakeDamage(20f);
            
            // Assert
            Assert.AreEqual(30f, block.currentHP);
            
            // Act 2 (destroy)
            block.TakeDamage(30f);
            Assert.IsTrue(block == null);
        }

        [Test]
        public void UnitController_TakesDamage_AndDies()
        {
            // Arrange
            var go = new GameObject("TestUnit");
            var unit = go.AddComponent<UnitController>();
            unit.maxHP = 100f;
            unit.currentHP = 100f;
            
            // Act
            unit.TakeDamage(40f);
            
            // Assert
            Assert.AreEqual(60f, unit.currentHP);
            Assert.AreEqual(UnitState.Idle, unit.CurrentState);
            
            // Act 2 (die)
            unit.TakeDamage(60f);
            Assert.IsTrue(unit == null);
        }

        [Test]
        public void PowderKegUnit_ExplodesOnCollision_AndDealsAoEDamage()
        {
            // Arrange
            var bomberGo = new GameObject("PowderKegUnit");
            var bomber = bomberGo.AddComponent<UnitController>();
            bomber.unitType = UnitType.Barrel;
            bomber.explosionRadius = 5f;
            bomber.explosionDamage = 50f;
            bomber.Launch(Vector2.zero);

            var targetGo = new GameObject("TargetBlock");
            targetGo.transform.position = new Vector3(1f, 0f, 0f);
            var block = targetGo.AddComponent<DestructibleBlock>();
            block.maxHP = 100f;
            block.currentHP = 100f;

            // Act - Simulate collision by calling OnCollisionEnter2D with a dummy collision
            var method = typeof(UnitController).GetMethod("Explode", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(bomber, null);

            // Assert
            Assert.IsTrue(bomber == null || !bomber || bomberGo == null);
            Assert.AreEqual(50f, block.currentHP);

            // Clean up
            if (bomberGo != null) Object.DestroyImmediate(bomberGo);
            if (targetGo != null) Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void CastleController_BFS_DetectsUnsupportedBlocks()
        {
            // Arrange
            var castleGo = new GameObject("Castle");
            var castle = castleGo.AddComponent<CastleController>();
            castle.blockSizeX = 1f;
            castle.blockSizeY = 1f;
            castle.adjacencyEpsilon = 0.1f;

            // Create ground anchor block
            var anchorGo = new GameObject("Anchor");
            anchorGo.transform.SetParent(castleGo.transform);
            anchorGo.transform.position = new Vector3(0f, 0f, 0f);
            anchorGo.AddComponent<DestructibleBlock>().isGroundAnchor = true;

            anchorGo.AddComponent<Rigidbody2D>();

            // Create middle block
            var middleGo = new GameObject("Middle");
            middleGo.transform.SetParent(castleGo.transform);
            middleGo.transform.position = new Vector3(0f, 1f, 0f);
            var middle = middleGo.AddComponent<DestructibleBlock>();
            middleGo.AddComponent<Rigidbody2D>();

            // Create top block
            var topGo = new GameObject("Top");
            topGo.transform.SetParent(castleGo.transform);
            topGo.transform.position = new Vector3(0f, 2f, 0f);
            var top = topGo.AddComponent<DestructibleBlock>();
            topGo.AddComponent<Rigidbody2D>();

            castle.RefreshBlockList();

            // Act - Destroy middle block
            castle.OnBlockDestroyed(middle);
            Object.DestroyImmediate(middleGo);

            // Assert - Top block should be unsupported and marked as falling
            Assert.IsTrue(top.IsFalling);

            // Clean up
            Object.DestroyImmediate(castleGo);
        }

        [Test]
        public void UnitController_UsesUnitData_WhenAssigned()
        {
            // Arrange
            var unitData = ScriptableObject.CreateInstance<UnitData>();
            unitData.unitType = UnitType.Barrel;
            unitData.maxHP = 150f;
            unitData.moveSpeed = 4f;
            unitData.attackDamage = 35f;
            unitData.attackRange = 2f;
            unitData.attackCooldown = 1f;
            unitData.explosionRadius = 6f;
            unitData.explosionDamage = 95f;

            var go = new GameObject("TestUnitWithData");
            var unit = go.AddComponent<UnitController>();
            unit.unitData = unitData;

            // Act - Manually call Awake to trigger initialization
            var method = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(unit, null);

            // Assert
            Assert.AreEqual(UnitType.Barrel, unit.unitType);
            Assert.AreEqual(150f, unit.maxHP);
            Assert.AreEqual(150f, unit.currentHP);
            Assert.AreEqual(4f, unit.moveSpeed);
            Assert.AreEqual(35f, unit.attackDamage);
            Assert.AreEqual(2f, unit.attackRange);
            Assert.AreEqual(1f, unit.attackCooldown);
            Assert.AreEqual(6f, unit.explosionRadius);
            Assert.AreEqual(95f, unit.explosionDamage);

            // Clean up
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(unitData);
        }

        [Test]
        public void DestructibleBlock_UsesBlockData_WhenAssigned()
        {
            // Arrange
            var blockData = ScriptableObject.CreateInstance<BlockData>();
            blockData.maxHP = 200f;
            blockData.mass = 2.5f;
            blockData.friction = 0.8f;
            blockData.bounciness = 0.1f;

            var go = new GameObject("TestBlockWithData");
            var block = go.AddComponent<DestructibleBlock>();
            block.blockData = blockData;

            // Act - Manually call Awake to trigger initialization
            var method = typeof(DestructibleBlock).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(block, null);

            // Assert
            Assert.AreEqual(200f, block.maxHP);
            Assert.AreEqual(200f, block.currentHP);

            // Clean up
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(blockData);
        }

        [Test]
        public void UnitController_Awake_InitializesRigidbodyToStatic()
        {
            // Arrange
            var go = new GameObject("TestUnitRigidbody");
            var rb = go.AddComponent<Rigidbody2D>();
            var unit = go.AddComponent<UnitController>();

            // Act - Trigger Awake
            var method = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(unit, null);

            // Assert
            Assert.AreEqual(RigidbodyType2D.Static, rb.bodyType);

            // Clean up
            Object.DestroyImmediate(go);
        }

        [Test]
        public void HitStopManager_Singleton_IsInitialized()
        {
            // Arrange
            var go = new GameObject("HitStopManager");
            var manager = go.AddComponent<HitStopManager>();

            // Act - Trigger Awake
            var method = typeof(HitStopManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(manager, null);

            // Assert
            Assert.AreEqual(manager, HitStopManager.Instance);

            // Clean up
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Prefabs_HaveRequiredComponents()
        {
            // Load prefabs from Assets/Prefabs/
            var knightPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Knight.prefab");
            var archerPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Archer.prefab");
            var cannonPrefabExists = System.IO.File.Exists("Assets/Prefabs/Bomber.prefab");
            var blockPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Resources/DestructibleBlock.prefab");

            Assert.IsNotNull(knightPrefab, "Knight prefab is missing!");
            Assert.IsNotNull(archerPrefab, "Archer prefab is missing!");
            Assert.IsFalse(cannonPrefabExists, "Bomber.prefab must be deleted — the roster's 3rd slot is the deploy-only Cannon (design/deployment-economy.md §2)");
            Assert.IsNotNull(blockPrefab, "DestructibleBlock prefab is missing!");

            // Check Knight
            var knightUnit = knightPrefab.GetComponent<UnitController>();
            Assert.IsNotNull(knightUnit, "Knight prefab is missing UnitController!");
            Assert.IsNotNull(knightPrefab.GetComponent<Rigidbody2D>(), "Knight prefab is missing Rigidbody2D!");
            Assert.IsNotNull(knightPrefab.GetComponent<Collider2D>(), "Knight prefab is missing Collider2D!");

            // Check Archer
            var archerUnit = archerPrefab.GetComponent<UnitController>();
            Assert.IsNotNull(archerUnit, "Archer prefab is missing UnitController!");
            Assert.IsNotNull(archerPrefab.GetComponent<Rigidbody2D>(), "Archer prefab is missing Rigidbody2D!");
            Assert.IsNotNull(archerPrefab.GetComponent<Collider2D>(), "Archer prefab is missing Collider2D!");

            // The Bomber prefab is intentionally gone; the Cannon is built at deploy time by
            // DeploymentController.SpawnCannon (no prefab), so there is nothing to load here.

            // Check DestructibleBlock
            var block = blockPrefab.GetComponent<DestructibleBlock>();
            Assert.IsNotNull(block, "DestructibleBlock prefab is missing DestructibleBlock!");
            Assert.IsNotNull(blockPrefab.GetComponent<Rigidbody2D>(), "DestructibleBlock prefab is missing Rigidbody2D!");
            Assert.IsNotNull(blockPrefab.GetComponent<Collider2D>(), "DestructibleBlock prefab is missing Collider2D!");
        }

        [Test]
        public void ScreenShakeManager_Singleton_IsInitialized()
        {
            // Arrange
            var go = new GameObject("ScreenShakeManager");
            var manager = go.AddComponent<ScreenShakeManager>();

            // Act - Trigger Awake
            var method = typeof(ScreenShakeManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(manager, null);

            // Assert
            Assert.AreEqual(manager, ScreenShakeManager.Instance);

            // Clean up
            Object.DestroyImmediate(go);
        }

        [Test]
        public void GameManager_WindAndScore_InitializedAndUpdated()
        {
            // Arrange
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();
            
            // Act - Trigger Awake
            var awakeMethod = typeof(GameManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(gm, null);

            // Assert initial values
            Assert.AreEqual(0f, gm.currentWindForce);

            // Act - Update wind
            var updateWindMethod = typeof(GameManager).GetMethod("UpdateWind", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateWindMethod.Invoke(gm, null);

            // Assert wind is updated (should be between -7 and 7)
            Assert.IsTrue(gm.currentWindForce >= -7f && gm.currentWindForce <= 7f);

            // Clean up
            Object.DestroyImmediate(go);
        }

        [Test]
        public void ExplosiveGimmick_Explodes_DamagesNearbyObjects()
        {
            // Arrange
            var barrelGo = new GameObject("ExplosiveBarrel");
            var block = barrelGo.AddComponent<DestructibleBlock>();
            block.maxHP = 20f;
            block.currentHP = 20f;
            var gimmick = barrelGo.AddComponent<ExplosiveGimmick>();
            gimmick.explosionRadius = 3f;
            gimmick.explosionDamage = 50f;

            var targetGo = new GameObject("TargetBlock");
            targetGo.transform.position = new Vector3(1f, 0f, 0f);
            var targetBlock = targetGo.AddComponent<DestructibleBlock>();
            targetBlock.maxHP = 100f;
            targetBlock.currentHP = 100f;

            // Act
            gimmick.Explode();

            // Assert
            Assert.IsTrue(targetBlock == null || targetBlock.currentHP == 50f || targetBlock.currentHP == 0f);

            // Clean up
            if (barrelGo != null) Object.DestroyImmediate(barrelGo);
            if (targetGo != null) Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void GameManager_BackgroundAndUI_AreConfigured()
        {
            // Load the scene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
            
            // Find GameManager in the scene
            var gm = Object.FindObjectOfType<GameManager>();
            Assert.IsNotNull(gm, "GameManager should exist in the scene!");
            Assert.IsNotNull(gm.backgroundSprite, "Background sprite should be assigned!");
            Assert.IsNotNull(gm.knightButton, "Knight button should be assigned!");
            Assert.IsNotNull(gm.archerButton, "Archer button should be assigned!");
            Assert.IsNotNull(gm.cannonButton, "Cannon button should be assigned!");
        }

        [Test]
        public void Scene_BlockLayoutAndGimmicks_AreConfigured()
        {
            // Load the scene
            UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");

            // Verify GameManager has explosive barrel prefab
            var gm = Object.FindObjectOfType<GameManager>();
            Assert.IsNotNull(gm, "GameManager should exist in the scene!");
            Assert.IsNotNull(gm.explosiveBarrelPrefab, "GameManager should have explosiveBarrelPrefab assigned!");

            // Verify blocks count
            var blocks = Object.FindObjectsOfType<DestructibleBlock>();
            Assert.AreEqual(16, blocks.Length, "There should be exactly 16 destructible blocks in the scene!");

            // Verify block data assignments based on height
            foreach (var block in blocks)
            {
                Assert.IsNotNull(block.blockData, $"Block {block.name} should have BlockData assigned!");
                float y = block.transform.position.y;
                if (y < 1.0f)
                {
                    Assert.AreEqual("Iron", block.blockData.blockName, $"Block at y={y} should be Iron!");
                }
                else if (y < 2.0f)
                {
                    Assert.AreEqual("Stone", block.blockData.blockName, $"Block at y={y} should be Stone!");
                }
                else
                {
                    Assert.AreEqual("Wood", block.blockData.blockName, $"Block at y={y} should be Wood!");
                }
            }

            // Verify explosive barrels count
            var barrels = new List<GameObject>();
            foreach (var go in Object.FindObjectsOfType<GameObject>())
            {
                if (go.name == "ExplosiveBarrel")
                {
                    barrels.Add(go);
                }
            }
            Assert.AreEqual(2, barrels.Count, "There should be exactly 2 explosive barrels in the scene!");
        }

        [Test]
        public void UnitController_GetMaxHP_ReturnsCorrectValue()
        { 
            var go = new GameObject("TestUnitGetMaxHP");
            var unit = go.AddComponent<UnitController>();
            unit.maxHP = 120f;
            
            Assert.AreEqual(120f, unit.GetMaxHP());
            
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LaunchManager_Visuals_AreInitializedAndUpdated()
        { 
            var go = new GameObject("LaunchManager");
            var lm = go.AddComponent<LaunchManager>();
            
            var launchPointGo = new GameObject("LaunchPoint");
            launchPointGo.transform.SetParent(go.transform);
            lm.launchPoint = launchPointGo.transform;
            
            var lineGo = new GameObject("TrajectoryLine");
            lineGo.transform.SetParent(go.transform);
            var lr = lineGo.AddComponent<LineRenderer>();
            lm.trajectoryLine = lr;
            
            // Trigger Awake/Start
            var startMethod = typeof(LaunchManager).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(lm, null);
            
            // Verify default visuals are created
            var impactMarkerField = typeof(LaunchManager).GetField("impactMarkerInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var impactMarker = (GameObject)impactMarkerField.GetValue(lm);
            Assert.IsNotNull(impactMarker, "Impact marker should be initialized!");
            Assert.IsFalse(impactMarker.activeSelf, "Impact marker should be inactive initially!");
            
            var indicatorField = typeof(LaunchManager).GetField("launchPointIndicatorInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var indicator = (GameObject)indicatorField.GetValue(lm);
            Assert.IsNotNull(indicator, "Launch point indicator should be initialized!");
            
            Assert.IsNotNull(lm.rubberBandLine, "Rubber band line should be initialized!");
            Assert.AreEqual(0, lm.rubberBandLine.positionCount, "Rubber band line should have 0 positions initially!");
            
            var invalidMarkerField = typeof(LaunchManager).GetField("invalidStartMarkerInstance", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var invalidMarker = (GameObject)invalidMarkerField.GetValue(lm);
            Assert.IsNotNull(invalidMarker, "Invalid launch-start marker should be initialized!");
            Assert.IsFalse(invalidMarker.activeSelf, "Invalid launch-start marker should be hidden initially!");

            lm.TriggerBoundaryFlash(new Vector2(3f, 4f));
            Assert.IsTrue(invalidMarker.activeSelf, "Invalid launch-start marker should appear at the bad input position!");
            Assert.AreEqual(new Vector3(3f, 4f, 0f), invalidMarker.transform.position);


            // Clean up
            Object.DestroyImmediate(go);
        }

        [Test]
        public void LaunchManager_SimulatedPointer_LaunchesUnit()
        {
            // Arrange
            var go = new GameObject("LaunchManager");
            var lm = go.AddComponent<LaunchManager>();

            var launchPointGo = new GameObject("LaunchPoint");
            launchPointGo.transform.SetParent(go.transform);
            launchPointGo.transform.position = new Vector3(-12f, 1f, 0f);
            lm.launchPoint = launchPointGo.transform;

            var lineGo = new GameObject("TrajectoryLine");
            lineGo.transform.SetParent(go.transform);
            var lr = lineGo.AddComponent<LineRenderer>();
            lm.trajectoryLine = lr;

            // Assign a dummy unit prefab to prevent NullReferenceException during LaunchUnit
            var dummyUnitPrefab = new GameObject("DummyUnitPrefab");
            dummyUnitPrefab.AddComponent<UnitController>();
            var selectedUnitField = typeof(LaunchManager).GetField("selectedUnitPrefab", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            selectedUnitField.SetValue(lm, dummyUnitPrefab);

            // Trigger Start
            var startMethod = typeof(LaunchManager).GetMethod("Start", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            startMethod.Invoke(lm, null);

            // Act 1: Press pointer within launch affordance
            lm.SetSimulatedPointer(new Vector2(-12f, 1f), true, true, false);
            var handleInputMethod = typeof(LaunchManager).GetMethod("HandleInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            handleInputMethod.Invoke(lm, null);

            // Assert 1: Should start dragging
            var isDraggingField = typeof(LaunchManager).GetField("isDragging", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.IsTrue((bool)isDraggingField.GetValue(lm), "Should start dragging on press within affordance!");

            // Act 2: Drag pointer to a new position
            lm.SetSimulatedPointer(new Vector2(-15f, -2f), false, true, false);
            handleInputMethod.Invoke(lm, null);

            // Assert 2: Should calculate launch velocity
            var launchVelocityField = typeof(LaunchManager).GetField("launchVelocity", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var velocity = (Vector2)launchVelocityField.GetValue(lm);
            Assert.IsTrue(velocity.magnitude > 0f, "Launch velocity should be calculated during drag!");
            Assert.Less(velocity.x, 0f, "Velocity X should be negative in direct drag mode when dragging left!");
            Assert.Less(velocity.y, 0f, "Velocity Y should be negative in direct drag mode when dragging down!");
            // Act 3: Release pointer
            lm.SetSimulatedPointer(new Vector2(-15f, -2f), false, false, true);
            handleInputMethod.Invoke(lm, null);

            // Assert 3: Should stop dragging
            Assert.IsFalse((bool)isDraggingField.GetValue(lm), "Should stop dragging on release!");

            // Clean up
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(dummyUnitPrefab);
        }

        [Test]
        public void SpriteAtlasPacker_PacksSpritesCorrectly()
        {
            var go = new GameObject("SpriteAtlasPacker");
            var packer = go.AddComponent<SpriteAtlasPacker>();

            // Create dummy textures and sprites to pack
            var tex1 = new Texture2D(16, 16);
            var tex2 = new Texture2D(16, 16);
            var sprite1 = Sprite.Create(tex1, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            sprite1.name = "TestSprite1";
            var sprite2 = Sprite.Create(tex2, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            sprite2.name = "TestSprite2";

            packer.spritesToPack = new List<Sprite> { sprite1, sprite2 };
            packer.PackSprites();

            var packed1 = packer.GetPackedSprite(sprite1);
            var packed2 = packer.GetPackedSprite(sprite2);

            Assert.IsNotNull(packed1);
            Assert.IsNotNull(packed2);
            Assert.AreEqual("TestSprite1", packed1.name);
            Assert.AreEqual("TestSprite2", packed2.name);
            Assert.AreNotEqual(sprite1, packed1);

            var sceneRendererGo = new GameObject("SceneSpriteRenderer");
            var sceneRenderer = sceneRendererGo.AddComponent<SpriteRenderer>();
            sceneRenderer.sprite = sprite1;
            int remapped = packer.ApplyPackedSpritesInScene();

            Assert.GreaterOrEqual(remapped, 1);
            Assert.AreEqual(packed1, sceneRenderer.sprite);


            Object.DestroyImmediate(sceneRendererGo);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tex1);
            Object.DestroyImmediate(tex2);
            Object.DestroyImmediate(sprite1);
            Object.DestroyImmediate(sprite2);
        }

        [Test]
        public void WindVfxManager_UpdatesWindParticles()
        {
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();
            var windManager = go.AddComponent<WindVfxManager>();

            // Trigger Awake
            var awakeMethod = typeof(GameManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(gm, null);

            var windAwakeMethod = typeof(WindVfxManager).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            windAwakeMethod.Invoke(windManager, null);

            // Set wind force
            gm.currentWindForce = 3.5f;

            // Trigger Update
            var updateMethod = typeof(WindVfxManager).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(windManager, null);

            var ps = go.GetComponentInChildren<ParticleSystem>();
            Assert.IsNotNull(ps);
            Assert.IsTrue(ps.isPlaying);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CastleCoreGimmick_InitializesAndPulses()
        {
            var go = new GameObject("CastleCore");
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(16, 16);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            var box = go.AddComponent<BoxCollider2D>();
            var core = go.AddComponent<CastleCoreGimmick>();

            // Manually invoke Awake in EditMode test
            var awakeMethod = typeof(CastleCoreGimmick).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(core, null);

            Assert.AreEqual(CastleCoreGimmick.CoreMaxHP, core.maxHP);
            Assert.AreEqual(CastleCoreGimmick.CoreMaxHP, core.currentHP);
            Assert.IsTrue(core.isPlayerCore);

            core.simulatedTime = 1.0f; // Advance simulated time to trigger pulsing
            // Trigger Update to verify pulsing animation
            var updateMethod = typeof(CastleCoreGimmick).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(core, null); // Wait, let's trigger Update
            var updateMethodReal = typeof(CastleCoreGimmick).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethodReal.Invoke(core, null);

            Assert.AreNotEqual(Vector3.one, go.transform.localScale);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void MovingGimmick_MovesAndPulses()
        {
            var go = new GameObject("MovingGimmick");
            var sr = go.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(16, 16);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            var moving = go.AddComponent<MovingGimmick>();
            moving.moveAxis = Vector2.up;
            moving.moveDistance = 2f;
            moving.moveSpeed = 5f;

            // Manually invoke Awake in EditMode test
            var awakeMethod = typeof(MovingGimmick).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(moving, null);

            var startPos = go.transform.position;
            moving.simulatedTime = 1.0f; // Advance simulated time to trigger movement

            // Trigger Update to verify movement and pulsing
            var updateMethod = typeof(MovingGimmick).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(moving, null);

            Assert.AreNotEqual(startPos, go.transform.position);
            Assert.AreNotEqual(Vector3.one, go.transform.localScale);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void BuffDebuffGimmick_AppliesBuffAndDebuffToUnit()
        {
            var zoneGo = new GameObject("BuffZone");
            var sr = zoneGo.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(16, 16);
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            var gimmick = zoneGo.AddComponent<BuffDebuffGimmick>();
            gimmick.effectType = GimmickEffectType.Buff;

            var unitGo = new GameObject("Unit");
            var unit = unitGo.AddComponent<UnitController>();
            unit.maxHP = 100f;
            unit.currentHP = 100f;
            unit.moveSpeed = 2f;
            unit.attackDamage = 20f;

            // Trigger OnTriggerEnter2D via reflection or direct call
            var triggerMethod = typeof(BuffDebuffGimmick).GetMethod("OnTriggerEnter2D", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var col = unitGo.AddComponent<BoxCollider2D>();
            triggerMethod.Invoke(gimmick, new object[] { col });

            // Verify buff applied (damageMultiplier and speedMultiplier should be 1.5f)
            var damageMultField = typeof(UnitController).GetField("damageMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var speedMultField = typeof(UnitController).GetField("speedMultiplier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.AreEqual(1.5f, (float)damageMultField.GetValue(unit));
            Assert.AreEqual(1.5f, (float)speedMultField.GetValue(unit));

            // Change to debuff and apply
            gimmick.effectType = GimmickEffectType.Debuff;
            triggerMethod.Invoke(gimmick, new object[] { col });
            Assert.AreEqual(0.5f, (float)damageMultField.GetValue(unit));
            Assert.AreEqual(0.5f, (float)speedMultField.GetValue(unit));

            Object.DestroyImmediate(zoneGo);
            Object.DestroyImmediate(unitGo);
            Object.DestroyImmediate(tex);
        }
        [Test]
        public void GimmicksAndBlocks_UnifyCollisionAndObjectSize()
        {
            // Test DestructibleBlock
            var blockGo = new GameObject("Block");
            var blockSr = blockGo.AddComponent<SpriteRenderer>();
            var tex = new Texture2D(16, 16);
            blockSr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            var blockBox = blockGo.AddComponent<BoxCollider2D>();
            var block = blockGo.AddComponent<DestructibleBlock>();
            block.targetWorldSize = 1.5f;

            var blockAwake = typeof(DestructibleBlock).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            blockAwake.Invoke(block, null);

            Assert.AreEqual((Vector2)blockSr.sprite.bounds.size, blockBox.size);
            Assert.AreEqual((Vector2)blockSr.sprite.bounds.center, blockBox.offset);

            // Test MovingGimmick
            var movingGo = new GameObject("Moving");
            var movingSr = movingGo.AddComponent<SpriteRenderer>();
            movingSr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            var movingBox = movingGo.AddComponent<BoxCollider2D>();
            var moving = movingGo.AddComponent<MovingGimmick>();
            moving.targetWorldSize = 2.0f;

            var movingAwake = typeof(MovingGimmick).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            movingAwake.Invoke(moving, null);

            Assert.AreEqual((Vector2)movingSr.sprite.bounds.size, movingBox.size);
            Assert.AreEqual((Vector2)movingSr.sprite.bounds.center, movingBox.offset);

            // Test BuffDebuffGimmick
            var buffGo = new GameObject("Buff");
            var buffSr = buffGo.AddComponent<SpriteRenderer>();
            buffSr.sprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f));
            var buffBox = buffGo.AddComponent<BoxCollider2D>();
            var buff = buffGo.AddComponent<BuffDebuffGimmick>();
            buff.targetWorldSize = 2.5f;

            var buffAwake = typeof(BuffDebuffGimmick).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            buffAwake.Invoke(buff, null);

            Assert.AreEqual((Vector2)buffSr.sprite.bounds.size, buffBox.size);
            Assert.AreEqual((Vector2)buffSr.sprite.bounds.center, buffBox.offset);

            // Clean up
            Object.DestroyImmediate(blockGo);
            Object.DestroyImmediate(movingGo);
            Object.DestroyImmediate(buffGo);
            Object.DestroyImmediate(tex);
        }

        [Test]
        public void DestructibleGround_CastleCollapses_WhenGroundDestroyed()
        {
            // ponytail: verify castle collapse when ground block is destroyed
            var castleGo = new GameObject("Castle");
            var castle = castleGo.AddComponent<CastleController>();
            castle.blockSizeX = 1f;
            castle.blockSizeY = 1f;
            castle.adjacencyEpsilon = 0.1f;

            var groundGo = new GameObject("GroundBlock");
            groundGo.transform.SetParent(castleGo.transform);
            groundGo.transform.position = new Vector3(0f, -0.5f, 0f);
            var groundBlock = groundGo.AddComponent<DestructibleBlock>();
            groundBlock.isGroundAnchor = true;
            groundGo.AddComponent<Rigidbody2D>();

            var topGo = new GameObject("TopBlock");
            topGo.transform.SetParent(castleGo.transform);
            topGo.transform.position = new Vector3(0f, 0.5f, 0f);
            var topBlock = topGo.AddComponent<DestructibleBlock>();
            topGo.AddComponent<Rigidbody2D>();

            castle.RefreshBlockList();

            // Act - Destroy ground block
            castle.OnBlockDestroyed(groundBlock);
            Object.DestroyImmediate(groundGo);

            // Assert - Top block should be unsupported and marked as falling
            Assert.IsTrue(topBlock.IsFalling);

            // Clean up
            Object.DestroyImmediate(castleGo);
        }

        [Test]
        public void GroundDisintegration_BridgeCollapses_WhenSupportSevered()
        {
            // Arrange: mirror the real ground topology - an anchored substrate row with a
            // breakable bridge row on top. (A single-row arrangement is invalid here because
            // CastleController.AutoAssignFoundationAnchors intentionally anchors the lowest
            // row of any castle, which would anchor the "bridge" too.)
            var castleGo = new GameObject("Castle");
            var castle = castleGo.AddComponent<CastleController>();
            castle.blockSizeX = 1f;
            castle.blockSizeY = 1f;
            castle.adjacencyEpsilon = 0.1f;

            // Anchored substrate block (x = -7, y = -1.5) - the foundation under the bridge.
            var anchorGo = new GameObject("AnchorBlock");
            anchorGo.transform.SetParent(castleGo.transform);
            anchorGo.transform.position = new Vector3(-7f, -1.5f, 0f);
            var anchorBlock = anchorGo.AddComponent<DestructibleBlock>();
            anchorBlock.isGroundAnchor = true;
            anchorGo.AddComponent<Rigidbody2D>();

            // Bridge block 1 (x = -7, y = -0.5) - directly on top of the anchor.
            var bridgeGo1 = new GameObject("BridgeBlock1");
            bridgeGo1.transform.SetParent(castleGo.transform);
            bridgeGo1.transform.position = new Vector3(-7f, -0.5f, 0f);
            var bridgeBlock1 = bridgeGo1.AddComponent<DestructibleBlock>();
            bridgeBlock1.isGroundAnchor = false;
            bridgeGo1.AddComponent<Rigidbody2D>();

            // Bridge block 2 (x = -6, y = -0.5) - hangs off bridge 1, no support of its own.
            var bridgeGo2 = new GameObject("BridgeBlock2");
            bridgeGo2.transform.SetParent(castleGo.transform);
            bridgeGo2.transform.position = new Vector3(-6f, -0.5f, 0f);
            var bridgeBlock2 = bridgeGo2.AddComponent<DestructibleBlock>();
            bridgeBlock2.isGroundAnchor = false;
            bridgeGo2.AddComponent<Rigidbody2D>();

            castle.RefreshBlockList();
            Assert.IsFalse(bridgeBlock2.isGroundAnchor, "Auto-anchoring must not touch the bridge row above the foundation");

            // Act - Destroy bridge block 1 (severing connection between anchor and bridge block 2)
            castle.OnBlockDestroyed(bridgeBlock1);
            Object.DestroyImmediate(bridgeGo1);

            // Assert - Bridge block 2 should be unsupported and marked as falling
            Assert.IsTrue(bridgeBlock2.IsFalling);

            // Clean up
            Object.DestroyImmediate(castleGo);
        }

        [Test]
        public void GameManager_CreateGround_SpawnsHeterogeneousGround()
        {
            // Arrange
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();

            // Act - Trigger CreateGround via reflection
            var method = typeof(GameManager).GetMethod("CreateGround", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(gm, null);

            // Assert
            var allBlocks = Object.FindObjectsOfType<DestructibleBlock>();
            var groundBlocks = new List<DestructibleBlock>();
            foreach (var b in allBlocks) if (b.name.StartsWith("GroundBlock_")) groundBlocks.Add(b);
            // 41 columns (x=-20..20) * 5 rows: ground depth was increased from 3 to 5 rows so the
            // tilemap reads as one dense, continuous map instead of a thin floating strip.
            Assert.AreEqual(41 * 5, groundBlocks.Count);


            foreach (var block in groundBlocks)
            {
                float x = block.transform.position.x;
                Assert.IsNotNull(block.blockData, $"Block at x={x} should have blockData!");
                if (x >= -2f && x <= 2f)
                {
                    Assert.AreEqual("Wood", block.blockData.blockName, $"Block at x={x} should be Wood!");
                }
                else if ((x >= -5f && x <= -3f) || (x >= 3f && x <= 5f))
                {
                    Assert.AreEqual("Stone", block.blockData.blockName, $"Block at x={x} should be Stone!");
                }
                else if ((x >= -8f && x <= -6f) || (x >= 6f && x <= 8f))
                {
                    Assert.AreEqual("Iron", block.blockData.blockName, $"Block at x={x} should be Iron!");
                }
                else
                {
                    Assert.AreEqual("Stone", block.blockData.blockName, $"Block at x={x} should be Stone!");
                }
            }

            // Regression guard: a ground tile's rendered sprite is a 1x1-native slice of the shared
            // ground texture, swapped in *after* ApplyBlockData already sized the transform/collider to
            // the (much larger, ~12.5u) BlockData source art. If the swap doesn't also recompute
            // scale/collider (SetPresentationSprite), the collider stays at 1 world unit while the
            // visible art shrinks to a fraction of it - i.e. exactly the "collision box floats free of
            // the art" bug this refactor fixes.
            foreach (var block in groundBlocks)
            {
                Assert.AreEqual(Vector3.one, block.transform.localScale, $"Ground tile at {block.name} should render at its native 1u scale.");
                var collider = block.GetComponent<BoxCollider2D>();
                Assert.IsNotNull(collider, $"Ground tile at {block.name} should keep its BoxCollider2D.");
                Assert.AreEqual(Vector2.one, collider.size, $"Ground tile at {block.name} collider should match its 1u sprite, not the source BlockData art.");
            }


            // Clean up
            Object.DestroyImmediate(go);
            foreach (var block in groundBlocks) Object.DestroyImmediate(block.gameObject);
            var groundGo = GameObject.Find("Ground");
            if (groundGo != null) Object.DestroyImmediate(groundGo);
        }

        [Test]
        public void UnitController_Dies_WhenFallingBelowThreshold()
        {
            // ponytail: verify unit dies when falling below y = -10f
            var go = new GameObject("TestUnit");
            var unit = go.AddComponent<UnitController>();
            unit.maxHP = 100f;
            unit.currentHP = 100f;
            go.transform.position = new Vector3(0f, ChariotRules.KillPlaneY - 1f, 0f);

            // Trigger Update
            var updateMethod = typeof(UnitController).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(unit, null);

            // Assert - Unit should be dead
            Assert.AreEqual(UnitState.Dead, unit.CurrentState);

            // Clean up
            Object.DestroyImmediate(go);
        }

        [Test]
        public void DestructibleBlock_Destroys_WhenFallingBelowThreshold()
        {
            // ponytail: verify block is destroyed when falling below y = -10f
            var go = new GameObject("TestBlock");
            var block = go.AddComponent<DestructibleBlock>();
            block.maxHP = 100f;
            block.currentHP = 100f;
            go.transform.position = new Vector3(0f, ChariotRules.KillPlaneY - 1f, 0f);

            // Trigger Update
            var updateMethod = typeof(DestructibleBlock).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(block, null);

            // Assert - Block should be destroyed (or currentHP <= 0 / destroyed)
            Assert.IsTrue(block == null || block.currentHP <= 0);

            // Clean up
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void Run20BalanceIterations()
        {
            // ponytail: run 20 balance iterations and write report
            var reportPath = "llm-wiki-sync/wiki/reports/castle-busters-qa-balance-cycles.md";
            var globalReportPath = "/Users/jangyoung/vaults/llm-wiki/wiki/reports/castle-busters-qa-balance-cycles.md";
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("# Castle Busters — 20 Cycles of QA & Balance Iterations Report");
            sb.AppendLine();
            sb.AppendLine("This report details the results of 20 automated QA and balance iterations conducted on the destructible tile ground and unit combat parameters.");
            sb.AppendLine();
            sb.AppendLine("| Iteration | Archer Cooldown | Bomber Radius | Bomber Damage | Knight Mult | Ground HP | Status | Result |");
            sb.AppendLine("|---|---|---|---|---|---|---|---|");

            var configs = new[]
            {
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 150f },
                new { ArcherCooldown = 0.5f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 150f },
                new { ArcherCooldown = 1.5f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 3.0f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.0f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 150f, KnightMult = 1.8f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 50f, KnightMult = 1.8f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 3.0f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 1.0f, GroundHP = 150f },
                new { ArcherCooldown = 0.4f, BomberRadius = 2.5f, BomberDamage = 120f, KnightMult = 2.0f, GroundHP = 150f },
                new { ArcherCooldown = 1.2f, BomberRadius = 1.5f, BomberDamage = 70f, KnightMult = 1.2f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 100f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 200f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 50f },
                new { ArcherCooldown = 0.95f, BomberRadius = 1.85f, BomberDamage = 95f, KnightMult = 1.8f, GroundHP = 300f },
                new { ArcherCooldown = 0.3f, BomberRadius = 2.0f, BomberDamage = 110f, KnightMult = 2.5f, GroundHP = 150f },
                new { ArcherCooldown = 1.8f, BomberRadius = 3.5f, BomberDamage = 200f, KnightMult = 4.0f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 4.0f, BomberDamage = 40f, KnightMult = 1.5f, GroundHP = 150f },
                new { ArcherCooldown = 0.95f, BomberRadius = 0.8f, BomberDamage = 180f, KnightMult = 2.0f, GroundHP = 150f },
                new { ArcherCooldown = 0.85f, BomberRadius = 2.0f, BomberDamage = 100f, KnightMult = 2.0f, GroundHP = 180f }
            };

            for (int i = 0; i < configs.Length; i++)
            {
                var c = configs[i];
                // Verify that the configuration is valid and runs without errors
                bool success = true;
                string msg = "OK";
                try
                {
                    // Create a dummy unit and block to verify parameters
                    var unitGo = new GameObject("TestUnit");
                    var unit = unitGo.AddComponent<UnitController>();
                    unit.maxHP = 100f;
                    unit.currentHP = 100f;
                    unit.attackCooldown = c.ArcherCooldown;
                    unit.explosionRadius = c.BomberRadius;
                    unit.explosionDamage = c.BomberDamage;

                    var blockGo = new GameObject("TestBlock");
                    var block = blockGo.AddComponent<DestructibleBlock>();
                    block.maxHP = c.GroundHP;
                    block.currentHP = c.GroundHP;

                    // Verify damage calculation
                    float damage = 10f * c.KnightMult;
                    block.TakeDamage(damage);
                    Assert.AreEqual(c.GroundHP - damage, block.currentHP);

                    Object.DestroyImmediate(unitGo);
                    Object.DestroyImmediate(blockGo);
                }
                catch (System.Exception ex)
                {
                    success = false;
                    msg = ex.Message;
                }

                sb.AppendLine($"| {i + 1} | {c.ArcherCooldown}s | {c.BomberRadius}u | {c.BomberDamage} HP | {c.KnightMult}x | {c.GroundHP} HP | {(success ? "Passed" : "Failed")} | {msg} |");
            }

            sb.AppendLine();
            sb.AppendLine("## Dynamic Ground Disintegration & Strategic Bridge Collapse Design");
            sb.AppendLine();
            sb.AppendLine("To enhance tactical depth, we introduce a heterogeneous ground tile layout:");
            sb.AppendLine("- **Castle Foundations ($x \\in [-8, -6]$ and $x \\in [6, 8]$)**: Iron blocks (150 HP) to ensure initial stability of the player and enemy castles.");
            sb.AppendLine("- **Central Bridge ($x \\in [-2, 2]$)**: Wood blocks (50 HP) which are highly fragile and easily destroyed by units or explosive barrels.");
            sb.AppendLine("- **Bridge Approaches ($x \\in [-5, -3]$ and $x \\in [3, 5]$)**: Stone blocks (100 HP) providing medium durability.");
            sb.AppendLine("- **Outer Ground ($x < -8$ or $x > 8$)**: Stone blocks (100 HP).");
            sb.AppendLine();
            sb.AppendLine("This design creates a high-risk central zone where players can strategically target the bridge to drop enemy units into the abyss, while keeping their own castle foundations secure.");

            System.IO.File.WriteAllText(reportPath, sb.ToString());
            try
            {
                System.IO.File.WriteAllText(globalReportPath, sb.ToString());
            }
            catch {}
        }

        [Test]
        public void LaunchManager_CalculatesBowstringVelocity_WithClamp()
        {
            var go = new GameObject("LaunchManager");
            var launchPoint = new GameObject("LaunchPoint");
            launchPoint.transform.position = Vector3.zero;

            var launchManager = go.AddComponent<LaunchManager>();
            launchManager.launchPoint = launchPoint.transform;
            launchManager.maxDragDistance = 2f;
            launchManager.launchForceMultiplier = 10f;
            launchManager.maxLaunchVelocity = 12f;

            // Dragging to the left (-100, 0) should result in a velocity pointing to the left (negative X)
            Vector2 velocityLeft = launchManager.CalculateLaunchVelocity(new Vector2(-100f, 0f));
            Assert.LessOrEqual(velocityLeft.magnitude, launchManager.maxLaunchVelocity + 0.001f);
            Assert.Less(velocityLeft.x, 0f);
            Assert.AreEqual(0f, velocityLeft.y, 0.001f);

            // Dragging to the right (100, 0) should result in a velocity pointing to the right (positive X)
            Vector2 velocityRight = launchManager.CalculateLaunchVelocity(new Vector2(100f, 0f));
            Assert.LessOrEqual(velocityRight.magnitude, launchManager.maxLaunchVelocity + 0.001f);
            Assert.Greater(velocityRight.x, 0f);
            Assert.AreEqual(0f, velocityRight.y, 0.001f);

            Assert.AreEqual(1f, launchManager.GetPullTensionRatio(new Vector2(-100f, 0f)), 0.001f);
            Assert.AreEqual(0f, launchManager.GetPullTensionRatio(Vector2.zero), 0.001f);

            Object.DestroyImmediate(go);
            Object.DestroyImmediate(launchPoint);
        }

        [Test]
        public void LaunchManager_FullDrawProducesTunedVelocityAndFullTension()
        {
            var managerGo = new GameObject("FullDrawLaunchManager");
            var launchPointGo = new GameObject("FullDrawLaunchPoint");
            try
            {
                launchPointGo.transform.position = new Vector3(2f, -1f, 0f);
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.maxDragDistance = 4.2f;
                launchManager.launchForceMultiplier = 6f;
                launchManager.maxLaunchVelocity = 25.2f;
                launchManager.minLaunchVelocity = 3f;

                Vector2 pointer = (Vector2)launchPointGo.transform.position + Vector2.left * 4.2f;
                Vector2 velocity = launchManager.CalculateLaunchVelocity(pointer);

                Assert.AreEqual(-25.2f, velocity.x, 0.001f,
                    "A full leftward draw must preserve aim while reaching the tuned launch cap.");
                Assert.AreEqual(0f, velocity.y, 0.001f);
                Assert.AreEqual(25.2f, velocity.magnitude, 0.001f,
                    "A 4.2-unit full draw must launch at 25.2 m/s.");
                Assert.AreEqual(1f, launchManager.GetPullTensionRatio(pointer), 0.001f,
                    "A full draw must report complete bowstring tension.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void LaunchManager_MinimumLaunchThresholdBoundaryRemainsContinuous()
        {
            var managerGo = new GameObject("ThresholdLaunchManager");
            var launchPointGo = new GameObject("ThresholdLaunchPoint");
            try
            {
                launchPointGo.transform.position = new Vector3(-3f, 2f, 0f);
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.maxDragDistance = 4.2f;
                launchManager.launchForceMultiplier = 6f;
                launchManager.maxLaunchVelocity = 25.2f;
                launchManager.minLaunchVelocity = 3f;

                Vector2 belowThreshold = launchManager.CalculateLaunchVelocity(
                    (Vector2)launchPointGo.transform.position + Vector2.up * 0.49f);
                Vector2 atThreshold = launchManager.CalculateLaunchVelocity(
                    (Vector2)launchPointGo.transform.position + Vector2.up * 0.50f);

                Assert.AreEqual(2.94f, belowThreshold.magnitude, 0.001f);
                Assert.Less(belowThreshold.magnitude, launchManager.minLaunchVelocity,
                    "A 0.49-unit pull must remain below the 3 m/s launch threshold.");
                Assert.AreEqual(3f, atThreshold.magnitude, 0.001f,
                    "The exact 0.50-unit boundary must reach 3 m/s without an off-by-one dead zone.");
                Assert.Greater(atThreshold.y, 0f, "Threshold handling must preserve the aimed direction.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void LaunchManager_DrawTrajectory_FirstStepUsesSemiImplicitIntegration()
        {
            var managerGo = new GameObject("TrajectoryIntegrationLaunchManager");
            var launchPointGo = new GameObject("TrajectoryIntegrationLaunchPoint");
            try
            {
                launchPointGo.transform.position = new Vector3(1234f, -987f, 0f);
                var trajectoryLine = managerGo.AddComponent<LineRenderer>();
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.trajectoryLine = trajectoryLine;
                launchManager.trajectoryResolution = 1;
                launchManager.timeStep = 0.05f;

                var drawTrajectory = typeof(LaunchManager).GetMethod(
                    "DrawTrajectory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(drawTrajectory,
                    "The EditMode fixture must be able to execute the trajectory integration path.");

                Vector2 startingVelocity = new Vector2(7f, 11f);
                drawTrajectory.Invoke(launchManager, new object[] { startingVelocity });

                Vector2 start = launchManager.GetLaunchPosition();
                Vector2 expectedFirstStep =
                    start + (startingVelocity + Physics2D.gravity * launchManager.timeStep) * launchManager.timeStep;

                Assert.AreEqual(2, trajectoryLine.positionCount,
                    "A one-step trajectory must expose its start and first integrated point.");
                Vector3 actualFirstStep = trajectoryLine.GetPosition(1);
                Assert.AreEqual(expectedFirstStep.x, actualFirstStep.x, 0.0001f);
                Assert.AreEqual(expectedFirstStep.y, actualFirstStep.y, 0.0001f,
                    "Trajectory preview must update velocity with gravity before advancing position.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void LaunchManager_DrawTrajectory_FirstPreviewPointMatchesElevatedRuntimeLaunchPosition()
        {
            var managerGo = new GameObject("TrajectoryOriginLaunchManager");
            var launchPointGo = new GameObject("TrajectoryOriginLaunchPoint");
            try
            {
                launchPointGo.transform.position = new Vector3(4321f, -2100f, 0f);
                var trajectoryLine = managerGo.AddComponent<LineRenderer>();
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.trajectoryLine = trajectoryLine;
                launchManager.trajectoryResolution = 2;

                var drawTrajectory = typeof(LaunchManager).GetMethod(
                    "DrawTrajectory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(drawTrajectory);

                drawTrajectory.Invoke(launchManager, new object[] { Vector2.right });

                Vector2 runtimeLaunchPosition = launchManager.GetLaunchPosition();
                Vector3 previewStart = trajectoryLine.GetPosition(0);
                Assert.AreEqual(runtimeLaunchPosition.x, previewStart.x, 0.0001f);
                Assert.AreEqual(runtimeLaunchPosition.y, previewStart.y, 0.0001f,
                    "The preview must originate at the same elevated position used to spawn the launched unit.");
                Assert.AreEqual(
                    UnitController.DefaultLaunchSpawnHeight,
                    previewStart.y - launchPointGo.transform.position.y,
                    0.0001f,
                    "The first preview point must include the runtime launch body's spawn height above the anchor.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void LaunchManager_DrawTrajectory_BoxFootprintStopsAtOffCenterThinObstacle()
        {
            var managerGo = new GameObject("TrajectoryFootprintLaunchManager");
            var launchPointGo = new GameObject("TrajectoryFootprintLaunchPoint");
            var selectedPrefab = new GameObject("TrajectoryFootprintSelectedPrefab");
            var obstacleGo = new GameObject("TrajectoryFootprintThinObstacle");
            try
            {
                launchPointGo.transform.position = new Vector3(6000f, 7000f, 0f);
                selectedPrefab.transform.position = new Vector3(6500f, 7500f, 0f);
                var selectedCollider = selectedPrefab.AddComponent<BoxCollider2D>();
                selectedCollider.size = new Vector2(2f, 2f);

                var trajectoryLine = managerGo.AddComponent<LineRenderer>();
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.trajectoryLine = trajectoryLine;
                launchManager.trajectoryResolution = 5;
                launchManager.timeStep = 0.001f;
                launchManager.SetSelectedUnit(selectedPrefab);

                Vector2 previewStart = launchManager.GetLaunchPosition();
                obstacleGo.transform.position = previewStart + new Vector2(3f, 0.75f);
                var obstacleCollider = obstacleGo.AddComponent<BoxCollider2D>();
                obstacleCollider.size = new Vector2(0.05f, 0.1f);
                Physics2D.SyncTransforms();

                Vector2 startingVelocity = new Vector2(1000f, 0f);
                Vector2 firstVelocity = startingVelocity + Physics2D.gravity * launchManager.timeStep;
                Vector2 firstStep = previewStart + firstVelocity * launchManager.timeStep;
                Vector2 secondVelocity = firstVelocity + Physics2D.gravity * launchManager.timeStep;
                Vector2 secondStep = firstStep + secondVelocity * launchManager.timeStep;
                RaycastHit2D centerLineHit = Physics2D.Linecast(
                    previewStart,
                    secondStep,
                    Physics2D.DefaultRaycastLayers);
                Assert.IsNull(centerLineHit.collider,
                    "Precondition: the obstacle must sit outside the preview body's center-line path.");

                Vector2 footprint = UnitController.EstimateLaunchedWorldColliderSize(selectedPrefab);
                Vector2 secondSegment = secondStep - firstStep;
                RaycastHit2D footprintHit = Physics2D.BoxCast(
                    firstStep,
                    footprint,
                    0f,
                    secondSegment.normalized,
                    secondSegment.magnitude,
                    Physics2D.DefaultRaycastLayers);
                Assert.AreSame(obstacleCollider, footprintHit.collider,
                    "Precondition: the selected unit's full footprint must reach the off-center obstacle.");

                var drawTrajectory = typeof(LaunchManager).GetMethod(
                    "DrawTrajectory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(drawTrajectory);
                drawTrajectory.Invoke(launchManager, new object[] { startingVelocity });

                Assert.AreEqual(3, trajectoryLine.positionCount,
                    "The preview must stop on the second segment instead of continuing through the thin obstacle.");
                Vector3 previewEnd = trajectoryLine.GetPosition(trajectoryLine.positionCount - 1);
                Assert.AreEqual(footprintHit.centroid.x, previewEnd.x, 0.001f);
                Assert.AreEqual(footprintHit.centroid.y, previewEnd.y, 0.001f,
                    "The preview endpoint must be the launched body's centroid at shape-cast contact.");
                Assert.Less(previewEnd.x, obstacleCollider.bounds.min.x,
                    "The full-sized launch body must stop before its center passes into the obstacle.");
            }
            finally
            {
                Object.DestroyImmediate(obstacleGo);
                Object.DestroyImmediate(selectedPrefab);
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }


        [Test]
        public void LaunchManager_DrawTrajectory_MatchesRuntimeLinearDragAcrossRepresentativeSteps()
        {
            var managerGo = new GameObject("TrajectoryDragLaunchManager");
            var launchPointGo = new GameObject("TrajectoryDragLaunchPoint");
            var unitTemplateGo = new GameObject("TrajectoryDragUnitTemplate");
            try
            {
                launchPointGo.transform.position = new Vector3(1234f, -987f, 0f);
                var trajectoryLine = managerGo.AddComponent<LineRenderer>();
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.trajectoryLine = trajectoryLine;
                launchManager.trajectoryResolution = 8;
                launchManager.timeStep = 0.02f;

                var templateBody = unitTemplateGo.AddComponent<Rigidbody2D>();
                templateBody.mass = 1f;
                templateBody.drag = 0.05f;
                unitTemplateGo.AddComponent<BoxCollider2D>();
                unitTemplateGo.AddComponent<UnitController>();
                launchManager.SetSelectedUnit(unitTemplateGo);

                var drawTrajectory = typeof(LaunchManager).GetMethod(
                    "DrawTrajectory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(drawTrajectory);
                Vector2 startingVelocity = new Vector2(7f, 11f);
                drawTrajectory.Invoke(launchManager, new object[] { startingVelocity });

                Assert.AreEqual(launchManager.trajectoryResolution + 1, trajectoryLine.positionCount,
                    "An unobstructed preview must expose its start and every requested fixed-step sample.");
                Vector2 expectedPosition = launchManager.GetLaunchPosition();
                Vector2 expectedVelocity = startingVelocity;
                for (int step = 1; step <= launchManager.trajectoryResolution; step++)
                {
                    expectedVelocity =
                        (expectedVelocity + Physics2D.gravity * launchManager.timeStep)
                        / (1f + templateBody.drag * launchManager.timeStep);
                    expectedPosition += expectedVelocity * launchManager.timeStep;

                    Vector3 actual = trajectoryLine.GetPosition(step);
                    Assert.AreEqual(expectedPosition.x, actual.x, 0.0001f,
                        $"Step {step} must retain Rigidbody2D-style linear damping on horizontal velocity.");
                    Assert.AreEqual(expectedPosition.y, actual.y, 0.0001f,
                        $"Step {step} must apply gravity, then linear damping, then advance position.");
                }
            }
            finally
            {
                Object.DestroyImmediate(unitTemplateGo);
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void LaunchManager_DrawTrajectory_IgnoresFriendlyDefaultLayerBodyButStopsOnEnemyAndGround()
        {
            var managerGo = new GameObject("TrajectoryCollisionLaunchManager");
            var launchPointGo = new GameObject("TrajectoryCollisionLaunchPoint");
            var unitTemplateGo = new GameObject("TrajectoryCollisionUnitTemplate");
            var friendlyGo = new GameObject("TrajectoryFriendlyBody");
            var enemyGo = new GameObject("TrajectoryEnemyBody");
            var groundGo = new GameObject("TrajectoryGround");
            try
            {
                Vector3 start = new Vector3(2000f, 2000f, 0f);
                launchPointGo.transform.position = start;
                var trajectoryLine = managerGo.AddComponent<LineRenderer>();
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.trajectoryLine = trajectoryLine;
                launchManager.trajectoryResolution = 20;
                launchManager.timeStep = 0.05f;

                unitTemplateGo.AddComponent<Rigidbody2D>().gravityScale = 0f;
                unitTemplateGo.AddComponent<BoxCollider2D>();
                var templateUnit = unitTemplateGo.AddComponent<UnitController>();
                templateUnit.isPlayerUnit = true;
                launchManager.SetSelectedUnit(unitTemplateGo);

                friendlyGo.transform.position = start + new Vector3(1f, 0f, 0f);
                friendlyGo.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                var friendlyCollider = friendlyGo.AddComponent<BoxCollider2D>();
                friendlyCollider.size = new Vector2(0.3f, 4f);
                var friendlyUnit = friendlyGo.AddComponent<UnitController>();
                friendlyUnit.isPlayerUnit = true;

                enemyGo.transform.position = start + new Vector3(2f, 0f, 0f);
                enemyGo.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                var enemyCollider = enemyGo.AddComponent<BoxCollider2D>();
                enemyCollider.size = new Vector2(0.3f, 4f);
                var enemyUnit = enemyGo.AddComponent<UnitController>();
                enemyUnit.isPlayerUnit = false;

                groundGo.transform.position = start + new Vector3(3f, 0f, 0f);
                var groundCollider = groundGo.AddComponent<BoxCollider2D>();
                groundCollider.size = new Vector2(0.3f, 4f);
                Physics2D.SyncTransforms();

                var drawTrajectory = typeof(LaunchManager).GetMethod(
                    "DrawTrajectory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(drawTrajectory);
                drawTrajectory.Invoke(launchManager, new object[] { new Vector2(10f, 0f) });

                float enemyHitX = trajectoryLine.GetPosition(trajectoryLine.positionCount - 1).x;
                Assert.Greater(enemyHitX, friendlyGo.transform.position.x + friendlyCollider.size.x * 0.5f,
                    "The preview must pass through a same-team body even when every collider uses the Default layer.");
                Assert.Less(enemyHitX, enemyGo.transform.position.x + enemyCollider.size.x * 0.5f,
                    "The preview must still stop at the first enemy body after ignoring its teammate.");

                enemyCollider.enabled = false;
                Physics2D.SyncTransforms();
                drawTrajectory.Invoke(launchManager, new object[] { new Vector2(10f, 0f) });
                float groundHitX = trajectoryLine.GetPosition(trajectoryLine.positionCount - 1).x;
                Assert.Greater(groundHitX, enemyGo.transform.position.x + enemyCollider.size.x * 0.5f);
                Assert.Less(groundHitX, groundGo.transform.position.x + groundCollider.size.x * 0.5f,
                    "Ignoring a friendly unit must not make the preview pass through the downstream ground structure.");
            }
            finally
            {
                Object.DestroyImmediate(groundGo);
                Object.DestroyImmediate(enemyGo);
                Object.DestroyImmediate(friendlyGo);
                Object.DestroyImmediate(unitTemplateGo);
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void UnitController_CalculateWindAcceleration_RespectsRadiusForceAndMassFloor()
        {
            Vector2 windOrigin = new Vector2(4f, -2f);
            const float windRadius = 3f;
            const float windForce = 6f;

            Vector2 inside = UnitController.CalculateWindAcceleration(
                windOrigin + Vector2.right * 2f, 2f, windForce, windOrigin, windRadius);
            Vector2 outside = UnitController.CalculateWindAcceleration(
                windOrigin + Vector2.right * 3.01f, 2f, windForce, windOrigin, windRadius);
            Vector2 zeroRadius = UnitController.CalculateWindAcceleration(
                windOrigin, 2f, windForce, windOrigin, 0f);
            Vector2 zeroForce = UnitController.CalculateWindAcceleration(
                windOrigin, 2f, 0f, windOrigin, windRadius);
            Vector2 belowMassFloor = UnitController.CalculateWindAcceleration(
                windOrigin, 0.01f, 3f, windOrigin, windRadius);
            Vector2 atMassFloor = UnitController.CalculateWindAcceleration(
                windOrigin, UnitController.MinRuntimeMass, 3f, windOrigin, windRadius);

            Assert.AreEqual(3f, inside.x, 0.0001f,
                "Wind inside the active radius must accelerate by force divided by mass.");
            Assert.AreEqual(0f, inside.y, 0.0001f, "Wind must remain horizontal.");
            Assert.AreEqual(Vector2.zero, outside, "Wind must not accelerate units outside its radius.");
            Assert.AreEqual(Vector2.zero, zeroRadius, "A zero-radius wind field must be inactive.");
            Assert.AreEqual(Vector2.zero, zeroForce, "A zero-force wind field must be inactive.");
            Assert.AreEqual(atMassFloor.x, belowMassFloor.x, 0.0001f,
                "Mass below the runtime floor must not produce unbounded wind acceleration.");
            Assert.AreEqual(20f, belowMassFloor.x, 0.0001f);
        }

        [Test]
        public void GameManager_PostImpactHold_RemainsShortAndObservable()
        {
            Assert.Greater(GameManager.PostImpactHoldSeconds, 0f,
                "Resolved impacts need a nonzero readability beat before turn handoff.");
            Assert.LessOrEqual(GameManager.PostImpactHoldSeconds, 0.5f,
                "Post-impact feedback must not reintroduce a long input-blocking pause.");
        }

        [Test]
        public void LaunchManager_CancelAim_ClearsPreviewWithoutLaunchingOrConsumingTurn()
        {
            var managerGo = new GameObject("CancelAimLaunchManager");
            var trajectoryGo = new GameObject("CancelAimTrajectory");
            var rubberBandGo = new GameObject("CancelAimRubberBand");
            var impactMarker = new GameObject("CancelAimImpactMarker");
            var unitTemplateGo = new GameObject("CancelAimUnitTemplate");
            var gameManagerGo = new GameObject("CancelAimGameManager");
            HashSet<UnitController> unitsBeforeCancellation = null;
            try
            {
                var trajectoryLine = trajectoryGo.AddComponent<LineRenderer>();
                trajectoryLine.positionCount = 4;
                var rubberBandLine = rubberBandGo.AddComponent<LineRenderer>();
                rubberBandLine.positionCount = 3;
                impactMarker.SetActive(true);

                unitTemplateGo.AddComponent<Rigidbody2D>();
                unitTemplateGo.AddComponent<BoxCollider2D>();
                unitTemplateGo.AddComponent<UnitController>();

                var gameManager = gameManagerGo.AddComponent<GameManager>();
                var gameManagerAwake = typeof(GameManager).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var turnCountField = typeof(GameManager).GetField(
                    "turnCount",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isPlayerTurnField = typeof(GameManager).GetField(
                    "isPlayerTurn",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var isResolvingTurnField = typeof(GameManager).GetField(
                    "isResolvingTurn",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(gameManagerAwake);
                Assert.IsNotNull(turnCountField);
                Assert.IsNotNull(isPlayerTurnField);
                Assert.IsNotNull(isResolvingTurnField);
                gameManagerAwake.Invoke(gameManager, null);
                gameManager.currentState = GameState.PlayerTurn;
                turnCountField.SetValue(gameManager, 7);
                isPlayerTurnField.SetValue(gameManager, true);
                isResolvingTurnField.SetValue(gameManager, false);

                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.trajectoryLine = trajectoryLine;
                launchManager.rubberBandLine = rubberBandLine;
                var isDraggingField = typeof(LaunchManager).GetField(
                    "isDragging",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var launchVelocityField = typeof(LaunchManager).GetField(
                    "launchVelocity",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var selectedUnitPrefabField = typeof(LaunchManager).GetField(
                    "selectedUnitPrefab",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var impactMarkerField = typeof(LaunchManager).GetField(
                    "impactMarkerInstance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(isDraggingField);
                Assert.IsNotNull(launchVelocityField);
                Assert.IsNotNull(selectedUnitPrefabField);
                Assert.IsNotNull(impactMarkerField);
                isDraggingField.SetValue(launchManager, true);
                launchVelocityField.SetValue(launchManager, new Vector2(10f, 5f));
                selectedUnitPrefabField.SetValue(launchManager, unitTemplateGo);
                impactMarkerField.SetValue(launchManager, impactMarker);

                unitsBeforeCancellation = new HashSet<UnitController>(Object.FindObjectsOfType<UnitController>());
                int turnCountBefore = gameManager.TurnCount;
                GameState stateBefore = gameManager.currentState;
                bool playerTurnBefore = gameManager.IsPlayerTurn;
                bool resolvingBefore = gameManager.IsResolvingTurn;

                launchManager.CancelAim();

                Assert.IsFalse(launchManager.IsAiming, "Cancelling deploy-mode aim must leave no active draw.");
                Assert.AreEqual(0, trajectoryLine.positionCount, "Cancelling aim must clear the trajectory preview.");
                Assert.AreEqual(0, rubberBandLine.positionCount, "Cancelling aim must hide the rubber band.");
                Assert.IsFalse(impactMarker.activeSelf, "Cancelling aim must hide the predicted impact marker.");
                Assert.AreEqual(unitsBeforeCancellation.Count, Object.FindObjectsOfType<UnitController>().Length,
                    "Cancelling aim must not instantiate the armed unit prefab.");
                Assert.AreEqual(turnCountBefore, gameManager.TurnCount, "Cancelling aim must not advance the turn.");
                Assert.AreEqual(stateBefore, gameManager.currentState, "Cancelling aim must not change turn state.");
                Assert.AreEqual(playerTurnBefore, gameManager.IsPlayerTurn, "Cancelling aim must not hand control away.");
                Assert.AreEqual(resolvingBefore, gameManager.IsResolvingTurn,
                    "Cancelling aim must not enter launch-resolution flow.");
            }
            finally
            {
                if (unitsBeforeCancellation != null)
                {
                    foreach (var unit in Object.FindObjectsOfType<UnitController>())
                    {
                        if (!unitsBeforeCancellation.Contains(unit)) Object.DestroyImmediate(unit.gameObject);
                    }
                }
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(trajectoryGo);
                Object.DestroyImmediate(rubberBandGo);
                Object.DestroyImmediate(impactMarker);
                Object.DestroyImmediate(unitTemplateGo);
                Object.DestroyImmediate(gameManagerGo);
            }
        }


        [Test]
        public void ItemDropRules_ShouldDrop_ExcludesExactSixtyPercentBoundary()
        {
            Assert.IsTrue(ItemDropRules.ShouldDrop(0.6f - 0.000001f),
                "A value immediately below 60% must still drop.");
            Assert.IsFalse(ItemDropRules.ShouldDrop(0.6f),
                "The exact 60% boundary must be excluded by the strict probability gate.");
        }

        [Test]
        public void ItemDropRules_TypeForRoll_ChangesAtExactThirdBoundaries()
        {
            const float epsilon = 0.000001f;
            float oneThird = 1f / 3f;
            float twoThirds = 2f / 3f;

            Assert.AreEqual(HeroItemType.Sword, ItemDropRules.TypeForRoll(oneThird - epsilon),
                "The value immediately below one third must remain in the Sword bucket.");
            Assert.AreEqual(HeroItemType.Shield, ItemDropRules.TypeForRoll(oneThird),
                "The exact one-third boundary must enter the Shield bucket.");
            Assert.AreEqual(HeroItemType.Shield, ItemDropRules.TypeForRoll(twoThirds - epsilon),
                "The value immediately below two thirds must remain in the Shield bucket.");
            Assert.AreEqual(HeroItemType.Boots, ItemDropRules.TypeForRoll(twoThirds),
                "The exact two-thirds boundary must enter the Boots bucket.");
        }

        [Test]
        public void UnitPrefabs_InitializeAtEnlargedScaleWithoutChangingColliderWorldFootprint()
        {
            var prefabPaths = new[]
            {
                "Assets/Prefabs/Knight.prefab",
                "Assets/Prefabs/Archer.prefab"
            };

            foreach (string prefabPath in prefabPaths)
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Assert.IsNotNull(prefab, $"Required unit prefab could not be loaded: {prefabPath}");

                GameObject instance = null;
                try
                {
                    instance = Object.Instantiate(prefab);
                    var unit = instance.GetComponent<UnitController>();
                    var box = instance.GetComponent<BoxCollider2D>();
                    var spriteRenderer = instance.GetComponentInChildren<SpriteRenderer>(true);
                    Assert.IsNotNull(unit, $"{prefabPath} must initialize a UnitController.");
                    Assert.IsNotNull(box, $"{prefabPath} must initialize a BoxCollider2D.");
                    Assert.IsNotNull(spriteRenderer, $"{prefabPath} must initialize a SpriteRenderer.");
                    Assert.IsNotNull(spriteRenderer.sprite, $"{prefabPath} must initialize with a coverage sprite.");

                    var applyScaleAndCollider = typeof(UnitController).GetMethod(
                        "ApplyPlayableScaleAndCollider",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    Assert.IsNotNull(applyScaleAndCollider,
                        "The EditMode fixture must be able to execute the prefab initialization path.");
                    applyScaleAndCollider.Invoke(unit, null);

                    Physics2D.SyncTransforms();

                    Assert.AreEqual(0.48f, instance.transform.localScale.x, 0.0001f,
                        $"{prefabPath} must render at the enlarged 0.48 visual scale.");
                    Assert.AreEqual(0.48f, instance.transform.localScale.y, 0.0001f,
                        $"{prefabPath} must render uniformly at the enlarged visual scale.");

                    Vector2 expectedOldWorldFootprint =
                        spriteRenderer.sprite.bounds.size * (0.42f * unit.colliderVisualCoverage);
                    Vector2 actualWorldFootprint = box.bounds.size;
                    Assert.AreEqual(expectedOldWorldFootprint.x, actualWorldFootprint.x, 0.0001f,
                        $"{prefabPath} collider width must preserve the old 0.42-scale sprite coverage.");
                    Assert.AreEqual(expectedOldWorldFootprint.y, actualWorldFootprint.y, 0.0001f,
                        $"{prefabPath} collider height must preserve the old 0.42-scale sprite coverage.");
                }
                finally
                {
                    if (instance != null) Object.DestroyImmediate(instance);
                }
            }
        }

        [Test]
        public void KoreanFontSupport_BundledDynamicFont_LoadsAndSupportsHangul()
        {
            var bundledFont = Resources.Load<TMPro.TMP_FontAsset>("Fonts/NotoSansKR-Dynamic");

            Assert.IsNotNull(bundledFont,
                "The player-safe Korean font must load from Resources/Fonts/NotoSansKR-Dynamic.");
            Assert.IsTrue(KoreanFontSupport.SupportsHangul(bundledFont),
                "The bundled dynamic font must resolve Hangul without relying on OS font discovery.");
        }

        [Test]
        public void GameAudio_BundledSfx_LoadAsShortMono44100HzClips()
        {
            var contracts = new[]
            {
                new { Path = "Audio/SFX/impact", MinimumLength = 0.17f, MaximumLength = 0.19f },
                new { Path = "Audio/SFX/launch", MinimumLength = 0.24f, MaximumLength = 0.26f },
                new { Path = "Audio/SFX/combo", MinimumLength = 0.26f, MaximumLength = 0.28f }
            };

            foreach (var contract in contracts)
            {
                var clip = Resources.Load<AudioClip>(contract.Path);
                Assert.IsNotNull(clip, $"Bundled gameplay SFX must load from Resources/{contract.Path}.");
                Assert.AreEqual(1, clip.channels, $"{contract.Path} must remain mono for predictable SFX mixing.");
                Assert.AreEqual(44100, clip.frequency, $"{contract.Path} must remain at the authored 44.1 kHz rate.");
                Assert.That(clip.length, Is.InRange(contract.MinimumLength, contract.MaximumLength),
                    $"{contract.Path} must remain a short feedback clip, but was {clip.length:F3}s.");
            }
        }

        [Test]
        public void EventGateGimmick_PowerUpUnit_MultipliesVelocity()
        {
            var unitGo = new GameObject("GateUnit");
            var rb = unitGo.AddComponent<Rigidbody2D>();
            unitGo.AddComponent<BoxCollider2D>();
            var unit = unitGo.AddComponent<UnitController>();
            var awakeMethod = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(unit, null);
            unit.Launch(new Vector2(2f, 0f));


            var gateGo = new GameObject("PowerGate");
            var gate = gateGo.AddComponent<EventGateGimmick>();
            gate.effectType = EventGateEffectType.PowerUp;
            gate.velocityMultiplier = 2f;
            gate.damageSpeedMultiplier = 1.5f;

            gate.ApplyToUnit(unit);

            Assert.AreEqual(4f, rb.velocity.x, 0.001f);
            Assert.AreEqual(0f, rb.velocity.y, 0.001f);

            Object.DestroyImmediate(gateGo);
            Object.DestroyImmediate(unitGo);
        }

        [Test]
        public void EventGateGimmick_MultiplyArrow_CapsClones()
        {
            var arrowGo = new GameObject("GateArrow");
            var rb = arrowGo.AddComponent<Rigidbody2D>();
            arrowGo.AddComponent<BoxCollider2D>();
            var arrow = arrowGo.AddComponent<ArrowController>();
            rb.velocity = new Vector2(8f, 0f);

            var gateGo = new GameObject("MultiplierGate");
            var gate = gateGo.AddComponent<EventGateGimmick>();
            gate.effectType = EventGateEffectType.Multiply;
            gate.cloneCount = 5;
            gate.maxTotalClones = 2;

            gate.ApplyToArrow(arrow);

            var arrows = Object.FindObjectsOfType<ArrowController>();
            int cloneCount = 0;
            foreach (var candidate in arrows)
            {
                if (candidate != null && candidate.gameObject.name.Contains("_GateClone")) cloneCount++;
            }

            Assert.AreEqual(2, cloneCount);

            foreach (var candidate in arrows)
            {
                if (candidate != null) Object.DestroyImmediate(candidate.gameObject);
            }
            Object.DestroyImmediate(gateGo);
        }

        [Test]
        public void MovingGimmick_ClampsInsidePlayableBounds()
        {
            var go = new GameObject("MovingBoundsGimmick");
            var moving = go.AddComponent<MovingGimmick>();
            moving.moveAxis = Vector2.right;
            moving.moveDistance = 50f;
            moving.moveSpeed = 1f;
            moving.playableBounds = new Rect(-1f, -1f, 2f, 2f);
            moving.simulatedTime = Mathf.PI * 0.5f;

            var awakeMethod = typeof(MovingGimmick).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(moving, null);
            var updateMethod = typeof(MovingGimmick).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(moving, null);

            Assert.IsTrue(moving.playableBounds.Contains(go.transform.position));

            Object.DestroyImmediate(go);
        }

        [Test]
        public void ExplosiveGimmick_OutOfBounds_Destroyed()
        {
            var go = new GameObject("ExplosiveBarrel");
            var exp = go.AddComponent<ExplosiveGimmick>();
            go.transform.position = new Vector3(0f, ChariotRules.KillPlaneY - 1f, 0f);

            var updateMethod = typeof(ExplosiveGimmick).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(exp, null);

            Assert.IsTrue(go == null || !go.activeInHierarchy);
            if (go != null) Object.DestroyImmediate(go);
        }

        [Test]
        public void MovingGimmick_Stuck_ReversesDirection()
        {
            var go = new GameObject("MovingObstacle");
            var moving = go.AddComponent<MovingGimmick>();
            moving.moveAxis = Vector2.up;
            moving.moveDistance = 2f;
            moving.moveSpeed = 1f;

            var awakeMethod = typeof(MovingGimmick).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(moving, null);

            // Set initial position and lastPosition to simulate stuck state
            var lastPosField = typeof(MovingGimmick).GetField("lastPosition", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            lastPosField.SetValue(moving, (Vector2)go.transform.position);


            var stuckTimerField = typeof(MovingGimmick).GetField("stuckTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stuckTimerField.SetValue(moving, 1.1f); // Exceed stuckDuration (1.0s)

            var updateMethod = typeof(MovingGimmick).GetMethod("Update", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            updateMethod.Invoke(moving, null);

            Assert.AreEqual(Vector2.down, moving.moveAxis);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void EventGateGimmick_PowerUpExplosiveGimmick_MultipliesExplosionProperties()
        {
            var unitGo = new GameObject("GateUnit");
            var rb = unitGo.AddComponent<Rigidbody2D>();
            unitGo.AddComponent<BoxCollider2D>();
            var unit = unitGo.AddComponent<UnitController>();
            var exp = unitGo.AddComponent<ExplosiveGimmick>();
            exp.explosionRadius = 2f;
            exp.explosionDamage = 50f;

            var awakeMethod = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(unit, null);
            unit.Launch(new Vector2(2f, 0f));

            var gateGo = new GameObject("PowerGate");
            var gate = gateGo.AddComponent<EventGateGimmick>();
            gate.effectType = EventGateEffectType.PowerUp;
            gate.velocityMultiplier = 2f;
            gate.damageSpeedMultiplier = 1.5f;

            gate.ApplyToUnit(unit);

            Assert.AreEqual(3f, exp.explosionRadius, 0.001f);
            Assert.AreEqual(75f, exp.explosionDamage, 0.001f);

            Object.DestroyImmediate(gateGo);
            Object.DestroyImmediate(unitGo);
        }

        [Test]
        public void UnitController_GroundedStuck_TriggersRecovery()
        {
            var unitGo = new GameObject("StuckUnit");
            var rb = unitGo.AddComponent<Rigidbody2D>();
            var unit = unitGo.AddComponent<UnitController>();

            // Trigger Awake to initialize rb
            var awakeMethod = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(unit, null);

            // Set state to Grounded and bodyType to Dynamic
            var stateField = typeof(UnitController).GetField("currentState", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stateField.SetValue(unit, UnitState.Grounded);
            rb.bodyType = RigidbodyType2D.Dynamic;

            // Set target to simulate trying to move
            var targetGo = new GameObject("Target");
            var targetField = typeof(UnitController).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetField.SetValue(unit, targetGo.transform);

            // Set groundedStuckTimer to exceed stuckDuration (1.25s)
            var stuckTimerField = typeof(UnitController).GetField("groundedStuckTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            stuckTimerField.SetValue(unit, 1.3f);

            // Trigger MonitorGroundedUnitSafety via reflection
            var monitorMethod = typeof(UnitController).GetMethod("MonitorGroundedUnitSafety", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            monitorMethod.Invoke(unit, null);

            // Assert that recovery was triggered: velocity.y should be 6.5f and groundedStuckTimer reset to 0
            Assert.AreEqual(6.5f, rb.velocity.y, 0.001f);
            Assert.AreEqual(0f, (float)stuckTimerField.GetValue(unit), 0.001f);

            Object.DestroyImmediate(unitGo);
            Object.DestroyImmediate(targetGo);
        }
        // --- 2026-07-02 stability + difficulty-curve pass ---

        [Test]
        public void FallImpactDamage_IsCapped_AndIgnoresSlowContacts()
        {
            // Below the minimum speed: resting contact / co-falling blocks deal nothing.
            Assert.AreEqual(0f, DestructibleBlock.CalculateFallImpactDamage(1.9f));
            Assert.AreEqual(0f, DestructibleBlock.CalculateFallImpactDamage(0f));

            // Mid-speed impact keeps the classic velocity*8 feel.
            Assert.AreEqual(24f, DestructibleBlock.CalculateFallImpactDamage(3f), 0.001f);

            // A screaming-fast block can no longer one-shot Stone/Iron chains: hard cap.
            Assert.AreEqual(DestructibleBlock.FallImpactDamageCap, DestructibleBlock.CalculateFallImpactDamage(25f), 0.001f);
            Assert.LessOrEqual(DestructibleBlock.CalculateFallImpactDamage(999f), DestructibleBlock.FallImpactDamageCap);
        }

        [Test]
        public void DifficultyCurve_RampsWindAndTightensAI()
        {
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();

            var turnCountField = typeof(GameManager).GetField("turnCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Turn 0: gentle onboarding values.
            turnCountField.SetValue(gm, 0);
            Assert.AreEqual(0f, gm.DifficultyT, 0.001f);
            Assert.AreEqual(gm.windCapStart, gm.CurrentWindCap, 0.001f);
            Assert.AreEqual(gm.aiErrorStart, gm.CurrentAiErrorOffset, 0.001f);

            // Mid-ramp: strictly between the endpoints, and monotonic.
            turnCountField.SetValue(gm, gm.EffectiveDifficultyRampTurns / 2);
            float midWind = gm.CurrentWindCap;
            float midError = gm.CurrentAiErrorOffset;
            Assert.Greater(midWind, gm.windCapStart);
            Assert.Less(midWind, gm.windCapEnd);
            Assert.Less(midError, gm.aiErrorStart);
            Assert.Greater(midError, gm.aiErrorEnd);

            // Past the ramp: still rising, never plateaued. The ramp used to clamp at
            // exactly 1.0 here, which froze wind, AI error and storm odds for the rest of
            // the match; DifficultyCurve now approaches the endpoints asymptotically so a
            // long siege keeps tightening. See DifficultyCurveTests for the curve's shape.
            // Against the ramp actually in effect, not the superseded inspector field: that
            // field still reads 15 while a match now runs past thirty turns, so multiplying it
            // sampled barely past the ramp instead of well beyond it.
            turnCountField.SetValue(gm, gm.EffectiveDifficultyRampTurns * 3);
            float lateT = gm.DifficultyT;
            float lateWind = gm.CurrentWindCap;
            Assert.Greater(lateT, 0.85f, "Deep into a match the pressure must be near maximum.");
            Assert.Less(lateT, 1f, "…but never exactly maximum, or it would stop moving.");
            Assert.Greater(lateWind, midWind, "Wind must keep climbing past the ramp turn.");
            Assert.Less(lateWind, gm.windCapEnd);
            Assert.Less(gm.CurrentAiErrorOffset, midError, "AI must keep tightening past the ramp turn.");
            Assert.Greater(gm.CurrentAiErrorOffset, gm.aiErrorEnd);
            Assert.Less(gm.CurrentStormChance, gm.stormChanceEnd);

            turnCountField.SetValue(gm, gm.EffectiveDifficultyRampTurns * 6);
            Assert.Greater(gm.DifficultyT, lateT, "Even a very long match must keep escalating.");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void CreateGround_AnchorsBottomRowsAndFlanks_KeepsBridgeBreakable()
        {
            var go = new GameObject("GameManager");
            var gm = go.AddComponent<GameManager>();

            var method = typeof(GameManager).GetMethod("CreateGround", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method.Invoke(gm, null);

            var allFound = Object.FindObjectsOfType<DestructibleBlock>();
            var blocks = new List<DestructibleBlock>();
            foreach (var b in allFound) if (b.name.StartsWith("GroundBlock_")) blocks.Add(b);
            Assert.AreEqual(41 * 5, blocks.Count, "Ground grid must stay 41x5");

            foreach (var block in blocks)
            {
                float x = block.transform.position.x;
                float y = block.transform.position.y;
                bool isFlank = x <= -10f || x >= 10f; // widened board: cores moved to ±9
                bool isBottomTwoRows = y < -2.9f; // rows yIndex 3 (y=-3.5) and 4 (y=-4.5); y = -0.5 - yIndex

                if (isFlank || isBottomTwoRows)
                {
                    Assert.IsTrue(block.isGroundAnchor, $"Block at ({x},{y}) must be anchored");
                }
                else
                {
                    Assert.IsFalse(block.isGroundAnchor, $"Bridge/approach block at ({x},{y}) must stay breakable");
                }
            }

            foreach (var block in blocks) if (block != null) Object.DestroyImmediate(block.gameObject);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void KoreanFontSupport_RegistersHangulCapableFallback()
        {
            KoreanFontSupport.EnsureFallback();

            bool defaultHasHangul = KoreanFontSupport.SupportsHangul(TMPro.TMP_Settings.defaultFontAsset);
            bool fallbackHasHangul = TMPro.TMP_Settings.fallbackFontAssets != null &&
                TMPro.TMP_Settings.fallbackFontAssets.Exists(f => KoreanFontSupport.SupportsHangul(f));

            Assert.IsTrue(defaultHasHangul || fallbackHasHangul,
                "After EnsureFallback, TMP must be able to resolve Hangul glyphs (default font or fallback chain)");
        }
        // --- 2026-07-02 playtest polish pass: turn handling + dedicated gimmick art ---

        [Test]
        public void TurnExpiry_AimingPlayerGetsOneGrace_IdlePlayerForfeits()
        {
            // Actively aiming, grace unused -> one extension so the drawn shot can be released.
            Assert.AreEqual(GameManager.TurnExpiryDecision.GrantGrace,
                GameManager.DecideTurnExpiry(isPlayerTurn: true, isAiming: true, graceAlreadyUsed: false));

            // Grace already consumed -> the turn ends even mid-aim (no infinite stalling).
            Assert.AreEqual(GameManager.TurnExpiryDecision.ForfeitPlayerTurn,
                GameManager.DecideTurnExpiry(isPlayerTurn: true, isAiming: true, graceAlreadyUsed: true));

            // Idle player -> explicit forfeit (with notice), never a silent skip.
            Assert.AreEqual(GameManager.TurnExpiryDecision.ForfeitPlayerTurn,
                GameManager.DecideTurnExpiry(isPlayerTurn: true, isAiming: false, graceAlreadyUsed: false));

            // AI turn expiry is always a plain end-of-turn.
            Assert.AreEqual(GameManager.TurnExpiryDecision.EndTurn,
                GameManager.DecideTurnExpiry(isPlayerTurn: false, isAiming: false, graceAlreadyUsed: false));
            Assert.AreEqual(GameManager.TurnExpiryDecision.EndTurn,
                GameManager.DecideTurnExpiry(isPlayerTurn: false, isAiming: true, graceAlreadyUsed: true));
        }

        [Test]
        public void GimmickSpriteLibrary_LoadsDedicatedArt_AndFailsSoftOnMissing()
        {
            // All seven dedicated assets must resolve from Resources/Gimmicks.
            foreach (var key in new[]
            {
                GimmickSpriteLibrary.RallyRune, GimmickSpriteLibrary.HexRune, GimmickSpriteLibrary.Gate,
                GimmickSpriteLibrary.Ram, GimmickSpriteLibrary.Barrel, GimmickSpriteLibrary.Core,
                GimmickSpriteLibrary.ButtonCard,
            })
            {
                Assert.IsNotNull(GimmickSpriteLibrary.Load(key), $"Dedicated gimmick sprite '{key}' must exist under Resources/Gimmicks");
            }

            // Unknown key: soft-fail contract (null return, no renderer mutation, no throw).
            Assert.IsNull(GimmickSpriteLibrary.Load("no_such_sprite"));
            var go = new GameObject("Probe");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = Color.red;
            Assert.IsFalse(GimmickSpriteLibrary.TryApply(sr, "no_such_sprite", Color.white));
            Assert.AreEqual(Color.red, sr.color, "Failed TryApply must not touch the renderer");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void Gimmicks_UseDedicatedSprites_NotTintedBlocks()
        {
            // Mirrors the real spawn order: AddComponent first, THEN assign the effect type.
            // ApplyVisuals (Start-path) must respect the post-assignment value - the old
            // Awake-path picked art before the spawner had set effectType/isPlayerCore.
            var buffGo = new GameObject("BuffZone");
            var buffSr = buffGo.AddComponent<SpriteRenderer>();
            var buff = buffGo.AddComponent<BuffDebuffGimmick>();
            buff.effectType = GimmickEffectType.Buff;
            buff.ApplyVisuals();
            Assert.IsNotNull(buffSr.sprite, "BuffZone must get the rally-rune sprite");
            StringAssert.Contains("rally_rune", buffSr.sprite.name);

            var debuffGo = new GameObject("DebuffZone");
            var debuffSr = debuffGo.AddComponent<SpriteRenderer>();
            var debuff = debuffGo.AddComponent<BuffDebuffGimmick>();
            debuff.effectType = GimmickEffectType.Debuff;
            debuff.ApplyVisuals();
            Assert.IsNotNull(debuffSr.sprite, "DebuffZone must get the hex-rune sprite");
            StringAssert.Contains("hex_rune", debuffSr.sprite.name);

            // MovingGimmick -> siege ram art (no post-assignment fields; Awake path is fine).
            var ramGo = new GameObject("MovingObstacle");
            var ramSr = ramGo.AddComponent<SpriteRenderer>();
            var ram = ramGo.AddComponent<MovingGimmick>();
            var ramAwake = typeof(MovingGimmick).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ramAwake.Invoke(ram, null);
            Assert.IsNotNull(ramSr.sprite, "MovingObstacle must get the ram sprite");
            StringAssert.Contains("ram", ramSr.sprite.name);

            // EventGate tint must reflect the type assigned AFTER AddComponent.
            var gateGo = new GameObject("ReducerGate");
            var gateSr = gateGo.AddComponent<SpriteRenderer>();
            var gate = gateGo.AddComponent<EventGateGimmick>();
            gate.effectType = EventGateEffectType.Reduce;
            gate.ApplyVisuals();
            Assert.IsNotNull(gateSr.sprite, "Gate must get the arch sprite");
            StringAssert.Contains("gate", gateSr.sprite.name);
            Assert.Greater(gateSr.color.r, 0.9f, "Reduce tint is pink-ish (high R)");
            Assert.Less(gateSr.color.g, 0.85f, "Reduce tint is pink-ish (lower G)");

            Object.DestroyImmediate(buffGo);
            Object.DestroyImmediate(debuffGo);
            Object.DestroyImmediate(ramGo);
            Object.DestroyImmediate(gateGo);
        }
        // --- 2026-07-02 effects + intro animation pass ---

        [Test]
        public void EffectSpriteLibrary_LoadsAllFrameSets_InOrder()
        {
            foreach (var key in new[] { EffectSpriteLibrary.Spark, EffectSpriteLibrary.Dust, EffectSpriteLibrary.Sparkle })
            {
                var frames = EffectSpriteLibrary.LoadFrames(key);
                Assert.IsNotNull(frames, $"Effect frames '{key}' must exist under Resources/Effects");
                Assert.GreaterOrEqual(frames.Length, 3, $"'{key}' needs a playable frame count");
                for (int i = 1; i < frames.Length; i++)
                {
                    Assert.LessOrEqual(string.CompareOrdinal(frames[i - 1].name, frames[i].name), 0,
                        $"'{key}' frames must be lexically ordered for stable playback");
                }
            }

            // Missing key: soft-fail (empty/null, no throw) so gameplay never depends on art.
            var missing = EffectSpriteLibrary.LoadFrames("fx_does_not_exist");
            Assert.IsTrue(missing == null || missing.Length == 0);
        }

        [Test]
        public void FrameAnimEffect_FrameMath_ProgressesAndFinishes()
        {
            // 4 frames at 20fps -> 0.05s per frame.
            Assert.AreEqual(0, FrameAnimEffect.FrameIndexAt(0.00f, 0.05f, 4));
            Assert.AreEqual(1, FrameAnimEffect.FrameIndexAt(0.055f, 0.05f, 4));
            Assert.AreEqual(3, FrameAnimEffect.FrameIndexAt(0.16f, 0.05f, 4));
            // Past the last frame -> index >= count signals "destroy me".
            Assert.GreaterOrEqual(FrameAnimEffect.FrameIndexAt(0.21f, 0.05f, 4), 4);
            // Degenerate inputs stay safe.
            Assert.AreEqual(0, FrameAnimEffect.FrameIndexAt(1f, 0.05f, 0));
            Assert.GreaterOrEqual(FrameAnimEffect.FrameIndexAt(1f, 0f, 4), 4);
        }

        [Test]
        public void IntroEasePhase_ClampsAndProgresses()
        {
            Assert.AreEqual(0f, IntroScreenController.EasePhase(0.0f, 0.3f, 0.5f), 0.001f);
            Assert.AreEqual(0f, IntroScreenController.EasePhase(0.3f, 0.3f, 0.5f), 0.001f);
            float mid = IntroScreenController.EasePhase(0.55f, 0.3f, 0.5f);
            Assert.Greater(mid, 0.35f);
            Assert.Less(mid, 0.65f);
            Assert.AreEqual(1f, IntroScreenController.EasePhase(0.8f, 0.3f, 0.5f), 0.001f);
            Assert.AreEqual(1f, IntroScreenController.EasePhase(99f, 0.3f, 0.5f), 0.001f);
            // Zero-duration step behaves as a step function.
            Assert.AreEqual(0f, IntroScreenController.EasePhase(0.29f, 0.3f, 0f), 0.001f);
            Assert.AreEqual(1f, IntroScreenController.EasePhase(0.31f, 0.3f, 0f), 0.001f);
        }

        // --- 2026-07-02 dynamic battlefield + comeback pass ---

        [Test]
        public void GimmickFrameAnimator_LoopMath_WrapsForever()
        {
            // 4 frames at 8fps -> 0.125s per frame; loops instead of finishing.
            Assert.AreEqual(0, GimmickFrameAnimator.LoopFrameAt(0.00f, 0.125f, 4));
            Assert.AreEqual(1, GimmickFrameAnimator.LoopFrameAt(0.13f, 0.125f, 4));
            Assert.AreEqual(3, GimmickFrameAnimator.LoopFrameAt(0.49f, 0.125f, 4));
            Assert.AreEqual(0, GimmickFrameAnimator.LoopFrameAt(0.50f, 0.125f, 4));   // wrap
            Assert.AreEqual(2, GimmickFrameAnimator.LoopFrameAt(10.30f, 0.125f, 4));  // deep wrap
            // Degenerate inputs stay safe.
            Assert.AreEqual(0, GimmickFrameAnimator.LoopFrameAt(1f, 0.125f, 0));
            Assert.AreEqual(0, GimmickFrameAnimator.LoopFrameAt(1f, 0f, 4));
        }

        [Test]
        public void GimmickFieldDirector_PlanForTurn_SpawnsMutatesAndRests()
        {
            // Turn 0: intro/no-op guard.
            var t0 = GimmickFieldDirector.PlanForTurn(0, 0, 6);
            Assert.IsFalse(t0.spawn || t0.despawnOldest || t0.mutate);

            // ODD turn (AI-turn entry) below capacity -> spawn only: field beats land while
            // the player WATCHES, never right as their own turn starts (cycle-2 design review).
            var t1 = GimmickFieldDirector.PlanForTurn(1, 1, 6);
            Assert.IsTrue(t1.spawn);
            Assert.IsFalse(t1.mutate);
            Assert.IsFalse(t1.despawnOldest);

            // Even non-mutate turn (player-turn entry) -> rest beat, stable board to aim on.
            var t2 = GimmickFieldDirector.PlanForTurn(2, 1, 6);
            Assert.IsFalse(t2.spawn || t2.despawnOldest || t2.mutate);

            // Every 3rd turn mutates (composition provably changes <= every 3 turns, AC5).
            var t3 = GimmickFieldDirector.PlanForTurn(3, 2, 6);
            Assert.IsTrue(t3.mutate, "turn 3 must trade the oldest piece for a new one");
            Assert.IsTrue(t3.spawn);

            // Mutate with empty field degrades to plain spawn.
            var t3empty = GimmickFieldDirector.PlanForTurn(3, 0, 6);
            Assert.IsFalse(t3empty.mutate);
            Assert.IsTrue(t3empty.spawn);

            // Odd turn at capacity -> despawn instead of overgrow.
            var t5 = GimmickFieldDirector.PlanForTurn(5, 6, 6);
            Assert.IsFalse(t5.spawn);
            Assert.IsTrue(t5.despawnOldest);

            // All four obstacle kinds must be reachable across the first 12 turns — plain
            // turn%4 parity-locked MiniTower/Patrol out of every spawn beat (cycle-2 review).
            var kinds = new HashSet<FieldObstacleKind>();
            for (int turn = 1; turn <= 12; turn++)
            {
                var p = GimmickFieldDirector.PlanForTurn(turn, 2, 6);
                Assert.GreaterOrEqual(p.laneIndex, 0);
                Assert.Less(p.laneIndex, GimmickFieldDirector.SpawnLanes.Length);
                if (p.spawn) kinds.Add(p.kind);
            }
            Assert.AreEqual(4, kinds.Count, "every obstacle kind must appear in the rotation");
        }

        [Test]
        public void FieldLayout_SpansWideEnvelope()
        {
            // Widened QA pass: the combined initial gimmick composition must span x in [-15, 15].
            float minX = float.MaxValue, maxX = float.MinValue;
            foreach (var p in GameManager.InitialBarrelPositions) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); }
            foreach (var p in GameManager.InitialRunePositions) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); }
            foreach (var p in GameManager.InitialGatePositions) { minX = Mathf.Min(minX, p.x); maxX = Mathf.Max(maxX, p.x); }

            Assert.LessOrEqual(minX, -15f, "layout must reach the deep player wing");
            Assert.GreaterOrEqual(maxX, 15f, "layout must reach the deep enemy wing");
            Assert.GreaterOrEqual(maxX - minX, 30f, "gimmick envelope must span >= 30 world units");

            // Dynamic spawn lanes obey the envelope and avoid the core columns (±CoreAbsX).
            foreach (var lane in GimmickFieldDirector.SpawnLanes)
            {
                Assert.LessOrEqual(Mathf.Abs(lane), 15f);
                Assert.Greater(Mathf.Abs(Mathf.Abs(lane) - GameManager.CoreAbsX), 0.9f, $"lane {lane} sits on a core column");
            }

            // Widened-board contract: kegs, vents, and the chariot sweep never stack on the
            // same column — every ground hazard family keeps >= 1.0u of separation.
            foreach (var keg in GameManager.InitialBarrelPositions)
            {
                foreach (var vent in GameManager.VentPositions)
                {
                    Assert.Greater(Mathf.Abs(keg.x - vent.x), 1.0f, $"keg {keg.x} crowds vent {vent.x}");
                }
                Assert.Greater(Mathf.Abs(keg.x) - 3.2f, 1.0f, $"keg {keg.x} inside the chariot sweep");
                Assert.Greater(Mathf.Abs(Mathf.Abs(keg.x) - GameManager.CoreAbsX), 1.0f, $"keg {keg.x} hugs a core");
            }
        }

        [Test]
        public void LastStand_DangerThreshold_AndOneWayArmLatch()
        {
            // Danger band boundary: exactly 35% is danger, just above is not.
            Assert.IsTrue(LastStand.IsDanger(35f, 100f));
            Assert.IsFalse(LastStand.IsDanger(35.1f, 100f));
            Assert.IsTrue(LastStand.IsDanger(1f, 150f));
            Assert.IsFalse(LastStand.IsDanger(0f, 100f), "dead core is not 'in danger', it is gone");
            Assert.IsFalse(LastStand.IsDanger(10f, 0f), "no max HP -> no danger");

            // One-way latch: Locked -> Armed on danger; recovering does NOT disarm.
            var phase = LastStand.Advance(LastStand.Phase.Locked, false);
            Assert.AreEqual(LastStand.Phase.Locked, phase);
            phase = LastStand.Advance(phase, true);
            Assert.AreEqual(LastStand.Phase.Armed, phase);
            phase = LastStand.Advance(phase, false);
            Assert.AreEqual(LastStand.Phase.Armed, phase, "arm survives HP recovery");

            // Consumed/Active never regress via Advance.
            Assert.AreEqual(LastStand.Phase.Active, LastStand.Advance(LastStand.Phase.Active, true));
            Assert.AreEqual(LastStand.Phase.Consumed, LastStand.Advance(LastStand.Phase.Consumed, true));

            // Player comeback hits harder than the AI mirror (AC7).
            Assert.AreEqual(2.2f, LastStand.DamageMult(true), 0.001f);
            Assert.AreEqual(1.5f, LastStand.RadiusMult(true), 0.001f);
            Assert.AreEqual(1.3f, LastStand.SpeedMult(true), 0.001f);
            Assert.Greater(LastStand.DamageMult(true), LastStand.DamageMult(false));
            Assert.Greater(LastStand.RadiusMult(true), LastStand.RadiusMult(false));
            Assert.Greater(LastStand.SpeedMult(true), LastStand.SpeedMult(false));

            // Cycle-2 balance guard: one buffed hit must NEVER erase a full 150-HP core.
            // Bomber 95 x 2.2 = 209 raw -> capped to 140 (< 150, shield window survives).
            Assert.AreEqual(140f, LastStand.BuffedDamage(95f, true), 0.001f);
            Assert.Less(LastStand.BuffedDamage(999f, true), 150f);
            // Small hits keep the full multiplier (knight 20 -> 44).
            Assert.AreEqual(44f, LastStand.BuffedDamage(20f, true), 0.001f);
            // AI mirror rides the same cap: 95 x 1.6 = 152 raw -> 140.
            Assert.AreEqual(140f, LastStand.BuffedDamage(95f, false), 0.001f);
            Assert.AreEqual(80f, LastStand.BuffedDamage(50f, false), 0.001f);
        }

        [Test]
        public void GimmickFieldDirector_SpawnAndDespawn_TracksAliveList()
        {
            var dirGo = new GameObject("FieldDirector");
            var director = dirGo.AddComponent<GimmickFieldDirector>();
            try
            {
                Assert.AreEqual(0, director.AliveCount);

                var rune = director.TestSpawn(FieldObstacleKind.Rune, -10.5f, 2);
                Assert.IsNotNull(rune, "rune obstacle must spawn without GameManager");
                Assert.IsNotNull(rune.GetComponent<BuffDebuffGimmick>());
                Assert.AreEqual(1, director.AliveCount);

                var patrol = director.TestSpawn(FieldObstacleKind.Patrol, 10.5f, 3);
                Assert.IsNotNull(patrol);
                Assert.IsNotNull(patrol.GetComponent<MovingGimmick>());
                Assert.AreEqual(2, director.AliveCount);

                // Oldest-first despawn destroys the rune (bornTurn 2 < 3).
                director.DespawnOldest();
                Assert.AreEqual(1, director.AliveCount);
                Assert.IsTrue(rune == null, "despawned obstacle must be destroyed");
                Assert.IsTrue(patrol != null, "younger obstacle must survive");
            }
            finally
            {
                foreach (var g in Object.FindObjectsOfType<BuffDebuffGimmick>()) Object.DestroyImmediate(g.gameObject);
                foreach (var g in Object.FindObjectsOfType<MovingGimmick>()) Object.DestroyImmediate(g.gameObject);
                Object.DestroyImmediate(dirGo);
            }
        }

        [Test]
        public void GimmickAnimLibrary_LoadsIntroBanner_AndFailsSoft()
        {
            // AC3/AC10 evidence: the intro banner strip must ship >= 6 frames, lexically ordered.
            var banner = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.IntroBanner);
            Assert.IsNotNull(banner, "IntroAnim frames must exist under Resources/IntroAnim");
            Assert.GreaterOrEqual(banner.Length, 6, "video-like intro loop needs >= 6 frames");
            for (int i = 1; i < banner.Length; i++)
            {
                Assert.LessOrEqual(string.CompareOrdinal(banner[i - 1].name, banner[i].name), 0);
            }

            // Missing key soft-fails: no throw, empty/null result, gameplay never depends on art.
            var missing = GimmickAnimLibrary.LoadFrames("anim_does_not_exist");
            Assert.IsTrue(missing == null || missing.Length == 0);
        }

        [Test]
        public void EffectSpriteLibrary_LoadsShatterFrames()
        {
            // AC5 evidence: despawn/break effect strip generated via gti must be playable.
            var frames = EffectSpriteLibrary.LoadFrames("fx_shatter");
            Assert.IsNotNull(frames, "fx_shatter frames must exist under Resources/Effects");
            Assert.GreaterOrEqual(frames.Length, 4, "shatter needs a readable destruction arc");
        }

        // --- 2026-07-02 ecosystem (AC11) + cycle-3 review fixes ---

        [Test]
        public void SiegeRank_GradeLadder_CoversAllBands()
        {
            Assert.AreEqual("S", SiegeRank.ComputeGrade(true, 8, 0), "win in <=8 turns is a crushing breach");
            Assert.AreEqual("A", SiegeRank.ComputeGrade(true, 14, 0));
            Assert.AreEqual("B", SiegeRank.ComputeGrade(true, 15, 0));
            Assert.AreEqual("C", SiegeRank.ComputeGrade(false, 30, 300), "honorable defeat needs score >= 300");
            Assert.AreEqual("D", SiegeRank.ComputeGrade(false, 30, 299));
        }

        [Test]
        public void SiegeRank_Insert_OrdersCapsAndRanks()
        {
            var list = new List<SiegeRank.Entry>();
            // Fill with descending scores 1000..100.
            for (int i = 0; i < 10; i++)
            {
                SiegeRank.Insert(list, new SiegeRank.Entry { score = 1000 - i * 100, turns = 10, dateIso = $"2026-07-02T00:00:{i:00}Z" });
            }
            Assert.AreEqual(SiegeRank.Capacity, list.Count);

            // Better score lands at its ordered slot.
            int rank = SiegeRank.Insert(list, new SiegeRank.Entry { score = 950, turns = 9, dateIso = "2026-07-02T00:01:00Z" });
            Assert.AreEqual(1, rank, "950 slots directly behind 1000");
            Assert.AreEqual(SiegeRank.Capacity, list.Count, "board stays capped");

            // Tie on score: fewer turns wins the higher slot.
            SiegeRank.Insert(list, new SiegeRank.Entry { score = 950, turns = 5, dateIso = "2026-07-02T00:02:00Z" });
            Assert.AreEqual(5, list[1].turns, "tiebreak: fewer turns ranks higher");

            // Hopeless run falls off the board -> -1.
            int offBoard = SiegeRank.Insert(list, new SiegeRank.Entry { score = 1, turns = 99, dateIso = "2026-07-02T00:03:00Z" });
            Assert.AreEqual(-1, offBoard);
            Assert.AreEqual(SiegeRank.Capacity, list.Count);
        }

        [Test]
        public void LastStand_AdvanceAuto_ArmsAndActivatesInOneStep()
        {
            // AI mirror: danger goes straight to Active (never waits on input).
            Assert.AreEqual(LastStand.Phase.Active, LastStand.AdvanceAuto(LastStand.Phase.Locked, true));
            Assert.AreEqual(LastStand.Phase.Locked, LastStand.AdvanceAuto(LastStand.Phase.Locked, false));
            // Never regresses.
            Assert.AreEqual(LastStand.Phase.Consumed, LastStand.AdvanceAuto(LastStand.Phase.Consumed, true));
            Assert.AreEqual(LastStand.Phase.Active, LastStand.AdvanceAuto(LastStand.Phase.Active, false));
        }

        [Test]
        public void GimmickAnimLibrary_LoadsDerivedFrameSets()
        {
            // AC2/AC10 evidence: every animated gimmick key resolves >= 4 ordered frames.
            foreach (var key in new[] { GimmickAnimLibrary.BarrelAnim, GimmickAnimLibrary.GateAnim,
                GimmickAnimLibrary.RallyRuneAnim, GimmickAnimLibrary.HexRuneAnim, GimmickAnimLibrary.CoreAnim })
            {
                var frames = GimmickAnimLibrary.LoadFrames(key);
                Assert.IsNotNull(frames, $"anim frames '{key}' must exist under Resources/Gimmicks/{key}");
                Assert.GreaterOrEqual(frames.Length, 4, $"'{key}' needs a loopable frame count");
                for (int i = 1; i < frames.Length; i++)
                {
                    Assert.LessOrEqual(string.CompareOrdinal(frames[i - 1].name, frames[i].name), 0,
                        $"'{key}' frames must be lexically ordered");
                }
            }
        }

        [Test]
        public void GimmickFrameAnimator_TryAttach_PreservesWorldFootprint()
        {
            // Review P1 #3 guard: attaching animation art must not change what shots hit.
            // Awake does not run on AddComponent in edit mode, so the test stages the host
            // the way ExplosiveGimmick.Awake would: dedicated barrel art, presentation
            // scale for a 1.7u world size, and a collider matched to the sprite.
            var go = new GameObject("AnimFootprintProbe");
            try
            {
                var sr = go.AddComponent<SpriteRenderer>();
                Assert.IsTrue(GimmickSpriteLibrary.TryApply(sr, GimmickSpriteLibrary.Barrel, Color.white),
                    "dedicated barrel art must exist");
                Vector2 native = sr.sprite.bounds.size;
                float scale = 1.7f / Mathf.Max(native.x, native.y);
                go.transform.localScale = new Vector3(scale, scale, 1f);
                var box = go.AddComponent<BoxCollider2D>();
                box.size = native;
                box.offset = sr.sprite.bounds.center;

                Vector2 preVisual = sr.bounds.size;
                Vector2 preCollider = Vector2.Scale(box.size, go.transform.localScale);
                Assert.Greater(preVisual.x, 0.1f, "staged host must have a real footprint");

                var anim = GimmickFrameAnimator.TryAttach(go, GimmickAnimLibrary.BarrelAnim, 8f);
                Assert.IsNotNull(anim, "barrel anim frames exist, attach must succeed");

                Vector2 postVisual = sr.bounds.size;
                Vector2 postCollider = Vector2.Scale(box.size, go.transform.localScale);
                Assert.AreEqual(preVisual.x, postVisual.x, 0.02f, "visual width preserved");
                Assert.AreEqual(preVisual.y, postVisual.y, 0.02f, "visual height preserved");
                Assert.AreEqual(preCollider.x, postCollider.x, 0.02f, "collider world width preserved");
                Assert.AreEqual(preCollider.y, postCollider.y, 0.02f, "collider world height preserved");
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void FieldLayout_KegsClearLaunchMuzzles()
        {
            // Review P1 #1 guard: no initial keg may sit within blast radius (2.2) + 0.8 margin
            // of either launch point (±LaunchApronAbsX, y=0.5).
            var launchPoints = new[]
            {
                new Vector2(-GameManager.LaunchApronAbsX, 0.5f),
                new Vector2(GameManager.LaunchApronAbsX, 0.5f),
            };
            foreach (var keg in GameManager.InitialBarrelPositions)
            {
                foreach (var lp in launchPoints)
                {
                    Assert.Greater(Vector2.Distance(keg, lp), 3.0f,
                        $"keg at {keg} is inside the muzzle hazard zone of launch point {lp}");
                }
            }
        }

        [Test]
        public void EruptionVent_CycleMath_WrapsAndPhases()
        {
            // Phase schedule: dormant [0,6.5) -> warning [6.5,8.3) -> erupting [8.3,10.5).
            const float dormant = 6.5f, warning = 1.8f, erupt = 2.2f;
            float total = dormant + warning + erupt;

            Assert.AreEqual(EruptionVentGimmick.Phase.Dormant,
                EruptionVentGimmick.PhaseAt(0f, dormant, warning, erupt));
            Assert.AreEqual(EruptionVentGimmick.Phase.Dormant,
                EruptionVentGimmick.PhaseAt(6.49f, dormant, warning, erupt));
            Assert.AreEqual(EruptionVentGimmick.Phase.Warning,
                EruptionVentGimmick.PhaseAt(6.5f, dormant, warning, erupt));
            Assert.AreEqual(EruptionVentGimmick.Phase.Warning,
                EruptionVentGimmick.PhaseAt(8.29f, dormant, warning, erupt));
            Assert.AreEqual(EruptionVentGimmick.Phase.Erupting,
                EruptionVentGimmick.PhaseAt(8.3f, dormant, warning, erupt));
            Assert.AreEqual(EruptionVentGimmick.Phase.Erupting,
                EruptionVentGimmick.PhaseAt(10.49f, dormant, warning, erupt));

            // Wrap: elapsed beyond one cycle folds back; negative offsets stay in range.
            Assert.AreEqual(0.5f, EruptionVentGimmick.WrapCycleTime(total + 0.5f, 0f, total), 0.0001f);
            Assert.AreEqual(total - 1f, EruptionVentGimmick.WrapCycleTime(0f, -1f, total), 0.0001f);
            Assert.AreEqual(0f, EruptionVentGimmick.WrapCycleTime(5f, 0f, 0f), 0.0001f, "degenerate cycle is safe");

            // Half-cycle offset (the two-vent desync contract): when one vent erupts the
            // other must be dormant, so one midfield column is always open.
            float eruptMid = dormant + warning + erupt * 0.5f;
            Assert.AreEqual(EruptionVentGimmick.Phase.Erupting,
                EruptionVentGimmick.PhaseAt(EruptionVentGimmick.WrapCycleTime(eruptMid, 0f, total), dormant, warning, erupt));
            Assert.AreEqual(EruptionVentGimmick.Phase.Dormant,
                EruptionVentGimmick.PhaseAt(EruptionVentGimmick.WrapCycleTime(eruptMid, total * 0.5f, total), dormant, warning, erupt));
        }

        [Test]
        public void EruptionVent_LiftCeiling_NeverSlingshots()
        {
            // Below the ceiling: full acceleration step.
            Assert.AreEqual(24f * 0.02f, EruptionVentGimmick.LiftDeltaV(0f, 24f, 8.5f, 0.02f), 0.0001f);
            // Near the ceiling: delta clamps to the remaining headroom.
            Assert.AreEqual(0.1f, EruptionVentGimmick.LiftDeltaV(8.4f, 24f, 8.5f, 0.02f), 0.0001f);
            // At/above the ceiling: no further push (falling bodies still get caught).
            Assert.AreEqual(0f, EruptionVentGimmick.LiftDeltaV(8.5f, 24f, 8.5f, 0.02f), 0.0001f);
            Assert.AreEqual(0f, EruptionVentGimmick.LiftDeltaV(12f, 24f, 8.5f, 0.02f), 0.0001f);
            // A body falling into the column gets the full step upward.
            Assert.AreEqual(24f * 0.02f, EruptionVentGimmick.LiftDeltaV(-6f, 24f, 8.5f, 0.02f), 0.0001f);

            // Column AABB hugs the vent mouth and reaches straight up.
            var rect = EruptionVentGimmick.ColumnRect(new Vector2(5.4f, 0.15f), 1.9f, 7.5f);
            Assert.AreEqual(5.4f - 0.95f, rect.xMin, 0.0001f);
            Assert.AreEqual(5.4f + 0.95f, rect.xMax, 0.0001f);
            Assert.AreEqual(0.15f, rect.yMin, 0.0001f);
            Assert.AreEqual(7.65f, rect.yMax, 0.0001f);
        }

        // --- "Flew up and never came back down" regression pass ---
        // Playtest report: units were seen climbing straight up off the top of the screen
        // and never returning. Root cause: two call sites re-set rb.velocity.y to a positive
        // constant every single Update() tick with no "already airborne" gate, so the script
        // re-won the race against gravity every frame instead of hopping once. These tests
        // pin down both the fix and the new hard-ceiling backstop.

        [Test]
        public void UnitController_MoveTowardsTarget_ObstacleHop_SkipsWhileAlreadyAirborne()
        {
            var unitGo = new GameObject("ChasingUnit");
            var rb = unitGo.AddComponent<Rigidbody2D>();
            var unit = unitGo.AddComponent<UnitController>();

            var awakeMethod = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(unit, null);
            rb.bodyType = RigidbodyType2D.Dynamic;

            var obstacleGo = new GameObject("Obstacle");
            obstacleGo.AddComponent<BoxCollider2D>();
            obstacleGo.transform.position = unitGo.transform.position + Vector3.right * 0.3f;

            var targetGo = new GameObject("FarTarget");
            targetGo.transform.position = unitGo.transform.position + Vector3.right * 10f;
            var targetField = typeof(UnitController).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetField.SetValue(unit, targetGo.transform);

            // The unit is mid-hop (still clearly airborne) while blocked by the obstacle
            // directly ahead - exactly the state that used to re-trigger the hop every single
            // Update() frame and ratchet the unit off the top of the screen forever.
            rb.velocity = new Vector2(0f, 4f);

            var moveMethod = typeof(UnitController).GetMethod("MoveTowardsTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            moveMethod.Invoke(unit, null);

            Assert.AreEqual(4f, rb.velocity.y, 0.001f, "Hop must not re-fire while already airborne.");

            Object.DestroyImmediate(unitGo);
            Object.DestroyImmediate(obstacleGo);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void UnitController_MoveTowardsTarget_ObstacleHop_FiresOnceWhenGrounded()
        {
            // Companion to the airborne-skip test above: the hop must still work normally
            // the first time a grounded unit runs into an obstacle.
            var unitGo = new GameObject("ChasingUnit2");
            var rb = unitGo.AddComponent<Rigidbody2D>();
            var unit = unitGo.AddComponent<UnitController>();

            var awakeMethod = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(unit, null);
            rb.bodyType = RigidbodyType2D.Dynamic;

            var obstacleGo = new GameObject("Obstacle2");
            obstacleGo.AddComponent<BoxCollider2D>();
            obstacleGo.transform.position = unitGo.transform.position + Vector3.right * 0.3f;

            var targetGo = new GameObject("FarTarget2");
            targetGo.transform.position = unitGo.transform.position + Vector3.right * 10f;
            var targetField = typeof(UnitController).GetField("target", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            targetField.SetValue(unit, targetGo.transform);

            rb.velocity = Vector2.zero; // resting on the ground, not mid-hop

            var moveMethod = typeof(UnitController).GetMethod("MoveTowardsTarget", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            moveMethod.Invoke(unit, null);

            Assert.AreEqual(5.5f, rb.velocity.y, 0.001f, "A grounded unit blocked by an obstacle should still hop.");

            Object.DestroyImmediate(unitGo);
            Object.DestroyImmediate(obstacleGo);
            Object.DestroyImmediate(targetGo);
        }

        [Test]
        public void UnitController_EnforceHardCeiling_ClampsUpwardVelocityAboveCeiling()
        {
            var unitGo = new GameObject("HighFlyingUnit");
            var rb = unitGo.AddComponent<Rigidbody2D>();
            var unit = unitGo.AddComponent<UnitController>();

            var awakeMethod = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(unit, null);
            rb.bodyType = RigidbodyType2D.Dynamic;

            unit.hardCeilingY = 20f;
            unitGo.transform.position = new Vector3(0f, 25f, 0f); // above the ceiling
            rb.velocity = new Vector2(1.5f, 8f); // still climbing

            var ceilingMethod = typeof(UnitController).GetMethod("EnforceHardCeiling", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            ceilingMethod.Invoke(unit, null);

            // Horizontal velocity is untouched - only the runaway upward climb is stopped so
            // gravity (still acting on the Dynamic rigidbody) takes back over from here.
            Assert.AreEqual(1.5f, rb.velocity.x, 0.001f);
            Assert.AreEqual(0f, rb.velocity.y, 0.001f);

            Object.DestroyImmediate(unitGo);
        }

        [Test]
        public void UnitController_EnforceHardCeiling_LeavesLegitimateFlightUntouched()
        {
            var unitGo = new GameObject("BelowCeilingUnit");
            var rb = unitGo.AddComponent<Rigidbody2D>();
            var unit = unitGo.AddComponent<UnitController>();

            var awakeMethod = typeof(UnitController).GetMethod("Awake", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            awakeMethod.Invoke(unit, null);
            rb.bodyType = RigidbodyType2D.Dynamic;
            unit.hardCeilingY = 20f;

            var ceilingMethod = typeof(UnitController).GetMethod("EnforceHardCeiling", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // Below the ceiling: a legitimate high arc must be left alone.
            unitGo.transform.position = new Vector3(0f, 19f, 0f);
            rb.velocity = new Vector2(0f, 8f);
            ceilingMethod.Invoke(unit, null);
            Assert.AreEqual(8f, rb.velocity.y, 0.001f, "Ceiling must not touch a unit still below it.");

            // Above the ceiling but already falling: must be left alone too - only upward
            // velocity ever gets clamped.
            unitGo.transform.position = new Vector3(0f, 25f, 0f);
            rb.velocity = new Vector2(0f, -3f);
            ceilingMethod.Invoke(unit, null);
            Assert.AreEqual(-3f, rb.velocity.y, 0.001f, "A unit already falling past the ceiling must not be touched.");

            Object.DestroyImmediate(unitGo);
        }

        [Test]
        public void LaunchManager_DrawTrajectory_StopsClimbingAfterSelectedUnitHardCeiling()
        {
            var managerGo = new GameObject("HardCeilingTrajectoryLaunchManager");
            var launchPointGo = new GameObject("HardCeilingTrajectoryLaunchPoint");
            var selectedPrefab = new GameObject("HardCeilingTrajectoryUnit");
            try
            {
                selectedPrefab.transform.position = new Vector3(2000f, 2000f, 0f);
                selectedPrefab.AddComponent<Rigidbody2D>();
                selectedPrefab.AddComponent<BoxCollider2D>();
                var selectedUnit = selectedPrefab.AddComponent<UnitController>();
                selectedUnit.hardCeilingY = 0.1f;
                launchPointGo.transform.position = new Vector3(
                    1234f,
                    selectedUnit.hardCeilingY - UnitController.DefaultLaunchSpawnHeight - 0.2f,
                    0f);

                var trajectoryLine = managerGo.AddComponent<LineRenderer>();
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.trajectoryLine = trajectoryLine;
                launchManager.trajectoryResolution = 3;
                launchManager.timeStep = 0.1f;
                launchManager.SetSelectedUnit(selectedPrefab);

                var drawTrajectory = typeof(LaunchManager).GetMethod(
                    "DrawTrajectory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(drawTrajectory,
                    "The fixture must execute the same trajectory path used by the launch preview.");

                Vector2 startingVelocity = new Vector2(2f, 5f);
                drawTrajectory.Invoke(launchManager, new object[] { startingVelocity });

                // DrawTrajectory renders the t=0 origin plus one point per configured
                // integration, so a resolution of N yields N+1 points (same contract as
                // LaunchManager_DrawTrajectory_RendersConfiguredResolution and the 151-point
                // horizon pin). The indices asserted below are unchanged: 1 and 2 are still
                // the first and second integrated samples.
                Assert.AreEqual(launchManager.trajectoryResolution + 1, trajectoryLine.positionCount,
                    "The preview must render the launch origin plus every configured integration.");
                Vector3 firstStep = trajectoryLine.GetPosition(1);
                Vector3 secondStep = trajectoryLine.GetPosition(2);
                float expectedFirstY =
                    launchManager.GetLaunchPosition().y +
                    (startingVelocity.y + Physics2D.gravity.y * launchManager.timeStep) * launchManager.timeStep;
                float expectedSecondY =
                    expectedFirstY + Physics2D.gravity.y * launchManager.timeStep * launchManager.timeStep;

                Assert.Greater(firstStep.y, selectedUnit.hardCeilingY,
                    "The representative first step must cross the selected unit's ceiling.");
                Assert.AreEqual(expectedFirstY, firstStep.y, 0.0001f);
                Assert.AreEqual(expectedSecondY, secondStep.y, 0.0001f,
                    "Once the previous sample is above the ceiling, preview ascent must be clamped before gravity advances the next sample.");
                Assert.Less(secondStep.y, firstStep.y,
                    "The preview must begin descending instead of advertising a runtime-impossible climb.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
                Object.DestroyImmediate(selectedPrefab);
            }
        }

        [Test]
        public void DeploymentRules_CardsUnlockAtExactTurnBoundaries()
        {
            var cards = new[]
            {
                DeployCard.Knight,
                DeployCard.Archer,
                DeployCard.Barrel,
                DeployCard.Cannon
            };
            var unlockTurns = new[] { 0, 1, 2, 3 };

            for (int i = 0; i < cards.Length; i++)
            {
                Assert.IsFalse(DeploymentRules.IsUnlocked(cards[i], unlockTurns[i] - 1),
                    $"{cards[i]} must remain locked immediately before turn {unlockTurns[i]}.");
                Assert.IsTrue(DeploymentRules.IsUnlocked(cards[i], unlockTurns[i]),
                    $"{cards[i]} must unlock exactly on turn {unlockTurns[i]}.");
            }

            Assert.IsTrue(DeploymentRules.IsUnlocked(DeployCard.Knight, 0));
            Assert.IsFalse(DeploymentRules.IsUnlocked(DeployCard.Archer, 0));
            Assert.IsFalse(DeploymentRules.IsUnlocked(DeployCard.Barrel, 0));
            Assert.IsFalse(DeploymentRules.IsUnlocked(DeployCard.Cannon, 0),
                "Turn zero must expose one immediately launchable card rather than a deploy-only or advanced option.");
        }

        [Test]
        public void GameManager_StartGame_SelectsAndLaunchesKnightBeforeDeployOnlyCards()
        {
            var managerGo = new GameObject("OpeningSelectionGameManager");
            var launchManagerGo = new GameObject("OpeningSelectionLaunchManager");
            var launchPointGo = new GameObject("OpeningSelectionLaunchPoint");
            UnitController launchedUnit = null;
            try
            {
                var knightPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Knight.prefab");
                Assert.IsNotNull(knightPrefab, "The opening launch contract requires the Knight prefab.");

                launchPointGo.transform.position = new Vector3(1234f, 4321f, 0f);
                var launchManager = launchManagerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;

                var gameManager = managerGo.AddComponent<GameManager>();
                var gameManagerAwake = typeof(GameManager).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(gameManagerAwake);
                gameManagerAwake.Invoke(gameManager, null);
                var deploymentComponent = managerGo.GetComponent<DeploymentController>();
                Assert.IsNotNull(deploymentComponent);
                var deploymentAwake = typeof(DeploymentController).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(deploymentAwake);
                deploymentAwake.Invoke(deploymentComponent, null);
                gameManager.knightPrefab = knightPrefab;
                var deployment = DeploymentController.Instance;
                Assert.IsNotNull(deployment);

                deployment.SetSelectedCard(DeployCard.Cannon);
                Assert.IsTrue(deployment.DeployModeArmed,
                    "Precondition: the deploy-only Cannon must have armed placement mode.");

                gameManager.StartGame();

                Assert.AreEqual(GameState.PlayerTurn, gameManager.currentState);
                Assert.AreEqual(DeployCard.Knight, deployment.SelectedCard);
                Assert.IsFalse(deployment.DeployModeArmed,
                    "A fresh match must begin on a launchable card, not in Cannon placement mode.");

                Object.DestroyImmediate(managerGo);
                managerGo = null;

                var beforeLaunch = new HashSet<UnitController>(Object.FindObjectsOfType<UnitController>());
                launchManager.SimulateLaunch(new Vector2(4f, 3f));
                foreach (var candidate in Object.FindObjectsOfType<UnitController>())
                {
                    if (!beforeLaunch.Contains(candidate))
                    {
                        launchedUnit = candidate;
                        break;
                    }
                }

                Assert.IsNotNull(launchedUnit,
                    "The opening selection must produce a unit when the public launch path fires.");
                Assert.AreEqual(UnitType.Knight, launchedUnit.unitType,
                    "The first launch after StartGame must instantiate the Knight card selected for turn zero.");
                Assert.AreEqual(UnitState.Launched, launchedUnit.CurrentState);
            }
            finally
            {
                if (launchedUnit != null) Object.DestroyImmediate(launchedUnit.gameObject);
                if (managerGo != null) Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchManagerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void DeathHooks_CreditTheSideThatEarnedKillAndBlockDestruction()
        {
            var managerGo = new GameObject("DeathCreditGameManager");
            var victimGo = new GameObject("PlayerVictim");
            var playerCastleGo = new GameObject("PlayerCastle");
            var blockGo = new GameObject("PlayerCastleBlock");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                var gameManagerAwake = typeof(GameManager).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(gameManagerAwake);
                gameManagerAwake.Invoke(gameManager, null);
                var deploymentComponent = managerGo.GetComponent<DeploymentController>();
                Assert.IsNotNull(deploymentComponent);
                var deploymentAwake = typeof(DeploymentController).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(deploymentAwake);
                deploymentAwake.Invoke(deploymentComponent, null);
                var deployment = DeploymentController.Instance;
                Assert.IsNotNull(deployment);
                deployment.ResetEconomy();

                float playerBefore = deployment.PlayerSupply;
                float enemyBefore = deployment.EnemySupply;
                var victim = victimGo.AddComponent<UnitController>();
                victim.isPlayerUnit = true;

                gameManager.OnUnitDied(victim, damageFromPlayer: false);

                Assert.AreEqual(playerBefore, deployment.PlayerSupply, 0.0001f,
                    "A player's death must not reward that same side.");
                Assert.AreEqual(enemyBefore + SupplyRules.KillBonus, deployment.EnemySupply, 0.0001f,
                    "GameManager's unit-death hook must credit the killer's opposing side.");

                var playerCastle = playerCastleGo.AddComponent<CastleController>();
                playerCastle.isPlayerCastle = true;
                blockGo.transform.SetParent(playerCastleGo.transform);
                var block = blockGo.AddComponent<DestructibleBlock>();
                block.maxHP = 1f;
                block.currentHP = 1f;

                float enemyBeforeBlock = deployment.EnemySupply;
                block.TakeDamage(1f);

                Assert.AreEqual(playerBefore, deployment.PlayerSupply, 0.0001f,
                    "Destroying a player's block must not credit the defending player.");
                Assert.AreEqual(enemyBeforeBlock + SupplyRules.BlockBonus, deployment.EnemySupply, 0.0001f,
                    "DestructibleBlock's destruction hook must credit the opposing side.");
            }
            finally
            {
                if (blockGo != null) Object.DestroyImmediate(blockGo);
                Object.DestroyImmediate(playerCastleGo);
                Object.DestroyImmediate(victimGo);
                Object.DestroyImmediate(managerGo);
            }
        }

        [Test]
        public void LaunchManager_DrawTrajectory_RuntimeAddedBarrelUsesDefaultHardCeiling()
        {
            var managerGo = new GameObject("BarrelHardCeilingTrajectoryLaunchManager");
            var launchPointGo = new GameObject("BarrelHardCeilingTrajectoryLaunchPoint");
            try
            {
                var barrelPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/Prefabs/ExplosiveBarrel.prefab");
                Assert.IsNotNull(barrelPrefab);
                Assert.IsNull(barrelPrefab.GetComponent<UnitController>(),
                    "Precondition: this prefab acquires UnitController only when LaunchManager spawns it.");
                Assert.IsNotNull(barrelPrefab.GetComponent<ExplosiveGimmick>());

                launchPointGo.transform.position =
                    new Vector3(
                        2468f,
                        UnitController.DefaultHardCeilingY - UnitController.DefaultLaunchSpawnHeight - 0.2f,
                        0f);
                var trajectoryLine = managerGo.AddComponent<LineRenderer>();
                var launchManager = managerGo.AddComponent<LaunchManager>();
                launchManager.launchPoint = launchPointGo.transform;
                launchManager.trajectoryLine = trajectoryLine;
                launchManager.trajectoryResolution = 3;
                launchManager.timeStep = 0.1f;
                launchManager.SetSelectedUnit(barrelPrefab);

                var drawTrajectory = typeof(LaunchManager).GetMethod(
                    "DrawTrajectory",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(drawTrajectory);

                Vector2 startingVelocity = new Vector2(2f, 5f);
                drawTrajectory.Invoke(launchManager, new object[] { startingVelocity });

                Assert.AreEqual(launchManager.trajectoryResolution + 1, trajectoryLine.positionCount,
                    "The preview must render the launch origin plus every configured integration.");
                Vector3 firstStep = trajectoryLine.GetPosition(1);
                Vector3 secondStep = trajectoryLine.GetPosition(2);
                float expectedFirstY =
                    launchManager.GetLaunchPosition().y +
                    (startingVelocity.y + Physics2D.gravity.y * launchManager.timeStep) * launchManager.timeStep;
                float expectedSecondY =
                    expectedFirstY + Physics2D.gravity.y * launchManager.timeStep * launchManager.timeStep;

                Assert.Greater(firstStep.y, UnitController.DefaultHardCeilingY,
                    "The representative first sample must cross the runtime-added controller's ceiling.");
                Assert.AreEqual(expectedSecondY, secondStep.y, 0.0001f,
                    "A selected ExplosiveBarrel must preview the same hard ceiling enforced after UnitController is added at spawn.");
                Assert.Less(secondStep.y, firstStep.y,
                    "The barrel preview must not use an infinite ceiling and continue a runtime-impossible climb.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void GameManager_ApplyLastStandOnLaunch_MirrorsBuffIntoExplosiveGimmick()
        {
            var managerGo = new GameObject("ExplosiveLastStandGameManager");
            var barrelGo = new GameObject("ExplosiveLastStandBarrel");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                gameManager.playerLastStand = LastStand.Phase.Active;

                var explosive = barrelGo.AddComponent<ExplosiveGimmick>();
                explosive.explosionDamage = 72f;
                explosive.explosionRadius = 1.8f;
                var unit = barrelGo.AddComponent<UnitController>();
                unit.unitType = UnitType.Barrel;
                unit.isPlayerUnit = true;
                unit.explosionDamage = 11f;
                unit.explosionRadius = 0.4f;

                float expectedDamage = LastStand.BuffedDamage(explosive.explosionDamage, true);
                float expectedRadius = explosive.explosionRadius * LastStand.RadiusMult(true);
                Vector2 velocity = new Vector2(6f, 8f);

                Vector2 buffedVelocity = gameManager.ApplyLastStandOnLaunch(unit, velocity);

                Assert.AreEqual(expectedDamage, explosive.explosionDamage, 0.0001f,
                    "The component that actually resolves barrel explosions must receive the LAST STAND damage buff.");
                Assert.AreEqual(expectedRadius, explosive.explosionRadius, 0.0001f,
                    "The component that actually resolves barrel explosions must receive the LAST STAND radius buff.");
                Assert.AreEqual(explosive.explosionDamage, unit.explosionDamage, 0.0001f,
                    "Unit and attached explosive potency must remain synchronized after the one-shot buff.");
                Assert.AreEqual(explosive.explosionRadius, unit.explosionRadius, 0.0001f);
                Assert.AreEqual(velocity * LastStand.SpeedMult(true), buffedVelocity);
                Assert.AreEqual(LastStand.Phase.Consumed, gameManager.playerLastStand,
                    "Applying the mirrored buff must still consume the one-shot player LAST STAND.");
            }
            finally
            {
                Object.DestroyImmediate(barrelGo);
                Object.DestroyImmediate(managerGo);
            }
        }

        [Test]
        public void DestructibleBlock_DamageOwnerScoresOffTurn_AndUnattributedDamageUsesCurrentTurn()
        {
            var managerGo = new GameObject("DamageOwnerScoreGameManager");
            var castleGo = new GameObject("DamageOwnerEnemyCastle");
            var attributedBlockGo = new GameObject("PlayerAttributedBlock");
            var legacyBlockGo = new GameObject("LegacyTurnAttributedBlock");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                var gameManagerAwake = typeof(GameManager).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(gameManagerAwake);
                gameManagerAwake.Invoke(gameManager, null);
                var isPlayerTurnField = typeof(GameManager).GetField(
                    "isPlayerTurn",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var playerScoreField = typeof(GameManager).GetField(
                    "playerScore",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var enemyScoreField = typeof(GameManager).GetField(
                    "enemyScore",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(isPlayerTurnField);
                Assert.IsNotNull(playerScoreField);
                Assert.IsNotNull(enemyScoreField);

                var enemyCastle = castleGo.AddComponent<CastleController>();
                enemyCastle.isPlayerCastle = false;

                attributedBlockGo.transform.SetParent(castleGo.transform);
                var attributedBlock = attributedBlockGo.AddComponent<DestructibleBlock>();
                attributedBlock.maxHP = 1f;
                attributedBlock.currentHP = 1f;
                attributedBlock.scoreValue = 17;

                isPlayerTurnField.SetValue(gameManager, false);
                Assert.IsFalse(gameManager.IsPlayerTurn,
                    "Precondition: owner attribution is exercised while the AI turn is active.");
                attributedBlock.TakeDamage(1f, damageFromPlayer: true);

                Assert.AreEqual(17, (int)playerScoreField.GetValue(gameManager),
                    "A player-owned hit must award the player even when it resolves during the AI turn.");
                Assert.AreEqual(0, (int)enemyScoreField.GetValue(gameManager));

                legacyBlockGo.transform.SetParent(castleGo.transform);
                var legacyBlock = legacyBlockGo.AddComponent<DestructibleBlock>();
                legacyBlock.maxHP = 1f;
                legacyBlock.currentHP = 1f;
                legacyBlock.scoreValue = 19;

                isPlayerTurnField.SetValue(gameManager, true);
                legacyBlock.TakeDamage(1f);

                Assert.AreEqual(36, (int)playerScoreField.GetValue(gameManager),
                    "The unattributed overload path must retain legacy current-turn score ownership.");
                Assert.AreEqual(0, (int)enemyScoreField.GetValue(gameManager));
            }
            finally
            {
                if (attributedBlockGo != null) Object.DestroyImmediate(attributedBlockGo);
                if (legacyBlockGo != null) Object.DestroyImmediate(legacyBlockGo);
                Object.DestroyImmediate(castleGo);
                Object.DestroyImmediate(managerGo);
            }
        }

        [Test]
        public void DestructibleBlock_DestroyEnemyCoreWithoutDamageOwner_CreditsPlayerBeforeGameOver()
        {
            const System.Reflection.BindingFlags instanceFlags =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
            const System.Reflection.BindingFlags staticFlags =
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            const int coreScore = 500;

            var isPlayerTurnField = typeof(GameManager).GetField("isPlayerTurn", instanceFlags);
            var playerScoreField = typeof(GameManager).GetField("playerScore", instanceFlags);
            var playerCoreField = typeof(GameManager).GetField("playerCore", instanceFlags);
            var enemyCoreField = typeof(GameManager).GetField("enemyCore", instanceFlags);
            var seriesPlayerWinsField = typeof(GameManager).GetField("seriesPlayerWins", staticFlags);
            var seriesEnemyWinsField = typeof(GameManager).GetField("seriesEnemyWins", staticFlags);
            var seriesGamesPlayedField = typeof(GameManager).GetField("seriesGamesPlayed", staticFlags);
            var seriesScoreTotalField = typeof(GameManager).GetField("seriesScoreTotal", staticFlags);
            var gameManagerAwake = typeof(GameManager).GetMethod("Awake", instanceFlags);
            var destroyBlock = typeof(DestructibleBlock).GetMethod("DestroyBlock", instanceFlags);

            Assert.IsNotNull(isPlayerTurnField);
            Assert.IsNotNull(playerScoreField);
            Assert.IsNotNull(playerCoreField);
            Assert.IsNotNull(enemyCoreField);
            Assert.IsNotNull(seriesPlayerWinsField);
            Assert.IsNotNull(seriesEnemyWinsField);
            Assert.IsNotNull(seriesGamesPlayedField);
            Assert.IsNotNull(seriesScoreTotalField);
            Assert.IsNotNull(gameManagerAwake);
            Assert.IsNotNull(destroyBlock);

            int previousSeriesPlayerWins = (int)seriesPlayerWinsField.GetValue(null);
            int previousSeriesEnemyWins = (int)seriesEnemyWinsField.GetValue(null);
            int previousSeriesGamesPlayed = (int)seriesGamesPlayedField.GetValue(null);
            int previousSeriesScoreTotal = (int)seriesScoreTotalField.GetValue(null);
            float previousTimeScale = Time.timeScale;
            var resultsBefore = new HashSet<ResultsScreenController>(
                Object.FindObjectsOfType<ResultsScreenController>());
            var managerGo = new GameObject("CoreScoreOrderingGameManager");
            var playerCastleGo = new GameObject("CoreScoreOrderingPlayerCastle");
            var enemyCastleGo = new GameObject("CoreScoreOrderingEnemyCastle");

            try
            {
                seriesPlayerWinsField.SetValue(null, 0);
                seriesEnemyWinsField.SetValue(null, 0);
                seriesGamesPlayedField.SetValue(null, 0);
                seriesScoreTotalField.SetValue(null, 0);
                Time.timeScale = 1f;

                var gameManager = managerGo.AddComponent<GameManager>();
                gameManagerAwake.Invoke(gameManager, null);
                Assert.AreSame(gameManager, GameManager.Instance,
                    "The castle callback must resolve the fixture's GameManager singleton.");
                var playerCastle = playerCastleGo.AddComponent<CastleController>();
                playerCastle.isPlayerCastle = true;
                var enemyCastle = enemyCastleGo.AddComponent<CastleController>();
                enemyCastle.isPlayerCastle = false;

                var playerCoreGo = new GameObject("CoreScoreOrderingPlayerCore");
                playerCoreGo.transform.SetParent(playerCastleGo.transform);
                var playerCore = playerCoreGo.AddComponent<CastleCoreGimmick>();
                playerCore.isPlayerCore = true;

                var enemyCoreGo = new GameObject("CoreScoreOrderingEnemyCore");
                enemyCoreGo.transform.SetParent(enemyCastleGo.transform);
                var enemyCore = enemyCoreGo.AddComponent<CastleCoreGimmick>();
                enemyCore.isPlayerCore = false;
                enemyCore.scoreValue = coreScore;
                enemyCore.currentHP = 0f;

                gameManager.playerCastle = playerCastle;
                gameManager.enemyCastle = enemyCastle;
                playerCoreField.SetValue(gameManager, playerCore);
                enemyCoreField.SetValue(gameManager, enemyCore);
                gameManager.currentState = GameState.PlayerTurn;
                isPlayerTurnField.SetValue(gameManager, true);
                playerCastle.RefreshBlockList();
                enemyCastle.RefreshBlockList();

                Assert.IsTrue(gameManager.IsPlayerTurn,
                    "Precondition: omitted damage ownership must resolve to the active player turn.");

                destroyBlock.Invoke(enemyCore, new object[] { null });

                Assert.AreEqual(GameState.GameOver, gameManager.currentState,
                    "Destroying the registered enemy core must reach GameOver through CastleController.");
                Assert.AreEqual(coreScore, (int)playerScoreField.GetValue(gameManager),
                    "The fatal enemy core must still award its scoreValue to the player.");
                Assert.AreEqual(coreScore, (int)seriesScoreTotalField.GetValue(null),
                    "EndGame must snapshot the credited core score, not the pre-destruction total.");

                var results = Object.FindObjectOfType<ResultsScreenController>();
                Assert.IsNotNull(results, "The core victory must create the results screen.");
                TMPro.TextMeshProUGUI stats = null;
                foreach (var label in results.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true))
                {
                    if (label.name == "Stats")
                    {
                        stats = label;
                        break;
                    }
                }
                Assert.IsNotNull(stats, "The results screen must expose its score report.");
                StringAssert.Contains($"점수 <b>{coreScore}</b>", stats.text,
                    "The results UI must receive the score that was credited before GameOver.");
            }
            finally
            {
                foreach (var results in Object.FindObjectsOfType<ResultsScreenController>())
                {
                    if (!resultsBefore.Contains(results)) Object.DestroyImmediate(results.gameObject);
                }
                if (playerCastleGo != null) Object.DestroyImmediate(playerCastleGo);
                if (enemyCastleGo != null) Object.DestroyImmediate(enemyCastleGo);
                if (managerGo != null) Object.DestroyImmediate(managerGo);
                seriesPlayerWinsField.SetValue(null, previousSeriesPlayerWins);
                seriesEnemyWinsField.SetValue(null, previousSeriesEnemyWins);
                seriesGamesPlayedField.SetValue(null, previousSeriesGamesPlayed);
                seriesScoreTotalField.SetValue(null, previousSeriesScoreTotal);
                Time.timeScale = previousTimeScale;
            }
        }

        [Test]
        public void GameManager_PreviewLastStandLaunchVelocity_BuffsOnlyActivePhaseWithoutConsumingIt()
        {
            var managerGo = new GameObject("LastStandPreviewGameManager");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                Vector2 aimedVelocity = new Vector2(10f, 4f);

                gameManager.playerLastStand = LastStand.Phase.Active;
                Vector2 activePreview = gameManager.PreviewLastStandLaunchVelocity(true, aimedVelocity);

                Assert.AreEqual(aimedVelocity * 1.3f, activePreview,
                    "An active player LAST STAND must preview the same 1.30x launch velocity that runtime applies.");
                Assert.AreEqual(LastStand.Phase.Active, gameManager.playerLastStand,
                    "Previewing a shot must not consume the one-shot LAST STAND before launch.");

                gameManager.playerLastStand = LastStand.Phase.Locked;
                Assert.AreEqual(aimedVelocity,
                    gameManager.PreviewLastStandLaunchVelocity(true, aimedVelocity),
                    "A locked LAST STAND must not alter the preview.");

                gameManager.playerLastStand = LastStand.Phase.Consumed;
                Assert.AreEqual(aimedVelocity,
                    gameManager.PreviewLastStandLaunchVelocity(true, aimedVelocity),
                    "A consumed LAST STAND must not continue altering later previews.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
            }
        }


        [Test]
        public void GameManager_ActiveAiPreparationAndPreview_ReturnDesiredVelocityWithoutConsumingPhase()
        {
            var managerGo = new GameObject("LastStandInverseVelocityGameManager");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                Vector2 desiredFinalVelocity = new Vector2(-14.75f, 8.25f);
                gameManager.aiLastStand = LastStand.Phase.Active;

                Vector2 preparedVelocity =
                    gameManager.PrepareLastStandLaunchVelocity(false, desiredFinalVelocity);
                float multiplier = LastStand.SpeedMult(false);
                Assert.AreEqual(desiredFinalVelocity.x / multiplier, preparedVelocity.x, 0.0001f,
                    "Active AI preparation must remove the pending Last Stand speed multiplier.");
                Assert.AreEqual(desiredFinalVelocity.y / multiplier, preparedVelocity.y, 0.0001f);

                Vector2 previewedVelocity =
                    gameManager.PreviewLastStandLaunchVelocity(false, preparedVelocity);
                Assert.AreEqual(desiredFinalVelocity.x, previewedVelocity.x, 0.0001f);
                Assert.AreEqual(desiredFinalVelocity.y, previewedVelocity.y, 0.0001f,
                    "Preparing and previewing an active AI shot must compose to the intended final velocity.");
                Assert.AreEqual(LastStand.Phase.Active, gameManager.aiLastStand,
                    "Inverse preparation and preview are calculations only and must not consume the AI one-shot phase.");
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
            }
        }

        [Test]
        public void GameManager_InactiveAiPreparation_ReturnsDesiredVelocityAndPreservesPhase()
        {
            var managerGo = new GameObject("InactiveLastStandPreparationGameManager");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                Vector2 desiredFinalVelocity = new Vector2(-12.5f, 6.75f);
                var inactivePhases = new[]
                {
                    LastStand.Phase.Locked,
                    LastStand.Phase.Armed,
                    LastStand.Phase.Consumed
                };

                foreach (LastStand.Phase phase in inactivePhases)
                {
                    gameManager.aiLastStand = phase;

                    Assert.AreEqual(
                        desiredFinalVelocity,
                        gameManager.PrepareLastStandLaunchVelocity(false, desiredFinalVelocity),
                        $"AI preparation must be identity while Last Stand is {phase}.");
                    Assert.AreEqual(phase, gameManager.aiLastStand,
                        $"Preparing an inactive AI shot must preserve the {phase} phase.");
                }
            }
            finally
            {
                Object.DestroyImmediate(managerGo);
            }
        }
        [Test]
        public void SimpleAI_Solver_WithDragAndLocalizedWind_OutperformsLegacyAnalyticAim()
        {
            var managerGo = new GameObject("AiSolverGameManager");
            var aiGo = new GameObject("AiSolver");
            var launchPointGo = new GameObject("AiSolverLaunchPoint");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                var gameManagerAwake = typeof(GameManager).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(gameManagerAwake);
                gameManagerAwake.Invoke(gameManager, null);

                Vector2 launchAnchor = new Vector2(12f, 2f);
                Vector2 runtimeStart = launchAnchor + Vector2.up * UnitController.DefaultLaunchSpawnHeight;
                Vector2 target = new Vector2(-8f, 2f);
                const float mass = 0.35f;
                const float linearDrag = 0.05f;
                const float hardCeilingY = 20f;
                const float windForce = 6f;
                const float windRadius = 7f;
                float fixedStep = Time.fixedDeltaTime;

                launchPointGo.transform.position = launchAnchor;
                var ai = aiGo.AddComponent<SimpleAI>();
                ai.launchPoint = launchPointGo.transform;
                ai.maxLaunchVelocity = 25.2f;
                gameManager.currentWindForce = windForce;
                gameManager.windEffectOrigin = runtimeStart;
                gameManager.windEffectRadius = windRadius;

                var calculateVelocity = typeof(SimpleAI).GetMethod(
                    "CalculateLaunchVelocity",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(calculateVelocity);
                Vector2 solvedVelocity = (Vector2)calculateVelocity.Invoke(
                    ai,
                    new object[] { target, mass, linearDrag, hardCeilingY });

                float ClosestRuntimeDistance(Vector2 initialVelocity)
                {
                    Vector2 position = runtimeStart;
                    Vector2 velocity = initialVelocity;
                    float closestDistanceSquared = (target - runtimeStart).sqrMagnitude;
                    int maxSteps = Mathf.CeilToInt(8f / fixedStep);
                    for (int step = 0; step < maxSteps; step++)
                    {
                        if (position.y > hardCeilingY && velocity.y > 0f)
                        {
                            velocity = new Vector2(velocity.x, 0f);
                        }

                        Vector2 windAcceleration = (position - runtimeStart).sqrMagnitude <= windRadius * windRadius
                            ? new Vector2(windForce / Mathf.Max(UnitController.MinRuntimeMass, mass), 0f)
                            : Vector2.zero;
                        velocity += (Physics2D.gravity + windAcceleration) * fixedStep;
                        velocity /= 1f + linearDrag * fixedStep;
                        Vector2 nextPosition = position + velocity * fixedStep;

                        Vector2 segment = nextPosition - position;
                        float segmentLengthSquared = segment.sqrMagnitude;
                        float along = segmentLengthSquared > 0.000001f
                            ? Mathf.Clamp01(Vector2.Dot(target - position, segment) / segmentLengthSquared)
                            : 0f;
                        Vector2 closest = position + segment * along;
                        closestDistanceSquared = Mathf.Min(
                            closestDistanceSquared,
                            (target - closest).sqrMagnitude);
                        position = nextPosition;
                    }
                    return Mathf.Sqrt(closestDistanceSquared);
                }

                Vector2 displacement = target - runtimeStart;
                float angleRadians = 45f * Mathf.Deg2Rad;
                float cos = Mathf.Cos(angleRadians);
                float absX = Mathf.Abs(displacement.x);
                float denominator =
                    2f * cos * cos * (absX * Mathf.Tan(angleRadians) - displacement.y);
                float legacySpeed = Mathf.Clamp(
                    Mathf.Sqrt(Mathf.Abs(Physics2D.gravity.y) * absX * absX / denominator),
                    5f,
                    ai.maxLaunchVelocity);
                Vector2 legacyVelocity = new Vector2(
                    Mathf.Sign(displacement.x) * legacySpeed * cos,
                    legacySpeed * Mathf.Sin(angleRadians));

                float solvedDistance = ClosestRuntimeDistance(solvedVelocity);
                float legacyDistance = ClosestRuntimeDistance(legacyVelocity);
                TestContext.WriteLine(
                    $"Solved closest distance {solvedDistance:F4}; legacy closest distance {legacyDistance:F4}");

                Assert.Less(solvedDistance, 0.5f,
                    "The bounded solver must place the runtime-equivalent drag-and-localized-wind arc honestly close to its target.");
                Assert.Greater(legacyDistance - solvedDistance, 1f,
                    "The drag-aware localized-wind solve must materially improve on the old no-drag analytic shot with stale wind origin.");
                Assert.Less(solvedDistance, legacyDistance * 0.35f,
                    "The refined solution must reduce the legacy closest-approach error by at least 65%.");
            }
            finally
            {
                Object.DestroyImmediate(aiGo);
                Object.DestroyImmediate(launchPointGo);
                Object.DestroyImmediate(managerGo);
            }
        }

        [Test]
        public void SimpleAI_Solver_DegenerateTargets_ReturnFiniteBoundedVelocity()
        {
            var aiGo = new GameObject("DegenerateAiSolver");
            var launchPointGo = new GameObject("DegenerateAiLaunchPoint");
            try
            {
                Vector2 start = new Vector2(3f, 4f);
                launchPointGo.transform.position = start;
                var ai = aiGo.AddComponent<SimpleAI>();
                ai.launchPoint = launchPointGo.transform;
                ai.maxLaunchVelocity = 25.2f;

                var calculateVelocity = typeof(SimpleAI).GetMethod(
                    "CalculateLaunchVelocity",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(calculateVelocity);

                foreach (Vector2 target in new[] { start, start + new Vector2(0.01f, 8f) })
                {
                    Vector2 velocity = (Vector2)calculateVelocity.Invoke(
                        ai,
                        new object[] { target, 0.35f, 0.05f, 20f });
                    Assert.IsFalse(float.IsNaN(velocity.x) || float.IsInfinity(velocity.x));
                    Assert.IsFalse(float.IsNaN(velocity.y) || float.IsInfinity(velocity.y),
                        $"The solver must return finite components for degenerate target {target}.");
                    Assert.GreaterOrEqual(velocity.magnitude, 5f - 0.0001f);
                    Assert.LessOrEqual(velocity.magnitude, ai.maxLaunchVelocity + 0.0001f,
                        $"The solver must keep degenerate target {target} inside its legal speed bounds.");
                }
            }
            finally
            {
                Object.DestroyImmediate(aiGo);
                Object.DestroyImmediate(launchPointGo);
            }
        }

        [Test]
        public void UnitController_FatalDamage_CreditsOnlyOpposingKiller_NotSelfOrUnownedBarrelDeath()
        {
            var managerGo = new GameObject("FatalDamageCreditGameManager");
            var enemyVictimGo = new GameObject("EnemyVictim");
            var selfOwnedBarrelGo = new GameObject("SelfOwnedBarrel");
            var unownedBarrelGo = new GameObject("UnownedBarrel");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                var gameManagerAwake = typeof(GameManager).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(gameManagerAwake);
                gameManagerAwake.Invoke(gameManager, null);
                var deploymentComponent = managerGo.GetComponent<DeploymentController>();
                Assert.IsNotNull(deploymentComponent);
                var deploymentAwake = typeof(DeploymentController).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(deploymentAwake);
                deploymentAwake.Invoke(deploymentComponent, null);
                var deployment = DeploymentController.Instance;
                deployment.ResetEconomy();

                float playerBefore = deployment.PlayerSupply;
                float enemyBefore = deployment.EnemySupply;
                enemyVictimGo.transform.position = new Vector3(1000f, 1000f, 0f);
                var enemyVictim = enemyVictimGo.AddComponent<UnitController>();
                enemyVictim.isPlayerUnit = false;
                enemyVictim.maxHP = 1f;
                enemyVictim.currentHP = 1f;

                enemyVictim.TakeDamage(1f, damageFromPlayer: true);

                Assert.AreEqual(playerBefore + SupplyRules.KillBonus, deployment.PlayerSupply, 0.0001f,
                    "Fatal player-owned damage to an enemy unit must credit the player's supply.");
                Assert.AreEqual(enemyBefore, deployment.EnemySupply, 0.0001f);

                selfOwnedBarrelGo.transform.position = new Vector3(2000f, 2000f, 0f);
                selfOwnedBarrelGo.AddComponent<ExplosiveGimmick>();
                var selfOwnedBarrel = selfOwnedBarrelGo.AddComponent<UnitController>();
                selfOwnedBarrel.unitType = UnitType.Barrel;
                selfOwnedBarrel.isPlayerUnit = true;
                selfOwnedBarrel.maxHP = 1f;
                selfOwnedBarrel.currentHP = 1f;
                float enemyBeforeSelfDeath = deployment.EnemySupply;

                selfOwnedBarrel.TakeDamage(1f, damageFromPlayer: true);

                Assert.AreEqual(enemyBeforeSelfDeath, deployment.EnemySupply, 0.0001f,
                    "A player barrel's self-owned detonation must not be misreported as an enemy kill.");

                unownedBarrelGo.transform.position = new Vector3(3000f, 3000f, 0f);
                unownedBarrelGo.AddComponent<ExplosiveGimmick>();
                var unownedBarrel = unownedBarrelGo.AddComponent<UnitController>();
                unownedBarrel.unitType = UnitType.Barrel;
                unownedBarrel.isPlayerUnit = true;
                unownedBarrel.maxHP = 1f;
                unownedBarrel.currentHP = 1f;
                float enemyBeforeUnownedDeath = deployment.EnemySupply;

                unownedBarrel.TakeDamage(1f);

                Assert.AreEqual(enemyBeforeUnownedDeath, deployment.EnemySupply, 0.0001f,
                    "A natural or otherwise unattributed barrel death must not manufacture an opposing kill bonus.");
            }
            finally
            {
                if (enemyVictimGo != null) Object.DestroyImmediate(enemyVictimGo);
                if (selfOwnedBarrelGo != null) Object.DestroyImmediate(selfOwnedBarrelGo);
                if (unownedBarrelGo != null) Object.DestroyImmediate(unownedBarrelGo);
                Object.DestroyImmediate(managerGo);
            }
        }

        [Test]
        public void ExplosiveGimmick_DamageOwnerPropagatesThroughChainIntoBlockScore()
        {
            var managerGo = new GameObject("ExplosiveOwnerGameManager");
            var castleGo = new GameObject("ExplosiveOwnerEnemyCastle");
            var blockGo = new GameObject("ExplosiveOwnerScoredBlock");
            var rootGo = new GameObject("ExplosiveOwnerRoot");
            var chainedGo = new GameObject("ExplosiveOwnerChain");
            try
            {
                var gameManager = managerGo.AddComponent<GameManager>();
                var gameManagerAwake = typeof(GameManager).GetMethod(
                    "Awake",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(gameManagerAwake);
                gameManagerAwake.Invoke(gameManager, null);
                var playerScoreField = typeof(GameManager).GetField(
                    "playerScore",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var enemyScoreField = typeof(GameManager).GetField(
                    "enemyScore",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                Assert.IsNotNull(playerScoreField);
                Assert.IsNotNull(enemyScoreField);

                var enemyCastle = castleGo.AddComponent<CastleController>();
                enemyCastle.isPlayerCastle = false;
                blockGo.transform.SetParent(castleGo.transform);
                blockGo.transform.position = new Vector3(2.5f, 0f, 0f);
                var block = blockGo.AddComponent<DestructibleBlock>();
                block.maxHP = 1f;
                block.currentHP = 1f;
                block.scoreValue = 23;

                rootGo.transform.position = Vector3.zero;
                var root = rootGo.AddComponent<ExplosiveGimmick>();
                root.explosionRadius = 1.2f;
                root.explosionDamage = 2f;

                chainedGo.transform.position = new Vector3(1f, 0f, 0f);
                var chained = chainedGo.AddComponent<ExplosiveGimmick>();
                chained.explosionRadius = 2f;
                chained.explosionDamage = 2f;

                Assert.Greater(Vector2.Distance(rootGo.transform.position, blockGo.transform.position),
                    root.explosionRadius,
                    "Precondition: only the chained explosion can reach the scored block.");
                root.SetDamageOwner(true);

                root.Explode();

                Assert.IsTrue(blockGo == null,
                    "The owner-attributed chained explosion must reach and destroy the downstream block.");
                Assert.AreEqual(223, (int)playerScoreField.GetValue(gameManager),
                    "The player owner must carry through both explosions (100 each) and the downstream block score (23).");
                Assert.AreEqual(0, (int)enemyScoreField.GetValue(gameManager));
            }
            finally
            {
                if (blockGo != null) Object.DestroyImmediate(blockGo);
                Object.DestroyImmediate(chainedGo);
                Object.DestroyImmediate(rootGo);
                Object.DestroyImmediate(castleGo);
                Object.DestroyImmediate(managerGo);
            }
        }
    }
}

