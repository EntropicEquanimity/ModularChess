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

        public static void UnlockAll()
        {
            ModeDefinition[] modes = ModeCatalog.All;
            for (int i = 0; i < modes.Length; i++)
                PlayerPrefs.SetInt(Key(modes[i].Id), 1);
            PlayerPrefs.Save();
        }

        public static void ClearAll()
        {
            ModeDefinition[] modes = ModeCatalog.All;
            for (int i = 0; i < modes.Length; i++)
                PlayerPrefs.DeleteKey(Key(modes[i].Id));
            PlayerPrefs.Save();
        }

        static string Key(ModeId id) => KeyPrefix + (int)id;
    }
}
