using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace CastleBusters
{
    public class PendingBrickInfo : MonoBehaviour
    {
        public BrickPlacementController.SelectedBlockType blockType;
    }

    /// <summary>
    /// Pre-designated player builds: during the ENEMY turn the player clicks the field to
    /// mark up to <see cref="BrickPlacementRules.MaxPendingBricks"/> brick sites (ghost
    /// previews); the stone bricks materialize the moment the player's own turn begins.
    /// Placement validation (launch-ring/unit-spawn exclusion, field band) lives in
    /// BrickPlacementRules so EditMode tests pin it. Clicking an existing ghost removes it.
    /// </summary>
    public class BrickPlacementController : MonoBehaviour
    {
        public static BrickPlacementController Instance { get; private set; }

        public enum SelectedBlockType { Wood, Stone, Iron }
        public SelectedBlockType selectedBlockType = SelectedBlockType.Stone;

        private GameObject blockUIPanel;
        private UnityEngine.UI.Button woodBtn;
        private UnityEngine.UI.Button stoneBtn;
        private UnityEngine.UI.Button ironBtn;
        private TextMeshProUGUI woodText;
        private TextMeshProUGUI stoneText;
        private TextMeshProUGUI ironText;

        private readonly List<GameObject> ghosts = new List<GameObject>();
        private bool hintShownThisTurn;

        public int PendingCount
        {
            get
            {
                ghosts.RemoveAll(g => g == null);
                return ghosts.Count;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            if (Instance == null) Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (blockUIPanel != null) Destroy(blockUIPanel);
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                if (blockUIPanel != null) blockUIPanel.SetActive(false);
                return;
            }

            if (gm.EnforcesOneShotTurns)
            {
                // Reserving bricks is a placement action, so it is unavailable in the
                // aim-once/fire-once loop just like roster deployment.
                if (blockUIPanel != null) blockUIPanel.SetActive(false);
                return;
            }

            // Create UI dynamically if it doesn't exist
            if (blockUIPanel == null)
            {
                CreateBlockUI();
            }

            if (blockUIPanel != null)
            {
                blockUIPanel.SetActive(gm.currentState == GameState.AITurn);
            }

            // Designation window: the OPPONENT's turn only.
            if (gm.currentState != GameState.AITurn)
            {
                hintShownThisTurn = false;
                return;
            }

            // Deploy mode owns the click while armed. Both systems listen for the same
            // left-click during the ENEMY turn, so without this guard one click would BOTH
            // deploy a unit and designate a brick — the player pays supply and silently
            // burns a brick slot (design/deployment-economy.md §2: the two placement verbs
            // are mutually exclusive).
            bool deployArmed = DeploymentController.Instance != null && DeploymentController.Instance.DeployModeArmed;

            // One predicate, both surfaces. The HUD strip asks BrickPlacementRules the same
            // question before it offers "클릭: 벽돌 예약", so the instruction and the gate cannot
            // drift apart — which is exactly how the screen came to promise a click this method
            // was already refusing (`design/visibility-spec-v2.md` §5-A).
            if (!BrickPlacementRules.DesignationOpen(gm.EnforcesOneShotTurns, true, deployArmed)) return;

            if (!hintShownThisTurn)
            {
                hintShownThisTurn = true;
                GameFeelVfx.SpawnFeedbackLabel(new Vector3(0f, 4.5f, 0f),
                    $"클릭: 벽돌 배치 지정 (최대 {BrickPlacementRules.MaxPendingBricks})",
                    new Color(0.75f, 0.9f, 1f, 0.95f), 2.2f, 0.9f);
            }

            if (!Input.GetMouseButtonDown(0)) return;
            if (UnityEngine.EventSystems.EventSystem.current != null &&
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

            var cam = Camera.main;
            if (cam == null) return;
            Vector2 worldPos = cam.ScreenToWorldPoint(Input.mousePosition);

            // Clicking near an existing ghost cancels that designation.
            ghosts.RemoveAll(g => g == null);
            var hitGhost = ghosts.Find(g => Vector2.Distance(g.transform.position, worldPos) < 0.7f);
            if (hitGhost != null)
            {
                ghosts.Remove(hitGhost);
                Destroy(hitGhost);
                GameFeelVfx.SpawnFeedbackLabel(worldPos, "지정 취소", new Color(1f, 0.8f, 0.4f, 1f), 1.5f, 0.4f);
                return;
            }

            if (ghosts.Count >= BrickPlacementRules.MaxPendingBricks)
            {
                GameFeelVfx.SpawnFeedbackLabel(worldPos, "배치 한도 초과", new Color(1f, 0.5f, 0.3f, 1f), 1.6f, 0.4f);
                return;
            }

            if (!BrickPlacementRules.CanPlace(worldPos))
            {
                GameFeelVfx.SpawnFeedbackLabel(worldPos, "여기엔 지을 수 없음", new Color(1f, 0.4f, 0.3f, 1f), 1.6f, 0.4f);
                return;
            }

            ghosts.Add(CreateGhost(worldPos));
            GameFeelVfx.SpawnFeedbackLabel(worldPos + Vector2.up * 0.6f, "벽돌 예약!", new Color(0.6f, 0.95f, 1f, 1f), 1.8f, 0.5f);
        }

        private GameObject CreateGhost(Vector2 position)
        {
            var ghost = new GameObject("BrickGhost");
            ghost.transform.position = position;
            
            var info = ghost.AddComponent<PendingBrickInfo>();
            info.blockType = selectedBlockType;

            var sr = ghost.AddComponent<SpriteRenderer>();
            BlockData blockData = GetBlockDataForType(selectedBlockType);
            sr.sprite = blockData != null ? blockData.normalSprite : null;

            Color baseColor = blockData != null ? blockData.blockColor : Color.white;
            sr.color = new Color(baseColor.r, baseColor.g, baseColor.b, 0.45f); // translucent blueprint tint
            sr.sortingOrder = 9;

            if (sr.sprite != null)
            {
                Vector2 native = sr.sprite.bounds.size;
                float maxNative = Mathf.Max(native.x, native.y);
                if (maxNative > 0.0001f)
                {
                    float s = 1f / maxNative;
                    ghost.transform.localScale = new Vector3(s, s, 1f);
                }
            }
            return ghost;
        }

        private BlockData GetBlockDataForType(SelectedBlockType type)
        {
            switch (type)
            {
                case SelectedBlockType.Wood:
                    return Resources.Load<BlockData>("WoodBlockData");
                case SelectedBlockType.Iron:
                    return Resources.Load<BlockData>("IronBlockData");
                case SelectedBlockType.Stone:
                default:
                    return Resources.Load<BlockData>("StoneBlockData");
            }
        }

        /// <summary>GameManager turn hook: player's turn begins → pending bricks become real.</summary>
        public void OnTurnChanged(bool isPlayerTurn)
        {
            if (!isPlayerTurn) return;
            ghosts.RemoveAll(g => g == null);
            if (ghosts.Count == 0) return;

            var blockPrefab = Resources.Load<GameObject>("DestructibleBlock");
            var stone = Resources.Load<BlockData>("StoneBlockData");
            var wood = Resources.Load<BlockData>("WoodBlockData");
            var iron = Resources.Load<BlockData>("IronBlockData");

            foreach (var ghost in ghosts)
            {
                Vector3 pos = ghost.transform.position;
                SelectedBlockType type = SelectedBlockType.Stone;
                var info = ghost.GetComponent<PendingBrickInfo>();
                if (info != null) type = info.blockType;

                if (Application.isPlaying) Destroy(ghost); else DestroyImmediate(ghost);
                if (blockPrefab == null) continue;

                var brick = Instantiate(blockPrefab, pos, Quaternion.identity);
                brick.name = "PlayerBrick";
                var db = brick.GetComponent<DestructibleBlock>();
                if (db != null)
                {
                    BlockData data = stone;
                    if (type == SelectedBlockType.Wood) data = wood;
                    else if (type == SelectedBlockType.Iron) data = iron;

                    if (data != null) db.ApplyBlockData(data);
                    db.isGroundAnchor = false;
                }
                if (brick.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.bodyType = RigidbodyType2D.Dynamic; // settles onto whatever is below
                }

                // Touch field don't move enemy: ignore collision with overlapping enemy units
                var brickCol = brick.GetComponent<Collider2D>();
                if (brickCol != null)
                {
                    var units = UnitController.ActiveOrScene;
                    for (int i = 0; i < units.Count; i++)
                    {
                        var enemy = units[i];
                        if (enemy.isPlayerUnit) continue;
                        var enemyCol = enemy.GetComponent<Collider2D>();
                        if (enemyCol != null && brickCol.bounds.Intersects(enemyCol.bounds))
                        {
                            Physics2D.IgnoreCollision(brickCol, enemyCol, true);
                        }
                    }
                }

                // Textured brick-dust burst (playtest QA: the flat white/blue fx_spawn burst
                // read as a placeholder flash, not a "brick materializing" effect). fx_spawn
                // is now a real stone/rubble sprite sequence (god-tibo-imagen art pass), so
                // the tint stays near-white/warm to let the baked texture colors show through
                // instead of washing them back out with a strong blue multiply.
                FrameAnimEffect.Spawn("fx_spawn", pos, 1.8f, new Color(1f, 0.96f, 0.88f, 0.95f), 14f, 40);

                GameFeelVfx.SpawnShockwaveRing(pos, new Color(0.55f, 0.85f, 1f, 0.5f), 1.3f, 0.35f);
            }
            ghosts.Clear();
            GameFeelVfx.SpawnFeedbackLabel(new Vector3(0f, 4.5f, 0f), "예약 벽돌 건설!",
                new Color(0.65f, 0.95f, 1f, 1f), 2.0f, 0.7f);
            SiegeAlarmSystem.Post("예약 벽돌이 건설되었습니다", new Color(0.65f, 0.95f, 1f, 1f));
        }

        /// <summary>Rematch hygiene: drop all pending designations.</summary>
        public void ClearPending()
        {
            foreach (var g in ghosts)
            {
                if (g == null) continue;
                if (Application.isPlaying) Destroy(g); else DestroyImmediate(g);
            }
            ghosts.Clear();
        }

        private void CreateBlockUI()
        {
            var canvas = HudCanvas.Resolve();
            if (canvas == null) return;

            // Panel for block selection
            blockUIPanel = new GameObject("BlockSelectionPanel");
            blockUIPanel.transform.SetParent(HudCanvas.Root(), false);
            var panelRt = blockUIPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0.5f, 0f);
            panelRt.anchorMax = new Vector2(0.5f, 0f);
            panelRt.pivot = new Vector2(0.5f, 0f);
            panelRt.sizeDelta = new Vector2(390f, 50f);
            panelRt.anchoredPosition = new Vector2(0f, -36f); // centered and clean above unit selection

            var bgImage = blockUIPanel.AddComponent<UnityEngine.UI.Image>();
            var cardSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (cardSprite != null)
            {
                bgImage.sprite = cardSprite;
                bgImage.type = UnityEngine.UI.Image.Type.Sliced;

            }
            bgImage.color = new Color(0.12f, 0.12f, 0.15f, 0.75f);

            // Wood block button
            woodBtn = CreateSingleButton("WoodBlockButton", blockUIPanel.transform, 0, "WOOD WALL", new Color(0.72f, 0.58f, 0.42f, 1f));
            stoneBtn = CreateSingleButton("StoneBlockButton", blockUIPanel.transform, 1, "STONE WALL", new Color(0.6f, 0.72f, 0.85f, 1f));
            ironBtn = CreateSingleButton("IronBlockButton", blockUIPanel.transform, 2, "IRON WALL", new Color(0.55f, 0.55f, 0.6f, 1f));

            woodBtn.onClick.AddListener(() => SetSelectedType(SelectedBlockType.Wood));
            stoneBtn.onClick.AddListener(() => SetSelectedType(SelectedBlockType.Stone));
            ironBtn.onClick.AddListener(() => SetSelectedType(SelectedBlockType.Iron));

            UpdateBlockUISelection();
        }

        private UnityEngine.UI.Button CreateSingleButton(string name, Transform parent, int index, string labelText, Color normalColor)
        {
            var btnGo = new GameObject(name);
            btnGo.transform.SetParent(parent, false);
            var image = btnGo.AddComponent<UnityEngine.UI.Image>();
            
            var cardSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.ButtonCard);
            if (cardSprite != null)
            {
                image.sprite = cardSprite;
                image.type = UnityEngine.UI.Image.Type.Sliced;

            }
            image.color = normalColor;

            var btn = btnGo.AddComponent<UnityEngine.UI.Button>();
            var colors = btn.colors;
            colors.normalColor = normalColor;
            colors.highlightedColor = normalColor * 1.15f;
            colors.pressedColor = normalColor * 0.85f;
            colors.selectedColor = normalColor * 1.1f;
            btn.colors = colors;

            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0f, 0.5f);
            rt.sizeDelta = new Vector2(110f, 36f);
            float xPos = 15f + index * 125f;
            rt.anchoredPosition = new Vector2(xPos, 0f);

            var txtGo = new GameObject("Text");
            txtGo.transform.SetParent(btnGo.transform, false);
            var txtRt = txtGo.AddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = new Vector2(4f, 4f);
            txtRt.offsetMax = new Vector2(-4f, -4f);

            var tmp = txtGo.AddComponent<TextMeshProUGUI>();
            tmp.text = labelText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.fontStyle = FontStyles.Bold;
            
            // "modify button text to fit in the box. At least 3 times repeat"
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 5f;
            tmp.fontSizeMax = 12f;

            if (index == 0) woodText = tmp;
            else if (index == 1) stoneText = tmp;
            else if (index == 2) ironText = tmp;

            return btn;
        }

        private void SetSelectedType(SelectedBlockType type)
        {
            selectedBlockType = type;
            UpdateBlockUISelection();
            GameFeelVfx.SpawnFeedbackLabel(new Vector3(0f, 4.5f, 0f), $"{type} Block Selected", new Color(1f, 1f, 1f, 1f), 2f, 0.5f);
        }

        private void UpdateBlockUISelection()
        {
            if (woodBtn != null) SetButtonSelectedState(woodBtn, selectedBlockType == SelectedBlockType.Wood);
            if (stoneBtn != null) SetButtonSelectedState(stoneBtn, selectedBlockType == SelectedBlockType.Stone);
            if (ironBtn != null) SetButtonSelectedState(ironBtn, selectedBlockType == SelectedBlockType.Iron);
        }

        private void SetButtonSelectedState(UnityEngine.UI.Button btn, bool selected)
        {
            var outline = btn.GetComponent<UnityEngine.UI.Outline>();
            if (selected)
            {
                if (outline == null)
                {
                    outline = btn.gameObject.AddComponent<UnityEngine.UI.Outline>();
                    outline.effectColor = new Color(1f, 0.85f, 0.22f, 1f);
                    outline.effectDistance = new Vector2(3f, -3f);
                }
                btn.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
            }
            else
            {
                if (outline != null) Destroy(outline);
                btn.transform.localScale = Vector3.one;
            }
        }
    }
}
