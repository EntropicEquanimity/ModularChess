using ModularChess.Core;
using ModularChess.Match;
using NUnit.Framework;

namespace ModularChess.Match.Tests
{
    public class SimpleAiMateTests
    {
        [Test]
        public void HardChoose_FindsMateInOne()
        {
            GameState state = GameState.FromFen("6k1/5ppp/8/8/8/8/5PPP/4Q1K1 w - - 0 1");
            Move? move = SimpleAi.Choose(state, AiStrength.Hard, Side.White);
            Assert.IsNotNull(move);
            GameState next = state.Apply(move.Value);
            Assert.AreEqual(GameStatus.Checkmate, next.Status);
        }

        [Test]
        public void MediumChoose_CapturesHangingQueen()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/7q/4K2R w - - 0 1");
            Move? move = SimpleAi.Choose(state, AiStrength.Medium, Side.White);
            Assert.IsNotNull(move);
            Assert.AreEqual(new Square(7, 1), move.Value.To);
            Assert.AreEqual(MoveKind.Capture, move.Value.Kind);
        }

        [Test]
        public void MediumChoose_KingCapturesHangingPawnInsteadOfFleeing()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/4K3 w - - 0 1");
            Move? move = SimpleAi.Choose(state, AiStrength.Medium, Side.White);
            Assert.IsNotNull(move);
            Assert.AreEqual(MoveKind.Capture, move.Value.Kind);
            Assert.AreEqual(new Square(3, 1), move.Value.To);
            Piece king = state.Board.GetPiece(move.Value.From);
            Assert.IsNotNull(king);
            Assert.AreEqual(PieceType.King, king.Type);
        }
    }
}
