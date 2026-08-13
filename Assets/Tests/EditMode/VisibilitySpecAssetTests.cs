using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Pins that every image the visibility spec names actually loads as a Sprite.
    ///
    /// This exact failure has happened here: task #17 copied PNGs without their .meta, Unity
    /// imported them as textureType Default, and <c>Resources.LoadAll&lt;Sprite&gt;</c> returned
    /// an empty array — the slingshot silently fell back to a procedural ring and nobody noticed
    /// until the art was inspected. A missing sprite does not throw; it draws nothing.
    ///
    /// Named after the spec rather than the feature, because the spec is what promises these
    /// files exist under these names (`design/visibility-spec.md` §1).
    /// </summary>
    public class VisibilitySpecAssetTests
    {
        /// <summary>Placeholders the spec commits to. Real art replaces the file, never the name.</summary>
        private static readonly string[] Placeholders =
        {
            "Gimmicks/ui_ph_turn_banner",
            "Gimmicks/ui_ph_enemy_telegraph",
            "Gimmicks/ui_ph_power_meter",
            "Gimmicks/ui_ph_angle_dial",
            "Gimmicks/ui_ph_projectile_next",
            "Gimmicks/ui_ph_impact_marker",
            "Gimmicks/ui_ph_step_coach",
        };

        /// <summary>Existing art the spec reuses. Listed so a cleanup pass cannot quietly drop it.</summary>
        private static readonly string[] Existing =
        {
            "Gimmicks/ui_drag_gesture",
            "Gimmicks/ui_gauge_frame",
            "Gimmicks/ui_button_card",
            "Gimmicks/gimmick_barrel",
        };

        [Test]
        public void EveryPlaceholder_LoadsAsASprite()
        {
            var missing = Placeholders.Where(p => Resources.Load<Sprite>(p) == null).ToArray();
            Assert.IsEmpty(missing,
                "design/visibility-spec.md names these placeholders; each must import as a Sprite. "
                + "A PNG without its .meta imports as textureType Default and loads as null, which "
                + "renders as nothing rather than as an error. Missing: " + string.Join(", ", missing));
        }

        [Test]
        public void ReusedArt_StillLoadsAsASprite()
        {
            var missing = Existing.Where(p => Resources.Load<Sprite>(p) == null).ToArray();
            Assert.IsEmpty(missing,
                "The spec reuses this art; dropping it silently breaks the layout it was chosen for. "
                + "Missing: " + string.Join(", ", missing));
        }

        /// <summary>
        /// A placeholder must stay obviously provisional. White-box art that drifts into looking
        /// finished is how a stand-in ships: nobody replaces what nobody notices.
        /// </summary>
        [Test]
        public void Placeholders_AreStillWhiteBoxes()
        {
            foreach (var path in Placeholders)
            {
                var sprite = Resources.Load<Sprite>(path);
                Assert.IsNotNull(sprite, $"{path} must load before its look can be checked");

                var tex = sprite.texture;
                Assert.IsTrue(tex.isReadable,
                    $"{path} must stay readable so this check can run; set Read/Write in the importer");

                // Sample the middle. A white box is white there; real art almost never is.
                var c = tex.GetPixel(tex.width / 2, tex.height / 2);
                Assert.Greater(c.r, 0.9f, $"{path} centre should still be white — is this real art now?");
                Assert.Greater(c.g, 0.9f, $"{path} centre should still be white");
                Assert.Greater(c.b, 0.9f, $"{path} centre should still be white");
            }
        }
    }
}
