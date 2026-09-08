using System;

namespace ModularChess.Core
{
    public readonly struct Move : IEquatable<Move>
    {
        public Square From { get; }
        public Square To { get; }
        public MoveKind Kind { get; }
        public PieceType? PromotionType { get; }
        public PieceType? CapturedType { get; }

        public Move(
            Square from,
            Square to,
            MoveKind kind,
            PieceType? promotionType = null,
            PieceType? capturedType = null)
        {
            if (kind == MoveKind.Promotion)
            {
                if (promotionType == null)
                {
                    throw new ArgumentException("Promotion moves require PromotionType.", nameof(promotionType));
                }

                if (promotionType == PieceType.Pawn || promotionType == PieceType.King)
                {
                    throw new ArgumentException("Cannot promote to pawn or king.", nameof(promotionType));
                }
            }
            else if (promotionType != null)
            {
                throw new ArgumentException("PromotionType is only valid for promotion moves.", nameof(promotionType));
            }

            From = from;
            To = to;
            Kind = kind;
            PromotionType = promotionType;
            CapturedType = capturedType;
        }

        public bool Equals(Move other)
        {
            return From.Equals(other.From)
                   && To.Equals(other.To)
                   && Kind == other.Kind
                   && PromotionType == other.PromotionType
                   && CapturedType == other.CapturedType;
        }

        public override bool Equals(object obj) => obj is Move other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = From.GetHashCode();
                hash = (hash * 397) ^ To.GetHashCode();
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ PromotionType.GetHashCode();
                hash = (hash * 397) ^ CapturedType.GetHashCode();
                return hash;
            }
        }

        public static bool operator ==(Move left, Move right) => left.Equals(right);

        public static bool operator !=(Move left, Move right) => !left.Equals(right);

        public override string ToString()
        {
            string text = $"{From}{To}";
            if (Kind == MoveKind.Promotion && PromotionType.HasValue)
            {
                text += Fen.PieceTypeToFenChar(PromotionType.Value, Side.White);
            }

            return text;
        }
    }
}
