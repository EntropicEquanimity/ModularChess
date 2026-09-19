using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class JoinOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_InputField codeField;
        [SerializeField] Button enterButton;
        [SerializeField] Button backButton;
        [SerializeField] Transform title;
        #endregion

        #region Public Methods
        public void Bind(UnityAction onEnter, UnityAction onBack)
        {
            Resolve();
            GameAudio.Bind(enterButton, onEnter);
            GameAudio.Bind(backButton, onBack);
            RefreshLoc();
        }
        public string Code
        {
            get
            {
                Resolve();
                return codeField != null ? codeField.text.Trim().ToUpperInvariant() : string.Empty;
            }
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(title, "join.title");
            LocalizedText.Bind(enterButton, "join.enter");
            LocalizedText.Bind(backButton, "menu.back");
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (codeField == null)
            {
                Transform named = FindNamed("JoinCodeField");
                if (named != null)
                    codeField = named.GetComponentInChildren<TMP_InputField>(true);
                if (codeField == null)
                    codeField = GetComponentInChildren<TMP_InputField>(true);
            }
            if (enterButton == null)
                enterButton = ButtonNamed("EnterButton");
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
