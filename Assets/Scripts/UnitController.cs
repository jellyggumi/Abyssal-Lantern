using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CastleBusters
{
    /// <summary>
    /// Roster/field body kinds. The old `Bomber` (launched bomb soldier) was removed with
    /// the deployment overhaul (design/deployment-economy.md §2): its splash-at-range niche
    /// is now the deploy-only <see cref="Cannon"/> installation. `Barrel` is the powder-keg
    /// gimmick, which kept the fuse/detonate behaviour the bomber used to borrow.
    /// </summary>
    public enum UnitType { Knight, Archer, Barrel, Cannon }
    public enum UnitState { Idle, Launched, Grounded, Attacking, Dead }

    public class UnitController : MonoBehaviour
    {
        // One flight-physics contract is shared by the runtime Rigidbody2D path and
        // LaunchManager's predictive arc. Keeping the mass reduction and wind acceleration
        // here prevents the real projectile from drifting away from the drawn trajectory.
        public const float RuntimeMassScale = 0.35f;
        public const float MinRuntimeMass = 0.15f;
        public const float DefaultHardCeilingY = 20f;
        public const float DefaultLaunchSpawnHeight = 0.9f;

        public static Vector2 CalculateWindAcceleration(
            Vector2 position,
            float mass,
            float windForce,
            Vector2 windOrigin,
            float windRadius)
        {
            if (Mathf.Approximately(windForce, 0f) || windRadius <= 0f) return Vector2.zero;
            if ((position - windOrigin).sqrMagnitude > windRadius * windRadius) return Vector2.zero;

            return new Vector2(windForce / Mathf.Max(MinRuntimeMass, mass), 0f);
        }
        private const float ReferenceVisualScale = 0.42f;
        // Shared ally/enemy multiply-tint for sprites. UnitSpriteAnimator (Knight/Archer)
        // and ExplosiveGimmick (the powder-keg gimmick, which owns its own sprite and does NOT
        // use UnitSpriteAnimator - see the isGimmickVisual check in Awake below) both read these
        // so every launched object reads as clearly player (cool blue) or enemy (warm red) at a
        // glance, and the two can never visually drift apart from each other again.
        public static readonly Color AllySpriteTint = new Color(0.55f, 0.78f, 1f, 1f);
        public static readonly Color EnemySpriteTint = new Color(1f, 0.5f, 0.42f, 1f);


        public UnitType unitType;
        public bool isPlayerUnit = true;
        public UnitData unitData;

        [Header("Stats")]
        public float maxHP = 100f;
        public float currentHP;
        public float moveSpeed = 2f;
        public float attackDamage = 20f;
        public float attackRange = 1.5f;
        public float attackCooldown = 1.5f;
        [Header("Barrel Specific (powder keg)")]

        public float explosionRadius = 1.6f;
        public float explosionDamage = 70f;
        public GameObject explosionEffectPrefab;

        [Header("Knight Specific")]
        public float knightPushForceMultiplier = 1.6f;
        public float knightComboIntervalSeconds = 0.14f;

        [Header("Archer Specific")]
        public GameObject arrowPrefab;
        public Transform firePoint;
        public float archerJumpVelocity = 6.5f;
        public float archerVolleyFollowupDelaySeconds = 0.18f;


        [Header("Presentation Scale")]
        public float visualScale = 0.48f; // prefabs serialize the same value; enlarged hero pass
        public float colliderVisualCoverage = 0.82f;
        public float trailWidthScale = 0.18f;
        [Header("QA Safeguards")]

        public Rect playableBounds = new Rect(-22f, -9.5f, 44f, 24f);
        public float stuckVelocityThreshold = 0.05f;
        public float stuckDuration = 1.25f;
        public int maxDirectionFlipsBeforeRecovery = 6;
        // Playtest bug: units were seen climbing straight up off the top of the screen and
        // never returning. Root cause was two obstacle-hop/push sites that re-set
        // rb.velocity.y to a positive constant every single Update() tick with no "already
        // airborne" gate (unlike the other hop sites, which do gate on it) - while a unit sat
        // pinned against a wall/blocker the script re-won the race against gravity every
        // frame and the unit ratcheted upward forever. Those call sites are now gated (see
        // MoveTowardsTarget/knight push below), and this hard ceiling is the backstop: no
        // matter what future code sets a stray positive rb.velocity.y, once a unit climbs
        // above this height gravity is guaranteed to win from then on (see EnforceHardCeiling).
        public float hardCeilingY = DefaultHardCeilingY;

        private UnitState currentState = UnitState.Idle;

        private Rigidbody2D rb;
        private Collider2D col;
        private TrailRenderer trailRenderer;
        private float lastAttackTime;
        // AOS overhaul (§2): 1-based swing counter driving the 3/6 (knight) and 5/10
        // (archer) combo beats. Never reset — the modulo cycle repeats naturally.
        private int attackOrdinal;
        // Powder-keg landing fuse: armed on touchdown, detonates BarrelFuseSeconds later.
        private bool fuseArmed;
        // Deployed artillery (대포): set by MakeStationaryInstallation() so the walk/chase/
        // melee loop is skipped entirely and the battery holds its placed ground.
        private bool isStationaryInstallation;
        private Transform target;
        private float stuckTimer;
        private float groundedStuckTimer;
        private float lastMoveDirection;

        private int rapidDirectionFlipCount;
        private float directionFlipWindowTimer;

        // Buff/Debuff fields
        private float damageMultiplier = 1.0f;
        private float speedMultiplier = 1.0f;
        private float buffTimer = 0f;
        private float debuffTimer = 0f;
        private Color originalColor = Color.white;
        private bool hasStoredOriginalColor = false;
        // Carries the side that caused the fatal hit through delayed Barrel detonation.
        // Null means environmental/self-expiry with no external killer.
        private bool? fatalDamageFromPlayer;

        public UnitState CurrentState => currentState;
        /// <summary>True while this live powder keg is waiting for its armed fuse to resolve.</summary>
        public bool IsFusePending => unitType == UnitType.Barrel && fuseArmed && currentState != UnitState.Dead;
        public float GetMaxHP() => maxHP;

        public static readonly List<UnitController> Active = new List<UnitController>();
        private void OnEnable() { Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }
        // EditMode tests call Explode()/placement code directly without OnEnable ever firing,
        // so the registry is empty outside play mode — fall back to the old scene scan there.
        // Play-mode hot paths always take the list branch (zero scans per frame).
        public static IReadOnlyList<UnitController> ActiveOrScene =>
            Application.isPlaying ? Active : (IReadOnlyList<UnitController>)FindObjectsOfType<UnitController>();

        private void Awake()
        {
            ApplyPlayableScaleAndCollider();

            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<Collider2D>();

            var sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) sr.sortingOrder = 3;
            foreach (var childSr in GetComponentsInChildren<SpriteRenderer>(true))
            {
                childSr.sortingOrder = 3;
            }

            // Gimmick objects (e.g. ExplosiveGimmick/powder keg) that get a UnitController
            // bolted on at launch time (see LaunchManager.SpawnAndLaunchOne) already own their
            // sprite/animation via GimmickFrameAnimator. Adding a UnitSpriteAnimator here would
            // run its Awake() before this object's unitType is assigned (that happens right
            // after AddComponent<UnitController>() returns), so it would bake in the C# enum
            // default (Knight) and then keep overwriting the barrel sprite with unit character
            // frames every frame — the gimmick would visually turn into a random unit mid-flight.
            bool isGimmickVisual = GetComponent<ExplosiveGimmick>() != null;
            var animator = GetComponent<UnitSpriteAnimator>();
            if (animator == null && !isGimmickVisual)
            {
                animator = gameObject.AddComponent<UnitSpriteAnimator>();
            }
            if (animator != null) animator.CaptureBaseScale();


            if (unitData != null)
            {
                unitType = unitData.unitType;
                maxHP = unitData.maxHP;
                moveSpeed = unitData.moveSpeed;
                attackDamage = unitData.attackDamage;
                attackRange = unitData.attackRange;
                attackCooldown = unitData.attackCooldown;
                if (unitType == UnitType.Barrel)
                {
                    explosionRadius = unitData.explosionRadius;
                    explosionDamage = unitData.explosionDamage;
                }
                else if (unitType == UnitType.Knight)
                {
                    knightPushForceMultiplier = unitData.knightPushForceMultiplier;
                    knightComboIntervalSeconds = unitData.knightComboIntervalSeconds;
                }
                else if (unitType == UnitType.Archer)
                {
                    archerJumpVelocity = unitData.archerJumpVelocity;
                    archerVolleyFollowupDelaySeconds = unitData.archerVolleyFollowupDelaySeconds;
                }

            }


            currentHP = maxHP;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Static;
                rb.mass = Mathf.Max(MinRuntimeMass, rb.mass * RuntimeMassScale);

                rb.sharedMaterial = new PhysicsMaterial2D { friction = 0.4f, bounciness = 0f };

                // BUGFIX (units "lying down" and unable to attack): LaunchManager's player
                // spawn path (SpawnAndLaunchOne) never called InitializeUnit, so the rigidbody
                // kept free rotation for the entire Launched flight - a glancing collision with
                // another unit, arrow, or gimmick before the first real Ground/DestructibleBlock
                // hit could tip it onto its side, and it then landed and got FreezeRotation'd at
                // that tumbled angle forever (or, if it never touched Ground/a block directly,
                // never transitioned out of Launched at all, so it could never attack). The
                // in-flight "spin" the player sees is a purely cosmetic child-transform rotation
                // driven by UnitSpriteAnimator, not this rigidbody, so freezing physics rotation
                // here for the unit's entire lifetime (both flight and grounded) removes the
                // tumble without touching that visual. Setting the constraint here (once, in
                // Awake, before any bodyType switch) covers every spawn path - player launches,
                // InitializeUnit-driven enemy/clone spawns, and gimmick-bolted units alike.
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }

        }

        private void ApplyPlayableScaleAndCollider()
        {
            if (GetComponent<ExplosiveGimmick>() != null) return;
            visualScale = Mathf.Max(0.01f, visualScale);

            // Wider stages frame more world, so a fixed-size body renders smaller there.
            // Folding the stage factor in here keeps a soldier the same size on screen on
            // every board. The serialized visualScale is deliberately not overwritten —
            // this must not compound if the method runs twice.
            float renderScale = Mathf.Max(0.01f, visualScale * GameManager.StageActorVisualScale);

            transform.localScale = new Vector3(renderScale, renderScale, 1f);

            var sr = GetComponentInChildren<SpriteRenderer>(true);
            var box = GetComponent<BoxCollider2D>();
            if (sr != null && sr.sprite != null && box != null)
            {
                Vector2 spriteSize = sr.sprite.bounds.size;
                // Dividing by the same factor the transform multiplies by leaves world
                // collider extents identical on every stage: art scales, hitboxes do not.
                float referenceScaleRatio = ReferenceVisualScale / renderScale;
                box.size = new Vector2(
                    Mathf.Max(0.25f / renderScale, spriteSize.x * colliderVisualCoverage * referenceScaleRatio),
                    Mathf.Max(0.25f / renderScale, spriteSize.y * colliderVisualCoverage * referenceScaleRatio));
                box.offset = sr.sprite.bounds.center;
            }
        }

        /// <summary>
        /// Predicts the collider bounds Awake produces, relative to the spawned root, without
        /// instantiating a preview body. LaunchManager uses the same bounds for spawn clearance
        /// and trajectory casts, so preview and runtime begin from one resolved root position.
        /// </summary>
        public static Bounds EstimateLaunchedWorldColliderBounds(GameObject prefab)
        {
            if (prefab == null)
            {
                return new Bounds(Vector3.zero, new Vector3(0.05f, 0.05f, 0f));
            }

            var spriteRenderer = prefab.GetComponentInChildren<SpriteRenderer>(true);
            var sprite = spriteRenderer != null ? spriteRenderer.sprite : null;
            var explosive = prefab.GetComponent<ExplosiveGimmick>();
            if (explosive != null && sprite != null)
            {
                Vector2 native = sprite.bounds.size;
                float maxNative = Mathf.Max(native.x, native.y);
                if (maxNative > 0.0001f)
                {
                    float scale = Mathf.Max(0.05f, explosive.targetWorldSize) / maxNative;
                    Vector2 center = sprite.bounds.center * scale;
                    Vector2 size = native * scale;
                    return new Bounds(center, new Vector3(size.x, size.y, 0f));
                }
            }

            var unit = prefab.GetComponent<UnitController>();
            if (unit != null && sprite != null)
            {
                // Must mirror ApplyPlayableScaleAndCollider exactly, stage factor included:
                // the runtime offset is the sprite centre times the *rendered* scale, so a
                // prediction using the bare visualScale would drift on the wider stages and
                // the aim preview would start from the wrong point.
                float renderScale = Mathf.Max(0.01f, unit.visualScale * GameManager.StageActorVisualScale);
                Vector2 spriteSize = sprite.bounds.size;
                Vector2 size = new Vector2(
                    Mathf.Max(0.25f, spriteSize.x * unit.colliderVisualCoverage * ReferenceVisualScale),
                    Mathf.Max(0.25f, spriteSize.y * unit.colliderVisualCoverage * ReferenceVisualScale));
                Vector2 center = sprite.bounds.center * renderScale;
                return new Bounds(center, new Vector3(size.x, size.y, 0f));
            }

            var box = prefab.GetComponent<BoxCollider2D>();
            if (box != null)
            {
                Vector3 scale = prefab.transform.lossyScale;
                Vector2 size = new Vector2(
                    Mathf.Abs(box.size.x * scale.x),
                    Mathf.Abs(box.size.y * scale.y));
                Vector2 center = new Vector2(box.offset.x * scale.x, box.offset.y * scale.y);
                return new Bounds(center, new Vector3(size.x, size.y, 0f));
            }

            return new Bounds(Vector3.zero, new Vector3(0.05f, 0.05f, 0f));
        }

        /// <summary>World-space size compatibility wrapper for trajectory footprint callers.</summary>
        public static Vector2 EstimateLaunchedWorldColliderSize(GameObject prefab)
        {
            Bounds bounds = EstimateLaunchedWorldColliderBounds(prefab);
            return new Vector2(bounds.size.x, bounds.size.y);
        }

        /// <summary>
        /// Fallback for spawn paths that do not resolve prefab bounds before Instantiate.
        /// SimpleAI still uses this after Awake computes the live collider. LaunchManager instead
        /// resolves <see cref="EstimateLaunchedWorldColliderBounds"/> before spawning so its
        /// preview and runtime root positions cannot diverge through a post-spawn correction.
        /// </summary>
        public static void SnapColliderAboveGround(GameObject go, float groundY)
        {
            var col = go.GetComponent<Collider2D>();
            if (col == null) return;
            float bottomGap = groundY - col.bounds.min.y;
            if (bottomGap > 0.01f) go.transform.position += new Vector3(0f, bottomGap, 0f);
        }


        public void InitializeUnit(bool isPlayer, UnitState state)
        {
            isPlayerUnit = isPlayer;
            currentState = state;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            ApplyHeroGrowth();
        }

        // Hero growth (content pass): collected loot raises the whole side's stats; each
        // fresh unit instance bakes the multipliers in exactly once.
        private bool growthApplied;

        private void ApplyHeroGrowth()
        {
            if (growthApplied) return;
            growthApplied = true;
            float dmg = HeroGrowth.DamageMult(isPlayerUnit);
            attackDamage *= dmg;
            var explosive = GetComponent<ExplosiveGimmick>();
            if (explosive != null)
            {
                explosive.SetPermanentPotency(
                    explosive.PermanentExplosionDamage * dmg,
                    explosive.PermanentExplosionRadius);
                explosionDamage = explosive.explosionDamage;
                explosionRadius = explosive.explosionRadius;
            }
            else
            {
                explosionDamage *= dmg;
            }
            float hp = HeroGrowth.HpMult(isPlayerUnit);
            maxHP *= hp;
            currentHP = maxHP;
            moveSpeed *= HeroGrowth.SpeedMult(isPlayerUnit);
        }

        private void Start()
        {
            IgnoreSameTeamCollisions();
            ConfigurePlayableBounds();
        }

        private void ConfigurePlayableBounds()
        {
            if (GameManager.Instance != null && GameManager.Instance.ActiveLayout.groundHalfWidth > 0)
            {
                float halfW = GameManager.Instance.ActiveLayout.groundHalfWidth;
                // Add a margin of 2.0u beyond the ground tiles so units aren't killed right at the edge
                float maxAbsX = halfW + 2f;
                playableBounds.xMin = -maxAbsX;
                playableBounds.width = maxAbsX * 2f;
            }
        }

        public void IgnoreSameTeamCollisions()
        {
            var myCol = col != null ? col : GetComponent<Collider2D>();
            if (myCol == null) return;

            for (int i = 0; i < Active.Count; i++)
            {
                var unit = Active[i];
                if (unit != this && unit.isPlayerUnit == isPlayerUnit)
                {
                    var otherCol = unit.GetComponent<Collider2D>();
                    if (otherCol != null)
                    {
                        Physics2D.IgnoreCollision(myCol, otherCol);
                    }
                }
            }
        }

        private void SetupTrailRenderer()
        {
            // Instantiate()-cloned units (EventGateGimmick.MultiplyUnit/MultiplyArrow's
            // Multiply-gate duplication) copy the source's own TrailRenderer component
            // verbatim, but Unity does not remap this private field onto the clone's copy
            // — the clone's `trailRenderer` reads null even though a TrailRenderer already
            // sits on the GameObject. Blindly calling AddComponent<TrailRenderer>() then
            // fails (DisallowMultipleComponent), returns null, and the null local field
            // NREs on the very next line (confirmed live via a Stage2 Multiply-gate clone).
            // Reuse the existing component when present instead of assuming a fresh add.
            trailRenderer = GetComponent<TrailRenderer>();
            if (trailRenderer == null) trailRenderer = gameObject.AddComponent<TrailRenderer>();
            trailRenderer.time = 0.5f;
            float width = unitType == UnitType.Barrel ? 0.14f : 0.09f;
            trailRenderer.startWidth = width * Mathf.Max(0.5f, trailWidthScale / 0.18f);
            trailRenderer.endWidth = 0f;
            trailRenderer.material = new Material(Shader.Find("Sprites/Default"));
            Color teamTint = isPlayerUnit ? new Color(0.45f, 0.85f, 1f, 0.75f) : new Color(1f, 0.35f, 0.25f, 0.75f);
            if (unitType == UnitType.Barrel) teamTint = new Color(1f, 0.65f, 0.12f, 0.85f);
            trailRenderer.startColor = teamTint;
            trailRenderer.endColor = new Color(teamTint.r, teamTint.g, teamTint.b, 0f);
            trailRenderer.emitting = false;
            trailRenderer.sortingOrder = 2;
        }

        public void Launch(Vector2 velocity)
        {
            currentState = UnitState.Launched;
            stuckTimer = 0f;
            ApplyHeroGrowth();
            // Comeback chokepoint: an Active LAST STAND on this side buffs damage/radius and
            // boosts the launch itself, exactly once.
            if (GameManager.Instance != null)
            {
                velocity = GameManager.Instance.ApplyLastStandOnLaunch(this, velocity);
            }
            GamePresentationDirector.Instance?.Focus(transform);
            GameFeelVfx.SpawnLaunchBurst(transform.position, isPlayerUnit ? new Color(0.45f, 0.85f, 1f, 0.7f) : new Color(1f, 0.35f, 0.25f, 0.7f), 0.28f);
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.velocity = velocity;
            }
            if (trailRenderer == null) SetupTrailRenderer();
            if (trailRenderer != null) trailRenderer.emitting = true;
        }

        /// <summary>
        /// The second creation verb (design/deployment-economy.md §2): the body is PLACED on
        /// the field already grounded and fighting, instead of being flung from the muzzle.
        /// Skips the Launched flight state entirely — no trail, no arc, no landing contact —
        /// so a deployed knight starts its walk on the frame it is paid for.
        /// Hero growth still applies, so a deployed body and a launched body of the same type
        /// are statistically identical; only the delivery differs.
        /// </summary>
        public void DeployGrounded()
        {
            currentState = UnitState.Grounded;
            stuckTimer = 0f;
            groundedStuckTimer = 0f;
            ApplyHeroGrowth();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            if (trailRenderer != null) trailRenderer.emitting = false;
            // Deployment skips landing contact, so a paid Barrel must enter the same fuse
            // state explicitly instead of falling through into the walking/melee loop.
            if (unitType == UnitType.Barrel) BeginFuse();
        }

        private void FixedUpdate()
        {
            if (currentState == UnitState.Dead) return;

            // Backstop for the "flew up and never came back down" bug: whatever set a stray
            // positive rb.velocity.y (obstacle hop, knight push, wind, a future bug we haven't
            // found yet), once the unit climbs above hardCeilingY its upward velocity is
            // clamped to zero every physics step from here on, so gravity is guaranteed to
            // take back over and bring it down instead of the unit racing gravity forever.
            EnforceHardCeiling();

            if (currentState == UnitState.Launched && rb != null)
            {
                if (GameManager.Instance != null)
                {
                    // Rigidbody2D applies gravity itself. Apply only the shared wind
                    // acceleration so runtime flight and LaunchManager.DrawTrajectory use
                    // the same radius, mass floor, and force-to-acceleration conversion.
                    var gm = GameManager.Instance;
                    Vector2 windAcceleration = CalculateWindAcceleration(
                        transform.position,
                        rb.mass,
                        gm.currentWindForce,
                        gm.windEffectOrigin,
                        gm.windEffectRadius);
                    if (windAcceleration.sqrMagnitude > 0f)
                    {
                        rb.AddForce(windAcceleration * rb.mass, ForceMode2D.Force);
                    }
                }

                MonitorLaunchedUnitSafety();
            }
            else if (currentState == UnitState.Grounded && rb != null)
            {
                MonitorGroundedUnitSafety();
            }
        }

        private void EnforceHardCeiling()
        {
            if (rb == null) return;
            if (transform.position.y > hardCeilingY && rb.velocity.y > 0f)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0f);
            }
        }


        private bool IsOutOfPlayableBounds(Vector2 position)
        {
            // Only the side walls and the floor are hard boundaries — there is no hard
            // ceiling here. Units launched on a high arc, caught by a strong wind gust, or
            // shoved by a knight push must be free to keep flying and arc back down instead
            // of vanishing the instant they cross an arbitrary on-screen height (playtest bug:
            // unit flies up while still clearly visible on screen, then just disappears with
            // no warning). "Gravity always wins eventually" used to be an assumption here, but
            // it wasn't actually guaranteed: two call sites (MoveTowardsTarget's obstacle hop,
            // the knight push) could re-win the race against gravity every single frame and
            // ratchet a unit upward forever with no bound. Those are fixed now (gated on not
            // already being airborne), and EnforceHardCeiling (FixedUpdate) is the backstop
            // that guarantees it going forward: past hardCeilingY, upward velocity is clamped
            // to zero so gravity is mechanically forced to take back over. ChariotRules.KillPlaneY
            // still catches anything on the way back down that falls through the floor.
            return position.x < playableBounds.xMin || position.x > playableBounds.xMax ||
                   position.y < playableBounds.yMin;
        }


        private void MonitorLaunchedUnitSafety()
        {
            if (rb == null) return;

            Vector2 position = transform.position;
            if (IsOutOfPlayableBounds(position))
            {
                GameFeelVfx.SpawnFeedbackLabel(transform.position, "OUT", new Color(1f, 0.45f, 0.25f, 1f), 1.6f, 0.4f);
                Die();
                return;
            }


            if (rb.velocity.magnitude <= stuckVelocityThreshold)
            {
                stuckTimer += Time.fixedDeltaTime;
                if (stuckTimer >= stuckDuration)
                {
                    GameFeelVfx.SpawnFeedbackLabel(transform.position, "STUCK FIX", new Color(1f, 0.85f, 0.25f, 1f), 1.6f, 0.4f);
                    currentState = UnitState.Grounded;
                    rb.velocity = Vector2.zero;
                    rb.angularVelocity = 0f;
                    rb.constraints = RigidbodyConstraints2D.FreezeRotation;
                    if (trailRenderer != null) trailRenderer.emitting = false;
                    GamePresentationDirector.Instance?.ClearFocus(transform);
                }
            }
            else
            {
                stuckTimer = 0f;
            }
        }

        private void MonitorGroundedUnitSafety()
        {
            if (rb == null || target == null)
            {
                groundedStuckTimer = 0f;
                return;
            }

            if (Mathf.Abs(rb.velocity.x) <= stuckVelocityThreshold)
            {
                groundedStuckTimer += Time.fixedDeltaTime;
                if (groundedStuckTimer >= stuckDuration)
                {
                    GameFeelVfx.SpawnFeedbackLabel(transform.position, "STUCK RECOVERY", new Color(1f, 0.85f, 0.25f, 1f), 1.6f, 0.4f);
                    transform.position += new Vector3(0f, 0.5f, 0f);
                    rb.velocity = new Vector2(rb.velocity.x, 6.5f);
                    groundedStuckTimer = 0f;
                }
            }
            else
            {
                groundedStuckTimer = 0f;
            }
        }

        private bool IsTargetInAttackRange()
        {
            if (target == null) return false;
            float dx = Mathf.Abs(target.position.x - transform.position.x);
            float dy = Mathf.Abs(target.position.y - transform.position.y);

            if (unitType != UnitType.Archer)
            {
                return dx <= attackRange && dy <= attackRange * 1.5f;
            }
            else
            {
                return Vector2.Distance(transform.position, target.position) <= attackRange;
            }
        }

        /// <summary>
        /// Converts this body into a placed installation (대포): it holds the ground it was
        /// deployed on and never walks, chases, or melees. CannonController owns the firing
        /// loop instead. Kinematic so a shell blast or a passing knight cannot shove a
        /// battery out of the position the player paid supply to choose.
        /// </summary>
        public void MakeStationaryInstallation()
        {
            isStationaryInstallation = true;
            currentState = UnitState.Grounded;
            target = null;
            if (trailRenderer != null) trailRenderer.emitting = false;
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.constraints = RigidbodyConstraints2D.FreezeAll;
            }
        }

        private void Update()
        {
            if (currentState == UnitState.Dead) return;

            // Combat runs only while a turn is actually live. During the intro card or the
            // results screen the board is a frozen diorama — but Update still ticks under
            // timeScale 0, and after a scene reload Time.time is large enough that every
            // attack cooldown reads as elapsed. Without this gate units melee-spam behind
            // the overlay, hit-stops fire, and the "game plays itself during intro" bug returns.
            var gm = GameManager.Instance;
            if (gm != null && gm.currentState != GameState.PlayerTurn && gm.currentState != GameState.AITurn) return;

            // Handle Buff/Debuff Timers
            if (buffTimer > 0f)
            {
                buffTimer -= Time.deltaTime;
                // Playtest note: a buff/debuff used to just silently revert to the base
                // color with no warning, so the player only ever learned it wore off by
                // suddenly losing/regaining damage output mid-fight. Blink the tint over
                // the last 0.8s (a fast 8Hz flicker) as a fair "about to expire" cue, then
                // fire a floating label + the normal ResetEffects revert on actual timeout.
                if (buffTimer <= 0.8f) ApplyExpiryBlink(new Color(0.2f, 1f, 0.3f, 1f), buffTimer);
                if (buffTimer <= 0f)
                {
                    ResetEffects();
                    SpawnBuffDebuffEndedLabel(true);
                }
            }
            if (debuffTimer > 0f)
            {
                debuffTimer -= Time.deltaTime;
                if (debuffTimer <= 0.8f) ApplyExpiryBlink(new Color(0.7f, 0.2f, 1f, 1f), debuffTimer);
                if (debuffTimer <= 0f)
                {
                    ResetEffects();
                    SpawnBuffDebuffEndedLabel(false);
                }
            }


            // Fuse-armed powder keg: no walking, no targeting — it sits and blows (§2).
            if (fuseArmed) return;

            // Placed installation (대포): CannonController drives aiming/firing; the walk,
            // target-chase, and melee loop below must never run for it.
            if (isStationaryInstallation) return;

            if (transform.position.y < ChariotRules.KillPlaneY)
            {
                Die();
                return;
            }
            // Grounded/Attacking bodies flung out of the arena (depenetration, blast waves,
            // vent columns) previously drifted forever — the stuck-recovery hop even laddered
            // them upward off-screen. Same OUT rule as launched units (QA pass, AOS overhaul).
            if ((currentState == UnitState.Grounded || currentState == UnitState.Attacking) &&
                IsOutOfPlayableBounds(transform.position))

            {
                GameFeelVfx.SpawnFeedbackLabel(transform.position, "OUT", new Color(1f, 0.45f, 0.25f, 1f), 1.6f, 0.4f);
                Die();
                return;
            }

            if (currentState == UnitState.Grounded)
            {
                FindTarget();
                // No-idle guarantee (flow-clarity pass): with nothing left to attack the
                // squad still ADVANCES on the enemy camp — marching bodies end up inside
                // the capture zone, so the match always visibly progresses.
                if (target == null) AdvanceTowardEnemyCamp();
                else MoveTowardsTarget();
            }
            else if (currentState == UnitState.Attacking)
            {
                FindTarget();
                if (target == null || !IsTargetInAttackRange()) currentState = UnitState.Grounded;
                else TryAttack();
            }
        }

        /// <summary>March on the enemy keep when no target remains (never stand idle).</summary>
        private void AdvanceTowardEnemyCamp()
        {
            if (rb == null) return;
            float campX = isPlayerUnit ? GameManager.CoreAbsX : -GameManager.CoreAbsX;
            float dir = Mathf.Sign(campX - transform.position.x);
            if (Mathf.Abs(campX - transform.position.x) < 1.2f)
            {
                // Arrived: hold the camp (capture-zone occupancy does the winning).
                rb.velocity = new Vector2(0f, rb.velocity.y);
                return;
            }
            rb.velocity = new Vector2(dir * moveSpeed * speedMultiplier * 0.85f, rb.velocity.y);

            // Same obstacle hop as the normal march.
            var hit = Physics2D.Raycast(transform.position, new Vector2(dir, 0f), 0.6f,
                ~LayerMask.GetMask("PlayerUnit", "EnemyUnit"));
            if (hit.collider != null && hit.collider.gameObject != gameObject && Mathf.Abs(rb.velocity.y) < 0.5f)
            {
                rb.velocity = new Vector2(rb.velocity.x, 5.5f);
            }
        }

        private void FindTarget()
        {
            // Target policy (TargetingRules): the opponent's INSTALLATIONS are the primary
            // objective — camp gimmicks (cores), enemy-placed kegs/towers on their half,
            // then enemy bodies, then plain wall/castle blocks. Terrain tiles are parented
            // to the castles for BFS support, so a y-based ground filter is what stops
            // units from beelining to the nearest floor tile and whacking the bridge
            // ("공격이 바닥으로만 향함" playtest feedback).
            Vector2 self = transform.position;
            Transform best = null;
            float bestScore = float.MaxValue;

            void Consider(Transform t, float weight)
            {
                float score = TargetingRules.Score(Vector2.Distance(self, t.position), weight);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            for (int i = 0; i < Active.Count; i++)
            {
                var u = Active[i];
                if (u == this || u.isPlayerUnit == isPlayerUnit || u.currentState == UnitState.Dead) continue;
                Consider(u.transform, TargetingRules.UnitWeight);
            }

            for (int i = 0; i < DestructibleBlock.Active.Count; i++)
            {
                var b = DestructibleBlock.Active[i];
                if (b == null || b.IsFalling) continue;
                if (TargetingRules.IsGroundTile(b.transform.position.y)) continue;

                var castle = b.GetComponentInParent<CastleController>();
                bool isCampGimmick = b.GetComponent<CastleCoreGimmick>() != null || b.GetComponent<ExplosiveGimmick>() != null;
                if (castle != null)
                {
                    if (castle.isPlayerCastle == isPlayerUnit) continue; // own structures
                    Consider(b.transform, isCampGimmick ? TargetingRules.GimmickWeight : TargetingRules.StructureWeight);
                }
                else if (TargetingRules.OnEnemyHalf(b.transform.position.x, isPlayerUnit))
                {
                    // Neutral installation stationed on the opponent's half — enemy-placed
                    // gimmick territory (kegs, field towers, the chariot mid-charge).
                    Consider(b.transform, TargetingRules.GimmickWeight);
                }
            }

            target = best;
        }

        private void MoveTowardsTarget()
        {
            if (target == null) return;

            if (IsTargetInAttackRange())
            {
                currentState = UnitState.Attacking;
                if (rb != null) rb.velocity = new Vector2(0, rb.velocity.y);
                return;
            }

            if (rb != null)
            {
                float dir = Mathf.Sign(target.position.x - transform.position.x);
                TrackGroundDirectionFlips(dir);
                rb.velocity = new Vector2(dir * moveSpeed * speedMultiplier, rb.velocity.y);

                // Jump over obstacles if blocked
                Vector2 rayOrigin = transform.position;
                Vector2 rayDirection = new Vector2(dir, 0f);
                float rayDistance = 0.6f;
                int mask = ~LayerMask.GetMask("PlayerUnit", "EnemyUnit");
                var hit = Physics2D.Raycast(rayOrigin, rayDirection, rayDistance, mask);
                // Gated on not already airborne (same guard AdvanceTowardEnemyCamp already used
                // below) - without it, a unit pinned against a wall/blocker re-fires this every
                // single Update() frame while still airborne from the previous hop, re-winning
                // the race against gravity every tick and ratcheting straight off the top of the
                // screen forever instead of a single clean hop over the obstacle.
                if (hit.collider != null && hit.collider.gameObject != gameObject && Mathf.Abs(rb.velocity.y) < 0.5f)
                {
                    rb.velocity = new Vector2(rb.velocity.x, 5.5f);
                }

                // Archer situational hop (§2): the target sits noticeably higher — jump to
                // regain a firing line instead of hugging the wall below it.
                if (unitType == UnitType.Archer && Mathf.Abs(rb.velocity.y) < 0.5f &&
                    UnitCombos.ArcherShouldJump(transform.position.y, target.position.y))
                {
                    rb.velocity = new Vector2(rb.velocity.x, archerJumpVelocity);

                }

                // Knight advance push (§2): an enemy body standing between the knight and a
                // farther objective gets shoved along the advance instead of stonewalling it.
                if (unitType == UnitType.Knight)
                {
                    var front = Physics2D.OverlapCircle(rayOrigin + rayDirection * (attackRange * 0.7f), 0.45f);
                    var blocker = front != null ? front.GetComponent<UnitController>() : null;
                    if (blocker != null && blocker != this && blocker.isPlayerUnit != isPlayerUnit &&
                        blocker.currentState != UnitState.Dead &&
                        UnitCombos.KnightShouldPush(
                            Vector2.Distance(transform.position, blocker.transform.position),
                            Vector2.Distance(transform.position, target.position), attackRange) &&
                        blocker.TryGetComponent<Rigidbody2D>(out var blockerRb) &&
                        Mathf.Abs(blockerRb.velocity.y) < 0.5f)
                    {
                        // Same fix as the obstacle hop above: this used to be
                        // Mathf.Max(blockerRb.velocity.y, 1.2f) with no airborne gate, so a
                        // blocker pinned in place every frame never dropped below 1.2 upward
                        // velocity and drifted off the top of the screen like a slow balloon.
                        // Gated + flat now: one clean pop per push, then gravity owns it again.
                        blockerRb.velocity = new Vector2(dir * moveSpeed * knightPushForceMultiplier, 1.2f);

                        GameFeelVfx.SpawnFeedbackLabel(blocker.transform.position + Vector3.up * 0.4f,
                            "PUSH!", new Color(1f, 0.9f, 0.5f, 0.9f), 1.5f, 0.35f);
                    }
                }

            }
        }

        private void TrackGroundDirectionFlips(float desiredDirection)
        {
            directionFlipWindowTimer += Time.deltaTime;
            if (directionFlipWindowTimer > 1.5f)
            {
                directionFlipWindowTimer = 0f;
                rapidDirectionFlipCount = 0;
            }

            if (lastMoveDirection != 0f && desiredDirection != 0f && Mathf.Sign(lastMoveDirection) != Mathf.Sign(desiredDirection))
            {
                rapidDirectionFlipCount++;
                if (rapidDirectionFlipCount >= maxDirectionFlipsBeforeRecovery)
                {
                    GameFeelVfx.SpawnFeedbackLabel(transform.position, "LOOP FIX", new Color(1f, 0.85f, 0.25f, 1f), 1.6f, 0.4f);
                    target = null;
                    rapidDirectionFlipCount = 0;
                    directionFlipWindowTimer = 0f;
                    FindTarget();
                }
            }

            lastMoveDirection = desiredDirection;
        }

        private void TryAttack()
        {
            if (Time.time - lastAttackTime < attackCooldown) return;
            lastAttackTime = Time.time;
            GetComponent<UnitSpriteAnimator>()?.PulseAttack();
            attackOrdinal++;
            if (unitType == UnitType.Archer) FireArcherVolley(); else PerformMeleeCombo();
        }

        private void PerformMeleeCombo()
        {
            int hits = unitType == UnitType.Knight ? UnitCombos.KnightHits(attackOrdinal) : 1;
            if (hits <= 1)
            {
                MeleeAttack();
                return;
            }
            StartCoroutine(MeleeComboRoutine(hits));
        }

        private IEnumerator MeleeComboRoutine(int hits)
        {
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.55f,
                hits >= 3 ? "TRIPLE!" : "DOUBLE!", new Color(1f, 0.72f, 0.2f, 1f), 2.3f, 0.6f);
            for (int i = 0; i < hits; i++)
            {
                if (currentState == UnitState.Dead) yield break;
                MeleeAttack();
                yield return new WaitForSeconds(knightComboIntervalSeconds);

            }
        }

        private void FireArcherVolley()
        {
            var kind = UnitCombos.ArcherVolley(attackOrdinal);
            if (kind == UnitCombos.ArcherVolleyKind.Single)
            {
                ShootArrow();
                return;
            }
            StartCoroutine(ArcherVolleyRoutine(kind));
        }

        private IEnumerator ArcherVolleyRoutine(UnitCombos.ArcherVolleyKind kind)
        {
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.55f,
                kind == UnitCombos.ArcherVolleyKind.FrontAndLob ? "SKY VOLLEY!" : "DOUBLE SHOT!",
                new Color(0.6f, 0.95f, 1f, 1f), 2.3f, 0.6f);
            ShootArrow();
            yield return new WaitForSeconds(archerVolleyFollowupDelaySeconds);

            if (currentState == UnitState.Dead) yield break;
            // 10th beat: the follow-up goes AERIAL — a gravity lob over cover.
            ShootArrow(lobbed: kind == UnitCombos.ArcherVolleyKind.FrontAndLob);
        }

        private void MeleeAttack()
        {
            if (target == null) return;
            Color hitColor = unitType == UnitType.Knight ? new Color(1f, 0.9f, 0.45f, 0.9f) : new Color(1f, 0.55f, 0.25f, 0.9f);
            GameFeelVfx.SpawnImpactBurst(target.position, hitColor, 0.35f);
            GameFeelVfx.SpawnShockwaveRing(target.position, hitColor, 0.55f, 0.22f);
            GameFeelVfx.SpawnFeedbackLabel(target.position, unitType == UnitType.Knight ? "SMASH" : "HIT", new Color(1f, 0.92f, 0.45f, 1f), 1.9f, 0.45f);
            GameplayUxDirector.NotifyImpact(target.position, unitType == UnitType.Knight ? "SMASH" : "HIT", new Color(1f, 0.92f, 0.45f, 1f));

            // Cycle 16: Knight deals 1.8x damage to blocks
            float damage = attackDamage * damageMultiplier;
            var block = target.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                if (unitType == UnitType.Knight) damage *= 1.8f;
                block.TakeDamage(damage, isPlayerUnit);
            }
            else
            {
                target.GetComponent<UnitController>()?.TakeDamage(damage, isPlayerUnit);
            }
        }

        private void ShootArrow(bool lobbed = false)
        {
            if (arrowPrefab == null)
            {
#if UNITY_EDITOR
                arrowPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Arrow.prefab");
#endif
                if (arrowPrefab == null)
                {
                    Debug.LogWarning($"[UnitController] arrowPrefab is missing on {gameObject.name} and could not be loaded from Assets/Prefabs/Arrow.prefab!");
                }
            }
            if (target == null || arrowPrefab == null) return;

            var spawnPoint = firePoint != null ? firePoint : transform;
            var arrow = Instantiate(arrowPrefab, spawnPoint.position, Quaternion.identity);
            if (arrow.TryGetComponent<Rigidbody2D>(out var arrowRb))
            {
                Vector2 dir = (target.position - spawnPoint.position).normalized;
                float arrowSpeed = arrow.TryGetComponent<ArrowController>(out var arrowController) ? arrowController.speed : 10f;
                if (lobbed)
                {
                    // Aerial follow-up: steep launch toward the target's side, under gravity,
                    // so the second shot arcs over whatever blocked the straight one.
                    dir = new Vector2(Mathf.Sign(dir.x == 0f ? 1f : dir.x) * 0.55f, 1.1f).normalized;
                    arrowRb.gravityScale = 1f;
                    arrowSpeed *= 1.15f;
                }
                arrowRb.velocity = dir * arrowSpeed;
                arrow.transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg, Vector3.forward);
            }
            arrow.GetComponent<ArrowController>()?.Initialize(attackDamage * damageMultiplier, isPlayerUnit);
        }

        public void TakeDamage(float damage, bool? damageFromPlayer = null)
        {
            if (currentState == UnitState.Dead) return;
            currentHP -= damage;
            GetComponent<UnitSpriteAnimator>()?.FlashHit();
            GameFeelVfx.SpawnDamageNumber(transform.position, damage, isPlayerUnit ? new Color(0.45f, 0.85f, 1f, 1f) : new Color(1f, 0.35f, 0.25f, 1f));
            GameFeelVfx.SpawnImpactBurst(transform.position, new Color(1f, 0.2f, 0.2f, 0.8f), Mathf.Clamp(damage / 120f, 0.18f, 0.55f), null, false);
            GameplayUxDirector.NotifyDamage(transform.position, damage, false);
            if (currentHP <= 0)
            {
                fatalDamageFromPlayer = damageFromPlayer;
                Die();
            }
        }

        private void Die()
        {
            currentState = UnitState.Dead;
            GamePresentationDirector.Instance?.ClearFocus(transform);
            if (unitType == UnitType.Barrel)
            {
                Explode();
                return;
            }
            GameFeelVfx.SpawnCollapseDust(transform.position, 0.28f);
            if (GameManager.Instance != null) GameManager.Instance.OnUnitDied(this, fatalDamageFromPlayer);
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (currentState != UnitState.Launched) return;

            var otherUnit = collision.collider.GetComponentInParent<UnitController>();
            if (otherUnit != null)
            {
                // Friendly bodies do not deal impact damage or force either unit to settle.
                if (otherUnit == this || otherUnit.currentState == UnitState.Dead ||
                    otherUnit.isPlayerUnit == isPlayerUnit)
                {
                    return;
                }

                float impactDamage = attackDamage * 1.5f;
                otherUnit.TakeDamage(impactDamage, isPlayerUnit);
                if (impactDamage > 0f)
                {
                    GameFeelVfx.PlayImpactSfx(Mathf.Clamp(impactDamage / 120f, 0.18f, 0.55f));
                }
                SettleLaunchedUnit();
                return;
            }

            var explosive = collision.gameObject.GetComponent<ExplosiveGimmick>();
            var block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (!collision.gameObject.CompareTag("Ground") && block == null && explosive == null) return;

            if (explosive != null)
            {
                explosive.SetDamageOwner(isPlayerUnit);
                explosive.Explode();
            }

            if (block != null)
            {
                float impactDamage = attackDamage * 1.5f;
                block.TakeDamage(impactDamage, isPlayerUnit);
                if (impactDamage > 0f)
                {
                    GameFeelVfx.PlayImpactSfx(Mathf.Clamp(impactDamage / 35f, 0.45f, 1.8f));
                }
            }

            SettleLaunchedUnit();
            if (unitType == UnitType.Knight && block != null)
            {
                if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(0.05f);
                if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.1f, 0.05f);
            }
        }

        private void SettleLaunchedUnit()
        {
            // Impact damage can synchronously kill this unit (for example, by detonating the
            // opposing Barrel), so never transition a dead body back into a live state.
            if (currentState != UnitState.Launched) return;

            if (unitType == UnitType.Barrel)
            {
                // AOS overhaul (§2): no contact detonation — the keg lands, arms a
                // 2-second fuse with a blink telegraph, THEN blows. Dying early
                // (TakeDamage → Die) still explodes immediately.
                BeginFuse();
                return;
            }

            currentState = UnitState.Grounded;
            GamePresentationDirector.Instance?.ClearFocus(transform);
            if (trailRenderer != null) trailRenderer.emitting = false;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
        }

        /// <summary>
        /// Powder-keg landing fuse (§2): the keg settles where it landed, blinks faster and
        /// faster for BarrelFuseSeconds, then detonates. Being killed first short-circuits
        /// through Die() → Explode(), so the payoff can be denied but never skipped.
        /// </summary>
        private void BeginFuse()
        {
            if (fuseArmed || currentState == UnitState.Dead) return;
            fuseArmed = true;
            currentState = UnitState.Grounded;
            GamePresentationDirector.Instance?.ClearFocus(transform);
            if (trailRenderer != null) trailRenderer.emitting = false;
            if (rb != null)
            {
                rb.velocity = Vector2.zero;
                rb.angularVelocity = 0f;
                rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.45f,
                "FUSE LIT!", new Color(1f, 0.6f, 0.15f, 1f), 2.0f, 0.5f);
            StartCoroutine(FuseRoutine());
        }

        private IEnumerator FuseRoutine()
        {
            float t = 0f;
            var renderers = GetComponentsInChildren<SpriteRenderer>(true);
            while (t < UnitCombos.BarrelFuseSeconds)
            {
                if (currentState == UnitState.Dead) yield break;
                t += Time.deltaTime;
                // Accelerating blink telegraph: white → hot red, faster near detonation.
                float blink = Mathf.PingPong(t * (3f + t * 5f), 1f);
                var tint = Color.Lerp(Color.white, new Color(1f, 0.32f, 0.18f, 1f), blink);
                foreach (var r in renderers) if (r != null) r.color = tint;
                yield return null;
            }
            if (currentState != UnitState.Dead)
            {
                fatalDamageFromPlayer = isPlayerUnit;
                Die();
            }
        }

        private void Explode()
        {
            var expGimmick = GetComponent<ExplosiveGimmick>();
            if (expGimmick != null)
            {
                expGimmick.SetDamageOwner(fatalDamageFromPlayer);
                expGimmick.Explode();
                if (GameManager.Instance != null) GameManager.Instance.OnUnitDied(this, fatalDamageFromPlayer);
                if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
                return;
            }
            if (unitData != null)

            {
                explosionRadius = unitData.explosionRadius;
                explosionDamage = unitData.explosionDamage;
            }
            if (explosionEffectPrefab == null)
            {
#if UNITY_EDITOR
                explosionEffectPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/ExplosionEffect.prefab");
#endif
            }
            if (explosionEffectPrefab != null) Instantiate(explosionEffectPrefab, transform.position, Quaternion.identity);
            GameFeelVfx.SpawnImpactBurst(transform.position, new Color(1f, 0.45f, 0.08f, 0.95f), Mathf.Clamp(explosionRadius * 0.55f, 0.75f, 2.4f));
            GameFeelVfx.SpawnShockwaveRing(transform.position, new Color(1f, 0.5f, 0.08f, 0.65f), explosionRadius * 1.35f, 0.42f);
            GameFeelVfx.SpawnFeedbackLabel(transform.position, "BOOM!", new Color(1f, 0.72f, 0.18f, 1f), 2.4f, 0.65f);
            GameplayUxDirector.NotifyImpact(transform.position, "BOOM!", new Color(1f, 0.72f, 0.18f, 1f));
            if (HitStopManager.Instance != null) HitStopManager.Instance.TriggerHitStop(0.05f);
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.65f);

            var blocks = DestructibleBlock.ActiveOrScene;
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var block = blocks[i];
                if (block != null && Vector2.Distance(transform.position, block.transform.position) <= explosionRadius)
                {
                    block.TakeDamage(explosionDamage, fatalDamageFromPlayer);
                }
            }
            var units = ActiveOrScene;
            for (int i = units.Count - 1; i >= 0; i--)
            {
                var unit = units[i];
                if (unit != null && unit != this && Vector2.Distance(transform.position, unit.transform.position) <= explosionRadius)
                {
                    unit.TakeDamage(explosionDamage, fatalDamageFromPlayer);
                }
            }

            // Physical blast wave (§4): dynamic bodies near the burst get thrown outward —
            // this is what lets a bomb flip or launch the siege chariot and scatter debris.
            // ponytail: full-scene Rigidbody2D scan, one-shot per explosion — registry not worth it
            foreach (var body in FindObjectsOfType<Rigidbody2D>())
            {
                if (body == null || body.gameObject == gameObject || body.bodyType != RigidbodyType2D.Dynamic) continue;
                Vector2 delta = (Vector2)body.transform.position - (Vector2)transform.position;
                float dist = delta.magnitude;
                if (dist > explosionRadius * 1.2f) continue;
                float falloff = 1f - Mathf.Clamp01(dist / (explosionRadius * 1.2f));
                Vector2 dir = dist > 0.01f ? delta / dist : Vector2.up;
                body.AddForce((dir + Vector2.up * 0.6f).normalized * (9f * falloff), ForceMode2D.Impulse);
                body.AddTorque(Random.Range(-4f, 4f) * falloff, ForceMode2D.Impulse);
            }
            if (GameManager.Instance != null) GameManager.Instance.OnUnitDied(this, fatalDamageFromPlayer);
            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }

        public void ApplyBuff(float multiplier, float duration)
        {
            damageMultiplier = multiplier;
            speedMultiplier = multiplier;
            buffTimer = duration;
            debuffTimer = 0f;

            var sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                if (!hasStoredOriginalColor)
                {
                    originalColor = sr.color;
                    hasStoredOriginalColor = true;
                }
                sr.color = new Color(0.2f, 1f, 0.3f, 1f); // Green glow
            }
        }

        public void ApplyDebuff(float multiplier, float duration)
        {
            damageMultiplier = multiplier;
            speedMultiplier = multiplier;
            debuffTimer = duration;
            buffTimer = 0f;

            var sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                if (!hasStoredOriginalColor)
                {
                    originalColor = sr.color;
                    hasStoredOriginalColor = true;
                }
                sr.color = new Color(0.7f, 0.2f, 1f, 1f); // Purple glow
            }
        }

        public void ResetEffects()
        {
            damageMultiplier = 1.0f;
            speedMultiplier = 1.0f;
            buffTimer = 0f;
            debuffTimer = 0f;

            var sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null && hasStoredOriginalColor)
            {
                sr.color = originalColor;
            }
        }

        /// <summary>
        /// Fast flicker toward the base color over the closing ~0.8s of a buff/debuff so a
        /// timer running out reads as a clear warning instead of an instant, unexplained
        /// revert. tintColor is the buff/debuff's own glow color (kept in sync with
        /// ApplyBuff/ApplyDebuff above).
        /// </summary>
        private void ApplyExpiryBlink(Color tintColor, float remaining)
        {
            var sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr == null || !hasStoredOriginalColor) return;
            float blink = 0.5f + 0.5f * Mathf.Sin(Time.time * 22f);
            sr.color = Color.Lerp(originalColor, tintColor, blink);
        }

        /// <summary>Playtest cue: a short "BUFF ENDED"/"DEBUFF ENDED" label so the timing of
        /// the revert is explicit rather than something the player has to infer.</summary>
        private void SpawnBuffDebuffEndedLabel(bool wasBuff)
        {
            if (!Application.isPlaying) return;
            GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 0.4f,
                wasBuff ? "BUFF ENDED" : "DEBUFF ENDED",
                wasBuff ? new Color(0.6f, 1f, 0.7f, 0.85f) : new Color(0.85f, 0.6f, 1f, 0.85f),
                2.0f, 0.55f);
        }


        public void ApplyLaunchPowerMultiplier(float velocityMultiplier, float damageAndSpeedMultiplier, float duration)
        {
            if (rb != null) rb.velocity *= velocityMultiplier;

            if (damageAndSpeedMultiplier >= 1f)
            {
                ApplyBuff(damageAndSpeedMultiplier, duration);
            }
            else
            {
                ApplyDebuff(damageAndSpeedMultiplier, duration);
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = unitType == UnitType.Barrel ? Color.red : Color.yellow;
            Gizmos.DrawWireSphere(transform.position, unitType == UnitType.Barrel ? explosionRadius : attackRange);
        }
    }
}
