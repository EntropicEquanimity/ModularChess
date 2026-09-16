using UnityEngine;

namespace ModularChess.Presentation
{
    public static class UiAnimPrefs
    {
        #region Fields
        public const string Key = "UiAnimSpeed";
        public const float MinMultiplier = 1f;
        public const float MaxMultiplier = 5f;
        public const float SkipStep = 6f;
        public static float SliderValue
        {
            get
            {
                float stored = PlayerPrefs.GetFloat(Key, MinMultiplier);
                return Mathf.Clamp(stored, MinMultiplier, SkipStep);
            }
            set
            {
                PlayerPrefs.SetFloat(Key, Mathf.Clamp(value, MinMultiplier, SkipStep));
                PlayerPrefs.Save();
            }
        }
        public static bool Instant => SliderValue >= SkipStep - 0.01f;
        public static float Multiplier
        {
            get
            {
                if (Instant)
                {
                    return 0f;
                }

                return Mathf.Clamp(SliderValue, MinMultiplier, MaxMultiplier);
            }
        }
        #endregion

        #region Public Methods
        public static float MoveDuration(float baseSeconds)
        {
            if (Instant || baseSeconds <= 0f || Multiplier <= 0f)
            {
                return 0f;
            }

            return baseSeconds / Multiplier;
        }
        #endregion
    }
}
