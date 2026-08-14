using System.Collections;
using CastleBusters;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Self-review finding. qa/coverage-cross-matrix.md argues that defects hide in empty
    /// cells, and the forecast strip shipped with only its rules cell filled: BuildLine was
    /// asserted as a pure string and the geometry as constants, while nothing checked that the
    /// strip is ever built or ever draws. That is the same shape as the wind/score defect this
    /// cycle fixed — a label whose values are right and whose pixels are absent — so the
    /// feature added to prevent that class of bug was itself exposed to it.
    ///
    /// Ensure() returns null when it cannot find a parent, quietly. These run it for real.
    /// </summary>
    public class SiegeForecastLiveSceneTests
    {
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator ForecastStrip_ExistsAndDrawsOnceTheSiegeStarts()
        {
            SceneManager.LoadScene("SampleScene");
            yield return new WaitForSecondsRealtime(2f);

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "the scene must bring up a GameManager");
            gm.BeginSiege();
            yield return null;
            yield return null;

            var strip = Object.FindObjectOfType<SiegeForecastStrip>();
            Assert.IsNotNull(strip,
                "no forecast strip in the running scene — Ensure() returns null quietly, so a "
                + "missing parent would remove the whole widget without any test noticing");

            var label = strip.GetComponent<TMP_Text>();
            Assert.IsNotNull(label, "the strip carries no text component");

            // A Canvas ancestor is the exact thing wind and score were missing.
            Assert.IsNotNull(label.canvas,
                "the strip has no Canvas above it, so it is updated but never drawn");

            yield return new WaitForSecondsRealtime(0.5f);
            Assert.IsNotEmpty(label.text ?? string.Empty,
                "the strip is present but blank during a live turn");
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator ForecastStrip_NamesTheProjectileTheTurnActuallyLoaded()
        {
            SceneManager.LoadScene("SampleScene");
            yield return new WaitForSecondsRealtime(2f);

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();
            yield return null;
            yield return new WaitForSecondsRealtime(0.5f);

            var strip = Object.FindObjectOfType<SiegeForecastStrip>();
            Assert.IsNotNull(strip, "no forecast strip in the running scene");
            var label = strip.GetComponent<TMP_Text>();

            // The rules test proves BuildLine agrees with the cycle. This proves the strip is
            // being fed the live turn rather than a stale or default one — a widget that renders
            // turn 0 forever would pass every pure-string assertion.
            string expected = OneShotSiegeRules.DisplayName(
                OneShotSiegeRules.ProjectileForTurn(gm.TurnCount));

            StringAssert.Contains(expected, label.text ?? string.Empty,
                $"turn {gm.TurnCount} loaded {expected} but the strip does not say so");
        }
    }
}
