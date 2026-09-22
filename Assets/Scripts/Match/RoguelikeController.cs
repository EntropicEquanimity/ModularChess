using System;
using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using UnityEngine;

namespace ModularChess.Match
{
    public sealed class RoguelikeController : MonoBehaviour
    {
        #region Fields
        const float EnemyThinkSeconds = 0.6f;
        const float PieceEntrySeconds = 1f;
        const float PieceStaggerSeconds = 0.25f;
        const float BoardEnterSeconds = 2f;
        [SerializeField] BoardView boardView;
        [SerializeField] RoguelikeHud hud;
        [SerializeField] RoguelikeShopView shop;
        RoguelikeRunState _run;
        RoguelikeRunSettings _settings = new RoguelikeRunSettings();
        RoguelikeStageSpawn _spawn;
        GameState _state;
        readonly Dictionary<Guid, Square> _stageHomes = new Dictionary<Guid, Square>(16);
        Square? _selected;
        IReadOnlyList<Move> _movesFromSelection = Array.Empty<Move>();
        IReadOnlyList<ShopItem> _shopItems = Array.Empty<ShopItem>();
        bool _subscribed;
        bool _rearranging;
        bool _offering;
        bool _shopping;
        bool _paused;
        bool _spawning;
        bool _clearing;
        bool _dragRearrange;
        bool _firstBoardEnter = true;
        bool _stalledEmptyTurns;
        float _enemyWait = -1f;
        System.Random _rng;
        public event Action LeftRun;
        public bool IsPlaying => _run != null && _state != null;
        public RoguelikeRunState Run => _run;
        public GameState State => _state;
        #endregion

        #region Unity
        void Update()
        {
            if (_run == null || _state == null || _paused || _offering || _rearranging || _spawning || _clearing || _shopping)
                return;
            if (_state.Status != GameStatus.InProgress)
                return;
            if (boardView != null && boardView.PiecesBusy)
                return;
            if (_state.LegalMoves.Count == 0)
            {
                if (_stalledEmptyTurns)
                    return;
                SkipTurn(_state.SideToMove.Opponent());
                _stalledEmptyTurns = _state.LegalMoves.Count == 0;
                return;
            }
            _stalledEmptyTurns = false;
            if (_state.SideToMove == _run.PlayerSide)
                return;
            if (_enemyWait < 0f)
                _enemyWait = EnemyThinkSeconds;
            _enemyWait -= Time.deltaTime;
            if (_enemyWait > 0f)
                return;
            _enemyWait = -1f;
            PlayEnemyMove();
        }
        void OnDestroy()
        {
            Unsubscribe();
        }
        #endregion

        #region Public Methods
        public void Configure(BoardView view, RoguelikeHud roguelikeHud, RoguelikeShopView shopView = null)
        {
            Unsubscribe();
            boardView = view;
            hud = roguelikeHud;
            shop = shopView;
            Subscribe();
        }
        public void Launch(RoguelikeRunSettings settings = null)
        {
            _settings = settings ?? new RoguelikeRunSettings();
            _rng = new System.Random(Environment.TickCount);
            Side player = ResolvePlayerSide(_settings.PlayerColor);
            _run = new RoguelikeRunState(player);
            _rearranging = false;
            _offering = false;
            _shopping = false;
            _paused = false;
            _spawning = false;
            _clearing = false;
            _dragRearrange = false;
            _firstBoardEnter = true;
            _stalledEmptyTurns = false;
            _enemyWait = -1f;
            _stageHomes.Clear();
            Subscribe();
            if (boardView != null)
            {
                boardView.gameObject.SetActive(true);
                boardView.CompleteMotion();
                boardView.SetMotionPaused(false);
                boardView.ReviewVision = false;
                boardView.ViewerSide = player;
            }
            hud?.Present(_run);
            shop?.Dismiss();
            BeginStage(null);
        }
        public void Leave()
        {
            Unsubscribe();
            _run = null;
            _state = null;
            _spawn = null;
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
            _rearranging = false;
            _offering = false;
            _shopping = false;
            _spawning = false;
            _clearing = false;
            _dragRearrange = false;
            _stageHomes.Clear();
            shop?.Dismiss();
            hud?.Dismiss();
            LeftRun?.Invoke();
        }
        public void GiveUp()
        {
            if (_run == null)
                return;
            hud?.ShowLose();
            shop?.Dismiss();
            _paused = true;
        }
        public void PickBoon(BoonDefinition def)
        {
            if (!_offering || def == null || _run == null)
                return;
            _run.AddBoon(def);
            _offering = false;
            hud?.HideBoonOffer();
            AfterBoon();
        }
        public void NextStage()
        {
            if (!_rearranging || _run == null)
                return;
            _rearranging = false;
            hud?.SetRearrange(false);
            if (_run.IsBossStage())
            {
                hud?.ShowWin();
                return;
            }
            List<CarriedPiece> army = ExtractArmy(_state, _run.PlayerSide);
            _run.AdvanceStage();
            if (_run.IsWon)
            {
                hud?.ShowWin();
                return;
            }
            BeginStage(army);
        }
        public void Rematch()
        {
            Launch(_settings);
        }
        public void BuyShopItem(int index)
        {
            if (!_shopping || _run == null || _state == null)
                return;
            if (index < 0 || index >= _shopItems.Count)
                return;
            ShopItem item = _shopItems[index];
            if (item.Price < 0)
                return;
            if (CountArmy(_state, _run.PlayerSide) >= _run.ArmySizeCap)
                return;
            if (!TryFindShopSquare(out Square square))
                return;
            if (!_run.TrySpendGold(item.Price))
                return;
            _state = _state.AddPiece(new Piece(item.Type, _run.PlayerSide), square);
            var next = new List<ShopItem>(_shopItems.Count);
            for (int i = 0; i < _shopItems.Count; i++)
                next.Add(i == index ? new ShopItem(item.Type, -1) : _shopItems[i]);
            _shopItems = next;
            RefreshBoard();
            hud?.Refresh(_run, _state);
            shop?.Present(_shopItems, _run, _state, BuyShopItem, RerollShop, CloseShop);
        }
        public void RerollShop()
        {
            if (!_shopping || _run == null)
                return;
            if (!_run.TrySpendGold(RoguelikeBalance.ShopRerollCost))
                return;
            _shopItems = ShopOfferBuilder.Build(_rng);
            hud?.Refresh(_run, _state);
            shop?.Present(_shopItems, _run, _state, BuyShopItem, RerollShop, CloseShop);
        }
        public void CloseShop()
        {
            if (!_shopping)
                return;
            _shopping = false;
            shop?.Dismiss();
            EnterRearrange();
        }
        #endregion

