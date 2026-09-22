namespace ModularChess.Core
{
    public sealed class FideLaw : ILaw
    {
        #region Fields
        public static FideLaw Instance { get; } = new FideLaw();
        public int SliderRange => int.MaxValue;
        public bool KingMayMove => true;
        public bool AllowsPromotion => true;
        public bool CheckFiltersMoves => true;
        public bool AllowsKingCapture => false;
        #endregion

        #region Public Methods
        public GameStatus ResolveStatus(
            bool inCheck,
            int legalMoveCount,
            int halfmoveClock,
            string[] positionKeys,
            Board board)
        {
            return DrawEvaluator.Resolve(inCheck, legalMoveCount, halfmoveClock, positionKeys, board);
        }
        public GameStatus? ResolveCapture(Piece captured, Side playerSide, PieceType stageTarget, Board boardAfter)
        {
            return null;
        }
        #endregion
    }
}
