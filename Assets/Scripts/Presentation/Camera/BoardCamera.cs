using UnityEngine;

namespace ModularChess.Presentation
{
    [RequireComponent(typeof(Camera))]
    public sealed class BoardCamera : MonoBehaviour
    {
        #region Fields
        [SerializeField] BoardView board;
        [SerializeField] float paddingSquares = 1f;
        [SerializeField] bool frameOnEnable = true;
        [SerializeField] bool followEveryFrame = true;
        [SerializeField] bool applyBackgroundColor = true;
        [SerializeField] Color backgroundColor = new Color(0.16f, 0.2f, 0.18f, 1f);
        const float TraumaDecay = 1.75f;
        const float TraumaMaxOffset = 0.55f;
        const float TraumaMaxRoll = 0.09f;
        Camera _camera;
        float _trauma;
        float _noise;
        Vector3 _framedPosition;
        Quaternion _framedRotation = Quaternion.identity;
        bool _hasFrame;
        static BoardCamera _active;
        public BoardView Board
        {
            get => board;
            set => board = value;
        }
        #endregion

        #region Unity
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
            _active = this;
            if (frameOnEnable)
                FrameBoard();
        }
        void OnDisable()
        {
            if (_active == this)
                _active = null;
        }
        void LateUpdate()
        {
            if (followEveryFrame)
                FrameBoard();
            ApplyTrauma();
        }
        #endregion

        #region Public Methods
        public static void AddTrauma(float amount)
        {
            if (_active == null)
                return;
            _active._trauma = Mathf.Clamp01(_active._trauma + amount);
        }
        public static void ClearTrauma()
        {
            if (_active == null)
                return;
            _active._trauma = 0f;
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
            _framedPosition = new Vector3(bounds.center.x, bounds.center.y, z);
            _framedRotation = Quaternion.identity;
            _hasFrame = true;
            if (_trauma <= 0.0001f)
            {
                transform.position = _framedPosition;
                transform.rotation = _framedRotation;
            }
        }
        #endregion

        #region Private Methods
        void ApplyTrauma()
        {
            Vector3 basePos = _hasFrame ? _framedPosition : transform.position;
            Quaternion baseRot = _hasFrame ? _framedRotation : transform.rotation;
            if (_trauma <= 0.0001f)
            {
                if (_hasFrame)
                {
                    transform.position = basePos;
                    transform.rotation = baseRot;
                }
                return;
            }
            float intensity = CameraShakePrefs.Multiplier;
            float shake = intensity > 0.0001f ? _trauma * _trauma * intensity : 0f;
            _noise += Time.deltaTime * 28f;
            float ox = TraumaMaxOffset * shake * (Mathf.PerlinNoise(_noise, 0.13f) * 2f - 1f);
            float oy = TraumaMaxOffset * shake * (Mathf.PerlinNoise(0.71f, _noise) * 2f - 1f);
            float roll = TraumaMaxRoll * shake * (Mathf.PerlinNoise(_noise, _noise) * 2f - 1f);
            transform.position = basePos + new Vector3(ox, oy, 0f);
            transform.rotation = baseRot * Quaternion.Euler(0f, 0f, roll * Mathf.Rad2Deg);
            _trauma = Mathf.Max(0f, _trauma - TraumaDecay * Time.deltaTime);
        }
        #endregion
    }
}
