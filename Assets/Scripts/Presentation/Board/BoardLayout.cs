using ModularChess.Core;
using UnityEngine;

namespace ModularChess.Presentation
{
    public readonly struct BoardLayout
    {
        public const int FileCount = 8;
        public const int RankCount = 8;

        public BoardLayout(float squareSize)
        {
            SquareSize = squareSize > 0f ? squareSize : 1f;
        }

        public float SquareSize { get; }

        public Vector3 BoardCenterLocal => new Vector3(
            FileCount * SquareSize * 0.5f,
            RankCount * SquareSize * 0.5f,
            0f);

        public Vector3 BoardSizeLocal => new Vector3(FileCount * SquareSize, RankCount * SquareSize, 0f);

        public Vector3 SquareCenterLocal(Square square, Side viewer)
        {
            Square display = Display(square, viewer);
            return new Vector3(
                (display.File + 0.5f) * SquareSize,
                (display.Rank + 0.5f) * SquareSize,
                0f);
        }

        public Vector3 SquareCenterLocal(Square square)
        {
            return SquareCenterLocal(square, Side.White);
        }
        public Vector3 CaptureSlotLocal(bool playerSide, int index)
        {
            float x = playerSide ? -1.5f * SquareSize : (FileCount + 1.5f) * SquareSize;
            float y = (index + 0.5f) * SquareSize;
            return new Vector3(x, y, 0f);
        }

        public bool TryGetSquare(Vector3 localPoint, out Square square, Side viewer)
        {
            int file = Mathf.FloorToInt(localPoint.x / SquareSize);
            int rank = Mathf.FloorToInt(localPoint.y / SquareSize);
            if (file < 0 || file >= FileCount || rank < 0 || rank >= RankCount)
            {
                square = default;
                return false;
            }

            square = Display(new Square(file, rank), viewer);
            return square.IsOnBoard;
        }

        public bool TryGetSquare(Vector3 localPoint, out Square square)
        {
            return TryGetSquare(localPoint, out square, Side.White);
        }

        public static Square Display(Square square, Side viewer)
        {
            if (viewer == Side.White)
            {
                return square;
            }

            return new Square(FileCount - 1 - square.File, RankCount - 1 - square.Rank);
        }
    }

    internal static class BoardRenderOrder
    {
        public const int Square = 0;
        public const int LastMove = 1;
        public const int Selected = 2;
        public const int Legal = 3;
        public const int PieceOutline = 4;
        public const int PieceBody = 5;
        public const int PieceGlyph = 6;
        public const int Cover = 10;
    }
}
