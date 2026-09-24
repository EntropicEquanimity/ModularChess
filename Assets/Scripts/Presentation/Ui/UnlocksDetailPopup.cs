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
        #region Fields
        const float Duration = 0.28f;
        [SerializeField] RectTransform clip;
        [SerializeField] RectTransform panel;
        [SerializeField] Button closeButton;
        [SerializeField] Button buyButton;
        [SerializeField] TMP_Text title;
        [SerializeField] TMP_Text summary;
        Tween _tween;
        bool _open;
        bool _wired;
        ModeId _id;
        UnlockProduct _product;
        Action _onChanged;
        public bool IsOpen => _open;
        #endregion

        #region Unity
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
        #endregion

        #region Public Methods
        public static UnlocksDetailPopup Ensure(Transform overlayRoot)
        {
            UnlocksDetailPopup existing = overlayRoot.GetComponentInChildren<UnlocksDetailPopup>(true);
            if (existing != null)
                return existing;
            GameObject prefab = RuntimePrefabs.UnlocksDetailPopup;
            if (prefab == null)
                return null;
            GameObject go = UnityEngine.Object.Instantiate(prefab, overlayRoot);
            go.name = "UnlocksDetailPopup";
            go.SetActive(false);
            return go.GetComponent<UnlocksDetailPopup>();
        }
        public void Open(UnlockProduct product, Action onChanged, RectTransform slideFrom)
        {
            Wire();
            _product = product;
            ModeId? mode = MeritUnlocks.AsMode(product);
            if (mode.HasValue)
                _id = mode.Value;
            _onChanged = onChanged;
            Loc.Changed -= OnLanguageChanged;
            Loc.Changed += OnLanguageChanged;
            _tween?.Kill();
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            PlaceClip(slideFrom);
            Fill();
            _open = true;
            if (panel == null)
                return;
            Vector2 hidden = HiddenPos();
            Vector2 shown = ShownPos();
            panel.anchoredPosition = hidden;
            float time = UiAnimPrefs.MoveDuration(Duration);
            if (time <= 0.001f)
            {
                panel.anchoredPosition = shown;
                return;
            }
            _tween = DOTween.To(() => panel.anchoredPosition, v => panel.anchoredPosition = v, shown, time)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(this);
        }
        public void Open(ModeId id, Action onChanged, RectTransform slideFrom)
        {
            UnlockProduct product = UnlockProduct.ModeFogOfWar;
            if (id == ModeId.PowerfulPieces) product = UnlockProduct.ModePowerfulPieces;
            else if (id == ModeId.Martyr) product = UnlockProduct.ModeMartyr;
            Open(product, onChanged, slideFrom);
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
            float time = UiAnimPrefs.MoveDuration(Duration);
            if (time <= 0.001f)
            {
                panel.anchoredPosition = HiddenPos();
                gameObject.SetActive(false);
                return;
            }
            _tween = DOTween.To(() => panel.anchoredPosition, v => panel.anchoredPosition = v, HiddenPos(), time)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetTarget(this)
                .OnComplete(() => gameObject.SetActive(false));
        }
        public void HideImmediate()
        {
            _tween?.Kill();
            _open = false;
            if (panel != null)
                panel.anchoredPosition = HiddenPos();
            gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        void Wire()
        {
            if (_wired)
                return;
            if (clip == null)
                clip = transform.Find("Clip") as RectTransform;
            if (panel == null && clip != null)
                panel = clip.Find("Panel") as RectTransform;
            if (panel == null)
                panel = transform.Find("Panel") as RectTransform;
            if (clip == null && panel != null && panel.parent != transform)
                clip = panel.parent as RectTransform;
            EnsureClip();
            if (title == null && panel != null)
            {
                Transform label = panel.Find("Label");
                if (label != null)
                    title = label.GetComponent<TMP_Text>();
            }
            if (summary == null && panel != null)
            {
                Transform box = panel.Find("DescriptionBox/Text");
                if (box != null)
                    summary = box.GetComponent<TMP_Text>();
            }
            if (buyButton == null && panel != null)
            {
                Transform buy = panel.Find("Buy");
                if (buy != null)
                    buyButton = buy.GetComponent<Button>();
            }
            if (closeButton == null && panel != null)
            {
                Transform close = panel.Find("Close");
                if (close != null)
                    closeButton = close.GetComponent<Button>();
            }
            if (buyButton != null)
                GameAudio.Bind(buyButton, Buy);
            if (closeButton != null)
            {
                GameAudio.Bind(closeButton, Close);
                LocalizedText.Bind(closeButton, "unlocks.close");
            }
            Loc.Changed -= OnLanguageChanged;
            Loc.Changed += OnLanguageChanged;
            _wired = true;
        }
        void EnsureClip()
        {
            if (clip != null || panel == null)
                return;
            var clipGo = new GameObject("Clip", typeof(RectTransform), typeof(RectMask2D));
            clipGo.transform.SetParent(transform, false);
            clip = (RectTransform)clipGo.transform;
            clip.anchorMin = new Vector2(0f, 0f);
            clip.anchorMax = new Vector2(0f, 1f);
            clip.pivot = new Vector2(0f, 0.5f);
            clip.sizeDelta = new Vector2(Mathf.Max(panel.sizeDelta.x, 300f), 0f);
            clip.anchoredPosition = new Vector2(580f, 0f);
            panel.SetParent(clip, false);
            panel.anchorMin = new Vector2(0f, 0.5f);
            panel.anchorMax = new Vector2(0f, 0.5f);
            panel.pivot = new Vector2(0f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
        }
        void OnLanguageChanged()
        {
            if (_open)
                Fill();
        }
        void Fill()
        {
            if (title != null)
                title.text = Loc.UnlockName(_product);
            if (summary != null)
            {
                string costLine = Loc.Format("unlocks.cost", MeritUnlocks.Cost(_product), MeritWallet.Balance);
                summary.text = Loc.UnlockSummary(_product) + "\n" + costLine;
            }
            bool owned = MeritUnlocks.IsOwned(_product);
            bool canBuy = MeritUnlocks.CanPurchase(_product);
            if (buyButton != null)
            {
                buyButton.interactable = canBuy;
                TMP_Text label = buyButton.GetComponentInChildren<TMP_Text>();
                if (label != null)
                {
                    if (owned)
                        label.text = Loc.Get("unlocks.unlocked");
                    else if (!MeritUnlocks.CanPurchase(_product) && MeritWallet.Balance < MeritUnlocks.Cost(_product))
                        label.text = Loc.Get("unlocks.needMerit");
                    else
                        label.text = Loc.Get("unlocks.buy");
                }
            }
        }
        void Buy()
        {
            if (!MeritUnlocks.TryPurchase(_product))
                return;
            Fill();
            _onChanged?.Invoke();
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
        Vector2 HiddenPos()
        {
            float width = panel != null ? Mathf.Max(panel.rect.width, panel.sizeDelta.x, 300f) : 300f;
            Vector2 shown = ShownPos();
            return new Vector2(-width, shown.y);
        }
        Vector2 ShownPos()
        {
            if (panel == null)
                return Vector2.zero;
            return new Vector2(0f, panel.anchoredPosition.y);
        }
        #endregion
    }
}
