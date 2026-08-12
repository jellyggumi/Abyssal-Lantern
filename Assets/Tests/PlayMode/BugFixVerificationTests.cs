using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using System.Collections;
using CastleBusters;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Regression tests for three fixes:
    ///  1) Gimmick selection (SelectUnit(3) must actually launch the explosive barrel prefab).
    ///  2) Y-axis kill-plane boundary (units below ChariotRules.KillPlaneY must die).
    ///  3) Wind effect radius scoping (wind must only push objects within windEffectRadius
    ///     of GameManager.windEffectOrigin, not the whole battlefield).
    /// </summary>
    public class BugFixVerificationTests
    {
        private static IEnumerator LoadAndBeginSiege()
        {
            SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.IsNotNull(GameManager.Instance, "GameManager must exist after scene load");
            GameManager.Instance.BeginSiege();
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.AreEqual(GameState.PlayerTurn, GameManager.Instance.currentState,
                "BeginSiege must hand control to the player before these tests run");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator SelectUnit3_LaunchesExplosiveBarrel_NotWrongPrefab()
        {
            yield return LoadAndBeginSiege();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm.explosiveBarrelPrefab, "Scene GameManager must have an explosiveBarrelPrefab assigned");

            var launchManager = Object.FindObjectOfType<LaunchManager>();
            Assert.IsNotNull(launchManager, "LaunchManager must exist in the scene");

            int barrelsBefore = Object.FindObjectsOfType<ExplosiveGimmick>().Length;


            gm.SelectUnit(3); // Gimmick slot
            yield return null;

            launchManager.SimulateLaunch(new Vector2(6f, 6f));
            yield return null;
            yield return new WaitForSecondsRealtime(0.2f);

            var barrelsAfter = Object.FindObjectsOfType<ExplosiveGimmick>();
            Assert.AreEqual(barrelsBefore + 1, barrelsAfter.Length,
                "SelectUnit(3) + launch must spawn exactly one new ExplosiveGimmick (the barrel), not a knight/archer/bomber");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator UnitBelowKillPlaneY_Dies()
        {
            yield return LoadAndBeginSiege();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm.knightPrefab, "Scene GameManager must have a knightPrefab assigned");
            Assert.Less(-25f, ChariotRules.KillPlaneY, "Sanity: test drop point (-25) must be below KillPlaneY (-20)");

            var unitGo = Object.Instantiate(gm.knightPrefab, new Vector3(0f, -25f, 0f), Quaternion.identity);
            Assert.IsNotNull(unitGo, "Knight prefab must instantiate");

            // Let Update() run for a couple of frames so the KillPlaneY check in
            // UnitController.Update() fires and Die() destroys the object.
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.1f);

            Assert.IsTrue(unitGo == null,
                "Unit placed below ChariotRules.KillPlaneY (-20) must be destroyed by the kill-plane check");
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator WindForce_OnlyAffectsObjectsWithinRadius()
        {
            yield return LoadAndBeginSiege();

            var gm = GameManager.Instance;
            gm.windEffectOrigin = Vector2.zero;
            gm.windEffectRadius = 10f;
            gm.currentWindForce = 8f; // strong easterly wind

            // In-range object: 3 units from origin, well inside the 10-unit radius.
            var inRangeGo = new GameObject("WindTest_InRange");
            var inRangeRb = inRangeGo.AddComponent<Rigidbody2D>();
            inRangeRb.gravityScale = 0f;
            inRangeGo.transform.position = new Vector3(3f, 0f, 0f);
            var inRangeArrow = inRangeGo.AddComponent<ArrowController>();
            inRangeArrow.Initialize(0f, true);

            // Out-of-range object: 50 units from origin, far outside the 10-unit radius.
            var outOfRangeGo = new GameObject("WindTest_OutOfRange");
            var outOfRangeRb = outOfRangeGo.AddComponent<Rigidbody2D>();
            outOfRangeRb.gravityScale = 0f;
            outOfRangeGo.transform.position = new Vector3(50f, 0f, 0f);
            var outOfRangeArrow = outOfRangeGo.AddComponent<ArrowController>();
            outOfRangeArrow.Initialize(0f, true);

            // Run several fixed-update physics steps so wind force (applied in
            // ArrowController.FixedUpdate) has time to accumulate into velocity.
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float inRangeSpeedX = inRangeRb.linearVelocity.x;
            float outOfRangeSpeedX = outOfRangeRb.linearVelocity.x;

            Object.Destroy(inRangeGo);
            Object.Destroy(outOfRangeGo);

            Assert.Greater(Mathf.Abs(inRangeSpeedX), 0.01f,
                "Object within windEffectRadius of windEffectOrigin must be pushed by wind");
            Assert.AreEqual(0f, outOfRangeSpeedX, 0.0001f,
                "Object outside windEffectRadius of windEffectOrigin must NOT be affected by wind (no other horizontal forces act on it)");
        }
        [UnityTest]
        [Timeout(60000)]
        public IEnumerator UnitAboveOldCeiling_DoesNotDisappear()
        {
            // Regression test for: "화면 위 y축으로 어느 이상 올라가면 사라진다" — units flung
            // high by a big arc/wind gust/knight push used to be instantly Die()'d the moment
            // they crossed UnitController.playableBounds' hard-coded ceiling (yMax = 14.5),
            // even while still clearly visible inside the camera's actual view. The fix removes
            // the ceiling check from IsOutOfPlayableBounds — only the side walls and the floor
            // remain hard boundaries — so a unit high above the old ceiling, but still within
            // the horizontal play area, must NOT be destroyed by the bounds check.
            yield return LoadAndBeginSiege();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm.knightPrefab, "Scene GameManager must have a knightPrefab assigned");

            var unitGo = Object.Instantiate(gm.knightPrefab, new Vector3(0f, 30f, 0f), Quaternion.identity);
            Assert.IsNotNull(unitGo, "Knight prefab must instantiate");
            var unit = unitGo.GetComponent<UnitController>();
            Assert.IsNotNull(unit, "Instantiated knight must have a UnitController");

            // Force it into the Launched state (the state whose FixedUpdate path runs the
            // playableBounds safety check) without going through a real trajectory launch.
            unit.InitializeUnit(true, UnitState.Launched);
            var rb = unitGo.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsFalse(unitGo == null,
                "A Launched unit at y=30 (above the old hard-coded ceiling of 14.5, but well within " +
                "the horizontal play area) must NOT be destroyed by the playable-bounds check");

            Object.Destroy(unitGo);
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator UnitOutsideHorizontalBounds_StillDies()
        {
            // Companion regression test: removing the ceiling must NOT disable the side-wall
            // out-of-bounds kill — a unit launched far outside playableBounds.xMin/xMax still
            // has to die.
            yield return LoadAndBeginSiege();

            var gm = GameManager.Instance;
            Assert.IsNotNull(gm.knightPrefab, "Scene GameManager must have a knightPrefab assigned");

            var unitGo = Object.Instantiate(gm.knightPrefab, new Vector3(1000f, 0f, 0f), Quaternion.identity);
            Assert.IsNotNull(unitGo, "Knight prefab must instantiate");
            var unit = unitGo.GetComponent<UnitController>();
            Assert.IsNotNull(unit, "Instantiated knight must have a UnitController");

            unit.InitializeUnit(true, UnitState.Launched);
            var rb = unitGo.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.IsTrue(unitGo == null,
                "A Launched unit at x=1000 (far outside playableBounds.xMin/xMax) must still be " +
                "destroyed by the playable-bounds check — only the ceiling rule was removed");
        }

    }
}
