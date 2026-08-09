using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Loops dedicated frame art on a gimmick's SpriteRenderer. Frames live under
    /// Resources/Gimmicks/&lt;key&gt;/ following the project convention (key_000.png ...).
    /// Attach AFTER the host computed its presentation scale: the animator re-derives the
    /// transform scale + BoxCollider2D size from frame 0 so the world footprint the host
    /// chose is preserved even when the animated art has a different native resolution.
    /// Missing art fails soft: the component detaches itself and the static sprite stays.
    /// </summary>
    public class GimmickFrameAnimator : MonoBehaviour
    {
        public float fps = 8f;
        public bool useUnscaledTime;

        private Sprite[] frames;
        private SpriteRenderer sr;
        private float elapsed;
        private int lastFrame = -1;
        private bool suspended;

        /// <summary>Pure loop math for EditMode tests: wraps forever, never overflows.</summary>
        public static int LoopFrameAt(float elapsedSeconds, float frameSeconds, int frameCount)
        {
            if (frameCount <= 0 || frameSeconds <= 0f) return 0;
            int raw = (int)(elapsedSeconds / frameSeconds);
            return ((raw % frameCount) + frameCount) % frameCount;
        }

        /// <summary>
        /// Attaches a looping animator when frame art exists for the key. Returns the animator
        /// or null (missing art / no renderer). Keeps the host's world size: scale and collider
        /// are recomputed from frame 0 against the renderer's current world bounds.
        /// </summary>
        public static GimmickFrameAnimator TryAttach(GameObject host, string key, float fps,
            bool useUnscaledTime = false)
        {
            if (host == null) return null;
            var sr = host.GetComponent<SpriteRenderer>();
            if (sr == null) return null;
            var frames = GimmickAnimLibrary.LoadFrames(key);
            if (frames == null || frames.Length < 2) return null;

            // Capture the host's world-space footprint BEFORE the frame swap: renderer
            // bounds per axis, plus the collider's world size when one exists. The anim
            // frames may have different native resolution/aspect than the static art; the
            // physics footprint (what shots hit) must not change when animation attaches
            // (code review cycle 3, P1 #3).
            Vector2 worldVisual = sr.sprite != null ? (Vector2)sr.bounds.size : Vector2.zero;
            var preBox = host.GetComponent<BoxCollider2D>();
            Vector2 worldCollider = preBox != null
                ? Vector2.Scale(preBox.size, (Vector2)host.transform.localScale)
                : Vector2.zero;
            Vector2 worldColliderOffset = preBox != null
                ? Vector2.Scale(preBox.offset, (Vector2)host.transform.localScale)
                : Vector2.zero;

            var anim = host.GetComponent<GimmickFrameAnimator>();
            if (anim == null) anim = host.AddComponent<GimmickFrameAnimator>();
            anim.fps = fps;
            anim.useUnscaledTime = useUnscaledTime;
            anim.sr = sr;
            anim.frames = frames;
            anim.elapsed = 0f;
            anim.lastFrame = -1;
            anim.suspended = false;

            sr.sprite = frames[0];
            if (worldVisual.sqrMagnitude > 0.0001f) anim.MatchWorldFootprint(worldVisual, worldCollider, worldColliderOffset);
            return anim;
        }

        private void MatchWorldFootprint(Vector2 worldVisual, Vector2 worldCollider, Vector2 worldColliderOffset)
        {
            if (sr.sprite == null) return;
            Vector2 native = sr.sprite.bounds.size;
            if (native.x <= 0.0001f || native.y <= 0.0001f) return;

            // Per-axis scale so the animated art occupies the SAME world rectangle the
            // static art did (uniform max-dimension scaling squashed aspect mismatches).
            var scale = new Vector3(worldVisual.x / native.x, worldVisual.y / native.y, 1f);
            transform.localScale = scale;

            if (TryGetComponent<BoxCollider2D>(out var box) && worldCollider.sqrMagnitude > 0.0001f)
            {
                // Rebuild local collider size/offset from the preserved WORLD values under
                // the new scale — physics footprint is identical to the pre-attach collider.
                box.size = new Vector2(worldCollider.x / scale.x, worldCollider.y / scale.y);
                box.offset = new Vector2(worldColliderOffset.x / scale.x, worldColliderOffset.y / scale.y);
            }
        }

        /// <summary>Damage states (cracked cores) take over the renderer; stop animating.</summary>
        public void Suspend() => suspended = true;

        private void Update()
        {
            if (suspended || frames == null || sr == null) return;
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            int frame = LoopFrameAt(elapsed, 1f / Mathf.Max(1f, fps), frames.Length);
            if (frame != lastFrame)
            {
                lastFrame = frame;
                sr.sprite = frames[frame];
            }
        }
    }

    /// <summary>
    /// Loader/cache for multi-frame gimmick animation art under Resources/Gimmicks/&lt;key&gt;/.
    /// Single-sprite art keeps using GimmickSpriteLibrary; this is the animated sibling.
    /// </summary>
    public static class GimmickAnimLibrary
    {
        public const string BarrelAnim = "barrel_anim";
        public const string Stage1BarrelAnim = "stage1_barrel_anim";
        public const string GateAnim = "gate_anim";
        public const string RallyRuneAnim = "rally_rune_anim";
        public const string HexRuneAnim = "hex_rune_anim";
        public const string CoreAnim = "core_anim";
        public const string IntroBanner = "IntroAnim"; // lives at Resources root, not Gimmicks/
        public const string LaunchGateAnim = "launch_gate_anim";
        public const string FlyingBeastAnim = "flying_beast_anim";
        private static readonly Dictionary<string, Sprite[]> cache = new Dictionary<string, Sprite[]>();

        public static Sprite[] LoadFrames(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (cache.TryGetValue(key, out var cached)) return cached;

            string path = key == IntroBanner ? key : $"Gimmicks/{key}";
            var frames = Resources.LoadAll<Sprite>(path);
            if (frames != null && frames.Length > 0)
            {
                frames = frames.OrderBy(f => f.name, StringComparer.Ordinal).ToArray();
            }
            cache[key] = frames;
            return frames;
        }
    }

    public enum FieldObstacleKind { Barrel, MiniTower, Rune, Patrol, SpikeTrap }

    /// <summary>
    /// Turn-driven battlefield lifecycle: new obstacles materialize, aged ones shatter,
    /// and the field composition mutates as the match progresses, so the board reads as a
    /// living siege instead of a fixed diorama. All scheduling decisions are pure
    /// (PlanForTurn) so EditMode tests pin the contract.
    /// </summary>
    public class GimmickFieldDirector : MonoBehaviour
    {
        public static GimmickFieldDirector Instance { get; private set; }

        // Set by GameManager.EnsureFieldDirector() from ApplyStageLayout()'s resolved stage.
        // Defaults to Stage1 so any test/scene that never assigns this keeps the original
        // 4-kind/3-turn-mutate/2-way-vent behavior byte-identical.
        public StageId stage = StageId.Stage1;

        public int maxFieldObstacles = 6;
        public int obstacleMaxAgeTurns = 6;
        // Widened lane envelope: symmetric around the bridge, clear of the cores (|x|=9)
        // and inside the ±14.5 launch aprons — ground content behind a launch point is dead
        // content (design review, cycle 2). Midfield ground lanes skip the chariot sweep
        // (|x|<=4.8) and the eruption vents (±5.4): solids there got plowed or cooked.
        public static readonly float[] SpawnLanes = { -13.5f, -12.5f, -6.5f, 0f, 6.5f, 12.5f, 13.5f };

        private struct FieldEntry
        {
            public GameObject go;
            public int bornTurn;
            public FieldObstacleKind kind;
        }

        private readonly List<FieldEntry> alive = new List<FieldEntry>();

        public int AliveCount
        {
            get
            {
                alive.RemoveAll(e => e.go == null);
                return alive.Count;
            }
        }

        public struct FieldPlan
        {
            public bool spawn;
            public bool despawnOldest;
            public bool mutate; // despawn oldest + spawn replacement in one beat
            public FieldObstacleKind kind;
            public int laneIndex;
        }

        /// <summary>
        /// Pure schedule (EditMode-pinned). GameManager turn parity: EndTurn increments
        /// turnCount BEFORE notifying, and the match starts on the player turn with count 0 —
        /// so ODD counts are AI-turn entries. Field beats land there on purpose: the board
        /// changes while the player watches the enemy volley, never right as they line up a shot.
        ///  - every 3rd turn (turn % 3 == 0, even or odd): MUTATE — trade the oldest piece for
        ///    a new one, so composition provably changes at least every 3 turns (AC5).
        ///  - other ODD turns: SPAWN when below capacity, else DESPAWN oldest.
        ///  - remaining even turns: rest beat (player reads a stable board).
        /// Kind rotates via ((turn/2)+turn)%4 — plain turn%4 could only ever reach two kinds on the
        /// odd spawn beats. Lane rotation stays coprime with the 7-lane table, but SOLID
        /// obstacles (blocking colliders: Barrel/MiniTower/Patrol) are folded onto the three
        /// inner lanes — a solid spawn 0.5u from a launch point muzzle-blocks the volley
        /// (code review, cycle 3). Trigger-only Runes may use the full wing envelope.
        /// </summary>
        public static FieldPlan PlanForTurn(int turn, int aliveCount, int maxObstacles)
        {
            return PlanForTurn(turn, aliveCount, maxObstacles, StageId.Stage1);
        }

        /// <summary>
        /// Stage-aware scheduler. Uses the stage's allowedGimmicks roster and mutation cadence
        /// to generate stage-specific variety, keeping the turn parity structure identical.
        /// </summary>
        public static FieldPlan PlanForTurn(int turn, int aliveCount, int maxObstacles, StageId stage)
        {
            var layout = StageDefinitions.For(stage);
            var allowed = layout.allowedGimmicks;
            if (allowed == null || allowed.Length == 0)
            {
                allowed = new[] { FieldObstacleKind.Barrel, FieldObstacleKind.MiniTower, FieldObstacleKind.Rune, FieldObstacleKind.Patrol };
            }
            int kindCount = allowed.Length;
            var kind = allowed[stage == StageId.Stage1 ? (((turn / 2) + turn) % kindCount) : (turn % kindCount)];
            var plan = new FieldPlan
            {
                kind = kind,
                laneIndex = LaneIndexFor(kind, turn),
            };
            if (turn <= 0) return plan;

            int mutateEveryNTurns = layout.mutateEveryNTurns;
            if (turn % mutateEveryNTurns == 0)
            {
                plan.mutate = aliveCount > 0;
                plan.spawn = true;
                plan.despawnOldest = aliveCount >= maxObstacles;
                return plan;
            }
            if (turn % 2 == 1)
            {
                if (aliveCount < maxObstacles) plan.spawn = true;
                else plan.despawnOldest = true;
            }
            return plan;
        }

        // Inner lane indices in SpawnLanes: {-6.5, 0, 6.5} — clear of both launch aprons
        // and both eruption vent columns (±5.4). The ±6.5 lanes start keg-occupied and the
        // chariot sweeps through 0, so FindClearLane's occupancy probe (with skip-on-full)
        // is what actually keeps solids from stacking — the table just names the columns.
        private static readonly int[] InnerLaneIndices = { 2, 3, 4 };

        /// <summary>Solid obstacles fold onto the inner lanes; runes and spike traps (no solid
        /// collider of their own — SpikeTrapGimmick detects proximity via OverlapCircleAll
        /// against unit colliders, so it can never depenetration-fling anything on spawn) roam
        /// the full table.</summary>
        public static int LaneIndexFor(FieldObstacleKind kind, int turn)
        {
            int raw = (turn * 3 + 1) % SpawnLanes.Length;
            if (kind == FieldObstacleKind.Rune || kind == FieldObstacleKind.SpikeTrap) return raw;
            return InnerLaneIndices[raw % InnerLaneIndices.Length];
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                // EditMode tests instantiate directors; Destroy() errors outside play mode.
                if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void NotifyTurnAdvanced(int turn)
        {
            PruneDeadEntries();

            // Expiry FIRST, plan SECOND: the schedule must decide from the field state the
            // player will actually see. Planning from the pre-expiry count let an expiry
            // beat + mutate beat crater the field by 2-3 pieces at once (review P2 #6).
            var expired = alive.Where(e => turn - e.bornTurn >= obstacleMaxAgeTurns).ToList();
            foreach (var e in expired) DespawnEntry(e);

            var plan = PlanForTurn(turn, alive.Count, maxFieldObstacles, stage);
            if (plan.mutate || plan.despawnOldest) DespawnOldest();
            if (plan.spawn) SpawnObstacle(plan.kind, SpawnLanes[plan.laneIndex], turn);

            // AOS overhaul: timed hazards (§3) and balance events (§6) ride the same beat.
            AdvanceTimedEvents(turn);
        }

        // ---- Timed vents (§3) + balance events (§6) ----

        private struct TimedEntry
        {
            public GameObject go;
            public int bornTurn;
            public int lifetimeTurns;
        }

        private readonly List<TimedEntry> timedEvents = new List<TimedEntry>();

        private void AdvanceTimedEvents(int turn)
        {
            // Expire aged event pieces with the same shatter language as field obstacles.
            timedEvents.RemoveAll(e => e.go == null);
            foreach (var e in timedEvents.Where(e => turn - e.bornTurn >= e.lifetimeTurns).ToList())
            {
                timedEvents.RemoveAll(x => x.go == e.go);
                if (e.go != null)
                {
                    FrameAnimEffect.Spawn("fx_shatter", e.go.transform.position, 1.9f, Color.white, 16f, 40);
                    if (Application.isPlaying) Destroy(e.go); else DestroyImmediate(e.go);
                }
            }

            if (VentSchedule.ShouldSpawnOnTurn(turn)) SpawnTimedVent(turn);

            var gm = GameManager.Instance;
            float playerFrac = 1f, enemyFrac = 1f;
            for (int i = 0; i < DestructibleBlock.Active.Count; i++)
            {
                if (DestructibleBlock.Active[i] is CastleCoreGimmick core)
                {
                    float frac = core.maxHP > 0f ? Mathf.Clamp01(core.currentHP / core.maxHP) : 0f;
                    if (core.isPlayerCore) playerFrac = frac; else enemyFrac = frac;
                }
            }
            var evt = BalanceEventPlanner.Plan(turn, playerFrac, enemyFrac);
            if (evt.kind != BalanceEventPlanner.EventKind.None && gm != null)
            {
                SpawnBalanceEvent(evt, turn, gm);
            }
        }

        /// <summary>
        /// A vent erupts somewhere on the terrain between the camps for a few turns, then
        /// vanishes — position is random per event, style alternates magma/petal.
        /// </summary>
        private void SpawnTimedVent(int turn)
        {
            float x = UnityEngine.Random.Range(VentSchedule.MinX, VentSchedule.MaxX);
            var go = new GameObject($"EventVent_T{turn}");
            go.transform.position = new Vector3(x, VentSchedule.GroundY, 0f);
            var vent = go.AddComponent<EruptionVentGimmick>();
            vent.style = VentSchedule.StyleForTurn(turn, stage);
            // Land in the WARNING band quickly so the fresh hazard telegraphs right away.
            vent.phaseOffset = Mathf.Max(0f, vent.dormantDuration - 1.2f);
            timedEvents.Add(new TimedEntry { go = go, bornTurn = turn, lifetimeTurns = VentSchedule.LifetimeTurns });

            FrameAnimEffect.Spawn("fx_spawn", go.transform.position + Vector3.up * 0.4f,
                2.1f, new Color(1f, 0.7f, 0.4f, 0.95f), 14f, 40);
            string spawnLabel = vent.style == EruptionStyle.Magma ? "지각 균열! MAGMA VENT"
                : vent.style == EruptionStyle.Frost ? "냉기 서리! FROST VENT"
                : "꽃가루 분출! PETAL VENT";
            GameFeelVfx.SpawnFeedbackLabel(go.transform.position + Vector3.up * 1.1f,
                spawnLabel, new Color(1f, 0.62f, 0.3f, 1f), 2.4f, 0.8f);
            string alarmText = vent.style == EruptionStyle.Magma
                ? $"마그마 벤트 발생 (x={x:F0}) — 저공 사격 주의, {VentSchedule.LifetimeTurns}턴 유지"
                : vent.style == EruptionStyle.Frost
                    ? $"서리 벤트 발생 (x={x:F0}) — 측면 강풍/둔화 주의, {VentSchedule.LifetimeTurns}턴 유지"
                    : $"꽃가루 벤트 발생 (x={x:F0}) — 상승기류, {VentSchedule.LifetimeTurns}턴 유지";
            SiegeAlarmSystem.Post(alarmText, new Color(1f, 0.62f, 0.3f, 1f));
        }

        private void SpawnBalanceEvent(BalanceEventPlanner.BalanceEvent evt, int turn, GameManager gm)
        {
            // BUGFIX (help/hindrance gimmick spawn range too narrow): this used to spawn every
            // Buff/Debuff rune and Power/Reduce gate at the exact same fixed x (+-11.5) turn
            // after turn, and that single point never scaled with the selected stage's actual
            // launch-apron distance (Stage2's tighter 13.5 apron vs Stage3's much wider 18.5),
            // so on Stage3 especially the events always clustered near the core instead of
            // ranging across the wider board. Now the approach point is (a) stage-aware -
            // scaled off the active stage's LaunchApronAbsX instead of a hardcoded literal -
            // and (b) randomized turn-to-turn within a safe band that stays clear of the core
            // (GameManager.CoreAbsX) on the inner edge and the launch ring on the outer edge,
            // so the balance/help gimmicks range across meaningfully different spots instead
            // of one repeated coordinate, while never landing somewhere that breaks the
            // existing core/ring clearance guarantees.
            float apron = GameManager.LaunchApronAbsX;
            float innerEdge = GameManager.CoreAbsX + 1.5f;
            float outerEdge = Mathf.Max(innerEdge + 0.5f, apron - 1.0f);
            float baseX = Mathf.Clamp(apron - 3.0f, innerEdge, outerEdge);
            float jitterX = UnityEngine.Random.Range(-2.2f, 2.2f);
            float sideAbsX = Mathf.Clamp(baseX + jitterX, innerEdge, outerEdge);
            float approachX = evt.onPlayerSide ? -sideAbsX : sideAbsX;
            float yJitter = UnityEngine.Random.Range(-0.35f, 0.35f);

            GameObject go = null;
            switch (evt.kind)
            {
                case BalanceEventPlanner.EventKind.BuffRune:
                case BalanceEventPlanner.EventKind.DebuffRune:
                {
                    go = new GameObject(evt.kind == BalanceEventPlanner.EventKind.BuffRune ? "EventBuffRune" : "EventHexRune");
                    go.transform.position = new Vector3(approachX, 3.5f + yJitter, 0f);

                    go.AddComponent<SpriteRenderer>();
                    var rune = go.AddComponent<BuffDebuffGimmick>();
                    rune.effectType = evt.kind == BalanceEventPlanner.EventKind.BuffRune
                        ? GimmickEffectType.Buff : GimmickEffectType.Debuff;
                    rune.targetWorldSize = 2.6f;
                    break;
                }
                case BalanceEventPlanner.EventKind.PowerGate:
                    go = gm.SpawnBalanceGate("EventPowerGate", new Vector3(approachX, 5.2f + yJitter, 0f), EventGateEffectType.PowerUp);
                    break;
                case BalanceEventPlanner.EventKind.ReduceGate:
                    go = gm.SpawnBalanceGate("EventReduceGate", new Vector3(approachX, 5.2f + yJitter, 0f), EventGateEffectType.Reduce);

                    break;
                case BalanceEventPlanner.EventKind.NeutralMultiplyGate:
                    go = gm.SpawnBalanceGate("EventMultiplyGate", new Vector3(0f, 6.1f, 0f), EventGateEffectType.Multiply);
                    break;
            }
            if (go == null) return;

            timedEvents.Add(new TimedEntry { go = go, bornTurn = turn, lifetimeTurns = BalanceEventPlanner.LifetimeTurns });
            // Magical rune/gate spawn: keep the clean light-burst art (fx_arcane), not the
            // new brick/rubble-textured fx_spawn (that reskin is for physical bricks/obstacles).
            FrameAnimEffect.Spawn(EffectSpriteLibrary.Arcane, go.transform.position, 2.1f, new Color(0.6f, 0.9f, 1f, 0.95f), 14f, 40);


            GameFeelVfx.SpawnFeedbackLabel(go.transform.position + Vector3.up * 1.0f,
                "전세 이벤트! FIELD EVENT", new Color(0.65f, 0.9f, 1f, 1f), 2.2f, 0.7f);
            string what = evt.kind == BalanceEventPlanner.EventKind.BuffRune ? "강화 룬"
                : evt.kind == BalanceEventPlanner.EventKind.DebuffRune ? "약화 룬"
                : evt.kind == BalanceEventPlanner.EventKind.PowerGate ? "파워 게이트"
                : evt.kind == BalanceEventPlanner.EventKind.ReduceGate ? "감속 게이트" : "증식 게이트";
            string side = evt.kind == BalanceEventPlanner.EventKind.NeutralMultiplyGate ? "중앙"
                : evt.onPlayerSide ? "아군 진영" : "적 진영";
            SiegeAlarmSystem.Post($"전세 이벤트: {side}에 {what} 출현", new Color(0.65f, 0.9f, 1f, 1f));
        }

        // Dead entries: destroyed GOs, and tower roots whose children are all gone —
        // an empty invisible root must not hold a capacity slot (review P2 #7).
        private void PruneDeadEntries()
        {
            alive.RemoveAll(e => e.go == null ||
                (e.kind == FieldObstacleKind.MiniTower && e.go.GetComponentInChildren<DestructibleBlock>() == null));
        }

        /// <summary>Rematch/StartGame entry: clear every field piece and the tracking list.</summary>
        public void ResetField()
        {
            alive.RemoveAll(e => e.go == null);
            foreach (var e in alive.ToList())
            {
                if (e.go == null) continue;
                if (Application.isPlaying) Destroy(e.go); else DestroyImmediate(e.go);
            }
            alive.Clear();

            foreach (var e in timedEvents)
            {
                if (e.go == null) continue;
                if (Application.isPlaying) Destroy(e.go); else DestroyImmediate(e.go);
            }
            timedEvents.Clear();
        }

        public void DespawnOldest()
        {
            alive.RemoveAll(e => e.go == null);
            if (alive.Count == 0) return;
            var oldest = alive.OrderBy(e => e.bornTurn).First();
            DespawnEntry(oldest);
        }

        private void DespawnEntry(FieldEntry entry)
        {
            alive.RemoveAll(e => e.go == entry.go || e.go == null);
            if (entry.go == null) return;

            Vector3 pos = entry.go.transform.position;
            FrameAnimEffect.Spawn("fx_shatter", pos, 1.9f, Color.white, 16f, 40);
            GameFeelVfx.SpawnImpactBurst(pos, new Color(0.75f, 0.72f, 0.66f, 0.8f), 0.9f);

            if (Application.isPlaying) Destroy(entry.go);
            else DestroyImmediate(entry.go);
        }

        public GameObject SpawnObstacle(FieldObstacleKind kind, float laneX, int turn)
        {
            // Solid obstacles materializing inside a resting unit/debris cause depenetration
            // flings on the next physics step (review P2 #9). Nudge along the lane table;
            // when EVERY inner lane is occupied (early game: kegs park on ±6.5), skip the
            // beat entirely — an overlapped spawn flings bodies, and a skipped spawn simply
            // keeps the widened midfield open (this QA pass is about breathing room).
            if (kind != FieldObstacleKind.Rune && kind != FieldObstacleKind.SpikeTrap && Application.isPlaying)
            {
                float? clear = FindClearLane(laneX);
                if (clear == null) return null;
                laneX = clear.Value;
            }

            GameObject go = null;
            switch (kind)
            {
                case FieldObstacleKind.Barrel:
                    go = GameManager.Instance != null
                        ? GameManager.Instance.SpawnFieldBarrel(new Vector3(laneX, 0.5f, 0f))
                        : null;
                    break;
                case FieldObstacleKind.MiniTower:
                    go = SpawnMiniTower(laneX);
                    break;
                case FieldObstacleKind.Rune:
                    go = SpawnFieldRune(laneX, turn);
                    break;
                case FieldObstacleKind.Patrol:
                    go = SpawnPatrol(laneX, turn);
                    break;
                case FieldObstacleKind.SpikeTrap:
                    go = SpawnSpikeTrap(laneX);
                    break;
            }
            if (go != null)
            {
                alive.Add(new FieldEntry { go = go, bornTurn = turn, kind = kind });
                FrameAnimEffect.Spawn("fx_spawn", go.transform.position + Vector3.up * 0.4f,
                    2.1f, new Color(0.55f, 0.8f, 1f, 0.95f), 14f, 40);
                GameFeelVfx.SpawnShockwaveRing(go.transform.position,
                    new Color(0.5f, 0.8f, 1f, 0.55f), 1.6f, 0.4f);
            }
            return go;
        }

        // Ground-level solid spawns check a 1x2 box (tower height) above the ground line;
        // occupied lanes defer to another clear inner lane, else the spawn beat is skipped.
        // Launch rings are hard-excluded (§5): a solid in the muzzle blocks every volley.
        private float? FindClearLane(float preferredX)
        {
            foreach (float candidate in LaneProbeOrder(preferredX))
            {
                if (LaunchRingRules.IsInsideRing(new Vector2(candidate, 0.5f))) continue;
                var hit = Physics2D.OverlapBox(new Vector2(candidate, 1.0f), new Vector2(1.0f, 1.9f), 0f);
                if (hit == null) return candidate;
            }
            return null;
        }

        private static IEnumerable<float> LaneProbeOrder(float preferredX)
        {
            yield return preferredX;
            foreach (int i in InnerLaneIndices)
            {
                float lane = SpawnLanes[i];
                if (!Mathf.Approximately(lane, preferredX)) yield return lane;
            }
        }

        /// <summary>Test entry: same spawn path; FX layers early-out outside play mode.</summary>
        public GameObject TestSpawn(FieldObstacleKind kind, float laneX, int turn)
        {
            return SpawnObstacle(kind, laneX, turn);
        }

        private GameObject SpawnMiniTower(float laneX)
        {
            var blockPrefab = Resources.Load<GameObject>("DestructibleBlock");
            if (blockPrefab == null) return null;
            var stone = Resources.Load<BlockData>("StoneBlockData");

            var root = new GameObject($"FieldTower_{laneX:F0}");
            root.transform.position = new Vector3(laneX, 0.5f, 0f);
            for (int i = 0; i < 2; i++)
            {
                var block = Instantiate(blockPrefab, new Vector3(laneX, 0.5f + i, 0f),
                    Quaternion.identity, root.transform);
                block.name = $"FieldTowerBlock_{i}";
                var db = block.GetComponent<DestructibleBlock>();
                if (db != null)
                {
                    if (stone != null) db.ApplyBlockData(stone);
                    db.isGroundAnchor = false;
                }
                if (block.TryGetComponent<Rigidbody2D>(out var rb))
                {
                    rb.bodyType = RigidbodyType2D.Dynamic;
                    rb.mass = 2f;
                }
            }
            return root;
        }

        private GameObject SpawnFieldRune(float laneX, int turn)
        {
            var go = new GameObject($"FieldRune_{laneX:F0}");
            go.transform.position = new Vector3(laneX, 3.4f, 0f);
            go.AddComponent<SpriteRenderer>();
            var rune = go.AddComponent<BuffDebuffGimmick>();
            rune.effectType = (turn % 6) < 3 ? GimmickEffectType.Buff : GimmickEffectType.Debuff;
            rune.targetWorldSize = 2.4f;
            return go;
        }

        private GameObject SpawnPatrol(float laneX, int turn)
        {
            var go = new GameObject($"FieldPatrol_{laneX:F0}");
            go.transform.position = new Vector3(laneX, 4.2f, 0f);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<BoxCollider2D>().size = new Vector2(1f, 1f);
            var moving = go.AddComponent<MovingGimmick>();
            moving.targetWorldSize = 2.4f;
            moving.moveAxis = (turn % 2 == 0) ? Vector2.right : Vector2.up;
            moving.moveDistance = 2.2f;
            moving.moveSpeed = 1.5f + 0.15f * (turn % 4);
            return go;
        }

        // Flush-with-ground floor plate (Stage3's proximity-triggered hazard). No collider of
        // its own — SpikeTrapGimmick detects proximity via OverlapCircleAll against unit
        // colliders, so it's exempt from FindClearLane's occupancy probe in SpawnObstacle.
        // SpriteRenderer added bare BEFORE the gimmick, matching every other spawner in this
        // class (SpawnMiniTower/SpawnPatrol/GameManager.SpawnExplosiveBarrel etc.): a nested
        // AddComponent<SpriteRenderer>() called from inside SpikeTrapGimmick.Awake() itself
        // silently fails to attach in EditMode (confirmed live — GetComponent/AddComponent
        // for a sibling component added during another component's own synchronous Awake()
        // does not see/attach the sibling), so every gimmick's Awake()-time fallback branch
        // is dead code in practice; pre-adding here is required, not just defensive.
        private GameObject SpawnSpikeTrap(float laneX)
        {
            var go = new GameObject($"FieldSpikeTrap_{laneX:F0}");
            go.transform.position = new Vector3(laneX, 0.05f, 0f);
            go.AddComponent<SpriteRenderer>();
            go.AddComponent<SpikeTrapGimmick>();
            return go;
        }
    }
    /// <summary>
    /// Comeback layer ("일발역전"): when a core drops into the danger band a one-shot
    /// LAST STAND becomes available — the next volley hits dramatically harder. The player
    /// arms it manually (R); the AI's weaker mirror arms itself. All decisions are pure
    /// static functions; GameManager owns the state and applies the multipliers on launch.
    /// </summary>
    public static class LastStand
    {
        public const float DangerHpFraction = 0.35f;

        public const float PlayerDamageMult = 2.2f;
        public const float PlayerRadiusMult = 1.5f;
        public const float PlayerSpeedMult = 1.3f;

        public const float AiDamageMult = 1.6f;
        public const float AiRadiusMult = 1.25f;
        public const float AiSpeedMult = 1.15f;

        // Design-review cycle 2: a buffed bomber (95 x 2.2 = 209) out-damaged the entire core
        // pool (150 HP + 50 shield) in one hit — a full-HP one-shot with no counterplay. The
        // comeback should breach walls and finish WOUNDED cores, not delete healthy ones:
        // cap any single buffed hit below the 150 core max so the shield window always exists.
        public const float SingleHitDamageCap = 140f;

        /// <summary>Buffed per-hit damage, capped so one volley can never erase a full core.</summary>
        public static float BuffedDamage(float baseDamage, bool isPlayer)
        {
            return Mathf.Min(baseDamage * DamageMult(isPlayer), SingleHitDamageCap);
        }

        public enum Phase { Locked, Armed, Active, Consumed }

        /// <summary>Danger when the core is at or below the fraction (inclusive). No core -> no danger.</summary>
        public static bool IsDanger(float currentHp, float maxHp)
        {
            if (maxHp <= 0f) return false;
            // +1e-4 keeps the 35%-exact boundary inclusive under float division error.
            return currentHp > 0f && currentHp / maxHp <= DangerHpFraction + 1e-4f;
        }

        /// <summary>One-way arm latch: once armed it survives recovering above the band.</summary>
        public static Phase Advance(Phase phase, bool inDanger)
        {
            if (phase == Phase.Locked && inDanger) return Phase.Armed;
            return phase;
        }

        /// <summary>
        /// AI variant: the mirror never waits on player input, so danger arms AND activates
        /// in one pure step (review P2 #8 — the imperative double-assign in Update was
        /// untestable and read as a bug).
        /// </summary>
        public static Phase AdvanceAuto(Phase phase, bool inDanger)
        {
            if (phase == Phase.Locked && inDanger) return Phase.Active;
            return phase;
        }

        public static float DamageMult(bool isPlayer) => isPlayer ? PlayerDamageMult : AiDamageMult;
        public static float RadiusMult(bool isPlayer) => isPlayer ? PlayerRadiusMult : AiRadiusMult;
        public static float SpeedMult(bool isPlayer) => isPlayer ? PlayerSpeedMult : AiSpeedMult;
    }
}
