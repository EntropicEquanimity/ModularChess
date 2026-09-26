using System.Collections.Generic;

namespace ModularChess.Core
{
    internal static class RandomizerRules
    {
        #region Public Methods
        public static Board Apply(Board board, MatchSettings settings, ref CastlingRights castling)
        {
            if (board == null || settings == null)
                return board;
            bool shuffle = settings.RandomShuffle;
            bool colors = settings.RandomColors;
            bool placement = settings.RandomPlacement;
            if (!shuffle && !colors && !placement)
                shuffle = true;
            var rng = new SeededRng(settings.MatchSeed ^ 7919);
            if (placement)
                board = Place(board, rng);
            else if (shuffle)
                board = Shuffle(board, rng);
            if (colors)
                board = Tint(board, rng);
            castling = FitCastling(board, castling);
            return board;
        }
        #endregion

        #region Private Methods
        static Board Shuffle(Board board, SeededRng rng)
        {
            Piece[] next = CopyPieces(board);
            ShuffleSide(next, Side.White, rng);
            ShuffleSide(next, Side.Black, rng);
            return new Board(next, CopyTerrain(board));
        }
        static void ShuffleSide(Piece[] squares, Side side, SeededRng rng)
        {
            var indexes = new List<int>(16);
            var pieces = new List<Piece>(16);
            for (int i = 0; i < squares.Length; i++)
            {
                Piece piece = squares[i];
                if (piece == null || piece.Side != side || piece.Type == PieceType.King)
                    continue;
                indexes.Add(i);
                pieces.Add(piece);
            }
            Piece[] arr = pieces.ToArray();
            rng.Shuffle(arr);
            for (int i = 0; i < indexes.Count; i++)
                squares[indexes[i]] = arr[i];
        }
        static Board Place(Board board, SeededRng rng)
        {
            Piece[] next = new Piece[64];
            PlaceSide(board, next, Side.White, rng);
            PlaceSide(board, next, Side.Black, rng);
            return new Board(next, CopyTerrain(board));
        }
        static void PlaceSide(Board source, Piece[] dest, Side side, SeededRng rng)
        {
            int back = side == Side.White ? 0 : 7;
            int minRank = side == Side.White ? 0 : 4;
            int maxRank = side == Side.White ? 3 : 7;
            Piece king = null;
            var others = new List<Piece>(16);
            for (int i = 0; i < 64; i++)
            {
                Piece piece = source.GetPiece(Square.FromIndex(i));
                if (piece == null || piece.Side != side)
                    continue;
                if (piece.Type == PieceType.King)
                    king = piece;
                else
                    others.Add(piece);
            }
            var backEmpty = new List<int>();
            var halfEmpty = new List<int>();
            for (int file = 0; file < 8; file++)
            {
                for (int rank = minRank; rank <= maxRank; rank++)
                {
                    var square = new Square(file, rank);
                    if (source.TerrainAt(square) == TerrainKind.Mountain)
                        continue;
                    int index = square.ToIndex();
                    if (rank == back)
                        backEmpty.Add(index);
                    halfEmpty.Add(index);
                }
            }
            if (king != null && backEmpty.Count > 0)
            {
                int pick = rng.Next(backEmpty.Count);
                int kingIndex = backEmpty[pick];
                dest[kingIndex] = king;
                halfEmpty.Remove(kingIndex);
            }
            PlaceSpread(dest, others.ToArray(), halfEmpty, rng);
        }
        static void PlaceSpread(Piece[] dest, Piece[] pieces, List<int> empty, SeededRng rng)
        {
            rng.Shuffle(pieces);
            var byFile = new List<int>[8];
            for (int f = 0; f < 8; f++)
                byFile[f] = new List<int>();
            for (int i = 0; i < empty.Count; i++)
            {
                Square square = Square.FromIndex(empty[i]);
                byFile[square.File].Add(empty[i]);
            }
            var placedOnFile = new int[8];
            for (int i = 0; i < 64; i++)
            {
                if (dest[i] == null)
                    continue;
                placedOnFile[Square.FromIndex(i).File]++;
            }
            for (int p = 0; p < pieces.Length; p++)
            {
                int bestFile = -1;
                int bestCount = int.MaxValue;
                for (int f = 0; f < 8; f++)
                {
                    if (byFile[f].Count == 0)
                        continue;
                    int count = placedOnFile[f];
                    if (bestFile < 0 || count < bestCount || (count == bestCount && rng.Next(2) == 0))
                    {
                        bestFile = f;
                        bestCount = count;
                    }
                }
                if (bestFile < 0)
                    break;
                int slot = rng.Next(byFile[bestFile].Count);
                int index = byFile[bestFile][slot];
                byFile[bestFile].RemoveAt(slot);
                dest[index] = pieces[p];
                placedOnFile[bestFile]++;
            }
        }
        static Board Tint(Board board, SeededRng rng)
        {
            Piece[] next = CopyPieces(board);
            for (int i = 0; i < next.Length; i++)
            {
                if (next[i] == null)
                    continue;
                byte hue = (byte)(1 + rng.Next(255));
                next[i] = next[i].WithHue(hue);
            }
            return new Board(next, CopyTerrain(board));
        }
        static CastlingRights FitCastling(Board board, CastlingRights rights)
        {
            CastlingRights next = rights;
            if (!HomeKingAndRook(board, Side.White, true))
                next = next.WithoutKingSide(Side.White);
            if (!HomeKingAndRook(board, Side.White, false))
                next = next.WithoutQueenSide(Side.White);
            if (!HomeKingAndRook(board, Side.Black, true))
                next = next.WithoutKingSide(Side.Black);
            if (!HomeKingAndRook(board, Side.Black, false))
                next = next.WithoutQueenSide(Side.Black);
            return next;
        }
        static bool HomeKingAndRook(Board board, Side side, bool kingSide)
        {
            int rank = side == Side.White ? 0 : 7;
            Piece king = board.GetPiece(new Square(4, rank));
            if (king == null || king.Type != PieceType.King || king.Side != side || king.HasMoved)
                return false;
            int rookFile = kingSide ? 7 : 0;
            Piece rook = board.GetPiece(new Square(rookFile, rank));
            return rook != null && rook.Type == PieceType.Rook && rook.Side == side && !rook.HasMoved;
        }
        static Piece[] CopyPieces(Board board)
        {
            var squares = new Piece[64];
            for (int i = 0; i < 64; i++)
                squares[i] = board.GetPiece(Square.FromIndex(i));
            return squares;
        }
        static TerrainKind[] CopyTerrain(Board board)
        {
            var cells = new TerrainKind[64];
            for (int i = 0; i < 64; i++)
                cells[i] = board.TerrainAt(Square.FromIndex(i));
            return cells;
        }
        #endregion
    }
}
