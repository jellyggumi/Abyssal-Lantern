using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// AOS capture objective (docs/design/aos-overhaul.md §1). One zone sits on each core;
    /// attackers standing inside without a defender fill the gauge, a full gauge ends the
    /// match — the alternative to breaking the core outright. Pure math in CaptureRules.
    /// </summary>
    public class CaptureZoneController : MonoBehaviour
    {
        /// <summary>Side that OWNS (defends) this zone.</summary>
        public bool ownedByPlayer;
        public float progress; // 0..1, filled by the attacking side

        private LineRenderer ring;
        private TMPro.TextMeshPro label;
        private float lastFxPulse;

        public static CaptureZoneController Create(Vector3 corePosition, bool ownedByPlayer)
        {
            var go = new GameObject(ownedByPlayer ? "PlayerCaptureZone" : "EnemyCaptureZone");
            go.transform.position = corePosition;
            var zone = go.AddComponent<CaptureZoneController>();
            zone.ownedByPlayer = ownedByPlayer;
            return zone;
        }

        private void Start()
        {
            BuildRing();
        }

        private void BuildRing()
        {
            ring = gameObject.AddComponent<LineRenderer>();
            ring.loop = true;
            ring.useWorldSpace = false;
            ring.startWidth = 0.09f;
            ring.endWidth = 0.09f;
            ring.material = new Material(Shader.Find("Sprites/Default"));
            ring.sortingOrder = 6;
            const int segments = 48;
            ring.positionCount = segments;
            for (int i = 0; i < segments; i++)
            {
                float a = i / (float)segments * Mathf.PI * 2f;
                ring.SetPosition(i, new Vector3(
                    Mathf.Cos(a) * CaptureRules.CaptureRadius,
                    Mathf.Sin(a) * CaptureRules.CaptureRadius, 0f));
            }
            ApplyRingColor(0f);

            var labelGo = new GameObject("CaptureLabel");
            labelGo.transform.SetParent(transform, false);
            labelGo.transform.localPosition = new Vector3(0f, CaptureRules.CaptureRadius + 0.7f, 0f);
            label = labelGo.AddComponent<TMPro.TextMeshPro>();
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.fontSize = 2.6f;
            label.sortingOrder = 20;
            label.text = string.Empty;
        }

        private void ApplyRingColor(float t)
        {
            // Owner tint at rest, attacker tint as the gauge fills.
            Color rest = ownedByPlayer ? new Color(0.3f, 0.7f, 1f, 0.35f) : new Color(1f, 0.6f, 0.2f, 0.35f);
            Color hot = ownedByPlayer ? new Color(1f, 0.35f, 0.2f, 0.95f) : new Color(0.35f, 0.9f, 1f, 0.95f);
            var c = Color.Lerp(rest, hot, t);
            if (ring != null) { ring.startColor = c; ring.endColor = c; }
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            if (gm.currentState != GameState.PlayerTurn && gm.currentState != GameState.AITurn) return;

            CountOccupants(out int attackers, out int defenders);
            float before = progress;
            progress = CaptureRules.Tick(progress, attackers, defenders, Time.deltaTime);

            ApplyRingColor(progress);
            UpdateLabel(attackers, defenders);

            if (progress > before && Time.time - lastFxPulse > 0.6f)
            {
                lastFxPulse = Time.time;
                GameFeelVfx.SpawnShockwaveRing(transform.position,
                    ownedByPlayer ? new Color(1f, 0.4f, 0.2f, 0.5f) : new Color(0.4f, 0.9f, 1f, 0.5f),
                    CaptureRules.CaptureRadius * (0.6f + 0.5f * progress), 0.45f);
            }

            // Capture alarm: one warning as the gauge crosses the halfway mark.
            if (before < 0.5f && progress >= 0.5f)
            {
                SiegeAlarmSystem.Post(
                    ownedByPlayer ? "경고! 아군 진영 점령 50% 돌파" : "적 진영 점령 50% 돌파!",
                    ownedByPlayer ? new Color(1f, 0.4f, 0.3f, 1f) : new Color(0.5f, 0.95f, 1f, 1f));
            }

            if (CaptureRules.Captured(progress))
            {
                gm.OnZoneCaptured(ownedByPlayer);
            }
        }

        private void CountOccupants(out int attackers, out int defenders)
        {
            attackers = 0;
            defenders = 0;
            for (int i = 0; i < UnitController.Active.Count; i++)
            {
                var unit = UnitController.Active[i];
                if (unit == null || unit.CurrentState == UnitState.Dead) continue;
                // Launched projectiles overflying the zone are not occupation.
                if (unit.CurrentState == UnitState.Launched) continue;
                if (Vector2.Distance(unit.transform.position, transform.position) > CaptureRules.CaptureRadius) continue;
                if (unit.isPlayerUnit == ownedByPlayer) defenders++;
                else attackers++;
            }
        }

        // Label change-gate: rebuilding the interpolated string every frame allocated
        // garbage; only touch label.text/color when the displayed state actually changed.
        private int lastLabelPercent = int.MinValue; // -1 encodes the "empty" state
        private bool lastLabelContested;

        private void UpdateLabel(int attackers, int defenders)
        {
            if (label == null) return;
            if (progress <= 0.02f)
            {
                if (lastLabelPercent != -1 || lastLabelContested)
                {
                    lastLabelPercent = -1;
                    lastLabelContested = false;
                    label.text = string.Empty;
                }
                return;
            }
            bool contested = attackers > 0 && defenders > 0;
            // Same rounding as the previous "{progress * 100f:F0}" (half away from zero; value is non-negative).
            int percent = Mathf.FloorToInt(progress * 100f + 0.5f);
            if (percent == lastLabelPercent && contested == lastLabelContested) return;
            lastLabelPercent = percent;
            lastLabelContested = contested;
            label.text = contested ? "경합 CONTESTED" : $"점령 {percent}%";
            label.color = contested
                ? new Color(1f, 0.85f, 0.3f, 1f)
                : (ownedByPlayer ? new Color(1f, 0.45f, 0.3f, 1f) : new Color(0.45f, 0.95f, 1f, 1f));
        }
    }
}
