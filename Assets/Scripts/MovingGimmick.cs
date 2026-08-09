using UnityEngine;

namespace CastleBusters
{
    public class MovingGimmick : MonoBehaviour
    {
        [Header("Movement Settings")]
        public Vector2 moveAxis = Vector2.up;
        public float moveDistance = 3.0f;
        public float moveSpeed = 2.0f;

        [Header("Presentation & Animation")]
        public float targetWorldSize = 3.1f; // Scaled up by 1.4x (from 2.2f to 3.1f) for usability and playability
        public float pulseSpeed = 4.0f;
        public float pulseAmount = 0.15f;
        public float rotationSpeed = 3.0f;
        public float maxRotationAngle = 20f;

        [Header("QA Safeguards")]
        public Rect playableBounds = new Rect(-20f, -1f, 40f, 16f); // widened board pass
        public int maxReversalsPerWindow = 5;
        public float reversalWindowSeconds = 2.0f;
        public float minimumMoveDistance = 0.6f;

        private Vector2 startPosition;
        private Vector3 baseScale;
        private SpriteRenderer spriteRenderer;
        private float previousMoveOffset;
        private float lastMoveDirection;
        private int reversalCount;
        private float reversalWindowTimer;
        private Vector2 lastPosition;
        private float stuckTimer;
        private const float stuckThreshold = 0.01f;
        private const float stuckDuration = 1.0f;

        [Header("Chariot Mode (AOS overhaul §4)")]
        // Field patrols keep the legacy kinematic sine ping-pong (EditMode-pinned).
        // The bridge CHARIOT is a destructible, gravity-bound war machine instead:
        // 3 HP-driven phases, wall ramming, falls when the floor is gone, flips under
        // blast/vent forces, and respawns 5 s after destruction (GameManager owns that).
        public bool chariotMode;
        private DestructibleBlock chariotBody;
        private Rigidbody2D chariotRb;
        private float chariotDir = 1f;
        private float ramReadyAt;
        private float sweepOriginX;
        private ChariotRules.ChariotPhase lastPhase = ChariotRules.ChariotPhase.Patrol;
        // The callout/shockwave used to fire the SAME frame the harder phase's speed and
        // flight pattern took effect, leaving zero reaction window. appliedPhase now lags
        // lastPhase by phaseTelegraphDelaySeconds so the warning genuinely precedes the hazard.
        private ChariotRules.ChariotPhase appliedPhase = ChariotRules.ChariotPhase.Patrol;
        private float phaseApplyAt;
        private const float phaseTelegraphDelaySeconds = 0.45f;

        private bool gameplayDestructionHandled;

        private void Update()
        {
            if (chariotMode)
            {
                UpdateChariot();
                return;
            }

            float time = GetTime();
            Vector2 safeAxis = moveAxis.sqrMagnitude > 0.0001f ? moveAxis.normalized : Vector2.up;

            // 1. Ping-pong movement with loop/bounds safeguards.
            float moveOffset = Mathf.Sin(time * moveSpeed) * moveDistance;
            TrackReversalSafety(moveOffset);
            Vector2 nextPosition = startPosition + safeAxis * moveOffset;
            if (!playableBounds.Contains(nextPosition))
            {
                float inset = 0.001f;
                nextPosition = new Vector2(
                    Mathf.Clamp(nextPosition.x, playableBounds.xMin + inset, playableBounds.xMax - inset),
                    Mathf.Clamp(nextPosition.y, playableBounds.yMin + inset, playableBounds.yMax - inset));
                startPosition = nextPosition - safeAxis * moveOffset;
            }
            transform.position = nextPosition;

            // Stuck detection
            if (moveSpeed > 0.01f && moveDistance > 0.01f)
            {
                if (Vector2.Distance(transform.position, lastPosition) < stuckThreshold)
                {
                    stuckTimer += Application.isPlaying ? Time.deltaTime : 0.02f;
                    if (stuckTimer >= stuckDuration)
                    {
                        moveAxis = -moveAxis;
                        stuckTimer = 0f;
                        if (Application.isPlaying)
                        {
                            GameFeelVfx.SpawnFeedbackLabel(transform.position, "STUCK RESOLVED", new Color(1f, 0.5f, 0.2f, 1f), 1.4f, 0.35f);
                        }
                    }
                }
                else
                {
                    stuckTimer = 0f;
                }
                lastPosition = transform.position;
            }


            // 2. Procedural scale pulsing animation
            float scalePulse = 1f + Mathf.Sin(time * pulseSpeed) * pulseAmount;
            transform.localScale = new Vector3(baseScale.x * scalePulse, baseScale.y * scalePulse, baseScale.z);

            // 3. Procedural rotation bobbing animation
            float rotAngle = Mathf.Sin(time * rotationSpeed) * maxRotationAngle;
            transform.localRotation = Quaternion.Euler(0f, 0f, rotAngle);
        }


