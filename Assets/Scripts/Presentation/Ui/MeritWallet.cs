using System;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class MeritWallet
    {
        #region Fields
        const string BalanceKey = "MeritBalance";
        const string CheckmateGrantsKey = "MeritCheckmateGrants";
        const string DailyStampKey = "MeritDailyStamp";
        const string DailyCountKey = "MeritDailyCount";
        public const int DailyVersusCap = 3;
        #endregion

        #region Public Methods
        public static int Balance => Mathf.Max(0, PlayerPrefs.GetInt(BalanceKey, 0));
        public static int CheckmateGrantsUsed => Mathf.Clamp(PlayerPrefs.GetInt(CheckmateGrantsKey, 0), 0, 3);
        public static event Action Changed;
        public static void Add(int amount)
        {
            if (amount <= 0) return;
            PlayerPrefs.SetInt(BalanceKey, Balance + amount);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        public static bool TrySpend(int amount)
        {
            if (amount <= 0) return true;
            if (Balance < amount) return false;
            PlayerPrefs.SetInt(BalanceKey, Balance - amount);
            PlayerPrefs.Save();
            Changed?.Invoke();
            return true;
        }
        public static void GrantVersusFinish(bool checkmate)
        {
            if (checkmate && CheckmateGrantsUsed < 3)
            {
                int grant = 3 - CheckmateGrantsUsed;
                PlayerPrefs.SetInt(CheckmateGrantsKey, CheckmateGrantsUsed + 1);
                PlayerPrefs.SetInt(BalanceKey, Balance + grant);
                PlayerPrefs.Save();
                Changed?.Invoke();
                return;
            }
            if (!TryConsumeDailySlot()) return;
            PlayerPrefs.SetInt(BalanceKey, Balance + 1);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        public static void Clear()
        {
            PlayerPrefs.DeleteKey(BalanceKey);
            PlayerPrefs.DeleteKey(CheckmateGrantsKey);
            PlayerPrefs.DeleteKey(DailyStampKey);
            PlayerPrefs.DeleteKey(DailyCountKey);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        public static void DebugFill(int amount)
        {
            PlayerPrefs.SetInt(BalanceKey, Mathf.Max(0, amount));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        #endregion

        #region Private Methods
        static bool TryConsumeDailySlot()
        {
            string today = DateTime.UtcNow.ToString("yyyyMMdd");
            string stamp = PlayerPrefs.GetString(DailyStampKey, string.Empty);
            int count = PlayerPrefs.GetInt(DailyCountKey, 0);
            if (stamp != today)
            {
                stamp = today;
                count = 0;
            }
            if (count >= DailyVersusCap) return false;
            PlayerPrefs.SetString(DailyStampKey, stamp);
            PlayerPrefs.SetInt(DailyCountKey, count + 1);
            return true;
        }
        #endregion
    }
}
