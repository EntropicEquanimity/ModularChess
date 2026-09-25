namespace ModularChess.Core
{
    public static class CampaignCatalog
    {
        #region Fields
        public const int DemoLevelCount = 10;
        public const int TotalLevelCount = 50;
        const string Start = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
        static readonly ModeId[] Fog = { ModeId.FogOfWar };
        static readonly ModeId[] Power = { ModeId.PowerfulPieces };
        static readonly ModeId[] Martyr = { ModeId.Martyr };
        static readonly ModeId[] FogPower = { ModeId.FogOfWar, ModeId.PowerfulPieces };
        static readonly ModeId[] FogMartyr = { ModeId.FogOfWar, ModeId.Martyr };
        static readonly ModeId[] PowerMartyr = { ModeId.PowerfulPieces, ModeId.Martyr };
        static readonly ModeId[] AllModes = { ModeId.FogOfWar, ModeId.PowerfulPieces, ModeId.Martyr };
        static readonly CampaignLevelDefinition[] Levels = BuildLevels();
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

        #region Private Methods
        static CampaignLevelDefinition[] BuildLevels()
        {
            var levels = new CampaignLevelDefinition[TotalLevelCount];
            levels[0] = L(0, "6k1/5ppp/8/8/8/8/5PPP/4Q1K1 w - - 0 1", null, 1, 0);
            levels[1] = L(1, "6k1/8/6K1/8/8/8/8/7R w - - 0 1", null, 1, 0);
            levels[2] = L(2, "r1bqkb1r/pppp1ppp/2n2n2/4p2Q/2B1P3/8/PPPP1PPP/RNB1K1NR w KQkq - 4 4", null, 1, 0);
            levels[3] = L(3, "8/8/8/8/8/4k3/4q3/4K3 b - - 0 1", null, 5, 0, Side.Black, AiStrength.Easy);
            levels[4] = L(4, "7k/8/8/8/8/8/8/R3K2R w KQ - 0 1", null, 8, 0);
            levels[5] = L(5, "4k3/8/8/8/8/8/4Q3/4K3 w - - 0 1", Fog, 3, 0);
            levels[6] = L(6, Start, Fog, 40, 4, Side.White, AiStrength.Easy);
            levels[7] = L(7, Start, Power, 40, 4, Side.White, AiStrength.Easy, 3);
            levels[8] = L(8, Start, Martyr, 40, 6, Side.White, AiStrength.Easy);
            levels[9] = L(9, Start, FogPower, 40, 5, Side.White, AiStrength.Easy, 3);
            for (int i = 10; i < 35; i++)
                levels[i] = MediumLevel(i);
            for (int i = 35; i < TotalLevelCount; i++)
                levels[i] = HardLevel(i);
            return levels;
        }
        static CampaignLevelDefinition MediumLevel(int index)
        {
            int lane = (index - 10) % 6;
            switch (lane)
            {
                case 0: return L(index, Start, null, 50, 6, Side.White, AiStrength.Medium);
                case 1: return L(index, Start, Fog, 50, 6, Side.White, AiStrength.Medium);
                case 2: return L(index, Start, Power, 50, 6, Side.White, AiStrength.Medium, 4);
                case 3: return L(index, Start, Martyr, 55, 8, Side.White, AiStrength.Medium);
                case 4: return L(index, Start, FogMartyr, 55, 7, Side.White, AiStrength.Medium);
                default: return L(index, Start, PowerMartyr, 55, 7, Side.White, AiStrength.Medium, 4);
            }
        }
        static CampaignLevelDefinition HardLevel(int index)
        {
            int lane = (index - 35) % 5;
            switch (lane)
            {
                case 0: return L(index, Start, Fog, 60, 8, Side.White, AiStrength.Hard);
                case 1: return L(index, Start, Power, 60, 8, Side.White, AiStrength.Hard, 5);
                case 2: return L(index, Start, Martyr, 65, 10, Side.White, AiStrength.Hard);
                case 3: return L(index, Start, FogPower, 65, 9, Side.White, AiStrength.Hard, 5);
                default: return L(index, Start, AllModes, 70, 10, Side.White, AiStrength.Hard, 5);
            }
        }
        static CampaignLevelDefinition L(
            int index,
            string fen,
            ModeId[] modes,
            int turnLimit,
            int lossLimit,
            Side playerSide = Side.White,
            AiStrength aiStrength = AiStrength.Easy,
            int empowerBudget = 4)
        {
            return new CampaignLevelDefinition(
                index,
                "campaign.level." + (index + 1),
                fen,
                modes,
                turnLimit,
                lossLimit,
                playerSide,
                aiStrength,
                empowerBudget);
        }
        #endregion
    }
}
