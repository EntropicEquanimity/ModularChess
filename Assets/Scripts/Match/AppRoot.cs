using System.Collections;
using System.Collections.Generic;
using ModularChess.Core;
using ModularChess.Presentation;
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
        readonly HostModeSettings _modeSettings = new HostModeSettings();
        Activity _activity;
        PlayOverlay _play;
        MatchSettingsOverlay _matchSettings;
        LobbyOverlay _lobbyView;
        JoinOverlay _join;
        CreditsOverlay _credits;
        AccountCreationOverlay _account;
        UnlocksView _unlocks;
        LocalLobby _lobby;
        static LocalLobby _openLobby;
        bool _prepConfirmArmed;
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
            CacheOverlayViews();
            _play?.Bind(
                () => OpenPrep(Activity.VersusAi),
                () => OpenPrep(Activity.VersusFriend),
                ShowJoin,
                ShowMainMenu);
            _matchSettings?.Bind(ConfirmPrep, ShowPlay);
            _lobbyView?.Bind(SitAsFriend, StartLobbyMatch, LeaveLobby);
            _join?.Bind(EnterJoinCode, ShowPlay);
            _unlocks?.Bind(ShowMainMenu);
            _credits?.Bind(ShowMainMenu);
            HookOptionsOverlay();
            BindAccountCreation();
            BindMenuLoc();
            _play?.ApplyWebGlLimits();
            HookLanguage();
            WarmOverlayMotions();
        }
        void CacheOverlayViews()
        {
            if (_play == null && playOverlay != null)
                _play = playOverlay.GetComponent<PlayOverlay>() ?? playOverlay.AddComponent<PlayOverlay>();
            if (_matchSettings == null && matchSettingsOverlay != null)
                _matchSettings = matchSettingsOverlay.GetComponent<MatchSettingsOverlay>()
                    ?? matchSettingsOverlay.AddComponent<MatchSettingsOverlay>();
            if (_lobbyView == null && lobbyOverlay != null)
                _lobbyView = lobbyOverlay.GetComponent<LobbyOverlay>() ?? lobbyOverlay.AddComponent<LobbyOverlay>();
            if (_join == null && joinOverlay != null)
                _join = joinOverlay.GetComponent<JoinOverlay>() ?? joinOverlay.AddComponent<JoinOverlay>();
            if (_credits == null && creditsOverlay != null)
                _credits = creditsOverlay.GetComponent<CreditsOverlay>() ?? creditsOverlay.AddComponent<CreditsOverlay>();
            if (_account == null && accountCreationOverlay != null)
                _account = accountCreationOverlay.GetComponent<AccountCreationOverlay>()
                    ?? accountCreationOverlay.AddComponent<AccountCreationOverlay>();
            if (_unlocks == null && unlocksOverlay != null)
                _unlocks = unlocksOverlay.GetComponent<UnlocksView>();
        }

        void ShowOverlay(GameObject overlay)
        {
            if (overlay != matchSettingsOverlay)
                _matchSettings?.HideModeSettingsImmediate();
            if (overlay != unlocksOverlay)
                _unlocks?.HideDetailImmediate();

            HideBoard();
            DismissScreens(overlay);
            OverlayMotion.Ensure(overlay)?.PlayEnter();
        }

        void HideOverlays()
        {
            _matchSettings?.HideModeSettingsImmediate();
            _unlocks?.HideDetailImmediate();
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
            CacheOverlayViews();
            _play?.ApplyWebGlLimits();
        }

        void ShowUnlocks()
        {
            ShowOverlay(unlocksOverlay);
            CacheOverlayViews();
            _unlocks?.Refresh();
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
            _matchSettings?.HideModeSettingsImmediate();
            _unlocks?.HideDetailImmediate();
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
            CacheOverlayViews();
            _matchSettings?.Present(
                _activity,
                _selectedModes,
                _modeSettings,
                0,
                0,
                HostColor.White,
                AiStrength.Medium);
        }

        void ConfirmPrep()
        {
            if (!_prepConfirmArmed)
                return;
            CacheOverlayViews();
            if (_matchSettings != null && _matchSettings.ModeSettingsOpen)
                return;
            int timePreset = _matchSettings != null ? _matchSettings.TimePreset : 0;
            int incrementPreset = _matchSettings != null ? _matchSettings.IncrementPreset : 0;
            HostColor hostColor = _matchSettings != null ? _matchSettings.HostColor : HostColor.White;
            AiStrength aiStrength = _matchSettings != null ? _matchSettings.AiStrength : AiStrength.Medium;
            MatchSettings settings = new MatchSettings(
                TimeFromPreset(timePreset, incrementPreset),
                hostColor,
                aiStrength,
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
            CacheOverlayViews();
            if (_lobby == null)
                return;
            _lobbyView?.Present(_lobby.Code, _lobby.FriendSeated);
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
            CacheOverlayViews();
            if (_openLobby == null || _join == null || _join.Code != _openLobby.Code)
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
            if (_matchSettings != null && _matchSettings.ModeSettingsOpen)
            {
                _matchSettings.CloseModeSettings();
                return;
            }
            if (_unlocks != null && _unlocks.CloseDetailIfOpen())
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

        void DebugUnlockAll()
        {
            ModeDlc.UnlockAll();
            CacheOverlayViews();
            _unlocks?.Refresh();
            if (matchSettingsOverlay != null && matchSettingsOverlay.activeSelf)
                ShowPrep();
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

        TimeControl TimeFromPreset(int timePreset, int incrementPreset)
        {
            int minutes;
            switch (timePreset)
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
            switch (incrementPreset)
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
            BindAccountCreation();
            if (IsActive(matchSettingsOverlay))
                ShowPrep();
            if (IsActive(lobbyOverlay))
                ShowLobby();
            if (IsActive(unlocksOverlay))
                ShowUnlocks();
            _play?.ApplyWebGlLimits();
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
            CacheOverlayViews();
            _play?.RefreshLoc();
            _matchSettings?.RefreshLoc();
            _lobbyView?.RefreshLoc();
            _join?.RefreshLoc();
            _credits?.RefreshLoc();
            _account?.RefreshLoc();
            LocalizedText.Bind(FindButton(unlocksOverlay, "BackButton"), "play.back");
            BindTitle(unlocksOverlay, "menu.unlocks");
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
            CacheOverlayViews();
            _account?.Bind(ConfirmAccount);
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
            _account = accountCreationOverlay.GetComponent<AccountCreationOverlay>()
                ?? accountCreationOverlay.AddComponent<AccountCreationOverlay>();
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
            if (!PlayerIdentity.TryCommit(_account != null ? _account.Stem : string.Empty))
                return;
            ShowMainMenu();
        }

        static void BindButton(GameObject root, string name, UnityEngine.Events.UnityAction action)
        {
            GameAudio.Bind(FindButton(root, name), action);
        }

        static Button FindButton(GameObject root, string name)
        {
            if (root == null)
                return null;
            Transform child = FindChild(root.transform, name);
            return child != null ? child.GetComponent<Button>() : null;
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
