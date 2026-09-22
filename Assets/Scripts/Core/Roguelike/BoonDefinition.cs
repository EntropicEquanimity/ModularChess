namespace ModularChess.Core
{
    public sealed class BoonDefinition
    {
        #region Fields
        public BoonId Id { get; }
        public BoonRarity Rarity { get; }
        public string NameKey { get; }
        public string DescriptionKey { get; }
        public int PawnCount { get; }
        public int MaxStacks { get; }
        #endregion

        #region Public Methods
        public BoonDefinition(
            BoonId id,
            BoonRarity rarity,
            string nameKey,
            string descriptionKey,
            int pawnCount,
            int maxStacks)
        {
            Id = id;
            Rarity = rarity;
            NameKey = nameKey;
            DescriptionKey = descriptionKey;
            PawnCount = pawnCount;
            MaxStacks = maxStacks;
        }
        #endregion
    }
}
