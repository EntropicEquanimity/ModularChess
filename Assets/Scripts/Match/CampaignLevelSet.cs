using System;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Match
{
    [CreateAssetMenu(fileName = "CampaignLevels", menuName = "Modular Chess/Campaign Level Set")]
    public sealed class CampaignLevelSet : ScriptableObject
    {
        #region Fields
        public const string AssetPath = "Assets/Data/Campaign/CampaignLevels.asset";
        [SerializeField] CampaignLevelEntry[] levels = Array.Empty<CampaignLevelEntry>();
        #endregion

        #region Public Methods
        public static CampaignLevelSet Load()
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<CampaignLevelSet>(AssetPath);
#else
            return null;
#endif
        }
        public CampaignLevelDefinition[] ToDefinitions()
        {
            if (levels == null || levels.Length == 0)
                return Array.Empty<CampaignLevelDefinition>();
            var defs = new CampaignLevelDefinition[levels.Length];
            for (int i = 0; i < levels.Length; i++)
                defs[i] = levels[i].ToDefinition(i);
            return defs;
        }
        #endregion
    }

    [Serializable]
    public sealed class CampaignLevelEntry
    {
        #region Fields
        public string titleKey;
        public string fen;
        public ModeId[] modes;
        public CampaignTimeObjectiveKind timeKind = CampaignTimeObjectiveKind.Turns;
        public int timeLimit = 40;
        public int clockMinutes;
        public int clockIncrement;
        public CampaignSpecialObjectiveKind specialKind = CampaignSpecialObjectiveKind.LoseNoPieces;
        public int specialCount;
        public PieceType specialPiece = PieceType.Pawn;
        public Side playerSide = Side.White;
        public AiStrength aiStrength = AiStrength.Easy;
        public int empowerBudget = 4;
        public int martyrThreshold = 6;
        #endregion

        #region Public Methods
        public CampaignLevelDefinition ToDefinition(int index)
        {
            string key = string.IsNullOrEmpty(titleKey) ? "campaign.level." + (index + 1) : titleKey;
            string board = string.IsNullOrEmpty(fen) ? "8/8/8/8/8/8/8/8 w - - 0 1" : fen;
            return new CampaignLevelDefinition(
                index,
                key,
                board,
                modes,
                timeKind,
                timeLimit,
                new TimeControl(clockMinutes, clockIncrement),
                specialKind,
                specialCount,
                specialPiece,
                playerSide,
                aiStrength,
                empowerBudget,
                martyrThreshold);
        }
        #endregion
    }
}
