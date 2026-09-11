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
    }
}
