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
        [SerializeField] BoardView boardView;
        [SerializeField] RoguelikeHud hud;
        RoguelikeRunState _run;
        GameState _state;
        Square? _selected;
        IReadOnlyList<Move> _movesFromSelection = Array.Empty<Move>();
        bool _subscribed;
        bool _rearranging;
        bool _offering;
        bool _paused;
        float _enemyWait = -1f;
        const float EnemyThinkSeconds = 0.6f;
        System.Random _rng;
        public event Action LeftRun;
        public bool IsPlaying => _run != null && _state != null;
        public RoguelikeRunState Run => _run;
        public GameState State => _state;
        #endregion

        #region Unity
        void Update()
        {
            if (_run == null || _state == null || _paused || _offering || _rearranging)
                return;
            if (_state.Status != GameStatus.InProgress)
                return;
            if (_state.SideToMove == _run.PlayerSide)
                return;
            if (boardView != null && boardView.PiecesBusy)
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
        public void Configure(BoardView view, RoguelikeHud roguelikeHud)
        {
            Unsubscribe();
            boardView = view;
            hud = roguelikeHud;
            Subscribe();
        }
        public void Launch()
        {
            _rng = new System.Random(Environment.TickCount);
            Side player = _rng.Next(2) == 0 ? Side.White : Side.Black;
            _run = new RoguelikeRunState(player);
            _run.SetEnemyBoon(EnemyBoonId.Reinforcements);
            _rearranging = false;
            _offering = false;
            _paused = false;
            _enemyWait = -1f;
            Subscribe();
            if (boardView != null)
            {
                boardView.gameObject.SetActive(true);
                boardView.CompleteMotion();
                boardView.SetMotionPaused(false);
                boardView.ReviewVision = false;
            }
            hud?.Present(_run);
            hud?.Bind(this);
            BeginStage();
        }
        public void Leave()
        {
            Unsubscribe();
            _run = null;
            _state = null;
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
            _rearranging = false;
            _offering = false;
            hud?.Dismiss();
            LeftRun?.Invoke();
        }
        public void PickBoon(BoonDefinition def)
        {
            if (!_offering || def == null || _run == null)
                return;
            _run.AddBoon(def);
            _offering = false;
            hud?.HideBoonOffer();
            EnterRearrange();
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
            _run.AdvanceStage();
            if (_run.IsWon)
            {
                hud?.ShowWin();
                return;
            }
            _run.SetEnemyBoon(EnemyBoonId.Reinforcements);
            BeginStage();
        }
        public void Rematch()
        {
            Launch();
        }
        #endregion

        #region Private Methods
        void BeginStage()
        {
            _state = RoguelikeStageFactory.Create(_run, _rng);
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
            _enemyWait = -1f;
            hud?.AnnounceEnemyBoon(_run.ActiveEnemyBoon);
            hud?.Refresh(_run, _state);
            RefreshBoard();
        }
        void EnterRearrange()
        {
            _rearranging = true;
            hud?.SetRearrange(true);
            hud?.Refresh(_run, _state);
        }
        void Subscribe()
        {
            if (_subscribed || boardView == null)
                return;
            boardView.SquareClicked += OnSquareClicked;
            _subscribed = true;
        }
        void Unsubscribe()
        {
            if (!_subscribed || boardView == null)
                return;
            boardView.SquareClicked -= OnSquareClicked;
            _subscribed = false;
        }
        void OnSquareClicked(Square square)
        {
            if (_run == null || _state == null || _offering)
                return;
            if (_rearranging)
            {
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
            _state = _state.Apply(move);
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
            if (_run.IsBossStage())
            {
                hud?.ShowWin();
                return;
            }
            IReadOnlyList<BoonDefinition> offer = BoonOfferBuilder.Build(
                BoonCatalog.PlayerPool,
                _run.Stacks,
                _run.StageNumber,
                _rng);
            if (offer.Count == 0)
            {
                EnterRearrange();
                return;
            }
            _offering = true;
            hud?.ShowBoonOffer(offer);
        }
        void PlayEnemyMove()
        {
            if (_state == null || _state.LegalMoves.Count == 0)
                return;
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
            boardView.Bind(_state);
            if (_state.History.Count > 0)
            {
                Move last = _state.History[_state.History.Count - 1];
                boardView.SetLastMove(last.From, last.To);
            }
            else
                boardView.ClearLastMove();
        }
        #endregion
    }
}
