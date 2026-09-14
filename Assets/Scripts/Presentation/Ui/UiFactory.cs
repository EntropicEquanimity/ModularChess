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
            if (button == null)
                button = go.AddComponent<Button>();
            GameAudio.Bind(button, onClick);
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
            toggle.onValueChanged.AddListener(_ => GameAudio.PlayUi());
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
            dropdown.onValueChanged.AddListener(_ => GameAudio.PlayUi());
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

        public static RectTransform Panel(Transform parent, Vector2 size)
        {
            GameObject prefab = RuntimePrefabs.Panel;
            GameObject go = prefab != null
                ? Object.Instantiate(prefab, parent)
                : new GameObject("Panel", typeof(RectTransform), typeof(Image));
            if (prefab == null)
                go.transform.SetParent(parent, false);
            go.name = "Panel";
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            return rect;
        }

        public static TextMeshProUGUI DescriptionBox(Transform parent, string text, Vector2 size)
        {
            GameObject prefab = RuntimePrefabs.DescriptionBox;
            GameObject go = prefab != null
                ? Object.Instantiate(prefab, parent)
                : new GameObject("DescriptionBox", typeof(RectTransform), typeof(Image));
            if (prefab == null)
                go.transform.SetParent(parent, false);
            go.name = "DescriptionBox";
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.sizeDelta = size;
            TextMeshProUGUI label = go.GetComponentInChildren<TextMeshProUGUI>();
            if (label == null)
                label = Label(go.transform, text, 16, TextAlignmentOptions.Center);
            label.text = text ?? string.Empty;
            label.color = Color.black;
            label.extraPadding = false;
            label.textWrappingMode = TextWrappingModes.Normal;
            return label;
        }

        public static Button ImageButton(Transform parent, Sprite sprite, UnityAction onClick)
        {
            GameObject prefab = RuntimePrefabs.ImageButton;
            GameObject go = prefab != null
                ? Object.Instantiate(prefab, parent)
                : new GameObject("ImageButton", typeof(RectTransform), typeof(Image), typeof(Button));
            if (prefab == null)
                go.transform.SetParent(parent, false);
            go.name = "ImageButton";

            Image icon = FindIconImage(go.transform);
            if (icon == null)
            {
                var iconGo = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                iconGo.transform.SetParent(go.transform, false);
                icon = iconGo.GetComponent<Image>();
            }

            icon.preserveAspect = true;
            icon.raycastTarget = false;
            if (sprite != null)
                icon.sprite = sprite;
            if (icon.sprite != null)
                icon.SetNativeSize();

            RectTransform buttonRect = go.GetComponent<RectTransform>();
            buttonRect.sizeDelta = icon.rectTransform.sizeDelta + new Vector2(16f, 16f);

            Button button = go.GetComponent<Button>();
            if (button == null)
                button = go.AddComponent<Button>();
            GameAudio.Bind(button, onClick);
            return button;
        }

        public static Slider Slider(Transform parent, float value, UnityAction<float> changed)
        {
            var go = new GameObject("Slider", typeof(RectTransform), typeof(Slider));
            go.transform.SetParent(parent, false);
            var slider = go.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            Image background = CreateSliderImage(go.transform, "Background", new Color(0.75f, 0.75f, 0.75f, 1f));
            RectTransform backgroundRect = background.rectTransform;
            backgroundRect.anchorMin = new Vector2(0f, 0.25f);
            backgroundRect.anchorMax = new Vector2(1f, 0.75f);
            backgroundRect.offsetMin = Vector2.zero;
            backgroundRect.offsetMax = Vector2.zero;

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(go.transform, false);
            RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(4f, 0f);
            fillAreaRect.offsetMax = new Vector2(-4f, 0f);

            Image fill = CreateSliderImage(fillArea.transform, "Fill", new Color(0.15f, 0.15f, 0.15f, 1f));
            RectTransform fillRect = fill.rectTransform;
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            handleArea.transform.SetParent(go.transform, false);
            RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
            handleAreaRect.anchorMin = Vector2.zero;
            handleAreaRect.anchorMax = Vector2.one;
            handleAreaRect.offsetMin = new Vector2(8f, 0f);
            handleAreaRect.offsetMax = new Vector2(-8f, 0f);

            Image handle = CreateSliderImage(handleArea.transform, "Handle", Color.black);
            RectTransform handleRect = handle.rectTransform;
            handleRect.sizeDelta = new Vector2(12f, 0f);

            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle;
            slider.value = Mathf.Clamp01(value);
            slider.onValueChanged.RemoveAllListeners();
            if (changed != null)
                slider.onValueChanged.AddListener(changed);
            return slider;
        }

        static Image CreateSliderImage(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Image image = go.GetComponent<Image>();
            image.sprite = RuntimeSprites.Pixel;
            image.color = color;
            image.type = Image.Type.Simple;
            return image;
        }

        static Image FindIconImage(Transform root)
        {
            Transform named = root.Find("Image");
            if (named != null)
            {
                Image namedImage = named.GetComponent<Image>();
                if (namedImage != null)
                    return namedImage;
            }

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].transform != root)
                    return images[i];
            }

            return null;
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
