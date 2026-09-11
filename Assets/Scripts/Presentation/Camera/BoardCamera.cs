using UnityEngine;

namespace ModularChess.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCamera : MonoBehaviour
    {
        [SerializeField] BoardView board;
        [SerializeField] float paddingSquares = 1f;
        [SerializeField] bool frameOnEnable = true;
        [SerializeField] bool followEveryFrame = true;
        [SerializeField] bool applyBackgroundColor = true;
        [SerializeField] Color backgroundColor = new Color(0.16f, 0.2f, 0.18f, 1f);

        Camera _camera;

        public BoardView Board
        {
            get => board;
            set => board = value;
        }

        void Reset()
        {
            paddingSquares = 1f;
            frameOnEnable = true;
            followEveryFrame = true;
            applyBackgroundColor = true;
            backgroundColor = new Color(0.16f, 0.2f, 0.18f, 1f);
        }

        void Awake()
        {
            _camera = GetComponent<Camera>();
            _camera.orthographic = true;
            if (applyBackgroundColor)
            {
                _camera.clearFlags = CameraClearFlags.SolidColor;
                _camera.backgroundColor = backgroundColor;
            }
        }

        void OnEnable()
        {
            if (frameOnEnable)
                FrameBoard();
        }

        void LateUpdate()
        {
            if (followEveryFrame)
                FrameBoard();
        }

        public void FrameBoard()
        {
            if (_camera == null)
                _camera = GetComponent<Camera>();

            BoardView target = board != null ? board : FindAnyObjectByType<BoardView>();
            if (target == null || !target.gameObject.activeInHierarchy || _camera == null)
                return;

            Bounds bounds = target.GetWorldBounds();
            float pad = paddingSquares * Mathf.Max(target.SquareSize, 0.01f);
            float width = bounds.size.x + pad * 2f;
            float height = bounds.size.y + pad * 2f;
            float aspect = Mathf.Max(_camera.aspect, 0.0001f);

            _camera.orthographic = true;
            _camera.orthographicSize = Mathf.Max(height * 0.5f, width * 0.5f / aspect);

            float z = transform.position.z;
            if (Mathf.Abs(z) < 0.01f)
                z = -10f;

            transform.position = new Vector3(bounds.center.x, bounds.center.y, z);
            transform.rotation = Quaternion.identity;
        }
    }
}
