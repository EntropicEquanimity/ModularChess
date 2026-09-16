using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class UiMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        #region Fields
        [SerializeField] RectTransform image;
        [SerializeField] float hoverPadX = 40f;
        [SerializeField] float hoverPadY = 10f;
        [SerializeField] float duration = 0.1f;
        Selectable _selectable;
        Tween _tween;
        Vector2 _restSize;
        bool _hover;
        bool _cached;
        #endregion

        #region Unity
        void Awake()
        {
            Cache();
        }
        void OnDisable()
        {
            _hover = false;
            _tween?.Kill();
            if (image != null)
                image.sizeDelta = _restSize;
        }
        void OnDestroy()
        {
            _tween?.Kill();
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (!CanAnimate())
                return;
            _hover = true;
            Play();
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            _hover = false;
            Play();
        }
        public void OnSelect(BaseEventData eventData)
        {
            if (!CanAnimate())
                return;
            _hover = true;
            Play();
        }
        public void OnDeselect(BaseEventData eventData)
        {
            _hover = false;
            Play();
        }
        #endregion

        #region Private Methods
        void Cache()
        {
            if (image == null)
            {
                Image graphic = GetComponent<Image>();
                if (graphic != null)
                    image = graphic.rectTransform;
            }
            if (_selectable == null)
                _selectable = GetComponent<Selectable>();
            if (_cached || image == null)
                return;
            _restSize = image.sizeDelta;
            _cached = true;
        }
        bool CanAnimate()
        {
            return _selectable == null || _selectable.IsInteractable();
        }
        void Play()
        {
            Cache();
            if (image == null)
                return;
            Vector2 target = _hover ? _restSize + new Vector2(hoverPadX, hoverPadY) : _restSize;
            float time = UiAnimPrefs.MoveDuration(duration);
            _tween?.Kill();
            if (time <= 0.001f)
            {
                image.sizeDelta = target;
                return;
            }
            _tween = DOTween.To(
                    () => image.sizeDelta,
                    v => image.sizeDelta = v,
                    target,
                    time)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(this);
        }
        #endregion
    }
}
