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
        const int MateScore = 100000;
        const int QuiesceMaxPly = 6;
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
        static readonly int[] KingMidTable =
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
        static readonly int[] KingEndTable =
        {
            -50, -30, -10, 0, 0, -10, -30, -50,
            -30, -10, 10, 20, 20, 10, -10, -30,
            -10, 10, 20, 30, 30, 20, 10, -10,
            0, 20, 30, 40, 40, 30, 20, 0,
            0, 20, 30, 40, 40, 30, 20, 0,
            -10, 10, 20, 30, 30, 20, 10, -10,
            -30, -10, 10, 20, 20, 10, -10, -30,
            -50, -30, -10, 0, 0, -10, -30, -50
        };
        static long _deadline;
        #endregion

        #region Public Methods
        public static Move? Choose(GameState state, AiStrength strength, Side aiSide)
        {
            if (state == null || state.DraftPending)
                return null;
            if (state.LegalMoves.Count == 0)
                return null;
            if (strength == AiStrength.Easy)
                return state.LegalMoves[UnityEngine.Random.Range(0, state.LegalMoves.Count)];
            Move? mate = FindMateInOne(state);
            if (mate != null)
                return mate.Value;
            int maxDepth = strength == AiStrength.Hard ? 4 : 3;
            double budget = strength == AiStrength.Hard ? 0.22 : 0.12;
            List<Move> ordered = OrderMoves(state, state.LegalMoves);
            Move best = ordered[0];
            int bestScore = int.MinValue / 2;
            _deadline = Stopwatch.GetTimestamp() + (long)(budget * Stopwatch.Frequency);
            for (int depth = 1; depth <= maxDepth; depth++)
            {
                if (TimedOut())
                    break;
                Move depthBest = best;
                int depthBestScore = int.MinValue / 2;
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
                    int score = -Negamax(next, depth - 1, -MateScore, MateScore, 1);
                    if (TimedOut())
                    {
                        complete = false;
                        break;
                    }
                    if (score > depthBestScore)
                    {
                        depthBestScore = score;
                        depthBest = move;
                    }
                }
                if (!complete)
                    break;
                best = depthBest;
                bestScore = depthBestScore;
                if (bestScore >= MateScore - 64)
                    break;
            }
            if (state.CanEndTurn() && PreferEndTurn(state, best, bestScore))
                return null;
            best = PreferKingCapture(state, best, bestScore);
            return best;
        }
        public static void AutopickEmpowered(GameState state, Side side, int budget, List<Guid> into)
        {
            if (state == null || into == null)
                return;
            budget = EmpoweredPowers.ClampBudget(budget);
            int spent = 0;
            for (int i = 0; i < into.Count; i++)
            {
                Square? square = state.Board.FindSquare(into[i]);
                if (!square.HasValue)
                    continue;
                Piece picked = state.Board.GetPiece(square.Value);
                if (picked != null)
                    spent += EmpoweredPowers.Cost(picked.Type);
            }
            var pool = new List<Piece>();
            for (int i = 0; i < 64; i++)
            {
                Piece piece = state.Board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Side == side && !into.Contains(piece.Id))
                    pool.Add(piece);
            }
            Shuffle(pool);
            if (!FillBudget(pool, budget - spent, into))
            {
                into.Clear();
                pool.Clear();
                for (int i = 0; i < 64; i++)
                {
                    Piece piece = state.Board.GetPiece(Square.FromIndex(i));
                    if (piece != null && piece.Side == side)
                        pool.Add(piece);
                }
                Shuffle(pool);
                FillBudget(pool, budget, into);
            }
        }
        static bool FillBudget(List<Piece> pool, int remaining, List<Guid> into)
        {
            if (remaining == 0)
                return true;
            if (remaining < 0)
                return false;
            for (int i = 0; i < pool.Count; i++)
            {
                Piece piece = pool[i];
                int cost = EmpoweredPowers.Cost(piece.Type);
                if (cost > remaining)
                    continue;
                pool.RemoveAt(i);
                into.Add(piece.Id);
                if (FillBudget(pool, remaining - cost, into))
                    return true;
                into.RemoveAt(into.Count - 1);
                pool.Insert(i, piece);
            }
            return false;
        }
        static void Shuffle(List<Piece> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                Piece tmp = list[i];
                list[i] = list[j];
                list[j] = tmp;
            }
        }
        #endregion

        #region Private Methods
        static bool TimedOut()
        {
            return Stopwatch.GetTimestamp() >= _deadline;
        }
        static Move? FindMateInOne(GameState state)
        {
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                GameState next = state.Apply(move);
                if (next.Status == GameStatus.Checkmate)
                    return move;
            }
            return null;
        }
        static bool PreferEndTurn(GameState state, Move best, int bestScore)
        {
            if (state.IsInCheck)
                return false;
            if (IsCaptureOrPromo(best) || GivesCheck(state, best))
                return false;
            GameState ended = state.EndTurn();
            int endScore = -Evaluate(ended, 0);
            return endScore >= bestScore;
        }
        static Move PreferKingCapture(GameState state, Move best, int bestScore)
        {
            if (bestScore >= MateScore - 64)
                return best;
            Move? take = BestKingCapture(state);
            if (take == null)
                return best;
            if (IsKingCapture(state, best))
                return best;
            if (IsCaptureOrPromo(best) && CaptureValue(best) > CaptureValue(take.Value))
                return best;
            return take.Value;
        }
        static Move? BestKingCapture(GameState state)
        {
            Move? best = null;
            int bestValue = -1;
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                if (!IsKingCapture(state, move))
                    continue;
                int value = CaptureValue(move);
                if (value > bestValue)
                {
                    bestValue = value;
                    best = move;
                }
            }
            return best;
        }
        static bool IsKingCapture(GameState state, Move move)
        {
            if (!IsCaptureOrPromo(move))
                return false;
            Piece piece = state.Board.GetPiece(move.From);
            return piece != null && piece.Type == PieceType.King;
        }
        static bool GivesCheck(GameState state, Move move)
        {
            GameState next = state.Apply(move);
            return next.Status == GameStatus.InProgress && next.IsInCheck;
        }
        static int Negamax(GameState state, int depth, int alpha, int beta, int ply)
        {
            if (TimedOut())
                return Evaluate(state, ply);
            if (state.Status != GameStatus.InProgress || state.DraftPending)
                return Evaluate(state, ply);
            if (depth <= 0)
                return Quiesce(state, alpha, beta, QuiesceMaxPly, ply);
            List<Move> ordered = OrderMoves(state, state.LegalMoves);
            if (ordered.Count == 0)
                return Evaluate(state, ply);
            int best = int.MinValue / 2;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (TimedOut())
                    break;
                GameState next = state.Apply(ordered[i]);
                int score = -Negamax(next, depth - 1, -beta, -alpha, ply + 1);
                if (score > best)
                    best = score;
                if (score > alpha)
                    alpha = score;
                if (alpha >= beta)
                    break;
            }
            return best == int.MinValue / 2 ? Evaluate(state, ply) : best;
        }
        static int Quiesce(GameState state, int alpha, int beta, int plyLeft, int ply)
        {
            if (TimedOut())
                return Evaluate(state, ply);
            int stand = Evaluate(state, ply);
            if (stand >= beta)
                return beta;
            if (stand > alpha)
                alpha = stand;
            if (plyLeft <= 0 || state.Status != GameStatus.InProgress || state.DraftPending)
                return stand;
            List<Move> captures = CaptureMoves(state);
            for (int i = 0; i < captures.Count; i++)
            {
                if (TimedOut())
                    break;
                GameState next = state.Apply(captures[i]);
                int score = -Quiesce(next, -beta, -alpha, plyLeft - 1, ply + 1);
                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }
            return alpha;
        }
        static List<Move> OrderMoves(GameState state, IReadOnlyList<Move> moves)
        {
            var scored = new List<KeyValuePair<int, Move>>(moves.Count);
            for (int i = 0; i < moves.Count; i++)
            {
                Move move = moves[i];
                scored.Add(new KeyValuePair<int, Move>(MoveOrderKey(state, move), move));
            }
            scored.Sort((a, b) => b.Key.CompareTo(a.Key));
            var ordered = new List<Move>(scored.Count);
            for (int i = 0; i < scored.Count; i++)
                ordered.Add(scored[i].Value);
            return ordered;
        }
        static int MoveOrderKey(GameState state, Move move)
        {
            int key = 0;
            if (IsCaptureOrPromo(move))
            {
                int victim = CaptureValue(move);
                Piece attacker = state.Board.GetPiece(move.From);
                int attackerVal = attacker != null ? (PieceValues.Get(attacker.Type) ?? 0) : 0;
                key += 10000 + victim * 100 - attackerVal;
                if (attacker != null && attacker.Type == PieceType.King)
                    key += 4000;
            }
            if (move.Kind == MoveKind.Promotion)
                key += 8000;
            return key;
        }
        static int CaptureValue(Move move)
        {
            if (move.CapturedType != null)
                return (PieceValues.Get(move.CapturedType.Value) ?? 0) * 10;
            if (move.Kind == MoveKind.EnPassant)
                return PieceValues.Pawn * 10;
            return 0;
        }
        static List<Move> CaptureMoves(GameState state)
        {
            var captures = new List<Move>();
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                if (!IsCaptureOrPromo(move))
                    continue;
                captures.Add(move);
            }
            captures.Sort((a, b) => CaptureValue(b).CompareTo(CaptureValue(a)));
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
        static int Evaluate(GameState state, int ply)
        {
            if (state.Status == GameStatus.Checkmate)
                return -MateScore + ply;
            if (state.Status == GameStatus.Stalemate
                || state.Status == GameStatus.Draw
                || state.Status == GameStatus.Timeout
                || state.Status == GameStatus.Resign
                || state.Status == GameStatus.Aborted)
            {
                return 0;
            }
            Side stm = state.SideToMove;
            VisionMap vision = VisionMap.Compute(state, stm);
            bool endgame = IsEndgame(state.Board);
            int total = 0;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = state.Board.GetPiece(square);
                if (piece == null)
                    continue;
                if (piece.Side != stm && !vision.IsIdentified(square))
                    continue;
                int points = PieceScore(piece, square, endgame);
                total += piece.Side == stm ? points : -points;
            }
            if (state.IsInCheck)
                total -= 45;
            total += Math.Min(state.LegalMoves.Count, 24);
            return total;
        }
        static bool IsEndgame(Board board)
        {
            int majors = 0;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = board.GetPiece(Square.FromIndex(i));
                if (piece == null)
                    continue;
                if (piece.Type == PieceType.Queen || piece.Type == PieceType.Rook)
                    majors++;
            }
            return majors <= 2;
        }
        static int PieceScore(Piece piece, Square square, bool endgame)
        {
            int material = (PieceValues.Get(piece.Type) ?? 0) * 100;
            if (piece.Type == PieceType.King)
                material = 0;
            return material + Pst(piece.Type, square, piece.Side, endgame);
        }
        static int Pst(PieceType type, Square square, Side side, bool endgame)
        {
            int index = PstIndex(square, side);
            switch (type)
            {
                case PieceType.Pawn: return PawnTable[index];
                case PieceType.Knight: return KnightTable[index];
                case PieceType.Bishop: return BishopTable[index];
                case PieceType.Rook: return RookTable[index];
                case PieceType.Queen: return QueenTable[index];
                case PieceType.King: return endgame ? KingEndTable[index] : KingMidTable[index];
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
