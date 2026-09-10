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

        readonly List<RaycastResult> _uiHits = new List<RaycastResult>(8);
        BoardView _board;

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
            if (pointer == null || !pointer.press.wasPressedThisFrame)
                return;

            Vector2 screen = pointer.position.ReadValue();
            if (IsPromotionOpen() || IsBlockedByUi(screen))
                return;

            Camera camera = pickCamera != null ? pickCamera : Camera.main;
            if (camera == null || !camera.pixelRect.Contains(screen))
                return;

            float depth = Mathf.Abs(camera.transform.position.z - _board.transform.position.z);
            Vector3 world = camera.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            if (!_board.TryPickSquare(world, out Square square))
                return;

            _board.NotifySquareClicked(square);
        }

        bool IsPromotionOpen()
        {
            if (promotionPicker == null)
                promotionPicker = FindAnyObjectByType<PromotionPicker>();
            return promotionPicker != null && promotionPicker.IsOpen;
        }

        bool IsBlockedByUi(Vector2 screen)
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;

            var eventData = new PointerEventData(eventSystem)
            {
                position = screen
            };
            _uiHits.Clear();
            eventSystem.RaycastAll(eventData, _uiHits);
            return _uiHits.Count > 0;
        }
    }
}
