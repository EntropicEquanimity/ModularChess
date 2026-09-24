using ModularChess.Core;
using NUnit.Framework;
using System;

namespace ModularChess.Core.Tests
{
    public class RoguelikeStageFactoryTests
    {
        [Test]
        public void Stage1_PlacesKingAndStartingPiece()
        {
            var run = new RoguelikeRunState(Side.White);
            var settings = new RoguelikeRunSettings { StartingPiece = PieceType.Rook };
            RoguelikeStageSpawn spawn = RoguelikeStageFactory.Create(run, new Random(3), settings);
            Assert.IsNotNull(spawn.State.Board.GetPiece(new Square(4, 0)));
            Assert.AreEqual(PieceType.King, spawn.State.Board.GetPiece(new Square(4, 0)).Type);
            Assert.IsTrue(spawn.StartingPieceId.HasValue);
            Square? startSquare = spawn.State.Board.FindSquare(spawn.StartingPieceId.Value);
            Assert.IsTrue(startSquare.HasValue);
            Assert.LessOrEqual(startSquare.Value.Rank, 1);
            Assert.Greater(spawn.EnemyPieceIds.Count, 0);
            Assert.AreEqual(PieceType.King, spawn.State.Board.GetPiece(new Square(4, 7)).Type);
        }

        [Test]
        public void Stage1_HasNoEnemyReinforcements()
        {
            var run = new RoguelikeRunState(Side.White);
            run.SetEnemyBoon(RoguelikeBalance.EnemyBoonForStage(1));
            RoguelikeStageSpawn spawn = RoguelikeStageFactory.Create(run, new Random(3));
            Assert.IsNull(run.ActiveEnemyBoon);
            for (int i = 0; i < 64; i++)
            {
                Piece piece = spawn.State.Board.GetPiece(Square.FromIndex(i));
                if (piece != null)
                    Assert.IsFalse(spawn.State.Runtime.IsSummoned(piece.Id));
            }
        }

        [Test]
        public void MiniBoss_PlacesKingPlusQueen()
        {
            var run = new RoguelikeRunState(Side.White);
            for (int i = 1; i < RoguelikeBalance.MiniBossStageA; i++)
                run.AdvanceStage();
            RoguelikeStageSpawn spawn = RoguelikeStageFactory.Create(run, new Random(4));
            Assert.AreEqual(PieceType.King, spawn.State.Board.GetPiece(new Square(4, 7)).Type);
            int queens = CountType(spawn.State.Board, Side.Black, PieceType.Queen);
            Assert.AreEqual(1, queens);
            Piece king = spawn.State.Board.GetPiece(new Square(4, 7));
            Assert.AreEqual(1, spawn.State.Runtime.ExtraLifeCount(king.Id));
        }

        [Test]
        public void Boss_PlacesKingPlusTwoQueensAndTwoLives()
        {
            var run = new RoguelikeRunState(Side.White);
            for (int i = 1; i < RoguelikeBalance.BossStage; i++)
                run.AdvanceStage();
            RoguelikeStageSpawn spawn = RoguelikeStageFactory.Create(run, new Random(5));
            Assert.AreEqual(PieceType.King, spawn.State.Board.GetPiece(new Square(4, 7)).Type);
            Assert.AreEqual(2, CountType(spawn.State.Board, Side.Black, PieceType.Queen));
            Piece king = spawn.State.Board.GetPiece(new Square(4, 7));
            Assert.AreEqual(2, spawn.State.Runtime.ExtraLifeCount(king.Id));
        }

        [Test]
        public void NormalStages_DoNotPlaceQueensFromPool()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                var run = new RoguelikeRunState(Side.White);
                RoguelikeStageSpawn spawn = RoguelikeStageFactory.Create(run, new Random(seed));
                Assert.AreEqual(0, CountType(spawn.State.Board, Side.Black, PieceType.Queen));
            }
        }

        [Test]
        public void CaptureGold_KingIsFive()
        {
            Assert.AreEqual(1, RoguelikeBalance.CaptureGold(PieceType.Pawn));
            Assert.AreEqual(5, RoguelikeBalance.CaptureGold(PieceType.Rook));
            Assert.AreEqual(5, RoguelikeBalance.CaptureGold(PieceType.King));
        }

        [Test]
        public void ShopOffer_HasFivePricedItems()
        {
            var items = ShopOfferBuilder.Build(new Random(9));
            Assert.AreEqual(5, items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                int value = PieceValues.Get(items[i].Type) ?? 0;
                Assert.Greater(value, 0);
                Assert.AreEqual(0, items[i].Price % value);
                int mult = items[i].Price / value;
                Assert.GreaterOrEqual(mult, 3);
                Assert.LessOrEqual(mult, 5);
            }
        }

        [Test]
        public void StageTarget_IsAlwaysKing()
        {
            Assert.AreEqual(PieceType.King, RoguelikeBalance.StageTarget(1));
            Assert.AreEqual(PieceType.King, RoguelikeBalance.StageTarget(6));
            Assert.AreEqual(PieceType.King, RoguelikeBalance.StageTarget(13));
        }

        static int CountType(Board board, Side side, PieceType type)
        {
            int count = 0;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Side == side && piece.Type == type)
                    count++;
            }
            return count;
        }
    }
}
