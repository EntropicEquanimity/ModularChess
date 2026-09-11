namespace ModularChess.Core
{
    internal static class SuperPawn
    {
        public static bool CanCaptureFrom(Square pawnSquare, Side pawnSide, Square from, PieceType capturerType)
        {
            int rear = pawnSide == Side.White ? -1 : 1;
            int fileDelta = from.File - pawnSquare.File;
            int rankDelta = from.Rank - pawnSquare.Rank;

            if (capturerType == PieceType.Knight)
            {
                bool knightAttack = false;
                for (int i = 0; i < Directions.KnightFiles.Length; i++)
                {
                    if (fileDelta == Directions.KnightFiles[i] && rankDelta == Directions.KnightRanks[i])
                    {
                        knightAttack = true;
                        break;
                    }
                }

                if (!knightAttack)
                {
                    return false;
                }

                return pawnSide == Side.White ? rankDelta < 0 : rankDelta > 0;
            }

            if (MathAbs(fileDelta) <= 1 && rankDelta == rear)
            {
                return true;
            }

            if (fileDelta == 0 && rankDelta * rear > 0)
            {
                return true;
            }

            if (MathAbs(fileDelta) == MathAbs(rankDelta) && rankDelta * rear > 0)
            {
                return true;
            }

            return false;
        }

        static int MathAbs(int value) => value < 0 ? -value : value;
    }
}
