using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class SettingsControl : MonoBehaviour
    {
        [SerializeField] TMP_Text nameLabel;
        [SerializeField] TMP_Text valueLabel;
        [SerializeField] Button minusButton;
        [SerializeField] Button plusButton;

        Func<int> _get;
        Action<int> _set;
        Func<int, string> _format;
        int _min;
        int _max;

        public void Bind(string label, Func<int> get, Action<int> set, int min, int max, Func<int, string> format = null)
        {
            _get = get;
            _set = set;
            _format = format;
            _min = min;
            _max = max;
            if (nameLabel != null)
                nameLabel.text = label;
            if (minusButton != null)
                GameAudio.Bind(minusButton, () => Step(-1));
            if (plusButton != null)
                GameAudio.Bind(plusButton, () => Step(1));
            Refresh();
        }

        void Step(int delta)
        {
            if (_get == null || _set == null)
                return;
            _set(Mathf.Clamp(_get() + delta, _min, _max));
            Refresh();
        }

        void Refresh()
        {
            int value = _get != null ? _get() : 0;
            if (valueLabel != null)
                valueLabel.text = _format != null ? _format(value) : value.ToString();
            if (minusButton != null)
                minusButton.interactable = value > _min;
            if (plusButton != null)
                plusButton.interactable = value < _max;
        }
    }
}
