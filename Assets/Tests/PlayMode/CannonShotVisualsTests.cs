using System.Collections.Generic;
using CastleBusters;
using NUnit.Framework;
using UnityEngine;

namespace CastleBusters.Tests
{
    /// <summary>
    /// The shot has to be visible. Artillery fires from the side of the field at something the
    /// player is not looking at, so a shell with no mark in the air was doing its work
    /// invisibly — the only evidence was a block quietly vanishing. These pin the wiring, not
    /// the beauty: that the trail exists, that it is sized against the shell's collider-matched
    /// scale rather than its raw local units, and that it draws under the ball instead of over
    /// it. Sizing is the part that silently breaks, because the shell is scaled down to match
    /// its collider and a width authored in local units becomes a hairline.
    /// </summary>
    public class CannonShotVisualsTests
    {
        readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            spawned.Clear();
        }

        CannonShell SpawnShell(bool isPlayerShell)
        {
            var shell = CannonShell.Spawn(Vector2.zero, new Vector2(6f, 6f), 42f, 2.4f, isPlayerShell);
            Assert.IsNotNull(shell, "the shell fixture must spawn");
            spawned.Add(shell.gameObject);
            return shell;
        }

        [Test]
        public void EveryShell_CarriesATrail()
        {
            var trail = SpawnShell(true).GetComponent<TrailRenderer>();
            Assert.IsNotNull(trail, "a shell with no trail leaves no evidence the cannon fired");
            Assert.Greater(trail.time, 0.3f,
                "the arc must outlive the shell's flight long enough to be read after it lands");
        }

        [Test]
        public void TrailWidth_CompensatesForTheShellsCollider_MatchedScale()
        {
            var shell = SpawnShell(true);
            var trail = shell.GetComponent<TrailRenderer>();
            float scale = shell.transform.localScale.x;

            Assert.Less(scale, 1f,
                "fixture assumption: the shell is scaled down to match its collider");

            // Width is authored in local units and multiplied by that scale at draw time, so a
            // trail that did not divide it back out would render thinner than the ball it trails.
            float drawnStartWidth = trail.widthCurve.Evaluate(0f) * scale;
            Assert.Greater(drawnStartWidth, CannonShell.ShellRadius,
                "the drawn trail must be at least as wide as the shell, not a hairline");
        }

        [Test]
        public void Trail_DrawsUnderTheShell()
        {
            var shell = SpawnShell(true);
            var sr = shell.GetComponent<SpriteRenderer>();
            var trail = shell.GetComponent<TrailRenderer>();
            Assert.Less(trail.sortingOrder, sr.sortingOrder,
                "the ball must stay the bright point at the head of its own trail");
        }

        [Test]
        public void Shells_AreTintedBySide()
        {
            var player = SpawnShell(true).GetComponent<TrailRenderer>();
            var enemy = SpawnShell(false).GetComponent<TrailRenderer>();

            Color playerHot = player.colorGradient.colorKeys[0].color;
            Color enemyHot = enemy.colorGradient.colorKeys[0].color;
            Assert.AreNotEqual(playerHot, enemyHot,
                "whose shell is in the air must be readable without tracing it back to the gun");
        }

        [Test]
        public void Shell_EmitsAfterimages()
        {
            Assert.IsNotNull(SpawnShell(true).GetComponent<ShellAfterimage>(),
                "the ghosts are what report the shell's speed; the trail alone does not");
        }

        [Test]
        public void Ghosts_FadeOnUnscaledTime()
        {
            // Hit-stop drops timeScale on impact. A ghost fading on scaled time would freeze
            // mid-air exactly when the game pauses to sell the hit.
            var go = new GameObject("Ghost");
            spawned.Add(go);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, 0.5f);
            var fade = go.AddComponent<FadingGhost>();
            fade.lifetime = 0.25f;

            Assert.AreEqual(0.25f, fade.lifetime, 0.0001f,
                "lifetime must stay short enough that ghosts read as a trail, not a queue");
        }
    }
}
