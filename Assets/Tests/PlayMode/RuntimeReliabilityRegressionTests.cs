using System.Collections;
using NUnit.Framework;
using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using CastleBusters;

namespace CastleBusters.Tests
{
    public class RuntimeReliabilityRegressionTests
    {
        private static readonly HeroItemType[] GrowthTypes =
        {
            HeroItemType.Sword,
            HeroItemType.Shield,
            HeroItemType.Boots
        };

        private readonly int[,] originalGrowthStacks = new int[2, 3];
        private float originalTimeScale;
        private StageId originalPendingStage;
        private GameObject spawnedShockwaveRing;
        private Scene temporaryTestScene;
        private bool hasTemporaryTestScene;
        private const string StageProgressPrefsKey = "CastleBusters.StageProgress.v1";
        private const string WarChestBalancePrefsKey = "CastleBusters.PrototypeWarChest.Balance";
        private const string BattleBannerSealPrefsKey = "CastleBusters.PrototypeWarChest.BattleBannerSeal";
        private const string ChroniclePackPrefsKey = "CastleBusters.MobileStore.ChroniclePack";
        private bool hadLeaderboardPrefs;
        private string originalLeaderboardPrefs;
        private bool hadStageProgressPrefs;
        private int originalStageProgressPrefs;
        private bool hadWarChestBalancePrefs;
        private int originalWarChestBalancePrefs;
        private bool hadBattleBannerSealPrefs;
        private int originalBattleBannerSealPrefs;
        private bool hadChroniclePackPrefs;
        private int originalChroniclePackPrefs;

        [UnitySetUp]
        public IEnumerator CaptureRuntimeState()
        {
            originalTimeScale = Time.timeScale;
            originalPendingStage = GameManager.PendingStage;
            for (var side = 0; side < 2; side++)
            {
                for (var type = 0; type < GrowthTypes.Length; type++)
                {
                    originalGrowthStacks[side, type] = HeroGrowth.Stacks(side == 0, GrowthTypes[type]);
                }
            }

            hadLeaderboardPrefs = PlayerPrefs.HasKey(LeaderboardStore.PrefsKey);
            originalLeaderboardPrefs = hadLeaderboardPrefs
                ? PlayerPrefs.GetString(LeaderboardStore.PrefsKey)
                : null;
            hadStageProgressPrefs = PlayerPrefs.HasKey(StageProgressPrefsKey);
            originalStageProgressPrefs = PlayerPrefs.GetInt(StageProgressPrefsKey);
            hadWarChestBalancePrefs = PlayerPrefs.HasKey(WarChestBalancePrefsKey);
            originalWarChestBalancePrefs = PlayerPrefs.GetInt(WarChestBalancePrefsKey);
            hadBattleBannerSealPrefs = PlayerPrefs.HasKey(BattleBannerSealPrefsKey);
            originalBattleBannerSealPrefs = PlayerPrefs.GetInt(BattleBannerSealPrefsKey);
            hadChroniclePackPrefs = PlayerPrefs.HasKey(ChroniclePackPrefsKey);
            originalChroniclePackPrefs = PlayerPrefs.GetInt(ChroniclePackPrefsKey);

            Time.timeScale = 1f;
            yield break;
        }

