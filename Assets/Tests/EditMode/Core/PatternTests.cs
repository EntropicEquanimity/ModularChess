using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class PatternTests
    {
        [Test]
        public void SuperPawn_BlocksFrontCorridorAndDiagonals()
        {
            Square pawn = new Square(4, 3);
            Assert.IsFalse(Pattern.SuperPawnAllowsCapture(pawn, Side.White, new Square(4, 4)));
            Assert.IsFalse(Pattern.SuperPawnAllowsCapture(pawn, Side.White, new Square(3, 4)));
            Assert.IsFalse(Pattern.SuperPawnAllowsCapture(pawn, Side.White, new Square(5, 5)));
            Assert.IsTrue(Pattern.SuperPawnAllowsCapture(pawn, Side.White, new Square(3, 3)));
            Assert.IsTrue(Pattern.SuperPawnAllowsCapture(pawn, Side.White, new Square(4, 2)));
        }
        [Test]
        public void SuperPawn_BlackFrontIsOppositeWhite()
        {
            Square pawn = new Square(4, 4);
            Assert.IsFalse(Pattern.SuperPawnAllowsCapture(pawn, Side.Black, new Square(4, 3)));
            Assert.IsFalse(Pattern.SuperPawnAllowsCapture(pawn, Side.Black, new Square(3, 3)));
            Assert.IsTrue(Pattern.SuperPawnAllowsCapture(pawn, Side.Black, new Square(3, 4)));
        }
        [Test]
        public void Ray_StopsAtBoardEdgeAndIncludesOccupants()
        {
            Board board = GameState.FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 0 1").Board;
            var steps = new System.Collections.Generic.List<PatternStep>(8);
            Pattern.Ray(board, new Square(0, 0), 1, 0, steps);
            Assert.AreEqual(7, steps.Count);
            Assert.IsNull(steps[0].Occupant);
            Assert.AreEqual(new Square(4, 0), steps[3].Square);
            Assert.AreEqual(PieceType.King, steps[3].Occupant.Type);
        }
    }
}
