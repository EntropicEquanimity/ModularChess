using System;
using ModularChess.Core;

namespace ModularChess.Core.Tests
{
    internal static class MoveTestHelper
    {
        public static Move Require(GameState state, string from, string to, PieceType? promotion = null)
        {
            Move? found = Find(state, from, to, promotion);
            if (found == null)
            {
                throw new InvalidOperationException(
                    $"No legal move {from}{to}{(promotion.HasValue ? promotion.ToString() : string.Empty)}.");
            }

            return found.Value;
        }

        public static bool Has(GameState state, string from, string to, PieceType? promotion = null)
        {
            return Find(state, from, to, promotion) != null;
        }

        public static GameState Play(GameState state, params string[] notations)
        {
            GameState current = state;
            for (int i = 0; i < notations.Length; i++)
            {
                current = PlayOne(current, notations[i]);
            }

            return current;
        }

        public static GameState PlayOne(GameState state, string notation)
        {
            if (notation == null || notation.Length < 4)
            {
                throw new ArgumentException("Move notation must be at least 4 characters.", nameof(notation));
            }

            string from = notation.Substring(0, 2);
            string to = notation.Substring(2, 2);
            PieceType? promotion = null;
            if (notation.Length >= 5)
            {
                promotion = ParsePromotion(notation[4]);
            }

            return state.Apply(Require(state, from, to, promotion));
        }

        private static Move? Find(GameState state, string from, string to, PieceType? promotion)
        {
            if (!Square.TryParse(from, out Square fromSquare) || !Square.TryParse(to, out Square toSquare))
            {
                throw new ArgumentException($"Invalid square in {from}{to}.");
            }

            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                if (move.From.Equals(fromSquare) && move.To.Equals(toSquare) && move.PromotionType == promotion)
                {
                    return move;
                }
            }

            return null;
        }

        private static PieceType ParsePromotion(char symbol)
        {
            switch (char.ToLowerInvariant(symbol))
            {
                case 'q':
                    return PieceType.Queen;
                case 'r':
                    return PieceType.Rook;
                case 'b':
                    return PieceType.Bishop;
                case 'n':
                    return PieceType.Knight;
                default:
                    throw new ArgumentOutOfRangeException(nameof(symbol), symbol, null);
            }
        }
    }
}
