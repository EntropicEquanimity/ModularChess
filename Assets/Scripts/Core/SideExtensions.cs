using System;

namespace ModularChess.Core
{
    public static class SideExtensions
    {
        #region Public Methods
        public static Side Opponent(this Side side)
        {
            switch (side)
            {
                case Side.White:
                    return Side.Black;
                case Side.Black:
                    return Side.White;
                default:
                    throw new ArgumentOutOfRangeException(nameof(side), side, null);
            }
        }
        #endregion
    }
}
