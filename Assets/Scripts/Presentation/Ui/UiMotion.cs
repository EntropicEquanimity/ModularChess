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
        [SerializeField] float openMinHeight = 8f;
        [SerializeField] float openPop = 4f;
        [SerializeField] float openDuration = 0.15f;
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
        void OnEnable()
        {
            PlayOpen();
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
        void PlayOpen()
        {
            Cache();
            if (image == null)
                return;
            _hover = false;
            float time = UiAnimPrefs.MoveDuration(openDuration);
            float startY = openMinHeight;
            float peakY = _restSize.y + openPop;
            _tween?.Kill();
            image.sizeDelta = new Vector2(_restSize.x, startY);
            if (time <= 0.001f)
            {
                image.sizeDelta = _restSize;
                return;
            }
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Append(TweenHeight(peakY, time * 0.7f, Ease.OutCubic));
            sequence.Append(TweenHeight(_restSize.y, time * 0.3f, Ease.InCubic));
            _tween = sequence;
        }
        Tween TweenHeight(float height, float time, Ease ease)
        {
            return DOTween.To(
                    () => image.sizeDelta.y,
                    y => image.sizeDelta = new Vector2(_restSize.x, y),
                    height,
                    time)
                .SetEase(ease)
                .SetUpdate(true)
                .SetTarget(this);
        }
        #endregion
    }
}
