using UnityEngine;

namespace ModularChess.Presentation
{
    public interface IPopupContent
    {
        void Bind(DetailsPopup popup);
    }

    public enum PopupDisableMode
    {
        MouseExit,
        OnUnselect
    }

    [System.Flags]
    public enum PopupTriggerMode
    {
        Hover = 1,
        Click = 2
    }

    public sealed class PopupTrigger : MonoBehaviour
    {
        #region Fields
        [SerializeField] PopupTriggerMode modes = PopupTriggerMode.Hover | PopupTriggerMode.Click;
        [SerializeField] float hoverDurationToTrigger = 1f;
        [SerializeField] bool popupFadeAway;
        [SerializeField] bool popupFadeIn;
        [SerializeField] PopupDisableMode disableMode = PopupDisableMode.MouseExit;
        [SerializeField] string title;
        [SerializeField] [TextArea] string body;
        #endregion

        #region Public Methods
        public PopupTriggerMode Modes => modes;
        public float HoverDurationToTrigger => hoverDurationToTrigger;
        public bool PopupFadeAway => popupFadeAway;
        public bool PopupFadeIn => popupFadeIn;
        public PopupDisableMode DisableMode => disableMode;
        public string Title => title;
        public string Body => body;
        public bool AllowsHover => (modes & PopupTriggerMode.Hover) != 0;
        public bool AllowsClick => (modes & PopupTriggerMode.Click) != 0;
        public void BindText(string popupTitle, string popupBody)
        {
            title = popupTitle ?? string.Empty;
            body = popupBody ?? string.Empty;
        }
        public void Fill(DetailsPopup popup)
        {
            if (popup == null)
                return;
            IPopupContent content = GetComponent<IPopupContent>();
            if (content != null)
            {
                content.Bind(popup);
                return;
            }
            popup.Present(title, body);
        }
        #endregion
    }
}
