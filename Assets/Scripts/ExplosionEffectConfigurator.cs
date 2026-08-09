using UnityEngine;

namespace CastleBusters
{
    public class ExplosionEffectConfigurator : MonoBehaviour
    {
        private void Awake()
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps != null)
            {
                // Load the generated explosion frames
                var sprites = Resources.LoadAll<Sprite>("GeneratedExplosionFrames");
                System.Array.Sort(sprites, (a, b) => string.Compare(a.name, b.name));

                var main = ps.main;
                main.startColor = Color.white; // Keep original sprite colors
                main.startSize = 2.5f; // Slightly larger for better visibility
                main.startSpeed = 0f; // Keep explosion centered
                main.startLifetime = 0.45f; // Snappy explosion duration

                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) }); // Spawn 1 particle that plays the animation

                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 0.1f;

                if (sprites.Length > 0)
                {
                    var tex = ps.textureSheetAnimation;
                    tex.enabled = true;
                    tex.mode = ParticleSystemAnimationMode.Sprites;
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        tex.AddSprite(sprites[i]);
                    }
                }

                var psr = GetComponent<ParticleSystemRenderer>();
                if (psr != null)
                {
                    Texture2D tex = (sprites.Length > 0 && sprites[0] != null) ? sprites[0].texture : null;
                    psr.sharedMaterial = GameFeelVfx.GetParticleMaterial(tex);
                    psr.sortingOrder = 35;
                }
            }
        }
    }
}
