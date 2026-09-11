using System;

namespace ModularChess.Core
{
    public readonly struct PieceStatus : IEquatable<PieceStatus>
    {
        public StatusKind Kind { get; }
        public Side AffectedSide { get; }
        public int RemainingTurns { get; }

        public PieceStatus(StatusKind kind, Side affectedSide, int remainingTurns)
        {
            Kind = kind;
            AffectedSide = affectedSide;
            RemainingTurns = remainingTurns;
        }

        public PieceStatus Tick()
        {
            return new PieceStatus(Kind, AffectedSide, RemainingTurns - 1);
        }

        public bool Equals(PieceStatus other)
        {
            return Kind == other.Kind
                   && AffectedSide == other.AffectedSide
                   && RemainingTurns == other.RemainingTurns;
        }

        public override bool Equals(object obj) => obj is PieceStatus other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Kind;
                hash = (hash * 397) ^ (int)AffectedSide;
                hash = (hash * 397) ^ RemainingTurns;
                return hash;
            }
        }
    }
}
