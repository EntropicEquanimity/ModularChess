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
        [SerializeField] GameObject historyOverlay;
        [SerializeField] GameObject accountCreationOverlay;
        [SerializeField] GameObject feedbackOverlay;
        [SerializeField] MatchController match;
        [SerializeField] BoardView board;
        [SerializeField] MatchHud hud;
        [SerializeField] RoguelikeController roguelike;

        MatchController _match;
        BoardView _board;
        MatchHud _hud;
        RoguelikeController _roguelike;
        RoguelikeHud _roguelikeHud;
        RoguelikeLobbyView _roguelikeLobby;
        RoguelikeShopView _roguelikeShop;
        MainMenuView _mainMenu;
        readonly List<ModeId> _selectedModes = new List<ModeId>();
        readonly HostModeSettings _modeSettings = new HostModeSettings();
        Activity _activity;
        PlayOverlay _play;
        MatchSettingsOverlay _matchSettings;
        LobbyOverlay _lobbyView;
        JoinOverlay _join;
        CreditsOverlay _credits;
        HistoryOverlay _history;
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
        MatchSession _lastSession;
        int _historyReturnIndex = -1;

        public void Initialize()
        {
            GameAudio.Ensure();
            PopupDirector.Ensure();
            ResolveReferences();
            BindMainMenu();
            BindOverlays();
        }

        void Start()
        {
            ResolveReferences();
            if (_match != null)
            {
                _match.LeftMatch += ShowMainMenu;
                _match.RematchRequested += OnRematchRequested;
                _match.ReplayLeftToHistory += OnReplayLeftToHistory;
            }
            if (_roguelike != null)
                _roguelike.LeftRun += ShowMainMenu;
            BindMainMenu();
            BindOverlays();
            GameAudio.Ensure();
            PopupDirector.Ensure();
            EnterApp();
        }

        void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            if (_match != null)
            {
                _match.LeftMatch -= ShowMainMenu;
                _match.RematchRequested -= OnRematchRequested;
                _match.ReplayLeftToHistory -= OnReplayLeftToHistory;
            }
            if (_roguelike != null)
                _roguelike.LeftRun -= ShowMainMenu;
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
            if (_dialogs == null)
                _dialogs = OverlayDialogs.Ensure(transform);
            if (_dialogs == null)
                return;
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
            _roguelike = roguelike != null ? roguelike : FindAnyObjectByType<RoguelikeController>();
            if (_roguelike == null)
            {
                GameObject go = new GameObject("RoguelikeController");
                go.transform.SetParent(transform, false);
                _roguelike = go.AddComponent<RoguelikeController>();
            }
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
                historyOverlay,
                accountCreationOverlay,
                feedbackOverlay,
                _roguelikeShop != null ? _roguelikeShop.gameObject : null
            };
        }

        void BindMainMenu()
        {
            if (mainMenu == null) return;
            if (_mainMenu == null)
                _mainMenu = mainMenu.GetComponent<MainMenuView>();
            if (_mainMenu == null) return;
            _mainMenu.Bind(
                ShowPlay,
                ShowHistory,
                ShowUnlocks,
                ShowOptions,
                ShowCredits,
                ShowFeedback,
                ShowQuitConfirm);
            HookLanguage();
        }

        void BindOverlays()
        {
            CacheOverlayViews();
            _play?.Bind(
                () => OpenPrep(Activity.VersusAi),
                () => OpenPrep(Activity.VersusFriend),
                ShowJoin,
                ShowRoguelikeLobby,
                ShowMainMenu);
            _play?.RefreshLocks();
            _matchSettings?.Bind(ConfirmPrep, ShowPlay);
            _lobbyView?.Bind(SitAsFriend, StartLobbyMatch, LeaveLobby);
            _join?.Bind(EnterJoinCode, ShowPlay);
            _unlocks?.Bind(ShowMainMenu);
            _credits?.Bind(ShowMainMenu);
            EnsureHistoryOverlay();
            _history?.Bind(ShowMainMenu, StartHistoryReplay);
            HookOptionsOverlay();
            BindAccountCreation();
            BindMenuLoc();
            _play?.ApplyWebGlLimits();
            HookLanguage();
            WarmOverlayMotions();
            _play?.RefreshLocks();
        }
        void CacheOverlayViews()
        {
            if (_play == null && playOverlay != null)
                _play = playOverlay.GetComponent<PlayOverlay>();
            if (_matchSettings == null && matchSettingsOverlay != null)
                _matchSettings = matchSettingsOverlay.GetComponent<MatchSettingsOverlay>();
            if (_lobbyView == null && lobbyOverlay != null)
                _lobbyView = lobbyOverlay.GetComponent<LobbyOverlay>();
            if (_join == null && joinOverlay != null)
                _join = joinOverlay.GetComponent<JoinOverlay>();
            if (_credits == null && creditsOverlay != null)
                _credits = creditsOverlay.GetComponent<CreditsOverlay>();
            if (_history == null && historyOverlay != null)
                _history = historyOverlay.GetComponent<HistoryOverlay>();
            if (_account == null && accountCreationOverlay != null)
                _account = accountCreationOverlay.GetComponent<AccountCreationOverlay>();
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
            if (_roguelikeLobby != null)
                DismissScreen(_roguelikeLobby.gameObject, keep);
            if (_roguelikeHud != null)
                DismissScreen(_roguelikeHud.gameObject, keep);
            if (_roguelikeShop != null)
                DismissScreen(_roguelikeShop.gameObject, keep);
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
            _play?.RefreshLocks();
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

        void ShowHistory()
        {
            EnsureHistoryOverlay();
            CacheOverlayViews();
            ShowOverlay(historyOverlay);
            _history?.Refresh();
            if (_historyReturnIndex >= 0)
            {
                _history?.SelectIndex(_historyReturnIndex);
                _historyReturnIndex = -1;
            }
        }

        void StartHistoryReplay(MatchHistoryRecord record)
        {
            if (record == null || !record.Replayable || _match == null)
                return;
            _historyReturnIndex = _history != null ? _history.SelectedIndex : -1;
            ShowBoard();
            _match.LaunchReplay(record, fromHistory: true);
            GameAudio.PlayMatchMusic();
        }

        void OnReplayLeftToHistory()
        {
            ShowHistory();
            GameAudio.PlayMenuMusic();
        }

        void OnRematchRequested()
        {
            if (_lastSession == null || _match == null)
                return;
            if (_lastSession.Activity == Activity.VersusFriend)
            {
                ShowMainMenu();
                OpenPrep(Activity.VersusFriend);
                return;
            }
            MatchSession next = new MatchSession
            {
                Activity = _lastSession.Activity,
                Rules = _lastSession.Rules,
                PlayerSide = ResolveColor(_lastSession.Rules.Settings.HostColor),
                Hotseat = _lastSession.Hotseat,
                JoinCode = _lastSession.JoinCode
            };
            StartMatch(next);
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
            var rules = new VersusRules(_selectedModes, settings);
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
            _lobbyView?.Present(_lobby.Code, _lobby.FriendSeated, _lobby.Rules.AllowsHotseat());
        }

        void SitAsFriend()
        {
            if (_lobby == null || !_lobby.Rules.AllowsHotseat()) return;
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
            if (_dialogs == null)
                return;
            _dialogs.ShowQuit(ExitGame, () => _dialogs.HideQuit());
        }

        void HandleEscape()
        {
            if (_dialogs == null)
                _dialogs = OverlayDialogs.Ensure(transform);
            if (_dialogs != null && _dialogs.CloseTop())
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
                if (_match.IsReplaying)
                {
                    _match.LeaveToMenu();
                    return;
                }
                _hud?.ToggleOptions();
                return;
            }

            if (_roguelike != null && _roguelike.IsPlaying)
            {
                _roguelike.Leave();
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

            if (IsActive(unlocksOverlay) || IsActive(optionsOverlay) || IsActive(creditsOverlay)
                || IsActive(historyOverlay) || IsActive(feedbackOverlay))
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
            ActivityDlc.UnlockAll();
            CacheOverlayViews();
            _unlocks?.Refresh();
            _play?.RefreshLocks();
            _roguelikeLobby?.Refresh();
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
            ActivityDlc.ClearAll();
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
            _lastSession = session;
            ShowBoard();
            if (_match == null)
                _match = FindAnyObjectByType<MatchController>();
            _match.Launch(session);
            GameAudio.PlayMatchMusic();
        }

        void ShowRoguelikeLobby()
        {
            EnsureRoguelikeLobby();
            if (_roguelikeLobby == null)
                return;
            if (_board != null)
                _board.gameObject.SetActive(false);
            if (_hud != null)
                _hud.gameObject.SetActive(false);
            _roguelikeLobby.Bind(StartRoguelikeFromLobby, ShowPlay);
            ShowOverlay(_roguelikeLobby.gameObject);
            _roguelikeLobby.Refresh();
        }

        void StartRoguelikeFromLobby()
        {
            if (!ActivityDlc.IsOwned(Activity.Roguelike))
                return;
            RoguelikeRunSettings settings = _roguelikeLobby != null
                ? _roguelikeLobby.Settings
                : new RoguelikeRunSettings();
            StartRoguelike(settings);
        }

        void StartRoguelike(RoguelikeRunSettings settings)
        {
            EnsureRoguelikeHud();
            EnsureRoguelikeShop();
            if (_roguelikeHud == null)
                return;
            if (mainMenu != null)
                DismissScreen(mainMenu, _roguelikeHud.gameObject);
            HideOverlays();
            _roguelikeLobby?.Dismiss();
            if (_board != null)
                _board.gameObject.SetActive(true);
            if (_hud != null)
                _hud.gameObject.SetActive(false);
            if (_roguelike == null)
                ResolveReferences();
            InjectRoguelikeDeps();
            _roguelikeHud.Bind(_roguelike, OpenRoguelikeOptions, ConfirmRoguelikeGiveUp);
            _roguelike.Launch(settings);
            GameAudio.PlayMatchMusic();
        }

        void OpenRoguelikeOptions()
        {
            OptionsOverlay.Ensure()?.OpenFromMatch();
        }

        void ConfirmRoguelikeGiveUp()
        {
            if (_dialogs == null)
                _dialogs = OverlayDialogs.Ensure(transform);
            _dialogs?.ShowQuit(
                () =>
                {
                    _dialogs.HideQuit();
                    _roguelike?.GiveUp();
                },
                () => _dialogs.HideQuit(),
                Loc.Get("roguelike.giveUp.confirm"));
        }

        void EnsureRoguelikeLobby()
        {
            if (_roguelikeLobby != null)
            {
                OverlayMotion.Ensure(_roguelikeLobby.gameObject);
                return;
            }
            _roguelikeLobby = FindAnyObjectByType<RoguelikeLobbyView>(FindObjectsInactive.Include);
            if (_roguelikeLobby == null)
            {
                GameObject prefab = RuntimePrefabs.RoguelikeLobby;
                if (prefab == null)
                    return;
                GameObject instance = Instantiate(prefab, transform);
                instance.name = "RoguelikeLobby";
                _roguelikeLobby = instance.GetComponent<RoguelikeLobbyView>();
            }
            if (_roguelikeLobby != null)
            {
                _roguelikeLobby.gameObject.SetActive(false);
                OverlayMotion.Ensure(_roguelikeLobby.gameObject);
            }
        }

        void EnsureRoguelikeHud()
        {
            if (_roguelikeHud != null)
            {
                OverlayMotion.Ensure(_roguelikeHud.gameObject);
                return;
            }
            _roguelikeHud = FindAnyObjectByType<RoguelikeHud>(FindObjectsInactive.Include);
            if (_roguelikeHud == null)
            {
                GameObject prefab = RuntimePrefabs.RoguelikeHud;
                if (prefab == null)
                    return;
                GameObject instance = Instantiate(prefab, transform);
                instance.name = "RoguelikeHud";
                _roguelikeHud = instance.GetComponent<RoguelikeHud>();
            }
            if (_roguelikeHud != null)
            {
                _roguelikeHud.gameObject.SetActive(false);
                OverlayMotion.Ensure(_roguelikeHud.gameObject);
            }
        }

        void EnsureRoguelikeShop()
        {
            if (_roguelikeShop != null)
            {
                OverlayMotion.Ensure(_roguelikeShop.gameObject);
                return;
            }
            _roguelikeShop = FindAnyObjectByType<RoguelikeShopView>(FindObjectsInactive.Include);
            if (_roguelikeShop == null)
            {
                GameObject prefab = RuntimePrefabs.RoguelikeShop;
                if (prefab == null)
                    return;
                GameObject instance = Instantiate(prefab, transform);
                instance.name = "RoguelikeShop";
                _roguelikeShop = instance.GetComponent<RoguelikeShopView>();
            }
            if (_roguelikeShop != null)
            {
                _roguelikeShop.gameObject.SetActive(false);
                OverlayMotion.Ensure(_roguelikeShop.gameObject);
            }
        }

        void InjectRoguelikeDeps()
        {
            if (_roguelike == null)
                return;
            _roguelike.Configure(_board, _roguelikeHud, _roguelikeShop);
        }

        void EnsureHistoryOverlay()
        {
            if (historyOverlay != null)
                return;
            GameObject prefab = RuntimePrefabs.History;
            if (prefab == null)
                return;
            historyOverlay = Instantiate(prefab, transform);
            historyOverlay.name = "History";
            historyOverlay.SetActive(false);
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
            if (_mainMenu == null && mainMenu != null)
                _mainMenu = mainMenu.GetComponent<MainMenuView>();
            _mainMenu?.RefreshLoc();
            CacheOverlayViews();
            _play?.RefreshLoc();
            _matchSettings?.RefreshLoc();
            _lobbyView?.RefreshLoc();
            _join?.RefreshLoc();
            _credits?.RefreshLoc();
            _history?.RefreshLoc();
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
            _account = accountCreationOverlay.GetComponent<AccountCreationOverlay>();
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
