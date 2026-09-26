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
        SpriteRenderer _landmine;
        SpriteRenderer _terrainSprite;
        BoxCollider2D _collider;
        BoardTheme _theme;
        Color _squareColor;
        TerrainKind _terrain;
        TerrainSpriteCatalog _terrainCatalog;
        Sprite _pickedTerrain;
        bool _lastMove;
        bool _selected;
        bool _legal;
        bool _hidden;
        bool _covered;

        public Square Square { get; private set; }
        public bool IsCovered => _covered;

        public void Initialize(
            Square square,
            float size,
            Color squareColor,
            BoardTheme theme,
            TerrainSpriteCatalog terrainCatalog = null)
        {
            Square = square;
            _theme = theme;
            _squareColor = squareColor;
            _terrainCatalog = terrainCatalog != null ? terrainCatalog : TerrainSpriteCatalog.Load();
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
            _landmine = FindRenderer("Landmine") ?? CreateRenderer("Landmine", BoardRenderOrder.Legal);
            _landmine.enabled = false;
            _terrainSprite = FindRenderer("Terrain") ?? CreateRenderer("Terrain", BoardRenderOrder.Terrain);
            _terrainSprite.enabled = false;
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

        public void SetTerrain(TerrainKind kind)
        {
            if (_terrain == kind)
                return;
            _terrain = kind;
            _pickedTerrain = kind == TerrainKind.None || _terrainCatalog == null
                ? null
                : _terrainCatalog.RandomSprite(kind);
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
        public void SetLandmine(Sprite sprite, bool visible)
        {
            if (_landmine == null)
                return;
            _landmine.sprite = sprite;
            if (!visible || sprite == null)
            {
                _landmine.enabled = false;
                return;
            }
            float size = _base != null ? _base.transform.localScale.x : 1f;
            float world = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float scale = world > 0.001f ? size * 0.72f / world : size * 0.55f;
            _landmine.transform.localScale = new Vector3(scale, scale, 1f);
            _landmine.color = new Color(1f, 1f, 1f, 0.4f);
            _landmine.enabled = true;
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
                Color color = _pickedTerrain == null ? TerrainColor(_squareColor, _terrain) : _squareColor;
                if (_hidden)
                    color = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
                _base.color = color;
            }
            ApplyTerrainSprite();
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
        void ApplyTerrainSprite()
        {
            if (_terrainSprite == null)
                return;
            Sprite sprite = _pickedTerrain;
            if (sprite == null)
            {
                _terrainSprite.enabled = false;
                return;
            }
            _terrainSprite.sprite = sprite;
            float size = _base != null ? _base.transform.localScale.x : 1f;
            float world = Mathf.Max(sprite.bounds.size.x, sprite.bounds.size.y);
            float scale = world > 0.001f ? size / world : size;
            _terrainSprite.transform.localScale = new Vector3(scale, scale, 1f);
            float alpha = _hidden ? 0.45f : 1f;
            _terrainSprite.color = new Color(1f, 1f, 1f, alpha);
            _terrainSprite.enabled = true;
        }
        static Color TerrainColor(Color square, TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Swamp:
                    return Color.Lerp(square, new Color(0.28f, 0.42f, 0.22f, 1f), 0.55f);
                case TerrainKind.Forest:
                    return Color.Lerp(square, new Color(0.12f, 0.38f, 0.16f, 1f), 0.5f);
                case TerrainKind.Mountain:
                    return Color.Lerp(square, new Color(0.45f, 0.45f, 0.48f, 1f), 0.7f);
                default:
                    return square;
            }
        }
    }
}
