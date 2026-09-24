using System;
using ModularChess.Core;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class UnlocksView : MonoBehaviour
    {
        #region Fields
        [SerializeField] Transform content;
        [SerializeField] UnlockRow rowPrefab;
        [SerializeField] UnlocksDetailPopup detail;
        [SerializeField] RectTransform slideFrom;
        [SerializeField] Button backButton;
        UnityAction _onBack;
        #endregion

        #region Unity
        void Awake()
        {
            Wire();
            if (detail != null)
                detail.gameObject.SetActive(false);
        }
        void OnEnable()
        {
            Loc.Changed += Refresh;
            Refresh();
        }
        void OnDisable()
        {
            Loc.Changed -= Refresh;
        }
        #endregion

        #region Public Methods
        public void Bind(UnityAction onBack)
        {
            Wire();
            _onBack = onBack;
            GameAudio.Bind(backButton, _onBack);
            LocalizedText.Bind(backButton, "menu.back");
        }
        public void Refresh()
        {
            Wire();
            if (content == null || rowPrefab == null)
                return;
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);
                UnlockProduct[] catalog = MeritUnlocks.All;
            for (int i = 0; i < catalog.Length; i++)
            {
                UnlockRow row = Instantiate(rowPrefab, content);
                row.gameObject.SetActive(true);
                row.Bind(catalog[i], OpenDetail);
            }
        }
        public bool CloseDetailIfOpen()
        {
            if (detail != null && detail.IsOpen)
            {
                detail.Close();
                return true;
            }
            return false;
        }
        public void HideDetailImmediate()
        {
            detail?.HideImmediate();
        }
        #endregion

        #region Private Methods
        void Wire()
        {
            if (content == null)
            {
                ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
                if (scroll != null)
                    content = scroll.content;
            }
            if (detail == null)
                detail = GetComponentInChildren<UnlocksDetailPopup>(true);
            if (detail == null)
                detail = UnlocksDetailPopup.Ensure(transform);
            if (slideFrom == null)
            {
                Transform named = transform.Find("ButtonGroup/ModesScroll");
                if (named == null)
                {
                    ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
                    if (scroll != null)
                        named = scroll.transform;
                }
                if (named != null)
                    slideFrom = named as RectTransform;
            }
            if (backButton == null)
            {
                Transform named = FindChild(transform, "BackButton");
                if (named != null)
                    backButton = named.GetComponent<Button>();
            }
        }
        static Transform FindChild(Transform root, string name)
        {
            if (root == null)
                return null;
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }
        void OpenDetail(UnlockProduct product)
        {
            Wire();
            if (detail == null)
                return;
            detail.Open(product, RefreshLocks, slideFrom);
        }
        void RefreshLocks()
        {
            UnlockRow[] rows = content != null ? content.GetComponentsInChildren<UnlockRow>(true) : System.Array.Empty<UnlockRow>();
            for (int i = 0; i < rows.Length; i++)
                rows[i].Refresh();
        }
        #endregion
    }
}
