namespace ModularChess.Core
{
    public sealed class MatchSettings
    {
        #region Fields
        public TimeControl Time { get; }
        public HostColor HostColor { get; }
        public AiStrength AiStrength { get; }
        public bool AllowEndTurnWithZeroMoves { get; }
        public int EmpowerBudget { get; }
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
            int empowerBudget = EmpoweredPowers.DefaultBudget,
            int martyrThreshold = 6,
            int martyrDraftOptions = 3)
        {
            Time = time ?? TimeControl.None;
            HostColor = hostColor;
            AiStrength = aiStrength;
            AllowEndTurnWithZeroMoves = allowEndTurnWithZeroMoves;
            EmpowerBudget = EmpoweredPowers.ClampBudget(empowerBudget);
            MartyrThreshold = martyrThreshold < 1 ? 6 : martyrThreshold;
            if (martyrDraftOptions < 1)
                MartyrDraftOptions = 3;
            else if (martyrDraftOptions > 5)
                MartyrDraftOptions = 5;
            else
                MartyrDraftOptions = martyrDraftOptions;
        }
        #endregion
    }
}
