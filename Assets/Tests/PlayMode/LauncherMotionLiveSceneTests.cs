using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Proves the board can say WHO is shooting, in the real scene.
    ///
    /// The complaint was that the player could not tell an attack was happening. The measured
    /// causes were all absences rather than bad values: the enemy apron carried no visual at all
    /// (<c>AILaunchPoint</c> had exactly one component, its Transform), the player's launcher was
    /// switched off for the whole enemy turn, the slingshot looped identically before and after
    /// firing, and enemy launches were silent. Four absences, and every one of them is invisible to
    /// an EditMode pin on the arithmetic — which is why these run in a live scene.
    ///
    /// The question each test asks is the one the survey proposed: does the frame before the shot
    /// differ from the frame of the shot?
    /// </summary>
    public class LauncherMotionLiveSceneTests
    {
        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        /// <summary>Re-arms the noise guard around the load: the runner re-arms LogAssert per test
        /// phase, and the Unity MCP plugin logs an authorization failure during a scene load when it
        /// cannot reach its local hub. House pattern, see HudCanvasContractTests.BootMatch.</summary>
        private static IEnumerator BootMatch()
        {
            LogAssert.ignoreFailingMessages = true;
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            LogAssert.ignoreFailingMessages = true;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"expected private field {target.GetType().Name}.{field}");
            f.SetValue(target, value);
        }

        /// <summary>
        /// The enemy side must have something on screen to attribute its shot to. This is the
        /// absence that made the other three defects unfixable: no amount of timing work encodes an
        /// actor when there is no actor drawn.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator EnemyLauncher_ExistsAndIsVisibleOnTheBoard()
        {
            yield return BootMatch();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();

            var ai = Object.FindFirstObjectByType<SimpleAI>();
            Assert.IsNotNull(ai, "the scene must provide a SimpleAI");
            Assert.IsNotNull(ai.launchPoint, "the AI must have a launch point to build a launcher on");

            // The enemy launcher is created lazily on the AI's first aim, so drive that entry point
            // rather than reaching into the private field — this is the path the game takes.
            var view = LauncherView.CreateEnemyLauncher(ai.launchPoint);
            Assert.IsNotNull(view,
                "the enemy launcher must be buildable from the existing slingshot art — the apron "
                + "had no visual at all before this, which is why the enemy's shot had no author");

            var sr = view.GetComponentInChildren<SpriteRenderer>(true);
            Assert.IsNotNull(sr, "the enemy launcher must render something");
            Assert.IsNotNull(sr.sprite, "with an actual sprite, not an empty renderer");
            Assert.IsTrue(sr.flipX,
                "mirrored: a launcher facing the wrong way teaches the player the opposite of how "
                + "that side's shots travel");

            // In frame, and on the enemy's half.
            Assert.Greater(view.transform.position.x, 0f, "the enemy launcher belongs on the enemy side");
            var cam = Camera.main;
            Assert.IsNotNull(cam);
            var vp = cam.WorldToViewportPoint(view.transform.position);
            Assert.That(vp.x, Is.InRange(0f, 1f), "the enemy launcher must be inside the frame");

            Object.Destroy(view.gameObject);
        }

        /// <summary>
        /// The player's launcher must stay on the board through the enemy turn. Hiding it was the
        /// original defect: with the enemy apron empty too, the enemy's entire 0.9s beat played
        /// against two blank muzzles.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator PlayerLauncher_StaysVisibleThroughTheEnemyTurn()
        {
            yield return BootMatch();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();
            yield return null;

            var lm = Object.FindFirstObjectByType<LaunchManager>();
            Assert.IsNotNull(lm);

            // Player turn first: bind the view and record the lit alpha.
            SetPrivate(gm, "isPlayerTurn", true);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.PlayerTurn;
            yield return null;
            yield return null;

            var playerView = Object.FindFirstObjectByType<LauncherView>();
            Assert.IsNotNull(playerView,
                "the player's launcher must carry a LauncherView once the match is running");
            var sr = playerView.GetComponentInChildren<SpriteRenderer>(true);
            Assert.IsNotNull(sr);
            float actingAlpha = sr.color.a;

            // Now the enemy turn.
            SetPrivate(gm, "isPlayerTurn", false);
            gm.currentState = GameState.AITurn;
            yield return null;
            yield return null;

            Assert.IsTrue(playerView.gameObject.activeInHierarchy,
                "the player's launcher must not switch off for the enemy turn — that is what left "
                + "the board with nothing to attribute the enemy's shot to");

            float waitingAlpha = sr.color.a;
            Assert.Less(waitingAlpha, actingAlpha,
                $"the waiting side must dim rather than vanish (acting {actingAlpha:F2} vs waiting {waitingAlpha:F2})");
            Assert.Greater(waitingAlpha, 0f, "dimmed, not invisible");
        }

        /// <summary>
        /// The fire kick has to actually move the launcher. This is the "does the launch frame
        /// differ from the frame before it" check: the slingshot previously looped at a fixed 8fps
        /// forever with no launch trigger, so the answer was no.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator FiringMovesTheLauncher_ThenItReturnsToRest()
        {
            yield return BootMatch();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();
            SetPrivate(gm, "isPlayerTurn", true);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.PlayerTurn;
            yield return null;
            yield return null;

            var view = Object.FindFirstObjectByType<LauncherView>();
            Assert.IsNotNull(view);
            var restPosition = view.transform.localPosition;

            view.NotifyFired(new Vector2(15f, 11f));
            Assert.IsTrue(view.IsRecoiling, "firing must start the kick");
            yield return null;

            float displaced = Vector3.Distance(view.transform.localPosition, restPosition);
            Assert.Greater(displaced, 0.01f,
                "the launcher must visibly move on the frame it fires — before this it looped "
                + "identically through its own shot");

            // The kick recovers on its own; a launcher stuck back would drift out of its apron.
            yield return new WaitForSecondsRealtime(LauncherFeedback.RecoilSeconds + 0.25f);
            Assert.IsFalse(view.IsRecoiling, "the kick must expire");
            Assert.Less(Vector3.Distance(view.transform.localPosition, restPosition), 0.01f,
                "and the launcher must settle back to its rest pose");
        }

        /// <summary>
        /// The windup must occupy the AI's existing pause. If it did not run, the reorder that made
        /// the aim available early would be a silent no-op — correctly computed, entirely invisible.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator EnemyWindup_LoadsTheLauncherBeforeTheShot()
        {
            yield return BootMatch();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();

            var ai = Object.FindFirstObjectByType<SimpleAI>();
            Assert.IsNotNull(ai);
            var view = LauncherView.CreateEnemyLauncher(ai.launchPoint);
            Assert.IsNotNull(view);

            Assert.AreEqual(0f, view.WindupProgress, 1e-3f, "at rest the launcher is not loaded");

            view.BeginWindup();
            yield return new WaitForSecondsRealtime(LauncherFeedback.WindupSeconds * 0.6f);
            float mid = view.WindupProgress;
            Assert.Greater(mid, 0.1f, "the windup must be progressing partway through the pause");
            Assert.Less(mid, 1f, "and not already finished");

            yield return new WaitForSecondsRealtime(LauncherFeedback.WindupSeconds);
            Assert.AreEqual(1f, view.WindupProgress, 1e-2f, "it reaches full draw and holds");

            // Firing clears the load: a launcher that stayed loaded after its shot would read as
            // permanently about to fire.
            view.NotifyFired(new Vector2(-15f, 11f));
            Assert.AreEqual(0f, view.WindupProgress, 1e-3f);

            Object.Destroy(view.gameObject);
        }

        /// <summary>
        /// The spent arcs must be separable without relying on hue. Both are on screen at once, so
        /// turn order cannot separate them the way it does in games where only one shot is ever
        /// visible — and WCAG 2.2 SC 1.4.1 forbids colour as the only distinguishing channel.
        /// </summary>
        [UnityTest]
        [Timeout(120000)]
        public IEnumerator PlayerAndEnemyArcs_DifferInShapeNotOnlyColour()
        {
            yield return BootMatch();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();

            ShotTraceDirector.ResetForNewMatch();

            // Identical geometry for both sides, so any difference in the drawn result is the
            // shape channel and nothing else.
            for (int side = 0; side < 2; side++)
            {
                bool byPlayer = side == 0;
                ShotTraceDirector.BeginShot(byPlayer, "기사", new Vector2(-14.5f, 3f));
                for (int i = 1; i <= 30; i++) ShotTraceDirector.Sample(new Vector2(-14.5f + i, 3f));
                ShotTraceDirector.Seal();
                yield return null;
            }

            var playerLine = GameObject.Find("ShotTrace_Player")?.GetComponent<LineRenderer>();
            var enemyLine = GameObject.Find("ShotTrace_Enemy")?.GetComponent<LineRenderer>();
            Assert.IsNotNull(playerLine, "the player arc must be drawn");
            Assert.IsNotNull(enemyLine, "the enemy arc must be drawn");

            // Width is the non-colour channel. The previous version of this test compared vertex
            // counts, which a dashed-geometry attempt satisfied while rendering solid — the
            // assertion passed and the pixels disagreed. Measuring the drawn width instead cannot
            // be satisfied by something invisible.
            float playerWidth = playerLine.startWidth;
            float enemyWidth = enemyLine.startWidth;
            Assert.Greater(playerWidth / enemyWidth, 1.4f,
                $"the two arcs must differ in width, not only in hue (player {playerWidth:F3} vs "
                + $"enemy {enemyWidth:F3}) — WCAG 1.4.1 forbids colour as the only distinction, and "
                + "both arcs are on screen at once so turn order cannot separate them");

            // Each arc must carry its dark casing. Without it the arcs measured 1.13:1 against the
            // sky, and no alpha fixes that: the team tints match the sky in luminance and differ
            // only in hue.
            var playerCore = playerLine.transform.Find("Core")?.GetComponent<LineRenderer>();
            var enemyCore = enemyLine.transform.Find("Core")?.GetComponent<LineRenderer>();
            Assert.IsNotNull(playerCore, "the player arc must have a bright core over its casing");
            Assert.IsNotNull(enemyCore, "the enemy arc must have a bright core over its casing");
            Assert.Greater(playerLine.startWidth, playerCore.startWidth,
                "the casing must be wider than the core, or no dark edge shows");
            Assert.Less(playerLine.startColor.grayscale, 0.2f,
                "the casing must actually be dark — that is the whole contrast mechanism");

            // And no marker: the icon form was cut, so nothing should be parented under an arc.
            Assert.IsEmpty(playerLine.GetComponentsInChildren<SpriteRenderer>(true),
                "the impact icon was removed; an arc must carry no child renderer");
        }
    }
}
