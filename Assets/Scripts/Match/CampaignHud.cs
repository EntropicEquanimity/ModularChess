using ModularChess.Core;
using ModularChess.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class CampaignHud : MatchHudBase
    {
        #region Fields
        [SerializeField] ObjectiveListView objectiveList;
        [SerializeField] GameObject clocksRoot;
        [SerializeField] Button nextLevelButton;
        bool _showsClock;
        MatchController _controller;
        string _levelTitle = string.Empty;
        #endregion

        #region Unity
        protected override void OnEnable()
        {
            _showsClock = false;
            base.OnEnable();
            ApplyClockRoot();
        }
        #endregion

        #region Public Methods
        protected override string RematchLocKey => "hud.tryAgain";
        public override void PresentSession(MatchSession session)
        {
            Wire();
            HideReplayChrome();
            CampaignLevelDefinition level = session?.CampaignLevel;
            _levelTitle = CampaignRowView.Title(level);
            _showsClock = level != null && level.ShowsClock;
            ApplyClockRoot();
            CampaignStarFlags saved = level != null
                ? CampaignProgress.GetStars(level.Index)
                : CampaignStarFlags.None;
            objectiveList?.Present(level, saved, animate: true);
        }
        public override void BindActions(MatchController controller)
        {
            base.BindActions(controller);
            _controller = controller;
            BindClick(nextLevelButton, controller.RequestNextCampaignLevel);
            LocalizedText.Bind(nextLevelButton, "hud.nextLevel");
        }
        public override void RefreshObjectives(CampaignObjectiveLive live)
        {
            objectiveList?.Refresh(live);
        }
        public override void SetNames(MatchSession session)
        {
        }
        protected override string TurnLabel(GameState state, bool inProgress)
        {
            string turn = inProgress
                ? Loc.Format("match.turnNumber", state.FullmoveNumber)
                : Loc.Get("match.over");
            if (_levelTitle.Length == 0)
                return turn;
            return Loc.Format("match.campaignHeadline", _levelTitle, turn);
        }
        public override void SetClock(MatchClock clock, Side playerSide)
        {
            ApplyClockRoot();
            base.SetClock(_showsClock ? clock : null, playerSide);
        }
        public override void SetMatchChromeVisible(bool visible)
        {
            ApplyClockRoot();
        }
        protected override void RefreshResultButtons(MatchController controller)
        {
            _controller = controller;
            bool over = controller != null
                && controller.State != null
                && controller.State.Status != GameStatus.InProgress
                && !controller.IsReplaying;
            if (rematchButton != null)
                rematchButton.gameObject.SetActive(over);
            HideReplayChrome();
            if (nextLevelButton == null)
                return;
            bool next = over
                && controller != null
                && controller.PlayerWonCampaign()
                && controller.Session?.CampaignLevel != null
                && CampaignCatalog.Get(controller.Session.CampaignLevel.Index + 1) != null;
            nextLevelButton.gameObject.SetActive(next);
        }
        protected override void ApplyGameOverButtons()
        {
            RefreshResultButtons(_controller);
        }
        #endregion

        #region Private Methods
        void ApplyClockRoot()
        {
            if (clocksRoot != null)
                clocksRoot.SetActive(_showsClock);
        }
        void HideReplayChrome()
        {
            if (replayButton != null)
                replayButton.gameObject.SetActive(false);
            if (replayControls != null)
                replayControls.gameObject.SetActive(false);
        }
        #endregion
    }
}
