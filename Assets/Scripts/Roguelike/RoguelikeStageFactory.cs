using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public sealed class RoguelikeStageSpawn
    {
        #region Fields
        public GameState State { get; }
        public Guid PlayerKingId { get; }
        public Guid? StartingPieceId { get; }
        public IReadOnlyList<Guid> EnemyPieceIds { get; }
        public IReadOnlyList<Guid> PlayerEntryIds { get; }
        #endregion

        #region Public Methods
        public RoguelikeStageSpawn(
            GameState state,
            Guid playerKingId,
            Guid? startingPieceId,
            IReadOnlyList<Guid> enemyPieceIds,
            IReadOnlyList<Guid> playerEntryIds)
        {
            State = state ?? throw new ArgumentNullException(nameof(state));
            PlayerKingId = playerKingId;
            StartingPieceId = startingPieceId;
            EnemyPieceIds = enemyPieceIds ?? Array.Empty<Guid>();
            PlayerEntryIds = playerEntryIds ?? Array.Empty<Guid>();
        }
        #endregion
    }

    public static class RoguelikeStageFactory
    {
        #region Public Methods
        public static RoguelikeStageSpawn Create(
            RoguelikeRunState run,
            Random rng,
            RoguelikeRunSettings settings = null,
            IReadOnlyList<CarriedPiece> carriedArmy = null)
        {
            if (run == null)
                throw new ArgumentNullException(nameof(run));
            Side player = run.PlayerSide;
            Side enemy = player.Opponent();
            PieceType target = run.StageTarget();
            Rules rules = StageRules.Create(player, target);
            int playerBack = player == Side.White ? 0 : 7;
            int enemyBack = enemy == Side.White ? 0 : 7;
            Piece playerKing;
            Board board;
            Guid? startingPieceId = null;
            var playerEntries = new List<Guid>(8);
            if (carriedArmy != null && carriedArmy.Count > 0)
            {
                board = Board.Empty();
                playerKing = PlaceCarriedArmy(ref board, player, playerBack, carriedArmy, playerEntries);
            }
            else
            {
                playerKing = new Piece(PieceType.King, player);
                board = Board.Empty()
                    .WithPiece(new Square(4, playerBack), playerKing);
                if (run.StageNumber == 1)
                {
                    PieceType startType = settings != null ? settings.StartingPiece : PieceType.Rook;
                    if (startType != PieceType.King && TryPlaceStartingPiece(ref board, player, startType, rng, out Guid startId))
                        startingPieceId = startId;
                }
            }
            var enemyIds = new List<Guid>(16);
            Piece enemyKing = new Piece(PieceType.King, enemy);
            board = board.WithPiece(new Square(4, enemyBack), enemyKing);
            enemyIds.Add(enemyKing.Id);
            int extraQueens = RoguelikeBalance.EnemyExtraQueens(run.StageNumber);
            for (int q = 0; q < extraQueens; q++)
                TryPlaceEnemy(ref board, enemy, PieceType.Queen, rng, enemyIds);
            int budget = RoguelikeBalance.EnemyBudget(run.StageNumber);
            board = SpendEnemyBudget(board, enemy, budget, rng, enemyIds);
            ModeRuntime runtime = ModeRuntime.Empty;
            int extraLives = RoguelikeBalance.EnemyKingExtraLives(run.StageNumber);
            if (extraLives > 0)
                runtime = runtime.GrantExtraLives(enemyKing.Id, extraLives);
            int pending = run.ConsumePendingReinforcements();
            if (pending > 0)
            {
                board = SummonPlacement.PlacePawns(
                    board, player, pending, skipBackRank: true, runtime, out runtime, rng);
                CollectSummoned(board, player, playerEntries, runtime);
            }
            if (run.ActiveEnemyBoon == EnemyBoonId.Reinforcements)
            {
                board = SummonPlacement.PlacePawns(
                    board, enemy, 1, skipBackRank: true, runtime, out runtime, rng);
                CollectSummoned(board, enemy, enemyIds, runtime);
            }
            GameState state = GameState.FromPosition(
                board,
                player,
                null,
                CastlingRights.None,
                0,
                1,
                rules: rules,
                runtime: runtime);
            return new RoguelikeStageSpawn(state, playerKing.Id, startingPieceId, enemyIds, playerEntries);
        }
        #endregion

        #region Private Methods
        static Piece PlaceCarriedArmy(
            ref Board board,
            Side player,
            int playerBack,
            IReadOnlyList<CarriedPiece> army,
            List<Guid> playerEntries)
        {
            Piece king = null;
            for (int i = 0; i < army.Count; i++)
            {
                CarriedPiece carried = army[i];
                if (carried.Type != PieceType.King)
                    continue;
                Square square = carried.Square.IsOnBoard ? carried.Square : new Square(4, playerBack);
                if (!board.CanPlace(square))
                    square = new Square(4, playerBack);
                king = new Piece(PieceType.King, player, carried.HasMoved, carried.Id);
                board = board.WithPiece(square, king);
                break;
            }
            if (king == null)
            {
                king = new Piece(PieceType.King, player);
                board = board.WithPiece(new Square(4, playerBack), king);
            }
            for (int i = 0; i < army.Count; i++)
            {
                CarriedPiece carried = army[i];
                if (carried.Type == PieceType.King)
                    continue;
                Square square = carried.Square;
                if (!square.IsOnBoard || !board.CanPlace(square))
                {
                    if (!TryFindEmpty(board, player, out square))
                        continue;
                }
                Piece piece = new Piece(carried.Type, player, carried.HasMoved, carried.Id);
                board = board.WithPiece(square, piece);
                playerEntries.Add(piece.Id);
            }
            return king;
        }
        static bool TryFindEmpty(Board board, Side player, out Square square)
        {
            int back = player == Side.White ? 0 : 7;
            int forward = player == Side.White ? 1 : -1;
            for (int depth = 0; depth < 4; depth++)
            {
                int rank = back + forward * depth;
                if (rank < 0 || rank >= Square.BoardSize)
                    break;
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    square = new Square(file, rank);
                    if (board.CanPlace(square))
                        return true;
                }
            }
            square = default;
            return false;
        }
        static void CollectSummoned(Board board, Side side, List<Guid> ids, ModeRuntime runtime)
        {
            for (int i = 0; i < 64; i++)
            {
                Piece piece = board.GetPiece(Square.FromIndex(i));
                if (piece == null || piece.Side != side)
                    continue;
                if (!runtime.IsSummoned(piece.Id))
                    continue;
                if (!ids.Contains(piece.Id))
                    ids.Add(piece.Id);
            }
        }
        static bool TryPlaceStartingPiece(
            ref Board board,
            Side player,
            PieceType type,
            Random rng,
            out Guid id)
        {
            id = Guid.Empty;
            var empties = new List<Square>(32);
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                if (board.CanPlace(square))
                    empties.Add(square);
            }
            if (empties.Count == 0)
                return false;
            Square chosen = empties[rng.Next(empties.Count)];
            Piece piece = new Piece(type, player);
            board = board.WithPiece(chosen, piece);
            if (board.GetPiece(chosen) == null)
                return false;
            id = piece.Id;
            return true;
        }
        static Board SpendEnemyBudget(
            Board board,
            Side enemy,
            int budget,
            Random rng,
            List<Guid> enemyIds)
        {
            int remaining = budget;
            if (remaining < 0)
                remaining = 0;
            var catalog = RoguelikeBalance.EnemyPiecePool;
            EnemySpawnWeights weights = RoguelikeBalance.RollEnemyWeights(rng);
            int guard = 64;
            while (remaining > 0 && guard-- > 0)
            {
                PieceType? pick = PickWeighted(catalog, weights, remaining, rng);
                if (pick == null)
                    break;
                int cost = PieceValues.Get(pick.Value) ?? remaining;
                if (!TryPlaceEnemy(ref board, enemy, pick.Value, rng, enemyIds))
                    break;
                remaining -= cost;
            }
            return board;
        }
        static PieceType? PickWeighted(
            (PieceType Type, int Cost)[] catalog,
            EnemySpawnWeights weights,
            int remaining,
            Random rng)
        {
            int total = 0;
            for (int i = 0; i < catalog.Length; i++)
            {
                if (catalog[i].Cost > remaining)
                    continue;
                int weight = weights.WeightOf(catalog[i].Type);
                if (weight > 0)
                    total += weight;
            }
            if (total <= 0)
                return null;
            int roll = rng.Next(total);
            int cursor = 0;
            for (int i = 0; i < catalog.Length; i++)
            {
                if (catalog[i].Cost > remaining)
                    continue;
                int weight = weights.WeightOf(catalog[i].Type);
                if (weight <= 0)
                    continue;
                cursor += weight;
                if (roll < cursor)
                    return catalog[i].Type;
            }
            return null;
        }
        static bool TryPlaceEnemy(ref Board board, Side enemy, PieceType type, Random rng, List<Guid> enemyIds)
        {
            int back = enemy == Side.White ? 0 : 7;
            int forward = enemy == Side.White ? 1 : -1;
            var empties = new List<Square>(32);
            for (int depth = 0; depth < 4; depth++)
            {
                int rank = back + forward * depth;
                if (rank < 0 || rank >= Square.BoardSize)
                    break;
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    Square square = new Square(file, rank);
                    if (board.CanPlace(square))
                        empties.Add(square);
                }
            }
            if (empties.Count == 0)
                return false;
            Square chosen = empties[rng.Next(empties.Count)];
            Piece piece = new Piece(type, enemy);
            board = board.WithPiece(chosen, piece);
            if (board.GetPiece(chosen) == null)
                return false;
            enemyIds.Add(piece.Id);
            return true;
        }
        #endregion
    }
}
