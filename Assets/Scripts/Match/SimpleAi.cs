using System;
using System.Collections.Generic;
using System.Diagnostics;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Match
{
    public static class SimpleAi
    {
        #region Fields
        const double SearchBudgetSeconds = 0.08;
        const int QuiesceMaxPly = 4;
        static readonly int[] PawnTable =
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            50, 50, 50, 50, 50, 50, 50, 50,
            10, 10, 20, 30, 30, 20, 10, 10,
            5, 5, 10, 25, 25, 10, 5, 5,
            0, 0, 0, 20, 20, 0, 0, 0,
            5, -5, -10, 0, 0, -10, -5, 5,
            5, 10, 10, -20, -20, 10, 10, 5,
            0, 0, 0, 0, 0, 0, 0, 0
        };
        static readonly int[] KnightTable =
        {
            -50, -40, -30, -30, -30, -30, -40, -50,
            -40, -20, 0, 0, 0, 0, -20, -40,
            -30, 0, 10, 15, 15, 10, 0, -30,
            -30, 5, 15, 20, 20, 15, 5, -30,
            -30, 0, 15, 20, 20, 15, 0, -30,
            -30, 5, 10, 15, 15, 10, 5, -30,
            -40, -20, 0, 5, 5, 0, -20, -40,
            -50, -40, -30, -30, -30, -30, -40, -50
        };
        static readonly int[] BishopTable =
        {
            -20, -10, -10, -10, -10, -10, -10, -20,
            -10, 0, 0, 0, 0, 0, 0, -10,
            -10, 0, 5, 10, 10, 5, 0, -10,
            -10, 5, 5, 10, 10, 5, 5, -10,
            -10, 0, 10, 10, 10, 10, 0, -10,
            -10, 10, 10, 10, 10, 10, 10, -10,
            -10, 5, 0, 0, 0, 0, 5, -10,
            -20, -10, -10, -10, -10, -10, -10, -20
        };
        static readonly int[] RookTable =
        {
            0, 0, 0, 0, 0, 0, 0, 0,
            5, 10, 10, 10, 10, 10, 10, 5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            -5, 0, 0, 0, 0, 0, 0, -5,
            0, 0, 0, 5, 5, 0, 0, 0
        };
        static readonly int[] QueenTable =
        {
            -20, -10, -10, -5, -5, -10, -10, -20,
            -10, 0, 0, 0, 0, 0, 0, -10,
            -10, 0, 5, 5, 5, 5, 0, -10,
            -5, 0, 5, 5, 5, 5, 0, -5,
            0, 0, 5, 5, 5, 5, 0, -5,
            -10, 5, 5, 5, 5, 5, 0, -10,
            -10, 0, 5, 0, 0, 0, 0, -10,
            -20, -10, -10, -5, -5, -10, -10, -20
        };
        static readonly int[] KingTable =
        {
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -30, -40, -40, -50, -50, -40, -40, -30,
            -20, -30, -30, -40, -40, -30, -30, -20,
            -10, -20, -20, -20, -20, -20, -20, -10,
            20, 20, 0, 0, 0, 0, 20, 20,
            20, 30, 10, 0, 0, 10, 30, 20
        };
        static long _deadline;
        #endregion

        #region Public Methods
        public static Move? Choose(GameState state, AiStrength strength, Side aiSide)
        {
            if (state == null || state.DraftPending || state.LegalMoves.Count == 0)
                return null;
            if (strength == AiStrength.Easy)
                return state.LegalMoves[UnityEngine.Random.Range(0, state.LegalMoves.Count)];
            int maxDepth = strength == AiStrength.Hard ? 3 : 2;
            List<Move> ordered = OrderMoves(state, state.LegalMoves);
            Move best = ordered[0];
            _deadline = Stopwatch.GetTimestamp() + (long)(SearchBudgetSeconds * Stopwatch.Frequency);
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                if (TimedOut())
                    break;
                Move depthBest = best;
                int bestScore = int.MinValue;
                bool complete = true;
                for (int i = 0; i < ordered.Count; i++)
                {
                    if (TimedOut())
                    {
                        complete = false;
                        break;
                    }
                    Move move = ordered[i];
                    GameState next = state.Apply(move);
                    int score = -Negamax(next, aiSide, depth - 1, -99999, 99999);
                    if (TimedOut())
                    {
                        complete = false;
                        break;
                    }
                    if (score > bestScore)
                    {
                        bestScore = score;
                        depthBest = move;
                    }
                }
                if (!complete)
                    break;
                best = depthBest;
            }
            return best;
        }
        public static void AutopickEmpowered(GameState state, Side side, int count, List<Guid> into)
        {
            var pool = new List<Guid>();
            for (int i = 0; i < 64; i++)
            {
                Piece piece = state.Board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Side == side)
                    pool.Add(piece.Id);
            }
            while (into.Count < count && pool.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                into.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }
        #endregion

        #region Private Methods
        static bool TimedOut()
        {
            return Stopwatch.GetTimestamp() >= _deadline;
        }
        static int Negamax(GameState state, Side aiSide, int depth, int alpha, int beta)
        {
            if (TimedOut())
                return Score(state, aiSide);
            if (state.Status != GameStatus.InProgress || state.DraftPending)
                return Score(state, aiSide);
            if (depth <= 0)
                return Quiesce(state, aiSide, alpha, beta, QuiesceMaxPly);
            List<Move> ordered = OrderMoves(state, state.LegalMoves);
            if (ordered.Count == 0)
                return Score(state, aiSide);
            int best = int.MinValue;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (TimedOut())
                    break;
                GameState next = state.Apply(ordered[i]);
                int score = -Negamax(next, aiSide, depth - 1, -beta, -alpha);
                if (score > best)
                    best = score;
                if (score > alpha)
                    alpha = score;
                if (alpha >= beta)
                    break;
            }
            return best == int.MinValue ? Score(state, aiSide) : best;
        }
        static int Quiesce(GameState state, Side aiSide, int alpha, int beta, int plyLeft)
        {
            if (TimedOut())
                return Score(state, aiSide);
            int stand = Score(state, aiSide);
            if (stand >= beta)
                return beta;
            if (stand > alpha)
                alpha = stand;
            if (plyLeft <= 0 || state.Status != GameStatus.InProgress || state.DraftPending)
                return stand;
            List<Move> captures = CaptureMoves(state.LegalMoves);
            for (int i = 0; i < captures.Count; i++)
            {
                if (TimedOut())
                    break;
                GameState next = state.Apply(captures[i]);
                int score = -Quiesce(next, aiSide, -beta, -alpha, plyLeft - 1);
                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }
            return alpha;
        }
        static List<Move> OrderMoves(GameState state, IReadOnlyList<Move> moves)
        {
            var ordered = new List<Move>(moves.Count);
            var quiet = new List<Move>();
            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];
                if (IsCaptureOrPromo(move))
                    ordered.Add(move);
                else
                    quiet.Add(move);
            }
            ordered.AddRange(quiet);
            return ordered;
        }
        static List<Move> CaptureMoves(IReadOnlyList<Move> moves)
        {
            var captures = new List<Move>();
            for (int i = 0; i < moves.Count; i++)
            {
                if (IsCaptureOrPromo(moves[i]))
                    captures.Add(moves[i]);
            }
            return captures;
        }
        static bool IsCaptureOrPromo(Move move)
        {
            return move.Kind == MoveKind.Capture
                || move.Kind == MoveKind.EnPassant
                || move.Kind == MoveKind.Bombard
                || move.Kind == MoveKind.Promotion
                || move.CapturedType != null;
        }
        static int Score(GameState state, Side aiSide)
        {
            if (state.Status == GameStatus.Checkmate)
                return state.SideToMove == aiSide ? -100000 : 100000;
            VisionMap vision = VisionMap.Compute(state, aiSide);
            int total = 0;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = state.Board.GetPiece(square);
                if (piece == null)
                    continue;
                int points = PieceScore(piece, square);
                if (piece.Side == aiSide)
                    total += points;
                else if (vision.IsIdentified(square))
                    total -= points;
            }
            total += state.SideToMove == aiSide ? state.LegalMoves.Count : -state.LegalMoves.Count;
            return total;
        }
        static int PieceScore(Piece piece, Square square)
        {
            int material = (PieceValues.Get(piece.Type) ?? 40) * 100;
            return material + Pst(piece.Type, square, piece.Side);
        }
        static int Pst(PieceType type, Square square, Side side)
        {
            int index = PstIndex(square, side);
            switch (type)
            {
                case PieceType.Pawn: return PawnTable[index];
                case PieceType.Knight: return KnightTable[index];
                case PieceType.Bishop: return BishopTable[index];
                case PieceType.Rook: return RookTable[index];
                case PieceType.Queen: return QueenTable[index];
                case PieceType.King: return KingTable[index];
                default: throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        static int PstIndex(Square square, Side side)
        {
            int rank = side == Side.White ? 7 - square.Rank : square.Rank;
            return rank * 8 + square.File;
        }
        #endregion
    }
}
