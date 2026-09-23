using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class GameState
    {
        #region Fields
        private readonly string[] _positionKeys;
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
        public Rules Rules { get; }
        public ModeRuntime Runtime { get; }
        public int MovesThisTurn => Runtime.MovesThisTurn;
        public bool TurnOpen => Runtime.ExtraMoveKingId != null || (Runtime.RallyArmed && MovesThisTurn > 0);
        public bool DraftPending => Runtime.PendingDraft != null;
        #endregion

        #region Public Methods
        public static GameState StartingPosition(Rules rules = null)
        {
            return Fen.Parse(Fen.StartingPosition, rules);
        }
        public static GameState FromFen(string fen, Rules rules = null)
        {
            return Fen.Parse(fen, rules);
        }
        internal static GameState FromPosition(
            Board board,
            Side sideToMove,
            Square? enPassantTarget,
            CastlingRights castlingRights,
            int halfmoveClock,
            int fullmoveNumber,
            Move[] history = null,
            string[] previousPositionKeys = null,
            Rules rules = null,
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
            {
                throw new InvalidOperationException("Cannot apply a move when the game is over.");
            }

            if (Runtime.PendingDraft != null)
            {
                throw new InvalidOperationException("Cannot apply a move during Draft.");
            }

            if (!IsLegal(move))
            {
                throw new InvalidOperationException("Move is not legal in the current position.");
            }

            Piece moving = Board.GetPiece(move.From);
            Piece captured = FindCaptured(move);
            ModeRuntime nextRuntime = Runtime;
            Board nextBoard = Board;
            bool bounced = false;
            GameStatus? captureStatus = null;

            if (captured != null)
            {
                CaptureResolution resolved = Rules.Hooks.ResolveCapture(Board, move, captured, nextRuntime);
                nextRuntime = resolved.Runtime;
                if (resolved.Kind == CaptureResolutionKind.Negate)
                {
                    bounced = true;
                }
                else
                {
                    nextBoard = Board.ApplyUnchecked(move);
                    Square origin = move.Kind == MoveKind.EnPassant
                        ? new Square(move.To.File, move.From.Rank)
                        : move.To;
                    nextRuntime = nextRuntime.AddCapture(captured, false, origin);
                    nextRuntime = Rules.Hooks.AfterCaptureRemoved(captured, Rules, nextRuntime);
                    if (Rules.PlayerSide != null)
                    {
                        captureStatus = Rules.Law.ResolveCapture(
                            captured,
                            Rules.PlayerSide.Value,
                            Rules.StageTarget,
                            nextBoard);
                    }
                }
            }
            else
            {
                nextBoard = Board.ApplyUnchecked(move);
            }

            if (move.Kind == MoveKind.Promotion
                && nextRuntime.IsEmpowered(moving.Id)
                && moving.Type == PieceType.Pawn)
            {
                nextRuntime = nextRuntime.WithoutEmpowered(moving.Id);
            }

            CastlingRights nextCastling = bounced ? CastlingRights : CastlingRights.AfterMove(move, Board);
            Square? nextEnPassant = bounced ? null : ComputeEnPassantTarget(move, moving);
            bool resetsClock = moving.Type == PieceType.Pawn || (captured != null && !bounced);
            int nextHalfmove = resetsClock ? 0 : HalfmoveClock + 1;
            int nextMovesThisTurn = nextRuntime.MovesThisTurn + 1;
            bool endsTurn = MoveEndsTurn(moving, move, nextRuntime);
            Side nextSide = endsTurn ? SideToMove.Opponent() : SideToMove;
            int nextFullmove = SideToMove == Side.Black && endsTurn ? FullmoveNumber + 1 : FullmoveNumber;

            if (endsTurn)
            {
                nextRuntime = nextRuntime.TickStatuses(SideToMove);
                nextBoard = Rules.Hooks.OnTurnEnd(nextBoard, nextRuntime, SideToMove, out nextRuntime);
                nextRuntime = nextRuntime.WithExtraKing(null).WithMovesThisTurn(0).WithRally(false);
                nextRuntime = Rules.Hooks.MaybeOpenDraft(this, nextRuntime, nextSide, nextBoard);
            }
            else
            {
                Guid? extraKing = moving.Type == PieceType.King && nextRuntime.IsEmpowered(moving.Id)
                    ? moving.Id
                    : nextRuntime.ExtraMoveKingId;
                nextRuntime = nextRuntime.WithExtraKing(extraKing).WithMovesThisTurn(nextMovesThisTurn);
            }

            Move[] nextHistory = AppendHistory(move);
            return new GameState(
                nextBoard,
                nextSide,
                nextEnPassant,
                nextCastling,
                nextHalfmove,
                nextFullmove,
                nextHistory,
                _positionKeys,
                Rules,
                nextRuntime,
                captureStatus,
                true);
        }
        public GameState EndTurn()
        {
            if (Status != GameStatus.InProgress)
            {
                throw new InvalidOperationException("Cannot end a turn when the game is over.");
            }

            if (!CanEndTurn())
            {
                throw new InvalidOperationException("End Turn is not legal.");
            }

            ModeRuntime nextRuntime = Runtime.TickStatuses(SideToMove);
            Board nextBoard = Rules.Hooks.OnTurnEnd(Board, nextRuntime, SideToMove, out nextRuntime);
            nextRuntime = nextRuntime.WithExtraKing(null).WithMovesThisTurn(0).WithRally(false);
            Side nextSide = SideToMove.Opponent();
            nextRuntime = Rules.Hooks.MaybeOpenDraft(this, nextRuntime, nextSide, nextBoard);
            int nextFullmove = SideToMove == Side.Black ? FullmoveNumber + 1 : FullmoveNumber;
            return new GameState(
                nextBoard,
                nextSide,
                null,
                CastlingRights,
                HalfmoveClock,
                nextFullmove,
                History as Move[] ?? CopyHistory(),
                _positionKeys,
                Rules,
                nextRuntime,
                null,
                true);
        }
        public bool CanEndTurn()
        {
            if (!TurnOpen)
            {
                return false;
            }

            if (IsInCheck)
            {
                return false;
            }

            if (MovesThisTurn == 0 && !Rules.Settings.AllowEndTurnWithZeroMoves)
            {
                return false;
            }

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
                    {
                        continue;
                    }

                    Piece piece = Board.GetPiece(square.Value);
                    if (piece != null && piece.Type == PieceType.Knight)
                    {
                        extraLife.Add(piece.Id);
                    }
                }
            }

            ModeRuntime next = Runtime.WithEmpowered(ids, extraLife);
            next = Rules.Hooks.MaybeOpenDraft(this, next, SideToMove, Board);
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
                _positionKeys,
                Rules,
                Runtime,
                Status == GameStatus.InProgress ? null : Status,
                false);
        }
        public GameState RelocateFriendly(Square from, Square to)
        {
            Piece moving = Board.GetPiece(from);
            if (moving == null || moving.Type == PieceType.King)
            {
                throw new InvalidOperationException("Cannot relocate that Piece.");
            }
            if (!Board.CanPlace(to))
            {
                throw new InvalidOperationException("Destination is not empty.");
            }
            Board next = Board.WithPiece(from, null).WithPiece(to, moving);
            return new GameState(
                next,
                SideToMove,
                EnPassantTarget,
                CastlingRights,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                _positionKeys,
                Rules,
                Runtime,
                Status == GameStatus.InProgress ? null : Status,
                false);
        }
        public GameState PrepareRearrange(Side player, IReadOnlyDictionary<Guid, Square> homes)
        {
            if (homes == null)
                throw new ArgumentNullException(nameof(homes));
            Board next = Board.Empty();
            var pending = new List<(Piece piece, Square preferred)>(16);
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = Board.GetPiece(square);
                if (piece == null || piece.Side != player)
                    continue;
                if (Runtime.IsSummoned(piece.Id))
                    continue;
                Square preferred = homes.TryGetValue(piece.Id, out Square home) ? home : square;
                if (piece.Type == PieceType.King)
                {
                    if (!next.CanPlace(preferred))
                        preferred = square;
                    if (!next.CanPlace(preferred))
                        continue;
                    next = next.WithPiece(preferred, piece);
                    continue;
                }
                pending.Add((piece, preferred));
            }
            for (int i = 0; i < pending.Count; i++)
            {
                Piece piece = pending[i].piece;
                Square preferred = pending[i].preferred;
                if (next.CanPlace(preferred))
                {
                    next = next.WithPiece(preferred, piece);
                    continue;
                }
                Square? fallback = FindEmptyNear(next, preferred, player);
                if (fallback == null)
                    continue;
                next = next.WithPiece(fallback.Value, piece);
            }
            return new GameState(
                next,
                player,
                null,
                CastlingRights.None,
                HalfmoveClock,
                FullmoveNumber,
                Array.Empty<Move>(),
                null,
                Rules,
                Runtime,
                null,
                false);
        }
        public GameState AddPiece(Piece piece, Square square)
        {
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));
            if (!Board.CanPlace(square))
                throw new InvalidOperationException("Destination is not empty.");
            return new GameState(
                Board.WithPiece(square, piece),
                SideToMove,
                EnPassantTarget,
                CastlingRights,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                _positionKeys,
                Rules,
                Runtime,
                Status == GameStatus.InProgress ? null : Status,
                false);
        }
        static Square? FindEmptyNear(Board board, Square preferred, Side player)
        {
            if (board.CanPlace(preferred))
                return preferred;
            int back = player == Side.White ? 0 : 7;
            int forward = player == Side.White ? 1 : -1;
            for (int depth = 0; depth < 4; depth++)
            {
                int rank = back + forward * depth;
                if (rank < 0 || rank >= Square.BoardSize)
                    break;
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    Square square = new Square(file, rank);
                    if (board.CanPlace(square))
                        return square;
                }
            }
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (board.CanPlace(square))
                    return square;
            }
            return null;
        }
        public GameState WithTerminal(GameStatus status)
        {
            if (status == GameStatus.InProgress)
            {
                throw new ArgumentException("Terminal status required.", nameof(status));
            }

            return new GameState(
                Board,
                SideToMove,
                EnPassantTarget,
                CastlingRights,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                _positionKeys,
                Rules,
                Runtime,
                status,
                false);
        }
        public GameState ApplyDraft(MartyrPower power, Guid? targetId, Square[] reinforcements)
        {
            if (Runtime.PendingDraft == null)
            {
                throw new InvalidOperationException("No Draft is pending.");
            }
            CastlingRights nextCastling = CastlingRights;
            if (targetId != null)
            {
                Square? square = Board.FindSquare(targetId.Value);
                if (square != null)
                {
                    nextCastling = nextCastling.WithoutPieceSquare(square.Value);
                }
            }
            ModeRuntime next = MartyrRules.Apply(
                this,
                power,
                targetId,
                reinforcements,
                out Board nextBoard);
            return new GameState(
                nextBoard,
                SideToMove,
                EnPassantTarget,
                nextCastling,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                _positionKeys,
                Rules,
                next,
                null,
                true);
        }
        public IReadOnlyList<Move> LegalMovesFrom(Square from)
        {
            List<Move> matches = new List<Move>();
            for (int i = 0; i < LegalMoves.Count; i++)
            {
                if (LegalMoves[i].From.Equals(from))
                {
                    matches.Add(LegalMoves[i]);
                }
            }

            return matches;
        }
        public bool IsExileTarget(Square square)
        {
            IReadOnlyList<Square> targets = DraftTargets(MartyrPower.Exile);
            for (int i = 0; i < targets.Count; i++)
            {
                if (targets[i].Equals(square))
                {
                    return true;
                }
            }
            return false;
        }
        public IReadOnlyList<Square> DraftTargets(MartyrPower power)
        {
            return Rules.Hooks.CollectDraftTargets(this, power);
        }
        #endregion

        #region Private Methods
        private GameState(
            Board board,
            Side sideToMove,
            Square? enPassantTarget,
            CastlingRights castlingRights,
            int halfmoveClock,
            int fullmoveNumber,
            Move[] history,
            string[] previousPositionKeys,
            Rules rules,
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
            Rules = rules ?? VersusRules.CoreOnly;
            Runtime = runtime ?? ModeRuntime.Empty;

            IsInCheck = AttackMap.IsInCheck(board, sideToMove, Rules, Runtime);
            List<Move> legal = MoveGenerator.GenerateLegal(
                board,
                sideToMove,
                enPassantTarget,
                castlingRights,
                Rules,
                Runtime);
            LegalMoves = legal;

            Square? keyEnPassant = EffectiveEnPassant(enPassantTarget, legal);
            string key = Fen.PositionKey(board, sideToMove, castlingRights, keyEnPassant);
            if (appendPositionKey)
            {
                string[] keys = new string[(previousPositionKeys?.Length ?? 0) + 1];
                if (previousPositionKeys != null && previousPositionKeys.Length > 0)
                {
                    Array.Copy(previousPositionKeys, keys, previousPositionKeys.Length);
                }

                keys[keys.Length - 1] = key;
                _positionKeys = keys;
            }
            else
            {
                _positionKeys = previousPositionKeys ?? new[] { key };
            }

            if (forcedStatus != null)
            {
                Status = forcedStatus.Value;
            }
            else if (Runtime.PendingDraft != null)
            {
                Status = GameStatus.InProgress;
            }
            else
            {
                Status = Rules.Law.ResolveStatus(IsInCheck, legal.Count, halfmoveClock, _positionKeys, board);
            }
        }
        private bool MoveEndsTurn(Piece moving, Move move, ModeRuntime runtime)
        {
            int after = runtime.MovesThisTurn + 1;
            if (after >= 2)
            {
                return true;
            }
            if (runtime.RallyArmed)
            {
                return false;
            }
            if (moving.Type == PieceType.King && runtime.IsEmpowered(moving.Id) && Runtime.ExtraMoveKingId == null)
            {
                return false;
            }
            return true;
        }
        private Piece FindCaptured(Move move)
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
        private GameState CloneWithRuntime(ModeRuntime runtime)
        {
            return new GameState(
                Board,
                SideToMove,
                EnPassantTarget,
                CastlingRights,
                HalfmoveClock,
                FullmoveNumber,
                History as Move[] ?? CopyHistory(),
                _positionKeys,
                Rules,
                runtime,
                Status == GameStatus.InProgress ? null : Status,
                false);
        }
        private Move[] CopyHistory()
        {
            var copy = new Move[History.Count];
            for (int i = 0; i < History.Count; i++)
            {
                copy[i] = History[i];
            }

            return copy;
        }
        private Move[] AppendHistory(Move move)
        {
            Move[] nextHistory = new Move[History.Count + 1];
            for (int i = 0; i < History.Count; i++)
            {
                nextHistory[i] = History[i];
            }

            nextHistory[History.Count] = move;
            return nextHistory;
        }
        private bool IsLegal(Move move)
        {
            for (int i = 0; i < LegalMoves.Count; i++)
            {
                if (LegalMoves[i].Equals(move))
                {
                    return true;
                }
            }

            return false;
        }
        private static Square? ComputeEnPassantTarget(Move move, Piece moving)
        {
            if (moving.Type != PieceType.Pawn)
            {
                return null;
            }

            if (Math.Abs(move.To.Rank - move.From.Rank) != 2)
            {
                return null;
            }

            int startRank = moving.Side == Side.White ? 1 : 6;
            if (move.From.Rank != startRank)
            {
                return null;
            }

            return new Square(move.From.File, (move.From.Rank + move.To.Rank) / 2);
        }
        private static Square? EffectiveEnPassant(Square? enPassantTarget, List<Move> legalMoves)
        {
            if (enPassantTarget == null)
            {
                return null;
            }

            for (int i = 0; i < legalMoves.Count; i++)
            {
                if (legalMoves[i].Kind == MoveKind.EnPassant)
                {
                    return enPassantTarget;
                }
            }

            return null;
        }
        #endregion
    }
}
