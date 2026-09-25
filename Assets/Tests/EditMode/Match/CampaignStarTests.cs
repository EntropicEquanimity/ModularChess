using ModularChess.Core;
using ModularChess.Presentation;
using NUnit.Framework;

namespace ModularChess.Match.Tests
{
    public class CampaignStarTests
    {
        [SetUp]
        public void SetUp()
        {
            MeritWallet.Clear();
            CampaignProgress.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            MeritWallet.Clear();
            CampaignProgress.Clear();
        }

        [Test]
        public void Eval_WinWithinLimits_EarnsAllStars()
        {
            CampaignLevelDefinition level = CampaignCatalog.Get(0);
            CampaignStarFlags flags = CampaignStarEval.Evaluate(level, true, 1, 0);
            Assert.AreEqual(CampaignStarFlags.Complete | CampaignStarFlags.TurnLimit | CampaignStarFlags.LossLimit, flags);
            Assert.AreEqual(3, CampaignStarEval.Count(flags));
        }

        [Test]
        public void Eval_WinOverTurnLimit_SkipsTurnStar()
        {
            CampaignLevelDefinition level = CampaignCatalog.Get(0);
            CampaignStarFlags flags = CampaignStarEval.Evaluate(level, true, 5, 0);
            Assert.AreEqual(CampaignStarFlags.Complete | CampaignStarFlags.LossLimit, flags);
        }

        [Test]
        public void Eval_Loss_EarnsNothing()
        {
            CampaignLevelDefinition level = CampaignCatalog.Get(0);
            Assert.AreEqual(CampaignStarFlags.None, CampaignStarEval.Evaluate(level, false, 1, 0));
        }

        [Test]
        public void Progress_AwardsMeritOncePerStar()
        {
            Assert.AreEqual(3, CampaignProgress.Award(0, CampaignStarFlags.Complete | CampaignStarFlags.TurnLimit | CampaignStarFlags.LossLimit));
            Assert.AreEqual(3, MeritWallet.Balance);
            Assert.AreEqual(0, CampaignProgress.Award(0, CampaignStarFlags.Complete | CampaignStarFlags.TurnLimit | CampaignStarFlags.LossLimit));
            Assert.AreEqual(3, MeritWallet.Balance);
            Assert.IsTrue(CampaignProgress.IsUnlocked(1));
            Assert.IsFalse(CampaignProgress.IsUnlocked(2));
        }

        [Test]
        public void Catalog_HasFiftyLevelsWithDifficultyBands()
        {
            Assert.AreEqual(CampaignCatalog.TotalLevelCount, CampaignCatalog.Count);
            Assert.AreEqual(CampaignCatalog.DemoLevelCount, 10);
            Assert.IsNotNull(CampaignCatalog.Get(0).Fen);
            Assert.AreEqual(AiStrength.Easy, CampaignCatalog.Get(4).AiStrength);
            Assert.AreEqual(AiStrength.Medium, CampaignCatalog.Get(10).AiStrength);
            Assert.AreEqual(AiStrength.Hard, CampaignCatalog.Get(35).AiStrength);
            Assert.IsTrue(CampaignCatalog.Get(5).Modes.Length > 0);
        }
    }
}
