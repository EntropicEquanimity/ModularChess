using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class ShopCardView : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text priceLabel;
        [SerializeField] Image icon;
        [SerializeField] Button button;
        #endregion

        #region Public Methods
        public void Present(ShopItem item, int gold, bool armyFull, UnityAction onBuy)
        {
            Resolve();
            bool sold = item.Price < 0;
            if (titleLabel != null)
                titleLabel.text = sold ? Loc.Get("roguelike.shop.sold") : Loc.PieceName(item.Type);
            if (priceLabel != null)
                priceLabel.text = sold ? string.Empty : Loc.Format("roguelike.shop.price", item.Price);
            if (icon != null)
            {
                icon.enabled = !sold;
                if (!sold)
                {
                    icon.sprite = ChessGlyphs.GetSprite(item.Type, Side.White);
                    icon.SetNativeSize();
                }
            }
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                bool canBuy = !sold && !armyFull && gold >= item.Price;
                button.interactable = canBuy;
                if (canBuy)
                    GameAudio.Bind(button, onBuy);
            }
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (titleLabel == null)
                titleLabel = FindTmp("Title");
            if (priceLabel == null)
                priceLabel = FindTmp("Price");
            if (icon == null)
            {
                Transform t = FindChild(transform, "Icon");
                if (t != null)
                    icon = t.GetComponent<Image>();
            }
            if (button == null)
                button = GetComponent<Button>();
        }
        TMP_Text FindTmp(string name)
        {
            Transform t = FindChild(transform, name);
            return t != null ? t.GetComponent<TMP_Text>() : null;
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
