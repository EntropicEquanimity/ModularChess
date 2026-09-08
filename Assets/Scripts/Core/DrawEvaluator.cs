namespace ModularChess.Core
{
    internal static class DrawEvaluator
    {
        public static GameStatus Resolve(
            bool inCheck,
            int legalMoveCount,
            int halfmoveClock,
            string[] positionKeys,
            Board board)
        {
            if (legalMoveCount == 0)
            {
                return inCheck ? GameStatus.Checkmate : GameStatus.Stalemate;
            }

            if (halfmoveClock >= 100 || IsThreefold(positionKeys) || IsInsufficientMaterial(board))
            {
                return GameStatus.Draw;
            }

            return GameStatus.InProgress;
        }

        private static bool IsThreefold(string[] positionKeys)
        {
            if (positionKeys.Length == 0)
            {
                return false;
            }

            string current = positionKeys[positionKeys.Length - 1];
            int count = 0;
            for (int i = 0; i < positionKeys.Length; i++)
            {
                if (positionKeys[i] == current)
                {
                    count++;
                    if (count >= 3)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static bool IsInsufficientMaterial(Board board)
        {
            Piece whiteMinor = null;
            Piece blackMinor = null;
            Square? whiteBishopSquare = null;
            Square? blackBishopSquare = null;
            int whiteNonKing = 0;
            int blackNonKing = 0;

            for (int file = 0; file < Square.BoardSize; file++)
            {
                for (int rank = 0; rank < Square.BoardSize; rank++)
                {
                    Square square = new Square(file, rank);
                    Piece piece = board.GetPiece(square);
                    if (piece == null || piece.Type == PieceType.King)
                    {
                        continue;
                    }

                    if (piece.Type == PieceType.Pawn || piece.Type == PieceType.Rook || piece.Type == PieceType.Queen)
                    {
                        return false;
                    }

                    if (piece.Side == Side.White)
                    {
                        whiteNonKing++;
                        whiteMinor = piece;
                        if (piece.Type == PieceType.Bishop)
                        {
                            whiteBishopSquare = square;
                        }
                    }
                    else
                    {
                        blackNonKing++;
                        blackMinor = piece;
                        if (piece.Type == PieceType.Bishop)
                        {
                            blackBishopSquare = square;
                        }
                    }

                    if (whiteNonKing > 1 || blackNonKing > 1)
                    {
                        return false;
                    }
                }
            }

            if (whiteNonKing == 0 && blackNonKing == 0)
            {
                return true;
            }

            if (whiteNonKing == 1 && blackNonKing == 0 && IsMinor(whiteMinor))
            {
                return true;
            }

            if (blackNonKing == 1 && whiteNonKing == 0 && IsMinor(blackMinor))
            {
                return true;
            }

            if (whiteNonKing == 1
                && blackNonKing == 1
                && whiteMinor.Type == PieceType.Bishop
                && blackMinor.Type == PieceType.Bishop
                && whiteBishopSquare != null
                && blackBishopSquare != null)
            {
                return SameColor(whiteBishopSquare.Value, blackBishopSquare.Value);
            }

            return false;
        }

        private static bool IsMinor(Piece piece)
        {
            return piece != null && (piece.Type == PieceType.Knight || piece.Type == PieceType.Bishop);
        }

        private static bool SameColor(Square a, Square b)
        {
            return ((a.File + a.Rank) & 1) == ((b.File + b.Rank) & 1);
        }
    }
}
