using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace CastleBusters
{
    public class SimpleAI : MonoBehaviour
    {
        [Header("AI Settings")]
        public Transform launchPoint;
        public GameObject[] unitPrefabs;
        public float maxLaunchVelocity = 25f;
        public float errorOffsetRange = 1.0f;

        public void TakeTurn() => StartCoroutine(PerformLaunch());

        private IEnumerator PerformLaunch()
        {
            if (unitPrefabs == null || unitPrefabs.Length == 0) { GameManager.Instance?.OnUnitLaunched(null); yield break; }

            yield return new WaitForSeconds(1.5f);
            var targetPos = FindTargetPosition() + new Vector2(Random.Range(-errorOffsetRange, errorOffsetRange), Random.Range(-errorOffsetRange, errorOffsetRange));

            var prefab = unitPrefabs[Random.Range(0, unitPrefabs.Length)];
            float mass = 1f;
            if (prefab.TryGetComponent<Rigidbody2D>(out var rb))
            {
                // Match the runtime mass reduction UnitController.Awake() applies on spawn (see
                // UnitController.RuntimeMassScale) so the AI's wind-compensation targeting uses
                // the same mass the projectile will actually fly with, same fix as
                // LaunchManager.DrawTrajectory.
                mass = Mathf.Max(UnitController.MinRuntimeMass, rb.mass * UnitController.RuntimeMassScale);
            }

            var velocity = CalculateLaunchVelocity(targetPos, mass);

            var unitGo = Instantiate(prefab, launchPoint.position, Quaternion.identity);
            // See UnitController.SnapColliderAboveGround: without this the unit spawns embedded
            // in the ground/platform at launchPoint and instantly "lands" instead of flying.
            UnitController.SnapColliderAboveGround(unitGo, launchPoint.position.y);
            var unit = unitGo.GetComponent<UnitController>();
            if (unit != null) { unit.isPlayerUnit = false; unit.Launch(velocity); GameManager.Instance?.RegisterAIUnit(unit); }
            GameManager.Instance?.OnUnitLaunched(unit);
        }



        // Reused across turns to avoid a fresh array + list per AI shot.
        private static readonly List<UnitController> candidates = new List<UnitController>();

        private Vector2 FindTargetPosition()
        {
            candidates.Clear();
            for (int i = 0; i < UnitController.Active.Count; i++)
            {
                var u = UnitController.Active[i];
                if (u.isPlayerUnit && u.CurrentState != UnitState.Dead) candidates.Add(u);
            }
            if (candidates.Count > 0) return candidates[Random.Range(0, candidates.Count)].transform.position;

            var blocks = GameManager.Instance?.playerCastle?.GetComponentsInChildren<DestructibleBlock>()
                .Where(b => !b.IsFalling).ToArray();
            if (blocks != null && blocks.Length > 0) return blocks[Random.Range(0, blocks.Length)].transform.position;

            return new Vector2(-5f, 0f);
        }

        private Vector2 CalculateLaunchVelocity(Vector2 target, float mass)
        {
            Vector2 displacement = target - (Vector2)launchPoint.position;
            float gravity = Mathf.Abs(Physics2D.gravity.y), angleRad = 45f * Mathf.Deg2Rad, absX = Mathf.Abs(displacement.x);

            // 1. Calculate initial velocity estimate (no wind)
            float denominator = 2f * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) * (absX * Mathf.Tan(angleRad) - displacement.y);
            float v2 = (gravity * absX * absX) / denominator;
            float v = Mathf.Clamp(v2 > 0 ? Mathf.Sqrt(v2) : maxLaunchVelocity * 0.7f, 5f, maxLaunchVelocity);

            // 2. Compensate for wind if GameManager exists and target is within wind effect radius
            if (GameManager.Instance != null && GameManager.Instance.currentWindForce != 0f)
            {
                var gm = GameManager.Instance;
                float distanceToWindOrigin = Vector2.Distance(target, gm.windEffectOrigin);
                if (distanceToWindOrigin <= gm.windEffectRadius)
                {
                    float wind = gm.currentWindForce;
                    float t = absX / (v * Mathf.Cos(angleRad));
                    float deltaX = 0.5f * (wind / mass) * (t * t);

                    // Adjust target position in the opposite direction of wind drift
                    target.x -= deltaX;

                    // Recalculate displacement and velocity with adjusted target
                    displacement = target - (Vector2)launchPoint.position;
                    absX = Mathf.Abs(displacement.x);
                    denominator = 2f * Mathf.Cos(angleRad) * Mathf.Cos(angleRad) * (absX * Mathf.Tan(angleRad) - displacement.y);
                    v2 = (gravity * absX * absX) / denominator;
                    v = Mathf.Clamp(v2 > 0 ? Mathf.Sqrt(v2) : maxLaunchVelocity * 0.7f, 5f, maxLaunchVelocity);
                }
            }

            return new Vector2(Mathf.Sign(displacement.x) * v * Mathf.Cos(angleRad), v * Mathf.Sin(angleRad));
        }
    }
}
