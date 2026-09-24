using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class CampaignRowView : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Text label;
        [SerializeField] Image background;
        int _index;
        static readonly Color Selected = new Color(0.85f, 0.9f, 1f, 1f);
        static readonly Color Idle = new Color(1f, 1f, 1f, 0.4f);
        static readonly Color Locked = new Color(1f, 1f, 1f, 0.2f);
        #endregion

        #region Public Methods
        public int Index => _index;
        public void Bind(int index, bool selected, UnityAction<int> onSelect)
        {
            _index = index;
            EnsureRefs();
            CampaignLevelDefinition level = CampaignCatalog.Get(index);
            bool unlocked = CampaignProgress.IsUnlocked(index);
            if (label != null)
                label.text = FormatRow(level, unlocked);
            SetSelected(selected);
            Button button = GetComponent<Button>();
            if (button == null) { button = gameObject.AddComponent<Button>(); }
            if (background == null) { background = GetComponent<Image>(); }
            if (background == null) { background = gameObject.AddComponent<Image>(); }
            background.raycastTarget = true;
            button.targetGraphic = background;
            button.transition = Selectable.Transition.None;
            button.interactable = unlocked;
            background.color = unlocked ? (selected ? Selected : Idle) : Locked;
            int captured = index;
            GameAudio.Bind(button, () => { if (CampaignProgress.IsUnlocked(captured)) onSelect?.Invoke(captured); });
        }
        public void SetSelected(bool selected)
        {
            if (background == null) return;
            if (!CampaignProgress.IsUnlocked(_index))
            {
                background.color = Locked;
                return;
            }
            background.color = selected ? Selected : Idle;
        }
        #endregion

        #region Private Methods
        void EnsureRefs()
        {
            if (label == null) { label = GetComponentInChildren<TMP_Text>(true); }
            if (background == null) { background = GetComponent<Image>(); }
        }
        static string FormatRow(CampaignLevelDefinition level, bool unlocked)
        {
            if (level == null) return string.Empty;
            string title = Loc.Get(level.TitleKey);
            if (!unlocked) return Loc.Format("campaign.locked", level.Index + 1, title);
            CampaignStarFlags stars = CampaignProgress.GetStars(level.Index);
            string marks = StarMarks(stars);
            return $"{level.Index + 1}. {title}  {marks}";
        }
        static string StarMarks(CampaignStarFlags flags)
        {
            return $"{Mark(flags, CampaignStarFlags.Complete)}{Mark(flags, CampaignStarFlags.TurnLimit)}{Mark(flags, CampaignStarFlags.LossLimit)}";
        }
        static string Mark(CampaignStarFlags flags, CampaignStarFlags star)
        {
            return (flags & star) != 0 ? "★" : "☆";
        }
        #endregion
    }
}
