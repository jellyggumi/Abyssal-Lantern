using UnityEngine;

namespace CastleBusters
{
    /// <summary>
    /// Local, deterministic reward ledger for the demo's post-series War Chest.
    /// These are non-transferable prototype marks: no price, IAP catalog, receipt,
    /// advertisement, random reward, or gameplay-stat effect is represented here.
    /// </summary>
    public static class SiegePrototypeEconomy
    {
        public const int SeriesVictoryMarks = 12;
        public const int BattleBannerSealPrice = 12;

        private const string BalanceKey = "CastleBusters.PrototypeWarChest.Balance";
        private const string BattleBannerSealKey = "CastleBusters.PrototypeWarChest.BattleBannerSeal";

        public static int Balance => Mathf.Max(0, PlayerPrefs.GetInt(BalanceKey, 0));
        public static bool HasBattleBannerSeal => PlayerPrefs.GetInt(BattleBannerSealKey, 0) != 0;

        /// <summary>Credits exactly one fixed mark reward for a series victory.</summary>
        public static int AwardSeriesVictory()
        {
            PlayerPrefs.SetInt(BalanceKey, Balance + SeriesVictoryMarks);
            PlayerPrefs.Save();
            return SeriesVictoryMarks;
        }

        /// <summary>
        /// Exchanges the one-time, gameplay-neutral battle-banner seal. It cannot be bought
        /// twice, and insufficient marks leave both stored values untouched.
        /// </summary>
        public static bool TryUnlockBattleBannerSeal()
        {
            if (HasBattleBannerSeal || Balance < BattleBannerSealPrice) return false;

            PlayerPrefs.SetInt(BalanceKey, Balance - BattleBannerSealPrice);
            PlayerPrefs.SetInt(BattleBannerSealKey, 1);
            PlayerPrefs.Save();
            return true;
        }

        /// <summary>Clears the local demo ledger; called when the player returns to Title.</summary>
        public static void ResetDemo()
        {
            PlayerPrefs.DeleteKey(BalanceKey);
            PlayerPrefs.DeleteKey(BattleBannerSealKey);
            PlayerPrefs.Save();
        }
    }
}
