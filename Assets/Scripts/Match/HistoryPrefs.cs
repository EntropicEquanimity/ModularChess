using ModularChess.Presentation;
using UnityEngine;

namespace ModularChess.Match
{
    public static class HistoryPrefs
    {
        #region Fields
        const string TierKey = "HistoryTier";
        static readonly int[] Caps = { 0, 5, 10, 20, 50 };
        public const int MaxTier = 4;
        #endregion

        #region Public Methods
        public static int Tier
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(TierKey, 0), 0, MaxTier);
            set
            {
                int next = Mathf.Clamp(value, 0, MaxTier);
                if (PlayerPrefs.GetInt(TierKey, 0) == next) return;
                PlayerPrefs.SetInt(TierKey, next);
                PlayerPrefs.Save();
                MatchHistoryStore.TrimToCap(Cap);
            }
        }
        public static int Cap => Caps[Tier];
        public static bool Unlocked => Tier > 0;
        public static int Snap(int value)
        {
            int best = Caps[0];
            for (int i = 0; i < Caps.Length; i++)
            {
                if (Caps[i] <= value)
                    best = Caps[i];
            }
            return best;
        }
        #endregion
    }
}
