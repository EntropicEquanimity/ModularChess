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
        [SerializeField] GameObject mainMenu;
        [SerializeField] GameObject playOverlay;
        [SerializeField] GameObject matchSettingsOverlay;
        [SerializeField] GameObject lobbyOverlay;
        [SerializeField] GameObject joinOverlay;
        [SerializeField] GameObject unlocksOverlay;
        [SerializeField] GameObject optionsOverlay;
        [SerializeField] GameObject creditsOverlay;
        [SerializeField] GameObject accountCreationOverlay;
        [SerializeField] GameObject feedbackOverlay;
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
        bool _locHooked;
        bool _pausedForOptions;
        bool _optionsHooked;

        public void Initialize()
        {
            GameAudio.Ensure();
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
            GameAudio.Ensure();
            EnterApp();
        }

        void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            if (_match != null)
                _match.LeftMatch -= ShowMainMenu;
            UnhookOptionsOverlay();
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

            if (accountCreationOverlay == null)
            {
                Transform found = transform.Find("AccountCreation");
                if (found != null)
                    accountCreationOverlay = found.gameObject;
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
                creditsOverlay,
                accountCreationOverlay,
                feedbackOverlay
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
            BindButton(mainMenu, "FeedbackButton", ShowFeedback);
            BindButton(mainMenu, "ExitButton", ShowQuitConfirm);
            Button customize = FindButton(mainMenu, "CustomizeButton");
            if (customize != null)
                customize.interactable = false;
            BindMenuLoc();
            HookLanguage();
            OverlayMotion.Ensure(mainMenu);
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
            BindButton(creditsOverlay, "BackButton", ShowMainMenu);

            HookOptionsOverlay();
            EnsureLanguageDropdown();
            BindAccountCreation();
            BindMenuLoc();
            ApplyWebGlPlayLimits();
            HookLanguage();
            WarmOverlayMotions();
        }

        void ShowOverlay(GameObject overlay)
        {
            if (overlay != matchSettingsOverlay)
                _modeSettingsPopup?.HideImmediate();
            UnlocksView unlocks = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            if (overlay != unlocksOverlay)
                unlocks?.HideDetailImmediate();

            HideBoard();
            DismissScreens(overlay);
            OverlayMotion.Ensure(overlay)?.PlayEnter();
        }

        void HideOverlays()
        {
            _modeSettingsPopup?.HideImmediate();
            UnlocksView unlocks = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            unlocks?.HideDetailImmediate();
            DismissScreens(null);
        }

        void DismissScreens(GameObject keep)
        {
            DismissScreen(mainMenu, keep);
            GameObject[] overlays = OverlayList();
            for (int i = 0; i < overlays.Length; i++)
                DismissScreen(overlays[i], keep);
        }

        static void DismissScreen(GameObject go, GameObject keep)
        {
            if (go == null || go == keep || !go.activeSelf)
                return;
            OverlayMotion.Ensure(go).PlayExit();
        }

        void WarmOverlayMotions()
        {
            OverlayMotion.Ensure(mainMenu);
            GameObject[] overlays = OverlayList();
            for (int i = 0; i < overlays.Length; i++)
                OverlayMotion.Ensure(overlays[i]);
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
            HideOverlays();
            if (_board != null)
                _board.gameObject.SetActive(true);
            if (_hud != null)
                _hud.gameObject.SetActive(true);
        }

        void EnterApp()
        {
            HideBoard();
            if (PlayerIdentity.HasName)
                ShowMainMenu();
            else
                ShowAccountCreation();
        }

        public void ShowMainMenu()
        {
            _pausedForOptions = false;
            if (!PlayerIdentity.HasName)
            {
                ShowAccountCreation();
                return;
            }

            HideBoard();
            DismissScreens(mainMenu);
            OverlayMotion.Ensure(mainMenu)?.PlayEnter();
            GameAudio.PlayMenuMusic();
        }

        void ShowAccountCreation()
        {
            if (PlayerIdentity.HasName)
            {
                ShowMainMenu();
                return;
            }

            BindAccountCreation();
            ShowOverlay(accountCreationOverlay);
            GameAudio.PlayMenuMusic();
        }

        void ShowPlay()
        {
            ShowOverlay(playOverlay);
            ApplyWebGlPlayLimits();
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
            OptionsOverlay.Ensure()?.OpenFromMenu();
        }
        void HookOptionsOverlay()
        {
            OptionsOverlay options = OptionsOverlay.Ensure();
            if (options == null)
                return;
            if (optionsOverlay == null)
                optionsOverlay = options.gameObject;
            if (_optionsHooked)
            {
                options.Refresh();
                return;
            }
            options.OpeningFromMenu += OnOptionsOpeningFromMenu;
            options.OpeningFromMatch += OnOptionsOpeningFromMatch;
            options.ClosedFromMenu += ShowMainMenu;
            options.ClosedFromMatch += OnOptionsClosedFromMatch;
            _optionsHooked = true;
            options.Refresh();
        }
        void UnhookOptionsOverlay()
        {
            if (!_optionsHooked)
                return;
            OptionsOverlay options = OptionsOverlay.Instance;
            if (options != null)
            {
                options.OpeningFromMenu -= OnOptionsOpeningFromMenu;
                options.OpeningFromMatch -= OnOptionsOpeningFromMatch;
                options.ClosedFromMenu -= ShowMainMenu;
                options.ClosedFromMatch -= OnOptionsClosedFromMatch;
            }
            _optionsHooked = false;
        }
        void OnOptionsOpeningFromMenu()
        {
            if (optionsOverlay == null && OptionsOverlay.Instance != null)
                optionsOverlay = OptionsOverlay.Instance.gameObject;
            _modeSettingsPopup?.HideImmediate();
            UnlocksView unlocks = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            unlocks?.HideDetailImmediate();
            HideBoard();
            DismissScreens(optionsOverlay);
        }
        void OnOptionsOpeningFromMatch()
        {
            _hud?.CloseOptionsIfOpen();
            if (_match != null && !_match.IsPaused)
            {
                _pausedForOptions = true;
                _match.SetPaused(true);
            }
        }
        void OnOptionsClosedFromMatch()
        {
            if (!_pausedForOptions)
                return;
            _match?.SetPaused(false);
            _pausedForOptions = false;
        }

        void ShowCredits()
        {
            ShowOverlay(creditsOverlay);
        }

        void ShowFeedback()
        {
            EnsureFeedbackOverlay();
            if (feedbackOverlay == null)
                return;
            FeedbackSurvey survey = feedbackOverlay.GetComponent<FeedbackSurvey>();
            if (survey == null)
                return;
            survey.Closed -= ShowMainMenu;
            survey.Closed += ShowMainMenu;
            ShowOverlay(feedbackOverlay);
            survey.Open();
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
                new[]
                {
                    Loc.Get("settings.time.none"),
                    Loc.Get("settings.time.bullet"),
                    Loc.Get("settings.time.blitz"),
                    Loc.Get("settings.time.rapid"),
                    Loc.Get("settings.time.standard"),
                    Loc.Get("settings.time.extended")
                },
                _timePreset,
                OnTimePresetChanged);
            EnsureIncrementDropdown();
            BindDropdown(
                matchSettingsOverlay,
                "IncrementDropdown",
                new[]
                {
                    Loc.Get("settings.inc.none"),
                    Loc.Get("settings.inc.1"),
                    Loc.Get("settings.inc.2"),
                    Loc.Get("settings.inc.5"),
                    Loc.Get("settings.inc.10"),
                    Loc.Get("settings.inc.15"),
                    Loc.Get("settings.inc.30"),
                    Loc.Get("settings.inc.60")
                },
                _incrementPreset,
                v => _incrementPreset = v);
            RefreshIncrementInteractable();
            BindDropdown(
                matchSettingsOverlay,
                "HostColorDropdown",
                new[]
                {
                    Loc.Get("settings.color.white"),
                    Loc.Get("settings.color.black"),
                    Loc.Get("settings.color.random")
                },
                (int)_hostColor,
                v => _hostColor = (HostColor)v);

            Transform aiGroup = FindChild(matchSettingsOverlay.transform, "AiGroup");
            if (aiGroup != null)
                aiGroup.gameObject.SetActive(_activity == Activity.VersusAi);
            if (_activity == Activity.VersusAi)
                BindDropdown(
                    matchSettingsOverlay,
                    "AiDropdown",
                    new[] { Loc.Get("settings.ai.easy"), Loc.Get("settings.ai.medium"), Loc.Get("settings.ai.hard") },
                    (int)_aiStrength,
                    v => _aiStrength = (AiStrength)v);

            Button confirm = FindButton(matchSettingsOverlay, "ConfirmButton");
            if (confirm != null)
            {
                TMP_Text label = confirm.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = _activity == Activity.VersusAi
                        ? Loc.Get("settings.start")
                        : Loc.Get("settings.createLobby");
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
                    text.text = owned
                        ? Loc.ModeName(def.Id)
                        : Loc.Format("mode.unlocks.suffix", Loc.ModeName(def.Id));
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
                GameAudio.Bind(settings, () => OpenModeSettings(captured, scroll));

                void RefreshSettingsAccess()
                {
                    settings.interactable = owned && toggle.isOn;
                }

                if (owned)
                {
                    toggle.onValueChanged.AddListener(value =>
                    {
                        GameAudio.PlayUi();
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
                code.text = Loc.Format("lobby.code", _lobby.Code);
            TMP_Text status = FindLabel(lobbyOverlay, "StatusLabel");
            if (status != null)
                status.text = _lobby.FriendSeated ? Loc.Get("lobby.seated") : Loc.Get("lobby.waiting");
            Button start = FindButton(lobbyOverlay, "StartButton");
            if (start != null)
            {
                start.interactable = _lobby.FriendSeated;
                TMP_Text label = start.GetComponentInChildren<TMP_Text>();
                if (label != null)
                    label.text = _lobby.FriendSeated ? Loc.Get("lobby.start") : Loc.Get("lobby.waitingHost");
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
            if (IsActive(accountCreationOverlay))
            {
                ShowQuitConfirm();
                return;
            }
            if (_modeSettingsPopup != null && _modeSettingsPopup.IsOpen)
            {
                _modeSettingsPopup.Close();
                return;
            }

            UnlocksView unlocks = unlocksOverlay != null ? unlocksOverlay.GetComponent<UnlocksView>() : null;
            if (unlocks != null && unlocks.CloseDetailIfOpen())
                return;

            if (OptionsOverlay.IsOpen)
            {
                OptionsOverlay.Ensure()?.Close();
                return;
            }

            if (_match != null && _match.IsPlaying && _board != null && _board.gameObject.activeSelf)
            {
                _hud?.ToggleOptions();
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

            if (IsActive(unlocksOverlay) || IsActive(optionsOverlay) || IsActive(creditsOverlay) || IsActive(feedbackOverlay))
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
            TMP_Dropdown created = UiFactory.Dropdown(time.parent, new[] { Loc.Get("settings.inc.none") }, 0, null);
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
            GameAudio.PlayMatchMusic();
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

        void HookLanguage()
        {
            if (_locHooked)
                return;
            _locHooked = true;
            Loc.Changed += OnLanguageChanged;
        }

        void OnLanguageChanged()
        {
            BindMenuLoc();
            OptionsOverlay.Ensure()?.Refresh();
            EnsureLanguageDropdown();
            BindAccountCreation();
            if (IsActive(matchSettingsOverlay))
                ShowPrep();
            if (IsActive(lobbyOverlay))
                ShowLobby();
            if (IsActive(unlocksOverlay))
                ShowUnlocks();
            ApplyWebGlPlayLimits();
        }

        void BindMenuLoc()
        {
            LocalizedText.Bind(FindButton(mainMenu, "PlayButton"), "menu.play");
            LocalizedText.Bind(FindButton(mainMenu, "UnlocksButton"), "menu.unlocks");
            LocalizedText.Bind(FindButton(mainMenu, "OptionsButton"), "menu.options");
            LocalizedText.Bind(FindButton(mainMenu, "CreditsButton"), "menu.credits");
            LocalizedText.Bind(FindButton(mainMenu, "FeedbackButton"), "menu.feedback");
            LocalizedText.Bind(FindButton(mainMenu, "ExitButton"), "menu.exit");
            LocalizedText.Bind(FindButton(mainMenu, "CustomizeButton"), "menu.customize");
            BindTitle(mainMenu, "menu.title");
            LocalizedText.Bind(FindButton(playOverlay, "VersusAiButton"), "play.versusAi");
            LocalizedText.Bind(FindButton(playOverlay, "VersusFriendButton"), "play.versusFriend");
            LocalizedText.Bind(FindButton(playOverlay, "JoinButton"), "play.join");
            LocalizedText.Bind(FindButton(playOverlay, "BackButton"), "play.back");
            BindTitle(playOverlay, "menu.title");
            LocalizedText.Bind(FindButton(matchSettingsOverlay, "BackButton"), "settings.back");
            BindTitle(matchSettingsOverlay, "menu.title");
            LocalizedText.Bind(FindButton(lobbyOverlay, "SitButton"), "lobby.sit");
            LocalizedText.Bind(FindButton(lobbyOverlay, "LeaveButton"), "lobby.leave");
            BindTitle(lobbyOverlay, "menu.title");
            LocalizedText.Bind(FindButton(joinOverlay, "EnterButton"), "join.enter");
            LocalizedText.Bind(FindButton(joinOverlay, "BackButton"), "play.back");
            BindTitle(joinOverlay, "menu.title");
            LocalizedText.Bind(FindButton(unlocksOverlay, "BackButton"), "play.back");
            BindTitle(unlocksOverlay, "menu.unlocks");
            LocalizedText.Bind(FindButton(creditsOverlay, "BackButton"), "credits.back");
            BindTitle(creditsOverlay, "menu.credits");
            BindTitle(accountCreationOverlay, "account.title");
            Transform languageText = accountCreationOverlay != null
                ? FindChild(accountCreationOverlay.transform, "LanguageText")
                : null;
            if (languageText != null)
                LocalizedText.Bind(languageText, "options.language");
            LocalizedText.Bind(FindButton(accountCreationOverlay, "ConfirmButton"), "account.confirm");
        }

        void BindTitle(GameObject root, string key)
        {
            if (root == null)
                return;
            Transform title = FindChild(root.transform, "Title");
            if (title != null)
                LocalizedText.Bind(title, key);
        }

        void BindAccountCreation()
        {
            EnsureAccountOverlay();
            if (accountCreationOverlay == null)
                return;

            BindLanguageDropdown(accountCreationOverlay, false);
            TMP_InputField field = FindInput(accountCreationOverlay, "NameInput");
            if (field != null)
            {
                field.characterLimit = PlayerIdentity.StemMax;
                field.contentType = TMP_InputField.ContentType.Alphanumeric;
                field.onSubmit.RemoveAllListeners();
                field.onSubmit.AddListener(_ =>
                {
                    GameAudio.PlayUi();
                    ConfirmAccount();
                });
                field.onValueChanged.RemoveAllListeners();
                field.onValueChanged.AddListener(RefreshAccountConfirm);
            }

            Button confirm = FindButton(accountCreationOverlay, "ConfirmButton");
            BindButton(accountCreationOverlay, "ConfirmButton", ConfirmAccount);
            LocalizedText.Bind(confirm, "account.confirm");
            RefreshAccountConfirm(field != null ? field.text : string.Empty);
        }

        void EnsureAccountOverlay()
        {
            if (accountCreationOverlay != null)
                return;
            GameObject prefab = RuntimePrefabs.AccountCreation;
            if (prefab == null)
                return;
            accountCreationOverlay = Instantiate(prefab, transform);
            accountCreationOverlay.name = "AccountCreation";
            accountCreationOverlay.SetActive(false);
            OverlayMotion.Ensure(accountCreationOverlay);
        }

        void EnsureFeedbackOverlay()
        {
            if (feedbackOverlay != null)
                return;
            GameObject prefab = RuntimePrefabs.FeedbackSurvey;
            if (prefab == null)
                return;
            feedbackOverlay = Instantiate(prefab, transform);
            feedbackOverlay.name = "FeedbackSurvey";
            feedbackOverlay.SetActive(false);
            OverlayMotion.Ensure(feedbackOverlay);
        }

        void ConfirmAccount()
        {
            if (PlayerIdentity.HasName)
            {
                ShowMainMenu();
                return;
            }

            TMP_InputField field = FindInput(accountCreationOverlay, "NameInput");
            string stem = field != null ? field.text : string.Empty;
            if (!PlayerIdentity.TryCommit(stem))
                return;
            ShowMainMenu();
        }

        void RefreshAccountConfirm(string stem)
        {
            Button confirm = FindButton(accountCreationOverlay, "ConfirmButton");
            if (confirm != null)
                confirm.interactable = PlayerIdentity.Sanitize(stem).Length > 0;
        }

        void EnsureLanguageDropdown()
        {
            BindLanguageDropdown(accountCreationOverlay, false);
        }

        void BindLanguageDropdown(GameObject root, bool createIfMissing)
        {
            if (root == null)
                return;

            Transform row = FindChild(root.transform, "LanguageDropdown");
            if (row == null)
            {
                if (!createIfMissing)
                    return;
                Transform parent = FindChild(root.transform, "ButtonGroup");
                if (parent == null)
                    return;

                TMP_Dropdown created = UiFactory.Dropdown(parent, LanguageOptionLabels(), Loc.LanguageIndex(), null);
                created.name = "LanguageDropdown";
                created.gameObject.name = "LanguageDropdown";
                row = created.transform;
                var element = created.gameObject.GetComponent<LayoutElement>();
                if (element == null)
                    element = created.gameObject.AddComponent<LayoutElement>();
                element.minWidth = 200f;
                element.preferredWidth = 200f;
                element.minHeight = 32f;
                element.preferredHeight = 32f;
            }

            TMP_Dropdown dropdown = row.GetComponent<TMP_Dropdown>();
            if (dropdown == null)
                dropdown = row.GetComponentInChildren<TMP_Dropdown>(true);
            if (dropdown == null)
                return;
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(LanguageOptionLabels()));
            dropdown.SetValueWithoutNotify(Loc.LanguageIndex());
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(index =>
            {
                GameAudio.PlayUi();
                if (index >= 0 && index < Loc.Codes.Length)
                    Loc.SetLanguage(Loc.Codes[index]);
            });
        }

        static string[] LanguageOptionLabels()
        {
            return new[] { Loc.Get("lang.en"), Loc.Get("lang.es"), Loc.Get("lang.tl") };
        }

        void ApplyWebGlPlayLimits()
        {
            if (Application.platform != RuntimePlatform.WebGLPlayer)
                return;
            Button friend = FindButton(playOverlay, "VersusFriendButton");
            if (friend != null)
                friend.interactable = false;
            Button join = FindButton(playOverlay, "JoinButton");
            if (join != null)
                join.interactable = false;
        }

        static void BindButton(GameObject root, string name, UnityEngine.Events.UnityAction action)
        {
            GameAudio.Bind(FindButton(root, name), action);
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
            dropdown.SetValueWithoutNotify(selected);
            dropdown.onValueChanged.RemoveAllListeners();
            dropdown.onValueChanged.AddListener(v =>
            {
                GameAudio.PlayUi();
                changed?.Invoke(v);
            });
        }

        static TMP_InputField FindInput(GameObject root, string name)
        {
            if (root == null)
                return null;
            Transform child = FindChild(root.transform, name);
            return child != null ? child.GetComponentInChildren<TMP_InputField>(true) : null;
        }

        static Button FindButton(GameObject root, string name)
        {
            if (root == null)
                return null;
            Transform child = FindChild(root.transform, name);
            return child != null ? child.GetComponent<Button>() : null;
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
