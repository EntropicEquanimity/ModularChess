using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class CampaignOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] Button backButton;
        [SerializeField] Button playButton;
        [SerializeField] Transform title;
        [SerializeField] Transform listParent;
        [SerializeField] CampaignRowView rowPrefab;
        readonly List<CampaignRowView> _rows = new List<CampaignRowView>();
        int _selected = -1;
        UnityAction _onBack;
        UnityAction<int> _onPlay;
        #endregion

        #region Public Methods
        public void Bind(UnityAction onBack, UnityAction<int> onPlay)
        {
            Resolve();
            _onBack = onBack;
            _onPlay = onPlay;
            GameAudio.Bind(backButton, () => _onBack?.Invoke());
            GameAudio.Bind(playButton, StartLevel);
            RefreshLoc();
            Refresh();
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(title, "play.campaign");
            LocalizedText.Bind(backButton, "menu.back");
            LocalizedText.Bind(playButton, "campaign.play");
        }
        public void Refresh()
        {
            Resolve();
            if (_selected >= CampaignCatalog.Count)
                _selected = -1;
            if (_selected < 0)
            {
                for (int i = 0; i < CampaignCatalog.Count; i++)
                {
                    if (CampaignProgress.IsUnlocked(i) && !CampaignProgress.HasStar(i, CampaignStarFlags.Complete))
                    {
                        _selected = i;
                        break;
                    }
                }
                if (_selected < 0 && CampaignProgress.IsUnlocked(0))
                    _selected = 0;
            }
            RebuildRows();
            UpdatePlayButton();
        }
        public void SelectIndex(int index)
        {
            if (index < 0 || index >= CampaignCatalog.Count) return;
            if (!CampaignProgress.IsUnlocked(index)) return;
            _selected = index;
            for (int i = 0; i < _rows.Count; i++)
                _rows[i].SetSelected(i == _selected);
            UpdatePlayButton();
        }
        #endregion

        #region Private Methods
        void StartLevel()
        {
            if (_selected < 0 || !CampaignProgress.IsUnlocked(_selected)) return;
            _onPlay?.Invoke(_selected);
        }
        void UpdatePlayButton()
        {
            if (playButton == null) return;
            playButton.interactable = _selected >= 0 && CampaignProgress.IsUnlocked(_selected);
        }
        void RebuildRows()
        {
            if (listParent == null || rowPrefab == null) return;
            for (int i = _rows.Count - 1; i >= 0; i--)
            {
                if (_rows[i] != null)
                    Destroy(_rows[i].gameObject);
            }
            _rows.Clear();
            for (int i = 0; i < CampaignCatalog.Count; i++)
            {
                CampaignRowView row = Instantiate(rowPrefab, listParent);
                row.gameObject.SetActive(true);
                row.Bind(i, i == _selected, SelectIndex);
                _rows.Add(row);
            }
        }
        void Resolve()
        {
            if (backButton == null)
            {
                Transform named = FindChild(transform, "BackButton");
                if (named != null) backButton = named.GetComponent<Button>();
            }
            if (playButton == null)
            {
                Transform named = FindChild(transform, "PlayButton");
                if (named != null) playButton = named.GetComponent<Button>();
                if (playButton == null)
                {
                    named = FindChild(transform, "ReplayButton");
                    if (named != null) playButton = named.GetComponent<Button>();
                }
            }
            if (title == null)
                title = FindChild(transform, "Title");
            if (listParent == null)
            {
                ScrollRect scroll = GetComponentInChildren<ScrollRect>(true);
                if (scroll != null)
                    listParent = scroll.content;
            }
            if (rowPrefab == null)
            {
                CampaignRowView existing = GetComponentInChildren<CampaignRowView>(true);
                if (existing != null && existing.transform.parent != listParent)
                    rowPrefab = existing;
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
