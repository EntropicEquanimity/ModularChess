using System;
using System.Collections.Generic;

namespace ModularChess.Core
{
    public readonly struct RunMoveResult
    {
        #region Fields
        public GameStatus Status { get; }
        public int GoldGained { get; }
        #endregion

        #region Public Methods
        public RunMoveResult(GameStatus status, int goldGained)
        {
            Status = status;
            GoldGained = goldGained;
        }
        #endregion
    }

    public sealed class RunSession
    {
        #region Fields
        readonly Dictionary<Guid, Square> _stageHomes = new Dictionary<Guid, Square>(16);
        readonly Random _rng;
        RoguelikeRunSettings _settings;
        List<ShopItem> _shopItems = new List<ShopItem>();
        public RoguelikeRunState Run { get; private set; }
        public GameState State { get; private set; }
        public RoguelikeStageSpawn LastSpawn { get; private set; }
        public IReadOnlyList<ShopItem> ShopItems => _shopItems;
        public IReadOnlyDictionary<Guid, Square> StageHomes => _stageHomes;
        public bool IsActive => Run != null && State != null;
        #endregion

        #region Public Methods
        public RunSession(Random rng = null)
        {
            _rng = rng ?? new Random();
        }
        public void Start(RoguelikeRunSettings settings = null)
        {
            _settings = settings ?? new RoguelikeRunSettings();
            Side player = ResolvePlayerSide(_settings.PlayerColor);
            Run = new RoguelikeRunState(player);
            State = null;
            LastSpawn = null;
            _shopItems = new List<ShopItem>();
            _stageHomes.Clear();
        }
        public void Clear()
        {
            Run = null;
            State = null;
            LastSpawn = null;
            _shopItems = new List<ShopItem>();
            _stageHomes.Clear();
        }
        public RoguelikeStageSpawn BeginStage(IReadOnlyList<CarriedPiece> carriedArmy = null)
        {
            if (Run == null)
            {
                throw new InvalidOperationException("Run is not started.");
            }
            Run.SetEnemyBoon(RoguelikeBalance.EnemyBoonForStage(Run.StageNumber));
            LastSpawn = RoguelikeStageFactory.Create(Run, _rng, _settings, carriedArmy);
            State = LastSpawn.State;
            CaptureStageHomes();
            return LastSpawn;
        }
        public void CaptureStageHomes()
        {
            _stageHomes.Clear();
            if (State == null || Run == null)
            {
                return;
            }
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = State.Board.GetPiece(square);
                if (piece == null || piece.Side != Run.PlayerSide)
                {
                    continue;
                }
                if (State.Runtime.IsSummoned(piece.Id))
                {
                    continue;
                }
                _stageHomes[piece.Id] = square;
            }
        }
        public RunMoveResult ApplyMove(Move move)
        {
            if (State == null || Run == null)
            {
                throw new InvalidOperationException("No active Stage.");
            }
            Side mover = State.SideToMove;
            PieceType? captured = move.CapturedType;
            State = State.Apply(move);
            int gold = 0;
            if (captured != null && mover == Run.PlayerSide && !CaptureBounced(move, captured.Value))
            {
                gold = RoguelikeBalance.CaptureGold(captured.Value);
                Run.AddGold(gold);
            }
            return new RunMoveResult(State.Status, gold);
        }
        public Move? ChooseEnemyMove()
        {
            return ChooseEnemyMove(State, _rng);
        }
        public static Move? ChooseEnemyMove(GameState state, Random rng)
        {
            if (state == null || state.LegalMoves.Count == 0 || rng == null)
            {
                return null;
            }
            for (int i = 0; i < state.LegalMoves.Count; i++)
            {
                Move move = state.LegalMoves[i];
                if (move.CapturedType != null)
                {
                    return move;
                }
            }
            return state.LegalMoves[rng.Next(state.LegalMoves.Count)];
        }
        public bool TrySkipEmptyTurn()
        {
            if (State == null || State.LegalMoves.Count > 0)
            {
                return false;
            }
            State = State.WithSideToMove(State.SideToMove.Opponent());
            return true;
        }
        public void OpenShop()
        {
            _shopItems = new List<ShopItem>(ShopOfferBuilder.Build(_rng));
        }
        public bool TryBuyShopItem(int index)
        {
            if (Run == null || State == null)
            {
                return false;
            }
            if (index < 0 || index >= _shopItems.Count)
            {
                return false;
            }
            ShopItem item = _shopItems[index];
            if (item.Price < 0)
            {
                return false;
            }
            if (CountArmy(State, Run.PlayerSide) >= Run.ArmySizeCap)
            {
                return false;
            }
            if (!Run.TrySpendGold(item.Price))
            {
                return false;
            }
            GameState next = RunPlacement.PlacePurchased(State, Run.PlayerSide, item.Type);
            if (next == null)
            {
                Run.AddGold(item.Price);
                return false;
            }
            State = next;
            var updated = new List<ShopItem>(_shopItems.Count);
            for (int i = 0; i < _shopItems.Count; i++)
            {
                updated.Add(i == index ? new ShopItem(item.Type, -1) : _shopItems[i]);
            }
            _shopItems = updated;
            return true;
        }
        public bool TryRerollShop()
        {
            if (Run == null)
            {
                return false;
            }
            if (!Run.TrySpendGold(RoguelikeBalance.ShopRerollCost))
            {
                return false;
            }
            _shopItems = new List<ShopItem>(ShopOfferBuilder.Build(_rng));
            return true;
        }
        public void AddBoon(BoonDefinition def)
        {
            if (Run == null)
            {
                throw new InvalidOperationException("Run is not started.");
            }
            Run.AddBoon(def);
        }
        public IReadOnlyList<BoonDefinition> BuildBoonOffer()
        {
            if (Run == null)
            {
                return Array.Empty<BoonDefinition>();
            }
            return BoonOfferBuilder.Build(BoonCatalog.PlayerPool, Run.Stacks, Run.StageNumber, _rng);
        }
        public void AwardKnockedOffKingGold()
        {
            if (State == null || Run == null)
            {
                return;
            }
            Side enemy = Run.PlayerSide.Opponent();
            for (int i = 0; i < 64; i++)
            {
                Piece piece = State.Board.GetPiece(Square.FromIndex(i));
                if (piece == null || piece.Side != enemy || piece.Type != PieceType.King)
                {
                    continue;
                }
                Run.AddGold(RoguelikeBalance.CaptureGold(PieceType.King));
                return;
            }
        }
        public void PrepareRearrange()
        {
            if (State == null || Run == null)
            {
                return;
            }
            State = State.PrepareRearrange(Run.PlayerSide, _stageHomes);
        }
        public Dictionary<Guid, Square> LivingHomes()
        {
            var result = new Dictionary<Guid, Square>(_stageHomes.Count);
            if (State == null || Run == null)
            {
                return result;
            }
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = State.Board.GetPiece(square);
                if (piece == null || piece.Side != Run.PlayerSide)
                {
                    continue;
                }
                if (State.Runtime.IsSummoned(piece.Id))
                {
                    continue;
                }
                if (!_stageHomes.TryGetValue(piece.Id, out Square home))
                {
                    continue;
                }
                if (!square.Equals(home))
                {
                    result[piece.Id] = home;
                }
            }
            return result;
        }
        public void RelocateFriendly(Square from, Square to)
        {
            if (State == null)
            {
                return;
            }
            State = State.RelocateFriendly(from, to);
        }
        public List<CarriedPiece> ExtractArmy()
        {
            var army = new List<CarriedPiece>(16);
            if (State == null || Run == null)
            {
                return army;
            }
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = State.Board.GetPiece(square);
                if (piece == null || piece.Side != Run.PlayerSide)
                {
                    continue;
                }
                if (State.Runtime.IsSummoned(piece.Id))
                {
                    continue;
                }
                army.Add(new CarriedPiece(piece.Type, square, piece.Id, piece.HasMoved));
            }
            return army;
        }
        public void AdvanceStage()
        {
            if (Run == null)
            {
                return;
            }
            Run.AdvanceStage();
        }
        public static int CountArmy(GameState state, Side player)
        {
            if (state == null)
            {
                return 0;
            }
            int count = 0;
            foreach (Piece piece in state.Board.OccupiedPieces)
            {
                if (piece.Side == player && piece.Type != PieceType.King && !state.Runtime.IsSummoned(piece.Id))
                {
                    count++;
                }
            }
            return count;
        }
        #endregion

        #region Private Methods
        bool CaptureBounced(Move move, PieceType captured)
        {
            Piece occupant = State.Board.GetPiece(move.To);
            return occupant != null && occupant.Type == captured && occupant.Side != Run.PlayerSide;
        }
        Side ResolvePlayerSide(HostColor color)
        {
            switch (color)
            {
                case HostColor.White:
                    return Side.White;
                case HostColor.Black:
                    return Side.Black;
                default:
                    return _rng.Next(2) == 0 ? Side.White : Side.Black;
            }
        }
        #endregion
    }
}
