using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class EffectIconView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Fields
        [SerializeField] Image icon;
        PieceDetailsPanel _details;
        string _title = string.Empty;
        string _body = string.Empty;
        public static bool Hovered { get; private set; }
        #endregion

        #region Unity
        void Awake()
        {
            if (icon == null)
                icon = GetComponent<Image>();
        }
        void OnDisable()
        {
            if (Hovered)
                Hovered = false;
        }
        #endregion

        #region Public Methods
        public void Bind(Sprite sprite, string title, string body, PieceDetailsPanel details)
        {
            if (icon == null)
                icon = GetComponent<Image>();
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
            _title = title ?? string.Empty;
            _body = body ?? string.Empty;
            _details = details;
            gameObject.SetActive(true);
        }
        public void OnPointerEnter(PointerEventData eventData)
        {
            Hovered = true;
            if (_details == null || _title.Length == 0)
                return;
            _details.ShowEffect(_title, _body);
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            Hovered = false;
            _details?.Hide();
        }
        #endregion
    }
}
