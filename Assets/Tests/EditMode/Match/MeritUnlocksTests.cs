using ModularChess.Core;
using ModularChess.Presentation;
using NUnit.Framework;

namespace ModularChess.Match.Tests
{
    public class MeritUnlocksTests
    {
        [SetUp]
        public void SetUp()
        {
            MeritWallet.Clear();
            MeritUnlocks.ClearAll();
            HistoryPrefs.Tier = 0;
        }

        [TearDown]
        public void TearDown()
        {
            MeritWallet.Clear();
            MeritUnlocks.ClearAll();
            HistoryPrefs.Tier = 0;
        }

        [Test]
        public void HistoryStartsLockedAtCapZero()
        {
            Assert.AreEqual(0, HistoryPrefs.Cap);
            Assert.IsFalse(HistoryPrefs.Unlocked);
        }

        [Test]
        public void BuyingHistoryTierRaisesCap()
        {
            MeritWallet.DebugFill(20);
            Assert.IsTrue(MeritUnlocks.TryPurchase(UnlockProduct.HistoryTier1));
            Assert.AreEqual(5, HistoryPrefs.Cap);
            Assert.IsTrue(MeritUnlocks.TryPurchase(UnlockProduct.HistoryTier2));
            Assert.AreEqual(10, HistoryPrefs.Cap);
        }

        [Test]
        public void ModePurchaseSpendsMerit()
        {
            MeritWallet.DebugFill(6);
            Assert.IsTrue(MeritUnlocks.TryPurchase(UnlockProduct.ModeFogOfWar));
            Assert.AreEqual(0, MeritWallet.Balance);
            Assert.IsTrue(MeritUnlocks.IsModeOwned(ModeId.FogOfWar));
        }

        [Test]
        public void VersusCheckmateGrantsThreeThenTwoThenOne()
        {
            MeritWallet.GrantVersusFinish(checkmate: true);
            Assert.AreEqual(3, MeritWallet.Balance);
            MeritWallet.GrantVersusFinish(checkmate: true);
            Assert.AreEqual(5, MeritWallet.Balance);
            MeritWallet.GrantVersusFinish(checkmate: true);
            Assert.AreEqual(6, MeritWallet.Balance);
        }

        [Test]
        public void VisibleShopItems_ShowsOneHistoryTierAtATime()
        {
            UnlockProduct[] start = MeritUnlocks.VisibleShopItems();
            Assert.Contains(UnlockProduct.HistoryTier1, start);
            Assert.IsFalse(System.Array.IndexOf(start, UnlockProduct.HistoryTier2) >= 0);
            MeritWallet.DebugFill(20);
            Assert.IsTrue(MeritUnlocks.TryPurchase(UnlockProduct.HistoryTier1));
            UnlockProduct[] after = MeritUnlocks.VisibleShopItems();
            Assert.Contains(UnlockProduct.HistoryTier1, after);
            Assert.Contains(UnlockProduct.HistoryTier2, after);
            Assert.IsFalse(System.Array.IndexOf(after, UnlockProduct.HistoryTier3) >= 0);
            Assert.Less(
                System.Array.IndexOf(after, UnlockProduct.HistoryTier2),
                System.Array.IndexOf(after, UnlockProduct.HistoryTier1));
        }
    }
}
