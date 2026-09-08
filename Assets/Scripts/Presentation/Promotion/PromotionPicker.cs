using System;
using ModularChess.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ModularChess.Presentation
{
    public sealed class PromotionPicker : MonoBehaviour
    {
        static readonly PieceType[] Options =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };

        public event Action<PieceType> PromotionChosen;

        Canvas _canvas;
        Text _title;
        readonly Button[] _buttons = new Button[4];
        readonly Image[] _glyphs = new Image[4];
        readonly Image[] _bodies = new Image[4];
        bool _open;
        bool _built;

        public bool IsOpen => _open;

        void Awake()
        {
            EnsureBuilt();
            Hide();
        }

        public void Show(Side side)
        {
            EnsureBuilt();
            EnsureEventSystem();
            ApplySide(side);
            gameObject.SetActive(true);
            _canvas.enabled = true;
            _open = true;

            if (EventSystem.current != null && _buttons[0] != null)
                EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject);
        }

        public void Hide()
        {
            _open = false;
            if (_canvas != null)
                _canvas.enabled = false;
        }

        void Update()
        {
            if (!_open)
                return;

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard.qKey.wasPressedThisFrame)
                Choose(PieceType.Queen);
            else if (keyboard.rKey.wasPressedThisFrame)
                Choose(PieceType.Rook);
            else if (keyboard.bKey.wasPressedThisFrame)
                Choose(PieceType.Bishop);
            else if (keyboard.nKey.wasPressedThisFrame)
                Choose(PieceType.Knight);
        }

        void EnsureBuilt()
        {
            if (_built)
                return;

            var overlay = new GameObject("PromotionOverlay", typeof(RectTransform));
            overlay.transform.SetParent(transform, false);

            _canvas = overlay.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = overlay.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            overlay.AddComponent<GraphicRaycaster>();

            var panel = CreateUi("Panel", overlay.transform);
            var panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(620f, 280f);

            var panelImage = panel.AddComponent<Image>();
            panelImage.sprite = RuntimeSprites.Pixel;
            panelImage.color = new Color(0.1f, 0.11f, 0.13f, 0.94f);

            var vertical = panel.AddComponent<VerticalLayoutGroup>();
            vertical.padding = new RectOffset(28, 28, 20, 24);
            vertical.spacing = 12f;
            vertical.childAlignment = TextAnchor.UpperCenter;
            vertical.childControlHeight = false;
            vertical.childControlWidth = true;
            vertical.childForceExpandHeight = false;
            vertical.childForceExpandWidth = true;

            var titleGo = CreateUi("Title", panel.transform);
            var titleRect = titleGo.GetComponent<RectTransform>();
            titleRect.sizeDelta = new Vector2(0f, 48f);
            _title = titleGo.AddComponent<Text>();
            _title.font = LoadUiFont();
            _title.fontSize = 32;
            _title.alignment = TextAnchor.MiddleCenter;
            _title.color = Color.white;
            _title.text = "Promote pawn";
            _title.enabled = _title.font != null;
            var titleLayout = titleGo.AddComponent<LayoutElement>();
            titleLayout.preferredHeight = 48f;

            var row = CreateUi("Buttons", panel.transform);
            var rowRect = row.GetComponent<RectTransform>();
            rowRect.sizeDelta = new Vector2(0f, 160f);
            var horizontal = row.AddComponent<HorizontalLayoutGroup>();
            horizontal.spacing = 18f;
            horizontal.childAlignment = TextAnchor.MiddleCenter;
            horizontal.childForceExpandHeight = true;
            horizontal.childForceExpandWidth = true;
            var rowLayout = row.AddComponent<LayoutElement>();
            rowLayout.preferredHeight = 160f;

            for (int i = 0; i < Options.Length; i++)
                _buttons[i] = CreateOptionButton(row.transform, Options[i], i);

            _built = true;
        }

        Button CreateOptionButton(Transform parent, PieceType type, int index)
        {
            var go = CreateUi(type.ToString(), parent);
            var image = go.AddComponent<Image>();
            image.sprite = RuntimeSprites.Circle;
            image.color = Color.white;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.navigation = new Navigation { mode = Navigation.Mode.Automatic };
            PieceType captured = type;
            button.onClick.AddListener(() => Choose(captured));

            var layout = go.AddComponent<LayoutElement>();
            layout.preferredWidth = 128f;
            layout.preferredHeight = 128f;
            layout.minWidth = 96f;
            layout.minHeight = 96f;

            var glyphGo = CreateUi("Glyph", go.transform);
            var glyphRect = glyphGo.GetComponent<RectTransform>();
            glyphRect.anchorMin = new Vector2(0.18f, 0.18f);
            glyphRect.anchorMax = new Vector2(0.82f, 0.82f);
            glyphRect.offsetMin = Vector2.zero;
            glyphRect.offsetMax = Vector2.zero;
            var glyph = glyphGo.AddComponent<Image>();
            glyph.sprite = ChessGlyphs.GetSprite(type);
            glyph.preserveAspect = true;
            glyph.raycastTarget = false;

            _bodies[index] = image;
            _glyphs[index] = glyph;
            return button;
        }

        void ApplySide(Side side)
        {
            bool white = side == Side.White;
            Color fill = white
                ? BoardTheme.Default.WhitePieceFill
                : BoardTheme.Default.BlackPieceFill;
            Color glyph = white
                ? BoardTheme.Default.WhitePieceGlyph
                : BoardTheme.Default.BlackPieceGlyph;

            if (_title != null)
                _title.color = Color.white;

            for (int i = 0; i < _bodies.Length; i++)
            {
                if (_bodies[i] != null)
                    _bodies[i].color = fill;
                if (_glyphs[i] != null)
                    _glyphs[i].color = glyph;
            }
        }

        void Choose(PieceType type)
        {
            if (!_open)
                return;

            Hide();
            PromotionChosen?.Invoke(type);
        }

        static GameObject CreateUi(string objectName, Transform parent)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        static void EnsureEventSystem()
        {
            EventSystem existing = EventSystem.current;
            if (existing == null)
            {
                var go = new GameObject("EventSystem");
                existing = go.AddComponent<EventSystem>();
                go.AddComponent<InputSystemUIInputModule>();
                return;
            }

            if (existing.GetComponent<InputSystemUIInputModule>() == null)
                existing.gameObject.AddComponent<InputSystemUIInputModule>();

            var standalone = existing.GetComponent<StandaloneInputModule>();
            if (standalone != null)
                standalone.enabled = false;
        }

        static Font LoadUiFont()
        {
            Font font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            return font;
        }
    }
}
