using UnityEngine;
using System.Collections.Generic;

namespace CastleBusters
{
    public class ArrowController : MonoBehaviour
    {
        [Header("Gameplay")]
        public float speed = 10f;
        public float lifetime = 5f;

        [Header("Presentation")]
        // Visibility pass (playtest QA): arrows were still hard to track against busy
        // battlefield backgrounds at 1.35u/0.18u thick, so the defaults now sit at the
        // previous hard cap (1.6u/0.24u) and FitArrowToPlayableScale clamps to those caps
        // so no designer override can shrink an arrow back into "hard to see" territory.
        public float visualLength = 1.6f;
        public float visualThickness = 0.24f;
        public Vector2 colliderSize = new Vector2(1.2f, 0.16f);
        public float maxVisualLength = 1.6f;
        public float maxVisualThickness = 0.24f;
        // Bright motion trail so a fast-flying arrow reads clearly mid-flight, not just at
        // its start/end pose (same "잘 보이도록" visibility ask as the size bump above).
        public Color trailColor = new Color(1f, 0.95f, 0.55f, 0.85f);
        public float trailTime = 0.14f;



        private float damage;
        private bool isPlayerArrow;
        private bool hasHit;
        private float damageMultiplier = 1.0f;
        // Opening-volley multiplier, captured once by the shooter (UnitController.ShootArrow)
        // at the moment this arrow was created and stored separately from damageMultiplier
        // (buff/debuff, applied via ApplyBuff/ApplyDebuff mid-flight). Applied exactly once to
        // this arrow's own direct hit, and forwarded unmodified to whatever it ignites after a
        // delayed impact (a field keg's ExplosiveGimmick) — never recomputed from mutable turn
        // state at OnTriggerEnter2D time, so a turn handoff mid-flight cannot change it.
        private float sourceMultiplier = 1.0f;
        private Rigidbody2D rb;

        public static readonly List<ArrowController> Active = new List<ArrowController>();
        private void OnEnable() { Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.gravityScale = 0f;

            // Remap to the packed atlas sprite BEFORE computing scale/collider: doing this
            // after FitArrowToPlayableScale() (as before) sized the transform/collider from
            // the RAW resource sprite's native bounds, then silently swapped in the (often
            // downscaled) packed sprite afterward with no resync — the arrow visibly
            // rendered far smaller than visualLength/visualThickness while the collider
            // still matched the shrunken sprite (same class of bug as the gimmick
            // collider/atlas mismatch fixed in SpriteAtlasPacker).
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                sr.sortingOrder = 4; // Render in front of units without dwarfing them.
                if (SpriteAtlasPacker.Instance != null)
                {
                    sr.sprite = SpriteAtlasPacker.Instance.GetPackedSprite(sr.sprite);
                }
            }
            SetupTrail();


            FitArrowToPlayableScale();
        }

        // Visibility pass: a short bright trail makes a fast arrow readable mid-flight
        // instead of only registering as a blur, without adding any extra colliders/logic.
        private void SetupTrail()
        {
            if (GetComponent<TrailRenderer>() != null) return;
            var trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = trailTime;
            trail.startWidth = visualThickness * 0.9f;
            trail.endWidth = 0.01f;
            trail.minVertexDistance = 0.02f;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.startColor = trailColor;
            trail.endColor = new Color(trailColor.r, trailColor.g, trailColor.b, 0f);
            trail.sortingOrder = 3; // just behind the arrow sprite itself
            trail.emitting = true;
        }


