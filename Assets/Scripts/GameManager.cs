using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
namespace CastleBusters
{
    public enum GameState { Setup, Intro, PlayerTurn, AITurn, GameOver }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }
        // Briefly hold the impact result on screen, then hand control back. The former
        // unconditional 2-second pause made resolved shots feel like input lag.
        public const float PostImpactHoldSeconds = 0.35f;


        public GameState currentState = GameState.Setup;
        public float turnDuration = 15f;

        // ---- Stage selection ----
        // Set by the intro screen's stage picker before BeginSiege(); survives the
        // ReloadArena() scene reload (static) so REMATCH replays the same stage and TITLE
        // resets to Stage1. Read once into currentStage/ActiveLayout in ApplyStageLayout()
        // (called from Start(), never Awake()) so EditMode reflection tests — which only
        // ever invoke Awake()/CreateGround() and never Start() — always see Stage1.
        public static StageId PendingStage = StageId.Stage1;
        public StageId currentStage = StageId.Stage1;
        public StageLayout ActiveLayout { get; private set; } = StageDefinitions.Stage1;

        /// <summary>
        /// Visual-only scale that keeps a unit the same size *on screen* across stages.
        /// The camera frames a wider board on Stage3 (47u vs Stage1's 39u), so at a fixed
        /// world size every soldier rendered ~17% smaller there and the wide board was the
        /// hardest to read — the opposite of what its extra range deserves.
        ///
        /// Simulation is untouched: UnitController divides the collider by the same factor
        /// it multiplies the transform by, so world collider extents are invariant. Only
        /// playable bodies scale — board geometry (blocks, castles) must not, because their
        /// world positions *are* the gameplay.
        /// </summary>
        public static float StageActorVisualScale
        {
            get
            {
                var gm = Instance;
                float baseline = StageDefinitions.Stage1.cameraDesiredWorldWidth;
                if (gm == null || baseline <= 0.01f) return 1f;
                return gm.ActiveLayout.cameraDesiredWorldWidth / baseline;
            }
        }
        private float turnTimer;

        [Header("Difficulty Curve")]
        // Session difficulty ramp: turn 0 -> gentle breeze + sloppy AI, turn ~difficultyRampTurns ->
        // gale-force wind ceiling + tight AI groupings. SmoothStep keeps the mid-game the steepest
        // part of the curve (slow onboarding, accelerating pressure, plateaued endgame).
        public int difficultyRampTurns = 15;
        public float windCapStart = 2.0f;
        public float windCapEnd = 6.5f;
        public float aiErrorStart = 2.5f;
        public float aiErrorEnd = 0.8f;
        public float stormChanceStart = 0.02f;
        public float stormChanceEnd = 0.15f;
        private int turnCount;
        private WebtoonPrologueController webtoonPrologue;
        private IntroScreenController introScreen;
        // Survives ReloadArena() (static) so Title/Rematch/RequestStage reloads never replay the
        // pre-title webtoon cold-open after the player has already seen it this app session.
        private static bool webtoonIntroShown;
        // Set by RequestStage when the campaign MOVES to a different stage, consumed by
        // ShowIntro on the next boot. Static so it survives the scene reload the advance
        // triggers — the flag has to outlive the GameManager that set it.
        private static bool pendingStageInterlude;



        [Header("Castles")]
        public CastleController playerCastle;
        public CastleController enemyCastle;

        [Header("Prefabs")]
        public GameObject knightPrefab;
        public GameObject archerPrefab;
        public GameObject explosiveBarrelPrefab;

        [Header("UI References")]
        public GameObject gameOverPanel;
        public TMPro.TextMeshProUGUI turnText;
        public TMPro.TextMeshProUGUI timerText;
        public TMPro.TextMeshProUGUI resultText;
        public TMPro.TextMeshProUGUI windText;
        public TMPro.TextMeshProUGUI scoreText;
        public UnityEngine.UI.Button knightButton;
        public UnityEngine.UI.Button archerButton;
        // Slot 3 became the Cannon card when the Bomber was removed
        // (design/deployment-economy.md §2). FormerlySerializedAs keeps the existing
        // SampleScene wiring attached instead of silently nulling the button reference.
        [UnityEngine.Serialization.FormerlySerializedAs("bomberButton")]
        public UnityEngine.UI.Button cannonButton;
        public UnityEngine.UI.Button gimmickButton;
        [System.NonSerialized] public UnityEngine.UI.Button lastStandButton;

        [Header("Background & Gimmicks")]

        // Legacy serialized/test compatibility only; active-stage Resources art is authoritative.
        public Sprite backgroundSprite;
        private static readonly Dictionary<StageId, Sprite> stageBackgroundSprites = new Dictionary<StageId, Sprite>();
        public float currentWindForce = 0f;
        public Vector2 windEffectOrigin = Vector2.zero;
        public float windEffectRadius = 10f;  // Radius within which wind effect applies
        public TMPro.TextMeshProUGUI gimmickStatusText;

        private int playerScore = 0;
        private int enemyScore = 0;
        private CastleCoreGimmick playerCore;
        private CastleCoreGimmick enemyCore;
        private readonly List<UnitController> activeUnits = new List<UnitController>();
        private bool isPlayerTurn;
        private GameObject selectedUnitPrefab;
        // Ground tile grid extents shared between CreateGround and GenerateGroundTexture.
        // Instance (not const): StageLayout.groundHalfWidth widens this for Stage3's larger
        // launch-apron gap. Set once in ApplyStageLayout() before CreateGround() runs.
        private int groundHalfWidth = 20;
        private int groundColumnCount => groundHalfWidth * 2 + 1;
        // |x| beyond which ground tiles become anchors (foundation + bottom rows). Stage3's
        // wider ground band pushes this out proportionally; see ApplyStageLayout().
        private float groundAnchorAbsX = 10f;

        [Header("Turn Handling")]
        // One short extension when the timer expires mid-aim, so a drawn shot is never yanked away.
        public const float AimGraceSeconds = 4f;
        public const float UrgencyThresholdSeconds = 5f;
        public const float IdleNudgeIntervalSeconds = 5f;
        private bool isResolvingTurn;
        private bool graceUsedThisTurn;
        private bool urgencyNotified;
        private float idleNudgeTimer;
        private LaunchManager cachedLaunchManager;
        private LaunchManager LaunchManagerRef
        {
            get
            {
                if (cachedLaunchManager == null) cachedLaunchManager = FindObjectOfType<LaunchManager>();
                return cachedLaunchManager;
            }
        }

        [Header("Comeback / LAST STAND (일발역전)")]
        // One-shot desperation buff, armed when the owner's core drops into the danger band.
        // Player arms manually (R); AI's weaker mirror arms itself. Pure rules in LastStand.
        public LastStand.Phase playerLastStand = LastStand.Phase.Locked;
        public LastStand.Phase aiLastStand = LastStand.Phase.Locked;
        private bool playerDangerNotified;
        private bool aiDangerNotified;

        public bool PlayerCoreInDanger => playerCore != null && LastStand.IsDanger(playerCore.currentHP, playerCore.maxHP);
        public bool EnemyCoreInDanger => enemyCore != null && LastStand.IsDanger(enemyCore.currentHP, enemyCore.maxHP);


        public bool IsPlayerTurn => isPlayerTurn;
        /// <summary>True while a volley resolves — drives the flow-state HUD strip.</summary>
        public bool IsResolvingTurn => isResolvingTurn;
        public float TurnTimeRemaining => turnTimer;
        public int TurnCount => turnCount;

        // Strictly rising, non-linear, and never flat: see DifficultyCurve. The old
        // smoothstep hit 1.0 at difficultyRampTurns and stopped, which left long matches
        // with an unchanging back half.
        public float DifficultyT => DifficultyCurve.Evaluate(turnCount, difficultyRampTurns);
        public float CurrentWindCap => Mathf.Lerp(windCapStart, windCapEnd, DifficultyT);
        public float CurrentAiErrorOffset => Mathf.Lerp(aiErrorStart, aiErrorEnd, DifficultyT);
        public float CurrentStormChance => Mathf.Lerp(stormChanceStart, stormChanceEnd, DifficultyT);

        private void Awake()
        {
            // Unity fake-null safe: plain `??` kept destroyed instances in the slot after
            // scene reloads (review P2 #5), silently no-oping every Instance dependency.
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            KoreanFontSupport.EnsureFallback();

            if (GetComponent<SpriteAtlasPacker>() == null)
            {
                gameObject.AddComponent<SpriteAtlasPacker>();
            }

            if (GetComponent<WindVfxManager>() == null)
            {
                gameObject.AddComponent<WindVfxManager>();
            }

            if (GetComponent<HitStopManager>() == null)
            {
                gameObject.AddComponent<HitStopManager>();
            }

            if (GetComponent<ScreenShakeManager>() == null)
            {
                gameObject.AddComponent<ScreenShakeManager>();
            }

            if (GetComponent<DebrisPool>() == null)
            {
                gameObject.AddComponent<DebrisPool>();
            }
            if (DeploymentController.Instance == null && FindObjectOfType<DeploymentController>() == null)
            {
                gameObject.AddComponent<DeploymentController>();
            }
        }

        private void OnEnable()
        {
            // Domain reloads mid-play wipe statics without re-running Awake; re-register so a
            // recompile during a session never leaves a live scene without GameManager.Instance
            // (symptom: frozen "dead" board where no button or turn logic responds).
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            // Test-isolation guard: a PlayMode session mutates these statics (LaunchApronAbsX,
            // LaunchRingRules ring positions) via ApplyStageLayout() at Start() for whatever
            // stage was active. Unity does NOT reload the domain on exiting Play Mode, so
            // without this reset a Stage3 PlayMode session leaves EditMode's subsequent
            // AosOverhaulTests/GamePlayTests reading Stage3 ring positions against
            // Stage1-hardcoded expected values (confirmed live: caused
            // LaunchRing_RejectsMuzzlePositions_AllowsMidfield to fail post-PlayMode).
            // PendingStage/currentStage are deliberately NOT reset here — a mid-play scene
            // reload (Rematch/RequestStage) also fires OnDestroy, and resetting the stage
            // choice there would break "Rematch replays the same stage".
            LaunchApronAbsX = StageDefinitions.Stage1.launchApronAbsX;
            LaunchRingRules.PlayerRingX = -StageDefinitions.Stage1.launchApronAbsX;
            LaunchRingRules.EnemyRingX = StageDefinitions.Stage1.launchApronAbsX;
        }

        private void Start()
        {
            ApplyStageLayout();
            ConfigureCamera();
            EnsurePresentationDirector();
            EnsureGameplayUxDirector();
            EnsureBrickPlacement();
            EnsureAlarmSystem();
            CreateBackground();
            CreateGround();
            EnsureFieldDirector();
            SetupGimmicks();
            SetupUIButtons();
            ApplyRuntimeSpriteAtlas();
            HeroGrowth.Reset();
            SpawnInitialUnits();
            ShowIntro();
        }

        /// <summary>
        /// Resolves PendingStage (set by the intro screen's stage picker, defaults to Stage1)
        /// into concrete layout numbers. Runs once at the START of Start() — before camera,
        /// ground, or gimmick setup all read these values. Deliberately NOT in Awake(): the
        /// EditMode reflection tests (GamePlayTests/AosOverhaulTests) construct a bare
        /// GameManager and invoke Awake()/CreateGround() directly without ever calling
        /// Start(), so they always see the Stage1 field-initializer defaults regardless of
        /// what a prior PlayMode session left PendingStage set to.
        /// </summary>
        private void ApplyStageLayout()
        {
            currentStage = PendingStage;
            var layout = StageDefinitions.For(currentStage);
            ActiveLayout = layout;

            LaunchApronAbsX = layout.launchApronAbsX;
            groundHalfWidth = Mathf.RoundToInt(layout.groundHalfWidth);
            groundAnchorAbsX = layout.groundAnchorAbsX;
            windCapEnd = layout.windCapEnd;

            LaunchRingRules.PlayerRingX = -layout.launchApronAbsX;
            LaunchRingRules.EnemyRingX = layout.launchApronAbsX;
        }


        // Intro gate: the finished board acts as a frozen diorama behind the title card until the
        // player actually starts the siege. This also stops the AI from volleying at an unmanned
        // castle while nobody is playing (the old "game plays itself to GameOver" QA failure).
        private void ShowIntro()
        {
            // Rematch flow (AC11): a reload requested via REMATCH boots straight into the
            // next siege — the intro is for arrivals, not for the "one more run" loop.
            if (skipIntroOnce)
            {
                skipIntroOnce = false;

                // ...but a CAMPAIGN ADVANCE also arrives here (RequestStage passes
                // skipIntro: true so the player is not made to sit through the title card
                // between stages). That is exactly where the connective narration belongs:
                // the board is built and frozen behind the cutscene, and StartGame runs the
                // moment it ends. Without this the campaign cut from a results screen
                // straight into a new battlefield, so three distinct stages read as "the
                // same fight again with different rocks".
                if (pendingStageInterlude)
                {
                    pendingStageInterlude = false;
                    currentState = GameState.Intro;
                    isPlayerTurn = false;
                    HitStopManager.Instance?.CancelPendingHitStop();
                    Time.timeScale = 0f;
                    StageInterludeController.Play(
                        StageInterlude.ForStageEntry(currentStage),
                        () => { Time.timeScale = 1f; StartGame(); });
                    return;
                }

                StartGame();
                return;
            }
            currentState = GameState.Intro;
            isPlayerTurn = false;

            // A hit-stop restore pending from the previous scene/turn must not thaw the
            // intro freeze 0.05s from now (rematch/title "game plays itself" bug).
            HitStopManager.Instance?.CancelPendingHitStop();
            Time.timeScale = 0f;

            // Cold open, once per app session: a short narration beat before the title card
            // that says where we are and what is at stake. The 11-page webtoon is the LONG
            // form and stays behind the title's 프롤로그 button — putting it in front of a
            // first-time player was the "long read before the game" problem. This is the
            // short form: three lines, skippable, and never replayed on a scene reload
            // (webtoonIntroShown is static, so Title/Rematch/RequestStage all pass it by).
            if (!webtoonIntroShown)
            {
                webtoonIntroShown = true;
                StageInterludeController.Play(
                    StageInterlude.ForStageEntry(StageId.Stage1),
                    ShowTitleScreen);
                return;
            }

            ShowTitleScreen();
        }

        private void ShowWebtoonPrologue()
        {
            if (webtoonPrologue != null) { webtoonPrologue.Dismiss(); webtoonPrologue = null; }
            if (introScreen != null) { introScreen.Dismiss(); introScreen = null; }
            webtoonPrologue = WebtoonPrologueController.Create(ShowTitleScreen);
        }

        private void ShowTitleScreen()
        {
            if (webtoonPrologue != null) { webtoonPrologue.Dismiss(); webtoonPrologue = null; }
            if (introScreen != null) { introScreen.Dismiss(); introScreen = null; }
            introScreen = IntroScreenController.Create(BeginSiege, MobileStorefront.OpenStore, ShowWebtoonPrologue);
        }

        public void BeginSiege()
        {
            if (currentState != GameState.Intro) return;
            Time.timeScale = 1f;
            // A cold-open cutscene may still be on screen when something starts the siege
            // directly (the title's START, or an automated playtest calling BeginSiege).
            // Dismiss rather than Complete: its callback is ShowTitleScreen, and running that
            // AFTER StartGame would drop a title card over a live match — which froze the
            // 30-game PlayMode sim mid-run.
            if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();
            if (webtoonPrologue != null) { webtoonPrologue.Dismiss(); webtoonPrologue = null; }
            if (introScreen != null) { introScreen.Dismiss(); introScreen = null; }
            StartGame();
        }



        private void EnsurePresentationDirector()
        {
            if (FindObjectOfType<GamePresentationDirector>() != null) return;
            var directorGo = new GameObject("GamePresentationDirector");
            var director = directorGo.AddComponent<GamePresentationDirector>();
            director.boardCenter = new Vector2(0f, 3.0f);
            // Stage-aware: Stage1/Stage2 keep the original 39/8.4/11.2 framing byte-identical;
            // Stage3's wider launch-apron gap needs a wider/taller board (ActiveLayout).
            director.desiredWorldWidth = ActiveLayout.cameraDesiredWorldWidth;
            director.targetHalfHeight = 8.4f;
            director.maxHalfHeight = ActiveLayout.cameraMaxHalfHeight;
        }

        private void EnsureGameplayUxDirector()
        {
            if (FindObjectOfType<GameplayUxDirector>() != null) return;
            var uxGo = new GameObject("GameplayUxDirector");
            uxGo.AddComponent<GameplayUxDirector>();
        }

        private void EnsureBrickPlacement()
        {
            if (FindObjectOfType<BrickPlacementController>() != null) return;
            var go = new GameObject("BrickPlacementController");
            go.AddComponent<BrickPlacementController>();
        }

        private void EnsureAlarmSystem()
        {
            if (FindObjectOfType<SiegeAlarmSystem>() != null) return;
            var alarmGo = new GameObject("SiegeAlarmSystem");
            alarmGo.AddComponent<SiegeAlarmSystem>();
        }

        private void ConfigureCamera()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                cam.orthographic = true;
                cam.orthographicSize = 11.2f;
                cam.transform.position = new Vector3(0f, 3f, -10f); // Readjusted from 2f to 3f (1.4x)
            }
        }

        private void CreateBackground()
        {
            var sprite = GetStageBackgroundSprite(currentStage);
            if (sprite == null) return;


            var bg = new GameObject("Background");
            bg.transform.position = new Vector3(0f, 6.5f, 10f);
            var sr = bg.AddComponent<SpriteRenderer>();
            sr.sortingLayerName = "Default";
            sr.sortingOrder = -10;
            sr.sprite = sprite;
            sr.color = ActiveLayout.backgroundTint;

            // Weather rides with the backdrop so each stage reads as its own place: rain on
            // the plain, snow on the dunes, ash in the gorge. Presentation only, and it
            // renders under the units so atmosphere never hides what the player is aiming at.
            if (Application.isPlaying)
            {
                StageWeather.Ensure().Apply(currentStage);
            }
        }

        private static Sprite GetStageBackgroundSprite(StageId stage)
        {
            if (stageBackgroundSprites.TryGetValue(stage, out var cachedSprite) && cachedSprite != null)
            {
                return cachedSprite;
            }

            string resourcePath = stage == StageId.Stage2 ? "Backgrounds/Background_Stage2" :
                stage == StageId.Stage3 ? "Backgrounds/Background_Stage3" :
                "Backgrounds/Background_Stage1";
            var texture = Resources.Load<Texture2D>(resourcePath);
            if (texture == null || texture.width <= 0 || texture.height <= 0) return null;

            var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
            stageBackgroundSprites[stage] = sprite;
            return sprite;
        }

        // Widened strategic layout QA pass: the two keeps moved from ±7 to ±CoreAbsX so the
        // no-man's-land between the castles is a real midfield, and every gimmick family got
        // breathing room (nothing closer than ~1.5u to its neighbour). Envelope spans
        // x in [-15, 15]; launch aprons sit at ±LaunchApronAbsX. Ground kegs keep >= 3.0u
        // clearance from the muzzles (blast radius 2.2 — kegs hugging the apron self-detonated
        // low-arc shots, review cycle 3 P1 #1) and 2.5u from the cores so a keg pop can never
        // splash a healthy core. Static tables: layout test pins the spread.
        public const float CoreAbsX = 9f;            // was 7 — wider castle gap for strategy; SHARED across stages, never changes
        // Mutable (not const): StageLayout.launchApronAbsX overrides this once at StartGame()
        // for Stage3's wider player-to-player gap. Stage1/Stage2's 14.5 default keeps every
        // existing caller/test byte-identical when either is active (the only stages EditMode
        // tests ever see, since they call Awake()/CreateGround() directly and never Start()).
        public static float LaunchApronAbsX = 14.5f;  // was 12 — launch offset from core is 5.5
        public static readonly Vector3[] InitialBarrelPositions =
        {
            new Vector3(-11f, 0.5f, 0f),
            new Vector3(-6.5f, 0.5f, 0f),
            new Vector3(6.5f, 0.5f, 0f),
            new Vector3(11f, 0.5f, 0f),
        };

        public static readonly Vector3[] InitialRunePositions =
        {
            new Vector3(-11.5f, 3.5f, 0f), // rally rune (buff), player approach
            new Vector3(11.5f, 3.5f, 0f),  // hex rune (debuff), enemy approach
        };

        public static readonly Vector3[] InitialGatePositions =
        {
            new Vector3(-15f, 5.4f, 0f),   // Multiply — deep player wing (aerial, arc-reachable)
            new Vector3(0f, 6.1f, 0f),     // PowerUp — apex over the bridge
            new Vector3(15f, 4.9f, 0f),    // Reduce — deep enemy wing (aerial, arc-reachable)
        };

        private void EnsureFieldDirector()
        {
            var existing = FindObjectOfType<GimmickFieldDirector>();
            var director = existing != null ? existing : new GameObject("GimmickFieldDirector").AddComponent<GimmickFieldDirector>();
            director.stage = currentStage;
            // Stage-aware composition: Stage2's "relentless bastion" cadence caps obstacle
            // count low; Stage3's wider gorge carries more pieces. Mutate cadence itself
            // lives in DynamicBattlefield.PlanForTurn (reads StageDefinitions directly), so
            // it isn't duplicated onto this MonoBehaviour.
            director.maxFieldObstacles = ActiveLayout.maxFieldObstacles;
        }

        private void SetupGimmicks()
        {
            // y=0.5 sits a 1x1 dynamic body flush on the y=0 ground surface (top ground row is
            // centered at y=-0.5). Spawning higher made barrels/cores visibly drop on the first
            // physics tick and left cores outside BFS adjacency range of the ground.
            // Per-stage composition, not a shared fixture: Stage1 keeps the original
            // bridge-hugging kegs, Stage2 ships with none (earned mid-match instead), Stage3
            // spreads them into the wings. See StageDefinitions for the concept rationale.
            foreach (var pos in ActiveLayout.barrelPositions) SpawnExplosiveBarrel(pos);

            SpawnCastleCores();
            SpawnCaptureZones();
            SpawnCastleWalls();
            AlignLaunchPoints();
            SpawnMovingGimmick();
            // AOS overhaul (§3, §6): eruption vents, runes, and event gates are no longer
            // fixtures — GimmickFieldDirector materializes them on turn beats, placed and
            // typed by the balance situation, and expires them after their lifetime.
            SetupGimmickUI();
            UpdateWind();
        }

        // ---- AOS objective (§1): capture zones over both cores ----

        private void SpawnCaptureZones()
        {
            if (playerCore != null) CaptureZoneController.Create(playerCore.transform.position, true);
            if (enemyCore != null) CaptureZoneController.Create(enemyCore.transform.position, false);
        }

        /// <summary>Capture win/loss entry (CaptureZoneController). Zone owner lost it.</summary>
        public void OnZoneCaptured(bool zoneOwnedByPlayer)
        {
            if (currentState == GameState.GameOver) return;
            EndGame(zoneOwnedByPlayer ? "KEEP SEIZED — DEFEAT!" : "CASTLE SEIZED — VICTORY!");
        }

        // ---- Castle walls (§5): runtime defensive walls, never inside a launch ring ----

        public static readonly Vector3[] WallBasePositions =
        {
            new Vector3(-7.5f, 0.5f, 0f),  // shields the player keep's approach
            new Vector3(7.5f, 0.5f, 0f),   // shields the enemy keep's approach
        };

        private void SpawnCastleWalls()
        {
            foreach (var basePos in WallBasePositions)
            {
                SpawnCastleWall(basePos, basePos.x < 0f);
            }
        }

        /// <summary>
        /// Builds a stone wall column at match-start, height driven by the active stage's
        /// composition (Stage1/3 = 2 blocks, Stage2 "Ashen Bastion" = 3 for a heavier,
        /// more fortified silhouette). Positions inside either launch affordance ring are
        /// refused — a wall in the muzzle blocks that side's every volley.
        /// </summary>
        public GameObject SpawnCastleWall(Vector3 basePosition, bool isPlayerSide)
        {
            if (LaunchRingRules.IsInsideRing(basePosition))
            {
                Debug.LogWarning($"[GameManager] Castle wall at {basePosition} rejected: inside a launch ring.");
                return null;
            }

            var blockPrefab = Resources.Load<GameObject>("DestructibleBlock");
            if (blockPrefab == null) return null;
            var stone = Resources.Load<BlockData>("StoneBlockData");

            var root = new GameObject(isPlayerSide ? "PlayerWall" : "EnemyWall");
            root.transform.position = basePosition;
            var parentCastle = isPlayerSide ? playerCastle : enemyCastle;
            if (parentCastle != null) root.transform.SetParent(parentCastle.transform);

            int height = Mathf.Max(1, ActiveLayout.wallHeightBlocks);
            for (int i = 0; i < height; i++)
            {
                var block = Instantiate(blockPrefab, basePosition + new Vector3(0f, i, 0f),
                    Quaternion.identity, root.transform);
                block.name = $"WallBlock_{i}";
                var db = block.GetComponent<DestructibleBlock>();
                if (db != null)
                {
                    if (stone != null) db.ApplyBlockData(stone);
                    db.isGroundAnchor = false;
                }
            }
            parentCastle?.RefreshBlockList();
            return root;
        }

        /// <summary>Director entry (§6): balance-event gates reuse the tuned gate recipe.</summary>
        public GameObject SpawnBalanceGate(string gateName, Vector3 position, EventGateEffectType effectType)
        {
            Sprite origSprite = null;
#if UNITY_EDITOR
            origSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/block_normal.png");
#endif
            return CreateEventGate(gateName, position, effectType, origSprite);
        }

        private GameObject CreateEventGate(string gateName, Vector3 position, EventGateEffectType effectType, Sprite sprite)
        {
            var gateGo = new GameObject(gateName);
            gateGo.transform.position = position;
            var sr = gateGo.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteAtlasPacker.Instance != null ? SpriteAtlasPacker.Instance.GetPackedSprite(sprite) : sprite;
            var gate = gateGo.AddComponent<EventGateGimmick>();
            gate.effectType = effectType;
            gate.targetWorldSize = effectType == EventGateEffectType.Multiply ? 2.25f : 2.05f;
            gate.cloneCount = 1;
            gate.maxTotalClones = 2;
            gate.velocityMultiplier = effectType == EventGateEffectType.PowerUp ? 1.35f : 1.15f;
            gate.damageSpeedMultiplier = effectType == EventGateEffectType.PowerUp ? 1.35f : 1.15f;
            gate.reduceVelocityMultiplier = 0.55f;
            gate.reduceDamageSpeedMultiplier = 0.65f;
            return gateGo;
        }

        private void SpawnCastleCores()
        {
            // Destroy any pre-existing block or barrel at the core positions to prevent overlap
            for (int i = DestructibleBlock.Active.Count - 1; i >= 0; i--)
            {
                var block = DestructibleBlock.Active[i];
                if (block != null)
                {
                    float x = block.transform.position.x;
                    float y = block.transform.position.y;
                    if (Mathf.Abs(y - 0.5f) < 0.6f && (Mathf.Abs(x - (-CoreAbsX)) < 0.1f || Mathf.Abs(x - CoreAbsX) < 0.1f))
                    {
                        Destroy(block.gameObject);
                    }
                }
            }
            var allGo = FindObjectsOfType<GameObject>();
            foreach (var go in allGo)
            {
                if (go != null && go.name == "ExplosiveBarrel")
                {
                    float x = go.transform.position.x;
                    float y = go.transform.position.y;
                    if (Mathf.Abs(y - 1.5f) < 0.1f && (Mathf.Abs(x - (-CoreAbsX)) < 0.1f || Mathf.Abs(x - CoreAbsX) < 0.1f))
                    {
                        Destroy(go);
                    }
                }
            }

            BlockData defaultData = null;
            if (playerCastle != null)
            {
                var firstBlock = playerCastle.GetComponentInChildren<DestructibleBlock>();
                if (firstBlock != null) defaultData = firstBlock.blockData;
            }

            var pCoreGo = new GameObject("PlayerCastleCore");
            pCoreGo.transform.position = new Vector3(-CoreAbsX, 0.5f, 0f);
            if (playerCastle != null) pCoreGo.transform.SetParent(playerCastle.transform);
            // SpriteRenderer/BoxCollider2D added bare: CastleCoreGimmick.Awake() (via
            // DestructibleBlock.Awake) and Start()->ApplyCoreVisuals() immediately assign
            // the real core art and recompute the collider from it, so any sprite set here
            // would just be discarded before the first frame renders — don't bother loading
            // or assigning one.
            pCoreGo.AddComponent<SpriteRenderer>();
            pCoreGo.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            var pRb = pCoreGo.AddComponent<Rigidbody2D>();
            pRb.bodyType = RigidbodyType2D.Dynamic;
            pRb.mass = 5.0f;
            playerCore = pCoreGo.AddComponent<CastleCoreGimmick>();
            playerCore.isPlayerCore = true;
            playerCore.blockData = defaultData;

            var eCoreGo = new GameObject("EnemyCastleCore");
            eCoreGo.transform.position = new Vector3(CoreAbsX, 0.5f, 0f);
            if (enemyCastle != null) eCoreGo.transform.SetParent(enemyCastle.transform);
            eCoreGo.AddComponent<SpriteRenderer>();
            eCoreGo.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            var eRb = eCoreGo.AddComponent<Rigidbody2D>();
            eRb.bodyType = RigidbodyType2D.Dynamic;
            eRb.mass = 5.0f;
            enemyCore = eCoreGo.AddComponent<CastleCoreGimmick>();
            enemyCore.isPlayerCore = false;
            enemyCore.blockData = defaultData;

            // Ensure each castle's structural-integrity list includes its core gimmick immediately,
            // so a direct hit on the core (or a nearby wall/ground tile break) always evaluates
            // collapse/support correctly instead of depending on Unity's Start() call order.
            if (playerCastle != null) playerCastle.RefreshBlockList();
            if (enemyCastle != null) enemyCastle.RefreshBlockList();

        }

        private void AlignLaunchPoints()
        {
            var lm = FindObjectOfType<LaunchManager>();
            if (lm != null && lm.launchPoint != null && playerCore != null)
            {
                lm.launchPoint.position = playerCore.transform.position + new Vector3(-(LaunchApronAbsX - CoreAbsX), 0f, 0f);
            }

            var ai = FindObjectOfType<SimpleAI>();
            if (ai != null && ai.launchPoint != null && enemyCore != null)
            {
                ai.launchPoint.position = enemyCore.transform.position + new Vector3(LaunchApronAbsX - CoreAbsX, 0f, 0f);
            }
        }

        private void SpawnMovingGimmick()
        {
            // The airborne WAR BEAST (§4 rework): a destructible flying gimmick with
            // 3 HP-driven flight patterns (glide/figure-8/swoop). It rams walls on dive
            // passes, gets flung by blasts/vent columns, and redeploys 5 s after death.
            var go = new GameObject("MovingObstacle");
            go.transform.position = new Vector3(0f, FlightRules.BaseAltitude, 0f);
            // SpriteRenderer added bare: MovingGimmick.Awake() immediately assigns its own
            // art (GimmickSpriteLibrary Ram tint, then the flying_beast_anim frame loop) and
            // recomputes scale/collider from it, so any sprite set here is discarded before
            // the first frame renders.
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            var moving = go.AddComponent<MovingGimmick>();
            moving.targetWorldSize = 3.1f;
            moving.chariotMode = true;
        }

        /// <summary>Chariot redeploy (§4): called from a live-match chariot destruction.</summary>
        public void ScheduleChariotRespawn()
        {
            if (!isActiveAndEnabled ||
                (currentState != GameState.PlayerTurn && currentState != GameState.AITurn))
            {
                return;
            }

            StartCoroutine(ChariotRespawnRoutine());
        }

        private IEnumerator ChariotRespawnRoutine()
        {
            yield return new WaitForSeconds(ChariotRules.RespawnDelaySeconds);
            if (currentState != GameState.PlayerTurn && currentState != GameState.AITurn) yield break;
            foreach (var m in FindObjectsOfType<MovingGimmick>())
            {
                if (m != null && m.chariotMode) yield break; // one chariot at a time
            }
            SpawnMovingGimmick();
            GameFeelVfx.SpawnFeedbackLabel(new Vector3(0f, 2.4f, 0f),
                "SIEGE ENGINE REDEPLOYED", new Color(0.85f, 0.92f, 1f, 1f), 2.4f, 0.7f);
        }

        // Vertical-hazard vents flank the chariot's sweep (±3.2 reach) and stay a lane
        // inside the ±6.5 kegs, so each hazard family owns its own column of the midfield.
        public static readonly Vector3[] VentPositions =
        {
            new Vector3(-5.4f, 0.15f, 0f),  // magma geyser, player approach
            new Vector3(5.4f, 0.15f, 0f),   // petal burst, enemy approach
        };

        // (§3) Vents are event pieces now — see GimmickFieldDirector.NotifyTurnAdvanced.

        // (§6) Runes are event pieces now — see GimmickFieldDirector balance events.

        private void SetupGimmickUI()
        {
            // UX text-diet pass: the FIELD INTEL panel was removed — a seven-line legend of
            // mostly static flavor text competing with the battlefield. Core HP lives on the
            // KEEP/BREACH badges and wind on the wind text; nothing here was load-bearing.
        }

        private GameObject SpawnExplosiveBarrel(Vector3 position)
        {
            if (explosiveBarrelPrefab != null) return Instantiate(explosiveBarrelPrefab, position, Quaternion.identity);

            var barrel = new GameObject("ExplosiveBarrel") { transform = { position = position } };
            // SpriteRenderer added bare: ExplosiveGimmick.Awake() immediately assigns the
            // real powder-keg art (GimmickSpriteLibrary Barrel) and recomputes scale/collider
            // from it, so any sprite/color set here is discarded before the first frame
            // renders. DestructibleBlock.Awake() runs first but no-ops (blockData is null
            // until ApplyBlockData is called explicitly), so it never touches the sprite.
            barrel.AddComponent<SpriteRenderer>();
            barrel.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            var rb = barrel.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 2.0f;

            var block = barrel.AddComponent<DestructibleBlock>();
            block.maxHP = block.currentHP = 20f;
            block.scoreValue = 50;
            var exp = barrel.AddComponent<ExplosiveGimmick>();
            exp.targetWorldSize = 1.7f; // Scaled up by 1.41x (from 1.2f to 1.7f)
            return barrel;
        }

        /// <summary>Field-director entry: identical barrel, caller keeps the handle.</summary>
        public GameObject SpawnFieldBarrel(Vector3 position) => SpawnExplosiveBarrel(position);

        private void SetupUIButtons()
        {
            knightButton?.onClick.AddListener(() => SelectUnit(0));
            archerButton?.onClick.AddListener(() => SelectUnit(1));
            cannonButton?.onClick.AddListener(() => SelectUnit(2));
            gimmickButton?.onClick.AddListener(() => SelectUnit(3));

            // Selection-row sizing (playtest QA pass): character cards run 1.5x the original
            // 82x54 face ("캐릭터 선택버튼크기 1.5배"); the gimmick/barrel card grows a more
            // modest 1.2x ("기믹타입지정 버튼 ... 20프로정도 커지게") so both grow without the
            // face/text ratio breaking. LayoutSelectionRow re-centers the row with the new,
            // variable widths so the larger cards never overlap.
            const float baseWidth = 82f;
            const float baseHeight = 54f;
            const float characterScale = 1.5f;
            const float gimmickScale = 1.5f;
            var characterSize = new Vector2(baseWidth * characterScale, baseHeight * characterScale);
            var gimmickSize = new Vector2(baseWidth * gimmickScale, baseHeight * gimmickScale);

            StyleSelectionButton(knightButton, "Knight", 0, characterSize);
            StyleSelectionButton(archerButton, "Archer", 1, characterSize);
            StyleSelectionButton(cannonButton, "Cannon", 2, characterSize);
            StyleSelectionButton(gimmickButton, "ExplosiveBarrel", 3, gimmickSize);

            LayoutSelectionRow(
                new[] { knightButton, archerButton, cannonButton, gimmickButton },
                new[] { characterSize, characterSize, characterSize, gimmickSize },
                // Height above the bottom edge: clears the launch-guide line that runs along
                // the very bottom while keeping the row inside thumb reach on a phone.
                104f, 16f);

            if (knightButton != null && knightButton.GetComponent<GameButtonAnimator>() == null) knightButton.gameObject.AddComponent<GameButtonAnimator>();
            if (archerButton != null && archerButton.GetComponent<GameButtonAnimator>() == null) archerButton.gameObject.AddComponent<GameButtonAnimator>();
            if (cannonButton != null && cannonButton.GetComponent<GameButtonAnimator>() == null) cannonButton.gameObject.AddComponent<GameButtonAnimator>();
            if (gimmickButton != null && gimmickButton.GetComponent<GameButtonAnimator>() == null) gimmickButton.gameObject.AddComponent<GameButtonAnimator>();
            SetupLastStandButton();
        }

        private void SetupLastStandButton()
        {
            if (lastStandButton != null)
            {
                RefreshLastStandButton();
                return;
            }
            if (gimmickButton == null) return;

            var buttonGo = Instantiate(gimmickButton.gameObject, gimmickButton.transform.parent);
            buttonGo.name = "LastStandButton";
            lastStandButton = buttonGo.GetComponent<UnityEngine.UI.Button>();
            if (lastStandButton == null)
            {
                Destroy(buttonGo);
                return;
            }

            lastStandButton.onClick.RemoveAllListeners();
            lastStandButton.onClick.AddListener(ActivatePlayerLastStand);

            var portrait = buttonGo.transform.Find("UnitPortrait");
            if (portrait != null) portrait.gameObject.SetActive(false);

            var cardImage = lastStandButton.GetComponent<UnityEngine.UI.Image>();
            var lastStandArt = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.LastStandButton);
            if (cardImage != null && lastStandArt != null)
            {
                cardImage.sprite = lastStandArt;
                cardImage.type = UnityEngine.UI.Image.Type.Simple;
                cardImage.color = Color.white;
            }

            var rt = lastStandButton.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(0f, -220f);
                rt.sizeDelta = new Vector2(156f, 104f);
                rt.localScale = Vector3.one;
            }

            var label = lastStandButton.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
            if (label != null)
            {
                label.text = "R  LAST STAND\nARMED";
                label.enableAutoSizing = true;
                label.fontSizeMin = 8f;
                label.fontSizeMax = 14f;
                label.enableWordWrapping = false;
                label.fontStyle = TMPro.FontStyles.Bold;
                label.characterSpacing = 1f;
                label.outlineWidth = 0.16f;
                label.outlineColor = new Color(0.02f, 0.015f, 0.01f, 0.95f);
                label.color = new Color(1f, 0.82f, 0.45f, 1f);
                label.alignment = TMPro.TextAlignmentOptions.Center;

                var labelRt = label.GetComponent<RectTransform>();
                if (labelRt != null)
                {
                    labelRt.anchorMin = new Vector2(0f, 0f);
                    labelRt.anchorMax = new Vector2(1f, 0.34f);
                    labelRt.pivot = new Vector2(0.5f, 0f);
                    labelRt.anchoredPosition = new Vector2(0f, 0f);
                    labelRt.sizeDelta = new Vector2(-12f, 0f);
                }
            }

            if (lastStandButton.GetComponent<GameButtonAnimator>() == null) lastStandButton.gameObject.AddComponent<GameButtonAnimator>();
            RefreshLastStandButton();
        }

        private bool CanActivatePlayerLastStand()
        {
            return currentState == GameState.PlayerTurn
                && IsPlayerTurn
                && !IsResolvingTurn
                && playerLastStand == LastStand.Phase.Armed;
        }

        private void RefreshLastStandButton()
        {
            if (lastStandButton == null) return;

            bool available = CanActivatePlayerLastStand();
            lastStandButton.interactable = available;
            lastStandButton.gameObject.SetActive(available);
        }

        // Centers a row of variable-width selection cards with a fixed edge gap so the
        // enlarged character/gimmick cards (playtest sizing pass) never overlap or drift
        // off their old shared baseline.
        /// <summary>
        /// Lays the siege-order cards out as a bottom-centred bar. The anchor is forced here
        /// rather than trusted from the scene: the cards were anchored to screen centre, so
        /// the row sat across the middle of the battlefield, covering both castles and the
        /// rally rings — the busiest part of the board. <paramref name="y"/> is now a height
        /// above the bottom edge.
        /// </summary>
        private static void LayoutSelectionRow(UnityEngine.UI.Button[] buttons, Vector2[] sizes, float y, float gap)
        {
            float totalWidth = 0f;
            for (int i = 0; i < sizes.Length; i++) totalWidth += sizes[i].x;
            totalWidth += gap * Mathf.Max(0, sizes.Length - 1);

            float cursor = -totalWidth / 2f;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i] == null) { cursor += sizes[i].x + gap; continue; }
                var rt = buttons[i].GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0.5f, 0f);
                    rt.anchorMax = new Vector2(0.5f, 0f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = new Vector2(cursor + sizes[i].x / 2f, y);
                }
                cursor += sizes[i].x + gap;
            }
        }

        private void StyleSelectionButton(UnityEngine.UI.Button button, string unitName, int index, Vector2 size)
        {
            if (button == null) return;

            // Compact siege-order cards: readable while staying out of the firing lane. Actual
            // x-position is assigned afterwards by LayoutSelectionRow so the variable widths
            // (character cards 1.5x, gimmick card 1.2x) never overlap.
            var rt = button.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.sizeDelta = size;
            }
            float scale = size.y / 54f; // relative to the original 82x54 face


            // Prefer the high-contrast Higgsfield selection glyphs, but keep the existing
            // unit/gimmick art as a gameplay-safe fallback when optional art is unavailable.
            string higgsfieldKey = unitName == "ExplosiveBarrel" ? HiggsfieldSpriteLibrary.Barrel : unitName;
            Sprite unitSprite = HiggsfieldSpriteLibrary.LoadUi(higgsfieldKey);
            if (unitSprite == null && unitName == "ExplosiveBarrel")
            {
                unitSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.Barrel);
                if (unitSprite == null && explosiveBarrelPrefab != null)
                {
                    var sr = explosiveBarrelPrefab.GetComponent<SpriteRenderer>();
                    if (sr != null) unitSprite = sr.sprite;
                }
            }
            else if (unitSprite == null)
            {
                unitSprite = Resources.Load<Sprite>($"GeneratedUnitFrames/{unitName}/Idle/idle_000");
            }

            var image = button.GetComponent<UnityEngine.UI.Image>();
            var cardSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (image != null && cardSprite != null)
            {
                // Generated wooden card frame as the button face; the unit portrait rides on a
                // dedicated child so the frame and portrait can be tinted independently.
                image.sprite = cardSprite;
                // 9-slice (root-cause text-overflow fix): with spriteBorder now baked into
                // ui_button_card.png.meta, Sliced keeps the frame's border pixel-thickness
                // constant on any button aspect ratio instead of Simple's non-uniform stretch
                // (which used to squash/distort the frame into the label's safe area).
                image.type = UnityEngine.UI.Image.Type.Sliced;

                image.color = Color.white;

                var portraitTransform = button.transform.Find("UnitPortrait");
                if (portraitTransform == null)
                {
                    var portraitGo = new GameObject("UnitPortrait");
                    portraitGo.transform.SetParent(button.transform, false);
                    var portraitImage = portraitGo.AddComponent<UnityEngine.UI.Image>();
                    portraitImage.raycastTarget = false;
                    portraitImage.preserveAspect = true;
                    var portraitRt = portraitGo.GetComponent<RectTransform>();
                    portraitRt.anchorMin = new Vector2(0.5f, 0.62f);
                    portraitRt.anchorMax = new Vector2(0.5f, 0.62f);
                    portraitRt.pivot = new Vector2(0.5f, 0.5f);
                    portraitRt.sizeDelta = new Vector2(34f, 34f) * scale;

                    portraitGo.transform.SetSiblingIndex(0);
                    portraitTransform = portraitGo.transform;
                }
                var portrait = portraitTransform.GetComponent<UnityEngine.UI.Image>();
                if (portrait != null && unitSprite != null) portrait.sprite = unitSprite;
            }
            else if (image != null)
            {
                // Legacy look: portrait directly on the button image with a dark border frame.
                if (unitSprite != null)
                {
                    image.sprite = unitSprite;
                    image.color = new Color(1f, 0.93f, 0.62f, 1f);
                }

                var borderTransform = button.transform.Find("BorderFrame");
                if (borderTransform == null)
                {
                    var borderGo = new GameObject("BorderFrame");
                    borderGo.transform.SetParent(button.transform, false);
                    var borderImage = borderGo.AddComponent<UnityEngine.UI.Image>();
                    borderImage.color = new Color(0.15f, 0.15f, 0.15f, 0.85f);
                    var borderRt = borderGo.GetComponent<RectTransform>();
                    borderRt.anchorMin = Vector2.zero;
                    borderRt.anchorMax = Vector2.one;
                    borderRt.sizeDelta = new Vector2(2f, 2f);
                    borderGo.transform.SetAsFirstSibling();
                }
            }

            // Keep a small numeric shortcut label readable on the compact button.
            var text = button.GetComponentInChildren<TMPro.TextMeshProUGUI>();
            if (text != null)
            {
                string role = unitName == "Knight" ? "BREACH" : unitName == "Archer" ? "ARC" : unitName == "Cannon" ? "SIEGE" : "HAZARD";
                string callSign = unitName == "ExplosiveBarrel" ? "POWDER KEG" : unitName.ToUpperInvariant();
                text.text = $"{index + 1}  {callSign}\n{role}";
                // Auto-size inside the card so long call signs (POWDER KEG) can never
                // clip past the card face on any viewport scale — fontSizeMax also scales
                // with the enlarged 1.5x/1.2x card faces so bigger buttons show bigger text
                // instead of the same small caption floating in more empty space.
                text.enableAutoSizing = true;
                text.fontSizeMin = 5f * scale;
                text.fontSizeMax = (unitName == "ExplosiveBarrel" ? 8f : 9f) * scale;
                text.enableWordWrapping = false;
                text.fontStyle = TMPro.FontStyles.Bold;
                text.characterSpacing = 1.5f;
                text.outlineWidth = 0.16f;
                text.outlineColor = new Color(0.02f, 0.015f, 0.01f, 0.95f);
                text.color = new Color(0.96f, 0.98f, 1f, 1f);
                text.alignment = TMPro.TextAlignmentOptions.Center;

                var textRt = text.GetComponent<RectTransform>();
                if (textRt != null)
                {
                    textRt.anchorMin = new Vector2(0f, 0f);
                    textRt.anchorMax = new Vector2(1f, 0f);
                    textRt.pivot = new Vector2(0.5f, 0f);
                    textRt.anchoredPosition = new Vector2(0f, 2f);
                    textRt.sizeDelta = new Vector2(-4f, 26f * scale);

                }
            }
        }

        public void AddScore(bool isPlayer, int amount)
        {
            if (isPlayer) playerScore += amount; else enemyScore += amount;
            UpdateUI();
        }

        private void UpdateWind()
        {
            // Difficulty curve: the wind ceiling widens from windCapStart to windCapEnd and storm
            // odds climb as the match progresses, so early turns teach aiming under gentle drift
            // while late turns demand real wind compensation.
            float windCap = CurrentWindCap;
            float baseWind = Random.Range(-windCap, windCap);
            if (Random.value < CurrentStormChance)
            {
                baseWind = Mathf.Sign(baseWind == 0f ? 1f : baseWind) * Random.Range(windCap * 0.7f, windCap);
            }
            currentWindForce = baseWind;
            WindVfxManager.Instance?.PulseWindChange(currentWindForce);
            UpdateUI();
        }

        private void ApplyRuntimeSpriteAtlas()
        {
            if (SpriteAtlasPacker.Instance == null) return;

            int remapped = SpriteAtlasPacker.Instance.ApplyPackedSpritesInScene();
        }

        private void CreateGround()
        {
            var ground = new GameObject("Ground") { tag = "Ground" };
            var blockPrefab = Resources.Load<GameObject>("DestructibleBlock");
            var ironData = Resources.Load<BlockData>("IronBlockData");
            var stoneData = Resources.Load<BlockData>("StoneBlockData");
            var woodData = Resources.Load<BlockData>("WoodBlockData");
            if (blockPrefab == null) return;

            // Generate one seamless ground texture and slice it per-tile, so neighbouring tiles read as
            // a single continuous map instead of a row of independent props. Resolution raised from 128
            // to 160px/tile (vs. the old flat color-per-tile look) for crisper edges when tiles crack,
            // fall, and get sampled by the explosion debris/particle systems; rows raised from 3 to
            // groundRowCount so the strip fills the camera's visible ground band instead of floating
            // above bare background.
            const int groundRowCount = 5;
            int blockRes = 160;
            int texWidth = groundColumnCount * blockRes;
            int texHeight = groundRowCount * blockRes;
            Texture2D groundTex = GenerateGroundTexture(texWidth, texHeight);

            for (int yIndex = 0; yIndex < groundRowCount; yIndex++)
            {
                float y = -0.5f - yIndex;
                int gridY = (groundRowCount - 1) - yIndex; // yIndex=0 is top row, last yIndex is bottom row

                for (int x = -groundHalfWidth; x <= groundHalfWidth; x++)
                {
                    int gridX = x + groundHalfWidth;
                    var go = Instantiate(blockPrefab, new Vector3(x, y, 0f), Quaternion.identity);
                    go.name = $"GroundBlock_{x}_{yIndex}";
                    var block = go.GetComponent<DestructibleBlock>();
                    if (block != null)
                    {
                        BlockData selectedData;
                        if (x >= -2 && x <= 2) selectedData = woodData;
                        else if ((x >= -5 && x <= -3) || (x >= 3 && x <= 5)) selectedData = stoneData;
                        else if ((x >= -8 && x <= -6) || (x >= 6 && x <= 8)) selectedData = ironData;
                        else selectedData = stoneData;

                        block.ApplyBlockData(selectedData);
                        // Anchors: the outer flanks (castle foundations) and the bottom two rows.
                        // The visible top rows (yIndex 0-2) stay breakable so the wood bridge can
                        // still be severed and dropped, but a cascade can never disintegrate the
                        // entire 41x5 grid down to the kill-plane - collapse depth is bounded.
                        block.isGroundAnchor = (x <= -groundAnchorAbsX || x >= groundAnchorAbsX) || yIndex >= groundRowCount - 2;

                        // Slice the seamless texture for this tile and recompute the block's scale +
                        // collider from it (SetPresentationSprite), instead of only swapping the visible
                        // sprite. ApplyBlockData above sized the collider/transform to selectedData's
                        // ~12.5u source art scaled down to 1u; leaving that scale in place while silently
                        // replacing the sprite with this already-1u-native texture slice used to render
                        // the tile at a fraction of its collider size - the exact "floating collision box"
                        // mismatch between visuals and physics that made the ground feel disconnected.
                        int pixelX = gridX * blockRes;
                        int pixelY = gridY * blockRes;
                        Sprite normalSlice = Sprite.Create(groundTex, new Rect(pixelX, pixelY, blockRes, blockRes), new Vector2(0.5f, 0.5f), blockRes);
                        normalSlice.name = $"GroundSlice_{x}_{yIndex}_Normal";
                        // Reset tint to white: the sliced ground texture already carries its own
                        // natural colors, so blockData.blockColor (applied by ApplyBlockData above for
                        // the non-ground case) must not be left multiplying it.
                        block.SetPresentationSprite(normalSlice, Color.white);


                        // Cracked/heavily-cracked slices are expensive to bake (per-pixel blend of the
                        // ground art against the crack pattern) and most ground tiles never visibly crack
                        // in a given match. Defer the bake until the tile's HP actually drops into that
                        // band instead of doing it for every one of the groundRowCount*columnCount tiles
                        // up front.
                        BlockData capturedData = selectedData;
                        block.SetLazyCrackedSprites(
                            () => CreateCrackedSlice(groundTex, pixelX, pixelY, capturedData.crackedSprite, blockRes),
                            () => CreateCrackedSlice(groundTex, pixelX, pixelY, capturedData.heavilyCrackedSprite, blockRes));
                    }
                    if (x < 0 && playerCastle != null) go.transform.SetParent(playerCastle.transform);
                    else if (x >= 0 && enemyCastle != null) go.transform.SetParent(enemyCastle.transform);
                    else go.transform.SetParent(ground.transform);
                }
            }
            if (playerCastle != null) playerCastle.RefreshBlockList();
            if (enemyCastle != null) enemyCastle.RefreshBlockList();
        }


        private Texture2D GenerateGroundTexture(int width, int height)
        {
            // Mipmapped + trilinear so the tilemap (and any sliced crack art baked from it) minifies
            // cleanly instead of shimmering/aliasing when the camera zooms out or a tile is scaled down
            // for explosion debris/particles - the actual "aliasing when it breaks" complaint.
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, true);
            tex.filterMode = FilterMode.Trilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.anisoLevel = 4;
            Color32[] colors = new Color32[width * height];

            // Beautiful organic colors
            Color32 grassColor = new Color32(76, 154, 42, 255);
            Color32 dirtColor = new Color32(139, 90, 43, 255);
            Color32 stoneColor = new Color32(90, 90, 90, 255);

            // The organic sine-wave boundaries only vary with x, but the original implementation
            // recomputed them for every single (x, y) pixel - i.e. height times more Sin/Cos calls
            // than necessary. Precompute one value per column instead; at higher tile resolution and
            // row counts this is the difference between ~10M and ~13K trig calls per ground rebuild.
            float[] grassBoundaryByColumn = new float[width];
            float[] stoneBoundaryByColumn = new float[width];
            for (int x = 0; x < width; x++)
            {
                grassBoundaryByColumn[x] = height * 0.8f;
                stoneBoundaryByColumn[x] = height * 0.4f;
            }

            for (int y = 0; y < height; y++)
            {
                int rowOffset = y * width;
                for (int x = 0; x < width; x++)
                {
                    float grassBoundary = grassBoundaryByColumn[x];
                    float stoneBoundary = stoneBoundaryByColumn[x];

                    Color32 finalColor;
                    if (y > grassBoundary)
                    {
                        // Grass layer with noise
                        byte noise = (byte)Random.Range(-10, 10);
                        finalColor = new Color32(
                            (byte)Mathf.Clamp(grassColor.r + noise, 0, 255),
                            (byte)Mathf.Clamp(grassColor.g + noise, 0, 255),
                            (byte)Mathf.Clamp(grassColor.b + noise, 0, 255),
                            255
                        );
                    }
                    else if (y > stoneBoundary)
                    {
                        float distToGrass = grassBoundary - y;
                        if (distToGrass < 8f)
                        {
                            float t = distToGrass / 8f;
                            finalColor = Color32.Lerp(grassColor, dirtColor, t);
                        }
                        else
                        {
                            // Dirt layer with noise
                            byte noise = (byte)Random.Range(-8, 8);
                            finalColor = new Color32(
                                (byte)Mathf.Clamp(dirtColor.r + noise, 0, 255),
                                (byte)Mathf.Clamp(dirtColor.g + noise, 0, 255),
                                (byte)Mathf.Clamp(dirtColor.b + noise, 0, 255),
                                255
                            );
                        }
                    }
                    else
                    {
                        float distToDirt = stoneBoundary - y;
                        if (distToDirt < 12f)
                        {
                            float t = distToDirt / 12f;
                            finalColor = Color32.Lerp(dirtColor, stoneColor, t);
                        }
                        else
                        {
                            // Stone layer with noise
                            byte noise = (byte)Random.Range(-6, 6);
                            finalColor = new Color32(
                                (byte)Mathf.Clamp(stoneColor.r + noise, 0, 255),
                                (byte)Mathf.Clamp(stoneColor.g + noise, 0, 255),
                                (byte)Mathf.Clamp(stoneColor.b + noise, 0, 255),
                                255
                            );
                        }
                    }
                    colors[rowOffset + x] = finalColor;
                }
            }
            tex.SetPixels32(colors);
            tex.Apply(true, false);
            return tex;

        }

        private Sprite CreateCrackedSlice(Texture2D groundTex, int pixelX, int pixelY, Sprite crackSprite, int res)
        {
            if (crackSprite == null) return null;

            Texture2D crackedTex = new Texture2D(res, res, TextureFormat.RGBA32, true);
            crackedTex.filterMode = FilterMode.Trilinear;
            crackedTex.wrapMode = TextureWrapMode.Clamp;
            crackedTex.anisoLevel = 4;


            Color[] basePixels = groundTex.GetPixels(pixelX, pixelY, res, res);
            Color[] blended = new Color[res * res];

            bool textureReadable = false;
            Texture2D crackTex = null;
            Rect crackRect = Rect.zero;

            try
            {
                crackTex = crackSprite.texture;
                crackRect = crackSprite.textureRect;
                // Test read to see if it throws an exception
                crackTex.GetPixel((int)crackRect.x, (int)crackRect.y);
                textureReadable = true;
            }
            catch
            {
                textureReadable = false;
            }

            if (textureReadable && crackTex != null)
            {
                for (int y = 0; y < res; y++)
                {
                    for (int x = 0; x < res; x++)
                    {
                        Color baseCol = basePixels[y * res + x];
                        float u = (float)x / res;
                        float v = (float)y / res;
                        int cx = (int)(crackRect.x + u * crackRect.width);
                        int cy = (int)(crackRect.y + v * crackRect.height);
                        Color crackCol = crackTex.GetPixel(cx, cy);

                        // Blend: darken the base color based on the crack's darkness/alpha
                        float blendFactor = crackCol.a * (1f - (crackCol.r + crackCol.g + crackCol.b) / 3f);
                        blended[y * res + x] = Color.Lerp(baseCol, Color.black, blendFactor * 0.6f);
                    }
                }
            }
            else
            {
                // Fallback: Procedurally generate a high-resolution anti-aliased crack pattern
                // Generate a few random jagged crack lines
                List<Vector2[]> crackLines = new List<Vector2[]>();
                int numCracks = Random.Range(2, 4);
                for (int i = 0; i < numCracks; i++)
                {
                    Vector2 start = new Vector2(Random.Range(res * 0.2f, res * 0.8f), Random.Range(res * 0.2f, res * 0.8f));
                    Vector2 end = start + new Vector2(Random.Range(-res * 0.4f, res * 0.4f), Random.Range(-res * 0.4f, res * 0.4f));
                    
                    // Create a jagged line with 3 segments
                    Vector2 mid1 = Vector2.Lerp(start, end, 0.33f) + new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
                    Vector2 mid2 = Vector2.Lerp(start, end, 0.66f) + new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
                    crackLines.Add(new Vector2[] { start, mid1, mid2, end });
                }

                for (int y = 0; y < res; y++)
                {
                    for (int x = 0; x < res; x++)
                    {
                        Color baseCol = basePixels[y * res + x];
                        Vector2 p = new Vector2(x, y);
                        
                        // Find minimum distance to any crack line segment
                        float minDist = float.MaxValue;
                        foreach (var line in crackLines)
                        {
                            for (int i = 0; i < line.Length - 1; i++)
                            {
                                Vector2 a = line[i];
                                Vector2 b = line[i + 1];
                                Vector2 v = b - a;
                                Vector2 w = p - a;
                                float t = Mathf.Clamp01(Vector2.Dot(w, v) / Vector2.Dot(v, v));
                                Vector2 c = a + t * v;
                                float dist = (p - c).magnitude;
                                if (dist < minDist) minDist = dist;
                            }
                        }

                        // Anti-aliased crack line drawing (thickness of 1.5 pixels)
                        float crackThickness = 1.5f;
                        if (minDist < crackThickness)
                        {
                            float blendFactor = Mathf.Clamp01(1f - minDist / crackThickness);
                            blended[y * res + x] = Color.Lerp(baseCol, new Color(0.15f, 0.15f, 0.15f, 1f), blendFactor * 0.75f);
                        }
                        else
                        {
                            blended[y * res + x] = baseCol;
                        }
                    }
                }
            }

            crackedTex.SetPixels(blended);
            // Lazily generated and used once as a plain sprite - build mips for smooth minification
            // and drop the CPU-side copy (makeNoLongerReadable) since nothing reads it again.
            crackedTex.Apply(true, true);

            return Sprite.Create(crackedTex, new Rect(0, 0, res, res), new Vector2(0.5f, 0.5f), res);
        }

        public void StartGame()
        {
            // Idempotent entry: tests and the intro gate both call this. Any lingering intro
            // overlay or frozen timescale is cleared before the first turn begins — including
            // a narration cutscene. Its canvas sits at sortingOrder 900 WITH a raycaster, so
            // one left alive over a live match both holds the board in Intro and swallows
            // every UI click underneath it.
            if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();
            if (introScreen != null) { introScreen.Dismiss(); introScreen = null; }
            Time.timeScale = 1f;
            turnCount = 0;
            playerLastStand = LastStand.Phase.Locked;
            aiLastStand = LastStand.Phase.Locked;
            playerDangerNotified = false;
            aiDangerNotified = false;
            // Rematch hygiene: field pieces from the previous match must not survive with
            // stale bornTurn ages, and the danger heartbeat must not bleed into a fresh
            // match (review P2 #4).
            GimmickFieldDirector.Instance?.ResetField();
            // Ruin-presentation state (crack-decal counts, wholeness milestones) is keyed by
            // block/castle object identity; a scene reload spawns fresh objects, so stale keys
            // from the previous match are dead weight — clear them with the same cadence.
            CastleRuinFx.ResetForNewMatch();
            BrickPlacementController.Instance?.ClearPending();
            HeroGrowth.Reset();
            GameplayUxDirector.SetDangerState(false);
            DeploymentController.Instance?.ResetEconomy();
            currentState = GameState.PlayerTurn;
            isPlayerTurn = true;
            turnTimer = turnDuration;
            SelectUnit(0);
            UpdateUI();
            RefreshLastStandButton();
            GameplayUxDirector.NotifyTurnChanged(true);
        }

        private void Update()
        {
            if (currentState == GameState.Intro)
            {
                if (webtoonPrologue != null)
                {
                    if (Input.GetKeyDown(KeyCode.Space)) webtoonPrologue.AdvancePage();
                    if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) webtoonPrologue.SkipToTitle();
                    return;
                }
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
                {
                    BeginSiege();
                }
                return;
            }


            if (isPlayerTurn)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) SelectUnit(0);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) SelectUnit(1);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) SelectUnit(2);
                else if (Input.GetKeyDown(KeyCode.Alpha4)) SelectUnit(3);
            }

            UpdateLastStandState();

            // While a volley resolves (units flying, blocks settling) the clock must NOT run:
            // letting it expire mid-resolve double-fired EndTurn (Update + WaitAndEndTurn) and
            // silently skipped the player's next turn.
            if (isResolvingTurn) return;

            var lm = LaunchManagerRef;
            bool aiming = isPlayerTurn && lm != null && lm.IsAiming;

            turnTimer -= Time.deltaTime;
            if (timerText != null) timerText.text = Mathf.CeilToInt(Mathf.Max(0f, turnTimer)).ToString();

            // Player-turn coaching: urgency warning near expiry, idle nudges while nothing is
            // happening, so the turn never silently evaporates while the player reads the board.
            if (isPlayerTurn)
            {
                if (!urgencyNotified && turnTimer <= UrgencyThresholdSeconds && turnTimer > 0f)
                {
                    urgencyNotified = true;
                    GameplayUxDirector.NotifyTurnUrgency(Mathf.CeilToInt(turnTimer));
                }

                if (!aiming && !urgencyNotified)
                {
                    idleNudgeTimer += Time.deltaTime;
                    if (idleNudgeTimer >= IdleNudgeIntervalSeconds)
                    {
                        idleNudgeTimer = 0f;
                        GameplayUxDirector.NotifyIdleNudge();
                        if (lm != null) GameFeelVfx.SpawnShockwaveRing(lm.GetLaunchPosition(), new Color(0.45f, 0.85f, 1f, 0.5f), 1.6f, 0.5f);
                    }
                }
                else
                {
                    idleNudgeTimer = 0f;
                }
            }

            if (turnTimer <= 0f)
            {
                var decision = DecideTurnExpiry(isPlayerTurn, aiming, graceUsedThisTurn);
                switch (decision)
                {
                    case TurnExpiryDecision.GrantGrace:
                        graceUsedThisTurn = true;
                        turnTimer = AimGraceSeconds;
                        GameplayUxDirector.NotifyAimGrace(AimGraceSeconds);
                        break;
                    case TurnExpiryDecision.ForfeitPlayerTurn:
                        GameplayUxDirector.NotifyTurnForfeited();
                        EndTurn();
                        break;
                    default:
                        EndTurn();
                        break;
                }
            }
        }

        public enum TurnExpiryDecision { EndTurn, GrantGrace, ForfeitPlayerTurn }

        // Pure decision so tests can pin the contract: an actively-aiming player gets ONE short
        // grace window to release the shot instead of having the turn yanked mid-drag; an idle
        // player forfeits with an explicit notice; AI expiry always just ends the turn.
        public static TurnExpiryDecision DecideTurnExpiry(bool isPlayerTurn, bool isAiming, bool graceAlreadyUsed)
        {
            if (!isPlayerTurn) return TurnExpiryDecision.EndTurn;
            if (isAiming && !graceAlreadyUsed) return TurnExpiryDecision.GrantGrace;
            return TurnExpiryDecision.ForfeitPlayerTurn;
        }

        // ---- LAST STAND (comeback) ----

        private void UpdateLastStandState()
        {
            // Arm latch + danger feedback for the player core.
            bool playerDanger = PlayerCoreInDanger;
            var advancedPlayer = LastStand.Advance(playerLastStand, playerDanger);
            if (advancedPlayer != playerLastStand)
            {
                playerLastStand = advancedPlayer;
                GameplayUxDirector.NotifyLastStandArmed(true);
            }
            if (playerDanger != playerDangerNotified)
            {
                playerDangerNotified = playerDanger;
                GameplayUxDirector.SetDangerState(playerDanger);
            }

            // AI mirror: danger arms AND activates in one pure step (LastStand.AdvanceAuto).
            var advancedAi = LastStand.AdvanceAuto(aiLastStand, EnemyCoreInDanger);
            if (advancedAi != aiLastStand)
            {
                aiLastStand = advancedAi;
                if (!aiDangerNotified)
                {
                    aiDangerNotified = true;
                    GameplayUxDirector.NotifyLastStandArmed(false);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                ActivatePlayerLastStand();
            }
            else
            {
                RefreshLastStandButton();
            }
        }

        public void ActivatePlayerLastStand()
        {
            if (!CanActivatePlayerLastStand())
            {
                RefreshLastStandButton();
                return;
            }

            playerLastStand = LastStand.Phase.Active;
            RefreshLastStandButton();
            GameplayUxDirector.NotifyLastStandActive();
            var lm = LaunchManagerRef;
            if (lm != null)
            {
                GameFeelVfx.SpawnShockwaveRing(lm.GetLaunchPosition(), new Color(1f, 0.35f, 0.2f, 0.8f), 2.6f, 0.6f);
            }
        }

        /// <summary>Returns the visible launch velocity without consuming an active Last Stand.</summary>
        public Vector2 PreviewLastStandLaunchVelocity(bool isPlayer, Vector2 velocity)
        {
            LastStand.Phase phase = isPlayer ? playerLastStand : aiLastStand;
            return phase == LastStand.Phase.Active ? velocity * LastStand.SpeedMult(isPlayer) : velocity;
        }

        /// <summary>
        /// Converts a desired final velocity into the pre-buff velocity UnitController.Launch
        /// must receive. Previewing the result and then applying the active Last Stand returns
        /// the intended trajectory instead of making AI aim overshoot by the speed multiplier.
        /// </summary>
        public Vector2 PrepareLastStandLaunchVelocity(bool isPlayer, Vector2 desiredFinalVelocity)
        {
            LastStand.Phase phase = isPlayer ? playerLastStand : aiLastStand;
            if (phase != LastStand.Phase.Active) return desiredFinalVelocity;

            float speedMultiplier = LastStand.SpeedMult(isPlayer);
            return speedMultiplier > 0f ? desiredFinalVelocity / speedMultiplier : desiredFinalVelocity;
        }

        /// <summary>
        /// Launch chokepoint: units call in right before their velocity is applied. Consumes an
        /// Active LAST STAND for the owning side and returns the buff multipliers exactly once.
        /// </summary>
        public Vector2 ApplyLastStandOnLaunch(UnitController unit, Vector2 velocity)
        {
            if (unit == null) return velocity;
            bool isPlayer = unit.isPlayerUnit;
            ref LastStand.Phase phase = ref isPlayer ? ref playerLastStand : ref aiLastStand;
            if (phase != LastStand.Phase.Active) return velocity;

            phase = LastStand.Phase.Consumed;
            RefreshLastStandButton();
            // Capped: one buffed hit must never erase a full 150-HP core (see LastStand notes).
            unit.attackDamage = LastStand.BuffedDamage(unit.attackDamage, isPlayer);
            var explosive = unit.GetComponent<ExplosiveGimmick>();
            if (explosive != null)
            {
                explosive.SetPermanentPotency(
                    LastStand.BuffedDamage(explosive.PermanentExplosionDamage, isPlayer),
                    explosive.PermanentExplosionRadius * LastStand.RadiusMult(isPlayer),
                    LastStand.SingleHitDamageCap);
                unit.explosionDamage = explosive.explosionDamage;
                unit.explosionRadius = explosive.explosionRadius;
            }
            else
            {
                unit.explosionDamage = LastStand.BuffedDamage(unit.explosionDamage, isPlayer);
                unit.explosionRadius *= LastStand.RadiusMult(isPlayer);
            }

            GameFeelVfx.SpawnImpactBurst(unit.transform.position, new Color(1f, 0.3f, 0.15f, 0.95f), 1.1f, null, false);
            GameFeelVfx.SpawnFeedbackLabel(unit.transform.position,
                isPlayer ? "일발역전!" : "적의 발악!",
                new Color(1f, 0.42f, 0.2f, 1f), 2.8f, 0.9f);
            return velocity * LastStand.SpeedMult(isPlayer);
        }

        public void SelectUnit(int unitTypeIndex)
        {
            DeployCard card;
            switch (unitTypeIndex)
            {
                case 0:
                    card = DeployCard.Knight;
                    selectedUnitPrefab = knightPrefab;
                    break;
                case 1:
                    card = DeployCard.Archer;
                    selectedUnitPrefab = archerPrefab;
                    break;
                case 2:
                    card = DeployCard.Cannon;
                    selectedUnitPrefab = null;
                    break;
                case 3:
                    card = DeployCard.Barrel;
                    selectedUnitPrefab = explosiveBarrelPrefab;
                    break;
                default:
                    return;
            }

            var deployment = DeploymentController.Instance;
            if (card != DeployCard.Cannon) deployment?.DisarmDeployMode();
            deployment?.SetSelectedCard(card);
            LaunchManagerRef?.SetSelectedUnit(selectedUnitPrefab, card);
            UpdateButtonVisuals(unitTypeIndex);
        }

        private void UpdateButtonVisuals(int selectedIndex)
        {
            StyleButtonState(knightButton, selectedIndex == 0);
            StyleButtonState(archerButton, selectedIndex == 1);
            StyleButtonState(cannonButton, selectedIndex == 2);
            StyleButtonState(gimmickButton, selectedIndex == 3);
        }


        private void StyleButtonState(UnityEngine.UI.Button button, bool isSelected)
        {
            if (button == null) return;
            bool cardLook = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard) != null;

            var image = button.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                image.color = cardLook
                    ? (isSelected ? Color.white : new Color(0.62f, 0.62f, 0.66f, 0.88f))
                    : (isSelected ? new Color(1f, 0.93f, 0.62f, 1f) : new Color(0.42f, 0.48f, 0.56f, 0.72f));
            }

            var portraitTransform = button.transform.Find("UnitPortrait");
            if (portraitTransform != null)
            {
                var portrait = portraitTransform.GetComponent<UnityEngine.UI.Image>();
                if (portrait != null) portrait.color = isSelected ? Color.white : new Color(0.75f, 0.75f, 0.8f, 0.9f);
            }

            var borderTransform = button.transform.Find("BorderFrame");
            if (borderTransform != null)
            {
                var borderImage = borderTransform.GetComponent<UnityEngine.UI.Image>();
                if (borderImage != null)
                {
                    borderImage.color = isSelected ? new Color(1f, 0.78f, 0.24f, 0.98f) : new Color(0.10f, 0.14f, 0.20f, 0.88f);
                }
            }

            var rt = button.GetComponent<RectTransform>();
            if (rt != null)
            {
                // Selection feedback: slightly larger pop (was 1.08x) so the active unit card
                // reads clearly against its neighbors at a glance.
                rt.localScale = isSelected ? new Vector3(1.12f, 1.12f, 1f) : Vector3.one;
            }
        }

        public void OnUnitLaunched(UnitController unit)
        {
            if (unit != null && !activeUnits.Contains(unit)) activeUnits.Add(unit);
            isResolvingTurn = true; // clock pauses while the volley resolves
            RefreshLastStandButton();
            StartCoroutine(WaitAndEndTurn(unit));
        }

        public void OnUnitDied(UnitController unit, bool? damageFromPlayer = null)
        {
            activeUnits.RemoveAll(u => u == unit || u == null);
            if (unit != null && damageFromPlayer.HasValue && damageFromPlayer.Value != unit.isPlayerUnit)
            {
                DeploymentController.Instance?.CreditKill(unit.isPlayerUnit);
            }
            CheckVictoryConditions();
        }

        private IEnumerator WaitAndEndTurn(UnitController launchedUnit)
        {
            var lm = FindObjectOfType<LaunchManager>();
            if (lm != null) lm.enabled = false;

            // Wait until every launched body and every armed Barrel fuse has resolved. A Barrel
            // becomes Grounded while its telegraph runs, but handing the turn over at that point
            // misattributes its delayed explosion to the opponent.
            // Hard 12s watchdog: a projectile or fuse wedged in an unforeseen state must never
            // freeze the match ("멈춰있는 것처럼 보임" flow-clarity pass).
            float launchedWait = 0f;
            bool anyLaunched = true;
            while (anyLaunched && launchedWait < 12f)
            {
                anyLaunched = false;
                for (int i = 0; i < UnitController.Active.Count; i++)
                {
                    var u = UnitController.Active[i];
                    if (u != null && (u.CurrentState == UnitState.Launched || u.IsFusePending)) { anyLaunched = true; break; }
                }
                launchedWait += Time.deltaTime;
                yield return null;
            }
            yield return new WaitForSeconds(PostImpactHoldSeconds);


            float settleTimer = 0f;
            while (settleTimer < 3f)
            {
                bool blocksMoving = false;
                for (int i = 0; i < DestructibleBlock.Active.Count; i++)
                {
                    var b = DestructibleBlock.Active[i];
                    if (b.TryGetComponent<Rigidbody2D>(out var rb) && rb.bodyType == RigidbodyType2D.Dynamic && rb.velocity.magnitude > 0.2f)
                    {
                        blocksMoving = true;
                        break;
                    }
                }
                bool arrowsInFlight = ArrowController.Active.Count > 0;

                if (!blocksMoving && !arrowsInFlight) break;

                settleTimer += Time.deltaTime;
                yield return null;
            }

            if (lm != null) lm.enabled = true;
            isResolvingTurn = false;
            EndTurn();
        }

        private void EndTurn()
        {
            if (currentState == GameState.GameOver) return;
            // Every handoff owns aim cleanup. A lost pointer or expired grace must not carry
            // drag state, trajectory, or rubber-band visuals through the AI turn.
            LaunchManagerRef?.CancelAim();
            turnCount++;
            isPlayerTurn = !isPlayerTurn;
            currentState = isPlayerTurn ? GameState.PlayerTurn : GameState.AITurn;
            turnTimer = turnDuration;
            graceUsedThisTurn = false;
            urgencyNotified = false;
            idleNudgeTimer = 0f;
            isResolvingTurn = false;
            RefreshLastStandButton();
            UpdateWind();
            UpdateUI();
            GameplayUxDirector.NotifyTurnChanged(isPlayerTurn);
            GimmickFieldDirector.Instance?.NotifyTurnAdvanced(turnCount);
            // Pre-designated player bricks materialize as the player's turn opens.
            BrickPlacementController.Instance?.OnTurnChanged(isPlayerTurn);
            if (!isPlayerTurn) StartCoroutine(ExecuteAITurn());
        }

        private IEnumerator ExecuteAITurn()
        {
            yield return new WaitForSeconds(1.5f);
            var ai = FindObjectOfType<SimpleAI>();
            if (ai != null)
            {
                // AI aim tightens along the same difficulty curve the wind rides on.
                ai.errorOffsetRange = CurrentAiErrorOffset;
                ai.TakeTurn();
            }
            else EndTurn();
        }

        public void RegisterAIUnit(UnitController unit) => activeUnits.Add(unit);

        private void UpdateUI()
        {
            if (turnText != null) turnText.text = isPlayerTurn ? "YOUR SIEGE TURN" : "ENEMY BATTERY";
            if (windText != null)
            {
                string direction = currentWindForce > 0.15f ? "EAST >>>" : currentWindForce < -0.15f ? "<<< WEST" : "CALM -";
                string strength = Mathf.Abs(currentWindForce) >= 3.5f ? "GALE" : Mathf.Abs(currentWindForce) >= 1.5f ? "BREEZE" : "STEADY";
                windText.text = $"BANNER WIND {direction}\n{strength} {Mathf.Abs(currentWindForce):F1}";
                windText.color = Mathf.Abs(currentWindForce) >= 3.5f ? new Color(1f, 0.78f, 0.25f, 1f) : new Color(0.65f, 0.9f, 1f, 1f);
            }
            if (scoreText != null) scoreText.text = $"SIEGE SCORE  {playerScore} - {enemyScore}";
        }

        public void CheckVictoryConditions()
        {
            if (currentState == GameState.GameOver) return;

            if (playerCore != null && enemyCore != null)
            {
                if (enemyCore.currentHP <= 0)
                {
                    EndGame("BREACH COMPLETE — VICTORY!");
                    return;
                }
                else if (playerCore.currentHP <= 0)
                {
                    EndGame("KEEP FALLEN — DEFEAT!");
                    return;
                }
            }
            else
            {
                if (playerCastle != null && enemyCastle != null)
                {
                    int playerBlockCount = playerCastle.GetComponentsInChildren<DestructibleBlock>().Length;
                    int enemyBlockCount = enemyCastle.GetComponentsInChildren<DestructibleBlock>().Length;

                    if (enemyBlockCount == 0 && playerBlockCount > 0)
                    {
                        EndGame("BREACH COMPLETE — VICTORY!");
                        return;
                    }
                    else if (playerBlockCount == 0)
                    {
                        EndGame("KEEP FALLEN — DEFEAT!");
                        return;
                    }
                }
            }
        }

        private void EndGame(string result)
        {
            currentState = GameState.GameOver;
            LaunchManagerRef?.CancelAim();
            RefreshLastStandButton();
            // Legacy scene game-over panel stays hidden: the ResultsScreenController card is
            // the single source of outcome UI — the old green banner bled through behind it.
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            // Retention loop (AC11): freeze the aftermath diorama and raise the results
            // card — stats, grade seal, persistent ranking, rematch/title/next-stage actions.
            bool victory = result.Contains("VICTORY");
            bool lastStandUsed = playerLastStand == LastStand.Phase.Consumed;

            // Best-of-3 series (SiegeSeries): the overall contest is decided by winning 2 of
            // up to 3 games, so every EndGame() call tallies into the running series instead
            // of being judged alone. seriesGamesPlayed/PlayerWins/EnemyWins/ScoreTotal are
            // static and survive the scene reload a "next game" or "next stage" triggers.
            if (victory) seriesPlayerWins++; else seriesEnemyWins++;
            seriesGamesPlayed++;
            seriesScoreTotal += playerScore;
            bool seriesDecided = SiegeSeries.IsSeriesDecided(seriesPlayerWins, seriesEnemyWins);
            bool seriesWonByPlayer = seriesDecided && SiegeSeries.PlayerWonSeries(seriesPlayerWins, seriesEnemyWins);
            int warChestReward = seriesWonByPlayer ? SiegePrototypeEconomy.AwardSeriesVictory() : 0;

            // Sequential campaign (§Stage progression): a SERIES win folds into the unlock
            // frontier; only offer NEXT STAGE when the series is actually decided in the
            // player's favor AND that decision unlocked one (replaying an already-cleared
            // stage still returns the existing frontier, which may equal currentStage's
            // successor or be further ahead — either way StageProgress.NextStage(currentStage)
            // is the correct "what comes right after THIS stage" offer, gated on it being
            // reachable). A single game win mid-series does NOT unlock the next stage —
            // only clinching the best-of-3 does.
            StageId? nextStage = null;
            if (seriesWonByPlayer)
            {
                var highestUnlocked = StageProgressStore.RecordVictory(currentStage);
                var candidate = StageProgress.NextStage(currentStage);
                if (candidate.HasValue && StageProgress.IsUnlocked(highestUnlocked, candidate.Value))
                    nextStage = candidate;
            }
            HitStopManager.Instance?.CancelPendingHitStop();
            Time.timeScale = 0f;

            // The scoreboard is built FIRST, always. Gating its creation behind a cutscene
            // meant "the match ended" and "the results exist" stopped being the same moment,
            // which broke every caller that waits for a results screen after a siege ends.
            ResultsScreenController.Create(victory, turnCount, playerScore,
                GameplayUxDirector.SessionMaxCombo, lastStandUsed, nextStage,
                seriesPlayerWins, seriesEnemyWins, seriesGamesPlayed, seriesDecided, seriesScoreTotal, warChestReward);

            // Campaign closer: clinching the series on the FINAL stage ends the campaign, and
            // that deserves a beat. It plays OVER the finished scoreboard (the interlude canvas
            // sorts above it), so the closing narration is a curtain on top of the result
            // rather than a gate in front of it. `nextStage == null` alone is not the test —
            // a mid-campaign defeat also has no next stage. It must be a series win with
            // nothing left after this stage.
            bool campaignCleared = seriesWonByPlayer && !StageProgress.NextStage(currentStage).HasValue;
            if (campaignCleared)
            {
                StageInterludeController.Play(StageInterlude.Epilogue(), null);
            }

            // The series is now fully resolved (2 wins clinched or 3 games played): the next
            // EndGame from Rematch/NextGame/NextStage must start counting a brand-new series
            // at 0-0, never keep accumulating across unrelated series.
            if (seriesDecided) ResetSeries();

        }

        // ---- Retention loop actions (results screen buttons / R key) ----

        // Scene reload rebuilds the entire runtime-generated world; this flag survives the
        // reload (static, domain stays loaded) and routes the fresh boot past the intro.
        private static bool skipIntroOnce;

        // Best-of-3 series tally (see SiegeSeries): survives ReloadArena() the same way
        // skipIntroOnce/PendingStage do, so consecutive games within one series keep their
        // running win counts and aggregate score across scene reloads. Reset to 0 whenever a
        // NEW series should start (RequestRematch, RequestTitle, RequestStage) - RequestNextGame
        // is the only entry point that deliberately leaves these alone.
        private static int seriesPlayerWins;
        private static int seriesEnemyWins;
        private static int seriesGamesPlayed;
        private static int seriesScoreTotal;

        private static void ResetSeries()
        {
            seriesPlayerWins = 0;
            seriesEnemyWins = 0;
            seriesGamesPlayed = 0;
            seriesScoreTotal = 0;
        }



        public static void RequestRematch()
        {
            skipIntroOnce = true;
            ResetSeries();
            ReloadArena();
        }

        /// <summary>
        /// Results-screen "다음 경기" action: continues the CURRENT best-of-3 series to its
        /// next game (same stage, series win counts/aggregate score untouched) - unlike
        /// RequestRematch, which deliberately starts a brand-new series from 0-0. Only ever
        /// invoked while SiegeSeries.IsSeriesDecided(seriesPlayerWins, seriesEnemyWins) is
        /// still false (ResultsScreenController only shows this action mid-series).
        /// </summary>
        public static void RequestNextGame()
        {
            skipIntroOnce = true;
            ReloadArena();
        }

        public static void RequestTitle()
        {
            skipIntroOnce = false;
            // Returning to the title abandons the campaign advance, so a cutscene armed for a
            // stage the player is no longer entering must not fire on the title's own boot.
            pendingStageInterlude = false;
            PendingStage = StageId.Stage1;
            ResetSeries();
            SiegePrototypeEconomy.ResetDemo();
            ReloadArena();
        }


        /// <summary>
        /// Intro-screen stage picker entry: PendingStage is a plain static field (no
        /// runtime world-rebuild path exists — layout is baked once in Start()), so
        /// switching stages reloads the scene same as Title/Rematch. skipIntroOnce stays
        /// false so the fresh boot re-shows the intro (now on the new stage's diorama)
        /// instead of dropping straight into play. A stage must clear BOTH gates to be
        /// selectable: StageDefinitions.For(stage).locked (design-time "not finished/
        /// offered yet") and StageProgress.IsUnlocked (sequential campaign — Stage2/3
        /// require clearing the stage right before them at least once). The intro
        /// picker already renders either kind of locked card non-interactive, but this
        /// is the authoritative guard so no other future caller can route live gameplay
        /// through an unfinished or not-yet-earned layout.
        /// </summary>
        /// <param name="skipIntro">True routes the fresh boot straight into gameplay
        /// (results-screen "다음 스테이지" button / auto-advance — same UX as Rematch, no
        /// redundant intro card between clearing one stage and starting the next); false
        /// (default) shows the intro on the new stage (intro screen's stage picker use
        /// case).</param>
        /// <returns>
        /// True when the request was accepted and a scene reload is now scheduled. False
        /// means a guard refused it — the caller MUST NOT latch itself as "navigated", or
        /// the player is stranded on a screen whose button silently did nothing.
        /// </returns>
        public static bool RequestStage(StageId stage, bool skipIntro = false)
        {
            if (StageDefinitions.For(stage).locked) return false;
            if (!StageProgress.IsUnlocked(StageProgressStore.Load(), stage)) return false;

            // NOTE: deliberately no `PendingStage == stage` early-out. That guard used to
            // treat "already the pending stage" as "nothing to do", but PendingStage only
            // records which layout the NEXT boot should build — it says nothing about
            // whether the caller still needs the reload. Three callers do:
            //   * the results-screen NEXT STAGE button and its auto-advance, which also need
            //     skipIntroOnce + ResetSeries applied, and
            //   * the intro picker, where re-selecting the currently pending stage was a
            //     visibly interactive card that did nothing at all.
            // Refusing there stranded the player on the results screen with the countdown
            // already stopped (the caller had latched `navigated`), which is exactly the
            // "다음 스테이지로 넘어가지 않는다" report.
            // Arm the connective cutscene only when the campaign actually MOVES and the
            // caller is skipping the title (the results-screen advance). Re-picking a stage
            // from the intro shows the title card anyway, and a rematch of the same stage
            // must never put a cutscene between the player and their retry.
            pendingStageInterlude = StageInterlude.ShouldPlayOnEntry(stage, PendingStage, skipIntro);
            PendingStage = stage;
            skipIntroOnce = skipIntro;
            ResetSeries();
            ReloadArena();
            return true;
        }

        private static void ReloadArena()
        {
            Time.timeScale = 1f;
            GameplayUxDirector.ResetSessionStats();
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void SpawnInitialUnits()
        {
            SpawnUnitsOnCastle(playerCastle, true);
            SpawnUnitsOnCastle(enemyCastle, false);
        }

        private void SpawnUnitsOnCastle(CastleController castle, bool isPlayer)
        {
            if (castle == null) return;
            var blocks = castle.GetComponentsInChildren<DestructibleBlock>();
            if (blocks.Length == 0) return;

            var topBlocks = blocks
                .GroupBy(b => Mathf.RoundToInt(b.transform.position.x))
                .Select(g => g.OrderByDescending(b => b.transform.position.y).First())
                .OrderBy(b => b.transform.position.x)
                .ToList();

            int count = Mathf.Min(3, topBlocks.Count);
            // Garrison composition: Knight/Archer only. The Bomber slot it used to cycle
            // through was removed with the deployment overhaul, and the Cannon is a paid
            // deploy — handing every side two free batteries at match start would erase the
            // supply cost that makes siting one a decision.
            var prefabs = new GameObject[] { knightPrefab, archerPrefab };

            for (int i = 0; i < count; i++)
            {
                int blockIndex = (i * topBlocks.Count) / count;
                var block = topBlocks[blockIndex];
                Vector3 spawnPos = block.transform.position + new Vector3(0f, castle.blockSizeY / 2f + 0.8f, 0f);

                var prefab = prefabs[i % prefabs.Length];
                if (prefab != null)
                {
                    var unitGo = Instantiate(prefab, spawnPos, Quaternion.identity);
                    var unit = unitGo.GetComponent<UnitController>();
                    if (unit != null)
                    {
                        unit.InitializeUnit(isPlayer, UnitState.Grounded);
                        activeUnits.Add(unit);
                    }
                }
            }
        }
    }
    internal sealed class GameButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 originalScale;
        private UnityEngine.UI.Button button;
        private bool isHovered;
        private bool isPressed;

        private void Awake()
        {
            originalScale = transform.localScale;
            button = GetComponent<UnityEngine.UI.Button>();
        }

        private void OnDisable()
        {
            transform.localScale = originalScale;
            isHovered = false;
            isPressed = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanAnimate()) return;
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
            transform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!CanAnimate()) return;
            isPressed = true;
            transform.localScale = originalScale * 0.92f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (!CanAnimate()) return;
            isPressed = false;
            transform.localScale = isHovered ? originalScale * 1.08f : originalScale;
        }

        private void Update()
        {
            if (!isHovered || isPressed || !CanAnimate()) return;
            float pulse = 1.04f + Mathf.Sin(Time.unscaledTime * 10f) * 0.04f;
            transform.localScale = originalScale * pulse;
        }

        private bool CanAnimate()
        {
            return button == null || button.interactable;
        }
    }
}
