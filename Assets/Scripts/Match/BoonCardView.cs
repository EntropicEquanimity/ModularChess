using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class BoonCardView : MonoBehaviour
    {
        #region Fields
        static readonly Color Grey = new Color(0.45f, 0.45f, 0.48f, 1f);
        static readonly Color Blue = new Color(0.28f, 0.45f, 0.82f, 1f);
        static readonly Color Gold = new Color(0.86f, 0.68f, 0.22f, 1f);
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] TMP_Text rarityLabel;
        [SerializeField] Image background;
        [SerializeField] Button button;
        #endregion

        #region Public Methods
        public void Present(BoonDefinition def, UnityAction onClick)
        {
            Resolve();
            if (def == null)
                return;
            if (titleLabel != null)
                titleLabel.text = Loc.Get(def.NameKey);
            if (descriptionLabel != null)
                descriptionLabel.text = Loc.Get(def.DescriptionKey);
            if (rarityLabel != null)
            {
                rarityLabel.text = string.Empty;
                rarityLabel.gameObject.SetActive(false);
            }
            if (background != null)
                background.color = ColorFor(def.Rarity);
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                GameAudio.Bind(button, onClick);
            }
        }
        #endregion

        #region Private Methods
        static Color ColorFor(BoonRarity rarity)
        {
            switch (rarity)
            {
                case BoonRarity.Blue:
                    return Blue;
                case BoonRarity.Gold:
                    return Gold;
                default:
                    return Grey;
            }
        }
        void Resolve()
        {
            if (titleLabel == null)
                titleLabel = FindTmp("Title");
            if (descriptionLabel == null)
                descriptionLabel = FindTmp("Description");
            if (rarityLabel == null)
                rarityLabel = FindTmp("Rarity");
            if (background == null)
                background = GetComponent<Image>();
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