        private void FitArrowToPlayableScale()
        {
            visualLength = Mathf.Max(0.05f, visualLength);
            visualThickness = Mathf.Max(0.02f, visualThickness);

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                Vector2 spriteSize = sr.sprite.bounds.size;
                float scaleX = spriteSize.x > 0f ? visualLength / spriteSize.x : transform.localScale.x;
                float scaleY = spriteSize.y > 0f ? visualThickness / spriteSize.y : transform.localScale.y;
                transform.localScale = new Vector3(scaleX, scaleY, transform.localScale.z);
            }

            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                box.isTrigger = true;
                Vector3 scale = transform.localScale;
                box.size = new Vector2(
                    colliderSize.x / Mathf.Max(0.001f, Mathf.Abs(scale.x)),
                    colliderSize.y / Mathf.Max(0.001f, Mathf.Abs(scale.y)));
                box.offset = Vector2.zero;
            }
        }

        private void FixedUpdate()
        {
            if (hasHit || rb == null) return;
            
            // Only apply wind force if within wind effect radius
            var gm = GameManager.Instance;
            if (gm != null && gm.currentWindForce != 0f)
            {
                float distanceToWindOrigin = Vector2.Distance(transform.position, gm.windEffectOrigin);
                if (distanceToWindOrigin <= gm.windEffectRadius)
                {
                    rb.AddForce(new Vector2(gm.currentWindForce, 0f), ForceMode2D.Force);
                }
            }
            
            if (rb.linearVelocity.sqrMagnitude > 0.1f) transform.rotation = Quaternion.AngleAxis(Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg, Vector3.forward);
        }

        public void Initialize(float damage, float sourceMultiplier, bool isPlayerArrow)
        {
            this.damage = damage;
            this.sourceMultiplier = sourceMultiplier;
            this.isPlayerArrow = isPlayerArrow;
            FitArrowToPlayableScale();
            Destroy(gameObject, lifetime);
        }

        public void ApplyBuff(float multiplier)
        {
            ApplyModifier(multiplier, new Color(0.2f, 1f, 0.3f, 1f));
        }

        public void ApplyDebuff(float multiplier)
        {
            ApplyModifier(multiplier, new Color(0.7f, 0.2f, 1f, 1f));
        }

        private void ApplyModifier(float multiplier, Color color)
        {
            damageMultiplier = multiplier;
            if (rb != null) rb.linearVelocity *= multiplier;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = color;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (hasHit) return;

            var unit = collision.GetComponent<UnitController>();
            if (unit != null && unit.isPlayerUnit != isPlayerArrow && unit.CurrentState != UnitState.Dead)
            {
                GameFeelVfx.SpawnImpactBurst(transform.position, new Color(1f, 0.9f, 0.6f, 0.8f), 0.35f);
                GameFeelVfx.SpawnShockwaveRing(transform.position, new Color(1f, 0.9f, 0.45f, 0.55f), 0.45f, 0.22f);
                GameFeelVfx.SpawnFeedbackLabel(transform.position, "HIT", new Color(1f, 0.95f, 0.45f, 1f), 1.7f, 0.45f);
                unit.TakeDamage(OneShotSiegeRules.ApplyDamageMultiplier(damage * damageMultiplier, sourceMultiplier), isPlayerArrow, sourceMultiplier);
                hasHit = true;
                Destroy(gameObject);
                return;
            }

            var explosive = collision.GetComponent<ExplosiveGimmick>();
            if (explosive != null && unit == null)
            {
                explosive.SetDamageContext(isPlayerArrow, sourceMultiplier);
                explosive.Explode();
                hasHit = true;
                Destroy(gameObject);
                return;
            }

            var block = collision.GetComponent<DestructibleBlock>();
            if (block != null && block.GetComponentInParent<CastleController>()?.isPlayerCastle != isPlayerArrow && !block.IsFalling)
            {
                GameFeelVfx.SpawnImpactBurst(transform.position, new Color(1f, 0.9f, 0.6f, 0.8f), 0.35f);
                GameFeelVfx.SpawnShockwaveRing(transform.position, new Color(1f, 0.9f, 0.45f, 0.55f), 0.45f, 0.22f);
                GameFeelVfx.SpawnFeedbackLabel(transform.position, "HIT", new Color(1f, 0.95f, 0.45f, 1f), 1.7f, 0.45f);
                block.TakeDamage(OneShotSiegeRules.ApplyDamageMultiplier(damage * damageMultiplier, sourceMultiplier), isPlayerArrow, sourceMultiplier);
                hasHit = true;
                Destroy(gameObject);
                return;
            }

            if (collision.CompareTag("Ground"))
            {
                GameFeelVfx.SpawnImpactBurst(transform.position, new Color(0.72f, 0.62f, 0.48f, 0.8f), 0.25f);
                GameFeelVfx.SpawnShockwaveRing(transform.position, new Color(0.72f, 0.62f, 0.48f, 0.45f), 0.35f, 0.2f);
                hasHit = true;
                Destroy(gameObject);
            }
        }

    }
}
