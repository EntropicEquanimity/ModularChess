using System;
using System.Collections.Generic;
using DG.Tweening;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class BoardView : MonoBehaviour, IBoardView
    {
        public event Action<Square> SquareClicked;
        public event Action<Square> SquarePressed;
        public event Action<Square?> SquareReleased;
        public event Action<Square?> SquareHovered;
        public event Action<bool> MatchChromeHidden;

        [SerializeField] float squareSize = 1f;
        [SerializeField] float captureSpacing = 0.7f;
        [SerializeField] BoardTheme theme;
        [SerializeField] bool allowSelectionWhenFinished;
        [SerializeField] bool buildOnAwake = true;
        [SerializeField] PieceView piecePrefab;
        [SerializeField] SquareView squarePrefab;

        readonly Dictionary<Guid, PieceView> _pieces = new Dictionary<Guid, PieceView>();
        readonly SquareView[] _squares = new SquareView[BoardLayout.FileCount * BoardLayout.RankCount];
        readonly HashSet<Guid> _seenIds = new HashSet<Guid>();
        readonly List<Guid> _staleIds = new List<Guid>();
        readonly List<PieceView> _deferredDestroy = new List<PieceView>();
        readonly HashSet<Guid> _pendingEmpowered = new HashSet<Guid>();
        readonly HashSet<Square> _validTargets = new HashSet<Square>();
        readonly HashSet<Guid> _banished = new HashSet<Guid>();
        readonly HashSet<Guid> _stageSnapIds = new HashSet<Guid>();
        readonly List<(Guid id, Vector3 dest, bool fromTop)> _stageEntries = new List<(Guid, Vector3, bool)>(32);
        Vector3 _restLocal;
        bool _restLocalCached;
        Tween _boardMotion;
        bool _stageReveal;
        Action _stageRevealDone;
        float _stagePieceDuration = 1f;
        float _stageStagger = 0.25f;

        Transform _squaresRoot;
        Transform _piecesRoot;
        BoardLayout _layout;
        GameState _state;
        VisionMap _vision = VisionMap.AllIdentified;
        Side _viewer = Side.White;
        Square? _selected;
        Square? _lastFrom;
        Square? _lastTo;
        Square? _hovered;
        Side _pieceLayoutViewer = Side.White;
        int _movingCount;
        bool _built;
        bool _targeting;
        Guid? _selectPopId;
        bool _chromeUntilIdle;
        Action _idleOnce;

        public GameState BoundState => _state;
        public bool PiecesBusy => _movingCount > 0 || (_boardMotion != null && _boardMotion.IsActive());
        public bool StageRevealBusy => _stageReveal;
        public bool HidingMatchChrome { get; private set; }
        public Side ViewerSide
        {
            get => _viewer;
            set
            {
                _viewer = value;
                if (_built)
                    Relayout();
            }
        }
        public Square? SelectedSquare => _selected;
        public float SquareSize => _built ? _layout.SquareSize : squareSize;
        public BoardLayout Layout => _built ? _layout : new BoardLayout(squareSize, captureSpacing);
        public bool ReviewVision { get; set; }

        public bool AllowSelectionWhenFinished
        {
            get => allowSelectionWhenFinished;
            set => allowSelectionWhenFinished = value;
        }

        void Reset()
        {
            squareSize = 1f;
            theme = BoardTheme.Default;
            allowSelectionWhenFinished = false;
            buildOnAwake = true;
            piecePrefab = LoadDefaultPiecePrefab();
            squarePrefab = LoadDefaultSquarePrefab();
        }

        void Awake()
        {
            EnsureTheme();
            ResolvePiecePrefab();
            ResolveSquarePrefab();
            if (buildOnAwake)
                Build();
        }

        public void Bind(GameState state)
        {
            Bind(state, VisionMap.Compute(state, _viewer), _viewer);
        }

        public void Bind(GameState state, VisionMap vision, Side viewer)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            EnsureBuilt();
            _state = state;
            _vision = vision ?? VisionMap.AllIdentified;
            _viewer = viewer;
            Relayout();
            SyncPieces();
            PruneSelectionAfterBind();
            RefreshHighlights();
        }

        public void SetMotionPaused(bool paused)
        {
            foreach (KeyValuePair<Guid, PieceView> pair in _pieces)
            {
                if (pair.Value != null)
                    pair.Value.SetMotionPaused(paused);
            }
        }

        public void CompleteMotion()
        {
            _boardMotion?.Kill();
            _boardMotion = null;
            if (_restLocalCached)
                transform.localPosition = _restLocal;
            foreach (KeyValuePair<Guid, PieceView> pair in _pieces)
            {
                if (pair.Value != null)
                    pair.Value.CompleteMotion();
            }

            _banished.Clear();
            _movingCount = 0;
            _idleOnce = null;
            _chromeUntilIdle = false;
            _stageReveal = false;
            _stageRevealDone = null;
            _stageEntries.Clear();
            SetMatchChromeHidden(false);
            FlushDeferred();
        }

        public void ClearPieceViews()
        {
            CompleteMotion();
            foreach (KeyValuePair<Guid, PieceView> pair in _pieces)
            {
                if (pair.Value != null)
                    Destroy(pair.Value.gameObject);
            }
            _pieces.Clear();
            _banished.Clear();
        }

        public void PlayBoardSlideIn(float duration, Action onComplete)
        {
            EnsureBuilt();
            CacheRestLocal();
            _boardMotion?.Kill();
            float drop = _layout.BoardSizeLocal.y + _layout.SquareSize * 3f;
            transform.localPosition = _restLocal + new Vector3(0f, -drop, 0f);
            float time = AnimationPrefs.MoveDuration(duration);
            if (time <= 0.001f || AnimationPrefs.Instant)
            {
                transform.localPosition = _restLocal;
                onComplete?.Invoke();
                return;
            }
            _boardMotion = DOTween.To(
                    () => transform.localPosition,
                    v => transform.localPosition = v,
                    _restLocal,
                    time)
                .SetEase(Ease.OutCubic)
                .SetTarget(this)
                .OnComplete(() =>
                {
                    _boardMotion = null;
                    onComplete?.Invoke();
                });
        }

        public void BindStageReveal(
            GameState state,
            Side viewer,
            IReadOnlyCollection<Guid> snapIds,
            IReadOnlyCollection<Guid> fromTopIds,
            IReadOnlyCollection<Guid> fromBottomIds,
            float pieceDuration,
            float stagger,
            bool startStagger,
            Action onComplete)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));
            EnsureBuilt();
            ClearPieceViews();
            _stageSnapIds.Clear();
            if (snapIds != null)
            {
                foreach (Guid id in snapIds)
                    _stageSnapIds.Add(id);
            }
            var fromTop = new HashSet<Guid>();
            if (fromTopIds != null)
            {
                foreach (Guid id in fromTopIds)
                    fromTop.Add(id);
            }
            var fromBottom = new HashSet<Guid>();
            if (fromBottomIds != null)
            {
                foreach (Guid id in fromBottomIds)
                    fromBottom.Add(id);
            }
            _stagePieceDuration = pieceDuration;
            _stageStagger = stagger;
            _stageRevealDone = onComplete;
            _stageReveal = true;
            _stageEntries.Clear();
            _state = state;
            _vision = VisionMap.AllIdentified;
            _viewer = viewer;
            Relayout();
            SyncPiecesForStage(fromTop, fromBottom);
            PruneSelectionAfterBind();
            RefreshHighlights();
            if (startStagger)
                StartStageStagger();
            else if (_stageEntries.Count == 0)
                FinishStageReveal();
        }

        public void PlayQueuedStageEntries(Action onComplete)
        {
            _stageRevealDone = onComplete;
            _stageReveal = true;
            StartStageStagger();
        }

        public void SetSelection(Square? square)
        {
            EnsureBuilt();
            _selected = square.HasValue && square.Value.IsOnBoard ? square : null;
            RefreshHighlights();
        }

        public void ClearSelection()
        {
            _selected = null;
            if (_built)
                RefreshHighlights();
        }

        public void SetLastMove(Square from, Square to)
        {
            _lastFrom = from;
            _lastTo = to;
            if (_built)
                RefreshHighlights();
        }

        public void ClearLastMove()
        {
            _lastFrom = null;
            _lastTo = null;
            if (_built)
                RefreshHighlights();
        }

        public Vector3 SquareToWorld(Square square)
        {
            EnsureBuilt();
            return transform.TransformPoint(_layout.SquareCenterLocal(square, _viewer));
        }

        public void SetPendingEmpowered(IEnumerable<Guid> ids)
        {
            _pendingEmpowered.Clear();
            if (ids == null)
                return;
            foreach (Guid id in ids)
                _pendingEmpowered.Add(id);
        }

        public void SetTargeting(IReadOnlyCollection<Square> validSquares)
        {
            EnsureBuilt();
            _targeting = true;
            _validTargets.Clear();
            if (validSquares != null)
            {
                foreach (Square square in validSquares)
                {
                    if (square.IsOnBoard)
                        _validTargets.Add(square);
                }
            }

            RefreshHighlights();
        }

        public void ClearTargeting()
        {
            if (!_targeting)
                return;
            _targeting = false;
            _validTargets.Clear();
            if (_built)
                RefreshHighlights();
        }

        public void PlayDeflect(Square from, Square toward)
        {
            EnsureBuilt();
            if (_state == null)
                return;
            Piece piece = _state.Board.GetPiece(from);
            if (piece == null || !_pieces.TryGetValue(piece.Id, out PieceView view) || view == null)
                return;
            Vector3 peak = _layout.SquareCenterLocal(toward, _viewer);
            if (view.PlayDeflect(peak, AnimationPrefs.MoveDuration(0.22f), OnPieceMotionEnded))
                _movingCount++;
        }
        public void PlayCheck(Guid kingId)
        {
            if (!_pieces.TryGetValue(kingId, out PieceView view) || view == null)
                return;
            view.PlayShiver();
            BoardCamera.AddTrauma(CaptureTrauma.Check);
        }
        public void PlayMateClear(Side defeated, Action onCleared)
        {
            PlayKnockOffSide(defeated, onCleared);
        }
        public void PlayKnockOffSide(Side side, Action onCleared)
        {
            EnsureBuilt();
            HoldMatchChrome();
            if (_movingCount > 0)
                _idleOnce += () => KnockOffDefeated(side, onCleared);
            else
                KnockOffDefeated(side, onCleared);
        }
        public void AnimatePiecesToSquares(IReadOnlyDictionary<Guid, Square> destinations, Action onComplete)
        {
            EnsureBuilt();
            if (destinations == null || destinations.Count == 0 || AnimationPrefs.Instant)
            {
                onComplete?.Invoke();
                return;
            }
            float duration = AnimationPrefs.MoveDuration(0.45f);
            int started = 0;
            foreach (KeyValuePair<Guid, Square> pair in destinations)
            {
                if (!_pieces.TryGetValue(pair.Key, out PieceView view) || view == null)
                    continue;
                Vector3 dest = _layout.SquareCenterLocal(pair.Value, _viewer);
                if (view.PlayMove(dest, duration, OnPieceMotionEnded))
                {
                    _movingCount++;
                    started++;
                }
                else
                    view.SnapTo(dest);
            }
            if (started == 0)
            {
                onComplete?.Invoke();
                return;
            }
            _idleOnce += () => onComplete?.Invoke();
        }
        public void PlayPowerFeel(MartyrPower power, Guid? targetId)
        {
            if (_state == null || AnimationPrefs.Instant)
                return;
            Side side = _state.SideToMove;
            switch (power)
            {
                case MartyrPower.Reinforcements:
                case MartyrPower.Revival:
                    if (PiecesBusy)
                        HoldMatchChrome();
                    break;
                case MartyrPower.FleetPawns:
                    PopSide(side, PieceType.Pawn, 0.16f);
                    break;
                case MartyrPower.Bombard:
                    BoardCamera.AddTrauma(CaptureTrauma.Minor);
                    break;
                case MartyrPower.UntouchableKing:
                    PlayKingFeel(side);
                    break;
                case MartyrPower.StasisField:
                    PlayTargetShiver(targetId);
                    break;
                case MartyrPower.KnightAscension:
                    PopSide(side, PieceType.Knight, 0.18f);
                    break;
                case MartyrPower.BattlefieldPromotion:
                    PlayTargetPop(targetId, 0.18f);
                    break;
                case MartyrPower.Rally:
                    BoardCamera.AddTrauma(CaptureTrauma.Pawn);
                    PlayKingFeel(side);
                    break;
                case MartyrPower.Exile:
                    PlayExileFeel(targetId);
                    break;
                case MartyrPower.Phalanx:
                    PopSide(side, PieceType.Pawn, 0.22f);
                    if (PiecesBusy)
                        HoldMatchChrome();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(power), power, null);
            }
        }

        public bool TryPickSquare(Vector3 worldPoint, out Square square)
        {
            EnsureBuilt();
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            if (!_layout.TryGetSquare(local, out square, _viewer))
                return false;
            if (IsCovered(square))
            {
                square = default;
                return false;
            }

            return true;
        }

        public Bounds GetWorldBounds()
        {
            EnsureBuilt();
            var bounds = new Bounds(transform.TransformPoint(Vector3.zero), Vector3.zero);
            bounds.Encapsulate(transform.TransformPoint(Vector3.zero));
            bounds.Encapsulate(transform.TransformPoint(_layout.BoardSizeLocal));
            bounds.Encapsulate(transform.TransformPoint(new Vector3(_layout.BoardSizeLocal.x, 0f, 0f)));
            bounds.Encapsulate(transform.TransformPoint(new Vector3(0f, _layout.BoardSizeLocal.y, 0f)));
            float tray = _layout.SquareSize * 2f;
            bounds.Encapsulate(transform.TransformPoint(new Vector3(-tray, 0f, 0f)));
            bounds.Encapsulate(transform.TransformPoint(new Vector3(_layout.BoardSizeLocal.x + tray, _layout.BoardSizeLocal.y, 0f)));
            return bounds;
        }

        public void NotifySquarePressed(Square square)
        {
            EnsureBuilt();
            if (!square.IsOnBoard || IsCovered(square))
                return;
            SquarePressed?.Invoke(square);
        }
        public void NotifySquareReleased(Square? square)
        {
            SquareReleased?.Invoke(square);
        }
        public void NotifySquareClicked(Square square)
        {
            if (_state == null || !square.IsOnBoard || IsCovered(square))
                return;
            if (_state.Status != GameStatus.InProgress && !allowSelectionWhenFinished)
                return;

            GameState stateBefore = _state;
            Square? selectedBefore = _selected;
            SquareClicked?.Invoke(square);

            if (_targeting)
                return;
            if (_state == stateBefore && _selected == selectedBefore)
                ApplyLocalSelection(square);
        }

        public void NotifySquareHovered(Square? square)
        {
            if (square.HasValue && (!square.Value.IsOnBoard || IsCovered(square.Value)))
                square = null;
            if (_hovered == square)
                return;
            _hovered = square;
            SquareHovered?.Invoke(square);
        }

        void EnsureTheme()
        {
            if (!theme.IsConfigured)
                theme = BoardTheme.Default;
        }

        void EnsureBuilt()
        {
            if (!_built)
                Build();
        }

        void Build()
        {
            EnsureTheme();
            _layout = new BoardLayout(squareSize, captureSpacing);
            EnsureRoots();

            for (int file = 0; file < BoardLayout.FileCount; file++)
            {
                for (int rank = 0; rank < BoardLayout.RankCount; rank++)
                {
                    int index = Index(file, rank);
                    if (_squares[index] == null)
                        _squares[index] = CreateSquare(new Square(file, rank));
                }
            }

            _built = true;
        }

        void Relayout()
        {
            if (!_built)
                return;

            for (int file = 0; file < BoardLayout.FileCount; file++)
            {
                for (int rank = 0; rank < BoardLayout.RankCount; rank++)
                {
                    var square = new Square(file, rank);
                    SquareView view = SquareAt(square);
                    if (view != null)
                        view.transform.localPosition = _layout.SquareCenterLocal(square, _viewer);
                }
            }
        }

        void EnsureRoots()
        {
            if (_squaresRoot == null)
            {
                var go = new GameObject("Squares");
                _squaresRoot = go.transform;
                _squaresRoot.SetParent(transform, false);
                _squaresRoot.localPosition = Vector3.zero;
                _squaresRoot.localRotation = Quaternion.identity;
            }

            if (_piecesRoot == null)
            {
                var go = new GameObject("Pieces");
                _piecesRoot = go.transform;
                _piecesRoot.SetParent(transform, false);
                _piecesRoot.localPosition = Vector3.zero;
                _piecesRoot.localRotation = Quaternion.identity;
            }
        }

        SquareView CreateSquare(Square square)
        {
            bool light = (square.File + square.Rank) % 2 != 0;
            SquareView prefab = ResolveSquarePrefab();
            SquareView view;
            if (prefab != null)
            {
                view = Instantiate(prefab, _squaresRoot);
            }
            else
            {
                var created = new GameObject();
                created.transform.SetParent(_squaresRoot, false);
                view = created.AddComponent<SquareView>();
            }

            view.gameObject.name = $"{(char)('a' + square.File)}{square.Rank + 1}";
            Transform t = view.transform;
            t.localPosition = _layout.SquareCenterLocal(square, _viewer);
            t.localRotation = Quaternion.identity;
            t.localScale = Vector3.one;
            view.Initialize(
                square,
                _layout.SquareSize,
                light ? theme.LightSquare : theme.DarkSquare,
                theme);
            return view;
        }

        void SyncPieces()
        {
            _seenIds.Clear();
            bool snapAll = _pieceLayoutViewer != _viewer;
            _pieceLayoutViewer = _viewer;
            int started = 0;

            for (int file = 0; file < BoardLayout.FileCount; file++)
            {
                for (int rank = 0; rank < BoardLayout.RankCount; rank++)
                {
                    var square = new Square(file, rank);
                    Piece piece = _state.Board.GetPiece(square);
                    if (piece == null)
                        continue;
                    if (_banished.Contains(piece.Id))
                    {
                        _seenIds.Add(piece.Id);
                        continue;
                    }

                    SquareSight sight = _vision[square];
                    if (sight == SquareSight.Hidden && piece.Side != _viewer && !ReviewVision)
                        continue;

                    _seenIds.Add(piece.Id);
                    bool created = false;
                    if (!_pieces.TryGetValue(piece.Id, out PieceView view))
                    {
                        view = CreatePieceView();
                        _pieces.Add(piece.Id, view);
                        created = true;
                    }

                    bool wasShadow = view.IsShadow;
                    bool shadow = !ReviewVision && sight == SquareSight.Shadow && piece.Side != _viewer;
                    bool identified = ReviewVision || sight == SquareSight.Identified || piece.Side == _viewer;
                    if (shadow)
                    {
                        view.BindShadow(_layout.SquareSize, theme);
                    }
                    else
                    {
                        view.Bind(piece, _layout.SquareSize, theme);
                        view.SetPopupState(_state);
                        bool empowered = identified
                            && (_state.Runtime.IsEmpowered(piece.Id) || _pendingEmpowered.Contains(piece.Id));
                        view.SetEmpoweredAura(empowered, piece.Side == _viewer);
                    }
                    view.SetGhosted(!shadow && _state.Runtime.HasStatus(piece.Id, StatusKind.Stasis));
                    Vector3 dest = _layout.SquareCenterLocal(square, _viewer);
                    view.gameObject.SetActive(true);
                    bool summoned = identified && !shadow && _state.Runtime.IsSummoned(piece.Id);
                    bool entry = created && summoned && !snapAll && !AnimationPrefs.Instant;
                    bool animate = !snapAll
                        && !created
                        && !shadow
                        && !wasShadow
                        && identified
                        && !AnimationPrefs.Instant
                        && (view.transform.localPosition - dest).sqrMagnitude > 0.0001f;
                    if (entry)
                    {
                        view.SnapTo(EntryStartLocal(dest, piece.Side == _viewer));
                        float duration = AnimationPrefs.MoveDuration(0.55f);
                        if (view.PlayMove(dest, duration, OnPieceMotionEnded))
                        {
                            HoldMatchChrome();
                            _movingCount++;
                            started++;
                        }
                        else
                        {
                            view.SnapTo(dest);
                        }
                    }
                    else if (animate)
                    {
                        float chebyshev = ChebyshevFromLocal(view.transform.localPosition, dest);
                        float duration = AnimationPrefs.MoveDuration(0.32f + 0.06f * chebyshev);
                        if (view.PlayMove(dest, duration, OnPieceMotionEnded))
                        {
                            _movingCount++;
                            started++;
                        }
                    }
                    else
                    {
                        view.SnapTo(dest);
                    }
                }
            }
            LayoutCaptures(snapAll, ref started);
            if (_pieces.Count == _seenIds.Count)
                return;

            _staleIds.Clear();
            foreach (KeyValuePair<Guid, PieceView> pair in _pieces)
            {
                if (!_seenIds.Contains(pair.Key))
                    _staleIds.Add(pair.Key);
            }

            bool defer = started > 0 || _movingCount > 0;
            for (int i = 0; i < _staleIds.Count; i++)
            {
                Guid id = _staleIds[i];
                PieceView view = _pieces[id];
                _pieces.Remove(id);
                if (view == null)
                    continue;
                view.gameObject.SetActive(false);
                if (defer)
                    _deferredDestroy.Add(view);
                else
                    Destroy(view.gameObject);
            }
        }

        void OnPieceMotionEnded()
        {
            _movingCount = Mathf.Max(0, _movingCount - 1);
            if (_movingCount == 0)
                FinishIdle();
        }

        void FlushDeferred()
        {
            for (int i = 0; i < _deferredDestroy.Count; i++)
            {
                if (_deferredDestroy[i] != null)
                    Destroy(_deferredDestroy[i].gameObject);
            }

            _deferredDestroy.Clear();
        }
        void FinishIdle()
        {
            FlushDeferred();
            Action idle = _idleOnce;
            _idleOnce = null;
            if (idle != null)
            {
                idle.Invoke();
                return;
            }
            if (_chromeUntilIdle)
            {
                _chromeUntilIdle = false;
                SetMatchChromeHidden(false);
            }
        }
        void KnockOffDefeated(Side defeated, Action onCleared)
        {
            BoardCamera.AddTrauma(CaptureTrauma.Mate);
            Vector3 origin = _layout.BoardCenterLocal;
            for (int i = 0; i < 64; i++)
            {
                Piece king = _state.Board.GetPiece(Square.FromIndex(i));
                if (king == null || king.Side != defeated || king.Type != PieceType.King)
                    continue;
                if (_pieces.TryGetValue(king.Id, out PieceView kingView) && kingView != null)
                    origin = kingView.transform.localPosition;
                break;
            }
            Rect board = new Rect(0f, 0f, _layout.BoardSizeLocal.x, _layout.BoardSizeLocal.y);
            float gravity = _layout.SquareSize * 22f;
            float duration = AnimationPrefs.MoveDuration(1.2f);
            int started = 0;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = _state.Board.GetPiece(Square.FromIndex(i));
                if (piece == null || piece.Side != defeated)
                    continue;
                if (!_pieces.TryGetValue(piece.Id, out PieceView view) || view == null)
                    continue;
                _banished.Add(piece.Id);
                Vector3 dir = view.transform.localPosition - origin;
                dir.z = 0f;
                if (dir.sqrMagnitude < 0.0001f)
                    dir = new Vector3(0f, -1f, 0f);
                else
                    dir.Normalize();
                int hash = piece.Id.GetHashCode();
                float wobble = ((hash & 1023) / 1023f - 0.5f) * 40f;
                dir = Quaternion.Euler(0f, 0f, wobble) * dir;
                float speed = _layout.SquareSize * (8.5f + (hash & 7) * 0.55f);
                Vector3 vel = dir * speed;
                vel.y += _layout.SquareSize * 4f;
                float delay = AnimationPrefs.MoveDuration(0.045f * started);
                if (view.PlayKnockOff(vel, board, gravity, duration, delay, OnPieceMotionEnded))
                {
                    _movingCount++;
                    started++;
                }
            }
            if (started == 0)
            {
                _chromeUntilIdle = false;
                SetMatchChromeHidden(false);
                onCleared?.Invoke();
                return;
            }
            _idleOnce += () =>
            {
                _chromeUntilIdle = false;
                SetMatchChromeHidden(false);
                onCleared?.Invoke();
            };
        }
        void HoldMatchChrome()
        {
            _chromeUntilIdle = true;
            SetMatchChromeHidden(true);
        }
        void SetMatchChromeHidden(bool hidden)
        {
            if (HidingMatchChrome == hidden)
                return;
            HidingMatchChrome = hidden;
            MatchChromeHidden?.Invoke(hidden);
        }
        Vector3 EntryStartLocal(Vector3 dest, bool mine)
        {
            float pad = _layout.SquareSize * 3f;
            float x = mine ? _layout.BoardSizeLocal.x + pad : -pad;
            return new Vector3(x, dest.y, dest.z);
        }
        Vector3 EntryStartVertical(Vector3 dest, bool fromTop)
        {
            float pad = _layout.SquareSize * 4f;
            float y = fromTop ? _layout.BoardSizeLocal.y + pad : -pad;
            return new Vector3(dest.x, y, dest.z);
        }
        void CacheRestLocal()
        {
            if (_restLocalCached)
                return;
            _restLocal = transform.localPosition;
            _restLocalCached = true;
        }
        void SyncPiecesForStage(HashSet<Guid> fromTop, HashSet<Guid> fromBottom)
        {
            EnsureTheme();
            _seenIds.Clear();
            for (int file = 0; file < BoardLayout.FileCount; file++)
            {
                for (int rank = 0; rank < BoardLayout.RankCount; rank++)
                {
                    Square square = new Square(file, rank);
                    Piece piece = _state.Board.GetPiece(square);
                    if (piece == null)
                        continue;
                    _seenIds.Add(piece.Id);
                    PieceView view = CreatePieceView();
                    _pieces[piece.Id] = view;
                    view.Bind(piece, _layout.SquareSize, theme);
                    view.SetEmpoweredAura(false, false);
                    view.SetGhosted(false);
                    Vector3 dest = _layout.SquareCenterLocal(square, _viewer);
                    view.gameObject.SetActive(true);
                    if (_stageSnapIds.Contains(piece.Id) || AnimationPrefs.Instant)
                    {
                        view.SnapTo(dest);
                        continue;
                    }
                    bool top = fromTop.Contains(piece.Id);
                    bool bottom = fromBottom.Contains(piece.Id);
                    if (!top && !bottom)
                    {
                        view.SnapTo(dest);
                        continue;
                    }
                    view.SnapTo(EntryStartVertical(dest, top));
                    _stageEntries.Add((piece.Id, dest, top));
                }
            }
            int started = 0;
            LayoutCaptures(true, ref started);
        }
        void StartStageStagger()
        {
            if (_stageEntries.Count == 0 || AnimationPrefs.Instant)
            {
                FinishStageReveal();
                return;
            }
            var ordered = new List<(Guid id, Vector3 dest, bool fromTop)>(_stageEntries.Count);
            for (int i = 0; i < _stageEntries.Count; i++)
            {
                if (_stageEntries[i].fromTop)
                    ordered.Add(_stageEntries[i]);
            }
            for (int i = 0; i < _stageEntries.Count; i++)
            {
                if (!_stageEntries[i].fromTop)
                    ordered.Add(_stageEntries[i]);
            }
            _stageEntries.Clear();
            float duration = AnimationPrefs.MoveDuration(_stagePieceDuration);
            float stagger = AnimationPrefs.MoveDuration(_stageStagger);
            int started = 0;
            for (int i = 0; i < ordered.Count; i++)
            {
                if (!_pieces.TryGetValue(ordered[i].id, out PieceView view) || view == null)
                    continue;
                float delay = stagger * started;
                if (view.PlayMove(ordered[i].dest, duration, delay, OnPieceMotionEnded))
                {
                    _movingCount++;
                    started++;
                }
            }
            if (started == 0)
            {
                FinishStageReveal();
                return;
            }
            _idleOnce += FinishStageReveal;
        }
        void FinishStageReveal()
        {
            _stageReveal = false;
            Action done = _stageRevealDone;
            _stageRevealDone = null;
            done?.Invoke();
        }
        void PopSide(Side side, PieceType type, float duration)
        {
            if (_state == null)
                return;
            for (int i = 0; i < 64; i++)
            {
                Square square = Square.FromIndex(i);
                Piece piece = _state.Board.GetPiece(square);
                if (piece == null || piece.Side != side || piece.Type != type)
                    continue;
                if (!_pieces.TryGetValue(piece.Id, out PieceView view) || view == null)
                    continue;
                if (view.PlayPop(duration, OnPieceMotionEnded))
                    _movingCount++;
            }
        }
        void PlayKingFeel(Side side)
        {
            Guid? kingId = KingId(side);
            if (kingId == null || !_pieces.TryGetValue(kingId.Value, out PieceView view) || view == null)
                return;
            view.PlayShiver();
        }
        void PlayTargetShiver(Guid? targetId)
        {
            if (targetId == null || !_pieces.TryGetValue(targetId.Value, out PieceView view) || view == null)
                return;
            view.PlayShiver();
        }
        void PlayTargetPop(Guid? targetId, float duration)
        {
            if (targetId == null || !_pieces.TryGetValue(targetId.Value, out PieceView view) || view == null)
                return;
            if (view.PlayPop(duration, OnPieceMotionEnded))
                _movingCount++;
        }
        void PlayExileFeel(Guid? targetId)
        {
            if (targetId == null)
                return;
            IReadOnlyList<CaptureRecord> captures = _state.Runtime.Captures;
            for (int i = 0; i < captures.Count; i++)
            {
                if (captures[i].Id != targetId.Value)
                    continue;
                BoardCamera.AddTrauma(CaptureTrauma.For(captures[i].Type));
                return;
            }
        }
        Guid? KingId(Side side)
        {
            if (_state == null)
                return null;
            for (int i = 0; i < 64; i++)
            {
                Piece piece = _state.Board.GetPiece(Square.FromIndex(i));
                if (piece != null && piece.Side == side && piece.Type == PieceType.King)
                    return piece.Id;
            }
            return null;
        }
        void LayoutCaptures(bool snapAll, ref int started)
        {
            IReadOnlyList<CaptureRecord> captures = _state.Runtime.Captures;
            int playerIndex = 0;
            int opponentIndex = 0;
            for (int i = 0; i < captures.Count; i++)
            {
                CaptureRecord record = captures[i];
                _seenIds.Add(record.Id);
                bool created = false;
                if (!_pieces.TryGetValue(record.Id, out PieceView view))
                {
                    view = CreatePieceView();
                    _pieces.Add(record.Id, view);
                    created = true;
                }
                view.BindCaptured(record.Id, record.Type, record.Side, theme);
                view.SetEmpoweredAura(false, false);
                view.SetGhosted(record.Exiled);
                int index = record.Side == _viewer ? playerIndex++ : opponentIndex++;
                Vector3 dest = _layout.CaptureSlotLocal(record.Side == _viewer, index);
                view.gameObject.SetActive(true);
                bool animate = !snapAll
                    && !created
                    && !AnimationPrefs.Instant
                    && (view.transform.localPosition - dest).sqrMagnitude > 0.0001f;
                if (animate)
                {
                    float duration = AnimationPrefs.MoveDuration(0.42f);
                    float height = _layout.SquareSize * 0.75f;
                    if (view.PlayCaptureDepart(dest, duration, height, OnPieceMotionEnded))
                    {
                        _movingCount++;
                        started++;
                    }
                }
                else
                {
                    view.SnapTo(dest);
                }
            }
        }

        float ChebyshevFromLocal(Vector3 from, Vector3 to)
        {
            float dx = Mathf.Abs(to.x - from.x) / _layout.SquareSize;
            float dy = Mathf.Abs(to.y - from.y) / _layout.SquareSize;
            return Mathf.Max(1f, Mathf.Max(dx, dy));
        }

        PieceView CreatePieceView()
        {
            PieceView prefab = ResolvePiecePrefab();
            PieceView view;
            if (prefab != null)
            {
                view = Instantiate(prefab, _piecesRoot);
            }
            else
            {
                var go = new GameObject("ChessPiece");
                go.transform.SetParent(_piecesRoot, false);
                view = go.AddComponent<PieceView>();
            }

            view.gameObject.SetActive(true);
            Transform t = view.transform;
            t.localPosition = new Vector3(-_layout.SquareSize * 3f, -_layout.SquareSize, 0f);
            t.localRotation = Quaternion.identity;
            return view;
        }

        PieceView ResolvePiecePrefab()
        {
            if (piecePrefab != null)
                return piecePrefab;

            GameObject loaded = RuntimePrefabs.ChessPiece;
            if (loaded == null)
                return null;

            piecePrefab = loaded.GetComponent<PieceView>();
            return piecePrefab;
        }

        static PieceView LoadDefaultPiecePrefab()
        {
            GameObject loaded = RuntimePrefabs.ChessPiece;
            return loaded != null ? loaded.GetComponent<PieceView>() : null;
        }

        SquareView ResolveSquarePrefab()
        {
            if (squarePrefab != null)
                return squarePrefab;

            GameObject loaded = RuntimePrefabs.ChessboardTile;
            if (loaded == null)
                return null;

            squarePrefab = loaded.GetComponent<SquareView>();
            return squarePrefab;
        }

        static SquareView LoadDefaultSquarePrefab()
        {
            GameObject loaded = RuntimePrefabs.ChessboardTile;
            return loaded != null ? loaded.GetComponent<SquareView>() : null;
        }

        bool IsCovered(Square square)
        {
            return _targeting && !_validTargets.Contains(square);
        }

        void ApplySelectedOutlines()
        {
            Guid? selectedId = null;
            if (_selected.HasValue && _state != null)
            {
                Piece piece = _state.Board.GetPiece(_selected.Value);
                if (piece != null)
                    selectedId = piece.Id;
            }

            foreach (KeyValuePair<Guid, PieceView> pair in _pieces)
            {
                if (pair.Value != null)
                    pair.Value.SetSelectedOutline(selectedId.HasValue && pair.Key == selectedId.Value);
            }
            if (selectedId != _selectPopId)
            {
                _selectPopId = selectedId;
                if (selectedId.HasValue
                    && _pieces.TryGetValue(selectedId.Value, out PieceView selected)
                    && selected != null
                    && !selected.IsMoving)
                    selected.PlaySelectPop();
            }
        }

        void OnDisable()
        {
            if (!Application.isPlaying)
                return;
            CompleteMotion();
        }

        void PruneSelectionAfterBind()
        {
            if (!_selected.HasValue || _state == null)
                return;

            Piece piece = _state.Board.GetPiece(_selected.Value);
            if (piece == null)
            {
                _selected = null;
                return;
            }

            if (_state.Status == GameStatus.InProgress && piece.Side != _state.SideToMove)
                _selected = null;
        }

        void ApplyLocalSelection(Square square)
        {
            if (_state == null)
                return;

            if (_selected.HasValue && IsLegalDestination(_selected.Value, square))
                return;

            Piece piece = _state.Board.GetPiece(square);
            if (piece != null && piece.Side == _state.SideToMove)
                SetSelection(square);
            else
                ClearSelection();
        }

        bool IsLegalDestination(Square from, Square to)
        {
            IReadOnlyList<Move> moves = _state.LegalMovesFrom(from);
            if (moves == null)
                return false;

            for (int i = 0; i < moves.Count; i++)
            {
                Square dest = moves[i].To;
                if (dest.File == to.File && dest.Rank == to.Rank)
                    return true;
            }

            return false;
        }

        void RefreshHighlights()
        {
            if (!_built)
                return;

            for (int i = 0; i < _squares.Length; i++)
            {
                if (_squares[i] != null)
                    _squares[i].ClearMarkers();
            }

            if (_lastFrom.HasValue && _vision.IsIdentified(_lastFrom.Value))
                SquareAt(_lastFrom.Value)?.SetLastMove(true);
            if (_lastTo.HasValue && _vision.IsIdentified(_lastTo.Value))
                SquareAt(_lastTo.Value)?.SetLastMove(true);

            for (int file = 0; file < BoardLayout.FileCount; file++)
            {
                for (int rank = 0; rank < BoardLayout.RankCount; rank++)
                {
                    var square = new Square(file, rank);
                    SquareView view = SquareAt(square);
                    if (view == null)
                        continue;
                    view.SetHidden(_vision[square] == SquareSight.Hidden);
                    view.SetCovered(IsCovered(square));
                }
            }

            ApplySelectedOutlines();

            if (!_selected.HasValue)
                return;

            SquareAt(_selected.Value)?.SetSelected(true);
            if (_state == null)
                return;

            IReadOnlyList<Move> moves = _state.LegalMovesFrom(_selected.Value);
            if (moves == null)
                return;

            for (int i = 0; i < moves.Count; i++)
                SquareAt(moves[i].To)?.SetLegal(true);
        }

        SquareView SquareAt(Square square)
        {
            if (!square.IsOnBoard)
                return null;
            return _squares[Index(square.File, square.Rank)];
        }

        static int Index(int file, int rank)
        {
            return file * BoardLayout.RankCount + rank;
        }

        void OnDrawGizmosSelected()
        {
            BoardLayout layout = Application.isPlaying && _built ? _layout : new BoardLayout(squareSize, captureSpacing);
            Vector3 center = transform.TransformPoint(layout.BoardCenterLocal);
            Vector3 size = transform.TransformVector(layout.BoardSizeLocal);
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawWireCube(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 0.05f));
        }
    }
}
