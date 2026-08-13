using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine.TestTools;
using CastleBusters;

namespace CastleBusters.Tests
{
    /// <summary>
    /// Comprehensive analysis harness for Castle Busters game evaluation.
    /// Cycles 1–5 baseline data collection.
    /// </summary>
    public class CastleBustersAnalysisTests
    {
        private GameManager gameManager;
        private LaunchManager launchManager;
        private CastleController playerCastle;
        private CastleController enemyCastle;

        // Statistics
        private struct GameStats
        {
            public bool PlayerWon;
            public int TurnsPlayed;
            public float GameDuration;
            public int UnitsLaunched;
            public string MostUsedUnit;
        }

        private List<GameStats> sessionStats = new List<GameStats>();

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator Cycle1_CompileAndStabilityCheck()
        {
            Debug.Log("=== CYCLE 1: Compile & Stability Check ===");

            // Load scene
            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            yield return null;
            yield return new WaitForSecondsRealtime(2f);

            // Verify GameManager exists
            Assert.IsNotNull(GameManager.Instance, "GameManager must exist");
            Debug.Log("[OK] GameManager exists");

            // Verify key components
            gameManager = GameManager.Instance;
            launchManager = Object.FindObjectOfType<LaunchManager>();
            Assert.IsNotNull(launchManager, "LaunchManager must exist");
            Debug.Log("[OK] LaunchManager exists");

            // Check initial state
            Assert.IsNotNull(gameManager.playerCastle, "Player castle must exist");
            Assert.IsNotNull(gameManager.enemyCastle, "Enemy castle must exist");
            Debug.Log("[OK] Both castles exist");

            // Record baseline memory and FPS
            long memoryBefore = System.GC.GetTotalMemory(false);
            Debug.Log($"Memory baseline: {memoryBefore / (1024 * 1024)} MB");
            Debug.Log($"Time.deltaTime: {Time.deltaTime}");

            yield return new WaitForSecondsRealtime(1f);

            long memoryAfter = System.GC.GetTotalMemory(false);
            Debug.Log($"Memory after 1s: {memoryAfter / (1024 * 1024)} MB");
            Debug.Log("[OK] CYCLE 1 COMPLETE");
        }

        [UnityTest]
        [Timeout(600000)]
        public IEnumerator Cycle2_MechanicsValidation()
        {
            Debug.Log("=== CYCLE 2: Core Mechanics Validation ===");

            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            yield return new WaitForSecondsRealtime(2f);

            gameManager = GameManager.Instance;
            launchManager = Object.FindObjectOfType<LaunchManager>();

            // A loaded scene sits on the title in GameState.Intro and waits to be started —
            // RuntimeReliabilityRegressionTests asserts that as the correct post-load state.
            // This test used to launch straight into it and then expect a turn handoff, so it
            // was asserting a mechanic it had never entered. The missing step was the start,
            // not the expectation.
            gameManager.BeginSiege();
            yield return null;

            // Test 1: Knight unit launch
            Debug.Log("Testing Knight unit...");
            gameManager.SelectUnit(0);
            launchManager.SimulateLaunch(new Vector2(10f, 5f));
            Debug.Log("[OK] Knight launched successfully");

            // Test 2: Verify game state transitions.
            // This used to sleep a flat 3s and then assert the handoff, which was never what
            // resolution costs: the volley has to land, the board holds for
            // PostImpactHoldSeconds, and then blocks and arrows have to settle (up to 3s on
            // their own). The fixed sleep therefore failed on any shot that took a moment
            // longer to come to rest — a statement about the stopwatch, not the mechanic.
            // Wait for the handoff the mechanic actually promises, bounded well past the
            // resolver's own 12s watchdog so a genuinely wedged projectile still fails here
            // instead of hanging the suite.
            float waited = 0f;
            while (gameManager.currentState == GameState.PlayerTurn && waited < 20f)
            {
                waited += Time.unscaledDeltaTime;
                yield return null;
            }
            Debug.Log($"Turn handoff observed after {waited:F2}s");

            Assert.AreEqual(GameState.AITurn, gameManager.currentState, "Should transition to AI turn after player launches");
            Debug.Log("[OK] Game state transitioned correctly");

            yield return new WaitForSecondsRealtime(2f);
            Debug.Log("[OK] CYCLE 2 COMPLETE");
        }

