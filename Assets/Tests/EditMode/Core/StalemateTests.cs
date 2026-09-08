using System;
using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class StalemateTests
    {
        [Test]
        public void KingAndQueen_StalemateFixture()
        {
            GameState state = GameState.FromFen("7k/5Q2/8/8/8/8/8/7K b - - 0 1");
            Assert.IsFalse(state.IsInCheck);
            Assert.AreEqual(0, state.LegalMoves.Count);
            Assert.AreEqual(GameStatus.Stalemate, state.Status);
            Assert.AreEqual(Side.Black, state.SideToMove);
        }

        [Test]
        public void Apply_Throws_WhenGameIsOver()
        {
            GameState state = GameState.FromFen("7k/5Q2/8/8/8/8/8/7K b - - 0 1");
            Move any = new Move(Parse("h8"), Parse("h7"), MoveKind.Quiet);
            Assert.Throws<InvalidOperationException>(() => state.Apply(any));
        }

        private static Square Parse(string algebraic)
        {
            Assert.IsTrue(Square.TryParse(algebraic, out Square square));
            return square;
        }
    }
}
