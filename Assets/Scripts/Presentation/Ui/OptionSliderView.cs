using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class OptionSliderView : MonoBehaviour
    {
        #region Fields
        [SerializeField] TMP_Text optionName;
        [SerializeField] Slider slider;
        [SerializeField] TMP_Text value;
        Func<string> _valueText;
        #endregion

        #region Public Methods
        public void Bind(
            string settingName,
            float min,
            float max,
            bool wholeNumbers,
            float current,
            UnityAction<float> changed,
            Func<string> valueText)
        {
            EnsureRefs();
            _valueText = valueText;
            if (optionName != null)
            {
                optionName.text = settingName ?? string.Empty;
            }

            if (slider == null)
            {
                return;
            }

            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = wholeNumbers;
            slider.SetValueWithoutNotify(Mathf.Clamp(current, min, max));
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v =>
            {
                if (changed != null)
                {
                    changed.Invoke(v);
                }

                RefreshValue();
            });
            RefreshValue();
        }
        #endregion

        #region Private Methods
        void EnsureRefs()
        {
            if (optionName == null)
            {
                Transform named = transform.Find("OptionName");
                if (named != null)
                {
                    optionName = named.GetComponent<TMP_Text>();
                }
            }

            if (slider == null)
            {
                Transform named = transform.Find("Slider");
                if (named != null)
                {
                    slider = named.GetComponent<Slider>();
                }

                if (slider == null)
                {
                    slider = GetComponentInChildren<Slider>(true);
                }
            }

            if (value == null)
            {
                Transform named = transform.Find("Value");
                if (named != null)
                {
                    value = named.GetComponent<TMP_Text>();
                }
            }
        }
        void RefreshValue()
        {
            if (value == null)
            {
                return;
            }

            if (_valueText != null)
            {
                value.text = _valueText() ?? string.Empty;
                return;
            }

            if (slider != null)
            {
                value.text = slider.wholeNumbers
                    ? slider.value.ToString("0")
                    : slider.value.ToString("0.#");
            }
        }
        #endregion
    }
}
