using System.Collections.Generic;
using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// 대포 — the deploy-only artillery installation that replaced the launched Bomber
    /// (design/deployment-economy.md §2, §6).
    ///
    /// It is placed, never launched: no flight state, no walk logic, no target chasing. It
    /// holds the ground it was deployed on, auto-fires a ballistic shell at the nearest
    /// enemy inside <see cref="CannonRules.Range"/> every <see cref="CannonRules.ReloadSeconds"/>,
    /// and dies to 140 damage like any other body — one clean volley hit removes it, which
    /// is the counterplay that keeps a 2-battery wall from being un-answerable.
    ///
    /// Pairs with a <see cref="UnitController"/> carrying <see cref="UnitType.Cannon"/>:
    /// that component owns HP/death/registry membership (so enemy units target the battery
    /// and the deploy cap can count it), while this one owns aiming and firing. The unit's
    /// walk/attack loop is suppressed via <see cref="UnitController.MakeStationaryInstallation"/>.
    /// </summary>
    [RequireComponent(typeof(UnitController))]
    public class CannonController : MonoBehaviour
    {
        public static readonly List<CannonController> Active = new List<CannonController>();

        [Header("Runtime (mirrors CannonRules)")]
        public float range = CannonRules.Range;
        public float reloadSeconds = CannonRules.ReloadSeconds;
        public float shellDamage = CannonRules.ShellDamage;
        public float shellSplashRadius = CannonRules.ShellSplashRadius;

        private UnitController owner;
        private float reloadRemaining;
        private Transform barrelPivot;
        private SpriteRenderer barrelRenderer;

        /// <summary>Seconds until the next shell. Drives the muzzle charge tell.</summary>
        public float ReloadRemaining => reloadRemaining;
        public bool IsPlayerCannon => owner != null && owner.isPlayerUnit;

        private void OnEnable() { Active.Add(this); }
        private void OnDisable() { Active.Remove(this); }

        /// <summary>EditMode has no OnEnable pass — fall back to a scene scan there.</summary>
        public static IReadOnlyList<CannonController> ActiveOrScene =>
            Application.isPlaying ? Active : (IReadOnlyList<CannonController>)FindObjectsOfType<CannonController>();

        public static int CountFor(bool isPlayer)
        {
            var list = ActiveOrScene;
            int n = 0;
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                if (c == null || c.owner == null) continue;
                if (c.owner.CurrentState == UnitState.Dead) continue;
                if (c.owner.isPlayerUnit == isPlayer) n++;
            }
            return n;
        }

        private void Awake()
        {
            owner = GetComponent<UnitController>();
            if (owner != null)
            {
                owner.unitType = UnitType.Cannon;
                owner.maxHP = CannonRules.MaxHP;
                owner.currentHP = CannonRules.MaxHP;
            }
            // First shell is not free: the battery must survive one full reload before it
            // ever fires, so a cannon dropped next to the core cannot burst it on arrival.
            reloadRemaining = reloadSeconds;
        }

        private void Start()
        {
            owner?.MakeStationaryInstallation();
            BuildVisual();
        }

        /// <summary>
        /// Seconds between target re-acquisitions. FindTarget walks every live
        /// DestructibleBlock (the 41x5 terrain grid plus both castles, ~200 entries) and
        /// calls GetComponentInParent on each, which walks the transform hierarchy. Doing
        /// that per battery per frame dominated the frame budget once two batteries per side
        /// were on the field — it doubled the 30-game PlayMode sim's runtime. A battery that
        /// reloads every 3.2 s does not need a fresh scan 60 times a second; re-acquiring 4
        /// times a second is far below the reload clock and stays visually immediate.
        /// </summary>
        private const float RetargetIntervalSeconds = 0.25f;

        private Transform cachedTarget;
        private float retargetTimer;

        private void Update()
        {
            if (!Application.isPlaying) return;
            if (owner == null || owner.CurrentState == UnitState.Dead) return;

            var gm = GameManager.Instance;
            if (gm != null && gm.currentState == GameState.GameOver) return;

            // Re-acquire on a clock, but drop a target the instant it dies or leaves range so
            // the battery never keeps aiming at a corpse for a quarter second.
            retargetTimer -= Time.deltaTime;
            if (retargetTimer <= 0f || !IsTargetStillValid(cachedTarget))
            {
                retargetTimer = RetargetIntervalSeconds;
                cachedTarget = FindTarget();
            }
            var target = cachedTarget;
            AimAt(target);

            // Reload only advances with a live target in the arc — an idle battery holds its
            // shell instead of banking charges to dump the instant something walks in.
            if (target == null)
            {
                reloadRemaining = Mathf.Min(reloadSeconds, reloadRemaining + Time.deltaTime * 0.25f);
                return;
            }

            reloadRemaining -= Time.deltaTime;
            if (reloadRemaining > 0f) return;

            reloadRemaining = reloadSeconds;
            Fire(target);
        }

        /// <summary>
        /// Cheap per-frame validity check on the cached target: destroyed, dead, or out of
        /// range forces an immediate re-acquire without paying for the full scan.
        /// </summary>
        private bool IsTargetStillValid(Transform t)
        {
            if (t == null) return false;
            if (!CannonRules.InRange(Vector2.Distance(transform.position, t.position))) return false;
            var unit = t.GetComponent<UnitController>();
            if (unit != null) return unit.CurrentState != UnitState.Dead;
            var block = t.GetComponent<DestructibleBlock>();
            if (block != null) return !block.IsFalling;
            return true;
        }

        /// <summary>
        /// Nearest legal enemy inside range, priority units → blocks (core included via
        /// DestructibleBlock inheritance). Mirrors UnitController.FindTarget's ground-tile
        /// and own-structure exclusions so a battery never shells the bridge or its own wall.
        /// </summary>
        private Transform FindTarget()
        {
            if (owner == null) return null;
            Vector2 self = transform.position;
            Transform best = null;
            float bestScore = float.MaxValue;

            void Consider(Transform t, float weight)
            {
                float distance = Vector2.Distance(self, t.position);
                if (!CannonRules.InRange(distance)) return;
                float score = TargetingRules.Score(distance, weight);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = t;
                }
            }

            var units = UnitController.ActiveOrScene;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || u == owner) continue;
                if (u.isPlayerUnit == owner.isPlayerUnit) continue;
                if (u.CurrentState == UnitState.Dead) continue;
                Consider(u.transform, TargetingRules.UnitWeight);
            }

            var blocks = DestructibleBlock.Active;
            for (int i = 0; i < blocks.Count; i++)
            {
                var b = blocks[i];
                if (b == null || b.IsFalling) continue;
                if (TargetingRules.IsGroundTile(b.transform.position.y)) continue;

                var castle = b.GetComponentInParent<CastleController>();
                bool isCampGimmick = b.GetComponent<CastleCoreGimmick>() != null || b.GetComponent<ExplosiveGimmick>() != null;
                if (castle != null)
                {
                    if (castle.isPlayerCastle == owner.isPlayerUnit) continue; // never our own wall
                    Consider(b.transform, isCampGimmick ? TargetingRules.GimmickWeight : TargetingRules.StructureWeight);
                }
                else if (TargetingRules.OnEnemyHalf(b.transform.position.x, owner.isPlayerUnit))
                {
                    Consider(b.transform, TargetingRules.GimmickWeight);
                }
            }

            return best;
        }

        private void AimAt(Transform target)
        {
            if (barrelPivot == null) return;
            float facing = owner != null && owner.isPlayerUnit ? 1f : -1f;
            float angle = 28f * facing;
            if (target != null)
            {
                Vector2 delta = (Vector2)target.position - Muzzle();
                // Aim along the ballistic launch vector, not the straight line, so the
                // rendered barrel matches where the shell actually goes.
                Vector2 v = CannonRules.SolveShellVelocity(Muzzle(), target.position, Physics2D.gravity.y);
                if (v.sqrMagnitude > 0.01f) angle = Mathf.Atan2(v.y, v.x) * Mathf.Rad2Deg;
                else if (delta.sqrMagnitude > 0.01f) angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            }
            barrelPivot.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

            if (barrelRenderer != null)
            {
                // Muzzle heat tell: the barrel glows as the reload completes, so an opponent
                // can read "that battery is about to fire" without a HUD element.
                float charge = 1f - Mathf.Clamp01(reloadRemaining / Mathf.Max(0.01f, reloadSeconds));
                Color cool = owner != null && owner.isPlayerUnit
                    ? new Color(0.62f, 0.78f, 0.95f, 1f)
                    : new Color(0.95f, 0.62f, 0.55f, 1f);
                barrelRenderer.color = Color.Lerp(cool, new Color(1f, 0.85f, 0.45f, 1f), charge * charge);
            }

            ApplyRecoil();
        }

        // Recoil: the barrel snaps back along its own axis on firing, then eases forward.
        // Without it the shell simply appeared and the battery looked inert at the one moment
        // it should feel heaviest. Purely visual — the shell's origin is Muzzle(), which is
        // computed from the cannon's transform and is untouched by this offset.
        private const float RecoilSeconds = 0.26f;
        private const float RecoilDistance = 0.3f;
        private const float BarrelRestOffsetX = 0.42f;
        private float recoilTimer;

        private void ApplyRecoil()
        {
            if (barrelRenderer == null) return;

            if (recoilTimer > 0f) recoilTimer = Mathf.Max(0f, recoilTimer - Time.deltaTime);

            // Fast kick back, slower settle: t^3 leaves most of the travel in the first
            // frames, which is what reads as a punch rather than a slide.
            float t = recoilTimer / RecoilSeconds;
            float kick = RecoilDistance * (t * t * t);

            var local = barrelRenderer.transform.localPosition;
            barrelRenderer.transform.localPosition = new Vector3(BarrelRestOffsetX - kick, local.y, local.z);
        }

        private Vector2 Muzzle() => (Vector2)transform.position + Vector2.up * CannonRules.MuzzleHeight;

        private void Fire(Transform target)
        {
            if (target == null) return;
            Vector2 muzzle = Muzzle();
            Vector2 velocity = CannonRules.SolveShellVelocity(muzzle, target.position, Physics2D.gravity.y);

            var shell = CannonShell.Spawn(muzzle, velocity, shellDamage, shellSplashRadius,
                owner != null && owner.isPlayerUnit);
            if (shell == null) return;

            recoilTimer = RecoilSeconds;

            Vector2 dir = velocity.sqrMagnitude > 0.01f ? velocity.normalized : Vector2.right;

            // Drawn blast at the muzzle, rotated onto the shot line so the flare reads as
            // coming out of the barrel. The particle burst and ring below stay: they carry
            // the ember spray and the pressure wave, which a flat sprite cannot.
            var blast = FrameAnimEffect.Spawn(EffectSpriteLibrary.MuzzleBlast,
                muzzle + dir * 0.45f, 1.5f, Color.white, fps: 22f, sortingOrder: 37);
            if (blast != null)
            {
                blast.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
            }

            // Ghost of the gun at full recoil. The recoil itself lasts a quarter of a second,
            // which the eye misses when it is looking downrange at the target.
            CannonShotVisuals.SpawnBarrelAfterimage(barrelRenderer, owner != null && owner.isPlayerUnit);

            GameFeelVfx.SpawnImpactBurst(muzzle + dir * 0.35f, new Color(1f, 0.82f, 0.35f, 0.9f), 0.42f);
            GameFeelVfx.SpawnShockwaveRing(muzzle, new Color(1f, 0.75f, 0.3f, 0.5f), 0.75f, 0.24f);
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.08f, 0.04f);
        }

        /// <summary>
        /// Procedural silhouette (base + rotating barrel) so the installation reads as
        /// artillery without shipping art. Dedicated Resources/Gimmicks art replaces the
        /// base plate when it exists, matching every other gimmick's fail-soft pattern.
        /// </summary>
        private void BuildVisual()
        {
            var baseRenderer = GetComponentInChildren<SpriteRenderer>();
            bool isPlayer = owner == null || owner.isPlayerUnit;
            Color team = isPlayer ? UnitController.AllySpriteTint : UnitController.EnemySpriteTint;

            if (baseRenderer == null)
            {
                var baseGo = new GameObject("CannonBase");
                baseGo.transform.SetParent(transform, false);
                baseRenderer = baseGo.AddComponent<SpriteRenderer>();
                baseRenderer.sprite = SolidSprite(new Vector2Int(28, 16), new Color(0.32f, 0.3f, 0.34f, 1f));
                baseRenderer.sortingOrder = 3;
            }
            if (GimmickSpriteLibrary.TryApply(baseRenderer, GimmickSpriteLibrary.Cannon, team))
            {
                FitSpriteWidth(baseRenderer, BaseWorldWidth);
            }
            else
            {
                baseRenderer.color = team;
            }

            if (barrelPivot == null)
            {
                var pivot = new GameObject("CannonBarrelPivot");
                pivot.transform.SetParent(transform, false);
                pivot.transform.localPosition = new Vector3(0f, CannonRules.MuzzleHeight, 0f);
                barrelPivot = pivot.transform;

                var barrelGo = new GameObject("CannonBarrel");
                barrelGo.transform.SetParent(pivot.transform, false);
                // Offset so the sprite extends forward from the pivot: the pivot rotates, the
                // barrel sweeps around it like a real trunnion instead of spinning in place.
                barrelGo.transform.localPosition = new Vector3(0.42f, 0f, 0f);
                barrelRenderer = barrelGo.AddComponent<SpriteRenderer>();
                barrelRenderer.sprite = SolidSprite(new Vector2Int(30, 9), Color.white);
                barrelRenderer.sortingOrder = 4;
            }

            // The barrel was a plain white rectangle — the most obviously placeholder
            // thing on the battlefield, sitting in front of detailed pixel art.
            if (GimmickSpriteLibrary.TryApply(barrelRenderer, GimmickSpriteLibrary.CannonBarrel, Color.white))
            {
                FitSpriteWidth(barrelRenderer, BarrelWorldWidth);
            }
        }

        // The placeholder sprites were authored at 32 px/unit; art imported from Resources
        // arrives at 100. Normalising to these world widths keeps every tuned offset
        // (muzzle height, trunnion pivot, barrel reach) valid — presentation may not
        // silently resize a gameplay object.
        //
        // CannonArtScale 1.0 → 1.5 (2026-08-13 request: 포 크기 확대). The battery reads as
        // the heaviest thing a turn can buy, so at one block wide it was losing that fight
        // against the soldiers it costs a whole turn to replace. Scaling the ART and the
        // trunnion offset together keeps the barrel on its pivot; CannonRules.MuzzleHeight
        // is deliberately NOT scaled — the shell's origin is ballistics, and moving it
        // would retune every solved arc.
        private const float CannonArtScale = 1.5f;
        private const float BaseWorldWidth = (28f / 32f) * CannonArtScale;
        private const float BarrelWorldWidth = (30f / 32f) * CannonArtScale;

        private static void FitSpriteWidth(SpriteRenderer renderer, float targetWorldWidth)
        {
            if (renderer == null || renderer.sprite == null) return;
            float native = renderer.sprite.bounds.size.x;
            if (native <= 0.0001f) return;

            float scale = targetWorldWidth / native;
            renderer.transform.localScale = new Vector3(scale, scale, 1f);
        }

        private static readonly Dictionary<string, Sprite> solidCache = new Dictionary<string, Sprite>();

        private static Sprite SolidSprite(Vector2Int size, Color color)
        {
            string key = $"{size.x}x{size.y}:{ColorUtility.ToHtmlStringRGBA(color)}";
            if (solidCache.TryGetValue(key, out var cached) && cached != null) return cached;

            var tex = new Texture2D(size.x, size.y);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var pixels = new Color[size.x * size.y];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = color;
            tex.SetPixels(pixels);
            tex.Apply();

            var sprite = Sprite.Create(tex, new Rect(0, 0, size.x, size.y), new Vector2(0.5f, 0.5f), 32f);
            solidCache[key] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// The cannon's ballistic shell: a real gravity-driven projectile (not hitscan), so the
    /// arc over the battery's own wall is physical and the opponent can read the incoming
    /// shot. Detonates on the first enemy body/structure or on the ground.
    /// </summary>
    public class CannonShell : MonoBehaviour
    {
        /// <summary>World radius of the shell. The art is fitted to this, never the reverse —
        /// this is the number collision, balance and tests are written against.</summary>
        public const float ShellRadius = 0.18f;

        public float damage = CannonRules.ShellDamage;
        public float splashRadius = CannonRules.ShellSplashRadius;
        public bool isPlayerShell = true;
        public float lifetime = 8f;
        // Opening-volley multiplier captured once at Spawn (below), applied exactly once at
        // Detonate and forwarded unmodified to every target and static keg the splash reaches
        // — never recomputed from mutable turn state at Detonate time, so a shell's minutes-
        // long ballistic arc (the battery fires on its own reload clock, not the volley gate)
        // cannot pick up a different scale than the one active when it left the muzzle.
        private float sourceMultiplier = 1f;

        private bool detonated;
        private Rigidbody2D rb;

        public static CannonShell Spawn(Vector2 position, Vector2 velocity, float damage, float splashRadius, bool isPlayerShell)
        {
            var go = new GameObject(isPlayerShell ? "PlayerCannonShell" : "EnemyCannonShell");
            go.transform.position = position;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("Cannonball");
            if (sr.sprite == null)
            {
                var tex = new Texture2D(10, 10);
                tex.filterMode = FilterMode.Point;
                for (int y = 0; y < 10; y++)
                    for (int x = 0; x < 10; x++)
                        tex.SetPixel(x, y, Vector2.Distance(new Vector2(x, y), new Vector2(5f, 5f)) < 5f
                            ? new Color(0.16f, 0.15f, 0.18f, 1f) : Color.clear);
                tex.Apply();
                sr.sprite = Sprite.Create(tex, new Rect(0, 0, 10, 10), new Vector2(0.5f, 0.5f), 24f);
            }
            sr.sortingOrder = 5;

            // The shell drew at its sprite's native size while colliding at ShellRadius, so
            // it looked several times bigger than it hit — a ball that visibly overlaps a
            // block without damaging it reads as a broken hitbox. Match the art to the
            // collider instead of the other way round: the collider is what the balance and
            // the tests are written against.
            if (sr.sprite != null)
            {
                float native = Mathf.Max(0.0001f, sr.sprite.bounds.size.x);
                float fit = (ShellRadius * 2f) / native;
                go.transform.localScale = new Vector3(fit, fit, 1f);
            }

            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 1f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.linearVelocity = velocity;

            var circle = go.AddComponent<CircleCollider2D>();
            // Set before the scale above is read anywhere; localScale multiplies this, so the
            // collider is authored in unscaled units and the art is fitted to it.
            circle.radius = ShellRadius / Mathf.Max(0.0001f, go.transform.localScale.x);
            circle.isTrigger = true;

            var shell = go.AddComponent<CannonShell>();
            shell.damage = damage;
            shell.splashRadius = splashRadius;
            shell.isPlayerShell = isPlayerShell;
            shell.sourceMultiplier = GameManager.CaptureDamageMultiplier(isPlayerShell);
            shell.rb = body;

            // Attached after the collider-matched scale above, because the trail sizes its
            // width against that scale.
            CannonShotVisuals.AttachShellTrail(go, isPlayerShell);

            Destroy(go, shell.lifetime);
            return shell;
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (detonated || collision == null) return;

            var unit = collision.GetComponent<UnitController>();
            if (unit != null)
            {
                if (unit.isPlayerUnit == isPlayerShell || unit.CurrentState == UnitState.Dead) return;
                Detonate();
                return;
            }

            var explosive = collision.GetComponent<ExplosiveGimmick>();
            if (explosive != null)
            {
                Detonate();
                return;
            }

            var block = collision.GetComponent<DestructibleBlock>();
            if (block != null)
            {
                if (block.IsFalling) return;
                var castle = block.GetComponentInParent<CastleController>();
                if (castle != null && castle.isPlayerCastle == isPlayerShell) return; // fly over our own wall
                Detonate();
                return;
            }

            if (collision.CompareTag("Ground")) Detonate();
        }

        /// <summary>Splash: every enemy body and structure inside the radius takes full damage.</summary>
        private void Detonate()
        {
            if (detonated) return;
            detonated = true;
            Vector2 at = transform.position;
            // Applied once, here, from the multiplier captured at Spawn — never re-derived
            // from the turn active at the moment this shell happens to land.
            float outgoingDamage = OneShotSiegeRules.ApplyDamageMultiplier(damage, sourceMultiplier);

            var units = UnitController.ActiveOrScene;
            for (int i = 0; i < units.Count; i++)
            {
                var u = units[i];
                if (u == null || u.isPlayerUnit == isPlayerShell || u.CurrentState == UnitState.Dead) continue;
                if (Vector2.Distance(at, u.transform.position) > splashRadius) continue;
                u.TakeDamage(outgoingDamage, isPlayerShell, sourceMultiplier);
            }

            var blocks = DestructibleBlock.Active;
            for (int i = blocks.Count - 1; i >= 0; i--)
            {
                var b = blocks[i];
                if (b == null || b.IsFalling) continue;
                var castle = b.GetComponentInParent<CastleController>();
                if (castle != null && castle.isPlayerCastle == isPlayerShell) continue;
                if (Vector2.Distance(at, b.transform.position) > splashRadius) continue;
                b.TakeDamage(outgoingDamage, isPlayerShell, sourceMultiplier);
            }

            // Static battlefield kegs have no UnitController/HP. Cannon splash still owns
            // the resulting chain, while launched Barrels remain governed by their HP above.
            foreach (var explosive in FindObjectsOfType<ExplosiveGimmick>())
            {
                if (explosive == null || explosive.GetComponent<UnitController>() != null) continue;
                if (Vector2.Distance(at, explosive.transform.position) > splashRadius) continue;
                explosive.SetDamageContext(isPlayerShell, sourceMultiplier);
                explosive.Explode();
            }

            GameFeelVfx.SpawnImpactBurst(at, new Color(1f, 0.72f, 0.3f, 0.95f), 0.7f);
            GameFeelVfx.SpawnShockwaveRing(at, new Color(1f, 0.6f, 0.25f, 0.6f), 1.4f, 0.3f);
            GameFeelVfx.SpawnFeedbackLabel(at, "포격!", new Color(1f, 0.82f, 0.4f, 1f), 1.9f, 0.5f);
            if (ScreenShakeManager.Instance != null) ScreenShakeManager.Instance.TriggerShake(0.12f, 0.06f);

            if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
        }

        private void FixedUpdate()
        {
            if (detonated || rb == null) return;
            if (rb.linearVelocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.AngleAxis(
                    Mathf.Atan2(rb.linearVelocity.y, rb.linearVelocity.x) * Mathf.Rad2Deg, Vector3.forward);
            }
            if (transform.position.y < ChariotRules.KillPlaneY)
            {
                if (Application.isPlaying) Destroy(gameObject); else DestroyImmediate(gameObject);
            }
        }
    }
}
