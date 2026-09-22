namespace ModularChess.Core
{
    public enum RoguelikeDifficulty
    {
        Easy,
        Normal,
        Hard
    }

    public sealed class RoguelikeRunSettings
    {
        #region Fields
        public PieceType StartingPiece { get; set; } = PieceType.Rook;
        public HostColor PlayerColor { get; set; } = HostColor.Random;
        public RoguelikeDifficulty Difficulty { get; set; } = RoguelikeDifficulty.Normal;
        #endregion
    }
}
