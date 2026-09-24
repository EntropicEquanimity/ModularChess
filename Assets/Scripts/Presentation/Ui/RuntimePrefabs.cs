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
        const string OverlayDialogsPath = "Assets/Prefabs/Overlays/OverlayDialogs.prefab";
        const string DebugMenuPath = "Assets/Prefabs/Popup/DebugMenu.prefab";
        const string HistoryPath = "Assets/Prefabs/Overlays/History.prefab";
        const string CampaignPath = "Assets/Prefabs/Overlays/Campaign.prefab";

        public static GameObject Canvas => Load(CanvasPath);
        public static GameObject TextButton => Load(ButtonPath);
        public static GameObject Toggle => Load(TogglePath);
        public static GameObject Dropdown => Load(DropdownPath);
        public static GameObject InputField => Load(InputPath);
        public static GameObject DescriptionBox => Load(DescriptionPath);
        public static GameObject ImageButton => Load(ImageButtonPath);
        public static GameObject Panel => Load(PanelPath);
        public static GameObject SelectionRow => Load(SelectionRowPath);
        public static GameObject ChessPiece => Load(ChessPiecePath);
        public static GameObject ChessboardTile => Load(ChessboardTilePath);
        public static GameObject PieceDetails => Load(PieceDetailsPath);
        public static GameObject EffectDescription => Load(EffectDescriptionPath);
        public static GameObject DraftRow => Load(DraftRowPath);
        public static GameObject ModeSettingsPopup => Load(ModeSettingsPopupPath);
        public static GameObject UnlocksDetailPopup => Load(UnlocksDetailPopupPath);
        public static GameObject SettingsControl => Load(SettingsControlPath);
        public static GameObject OptionSlider => Load(OptionSliderPath);
        public static GameObject PromotionPopup => Load(PromotionPopupPath);
        public static GameObject MatchHud => Load(MatchHudPath);
        public static GameObject ScrollView => Load(ScrollViewPath);
        public static GameObject AccountCreation => Load(AccountCreationPath);
        public static GameObject FeedbackSurvey => Load(FeedbackSurveyPath);
        public static GameObject OverlayDialogs => Load(OverlayDialogsPath);
        public static GameObject DebugMenu => Load(DebugMenuPath);
        public static GameObject History => Load(HistoryPath);
        public static GameObject Campaign => Load(CampaignPath);

        static GameObject Load(string assetPath)
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
            return null;
        }
    }
}
