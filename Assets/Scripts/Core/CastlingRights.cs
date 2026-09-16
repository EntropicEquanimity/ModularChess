using System;

namespace ModularChess.Core
{
    public readonly struct CastlingRights : IEquatable<CastlingRights>
    {
        #region Fields
        public static CastlingRights All { get; } = new CastlingRights(true, true, true, true);
        public static CastlingRights None { get; } = new CastlingRights(false, false, false, false);
        public bool WhiteKingSide { get; }
        public bool WhiteQueenSide { get; }
        public bool BlackKingSide { get; }
        public bool BlackQueenSide { get; }
        #endregion

        #region Public Methods
        public CastlingRights(
            bool whiteKingSide,
            bool whiteQueenSide,
            bool blackKingSide,
            bool blackQueenSide)
        {
            WhiteKingSide = whiteKingSide;
            WhiteQueenSide = whiteQueenSide;
            BlackKingSide = blackKingSide;
            BlackQueenSide = blackQueenSide;
        }
        public bool HasKingSide(Side side)
        {
            switch (side)
            {
                case Side.White:
                    return WhiteKingSide;
                case Side.Black:
                    return BlackKingSide;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }
        public bool HasQueenSide(Side side)
        {
            switch (side)
            {
                case Side.White:
                    return WhiteQueenSide;
                case Side.Black:
                    return BlackQueenSide;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }
        public CastlingRights WithoutKingSide(Side side)
        {
            switch (side)
            {
                case Side.White:
                    return new CastlingRights(false, WhiteQueenSide, BlackKingSide, BlackQueenSide);
                case Side.Black:
                    return new CastlingRights(WhiteKingSide, WhiteQueenSide, false, BlackQueenSide);
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }
        public CastlingRights WithoutQueenSide(Side side)
        {
            switch (side)
            {
                case Side.White:
                    return new CastlingRights(WhiteKingSide, false, BlackKingSide, BlackQueenSide);
                case Side.Black:
                    return new CastlingRights(WhiteKingSide, WhiteQueenSide, BlackKingSide, false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }
        public CastlingRights WithoutSide(Side side)
        {
            switch (side)
            {
                case Side.White:
                    return new CastlingRights(false, false, BlackKingSide, BlackQueenSide);
                case Side.Black:
                    return new CastlingRights(WhiteKingSide, WhiteQueenSide, false, false);
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }
        internal CastlingRights WithoutPieceSquare(Square square)
        {
            return WithoutRookOrigin(square);
        }
        internal CastlingRights AfterMove(Move move, Board before)
        {
            CastlingRights result = this;
            Piece moving = before.GetPiece(move.From);
            if (moving == null)
            {
                return result;
            }

            if (moving.Type == PieceType.King)
            {
                result = result.WithoutSide(moving.Side);
            }
            else if (moving.Type == PieceType.Rook)
            {
                result = result.WithoutRookOrigin(move.From);
            }

            if (move.CapturedType != null)
            {
                Square capturedSquare = move.Kind == MoveKind.EnPassant
                    ? new Square(move.To.File, move.From.Rank)
                    : move.To;
                result = result.WithoutRookOrigin(capturedSquare);
            }

            return result;
        }
        public bool Equals(CastlingRights other)
        {
            return WhiteKingSide == other.WhiteKingSide
                && WhiteQueenSide == other.WhiteQueenSide
                && BlackKingSide == other.BlackKingSide
                && BlackQueenSide == other.BlackQueenSide;
        }
        public override bool Equals(object obj) => obj is CastlingRights other && Equals(other);
        public override int GetHashCode()
        {
            int hash = WhiteKingSide ? 1 : 0;
            hash |= WhiteQueenSide ? 2 : 0;
            hash |= BlackKingSide ? 4 : 0;
            hash |= BlackQueenSide ? 8 : 0;
            return hash;
        }
        public static bool operator ==(CastlingRights left, CastlingRights right) => left.Equals(right);
        public static bool operator !=(CastlingRights left, CastlingRights right) => !left.Equals(right);
        public override string ToString()
        {
            if (this == None)
            {
                return "-";
            }
            string text = string.Empty;
            if (WhiteKingSide)
            {
                text += "K";
            }
            if (WhiteQueenSide)
            {
                text += "Q";
            }
            if (BlackKingSide)
            {
                text += "k";
            }
            if (BlackQueenSide)
            {
                text += "q";
            }
            return text;
        }
        #endregion

        #region Private Methods
        private CastlingRights WithoutRookOrigin(Square square)
        {
            if (square.File == 0 && square.Rank == 0)
            {
                return WithoutQueenSide(Side.White);
            }

            if (square.File == 7 && square.Rank == 0)
            {
                return WithoutKingSide(Side.White);
            }

            if (square.File == 0 && square.Rank == 7)
            {
                return WithoutQueenSide(Side.Black);
            }

            if (square.File == 7 && square.Rank == 7)
            {
                return WithoutKingSide(Side.Black);
            }

            return this;
        }
        #endregion
    }
}
