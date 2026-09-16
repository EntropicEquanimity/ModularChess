using System;
using ModularChess.Core;

namespace ModularChess.Presentation
{
    public static class CaptureTrauma
    {
        #region Fields
        public const float Pawn = 0.22f;
        public const float Minor = 0.4f;
        public const float Queen = 0.82f;
        public const float Check = 0.55f;
        public const float Mate = 0.9f;
        public const float Deflect = 0.28f;
        #endregion

        #region Public Methods
        public static float For(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return Pawn;
                case PieceType.Knight:
                case PieceType.Bishop:
                case PieceType.Rook:
                    return Minor;
                case PieceType.Queen:
                    return Queen;
                case PieceType.King:
                    return Minor;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        #endregion
    }
}
