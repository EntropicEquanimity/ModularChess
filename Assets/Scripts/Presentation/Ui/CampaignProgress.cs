using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class CampaignProgress
    {
        #region Fields
        const string StarsKeyPrefix = "CampaignStars_";
        #endregion

        #region Public Methods
        public static event System.Action Changed;
        public static CampaignStarFlags GetStars(int levelIndex)
        {
            return (CampaignStarFlags)PlayerPrefs.GetInt(StarsKey(levelIndex), 0);
        }
        public static bool HasStar(int levelIndex, CampaignStarFlags star)
        {
            return (GetStars(levelIndex) & star) != 0;
        }
        public static bool IsUnlocked(int levelIndex)
        {
            if (levelIndex <= 0) return true;
            return HasStar(levelIndex - 1, CampaignStarFlags.Complete);
        }
        public static int Award(int levelIndex, CampaignStarFlags earned)
        {
            if (earned == CampaignStarFlags.None) return 0;
            CampaignStarFlags before = GetStars(levelIndex);
            CampaignStarFlags newly = earned & ~before;
            if (newly == CampaignStarFlags.None) return 0;
            PlayerPrefs.SetInt(StarsKey(levelIndex), (int)(before | newly));
            PlayerPrefs.Save();
            int merit = CampaignStarEval.Count(newly);
            if (merit > 0)
                MeritWallet.Add(merit);
            Changed?.Invoke();
            return merit;
        }
        public static void Clear()
        {
            for (int i = 0; i < CampaignCatalog.Count; i++)
                PlayerPrefs.DeleteKey(StarsKey(i));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        #endregion

        #region Private Methods
        static string StarsKey(int levelIndex) => StarsKeyPrefix + levelIndex;
        #endregion
    }
}
