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

        ModeId? _modeId;
        Activity? _activity;
        Button _rowButton;

        public void Bind(ModeDefinition definition, UnityAction<ModeId> opened)
        {
            _modeId = definition.Id;
            _activity = null;
            if (nameLabel != null)
            {
                nameLabel.text = Loc.ModeName(definition.Id);
                nameLabel.raycastTarget = false;
            }

            if (statusIcon != null)
                statusIcon.raycastTarget = false;

            EnsureRowButton(() => opened?.Invoke(_modeId.Value));
            Refresh();
        }

        public void BindActivity(Activity activity, UnityAction<Activity> opened)
        {
            _activity = activity;
            _modeId = null;
            if (nameLabel != null)
            {
                nameLabel.text = Loc.ActivityName(activity);
                nameLabel.raycastTarget = false;
            }

            if (statusIcon != null)
                statusIcon.raycastTarget = false;

            EnsureRowButton(() => opened?.Invoke(_activity.Value));
            Refresh();
        }

        public void Refresh()
        {
            if (statusIcon == null)
                return;

            bool owned = _activity.HasValue
                ? ActivityDlc.IsOwned(_activity.Value)
                : _modeId.HasValue && ModeDlc.IsOwned(_modeId.Value);
            Sprite sprite = owned ? unlockedSprite : lockedSprite;
            if (sprite != null)
                statusIcon.sprite = sprite;
        }

        void EnsureRowButton(UnityAction opened)
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
            GameAudio.Bind(_rowButton, opened);
        }
    }
}
