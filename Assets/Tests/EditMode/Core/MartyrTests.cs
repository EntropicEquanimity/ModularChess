using System;
using System.Collections.Generic;
using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class MartyrTests
    {
        [Test]
        public void CapturesQueueDraftAtThreshold()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Assert.AreEqual(1, state.Runtime.LostMaterial(Side.Black));
            Assert.IsTrue(state.DraftPending);
            Assert.AreEqual(Side.Black, state.SideToMove);
        }
        [Test]
        public void TickStatusesUpdatesRemainingTurnsWithoutThrowing()
        {
            Guid id = Guid.NewGuid();
            ModeRuntime runtime = ModeRuntime.Empty.WithStatus(
                id,
                new PieceStatus(StatusKind.Invulnerable, Side.White, 5));
            ModeRuntime ticked = runtime.TickStatuses(Side.White);
            Assert.IsTrue(ticked.TryGetStatus(id, out PieceStatus status));
            Assert.AreEqual(4, status.RemainingTurns);
        }
        [Test]
        public void TickStatusesRemovesExpiredStatus()
        {
            Guid id = Guid.NewGuid();
            ModeRuntime runtime = ModeRuntime.Empty.WithStatus(
                id,
                new PieceStatus(StatusKind.Stasis, Side.Black, 1));
            ModeRuntime ticked = runtime.TickStatuses(Side.Black);
            Assert.IsFalse(ticked.TryGetStatus(id, out _));
        }
        [Test]
        public void ReinforcementsPlaceTwoPawnsAndSkipOccupiedSquares()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Square kingSquare = new Square(4, 7);
            Piece king = state.Board.GetPiece(kingSquare);
            Assert.IsNotNull(king);
            Assert.AreEqual(PieceType.King, king.Type);
            state = state.ApplyDraft(
                MartyrPower.Reinforcements,
                null,
                new[] { kingSquare });
            Piece stillKing = state.Board.GetPiece(kingSquare);
            Assert.IsNotNull(stillKing);
            Assert.AreEqual(PieceType.King, stillKing.Type);
            Assert.AreEqual(king.Id, stillKing.Id);
            int pawns = 0;
            for (int file = 0; file < Square.BoardSize; file++)
            {
                Piece piece = state.Board.GetPiece(new Square(file, 7));
                if (piece != null && piece.Type == PieceType.Pawn && piece.Side == Side.Black)
                {
                    pawns++;
                    Assert.IsTrue(state.Runtime.IsSummoned(piece.Id));
                }
            }
            Assert.AreEqual(2, pawns);
        }
        [Test]
        public void ReinforcementsNeverReplaceKingOnFileA()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("k7/3p4/8/8/8/8/8/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d7");
            Square kingSquare = new Square(0, 7);
            Piece king = state.Board.GetPiece(kingSquare);
            Assert.IsNotNull(king);
            Assert.AreEqual(PieceType.King, king.Type);
            state = state.ApplyDraft(
                MartyrPower.Reinforcements,
                null,
                new[] { kingSquare, kingSquare });
            Piece stillKing = state.Board.GetPiece(kingSquare);
            Assert.IsNotNull(stillKing);
            Assert.AreEqual(PieceType.King, stillKing.Type);
            Assert.AreEqual(king.Id, stillKing.Id);
            int pawns = 0;
            for (int file = 0; file < Square.BoardSize; file++)
            {
                Piece piece = state.Board.GetPiece(new Square(file, 7));
                if (piece != null && piece.Type == PieceType.Pawn && piece.Side == Side.Black)
                    pawns++;
            }
            Assert.AreEqual(2, pawns);
        }
        [Test]
        public void ReinforcementsNeverReplaceRookOnBackRank()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("r3k2r/3p4/8/8/8/8/8/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d7");
            Square aRook = new Square(0, 7);
            Square hRook = new Square(7, 7);
            Piece left = state.Board.GetPiece(aRook);
            Piece right = state.Board.GetPiece(hRook);
            Assert.IsNotNull(left);
            Assert.IsNotNull(right);
            state = state.ApplyDraft(
                MartyrPower.Reinforcements,
                null,
                new[] { aRook, hRook });
            Assert.AreEqual(PieceType.Rook, state.Board.GetPiece(aRook).Type);
            Assert.AreEqual(left.Id, state.Board.GetPiece(aRook).Id);
            Assert.AreEqual(PieceType.Rook, state.Board.GetPiece(hRook).Type);
            Assert.AreEqual(right.Id, state.Board.GetPiece(hRook).Id);
            int pawns = 0;
            for (int file = 0; file < Square.BoardSize; file++)
            {
                Piece piece = state.Board.GetPiece(new Square(file, 7));
                if (piece != null && piece.Type == PieceType.Pawn && piece.Side == Side.Black)
                    pawns++;
            }
            Assert.AreEqual(2, pawns);
        }
        [Test]
        public void StasisFieldIsNotOfferedWhenOpponentHasNoQueen()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/3P4/8/8/8/8/8/4K3 b - - 0 1", rules);
            state = MoveTestHelper.Play(state, "e8d7");
            Assert.IsTrue(state.DraftPending);
            DraftOffer offer = state.Runtime.PendingDraft.Value;
            Assert.IsFalse(offer.Contains(MartyrPower.StasisField));
        }
        [Test]
        public void FleetPawnsCannotBeOfferedAfterObtainLimit()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3pp3/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Assert.IsTrue(state.DraftPending);
            state = state.ApplyDraft(MartyrPower.FleetPawns, null, null);
            Assert.AreEqual(1, state.Runtime.ObtainCount(Side.Black, MartyrPower.FleetPawns));
            state = MoveTestHelper.Play(state, "e8e7");
            state = MoveTestHelper.Play(state, "d2e2");
            Assert.IsTrue(state.DraftPending);
            DraftOffer offer = state.Runtime.PendingDraft.Value;
            Assert.IsFalse(offer.Contains(MartyrPower.FleetPawns));
            AssertOfferHasNoDuplicates(offer);
        }
        [Test]
        public void BattlefieldPromotionPromotesTheChosenPawn()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/3n4/8/8/2p1p3/3QK3 w - - 0 1", rules);
            Piece chosen = state.Board.GetPiece(new Square(2, 1));
            Piece other = state.Board.GetPiece(new Square(4, 1));
            Assert.IsNotNull(chosen);
            Assert.IsNotNull(other);
            state = MoveTestHelper.Play(state, "d1d5");
            Assert.IsTrue(state.DraftPending);
            PieceType promotedTo = state.Runtime.PendingBattlefieldType ?? PieceType.Knight;
            state = state.ApplyDraft(MartyrPower.BattlefieldPromotion, chosen.Id, null);
            Piece promoted = state.Board.GetPiece(new Square(2, 1));
            Piece leftover = state.Board.GetPiece(new Square(4, 1));
            Assert.IsNotNull(promoted);
            Assert.AreEqual(chosen.Id, promoted.Id);
            Assert.AreEqual(promotedTo, promoted.Type);
            Assert.IsNotNull(leftover);
            Assert.AreEqual(other.Id, leftover.Id);
            Assert.AreEqual(PieceType.Pawn, leftover.Type);
        }
        [Test]
        public void CapturesAreRecordedInOrderAndSkipExtraLifeBounce()
        {
            MatchRules martyr = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 9));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3pp3/3QK3 w - - 0 1", martyr);
            Piece first = state.Board.GetPiece(new Square(3, 1));
            Piece second = state.Board.GetPiece(new Square(4, 1));
            state = MoveTestHelper.Play(state, "d1d2");
            Assert.AreEqual(1, state.Runtime.Captures.Count);
            Assert.AreEqual(first.Id, state.Runtime.Captures[0].Id);
            Assert.AreEqual(PieceType.Pawn, state.Runtime.Captures[0].Type);
            Assert.IsFalse(state.Runtime.Captures[0].Exiled);
            state = MoveTestHelper.Play(state, "e8e7");
            state = MoveTestHelper.Play(state, "d2e2");
            Assert.AreEqual(2, state.Runtime.Captures.Count);
            Assert.AreEqual(second.Id, state.Runtime.Captures[1].Id);
            MatchRules extraLife = new MatchRules(new[] { ModeId.PowerfulPieces }, MatchSettings.Default);
            GameState bounce = GameState.FromFen("4k3/8/8/8/8/8/3n4/3QK3 w - - 0 1", extraLife);
            Piece knight = bounce.Board.GetPiece(new Square(3, 1));
            bounce = bounce.ConfirmEmpowered(new[] { knight.Id });
            bounce = MoveTestHelper.Play(bounce, "d1d2");
            Assert.AreEqual(0, bounce.Runtime.Captures.Count);
            Assert.IsNotNull(bounce.Board.GetPiece(new Square(3, 1)));
        }
        [Test]
        public void DraftOfferNeverDuplicatesACard()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Assert.IsTrue(state.DraftPending);
            AssertOfferHasNoDuplicates(state.Runtime.PendingDraft.Value);
        }
        [Test]
        public void RallyKeepsTheTurnOpenForOneExtraMove()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            state = state.ApplyDraft(MartyrPower.Rally, null, null);
            Assert.IsTrue(state.Runtime.RallyArmed);
            Assert.AreEqual(Side.Black, state.SideToMove);
            state = MoveTestHelper.Play(state, "e8e7");
            Assert.AreEqual(Side.Black, state.SideToMove);
            Assert.IsTrue(state.TurnOpen);
            Assert.IsTrue(state.CanEndTurn());
            state = MoveTestHelper.Play(state, "e7e8");
            Assert.AreEqual(Side.White, state.SideToMove);
            Assert.IsFalse(state.Runtime.RallyArmed);
        }
        [Test]
        public void ExileRemovesAnEnemyThenReturnsItAfterTwoOfTheirTurns()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3pP3/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Piece queen = state.Board.GetPiece(new Square(3, 1));
            Assert.IsNotNull(queen);
            state = state.ApplyDraft(MartyrPower.Exile, queen.Id, null);
            Assert.IsNull(state.Board.GetPiece(new Square(3, 1)));
            Assert.AreEqual(1, CountExiled(state.Runtime));
            state = MoveTestHelper.Play(state, "e8e7");
            Assert.IsNull(state.Board.GetPiece(new Square(3, 1)));
            state = MoveTestHelper.Play(state, "e1d1");
            Assert.IsNull(state.Board.GetPiece(new Square(3, 1)));
            state = MoveTestHelper.Play(state, "e7e8");
            state = MoveTestHelper.Play(state, "d1e1");
            Piece returned = state.Board.GetPiece(new Square(3, 1));
            Assert.IsNotNull(returned);
            Assert.AreEqual(queen.Id, returned.Id);
            Assert.AreEqual(0, CountExiled(state.Runtime));
        }
        [Test]
        public void RevivalReturnsTheLastCapturedFriendlyAsSummoned()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/3QK3 w - - 0 1", rules);
            Piece pawn = state.Board.GetPiece(new Square(3, 1));
            state = MoveTestHelper.Play(state, "d1d2");
            state = state.ApplyDraft(MartyrPower.Revival, null, null);
            Assert.AreEqual(0, state.Runtime.Captures.Count);
            Piece revived = null;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = state.Board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Type == PieceType.Pawn && piece.Side == Side.Black)
                {
                    revived = piece;
                    break;
                }
            }
            Assert.IsNotNull(revived);
            Assert.AreNotEqual(pawn.Id, revived.Id);
            Assert.IsTrue(state.Runtime.IsSummoned(revived.Id));
        }
        [Test]
        public void RevivalPicksAmongEmptySquaresOnTheOpenRank()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            var files = new HashSet<int>();
            for (int n = 0; n < 32; n++)
            {
                GameState state = GameState.FromFen("4k3/3q4/8/8/8/8/3P4/4K3 b - - 0 1", rules);
                state = MoveTestHelper.Play(state, "d7d2");
                Assert.IsTrue(state.DraftPending);
                Assert.AreEqual(Side.White, state.SideToMove);
                state = state.ApplyDraft(MartyrPower.Revival, null, null);
                Square? found = null;
                for (int i = 0; i < 64; i++)
                {
                    Square square = Square.FromIndex(i);
                    Piece piece = state.Board.GetPiece(square);
                    if (piece != null && piece.Type == PieceType.Pawn && piece.Side == Side.White)
                    {
                        found = square;
                        break;
                    }
                }
                Assert.IsNotNull(found);
                Assert.AreEqual(0, found.Value.Rank);
                files.Add(found.Value.File);
            }
            Assert.Greater(files.Count, 1);
        }
        static void AssertOfferHasNoDuplicates(DraftOffer offer)
        {
            Assert.Greater(offer.Count, 0);
            for (int i = 0; i < offer.Count; i++)
            {
                for (int j = i + 1; j < offer.Count; j++)
                {
                    Assert.AreNotEqual(offer.At(i), offer.At(j));
                }
            }
        }
        [Test]
        public void DraftOfferUsesMartyrDraftOptionsCount()
        {
            MatchRules rules = new MatchRules(
                new[] { ModeId.Martyr },
                new MatchSettings(martyrThreshold: 1, martyrDraftOptions: 5));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Assert.IsTrue(state.DraftPending);
            DraftOffer offer = state.Runtime.PendingDraft.Value;
            Assert.AreEqual(5, offer.Count);
            AssertOfferHasNoDuplicates(offer);
        }
        [Test]
        public void ReinforcementsNotOfferedWhenBackRankIsFull()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("rnbqkbnr/3p4/8/8/8/8/3P4/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Assert.IsTrue(state.DraftPending);
            DraftOffer offer = state.Runtime.PendingDraft.Value;
            for (int i = 0; i < offer.Count; i++)
                Assert.AreNotEqual(MartyrPower.Reinforcements, offer.At(i));
        }
        [Test]
        public void ExileCannotTargetAbsolutelyPinnedPieces()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4q3/8/8/8/8/4N3/3p4/R2QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Assert.IsTrue(state.DraftPending);
            Square pinned = new Square(4, 2);
            Piece knight = state.Board.GetPiece(pinned);
            Assert.IsNotNull(knight);
            Assert.AreEqual(PieceType.Knight, knight.Type);
            Assert.IsFalse(state.IsExileTarget(pinned));
            Assert.IsTrue(state.IsExileTarget(new Square(0, 0)));
            state = state.ApplyDraft(MartyrPower.Exile, knight.Id, null);
            Assert.IsNotNull(state.Board.GetPiece(pinned));
            Assert.AreEqual(0, CountExiled(state.Runtime));
        }
        [Test]
        public void BattlefieldPromotionPieceHasLegalMoves()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/2pp4/3QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Piece pawn = state.Board.GetPiece(new Square(2, 1));
            Assert.IsNotNull(pawn);
            Assert.AreEqual(PieceType.Pawn, pawn.Type);
            state = state.ApplyDraft(MartyrPower.BattlefieldPromotion, pawn.Id, null);
            Piece promoted = state.Board.GetPiece(new Square(2, 1));
            Assert.IsNotNull(promoted);
            Assert.AreNotEqual(PieceType.Pawn, promoted.Type);
            Assert.AreEqual(pawn.Id, promoted.Id);
            Assert.Greater(state.LegalMovesFrom(new Square(2, 1)).Count, 0);
        }
        static int CountExiled(ModeRuntime runtime)
        {
            int count = 0;
            for (int i = 0; i < runtime.Captures.Count; i++)
            {
                if (runtime.Captures[i].Exiled)
                {
                    count++;
                }
            }
            return count;
        }
    }
}
