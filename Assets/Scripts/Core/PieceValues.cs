using System;

namespace ModularChess.Core
{
    public static class PieceValues
    {
        public const int Pawn = 1;
        public const int Knight = 3;
        public const int Bishop = 3;
        public const int Rook = 5;
        public const int Queen = 9;

        public static int? Get(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return Pawn;
                case PieceType.Knight:
                    return Knight;
                case PieceType.Bishop:
                    return Bishop;
                case PieceType.Rook:
                    return Rook;
                case PieceType.Queen:
                    return Queen;
                case PieceType.King:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
