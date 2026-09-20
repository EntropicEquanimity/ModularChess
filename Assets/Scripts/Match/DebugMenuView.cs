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
            Bind(autoplayStartButton, () =>
            {
                Autoplay.Start();
                RefreshAutoplay();
            });
            Bind(autoplayStopButton, () =>
            {
                Autoplay.Stop();
                RefreshAutoplay();
            });
            if (autoplayDifficulty != null)
            {
                autoplayDifficulty.onValueChanged.RemoveAllListeners();
                autoplayDifficulty.SetValueWithoutNotify((int)Autoplay.Strength);
                autoplayDifficulty.onValueChanged.AddListener(index =>
                {
                    if (index >= 0 && index <= (int)AiStrength.Hard)
                        Autoplay.SetStrength((AiStrength)index);
                });
            }
            RefreshAutoplay();
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (resetSaveButton == null) { resetSaveButton = ButtonNamed("dialog.resetSave"); }
            if (unlockAllButton == null) { unlockAllButton = ButtonNamed("dialog.unlockAll"); }
            if (winButton == null) { winButton = ButtonNamed("dialog.win"); }
            if (loseButton == null) { loseButton = ButtonNamed("dialog.lose"); }
            if (resetTimerButton == null) { resetTimerButton = ButtonNamed("dialog.resetTimer"); }
            if (closeButton == null) { closeButton = ButtonNamed("dialog.close"); }
            if (autoplayStartButton == null) { autoplayStartButton = ButtonNamed("dialog.autoplayStart"); }
            if (autoplayStopButton == null) { autoplayStopButton = ButtonNamed("dialog.autoplayStop"); }
            if (autoplayDifficulty == null)
            {
                Transform row = FindChild(transform, "dialog.autoplayDifficulty");
                if (row != null)
                {
                    autoplayDifficulty = row.GetComponent<TMP_Dropdown>();
                    if (autoplayDifficulty == null)
                        autoplayDifficulty = row.GetComponentInChildren<TMP_Dropdown>(true);
                }
            }
        }
        void RefreshAutoplay()
        {
            if (autoplayStartButton != null)
                autoplayStartButton.gameObject.SetActive(!Autoplay.Active);
            if (autoplayStopButton != null)
                autoplayStopButton.gameObject.SetActive(Autoplay.Active);
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
            return child != null ? child.GetComponent<Button>() : null;
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
