using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public static class UiFactory
    {
        public static Canvas CreateCanvas(Transform parent, int sortingOrder)
        {
            GameObject prefab = RuntimePrefabs.Canvas;
            GameObject go = prefab != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : CreateFallbackCanvas(parent);
            go.name = "UICanvas";
            Canvas canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortingOrder;
            if (go.GetComponent<GraphicRaycaster>() == null)
            {
                go.AddComponent<GraphicRaycaster>();
            }

            return canvas;
        }

        public static Button Button(Transform parent, string label, UnityAction onClick, Vector2 size)
        {
            GameObject prefab = RuntimePrefabs.TextButton;
            GameObject go = prefab != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : CreateFallbackButton(parent);
            go.name = label;
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
                text.extraPadding = false;
            }

            Button button = go.GetComponent<Button>();
            button.onClick.RemoveAllListeners();
            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            return button;
        }

        public static TextMeshProUGUI Label(Transform parent, string text, int fontSize, TextAlignmentOptions align)
        {
            var go = new GameObject("Label", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize < 16 ? 16 : fontSize - (fontSize % 16);
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.extraPadding = false;
            tmp.raycastTarget = false;
            TMP_FontAsset font = Resources.Load<TMP_FontAsset>("Fonts & Materials/PixelOperator SDF");
            if (font != null)
            {
                tmp.font = font;
            }

            return tmp;
        }

        public static Toggle Toggle(Transform parent, string label, bool on, UnityAction<bool> changed)
        {
            GameObject prefab = RuntimePrefabs.Toggle;
            GameObject go;
            Toggle toggle;
            if (prefab != null)
            {
                go = UnityEngine.Object.Instantiate(prefab, parent);
                toggle = go.GetComponent<Toggle>();
            }
            else
            {
                go = new GameObject("Toggle", typeof(RectTransform), typeof(Toggle));
                go.transform.SetParent(parent, false);
                toggle = go.GetComponent<Toggle>();
            }

            go.name = label;
            TextMeshProUGUI text = go.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = label;
            }

            toggle.isOn = on;
            toggle.onValueChanged.RemoveAllListeners();
            if (changed != null)
            {
                toggle.onValueChanged.AddListener(changed);
            }

            return toggle;
        }

        public static TMP_Dropdown Dropdown(Transform parent, IList<string> options, int selected, UnityAction<int> changed)
        {
            GameObject prefab = RuntimePrefabs.Dropdown;
            GameObject go = prefab != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : new GameObject("Dropdown", typeof(RectTransform), typeof(TMP_Dropdown));
            if (prefab == null)
            {
                go.transform.SetParent(parent, false);
            }

            var dropdown = go.GetComponent<TMP_Dropdown>();
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.value = selected;
            dropdown.onValueChanged.RemoveAllListeners();
            if (changed != null)
            {
                dropdown.onValueChanged.AddListener(changed);
            }

            return dropdown;
        }

        public static TMP_InputField Input(Transform parent, string placeholder)
        {
            GameObject prefab = RuntimePrefabs.InputField;
            GameObject go = prefab != null
                ? UnityEngine.Object.Instantiate(prefab, parent)
                : new GameObject("Input", typeof(RectTransform), typeof(TMP_InputField));
            if (prefab == null)
            {
                go.transform.SetParent(parent, false);
            }

            var field = go.GetComponent<TMP_InputField>();
            if (field.placeholder is TextMeshProUGUI placeholderText)
            {
                placeholderText.text = placeholder;
                placeholderText.extraPadding = false;
            }

            if (field.textComponent != null)
            {
                field.textComponent.extraPadding = false;
            }

            return field;
        }

        static GameObject CreateFallbackCanvas(Transform parent)
        {
            var go = new GameObject("UICanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.SetParent(parent, false);
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.referencePixelsPerUnit = 16f;
            return go;
        }

        static GameObject CreateFallbackButton(Transform parent)
        {
            var go = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var textGo = new GameObject("Text", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.alignment = TextAlignmentOptions.Center;
            var rect = textGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }
    }
}
