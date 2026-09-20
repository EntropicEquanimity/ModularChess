using ModularChess.Core;

namespace ModularChess.Match
{
    public static class Autoplay
    {
        #region Fields
        public static bool Active { get; private set; }
        public static AiStrength Strength { get; private set; } = AiStrength.Easy;
        #endregion

        #region Public Methods
        public static void Start()
        {
            Active = true;
        }
        public static void Stop()
        {
            Active = false;
        }
        public static void SetStrength(AiStrength strength)
        {
            Strength = strength;
        }
        public static void ClearOnLeave()
        {
            Active = false;
        }
        #endregion
    }
}
