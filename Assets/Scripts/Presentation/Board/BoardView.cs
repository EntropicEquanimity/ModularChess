using System;
using System.Collections.Generic;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class BoardView : MonoBehaviour, IBoardView
    {
        public event Action<Square> SquareClicked;

        [SerializeField] float squareSize = 1f;
        [SerializeField] BoardTheme theme;
        [SerializeField] bool allowSelectionWhenFinished;
        [SerializeField] bool buildOnAwake = true;

        readonly Dictionary<Guid, PieceView> _pieces = new Dictionary<Guid, PieceView>();
        readonly SquareView[] _squares = new SquareView[BoardLayout.FileCount * BoardLayout.RankCount];
        readonly HashSet<Guid> _seenIds = new HashSet<Guid>();
        readonly List<Guid> _staleIds = new List<Guid>();

        Transform _squaresRoot;
        Transform _piecesRoot;
        BoardLayout _layout;
        GameState _state;
        VisionMap _vision = VisionMap.AllIdentified;
        Side _viewer = Side.White;
        Square? _selected;
        Square? _lastFrom;
        Square? _lastTo;
        bool _built;

        public GameState BoundState => _state;
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
        public BoardLayout Layout => _built ? _layout : new BoardLayout(squareSize);

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
        }

        void Awake()
        {
            EnsureTheme();
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

        public bool TryPickSquare(Vector3 worldPoint, out Square square)
        {
            EnsureBuilt();
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            return _layout.TryGetSquare(local, out square, _viewer);
        }

        public Bounds GetWorldBounds()
        {
            EnsureBuilt();
            var bounds = new Bounds(transform.TransformPoint(Vector3.zero), Vector3.zero);
            bounds.Encapsulate(transform.TransformPoint(Vector3.zero));
            bounds.Encapsulate(transform.TransformPoint(_layout.BoardSizeLocal));
            bounds.Encapsulate(transform.TransformPoint(new Vector3(_layout.BoardSizeLocal.x, 0f, 0f)));
            bounds.Encapsulate(transform.TransformPoint(new Vector3(0f, _layout.BoardSizeLocal.y, 0f)));
            return bounds;
        }

        public void NotifySquareClicked(Square square)
        {
            if (_state == null || !square.IsOnBoard)
                return;
            if (_state.Status != GameStatus.InProgress && !allowSelectionWhenFinished)
                return;

            GameState stateBefore = _state;
            Square? selectedBefore = _selected;
            SquareClicked?.Invoke(square);

            if (_state == stateBefore && _selected == selectedBefore)
                ApplyLocalSelection(square);
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
            _layout = new BoardLayout(squareSize);
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
            var go = new GameObject($"{(char)('a' + square.File)}{square.Rank + 1}");
            go.transform.SetParent(_squaresRoot, false);
            go.transform.localPosition = _layout.SquareCenterLocal(square, _viewer);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            var view = go.AddComponent<SquareView>();
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

            for (int file = 0; file < BoardLayout.FileCount; file++)
            {
                for (int rank = 0; rank < BoardLayout.RankCount; rank++)
                {
                    var square = new Square(file, rank);
                    Piece piece = _state.Board.GetPiece(square);
                    if (piece == null)
                        continue;

                    SquareSight sight = _vision[square];
                    if (sight == SquareSight.Hidden && piece.Side != _viewer)
                        continue;

                    _seenIds.Add(piece.Id);
                    if (!_pieces.TryGetValue(piece.Id, out PieceView view))
                    {
                        view = CreatePieceView();
                        _pieces.Add(piece.Id, view);
                    }

                    bool shadow = sight == SquareSight.Shadow && piece.Side != _viewer;
                    if (shadow)
                        view.BindShadow(_layout.SquareSize, theme);
                    else
                        view.Bind(piece, _layout.SquareSize, theme);
                    view.transform.localPosition = _layout.SquareCenterLocal(square, _viewer);
                    view.gameObject.SetActive(true);
                }
            }

            if (_pieces.Count == _seenIds.Count)
                return;

            _staleIds.Clear();
            foreach (KeyValuePair<Guid, PieceView> pair in _pieces)
            {
                if (!_seenIds.Contains(pair.Key))
                    _staleIds.Add(pair.Key);
            }

            for (int i = 0; i < _staleIds.Count; i++)
            {
                Guid id = _staleIds[i];
                PieceView view = _pieces[id];
                _pieces.Remove(id);
                if (view != null)
                    Destroy(view.gameObject);
            }
        }

        PieceView CreatePieceView()
        {
            var go = new GameObject("Piece");
            go.transform.SetParent(_piecesRoot, false);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            return go.AddComponent<PieceView>();
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
                    SquareAt(square)?.SetHidden(_vision[square] == SquareSight.Hidden);
                }
            }

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
            BoardLayout layout = Application.isPlaying && _built ? _layout : new BoardLayout(squareSize);
            Vector3 center = transform.TransformPoint(layout.BoardCenterLocal);
            Vector3 size = transform.TransformVector(layout.BoardSizeLocal);
            Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
            Gizmos.DrawWireCube(center, new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), 0.05f));
        }
    }
}
