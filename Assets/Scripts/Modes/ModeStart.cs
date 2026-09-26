namespace ModularChess.Core
{
    internal static class ModeStart
    {
        #region Public Methods
        public static Board Apply(Board board, MatchRules rules, ref CastlingRights castling)
        {
            if (board == null || rules == null)
                return board;
            MatchSettings settings = rules.Settings ?? MatchSettings.Default;
            if (rules.Has(ModeId.Randomizer))
                board = RandomizerRules.Apply(board, settings, ref castling);
            if (rules.Has(ModeId.ComplexTerrain))
                board = TerrainLayouts.Apply(board, settings);
            return board;
        }
        #endregion
    }
}
