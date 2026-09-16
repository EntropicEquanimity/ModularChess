using DG.Tweening;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class OverlayMotion : MonoBehaviour
    {
        #region Fields
        const float Duration = 0.28f;
        const string BodyName = "SlideBody";
        Canvas _canvas;
        CanvasGroup _group;
        RectTransform _body;
        Tween _tween;
        int _sort;
        bool _exiting;
        #endregion

        #region Unity
        void OnDisable()
        {
            _tween?.Kill();
            _exiting = false;
        }
        #endregion

        #region Public Methods
        public static OverlayMotion Ensure(GameObject root)
        {
            if (root == null)
            {
                return null;
            }

            OverlayMotion motion = root.GetComponent<OverlayMotion>();
            if (motion == null)
            {
                motion = root.AddComponent<OverlayMotion>();
            }

            motion.Wire();
            return motion;
        }
        public void PlayEnter()
        {
            Wire();
            bool wasActive = gameObject.activeSelf;
            if (wasActive && !_exiting)
            {
                return;
            }

            _tween?.Kill();
            gameObject.SetActive(true);
            Wire();
            _exiting = false;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            if (_canvas != null)
            {
                _canvas.sortingOrder = _sort + 1;
            }

            float duration = UiAnimPrefs.MoveDuration(Duration);
            if (!wasActive || _group.alpha <= 0.01f)
            {
                _body.anchoredPosition = new Vector2(0f, -Travel());
                _group.alpha = 0f;
            }

            if (duration <= 0.001f)
            {
                FinishEnter();
                return;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(DOTween.To(
                () => _body.anchoredPosition,
                v => _body.anchoredPosition = v,
                Vector2.zero,
                duration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(
                () => _group.alpha,
                a => _group.alpha = a,
                1f,
                duration).SetEase(Ease.OutQuad));
            sequence.OnComplete(FinishEnter);
            _tween = sequence;
        }
        public void PlayExit()
        {
            if (!gameObject.activeSelf)
            {
                return;
            }

            Wire();
            if (_exiting)
            {
                return;
            }

            _tween?.Kill();
            _exiting = true;
            _group.blocksRaycasts = false;
            _group.interactable = false;
            if (_canvas != null)
            {
                _canvas.sortingOrder = _sort;
            }

            float duration = UiAnimPrefs.MoveDuration(Duration);
            if (duration <= 0.001f)
            {
                FinishExit();
                return;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(DOTween.To(
                () => _body.anchoredPosition,
                v => _body.anchoredPosition = v,
                new Vector2(0f, Travel()),
                duration).SetEase(Ease.InCubic));
            sequence.Join(DOTween.To(
                () => _group.alpha,
                a => _group.alpha = a,
                0f,
                duration).SetEase(Ease.InQuad));
            sequence.OnComplete(FinishExit);
            _tween = sequence;
        }
        public void HideImmediate()
        {
            _tween?.Kill();
            _exiting = false;
            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = true;
                _group.interactable = true;
            }

            if (_body != null)
            {
                _body.anchoredPosition = Vector2.zero;
            }

            if (_canvas != null)
            {
                _canvas.sortingOrder = _sort;
            }

            gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        void Wire()
        {
            if (_canvas == null)
            {
                _canvas = GetComponent<Canvas>();
                if (_canvas != null)
                {
                    _sort = _canvas.sortingOrder;
                }
            }

            if (_group == null)
            {
                _group = GetComponent<CanvasGroup>();
                if (_group == null)
                {
                    _group = gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (_body == null)
            {
                Transform existing = transform.Find(BodyName);
                if (existing != null)
                {
                    _body = existing as RectTransform;
                }
            }

            if (_body == null)
            {
                BuildBody();
            }
        }
        void BuildBody()
        {
            var go = new GameObject(BodyName, typeof(RectTransform));
            _body = go.GetComponent<RectTransform>();
            _body.SetParent(transform, false);
            _body.anchorMin = Vector2.zero;
            _body.anchorMax = Vector2.one;
            _body.offsetMin = Vector2.zero;
            _body.offsetMax = Vector2.zero;
            _body.anchoredPosition = Vector2.zero;
            _body.localScale = Vector3.one;
            int childCount = transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child == _body)
                {
                    continue;
                }

                child.SetParent(_body, false);
                i--;
                childCount--;
            }

            _body.SetAsFirstSibling();
        }
        void FinishEnter()
        {
            if (_body != null)
            {
                _body.anchoredPosition = Vector2.zero;
            }

            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = true;
                _group.interactable = true;
            }

            if (_canvas != null)
            {
                _canvas.sortingOrder = _sort;
            }
        }
        void FinishExit()
        {
            _exiting = false;
            if (_body != null)
            {
                _body.anchoredPosition = Vector2.zero;
            }

            if (_group != null)
            {
                _group.alpha = 1f;
                _group.blocksRaycasts = true;
                _group.interactable = true;
            }

            if (_canvas != null)
            {
                _canvas.sortingOrder = _sort;
            }

            gameObject.SetActive(false);
        }
        float Travel()
        {
            float scale = _canvas != null ? _canvas.scaleFactor : 1f;
            if (scale < 0.0001f)
            {
                scale = 1f;
            }

            return Screen.height / scale;
        }
        #endregion
    }
}
