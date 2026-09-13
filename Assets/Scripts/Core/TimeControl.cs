using System;

namespace ModularChess.Core
{
    public readonly struct TimeControl : IEquatable<TimeControl>
    {
        public int BaseMinutes { get; }
        public int IncrementSeconds { get; }

        public TimeControl(int baseMinutes, int incrementSeconds)
        {
            if (baseMinutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(baseMinutes));
            }

            if (incrementSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(incrementSeconds));
            }

            BaseMinutes = baseMinutes;
            IncrementSeconds = incrementSeconds;
        }

        public bool IsNone => BaseMinutes == 0 && IncrementSeconds == 0;

        public static TimeControl None => new TimeControl(0, 0);

        public static TimeControl Bullet => new TimeControl(1, 0);

        public static TimeControl Blitz => new TimeControl(5, 0);

        public static TimeControl Rapid => new TimeControl(15, 0);

        public static TimeControl Standard => new TimeControl(30, 0);

        public static TimeControl Extended => new TimeControl(120, 0);

        public bool Equals(TimeControl other)
        {
            return BaseMinutes == other.BaseMinutes && IncrementSeconds == other.IncrementSeconds;
        }

        public override bool Equals(object obj) => obj is TimeControl other && Equals(other);

        public override int GetHashCode() => (BaseMinutes * 397) ^ IncrementSeconds;

        public override string ToString()
        {
            return IsNone ? "None" : $"{BaseMinutes}+{IncrementSeconds}";
        }
    }
}
