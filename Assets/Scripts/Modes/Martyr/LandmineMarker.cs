using System;

namespace ModularChess.Core
{
    public readonly struct LandmineMarker : IEquatable<LandmineMarker>
    {
        public Side Owner { get; }
        public Square Square { get; }
        public LandmineMarker(Side owner, Square square)
        {
            Owner = owner;
            Square = square;
        }
        public bool Equals(LandmineMarker other)
        {
            return Owner == other.Owner && Square.Equals(other.Square);
        }
        public override bool Equals(object obj) => obj is LandmineMarker other && Equals(other);
        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Owner * 397) ^ Square.GetHashCode();
            }
        }
    }
}
