using System;
using ModularChess.Core;
using NUnit.Framework;
using UnityEditor;

namespace ModularChess.Match.Tests
{
    public class CampaignModePlayTests
    {
        [SetUp]
        public void SetUp()
        {
            BindCatalog();
        }

        [Test]
        public void AllLevels_StartInProgressWithBothKingsAndLegalMoves()
        {
            for (int i = 0; i < CampaignCatalog.Count; i++)
            {
                CampaignLevelDefinition level = CampaignCatalog.Get(i);
                GameState state = Start(level);
                Assert.AreEqual(GameStatus.InProgress, state.Status, "Level {0} status", i + 1);
                Assert.NotNull(state.Board.FindKing(Side.White), "Level {0} white king", i + 1);
                Assert.NotNull(state.Board.FindKing(Side.Black), "Level {0} black king", i + 1);
                Assert.Greater(state.LegalMoves.Count, 0, "Level {0} legal moves", i + 1);
            }
        }

        [Test]
        public void NewModeLevels_KeepAuthoredSettingsOnStart()
        {
            AssertLevelModes(25, 2, ModeId.ActionEconomy);
            AssertLevelModes(26, 3, ModeId.ComplexTerrain);
            Assert.AreEqual(TerrainLayoutKind.Woods, CampaignCatalog.Get(25).TerrainLayout);
            AssertLevelModes(28, 3, ModeId.ActionEconomy, ModeId.ComplexTerrain);
            Assert.AreEqual(TerrainLayoutKind.River, CampaignCatalog.Get(27).TerrainLayout);
            CampaignLevelDefinition tinted = CampaignCatalog.Get(29);
            Assert.IsTrue(HasMode(tinted, ModeId.ActionEconomy));
            Assert.IsTrue(HasMode(tinted, ModeId.ComplexTerrain));
            Assert.IsTrue(HasMode(tinted, ModeId.Randomizer));
            Assert.IsTrue(tinted.RandomColors);
            Assert.IsFalse(tinted.RandomShuffle);
            Assert.IsFalse(tinted.RandomPlacement);
        }

        [Test]
        public void PinLesson_QueenExploitsThePinnedKnight()
        {
            Assert.IsTrue(Has(Start(CampaignCatalog.Get(22)), "d1", "h5"));
            GameState twoActions = Start(CampaignCatalog.Get(24));
            Assert.IsTrue(Has(twoActions, "d1", "h5"));
            twoActions = Apply(twoActions, "d1", "h5");
            Assert.AreEqual(Side.White, twoActions.SideToMove);
            Assert.IsTrue(twoActions.CanEndTurn());
            Assert.IsFalse(Has(twoActions, "h5", "f7"));
            Assert.IsTrue(Has(Start(CampaignCatalog.Get(26)), "h5", "f7"));
        }

        [Test]
        public void PileOn_PawnAttacksThePinnedKnight()
        {
            Assert.IsTrue(Has(Start(CampaignCatalog.Get(23)), "d4", "d5"));
        }

        [Test]
        public void TerrainCampaignLevels_PaintTerrain()
        {
            Assert.Greater(CountTerrain(Start(CampaignCatalog.Get(25))), 0);
            Assert.Greater(CountTerrain(Start(CampaignCatalog.Get(27))), 0);
            Assert.Greater(CountTerrain(Start(CampaignCatalog.Get(29))), 0);
        }

        [Test]
        public void TintedPin_ColorsPiecesAndKeepsKings()
        {
            GameState state = Start(CampaignCatalog.Get(29));
            Assert.AreEqual(PieceType.King, state.Board.GetPiece(new Square(4, 0)).Type);
            Assert.AreEqual(PieceType.King, state.Board.GetPiece(new Square(4, 7)).Type);
            Assert.Greater(CountTinted(state), 0);
        }

        [Test]
        public void ActionEconomyOnRiverPin_AllowsASecondPaidMove()
        {
            GameState state = Start(CampaignCatalog.Get(27));
            Assert.IsTrue(Has(state, "d2", "d4"));
            state = Apply(state, "d2", "d4");
            Assert.AreEqual(Side.White, state.SideToMove);
            Assert.IsTrue(state.CanEndTurn());
            Assert.Greater(state.LegalMoves.Count, 0);
        }

        static void BindCatalog()
        {
            CampaignLevelSet set = AssetDatabase.LoadAssetAtPath<CampaignLevelSet>(CampaignLevelSet.AssetPath);
            Assert.IsNotNull(set);
            CampaignCatalog.Bind(set.ToDefinitions());
            Assert.AreEqual(50, CampaignCatalog.Count);
        }

        static GameState Start(CampaignLevelDefinition level)
        {
            return GameState.FromFen(level.Fen, new MatchRules(level.Modes, level.ToMatchSettings()));
        }

        static void AssertLevelModes(int oneBased, int actionPoints, params ModeId[] modes)
        {
            CampaignLevelDefinition level = CampaignCatalog.Get(oneBased - 1);
            MatchSettings settings = level.ToMatchSettings();
            Assert.AreEqual(actionPoints, settings.ActionPoints, "Level {0} action points", oneBased);
            for (int i = 0; i < modes.Length; i++)
                Assert.IsTrue(HasMode(level, modes[i]), "Level {0} missing {1}", oneBased, modes[i]);
        }

        static bool HasMode(CampaignLevelDefinition level, ModeId id)
        {
            if (level?.Modes == null)
                return false;
            for (int i = 0; i < level.Modes.Length; i++)
            {
                if (level.Modes[i] == id)
                    return true;
            }
            return false;
        }

        static bool Has(GameState state, string from, string to)
        {
            if (!Square.TryParse(from, out Square fromSquare) || !Square.TryParse(to, out Square toSquare))
                return false;
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                if (move.From.Equals(fromSquare) && move.To.Equals(toSquare))
                    return true;
            }
            return false;
        }

        static GameState Apply(GameState state, string from, string to)
        {
            if (!Square.TryParse(from, out Square fromSquare) || !Square.TryParse(to, out Square toSquare))
                throw new ArgumentException(from + to);
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                if (move.From.Equals(fromSquare) && move.To.Equals(toSquare))
                    return state.Apply(move);
            }
            throw new InvalidOperationException("No legal move " + from + to);
        }

        static int CountTerrain(GameState state)
        {
            int n = 0;
            for (int i = 0; i < 64; i++)
            {
                if (state.Board.TerrainAt(Square.FromIndex(i)) != TerrainKind.None)
                    n++;
            }
            return n;
        }

        static int CountTinted(GameState state)
        {
            int n = 0;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = state.Board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Hue != 0)
                    n++;
            }
            return n;
        }
    }
}
