using System;

namespace ModularChess.Core
{
    internal static class FogVision
    {
        public static VisionMap Compute(GameState state, Side viewer)
        {
            return VisionMap.FromCells(ComputeCells(state, viewer));
        }
        internal static SquareSight[] ComputeCells(GameState state, Side viewer)
        {
            var cells = new SquareSight[Square.BoardSize * Square.BoardSize];
            Board board = state.Board;
            ModeRuntime runtime = state.Runtime;
            int homeMin = viewer == Side.White ? 0 : 6;
            int homeMax = viewer == Side.White ? 1 : 7;
            for (int file = 0; file < Square.BoardSize; file++)
            {
                for (int rank = homeMin; rank <= homeMax; rank++)
                {
                    cells[new Square(file, rank).ToIndex()] = SquareSight.Identified;
                }
            }
            for (int i = 0; i < 64; i++)
            {
                Square from = Square.FromIndex(i);
                Piece piece = board.GetPiece(from);
                if (piece != null && piece.Side == viewer)
                {
                    cells[i] = SquareSight.Identified;
                    GrantFromPiece(board, runtime, state.EnPassantTarget, from, piece, cells);
                }
            }
            return cells;
        }

        static void GrantFromPiece(
            Board board,
            ModeRuntime runtime,
            Square? enPassantTarget,
            Square from,
            Piece piece,
            SquareSight[] cells)
        {
            bool empowered = runtime != null && runtime.IsEmpowered(piece.Id);
            switch (piece.Type)
            {
                case PieceType.Pawn:
                    GrantPawn(board, from, piece.Side, enPassantTarget, cells);
                    break;
                case PieceType.Knight:
                    GrantLeaper(from, Directions.KnightFiles, Directions.KnightRanks, cells);
                    break;
                case PieceType.Bishop:
                    GrantRay(board, from, Directions.BishopFiles, Directions.BishopRanks, false, cells);
                    break;
                case PieceType.Rook:
                    GrantRay(board, from, Directions.RookFiles, Directions.RookRanks, empowered, cells);
                    break;
                case PieceType.Queen:
                    GrantRay(board, from, Directions.BishopFiles, Directions.BishopRanks, false, cells);
                    GrantRay(board, from, Directions.RookFiles, Directions.RookRanks, false, cells);
                    if (empowered)
                    {
                        GrantLeaper(from, Directions.KnightFiles, Directions.KnightRanks, cells);
                    }

                    break;
                case PieceType.King:
                    GrantLeaper(from, Directions.KingFiles, Directions.KingRanks, cells);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static void GrantPawn(Board board, Square from, Side side, Square? enPassantTarget, SquareSight[] cells)
        {
            int forward = side == Side.White ? 1 : -1;
            int startRank = side == Side.White ? 1 : 6;
            Square one = from.Offset(0, forward);
            MarkIdentified(one, cells);
            if (one.IsOnBoard && board.GetPiece(one) == null && from.Rank == startRank)
            {
                Square two = from.Offset(0, forward * 2);
                MarkIdentified(two, cells);
            }

            MarkIdentified(from.Offset(-1, forward), cells);
            MarkIdentified(from.Offset(1, forward), cells);

            if (enPassantTarget != null)
            {
                Square ep = enPassantTarget.Value;
                Square jumped = new Square(ep.File, from.Rank);
                Piece jumpedPawn = board.GetPiece(jumped);
                if (jumpedPawn != null && jumpedPawn.Side != side && jumpedPawn.Type == PieceType.Pawn)
                {
                    if (from.Offset(-1, forward) == ep || from.Offset(1, forward) == ep)
                    {
                        MarkIdentified(jumped, cells);
                    }
                }
            }
        }

        static void GrantLeaper(Square from, int[] files, int[] ranks, SquareSight[] cells)
        {
            for (int i = 0; i < files.Length; i++)
            {
                MarkIdentified(from.Offset(files[i], ranks[i]), cells);
            }
        }

        static void GrantRay(
            Board board,
            Square from,
            int[] files,
            int[] ranks,
            bool passAllies,
            SquareSight[] cells)
        {
            for (int i = 0; i < files.Length; i++)
            {
                Square cursor = from.Offset(files[i], ranks[i]);
                while (cursor.IsOnBoard)
                {
                    Piece occupant = board.GetPiece(cursor);
                    MarkIdentified(cursor, cells);
                    if (TerrainRules.BlocksVisionThrough(board, cursor))
                    {
                        break;
                    }
                    if (occupant == null)
                    {
                        cursor = cursor.Offset(files[i], ranks[i]);
                        continue;
                    }

                    if (passAllies && occupant.Side == board.GetPiece(from).Side)
                    {
                        cursor = cursor.Offset(files[i], ranks[i]);
                        continue;
                    }

                    break;
                }
            }
        }

        static void MarkIdentified(Square square, SquareSight[] cells)
        {
            if (!square.IsOnBoard)
            {
                return;
            }

            cells[square.ToIndex()] = SquareSight.Identified;
        }
    }
}
