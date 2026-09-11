using System;

namespace ModularChess.Core
{
    public readonly struct DraftOffer
    {
        public MartyrPower First { get; }
        public MartyrPower Second { get; }
        public MartyrPower Third { get; }
        public PieceType? BattlefieldType { get; }

        public DraftOffer(MartyrPower first, MartyrPower second, MartyrPower third, PieceType? battlefieldType)
        {
            First = first;
            Second = second;
            Third = third;
            BattlefieldType = battlefieldType;
        }

        public MartyrPower At(int index)
        {
            switch (index)
            {
                case 0:
                    return First;
                case 1:
                    return Second;
                case 2:
                    return Third;
                default:
                    throw new ArgumentOutOfRangeException(nameof(index));
            }
        }
    }
}
