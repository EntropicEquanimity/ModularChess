using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class OverlayDialogs : MonoBehaviour
    {
        GameObject _quit;
        GameObject _debug;

        public bool QuitOpen => _quit != null && _quit.activeSelf;
        public bool DebugOpen => _debug != null && _debug.activeSelf;

        public static OverlayDialogs Ensure(Transform parent)
        {
            OverlayDialogs existing = parent.GetComponentInChildren<OverlayDialogs>(true);
            if (existing != null)
                return existing;
            var go = new GameObject("OverlayDialogs", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(OverlayDialogs));
            go.transform.SetParent(parent, false);
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300;
            canvas.pixelPerfect = true;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960f, 540f);
            scaler.referencePixelsPerUnit = 16f;
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.SetActive(true);
            return go.GetComponent<OverlayDialogs>();
        }

        public void ShowQuit(UnityAction confirm, UnityAction cancel)
        {
            HideDebugImmediate();
            if (_quit == null)
                _quit = BuildPanel("QuitConfirm", "Quit the Game?", new[] { "Yes", "Close" });
            BindButtons(_quit, new[] { confirm, cancel });
            _quit.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void HideQuit()
        {
            if (_quit != null)
                _quit.SetActive(false);
        }

        public void ShowDebug(
            UnityAction resetSave,
            UnityAction unlockAll,
            UnityAction win,
            UnityAction lose,
            UnityAction resetTimer)
        {
            HideQuit();
            if (_debug == null)
            {
                _debug = BuildPanel(
                    "DebugMenu",
                    "Debug",
                    new[] { "Reset Save Data", "Unlock all Modes", "Win", "Lose", "Reset Timer", "Close" });
            }

            BindButtons(_debug, new[] { resetSave, unlockAll, win, lose, resetTimer, HideDebugImmediate });
            _debug.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void HideDebugImmediate()
        {
            if (_debug != null)
                _debug.SetActive(false);
        }

        public bool CloseTop()
        {
            if (DebugOpen)
            {
                HideDebugImmediate();
                return true;
            }

            if (QuitOpen)
            {
                HideQuit();
                return true;
            }

            return false;
        }

        GameObject BuildPanel(string name, string title, string[] buttons)
        {
            RectTransform panel = UiFactory.Panel(transform, new Vector2(420f, 40f + buttons.Length * 40f + 48f));
            panel.name = name;
            panel.anchorMin = new Vector2(0.5f, 0.5f);
            panel.anchorMax = new Vector2(0.5f, 0.5f);
            panel.anchoredPosition = Vector2.zero;
            var layout = panel.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            TMP_Text heading = UiFactory.Label(panel, title, 32, TextAlignmentOptions.Center);
            heading.color = Color.black;
            var headingElement = heading.gameObject.AddComponent<LayoutElement>();
            headingElement.minHeight = 40f;
            headingElement.preferredHeight = 40f;
            for (int i = 0; i < buttons.Length; i++)
            {
                Button button = UiFactory.Button(panel, buttons[i], null, new Vector2(200f, 32f));
                button.name = buttons[i];
                var element = button.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 200f;
                element.preferredWidth = 200f;
                element.minHeight = 32f;
                element.preferredHeight = 32f;
            }

            panel.gameObject.SetActive(false);
            return panel.gameObject;
        }

        static void BindButtons(GameObject root, UnityAction[] actions)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            int count = Mathf.Min(buttons.Length, actions.Length);
            for (int i = 0; i < count; i++)
            {
                buttons[i].onClick.RemoveAllListeners();
                if (actions[i] != null)
                    buttons[i].onClick.AddListener(actions[i]);
            }
        }
    }
}
