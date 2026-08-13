using UnityEngine;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// Keeps runtime-generated UI inside the device safe area without changing its reference resolution.
    /// </summary>
    public sealed class MobileSafeArea : MonoBehaviour
    {
        public const string ContentRootName = "MobileSafeArea";

        private RectTransform contentRect;
        private Rect lastSafeArea;
        private Vector2Int lastScreenSize;

        public static RectTransform GetContentRoot(Canvas canvas)
        {
            if (canvas == null) return null;

            var existing = canvas.transform.Find(ContentRootName) as RectTransform;
            if (existing != null) return existing;

            var root = new GameObject(ContentRootName, typeof(RectTransform));
            root.transform.SetParent(canvas.transform, false);
            var rect = root.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            root.AddComponent<MobileSafeArea>();
            return rect;
        }

        public static void ConfigureCanvas(Canvas canvas)
        {
            if (canvas == null) return;
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            // HudScaleFloor owns the gameplay HUD's scaling algorithm. Safe-area setup may
            // add missing infrastructure, but must never replace that legibility floor.
            if (canvas.GetComponent<HudScaleFloor>() == null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }
            if (canvas.GetComponent<GraphicRaycaster>() == null) canvas.gameObject.AddComponent<GraphicRaycaster>();
            GetContentRoot(canvas);
        }

        public static Rect GetNormalizedSafeArea(Rect safeArea, int screenWidth, int screenHeight)
        {
            if (screenWidth <= 0 || screenHeight <= 0) return new Rect(0f, 0f, 1f, 1f);

            var xMin = Mathf.Clamp01(safeArea.xMin / screenWidth);
            var yMin = Mathf.Clamp01(safeArea.yMin / screenHeight);
            var xMax = Mathf.Clamp01(safeArea.xMax / screenWidth);
            var yMax = Mathf.Clamp01(safeArea.yMax / screenHeight);
            if (xMax < xMin || yMax < yMin) return new Rect(0f, 0f, 1f, 1f);
            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        private void Awake()
        {
            contentRect = transform as RectTransform;
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (contentRect == null) contentRect = transform as RectTransform;
            if (contentRect == null) return;

            var safeArea = Screen.safeArea;
            var screenSize = new Vector2Int(Screen.width, Screen.height);
            if (safeArea == lastSafeArea && screenSize == lastScreenSize) return;

            lastSafeArea = safeArea;
            lastScreenSize = screenSize;
            var normalized = GetNormalizedSafeArea(safeArea, screenSize.x, screenSize.y);
            contentRect.anchorMin = normalized.min;
            contentRect.anchorMax = normalized.max;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
        }
    }
}
