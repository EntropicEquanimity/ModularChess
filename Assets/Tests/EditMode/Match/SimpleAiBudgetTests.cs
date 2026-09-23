using System.Diagnostics;
using ModularChess.Core;
using ModularChess.Match;
using NUnit.Framework;

namespace ModularChess.Match.Tests
{
    public class SimpleAiBudgetTests
    {
        [Test]
        public void HardChoose_StartingFog_FinishesUnderBudget()
        {
            VersusRules rules = new VersusRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
            GameState state = GameState.StartingPosition(rules);
            AssertUnderBudget(state, AiStrength.Hard, Side.White, 200);
        }

        [Test]
        public void HardChoose_BusyMidgameFog_FinishesUnderBudget()
        {
            VersusRules rules = new VersusRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
            GameState state = GameState.StartingPosition(rules);
            state = Apply(state, "e2e4");
            state = Apply(state, "e7e5");
            state = Apply(state, "g1f3");
            state = Apply(state, "b8c6");
            state = Apply(state, "f1c4");
            state = Apply(state, "g8f6");
            state = Apply(state, "d2d4");
            state = Apply(state, "e5d4");
            AssertUnderBudget(state, AiStrength.Hard, state.SideToMove, 200);
        }

        static void AssertUnderBudget(GameState state, AiStrength strength, Side side, int maxMs)
        {
            var watch = Stopwatch.StartNew();
            Move? move = SimpleAi.Choose(state, strength, side);
            watch.Stop();
            Assert.IsNotNull(move);
            Assert.LessOrEqual(watch.ElapsedMilliseconds, maxMs, $"Choose took {watch.ElapsedMilliseconds}ms");
        }

        static GameState Apply(GameState state, string uci)
        {
            Assert.IsTrue(Square.TryParse(uci.Substring(0, 2), out Square from));
            Assert.IsTrue(Square.TryParse(uci.Substring(2, 2), out Square to));
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                if (move.From.Equals(from) && move.To.Equals(to))
                    return state.Apply(move);
            }
            Assert.Fail($"Move {uci} not legal");
            return state;
        }
    }
}
