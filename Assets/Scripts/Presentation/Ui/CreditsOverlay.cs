using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class CreditsOverlay : MonoBehaviour
    {
        #region Fields
        [SerializeField] Button backButton;
        [SerializeField] Transform title;
        #endregion

        #region Public Methods
        public void Bind(UnityAction onBack)
        {
            Resolve();
            GameAudio.Bind(backButton, onBack);
            RefreshLoc();
        }
        public void RefreshLoc()
        {
            Resolve();
            LocalizedText.Bind(title, "menu.credits");
            LocalizedText.Bind(backButton, "menu.back");
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (backButton == null)
            {
                Transform child = FindChild(transform, "BackButton");
                if (child != null) { backButton = child.GetComponent<Button>(); }
            }
            if (title == null) { title = FindChild(transform, "Title"); }
        }
        static Transform FindChild(Transform root, string name)
        {
            if (root == null) { return null; }
            if (root.name == name) { return root; }
            for (int i = 0; i < root.childCount; i++) {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null) { return found; }
            }
            return null;
        }
        #endregion
    }
}
