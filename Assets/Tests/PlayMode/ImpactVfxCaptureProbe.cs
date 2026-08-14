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

            // Strike it. This runs the whole feedback stack: damage number, impact burst, fx_spark
            // tinted white, debris, and a shockwave ring past the damage threshold.
            target.TakeDamage(target.maxHP * 0.5f, true);

            // The burst lasts 0.35s. Capture inside it, twice, so a one-frame flash cannot hide.
            yield return null;
            Shoot("impact-frame-1.png");

            // Name every renderer standing at the impact, on the SAME frame that was captured.
            // Pixel measurement says something pale-neutral is there; guessing which script owns it
            // was wrong once already, so this asks the scene instead of the source.
            DumpRenderersNear(target.transform.position, 1.5f);

            yield return new WaitForSecondsRealtime(0.08f);
            Shoot("impact-frame-2.png");
            yield return new WaitForSecondsRealtime(0.20f);
            Shoot("impact-frame-3.png");

            // A/B, on the deterministic path. Strike an identical block, disable the white-tinted
            // flash renderers the moment they appear, and capture the same frame. If the pale-neutral
            // cluster goes with them the cause is named; if it stays, the candidate list is wrong
            // again and the next probe looks elsewhere. Removing face_s0 from the particles left the
            // cluster at 7 -> 8 pixels, so this is the one lever still untested.
            DestructibleBlock second = null;
            foreach (var b in DestructibleBlock.Active)
            {
                if (b == null || b is CastleCoreGimmick || b == target) continue;
                var p = b.transform.position;
                if (p.x < 3f) continue;
                if (Mathf.Abs(p.y - target.transform.position.y) > 0.6f) continue;
                second = b; break;
            }

            if (second == null)
            {
                Debug.Log("[vfx] no second block at the same height - A/B skipped, not silently passed");
            }
            else
            {
                var cam2 = Camera.main;
                if (cam2 != null)
                    cam2.transform.position = new Vector3(second.transform.position.x,
                                                          second.transform.position.y + 1f,
                                                          cam2.transform.position.z);
                yield return null;
                Shoot("ab-before.png");
                Debug.Log($"[vfx] A/B second block {second.name} at {second.transform.position}");

                second.TakeDamage(second.maxHP * 0.5f, true);
                yield return null;
                int killed = SuppressFlashRenderers();
                Debug.Log($"[vfx] A/B disabled {killed} flash renderer(s)");
                Shoot("ab-noflash.png");
                DumpRenderersNear(second.transform.position, 1.5f);
            }

            Assert.Pass("captures written; measure the pixels");
        }

        /// <summary>
        /// Lists what is actually drawing near a world position: sprite renderers with their sprite
        /// and tint, particle systems with their material and texture, and text. The tint matters as
        /// much as the sprite, because the art is authored greyscale and the code multiplies colour
        /// into it — white art times a white tint is a pale square regardless of which sprite it is.
        /// </summary>
        private static void DumpRenderersNear(Vector3 centre, float radius)
        {
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr == null || !sr.enabled || sr.sprite == null) continue;
                if (Vector3.Distance(sr.transform.position, centre) > radius) continue;
                var c = sr.color;
                Debug.Log($"[at-impact] SpriteRenderer '{sr.gameObject.name}' sprite={sr.sprite.name} " +
                          $"tint=({c.r:F2},{c.g:F2},{c.b:F2},a{c.a:F2}) order={sr.sortingOrder} " +
                          $"scale={sr.transform.lossyScale.x:F2} d={Vector3.Distance(sr.transform.position, centre):F2}");
            }

            foreach (var ps in Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None))
            {
                if (ps == null) continue;
                if (Vector3.Distance(ps.transform.position, centre) > radius) continue;
                var r = ps.GetComponent<ParticleSystemRenderer>();
                var tex = r != null && r.sharedMaterial != null ? r.sharedMaterial.mainTexture : null;
                var sc = ps.main.startColor.color;
                Debug.Log($"[at-impact] ParticleSystem '{ps.gameObject.name}' alive={ps.particleCount} " +
                          $"tex={(tex != null ? tex.name : "NULL")} startColor=({sc.r:F2},{sc.g:F2},{sc.b:F2},a{sc.a:F2}) " +
                          $"size={ps.main.startSize.constantMax:F2} order={(r != null ? r.sortingOrder : 0)}");
            }

            foreach (var t in Object.FindObjectsByType<TMPro.TextMeshPro>(FindObjectsSortMode.None))
            {
                if (t == null) continue;
                if (Vector3.Distance(t.transform.position, centre) > radius) continue;
                Debug.Log($"[at-impact] TextMeshPro '{t.gameObject.name}' text='{t.text}' " +
                          $"colour=({t.color.r:F2},{t.color.g:F2},{t.color.b:F2}) size={t.fontSize:F1}");
            }
        }

        /// <summary>
        /// Disables any white-tinted flash renderer that appears, without touching production code.
        ///
        /// The A/B needs one lever, and adding a test-only switch to <c>FrameAnimEffect</c> would put
        /// test scaffolding in the shipped class. <c>DumpRenderersNear</c> already proved these
        /// objects are findable by name, so the probe reaches into the scene instead.
        /// </summary>
        private static int SuppressFlashRenderers()
        {
            int n = 0;
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (sr == null || !sr.enabled) continue;
                var name = sr.gameObject.name;
                if (name.StartsWith("Fx_", System.StringComparison.Ordinal)
                    || name.Contains("HiggsfieldImpactAccent"))
                {
                    sr.enabled = false;
                    n++;
                }
            }
            return n;
        }

        /// <summary>
        /// Captures a REAL collision, not a synthetic <c>TakeDamage</c> call.
        ///
        /// The first probe struck a block by calling TakeDamage, which never runs
        /// <c>OnCollisionEnter2D</c>. The report says "on collision", so presentation that only the
        /// collision path spawns was invisible to that measurement.
        ///
        /// Two passes: the shot as it ships, then the same shot with the white-tinted flash
        /// renderers disabled. The pale-neutral pixels survived removing face_s0 from the particle
        /// systems, so the remaining candidate gets isolated rather than argued about.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Capture_ARealProjectileCollision()
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

            var lm = Object.FindFirstObjectByType<LaunchManager>();
            Assert.IsNotNull(lm, "need the launch manager to fire a real shot");

            for (int pass = 0; pass < 2; pass++)
            {
                bool suppress = pass == 1;
                string tag = suppress ? "noflash" : "live";

                // 45 degrees at the tuned cap - the aiming study's reachable window.
                float rad = 45f * Mathf.Deg2Rad;
                float speed = lm.maxLaunchVelocity;
                lm.SimulateLaunch(new Vector2(speed * Mathf.Cos(rad), speed * Mathf.Sin(rad)));

                UnitController shot = null;
                for (float t = 0f; t < 2f && shot == null; t += Time.unscaledDeltaTime)
                {
                    for (int i = 0; i < UnitController.Active.Count; i++)
                    {
                        var u = UnitController.Active[i];
                        if (u != null && u.isPlayerUnit && u.CurrentState == UnitState.Launched)
                        {
                            shot = u; break;
                        }
                    }
                    yield return null;
                }
                Debug.Log($"[collide] {tag}: projectile found={shot != null}");

                // Follow the shot so the collision is framed wherever it lands.
                var cam = Camera.main;
                bool captured = false;
                for (float t = 0f; t < 8f && !captured; t += Time.unscaledDeltaTime)
                {
                    if (shot != null && cam != null)
                        cam.transform.position = new Vector3(shot.transform.position.x,
                                                             shot.transform.position.y,
                                                             cam.transform.position.z);

                    if (suppress) SuppressFlashRenderers();

                    bool landed = shot == null || shot.CurrentState != UnitState.Launched;
                    if (landed)
                    {
                        var where = shot != null ? shot.transform.position : new Vector3(6f, 3f, 0f);
                        if (cam != null)
                            cam.transform.position = new Vector3(where.x, where.y, cam.transform.position.z);
                        if (suppress) Debug.Log($"[collide] noflash: disabled {SuppressFlashRenderers()} renderer(s) at the hit");
                        Shoot($"collide-{tag}-hit0.png");
                        yield return null;
                        if (suppress) SuppressFlashRenderers();
                        Shoot($"collide-{tag}-hit1.png");
                        DumpRenderersNear(where, 1.5f);
                        Debug.Log($"[collide] {tag}: impact at {where}");
                        captured = true;
                    }
                    yield return null;
                }

                Debug.Log($"[collide] {tag}: captured={captured}");

                yield return new WaitForSecondsRealtime(1.2f);
                if (pass == 0) { gm.BeginSiege(); yield return null; }
            }

            Assert.Pass("real-collision captures written; compare live vs noflash");
        }
    }
}
