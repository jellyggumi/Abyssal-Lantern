using System.Collections.Generic;
using System.Reflection;
using CastleBusters;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace CastleBusters.Tests
{
    [TestFixture]
    public sealed class PreviewParityRegressionTests
    {
        private const float PositionTolerance = 0.001f;
        private readonly List<GameObject> createdObjects = new List<GameObject>();
        private bool previousQueriesHitTriggers;

        [SetUp]
        public void SetUp()
        {
            previousQueriesHitTriggers = Physics2D.queriesHitTriggers;
            Physics2D.queriesHitTriggers = true;
        }

        [TearDown]
        public void TearDown()
        {
            Physics2D.queriesHitTriggers = previousQueriesHitTriggers;

            for (int i = createdObjects.Count - 1; i >= 0; i--)
            {
                if (createdObjects[i] != null) Object.DestroyImmediate(createdObjects[i]);
            }

            createdObjects.Clear();
        }

        [Test]
        public void LaunchManager_DrawTrajectory_ConfiguredIntegrationSamplesRenderFullThreeSecondHorizon()
        {
            var managerObject = CreateObject("HorizonPreviewLaunchManager");
            var trajectoryLine = managerObject.AddComponent<LineRenderer>();
            var launchManager = managerObject.AddComponent<LaunchManager>();
            var launchPoint = CreateObject("HorizonPreviewLaunchPoint");
            var selectedBody = CreateObject("HorizonPreviewSelectedBody");
            // Kept far from SampleScene geometry (~30u extent) so no scene collider can
            // truncate the preview, but pulled in from the original 14000/12000: this test
            // accumulates 150 float32 additions from the launch origin, and at |x|=12000 that
            // drifts 60 ULP (0.0586 world units = 0.0078 s) — it blew the 0.001 s tolerance
            // for reasons unrelated to the integration under test. At 2000 the same
            // accumulation drifts 0.0005 s, so the tight tolerance keeps its meaning.
            selectedBody.transform.position = new Vector3(2400f, -2400f, 0f);
            var selectedCollider = selectedBody.AddComponent<BoxCollider2D>();
            selectedCollider.size = Vector2.one * 0.1f;

            launchPoint.transform.position = new Vector3(2000f, -2000f, 0f);
            launchManager.launchPoint = launchPoint.transform;
            launchManager.trajectoryLine = trajectoryLine;
            launchManager.trajectoryResolution = 150;
            launchManager.timeStep = 0.02f;
            launchManager.SetSelectedUnit(selectedBody);

            Physics2D.SyncTransforms();
            Vector2 initialVelocity = new Vector2(7.5f, 4f);
            InvokeTrajectory(launchManager, initialVelocity);

            Assert.That(trajectoryLine.positionCount, Is.EqualTo(151),
                "The configured 150 integrations must render the t=0 origin plus every integrated sample through the horizon.");

            Vector3 firstSample = trajectoryLine.GetPosition(0);
            Vector3 firstIntegratedSample = trajectoryLine.GetPosition(1);
            Vector3 lastSample = trajectoryLine.GetPosition(trajectoryLine.positionCount - 1);
            float observedFirstStepSeconds = (firstIntegratedSample.x - firstSample.x) / initialVelocity.x;
            float observedHorizonSeconds = (lastSample.x - firstSample.x) / initialVelocity.x;

            Assert.That(Vector2.Distance(firstSample, launchManager.GetLaunchPosition()),
                Is.LessThanOrEqualTo(PositionTolerance),
                "The first rendered point must be the unintegrated launch origin at t=0.");
            Assert.That(observedFirstStepSeconds, Is.EqualTo(0.02f).Within(PositionTolerance),
                "The point after the origin must represent the first 0.02-second integration, not another t=0 sample.");
            Assert.That(observedHorizonSeconds, Is.EqualTo(3f).Within(PositionTolerance),
                "The last rendered point must preserve all 150 integrations and reach the configured 3.0-second horizon.");
        }

        [Test]
        public void LaunchManager_DrawTrajectory_EventGateTriggerMultipliesDownstreamVelocityWithoutEndingPreview()
        {
            var managerObject = CreateObject("TriggerPreviewLaunchManager");
            var trajectoryLine = managerObject.AddComponent<LineRenderer>();
            var launchManager = managerObject.AddComponent<LaunchManager>();
            var launchPoint = CreateObject("TriggerPreviewLaunchPoint");
            var selectedBody = CreateObject("TriggerPreviewSelectedBody");
            selectedBody.transform.position = new Vector3(9000f, -8900f, 0f);
            selectedBody.AddComponent<Rigidbody2D>();
            var selectedCollider = selectedBody.AddComponent<BoxCollider2D>();
            selectedCollider.size = Vector2.one * 0.1f;
            selectedBody.AddComponent<UnitController>();

            launchPoint.transform.position = new Vector3(8000f, -8000f, 0f);
            launchManager.launchPoint = launchPoint.transform;
            launchManager.trajectoryLine = trajectoryLine;
            launchManager.trajectoryResolution = 5;
            launchManager.timeStep = 0.1f;
            launchManager.SetSelectedUnit(selectedBody);

            Vector2 previewStart = launchManager.GetLaunchPosition();
            var gateObject = CreateObject("TriggerPreviewEventGate");
            gateObject.transform.position = previewStart + new Vector2(2f, -0.25f);
            var gateCollider = gateObject.AddComponent<BoxCollider2D>();
            gateCollider.size = new Vector2(3f, 8f);
            var gate = gateObject.AddComponent<EventGateGimmick>();
            InvokeAwake(gate);
            gate.effectType = EventGateEffectType.PowerUp;
            gate.velocityMultiplier = 2f;

            Assert.That(gateCollider.isTrigger, Is.True,
                "The regression fixture must cross the same trigger collider used by a live EventGateGimmick.");
            Physics2D.SyncTransforms();

            Vector2 initialVelocity = new Vector2(10f, 0f);
            InvokeTrajectory(launchManager, initialVelocity);

            Assert.That(trajectoryLine.positionCount, Is.EqualTo(launchManager.trajectoryResolution + 1),
                "An EventGate trigger changes flight but is not an impact, so the preview must retain its origin and every integrated sample.");

            float unchangedStepX = initialVelocity.x * launchManager.timeStep;
            float firstPostGateStepX =
                trajectoryLine.GetPosition(2).x - trajectoryLine.GetPosition(1).x;
            float secondPostGateStepX =
                trajectoryLine.GetPosition(3).x - trajectoryLine.GetPosition(2).x;

            Assert.That(firstPostGateStepX, Is.EqualTo(unchangedStepX * gate.velocityMultiplier).Within(0.05f),
                "The first complete sample after crossing the gate must use its velocity multiplier, not the unchanged trajectory.");
            Assert.That(firstPostGateStepX, Is.GreaterThan(unchangedStepX + 0.5f),
                "The multiplied downstream position must be meaningfully separated from an unchanged preview.");
            Assert.That(secondPostGateStepX, Is.EqualTo(firstPostGateStepX).Within(0.05f),
                "Remaining inside a wide one-shot gate must not multiply preview velocity again on later samples.");
        }

        [Test]
        public void GameManager_BarrelSelection_PreviewOriginMatchesRuntimeLaunchOriginAboveGround()
        {
            var barrelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                "Assets/Prefabs/ExplosiveBarrel.prefab");
            Assert.That(barrelPrefab, Is.Not.Null,
                "The shipped ExplosiveBarrel prefab is required for production launch-origin parity.");

            var launchManagerObject = CreateObject("BarrelOriginLaunchManager");
            var trajectoryLine = launchManagerObject.AddComponent<LineRenderer>();
            var launchManager = launchManagerObject.AddComponent<LaunchManager>();
            var launchPoint = CreateObject("BarrelOriginLaunchPoint");
            var ground = CreateObject("BarrelOriginGround");
            var groundCollider = ground.AddComponent<BoxCollider2D>();
            groundCollider.size = new Vector2(20f, 1f);

            const float groundTopY = -6000f;
            ground.transform.position = new Vector3(6000f, groundTopY - 0.5f, 0f);
            launchPoint.transform.position = new Vector3(6000f, groundTopY, 0f);
            launchManager.launchPoint = launchPoint.transform;
            launchManager.trajectoryLine = trajectoryLine;
            launchManager.trajectoryResolution = 2;
            launchManager.timeStep = 0.02f;

            var gameManager = CreateInitializedGameManager("BarrelOriginGameManager");
            var gameManagerObject = gameManager.gameObject;
            gameManager.explosiveBarrelPrefab = barrelPrefab;
            gameManager.SelectUnit(3);

            Physics2D.SyncTransforms();
            InvokeTrajectory(launchManager, new Vector2(4f, 3f));
            Assert.That(trajectoryLine.positionCount, Is.EqualTo(launchManager.trajectoryResolution + 1),
                "The selected production Barrel must expose its launch origin through the visible preview.");
            Vector2 previewOrigin = trajectoryLine.GetPosition(0);

            var unitsBeforeLaunch = new HashSet<UnitController>(Object.FindObjectsOfType<UnitController>());
            Object.DestroyImmediate(gameManagerObject);
            launchManager.SimulateLaunch(new Vector2(4f, 3f));

            UnitController launchedBarrel = FindNewUnit(unitsBeforeLaunch);
            Assert.That(launchedBarrel, Is.Not.Null,
                "The public simulated-launch path must instantiate the Barrel selected through GameManager.");
            createdObjects.Add(launchedBarrel.gameObject);
            Assert.That(launchedBarrel.unitType, Is.EqualTo(UnitType.Barrel));

            Vector2 runtimeOrigin = launchedBarrel.transform.position;
            Assert.That(Vector2.Distance(previewOrigin, runtimeOrigin), Is.LessThanOrEqualTo(PositionTolerance),
                "The first preview sample and the spawned Barrel origin must use the same resolved production launch position.");
            Bounds predictedBounds = UnitController.EstimateLaunchedWorldColliderBounds(barrelPrefab);
            float predictedBottomY =
                previewOrigin.y + predictedBounds.center.y - predictedBounds.extents.y;
            Assert.That(predictedBottomY,
                Is.GreaterThanOrEqualTo(groundCollider.bounds.max.y - PositionTolerance),
                "The production Barrel bounds resolved from the preview origin must clear the launch ground.");
        }

        [Test]
        public void GameManager_CannonSelection_ArmsDeployModeAndShowsPlacementGuide()
        {
            var launchManagerObject = CreateObject("CannonGuideLaunchManager");
            var launchManager = launchManagerObject.AddComponent<LaunchManager>();
            var guideObject = CreateObject(
                "CannonGuideText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI));
            launchManager.controlGuideText = guideObject.GetComponent<TextMeshProUGUI>();

            var gameManager = CreateInitializedGameManager("CannonGuideGameManager");
            gameManager.SelectUnit(2);

            var deployment = DeploymentController.Instance;
            Assert.That(deployment, Is.Not.Null,
                "GameManager's production bootstrap must provide the deployment controller used by Cannon selection.");
            Assert.That(deployment.SelectedCard, Is.EqualTo(DeployCard.Cannon),
                "Selecting roster slot 3 must route the real selection flow to the Cannon deploy card.");
            Assert.That(deployment.DeployModeArmed, Is.True,
                "Selecting the deploy-only Cannon must arm placement rather than launch aiming.");

            string guide = launchManager.controlGuideText.text;
            Assert.That(guide.Contains("배치") || guide.Contains("설치"), Is.True,
                "The visible Cannon guide must tell the player to deploy/place the installation.");
            Assert.That(guide, Does.Not.Contain("드래그"),
                "A deploy-only Cannon guide must not instruct the player to drag the launch ring.");
            Assert.That(guide, Does.Not.Contain("발사"),
                "A deploy-only Cannon guide must not instruct the player to launch the Cannon.");
        }

        private GameObject CreateObject(string name, params System.Type[] components)
        {
            var gameObject = components.Length == 0
                ? new GameObject(name)
                : new GameObject(name, components);
            createdObjects.Add(gameObject);
            return gameObject;
        }

        private GameManager CreateInitializedGameManager(string name)
        {
            var gameManager = CreateObject(name).AddComponent<GameManager>();
            InvokeAwake(gameManager);

            var deployment = gameManager.GetComponent<DeploymentController>();
            Assert.That(deployment, Is.Not.Null,
                "The EditMode fixture must materialize GameManager's production deployment bootstrap.");
            InvokeAwake(deployment);
            return gameManager;
        }

        private static void InvokeAwake(MonoBehaviour component)
        {
            MethodInfo awake = component.GetType().GetMethod(
                "Awake",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(awake, Is.Not.Null,
                $"The EditMode fixture must execute {component.GetType().Name}.Awake through the production lifecycle path.");
            awake.Invoke(component, null);
        }

        private static void InvokeTrajectory(LaunchManager launchManager, Vector2 velocity)
        {
            MethodInfo drawTrajectory = typeof(LaunchManager).GetMethod(
                "DrawTrajectory",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.That(drawTrajectory, Is.Not.Null,
                "The fixture must execute the same trajectory renderer used by live aiming.");
            drawTrajectory.Invoke(launchManager, new object[] { velocity });
        }

        private static UnitController FindNewUnit(HashSet<UnitController> unitsBeforeLaunch)
        {
            foreach (UnitController candidate in Object.FindObjectsOfType<UnitController>())
            {
                if (!unitsBeforeLaunch.Contains(candidate)) return candidate;
            }

            return null;
        }
    }
}
