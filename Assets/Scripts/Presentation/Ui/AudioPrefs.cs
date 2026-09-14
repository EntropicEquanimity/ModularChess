using UnityEngine;

namespace ModularChess.Presentation
{
    public static class AudioPrefs
    {
        #region Fields
        public const string MusicKey = "MusicVolume";
        public const string SfxKey = "SfxVolume";
        public const int Min = 0;
        public const int Max = 100;
        const float MuteDb = -80f;
        public static int Music
        {
            get => Read(MusicKey);
            set => Write(MusicKey, value);
        }
        public static int Sfx
        {
            get => Read(SfxKey);
            set => Write(SfxKey, value);
        }
        public static float MusicDb => ToDb(Music);
        public static float SfxDb => ToDb(Sfx);
        public static float MusicLinear => ToLinear(Music);
        public static float SfxLinear => ToLinear(Sfx);
        #endregion

        #region Public Methods
        public static float ToDb(int slider)
        {
            if (slider <= Min)
            {
                return MuteDb;
            }

            return Mathf.Lerp(MuteDb, 0f, Mathf.Clamp01(slider / (float)Max));
        }

        public static float ToLinear(int slider)
        {
            if (slider <= Min)
            {
                return 0f;
            }

            return Mathf.Pow(10f, ToDb(slider) / 20f);
        }
        #endregion

        #region Private Methods
        static int Read(string key)
        {
            return Mathf.Clamp(PlayerPrefs.GetInt(key, Max), Min, Max);
        }

        static void Write(string key, int value)
        {
            PlayerPrefs.SetInt(key, Mathf.Clamp(value, Min, Max));
            PlayerPrefs.Save();
            GameAudio.ApplyVolumes();
        }
        #endregion
    }
}
