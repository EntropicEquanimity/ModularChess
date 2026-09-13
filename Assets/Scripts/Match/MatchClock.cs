using System;
using ModularChess.Core;

namespace ModularChess.Match
{
    public sealed class MatchClock
    {
        public float WhiteSeconds { get; private set; }
        public float BlackSeconds { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public bool Running { get; private set; }

        readonly TimeControl _control;

        public MatchClock(TimeControl control)
        {
            _control = control;
            float baseSeconds = control.IsNone ? 0f : control.BaseMinutes * 60f;
            WhiteSeconds = baseSeconds;
            BlackSeconds = baseSeconds;
        }

        public bool IsNone => _control.IsNone;

        public void Start()
        {
            Running = !_control.IsNone;
        }

        public void Stop()
        {
            Running = false;
        }

        public Side? Tick(float delta, Side toMove)
        {
            if (!Running || _control.IsNone)
            {
                return null;
            }

            ElapsedSeconds += delta;

            if (toMove == Side.White)
            {
                WhiteSeconds -= delta;
                if (WhiteSeconds <= 0f)
                {
                    WhiteSeconds = 0f;
                    Running = false;
                    return Side.White;
                }
            }
            else
            {
                BlackSeconds -= delta;
                if (BlackSeconds <= 0f)
                {
                    BlackSeconds = 0f;
                    Running = false;
                    return Side.Black;
                }
            }

            return null;
        }

        public void AddIncrement(Side sideThatEndedTurn)
        {
            if (_control.IsNone)
            {
                return;
            }

            float extra = _control.IncrementSeconds;
            if (sideThatEndedTurn == Side.White)
            {
                WhiteSeconds += extra;
            }
            else
            {
                BlackSeconds += extra;
            }
        }

        public void ResetToStart()
        {
            float baseSeconds = _control.IsNone ? 0f : _control.BaseMinutes * 60f;
            WhiteSeconds = baseSeconds;
            BlackSeconds = baseSeconds;
        }

        public string Format(Side side)
        {
            if (_control.IsNone)
            {
                return "--";
            }

            float seconds = side == Side.White ? WhiteSeconds : BlackSeconds;
            int total = Math.Max(0, (int)Math.Ceiling(seconds));
            int minutes = total / 60;
            int rest = total % 60;
            return $"{minutes}:{rest:00}";
        }
    }
}
