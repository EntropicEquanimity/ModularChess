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
        public int ActionPoints { get; }
        public TerrainLayoutKind TerrainLayout { get; }
        public int MatchSeed { get; }
        public bool RandomShuffle { get; }
        public bool RandomColors { get; }
        public bool RandomPlacement { get; }
        public bool TerrainOnPieces { get; }
        public static MatchSettings Default { get; } = new MatchSettings();
        public const int MinActionPoints = 2;
        public const int MaxActionPoints = 16;
        public const int DefaultActionPoints = 3;
        #endregion

        #region Public Methods
        public MatchSettings(
            TimeControl? time = null,
            HostColor hostColor = HostColor.White,
            AiStrength aiStrength = AiStrength.Medium,
            bool allowEndTurnWithZeroMoves = false,
            int empowerBudget = EmpoweredPowers.DefaultBudget,
            int martyrThreshold = 6,
            int martyrDraftOptions = 3,
            int actionPoints = DefaultActionPoints,
            TerrainLayoutKind terrainLayout = TerrainLayoutKind.Random,
            int matchSeed = 0,
            bool randomShuffle = true,
            bool randomColors = false,
            bool randomPlacement = false,
            bool terrainOnPieces = true)
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
            if (actionPoints < MinActionPoints)
                ActionPoints = DefaultActionPoints;
            else if (actionPoints > MaxActionPoints)
                ActionPoints = MaxActionPoints;
            else
                ActionPoints = actionPoints;
            TerrainLayout = terrainLayout;
            MatchSeed = matchSeed;
            RandomShuffle = randomShuffle;
            RandomColors = randomColors;
            RandomPlacement = randomPlacement;
            TerrainOnPieces = terrainOnPieces;
            if (!RandomShuffle && !RandomColors && !RandomPlacement)
                RandomShuffle = true;
        }
        public static int ClampActionPoints(int value)
        {
            if (value < MinActionPoints) return MinActionPoints;
            if (value > MaxActionPoints) return MaxActionPoints;
            return value;
        }
        #endregion
    }
}
