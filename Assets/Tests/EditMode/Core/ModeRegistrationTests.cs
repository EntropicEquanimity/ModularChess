using System.Collections.Generic;
using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class ModeRegistrationTests
    {
        [Test]
        public void Vision_WithoutFog_IsAllIdentified()
        {
            GameState state = GameState.StartingPosition();
            VisionMap vision = VisionMap.Compute(state, Side.White);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(4, 6)]);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(0, 7)]);
        }
        [Test]
        public void Vision_WithFog_GoesThroughRegisteredHook()
        {
            VersusRules rules = new VersusRules(new[] { ModeId.FogOfWar }, MatchSettings.Default);
            GameState state = GameState.StartingPosition(rules);
            VisionMap vision = VisionMap.Compute(state, Side.White);
            Assert.AreEqual(SquareSight.Identified, vision[new Square(4, 1)]);
            Assert.AreNotEqual(SquareSight.Identified, vision[new Square(4, 6)]);
        }
        [Test]
        public void DraftTargets_WithoutMartyr_AreEmpty()
        {
            GameState state = GameState.StartingPosition();
            IReadOnlyList<Square> targets = state.DraftTargets(MartyrPower.Exile);
            Assert.AreEqual(0, targets.Count);
        }
        [Test]
        public void DraftTargets_Exile_MatchesIsExileTarget()
        {
            VersusRules rules = new VersusRules(new[] { ModeId.Martyr }, new MatchSettings(martyrThreshold: 1));
            GameState state = GameState.FromFen("4qk2/8/8/8/8/4N3/3p4/R2QK3 w - - 0 1", rules);
            state = MoveTestHelper.Play(state, "d1d2");
            Assert.IsTrue(state.DraftPending);
            IReadOnlyList<Square> targets = state.DraftTargets(MartyrPower.Exile);
            Assert.Greater(targets.Count, 0);
            Square pinned = new Square(4, 2);
            Assert.IsFalse(Contains(targets, pinned));
            Assert.IsTrue(Contains(targets, new Square(0, 0)));
            Assert.IsFalse(state.IsExileTarget(pinned));
            Assert.IsTrue(state.IsExileTarget(new Square(0, 0)));
        }
        [Test]
        public void DraftTargets_Reinforcements_AreEmptyBackRank()
        {
            VersusRules rules = new VersusRules(new[] { ModeId.Martyr }, MatchSettings.Default);
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/4K3 w - - 0 1", rules);
            IReadOnlyList<Square> targets = state.DraftTargets(MartyrPower.Reinforcements);
            Assert.AreEqual(7, targets.Count);
            for (int i = 0; i < targets.Count; i++)
            {
                Assert.AreEqual(0, targets[i].Rank);
                Assert.IsTrue(state.Board.CanPlace(targets[i]));
            }
        }
        [Test]
        public void StageRules_UsesRoguelikeLawAndExtraLifeHooks()
        {
            StageRules rules = StageRules.Create(Side.White, PieceType.King);
            Assert.AreEqual(Side.White, rules.PlayerSide);
            Assert.AreEqual(PieceType.King, rules.StageTarget);
            Assert.AreEqual(0, rules.Modes.Count);
            Assert.IsInstanceOf<RoguelikeLaw>(rules.Law);
            Assert.AreSame(ModeHooks.ExtraLife, rules.Hooks);
        }
        static bool Contains(IReadOnlyList<Square> squares, Square needle)
        {
            for (int i = 0; i < squares.Count; i++)
            {
                if (squares[i].Equals(needle))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
