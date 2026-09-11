using System;
using System.Collections.Generic;
using System.Text;
using ModularChess.Core;
using ModularChess.Presentation;
using UnityEngine;
using UnityEngine.UI;

namespace ModularChess.Match
{
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private Text turnText;
        [SerializeField] private Text checkText;
        [SerializeField] private Text moveListText;
        [SerializeField] private Text gameOverText;
        [SerializeField] private GameObject gameOverBanner;
        Text _statusLine;
        Text _clockText;
        Button _endTurnButton;
        Button _pauseButton;
        Button _resignButton;
        Button _leaveButton;
        Transform _draftRow;
        Action<MartyrPower> _onDraft;

        private void Awake()
        {
            EnsureBuilt();
        }

        public void Bind(GameState state, IReadOnlyList<Move> moves)
        {
            EnsureBuilt();
            if (state == null)
            {
                return;
            }

            bool inProgress = state.Status == GameStatus.InProgress;
            turnText.text = inProgress ? $"{state.SideToMove} to move" : "Game over";
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

        private void EnsureBuilt()
        {
            if (turnText != null
                && checkText != null
                && moveListText != null
                && gameOverText != null
                && gameOverBanner != null)
            {
                return;
            }

            Font font = BuiltinFont();
            RectTransform root = GetComponent<RectTransform>();
            if (root == null)
            {
                root = gameObject.AddComponent<RectTransform>();
            }

            if (turnText == null)
            {
                turnText = CreateText(
                    "TurnLabel",
                    root,
                    font,
                    36,
                    TextAnchor.UpperCenter,
                    Color.white,
                    new Vector2(0.2f, 0.9f),
                    new Vector2(0.8f, 1f),
                    new Vector2(12f, -8f),
                    new Vector2(-12f, -4f));
            }

            if (checkText == null)
            {
                checkText = CreateText(
                    "CheckLabel",
                    root,
                    font,
                    28,
                    TextAnchor.UpperCenter,
                    new Color(1f, 0.35f, 0.3f, 1f),
                    new Vector2(0.2f, 0.84f),
                    new Vector2(0.8f, 0.92f),
                    new Vector2(12f, 0f),
                    new Vector2(-12f, 0f));
                checkText.gameObject.SetActive(false);
            }

            if (moveListText == null)
            {
                moveListText = CreateText(
                    "MoveList",
                    root,
                    font,
                    20,
                    TextAnchor.UpperLeft,
                    new Color(0.92f, 0.92f, 0.88f, 1f),
                    new Vector2(0f, 0.08f),
                    new Vector2(0.28f, 0.84f),
                    new Vector2(16f, 12f),
                    new Vector2(-8f, -12f));
                moveListText.alignment = TextAnchor.UpperLeft;
                moveListText.horizontalOverflow = HorizontalWrapMode.Wrap;
                moveListText.verticalOverflow = VerticalWrapMode.Overflow;
            }

            if (gameOverBanner == null)
            {
                GameObject banner = new GameObject("GameOverBanner", typeof(RectTransform), typeof(Image));
                banner.transform.SetParent(root, false);
                RectTransform bannerRect = banner.GetComponent<RectTransform>();
                bannerRect.anchorMin = new Vector2(0.18f, 0.38f);
                bannerRect.anchorMax = new Vector2(0.82f, 0.62f);
                bannerRect.offsetMin = Vector2.zero;
                bannerRect.offsetMax = Vector2.zero;
                Image background = banner.GetComponent<Image>();
                background.color = new Color(0f, 0f, 0f, 0.72f);
                background.raycastTarget = false;
                banner.SetActive(false);
                gameOverBanner = banner;
            }

            if (gameOverText == null)
            {
                gameOverText = CreateText(
                    "GameOverLabel",
                    gameOverBanner.GetComponent<RectTransform>(),
                    font,
                    44,
                    TextAnchor.MiddleCenter,
                    Color.white,
                    Vector2.zero,
                    Vector2.one,
                    new Vector2(16f, 12f),
                    new Vector2(-16f, -12f));
            }
        }

        private static Text CreateText(
            string objectName,
            Transform parent,
            Font font,
            int fontSize,
            TextAnchor alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text), typeof(Shadow));
            textObject.transform.SetParent(parent, false);

            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            Shadow shadow = textObject.GetComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.75f);
            shadow.effectDistance = new Vector2(1.5f, -1.5f);

            return text;
        }

        private static Font BuiltinFont()
        {
            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private static string FormatResult(GameState state)
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

        private static string FormatMoveList(IReadOnlyList<Move> moves)
        {
            if (moves == null || moves.Count == 0)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
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

        private static string FormatMove(Move move)
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

        private static string PromotionLetter(PieceType type)
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

        public void BindActions(MatchController controller)
        {
            EnsureBuilt();
            EnsureActions();
            _endTurnButton.onClick.RemoveAllListeners();
            _endTurnButton.onClick.AddListener(controller.RequestEndTurn);
            _pauseButton.onClick.RemoveAllListeners();
            _pauseButton.onClick.AddListener(controller.TogglePause);
            _resignButton.onClick.RemoveAllListeners();
            _resignButton.onClick.AddListener(controller.Resign);
            _leaveButton.onClick.RemoveAllListeners();
            _leaveButton.onClick.AddListener(controller.LeaveToMenu);
        }

        public void SetClock(MatchClock clock)
        {
            EnsureBuilt();
            EnsureActions();
            if (_clockText == null)
            {
                return;
            }

            if (clock == null || clock.IsNone)
            {
                _clockText.text = string.Empty;
                return;
            }

            _clockText.text = $"W {clock.Format(Side.White)}   B {clock.Format(Side.Black)}";
        }

        public void SetEndTurnVisible(bool visible)
        {
            EnsureActions();
            if (_endTurnButton != null)
            {
                _endTurnButton.gameObject.SetActive(visible);
            }
        }

        public void SetStatusLine(string text)
        {
            EnsureActions();
            if (_statusLine != null)
            {
                _statusLine.text = text ?? string.Empty;
            }
        }

        public void ShowDraft(GameState state, Action<MartyrPower> onPick)
        {
            EnsureActions();
            _onDraft = onPick;
            if (_draftRow == null || state?.Runtime.PendingDraft == null)
            {
                return;
            }

            _draftRow.gameObject.SetActive(true);
            DraftOffer offer = state.Runtime.PendingDraft.Value;
            SetDraftButton(0, offer.First);
            SetDraftButton(1, offer.Second);
            SetDraftButton(2, offer.Third);
        }

        public void HideDraft()
        {
            if (_draftRow != null)
            {
                _draftRow.gameObject.SetActive(false);
            }
        }

        void SetDraftButton(int index, MartyrPower power)
        {
            Transform child = _draftRow.GetChild(index);
            var button = child.GetComponent<Button>();
            var label = child.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = power.ToString();
            }

            button.onClick.RemoveAllListeners();
            MartyrPower captured = power;
            button.onClick.AddListener(() => _onDraft?.Invoke(captured));
        }

        void EnsureActions()
        {
            if (_leaveButton != null)
            {
                return;
            }

            RectTransform root = GetComponent<RectTransform>();
            Font font = BuiltinFont();
            _statusLine = CreateText(
                "StatusLine",
                root,
                font,
                22,
                TextAnchor.LowerCenter,
                Color.white,
                new Vector2(0.15f, 0f),
                new Vector2(0.85f, 0.08f),
                new Vector2(8f, 8f),
                new Vector2(-8f, 4f));
            _clockText = CreateText(
                "Clock",
                root,
                font,
                22,
                TextAnchor.UpperRight,
                Color.white,
                new Vector2(0.7f, 0.9f),
                new Vector2(1f, 1f),
                new Vector2(8f, -8f),
                new Vector2(-16f, -4f));

            _endTurnButton = CreateHudButton(root, "End Turn", new Vector2(0.82f, 0.12f), new Vector2(0.98f, 0.2f));
            _pauseButton = CreateHudButton(root, "Pause", new Vector2(0.82f, 0.22f), new Vector2(0.98f, 0.3f));
            _resignButton = CreateHudButton(root, "Resign", new Vector2(0.82f, 0.32f), new Vector2(0.98f, 0.4f));
            _leaveButton = CreateHudButton(root, "Leave", new Vector2(0.82f, 0.42f), new Vector2(0.98f, 0.5f));
            _endTurnButton.gameObject.SetActive(false);

            var draft = new GameObject("DraftRow", typeof(RectTransform));
            draft.transform.SetParent(root, false);
            var draftRect = draft.GetComponent<RectTransform>();
            draftRect.anchorMin = new Vector2(0.2f, 0.08f);
            draftRect.anchorMax = new Vector2(0.8f, 0.2f);
            draftRect.offsetMin = Vector2.zero;
            draftRect.offsetMax = Vector2.zero;
            var layout = draft.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
            _draftRow = draft.transform;
            for (int i = 0; i < 3; i++)
            {
                CreateHudButton(draftRect, "Power", new Vector2(0f, 0f), new Vector2(1f, 1f));
            }

            draft.SetActive(false);
        }

        static Button CreateHudButton(Transform parent, string label, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = new Color(0.15f, 0.16f, 0.2f, 0.9f);
            var text = CreateText(
                "Label",
                rect,
                BuiltinFont(),
                20,
                TextAnchor.MiddleCenter,
                Color.white,
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            text.raycastTarget = false;
            text.text = label;
            return go.GetComponent<Button>();
        }
    }
}
