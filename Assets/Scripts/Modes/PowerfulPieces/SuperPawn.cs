namespace ModularChess.Core
{
    internal static class SuperPawn
    {
        public static bool CanCaptureFrom(Square pawnSquare, Side pawnSide, Square from)
        {
            int forward = pawnSide == Side.White ? 1 : -1;
            int fileDelta = from.File - pawnSquare.File;
            int rankDelta = from.Rank - pawnSquare.Rank;
            return rankDelta != forward || fileDelta < -1 || fileDelta > 1;
        }
    }
}
