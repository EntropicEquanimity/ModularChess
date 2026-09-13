using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class PowerfulPiecesTests
    {
        [Test]
        public void EmpoweredQueen_HasKnightLeap()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
            GameState state = GameState.StartingPosition(rules);
            Piece queen = state.Board.GetPiece(new Square(3, 0));
            Piece knight = state.Board.GetPiece(new Square(1, 0));
            state = state.ConfirmEmpowered(new[] { queen.Id, knight.Id });
            Assert.IsTrue(MoveTestHelper.Has(state, "d1", "c3"));
            Assert.IsTrue(MoveTestHelper.Has(state, "d1", "e3"));
        }

        [Test]
        public void EmpoweredKing_KeepsTurnOpen()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K2R w - - 0 1", rules);
            Piece king = state.Board.GetPiece(new Square(4, 0));
            state = state.ConfirmEmpowered(new[] { king.Id });
            state = MoveTestHelper.Play(state, "e1e2");
            Assert.AreEqual(Side.White, state.SideToMove);
            Assert.IsTrue(state.TurnOpen);
            Assert.IsTrue(state.CanEndTurn());
            state = state.EndTurn();
            Assert.AreEqual(Side.Black, state.SideToMove);
        }

        [Test]
        public void EmpoweredPowers_DescribeEveryCorePieceType()
        {
            Assert.AreEqual("Empowered", EmpoweredPowers.EffectName);
            Assert.IsFalse(string.IsNullOrWhiteSpace(EmpoweredPowers.Describe(PieceType.Pawn)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(EmpoweredPowers.Describe(PieceType.Knight)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(EmpoweredPowers.Describe(PieceType.Bishop)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(EmpoweredPowers.Describe(PieceType.Rook)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(EmpoweredPowers.Describe(PieceType.Queen)));
            Assert.IsFalse(string.IsNullOrWhiteSpace(EmpoweredPowers.Describe(PieceType.King)));
        }
    }
}
