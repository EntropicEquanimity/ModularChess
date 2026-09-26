using System;
using ModularChess.Core;
using ModularChess.Presentation;
using NUnit.Framework;
using UnityEditor;

namespace ModularChess.Match.Tests
{
    public class CampaignStarTests
    {
        static CampaignLevelDefinition SampleLevel()
        {
            return new CampaignLevelDefinition(
                0,
                "campaign.level.1",
                "8/8/8/8/8/8/8/8 w - - 0 1",
                Array.Empty<ModeId>(),
                CampaignTimeObjectiveKind.Turns,
                1,
                TimeControl.None,
                CampaignSpecialObjectiveKind.LoseNoPieces,
                0,
                PieceType.Pawn);
        }

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
            CampaignLevelDefinition level = SampleLevel();
            CampaignObjectiveLive live = CampaignStarEval.BuildLive(level, 1, 0, false, false, false);
            CampaignStarFlags flags = CampaignStarEval.Evaluate(level, true, live);
            Assert.AreEqual(CampaignStarFlags.Complete | CampaignStarFlags.Time | CampaignStarFlags.Special, flags);
            Assert.AreEqual(3, CampaignStarEval.Count(flags));
        }

        [Test]
        public void Eval_WinOverTurnLimit_SkipsTimeStar()
        {
            CampaignLevelDefinition level = SampleLevel();
            CampaignObjectiveLive live = CampaignStarEval.BuildLive(level, 5, 0, false, false, false);
            CampaignStarFlags flags = CampaignStarEval.Evaluate(level, true, live);
            Assert.AreEqual(CampaignStarFlags.Complete | CampaignStarFlags.Special, flags);
        }

        [Test]
        public void Eval_Loss_EarnsNothing()
        {
            CampaignLevelDefinition level = SampleLevel();
            CampaignObjectiveLive live = CampaignStarEval.BuildLive(level, 1, 0, false, false, false);
            Assert.AreEqual(CampaignStarFlags.None, CampaignStarEval.Evaluate(level, false, live));
        }

        [Test]
        public void Progress_AwardsMeritOncePerStar()
        {
            Assert.AreEqual(3, CampaignProgress.Award(0, CampaignStarFlags.Complete | CampaignStarFlags.Time | CampaignStarFlags.Special));
            Assert.AreEqual(3, MeritWallet.Balance);
            Assert.AreEqual(0, CampaignProgress.Award(0, CampaignStarFlags.Complete | CampaignStarFlags.Time | CampaignStarFlags.Special));
            Assert.AreEqual(3, MeritWallet.Balance);
            Assert.IsTrue(CampaignProgress.IsUnlocked(1));
            Assert.IsFalse(CampaignProgress.IsUnlocked(2));
        }

        [Test]
        public void Asset_HasFiftyLevelsWithDifficultyBands()
        {
            CampaignLevelSet set = AssetDatabase.LoadAssetAtPath<CampaignLevelSet>(CampaignLevelSet.AssetPath);
            Assert.IsNotNull(set);
            CampaignCatalog.Bind(set.ToDefinitions());
            Assert.AreEqual(50, CampaignCatalog.Count);
            Assert.AreEqual(CampaignCatalog.DemoLevelCount, 10);
            Assert.IsNotNull(CampaignCatalog.Get(0).Fen);
            Assert.AreEqual(AiStrength.Easy, CampaignCatalog.Get(4).AiStrength);
            Assert.AreEqual(AiStrength.Medium, CampaignCatalog.Get(10).AiStrength);
            Assert.AreEqual(AiStrength.Hard, CampaignCatalog.Get(35).AiStrength);
            Assert.IsTrue(CampaignCatalog.Get(5).Modes.Length > 0);
            MatchSettings authored = CampaignCatalog.Get(24).ToMatchSettings();
            Assert.AreEqual(2, authored.ActionPoints);
            Assert.IsTrue(HasMode(CampaignCatalog.Get(24), ModeId.ActionEconomy));
            Assert.AreEqual(TerrainLayoutKind.Woods, CampaignCatalog.Get(25).TerrainLayout);
            Assert.IsTrue(HasMode(CampaignCatalog.Get(25), ModeId.ComplexTerrain));
        }

        static bool HasMode(CampaignLevelDefinition level, ModeId id)
        {
            if (level?.Modes == null)
                return false;
            for (int i = 0; i < level.Modes.Length; i++)
            {
                if (level.Modes[i] == id)
                    return true;
            }
            return false;
        }

        [Test]
        public void CampaignLevel_ToMatchSettings_UsesAuthoredModeFields()
        {
            CampaignLevelDefinition level = new CampaignLevelDefinition(
                4,
                "campaign.level.5",
                "8/8/8/8/8/8/8/8 w - - 0 1",
                new[] { ModeId.ActionEconomy, ModeId.ComplexTerrain },
                CampaignTimeObjectiveKind.Turns,
                8,
                TimeControl.None,
                CampaignSpecialObjectiveKind.LoseNoPieces,
                0,
                PieceType.Pawn,
                actionPoints: 5,
                terrainLayout: TerrainLayoutKind.River,
                matchSeed: 12,
                terrainOnPieces: false);
            MatchSettings settings = level.ToMatchSettings();
            Assert.AreEqual(5, settings.ActionPoints);
            Assert.AreEqual(TerrainLayoutKind.River, settings.TerrainLayout);
            Assert.AreEqual(12, settings.MatchSeed);
            Assert.IsFalse(settings.TerrainOnPieces);
            Assert.AreEqual(HostColor.White, settings.HostColor);
            Assert.AreEqual(AiStrength.Easy, settings.AiStrength);
        }

        [Test]
        public void BuildLive_StrikesTimeWhenOverTurnLimit()
        {
            CampaignLevelDefinition level = SampleLevel();
            CampaignObjectiveLive live = CampaignStarEval.BuildLive(level, 2, 0, false, false, false);
            Assert.IsTrue(live.TimeFailed);
            Assert.IsFalse(live.SpecialFailed);
        }

        [Test]
        public void BuildLive_StrikesSpecialWhenPieceLost()
        {
            CampaignLevelDefinition level = SampleLevel();
            CampaignObjectiveLive live = CampaignStarEval.BuildLive(level, 1, 1, false, false, false);
            Assert.IsTrue(live.SpecialFailed);
        }
    }
}
