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
        Activity _activity;
        HostColor _hostColor = HostColor.White;
        AiStrength _aiStrength = AiStrength.Medium;
        int _timePreset;
        LocalLobby _lobby;
        static LocalLobby _openLobby;

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
            ShowPrep();
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
            Transform modesScroll = FindChild(matchSettingsOverlay.transform, "ModesScroll");
            ScrollRect scroll = modesScroll != null ? modesScroll.GetComponent<ScrollRect>() : null;
            if (scroll == null || scroll.content == null)
                return;

            Transform content = scroll.content;
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
                if (!ModeDlc.IsOwned(def.Id))
                    continue;

                GameObject go = togglePrefab != null
                    ? Object.Instantiate(togglePrefab, content)
                    : new GameObject(def.DisplayName, typeof(RectTransform), typeof(Toggle));
                go.name = def.Id.ToString();
                Toggle toggle = go.GetComponent<Toggle>();
                TMP_Text text = go.GetComponentInChildren<TMP_Text>();
                if (text != null)
                    text.text = def.DisplayName;
                ModeId captured = def.Id;
                toggle.isOn = _selectedModes.Contains(captured);
                toggle.onValueChanged.RemoveAllListeners();
                toggle.onValueChanged.AddListener(value =>
                {
                    if (value && !_selectedModes.Contains(captured))
                        _selectedModes.Add(captured);
                    if (!value)
                        _selectedModes.Remove(captured);
                });
                _spawnedModeToggles.Add(go);
            }
        }

        void ConfirmPrep()
        {
            MatchSettings settings = new MatchSettings(
                TimeFromPreset(),
                _hostColor,
                _aiStrength);
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
