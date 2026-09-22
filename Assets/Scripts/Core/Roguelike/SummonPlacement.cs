using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public static class SummonPlacement
    {
        #region Public Methods
        public static Board PlacePawns(
            Board board,
            Side side,
            int count,
            bool skipBackRank,
            ModeRuntime runtime,
            out ModeRuntime nextRuntime,
            Random rng)
        {
            nextRuntime = runtime ?? ModeRuntime.Empty;
            if (count <= 0 || board == null)
                return board;
            int back = side == Side.White ? 0 : 7;
            int forward = side == Side.White ? 1 : -1;
            int startOffset = skipBackRank ? 1 : 0;
            int placed = 0;
            for (int depth = startOffset; depth < 4 && placed < count; depth++)
            {
                int rank = back + forward * depth;
                if (rank < 0 || rank >= Square.BoardSize)
                    break;
                var empties = new List<Square>(8);
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    Square square = new Square(file, rank);
                    if (board.CanPlace(square))
                        empties.Add(square);
                }
                while (empties.Count > 0 && placed < count)
                {
                    int pick = rng.Next(empties.Count);
                    Square square = empties[pick];
                    empties.RemoveAt(pick);
                    Piece pawn = new Piece(PieceType.Pawn, side);
                    Board next = board.WithPiece(square, pawn);
                    if (next.GetPiece(square) == null || next.GetPiece(square).Type != PieceType.Pawn)
                        continue;
                    board = next;
                    nextRuntime = nextRuntime.AddSummoned(pawn.Id);
                    placed++;
                }
            }
            return board;
        }
        #endregion
    }
}
