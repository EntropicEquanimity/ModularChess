using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class GameState
    {
        #region Fields
        readonly string[] _positionKeys;
        readonly Side[] _historySides;
        public Board Board { get; }
        public Side SideToMove { get; }
        public GameStatus Status { get; }
        public bool IsInCheck { get; }
        public Square? EnPassantTarget { get; }
        public IReadOnlyList<Move> LegalMoves { get; }
        public IReadOnlyList<Move> History { get; }
        public CastlingRights CastlingRights { get; }
        public int HalfmoveClock { get; }
        public int FullmoveNumber { get; }
        public MatchRules Rules { get; }
        public ModeRuntime Runtime { get; }
        public int MovesThisTurn => Runtime.MovesThisTurn;
        public bool TurnOpen => Runtime.ExtraMoveKingId != null
            || (Runtime.RallyArmed && MovesThisTurn > 0)
            || (Runtime.OverloadPieceId != null && Runtime.OverloadMovesMade < 2)
            || (Rules != null && Rules.Hooks.TurnStaysOpen(Runtime, Rules.Settings));
        public bool DraftPending => Runtime.PendingDraft != null;
        #endregion

        #region Public Methods
        public Side HistorySide(int index)
        {
            if (_historySides == null || index < 0 || index >= _historySides.Length)
                return index % 2 == 0 ? Side.White : Side.Black;
            return _historySides[index];
        }
        public static GameState StartingPosition(MatchRules rules = null) => Fen.Parse(Fen.StartingPosition, rules);
        public static GameState FromFen(string fen, MatchRules rules = null) => Fen.Parse(fen, rules);
        internal static GameState FromPosition(
            Board board,
            Side sideToMove,
            Square? enPassantTarget,
            CastlingRights castlingRights,
            int halfmoveClock,
            int fullmoveNumber,
            Move[] history = null,
            string[] previousPositionKeys = null,
            MatchRules rules = null,
            ModeRuntime runtime = null,
            GameStatus? forcedStatus = null)
        {
            return new GameState(
                board,
                sideToMove,
                enPassantTarget,
                castlingRights,
                halfmoveClock,
                fullmoveNumber,
                history ?? Array.Empty<Move>(),
                null,
                previousPositionKeys,
                rules,
                runtime,
                forcedStatus,
                appendPositionKey: previousPositionKeys == null || previousPositionKeys.Length == 0);
        }
        public string ToFen() => Fen.Format(this);
        public GameState Apply(Move move)
        {
            if (Status != GameStatus.InProgress)
                throw new InvalidOperationException("Cannot apply a move when the game is over.");
            if (Runtime.PendingDraft != null)
                throw new InvalidOperationException("Cannot apply a move during Draft.");
            if (!IsLegal(move))
                throw new InvalidOperationException("Move is not legal in the current position.");
            Piece moving = Board.GetPiece(move.From);
            Piece captured = FindCaptured(move);
            ModeRuntime nextRuntime = Runtime;
            Board nextBoard = Board;
            bool bounced = false;
            if (captured != null)
            {
                CaptureResolution resolved = Rules.Hooks.ResolveCapture(Board, move, captured, nextRuntime);
                nextRuntime = resolved.Runtime;
                if (resolved.Kind == CaptureResolutionKind.Negate)
                    bounced = true;
                else
                {
                    nextBoard = Board.ApplyUnchecked(move);
                    Square origin = move.Kind == MoveKind.EnPassant
                        ? new Square(move.To.File, move.From.Rank)
                        : move.To;
                    nextRuntime = nextRuntime.AddCapture(captured, false, origin);
                    if (Rules.Has(ModeId.Martyr))
                        nextRuntime = CreditLostMaterial(nextRuntime, captured);
                    nextBoard = ResolveMartyrAfterCapture(
                        nextBoard, move, moving, captured, origin, ref nextRuntime);
                }
            }
            else
            {
                nextBoard = Board.ApplyUnchecked(move);
                nextBoard = ResolveLandmine(nextBoard, move, moving, ref nextRuntime);
            }
            if (move.Kind == MoveKind.Promotion
                && nextRuntime.IsEmpowered(moving.Id)
                && moving.Type == PieceType.Pawn)
                nextRuntime = nextRuntime.WithoutEmpowered(moving.Id);
            if (nextRuntime.OverloadPieceId != null && nextRuntime.OverloadPieceId.Value == moving.Id)
            {
                int overloadMoves = nextRuntime.OverloadMovesMade + 1;
                nextRuntime = nextRuntime.WithOverload(moving.Id, overloadMoves);
                if (overloadMoves >= 2)
                    nextBoard = RemoveOverloadPiece(nextBoard, moving, move.To, ref nextRuntime);
            }
            CastlingRights nextCastling = bounced ? CastlingRights : CastlingRights.AfterMove(move, Board);
            Square? nextEnPassant = bounced ? null : ComputeEnPassantTarget(move, moving);
            bool resetsClock = moving.Type == PieceType.Pawn || (captured != null && !bounced);
            int nextHalfmove = resetsClock ? 0 : HalfmoveClock + 1;
            int nextMovesThisTurn = nextRuntime.MovesThisTurn + 1;
            bool paid = MoveCostsAction(moving, nextRuntime);
            int paidAfter = nextRuntime.PaidMovesThisTurn + (paid ? 1 : 0);
            bool usedRallyExtra = !paid
                && nextRuntime.RallyArmed
                && nextRuntime.MovesThisTurn > 0
                && !nextRuntime.RallyExtraSpent;
            bool endsTurn = MoveEndsTurn(moving, nextRuntime, paidAfter, usedRallyExtra);
            if (!endsTurn)
                nextRuntime = nextRuntime.WithMoved(moving.Id);
            Side nextSide = endsTurn ? SideToMove.Opponent() : SideToMove;
            int nextFullmove = SideToMove == Side.Black && endsTurn ? FullmoveNumber + 1 : FullmoveNumber;
            if (endsTurn)
            {
                nextBoard = FinishTurnSideEffects(nextBoard, ref nextRuntime);
                nextRuntime = MaybeOpenDraft(nextRuntime, nextSide, nextBoard);
            }
            else
            {
                Guid? extraKing = nextRuntime.ExtraMoveKingId;
                if (moving.Type == PieceType.King && nextRuntime.IsEmpowered(moving.Id))
                    extraKing = Runtime.ExtraMoveKingId == null ? moving.Id : null;
                nextRuntime = nextRuntime.WithExtraKing(extraKing)
                    .WithMovesThisTurn(nextMovesThisTurn)
                    .WithPaidMoves(paidAfter);
                if (usedRallyExtra)
                    nextRuntime = nextRuntime.WithRallyExtraSpent(true);
            }
            return new GameState(
                nextBoard,
                nextSide,
                nextEnPassant,
                nextCastling,
                nextHalfmove,
                nextFullmove,
                AppendHistory(move),
                AppendHistorySides(SideToMove),
                _positionKeys,
                Rules,
                nextRuntime,
                null,
                endsTurn);
        }
        public GameState EndTurn()
        {
            if (Status != GameStatus.InProgress)
                throw new InvalidOperationException("Cannot end a turn when the game is over.");
            if (!CanEndTurn())
                throw new InvalidOperationException("End Turn is not legal.");
            ModeRuntime nextRuntime = Runtime;
            Board nextBoard = FinishTurnSideEffects(Board, ref nextRuntime);
            Side nextSide = SideToMove.Opponent();
            nextRuntime = MaybeOpenDraft(nextRuntime, nextSide, nextBoard);
            int nextFullmove = SideToMove == Side.Black ? FullmoveNumber + 1 : FullmoveNumber;
            return new GameState(
                nextBoard,
                nextSide,
                null,
                CastlingRights,
                HalfmoveClock,
                nextFullmove,
                History as Move[] ?? CopyHistory(),
                CopyHistorySides(),
                _positionKeys,
                Rules,
                nextRuntime,
                null,
                true);
        }
        public bool CanEndTurn()
        {
            if (!TurnOpen || IsInCheck)
                return false;
            if (MovesThisTurn == 0 && LegalMoves.Count > 0 && !Rules.Settings.AllowEndTurnWithZeroMoves)
                return false;
            return true;
        }
        public GameState ConfirmEmpowered(IReadOnlyList<Guid> ids)
        {
            var extraLife = new List<Guid>();
            if (ids != null)
            {
                for (int i = 0; i < ids.Count; i++)
                {
                    Square? square = Board.FindSquare(ids[i]);
                    if (square == null)
                        continue;
                    Piece piece = Board.GetPiece(square.Value);
                    if (piece != null && piece.Type == PieceType.Knight)
                        extraLife.Add(piece.Id);
                }
            }
            ModeRuntime next = Runtime.WithEmpowered(ids, extraLife);
            next = MaybeOpenDraft(next, SideToMove, Board);
            return CloneWithRuntime(next);
        }
        public GameState WithSideToMove(Side side)
        {
            return new GameState(
                Board,
                side,
                EnPassantTarget,
                CastlingRights,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                CopyHistorySides(),
                _positionKeys,
                Rules,
                Runtime,
                Status == GameStatus.InProgress ? null : Status,
                false);
        }
        public GameState WithTerminal(GameStatus status)
        {
            if (status == GameStatus.InProgress)
                throw new ArgumentException("Terminal status required.", nameof(status));
            return new GameState(
                Board,
                SideToMove,
                EnPassantTarget,
                CastlingRights,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                CopyHistorySides(),
                _positionKeys,
                Rules,
                Runtime,
                status,
                false);
        }
        public GameState ApplyDraft(MartyrPower power, Guid? targetId, Square[] reinforcements)
        {
            if (Runtime.PendingDraft == null)
                throw new InvalidOperationException("No Draft is pending.");
            CastlingRights nextCastling = CastlingRights;
            if (targetId != null)
            {
                Square? square = Board.FindSquare(targetId.Value);
                if (square != null)
                    nextCastling = nextCastling.WithoutPieceSquare(square.Value);
            }
            ModeRuntime next = MartyrRules.Apply(this, power, targetId, reinforcements, out Board nextBoard);
            return new GameState(
                nextBoard,
                SideToMove,
                EnPassantTarget,
                nextCastling,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                CopyHistorySides(),
                _positionKeys,
                Rules,
                next,
                null,
                true);
        }
        public IReadOnlyList<Move> LegalMovesFrom(Square from)
        {
            var matches = new List<Move>();
            for (int i = 0; i < LegalMoves.Count; i++)
            {
                if (LegalMoves[i].From.Equals(from))
                    matches.Add(LegalMoves[i]);
            }
            return matches;
        }
        public bool IsExileTarget(Square square)
        {
            return MartyrRules.CanExile(Board, square, Rules, Runtime, SideToMove);
        }
        public bool IsVanishingActPiece(Square square)
        {
            Piece piece = Board.GetPiece(square);
            if (piece == null || piece.Side != SideToMove) return false;
            int? value = PieceValues.Get(piece.Type);
            if (value == null || value.Value <= 1) return false;
            return AttackMap.IsAttacked(Board, square, SideToMove.Opponent(), Rules, Runtime);
        }
        public static bool IsOwnHalf(Square square, Side side) => MartyrRules.IsOwnHalf(square, side);
        public static bool IsBackTwoRanks(Square square, Side side) => MartyrRules.IsBackTwoRanks(square, side);
        #endregion

        #region Private Methods
        GameState(
            Board board,
            Side sideToMove,
            Square? enPassantTarget,
            CastlingRights castlingRights,
            int halfmoveClock,
            int fullmoveNumber,
            Move[] history,
            Side[] historySides,
            string[] previousPositionKeys,
            MatchRules rules,
            ModeRuntime runtime,
            GameStatus? forcedStatus,
            bool appendPositionKey)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            SideToMove = sideToMove;
            EnPassantTarget = enPassantTarget;
            CastlingRights = castlingRights;
            HalfmoveClock = halfmoveClock;
            FullmoveNumber = fullmoveNumber;
            History = history ?? Array.Empty<Move>();
            _historySides = AlignHistorySides(History.Count, historySides);
            Rules = rules ?? MatchRules.CoreOnly;
            Runtime = runtime ?? ModeRuntime.Empty;
            IsInCheck = AttackMap.IsInCheck(board, sideToMove, Rules, Runtime);
            List<Move> legal = MoveGenerator.GenerateLegal(
                board, sideToMove, enPassantTarget, castlingRights, Rules, Runtime);
            LegalMoves = legal;
            Square? keyEnPassant = EffectiveEnPassant(enPassantTarget, legal);
            string key = Fen.PositionKey(board, sideToMove, castlingRights, keyEnPassant);
            if (appendPositionKey)
            {
                string[] keys = new string[(previousPositionKeys?.Length ?? 0) + 1];
                if (previousPositionKeys != null && previousPositionKeys.Length > 0)
                    Array.Copy(previousPositionKeys, keys, previousPositionKeys.Length);
                keys[keys.Length - 1] = key;
                _positionKeys = keys;
            }
            else
                _positionKeys = previousPositionKeys ?? new[] { key };
            if (forcedStatus != null)
                Status = forcedStatus.Value;
            else if (Runtime.PendingDraft != null || (TurnOpen && legal.Count == 0 && !IsInCheck))
                Status = GameStatus.InProgress;
            else
                Status = DrawEvaluator.Resolve(IsInCheck, legal.Count, halfmoveClock, _positionKeys, board);
        }
        bool MoveCostsAction(Piece moving, ModeRuntime runtime)
        {
            if (runtime.ExtraMoveKingId != null && runtime.ExtraMoveKingId.Value == moving.Id)
                return false;
            if (runtime.RallyArmed && runtime.MovesThisTurn > 0 && !runtime.RallyExtraSpent)
                return false;
            if (runtime.OverloadPieceId != null
                && runtime.OverloadPieceId.Value == moving.Id
                && runtime.OverloadMovesMade >= 2)
                return false;
            return true;
        }
        bool MoveEndsTurn(Piece moving, ModeRuntime runtime, int paidAfter, bool usedRallyExtra)
        {
            if (runtime.OverloadPieceId != null
                && runtime.OverloadPieceId.Value == moving.Id
                && runtime.OverloadMovesMade >= 2)
                return true;
            bool kingFollowUpOpens = moving.Type == PieceType.King
                && runtime.IsEmpowered(moving.Id)
                && Runtime.ExtraMoveKingId == null;
            bool rallyStill = runtime.RallyArmed && !runtime.RallyExtraSpent && !usedRallyExtra;
            bool overloadOpen = runtime.OverloadPieceId != null
                && runtime.OverloadPieceId.Value == moving.Id
                && runtime.OverloadMovesMade < 2;
            bool extrasOpen = kingFollowUpOpens || rallyStill || overloadOpen;
            if (Rules != null && Rules.Hooks.HasTurnEconomy)
                return !Rules.Hooks.KeepsTurnAfterMove(paidAfter, extrasOpen, Rules.Settings);
            int after = runtime.MovesThisTurn + 1;
            if (after >= 2) return true;
            if (runtime.RallyArmed) return false;
            if (kingFollowUpOpens) return false;
            return true;
        }
        Board FinishTurnSideEffects(Board board, ref ModeRuntime runtime)
        {
            if (runtime.OverloadPieceId != null && runtime.OverloadMovesMade < 2)
            {
                Square? square = board.FindSquare(runtime.OverloadPieceId.Value);
                if (square != null)
                {
                    Piece piece = board.GetPiece(square.Value);
                    if (piece != null)
                        board = RemoveOverloadPiece(board, piece, square.Value, ref runtime);
                }
                else
                    runtime = runtime.ClearOverload();
            }
            runtime = runtime.TickStatuses(SideToMove).TickSideEffects(SideToMove);
            board = MartyrRules.ResolveExpiredExiles(board, runtime, SideToMove, out runtime);
            runtime = runtime.WithExtraKing(null)
                .WithMovesThisTurn(0)
                .WithPaidMoves(0)
                .WithRally(false)
                .WithRallyExtraSpent(false)
                .ClearOverload()
                .ClearMovedThisTurn();
            return board;
        }
        Board ResolveMartyrAfterCapture(
            Board board,
            Move move,
            Piece moving,
            Piece captured,
            Square lossSquare,
            ref ModeRuntime runtime)
        {
            if (!Rules.Has(ModeId.Martyr)) return board;
            board = ResolveBloodDebt(board, move, moving, captured, lossSquare, ref runtime);
            board = ResolveLandmine(board, move, moving, ref runtime);
            return ResolveReserveCall(board, move, moving, captured, lossSquare, ref runtime);
        }
        Board ResolveBloodDebt(
            Board board,
            Move move,
            Piece moving,
            Piece captured,
            Square lossSquare,
            ref ModeRuntime runtime)
        {
            if (runtime.BloodDebtCharges(captured.Side) <= 0) return board;
            Square capturerSquare = move.Kind == MoveKind.Bombard ? move.From : move.To;
            Piece capturer = board.GetPiece(capturerSquare);
            if (capturer == null || capturer.Id != moving.Id) return board;
            if (capturer.Type == PieceType.King) return board;
            runtime = runtime.SpendBloodDebt(captured.Side);
            var retaliate = new Move(lossSquare, capturerSquare, MoveKind.Capture, capturedType: capturer.Type);
            CaptureResolution resolved = Rules.Hooks.ResolveCapture(board, retaliate, capturer, runtime);
            runtime = resolved.Runtime;
            if (resolved.Kind == CaptureResolutionKind.Negate) return board;
            runtime = runtime.AddCapture(capturer, false, capturerSquare);
            runtime = CreditLostMaterial(runtime, capturer);
            return board.WithPiece(capturerSquare, null);
        }
        Board ResolveReserveCall(
            Board board,
            Move move,
            Piece moving,
            Piece captured,
            Square lossSquare,
            ref ModeRuntime runtime)
        {
            if (!runtime.ReserveCallArmed(captured.Side) || captured.Type == PieceType.Pawn) return board;
            runtime = runtime.WithReserveCall(captured.Side, false);
            Square capturerSquare = move.Kind == MoveKind.Bombard ? move.From : move.To;
            Piece capturer = board.GetPiece(capturerSquare);
            if (capturer != null && capturer.Id == moving.Id)
            {
                board = board.WithPiece(capturerSquare, null);
                if (board.CanPlace(move.From))
                    board = board.WithPiece(move.From, capturer);
            }
            if (board.CanPlace(lossSquare))
            {
                var pawn = new Piece(PieceType.Pawn, captured.Side);
                board = board.WithPiece(lossSquare, pawn);
                runtime = runtime.AddSummoned(pawn.Id);
            }
            return board;
        }
        Board ResolveLandmine(Board board, Move move, Piece moving, ref ModeRuntime runtime)
        {
            if (!Rules.Has(ModeId.Martyr) || move.Kind == MoveKind.Bombard) return board;
            if (!runtime.TryGetLandmine(move.To, out LandmineMarker mine)) return board;
            if (mine.Owner == moving.Side) return board;
            Piece occupant = board.GetPiece(move.To);
            if (occupant == null || occupant.Id != moving.Id) return board;
            var detonate = new Move(move.To, move.To, MoveKind.Capture, capturedType: occupant.Type);
            CaptureResolution resolved = Rules.Hooks.ResolveCapture(board, detonate, occupant, runtime);
            runtime = resolved.Runtime.RemoveLandmineAt(move.To);
            if (resolved.Kind == CaptureResolutionKind.Negate) return board;
            runtime = runtime.AddCapture(occupant, false, move.To);
            runtime = CreditLostMaterial(runtime, occupant);
            return board.WithPiece(move.To, null);
        }
        Board RemoveOverloadPiece(Board board, Piece piece, Square square, ref ModeRuntime runtime)
        {
            Piece occupant = board.GetPiece(square);
            if (occupant == null || occupant.Id != piece.Id)
            {
                runtime = runtime.ClearOverload();
                return board;
            }
            runtime = runtime.AddCapture(occupant, false, square).ClearOverload();
            runtime = CreditLostMaterial(runtime, occupant);
            return board.WithPiece(square, null);
        }
        ModeRuntime CreditLostMaterial(ModeRuntime runtime, Piece piece)
        {
            if (runtime.IsSummoned(piece.Id))
                return runtime;
            int? value = PieceValues.Get(piece.Type);
            if (value == null)
                return runtime;
            return runtime.AddLostMaterial(piece.Side, value.Value, Rules.Settings.MartyrThreshold);
        }
        ModeRuntime MaybeOpenDraft(ModeRuntime runtime, Side sideToMove, Board board)
        {
            if (!Rules.Has(ModeId.Martyr) || runtime.PendingDraft != null)
                return runtime;
            int queued = sideToMove == Side.White ? runtime.WhiteDraftsQueued : runtime.BlackDraftsQueued;
            if (queued <= 0)
                return runtime;
            DraftOffer offer = MartyrRules.BuildOffer(this, runtime, sideToMove, board);
            PieceType? battlefield = offer.Contains(MartyrPower.BattlefieldPromotion)
                ? offer.BattlefieldType
                : null;
            return runtime.WithPendingDraft(offer, battlefield);
        }
        Piece FindCaptured(Move move)
        {
            switch (move.Kind)
            {
                case MoveKind.Capture:
                case MoveKind.Promotion:
                case MoveKind.Bombard:
                    return Board.GetPiece(move.To);
                case MoveKind.EnPassant:
                    return Board.GetPiece(new Square(move.To.File, move.From.Rank));
                case MoveKind.Quiet:
                case MoveKind.CastleKingSide:
                case MoveKind.CastleQueenSide:
                case MoveKind.Swap:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        GameState CloneWithRuntime(ModeRuntime runtime)
        {
            return new GameState(
                Board,
                SideToMove,
                EnPassantTarget,
                CastlingRights,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                CopyHistorySides(),
                _positionKeys,
                Rules,
                runtime,
                Status == GameStatus.InProgress ? null : Status,
                false);
        }
        Move[] CopyHistory()
        {
            var copy = new Move[History.Count];
            for (int i = 0; i < History.Count; i++)
                copy[i] = History[i];
            return copy;
        }
        Side[] CopyHistorySides()
        {
            var copy = new Side[History.Count];
            for (int i = 0; i < History.Count; i++)
                copy[i] = HistorySide(i);
            return copy;
        }
        Move[] AppendHistory(Move move)
        {
            var nextHistory = new Move[History.Count + 1];
            for (int i = 0; i < History.Count; i++)
                nextHistory[i] = History[i];
            nextHistory[History.Count] = move;
            return nextHistory;
        }
        Side[] AppendHistorySides(Side side)
        {
            var next = new Side[History.Count + 1];
            for (int i = 0; i < History.Count; i++)
                next[i] = HistorySide(i);
            next[History.Count] = side;
            return next;
        }
        static Side[] AlignHistorySides(int count, Side[] sides)
        {
            if (count <= 0)
                return Array.Empty<Side>();
            if (sides != null && sides.Length == count)
                return sides;
            var aligned = new Side[count];
            for (int i = 0; i < count; i++)
                aligned[i] = sides != null && i < sides.Length
                    ? sides[i]
                    : (i % 2 == 0 ? Side.White : Side.Black);
            return aligned;
        }
        bool IsLegal(Move move)
        {
            for (int i = 0; i < LegalMoves.Count; i++)
            {
                if (LegalMoves[i].Equals(move))
                    return true;
            }
            return false;
        }
        static Square? ComputeEnPassantTarget(Move move, Piece moving)
        {
            if (moving.Type != PieceType.Pawn) return null;
            if (Math.Abs(move.To.Rank - move.From.Rank) != 2) return null;
            int startRank = moving.Side == Side.White ? 1 : 6;
            if (move.From.Rank != startRank) return null;
            return new Square(move.From.File, (move.From.Rank + move.To.Rank) / 2);
        }
        static Square? EffectiveEnPassant(Square? enPassantTarget, List<Move> legalMoves)
        {
            if (enPassantTarget == null) return null;
            for (int i = 0; i < legalMoves.Count; i++)
            {
                if (legalMoves[i].Kind == MoveKind.EnPassant)
                    return enPassantTarget;
            }
            return null;
        }
        #endregion
    }
}
