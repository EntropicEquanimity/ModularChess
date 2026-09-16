namespace ModularChess.Core
{
    internal static class SuperPawn
    {
        public static bool CanCaptureFrom(Square pawnSquare, Side pawnSide, Square from)
        {
            int forward = pawnSide == Side.White ? 1 : -1;
            int fileDelta = from.File - pawnSquare.File;
            int rankDelta = from.Rank - pawnSquare.Rank;
            int along = rankDelta * forward;
            if (along <= 0)
                return true;
            int absFile = fileDelta < 0 ? -fileDelta : fileDelta;
            if (absFile <= 1 || absFile == along)
                return false;
            return true;
        }
    }
}
