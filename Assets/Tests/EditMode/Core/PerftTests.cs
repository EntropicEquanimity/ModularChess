using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class PerftTests
    {
        [Test]
        public void StartingPosition_Perft_MatchesKnownCounts()
        {
            GameState start = GameState.StartingPosition();
            Assert.AreEqual(20, Perft(start, 1));
            Assert.AreEqual(400, Perft(start, 2));
            Assert.AreEqual(8902, Perft(start, 3));
        }

        private static long Perft(GameState state, int depth)
        {
            if (depth <= 0)
            {
                return 1;
            }

            long nodes = 0;
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                GameState next = state.Apply(state.LegalMoves[i]);
                nodes += depth == 1 ? 1 : Perft(next, depth - 1);
            }

            return nodes;
        }
    }
}
