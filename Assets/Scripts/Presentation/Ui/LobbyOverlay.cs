using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class LobbyOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Text joinCodeLabel;
        [SerializeField] TMP_Text statusLabel;
        [SerializeField] Button sitButton;
        [SerializeField] Button startButton;
        [SerializeField] Button leaveButton;
        [SerializeField] Transform title;
        #endregion

        #region Public Methods
        public void Bind(UnityAction onSit, UnityAction onStart, UnityAction onLeave)
        {
            Resolve();
            GameAudio.Bind(sitButton, onSit);
            GameAudio.Bind(startButton, onStart);
            GameAudio.Bind(leaveButton, onLeave);
            RefreshLoc();
        }
        public void Present(string code, bool friendSeated)
        {
            Resolve();
            if (joinCodeLabel != null)
                joinCodeLabel.text = Loc.Format("lobby.code", code);
            if (statusLabel != null)
                statusLabel.text = friendSeated ? Loc.Get("lobby.seated") : Loc.Get("lobby.waiting");
            if (startButton != null)
            {
                startButton.interactable = friendSeated;
                LocalizedText.Bind(startButton, friendSeated ? "lobby.start" : "lobby.waitingHost");
            }
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(title, "lobby.title");
            LocalizedText.Bind(sitButton, "lobby.sit");
            LocalizedText.Bind(leaveButton, "lobby.leave");
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (joinCodeLabel == null)
                joinCodeLabel = LabelNamed("JoinCodeLabel");
            if (statusLabel == null)
                statusLabel = LabelNamed("StatusLabel");
            if (sitButton == null)
                sitButton = ButtonNamed("SitButton");
            if (startButton == null)
                startButton = ButtonNamed("StartButton");
            if (leaveButton == null)
                leaveButton = ButtonNamed("LeaveButton");
            if (title == null)
                title = FindNamed("Title");
        }
        Button ButtonNamed(string name)
        {
            Transform child = FindNamed(name);
            return child != null ? child.GetComponent<Button>() : null;
        }
        TMP_Text LabelNamed(string name)
        {
            Transform child = FindNamed(name);
            if (child == null)
                return null;
            TMP_Text tmp = child.GetComponent<TMP_Text>();
            return tmp != null ? tmp : child.GetComponentInChildren<TMP_Text>(true);
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
