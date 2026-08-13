using System.Collections;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Reports what each launcher actually IS at runtime — sprite name, world size, alpha — so the
    /// claim "both launchers are on the board" rests on measured values rather than on squinting at
    /// a downscaled screenshot.
    ///
    /// Written because the first capture showed ring-like shapes at both aprons, and a screenshot
    /// cannot distinguish the authored slingshot art from the procedural cyan ring fallback. Reading
    /// the renderer settles it.
    /// </summary>
    public class LauncherIdentityProbe
    {
        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"expected private field {target.GetType().Name}.{field}");
            f.SetValue(target, value);
        }

        private static string Describe(GameObject go)
        {
            if (go == null) return "(null)";
            var sb = new StringBuilder();
            sb.Append($"{go.name} active={go.activeInHierarchy} pos={go.transform.position} scale={go.transform.localScale} ");
            var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
            sb.Append($"renderers={renderers.Length} ");
            foreach (var r in renderers)
            {
                string sprite = r.sprite != null ? r.sprite.name : "(no sprite)";
                Vector2 size = r.sprite != null ? (Vector2)r.sprite.bounds.size : Vector2.zero;
                Vector2 world = new Vector2(size.x * go.transform.localScale.x, size.y * go.transform.localScale.y);
                sb.Append($"[{r.name} sprite={sprite} flipX={r.flipX} alpha={r.color.a:F2} worldSize={world.x:F2}x{world.y:F2}] ");
            }
            var anim = go.GetComponentInChildren<GimmickFrameAnimator>(true);
            sb.Append(anim != null ? $"animator=yes fps={anim.fps}" : "animator=no");
            return sb.ToString();
        }

        [UnityTest]
        [Timeout(120000)]
        public IEnumerator Probe_WhatEachLauncherActuallyIs()
        {
            LogAssert.ignoreFailingMessages = true;
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            LogAssert.ignoreFailingMessages = true;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm);
            gm.BeginSiege();
            yield return null;

            // Does the slingshot art exist at all in this build?
            var slingFrames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.SlingshotAnim);
            var gateFrames = GimmickAnimLibrary.LoadFrames(GimmickAnimLibrary.LaunchGateAnim);
            Debug.Log($"[probe] slingshot frames={(slingFrames == null ? 0 : slingFrames.Length)} "
                      + $"gate frames={(gateFrames == null ? 0 : gateFrames.Length)}");

            SetPrivate(gm, "isPlayerTurn", true);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.PlayerTurn;
            yield return null;
            yield return null;

            foreach (var view in Object.FindObjectsByType<LauncherView>(FindObjectsSortMode.None))
            {
                Debug.Log($"[probe] PLAYER-TURN side={(view.isPlayerSide ? "player" : "enemy")} {Describe(view.gameObject)}");
            }

            var ai = Object.FindFirstObjectByType<SimpleAI>();
            Assert.IsNotNull(ai);
            var enemy = LauncherView.CreateEnemyLauncher(ai.launchPoint);
            Assert.IsNotNull(enemy, "the enemy launcher must be constructible");
            yield return null;
            Debug.Log($"[probe] ENEMY-BUILT {Describe(enemy.gameObject)}");

            // Now flip to the enemy turn and re-read both alphas: this is the acting/waiting gap.
            SetPrivate(gm, "isPlayerTurn", false);
            gm.currentState = GameState.AITurn;
            yield return null;
            yield return null;

            foreach (var view in Object.FindObjectsByType<LauncherView>(FindObjectsSortMode.None))
            {
                Debug.Log($"[probe] ENEMY-TURN side={(view.isPlayerSide ? "player" : "enemy")} {Describe(view.gameObject)}");
            }

            Object.Destroy(enemy.gameObject);
        }
    }
}
