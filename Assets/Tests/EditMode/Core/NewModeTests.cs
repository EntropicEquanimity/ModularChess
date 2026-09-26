using ModularChess.Core;
using NUnit.Framework;

namespace ModularChess.Core.Tests
{
    public class NewModeTests
    {
        [Test]
        public void ActionEconomy_AllowsThreePaidMovesThenSwitchesSide()
        {
            MatchRules rules = new MatchRules(
                new[] { ModeId.ActionEconomy },
                new MatchSettings(actionPoints: 3));
            GameState state = GameState.StartingPosition(rules);
            state = MoveTestHelper.Play(state, "e2e4");
            Assert.AreEqual(Side.White, state.SideToMove);
            Assert.IsTrue(state.CanEndTurn());
            state = MoveTestHelper.Play(state, "d2d4");
            Assert.AreEqual(Side.White, state.SideToMove);
            state = MoveTestHelper.Play(state, "a2a3");
            Assert.AreEqual(Side.Black, state.SideToMove);
        }

        [Test]
        public void ActionEconomy_CannotMoveTheSamePieceTwice()
        {
            MatchRules rules = new MatchRules(
                new[] { ModeId.ActionEconomy },
                new MatchSettings(actionPoints: 3));
            GameState state = GameState.StartingPosition(rules);
            state = MoveTestHelper.Play(state, "e2e4");
            Assert.AreEqual(Side.White, state.SideToMove);
            Assert.IsFalse(MoveTestHelper.Has(state, "e4", "e5"));
            Assert.IsTrue(MoveTestHelper.Has(state, "d2", "d4"));
        }

        [Test]
        public void RiverSpansTheBoardWithEmptyBuffer()
        {
            MatchSettings settings = new MatchSettings(
                terrainLayout: TerrainLayoutKind.River,
                matchSeed: 1,
                terrainOnPieces: false);
            MatchRules rules = new MatchRules(new[] { ModeId.ComplexTerrain }, settings);
            GameState state = GameState.StartingPosition(rules);
            Assert.AreEqual(TerrainKind.Swamp, state.Board.TerrainAt(new Square(0, 3)));
            Assert.AreEqual(TerrainKind.Swamp, state.Board.TerrainAt(new Square(7, 4)));
            Assert.AreEqual(TerrainKind.None, state.Board.TerrainAt(new Square(3, 2)));
            Assert.AreEqual(TerrainKind.None, state.Board.TerrainAt(new Square(3, 5)));
            Assert.AreNotEqual(TerrainKind.Swamp, state.Board.TerrainAt(new Square(4, 0)));
        }

        [Test]
        public void RandomizerPlacement_SpreadsAcrossFiles()
        {
            MatchSettings settings = new MatchSettings(
                matchSeed: 99,
                randomShuffle: false,
                randomColors: false,
                randomPlacement: true);
            MatchRules rules = new MatchRules(new[] { ModeId.Randomizer }, settings);
            GameState state = GameState.StartingPosition(rules);
            int files = 0;
            for (int file = 0; file < 8; file++)
            {
                bool used = false;
                for (int rank = 0; rank < 4; rank++)
                {
                    if (state.Board.GetPiece(new Square(file, rank)) != null)
                        used = true;
                }
                if (used)
                    files++;
            }
            Assert.GreaterOrEqual(files, 5);
        }

        [Test]
        public void SwampStopsSliderAndAllowsLanding()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 0 1");
            Board board = state.Board.WithTerrain(new Square(0, 3), TerrainKind.Swamp);
            state = GameState.FromPosition(
                board,
                state.SideToMove,
                state.EnPassantTarget,
                state.CastlingRights,
                state.HalfmoveClock,
                state.FullmoveNumber);
            Assert.IsTrue(MoveTestHelper.Has(state, "a1", "a4"));
            Assert.IsFalse(MoveTestHelper.Has(state, "a1", "a5"));
            Assert.IsFalse(MoveTestHelper.Has(state, "a1", "a8"));
        }

        [Test]
        public void MountainBlocksLandingAndSliderPath()
        {
            GameState state = GameState.FromFen("4k3/8/8/8/8/8/8/R3K3 w - - 0 1");
            Board board = state.Board.WithTerrain(new Square(0, 3), TerrainKind.Mountain);
            state = GameState.FromPosition(
                board,
                state.SideToMove,
                state.EnPassantTarget,
                state.CastlingRights,
                state.HalfmoveClock,
                state.FullmoveNumber);
            Assert.IsFalse(MoveTestHelper.Has(state, "a1", "a4"));
            Assert.IsFalse(MoveTestHelper.Has(state, "a1", "a8"));
            Assert.IsTrue(MoveTestHelper.Has(state, "a1", "a3"));
        }

        [Test]
        public void RandomizerShuffle_IsDeterministicAndKeepsKings()
        {
            MatchSettings settings = new MatchSettings(
                matchSeed: 42,
                randomShuffle: true,
                randomColors: false,
                randomPlacement: false);
            MatchRules rules = new MatchRules(new[] { ModeId.Randomizer }, settings);
            GameState a = GameState.StartingPosition(rules);
            GameState b = GameState.StartingPosition(rules);
            Assert.AreEqual(a.ToFen(), b.ToFen());
            Piece whiteKing = a.Board.GetPiece(new Square(4, 0));
            Piece blackKing = a.Board.GetPiece(new Square(4, 7));
            Assert.NotNull(whiteKing);
            Assert.NotNull(blackKing);
            Assert.AreEqual(PieceType.King, whiteKing.Type);
            Assert.AreEqual(PieceType.King, blackKing.Type);
        }

        [Test]
        public void RandomizerColors_TintsPieces()
        {
            MatchSettings settings = new MatchSettings(
                matchSeed: 7,
                randomShuffle: false,
                randomColors: true,
                randomPlacement: false);
            MatchRules rules = new MatchRules(new[] { ModeId.Randomizer }, settings);
            GameState state = GameState.StartingPosition(rules);
            Piece pawn = state.Board.GetPiece(new Square(0, 1));
            Assert.NotNull(pawn);
            Assert.AreNotEqual(0, pawn.Hue);
        }

        [Test]
        public void ForestHidesEnemyWithoutIdentifiedVision()
        {
            MatchRules rules = new MatchRules(
                new[] { ModeId.ComplexTerrain },
                MatchSettings.Default);
            GameState state = GameState.FromFen("4k3/8/8/8/4n3/8/8/4K3 w - - 0 1");
            Board board = state.Board.WithTerrain(new Square(4, 3), TerrainKind.Forest);
            state = GameState.FromPosition(
                board,
                state.SideToMove,
                state.EnPassantTarget,
                state.CastlingRights,
                state.HalfmoveClock,
                state.FullmoveNumber,
                rules: rules);
            VisionMap vision = VisionMap.Compute(state, Side.White);
            Assert.AreEqual(SquareSight.Hidden, vision[new Square(4, 3)]);
        }
    }
}
