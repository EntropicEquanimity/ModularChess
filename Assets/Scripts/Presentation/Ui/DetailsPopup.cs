using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class DetailsPopup : MonoBehaviour
    {
        #region Fields
        const float FadeSeconds = 0.12f;
        [SerializeField] PieceDetailsPanel pieceDetails;
        [SerializeField] CanvasGroup canvasGroup;
        RectTransform _rect;
        Transform _anchor;
        bool _fadeIn;
        bool _fadeAway;
        float _alpha = 1f;
        float _target = 1f;
        bool _hiding;
        #endregion

        #region Unity
        void Awake()
        {
            Wire();
        }
        void LateUpdate()
        {
            FollowAnchor();
            if (Mathf.Abs(_alpha - _target) < 0.01f)
            {
                _alpha = _target;
                ApplyAlpha();
                if (_hiding && _alpha <= 0.01f)
                    gameObject.SetActive(false);
                return;
            }
            float step = Time.unscaledDeltaTime / Mathf.Max(0.01f, FadeSeconds);
            _alpha = Mathf.MoveTowards(_alpha, _target, step);
            ApplyAlpha();
            if (_hiding && _alpha <= 0.01f)
                gameObject.SetActive(false);
        }
        #endregion

        #region Public Methods
        public void Present(string title, string body)
        {
            Wire();
            pieceDetails?.ShowText(title, body);
            ShowNow();
        }
        public void PresentPiece(ModularChess.Core.Piece piece, ModularChess.Core.GameState state)
        {
            Wire();
            pieceDetails?.Show(piece, state, null);
            ShowNow();
        }
        public void AnchorTo(Transform target)
        {
            _anchor = target;
            FollowAnchor();
        }
        public void ConfigureFade(bool fadeIn, bool fadeAway)
        {
            _fadeIn = fadeIn;
            _fadeAway = fadeAway;
        }
        public void Hide()
        {
            if (!gameObject.activeSelf)
                return;
            _hiding = true;
            if (_fadeAway)
            {
                _target = 0f;
                return;
            }
            gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        void ShowNow()
        {
            _hiding = false;
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (_fadeIn)
            {
                _alpha = 0f;
                _target = 1f;
            }
            else
            {
                _alpha = 1f;
                _target = 1f;
            }
            ApplyAlpha();
            FollowAnchor();
            LayoutRebuilder.ForceRebuildLayoutImmediate(_rect);
        }
        void FollowAnchor()
        {
            if (_anchor == null || _rect == null)
                return;
            Canvas canvas = GetComponentInParent<Canvas>();
            Camera worldCam = BoardCamera.ActiveCamera;
            if (worldCam == null)
                worldCam = Camera.main;
            Vector3 world = _anchor.position;
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(worldCam, world);
            RectTransform parent = _rect.parent as RectTransform;
            if (parent == null)
                return;
            Camera overlayCam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, overlayCam, out Vector2 local))
                return;
            float pad = 24f;
            float halfW = _rect.rect.width * 0.5f;
            float halfH = _rect.rect.height * 0.5f;
            if (halfW < 1f)
                halfW = 80f;
            if (halfH < 1f)
                halfH = 40f;
            Vector2 parentSize = parent.rect.size;
            Vector2 desired = local + new Vector2(halfW + pad, halfH + pad);
            float minX = -parentSize.x * 0.5f + halfW + 8f;
            float maxX = parentSize.x * 0.5f - halfW - 8f;
            float minY = -parentSize.y * 0.5f + halfH + 8f;
            float maxY = parentSize.y * 0.5f - halfH - 8f;
            if (desired.x > maxX)
                desired.x = local.x - halfW - pad;
            if (desired.y > maxY)
                desired.y = local.y - halfH - pad;
            desired.x = Mathf.Clamp(desired.x, minX, maxX);
            desired.y = Mathf.Clamp(desired.y, minY, maxY);
            _rect.anchoredPosition = desired;
        }
        void ApplyAlpha()
        {
            if (canvasGroup != null)
                canvasGroup.alpha = _alpha;
        }
        void Wire()
        {
            if (_rect == null)
                _rect = transform as RectTransform;
            if (pieceDetails == null)
                pieceDetails = GetComponent<PieceDetailsPanel>();
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        #endregion
    }
}
