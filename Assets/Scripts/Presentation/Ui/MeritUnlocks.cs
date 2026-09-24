using System;
using ModularChess.Core;
using ModularChess.Match;
using UnityEngine;

namespace ModularChess.Presentation
{
    public static class MeritUnlocks
    {
        #region Fields
        const string ModeKeyPrefix = "OwnedMode_";
        const string HistoryTierKey = "HistoryTier";
        const string NotationKey = "UnlockShowNotation";
        static readonly UnlockProduct[] Catalog =
        {
            UnlockProduct.ModeFogOfWar,
            UnlockProduct.ModePowerfulPieces,
            UnlockProduct.ModeMartyr,
            UnlockProduct.HistoryTier1,
            UnlockProduct.HistoryTier2,
            UnlockProduct.HistoryTier3,
            UnlockProduct.HistoryTier4,
            UnlockProduct.ShowNotation
        };
        #endregion

        #region Public Methods
        public static UnlockProduct[] All => Catalog;
        public static int Cost(UnlockProduct product)
        {
            switch (product)
            {
                case UnlockProduct.ModeFogOfWar: return 6;
                case UnlockProduct.ModePowerfulPieces: return 12;
                case UnlockProduct.ModeMartyr: return 15;
                case UnlockProduct.HistoryTier1: return 1;
                case UnlockProduct.HistoryTier2: return 3;
                case UnlockProduct.HistoryTier3: return 5;
                case UnlockProduct.HistoryTier4: return 10;
                case UnlockProduct.ShowNotation: return 1;
                default: throw new ArgumentOutOfRangeException(nameof(product), product, null);
            }
        }
        public static bool IsOwned(UnlockProduct product)
        {
            switch (product)
            {
                case UnlockProduct.ModeFogOfWar:
                    return IsModeOwned(ModeId.FogOfWar);
                case UnlockProduct.ModePowerfulPieces:
                    return IsModeOwned(ModeId.PowerfulPieces);
                case UnlockProduct.ModeMartyr:
                    return IsModeOwned(ModeId.Martyr);
                case UnlockProduct.HistoryTier1:
                    return HistoryPrefs.Tier >= 1;
                case UnlockProduct.HistoryTier2:
                    return HistoryPrefs.Tier >= 2;
                case UnlockProduct.HistoryTier3:
                    return HistoryPrefs.Tier >= 3;
                case UnlockProduct.HistoryTier4:
                    return HistoryPrefs.Tier >= 4;
                case UnlockProduct.ShowNotation:
                    return PlayerPrefs.GetInt(NotationKey, 0) == 1;
                default:
                    throw new ArgumentOutOfRangeException(nameof(product), product, null);
            }
        }
        public static bool IsModeOwned(ModeId id) => PlayerPrefs.GetInt(ModeKey(id), 0) == 1;
        public static bool ShowNotation => IsOwned(UnlockProduct.ShowNotation);
        public static bool CanPurchase(UnlockProduct product)
        {
            if (IsOwned(product)) return false;
            if (!PrerequisiteMet(product)) return false;
            return MeritWallet.Balance >= Cost(product);
        }
        public static bool TryPurchase(UnlockProduct product)
        {
            if (IsOwned(product) || !PrerequisiteMet(product)) return false;
            if (!MeritWallet.TrySpend(Cost(product))) return false;
            Grant(product);
            return true;
        }
        public static ModeId? AsMode(UnlockProduct product)
        {
            switch (product)
            {
                case UnlockProduct.ModeFogOfWar: return ModeId.FogOfWar;
                case UnlockProduct.ModePowerfulPieces: return ModeId.PowerfulPieces;
                case UnlockProduct.ModeMartyr: return ModeId.Martyr;
                default: return null;
            }
        }
        public static void UnlockAll()
        {
            ModeDefinition[] modes = ModeCatalog.All;
            for (int i = 0; i < modes.Length; i++)
                PlayerPrefs.SetInt(ModeKey(modes[i].Id), 1);
            HistoryPrefs.Tier = 4;
            PlayerPrefs.SetInt(NotationKey, 1);
            PlayerPrefs.Save();
        }
        public static void ClearAll()
        {
            ModeDefinition[] modes = ModeCatalog.All;
            for (int i = 0; i < modes.Length; i++)
                PlayerPrefs.DeleteKey(ModeKey(modes[i].Id));
            HistoryPrefs.Tier = 0;
            PlayerPrefs.DeleteKey(NotationKey);
            PlayerPrefs.Save();
        }
        #endregion

        #region Private Methods
        static bool PrerequisiteMet(UnlockProduct product)
        {
            switch (product)
            {
                case UnlockProduct.HistoryTier2: return HistoryPrefs.Tier >= 1;
                case UnlockProduct.HistoryTier3: return HistoryPrefs.Tier >= 2;
                case UnlockProduct.HistoryTier4: return HistoryPrefs.Tier >= 3;
                default: return true;
            }
        }
        static void Grant(UnlockProduct product)
        {
            switch (product)
            {
                case UnlockProduct.ModeFogOfWar:
                    PlayerPrefs.SetInt(ModeKey(ModeId.FogOfWar), 1);
                    break;
                case UnlockProduct.ModePowerfulPieces:
                    PlayerPrefs.SetInt(ModeKey(ModeId.PowerfulPieces), 1);
                    break;
                case UnlockProduct.ModeMartyr:
                    PlayerPrefs.SetInt(ModeKey(ModeId.Martyr), 1);
                    break;
                case UnlockProduct.HistoryTier1:
                    HistoryPrefs.Tier = 1;
                    break;
                case UnlockProduct.HistoryTier2:
                    HistoryPrefs.Tier = 2;
                    break;
                case UnlockProduct.HistoryTier3:
                    HistoryPrefs.Tier = 3;
                    break;
                case UnlockProduct.HistoryTier4:
                    HistoryPrefs.Tier = 4;
                    break;
                case UnlockProduct.ShowNotation:
                    PlayerPrefs.SetInt(NotationKey, 1);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(product), product, null);
            }
            PlayerPrefs.Save();
        }
        static string ModeKey(ModeId id) => ModeKeyPrefix + (int)id;
        #endregion
    }
}
