using System;
using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using UnityEngine;

namespace ModularChess.Match
{
    [DefaultExecutionOrder(50)]
    public sealed class MatchController : MonoBehaviour, IInitializable
    {
        [SerializeField] private BoardView boardView;
        [SerializeField] private PromotionPicker promotionPicker;
        [SerializeField] private MatchHud hud;

        MatchSession _session;
        GameState _state;
        Square? _selected;
        IReadOnlyList<Move> _movesFromSelection = Array.Empty<Move>();
        List<Move> _pendingPromotions;
        bool _subscribed;
        bool _inSetup;
        bool _paused;
        float _setupRemaining;
        readonly List<Guid> _whitePicks = new List<Guid>();
        readonly List<Guid> _blackPicks = new List<Guid>();
        MatchClock _clock;
        public event Action LeftMatch;

        public GameState State => _state;
        public bool IsPlaying => _session != null && _state != null;

        public void Configure(BoardView view, PromotionPicker picker, MatchHud matchHud)
        {
            Unsubscribe();
            boardView = view;
            promotionPicker = picker;
            hud = matchHud;
            Subscribe();
        }

        public void Initialize()
        {
            Subscribe();
        }

        private void Start()
        {
            if (boardView == null)
                boardView = FindAnyObjectByType<BoardView>();
            if (promotionPicker == null)
                promotionPicker = FindAnyObjectByType<PromotionPicker>();
            if (hud == null)
                hud = FindAnyObjectByType<MatchHud>();
            Subscribe();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Update()
        {
            if (_session == null || _state == null || _paused)
            {
                return;
            }

            if (_inSetup)
            {
                _setupRemaining -= Time.deltaTime;
                if (_setupRemaining <= 0f)
                {
                    FinishSetup(timeout: true);
                }

                return;
            }

            if (_state.DraftPending && _session.IsAi && _state.SideToMove != _session.PlayerSide)
            {
                ResolveAiDraft();
                return;
            }

            if (_clock != null && _state.Status == GameStatus.InProgress && !_state.DraftPending)
            {
                Side? flagged = _clock.Tick(Time.deltaTime, _state.SideToMove);
                if (flagged != null)
                {
                    _state = _state.WithTerminal(GameStatus.Timeout);
                    RefreshPresentation();
                    return;
                }
            }

            if (_session.IsAi
                && _state.Status == GameStatus.InProgress
                && !_state.DraftPending
                && _state.SideToMove != _session.PlayerSide
                && _pendingPromotions == null)
            {
                PlayAi();
            }
        }

        public void Launch(MatchSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
            Subscribe();
            _state = GameState.StartingPosition(session.Rules);
            _pendingPromotions = null;
            ClearSelection();
            _whitePicks.Clear();
            _blackPicks.Clear();
            _paused = false;
            _clock = new MatchClock(session.Rules.Settings.Time);
            hud?.BindActions(this);
            if (promotionPicker != null)
                promotionPicker.Hide();

            _inSetup = session.Rules.Has(ModeId.PowerfulPieces);
            if (_inSetup)
            {
                _setupRemaining = 30f;
                if (session.IsAi)
                {
                    Side aiSide = session.PlayerSide.Opponent();
                    List<Guid> aiPicks = aiSide == Side.White ? _whitePicks : _blackPicks;
                    SimpleAi.AutopickEmpowered(_state, aiSide, session.Rules.Settings.EmpoweredCount, aiPicks);
                }
            }
            else
            {
                _clock.Start();
            }

            if (boardView != null)
                boardView.gameObject.SetActive(true);

            RefreshPresentation();
        }

        public void LeaveToMenu()
        {
            if (_state != null && _state.Status == GameStatus.InProgress)
            {
                _state = _state.WithTerminal(_inSetup ? GameStatus.Aborted : GameStatus.Resign);
            }

            _session = null;
            _inSetup = false;
            _clock?.Stop();
            LeftMatch?.Invoke();
        }

        public void Resign()
        {
            if (_state == null || _inSetup || _state.Status != GameStatus.InProgress)
                return;
            _state = _state.WithTerminal(GameStatus.Resign);
            RefreshPresentation();
        }

        public void TogglePause()
        {
            if (_session == null || !_session.IsAi || _inSetup || _state == null || _state.DraftPending)
                return;
            _paused = !_paused;
            if (_paused)
                _clock?.Stop();
            else
                _clock?.Start();
            hud?.SetStatusLine(_paused ? "Paused" : string.Empty);
        }

        public void RequestEndTurn()
        {
            if (_state == null || !_state.CanEndTurn())
                return;
            Side ended = _state.SideToMove;
            _state = _state.EndTurn();
            _clock?.AddIncrement(ended);
            ClearSelection();
            RefreshPresentation();
        }

        private void Subscribe()
        {
            if (_subscribed || boardView == null)
                return;
            boardView.SquareClicked += OnSquareClicked;
            if (promotionPicker != null)
                promotionPicker.PromotionChosen += OnPromotionChosen;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            if (boardView != null)
                boardView.SquareClicked -= OnSquareClicked;
            if (promotionPicker != null)
                promotionPicker.PromotionChosen -= OnPromotionChosen;
            _subscribed = false;
        }

        private void OnSquareClicked(Square square)
        {
            if (_inSetup)
            {
                HandleSetupClick(square);
                return;
            }

            if (!CanAcceptBoardInput())
                return;

            if (_selected.HasValue)
            {
                Square selected = _selected.Value;
                if (selected.Equals(square))
                {
                    ClearSelection();
                    RefreshPresentation();
                    return;
                }

                if (IsFriendly(square))
                {
                    Select(square);
                    RefreshPresentation();
                    return;
                }

                List<Move> destinations = MovesTo(square);
                if (destinations.Count == 0)
                {
                    ClearSelection();
                    RefreshPresentation();
                    return;
                }

                if (destinations.Count == 1)
                {
                    Commit(destinations[0]);
                    return;
                }

                BeginPromotion(destinations);
                return;
            }

            if (IsFriendly(square))
            {
                Select(square);
                RefreshPresentation();
            }
        }

        void HandleSetupClick(Square square)
        {
            if (_state == null)
                return;
            Piece piece = _state.Board.GetPiece(square);
            Side picker = _session.Hotseat ? (_whitePicks.Count >= _session.Rules.Settings.EmpoweredCount ? Side.Black : Side.White) : _session.PlayerSide;
            if (piece == null || piece.Side != picker)
                return;

            List<Guid> picks = picker == Side.White ? _whitePicks : _blackPicks;
            if (picks.Contains(piece.Id))
                picks.Remove(piece.Id);
            else if (picks.Count < _session.Rules.Settings.EmpoweredCount)
                picks.Add(piece.Id);

            if (_whitePicks.Count >= _session.Rules.Settings.EmpoweredCount
                && _blackPicks.Count >= _session.Rules.Settings.EmpoweredCount)
            {
                FinishSetup(timeout: false);
                return;
            }

            RefreshPresentation();
        }

        void FinishSetup(bool timeout)
        {
            int n = _session.Rules.Settings.EmpoweredCount;
            if (timeout)
            {
                if (_whitePicks.Count < n)
                    SimpleAi.AutopickEmpowered(_state, Side.White, n, _whitePicks);
                if (_blackPicks.Count < n)
                    SimpleAi.AutopickEmpowered(_state, Side.Black, n, _blackPicks);
            }

            var all = new List<Guid>();
            all.AddRange(_whitePicks);
            all.AddRange(_blackPicks);
            _state = _state.ConfirmEmpowered(all);
            _inSetup = false;
            _clock?.Start();
            RefreshPresentation();
        }

        private void OnPromotionChosen(PieceType pieceType)
        {
            if (_pendingPromotions == null || _state == null)
                return;

            Move? chosen = null;
            for (int i = 0; i < _pendingPromotions.Count; i++)
            {
                Move candidate = _pendingPromotions[i];
                if (candidate.PromotionType == pieceType)
                {
                    chosen = candidate;
                    break;
                }
            }

            if (!chosen.HasValue)
                return;
            Commit(chosen.Value);
        }

        private bool CanAcceptBoardInput()
        {
            if (!enabled || _state == null || _state.Status != GameStatus.InProgress || _pendingPromotions != null)
                return false;
            if (_state.DraftPending || _paused || _inSetup)
                return false;
            if (_session != null && _session.IsAi && _state.SideToMove != _session.PlayerSide)
                return false;
            return true;
        }

        private bool IsFriendly(Square square)
        {
            Piece piece = _state.Board.GetPiece(square);
            return piece != null && piece.Side == _state.SideToMove;
        }

        private void Select(Square square)
        {
            _selected = square;
            _movesFromSelection = _state.LegalMovesFrom(square);
        }

        private void ClearSelection()
        {
            _selected = null;
            _movesFromSelection = Array.Empty<Move>();
        }

        private List<Move> MovesTo(Square destination)
        {
            List<Move> matches = new List<Move>();
            for (int i = 0; i < _movesFromSelection.Count; i++)
            {
                Move move = _movesFromSelection[i];
                if (move.To.Equals(destination))
                    matches.Add(move);
            }

            return matches;
        }

        private void BeginPromotion(List<Move> promotions)
        {
            _pendingPromotions = promotions;
            promotionPicker.Show(_state.SideToMove);
        }

        private void Commit(Move move)
        {
            Side moved = _state.SideToMove;
            _state = _state.Apply(move);
            _pendingPromotions = null;
            ClearSelection();
            promotionPicker.Hide();
            if (_state.SideToMove != moved)
                _clock?.AddIncrement(moved);
            RefreshPresentation();
        }

        void PlayAi()
        {
            if (_state.TurnOpen)
            {
                _state = _state.EndTurn();
                RefreshPresentation();
                return;
            }

            Move? move = SimpleAi.Choose(_state, _session.Rules.Settings.AiStrength, _state.SideToMove);
            if (move == null)
                return;
            Commit(move.Value);
        }

        void ResolveAiDraft()
        {
            DraftOffer? offer = _state.Runtime.PendingDraft;
            if (offer == null)
                return;
            _state = _state.ApplyDraft(offer.Value.First, null, null);
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            if (boardView == null || _state == null)
                return;

            Side viewer = _session != null && _session.Hotseat ? _state.SideToMove : (_session?.PlayerSide ?? Side.White);
            VisionMap vision = VisionMap.Compute(_state, viewer);
            boardView.ViewerSide = viewer;
            boardView.Bind(_state, vision, viewer);
            if (_selected.HasValue)
                boardView.SetSelection(_selected);
            else
                boardView.ClearSelection();

            if (_state.History.Count > 0)
            {
                Move last = _state.History[_state.History.Count - 1];
                boardView.SetLastMove(last.From, last.To);
            }
            else
            {
                boardView.ClearLastMove();
            }

            if (hud == null)
                return;

            hud.Bind(_state, _state.History);
            hud.SetClock(_clock);
            hud.SetEndTurnVisible(_state.CanEndTurn());
            if (_inSetup)
            {
                hud.SetStatusLine($"Setup {Mathf.CeilToInt(_setupRemaining)}s  White {_whitePicks.Count}/{_session.Rules.Settings.EmpoweredCount}  Black {_blackPicks.Count}/{_session.Rules.Settings.EmpoweredCount}");
            }
            else if (_state.DraftPending)
            {
                hud.SetStatusLine(_session != null && _session.IsAi && _state.SideToMove != _session.PlayerSide
                    ? string.Empty
                    : "Draft: pick a power");
                hud.ShowDraft(_state, power =>
                {
                    _state = _state.ApplyDraft(power, null, null);
                    RefreshPresentation();
                });
            }
            else
            {
                hud.HideDraft();
            }
        }
    }
}
