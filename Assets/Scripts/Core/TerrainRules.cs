namespace ModularChess.Core
{
    internal static class TerrainRules
    {
        #region Public Methods
        public static bool CanLand(Board board, Square square)
        {
            return square.IsOnBoard && board.TerrainAt(square) != TerrainKind.Mountain;
        }
        public static bool BlocksMoveThrough(Board board, Square square)
        {
            TerrainKind kind = board.TerrainAt(square);
            return kind == TerrainKind.Mountain || kind == TerrainKind.Swamp;
        }
        public static bool BlocksVisionThrough(Board board, Square square)
        {
            TerrainKind kind = board.TerrainAt(square);
            return kind == TerrainKind.Mountain || kind == TerrainKind.Forest;
        }
        #endregion
    }
}
