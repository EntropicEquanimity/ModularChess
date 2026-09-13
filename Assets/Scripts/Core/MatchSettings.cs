namespace ModularChess.Core
{
    public sealed class MatchSettings
    {
        #region Fields
        public TimeControl Time { get; }
        public HostColor HostColor { get; }
        public AiStrength AiStrength { get; }
        public bool AllowEndTurnWithZeroMoves { get; }
        public int EmpoweredCount { get; }
        public int MartyrThreshold { get; }
        public int MartyrDraftOptions { get; }
        public static MatchSettings Default { get; } = new MatchSettings();
        #endregion

        #region Public Methods
        public MatchSettings(
            TimeControl? time = null,
            HostColor hostColor = HostColor.White,
            AiStrength aiStrength = AiStrength.Medium,
            bool allowEndTurnWithZeroMoves = false,
            int empoweredCount = 2,
            int martyrThreshold = 6,
            int martyrDraftOptions = 3)
        {
            Time = time ?? TimeControl.None;
            HostColor = hostColor;
            AiStrength = aiStrength;
            AllowEndTurnWithZeroMoves = allowEndTurnWithZeroMoves;
            EmpoweredCount = empoweredCount < 1 ? 2 : empoweredCount;
            MartyrThreshold = martyrThreshold < 1 ? 6 : martyrThreshold;
            MartyrDraftOptions = martyrDraftOptions < 1 ? 3 : martyrDraftOptions;
        }
        #endregion
    }
}
