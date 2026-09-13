using ModularChess.Core;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class UnlocksView : MonoBehaviour
    {
        [SerializeField] Transform content;
        [SerializeField] UnlockRow rowPrefab;
        UnlocksDetailPopup _detail;

        void OnEnable()
        {
            Refresh();
        }

        public void Refresh()
        {
            if (content == null)
            {
                ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
                if (scroll != null)
                    content = scroll.content;
            }

            if (content == null || rowPrefab == null)
                return;

            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            ModeDefinition[] modes = ModeCatalog.All;
            for (int i = 0; i < modes.Length; i++)
            {
                UnlockRow row = Instantiate(rowPrefab, content);
                row.gameObject.SetActive(true);
                row.Bind(modes[i], OpenDetail);
            }
        }

        public bool CloseDetailIfOpen()
        {
            if (_detail != null && _detail.IsOpen)
            {
                _detail.Close();
                return true;
            }

            return false;
        }

        public void HideDetailImmediate()
        {
            _detail?.HideImmediate();
        }

        void OpenDetail(ModeId id)
        {
            _detail = UnlocksDetailPopup.Ensure(transform);
            _detail.Open(id, RefreshLocks);
        }

        void RefreshLocks()
        {
            UnlockRow[] rows = content != null ? content.GetComponentsInChildren<UnlockRow>(true) : System.Array.Empty<UnlockRow>();
            for (int i = 0; i < rows.Length; i++)
                rows[i].Refresh();
        }
    }
}