        private void TrackReversalSafety(float moveOffset)
        {
            reversalWindowTimer += Application.isPlaying ? Time.deltaTime : 0.02f;
            if (reversalWindowTimer > reversalWindowSeconds)
            {
                reversalWindowTimer = 0f;
                reversalCount = 0;
            }

            float direction = Mathf.Sign(moveOffset - previousMoveOffset);
            if (lastMoveDirection != 0f && direction != 0f && Mathf.Sign(lastMoveDirection) != Mathf.Sign(direction))
            {
                reversalCount++;
                if (reversalCount > maxReversalsPerWindow)
                {
                    moveDistance = Mathf.Max(minimumMoveDistance, moveDistance * 0.75f);
                    moveSpeed = Mathf.Max(0.25f, moveSpeed * 0.75f);
                    reversalCount = 0;
                    reversalWindowTimer = 0f;
                    GameFeelVfx.SpawnFeedbackLabel(transform.position, "PATH FIX", new Color(1f, 0.85f, 0.25f, 1f), 1.4f, 0.35f);
                }
            }

            if (direction != 0f) lastMoveDirection = direction;
            previousMoveOffset = moveOffset;
        }

        private void Awake()
        {
            startPosition = transform.position;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                spriteRenderer.sortingOrder = 4; // Render in front of blocks
                // Dedicated siege-ram art; orange-tinted block fallback keeps old scenes working.
                if (!GimmickSpriteLibrary.TryApply(spriteRenderer, GimmickSpriteLibrary.Ram, Color.white))
                {
                    spriteRenderer.color = new Color(0.9f, 0.4f, 0.1f); // Orange color
                }
            }

            // Ensure it has a Rigidbody2D set to Kinematic so it can move and collide
            var rb = GetComponent<Rigidbody2D>();
            if (rb == null) rb = gameObject.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.useFullKinematicContacts = true;

            // Ensure it has a collider
            var col = GetComponent<Collider2D>();
            if (col == null) gameObject.AddComponent<BoxCollider2D>();

            ApplyPresentationScale();
            baseScale = transform.localScale;
        }

        // Spawners AddComponent() first and assign targetWorldSize right after, so Awake's
        // scale pass always sees the default (3.1) — field patrols spawned ~29% oversized
        // (code review, cycle 3). Start runs after assignment; re-applying is idempotent
        // because the scale is derived from the sprite's native size, not the current scale.
        private void Start()
        {
            ApplyPresentationScale();
            baseScale = transform.localScale;
            if (chariotMode) InitChariot();
        }

        // ---- War beast mode (§4, reworked: the lateral chariot now FLIES) ----

        private float flightTime;

