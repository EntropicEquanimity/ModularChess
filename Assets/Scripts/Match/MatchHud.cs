using System;
using System.Collections.Generic;
using System.Text;
using ModularChess.Core;
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
            moveListText.text = FormatMoveList(moves);

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
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            return font;
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
    }
}
