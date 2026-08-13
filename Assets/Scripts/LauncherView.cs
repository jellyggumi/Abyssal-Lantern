using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// One launcher's motion: it dims when its side is waiting, loads while its side aims, and
    /// kicks back when it fires.
    ///
    /// Before this existed the board could not say who was shooting. The player's slingshot was
    /// switched OFF for the whole enemy turn (`SetActive(isPlayerTurn && !deployArmed)`) and the
    /// enemy's launch point was a bare Transform with no visual at all — so for 0.9s every enemy
    /// turn, both muzzles showed nothing, and a projectile appeared out of empty air with zero
    /// frames of windup. The slingshot art meanwhile looped at a fixed 8fps forever, identical
    /// before and after firing, so even the player's own shot had no moment.
    ///
    /// The fix is not more screen elements; the arithmetic in <see cref="LauncherFeedback"/>
    /// explains the numbers, and the survey behind both is in
    /// `.survey/siege-impact-vfx-and-attack-motion/`. This class only applies them.
    ///
    /// Presentation only: it reads turn state and writes to its own transform and renderers,
    /// never back into the simulation (CLAUDE.md §2). It deliberately does not touch the camera —
    /// WCAG 2.2 SC 2.3.3 treats a non-essential camera move as motion that must be disableable,
    /// while colour and opacity changes are excluded from that requirement entirely.
    /// </summary>
    [DisallowMultipleComponent]
    public class LauncherView : MonoBehaviour
    {
        /// <summary>Which side this launcher belongs to. Drives the acting/waiting dim.</summary>
        public bool isPlayerSide = true;

        /// <summary>Idle breathing, preserved from the previous inline pulse so the launcher still
        /// reads as alive while waiting. Kept small: the windup has to out-read it.</summary>
        public float idlePulseAmplitude = 0.06f;
        public float idlePulseSpeed = 6f;

        private Vector3 baseScale = Vector3.one;
        private Vector3 baseLocalPosition;
        private SpriteRenderer[] renderers;

        private float windupElapsed = -1f;   // negative = not winding up
        private float recoilTimer;
        private Vector2 recoilDirection = Vector2.left;

        /// <summary>Windup progress in 0..1, or 0 when idle. Exposed for tests and diagnostics.</summary>
        public float WindupProgress => windupElapsed < 0f ? 0f : LauncherFeedback.WindupProgress(windupElapsed);

        /// <summary>True while the fire kick is still travelling.</summary>
        public bool IsRecoiling => recoilTimer > 0f;

        /// <summary>
        /// Captures the rest pose. Called from Start, and again by whoever rescales the launcher
        /// afterwards — the previous inline pulse overwrote a fitted world scale by assigning the
        /// raw pulse to localScale, which silently rendered authored art at native size.
        /// </summary>
        public void CaptureRestPose()
        {
            baseScale = transform.localScale;
            baseLocalPosition = transform.localPosition;
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        private void Start()
        {
            if (renderers == null) CaptureRestPose();
        }

        /// <summary>
        /// The side is taking aim. Runs the load pose for <see cref="LauncherFeedback.WindupSeconds"/>.
        ///
        /// For the AI this is called BEFORE its existing pre-launch wait, which is the whole point:
        /// that pause was already commented as "enough of a pause to read as the enemy taking aim"
        /// but the aim was computed after it, leaving the window empty. Nothing is added to the
        /// turn budget — the window is simply filled.
        /// </summary>
        public void BeginWindup()
        {
            windupElapsed = 0f;
        }

        /// <summary>The shot has left. Ends the load and starts the kick, directed opposite to the
        /// shot so the launcher recoils the way a real one would.</summary>
        public void NotifyFired(Vector2 shotVelocity)
        {
            windupElapsed = -1f;
            recoilTimer = LauncherFeedback.RecoilSeconds;
            recoilDirection = shotVelocity.sqrMagnitude > 0.01f
                ? -shotVelocity.normalized
                : (isPlayerSide ? Vector2.left : Vector2.right);
        }

        private void Update()
        {
            if (renderers == null) CaptureRestPose();

            if (windupElapsed >= 0f) windupElapsed += Time.deltaTime;
            if (recoilTimer > 0f) recoilTimer = Mathf.Max(0f, recoilTimer - Time.deltaTime);

            var gm = GameManager.Instance;
            // Resolution belongs to whoever fired, so the launcher that just shot stays lit while
            // its shot is still in the air. Without this the highlight would snap to the other
            // side the instant the turn flag flipped, mid-flight.
            bool acting = gm == null || gm.IsPlayerTurn == isPlayerSide;

            float pulse = 1f + Mathf.Sin(Time.time * idlePulseSpeed) * idlePulseAmplitude;
            // Multiply into the fitted base scale, never assign over it.
            float scale = pulse * LauncherFeedback.WindupScale(WindupProgress);
            transform.localScale = baseScale * scale;

            float kick = LauncherFeedback.RecoilOffset(recoilTimer);
            transform.localPosition = baseLocalPosition + (Vector3)(recoilDirection * kick);

            float alpha = LauncherFeedback.SideAlpha(acting);
            for (int i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null) continue;
                var c = r.color;
                if (!Mathf.Approximately(c.a, alpha)) r.color = new Color(c.r, c.g, c.b, alpha);
            }
        }

        /// <summary>
        /// Builds the enemy launcher, mirrored, from the same art the player's uses.
        ///
        /// The enemy apron had no visual whatsoever — <c>AILaunchPoint</c> carries a single
        /// component, its Transform. That absence is why no actor could be attributed to the
        /// enemy's shot: there was nothing on screen to attribute it to. Reusing the player's
        /// slingshot art rather than generating new art keeps this a wiring change, and the
        /// survey's finding was that the missing pieces here are wiring, not assets.
        /// </summary>
        public static LauncherView CreateEnemyLauncher(Transform launchPoint)
        {
            if (launchPoint == null) return null;

            string key = GimmickAnimLibrary.SlingshotAnim;
            var frames = GimmickAnimLibrary.LoadFrames(key);
            if (frames == null || frames.Length < 2)
            {
                key = GimmickAnimLibrary.LaunchGateAnim;
                frames = GimmickAnimLibrary.LoadFrames(key);
            }
            if (frames == null || frames.Length == 0) return null;

            var go = new GameObject("EnemyLauncherView");
            go.transform.SetParent(launchPoint, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = frames[0];
            sr.sortingOrder = 8;
            sr.color = Color.white;
            // Mirrored: the enemy machine throws leftward, and a launcher facing the wrong way
            // would teach the player the opposite of how that side's shots travel.
            sr.flipX = true;

            float native = Mathf.Max(sr.sprite.bounds.size.x, sr.sprite.bounds.size.y);
            if (native > 0.0001f)
            {
                // Same 1.6u target the player's slingshot uses, so neither side looks like the
                // bigger machine — the keeps are supposed to be the giants on this board.
                float target = key == GimmickAnimLibrary.SlingshotAnim ? 1.6f : 2.4f;
                float s = target / native;
                go.transform.localScale = new Vector3(s, s, 1f);
            }
            if (key == GimmickAnimLibrary.SlingshotAnim)
            {
                go.transform.localPosition = new Vector3(0f, 0.85f * (1.6f / 3.1f), 0f);
            }

            // TryAttach re-derives scale from frame 0 to preserve the world footprint, so the
            // rest pose must be captured after it runs.
            GimmickFrameAnimator.TryAttach(go, key, 8f);

            var view = go.AddComponent<LauncherView>();
            view.isPlayerSide = false;
            view.CaptureRestPose();
            return view;
        }
    }
}
