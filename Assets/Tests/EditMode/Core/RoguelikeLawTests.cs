using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class RoguelikeLawTests
    {
        [Test]
        public void King_HasNoLegalMoves_OnceStageStarted()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.KingsOnly(rules);
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "e2"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "d1"));
            Assert.IsFalse(MoveTestHelper.Has(state, "e1", "d2"));
        }

        [Test]
        public void Slider_RangeIsCappedAtThree()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteRookOnA1(rules);
            Assert.IsTrue(MoveTestHelper.Has(state, "a1", "a4"));
            Assert.IsFalse(MoveTestHelper.Has(state, "a1", "a5"));
        }

        [Test]
        public void Check_DoesNotForceReply()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteInCheckWithKnightEscape(rules);
            Assert.IsTrue(state.IsInCheck);
            Assert.IsTrue(MoveTestHelper.Has(state, "b1", "a3"));
        }

        [Test]
        public void CapturingEnemyKing_ClearsStage()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteQueenAttacksEnemyKing(rules);
            state = MoveTestHelper.PlayOne(state, "e5e8");
            Assert.AreEqual(GameStatus.StageCleared, state.Status);
        }

        [Test]
        public void OnlyEnemyKingRemaining_ClearsStage()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteQueenAttacksEnemyPawnBesideKing(rules);
            state = MoveTestHelper.PlayOne(state, "d5d7");
            Assert.AreEqual(GameStatus.StageCleared, state.Status);
        }

        [Test]
        public void CapturingPlayerKing_EndsRun()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.BlackQueenAttacksPlayerKing(rules);
            state = MoveTestHelper.PlayOne(state, "e4e1");
            Assert.AreEqual(GameStatus.RunLost, state.Status);
        }

        [Test]
        public void NoPromotion_OnLastRank()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
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
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteKingAloneNoMoves(rules);
            Assert.AreEqual(GameStatus.InProgress, state.Status);
            Assert.AreEqual(0, state.LegalMoves.Count);
        }

        [Test]
        public void ExtraLives_StackAndBounceUntilSpent()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteQueenAttacksEnemyKing(rules);
            Piece king = state.Board.GetPiece(new Square(4, 7));
            ModeRuntime runtime = ModeRuntime.Empty.GrantExtraLives(king.Id, 2);
            state = GameState.FromPosition(
                state.Board,
                state.SideToMove,
                state.EnPassantTarget,
                state.CastlingRights,
                state.HalfmoveClock,
                state.FullmoveNumber,
                rules: rules,
                runtime: runtime);
            state = MoveTestHelper.PlayOne(state, "e5e8");
            Assert.AreEqual(GameStatus.InProgress, state.Status);
            Assert.AreEqual(PieceType.King, state.Board.GetPiece(new Square(4, 7)).Type);
            Assert.AreEqual(1, state.Runtime.ExtraLifeCount(king.Id));
            state = state.WithSideToMove(Side.White);
            state = MoveTestHelper.PlayOne(state, "e5e8");
            Assert.AreEqual(GameStatus.InProgress, state.Status);
            Assert.AreEqual(0, state.Runtime.ExtraLifeCount(king.Id));
            state = state.WithSideToMove(Side.White);
            state = MoveTestHelper.PlayOne(state, "e5e8");
            Assert.AreEqual(GameStatus.StageCleared, state.Status);
        }

        [Test]
        public void PrepareRearrange_RemovesSummonedPieces()
        {
            Rules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteRookOnA1(rules);
            ModeRuntime runtime = ModeRuntime.Empty;
            Board board = SummonPlacement.PlacePawns(
                state.Board, Side.White, 1, true, runtime, out runtime, new System.Random(2));
            state = GameState.FromPosition(
                board,
                state.SideToMove,
                null,
                CastlingRights.None,
                0,
                1,
                rules: rules,
                runtime: runtime);
            int summoned = 0;
            foreach (Piece piece in state.Board.OccupiedPieces)
            {
                if (state.Runtime.IsSummoned(piece.Id))
                    summoned++;
            }
            Assert.AreEqual(1, summoned);
            var homes = new System.Collections.Generic.Dictionary<System.Guid, Square>();
            state = state.PrepareRearrange(Side.White, homes);
            foreach (Piece piece in state.Board.OccupiedPieces)
                Assert.IsFalse(state.Runtime.IsSummoned(piece.Id));
            int pawns = 0;
            foreach (Piece piece in state.Board.OccupiedPieces)
            {
                if (piece.Side == Side.White && piece.Type == PieceType.Pawn)
                    pawns++;
            }
            Assert.AreEqual(0, pawns);
        }
    }
}
