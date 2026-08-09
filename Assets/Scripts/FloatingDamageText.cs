using TMPro;
using UnityEngine;

namespace CastleBusters
{
    public class FloatingDamageText : MonoBehaviour
    {
        public float lifetime = 0.75f;
        public float riseDistance = 1f;
        // Set by GameFeelVfx.SpawnDamageNumber for magnitude-tiered hits: adds an extra
        // opening scale punch so a big number visibly "lands harder" than a small one.
        public bool critPunch = false;


        private TextMeshPro text;
        private Vector3 startPosition;
        private float elapsed;
        private float horizontalDrift;

        private void Awake()
        {
            text = GetComponent<TextMeshPro>();
            if (text != null)
            {
                text.outlineWidth = 0.22f;
                text.outlineColor = Color.black;
            }
            startPosition = transform.position;
            horizontalDrift = Random.Range(-0.45f, 0.45f); // Cycle 11: Random horizontal drift
        }
        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = lifetime <= 0f ? 1f : Mathf.Clamp01(elapsed / lifetime);

            // Cycle 11: Apply horizontal drift and parabolic rise
            Vector3 currentPos = Vector3.Lerp(startPosition, startPosition + Vector3.up * riseDistance, 1f - Mathf.Pow(1f - t, 2f));
            currentPos.x += horizontalDrift * t;
            transform.position = currentPos;

            if (text != null)
            {
                Color c = text.color;
                c.a = 1f - t;
                text.color = c;
                // Cycle 11: Scale bounce animation
                float scaleBounce = Mathf.Lerp(0.75f, 1.35f, Mathf.Sin(t * Mathf.PI * 1.5f));
                if (critPunch)
                {
                    // Extra opening punch in the first ~0.15s of life only, so it reads as a
                    // sharp "landed hard" hit without dragging out the rest of the float/fade.
                    float punchT = Mathf.Clamp01(t / 0.2f);
                    float punch = 1f + (1f - punchT) * 0.55f;
                    scaleBounce *= punch;
                }
                transform.localScale = Vector3.one * scaleBounce;
            }


            if (t >= 1f) Destroy(gameObject);
        }
    }
}
