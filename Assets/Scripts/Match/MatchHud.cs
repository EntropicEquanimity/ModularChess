using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class MatchHud : MonoBehaviour
    {
        #region Fields
        const float OptionsDuration = 0.28f;
        const float StatusFadeIn = 0.28f;
        const float StatusFadeOut = 0.5f;
        const float GameOverFade = 2.5f;
        const float DraftDescHeight = 128f;
        const float DraftConfirmHeight = 40f;
        static readonly Color CheckColor = new Color(0.7f, 0.1f, 0.1f, 1f);
        [SerializeField] TMP_Text turnText;
        [SerializeField] TMP_Text moveListText;
        [SerializeField] TMP_Text gameOverText;
        [SerializeField] GameObject gameOverBanner;
        [SerializeField] TMP_Text statusLine;
        [SerializeField] TMP_Text clockText;
        [SerializeField] TMP_Text opponentClock;
        [SerializeField] TMP_Text lostMaterialText;
        [SerializeField] TMP_Text playerName;
        [SerializeField] TMP_Text opponentName;
        [SerializeField] Button endTurnButton;
        [SerializeField] Button pauseButton;
        [SerializeField] Button resignButton;
        [SerializeField] Button leaveButton;
        [SerializeField] Button setupConfirmButton;
        [SerializeField] Button optionsButton;
        [SerializeField] RectTransform buttonGroup;
        [SerializeField] Transform draftRow;
        [SerializeField] RectTransform draftDescription;
        [SerializeField] PieceDetailsPanel pieceDetails;
        [SerializeField] GameObject statusRoot;
        [SerializeField] GameObject matchChrome;
        Tween _draftTween;
        Tween _optionsTween;
        Tween _statusTween;
        Tween _gameOverTween;
        Tween _endTurnTween;
        bool _endTurnShown;
        Vector2 _endTurnRest = new Vector2(-8f, 8f);
        Action<MartyrPower> _onDraft;
        Button _draftConfirm;
        RectTransform _draftDescClip;
        RectTransform _draftDescBox;
        TMP_Text _draftDescText;
        MartyrPower _previewPower;
        int _previewIndex = -1;
        bool _wired;
        bool _optionsOpen;
        bool _statusVisible;
        bool _inCheck;
        bool _gameOverShown;
        bool _deferGameOver;
        string _statusOverride = string.Empty;
        Color _statusColor = Color.white;
        CanvasGroup _statusGroup;
        CanvasGroup _gameOverGroup;
        ScrollRect _moveListScroll;
        LayoutElement _moveListLayout;
        float _optionsRestY;
        public bool OptionsOpen => _optionsOpen;
        #endregion

        #region Unity
        void Awake()
        {
            Wire();
        }
        void OnEnable()
        {
            Wire();
            HideTransient();
        }
        void OnDisable()
        {
            KillTweens();
            HideOptionsImmediate();
        }
        void OnDestroy()
        {
            KillTweens();
        }
        #endregion

        #region Public Methods
        public void Bind(GameState state, IReadOnlyList<Move> moves)
        {
            Wire();
            if (state == null)
            {
                return;
            }

            bool inProgress = state.Status == GameStatus.InProgress;
            if (turnText != null)
            {
                turnText.text = inProgress
                    ? Loc.Format("match.turn", Loc.SideName(state.SideToMove))
                    : Loc.Get("match.over");
            }

            _inCheck = inProgress && state.IsInCheck;
            ApplyStatus();

            bool showMoves = PlayerPrefs.GetInt("ShowNotation", 1) == 1;
            SetMoveList(showMoves ? FormatMoveList(moves) : string.Empty);

            string result = FormatResult(state);
            if (result.Length > 0)
            {
                if (gameOverText != null)
                {
                    gameOverText.text = result;
                }

                if (!_deferGameOver)
                {
                    ShowGameOver();
                }
            }
            else
            {
                HideGameOverImmediate();
            }
        }
        public void BindActions(MatchController controller)
        {
            Wire();
            BindClick(endTurnButton, controller.RequestEndTurn);
            BindClick(pauseButton, controller.TogglePause);
            BindClick(resignButton, controller.Resign);
            BindClick(leaveButton, controller.LeaveToMenu);
            BindClick(setupConfirmButton, controller.ConfirmSetup);
            BindClick(optionsButton, ToggleOptions);
        }
        public void ToggleOptions()
        {
            if (_optionsOpen)
            {
                CloseOptions();
            }
            else
            {
                OpenOptions();
            }
        }
        public bool CloseOptionsIfOpen()
        {
            if (!_optionsOpen)
            {
                return false;
            }

            CloseOptions();
            return true;
        }
        public void SetClock(MatchClock clock, Side playerSide)
        {
            Wire();
            bool timed = clock != null && !clock.IsNone;
            string player = timed ? clock.Format(playerSide) : string.Empty;
            string opponent = timed ? clock.Format(playerSide.Opponent()) : string.Empty;
            ApplyClock(clockText, player, timed);
            ApplyClock(opponentClock, opponent, timed);
        }
        public void SetNames(MatchSession session)
        {
            Wire();
            if (playerName != null)
            {
                playerName.text = PlayerIdentity.DisplayName;
            }

            if (opponentName != null)
            {
                opponentName.text = OpponentLabel(session);
            }
        }
        public void SetMatchChromeVisible(bool visible)
        {
            Wire();
            if (matchChrome != null)
            {
                matchChrome.SetActive(visible);
                return;
            }
            if (playerName != null && playerName.transform.parent != null)
            {
                playerName.transform.parent.gameObject.SetActive(visible);
                return;
            }
            if (playerName != null)
            {
                playerName.gameObject.SetActive(visible);
            }
            if (opponentName != null)
            {
                opponentName.gameObject.SetActive(visible);
            }
        }
        public void SetDeferGameOver(bool defer)
        {
            _deferGameOver = defer;
            if (!defer)
            {
                return;
            }
            HideGameOverImmediate();
        }
        public void RevealGameOver()
        {
            _deferGameOver = false;
            ShowGameOver();
        }
        public void SetEndTurnVisible(bool visible)
        {
            Wire();
            if (endTurnButton == null) return;
            PlaceEndTurn();
            if (visible == _endTurnShown && endTurnButton.gameObject.activeSelf == visible) return;
            _endTurnTween?.Kill();
            RectTransform rect = endTurnButton.transform as RectTransform;
            _endTurnShown = visible;
            if (visible)
            {
                endTurnButton.gameObject.SetActive(true);
                endTurnButton.transform.SetAsLastSibling();
                if (rect != null)
                {
                    rect.anchoredPosition = HiddenEndTurnPos();
                    _endTurnTween = DOTween.To(
                            () => rect.anchoredPosition,
                            v => rect.anchoredPosition = v,
                            _endTurnRest,
                            OptionsDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true)
                        .SetTarget(endTurnButton);
                }
                return;
            }
            if (rect == null || !endTurnButton.gameObject.activeSelf)
            {
                HideEndTurnImmediate();
                return;
            }
            _endTurnTween = DOTween.To(
                    () => rect.anchoredPosition,
                    v => rect.anchoredPosition = v,
                    HiddenEndTurnPos(),
                    OptionsDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetTarget(endTurnButton)
                .OnComplete(HideEndTurnImmediate);
        }
        public void SetPauseVisible(bool visible)
        {
            SetActive(pauseButton, visible);
        }
        public void SetResignVisible(bool visible)
        {
            SetActive(resignButton, visible);
        }
        public void SetSetupConfirm(bool visible, bool interactable, bool opponentReady)
        {
            Wire();
            if (setupConfirmButton == null) return;
            PlaceSetupConfirm();
            SetActive(setupConfirmButton, visible);
            setupConfirmButton.interactable = visible && interactable;
            LocalizedText.Bind(
                setupConfirmButton,
                opponentReady ? "hud.setupConfirm.ready" : "hud.setupConfirm");
        }
        public void SetLostMaterial(int? white, int? black, int threshold)
        {
            Wire();
            if (lostMaterialText == null)
            {
                return;
            }

            if (white == null || black == null)
            {
                lostMaterialText.text = string.Empty;
                return;
            }

            lostMaterialText.text = Loc.Format("match.lost", white.Value, black.Value, threshold);
        }
        public void SetStatusLine(string text)
        {
            Wire();
            _statusOverride = text ?? string.Empty;
            ApplyStatus();
        }
        public void ShowDraft(GameState state, Action<MartyrPower> onPick)
        {
            Wire();
            _onDraft = onPick;
            if (draftRow == null || state?.Runtime.PendingDraft == null)
            {
                return;
            }

            draftRow.gameObject.SetActive(true);
            draftRow.SetAsLastSibling();
            DraftOffer offer = state.Runtime.PendingDraft.Value;
            PieceType? battlefield = state.Runtime.PendingBattlefieldType ?? offer.BattlefieldType;
            for (int i = 0; i < draftRow.childCount; i++)
            {
                Transform child = draftRow.GetChild(i);
                if (i < offer.Count)
                {
                    child.gameObject.SetActive(true);
                    SetDraftButton(i, offer.At(i), battlefield);
                }
                else
                {
                    child.gameObject.SetActive(false);
                }
            }
        }
        public void HideDraft()
        {
            HideDraftInspect();
            if (draftRow != null)
            {
                draftRow.gameObject.SetActive(false);
            }

            if (draftDescription != null)
            {
                draftDescription.gameObject.SetActive(false);
            }
        }
        public void ShowPieceDetails(Piece piece, GameState state, IReadOnlyCollection<Guid> pendingEmpowered)
        {
            Wire();
            if (pieceDetails == null)
            {
                return;
            }

            pieceDetails.Show(piece, state, pendingEmpowered);
        }
        public void HidePieceDetails()
        {
            pieceDetails?.Hide();
        }
        #endregion

        #region Private Methods
        void Wire()
        {
            if (_wired)
            {
                return;
            }

            _wired = true;
            if (turnText == null)
            {
                turnText = FindLabel("TurnLabel");
            }

            Transform check = FindChild(transform, "CheckLabel");
            if (check != null)
            {
                check.gameObject.SetActive(false);
            }

            if (moveListText == null)
            {
                moveListText = FindLabel("MoveList");
            }

            if (gameOverBanner == null)
            {
                Transform banner = FindChild(transform, "GameOverBanner");
                if (banner != null)
                {
                    gameOverBanner = banner.gameObject;
                }
            }

            if (gameOverText == null && gameOverBanner != null)
            {
                gameOverText = gameOverBanner.GetComponentInChildren<TMP_Text>(true);
            }

            if (statusLine == null)
            {
                statusLine = FindLabel("StatusLine");
            }

            if (statusLine != null)
            {
                _statusColor = statusLine.color;
            }

            if (statusRoot == null)
            {
                Transform status = FindChild(transform, "Status");
                if (status != null)
                {
                    statusRoot = status.gameObject;
                }
                else if (statusLine != null)
                {
                    statusRoot = statusLine.transform.parent != null
                        ? statusLine.transform.parent.gameObject
                        : statusLine.gameObject;
                }
            }

            if (clockText == null)
            {
                clockText = FindClockIn("PlayerName");
            }

            if (opponentClock == null)
            {
                opponentClock = FindClockIn("OpponentName");
            }

            if (lostMaterialText == null)
            {
                lostMaterialText = FindLabel("LostMaterial");
            }

            if (playerName == null)
            {
                playerName = FindLabelIn("PlayerName");
            }

            if (opponentName == null)
            {
                opponentName = FindLabelIn("OpponentName");
            }

            if (endTurnButton == null)
            {
                endTurnButton = FindButton("End Turn");
            }

            if (pauseButton == null)
            {
                pauseButton = FindButton("Pause");
            }

            if (resignButton == null)
            {
                resignButton = FindButton("Resign");
            }

            if (leaveButton == null)
            {
                leaveButton = FindButton("Leave");
            }

            if (setupConfirmButton == null)
            {
                setupConfirmButton = FindButton("Confirm Setup");
            }

            if (optionsButton == null)
            {
                optionsButton = FindButton("OptionsButton");
            }

            if (buttonGroup == null)
            {
                Transform group = FindChild(transform, "ButtonGroup");
                if (group != null)
                {
                    buttonGroup = group as RectTransform;
                }
            }

            if (draftRow == null)
            {
                draftRow = FindChild(transform, "DraftRow");
            }

            if (draftDescription == null)
            {
                Transform box = FindChild(transform, "DescriptionBox");
                if (box != null)
                {
                    draftDescription = box as RectTransform;
                }
            }

            if (pieceDetails == null)
            {
                pieceDetails = GetComponentInChildren<PieceDetailsPanel>(true);
            }

            if (matchChrome == null)
            {
                Transform names = FindChild(transform, "PlayerNames");
                if (names != null)
                {
                    matchChrome = names.gameObject;
                }
            }

            if (buttonGroup != null)
            {
                _optionsRestY = buttonGroup.anchoredPosition.y;
            }

            if (statusRoot != null)
            {
                _statusGroup = statusRoot.GetComponent<CanvasGroup>();
                if (_statusGroup == null)
                {
                    _statusGroup = statusRoot.AddComponent<CanvasGroup>();
                }

                _statusGroup.blocksRaycasts = false;
                _statusGroup.interactable = false;
            }

            if (gameOverBanner != null)
            {
                _gameOverGroup = gameOverBanner.GetComponent<CanvasGroup>();
                if (_gameOverGroup == null)
                {
                    _gameOverGroup = gameOverBanner.AddComponent<CanvasGroup>();
                }
            }

            WrapMoveList();
            if (moveListText != null)
            {
                moveListText.overflowMode = TextOverflowModes.Overflow;
                moveListText.extraPadding = false;
            }

            BindHudLabels();
        }
        void BindHudLabels()
        {
            LocalizedText.Bind(endTurnButton, "hud.endTurn");
            LocalizedText.Bind(pauseButton, "hud.pause");
            LocalizedText.Bind(resignButton, "hud.resign");
            LocalizedText.Bind(leaveButton, "hud.leave");
            LocalizedText.Bind(setupConfirmButton, "hud.setupConfirm");
        }
        void HideTransient()
        {
            HideOptionsImmediate();
            HideDraft();
            HidePieceDetails();
            _deferGameOver = false;
            HideGameOverImmediate();
            SetMatchChromeVisible(true);
            SetSetupConfirm(false, false, false);
            HideEndTurnImmediate();
            _inCheck = false;
            _statusOverride = string.Empty;
            if (statusRoot != null)
            {
                SetGroupAlpha(_statusGroup, 0f);
                statusRoot.SetActive(false);
            }

            _statusVisible = false;
            if (statusLine != null)
            {
                statusLine.text = string.Empty;
            }
        }
        void WrapMoveList()
        {
            if (moveListText == null || _moveListScroll != null)
            {
                return;
            }

            GameObject prefab = RuntimePrefabs.ScrollView;
            if (prefab == null)
            {
                return;
            }

            RectTransform listRect = moveListText.rectTransform;
            Transform parent = listRect.parent;
            int sibling = listRect.GetSiblingIndex();
            GameObject scrollGo = Instantiate(prefab, parent);
            scrollGo.name = "MoveListScroll";
            var scrollRect = scrollGo.GetComponent<RectTransform>();
            CopyRect(listRect, scrollRect);
            scrollGo.transform.SetSiblingIndex(sibling);

            _moveListScroll = scrollGo.GetComponent<ScrollRect>();
            if (_moveListScroll != null)
            {
                _moveListScroll.horizontal = false;
                _moveListScroll.vertical = true;
            }

            var backdrop = scrollGo.GetComponent<Image>();
            if (backdrop != null)
            {
                Color color = backdrop.color;
                color.a = 0f;
                backdrop.color = color;
            }

            Transform content = FindChild(scrollGo.transform, "Content");
            if (content == null && _moveListScroll != null)
            {
                content = _moveListScroll.content;
            }

            if (content == null)
            {
                return;
            }

            var layout = content.GetComponent<VerticalLayoutGroup>();
            if (layout != null)
            {
                layout.enabled = false;
            }

            var fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter != null)
            {
                fitter.enabled = false;
            }

            listRect.SetParent(content, false);
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchoredPosition = Vector2.zero;
            listRect.sizeDelta = new Vector2(0f, 0f);
            moveListText.overflowMode = TextOverflowModes.Overflow;
            moveListText.extraPadding = false;
            moveListText.raycastTarget = false;
            _moveListLayout = listRect.GetComponent<LayoutElement>();
            if (_moveListLayout == null)
            {
                _moveListLayout = listRect.gameObject.AddComponent<LayoutElement>();
            }
        }
        void SetMoveList(string text)
        {
            if (moveListText == null)
            {
                return;
            }

            bool show = !string.IsNullOrEmpty(text);
            if (_moveListScroll != null)
            {
                _moveListScroll.gameObject.SetActive(show);
            }
            else
            {
                moveListText.gameObject.SetActive(show);
            }

            moveListText.text = text ?? string.Empty;
            if (!show)
            {
                return;
            }

            moveListText.overflowMode = TextOverflowModes.Overflow;
            moveListText.ForceMeshUpdate();
            float height = Mathf.Max(moveListText.preferredHeight, moveListText.fontSize);
            if (_moveListLayout != null)
            {
                _moveListLayout.minHeight = height;
                _moveListLayout.preferredHeight = height;
            }

            RectTransform listRect = moveListText.rectTransform;
            listRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
            if (_moveListScroll != null && _moveListScroll.content != null)
            {
                RectTransform content = _moveListScroll.content;
                content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
                LayoutRebuilder.ForceRebuildLayoutImmediate(content);
                _moveListScroll.verticalNormalizedPosition = 0f;
            }
        }
        void OpenOptions()
        {
            Wire();
            if (buttonGroup == null)
            {
                return;
            }

            _optionsTween?.Kill();
            _optionsOpen = true;
            buttonGroup.anchoredPosition = HiddenOptionsPos();
            buttonGroup.gameObject.SetActive(true);
            RaiseOptionsChrome();
            _optionsTween = DOTween.To(
                    () => buttonGroup.anchoredPosition,
                    v => buttonGroup.anchoredPosition = v,
                    ShownOptionsPos(),
                    OptionsDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(buttonGroup);
        }
        void CloseOptions()
        {
            if (buttonGroup == null)
            {
                _optionsOpen = false;
                return;
            }

            _optionsOpen = false;
            _optionsTween?.Kill();
            _optionsTween = DOTween.To(
                    () => buttonGroup.anchoredPosition,
                    v => buttonGroup.anchoredPosition = v,
                    HiddenOptionsPos(),
                    OptionsDuration)
                .SetEase(Ease.InCubic)
                .SetUpdate(true)
                .SetTarget(buttonGroup)
                .OnComplete(() =>
                {
                    if (!_optionsOpen && buttonGroup != null)
                    {
                        buttonGroup.gameObject.SetActive(false);
                    }
                });
        }
        void HideOptionsImmediate()
        {
            _optionsTween?.Kill();
            _optionsOpen = false;
            if (buttonGroup == null)
            {
                return;
            }

            buttonGroup.anchoredPosition = HiddenOptionsPos();
            buttonGroup.gameObject.SetActive(false);
        }
        void RaiseOptionsChrome()
        {
            if (buttonGroup != null)
            {
                buttonGroup.SetAsLastSibling();
            }

            if (optionsButton != null)
            {
                optionsButton.transform.SetAsLastSibling();
            }
        }
        Vector2 ShownOptionsPos()
        {
            return new Vector2(0f, _optionsRestY);
        }
        Vector2 HiddenOptionsPos()
        {
            float width = 200f;
            if (buttonGroup != null)
            {
                width = Mathf.Max(buttonGroup.sizeDelta.x, 200f);
            }

            return new Vector2(width, _optionsRestY);
        }
        void ApplyStatus()
        {
            if (statusLine == null)
            {
                return;
            }

            if (_statusOverride.Length > 0)
            {
                statusLine.color = _statusColor;
                statusLine.text = _statusOverride;
                ShowStatus();
                return;
            }

            if (_inCheck)
            {
                statusLine.color = CheckColor;
                statusLine.text = Loc.Get("match.check");
                ShowStatus();
                return;
            }

            statusLine.color = _statusColor;
            statusLine.text = string.Empty;
            HideStatus();
        }
        void ShowStatus()
        {
            if (statusRoot == null)
            {
                return;
            }

            statusRoot.SetActive(true);
            if (_statusVisible)
            {
                return;
            }

            _statusVisible = true;
            _statusTween?.Kill();
            _statusTween = DOTween.To(
                    () => GroupAlpha(_statusGroup),
                    a => SetGroupAlpha(_statusGroup, a),
                    1f,
                    StatusFadeIn)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(statusRoot);
        }
        void HideStatus()
        {
            if (!_statusVisible && (statusRoot == null || !statusRoot.activeSelf))
            {
                return;
            }

            _statusVisible = false;
            if (statusRoot == null)
            {
                return;
            }

            _statusTween?.Kill();
            _statusTween = DOTween.To(
                    () => GroupAlpha(_statusGroup),
                    a => SetGroupAlpha(_statusGroup, a),
                    0f,
                    StatusFadeOut)
                .SetEase(Ease.InQuad)
                .SetUpdate(true)
                .SetTarget(statusRoot)
                .OnComplete(() =>
                {
                    if (statusRoot != null)
                    {
                        statusRoot.SetActive(false);
                    }
                });
        }
        void ShowGameOver()
        {
            if (gameOverBanner == null)
            {
                return;
            }

            if (_gameOverShown)
            {
                return;
            }

            _gameOverShown = true;
            gameOverBanner.SetActive(true);
            gameOverBanner.transform.SetAsLastSibling();
            if (_optionsOpen)
            {
                RaiseOptionsChrome();
            }
            else if (optionsButton != null)
            {
                optionsButton.transform.SetAsLastSibling();
            }

            _gameOverTween?.Kill();
            if (_gameOverGroup != null)
            {
                _gameOverGroup.alpha = 0f;
                _gameOverGroup.blocksRaycasts = true;
                _gameOverGroup.interactable = true;
            }

            _gameOverTween = DOTween.To(
                    () => GroupAlpha(_gameOverGroup),
                    a =>
                    {
                        if (_gameOverGroup != null)
                        {
                            _gameOverGroup.alpha = a;
                        }
                    },
                    1f,
                    GameOverFade)
                .SetEase(Ease.OutQuad)
                .SetUpdate(true)
                .SetTarget(gameOverBanner);
        }
        void HideGameOverImmediate()
        {
            _gameOverTween?.Kill();
            _gameOverShown = false;
            if (_gameOverGroup != null)
            {
                _gameOverGroup.alpha = 0f;
                _gameOverGroup.blocksRaycasts = false;
                _gameOverGroup.interactable = false;
            }

            if (gameOverBanner != null)
            {
                gameOverBanner.SetActive(false);
            }
        }
        void SetDraftButton(int index, MartyrPower power, PieceType? battlefield)
        {
            if (draftRow == null || index >= draftRow.childCount)
            {
                return;
            }

            Transform child = draftRow.GetChild(index);
            var button = child.GetComponent<Button>();
            TMP_Text label = child.GetComponentInChildren<TMP_Text>();
            if (label != null)
            {
                label.text = FormatPower(power);
            }

            if (button == null)
            {
                return;
            }

            button.onClick.RemoveAllListeners();
            EventTrigger trigger = button.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = button.gameObject.AddComponent<EventTrigger>();
            }

            trigger.triggers.Clear();
            MartyrPower captured = power;
            int capturedIndex = index;
            PieceType? capturedType = battlefield;
            button.onClick.AddListener(() =>
            {
                GameAudio.PlayUi();
                PreviewDraft(capturedIndex, captured, capturedType, child);
            });
            AddPointer(trigger, EventTriggerType.PointerEnter, () => PreviewDraft(capturedIndex, captured, capturedType, child));
        }
        void PreviewDraft(int index, MartyrPower power, PieceType? battlefield, Transform host)
        {
            EnsureDraftInspect();
            if (_draftConfirm == null || host == null)
            {
                return;
            }

            bool same = _previewIndex == index && _draftConfirm.gameObject.activeSelf;
            _previewIndex = index;
            _previewPower = power;
            if (draftRow != null)
            {
                draftRow.SetAsLastSibling();
            }
            PlaceDraftInspect(host);
            if (_draftDescText != null)
            {
                _draftDescText.text = DescribePower(power, battlefield);
            }

            _draftConfirm.gameObject.SetActive(true);
            if (_draftDescClip != null)
            {
                _draftDescClip.gameObject.SetActive(true);
            }

            if (same)
            {
                return;
            }

            SlideDraftDescription();
        }
        void ConfirmDraft()
        {
            if (_previewIndex < 0)
            {
                return;
            }

            _onDraft?.Invoke(_previewPower);
        }
        void EnsureDraftInspect()
        {
            if (_draftConfirm != null)
            {
                return;
            }

            _draftConfirm = UiFactory.Button(transform, Loc.Get("martyr.confirm"), ConfirmDraft, new Vector2(180f, DraftConfirmHeight));
            LocalizedText.Bind(_draftConfirm, "martyr.confirm");
            IgnoreLayout(_draftConfirm.transform);
            _draftConfirm.gameObject.SetActive(false);

            GameObject clipPrefab = RuntimePrefabs.Panel;
            GameObject clipGo = clipPrefab != null
                ? Instantiate(clipPrefab, _draftConfirm.transform)
                : new GameObject("DraftDescClip", typeof(RectTransform));
            clipGo.name = "DraftDescClip";
            clipGo.transform.SetParent(_draftConfirm.transform, false);
            if (clipGo.GetComponent<RectMask2D>() == null)
            {
                clipGo.AddComponent<RectMask2D>();
            }

            var clipImage = clipGo.GetComponent<Image>();
            if (clipImage != null)
            {
                Color color = clipImage.color;
                color.a = 0f;
                clipImage.color = color;
                clipImage.raycastTarget = false;
            }

            _draftDescClip = clipGo.GetComponent<RectTransform>();
            IgnoreLayout(_draftDescClip);
            _draftDescClip.anchorMin = new Vector2(0f, 1f);
            _draftDescClip.anchorMax = new Vector2(1f, 1f);
            _draftDescClip.pivot = new Vector2(0.5f, 0f);
            _draftDescClip.anchoredPosition = Vector2.zero;
            _draftDescClip.sizeDelta = new Vector2(0f, DraftDescHeight);

            GameObject boxPrefab = RuntimePrefabs.DescriptionBox;
            if (boxPrefab == null)
            {
                return;
            }

            GameObject boxGo = Instantiate(boxPrefab, _draftDescClip);
            boxGo.name = "DescriptionBox";
            _draftDescBox = boxGo.GetComponent<RectTransform>();
            _draftDescBox.anchorMin = new Vector2(0f, 0f);
            _draftDescBox.anchorMax = new Vector2(1f, 1f);
            _draftDescBox.pivot = new Vector2(0.5f, 0f);
            _draftDescBox.offsetMin = Vector2.zero;
            _draftDescBox.offsetMax = Vector2.zero;
            _draftDescText = boxGo.GetComponentInChildren<TMP_Text>(true);
            if (_draftDescText != null)
            {
                _draftDescText.fontSize = 16;
                _draftDescText.overflowMode = TextOverflowModes.Overflow;
                _draftDescText.textWrappingMode = TextWrappingModes.Normal;
                _draftDescText.alignment = TextAlignmentOptions.Top;
            }
        }
        void PlaceDraftInspect(Transform host)
        {
            var confirmRect = _draftConfirm.transform as RectTransform;
            confirmRect.SetParent(host, false);
            confirmRect.anchorMin = new Vector2(0f, 1f);
            confirmRect.anchorMax = new Vector2(1f, 1f);
            confirmRect.pivot = new Vector2(0.5f, 0f);
            confirmRect.anchoredPosition = new Vector2(0f, 4f);
            confirmRect.sizeDelta = new Vector2(0f, DraftConfirmHeight);
            confirmRect.SetAsLastSibling();
        }
        void SlideDraftDescription()
        {
            if (_draftDescBox == null)
            {
                return;
            }

            _draftTween?.Kill();
            _draftDescBox.anchoredPosition = HiddenDraftDescPos();
            _draftTween = DOTween.To(
                    () => _draftDescBox.anchoredPosition,
                    v => _draftDescBox.anchoredPosition = v,
                    ShownDraftDescPos(),
                    OptionsDuration)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(_draftDescBox);
        }
        void HideDraftInspect()
        {
            _draftTween?.Kill();
            _previewIndex = -1;
            if (_draftConfirm != null)
            {
                _draftConfirm.gameObject.SetActive(false);
            }

            if (_draftDescClip != null)
            {
                _draftDescClip.gameObject.SetActive(false);
            }
        }
        static Vector2 ShownDraftDescPos()
        {
            return Vector2.zero;
        }
        static Vector2 HiddenDraftDescPos()
        {
            return new Vector2(0f, -DraftDescHeight);
        }
        static void AddPointer(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }
        static void IgnoreLayout(Transform target)
        {
            if (target == null)
            {
                return;
            }

            var element = target.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = target.gameObject.AddComponent<LayoutElement>();
            }

            element.ignoreLayout = true;
        }
        static string DescribePower(MartyrPower power, PieceType? battlefield)
        {
            switch (power)
            {
                case MartyrPower.Reinforcements:
                    return Loc.Get("martyr.desc.reinforcements");
                case MartyrPower.FleetPawns:
                    return Loc.Get("martyr.desc.fleet");
                case MartyrPower.Bombard:
                    return Loc.Get("martyr.desc.bombard");
                case MartyrPower.UntouchableKing:
                    return Loc.Get("martyr.desc.untouchable");
                case MartyrPower.StasisField:
                    return Loc.Get("martyr.desc.stasis");
                case MartyrPower.KnightAscension:
                    return Loc.Get("martyr.desc.ascension");
                case MartyrPower.BattlefieldPromotion:
                    return Loc.Format("martyr.desc.battlefield", Loc.PieceName(battlefield ?? PieceType.Knight));
                case MartyrPower.Rally:
                    return Loc.Get("martyr.desc.rally");
                case MartyrPower.Revival:
                    return Loc.Get("martyr.desc.revival");
                case MartyrPower.Exile:
                    return Loc.Get("martyr.desc.exile");
                case MartyrPower.Phalanx:
                    return Loc.Get("martyr.desc.phalanx");
                default:
                    throw new ArgumentOutOfRangeException(nameof(power), power, null);
            }
        }
        void KillTweens()
        {
            _draftTween?.Kill();
            _optionsTween?.Kill();
            _statusTween?.Kill();
            _gameOverTween?.Kill();
            _endTurnTween?.Kill();
        }
        TMP_Text FindClockIn(string rowName)
        {
            Transform row = FindChild(transform, rowName);
            if (row == null)
            {
                return null;
            }

            Transform labeled = FindChild(row, "ClockText");
            if (labeled != null)
            {
                TMP_Text tmp = labeled.GetComponent<TMP_Text>();
                if (tmp != null)
                {
                    return tmp;
                }
            }

            return null;
        }
        static void ApplyClock(TMP_Text label, string text, bool visible)
        {
            if (label == null) return;
            label.text = text;
            label.gameObject.SetActive(visible);
        }
        TMP_Text FindLabel(string name)
        {
            Transform child = FindChild(transform, name);
            return child != null ? child.GetComponent<TMP_Text>() : null;
        }
        TMP_Text FindLabelIn(string name)
        {
            Transform child = FindChild(transform, name);
            if (child == null)
            {
                return null;
            }

            TMP_Text tmp = child.GetComponent<TMP_Text>();
            return tmp != null ? tmp : child.GetComponentInChildren<TMP_Text>(true);
        }
        static string OpponentLabel(MatchSession session)
        {
            if (session == null || !session.IsAi || session.Rules == null)
            {
                return Loc.Get("hud.friend");
            }

            string difficulty;
            switch (session.Rules.Settings.AiStrength)
            {
                case AiStrength.Easy:
                    difficulty = Loc.Get("settings.ai.easy");
                    break;
                case AiStrength.Hard:
                    difficulty = Loc.Get("settings.ai.hard");
                    break;
                default:
                    difficulty = Loc.Get("settings.ai.medium");
                    break;
            }

            return Loc.Format("hud.bot", difficulty);
        }
        Button FindButton(string name)
        {
            Transform child = FindChild(transform, name);
            return child != null ? child.GetComponent<Button>() : null;
        }
        static void BindClick(Button button, UnityEngine.Events.UnityAction action)
        {
            GameAudio.Bind(button, action);
        }
        static void SetActive(Button button, bool visible)
        {
            if (button != null)
            {
                button.gameObject.SetActive(visible);
            }
        }
        void PlaceSetupConfirm()
        {
            RectTransform rect = setupConfirmButton.transform as RectTransform;
            if (rect == null) return;
            if (rect.parent != transform)
            {
                rect.SetParent(transform, false);
            }
            rect.anchorMin = new Vector2(1f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-8f, -70f);
            rect.sizeDelta = new Vector2(220f, 32f);
            rect.SetAsLastSibling();
        }
        void PlaceEndTurn()
        {
            RectTransform rect = endTurnButton.transform as RectTransform;
            if (rect == null) return;
            if (rect.parent != transform)
            {
                rect.SetParent(transform, false);
            }
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(1f, 0f);
            rect.sizeDelta = new Vector2(200f, 32f);
            _endTurnRest = new Vector2(-8f, 8f);
        }
        Vector2 HiddenEndTurnPos()
        {
            return new Vector2(_endTurnRest.x + 220f, _endTurnRest.y);
        }
        void HideEndTurnImmediate()
        {
            _endTurnTween?.Kill();
            _endTurnShown = false;
            if (endTurnButton != null)
            {
                endTurnButton.gameObject.SetActive(false);
            }
        }
        static void CopyRect(RectTransform from, RectTransform to)
        {
            if (from == null || to == null)
            {
                return;
            }

            to.anchorMin = from.anchorMin;
            to.anchorMax = from.anchorMax;
            to.pivot = from.pivot;
            to.anchoredPosition = from.anchoredPosition;
            to.sizeDelta = from.sizeDelta;
        }
        static float GroupAlpha(CanvasGroup group)
        {
            return group != null ? group.alpha : 1f;
        }
        static void SetGroupAlpha(CanvasGroup group, float alpha)
        {
            if (group != null)
            {
                group.alpha = alpha;
            }
        }
        static Transform FindChild(Transform root, string name)
        {
            if (root == null)
            {
                return null;
            }

            if (root.name == name)
            {
                return root;
            }

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChild(root.GetChild(i), name);
                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }
        static string FormatPower(MartyrPower power)
        {
            switch (power)
            {
                case MartyrPower.Reinforcements:
                    return Loc.Get("martyr.power.reinforcements");
                case MartyrPower.FleetPawns:
                    return Loc.Get("martyr.power.fleet");
                case MartyrPower.Bombard:
                    return Loc.Get("martyr.power.bombard");
                case MartyrPower.UntouchableKing:
                    return Loc.Get("martyr.power.untouchable");
                case MartyrPower.StasisField:
                    return Loc.Get("martyr.power.stasis");
                case MartyrPower.KnightAscension:
                    return Loc.Get("martyr.power.ascension");
                case MartyrPower.BattlefieldPromotion:
                    return Loc.Get("martyr.power.battlefield");
                case MartyrPower.Rally:
                    return Loc.Get("martyr.power.rally");
                case MartyrPower.Revival:
                    return Loc.Get("martyr.power.revival");
                case MartyrPower.Exile:
                    return Loc.Get("martyr.power.exile");
                case MartyrPower.Phalanx:
                    return Loc.Get("martyr.power.phalanx");
                default:
                    throw new ArgumentOutOfRangeException(nameof(power), power, null);
            }
        }
        static string FormatResult(GameState state)
        {
            switch (state.Status)
            {
                case GameStatus.InProgress:
                    return string.Empty;
                case GameStatus.Checkmate:
                    Side winner = state.SideToMove == Side.White ? Side.Black : Side.White;
                    return Loc.Format("result.checkmate", Loc.SideName(winner));
                case GameStatus.Stalemate:
                    return Loc.Get("result.stalemate");
                case GameStatus.Draw:
                    return Loc.Get("result.draw");
                case GameStatus.Timeout:
                    return Loc.Format("result.timeout", Loc.SideName(state.SideToMove));
                case GameStatus.Resign:
                    return Loc.Format("result.resign", Loc.SideName(state.SideToMove));
                case GameStatus.Aborted:
                    return Loc.Get("result.aborted");
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state.Status, null);
            }
        }
        static string FormatMoveList(IReadOnlyList<Move> moves)
        {
            if (moves == null || moves.Count == 0)
            {
                return string.Empty;
            }

            var builder = new StringBuilder();
            for (int i = 0; i < moves.Count; i++)
            {
                if (i % 2 == 0)
                {
                    if (i > 0)
                    {
                        builder.Append('\n');
                    }

                    builder.Append((i / 2) + 1);
                    builder.Append(". ");
                }
                else
                {
                    builder.Append("  ");
                }

                builder.Append(FormatMove(moves[i]));
            }

            return builder.ToString();
        }
        static string FormatMove(Move move)
        {
            switch (move.Kind)
            {
                case MoveKind.CastleKingSide:
                    return "O-O";
                case MoveKind.CastleQueenSide:
                    return "O-O-O";
                case MoveKind.Quiet:
                case MoveKind.Capture:
                case MoveKind.EnPassant:
                case MoveKind.Promotion:
                case MoveKind.Swap:
                case MoveKind.Bombard:
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(move), move.Kind, null);
            }

            string text = $"{move.From}-{move.To}";
            if (move.PromotionType is PieceType promotion)
            {
                text += $"={PromotionLetter(promotion)}";
            }

            return text;
        }
        static string PromotionLetter(PieceType type)
        {
            switch (type)
            {
                case PieceType.Queen:
                    return "Q";
                case PieceType.Rook:
                    return "R";
                case PieceType.Bishop:
                    return "B";
                case PieceType.Knight:
                    return "N";
                case PieceType.Pawn:
                case PieceType.King:
                    return type.ToString();
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        #endregion
    }
}
