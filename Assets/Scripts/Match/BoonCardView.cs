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
        [SerializeField] TMP_Text titleLabel;
        [SerializeField] TMP_Text descriptionLabel;
        [SerializeField] TMP_Text rarityLabel;
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
                rarityLabel.text = def.Rarity.ToString();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                GameAudio.Bind(button, onClick);
            }
        }
        #endregion

        #region Private Methods
        void Resolve()
        {
            if (titleLabel == null)
                titleLabel = FindTmp("Title");
            if (descriptionLabel == null)
                descriptionLabel = FindTmp("Description");
            if (rarityLabel == null)
                rarityLabel = FindTmp("Rarity");
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
