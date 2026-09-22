using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public static class RoguelikeStageFactory
    {
        #region Public Methods
        public static GameState Create(RoguelikeRunState run, Random rng)
        {
            if (run == null)
                throw new ArgumentNullException(nameof(run));
            Side player = run.PlayerSide;
            Side enemy = player.Opponent();
            PieceType target = run.StageTarget();
            MatchRules rules = MatchRules.Roguelike(player, target);
            int playerBack = player == Side.White ? 0 : 7;
            int enemyBack = enemy == Side.White ? 0 : 7;
            Board board = Board.Empty()
                .WithPiece(new Square(4, playerBack), new Piece(PieceType.King, player));
            if (target == PieceType.King)
            {
                board = board.WithPiece(new Square(4, enemyBack), new Piece(PieceType.King, enemy));
            }
            else
            {
                board = board.WithPiece(new Square(4, enemyBack), new Piece(PieceType.Queen, enemy));
            }
            int budget = EnemyBudget(run.StageNumber);
            board = SpendEnemyBudget(board, enemy, budget, target, rng);
            ModeRuntime runtime = ModeRuntime.Empty;
            int pending = run.ConsumePendingReinforcements();
            if (pending > 0)
            {
                board = SummonPlacement.PlacePawns(
                    board, player, pending, skipBackRank: true, runtime, out runtime, rng);
            }
            if (run.ActiveEnemyBoon == EnemyBoonId.Reinforcements)
            {
                board = SummonPlacement.PlacePawns(
                    board, enemy, 1, skipBackRank: true, runtime, out runtime, rng);
            }
            return GameState.FromPosition(
                board,
                player,
                null,
                CastlingRights.None,
                0,
                1,
                rules: rules,
                runtime: runtime);
        }
        public static int EnemyBudget(int stageNumber)
        {
            return 2 + Math.Max(0, stageNumber - 1) * 2;
        }
        #endregion

        #region Private Methods
        static Board SpendEnemyBudget(Board board, Side enemy, int budget, PieceType target, Random rng)
        {
            int spent = target == PieceType.Queen ? PieceValues.Queen : 0;
            int remaining = budget - spent;
            if (remaining < 0)
                remaining = 0;
            var catalog = new[]
            {
                (PieceType.Pawn, PieceValues.Pawn),
                (PieceType.Knight, PieceValues.Knight),
                (PieceType.Bishop, PieceValues.Bishop),
                (PieceType.Rook, PieceValues.Rook),
                (PieceType.Queen, PieceValues.Queen)
            };
            int guard = 64;
            while (remaining > 0 && guard-- > 0)
            {
                var affordable = new List<(PieceType type, int cost)>(5);
                for (int i = 0; i < catalog.Length; i++)
                {
                    if (catalog[i].Item2 <= remaining)
                        affordable.Add(catalog[i]);
                }
                if (affordable.Count == 0)
                    break;
                var pick = affordable[rng.Next(affordable.Count)];
                if (!TryPlaceEnemy(ref board, enemy, pick.type, rng))
                    break;
                remaining -= pick.cost;
            }
            return board;
        }
        static bool TryPlaceEnemy(ref Board board, Side enemy, PieceType type, Random rng)
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
            board = board.WithPiece(chosen, new Piece(type, enemy));
            return board.GetPiece(chosen) != null;
        }
        #endregion
    }
}
