using System;
using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class RunSessionTests
    {
        [Test]
        public void PlacePurchased_AddsPieceOnPlayerBackRanks()
        {
            StageRules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K3 w - - 0 1", rules);
            GameState next = RunPlacement.PlacePurchased(state, Side.White, PieceType.Knight);
            Assert.IsNotNull(next);
            Assert.AreEqual(PieceType.Knight, next.Board.GetPiece(new Square(0, 0)).Type);
        }
        [Test]
        public void ChooseEnemyMove_PrefersCapture()
        {
            StageRules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/3p4/3QK3 b - - 0 1", rules);
            Move? move = RunSession.ChooseEnemyMove(state, new Random(1));
            Assert.IsTrue(move.HasValue);
            Assert.IsNotNull(move.Value.CapturedType);
        }
        [Test]
        public void TryBuyShopItem_PlacesAndSpendsGold()
        {
            var session = new RunSession(new Random(2));
            session.Start(new RoguelikeRunSettings { PlayerColor = HostColor.White });
            session.BeginStage(null);
            session.Run.AddGold(50);
            session.OpenShop();
            Assert.Greater(session.ShopItems.Count, 0);
            int goldBefore = session.Run.Gold;
            int armyBefore = RunSession.CountArmy(session.State, session.Run.PlayerSide);
            ShopItem item = session.ShopItems[0];
            Assert.IsTrue(session.TryBuyShopItem(0));
            Assert.AreEqual(goldBefore - item.Price, session.Run.Gold);
            Assert.AreEqual(-1, session.ShopItems[0].Price);
            Assert.AreEqual(armyBefore + 1, RunSession.CountArmy(session.State, session.Run.PlayerSide));
        }
        [Test]
        public void ChooseEnemyMove_NullWhenNoLegalMoves()
        {
            StageRules rules = StageRules.Create(Side.White, PieceType.King);
            GameState state = RoguelikePositions.WhiteKingAloneNoMoves(rules);
            Assert.AreEqual(0, state.LegalMoves.Count);
            Assert.IsFalse(RunSession.ChooseEnemyMove(state, new Random(3)).HasValue);
        }
        [Test]
        public void RunStartsWithStartingGold()
        {
            var run = new RoguelikeRunState(Side.White);
            Assert.AreEqual(RoguelikeBalance.StartingGold, run.Gold);
        }
        [Test]
        public void BeginStage_ResetsTurnLimit()
        {
            var session = new RunSession(new Random(4));
            session.Start(new RoguelikeRunSettings { PlayerColor = HostColor.White });
            session.BeginStage(null);
            Assert.AreEqual(RoguelikeBalance.StageTurnLimit, session.TurnsRemaining);
            Assert.IsTrue(session.TryPassPlayerTurn(out bool timedOut));
            Assert.IsFalse(timedOut);
            Assert.AreEqual(RoguelikeBalance.StageTurnLimit - 1, session.TurnsRemaining);
        }
        [Test]
        public void ExhaustingPlayerTurns_TimesOut()
        {
            var session = new RunSession(new Random(5));
            session.Start(new RoguelikeRunSettings { PlayerColor = HostColor.White });
            session.BeginStage(null);
            bool timedOut = false;
            for (int i = 0; i < RoguelikeBalance.StageTurnLimit; i++)
                timedOut = session.ConsumePlayerTurn();
            Assert.IsTrue(timedOut);
            Assert.AreEqual(0, session.TurnsRemaining);
        }
    }
}
