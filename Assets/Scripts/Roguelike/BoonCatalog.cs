using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public static class BoonCatalog
    {
        #region Fields
        static readonly BoonDefinition[] Pass1Pool =
        {
            new BoonDefinition(
                BoonId.Reinforcements,
                BoonRarity.Grey,
                "roguelike.boon.reinforcements",
                "roguelike.boon.reinforcements.desc",
                pawnCount: 1,
                maxStacks: 3)
        };
        #endregion

        #region Public Methods
        public static IReadOnlyList<BoonDefinition> PlayerPool => Pass1Pool;
        public static BoonDefinition Get(BoonId id, BoonRarity rarity)
        {
            for (int i = 0; i < Pass1Pool.Length; i++)
            {
                BoonDefinition def = Pass1Pool[i];
                if (def.Id == id && def.Rarity == rarity)
                    return def;
            }
            throw new ArgumentException($"No boon {id} at {rarity}.");
        }
        #endregion
    }
}
