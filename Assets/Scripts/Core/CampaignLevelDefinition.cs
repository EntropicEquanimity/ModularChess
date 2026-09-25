using System;

namespace ModularChess.Core
{
    [Flags]
    public enum CampaignStarFlags
    {
        None = 0,
        Complete = 1,
        Time = 2,
        Special = 4
    }

    public enum CampaignTimeObjectiveKind
    {
        Turns,
        Clock
    }

    public enum CampaignSpecialObjectiveKind
    {
        None,
        LoseNoPieces,
        MaxLosses,
        CapturePiece,
        DoNotMovePiece,
        PromotePawn
    }

    public sealed class CampaignLevelDefinition
    {
        #region Fields
        public int Index { get; }
        public string TitleKey { get; }
        public string Fen { get; }
        public ModeId[] Modes { get; }
        public CampaignTimeObjectiveKind TimeKind { get; }
        public int TimeLimit { get; }
        public TimeControl Clock { get; }
        public CampaignSpecialObjectiveKind SpecialKind { get; }
        public int SpecialCount { get; }
        public PieceType SpecialPiece { get; }
        public Side PlayerSide { get; }
        public AiStrength AiStrength { get; }
        public int EmpowerBudget { get; }
        public int MartyrThreshold { get; }
        public int TurnLimit => TimeKind == CampaignTimeObjectiveKind.Turns ? TimeLimit : int.MaxValue;
        public int LossLimit => SpecialKind == CampaignSpecialObjectiveKind.LoseNoPieces
            ? 0
            : SpecialKind == CampaignSpecialObjectiveKind.MaxLosses ? SpecialCount : int.MaxValue;
        public bool ShowsClock => TimeKind == CampaignTimeObjectiveKind.Clock && !Clock.IsNone;
        #endregion

        #region Public Methods
        public CampaignLevelDefinition(
            int index,
            string titleKey,
            string fen,
            ModeId[] modes,
            CampaignTimeObjectiveKind timeKind,
            int timeLimit,
            TimeControl clock,
            CampaignSpecialObjectiveKind specialKind,
            int specialCount,
            PieceType specialPiece,
            Side playerSide = Side.White,
            AiStrength aiStrength = AiStrength.Easy,
            int empowerBudget = 4,
            int martyrThreshold = 6)
        {
            Index = index;
            TitleKey = titleKey ?? throw new ArgumentNullException(nameof(titleKey));
            Fen = fen ?? throw new ArgumentNullException(nameof(fen));
            Modes = modes ?? Array.Empty<ModeId>();
            TimeKind = timeKind;
            TimeLimit = Math.Max(1, timeLimit);
            Clock = clock;
            SpecialKind = specialKind;
            SpecialCount = Math.Max(0, specialCount);
            SpecialPiece = specialPiece;
            PlayerSide = playerSide;
            AiStrength = aiStrength;
            EmpowerBudget = Math.Max(3, empowerBudget);
            MartyrThreshold = martyrThreshold < 1 ? 6 : martyrThreshold;
        }
        #endregion
    }

    public readonly struct CampaignObjectiveLive
    {
        #region Fields
        public bool TimeFailed { get; }
        public bool SpecialFailed { get; }
        public bool SpecialMet { get; }
        public int PlayerTurns { get; }
        public int PiecesLost { get; }
        #endregion

        #region Public Methods
        public CampaignObjectiveLive(bool timeFailed, bool specialFailed, bool specialMet, int playerTurns, int piecesLost)
        {
            TimeFailed = timeFailed;
            SpecialFailed = specialFailed;
            SpecialMet = specialMet;
            PlayerTurns = playerTurns;
            PiecesLost = piecesLost;
        }
        #endregion
    }

    public static class CampaignStarEval
    {
        public static CampaignStarFlags Evaluate(
            CampaignLevelDefinition level,
            bool won,
            CampaignObjectiveLive live)
        {
            if (level == null || !won) return CampaignStarFlags.None;
            CampaignStarFlags flags = CampaignStarFlags.Complete;
            if (!live.TimeFailed && TimeMet(level, live))
                flags |= CampaignStarFlags.Time;
            if (!live.SpecialFailed && SpecialMet(level, live))
                flags |= CampaignStarFlags.Special;
            return flags;
        }
        public static int Count(CampaignStarFlags flags)
        {
            int n = 0;
            if ((flags & CampaignStarFlags.Complete) != 0) n++;
            if ((flags & CampaignStarFlags.Time) != 0) n++;
            if ((flags & CampaignStarFlags.Special) != 0) n++;
            return n;
        }
        public static bool TimeMet(CampaignLevelDefinition level, CampaignObjectiveLive live)
        {
            if (level == null || live.TimeFailed) return false;
            if (level.TimeKind == CampaignTimeObjectiveKind.Turns)
                return live.PlayerTurns <= level.TimeLimit;
            return !live.TimeFailed;
        }
        public static CampaignObjectiveLive BuildLive(
            CampaignLevelDefinition level,
            int playerTurns,
            int piecesLost,
            bool specialMet,
            bool specialFailed,
            bool playerTimedOut)
        {
            bool timeFailed = level != null && level.TimeKind == CampaignTimeObjectiveKind.Turns
                ? playerTurns > level.TimeLimit
                : playerTimedOut;
            bool lostTooMany = level != null
                && (level.SpecialKind == CampaignSpecialObjectiveKind.LoseNoPieces
                    || level.SpecialKind == CampaignSpecialObjectiveKind.MaxLosses)
                && piecesLost > level.LossLimit;
            return new CampaignObjectiveLive(
                timeFailed,
                specialFailed || lostTooMany,
                specialMet,
                playerTurns,
                piecesLost);
        }
        public static bool SpecialMet(CampaignLevelDefinition level, CampaignObjectiveLive live)
        {
            if (level == null || live.SpecialFailed) return false;
            switch (level.SpecialKind)
            {
                case CampaignSpecialObjectiveKind.None:
                    return false;
                case CampaignSpecialObjectiveKind.LoseNoPieces:
                    return live.PiecesLost <= 0;
                case CampaignSpecialObjectiveKind.MaxLosses:
                    return live.PiecesLost <= level.SpecialCount;
                case CampaignSpecialObjectiveKind.CapturePiece:
                case CampaignSpecialObjectiveKind.PromotePawn:
                    return live.SpecialMet;
                case CampaignSpecialObjectiveKind.DoNotMovePiece:
                    return !live.SpecialFailed;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level.SpecialKind), level.SpecialKind, null);
            }
        }
    }
}
