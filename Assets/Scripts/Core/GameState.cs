using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class GameState
    {
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
        public MatchRules Rules { get; }
        public ModeRuntime Runtime { get; }

        public int MovesThisTurn => Runtime.MovesThisTurn;
        public bool TurnOpen => Runtime.ExtraMoveKingId != null;
        public bool DraftPending => Runtime.PendingDraft != null;

        private GameState(
            Board board,
            Side sideToMove,
            Square? enPassantTarget,
            CastlingRights castlingRights,
            int halfmoveClock,
            int fullmoveNumber,
            Move[] history,
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
            Rules = rules ?? MatchRules.CoreOnly;
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
                Status = DrawEvaluator.Resolve(IsInCheck, legal.Count, halfmoveClock, _positionKeys, board);
            }
        }

        public static GameState StartingPosition(MatchRules rules = null)
        {
            return Fen.Parse(Fen.StartingPosition, rules);
        }

        public static GameState FromFen(string fen, MatchRules rules = null)
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

            if (captured != null && nextRuntime.ExtraLifeAvailable(captured.Id))
            {
                bounced = true;
                nextRuntime = nextRuntime.SpendExtraLife(captured.Id);
            }
            else
            {
                nextBoard = Board.ApplyUnchecked(move);
                if (captured != null && Rules.Has(ModeId.Martyr) && !nextRuntime.IsSummoned(captured.Id))
                {
                    int? value = PieceValues.Get(captured.Type);
                    if (value != null)
                    {
                        nextRuntime = nextRuntime.AddLostMaterial(
                            captured.Side,
                            value.Value,
                            Rules.Settings.MartyrThreshold);
                    }
                }
            }

            if (move.Kind == MoveKind.Promotion && nextRuntime.IsEmpowered(moving.Id) && moving.Type == PieceType.Pawn)
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
                nextRuntime = nextRuntime.WithExtraKing(null).WithMovesThisTurn(0);
                nextRuntime = MaybeOpenDraft(nextRuntime, nextSide);
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
                null,
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

            ModeRuntime nextRuntime = Runtime.TickStatuses(SideToMove).WithExtraKing(null).WithMovesThisTurn(0);
            Side nextSide = SideToMove.Opponent();
            nextRuntime = MaybeOpenDraft(nextRuntime, nextSide);
            int nextFullmove = SideToMove == Side.Black ? FullmoveNumber + 1 : FullmoveNumber;
            return new GameState(
                Board,
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
            next = MaybeOpenDraft(next, SideToMove);
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
                CastlingRights,
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

        bool MoveEndsTurn(Piece moving, Move move, ModeRuntime runtime)
        {
            if (moving.Type == PieceType.King && runtime.IsEmpowered(moving.Id) && Runtime.ExtraMoveKingId == null)
            {
                return false;
            }

            return true;
        }

        ModeRuntime MaybeOpenDraft(ModeRuntime runtime, Side sideToMove)
        {
            if (!Rules.Has(ModeId.Martyr) || runtime.PendingDraft != null)
            {
                return runtime;
            }

            int queued = sideToMove == Side.White ? runtime.WhiteDraftsQueued : runtime.BlackDraftsQueued;
            if (queued <= 0)
            {
                return runtime;
            }

            DraftOffer offer = MartyrRules.BuildOffer(this, runtime, sideToMove);
            PieceType? battlefield = null;
            if (offer.First == MartyrPower.BattlefieldPromotion
                || offer.Second == MartyrPower.BattlefieldPromotion
                || offer.Third == MartyrPower.BattlefieldPromotion)
            {
                battlefield = offer.BattlefieldType;
            }

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
            {
                copy[i] = History[i];
            }

            return copy;
        }

        Move[] AppendHistory(Move move)
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
    }
}
