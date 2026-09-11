using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class SquareView : MonoBehaviour
    {
        SpriteRenderer _base;
        SpriteRenderer _overlay;
        SpriteRenderer _marker;
        BoardTheme _theme;
        Color _squareColor;
        bool _lastMove;
        bool _selected;
        bool _legal;
        bool _hidden;

        public Square Square { get; private set; }

        public void Initialize(Square square, float size, Color squareColor, BoardTheme theme)
        {
            Square = square;
            _theme = theme;
            _squareColor = squareColor;

            _base = CreateRenderer("Base", BoardRenderOrder.Square);
            _base.sprite = RuntimeSprites.Pixel;
            _base.color = squareColor;
            _base.transform.localScale = new Vector3(size, size, 1f);

            _overlay = CreateRenderer("Overlay", BoardRenderOrder.LastMove);
            _overlay.sprite = RuntimeSprites.Pixel;
            _overlay.transform.localScale = new Vector3(size, size, 1f);
            _overlay.enabled = false;

            _marker = CreateRenderer("Marker", BoardRenderOrder.Legal);
            _marker.sprite = RuntimeSprites.Circle;
            _marker.transform.localScale = new Vector3(size * 0.32f, size * 0.32f, 1f);
            _marker.enabled = false;

            var collider = gameObject.GetComponent<BoxCollider2D>();
            if (collider == null)
                collider = gameObject.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(size, size);
            collider.isTrigger = true;

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
