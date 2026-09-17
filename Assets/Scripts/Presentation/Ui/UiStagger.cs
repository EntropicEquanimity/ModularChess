using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace ModularChess.Presentation
{
    public sealed class UiStagger : MonoBehaviour
    {
        #region Fields
        [SerializeField] float delay = 0.1f;
        [SerializeField] RectTransform[] items;
        readonly List<GameObject> _queue = new List<GameObject>();
        Tween _tween;
        bool _collected;
        #endregion

        #region Unity
        void Awake()
        {
            Collect();
        }
        void OnEnable()
        {
            Collect();
            Hide();
            Play();
        }
        void OnDisable()
        {
            _tween?.Kill();
            Show();
        }
        void OnDestroy()
        {
            _tween?.Kill();
        }
        #endregion

        #region Private Methods
        void Collect()
        {
            if (_collected)
                return;
            _queue.Clear();
            if (items != null && items.Length > 0)
            {
                for (int i = 0; i < items.Length; i++)
                {
                    if (items[i] != null)
                        _queue.Add(items[i].gameObject);
                }
            }
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                    _queue.Add(transform.GetChild(i).gameObject);
            }
            _collected = true;
        }
        void Hide()
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i] != null)
                    _queue[i].SetActive(false);
            }
        }
        void Show()
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                if (_queue[i] != null)
                    _queue[i].SetActive(true);
            }
        }
        void Play()
        {
            _tween?.Kill();
            float step = UiAnimPrefs.MoveDuration(delay);
            if (step <= 0.001f)
            {
                Show();
                return;
            }
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            for (int i = 0; i < _queue.Count; i++)
            {
                GameObject go = _queue[i];
                sequence.InsertCallback(step * i, () =>
                {
                    if (go != null)
                        go.SetActive(true);
                });
            }
            _tween = sequence;
        }
        #endregion
    }
}
