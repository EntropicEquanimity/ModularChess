using ModularChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class PlayOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] Button versusAiButton;
        [SerializeField] Button versusFriendButton;
        [SerializeField] Button joinButton;
        [SerializeField] Button roguelikeButton;
        [SerializeField] Button backButton;
        [SerializeField] Transform title;
        #endregion

        #region Public Methods
        public void Bind(
            UnityAction onVersusAi,
            UnityAction onVersusFriend,
            UnityAction onJoin,
            UnityAction onRoguelike,
            UnityAction onBack)
        {
            Resolve();
            GameAudio.Bind(versusAiButton, onVersusAi);
            GameAudio.Bind(versusFriendButton, onVersusFriend);
            GameAudio.Bind(joinButton, onJoin);
            if (roguelikeButton != null)
                GameAudio.Bind(roguelikeButton, onRoguelike);
            GameAudio.Bind(backButton, onBack);
            RefreshLoc();
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(title, "menu.play");
            LocalizedText.Bind(versusAiButton, "play.versusAi");
            LocalizedText.Bind(versusFriendButton, "play.versusFriend");
            LocalizedText.Bind(joinButton, "play.join");
            if (roguelikeButton != null)
                LocalizedText.Bind(roguelikeButton, "play.roguelike");
            LocalizedText.Bind(backButton, "menu.back");
        }
        public void ApplyWebGlLimits()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                return;
            Resolve();
            if (versusFriendButton != null)
                versusFriendButton.interactable = false;
            if (joinButton != null)
                joinButton.interactable = false;
        }
        public void RefreshLocks()
        {
            Resolve();
            if (roguelikeButton != null)
                roguelikeButton.interactable = ActivityDlc.IsOwned(Activity.Roguelike);
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (versusAiButton == null)
                versusAiButton = ButtonNamed("VersusAiButton");
            if (versusFriendButton == null)
                versusFriendButton = ButtonNamed("VersusFriendButton");
            if (joinButton == null)
                joinButton = ButtonNamed("JoinButton");
            if (roguelikeButton == null)
                roguelikeButton = ButtonNamed("RoguelikeButton");
            if (backButton == null)
                backButton = ButtonNamed("BackButton");
            if (title == null)
                title = FindNamed("Title");
        }
        Button ButtonNamed(string name)
        {
            Transform child = FindNamed(name);
            return child != null ? child.GetComponent<Button>() : null;
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
