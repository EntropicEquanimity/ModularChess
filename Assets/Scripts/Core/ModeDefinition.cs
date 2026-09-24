using System;

namespace ModularChess.Core
{
    public sealed class ModeDefinition
    {
        #region Fields
        public ModeId Id { get; }
        public string DisplayName { get; }
        public string Summary { get; }
        public int Priority { get; }
        public bool AllowsHotseat { get; }
        #endregion

        #region Public Methods
        public ModeDefinition(
            ModeId id,
            string displayName,
            string summary,
            int priority = 0,
            bool allowsHotseat = true)
        {
            Id = id;
            DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
            Summary = summary ?? string.Empty;
            Priority = priority;
            AllowsHotseat = allowsHotseat;
        }
        public bool Allows(Activity activity)
        {
            switch (activity)
            {
                case Activity.VersusAi:
                case Activity.VersusFriend:
                case Activity.Campaign:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(activity), activity, null);
            }
        }
        #endregion
    }
}
