using DG.Tweening;
using ModularChess.Core;
using ModularChess.Presentation;
using UnityEngine;

namespace ModularChess.Match
{
    public sealed class ObjectiveListView : MonoBehaviour
    {
        #region Fields
        const float SlideSeconds = 0.28f;
        const float Stagger = 0.08f;
        const float OffscreenX = -420f;
        [SerializeField] ObjectiveRowView completion;
        [SerializeField] ObjectiveRowView time;
        [SerializeField] ObjectiveRowView special;
        [SerializeField] Sprite incompleteIcon;
        [SerializeField] Sprite completeIcon;
        Tween _enterTween;
        CampaignLevelDefinition _level;
        CampaignStarFlags _saved;
        #endregion

        #region Unity
        void OnDisable()
        {
            KillEnter();
        }
        void OnDestroy()
        {
            KillEnter();
        }
        #endregion

        #region Public Methods
        public void Present(CampaignLevelDefinition level, CampaignStarFlags saved, bool animate)
        {
            _level = level;
            _saved = saved;
            EnsureRows();
            Refresh(new CampaignObjectiveLive(false, false, false, 0, 0));
            if (animate)
                PlayEnter();
        }
        public void Refresh(CampaignObjectiveLive live)
        {
            EnsureRows();
            if (_level == null)
            {
                completion?.Bind(string.Empty, false, false, incompleteIcon, completeIcon);
                time?.Bind(string.Empty, false, false, incompleteIcon, completeIcon);
                special?.Bind(string.Empty, false, false, incompleteIcon, completeIcon);
                return;
            }
            bool completeSaved = (_saved & CampaignStarFlags.Complete) != 0;
            bool timeSaved = (_saved & CampaignStarFlags.Time) != 0;
            bool specialSaved = (_saved & CampaignStarFlags.Special) != 0;
            completion?.Bind(Loc.Get("campaign.objective.complete"), completeSaved, false, incompleteIcon, completeIcon);
            time?.Bind(TimeText(_level), timeSaved, live.TimeFailed && !timeSaved, incompleteIcon, completeIcon);
            special?.Bind(SpecialText(_level), specialSaved, live.SpecialFailed && !specialSaved, incompleteIcon, completeIcon);
        }
        #endregion

        #region Private Methods
        void EnsureRows()
        {
            if (completion != null && time != null && special != null)
                return;
            if (completion == null)
                completion = RowNamed(transform, "ObjectiveCompletion");
            if (time == null)
                time = RowNamed(transform, "ObjectiveTime");
            if (special == null)
                special = RowNamed(transform, "ObjectiveSpecial");
        }
        static ObjectiveRowView RowNamed(Transform root, string name)
        {
            Transform child = root.Find(name);
            if (child == null)
                return null;
            ObjectiveRowView row = child.GetComponent<ObjectiveRowView>();
            if (row == null)
                row = child.gameObject.AddComponent<ObjectiveRowView>();
            return row;
        }
        void PlayEnter()
        {
            KillEnter();
            Sequence sequence = DOTween.Sequence().SetUpdate(true);
            float delay = 0f;
            AppendSlide(sequence, completion, ref delay);
            AppendSlide(sequence, time, ref delay);
            AppendSlide(sequence, special, ref delay);
            _enterTween = sequence;
        }
        static void AppendSlide(Sequence sequence, ObjectiveRowView row, ref float delay)
        {
            if (row == null || !row.gameObject.activeSelf)
                return;
            RectTransform rect = row.SlideRoot;
            Vector2 rest = rect.anchoredPosition;
            float restX = rest.x;
            rect.anchoredPosition = new Vector2(OffscreenX, rest.y);
            sequence.Insert(delay,
                DOTween.To(() => rect.anchoredPosition, v => rect.anchoredPosition = v, new Vector2(restX, rest.y), UiAnimPrefs.MoveDuration(SlideSeconds))
                    .SetEase(Ease.OutCubic));
            delay += Stagger;
        }
        void KillEnter()
        {
            _enterTween?.Kill();
            _enterTween = null;
        }
        static string TimeText(CampaignLevelDefinition level)
        {
            if (level.TimeKind == CampaignTimeObjectiveKind.Clock)
                return Loc.Format("campaign.objective.clock", level.Clock.ToString());
            return Loc.Format("campaign.objective.turns", level.TimeLimit);
        }
        static string SpecialText(CampaignLevelDefinition level)
        {
            switch (level.SpecialKind)
            {
                case CampaignSpecialObjectiveKind.None:
                    return string.Empty;
                case CampaignSpecialObjectiveKind.LoseNoPieces:
                    return Loc.Get("campaign.objective.loseNone");
                case CampaignSpecialObjectiveKind.MaxLosses:
                    return Loc.Format("campaign.objective.maxLosses", level.SpecialCount);
                case CampaignSpecialObjectiveKind.CapturePiece:
                    return Loc.Format("campaign.objective.capture", Loc.PieceName(level.SpecialPiece));
                case CampaignSpecialObjectiveKind.DoNotMovePiece:
                    return Loc.Format("campaign.objective.dontMove", Loc.PieceName(level.SpecialPiece));
                case CampaignSpecialObjectiveKind.PromotePawn:
                    return Loc.Get("campaign.objective.promote");
                default:
                    return string.Empty;
            }
        }
        #endregion
    }
}
