using System;
using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class PromotionTests
    {
        [Test]
        public void GeneratesFourPromotionChoices_AndDoesNotAutoPick()
        {
            GameState state = GameState.FromFen("k7/4P3/8/8/8/8/8/4K3 w - - 0 1");
            Assert.AreEqual(4, CountPromotions(state, "e7", "e8"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e7", "e8", PieceType.Queen));
            Assert.IsTrue(MoveTestHelper.Has(state, "e7", "e8", PieceType.Rook));
            Assert.IsTrue(MoveTestHelper.Has(state, "e7", "e8", PieceType.Bishop));
            Assert.IsTrue(MoveTestHelper.Has(state, "e7", "e8", PieceType.Knight));
            Assert.IsFalse(MoveTestHelper.Has(state, "e7", "e8"));
        }

        [Test]
        public void Apply_RequiresPromotionType_AndKeepsPieceId()
        {
            GameState state = GameState.FromFen("k7/4P3/8/8/8/8/8/4K3 w - - 0 1");
            Piece pawn = state.Board.GetPiece(Parse("e7"));
            GameState queen = MoveTestHelper.Play(state, "e7e8q");
            Piece promoted = queen.Board.GetPiece(Parse("e8"));
            Assert.AreEqual(pawn.Id, promoted.Id);
            Assert.AreEqual(PieceType.Queen, promoted.Type);
            Assert.AreEqual(Side.White, promoted.Side);
            Assert.AreEqual(MoveKind.Promotion, queen.History[0].Kind);
            Assert.AreEqual(PieceType.Queen, queen.History[0].PromotionType);
            Assert.AreEqual(Side.Black, queen.SideToMove);
        }

        [Test]
        public void CapturePromotion_SetsCapturedType()
        {
            GameState state = GameState.FromFen("3qk3/4P3/8/8/8/8/8/4K3 w - - 0 1");
            Move move = MoveTestHelper.Require(state, "e7", "d8", PieceType.Knight);
            Assert.AreEqual(MoveKind.Promotion, move.Kind);
            Assert.AreEqual(PieceType.Queen, move.CapturedType);
            GameState next = state.Apply(move);
            Assert.AreEqual(PieceType.Knight, next.Board.GetPiece(Parse("d8")).Type);
            Assert.IsNull(next.Board.GetPiece(Parse("e7")));
        }

        [Test]
        public void Apply_RejectsQuietMoveOntoPromotionRank()
        {
            GameState state = GameState.FromFen("k7/4P3/8/8/8/8/8/4K3 w - - 0 1");
            Move quiet = new Move(Parse("e7"), Parse("e8"), MoveKind.Quiet);
            Assert.Throws<InvalidOperationException>(() => state.Apply(quiet));
        }

        private static int CountPromotions(GameState state, string from, string to)
        {
            int count = 0;
            Square fromSquare = Parse(from);
            Square toSquare = Parse(to);
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                if (move.From.Equals(fromSquare) && move.To.Equals(toSquare) && move.Kind == MoveKind.Promotion)
                {
                    count++;
                }
            }

            return count;
        }

        private static Square Parse(string algebraic)
        {
            Assert.IsTrue(Square.TryParse(algebraic, out Square square));
            return square;
        }
    }
}
