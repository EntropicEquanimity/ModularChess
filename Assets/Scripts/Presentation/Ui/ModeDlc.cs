using ModularChess.Core;

namespace ModularChess.Presentation
{
    public static class ModeDlc
    {
        public static bool IsOwned(ModeId id) => MeritUnlocks.IsModeOwned(id);
        public static void Purchase(ModeId id)
        {
            UnlockProduct? product = MeritUnlocks.FromMode(id);
            if (product == null) return;
            MeritUnlocks.TryPurchase(product.Value);
        }
        public static void UnlockAll() => MeritUnlocks.UnlockAll();
        public static void ClearAll() => MeritUnlocks.ClearAll();
    }
}
