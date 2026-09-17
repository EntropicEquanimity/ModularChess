using System.Collections.Generic;

namespace ModularChess.Core
{
    internal readonly struct PatternStep
    {
        #region Fields
        public Square Square { get; }
        public Piece Occupant { get; }
        #endregion

        #region Public Methods
        public PatternStep(Square square, Piece occupant)
        {
            Square = square;
            Occupant = occupant;
        }
        #endregion
    }

    internal static class Pattern
    {
        #region Public Methods
        public static bool SuperPawnAllowsCapture(Square pawnSquare, Side pawnSide, Square from)
        {
            int forward = pawnSide == Side.White ? 1 : -1;
            int fileDelta = from.File - pawnSquare.File;
            int rankDelta = from.Rank - pawnSquare.Rank;
            int along = rankDelta * forward;
            if (along <= 0)
            {
                return true;
            }
            int absFile = fileDelta < 0 ? -fileDelta : fileDelta;
            if (absFile <= 1 || absFile == along)
            {
                return false;
            }
            return true;
        }
        public static void Ray(Board board, Square from, int fileDelta, int rankDelta, List<PatternStep> steps)
        {
            steps.Clear();
            Square cursor = from.Offset(fileDelta, rankDelta);
            while (cursor.IsOnBoard)
            {
                steps.Add(new PatternStep(cursor, board.GetPiece(cursor)));
                cursor = cursor.Offset(fileDelta, rankDelta);
            }
        }
        #endregion
    }
}
