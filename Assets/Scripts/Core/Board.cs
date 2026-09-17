using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class Board
    {
        #region Fields
        private readonly Piece[] _squares;
        public IEnumerable<Piece> OccupiedPieces
        {
            get
            {
                for (int i = 0; i < _squares.Length; i++)
                {
                    Piece piece = _squares[i];
                    if (piece != null)
                    {
                        yield return piece;
                    }
                }
            }
        }
        #endregion

        #region Public Methods
        internal Board(Piece[] squares)
        {
            if (squares == null)
            {
                throw new ArgumentNullException(nameof(squares));
            }

            if (squares.Length != Square.BoardSize * Square.BoardSize)
            {
                throw new ArgumentException("Board must contain 64 squares.", nameof(squares));
            }

            _squares = squares;
        }
        internal static Board Empty()
        {
            return new Board(new Piece[Square.BoardSize * Square.BoardSize]);
        }
        public Piece GetPiece(Square square)
        {
            if (!square.IsOnBoard)
            {
                throw new ArgumentOutOfRangeException(nameof(square), square, "Square is off the board.");
            }

            return _squares[square.ToIndex()];
        }
        public bool IsEmpty(Square square)
        {
            return square.IsOnBoard && _squares[square.ToIndex()] == null;
        }
        public bool CanPlace(Square square)
        {
            return IsEmpty(square);
        }
        public Square? FindKing(Side side)
        {
            for (int i = 0; i < _squares.Length; i++)
            {
                Piece piece = _squares[i];
                if (piece != null && piece.Type == PieceType.King && piece.Side == side)
                {
                    return Square.FromIndex(i);
                }
            }

            return null;
        }
        public Square? FindSquare(Guid pieceId)
        {
            for (int i = 0; i < _squares.Length; i++)
            {
                Piece piece = _squares[i];
                if (piece != null && piece.Id == pieceId)
                {
                    return Square.FromIndex(i);
                }
            }

            return null;
        }
        internal Board ApplyUnchecked(Move move)
        {
            Piece moving = GetPiece(move.From);
            if (moving == null)
            {
                throw new InvalidOperationException("Cannot apply a move from an empty square.");
            }

            Piece[] next = (Piece[])_squares.Clone();
            next[move.From.ToIndex()] = null;

            switch (move.Kind)
            {
                case MoveKind.Quiet:
                case MoveKind.Capture:
                    next[move.To.ToIndex()] = moving.AsMoved();
                    break;
                case MoveKind.Promotion:
                    next[move.To.ToIndex()] = moving.WithType(move.PromotionType.Value);
                    break;
                case MoveKind.EnPassant:
                    next[new Square(move.To.File, move.From.Rank).ToIndex()] = null;
                    next[move.To.ToIndex()] = moving.AsMoved();
                    break;
                case MoveKind.CastleKingSide:
                    ApplyCastle(next, moving, move.From, kingFile: 6, rookFromFile: 7, rookToFile: 5);
                    break;
                case MoveKind.CastleQueenSide:
                    ApplyCastle(next, moving, move.From, kingFile: 2, rookFromFile: 0, rookToFile: 3);
                    break;
                case MoveKind.Swap:
                    Piece swapped = _squares[move.To.ToIndex()];
                    next[move.To.ToIndex()] = moving.AsMoved();
                    next[move.From.ToIndex()] = swapped == null ? null : swapped.AsMoved();
                    break;
                case MoveKind.Bombard:
                    next[move.From.ToIndex()] = moving.AsMoved();
                    next[move.To.ToIndex()] = null;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(move), move.Kind, null);
            }

            return new Board(next);
        }
        internal Board WithPiece(Square square, Piece piece)
        {
            if (!square.IsOnBoard)
            {
                throw new ArgumentOutOfRangeException(nameof(square), square, "Square is off the board.");
            }

            int index = square.ToIndex();
            Piece occupant = _squares[index];
            if (occupant != null
                && occupant.Type == PieceType.King
                && (piece == null || piece.Type != PieceType.King))
            {
                return this;
            }

            Piece[] next = (Piece[])_squares.Clone();
            next[index] = piece;
            return new Board(next);
        }
        #endregion

        #region Private Methods
        private static void ApplyCastle(
            Piece[] squares,
            Piece king,
            Square kingFrom,
            int kingFile,
            int rookFromFile,
            int rookToFile)
        {
            Square rookFrom = new Square(rookFromFile, kingFrom.Rank);
            int rookFromIndex = rookFrom.ToIndex();
            Piece rook = squares[rookFromIndex];
            if (rook == null || rook.Type != PieceType.Rook || rook.Side != king.Side)
            {
                throw new InvalidOperationException("Castling requires an unmoved rook on the original square.");
            }

            squares[rookFromIndex] = null;
            squares[new Square(kingFile, kingFrom.Rank).ToIndex()] = king.AsMoved();
            squares[new Square(rookToFile, kingFrom.Rank).ToIndex()] = rook.AsMoved();
        }
        #endregion
    }
}
