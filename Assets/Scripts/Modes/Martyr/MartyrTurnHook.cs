namespace ModularChess.Core
{
    internal sealed class MartyrTurnHook : ITurnHook
    {
        #region Public Methods
        public ModeRuntime AfterCaptureRemoved(Piece captured, Rules rules, ModeRuntime runtime)
        {
            if (captured == null || runtime == null || runtime.IsSummoned(captured.Id))
            {
                return runtime ?? ModeRuntime.Empty;
            }
            int? value = PieceValues.Get(captured.Type);
            if (value == null)
            {
                return runtime;
            }
            return runtime.AddLostMaterial(captured.Side, value.Value, rules.Settings.MartyrThreshold);
        }
        public Board OnTurnEnd(Board board, ModeRuntime runtime, Side endingSide, out ModeRuntime nextRuntime)
        {
            return MartyrRules.ResolveExpiredExiles(board, runtime, endingSide, out nextRuntime);
        }
        public ModeRuntime MaybeOpenDraft(GameState state, ModeRuntime runtime, Side sideToMove, Board board)
        {
            if (runtime.PendingDraft != null)
            {
                return runtime;
            }
            int queued = sideToMove == Side.White ? runtime.WhiteDraftsQueued : runtime.BlackDraftsQueued;
            if (queued <= 0)
            {
                return runtime;
            }
            DraftOffer offer = MartyrRules.BuildOffer(state, runtime, sideToMove, board);
            PieceType? battlefield = offer.Contains(MartyrPower.BattlefieldPromotion)
                ? offer.BattlefieldType
                : null;
            return runtime.WithPendingDraft(offer, battlefield);
        }
        #endregion
    }
}
