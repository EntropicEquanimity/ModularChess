using System.Collections.Generic;

namespace ModularChess.Core
{
    internal static class TerrainLayouts
    {
        #region Fields
        const int TargetTiles = 26;
        #endregion

        #region Public Methods
        public static Board Apply(Board board, MatchSettings settings)
        {
            if (board == null || settings == null)
                return board;
            var cells = new TerrainKind[64];
            var rng = new SeededRng(settings.MatchSeed);
            switch (settings.TerrainLayout)
            {
                case TerrainLayoutKind.River:
                    PaintRiver(cells, board, settings, rng);
                    break;
                case TerrainLayoutKind.Woods:
                    Scatter(cells, board, settings, rng, TerrainKind.Forest, TargetTiles, null);
                    break;
                case TerrainLayoutKind.Peaks:
                    PaintPeaks(cells, board, settings, rng);
                    break;
                case TerrainLayoutKind.Border:
                    PaintBorder(cells, board, settings, rng);
                    break;
                default:
                    ScatterMixed(cells, board, settings, rng, TargetTiles);
                    break;
            }
            return board.WithTerrain(cells);
        }
        #endregion

        #region Private Methods
        static void PaintRiver(TerrainKind[] cells, Board board, MatchSettings settings, SeededRng rng)
        {
            for (int file = 0; file < 8; file++)
            {
                TrySet(cells, new Square(file, 3), TerrainKind.Swamp, board, settings);
                TrySet(cells, new Square(file, 4), TerrainKind.Swamp, board, settings);
            }
            var forestRanks = new[] { 0, 1, 6, 7 };
            Scatter(cells, board, settings, rng, TerrainKind.Forest, TargetTiles, forestRanks);
        }
        static void PaintPeaks(TerrainKind[] cells, Board board, MatchSettings settings, SeededRng rng)
        {
            var center = new List<Square>();
            for (int file = 1; file < 7; file++)
            {
                for (int rank = 2; rank < 6; rank++)
                    center.Add(new Square(file, rank));
            }
            Square[] order = center.ToArray();
            rng.Shuffle(order);
            int mountains = 12;
            for (int i = 0; i < order.Length && mountains > 0; i++)
            {
                if (TrySet(cells, order[i], TerrainKind.Mountain, board, settings))
                    mountains--;
            }
            Scatter(cells, board, settings, rng, TerrainKind.Forest, TargetTiles, null);
        }
        static void PaintBorder(TerrainKind[] cells, Board board, MatchSettings settings, SeededRng rng)
        {
            for (int rank = 0; rank < 8; rank++)
            {
                TrySet(cells, new Square(0, rank), TerrainKind.Mountain, board, settings);
                TrySet(cells, new Square(7, rank), TerrainKind.Mountain, board, settings);
            }
            Scatter(cells, board, settings, rng, TerrainKind.Forest, TargetTiles, null);
        }
        static void ScatterMixed(TerrainKind[] cells, Board board, MatchSettings settings, SeededRng rng, int target)
        {
            var kinds = new[] { TerrainKind.Swamp, TerrainKind.Forest, TerrainKind.Mountain };
            Square[] squares = AllSquares();
            rng.Shuffle(squares);
            int placed = CountFilled(cells);
            for (int i = 0; i < squares.Length && placed < target; i++)
            {
                TerrainKind kind = kinds[rng.Next(kinds.Length)];
                if (TrySet(cells, squares[i], kind, board, settings))
                    placed++;
            }
        }
        static void Scatter(
            TerrainKind[] cells,
            Board board,
            MatchSettings settings,
            SeededRng rng,
            TerrainKind kind,
            int target,
            int[] ranks)
        {
            var list = new List<Square>(64);
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (ranks != null && !Contains(ranks, square.Rank))
                    continue;
                list.Add(square);
            }
            Square[] squares = list.ToArray();
            rng.Shuffle(squares);
            int placed = CountFilled(cells);
            for (int i = 0; i < squares.Length && placed < target; i++)
            {
                if (TrySet(cells, squares[i], kind, board, settings))
                    placed++;
            }
        }
        static bool TrySet(TerrainKind[] cells, Square square, TerrainKind kind, Board board, MatchSettings settings)
        {
            if (!square.IsOnBoard)
                return false;
            int i = square.ToIndex();
            if (cells[i] != TerrainKind.None)
                return false;
            bool occupied = board.GetPiece(square) != null;
            if (kind == TerrainKind.Mountain && occupied)
                return false;
            if (occupied && settings != null && !settings.TerrainOnPieces)
                return false;
            cells[i] = kind;
            return true;
        }
        static int CountFilled(TerrainKind[] cells)
        {
            int n = 0;
            for (int i = 0; i < cells.Length; i++)
            {
                if (cells[i] != TerrainKind.None)
                    n++;
            }
            return n;
        }
        static Square[] AllSquares()
        {
            var squares = new Square[64];
            for (int i = 0; i < 64; i++)
                squares[i] = Square.FromIndex(i);
            return squares;
        }
        static bool Contains(int[] ranks, int rank)
        {
            for (int i = 0; i < ranks.Length; i++)
            {
                if (ranks[i] == rank)
                    return true;
            }
            return false;
        }
        #endregion
    }
}
