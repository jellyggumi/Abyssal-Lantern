using UnityEngine;

namespace CastleBusters
{
    public class WindVfxManager : MonoBehaviour
    {
        public static WindVfxManager Instance { get; private set; }

        private ParticleSystem windParticleSystem;
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

            windParticleSystem.Play();
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

        }

        public void PulseWindChange(float newWindForce)
        {
            pulseTimer = 0.7f;
            float accentScale = Mathf.Clamp(0.38f + Mathf.Abs(newWindForce) * 0.045f, 0.38f, 0.68f);
            GameFeelVfx.SpawnHiggsfieldAccent(
                new Vector3(0f, 7.2f, 0f),
                HiggsfieldSpriteLibrary.Wind,
                new Color(0.9f, 1f, 1f, 0.82f),
                accentScale,
                0.5f,
                33);
        }
    }
}
