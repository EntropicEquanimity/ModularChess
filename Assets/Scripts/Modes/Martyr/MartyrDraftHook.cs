using System.Collections.Generic;

namespace ModularChess.Core
{
    internal sealed class MartyrDraftHook : IDraftHook
    {
        #region Public Methods
        public void CollectTargets(GameState state, MartyrPower power, List<Square> into)
        {
            MartyrRules.CollectDraftTargets(state, power, into);
        }
        #endregion
    }
}