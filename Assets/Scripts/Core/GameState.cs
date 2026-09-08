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

        private GameState(
            Board board,
            Side sideToMove,
            Square? enPassantTarget,
            CastlingRights castlingRights,
            int halfmoveClock,
            int fullmoveNumber,
            Move[] history,
            string[] previousPositionKeys)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            SideToMove = sideToMove;
            EnPassantTarget = enPassantTarget;
            CastlingRights = castlingRights;
            HalfmoveClock = halfmoveClock;
            FullmoveNumber = fullmoveNumber;
            History = history ?? Array.Empty<Move>();

            IsInCheck = AttackMap.IsInCheck(board, sideToMove);
            List<Move> legal = MoveGenerator.GenerateLegal(board, sideToMove, enPassantTarget, castlingRights);
            LegalMoves = legal;

            Square? keyEnPassant = EffectiveEnPassant(enPassantTarget, legal);
            string key = Fen.PositionKey(board, sideToMove, castlingRights, keyEnPassant);
            string[] keys = new string[(previousPositionKeys?.Length ?? 0) + 1];
            if (previousPositionKeys != null && previousPositionKeys.Length > 0)
            {
                Array.Copy(previousPositionKeys, keys, previousPositionKeys.Length);
            }

            keys[keys.Length - 1] = key;
            _positionKeys = keys;

            Status = DrawEvaluator.Resolve(IsInCheck, legal.Count, halfmoveClock, keys, board);
        }

        public static GameState StartingPosition()
        {
            return Fen.Parse(Fen.StartingPosition);
        }

        public static GameState FromFen(string fen)
        {
            return Fen.Parse(fen);
        }

        internal static GameState FromPosition(
            Board board,
            Side sideToMove,
            Square? enPassantTarget,
            CastlingRights castlingRights,
            int halfmoveClock,
            int fullmoveNumber,
            Move[] history = null,
            string[] previousPositionKeys = null)
        {
            return new GameState(
                board,
                sideToMove,
                enPassantTarget,
                castlingRights,
                halfmoveClock,
                fullmoveNumber,
                history ?? Array.Empty<Move>(),
                previousPositionKeys);
        }

        public string ToFen() => Fen.Format(this);

        public GameState Apply(Move move)
        {
            if (Status != GameStatus.InProgress)
            {
                throw new InvalidOperationException("Cannot apply a move when the game is over.");
            }

            if (!IsLegal(move))
            {
                throw new InvalidOperationException("Move is not legal in the current position.");
            }

            Piece moving = Board.GetPiece(move.From);
            Board nextBoard = Board.ApplyUnchecked(move);
            CastlingRights nextCastling = CastlingRights.AfterMove(move, Board);
            Square? nextEnPassant = ComputeEnPassantTarget(move, moving);
            bool resetsClock = moving.Type == PieceType.Pawn || move.CapturedType != null;
            int nextHalfmove = resetsClock ? 0 : HalfmoveClock + 1;
            int nextFullmove = SideToMove == Side.Black ? FullmoveNumber + 1 : FullmoveNumber;
            Side nextSide = SideToMove.Opponent();

            Move[] nextHistory = new Move[History.Count + 1];
            for (int i = 0; i < History.Count; i++)
            {
                nextHistory[i] = History[i];
            }

            nextHistory[History.Count] = move;

            return new GameState(
                nextBoard,
                nextSide,
                nextEnPassant,
                nextCastling,
                nextHalfmove,
                nextFullmove,
                nextHistory,
                _positionKeys);
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
