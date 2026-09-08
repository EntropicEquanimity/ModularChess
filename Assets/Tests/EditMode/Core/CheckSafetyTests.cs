using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class CheckSafetyTests
    {
        [Test]
        public void King_CannotMoveAdjacentToEnemyKing()
        {
            GameState state = GameState.FromFen("8/8/8/8/8/4k3/8/4K3 w - - 0 1");
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "d1"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "f1"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "d2"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "e2"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "f2"));
        }

        [Test]
        public void PinnedPiece_CannotExposeKing()
        {
            GameState state = GameState.FromFen("4k3/4r3/8/8/8/8/4N3/4K3 w - - 0 1");
            Assert.IsFalse(state.IsInCheck);
            Assert.AreEqual(0, state.LegalMovesFrom(Parse("e2")).Count);
        }

        [Test]
        public void MustEscapeCheck_CannotIgnoreAttacker()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/r3K3 w - - 0 1");
            Assert.IsTrue(state.IsInCheck);
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "d1"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "f1"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "d2"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "e2"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "f2"));
        }

        [Test]
        public void CapturingOwnKingAttacker_IsLegalWhenSafe()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/4r3/4K3 w - - 0 1");
            Assert.IsTrue(state.IsInCheck);
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "e2"));
            GameState next = MoveTestHelper.Play(state, "e1e2");
            Assert.IsFalse(next.IsInCheck);
            Assert.AreEqual(PieceType.King, next.Board.GetPiece(Parse("e2")).Type);
        }

        private static Square Parse(string algebraic)
        {
            Assert.IsTrue(Square.TryParse(algebraic, out Square square));
            return square;
        }
    }
}
