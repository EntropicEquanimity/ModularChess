using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class RoguelikeHud : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Text stageLabel;
        [SerializeField] TMP_Text goldLabel;
        [SerializeField] TMP_Text armyLabel;
        [SerializeField] TMP_Text enemyBoonLabel;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] TMP_Text turnsLabel;
        [SerializeField] Button leaveButton;
        [SerializeField] Button nextStageButton;
        [SerializeField] Button rematchButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button giveUpButton;
        [SerializeField] Button endTurnButton;
        [SerializeField] GameObject rearrangePanel;
        [SerializeField] GameObject resultsPanel;
        [SerializeField] TMP_Text resultsLabel;
        [SerializeField] BoonOfferView boonOffer;
        RoguelikeController _controller;
        UnityAction _onOptions;
        UnityAction _onGiveUp;
        #endregion

        #region Unity
        void Awake()
        {
            Resolve();
        }
        #endregion

        #region Public Methods
        public void Bind(RoguelikeController controller, UnityAction onOptions = null, UnityAction onGiveUp = null)
        {
            Resolve();
            _controller = controller;
            _onOptions = onOptions;
            _onGiveUp = onGiveUp;
            if (leaveButton != null)
            {
                leaveButton.onClick.RemoveAllListeners();
                GameAudio.Bind(leaveButton, () => _controller?.Leave());
            }
            if (nextStageButton != null)
            {
                nextStageButton.onClick.RemoveAllListeners();
                GameAudio.Bind(nextStageButton, () => _controller?.NextStage());
            }
            if (rematchButton != null)
            {
                rematchButton.onClick.RemoveAllListeners();
                GameAudio.Bind(rematchButton, () => _controller?.Rematch());
            }
            if (optionsButton != null)
            {
                optionsButton.onClick.RemoveAllListeners();
                GameAudio.Bind(optionsButton, () =>
                {
                    if (_onOptions != null)
                        _onOptions.Invoke();
                    else
                        OptionsOverlay.Ensure()?.OpenFromMatch();
                });
            }
            if (giveUpButton != null)
            {
                giveUpButton.onClick.RemoveAllListeners();
                GameAudio.Bind(giveUpButton, () => _onGiveUp?.Invoke());
            }
            if (endTurnButton != null)
            {
                endTurnButton.onClick.RemoveAllListeners();
                GameAudio.Bind(endTurnButton, () => _controller?.RequestEndTurn());
            }
            if (boonOffer != null)
                boonOffer.Bind(def => _controller?.PickBoon(def));
        }
        public void Present(RoguelikeRunState run)
        {
            Resolve();
            if (resultsPanel != null)
                resultsPanel.SetActive(false);
            SetRearrange(false);
            HideBoonOffer();
            Refresh(run, null);
            OverlayMotion.Ensure(gameObject)?.PlayEnter();
        }
        public void Dismiss()
        {
            HideBoonOffer();
            OverlayMotion.Ensure(gameObject)?.PlayExit();
        }
        public void Refresh(RoguelikeRunState run, GameState state, int turnsRemaining = -1)
        {
            Resolve();
            if (run == null)
                return;
            if (stageLabel != null)
                stageLabel.text = Loc.Format("roguelike.stage", run.StageNumber);
            if (goldLabel != null)
                goldLabel.text = Loc.Format("roguelike.gold", run.Gold);
            if (armyLabel != null)
                armyLabel.text = Loc.Format("roguelike.army", CountArmy(state, run.PlayerSide), run.ArmySizeCap);
            if (turnsLabel != null)
            {
                int turns = turnsRemaining >= 0 ? turnsRemaining : RoguelikeBalance.StageTurnLimit;
                turnsLabel.text = Loc.Format("roguelike.turnsLeft", turns);
            }
            bool playerTurn = state != null
                && state.Status == GameStatus.InProgress
                && state.SideToMove == run.PlayerSide;
            bool blocked = (rearrangePanel != null && rearrangePanel.activeSelf)
                || (resultsPanel != null && resultsPanel.activeSelf)
                || (boonOffer != null && boonOffer.gameObject.activeInHierarchy);
            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(playerTurn && !blocked);
            if (statusLabel != null && state != null)
            {
                if (state.Status == GameStatus.InProgress)
                    statusLabel.text = playerTurn
                        ? Loc.Get("roguelike.yourTurn")
                        : Loc.Get("roguelike.enemyTurn");
            }
        }
        public void ShowWin()
        {
            ShowResults(Loc.Get("roguelike.win"));
        }
        public void ShowLose()
        {
            ShowResults(Loc.Get("roguelike.lose"));
        }
        public void ShowLoseOutOfTime()
        {
            ShowResults(Loc.Get("roguelike.lose.time"));
        }
        public void AnnounceEnemyBoon(EnemyBoonId? boon)
        {
            Resolve();
            if (enemyBoonLabel == null)
                return;
            if (boon == null)
            {
                enemyBoonLabel.text = string.Empty;
                return;
            }
            enemyBoonLabel.text = Loc.Format("roguelike.enemyBoon", Loc.Get("roguelike.enemyBoon.reinforcements"));
        }
        public void ShowBoonOffer(IReadOnlyList<BoonDefinition> offer)
        {
            Resolve();
            boonOffer?.Present(offer);
        }
        public void HideBoonOffer()
        {
            boonOffer?.Hide();
        }
        public void SetRearrange(bool on)
        {
            Resolve();
            if (rearrangePanel != null)
                rearrangePanel.SetActive(on);
            if (nextStageButton != null)
                nextStageButton.gameObject.SetActive(on);
            if (statusLabel != null && on)
                statusLabel.text = Loc.Get("roguelike.rearrange");
            if (endTurnButton != null && on)
                endTurnButton.gameObject.SetActive(false);
        }
        #endregion

        #region Private Methods
        void ShowResults(string text)
        {
            Resolve();
            HideBoonOffer();
            SetRearrange(false);
            if (endTurnButton != null)
                endTurnButton.gameObject.SetActive(false);
            if (resultsPanel != null)
                resultsPanel.SetActive(true);
            if (resultsLabel != null)
                resultsLabel.text = text;
        }
        static int CountArmy(GameState state, Side side)
        {
            if (state == null)
                return 0;
            int count = 0;
            foreach (Piece piece in state.Board.OccupiedPieces)
            {
                if (piece.Side == side && piece.Type != PieceType.King && (state.Runtime == null || !state.Runtime.IsSummoned(piece.Id)))
                    count++;
            }
            return count;
        }
        void Resolve()
        {
            if (stageLabel == null)
                stageLabel = FindTmp("StageLabel");
            if (goldLabel == null)
                goldLabel = FindTmp("GoldLabel");
            if (armyLabel == null)
                armyLabel = FindTmp("ArmyLabel");
            if (enemyBoonLabel == null)
                enemyBoonLabel = FindTmp("EnemyBoonLabel");
            if (statusLabel == null)
                statusLabel = FindTmp("StatusLabel");
            if (turnsLabel == null)
                turnsLabel = FindTmp("TurnsLabel");
            if (leaveButton == null)
                leaveButton = FindButton("LeaveButton");
            if (nextStageButton == null)
                nextStageButton = FindButton("NextStageButton");
            if (rematchButton == null)
                rematchButton = FindButton("RematchButton");
            if (optionsButton == null)
                optionsButton = FindButton("Options");
            if (giveUpButton == null)
                giveUpButton = FindButton("Give Up");
            if (endTurnButton == null)
                endTurnButton = FindButton("EndTurn");
            if (rearrangePanel == null)
            {
                Transform t = FindNamed("RearrangePanel");
                if (t != null)
                    rearrangePanel = t.gameObject;
            }
            if (resultsPanel == null)
            {
                Transform t = FindNamed("ResultsPanel");
                if (t != null)
                    resultsPanel = t.gameObject;
            }
            if (resultsLabel == null)
                resultsLabel = FindTmp("ResultsLabel");
            if (boonOffer == null)
                boonOffer = GetComponentInChildren<BoonOfferView>(true);
        }
        TMP_Text FindTmp(string name)
        {
            Transform t = FindNamed(name);
            return t != null ? t.GetComponent<TMP_Text>() : null;
        }
        Button FindButton(string name)
        {
            Transform t = FindNamed(name);
            return t != null ? t.GetComponent<Button>() : null;
        }
        Transform FindNamed(string name)
        {
            return FindChild(transform, name);
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
        #endregion
    }
}
