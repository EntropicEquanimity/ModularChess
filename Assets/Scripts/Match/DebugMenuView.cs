using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class DebugMenuView : MonoBehaviour
    {
        #region Fields
        [SerializeField] Button resetSaveButton;
        [SerializeField] Button unlockAllButton;
        [SerializeField] Button winButton;
        [SerializeField] Button loseButton;
        [SerializeField] Button resetTimerButton;
        [SerializeField] Button closeButton;
        [SerializeField] Button autoplayStartButton;
        [SerializeField] Button autoplayStopButton;
        [SerializeField] TMP_Dropdown autoplayDifficulty;
        [SerializeField] Button vsAiNoneButton;
        [SerializeField] Button vsAiAllNoRandomizerButton;
        [SerializeField] Button vsAiAllButton;
        [SerializeField] Button vsAiSpecificButton;
        [SerializeField] TMP_Dropdown vsAiModeDropdown;
        [SerializeField] Button jumpLevelButton;
        [SerializeField] TMP_Dropdown campaignLevelDropdown;
        [SerializeField] Button revealFogButton;
        [SerializeField] Button meritPlusButton;
        [SerializeField] Button meritMinusButton;
        [SerializeField] Button unlockAllCampaignButton;
        [SerializeField] Button clearAllCampaignButton;
        [SerializeField] Button vsAiRandomModesButton;
        UnityAction<ModeId> _vsAiSpecific;
        UnityAction<int> _jumpLevel;
        #endregion

        #region Public Methods
        public void Present(
            UnityAction resetSave,
            UnityAction unlockAll,
            UnityAction win,
            UnityAction lose,
            UnityAction resetTimer,
            UnityAction close,
            UnityAction vsAiNone,
            UnityAction vsAiAllNoRandomizer,
            UnityAction vsAiAll,
            UnityAction<ModeId> vsAiSpecific,
            UnityAction<int> jumpLevel,
            UnityAction revealFog,
            UnityAction meritPlus,
            UnityAction meritMinus,
            UnityAction unlockAllCampaign,
            UnityAction clearAllCampaign,
            UnityAction vsAiRandomModes)
        {
            Resolve();
            _vsAiSpecific = vsAiSpecific;
            _jumpLevel = jumpLevel;
            Bind(resetSaveButton, resetSave);
            Bind(unlockAllButton, unlockAll);
            Bind(winButton, win);
            Bind(loseButton, lose);
            Bind(resetTimerButton, resetTimer);
            Bind(closeButton, close);
            Bind(autoplayStartButton, OnAutoplayStart);
            Bind(autoplayStopButton, OnAutoplayStop);
            Bind(vsAiNoneButton, vsAiNone);
            Bind(vsAiAllNoRandomizerButton, vsAiAllNoRandomizer);
            Bind(vsAiAllButton, vsAiAll);
            Bind(vsAiSpecificButton, OnVsAiSpecific);
            Bind(jumpLevelButton, OnJumpLevel);
            Bind(revealFogButton, revealFog);
            Bind(meritPlusButton, meritPlus);
            Bind(meritMinusButton, meritMinus);
            Bind(unlockAllCampaignButton, unlockAllCampaign);
            Bind(clearAllCampaignButton, clearAllCampaign);
            Bind(vsAiRandomModesButton, vsAiRandomModes);
            FillModeDropdown();
            FillCampaignDropdown();
            WireDifficulty();
            RefreshAutoplay();
        }
        #endregion

        #region Private Methods
        void OnAutoplayStart()
        {
            Autoplay.Start();
            RefreshAutoplay();
        }
        void OnAutoplayStop()
        {
            Autoplay.Stop();
            RefreshAutoplay();
        }
        void WireDifficulty()
        {
            if (autoplayDifficulty == null) return;
            autoplayDifficulty.onValueChanged.RemoveAllListeners();
            autoplayDifficulty.SetValueWithoutNotify((int)Autoplay.Strength);
            autoplayDifficulty.onValueChanged.AddListener(OnDifficultyChanged);
        }
        void OnDifficultyChanged(int index)
        {
            if (index >= 0 && index <= (int)AiStrength.Hard) Autoplay.SetStrength((AiStrength)index);
        }
        void OnVsAiSpecific()
        {
            if (_vsAiSpecific == null || vsAiModeDropdown == null)
                return;
            int index = vsAiModeDropdown.value;
            if (index < 0 || index >= ModeCatalog.All.Length)
                return;
            _vsAiSpecific(ModeCatalog.All[index].Id);
        }
        void OnJumpLevel()
        {
            if (_jumpLevel == null || campaignLevelDropdown == null)
                return;
            _jumpLevel(campaignLevelDropdown.value);
        }
        void FillModeDropdown()
        {
            if (vsAiModeDropdown == null)
                return;
            int keep = vsAiModeDropdown.value;
            vsAiModeDropdown.ClearOptions();
            var options = new List<TMP_Dropdown.OptionData>(ModeCatalog.All.Length);
            for (int i = 0; i < ModeCatalog.All.Length; i++)
                options.Add(new TMP_Dropdown.OptionData(Loc.ModeName(ModeCatalog.All[i].Id)));
            vsAiModeDropdown.AddOptions(options);
            vsAiModeDropdown.SetValueWithoutNotify(Mathf.Clamp(keep, 0, options.Count - 1));
        }
        void FillCampaignDropdown()
        {
            if (campaignLevelDropdown == null)
                return;
            int keep = campaignLevelDropdown.value;
            campaignLevelDropdown.ClearOptions();
            int count = CampaignCatalog.Count;
            var options = new List<TMP_Dropdown.OptionData>(count);
            for (int i = 0; i < count; i++)
            {
                CampaignLevelDefinition level = CampaignCatalog.Get(i);
                string title = level != null ? Loc.Get(level.TitleKey) : Loc.Format("campaign.level.n", i + 1);
                options.Add(new TMP_Dropdown.OptionData((i + 1) + ". " + title));
            }
            campaignLevelDropdown.AddOptions(options);
            if (options.Count > 0)
                campaignLevelDropdown.SetValueWithoutNotify(Mathf.Clamp(keep, 0, options.Count - 1));
        }
        void Resolve()
        {
            resetSaveButton = ButtonNamed("dialog.resetSave") ?? resetSaveButton;
            unlockAllButton = ButtonNamed("dialog.unlockAll") ?? unlockAllButton;
            winButton = ButtonNamed("dialog.win") ?? winButton;
            loseButton = ButtonNamed("dialog.lose") ?? loseButton;
            resetTimerButton = ButtonNamed("dialog.resetTimer") ?? resetTimerButton;
            closeButton = ButtonNamed("dialog.close") ?? closeButton;
            autoplayStartButton = ButtonNamed("dialog.autoplayStart") ?? autoplayStartButton;
            autoplayStopButton = ButtonNamed("dialog.autoplayStop") ?? autoplayStopButton;
            vsAiNoneButton = ButtonNamed("dialog.vsAiNone") ?? vsAiNoneButton;
            vsAiAllNoRandomizerButton = ButtonNamed("dialog.vsAiAllNoRandomizer") ?? vsAiAllNoRandomizerButton;
            vsAiAllButton = ButtonNamed("dialog.vsAiAll") ?? vsAiAllButton;
            vsAiSpecificButton = ButtonNamed("dialog.vsAiSpecific") ?? vsAiSpecificButton;
            jumpLevelButton = ButtonNamed("dialog.jumpLevel") ?? jumpLevelButton;
            revealFogButton = ButtonNamed("dialog.revealFog") ?? revealFogButton;
            meritPlusButton = ButtonNamed("dialog.meritPlus") ?? meritPlusButton;
            meritMinusButton = ButtonNamed("dialog.meritMinus") ?? meritMinusButton;
            unlockAllCampaignButton = ButtonNamed("dialog.unlockAllCampaign") ?? unlockAllCampaignButton;
            clearAllCampaignButton = ButtonNamed("dialog.clearAllCampaign") ?? clearAllCampaignButton;
            vsAiRandomModesButton = ButtonNamed("dialog.vsAiRandomModes") ?? vsAiRandomModesButton;
            if (autoplayDifficulty == null) autoplayDifficulty = DropdownNamed("dialog.autoplayDifficulty");
            if (vsAiModeDropdown == null) vsAiModeDropdown = DropdownNamed("dialog.vsAiMode");
            if (campaignLevelDropdown == null) campaignLevelDropdown = DropdownNamed("dialog.campaignLevel");
        }
        void RefreshAutoplay()
        {
            if (autoplayStartButton != null) autoplayStartButton.gameObject.SetActive(!Autoplay.Active);
            if (autoplayStopButton != null) autoplayStopButton.gameObject.SetActive(Autoplay.Active);
        }
        static void Bind(Button button, UnityAction action)
        {
            if (button == null || action == null) return;
            button.onClick.RemoveAllListeners();
            GameAudio.Bind(button, action);
        }
        Button ButtonNamed(string name)
        {
            Transform child = FindChild(transform, name);
            if (child == null)  return null;
            Button button = child.GetComponent<Button>();
            return button != null ? button : child.GetComponentInChildren<Button>(true);
        }
        TMP_Dropdown DropdownNamed(string name)
        {
            Transform child = FindChild(transform, name);
            if (child == null) return null;
            TMP_Dropdown dropdown = child.GetComponent<TMP_Dropdown>();
            return dropdown != null ? dropdown : child.GetComponentInChildren<TMP_Dropdown>(true);
        }
        static Transform FindChild(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)  return found;
            }
            return null;
        }
        #endregion
    }
}
