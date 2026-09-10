using ModularChess.Presentation;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace ModularChess.Match
{
    [DefaultExecutionOrder(-100)]
    public sealed class MatchBootstrap : MonoBehaviour
    {
        [SerializeField] private Camera matchCamera;
        [SerializeField] private BoardView boardView;
        [SerializeField] private PromotionPicker promotionPicker;
        [SerializeField] private MatchHud matchHud;
        [SerializeField] private MatchController matchController;

        private void Awake()
        {
            EnsureEventSystem(transform);
            Camera cam = EnsureCamera();
            BoardView board = EnsureBoardView();
            MatchHud hud = EnsureHud();
            PromotionPicker picker = EnsurePromotionPicker();
            EnsureBoardPointerInput(board, cam, picker);
            EnsureBoardCamera(cam, board);
            MatchController controller = EnsureMatchController();
            controller.Configure(board, picker, hud);
        }

        private Camera EnsureCamera()
        {
            if (matchCamera != null)
            {
                ApplyOrthographic(matchCamera);
                return matchCamera;
            }

            Camera existing = Camera.main;
            if (existing == null)
            {
                existing = FindAnyObjectByType<Camera>();
            }

            if (existing != null)
            {
                matchCamera = existing;
                ApplyOrthographic(matchCamera);
                return matchCamera;
            }

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            matchCamera = cameraObject.AddComponent<Camera>();
            cameraObject.AddComponent<AudioListener>();
            ApplyOrthographic(matchCamera);
            return matchCamera;
        }

        private static void ApplyOrthographic(Camera cam)
        {
            cam.orthographic = true;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 1000f;
            cam.orthographicSize = 5.5f;
            Vector3 position = cam.transform.position;
            float z = Mathf.Abs(position.z) < 0.01f ? -10f : position.z;
            cam.transform.position = new Vector3(4f, 4f, z);
        }

        private static void EnsureBoardCamera(Camera cam, BoardView board)
        {
            BoardCamera boardCamera = cam.GetComponent<BoardCamera>();
            if (boardCamera == null)
            {
                boardCamera = cam.gameObject.AddComponent<BoardCamera>();
            }

            boardCamera.Board = board;
            boardCamera.FrameBoard();
        }

        private static void EnsureBoardPointerInput(BoardView board, Camera cam, PromotionPicker picker)
        {
            BoardPointerInput pointer = board.GetComponent<BoardPointerInput>();
            if (pointer == null)
            {
                pointer = board.gameObject.AddComponent<BoardPointerInput>();
            }

            pointer.PickCamera = cam;
            pointer.PromotionPicker = picker;
        }

        private BoardView EnsureBoardView()
        {
            if (boardView != null)
            {
                return boardView;
            }

            boardView = FindAnyObjectByType<BoardView>();
            if (boardView != null)
            {
                return boardView;
            }

            GameObject boardObject = new GameObject("Board");
            boardObject.transform.SetParent(transform, false);
            boardView = boardObject.AddComponent<BoardView>();
            return boardView;
        }

        private MatchHud EnsureHud()
        {
            if (matchHud != null)
            {
                return matchHud;
            }

            matchHud = FindAnyObjectByType<MatchHud>();
            if (matchHud != null)
            {
                return matchHud;
            }

            GameObject canvasObject = new GameObject("MatchCanvas");
            canvasObject.transform.SetParent(transform, false);

            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();

            matchHud = canvasObject.AddComponent<MatchHud>();
            return matchHud;
        }

        private PromotionPicker EnsurePromotionPicker()
        {
            if (promotionPicker != null)
            {
                return promotionPicker;
            }

            promotionPicker = FindAnyObjectByType<PromotionPicker>();
            if (promotionPicker != null)
            {
                return promotionPicker;
            }

            GameObject pickerObject = new GameObject("PromotionPicker");
            pickerObject.transform.SetParent(transform, false);
            promotionPicker = pickerObject.AddComponent<PromotionPicker>();
            return promotionPicker;
        }

        private MatchController EnsureMatchController()
        {
            if (matchController != null)
            {
                return matchController;
            }

            matchController = GetComponent<MatchController>();
            if (matchController == null)
            {
                matchController = gameObject.AddComponent<MatchController>();
            }

            return matchController;
        }

        private static void EnsureEventSystem(Transform parent)
        {
            EventSystem eventSystem = FindAnyObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                GameObject eventObject = new GameObject("EventSystem");
                eventObject.transform.SetParent(parent, false);
                eventSystem = eventObject.AddComponent<EventSystem>();
            }

            StandaloneInputModule standalone = eventSystem.GetComponent<StandaloneInputModule>();
            if (standalone != null)
            {
                Destroy(standalone);
            }

            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
