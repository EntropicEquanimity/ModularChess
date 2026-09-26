using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    [CreateAssetMenu(menuName = "Modular Chess/Terrain Sprites", fileName = "TerrainSprites")]
    public sealed class TerrainSpriteCatalog : ScriptableObject
    {
        #region Fields
        public const string AssetPath = "Assets/Data/TerrainSprites.asset";
        static TerrainSpriteCatalog _cached;
        [SerializeField] Sprite[] swamp;
        [SerializeField] Sprite[] forest;
        [SerializeField] Sprite[] mountain;
        #endregion

        #region Public Methods
        public static TerrainSpriteCatalog Load()
        {
            if (_cached != null)
                return _cached;
#if UNITY_EDITOR
            _cached = UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainSpriteCatalog>(AssetPath);
#endif
            return _cached;
        }
        public Sprite RandomSprite(TerrainKind kind)
        {
            Sprite[] list = ListFor(kind);
            if (list == null || list.Length == 0)
                return null;
            int start = Random.Range(0, list.Length);
            for (int i = 0; i < list.Length; i++)
            {
                Sprite sprite = list[(start + i) % list.Length];
                if (sprite != null)
                    return sprite;
            }
            return null;
        }
        #endregion

        #region Private Methods
        Sprite[] ListFor(TerrainKind kind)
        {
            switch (kind)
            {
                case TerrainKind.Swamp: return swamp;
                case TerrainKind.Forest: return forest;
                case TerrainKind.Mountain: return mountain;
                default: return null;
            }
        }
        #endregion
    }
}
