using System.Collections.Generic;
using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CastleBusters.Tests
{
    [TestFixture]
    public sealed class ProductionPathRegressionTests
    {
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private RegisteredEventSystem registeredEventSystem;

        [TearDown]
        public void TearDown()
        {
            if (registeredEventSystem != null)
            {
                registeredEventSystem.UnregisterForTest();
                registeredEventSystem = null;
            }

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null)
                {
                    Object.DestroyImmediate(createdObjects[i]);
                }
            }

            createdObjects.Clear();
        }

        [Test]
        public void GameManager_EndTurn_CancelsActiveAimBeforeAdvancingToAiTurn()
        {
            var gameManager = CreateObject("EndTurnRegressionGameManager").AddComponent<GameManager>();
            var launchManager = CreateObject("EndTurnRegressionLaunchManager").AddComponent<LaunchManager>();
            var trajectoryLine = CreateObject("EndTurnRegressionTrajectory").AddComponent<LineRenderer>();
            var rubberBandLine = CreateObject("EndTurnRegressionRubberBand").AddComponent<LineRenderer>();

            launchManager.trajectoryLine = trajectoryLine;
            launchManager.rubberBandLine = rubberBandLine;
            trajectoryLine.positionCount = 4;
            rubberBandLine.positionCount = 3;
            SetPrivateField(launchManager, "isDragging", true);

            gameManager.currentState = GameState.PlayerTurn;
            SetPrivateField(gameManager, "isPlayerTurn", true);
            SetPrivateField(gameManager, "turnCount", 6);
            SetPrivateField(gameManager, "cachedLaunchManager", launchManager);

            Assert.That(launchManager.IsAiming, Is.True, "Precondition: the simulated draw must be active.");
            Assert.That(trajectoryLine.positionCount, Is.GreaterThan(0), "Precondition: trajectory preview must be visible.");
            Assert.That(rubberBandLine.positionCount, Is.GreaterThan(0), "Precondition: rubber-band preview must be visible.");

            InvokePrivate<GameManager>(gameManager, "EndTurn");

            Assert.That(launchManager.IsAiming, Is.False,
                "Turn handoff must cancel the in-progress draw before control changes sides.");
            Assert.That(trajectoryLine.positionCount, Is.Zero,
                "Turn handoff must remove the stale trajectory preview.");
            Assert.That(rubberBandLine.positionCount, Is.Zero,
                "Turn handoff must remove the stale rubber-band preview.");
            Assert.That(gameManager.TurnCount, Is.EqualTo(7),
                "Ending the player turn must advance the turn exactly once.");
            Assert.That(gameManager.IsPlayerTurn, Is.False,
                "Ending the player turn must hand control to the AI.");
            Assert.That(gameManager.currentState, Is.EqualTo(GameState.AITurn),
                "The public turn state must agree with the AI handoff.");
        }

        [Test]
        public void LaunchManager_CancelAim_ClearsDrawFeedbackAndRestoresSelectedUnitGuide()
        {
            LaunchManager launchManager = CreateActiveAim(
                "CancelAimRegression",
                out _,
                out LineRenderer trajectoryLine,
                out LineRenderer rubberBandLine,
                out TextMeshProUGUI controlGuide,
                out TextMeshProUGUI launchAlert);

            Assert.That(launchManager.IsAiming, Is.True, "Precondition: the production pointer seam must begin an active draw.");
            Assert.That(trajectoryLine.positionCount, Is.GreaterThan(0), "Precondition: active aim must render its trajectory.");
            Assert.That(rubberBandLine.positionCount, Is.EqualTo(3), "Precondition: active aim must render the three-point rubber band.");

            launchAlert.text = "STALE LAUNCH ALERT";
            controlGuide.text = "STALE AIM GUIDE";
            controlGuide.color = Color.red;

            launchManager.CancelAim();

            Assert.That(launchManager.IsAiming, Is.False, "CancelAim must leave no active draw.");
            Assert.That(trajectoryLine.positionCount, Is.Zero, "CancelAim must remove the trajectory preview.");
            Assert.That(rubberBandLine.positionCount, Is.Zero, "CancelAim must remove the rubber-band preview.");
            Assert.That(launchAlert.text, Is.Empty, "CancelAim must clear launch alerts left by the abandoned draw.");
            Assert.That(controlGuide.text, Does.Contain("KNIGHT"),
                "Cleanup must restore the selected unit identity instead of leaving stale aim copy.");
            Assert.That(controlGuide.text, Does.Contain("준비"), "Cleanup must restore the ready-state guide.");
            Assert.That(controlGuide.text, Does.Contain("당겨"),
                "The restored Knight guide must explain the launch gesture (drag-from-anywhere pull).");
            Assert.That(controlGuide.text, Does.Contain("발사"), "The restored Knight guide must explain the launch action.");
            Assert.That(controlGuide.color, Is.EqualTo(new Color(0.8f, 0.95f, 1f, 0.95f)),
                "Cleanup must restore the normal ready-guide color after contaminated aim feedback.");
        }

        [Test]
        public void LaunchManager_GameOverPointerRelease_DoesNotLaunchResolveTurnOrConsumeActiveShot()
        {
            LaunchManager launchManager = CreateActiveAim(
                "GameOverReleaseRegression",
                out GameManager gameManager,
                out LineRenderer trajectoryLine,
                out LineRenderer rubberBandLine,
                out _,
                out _);
            var unitsBeforeRelease = new HashSet<UnitController>(Object.FindObjectsOfType<UnitController>());

            gameManager.playerLastStand = LastStand.Phase.Active;
            gameManager.currentState = GameState.GameOver;
            SetPrivateField(gameManager, "turnCount", 9);
            SetPrivateField(gameManager, "isResolvingTurn", false);

            Assert.That(launchManager.IsAiming, Is.True, "Precondition: GameOver must receive an already-held draw.");
            Assert.That(trajectoryLine.positionCount, Is.GreaterThan(0), "Precondition: the held draw must have a visible trajectory.");
            Assert.That(rubberBandLine.positionCount, Is.GreaterThan(0), "Precondition: the held draw must have a visible rubber band.");

            launchManager.SetSimulatedPointer(
                launchManager.GetLaunchAnchorPosition() + new Vector2(2f, 1f),
                pressed: false,
                held: false,
                released: true);
            InvokePrivate<object>(launchManager, "Update");

            foreach (UnitController candidate in Object.FindObjectsOfType<UnitController>())
            {
                Assert.That(unitsBeforeRelease.Contains(candidate), Is.True,
                    "A release received during GameOver must not instantiate a launched unit.");
            }
            Assert.That(gameManager.IsResolvingTurn, Is.False,
                "A suppressed GameOver release must not notify GameManager that a launch is resolving.");
            Assert.That(gameManager.TurnCount, Is.EqualTo(9),
                "A suppressed GameOver release must not resolve or advance the turn.");
            Assert.That(gameManager.playerLastStand, Is.EqualTo(LastStand.Phase.Active),
                "A suppressed GameOver release must not consume the player's active one-shot launch resource.");
            Assert.That(gameManager.currentState, Is.EqualTo(GameState.GameOver),
                "Suppressed release input must leave the terminal game state unchanged.");
        }

        [Test]
        public void DeploymentController_OneShotCannonPlacement_ConsumesTheTurnShot()
        {
            var gameManager = CreateObject("OneShotCannonGameManager").AddComponent<GameManager>();
            var deployment = CreateObject("OneShotCannonDeployment").AddComponent<DeploymentController>();

            gameManager.enforceOneShotTurns = true;
            gameManager.currentState = GameState.PlayerTurn;
            SetPrivateField(gameManager, "isPlayerTurn", true);
            SetPrivateField(gameManager, "turnCount", 6);

            // EditMode never runs Awake, so the singleton TryDeploy reads must be wired by
            // hand — otherwise the turn gate sees turn 0 and reports the cannon Locked.
            var instanceProperty = typeof(GameManager).GetProperty(
                nameof(GameManager.Instance), BindingFlags.Public | BindingFlags.Static);
            var previousInstance = (GameManager)instanceProperty.GetValue(null);
            instanceProperty.SetValue(null, gameManager);

            try
            {
                // Auto-property backing fields: enough supply for the 12-cost battery, and the
                // breach requirement already earned.
                SetPrivateField(deployment, "<PlayerSupply>k__BackingField", 20f);
                SetPrivateField(deployment, "<PlayerBreaches>k__BackingField",
                    DeploymentRules.CannonBreachRequirement);

                var reason = deployment.TryDeploy(DeployCard.Cannon, new Vector2(-3f, 1f), true);

                Assert.That(reason, Is.EqualTo(DeployBlockReason.None),
                    "With breach and supply met, siting the battery must succeed in the one-shot loop.");
                Assert.That(gameManager.IsResolvingTurn, Is.True,
                    "The emplacement is the turn's action — it must resolve the turn like a volley does.");
                Assert.That(gameManager.TryCommitTurnShot(), Is.False,
                    "A turn that bought an installation must not also be able to fire.");
            }
            finally
            {
                instanceProperty.SetValue(null, previousInstance);
            }
        }

        [Test]
        public void DeploymentController_SpawnCannon_NeutralizesUnitDefaultsForStationaryInstallation()
        {
            var deployment = CreateObject("CannonSpawnRegressionDeployment").AddComponent<DeploymentController>();

            var cannon = InvokePrivate<GameObject>(deployment, "SpawnCannon", new Vector2(2f, 3f), true);
            Assert.That(cannon, Is.Not.Null, "The Cannon deployment path must produce an installation.");
            createdObjects.Add(cannon);

            var box = cannon.GetComponent<BoxCollider2D>();
            var body = cannon.GetComponent<Rigidbody2D>();
            var unit = cannon.GetComponent<UnitController>();
            var characterAnimator = cannon.GetComponent<UnitSpriteAnimator>();

            Assert.That(cannon.transform.localScale, Is.EqualTo(Vector3.one),
                "Cannon must undo UnitController.Awake's character root scale.");
            Assert.That(box, Is.Not.Null, "The Cannon installation must expose its collision footprint.");
            Assert.That(box.size, Is.EqualTo(new Vector2(0.95f, 0.75f)),
                "Cannon must restore its authored full-size collision footprint after UnitController.Awake.");
            Assert.That(characterAnimator == null || !characterAnimator.isActiveAndEnabled, Is.True,
                "The stationary Cannon must not retain UnitController.Awake's character animator.");
            Assert.That(unit, Is.Not.Null, "The Cannon installation must participate in unit combat and damage.");
            Assert.That(unit.unitType, Is.EqualTo(UnitType.Cannon),
                "The spawned installation must expose the Cannon identity to targeting and cap rules.");
            Assert.That(unit.maxHP, Is.EqualTo(140f),
                "The spawned Cannon must expose the documented 140 maximum HP.");
            Assert.That(unit.currentHP, Is.EqualTo(140f),
                "A newly spawned Cannon must begin at its full documented HP.");
            Assert.That(unit.CurrentState, Is.EqualTo(UnitState.Grounded),
                "A deployed Cannon must begin as a grounded installation, not a launched body.");
            Assert.That(body, Is.Not.Null, "The Cannon installation must expose a physics body.");
            Assert.That(body.bodyType, Is.EqualTo(RigidbodyType2D.Kinematic),
                "The deployed Cannon must remain fixed at the paid placement position.");
            Assert.That(body.constraints, Is.EqualTo(RigidbodyConstraints2D.FreezeAll),
                "The deployed Cannon must not translate or rotate under combat impulses.");
        }

        private LaunchManager CreateActiveAim(
            string prefix,
            out GameManager gameManager,
            out LineRenderer trajectoryLine,
            out LineRenderer rubberBandLine,
            out TextMeshProUGUI controlGuide,
            out TextMeshProUGUI launchAlert)
        {
            gameManager = CreateObject($"{prefix}GameManager").AddComponent<GameManager>();
            InvokePrivate<object>(gameManager, "Awake");
            var deployment = gameManager.GetComponent<DeploymentController>();
            Assert.That(deployment, Is.Not.Null,
                "The active-aim fixture must use GameManager's production deployment bootstrap.");
            InvokePrivate<object>(deployment, "Awake");

            var launchManager = CreateObject($"{prefix}LaunchManager").AddComponent<LaunchManager>();
            trajectoryLine = CreateObject($"{prefix}Trajectory").AddComponent<LineRenderer>();
            rubberBandLine = CreateObject($"{prefix}RubberBand").AddComponent<LineRenderer>();
            var launchPoint = CreateObject($"{prefix}LaunchPoint");
            launchPoint.transform.position = new Vector3(16000f, -16000f, 0f);
            var selectedUnit = CreateObject($"{prefix}KnightPrefab");
            selectedUnit.transform.position = new Vector3(17000f, -17000f, 0f);
            selectedUnit.AddComponent<BoxCollider2D>().size = Vector2.one * 0.1f;
            selectedUnit.AddComponent<UnitController>().unitType = UnitType.Knight;

            controlGuide = CreateObject(
                $"{prefix}ControlGuide",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            launchAlert = CreateObject(
                $"{prefix}LaunchAlert",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();

            launchManager.launchPoint = launchPoint.transform;
            launchManager.trajectoryLine = trajectoryLine;
            launchManager.rubberBandLine = rubberBandLine;
            launchManager.controlGuideText = controlGuide;
            launchManager.trajectoryResolution = 6;
            launchManager.timeStep = 0.02f;
            launchManager.SetSelectedUnit(selectedUnit, DeployCard.Knight);
            SetPrivateField(launchManager, "launchAlertText", launchAlert);
            SetPrivateField(gameManager, "cachedLaunchManager", launchManager);
            SetPrivateField(gameManager, "isPlayerTurn", true);
            gameManager.currentState = GameState.PlayerTurn;

            registeredEventSystem = CreateObject($"{prefix}EventSystem").AddComponent<RegisteredEventSystem>();
            registeredEventSystem.RegisterForTest();
            EventSystem.current = registeredEventSystem;

            Vector2 launchAnchor = launchManager.GetLaunchAnchorPosition();
            launchManager.SetSimulatedPointer(launchAnchor, pressed: true, held: false, released: false);
            InvokePrivate<object>(launchManager, "Update");
            launchManager.SetSimulatedPointer(
                launchAnchor + new Vector2(2f, 1f),
                pressed: false,
                held: true,
                released: false);
            InvokePrivate<object>(launchManager, "Update");

            return launchManager;
        }

        private GameObject CreateObject(string name, params System.Type[] components)
        {
            var gameObject = components.Length == 0
                ? new GameObject(name)
                : new GameObject(name, components);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {target.GetType().Name}.{fieldName}.");
            field.SetValue(target, value);
        }

        private static TResult InvokePrivate<TResult>(object target, string methodName, params object[] arguments)
        {
            var method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Expected private method {target.GetType().Name}.{methodName}.");
            return (TResult)method.Invoke(target, arguments);
        }
        private sealed class RegisteredEventSystem : EventSystem
        {
            private bool registered;

            public void RegisterForTest()
            {
                if (registered) return;
                OnEnable();
                registered = true;
            }

            public void UnregisterForTest()
            {
                if (!registered) return;
                OnDisable();
                registered = false;
            }
        }

    }
}
