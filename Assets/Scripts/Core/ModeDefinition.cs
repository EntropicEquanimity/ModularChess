using System;

namespace ModularChess.Core
{
    public sealed class ModeDefinition
    {
        public ModeId Id { get; }
        public string DisplayName { get; }
        public string Summary { get; }
        public int Priority { get; }

        public ModeDefinition(ModeId id, string displayName, string summary, int priority = 0)
        {
            Id = id;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Summary = summary ?? string.Empty;
            Priority = priority;
        }

        public bool Allows(Activity activity)
        {
            switch (activity)
            {
                case Activity.VersusAi:
                case Activity.VersusFriend:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(activity), activity, null);
            }
        }
    }
}
