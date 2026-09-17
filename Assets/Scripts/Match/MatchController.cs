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
        int _setupConfirms;
        readonly List<Guid> _whitePicks = new List<Guid>();
        readonly List<Guid> _blackPicks = new List<Guid>();
        MatchClock _clock;
        bool _historyWritten;
        bool _draftTiming;
        float _draftRemaining;
        float _aiWait = -1f;
        MartyrPower? _draftTargeting;
        readonly List<Square> _draftTargets = new List<Square>();
        readonly List<Square> _reinforcementPicks = new List<Square>();
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

            if (_paused)
            {
                hud?.SetClock(_clock, ClockSide());
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
            _setupConfirms = 0;
            _hovered = null;
            _pinnedPieceId = null;
            _paused = false;
            _clock = new MatchClock(session.Rules.Settings.Time);
            _historyWritten = false;
            _draftTiming = false;
            _draftRemaining = 0f;
            _draftTargeting = null;
            _draftTargets.Clear();
            _reinforcementPicks.Clear();
            _aiWait = -1f;
            boardView?.CompleteMotion();
            boardView?.SetMotionPaused(false);
            BoardCamera.ClearTrauma();
            hud?.SetDeferGameOver(false);
            hud?.SetMatchChromeVisible(true);
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
            hud?.SetSetupConfirm(false, false, false);
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
            hud?.SetClock(_clock, ClockSide());
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
            PinFromClick(square);
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
                RefreshPieceDetails();
                return;
            }
            if (_selected.HasValue)
            {
                if (_selected.Value.Equals(square))
                {
                    ClearSelection();
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
            GameAudio.PlayIllegal();
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
            else
            {
                GameAudio.PlayIllegal();
                return;
            }

            GameAudio.PlaySelect();
            RefreshPresentation();
        }

        public void ConfirmSetup()
        {
            if (!_inSetup || _session == null)
                return;
            int n = _session.Rules.Settings.EmpoweredCount;
            if (_whitePicks.Count < n || _blackPicks.Count < n)
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
            _setupConfirms = 0;
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
            GameAudio.PlaySelect();
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
            Piece victim = FindVictim(_state, move);
            _state = _state.Apply(move);
            _pendingPromotions = null;
            ClearSelection();
            promotionPicker?.Hide();
            if (_state.SideToMove != moved)
                _clock?.AddIncrement(moved);
            bool bounce = victim != null && _state.Runtime.ExtraLifeSpent(victim.Id);
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
            MartyrPower power;
            if (_draftTargeting != null)
            {
                power = _draftTargeting.Value;
            }
            else
            {
                MartyrPower[] options = new MartyrPower[offer.Value.Count];
                for (int i = 0; i < options.Length; i++)
                    options[i] = offer.Value.At(i);
                power = options[UnityEngine.Random.Range(0, options.Length)];
            }
            Guid? target = power == MartyrPower.Reinforcements ? null : TargetIfNeeded(power);
            Square[] reinforcements = power == MartyrPower.Reinforcements ? FillReinforcements() : null;
            CommitDraft(power, target, reinforcements);
        }
        void TryPickDraft(MartyrPower power)
        {
            if (NeedsBoardTarget(power) && CountTargets(power) > 1)
            {
                _draftTargeting = power;
                _reinforcementPicks.Clear();
                RefreshPresentation();
                return;
            }
            Guid? target = power == MartyrPower.Reinforcements ? null : TargetIfNeeded(power);
            Square[] reinforcements = power == MartyrPower.Reinforcements ? FillReinforcements() : null;
            CommitDraft(power, target, reinforcements);
        }
        void CommitDraft(MartyrPower power, Guid? targetId, Square[] reinforcements)
        {
            _draftTiming = false;
            _draftTargeting = null;
            _reinforcementPicks.Clear();
            _state = _state.ApplyDraft(power, targetId, reinforcements);
            RefreshPresentation();
            boardView?.PlayPowerFeel(power, targetId);
        }
        void HandleDraftTarget(Square square)
        {
            if (_state == null || _draftTargeting == null)
                return;
            if (_draftTargeting == MartyrPower.Reinforcements)
            {
                HandleReinforcementSquare(square);
                return;
            }
            Piece piece = _state.Board.GetPiece(square);
            if (piece == null)
            {
                GameAudio.PlayIllegal();
                return;
            }
            if (_draftTargeting == MartyrPower.BattlefieldPromotion)
            {
                if (piece.Side != _state.SideToMove || piece.Type != PieceType.Pawn)
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                CommitDraft(MartyrPower.BattlefieldPromotion, piece.Id, null);
                return;
            }
            if (_draftTargeting == MartyrPower.StasisField)
            {
                if (piece.Side == _state.SideToMove || piece.Type != PieceType.Queen)
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                CommitDraft(MartyrPower.StasisField, piece.Id, null);
                return;
            }
            if (_draftTargeting == MartyrPower.Exile)
            {
                if (piece.Side == _state.SideToMove || piece.Type == PieceType.King)
                {
                    GameAudio.PlayIllegal();
                    return;
                }
                CommitDraft(MartyrPower.Exile, piece.Id, null);
            }
        }
        void HandleReinforcementSquare(Square square)
        {
            if (!_state.Board.CanPlace(square) || !_draftTargets.Contains(square))
            {
                GameAudio.PlayIllegal();
                return;
            }
            if (_reinforcementPicks.Contains(square))
                return;
            _reinforcementPicks.Add(square);
            if (_reinforcementPicks.Count >= 2 || CountTargets(MartyrPower.Reinforcements) == 0)
            {
                CommitDraft(MartyrPower.Reinforcements, null, _reinforcementPicks.ToArray());
                return;
            }
            RefreshPresentation();
        }
        Square[] FillReinforcements()
        {
            CollectDraftTargets(MartyrPower.Reinforcements);
            var chosen = new List<Square>(_reinforcementPicks);
            var remaining = new List<Square>(_draftTargets);
            while (chosen.Count < 2 && remaining.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, remaining.Count);
                chosen.Add(remaining[index]);
                remaining.RemoveAt(index);
            }
            return chosen.ToArray();
        }
        bool NeedsBoardTarget(MartyrPower power)
        {
            return power == MartyrPower.BattlefieldPromotion
                || power == MartyrPower.StasisField
                || power == MartyrPower.Exile
                || power == MartyrPower.Reinforcements;
        }
        int CountTargets(MartyrPower power)
        {
            CollectDraftTargets(power);
            return _draftTargets.Count;
        }
        Guid? TargetIfNeeded(MartyrPower power)
        {
            CollectDraftTargets(power);
            if (_draftTargets.Count == 0)
                return null;
            Square square = _draftTargets[UnityEngine.Random.Range(0, _draftTargets.Count)];
            Piece piece = _state.Board.GetPiece(square);
            return piece != null ? piece.Id : (Guid?)null;
        }
        void CollectDraftTargets(MartyrPower power)
        {
            _draftTargets.Clear();
            if (_state == null)
                return;
            Side side;
            PieceType type;
            if (power == MartyrPower.BattlefieldPromotion)
            {
                side = _state.SideToMove;
                type = PieceType.Pawn;
            }
            else if (power == MartyrPower.StasisField)
            {
                side = _state.SideToMove.Opponent();
                type = PieceType.Queen;
            }
            else if (power == MartyrPower.Exile)
            {
                Side enemy = _state.SideToMove.Opponent();
                for (int i = 0; i < 64; i++)
                {
                    Square square = Square.FromIndex(i);
                    Piece piece = _state.Board.GetPiece(square);
                    if (piece != null && piece.Side == enemy && piece.Type != PieceType.King)
                        _draftTargets.Add(square);
                }
                return;
            }
            else if (power == MartyrPower.Reinforcements)
            {
                int back = _state.SideToMove == Side.White ? 0 : 7;
                for (int file = 0; file < Square.BoardSize; file++)
                {
                    Square square = new Square(file, back);
                    if (!_state.Board.CanPlace(square))
                        continue;
                    if (_reinforcementPicks.Contains(square))
                        continue;
                    _draftTargets.Add(square);
                }
                return;
            }
            else
            {
                return;
            }
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
            if (boardView == null || _draftTargeting == null)
                return;
            CollectDraftTargets(_draftTargeting.Value);
            boardView.SetTargeting(_draftTargets);
        }
        string DraftTargetLine(int secondsLeft)
        {
            string key = _draftTargeting == MartyrPower.Reinforcements
                ? "martyr.pick.reinforcements"
                : _draftTargeting == MartyrPower.StasisField
                    ? "martyr.pick.queen"
                    : _draftTargeting == MartyrPower.Exile
                        ? "martyr.pick.exile"
                        : "martyr.pick.pawn";
            return Loc.Format(key, secondsLeft);
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
            else if (_draftTargeting != null)
                ApplyDraftTargeting();

            if (hud == null)
                return;

            hud.Bind(_state, _state.History);
            hud.SetNames(_session);
            hud.SetClock(_clock, ClockSide());
            bool localTurn = _session == null || _session.Hotseat || _state.SideToMove == _session.PlayerSide;
            hud.SetEndTurnVisible(!_inSetup && localTurn && _state.CanEndTurn());
            hud.SetPauseVisible(!_inSetup && _session != null && _session.IsAi && _state.Status == GameStatus.InProgress);
            hud.SetResignVisible(!_inSetup && _state.Status == GameStatus.InProgress);
            int n = _session != null ? _session.Rules.Settings.EmpoweredCount : 0;
            bool bothPicked = _whitePicks.Count >= n && _blackPicks.Count >= n;
            if (!bothPicked)
                _setupConfirms = 0;
            bool opponentReady = _session != null && !_session.IsAi && _setupConfirms > 0;
            hud.SetSetupConfirm(_inSetup, bothPicked, opponentReady);
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

        Side ClockSide()
        {
            return _session?.PlayerSide ?? Side.White;
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
            return Loc.Format("match.setup", Mathf.CeilToInt(_setupRemaining), count, n);
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
