namespace ModularChess.Core
{
    internal static class Directions
    {
        #region Fields
        public static readonly int[] KnightFiles = { 1, 2, 2, 1, -1, -2, -2, -1 };
        public static readonly int[] KnightRanks = { 2, 1, -1, -2, -2, -1, 1, 2 };
        public static readonly int[] KingFiles = { 1, 1, 0, -1, -1, -1, 0, 1 };
        public static readonly int[] KingRanks = { 0, 1, 1, 1, 0, -1, -1, -1 };
        public static readonly int[] BishopFiles = { 1, 1, -1, -1 };
        public static readonly int[] BishopRanks = { 1, -1, 1, -1 };
        public static readonly int[] RookFiles = { 1, -1, 0, 0 };
        public static readonly int[] RookRanks = { 0, 0, 1, -1 };
        #endregion
    }
}
