namespace ModularChess.Core
{
    public sealed class RoguelikeLaw : ILaw
    {
        #region Fields
        public static RoguelikeLaw Instance { get; } = new RoguelikeLaw();
        public const int BaseSliderRange = 3;
        public int SliderRange => BaseSliderRange;
        public bool KingMayMove => false;
        public bool AllowsPromotion => false;
        public bool CheckFiltersMoves => false;
        public bool AllowsKingCapture => true;
        #endregion

        #region Public Methods
        public GameStatus ResolveStatus(
            bool inCheck,
            int legalMoveCount,
            int halfmoveClock,
            string[] positionKeys,
            Board board)
        {
            return GameStatus.InProgress;
        }
        public GameStatus? ResolveCapture(Piece captured, Side playerSide, PieceType stageTarget, Board boardAfter)
        {
            if (captured == null)
                return null;
            if (captured.Type == PieceType.King && captured.Side == playerSide)
                return GameStatus.RunLost;
            if (captured.Type == stageTarget && captured.Side != playerSide)
                return GameStatus.StageCleared;
            if (boardAfter != null && OnlyEnemyKingRemains(boardAfter, playerSide.Opponent()))
                return GameStatus.StageCleared;
            return null;
        }
        #endregion

        #region Private Methods
        static bool OnlyEnemyKingRemains(Board board, Side enemy)
        {
            bool sawKing = false;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = board.GetPiece(Square.FromIndex(i));
                if (piece == null || piece.Side != enemy)
                    continue;
                if (piece.Type != PieceType.King)
                    return false;
                sawKing = true;
            }
            return sawKing;
        }
        #endregion
    }
}