        private void InitChariot()
        {
            sweepOriginX = transform.position.x;
            chariotRb = GetComponent<Rigidbody2D>();
            if (chariotRb == null) chariotRb = gameObject.AddComponent<Rigidbody2D>();
            // Dynamic + zero gravity: the beast holds itself aloft, but blast waves and
            // vent columns still shove it around — knockback displaces, the flight
            // steering then pulls it back onto its pattern ("날아갈 수 있음").
            chariotRb.bodyType = RigidbodyType2D.Dynamic;
            chariotRb.mass = 6f;
            chariotRb.gravityScale = 0f;
            chariotRb.constraints = RigidbodyConstraints2D.FreezeRotation;
            chariotRb.sharedMaterial = new PhysicsMaterial2D { friction = 0.35f, bounciness = 0.05f };

            // DestructibleBlock owns the HP container; its explicit gameplay-destruction
            // callback grants the reward and queues the 5 s redeploy before removing this GO.
            chariotBody = GetComponent<DestructibleBlock>();
            if (chariotBody == null) chariotBody = gameObject.AddComponent<DestructibleBlock>();
            chariotBody.maxHP = chariotBody.currentHP = ChariotRules.MaxHP;
            chariotBody.isGroundAnchor = false;

            // DestructibleBlock.Awake (runs inside AddComponent above) parks the body as
            // Static; reassert the flight body AFTER.
            chariotRb.bodyType = RigidbodyType2D.Dynamic;
            chariotRb.gravityScale = 0f;

            // Dedicated winged art (gti/perfectpixel): looping flap cycle when the frames
            // exist; the static ram sprite stays as the soft fallback.
            GimmickFrameAnimator.TryAttach(gameObject, GimmickAnimLibrary.FlyingBeastAnim, 9f);
        }

        private void UpdateChariot()
        {
            if (chariotRb == null || !Application.isPlaying) return;

            // Blasted off the world: only a live match gets the defeat callout, reward, and redeploy.
            if (transform.position.y < ChariotRules.KillPlaneY || Mathf.Abs(transform.position.x) > 24f)
            {
                DestroyFromKillPlane();
                return;
            }

            float hp = chariotBody != null ? chariotBody.currentHP : ChariotRules.MaxHP;
            float maxHp = chariotBody != null ? chariotBody.maxHP : ChariotRules.MaxHP;
            var phase = ChariotRules.PhaseForHealth(hp, maxHp);
            if (phase != lastPhase)
            {
                lastPhase = phase;
                phaseApplyAt = Time.time + phaseTelegraphDelaySeconds;
                string callout = phase == ChariotRules.ChariotPhase.Rampage ? "WAR BEAST RAMPAGE!"
                    : phase == ChariotRules.ChariotPhase.Frenzy ? "WAR BEAST FRENZY!" : "WAR BEAST PATROL";
                GameFeelVfx.SpawnFeedbackLabel(transform.position + Vector3.up * 1.2f, callout,
                    new Color(1f, 0.55f, 0.2f, 1f), 2.4f, 0.7f);
                GameFeelVfx.SpawnShockwaveRing(transform.position, new Color(1f, 0.5f, 0.15f, 0.6f), 2.2f, 0.4f);
                SiegeAlarmSystem.Post(
                    phase == ChariotRules.ChariotPhase.Rampage ? "야수 돌진 페이즈! 저공 급강하 주의"
                    : phase == ChariotRules.ChariotPhase.Frenzy ? "야수 광란 페이즈! 8자 비행 가속"
                    : "야수 순찰 비행", new Color(1f, 0.6f, 0.25f, 1f));
            }
            if (appliedPhase != lastPhase && Time.time >= phaseApplyAt)
            {
                appliedPhase = lastPhase;
            }

            // Pattern clock scales with phase aggression; the steering is a homing pull so
            // explosions visibly fling the beast off-course before it recovers. Uses
            // appliedPhase (not lastPhase) so the harder pattern only kicks in after the
            // telegraph window above has actually had time to read on screen.
            flightTime += Time.deltaTime * (0.7f + 0.45f * (int)appliedPhase);

            Vector2 wantPos = FlightRules.FlightPoint(appliedPhase, flightTime, sweepOriginX);

            Vector2 steer = (wantPos - (Vector2)transform.position) * FlightRules.SteerGain;
            chariotRb.velocity = Vector2.Lerp(chariotRb.velocity, steer, Time.deltaTime * 4f);
            chariotDir = Mathf.Abs(chariotRb.velocity.x) > 0.05f ? Mathf.Sign(chariotRb.velocity.x) : chariotDir;

            // Face travel + bank into turns; wing-beat scale pulse sells the flight.
            if (spriteRenderer != null) spriteRenderer.flipX = chariotDir < 0f;
            float bank = Mathf.Clamp(-chariotRb.velocity.x * 3.5f, -18f, 18f);
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.Euler(0f, 0f, bank), Time.deltaTime * 5f);
            float pulse = 1f + Mathf.Sin(Time.time * (7f + (int)appliedPhase * 3f)) * 0.04f;

