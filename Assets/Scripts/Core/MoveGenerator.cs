using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    internal static class MoveGenerator
    {
        #region Fields
        private static readonly PieceType[] PromotionTypes =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };
        static readonly List<PatternStep> RayBuffer = new List<PatternStep>(8);
        #endregion

        #region Public Methods
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
                if (!IsAllowedByRuntime(board, move, runtime))
                {
                    continue;
                }

                Board next = ApplyForLegality(board, move, rules, runtime);
                ModeRuntime afterRuntime = RuntimeAfterMove(board, move, rules, runtime);
                if (AttackMap.IsInCheck(next, side, rules, afterRuntime))
                {
                    continue;
                }
                if (runtime.OverloadPieceId != null
                    && DeliversCheck(board, move, side, rules, runtime, next, afterRuntime))
                {
                    continue;
                }
                legal.Add(move);
            }

            if (runtime.ExtraMoveKingId == null)
            {
                AddCastling(board, side, castlingRights, legal, rules, runtime);
            }

            if (runtime.ExtraMoveKingId != null)
            {
                FilterToPiece(board, legal, runtime.ExtraMoveKingId.Value);
            }
            if (runtime.OverloadPieceId != null)
            {
                FilterToPiece(board, legal, runtime.OverloadPieceId.Value);
            }
            return legal;
        }
        #endregion

        #region Private Methods
        private static List<Move> GeneratePseudoLegal(
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
                            AddLeaperMoves(
                                board,
                                from,
                                piece,
                                Directions.KnightFiles,
                                Directions.KnightRanks,
                                runtime,
                                moves);
                            break;
                        case PieceType.Bishop:
                            AddSliderMoves(
                                board,
                                from,
                                piece,
                                Directions.BishopFiles,
                                Directions.BishopRanks,
                                false,
                                moves);
                            break;
                        case PieceType.Rook:
                            AddSliderMoves(
                                board,
                                from,
                                piece,
                                Directions.RookFiles,
                                Directions.RookRanks,
                                runtime.IsEmpowered(piece.Id),
                                moves);
                            break;
                        case PieceType.Queen:
                            AddSliderMoves(
                                board,
                                from,
                                piece,
                                Directions.BishopFiles,
                                Directions.BishopRanks,
                                false,
                                moves);
                            AddSliderMoves(
                                board,
                                from,
                                piece,
                                Directions.RookFiles,
                                Directions.RookRanks,
                                false,
                                moves);
                            if (runtime.IsEmpowered(piece.Id))
                            {
                                AddLeaperMoves(
                                    board,
                                    from,
                                    piece,
                                    Directions.KnightFiles,
                                    Directions.KnightRanks,
                                    runtime,
                                    moves);
                            }

                            break;
                        case PieceType.King:
                            AddLeaperMoves(
                                board,
                                from,
                                piece,
                                Directions.KingFiles,
                                Directions.KingRanks,
                                runtime,
                                moves);
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

            if (runtime.IsEmpowered(pawn.Id))
            {
                return;
            }

            AddPawnCapture(
                board, from, from.Offset(-1, forward), pawn, enPassantTarget, promotionRank, moves);
            AddPawnCapture(
                board, from, from.Offset(1, forward), pawn, enPassantTarget, promotionRank, moves);
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
        private static void AddSliderMoves(
            Board board,
            Square from,
            Piece piece,
            int[] fileDeltas,
            int[] rankDeltas,
            bool passAllies,
            List<Move> moves)
        {
            for (int i = 0; i < fileDeltas.Length; i++)
            {
                Pattern.Ray(board, from, fileDeltas[i], rankDeltas[i], RayBuffer);
                for (int s = 0; s < RayBuffer.Count; s++)
                {
                    PatternStep step = RayBuffer[s];
                    Piece occupant = step.Occupant;
                    if (occupant == null)
                    {
                        moves.Add(new Move(from, step.Square, MoveKind.Quiet));
                        continue;
                    }
                    if (occupant.Side == piece.Side)
                    {
                        if (passAllies)
                        {
                            continue;
                        }
                        break;
                    }
                    if (occupant.Type != PieceType.King)
                    {
                        moves.Add(new Move(from, step.Square, MoveKind.Capture, capturedType: occupant.Type));
                    }
                    break;
                }
            }
        }
        private static void AddModeMoves(
            Board board,
            Side side,
            MatchRules rules,
            ModeRuntime runtime,
            List<Move> moves)
        {
            HooksOf(rules).AppendMoves(board, side, runtime, moves);
        }
        private static bool IsAllowedByRuntime(Board board, Move move, ModeRuntime runtime)
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
                if (runtime.IsEmpowered(captured.Id)
                    && captured.Type == PieceType.Pawn
                    && !Pattern.SuperPawnAllowsCapture(PawnSquare(move), captured.Side, move.From))
                {
                    return false;
                }
            }
            Side opponent = moving.Side.Opponent();
            if (runtime.IronCurtainTurns(opponent) > 0
                && MartyrRules.IsBackTwoRanks(move.To, opponent)
                && move.Kind != MoveKind.Bombard)
            {
                return false;
            }
            return true;
        }
        private static Square PawnSquare(Move move)
        {
            if (move.Kind == MoveKind.EnPassant)
            {
                return new Square(move.To.File, move.From.Rank);
            }

            return move.To;
        }
        private static Piece CapturedPiece(Board board, Move move)
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
        private static Board ApplyForLegality(Board board, Move move, MatchRules rules, ModeRuntime runtime)
        {
            Piece captured = CapturedPiece(board, move);
            if (captured != null)
            {
                CaptureResolution resolved = HooksOf(rules).ResolveCapture(board, move, captured, runtime);
                if (resolved.Kind == CaptureResolutionKind.Negate)
                {
                    return board;
                }
            }

            return board.ApplyUnchecked(move);
        }
        private static ModeRuntime RuntimeAfterMove(Board board, Move move, MatchRules rules, ModeRuntime runtime)
        {
            Piece captured = CapturedPiece(board, move);
            if (captured == null)
            {
                return runtime;
            }

            return HooksOf(rules).ResolveCapture(board, move, captured, runtime).Runtime;
        }
        private static ModeHooks HooksOf(MatchRules rules)
        {
            return rules != null ? rules.Hooks : ModeHooks.None;
        }
        private static void FilterToPiece(Board board, List<Move> legal, Guid pieceId)
        {
            for (int i = legal.Count - 1; i >= 0; i--)
            {
                Piece piece = board.GetPiece(legal[i].From);
                if (piece == null || piece.Id != pieceId)
                {
                    legal.RemoveAt(i);
                }
            }
        }
        private static bool DeliversCheck(
            Board board,
            Move move,
            Side side,
            MatchRules rules,
            ModeRuntime runtime,
            Board next,
            ModeRuntime afterRuntime)
        {
            Piece moving = board.GetPiece(move.From);
            if (moving == null || runtime.OverloadPieceId == null || moving.Id != runtime.OverloadPieceId.Value)
            {
                return false;
            }
            return AttackMap.IsInCheck(next, side.Opponent(), rules, afterRuntime);
        }
        private static void FilterToKing(Board board, List<Move> legal, Guid kingId)
        {
            FilterToPiece(board, legal, kingId);
        }
        private static void AddCastling(
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

            if (rights.HasKingSide(side)
                && CanCastle(board, from, kingFile: 6, rookFile: 7, throughFile: 5, side, rules, runtime))
            {
                legal.Add(new Move(from, new Square(6, backRank), MoveKind.CastleKingSide));
            }

            if (rights.HasQueenSide(side)
                && CanCastle(board, from, kingFile: 2, rookFile: 0, throughFile: 3, side, rules, runtime))
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
        #endregion
    }
}
