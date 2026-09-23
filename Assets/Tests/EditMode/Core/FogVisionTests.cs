using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class FogVisionTests
    {
        [Test]
        public void StartingPosition_HomeRanksAreIdentified()
        {
            VersusRules rules = new VersusRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
            GameState state = GameState.StartingPosition(rules);
            VisionMap vision = VisionMap.Compute(state, Side.White);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(0, 0)]);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(4, 1)]);
            Assert.AreNotEqual(SquareSight.Identified, vision[new Square(4, 6)]);
        }
    }
}
