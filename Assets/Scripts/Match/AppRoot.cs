using System;
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
        [SerializeField] GameObject campaignOverlay;
        [SerializeField] GameObject accountCreationOverlay;
        [SerializeField] GameObject feedbackOverlay;
        [SerializeField] MatchController match;
        [SerializeField] BoardView board;
        [SerializeField] MatchHud hud;
        [SerializeField] CampaignHud campaignHud;
        [SerializeField] CampaignLevelSet campaignLevels;

        MatchController _match;
        BoardView _board;
        MatchHudBase _activeHud;
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
        CampaignOverlay _campaign;
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
            ResolveReferences();
            BindMainMenu();
            BindOverlays();
        }

        void Start()
        {
            ResolveReferences();
            if (_match != null)
            {
                _match.LeftMatch += OnLeftMatch;
                _match.RematchRequested += OnRematchRequested;
                _match.NextCampaignRequested += OnNextCampaignRequested;
                _match.ReplayLeftToHistory += OnReplayLeftToHistory;
            }
            BindMainMenu();
            BindOverlays();
            GameAudio.Ensure();
            EnterApp();
        }

        void OnDestroy()
        {
            Loc.Changed -= OnLanguageChanged;
            if (_match != null)
            {
                _match.LeftMatch -= OnLeftMatch;
                _match.RematchRequested -= OnRematchRequested;
                _match.NextCampaignRequested -= OnNextCampaignRequested;
                _match.ReplayLeftToHistory -= OnReplayLeftToHistory;
            }
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
            _dialogs.ShowDebug(
                DebugResetSave,
                DebugUnlockAll,
                DebugWin,
                DebugLose,
                DebugResetTimer,
                DebugVsAiNone,
                DebugVsAiAllNoRandomizer,
                DebugVsAiAll,
                DebugVsAiSpecific,
                DebugJumpToLevel,
                DebugRevealFog,
                DebugMeritPlus,
                DebugMeritMinus,
                DebugUnlockAllCampaign,
                DebugClearAllCampaign,
                DebugVsAiRandomModes);
        }

        void ResolveReferences()
        {
            _match = match != null ? match : FindAnyObjectByType<MatchController>();
            _board = board != null ? board : FindAnyObjectByType<BoardView>();
            if (hud == null)
                hud = FindAnyObjectByType<MatchHud>(FindObjectsInactive.Include);
            if (campaignHud == null)
                campaignHud = FindAnyObjectByType<CampaignHud>(FindObjectsInactive.Include);
            if (campaignLevels == null)
                campaignLevels = CampaignLevelSet.Load();
            CampaignCatalog.Bind(campaignLevels != null
                ? campaignLevels.ToDefinitions()
                : System.Array.Empty<CampaignLevelDefinition>());
            _activeHud = hud != null ? hud : (MatchHudBase)campaignHud;
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
                campaignOverlay,
                accountCreationOverlay,
                feedbackOverlay
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
                ShowCampaign,
                ShowMainMenu);
            _matchSettings?.Bind(ConfirmPrep, ShowPlay);
            _lobbyView?.Bind(SitAsFriend, StartLobbyMatch, LeaveLobby);
            _join?.Bind(EnterJoinCode, ShowPlay);
            _unlocks?.Bind(ShowMainMenu);
            _credits?.Bind(ShowMainMenu);
            EnsureHistoryOverlay();
            EnsureCampaignOverlay();
            CacheOverlayViews();
            _history?.Bind(ShowMainMenu, StartHistoryReplay);
            _campaign?.Bind(ShowPlay, index => StartCampaignLevel(index));
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
            if (_campaign == null && campaignOverlay != null)
                _campaign = campaignOverlay.GetComponent<CampaignOverlay>();
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
            if (hud != null)
                hud.gameObject.SetActive(false);
            if (campaignHud != null)
                campaignHud.gameObject.SetActive(false);
        }

        void ShowBoard()
        {
            HideOverlays();
            if (_board != null)
                _board.gameObject.SetActive(true);
            if (_activeHud != null)
                _activeHud.gameObject.SetActive(true);
        }

        void SelectHud(MatchSession session)
        {
            bool campaign = session != null && session.Activity == Activity.Campaign;
            if (hud != null)
                hud.gameObject.SetActive(!campaign);
            if (campaignHud != null)
                campaignHud.gameObject.SetActive(campaign);
            _activeHud = campaign && campaignHud != null ? campaignHud : (MatchHudBase)hud;
            _match?.SetHud(_activeHud);
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
            _mainMenu?.RefreshHistoryGate();
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
        void ShowCampaign()
        {
            EnsureCampaignOverlay();
            CacheOverlayViews();
            _campaign?.Bind(ShowPlay, index => StartCampaignLevel(index));
            ShowOverlay(campaignOverlay);
            _campaign?.Refresh();
        }
        void StartCampaignLevel(int index, bool ignoreUnlock = false)
        {
            CampaignLevelDefinition level = CampaignCatalog.Get(index);
            if (level == null || (!ignoreUnlock && !CampaignProgress.IsUnlocked(index)))
                return;
            StartMatch(new MatchSession
            {
                Activity = Activity.Campaign,
                Rules = new MatchRules(level.Modes, level.ToMatchSettings()),
                PlayerSide = level.PlayerSide,
                Hotseat = false,
                CampaignLevel = level
            });
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
            _activeHud?.CloseOptionsIfOpen();
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
            if (!HistoryPrefs.Unlocked)
                return;
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
            SelectHud(null);
            ShowBoard();
            _match.SetHud(_activeHud);
            _match.LaunchReplay(record, fromHistory: true);
            GameAudio.PlayMatchMusic();
        }

        void OnReplayLeftToHistory()
        {
            ShowHistory();
            GameAudio.PlayMenuMusic();
        }

        void OnLeftMatch()
        {
            if (_lastSession != null && _lastSession.Activity == Activity.Campaign)
            {
                ShowCampaign();
                GameAudio.PlayMenuMusic();
                return;
            }
            ShowMainMenu();
        }
        void OnNextCampaignRequested()
        {
            if (_lastSession == null || _lastSession.CampaignLevel == null)
                return;
            StartCampaignLevel(_lastSession.CampaignLevel.Index + 1);
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
            if (_lastSession.Activity == Activity.Campaign && _lastSession.CampaignLevel != null)
            {
                StartCampaignLevel(_lastSession.CampaignLevel.Index);
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
            int seed = _modeSettings.MatchSeed != 0 ? _modeSettings.MatchSeed : Environment.TickCount;
            MatchSettings settings = new MatchSettings(
                TimeFromPreset(timePreset, incrementPreset),
                hostColor,
                aiStrength,
                false,
                _modeSettings.EmpowerBudget,
                _modeSettings.MartyrThreshold,
                _modeSettings.MartyrDraftOptions,
                _modeSettings.ActionPoints,
                _modeSettings.TerrainLayout,
                seed,
                _modeSettings.RandomShuffle,
                _modeSettings.RandomColors,
                _modeSettings.RandomPlacement,
                _modeSettings.TerrainOnPieces);
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
            var transport = new LocalHotseatTransport();
            transport.Host(_lobby.Code);
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
            LocalHotseatTransport.OpenHost?.Close();
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
            try
            {
                var guest = new LocalHotseatTransport();
                guest.Join(_openLobby.Code);
            }
            catch (System.InvalidOperationException)
            {
                return;
            }
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

            if (_match != null && _board != null && _board.gameObject.activeSelf)
            {
                if (_match.IsReplaying)
                {
                    _match.LeaveToMenu();
                    return;
                }
                _activeHud?.ToggleOptions();
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

            if (IsActive(joinOverlay) || IsActive(playOverlay) || IsActive(campaignOverlay))
            {
                if (IsActive(joinOverlay) || IsActive(campaignOverlay))
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
            MeritWallet.DebugFill(999);
            MeritUnlocks.UnlockAll();
            CacheOverlayViews();
            _unlocks?.Refresh();
            _mainMenu?.RefreshHistoryGate();
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
        void DebugVsAiNone()
        {
            StartDebugVersus(Array.Empty<ModeId>());
        }
        void DebugVsAiAllNoRandomizer()
        {
            StartDebugVersus(ModesExcept(ModeId.Randomizer));
        }
        void DebugVsAiAll()
        {
            StartDebugVersus(AllModeIds());
        }
        void DebugVsAiSpecific(ModeId id)
        {
            StartDebugVersus(new[] { id });
        }
        void DebugJumpToLevel(int index)
        {
            _dialogs?.HideDebugImmediate();
            StartCampaignLevel(index, true);
        }
        void DebugRevealFog()
        {
            _match?.DebugRevealFog();
        }
        void DebugMeritPlus()
        {
            MeritWallet.Add(20);
            _unlocks?.Refresh();
        }
        void DebugMeritMinus()
        {
            MeritWallet.TrySpend(20);
            _unlocks?.Refresh();
        }
        void DebugUnlockAllCampaign()
        {
            CampaignProgress.UnlockAll();
            _campaign?.Refresh();
        }
        void DebugClearAllCampaign()
        {
            CampaignProgress.Clear();
            _campaign?.Refresh();
        }
        void DebugVsAiRandomModes()
        {
            StartDebugVersus(RandomModeIds());
        }
        void StartDebugVersus(ModeId[] modes)
        {
            _dialogs?.HideDebugImmediate();
            MatchSettings settings = RandomVersusSettings();
            StartMatch(new MatchSession
            {
                Activity = Activity.VersusAi,
                Rules = new MatchRules(modes, settings),
                PlayerSide = ResolveColor(settings.HostColor),
                Hotseat = false
            });
        }
        static MatchSettings RandomVersusSettings()
        {
            var host = (HostColor)UnityEngine.Random.Range(0, 3);
            var ai = (AiStrength)UnityEngine.Random.Range(0, 3);
            int seed = UnityEngine.Random.Range(1, int.MaxValue);
            int actionPoints = UnityEngine.Random.Range(MatchSettings.MinActionPoints, MatchSettings.MaxActionPoints + 1);
            var layout = (TerrainLayoutKind)UnityEngine.Random.Range(0, 5);
            int empower = UnityEngine.Random.Range(EmpoweredPowers.MinBudget, EmpoweredPowers.MaxBudget + 1);
            int martyr = UnityEngine.Random.Range(1, 16);
            int draft = UnityEngine.Random.Range(1, 6);
            bool shuffle = UnityEngine.Random.value >= 0.5f;
            bool colors = UnityEngine.Random.value >= 0.5f;
            bool place = UnityEngine.Random.value >= 0.5f;
            bool onPieces = UnityEngine.Random.value >= 0.5f;
            return new MatchSettings(
                TimeControl.None,
                host,
                ai,
                false,
                empower,
                martyr,
                draft,
                actionPoints,
                layout,
                seed,
                shuffle,
                colors,
                place,
                onPieces);
        }
        static ModeId[] AllModeIds()
        {
            var ids = new ModeId[ModeCatalog.All.Length];
            for (int i = 0; i < ModeCatalog.All.Length; i++)
                ids[i] = ModeCatalog.All[i].Id;
            return ids;
        }
        static ModeId[] RandomModeIds()
        {
            ModeDefinition[] all = ModeCatalog.All;
            var picked = new List<ModeId>(all.Length);
            for (int i = 0; i < all.Length; i++)
            {
                if (UnityEngine.Random.value < 0.5f)
                    picked.Add(all[i].Id);
            }
            if (picked.Count == 0 && all.Length > 0)
                picked.Add(all[UnityEngine.Random.Range(0, all.Length)].Id);
            return picked.ToArray();
        }
        static ModeId[] ModesExcept(ModeId skip)
        {
            var ids = new List<ModeId>(ModeCatalog.All.Length);
            for (int i = 0; i < ModeCatalog.All.Length; i++)
            {
                if (ModeCatalog.All[i].Id != skip)
                    ids.Add(ModeCatalog.All[i].Id);
            }
            return ids.ToArray();
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
            _lastSession = session;
            SelectHud(session);
            ShowBoard();
            if (_match == null)
                _match = FindAnyObjectByType<MatchController>();
            _match?.SetHud(_activeHud);
            _match.Launch(session);
            GameAudio.PlayMatchMusic();
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
        void EnsureCampaignOverlay()
        {
            if (campaignOverlay != null)
                return;
            GameObject prefab = RuntimePrefabs.Campaign;
            if (prefab == null)
                return;
            campaignOverlay = Instantiate(prefab, transform);
            campaignOverlay.name = "Campaign";
            campaignOverlay.SetActive(false);
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
                    return UnityEngine.Random.value < 0.5f ? Side.White : Side.Black;
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
