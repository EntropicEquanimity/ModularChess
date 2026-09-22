using System.Collections.Generic;
using ModularChess.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace ModularChess.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BoardPointerInput : MonoBehaviour
    {
        [SerializeField] Camera pickCamera;
        [SerializeField] PromotionPicker promotionPicker;

        static readonly List<RaycastResult> SharedHits = new List<RaycastResult>(16);
        readonly List<RaycastResult> _uiHits = new List<RaycastResult>(8);
        BoardView _board;
        bool _pressOnBoard;

        public Camera PickCamera
        {
            get => pickCamera;
            set => pickCamera = value;
        }

        public PromotionPicker PromotionPicker
        {
            get => promotionPicker;
            set => promotionPicker = value;
        }

        void Awake()
        {
            _board = GetComponent<BoardView>();
            if (_board == null)
                _board = GetComponentInParent<BoardView>();
        }

        void Update()
        {
            if (_board == null)
                return;

            Pointer pointer = Pointer.current;
            if (pointer == null)
                return;

            Vector2 screen = pointer.position.ReadValue();
            bool uiBlocked = IsBlockedByUi(screen);
            if (!uiBlocked && !IsPromotionOpen())
                UpdateHover(screen);
            else
                _board.NotifySquareHovered(null);

            if (pointer.press.wasPressedThisFrame)
            {
                _pressOnBoard = false;
                if (IsPromotionOpen() || uiBlocked)
                    return;
                if (!TryPick(screen, out Square pressed))
                    return;
                _pressOnBoard = true;
                _board.NotifySquarePressed(pressed);
                _board.NotifySquareClicked(pressed);
                return;
            }

            if (!pointer.press.wasReleasedThisFrame || !_pressOnBoard)
                return;
            _pressOnBoard = false;
            if (uiBlocked || IsPromotionOpen())
            {
                _board.NotifySquareReleased(null);
                return;
            }
            if (TryPick(screen, out Square released))
                _board.NotifySquareReleased(released);
            else
                _board.NotifySquareReleased(null);
        }

        public static bool IsScreenBlockedByUi()
        {
            Pointer pointer = Pointer.current;
            if (pointer == null)
                return false;
            return IsBlockedByUiAt(pointer.position.ReadValue(), SharedHits);
        }

        bool TryPick(Vector2 screen, out Square square)
        {
            square = default;
            Camera camera = pickCamera != null ? pickCamera : Camera.main;
            if (camera == null || !camera.pixelRect.Contains(screen))
                return false;
            float depth = Mathf.Abs(camera.transform.position.z - _board.transform.position.z);
            Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            return _board.TryPickSquare(world, out square);
        }

        void UpdateHover(Vector2 screen)
        {
            if (TryPick(screen, out Square square))
                _board.NotifySquareHovered(square);
            else
                _board.NotifySquareHovered(null);
        }

        bool IsPromotionOpen()
        {
            if (promotionPicker == null)
                promotionPicker = FindAnyObjectByType<PromotionPicker>();
            return promotionPicker != null && promotionPicker.IsOpen;
        }

        bool IsBlockedByUi(Vector2 screen)
        {
            return IsBlockedByUiAt(screen, _uiHits);
        }

        static bool IsBlockedByUiAt(Vector2 screen, List<RaycastResult> hits)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var eventData = new PointerEventData(eventSystem)
            {
                position = screen
            };
            hits.Clear();
            eventSystem.RaycastAll(eventData, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                GameObject hit = hits[i].gameObject;
                if (hit == null)
                    continue;
                if (hit.GetComponentInParent<Canvas>() == null)
                    continue;
                if (hit.GetComponentInParent<BoardView>() != null)
                    continue;
                return true;
            }

            return false;
        }
    }
}
