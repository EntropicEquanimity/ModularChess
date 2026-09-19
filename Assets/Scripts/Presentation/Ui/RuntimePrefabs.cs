using UnityEngine;

namespace ModularChess.Presentation
{
    public static class RuntimePrefabs
    {
        const string ButtonPath = "Assets/Prefabs/UI/Components/TextButton.prefab";
        const string CanvasPath = "Assets/Prefabs/UI/Components/Canvas.prefab";
        const string TogglePath = "Assets/Prefabs/UI/Components/Toggle.prefab";
        const string DropdownPath = "Assets/Prefabs/UI/Components/Dropdown.prefab";
        const string InputPath = "Assets/Prefabs/UI/Components/InputField.prefab";
        const string DescriptionPath = "Assets/Prefabs/UI/DescriptionBox.prefab";
        const string ImageButtonPath = "Assets/Prefabs/UI/Components/ImageButton.prefab";
        const string PanelPath = "Assets/Prefabs/UI/Components/Panel.prefab";
        const string SelectionRowPath = "Assets/Prefabs/UI/Components/SelectionRow.prefab";
        const string ChessPiecePath = "Assets/Prefabs/Game/ChessPiece.prefab";
        const string ChessboardTilePath = "Assets/Prefabs/Game/ChessboardTile.prefab";
        const string PieceDetailsPath = "Assets/Prefabs/UI/UnitDetails.prefab";
        const string EffectDescriptionPath = "Assets/Prefabs/UI/EffectDescription.prefab";
        const string DraftRowPath = "Assets/Prefabs/Popup/DraftRow.prefab";
        const string ModeSettingsPopupPath = "Assets/Prefabs/Overlays/ModeSettingsPopup.prefab";
        const string UnlocksDetailPopupPath = "Assets/Prefabs/Popup/UnlocksDetailPopup.prefab";
        const string SettingsControlPath = "Assets/Prefabs/UI/SettingsControl.prefab";
        const string OptionSliderPath = "Assets/Prefabs/UI/Components/OptionSlider.prefab";
        const string PromotionPopupPath = "Assets/Prefabs/Overlays/PromotionPopup.prefab";
        const string MatchHudPath = "Assets/Prefabs/Overlays/MatchHud.prefab";
        const string ScrollViewPath = "Assets/Prefabs/UI/Components/Scroll View.prefab";
        const string AccountCreationPath = "Assets/Prefabs/Overlays/AccountCreation.prefab";
        const string FeedbackSurveyPath = "Assets/Prefabs/Overlays/FeedbackSurvey.prefab";

        public static GameObject Canvas => Load(CanvasPath, "UI/Canvas");
        public static GameObject TextButton => Load(ButtonPath, "UI/TextButton");
        public static GameObject Toggle => Load(TogglePath, "UI/Toggle");
        public static GameObject Dropdown => Load(DropdownPath, "UI/Dropdown");
        public static GameObject InputField => Load(InputPath, "UI/InputField");
        public static GameObject DescriptionBox => Load(DescriptionPath, "UI/DescriptionBox");
        public static GameObject ImageButton => Load(ImageButtonPath, "UI/ImageButton");
        public static GameObject Panel => Load(PanelPath, "UI/Panel");
        public static GameObject SelectionRow => Load(SelectionRowPath, "UI/SelectionRow");
        public static GameObject ChessPiece => Load(ChessPiecePath, "Game/ChessPiece");
        public static GameObject ChessboardTile => Load(ChessboardTilePath, "Game/ChessboardTile");
        public static GameObject PieceDetails => Load(PieceDetailsPath, "UI/UnitDetails");
        public static GameObject EffectDescription => Load(EffectDescriptionPath, "UI/EffectDescription");
        public static GameObject DraftRow => Load(DraftRowPath, "Popup/DraftRow");
        public static GameObject ModeSettingsPopup => Load(ModeSettingsPopupPath, "Overlays/ModeSettingsPopup");
        public static GameObject UnlocksDetailPopup => Load(UnlocksDetailPopupPath, "Popup/UnlocksDetailPopup");
        public static GameObject SettingsControl => Load(SettingsControlPath, "UI/SettingsControl");
        public static GameObject OptionSlider => Load(OptionSliderPath, "UI/OptionSlider");
        public static GameObject PromotionPopup => Load(PromotionPopupPath, "Overlays/PromotionPopup");
        public static GameObject MatchHud => Load(MatchHudPath, "Overlays/MatchHud");
        public static GameObject ScrollView => Load(ScrollViewPath, "UI/Scroll View");
        public static GameObject AccountCreation => Load(AccountCreationPath, "Overlays/AccountCreation");
        public static GameObject FeedbackSurvey => Load(FeedbackSurveyPath, "Overlays/FeedbackSurvey");

        static GameObject Load(string assetPath, string resourcesName)
        {
#if UNITY_EDITOR
            GameObject editor = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (editor != null)
                return editor;
            if (assetPath.Contains("/Components/"))
            {
                string legacy = assetPath.Replace("/Components/", "/");
                editor = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(legacy);
                if (editor != null)
                    return editor;
            }
#endif
            return Resources.Load<GameObject>(resourcesName);
        }
    }
}
