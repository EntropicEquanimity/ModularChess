using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    internal static class MoveGenerator
    {
        private static readonly PieceType[] PromotionTypes =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };

        public static List<Move> GenerateLegal(
            Board board,
            Side side,
            Square? enPassantTarget,
            CastlingRights castlingRights)
        {
            List<Move> pseudo = GeneratePseudoLegal(board, side, enPassantTarget);
            List<Move> legal = new List<Move>(pseudo.Count + 2);
            for (int i = 0; i < pseudo.Count; i++)
            {
                Move move = pseudo[i];
                Board next = board.ApplyUnchecked(move);
                if (!AttackMap.IsInCheck(next, side))
                {
                    legal.Add(move);
                }
            }

            AddCastling(board, side, castlingRights, legal);
            return legal;
        }

        private static List<Move> GeneratePseudoLegal(Board board, Side side, Square? enPassantTarget)
        {
            List<Move> moves = new List<Move>(64);
            for (int file = 0; file < Square.BoardSize; file++)
            {
                for (int rank = 0; rank < Square.BoardSize; rank++)
                {
                    Square from = new Square(file, rank);
                    Piece piece = board.GetPiece(from);
                    if (piece == null || piece.Side != side)
                    {
                        continue;
                    }

                    switch (piece.Type)
                    {
                        case PieceType.Pawn:
                            AddPawnMoves(board, from, piece, enPassantTarget, moves);
                            break;
                        case PieceType.Knight:
                            AddLeaperMoves(board, from, piece, Directions.KnightFiles, Directions.KnightRanks, moves);
                            break;
                        case PieceType.Bishop:
                            AddSliderMoves(board, from, piece, Directions.BishopFiles, Directions.BishopRanks, moves);
                            break;
                        case PieceType.Rook:
                            AddSliderMoves(board, from, piece, Directions.RookFiles, Directions.RookRanks, moves);
                            break;
                        case PieceType.Queen:
                            AddSliderMoves(board, from, piece, Directions.BishopFiles, Directions.BishopRanks, moves);
                            AddSliderMoves(board, from, piece, Directions.RookFiles, Directions.RookRanks, moves);
                            break;
                        case PieceType.King:
                            AddLeaperMoves(board, from, piece, Directions.KingFiles, Directions.KingRanks, moves);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return moves;
        }

        private static void AddPawnMoves(
            Board board,
            Square from,
            Piece pawn,
            Square? enPassantTarget,
            List<Move> moves)
        {
            int forward = pawn.Side == Side.White ? 1 : -1;
            int startRank = pawn.Side == Side.White ? 1 : 6;
            int promotionRank = pawn.Side == Side.White ? 7 : 0;

            Square one = from.Offset(0, forward);
            if (one.IsOnBoard && board.GetPiece(one) == null)
            {
                AddPawnAdvance(from, one, promotionRank, moves);
                if (from.Rank == startRank)
                {
                    Square two = from.Offset(0, forward * 2);
                    if (two.IsOnBoard && board.GetPiece(two) == null)
                    {
                        moves.Add(new Move(from, two, MoveKind.Quiet));
                    }
                }
            }

            AddPawnCapture(board, from, from.Offset(-1, forward), pawn, enPassantTarget, promotionRank, moves);
            AddPawnCapture(board, from, from.Offset(1, forward), pawn, enPassantTarget, promotionRank, moves);
        }

        private static void AddPawnAdvance(Square from, Square to, int promotionRank, List<Move> moves)
        {
            if (to.Rank == promotionRank)
            {
                AddPromotions(from, to, null, moves);
                return;
            }

            moves.Add(new Move(from, to, MoveKind.Quiet));
        }

        private static void AddPawnCapture(
            Board board,
            Square from,
            Square to,
            Piece pawn,
            Square? enPassantTarget,
            int promotionRank,
            List<Move> moves)
        {
            if (!to.IsOnBoard)
            {
                return;
            }

            Piece occupant = board.GetPiece(to);
            if (occupant != null && occupant.Side != pawn.Side && occupant.Type != PieceType.King)
            {
                if (to.Rank == promotionRank)
                {
                    AddPromotions(from, to, occupant.Type, moves);
                    return;
                }

                moves.Add(new Move(from, to, MoveKind.Capture, capturedType: occupant.Type));
                return;
            }

            if (enPassantTarget != null && to == enPassantTarget.Value)
            {
                Square capturedSquare = new Square(to.File, from.Rank);
                Piece captured = board.GetPiece(capturedSquare);
                if (captured != null && captured.Side != pawn.Side && captured.Type == PieceType.Pawn)
                {
                    moves.Add(new Move(from, to, MoveKind.EnPassant, capturedType: PieceType.Pawn));
                }
            }
        }

        private static void AddPromotions(Square from, Square to, PieceType? capturedType, List<Move> moves)
        {
            for (int i = 0; i < PromotionTypes.Length; i++)
            {
                moves.Add(new Move(from, to, MoveKind.Promotion, PromotionTypes[i], capturedType));
            }
        }

        private static void AddLeaperMoves(
            Board board,
            Square from,
            Piece piece,
            int[] fileDeltas,
            int[] rankDeltas,
            List<Move> moves)
        {
            for (int i = 0; i < fileDeltas.Length; i++)
            {
                Square to = from.Offset(fileDeltas[i], rankDeltas[i]);
                if (!to.IsOnBoard)
                {
                    continue;
                }

                Piece occupant = board.GetPiece(to);
                if (occupant == null)
                {
                    moves.Add(new Move(from, to, MoveKind.Quiet));
                    continue;
                }

                if (occupant.Side != piece.Side && occupant.Type != PieceType.King)
                {
                    moves.Add(new Move(from, to, MoveKind.Capture, capturedType: occupant.Type));
                }
            }
        }

        private static void AddSliderMoves(
            Board board,
            Square from,
            Piece piece,
            int[] fileDeltas,
            int[] rankDeltas,
            List<Move> moves)
        {
            for (int i = 0; i < fileDeltas.Length; i++)
            {
                Square cursor = from.Offset(fileDeltas[i], rankDeltas[i]);
                while (cursor.IsOnBoard)
                {
                    Piece occupant = board.GetPiece(cursor);
                    if (occupant == null)
                    {
                        moves.Add(new Move(from, cursor, MoveKind.Quiet));
                    }
                    else
                    {
                        if (occupant.Side != piece.Side && occupant.Type != PieceType.King)
                        {
                            moves.Add(new Move(from, cursor, MoveKind.Capture, capturedType: occupant.Type));
                        }

                        break;
                    }

                    cursor = cursor.Offset(fileDeltas[i], rankDeltas[i]);
                }
            }
        }

        private static void AddCastling(Board board, Side side, CastlingRights rights, List<Move> legal)
        {
            Square? kingSquare = board.FindKing(side);
            if (kingSquare == null)
            {
                return;
            }

            Square from = kingSquare.Value;
            int backRank = side == Side.White ? 0 : 7;
            if (from.File != 4 || from.Rank != backRank)
            {
                return;
            }

            if (AttackMap.IsAttacked(board, from, side.Opponent()))
            {
                return;
            }

            if (rights.HasKingSide(side) && CanCastle(board, from, kingFile: 6, rookFile: 7, throughFile: 5, side))
            {
                legal.Add(new Move(from, new Square(6, backRank), MoveKind.CastleKingSide));
            }

            if (rights.HasQueenSide(side) && CanCastle(board, from, kingFile: 2, rookFile: 0, throughFile: 3, side))
            {
                legal.Add(new Move(from, new Square(2, backRank), MoveKind.CastleQueenSide));
            }
        }

        private static bool CanCastle(
            Board board,
            Square kingFrom,
            int kingFile,
            int rookFile,
            int throughFile,
            Side side)
        {
            Square rookFrom = new Square(rookFile, kingFrom.Rank);
            Piece rook = board.GetPiece(rookFrom);
            if (rook == null || rook.Type != PieceType.Rook || rook.Side != side)
            {
                return false;
            }

            int step = kingFile > kingFrom.File ? 1 : -1;
            for (int file = kingFrom.File + step; file != rookFile; file += step)
            {
                if (board.GetPiece(new Square(file, kingFrom.Rank)) != null)
                {
                    return false;
                }
            }

            Side enemy = side.Opponent();
            Square through = new Square(throughFile, kingFrom.Rank);
            Square dest = new Square(kingFile, kingFrom.Rank);
            if (AttackMap.IsAttacked(board, through, enemy) || AttackMap.IsAttacked(board, dest, enemy))
            {
                return false;
            }

            return true;
        }
    }
}
