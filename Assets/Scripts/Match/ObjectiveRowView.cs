using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class ObjectiveRowView : MonoBehaviour
    {
        #region Fields
        const float FailedAlpha = 0.8f;
        [SerializeField] Image icon;
        [SerializeField] TMP_Text label;
        #endregion

        #region Public Methods
        public void Bind(string text, bool complete, bool failed, Sprite incompleteIcon, Sprite completeIcon)
        {
            EnsureRefs();
            bool show = !string.IsNullOrEmpty(text);
            gameObject.SetActive(show);
            if (!show)
                return;
            if (label != null)
            {
                label.text = text;
                label.fontStyle = failed
                    ? label.fontStyle | FontStyles.Strikethrough
                    : label.fontStyle & ~FontStyles.Strikethrough;
                Color color = label.color;
                color.a = failed ? FailedAlpha : 1f;
                label.color = color;
            }
            if (icon != null)
            {
                Sprite sprite = complete && !failed ? completeIcon : incompleteIcon;
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
        }
        public RectTransform SlideRoot => (RectTransform)transform;
        #endregion

        #region Private Methods
        void EnsureRefs()
        {
            if (label == null)
            {
                Transform text = transform.Find("ObjectiveText");
                if (text != null)
                    label = text.GetComponent<TMP_Text>();
                if (label == null)
                    label = GetComponentInChildren<TMP_Text>(true);
            }
            if (icon == null)
            {
                Transform iconTf = transform.Find("ObjectiveIcon");
                if (iconTf != null)
                    icon = iconTf.GetComponent<Image>();
            }
        }
        #endregion
    }
}
