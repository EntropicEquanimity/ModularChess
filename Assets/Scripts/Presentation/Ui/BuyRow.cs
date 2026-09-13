using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class BuyRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Button buyButton;

        public void Bind(string displayName, UnityAction onBuy)
        {
            if (nameLabel != null)
                nameLabel.text = displayName;

            if (buyButton == null)
                return;

            buyButton.onClick.RemoveAllListeners();
            if (onBuy != null)
                buyButton.onClick.AddListener(onBuy);
        }
    }
}
