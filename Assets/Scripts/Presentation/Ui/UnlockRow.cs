using ModularChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class UnlockRow : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Image statusIcon;
        [SerializeField] Sprite lockedSprite;
        [SerializeField] Sprite unlockedSprite;
        UnlockProduct _product;
        Button _rowButton;
        #endregion

        #region Public Methods
        public void Bind(UnlockProduct product, UnityAction<UnlockProduct> opened)
        {
            _product = product;
            if (nameLabel != null)
            {
                nameLabel.text = Loc.UnlockName(product);
                nameLabel.raycastTarget = false;
            }
            if (statusIcon != null)
                statusIcon.raycastTarget = false;
            EnsureRowButton(opened);
            Refresh();
        }
        public void Refresh()
        {
            if (statusIcon == null)
                return;
            Sprite sprite = MeritUnlocks.IsOwned(_product) ? unlockedSprite : lockedSprite;
            if (sprite != null)
                statusIcon.sprite = sprite;
        }
        #endregion

        #region Private Methods
        void EnsureRowButton(UnityAction<UnlockProduct> opened)
        {
            _rowButton = GetComponent<Button>();
            if (_rowButton == null)
                _rowButton = gameObject.AddComponent<Button>();
            Image hit = GetComponent<Image>();
            if (hit == null)
            {
                hit = gameObject.AddComponent<Image>();
                hit.color = new Color(1f, 1f, 1f, 0f);
            }
            hit.raycastTarget = true;
            _rowButton.targetGraphic = hit;
            _rowButton.transition = Selectable.Transition.None;
            UnlockProduct captured = _product;
            GameAudio.Bind(_rowButton, () => opened?.Invoke(captured));
        }
        #endregion
    }
}
