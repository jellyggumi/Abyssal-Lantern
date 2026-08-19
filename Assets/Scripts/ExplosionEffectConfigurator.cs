using UnityEngine;

namespace CastleBusters
{
    public class ExplosionEffectConfigurator : MonoBehaviour
    {
        private void Awake()
        {
            var ps = GetComponent<ParticleSystem>();
            if (ps == null) return;

            // WHY THIS FILE WAS THE WHITE EXPLOSION.
            //
            // `ExplosiveBarrel.prefab` serialises a reference to the prefab this sits on, so it runs
            // in the editor AND in a build — the wiring was never the problem. The frames it loads
            // were imported as `textureType: 0` (Default) with `spriteMode: 0`, so this
            // `LoadAll<Sprite>` returned an EMPTY array even though the art is colourful (measured
            // saturation 0.31-0.95, under 1% near-white pixels). Empty meant no texture sheet, and
            // the renderer branch below fell to `GetParticleMaterial(null)` -> a pure white radial
            // blob, with `startColor = Color.white` painted over it. Third instance of that import
            // defect here, after fx_muzzle and fx_arcane.
            //
            // Loading now goes through `ExplosionFrames` so there is ONE runtime path, name-sorted
            // (LoadAll promises no order, and a shuffled explosion plays its frames out of sequence).
            var sprites = ExplosionFrames.Load();

            var main = ps.main;
            main.startSize = 2.5f;
            main.startSpeed = 0f;      // the explosion stays where the barrel was
            main.startLifetime = 0.45f;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });  // one particle plays the sheet

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.1f;

            var psr = GetComponent<ParticleSystemRenderer>();

            if (sprites.Length > 0)
            {
                var tex = ps.textureSheetAnimation;
                tex.enabled = true;
                tex.mode = ParticleSystemAnimationMode.Sprites;
                for (int i = 0; i < sprites.Length; i++) tex.AddSprite(sprites[i]);

                // White because the art carries its own colour and a tint could only mute it.
                main.startColor = Color.white;
                if (psr != null)
                {
                    psr.sharedMaterial = GameFeelVfx.GetParticleMaterial(sprites[0].texture);
                    psr.sortingOrder = 35;
                }
                return;
            }

            // No frames. The old code left white-on-white here, which is exactly what shipped: a
            // white tint over `GetDefaultParticleTexture()`'s white blob. If the art is ever
            // unreachable again, an amber ember reads as fire instead of as a bug — the default
            // texture is a colourless radial falloff, so the tint is the only thing that can carry
            // colour, and leaving it white is what made a missing asset look like a rendering fault.
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.72f, 0.20f, 0.95f),
                new Color(1f, 0.28f, 0.05f, 0.55f));
            if (psr != null)
            {
                psr.sharedMaterial = GameFeelVfx.GetParticleMaterial();
                psr.sortingOrder = 35;
            }
        }
    }
}
