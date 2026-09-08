using System;

namespace ModularChess.Core
{
    internal static class AttackMap
    {
        public static bool IsInCheck(Board board, Side side)
        {
            Square? king = board.FindKing(side);
            if (king == null)
            {
                return false;
            }

            return IsAttacked(board, king.Value, side.Opponent());
        }

        public static bool IsAttacked(Board board, Square square, Side bySide)
        {
            if (!square.IsOnBoard)
            {
                return false;
            }

            return IsAttackedByPawn(board, square, bySide)
                   || IsAttackedByKnight(board, square, bySide)
                   || IsAttackedByKing(board, square, bySide)
                   || IsAttackedBySlider(
                       board,
                       square,
                       bySide,
                       Directions.BishopFiles,
                       Directions.BishopRanks,
                       PieceType.Bishop)
                   || IsAttackedBySlider(
                       board,
                       square,
                       bySide,
                       Directions.RookFiles,
                       Directions.RookRanks,
                       PieceType.Rook);
        }

        private static bool IsAttackedByPawn(Board board, Square target, Side bySide)
        {
            int rankDelta = bySide == Side.White ? -1 : 1;
            return HasPiece(board, target.Offset(-1, rankDelta), bySide, PieceType.Pawn)
                   || HasPiece(board, target.Offset(1, rankDelta), bySide, PieceType.Pawn);
        }

        private static bool IsAttackedByKnight(Board board, Square target, Side bySide)
        {
            for (int i = 0; i < Directions.KnightFiles.Length; i++)
            {
                if (HasPiece(board, target.Offset(Directions.KnightFiles[i], Directions.KnightRanks[i]), bySide, PieceType.Knight))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAttackedByKing(Board board, Square target, Side bySide)
        {
            for (int i = 0; i < Directions.KingFiles.Length; i++)
            {
                if (HasPiece(board, target.Offset(Directions.KingFiles[i], Directions.KingRanks[i]), bySide, PieceType.King))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsAttackedBySlider(
            Board board,
            Square target,
            Side bySide,
            int[] fileDeltas,
            int[] rankDeltas,
            PieceType slider)
        {
            for (int i = 0; i < fileDeltas.Length; i++)
            {
                Square cursor = target.Offset(fileDeltas[i], rankDeltas[i]);
                while (cursor.IsOnBoard)
                {
                    Piece piece = board.GetPiece(cursor);
                    if (piece != null)
                    {
                        if (piece.Side == bySide && (piece.Type == slider || piece.Type == PieceType.Queen))
                        {
                            return true;
                        }

                        break;
                    }

                    cursor = cursor.Offset(fileDeltas[i], rankDeltas[i]);
                }
            }

            return false;
        }

        private static bool HasPiece(Board board, Square square, Side side, PieceType type)
        {
            if (!square.IsOnBoard)
            {
                return false;
            }

            Piece piece = board.GetPiece(square);
            return piece != null && piece.Side == side && piece.Type == type;
        }
    }
}
