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
        public ILaw Law { get; }
        public Side? PlayerSide { get; }
        public PieceType StageTarget { get; }
        public bool IsCoreOnly => Modes.Count == 0;
        internal ModeHooks Hooks { get; }
        #endregion

        #region Public Methods
        public static MatchRules Roguelike(Side playerSide, PieceType stageTarget)
        {
            return new MatchRules(
                Array.Empty<ModeId>(),
                MatchSettings.Default,
                RoguelikeLaw.Instance,
                playerSide,
                stageTarget);
        }
        public MatchRules(IReadOnlyList<ModeId> modes, MatchSettings settings)
            : this(modes, settings, FideLaw.Instance, null, PieceType.King)
        {
        }
        public MatchRules(
            IReadOnlyList<ModeId> modes,
            MatchSettings settings,
            ILaw law,
            Side? playerSide,
            PieceType stageTarget)
        {
            Settings = settings ?? MatchSettings.Default;
            Law = law ?? FideLaw.Instance;
            PlayerSide = playerSide;
            StageTarget = stageTarget;
            if (modes == null || modes.Count == 0)
            {
                Modes = Array.Empty<ModeId>();
                Hooks = ModeHooks.None;
                return;
            }

            var copy = new ModeId[modes.Count];
            for (int i = 0; i < modes.Count; i++)
            {
                copy[i] = modes[i];
            }

            Modes = copy;
            Hooks = ModeHooks.For(Modes);
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
        public bool AllowsHotseat()
        {
            for (int i = 0; i < Modes.Count; i++)
            {
                if (!ModeCatalog.Get(Modes[i]).AllowsHotseat)
                    return false;
            }
            return true;
        }
        #endregion
    }
}
