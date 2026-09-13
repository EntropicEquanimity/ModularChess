using System;
using DG.Tweening;
using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class PieceView : MonoBehaviour
    {
        static readonly AnimationCurve HandEase = new AnimationCurve(
            new Keyframe(0f, 0f, 0f, 0f),
            new Keyframe(0.2f, 0.14f, 1.2f, 1.2f),
            new Keyframe(0.8f, 0.86f, 1.2f, 1.2f),
            new Keyframe(1f, 1f, 0f, 0f));

        SpriteRenderer _outline;
        SpriteRenderer _body;
        SpriteRenderer _glyph;
        Tween _motion;
        Action _onEnded;
        Vector3 _restScale = Vector3.one;
        Color _outlineColor = Color.white;
        Color _bodyColor = Color.white;
        Color _glyphColor = Color.white;
        bool _outlineEnabled;
        bool _bodyEnabled;
        bool _glyphEnabled = true;
        int _outlineOrder;
        int _bodyOrder;
        int _glyphOrder;
        bool _cachedVisuals;

        static readonly Color AlliedAura = new Color(0.22f, 0.82f, 0.32f, 0.55f);
        static readonly Color EnemyAura = new Color(0.9f, 0.18f, 0.18f, 0.55f);

        public Guid PieceId { get; private set; }
        public bool IsShadow { get; private set; }
        public bool IsMoving => _motion != null && _motion.IsActive();

        public void Bind(Piece piece, float squareSize, BoardTheme theme)
        {
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));

            PieceId = piece.Id;
            IsShadow = false;
            name = $"{piece.Side} {piece.Type}";
            EnsureRenderers();
            CacheVisuals();
            RestorePrefabVisuals();

            _glyph.sprite = ChessGlyphs.GetSprite(piece.Type, piece.Side);
            _glyph.enabled = true;
        }

        public void BindShadow(float squareSize, BoardTheme theme)
        {
            EnsureRenderers();
            CacheVisuals();
            IsShadow = true;
            name = "Shadow";
            _outline.enabled = false;
            _body.enabled = true;
            _glyph.enabled = false;
            _body.color = theme.BlackPieceFill;
        }

        public void SetEmpoweredAura(bool show, bool allied)
        {
            if (_body == null || IsShadow)
                return;

            if (!show)
            {
                _body.enabled = _bodyEnabled;
                _body.color = _bodyColor;
                return;
            }

            _body.enabled = true;
            _body.color = allied ? AlliedAura : EnemyAura;
        }

        public void SetSelectedOutline(bool selected)
        {
            if (_outline == null || IsShadow)
                return;
            _outline.enabled = selected || _outlineEnabled;
        }

        public void SnapTo(Vector3 localPosition)
        {
            KillMotion(invokeEnded: true);
            transform.localPosition = localPosition;
            transform.localScale = _restScale;
            SetLifted(false);
        }

        public bool PlayMove(Vector3 dest, float duration, Action onEnded)
        {
            KillMotion(invokeEnded: true);
            if (duration <= 0.001f || (transform.localPosition - dest).sqrMagnitude < 0.0001f)
            {
                transform.localPosition = dest;
                transform.localScale = _restScale;
                SetLifted(false);
                return false;
            }

            _onEnded = onEnded;
            SetLifted(true);
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Join(DOTween.To(
                    () => transform.localPosition,
                    v => transform.localPosition = v,
                    dest,
                    duration)
                .SetEase(HandEase));
            seq.Join(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    _restScale * 1.12f,
                    duration * 0.2f)
                .SetEase(Ease.OutCubic));
            seq.Insert(
                duration * 0.55f,
                DOTween.To(
                        () => transform.localScale,
                        v => transform.localScale = v,
                        _restScale,
                        duration * 0.45f)
                    .SetEase(Ease.InCubic));
            seq.OnKill(OnMotionKilled);
            _motion = seq;
            return true;
        }

        public void SetMotionPaused(bool paused)
        {
            if (_motion == null || !_motion.IsActive())
                return;
            if (paused)
                _motion.Pause();
            else
                _motion.Play();
        }

        public void CompleteMotion()
        {
            if (_motion != null && _motion.IsActive())
                _motion.Complete(true);
            else
                KillMotion(invokeEnded: true);
            transform.localScale = _restScale;
            SetLifted(false);
        }

        void OnMotionKilled()
        {
            _motion = null;
            transform.localScale = _restScale;
            SetLifted(false);
            Action ended = _onEnded;
            _onEnded = null;
            ended?.Invoke();
        }

        void KillMotion(bool invokeEnded)
        {
            if (_motion == null)
                return;
            if (!invokeEnded)
                _onEnded = null;
            if (_motion.IsActive())
                _motion.Kill();
            _motion = null;
        }

        void SetLifted(bool lifted)
        {
            int add = lifted ? 10 : 0;
            if (_outline != null)
                _outline.sortingOrder = _outlineOrder + add;
            if (_body != null)
                _body.sortingOrder = _bodyOrder + add;
            if (_glyph != null)
                _glyph.sortingOrder = _glyphOrder + add;
        }

        void CacheVisuals()
        {
            if (_cachedVisuals)
                return;

            _restScale = transform.localScale;
            if (_restScale.sqrMagnitude < 0.0001f)
                _restScale = Vector3.one;
            if (_outline != null)
            {
                _outlineEnabled = _outline.enabled;
                _outlineColor = _outline.color;
                _outlineOrder = _outline.sortingOrder;
            }

            if (_body != null)
            {
                _bodyEnabled = _body.enabled;
                _bodyColor = _body.color;
                _bodyOrder = _body.sortingOrder;
            }

            if (_glyph != null)
            {
                _glyphEnabled = _glyph.enabled;
                _glyphColor = _glyph.color;
                _glyphOrder = _glyph.sortingOrder;
            }

            _cachedVisuals = true;
        }

        void RestorePrefabVisuals()
        {
            if (_outline != null)
            {
                _outline.enabled = _outlineEnabled;
                _outline.color = _outlineColor;
            }

            if (_body != null)
            {
                _body.enabled = _bodyEnabled;
                _body.color = _bodyColor;
            }

            if (_glyph != null)
            {
                _glyph.enabled = _glyphEnabled;
                _glyph.color = _glyphColor;
            }
        }

        void EnsureRenderers()
        {
            if (_body != null)
                return;

            _outline = FindRenderer("Outline");
            _body = FindRenderer("Body");
            _glyph = FindRenderer("Glyph");
            if (_outline == null)
                _outline = CreateRenderer("Outline", BoardRenderOrder.PieceOutline, RuntimeSprites.Circle);
            if (_body == null)
                _body = CreateRenderer("Body", BoardRenderOrder.PieceBody, RuntimeSprites.Circle);
            if (_glyph == null)
                _glyph = CreateRenderer("Glyph", BoardRenderOrder.PieceGlyph, RuntimeSprites.Pixel);
        }

        SpriteRenderer FindRenderer(string childName)
        {
            Transform child = transform.Find(childName);
            return child != null ? child.GetComponent<SpriteRenderer>() : null;
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

        void OnDestroy()
        {
            KillMotion(invokeEnded: false);
        }
    }
}
