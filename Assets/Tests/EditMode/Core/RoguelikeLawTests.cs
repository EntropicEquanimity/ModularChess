using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class RoguelikeLawTests
    {
        [Test]
        public void King_HasNoLegalMoves_OnceStageStarted()
        {
            MatchRules rules = MatchRules.Roguelike(Side.White, PieceType.King);
            GameState state = RoguelikePositions.KingsOnly(rules);
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "e2"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "d1"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "d2"));
        }

        [Test]
        public void Slider_RangeIsCappedAtThree()
        {
            MatchRules rules = MatchRules.Roguelike(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteRookOnA1(rules);
            Assert.IsTrue(MoveTestHelper.Has(state, "a1", "a4"));
            Assert.IsFalse(MoveTestHelper.Has(state, "a1", "a5"));
        }

        [Test]
        public void Check_DoesNotForceReply()
        {
            MatchRules rules = MatchRules.Roguelike(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteInCheckWithKnightEscape(rules);
            Assert.IsTrue(state.IsInCheck);
            Assert.IsTrue(MoveTestHelper.Has(state, "b1", "a3"));
        }

        [Test]
        public void CapturingEnemyKing_ClearsStage()
        {
            MatchRules rules = MatchRules.Roguelike(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteQueenAttacksEnemyKing(rules);
            state = MoveTestHelper.PlayOne(state, "e5e8");
            Assert.AreEqual(GameStatus.StageCleared, state.Status);
        }

        [Test]
        public void CapturingPlayerKing_EndsRun()
        {
            MatchRules rules = MatchRules.Roguelike(Side.White, PieceType.King);
            GameState state = RoguelikePositions.BlackQueenAttacksPlayerKing(rules);
            state = MoveTestHelper.PlayOne(state, "e4e1");
            Assert.AreEqual(GameStatus.RunLost, state.Status);
        }

        [Test]
        public void NoPromotion_OnLastRank()
        {
            MatchRules rules = MatchRules.Roguelike(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhitePawnOnSeventh(rules);
            Assert.IsTrue(MoveTestHelper.Has(state, "a7", "a8"));
            Assert.IsFalse(MoveTestHelper.Has(state, "a7", "a8", PieceType.Queen));
            state = MoveTestHelper.PlayOne(state, "a7a8");
            Assert.IsTrue(Square.TryParse("a8", out Square a8));
            Assert.AreEqual(PieceType.Pawn, state.Board.GetPiece(a8).Type);
        }

        [Test]
        public void EmptyLegalMoves_IsNotCheckmate()
        {
            MatchRules rules = MatchRules.Roguelike(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteKingAloneNoMoves(rules);
            Assert.AreEqual(GameStatus.InProgress, state.Status);
            Assert.AreEqual(0, state.LegalMoves.Count);
        }
    }
}
