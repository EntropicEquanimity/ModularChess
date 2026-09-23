using System;

namespace ModularChess.Core
{
    public static class RunPlacement
    {
        #region Public Methods
        public static bool TryFindEmpty(Board board, Side player, out Square square)
        {
            square = default;
            if (board == null)
            {
                return false;
            }
            int back = player == Side.White ? 0 : 7;
            int forward = player == Side.White ? 1 : -1;
            for (int depth = 0; depth < 4; depth++)
            {
                int rank = back + forward * depth;
                if (rank < 0 || rank >= Square.BoardSize)
                {
                    break;
                }
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    square = new Square(file, rank);
                    if (board.CanPlace(square))
                    {
                        return true;
                    }
                }
            }
            return false;
        }
        public static GameState PlacePurchased(GameState state, Side player, PieceType type)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }
            if (!TryFindEmpty(state.Board, player, out Square square))
            {
                return null;
            }
            return state.AddPiece(Piece.Create(type, player), square);
        }
        #endregion
    }
}
