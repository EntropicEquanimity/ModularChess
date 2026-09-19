using UnityEngine;

namespace ModularChess.Match
{
    public static class HistoryPrefs
    {
        #region Fields
        const string Key = "HistoryCap";
        public const int Min = 5;
        public const int Max = 50;
        public const int Step = 5;
        public const int Default = 10;
        #endregion

        #region Public Methods
        public static int Cap
        {
            get => Snap(PlayerPrefs.GetInt(Key, Default));
            set
            {
                int next = Snap(value);
                if (PlayerPrefs.GetInt(Key, Default) == next) return;
                PlayerPrefs.SetInt(Key, next);
                PlayerPrefs.Save();
                MatchHistoryStore.TrimToCap(next);
            }
        }
        public static int SliderUnits
        {
            get => Cap / Step;
            set => Cap = value * Step;
        }
        public static int MinUnits => Min / Step;
        public static int MaxUnits => Max / Step;
        public static int Snap(int value)
        {
            int clamped = Mathf.Clamp(value, Min, Max);
            return Mathf.RoundToInt(clamped / (float)Step) * Step;
        }
        #endregion
    }
}