        #region Private Methods
        void BeginStage(IReadOnlyList<CarriedPiece> carriedArmy)
        {
            _run.SetEnemyBoon(RoguelikeBalance.EnemyBoonForStage(_run.StageNumber));
            _shopping = false;
            shop?.Dismiss();
            _spawn = RoguelikeStageFactory.Create(_run, _rng, _settings, carriedArmy);
            _state = _spawn.State;
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
            _enemyWait = -1f;
            _spawning = true;
            _clearing = false;
            CaptureStageHomes();
            hud?.AnnounceEnemyBoon(_run.ActiveEnemyBoon);
            hud?.Refresh(_run, _state);
            if (boardView == null)
            {
                _spawning = false;
                return;
            }
            boardView.ViewerSide = _run.PlayerSide;
            var snap = new List<Guid>(4) { _spawn.PlayerKingId };
            if (_firstBoardEnter && _spawn.StartingPieceId.HasValue)
                snap.Add(_spawn.StartingPieceId.Value);
            if (carriedArmy != null)
            {
                for (int i = 0; i < carriedArmy.Count; i++)
                    snap.Add(carriedArmy[i].Id);
            }
            var fromBottom = new List<Guid>(_spawn.PlayerEntryIds);
            if (!_firstBoardEnter && carriedArmy == null && _spawn.StartingPieceId.HasValue)
                fromBottom.Add(_spawn.StartingPieceId.Value);
            bool slide = _firstBoardEnter;
            _firstBoardEnter = false;
            boardView.BindStageReveal(
                _state,
                _run.PlayerSide,
                snap,
                _spawn.EnemyPieceIds,
                fromBottom,
                PieceEntrySeconds,
                PieceStaggerSeconds,
                startStagger: !slide,
                onComplete: slide ? null : OnSpawnComplete);
            if (slide)
            {
                boardView.PlayBoardSlideIn(BoardEnterSeconds, () =>
                    boardView.PlayQueuedStageEntries(OnSpawnComplete));
            }
        }
        void OnSpawnComplete()
        {
            _spawning = false;
            CaptureStageHomes();
            hud?.Refresh(_run, _state);
        }
        void CaptureStageHomes()
        {
            _stageHomes.Clear();
            if (_state == null || _run == null)
                return;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = _state.Board.GetPiece(square);
                if (piece == null || piece.Side != _run.PlayerSide)
                    continue;
                if (_state.Runtime.IsSummoned(piece.Id))
                    continue;
                _stageHomes[piece.Id] = square;
            }
        }
        void EnterRearrange()
        {
            _rearranging = true;
            _dragRearrange = false;
            hud?.SetRearrange(true);
            hud?.Refresh(_run, _state);
        }
        void AfterBoon()
        {
            if (_run != null && RoguelikeBalance.IsShopStage(_run.StageNumber) && shop != null)
            {
                OpenShop();
                return;
            }
            EnterRearrange();
        }
        void OpenShop()
        {
            _shopping = true;
            _shopItems = ShopOfferBuilder.Build(_rng);
            hud?.Refresh(_run, _state);
            shop.Present(_shopItems, _run, _state, BuyShopItem, RerollShop, CloseShop);
        }
        void Subscribe()
        {
            if (_subscribed || boardView == null)
                return;
            boardView.SquareClicked += OnSquareClicked;
            boardView.SquarePressed += OnSquarePressed;
            boardView.SquareReleased += OnSquareReleased;
            _subscribed = true;
        }
        void Unsubscribe()
        {
            if (!_subscribed || boardView == null)
                return;
            boardView.SquareClicked -= OnSquareClicked;
            boardView.SquarePressed -= OnSquarePressed;
            boardView.SquareReleased -= OnSquareReleased;
            _subscribed = false;
        }
        void OnSquarePressed(Square square)
        {
            if (!_rearranging || _run == null || _state == null || _clearing)
                return;
            Piece piece = _state.Board.GetPiece(square);
            if (piece == null || piece.Side != _run.PlayerSide || piece.Type == PieceType.King)
            {
                _dragRearrange = false;
                return;
            }
            _selected = square;
            _dragRearrange = true;
            boardView?.SetSelection(square);
        }
        void OnSquareReleased(Square? square)
        {
            if (!_rearranging || !_dragRearrange)
                return;
            _dragRearrange = false;
            if (_selected == null || square == null)
                return;
            if (_selected.Value.Equals(square.Value))
                return;
            if (!_state.Board.CanPlace(square.Value))
            {
                ClearSelection();
                return;
            }
            _state = _state.RelocateFriendly(_selected.Value, square.Value);
            ClearSelection();
            RefreshBoard();
        }
        void OnSquareClicked(Square square)
        {
            if (_run == null || _state == null || _offering || _spawning || _clearing || _shopping)
                return;
            if (boardView != null && boardView.PiecesBusy)
                return;
            if (_rearranging)
            {
                if (_dragRearrange)
                    return;
                HandleRearrangeClick(square);
                return;
            }
            if (_state.Status != GameStatus.InProgress || _state.SideToMove != _run.PlayerSide)
                return;
            if (_selected != null)
            {
                for (int i = 0; i < _movesFromSelection.Count; i++)
                {
                    if (_movesFromSelection[i].To.Equals(square))
                    {
                        ApplyMove(_movesFromSelection[i]);
                        return;
                    }
                }
            }
            Piece piece = _state.Board.GetPiece(square);
            if (piece == null || piece.Side != _run.PlayerSide)
            {
                ClearSelection();
                return;
            }
            _selected = square;
            _movesFromSelection = _state.LegalMovesFrom(square);
            boardView?.SetSelection(square);
        }
        void HandleRearrangeClick(Square square)
        {
            if (_selected == null)
            {
                Piece piece = _state.Board.GetPiece(square);
                if (piece == null || piece.Side != _run.PlayerSide || piece.Type == PieceType.King)
                    return;
                _selected = square;
                boardView?.SetSelection(square);
                return;
            }
            if (_selected.Value.Equals(square))
            {
                ClearSelection();
                return;
            }
            if (!_state.Board.CanPlace(square))
            {
                ClearSelection();
                return;
            }
            _state = _state.RelocateFriendly(_selected.Value, square);
            ClearSelection();
            RefreshBoard();
        }
        void ApplyMove(Move move)
        {
            Side mover = _state.SideToMove;
            PieceType? captured = move.CapturedType;
            _state = _state.Apply(move);
            if (captured != null && mover == _run.PlayerSide && !CaptureBounced(move, captured.Value))
                _run.AddGold(RoguelikeBalance.CaptureGold(captured.Value));
            ClearSelection();
            RefreshBoard();
            hud?.Refresh(_run, _state);
            if (_state.Status == GameStatus.StageCleared)
            {
                OnStageCleared();
                return;
            }
            if (_state.Status == GameStatus.RunLost)
                hud?.ShowLose();
        }
        void OnStageCleared()
        {
            _clearing = true;
            _offering = false;
            _shopping = false;
            _rearranging = false;
            AwardKnockedOffKingGold();
            Side enemy = _run.PlayerSide.Opponent();
            if (boardView == null)
            {
                FinishStageClearPresentation();
                return;
            }
            boardView.PlayKnockOffSide(enemy, FinishStageClearPresentation);
        }
        void FinishStageClearPresentation()
        {
            if (_run == null)
                return;
            if (_run.IsBossStage())
            {
                _clearing = false;
                hud?.ShowWin();
                return;
            }
            Dictionary<Guid, Square> livingHomes = LivingHomes();
            if (boardView != null && livingHomes.Count > 0 && !AnimationPrefs.Instant)
            {
                boardView.AnimatePiecesToSquares(livingHomes, ApplyHomesAndContinue);
                return;
            }
            ApplyHomesAndContinue();
        }
        void ApplyHomesAndContinue()
        {
            if (_run == null || _state == null)
                return;
            _state = _state.PrepareRearrange(_run.PlayerSide, _stageHomes);
            RefreshBoard();
            _clearing = false;
            IReadOnlyList<BoonDefinition> offer = BoonOfferBuilder.Build(
                BoonCatalog.PlayerPool,
                _run.Stacks,
                _run.StageNumber,
                _rng);
            if (offer.Count == 0)
            {
                AfterBoon();
                return;
            }
            _offering = true;
            hud?.ShowBoonOffer(offer);
        }
        Dictionary<Guid, Square> LivingHomes()
        {
            var result = new Dictionary<Guid, Square>(_stageHomes.Count);
            if (_state == null || _run == null)
                return result;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = _state.Board.GetPiece(square);
                if (piece == null || piece.Side != _run.PlayerSide)
                    continue;
                if (_state.Runtime.IsSummoned(piece.Id))
                    continue;
                if (!_stageHomes.TryGetValue(piece.Id, out Square home))
                    continue;
                if (!square.Equals(home))
                    result[piece.Id] = home;
            }
            return result;
        }
        void PlayEnemyMove()
        {
            if (_state == null)
                return;
            if (_state.LegalMoves.Count == 0)
            {
                SkipTurn(_state.SideToMove.Opponent());
                return;
            }
            Move? best = null;
            for (int i = 0; i < _state.LegalMoves.Count; i++)
            {
                Move move = _state.LegalMoves[i];
                if (move.CapturedType != null)
                {
                    best = move;
                    break;
                }
            }
            if (best == null)
                best = _state.LegalMoves[_rng.Next(_state.LegalMoves.Count)];
            ApplyMove(best.Value);
        }
        void SkipTurn(Side next)
        {
            if (_run == null || _state == null)
                return;
            _state = _state.WithSideToMove(next);
            _enemyWait = -1f;
            ClearSelection();
            hud?.Refresh(_run, _state);
        }
        bool CaptureBounced(Move move, PieceType captured)
        {
            Piece occupant = _state.Board.GetPiece(move.To);
            return occupant != null && occupant.Type == captured && occupant.Side != _run.PlayerSide;
        }
        void AwardKnockedOffKingGold()
        {
            if (_state == null || _run == null)
                return;
            Side enemy = _run.PlayerSide.Opponent();
            for (int i = 0; i < 64; i++)
            {
                Piece piece = _state.Board.GetPiece(Square.FromIndex(i));
                if (piece == null || piece.Side != enemy || piece.Type != PieceType.King)
                    continue;
                _run.AddGold(RoguelikeBalance.CaptureGold(PieceType.King));
                hud?.Refresh(_run, _state);
                return;
            }
        }
        void ClearSelection()
        {
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
            boardView?.ClearSelection();
        }
        void RefreshBoard()
        {
            if (boardView == null || _state == null)
                return;
            boardView.ViewerSide = _run.PlayerSide;
            boardView.Bind(_state);
            if (_state.History.Count > 0)
            {
                Move last = _state.History[_state.History.Count - 1];
                boardView.SetLastMove(last.From, last.To);
            }
            else
                boardView.ClearLastMove();
        }
        static List<CarriedPiece> ExtractArmy(GameState state, Side player)
        {
            var army = new List<CarriedPiece>(16);
            if (state == null)
                return army;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = state.Board.GetPiece(square);
                if (piece == null || piece.Side != player)
                    continue;
                if (state.Runtime.IsSummoned(piece.Id))
                    continue;
                army.Add(new CarriedPiece(piece.Type, square, piece.Id, piece.HasMoved));
            }
            return army;
        }
        static int CountArmy(GameState state, Side player)
        {
            if (state == null)
                return 0;
            int count = 0;
            foreach (Piece piece in state.Board.OccupiedPieces)
            {
                if (piece.Side == player && piece.Type != PieceType.King && !state.Runtime.IsSummoned(piece.Id))
                    count++;
            }
            return count;
        }
        bool TryFindShopSquare(out Square square)
        {
            square = default;
            if (_state == null || _run == null)
                return false;
            int back = _run.PlayerSide == Side.White ? 0 : 7;
            int forward = _run.PlayerSide == Side.White ? 1 : -1;
            for (int depth = 0; depth < 4; depth++)
            {
                int rank = back + forward * depth;
                if (rank < 0 || rank >= Square.BoardSize)
                    break;
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    square = new Square(file, rank);
                    if (_state.Board.CanPlace(square))
                        return true;
                }
            }
            return false;
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
