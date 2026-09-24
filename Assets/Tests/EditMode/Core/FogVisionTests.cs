using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class FogVisionTests
    {
        [Test]
        public void StartingPosition_HomeRanksAreIdentified()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
            GameState state = GameState.StartingPosition(rules);
            VisionMap vision = VisionMap.Compute(state, Side.White);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(0, 0)]);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(4, 1)]);
            Assert.AreNotEqual(SquareSight.Identified, vision[new Square(4, 6)]);
        }

        [Test]
        public void OccupantBeyondRayBlocker_IsHiddenNotShadow()
        {
            MatchRules rules = new MatchRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
            GameState state = GameState.FromFen("4k3/8/4n3/8/4r3/8/4R3/4K3 w - - 0 1", rules);
            VisionMap vision = VisionMap.Compute(state, Side.White);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(4, 3)]);
            Assert.AreEqual(SquareSight.Hidden, vision[new Square(4, 5)]);
        }
    }
}
