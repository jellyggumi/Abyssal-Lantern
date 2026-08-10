using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace CastleBusters
{
    public static class GameFeelVfx
    {
        private static Sprite cachedRingSprite;
        private static AudioSource presentationAudioSource;
        private static GameObject presentationAudioHost;
        private static AudioClip impactSfx;
        private static AudioClip launchSfx;
        private static AudioClip comboSfx;
        private const string impactSfxPath = "Audio/SFX/impact";
        private const string launchSfxPath = "Audio/SFX/launch";
        private const string comboSfxPath = "Audio/SFX/combo";

        private static AudioSource GetPresentationAudioSource()
        {
            if (!Application.isPlaying) return null;

            if (presentationAudioSource != null) return presentationAudioSource;
            if (presentationAudioHost == null)
            {
                presentationAudioHost = new GameObject("GameFeelVfxAudio");
                Object.DontDestroyOnLoad(presentationAudioHost);
            }

            presentationAudioSource = presentationAudioHost.AddComponent<AudioSource>();
            presentationAudioSource.playOnAwake = false;
            presentationAudioSource.spatialBlend = 0f;
            presentationAudioSource.pitch = 1f;
            presentationAudioSource.loop = false;
            return presentationAudioSource;
        }

        private static AudioClip LoadImpactClip()
        {
            if (impactSfx == null) impactSfx = Resources.Load<AudioClip>(impactSfxPath);
            return impactSfx;
        }

        private static AudioClip LoadLaunchClip()
        {
            if (launchSfx == null) launchSfx = Resources.Load<AudioClip>(launchSfxPath);
            return launchSfx;
        }

        private static AudioClip LoadComboClip()
        {
            if (comboSfx == null) comboSfx = Resources.Load<AudioClip>(comboSfxPath);
            return comboSfx;
        }

        private static void PlayOneShotPresentationSfx(AudioClip clip, float volume)
        {
            if (!Application.isPlaying || clip == null) return;

            var source = GetPresentationAudioSource();
            if (source == null) return;

            source.pitch = 1f;
            source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        public static void PlayImpactSfx(float intensity)
        {
            if (!Application.isPlaying) return;
            var clip = LoadImpactClip();
            float volume = Mathf.Lerp(0.28f, 0.58f, Mathf.Clamp01(intensity / 1.8f));
            PlayOneShotPresentationSfx(clip, volume);
        }

        public static void PlayLaunchSfx(float powerPercent)
        {
            if (!Application.isPlaying) return;
            var clip = LoadLaunchClip();
            float volume = Mathf.Lerp(0.35f, 0.72f, Mathf.Clamp01(powerPercent / 100f));
            PlayOneShotPresentationSfx(clip, volume);
        }

        public static void PlayComboSfx(int comboCount)
        {
            if (!Application.isPlaying) return;
            var clip = LoadComboClip();
            float comboScale = Mathf.Clamp01((comboCount - 1f) / 6f);
            float volume = Mathf.Lerp(0.2f, 0.4f, comboScale);
            PlayOneShotPresentationSfx(clip, volume);
        }

        public static void SpawnDamageNumber(Vector3 position, float amount, Color color)
        {
            if (!Application.isPlaying || amount <= 0f) return;

            var go = new GameObject("DamageNumber");
            go.transform.position = position + new Vector3(0f, 0.45f, 0f);
            var text = go.AddComponent<TextMeshPro>();
            text.text = Mathf.CeilToInt(amount).ToString();

            // Playtest note: every damage number used the same fixed 3.5 size/color
            // regardless of how big the hit actually was, so a 5-damage graze and an
            // 80-damage bomb blast read identically -- no sense of "that one really landed".
            // Tier font size + color by magnitude (25/50 damage are typical mid/heavy hits
            // across arrows/blocks/explosions in this project) so bigger numbers pop bigger
            // and hotter, and crit-tier hits get an extra scale punch from FloatingDamageText.
            float tier = Mathf.InverseLerp(10f, 60f, amount);
            text.fontSize = Mathf.Lerp(3.2f, 5.6f, tier);
            bool isCrit = amount >= 50f;
            text.color = isCrit ? Color.Lerp(color, new Color(1f, 0.55f, 0.15f, 1f), 0.6f) : color;
            text.alignment = TextAlignmentOptions.Center;
            text.sortingOrder = 40;

            var animator = go.AddComponent<FloatingDamageText>();
            animator.lifetime = 0.75f;
            animator.riseDistance = 1.15f;
            animator.critPunch = isCrit;
        }


        public static void SpawnFeedbackLabel(Vector3 position, string message, Color color, float fontSize = 2.7f, float lifetime = 0.65f)
        {
            if (!Application.isPlaying || string.IsNullOrWhiteSpace(message)) return;

            var go = new GameObject("FeedbackLabel");
            go.transform.position = position + new Vector3(0f, 0.72f, 0f);
            var text = go.AddComponent<TextMeshPro>();
            text.text = message;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.Center;
            text.color = color;
            text.sortingOrder = 45;

            var animator = go.AddComponent<FloatingDamageText>();
            animator.lifetime = lifetime;
            animator.riseDistance = 0.85f;
        }
        /// <summary>
        /// Visual launch puff only. Launch audio is emitted by NotifyLaunch; impact audio is
        /// reserved for actual damage so one bow release never plays both sound identities.
        /// </summary>
        public static void SpawnLaunchBurst(Vector3 position, Color color, float intensity = 1f, Sprite sprite = null)
        {
            SpawnImpactBurstCore(position, color, intensity, sprite);
        }

        /// <summary>
        /// Spawns impact visuals and, by default, one impact sound for the owning gameplay
        /// event. Damage receivers that are called from an already-audible event pass
        /// <paramref name="playAudio"/> false so splash/melee hits do not stack one-shots
        /// once per victim.
        /// </summary>
        public static void SpawnImpactBurst(Vector3 position, Color color, float intensity = 1f, Sprite sprite = null, bool playAudio = true)
        {
            if (playAudio) PlayImpactSfx(intensity);
            SpawnImpactBurstCore(position, color, intensity, sprite);
            SpawnHiggsfieldAccent(
                position,
                HiggsfieldSpriteLibrary.Impact,
                new Color(1f, 1f, 1f, 0.9f),
                Mathf.Lerp(0.34f, 0.68f, Mathf.Clamp01(intensity / 1.8f)),
                0.2f,
                36);

            // Playtest note: at high intensity (core hits, breaks) a single burst read as
            // "thin" next to the hit-stop + screen shake it's paired with. Layer a smaller,
            // slightly delayed secondary puff of the same art so big impacts get a visible
            // "crack, then scatter" read instead of one flat pop. Kept behind an intensity
            // gate so ordinary hits stay cheap and don't get busier than they need to, and
            // routed through the Core method (not SpawnImpactBurst) so the delayed puff
            // never re-triggers its own secondary burst.
            if (intensity >= 1.4f)
            {
                SpawnDelayedSecondaryBurst(position, color, intensity, sprite);
            }
        }

        private static void SpawnImpactBurstCore(Vector3 position, Color color, float intensity, Sprite sprite)
        {
            if (!Application.isPlaying) return;

            // Dedicated ember-shard art (generated) replaces the plain radial-gradient dot;
            // callers can still hand in their own sprite (petals, smoke) for themed bursts.
            if (sprite == null) sprite = EffectSpriteLibrary.LoadParticleSprite(EffectSpriteLibrary.ParticleEmber);

            var go = new GameObject("ImpactBurst");
            go.transform.position = position;
            var ps = go.AddComponent<ParticleSystem>();

            // Stop the particle system before configuring to avoid "Setting duration while playing" warnings
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.duration = 0.35f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.55f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.5f * intensity, 4.5f * intensity);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f * intensity, 0.22f * intensity);
            main.startColor = color;
            main.gravityModifier = 0.35f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)Mathf.Clamp(Mathf.RoundToInt(12f * intensity), 6, 40)) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f * intensity;

            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            if (sprite != null)
            {
                var textureSheet = ps.textureSheetAnimation;
                textureSheet.enabled = true;
                textureSheet.mode = ParticleSystemAnimationMode.Sprites;
                textureSheet.AddSprite(sprite);
            }

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.sortingOrder = 35;
            renderer.sharedMaterial = GetParticleMaterial(sprite != null ? sprite.texture : null);

            // Play now that configuration is complete
            ps.Play();

            Object.Destroy(go, 1.2f);
        }


        private static void SpawnDelayedSecondaryBurst(Vector3 position, Color color, float intensity, Sprite sprite)
        {
            // EditMode tests call gameplay code (TakeDamage/Explode) directly without ever
            // entering play mode; DontDestroyOnLoad throws there ("can only be used in play
            // mode"). SpawnImpactBurstCore already no-ops outside Application.isPlaying, so
            // guard the coroutine runner the same way instead of letting it explode 4 tests.
            if (!Application.isPlaying) return;

            if (secondaryBurstRunner == null)
            {
                var runnerGo = new GameObject("GameFeelVfxSecondaryBurstRunner");
                Object.DontDestroyOnLoad(runnerGo);
                secondaryBurstRunner = runnerGo.AddComponent<GameFeelVfxCoroutineRunner>();
            }
            secondaryBurstRunner.StartCoroutine(SecondaryBurstRoutine(position, color, intensity, sprite));
        }


        private static System.Collections.IEnumerator SecondaryBurstRoutine(Vector3 position, Color color, float intensity, Sprite sprite)
        {
            yield return new WaitForSeconds(0.06f);
            if (!Application.isPlaying) yield break;
            var scatterOffset = new Vector3(Random.Range(-0.18f, 0.18f), Random.Range(-0.1f, 0.14f), 0f);
            SpawnImpactBurstCore(position + scatterOffset, color, intensity * 0.5f, sprite);
        }

        private static GameFeelVfxCoroutineRunner secondaryBurstRunner;

        public static void SpawnHiggsfieldAccent(
            Vector3 position,
            string key,
            Color color,
            float finalRadius,
            float lifetime,
            int sortingOrder = 36)
        {
            if (!Application.isPlaying) return;

            Sprite sprite = HiggsfieldSpriteLibrary.LoadVfx(key);
            if (sprite == null) return;

            var go = new GameObject($"Higgsfield{key}Accent");
            go.transform.position = position + new Vector3(0f, 0f, -0.015f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;

            var pulse = go.AddComponent<GameFeelRingPulse>();
            pulse.lifetime = Mathf.Max(0.08f, lifetime);
            pulse.finalRadius = Mathf.Max(0.05f, finalRadius);
            pulse.startColor = color;
        }


        public static void SpawnShockwaveRing(Vector3 position, Color color, float finalRadius = 1.2f, float lifetime = 0.35f)
        {
            if (!Application.isPlaying) return;

            var go = new GameObject("ShockwaveRing");
            go.transform.position = position + new Vector3(0f, 0f, -0.01f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = GetRingSprite();
            sr.color = color;
            sr.sortingOrder = 34;

            var ring = go.AddComponent<GameFeelRingPulse>();
            ring.lifetime = Mathf.Max(0.08f, lifetime);
            ring.finalRadius = Mathf.Max(0.05f, finalRadius);
            ring.startColor = color;
        }

        public static void SpawnCollapseDust(Vector3 position, float intensity = 1f, Sprite sprite = null)
        {
            if (sprite == null) sprite = EffectSpriteLibrary.LoadParticleSprite(EffectSpriteLibrary.ParticleSmoke);
            SpawnImpactBurst(position, new Color(0.72f, 0.62f, 0.48f, 0.85f), intensity, sprite);
            SpawnHiggsfieldAccent(
                position,
                HiggsfieldSpriteLibrary.CollapseDust,
                new Color(1f, 1f, 1f, 0.88f),
                Mathf.Clamp(0.48f * intensity, 0.4f, 0.85f),
                0.42f,
                35);
        }

        private static Sprite GetRingSprite()
        {
            if (cachedRingSprite != null) return cachedRingSprite;

            const int size = 48;
            const float outerRadius = 22f;
            const float innerRadius = 17f;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };

            var clear = new Color(1f, 1f, 1f, 0f);
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x + 0.5f - size * 0.5f;
                    float dy = y + 0.5f - size * 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    texture.SetPixel(x, y, d <= outerRadius && d >= innerRadius ? Color.white : clear);
                }
            }

            texture.Apply(false, true);
            cachedRingSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
            cachedRingSprite.name = "GeneratedShockwaveRing";
            return cachedRingSprite;
        }

        private static Texture2D cachedParticleTexture;
        private static readonly Dictionary<Texture2D, Material> cachedParticleMaterials = new Dictionary<Texture2D, Material>();


        public static Texture2D GetDefaultParticleTexture()
        {
            if (cachedParticleTexture != null) return cachedParticleTexture;

            const int size = 32;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    float alpha = Mathf.Clamp01(1f - (dist / center));
                    alpha = Mathf.Pow(alpha, 1.5f); // smooth falloff
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply(false, true);
            cachedParticleTexture = texture;
            return cachedParticleTexture;
        }

        public static Material GetParticleMaterial(Texture2D customTexture = null)
        {
            Texture2D texture = customTexture != null ? customTexture : GetDefaultParticleTexture();
            if (cachedParticleMaterials.TryGetValue(texture, out Material material)) return material;

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) return null;

            material = new Material(shader);
            material.mainTexture = texture;
            cachedParticleMaterials.Add(texture, material);
            return material;
        }
    }


    public class GameplayUxDirector : MonoBehaviour
    {
        public static GameplayUxDirector Instance { get; private set; }

        private static Sprite cachedUiSprite;
        private Canvas canvas;
        private RectTransform root;
        private TextMeshProUGUI turnToastText;
        private TextMeshProUGUI comboText;
        private GameObject comboBackplate;
        private GameObject toastBackplate;
        private RectTransform turnToastRt;
        private RectTransform toastBackplateRt;
        private Image toastBackplateImg;
        private Image turnProgressFill;
        private CoreHealthBadge playerCoreBadge;
        private CoreHealthBadge enemyCoreBadge;
        private CastleCoreGimmick playerCore;
        private CastleCoreGimmick enemyCore;
        private string lastTurnLabel = string.Empty;
        private int comboCount;
        // Session-best combo for the results screen; reset by ResetSessionStats().
        public static int SessionMaxCombo { get; private set; }
        public static void ResetSessionStats() => SessionMaxCombo = 0;
        private float lastComboTime;
        private float toastUntil;
        private float nextHazardPulse;
        private float toastDuration;
        private float toastStartTime;
        private Vector2 toastBaseAnchoredPos = new Vector2(0f, 200f);
        private float comboPopTimer;
        private bool hasStoredToastBasePos = false;
        private bool playerCoreLowAnnounced;
        private bool enemyCoreLowAnnounced;
        private Image dangerVignette;
        private bool dangerActive;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            EnsureHud();
            RefreshCoreReferences();
            ShowToast("적 코어를 부숴라", new Color(1f, 0.86f, 0.25f, 1f), 2.2f);
        }

        private void Update()
        {
            // The gameplay HUD must stay invisible while an overlay owns the screen: the intro
            // title card AND the results screen. Time.time is frozen under timeScale 0, so a
            // "BREACH COMPLETE" toast or combo counter raised at the killing blow would
            // otherwise hang behind the results card forever.
            var state = GameManager.Instance != null ? GameManager.Instance.currentState : GameState.PlayerTurn;
            bool overlayOwnsScreen = state == GameState.Intro || state == GameState.GameOver;
            if (overlayOwnsScreen)
            {
                if (root != null && root.gameObject.activeSelf) root.gameObject.SetActive(false);
                return;
            }
            EnsureHud();
            if (root != null && !root.gameObject.activeSelf) root.gameObject.SetActive(true);
            RefreshCoreReferences();
            UpdateToastExpiry();
            UpdateTurnProgress();
            UpdateCoreBadges();
            UpdateCoreWarningToasts();
            UpdateComboTimeout();
            PulseHazardLabels();
            AnimateToastAndCombo();
            UpdateDangerVignette();
        }

        private void AnimateToastAndCombo()
        {
            if (turnToastText != null && turnToastText.gameObject.activeSelf)
            {
                var rt = turnToastRt; // cached at creation in EnsureHud — no per-frame GetComponent
                if (rt != null)
                {
                    if (!hasStoredToastBasePos)
                    {
                        toastBaseAnchoredPos = rt.anchoredPosition;
                        hasStoredToastBasePos = true;
                    }

                    float elapsed = Time.time - toastStartTime;
                    float t = toastDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / toastDuration);

                    float alpha = 1f;
                    float yOffset = 0f;
                    if (t < 0.15f)
                    {
                        float norm = t / 0.15f;
                        alpha = norm;
                        yOffset = Mathf.Lerp(30f, 0f, 1f - Mathf.Pow(1f - norm, 2f));
                    }
                    else if (t > 0.85f)
                    {
                        float norm = (1f - t) / 0.15f;
                        alpha = norm;
                        yOffset = Mathf.Lerp(-30f, 0f, 1f - Mathf.Pow(1f - norm, 2f));
                    }

                    var color = turnToastText.color;
                    color.a = alpha;
                    turnToastText.color = color;
                    rt.anchoredPosition = toastBaseAnchoredPos + new Vector2(0f, yOffset);

                    if (toastBackplate != null)
                    {
                        if (toastBackplateRt != null)
                        {
                            toastBackplateRt.anchoredPosition = toastBaseAnchoredPos + new Vector2(0f, yOffset);
                        }
                        if (toastBackplateImg != null)
                        {
                            toastBackplateImg.color = new Color(0f, 0.05f, 0.1f, 0.58f * alpha);
                        }
                    }
                }
            }

            if (comboText != null && comboText.gameObject.activeSelf)
            {
                comboPopTimer += Time.deltaTime;
                float t = Mathf.Clamp01(comboPopTimer / 0.25f);
                float scaleMultiplier = t < 0.5f
                    ? Mathf.Lerp(1f, 1.3f, t / 0.5f)
                    : Mathf.Lerp(1.3f, 1f, (t - 0.5f) / 0.5f);

                Vector3 comboScale = Vector3.one * scaleMultiplier * Mathf.Clamp(1f + comboCount * 0.04f, 1f, 1.35f);
                comboText.transform.localScale = comboScale;
                if (comboBackplate != null) comboBackplate.transform.localScale = comboScale;
            }
        }

        // ---- Danger state (comeback layer) ----

        /// <summary>Persistent low-core warning: pulsing red frame + heartbeat on the badge.</summary>
        public static void SetDangerState(bool active)
        {
            if (Instance == null) return;
            Instance.dangerActive = active;
            if (active)
            {
                Instance.ShowToast("위기! R로 일발역전 대기", new Color(1f, 0.35f, 0.2f, 1f), 2.4f);
            }
        }

        public static void NotifyLastStandArmed(bool isPlayer)
        {
            Instance?.ShowToast(isPlayer
                ? "일발역전 준비 완료 — R"
                : "적이 필사적입니다",
                isPlayer ? new Color(1f, 0.55f, 0.15f, 1f) : new Color(1f, 0.4f, 0.45f, 1f), 2.6f);
        }

        public static void NotifyLastStandActive()
        {
            Instance?.ShowToast("일발역전! 다음 발사 ×2.2", new Color(1f, 0.42f, 0.12f, 1f), 2.4f);
        }

        private void UpdateDangerVignette()
        {
            if (root == null) return;
            if (dangerVignette == null)
            {
                var go = new GameObject("DangerVignette");
                go.transform.SetParent(root, false);
                go.transform.SetAsFirstSibling(); // behind every HUD element
                dangerVignette = go.AddComponent<Image>();
                dangerVignette.raycastTarget = false;
                dangerVignette.color = new Color(0.85f, 0.08f, 0.05f, 0f);
                var rt = go.GetComponent<RectTransform>();
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                dangerVignette.gameObject.SetActive(false);
            }

            if (!dangerActive)
            {
                if (dangerVignette.gameObject.activeSelf) dangerVignette.gameObject.SetActive(false);
                return;
            }

            if (!dangerVignette.gameObject.activeSelf) dangerVignette.gameObject.SetActive(true);
            // Heartbeat: two quick beats per ~1.1s cycle, alpha 0.05..0.22.
            float cycle = Time.time % 1.1f;
            float beat = Mathf.Max(
                Mathf.Exp(-18f * Mathf.Abs(cycle - 0.12f)),
                0.75f * Mathf.Exp(-18f * Mathf.Abs(cycle - 0.38f)));
            var c = dangerVignette.color;
            c.a = 0.05f + 0.17f * beat;
            dangerVignette.color = c;
        }

        public static void NotifyTurnChanged(bool isPlayerTurn)
        {
            Instance?.ShowToast(isPlayerTurn ? "내 턴" : "적 턴", isPlayerTurn ? new Color(0.55f, 0.9f, 1f, 1f) : new Color(1f, 0.45f, 0.28f, 1f), 1.7f);
        }

        public static void NotifyLaunch(string unitName, float powerPercent, float angle)
        {
            GameFeelVfx.PlayLaunchSfx(powerPercent);
            Instance?.ShowLaunchToast(unitName, powerPercent, angle);
        }

        public static void NotifyDamage(Vector3 position, float amount, bool isCore)
        {
            if (Instance == null) return;
            Instance.RegisterCombo(isCore ? "CORE HIT" : "HIT", isCore ? new Color(1f, 0.72f, 0.18f, 1f) : new Color(1f, 0.92f, 0.35f, 1f));
            if (isCore)
            {
                GameFeelVfx.SpawnShockwaveRing(position, new Color(1f, 0.72f, 0.18f, 0.55f), 1.9f, 0.42f);
                GameFeelVfx.SpawnFeedbackLabel(position + Vector3.up * 0.35f, "CORE HIT!", new Color(1f, 0.72f, 0.18f, 1f), 2.5f, 0.7f);
                GameFeelVfx.SpawnHiggsfieldAccent(
                    position,
                    HiggsfieldSpriteLibrary.CoreCrack,
                    new Color(1f, 1f, 1f, 0.94f),
                    0.62f,
                    0.36f,
                    37);
            }
        }

        public static void NotifyBreak(Vector3 position, bool isCore)
        {
            if (Instance == null) return;
            Instance.RegisterCombo(isCore ? "CORE BREAK" : "BLOCK BREAK", isCore ? new Color(1f, 0.3f, 0.12f, 1f) : new Color(1f, 0.78f, 0.25f, 1f));
            if (isCore) Instance.ShowToast("코어 파괴!", new Color(1f, 0.35f, 0.12f, 1f), 2f);
        }

        public static void NotifyImpact(Vector3 position, string label, Color color)
        {
            Instance?.RegisterCombo(label, color);
            GameFeelVfx.SpawnFeedbackLabel(position, label, color, 2.0f, 0.5f);
        }

        // --- Turn-handling coach notifications (2026-07-02 playtest pass) ---

        public static void NotifyTurnUrgency(int secondsLeft)
        {
            Instance?.ShowToast($"{secondsLeft}초 — 지금 발사!", new Color(1f, 0.62f, 0.2f, 1f), 1.6f);
        }

        public static void NotifyIdleNudge()
        {
            Instance?.ShowToast("푸른 링에서 당겨 발사", new Color(0.55f, 0.9f, 1f, 0.95f), 1.4f);
        }

        public static void NotifyAimGrace(float graceSeconds)
        {
            Instance?.ShowToast($"조준 유지 — {graceSeconds:F0}초 연장", new Color(1f, 0.9f, 0.4f, 1f), 1.6f);
        }

        public static void NotifyTurnForfeited()
        {
            Instance?.ShowToast("발사 기회를 놓쳤습니다", new Color(1f, 0.45f, 0.28f, 1f), 1.8f);
        }

        private void ShowLaunchToast(string unitName, float powerPercent, float angle)
        {
            string grade = powerPercent >= 78f ? "FULL-DRAW VOLLEY" : powerPercent >= 42f ? "CLEAN SIEGE ARC" : "LIGHT LOB";
            ShowToast($"{grade}: {unitName}  {powerPercent:F0}% / {angle:F0}°", new Color(0.65f, 0.95f, 1f, 1f), 1.45f);
            RegisterCombo("VOLLEY", new Color(0.65f, 0.95f, 1f, 1f));
        }

        private void RegisterCombo(string label, Color color)
        {
            if (Time.time - lastComboTime > 3.0f) comboCount = 0;
            comboCount++;
            if (comboCount > SessionMaxCombo) SessionMaxCombo = comboCount;
            lastComboTime = Time.time;
            comboPopTimer = 0f;

            if (comboCount > 1)
            {
                GameFeelVfx.PlayComboSfx(comboCount);
            }

            if (comboText != null)
            {
                comboText.text = comboCount <= 1 ? label : $"{label} x{comboCount}";
                comboText.color = color;
                comboText.transform.localScale = Vector3.one;

                // A visible turn/launch toast owns this central lane; retain the combo state
                // for accounting and subsequent events without rendering a competing banner.
                bool toastOwnsCentralLane = turnToastText != null
                    && turnToastText.gameObject.activeSelf
                    && Time.time <= toastUntil;
                comboText.gameObject.SetActive(!toastOwnsCentralLane);
                if (comboBackplate != null)
                {
                    comboBackplate.transform.localScale = Vector3.one;
                    comboBackplate.SetActive(!toastOwnsCentralLane);
                }
            }
        }

        private void ShowToast(string message, Color color, float duration)
        {
            if (turnToastText == null) return;
            turnToastText.text = message;
            turnToastText.color = color;
            toastUntil = Time.time + duration;
            toastDuration = duration;
            toastStartTime = Time.time;
            turnToastText.gameObject.SetActive(true);
            if (toastBackplate != null) toastBackplate.SetActive(true);

            // A toast entering the central lane takes precedence over a banner already shown.
            if (comboText != null) comboText.gameObject.SetActive(false);
            if (comboBackplate != null) comboBackplate.SetActive(false);
        }

        private void UpdateComboTimeout()
        {
            if (Time.time - lastComboTime > 3.0f)
            {
                if (comboText != null && comboText.gameObject.activeSelf)
                {
                    comboText.gameObject.SetActive(false);
                    comboText.transform.localScale = Vector3.one;
                    if (comboBackplate != null)
                    {
                        comboBackplate.SetActive(false);
                        comboBackplate.transform.localScale = Vector3.one;
                    }
                }
                comboCount = 0;
            }
        }

        private void EnsureHud()
        {
            if (root != null) return;
            // Never adopt the intro title canvas as the HUD host - it is transient and sits on
            // a much higher sorting order than gameplay UI.
            canvas = null;
            // ponytail: one-shot HUD init, not per-frame — registry swap if canvases ever churn
            foreach (var c in FindObjectsOfType<Canvas>())
            {
                if (c.GetComponent<IntroScreenController>() != null) continue;
                canvas = c;
                break;
            }
            if (canvas == null)
            {
                var canvasGo = new GameObject("GameplayCanvas");
                canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();
            }
            else
            {
                var scaler = canvas.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
            MobileSafeArea.ConfigureCanvas(canvas);

            var rootGo = new GameObject("GameplayUxDirectorHUD");
            rootGo.transform.SetParent(MobileSafeArea.GetContentRoot(canvas), false);
            root = rootGo.AddComponent<RectTransform>();
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            comboBackplate = CreatePanel("ComboBackplate", new Vector2(0.60f, 0.60f), new Vector2(0.5f, 0.5f), new Vector2(400f, 52f), new Color(0.09f, 0.045f, 0.01f, 0.46f));
            toastBackplate = CreatePanel("ToastBackplate", new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(800f, 64f), new Color(0f, 0.05f, 0.1f, 0.58f));
            toastBackplateRt = toastBackplate.GetComponent<RectTransform>();
            toastBackplateImg = toastBackplate.GetComponent<Image>();

            // Keep the transient coaching lane vertically high without shifting it toward the
            // right edge on narrower aspect ratios.
            turnToastText = CreateText("TurnToastText", new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.5f), new Vector2(800f, 64f), 28, TextAlignmentOptions.Center, Color.white);
            turnToastRt = turnToastText.rectTransform;
            comboText = CreateText("ComboText", new Vector2(0.60f, 0.60f), new Vector2(0.5f, 0.5f), new Vector2(400f, 52f), 24, TextAlignmentOptions.Center, new Color(1f, 0.85f, 0.22f, 1f));
            comboBackplate.SetActive(false);
            if (toastBackplate != null) toastBackplate.SetActive(false);

            var barBg = CreatePanel("TurnProgressBackground", new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(620f, 10f), new Color(0f, 0f, 0f, 0.45f));
            barBg.GetComponent<RectTransform>().anchoredPosition = new Vector2(0f, -78f);
            var fillGo = CreatePanel("TurnProgressFill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(620f, 10f), new Color(0.35f, 0.85f, 1f, 0.85f));
            fillGo.transform.SetParent(barBg.transform, false);
            turnProgressFill = fillGo.GetComponent<Image>();

            // The serialized turn label occupies top offsets 20-70. Keep progress, wind, and
            // countdown in successive fixed top bands so their spacing survives aspect changes.
            var timerText = GameManager.Instance != null ? GameManager.Instance.timerText : null;
            if (timerText != null)
            {
                var timerRt = timerText.rectTransform;
                timerRt.anchorMin = timerRt.anchorMax = new Vector2(0.5f, 1f);
                timerRt.pivot = new Vector2(0.5f, 1f);
                timerRt.anchoredPosition = new Vector2(0f, -134f);
                timerRt.sizeDelta = new Vector2(100f, 40f);
            }

            playerCoreBadge = new CoreHealthBadge(root, "KEEP CORE", new Vector2(0.18f, 0.84f), new Color(0.25f, 0.75f, 1f, 1f));
            enemyCoreBadge = new CoreHealthBadge(root, "BREACH CORE", new Vector2(0.82f, 0.84f), new Color(1f, 0.62f, 0.18f, 1f));
        }

        private TextMeshProUGUI CreateText(string name, Vector2 anchor, Vector2 pivot, Vector2 size, int fontSize, TextAlignmentOptions alignment, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var text = go.AddComponent<TextMeshProUGUI>();
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 3f;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            text.outlineWidth = 0.18f;
            text.outlineColor = new Color(0.025f, 0.018f, 0.01f, 0.92f);
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return text;
        }

        private GameObject CreatePanel(string name, Vector2 anchor, Vector2 pivot, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var image = go.AddComponent<Image>();
            image.sprite = GetUiSprite();
            image.color = color;
            image.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = Vector2.zero;
            return go;
        }

        private void RefreshCoreReferences()
        {
            if (playerCore != null && enemyCore != null) return;
            for (int i = 0; i < DestructibleBlock.Active.Count; i++)
            {
                if (DestructibleBlock.Active[i] is CastleCoreGimmick core)
                {
                    if (core.isPlayerCore) playerCore = core; else enemyCore = core;
                }
            }
        }

        // UX text-diet pass: the objective band, wind hint, command strip, and WAR ROOM
        // coaching panel were removed — they quadrupled the persistent on-screen text while
        // duplicating info the core HUD (turn text, wind text, unit cards) already shows.
        private void UpdateToastExpiry()
        {
            if (turnToastText != null && turnToastText.gameObject.activeSelf && Time.time > toastUntil)
            {
                turnToastText.gameObject.SetActive(false);
                if (toastBackplate != null) toastBackplate.SetActive(false);
            }
        }

        private void UpdateTurnProgress()
        {
            if (turnProgressFill == null || GameManager.Instance == null) return;
            float ratio = Mathf.Clamp01(GameManager.Instance.TurnTimeRemaining / Mathf.Max(0.01f, GameManager.Instance.turnDuration));
            turnProgressFill.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
            turnProgressFill.color = ratio < 0.25f ? new Color(1f, 0.38f, 0.22f, 0.9f) : new Color(0.35f, 0.85f, 1f, 0.85f);
            string label = GameManager.Instance.IsPlayerTurn ? "YOUR TURN" : "ENEMY TURN";
            if (label != lastTurnLabel)
            {
                lastTurnLabel = label;
                NotifyTurnChanged(GameManager.Instance.IsPlayerTurn);
            }
        }


        private void UpdateCoreWarningToasts()
        {
            if (playerCore != null && !playerCoreLowAnnounced && playerCore.currentHP <= playerCore.maxHP * 0.35f)
            {
                playerCoreLowAnnounced = true;
                ShowToast("KEEP WARNING — YOUR CORE IS CRACKING", new Color(1f, 0.45f, 0.2f, 1f), 1.8f);
            }

            if (enemyCore != null && !enemyCoreLowAnnounced && enemyCore.currentHP <= enemyCore.maxHP * 0.35f)
            {
                enemyCoreLowAnnounced = true;
                ShowToast("ENEMY CORE EXPOSED — ORDER THE BREACH", new Color(1f, 0.86f, 0.28f, 1f), 1.8f);
            }
        }

        private void UpdateCoreBadges()
        {
            playerCoreBadge?.Update(playerCore, Camera.main);
            enemyCoreBadge?.Update(enemyCore, Camera.main);
        }

        private void PulseHazardLabels()
        {
            if (Time.time < nextHazardPulse) return;
            nextHazardPulse = Time.time + 2.8f;
            // ponytail: 2.8s cadence, not per-frame — registry swap when gimmick count grows
            foreach (var explosive in FindObjectsOfType<ExplosiveGimmick>())
            {
                GameFeelVfx.SpawnFeedbackLabel(explosive.transform.position + Vector3.up * 0.7f, "POWDER KEG", new Color(1f, 0.38f, 0.1f, 0.85f), 1.25f, 0.35f);
            }
            foreach (var moving in FindObjectsOfType<MovingGimmick>())
            {
                GameFeelVfx.SpawnFeedbackLabel(moving.transform.position + Vector3.up * 0.7f, "SIEGE ENGINE", new Color(0.75f, 0.9f, 1f, 0.85f), 1.2f, 0.35f);
            }
            foreach (var zone in FindObjectsOfType<BuffDebuffGimmick>())
            {
                string label = zone.effectType == GimmickEffectType.Buff ? "RALLY RUNE" : "HEX FIELD";
                Color color = zone.effectType == GimmickEffectType.Buff ? new Color(0.35f, 1f, 0.45f, 0.86f) : new Color(0.9f, 0.35f, 1f, 0.86f);
                GameFeelVfx.SpawnFeedbackLabel(zone.transform.position + Vector3.up * 0.9f, label, color, 1.15f, 0.35f);
            }
        }

        private static Sprite GetUiSprite()
        {
            if (cachedUiSprite != null) return cachedUiSprite;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, true);
            cachedUiSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            cachedUiSprite.name = "GeneratedUiWhitePixel";
            return cachedUiSprite;
        }

        private class CoreHealthBadge
        {
            private readonly RectTransform container;
            private readonly Image fill;
            private readonly TextMeshProUGUI label;
            private readonly Color tint;
            private readonly string title;
            private Vector2 baseAnchoredPosition;
            private float shakeTimer;
            private float shakeIntensity;
            private float lastHP = -1f;
            private bool hasStoredBasePos;

            public CoreHealthBadge(RectTransform parent, string title, Vector2 anchor, Color tint)
            {
                this.tint = tint;
                this.title = title;
                var go = new GameObject(title.Replace(" ", "") + "Badge");
                go.transform.SetParent(parent, false);
                container = go.AddComponent<RectTransform>();
                container.anchorMin = anchor;
                container.anchorMax = anchor;
                container.pivot = new Vector2(0.5f, 0.5f);
                container.sizeDelta = new Vector2(260f, 44f);

                var bgGo = new GameObject("Background");
                bgGo.transform.SetParent(container, false);
                var bg = bgGo.AddComponent<Image>();
                bg.sprite = GetUiSprite();
                bg.color = new Color(0f, 0f, 0f, 0.5f);
                bg.raycastTarget = false;
                var bgRt = bgGo.GetComponent<RectTransform>();
                bgRt.anchorMin = Vector2.zero;
                bgRt.anchorMax = Vector2.one;
                bgRt.offsetMin = Vector2.zero;
                bgRt.offsetMax = Vector2.zero;

                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(container, false);
                fill = fillGo.AddComponent<Image>();
                fill.sprite = GetUiSprite();
                fill.color = tint;
                fill.raycastTarget = false;
                var fillRt = fillGo.GetComponent<RectTransform>();
                fillRt.anchorMin = new Vector2(0f, 0f);
                fillRt.anchorMax = new Vector2(1f, 1f);
                fillRt.pivot = new Vector2(0f, 0.5f);
                fillRt.offsetMin = Vector2.zero;
                fillRt.offsetMax = Vector2.zero;

                // Ornate frame over the fill, so the core gauge reads as siege hardware
                // instead of a flat coloured strip. Sliced with tower end-caps as fixed
                // borders (see the sprite's spriteBorder), so widening the badge stretches
                // only the riveted span between them. Soft-fails to the bare bar if the art
                // is missing, and never intercepts clicks.
                var frameSprite = GimmickSpriteLibrary.Load(GimmickSpriteLibrary.GaugeFrame);
                if (frameSprite != null)
                {
                    var frameGo = new GameObject("Frame");
                    frameGo.transform.SetParent(container, false);
                    var frame = frameGo.AddComponent<Image>();
                    frame.sprite = frameSprite;
                    frame.type = Image.Type.Sliced;
                    frame.raycastTarget = false;
                    var frameRt = frameGo.GetComponent<RectTransform>();
                    frameRt.anchorMin = Vector2.zero;
                    frameRt.anchorMax = Vector2.one;
                    // Bleed slightly past the fill so the frame's inner lip overlaps the bar
                    // edge rather than leaving a hairline of background between them.
                    frameRt.offsetMin = new Vector2(-6f, -7f);
                    frameRt.offsetMax = new Vector2(6f, 7f);
                }

                var textGo = new GameObject("Label");
                textGo.transform.SetParent(container, false);
                label = textGo.AddComponent<TextMeshProUGUI>();
                label.fontSize = 17;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = Color.white;
                label.raycastTarget = false;
                var textRt = textGo.GetComponent<RectTransform>();
                textRt.anchorMin = Vector2.zero;
                textRt.anchorMax = Vector2.one;
                textRt.offsetMin = Vector2.zero;
                textRt.offsetMax = Vector2.zero;
                label.text = title;
            }

            public void Update(CastleCoreGimmick core, Camera cam)
            {
                if (container == null) return;
                bool alive = core != null;
                container.gameObject.SetActive(alive);
                if (!alive) return;

                if (!hasStoredBasePos)
                {
                    baseAnchoredPosition = container.anchoredPosition;
                    hasStoredBasePos = true;
                }

                if (lastHP >= 0f && core.currentHP < lastHP)
                {
                    shakeTimer = 0.4f;
                    shakeIntensity = 8f;
                }
                lastHP = core.currentHP;

                if (shakeTimer > 0f)
                {
                    shakeTimer -= Time.deltaTime;
                    var offset = new Vector2(Random.Range(-1f, 1f), Random.Range(-1f, 1f)) * shakeIntensity;
                    container.anchoredPosition = baseAnchoredPosition + offset;
                    shakeIntensity = Mathf.Max(0f, shakeIntensity - Time.deltaTime * 20f);
                }
                else
                {
                    container.anchoredPosition = baseAnchoredPosition;
                }

                float ratio = Mathf.Clamp01(core.currentHP / Mathf.Max(1f, core.maxHP));
                fill.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
                fill.color = Color.Lerp(new Color(1f, 0.22f, 0.12f, 0.9f), tint, ratio);
                label.text = $"{title}  {Mathf.CeilToInt(core.currentHP)}/{Mathf.CeilToInt(core.maxHP)}";
            }
        }
    }

    public class GameFeelRingPulse : MonoBehaviour
    {
        public float lifetime = 0.35f;
        public float finalRadius = 1.2f;
        public Color startColor = Color.white;

        private SpriteRenderer spriteRenderer;
        private float elapsed;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            transform.localScale = Vector3.zero;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = lifetime <= 0f ? 1f : Mathf.Clamp01(elapsed / lifetime);
            float eased = 1f - Mathf.Pow(1f - t, 2f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.05f, finalRadius, eased);

            if (spriteRenderer != null)
            {
                Color c = startColor;
                c.a *= 1f - t;
                spriteRenderer.color = c;
            }

            if (t >= 1f) Destroy(gameObject);
        }
    }

    /// <summary>
    /// GameFeelVfx is a static utility class, so it has no MonoBehaviour of its own to run
    /// coroutines on. This tiny persistent runner exists solely so
    /// SpawnDelayedSecondaryBurst() can WaitForSeconds() before firing the layered burst.
    /// </summary>
    public class GameFeelVfxCoroutineRunner : MonoBehaviour
    {
    }
}
