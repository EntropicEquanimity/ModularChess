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
        public byte Hue { get; }
        #endregion

        #region Public Methods
        internal Piece(PieceType type, Side side, bool hasMoved = false, Guid? id = null, byte hue = 0)
        {
            Id = id ?? Guid.NewGuid();
            Type = type;
            Side = side;
            HasMoved = hasMoved;
            Hue = hue;
        }
        internal Piece AsMoved()
        {
            if (HasMoved) { return this; }
            return new Piece(Type, Side, true, Id, Hue);
        }
        internal Piece WithType(PieceType type)
        {
            return new Piece(type, Side, true, Id, Hue);
        }
        internal Piece WithHue(byte hue)
        {
            return new Piece(Type, Side, HasMoved, Id, hue);
        }
        #endregion
    }
}
