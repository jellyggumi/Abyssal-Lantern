using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Captures the frame a block is struck on, so "white on impact" can be measured instead of
    /// guessed at.
    ///
    /// Four candidates were ranked from code alone and none could be confirmed that way
    /// (`design/impact-vfx-and-projectile-art-request.md` §2): the white radial fallback texture,
    /// fx_spark tinted white, the block's own sprite handed to the particle system as a material
    /// texture, and the shockwave ring. Static reading cannot separate them because they all live
    /// in the same 0.35s. A render of the actual frame can.
    ///
    /// Diagnostic, not a gate. It fails only if a capture cannot be produced — a screenshot that
    /// silently did not render would be worse than none.
    /// </summary>
    public class ImpactVfxCaptureProbe
    {
        private const string EvidenceDir = "_workspace/current/qa/evidence/impact-vfx";

        [SetUp]
        public void SetUp()
        {
            Directory.CreateDirectory(EvidenceDir);
            LogAssert.ignoreFailingMessages = true;
        }

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private static void ClearColdOpen()
        {
            if (NarrativeVideoIntro.Active != null) NarrativeVideoIntro.Active.Skip();
            if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();
            foreach (var intro in Object.FindObjectsByType<IntroScreenController>(FindObjectsSortMode.None))
                if (intro != null) intro.Dismiss();
            foreach (var p in Object.FindObjectsByType<WebtoonPrologueController>(FindObjectsSortMode.None))
                if (p != null) p.Dismiss();
            Time.timeScale = 1f;
        }

        /// <summary>Renders the main camera to a RenderTexture. ScreenCapture produces nothing in
        /// batch mode; this is the house pattern (HudFixEvidenceCapture.Shoot).</summary>
        private static void Shoot(string name, int w = 1365, int h = 768)
        {
            var cam = Camera.main;
            if (cam == null)
                foreach (var c in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                    if (c.isActiveAndEnabled) { cam = c; break; }
            Assert.IsNotNull(cam, "no active camera to capture from");

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

            var info = new FileInfo(path);
            Assert.Greater(info.Length, 4096, $"{name} is suspiciously small — did it render?");
            Debug.Log($"[vfx] wrote {path} ({info.Length} bytes)");
        }

        [UnityTest]
        [Timeout(240000)]
        public IEnumerator Capture_TheFrameABlockIsStruckOn()
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

            // Report what the shaders actually resolve to. Shader.Find only sees what is in the
            // build, and nothing in this project references either shader from a material asset —
            // the only two .mat files are TextMeshPro fonts — so stripping is a live risk.
            var urp = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            var sprites = Shader.Find("Sprites/Default");
            Debug.Log($"[vfx] URP Particles/Unlit resolved: {urp != null} | Sprites/Default resolved: {sprites != null}");

            var mat = GameFeelVfx.GetParticleMaterial();
            Debug.Log($"[vfx] default particle material: {(mat == null ? "NULL — this renders as untextured white/magenta" : mat.shader.name)}");
            var emberMat = GameFeelVfx.GetParticleMaterial(
                EffectSpriteLibrary.LoadParticleSprite(EffectSpriteLibrary.ParticleEmber)?.texture);
            Debug.Log($"[vfx] ember particle material: {(emberMat == null ? "NULL" : emberMat.shader.name)}");

            // Effect frame inventory, since fx_frost is declared with zero files on disk.
            foreach (var key in new[] { EffectSpriteLibrary.Spark, EffectSpriteLibrary.Dust,
                                        EffectSpriteLibrary.Frost, "fx_shatter" })
            {
                var frames = EffectSpriteLibrary.LoadFrames(key);
                int n = frames == null ? 0 : frames.Length;
                string sizes = "";
                if (frames != null)
                    foreach (var f in frames) sizes += $"{f.rect.width:F0}x{f.rect.height:F0} ";
                Debug.Log($"[vfx] {key}: {n} frame(s)  {sizes}");
            }

            // Pick an enemy wall block near the camera centre so the capture frames it.
            DestructibleBlock target = null;
            float bestScore = float.MaxValue;
            foreach (var b in DestructibleBlock.Active)
            {
                if (b == null || b is CastleCoreGimmick) continue;
                var p = b.transform.position;
                if (p.x < 3f) continue;                       // enemy side
                float score = Mathf.Abs(p.y - 3f) + Mathf.Abs(p.x - 6f);
                if (score < bestScore) { bestScore = score; target = b; }
            }
            Assert.IsNotNull(target, "expected an enemy wall block to strike");
            Debug.Log($"[vfx] striking {target.name} at {target.transform.position} " +
                      $"(sprite={(target.GetComponent<SpriteRenderer>()?.sprite?.name ?? "none")})");

            // Frame the block, then capture a clean 'before'.
            var cam = Camera.main;
            if (cam != null) cam.transform.position = new Vector3(target.transform.position.x,
                                                                 target.transform.position.y + 1f,
                                                                 cam.transform.position.z);
            yield return null;
            Shoot("before-impact.png");

            // Strike it. This runs the whole feedback stack: damage number, impact burst with the
            // BLOCK'S OWN sprite as the particle texture, fx_spark tinted white, debris, and a
            // shockwave ring past the damage threshold.
            target.TakeDamage(target.maxHP * 0.5f, true);

            // The burst lasts 0.35s. Capture inside it, twice, so a one-frame flash cannot hide.
            yield return null;
            Shoot("impact-frame-1.png");
            yield return new WaitForSecondsRealtime(0.08f);
            Shoot("impact-frame-2.png");
            yield return new WaitForSecondsRealtime(0.20f);
            Shoot("impact-frame-3.png");

            Assert.Pass("captures written; measure the pixels");
        }
    }
}
