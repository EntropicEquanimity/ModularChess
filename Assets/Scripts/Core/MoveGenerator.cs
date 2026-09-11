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
            CastlingRights castlingRights,
            MatchRules rules = null,
            ModeRuntime runtime = null)
        {
            runtime = runtime ?? ModeRuntime.Empty;
            List<Move> pseudo = GeneratePseudoLegal(board, side, enPassantTarget, rules, runtime);
            AddModeMoves(board, side, rules, runtime, pseudo);
            List<Move> legal = new List<Move>(pseudo.Count + 2);
            for (int i = 0; i < pseudo.Count; i++)
            {
                Move move = pseudo[i];
                if (!IsAllowedByRuntime(board, move, rules, runtime))
                {
                    continue;
                }

                Board next = ApplyForLegality(board, move, runtime);
                if (!AttackMap.IsInCheck(next, side, rules, RuntimeAfterMove(board, move, runtime)))
                {
                    legal.Add(move);
                }
            }

            if (runtime.ExtraMoveKingId == null)
            {
                AddCastling(board, side, castlingRights, legal, rules, runtime);
            }

            if (runtime.ExtraMoveKingId != null)
            {
                FilterToKing(board, legal, runtime.ExtraMoveKingId.Value);
            }

            return legal;
        }

        static List<Move> GeneratePseudoLegal(
            Board board,
            Side side,
            Square? enPassantTarget,
            MatchRules rules,
            ModeRuntime runtime)
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

                    if (runtime.HasStatus(piece.Id, StatusKind.Stasis))
                    {
                        continue;
                    }

                    switch (piece.Type)
                    {
                        case PieceType.Pawn:
                            AddPawnMoves(board, from, piece, enPassantTarget, runtime, moves);
                            break;
                        case PieceType.Knight:
                            AddLeaperMoves(board, from, piece, Directions.KnightFiles, Directions.KnightRanks, runtime, moves);
                            break;
                        case PieceType.Bishop:
                            AddSliderMoves(board, from, piece, Directions.BishopFiles, Directions.BishopRanks, false, runtime, moves);
                            break;
                        case PieceType.Rook:
                            AddSliderMoves(
                                board,
                                from,
                                piece,
                                Directions.RookFiles,
                                Directions.RookRanks,
                                runtime.IsEmpowered(piece.Id),
                                runtime,
                                moves);
                            break;
                        case PieceType.Queen:
                            AddSliderMoves(board, from, piece, Directions.BishopFiles, Directions.BishopRanks, false, runtime, moves);
                            AddSliderMoves(board, from, piece, Directions.RookFiles, Directions.RookRanks, false, runtime, moves);
                            if (runtime.IsEmpowered(piece.Id))
                            {
                                AddLeaperMoves(board, from, piece, Directions.KnightFiles, Directions.KnightRanks, runtime, moves);
                            }

                            break;
                        case PieceType.King:
                            AddLeaperMoves(board, from, piece, Directions.KingFiles, Directions.KingRanks, runtime, moves);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            return moves;
        }

        static void AddPawnMoves(
            Board board,
            Square from,
            Piece pawn,
            Square? enPassantTarget,
            ModeRuntime runtime,
            List<Move> moves)
        {
            int forward = pawn.Side == Side.White ? 1 : -1;
            int startRank = pawn.Side == Side.White ? 1 : 6;
            int promotionRank = pawn.Side == Side.White ? 7 : 0;
            bool fleet = runtime.FleetPawns(pawn.Side);

            Square one = from.Offset(0, forward);
            if (one.IsOnBoard && board.GetPiece(one) == null)
            {
                AddPawnAdvance(from, one, promotionRank, moves);
                bool canDouble = from.Rank == startRank || fleet;
                if (canDouble)
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

        static void AddPawnAdvance(Square from, Square to, int promotionRank, List<Move> moves)
        {
            if (to.Rank == promotionRank)
            {
                AddPromotions(from, to, null, moves);
                return;
            }

            moves.Add(new Move(from, to, MoveKind.Quiet));
        }

        static void AddPawnCapture(
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

        static void AddPromotions(Square from, Square to, PieceType? capturedType, List<Move> moves)
        {
            for (int i = 0; i < PromotionTypes.Length; i++)
            {
                moves.Add(new Move(from, to, MoveKind.Promotion, PromotionTypes[i], capturedType));
            }
        }

        static void AddLeaperMoves(
            Board board,
            Square from,
            Piece piece,
            int[] fileDeltas,
            int[] rankDeltas,
            ModeRuntime runtime,
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

        static void AddSliderMoves(
            Board board,
            Square from,
            Piece piece,
            int[] fileDeltas,
            int[] rankDeltas,
            bool passAllies,
            ModeRuntime runtime,
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
                        if (occupant.Side == piece.Side)
                        {
                            if (passAllies)
                            {
                                cursor = cursor.Offset(fileDeltas[i], rankDeltas[i]);
                                continue;
                            }

                            break;
                        }

                        if (occupant.Type != PieceType.King)
                        {
                            moves.Add(new Move(from, cursor, MoveKind.Capture, capturedType: occupant.Type));
                        }

                        break;
                    }

                    cursor = cursor.Offset(fileDeltas[i], rankDeltas[i]);
                }
            }
        }

        static void AddModeMoves(Board board, Side side, MatchRules rules, ModeRuntime runtime, List<Move> moves)
        {
            if (rules == null || rules.IsCoreOnly)
            {
                return;
            }

            if (rules.Has(ModeId.PowerfulPieces))
            {
                AddBishopSwaps(board, side, runtime, moves);
            }

            if (rules.Has(ModeId.Martyr))
            {
                AddBombards(board, side, runtime, moves);
            }
        }

        static void AddBishopSwaps(Board board, Side side, ModeRuntime runtime, List<Move> moves)
        {
            for (int i = 0; i < 64; i++)
            {
                Square from = Square.FromIndex(i);
                Piece piece = board.GetPiece(from);
                if (piece == null || piece.Side != side || piece.Type != PieceType.Bishop)
                {
                    continue;
                }

                if (!runtime.IsEmpowered(piece.Id) || runtime.HasStatus(piece.Id, StatusKind.Stasis))
                {
                    continue;
                }

                for (int d = 0; d < Directions.KingFiles.Length; d++)
                {
                    Square to = from.Offset(Directions.KingFiles[d], Directions.KingRanks[d]);
                    if (!to.IsOnBoard)
                    {
                        continue;
                    }

                    Piece occupant = board.GetPiece(to);
                    if (occupant != null && occupant.Side == side && occupant.Type == PieceType.Pawn)
                    {
                        moves.Add(new Move(from, to, MoveKind.Swap));
                    }
                }
            }
        }

        static void AddBombards(Board board, Side side, ModeRuntime runtime, List<Move> moves)
        {
            if (!runtime.Bombard(side))
            {
                return;
            }

            for (int i = 0; i < 64; i++)
            {
                Square from = Square.FromIndex(i);
                Piece piece = board.GetPiece(from);
                if (piece == null || piece.Side != side || piece.Type != PieceType.Rook)
                {
                    continue;
                }

                if (runtime.HasStatus(piece.Id, StatusKind.Stasis))
                {
                    continue;
                }

                for (int d = 0; d < Directions.RookFiles.Length; d++)
                {
                    int distance = 0;
                    Square cursor = from.Offset(Directions.RookFiles[d], Directions.RookRanks[d]);
                    while (cursor.IsOnBoard)
                    {
                        distance++;
                        Piece occupant = board.GetPiece(cursor);
                        if (occupant == null)
                        {
                            cursor = cursor.Offset(Directions.RookFiles[d], Directions.RookRanks[d]);
                            continue;
                        }

                        if (occupant.Side != side && occupant.Type != PieceType.King && distance >= 5)
                        {
                            moves.Add(new Move(from, cursor, MoveKind.Bombard, capturedType: occupant.Type));
                        }

                        break;
                    }
                }
            }
        }

        static bool IsAllowedByRuntime(Board board, Move move, MatchRules rules, ModeRuntime runtime)
        {
            Piece moving = board.GetPiece(move.From);
            if (moving == null)
            {
                return false;
            }

            Piece captured = CapturedPiece(board, move);
            if (captured != null)
            {
                if (!AttackMap.CanTarget(runtime, captured))
                {
                    return false;
                }

                if (move.Kind == MoveKind.EnPassant
                    && rules != null
                    && rules.Has(ModeId.PowerfulPieces)
                    && runtime.IsEmpowered(captured.Id)
                    && captured.Type == PieceType.Pawn)
                {
                    return false;
                }

                if (rules != null
                    && rules.Has(ModeId.PowerfulPieces)
                    && runtime.IsEmpowered(captured.Id)
                    && captured.Type == PieceType.Pawn
                    && !SuperPawn.CanCaptureFrom(move.To, captured.Side, move.From, moving.Type)
                    && move.Kind != MoveKind.EnPassant)
                {
                    return false;
                }
            }

            return true;
        }

        static Piece CapturedPiece(Board board, Move move)
        {
            switch (move.Kind)
            {
                case MoveKind.Capture:
                case MoveKind.Promotion:
                case MoveKind.Bombard:
                    return board.GetPiece(move.To);
                case MoveKind.EnPassant:
                    return board.GetPiece(new Square(move.To.File, move.From.Rank));
                case MoveKind.Quiet:
                case MoveKind.CastleKingSide:
                case MoveKind.CastleQueenSide:
                case MoveKind.Swap:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        static Board ApplyForLegality(Board board, Move move, ModeRuntime runtime)
        {
            Piece captured = CapturedPiece(board, move);
            if (captured != null
                && captured.Type == PieceType.Knight
                && runtime.ExtraLifeAvailable(captured.Id)
                && move.Kind != MoveKind.Bombard)
            {
                return board;
            }

            if (captured != null && runtime.ExtraLifeAvailable(captured.Id) && captured.Type != PieceType.Pawn)
            {
                return board;
            }

            return board.ApplyUnchecked(move);
        }

        static ModeRuntime RuntimeAfterMove(Board board, Move move, ModeRuntime runtime)
        {
            Piece captured = CapturedPiece(board, move);
            if (captured != null && runtime.ExtraLifeAvailable(captured.Id))
            {
                return runtime.SpendExtraLife(captured.Id);
            }

            return runtime;
        }

        static void FilterToKing(Board board, List<Move> legal, Guid kingId)
        {
            for (int i = legal.Count - 1; i >= 0; i--)
            {
                Piece piece = board.GetPiece(legal[i].From);
                if (piece == null || piece.Id != kingId)
                {
                    legal.RemoveAt(i);
                }
            }
        }

        static void AddCastling(
            Board board,
            Side side,
            CastlingRights rights,
            List<Move> legal,
            MatchRules rules,
            ModeRuntime runtime)
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

            Piece king = board.GetPiece(from);
            if (king != null && runtime.HasStatus(king.Id, StatusKind.Stasis))
            {
                return;
            }

            if (AttackMap.IsAttacked(board, from, side.Opponent(), rules, runtime))
            {
                return;
            }

            if (rights.HasKingSide(side) && CanCastle(board, from, kingFile: 6, rookFile: 7, throughFile: 5, side, rules, runtime))
            {
                legal.Add(new Move(from, new Square(6, backRank), MoveKind.CastleKingSide));
            }

            if (rights.HasQueenSide(side) && CanCastle(board, from, kingFile: 2, rookFile: 0, throughFile: 3, side, rules, runtime))
            {
                legal.Add(new Move(from, new Square(2, backRank), MoveKind.CastleQueenSide));
            }
        }

        static bool CanCastle(
            Board board,
            Square kingFrom,
            int kingFile,
            int rookFile,
            int throughFile,
            Side side,
            MatchRules rules,
            ModeRuntime runtime)
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
            if (AttackMap.IsAttacked(board, through, enemy, rules, runtime)
                || AttackMap.IsAttacked(board, dest, enemy, rules, runtime))
            {
                return false;
            }

            return true;
        }
    }
}
