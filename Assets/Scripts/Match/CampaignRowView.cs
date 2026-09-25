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
        [SerializeField] Image starComplete;
        [SerializeField] Image starTurn;
        [SerializeField] Image starLoss;
        int _index;
        static readonly Color Selected = new Color(0.85f, 0.9f, 1f, 1f);
        static readonly Color Idle = new Color(1f, 1f, 1f, 0.4f);
        static readonly Color Locked = new Color(1f, 1f, 1f, 0.2f);
        static readonly Color StarEarned = new Color(1f, 1f, 1f, 1f);
        static readonly Color StarUnearned = new Color(1f, 1f, 1f, 0.5f);
        static readonly Color StarLockedTint = new Color(0.65f, 0.65f, 0.65f, 0.35f);
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
            ApplyStars(unlocked, level != null ? CampaignProgress.GetStars(level.Index) : CampaignStarFlags.None);
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
        void ApplyStars(bool unlocked, CampaignStarFlags flags)
        {
            ApplyStar(starComplete, unlocked, (flags & CampaignStarFlags.Complete) != 0);
            ApplyStar(starTurn, unlocked, (flags & CampaignStarFlags.TurnLimit) != 0);
            ApplyStar(starLoss, unlocked, (flags & CampaignStarFlags.LossLimit) != 0);
        }
        static void ApplyStar(Image image, bool unlocked, bool earned)
        {
            if (image == null) return;
            if (!unlocked)
            {
                image.color = StarLockedTint;
                return;
            }
            image.color = earned ? StarEarned : StarUnearned;
        }
        static string FormatRow(CampaignLevelDefinition level, bool unlocked)
        {
            if (level == null) return string.Empty;
            string title = LevelTitle(level);
            if (!unlocked) return Loc.Format("campaign.locked", level.Index + 1, title);
            return $"{level.Index + 1}. {title}";
        }
        static string LevelTitle(CampaignLevelDefinition level)
        {
            string keyed = Loc.Get(level.TitleKey);
            if (keyed != level.TitleKey)
                return keyed;
            return Loc.Format("campaign.level.n", level.Index + 1);
        }
        #endregion
    }
}
