namespace ModularChess.Core
{
    public static class EmpoweredPowers
    {
        public const string EffectName = "Empowered";

        public static string Describe(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return "Super Pawn: cannot Capture. May be Captured from any Square except the 3 Squares in front.";
                case PieceType.Knight:
                    return "Extra Life: the first Capture of this Knight is negated. Then Extra Life is gone and this Piece is no longer Empowered.";
                case PieceType.Bishop:
                    return "May swap with an allied Pawn on any of the 8 neighboring Squares instead of a normal Move.";
                case PieceType.Rook:
                    return "May pass through allied Pieces when moving.";
                case PieceType.Queen:
                    return "Moves as a Queen or a Knight in a single Move.";
                case PieceType.King:
                    return "After this King Moves, it may make an optional extra Move or End Turn.";
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
