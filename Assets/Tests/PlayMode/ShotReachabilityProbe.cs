using System.Collections;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Reports where a launched shot actually ends up, and whether the enemy loses anything when
    /// it does. Diagnostic, not a gate: it asserts only that a launch produced a projectile, so a
    /// balance number can never fail the build.
    ///
    /// Written for two user reports that may be the same defect — "the arc's angle cannot hit the
    /// enemy" and "the enemy's energy does not go down when hit". An offline model of the
    /// integration says most (angle, power) pairs fall short of the keep, but a model is not a
    /// measurement: it knows nothing about colliders, the ground tiles in the corridor, or the
    /// bodies already standing on the field. This runs the real scene and prints what happened.
    /// </summary>
    public class ShotReachabilityProbe
    {
        // Enemy structure band around the core at x=+9 (outpost + stepped courses).
        private const float KeepLo = 6.0f;
        private const float KeepHi = 11.5f;

        [SetUp]
        public void SetUp() => LogAssert.ignoreFailingMessages = true;

        [TearDown]
        public void TearDown() => LogAssert.ignoreFailingMessages = false;

        private static void SetPrivate(object target, string field, object value)
        {
            var f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"expected private field {target.GetType().Name}.{field}");
            f.SetValue(target, value);
        }

        private static IEnumerator BootMatch()
        {
            LogAssert.ignoreFailingMessages = true;
            GameManager.PendingStage = StageId.Stage1;
            SceneManager.LoadScene("SampleScene", LoadSceneMode.Single);
            yield return null;
            yield return new WaitForSecondsRealtime(2f);
            LogAssert.ignoreFailingMessages = true;

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm, "the arena must have a GameManager");
            gm.BeginSiege();
            if (NarrativeVideoIntro.Active != null) NarrativeVideoIntro.Active.Skip();
            if (StageInterludeController.Active != null) StageInterludeController.Active.Dismiss();
            SetPrivate(gm, "isPlayerTurn", true);
            SetPrivate(gm, "isResolvingTurn", false);
            gm.currentState = GameState.PlayerTurn;
            yield return null;
        }

        private static CastleCoreGimmick EnemyCore()
        {
            var gm = GameManager.Instance;
            return gm != null && gm.enemyCastle != null
                ? gm.enemyCastle.GetComponentInChildren<CastleCoreGimmick>(true)
                : null;
        }

        private static int EnemyWallBlocks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.enemyCastle == null) return -1;
            int n = 0;
            foreach (var b in gm.enemyCastle.GetComponentsInChildren<DestructibleBlock>(true))
            {
                if (b != null && !(b is CastleCoreGimmick)) n++;
            }
            return n;
        }

        /// <summary>
        /// Fires one shot at a given angle and power and records the outcome.
        ///
        /// The angle/power pair comes from the offline model's best candidates, so if the shot still
        /// falls short the model and the engine disagree — which is itself the finding.
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Probe_OneShotAtTheModelsBestAngle()
        {
            yield return BootMatch();

            var lm = Object.FindFirstObjectByType<LaunchManager>();
            Assert.IsNotNull(lm, "the scene must provide a LaunchManager");

            var core = EnemyCore();
            Assert.IsNotNull(core, "the enemy keep must have a core to damage");
            float hpBefore = core.currentHP;
            int wallsBefore = EnemyWallBlocks();

            // Model said 70% power at ~25 degrees lands inside the keep band.
            float speed = lm.maxLaunchVelocity * 0.70f;
            float rad = 25f * Mathf.Deg2Rad;
            var v = new Vector2(speed * Mathf.Cos(rad), speed * Mathf.Sin(rad));
            Debug.Log($"[reach] firing angle=25deg power=70% velocity={v} (|v|={v.magnitude:F2})");

            lm.SimulateLaunch(v);
            yield return null;

            // Follow whichever player projectile exists, and record its peak and resting place.
            UnitController shot = null;
            for (int i = 0; i < UnitController.Active.Count; i++)
            {
                var u = UnitController.Active[i];
                if (u != null && u.isPlayerUnit && u.CurrentState == UnitState.Launched) { shot = u; break; }
            }
            Debug.Log($"[reach] projectile found={(shot != null)}");

            float peakY = float.MinValue, lastX = float.NaN;
            int launchedFrames = 0;
            float watch = 0f;
            while (shot != null && watch < 12f)
            {
                if (shot.CurrentState == UnitState.Launched) launchedFrames++;
                peakY = Mathf.Max(peakY, shot.transform.position.y);
                lastX = shot.transform.position.x;
                if (shot.CurrentState != UnitState.Launched && launchedFrames > 0) break;
                watch += Time.unscaledDeltaTime;
                yield return null;
            }

            // Let the turn resolve so damage and the readback land.
            yield return new WaitForSecondsRealtime(6f);

            float hpAfter = core != null ? core.currentHP : -1f;
            int wallsAfter = EnemyWallBlocks();

            var sb = new StringBuilder();
            sb.AppendLine("[reach] ---- result ----");
            sb.AppendLine($"[reach] frames in Launched state : {launchedFrames}");
            sb.AppendLine($"[reach] peak height y            : {peakY:F2}");
            sb.AppendLine($"[reach] x where flight ended     : {lastX:F2}   (enemy keep spans {KeepLo}..{KeepHi})");
            sb.AppendLine($"[reach] reached the keep?        : {(lastX >= KeepLo ? "YES" : "NO — fell short")}");
            sb.AppendLine($"[reach] enemy core HP            : {hpBefore:F1} -> {hpAfter:F1}  (delta {hpAfter - hpBefore:F1})");
            sb.AppendLine($"[reach] enemy wall blocks        : {wallsBefore} -> {wallsAfter}  (delta {wallsAfter - wallsBefore})");
            sb.AppendLine($"[reach] readback line            : \"{ShotTraceDirector.LatestLine}\"");
            Debug.Log(sb.ToString());

            Assert.Greater(launchedFrames, 0,
                "the launch must at least produce a projectile in the Launched state — everything "
                + "else in this probe is a measurement, not a gate");
        }

        /// <summary>
        /// Sweeps power at a fixed 45 degrees and prints where each shot ends up. One boot per shot,
        /// because the one-shot turn gate allows exactly one launch per turn by design.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Probe_PowerSweepAtFortyFiveDegrees()
        {
            foreach (float power in new[] { 0.4f, 0.6f, 0.8f, 1.0f })
            {
                yield return BootMatch();

                var lm = Object.FindFirstObjectByType<LaunchManager>();
                Assert.IsNotNull(lm);

                float speed = lm.maxLaunchVelocity * power;
                float rad = 45f * Mathf.Deg2Rad;
                lm.SimulateLaunch(new Vector2(speed * Mathf.Cos(rad), speed * Mathf.Sin(rad)));
                yield return null;

                UnitController shot = null;
                for (int i = 0; i < UnitController.Active.Count; i++)
                {
                    var u = UnitController.Active[i];
                    if (u != null && u.isPlayerUnit && u.CurrentState == UnitState.Launched) { shot = u; break; }
                }

                float peakY = float.MinValue, lastX = float.NaN;
                int launchedFrames = 0;
                float watch = 0f;
                while (shot != null && watch < 10f)
                {
                    if (shot.CurrentState == UnitState.Launched) launchedFrames++;
                    peakY = Mathf.Max(peakY, shot.transform.position.y);
                    lastX = shot.transform.position.x;
                    if (shot.CurrentState != UnitState.Launched && launchedFrames > 0) break;
                    watch += Time.unscaledDeltaTime;
                    yield return null;
                }

                string verdict = float.IsNaN(lastX) ? "no projectile"
                    : lastX < KeepLo ? "SHORT"
                    : lastX <= KeepHi ? "ON THE KEEP"
                    : "PAST the keep";
                Debug.Log($"[sweep] 45deg power={power * 100:F0}%  launchedFrames={launchedFrames}  "
                          + $"peakY={peakY:F2}  endX={lastX:F2}  -> {verdict}");
            }

            Assert.Pass("sweep recorded; see the [sweep] lines");
        }
    }
}
