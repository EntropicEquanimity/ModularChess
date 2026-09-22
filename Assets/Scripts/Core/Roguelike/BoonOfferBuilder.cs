using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public static class BoonOfferBuilder
    {
        #region Fields
        public const int OfferSlots = 3;
        #endregion

        #region Public Methods
        public static IReadOnlyList<BoonDefinition> Build(
            IReadOnlyList<BoonDefinition> pool,
            IReadOnlyDictionary<BoonId, int> ownedStacks,
            int stageNumber,
            Random rng)
        {
            if (pool == null || pool.Count == 0)
                return Array.Empty<BoonDefinition>();
            var available = new List<BoonDefinition>(pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                BoonDefinition def = pool[i];
                int owned = 0;
                if (ownedStacks != null)
                    ownedStacks.TryGetValue(def.Id, out owned);
                if (owned >= def.MaxStacks)
                    continue;
                available.Add(def);
            }
            if (available.Count == 0)
                return Array.Empty<BoonDefinition>();
            int want = OfferSlots;
            if (available.Count < want)
                want = available.Count;
            var offer = new List<BoonDefinition>(want);
            var bag = new List<BoonDefinition>(available);
            for (int i = 0; i < want; i++)
            {
                int pick = rng.Next(bag.Count);
                offer.Add(bag[pick]);
                bag.RemoveAt(pick);
            }
            return offer;
        }
        #endregion
    }
}
