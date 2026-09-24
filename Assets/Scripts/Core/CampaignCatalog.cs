namespace ModularChess.Core
{
    public static class CampaignCatalog
    {
        #region Fields
        public const int DemoLevelCount = 10;
        static readonly CampaignLevelDefinition[] Levels =
        {
            new CampaignLevelDefinition(
                0, "campaign.level.1",
                "6k1/5ppp/8/8/8/8/5PPP/4Q1K1 w - - 0 1",
                null, turnLimit: 1, lossLimit: 0),
            new CampaignLevelDefinition(
                1, "campaign.level.2",
                "6k1/8/6K1/8/8/8/8/7R w - - 0 1",
                null, turnLimit: 1, lossLimit: 0),
            new CampaignLevelDefinition(
                2, "campaign.level.3",
                "r1bqkb1r/pppp1ppp/2n2n2/4p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 4 4",
                null, turnLimit: 1, lossLimit: 0),
            new CampaignLevelDefinition(
                3, "campaign.level.4",
                "8/8/8/8/8/4k3/4q3/4K3 b - - 0 1",
                null, turnLimit: 5, lossLimit: 0, playerSide: Side.Black, aiStrength: AiStrength.Easy),
            new CampaignLevelDefinition(
                4, "campaign.level.5",
                "7k/8/8/8/8/8/8/R3K2R w KQ - 0 1",
                null, turnLimit: 8, lossLimit: 0),
            new CampaignLevelDefinition(
                5, "campaign.level.6",
                "rnbqkbnr/pppp1ppp/8/4p3/4P3/5N2/PPPP1PPP/RNBQKB1R b KQkq - 1 2",
                null, turnLimit: 20, lossLimit: 2, playerSide: Side.Black, aiStrength: AiStrength.Easy),
            new CampaignLevelDefinition(
                6, "campaign.level.7",
                "4k3/8/8/8/8/8/4Q3/4K3 w - - 0 1",
                new[] { ModeId.FogOfWar }, turnLimit: 3, lossLimit: 0),
            new CampaignLevelDefinition(
                7, "campaign.level.8",
                "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                new[] { ModeId.FogOfWar }, turnLimit: 40, lossLimit: 4, aiStrength: AiStrength.Easy),
            new CampaignLevelDefinition(
                8, "campaign.level.9",
                "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                new[] { ModeId.PowerfulPieces }, turnLimit: 40, lossLimit: 4, aiStrength: AiStrength.Easy, empowerBudget: 3),
            new CampaignLevelDefinition(
                9, "campaign.level.10",
                "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1",
                new[] { ModeId.Martyr }, turnLimit: 40, lossLimit: 6, aiStrength: AiStrength.Easy)
        };
        #endregion

        #region Public Methods
        public static int Count => Levels.Length;
        public static CampaignLevelDefinition Get(int index)
        {
            if (index < 0 || index >= Levels.Length) return null;
            return Levels[index];
        }
        public static CampaignLevelDefinition[] All => Levels;
        #endregion
    }
}
