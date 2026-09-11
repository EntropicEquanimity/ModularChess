using System;
using System.Collections.Generic;
using ModularChess.Core;

namespace ModularChess.Match
{
    public static class SimpleAi
    {
        public static Move? Choose(GameState state, AiStrength strength, Side aiSide)
        {
            if (state == null || state.LegalMoves.Count == 0)
            {
                return null;
            }

            int depth = 0;
            switch (strength)
            {
                case AiStrength.Easy:
                    depth = 0;
                    break;
                case AiStrength.Medium:
                    depth = 1;
                    break;
                case AiStrength.Hard:
                    depth = 2;
                    break;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(strength), strength, null);
            }

            if (depth == 0)
            {
                return state.LegalMoves[UnityEngine.Random.Range(0, state.LegalMoves.Count)];
            }

            Move best = state.LegalMoves[0];
            int bestScore = int.MinValue;
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                GameState next = state.Apply(move);
                int score = -Evaluate(next, aiSide, depth - 1, -99999, 99999);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = move;
                }
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
                {
                    pool.Add(piece.Id);
                }
            }

            while (into.Count < count && pool.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);
                into.Add(pool[index]);
                pool.RemoveAt(index);
            }
        }

        static int Evaluate(GameState state, Side aiSide, int depth, int alpha, int beta)
        {
            if (state.Status != GameStatus.InProgress || depth == 0)
            {
                return Score(state, aiSide);
            }

            int best = int.MinValue;
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                GameState next = state.Apply(state.LegalMoves[i]);
                int score = -Evaluate(next, aiSide, depth - 1, -beta, -alpha);
                if (score > best)
                {
                    best = score;
                }

                if (score > alpha)
                {
                    alpha = score;
                }

                if (alpha >= beta)
                {
                    break;
                }
            }

            return best;
        }

        static int Score(GameState state, Side aiSide)
        {
            if (state.Status == GameStatus.Checkmate)
            {
                return state.SideToMove == aiSide ? -100000 : 100000;
            }

            VisionMap vision = VisionMap.Compute(state, aiSide);
            int total = 0;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = state.Board.GetPiece(square);
                if (piece == null)
                {
                    continue;
                }

                int? value = PieceValues.Get(piece.Type);
                int points = value ?? 40;
                if (piece.Side == aiSide)
                {
                    total += points * 10;
                }
                else if (vision.IsIdentified(square))
                {
                    total -= points * 10;
                }
            }

            return total;
        }
    }
}
