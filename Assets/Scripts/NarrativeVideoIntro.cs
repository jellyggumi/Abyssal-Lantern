using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace CastleBusters
{
    /// <summary>
    /// Plays the narrative reel as the prologue, and hands back to the panel prologue when
    /// it cannot.
    ///
    /// Video is the least reliable thing a WebGL build can attempt: <see cref="VideoPlayer"/>
    /// there can only stream from a URL, the browser decides whether it will decode the
    /// codec at all, and a failure surfaces asynchronously — or not at all, as a prepare that
    /// simply never completes. So this never assumes success. It reports
    /// <c>onUnavailable</c> on an error event *and* on a prepare timeout, and the caller
    /// shows the 11-page panel prologue instead. A player must never be left staring at a
    /// black screen because a codec was missing.
    ///
    /// Clicking or pressing a key skips, like the panel prologue.
    /// </summary>
    public sealed class NarrativeVideoIntro : MonoBehaviour
    {
        private const string FileName = "narrative.mp4";

        /// <summary>Generous enough for a cold CDN fetch on a slow line, short enough that a
        /// silent failure does not read as a hang.</summary>
        private const float PrepareTimeoutSeconds = 6f;

        private VideoPlayer player;
        private RenderTexture target;
        private Action onFinished;
        private Action onUnavailable;
        private float waitedSeconds;
        private bool started;
        private bool settled;

        private static NarrativeVideoIntro active;

        /// <summary>The reel currently on screen, if any. Mirrors StageInterludeController.Active
        /// so a caller that needs the screen back can dismiss whatever cold open is playing
        /// without knowing which of the two it got.
        ///
        /// Compared against Unity's null rather than returned raw: a scene load destroys the
        /// host without Settle ever running, and a stale reference survives `?.` — which checks
        /// C# null, not Unity's — so callers would reach a destroyed object.</summary>
        public static NarrativeVideoIntro Active => active != null ? active : null;

        public static NarrativeVideoIntro Play(Action onFinished, Action onUnavailable)
        {
            Active?.Skip();

            var host = new GameObject("NarrativeVideoIntro");
            var intro = host.AddComponent<NarrativeVideoIntro>();
            active = intro;
            intro.Begin(onFinished, onUnavailable);
            return intro;
        }

        /// <summary>Cut the reel short and continue as if it had finished — the same outcome as
        /// the player pressing a key, so skipping lands on the title rather than the fallback.</summary>
        public void Skip() => Settle(true);

        private void OnDestroy()
        {
            // A scene load destroys the host without Settle running; without this the
            // static would keep pointing at a destroyed object across the reload.
            if (active == this) active = null;
        }

        private void Begin(Action finished, Action unavailable)
        {
            onFinished = finished;
            onUnavailable = unavailable;

            string url = Path.Combine(Application.streamingAssetsPath, FileName);
            if (string.IsNullOrEmpty(url))
            {
                Settle(false);
                return;
            }

            var canvasGo = new GameObject("NarrativeCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            canvasGo.AddComponent<GraphicRaycaster>();

            var backdrop = new GameObject("Backdrop").AddComponent<Image>();
            backdrop.transform.SetParent(canvasGo.transform, false);
            backdrop.color = Color.black;
            Stretch(backdrop.rectTransform);

            target = new RenderTexture(960, 540, 0);
            var view = new GameObject("Frame").AddComponent<RawImage>();
            view.transform.SetParent(canvasGo.transform, false);
            view.texture = target;
            view.raycastTarget = false;
            Stretch(view.rectTransform);

            player = gameObject.AddComponent<VideoPlayer>();
            player.playOnAwake = false;
            player.source = VideoSource.Url;
            player.url = url;
            player.renderMode = VideoRenderMode.RenderTexture;
            player.targetTexture = target;
            player.isLooping = false;
            player.audioOutputMode = VideoAudioOutputMode.Direct;
            player.errorReceived += OnError;
            player.loopPointReached += OnReachedEnd;
            player.prepareCompleted += OnPrepared;
            player.Prepare();
        }

        private void Update()
        {
            if (settled) return;

            if (!started)
            {
                waitedSeconds += Time.unscaledDeltaTime;
                if (waitedSeconds >= PrepareTimeoutSeconds)
                {
                    // A prepare that never completes is the failure mode with no event, and
                    // the one that would otherwise hang the opening.
                    Settle(false);
                }
                return;
            }

            if (Input.anyKeyDown || Input.GetMouseButtonDown(0)) Settle(true);
        }

        private void OnPrepared(VideoPlayer _)
        {
            if (settled) return;
            started = true;
            player.Play();
        }

        private void OnError(VideoPlayer _, string message)
        {
            Debug.LogWarning($"[NarrativeVideoIntro] falling back to panels: {message}");
            Settle(false);
        }

        private void OnReachedEnd(VideoPlayer _) => Settle(true);

        /// <summary>Tear down once, and call exactly one of the two callbacks.</summary>
        private void Settle(bool played)
        {
            if (settled) return;
            settled = true;
            if (active == this) active = null;

            var finished = onFinished;
            var unavailable = onUnavailable;
            onFinished = null;
            onUnavailable = null;

            if (player != null)
            {
                player.errorReceived -= OnError;
                player.loopPointReached -= OnReachedEnd;
                player.prepareCompleted -= OnPrepared;
                player.Stop();
            }
            if (target != null) target.Release();
            Destroy(gameObject);

            if (played) finished?.Invoke();
            else unavailable?.Invoke();
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }
}
