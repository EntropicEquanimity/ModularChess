namespace ModularChess.Core
{
    internal sealed class ExtraLifeResolution : ICaptureResolution
    {
        #region Fields
        public int Priority => 100;
        #endregion

        #region Public Methods
        public CaptureResolution Resolve(Board board, Move move, Piece captured, ModeRuntime runtime)
        {
            if (captured == null || !runtime.ExtraLifeAvailable(captured.Id))
            {
                return CaptureResolution.Continue(runtime);
            }
            return CaptureResolution.Negate(runtime.SpendExtraLife(captured.Id));
        }
        #endregion
    }
}
