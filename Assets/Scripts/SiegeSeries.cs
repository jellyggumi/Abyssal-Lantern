using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Best-of-3 match series (ranking overhaul): "전체 게임 경기를 총 3판 2선승해야 이기는 것" -
    /// the overall contest between the two sides is decided by whoever wins 2 of up to 3
    /// individual sieges first, not by a single game's swingy outcome. GameManager tallies
    /// seriesPlayerWins/seriesEnemyWins across EndGame() calls and only records a leaderboard
    /// entry / offers the next campaign stage once the series is actually decided; this class
    /// holds the pure win/continue/scoring rules so EditMode tests can pin the contract without
    /// touching MonoBehaviour state.
    /// </summary>
    public static class SiegeSeries
    {
        public const int WinsNeeded = 2;
        public const int MaxGames = 3;

        /// <summary>True once either side has clinched WinsNeeded, or MaxGames have been played.</summary>
        public static bool IsSeriesDecided(int playerWins, int enemyWins)
        {
            return playerWins >= WinsNeeded || enemyWins >= WinsNeeded || (playerWins + enemyWins) >= MaxGames;
        }

        /// <summary>Only meaningful once IsSeriesDecided is true.</summary>
        public static bool PlayerWonSeries(int playerWins, int enemyWins)
        {
            return playerWins > enemyWins;
        }

        /// <summary>1-based number of the game about to be played next, clamped to MaxGames.</summary>
        public static int NextGameNumber(int gamesPlayedSoFar)
        {
            return Mathf.Clamp(gamesPlayedSoFar + 1, 1, MaxGames);
        }

        /// <summary>
        /// Persistent ranking score for a decided series: the sum of every game's score in
        /// the series, plus a decisive-sweep bonus so a clean 2-0 series outranks a narrower
        /// 2-1 series that happened to total the same raw points.
        /// </summary>
        public static int SeriesScore(int totalScoreAcrossGames, int playerWins, int enemyWins)
        {
            bool sweep = PlayerWonSeries(playerWins, enemyWins) && enemyWins == 0;
            return totalScoreAcrossGames + (sweep ? 150 : 0);
        }
    }
}
