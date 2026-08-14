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

        /// <summary>
        /// Counts the enemy's WALL blocks, excluding ground tiles.
        ///
        /// The first version counted every DestructibleBlock parented under the enemy castle and
        /// returned 143 where one side's keep is 15 blocks — the ground tiling is parented there too
        /// (task #49 found the same thing about scene-authored kegs). A counter an order of magnitude
        /// off cannot measure a two-block hit, so it excluded nothing and proved nothing.
        /// </summary>
        private static int EnemyWallBlocks()
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.enemyCastle == null) return -1;
            int n = 0;
            foreach (var b in gm.enemyCastle.GetComponentsInChildren<DestructibleBlock>(true))
            {
                if (b == null || b is CastleCoreGimmick) continue;
                if (b.isGroundAnchor) continue;   // the ground is not the wall
                n++;
            }
            return n;
        }

        /// <summary>
        /// Fires into the window the curve now provides and reports whether the enemy actually
        /// loses health. This is the measurement the earlier run could not make: it fired at a draw
        /// the old linear curve put short of the keep, so "core HP unchanged" proved only that a
        /// miss does no damage.
        /// </summary>
        [UnityTest]
        [Timeout(180000)]
        public IEnumerator Probe_OneShotIntoTheKeepWindow()
        {
            yield return BootMatch();

            var lm = Object.FindFirstObjectByType<LaunchManager>();
            Assert.IsNotNull(lm, "the scene must provide a LaunchManager");

            var core = EnemyCore();
            Assert.IsNotNull(core, "the enemy keep must have a core to damage");
            float hpBefore = core.currentHP;
            int wallsBefore = EnemyWallBlocks();

            // Mid-window at 45 degrees under the new curve. The draw is what the player controls,
            // so the probe speaks in draw and lets the curve produce the speed.
            const float draw = 0.85f;
            float speed = LaunchPowerCurve.SpeedForDraw(draw, lm.maxLaunchVelocity);
            float rad = 45f * Mathf.Deg2Rad;
            var v = new Vector2(speed * Mathf.Cos(rad), speed * Mathf.Sin(rad));
            Debug.Log($"[reach] cap={lm.maxLaunchVelocity:F2} draw={draw * 100:F0}% -> speed={speed:F2} velocity={v}");

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

            // Watch the flight, but do NOT treat the loop's last sample as the impact. The first
            // revision did, and its "1578 frames in Launched state" was the 12s cap being spent:
            // a knight that lands and rolls slowly stays in the Launched state until the stuck
            // monitor grounds it, so the recorded x was "where it was at the cutoff", which is a
            // statement about the stopwatch and not about the shot.
            float peakY = float.MinValue;
            int launchedFrames = 0;
            float watch = 0f;
            bool resolved = false;
            while (shot != null && watch < 20f)
            {
                if (shot.CurrentState == UnitState.Launched) launchedFrames++;
                peakY = Mathf.Max(peakY, shot.transform.position.y);
                if (shot.CurrentState != UnitState.Launched && launchedFrames > 0) { resolved = true; break; }
                watch += Time.unscaledDeltaTime;
                yield return null;
            }

            // Let the turn resolve so the trace seals and damage lands.
            yield return new WaitForSecondsRealtime(8f);

            // The sealed arc's final vertex IS the impact point — the trace director records it
            // during flight and freezes it at the settle boundary, which is exactly the moment the
            // shot stopped being a shot.
            float impactX = float.NaN;
            var traceGo = GameObject.Find("ShotTrace_Player");
            if (traceGo != null)
            {
                var line = traceGo.GetComponent<LineRenderer>();
                if (line != null && line.positionCount > 0)
                    impactX = line.GetPosition(line.positionCount - 1).x;
            }

            float hpAfter = core != null ? core.currentHP : -1f;
            int wallsAfter = EnemyWallBlocks();

            var sb = new StringBuilder();
            sb.AppendLine("[reach] ---- result ----");
            sb.AppendLine($"[reach] flight resolved on its own : {resolved} (watched {watch:F1}s, {launchedFrames} frames)");
            sb.AppendLine($"[reach] peak height y             : {peakY:F2}");
            sb.AppendLine($"[reach] IMPACT x (sealed arc)      : {impactX:F2}   (enemy keep spans {KeepLo}..{KeepHi})");
            sb.AppendLine($"[reach] reached the keep?         : {(impactX >= KeepLo ? "YES" : "NO — fell short")}");
            sb.AppendLine($"[reach] enemy core HP             : {hpBefore:F1} -> {hpAfter:F1}  (delta {hpAfter - hpBefore:F1})");
            sb.AppendLine($"[reach] enemy wall blocks (noisy)  : {wallsBefore} -> {wallsAfter}");
            sb.AppendLine($"[reach] readback line             : \"{ShotTraceDirector.LatestLine}\"");
            Debug.Log(sb.ToString());

            Assert.Greater(launchedFrames, 0,
                "the launch must at least produce a projectile in the Launched state — everything "
                + "else in this probe is a measurement, not a gate");
        }

        /// <summary>
        /// The flying beast's current HP, or NaN when there is no beast in the scene.
        ///
        /// Found by name because the chariot is created bare in
        /// <c>GameManager.SpawnMovingGimmick</c> and its <c>DestructibleBlock</c> is added at
        /// runtime, so there is no prefab or static to ask.
        /// </summary>
        private static float BeastHp()
        {
            var go = GameObject.Find("MovingObstacle");
            if (go == null) return float.NaN;
            var db = go.GetComponent<DestructibleBlock>();
            return db != null ? db.currentHP : float.NaN;
        }

        /// <summary>
        /// The x of the last vertex of the player's sealed arc, or NaN when there is no arc.
        ///
        /// Read through <c>GameObject.Find</c> rather than a static because the trace root is
        /// destroyed and rebuilt per shot, so a cached reference goes stale silently — the same
        /// Unity-null trap <c>ShotTraceDirector</c> documents about its own fields.
        /// </summary>
        private static float SealedArcX()
        {
            var traceGo = GameObject.Find("ShotTrace_Player");
            if (traceGo == null) return float.NaN;
            var line = traceGo.GetComponent<LineRenderer>();
            if (line == null || line.positionCount == 0) return float.NaN;
            return line.GetPosition(line.positionCount - 1).x;
        }

        /// <summary>
        /// Sweeps power at a fixed 45 degrees and prints where each shot ends up. One boot per shot,
        /// because the one-shot turn gate allows exactly one launch per turn by design.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Probe_PowerSweepAtFortyFiveDegrees()
        {
            // Resolution matters more than range, and the run has to fit one batch-mode session:
            // ten samples at an 8s settle each overran the timeout with no output at all. These five
            // bracket the edge the coarse sweep left between 60% (fell 6.6u short of 80%) and 94%
            // (the first sample that landed on the keep).
            //
            // Three runs of this sweep gave three different answers at the SAME draw (86% landed at
            // 0.40, then -13.42, then 6.87) while peak height stayed identical, so the launch is
            // deterministic and something downstream is not. The projectile is logged per sample
            // because the roster rotates per turn: a knight and a barrel are different masses and
            // different colliders, and comparing their ranges as if they were one weapon would make
            // every figure in this sweep meaningless.
            // ONE sample. Each costs a scene boot plus an 8s settle, and batch mode kept dying to
            // the domain-reload hang before printing anything: five samples failed twice, three
            // succeeded once (82s) then failed twice more at 450s. 60% is the draw that gets
            // intercepted at midfield, which is the case worth watching — it is the one whose
            // readback used to claim a wall breach. Widen this list when investigating range again,
            // and expect to fight the reload.
            foreach (float power in new[] { 0.60f })
            {
                yield return BootMatch();

                var lm = Object.FindFirstObjectByType<LaunchManager>();
                Assert.IsNotNull(lm);

                // Baselines before the shot, so "did anything take damage" is answerable.
                float beastHpBefore = BeastHp();
                int wallsBefore = EnemyWallBlocks();

                // Sweep in DRAW, not speed: draw is what the player controls, and the curve is
                // what turns it into speed. Sweeping speed would hide the very thing being fixed.
                float speed = LaunchPowerCurve.SpeedForDraw(power, lm.maxLaunchVelocity);
                float rad = 45f * Mathf.Deg2Rad;
                lm.SimulateLaunch(new Vector2(speed * Mathf.Cos(rad), speed * Mathf.Sin(rad)));
                yield return null;

                UnitController shot = null;
                for (int i = 0; i < UnitController.Active.Count; i++)
                {
                    var u = UnitController.Active[i];
                    if (u != null && u.isPlayerUnit && u.CurrentState == UnitState.Launched) { shot = u; break; }
                }

                // Identity and physics of THIS shot, captured while it is still alive. A range table
                // that silently mixes weapons is worse than no table.
                string projectile = "none";
                float mass = float.NaN, drag = float.NaN, colliderW = float.NaN;
                if (shot != null)
                {
                    projectile = shot.unitType.ToString();
                    if (shot.TryGetComponent<Rigidbody2D>(out var srb))
                    {
                        mass = srb.mass;
                        drag = srb.linearDamping;
                    }
                    var col = shot.GetComponent<Collider2D>();
                    if (col != null) colliderW = col.bounds.size.x;
                }

                float peakY = float.MinValue;
                int launchedFrames = 0;
                float watch = 0f;
                while (shot != null && watch < 20f)
                {
                    if (shot.CurrentState == UnitState.Launched) launchedFrames++;
                    peakY = Mathf.Max(peakY, shot.transform.position.y);
                    if (shot.CurrentState != UnitState.Launched && launchedFrames > 0) break;
                    watch += Time.unscaledDeltaTime;
                    yield return null;
                }

                // Three readings, not one, because the previous single reading produced a table that
                // was not monotonic in draw and I could not tell which part was the game and which
                // was the measurement. Two candidate causes had to be separated: the shot still
                // rolling when sampled, and the trace being replaced during the settle wait.
                //
                //   arcAtResolve - the sealed arc the instant the shot leaves Launched
                //   arcAfterWait - the same arc 8 seconds later, once the turn has moved on
                //   unitRestX    - where the projectile object itself ended up
                //
                // If arcAtResolve and arcAfterWait differ, the wait is the problem and the earlier
                // numbers were reading something other than this shot.
                float arcAtResolve = SealedArcX();
                float unitRestAtResolve = shot != null ? shot.transform.position.x : float.NaN;

                // Capture the readback DURING the settle, sampling until it stops being ours.
                //
                // The 8s wait lets the AI take its turn, and its shot seals over LatestLine. I read
                // the line after the wait and drew a conclusion from it about MY shot - it began
                // "적 기사", which is the enemy describing its own shot, so the comparison was
                // between my hit target and someone else's readback. Recording the owner flag is
                // what makes that mistake impossible to repeat silently.
                string ourLine = ShotTraceDirector.LatestLineByPlayer ? ShotTraceDirector.LatestLine : "";
                for (float t = 0f; t < 8f; t += Time.unscaledDeltaTime)
                {
                    if (ShotTraceDirector.LatestLineByPlayer && !string.IsNullOrEmpty(ShotTraceDirector.LatestLine))
                        ourLine = ShotTraceDirector.LatestLine;
                    yield return null;
                }
                float arcAfterWait = SealedArcX();
                float unitRestX = shot != null ? shot.transform.position.x : float.NaN;
                float impactX = !float.IsNaN(arcAtResolve) ? arcAtResolve : arcAfterWait;

                // WHAT was hit, not just where the arc ended.
                //
                // The first version of this sweep classified purely by x and produced a table where
                // impact x was NOT monotonic in draw: 78% landed at 3.16 while 86% landed at 0.40,
                // and two runs of the same 80-82% draw gave 6.10 then 2.33. Reading that as "the
                // shot fell short" would be wrong - the launch is monotonic (peak y rose 4.45 ->
                // 9.08 across the sweep), so something is INTERCEPTING the arc partway. Midfield
                // holds advancing enemy units and randomly-placed gimmicks, and a shot that hits one
                // has not fallen short: it hit a target. Naming the collider is the difference
                // between a defect and a design.
                string hit = "nothing found";
                if (!float.IsNaN(impactX))
                {
                    float best = float.MaxValue;
                    foreach (var u in UnitController.Active)
                    {
                        if (u == null || u.isPlayerUnit) continue;
                        float d = Mathf.Abs(u.transform.position.x - impactX);
                        if (d < best && d < 1.2f) { best = d; hit = $"enemy unit {u.unitType} at x={u.transform.position.x:F2}"; }
                    }
                    foreach (var b in DestructibleBlock.Active)
                    {
                        if (b == null) continue;
                        float d = Mathf.Abs(b.transform.position.x - impactX);
                        if (d < best && d < 1.2f)
                        {
                            best = d;
                            hit = $"{(b.isGroundAnchor ? "GROUND tile" : "wall block")} '{b.name}' at x={b.transform.position.x:F2}";
                        }
                    }
                }

                string verdict = float.IsNaN(impactX) ? "no sealed arc"
                    : impactX < KeepLo ? "SHORT"
                    : impactX <= KeepHi ? "ON THE KEEP"
                    : "PAST the keep";
                // An intercepted shot is only a wasted shot if nothing takes damage. The midfield
                // holds a 3.1u-wide flying beast at altitude 4.2±1.8 that sweeps the lane the arc
                // must cross, and it carries its own DestructibleBlock HP - so "hit the beast" may
                // be a legitimate hit rather than a lost turn. The readback line is what the player
                // is actually told, so it is recorded verbatim next to the numbers.
                Debug.Log($"[sweep] 45deg draw={power * 100:F0}%  proj={projectile} "
                          + $"mass={mass:F2} colW={colliderW:F2}  peakY={peakY:F2}  "
                          + $"end={arcAfterWait:F2}  -> {verdict}  | hit: {hit}"
                          + $"\n         beastHP={beastHpBefore:F0}->{BeastHp():F0}  walls={wallsBefore}->{EnemyWallBlocks()}"
                          + $"  ourReadback=\"{ourLine}\""
                          + $"  lastLine=\"{ShotTraceDirector.LatestLine}\" (byPlayer={ShotTraceDirector.LatestLineByPlayer})");
            }

            Assert.Pass("sweep recorded; see the [sweep] lines");
        }

        /// <summary>
        /// Hunts a shot that lands ON the core, then reports whether the core loses HP.
        ///
        /// This is the measurement `qa/aim-space-reachability.md` §5 calls the top outstanding one.
        /// Every shot measured so far stopped at the wall, which is the design working — the wall is
        /// meant to be in the way — so "the core did not lose HP" has never actually been a test of
        /// the damage path.
        ///
        /// The offline model says NO angle/draw reaches the core under the current curve: its best
        /// candidate is 59.5 degrees at 99% draw, landing at x=10.38, which is 0.23u past the core's
        /// far edge. That model has been wrong about this engine before (it put full draw 7u short of
        /// where the live scene actually landed), so this sweeps the live scene around that candidate
        /// instead of trusting either number.
        /// </summary>
        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Probe_HuntACoreHit()
        {
            // The model's near-miss bracket, plus one lower angle in case the engine's real range
            // sits short of the model's.
            (float angle, float draw)[] candidates =
            {
                (59.5f, 0.99f),
                (59.5f, 0.96f),
                (62.0f, 0.99f),
                (55.0f, 0.99f),
            };

            foreach (var c in candidates)
            {
                yield return BootMatch();

                var lm = Object.FindFirstObjectByType<LaunchManager>();
                Assert.IsNotNull(lm);
                var core = EnemyCore();
                Assert.IsNotNull(core, "the enemy keep must have a core");

                float hpBefore = core.currentHP;
                int wallsBefore = EnemyWallBlocks();

                float speed = LaunchPowerCurve.SpeedForDraw(c.draw, lm.maxLaunchVelocity);
                float rad = c.angle * Mathf.Deg2Rad;
                lm.SimulateLaunch(new Vector2(speed * Mathf.Cos(rad), speed * Mathf.Sin(rad)));
                yield return null;

                UnitController shot = null;
                for (int i = 0; i < UnitController.Active.Count; i++)
                {
                    var u = UnitController.Active[i];
                    if (u != null && u.isPlayerUnit && u.CurrentState == UnitState.Launched) { shot = u; break; }
                }

                float peakY = float.MinValue;
                int launched = 0;
                float watch = 0f;
                while (shot != null && watch < 20f)
                {
                    if (shot.CurrentState == UnitState.Launched) launched++;
                    peakY = Mathf.Max(peakY, shot.transform.position.y);
                    if (shot.CurrentState != UnitState.Launched && launched > 0) break;
                    watch += Time.unscaledDeltaTime;
                    yield return null;
                }

                // Let the collapse chain and any credited damage resolve before reading HP.
                yield return new WaitForSecondsRealtime(6f);

                float impactX = float.NaN;
                var traceGo = GameObject.Find("ShotTrace_Player");
                if (traceGo != null)
                {
                    var line = traceGo.GetComponent<LineRenderer>();
                    if (line != null && line.positionCount > 0)
                        impactX = line.GetPosition(line.positionCount - 1).x;
                }

                float hpAfter = core.currentHP;
                int wallsAfter = EnemyWallBlocks();
                bool onCore = !float.IsNaN(impactX) && impactX >= 7.85f && impactX <= 10.15f;

                Debug.Log($"[core] {c.angle:F1}deg draw={c.draw * 100:F0}% speed={speed:F2} "
                          + $"peakY={peakY:F2} impactX={impactX:F2} onCore={onCore} "
                          + $"| coreHP {hpBefore:F1} -> {hpAfter:F1} (delta {hpAfter - hpBefore:F1}) "
                          + $"| walls {wallsBefore} -> {wallsAfter}");
            }

            Assert.Pass("core-hunt recorded; see the [core] lines");
        }
    }
}
