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
    /// files exist under these names (`design/visibility-spec-v2.md` §5-B).
    /// </summary>
    public class VisibilitySpecAssetTests
    {
        /// <summary>
        /// Placeholders the spec commits to. Real art replaces the file, never the name.
        ///
        /// This list shrank from seven to one, and the deletion is the point. The v1 draft
        /// proposed seven new UI elements; a survey of twelve comparable titles found that
        /// adding a display element per missed affordance is a documented failure path — the
        /// Into the Breach team spent eighteen months building what they call "an icon mess"
        /// that way, and their fix was deleting mechanics, not adding display. v2 keeps the one
        /// element that answers the actual gap (where did the shot land) and drops the other six
        /// as unaudited output that would otherwise ship in the build against CLAUDE.md §3.
        /// </summary>
        private static readonly string[] Placeholders =
        {
            "Gimmicks/ui_ph_impact_marker",
        };

        /// <summary>Existing art the spec reuses. Listed so a cleanup pass cannot quietly drop it.</summary>
        private static readonly string[] Existing =
        {
            "Gimmicks/ui_drag_gesture",
            "Gimmicks/ui_gauge_frame",
            "Gimmicks/ui_button_card",
            "Gimmicks/gimmick_barrel",
        };

        /// <summary>
        /// The six elements v2 cancelled must stay gone.
        ///
        /// Without this the deletion is reversible by accident: a later session reading the v1
        /// draft would regenerate them, and unused art in <c>Resources/</c> is invisible until
        /// someone audits the folder. The reasons are per-file, not blanket — see
        /// `design/visibility-spec-v2.md` §5-B.
        /// </summary>
        private static readonly string[] Cancelled =
        {
            "Gimmicks/ui_ph_enemy_telegraph",   // no pre-action telegraph: 0.9s window, zero enemy-turn inputs
            "Gimmicks/ui_ph_turn_banner",       // turn label + flow strip already say whose turn it is
            "Gimmicks/ui_ph_power_meter",       // live power/angle readout already exists during the pull
            "Gimmicks/ui_ph_angle_dial",        // the v1 draft itself called this redundant with the trajectory
            "Gimmicks/ui_ph_projectile_next",   // this round's portrait already ships (task #48)
            "Gimmicks/ui_ph_step_coach",        // controlGuideText already occupies that role and position
        };

        [Test]
        public void EveryPlaceholder_LoadsAsASprite()
        {
            var missing = Placeholders.Where(p => Resources.Load<Sprite>(p) == null).ToArray();
            Assert.IsEmpty(missing,
                "design/visibility-spec-v2.md names these placeholders; each must import as a Sprite. "
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
        /// The cancelled six must not come back.
        ///
        /// Deleting a file is not a decision anyone can see six months later; this test is where
        /// the decision lives. If a future spec revives one of these elements, delete its entry
        /// here deliberately — that edit is the record that the reasoning was revisited rather
        /// than forgotten.
        /// </summary>
        [Test]
        public void CancelledPlaceholders_StayDeleted()
        {
            var resurrected = Cancelled.Where(p => Resources.Load<Sprite>(p) != null).ToArray();
            Assert.IsEmpty(resurrected,
                "design/visibility-spec-v2.md §5-B cancelled these; each was an element the survey "
                + "argued against building, and unused art in Resources/ ships in the build without "
                + "an audit (CLAUDE.md §3). Back again: " + string.Join(", ", resurrected));
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
