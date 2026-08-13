using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Captures what the change looks like, because "the white box is gone" and "you can see who is
    /// shooting" are claims about pixels and the assertions next door only prove the wiring.
    ///
    /// Writes into `_workspace/current/qa/evidence/` following the existing capture fixtures. Not a
    /// pass/fail gate on appearance — it fails only if a capture cannot be produced at all, since a
    /// screenshot that silently did not render is worse than none.
    /// </summary>
    public class LauncherMotionEvidenceCapture
    {
        private const string EvidenceDir = "_workspace/current/qa/evidence/launcher-motion";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"expected private field {target.GetType().Name}.{field}");
            f.SetValue(target, value);
        }

        /// <summary>
        /// Clears everything covering the board before a capture.
        ///
        /// Two attempts were needed and both failures are worth recording, because the
        /// assertion-based tests next door never saw either — they read object state, not pixels,
        /// which is the whole reason this capture exists as separate evidence.
        ///
        /// First attempt rendered the cold-open narrative art. Second still rendered the TITLE
        /// card: <c>BeginSiege()</c> does dismiss the title, but it returns early unless the state
        /// is already <c>Intro</c>, and two seconds after a scene load it may not be. So the title
        /// is dismissed directly here rather than assumed away.
        ///
        /// Every lookup is Unity-null aware: `?.` alone sees C# null and would touch a host the
        /// scene load already destroyed.
        /// </summary>
        private static void ClearColdOpen()
        {
            if (NarrativeVideoIntro.Active != null) NarrativeVideoIntro.Active.Skip();
            if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();

            foreach (var intro in Object.FindObjectsByType<IntroScreenController>(FindObjectsSortMode.None))
            {
                if (intro != null) intro.Dismiss();
            }
            foreach (var prologue in Object.FindObjectsByType<WebtoonPrologueController>(FindObjectsSortMode.None))
            {
                if (prologue != null) prologue.Dismiss();
            }

            Time.timeScale = 1f;
        }

        /// <summary>
        /// Renders the main camera to a RenderTexture and writes a PNG.
        ///
        /// Not <c>ScreenCapture.CaptureScreenshot</c>: in batch mode that produced nothing at all
        /// (the run timed out with an empty evidence folder), because there is no presented
        /// backbuffer to capture. Explicit <c>cam.Render()</c> into a RenderTexture is what the
        /// existing capture fixtures here do, and it works headless. Overlay canvases bypass camera
        /// rendering, so each is briefly switched to camera space and restored — same as
        /// HudFixEvidenceCapture.Shoot.
        /// </summary>
        private static void Shoot(string name, int w = 1365, int h = 768)
        {
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (c.isActiveAndEnabled) { cam = c; break; }
                }
            }
            Assert.IsNotNull(cam, "no active camera to capture from");

            var canvases = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var saved = new (Canvas c, RenderMode mode, Camera cam, float dist)[canvases.Length];
            for (var i = 0; i < canvases.Length; i++)
            {
                var c = canvases[i];
                saved[i] = (c, c.renderMode, c.worldCamera, c.planeDistance);
                if (c.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = cam;
                c.planeDistance = cam.nearClipPlane + 0.01f;
            }
            Canvas.ForceUpdateCanvases();

            var rt = new RenderTexture(w, h, 24);
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;

            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;

            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();

            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            foreach (var (c, mode, worldCam, dist) in saved)
            {
                if (c == null) continue;
                c.renderMode = mode;
                c.worldCamera = worldCam;
                c.planeDistance = dist;
            }

            var path = Path.Combine(EvidenceDir, name);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);

            var info = new FileInfo(path);
            Assert.Greater(info.Length, 4096, $"{name} is suspiciously small — did it render?");
            Debug.Log($"[evidence] {path} ({info.Length} bytes)");
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator Capture_BothLaunchersAcrossATurnHandoff()
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
            ClearColdOpen();
            yield return null;

            var ai = Object.FindFirstObjectByType<SimpleAI>();
            Assert.IsNotNull(ai);
            // Bring the enemy launcher up now so the capture shows both machines on the apron —
            // in play it appears on the AI's first aim.
            var enemyView = LauncherView.CreateEnemyLauncher(ai.launchPoint);
            Assert.IsNotNull(enemyView);

            // Player turn: player launcher lit, enemy dimmed.
            SetPrivate(gm, "isPlayerTurn", true);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.PlayerTurn;
            yield return null;
            yield return new WaitForSecondsRealtime(0.6f);
            Shoot("player-turn-both-launchers.png");

            // Enemy turn with the windup running: this is the frame that used to be two empty
            // muzzles for 0.9 seconds.
            SetPrivate(gm, "isPlayerTurn", false);
            gm.currentState = GameState.AITurn;
            enemyView.BeginWindup();
            yield return new WaitForSecondsRealtime(LauncherFeedback.WindupSeconds * 0.8f);
            Shoot("enemy-turn-windup.png");

            // And the fire kick.
            enemyView.NotifyFired(new Vector2(-15f, 11f));
            yield return null;
            Shoot("enemy-fire-recoil.png");
        }

        /// <summary>
        /// The arcs, side by side, with no icon at either endpoint — the visual claim of the impact
        /// lane. Identical geometry per side so the dashed/solid difference is the only variable.
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Capture_SpentArcsWithoutTheWhiteBox()
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
            ClearColdOpen();
            ShotTraceDirector.ResetForNewMatch();

            // Two arcs over the board: a player lob left-to-right, an enemy lob right-to-left.
            ShotTraceDirector.BeginShot(true, "기사", new Vector2(-14.5f, 3f));
            for (int i = 0; i <= 28; i++)
            {
                float x = -14.5f + i;
                ShotTraceDirector.Sample(new Vector2(x, 3f + 6f * Mathf.Sin(i / 28f * Mathf.PI)));
            }
            ShotTraceDirector.Seal();

            ShotTraceDirector.BeginShot(false, "화약통", new Vector2(14.5f, 3f));
            for (int i = 0; i <= 28; i++)
            {
                float x = 14.5f - i;
                ShotTraceDirector.Sample(new Vector2(x, 3f + 5f * Mathf.Sin(i / 28f * Mathf.PI)));
            }
            ShotTraceDirector.Seal();

            yield return null;
            Shoot("spent-arcs-solid-vs-dashed.png");
        }
    }
}
