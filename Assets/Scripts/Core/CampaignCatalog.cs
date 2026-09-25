using System;

namespace ModularChess.Core
{
    public static class CampaignCatalog
    {
        #region Fields
        public const int DemoLevelCount = 10;
        static CampaignLevelDefinition[] Levels = Array.Empty<CampaignLevelDefinition>();
        #endregion

        #region Public Methods
        public static int Count => Levels.Length;
        public static CampaignLevelDefinition[] All => Levels;
        public static CampaignLevelDefinition Get(int index)
        {
            if (index < 0 || index >= Levels.Length) return null;
            return Levels[index];
        }
        public static void Bind(CampaignLevelDefinition[] levels)
        {
            Levels = levels ?? Array.Empty<CampaignLevelDefinition>();
        }
        #endregion
    }
}
