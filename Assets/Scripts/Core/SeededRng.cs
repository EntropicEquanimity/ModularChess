namespace ModularChess.Core
{
    internal sealed class SeededRng
    {
        #region Fields
        uint _state;
        #endregion

        #region Public Methods
        public SeededRng(int seed)
        {
            _state = seed == 0 ? 1u : (uint)seed;
        }
        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 1)
                return 0;
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return (int)(_state % (uint)maxExclusive);
        }
        public void Shuffle<T>(T[] items)
        {
            if (items == null)
                return;
            for (int i = items.Length - 1; i > 0; i--)
            {
                int j = Next(i + 1);
                T swap = items[i];
                items[i] = items[j];
                items[j] = swap;
            }
        }
        #endregion
    }
}
