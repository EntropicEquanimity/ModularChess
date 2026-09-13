using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class GameplayLoopTests
    {
        [Test]
        public void FogOfWar_HidesEnemyHomeWhileInProgress()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
            GameState state = GameState.StartingPosition(rules);
            VisionMap vision = VisionMap.Compute(state, Side.White);
            Assert.AreEqual(GameStatus.InProgress, state.Status);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(4, 1)]);
            Assert.AreNotEqual(SquareSight.Identified, vision[new Square(4, 6)]);
        }

        [Test]
        public void FogOfWar_LiftsOnCheckmate()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
            GameState state = MoveTestHelper.Play(
                GameState.StartingPosition(rules),
                "f2f3",
                "e7e5",
                "g2g4",
                "d8h4");
            Assert.AreEqual(GameStatus.Checkmate, state.Status);
            VisionMap vision = VisionMap.Compute(state, Side.White);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(4, 6)]);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(0, 7)]);
        }
    }
}
