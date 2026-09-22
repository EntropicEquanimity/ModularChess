namespace ModularChess.Core
{
    public interface ILaw
    {
        int SliderRange { get; }
        bool KingMayMove { get; }
        bool AllowsPromotion { get; }
        bool CheckFiltersMoves { get; }
        bool AllowsKingCapture { get; }
        GameStatus ResolveStatus(bool inCheck, int legalMoveCount, int halfmoveClock, string[] positionKeys, Board board);
        GameStatus? ResolveCapture(Piece captured, Side playerSide, PieceType stageTarget);
    }
}
