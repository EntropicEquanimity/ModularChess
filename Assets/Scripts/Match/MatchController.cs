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
        bool _historyWritten;
        bool _draftTiming;
        float _draftRemaining;
        float _aiWait = -1f;
        Square? _hovered;
        Guid? _pinnedPieceId;
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

        public bool IsPaused => _paused;
        public bool InSetup => _inSetup;
        public bool DraftPending => _state != null && _state.DraftPending;
        public MatchSession Session => _session;

        private void Update()
        {
            if (_session == null || _state == null)
                return;

            if (_paused)
            {
                hud?.SetClock(_clock);
                return;
            }

            if (_inSetup)
            {
                _setupRemaining -= Time.deltaTime;
                hud?.SetStatusLine(SetupStatusLine());
                if (_setupRemaining <= 0f)
                    FinishSetup(timeout: true);
                return;
            }

            if (_state.DraftPending)
            {
                TickDraft();
                return;
            }

            _draftTiming = false;

            if (_clock != null && _state.Status == GameStatus.InProgress && !_state.DraftPending)
            {
                Side? flagged = _clock.Tick(Time.deltaTime, _state.SideToMove);
                hud?.SetClock(_clock);
                if (flagged != null)
                {
                    _state = _state.WithTerminal(GameStatus.Timeout);
                    RefreshPresentation();
                    return;
                }
            }
            else
            {
                hud?.SetClock(_clock);
            }

            if (_session.IsAi
                && _state.Status == GameStatus.InProgress
                && !_state.DraftPending
                && _state.SideToMove != _session.PlayerSide
                && _pendingPromotions == null)
            {
                if (boardView != null && boardView.PiecesBusy)
                    return;
                if (_aiWait < 0f)
                    _aiWait = UnityEngine.Random.Range(1f, 3f);
                _aiWait -= Time.deltaTime;
                if (_aiWait > 0f)
                    return;
                _aiWait = -1f;
                PlayAi();
            }
            else
            {
                _aiWait = -1f;
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
            _hovered = null;
            _pinnedPieceId = null;
            _paused = false;
            _clock = new MatchClock(session.Rules.Settings.Time);
            _historyWritten = false;
            _draftTiming = false;
            _draftRemaining = 0f;
            _aiWait = -1f;
            boardView?.CompleteMotion();
            boardView?.SetMotionPaused(false);
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
                WriteHistory();
            }

            _session = null;
            _inSetup = false;
            _clock?.Stop();
            _aiWait = -1f;
            _hovered = null;
            _pinnedPieceId = null;
            boardView?.ClearTargeting();
            boardView?.SetPendingEmpowered(null);
            boardView?.CompleteMotion();
            hud?.HidePieceDetails();
            LeftMatch?.Invoke();
        }

        public void Resign()
        {
            if (_state == null || _inSetup || _state.Status != GameStatus.InProgress)
                return;
            _state = _state.WithTerminal(GameStatus.Resign);
            RefreshPresentation();
        }

        public bool TryPauseFromEscape()
        {
            if (_session == null || !_session.IsAi || _inSetup || _state == null || _state.DraftPending)
                return false;
            if (_state.Status != GameStatus.InProgress)
                return false;
            TogglePause();
            return true;
        }

        public void DebugWin()
        {
            if (!CanDebugEnd())
                return;
            Side winner = _session.Hotseat ? _state.SideToMove : _session.PlayerSide;
            if (_state.SideToMove == winner)
                _state = _state.WithSideToMove(winner.Opponent());
            _state = _state.WithTerminal(GameStatus.Checkmate);
            RefreshPresentation();
        }

        public void DebugLose()
        {
            if (!CanDebugEnd())
                return;
            Side loser = _session.Hotseat ? _state.SideToMove : _session.PlayerSide;
            if (_state.SideToMove != loser)
                _state = _state.WithSideToMove(loser);
            _state = _state.WithTerminal(GameStatus.Resign);
            RefreshPresentation();
        }

        public void DebugResetTimer()
        {
            _clock?.ResetToStart();
            hud?.SetClock(_clock);
        }

        bool CanDebugEnd()
        {
            return _state != null && !_inSetup && _state.Status == GameStatus.InProgress;
        }

        void WriteHistory()
        {
            if (_historyWritten || _session == null || _state == null)
                return;
            if (_state.Status == GameStatus.InProgress || _state.Status == GameStatus.Aborted)
                return;
            _historyWritten = true;
            int seconds = _clock != null ? Mathf.FloorToInt(_clock.ElapsedSeconds) : 0;
            MatchHistoryStore.Record(_session, _state, seconds);
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
            boardView?.SetMotionPaused(_paused);
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
            boardView.SquareHovered += OnSquareHovered;
            if (promotionPicker != null)
                promotionPicker.PromotionChosen += OnPromotionChosen;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!_subscribed)
                return;
            if (boardView != null)
            {
                boardView.SquareClicked -= OnSquareClicked;
                boardView.SquareHovered -= OnSquareHovered;
            }
            if (promotionPicker != null)
                promotionPicker.PromotionChosen -= OnPromotionChosen;
            _subscribed = false;
        }

        private void OnSquareClicked(Square square)
        {
            PinFromClick(square);
            if (_inSetup)
            {
                HandleSetupClick(square);
                return;
            }

            if (!CanAcceptBoardInput())
            {
                RefreshPieceDetails();
                return;
            }

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
            Side picker = SetupPicker();
            if (piece == null || piece.Side != picker)
                return;

            List<Guid> picks = picker == Side.White ? _whitePicks : _blackPicks;
            if (picks.Contains(piece.Id))
                picks.Remove(piece.Id);
            else if (picks.Count < _session.Rules.Settings.EmpoweredCount)
                picks.Add(piece.Id);

            RefreshPresentation();
        }

        public void ConfirmSetup()
        {
            if (!_inSetup || _session == null)
                return;
            int n = _session.Rules.Settings.EmpoweredCount;
            if (_whitePicks.Count < n || _blackPicks.Count < n)
                return;
            FinishSetup(timeout: false);
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
            if (boardView != null && boardView.PiecesBusy)
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
            if (promotionPicker != null)
                promotionPicker.Show(_state.SideToMove);
        }

        private void Commit(Move move)
        {
            Side moved = _state.SideToMove;
            _state = _state.Apply(move);
            _pendingPromotions = null;
            ClearSelection();
            promotionPicker?.Hide();
            if (_state.SideToMove != moved)
                _clock?.AddIncrement(moved);
            RefreshPresentation();
        }

        void PlayAi()
        {
            if (_state.TurnOpen)
            {
                Side ended = _state.SideToMove;
                _state = _state.EndTurn();
                _clock?.AddIncrement(ended);
                RefreshPresentation();
                return;
            }

            Move? move = SimpleAi.Choose(_state, _session.Rules.Settings.AiStrength, _state.SideToMove);
            if (move == null)
                return;
            Commit(move.Value);
        }

        void TickDraft()
        {
            if (_session != null && _session.IsAi && _state.SideToMove != _session.PlayerSide)
            {
                ResolveAiDraft();
                return;
            }

            if (!_draftTiming)
            {
                _draftTiming = true;
                _draftRemaining = 60f;
                RefreshPresentation();
            }

            _draftRemaining -= Time.deltaTime;
            int left = Mathf.Max(0, Mathf.CeilToInt(_draftRemaining));
            hud?.SetStatusLine($"Draft: pick a power  {left}s");
            hud?.SetClock(_clock);
            if (_draftRemaining <= 0f)
                TimeoutDraft();
        }

        void TimeoutDraft()
        {
            ApplyRandomDraft();
        }

        void ResolveAiDraft()
        {
            ApplyRandomDraft();
        }

        bool TryResolveAiDraft()
        {
            if (_state == null || !_state.DraftPending)
                return false;
            if (_session == null || !_session.IsAi || _state.SideToMove == _session.PlayerSide)
                return false;

            ApplyRandomDraft();
            return true;
        }

        void ApplyRandomDraft()
        {
            DraftOffer? offer = _state.Runtime.PendingDraft;
            if (offer == null)
                return;
            MartyrPower[] options = { offer.Value.First, offer.Value.Second, offer.Value.Third };
            MartyrPower power = options[UnityEngine.Random.Range(0, options.Length)];
            _draftTiming = false;
            _state = _state.ApplyDraft(power, null, null);
            RefreshPresentation();
        }

        private void RefreshPresentation()
        {
            if (boardView == null || _state == null)
                return;
            if (TryResolveAiDraft())
                return;

            Side viewer = _session != null && _session.Hotseat ? _state.SideToMove : (_session?.PlayerSide ?? Side.White);
            VisionMap vision = VisionMap.Compute(_state, viewer);
            boardView.ViewerSide = viewer;
            if (_inSetup)
            {
                boardView.SetPendingEmpowered(PendingEmpoweredIds());
            }
            else
            {
                boardView.SetPendingEmpowered(null);
                boardView.ClearTargeting();
            }

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

            if (_inSetup)
                ApplySetupTargeting();

            if (hud == null)
                return;

            hud.Bind(_state, _state.History);
            hud.SetClock(_clock);
            hud.SetEndTurnVisible(!_inSetup && _state.CanEndTurn());
            hud.SetPauseVisible(!_inSetup && _session != null && _session.IsAi && _state.Status == GameStatus.InProgress);
            hud.SetResignVisible(!_inSetup && _state.Status == GameStatus.InProgress);
            int n = _session != null ? _session.Rules.Settings.EmpoweredCount : 0;
            hud.SetSetupConfirmVisible(_inSetup && _whitePicks.Count >= n && _blackPicks.Count >= n);
            if (_session != null && _session.Rules.Has(ModeId.Martyr))
            {
                hud.SetLostMaterial(
                    _state.Runtime.LostMaterial(Side.White),
                    _state.Runtime.LostMaterial(Side.Black),
                    _session.Rules.Settings.MartyrThreshold);
            }
            else
            {
                hud.SetLostMaterial(null, null, 0);
            }

            if (_state.Status != GameStatus.InProgress)
                WriteHistory();
            if (_inSetup)
            {
                hud.SetStatusLine(SetupStatusLine());
            }
            else if (_state.DraftPending)
            {
                int left = _draftTiming ? Mathf.Max(0, Mathf.CeilToInt(_draftRemaining)) : 60;
                hud.SetStatusLine($"Draft: pick a power  {left}s");
                hud.ShowDraft(_state, power =>
                {
                    _draftTiming = false;
                    _state = _state.ApplyDraft(power, null, null);
                    RefreshPresentation();
                });
            }
            else
            {
                hud.HideDraft();
                if (!_paused)
                    hud.SetStatusLine(string.Empty);
            }

            RefreshPieceDetails();
        }

        void OnSquareHovered(Square? square)
        {
            _hovered = square;
            RefreshPieceDetails();
        }

        void PinFromClick(Square square)
        {
            Piece piece = InspectablePiece(square);
            _pinnedPieceId = piece != null ? piece.Id : (Guid?)null;
        }

        void ApplySetupTargeting()
        {
            if (boardView == null)
                return;
            if (!_inSetup)
            {
                boardView.ClearTargeting();
                return;
            }

            Side picker = SetupPicker();
            var valid = new List<Square>();
            for (int file = 0; file < BoardLayout.FileCount; file++)
            {
                for (int rank = 0; rank < BoardLayout.RankCount; rank++)
                {
                    var square = new Square(file, rank);
                    Piece piece = _state.Board.GetPiece(square);
                    if (piece != null && piece.Side == picker)
                        valid.Add(square);
                }
            }

            boardView.SetTargeting(valid);
        }

        Side SetupPicker()
        {
            if (_session != null && _session.Hotseat)
                return _whitePicks.Count >= _session.Rules.Settings.EmpoweredCount ? Side.Black : Side.White;
            return _session?.PlayerSide ?? Side.White;
        }

        List<Guid> PendingEmpoweredIds()
        {
            List<Guid> picks = SetupPicker() == Side.White ? _whitePicks : _blackPicks;
            return new List<Guid>(picks);
        }

        string SetupStatusLine()
        {
            int n = _session.Rules.Settings.EmpoweredCount;
            Side picker = SetupPicker();
            int count = picker == Side.White ? _whitePicks.Count : _blackPicks.Count;
            return $"Setup {Mathf.CeilToInt(_setupRemaining)}s  {count}/{n}";
        }

        void RefreshPieceDetails()
        {
            if (hud == null || _state == null)
                return;

            Piece piece = null;
            if (_hovered.HasValue)
                piece = InspectablePiece(_hovered.Value);
            if (piece == null && _pinnedPieceId.HasValue)
            {
                Square? square = _state.Board.FindSquare(_pinnedPieceId.Value);
                if (square.HasValue)
                    piece = InspectablePiece(square.Value);
            }

            if (piece == null)
            {
                hud.HidePieceDetails();
                return;
            }

            hud.ShowPieceDetails(piece, _state, _inSetup ? PendingEmpoweredIds() : null);
        }

        Piece InspectablePiece(Square square)
        {
            if (_state == null || !square.IsOnBoard)
                return null;
            Piece piece = _state.Board.GetPiece(square);
            if (piece == null)
                return null;

            Side viewer = _session != null && _session.Hotseat ? _state.SideToMove : (_session?.PlayerSide ?? Side.White);
            if (piece.Side == viewer)
                return piece;

            VisionMap vision = VisionMap.Compute(_state, viewer);
            return vision[square] == SquareSight.Identified ? piece : null;
        }
    }
}
