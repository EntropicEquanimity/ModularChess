using System.Collections.Generic;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class HistoryOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] Button backButton;
        [SerializeField] Button replayButton;
        [SerializeField] Transform title;
        [SerializeField] Transform listParent;
        [SerializeField] TMP_Text emptyLabel;
        [SerializeField] HistoryRowView rowPrefab;
        readonly List<HistoryRowView> _rows = new List<HistoryRowView>();
        MatchHistoryRecord[] _records = new MatchHistoryRecord[0];
        int _selected = -1;
        UnityAction _onBack;
        UnityAction<MatchHistoryRecord> _onReplay;
        #endregion

        #region Public Methods
        public int SelectedIndex => _selected;
        public void Bind(UnityAction onBack, UnityAction<MatchHistoryRecord> onReplay)
        {
            Resolve();
            _onBack = onBack;
            _onReplay = onReplay;
            GameAudio.Bind(backButton, () => _onBack?.Invoke());
            GameAudio.Bind(replayButton, StartReplay);
            RefreshLoc();
            Refresh();
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(title, "menu.history");
            LocalizedText.Bind(backButton, "menu.back");
            LocalizedText.Bind(replayButton, "hud.replay");
            if (emptyLabel != null)
                emptyLabel.text = Loc.Get("history.empty");
        }
        public void Refresh()
        {
            Resolve();
            _records = MatchHistoryStore.ListNewestFirst();
            if (_selected >= _records.Length)
                _selected = -1;
            RebuildRows();
            UpdateReplayButton();
        }
        public void SelectIndex(int index)
        {
            if (index < 0 || index >= _records.Length) return;
            _selected = index;
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].SetSelected(i == _selected);
            UpdateReplayButton();
        }
        #endregion

        #region Private Methods
        void StartReplay()
        {
            if (_selected < 0 || _selected >= _records.Length) return;
            MatchHistoryRecord record = _records[_selected];
            if (record == null || !record.Replayable) return;
            _onReplay?.Invoke(record);
        }
        void UpdateReplayButton()
        {
            if (replayButton == null) return;
            bool ok = _selected >= 0
                && _selected < _records.Length
                && _records[_selected] != null
                && _records[_selected].Replayable;
            replayButton.interactable = ok;
        }
        void RebuildRows()
        {
            ClearRows();
            bool empty = _records == null || _records.Length == 0;
            if (emptyLabel != null)
                emptyLabel.gameObject.SetActive(empty);
            if (empty || listParent == null) return;
            for (int i = 0; i < _records.Length; i++)
            {
                HistoryRowView row = CreateRow();
                row.Bind(_records[i], i, i == _selected, SelectIndex);
                _rows.Add(row);
            }
        }
        HistoryRowView CreateRow()
        {
            if (rowPrefab != null)
            {
                HistoryRowView instance = Instantiate(rowPrefab, listParent);
                instance.gameObject.SetActive(true);
                return instance;
            }
            var go = new GameObject("HistoryRow", typeof(RectTransform), typeof(Image), typeof(HistoryRowView));
            go.transform.SetParent(listParent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(0f, 40f);
            LayoutElement layout = go.AddComponent<LayoutElement>();
            layout.minHeight = 40f;
            layout.preferredHeight = 40f;
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(12f, 4f);
            labelRect.offsetMax = new Vector2(-12f, -4f);
            TMP_Text text = labelGo.AddComponent<TextMeshProUGUI>();
            text.fontSize = 24f;
            text.color = Color.black;
            text.raycastTarget = false;
            return go.GetComponent<HistoryRowView>();
        }
        void ClearRows()
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }
            _rows.Clear();
        }
        void Resolve()
        {
            if (backButton == null)
            {
                Transform child = FindChild(transform, "BackButton");
                if (child != null)
                    backButton = child.GetComponent<Button>();
            }
            if (listParent == null)
            {
                Transform scroll = FindChild(transform, "HistoryScroll");
                ScrollRect rect = scroll != null ? scroll.GetComponent<ScrollRect>() : null;
                if (rect != null && rect.content != null)
                    listParent = rect.content;
                else
                {
                    Transform content = FindChild(transform, "Content");
                    if (content != null)
                        listParent = content;
                    else
                        listParent = FindChild(transform, "ButtonGroup");
                }
            }
            if (replayButton == null)
            {
                Transform child = FindChild(transform, "ReplayButton");
                if (child != null)
                    replayButton = child.GetComponent<Button>();
            }
            if (replayButton == null && listParent != null)
            {
                GameObject prefab = RuntimePrefabs.TextButton;
                if (prefab != null)
                {
                    GameObject instance = Instantiate(prefab, listParent);
                    instance.name = "ReplayButton";
                    replayButton = instance.GetComponent<Button>();
                    instance.transform.SetAsLastSibling();
                }
            }
            if (title == null)
                title = FindChild(transform, "Title");
            if (emptyLabel == null)
            {
                Transform child = FindChild(transform, "EmptyLabel");
                if (child != null)
                    emptyLabel = child.GetComponent<TMP_Text>();
            }
            if (emptyLabel == null && listParent != null)
            {
                var go = new GameObject("EmptyLabel", typeof(RectTransform));
                go.transform.SetParent(listParent, false);
                emptyLabel = go.AddComponent<TextMeshProUGUI>();
                emptyLabel.fontSize = 24f;
                emptyLabel.alignment = TextAlignmentOptions.Center;
                emptyLabel.color = Color.black;
                emptyLabel.raycastTarget = false;
                LayoutElement layout = go.AddComponent<LayoutElement>();
                layout.minHeight = 40f;
                layout.preferredHeight = 40f;
                go.transform.SetAsFirstSibling();
            }
        }
        static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
        #endregion
    }
}
