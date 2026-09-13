using UnityEngine;

namespace ModularChess.Presentation
{
    internal static class RuntimeSprites
    {
        static Sprite _pixel;
        static Sprite _circle;
        static Sprite _lock;

        public static Sprite LockIcon
        {
            get
            {
                if (_lock != null)
                    return _lock;
#if UNITY_EDITOR
                _lock = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Lock.png");
#endif
                if (_lock == null)
                    _lock = Resources.Load<Sprite>("Lock");
                return _lock;
            }
        }

        public static Sprite Pixel
        {
            get
            {
                if (_pixel == null)
                    _pixel = CreateSolid(4, 4f);
                return _pixel;
            }
        }

        public static Sprite Circle
        {
            get
            {
                if (_circle == null)
                    _circle = CreateCircle(64, 64f);
                return _circle;
            }
        }

        static Sprite CreateSolid(int size, float pixelsPerUnit)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite CreateCircle(int size, float pixelsPerUnit)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };

            var pixels = new Color[size * size];
            float center = (size - 1) * 0.5f;
            float radius = size * 0.5f - 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center;
                    float dy = y - center;
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply(false, false);

            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, size, size),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
