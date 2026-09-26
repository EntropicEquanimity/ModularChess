using System;

namespace ModularChess.Core
{
    internal sealed class ActionEconomyTurn : ITurnHook
    {
        public bool PoolKeepsTurnOpen(ModeRuntime runtime, MatchSettings settings)
        {
            if (runtime == null || settings == null)
                return false;
            return runtime.MovesThisTurn > 0 && runtime.PaidMovesThisTurn < settings.ActionPoints;
        }
        public bool KeepsTurnAfterMove(int paidAfter, bool extrasOpen, MatchSettings settings)
        {
            if (extrasOpen)
                return true;
            if (settings == null)
                return false;
            return paidAfter < settings.ActionPoints;
        }
        public bool BlocksRepeatPiece(ModeRuntime runtime, Guid pieceId)
        {
            if (runtime == null)
                return false;
            if (runtime.ExtraMoveKingId != null || runtime.OverloadPieceId != null)
                return false;
            return runtime.MovedThisTurn(pieceId);
        }
    }
}
