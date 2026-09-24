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
        public void EmpoweredBishop_SwapsWithAdjacentPawnsInEightDirections()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
            GameState state = GameState.FromFen("4k3/8/8/3ppp2/3pBp2/3ppp2/8/4K3 w - - 0 1", rules);
            Piece bishop = state.Board.GetPiece(new Square(4, 3));
            state = state.ConfirmEmpowered(new[] { bishop.Id });
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "d3"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "e3"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "f3"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "d4"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "f4"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "d5"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "e5"));
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "f5"));
            state = MoveTestHelper.Play(state, "e4d4");
            Assert.AreEqual(PieceType.Pawn, state.Board.GetPiece(new Square(4, 3)).Type);
            Assert.AreEqual(PieceType.Bishop, state.Board.GetPiece(new Square(3, 3)).Type);
        }
        [Test]
        public void EmpoweredPawn_MovesForwardAndCannotCapture()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
            GameState state = GameState.FromFen("4k3/8/8/3p4/4P3/8/8/4K3 w - - 0 1", rules);
            Piece pawn = state.Board.GetPiece(new Square(4, 3));
            state = state.ConfirmEmpowered(new[] { pawn.Id });
            Assert.IsTrue(MoveTestHelper.Has(state, "e4", "e5"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e4", "d5"));
        }
        [Test]
        public void EmpoweredPawn_IsProtectedOnlyFromTheThreeFrontSquares()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
            GameState front = GameState.FromFen("4k3/8/8/4q3/4P3/8/8/4K3 b - - 0 1", rules);
            Piece frontPawn = front.Board.GetPiece(new Square(4, 3));
            front = front.ConfirmEmpowered(new[] { frontPawn.Id });
            Assert.IsFalse(MoveTestHelper.Has(front, "e5", "e4"));
            GameState diagonal = GameState.FromFen("4k3/8/8/3q4/4P3/8/8/4K3 b - - 0 1", rules);
            Piece diagonalPawn = diagonal.Board.GetPiece(new Square(4, 3));
            diagonal = diagonal.ConfirmEmpowered(new[] { diagonalPawn.Id });
            Assert.IsFalse(MoveTestHelper.Has(diagonal, "d5", "e4"));
            GameState side = GameState.FromFen("4k3/8/8/8/3qP3/8/8/4K3 b - - 0 1", rules);
            Piece sidePawn = side.Board.GetPiece(new Square(4, 3));
            side = side.ConfirmEmpowered(new[] { sidePawn.Id });
            Assert.IsTrue(MoveTestHelper.Has(side, "d4", "e4"));
        }
        [Test]
        public void EmpoweredPawn_BlackFrontIsOppositeWhite()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
            GameState front = GameState.FromFen("4k3/8/8/4p3/4Q3/8/8/4K3 w - - 0 1", rules);
            Piece frontPawn = front.Board.GetPiece(new Square(4, 4));
            front = front.ConfirmEmpowered(new[] { frontPawn.Id });
            Assert.IsFalse(MoveTestHelper.Has(front, "e4", "e5"));
            GameState diagonal = GameState.FromFen("4k3/8/8/4p3/3Q4/8/8/4K3 w - - 0 1", rules);
            Piece diagonalPawn = diagonal.Board.GetPiece(new Square(4, 4));
            diagonal = diagonal.ConfirmEmpowered(new[] { diagonalPawn.Id });
            Assert.IsFalse(MoveTestHelper.Has(diagonal, "d4", "e5"));
            GameState side = GameState.FromFen("4k3/8/8/3Qp3/8/8/8/4K3 w - - 0 1", rules);
            Piece sidePawn = side.Board.GetPiece(new Square(4, 4));
            side = side.ConfirmEmpowered(new[] { sidePawn.Id });
            Assert.IsTrue(MoveTestHelper.Has(side, "d5", "e5"));
        }
        [Test]
        public void EmpoweredBishop_CannotCaptureEnemySuperPawnFromTheFront()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
            GameState adjacent = GameState.FromFen("4k3/8/8/4p3/3B4/8/8/4K3 w - - 0 1", rules);
            Piece bishop = adjacent.Board.GetPiece(new Square(3, 3));
            Piece pawn = adjacent.Board.GetPiece(new Square(4, 4));
            adjacent = adjacent.ConfirmEmpowered(new[] { bishop.Id, pawn.Id });
            Assert.IsFalse(MoveTestHelper.Has(adjacent, "d4", "e5"));
            GameState distant = GameState.FromFen("4k3/8/8/4p3/8/2B5/8/4K3 w - - 0 1", rules);
            Piece distantBishop = distant.Board.GetPiece(new Square(2, 2));
            Piece distantPawn = distant.Board.GetPiece(new Square(4, 4));
            distant = distant.ConfirmEmpowered(new[] { distantBishop.Id, distantPawn.Id });
            Assert.IsFalse(MoveTestHelper.Has(distant, "c3", "e5"));
            GameState rear = GameState.FromFen("4k3/8/3B4/4p3/8/8/8/4K3 w - - 0 1", rules);
            Piece rearBishop = rear.Board.GetPiece(new Square(3, 5));
            Piece rearPawn = rear.Board.GetPiece(new Square(4, 4));
            rear = rear.ConfirmEmpowered(new[] { rearBishop.Id, rearPawn.Id });
            Assert.IsTrue(MoveTestHelper.Has(rear, "d6", "e5"));
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

        [Test]
        public void EmpoweredPowers_CostTable()
        {
            Assert.AreEqual(3, EmpoweredPowers.Cost(PieceType.Queen));
            Assert.AreEqual(2, EmpoweredPowers.Cost(PieceType.King));
            Assert.AreEqual(2, EmpoweredPowers.Cost(PieceType.Rook));
            Assert.AreEqual(2, EmpoweredPowers.Cost(PieceType.Knight));
            Assert.AreEqual(1, EmpoweredPowers.Cost(PieceType.Bishop));
            Assert.AreEqual(1, EmpoweredPowers.Cost(PieceType.Pawn));
        }

        [Test]
        public void MatchSettings_DefaultEmpowerBudgetIsFour()
        {
            Assert.AreEqual(4, MatchSettings.Default.EmpowerBudget);
            Assert.AreEqual(3, new MatchSettings(empowerBudget: 1).EmpowerBudget);
            Assert.AreEqual(20, new MatchSettings(empowerBudget: 99).EmpowerBudget);
        }
    }
}
