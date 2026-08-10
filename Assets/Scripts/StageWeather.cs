using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Ambient weather over the battlefield, one look per stage: rain on the plain, snow on
    /// the dunes' cold nights, drifting ash in the volcanic gorge.
    ///
    /// Presentation only. It reads the active layout for framing and never writes to
    /// simulation state, and the emitter sits behind the HUD but in front of the backdrop so
    /// it never obscures a unit silhouette or a launch ring — weather that hides the board
    /// would cost readability for atmosphere, which is the wrong trade (presentation-spec G4).
    ///
    /// Self-contained: <see cref="Ensure"/> builds the emitter on demand, so no scene object
    /// or prefab has to carry it and a missing particle sprite simply means no weather.
    /// </summary>
    public sealed class StageWeather : MonoBehaviour
    {
        private ParticleSystem system;
        private StageId builtFor;
        private bool built;

        public static StageWeather Ensure()
        {
            var existing = FindObjectOfType<StageWeather>();
            if (existing != null) return existing;

            var host = new GameObject("StageWeather");
            return host.AddComponent<StageWeather>();
        }

        /// <summary>Rebuild for a stage. Cheap to call repeatedly — it no-ops when the look
        /// already matches.</summary>
        public void Apply(StageId stage)
        {
            if (built && builtFor == stage) return;
            builtFor = stage;
            built = true;

            string key = stage == StageId.Stage2 ? EffectSpriteLibrary.ParticleSnow
                : stage == StageId.Stage3 ? EffectSpriteLibrary.ParticleAsh
                : EffectSpriteLibrary.ParticleRain;

            var sprite = EffectSpriteLibrary.LoadParticleSprite(key);
            if (sprite == null)
            {
                if (system != null) system.Stop();
                return;
            }

            var layout = GameManager.Instance != null
                ? GameManager.Instance.ActiveLayout
                : StageDefinitions.Stage1;

            float width = Mathf.Max(10f, layout.cameraDesiredWorldWidth);
            float top = layout.cameraMaxHalfHeight;

            if (system == null) system = gameObject.AddComponent<ParticleSystem>();
            system.Stop();

            var main = system.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.loop = true;
            main.maxParticles = 220;

            // Rain falls fast and straight-ish; snow and ash drift. Lifetime is derived from
            // the fall height so a flake always clears the frame instead of vanishing midair.
            bool isRain = key == EffectSpriteLibrary.ParticleRain;
            float speed = isRain ? 11f : (key == EffectSpriteLibrary.ParticleSnow ? 1.7f : 1.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.8f, speed * 1.2f);
            main.startLifetime = (top * 2.4f) / speed;
            main.startSize = new ParticleSystem.MinMaxCurve(isRain ? 0.30f : 0.22f, isRain ? 0.5f : 0.4f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.gravityModifier = isRain ? 0.15f : 0.02f;
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 1f, 1f, isRain ? 0.55f : 0.75f));

            var emission = system.emission;
            emission.rateOverTime = isRain ? 90f : 26f;

            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(width * 1.15f, 0.2f, 1f);
            shape.position = new Vector3(0f, top * 1.15f, 0f);
            shape.rotation = new Vector3(90f, 0f, 0f);

            var velocity = system.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            // Sideways drift: a slant sells wind on rain, a wander sells weightlessness on
            // snow and ash.
            velocity.x = new ParticleSystem.MinMaxCurve(isRain ? -2.2f : -0.8f, isRain ? -1.2f : 0.8f);

            var renderer = system.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            renderer.material = new Material(Shader.Find("Sprites/Default")) { mainTexture = sprite.texture };
            // Above the backdrop, below units and HUD: atmosphere must never sit on top of
            // the things the player is reading.
            renderer.sortingOrder = 1;

            system.Play();
        }
    }
}
