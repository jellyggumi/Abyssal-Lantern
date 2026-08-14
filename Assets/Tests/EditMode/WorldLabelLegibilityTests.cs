using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The gate for WORLD-space labels, which had none.
    ///
    /// `design/visibility-spec.md:116` promised every label clears
    /// <see cref="HudCanvas.LegibleFloorPixels"/> at the smallest supported window, and
    /// `HudCanvasContractTests.HudLabelSizes_ClearTheLegibilityFloorAtTheSmallestWindow` enforces
    /// it — for screen-space labels only. Three of its filters each exclude world labels: it
    /// enumerates `TextMeshProUGUI` (world labels are `TextMeshPro`), skips anything with a null
    /// canvas (world labels have none), and matches on the HUD canvas name. So the spec said "every
    /// label" and the enforcement covered half, which is how the smallest player-facing label in the
    /// game shipped at 19% of its own floor.
    ///
    /// Measurements and derivation: `_workspace/current/qa/unit-action-legibility-measurement.md`.
    /// </summary>
    public class WorldLabelLegibilityTests
    {
        // Camera fit inputs, from GamePresentationDirector's serialized defaults.
        private const float TargetHalfHeight = 8.4f;
        private const float DesiredWorldWidth = 45f;

        // Smallest supported window, the same one the HUD contract test uses.
        private const int MinWidth = 1024;
        private const int MinHeight = 576;

        // A soldier's drawn height, for the subordination check.
        private const float SoldierBodyHeight = 1.15f;   // Knight.prefab:137 bodyWorldHeight

        /// <summary>
        /// Pixels spanned by one em of a world-space TMP label.
        ///
        /// World `TextMeshPro` takes TMP's 0.1 scale branch — `m_isOrthographic` is assigned only
        /// inside `TextMeshProUGUI`, so an orthographic camera does not change it — which makes one
        /// em exactly <c>fontSize * 0.1</c> world units.
        /// </summary>
        private static float EmPixels(float fontSize, float zoom)
        {
            float aspect = (float)MinWidth / MinHeight;
            float fitted = Mathf.Max(TargetHalfHeight, DesiredWorldWidth / (2f * aspect));
            return (fontSize * 0.1f) * (MinHeight / (2f * fitted * zoom));
        }

        private static float WorstZoom => CameraFraming.MaxZoom * CameraFraming.AimZoomOut;

        /// <summary>
        /// The derivation is pinned before anything is asserted with it.
        ///
        /// If TMP's scale branch or the camera fit changes, this fails first and names the cause,
        /// rather than the size tests failing and sending the next reader hunting.
        /// </summary>
        [Test]
        public void TheConversionReproducesTheMeasuredValues()
        {
            // QA measured these against the shipped 1.9pt melee label before it was raised.
            Assert.AreEqual(4.32f, EmPixels(1.9f, 1f), 0.02f,
                "one em of a 1.9pt world label at 1024x576, zoom 1.0");
            Assert.AreEqual(2.29f, EmPixels(1.9f, WorstZoom), 0.02f,
                "the same label at the worst framing (max zoom x aim zoom)");
            Assert.AreEqual(1.888f, WorstZoom, 0.001f,
                "worst framing is max zoom 1.6 times aim zoom-out 1.18");
        }

        /// <summary>
        /// Every label goes through one floor, and it is enforced in the spawner rather than at the
        /// call sites — about twenty-five of which still pass smaller numbers.
        /// </summary>
        [Test]
        public void TheSpawnerFloorIsTheLargestSubordinateSize()
        {
            float floor = GameFeelVfx.MinWorldLabelFontSize;

            // Subordination: the annotation must not out-size the actor it annotates.
            float labelWorldHeight = floor * 0.1f;
            Assert.LessOrEqual(labelWorldHeight, SoldierBodyHeight * 0.5f,
                $"a {floor}pt label spans {labelWorldHeight:F2} world units against a "
                + $"{SoldierBodyHeight} unit soldier - past half the body it stops reading as an "
                + "annotation and starts competing with the unit");

            // And it must be the LARGEST such size, or we left legibility on the table for nothing.
            float nextStep = floor + 0.5f;   // 6.0pt, the next authored step
            Assert.Greater(nextStep * 0.1f, SoldierBodyHeight * 0.5f,
                $"{nextStep}pt would still be subordinate, so the floor is set lower than it needs "
                + "to be - raise MinWorldLabelFontSize");
        }

        /// <summary>
        /// The shortfall against the HUD floor is asserted, not hidden.
        ///
        /// This test passing does NOT mean world labels are legible by the HUD standard — they are
        /// not, and cannot be without growing past the soldier. It means the gap is exactly the size
        /// we decided to accept, so a future change that quietly widens it fails here. The honest
        /// answer to sustained action state is the unit's animation, not this label.
        /// </summary>
        [Test]
        public void TheShortfallAgainstTheHudFloorIsExactlyWhatWeAccepted()
        {
            float worst = EmPixels(GameFeelVfx.MinWorldLabelFontSize, WorstZoom);
            float floor = HudCanvas.LegibleFloorPixels;

            Assert.Less(worst, floor,
                "if this ever passes the floor, the compromise is gone and the comment in "
                + "MinWorldLabelFontSize is stale - delete the compromise and say so");

            float shortfall = floor / worst;
            Assert.AreEqual(1.81f, shortfall, 0.05f,
                $"accepted shortfall is 1.81x ({worst:F2}px against a {floor}px floor). A larger "
                + "number means a regression; a smaller one means someone improved this and should "
                + "update the recorded figure");
        }

        /// <summary>
        /// At the best framing a label does clear the floor, which is what makes the compromise
        /// worth making rather than a wash.
        /// </summary>
        [Test]
        public void AtTheDefaultFramingTheLabelClearsTheFloor()
        {
            float best = EmPixels(GameFeelVfx.MinWorldLabelFontSize, CameraFraming.MinZoom);
            Assert.Greater(best, HudCanvas.LegibleFloorPixels,
                $"at zoom {CameraFraming.MinZoom} a label spans {best:F2}px and must clear the "
                + $"{HudCanvas.LegibleFloorPixels}px floor - the shortfall is only supposed to "
                + "appear when the player zooms out");
        }

        /// <summary>
        /// The casing has to render at least one pixel or it is not there.
        ///
        /// The previous outline was 0.22 at 1.9pt, which is 0.65px — a sub-pixel casing, which is
        /// why the two-layer treatment that fixed the shot trace could not be applied at this size.
        /// Ordering matters and is recorded here: size first, then contrast.
        /// </summary>
        [Test]
        public void TheDarkCasingRendersAtLeastOnePixel()
        {
            const float capRatio = 59f / 86f;          // LiberationSans SDF: capLine / pointSize
            const float outlineRatio = 0.25f;          // GameFeelVfx.SpawnFeedbackLabel

            float cap = EmPixels(GameFeelVfx.MinWorldLabelFontSize, WorstZoom) * capRatio;
            float outline = cap * outlineRatio;

            Assert.GreaterOrEqual(outline, 1f,
                $"cap {cap:F2}px x outline ratio {outlineRatio} = {outline:F2}px. Below one pixel "
                + "the casing cannot supply the luminance difference, and the label core measured "
                + "1.21:1 against cloud on its own");
        }

        /// <summary>
        /// Diagnostics must not reach players.
        ///
        /// Six recovery messages were shipping on the same channel as the labels that describe the
        /// siege: a player could not tell which floating words were about their attack and which
        /// were about a physics rescue. This asserts the routing exists, by name, so re-pointing one
        /// at the player channel is a test failure rather than a silent regression.
        /// </summary>
        [Test]
        public void RecoveryDiagnosticsUseTheDeveloperChannel()
        {
            string[] diagnostics = { "STUCK FIX", "STUCK RECOVERY", "LOOP FIX", "OUT", "PATH FIX", "STUCK RESOLVED" };

            string[] sources =
            {
                "Assets/Scripts/UnitController.cs",
                "Assets/Scripts/MovingGimmick.cs",
            };

            foreach (var path in sources)
            {
                Assert.IsTrue(System.IO.File.Exists(path), $"precondition: {path} must exist");
                var text = System.IO.File.ReadAllText(path);

                foreach (var line in text.Split('\n'))
                {
                    if (!line.Contains("SpawnFeedbackLabel")) continue;
                    var hit = diagnostics.FirstOrDefault(d => line.Contains($"\"{d}\""));
                    Assert.IsNull(hit,
                        $"{path}: \"{hit}\" is a recovery diagnostic and must use "
                        + "SpawnDiagnosticLabel so it does not ship to players");
                }
            }
        }
    }
}
