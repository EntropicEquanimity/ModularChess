using System;

namespace ModularChess.Core
{
    public sealed class Piece
    {
        #region Fields
        public Guid Id { get; }
        public PieceType Type { get; }
        public Side Side { get; }
        public bool HasMoved { get; }
        #endregion

        #region Public Methods
        public static Piece Create(PieceType type, Side side) { return new Piece(type, side); }
        internal Piece(PieceType type, Side side, bool hasMoved = false, Guid? id = null)
        {
            Id = id ?? Guid.NewGuid();
            Type = type;
            Side = side;
            HasMoved = hasMoved;
        }
        internal Piece AsMoved()
        {
            if (HasMoved)
            {
                return this;
            }

            return new Piece(Type, Side, true, Id);
        }
        internal Piece WithType(PieceType type)
        {
            return new Piece(type, Side, true, Id);
        }
        #endregion
    }
}
