using ModularChess.Core;

namespace ModularChess.Match
{
    public sealed class MatchSession
    {
        public Activity Activity { get; set; }
        public MatchRules Rules { get; set; }
        public Side PlayerSide { get; set; }
        public bool Hotseat { get; set; }
        public string JoinCode { get; set; }

        public bool IsAi => Activity == Activity.VersusAi;
    }
}
