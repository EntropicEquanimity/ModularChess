using System;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using ModularChess.Core;
using ModularChess.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] TMP_Text turnText;
        [SerializeField] TMP_Text checkText;
        [SerializeField] TMP_Text moveListText;
        [SerializeField] TMP_Text gameOverText;
        [SerializeField] GameObject gameOverBanner;

        TMP_Text _statusLine;
        TMP_Text _clockText;
        Button _endTurnButton;
        Button _pauseButton;
        Button _resignButton;
        Button _leaveButton;
        Button _setupConfirmButton;
        TMP_Text _lostMaterialText;
        Transform _draftRow;
        RectTransform _draftDescription;
        Tween _draftTween;
        Action<MartyrPower> _onDraft;
        bool _draftPromptShown;

        void Awake()
        {
            EnsureBuilt();
        }

        void OnDestroy()
        {
            _draftTween?.Kill();
        }

        public void Bind(GameState state, IReadOnlyList<Move> moves)
        {
            EnsureBuilt();
            if (state == null)
                return;

            bool inProgress = state.Status == GameStatus.InProgress;
            turnText.text = inProgress ? $"{state.SideToMove} to move" : "Match over";
            checkText.gameObject.SetActive(inProgress && state.IsInCheck);
            checkText.text = "Check";
            bool showMoves = PlayerPrefs.GetInt("ShowNotation", 1) == 1;
            moveListText.gameObject.SetActive(showMoves);
            moveListText.text = showMoves ? FormatMoveList(moves) : string.Empty;

            string result = FormatResult(state);
            bool showResult = result.Length > 0;
            gameOverBanner.SetActive(showResult);
            gameOverText.text = result;
        }

        public void BindActions(MatchController controller)
        {
            EnsureBuilt();
            _endTurnButton.onClick.RemoveAllListeners();
            _endTurnButton.onClick.AddListener(controller.RequestEndTurn);
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(controller.TogglePause);
            _resignButton.onClick.RemoveAllListeners();
            _resignButton.onClick.AddListener(controller.Resign);
            _leaveButton.onClick.RemoveAllListeners();
            _leaveButton.onClick.AddListener(controller.LeaveToMenu);
            if (_setupConfirmButton != null)
            {
                _setupConfirmButton.onClick.RemoveAllListeners();
                _setupConfirmButton.onClick.AddListener(controller.ConfirmSetup);
            }
        }

        public void SetClock(MatchClock clock)
        {
            EnsureBuilt();
            if (_clockText == null)
                return;
            if (clock == null || clock.IsNone)
            {
                _clockText.text = string.Empty;
                return;
            }

            _clockText.text = $"W {clock.Format(Side.White)}   B {clock.Format(Side.Black)}";
        }

        public void SetEndTurnVisible(bool visible)
        {
            EnsureBuilt();
            if (_endTurnButton != null)
                _endTurnButton.gameObject.SetActive(visible);
        }

        public void SetPauseVisible(bool visible)
        {
            EnsureBuilt();
            if (_pauseButton != null)
                _pauseButton.gameObject.SetActive(visible);
        }

        public void SetResignVisible(bool visible)
        {
            EnsureBuilt();
            if (_resignButton != null)
                _resignButton.gameObject.SetActive(visible);
        }

        public void SetSetupConfirmVisible(bool visible)
        {
            EnsureBuilt();
            if (_setupConfirmButton != null)
                _setupConfirmButton.gameObject.SetActive(visible);
        }

        public void SetLostMaterial(int? white, int? black, int threshold)
        {
            EnsureBuilt();
            if (_lostMaterialText == null)
                return;
            if (white == null || black == null)
            {
                _lostMaterialText.text = string.Empty;
                return;
            }

            _lostMaterialText.text = $"Lost W {white.Value}  B {black.Value}  next {threshold}";
        }

        public void SetStatusLine(string text)
        {
            EnsureBuilt();
            if (_statusLine != null)
                _statusLine.text = text ?? string.Empty;
        }

        public void ShowDraft(GameState state, Action<MartyrPower> onPick)
        {
            EnsureBuilt();
            _onDraft = onPick;
            if (_draftRow == null || state?.Runtime.PendingDraft == null)
                return;

            _draftRow.gameObject.SetActive(true);
            DraftOffer offer = state.Runtime.PendingDraft.Value;
            SetDraftButton(0, offer.First);
            SetDraftButton(1, offer.Second);
            SetDraftButton(2, offer.Third);
            ShowDraftDescription();
        }

        public void HideDraft()
        {
            _draftTween?.Kill();
            _draftPromptShown = false;
            if (_draftRow != null)
                _draftRow.gameObject.SetActive(false);
            if (_draftDescription != null)
                _draftDescription.gameObject.SetActive(false);
        }

        void ShowDraftDescription()
        {
            if (_draftDescription == null)
                return;
            _draftDescription.gameObject.SetActive(true);
            if (_draftPromptShown)
                return;
            _draftPromptShown = true;
            float shownY = _draftDescription.anchoredPosition.y;
            _draftDescription.anchoredPosition = new Vector2(_draftDescription.anchoredPosition.x, shownY - 64f);
            _draftTween?.Kill();
            _draftTween = DOTween.To(
                    () => _draftDescription.anchoredPosition,
                    v => _draftDescription.anchoredPosition = v,
                    new Vector2(_draftDescription.anchoredPosition.x, shownY),
                    0.28f)
                .SetEase(Ease.OutCubic)
                .SetUpdate(true)
                .SetTarget(_draftDescription);
        }

        void SetDraftButton(int index, MartyrPower power)
        {
            Transform child = _draftRow.GetChild(index);
            var button = child.GetComponent<Button>();
            TMP_Text label = child.GetComponentInChildren<TMP_Text>();
            if (label != null)
                label.text = FormatPower(power);
            button.onClick.RemoveAllListeners();
            MartyrPower captured = power;
            button.onClick.AddListener(() => _onDraft?.Invoke(captured));
        }

        void EnsureBuilt()
        {
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
                root = gameObject.AddComponent<RectTransform>();

            if (turnText == null)
                turnText = PlaceLabel(root, "TurnLabel", 16, new Vector2(0.2f, 0.9f), new Vector2(0.8f, 1f));
            if (checkText == null)
            {
                checkText = PlaceLabel(root, "CheckLabel", 16, new Vector2(0.2f, 0.84f), new Vector2(0.8f, 0.92f));
                checkText.color = new Color(0.7f, 0.1f, 0.1f, 1f);
                checkText.gameObject.SetActive(false);
            }

            if (moveListText == null)
            {
                moveListText = PlaceLabel(root, "MoveList", 16, new Vector2(0f, 0.08f), new Vector2(0.28f, 0.84f));
                moveListText.alignment = TextAlignmentOptions.TopLeft;
                moveListText.textWrappingMode = TextWrappingModes.Normal;
            }

            if (gameOverBanner == null)
            {
                RectTransform banner = UiFactory.Panel(root, new Vector2(420f, 120f));
                banner.name = "GameOverBanner";
                banner.anchorMin = new Vector2(0.5f, 0.5f);
                banner.anchorMax = new Vector2(0.5f, 0.5f);
                banner.anchoredPosition = Vector2.zero;
                banner.gameObject.SetActive(false);
                gameOverBanner = banner.gameObject;
            }

            if (gameOverText == null)
            {
                gameOverText = gameOverBanner.GetComponentInChildren<TMP_Text>();
                if (gameOverText == null)
                    gameOverText = UiFactory.Label(gameOverBanner.transform, string.Empty, 32, TextAlignmentOptions.Center);
                gameOverText.color = Color.black;
                Stretch(gameOverText.rectTransform);
            }

            if (_statusLine == null)
                _statusLine = PlaceLabel(root, "StatusLine", 16, new Vector2(0.15f, 0f), new Vector2(0.85f, 0.08f));
            if (_clockText == null)
                _clockText = PlaceLabel(root, "Clock", 16, new Vector2(0.7f, 0.9f), new Vector2(1f, 1f));
            if (_lostMaterialText == null)
            {
                _lostMaterialText = PlaceLabel(root, "LostMaterial", 16, new Vector2(0f, 0.9f), new Vector2(0.3f, 1f));
                _lostMaterialText.alignment = TextAlignmentOptions.MidlineLeft;
            }

            if (_leaveButton == null)
            {
                _endTurnButton = PlaceButton(root, "End Turn", new Vector2(0.78f, 0.12f), new Vector2(0.98f, 0.2f));
                _pauseButton = PlaceButton(root, "Pause", new Vector2(0.78f, 0.22f), new Vector2(0.98f, 0.3f));
                _resignButton = PlaceButton(root, "Resign", new Vector2(0.78f, 0.32f), new Vector2(0.98f, 0.4f));
                _leaveButton = PlaceButton(root, "Leave", new Vector2(0.78f, 0.42f), new Vector2(0.98f, 0.5f));
                _setupConfirmButton = PlaceButton(root, "Confirm Setup", new Vector2(0.78f, 0.52f), new Vector2(0.98f, 0.6f));
                _endTurnButton.gameObject.SetActive(false);
                _pauseButton.gameObject.SetActive(false);
                _setupConfirmButton.gameObject.SetActive(false);
            }

            if (_setupConfirmButton == null)
            {
                _setupConfirmButton = PlaceButton(root, "Confirm Setup", new Vector2(0.78f, 0.52f), new Vector2(0.98f, 0.6f));
                _setupConfirmButton.gameObject.SetActive(false);
            }

            if (_draftRow == null)
            {
                TMP_Text description = UiFactory.DescriptionBox(root, "Choose one power.", new Vector2(420f, 64f));
                description.fontSize = 16;
                _draftDescription = description.rectTransform.parent as RectTransform;
                if (_draftDescription == null)
                    _draftDescription = description.rectTransform;
                _draftDescription.anchorMin = new Vector2(0.5f, 0.22f);
                _draftDescription.anchorMax = new Vector2(0.5f, 0.22f);
                _draftDescription.pivot = new Vector2(0.5f, 0f);
                _draftDescription.anchoredPosition = Vector2.zero;
                _draftDescription.gameObject.SetActive(false);

                var draft = new GameObject("DraftRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                draft.transform.SetParent(root, false);
                var draftRect = draft.GetComponent<RectTransform>();
                draftRect.anchorMin = new Vector2(0.2f, 0.08f);
                draftRect.anchorMax = new Vector2(0.8f, 0.2f);
                draftRect.offsetMin = Vector2.zero;
                draftRect.offsetMax = Vector2.zero;
                var layout = draft.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childForceExpandWidth = true;
                layout.childForceExpandHeight = true;
                layout.childControlWidth = true;
                layout.childControlHeight = true;
                _draftRow = draft.transform;
                for (int i = 0; i < 3; i++)
                {
                    Button button = UiFactory.Button(draftRect, "Power", null, new Vector2(200f, 32f));
                    var element = button.gameObject.AddComponent<LayoutElement>();
                    element.minWidth = 200f;
                    element.flexibleWidth = 1f;
                    element.minHeight = 32f;
                    element.preferredHeight = 32f;
                }

                draft.SetActive(false);
            }
        }

        static TMP_Text PlaceLabel(RectTransform root, string name, int size, Vector2 anchorMin, Vector2 anchorMax)
        {
            TMP_Text label = UiFactory.Label(root, string.Empty, size, TextAlignmentOptions.Center);
            label.gameObject.name = name;
            Stretch(label.rectTransform, anchorMin, anchorMax);
            return label;
        }

        static Button PlaceButton(RectTransform root, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            Button button = UiFactory.Button(root, label, null, new Vector2(200f, 32f));
            button.name = label;
            Stretch(button.GetComponent<RectTransform>(), anchorMin, anchorMax);
            return button;
        }

        static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static void Stretch(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        static string FormatPower(MartyrPower power)
        {
            switch (power)
            {
                case MartyrPower.Reinforcements:
                    return "Reinforcements";
                case MartyrPower.FleetPawns:
                    return "Fleet Pawns";
                case MartyrPower.Bombard:
                    return "Bombard";
                case MartyrPower.UntouchableKing:
                    return "Untouchable King";
                case MartyrPower.StasisField:
                    return "Stasis Field";
                case MartyrPower.KnightAscension:
                    return "Knight Ascension";
                case MartyrPower.BattlefieldPromotion:
                    return "Battlefield Promotion";
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
                    return $"{winner} wins by checkmate";
                case GameStatus.Stalemate:
                    return "Stalemate";
                case GameStatus.Draw:
                    return "Draw";
                case GameStatus.Timeout:
                    return $"{state.SideToMove} loses on time";
                case GameStatus.Resign:
                    return $"{state.SideToMove} resigns";
                case GameStatus.Aborted:
                    return "Match aborted";
                default:
                    throw new ArgumentOutOfRangeException(nameof(state), state.Status, null);
            }
        }

        static string FormatMoveList(IReadOnlyList<Move> moves)
        {
            if (moves == null || moves.Count == 0)
                return string.Empty;

            var builder = new StringBuilder();
            for (int i = 0; i < moves.Count; i++)
            {
                if (i % 2 == 0)
                {
                    if (i > 0)
                        builder.Append('\n');
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
                text += $"={PromotionLetter(promotion)}";
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
    }
}
