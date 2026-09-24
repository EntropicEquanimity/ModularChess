using System;

namespace ModularChess.Core
{
    public static class RoguelikeBalance
    {
        #region Fields
        public const int StageCount = 13;
        public const int MiniBossStageA = 6;
        public const int MiniBossStageB = 12;
        public const int BossStage = 13;
        public const int FirstEnemyBoonStage = 4;
        public const int EnemyBudgetBase = 1;
        public const int EnemyBudgetPerStage = 2;
        public const int KingCaptureGold = 5;
        public const int StartingGold = 5;
        public const int StageTurnLimit = 20;
        public const int ShopSlotCount = 5;
        public const int ShopRerollCost = 2;
        public const int ShopPriceMinMult = 3;
        public const int ShopPriceMaxMult = 5;
        public const int PawnWeightMin = 20;
        public const int PawnWeightMax = 100;
        public const int MinorWeightMin = 0;
        public const int MinorWeightMax = 30;
        public const int RookWeightMin = 0;
        public const int RookWeightMax = 20;
        public static readonly int[] ShopStages = { 4, 8, 12 };
        public static readonly (PieceType Type, int Cost)[] EnemyPiecePool =
        {
            (PieceType.Pawn, PieceValues.Pawn),
            (PieceType.Knight, PieceValues.Knight),
            (PieceType.Bishop, PieceValues.Bishop),
            (PieceType.Rook, PieceValues.Rook)
        };
        #endregion

        #region Public Methods
        public static int EnemyBudget(int stageNumber)
        {
            return EnemyBudgetBase + Math.Max(0, stageNumber - 1) * EnemyBudgetPerStage;
        }
        public static PieceType StageTarget(int stageNumber)
        {
            return PieceType.King;
        }
        public static bool IsBossStage(int stageNumber)
        {
            return stageNumber == BossStage;
        }
        public static bool IsMiniBossStage(int stageNumber)
        {
            return stageNumber == MiniBossStageA || stageNumber == MiniBossStageB;
        }
        public static int EnemyExtraQueens(int stageNumber)
        {
            if (IsBossStage(stageNumber))
                return 2;
            if (IsMiniBossStage(stageNumber))
                return 1;
            return 0;
        }
        public static int EnemyKingExtraLives(int stageNumber)
        {
            if (IsBossStage(stageNumber))
                return 2;
            if (IsMiniBossStage(stageNumber))
                return 1;
            return 0;
        }
        public static int EnemyBoonCount(int stageNumber)
        {
            if (stageNumber < FirstEnemyBoonStage)
                return 0;
            return stageNumber / 3;
        }
        public static EnemyBoonId? EnemyBoonForStage(int stageNumber)
        {
            if (EnemyBoonCount(stageNumber) <= 0)
                return null;
            return EnemyBoonId.Reinforcements;
        }
        public static bool IsShopStage(int stageNumber)
        {
            for (int i = 0; i < ShopStages.Length; i++)
            {
                if (ShopStages[i] == stageNumber)
                    return true;
            }
            return false;
        }
        public static int CaptureGold(PieceType type)
        {
            if (type == PieceType.King)
                return KingCaptureGold;
            return PieceValues.Get(type) ?? 0;
        }
        public static int ShopPrice(PieceType type, Random rng)
        {
            int value = PieceValues.Get(type) ?? 0;
            int mult = rng.Next(ShopPriceMinMult, ShopPriceMaxMult + 1);
            return value * mult;
        }
        public static EnemySpawnWeights RollEnemyWeights(Random rng)
        {
            return new EnemySpawnWeights(
                rng.Next(PawnWeightMin, PawnWeightMax + 1),
                rng.Next(MinorWeightMin, MinorWeightMax + 1),
                rng.Next(MinorWeightMin, MinorWeightMax + 1),
                rng.Next(RookWeightMin, RookWeightMax + 1));
        }
        #endregion
    }

    public readonly struct EnemySpawnWeights
    {
        #region Fields
        public int Pawn { get; }
        public int Bishop { get; }
        public int Knight { get; }
        public int Rook { get; }
        #endregion

        #region Public Methods
        public EnemySpawnWeights(int pawn, int bishop, int knight, int rook)
        {
            Pawn = pawn;
            Bishop = bishop;
            Knight = knight;
            Rook = rook;
        }
        public int WeightOf(PieceType type)
        {
            switch (type)
            {
                case PieceType.Pawn:
                    return Pawn;
                case PieceType.Bishop:
                    return Bishop;
                case PieceType.Knight:
                    return Knight;
                case PieceType.Rook:
                    return Rook;
                default:
                    return 0;
            }
        }
        #endregion
    }

    public readonly struct ShopItem
    {
        #region Fields
        public PieceType Type { get; }
        public int Price { get; }
        #endregion

        #region Public Methods
        public ShopItem(PieceType type, int price)
        {
            Type = type;
            Price = price;
        }
        #endregion
    }
}
