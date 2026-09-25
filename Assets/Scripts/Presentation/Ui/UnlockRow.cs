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
        static readonly Color Affordable = Color.black;
        static readonly Color Unaffordable = new Color(0.75f, 0.08f, 0.08f, 1f);
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Image statusIcon;
        [SerializeField] Sprite lockedSprite;
        [SerializeField] Sprite unlockedSprite;
        [SerializeField] TMP_Text priceLabel;
        [SerializeField] GameObject priceIcon;
        UnlockProduct _product;
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
            if (priceLabel != null)
                priceLabel.raycastTarget = false;
            UnlockProduct captured = product;
            GameAudio.Bind(GetComponent<Button>(), () => opened?.Invoke(captured));
            Refresh();
        }
        public void Refresh()
        {
            bool owned = MeritUnlocks.IsOwned(_product);
            if (statusIcon != null)
            {
                Sprite sprite = owned ? unlockedSprite : lockedSprite;
                if (sprite != null)
                    statusIcon.sprite = sprite;
            }
            if (priceLabel != null)
            {
                priceLabel.gameObject.SetActive(!owned);
                if (!owned)
                {
                    int cost = MeritUnlocks.Cost(_product);
                    priceLabel.text = cost.ToString();
                    priceLabel.color = MeritWallet.Balance >= cost ? Affordable : Unaffordable;
                }
            }
            if (priceIcon != null)
                priceIcon.SetActive(!owned);
        }
        #endregion
    }
}
