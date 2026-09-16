using System;

namespace ModularChess.Core
{
    public readonly struct DraftOffer
    {
        public MartyrPower First { get; }
        public MartyrPower? Second { get; }
        public MartyrPower? Third { get; }
        public PieceType? BattlefieldType { get; }
        public int Count { get; }

        public DraftOffer(MartyrPower first, MartyrPower? second, MartyrPower? third, PieceType? battlefieldType)
        {
            First = first;
            Second = second;
            Third = third;
            BattlefieldType = battlefieldType;
            int count = 1;
            if (second != null)
            {
                count++;
            }
            if (third != null)
            {
                count++;
            }
            Count = count;
        }

        public bool Contains(MartyrPower power)
        {
            return First == power || Second == power || Third == power;
        }
        public MartyrPower At(int index)
        {
            switch (index)
            {
                case 0:
                    return First;
                case 1:
                    if (Second == null)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    return Second.Value;
                case 2:
                    if (Third == null)
                    {
                        throw new ArgumentOutOfRangeException(nameof(index));
                    }
                    return Third.Value;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
