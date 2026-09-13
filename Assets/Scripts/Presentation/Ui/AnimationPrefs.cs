using UnityEngine;

namespace ModularChess.Presentation
{
    public static class AnimationPrefs
    {
        public const string Key = "AnimSpeed";

        public static float SliderValue
        {
            get => Mathf.Clamp01(PlayerPrefs.GetFloat(Key, 0f));
            set
            {
                PlayerPrefs.SetFloat(Key, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static bool Instant => SliderValue >= 0.999f;

        public static float MoveDuration(float baseSeconds)
        {
            if (Instant || baseSeconds <= 0f)
                return 0f;

            float t = SliderValue;
            if (t <= 0.25f)
                return baseSeconds / Mathf.Lerp(1f, 1.5f, t / 0.25f);
            if (t <= 0.5f)
                return baseSeconds / Mathf.Lerp(1.5f, 2f, (t - 0.25f) / 0.25f);
            if (t <= 0.75f)
                return baseSeconds / Mathf.Lerp(2f, 3f, (t - 0.5f) / 0.25f);
            return Mathf.Lerp(baseSeconds / 3f, 0f, (t - 0.75f) / 0.25f);
        }

        public static string SpeedLabel
        {
            get
            {
                if (Instant)
                    return "Off";

                float t = SliderValue;
                float mul;
                if (t <= 0.25f)
                    mul = Mathf.Lerp(1f, 1.5f, t / 0.25f);
                else if (t <= 0.5f)
                    mul = Mathf.Lerp(1.5f, 2f, (t - 0.25f) / 0.25f);
                else if (t <= 0.75f)
                    mul = Mathf.Lerp(2f, 3f, (t - 0.5f) / 0.25f);
                else
                {
                    float duration = MoveDuration(1f);
                    if (duration <= 0.001f)
                        return "Off";
                    mul = 1f / duration;
                }

                return mul % 1f < 0.05f ? $"{mul:0}x" : $"{mul:0.#}x";
            }
        }
    }
}
