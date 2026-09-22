using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public static class ShopOfferBuilder
    {
        #region Fields
        static readonly PieceType[] Catalog =
        {
            PieceType.Pawn,
            PieceType.Knight,
            PieceType.Bishop,
            PieceType.Rook,
            PieceType.Queen
        };
        #endregion

        #region Public Methods
        public static IReadOnlyList<ShopItem> Build(Random rng, int slots = RoguelikeBalance.ShopSlotCount)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            if (slots < 0)
                slots = 0;
            var items = new List<ShopItem>(slots);
            for (int i = 0; i < slots; i++)
            {
                PieceType type = Catalog[rng.Next(Catalog.Length)];
                items.Add(new ShopItem(type, RoguelikeBalance.ShopPrice(type, rng)));
            }
            return items;
        }
        #endregion
    }
}