            transform.localScale = new Vector3(baseScale.x * pulse, baseScale.y * pulse, baseScale.z);
        }

        private void OnCollisionStay2D(Collision2D collision)
        {
            if (!chariotMode || !Application.isPlaying || Time.time < ramReadyAt) return;

            // Swoop ramming: structures ABOVE the ground line get crushed on contact —
            // Rampage dive passes are how the beast breaches walls. Terrain is spared.
            var block = collision.gameObject.GetComponent<DestructibleBlock>();
            if (block == null || block == chariotBody) return;
            if (block.transform.position.y < 0f) return;

            ramReadyAt = Time.time + ChariotRules.RamCooldownSeconds;
            block.TakeDamage(ChariotRules.RamDamage);
            GameFeelVfx.SpawnImpactBurst(collision.GetContact(0).point, new Color(1f, 0.6f, 0.25f, 0.9f), 0.5f);
            GameFeelVfx.SpawnFeedbackLabel(collision.GetContact(0).point + Vector2.up * 0.4f,
                "RAM!", new Color(1f, 0.7f, 0.3f, 1f), 1.8f, 0.4f);
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.15f, 0.08f);
        }

        /// <summary>
        /// Handles the match-only consequences of a chariot destruction. This is called by
        /// DestructibleBlock's HP death path and by DestroyFromKillPlane(), never by teardown.
        /// </summary>
        public bool HandleGameplayDestruction()
        {
            if (!chariotMode || gameplayDestructionHandled || !Application.isPlaying) return false;
            gameplayDestructionHandled = true;

            var gm = GameManager.Instance;
            if (gm == null || !gm.isActiveAndEnabled ||
                (gm.currentState != GameState.PlayerTurn && gm.currentState != GameState.AITurn))
            {
                return false;
            }

            ItemDropper.SpawnGuaranteed(transform.position);
            SiegeAlarmSystem.Post("전쟁 야수 격추! 5초 후 재출격", new Color(1f, 0.7f, 0.3f, 1f));
            gm.ScheduleChariotRespawn();
            return true;
        }

        public void DestroyFromKillPlane()
        {
            if (HandleGameplayDestruction())
            {
                GameFeelVfx.SpawnFeedbackLabel(new Vector3(Mathf.Clamp(transform.position.x, -12f, 12f), 1.5f, 0f),
                    "WAR BEAST DOWN!", new Color(1f, 0.5f, 0.2f, 1f), 2.2f, 0.6f);
            }

            Destroy(gameObject);
        }

        private void ApplyPresentationScale()
        {
            if (spriteRenderer == null || spriteRenderer.sprite == null) return;
            Vector2 native = spriteRenderer.sprite.bounds.size;
            float maxNative = Mathf.Max(native.x, native.y);
            if (maxNative <= 0.0001f) return;

            float scale = targetWorldSize / maxNative;
            transform.localScale = new Vector3(scale, scale, 1f);

            if (TryGetComponent<BoxCollider2D>(out var box))
            {
                box.size = native;
                box.offset = spriteRenderer.sprite.bounds.center;
            }
        }

        // Simulated time for EditMode testing
        [HideInInspector]
        public float simulatedTime = 0f;

        private float GetTime() => Application.isPlaying ? Time.time : simulatedTime;
    }
}
