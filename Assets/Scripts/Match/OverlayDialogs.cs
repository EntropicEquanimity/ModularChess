using ModularChess.Core;
using ModularChess.Presentation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class OverlayDialogs : MonoBehaviour
    {
        #region Fields
        [SerializeField] GameObject background;
        [SerializeField] GameObject quitConfirm;
        [SerializeField] GameObject debugMenu;
        [SerializeField] GameObject debugMenuPrefab;
        DebugMenuView _debugView;
        bool _woken;
        #endregion

        #region Public Methods
        public bool QuitOpen => quitConfirm != null && quitConfirm.activeSelf;
        public bool DebugOpen => debugMenu != null && debugMenu.activeSelf;
        public bool IsOpen => QuitOpen || DebugOpen;
        public static OverlayDialogs Ensure(Transform parent)
        {
            OverlayDialogs existing = parent != null
                ? parent.GetComponentInChildren<OverlayDialogs>(true)
                : FindAnyObjectByType<OverlayDialogs>(FindObjectsInactive.Include);
            if (existing != null)
            {
                existing.Wake();
                return existing;
            }
            GameObject prefab = RuntimePrefabs.OverlayDialogs;
            if (prefab == null || parent == null)
                return null;
            GameObject go = Instantiate(prefab, parent);
            go.name = "OverlayDialogs";
            OverlayDialogs dialogs = go.GetComponent<OverlayDialogs>();
            if (dialogs == null)
                dialogs = go.AddComponent<OverlayDialogs>();
            dialogs.Wake();
            go.SetActive(false);
            return dialogs;
        }
        public void ShowQuit(UnityAction confirm, UnityAction cancel)
        {
            Wake();
            HideDebugImmediate();
            if (quitConfirm == null)
                return;
            BindButtons(quitConfirm, new[] { confirm, cancel ?? HideQuit });
            Present(quitConfirm);
        }
        public void HideQuit()
        {
            if (quitConfirm != null)
                quitConfirm.SetActive(false);
            RefreshChrome();
        }
        public void ShowDebug(
            UnityAction resetSave,
            UnityAction unlockAll,
            UnityAction win,
            UnityAction lose,
            UnityAction resetTimer,
            UnityAction vsAiNone,
            UnityAction vsAiAllNoRandomizer,
            UnityAction vsAiAll,
            UnityAction<ModeId> vsAiSpecific,
            UnityAction<int> jumpLevel,
            UnityAction revealFog,
            UnityAction meritPlus,
            UnityAction meritMinus,
            UnityAction unlockAllCampaign,
            UnityAction clearAllCampaign,
            UnityAction vsAiRandomModes)
        {
            Wake();
            HideQuit();
            EnsureDebugMenu();
            if (debugMenu == null)  return;
            _debugView = debugMenu.GetComponent<DebugMenuView>();
            if (_debugView == null)  _debugView = debugMenu.AddComponent<DebugMenuView>();
            debugMenu.transform.SetAsLastSibling();
            _debugView.Present(
                resetSave,
                unlockAll,
                win,
                lose,
                resetTimer,
                HideDebugImmediate,
                vsAiNone,
                vsAiAllNoRandomizer,
                vsAiAll,
                vsAiSpecific,
                jumpLevel,
                revealFog,
                meritPlus,
                meritMinus,
                unlockAllCampaign,
                clearAllCampaign,
                vsAiRandomModes);
            Present(debugMenu);
        }
        public void HideDebugImmediate()
        {
            if (debugMenu != null)
                debugMenu.SetActive(false);
            RefreshChrome();
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
        #endregion

        #region Private Methods
        void Wake()
        {
            if (_woken)
                return;
            if (background == null)
            {
                Transform child = transform.Find("BG");
                if (child != null) background = child.gameObject;
            }
            if (quitConfirm == null)
            {
                Transform child = transform.Find("QuitConfirm");
                if (child != null) quitConfirm = child.gameObject;
            }
            if (debugMenu == null)
            {
                Transform child = transform.Find("DebugMenu");
                if (child != null) debugMenu = child.gameObject;
            }
            EnsureDebugMenu();
            if (background != null) background.SetActive(false);
            if (quitConfirm != null) quitConfirm.SetActive(false);
            if (debugMenu != null) debugMenu.SetActive(false);
            _woken = true;
        }
        void EnsureDebugMenu()
        {
            if (debugMenu != null && HasAutoplayControls(debugMenu))
                return;
            if (debugMenu != null)
            {
                Destroy(debugMenu);
                debugMenu = null;
                _debugView = null;
            }
            GameObject prefab = debugMenuPrefab != null ? debugMenuPrefab : RuntimePrefabs.DebugMenu;
            if (prefab == null)
                return;
            debugMenu = Instantiate(prefab, transform);
            debugMenu.name = "DebugMenu";
            debugMenu.SetActive(false);
            _debugView = null;
        }
        static bool HasAutoplayControls(GameObject root)
        {
            if (root == null) return false;
            Transform start = FindNamed(root.transform, "dialog.autoplayStart");
            Transform stop = FindNamed(root.transform, "dialog.autoplayStop");
            return start != null && stop != null;
        }
        static Transform FindNamed(Transform root, string name)
        {
            if (root == null) return null;
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindNamed(root.GetChild(i), name);
                if (found != null) return found;
            }
            return null;
        }
        void Present(GameObject panel)
        {
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            if (background != null) background.SetActive(true);
            panel.SetActive(true);
        }
        void RefreshChrome()
        {
            if (IsOpen) return;
            if (background != null) background.SetActive(false);
            gameObject.SetActive(false);
        }
        static void BindButtons(GameObject root, UnityAction[] actions)
        {
            Button[] buttons = root.GetComponentsInChildren<Button>(true);
            int count = Mathf.Min(buttons.Length, actions.Length);
            for (int i = 0; i < count; i++)
            {
                buttons[i].onClick.RemoveAllListeners();
                if (actions[i] != null) GameAudio.Bind(buttons[i], actions[i]);
            }
        }
        #endregion
    }
}
