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
            inRangeArrow.Initialize(0f, 1f, true);

            // Out-of-range object: 50 units from origin, far outside the 10-unit radius.
            var outOfRangeGo = new GameObject("WindTest_OutOfRange");
            var outOfRangeRb = outOfRangeGo.AddComponent<Rigidbody2D>();
            outOfRangeRb.gravityScale = 0f;
            outOfRangeGo.transform.position = new Vector3(50f, 0f, 0f);
            var outOfRangeArrow = outOfRangeGo.AddComponent<ArrowController>();
            outOfRangeArrow.Initialize(0f, 1f, true);

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


        [UnityTest]
        [Timeout(60000)]
        public IEnumerator UnitMelee_DirectUnitDamage_AppliesOpeningMultiplierExactlyOnce_EnemyLaterAndNoCommitStayFull()
        {
            yield return LoadAndBeginSiege();

            var gameManager = GameManager.Instance;
            var snapshot = new OpeningVolleyStateSnapshot(gameManager);
            try
            {
                gameManager.enabled = false;
                gameManager.enforceOneShotTurns = true;
                ConfigureTurn(gameManager, turnCount: 0, isPlayerTurn: true);
                ResetCommittedShot(gameManager);

                yield return AssertMeleeDamage(
                    gameManager.knightPrefab,
                    attackerIsPlayer: true,
                    expectedDamage: 20f,
                    origin: new Vector3(-6f, 12f, 0f),
                    "Player melee before a shot is committed must remain full strength.");

                Assert.That(gameManager.TryCommitTurnShot(), Is.True,
                    "The opening fixture must commit its shot through GameManager's public turn gate.");
                yield return AssertMeleeDamage(
                    gameManager.knightPrefab,
                    attackerIsPlayer: true,
                    expectedDamage: 10f,
                    origin: new Vector3(-2f, 12f, 0f),
                    "The committed player opening melee must apply the 0.5 multiplier exactly once.");

                ConfigureTurn(gameManager, turnCount: 1, isPlayerTurn: true);
                yield return AssertMeleeDamage(
                    gameManager.knightPrefab,
                    attackerIsPlayer: true,
                    expectedDamage: 20f,
                    origin: new Vector3(2f, 12f, 0f),
                    "Player melee after the opening turn must return to full strength.");

                ConfigureTurn(gameManager, turnCount: 0, isPlayerTurn: true);
                yield return AssertMeleeDamage(
                    gameManager.knightPrefab,
                    attackerIsPlayer: false,
                    expectedDamage: 20f,
                    origin: new Vector3(6f, 12f, 0f),
                    "Enemy melee during the committed player opening turn must remain full strength.");
            }
            finally
            {
                snapshot.Restore(gameManager);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator CommittedMelee_KillsProductionFieldKeg_AndOnDestroyCarriesOpeningContext()
        {
            yield return LoadAndBeginSiege();

            const float targetHp = 500f;
            var gameManager = GameManager.Instance;
            var snapshot = new OpeningVolleyStateSnapshot(gameManager);
            float originalTimeScale = Time.timeScale;
            Random.State originalRandomState = Random.state;
            GameObject attackerObject = null;
            GameObject fieldKegObject = null;
            GameObject targetObject = null;
            var kegPosition = new Vector3(3f, 12f, 0f);

            try
            {
                PrepareCommittedOpeningShot(gameManager);
                fieldKegObject = CreateFieldKeg(
                    gameManager,
                    "OpeningVolleyMelee_FieldKeg",
                    kegPosition);
                var kegBlock = fieldKegObject.GetComponent<DestructibleBlock>();
                Assert.That(kegBlock, Is.Not.Null,
                    "The production field keg must expose the block whose fatal melee hit drives OnDestroy.");
                targetObject = CreateDamageTarget(
                    "OpeningVolleyMelee_SplashTarget",
                    kegPosition + Vector3.up * 1.2f,
                    isPlayer: false,
                    targetHp);
                var target = targetObject.GetComponent<UnitController>();

                attackerObject = Object.Instantiate(
                    gameManager.knightPrefab,
                    kegPosition + Vector3.left * 0.75f,
                    Quaternion.identity);
                attackerObject.name = "OpeningVolleyMelee_FieldKegKnight";
                var attacker = attackerObject.GetComponent<UnitController>();
                Assert.That(attacker, Is.Not.Null, "The shipped Knight prefab must contain UnitController.");
                attacker.InitializeUnit(isPlayer: true, UnitState.Grounded);
                attacker.unitType = UnitType.Knight;
                attacker.attackDamage = 30f;
                attacker.attackRange = 2f;
                attacker.attackCooldown = 999f;
                attacker.moveSpeed = 0f;
                FreezeUnitBody(attackerObject);
                SetPrivateField(
                    attacker,
                    "lastAttackTime",
                    Time.time - attacker.attackCooldown - 1f);
                Assert.That(gameManager.TurnCount, Is.Zero,
                    "Committed melee is intentionally an immediate pre-handoff route.");
                Assert.That(gameManager.IsPlayerTurn, Is.True,
                    "Committed melee must resolve while the opening player turn is still active.");

                Vector2Int scoreBefore = ReadScoreboard(gameManager);
                float firstAttackDeadline = Time.realtimeSinceStartup + 3f;
                while (fieldKegObject != null &&
                       kegBlock.currentHP > 0f &&
                       Time.realtimeSinceStartup < firstAttackDeadline)
                {
                    yield return null;
                }
                Assert.That(
                    fieldKegObject == null || kegBlock.currentHP <= 0f,
                    Is.True,
                    "The committed production melee route must deal the field keg's fatal first hit.");
                attacker.enabled = false;

                yield return WaitForExplosionOutcome(
                    fieldKegObject,
                    target,
                    targetHp,
                    requireFieldKegDestroyed: true);

                AssertPlayerScoreDelta(gameManager, scoreBefore, expectedPlayerDelta: 100);
            }
            finally
            {
                HitStopManager.Instance?.CancelPendingHitStop();
                Time.timeScale = originalTimeScale;
                Random.state = originalRandomState;
                DestroyPickupsAt(kegPosition);
                if (attackerObject != null) Object.Destroy(attackerObject);
                if (fieldKegObject != null) Object.Destroy(fieldKegObject);
                if (targetObject != null) Object.Destroy(targetObject);
                snapshot.Restore(gameManager);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator FatalFieldKegHit_FirstSameFrameContextRemainsAuthoritativeThroughDeferredOnDestroy()
        {
            yield return LoadAndBeginSiege();

            const float targetHp = 500f;
            var gameManager = GameManager.Instance;
            var snapshot = new OpeningVolleyStateSnapshot(gameManager);
            float originalTimeScale = Time.timeScale;
            Random.State originalRandomState = Random.state;
            GameObject fieldKegObject = null;
            GameObject targetObject = null;
            var isolatedKegPosition = new Vector3(48f, 12f, 0f);

            try
            {
                PrepareCommittedOpeningShot(gameManager);
                float capturedMultiplier = GameManager.CaptureDamageMultiplier(true);
                Assert.That(capturedMultiplier, Is.EqualTo(0.5f).Within(0.0001f),
                    "The fatal first hit must carry the committed player's opening multiplier.");

                fieldKegObject = CreateFieldKeg(
                    gameManager,
                    "OpeningVolleyFatalContextRace_FieldKeg",
                    isolatedKegPosition);
                var kegBlock = fieldKegObject.GetComponent<DestructibleBlock>();
                targetObject = CreateDamageTarget(
                    "OpeningVolleyFatalContextRace_SplashTarget",
                    isolatedKegPosition + Vector3.up * 1.2f,
                    isPlayer: false,
                    targetHp);
                var target = targetObject.GetComponent<UnitController>();
                Vector2Int scoreBefore = ReadScoreboard(gameManager);

                kegBlock.TakeDamage(
                    kegBlock.currentHP,
                    damageFromPlayer: true,
                    sourceMultiplier: capturedMultiplier);
                Assert.That(kegBlock.currentHP, Is.LessThanOrEqualTo(0f),
                    "The first hit must synchronously enter the fatal destruction path.");
                Assert.That(fieldKegObject != null, Is.True,
                    "Unity must still be deferring Destroy/OnDestroy when the competing hit is delivered.");

                // No yield: this competing enemy hit arrives in the same frame, after destruction
                // started but before ExplosiveGimmick.OnDestroy consumes the fatal hit's context.
                kegBlock.TakeDamage(
                    kegBlock.maxHP,
                    damageFromPlayer: false,
                    sourceMultiplier: 1f);

                yield return WaitForExplosionOutcome(
                    fieldKegObject,
                    target,
                    targetHp,
                    requireFieldKegDestroyed: true);

                AssertPlayerScoreDelta(gameManager, scoreBefore, expectedPlayerDelta: 100);
            }
            finally
            {
                HitStopManager.Instance?.CancelPendingHitStop();
                Time.timeScale = originalTimeScale;
                Random.state = originalRandomState;
                DestroyPickupsAt(isolatedKegPosition);
                if (fieldKegObject != null) Object.Destroy(fieldKegObject);
                if (targetObject != null) Object.Destroy(targetObject);
                snapshot.Restore(gameManager);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator ArcherArrow_CapturedOpeningMultiplierHitsProductionFieldKegBeforeHandoff()
        {
            yield return LoadAndBeginSiege();

            const float attackDamage = 20f;
            const float targetHp = 500f;
            var gameManager = GameManager.Instance;
            var snapshot = new OpeningVolleyStateSnapshot(gameManager);
            float originalTimeScale = Time.timeScale;
            Random.State originalRandomState = Random.state;
            GameObject archerObject = null;
            GameObject fieldKegObject = null;
            GameObject targetObject = null;
            ArrowController spawnedArrow = null;
            var kegPosition = new Vector3(3f, 12f, 0f);

            try
            {
                PrepareCommittedOpeningShot(gameManager);
                Assert.That(gameManager.archerPrefab, Is.Not.Null,
                    "The shipped Archer prefab is required to drive the production firing route.");
                fieldKegObject = CreateFieldKeg(
                    gameManager,
                    "OpeningVolleyArrow_FieldKeg",
                    kegPosition);

                var arrowsBefore = Object.FindObjectsOfType<ArrowController>();
                Assert.That(arrowsBefore, Is.Empty,
                    "A fresh siege must not contain another ArrowController that could contribute to the final HP delta.");
                archerObject = Object.Instantiate(
                    gameManager.archerPrefab,
                    new Vector3(-2f, 12f, 0f),
                    Quaternion.identity);
                archerObject.name = "OpeningVolleyArrow_Archer";
                var archer = archerObject.GetComponent<UnitController>();
                Assert.That(archer, Is.Not.Null, "The shipped Archer prefab must contain UnitController.");
                archer.InitializeUnit(isPlayer: true, UnitState.Grounded);
                archer.unitType = UnitType.Archer;
                archer.attackDamage = attackDamage;
                archer.attackRange = 8f;
                archer.attackCooldown = 0f;
                archer.moveSpeed = 0f;
                FreezeUnitBody(archerObject);

                for (int frame = 0; frame < 30 && spawnedArrow == null; frame++)
                {
                    yield return null;
                    foreach (var candidate in Object.FindObjectsOfType<ArrowController>())
                    {
                        if (System.Array.IndexOf(arrowsBefore, candidate) >= 0) continue;
                        spawnedArrow = candidate;
                        archerObject.SetActive(false);
                        break;
                    }
                }

                Assert.That(spawnedArrow, Is.Not.Null,
                    "The grounded shipped Archer must create an ArrowController through its real attack loop.");
                int newArrowCount = 0;
                foreach (var candidate in Object.FindObjectsOfType<ArrowController>())
                {
                    if (System.Array.IndexOf(arrowsBefore, candidate) < 0) newArrowCount++;
                }
                Assert.That(newArrowCount, Is.EqualTo(1),
                    "The Archer must be disabled on the first captured shot so exactly one projectile can own the HP delta.");
                Assert.That(archerObject.activeSelf, Is.False,
                    "The firing Archer must remain disabled before the splash target exists.");

                var arrowBody = spawnedArrow.GetComponent<Rigidbody2D>();
                Assert.That(arrowBody, Is.Not.Null, "The shipped Arrow prefab must retain its Rigidbody2D.");

                Assert.That(gameManager.TurnCount, Is.Zero,
                    "Arrow impact is intentionally pre-handoff because production waits for active arrows to resolve.");
                Assert.That(gameManager.IsPlayerTurn, Is.True,
                    "The opening player turn must remain active while its ArrowController is in flight.");
                targetObject = CreateDamageTarget(
                    "OpeningVolleyArrow_SplashTarget",
                    kegPosition + Vector3.up * 1.2f,
                    isPlayer: false,
                    targetHp);
                var target = targetObject.GetComponent<UnitController>();
                Vector2Int scoreBefore = ReadScoreboard(gameManager);

                spawnedArrow.transform.position = kegPosition + Vector3.left;
                arrowBody.gravityScale = 0f;
                arrowBody.linearVelocity = new Vector2(8f, 0f);
                Physics2D.SyncTransforms();

                yield return WaitForExplosionOutcome(
                    fieldKegObject,
                    target,
                    targetHp,
                    requireFieldKegDestroyed: false);

                Assert.That(archerObject.activeSelf, Is.False,
                    "Only the one captured Arrow may contribute to the final splash-target HP delta.");
                AssertPlayerScoreDelta(gameManager, scoreBefore, expectedPlayerDelta: 100);
            }
            finally
            {
                HitStopManager.Instance?.CancelPendingHitStop();
                Time.timeScale = originalTimeScale;
                Random.state = originalRandomState;
                DestroyPickupsAt(kegPosition);
                if (spawnedArrow != null) Object.Destroy(spawnedArrow.gameObject);
                if (archerObject != null) Object.Destroy(archerObject);
                if (fieldKegObject != null) Object.Destroy(fieldKegObject);
                if (targetObject != null) Object.Destroy(targetObject);
                snapshot.Restore(gameManager);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator CannonSplash_CapturedOpeningMultiplierSurvivesTurnHandoff()
        {
            yield return LoadAndBeginSiege();

            const float shellDamage = 80f;
            const float targetHp = 500f;
            var gameManager = GameManager.Instance;
            var snapshot = new OpeningVolleyStateSnapshot(gameManager);
            float originalTimeScale = Time.timeScale;
            Random.State originalRandomState = Random.state;
            GameObject fieldKegObject = null;
            GameObject targetObject = null;
            CannonShell shell = null;
            var kegPosition = new Vector3(3f, 12f, 0f);

            try
            {
                PrepareCommittedOpeningShot(gameManager);
                fieldKegObject = CreateFieldKeg(
                    gameManager,
                    "OpeningVolleyCannon_FieldKeg",
                    kegPosition);
                targetObject = CreateDamageTarget(
                    "OpeningVolleyCannon_SplashTarget",
                    kegPosition + Vector3.up * 1.2f,
                    isPlayer: false,
                    targetHp);
                var target = targetObject.GetComponent<UnitController>();

                shell = CannonShell.Spawn(
                    new Vector2(-10f, 30f),
                    Vector2.zero,
                    shellDamage,
                    splashRadius: 0.5f,
                    isPlayerShell: true);
                Assert.That(shell, Is.Not.Null,
                    "CannonShell.Spawn must create the real gravity-driven cannon projectile.");
                var shellBody = shell.GetComponent<Rigidbody2D>();
                Assert.That(shellBody, Is.Not.Null, "The real Cannon shell must retain its Rigidbody2D.");
                shellBody.gravityScale = 0f;

                yield return CompleteRealTurnHandoff(gameManager);

                Vector2Int scoreBefore = ReadScoreboard(gameManager);
                shell.transform.position = kegPosition;
                shellBody.gravityScale = 0f;
                shellBody.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();

                yield return WaitForExplosionOutcome(
                    fieldKegObject,
                    target,
                    targetHp,
                    requireFieldKegDestroyed: false);

                AssertPlayerScoreDelta(gameManager, scoreBefore, expectedPlayerDelta: 100);
            }
            finally
            {
                HitStopManager.Instance?.CancelPendingHitStop();
                Time.timeScale = originalTimeScale;
                Random.state = originalRandomState;
                DestroyPickupsAt(kegPosition);
                if (shell != null) Object.Destroy(shell.gameObject);
                if (fieldKegObject != null) Object.Destroy(fieldKegObject);
                if (targetObject != null) Object.Destroy(targetObject);
                snapshot.Restore(gameManager);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator LaunchedBarrelFuse_CapturedOpeningMultiplierResolvesBeforeTurnHandoff()
        {
            yield return LoadAndBeginSiege();

            const float explosionDamage = 80f;
            const float targetHp = 500f;
            var gameManager = GameManager.Instance;
            var snapshot = new OpeningVolleyStateSnapshot(gameManager);
            float originalTimeScale = Time.timeScale;
            Random.State originalRandomState = Random.state;
            GameObject barrelObject = null;
            GameObject landingBlockObject = null;
            GameObject targetObject = null;
            UnitController[] unrelatedUnits = null;
            CannonController[] unrelatedCannons = null;
            var launchPosition = new Vector3(-3f, 12f, 0f);
            var explosionPosition = Vector3.zero;

            try
            {
                PrepareCommittedOpeningShot(gameManager);
                Assert.That(gameManager.explosiveBarrelPrefab, Is.Not.Null,
                    "The shipped ExplosiveBarrel prefab is required to drive the launched fuse route.");
                unrelatedUnits = Object.FindObjectsOfType<UnitController>();
                foreach (var unit in unrelatedUnits)
                {
                    if (unit != null) unit.enabled = false;
                }
                unrelatedCannons = Object.FindObjectsOfType<CannonController>();
                foreach (var cannon in unrelatedCannons)
                {
                    if (cannon != null) cannon.enabled = false;
                }


                barrelObject = Object.Instantiate(
                    gameManager.explosiveBarrelPrefab,
                    launchPosition,
                    Quaternion.identity);
                barrelObject.name = "OpeningVolleyFuse_LaunchedBarrel";
                var barrel = barrelObject.AddComponent<UnitController>();
                barrel.unitType = UnitType.Barrel;
                barrel.InitializeUnit(isPlayer: true, UnitState.Idle);
                var barrelExplosive = barrelObject.GetComponent<ExplosiveGimmick>();
                Assert.That(barrelExplosive, Is.Not.Null,
                    "The shipped ExplosiveBarrel prefab must retain ExplosiveGimmick.");
                barrelExplosive.SetPermanentPotency(explosionDamage, 2f);

                landingBlockObject = new GameObject("OpeningVolleyFuse_LandingBlock");
                landingBlockObject.transform.position = launchPosition + Vector3.right * 0.75f;
                landingBlockObject.AddComponent<BoxCollider2D>();
                var landingBlock = landingBlockObject.AddComponent<DestructibleBlock>();
                landingBlock.maxHP = targetHp;
                landingBlock.currentHP = targetHp;

                var barrelBody = barrelObject.GetComponent<Rigidbody2D>();
                Assert.That(barrelBody, Is.Not.Null,
                    "The shipped launched Barrel must retain its Rigidbody2D.");
                barrelBody.gravityScale = 0f;
                barrel.Launch(new Vector2(2f, 0f));

                Assert.That(gameManager.TurnCount, Is.Zero,
                    "The production turn resolver waits for an armed Barrel fuse, so this delayed route is intentionally pre-handoff.");
                Assert.That(gameManager.IsPlayerTurn, Is.True,
                    "The opening player turn must remain active until its launched Barrel fuse resolves.");
                Vector2Int scoreBefore = ReadScoreboard(gameManager);
                barrelObject.transform.position = launchPosition;
                barrelBody.linearVelocity = new Vector2(2f, 0f);
                Physics2D.SyncTransforms();

                float armDeadline = Time.realtimeSinceStartup + 2f;
                for (int fixedStep = 0;
                     fixedStep < 120 && barrel != null && !barrel.IsFusePending &&
                     Time.realtimeSinceStartup < armDeadline;
                     fixedStep++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(barrel != null && barrel.IsFusePending, Is.True,
                    "A real launched Barrel collision must settle the body and arm its fuse after handoff.");

                explosionPosition = barrel.transform.position;
                targetObject = CreateDamageTarget(
                    "OpeningVolleyFuse_Target",
                    explosionPosition + Vector3.up * 1.2f,
                    isPlayer: false,
                    targetHp);
                var target = targetObject.GetComponent<UnitController>();

                float fuseDeadline = Time.realtimeSinceStartup + UnitCombos.BarrelFuseSeconds + 2f;
                while (barrel != null && Time.realtimeSinceStartup < fuseDeadline)
                {
                    yield return null;
                }

                Assert.That(barrel == null, Is.True,
                    "The launched shipped Barrel must complete its real fuse and destroy its body.");
                Assert.That(
                    targetHp - target.currentHP,
                    Is.EqualTo(explosionDamage * 0.5f).Within(0.0001f),
                    "A launched opening Barrel must retain its launch-time 0.5 multiplier through " +
                    "landing and its fuse delay without double scaling.");
                AssertPlayerScoreDelta(gameManager, scoreBefore, expectedPlayerDelta: 100);
            }
            finally
            {
                HitStopManager.Instance?.CancelPendingHitStop();
                Time.timeScale = originalTimeScale;
                Random.state = originalRandomState;
                if (unrelatedUnits != null)
                {
                    foreach (var unit in unrelatedUnits)
                    {
                        if (unit != null) unit.enabled = true;
                    }
                }
                if (unrelatedCannons != null)
                {
                    foreach (var cannon in unrelatedCannons)
                    {
                        if (cannon != null) cannon.enabled = true;
                    }
                }
                DestroyPickupsAt(explosionPosition);
                if (barrelObject != null) Object.Destroy(barrelObject);
                if (landingBlockObject != null) Object.Destroy(landingBlockObject);
                if (targetObject != null) Object.Destroy(targetObject);
                snapshot.Restore(gameManager);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator LaunchedUnitImpact_DelegatesOpeningDamageContextToProductionFieldKegBeforeHandoff()
        {
            yield return LoadAndBeginSiege();

            const float targetHp = 500f;
            var gameManager = GameManager.Instance;
            var snapshot = new OpeningVolleyStateSnapshot(gameManager);
            float originalTimeScale = Time.timeScale;
            Random.State originalRandomState = Random.state;
            GameObject attackerObject = null;
            GameObject fieldKegObject = null;
            GameObject targetObject = null;
            var kegPosition = new Vector3(3f, 12f, 0f);

            try
            {
                PrepareCommittedOpeningShot(gameManager);
                Assert.That(gameManager.knightPrefab, Is.Not.Null,
                    "The shipped Knight prefab is required to drive the launched-unit collision route.");
                fieldKegObject = CreateFieldKeg(
                    gameManager,
                    "OpeningVolleyDelegation_FieldKeg",
                    kegPosition);
                targetObject = CreateDamageTarget(
                    "OpeningVolleyDelegation_SplashTarget",
                    kegPosition + Vector3.up * 1.2f,
                    isPlayer: false,
                    targetHp);
                var target = targetObject.GetComponent<UnitController>();

                attackerObject = Object.Instantiate(
                    gameManager.knightPrefab,
                    new Vector3(-1f, 12f, 0f),
                    Quaternion.identity);
                attackerObject.name = "OpeningVolleyDelegation_LaunchedKnight";
                var attacker = attackerObject.GetComponent<UnitController>();
                Assert.That(attacker, Is.Not.Null,
                    "The shipped Knight prefab must contain UnitController.");
                attacker.InitializeUnit(isPlayer: true, UnitState.Idle);
                attacker.attackDamage = 30f;
                var attackerBody = attackerObject.GetComponent<Rigidbody2D>();
                Assert.That(attackerBody, Is.Not.Null,
                    "The shipped launched Knight must retain its Rigidbody2D.");
                attackerBody.gravityScale = 0f;
                attackerBody.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

                attacker.Launch(new Vector2(8f, 0f));

                Assert.That(gameManager.TurnCount, Is.Zero,
                    "Launched-unit impact is intentionally pre-handoff because production waits for launched bodies to settle.");
                Assert.That(gameManager.IsPlayerTurn, Is.True,
                    "The opening player turn must remain active while its launched unit is in flight.");
                Vector2Int scoreBefore = ReadScoreboard(gameManager);
                attackerObject.transform.position = kegPosition + Vector3.left;
                attackerBody.linearVelocity = new Vector2(8f, 0f);
                Physics2D.SyncTransforms();

                yield return WaitForExplosionOutcome(
                    fieldKegObject,
                    target,
                    targetHp,
                    requireFieldKegDestroyed: false);

                AssertPlayerScoreDelta(gameManager, scoreBefore, expectedPlayerDelta: 100);
            }
            finally
            {
                HitStopManager.Instance?.CancelPendingHitStop();
                Time.timeScale = originalTimeScale;
                Random.state = originalRandomState;
                DestroyPickupsAt(kegPosition);
                if (attackerObject != null) Object.Destroy(attackerObject);
                if (fieldKegObject != null) Object.Destroy(fieldKegObject);
                if (targetObject != null) Object.Destroy(targetObject);
                snapshot.Restore(gameManager);
            }
        }

        [UnityTest]
        [Timeout(60000)]
        public IEnumerator ChainedFieldKegExplosion_CapturedOpeningContextSurvivesRealTurnHandoff()
        {
            yield return LoadAndBeginSiege();

            const float targetHp = 500f;
            var gameManager = GameManager.Instance;
            var snapshot = new OpeningVolleyStateSnapshot(gameManager);
            float originalTimeScale = Time.timeScale;
            Random.State originalRandomState = Random.state;
            GameObject sourceKegObject = null;
            GameObject chainedKegObject = null;
            GameObject targetObject = null;
            var sourcePosition = new Vector3(3f, 12f, 0f);
            var chainedPosition = new Vector3(4.5f, 12f, 0f);

            try
            {
                PrepareCommittedOpeningShot(gameManager);
                float capturedMultiplier = GameManager.CaptureDamageMultiplier(true);
                Assert.That(capturedMultiplier, Is.EqualTo(0.5f).Within(0.0001f),
                    "The committed player opening action must capture the 0.5 multiplier before handoff.");

                sourceKegObject = CreateFieldKeg(
                    gameManager,
                    "OpeningVolleyChain_SourceFieldKeg",
                    sourcePosition);
                chainedKegObject = CreateFieldKeg(
                    gameManager,
                    "OpeningVolleyChain_ChainedFieldKeg",
                    chainedPosition);
                targetObject = CreateDamageTarget(
                    "OpeningVolleyChain_SplashTarget",
                    chainedPosition + Vector3.right * 1.7f,
                    isPlayer: false,
                    targetHp);
                var target = targetObject.GetComponent<UnitController>();

                yield return CompleteRealTurnHandoff(gameManager);
                Assert.That(GameManager.CaptureDamageMultiplier(true), Is.EqualTo(1f).Within(0.0001f),
                    "The live post-handoff turn must no longer qualify for opening damage.");

                Vector2Int scoreBefore = ReadScoreboard(gameManager);
                var sourceBlock = sourceKegObject.GetComponent<DestructibleBlock>();
                sourceBlock.TakeDamage(
                    sourceBlock.currentHP,
                    damageFromPlayer: true,
                    sourceMultiplier: capturedMultiplier);

                yield return WaitForExplosionOutcome(
                    chainedKegObject,
                    target,
                    targetHp,
                    requireFieldKegDestroyed: false);

                AssertPlayerScoreDelta(gameManager, scoreBefore, expectedPlayerDelta: 200);
            }
            finally
            {
                HitStopManager.Instance?.CancelPendingHitStop();
                Time.timeScale = originalTimeScale;
                Random.state = originalRandomState;
                DestroyPickupsAt(sourcePosition);
                DestroyPickupsAt(chainedPosition);
                if (sourceKegObject != null) Object.Destroy(sourceKegObject);
                if (chainedKegObject != null) Object.Destroy(chainedKegObject);
                if (targetObject != null) Object.Destroy(targetObject);
                snapshot.Restore(gameManager);
            }
        }

        private static IEnumerator AssertMeleeDamage(
            GameObject knightPrefab,
            bool attackerIsPlayer,
            float expectedDamage,
            Vector3 origin,
            string failureMessage)
        {
            const float attackDamage = 20f;
            const float targetHp = 500f;
            GameObject attackerObject = null;
            GameObject targetObject = null;

            try
            {
                Assert.That(knightPrefab, Is.Not.Null,
                    "The shipped Knight prefab is required to drive the production melee route.");
                attackerObject = Object.Instantiate(knightPrefab, origin, Quaternion.identity);
                attackerObject.name = attackerIsPlayer
                    ? "OpeningVolleyMelee_Player"
                    : "OpeningVolleyMelee_Enemy";
                var attacker = attackerObject.GetComponent<UnitController>();
                Assert.That(attacker, Is.Not.Null, "The shipped Knight prefab must contain UnitController.");
                attacker.InitializeUnit(attackerIsPlayer, UnitState.Grounded);
                attacker.unitType = UnitType.Knight;
                attacker.attackDamage = attackDamage;
                attacker.attackRange = 2f;
                attacker.attackCooldown = 0f;
                attacker.moveSpeed = 0f;
                FreezeUnitBody(attackerObject);

                targetObject = CreateDamageTarget(
                    "OpeningVolleyMelee_Target",
                    origin + Vector3.right * 0.75f,
                    !attackerIsPlayer,
                    targetHp);
                var target = targetObject.GetComponent<UnitController>();

                for (int frame = 0; frame < 30 && target.currentHP == targetHp; frame++)
                {
                    yield return null;
                }

                Assert.That(
                    targetHp - target.currentHP,
                    Is.EqualTo(expectedDamage).Within(0.0001f),
                    failureMessage);
            }
            finally
            {
                if (attackerObject != null) Object.Destroy(attackerObject);
                if (targetObject != null) Object.Destroy(targetObject);
            }

            yield return null;
        }

        private static GameObject CreateFieldKeg(
            GameManager gameManager,
            string name,
            Vector3 position)
        {
            var fieldKegObject = gameManager.SpawnFieldBarrel(position);
            Assert.That(fieldKegObject, Is.Not.Null,
                "SpawnFieldBarrel must create the production field-keg body.");
            fieldKegObject.name = name;

            var block = fieldKegObject.GetComponent<DestructibleBlock>();
            var explosive = fieldKegObject.GetComponent<ExplosiveGimmick>();
            Assert.That(block, Is.Not.Null,
                "A production field keg must carry the DestructibleBlock used by fatal-hit routes.");
            Assert.That(explosive, Is.Not.Null,
                "A production field keg must carry the ExplosiveGimmick used by direct carrier routes.");
            block.maxHP = 20f;
            block.currentHP = 20f;
            explosive.SetPermanentPotency(damage: 80f, radius: 2f);
            FreezeUnitBody(fieldKegObject);
            return fieldKegObject;
        }

        private static IEnumerator CompleteRealTurnHandoff(
            GameManager gameManager,
            UnitController resolvedUnit = null)
        {
            int turnBefore = gameManager.TurnCount;
            bool playerTurnBefore = gameManager.IsPlayerTurn;
            bool enabledBefore = gameManager.enabled;
            gameManager.enabled = true;

            try
            {
                gameManager.OnUnitLaunched(resolvedUnit);
                float deadline = Time.realtimeSinceStartup + 3f;
                while ((gameManager.TurnCount == turnBefore ||
                        gameManager.IsPlayerTurn == playerTurnBefore) &&
                       Time.realtimeSinceStartup < deadline)
                {
                    yield return null;
                }

                Assert.That(gameManager.TurnCount, Is.EqualTo(turnBefore + 1),
                    "The public OnUnitLaunched resolution path must complete exactly one real turn transition.");
                Assert.That(gameManager.IsPlayerTurn, Is.EqualTo(!playerTurnBefore),
                    "A real turn transition must hand control to the opposing side before delayed damage is released.");
                Assert.That(
                    gameManager.currentState,
                    Is.EqualTo(gameManager.IsPlayerTurn ? GameState.PlayerTurn : GameState.AITurn),
                    "Public turn state must agree with the completed handoff.");
            }
            finally
            {
                gameManager.StopAllCoroutines();
                gameManager.enabled = enabledBefore;
            }
        }

        private static IEnumerator WaitForExplosionOutcome(
            GameObject fieldKegObject,
            UnitController splashTarget,
            float targetHp,
            bool requireFieldKegDestroyed)
        {
            float deadline = Time.realtimeSinceStartup + 3f;
            while (splashTarget != null &&
                   splashTarget.currentHP == targetHp &&
                   Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(
                targetHp - splashTarget.currentHP,
                Is.EqualTo(40f).Within(0.0001f),
                "The production field-keg route must apply 80 base damage at the captured 0.5 " +
                "multiplier exactly once; dropped context yields 80 and double application yields 20.");
            if (requireFieldKegDestroyed)
            {
                Assert.That(fieldKegObject == null, Is.True,
                    "The fatal DestructibleBlock route must destroy the field keg so ExplosiveGimmick.OnDestroy detonates it.");
            }
        }

        private static Vector2Int ReadScoreboard(GameManager gameManager)
        {
            Assert.That(gameManager.scoreText, Is.Not.Null,
                "The shipped scoreboard is the player-visible ownership outcome for explosion attribution.");
            var match = Regex.Match(
                gameManager.scoreText.text ?? string.Empty,
                @"SIEGE SCORE\s+(\d+)\s*-\s*(\d+)");
            Assert.That(match.Success, Is.True,
                $"Expected a parseable shipped scoreboard, got '{gameManager.scoreText.text}'.");
            return new Vector2Int(
                int.Parse(match.Groups[1].Value),
                int.Parse(match.Groups[2].Value));
        }

        private static void AssertPlayerScoreDelta(
            GameManager gameManager,
            Vector2Int scoreBefore,
            int expectedPlayerDelta)
        {
            Vector2Int scoreAfter = ReadScoreboard(gameManager);
            Assert.That(scoreAfter.x - scoreBefore.x, Is.EqualTo(expectedPlayerDelta),
                "Every detonated field keg must visibly credit the player owner exactly once.");
            Assert.That(scoreAfter.y, Is.EqualTo(scoreBefore.y),
                "Player-owned field-keg damage must not be misattributed to the enemy scoreboard.");
        }

        private static GameObject CreateDamageTarget(
            string name,
            Vector3 position,
            bool isPlayer,
            float hitPoints)
        {
            var targetObject = new GameObject(name);
            targetObject.transform.position = position;
            targetObject.AddComponent<BoxCollider2D>();
            var target = targetObject.AddComponent<UnitController>();
            target.InitializeUnit(isPlayer, UnitState.Idle);
            target.maxHP = hitPoints;
            target.currentHP = hitPoints;
            FreezeUnitBody(targetObject);
            return targetObject;
        }

        private static void FreezeUnitBody(GameObject unitObject)
        {
            var body = unitObject.GetComponent<Rigidbody2D>();
            if (body == null) return;
            body.gravityScale = 0f;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
        }

        private static void PrepareCommittedOpeningShot(GameManager gameManager)
        {
            gameManager.enabled = false;
            gameManager.enforceOneShotTurns = true;
            ConfigureTurn(gameManager, turnCount: 0, isPlayerTurn: true);
            ResetCommittedShot(gameManager);
            Assert.That(gameManager.TryCommitTurnShot(), Is.True,
                "The opening fixture must commit its shot through GameManager's public turn gate.");
        }

        private static void ConfigureTurn(GameManager gameManager, int turnCount, bool isPlayerTurn)
        {
            gameManager.currentState = isPlayerTurn ? GameState.PlayerTurn : GameState.AITurn;
            SetPrivateField(gameManager, "isPlayerTurn", isPlayerTurn);
            SetPrivateField(gameManager, "turnCount", turnCount);
        }

        private static void ResetCommittedShot(GameManager gameManager)
        {
            GetPrivateField<OneShotTurnGate>(gameManager, "oneShotTurnGate").BeginTurn();
        }

        private static void DestroyPickupsAt(Vector3 position)
        {
            foreach (var pickup in Object.FindObjectsOfType<ItemPickup>())
            {
                if (pickup != null && (pickup.transform.position - position).sqrMagnitude < 0.0001f)
                {
                    Object.Destroy(pickup.gameObject);
                }
            }
        }

        private sealed class OpeningVolleyStateSnapshot
        {
            private readonly bool enabled;
            private readonly int turnCount;
            private readonly bool isPlayerTurn;
            private readonly bool enforcesOneShotTurns;
            private readonly bool shotCommitted;
            private readonly GameState state;

            public OpeningVolleyStateSnapshot(GameManager gameManager)
            {
                enabled = gameManager.enabled;
                turnCount = gameManager.TurnCount;
                isPlayerTurn = gameManager.IsPlayerTurn;
                enforcesOneShotTurns = gameManager.enforceOneShotTurns;
                shotCommitted = GetPrivateField<OneShotTurnGate>(
                    gameManager,
                    "oneShotTurnGate").ShotCommitted;
                state = gameManager.currentState;
            }

            public void Restore(GameManager gameManager)
            {
                if (gameManager == null) return;
                gameManager.enforceOneShotTurns = enforcesOneShotTurns;
                gameManager.currentState = state;
                SetPrivateField(gameManager, "isPlayerTurn", isPlayerTurn);
                SetPrivateField(gameManager, "turnCount", turnCount);
                var gate = GetPrivateField<OneShotTurnGate>(gameManager, "oneShotTurnGate");
                gate.BeginTurn();
                if (shotCommitted) gate.TryCommitShot();
                gameManager.enabled = enabled;
            }
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {target.GetType().Name}.{fieldName}.");
            return (T)field.GetValue(target);
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected private field {target.GetType().Name}.{fieldName}.");
            field.SetValue(target, value);
        }
    }
}
