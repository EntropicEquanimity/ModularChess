using System;
using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class StartingPositionTests
    {
        [Test]
        public void White_Has_20_LegalMoves()
        {
            GameState state = GameState.StartingPosition();
            Assert.AreEqual(Side.White, state.SideToMove);
            Assert.AreEqual(GameStatus.InProgress, state.Status);
            Assert.IsFalse(state.IsInCheck);
            Assert.AreEqual(20, state.LegalMoves.Count);
            Assert.AreEqual(0, state.History.Count);
        }

        [Test]
        public void Black_Has_20_LegalMoves_After_e4()
        {
            GameState state = MoveTestHelper.Play(GameState.StartingPosition(), "e2e4");
            Assert.AreEqual(Side.Black, state.SideToMove);
            Assert.AreEqual(20, state.LegalMoves.Count);
            Assert.AreEqual("e3", state.EnPassantTarget.Value.ToString());
        }

        [Test]
        public void Apply_DoesNotMutateOriginal()
        {
            GameState original = GameState.StartingPosition();
            Square e2 = Parse("e2");
            Square e4 = Parse("e4");
            Piece pawn = original.Board.GetPiece(e2);
            GameState next = MoveTestHelper.Play(original, "e2e4");

            Assert.AreSame(pawn, original.Board.GetPiece(e2));
            Assert.IsNull(original.Board.GetPiece(e4));
            Assert.AreEqual(20, original.LegalMoves.Count);
            Assert.AreEqual(Side.White, original.SideToMove);
            Assert.AreEqual(pawn.Id, next.Board.GetPiece(e4).Id);
            Assert.IsTrue(next.Board.GetPiece(e4).HasMoved);
        }

        [Test]
        public void Apply_Throws_WhenMoveIsIllegal()
        {
            GameState state = GameState.StartingPosition();
            Move illegal = new Move(Parse("e2"), Parse("e5"), MoveKind.Quiet);
            Assert.Throws<InvalidOperationException>(() => state.Apply(illegal));
        }

        [Test]
        public void StandardFen_RoundTrips()
        {
            GameState state = GameState.StartingPosition();
            Assert.AreEqual(Fen.StartingPosition, state.ToFen());
        }

        private static Square Parse(string algebraic)
        {
            Assert.IsTrue(Square.TryParse(algebraic, out Square square));
            return square;
        }
    }
}
