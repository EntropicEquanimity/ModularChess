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
        Side? _selectionSide;
        IReadOnlyList<Move> _movesFromSelection = Array.Empty<Move>();
        List<Move> _pendingPromotions;
        bool _subscribed;
        bool _inSetup;
        bool _paused;
        float _setupRemaining;
        int _setupConfirms;
        readonly List<Guid> _whitePicks = new List<Guid>();
        readonly List<Guid> _blackPicks = new List<Guid>();
        readonly List<MatchHistoryEvent> _historyEvents = new List<MatchHistoryEvent>();
        MatchClock _clock;
        bool _historyWritten;
        int _campaignPlayerTurns;
        int _campaignPiecesLost;
        bool _draftTiming;
        float _draftRemaining;
        float _aiWait = -1f;
        float _autoplayRematchWait = -1f;
        bool _autoplayRematchSent;
        const float AiThinkSeconds = 1f;
        const float AutoplayRematchSeconds = 10f;
        MartyrPower? _draftTargeting;
        readonly List<Square> _draftTargets = new List<Square>();
        readonly List<Square> _reinforcementPicks = new List<Square>();
        Guid? _vanishingPieceId;
        Square? _hovered;
        Guid? _pinnedPieceId;
        bool _replaying;
        MatchHistoryRecord _replayRecord;
        int _replayIndex;
        bool _replayAuto;
        float _replayWait;
        float _replaySpeed = 1f;
        bool _replayFromHistory;
        const float ReplayTurnWait = 2f;
        public event Action LeftMatch;
        public event Action RematchRequested;
        public event Action ReplayRequested;
        public event Action ReplayLeftToHistory;

        public GameState State => _state;
        public bool IsPlaying => _session != null && _state != null;
        public bool IsReplaying => _replaying;
        public bool ReplayFromHistory => _replayFromHistory;
        public MatchHistoryRecord ReplayRecord => _replayRecord;

        public void Configure(BoardView view, PromotionPicker picker, MatchHud matchHud)
        {
            Unsubscribe();
            boardView = view;
            promotionPicker = picker;
            hud = matchHud;
            Subscribe();
            HookLanguage();
        }

        public void Initialize()
        {
            Subscribe();
            HookLanguage();
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
            HookLanguage();
        }

        private void OnDestroy()
        {
            Unsubscribe();
            Loc.Changed -= OnLanguageChanged;
        }

        public bool IsPaused => _paused;
        public bool InSetup => _inSetup;
        public bool DraftPending => _state != null && _state.DraftPending;
        public MatchSession Session => _session;

        private void Update()
        {
            if (_session == null || _state == null)
                return;
            if (_replaying)
            {
                TickReplay();
                return;
            }
            if (TickAutoplayRematch())
                return;
            if (_paused)
            {
                hud?.SetClock(_clock, ClockSide());
                return;
            }
            if (_inSetup)
            {
                if (Autoplay.Active)
                    TickAutoplaySetup();
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
                if (boardView == null || !boardView.HidingMatchChrome)
                {
                    Side? flagged = _clock.Tick(Time.deltaTime, _state.SideToMove);
                    hud?.SetClock(_clock, ClockSide());
                    if (flagged != null)
                    {
                        _state = _state.WithTerminal(GameStatus.Timeout);
                        RefreshPresentation();
                        return;
                    }
                }
            }
            else
            {
                hud?.SetClock(_clock, ClockSide());
            }
            if (_pendingPromotions != null && Autoplay.Active && IsPlayerSideToMove())
            {
                OnPromotionChosen(PieceType.Queen);
                return;
            }
            if (ShouldRunSideAi(out AiStrength strength))
            {
                if (boardView != null && boardView.PiecesBusy)
                    return;
                if (_aiWait < 0f)
                    _aiWait = AiThinkSeconds;
                _aiWait -= Time.deltaTime;
                if (_aiWait > 0f)
                    return;
                _aiWait = -1f;
                PlaySideAi(strength);
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
            if (session.IsCampaign)
                _state = GameState.FromFen(session.CampaignLevel.Fen, session.Rules);
            else
                _state = GameState.StartingPosition(session.Rules);
            _pendingPromotions = null;
            ClearSelection();
            _whitePicks.Clear();
            _blackPicks.Clear();
            _setupConfirms = 0;
            _hovered = null;
            _pinnedPieceId = null;
            _paused = false;
            _replaying = false;
            _replayRecord = null;
            _replayIndex = 0;
            _replayAuto = false;
            _replayWait = 0f;
            _replaySpeed = 1f;
            _replayFromHistory = false;
            _historyEvents.Clear();
            _clock = new MatchClock(session.Rules.Settings.Time);
            _historyWritten = false;
            _campaignPlayerTurns = 0;
            _campaignPiecesLost = 0;
            _draftTiming = false;
            _draftRemaining = 0f;
            _draftTargeting = null;
            _draftTargets.Clear();
            _reinforcementPicks.Clear();
            _vanishingPieceId = null;
            _aiWait = -1f;
            _autoplayRematchWait = -1f;
            _autoplayRematchSent = false;
            boardView?.CompleteMotion();
            boardView?.SetMotionPaused(false);
            if (boardView != null)
                boardView.ReviewVision = false;
            BoardCamera.ClearTrauma();
            hud?.SetDeferGameOver(false);
            hud?.SetMatchChromeVisible(true);
            hud?.SetReplayMode(false);
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
                    SimpleAi.AutopickEmpowered(_state, aiSide, session.Rules.Settings.EmpowerBudget, aiPicks);
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
            if (_replaying)
            {
                EndReplay();
                return;
            }
            if (_state != null && _state.Status == GameStatus.InProgress)
            {
                _state = _state.WithTerminal(_inSetup ? GameStatus.Aborted : GameStatus.Resign);
                WriteHistory();
            }

            _session = null;
            _inSetup = false;
            _clock?.Stop();
            _aiWait = -1f;
            _autoplayRematchWait = -1f;
            _autoplayRematchSent = false;
            Autoplay.ClearOnLeave();
            _hovered = null;
            _pinnedPieceId = null;
            boardView?.ClearTargeting();
            boardView?.SetPendingEmpowered(null);
            boardView?.CompleteMotion();
            if (boardView != null)
                boardView.ReviewVision = false;
            hud?.HidePieceDetails();
            hud?.SetSetupConfirm(false, false, false);
            hud?.SetReplayMode(false);
            LeftMatch?.Invoke();
        }

        public void RequestRematch()
        {
            if (_replaying || _session == null)
                return;
            RematchRequested?.Invoke();
        }

        public void RequestReplay()
        {
            if (_replaying)
                return;
            MatchHistoryRecord record = MatchHistoryStore.Latest();
            if (record == null || !record.Replayable)
                return;
            ReplayRequested?.Invoke();
            LaunchReplay(record, fromHistory: false);
        }

        public void LaunchReplay(MatchHistoryRecord record, bool fromHistory)
        {
            if (record == null || !record.Replayable)
                return;
            _replayRecord = record;
            _replayFromHistory = fromHistory;
            _replaying = true;
            _replayIndex = 0;
            _replayAuto = false;
            _replayWait = 0f;
            _replaySpeed = 1f;
            _session = MatchHistoryStore.SessionFrom(record);
            _state = MatchHistoryPlayback.StartingState(record);
            _pendingPromotions = null;
            ClearSelection();
            _whitePicks.Clear();
            _blackPicks.Clear();
            _setupConfirms = 0;
            _hovered = null;
            _pinnedPieceId = null;
            _paused = false;
            _inSetup = false;
            _historyWritten = true;
            _historyEvents.Clear();
            _draftTiming = false;
            _draftTargeting = null;
            _draftTargets.Clear();
            _reinforcementPicks.Clear();
            _vanishingPieceId = null;
            _aiWait = -1f;
            _clock = null;
            Subscribe();
            boardView?.CompleteMotion();
            boardView?.SetMotionPaused(false);
            if (boardView != null)
                boardView.ReviewVision = true;
            BoardCamera.ClearTrauma();
            hud?.SetDeferGameOver(false);
            hud?.SetMatchChromeVisible(true);
            hud?.SetReplayMode(true);
            hud?.BindActions(this);
            hud?.BindReplay(this);
            promotionPicker?.Hide();
            RefreshPresentation();
        }

        public void ReplayNext()
        {
            if (!_replaying)
                return;
            StopReplayAuto();
            StepReplay(1);
        }

        public void ReplayLast()
        {
            if (!_replaying)
                return;
            StopReplayAuto();
            StepReplay(-1);
        }

        public void ReplayRestart()
        {
            if (!_replaying || _replayRecord == null)
                return;
            StopReplayAuto();
            _replayIndex = 0;
            _state = MatchHistoryPlayback.StartingState(_replayRecord);
            ClearSelection();
            RefreshPresentation();
        }

        public void ToggleReplayAuto()
        {
            if (!_replaying)
                return;
            _replayAuto = !_replayAuto;
            _replayWait = 0f;
            hud?.SetReplayAuto(_replayAuto, _replaySpeed);
        }

        public void SetReplaySpeed(float speed)
        {
            if (!_replaying) return;
            _replaySpeed = speed;
            hud?.SetReplayAuto(_replayAuto, _replaySpeed);
        }

        void EndReplay()
        {
            bool toHistory = _replayFromHistory;
            _replaying = false;
            _replayRecord = null;
            _replayIndex = 0;
            _replayAuto = false;
            _session = null;
            _state = null;
            _clock = null;
            if (boardView != null)
                boardView.ReviewVision = false;
            hud?.SetReplayMode(false);
            hud?.HidePieceDetails();
            if (toHistory)
                ReplayLeftToHistory?.Invoke();
            else
                LeftMatch?.Invoke();
        }

        void TickReplay()
        {
            hud?.SetClock(null, ClockSide());
            if (!_replayAuto)
                return;
            if (boardView != null && boardView.PiecesBusy)
                return;
            if (_replayWait > 0f)
            {
                _replayWait -= Time.deltaTime;
                return;
            }
            if (!StepReplay(1))
            {
                _replayAuto = false;
                hud?.SetReplayAuto(false, _replaySpeed);
            }
        }

        bool StepReplay(int delta)
        {
            if (_replayRecord?.events == null)
                return false;
            int next = _replayIndex + delta;
            if (next < 0 || next > _replayRecord.events.Length)
                return false;
            if (delta == 0)
                return false;
            GameState before = _state;
            if (delta > 0)
            {
                for (int i = 0; i < delta; i++)
                {
                    if (_replayIndex >= _replayRecord.events.Length)
                        return false;
                    MatchHistoryEvent e = _replayRecord.events[_replayIndex];
                    if (!MatchHistoryPlayback.TryApply(ref _state, e))
                        return false;
                    _replayIndex++;
                }
                if (_replayAuto && MatchHistoryPlayback.EndsTurn(before, _state))
                    _replayWait = ReplayTurnWait / Mathf.Max(0.5f, _replaySpeed);
            }
            else
            {
                _replayIndex = next;
                _state = MatchHistoryPlayback.StateAt(_replayRecord, _replayIndex);
            }
            ClearSelection();
            RefreshPresentation();
            return _replayIndex < (_replayRecord.events?.Length ?? 0);
        }

        void StopReplayAuto()
        {
            if (!_replayAuto)
                return;
            _replayAuto = false;
            _replayWait = 0f;
            hud?.SetReplayAuto(false, _replaySpeed);
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
            hud?.SetClock(_clock, ClockSide());
        }

        bool CanDebugEnd()
        {
            return _state != null && !_inSetup && _state.Status == GameStatus.InProgress;
        }

        void WriteHistory()
        {
            if (_historyWritten || _session == null || _state == null || _replaying)
                return;
            if (_state.Status == GameStatus.InProgress || _state.Status == GameStatus.Aborted)
                return;
            _historyWritten = true;
            if (_session.Activity == Activity.Campaign)
            {
                AwardCampaignStars();
                return;
            }
            int seconds = _clock != null ? Mathf.FloorToInt(_clock.ElapsedSeconds) : 0;
            MatchHistoryStore.Record(_session, _state, seconds, _historyEvents);
            bool checkmate = _state.Status == GameStatus.Checkmate;
            MeritWallet.GrantVersusFinish(checkmate);
        }
        void AwardCampaignStars()
        {
            if (!_session.IsCampaign) return;
            bool won = PlayerWonCampaign();
            CampaignStarFlags earned = CampaignStarEval.Evaluate(
                _session.CampaignLevel,
                won,
                _campaignPlayerTurns,
                _campaignPiecesLost);
            CampaignProgress.Award(_session.CampaignLevel.Index, earned);
        }
        bool PlayerWonCampaign()
        {
            if (_state.Status != GameStatus.Checkmate) return false;
            Side winner = _state.SideToMove.Opponent();
            return winner == _session.PlayerSide;
        }
        void NoteCampaignProgress(Side moved, Piece victim, bool bounce)
        {
            if (_session == null || !_session.IsCampaign) return;
            if (victim != null && !bounce && victim.Side == _session.PlayerSide)
                _campaignPiecesLost++;
            NoteCampaignTurnEnd(moved);
        }
        void NoteCampaignTurnEnd(Side ended)
        {
            if (_session == null || !_session.IsCampaign) return;
            if (_state.SideToMove != ended && ended == _session.PlayerSide)
                _campaignPlayerTurns++;
        }

        public void TogglePause()
        {
            if (_session == null || !_session.IsAi || _inSetup || _state == null || _state.DraftPending)
                return;
            SetPaused(!_paused);
            hud?.SetStatusLine(_paused ? Loc.Get("match.paused") : string.Empty);
        }
        public void SetPaused(bool paused)
        {
            if (_session == null || _state == null)
                return;
            if (_paused == paused)
                return;
            _paused = paused;
            if (_paused)
                _clock?.Stop();
            else
                _clock?.Start();
            boardView?.SetMotionPaused(_paused);
        }
        public void OpenSettings()
        {
            OptionsOverlay.Ensure()?.OpenFromMatch();
        }

        public void RequestEndTurn()
        {
            if (_state == null || !_state.CanEndTurn())
                return;
            Side ended = _state.SideToMove;
            _state = _state.EndTurn();
            NoteCampaignTurnEnd(ended);
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
            boardView.MatchChromeHidden += OnMatchChromeHidden;
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
                boardView.MatchChromeHidden -= OnMatchChromeHidden;
            }
            if (promotionPicker != null)
                promotionPicker.PromotionChosen -= OnPromotionChosen;
            _subscribed = false;
        }

        void HookLanguage()
        {
            Loc.Changed -= OnLanguageChanged;
            Loc.Changed += OnLanguageChanged;
        }

        void OnLanguageChanged()
        {
            if (_state != null)
                RefreshPresentation();
            if (_paused)
                hud?.SetStatusLine(Loc.Get("match.paused"));
        }

        private void OnSquareClicked(Square square)
        {
            if (OptionsOverlay.IsOpen || (hud != null && hud.BlocksBoardInput))
                return;
            if (BoardPointerInput.IsScreenBlockedByUi())
                return;
            PinFromClick(square);
            if (_replaying)
            {
                PlayBoardTapSfx(square);
                RefreshPieceDetails();
                return;
            }
            if (_draftTargeting != null)
            {
                HandleDraftTarget(square);
                return;
            }
            if (_inSetup)
            {
                HandleSetupClick(square);
                return;
            }
            if (!CanAcceptBoardInput())
            {
                PlayBoardTapSfx(square);
                RefreshPieceDetails();
                return;
            }
            if (_selected.HasValue)
            {
                _movesFromSelection = _state.LegalMovesFrom(_selected.Value);
                if (_selected.Value.Equals(square))
                {
                    ClearSelection();
                    GameAudio.PlayTap();
                    RefreshPresentation();
                    return;
                }
                List<Move> destinations = MovesTo(square);
                if (destinations.Count == 1)
                {
                    Commit(destinations[0]);
                    return;
                }
                if (destinations.Count > 1)
                {
                    BeginPromotion(destinations);
                    return;
                }
                if (IsFriendly(square))
                {
                    Select(square);
                    RefreshPresentation();
                    return;
                }
                Piece inspect = InspectablePiece(square);
                if (inspect != null)
                {
                    GameAudio.PlayTap();
                    ClearSelection();
                    RefreshPresentation();
                    return;
                }
                if (_state.Board.GetPiece(square) == null || !IsVisibleOccupant(square))
                {
                    GameAudio.PlayMove();
                    ClearSelection();
                    RefreshPresentation();
                    return;
                }
                GameAudio.PlayIllegal();
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
            PlayBoardTapSfx(square);
        }

        void PlayBoardTapSfx(Square square)
        {
            if (InspectablePiece(square) != null)
            {
                GameAudio.PlayTap();
                return;
            }
            GameAudio.PlayMove();
        }

        bool IsVisibleOccupant(Square square)
        {
            Piece piece = _state?.Board.GetPiece(square);
            if (piece == null)
                return false;
            return InspectablePiece(square) != null;
        }

        void HandleSetupClick(Square square)
        {
            if (_state == null || Autoplay.Active)
                return;
            Piece piece = _state.Board.GetPiece(square);
            Side picker = SetupPicker();
            if (piece == null || piece.Side != picker)
                return;

            List<Guid> picks = picker == Side.White ? _whitePicks : _blackPicks;
            if (picks.Contains(piece.Id))
                picks.Remove(piece.Id);
            else
            {
                int remaining = SetupRemaining(picker);
                if (EmpoweredPowers.Cost(piece.Type) > remaining)
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                picks.Add(piece.Id);
            }

            GameAudio.PlayTap();
            RefreshPresentation();
        }

        public void ConfirmSetup()
        {
            if (!_inSetup || _session == null)
                return;
            int budget = _session.Rules.Settings.EmpowerBudget;
            if (SetupSpent(Side.White) != budget || SetupSpent(Side.Black) != budget)
                return;
            if (!_session.IsAi && _setupConfirms == 0)
            {
                _setupConfirms = 1;
                RefreshPresentation();
                return;
            }
            FinishSetup(timeout: false);
        }

        void FinishSetup(bool timeout)
        {
            int budget = _session.Rules.Settings.EmpowerBudget;
            if (timeout)
            {
                if (SetupSpent(Side.White) != budget)
                    SimpleAi.AutopickEmpowered(_state, Side.White, budget, _whitePicks);
                if (SetupSpent(Side.Black) != budget)
                    SimpleAi.AutopickEmpowered(_state, Side.Black, budget, _blackPicks);
            }
            var all = new List<Guid>();
            all.AddRange(_whitePicks);
            all.AddRange(_blackPicks);
            AppendEmpoweredEvent(all);
            _state = _state.ConfirmEmpowered(all);
            _inSetup = false;
            _setupConfirms = 0;
            _clock?.Start();
            RefreshPresentation();
        }

        void AppendEmpoweredEvent(IReadOnlyList<Guid> ids)
        {
            var squares = new List<Square>(ids.Count);
            for (int i = 0; i < ids.Count; i++)
            {
                Square? square = _state.Board.FindSquare(ids[i]);
                if (square.HasValue)
                    squares.Add(square.Value);
            }
            _historyEvents.Add(MatchHistoryStore.EmpoweredEvent(squares));
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
            if (OptionsOverlay.IsOpen || (hud != null && hud.BlocksBoardInput))
                return false;
            if (_session != null && _session.IsAi && _state.SideToMove != _session.PlayerSide)
                return false;
            if (Autoplay.Active && IsPlayerSideToMove())
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
            _selectionSide = _state.SideToMove;
            _movesFromSelection = _state.LegalMovesFrom(square);
            GameAudio.PlayTap();
        }

        private void ClearSelection()
        {
            _selected = null;
            _selectionSide = null;
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
            Piece victim = FindVictim(_state, move);
            _historyEvents.Add(MatchHistoryStore.MoveEvent(move.From, move.To, move.Kind, move.PromotionType));
            _state = _state.Apply(move);
            _pendingPromotions = null;
            ClearSelection();
            promotionPicker?.Hide();
            bool bounce = victim != null && _state.Runtime.ExtraLifeSpent(victim.Id);
            NoteCampaignProgress(moved, victim, bounce);
            if (_state.SideToMove != moved)
                _clock?.AddIncrement(moved);
            PlayMoveSfx(move, moved, bounce);
            if (bounce)
                BoardCamera.AddTrauma(CaptureTrauma.Deflect);
            else if (victim != null)
                BoardCamera.AddTrauma(CaptureTrauma.For(victim.Type));
            bool mate = _state.Status == GameStatus.Checkmate;
            if (_state.IsInCheck || mate)
                GameAudio.PlayCheck();
            if (mate)
                hud?.SetDeferGameOver(true);
            RefreshPresentation();
            if (bounce)
                boardView?.PlayDeflect(move.From, move.To);
            else if (mate)
                PlayMatePresentation(moved);
            else if (_state.IsInCheck)
                PlayCheckPresentation();
        }

        void PlayMoveSfx(Move move, Side mover, bool bounce)
        {
            Side viewer = _session != null && _session.Hotseat
                ? mover
                : (_session?.PlayerSide ?? Side.White);
            bool own = _session == null || _session.Hotseat || mover == viewer;
            VisionMap vision = VisionMap.Compute(_state, viewer);
            bool identified = own || vision.IsIdentified(move.From) || vision.IsIdentified(move.To);
            if (!identified)
            {
                GameAudio.PlayHidden();
                return;
            }

            bool capture = !bounce
                && (move.Kind == MoveKind.Capture
                    || move.Kind == MoveKind.EnPassant
                    || move.Kind == MoveKind.Bombard
                    || move.CapturedType != null);
            if (capture)
                GameAudio.PlayCapture();
            else
                GameAudio.PlayMove();
        }
        void PlayMatePresentation(Side winner)
        {
            if (boardView == null)
            {
                hud?.RevealGameOver();
                return;
            }
            boardView.PlayMateClear(winner.Opponent(), () => hud?.RevealGameOver());
        }
        void PlayCheckPresentation()
        {
            Guid? kingId = FindKingId(_state, _state.SideToMove);
            if (kingId == null)
                return;
            boardView?.PlayCheck(kingId.Value);
        }
        void OnMatchChromeHidden(bool hidden)
        {
            hud?.SetMatchChromeVisible(!hidden);
        }
        static Piece FindVictim(GameState state, Move move)
        {
            switch (move.Kind)
            {
                case MoveKind.Capture:
                case MoveKind.Promotion:
                case MoveKind.Bombard:
                    return state.Board.GetPiece(move.To);
                case MoveKind.EnPassant:
                    return state.Board.GetPiece(new Square(move.To.File, move.From.Rank));
                case MoveKind.Quiet:
                case MoveKind.CastleKingSide:
                case MoveKind.CastleQueenSide:
                case MoveKind.Swap:
                    return null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(move), move.Kind, null);
            }
        }
        static Guid? FindKingId(GameState state, Side side)
        {
            if (state == null)
                return null;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = state.Board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Side == side && piece.Type == PieceType.King)
                    return piece.Id;
            }
            return null;
        }

        void PlayAi()
        {
            PlaySideAi(_session.Rules.Settings.AiStrength);
        }
        void PlaySideAi(AiStrength strength)
        {
            if (_state.DraftPending)
                return;
            if (_state.LegalMoves.Count > 0)
            {
                Move? move = SimpleAi.Choose(_state, strength, _state.SideToMove);
                if (move == null)
                    return;
                Commit(move.Value);
                return;
            }
            if (!_state.CanEndTurn())
                return;
            Side ended = _state.SideToMove;
            _state = _state.EndTurn();
            NoteCampaignTurnEnd(ended);
            _clock?.AddIncrement(ended);
            ClearSelection();
            RefreshPresentation();
        }
        bool ShouldRunSideAi(out AiStrength strength)
        {
            strength = AiStrength.Easy;
            if (_state.Status != GameStatus.InProgress || _state.DraftPending || _pendingPromotions != null)
                return false;
            if (Autoplay.Active && IsPlayerSideToMove())
            {
                strength = Autoplay.Strength;
                return true;
            }
            if (_session.IsAi && _state.SideToMove != _session.PlayerSide)
            {
                strength = _session.Rules.Settings.AiStrength;
                return true;
            }
            return false;
        }
        bool IsPlayerSideToMove()
        {
            return _session != null && _state != null && _state.SideToMove == _session.PlayerSide;
        }
        bool TickAutoplayRematch()
        {
            if (!Autoplay.Active || _state.Status == GameStatus.InProgress)
            {
                _autoplayRematchWait = -1f;
                _autoplayRematchSent = false;
                return false;
            }
            if (_autoplayRematchSent)
                return true;
            if (_session.Activity == Activity.VersusFriend && !CanAutoplayFriendRematch())
                return false;
            if (_autoplayRematchWait < 0f)
                _autoplayRematchWait = AutoplayRematchSeconds;
            _autoplayRematchWait -= Time.deltaTime;
            if (_autoplayRematchWait > 0f)
                return true;
            _autoplayRematchWait = -1f;
            _autoplayRematchSent = true;
            RequestRematch();
            return true;
        }
        static bool CanAutoplayFriendRematch()
        {
            return false;
        }
        void TickAutoplaySetup()
        {
            if (_session == null || _state == null)
                return;
            int budget = _session.Rules.Settings.EmpowerBudget;
            List<Guid> picks = _session.PlayerSide == Side.White ? _whitePicks : _blackPicks;
            if (SetupSpent(_session.PlayerSide) != budget)
                SimpleAi.AutopickEmpowered(_state, _session.PlayerSide, budget, picks);
            if (SetupSpent(Side.White) == budget && SetupSpent(Side.Black) == budget)
                ConfirmSetup();
        }

        void TickDraft()
        {
            bool aiDraft = _session != null && _session.IsAi && _state.SideToMove != _session.PlayerSide;
            bool autoplayDraft = Autoplay.Active && IsPlayerSideToMove();
            if (aiDraft || autoplayDraft)
            {
                if (!_draftTiming)
                {
                    _draftTiming = true;
                    _draftRemaining = AiThinkSeconds;
                }
                _draftRemaining -= Time.deltaTime;
                hud?.SetClock(_clock, ClockSide());
                if (_draftRemaining <= 0f)
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
            hud?.SetClock(_clock, ClockSide());
            if (_draftTargeting != null)
                hud?.SetStatusLine(DraftTargetLine(left));
            else
                hud?.SetStatusLine(Loc.Format("match.draft", left));
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
            _aiWait = AiThinkSeconds;
        }

        void ApplyRandomDraft()
        {
            DraftOffer? offer = _state.Runtime.PendingDraft;
            if (offer == null) return;
            MartyrPower power;
            if (_draftTargeting != null) power = _draftTargeting.Value;
            else
            {
                MartyrPower[] options = new MartyrPower[offer.Value.Count];
                for (int i = 0; i < options.Length; i++) options[i] = offer.Value.At(i);
                power = options[UnityEngine.Random.Range(0, options.Length)];
            }
            CommitDraft(power, AutoTargetId(power), AutoSquares(power));
        }
        void TryPickDraft(MartyrPower power)
        {
            if (NeedsBoardTarget(power) && NeedsInteractiveTargeting(power))
            {
                _draftTargeting = power;
                _reinforcementPicks.Clear();
                _vanishingPieceId = null;
                RefreshPresentation();
                return;
            }
            CommitDraft(power, AutoTargetId(power), AutoSquares(power));
        }
        void CommitDraft(MartyrPower power, Guid? targetId, Square[] reinforcements)
        {
            _draftTiming = false;
            _draftTargeting = null;
            _reinforcementPicks.Clear();
            _vanishingPieceId = null;
            ClearSelection();
            Square? targetSquare = null;
            if (targetId.HasValue) targetSquare = _state.Board.FindSquare(targetId.Value);
            _historyEvents.Add(MatchHistoryStore.DraftEvent(power, targetSquare, reinforcements));
            _state = _state.ApplyDraft(power, targetId, reinforcements);
            RefreshPresentation();
            boardView?.PlayPowerFeel(power, targetId);
        }
        void HandleDraftTarget(Square square)
        {
            if (_state == null || _draftTargeting == null) return;
            MartyrPower power = _draftTargeting.Value;
            if (power == MartyrPower.Reinforcements
                || power == MartyrPower.SecondFront
                || power == MartyrPower.Landmine)
            {
                HandleSquarePick(power, square);
                return;
            }
            if (power == MartyrPower.Rearguard)
            {
                HandleRearguardSquare(square);
                return;
            }
            if (power == MartyrPower.VanishingAct)
            {
                HandleVanishingSquare(square);
                return;
            }
            Piece piece = _state.Board.GetPiece(square);
            if (piece == null)
            {
                GameAudio.PlayIllegal();
                return;
            }
            if (power == MartyrPower.BattlefieldPromotion)
            {
                if (piece.Side != _state.SideToMove || piece.Type != PieceType.Pawn)
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                CommitDraft(MartyrPower.BattlefieldPromotion, piece.Id, null);
                return;
            }
            if (power == MartyrPower.StasisField)
            {
                if (piece.Side == _state.SideToMove || piece.Type != PieceType.Queen)
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                CommitDraft(MartyrPower.StasisField, piece.Id, null);
                return;
            }
            if (power == MartyrPower.Exile)
            {
                if (!_state.IsExileTarget(square))
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                CommitDraft(MartyrPower.Exile, piece.Id, null);
                return;
            }
            if (power == MartyrPower.Turncoat)
            {
                if (piece.Side == _state.SideToMove || piece.Type != PieceType.Pawn)
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                CommitDraft(MartyrPower.Turncoat, piece.Id, null);
                return;
            }
            if (power == MartyrPower.Overload)
            {
                if (piece.Side != _state.SideToMove || piece.Type == PieceType.King)
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                CommitDraft(MartyrPower.Overload, piece.Id, null);
            }
        }
        void HandleSquarePick(MartyrPower power, Square square)
        {
            if (!_state.Board.CanPlace(square) || !_draftTargets.Contains(square))
            {
                GameAudio.PlayIllegal();
                return;
            }
            if (power == MartyrPower.Reinforcements)
            {
                if (_reinforcementPicks.Contains(square)) return;
                _reinforcementPicks.Add(square);
                if (_reinforcementPicks.Count >= 2 || CountTargets(MartyrPower.Reinforcements) == 0)
                {
                    CommitDraft(MartyrPower.Reinforcements, null, _reinforcementPicks.ToArray());
                    return;
                }
                RefreshPresentation();
                return;
            }
            CommitDraft(power, null, new[] { square });
        }
        void HandleRearguardSquare(Square square)
        {
            Piece piece = _state.Board.GetPiece(square);
            if (piece == null
                || piece.Side != _state.SideToMove
                || piece.Type != PieceType.Pawn
                || !_draftTargets.Contains(square))
            {
                GameAudio.PlayIllegal();
                return;
            }
            if (_reinforcementPicks.Contains(square)) return;
            _reinforcementPicks.Add(square);
            if (_reinforcementPicks.Count >= 2 || CountTargets(MartyrPower.Rearguard) == 0)
            {
                CommitDraft(MartyrPower.Rearguard, null, _reinforcementPicks.ToArray());
                return;
            }
            RefreshPresentation();
        }
        void HandleVanishingSquare(Square square)
        {
            if (_vanishingPieceId == null)
            {
                Piece piece = _state.Board.GetPiece(square);
                if (piece == null || !_draftTargets.Contains(square))
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                _vanishingPieceId = piece.Id;
                RefreshPresentation();
                return;
            }
            if (!_state.Board.CanPlace(square) || !_draftTargets.Contains(square))
            {
                GameAudio.PlayIllegal();
                return;
            }
            CommitDraft(MartyrPower.VanishingAct, _vanishingPieceId, new[] { square });
        }
        Square[] FillMultiSquares(MartyrPower power, int want)
        {
            CollectDraftTargets(power);
            var chosen = new List<Square>(_reinforcementPicks);
            var remaining = new List<Square>(_draftTargets);
            while (chosen.Count < want && remaining.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, remaining.Count);
                chosen.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
            return chosen.ToArray();
        }
        Square[] AutoSquares(MartyrPower power)
        {
            switch (power)
            {
                case MartyrPower.Reinforcements:
                    return FillMultiSquares(MartyrPower.Reinforcements, 2);
                case MartyrPower.Rearguard:
                    return FillMultiSquares(MartyrPower.Rearguard, 2);
                case MartyrPower.SecondFront:
                case MartyrPower.Landmine:
                    CollectDraftTargets(power);
                    if (_draftTargets.Count == 0) return null;
                    return new[] { _draftTargets[UnityEngine.Random.Range(0, _draftTargets.Count)] };
                case MartyrPower.VanishingAct:
                    CollectDraftTargets(MartyrPower.VanishingAct);
                    if (_draftTargets.Count == 0) return null;
                    return new[] { _draftTargets[UnityEngine.Random.Range(0, _draftTargets.Count)] };
                default:
                    return null;
            }
        }
        Guid? AutoTargetId(MartyrPower power)
        {
            if (power == MartyrPower.Reinforcements
                || power == MartyrPower.SecondFront
                || power == MartyrPower.Landmine
                || power == MartyrPower.Rearguard)
            {
                return null;
            }
            if (power == MartyrPower.VanishingAct)
            {
                if (_vanishingPieceId != null) return _vanishingPieceId;
                CollectDraftTargets(MartyrPower.VanishingAct);
                if (_draftTargets.Count == 0) return null;
                Piece piece = _state.Board.GetPiece(_draftTargets[UnityEngine.Random.Range(0, _draftTargets.Count)]);
                if (piece == null) return null;
                _vanishingPieceId = piece.Id;
                return piece.Id;
            }
            CollectDraftTargets(power);
            if (_draftTargets.Count == 0) return null;
            Piece target = _state.Board.GetPiece(_draftTargets[UnityEngine.Random.Range(0, _draftTargets.Count)]);
            return target != null ? target.Id : (Guid?)null;
        }
        bool NeedsBoardTarget(MartyrPower power)
        {
            switch (power)
            {
                case MartyrPower.BattlefieldPromotion:
                case MartyrPower.StasisField:
                case MartyrPower.Exile:
                case MartyrPower.Reinforcements:
                case MartyrPower.SecondFront:
                case MartyrPower.Turncoat:
                case MartyrPower.VanishingAct:
                case MartyrPower.Rearguard:
                case MartyrPower.Overload:
                case MartyrPower.Landmine:
                    return true;
                default:
                    return false;
            }
        }
        bool NeedsInteractiveTargeting(MartyrPower power)
        {
            if (power == MartyrPower.VanishingAct) return CountTargets(power) >= 1;
            if (power == MartyrPower.Reinforcements || power == MartyrPower.Rearguard)
            {
                return CountTargets(power) > 1;
            }
            return CountTargets(power) > 1;
        }
        int CountTargets(MartyrPower power)
        {
            CollectDraftTargets(power);
            return _draftTargets.Count;
        }
        void CollectDraftTargets(MartyrPower power)
        {
            _draftTargets.Clear();
            if (_state == null) return;
            Side side = _state.SideToMove;
            switch (power)
            {
                case MartyrPower.BattlefieldPromotion:
                    CollectPieces(side, PieceType.Pawn);
                    break;
                case MartyrPower.StasisField:
                    CollectPieces(side.Opponent(), PieceType.Queen);
                    break;
                case MartyrPower.Exile:
                    for (int i = 0; i < 64; i++)
                    {
                        Square square = Square.FromIndex(i);
                        if (_state.IsExileTarget(square)) _draftTargets.Add(square);
                    }
                    break;
                case MartyrPower.Reinforcements:
                {
                    int back = side == Side.White ? 0 : 7;
                    for (int file = 0; file < Square.BoardSize; file++)
                    {
                        Square square = new Square(file, back);
                        if (!_state.Board.CanPlace(square)) continue;
                        if (_reinforcementPicks.Contains(square)) continue;
                        _draftTargets.Add(square);
                    }
                    break;
                }
                case MartyrPower.SecondFront:
                    for (int i = 0; i < 64; i++)
                    {
                        Square square = Square.FromIndex(i);
                        if (GameState.IsBackTwoRanks(square, side) && _state.Board.CanPlace(square))
                            _draftTargets.Add(square);
                    }
                    break;
                case MartyrPower.Turncoat:
                    CollectPieces(side.Opponent(), PieceType.Pawn);
                    break;
                case MartyrPower.VanishingAct:
                    if (_vanishingPieceId == null)
                    {
                        for (int i = 0; i < 64; i++)
                        {
                            Square square = Square.FromIndex(i);
                            if (_state.IsVanishingActPiece(square)) _draftTargets.Add(square);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < 64; i++)
                        {
                            Square square = Square.FromIndex(i);
                            if (GameState.IsOwnHalf(square, side) && _state.Board.CanPlace(square))
                                _draftTargets.Add(square);
                        }
                    }
                    break;
                case MartyrPower.Rearguard:
                    for (int i = 0; i < 64; i++)
                    {
                        Square square = Square.FromIndex(i);
                        if (_reinforcementPicks.Contains(square)) continue;
                        Piece piece = _state.Board.GetPiece(square);
                        if (piece != null
                            && piece.Side == side
                            && piece.Type == PieceType.Pawn
                            && GameState.IsBackTwoRanks(square, side))
                        {
                            _draftTargets.Add(square);
                        }
                    }
                    break;
                case MartyrPower.Overload:
                    for (int i = 0; i < 64; i++)
                    {
                        Square square = Square.FromIndex(i);
                        Piece piece = _state.Board.GetPiece(square);
                        if (piece != null && piece.Side == side && piece.Type != PieceType.King)
                            _draftTargets.Add(square);
                    }
                    break;
                case MartyrPower.Landmine:
                    for (int i = 0; i < 64; i++)
                    {
                        Square square = Square.FromIndex(i);
                        if (GameState.IsOwnHalf(square, side)
                            && _state.Board.CanPlace(square)
                            && !_state.Runtime.TryGetLandmine(square, out _))
                        {
                            _draftTargets.Add(square);
                        }
                    }
                    break;
            }
        }
        void CollectPieces(Side side, PieceType type)
        {
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = _state.Board.GetPiece(square);
                if (piece != null && piece.Side == side && piece.Type == type)
                    _draftTargets.Add(square);
            }
        }
        void ApplyDraftTargeting()
        {
            if (boardView == null || _draftTargeting == null) return;
            CollectDraftTargets(_draftTargeting.Value);
            boardView.SetTargeting(_draftTargets);
        }
        string DraftTargetLine(int secondsLeft)
        {
            string key;
            switch (_draftTargeting)
            {
                case MartyrPower.Reinforcements:
                    key = "martyr.pick.reinforcements";
                    break;
                case MartyrPower.StasisField:
                    key = "martyr.pick.queen";
                    break;
                case MartyrPower.Exile:
                    key = "martyr.pick.exile";
                    break;
                case MartyrPower.SecondFront:
                    key = "martyr.pick.secondFront";
                    break;
                case MartyrPower.Turncoat:
                    key = "martyr.pick.turncoat";
                    break;
                case MartyrPower.VanishingAct:
                    key = _vanishingPieceId == null ? "martyr.pick.vanishing.piece" : "martyr.pick.vanishing.square";
                    break;
                case MartyrPower.Rearguard:
                    key = "martyr.pick.rearguard";
                    break;
                case MartyrPower.Overload:
                    key = "martyr.pick.overload";
                    break;
                case MartyrPower.Landmine:
                    key = "martyr.pick.landmine";
                    break;
                default:
                    key = "martyr.pick.pawn";
                    break;
            }
            return Loc.Format(key, secondsLeft);
        }

        private void RefreshPresentation()
        {
            if (boardView == null || _state == null)
                return;
            if (_selected.HasValue && _selectionSide != _state.SideToMove)
                ClearSelection();

            Side viewer = _session != null && _session.Hotseat ? _state.SideToMove : (_session?.PlayerSide ?? Side.White);
            VisionMap vision = VisionMap.Compute(_state, viewer);
            boardView.ViewerSide = viewer;
            boardView.ReviewVision = _replaying;
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
            else if (_draftTargeting != null)
                ApplyDraftTargeting();

            if (hud == null)
                return;

            hud.Bind(_state, _state.History, viewer);
            hud.SetNames(_session);
            if (_replaying)
            {
                hud.SetClock(null, ClockSide());
                hud.SetEndTurnVisible(false);
                hud.SetPauseVisible(false);
                hud.SetResignVisible(false);
                hud.SetSetupConfirm(false, false, false);
                hud.HideDraft();
                hud.SetLostMaterial(
                    _session != null && _session.Rules.Has(ModeId.Martyr)
                        ? _state.Runtime.LostMaterial(Side.White)
                        : (int?)null,
                    _session != null && _session.Rules.Has(ModeId.Martyr)
                        ? _state.Runtime.LostMaterial(Side.Black)
                        : (int?)null,
                    _session != null ? _session.Rules.Settings.MartyrThreshold : 0,
                    _session != null && _session.Rules.Has(ModeId.Martyr)
                        ? _state.Runtime.BloodDebtCharges(Side.White)
                        : 0,
                    _session != null && _session.Rules.Has(ModeId.Martyr)
                        ? _state.Runtime.BloodDebtCharges(Side.Black)
                        : 0);
                hud.SetReplayHeadline(_replayRecord);
                hud.SetReplayAuto(_replayAuto, _replaySpeed);
                RefreshPieceDetails();
                return;
            }

            hud.SetClock(_clock, ClockSide());
            bool localTurn = _session == null || _session.Hotseat || _state.SideToMove == _session.PlayerSide;
            hud.SetEndTurnVisible(!_inSetup && localTurn && _state.CanEndTurn() && !Autoplay.Active);
            hud.SetPauseVisible(!_inSetup && _session != null && _session.IsAi && _state.Status == GameStatus.InProgress);
            hud.SetResignVisible(!_inSetup && _state.Status == GameStatus.InProgress);
            int budget = _session != null ? _session.Rules.Settings.EmpowerBudget : 0;
            bool bothPicked = SetupSpent(Side.White) == budget && SetupSpent(Side.Black) == budget;
            if (!bothPicked)
                _setupConfirms = 0;
            bool opponentReady = _session != null && !_session.IsAi && _setupConfirms > 0;
            hud.SetSetupConfirm(_inSetup, bothPicked, opponentReady);
            if (_session != null && _session.Rules.Has(ModeId.Martyr))
            {
                hud.SetLostMaterial(
                    _state.Runtime.LostMaterial(Side.White),
                    _state.Runtime.LostMaterial(Side.Black),
                    _session.Rules.Settings.MartyrThreshold,
                    _state.Runtime.BloodDebtCharges(Side.White),
                    _state.Runtime.BloodDebtCharges(Side.Black));
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
            else if (_draftTargeting != null)
            {
                int left = _draftTiming ? Mathf.Max(0, Mathf.CeilToInt(_draftRemaining)) : 60;
                hud.HideDraft();
                hud.SetStatusLine(DraftTargetLine(left));
            }
            else if (_state.DraftPending)
            {
                int left = _draftTiming ? Mathf.Max(0, Mathf.CeilToInt(_draftRemaining)) : 60;
                hud.SetStatusLine(Loc.Format("match.draft", left));
                hud.ShowDraft(_state, TryPickDraft);
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
            if (OptionsOverlay.IsOpen || (hud != null && hud.BlocksBoardInput) || BoardPointerInput.IsScreenBlockedByUi())
            {
                _hovered = null;
                if (!EffectIconView.Hovered)
                    RefreshPieceDetails();
                return;
            }

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
            int remaining = SetupRemaining(picker);
            List<Guid> picks = picker == Side.White ? _whitePicks : _blackPicks;
            var valid = new List<Square>();
            var dimmed = new List<Guid>();
            for (int file = 0; file < BoardLayout.FileCount; file++)
            {
                for (int rank = 0; rank < BoardLayout.RankCount; rank++)
                {
                    var square = new Square(file, rank);
                    Piece piece = _state.Board.GetPiece(square);
                    if (piece == null || piece.Side != picker)
                        continue;
                    bool selected = picks.Contains(piece.Id);
                    if (selected || EmpoweredPowers.Cost(piece.Type) <= remaining)
                        valid.Add(square);
                    else
                        dimmed.Add(piece.Id);
                }
            }

            boardView.SetTargeting(valid);
            boardView.SetDimmed(dimmed);
        }

        Side ClockSide()
        {
            return _session?.PlayerSide ?? Side.White;
        }

        Side SetupPicker()
        {
            if (_session != null && _session.Hotseat)
            {
                int budget = _session.Rules.Settings.EmpowerBudget;
                return SetupSpent(Side.White) == budget ? Side.Black : Side.White;
            }
            return _session?.PlayerSide ?? Side.White;
        }

        List<Guid> PendingEmpoweredIds()
        {
            List<Guid> picks = SetupPicker() == Side.White ? _whitePicks : _blackPicks;
            return new List<Guid>(picks);
        }

        int SetupSpent(Side side)
        {
            List<Guid> picks = side == Side.White ? _whitePicks : _blackPicks;
            return SetupSpent(picks);
        }

        int SetupSpent(List<Guid> picks)
        {
            if (_state == null || picks == null)
                return 0;
            int spent = 0;
            for (int i = 0; i < picks.Count; i++)
            {
                Square? square = _state.Board.FindSquare(picks[i]);
                if (!square.HasValue)
                    continue;
                Piece piece = _state.Board.GetPiece(square.Value);
                if (piece != null)
                    spent += EmpoweredPowers.Cost(piece.Type);
            }
            return spent;
        }

        int SetupRemaining(Side side)
        {
            if (_session == null)
                return 0;
            return _session.Rules.Settings.EmpowerBudget - SetupSpent(side);
        }

        string SetupStatusLine()
        {
            int budget = _session.Rules.Settings.EmpowerBudget;
            Side picker = SetupPicker();
            int spent = SetupSpent(picker);
            return Loc.Format("match.setup", Mathf.CeilToInt(_setupRemaining), spent, budget);
        }

        void RefreshPieceDetails()
        {
            if (hud == null || _state == null)
                return;
            if (EffectIconView.Hovered)
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
            if (_replaying)
                return piece;

            Side viewer = _session != null && _session.Hotseat ? _state.SideToMove : (_session?.PlayerSide ?? Side.White);
            if (piece.Side == viewer)
                return piece;

            VisionMap vision = VisionMap.Compute(_state, viewer);
            return vision[square] == SquareSight.Identified ? piece : null;
        }
    }
}
