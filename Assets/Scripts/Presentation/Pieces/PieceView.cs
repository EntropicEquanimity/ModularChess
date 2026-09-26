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
        Color _liveOutline = Color.white;
        Color _liveBody = Color.white;
        Color _liveGlyph = Color.white;
        Color _glyphBeforeThreat = Color.white;
        Color _bodyBeforeThreat = Color.white;
        bool _captureThreatened;
        bool _restoreTintOnKill = true;
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
        bool _ghosted;

        static readonly Color AlliedAura = new Color(0.22f, 0.82f, 0.32f, 0.55f);
        static readonly Color EnemyAura = new Color(0.9f, 0.18f, 0.18f, 0.55f);

        public Guid PieceId { get; private set; }
        public bool IsMoving => _motion != null && _motion.IsActive();

        public void Bind(Piece piece, float squareSize, BoardTheme theme)
        {
            if (piece == null)
                throw new ArgumentNullException(nameof(piece));

            PieceId = piece.Id;
            name = $"{piece.Side} {piece.Type}";
            EnsureRenderers();
            CacheVisuals();
            _ghosted = false;
            _captureThreatened = false;
            RestorePrefabVisuals();
            if (piece.Hue != 0)
            {
                _glyph.sprite = ChessGlyphs.GetSprite(piece.Type, Side.White);
                _glyph.color = Color.HSVToRGB(piece.Hue / 255f, 0.62f, 0.95f);
            }
            else
            {
                _glyph.sprite = ChessGlyphs.GetSprite(piece.Type, piece.Side);
            }
            _glyph.enabled = true;
        }
        public void BindCaptured(Guid id, PieceType type, Side side, BoardTheme theme)
        {
            PieceId = id;
            name = $"{side} {type}";
            EnsureRenderers();
            CacheVisuals();
            _ghosted = false;
            _captureThreatened = false;
            RestorePrefabVisuals();
            _glyph.sprite = ChessGlyphs.GetSprite(type, side);
            _glyph.enabled = true;
        }

        public void SetEmpoweredAura(bool show, bool allied)
        {
            if (_body == null)
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
            if (_outline == null)
                return;
            if (selected)
            {
                _outline.enabled = true;
                _outline.color = new Color32(246, 246, 105, 230);
                return;
            }
            _outline.enabled = _outlineEnabled;
            _outline.color = _outlineColor;
        }

        public void SetCaptureThreat(bool threatened)
        {
            if (threatened == _captureThreatened)
                return;
            if (threatened)
            {
                if (_glyph != null)
                    _glyphBeforeThreat = _glyph.color;
                if (_body != null)
                    _bodyBeforeThreat = _body.color;
                _captureThreatened = true;
                Color tint = new Color(1f, 0.18f, 0.14f, _ghosted ? 0.45f : 1f);
                if (_glyph != null)
                    _glyph.color = tint;
                if (_body != null && _body.enabled)
                    _body.color = new Color(tint.r, tint.g, tint.b, _ghosted ? 0.4f : 0.7f);
                return;
            }
            _captureThreatened = false;
            if (_glyph != null)
                _glyph.color = _glyphBeforeThreat;
            if (_body != null && _body.enabled)
                _body.color = _bodyBeforeThreat;
        }
        public void SetGhosted(bool ghosted)
        {
            _ghosted = ghosted;
            ApplyGhostAlpha();
            CaptureLiveTint();
        }
        public void SnapTo(Vector3 localPosition)
        {
            KillMotion(invokeEnded: true);
            transform.localPosition = localPosition;
            transform.localScale = _restScale;
            transform.localRotation = Quaternion.identity;
            SetLifted(false);
            ApplyGhostAlpha();
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
        public bool PlayArc(Vector3 dest, float duration, float height, Action onEnded)
        {
            KillMotion(invokeEnded: true);
            if (duration <= 0.001f || (transform.localPosition - dest).sqrMagnitude < 0.0001f)
            {
                transform.localPosition = dest;
                transform.localScale = _restScale;
                SetLifted(false);
                return false;
            }
            Vector3 start = transform.localPosition;
            _onEnded = onEnded;
            SetLifted(true);
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Join(DOTween.To(
                    () => 0f,
                    t =>
                    {
                        Vector3 point = Vector3.LerpUnclamped(start, dest, t);
                        point.y += height * 4f * t * (1f - t);
                        transform.localPosition = point;
                    },
                    1f,
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
        public bool PlayCaptureDepart(Vector3 dest, float duration, float height, Action onEnded)
        {
            KillMotion(invokeEnded: true);
            if (duration <= 0.001f || (transform.localPosition - dest).sqrMagnitude < 0.0001f)
            {
                RestoreLiveTint();
                transform.localPosition = dest;
                transform.localScale = _restScale;
                SetLifted(false);
                return false;
            }
            CaptureLiveTint();
            Vector3 start = transform.localPosition;
            Vector3 squashed = new Vector3(_restScale.x * 1.38f, _restScale.y * 0.42f, _restScale.z);
            float hit = Mathf.Max(0.02f, AnimationPrefs.MoveDuration(2f / 60f));
            float unsquash = Mathf.Max(0.03f, AnimationPrefs.MoveDuration(0.07f));
            _onEnded = onEnded;
            _restoreTintOnKill = true;
            SetLifted(true);
            ApplyFlash(Color.white, Color.white);
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    squashed,
                    hit)
                .SetEase(Ease.OutCubic));
            seq.AppendCallback(RestoreLiveTint);
            seq.Append(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    _restScale,
                    unsquash)
                .SetEase(Ease.OutBack));
            seq.Append(DOTween.To(
                    () => 0f,
                    t =>
                    {
                        Vector3 point = Vector3.LerpUnclamped(start, dest, t);
                        point.y += height * 4f * t * (1f - t);
                        transform.localPosition = point;
                    },
                    1f,
                    duration)
                .SetEase(HandEase));
            seq.OnKill(OnMotionKilled);
            _motion = seq;
            return true;
        }
        public bool PlayDeflect(Vector3 toward, float duration, Action onEnded)
        {
            KillMotion(invokeEnded: true);
            if (duration <= 0.001f)
            {
                transform.localScale = _restScale;
                SetLifted(false);
                return false;
            }
            Vector3 home = transform.localPosition;
            Vector3 peak = Vector3.Lerp(home, toward, 0.55f);
            _onEnded = onEnded;
            SetLifted(true);
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(DOTween.To(
                    () => transform.localPosition,
                    v => transform.localPosition = v,
                    peak,
                    duration * 0.38f)
                .SetEase(Ease.OutCubic));
            seq.Append(DOTween.To(
                    () => transform.localPosition,
                    v => transform.localPosition = v,
                    home,
                    duration * 0.62f)
                .SetEase(Ease.OutBack));
            seq.OnKill(OnMotionKilled);
            _motion = seq;
            return true;
        }
        public bool PlaySelectPop()
        {
            KillMotion(invokeEnded: true);
            float duration = AnimationPrefs.MoveDuration(0.1f);
            if (duration <= 0.001f)
            {
                transform.localScale = _restScale;
                return false;
            }
            Vector3 peak = new Vector3(_restScale.x * 1.14f, _restScale.y * 1.2f, _restScale.z);
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    peak,
                    duration * 0.4f)
                .SetEase(Ease.OutCubic));
            seq.Append(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    _restScale,
                    duration * 0.6f)
                .SetEase(Ease.OutBack));
            seq.OnKill(OnMotionKilled);
            _motion = seq;
            return true;
        }
        public bool PlayShiver()
        {
            KillMotion(invokeEnded: true);
            float duration = AnimationPrefs.MoveDuration(0.16f);
            if (duration <= 0.001f)
                return false;
            CaptureLiveTint();
            _restoreTintOnKill = true;
            Vector3 peak = new Vector3(_restScale.x * 0.82f, _restScale.y * 1.28f, _restScale.z);
            ApplyFlash(null, new Color(1f, 0.22f, 0.22f, 1f));
            if (_outline != null)
                _outline.color = new Color(1f, 0.18f, 0.18f, _liveOutline.a);
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    peak,
                    duration * 0.4f)
                .SetEase(Ease.OutCubic));
            seq.Append(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    _restScale,
                    duration * 0.6f)
                .SetEase(Ease.OutBack));
            seq.OnKill(OnMotionKilled);
            _motion = seq;
            return true;
        }
        public bool PlayPop(float duration, Action onEnded)
        {
            KillMotion(invokeEnded: true);
            duration = AnimationPrefs.MoveDuration(duration);
            if (duration <= 0.001f)
            {
                transform.localScale = _restScale;
                return false;
            }
            Vector3 peak = new Vector3(_restScale.x * 0.88f, _restScale.y * 1.38f, _restScale.z);
            _onEnded = onEnded;
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Append(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    peak,
                    duration * 0.45f)
                .SetEase(Ease.OutCubic));
            seq.Append(DOTween.To(
                    () => transform.localScale,
                    v => transform.localScale = v,
                    _restScale,
                    duration * 0.55f)
                .SetEase(Ease.OutBack));
            seq.OnKill(OnMotionKilled);
            _motion = seq;
            return true;
        }
        public bool PlayFadeOut(float duration, Action onEnded)
        {
            KillMotion(invokeEnded: true);
            if (duration <= 0.001f)
            {
                SetRenderAlpha(0f);
                gameObject.SetActive(false);
                return false;
            }
            _onEnded = onEnded;
            _restoreTintOnKill = false;
            Sequence seq = DOTween.Sequence().SetTarget(this);
            seq.Join(DOTween.To(
                    () => 1f,
                    a => SetRenderAlpha(a),
                    0f,
                    duration)
                .SetEase(Ease.InQuad));
            seq.OnKill(OnFadeKilled);
            _motion = seq;
            return true;
        }
        public bool PlayKnockOff(
            Vector3 velocity,
            Rect board,
            float gravity,
            float duration,
            float delay,
            Action onEnded)
        {
            KillMotion(invokeEnded: true);
            if (duration <= 0.001f)
            {
                gameObject.SetActive(false);
                return false;
            }
            Vector3 pos = transform.localPosition;
            Vector3 vel = velocity;
            Vector3 scale = transform.localScale;
            int hits = 0;
            float last = 0f;
            float spin = vel.x >= 0f ? -260f : 260f;
            _onEnded = onEnded;
            _restoreTintOnKill = true;
            SetLifted(true);
            Sequence seq = DOTween.Sequence().SetTarget(this);
            if (delay > 0.001f)
                seq.AppendInterval(delay);
            seq.Append(DOTween.To(
                    () => 0f,
                    t =>
                    {
                        float dt = Mathf.Min(0.05f, Mathf.Max(0f, t - last) * duration);
                        last = t;
                        vel.y -= gravity * dt;
                        pos += vel * dt;
                        int before = hits;
                        if (hits < 2)
                        {
                            if (pos.x < board.xMin)
                            {
                                pos.x = board.xMin;
                                vel.x = Mathf.Abs(vel.x) * 0.62f;
                                hits++;
                            }
                            else if (pos.x > board.xMax)
                            {
                                pos.x = board.xMax;
                                vel.x = -Mathf.Abs(vel.x) * 0.62f;
                                hits++;
                            }
                            if (pos.y < board.yMin)
                            {
                                pos.y = board.yMin;
                                vel.y = Mathf.Abs(vel.y) * 0.62f;
                                hits++;
                            }
                            else if (pos.y > board.yMax)
                            {
                                pos.y = board.yMax;
                                vel.y = -Mathf.Abs(vel.y) * 0.62f;
                                hits++;
                            }
                        }
                        if (hits > before)
                            scale = new Vector3(_restScale.x * 1.28f, _restScale.y * 0.58f, _restScale.z);
                        else
                            scale = Vector3.Lerp(scale, _restScale, 1f - Mathf.Exp(-dt * 16f));
                        transform.localPosition = pos;
                        transform.localScale = scale;
                        transform.localRotation = Quaternion.Euler(0f, 0f, spin * t);
                    },
                    1f,
                    duration)
                .SetEase(Ease.Linear));
            seq.OnKill(OnKnockKilled);
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
            transform.localRotation = Quaternion.identity;
            SetLifted(false);
        }

        void OnMotionKilled()
        {
            _motion = null;
            transform.localScale = _restScale;
            SetLifted(false);
            if (_restoreTintOnKill)
                RestoreLiveTint();
            _restoreTintOnKill = true;
            Action ended = _onEnded;
            _onEnded = null;
            ended?.Invoke();
        }
        void OnFadeKilled()
        {
            _motion = null;
            transform.localScale = _restScale;
            SetLifted(false);
            SetRenderAlpha(0f);
            gameObject.SetActive(false);
            _restoreTintOnKill = true;
            Action ended = _onEnded;
            _onEnded = null;
            ended?.Invoke();
        }
        void OnKnockKilled()
        {
            _motion = null;
            transform.localScale = _restScale;
            transform.localRotation = Quaternion.identity;
            SetLifted(false);
            gameObject.SetActive(false);
            _restoreTintOnKill = true;
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
            CaptureLiveTint();
            ApplyGhostAlpha();
        }
        void CaptureLiveTint()
        {
            _liveOutline = _outline != null ? _outline.color : Color.white;
            _liveBody = _body != null ? _body.color : Color.white;
            _liveGlyph = _glyph != null ? _glyph.color : Color.white;
        }
        void RestoreLiveTint()
        {
            if (_outline != null)
                _outline.color = _liveOutline;
            if (_body != null)
                _body.color = _liveBody;
            if (_glyph != null)
                _glyph.color = _liveGlyph;
        }
        void ApplyFlash(Color? body, Color? glyph)
        {
            if (body != null && _body != null)
                _body.color = body.Value;
            if (glyph != null && _glyph != null)
                _glyph.color = glyph.Value;
        }
        void ApplyGhostAlpha()
        {
            SetRenderAlpha(_ghosted ? 0.45f : 1f);
        }
        void SetRenderAlpha(float alpha)
        {
            if (_outline != null)
            {
                Color color = _outline.color;
                color.a = alpha * _outlineColor.a;
                _outline.color = color;
            }
            if (_body != null)
            {
                Color color = _body.color;
                color.a = alpha * _bodyColor.a;
                _body.color = color;
            }
            if (_glyph != null)
            {
                Color color = _glyph.color;
                color.a = alpha * _glyphColor.a;
                _glyph.color = color;
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
