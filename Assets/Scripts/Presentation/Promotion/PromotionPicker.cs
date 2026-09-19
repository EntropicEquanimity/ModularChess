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
        #region Fields
        static readonly PieceType[] Options =
        {
            PieceType.Queen,
            PieceType.Rook,
            PieceType.Bishop,
            PieceType.Knight
        };
        [SerializeField] GameObject popupPrefab;
        Canvas _canvas;
        GameObject _popup;
        readonly Button[] _buttons = new Button[4];
        readonly Image[] _icons = new Image[4];
        bool _open;
        bool _built;
        public event Action<PieceType> PromotionChosen;
        public bool IsOpen => _open;
        #endregion

        #region Unity
        void Awake()
        {
            EnsureBuilt();
            Hide();
        }
        void Update()
        {
            if (!_open)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.qKey.wasPressedThisFrame)
            {
                Choose(PieceType.Queen);
            }
            else if (keyboard.rKey.wasPressedThisFrame)
            {
                Choose(PieceType.Rook);
            }
            else if (keyboard.bKey.wasPressedThisFrame)
            {
                Choose(PieceType.Bishop);
            }
            else if (keyboard.nKey.wasPressedThisFrame)
            {
                Choose(PieceType.Knight);
            }
        }
        #endregion

        #region Public Methods
        public void Show(Side side)
        {
            EnsureBuilt();
            if (_popup == null)
            {
                return;
            }

            EnsureEventSystem();
            ApplySide(side);
            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(true);
            }

            _popup.SetActive(true);
            _open = true;
            if (EventSystem.current != null && _buttons[0] != null)
            {
                EventSystem.current.SetSelectedGameObject(_buttons[0].gameObject);
            }
        }
        public void Hide()
        {
            _open = false;
            if (_popup != null)
            {
                _popup.SetActive(false);
            }

            if (_canvas != null)
            {
                _canvas.gameObject.SetActive(false);
            }
        }
        #endregion

        #region Private Methods
        void EnsureBuilt()
        {
            if (_built)
            {
                return;
            }

            _built = true;
            GameObject prefab = popupPrefab != null ? popupPrefab : RuntimePrefabs.PromotionPopup;
            if (prefab == null)
            {
                return;
            }

            _canvas = UiFactory.CreateCanvas(transform, 250);
            _canvas.gameObject.name = "PromotionCanvas";
            _popup = Instantiate(prefab, _canvas.transform);
            _popup.name = "PromotionPopup";
            LocalizedText.Bind(FindNamed(_popup.transform, "Title"), "promotion.title");
            var view = _popup.GetComponent<PromotionPopupView>();
            if (view == null)
                view = _popup.AddComponent<PromotionPopupView>();
            view.Bind(Choose);
            if (view.FirstButton != null)
                _buttons[0] = view.FirstButton;
        }
        void ApplySide(Side side)
        {
            var view = _popup != null ? _popup.GetComponent<PromotionPopupView>() : null;
            if (view != null)
            {
                view.SetSide(side);
                return;
            }
            for (int i = 0; i < Options.Length; i++)
            {
                Image icon = _icons[i];
                if (icon == null)
                    continue;
                icon.sprite = ChessGlyphs.GetSprite(Options[i], side);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
        }
        void Choose(PieceType type)
        {
            if (!_open)
            {
                return;
            }

            Hide();
            PromotionChosen?.Invoke(type);
        }
        static Transform FindNamed(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
        static Image FindIcon(Transform root)
        {
            Transform named = root.Find("Image");
            if (named != null)
            {
                Image namedImage = named.GetComponent<Image>();
                if (namedImage != null)
                {
                    return namedImage;
                }
            }

            Image[] images = root.GetComponentsInChildren<Image>(true);
            for (int i = 0; i < images.Length; i++)
            {
                if (images[i].transform != root)
                {
                    return images[i];
                }
            }

            return null;
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
            {
                existing.gameObject.AddComponent<InputSystemUIInputModule>();
            }

            var standalone = existing.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                standalone.enabled = false;
            }
        }
        #endregion
    }
}
