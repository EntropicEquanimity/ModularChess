using System;

namespace ModularChess.Core
{
    public readonly struct CaptureRecord
    {
        public Guid Id { get; }
        public Side Side { get; }
        public PieceType Type { get; }
        public bool Exiled { get; }
        public Square Origin { get; }
        public int RemainingTurns { get; }
        public bool HasMoved { get; }

        public CaptureRecord(
            Guid id,
            Side side,
            PieceType type,
            bool exiled = false,
            Square origin = default,
            int remainingTurns = 0,
            bool hasMoved = true)
        {
            Id = id;
            Side = side;
            Type = type;
            Exiled = exiled;
            Origin = origin;
            RemainingTurns = remainingTurns;
            HasMoved = hasMoved;
        }

        public CaptureRecord WithRemaining(int remainingTurns)
        {
            return new CaptureRecord(Id, Side, Type, Exiled, Origin, remainingTurns, HasMoved);
        }
    }
}
