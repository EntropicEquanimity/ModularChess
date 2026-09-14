using System.Text;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class PlayerIdentity
    {
        #region Fields
        public const string PrefsKey = "PlayerName";
        public const int StemMax = 12;
        public static string DisplayName => PlayerPrefs.GetString(PrefsKey, string.Empty);
        public static bool HasName => DisplayName.Length > 0;
        #endregion

        #region Public Methods
        /// <summary>Writes a stem of up to 12 English letters or digits plus four generated digits once.</summary>
        public static bool TryCommit(string stem)
        {
            if (HasName)
            {
                return false;
            }

            string clean = Sanitize(stem);
            if (clean.Length == 0)
            {
                return false;
            }

            string suffix = Random.Range(0, 10000).ToString("D4");
            PlayerPrefs.SetString(PrefsKey, clean + suffix);
            PlayerPrefs.Save();
            return true;
        }

        public static string Sanitize(string stem)
        {
            if (string.IsNullOrEmpty(stem))
            {
                return string.Empty;
            }

            var builder = new StringBuilder(StemMax);
            for (int i = 0; i < stem.Length && builder.Length < StemMax; i++)
            {
                char c = stem[i];
                if (IsEnglishAlphanumeric(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }
        #endregion

        #region Private Methods
        static bool IsEnglishAlphanumeric(char c)
        {
            return c >= 'A' && c <= 'Z'
                || c >= 'a' && c <= 'z'
                || c >= '0' && c <= '9';
        }
        #endregion
    }
}
