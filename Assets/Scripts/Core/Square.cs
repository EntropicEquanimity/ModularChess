using System;

namespace ModularChess.Core
{
    public readonly struct Square : IEquatable<Square>
    {
        #region Fields
        public const int BoardSize = 8;
        public int File { get; }
        public int Rank { get; }
        public bool IsOnBoard => File >= 0 && File < BoardSize && Rank >= 0 && Rank < BoardSize;
        #endregion

        #region Public Methods
        public Square(int file, int rank)
        {
            File = file;
            Rank = rank;
        }
        public Square Offset(int fileDelta, int rankDelta) => new Square(File + fileDelta, Rank + rankDelta);
        public int ToIndex()
        {
            if (!IsOnBoard)
            {
                throw new InvalidOperationException("Cannot index a square that is off the board.");
            }

            return Rank * BoardSize + File;
        }
        public static Square FromIndex(int index)
        {
            if (index < 0 || index >= BoardSize * BoardSize)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, null);
            }

            return new Square(index % BoardSize, index / BoardSize);
        }
        public static bool TryParse(string algebraic, out Square square)
        {
            square = default;
            if (string.IsNullOrEmpty(algebraic) || algebraic.Length != 2)
            {
                return false;
            }

            char fileChar = char.ToLowerInvariant(algebraic[0]);
            char rankChar = algebraic[1];
            if (fileChar < 'a' || fileChar > 'h' || rankChar < '1' || rankChar > '8')
            {
                return false;
            }

            square = new Square(fileChar - 'a', rankChar - '1');
            return true;
        }
        public override string ToString()
        {
            if (!IsOnBoard)
            {
                return "?";
            }

            return $"{(char)('a' + File)}{Rank + 1}";
        }
        public bool Equals(Square other) => File == other.File && Rank == other.Rank;
        public override bool Equals(object obj) => obj is Square other && Equals(other);
        public override int GetHashCode() => (File << 3) ^ Rank;
        public static bool operator ==(Square left, Square right) => left.Equals(right);
        public static bool operator !=(Square left, Square right) => !left.Equals(right);
        #endregion
    }
}
