using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class ActivityDlc
    {
        const string KeyPrefix = "OwnedActivity_";

        public static bool IsOwned(Activity id)
        {
            return PlayerPrefs.GetInt(Key(id), 0) == 1;
        }

        public static void Purchase(Activity id)
        {
            PlayerPrefs.SetInt(Key(id), 1);
            PlayerPrefs.Save();
        }

        public static void UnlockAll()
        {
            PlayerPrefs.SetInt(Key(Activity.Roguelike), 1);
            PlayerPrefs.Save();
        }

        public static void ClearAll()
        {
            PlayerPrefs.DeleteKey(Key(Activity.Roguelike));
            PlayerPrefs.Save();
        }

        static string Key(Activity id) => KeyPrefix + (int)id;
    }
}
