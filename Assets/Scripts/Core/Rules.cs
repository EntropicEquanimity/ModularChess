using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public interface IRules
    {
        MatchSettings Settings { get; }
        ILaw Law { get; }
        IReadOnlyList<ModeId> Modes { get; }
        Side? PlayerSide { get; }
        PieceType StageTarget { get; }
        bool Has(ModeId id);
        bool Allows(Activity activity);
        bool AllowsHotseat();
    }

    public abstract class Rules : IRules
    {
        #region Fields
        public MatchSettings Settings { get; }
        public ILaw Law { get; }
        public IReadOnlyList<ModeId> Modes { get; }
        public Side? PlayerSide { get; }
        public PieceType StageTarget { get; }
        internal ModeHooks Hooks { get; }
        #endregion

        #region Public Methods
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
                {
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region Private Methods
        internal Rules(
            IReadOnlyList<ModeId> modes,
            MatchSettings settings,
            ILaw law,
            Side? playerSide,
            PieceType stageTarget,
            ModeHooks hooks)
        {
            Settings = settings ?? MatchSettings.Default;
            Law = law ?? FideLaw.Instance;
            PlayerSide = playerSide;
            StageTarget = stageTarget;
            Hooks = hooks ?? ModeHooks.None;
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
        #endregion
    }

    public sealed class VersusRules : Rules
    {
        #region Fields
        public static VersusRules CoreOnly { get; } = new VersusRules(
            Array.Empty<ModeId>(),
            MatchSettings.Default);
        public bool IsCoreOnly => Modes.Count == 0;
        #endregion

        #region Public Methods
        public VersusRules(IReadOnlyList<ModeId> modes, MatchSettings settings)
            : base(
                modes,
                settings,
                FideLaw.Instance,
                null,
                PieceType.King,
                modes == null || modes.Count == 0 ? ModeHooks.None : ModeHooks.For(modes))
        {
        }
        #endregion
    }

    public sealed class StageRules : Rules
    {
        #region Public Methods
        public static StageRules Create(Side playerSide, PieceType stageTarget)
        {
            return new StageRules(playerSide, stageTarget);
        }
        #endregion

        #region Private Methods
        StageRules(Side playerSide, PieceType stageTarget)
            : base(
                Array.Empty<ModeId>(),
                MatchSettings.Default,
                RoguelikeLaw.Instance,
                playerSide,
                stageTarget,
                ModeHooks.ExtraLife)
        {
        }
        #endregion
    }
}
