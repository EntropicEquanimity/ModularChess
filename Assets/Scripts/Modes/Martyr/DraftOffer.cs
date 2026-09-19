using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public readonly struct DraftOffer
    {
        readonly MartyrPower[] _powers;
        public PieceType? BattlefieldType { get; }
        public int Count => _powers == null ? 0 : _powers.Length;

        public DraftOffer(IReadOnlyList<MartyrPower> powers, PieceType? battlefieldType)
        {
            if (powers == null || powers.Count == 0)
            {
                throw new ArgumentException("Draft offer needs at least one power.", nameof(powers));
            }

            _powers = new MartyrPower[powers.Count];
            for (int i = 0; i < powers.Count; i++)
            {
                _powers[i] = powers[i];
            }

            BattlefieldType = battlefieldType;
        }

        public bool Contains(MartyrPower power)
        {
            for (int i = 0; i < Count; i++)
            {
                if (_powers[i] == power)
                {
                    return true;
                }
            }

            return false;
        }
        public MartyrPower At(int index)
        {
            if (index < 0 || index >= Count)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return _powers[index];
        }
    }
}
