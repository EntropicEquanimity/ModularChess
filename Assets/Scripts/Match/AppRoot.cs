using System.Collections;
using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
        int _incrementPreset;
        LocalLobby _lobby;
        static LocalLobby _openLobby;
        bool _prepConfirmArmed;
        ModeSettingsPopup _modeSettingsPopup;
        OverlayDialogs _dialogs;
        readonly List<float> _gravePresses = new List<float>();

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

        void Update()
        {
            if (_dialogs == null)
                _dialogs = OverlayDialogs.Ensure(transform);

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
                return;

            if (keyboard[Key.Backquote].wasPressedThisFrame)
                HandleGrave();
            if (keyboard.escapeKey.wasPressedThisFrame)
                HandleEscape();
        }

        void HandleGrave()
        {
            float now = Time.unscaledTime;
            _gravePresses.Add(now);
            _gravePresses.RemoveAll(t => now - t > 1f);
            if (_gravePresses.Count < 4)
                return;
            _gravePresses.Clear();
            if (_dialogs.DebugOpen)
            {
                _dialogs.HideDebugImmediate();
                return;
            }

            _dialogs.ShowDebug(DebugResetSave, DebugUnlockAll, DebugWin, DebugLose, DebugResetTimer);
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
            BindButton(mainMenu, "ExitButton", ShowQuitConfirm);
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

            EnsureAnimationSlider();
        }

        void ShowOverlay(GameObject overlay)
        {
            if (overlay != matchSettingsOverlay)
                _modeSettingsPopup?.HideImmediate();
            UnlocksView unlocks = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            if (overlay != unlocksOverlay)
                unlocks?.HideDetailImmediate();

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
            UnlocksView unlocks = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            unlocks?.HideDetailImmediate();
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

            BindDropdown(
                matchSettingsOverlay,
                "TimeDropdown",
                new[] { "None", "Bullet (1 min)", "Blitz (5 min)", "Rapid (15 min)", "Standard (30 min)", "Extended (120 min)" },
                _timePreset,
                OnTimePresetChanged);
            EnsureIncrementDropdown();
            BindDropdown(
                matchSettingsOverlay,
                "IncrementDropdown",
                new[] { "None", "1 second", "2 seconds", "5 seconds", "10 seconds", "15 seconds", "30 seconds", "60 seconds" },
                _incrementPreset,
                v => _incrementPreset = v);
            RefreshIncrementInteractable();
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

        void EnsureAnimationSlider()
        {
            if (optionsOverlay == null)
                return;

            Transform group = FindChild(optionsOverlay.transform, "ButtonGroup");
            if (group == null)
                return;

            Transform existing = FindChild(optionsOverlay.transform, "AnimationRow");
            GameObject rowGo;
            TMP_Text valueLabel;
            Slider slider;
            if (existing != null)
            {
                rowGo = existing.gameObject;
                slider = rowGo.GetComponentInChildren<Slider>(true);
                valueLabel = FindLabel(rowGo, "AnimationValue");
            }
            else
            {
                rowGo = new GameObject("AnimationRow", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                rowGo.transform.SetParent(group, false);
                var layout = rowGo.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 8;
                layout.childAlignment = TextAnchor.MiddleLeft;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
                var rowElement = rowGo.GetComponent<LayoutElement>();
                rowElement.minHeight = 32;
                rowElement.preferredHeight = 32;
                rowElement.minWidth = 220;
                rowElement.preferredWidth = 220;
                RectTransform rowRect = rowGo.GetComponent<RectTransform>();
                rowRect.sizeDelta = new Vector2(220f, 32f);
                int backIndex = -1;
                for (int i = 0; i < group.childCount; i++)
                {
                    if (group.GetChild(i).name == "BackButton")
                    {
                        backIndex = i;
                        break;
                    }
                }

                if (backIndex >= 0)
                    rowGo.transform.SetSiblingIndex(backIndex);

                TMP_Text title = UiFactory.Label(rowGo.transform, "Animation", 16, TextAlignmentOptions.MidlineLeft);
                title.color = Color.black;
                title.name = "AnimationLabel";
                var titleElement = title.gameObject.AddComponent<LayoutElement>();
                titleElement.minWidth = 96;
                titleElement.preferredWidth = 96;
                titleElement.flexibleWidth = 0;
                titleElement.minHeight = 32;
                titleElement.preferredHeight = 32;

                slider = UiFactory.Slider(rowGo.transform, AnimationPrefs.SliderValue, null);
                var sliderElement = slider.gameObject.AddComponent<LayoutElement>();
                sliderElement.minWidth = 80;
                sliderElement.preferredWidth = 80;
                sliderElement.flexibleWidth = 1;
                sliderElement.minHeight = 32;
                sliderElement.preferredHeight = 32;

                valueLabel = UiFactory.Label(rowGo.transform, AnimationPrefs.SpeedLabel, 16, TextAlignmentOptions.MidlineRight);
                valueLabel.color = Color.black;
                valueLabel.name = "AnimationValue";
                var valueElement = valueLabel.gameObject.AddComponent<LayoutElement>();
                valueElement.minWidth = 40;
                valueElement.preferredWidth = 40;
                valueElement.flexibleWidth = 0;
                valueElement.minHeight = 32;
                valueElement.preferredHeight = 32;
            }

            if (slider == null)
                return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.SetValueWithoutNotify(AnimationPrefs.SliderValue);
            if (valueLabel != null)
                valueLabel.text = AnimationPrefs.SpeedLabel;
            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(v =>
            {
                AnimationPrefs.SliderValue = v;
                if (valueLabel != null)
                    valueLabel.text = AnimationPrefs.SpeedLabel;
            });
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

            GameObject rowPrefab = RuntimePrefabs.SelectionRow;
            ModeDefinition[] modes = ModeCatalog.All;
            for (int i = 0; i < modes.Length; i++)
            {
                ModeDefinition def = modes[i];
                if (!def.Allows(_activity))
                    continue;

                bool owned = ModeDlc.IsOwned(def.Id);
                GameObject row;
                if (rowPrefab != null)
                {
                    row = Object.Instantiate(rowPrefab, content);
                }
                else
                {
                    row = new GameObject(def.Id.ToString(), typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
                    row.transform.SetParent(content, false);
                }

                row.name = def.Id.ToString();
                row.SetActive(true);
                FitModeRow(row);

                Toggle toggle = row.GetComponentInChildren<Toggle>(true);
                if (toggle == null)
                {
                    Destroy(row);
                    continue;
                }

                GameObject toggleGo = toggle.gameObject;
                var toggleElement = toggleGo.GetComponent<LayoutElement>();
                if (toggleElement == null)
                    toggleElement = toggleGo.AddComponent<LayoutElement>();
                toggleElement.minWidth = 160;
                toggleElement.flexibleWidth = 1;
                toggleElement.minHeight = 32;
                toggleElement.preferredHeight = 32;
                toggleElement.flexibleHeight = 0;

                TMP_Text text = toggleGo.GetComponentInChildren<TMP_Text>();
                if (text != null)
                {
                    text.text = owned ? def.DisplayName : def.DisplayName + " (Unlocks)";
                    text.color = Color.black;
                    text.fontSize = 16;
                    text.textWrappingMode = TextWrappingModes.NoWrap;
                    text.overflowMode = TextOverflowModes.Ellipsis;
                }

                ModeId captured = def.Id;
                toggle.interactable = owned;
                toggle.isOn = owned && _selectedModes.Contains(captured);
                toggle.onValueChanged.RemoveAllListeners();

                var group = row.GetComponent<CanvasGroup>();
                if (group == null)
                    group = row.AddComponent<CanvasGroup>();
                group.alpha = owned ? 1f : 0.45f;
                group.interactable = owned;
                group.blocksRaycasts = true;

                Transform settingsTransform = row.transform.Find("SettingsButton");
                Button settings = settingsTransform != null
                    ? settingsTransform.GetComponent<Button>()
                    : row.GetComponentInChildren<Button>(true);
                if (settings == null)
                    settings = UiFactory.Button(row.transform, "...", null, new Vector2(32f, 32f));
                settings.name = "SettingsButton";
                settings.onClick.RemoveAllListeners();
                settings.onClick.AddListener(() => OpenModeSettings(captured, scroll));

                void RefreshSettingsAccess()
                {
                    settings.interactable = owned && toggle.isOn;
                }

                if (owned)
                {
                    toggle.onValueChanged.AddListener(value =>
                    {
                        if (value && !_selectedModes.Contains(captured))
                            _selectedModes.Add(captured);
                        if (!value)
                            _selectedModes.Remove(captured);
                        RefreshSettingsAccess();
                    });
                }

                RefreshSettingsAccess();
                TMP_Text settingsLabel = settings.GetComponentInChildren<TMP_Text>();
                if (settingsLabel != null)
                {
                    settingsLabel.fontSize = 16;
                    settingsLabel.textWrappingMode = TextWrappingModes.NoWrap;
                    settingsLabel.overflowMode = TextOverflowModes.Overflow;
                }

                settings.GetComponent<RectTransform>().sizeDelta = new Vector2(32f, 32f);
                var settingsElement = settings.gameObject.GetComponent<LayoutElement>();
                if (settingsElement == null)
                    settingsElement = settings.gameObject.AddComponent<LayoutElement>();
                settingsElement.minWidth = 32;
                settingsElement.preferredWidth = 32;
                settingsElement.flexibleWidth = 0;
                settingsElement.minHeight = 32;
                settingsElement.preferredHeight = 32;
                settingsElement.flexibleHeight = 0;

                _spawnedModeToggles.Add(row);
            }
        }

        static void FitModeRow(GameObject row)
        {
            var rowLayout = row.GetComponent<HorizontalLayoutGroup>();
            if (rowLayout != null)
            {
                rowLayout.spacing = 4;
                rowLayout.childAlignment = TextAnchor.MiddleLeft;
                rowLayout.childControlWidth = true;
                rowLayout.childControlHeight = true;
                rowLayout.childForceExpandWidth = false;
                rowLayout.childForceExpandHeight = false;
            }

            var rowElement = row.GetComponent<LayoutElement>();
            if (rowElement == null)
                rowElement = row.AddComponent<LayoutElement>();
            rowElement.minHeight = 32;
            rowElement.preferredHeight = 32;
            rowElement.flexibleHeight = 0;
            rowElement.minWidth = 200;
            rowElement.flexibleWidth = 1;

            RectTransform rect = row.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, 32f);
        }

        void OpenModeSettings(ModeId id, ScrollRect scroll)
        {
            if (!ModeDlc.IsOwned(id) || !_selectedModes.Contains(id))
                return;
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

        void ShowQuitConfirm()
        {
            if (_dialogs == null)
                _dialogs = OverlayDialogs.Ensure(transform);
            _dialogs.ShowQuit(ExitGame, () => _dialogs.HideQuit());
        }

        void HandleEscape()
        {
            if (_dialogs == null)
                _dialogs = OverlayDialogs.Ensure(transform);
            if (_dialogs.CloseTop())
                return;
            if (_modeSettingsPopup != null && _modeSettingsPopup.IsOpen)
            {
                _modeSettingsPopup.Close();
                return;
            }

            UnlocksView unlocks = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            if (unlocks != null && unlocks.CloseDetailIfOpen())
                return;

            if (_match != null && _match.IsPlaying && _board != null && _board.gameObject.activeSelf)
            {
                _match.TryPauseFromEscape();
                return;
            }

            if (IsActive(matchSettingsOverlay))
            {
                ShowPlay();
                return;
            }

            if (IsActive(lobbyOverlay))
            {
                LeaveLobby();
                return;
            }

            if (IsActive(joinOverlay) || IsActive(playOverlay))
            {
                if (IsActive(joinOverlay))
                    ShowPlay();
                else
                    ShowMainMenu();
                return;
            }

            if (IsActive(unlocksOverlay) || IsActive(optionsOverlay) || IsActive(creditsOverlay))
            {
                ShowMainMenu();
                return;
            }

            if (mainMenu != null && mainMenu.activeSelf)
                ShowQuitConfirm();
        }

        static bool IsActive(GameObject go)
        {
            return go != null && go.activeSelf;
        }

        void OnTimePresetChanged(int value)
        {
            _timePreset = value;
            if (value == 0)
                _incrementPreset = 0;
            RefreshIncrementInteractable();
        }

        void RefreshIncrementInteractable()
        {
            Transform child = matchSettingsOverlay != null ? FindChild(matchSettingsOverlay.transform, "IncrementDropdown") : null;
            TMP_Dropdown dropdown = child != null ? child.GetComponent<TMP_Dropdown>() : null;
            if (dropdown == null)
                return;
            bool on = _timePreset != 0;
            dropdown.interactable = on;
            if (!on)
            {
                dropdown.value = 0;
                _incrementPreset = 0;
            }
        }

        void EnsureIncrementDropdown()
        {
            if (matchSettingsOverlay == null)
                return;
            if (FindChild(matchSettingsOverlay.transform, "IncrementDropdown") != null)
                return;
            Transform time = FindChild(matchSettingsOverlay.transform, "TimeDropdown");
            if (time == null)
                return;
            TMP_Dropdown created = UiFactory.Dropdown(time.parent, new[] { "None" }, 0, null);
            created.name = "IncrementDropdown";
            created.gameObject.name = "IncrementDropdown";
            var rect = created.GetComponent<RectTransform>();
            var timeRect = time.GetComponent<RectTransform>();
            rect.sizeDelta = timeRect != null ? timeRect.sizeDelta : new Vector2(200f, 32f);
            var element = created.gameObject.GetComponent<LayoutElement>();
            if (element == null)
                element = created.gameObject.AddComponent<LayoutElement>();
            element.minWidth = 200f;
            element.preferredWidth = 200f;
            element.minHeight = 32f;
            element.preferredHeight = 32f;
            element.flexibleWidth = 0f;
            element.flexibleHeight = 0f;
            created.transform.SetSiblingIndex(time.GetSiblingIndex() + 1);
        }

        void DebugUnlockAll()
        {
            ModeDlc.UnlockAll();
            UnlocksView view = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            view?.Refresh();
            if (matchSettingsOverlay != null && matchSettingsOverlay.activeSelf)
                FillModeToggles();
        }

        void DebugWin()
        {
            _match?.DebugWin();
            _dialogs?.HideDebugImmediate();
        }

        void DebugLose()
        {
            _match?.DebugLose();
            _dialogs?.HideDebugImmediate();
        }

        void DebugResetTimer()
        {
            _match?.DebugResetTimer();
        }

        void DebugResetSave()
        {
            bool inMatch = _match != null && _match.IsPlaying;
            bool afterSetup = inMatch && !_match.InSetup;
            if (afterSetup)
                _match.Resign();

            ModeDlc.ClearAll();
            MatchHistoryStore.Delete();
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            _dialogs?.HideDebugImmediate();
            ExitGame();
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
            int minutes;
            switch (_timePreset)
            {
                case 1:
                    minutes = 1;
                    break;
                case 2:
                    minutes = 5;
                    break;
                case 3:
                    minutes = 15;
                    break;
                case 4:
                    minutes = 30;
                    break;
                case 5:
                    minutes = 120;
                    break;
                default:
                    return TimeControl.None;
            }

            int increment;
            switch (_incrementPreset)
            {
                case 1:
                    increment = 1;
                    break;
                case 2:
                    increment = 2;
                    break;
                case 3:
                    increment = 5;
                    break;
                case 4:
                    increment = 10;
                    break;
                case 5:
                    increment = 15;
                    break;
                case 6:
                    increment = 30;
                    break;
                case 7:
                    increment = 60;
                    break;
                default:
                    increment = 0;
                    break;
            }

            return new TimeControl(minutes, increment);
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
