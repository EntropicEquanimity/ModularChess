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
        readonly RunSession _session = new RunSession();
        RoguelikeRunSettings _settings = new RoguelikeRunSettings();
        Square? _selected;
        IReadOnlyList<Move> _movesFromSelection = Array.Empty<Move>();
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
        public event Action LeftRun;
        public bool IsPlaying => _session.IsActive;
        public RoguelikeRunState Run => _session.Run;
        public GameState State => _session.State;
        #endregion

        #region Unity
        void Update()
        {
            if (!_session.IsActive || _paused || _offering || _rearranging || _spawning || _clearing || _shopping)
                return;
            if (_session.State.Status != GameStatus.InProgress)
                return;
            if (boardView != null && boardView.PiecesBusy)
                return;
            if (_session.State.LegalMoves.Count == 0)
            {
                if (_stalledEmptyTurns)
                    return;
                _session.TrySkipEmptyTurn(out bool timedOut);
                _stalledEmptyTurns = _session.State.LegalMoves.Count == 0;
                RefreshHud();
                if (timedOut)
                    LoseOutOfTime();
                return;
            }
            _stalledEmptyTurns = false;
            if (_session.State.SideToMove == _session.Run.PlayerSide)
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
            _session.Start(_settings);
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
            Subscribe();
            if (boardView != null)
            {
                boardView.gameObject.SetActive(true);
                boardView.CompleteMotion();
                boardView.SetMotionPaused(false);
                boardView.ReviewVision = false;
                boardView.ViewerSide = _session.Run.PlayerSide;
            }
            hud?.Present(_session.Run);
            shop?.Dismiss();
            BeginStage(null);
        }
        public void Leave()
        {
            Unsubscribe();
            _session.Clear();
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
            _rearranging = false;
            _offering = false;
            _shopping = false;
            _spawning = false;
            _clearing = false;
            _dragRearrange = false;
            shop?.Dismiss();
            hud?.Dismiss();
            LeftRun?.Invoke();
        }
        public void GiveUp()
        {
            if (!_session.IsActive)
                return;
            hud?.ShowLose();
            shop?.Dismiss();
            _paused = true;
        }
        public void PickBoon(BoonDefinition def)
        {
            if (!_offering || def == null || !_session.IsActive)
                return;
            _session.AddBoon(def);
            _offering = false;
            hud?.HideBoonOffer();
            AfterBoon();
        }
        public void NextStage()
        {
            if (!_rearranging || !_session.IsActive)
                return;
            _rearranging = false;
            hud?.SetRearrange(false);
            if (_session.Run.IsBossStage())
            {
                hud?.ShowWin();
                return;
            }
            List<CarriedPiece> army = _session.ExtractArmy();
            _session.AdvanceStage();
            if (_session.Run.IsWon)
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
            if (!_shopping || !_session.IsActive)
                return;
            if (!_session.TryBuyShopItem(index))
                return;
            RefreshBoard();
            hud?.Refresh(_session.Run, _session.State, _session.TurnsRemaining);
            shop?.Present(_session.ShopItems, _session.Run, _session.State, BuyShopItem, RerollShop, CloseShop);
        }
        public void RerollShop()
        {
            if (!_shopping || !_session.IsActive)
                return;
            if (!_session.TryRerollShop())
                return;
            hud?.Refresh(_session.Run, _session.State, _session.TurnsRemaining);
            shop?.Present(_session.ShopItems, _session.Run, _session.State, BuyShopItem, RerollShop, CloseShop);
        }
        public void CloseShop()
        {
            if (!_shopping)
                return;
            _shopping = false;
            shop?.Dismiss();
            EnterRearrange();
        }
        public void RequestEndTurn()
        {
            if (!_session.IsActive || _paused || _offering || _rearranging || _spawning || _clearing || _shopping)
                return;
            if (_session.State.Status != GameStatus.InProgress)
                return;
            if (_session.State.SideToMove != _session.Run.PlayerSide)
                return;
            if (boardView != null && boardView.PiecesBusy)
                return;
            if (!_session.TryPassPlayerTurn(out bool timedOut))
                return;
            ClearSelection();
            RefreshBoard();
            RefreshHud();
            if (timedOut)
                LoseOutOfTime();
        }
        #endregion

        #region Private Methods
        void RefreshHud()
        {
            hud?.Refresh(_session.Run, _session.State, _session.TurnsRemaining);
        }
        void LoseOutOfTime()
        {
            _paused = true;
            shop?.Dismiss();
            hud?.ShowLoseOutOfTime();
        }
        void BeginStage(IReadOnlyList<CarriedPiece> carriedArmy)
        {
            _shopping = false;
            shop?.Dismiss();
            RoguelikeStageSpawn spawn = _session.BeginStage(carriedArmy);
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
            _enemyWait = -1f;
            _spawning = true;
            _clearing = false;
            hud?.AnnounceEnemyBoon(_session.Run.ActiveEnemyBoon);
            hud?.Refresh(_session.Run, _session.State, _session.TurnsRemaining);
            if (boardView == null)
            {
                _spawning = false;
                return;
            }
            boardView.ViewerSide = _session.Run.PlayerSide;
            var snap = new List<Guid>(4) { spawn.PlayerKingId };
            if (_firstBoardEnter && spawn.StartingPieceId.HasValue)
                snap.Add(spawn.StartingPieceId.Value);
            if (carriedArmy != null)
            {
                for (int i = 0; i < carriedArmy.Count; i++)
                    snap.Add(carriedArmy[i].Id);
            }
            var fromBottom = new List<Guid>(spawn.PlayerEntryIds);
            if (!_firstBoardEnter && carriedArmy == null && spawn.StartingPieceId.HasValue)
                fromBottom.Add(spawn.StartingPieceId.Value);
            bool slide = _firstBoardEnter;
            _firstBoardEnter = false;
            boardView.BindStageReveal(
                _session.State,
                _session.Run.PlayerSide,
                snap,
                spawn.EnemyPieceIds,
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
            _session.CaptureStageHomes();
            hud?.Refresh(_session.Run, _session.State, _session.TurnsRemaining);
        }
        void EnterRearrange()
        {
            _rearranging = true;
            _dragRearrange = false;
            hud?.SetRearrange(true);
            hud?.Refresh(_session.Run, _session.State, _session.TurnsRemaining);
        }
        void AfterBoon()
        {
            if (_session.IsActive && RoguelikeBalance.IsShopStage(_session.Run.StageNumber) && shop != null)
            {
                OpenShop();
                return;
            }
            EnterRearrange();
        }
        void OpenShop()
        {
            _shopping = true;
            _session.OpenShop();
            hud?.Refresh(_session.Run, _session.State, _session.TurnsRemaining);
            shop.Present(_session.ShopItems, _session.Run, _session.State, BuyShopItem, RerollShop, CloseShop);
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
            if (!_rearranging || !_session.IsActive || _clearing)
                return;
            Piece piece = _session.State.Board.GetPiece(square);
            if (piece == null || piece.Side != _session.Run.PlayerSide || piece.Type == PieceType.King)
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
            if (!_session.State.Board.CanPlace(square.Value))
            {
                ClearSelection();
                return;
            }
            _session.RelocateFriendly(_selected.Value, square.Value);
            ClearSelection();
            RefreshBoard();
        }
        void OnSquareClicked(Square square)
        {
            if (!_session.IsActive || _offering || _spawning || _clearing || _shopping)
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
            if (_session.State.Status != GameStatus.InProgress || _session.State.SideToMove != _session.Run.PlayerSide)
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
            Piece piece = _session.State.Board.GetPiece(square);
            if (piece == null || piece.Side != _session.Run.PlayerSide)
            {
                ClearSelection();
                return;
            }
            _selected = square;
            _movesFromSelection = _session.State.LegalMovesFrom(square);
            boardView?.SetSelection(square);
        }
        void HandleRearrangeClick(Square square)
        {
            if (_selected == null)
            {
                Piece piece = _session.State.Board.GetPiece(square);
                if (piece == null || piece.Side != _session.Run.PlayerSide || piece.Type == PieceType.King)
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
            if (!_session.State.Board.CanPlace(square))
            {
                ClearSelection();
                return;
            }
            _session.RelocateFriendly(_selected.Value, square);
            ClearSelection();
            RefreshBoard();
        }
        void ApplyMove(Move move)
        {
            RunMoveResult result = _session.ApplyMove(move);
            ClearSelection();
            RefreshBoard();
            RefreshHud();
            if (result.Status == GameStatus.StageCleared)
            {
                OnStageCleared();
                return;
            }
            if (result.Status == GameStatus.RunLost)
            {
                hud?.ShowLose();
                return;
            }
            if (result.TimedOut)
                LoseOutOfTime();
        }
        void OnStageCleared()
        {
            _clearing = true;
            _offering = false;
            _shopping = false;
            _rearranging = false;
            _session.AwardKnockedOffKingGold();
            hud?.Refresh(_session.Run, _session.State, _session.TurnsRemaining);
            Side enemy = _session.Run.PlayerSide.Opponent();
            if (boardView == null)
            {
                FinishStageClearPresentation();
                return;
            }
            boardView.PlayKnockOffSide(enemy, FinishStageClearPresentation);
        }
        void FinishStageClearPresentation()
        {
            if (!_session.IsActive)
                return;
            if (_session.Run.IsBossStage())
            {
                _clearing = false;
                hud?.ShowWin();
                return;
            }
            Dictionary<Guid, Square> livingHomes = _session.LivingHomes();
            if (boardView != null && livingHomes.Count > 0 && !AnimationPrefs.Instant)
            {
                boardView.AnimatePiecesToSquares(livingHomes, ApplyHomesAndContinue);
                return;
            }
            ApplyHomesAndContinue();
        }
        void ApplyHomesAndContinue()
        {
            if (!_session.IsActive)
                return;
            _session.PrepareRearrange();
            RefreshBoard();
            _clearing = false;
            IReadOnlyList<BoonDefinition> offer = _session.BuildBoonOffer();
            if (offer.Count == 0)
            {
                AfterBoon();
                return;
            }
            _offering = true;
            hud?.ShowBoonOffer(offer);
        }
        void PlayEnemyMove()
        {
            if (!_session.IsActive)
                return;
            Move? best = _session.ChooseEnemyMove();
            if (best == null)
            {
                _session.TrySkipEmptyTurn(out bool timedOut);
                RefreshHud();
                if (timedOut)
                    LoseOutOfTime();
                return;
            }
            ApplyMove(best.Value);
        }
        void ClearSelection()
        {
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
            boardView?.ClearSelection();
        }
        void RefreshBoard()
        {
            if (boardView == null || !_session.IsActive)
                return;
            boardView.ViewerSide = _session.Run.PlayerSide;
            boardView.Bind(_session.State);
            if (_session.State.History.Count > 0)
            {
                Move last = _session.State.History[_session.State.History.Count - 1];
                boardView.SetLastMove(last.From, last.To);
            }
            else
                boardView.ClearLastMove();
        }
        #endregion
    }
}
