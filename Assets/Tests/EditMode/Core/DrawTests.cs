using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class DrawTests
    {
        [Test]
        public void KingVersusKing_IsDraw()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K3 w - - 0 1");
            Assert.AreEqual(GameStatus.Draw, state.Status);
        }

        [Test]
        public void FiftyMoveRule_DrawsOnHundredthHalfmove()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K2R w - - 99 1");
            Assert.AreEqual(GameStatus.InProgress, state.Status);
            GameState next = MoveTestHelper.Play(state, "e1d1");
            Assert.AreEqual(100, next.HalfmoveClock);
            Assert.AreEqual(GameStatus.Draw, next.Status);
        }

        [Test]
        public void PieceValues_MatchStandardPoints()
        {
            Assert.AreEqual(1, PieceValues.Get(PieceType.Pawn));
            Assert.AreEqual(3, PieceValues.Get(PieceType.Knight));
            Assert.AreEqual(3, PieceValues.Get(PieceType.Bishop));
            Assert.AreEqual(5, PieceValues.Get(PieceType.Rook));
            Assert.AreEqual(9, PieceValues.Get(PieceType.Queen));
            Assert.IsNull(PieceValues.Get(PieceType.King));
        }

        [Test]
        public void ThreefoldRepetition_IsDraw()
        {
            GameState state = MoveTestHelper.Play(
                GameState.StartingPosition(),
                "g1f3",
                "g8f6",
                "f3g1",
                "f6g8",
                "g1f3",
                "g8f6",
                "f3g1",
                "f6g8");
            Assert.AreEqual(GameStatus.Draw, state.Status);
            Assert.AreEqual(Fen.StartingPosition.Split(' ')[0], state.ToFen().Split(' ')[0]);
        }
    }
}
