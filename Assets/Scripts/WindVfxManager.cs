using TMPro;
using UnityEngine;

namespace CastleBusters
{
    public class WindVfxManager : MonoBehaviour
    {
        public static WindVfxManager Instance { get; private set; }

        private ParticleSystem windParticleSystem;
        private TextMeshPro windDirectionLabel;
        private TextMeshProUGUI windUiText;
        private float pulseTimer;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                SetupWindParticles();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void SetupWindParticles()
        {
            SetupWindUiText();

            var go = new GameObject("WindParticleSystem");
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(0f, 8f, 0f);

            windParticleSystem = go.AddComponent<ParticleSystem>();
            windParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var mainModule = windParticleSystem.main;
            mainModule.duration = 1f;
            mainModule.loop = true;
            mainModule.startLifetime = 4f;
            mainModule.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.28f);
            mainModule.startColor = new Color(0.65f, 0.9f, 1f, 0.35f);
            mainModule.simulationSpace = ParticleSystemSimulationSpace.World;
            mainModule.playOnAwake = false;

            var shape = windParticleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(30f, 5f, 1f);
            shape.rotation = new Vector3(0f, 0f, 0f);

            var velocityModule = windParticleSystem.velocityOverLifetime;
            velocityModule.enabled = true;
            velocityModule.space = ParticleSystemSimulationSpace.World;

            var emissionModule = windParticleSystem.emission;
            emissionModule.rateOverTime = 0f;

            var psr = windParticleSystem.GetComponent<ParticleSystemRenderer>();
            psr.sortingOrder = 6;
            psr.sharedMaterial = GameFeelVfx.GetParticleMaterial();

            var labelGo = new GameObject("WorldWindDirectionLabel");
            labelGo.transform.SetParent(transform);
            labelGo.transform.position = new Vector3(0f, 7.0f, 0f);
            windDirectionLabel = labelGo.AddComponent<TextMeshPro>();
            windDirectionLabel.alignment = TextAlignmentOptions.Center;
            windDirectionLabel.fontSize = 5.5f;
            windDirectionLabel.sortingOrder = 20;
            windDirectionLabel.color = new Color(0.65f, 0.9f, 1f, 0.75f);
            windDirectionLabel.text = "WIND";

            windParticleSystem.Play();
        }

        private void SetupWindUiText()
        {
            var canvas = FindObjectOfType<Canvas>();
            if (canvas == null || windUiText != null) return;
            MobileSafeArea.ConfigureCanvas(canvas);

            var go = new GameObject("ScreenWindDirectionUI");
            go.transform.SetParent(MobileSafeArea.GetContentRoot(canvas), false);
            windUiText = go.AddComponent<TextMeshProUGUI>();
            windUiText.fontSize = 22;
            windUiText.color = new Color(0.65f, 0.9f, 1f, 0.9f);
            windUiText.alignment = TextAlignmentOptions.Center;

            var rectTransform = go.GetComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0.95f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.95f);
            rectTransform.pivot = new Vector2(0.5f, 1f);
            rectTransform.anchoredPosition = new Vector2(0f, -10f);
            rectTransform.sizeDelta = new Vector2(460f, 54f);
        }

        private void Update()
        {
            if (GameManager.Instance == null || windParticleSystem == null) return;

            float windForce = GameManager.Instance.currentWindForce;
            float absWind = Mathf.Abs(windForce);
            if (pulseTimer > 0f) pulseTimer -= Time.deltaTime;
            float pulse = pulseTimer > 0f ? 1.8f : 1f;

            var velocityModule = windParticleSystem.velocityOverLifetime;
            var emissionModule = windParticleSystem.emission;
            var mainModule = windParticleSystem.main;

            velocityModule.x = new ParticleSystem.MinMaxCurve(windForce * 1.9f);
            velocityModule.y = new ParticleSystem.MinMaxCurve(-0.35f);
            emissionModule.rateOverTime = (8f + absWind * 7f) * pulse;
            mainModule.startSpeed = new ParticleSystem.MinMaxCurve(absWind * 0.75f + 1.5f);
            mainModule.startColor = absWind >= 3.5f
                ? new Color(1f, 0.78f, 0.25f, 0.45f)
                : new Color(0.65f, 0.9f, 1f, 0.35f);

            string arrow = windForce > 0.15f ? ">>>" : windForce < -0.15f ? "<<<" : "---";
            string strength = absWind >= 3.5f ? "STRONG" : absWind >= 1.5f ? "MED" : "LIGHT";

            if (windDirectionLabel != null)
            {
                windDirectionLabel.text = $"{arrow} WIND {absWind:F1} {arrow}";
                windDirectionLabel.transform.localScale = Vector3.one * (pulseTimer > 0f ? 1.15f : 1f);
                windDirectionLabel.color = absWind >= 3.5f
                    ? new Color(1f, 0.78f, 0.25f, 0.9f)
                    : new Color(0.65f, 0.9f, 1f, 0.75f);
            }

            if (windUiText != null)
            {
                windUiText.text = $"WIND / 바람  {arrow}  {strength} {absWind:F1}";
                windUiText.color = absWind >= 3.5f
                    ? new Color(1f, 0.78f, 0.25f, 0.95f)
                    : new Color(0.65f, 0.9f, 1f, 0.9f);
            }
        }

        public void PulseWindChange(float newWindForce)
        {
            pulseTimer = 0.7f;
        }
    }
}
