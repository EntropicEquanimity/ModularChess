using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class MatchRules
    {
        #region Fields
        public static MatchRules CoreOnly { get; } = new MatchRules(
            Array.Empty<ModeId>(),
            MatchSettings.Default);
        public IReadOnlyList<ModeId> Modes { get; }
        public MatchSettings Settings { get; }
        public bool IsCoreOnly => Modes.Count == 0;
        #endregion

        #region Public Methods
        public MatchRules(IReadOnlyList<ModeId> modes, MatchSettings settings)
        {
            Settings = settings ?? MatchSettings.Default;
            if (modes == null || modes.Count == 0)
            {
                Modes = Array.Empty<ModeId>();
                return;
            }

            var copy = new ModeId[modes.Count];
            for (int i = 0; i < modes.Count; i++)
            {
                copy[i] = modes[i];
            }

            Modes = copy;
        }
        public bool Has(ModeId id)
        {
            for (int i = 0; i < Modes.Count; i++)
            {
                if (Modes[i] == id)
                {
                    return true;
                }
            }

            return false;
        }
        public bool Allows(Activity activity)
        {
            for (int i = 0; i < Modes.Count; i++)
            {
                if (!ModeCatalog.Get(Modes[i]).Allows(activity))
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
    }
}
