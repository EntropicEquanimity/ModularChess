using ModularChess.Core;
using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace ModularChess.Core.Tests
{
    public class BoonOfferTests
    {
        [Test]
        public void Offer_WithOneBoon_ShowsOneCard()
        {
            IReadOnlyList<BoonDefinition> offer = BoonOfferBuilder.Build(
                BoonCatalog.PlayerPool,
                new Dictionary<BoonId, int>(),
                stageNumber: 1,
                new Random(1));
            Assert.AreEqual(1, offer.Count);
            Assert.AreEqual(BoonId.Reinforcements, offer[0].Id);
        }

        [Test]
        public void Reinforcements_SkipsBackRank()
        {
            Board board = Board.Empty()
                .WithPiece(new Square(4, 0), new Piece(PieceType.King, Side.White))
                .WithPiece(new Square(4, 7), new Piece(PieceType.King, Side.Black));
            Board next = SummonPlacement.PlacePawns(
                board, Side.White, 1, skipBackRank: true, ModeRuntime.Empty, out _, new Random(2));
            for (int file = 0; file < 8; file++)
            {
                Piece piece = next.GetPiece(new Square(file, 0));
                if (piece != null && piece.Type == PieceType.Pawn)
                    Assert.Fail("Reinforcement pawn on back rank.");
            }
            bool found = false;
            for (int file = 0; file < 8; file++)
            {
                Piece piece = next.GetPiece(new Square(file, 1));
                if (piece != null && piece.Type == PieceType.Pawn && piece.Side == Side.White)
                    found = true;
            }
            Assert.IsTrue(found);
        }
    }
}
