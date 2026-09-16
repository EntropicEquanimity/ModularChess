using UnityEngine;

namespace ModularChess.Presentation
{
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BackgroundScroller : MonoBehaviour
    {
        #region Fields
        static readonly int ScrollId = Shader.PropertyToID("_Scroll");
        [SerializeField] SpriteRenderer target;
        [SerializeField] Vector2 scrollSpeed = new Vector2(0.04f, 0.03f);
        MaterialPropertyBlock _block;
        #endregion

        #region Unity
        void Awake()
        {
            Wire();
        }
        void Update()
        {
            if (target == null) return;
            float t = Time.unscaledTime;
            _block.SetVector(ScrollId, new Vector4(
                Mathf.Repeat(t * scrollSpeed.x, 1f),
                Mathf.Repeat(t * scrollSpeed.y, 1f),
                0f,
                0f));
            target.SetPropertyBlock(_block);
        }
        #endregion

        #region Private Methods
        void Wire()
        {
            if (target == null)
            {
                target = GetComponent<SpriteRenderer>();
            }
            _block = new MaterialPropertyBlock();
            if (target != null)
            {
                target.GetPropertyBlock(_block);
            }
        }
        #endregion
    }
}
