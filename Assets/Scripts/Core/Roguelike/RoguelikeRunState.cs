using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class RoguelikeRunState
    {
        #region Fields
        public const int StageCount = 13;
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
            Gold = 0;
            ArmySizeCap = DefaultArmySize;
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
            if (StageNumber == 6 || StageNumber == 12 || StageNumber == 13)
                return PieceType.Queen;
            return PieceType.King;
        }
        public bool IsBossStage()
        {
            return StageNumber == 13;
        }
        public bool IsMiniBossStage()
        {
            return StageNumber == 6 || StageNumber == 12;
        }
        #endregion
    }
}
