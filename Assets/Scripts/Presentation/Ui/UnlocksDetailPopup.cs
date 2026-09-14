using System;
using DG.Tweening;
using ModularChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class UnlocksDetailPopup : MonoBehaviour
    {
        const float Duration = 0.28f;
        const float PanelWidth = 420f;
        const float PanelHeight = 320f;

        RectTransform _clip;
        RectTransform _panel;
        Image _blocker;
        TMP_Text _title;
        TMP_Text _summary;
        Button _buy;
        Tween _tween;
        ModeId _id;
        Action _onChanged;
        bool _open;

        public bool IsOpen => _open;

        public static UnlocksDetailPopup Ensure(Transform overlayRoot)
        {
            UnlocksDetailPopup existing = overlayRoot.GetComponentInChildren<UnlocksDetailPopup>(true);
            if (existing != null)
                return existing;

            var go = new GameObject("UnlocksDetailPopup", typeof(RectTransform), typeof(UnlocksDetailPopup));
            go.transform.SetParent(overlayRoot, false);
            UnlocksDetailPopup popup = go.GetComponent<UnlocksDetailPopup>();
            popup.Build();
            go.SetActive(false);
            return popup;
        }

        public void Open(ModeId id, Action onChanged)
        {
            _id = id;
            _onChanged = onChanged;
            Loc.Changed -= OnLanguageChanged;
            Loc.Changed += OnLanguageChanged;
            _tween?.Kill();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            Fill();
            _open = true;
            _panel.anchoredPosition = HiddenPos();
            SetBlockerAlpha(0f);
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(DOTween.To(() => _panel.anchoredPosition, v => _panel.anchoredPosition = v, Vector2.zero, Duration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => _blocker.color.a, SetBlockerAlpha, 0.45f, Duration).SetEase(Ease.OutQuad));
            _tween = sequence;
        }

        public void Close()
        {
            if (!_open && !gameObject.activeSelf)
                return;
            _open = false;
            _tween?.Kill();
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(DOTween.To(() => _panel.anchoredPosition, v => _panel.anchoredPosition = v, HiddenPos(), Duration).SetEase(Ease.InCubic));
            sequence.Join(DOTween.To(() => _blocker.color.a, SetBlockerAlpha, 0f, Duration).SetEase(Ease.InQuad));
            sequence.OnComplete(() => gameObject.SetActive(false));
            _tween = sequence;
        }

        public void HideImmediate()
        {
            _tween?.Kill();
            _open = false;
            if (_panel != null)
                _panel.anchoredPosition = HiddenPos();
            SetBlockerAlpha(0f);
            gameObject.SetActive(false);
        }

        void OnLanguageChanged()
        {
            if (_open)
                Fill();
        }

        void OnDisable()
        {
            Loc.Changed -= OnLanguageChanged;
            _tween?.Kill();
            _open = false;
        }

        void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            _tween?.Kill();
        }

        void Build()
        {
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.pivot = new Vector2(0.5f, 0.5f);
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var blockerGo = new GameObject("Blocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            blockerGo.transform.SetParent(transform, false);
            _blocker = blockerGo.GetComponent<Image>();
            _blocker.color = new Color(0f, 0f, 0f, 0f);
            _blocker.raycastTarget = true;
            var blockerRect = (RectTransform)blockerGo.transform;
            blockerRect.anchorMin = Vector2.zero;
            blockerRect.anchorMax = Vector2.one;
            blockerRect.offsetMin = Vector2.zero;
            blockerRect.offsetMax = Vector2.zero;

            var clipGo = new GameObject("Clip", typeof(RectTransform), typeof(RectMask2D));
            clipGo.transform.SetParent(transform, false);
            _clip = (RectTransform)clipGo.transform;
            _clip.anchorMin = new Vector2(0.5f, 0.5f);
            _clip.anchorMax = new Vector2(0.5f, 0.5f);
            _clip.pivot = new Vector2(0.5f, 0.5f);
            _clip.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            RectTransform panel = UiFactory.Panel(_clip, new Vector2(PanelWidth, PanelHeight));
            _panel = panel;
            _panel.anchorMin = new Vector2(0.5f, 0.5f);
            _panel.anchorMax = new Vector2(0.5f, 0.5f);
            _panel.pivot = new Vector2(0.5f, 0.5f);
            var layout = _panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _title = UiFactory.Label(_panel, "Mode", 32, TextAlignmentOptions.Center);
            _title.color = Color.black;
            var titleElement = _title.gameObject.AddComponent<LayoutElement>();
            titleElement.minHeight = 40;
            titleElement.preferredHeight = 40;

            _summary = UiFactory.DescriptionBox(_panel, string.Empty, new Vector2(PanelWidth - 32f, 96f));
            _summary.fontSize = 16;
            RectTransform summaryBox = _summary.rectTransform.parent as RectTransform ?? _summary.rectTransform;
            var summaryElement = summaryBox.gameObject.AddComponent<LayoutElement>();
            summaryElement.minHeight = 96;
            summaryElement.preferredHeight = 96;
            summaryElement.flexibleHeight = 1;

            _buy = UiFactory.Button(_panel, Loc.Get("unlocks.buy"), Buy, new Vector2(200f, 32f));
            var buyElement = _buy.gameObject.AddComponent<LayoutElement>();
            buyElement.minWidth = 200f;
            buyElement.preferredWidth = 200f;
            buyElement.minHeight = 32f;
            buyElement.preferredHeight = 32f;

            Button close = UiFactory.Button(_panel, Loc.Get("unlocks.close"), Close, new Vector2(200f, 32f));
            LocalizedText.Bind(close, "unlocks.close");
            var closeElement = close.gameObject.AddComponent<LayoutElement>();
            closeElement.minWidth = 200f;
            closeElement.preferredWidth = 200f;
            closeElement.minHeight = 32f;
            closeElement.preferredHeight = 32f;
        }

        void Fill()
        {
            _title.text = Loc.ModeName(_id);
            _summary.text = Loc.ModeSummary(_id);
            bool owned = ModeDlc.IsOwned(_id);
            _buy.interactable = !owned;
            TMP_Text label = _buy.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = owned ? Loc.Get("unlocks.unlocked") : Loc.Get("unlocks.buy");
        }

        void Buy()
        {
            if (ModeDlc.IsOwned(_id))
                return;
            ModeDlc.Purchase(_id);
            Fill();
            _onChanged?.Invoke();
        }

        void SetBlockerAlpha(float alpha)
        {
            if (_blocker == null)
                return;
            Color color = _blocker.color;
            color.a = alpha;
            _blocker.color = color;
        }

        static Vector2 HiddenPos()
        {
            return new Vector2(PanelWidth, 0f);
        }
    }
}
