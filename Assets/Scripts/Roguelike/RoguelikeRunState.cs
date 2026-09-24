using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class RoguelikeRunState
    {
        #region Fields
        public const int StageCount = RoguelikeBalance.StageCount;
        public const int DefaultArmySize = 3;
        readonly Dictionary<BoonId, int> _stacks = new Dictionary<BoonId, int>();
        readonly List<BoonDefinition> _owned = new List<BoonDefinition>();
        public Side PlayerSide { get; }
        public int StageNumber { get; private set; }
        public int Gold { get; private set; }
        public int ArmySizeCap { get; private set; }
        public int PendingReinforcementPawns { get; private set; }
        public EnemyBoonId? ActiveEnemyBoon { get; private set; }
        public IReadOnlyList<BoonDefinition> OwnedBoons => _owned;
        public IReadOnlyDictionary<BoonId, int> Stacks => _stacks;
        public bool IsWon => StageNumber > StageCount;
        #endregion

        #region Public Methods
        public RoguelikeRunState(Side playerSide)
        {
            PlayerSide = playerSide;
            StageNumber = 1;
            Gold = RoguelikeBalance.StartingGold;
            ArmySizeCap = DefaultArmySize;
        }
        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;
            Gold += amount;
        }
        public bool TrySpendGold(int amount)
        {
            if (amount <= 0 || Gold < amount)
                return false;
            Gold -= amount;
            return true;
        }
        public void AddBoon(BoonDefinition def)
        {
            if (def == null)
                throw new ArgumentNullException(nameof(def));
            _owned.Add(def);
            _stacks.TryGetValue(def.Id, out int count);
            _stacks[def.Id] = count + 1;
            if (def.Id == BoonId.Reinforcements)
                PendingReinforcementPawns += def.PawnCount;
        }
        public int ConsumePendingReinforcements()
        {
            int n = PendingReinforcementPawns;
            PendingReinforcementPawns = 0;
            return n;
        }
        public void AdvanceStage()
        {
            StageNumber++;
        }
        public void SetEnemyBoon(EnemyBoonId? boon)
        {
            ActiveEnemyBoon = boon;
        }
        public PieceType StageTarget()
        {
            return RoguelikeBalance.StageTarget(StageNumber);
        }
        public bool IsBossStage()
        {
            return RoguelikeBalance.IsBossStage(StageNumber);
        }
        public bool IsMiniBossStage()
        {
            return RoguelikeBalance.IsMiniBossStage(StageNumber);
        }
        #endregion
    }
}
