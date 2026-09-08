using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class CastlingTests
    {
        [Test]
        public void KingSide_IsLegal_WhenPathClearAndUnmoved()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K2R w K - 0 1");
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "g1"));
            GameState next = MoveTestHelper.Play(state, "e1g1");
            Assert.AreEqual(MoveKind.CastleKingSide, next.History[0].Kind);
            Assert.AreEqual(PieceType.King, next.Board.GetPiece(Parse("g1")).Type);
            Assert.AreEqual(PieceType.Rook, next.Board.GetPiece(Parse("f1")).Type);
            Assert.IsNull(next.Board.GetPiece(Parse("e1")));
            Assert.IsNull(next.Board.GetPiece(Parse("h1")));
            Assert.IsFalse(next.CastlingRights.HasKingSide(Side.White));
            Assert.AreEqual(Side.Black, next.SideToMove);
        }

        [Test]
        public void QueenSide_IsLegal_WhenPathClearAndUnmoved()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/R3K3 w Q - 0 1");
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "c1"));
            GameState next = MoveTestHelper.Play(state, "e1c1");
            Assert.AreEqual(MoveKind.CastleQueenSide, next.History[0].Kind);
            Assert.AreEqual(PieceType.King, next.Board.GetPiece(Parse("c1")).Type);
            Assert.AreEqual(PieceType.Rook, next.Board.GetPiece(Parse("d1")).Type);
            Assert.IsNull(next.Board.GetPiece(Parse("a1")));
        }

        [Test]
        public void Illegal_WhenPathBlocked()
        {
            GameState kingSide = GameState.FromFen("4k3/8/8/8/8/8/8/4KB1R w K - 0 1");
            Assert.IsFalse(MoveTestHelper.Has(kingSide, "e1", "g1"));
            GameState queenSide = GameState.FromFen("4k3/8/8/8/8/8/8/RN2K3 w Q - 0 1");
            Assert.IsFalse(MoveTestHelper.Has(queenSide, "e1", "c1"));
        }

        [Test]
        public void Illegal_WhenKingInCheck()
        {
            GameState state = GameState.FromFen("4k3/4r3/8/8/8/8/8/4K2R w K - 0 1");
            Assert.IsTrue(state.IsInCheck);
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "g1"));
        }

        [Test]
        public void Illegal_WhenKingPassesThroughCheck()
        {
            GameState state = GameState.FromFen("4kr2/8/8/8/8/8/8/4K2R w K - 0 1");
            Assert.IsFalse(state.IsInCheck);
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "g1"));
        }

        [Test]
        public void Illegal_WhenKingLandsInCheck()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/6r1/4K2R w K - 0 1");
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "g1"));
        }

        [Test]
        public void Illegal_WhenRightsLost()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K2R w - - 0 1");
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "g1"));
        }

        [Test]
        public void QueenSide_AllowsAttackedBFile()
        {
            GameState state = GameState.FromFen("1r2k3/8/8/8/8/8/8/R3K3 w Q - 0 1");
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "c1"));
        }

        [Test]
        public void BothSides_CanCastleFromOpenBackRanks()
        {
            GameState state = GameState.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "g1"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e1", "c1"));
            GameState blackToMove = MoveTestHelper.Play(state, "e1g1");
            Assert.IsTrue(MoveTestHelper.Has(blackToMove, "e8", "g8"));
            Assert.IsTrue(MoveTestHelper.Has(blackToMove, "e8", "c8"));
        }

        private static Square Parse(string algebraic)
        {
            Assert.IsTrue(Square.TryParse(algebraic, out Square square));
            return square;
        }
    }
}
