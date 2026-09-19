using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class DraftRow : MonoBehaviour
    {
        #region Fields
        [SerializeField] RectTransform[] optionRoots;
        [SerializeField] Button hideShowButton;
        [SerializeField] GameObject hideIcon;
        [SerializeField] GameObject showIcon;
        [SerializeField] float optionsDelay = 1f;
        [SerializeField] float hideLockDuration = 4f;
        [SerializeField] float descriptionHeight = 300f;
        [SerializeField] float descriptionDuration = 0.28f;
        [SerializeField] float descriptionDelay = 0.5f;
        readonly List<RectTransform> _options = new List<RectTransform>();
        readonly List<RectTransform> _descriptions = new List<RectTransform>();
        Tween _sequence;
        Action<int> _onPicked;
        int _used;
        bool _buttonsShown;
        bool _optionsHidden;
        bool _collected;
        #endregion

        #region Unity
        void Awake()
        {
            Collect();
        }
        void OnDisable()
        {
            KillSequence();
        }
        void OnDestroy()
        {
            KillSequence();
        }
        #endregion

        #region Public Methods
        public void Present(IReadOnlyList<DraftChoice> choices, Action<int> onPicked)
        {
            Collect();
            _onPicked = onPicked;
            _optionsHidden = false;
            _buttonsShown = false;
            _used = choices != null ? Mathf.Min(choices.Count, _options.Count) : 0;
            KillSequence();
            BindChoices(choices);
            HideOptions();
            PrepareDescriptions();
            if (hideShowButton != null)
            {
                hideShowButton.gameObject.SetActive(true);
                hideShowButton.interactable = false;
                GameAudio.Bind(hideShowButton, ToggleOptions);
            }
            RefreshIcons();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            float showAt = UiAnimPrefs.MoveDuration(optionsDelay);
            float riseAt = showAt + UiAnimPrefs.MoveDuration(descriptionDelay);
            float lockAt = UiAnimPrefs.MoveDuration(hideLockDuration);
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.InsertCallback(showAt, RevealButtons);
            sequence.InsertCallback(riseAt, RiseDescriptions);
            sequence.InsertCallback(lockAt, UnlockHideShow);
            _sequence = sequence;
        }
        public void Dismiss()
        {
            KillSequence();
            _onPicked = null;
            _buttonsShown = false;
            _optionsHidden = false;
            _used = 0;
            if (hideShowButton != null)
                hideShowButton.interactable = false;
            gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        void Collect()
        {
            if (_collected)
                return;
            _options.Clear();
            if (optionRoots != null && optionRoots.Length > 0)
            {
                for (int i = 0; i < optionRoots.Length; i++)
                {
                    if (optionRoots[i] != null)
                        _options.Add(optionRoots[i]);
                }
            }
            else
            {
                for (int i = 0; i < transform.childCount; i++)
                {
                    Transform child = transform.GetChild(i);
                    if (child.name.StartsWith("Power", StringComparison.Ordinal))
                        _options.Add(child as RectTransform);
                }
                _options.Sort((a, b) => string.CompareOrdinal(a != null ? a.name : null, b != null ? b.name : null));
            }
            if (hideShowButton == null)
            {
                Transform hide = transform.Find("Hide/Show");
                if (hide != null)
                    hideShowButton = hide.GetComponent<Button>();
            }
            if (hideShowButton != null)
            {
                if (hideIcon == null)
                {
                    Transform icon = hideShowButton.transform.Find("Hide");
                    if (icon != null)
                        hideIcon = icon.gameObject;
                }
                if (showIcon == null)
                {
                    Transform icon = hideShowButton.transform.Find("Show");
                    if (icon != null)
                        showIcon = icon.gameObject;
                }
            }
            _descriptions.Clear();
            for (int i = 0; i < _options.Count; i++)
            {
                RectTransform option = _options[i];
                Transform box = option != null ? option.Find("DescriptionBox") : null;
                _descriptions.Add(box as RectTransform);
                if (option != null && option.sizeDelta.y < 8f)
                    option.sizeDelta = new Vector2(Mathf.Max(150f, option.sizeDelta.x), 32f);
                var stagger = option != null ? option.GetComponent<UiStagger>() : null;
                if (stagger != null)
                    stagger.enabled = false;
            }
            _collected = true;
        }
        void BindChoices(IReadOnlyList<DraftChoice> choices)
        {
            for (int i = 0; i < _options.Count; i++)
            {
                RectTransform option = _options[i];
                if (option == null)
                    continue;
                bool used = i < _used;
                Transform labelTf = option.Find("Text");
                TMP_Text label = labelTf != null ? labelTf.GetComponent<TMP_Text>() : null;
                TMP_Text body = DescriptionText(i);
                if (used && choices != null)
                {
                    if (label != null)
                        label.text = choices[i].Label;
                    if (body != null)
                        body.text = choices[i].Description;
                }
                var button = option.GetComponent<Button>();
                if (button == null)
                    continue;
                int index = i;
                if (used)
                    GameAudio.Bind(button, () => Pick(index));
                else
                    button.onClick.RemoveAllListeners();
                button.interactable = used;
            }
        }
        void HideOptions()
        {
            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i] != null)
                    _options[i].gameObject.SetActive(false);
            }
        }
        void PrepareDescriptions()
        {
            for (int i = 0; i < _descriptions.Count; i++)
            {
                RectTransform box = _descriptions[i];
                if (box == null)
                    continue;
                Vector2 size = box.sizeDelta;
                float rest = descriptionHeight > 0f ? descriptionHeight : size.y;
                if (rest < 8f)
                    rest = 300f;
                box.sizeDelta = new Vector2(size.x, 0f);
                box.gameObject.SetActive(true);
            }
        }
        void RevealButtons()
        {
            _buttonsShown = true;
            for (int i = 0; i < _options.Count; i++)
            {
                if (_options[i] == null)
                    continue;
                _options[i].gameObject.SetActive(i < _used && !_optionsHidden);
            }
            RefreshIcons();
        }
        void RiseDescriptions()
        {
            if (_optionsHidden)
                return;
            float time = UiAnimPrefs.MoveDuration(descriptionDuration);
            for (int i = 0; i < _used; i++)
            {
                RectTransform box = i < _descriptions.Count ? _descriptions[i] : null;
                if (box == null)
                    continue;
                float rest = descriptionHeight > 0f ? descriptionHeight : 300f;
                box.DOKill();
                if (time <= 0.001f)
                {
                    box.sizeDelta = new Vector2(box.sizeDelta.x, rest);
                    continue;
                }
                DOTween.To(
                        () => box.sizeDelta.y,
                        y => box.sizeDelta = new Vector2(box.sizeDelta.x, y),
                        rest,
                        time)
                    .SetEase(Ease.OutCubic)
                    .SetUpdate(true)
                    .SetTarget(box);
            }
        }
        void UnlockHideShow()
        {
            if (hideShowButton != null)
                hideShowButton.interactable = true;
        }
        void ToggleOptions()
        {
            if (!_buttonsShown)
                return;
            _optionsHidden = !_optionsHidden;
            for (int i = 0; i < _used; i++)
            {
                if (_options[i] != null)
                    _options[i].gameObject.SetActive(!_optionsHidden);
            }
            RefreshIcons();
        }
        void Pick(int index)
        {
            if (!_buttonsShown || _optionsHidden || index < 0 || index >= _used)
                return;
            _onPicked?.Invoke(index);
        }
        void RefreshIcons()
        {
            bool hidden = _optionsHidden;
            if (hideIcon != null)
                hideIcon.SetActive(!hidden);
            if (showIcon != null)
                showIcon.SetActive(hidden);
        }
        TMP_Text DescriptionText(int index)
        {
            if (index < 0 || index >= _descriptions.Count || _descriptions[index] == null)
                return null;
            return _descriptions[index].GetComponentInChildren<TMP_Text>(true);
        }
        void KillSequence()
        {
            _sequence?.Kill();
            _sequence = null;
            for (int i = 0; i < _descriptions.Count; i++)
            {
                if (_descriptions[i] != null)
                    _descriptions[i].DOKill();
            }
        }
        #endregion
    }

    public readonly struct DraftChoice
    {
        public readonly string Label;
        public readonly string Description;
        public DraftChoice(string label, string description)
        {
            Label = label ?? string.Empty;
            Description = description ?? string.Empty;
        }
    }
}
