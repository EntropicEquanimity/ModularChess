using System.Collections;
using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Match
{
    [DefaultExecutionOrder(-50)]
    public sealed class AppRoot : MonoBehaviour, IInitializable
    {
        const string FeedbackUrl = "https://forms.gle/";
        const string ShowNotationKey = "ShowNotation";

        [SerializeField] GameObject mainMenu;
        [SerializeField] GameObject playOverlay;
        [SerializeField] GameObject matchSettingsOverlay;
        [SerializeField] GameObject lobbyOverlay;
        [SerializeField] GameObject joinOverlay;
        [SerializeField] GameObject unlocksOverlay;
        [SerializeField] GameObject optionsOverlay;
        [SerializeField] GameObject creditsOverlay;
        [SerializeField] MatchController match;
        [SerializeField] BoardView board;
        [SerializeField] MatchHud hud;

        MatchController _match;
        BoardView _board;
        MatchHud _hud;
        bool _menuBound;
        readonly List<ModeId> _selectedModes = new List<ModeId>();
        readonly List<GameObject> _spawnedModeToggles = new List<GameObject>();
        readonly HostModeSettings _modeSettings = new HostModeSettings();
        Activity _activity;
        HostColor _hostColor = HostColor.White;
        AiStrength _aiStrength = AiStrength.Medium;
        int _timePreset;
        LocalLobby _lobby;
        static LocalLobby _openLobby;
        bool _prepConfirmArmed;
        ModeSettingsPopup _modeSettingsPopup;

        public void Initialize()
        {
            ResolveReferences();
            BindMainMenu();
            BindOverlays();
        }

        void Start()
        {
            ResolveReferences();
            if (_match != null)
                _match.LeftMatch += ShowMainMenu;
            BindMainMenu();
            BindOverlays();
            ShowMainMenu();
            HideBoard();
        }

        void ResolveReferences()
        {
            _match = match != null ? match : FindAnyObjectByType<MatchController>();
            _board = board != null ? board : FindAnyObjectByType<BoardView>();
            _hud = hud != null ? hud : FindAnyObjectByType<MatchHud>();
            if (mainMenu == null)
            {
                Transform found = transform.Find("MainMenu");
                if (found != null)
                    mainMenu = found.gameObject;
            }
        }

        GameObject[] OverlayList()
        {
            return new[]
            {
                playOverlay,
                matchSettingsOverlay,
                lobbyOverlay,
                joinOverlay,
                unlocksOverlay,
                optionsOverlay,
                creditsOverlay
            };
        }

        void BindMainMenu()
        {
            if (_menuBound || mainMenu == null)
                return;

            BindButton(mainMenu, "PlayButton", ShowPlay);
            BindButton(mainMenu, "UnlocksButton", ShowUnlocks);
            BindButton(mainMenu, "OptionsButton", ShowOptions);
            BindButton(mainMenu, "CreditsButton", ShowCredits);
            BindButton(mainMenu, "FeedbackButton", OpenFeedback);
            BindButton(mainMenu, "ExitButton", ExitGame);
            Button customize = FindButton(mainMenu, "CustomizeButton");
            if (customize != null)
                customize.interactable = false;
            _menuBound = true;
        }

        void BindOverlays()
        {
            BindButton(playOverlay, "VersusAiButton", () => OpenPrep(Activity.VersusAi));
            BindButton(playOverlay, "VersusFriendButton", () => OpenPrep(Activity.VersusFriend));
            BindButton(playOverlay, "JoinButton", ShowJoin);
            BindButton(playOverlay, "BackButton", ShowMainMenu);

            BindButton(matchSettingsOverlay, "ConfirmButton", ConfirmPrep);
            BindButton(matchSettingsOverlay, "BackButton", ShowPlay);

            BindButton(lobbyOverlay, "SitButton", SitAsFriend);
            BindButton(lobbyOverlay, "StartButton", StartLobbyMatch);
            BindButton(lobbyOverlay, "LeaveButton", LeaveLobby);

            BindButton(joinOverlay, "EnterButton", EnterJoinCode);
            BindButton(joinOverlay, "BackButton", ShowPlay);

            BindButton(unlocksOverlay, "BackButton", ShowMainMenu);
            BindButton(optionsOverlay, "BackButton", ShowMainMenu);
            BindButton(creditsOverlay, "BackButton", ShowMainMenu);

            Toggle notation = FindToggle(optionsOverlay, "NotationToggle");
            if (notation != null)
            {
                notation.onValueChanged.RemoveAllListeners();
                notation.isOn = PlayerPrefs.GetInt(ShowNotationKey, 1) == 1;
                notation.onValueChanged.AddListener(on =>
                {
                    PlayerPrefs.SetInt(ShowNotationKey, on ? 1 : 0);
                    PlayerPrefs.Save();
                });
            }
        }

        void ShowOverlay(GameObject overlay)
        {
            if (overlay != matchSettingsOverlay)
                _modeSettingsPopup?.HideImmediate();

            HideBoard();
            if (mainMenu != null)
                mainMenu.SetActive(false);
            GameObject[] overlays = OverlayList();
            for (int i = 0; i < overlays.Length; i++)
            {
                if (overlays[i] != null)
                    overlays[i].SetActive(overlays[i] == overlay);
            }
        }

        void HideOverlays()
        {
            _modeSettingsPopup?.HideImmediate();
            GameObject[] overlays = OverlayList();
            for (int i = 0; i < overlays.Length; i++)
            {
                if (overlays[i] != null)
                    overlays[i].SetActive(false);
            }
        }

        void HideBoard()
        {
            if (_board != null)
                _board.gameObject.SetActive(false);
            if (_hud != null)
                _hud.gameObject.SetActive(false);
        }

        void ShowBoard()
        {
            if (mainMenu != null)
                mainMenu.SetActive(false);
            HideOverlays();
            if (_board != null)
                _board.gameObject.SetActive(true);
            if (_hud != null)
                _hud.gameObject.SetActive(true);
        }

        public void ShowMainMenu()
        {
            HideBoard();
            HideOverlays();
            if (mainMenu != null)
                mainMenu.SetActive(true);
        }

        void ShowPlay()
        {
            ShowOverlay(playOverlay);
        }

        void ShowUnlocks()
        {
            ShowOverlay(unlocksOverlay);
            UnlocksView view = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            if (view != null)
                view.Refresh();
        }

        void ShowOptions()
        {
            ShowOverlay(optionsOverlay);
        }

        void ShowCredits()
        {
            ShowOverlay(creditsOverlay);
        }

        void OpenPrep(Activity activity)
        {
            _activity = activity;
            _selectedModes.Clear();
            _prepConfirmArmed = false;
            StartCoroutine(ShowPrepAfterPointer());
        }

        IEnumerator ShowPrepAfterPointer()
        {
            yield return null;
            ShowPrep();
            yield return null;
            _prepConfirmArmed = true;
        }

        void ShowPrep()
        {
            ShowOverlay(matchSettingsOverlay);
            if (matchSettingsOverlay == null)
                return;

            BindDropdown(matchSettingsOverlay, "TimeDropdown", new[] { "None", "10+5", "5+3" }, _timePreset, v => _timePreset = v);
            BindDropdown(matchSettingsOverlay, "HostColorDropdown", new[] { "White", "Black", "Random" }, (int)_hostColor, v => _hostColor = (HostColor)v);

            Transform aiGroup = FindChild(matchSettingsOverlay.transform, "AiGroup");
            if (aiGroup != null)
                aiGroup.gameObject.SetActive(_activity == Activity.VersusAi);
            if (_activity == Activity.VersusAi)
                BindDropdown(matchSettingsOverlay, "AiDropdown", new[] { "Easy", "Medium", "Hard" }, (int)_aiStrength, v => _aiStrength = (AiStrength)v);

            Button confirm = FindButton(matchSettingsOverlay, "ConfirmButton");
            if (confirm != null)
            {
                TMP_Text label = confirm.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = _activity == Activity.VersusAi ? "Start" : "Create Lobby";
            }

            FillModeToggles();
        }

        void FillModeToggles()
        {
            ScrollRect scroll = FindModesScroll();
            if (scroll == null || scroll.content == null)
                return;

            if (matchSettingsOverlay != null)
                _modeSettingsPopup = ModeSettingsPopup.Ensure(matchSettingsOverlay.transform);

            Transform content = scroll.content;
            VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
            if (contentLayout != null)
            {
                contentLayout.childForceExpandWidth = true;
                contentLayout.childControlWidth = true;
                contentLayout.childControlHeight = false;
            }

            for (int i = 0; i < _spawnedModeToggles.Count; i++)
            {
                if (_spawnedModeToggles[i] != null)
                    Destroy(_spawnedModeToggles[i]);
            }

            _spawnedModeToggles.Clear();
            for (int i = content.childCount - 1; i >= 0; i--)
                Destroy(content.GetChild(i).gameObject);

            GameObject togglePrefab = RuntimePrefabs.Toggle;
            ModeDefinition[] modes = ModeCatalog.All;
            for (int i = 0; i < modes.Length; i++)
            {
                ModeDefinition def = modes[i];
                if (!def.Allows(_activity))
                    continue;

                bool owned = ModeDlc.IsOwned(def.Id);
                GameObject row = new GameObject(def.Id.ToString(), typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                row.transform.SetParent(content, false);
                var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
                rowLayout.spacing = 4;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = true;
                var rowElement = row.GetComponent<LayoutElement>();
                rowElement.minHeight = 32;
                rowElement.preferredHeight = 32;
                rowElement.minWidth = 200;
                rowElement.flexibleWidth = 1;

                GameObject toggleGo;
                if (togglePrefab != null)
                {
                    toggleGo = Object.Instantiate(togglePrefab, row.transform);
                }
                else
                {
                    toggleGo = new GameObject(def.DisplayName, typeof(RectTransform), typeof(Toggle));
                    toggleGo.transform.SetParent(row.transform, false);
                }
                toggleGo.name = def.DisplayName;
                toggleGo.SetActive(true);
                var toggleElement = toggleGo.GetComponent<LayoutElement>();
                if (toggleElement == null)
                    toggleElement = toggleGo.AddComponent<LayoutElement>();
                toggleElement.minWidth = 160;
                toggleElement.flexibleWidth = 1;
                toggleElement.minHeight = 32;
                toggleElement.preferredHeight = 32;

                Toggle toggle = toggleGo.GetComponent<Toggle>();
                TMP_Text text = toggleGo.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = owned ? def.DisplayName : def.DisplayName + " (Unlocks)";
                    text.enableWordWrapping = false;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                    RectTransform textRect = text.rectTransform;
                    textRect.anchorMin = new Vector2(0f, 0f);
                    textRect.anchorMax = new Vector2(1f, 1f);
                    textRect.offsetMin = new Vector2(28f, 0f);
                    textRect.offsetMax = new Vector2(-4f, 0f);
                }

                ModeId captured = def.Id;
                toggle.interactable = owned;
                toggle.isOn = owned && _selectedModes.Contains(captured);
                toggle.onValueChanged.RemoveAllListeners();
                if (owned)
                {
                    toggle.onValueChanged.AddListener(value =>
                    {
                        if (value && !_selectedModes.Contains(captured))
                            _selectedModes.Add(captured);
                        if (!value)
                            _selectedModes.Remove(captured);
                    });
                }

                Button settings = UiFactory.Button(row.transform, "...", () => OpenModeSettings(captured, scroll), new Vector2(32f, 32f));
                settings.name = "SettingsButton";
                var settingsElement = settings.gameObject.GetComponent<LayoutElement>();
                if (settingsElement == null)
                    settingsElement = settings.gameObject.AddComponent<LayoutElement>();
                settingsElement.minWidth = 32;
                settingsElement.preferredWidth = 32;
                settingsElement.flexibleWidth = 0;
                settingsElement.minHeight = 32;
                settingsElement.preferredHeight = 32;

                _spawnedModeToggles.Add(row);
            }
        }

        void OpenModeSettings(ModeId id, ScrollRect scroll)
        {
            if (matchSettingsOverlay != null)
                _modeSettingsPopup = ModeSettingsPopup.Ensure(matchSettingsOverlay.transform);
            if (_modeSettingsPopup == null)
                return;
            RectTransform slideFrom = scroll != null ? scroll.transform as RectTransform : null;
            _modeSettingsPopup.Open(id, _modeSettings, slideFrom);
        }

        ScrollRect FindModesScroll()
        {
            Transform named = FindChild(matchSettingsOverlay.transform, "ModesScroll");
            if (named == null)
                named = FindChild(matchSettingsOverlay.transform, "Scroll View");
            if (named != null)
            {
                ScrollRect namedScroll = named.GetComponent<ScrollRect>();
                if (namedScroll == null)
                    namedScroll = named.GetComponentInChildren<ScrollRect>(true);
                if (namedScroll != null)
                    return namedScroll;
            }

            ScrollRect[] rects = matchSettingsOverlay.GetComponentsInChildren<ScrollRect>(true);
            for (int i = 0; i < rects.Length; i++)
            {
                if (rects[i].GetComponentInParent<TMP_Dropdown>(true) == null)
                    return rects[i];
            }

            return null;
        }

        void ConfirmPrep()
        {
            if (!_prepConfirmArmed)
                return;
            if (_modeSettingsPopup != null && _modeSettingsPopup.IsOpen)
                return;

            MatchSettings settings = new MatchSettings(
                TimeFromPreset(),
                _hostColor,
                _aiStrength,
                false,
                _modeSettings.EmpoweredCount,
                _modeSettings.MartyrThreshold,
                _modeSettings.MartyrDraftOptions);
            var rules = new MatchRules(_selectedModes, settings);
            if (_activity == Activity.VersusAi)
            {
                StartMatch(new MatchSession
                {
                    Activity = _activity,
                    Rules = rules,
                    PlayerSide = ResolveColor(settings.HostColor),
                    Hotseat = false
                });
                return;
            }

            _lobby = new LocalLobby(LocalLobby.CreateCode(), rules, settings);
            _openLobby = _lobby;
            ShowLobby();
        }

        void ShowLobby()
        {
            ShowOverlay(lobbyOverlay);
            if (lobbyOverlay == null || _lobby == null)
                return;

            TMP_Text code = FindLabel(lobbyOverlay, "JoinCodeLabel");
            if (code != null)
                code.text = "Join Code: " + _lobby.Code;
            TMP_Text status = FindLabel(lobbyOverlay, "StatusLabel");
            if (status != null)
                status.text = _lobby.FriendSeated ? "Friend seated" : "Waiting for friend";
            Button start = FindButton(lobbyOverlay, "StartButton");
            if (start != null)
            {
                start.interactable = _lobby.FriendSeated;
                TMP_Text label = start.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = _lobby.FriendSeated ? "Start" : "waiting for host";
            }
        }

        void SitAsFriend()
        {
            if (_lobby == null)
                return;
            _lobby.FriendSeated = true;
            ShowLobby();
        }

        void StartLobbyMatch()
        {
            if (_lobby == null || !_lobby.FriendSeated)
                return;
            StartMatch(new MatchSession
            {
                Activity = Activity.VersusFriend,
                Rules = _lobby.Rules,
                PlayerSide = ResolveColor(_lobby.Settings.HostColor),
                Hotseat = true,
                JoinCode = _lobby.Code
            });
        }

        void LeaveLobby()
        {
            _openLobby = null;
            _lobby = null;
            ShowPlay();
        }

        void ShowJoin()
        {
            ShowOverlay(joinOverlay);
        }

        void EnterJoinCode()
        {
            TMP_InputField field = joinOverlay != null ? joinOverlay.GetComponentInChildren<TMP_InputField>(true) : null;
            if (_openLobby == null || field == null || field.text.Trim().ToUpperInvariant() != _openLobby.Code)
                return;
            _openLobby.FriendSeated = true;
            _lobby = _openLobby;
            ShowLobby();
        }

        void OpenFeedback()
        {
            Application.OpenURL(FeedbackUrl);
        }

        void ExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void StartMatch(MatchSession session)
        {
            ShowBoard();
            if (_match == null)
                _match = FindAnyObjectByType<MatchController>();
            _match.Launch(session);
        }

        TimeControl TimeFromPreset()
        {
            switch (_timePreset)
            {
                case 1:
                    return TimeControl.TenPlusFive;
                case 2:
                    return TimeControl.FivePlusThree;
                default:
                    return TimeControl.None;
            }
        }

        static Side ResolveColor(HostColor color)
        {
            switch (color)
            {
                case HostColor.White:
                    return Side.White;
                case HostColor.Black:
                    return Side.Black;
                case HostColor.Random:
                    return Random.value < 0.5f ? Side.White : Side.Black;
                default:
                    throw new System.ArgumentOutOfRangeException(nameof(color), color, null);
            }
        }

        static void BindButton(GameObject root, string name, UnityEngine.Events.UnityAction action)
        {
            Button button = FindButton(root, name);
            if (button == null)
                return;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        static void BindDropdown(GameObject root, string name, string[] options, int selected, UnityEngine.Events.UnityAction<int> changed)
        {
            Transform child = FindChild(root.transform, name);
            if (child == null)
                return;
            TMP_Dropdown dropdown = child.GetComponent<TMP_Dropdown>();
            if (dropdown == null)
                return;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options));
            dropdown.value = selected;
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(changed);
        }

        static Button FindButton(GameObject root, string name)
        {
            if (root == null)
                return null;
            Transform child = FindChild(root.transform, name);
            return child != null ? child.GetComponent<Button>() : null;
        }

        static Toggle FindToggle(GameObject root, string name)
        {
            if (root == null)
                return null;
            Transform child = FindChild(root.transform, name);
            return child != null ? child.GetComponent<Toggle>() : null;
        }

        static TMP_Text FindLabel(GameObject root, string name)
        {
            if (root == null)
                return null;
            Transform child = FindChild(root.transform, name);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }

        static Transform FindChild(Transform root, string name)
        {
            if (root.name == name)
                return root;
            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
