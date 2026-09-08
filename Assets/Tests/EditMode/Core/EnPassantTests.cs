using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class EnPassantTests
    {
        [Test]
        public void Capture_RemovesThePassedPawn()
        {
            GameState state = GameState.FromFen("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
            Assert.IsTrue(MoveTestHelper.Has(state, "e5", "d6"));
            Move move = MoveTestHelper.Require(state, "e5", "d6");
            Assert.AreEqual(MoveKind.EnPassant, move.Kind);
            Assert.AreEqual(PieceType.Pawn, move.CapturedType);

            GameState next = state.Apply(move);
            Assert.AreEqual(PieceType.Pawn, next.Board.GetPiece(Parse("d6")).Type);
            Assert.AreEqual(Side.White, next.Board.GetPiece(Parse("d6")).Side);
            Assert.IsNull(next.Board.GetPiece(Parse("d5")));
            Assert.IsNull(next.Board.GetPiece(Parse("e5")));
            Assert.IsNull(next.EnPassantTarget);
            Assert.AreEqual(Side.Black, next.SideToMove);
        }

        [Test]
        public void Available_OnlyOnTheImmediateReply()
        {
            GameState afterDouble = MoveTestHelper.Play(GameState.StartingPosition(), "e2e4", "d7d5", "e4e5", "f7f5");
            Assert.AreEqual("f6", afterDouble.EnPassantTarget.Value.ToString());
            Assert.IsTrue(MoveTestHelper.Has(afterDouble, "e5", "f6"));

            GameState missed = MoveTestHelper.Play(afterDouble, "g1f3");
            Assert.IsNull(missed.EnPassantTarget);
            Assert.IsFalse(MoveTestHelper.Has(missed, "e5", "f6"));
        }

        [Test]
        public void FromStartingSequence_e4_d5_e5_f5_exf6()
        {
            GameState state = MoveTestHelper.Play(GameState.StartingPosition(), "e2e4", "d7d5", "e4e5", "f7f5", "e5f6");
            Assert.AreEqual(MoveKind.EnPassant, state.History[state.History.Count - 1].Kind);
            Assert.IsNull(state.Board.GetPiece(Parse("f5")));
        }

        private static Square Parse(string algebraic)
        {
            Assert.IsTrue(Square.TryParse(algebraic, out Square square));
            return square;
        }
    }
}
