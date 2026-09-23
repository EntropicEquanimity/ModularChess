namespace ModularChess.Core
{
    internal sealed class FogVisionHook : IVisionHook
    {
        #region Public Methods
        public VisionMap Compute(GameState state, Side viewer) { return FogVision.Compute(state, viewer); }
        #endregion
    }
}
