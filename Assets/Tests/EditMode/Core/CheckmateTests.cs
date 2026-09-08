using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class CheckmateTests
    {
        [Test]
        public void FoolsMate_IsCheckmate()
        {
            GameState state = MoveTestHelper.Play(GameState.StartingPosition(), "f2f3", "e7e5", "g2g4", "d8h4");
            Assert.AreEqual(GameStatus.Checkmate, state.Status);
            Assert.AreEqual(Side.White, state.SideToMove);
            Assert.IsTrue(state.IsInCheck);
            Assert.AreEqual(0, state.LegalMoves.Count);
        }

        [Test]
        public void ScholarsMate_IsCheckmate()
        {
            GameState state = MoveTestHelper.Play(
                GameState.StartingPosition(),
                "e2e4",
                "e7e5",
                "d1h5",
                "b8c6",
                "f1c4",
                "g8f6",
                "h5f7");

            Assert.AreEqual(GameStatus.Checkmate, state.Status);
            Assert.IsTrue(state.IsInCheck);
            Assert.AreEqual(0, state.LegalMoves.Count);
            Assert.AreEqual(PieceType.Queen, state.Board.GetPiece(Parse("f7")).Type);
            Assert.AreEqual(Side.Black, state.SideToMove);
        }

        private static Square Parse(string algebraic)
        {
            Assert.IsTrue(Square.TryParse(algebraic, out Square square));
            return square;
        }
    }
}