        [UnityTearDown]
        public IEnumerator RestoreRuntimeState()
        {
            if (spawnedShockwaveRing != null) Object.Destroy(spawnedShockwaveRing);

            Time.timeScale = 1f;
            GameManager.PendingStage = originalPendingStage;
            // Restore persistent player data before a scene operation: in EditMode,
            // LoadScene can throw and must not strand test-mutated progression/economy prefs.
            RestorePlayerPrefsState();
            if (hasTemporaryTestScene && temporaryTestScene.IsValid() && temporaryTestScene.isLoaded)
            {
                SceneManager.SetActiveScene(temporaryTestScene);
            }
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return null;

            HeroGrowth.Reset();
            for (var side = 0; side < 2; side++)
            {
                for (var type = 0; type < GrowthTypes.Length; type++)
                {
                    for (var stack = 0; stack < originalGrowthStacks[side, type]; stack++)
                    {
                        HeroGrowth.Grant(side == 0, GrowthTypes[type]);
                    }
                }
            }


            Time.timeScale = originalTimeScale;
            spawnedShockwaveRing = null;
            temporaryTestScene = default;
            hasTemporaryTestScene = false;
        }
        [Test]
        public void GetParticleMaterial_DefaultTexture_ReusesSharedMaterial()
        {
            var firstMaterial = GameFeelVfx.GetParticleMaterial();
            var secondMaterial = GameFeelVfx.GetParticleMaterial();

            Assert.IsNotNull(firstMaterial, "The default particle texture must resolve a renderable material");
            Assert.AreSame(firstMaterial, secondMaterial, "Repeated default particle material requests must reuse the shared material instance");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator ShockwaveRing_SpawnsWithRenderableSpriteAndCleansItselfUp()
        {
            Time.timeScale = 1f;

            GameFeelVfx.SpawnShockwaveRing(Vector3.zero, Color.white, finalRadius: 1.2f, lifetime: 0.08f);
            yield return null;

            var ring = GameObject.Find("ShockwaveRing");
            Assert.IsNotNull(ring, "Spawning the gameplay shockwave must create a visible ring GameObject");
            spawnedShockwaveRing = ring;

            var renderer = ring.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(renderer, "The shockwave ring must expose a SpriteRenderer");
            Assert.IsNotNull(renderer.sprite, "The shockwave ring must have renderable sprite content");
            Assert.IsNotNull(renderer.sharedMaterial, "The shockwave ring must have a renderable material in PlayMode");

            yield return new WaitForSecondsRealtime(0.25f);

            Assert.IsTrue(ring == null, "A shockwave ring must naturally remove itself after its configured lifetime");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator LeavingGameplayScene_DoesNotCreateReplacementGameplayOrDropObjects()
        {
            yield return LoadAndBeginStage(StageId.Stage1);
            Assert.IsTrue(HasActiveChariot(), "Precondition: the active siege must contain the gameplay chariot before scene exit");

            var gameplayScene = SceneManager.GetActiveScene();
            temporaryTestScene = SceneManager.CreateScene("RuntimeReliabilityEmptyScene");
            hasTemporaryTestScene = true;
            Assert.IsTrue(SceneManager.SetActiveScene(temporaryTestScene), "The empty scene must become active before unloading gameplay");

            var unload = SceneManager.UnloadSceneAsync(gameplayScene);
            Assert.IsNotNull(unload, "The gameplay scene must be unloadable during PlayMode");
            yield return unload;
            yield return null;
            yield return new WaitForSecondsRealtime(ChariotRules.RespawnDelaySeconds + 0.5f);

            Assert.Zero(Object.FindObjectsOfType<MovingGimmick>().Length,
                "Leaving gameplay must not create a replacement moving gameplay object in the active scene");
            Assert.Zero(Object.FindObjectsOfType<ItemPickup>().Length,
                "Leaving gameplay must not create an active item drop in the replacement scene");
            Assert.IsNull(GameObject.Find("ShockwaveRing"),
                "Leaving gameplay must not create an active drop shockwave in the replacement scene");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator ChariotDestroyedByPublicBlockDamage_SpawnsExactlyOneItemPickup()
        {
            yield return LoadAndBeginStage(StageId.Stage1);

            var chariot = ActiveChariot();
            Assert.IsNotNull(chariot, "Precondition: the active siege must contain a chariot");
            var chariotBlock = chariot.GetComponent<DestructibleBlock>();
            Assert.IsNotNull(chariotBlock, "The active chariot must expose its public DestructibleBlock damage surface");
            Assert.Greater(chariotBlock.currentHP, 0f, "The active chariot must be alive before damage is applied");

            var pickupsBefore = Object.FindObjectsOfType<ItemPickup>().Length;
            chariotBlock.TakeDamage(chariotBlock.currentHP + 1f);
            yield return null;

            Assert.AreEqual(pickupsBefore + 1, Object.FindObjectsOfType<ItemPickup>().Length,
                "Destroying an active chariot through DestructibleBlock damage must create exactly one item pickup");

            yield return null;
            Assert.AreEqual(pickupsBefore + 1, Object.FindObjectsOfType<ItemPickup>().Length,
                "The destroyed chariot must not create a duplicate item pickup on a subsequent frame");

            yield return new WaitForSecondsRealtime(ChariotRules.RespawnDelaySeconds + 0.5f);
            Assert.IsTrue(chariot == null, "The destroyed chariot instance must not survive its public damage path");
            Assert.AreEqual(1, ActiveChariotCount(), "A live chariot destruction must schedule exactly one replacement chariot");
            Assert.AreNotSame(chariot, ActiveChariot(), "The post-delay chariot must be a new gameplay instance");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator BootBeginSiegeLaunchAndTurnResolution_ReachesAITurnWithoutUnexpectedLogs()
        {
            yield return LoadAndBeginStage(StageId.Stage1);

            var gameManager = GameManager.Instance;
            var launchManager = Object.FindObjectOfType<LaunchManager>();
            Assert.IsNotNull(launchManager, "A begun siege must expose LaunchManager");

            gameManager.SelectUnit(0);
            launchManager.SimulateLaunch(new Vector2(12f, 8f));

            var deadline = Time.realtimeSinceStartup + 20f;
            while (gameManager.currentState == GameState.PlayerTurn && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(GameState.AITurn, gameManager.currentState,
                "A selected unit launched through the public runtime path must resolve the player turn into AI turn");
            LogAssert.NoUnexpectedReceived();
        }
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator LastStandButton_ArmedPlayerClickActivatesAndLaunchConsumesBuff()
        {
            yield return LoadAndBeginStage(StageId.Stage1);

            var gameManager = GameManager.Instance;
            CastleCoreGimmick playerCore = null;
            foreach (var core in Object.FindObjectsOfType<CastleCoreGimmick>())
            {
                if (!core.isPlayerCore) continue;

                playerCore = core;
                break;
            }

            Assert.IsNotNull(playerCore, "A begun siege must expose the player castle core through its public gameplay component");
            var dangerHp = playerCore.maxHP * LastStand.DangerHpFraction;
            playerCore.TakeDamage(Mathf.Max(0f, playerCore.currentHP - dangerHp));
            Assert.LessOrEqual(playerCore.currentHP, dangerHp,
                "Public core damage must place the player core at the documented Last Stand danger threshold");

            var armDeadline = Time.realtimeSinceStartup + 5f;
            while (gameManager.playerLastStand != LastStand.Phase.Armed && Time.realtimeSinceStartup < armDeadline)
            {
                yield return null;
            }

            Assert.AreEqual(LastStand.Phase.Armed, gameManager.playerLastStand,
                "A danger-threshold player core must arm Last Stand during the real player turn");

            var lastStandButton = gameManager.lastStandButton;
            Assert.IsNotNull(lastStandButton, "GameManager must expose the public Last Stand HUD button");
            Assert.IsTrue(lastStandButton.gameObject.activeInHierarchy,
                "An armed, non-resolving player turn must make the Last Stand button visible");
            Assert.IsTrue(lastStandButton.interactable,
                "An armed, non-resolving player turn must make the Last Stand button interactable");
            Assert.IsNotNull(lastStandButton.targetGraphic,
                "The Last Stand button must expose a usable target graphic");
            Assert.IsTrue(lastStandButton.targetGraphic.gameObject.activeInHierarchy,
                "The Last Stand target graphic must be active with its button");
            Assert.IsTrue(lastStandButton.targetGraphic.raycastTarget,
                "The Last Stand target graphic must accept pointer raycasts");
            var cardImage = lastStandButton.GetComponent<UnityEngine.UI.Image>();
            Assert.IsNotNull(cardImage, "The Last Stand button must retain its card Image component");
            var expectedCardSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.LastStandButton);
            Assert.IsNotNull(expectedCardSprite,
                "The generated Last Stand card art resource must resolve");
            Assert.AreSame(expectedCardSprite, cardImage.sprite,
                "The Last Stand button must use its generated card art rather than a generic fallback");
            var cardRect = lastStandButton.GetComponent<RectTransform>();
            Assert.IsNotNull(cardRect, "The Last Stand button must expose a RectTransform for HUD layout");
            Assert.AreEqual(156f, cardRect.sizeDelta.x, 0.001f,
                "The Last Stand card must retain its intended 156-pixel width");
            Assert.AreEqual(104f, cardRect.sizeDelta.y, 0.001f,
                "The Last Stand card must retain its intended 104-pixel height");
            var label = lastStandButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            Assert.IsNotNull(label, "The Last Stand button must retain its two-line status label");
            Assert.AreEqual("R  LAST STAND\nARMED", label.text,
                "The Last Stand card must retain its two-line Armed label");
            var labelRect = label.GetComponent<RectTransform>();
            Assert.IsNotNull(labelRect, "The Last Stand label must expose a RectTransform for card layout");
            Assert.AreEqual(new Vector2(0f, 0f), labelRect.anchorMin,
                "The Last Stand label must start at the lower card edge");
            Assert.AreEqual(new Vector2(1f, 0.34f), labelRect.anchorMax,
                "The Last Stand label must remain within the lower card band");
            Assert.AreEqual(new Vector2(0.5f, 0f), labelRect.pivot,
                "The Last Stand label must pivot from the lower center");
            Assert.AreEqual(new Vector2(0f, 0f), labelRect.anchoredPosition,
                "The Last Stand label must remain centered in its lower band");
            Assert.AreEqual(new Vector2(-12f, 0f), labelRect.sizeDelta,
                "The Last Stand label must retain its readable horizontal inset");
            // A newly activated UGUI Graphic requires one normal frame plus an explicit canvas synchronization before headless PlayMode raycasting.
            yield return null;
            Canvas.ForceUpdateCanvases();


            var eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>();
            Assert.IsNotNull(eventSystem, "Gameplay HUD must expose an EventSystem for Last Stand pointer clicks");
            Assert.IsTrue(eventSystem.gameObject.activeInHierarchy,
                "The Last Stand EventSystem must be active while the button is interactable");
            Assert.IsTrue(eventSystem.isActiveAndEnabled,
                "The Last Stand EventSystem must be enabled while the button is interactable");

            var canvas = lastStandButton.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas, "The Last Stand button must be parented by an active HUD Canvas");
            Assert.IsTrue(canvas.isActiveAndEnabled,
                "The Last Stand Canvas must be active while the button is interactable");
            var graphicRaycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Assert.IsNotNull(graphicRaycaster,
                "The Last Stand Canvas must expose a GraphicRaycaster for pointer routing");
            Assert.IsTrue(graphicRaycaster.isActiveAndEnabled,
                "The Last Stand GraphicRaycaster must be active while the button is interactable");

            BaseInputModule inputModule = null;
            foreach (var candidate in eventSystem.GetComponents<BaseInputModule>())
            {
                if (!candidate.isActiveAndEnabled) continue;

                inputModule = candidate;
                break;
            }

            Assert.IsNotNull(inputModule,
                "The Last Stand EventSystem must expose an enabled input module for pointer routing");

            var screenPosition = RectTransformUtility.WorldToScreenPoint(
                graphicRaycaster.eventCamera,
                cardRect.TransformPoint(cardRect.rect.center));
            var pointerEventData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left
            };
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointerEventData, raycastResults);
            Assert.IsNotEmpty(raycastResults,
                "A pointer at the Last Stand card center must produce a UGUI raycast result");
            var foremostHit = raycastResults[0].gameObject;
            var foremostHitTransform = foremostHit.transform;
            Assert.IsTrue(
                foremostHitTransform == lastStandButton.transform
                || foremostHitTransform.IsChildOf(lastStandButton.transform),
                "The foremost pointer target at the Last Stand card center must be the button or its child");
            GameObject actualHit = null;
            foreach (var raycastResult in raycastResults)
            {
                var hitTransform = raycastResult.gameObject.transform;
                if (hitTransform != lastStandButton.transform && !hitTransform.IsChildOf(lastStandButton.transform)) continue;

                actualHit = raycastResult.gameObject;
                break;
            }

            Assert.IsNotNull(actualHit,
                "A pointer at the Last Stand card center must route through the active Last Stand card subtree");

            ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                actualHit,
                pointerEventData,
                ExecuteEvents.pointerClickHandler);

            Assert.AreEqual(LastStand.Phase.Active, gameManager.playerLastStand,
                "A Last Stand button pointer click must use the authoritative Armed-to-Active activation path");
            Assert.IsFalse(lastStandButton.gameObject.activeInHierarchy,
                "The Last Stand button must hide immediately once its one-shot effect becomes active");

            var knightTemplate = gameManager.knightPrefab.GetComponent<UnitController>();
            Assert.IsNotNull(knightTemplate, "The player selection row must retain its Knight launch prefab");
            var expectedAttackDamage = LastStand.BuffedDamage(
                knightTemplate.attackDamage * HeroGrowth.DamageMult(true), true);
            var expectedExplosionDamage = LastStand.BuffedDamage(
                knightTemplate.explosionDamage * HeroGrowth.DamageMult(true), true);
            var expectedExplosionRadius = knightTemplate.explosionRadius * LastStand.RadiusMult(true);
            var existingUnits = new System.Collections.Generic.HashSet<UnitController>(
                Object.FindObjectsOfType<UnitController>());
            var launchManager = Object.FindObjectOfType<LaunchManager>();
            Assert.IsNotNull(launchManager, "An armed player turn must retain the public launch manager");

            gameManager.SelectUnit(0);
            launchManager.SimulateLaunch(new Vector2(12f, 8f));

            Assert.AreEqual(LastStand.Phase.Consumed, gameManager.playerLastStand,
                "Launching the selected unit must consume Active Last Stand through the normal launch chokepoint");

            yield return null;

            UnitController launchedKnight = null;
            foreach (var unit in Object.FindObjectsOfType<UnitController>())
            {
                if (existingUnits.Contains(unit) || !unit.isPlayerUnit || unit.unitType != UnitType.Knight) continue;

                launchedKnight = unit;
                break;
            }

            Assert.IsNotNull(launchedKnight, "The public Knight launch must create a new player unit instance");
            Assert.AreEqual(expectedAttackDamage, launchedKnight.attackDamage, 0.001f,
                "Consuming Last Stand through a real launch must apply its documented player damage effect");
            Assert.AreEqual(expectedExplosionDamage, launchedKnight.explosionDamage, 0.001f,
                "Consuming Last Stand through a real launch must apply its documented player explosion damage effect");
            Assert.AreEqual(expectedExplosionRadius, launchedKnight.explosionRadius, 0.001f,
                "Consuming Last Stand through a real launch must apply its documented player explosion radius effect");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator LaunchToast_SuppressesConcurrentComboBannerWhileRetainingComboState()
        {
            yield return LoadAndBeginStage(StageId.Stage1);

            Assert.IsNotNull(GameplayUxDirector.Instance, "A begun siege must initialize the gameplay UX director before HUD feedback is registered");
            GameplayUxDirector.ResetSessionStats();
            GameplayUxDirector.NotifyLaunch("Knight", 50f, 45f);
            GameplayUxDirector.NotifyBreak(Vector3.zero, false);
            yield return null;

            var hud = GameObject.Find("GameplayUxDirectorHUD");
            Assert.IsNotNull(hud, "Gameplay feedback must expose the HUD root for observable PlayMode state");
            var turnToast = hud.transform.Find("TurnToastText");
            var toastBackplate = hud.transform.Find("ToastBackplate");
            var combo = hud.transform.Find("ComboText");
            var comboBackplate = hud.transform.Find("ComboBackplate");
            Assert.IsNotNull(turnToast, "Gameplay feedback must expose the turn-toast GameObject");
            Assert.IsNotNull(toastBackplate, "Gameplay feedback must expose the toast backplate GameObject");
            Assert.IsNotNull(combo, "Gameplay feedback must expose the combo-banner GameObject");
            Assert.IsNotNull(comboBackplate, "Gameplay feedback must expose the combo-banner backplate GameObject");

            var turnToastText = turnToast.GetComponent<TMPro.TextMeshProUGUI>();
            var comboText = combo.GetComponent<TMPro.TextMeshProUGUI>();
            Assert.IsNotNull(turnToastText, "The turn toast must expose its visible text component");
            Assert.IsNotNull(comboText, "The combo banner must expose its visible text component");
            Assert.IsTrue(turnToast.gameObject.activeSelf, "A launch must keep its turn toast visible");
            Assert.IsTrue(toastBackplate.gameObject.activeSelf, "A visible launch toast must retain its backplate");
            Assert.IsTrue(turnToastText.text.StartsWith("CLEAN SIEGE ARC"), "The active turn toast must remain the launch-grade feedback");
            Assert.IsFalse(combo.gameObject.activeSelf, "A block-break combo must not render in the central lane while the launch toast owns it");
            Assert.IsFalse(comboBackplate.gameObject.activeSelf, "The hidden concurrent combo must not leave its backplate visible");
            Assert.AreEqual("BLOCK BREAK x2", comboText.text, "The suppressed combo banner must still receive the block-break state update");
            Assert.AreEqual(2, GameplayUxDirector.SessionMaxCombo, "Suppressing the combo banner must not discard launch or block-break session accounting");

            yield return new WaitForSecondsRealtime(1.7f);
            yield return null;
            Assert.IsFalse(turnToast.gameObject.activeSelf, "The launch toast must expire before normal combo-banner behavior resumes");

            GameplayUxDirector.NotifyBreak(Vector3.zero, false);
            yield return null;

            Assert.IsTrue(combo.gameObject.activeSelf, "A block-break combo without an active toast must render normally");
            Assert.IsTrue(comboBackplate.gameObject.activeSelf, "A visible combo must restore its backplate");
            Assert.AreEqual("BLOCK BREAK x3", comboText.text, "The resumed combo banner must continue the existing combo count");
            Assert.AreEqual(3, GameplayUxDirector.SessionMaxCombo, "A no-toast combo must continue to update session-best accounting");
            GameplayUxDirector.ResetSessionStats();
        }


        [UnityTest]
        [Timeout(60000)]
        public IEnumerator NewSceneBoot_ResetsPriorMatchHeroGrowthBeforeInitialUnitsSpawn()
        {
            HeroGrowth.Reset();
            HeroGrowth.Grant(true, HeroItemType.Sword);
            HeroGrowth.Grant(true, HeroItemType.Shield);
            HeroGrowth.Grant(true, HeroItemType.Boots);

            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.Zero(HeroGrowth.Stacks(true, HeroItemType.Sword),
                "A new scene boot must clear prior-match player sword stacks before initial units spawn");
            Assert.Zero(HeroGrowth.Stacks(true, HeroItemType.Shield),
                "A new scene boot must clear prior-match player shield stacks before initial units spawn");
            Assert.Zero(HeroGrowth.Stacks(true, HeroItemType.Boots),
                "A new scene boot must clear prior-match player boot stacks before initial units spawn");

            var gameManager = GameManager.Instance;
            Assert.IsNotNull(gameManager, "The new scene must create its GameManager");

            var playerUnitCount = 0;
            foreach (var unit in Object.FindObjectsOfType<UnitController>())
            {
                if (!unit.isPlayerUnit) continue;

                playerUnitCount++;
                var template = InitialUnitTemplate(gameManager, unit.unitType);
                Assert.IsNotNull(template, $"The initial {unit.unitType} prefab must expose UnitController stats");

                var expectedMaxHP = template.unitData != null ? template.unitData.maxHP : template.maxHP;
                var expectedDamage = template.unitData != null ? template.unitData.attackDamage : template.attackDamage;
                var expectedSpeed = template.unitData != null ? template.unitData.moveSpeed : template.moveSpeed;
                Assert.AreEqual(expectedMaxHP, unit.maxHP, 0.0001f,
                    $"Initial {unit.unitType} health must not retain a prior-match shield bonus");
                Assert.AreEqual(expectedDamage, unit.attackDamage, 0.0001f,
                    $"Initial {unit.unitType} damage must not retain a prior-match sword bonus");
                Assert.AreEqual(expectedSpeed, unit.moveSpeed, 0.0001f,
                    $"Initial {unit.unitType} speed must not retain a prior-match boots bonus");
            }

            Assert.AreEqual(3, playerUnitCount, "A new siege must start with its three unmodified player units");
            HeroGrowth.Reset();
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator ControlGuideText_HasDarkOutlineAfterLaunchManagerInitializes()
        {
            yield return LoadAndBeginStage(StageId.Stage1);

            var launchManager = Object.FindObjectOfType<LaunchManager>();
            Assert.IsNotNull(launchManager, "Gameplay must create LaunchManager before its control guide can render");
            Assert.IsNotNull(launchManager.controlGuideText, "LaunchManager must create the visible ControlGuideText");

            var guide = launchManager.controlGuideText;
            Assert.Greater(guide.outlineWidth, 0f, "ControlGuideText must retain a visible TMP outline");
            Assert.LessOrEqual(guide.outlineColor.r, 26, "ControlGuideText outline must be dark enough to separate HUD text from the battlefield");
            Assert.LessOrEqual(guide.outlineColor.g, 26, "ControlGuideText outline must be dark enough to separate HUD text from the battlefield");
            Assert.LessOrEqual(guide.outlineColor.b, 26, "ControlGuideText outline must be dark enough to separate HUD text from the battlefield");
            Assert.GreaterOrEqual(guide.outlineColor.a, 128, "ControlGuideText outline must remain visibly opaque");
        }

        [Test]
        public void CalculateOrthographicSize_PreservesStageWidthsAcrossSupportedAspects()
        {
            const float targetHalfHeight = 8.4f;
            const float sixteenthByNinth = 16f / 9f;
            var stages = new[] { StageId.Stage1, StageId.Stage2, StageId.Stage3 };
            var expectedSixteenByNineSizes = new[] { 10.96875f, 10.209375f, 13.21875f };
            var aspects = new[] { 4f / 3f, 16f / 10f, 21f / 9f };

            for (var i = 0; i < stages.Length; i++)
            {
                var desiredWidth = StageDefinitions.For(stages[i]).cameraDesiredWorldWidth;
                var sixteenByNineSize = GamePresentationDirector.CalculateOrthographicSize(
                    targetHalfHeight, desiredWidth, sixteenthByNinth);

                Assert.AreEqual(expectedSixteenByNineSizes[i], sixteenByNineSize, 0.0001f,
                    $"{stages[i]} must retain its authored 16:9 camera framing");

                foreach (var aspect in aspects)
                {
                    var requiredSize = desiredWidth / (2f * aspect);
                    var fittedSize = GamePresentationDirector.CalculateOrthographicSize(
                        targetHalfHeight, desiredWidth, aspect);

                    Assert.GreaterOrEqual(fittedSize, requiredSize - 0.0001f,
                        $"{stages[i]} must show its full authored board width at {aspect:0.###}:1 without cropping");
                    Assert.AreEqual(Mathf.Max(targetHalfHeight, requiredSize), fittedSize, 0.0001f,
                        $"{stages[i]} must derive camera height from its authored board width at {aspect:0.###}:1");
                }
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator StageBackgroundResources_AreDistinctAndMatchTheSelectedStage()
        {
            var backgrounds = new[]
            {
                Resources.Load<Texture2D>("Backgrounds/Background_Stage1"),
                Resources.Load<Texture2D>("Backgrounds/Background_Stage2"),
                Resources.Load<Texture2D>("Backgrounds/Background_Stage3")
            };
            var stages = new[] { StageId.Stage1, StageId.Stage2, StageId.Stage3 };

            for (var i = 0; i < backgrounds.Length; i++)
            {
                Assert.IsNotNull(backgrounds[i], $"{stages[i]} must provide a stage background texture under Resources/Backgrounds");
            }

            Assert.AreNotSame(backgrounds[0], backgrounds[1], "Stage1 and Stage2 must not share a background texture");
            Assert.AreNotSame(backgrounds[0], backgrounds[2], "Stage1 and Stage3 must not share a background texture");
            Assert.AreNotSame(backgrounds[1], backgrounds[2], "Stage2 and Stage3 must not share a background texture");

            for (var i = 0; i < stages.Length; i++)
            {
                GameManager.PendingStage = stages[i];
                SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
                yield return null;
                yield return new WaitForSecondsRealtime(1.5f);

                Assert.IsNotNull(GameManager.Instance, "The stage scene must create a GameManager");
                Assert.AreEqual(stages[i], GameManager.Instance.currentStage, "The scene must resolve the requested stage");

                var background = GameObject.Find("Background");
                Assert.IsNotNull(background, "The stage scene must expose its visible background GameObject");
                var renderer = background.GetComponent<SpriteRenderer>();
                Assert.IsNotNull(renderer, "The visible background must use a SpriteRenderer");
                Assert.IsNotNull(renderer.sprite, "The visible background must resolve a sprite");
                Assert.AreSame(backgrounds[i], renderer.sprite.texture,
                    $"The visible background must resolve the Resources texture for {stages[i]}");
                var camera = Camera.main;
                Assert.IsNotNull(camera, $"{stages[i]} must expose the gameplay camera used to frame its background");
                Assert.Greater(renderer.bounds.size.x, 0f, $"{stages[i]} background must have a visible world width");
                Assert.Greater(renderer.bounds.size.y, 0f, $"{stages[i]} background must have a visible world height");
                Assert.AreEqual(background.transform.localScale.x, background.transform.localScale.y, 0.0001f,
                    $"{stages[i]} background must use uniform scaling so its source art is not distorted");

                AssertBackgroundCoversCamera(renderer, camera, stages[i], "initial framing");
                var layout = GameManager.Instance.ActiveLayout;
                Assert.AreEqual(layout.backgroundTint.r, renderer.color.r, 0.001f,
                    $"{stages[i]} background red tint must match the active layout");
                Assert.AreEqual(layout.backgroundTint.g, renderer.color.g, 0.001f,
                    $"{stages[i]} background green tint must match the active layout");
                Assert.AreEqual(layout.backgroundTint.b, renderer.color.b, 0.001f,
                    $"{stages[i]} background blue tint must match the active layout");
                GameManager.Instance.BeginSiege();
                yield return new WaitForSecondsRealtime(0.5f);
                Assert.AreEqual(GameState.PlayerTurn, GameManager.Instance.currentState,
                    $"{stages[i]} must enter the player turn before focused camera coverage is checked");

                var director = GamePresentationDirector.Instance;
                Assert.IsNotNull(director, $"{stages[i]} must expose its camera presentation director");
                var focusTarget = new GameObject($"{stages[i]}BackgroundCoverageFocusTarget");
                try
                {
                    focusTarget.transform.position = new Vector3(7.5f, -director.focusExtraHeight - 0.25f, 0f);
                    director.Focus(focusTarget.transform);
                    yield return new WaitForSecondsRealtime(1.5f);

                    Assert.Greater(camera.transform.position.x, 0f,
                        $"{stages[i]} camera must follow the permitted right-side focus target");
                    Assert.Less(camera.transform.position.y, director.boardCenter.y,
                        $"{stages[i]} camera must move below the board center for the permitted low-side focus target");
                    AssertBackgroundCoversCamera(renderer, camera, stages[i], "focused framing");
                }
                finally
                {
                    director.ClearFocus(focusTarget.transform);
                    Object.Destroy(focusTarget);
                }
            }

            GameManager.PendingStage = StageId.Stage1;
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator StagePicker_UsesDistinctDedicatedCards_AndKeepsInteractiveComponents()
        {
            var stages = new[] { StageId.Stage1, StageId.Stage2, StageId.Stage3 };
            var cardKeys = new[]
            {
                GimmickSpriteLibrary.Stage1Card,
                GimmickSpriteLibrary.Stage2Card,
                GimmickSpriteLibrary.Stage3Card
            };
            var expectedCards = new Sprite[cardKeys.Length];
            for (var i = 0; i < cardKeys.Length; i++)
            {
                expectedCards[i] = RequireGimmickSprite(cardKeys[i]);
            }

            Assert.AreNotSame(expectedCards[0], expectedCards[1], "Stage1 and Stage2 must not share card art");
            Assert.AreNotSame(expectedCards[0], expectedCards[2], "Stage1 and Stage3 must not share card art");
            Assert.AreNotSame(expectedCards[1], expectedCards[2], "Stage2 and Stage3 must not share card art");

            var genericCard = RequireGimmickSprite(GimmickSpriteLibrary.ButtonCard);
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            var existingPrologue = Object.FindObjectOfType<WebtoonPrologueController>();
            if (existingPrologue != null) existingPrologue.Dismiss();
            var existingIntro = Object.FindObjectOfType<IntroScreenController>();
            if (existingIntro != null) existingIntro.Dismiss();
            var freshIntro = IntroScreenController.Create(null);
            Assert.IsNotNull(freshIntro, "The title factory must create an intro surface for picker input coverage");
            Canvas.ForceUpdateCanvases();

            GameObject stage1Card = null;
            for (var i = 0; i < stages.Length; i++)
            {
                var card = GameObject.Find($"Stage_{stages[i]}");
                Assert.IsNotNull(card, $"Intro must create a stage card for {stages[i]}");
                var image = card.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(image, $"{stages[i]} card must retain its Image component");
                Assert.AreSame(expectedCards[i], image.sprite,
                    $"{stages[i]} card must use its generated dedicated art rather than a generic fallback");
                Assert.AreNotSame(genericCard, image.sprite,
                    $"{stages[i]} card must not silently fall back to ui_button_card while its generated art exists");

                var button = card.GetComponent<UnityEngine.UI.Button>();
                Assert.IsNotNull(button, $"{stages[i]} card must retain its Button component");
                Assert.AreSame(image, button.targetGraphic, $"{stages[i]} button must keep its card Image as targetGraphic");
                var expectedInteractable = !StageDefinitions.For(stages[i]).locked &&
                    StageProgress.IsUnlocked(StageProgressStore.Load(), stages[i]);
                Assert.AreEqual(expectedInteractable, button.interactable,
                    $"{stages[i]} card must preserve its existing structural and campaign lock behavior");
                if (stages[i] == GameManager.PendingStage)
                {
                    Assert.IsTrue(expectedInteractable, "Only a selectable stage may render as the active pick");
                    Assert.AreEqual(Color.white, image.color,
                        "The selected unlocked stage card must retain its selected visual state");
                }
                else if (!expectedInteractable)
                {
                    Assert.AreEqual(new Color(0.32f, 0.32f, 0.35f, 0.55f), image.color,
                        "A locked stage card must remain dim rather than appearing selected");
                }

                if (stages[i] == StageId.Stage1) stage1Card = card;
            }

            Assert.IsNotNull(stage1Card, "The unlocked Stage1 card must exist for pointer routing");
            var picker = GameObject.Find("StagePicker");
            Assert.IsNotNull(picker, "Intro must expose the stage picker input surface");
            var pickerGroup = picker.GetComponent<CanvasGroup>();
            Assert.IsNotNull(pickerGroup, "Stage picker must gate its visual and pointer state together");
            Assert.IsFalse(pickerGroup.blocksRaycasts,
                "An invisible stage picker must not intercept or route pointer input");

            var eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>();
            Assert.IsNotNull(eventSystem, "The intro must expose an EventSystem for stage-card pointer routing");
            var canvas = stage1Card.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas, "The Stage1 card must be parented by an active intro Canvas");
            var graphicRaycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Assert.IsNotNull(graphicRaycaster, "The intro Canvas must expose a GraphicRaycaster");
            var stage1Rect = stage1Card.GetComponent<RectTransform>();
            var screenPosition = RectTransformUtility.WorldToScreenPoint(
                graphicRaycaster.eventCamera,
                stage1Rect.TransformPoint(stage1Rect.rect.center));
            var pointerEventData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left
            };
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            eventSystem.RaycastAll(pointerEventData, raycastResults);
            foreach (var raycastResult in raycastResults)
            {
                var hitTransform = raycastResult.gameObject.transform;
                Assert.IsFalse(hitTransform == stage1Card.transform || hitTransform.IsChildOf(stage1Card.transform),
                    "An invisible Stage1 card must not receive pointer routing");
            }

            yield return new WaitForSecondsRealtime(3f);
            Canvas.ForceUpdateCanvases();
            picker = GameObject.Find("StagePicker");
            Assert.IsNotNull(picker, "The title flow must retain a stage picker after its entrance completes");
            pickerGroup = picker.GetComponent<CanvasGroup>();
            Assert.IsNotNull(pickerGroup, "The completed title flow must retain the picker interaction gate");
            stage1Card = GameObject.Find($"Stage_{StageId.Stage1}");
            Assert.IsNotNull(stage1Card, "The completed title flow must retain the unlocked Stage1 card");
            eventSystem = EventSystem.current ?? Object.FindObjectOfType<EventSystem>();
            Assert.IsNotNull(eventSystem, "The completed title flow must retain an EventSystem");
            canvas = stage1Card.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas, "The completed Stage1 card must remain parented by a Canvas");
            graphicRaycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            Assert.IsNotNull(graphicRaycaster, "The completed intro Canvas must retain a GraphicRaycaster");
            stage1Rect = stage1Card.GetComponent<RectTransform>();
            Assert.IsNotNull(stage1Rect, "The completed Stage1 card must retain its layout rectangle");
            screenPosition = RectTransformUtility.WorldToScreenPoint(
                graphicRaycaster.eventCamera,
                stage1Rect.TransformPoint(stage1Rect.rect.center));
            pointerEventData = new PointerEventData(eventSystem)
            {
                position = screenPosition,
                button = PointerEventData.InputButton.Left
            };
            Assert.IsTrue(pickerGroup.interactable, "The fully visible stage picker must enable its card interactions");
            Assert.IsTrue(pickerGroup.blocksRaycasts, "The fully visible stage picker must accept pointer routing");

            raycastResults.Clear();
            eventSystem.RaycastAll(pointerEventData, raycastResults);
            GameObject stage1Hit = null;
            foreach (var raycastResult in raycastResults)
            {
                var hitTransform = raycastResult.gameObject.transform;
                if (hitTransform == stage1Card.transform || hitTransform.IsChildOf(stage1Card.transform))
                {
                    stage1Hit = raycastResult.gameObject;
                    break;
                }
            }

            Assert.IsNotNull(stage1Hit, "A visible unlocked Stage1 card must receive the pointer route");
            ExecuteEvents.ExecuteHierarchy<IPointerClickHandler>(
                stage1Hit,
                pointerEventData,
                ExecuteEvents.pointerClickHandler);
            yield return null;
            Assert.AreEqual(StageId.Stage1, GameManager.PendingStage,
                "A visible Stage1 card pointer click must use the public stage selection route");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator Stage1BarrelSkin_ResolvesSeparately_WhileStage1KeepsItsBarrelPresentation()
        {
            var stage1Barrel = RequireGimmickSprite(GimmickSpriteLibrary.Stage1Barrel);
            var genericBarrel = RequireGimmickSprite(GimmickSpriteLibrary.Barrel);
            Assert.AreNotSame(genericBarrel, stage1Barrel,
                "Stage1's generated barrel skin must be distinct from the legacy barrel resource");

            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.IsNotNull(GameManager.Instance, "Stage1 must create its GameManager before barrel presentation is inspected");
            Assert.AreEqual(StageId.Stage1, GameManager.Instance.currentStage, "The loaded scene must resolve Stage1");
            var stage1Frames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.Stage1BarrelAnim);
            Assert.IsNotNull(stage1Frames,
                $"Stage1 barrel animation frames must resolve from Resources/Gimmicks/{GimmickAnimLibrary.Stage1BarrelAnim}");
            Assert.AreEqual(4, stage1Frames.Length, "Stage1 barrel animation must use the supplied four-frame pulse");
            for (var i = 0; i < stage1Frames.Length; i++)
            {
                Assert.AreEqual($"stage1_barrel_anim_{i:000}", stage1Frames[i].name,
                    "Stage1 barrel frames must retain their deterministic resource order");
                Assert.AreEqual(stage1Barrel.rect.size.x, stage1Frames[i].rect.size.x, 0.001f,
                    "Every Stage1 barrel frame must use the static barrel canvas width");
                Assert.AreEqual(stage1Barrel.rect.size.y, stage1Frames[i].rect.size.y, 0.001f,
                    "Every Stage1 barrel frame must use the static barrel canvas height");
            }
            Assert.AreEqual(0, GimmickFrameAnimator.LoopFrameAt(0f, 0.25f, stage1Frames.Length));
            Assert.AreEqual(0, GimmickFrameAnimator.LoopFrameAt(0.2499f, 0.25f, stage1Frames.Length));
            Assert.AreEqual(1, GimmickFrameAnimator.LoopFrameAt(0.25f, 0.25f, stage1Frames.Length));
            Assert.AreEqual(1, GimmickFrameAnimator.LoopFrameAt(0.4999f, 0.25f, stage1Frames.Length));
            Assert.AreEqual(2, GimmickFrameAnimator.LoopFrameAt(0.5f, 0.25f, stage1Frames.Length));
            Assert.AreEqual(2, GimmickFrameAnimator.LoopFrameAt(0.7499f, 0.25f, stage1Frames.Length));
            Assert.AreEqual(3, GimmickFrameAnimator.LoopFrameAt(0.75f, 0.25f, stage1Frames.Length));
            Assert.AreEqual(3, GimmickFrameAnimator.LoopFrameAt(0.9999f, 0.25f, stage1Frames.Length));
            Assert.AreEqual(0, GimmickFrameAnimator.LoopFrameAt(1f, 0.25f, stage1Frames.Length));

            var barrel = Object.FindObjectOfType<ExplosiveGimmick>();
            Assert.IsNotNull(barrel, "Stage1 must retain its existing explosive barrel placement");
            var barrelRenderer = barrel.GetComponent<SpriteRenderer>();
            Assert.IsNotNull(barrelRenderer, "Stage1 barrel must retain its SpriteRenderer");
            var animator = barrel.GetComponent<GimmickFrameAnimator>();
            Assert.IsNotNull(animator, "Stage1 barrel must retain its existing animation attachment after the skin changes");
            Assert.AreEqual(4f, animator.fps, 0.0001f, "Stage1 barrel must play its four-frame pulse at 4 FPS");
            Assert.GreaterOrEqual(System.Array.IndexOf(stage1Frames, barrelRenderer.sprite), 0,
                "Stage1 barrel's visible frame must come from the generated Stage1 barrel animation rather than barrel_anim");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator Stage2SpikeTrap_UsesGeneratedDormantAndArmedSkins()
        {
            var dormant = RequireGimmickSprite(GimmickSpriteLibrary.Stage2SpikeTrapDormant);
            var armed = RequireGimmickSprite(GimmickSpriteLibrary.Stage2SpikeTrapArmed);
            Assert.AreNotSame(dormant, armed, "Stage2 dormant and armed spike-trap art must be distinct");
            Assert.AreNotSame(RequireGimmickSprite(GimmickSpriteLibrary.SpikeTrapDormant), dormant,
                "Stage2 dormant trap art must not silently use the generic trap resource");
            Assert.AreNotSame(RequireGimmickSprite(GimmickSpriteLibrary.SpikeTrapArmed), armed,
                "Stage2 armed trap art must not silently use the generic trap resource");

            yield return LoadAndBeginStage(StageId.Stage2);
            Assert.AreEqual(StageId.Stage2, GameManager.Instance.currentStage, "The loaded scene must resolve Stage2");

            var trapObject = new GameObject("Stage2SpikeTrapPresentationProbe");
            trapObject.transform.position = new Vector3(1000f, 1000f, 0f);
            var trapRenderer = trapObject.AddComponent<SpriteRenderer>();
            var trap = trapObject.AddComponent<SpikeTrapGimmick>();
            Assert.AreSame(dormant, trapRenderer.sprite,
                "A Stage2 spike trap must start with its generated dormant skin");

            var unitObject = new GameObject("Stage2SpikeTrapTriggerProbe");
            unitObject.transform.position = trapObject.transform.position;
            unitObject.AddComponent<CircleCollider2D>();
            unitObject.AddComponent<UnitController>();
            try
            {
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.AreSame(dormant, trapRenderer.sprite,
                    "An arming Stage2 spike trap must retain its dormant skin until activation");

                yield return new WaitForSecondsRealtime(trap.armDelaySeconds + 0.1f);
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.AreSame(armed, trapRenderer.sprite,
                    "An active Stage2 spike trap must use its generated armed skin");
            }
            finally
            {
                Object.Destroy(trapObject);
                Object.Destroy(unitObject);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator Stage3FrostVent_UsesGeneratedSkin_OnlyForFrostStyle()
        {
            var stage3Frost = RequireGimmickSprite(GimmickSpriteLibrary.Stage3FrostVent);
            var genericFrost = RequireGimmickSprite(GimmickSpriteLibrary.VentFrost);
            Assert.AreNotSame(genericFrost, stage3Frost,
                "Stage3's generated Frost vent skin must be distinct from the generic Frost vent resource");

            GameManager.PendingStage = StageId.Stage3;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsNotNull(GameManager.Instance, "Stage3 must create its GameManager before Frost vent presentation is inspected");
            Assert.AreEqual(StageId.Stage3, GameManager.Instance.currentStage, "The loaded scene must resolve Stage3");

            var frostObject = new GameObject("Stage3FrostVentPresentationProbe");
            var frostRenderer = frostObject.AddComponent<SpriteRenderer>();
            var frostVent = frostObject.AddComponent<EruptionVentGimmick>();
            frostVent.style = EruptionStyle.Frost;

            var magmaObject = new GameObject("Stage3MagmaVentPresentationProbe");
            var magmaRenderer = magmaObject.AddComponent<SpriteRenderer>();
            var magmaVent = magmaObject.AddComponent<EruptionVentGimmick>();
            magmaVent.style = EruptionStyle.Magma;

            try
            {
                yield return null;
                Assert.AreSame(stage3Frost, frostRenderer.sprite,
                    "A Frost-style vent on Stage3 must use its generated Frost skin");
                var magma = RequireGimmickSprite(GimmickSpriteLibrary.VentMagma);
                Assert.AreSame(magma, magmaRenderer.sprite,
                    "A non-Frost Stage3 vent must retain its existing dedicated style resource");
                Assert.AreNotSame(stage3Frost, magmaRenderer.sprite,
                    "Stage3's generated Frost skin must not be used for non-Frost vent styles");
            }
            finally
            {
                Object.Destroy(frostObject);
                Object.Destroy(magmaObject);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator WebtoonPrologue_PageOneConstructsKnightPrologueIdleActor()
        {
            const string resourcePath = "GeneratedUnitFrames/KnightPrologue/Idle";
            var frames = Resources.LoadAll<Sprite>(resourcePath);
            Assert.AreEqual(4, frames.Length,
                $"Page_1's KnightPrologue actor must resolve four Resources/{resourcePath} frames.");
            foreach (var frame in frames)
            {
                Assert.IsNotNull(frame, $"Resources/{resourcePath} must not contain null KnightPrologue frames.");
            }

            System.Array.Sort(frames, (left, right) => string.CompareOrdinal(left.name, right.name));
            var expectedFrameNames = new[] { "idle_000", "idle_001", "idle_002", "idle_003" };
            for (var frameIndex = 0; frameIndex < expectedFrameNames.Length; frameIndex++)
            {
                Assert.AreEqual(expectedFrameNames[frameIndex], frames[frameIndex].name,
                    $"KnightPrologue's sorted frame {frameIndex} must retain its generated idle ordering.");
            }


            var idleFrame = Resources.Load<Sprite>($"{resourcePath}/idle_000");
            Assert.IsNotNull(idleFrame, "KnightPrologue's initial idle_000 resource must resolve directly.");

            var prologue = WebtoonPrologueController.Create(null);
            try
            {
                var page = prologue.transform.Find("MobileSafeArea/Viewport/PanelStrip/Page_1");
                Assert.IsNotNull(page, "The actual webtoon construction must build Page_1.");
                var leftActor = page.Find("Panel/LeftActor")?.GetComponent<UnityEngine.UI.Image>();
                Assert.IsNotNull(leftActor, "Page_1 must build its left actor as a UI Image.");
                Assert.IsTrue(leftActor.enabled, "Page_1's KnightPrologue actor must be renderable.");
                Assert.AreSame(idleFrame, leftActor.sprite,
                    "Page_1's actual left actor must begin on KnightPrologue idle_000.");
            }
            finally
            {
                Object.Destroy(prologue.gameObject);
            }

            yield return null;
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator ChronicleReplay_EntitlementGatesTitleAction_AndReturnsToPausedTitleWithoutRuntimeMutation()
        {
            var originalEconomyBalance = SiegePrototypeEconomy.Balance;
            var originalBattleBannerSeal = SiegePrototypeEconomy.HasBattleBannerSeal;
            var originalStageProgress = StageProgressStore.Load();
            var hadOriginalLeaderboard = PlayerPrefs.HasKey(LeaderboardStore.PrefsKey);
            var originalLeaderboard = hadOriginalLeaderboard
                ? PlayerPrefs.GetString(LeaderboardStore.PrefsKey)
                : null;
            var originalStorefront = MobileStorefront.Instance;

            PlayerPrefs.DeleteKey(ChroniclePackPrefsKey);
            PlayerPrefs.Save();
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            var titleDeadline = Time.realtimeSinceStartup + 8f;
            var coldOpenTitle = Object.FindObjectOfType<IntroScreenController>();
            var coldOpenPrologue = Object.FindObjectOfType<WebtoonPrologueController>();
            var coldOpenSkipButton = coldOpenPrologue?.transform.Find("MobileSafeArea/Viewport/SkipToTitleButton")
                ?.GetComponent<UnityEngine.UI.Button>();
            while (coldOpenTitle == null &&
                   coldOpenSkipButton == null &&
                   Time.realtimeSinceStartup < titleDeadline)
            {
                yield return null;
                coldOpenTitle = Object.FindObjectOfType<IntroScreenController>();
                coldOpenPrologue = Object.FindObjectOfType<WebtoonPrologueController>();
                coldOpenSkipButton = coldOpenPrologue?.transform.Find("MobileSafeArea/Viewport/SkipToTitleButton")
                    ?.GetComponent<UnityEngine.UI.Button>();
            }

            if (coldOpenSkipButton != null)
            {
                Assert.IsNotNull(coldOpenSkipButton,
                    "The initial prologue must expose its public SKIP action before title entitlement coverage.");
                coldOpenSkipButton.onClick.Invoke();
            }

            while (Object.FindObjectOfType<IntroScreenController>() == null &&
                   Time.realtimeSinceStartup < titleDeadline)
            {
                yield return null;
            }

            var unentitledTitle = Object.FindObjectOfType<IntroScreenController>();
            Assert.IsNotNull(unentitledTitle,
                "The title must render after the cold-open is skipped or when the session has already seen it.");
            Assert.IsNull(unentitledTitle.transform.Find("MobileSafeArea/Root/ChronicleReplayButton"),
                "An unentitled title must not render a ChronicleReplayButton.");
            Assert.AreEqual(GameState.Intro, GameManager.Instance.currentState,
                "The title entitlement check must remain in the Intro state.");
            Assert.AreEqual(0f, Time.timeScale,
                "The title entitlement check must keep runtime time paused.");

            PlayerPrefs.SetInt(ChroniclePackPrefsKey, 1);
            PlayerPrefs.Save();
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;

            titleDeadline = Time.realtimeSinceStartup + 8f;
            while (Object.FindObjectOfType<IntroScreenController>() == null &&
                   Time.realtimeSinceStartup < titleDeadline)
            {
                yield return null;
            }

            var entitledTitle = Object.FindObjectOfType<IntroScreenController>();
            Assert.IsNotNull(entitledTitle,
                "A persistent Chronicle entitlement must render a fresh title after scene reconstruction.");
            var replayButton = entitledTitle.transform.Find("MobileSafeArea/Root/ChronicleReplayButton")
                ?.GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(replayButton,
                "An entitled title must render the named ChronicleReplayButton.");
            Assert.IsTrue(replayButton.interactable,
                "The entitled ChronicleReplayButton must be an interactive title action.");

            replayButton.onClick.Invoke();
            yield return null;
            Assert.AreSame(originalStorefront, MobileStorefront.Instance,
                "Chronicle replay must not open or initialize a storefront.");
            Assert.IsTrue(MobileStoreEntitlements.HasChroniclePack,
                "Chronicle replay must not revoke the existing Chronicle entitlement.");

            var replayPrologue = Object.FindObjectOfType<WebtoonPrologueController>();
            Assert.IsNotNull(replayPrologue,
                "Clicking ChronicleReplayButton must open the existing Webtoon prologue.");
            Assert.AreEqual(GameState.Intro, GameManager.Instance.currentState,
                "Opening the replay prologue must preserve the Intro state.");
            Assert.AreEqual(0f, Time.timeScale,
                "Opening the replay prologue must preserve the title's paused runtime.");

            var replaySkipButton = replayPrologue.transform.Find("MobileSafeArea/Viewport/SkipToTitleButton")
                ?.GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(replaySkipButton,
                "The replayed prologue must expose its public SKIP action.");
            Assert.IsTrue(replaySkipButton.interactable,
                "The replayed prologue SKIP action must be interactive.");
            replaySkipButton.onClick.Invoke();

            var returnDeadline = Time.realtimeSinceStartup + 8f;
            IntroScreenController returnedTitle;
            do
            {
                yield return null;
                returnedTitle = Object.FindObjectOfType<IntroScreenController>();
            } while ((returnedTitle == null || returnedTitle == entitledTitle) &&
                     Time.realtimeSinceStartup < returnDeadline);

            Assert.IsNotNull(returnedTitle,
                "Completing the replay through SKIP must restore a fresh title screen.");
            Assert.AreNotSame(entitledTitle, returnedTitle,
                "Completing the replay must recreate rather than retain the title that launched it.");
            Assert.IsNotNull(returnedTitle.transform.Find("MobileSafeArea/Root/ChronicleReplayButton")
                    ?.GetComponent<UnityEngine.UI.Button>(),
                "The persistent Chronicle entitlement must keep the replay action available after return.");
            Assert.AreEqual(GameState.Intro, GameManager.Instance.currentState,
                "Completing the replay must return to the Intro state.");
            Assert.AreEqual(0f, Time.timeScale,
                "Completing the replay must return to a paused title runtime.");
            Assert.AreEqual(originalEconomyBalance, SiegePrototypeEconomy.Balance,
                "Chronicle replay must not mutate the prototype economy balance.");
            Assert.AreEqual(originalBattleBannerSeal, SiegePrototypeEconomy.HasBattleBannerSeal,
                "Chronicle replay must not mutate the prototype banner entitlement.");
            Assert.AreEqual(originalStageProgress, StageProgressStore.Load(),
                "Chronicle replay must not mutate campaign stage progress.");
            Assert.AreEqual(hadOriginalLeaderboard, PlayerPrefs.HasKey(LeaderboardStore.PrefsKey),
                "Chronicle replay must not add or remove leaderboard persistence.");
            if (hadOriginalLeaderboard)
            {
                Assert.AreEqual(originalLeaderboard, PlayerPrefs.GetString(LeaderboardStore.PrefsKey),
                    "Chronicle replay must not mutate leaderboard persistence.");
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator BestOfThreePlayerClinch_AwardsOnce_UnlocksBanner_AndTitleResetsDemo()
        {
            SiegePrototypeEconomy.ResetDemo();
            LeaderboardStore.Save(new SiegeRank.Board());
            StageProgressStore.Save(StageId.Stage1);

            yield return LoadAndBeginStage(StageId.Stage1);

            var firstGameManager = GameManager.Instance;
            var firstEnemyCore = FindCore(false);
            Assert.IsNotNull(firstEnemyCore, "The first actual siege must expose an enemy core to win through public damage.");
            firstEnemyCore.TakeDamage(firstEnemyCore.currentHP + 1f);
            yield return WaitForResultsScreen();

            var firstResults = Object.FindObjectOfType<ResultsScreenController>();
            Assert.IsNotNull(firstResults, "The first game must render its actual results screen.");
            Assert.Zero(SiegePrototypeEconomy.Balance,
                "The first best-of-three win must not award marks before the series is clinched.");
            Assert.IsNull(firstResults.transform.Find("MobileSafeArea/PrototypeBannerButton"),
                "The one-time banner exchange must not be created before a decided player series win.");
            Assert.Zero(LeaderboardStore.Load().entries.Count,
                "The first game must not write a ranked entry before the series is decided.");

            var nextGameButton = firstResults.transform.Find("MobileSafeArea/NextGameButton")
                ?.GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(nextGameButton, "A mid-series actual results screen must render NextGameButton.");
            Assert.IsTrue(nextGameButton.interactable, "The rendered next-game action must be clickable.");
            nextGameButton.onClick.Invoke();

            yield return WaitForReloadedPlayerTurn(firstGameManager);

            var clinchGameManager = GameManager.Instance;
            var clinchEnemyCore = FindCore(false);
            Assert.IsNotNull(clinchEnemyCore, "The continued series game must expose an enemy core to win through public damage.");
            clinchEnemyCore.TakeDamage(clinchEnemyCore.currentHP + 1f);
            yield return WaitForResultsScreen();

            var clinchResults = Object.FindObjectOfType<ResultsScreenController>();
            Assert.IsNotNull(clinchResults, "The clinching game must render its actual results screen.");
            Assert.AreEqual(SiegePrototypeEconomy.SeriesVictoryMarks, SiegePrototypeEconomy.Balance,
                "Only the 2-0 clinch must award the exact published series-victory mark total.");
            Assert.AreEqual(1, LeaderboardStore.Load().entries.Count,
                "The decided series must write exactly one ranked entry after the clinch.");
            clinchGameManager.CheckVictoryConditions();
            yield return null;
            Assert.AreEqual(SiegePrototypeEconomy.SeriesVictoryMarks, SiegePrototypeEconomy.Balance,
                "Rechecking a completed game-over state must not issue the clinch reward twice.");

            var prototypeBannerButton = clinchResults.transform.Find("MobileSafeArea/PrototypeBannerButton")
                ?.GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(prototypeBannerButton,
                "A decided player series win must render PrototypeBannerButton.");
            Assert.IsTrue(prototypeBannerButton.interactable,
                "The rendered prototype banner exchange must initially be clickable with a clinch reward.");
            prototypeBannerButton.onClick.Invoke();
            yield return null;
            Assert.IsTrue(SiegePrototypeEconomy.HasBattleBannerSeal,
                "Clicking the rendered prototype banner must unlock the one-time seal.");
            Assert.Zero(SiegePrototypeEconomy.Balance,
                "Unlocking the rendered banner must consume the exact clinch reward.");
            Assert.IsFalse(prototypeBannerButton.interactable,
                "A successful rendered banner exchange must disable its button.");

            var titleButton = clinchResults.transform.Find("MobileSafeArea/TitleButton")?.GetComponent<UnityEngine.UI.Button>();
            Assert.IsNotNull(titleButton, "The decided series results must render TitleButton.");
            titleButton.onClick.Invoke();
            yield return WaitForReloadedGameManager(clinchGameManager);

            Assert.AreEqual(StageId.Stage1, GameManager.PendingStage,
                "The rendered TitleButton route must restore the title's Stage1 selection.");
            Assert.Zero(SiegePrototypeEconomy.Balance,
                "The rendered TitleButton route must ResetDemo's mark ledger.");
            Assert.IsFalse(SiegePrototypeEconomy.HasBattleBannerSeal,
                "The rendered TitleButton route must ResetDemo's one-time banner unlock.");
        }

        private static IEnumerator WaitForResultsScreen()
        {
            var deadline = Time.realtimeSinceStartup + 8f;
            while (Object.FindObjectOfType<ResultsScreenController>() == null &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsNotNull(Object.FindObjectOfType<ResultsScreenController>(),
                "Ending an actual siege must render a ResultsScreenController within the bounded wait.");
        }

        private static IEnumerator WaitForReloadedPlayerTurn(GameManager previousGameManager)
        {
            yield return WaitForReloadedGameManager(previousGameManager);

            var deadline = Time.realtimeSinceStartup + 3f;
            while (GameManager.Instance.currentState != GameState.PlayerTurn &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.AreEqual(GameState.PlayerTurn, GameManager.Instance.currentState,
                "The rendered next-game route must resume the continuing series on a real player turn.");
        }

        private static IEnumerator WaitForReloadedGameManager(GameManager previousGameManager)
        {
            var deadline = Time.realtimeSinceStartup + 8f;
            while ((GameManager.Instance == null || GameManager.Instance == previousGameManager) &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.IsNotNull(GameManager.Instance,
                "The rendered results action must reload a GameManager within the bounded wait.");
            Assert.AreNotSame(previousGameManager, GameManager.Instance,
                "The rendered results action must create a fresh GameManager scene instance.");
        }

        private static CastleCoreGimmick FindCore(bool playerCore)
        {
            foreach (var core in Object.FindObjectsOfType<CastleCoreGimmick>())
            {
                if (core.isPlayerCore == playerCore) return core;
            }

            return null;
        }

        private void RestorePlayerPrefsState()
        {
            if (hadLeaderboardPrefs) PlayerPrefs.SetString(LeaderboardStore.PrefsKey, originalLeaderboardPrefs);
            else PlayerPrefs.DeleteKey(LeaderboardStore.PrefsKey);
            if (hadStageProgressPrefs) PlayerPrefs.SetInt(StageProgressPrefsKey, originalStageProgressPrefs);
            else PlayerPrefs.DeleteKey(StageProgressPrefsKey);
            if (hadWarChestBalancePrefs) PlayerPrefs.SetInt(WarChestBalancePrefsKey, originalWarChestBalancePrefs);
            else PlayerPrefs.DeleteKey(WarChestBalancePrefsKey);
            if (hadBattleBannerSealPrefs) PlayerPrefs.SetInt(BattleBannerSealPrefsKey, originalBattleBannerSealPrefs);
            else PlayerPrefs.DeleteKey(BattleBannerSealPrefsKey);
            if (hadChroniclePackPrefs) PlayerPrefs.SetInt(ChroniclePackPrefsKey, originalChroniclePackPrefs);
            else PlayerPrefs.DeleteKey(ChroniclePackPrefsKey);
            PlayerPrefs.Save();
        }

        private static void AssertBackgroundCoversCamera(SpriteRenderer renderer, Camera camera, StageId stage,
            string framing)
        {
            var cameraHalfWidth = camera.orthographicSize * camera.aspect;
            var cameraMin = camera.transform.position - new Vector3(cameraHalfWidth, camera.orthographicSize, 0f);
            var cameraMax = camera.transform.position + new Vector3(cameraHalfWidth, camera.orthographicSize, 0f);
            Assert.LessOrEqual(renderer.bounds.min.x, cameraMin.x,
                $"{stage} background must cover the camera's left edge during {framing}");
            Assert.GreaterOrEqual(renderer.bounds.max.x, cameraMax.x,
                $"{stage} background must cover the camera's right edge during {framing}");
            Assert.LessOrEqual(renderer.bounds.min.y, cameraMin.y,
                $"{stage} background must cover the camera's bottom edge during {framing}");
            Assert.GreaterOrEqual(renderer.bounds.max.y, cameraMax.y,
                $"{stage} background must cover the camera's top edge during {framing}");
        }

        private static Sprite RequireGimmickSprite(string key)
        {
            var resourceSprite = Resources.Load<Sprite>($"Gimmicks/{key}");
            Assert.IsNotNull(resourceSprite,
                $"Generated gimmick sprite '{key}' must resolve directly from Resources/Gimmicks without an editor-only fallback");
            var librarySprite = GimmickSpriteLibrary.Load(key);
            Assert.AreSame(resourceSprite, librarySprite,
                $"GimmickSpriteLibrary must expose the exact Resources/Gimmicks sprite '{key}'");
            return resourceSprite;
        }

        private static IEnumerator LoadAndBeginStage(StageId stage)
        {
            GameManager.PendingStage = stage;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);

            Assert.IsNotNull(GameManager.Instance, "GameManager must exist after loading gameplay");
            GameManager.Instance.BeginSiege();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(GameState.PlayerTurn, GameManager.Instance.currentState,
                "Beginning the siege must hand control to the player before teardown coverage runs");
        }

        private static UnitController InitialUnitTemplate(GameManager gameManager, UnitType unitType)
        {
            GameObject prefab;
            switch (unitType)
            {
                case UnitType.Knight:
                    prefab = gameManager.knightPrefab;
                    break;
                case UnitType.Archer:
                    prefab = gameManager.archerPrefab;
                    break;
                case UnitType.Bomber:
                    prefab = gameManager.bomberPrefab;
                    break;
                default:
                    return null;
            }

            return prefab != null ? prefab.GetComponent<UnitController>() : null;
        }

        private static MovingGimmick ActiveChariot()
        {
            foreach (var movingGimmick in Object.FindObjectsOfType<MovingGimmick>())
            {
                if (movingGimmick.chariotMode) return movingGimmick;
            }

            return null;
        }

        private static int ActiveChariotCount()
        {
            var count = 0;
            foreach (var movingGimmick in Object.FindObjectsOfType<MovingGimmick>())
            {
                if (movingGimmick.chariotMode) count++;
            }

            return count;
        }

        private static bool HasActiveChariot() => ActiveChariot() != null;
    }
}
