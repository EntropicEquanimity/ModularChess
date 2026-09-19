using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class SquareView : MonoBehaviour
    {
        SpriteRenderer _base;
        SpriteRenderer _overlay;
        SpriteRenderer _marker;
        SpriteRenderer _cover;
        BoxCollider2D _collider;
        BoardTheme _theme;
        Color _squareColor;
        bool _lastMove;
        bool _selected;
        bool _legal;
        bool _hidden;
        bool _covered;

        public Square Square { get; private set; }
        public bool IsCovered => _covered;

        public void Initialize(Square square, float size, Color squareColor, BoardTheme theme)
        {
            Square = square;
            _theme = theme;
            _squareColor = squareColor;

            _base = FindRenderer("Base") ?? CreateRenderer("Base", BoardRenderOrder.Square);
            if (_base.sprite == null)
                _base.sprite = RuntimeSprites.Pixel;
            _base.color = squareColor;
            _base.transform.localScale = new Vector3(size, size, 1f);

            _overlay = FindRenderer("Overlay") ?? CreateRenderer("Overlay", BoardRenderOrder.LastMove);
            if (_overlay.sprite == null)
                _overlay.sprite = RuntimeSprites.Pixel;
            _overlay.transform.localScale = new Vector3(size, size, 1f);
            _overlay.enabled = false;

            _marker = FindRenderer("Marker") ?? CreateRenderer("Marker", BoardRenderOrder.Legal);
            _marker.sprite = RuntimeSprites.Circle;
            _marker.transform.localScale = new Vector3(size * 0.32f, size * 0.32f, 1f);
            _marker.enabled = false;

            _cover = FindRenderer("Cover") ?? CreateRenderer("Cover", BoardRenderOrder.Cover);
            if (_cover.sprite == null)
                _cover.sprite = RuntimeSprites.Pixel;
            _cover.transform.localScale = new Vector3(size, size, 1f);
            _cover.sortingOrder = BoardRenderOrder.Cover;
            if (_cover.color.a <= 0f)
                _cover.color = new Color(0f, 0f, 0f, 0.7f);

            _collider = gameObject.GetComponent<BoxCollider2D>();
            if (_collider == null)
                _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.size = new Vector2(size, size);
            _collider.isTrigger = true;

            ApplyCover();
            ApplyMarkers();
        }

        public void ClearMarkers()
        {
            _lastMove = false;
            _selected = false;
            _legal = false;
            _hidden = false;
            ApplyMarkers();
        }

        public void SetLastMove(bool value)
        {
            _lastMove = value;
            ApplyMarkers();
        }

        public void SetSelected(bool value)
        {
            _selected = value;
            ApplyMarkers();
        }

        public void SetLegal(bool value)
        {
            _legal = value;
            ApplyMarkers();
        }

        public void SetHidden(bool value)
        {
            _hidden = value;
            ApplyMarkers();
        }

        public void SetCovered(bool value)
        {
            if (_covered == value)
                return;
            _covered = value;
            ApplyCover();
        }

        SpriteRenderer FindRenderer(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<SpriteRenderer>() : null;
        }

        SpriteRenderer CreateRenderer(string childName, int order)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            var spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sortingOrder = order;
            return spriteRenderer;
        }

        void ApplyCover()
        {
            if (_cover != null)
                _cover.enabled = _covered;
            if (_collider != null)
                _collider.enabled = !_covered;
        }

        void ApplyMarkers()
        {
            if (_base != null)
            {
                _base.color = _hidden
                    ? new Color(_squareColor.r * 0.45f, _squareColor.g * 0.45f, _squareColor.b * 0.45f, 1f)
                    : _squareColor;
            }

            if (_overlay == null)
                return;

            if (_selected)
            {
                _overlay.enabled = true;
                _overlay.color = _theme.Selected;
                _overlay.sortingOrder = BoardRenderOrder.Selected;
            }
            else if (_lastMove)
            {
                _overlay.enabled = true;
                _overlay.color = _theme.LastMove;
                _overlay.sortingOrder = BoardRenderOrder.LastMove;
            }
            else
            {
                _overlay.enabled = false;
            }

            if (_marker == null)
                return;

            _marker.enabled = _legal;
            if (_legal)
                _marker.color = _theme.LegalMove;
        }
    }
}
