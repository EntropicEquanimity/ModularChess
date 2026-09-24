namespace ModularChess.Core
{
    public static class EmpoweredPowers
    {
        public const string EffectName = "Empowered";
        public const int DefaultBudget = 4;
        public const int MinBudget = 3;
        public const int MaxBudget = 20;

        public static int Cost(PieceType type)
        {
            switch (type)
            {
                case PieceType.Queen:
                    return 3;
                case PieceType.King:
                case PieceType.Rook:
                case PieceType.Knight:
                    return 2;
                case PieceType.Bishop:
                case PieceType.Pawn:
                    return 1;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        public static string Describe(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return "Super Pawn: cannot Capture. May be Captured from the side or behind, not from in front.";
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

        public static int ClampBudget(int budget)
        {
            if (budget < MinBudget)
                return MinBudget;
            if (budget > MaxBudget)
                return MaxBudget;
            return budget;
        }
    }
}
