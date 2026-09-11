using System;
using DG.Tweening;
using ModularChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class HostModeSettings
    {
        public int EmpoweredCount = 2;
        public int MartyrThreshold = 6;
        public int MartyrDraftOptions = 3;
    }

    public sealed class ModeSettingsPopup : MonoBehaviour
    {
        const float Duration = 0.28f;
        const float PanelWidth = 240f;
        const float PanelHeight = 320f;

        RectTransform _clip;
        RectTransform _panel;
        Image _blocker;
        TMP_Text _title;
        TMP_Text _summary;
        Transform _fields;
        Tween _tween;
        bool _open;

        public bool IsOpen => _open;

        public static ModeSettingsPopup Ensure(Transform overlayRoot)
        {
            ModeSettingsPopup existing = overlayRoot.GetComponentInChildren<ModeSettingsPopup>(true);
            if (existing != null)
                return existing;

            var go = new GameObject("ModeSettingsPopup", typeof(RectTransform), typeof(ModeSettingsPopup));
            go.transform.SetParent(overlayRoot, false);
            ModeSettingsPopup popup = go.GetComponent<ModeSettingsPopup>();
            popup.Build();
            go.SetActive(false);
            return popup;
        }

        public void Open(ModeId id, HostModeSettings settings, RectTransform slideFrom)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            _tween?.Kill();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlaceClip(slideFrom);
            Fill(id, settings);
            _open = true;

            Vector2 hidden = HiddenPos();
            Vector2 shown = Vector2.zero;
            _panel.anchoredPosition = hidden;
            SetBlockerAlpha(0f);
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(DOTween.To(() => _panel.anchoredPosition, v => _panel.anchoredPosition = v, shown, Duration).SetEase(Ease.OutCubic));
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

        void OnDisable()
        {
            _tween?.Kill();
            _open = false;
        }

        void OnDestroy()
        {
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
            _clip.pivot = new Vector2(0f, 0.5f);
            _clip.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var panelGo = new GameObject("Panel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(VerticalLayoutGroup));
            panelGo.transform.SetParent(_clip, false);
            _panel = (RectTransform)panelGo.transform;
            _panel.anchorMin = new Vector2(0f, 0.5f);
            _panel.anchorMax = new Vector2(0f, 0.5f);
            _panel.pivot = new Vector2(0f, 0.5f);
            _panel.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            _panel.anchoredPosition = HiddenPos();
            var panelImage = panelGo.GetComponent<Image>();
            panelImage.color = new Color(0.1f, 0.1f, 0.12f, 1f);
            panelImage.raycastTarget = true;
            var layout = panelGo.GetComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            _title = UiFactory.Label(_panel, "Mode", 16, TextAlignmentOptions.Center);
            StretchLabel(_title, 32);

            _summary = UiFactory.Label(_panel, string.Empty, 16, TextAlignmentOptions.TopLeft);
            _summary.enableWordWrapping = true;
            StretchLabel(_summary, 80);

            var fieldsGo = new GameObject("Fields", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            fieldsGo.transform.SetParent(_panel, false);
            _fields = fieldsGo.transform;
            var fieldsLayout = fieldsGo.GetComponent<VerticalLayoutGroup>();
            fieldsLayout.spacing = 8;
            fieldsLayout.childAlignment = TextAnchor.UpperCenter;
            fieldsLayout.childControlWidth = true;
            fieldsLayout.childControlHeight = false;
            fieldsLayout.childForceExpandWidth = true;
            fieldsLayout.childForceExpandHeight = false;
            var fieldsElement = fieldsGo.GetComponent<LayoutElement>();
            fieldsElement.flexibleHeight = 1;
            fieldsElement.minHeight = 40;

            Button close = UiFactory.Button(_panel, "Close", Close, new Vector2(200f, 32f));
            var closeElement = close.gameObject.AddComponent<LayoutElement>();
            closeElement.minWidth = 200f;
            closeElement.preferredWidth = 200f;
            closeElement.minHeight = 32f;
            closeElement.preferredHeight = 32f;
        }

        void Fill(ModeId id, HostModeSettings settings)
        {
            ModeDefinition def = ModeCatalog.Get(id);
            _title.text = def.DisplayName;
            _summary.text = def.Summary;

            for (int i = _fields.childCount - 1; i >= 0; i--)
                DestroyImmediate(_fields.GetChild(i).gameObject);

            switch (id)
            {
                case ModeId.FogOfWar:
                    break;
                case ModeId.PowerfulPieces:
                    AddStepper("Empowered Pieces", () => settings.EmpoweredCount, v => settings.EmpoweredCount = v, 1, 8);
                    break;
                case ModeId.Martyr:
                    AddStepper("Lost Material", () => settings.MartyrThreshold, v => settings.MartyrThreshold = v, 1, 18);
                    AddStepper("Draft options", () => settings.MartyrDraftOptions, v => settings.MartyrDraftOptions = v, 1, 5);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }

        void AddStepper(string label, Func<int> get, Action<int> set, int min, int max)
        {
            var row = new GameObject(label, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(LayoutElement));
            row.transform.SetParent(_fields, false);
            var column = row.GetComponent<VerticalLayoutGroup>();
            column.spacing = 4;
            column.childAlignment = TextAnchor.MiddleCenter;
            column.childControlWidth = true;
            column.childControlHeight = false;
            column.childForceExpandWidth = true;
            column.childForceExpandHeight = false;
            row.GetComponent<LayoutElement>().minHeight = 56;

            UiFactory.Label(row.transform, label, 16, TextAlignmentOptions.Center);

            var controls = new GameObject("Controls", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
            controls.transform.SetParent(row.transform, false);
            var horizontal = controls.GetComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 8;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childControlWidth = false;
            horizontal.childControlHeight = false;
            horizontal.childForceExpandWidth = false;
            horizontal.childForceExpandHeight = false;
            controls.GetComponent<LayoutElement>().minHeight = 32;

            TMP_Text valueLabel = null;
            void Refresh()
            {
                if (valueLabel != null)
                    valueLabel.text = get().ToString();
            }

            Button minus = UiFactory.Button(controls.transform, "-", () =>
            {
                set(Mathf.Max(min, get() - 1));
                Refresh();
            }, new Vector2(32f, 32f));
            minus.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);

            valueLabel = UiFactory.Label(controls.transform, get().ToString(), 16, TextAlignmentOptions.Center);
            var valueRect = valueLabel.rectTransform;
            valueRect.sizeDelta = new Vector2(48f, 32f);

            Button plus = UiFactory.Button(controls.transform, "+", () =>
            {
                set(Mathf.Min(max, get() + 1));
                Refresh();
            }, new Vector2(32f, 32f));
            plus.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
        }

        void PlaceClip(RectTransform slideFrom)
        {
            if (_clip == null)
                return;

            _clip.sizeDelta = new Vector2(PanelWidth, PanelHeight);
            if (slideFrom == null)
            {
                _clip.anchoredPosition = Vector2.zero;
                return;
            }

            var parent = (RectTransform)_clip.parent;
            var corners = new Vector3[4];
            slideFrom.GetWorldCorners(corners);
            Vector3 rightMid = (corners[2] + corners[3]) * 0.5f;
            Camera camera = null;
            Canvas canvas = parent.GetComponentInParent<Canvas>();
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                camera = canvas.worldCamera;

            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, rightMid);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent, screen, camera, out Vector2 local))
                return;

            Vector2 pivotOffset = new Vector2(
                (_clip.anchorMin.x - parent.pivot.x) * parent.rect.width,
                (_clip.anchorMin.y - parent.pivot.y) * parent.rect.height);
            _clip.anchoredPosition = local - pivotOffset;
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
            return new Vector2(-PanelWidth, 0f);
        }

        static void StretchLabel(TMP_Text label, float height)
        {
            var element = label.gameObject.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;
            label.raycastTarget = false;
        }
    }
}
