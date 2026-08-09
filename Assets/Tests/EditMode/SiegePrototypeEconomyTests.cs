using NUnit.Framework;
using CastleBusters;

namespace CastleBusters.Tests
{
    [TestFixture]
    public class SiegePrototypeEconomyTests
    {
        [SetUp]
        public void SetUp()
        {
            SiegePrototypeEconomy.ResetDemo();
        }

        [TearDown]
        public void TearDown()
        {
            SiegePrototypeEconomy.ResetDemo();
        }

        [Test]
        public void SeriesVictoryAndBattleBannerSeal_FollowThePublishedOneTimeExchangeContract()
        {
            Assert.Zero(SiegePrototypeEconomy.Balance, "ResetDemo must clear the local balance.");
            Assert.IsFalse(SiegePrototypeEconomy.HasBattleBannerSeal, "ResetDemo must lock the Battle Banner Seal.");
            Assert.AreEqual(12, SiegePrototypeEconomy.SeriesVictoryMarks, "The published series-victory reward changed unexpectedly.");
            Assert.AreEqual(12, SiegePrototypeEconomy.BattleBannerSealPrice, "The published Battle Banner Seal price changed unexpectedly.");

            var awardedBalance = SiegePrototypeEconomy.AwardSeriesVictory();

            Assert.AreEqual(SiegePrototypeEconomy.SeriesVictoryMarks, awardedBalance, "One series victory must return its published reward.");
            Assert.AreEqual(SiegePrototypeEconomy.SeriesVictoryMarks, SiegePrototypeEconomy.Balance, "One series victory must credit exactly its published reward.");

            var balanceBeforeUnlock = SiegePrototypeEconomy.Balance;
            Assert.IsTrue(SiegePrototypeEconomy.TryUnlockBattleBannerSeal(), "A balance equal to the published price must unlock the Battle Banner Seal.");
            Assert.IsTrue(SiegePrototypeEconomy.HasBattleBannerSeal, "A successful exchange must unlock the Battle Banner Seal.");
            Assert.AreEqual(balanceBeforeUnlock - SiegePrototypeEconomy.BattleBannerSealPrice, SiegePrototypeEconomy.Balance, "A successful exchange must spend exactly the published price.");

            var balanceAfterUnlock = SiegePrototypeEconomy.Balance;
            Assert.IsFalse(SiegePrototypeEconomy.TryUnlockBattleBannerSeal(), "The Battle Banner Seal must not be exchangeable twice.");
            Assert.AreEqual(balanceAfterUnlock, SiegePrototypeEconomy.Balance, "A rejected duplicate exchange must not spend any balance.");
            Assert.IsTrue(SiegePrototypeEconomy.HasBattleBannerSeal, "A rejected duplicate exchange must not relock the purchased Battle Banner Seal.");

            SiegePrototypeEconomy.ResetDemo();

            Assert.Zero(SiegePrototypeEconomy.Balance, "ResetDemo must restore zero balance after a purchase.");
            Assert.IsFalse(SiegePrototypeEconomy.HasBattleBannerSeal, "ResetDemo must restore the Battle Banner Seal to locked.");
        }
    }
}
