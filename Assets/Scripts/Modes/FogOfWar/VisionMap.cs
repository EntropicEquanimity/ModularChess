using System;

namespace ModularChess.Core
{
    public sealed class VisionMap
    {
        readonly SquareSight[] _cells;
        VisionMap(SquareSight[] cells)
        {
            _cells = cells;
        }
        public SquareSight this[Square square]
        {
            get
            {
                if (!square.IsOnBoard) return SquareSight.Hidden;
                return _cells[square.ToIndex()];
            }
        }
        public bool IsIdentified(Square square) => this[square] == SquareSight.Identified;
        public static VisionMap AllIdentified { get; } = Fill(SquareSight.Identified);
        public static VisionMap Compute(GameState state, Side viewer)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (state.Status != GameStatus.InProgress) return AllIdentified;
            bool fog = state.Rules != null && state.Rules.Has(ModeId.FogOfWar);
            bool terrain = state.Rules != null && state.Rules.Has(ModeId.ComplexTerrain);
            SquareSight[] cells;
            if (fog)
            {
                cells = FogVision.ComputeCells(state, viewer);
            }
            else
            {
                cells = FillCells(SquareSight.Identified);
                if (terrain)
                    HideForestCover(cells, state, viewer);
            }
            ApplyDustCloud(cells, state, viewer);
            ApplyFogVisionPower(cells, state, viewer);
            return new VisionMap(cells);
        }
        public static VisionMap Fill(SquareSight sight)
        {
            return new VisionMap(FillCells(sight));
        }
        internal static VisionMap FromCells(SquareSight[] cells)
        {
            return new VisionMap(cells);
        }
        static SquareSight[] FillCells(SquareSight sight)
        {
            var cells = new SquareSight[Square.BoardSize * Square.BoardSize];
            for (int i = 0; i < cells.Length; i++) cells[i] = sight;
            return cells;
        }
        static void HideForestCover(SquareSight[] cells, GameState state, Side viewer)
        {
            SquareSight[] vision = FogVision.ComputeCells(state, viewer);
            Board board = state.Board;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (board.TerrainAt(square) != TerrainKind.Forest)
                    continue;
                Piece piece = board.GetPiece(square);
                if (piece == null || piece.Side == viewer)
                    continue;
                if (vision[i] != SquareSight.Identified)
                    cells[i] = SquareSight.Hidden;
            }
        }
        static void ApplyDustCloud(SquareSight[] cells, GameState state, Side viewer)
        {
            Side opponent = viewer.Opponent();
            if (state.Runtime.DustCloudTurns(opponent) <= 0) return;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (!MartyrRules.IsOwnHalf(square, opponent)) continue;
                cells[i] = SquareSight.Hidden;
            }
        }
        static void ApplyFogVisionPower(SquareSight[] cells, GameState state, Side viewer)
        {
            if (state.Runtime.FogVisionTurns(viewer) <= 0) return;
            Board board = state.Board;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Side != viewer)
                {
                    cells[i] = SquareSight.Identified;
                }
            }
        }
    }
}
