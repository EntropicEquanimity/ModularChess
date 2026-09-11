using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class ModeDlc
    {
        const string KeyPrefix = "OwnedMode_";

        public static bool IsOwned(ModeId id)
        {
            return PlayerPrefs.GetInt(Key(id), 0) == 1;
        }

        public static void Purchase(ModeId id)
        {
            PlayerPrefs.SetInt(Key(id), 1);
            PlayerPrefs.Save();
        }

        static string Key(ModeId id) => KeyPrefix + (int)id;
    }
}
