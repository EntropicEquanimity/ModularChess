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

        [SerializeField] RectTransform clip;
        [SerializeField] RectTransform panel;
        [SerializeField] Image blocker;
        [SerializeField] Button blockerButton;
        [SerializeField] Button closeButton;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text summary;
        [SerializeField] GameObject fields;
        [SerializeField] GameObject settingsControlPrefab;

        Tween _tween;
        bool _open;
        bool _wired;
        ModeId _id;
        HostModeSettings _settings;

        public bool IsOpen => _open;

        public static ModeSettingsPopup Ensure(Transform overlayRoot)
        {
            ModeSettingsPopup existing = overlayRoot.GetComponentInChildren<ModeSettingsPopup>(true);
            if (existing != null && existing.gameObject.scene.IsValid())
                return existing;

            GameObject prefab = RuntimePrefabs.ModeSettingsPopup;
            if (prefab == null || overlayRoot == null || !overlayRoot.gameObject.scene.IsValid())
                return null;

            GameObject go = UnityEngine.Object.Instantiate(prefab, overlayRoot);
            go.name = "ModeSettingsPopup";
            go.SetActive(false);
            return go.GetComponent<ModeSettingsPopup>();
        }

        public void Open(ModeId id, HostModeSettings settings, RectTransform slideFrom)
        {
            if (settings == null)
                throw new ArgumentNullException(nameof(settings));

            Wire();
            _id = id;
            _settings = settings;
            _tween?.Kill();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlaceClip(slideFrom);
            Fill(id, settings);
            _open = true;

            Vector2 hidden = HiddenPos();
            Vector2 shown = ShownPos();
            panel.anchoredPosition = hidden;
            SetBlockerAlpha(0f);
            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(DOTween.To(() => panel.anchoredPosition, v => panel.anchoredPosition = v, shown, Duration).SetEase(Ease.OutCubic));
            sequence.Join(DOTween.To(() => blocker.color.a, SetBlockerAlpha, 0.45f, Duration).SetEase(Ease.OutQuad));
            _tween = sequence;
        }

        public void Close()
        {
            if (!_open && !gameObject.activeSelf)
                return;

            _open = false;
            _tween?.Kill();
            if (panel == null)
            {
                gameObject.SetActive(false);
                return;
            }

            Sequence sequence = DOTween.Sequence().SetUpdate(true).SetTarget(this);
            sequence.Join(DOTween.To(() => panel.anchoredPosition, v => panel.anchoredPosition = v, HiddenPos(), Duration).SetEase(Ease.InCubic));
            sequence.Join(DOTween.To(() => blocker.color.a, SetBlockerAlpha, 0f, Duration).SetEase(Ease.InQuad));
            sequence.OnComplete(() => gameObject.SetActive(false));
            _tween = sequence;
        }

        public void HideImmediate()
        {
            _tween?.Kill();
            _open = false;
            if (panel != null)
                panel.anchoredPosition = HiddenPos();
            SetBlockerAlpha(0f);
            gameObject.SetActive(false);
        }

        void Awake()
        {
            Wire();
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

        void Wire()
        {
            if (_wired)
                return;

            if (clip == null)
                clip = transform.Find("Clip") as RectTransform;
            if (panel == null && clip != null)
                panel = clip.Find("Panel") as RectTransform;
            if (blocker == null)
            {
                Transform blockerTransform = transform.Find("Blocker");
                if (blockerTransform != null)
                    blocker = blockerTransform.GetComponent<Image>();
            }

            if (blockerButton == null && blocker != null)
                blockerButton = blocker.GetComponent<Button>();
            if (closeButton == null && panel != null)
            {
                Transform closeTransform = panel.Find("Close");
                if (closeTransform != null)
                    closeButton = closeTransform.GetComponent<Button>();
            }

            if (title == null && panel != null)
            {
                Transform titleTransform = panel.Find("Label");
                if (titleTransform != null)
                    title = titleTransform.GetComponent<TMP_Text>();
            }

            if (summary == null && panel != null)
            {
                Transform summaryTransform = panel.Find("DescriptionBox/Text");
                if (summaryTransform != null)
                    summary = summaryTransform.GetComponent<TMP_Text>();
            }

            if (fields == null && panel != null)
            {
                Transform fieldsTransform = panel.Find("Fields");
                if (fieldsTransform != null)
                    fields = fieldsTransform.gameObject;
            }

            if (settingsControlPrefab == null)
                settingsControlPrefab = RuntimePrefabs.SettingsControl;

            if (blockerButton != null)
                GameAudio.Bind(blockerButton, Close);
            if (closeButton != null)
            {
                GameAudio.Bind(closeButton, Close);
                LocalizedText.Bind(closeButton, "mode.settings.close");
            }

            Loc.Changed -= OnLanguageChanged;
            Loc.Changed += OnLanguageChanged;
            _wired = true;
        }

        void OnLanguageChanged()
        {
            if (_open && _settings != null)
                Fill(_id, _settings);
        }

        void Fill(ModeId id, HostModeSettings settings)
        {
            if (title != null)
                title.text = Loc.ModeName(id);
            if (summary != null)
                summary.text = Loc.ModeSummary(id);

            Transform content = FieldsContent();
            if (content != null)
            {
                for (int i = content.childCount - 1; i >= 0; i--)
                {
                    GameObject child = content.GetChild(i).gameObject;
                    if (Application.isPlaying)
                        Destroy(child);
                    else
                        DestroyImmediate(child);
                }
            }

            bool hasFields = id != ModeId.FogOfWar;
            if (fields != null)
                fields.SetActive(hasFields);
            if (!hasFields)
                return;

            switch (id)
            {
                case ModeId.FogOfWar:
                    return;
                case ModeId.PowerfulPieces:
                    AddStepper(content, Loc.Get("mode.setting.empowered"), () => settings.EmpoweredCount, v => settings.EmpoweredCount = v, 1, 8);
                    break;
                case ModeId.Martyr:
                    AddStepper(content, Loc.Get("mode.setting.lost"), () => settings.MartyrThreshold, v => settings.MartyrThreshold = v, 1, 18);
                    AddStepper(content, Loc.Get("mode.setting.draft"), () => settings.MartyrDraftOptions, v => settings.MartyrDraftOptions = v, 1, 5);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }

            FitFieldsToContent();
        }

        void AddStepper(Transform content, string label, Func<int> get, Action<int> set, int min, int max)
        {
            if (content == null || !content.gameObject.scene.IsValid() || settingsControlPrefab == null)
                return;

            GameObject go = Instantiate(settingsControlPrefab, content);
            go.name = label;
            go.SetActive(true);
            SettingsControl control = go.GetComponent<SettingsControl>();
            if (control == null)
                control = go.AddComponent<SettingsControl>();
            control.Bind(label, get, set, min, max);
        }

        void FitFieldsToContent()
        {
            Transform content = FieldsContent();
            if (fields == null || content == null)
                return;

            var contentRect = (RectTransform)content;
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            float height = LayoutUtility.GetPreferredHeight(contentRect);
            if (height <= 0f)
                height = contentRect.rect.height;
            if (height <= 0f)
            {
                int n = contentRect.childCount;
                height = n * 40f + 8f;
            }

            var element = fields.GetComponent<LayoutElement>();
            if (element == null)
                element = fields.AddComponent<LayoutElement>();
            element.minHeight = height;
            element.preferredHeight = height;

            var fieldsRect = (RectTransform)fields.transform;
            fieldsRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);

            if (panel != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        }

        Transform FieldsContent()
        {
            if (fields == null)
                return null;
            ScrollRect scroll = fields.GetComponent<ScrollRect>();
            if (scroll != null && scroll.content != null)
                return scroll.content;
            Transform viewport = fields.transform.Find("Viewport");
            if (viewport != null)
                return viewport.Find("Content");
            return null;
        }

        void PlaceClip(RectTransform slideFrom)
        {
            if (clip == null || slideFrom == null)
                return;

            var parent = (RectTransform)clip.parent;
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
                (clip.anchorMin.x - parent.pivot.x) * parent.rect.width,
                clip.anchoredPosition.y);
            clip.anchoredPosition = new Vector2(local.x - pivotOffset.x, clip.anchoredPosition.y);
        }

        void SetBlockerAlpha(float alpha)
        {
            if (blocker == null)
                return;
            Color color = blocker.color;
            color.a = alpha;
            blocker.color = color;
        }

        Vector2 HiddenPos()
        {
            float width = panel != null ? Mathf.Max(panel.rect.width, panel.sizeDelta.x) : 240f;
            Vector2 shown = ShownPos();
            return new Vector2(-width, shown.y);
        }

        Vector2 ShownPos()
        {
            if (panel == null)
                return Vector2.zero;
            return new Vector2(0f, panel.anchoredPosition.y);
        }
    }
}
