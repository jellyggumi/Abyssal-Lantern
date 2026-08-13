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
        public float maxLaunchVelocity = 25.2f;
        public float errorOffsetRange = 1.0f;

        public void TakeTurn() => StartCoroutine(PerformLaunch());

        // The enemy apron had no visual at all — AILaunchPoint carries one component, its
        // Transform. That absence, not the aim timing, is why the enemy's shot had no author:
        // there was nothing on screen to attribute it to.
        private LauncherView launcherView;

        private LauncherView ResolveLauncherView()
        {
            if (launcherView != null) return launcherView;
            launcherView = LauncherView.CreateEnemyLauncher(launchPoint);
            return launcherView;
        }

        private Vector2 GetLaunchPosition()
        {
            Vector2 anchor = launchPoint != null ? (Vector2)launchPoint.position : (Vector2)transform.position;
            return anchor + Vector2.up * UnitController.DefaultLaunchSpawnHeight;
        }

        private IEnumerator PerformLaunch()
        {
            if (unitPrefabs == null || unitPrefabs.Length == 0) { GameManager.Instance?.OnUnitLaunched(null); yield break; }

            // Aim FIRST, then wait. The order used to be reversed, and the comment on the wait
            // already described what it was supposed to be — "enough of a pause to read as the
            // enemy taking aim" — while the aim was computed after it. So the window existed and
            // was empty; the intent was right and the sequence was wrong. Nothing is added to the
            // 0.9s beat here, which matters because the games that spend more AI time are the
            // games whose players cut it back (GameSpot faulted Worms Armageddon for exactly
            // that), and this project already reclaimed 2.1s of dead air and reinvested it in
            // more turns. `.survey/siege-impact-vfx-and-attack-motion/synthesis.md` §2
            var targetPos = FindTargetPosition() + new Vector2(Random.Range(-errorOffsetRange, errorOffsetRange), Random.Range(-errorOffsetRange, errorOffsetRange));

            // Now the pause has something to show: the machine loads while it aims.
            ResolveLauncherView()?.BeginWindup();
            yield return new WaitForSeconds(0.5f);

            var gameManager = GameManager.Instance;

            var automaticPrefab = gameManager != null && gameManager.EnforcesOneShotTurns
                ? gameManager.AutomaticProjectilePrefab
                : null;
            var prefab = automaticPrefab != null
                ? automaticPrefab
                : unitPrefabs[Random.Range(0, unitPrefabs.Length)];
            if (prefab == null) { gameManager?.OnUnitLaunched(null); yield break; }
            float mass = UnitController.MinRuntimeMass;
            float linearDrag = 0f;
            float hardCeilingY = UnitController.DefaultHardCeilingY;
            if (prefab.TryGetComponent<Rigidbody2D>(out var rb))
            {
                // Match the mass reduction UnitController.Awake() applies on spawn.
                mass = Mathf.Max(UnitController.MinRuntimeMass, rb.mass * UnitController.RuntimeMassScale);
                linearDrag = Mathf.Max(0f, rb.linearDamping);
            }
            if (prefab.TryGetComponent<UnitController>(out var prefabUnit))
            {
                hardCeilingY = prefabUnit.hardCeilingY;
            }

            if (gameManager != null)
            {
                // Wind is spatial. The runtime body and this prediction must start from the
                // AI muzzle, never from the previous player shot.
                gameManager.windEffectOrigin = GetLaunchPosition();
            }

            var desiredFinalVelocity = CalculateLaunchVelocity(targetPos, mass, linearDrag, hardCeilingY);
            var velocity = gameManager != null
                ? gameManager.PrepareLastStandLaunchVelocity(false, desiredFinalVelocity)
                : desiredFinalVelocity;

            if (gameManager != null && !gameManager.TryCommitTurnShot()) yield break;

            var unitGo = Instantiate(prefab, GetLaunchPosition(), Quaternion.identity);
            // See UnitController.SnapColliderAboveGround: without this the unit spawns embedded
            // in the ground/platform at launchPoint and instantly "lands" instead of flying.
            UnitController.SnapColliderAboveGround(unitGo, launchPoint != null ? launchPoint.position.y : transform.position.y);
            var unit = unitGo.GetComponent<UnitController>();
            if (unit != null) { unit.isPlayerUnit = false; unit.Launch(velocity); GameManager.Instance?.RegisterAIUnit(unit); }

            // The machine kicks, and the shot is finally audible. NotifyLaunch used to be called
            // from the player's LaunchManager only, so every enemy volley was silent — and an
            // auditory signal reaches central processing in 8-10ms against 20-40ms for a visual
            // one, which makes this the cheapest attribution channel on the board.
            launcherView?.NotifyFired(velocity);
            float powerPercent = velocity.magnitude / Mathf.Max(0.01f, maxLaunchVelocity) * 100f;
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            if (angle < 0f) angle += 360f;
            GameplayUxDirector.NotifyEnemyLaunch(
                unit != null ? DeploymentRules.DisplayName(unit.unitType) : "부대", powerPercent, angle);

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

        private Vector2 CalculateLaunchVelocity(
            Vector2 target,
            float mass,
            float linearDrag,
            float hardCeilingY)
        {
            Vector2 start = GetLaunchPosition();
            Vector2 displacement = target - start;
            float maxSpeed = Mathf.Max(5f, maxLaunchVelocity);
            float absX = Mathf.Abs(displacement.x);

            if (displacement.sqrMagnitude < 0.0001f) return Vector2.up * 5f;
            if (absX < 0.05f)
            {
                Vector2 direct = displacement.normalized * maxSpeed;
                return IsFinite(direct) ? direct : Vector2.up * 5f;
            }

            float gravityMagnitude = Mathf.Abs(Physics2D.gravity.y);
            const float baseAngleDegrees = 45f;
            float baseAngleRadians = baseAngleDegrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(baseAngleRadians);
            float denominator =
                2f * cos * cos * (absX * Mathf.Tan(baseAngleRadians) - displacement.y);
            float speedSquared = denominator > 0.0001f
                ? gravityMagnitude * absX * absX / denominator
                : maxSpeed * maxSpeed * 0.49f;
            float bestSpeed = Mathf.Clamp(
                speedSquared > 0f && !float.IsNaN(speedSquared) && !float.IsInfinity(speedSquared)
                    ? Mathf.Sqrt(speedSquared)
                    : maxSpeed * 0.7f,
                5f,
                maxSpeed);
            float bestAngle = baseAngleDegrees;

            var gameManager = GameManager.Instance;
            float windForce = gameManager != null ? gameManager.currentWindForce : 0f;
            Vector2 windOrigin = gameManager != null ? gameManager.windEffectOrigin : start;
            float windRadius = gameManager != null ? gameManager.windEffectRadius : 0f;
            float fixedStep = Mathf.Max(0.001f, Time.fixedDeltaTime);
            float bestError = SimulateClosestDistanceSquared(
                start, target, bestSpeed, bestAngle, mass, linearDrag, hardCeilingY,
                windForce, windOrigin, windRadius, Physics2D.gravity, fixedStep);

            // Bounded deterministic shooting solve: five 5x5 refinements, each evaluated
            // with the same fixed-step order as Rigidbody2D and the trajectory preview.
            float speedStep = Mathf.Max(1f, (maxSpeed - 5f) * 0.25f);
            float angleStep = 12f;
            for (int pass = 0; pass < 5; pass++)
            {
                float centerSpeed = bestSpeed;
                float centerAngle = bestAngle;
                for (int angleOffset = -2; angleOffset <= 2; angleOffset++)
                {
                    float angle = Mathf.Clamp(centerAngle + angleOffset * angleStep, 15f, 75f);
                    for (int speedOffset = -2; speedOffset <= 2; speedOffset++)
                    {
                        float speed = Mathf.Clamp(centerSpeed + speedOffset * speedStep, 5f, maxSpeed);
                        float error = SimulateClosestDistanceSquared(
                            start, target, speed, angle, mass, linearDrag, hardCeilingY,
                            windForce, windOrigin, windRadius, Physics2D.gravity, fixedStep);
                        if (error < bestError ||
                            (Mathf.Approximately(error, bestError) && speed < bestSpeed))
                        {
                            bestError = error;
                            bestSpeed = speed;
                            bestAngle = angle;
                        }
                    }
                }
                speedStep *= 0.4f;
                angleStep *= 0.4f;
            }

            float direction = Mathf.Sign(displacement.x);
            float angleRadians = bestAngle * Mathf.Deg2Rad;
            Vector2 result = new Vector2(
                direction * bestSpeed * Mathf.Cos(angleRadians),
                bestSpeed * Mathf.Sin(angleRadians));
            return IsFinite(result) ? result : displacement.normalized * 5f;
        }

        private static float SimulateClosestDistanceSquared(
            Vector2 start,
            Vector2 target,
            float speed,
            float angleDegrees,
            float mass,
            float linearDrag,
            float hardCeilingY,
            float windForce,
            Vector2 windOrigin,
            float windRadius,
            Vector2 gravity,
            float fixedStep)
        {
            float direction = Mathf.Sign(target.x - start.x);
            float angleRadians = angleDegrees * Mathf.Deg2Rad;
            Vector2 velocity = new Vector2(
                direction * speed * Mathf.Cos(angleRadians),
                speed * Mathf.Sin(angleRadians));
            Vector2 position = start;
            float bestDistanceSquared = (target - start).sqrMagnitude;
            float dragDivisor = 1f + Mathf.Max(0f, linearDrag) * fixedStep;

            // Eight simulated seconds is longer than a legal max-range launch. Keeping the
            // count fixed makes the solve deterministic and prevents an accidental hot loop.
            int maxSteps = Mathf.CeilToInt(8f / fixedStep);
            for (int step = 0; step < maxSteps; step++)
            {
                if (position.y > hardCeilingY && velocity.y > 0f)
                {
                    velocity = new Vector2(velocity.x, 0f);
                }

                Vector2 acceleration = gravity + UnitController.CalculateWindAcceleration(
                    position, mass, windForce, windOrigin, windRadius);
                velocity += acceleration * fixedStep;
                velocity /= dragDivisor;
                Vector2 nextPosition = position + velocity * fixedStep;

                Vector2 segment = nextPosition - position;
                float segmentLengthSquared = segment.sqrMagnitude;
                float along = segmentLengthSquared > 0.000001f
                    ? Mathf.Clamp01(Vector2.Dot(target - position, segment) / segmentLengthSquared)
                    : 0f;
                Vector2 closest = position + segment * along;
                bestDistanceSquared = Mathf.Min(bestDistanceSquared, (target - closest).sqrMagnitude);

                position = nextPosition;
                if (!IsFinite(position) || !IsFinite(velocity)) return float.PositiveInfinity;
            }

            return bestDistanceSquared;
        }

        private static bool IsFinite(Vector2 value) =>
            !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y);
    }
}
