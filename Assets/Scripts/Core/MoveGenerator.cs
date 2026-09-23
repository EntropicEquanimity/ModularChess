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
            Rules rules = null,
            ModeRuntime runtime = null)
        {
            runtime = runtime ?? ModeRuntime.Empty;
            rules = rules ?? VersusRules.CoreOnly;
            ILaw law = rules.Law;
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

                if (!law.CheckFiltersMoves)
                {
                    legal.Add(move);
                    continue;
                }

                Board next = ApplyForLegality(board, move, rules, runtime);
                if (!AttackMap.IsInCheck(next, side, rules, RuntimeAfterMove(board, move, rules, runtime)))
                {
                    legal.Add(move);
                }
            }

            if (runtime.ExtraMoveKingId == null && law.KingMayMove)
            {
                AddCastling(board, side, castlingRights, legal, rules, runtime);
            }

            if (runtime.ExtraMoveKingId != null)
            {
                FilterToKing(board, legal, runtime.ExtraMoveKingId.Value);
            }

            return legal;
        }
        #endregion

        #region Private Methods
        private static List<Move> GeneratePseudoLegal(
            Board board,
            Side side,
            Square? enPassantTarget,
            Rules rules,
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
                            AddPawnMoves(board, from, piece, enPassantTarget, runtime, rules, moves);
                            break;
                        case PieceType.Knight:
                            AddLeaperMoves(
                                board,
                                from,
                                piece,
                                Directions.KnightFiles,
                                Directions.KnightRanks,
                                runtime,
                                LawOf(rules).AllowsKingCapture,
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
                                LawOf(rules).SliderRange,
                                LawOf(rules).AllowsKingCapture,
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
                                LawOf(rules).SliderRange,
                                LawOf(rules).AllowsKingCapture,
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
                                LawOf(rules).SliderRange,
                                LawOf(rules).AllowsKingCapture,
                                moves);
                            AddSliderMoves(
                                board,
                                from,
                                piece,
                                Directions.RookFiles,
                                Directions.RookRanks,
                                false,
                                LawOf(rules).SliderRange,
                                LawOf(rules).AllowsKingCapture,
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
                                    LawOf(rules).AllowsKingCapture,
                                    moves);
                            }

                            break;
                        case PieceType.King:
                            if (!LawOf(rules).KingMayMove)
                            {
                                break;
                            }
                            AddLeaperMoves(
                                board,
                                from,
                                piece,
                                Directions.KingFiles,
                                Directions.KingRanks,
                                runtime,
                                LawOf(rules).AllowsKingCapture,
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
            Rules rules,
            List<Move> moves)
        {
            int forward = pawn.Side == Side.White ? 1 : -1;
            int startRank = pawn.Side == Side.White ? 1 : 6;
            int promotionRank = pawn.Side == Side.White ? 7 : 0;
            bool fleet = runtime.FleetPawns(pawn.Side);
            bool allowsPromotion = LawOf(rules).AllowsPromotion;
            bool allowsKingCapture = LawOf(rules).AllowsKingCapture;

            Square one = from.Offset(0, forward);
            if (one.IsOnBoard && board.GetPiece(one) == null)
            {
                AddPawnAdvance(from, one, promotionRank, allowsPromotion, moves);
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
                board, from, from.Offset(-1, forward), pawn, enPassantTarget, promotionRank, allowsPromotion, allowsKingCapture, moves);
            AddPawnCapture(
                board, from, from.Offset(1, forward), pawn, enPassantTarget, promotionRank, allowsPromotion, allowsKingCapture, moves);
        }
        private static void AddPawnAdvance(Square from, Square to, int promotionRank, bool allowsPromotion, List<Move> moves)
        {
            if (to.Rank == promotionRank && allowsPromotion)
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
            bool allowsPromotion,
            bool allowsKingCapture,
            List<Move> moves)
        {
            if (!to.IsOnBoard)
            {
                return;
            }

            Piece occupant = board.GetPiece(to);
            if (occupant != null && occupant.Side != pawn.Side && (allowsKingCapture || occupant.Type != PieceType.King))
            {
                if (to.Rank == promotionRank && allowsPromotion)
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
            bool allowsKingCapture,
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

                if (occupant.Side != piece.Side && (allowsKingCapture || occupant.Type != PieceType.King))
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
            int sliderRange,
            bool allowsKingCapture,
            List<Move> moves)
        {
            for (int i = 0; i < fileDeltas.Length; i++)
            {
                Pattern.Ray(board, from, fileDeltas[i], rankDeltas[i], RayBuffer);
                int limit = RayBuffer.Count;
                if (sliderRange < limit)
                {
                    limit = sliderRange;
                }
                for (int s = 0; s < limit; s++)
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
                    if (allowsKingCapture || occupant.Type != PieceType.King)
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
            Rules rules,
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
        private static Board ApplyForLegality(Board board, Move move, Rules rules, ModeRuntime runtime)
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
        private static ModeRuntime RuntimeAfterMove(Board board, Move move, Rules rules, ModeRuntime runtime)
        {
            Piece captured = CapturedPiece(board, move);
            if (captured == null)
            {
                return runtime;
            }

            return HooksOf(rules).ResolveCapture(board, move, captured, runtime).Runtime;
        }
        private static ModeHooks HooksOf(Rules rules)
        {
            return rules != null ? rules.Hooks : ModeHooks.None;
        }
        private static ILaw LawOf(Rules rules)
        {
            return rules != null ? rules.Law : FideLaw.Instance;
        }
        private static void FilterToKing(Board board, List<Move> legal, Guid kingId)
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
        private static void AddCastling(
            Board board,
            Side side,
            CastlingRights rights,
            List<Move> legal,
            Rules rules,
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
            Rules rules,
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
