using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class OccupancyTests
    {
        [Test]
        public void CanPlace_IsOnlyTrueOnEmptySquares()
        {
            GameState state = GameState.StartingPosition();
            Square king = new Square(4, 0);
            Assert.IsFalse(state.Board.IsEmpty(king));
            Assert.IsFalse(state.Board.CanPlace(king));
            Square empty = new Square(4, 3);
            Assert.IsTrue(state.Board.IsEmpty(empty));
            Assert.IsTrue(state.Board.CanPlace(empty));
            Assert.IsFalse(state.Board.CanPlace(new Square(4, 9)));
        }
        [Test]
        public void WithPiece_DoesNotReplaceAKing()
        {
            GameState state = GameState.StartingPosition();
            Square kingSquare = new Square(4, 0);
            Piece king = state.Board.GetPiece(kingSquare);
            Board next = state.Board.WithPiece(kingSquare, new Piece(PieceType.Pawn, Side.White));
            Piece still = next.GetPiece(kingSquare);
            Assert.AreEqual(PieceType.King, still.Type);
            Assert.AreEqual(king.Id, still.Id);
        }
        [Test]
        public void WithPiece_DoesNotReplaceADifferentPiece()
        {
            GameState state = GameState.StartingPosition();
            Square rookSquare = new Square(0, 0);
            Piece rook = state.Board.GetPiece(rookSquare);
            Board next = state.Board.WithPiece(rookSquare, new Piece(PieceType.Pawn, Side.White));
            Piece still = next.GetPiece(rookSquare);
            Assert.AreEqual(PieceType.Rook, still.Type);
            Assert.AreEqual(rook.Id, still.Id);
        }
        [Test]
        public void AddPiece_PlacesACreateFactoryPiece()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K3 w - - 0 1");
            Piece knight = Piece.Create(PieceType.Knight, Side.White);
            GameState next = state.AddPiece(knight, new Square(0, 0));
            Assert.AreEqual(PieceType.Knight, next.Board.GetPiece(new Square(0, 0)).Type);
            Assert.AreEqual(Side.White, next.Board.GetPiece(new Square(0, 0)).Side);
        }
    }
}
