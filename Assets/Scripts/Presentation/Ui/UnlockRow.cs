using ModularChess.Core;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class UnlockRow : MonoBehaviour
    {
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] Image statusIcon;
        [SerializeField] Sprite lockedSprite;
        [SerializeField] Sprite unlockedSprite;

        ModeId _id;
        Button _rowButton;

        public void Bind(ModeDefinition definition, UnityAction<ModeId> opened)
        {
            _id = definition.Id;
            if (nameLabel != null)
            {
                nameLabel.text = definition.DisplayName;
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

            Sprite sprite = ModeDlc.IsOwned(_id) ? unlockedSprite : lockedSprite;
            if (sprite != null)
                statusIcon.sprite = sprite;
        }

        void EnsureRowButton(UnityAction<ModeId> opened)
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
            _rowButton.onClick.RemoveAllListeners();
            ModeId captured = _id;
            _rowButton.onClick.AddListener(() => opened?.Invoke(captured));
        }
    }
}
