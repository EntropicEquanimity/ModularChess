using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class MainMenuView : MonoBehaviour
    {
        #region Fields
        [SerializeField] Button playButton;
        [SerializeField] Button historyButton;
        [SerializeField] Button unlocksButton;
        [SerializeField] Button optionsButton;
        [SerializeField] Button creditsButton;
        [SerializeField] Button feedbackButton;
        [SerializeField] Button exitButton;
        [SerializeField] Button customizeButton;
        [SerializeField] Transform title;
        [SerializeField] TMP_Text meritText;
        bool _bound;
        #endregion

        #region Unity
        void OnEnable()
        {
            MeritWallet.Changed += RefreshMerit;
            RefreshMerit();
        }
        void OnDisable()
        {
            MeritWallet.Changed -= RefreshMerit;
        }
        #endregion

        #region Public Methods
        public void Bind(
            UnityAction onPlay,
            UnityAction onHistory,
            UnityAction onUnlocks,
            UnityAction onOptions,
            UnityAction onCredits,
            UnityAction onFeedback,
            UnityAction onExit)
        {
            Resolve();
            if (_bound) return;
            GameAudio.Bind(playButton, onPlay);
            GameAudio.Bind(historyButton, onHistory);
            GameAudio.Bind(unlocksButton, onUnlocks);
            GameAudio.Bind(optionsButton, onOptions);
            GameAudio.Bind(creditsButton, onCredits);
            GameAudio.Bind(feedbackButton, onFeedback);
            GameAudio.Bind(exitButton, onExit);
            if (customizeButton != null)
                customizeButton.interactable = false;
            RefreshHistoryGate();
            RefreshMerit();
            OverlayMotion.Ensure(gameObject);
            _bound = true;
            RefreshLoc();
        }
        public void RefreshHistoryGate()
        {
            Resolve();
            if (historyButton != null)
                historyButton.interactable = HistoryPrefs.Unlocked;
        }
        public void RefreshMerit()
        {
            if (meritText != null)
                meritText.text = MeritWallet.Balance.ToString();
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(playButton, "menu.play");
            LocalizedText.Bind(historyButton, "menu.history");
            LocalizedText.Bind(unlocksButton, "menu.unlocks");
            LocalizedText.Bind(optionsButton, "menu.options");
            LocalizedText.Bind(creditsButton, "menu.credits");
            LocalizedText.Bind(feedbackButton, "menu.feedback");
            LocalizedText.Bind(exitButton, "menu.exit");
            LocalizedText.Bind(customizeButton, "menu.customize");
            LocalizedText.Bind(title, "menu.title");
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (playButton == null)
                playButton = ButtonNamed("PlayButton");
            if (historyButton == null)
                historyButton = ButtonNamed("HistoryButton");
            if (unlocksButton == null)
                unlocksButton = ButtonNamed("UnlocksButton");
            if (optionsButton == null)
                optionsButton = ButtonNamed("OptionsButton");
            if (creditsButton == null)
                creditsButton = ButtonNamed("CreditsButton");
            if (feedbackButton == null)
                feedbackButton = ButtonNamed("FeedbackButton");
            if (exitButton == null)
                exitButton = ButtonNamed("ExitButton");
            if (customizeButton == null)
                customizeButton = ButtonNamed("CustomizeButton");
            if (title == null)
                title = FindChild(transform, "Title");
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
