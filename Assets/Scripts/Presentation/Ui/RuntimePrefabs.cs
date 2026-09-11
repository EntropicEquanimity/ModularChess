using UnityEngine;

namespace ModularChess.Presentation
{
    public static class RuntimePrefabs
    {
        const string ButtonPath = "Assets/Prefabs/UI/TextButton.prefab";
        const string CanvasPath = "Assets/Prefabs/UI/Canvas.prefab";
        const string TogglePath = "Assets/Prefabs/UI/Toggle.prefab";
        const string DropdownPath = "Assets/Prefabs/UI/Dropdown.prefab";
        const string InputPath = "Assets/Prefabs/UI/InputField.prefab";

        public static GameObject Canvas => Load(CanvasPath, "UI/Canvas");
        public static GameObject TextButton => Load(ButtonPath, "UI/TextButton");
        public static GameObject Toggle => Load(TogglePath, "UI/Toggle");
        public static GameObject Dropdown => Load(DropdownPath, "UI/Dropdown");
        public static GameObject InputField => Load(InputPath, "UI/InputField");

        static GameObject Load(string assetPath, string resourcesName)
        {
#if UNITY_EDITOR
            GameObject editor = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (editor != null)
            {
                return editor;
            }
#endif
            return Resources.Load<GameObject>(resourcesName);
        }
    }
}