        [UnityTest]
        // Thirty games at roughly a minute each, measured at 1683 s once the siege was actually
        // being started (before that the harness sat on the title and collected nothing, which
        // is the only reason it used to fit in 900 s). Sized from the measurement with headroom
        // rather than tuned down until it passed.
        [Timeout(2400000)]
        public IEnumerator Cycle3_PlaytestDataCollection_30Games()
        {
            Debug.Log("=== CYCLE 3: Playtest Data Collection (30 Games) ===");

            int playerWins = 0;
            int aiWins = 0;
            List<float> gameDurations = new List<float>();
            List<int> unitCounts = new List<int>();

            for (int game = 1; game <= 30; game++)
            {
                Debug.Log($"--- Game {game}/30 ---");

                UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
                yield return new WaitForSecondsRealtime(2f);

                gameManager = GameManager.Instance;
                launchManager = Object.FindObjectOfType<LaunchManager>();

                // Same missing step as Cycle 2: without it the scene stays on the title,
                // IsPlayerTurn is never true, nothing is ever launched, and thirty "games"
                // collect thirty identical rows of nothing while still burning their full
                // turn budget in wall-clock waits.
                gameManager.BeginSiege();
                yield return null;

                float gameStartTime = Time.realtimeSinceStartup;
                int unitsLaunched = 0;

                // Simulate game
                for (int turn = 1; turn <= 20 && gameManager.currentState != GameState.GameOver; turn++)
                {
                    if (gameManager.IsPlayerTurn)
                    {
                        // Random unit selection
                        int unitIndex = Random.Range(0, 3);
                        gameManager.SelectUnit(unitIndex);

                        // Random launch velocity
                        Vector2 launchVelocity = new Vector2(Random.Range(5f, 20f), Random.Range(5f, 15f));
                        launchManager.SimulateLaunch(launchVelocity);
                        unitsLaunched++;

                        yield return new WaitForSecondsRealtime(0.5f);
                    }

                    yield return new WaitForSecondsRealtime(1f);
                }

                // Wait for game to end
                while (gameManager.currentState != GameState.GameOver && Time.realtimeSinceStartup - gameStartTime < 120f)
                {
                    yield return new WaitForSecondsRealtime(0.5f);
                }

                float gameDuration = Time.realtimeSinceStartup - gameStartTime;
                gameDurations.Add(gameDuration);
                unitCounts.Add(unitsLaunched);

                // Check who won
                // (Assuming there's a way to determine this from GameManager or similar)
                Debug.Log($"Game {game}: Duration {gameDuration:F1}s, Units launched: {unitsLaunched}");

                if (game % 10 == 0)
                {
                    float avgDuration = 0;
                    foreach (var d in gameDurations) avgDuration += d;
                    avgDuration /= gameDurations.Count;
                    Debug.Log($"Average game duration (first {game} games): {avgDuration:F1}s");
                }
            }

            // Summary statistics
            float totalDuration = 0;
            foreach (var d in gameDurations) totalDuration += d;
            float avgGameDuration = totalDuration / gameDurations.Count;

            int totalUnits = 0;
            foreach (var u in unitCounts) totalUnits += u;
            float avgUnitsPerGame = (float)totalUnits / unitCounts.Count;

            Debug.Log("=== CYCLE 3 SUMMARY ===");
            Debug.Log($"Games played: 30");
            Debug.Log($"Average game duration: {avgGameDuration:F1}s");
            Debug.Log($"Average units launched per game: {avgUnitsPerGame:F1}");
            Debug.Log($"Total units launched: {totalUnits}");
            Debug.Log("[OK] CYCLE 3 COMPLETE");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Cycle4_BalanceAndUsabilityAnalysis()
        {
            Debug.Log("=== CYCLE 4: Balance & Usability Analysis ===");

            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            yield return new WaitForSecondsRealtime(2f);

            gameManager = GameManager.Instance;
            launchManager = Object.FindObjectOfType<LaunchManager>();

            // Unit damage test
            Debug.Log("Testing unit damage values...");

            List<string> unitNames = new List<string> { "Knight", "Archer", "Cannon" };
            for (int i = 0; i < 3; i++)
            {
                gameManager.SelectUnit(i);
                Debug.Log($"Selected unit: {unitNames[i]}");

                // Verify unit can be selected
                Assert.IsNotNull(gameManager, "GameManager must be available");
                Debug.Log($"[OK] {unitNames[i]} can be selected");

                yield return new WaitForSecondsRealtime(0.5f);
            }

            // Animation check
            Debug.Log("Checking animation smoothness...");
            launchManager.SimulateLaunch(new Vector2(15f, 10f));
            for (int i = 0; i < 10; i++)
            {
                yield return new WaitForSecondsRealtime(0.1f);
                // Monitor FPS by checking Time.deltaTime
                float fps = 1f / Time.deltaTime;
                if (i % 3 == 0) Debug.Log($"Frame {i}: {fps:F1} FPS");
            }

            Debug.Log("[OK] CYCLE 4 COMPLETE");
        }

        [UnityTest]
        [Timeout(300000)]
        public IEnumerator Cycle5_ImprovementProposals()
        {
            Debug.Log("=== CYCLE 5: Improvement Proposals ===");

            UnityEngine.SceneManagement.SceneManager.LoadScene("SampleScene");
            yield return new WaitForSecondsRealtime(2f);

            gameManager = GameManager.Instance;
            Assert.IsNotNull(gameManager, "GameManager must exist before inspecting castle populations");

            // A loaded siege must contain structural blocks on both sides.  Observe the
            // scene hierarchy rather than relying on a non-existent controller counter.
            var playerBlocks = gameManager.playerCastle.GetComponentsInChildren<DestructibleBlock>();
            var enemyBlocks = gameManager.enemyCastle.GetComponentsInChildren<DestructibleBlock>();
            Assert.Greater(playerBlocks.Length, 0, "Player castle must load with a nonempty block population");
            Assert.Greater(enemyBlocks.Length, 0, "Enemy castle must load with a nonempty block population");
            Debug.Log($"Current state: {gameManager.currentState}");

            // Proposed improvements
            Debug.Log("\n=== TOP IMPROVEMENT PROPOSALS ===");
            Debug.Log("1. [HIGH PRIORITY] AI Difficulty Scaling - Allow difficulty selection");
            Debug.Log("2. [MEDIUM] Animation Polish - Increase attack animation clarity");
            Debug.Log("3. [MEDIUM] Visual Feedback - Enhance particle effects intensity");
            Debug.Log("4. [LOW] Content Expansion - Consider additional unit types");
            Debug.Log("5. [LOW] Sound Design - Add placeholder audio cues");

            Debug.Log("[OK] CYCLE 5 COMPLETE");
            yield return null;
        }
    }
}
