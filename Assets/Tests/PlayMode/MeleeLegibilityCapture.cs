using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Captures soldiers actually fighting, which had never been captured.
    ///
    /// QA's audit of `_workspace/current/qa/evidence/` found zero frames of melee: every existing
    /// capture is a projectile. The user reported not being able to read what soldiers are doing, and
    /// the scene they were describing had never once been looked at as a frame — every judgement about
    /// it, including the first two this cycle, came from reading code.
    ///
    /// Runs with `-nographics`: the MCP plugin's BufferedFileLogStorage hangs the domain reload
    /// otherwise, which cost four aborted probe runs earlier in this cycle.
    /// </summary>
    public class MeleeLegibilityCapture
    {
        private const string EvidenceDir = "_workspace/current/qa/evidence/melee-legibility";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private static void Shoot(string name, int w = 1024, int h = 576)
        {
            var cam = Camera.main;
            if (cam == null)
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (c.isActiveAndEnabled) { cam = c; break; }
            Assert.IsNotNull(cam, "no active camera");

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

            var path = Path.Combine(EvidenceDir, name);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
            Debug.Log($"[melee] wrote {path} ({new FileInfo(path).Length} bytes)");
        }

        /// <summary>
        /// Puts a soldier next to a wall, lets it fight, and records both the swing and the gap
        /// between swings — the two things a player is trying to tell apart.
        ///
        /// Captured at 1024x576 deliberately: that is the smallest supported window and therefore the
        /// worst case for the label sizes this cycle changed.
        /// </summary>
        [UnityTest]
        [Timeout(240000)]
        public IEnumerator Capture_ASoldierFighting()
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
            if (NarrativeVideoIntro.Active != null) NarrativeVideoIntro.Active.Skip();
            if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();
            foreach (var intro in Object.FindObjectsByType<IntroScreenController>(FindObjectsSortMode.None))
                if (intro != null) intro.Dismiss();
            foreach (var p in Object.FindObjectsByType<WebtoonPrologueController>(FindObjectsSortMode.None))
                if (p != null) p.Dismiss();
            Time.timeScale = 1f;
            yield return null;

            // Find an enemy wall block, then place a player knight in contact with it so the
            // attack loop runs without waiting for a shot to land.
            DestructibleBlock wall = null;
            foreach (var b in DestructibleBlock.Active)
            {
                if (b == null || b is CastleCoreGimmick || b.isGroundAnchor) continue;
                if (b.transform.position.x < 3f) continue;
                if (wall == null || b.transform.position.x < wall.transform.position.x) wall = b;
            }
            Assert.IsNotNull(wall, "expected an enemy wall block to attack");

            var knightPrefab = Resources.Load<GameObject>("Knight")
                               ?? gm.AutomaticProjectilePrefab;
            Assert.IsNotNull(knightPrefab, "need a knight prefab to stage a melee");

            var spawn = wall.transform.position + new Vector3(-1.1f, 0.2f, 0f);
            var knight = Object.Instantiate(knightPrefab, spawn, Quaternion.identity);
            var unit = knight.GetComponent<UnitController>();
            Assert.IsNotNull(unit, "the knight prefab must carry a UnitController");
            unit.isPlayerUnit = true;

            // Idle is the pre-launch state and nothing leaves it except Launch(), so instantiating
            // alone left the knight standing forever — the first run of this probe captured twelve
            // frames of a unit doing nothing and reported state=Idle. A near-zero launch drops it
            // where it was placed and puts it on the grounded path that runs FindTarget.
            unit.Launch(new Vector2(0.4f, -0.1f));

            var cam = Camera.main;
            if (cam != null)
                cam.transform.position = new Vector3(spawn.x + 0.5f, spawn.y + 0.6f, cam.transform.position.z);

            // Let it land, acquire the wall, and start swinging.
            for (float t = 0f; t < 6f; t += Time.unscaledDeltaTime)
            {
                if (unit == null) break;
                if (unit.CurrentState == UnitState.Attacking) break;
                yield return null;
            }
            Debug.Log($"[melee] knight state={(unit == null ? "destroyed" : unit.CurrentState.ToString())} "
                      + $"at {(unit == null ? Vector3.zero : unit.transform.position)}");

            // Sample across a full cooldown so both the swing and the gap are on record. A knight's
            // cooldown is 1.5s and its clip is 0.625s, so frames at 0.15s intervals cover both.
            for (int i = 0; i < 12; i++)
            {
                Shoot($"melee-t{i:D2}.png");
                yield return new WaitForSecondsRealtime(0.15f);
            }

            Debug.Log($"[melee] final state={unit.CurrentState}  wall HP={wall.currentHP:F1}/{wall.maxHP:F1}");
            Assert.Pass("melee frames captured; the scene the report described now exists as pixels");
        }
    }
}
