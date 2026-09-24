using ModularChess.Core;

namespace ModularChess.Presentation
{
    public static class ModeDlc
    {
        public static bool IsOwned(ModeId id) => MeritUnlocks.IsModeOwned(id);
        public static void Purchase(ModeId id)
        {
            UnlockProduct? product = ProductFor(id);
            if (product == null) return;
            MeritUnlocks.TryPurchase(product.Value);
        }
        public static void UnlockAll() => MeritUnlocks.UnlockAll();
        public static void ClearAll() => MeritUnlocks.ClearAll();
        static UnlockProduct? ProductFor(ModeId id)
        {
            switch (id)
            {
                case ModeId.FogOfWar: return UnlockProduct.ModeFogOfWar;
                case ModeId.PowerfulPieces: return UnlockProduct.ModePowerfulPieces;
                case ModeId.Martyr: return UnlockProduct.ModeMartyr;
                default: return null;
            }
        }
    }
}
