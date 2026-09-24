using System;

namespace ModularChess.Core
{
    [Flags]
    public enum CampaignStarFlags
    {
        None = 0,
        Complete = 1,
        TurnLimit = 2,
        LossLimit = 4
    }

    public sealed class CampaignLevelDefinition
    {
        #region Fields
        public int Index { get; }
        public string TitleKey { get; }
        public string Fen { get; }
        public ModeId[] Modes { get; }
        public int TurnLimit { get; }
        public int LossLimit { get; }
        public Side PlayerSide { get; }
        public AiStrength AiStrength { get; }
        public int EmpowerBudget { get; }
        #endregion

        #region Public Methods
        public CampaignLevelDefinition(
            int index,
            string titleKey,
            string fen,
            ModeId[] modes,
            int turnLimit,
            int lossLimit,
            Side playerSide = Side.White,
            AiStrength aiStrength = AiStrength.Easy,
            int empowerBudget = 4)
        {
            Index = index;
            TitleKey = titleKey ?? throw new ArgumentNullException(nameof(titleKey));
            Fen = fen ?? throw new ArgumentNullException(nameof(fen));
            Modes = modes ?? Array.Empty<ModeId>();
            TurnLimit = Math.Max(1, turnLimit);
            LossLimit = Math.Max(0, lossLimit);
            PlayerSide = playerSide;
            AiStrength = aiStrength;
            EmpowerBudget = Math.Max(3, empowerBudget);
        }
        #endregion
    }

    public static class CampaignStarEval
    {
        public static CampaignStarFlags Evaluate(
            CampaignLevelDefinition level,
            bool won,
            int playerTurns,
            int piecesLost)
        {
            if (level == null || !won) return CampaignStarFlags.None;
            CampaignStarFlags flags = CampaignStarFlags.Complete;
            if (playerTurns <= level.TurnLimit)
                flags |= CampaignStarFlags.TurnLimit;
            if (piecesLost <= level.LossLimit)
                flags |= CampaignStarFlags.LossLimit;
            return flags;
        }
        public static int Count(CampaignStarFlags flags)
        {
            int n = 0;
            if ((flags & CampaignStarFlags.Complete) != 0) n++;
            if ((flags & CampaignStarFlags.TurnLimit) != 0) n++;
            if ((flags & CampaignStarFlags.LossLimit) != 0) n++;
            return n;
        }
    }
}
