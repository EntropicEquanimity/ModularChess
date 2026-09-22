#if UNITY_EDITOR
using ModularChess.Match;
using ModularChess.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.EditorTools
{
    public static class RoguelikePrefabBuilder
    {
        const string HudPath = "Assets/Prefabs/Overlays/RoguelikeHud.prefab";
        const string CardPath = "Assets/Prefabs/Overlays/BoonCard.prefab";
        const string OfferPath = "Assets/Prefabs/Overlays/BoonOffer.prefab";

        [MenuItem("Modular Chess/Build Roguelike Prefabs")]
        public static void Build()
        {
            EnsureFolders();
            GameObject card = BuildBoonCard();
            PrefabUtility.SaveAsPrefabAsset(card, CardPath);
            Object.DestroyImmediate(card);
            GameObject offer = BuildBoonOffer();
            PrefabUtility.SaveAsPrefabAsset(offer, OfferPath);
            Object.DestroyImmediate(offer);
            GameObject hud = BuildHud();
            PrefabUtility.SaveAsPrefabAsset(hud, HudPath);
            Object.DestroyImmediate(hud);
            EnsurePlayButton();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Roguelike prefabs built.");
        }

        static void EnsurePlayButton()
        {
            const string playPath = "Assets/Prefabs/Overlays/Play.prefab";
            GameObject playRoot = PrefabUtility.LoadPrefabContents(playPath);
            if (playRoot == null)
                return;
            Transform existing = FindDeep(playRoot.transform, "RoguelikeButton");
            if (existing == null)
            {
                Transform group = FindDeep(playRoot.transform, "ButtonGroup");
                if (group != null)
                {
                    GameObject buttonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UI/Components/TextButton.prefab");
                    GameObject button;
                    if (buttonPrefab != null)
                        button = (GameObject)PrefabUtility.InstantiatePrefab(buttonPrefab, group);
                    else
                    {
                        button = new GameObject("RoguelikeButton", typeof(RectTransform), typeof(Image), typeof(Button));
                        button.transform.SetParent(group, false);
                    }
                    button.name = "RoguelikeButton";
                    button.transform.SetSiblingIndex(Mathf.Max(0, group.childCount - 2));
                    TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
                    if (label != null)
                        label.text = "Roguelike";
                }
            }
            PlayOverlay overlay = playRoot.GetComponent<PlayOverlay>();
            if (overlay != null)
            {
                SerializedObject so = new SerializedObject(overlay);
                Transform btn = FindDeep(playRoot.transform, "RoguelikeButton");
                if (btn != null)
                {
                    so.FindProperty("roguelikeButton").objectReferenceValue = btn.GetComponent<Button>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }
            PrefabUtility.SaveAsPrefabAsset(playRoot, playPath);
            PrefabUtility.UnloadPrefabContents(playRoot);
        }

        static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindDeep(root.GetChild(i), name);
                if (found != null)
                    return found;
            }
            return null;
        }

        public static void BuildFromBatch()
        {
            Build();
            EditorApplication.Exit(0);
        }

        static void EnsureFolders()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs/Overlays"))
                AssetDatabase.CreateFolder("Assets/Prefabs", "Overlays");
        }

        static GameObject BuildHud()
        {
            GameObject root = new GameObject("RoguelikeHud", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(RoguelikeHud));
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 210;
            CanvasScaler scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 540);
            RectTransform rt = root.GetComponent<RectTransform>();
            Stretch(rt);
            AddLabel(root.transform, "StageLabel", "Stage 1", new Vector2(12, -12), TextAnchor.UpperLeft);
            AddLabel(root.transform, "GoldLabel", "Gold 0", new Vector2(12, -40), TextAnchor.UpperLeft);
            AddLabel(root.transform, "ArmyLabel", "Army 0/3", new Vector2(12, -68), TextAnchor.UpperLeft);
            AddLabel(root.transform, "EnemyBoonLabel", "", new Vector2(12, -96), TextAnchor.UpperLeft);
            AddLabel(root.transform, "StatusLabel", "", new Vector2(0, 40), TextAnchor.LowerCenter);
            AddButton(root.transform, "LeaveButton", "Leave", new Vector2(-12, -12), TextAnchor.UpperRight);
            GameObject rearrange = new GameObject("RearrangePanel", typeof(RectTransform));
            rearrange.transform.SetParent(root.transform, false);
            Stretch(rearrange.GetComponent<RectTransform>());
            rearrange.SetActive(false);
            AddButton(rearrange.transform, "NextStageButton", "Next Stage", new Vector2(0, 80), TextAnchor.LowerCenter);
            GameObject results = new GameObject("ResultsPanel", typeof(RectTransform), typeof(Image));
            results.transform.SetParent(root.transform, false);
            Stretch(results.GetComponent<RectTransform>());
            results.GetComponent<Image>().color = new Color(0, 0, 0, 0.65f);
            results.SetActive(false);
            AddLabel(results.transform, "ResultsLabel", "Result", new Vector2(0, 40), TextAnchor.MiddleCenter);
            AddButton(results.transform, "RematchButton", "Rematch", new Vector2(-80, -40), TextAnchor.MiddleCenter);
            Button leaveResults = AddButton(results.transform, "ResultsLeaveButton", "Leave", new Vector2(80, -40), TextAnchor.MiddleCenter);
            leaveResults.name = "LeaveButton";
            GameObject offerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(OfferPath);
            if (offerPrefab != null)
            {
                GameObject offer = (GameObject)PrefabUtility.InstantiatePrefab(offerPrefab);
                offer.transform.SetParent(root.transform, false);
                offer.name = "BoonOffer";
            }
            return root;
        }

        static GameObject BuildBoonOffer()
        {
            GameObject root = new GameObject("BoonOffer", typeof(RectTransform), typeof(Image), typeof(BoonOfferView));
            Stretch(root.GetComponent<RectTransform>());
            root.GetComponent<Image>().color = new Color(0, 0, 0, 0.55f);
            GameObject cardRoot = new GameObject("CardRoot", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            cardRoot.transform.SetParent(root.transform, false);
            RectTransform crt = cardRoot.GetComponent<RectTransform>();
            crt.anchorMin = new Vector2(0.5f, 0.5f);
            crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(700, 220);
            HorizontalLayoutGroup layout = cardRoot.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 16;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            BoonOfferView view = root.GetComponent<BoonOfferView>();
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("cardRoot").objectReferenceValue = cardRoot.transform;
            so.FindProperty("root").objectReferenceValue = root;
            GameObject cardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CardPath);
            if (cardPrefab != null)
                so.FindProperty("cardPrefab").objectReferenceValue = cardPrefab.GetComponent<BoonCardView>();
            so.ApplyModifiedPropertiesWithoutUndo();
            root.SetActive(false);
            return root;
        }

        static GameObject BuildBoonCard()
        {
            GameObject root = new GameObject("BoonCard", typeof(RectTransform), typeof(Image), typeof(Button), typeof(BoonCardView));
            RectTransform rt = root.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(200, 200);
            root.GetComponent<Image>().color = new Color(0.85f, 0.85f, 0.9f, 1f);
            AddLabel(root.transform, "Rarity", "Grey", new Vector2(0, -16), TextAnchor.UpperCenter);
            AddLabel(root.transform, "Title", "Reinforcements", new Vector2(0, 20), TextAnchor.MiddleCenter);
            AddLabel(root.transform, "Description", "Gain 1 pawn next stage.", new Vector2(0, -40), TextAnchor.MiddleCenter);
            return root;
        }

        static TMP_Text AddLabel(Transform parent, string name, string text, Vector2 anchored, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            SetAnchor(rt, anchor);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(280, 36);
            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 18;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            return tmp;
        }

        static Button AddButton(Transform parent, string name, string label, Vector2 anchored, TextAnchor anchor)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            RectTransform rt = go.GetComponent<RectTransform>();
            SetAnchor(rt, anchor);
            rt.anchoredPosition = anchored;
            rt.sizeDelta = new Vector2(140, 40);
            go.GetComponent<Image>().color = new Color(0.2f, 0.2f, 0.25f, 1f);
            AddLabel(go.transform, "Text", label, Vector2.zero, TextAnchor.MiddleCenter);
            return go.GetComponent<Button>();
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        static void SetAnchor(RectTransform rt, TextAnchor anchor)
        {
            switch (anchor)
            {
                case TextAnchor.UpperLeft:
                    rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                    rt.pivot = new Vector2(0, 1);
                    break;
                case TextAnchor.UpperRight:
                    rt.anchorMin = rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    break;
                case TextAnchor.LowerCenter:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0);
                    rt.pivot = new Vector2(0.5f, 0);
                    break;
                case TextAnchor.MiddleCenter:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    break;
                default:
                    rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                    break;
            }
        }
    }
}
#endif
