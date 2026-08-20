using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CastleBusters
{
    /// <summary>
    /// Adds pixel-perfect hover and click animations to UI buttons.
    /// </summary>
    public class UIButtonAnimator : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        private Vector3 originalScale;
        private Button button;
        private bool isHovered = false;

        private void Awake()
        {
            originalScale = transform.localScale;
            button = GetComponent<Button>();
        }

        private void OnDisable()
        {
            transform.localScale = originalScale;
            isHovered = false;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            transform.localScale = originalScale;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && !button.interactable) return;
            // Snappy squash animation for pixel-art feel
            transform.localScale = originalScale * 0.9f;
            // Press confirmation: the 0.9x squash is easy to miss under a finger on mobile,
            // and gameplay is loud while every menu was dead silent.
            GameFeelVfx.PlayUiClickSfx();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (isHovered)
            {
                transform.localScale = originalScale * 1.1f;
            }
            else
            {
                transform.localScale = originalScale;
            }
        }

        private void Update()
        {
            if (isHovered && transform.localScale != originalScale * 0.9f)
            {
                // Snappy pulse animation for pixel-art feel
                float pulse = 1.05f + Mathf.Sin(Time.unscaledTime * 12f) * 0.05f;
                transform.localScale = originalScale * pulse;
            }
        }
    }
}
