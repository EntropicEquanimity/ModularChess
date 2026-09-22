using System;

namespace ModularChess.Core
{
    public readonly struct CarriedPiece
    {
        #region Fields
        public PieceType Type { get; }
        public Square Square { get; }
        public Guid Id { get; }
        public bool HasMoved { get; }
        #endregion

        #region Public Methods
        public CarriedPiece(PieceType type, Square square, Guid id, bool hasMoved)
        {
            Type = type;
            Square = square;
            Id = id;
            HasMoved = hasMoved;
        }
        #endregion
    }
}
