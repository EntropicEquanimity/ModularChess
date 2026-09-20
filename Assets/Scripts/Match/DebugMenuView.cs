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
        #endregion

        #region Public Methods
        public void Present(
            UnityAction resetSave,
            UnityAction unlockAll,
            UnityAction win,
            UnityAction lose,
            UnityAction resetTimer,
            UnityAction close)
        {
            Resolve();
            Bind(resetSaveButton, resetSave);
            Bind(unlockAllButton, unlockAll);
            Bind(winButton, win);
            Bind(loseButton, lose);
            Bind(resetTimerButton, resetTimer);
            Bind(closeButton, close);
            Bind(autoplayStartButton, OnAutoplayStart);
            Bind(autoplayStopButton, OnAutoplayStop);
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
            if (autoplayDifficulty == null) autoplayDifficulty = DropdownNamed("dialog.autoplayDifficulty");
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
