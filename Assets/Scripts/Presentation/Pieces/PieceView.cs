using System;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class PieceView : MonoBehaviour
    {
        SpriteRenderer _outline;
        SpriteRenderer _body;
        SpriteRenderer _glyph;

        public Guid PieceId { get; private set; }

        public void Bind(Piece piece, float squareSize, BoardTheme theme)
        {
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));

            PieceId = piece.Id;
            name = $"{piece.Side} {piece.Type}";
            EnsureRenderers();

            float bodySize = squareSize * 0.78f;
            _outline.transform.localScale = new Vector3(bodySize * 1.08f, bodySize * 1.08f, 1f);
            _body.transform.localScale = new Vector3(bodySize, bodySize, 1f);
            _glyph.transform.localScale = new Vector3(bodySize * 0.62f, bodySize * 0.62f, 1f);

            bool white = piece.Side == Side.White;
            _outline.color = white ? theme.WhitePieceOutline : theme.BlackPieceOutline;
            _body.color = white ? theme.WhitePieceFill : theme.BlackPieceFill;
            _glyph.sprite = ChessGlyphs.GetSprite(piece.Type, piece.Side);
            _glyph.color = Color.white;
            bool hasArt = ChessArt.Get(piece.Type, piece.Side) != null;
            _outline.enabled = !hasArt;
            _body.enabled = !hasArt;
            _glyph.enabled = true;
        }

        public void BindShadow(float squareSize, BoardTheme theme)
        {
            EnsureRenderers();
            name = "Shadow";
            float bodySize = squareSize * 0.7f;
            _outline.enabled = false;
            _body.enabled = true;
            _glyph.enabled = false;
            _body.transform.localScale = new Vector3(bodySize, bodySize, 1f);
            _body.color = theme.BlackPieceFill;
        }

        void EnsureRenderers()
        {
            if (_body != null)
                return;

            _outline = CreateRenderer("Outline", BoardRenderOrder.PieceOutline, RuntimeSprites.Circle);
            _body = CreateRenderer("Body", BoardRenderOrder.PieceBody, RuntimeSprites.Circle);
            _glyph = CreateRenderer("Glyph", BoardRenderOrder.PieceGlyph, RuntimeSprites.Pixel);
        }

        SpriteRenderer CreateRenderer(string childName, int order, Sprite sprite)
        {
            var child = new GameObject(childName);
            child.transform.SetParent(transform, false);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            var spriteRenderer = child.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = sprite;
            spriteRenderer.sortingOrder = order;
            return spriteRenderer;
        }
    }
}
