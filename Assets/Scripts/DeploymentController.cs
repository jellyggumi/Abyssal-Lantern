using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Mid-battle creation (전투 중 생성) — design/deployment-economy.md.
    ///
    /// Owns the Supply resource for both sides, the per-card cooldown clocks, the player's
    /// click-to-place flow, and the AI's mirror of it. Supply accrues in REAL TIME on both
    /// turns, so the enemy turn stops being dead air: the player can reinforce the line or
    /// site a cannon while the AI aims.
    ///
    /// All conditions live in <see cref="DeploymentRules"/>/<see cref="SupplyRules"/> (pure,
    /// EditMode-pinned); this component only reads scene state and applies the verdict.
    /// </summary>
    public class DeploymentController : MonoBehaviour
    {
        public static DeploymentController Instance { get; private set; }

        /// <summary>True while the player has armed deploy mode (click places instead of aims).</summary>
        public bool DeployModeArmed { get; private set; }
        public DeployCard SelectedCard { get; private set; } = DeployCard.Knight;

        public float PlayerSupply { get; private set; } = SupplyRules.StartSupply;
        public float EnemySupply { get; private set; } = SupplyRules.StartSupply;

        private readonly float[] playerCooldowns = new float[4];
        private readonly float[] enemyCooldowns = new float[4];

        private GameObject ghost;
        private SpriteRenderer ghostRenderer;
        private LineRenderer zoneLine;
        private TextMeshProUGUI supplyText;
        private UnityEngine.UI.Image supplyFill;
        private UnityEngine.UI.Button deployToggleButton;
        private TextMeshProUGUI deployToggleLabel;
        private float aiThinkTimer;
        private bool hintShown;
        private const float HudRefreshInterval = 0.1f;
        private float hudRefreshTimer;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        private void OnEnable() { if (Instance == null) Instance = this; }
        private void OnDestroy() { if (Instance == this) Instance = null; }

        /// <summary>Rematch hygiene: both economies restart at the documented opening float.</summary>
        public void ResetEconomy()
        {
            PlayerSupply = SupplyRules.StartSupply;
            EnemySupply = SupplyRules.StartSupply;
            for (int i = 0; i < playerCooldowns.Length; i++) { playerCooldowns[i] = 0f; enemyCooldowns[i] = 0f; }
            DisarmDeployMode();
        }

        public float SupplyOf(bool isPlayer) => isPlayer ? PlayerSupply : EnemySupply;

        public float CooldownOf(DeployCard card, bool isPlayer) =>
            (isPlayer ? playerCooldowns : enemyCooldowns)[(int)card];

        /// <summary>
        /// Live population of the card's cap group on the given side. Counts EVERY live body
        /// of that group — launched or deployed — so the two creation verbs share one army
        /// ceiling instead of doubling it.
        /// </summary>
        public static int AliveInGroup(DeployCard card, bool isPlayer)
        {
            var group = DeploymentRules.GroupOf(card);
            if (group == DeployCapGroup.Battery) return CannonController.CountFor(isPlayer);

            var units = UnitController.ActiveOrScene;
            int n = 0;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || u.isPlayerUnit != isPlayer || u.CurrentState == UnitState.Dead) continue;
                var uGroup = u.unitType == UnitType.Barrel ? DeployCapGroup.Hazard
                    : u.unitType == UnitType.Cannon ? DeployCapGroup.Battery
                    : DeployCapGroup.Body;
                if (uGroup == group) n++;
            }
            return n;
        }

        // ---- Supply credits (called from the death/collapse hooks) ----

        /// <summary>A kill pays the KILLER's side (design §3: the volley funds the deploy).</summary>
        public void CreditKill(bool victimWasPlayerUnit)
        {
            CreditSide(!victimWasPlayerUnit, SupplyRules.KillBonus);
        }

        /// <summary>Collapsing an enemy block pays the side that owns the opposite castle.</summary>
        /// <summary>Enemy keep blocks the player has brought down. Drives the battery's
        /// breach unlock.</summary>
        public int PlayerBreaches { get; private set; }

        /// <summary>Player keep blocks the enemy has brought down.</summary>
        public int EnemyBreaches { get; private set; }

        public int BreachesFor(bool isPlayer) => isPlayer ? PlayerBreaches : EnemyBreaches;

        /// <param name="blockWasCore">The core is the objective, not a wall; breaching it
        /// ends the match, so it must not also count toward unlocking artillery.</param>
        public void CreditBlockDestroyed(bool blockBelongedToPlayer, bool blockWasCore = false)
        {
            CreditSide(!blockBelongedToPlayer, SupplyRules.BlockBonus);

            if (blockWasCore) return;

            // Counted by whose wall fell, never by who fired. That is what closes the
            // obvious loophole: demolishing your own keep credits the opponent's tally,
            // so it can never unlock your own battery.
            if (blockBelongedToPlayer) EnemyBreaches++;
            else PlayerBreaches++;
        }

        /// <summary>Clears the tallies for a fresh match.</summary>
        public void ResetBreaches()
        {
            PlayerBreaches = 0;
            EnemyBreaches = 0;
        }

        private void CreditSide(bool isPlayer, float amount)
        {
            if (isPlayer) PlayerSupply = SupplyRules.Credit(PlayerSupply, amount);
            else EnemySupply = SupplyRules.Credit(EnemySupply, amount);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && gm.EnforcesOneShotTurns)
            {
                // One-shot loop: the volley itself is rule-driven, but the CANNON is an
                // installation, not a launch — it remains the one thing a turn may buy
                // INSTEAD of its shot. Placing it consumes the turn (TryDeploy commits the
                // one-shot gate), so "one action per turn" holds: fire, or emplace artillery.
                SelectedCard = DeployCard.Cannon;
                bool playerCanAct = gm.currentState == GameState.PlayerTurn
                    && gm.IsPlayerTurn && !gm.IsResolvingTurn;

                EnsureHud();
                SetHudVisible(playerCanAct);
                if (!playerCanAct)
                {
                    DisarmDeployMode();
                    return;
                }

                float oneShotDt = Time.deltaTime;
                PlayerSupply = SupplyRules.Regen(PlayerSupply, oneShotDt);
                for (int i = 0; i < playerCooldowns.Length; i++)
                {
                    playerCooldowns[i] = Mathf.Max(0f, playerCooldowns[i] - oneShotDt);
                }

                HandlePlayerInput();
                UpdateGhost();
                hudRefreshTimer -= Time.unscaledDeltaTime;
                if (hudRefreshTimer <= 0f)
                {
                    hudRefreshTimer = HudRefreshInterval;
                    UpdateHud();
                }
                // No AI deployment in the one-shot loop: the enemy turn is its shot.
                return;
            }
            bool battleLive = gm != null &&
                (gm.currentState == GameState.PlayerTurn || gm.currentState == GameState.AITurn);

            EnsureHud();
            if (!battleLive)
            {
                SetHudVisible(false);
                DisarmDeployMode();
                return;
            }
            SetHudVisible(true);

            float dt = Time.deltaTime;
            PlayerSupply = SupplyRules.Regen(PlayerSupply, dt);
            EnemySupply = SupplyRules.Regen(EnemySupply, dt);
            for (int i = 0; i < playerCooldowns.Length; i++)
            {
                playerCooldowns[i] = Mathf.Max(0f, playerCooldowns[i] - dt);
                enemyCooldowns[i] = Mathf.Max(0f, enemyCooldowns[i] - dt);
            }

            if (!hintShown)
            {
                hintShown = true;
                GameFeelVfx.SpawnFeedbackLabel(new Vector3(0f, 5.6f, 0f),
                    "D 키 / 배치 버튼 → 전투 중 병력·대포 설치",
                    new Color(0.8f, 0.95f, 1f, 0.95f), 2.4f, 1.0f);
            }

            HandlePlayerInput();
            UpdateGhost();
            hudRefreshTimer -= Time.unscaledDeltaTime;
            if (hudRefreshTimer <= 0f)
            {
                hudRefreshTimer = HudRefreshInterval;
                UpdateHud();
            }
            TickAi(dt);
        }

        // ---- Player flow ----

        private void HandlePlayerInput()
        {
            if (Input.GetKeyDown(KeyCode.D)) ToggleDeployMode();
            if (Input.GetKeyDown(KeyCode.Escape) && DeployModeArmed) DisarmDeployMode();
            if (!DeployModeArmed) return;

            if (!Input.GetMouseButtonDown(0)) return;
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            var cam = Camera.main;
            if (cam == null) return;
            Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            TryDeploy(SelectedCard, worldPos, true);
        }

        public void ToggleDeployMode()
        {
            if (DeployModeArmed) DisarmDeployMode(); else ArmDeployMode();
        }

        public void ArmDeployMode()
        {
            DeployModeArmed = true;
            // Deploy and aim are mutually exclusive: an armed placement click must never also
            // start drawing the sling, or the player pays supply AND burns their volley.
            var lm = FindObjectOfType<LaunchManager>();
            if (lm != null) lm.CancelAim();
            // Names the consequence, not the mode: an armed click SPENDS the turn, and the
            // player must be told how to back out before they find out by spending it.
            SiegeAlarmSystem.Post(
                $"{DeploymentRules.DisplayName(SelectedCard)} 설치 — 전장을 클릭 (턴 소모) · Esc 취소",
                new Color(1f, 0.84f, 0.3f, 1f));
            UpdateHud(); // armed state changes what a click means: never wait on the throttle
        }

        public void DisarmDeployMode()
        {
            bool wasArmed = DeployModeArmed;
            DeployModeArmed = false;
            if (ghost != null) ghost.SetActive(false);
            if (zoneLine != null) zoneLine.enabled = false;
            if (wasArmed) UpdateHud();
        }

        /// <summary>GameManager selection hook: the roster card also selects what deploy places.</summary>
        public void SetSelectedCard(DeployCard card)
        {
            SelectedCard = card;
            // The Cannon cannot be launched, so selecting it arms deploy mode outright —
            // otherwise the card would look selected while every drag silently did nothing.
            if (DeploymentRules.IsDeployOnly(card)) ArmDeployMode();
        }

        /// <summary>
        /// Full deploy attempt. Returns the blocking reason (None on success). Public so the
        /// AI, the HUD, and PlayMode tests all exercise the exact same path.
        /// </summary>
        public DeployBlockReason TryDeploy(DeployCard card, Vector2 position, bool isPlayer)
        {
            var gm = GameManager.Instance;
            int turn = gm != null ? gm.TurnCount : 0;

            var reason = DeploymentRules.Evaluate(
                card, turn, AliveInGroup(card, isPlayer), CooldownOf(card, isPlayer),
                SupplyOf(isPlayer), position, isPlayer);

            // Breach gate sits alongside the turn gate rather than inside Evaluate: the
            // rule needs match state (how much of the enemy keep has fallen) that the pure
            // rules table has no business holding.
            if (reason == DeployBlockReason.None &&
                !DeploymentRules.BreachSatisfied(card, BreachesFor(isPlayer)))
            {
                reason = DeployBlockReason.Locked;
            }

            if (reason == DeployBlockReason.None && OverlapsEnemyBody(position, isPlayer))
            {
                reason = DeployBlockReason.Zone;
            }

            if (reason != DeployBlockReason.None)
            {
                if (isPlayer)
                {
                    // Name the condition the player actually has to solve. A turn-gated card
                    // and a breach-gated one are both "Locked", but telling someone to wait
                    // when they need to knock a wall down sends them the wrong way.
                    string text = DeploymentRules.NeedsBreach(card) &&
                                  !DeploymentRules.BreachSatisfied(card, BreachesFor(true))
                        ? DeploymentRules.BreachReasonText(BreachesFor(true))
                        : DeploymentRules.ReasonText(reason, card, turn);

                    GameFeelVfx.SpawnFeedbackLabel(position, text,
                        new Color(1f, 0.5f, 0.35f, 1f), 1.7f, 0.45f);
                }
                return reason;
            }

            float cost = DeploymentRules.CostOf(card);
            if (!SupplyRules.TrySpend(SupplyOf(isPlayer), cost, out float remaining))
            {
                return DeployBlockReason.Supply;
            }

            // One-shot loop: an installation IS the turn's action. Commit the one-shot gate
            // BEFORE spending supply or spawning, so a turn that has already fired cannot
            // also emplace, and a placement can never be paid for without owning the turn.
            bool consumesOneShotTurn = isPlayer && gm != null && gm.EnforcesOneShotTurns;
            if (consumesOneShotTurn && !gm.TryCommitTurnShot())
            {
                return DeployBlockReason.Cooldown;
            }

            if (!SpawnDeployed(card, position, isPlayer))
            {
                return DeployBlockReason.Locked;
            }

            if (isPlayer)
            {
                PlayerSupply = remaining;
                playerCooldowns[(int)card] = DeploymentRules.CooldownOf(card);
            }
            else
            {
                EnemySupply = remaining;
                enemyCooldowns[(int)card] = DeploymentRules.CooldownOf(card);
            }

            if (consumesOneShotTurn)
            {
                // The emplacement was this turn's action: resolve and hand over exactly as
                // a launch does (OnUnitLaunched tolerates null — nothing is in flight).
                DisarmDeployMode();
                GameFeelVfx.SpawnFeedbackLabel(position + Vector2.up * 0.9f, "화포 설치 — 턴 종료",
                    new Color(0.7f, 0.95f, 1f, 1f), 1.7f, 0.45f);
                gm.OnUnitLaunched(null);
            }
            return DeployBlockReason.None;
        }

        private static bool OverlapsEnemyBody(Vector2 position, bool deployerIsPlayer)
        {
            var hits = Physics2D.OverlapCircleAll(position, DeploymentRules.EnemyOverlapRadius);
            for (int i = 0; i < hits.Length; i++)
            {
                var unit = hits[i] != null ? hits[i].GetComponent<UnitController>() : null;
                if (unit != null && unit.isPlayerUnit != deployerIsPlayer && unit.CurrentState != UnitState.Dead)
                {
                    return true;
                }
            }
            return false;
        }

        private bool SpawnDeployed(DeployCard card, Vector2 position, bool isPlayer)
        {
            GameObject spawned = card == DeployCard.Cannon
                ? SpawnCannon(position, isPlayer)
                : SpawnFromPrefab(card, position, isPlayer);

            if (spawned == null) return false;

            Color team = isPlayer ? new Color(0.55f, 0.9f, 1f, 1f) : new Color(1f, 0.55f, 0.4f, 1f);
            FrameAnimEffect.Spawn("fx_spawn", position, 1.6f, new Color(1f, 0.96f, 0.9f, 0.95f), 14f, 40);
            GameFeelVfx.SpawnShockwaveRing(position, team, 1.15f, 0.3f);
            GameFeelVfx.SpawnFeedbackLabel(position + Vector2.up * 0.7f,
                $"{DeploymentRules.DisplayName(card)} 배치!", team, 1.9f, 0.5f);
            if (isPlayer)
            {
                SiegeAlarmSystem.Post($"{DeploymentRules.DisplayName(card)} 배치 (−{DeploymentRules.CostOf(card):0} 보급)", team);
            }
            return true;
        }

        private GameObject SpawnFromPrefab(DeployCard card, Vector2 position, bool isPlayer)
        {
            var gm = GameManager.Instance;
            GameObject prefab = null;
            if (gm != null)
            {
                prefab = card == DeployCard.Knight ? gm.knightPrefab
                    : card == DeployCard.Archer ? gm.archerPrefab
                    : gm.explosiveBarrelPrefab;
            }
            if (prefab == null) return null;

            var go = Instantiate(prefab, position, Quaternion.identity);
            UnitController.SnapColliderAboveGround(go, position.y);

            var unit = go.GetComponent<UnitController>();
            if (unit == null && go.GetComponent<ExplosiveGimmick>() != null)
            {
                unit = go.AddComponent<UnitController>();
                unit.unitType = UnitType.Barrel;
                unit.maxHP = 20f;
                unit.currentHP = 20f;
            }
            if (unit != null)
            {
                unit.isPlayerUnit = isPlayer;
                go.GetComponent<ExplosiveGimmick>()?.ApplyTeamTint(isPlayer);
                // Deployed, not launched: the body starts on the ground already fighting,
                // which is the whole point of the second creation verb.
                unit.DeployGrounded();
                if (!isPlayer && gm != null) gm.RegisterAIUnit(unit);
            }
            return go;
        }

        private GameObject SpawnCannon(Vector2 position, bool isPlayer)
        {
            var go = new GameObject(isPlayer ? "PlayerCannon" : "EnemyCannon");
            go.transform.position = position;

            var body = go.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;

            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.95f, 0.75f);

            var unit = go.AddComponent<UnitController>();
            // UnitController.Awake runs immediately with the enum's default Knight identity:
            // undo its character-only root scaling and presentation animator before the
            // installation is configured. CannonController builds its own fixed-size visuals.
            go.transform.localScale = Vector3.one;
            box.size = new Vector2(0.95f, 0.75f);
            box.offset = Vector2.zero;
            var characterAnimator = go.GetComponent<UnitSpriteAnimator>();
            if (characterAnimator != null)
            {
                characterAnimator.enabled = false;
                if (Application.isPlaying) Destroy(characterAnimator);
                else DestroyImmediate(characterAnimator);
            }
            unit.unitType = UnitType.Cannon;
            unit.isPlayerUnit = isPlayer;
            unit.maxHP = CannonRules.MaxHP;
            unit.currentHP = CannonRules.MaxHP;
            unit.attackDamage = CannonRules.ShellDamage;
            unit.attackRange = CannonRules.Range;

            go.AddComponent<CannonController>();
            unit.DeployGrounded();
            unit.MakeStationaryInstallation();
            if (!isPlayer) GameManager.Instance?.RegisterAIUnit(unit);
            return go;
        }

        // ---- AI mirror ----

        private void TickAi(float dt)
        {
            aiThinkTimer -= dt;
            if (aiThinkTimer > 0f) return;
            aiThinkTimer = 1.25f;

            var gm = GameManager.Instance;
            int turn = gm != null ? gm.TurnCount : 0;

            var card = DeployCard.Knight;
            bool found = false;
            foreach (var candidate in DeploymentRules.AiPreferenceOrder)
            {
                if (!DeploymentRules.IsUnlocked(candidate, turn)) continue;
                if (CooldownOf(candidate, false) > 0f) continue;
                if (AliveInGroup(candidate, false) >= DeploymentRules.CapFor(candidate)) continue;
                if (EnemySupply + 1e-4f < DeploymentRules.CostOf(candidate)) continue;
                card = candidate;
                found = true;
                break;
            }
            if (!found) return;

            // Site it on the enemy half, spread across the band so batteries do not stack.
            for (int attempt = 0; attempt < 6; attempt++)
            {
                var spot = new Vector2(
                    Random.Range(2.5f, DeploymentRules.MaxAbsX - 1.5f),
                    card == DeployCard.Cannon ? 0.6f : 0.5f);
                if (TryDeploy(card, spot, false) == DeployBlockReason.None) return;
            }
        }

        // ---- Presentation ----

        private void UpdateGhost()
        {
            if (!DeployModeArmed)
            {
                if (ghost != null) ghost.SetActive(false);
                if (zoneLine != null) zoneLine.enabled = false;
                return;
            }

            var cam = Camera.main;
            if (cam == null) return;
            Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);

            EnsureGhost();
            ghost.SetActive(true);
            ghost.transform.position = worldPos;

            var gm = GameManager.Instance;
            int turn = gm != null ? gm.TurnCount : 0;
            var reason = DeploymentRules.Evaluate(SelectedCard, turn,
                AliveInGroup(SelectedCard, true), CooldownOf(SelectedCard, true),
                PlayerSupply, worldPos, true);

            if (reason == DeployBlockReason.None &&
                !DeploymentRules.BreachSatisfied(SelectedCard, PlayerBreaches))
            {
                // The placement ghost must agree with the click, or the player learns the
                // rule by being refused rather than by looking.
                reason = DeployBlockReason.Locked;
            }

            ghostRenderer.color = reason == DeployBlockReason.None
                ? new Color(0.55f, 1f, 0.7f, 0.55f)
                : new Color(1f, 0.35f, 0.3f, 0.45f);

            DrawZoneBand();
        }

        private void EnsureGhost()
        {
            if (ghost != null) return;
            ghost = new GameObject("DeployGhost");
            ghostRenderer = ghost.AddComponent<SpriteRenderer>();
            ghostRenderer.sortingOrder = 10;

            var tex = new Texture2D(24, 24);
            tex.filterMode = FilterMode.Point;
            for (int y = 0; y < 24; y++)
            {
                for (int x = 0; x < 24; x++)
                {
                    bool edge = x < 2 || y < 2 || x > 21 || y > 21;
                    tex.SetPixel(x, y, edge ? Color.white : new Color(1f, 1f, 1f, 0.25f));
                }
            }
            tex.Apply();
            ghostRenderer.sprite = Sprite.Create(tex, new Rect(0, 0, 24, 24), new Vector2(0.5f, 0.5f), 24f);
        }

        /// <summary>Draws the legal player band so "where can I place" needs no tooltip.</summary>
        private void DrawZoneBand()
        {
            if (zoneLine == null)
            {
                var go = new GameObject("DeployZoneBand");
                go.transform.SetParent(transform, false);
                zoneLine = go.AddComponent<LineRenderer>();
                zoneLine.material = new Material(Shader.Find("Sprites/Default"));
                zoneLine.startWidth = 0.07f;
                zoneLine.endWidth = 0.07f;
                zoneLine.sortingOrder = 9;
                zoneLine.loop = true;
                zoneLine.positionCount = 4;
                zoneLine.startColor = new Color(0.5f, 0.95f, 1f, 0.5f);
                zoneLine.endColor = new Color(0.5f, 0.95f, 1f, 0.2f);
            }
            zoneLine.enabled = true;
            float nearX = -DeploymentRules.MinAbsX;
            float farX = -Mathf.Min(DeploymentRules.MaxAbsX,
                Mathf.Abs(LaunchRingRules.PlayerRingX) - LaunchRingRules.RingRadius);
            zoneLine.SetPosition(0, new Vector3(farX, DeploymentRules.MinY, 0f));
            zoneLine.SetPosition(1, new Vector3(nearX, DeploymentRules.MinY, 0f));
            zoneLine.SetPosition(2, new Vector3(nearX, DeploymentRules.MaxY, 0f));
            zoneLine.SetPosition(3, new Vector3(farX, DeploymentRules.MaxY, 0f));
        }

        private void EnsureHud()
        {
            if (supplyText != null) return;
            var canvas = HudCanvas.Resolve();
            if (canvas == null) return;

            var panel = new GameObject("SupplyPanel");
            panel.transform.SetParent(MobileSafeArea.GetContentRoot(canvas), false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0f, 1f);
            panelRt.anchorMax = new Vector2(0f, 1f);
            panelRt.pivot = new Vector2(0f, 1f);
            panelRt.anchoredPosition = new Vector2(18f, -104f);
            // 226 -> 300 wide. "보급 9/24  ·  3턴 해금" is a single line that did not fit 226
            // units at this size, so TMP wrapped it and the second line landed on the deploy
            // toggle below (`qa/evidence/hud-fix/hud-overlap.md`). The friendly core badge now
            // starts at x 308 in canvas units, so 300 stays clear of it.
            panelRt.sizeDelta = new Vector2(300f, 34f);

            var bg = panel.AddComponent<UnityEngine.UI.Image>();
            bg.color = new Color(0.05f, 0.07f, 0.1f, 0.72f);

            var fillGo = new GameObject("SupplyFill");
            fillGo.transform.SetParent(panel.transform, false);
            supplyFill = fillGo.AddComponent<UnityEngine.UI.Image>();
            supplyFill.color = new Color(0.35f, 0.85f, 1f, 0.85f);
            var fillRt = fillGo.GetComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(1f, 1f);
            fillRt.offsetMin = new Vector2(2f, 2f);
            fillRt.offsetMax = new Vector2(-2f, -2f);
            supplyFill.type = UnityEngine.UI.Image.Type.Filled;
            supplyFill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;

            var textGo = new GameObject("SupplyText");
            textGo.transform.SetParent(panel.transform, false);
            supplyText = textGo.AddComponent<TextMeshProUGUI>();
            supplyText.fontSize = HudCanvas.PrimaryLabelSize;
            supplyText.alignment = TextAlignmentOptions.Center;
            // A supply readout that wraps costs more than one that runs a little long: the
            // second line lands on whatever is beneath it.
            supplyText.enableWordWrapping = false;
            supplyText.overflowMode = TextOverflowModes.Overflow;
            supplyText.color = new Color(0.97f, 0.99f, 1f, 1f);
            supplyText.outlineWidth = 0.18f;
            supplyText.outlineColor = new Color(0.02f, 0.02f, 0.03f, 0.95f);
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            // Parented to the SAFE-AREA content root, not the raw canvas (2026-08-13):
            // MobileSafeArea insets that root, so a button pinned to the canvas sat outside
            // the inset band — it never appeared in any live capture, which left the D key
            // as the battery's only entrance and nothing on screen said the key existed.
            var hudRoot = MobileSafeArea.GetContentRoot(canvas);
            var buttonGo = new GameObject("DeployToggleButton");
            buttonGo.transform.SetParent(hudRoot, false);
            var buttonRt = buttonGo.AddComponent<RectTransform>();
            buttonRt.anchorMin = new Vector2(0f, 1f);
            buttonRt.anchorMax = new Vector2(0f, 1f);
            buttonRt.pivot = new Vector2(0f, 1f);
            // Both sides widened this to 300 independently — main hit the same clipping.
            // The vertical is where they differ, and it is not a taste call: the supply panel
            // above is 34 tall at 26pt (auto-merged from this branch), so main's -134 leaves a
            // 30-unit gap for glyphs that stand ~34, which is exactly the 74% overlap measured
            // in `qa/evidence/hud-fix/hud-overlap.md`. -152 keeps the gap at 48.
            buttonRt.anchoredPosition = new Vector2(18f, -152f);
            buttonRt.sizeDelta = new Vector2(300f, 34f);
            var buttonImage = buttonGo.AddComponent<UnityEngine.UI.Image>();
            buttonImage.color = new Color(0.1f, 0.14f, 0.2f, 0.85f);
            deployToggleButton = buttonGo.AddComponent<UnityEngine.UI.Button>();
            deployToggleButton.targetGraphic = buttonImage;
            deployToggleButton.onClick.AddListener(ToggleDeployMode);

            var labelGo = new GameObject("DeployToggleLabel");
            labelGo.transform.SetParent(buttonGo.transform, false);
            deployToggleLabel = labelGo.AddComponent<TextMeshProUGUI>();
            deployToggleLabel.fontSize = HudCanvas.SecondaryLabelSize;
            deployToggleLabel.alignment = TextAlignmentOptions.Center;
            deployToggleLabel.enableWordWrapping = false;
            deployToggleLabel.overflowMode = TextOverflowModes.Overflow;
            deployToggleLabel.color = new Color(0.85f, 0.95f, 1f, 1f);
            // Measured 0.00 on 2026-08-19; the other HUD labels carry 0.18. Pale blue on a bright
            // sky is the worst case for a label with no outline, and this one sits over the board.
            HudCanvas.TryApplyOutline(deployToggleLabel, 0.18f, new Color(0.06f, 0.07f, 0.10f, 1f));
            var labelRt = labelGo.GetComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
        }

        private void SetHudVisible(bool visible)
        {
            if (supplyText != null && supplyText.transform.parent != null)
            {
                supplyText.transform.parent.gameObject.SetActive(visible);
            }
            if (deployToggleButton != null) deployToggleButton.gameObject.SetActive(visible);
        }

        private void UpdateHud()
        {
            var gm = GameManager.Instance;
            int turn = gm != null ? gm.TurnCount : 0;

            if (supplyFill != null) supplyFill.fillAmount = PlayerSupply / SupplyRules.MaxSupply;
            if (supplyText != null)
            {
                float cost = DeploymentRules.CostOf(SelectedCard);
                float cd = CooldownOf(SelectedCard, true);
                string state = !DeploymentRules.IsUnlocked(SelectedCard, turn)
                    ? $"{DeploymentRules.UnlockTurn(SelectedCard)}턴 해금"
                    : cd > 0f ? $"{cd:0.0}s"
                    : $"{DeploymentRules.DisplayName(SelectedCard)} {cost:0}";
                supplyText.text = $"보급 {PlayerSupply:0}/{SupplyRules.MaxSupply:0}  ·  {state}";
            }
            // Always recomputed — supply regenerates and breaches land continuously, so a
            // change-gated label would keep showing a stale gate. The old version only
            // refreshed on the armed-mode edge and read `배치 모드 ON/OFF`, which named an
            // internal mode instead of the action, its cost, or the gate blocking it.
            if (deployToggleLabel != null)
            {
                var gmRef = GameManager.Instance;
                bool playerCanAct = gmRef != null
                    && gmRef.currentState == GameState.PlayerTurn
                    && gmRef.IsPlayerTurn
                    && !gmRef.IsResolvingTurn;

                var prompt = TurnActionPrompt.ForCannon(
                    playerCanAct,
                    DeployModeArmed,
                    turn,
                    PlayerBreaches,
                    PlayerSupply,
                    CooldownOf(DeployCard.Cannon, true));

                deployToggleLabel.text = prompt.label;
                deployToggleLabel.color = TurnActionPrompt.ColorFor(prompt.tone);
                if (deployToggleButton != null) deployToggleButton.interactable = prompt.interactable;
            }
        }
    }
}
