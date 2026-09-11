using ModularChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class UnlockRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Button buyButton;

        ModeId _id;

        public void Bind(ModeDefinition definition)
        {
            _id = definition.Id;
            if (nameLabel != null)
                nameLabel.text = definition.DisplayName;
            if (buyButton != null)
            {
                buyButton.onClick.RemoveAllListeners();
                buyButton.onClick.AddListener(Buy);
            }

            Refresh();
        }

        void Buy()
        {
            ModeDlc.Purchase(_id);
            Refresh();
        }

        void Refresh()
        {
            bool owned = ModeDlc.IsOwned(_id);
            if (buyButton == null)
                return;
            buyButton.interactable = !owned;
            TMP_Text label = buyButton.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = owned ? "Owned" : "Buy";
        }
    }
}
